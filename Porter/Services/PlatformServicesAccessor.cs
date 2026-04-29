using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

using Porter.Services.Interfaces;

namespace Porter.Services;

public class PlatformServicesAccessor(IClassicDesktopStyleApplicationLifetime applicationLifetime) : IPlatformServicesAccessor
{
	public Window MainWindow => applicationLifetime.MainWindow!;

	public IClipboard? Clipboard => applicationLifetime.MainWindow?.Clipboard;

	public void Shutdown() => applicationLifetime.Shutdown();
}
