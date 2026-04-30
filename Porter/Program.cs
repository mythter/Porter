using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Avalonia;

using Porter.Services;

namespace Porter;

[SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "Entry point class")]
internal sealed class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		// All early-startup logging routes through one append-only file (crash.log) keyed by
		// category — replaces the legacy fatal.log/task.log/crash.log trio.
		var logger = new FileCrashLogger();

		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			if (e.ExceptionObject is Exception ex)
				logger.Log("AppDomain.UnhandledException", ex);
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			logger.Log("TaskScheduler.UnobservedTaskException", e.Exception);
			e.SetObserved();
		};

		try
		{
			BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(args);
		}
		catch (Exception ex)
		{
			logger.Log("Startup", ex);

			var lastCrash = CrashService.GetCrashData();

			// Write a crash marker so the next launch can show the recovery dialog.
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
#if DEBUG
				.WithDeveloperTools()
#endif
			.WithInterFont()
			.LogToTrace();

	private static void TryRestartApp()
	{
		try
		{
			// Environment.ProcessPath is the canonical AOT/single-file-safe way to get the
			// running executable path (Assembly.Location returns empty for embedded assemblies).
			var exe = Environment.ProcessPath;
			if (!string.IsNullOrEmpty(exe))
				Process.Start(exe);
		}
		catch
		{
			// Ignore
		}
	}
}
