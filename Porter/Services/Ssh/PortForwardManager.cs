using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Porter.Models;

namespace Porter.Services.Ssh;

public class PortForwardManager(IPrivateKeyCache privateKeyCache) : IDisposable
{
	#region Constants

	private const string LOCALHOST = "127.0.0.1";

	#endregion

	#region Private Fields

	private bool _disposed;

	private readonly IPrivateKeyCache _privateKeyCache = privateKeyCache;

	private readonly ConcurrentDictionary<SshConnectionOptions, SemaphoreSlim> _locks = new();

	private readonly ConcurrentDictionary<SshConnectionOptions, SshConnection> _connections = new();

	private readonly ConcurrentDictionary<LocalPortForwardKey, SshTunnel> _forwardToTunnel = new();

	#endregion

	#region Events

	public event Action<SshTunnel, Exception>? TunnelFailed;

	#endregion

	#region Public Methods

	public Task<bool> StartForward(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		return StartForwardInternal(tunnel, promptPassphrase, cancellationToken);
	}

	public void StopForward(SshTunnel tunnel)
	{
		if (TryCreateLocalPortForward(tunnel) is not { } localPortForward)
		{
			return;
		}

		_forwardToTunnel.TryRemove(localPortForward, out _);

		if (GetConnectionByPortForward(localPortForward) is not { } connection)
			return;

		if (connection.IsForwardStarted(localPortForward))
		{
			connection.StopForward(localPortForward);
		}

		// If no forwards remain on this connection, drop it. The decrypted private key (if any) lives
		// in IPrivateKeyCache and survives the connection's disposal, so reconnecting later does NOT
		// require asking the user for the passphrase again.
		if (connection.Forwards.Count != 0)
			return;

		var options = FindOptionsForConnection(connection);
		if (options is null)
			return;

		var semaphore = _locks.GetOrAdd(options, _ => new SemaphoreSlim(1, 1));
		semaphore.Wait();
		try
		{
			if (connection.Forwards.Count != 0)
				return;

			if (_connections.TryRemove(options, out var removed))
			{
				removed.Dispose();
			}
		}
		finally
		{
			semaphore.Release();
		}
	}

	public bool IsForwardStarted(SshTunnel tunnel)
	{
		if (TryCreateLocalPortForward(tunnel) is not { } localPortForward)
		{
			return false;
		}

		return GetConnectionByPortForward(localPortForward) is { } connection && connection.IsForwardStarted(localPortForward);
	}

	public bool IsAnyForwardStarted()
	{
		return _connections.Values.Select(c => (Connection: c, c.Forwards)).Any(x => x.Forwards.Any(x.Connection.IsForwardStarted));
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Protected Methods

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			foreach (var connection in _connections.Values)
			{
				connection.Dispose();
			}

			_connections.Clear();

			foreach (var semaphore in _locks.Values)
			{
				semaphore.Dispose();
			}

			_locks.Clear();
		}

		_disposed = true;
	}

	#endregion

	#region Private Methods

	private async Task<bool> StartForwardInternal(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		if (tunnel.SshServer?.User is null || tunnel.SshServer?.Host is null)
		{
			return false;
		}

		var options = new SshConnectionOptions(tunnel.SshServer.User, tunnel.SshServer.Host, tunnel.SshServer.Port, tunnel.PrivateKey?.FilePath);

		var semaphore = _locks.GetOrAdd(options, _ => new SemaphoreSlim(1, 1));

		await semaphore.WaitAsync(cancellationToken);

		SshConnection? connection = null;
		var connectionWasCreated = false;

		try
		{
			if (!_connections.TryGetValue(options, out connection))
			{
				connection = new SshConnection(options, _privateKeyCache);
				connectionWasCreated = true;

				if (!await connection.InitializeAsync(promptPassphrase, cancellationToken))
				{
					connection.Dispose();
					return false;
				}

				_connections.TryAdd(options, connection);
			}

			if (!connection.IsConnected && !await connection.ConnectAsync(cancellationToken))
			{
				CleanupConnectionIfNeeded(options, connection, connectionWasCreated);
				return false;
			}

			if (TryCreateLocalPortForward(tunnel) is not { } localPortForward)
			{
				CleanupConnectionIfNeeded(options, connection, connectionWasCreated);
				return false;
			}

			// Check if cancellation was requested before starting the forward.
			// This prevents starting forwards when Stop() was called during connection.
			cancellationToken.ThrowIfCancellationRequested();

			if (!connection.IsForwardStarted(localPortForward))
			{
				_forwardToTunnel[localPortForward] = tunnel;

				connection.StartForward(localPortForward, ex =>
				{
					if (_forwardToTunnel.TryRemove(localPortForward, out var t))
					{
						TunnelFailed?.Invoke(t, ex);
					}
				});
			}

			return true;
		}
		catch
		{
			// If exception occurred and connection was just created without any forwards,
			// remove it from the pool to prevent it from being reused.
			CleanupConnectionIfNeeded(options, connection, connectionWasCreated);
			throw;
		}
		finally
		{
			semaphore.Release();
		}
	}

	private void CleanupConnectionIfNeeded(SshConnectionOptions options, SshConnection? connection, bool wasCreated)
	{
		if (wasCreated && connection is not null && connection.Forwards.Count == 0)
		{
			_connections.TryRemove(options, out _);
			connection.Dispose();
		}
	}

	private SshConnection? GetConnectionByPortForward(LocalPortForwardKey portForward)
	{
		return _connections.Values.FirstOrDefault(c => c.Forwards.Contains(portForward));
	}

	private SshConnectionOptions? FindOptionsForConnection(SshConnection connection)
	{
		return _connections.FirstOrDefault(p => ReferenceEquals(p.Value, connection)).Key;
	}

	private static LocalPortForwardKey? TryCreateLocalPortForward(SshTunnel tunnel)
	{
		if (tunnel.RemoteServer?.Host is null || tunnel.RemoteServer?.Port is null)
		{
			return null;
		}

		return new LocalPortForwardKey(LOCALHOST, (uint?)tunnel.LocalPort, tunnel.RemoteServer.Host, (uint)tunnel.RemoteServer.Port);
	}

	#endregion
}
