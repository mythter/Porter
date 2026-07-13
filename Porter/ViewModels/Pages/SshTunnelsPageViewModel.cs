using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Myth.Avalonia.Services.Abstractions;
using Myth.Avalonia.Services.Extensions;

using Porter.Enums;
using Porter.Extensions;
using Porter.Messages;
using Porter.Models;
using Porter.Services.Interfaces;
using Porter.ViewModels.Controls;

namespace Porter.ViewModels.Pages;

public partial class SshTunnelsPageViewModel : PageViewModel, IDialogContext, IDisposable
{
	#region Private Fields

	private readonly IMessenger _messenger;

	private readonly IPlatformServicesAccessor _platformServices;

	private readonly IAppDataProvider<AppData> _appDataProvider;

	private readonly ITunnelService _tunnelService;

	private readonly IDialogContextProvider _dialogContextProvider;

	private readonly Dictionary<SshTunnel, CancellationTokenSource> _connectingTunnels = [];

	private CancellationTokenSource? _startAllCancellationTokenSource;

	private bool _disposed;

	#endregion

	#region Public Properties

	public AppData AppData => _appDataProvider.Value;

	public AppSettings Settings => _appDataProvider.Value.Settings;

	public ObservableCollection<SshTunnelViewModel> Items { get; }

	public static AppTheme[] AppThemes { get; } = Enum.GetValues<AppTheme>();

	#endregion

	#region Constructors

	public SshTunnelsPageViewModel(
		IMessenger messenger,
		IPlatformServicesAccessor platformServices,
		ITunnelService tunnelService,
		IDialogContextProvider dialogContextProvider,
		IAppDataProvider<AppData> appDataProvider)
	{
		PageName = PageNames.Tunnels;

		_messenger = messenger;
		_platformServices = platformServices;
		_appDataProvider = appDataProvider;
		_tunnelService = tunnelService;
		_dialogContextProvider = dialogContextProvider;

		Items = new ObservableCollection<SshTunnelViewModel>(AppData.SshTunnels.Select(CreateSshTunnelViewModel));

		AppData.SshTunnels.CollectionChanged += OnSshTunnelsCollectionChanged;

		Settings.PropertyChanged += OnSettingsChanges;
	}

	#endregion

	#region Commands

	[RelayCommand]
	public void OpenMainWindow() => _platformServices.MainWindow.Show();

	[RelayCommand]
	public void GoToSshServers() => _messenger.Send(new NavigateMessage(PageNames.SshServers));

	[RelayCommand]
	public void GoToRemoteServers() => _messenger.Send(new NavigateMessage(PageNames.RemoteServers));

	[RelayCommand]
	public void GoToPrivateKeys() => _messenger.Send(new NavigateMessage(PageNames.PrivateKeys));

	[RelayCommand]
	private void SelectTheme(AppTheme theme) => Settings.Theme = theme;

