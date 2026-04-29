using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class SshTunnel : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial int? LocalPort { get; set; }

	[ObservableProperty]
	public partial SshServer? SshServer { get; set; }

	[ObservableProperty]
	public partial PrivateKey? PrivateKey { get; set; }

	[ObservableProperty]
	public partial RemoteServer? RemoteServer { get; set; }
}
