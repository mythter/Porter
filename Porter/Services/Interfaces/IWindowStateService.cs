using Avalonia.Controls;

using Porter.Models;

namespace Porter.Services.Interfaces;

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