	[RelayCommand]
	public async Task StartAllSshTunnels()
	{
		if (Items.Count == 0)
			return;

		_startAllCancellationTokenSource ??= new CancellationTokenSource();

		try
		{
			foreach (var tunnel in Items.Select(i => i.Model))
			{
				if (_startAllCancellationTokenSource.IsCancellationRequested)
					break;

				try
				{
					await StartTunnel(tunnel, _startAllCancellationTokenSource.Token);
				}
				catch (OperationCanceledException)
				{
					// User-initiated cancellation; stop iterating but treat as non-error.
					break;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"StartAsync failed for tunnel {tunnel.Name}: {ex}");
				}
			}
		}
		finally
		{
			_startAllCancellationTokenSource.Dispose();
			_startAllCancellationTokenSource = null;
		}
	}

	[RelayCommand]
	public void StopAllSshTunnels()
	{
		_startAllCancellationTokenSource?.Cancel();

		foreach (var tunnel in Items)
		{
			_tunnelService.Stop(tunnel.Model.Id);
		}
	}

	[RelayCommand]
	public void AddSshTunnel()
	{
		var tunnel = new SshTunnel();

		AppData.SshTunnels.Add(tunnel);
	}

	[RelayCommand]
	private void Delete(SshTunnel tunnel)
	{
		ArgumentNullException.ThrowIfNull(tunnel);

		_tunnelService.Stop(tunnel.Id);

		AppData.SshTunnels.Remove(x => x.Id == tunnel.Id);
	}

	[RelayCommand]
	public async Task ExportSettings()
	{
		var file = await this.ShowSaveFileDialogAsync(
			title: "Export settings",
			suggestedFileName: "settings.json",
			fileTypeFilter: new Dictionary<string, string[]> { ["JSON files"] = ["*.json"] }
		);

		if (file is not null)
		{
			_appDataProvider.Save(file);
		}
	}

	[RelayCommand]
	public async Task ImportSettings()
	{
		var file = await this.ShowOpenFileDialogAsync(
			"Choose settings file",
			new Dictionary<string, string[]> { ["JSON files"] = ["*.json"] }
		);

		if (file is not null)
		{
			// Tear down running tunnels first — after Load() the SshTunnel instances they were
			// started against are orphaned and the UI loses its handles to stop them.
			StopAllSshTunnels();

			_appDataProvider.Load(file);

			// Load() replaces _appDataProvider.Value with a fresh AppData instance, so all
			// collections this page is bound to are now orphaned. Re-navigating recreates the
			// page VM against the new Value and rebinds the UI.
			_messenger.Send(new NavigateMessage(PageNames.Tunnels));
		}
	}

	#endregion

	#region Public Methods

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Protected Methods

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			AppData.SshTunnels.CollectionChanged -= OnSshTunnelsCollectionChanged;
			Settings.PropertyChanged -= OnSettingsChanges;

			foreach (var item in Items)
				item.Dispose();

			Items.Clear();

			_startAllCancellationTokenSource?.Cancel();
			_startAllCancellationTokenSource?.Dispose();
			_startAllCancellationTokenSource = null;

			foreach (var cts in _connectingTunnels.Values)
			{
				cts.Cancel();
				cts.Dispose();
			}

			_connectingTunnels.Clear();
		}

		_disposed = true;
	}

	#endregion

	#region Private Methods

	private void OnSshTunnelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		foreach (var tunnel in e.OldItems?.Cast<SshTunnel>() ?? [])
		{
			// Dispose the matching VM so its event subscriptions on Model/State are released
			// before we drop the reference.
			var match = Items.FirstOrDefault(x => x.Model.Id == tunnel.Id);
			if (match is not null)
			{
				Items.Remove(match);
				match.Dispose();
			}
		}

		foreach (SshTunnel tunnel in e.NewItems?.Cast<SshTunnel>() ?? [])
		{
			Items.Add(CreateSshTunnelViewModel(tunnel));
		}
	}

	private void OnSettingsChanges(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(AppSettings.Theme) &&
			Application.Current is { } app)
		{
			app.RequestedThemeVariant = Settings.Theme switch
			{
				AppTheme.Light => ThemeVariant.Light,
				AppTheme.Dark => ThemeVariant.Dark,
				_ => ThemeVariant.Default,
			};
		}
	}

	private async Task<bool> OnStartForward(SshTunnel tunnel, CancellationToken? cancellationToken = null)
	{
		var cts = cancellationToken is null
			? new CancellationTokenSource()
			: CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);

		_connectingTunnels.TryAdd(tunnel, cts);

		var started = false;

		try
		{
			started = await StartTunnel(tunnel, cts.Token);
		}
		catch (OperationCanceledException)
		{
			// Cancelled by the user via the toggle button; not an error.
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"StartAsync failed for tunnel {tunnel.Name}: {ex}");
		}
		finally
		{
			cts?.Dispose();
			_connectingTunnels.Remove(tunnel);
		}

		return started;
	}

	private Task<bool> StartTunnel(SshTunnel tunnel, CancellationToken cancellationToken = default)
	{
		var sshServer = AppData.SshServers.FirstOrDefault(s => s.Id == tunnel.SshServerId);
		var remoteServer = AppData.RemoteServers.FirstOrDefault(s => s.Id == tunnel.RemoteServerId);
		var privateKey = AppData.PrivateKeys.FirstOrDefault(k => k.Id == tunnel.PrivateKeyId);

		// TODO: maybe show a message to the user if any of these are null, instead of silently skipping them
		if (sshServer is null || remoteServer is null)
			return Task.FromResult(false);

		var promptPassphraseCallback = privateKey?.FilePath is null
			? (Func<Task<string?>>?)null
			: () => Dispatcher.UIThread.InvokeAsync(() => ShowPrivateKeyPasswordDialogAsync(privateKey));

		return _tunnelService.StartAsync(tunnel, sshServer, remoteServer, privateKey, promptPassphraseCallback, cancellationToken);
	}

	private void OnStopForward(SshTunnel tunnel)
	{
		if (_connectingTunnels.TryGetValue(tunnel, out var cts))
		{
			cts.Cancel();
		}

		_tunnelService.Stop(tunnel.Id);
	}

	private SshTunnelViewModel CreateSshTunnelViewModel(SshTunnel tunnel)
	{
		return new SshTunnelViewModel(
			tunnel,
			_appDataProvider,
			_tunnelService,
			OnStartForward,
			OnStopForward);
	}

	private async Task<string?> ShowPrivateKeyPasswordDialogAsync(PrivateKey privateKey)
	{
		var mainContext = _dialogContextProvider.GetMainDialogContext();

		var dialogViewModel = new PrivateKeyPasswordViewModel(privateKey);
		var password = await mainContext.ShowDialogWindowSafe<string>("Enter private key passphrase", dialogViewModel);

		return password;
	}

	#endregion
}
