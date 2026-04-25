using Porter.Enums;
using Porter.Factories;

namespace Porter.ViewModels
{
	public class MiniViewModel : ViewModelBase
	{
		public PageViewModel Page { get; set; }

		public MiniViewModel(PageFactory pageFactory)
		{
			Page = pageFactory.GetPageViewModel(PageNames.Tunnels);
		}
	}
}
