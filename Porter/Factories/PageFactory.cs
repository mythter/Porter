using System;

using Porter.Enums;
using Porter.ViewModels.Pages;

namespace Porter.Factories;

public class PageFactory(Func<PageNames, PageViewModel> factory)
{
	public PageViewModel GetPageViewModel(PageNames pageName) => factory(pageName);
}
