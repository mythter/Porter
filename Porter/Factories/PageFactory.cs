using System;

using Porter.Enums;
using Porter.ViewModels;

namespace Porter.Factories
{
	public class PageFactory(Func<PageNames, PageViewModel> factory)
	{
		public PageViewModel GetPageViewModel(PageNames pageName) => factory(pageName);
	}
}
