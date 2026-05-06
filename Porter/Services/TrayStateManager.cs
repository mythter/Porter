using System;

using Porter.Enums;
using Porter.Services.Interfaces;

namespace Porter.Services;

/// <summary>
/// Centralizes the tray icon state. Subscribes to <see cref="ITunnelService.OverallStateChanged"/>
/// so view models no longer need to compute the aggregate state or call <see cref="ITrayService"/>
/// directly.
/// </summary>
public sealed class TrayStateManager : ITrayStateManager, IDisposable
{
	#region Private Fields

	private bool _disposed;

	private readonly ITrayService _trayService;

	private readonly ITunnelService _tunnelService;

	#endregion

	#region Constructors

	public TrayStateManager(ITrayService trayService, ITunnelService tunnelService)
	{
		_trayService = trayService;
		_tunnelService = tunnelService;
		_tunnelService.OverallStateChanged += OnOverallStateChanged;
	}

	#endregion

	#region Public Methods

	public void Refresh()
	{
		_trayService.SetTrayIcon(_tunnelService.GetOverallState());
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_tunnelService.OverallStateChanged -= OnOverallStateChanged;

		_disposed = true;
	}

	#endregion

	#region Private Methods

	private void OnOverallStateChanged(ForwardState state)
	{
		_trayService.SetTrayIcon(state);
	}

	#endregion
}
