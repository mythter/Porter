using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Renci.SshNet;

namespace Porter.Services.Ssh;

public class SshConnection(SshConnectionOptions options, IPrivateKeyCache privateKeyCache) : IDisposable
{
	#region Private Fields

	private bool _disposed;

	private SshClient? _sshClient;

	private readonly ConcurrentDictionary<LocalPortForwardKey, ForwardedPortLocal> _forwards = new();

	private readonly SshConnectionOptions _options = options;

	private readonly IPrivateKeyCache _privateKeyCache = privateKeyCache;

	#endregion

	#region Public Properties

	public ICollection<LocalPortForwardKey> Forwards => _forwards.Keys;

	public bool IsConnected => _sshClient?.IsConnected ?? false;

	#endregion

	#region Public Methods

	public async Task<bool> InitializeAsync(Func<Task<string?>>? promptPassphrase = null, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return false;

		if (_sshClient is not null)
		{
			throw new InvalidOperationException("SSH client is already initialized.");
		}

		AuthenticationMethod auth = new NoneAuthenticationMethod(_options.User);

		if (_options.PrivateKeyFilePath is not null)
		{
			var keyFile = await GetPrivateKeyFileAsync(promptPassphrase).ConfigureAwait(false);
			if (keyFile is null)
				return false;

			auth = new PrivateKeyAuthenticationMethod(_options.User, keyFile);
		}

		var connectionInfo = _options.Port switch
		{
			null => new ConnectionInfo(_options.Host, _options.User, auth),
			_ => new ConnectionInfo(_options.Host, _options.Port.Value, _options.User, auth)
		};

		SshClient? client = null;
		try
		{
			client = new SshClient(connectionInfo)
			{
				KeepAliveInterval = TimeSpan.FromMinutes(5)
			};
			_sshClient = client;
			client = null; // ownership transferred
			return true;
		}
		finally
		{
			client?.Dispose();
		}
	}

	public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_sshClient is null)
		{
			throw new InvalidOperationException("SSH client is not initialized.");
		}

		if (_sshClient.IsConnected)
		{
			return true;
		}

		await _sshClient.ConnectAsync(cancellationToken);

		return true;
	}

	public void StartForward(LocalPortForwardKey portForward, Action<Exception>? exceptionCallback = null)
	{
		AddOrGetLocalForward(portForward, exceptionCallback)?.Start();
	}

	public void StopForward(LocalPortForwardKey portForward)
	{
		StopForwardInternal(portForward);
	}

	public bool IsForwardStarted(LocalPortForwardKey portForward)
	{
		return _forwards.TryGetValue(portForward, out var forward) && forward.IsStarted;
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
			foreach (var entry in _forwards.Values)
			{
				entry.Dispose();
			}

			_forwards.Clear();

			if (_sshClient?.IsConnected ?? false)
			{
				_sshClient.Disconnect();
			}

			_sshClient?.Dispose();
		}

		_disposed = true;
	}

	#endregion

	#region Private Methods

	private Task<PrivateKeyFile?> GetPrivateKeyFileAsync(Func<Task<string?>>? promptPassphrase)
	{
		var path = _options.PrivateKeyFilePath;
		if (path is null)
			return Task.FromResult<PrivateKeyFile?>(null);

		// Defer loading to the cache so the decrypted key (and its passphrase prompt) is reused
		// across multiple connect/disconnect cycles. The cache owns the key's lifetime.
		return _privateKeyCache.GetAsync(path, promptPassphrase);
	}

	private ForwardedPortLocal? AddOrGetLocalForward(LocalPortForwardKey portForward, Action<Exception>? exceptionCallback = null)
	{
		if (!_forwards.TryGetValue(portForward, out var forward))
		{
			forward = AddLocalForward(portForward, exceptionCallback);
		}

		return forward;
	}

	private ForwardedPortLocal? AddLocalForward(LocalPortForwardKey portForward, Action<Exception>? exceptionCallback = null)
	{
		if (_sshClient is null)
			return null;

		if (portForward.BoundHost is null && portForward.BoundPort is null)
		{
			throw new InvalidOperationException("At least one of BoundHost or BoundPort must be specified for local port forwarding.");
		}

		var forward = CreateForwardedPortLocal(portForward);

		forward.Exception += (_, e) =>
		{
			StopForwardInternal(portForward);
			exceptionCallback?.Invoke(e.Exception);
		};

		_sshClient.AddForwardedPort(forward);

		_forwards[portForward] = forward;

		return forward;
	}

	private void StopForwardInternal(LocalPortForwardKey portForward)
	{
		if (_forwards.TryRemove(portForward, out var forward))
		{
			forward.Stop();

			_sshClient?.RemoveForwardedPort(forward);
		}
	}

	private static ForwardedPortLocal CreateForwardedPortLocal(LocalPortForwardKey portForward)
	{
		ForwardedPortLocal forward;

		if (portForward.BoundHost is null)
		{
			forward = new ForwardedPortLocal(portForward.BoundPort!.Value, portForward.Host, portForward.Port);
		}
		else if (portForward.BoundPort is null)
		{
			forward = new ForwardedPortLocal(portForward.BoundHost, portForward.Host, portForward.Port);
		}
		else
		{
			forward = new ForwardedPortLocal(portForward.BoundHost, portForward.BoundPort!.Value, portForward.Host, portForward.Port);
		}

		return forward;
	}

	#endregion
}
