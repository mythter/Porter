using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.Input;

using Porter.ControlModels;
using Porter.Models;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class SshServersViewModel : ViewModelBase
	{
		public MainViewModel MainViewModel { get; }

		public ObservableCollection<SshServerControlModel> Items { get; }

		public event EventHandler<SshServer>? SshServerChanged;

		public SshServersViewModel(MainViewModel mainViewModel)
		{
			MainViewModel = mainViewModel;

			Items = new ObservableCollection<SshServerControlModel>(
				StorageManager.SshServers.Select(CreateSshServerControlModel));
		}

		[RelayCommand]
		public void AddSshServer()
		{
			var server = new SshServer();

			StorageManager.SshServers.Add(server);
			Items.Add(CreateSshServerControlModel(server));
		}

		private void DeleteSshServer(Guid id)
		{
			if (StorageManager.SshServers.FirstOrDefault(pk => pk.Id == id) is not { } serverToDelete)
				return;

			StorageManager.SshServers.Remove(serverToDelete);

			if (Items.FirstOrDefault(i => i.Id == id) is { } item)
			{
				Items.Remove(item);
			}
		}

		private void ChangeSshServerName(Guid id, string? newName)
		{
			if (StorageManager.SshServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Name = newName;

			SshServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private void ChangeSshServerUser(Guid id, string? newUser)
		{
			if (StorageManager.SshServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.User = newUser;

			SshServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private void ChangeSshServerHost(Guid id, string? newHost)
		{
			if (StorageManager.SshServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Host = newHost;

			SshServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private void ChangeSshServerPort(Guid id, int? newPort)
		{
			if (StorageManager.SshServers.FirstOrDefault(s => s.Id == id) is not { } serverToUpdate)
				return;

			serverToUpdate.Port = newPort;

			SshServerChanged?.Invoke(this, serverToUpdate);

			StorageManager.SaveSshServers();
		}

		private SshServerControlModel CreateSshServerControlModel(SshServer server)
		{
			return new SshServerControlModel(server)
			{
				DeleteSshServer = new RelayCommand<Guid>(DeleteSshServer),
				ChangeSshServerName = ChangeSshServerName,
				ChangeSshServerUser = ChangeSshServerUser,
				ChangeSshServerHost = ChangeSshServerHost,
				ChangeSshServerPort = ChangeSshServerPort,
			};
		}
	}
}
