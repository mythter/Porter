using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Myth.Avalonia.Services.Abstractions;

using Porter.Enums;
using Porter.Messages;
using Porter.ViewModels.Pages;

namespace Porter.ViewModels;

public partial class MainViewModel : ViewModelBase, IRecipient<NavigateMessage>, IDialogContext
{
	#region Private Fields

	private readonly Func<PageNames, PageViewModel> _pageFactory;

	private readonly IMessenger _messenger;

	#endregion

	#region Public Properties

	[ObservableProperty]
	public partial PageViewModel CurrentPage { get; set; } = null!;

	#endregion

	#region Constructors

	public MainViewModel(Func<PageNames, PageViewModel> pageFactory, IMessenger messenger)
	{
		_pageFactory = pageFactory;
		_messenger = messenger;

		messenger.Register(this);

		// Use the unified navigation pipeline for the initial page so that any future Receive-side
		// hooks (logging, history, etc.) also see it.
		_messenger.Send(new NavigateMessage(PageNames.Tunnels));
	}

	#endregion

	#region Commands

	[RelayCommand]
	public void GoToSshServers() => _messenger.Send(new NavigateMessage(PageNames.SshServers));

	[RelayCommand]
	public void GoToRemoteServers() => _messenger.Send(new NavigateMessage(PageNames.RemoteServers));

	[RelayCommand]
	public void GoToPrivateKeys() => _messenger.Send(new NavigateMessage(PageNames.PrivateKeys));

	[RelayCommand]
	public void GoToTunnels() => _messenger.Send(new NavigateMessage(PageNames.Tunnels));

	#endregion

	#region Implementation IRecipient<NavigateMessage>

	public void Receive(NavigateMessage message)
	{
		var previous = CurrentPage;
		CurrentPage = _pageFactory(message.Page);

		// Page VMs are transient (a fresh instance per navigation). Dispose the previous one
		// so its event subscriptions on AppData/State release before it's GC'd.
		if (previous is IDisposable disposable && !ReferenceEquals(previous, CurrentPage))
			disposable.Dispose();
	}

	#endregion
}
