using System;

using Avalonia.Controls.ApplicationLifetimes;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;

using Porter.Enums;
using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;
using Porter.Services.Ssh;
using Porter.ViewModels;
using Porter.ViewModels.Controls;
using Porter.ViewModels.Pages;
using Porter.Views;

namespace Porter.Extensions;

/// <summary>
/// Centralizes Porter's service registrations so <see cref="App"/> only owns lifecycle wiring.
/// All registrations are AOT-friendly: closed generics, no <c>ActivatorUtilities</c> with runtime
/// types, no reflection-based factories.
/// </summary>
public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddServices(
		this IServiceCollection services,
		IClassicDesktopStyleApplicationLifetime desktop)
	{
		// === ViewModels ===
		// MainViewModel is a singleton: it owns the navigation pipeline and acts as the main
		// IDialogContext. Page VMs are transient — recreated per navigation so state resets.
		services.AddSingleton<MainViewModel>();
		services.AddSingleton<MiniViewModel>();
		services.AddTransient<SshTunnelsPageViewModel>();
		services.AddTransient<PrivateKeysPageViewModel>();
		services.AddTransient<SshServersPageViewModel>();
		services.AddTransient<RemoteServersPageViewModel>();

		// Typed factory delegates (AOT-safe, no ActivatorUtilities).
		services.AddSingleton<Func<PageNames, PageViewModel>>(sp =>
			name => name switch
			{
				PageNames.Tunnels => sp.GetRequiredService<SshTunnelsPageViewModel>(),
				PageNames.PrivateKeys => sp.GetRequiredService<PrivateKeysPageViewModel>(),
				PageNames.SshServers => sp.GetRequiredService<SshServersPageViewModel>(),
				PageNames.RemoteServers => sp.GetRequiredService<RemoteServersPageViewModel>(),
				_ => throw new InvalidOperationException($"Unknown page: {name}")
			});

		// === Infrastructure / cross-cutting ===
		services.AddSingleton<ICrashLogger, FileCrashLogger>();
		services.AddSingleton<IMessenger, WeakReferenceMessenger>();
		services.AddSingleton<IPlatformServicesAccessor>(new PlatformServicesAccessor(desktop));
		services.AddSingleton<IAppDataProvider<AppData>, AppDataProvider>();
		services.AddSingleton<IWindowStateService, WindowStateService>();

		// Lazy factory breaks the construction cycle: DialogContextProvider resolves
		// MainViewModel only when a dialog is first shown — long after MainViewModel ctor returns.
		services.AddSingleton<IDialogContextProvider>(sp =>
			new DialogContextProvider(() => sp.GetRequiredService<MainViewModel>()));

		// === SSH stack ===
		services.AddSingleton<IPrivateKeyCache, PrivateKeyCache>();
		services.AddSingleton<PortForwardManager>();
		services.AddSingleton<ITunnelService, TunnelService>();
		services.AddSingleton<ITrayStateManager, TrayStateManager>();

		// === Windows ===
		// MiniWindow is a singleton because the tray service needs a stable instance to toggle.
		services.AddSingleton<MiniWindow>();

		// TrayService composes the desktop lifetime + MiniWindow. Registered as both concrete
		// (so App can grab TrayIcon for MainWindow) and as ITrayService for everything else.
		services.AddSingleton(sp => new TrayService(desktop, sp.GetRequiredService<MiniWindow>()));
		services.AddSingleton<ITrayService>(sp => sp.GetRequiredService<TrayService>());

		return services;
	}
}
