using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;

using Microsoft.Extensions.DependencyInjection;

using Myth.Avalonia.Controls.Enums;

using Porter.Configuration;
using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;
using Porter.ViewModels;
using Porter.Views;

using Color = Avalonia.Media.Color;

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

			_serviceProvider = services;

			// Resolve once so the tray manager subscribes to ITunnelService events for the app's
			// lifetime (otherwise the indicator would only update if some VM happened to resolve it).
			services.GetRequiredService<ITrayStateManager>().Refresh();

			var trayService = services.GetRequiredService<TrayService>();

			var miniWindow = services.GetRequiredService<MiniWindow>();
			miniWindow.DataContext = services.GetRequiredService<MiniViewModel>();

			var mainViewModel = services.GetRequiredService<MainViewModel>();

			var appDataProvider = services.GetRequiredService<IAppDataProvider<AppData>>();

			ApplyTheme(appDataProvider);

			desktop.MainWindow = new MainWindow(
				appDataProvider,
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

	private static void ApplyTheme(IAppDataProvider<AppData> appDataProvider)
	{
		if (Application.Current is { } app)
		{
			app.ActualThemeVariantChanged += (sender, args) =>
			{
				if (app.ActualThemeVariant == ThemeVariant.Light &&
					Application.Current!.TryGetResource("ToggleButtonBackgroundChecked", out var resource) &&
					resource is SolidColorBrush brush)
				{
					Application.Current.Resources["CustomToggleButtonBackgroundChecked"] = LightenPercent(brush.Color, 0.6f);
				}
			};

			app.RequestedThemeVariant = appDataProvider.Value.Settings.Theme switch
			{
				AppTheme.Light => ThemeVariant.Light,
				AppTheme.Dark => ThemeVariant.Dark,
				_ => ThemeVariant.Default,
			};
		}
	}

	private static Color LightenPercent(Color color, float percent)
	{
		var hsl = color.ToHsl();

		var l = hsl.L + (hsl.L * percent);
		l = Math.Clamp(l, 0f, 1f);

		return HsvColor.FromHsv(hsl.H, hsl.S, l).ToRgb();
	}
}
