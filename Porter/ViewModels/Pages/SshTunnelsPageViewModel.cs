using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

public partial class SshTunnelsPageViewModel : PageViewModel, IDialogContext
{
	#region Private Fields

	private readonly IMessenger _messenger;

	private readonly IPlatformServicesAccessor _platformServices;

	private readonly IAppDataProvider<AppData> _appDataProvider;

	private readonly ITunnelService _tunnelService;

	private readonly IDialogContextProvider _dialogContextProvider;

	private readonly Func<SshTunnel, SshTunnelViewModel> _sshTunnelViewModelFactory;

	private readonly Dictionary<SshTunnel, CancellationTokenSource> _connectingTunnels = [];

	private CancellationTokenSource? _startAllCancellationTokenSource;

	#endregion

	#region Public Properties

	public AppData AppData => _appDataProvider.Value;

	public AppSettings Settings => _appDataProvider.Value.Settings;

	public ObservableCollection<SshTunnelViewModel> Items { get; }

	#endregion

	#region Constructors

	public SshTunnelsPageViewModel(
		IMessenger messenger,
		IPlatformServicesAccessor platformServices,
		Func<SshTunnel, SshTunnelViewModel> sshTunnelViewModelFactory,
		ITunnelService tunnelService,
		IDialogContextProvider dialogContextProvider,
		IAppDataProvider<AppData> appDataProvider)
	{
		PageName = PageNames.Tunnels;

		_messenger = messenger;
		_platformServices = platformServices;
		_appDataProvider = appDataProvider;
		_sshTunnelViewModelFactory = sshTunnelViewModelFactory;
		_tunnelService = tunnelService;
		_dialogContextProvider = dialogContextProvider;

		Items = new ObservableCollection<SshTunnelViewModel>(AppData.SshTunnels.Select(CreateSshTunnelViewModel));

		AppData.SshTunnels.CollectionChanged += (s, e) =>
		{
			foreach (var tunnel in e.OldItems?.Cast<SshTunnel>() ?? [])
			{
				Items.Remove(x => x.Model.Id == tunnel.Id);
			}

			foreach (SshTunnel tunnel in e.NewItems?.Cast<SshTunnel>() ?? [])
			{
				Items.Add(CreateSshTunnelViewModel(tunnel));
			}
		};
	}

	#endregion

	#region Commands

	[RelayCommand]
	public void Exit() => _platformServices.Shutdown();

	[RelayCommand]
	public void OpenMainWindow() => _platformServices.MainWindow.Show();

	[RelayCommand]
	public void GoToSshServers() => _messenger.Send(new NavigateMessage(PageNames.SshServers));

	[RelayCommand]
	public void GoToRemoteServers() => _messenger.Send(new NavigateMessage(PageNames.RemoteServers));

	[RelayCommand]
	public void GoToPrivateKeys() => _messenger.Send(new NavigateMessage(PageNames.PrivateKeys));

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

				var promptPassphraseCallback = tunnel.PrivateKey?.FilePath is null
					? (Func<Task<string?>>?)null
					: () => Dispatcher.UIThread.InvokeAsync(() => ShowPrivateKeyPasswordDialogAsync(tunnel.PrivateKey));

				try
				{
					await _tunnelService.StartAsync(tunnel, promptPassphraseCallback, _startAllCancellationTokenSource.Token);
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
			_tunnelService.Stop(tunnel.Model);
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

	#region Private Methods

	private async Task<bool> OnStartForward(SshTunnel tunnel, CancellationToken? cancellationToken = null)
	{
		var cts = cancellationToken is null
			? new CancellationTokenSource()
			: CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);

		_connectingTunnels.TryAdd(tunnel, cts);

		var started = false;

		var promptPassphraseCallback = tunnel.PrivateKey?.FilePath is null
			? (Func<Task<string?>>?)null
			: () => Dispatcher.UIThread.InvokeAsync(() => ShowPrivateKeyPasswordDialogAsync(tunnel.PrivateKey));

		try
		{
			started = await _tunnelService.StartAsync(tunnel, promptPassphraseCallback, cts.Token);
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

	private void OnStopForward(SshTunnel tunnel)
	{
		if (_connectingTunnels.TryGetValue(tunnel, out var cts))
		{
			cts.Cancel();
		}

		_tunnelService.Stop(tunnel);
	}

	private SshTunnelViewModel CreateSshTunnelViewModel(SshTunnel tunnel)
	{
		var sshTunnelControlModel = _sshTunnelViewModelFactory(tunnel);

		sshTunnelControlModel.StartForward = OnStartForward;
		sshTunnelControlModel.StopForward = OnStopForward;

		return sshTunnelControlModel;
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
