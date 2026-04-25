using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class SshServer : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	private string? name;

	[ObservableProperty]
	private string? user;

	[ObservableProperty]
	private string? host;

	[ObservableProperty]
	private int? port;
}
