using Avalonia.Controls;

namespace Porter.Services.Interfaces;

public interface IPlatformServicesAccessor
{
	public Window MainWindow { get; }

	public void Shutdown();
}
