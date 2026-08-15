using CommunityToolkit.Mvvm.ComponentModel;

using Porter.Enums;

namespace Porter.Models;

public partial class AppSettings : ObservableObject
{
	[ObservableProperty]
	public partial bool OnCloseMinimizeToTray { get; set; }

	[ObservableProperty]
	public partial AppTheme Theme { get; set; }
}
