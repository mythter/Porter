using Avalonia;
using Avalonia.Controls;

using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.Services;

public sealed class WindowStateService : IWindowStateService
{
	public void Restore(Window window, WindowSettings settings)
	{
		if (settings.Maximized)
		{
			window.WindowState = WindowState.Maximized;
			return;
		}

		var screen = window.Screens.ScreenFromPoint(new PixelPoint(settings.Left, settings.Top));
		if (screen is null)
		{
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			return;
		}

		if (settings.Width > 0 && settings.Height > 0)
		{
			window.Position = new PixelPoint(settings.Left, settings.Top);
			window.Width = settings.Width;
			window.Height = settings.Height;
		}
	}

	public void Save(Window window, WindowSettings settings)
	{
		settings.Left = window.Position.X;
		settings.Top = window.Position.Y;
		settings.Width = window.Width;
		settings.Height = window.Height;
		settings.Maximized = window.WindowState == WindowState.Maximized;
	}
}
