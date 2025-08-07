using System;

using CommunityToolkit.Mvvm.Input;

using Porter.Interfaces;

namespace Porter.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	#region Commands


	#endregion

	#region ViewModels

	public TunnelsViewModel TunnelsViewModel { get; }

	public SshServersViewModel SshServersViewModel { get; }

	public RemoteServersViewModel RemoteServersViewModel { get; }

	public PrivateKeysViewModel PrivateKeysViewModel { get; }

	#endregion

	#region Services

	public IDialogService DialogService { get; private set; }

	#endregion

	private ViewModelBase _CurrentPage;

	/// <summary>
	/// Gets the current page. The property is read-only
	/// </summary>
	public ViewModelBase CurrentPage
	{
		get { return _CurrentPage; }
		private set { SetProperty(ref _CurrentPage, value); }
	}

	public MainViewModel()
	{

	}

	public MainViewModel(
		IDialogService fileDialogService,
		Action exitAction,
		Action openMainWindow)
	{
		DialogService = fileDialogService;

		SshServersViewModel = new SshServersViewModel(this);
		RemoteServersViewModel = new RemoteServersViewModel(this);
		PrivateKeysViewModel = new PrivateKeysViewModel(this);
		TunnelsViewModel = new TunnelsViewModel(this, exitAction, openMainWindow);

		_CurrentPage = TunnelsViewModel;
	}

	[RelayCommand]
	public void GoToSshServers()
	{
		CurrentPage = SshServersViewModel;
	}

	[RelayCommand]
	public void GoToRemoteServers()
	{
		CurrentPage = RemoteServersViewModel;
	}

	[RelayCommand]
	public void GoToPrivateKeys()
	{
		CurrentPage = PrivateKeysViewModel;
	}

	[RelayCommand]
	public void GoToTunnels()
	{
		CurrentPage = TunnelsViewModel;
	}
}
