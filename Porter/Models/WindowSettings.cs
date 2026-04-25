using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class WindowSettings : ObservableObject
{
	[ObservableProperty]
	private int left;

	[ObservableProperty]
	private int top;

	[ObservableProperty]
	private double width;

	[ObservableProperty]
	private double height;

	[ObservableProperty]
	private bool maximized;
}
