using System;
using System.Collections.Generic;
using System.Linq;
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
			return ComputeOverallStateNoLock() ?? ForwardState.None;
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
			state.IsIntendedToRun = true;
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
					state.LastError = started ? null : state.LastError;
				}
			}

			RaiseOverallStateIfChanged();
			return started;
		}
		catch (Exception ex)
		{
			lock (_stateSync)
			{
				// if user stops tunnel that was failed consider it failed anyway
				if (ex is OperationCanceledException && state.LastError is null)
				{
					state.State = TunnelState.Stopped;
				}
				else
				{
					state.State = TunnelState.Failed;
				}

				RaiseOverallStateIfChanged();

				// If user cancelled the start operation, clear the intent flag so this tunnel
				// doesn't affect the tray state calculation.
				if (ex is OperationCanceledException)
				{
					state.IsIntendedToRun = false;
				}
			}

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

		if (state.State == TunnelState.Connecting && !_forwardManager.IsForwardStarted(tunnel))
			return;

		_forwardManager.StopForward(tunnel);

		lock (_stateSync)
		{
			state.State = TunnelState.Stopped;
			state.IsIntendedToRun = false;
		}

		RaiseOverallStateIfChanged();

		ResetIntendedStateIfNoRunning();
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

		lock (_stateSync)
		{
			if (!_states.TryGetValue(tunnel.Id, out var state))
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

		ResetIntendedStateIfNoRunning();
	}

	private ForwardState? ComputeOverallStateNoLock()
	{
		// do not change state while some tunnels are still connecting, wait for them to finish first
		if (_states.Values.Any(s => s.State == TunnelState.Connecting))
			return null;

		// Only consider tunnels the user intends to be running.
		// Tracks user intent vs actual state to distinguish:
		// - User stopped all tunnels → None
		// - User wants them running but all failed → AllDown
		// - User wants them running and all are up → AllUp
		// - User wants them running but some failed → PartiallyDown
		var intendedCount = 0;
		var runningCount = 0;
		var failedCount = 0;
		var stoppedCount = 0;

		foreach (var s in _states.Values)
		{
			if (!s.IsIntendedToRun)
				continue;

			intendedCount++;

			if (s.State == TunnelState.Running)
				runningCount++;
			else if (s.State == TunnelState.Stopped)
				stoppedCount++;
			else if (s.State == TunnelState.Failed)
				failedCount++;
		}

		// No tunnels intended to run → None
		if (intendedCount == 0)
			return ForwardState.None;

		// All intended tunnels are stopped → don't change the state
		if (intendedCount == stoppedCount)
			return null;

		// All intended tunnels are running → AllUp
		if (runningCount == intendedCount)
			return ForwardState.AllUp;

		// All intended tunnels failed → AllDown
		if (failedCount == intendedCount)
			return ForwardState.AllDown;

		// Some intended tunnels running, some failed/connecting → PartiallyDown
		return ForwardState.PartiallyDown;
	}

	private void ResetIntendedStateIfNoRunning()
	{
		// If no tunnels are running anymore, reset all IsIntendedToRun flags.
		// This resets the intent tracking so the next time user starts tunnels,
		// the tray state reflects only newly started ones.
		lock (_stateSync)
		{
			if (!_states.Values.Any(s => s.State is not TunnelState.Running and not TunnelState.Connecting))
			{
				foreach (var s in _states.Values)
				{
					s.IsIntendedToRun = false;
				}
			}
		}
	}

	private void RaiseOverallStateIfChanged()
	{
		ForwardState? current;

		lock (_stateSync)
		{
			current = ComputeOverallStateNoLock();

			if (current is null || current == _lastOverallState)
				return;

			_lastOverallState = current.Value;
		}

		PostToUI(() => OverallStateChanged?.Invoke(current.Value));
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
