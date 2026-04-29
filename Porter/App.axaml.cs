using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;

using Porter.Enums;
using Porter.Factories;
using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;
using Porter.Services.Ssh;
using Porter.ViewModels;
using Porter.ViewModels.Controls;
using Porter.ViewModels.Pages;
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
		var collection = new ServiceCollection();

		collection.AddTransient<MainViewModel>();
		collection.AddTransient<MiniViewModel>();
		collection.AddTransient<SshTunnelsPageViewModel>();
		collection.AddTransient<PrivateKeysPageViewModel>();
		collection.AddTransient<SshServersPageViewModel>();
		collection.AddTransient<RemoteServersPageViewModel>();

		collection.AddSingleton<IAppDataProvider<AppData>, AppDataProvider>();

		collection.AddSingleton<Func<PageNames, PageViewModel>>(sp =>
			name => name switch
			{
				PageNames.Tunnels => sp.GetRequiredService<SshTunnelsPageViewModel>(),
				PageNames.PrivateKeys => sp.GetRequiredService<PrivateKeysPageViewModel>(),
				PageNames.SshServers => sp.GetRequiredService<SshServersPageViewModel>(),
				PageNames.RemoteServers => sp.GetRequiredService<RemoteServersPageViewModel>(),
				_ => throw new InvalidOperationException()
			});

		collection.AddSingleton<PageFactory>();

		collection.AddSingleton<PortForwardManager>();
		collection.AddSingleton<ITunnelService, TunnelService>();

		collection.AddSingleton<Func<SshTunnel, SshTunnelViewModel>>(sp =>
			tunnel => new SshTunnelViewModel(tunnel, sp.GetRequiredService<IAppDataProvider<AppData>>(), sp.GetRequiredService<ITunnelService>()));

		collection.AddSingleton<IDialogContextProvider, DialogContextProvider>();

		collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			collection.AddSingleton<IPlatformServicesAccessor>(new PlatformServicesAccessor(desktop));

			var miniWindow = new MiniWindow();

			var trayService = new TrayService(desktop, miniWindow);

			collection.AddSingleton<ITrayService>(trayService);

			var services = collection.BuildServiceProvider();
			Ioc.Default.ConfigureServices(services);

			miniWindow.DataContext = services.GetRequiredService<MiniViewModel>();

			var mainViewModel = services.GetRequiredService<MainViewModel>();

			var contextProvider = (DialogContextProvider)services.GetRequiredService<IDialogContextProvider>();
			contextProvider.SetMainDialogContext(mainViewModel);

			desktop.MainWindow = new MainWindow(services.GetRequiredService<IAppDataProvider<AppData>>())
			{
				DataContext = mainViewModel,
				TrayIcon = trayService.TrayIcon,
				MiniWindow = miniWindow
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
