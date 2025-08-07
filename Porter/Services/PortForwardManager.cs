using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Porter.Models;

namespace Porter.Services
{
	public class PortForwardManager : IDisposable
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);

		private readonly List<SshConnection> _connections = [];

		public async Task<bool> StartForward(
			SshTunnel tunnel,
			Action<Exception>?
			exceptionCallback = null,
			Func<PrivateKey, Task<string?>>? promptPassphrase = null,
			CancellationToken? cancellationToken = null)
		{
			try
			{
				// initiate async immediately
				return await Task.Run(async () => await StartForwardInternal(tunnel, exceptionCallback, promptPassphrase, cancellationToken));
			}
			catch
			{
				return false;
			}
		}

		public void StopForward(SshTunnel tunnel)
		{
			if (GetConnectionByTunnelId(tunnel.Id) is { } connection && connection.IsForwardStarted(tunnel.Id))
			{
				connection.StopForward(tunnel.Id);

				//if(connection.Tunnels.Count == 0)
				//{
				//	_connections.Remove(connection);
				//	connection.Dispose();
				//}
			}
		}

		public bool IsForwardStarted(SshTunnel tunnel)
		{
			return GetConnectionByTunnelId(tunnel.Id) is { } connection && connection.IsForwardStarted(tunnel.Id);
		}

		public void Dispose()
		{
			foreach (var connection in _connections)
			{
				connection.Dispose();
			}

			_connections.Clear();
		}

		private async Task<bool> StartForwardInternal(
			SshTunnel tunnel,
			Action<Exception>? exceptionCallback = null,
			Func<PrivateKey, Task<string?>>? promptPassphrase = null,
			CancellationToken? cancellationToken = null)
		{
			await _semaphore.WaitAsync(cancellationToken ?? CancellationToken.None);

			try
			{
				if (GetMatchedConnectionForTunnel(tunnel) is not { } connection)
				{
					if (tunnel.SshServer is not null && tunnel.PrivateKey is not null)
					{
						connection = new SshConnection(tunnel.SshServer!, tunnel.PrivateKey!);
						_connections.Add(connection);
					}
					else
					{
						return false;
					}
				}

				if (!connection.IsConnected &&
					!await connection.ConnectAsync(promptPassphrase, cancellationToken))
				{
					return false;
				}

				if (!connection.IsForwardStarted(tunnel.Id))
				{
					connection.StartForward(tunnel, exceptionCallback);
				}
			}
			finally
			{
				_semaphore.Release();
			}

			return true;
		}

		private SshConnection? GetMatchedConnectionForTunnel(SshTunnel tunnel)
		{
			return _connections.FirstOrDefault(c => c.IsConnectionMatched(tunnel));
		}

		private SshConnection? GetConnectionByTunnelId(Guid tunnelId)
		{
			return _connections.FirstOrDefault(c => c.Tunnels.Contains(tunnelId));
		}
	}
}
