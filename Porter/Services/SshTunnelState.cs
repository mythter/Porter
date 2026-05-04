using System;

using CommunityToolkit.Mvvm.ComponentModel;

using Porter.Enums;

namespace Porter.Services;

public partial class SshTunnelState : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsRunning), nameof(IsConnecting), nameof(IsStopped))]
	public partial TunnelState State { get; set; }

	[ObservableProperty]
	public partial Exception? LastError { get; set; }

	/// <summary>
	/// Tracks whether the user intends this tunnel to be running.
	/// Set to <c>true</c> when user starts the tunnel, <c>false</c> when user stops it.
	/// Remains <c>true</c> even if tunnel fails — distinguishes user-initiated stop vs connection loss.
	/// </summary>
	public bool IsIntendedToRun { get; set; }

	public bool IsRunning => State == TunnelState.Running;

	public bool IsConnecting => State == TunnelState.Connecting;

	public bool IsStopped => State == TunnelState.Stopped;
}
