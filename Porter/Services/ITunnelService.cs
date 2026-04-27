using System;
using System.Threading;
using System.Threading.Tasks;

using Porter.Models;

namespace Porter.Services;

public interface ITunnelService
{
	event Action<SshTunnel, Exception>? TunnelFailed;

	SshTunnelState GetState(Guid tunnelId);

	Task<bool> StartAsync(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default);

	void Stop(SshTunnel tunnel);

	bool IsAnyForwardStarted();
}
