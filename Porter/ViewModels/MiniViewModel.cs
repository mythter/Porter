using System;

using CommunityToolkit.Mvvm.Messaging;

using Porter.Enums;
using Porter.Messages;
using Porter.ViewModels.Pages;

namespace Porter.ViewModels;

public class MiniViewModel : ViewModelBase, IRecipient<NavigateMessage>
{
	#region Private Fields

	private readonly Func<PageNames, PageViewModel> _pageFactory;

	private PageViewModel? _page;

	#endregion

	#region Public Properties

	// Lazy: the embedded page VM only materializes when the mini-window is first shown.
	public PageViewModel Page => _page ??= _pageFactory(PageNames.Tunnels);

	#endregion

	#region Constructors

	public MiniViewModel(Func<PageNames, PageViewModel> pageFactory, IMessenger messenger)
	{
		_pageFactory = pageFactory;

		messenger.Register(this);
	}

	#endregion

	#region Public Methods

	public void Receive(NavigateMessage message)
	{
		// When the main window re-navigates to Tunnels (e.g., after importing settings),
		// the AppData instance has been replaced. Reset our cached page so it's recreated
		// with the new data on the next access.
		if (message.Page == PageNames.Tunnels && _page is not null)
		{
			if (_page is IDisposable disposable)
				disposable.Dispose();

			_page = null;
			OnPropertyChanged(nameof(Page));
		}
	}

	#endregion
}
