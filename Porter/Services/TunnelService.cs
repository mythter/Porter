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

	private readonly HashSet<Guid> _restartingTunnels = [];

	private readonly Lock _stateSync = new();

	private ForwardState _lastOverallState = ForwardState.None;

	#endregion

	#region Events

	/// <inheritdoc />
	public event Action<Guid, Exception>? TunnelFailed;

	/// <inheritdoc />
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

	/// <inheritdoc />
	public SshTunnelState GetState(Guid tunnelId)
	{
		lock (_stateSync)
		{
			return GetStateNoLock(tunnelId);
		}
	}

	/// <inheritdoc />
	public ForwardState GetOverallState()
	{
		lock (_stateSync)
		{
			return ComputeOverallStateNoLock() ?? ForwardState.None;
		}
	}

	/// <inheritdoc />
	public async Task<bool> StartAsync(
		SshTunnel tunnel,
		SshServer sshServer,
		RemoteServer remoteServer,
		PrivateKey? privateKey,
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

		bool started = false;

		try
		{
			started = await StartInternal(tunnel, sshServer, remoteServer, privateKey, promptPassphrase, cts.Token);

			lock (_stateSync)
			{
				// Don't overwrite a Failed state set by an SSH callback that arrived during start.
				if (state.State == TunnelState.Connecting)
				{
					state.State = started ? TunnelState.Running : TunnelState.Failed;
					state.LastError = started ? null : state.LastError;
				}
			}

			if (started)
			{
				ResetFailedTunnels();
			}

			RaiseOverallStateIfChanged();

			return started;
		}
		catch (Exception ex)
		{
			lock (_stateSync)
			{
				if (ex is OperationCanceledException)
				{
					state.State = TunnelState.Stopped;
				}
				else
				{
					state.State = TunnelState.Failed;
				}

				RaiseOverallStateIfChanged();
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

	/// <inheritdoc />
	public void Stop(Guid tunnelId)
	{
		CancellationTokenSource? cts;
		SshTunnelState state;

		lock (_stateSync)
		{
			_cts.TryGetValue(tunnelId, out cts);
			state = GetStateNoLock(tunnelId);
		}

		cts?.Cancel();

		if (state.State == TunnelState.Connecting && !_forwardManager.IsForwardStarted(tunnelId))
			return;

		_forwardManager.StopForward(tunnelId);

		lock (_stateSync)
		{
			state.State = TunnelState.Stopped;
		}

		RaiseOverallStateIfChanged();
	}

	/// <inheritdoc />
	public bool TryBeginRestart(Guid tunnelId)
	{
		lock (_stateSync)
		{
			return _restartingTunnels.Add(tunnelId);
		}
	}

	/// <inheritdoc />
	public void EndRestart(Guid tunnelId)
	{
		lock (_stateSync)
		{
			_restartingTunnels.Remove(tunnelId);
		}
	}

	/// <inheritdoc />
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
		SshServer sshServer,
		RemoteServer remoteServer,
		PrivateKey? privateKey,
		Func<Task<string?>>? promptPassphrase = null,
		CancellationToken cancellationToken = default)
	{
		var forwardOptions = new LocalPortForwardOptions
		{
			TunnelId = tunnel.Id,
			LocalPort = tunnel.LocalPort,
			SshServerUser = sshServer.User,
			SshServerHost = sshServer.Host,
			SshServerPort = sshServer.Port,
			RemoteServerHost = remoteServer.Host,
			RemoteServerPort = remoteServer.Port,
			PrivateKeyFilePath = privateKey?.FilePath,
		};

		return _forwardManager.StartForward(forwardOptions, promptPassphrase, cancellationToken);
	}

	private void OnTunnelFailed(Guid tunnelId, Exception ex)
	{
		CancellationTokenSource? cts;

		lock (_stateSync)
		{
			if (!_states.TryGetValue(tunnelId, out var state))
				return;

			_cts.TryGetValue(tunnelId, out cts);

			state.LastError = ex;
			state.State = TunnelState.Failed;
		}

		cts?.Cancel();

		// SSH error callbacks fire on background threads. Marshal the public events to the UI
		// thread so subscribers (mostly view models) can update bound state safely.
		PostToUI(() => TunnelFailed?.Invoke(tunnelId, ex));

		RaiseOverallStateIfChanged();
	}

	private ForwardState? ComputeOverallStateNoLock()
	{
		// do not change state while some tunnels are still connecting, wait for them to finish first
		if (_states.Values.Any(s => s.State == TunnelState.Connecting))
			return null;

		var runningCount = 0;
		var failedCount = 0;

		foreach (var state in _states.Values.Select(v => v.State))
		{
			if (state == TunnelState.Running)
				runningCount++;
			else if (state == TunnelState.Failed)
				failedCount++;
		}

		// All intended tunnels are running → AllUp
		if (runningCount > 0 && failedCount == 0)
			return ForwardState.AllUp;

		// All intended tunnels failed → AllDown
		if (runningCount == 0 && failedCount > 0)
			return ForwardState.AllDown;

		// Some tunnels running and some failed → PartiallyDown
		if (runningCount > 0 && failedCount > 0)
			return ForwardState.PartiallyDown;

		return ForwardState.None;
	}

	private void ResetFailedTunnels()
	{
		lock (_stateSync)
		{
			foreach (var state in _states.Values.Where(v => v.State == TunnelState.Failed))
			{
				state.State = TunnelState.Stopped;
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
		{
			action();
		}
		else
		{
			Dispatcher.UIThread.Post(action);
		}
	}

	#endregion
}
