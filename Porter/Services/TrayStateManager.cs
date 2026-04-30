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
	private readonly ITrayService _trayService;
	private readonly ITunnelService _tunnelService;
	private bool _disposed;

	public TrayStateManager(ITrayService trayService, ITunnelService tunnelService)
	{
		_trayService = trayService;
		_tunnelService = tunnelService;
		_tunnelService.OverallStateChanged += OnOverallStateChanged;
	}

	public void Refresh()
	{
		_trayService.SetTrayIcon(_tunnelService.GetOverallState());
	}

	public void Dispose()
	{
		if (_disposed)
			return;
		_disposed = true;
		_tunnelService.OverallStateChanged -= OnOverallStateChanged;
	}

	private void OnOverallStateChanged(ForwardState state)
	{
		_trayService.SetTrayIcon(state);
	}
}

public interface ITrayStateManager
{
	/// <summary>
	/// Forces a recomputation of the tray indicator from the current tunnel states. Useful at app
	/// start, after settings reload, etc.
	/// </summary>
	void Refresh();
}
