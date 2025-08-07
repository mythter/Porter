using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;

using Porter.Services;
using Porter.ViewModels;
using Porter.Views;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Porter.Controls")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Porter.AttachedProperties")]

namespace Porter;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Line below is needed to remove Avalonia data validation.
		// Without this line you will get duplicate validations from both Avalonia and CT
		BindingPlugins.DataValidators.RemoveAt(0);

		var vm = default(MainViewModel);

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			void exitCommand() => desktop.Shutdown();

			var mainWindow = new MainWindow();

			void openMainWindowCommand() => mainWindow.Show();

			var fileDialogService = new DialogService(mainWindow);

			vm = new MainViewModel(fileDialogService, exitCommand, openMainWindowCommand);

			mainWindow.DataContext = vm;

			desktop.MainWindow = mainWindow;

			var miniWindow = new MiniWindow()
			{
				DataContext = vm
			};

			var trayIcon = InitTrayIcon(desktop, mainWindow);

			mainWindow.TrayIcon = trayIcon;
			mainWindow.MiniWindow = miniWindow;
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			vm = new MainViewModel();

			singleViewPlatform.MainView = new TunnelsView
			{
				DataContext = vm
			};
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static TrayIcon InitTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
	{
		var exitItem = new NativeMenuItem("Exit");
		exitItem.Click += (s, e) => desktop.Shutdown();

		var openMainWindowItem = new NativeMenuItem("Open Main Window");
		openMainWindowItem.Click += (s, e) =>
		{
			if (!mainWindow.IsVisible)
			{
				mainWindow.Show();
			}

			mainWindow.Activate();
		};

		var trayIcon = new TrayIcon
		{
			Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://Porter/Assets/avalonia-logo.ico")))),
			ToolTipText = "Porter App",
			Menu = [
				openMainWindowItem,
				new NativeMenuItemSeparator(),
				exitItem,
			]
		};

		trayIcon.Clicked += (s, e) => mainWindow.MiniWindow?.ToggleVisibility();

		var icons = new TrayIcons() { trayIcon };

		TrayIcon.SetIcons(Current!, icons);

		return trayIcon;
	}
}
