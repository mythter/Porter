using System;
using System.Threading;
using System.Threading.Tasks;

using Porter.Enums;
using Porter.Models;

namespace Porter.Services.Interfaces;

public interface ITunnelService
{
	/// <summary>
	/// Raised on the UI thread whenever a tunnel fails to start or stops unexpectedly.
	/// Subscribers can use this to display error messages or notifications.
	/// </summary>
	event Action<Guid, Exception>? TunnelFailed;

	/// <summary>
	/// Raised on the UI thread whenever the aggregate <see cref="ForwardState"/> changes
	/// (e.g. all tunnels up, all down, partially down). Subscribers can use this to drive
	/// the tray indicator without recomputing state themselves.
	/// </summary>
	event Action<ForwardState>? OverallStateChanged;

	/// <summary>
	/// Returns the current state of the specified tunnel.
	/// </summary>
	/// <param name="tunnelId">The ID of the tunnel.</param>
	/// <returns>The current state of the tunnel.</returns>
	SshTunnelState GetState(Guid tunnelId);

	/// <summary>
	/// Starts the specified tunnel asynchronously. If the tunnel is already running, this method does nothing.
	/// </summary>
	/// <param name="tunnel">The tunnel to start.</param>
	/// <param name="sshServer">The SSH server to connect to.</param>
	/// <param name="remoteServer">The remote server to forward traffic to.</param>
	/// <param name="privateKey">The private key for SSH authentication.</param>
	/// <param name="promptPassphrase">A function to prompt for the private key passphrase, if needed.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>True if the tunnel was started successfully, otherwise false.</returns>
	Task<bool> StartAsync(
		SshTunnel tunnel,
		SshServer sshServer,
		RemoteServer remoteServer,
		PrivateKey? privateKey,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops the specified tunnel if it is currently running. If the tunnel is not running, this method does nothing.
	/// </summary>
	/// <param name="tunnelId">The ID of the tunnel to stop.</param>
	void Stop(Guid tunnelId);

	/// <summary>
	/// Returns true if any of the tunnels currently tracked by the service are in a connecting or running state.
	/// </summary>
	/// <returns>True if any tunnel is connecting or running, otherwise false.</returns>
	bool IsAnyForwardStarted();

	/// <summary>
	/// Returns the aggregate state across all tunnels currently tracked by the service.
	/// </summary>
	ForwardState GetOverallState();
}
