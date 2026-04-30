using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using Porter.Configuration;
using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;
using Porter.ViewModels;
using Porter.Views;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Porter.Controls")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Porter.AttachedProperties")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Porter.Behaviors")]

namespace Porter;

public partial class App : Application
{
	private ServiceProvider? _serviceProvider;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var services = new ServiceCollection()
				.AddServices(desktop)
				.BuildServiceProvider();

			Ioc.Default.ConfigureServices(services);
			_serviceProvider = services;

			// Resolve once so the tray manager subscribes to ITunnelService events for the app's
			// lifetime (otherwise the indicator would only update if some VM happened to resolve it).
			services.GetRequiredService<ITrayStateManager>().Refresh();

			var trayService = services.GetRequiredService<TrayService>();

			var miniWindow = services.GetRequiredService<MiniWindow>();
			miniWindow.DataContext = services.GetRequiredService<MiniViewModel>();

			var mainViewModel = services.GetRequiredService<MainViewModel>();

			desktop.MainWindow = new MainWindow(
				services.GetRequiredService<IAppDataProvider<AppData>>(),
				services.GetRequiredService<IWindowStateService>())
			{
				DataContext = mainViewModel,
				TrayIcon = trayService.TrayIcon,
				MiniWindow = miniWindow
			};

			// Ensure SSH tunnels and cached private keys are released cleanly at shutdown.
			desktop.Exit += OnDesktopExit;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		// Disposing the ServiceProvider disposes singletons that implement IDisposable
		// (PortForwardManager — closes all SSH connections; PrivateKeyCache — wipes keys).
		_serviceProvider?.Dispose();
		_serviceProvider = null;
	}
}
