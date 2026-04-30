using System;

using Porter.Enums;
using Porter.ViewModels.Pages;

namespace Porter.ViewModels;

public class MiniViewModel(Func<PageNames, PageViewModel> pageFactory) : ViewModelBase
{
	public PageViewModel Page { get; set; } = pageFactory(PageNames.Tunnels);
}
