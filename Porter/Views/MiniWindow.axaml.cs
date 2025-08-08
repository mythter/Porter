using System;

using Avalonia.Controls;

using Porter.Enums;
using Porter.Helpers;

namespace Porter.Views;

public partial class MiniWindow : Window
{

	private const int _windowMargin = 5;

	private bool _isDeactivated;

	public MiniWindow()
	{
		InitializeComponent();

		Deactivated += OnDeactivated;
	}

	private void OnDeactivated(object? sender, EventArgs e)
	{
		_isDeactivated = true;
		Hide();
		Avalonia.Threading.DispatcherTimer.RunOnce(() => _isDeactivated = false, TimeSpan.FromMilliseconds(300));
	}

	public void ToggleVisibility()
	{
		if (IsVisible)
		{
			Hide();
		}
		else if (!_isDeactivated)
		{
			Show();
			PositionWindow();
			Activate();
		}
	}

	private void PositionWindow()
	{
		var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;

		if (screen is null)
			return;

		var (tbLocation, tbSize) = TaskBarHelper.GetTaskBarLocationAndSize(screen);

		var workArea = screen!.WorkingArea;

		var width = (int)(Width * screen.Scaling);
		var height = (int)(Height * screen.Scaling);

		(int x, int y) = tbLocation switch
		{
			TaskBarLocation.Top => (workArea.Width - width - _windowMargin, tbSize + _windowMargin),
			TaskBarLocation.Right => (workArea.Width - width - _windowMargin, workArea.Height - height - _windowMargin),
			TaskBarLocation.Bottom => (workArea.Width - width - _windowMargin, workArea.Height - height - _windowMargin),
			TaskBarLocation.Left => (tbSize + _windowMargin, workArea.Height - height - _windowMargin),
			_ => throw new NotSupportedException(),
		};

		Position = new Avalonia.PixelPoint(x, y);
	}
}