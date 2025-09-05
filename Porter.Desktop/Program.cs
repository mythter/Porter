using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia;

using Porter.Services;

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
				File.WriteAllText("fatal.log", ex.ToString());
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			File.WriteAllText("task.log", e.Exception.ToString());
			e.SetObserved();
		};

		try
		{
			BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(args);
		}
		catch (Exception ex)
		{
			var lastCrash = CrashService.GetCrashData();

			// Write a crash log
			if (CrashService.SetCrashData(ex))
			{
				// If we previously crashed in under 10 seconds, don't re-open
				if (lastCrash == null || lastCrash.CrashDate < DateTimeOffset.UtcNow - TimeSpan.FromSeconds(10))
				{
					TryRestartApp();
				}
			}
			else
			{
				throw;
			}
		}
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();

	private static void TryRestartApp()
	{
		try
		{
			Process.Start(typeof(Program).Assembly.Location.Replace(".dll", ".exe"));
		}
		catch
		{
			// Ignore
		}
	}
}
