using Porter.Enums;
using Porter.Factories;
using Porter.ViewModels.Pages;

namespace Porter.ViewModels;

public class MiniViewModel(PageFactory pageFactory) : ViewModelBase
{
	public PageViewModel Page { get; set; } = pageFactory.GetPageViewModel(PageNames.Tunnels);
}
