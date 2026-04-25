using System;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class SshTunnel : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	private string? name;

	[ObservableProperty]
	private int? localPort;

	[ObservableProperty]
	private SshServer? sshServer;

	[ObservableProperty]
	private PrivateKey? privateKey;

	[ObservableProperty]
	private RemoteServer? remoteServer;

	[ObservableProperty]
	[property: JsonIgnore]
	private bool isTunnelStarted;

	[ObservableProperty]
	[property: JsonIgnore]
	private bool isConnecting;
}
