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
	public partial Guid? SshServerId { get; set; }

	[ObservableProperty]
	public partial Guid? PrivateKeyId { get; set; }

	[ObservableProperty]
	public partial Guid? RemoteServerId { get; set; }
}
