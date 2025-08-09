using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.Input;

using Porter.ControlModels;
using Porter.Enums;
using Porter.Models;
using Porter.Services;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class TunnelsViewModel : ViewModelBase
	{
		public IRelayCommand ExitCommand { get; }
		public IRelayCommand OpenMainWindow { get; }

		public MainViewModel MainViewModel { get; }

		public List<MenuItemViewModel> SettingsMenuItems { get; }

		public ObservableCollection<SshTunnelControlModel> Items { get; }

		public PortForwardManager PortForwardManager { get; }

		public TunnelsViewModel()
		{

		}

		public TunnelsViewModel(MainViewModel mainViewModel, Action exitAction, Action openMainWindow)
		{
			MainViewModel = mainViewModel;

			mainViewModel.SshServersViewModel.SshServerChanged += OnSshServerChanged;
			mainViewModel.PrivateKeysViewModel.PrivateKeyChanged += OnPrivateKeyChanged;
			mainViewModel.RemoteServersViewModel.RemoteServerChanged += OnRemoteServerChanged;

			Items = new ObservableCollection<SshTunnelControlModel>(
				StorageManager.SshTunnels.Select(CreateSshTunnelControlModel));

			ExitCommand = new RelayCommand(exitAction);
			OpenMainWindow = new RelayCommand(openMainWindow);

			PortForwardManager = new();

			var settings = StorageManager.Settings;

			SettingsMenuItems =
			[
				new MenuItemViewModel
				{
					Header = "On close minimize to tray",
					Command = new RelayCommand(() => {
						settings.OnCloseMinimizeToTray = !settings.OnCloseMinimizeToTray;
						StorageManager.SaveSettings();
					}),
					ToggleType = MenuItemToggleType.CheckBox,
					IsChecked = settings.OnCloseMinimizeToTray,
				}
			];
		}

		public async Task<bool> OnStartForward(
			SshTunnel tunnel,
			Action<Exception>? exceptionCallback = null,
			Func<PrivateKey, Task<string?>>? promptPassphrase = null,
			CancellationToken? cancellationToken = null)
		{
			var started = await PortForwardManager.StartForward(
				tunnel,
				(e) =>
				{
					exceptionCallback?.Invoke(e);
					OnTunnelException(e);
				},
				promptPassphrase ?? (privateKey => Dispatcher.UIThread.InvokeAsync(() => MainViewModel.DialogService.ShowPrivateKeyPasswordDialogAsync(privateKey))),
				cancellationToken);

			if (started)
			{
				MainViewModel.TrayService.SetTrayIcon(ForwardState.AllUp);
			}
			else if (PortForwardManager.IsAnyForwardStarted())
			{
				MainViewModel.TrayService.SetTrayIcon(ForwardState.PartiallyDown);
			}
			else
			{
				MainViewModel.TrayService.SetTrayIcon(ForwardState.None);
			}

			return started;
		}

		public void OnStopForward(SshTunnel tunnel)
		{
			PortForwardManager.StopForward(tunnel);

			if (!PortForwardManager.IsAnyForwardStarted())
			{
				MainViewModel.TrayService.SetTrayIcon(ForwardState.None);
			}
		}

		public void OnTunnelException(Exception exception)
		{
			var forwardState = PortForwardManager.IsAnyForwardStarted() switch
			{
				true => ForwardState.PartiallyDown,
				false => ForwardState.AllDown,
			};

			Dispatcher.UIThread.Invoke(() => MainViewModel.TrayService.SetTrayIcon(forwardState));
		}

		[RelayCommand]
		public async Task StartAllSshTunnels()
		{
			using var cts = new CancellationTokenSource();

			async Task<string?> OnPromptPassphrase(PrivateKey privateKey)
			{
				var password = await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.DialogService.ShowPrivateKeyPasswordDialogAsync(privateKey));
				if (password is null)
				{
					await cts.CancelAsync();
				}

				return password;
			}

			foreach (var tunnel in Items)
			{
				await tunnel.StartTunnel(OnPromptPassphrase, cts.Token);
			}

			//await Task.WhenAll(Items.Select(tunnel => tunnel.StartTunnel(OnPromptPassphrase, cts.Token)));
		}

		[RelayCommand]
		public void StopAllSshTunnels()
		{
			foreach (var tunnel in Items)
			{
				tunnel.StopTunnel();
			}
		}

		[RelayCommand]
		public void AddSshTunnel()
		{
			var tunnel = new SshTunnel();

			StorageManager.SshTunnels.Add(tunnel);
			Items.Add(CreateSshTunnelControlModel(tunnel));
		}

		private void DeleteSshTunnel(Guid id)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(pk => pk.Id == id) is { } tunnelToDelete)
			{
				StorageManager.SshTunnels.Remove(tunnelToDelete);
				PortForwardManager.StopForward(tunnelToDelete);
			}

			if (Items.FirstOrDefault(i => i.Model.Id == id) is { } item)
			{
				Items.Remove(item);
			}
		}

		private static void ChangeSshTunnelName(Guid id, string? newName)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(s => s.Id == id) is not { } tunnelToUpdate)
				return;

			tunnelToUpdate.Name = newName;

			StorageManager.SaveSshTunnels();
		}

		private static void ChangeSshTunnelLocalPort(Guid id, int? newPort)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(s => s.Id == id) is not { } tunnelToUpdate)
				return;

			tunnelToUpdate.LocalPort = newPort;

			StorageManager.SaveSshTunnels();
		}

		private static void ChangeSshTunnelSshServer(Guid id, SshServer? newServer)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(s => s.Id == id) is not { } tunnelToUpdate)
				return;

			tunnelToUpdate.SshServer = newServer;

			StorageManager.SaveSshTunnels();
		}

		private static void ChangeSshTunnelPrivateKey(Guid id, PrivateKey? newPrivateKey)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(s => s.Id == id) is not { } tunnelToUpdate)
				return;

			tunnelToUpdate.PrivateKey = newPrivateKey;

			StorageManager.SaveSshTunnels();
		}

		private static void ChangeSshTunnelRemoteServer(Guid id, RemoteServer? newRemoteServer)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(s => s.Id == id) is not { } tunnelToUpdate)
				return;

			tunnelToUpdate.RemoteServer = newRemoteServer;

			StorageManager.SaveSshTunnels();
		}

		private void OnSshServerChanged(object? sender, SshServer sshServer)
		{
			foreach (var item in Items.Where(i => i.SelectedSshServer?.Id == sshServer.Id))
			{
				item.OnSshServerChanged(sender, sshServer);
			}
		}

		private void OnPrivateKeyChanged(object? sender, PrivateKey privateKey)
		{
			foreach (var item in Items.Where(i => i.SelectedPrivateKey?.Id == privateKey.Id))
			{
				item.OnPrivateKeyChanged(sender, privateKey);
			}
		}

		private void OnRemoteServerChanged(object? sender, RemoteServer remoteServer)
		{
			foreach (var item in Items.Where(i => i.SelectedRemoteServer?.Id == remoteServer.Id))
			{
				item.OnRemoteServerChanged(sender, remoteServer);
			}
		}

		private SshTunnelControlModel CreateSshTunnelControlModel(SshTunnel tunnel)
		{
			var sshTunnelControlModel = new SshTunnelControlModel(tunnel, StorageManager.SshServers, StorageManager.PrivateKeys, StorageManager.RemoteServers)
			{
				DeleteSshTunnel = new RelayCommand<Guid>(DeleteSshTunnel),
				ChangeSshTunnelName = ChangeSshTunnelName,
				ChangeSshTunnelLocalPort = ChangeSshTunnelLocalPort,
				SshServerSelectionChanged = ChangeSshTunnelSshServer,
				PrivateKeySelectionChanged = ChangeSshTunnelPrivateKey,
				RemoteServerSelectionChanged = ChangeSshTunnelRemoteServer,
				StartForward = OnStartForward,
				StopForward = OnStopForward,
			};

			return sshTunnelControlModel;
		}
	}
}
