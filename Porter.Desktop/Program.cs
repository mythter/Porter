using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia;

namespace Porter.Desktop;

class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			if (e.ExceptionObject is Exception ex)
				File.WriteAllText("fatal2.log", ex.ToString());
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			File.WriteAllText("task.log", e.Exception.ToString());
			e.SetObserved();
		};

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();

}
