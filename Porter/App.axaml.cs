using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

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

			var dialogService = new DialogService(mainWindow);
			
			var trayService = new TrayService(Current!, (s, e) => exitCommand(), mainWindow);

			vm = new MainViewModel(dialogService, trayService, exitCommand, openMainWindowCommand);

			mainWindow.DataContext = vm;

			var miniWindow = new MiniWindow()
			{
				DataContext = vm
			};

			mainWindow.TrayIcon = trayService.TrayIcon;
			mainWindow.MiniWindow = miniWindow;
			mainWindow.DialogService = dialogService;

			desktop.MainWindow = mainWindow;
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
}
