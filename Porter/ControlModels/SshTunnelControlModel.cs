using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Helpers;
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

		partial void OnNameChanged(string? oldValue, string? newValue) => UpdateState();
		partial void OnLocalPortChanged(int? oldValue, int? newValue) => UpdateState();
		partial void OnSelectedSshServerChanged(SshServer? oldValue, SshServer? newValue) => UpdateState();
		partial void OnSelectedPrivateKeyChanged(PrivateKey? oldValue, PrivateKey? newValue) => UpdateState();
		partial void OnSelectedRemoteServerChanged(RemoteServer? oldValue, RemoteServer? newValue) => UpdateState();

		public string MiniToolTip => GetMiniToolTip();

		public ObservableCollection<SshServer> SshServers { get; init; }

		public ObservableCollection<PrivateKey> PrivateKeys { get; init; }

		public ObservableCollection<RemoteServer> RemoteServers { get; init; }

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

			SshServers = sshServers;
			PrivateKeys = privateKeys;
			RemoteServers = remoteServers;

			SelectedSshServer = SshServers.FirstOrDefault(x => x?.Id == model.SshServer?.Id);
			SelectedPrivateKey = PrivateKeys.FirstOrDefault(x => x?.Id == model.PrivateKey?.Id);
			SelectedRemoteServer = RemoteServers.FirstOrDefault(x => x?.Id == model.RemoteServer?.Id);
		}

		[RelayCommand]
		public void StartTunnel()
		{
			int some = 1;
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

		public void OnSshServerChanged(object? sender, SshServer sshServer)
		{
			UpdateState();
		}

		public void OnPrivateKeyChanged(object? sender, PrivateKey privateKey)
		{
			UpdateState();
		}

		public void OnRemoteServerChanged(object? sender, RemoteServer remoteServer)
		{
			UpdateState();
		}

		private void UpdateState()
		{
			OnPropertyChanged(nameof(MiniToolTip));
		}

		private string GetMiniToolTip()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Tunnel Name: {Name ?? "-"}");
			sb.AppendLine($"Local Port: {LocalPort?.ToString() ?? "-"}");

			sb.Append("SSH server: ");
			if (StringHelper.IsAllNullOrEmpty(
				SelectedSshServer?.Name, 
				SelectedSshServer?.User, 
				SelectedSshServer?.Host))
			{
				sb.AppendLine("-");
			}
			else
			{
				if (SelectedSshServer?.Name is not null)
				{
					sb.Append($"{SelectedSshServer?.Name}");
				}

				if (SelectedSshServer?.Host is not null)
				{
					if (SelectedSshServer?.Name is not null)
						sb.Append(" - ");

					sb.Append(SelectedSshServer?.User is null
						? SelectedSshServer?.Host
						: $"{SelectedSshServer?.User}@{SelectedSshServer?.Host}");

					if (SelectedSshServer?.Port is not null)
					{
						sb.Append($":{SelectedSshServer?.Port}");
					}
				}

				sb.AppendLine();
			}

			sb.Append("Private key: ");
			if (StringHelper.IsAllNullOrEmpty(
				SelectedPrivateKey?.Name, 
				SelectedPrivateKey?.FilePath))
			{
				sb.AppendLine("-");
			}
			else
			{
				if (SelectedPrivateKey?.Name is not null)
				{
					sb.Append($"{SelectedPrivateKey?.Name}");
				}

				if (SelectedPrivateKey?.FilePath is not null)
				{
					if (SelectedPrivateKey?.Name is not null)
						sb.Append(" - ");

					sb.Append(SelectedPrivateKey?.FilePath);
				}

				sb.AppendLine();
			}

			sb.Append("Remote server: ");
			if (StringHelper.IsAllNullOrEmpty(
				SelectedRemoteServer?.Name,
				SelectedRemoteServer?.Host))
			{
				sb.Append('-');
			}
			else
			{
				if (SelectedRemoteServer?.Name is not null)
				{
					sb.Append($"{SelectedRemoteServer?.Name}");
				}

				if (SelectedRemoteServer?.Host is not null)
				{
					if (SelectedRemoteServer?.Name is not null)
						sb.Append(" - ");

					sb.Append(SelectedRemoteServer?.Port is null
						? SelectedRemoteServer?.Host
						: $"{SelectedRemoteServer?.Host}:{SelectedRemoteServer?.Port}");
				}
			}

			return sb.ToString();
		}
	}
}
