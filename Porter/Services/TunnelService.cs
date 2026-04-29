using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Porter.Enums;

using Porter.Models;
using Porter.Services.Ssh;

namespace Porter.Services;

public class TunnelService : ITunnelService
{
	#region Private Fields

	private readonly PortForwardManager _forwardManager;

	private readonly Dictionary<Guid, SshTunnelState> _states = [];

	private readonly Dictionary<Guid, CancellationTokenSource> _cts = [];

	#endregion

	#region Events

	public event Action<SshTunnel, Exception>? TunnelFailed;

	#endregion

	#region Constructors

	public TunnelService(PortForwardManager forwardManager)
	{
		_forwardManager = forwardManager;

		_forwardManager.TunnelFailed += OnTunnelFailed;
	}

	#endregion

	#region Public Methods

	public SshTunnelState GetState(Guid tunnelId)
	{
		if (!_states.TryGetValue(tunnelId, out var state))
		{
			state = new SshTunnelState { State = TunnelState.Stopped };
			_states[tunnelId] = state;
		}

		return state;
	}

	public async Task<bool> StartAsync(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		var state = GetState(tunnel.Id);

		if (state.State is TunnelState.Running or TunnelState.Connecting)
			return true;

		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_cts[tunnel.Id] = cts;

		state.State = TunnelState.Connecting;

		try
		{
			var started = await StartInternal(tunnel, promptPassphrase, cts.Token);

			state.State = started
				? TunnelState.Running
				: TunnelState.Failed;

			return started;
		}
		catch
		{
			state.State = TunnelState.Failed;
			throw;
		}
		finally
		{
			cts.Dispose();
			_cts.Remove(tunnel.Id);
		}
	}

	public void Stop(SshTunnel tunnel)
	{
		if (_cts.TryGetValue(tunnel.Id, out var cts))
		{
			cts.Cancel();
		}

		_forwardManager.StopForward(tunnel);

		GetState(tunnel.Id).State = TunnelState.Stopped;
	}

	public bool IsAnyForwardStarted()
	{
		return _forwardManager.IsAnyForwardStarted();
	}

	#endregion

	#region Private Methods

	private Task<bool> StartInternal(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		return _forwardManager.StartForward(tunnel, promptPassphrase, cancellationToken);
	}

	private void OnTunnelFailed(SshTunnel tunnel, Exception ex)
	{
		if (!_states.TryGetValue(tunnel.Id, out var state))
			return;

		if (_cts.TryGetValue(tunnel.Id, out var cts))
		{
			cts.Cancel();
		}

		state.LastError = ex;
		state.State = TunnelState.Failed;

		TunnelFailed?.Invoke(tunnel, ex);
	}

	#endregion
}
