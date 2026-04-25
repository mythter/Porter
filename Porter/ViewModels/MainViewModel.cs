using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Porter.Enums;
using Porter.Factories;
using Porter.Messages;
using Porter.ViewModels.Pages;

namespace Porter.ViewModels;

public partial class MainViewModel : ViewModelBase, IRecipient<NavigateMessage>
{
	#region Private Fields

	private readonly PageFactory _pageFactory;

	#endregion

	[ObservableProperty]
	private PageViewModel _currentPage;

	public MainViewModel()
	{

	}

	public MainViewModel(PageFactory pageFactory, IMessenger messenger)
	{
		_pageFactory = pageFactory;

		messenger.Register(this);

		GoToTunnels();
	}

	[RelayCommand]
	public void GoToSshServers() => CurrentPage = _pageFactory.GetPageViewModel(PageNames.SshServers);

	[RelayCommand]
	public void GoToRemoteServers() => CurrentPage = _pageFactory.GetPageViewModel(PageNames.RemoteServers);

	[RelayCommand]
	public void GoToPrivateKeys() => CurrentPage = _pageFactory.GetPageViewModel(PageNames.PrivateKeys);

	[RelayCommand]
	public void GoToTunnels() => CurrentPage = _pageFactory.GetPageViewModel(PageNames.Tunnels);

	public void Receive(NavigateMessage message)
	{
		CurrentPage = _pageFactory.GetPageViewModel(message.Page);
	}
}
