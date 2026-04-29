using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class WindowSettings : ObservableObject
{
	[ObservableProperty]
	public partial int Left { get; set; }

	[ObservableProperty]
	public partial int Top { get; set; }

	[ObservableProperty]
	public partial double Width { get; set; }

	[ObservableProperty]
	public partial double Height { get; set; }

	[ObservableProperty]
	public partial bool Maximized { get; set; }
}
