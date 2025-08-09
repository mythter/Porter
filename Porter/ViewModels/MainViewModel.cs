using System;

using CommunityToolkit.Mvvm.Input;

using Porter.Interfaces;

namespace Porter.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	#region Private Fields

	private readonly Action _exitAction;

	private readonly Action _openMainWindowAction;

	#endregion

	#region ViewModels

	public TunnelsViewModel TunnelsViewModel { get; private set; }

	public SshServersViewModel SshServersViewModel { get; private set; }

	public RemoteServersViewModel RemoteServersViewModel { get; private set; }

	public PrivateKeysViewModel PrivateKeysViewModel { get; private set; }

	#endregion

	#region Services

	public IDialogService DialogService { get; private set; }

	public ITrayService TrayService { get; private set; }

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
		ITrayService trayService,
		Action exitAction,
		Action openMainWindowAction)
	{
		DialogService = fileDialogService;
		TrayService = trayService;

		_exitAction = exitAction;
		_openMainWindowAction = openMainWindowAction;

		InitViewModels();

		_CurrentPage = TunnelsViewModel!;
	}

	public void InitViewModels()
	{
		SshServersViewModel = new SshServersViewModel(this);
		RemoteServersViewModel = new RemoteServersViewModel(this);
		PrivateKeysViewModel = new PrivateKeysViewModel(this);
		TunnelsViewModel = new TunnelsViewModel(this, _exitAction, _openMainWindowAction);
		OnPropertyChanged(nameof(TunnelsViewModel));
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
