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

	public bool IsRunning => State == TunnelState.Running;

	public bool IsConnecting => State == TunnelState.Connecting;

	public bool IsStopped => State == TunnelState.Stopped;
}
