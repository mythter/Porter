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
using Porter.Services;
using Porter.Services.Interfaces;
using Porter.ViewModels.Controls;

namespace Porter.ViewModels.Pages;

public partial class SshTunnelsPageViewModel : PageViewModel, IDialogContext
{
	#region Private Fields

	private readonly IMessenger _messenger;

	private readonly IPlatformServicesAccessor _platformServices;

	private readonly TrayService _trayService;

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

	public SshTunnelsPageViewModel()
	{
		PageName = PageNames.Tunnels;
	}

	public SshTunnelsPageViewModel(
		IMessenger messenger,
		TrayService trayService,
		IPlatformServicesAccessor platformServices,
		Func<SshTunnel, SshTunnelViewModel> sshTunnelViewModelFactory,
		ITunnelService tunnelService,
		IDialogContextProvider dialogContextProvider,
		IAppDataProvider<AppData> appDataProvider)
	{
		PageName = PageNames.Tunnels;

		_messenger = messenger;
		_platformServices = platformServices;
		_trayService = trayService;
		_appDataProvider = appDataProvider;
		_sshTunnelViewModelFactory = sshTunnelViewModelFactory;
		_tunnelService = tunnelService;
		_dialogContextProvider = dialogContextProvider;

		_tunnelService.TunnelFailed += OnTunnelFailed;

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

		var startedTunnels = 0;

		try
		{
			foreach (var tunnel in Items.Select(i => i.Model))
			{
				if (_startAllCancellationTokenSource.IsCancellationRequested)
					break;

				var promptPassphraseCallback = tunnel.PrivateKey?.FilePath is null
					? (Func<Task<string?>>?)null
					: () => Dispatcher.UIThread.InvokeAsync(() => ShowPrivateKeyPasswordDialogAsync(tunnel.PrivateKey));

				var started = false;

				try
				{
					started = await _tunnelService.StartAsync(tunnel, promptPassphraseCallback, _startAllCancellationTokenSource.Token);
				}
				catch { /* ignore */ }

				if (started)
					startedTunnels++;
			}
		}
		finally
		{
			_startAllCancellationTokenSource.Dispose();
			_startAllCancellationTokenSource = null;
		}

		if (startedTunnels == Items.Count)
		{
			_trayService.SetTrayIcon(ForwardState.AllUp);
		}
		else if (startedTunnels == 0)
		{
			_trayService.SetTrayIcon(ForwardState.AllDown);
		}
		else
		{
			_trayService.SetTrayIcon(ForwardState.PartiallyDown);
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

		_trayService.SetTrayIcon(ForwardState.None);
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
			_appDataProvider.Load(file);

			//MainViewModel.GoToTunnels();
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
		catch { /* ignore */ }
		finally
		{
			cts?.Dispose();
			_connectingTunnels.Remove(tunnel);
		}

		if (started)
		{
			_trayService.SetTrayIcon(ForwardState.AllUp);
		}
		else
		{
			_trayService.SetTrayIcon(ForwardState.None);
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

		if (!_tunnelService.IsAnyForwardStarted())
		{
			_trayService.SetTrayIcon(ForwardState.None);
		}
	}

	private void OnTunnelFailed(SshTunnel tunnel, Exception exception)
	{
		var forwardState = _tunnelService.IsAnyForwardStarted() switch
		{
			true => ForwardState.PartiallyDown,
			false => ForwardState.AllDown,
		};

		Dispatcher.UIThread.Invoke(() => _trayService.SetTrayIcon(forwardState));

		//Dispatcher.UIThread.InvokeAsync(async () =>
		//{
		//	await MainViewModel.DialogService.ShowErrorAsync(
		//		$"Tunnel {tunnel.Name ?? tunnel.LocalPort.ToString()} stopped.\n" +
		//		$"Exception message: {exception.Message}\n" +
		//		$"Stack trace: {exception.StackTrace ?? "not available"}");
		//});
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
