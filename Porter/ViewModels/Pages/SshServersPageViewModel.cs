using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Porter.Enums;
using Porter.Extensions;
using Porter.Messages;
using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.ViewModels.Pages;

public partial class SshServersPageViewModel : PageViewModel, IReorderableViewModel
{
	#region Private Fields

	private readonly IMessenger _messenger;

	private readonly AppData _appData;

	#endregion

	#region Public Properties

	public ObservableCollection<SshServer> Items { get; }

	#endregion

	#region Constructors

	public SshServersPageViewModel(IMessenger messenger, IAppDataProvider<AppData> appData)
	{
		PageName = PageNames.SshServers;

		_messenger = messenger;
		_appData = appData.Value;

		Items = _appData.SshServers;
	}

	#endregion

	#region Public Methods

	public void MoveItem(int oldIndex, int newIndex) => _appData.SshServers.Move(oldIndex, newIndex);

	#endregion

	#region Commands

	[RelayCommand]
	public void GoToTunnels() => _messenger.Send(new NavigateMessage(PageNames.Tunnels));

	[RelayCommand]
	public void AddSshServer()
	{
		var server = new SshServer();

		_appData.SshServers.Add(server);
	}

	[RelayCommand]
	private void Delete(SshServer server)
	{
		ArgumentNullException.ThrowIfNull(server);

		_appData.SshServers.Remove(x => x.Id == server.Id);
	}

	#endregion
}
