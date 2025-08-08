using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.Input;

using Porter.ControlModels;
using Porter.Models;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class RemoteServersViewModel : ViewModelBase
	{
		public MainViewModel MainViewModel { get; }

		public ObservableCollection<RemoteServerControlModel> Items { get; }

		public event EventHandler<RemoteServer>? RemoteServerChanged;

		public RemoteServersViewModel(MainViewModel mainViewModel)
		{
			MainViewModel = mainViewModel;

			Items = new ObservableCollection<RemoteServerControlModel>(
				StorageManager.RemoteServers.Select(CreateRemoteServerControlModel));
		}

		[RelayCommand]
		public void AddRemoteServer()
		{
			var server = new RemoteServer();

			StorageManager.RemoteServers.Add(server);
			Items.Add(CreateRemoteServerControlModel(server));
		}

		private void DeleteRemoteServer(Guid id)
		{
			if (StorageManager.RemoteServers.FirstOrDefault(pk => pk.Id == id) is { } serverToDelete)
			{
				StorageManager.RemoteServers.Remove(serverToDelete);
			}

			if (Items.FirstOrDefault(i => i.Id == id) is { } item)
			{
				Items.Remove(item);
			}
		}

		private void ChangeRemoteServerName(Guid id, string? newName)
		{
			if (StorageManager.RemoteServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Name = newName;

			RemoteServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private void ChangeRemoteServerHost(Guid id, string? newHost)
		{
			if (StorageManager.RemoteServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Host = newHost;

			RemoteServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private void ChangeRemoteServerPort(Guid id, int? newPort)
		{
			if (StorageManager.RemoteServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Port = newPort;

			RemoteServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private RemoteServerControlModel CreateRemoteServerControlModel(RemoteServer server)
		{
			return new RemoteServerControlModel(server)
			{
				DeleteRemoteServer = new RelayCommand<Guid>(DeleteRemoteServer),
				ChangeRemoteServerName = ChangeRemoteServerName,
				ChangeRemoteServerHost = ChangeRemoteServerHost,
				ChangeRemoteServerPort = ChangeRemoteServerPort,
			};
		}
	}
}
