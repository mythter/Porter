using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Porter.Models;

using Renci.SshNet;
using Renci.SshNet.Common;

namespace Porter.Services
{
	public class SshConnection : IDisposable
	{
		private const string LOCALHOST = "127.0.0.1";

		private SshClient? _sshClient;

		private readonly ConcurrentDictionary<Guid, ForwardedPortLocal> _forwards = new();

		public ICollection<Guid> Tunnels => _forwards.Keys;

		public SshServer SshServer { get; }

		public PrivateKey PrivateKey { get; }

		public bool IsConnected => _sshClient?.IsConnected ?? false;

		public SshConnection(SshServer sshServer, PrivateKey privateKey)
		{
			SshServer = new SshServer
			{
				Name = sshServer.Name,
				User = sshServer.User,
				Host = sshServer.Host,
				Port = sshServer.Port,
			};

			PrivateKey = new PrivateKey(privateKey.FilePath)
			{
				Name = privateKey.Name,
			};
		}

		public async Task<bool> ConnectAsync(Func<PrivateKey, Task<string?>>? promptPassphrase = null, CancellationToken? cancellationToken = null)
		{
			if (_sshClient?.IsConnected == true || cancellationToken?.IsCancellationRequested == true)
				return _sshClient?.IsConnected ?? false;

			if (_sshClient is null)
			{
				if (await GetPrivateKeyFile(PrivateKey, promptPassphrase) is not { } keyFile)
					return false;

				var auth = new PrivateKeyAuthenticationMethod(SshServer.User, keyFile);
				var connectionInfo = new ConnectionInfo(SshServer.Host, (int)SshServer.Port!, SshServer.User, auth);

				_sshClient = new SshClient(connectionInfo);
				_sshClient.KeepAliveInterval = TimeSpan.FromMinutes(5);
			}

			await _sshClient.ConnectAsync(cancellationToken ?? CancellationToken.None);
			return true;
		}

		public bool IsConnectionMatched(SshTunnel tunnel)
		{
			return SshServer.Equals(tunnel.SshServer) && PrivateKey.Equals(tunnel.PrivateKey);
		}

		public void StartForward(SshTunnel tunnel, Action<Exception>? exceptionCallback = null)
		{
			var forward = AddOrGetLocalForward(tunnel, exceptionCallback);
			forward?.Start();
		}

		public void StopForward(Guid tunnelId)
		{
			StopForwardByTunnelId(tunnelId);
		}

		public bool IsForwardStarted(Guid tunnelId)
		{
			return _forwards.TryGetValue(tunnelId, out var forward) && forward.IsStarted;
		}

		public void Dispose()
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

		private static async Task<PrivateKeyFile?> GetPrivateKeyFile(PrivateKey privateKey, Func<PrivateKey, Task<string?>>? promptPassphrase)
		{
			var passphrase = default(string);

			try
			{
				do
				{
					try
					{
						return passphrase is null
							? new PrivateKeyFile(privateKey.FilePath)
							: new PrivateKeyFile(privateKey.FilePath, passphrase);

					}
					catch (SshException ex) when (ex is SshPassPhraseNullOrEmptyException ||
												  // handling Renci.SshNet.Common.SshException: 'MAC verification failed for PuTTY key file'
												  ex.Message.Contains("putty", StringComparison.OrdinalIgnoreCase))
					{
						if (promptPassphrase is null)
							break;

						passphrase = await promptPassphrase(privateKey);
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

		private ForwardedPortLocal? AddOrGetLocalForward(SshTunnel tunnel, Action<Exception>? exceptionCallback = null)
		{
			if (_sshClient is null)
				return null;

			if (!_forwards.TryGetValue(tunnel.Id, out var forward))
			{
				forward = AddLocalForward(tunnel, exceptionCallback);
			}

			return forward;
		}

		private ForwardedPortLocal? AddLocalForward(SshTunnel tunnel, Action<Exception>? exceptionCallback = null)
		{
			if (_sshClient is null)
				return null;

			var forward = new ForwardedPortLocal(LOCALHOST, (uint)tunnel.LocalPort!, tunnel.RemoteServer!.Host, (uint)tunnel.RemoteServer.Port!);
			forward.Exception += (_, e) =>
			{
				StopForwardByTunnelId(tunnel.Id);
				exceptionCallback?.Invoke(e.Exception);
			};

			_sshClient.AddForwardedPort(forward);
			Debug.WriteLine($"Forwarding added to _sshClient, _sshClient.ForwardedPorts.Count = {_sshClient.ForwardedPorts.Count()}");
			_forwards[tunnel.Id] = forward;
			Debug.WriteLine($"Forwarding added to _forwards, _forwards.Count = {_forwards.Count}");
			return forward;
		}

		private void StopForwardByTunnelId(Guid tunnelId)
		{
			if (_forwards.TryRemove(tunnelId, out var forward))
			{
				forward.Stop();

				_sshClient?.RemoveForwardedPort(forward);

				Debug.WriteLine($"Forwarding removed from _sshClient and _forwards;" +
					$"\n_sshClient.ForwardedPorts.Count = {_sshClient?.ForwardedPorts.Count()}" +
					$"\n_forwards.Count = {_forwards.Count}");
			}
		}
	}
}
