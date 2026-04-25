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

public partial class RemoteServersPageViewModel : PageViewModel
{
	private readonly IMessenger _messenger;

	private readonly AppData _appData;

	public ObservableCollection<RemoteServer> Items { get; }

	public RemoteServersPageViewModel(IMessenger messenger, IAppDataProvider<AppData> appData)
	{
		PageName = PageNames.RemoteServers;

		_messenger = messenger;
		_appData = appData.Value;

		Items = _appData.RemoteServers;
	}

	[RelayCommand]
	public void GoToTunnels() => _messenger.Send(new NavigateMessage(PageNames.Tunnels));

	[RelayCommand]
	public void AddRemoteServer()
	{
		var server = new RemoteServer();

		_appData.RemoteServers.Add(server);
	}

	[RelayCommand]
	private void Delete(RemoteServer remoteServer)
	{
		ArgumentNullException.ThrowIfNull(remoteServer);

		_appData.RemoteServers.Remove(x => x.Id == remoteServer.Id);
	}
}
