using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class AppSettings : ObservableObject
{
	[ObservableProperty]
	public partial bool OnCloseMinimizeToTray { get; set; }
}
