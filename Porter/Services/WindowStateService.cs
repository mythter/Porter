using Avalonia;
using Avalonia.Controls;

using Porter.Models;

namespace Porter.Services;

public interface IWindowStateService
{
	/// <summary>
	/// Applies the persisted window position/size/maximized state to the given window.
	/// Falls back to centering on the primary screen when the persisted location is off-screen.
	/// </summary>
	void Restore(Window window, WindowSettings settings);

	/// <summary>
	/// Captures the current position/size/maximized state of the window into <paramref name="settings"/>.
	/// </summary>
	void Save(Window window, WindowSettings settings);
}

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
