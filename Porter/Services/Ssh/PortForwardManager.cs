using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

	private readonly ConcurrentDictionary<Guid, LocalPortForwardKey> _tunnelIdToForward = new();

	#endregion

	#region Events

	public event Action<Guid, Exception>? TunnelFailed;

	#endregion

	#region Public Methods

	public Task<bool> StartForward(
		LocalPortForwardOptions options,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		return StartForwardInternal(options, promptPassphrase, cancellationToken);
	}

	public void StopForward(Guid tunnelId)
	{
		if (!_tunnelIdToForward.TryRemove(tunnelId, out var localPortForward))
			return;

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

	public bool IsForwardStarted(Guid tunnelId)
	{
		if (!_tunnelIdToForward.TryGetValue(tunnelId, out var localPortForward))
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
		LocalPortForwardOptions forwardOptions,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		if (forwardOptions.SshServerUser is null || forwardOptions.SshServerHost is null)
		{
			return false;
		}

		var connectionOptions = new SshConnectionOptions(forwardOptions.SshServerUser, forwardOptions.SshServerHost, forwardOptions.SshServerPort, forwardOptions.PrivateKeyFilePath);

		var semaphore = _locks.GetOrAdd(connectionOptions, _ => new SemaphoreSlim(1, 1));

		await semaphore.WaitAsync(cancellationToken);

		SshConnection? connection = null;
		var connectionWasCreated = false;

		try
		{
			if (!_connections.TryGetValue(connectionOptions, out connection))
			{
				connection = new SshConnection(connectionOptions, _privateKeyCache);
				connectionWasCreated = true;

				if (!await connection.InitializeAsync(promptPassphrase, cancellationToken))
				{
					connection.Dispose();
					return false;
				}

				_connections.TryAdd(connectionOptions, connection);
			}

			if (!connection.IsConnected && !await connection.ConnectAsync(cancellationToken))
			{
				CleanupConnectionIfNeeded(connectionOptions, connection, connectionWasCreated);
				return false;
			}

			if (TryCreateLocalPortForward(forwardOptions) is not { } localPortForward)
			{
				CleanupConnectionIfNeeded(connectionOptions, connection, connectionWasCreated);
				return false;
			}

			// Check if cancellation was requested before starting the forward.
			// This prevents starting forwards when Stop() was called during connection.
			cancellationToken.ThrowIfCancellationRequested();

			if (!connection.IsForwardStarted(localPortForward))
			{
				_tunnelIdToForward[forwardOptions.TunnelId] = localPortForward;

				connection.StartForward(localPortForward, ex =>
				{
					if (_tunnelIdToForward.TryRemove(forwardOptions.TunnelId, out _))
					{
						TunnelFailed?.Invoke(forwardOptions.TunnelId, ex);
					}
				});
			}

			return true;
		}
		catch
		{
			// If exception occurred and connection was just created without any forwards,
			// remove it from the pool to prevent it from being reused.
			CleanupConnectionIfNeeded(connectionOptions, connection, connectionWasCreated);
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

	private static LocalPortForwardKey? TryCreateLocalPortForward(LocalPortForwardOptions forwardOptions)
	{
		if (forwardOptions.RemoteServerHost is null || forwardOptions.RemoteServerPort is null)
		{
			return null;
		}

		return new LocalPortForwardKey(LOCALHOST, (uint?)forwardOptions.LocalPort, forwardOptions.RemoteServerHost, (uint)forwardOptions.RemoteServerPort);
	}

	#endregion
}
