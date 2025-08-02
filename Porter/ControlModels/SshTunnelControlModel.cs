using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;

namespace Porter.ControlModels
{
	public partial class SshTunnelControlModel : ObservableObject
	{
		[ObservableProperty]
		private string? name;

		[ObservableProperty]
		private int? localPort;

		[ObservableProperty]
		private SshServer? selectedSshServer;

		[ObservableProperty]
		private PrivateKey? selectedPrivateKey;

		[ObservableProperty]
		private RemoteServer? selectedRemoteServer;

		public ObservableCollection<SshServer?> SshServers { get; init; }

		public ObservableCollection<PrivateKey?> PrivateKeys { get; init; }

		public ObservableCollection<RemoteServer?> RemoteServers { get; init; }

		public IRelayCommand<Guid>? DeleteSshTunnel { get; set; }

		public Action<Guid, string?>? ChangeSshTunnelName { get; set; }

		public Action<Guid, int?>? ChangeSshTunnelLocalPort { get; set; }

		public Action<Guid, SshServer?>? SshServerSelectionChanged { get; set; }

		public Action<Guid, PrivateKey?>? PrivateKeySelectionChanged { get; set; }

		public Action<Guid, RemoteServer?>? RemoteServerSelectionChanged { get; set; }

		public Guid Id { get; set; }

		public SshTunnelControlModel(
			SshTunnel model, 
			ObservableCollection<SshServer> sshServers, 
			ObservableCollection<PrivateKey> privateKeys,
			ObservableCollection<RemoteServer> remoteServers)
		{
			Id = model.Id;
			Name = model.Name;
			LocalPort = model.LocalPort;
			
			SshServers = new ObservableCollection<SshServer?>(new SshServer?[] { null }.Concat(sshServers));
			PrivateKeys = new ObservableCollection<PrivateKey?>(new PrivateKey?[] { null }.Concat(privateKeys));
			RemoteServers = new ObservableCollection<RemoteServer?>(new RemoteServer?[] { null }.Concat(remoteServers));

			SelectedSshServer = SshServers.FirstOrDefault(x => x?.Id == model.SshServer?.Id);
			SelectedPrivateKey = PrivateKeys.FirstOrDefault(x => x?.Id == model.PrivateKey?.Id);
			SelectedRemoteServer = RemoteServers.FirstOrDefault(x => x?.Id == model.RemoteServer?.Id);
		}

		public void OnNameLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshTunnelName?.Invoke(Id, Name);
		}

		public void OnNameKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshTunnelName?.Invoke(Id, Name);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnLocalPortLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshTunnelLocalPort?.Invoke(Id, LocalPort);
		}

		public void OnLocalPortKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshTunnelLocalPort?.Invoke(Id, LocalPort);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnSshServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			SshServerSelectionChanged?.Invoke(Id, SelectedSshServer);
		}

		public void OnPrivateKeySelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			PrivateKeySelectionChanged?.Invoke(Id, SelectedPrivateKey);
		}

		public void OnRemoteServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			RemoteServerSelectionChanged?.Invoke(Id, SelectedRemoteServer);
		}
	}
}
