using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class RemoteServer : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial string? Host { get; set; }

	[ObservableProperty]
	public partial int? Port { get; set; }
}
