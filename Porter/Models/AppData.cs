using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public class AppData : ObservableObject
{
	public AppSettings Settings { get; set; } = new();

	public WindowSettings WindowSettings { get; set; } = new();

	public ObservableCollection<SshServer> SshServers { get; set; } = [];

	public ObservableCollection<RemoteServer> RemoteServers { get; set; } = [];

	public ObservableCollection<PrivateKey> PrivateKeys { get; set; } = [];

	public ObservableCollection<SshTunnel> SshTunnels { get; set; } = [];
}
