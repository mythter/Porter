using CommunityToolkit.Mvvm.ComponentModel;

using Porter.Enums;

namespace Porter.ViewModels.Pages;

public partial class PageViewModel : ViewModelBase
{
	[ObservableProperty]
	private PageNames _pageName;
}
