using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using Porter.Enums;

using Porter.Models;
using Porter.Services.Interfaces;
using Porter.Services.Ssh;

namespace Porter.Services;

public class TunnelService : ITunnelService
{
	#region Private Fields

	private readonly PortForwardManager _forwardManager;

	private readonly Dictionary<Guid, SshTunnelState> _states = [];

	private readonly Dictionary<Guid, CancellationTokenSource> _cts = [];

	private readonly Lock _stateSync = new();

	private ForwardState _lastOverallState = ForwardState.None;

	#endregion

	#region Events

	public event Action<SshTunnel, Exception>? TunnelFailed;

	public event Action<ForwardState>? OverallStateChanged;

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
		lock (_stateSync)
		{
			return GetStateNoLock(tunnelId);
		}
	}

	public ForwardState GetOverallState()
	{
		lock (_stateSync)
		{
			return ComputeOverallStateNoLock();
		}
	}

	public async Task<bool> StartAsync(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		var state = GetState(tunnel.Id);

		CancellationTokenSource cts;
		lock (_stateSync)
		{
			if (state.State is TunnelState.Running or TunnelState.Connecting)
				return true;

			cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_cts[tunnel.Id] = cts;
			state.State = TunnelState.Connecting;
		}

		try
		{
			var started = await StartInternal(tunnel, promptPassphrase, cts.Token);

			lock (_stateSync)
			{
				// Don't overwrite a Failed state set by an SSH callback that arrived during start.
				if (state.State == TunnelState.Connecting)
				{
					state.State = started ? TunnelState.Running : TunnelState.Failed;
				}
			}

			RaiseOverallStateIfChanged();
			return started;
		}
		catch
		{
			lock (_stateSync)
			{
				state.State = TunnelState.Failed;
			}
			RaiseOverallStateIfChanged();
			throw;
		}
		finally
		{
			lock (_stateSync)
			{
				_cts.Remove(tunnel.Id);
			}
			cts.Dispose();
		}
	}

	public void Stop(SshTunnel tunnel)
	{
		CancellationTokenSource? cts;
		SshTunnelState state;
		lock (_stateSync)
		{
			_cts.TryGetValue(tunnel.Id, out cts);
			state = GetStateNoLock(tunnel.Id);
		}

		cts?.Cancel();

		_forwardManager.StopForward(tunnel);

		lock (_stateSync)
		{
			state.State = TunnelState.Stopped;
		}

		RaiseOverallStateIfChanged();
	}

	public bool IsAnyForwardStarted()
	{
		return _forwardManager.IsAnyForwardStarted();
	}

	#endregion

	#region Private Methods

	private SshTunnelState GetStateNoLock(Guid tunnelId)
	{
		if (!_states.TryGetValue(tunnelId, out var state))
		{
			state = new SshTunnelState { State = TunnelState.Stopped };
			_states[tunnelId] = state;
		}
		return state;
	}

	private Task<bool> StartInternal(
		SshTunnel tunnel,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		return _forwardManager.StartForward(tunnel, promptPassphrase, cancellationToken);
	}

	private void OnTunnelFailed(SshTunnel tunnel, Exception ex)
	{
		CancellationTokenSource? cts;
		SshTunnelState? state;
		lock (_stateSync)
		{
			if (!_states.TryGetValue(tunnel.Id, out state))
				return;

			_cts.TryGetValue(tunnel.Id, out cts);

			state.LastError = ex;
			state.State = TunnelState.Failed;
		}

		cts?.Cancel();

		// SSH error callbacks fire on background threads. Marshal the public events to the UI
		// thread so subscribers (mostly view models) can update bound state safely.
		PostToUI(() => TunnelFailed?.Invoke(tunnel, ex));
		RaiseOverallStateIfChanged();
	}

	private ForwardState ComputeOverallStateNoLock()
	{
		var total = 0;
		var running = 0;
		var failed = 0;

		foreach (var s in _states.Values)
		{
			total++;
			switch (s.State)
			{
				case TunnelState.Running: running++; break;
				case TunnelState.Failed: failed++; break;
			}
		}

		if (total == 0 || (running == 0 && failed == 0))
			return ForwardState.None;

		if (running == 0)
			return ForwardState.AllDown;

		if (failed == 0 && running == total)
			return ForwardState.AllUp;

		return ForwardState.PartiallyDown;
	}

	private void RaiseOverallStateIfChanged()
	{
		ForwardState current;
		lock (_stateSync)
		{
			current = ComputeOverallStateNoLock();
			if (current == _lastOverallState)
				return;
			_lastOverallState = current;
		}

		PostToUI(() => OverallStateChanged?.Invoke(current));
	}

	private static void PostToUI(Action action)
	{
		if (Dispatcher.UIThread.CheckAccess())
			action();
		else
			Dispatcher.UIThread.Post(action);
	}

	#endregion
}
