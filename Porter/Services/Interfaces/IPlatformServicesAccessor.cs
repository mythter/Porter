using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Porter.Services.Interfaces
{
	public interface IPlatformServicesAccessor
	{
		public Window MainWindow { get; }

		public IClipboard? Clipboard { get; }

		public void Shutdown();
	}
}
