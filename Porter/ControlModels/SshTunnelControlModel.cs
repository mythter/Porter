using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

		[ObservableProperty]
		private bool isTunnelStarted;

		[ObservableProperty]
		private bool isConnecting;

		public bool IsDisconnected => !IsTunnelStarted && !IsConnecting;

		public bool IsNameNullOrEmpty => string.IsNullOrEmpty(Name) && RemoteServerAlias is not null;

		public string? RemoteServerAlias => Model.RemoteServer?.Host switch
		{
			not null => Model.RemoteServer.Port switch
			{
				not null => $"{Model.RemoteServer.Host}:{Model.RemoteServer.Port}",
				_ => Model.RemoteServer.Host
			},
			_ => null
		};

		public SshTunnel Model { get; set; }

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

		public Func<SshTunnel, Action<Exception>?, Func<PrivateKey, Task<string?>>?, CancellationToken?, Task<bool>>? StartForward { get; set; }

		public Action<SshTunnel>? StopForward { get; set; }

		public SshTunnelControlModel(
			SshTunnel model,
			ObservableCollection<SshServer> sshServers,
			ObservableCollection<PrivateKey> privateKeys,
			ObservableCollection<RemoteServer> remoteServers)
		{
			Model = model;
			Name = model.Name;
			LocalPort = model.LocalPort;

			SshServers = sshServers;
			PrivateKeys = privateKeys;
			RemoteServers = remoteServers;

			SelectedSshServer = SshServers.FirstOrDefault(x => x?.Id == model.SshServer?.Id);
			SelectedPrivateKey = PrivateKeys.FirstOrDefault(x => x?.Id == model.PrivateKey?.Id);
			SelectedRemoteServer = RemoteServers.FirstOrDefault(x => x?.Id == model.RemoteServer?.Id);
		}

		partial void OnNameChanged(string? oldValue, string? newValue) 
		{
			OnPropertyChanged(nameof(IsNameNullOrEmpty));
			UpdateState();
		}

		partial void OnLocalPortChanged(int? oldValue, int? newValue) => UpdateState();
		partial void OnSelectedSshServerChanged(SshServer? oldValue, SshServer? newValue) => UpdateState();
		partial void OnSelectedPrivateKeyChanged(PrivateKey? oldValue, PrivateKey? newValue) => UpdateState();
		partial void OnSelectedRemoteServerChanged(RemoteServer? oldValue, RemoteServer? newValue) => UpdateState();
		partial void OnIsTunnelStartedChanged(bool oldValue, bool newValue) => UpdateTunnelState();
		partial void OnIsConnectingChanged(bool oldValue, bool newValue) => UpdateTunnelState();

		[RelayCommand]
		public async Task ToggleTunnel()
		{
			if (IsConnecting)
			{
				StopTunnel();
				return;
			}

			if (IsTunnelStarted)
			{
				StopTunnel();
			}
			else
			{
				await StartTunnel();
			}
		}

		public async Task<bool> StartTunnel(Func<PrivateKey, Task<string?>>? promptPassphrase = null, CancellationToken? cancellationToken = null)
		{
			if (!IsTunnelStarted && StartForward is not null)
			{
				IsConnecting = true;
				if (await StartForward(Model, OnTunnelException, promptPassphrase, cancellationToken))
				{
					IsTunnelStarted = true;
				}
				IsConnecting = false;
			}

			return IsTunnelStarted;
		}

		public void StopTunnel()
		{
			if ((IsConnecting || IsTunnelStarted) && StopForward is not null)
			{
				StopForward(Model);
				IsTunnelStarted = false;
			}
		}

		public void OnTunnelException(Exception ex)
		{
			IsTunnelStarted = false;
		}

		public void OnNameLostFocus(object? sender, RoutedEventArgs e)
		{
			Name = string.IsNullOrEmpty(Name) ? null : Name;
			ChangeSshTunnelName?.Invoke(Model.Id, Name);
		}

		public void OnNameKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			Name = string.IsNullOrEmpty(Name) ? null : Name;
			if (e.Key == Key.Enter)
			{
				ChangeSshTunnelName?.Invoke(Model.Id, Name);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnLocalPortLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshTunnelLocalPort?.Invoke(Model.Id, LocalPort);
		}

		public void OnLocalPortKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshTunnelLocalPort?.Invoke(Model.Id, LocalPort);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnSshServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			SshServerSelectionChanged?.Invoke(Model.Id, SelectedSshServer);
		}

		public void OnPrivateKeySelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			PrivateKeySelectionChanged?.Invoke(Model.Id, SelectedPrivateKey);
		}

		public void OnRemoteServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			RemoteServerSelectionChanged?.Invoke(Model.Id, SelectedRemoteServer);
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
			OnPropertyChanged(nameof(RemoteServerAlias));
			UpdateState();
		}

		private void UpdateState()
		{
			OnPropertyChanged(nameof(MiniToolTip));
		}

		private void UpdateTunnelState()
		{
			OnPropertyChanged(nameof(IsDisconnected));
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
