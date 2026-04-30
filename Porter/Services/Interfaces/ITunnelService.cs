using System;
using System.Threading;
using System.Threading.Tasks;

using Porter.Enums;
using Porter.Models;

namespace Porter.Services.Interfaces;

public interface ITunnelService
{
	event Action<SshTunnel, Exception>? TunnelFailed;

	/// <summary>
	/// Raised on the UI thread whenever the aggregate <see cref="ForwardState"/> changes
	/// (e.g. all tunnels up, all down, partially down). Subscribers can use this to drive
	/// the tray indicator without recomputing state themselves.
	/// </summary>
	event Action<ForwardState>? OverallStateChanged;

	SshTunnelState GetState(Guid tunnelId);

	Task<bool> StartAsync(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default);

	void Stop(SshTunnel tunnel);

	bool IsAnyForwardStarted();

	/// <summary>
	/// Returns the aggregate state across all tunnels currently tracked by the service.
	/// </summary>
	ForwardState GetOverallState();
}
