using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.ControlModels;
using Porter.Models;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class TunnelsViewModel : ViewModelBase
	{
		[ObservableProperty]
		private string status = "Приложение запущено";

		public IRelayCommand DoSomethingCommand { get; }
		public IRelayCommand ExitCommand { get; }
		public IRelayCommand OpenMainWindow { get; }

		public MainViewModel MainViewModel { get; }

		public List<MenuItemViewModel> SettingsMenuItems { get; }

		public ObservableCollection<SshTunnelControlModel> Items { get; }

		public TunnelsViewModel()
		{

		}

		public TunnelsViewModel(MainViewModel mainViewModel, Action exitAction, Action openMainWindow)
		{
			MainViewModel = mainViewModel;

			Items = new ObservableCollection<SshTunnelControlModel>(
				StorageManager.SshTunnels.Select(CreateSshTunnelControlModel));

			DoSomethingCommand = new RelayCommand(() =>
			{
				Status = $"Сделано! {DateTime.Now:T}";
			});

			ExitCommand = new RelayCommand(exitAction);
			OpenMainWindow = new RelayCommand(openMainWindow);

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

		[RelayCommand]
		public void AddSshTunnel()
		{
			var tunnel = new SshTunnel();

			StorageManager.SshTunnels.Add(tunnel);
			Items.Add(CreateSshTunnelControlModel(tunnel));
		}

		private void DeleteSshTunnel(Guid id)
		{
			if (StorageManager.SshTunnels.FirstOrDefault(pk => pk.Id == id) is not { } tunnelToDelete)
				return;

			StorageManager.SshTunnels.Remove(tunnelToDelete);

			if (Items.FirstOrDefault(i => i.Id == id) is { } item)
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

		private SshTunnelControlModel CreateSshTunnelControlModel(SshTunnel tunnel)
		{
			return new SshTunnelControlModel(tunnel, StorageManager.SshServers, StorageManager.PrivateKeys, StorageManager.RemoteServers)
			{
				DeleteSshTunnel = new RelayCommand<Guid>(DeleteSshTunnel),
				ChangeSshTunnelName = ChangeSshTunnelName,
				ChangeSshTunnelLocalPort = ChangeSshTunnelLocalPort,
				SshServerSelectionChanged = ChangeSshTunnelSshServer,
				PrivateKeySelectionChanged = ChangeSshTunnelPrivateKey,
				RemoteServerSelectionChanged = ChangeSshTunnelRemoteServer,
			};
		}
	}
}
