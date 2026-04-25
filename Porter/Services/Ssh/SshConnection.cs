using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Renci.SshNet;
using Renci.SshNet.Common;

namespace Porter.Services.Ssh;

public class SshConnection(SshConnectionOptions options) : IDisposable
{
	#region Private Fields

	private bool _disposed;

	private SshClient? _sshClient;

	private readonly ConcurrentDictionary<LocalPortForwardKey, ForwardedPortLocal> _forwards = new();

	private readonly SshConnectionOptions _options = options;

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
			if (await GetPrivateKeyFile(promptPassphrase) is { } keyFile)
			{
				auth = new PrivateKeyAuthenticationMethod(_options.User, keyFile);
			}
			else
			{
				return false;
			}
		}

		var connectionInfo = _options.Port switch
		{
			null => new ConnectionInfo(_options.Host, _options.User, auth),
			_ => new ConnectionInfo(_options.Host, _options.Port.Value, _options.User, auth)
		};


		_sshClient = new SshClient(connectionInfo)
		{
			KeepAliveInterval = TimeSpan.FromMinutes(5)
		};

		return true;
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
		var forward = AddOrGetLocalForward(portForward, exceptionCallback);
		forward?.Start();
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

	private async Task<PrivateKeyFile?> GetPrivateKeyFile(Func<Task<string?>>? promptPassphrase)
	{
		string? passphrase = null;

		try
		{
			do
			{
				try
				{
					if (_options.PrivateKeyFilePath is null)
						return null;

					if (passphrase is null)
						return new PrivateKeyFile(_options.PrivateKeyFilePath);

					return new PrivateKeyFile(_options.PrivateKeyFilePath, passphrase);

				}
				catch (SshException ex) when (ex is SshPassPhraseNullOrEmptyException ||
											  // handling Renci.SshNet.Common.SshException: 'MAC verification failed for PuTTY key file'
											  ex.Message.Contains("putty", StringComparison.OrdinalIgnoreCase))
				{
					if (promptPassphrase is null)
						break;

					passphrase = await promptPassphrase();
				}
			}
			while (passphrase is not null);
		}
		catch
		{
			return null;
		}

		return null;
	}

	private ForwardedPortLocal? AddOrGetLocalForward(LocalPortForwardKey portForward, Action<Exception>? exceptionCallback = null)
	{
		if (_sshClient is null)
			return null;

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

		ForwardedPortLocal forward;

		if (portForward.BoundHost is null && portForward.BoundPort is null)
		{
			throw new InvalidOperationException("At least one of BoundHost or BoundPort must be specified for local port forwarding.");
		}

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

		forward.Exception += (_, e) =>
		{
			StopForwardInternal(portForward);
			exceptionCallback?.Invoke(e.Exception);
		};

		_sshClient.AddForwardedPort(forward);
		Debug.WriteLine($"Forwarding added to _sshClient, _sshClient.ForwardedPorts.Count = {_sshClient.ForwardedPorts.Count()}");
		_forwards[portForward] = forward;
		Debug.WriteLine($"Forwarding added to _forwards, _forwards.Count = {_forwards.Count}");
		return forward;
	}

	private void StopForwardInternal(LocalPortForwardKey portForward)
	{
		if (_forwards.TryRemove(portForward, out var forward))
		{
			forward.Stop();

			_sshClient?.RemoveForwardedPort(forward);

			Debug.WriteLine($"Forwarding removed from _sshClient and _forwards;" +
				$"\n_sshClient.ForwardedPorts.Count = {_sshClient?.ForwardedPorts.Count()}" +
				$"\n_forwards.Count = {_forwards.Count}");
		}
	}

	#endregion
}
