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

	public SshKeysViewModel SshKeysViewModel { get; }

	#endregion

	#region Services

	public IFileDialogService FileDialogService { get; private set; }

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
		IFileDialogService fileDialogService,
		Action exitAction,
		Action openMainWindow)
	{
		FileDialogService = fileDialogService;

		TunnelsViewModel = new TunnelsViewModel(this, exitAction, openMainWindow);
		SshServersViewModel = new SshServersViewModel(this);
		RemoteServersViewModel = new RemoteServersViewModel(this);
		SshKeysViewModel = new SshKeysViewModel(this);

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
	public void GoToSshKeys()
	{
		CurrentPage = SshKeysViewModel;
	}

	[RelayCommand]
	public void GoToTunnels()
	{
		CurrentPage = TunnelsViewModel;
	}
}
