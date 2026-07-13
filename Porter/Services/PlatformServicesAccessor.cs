using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using Porter.Services.Interfaces;

namespace Porter.Services;

public class PlatformServicesAccessor(IClassicDesktopStyleApplicationLifetime applicationLifetime) : IPlatformServicesAccessor
{
	public Window MainWindow => applicationLifetime.MainWindow!;

	public void Shutdown() => applicationLifetime.Shutdown();
}
