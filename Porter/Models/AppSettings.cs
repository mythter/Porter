using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class AppSettings : ObservableObject
{
	[ObservableProperty]
	private bool onCloseMinimizeToTray;
}
