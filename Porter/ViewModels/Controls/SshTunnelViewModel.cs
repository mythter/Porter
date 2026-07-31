using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;

namespace Porter.ViewModels.Controls;

public partial class SshTunnelViewModel : ObservableObject, IDisposable
{
	#region Private Fields

	private bool _disposed;

	private readonly ITunnelService _tunnelService;

	private readonly Func<SshTunnel, CancellationToken?, Task<bool>> _startForward;

	private readonly Action<SshTunnel> _stopForward;

	private CancellationTokenSource? _connectingCts;

	#endregion

	#region Public Properties

	public bool IsNameNullOrEmpty => string.IsNullOrEmpty(Model.Name) && RemoteServerAlias is not null;

	public string? RemoteServerAlias => SelectedRemoteServer?.Host switch
	{
		not null => SelectedRemoteServer?.Port switch
		{
			not null => $"{SelectedRemoteServer?.Host}:{SelectedRemoteServer?.Port}",
			_ => SelectedRemoteServer?.Host
		},
		_ => null
	};

	public SshTunnel Model { get; }

	public SshTunnelState State { get; }

	public bool IsDisconnected => !State.IsRunning && !State.IsConnecting;

	[ObservableProperty]
	public partial SshServer? SelectedSshServer { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RemoteServerAlias))]
	public partial RemoteServer? SelectedRemoteServer { get; set; }

	[ObservableProperty]
	public partial PrivateKey? SelectedPrivateKey { get; set; }

	partial void OnSelectedSshServerChanged(SshServer? oldValue, SshServer? newValue)
	{
		Model.SshServerId = newValue?.Id;
		ResubscribeReferencedModel(oldValue, newValue);
	}

	partial void OnSelectedRemoteServerChanged(RemoteServer? oldValue, RemoteServer? newValue)
	{
		Model.RemoteServerId = newValue?.Id;
		ResubscribeReferencedModel(oldValue, newValue);
	}

	partial void OnSelectedPrivateKeyChanged(PrivateKey? oldValue, PrivateKey? newValue)
	{
		Model.PrivateKeyId = newValue?.Id;
		ResubscribeReferencedModel(oldValue, newValue);
	}

	public string MiniToolTip => GetMiniToolTip();

	public ObservableCollection<SshServer> SshServers { get; init; }

	public ObservableCollection<PrivateKey> PrivateKeys { get; init; }

	public ObservableCollection<RemoteServer> RemoteServers { get; init; }

	#endregion

	#region Constructors

	public SshTunnelViewModel(
		SshTunnel model,
		IAppDataProvider<AppData> appData,
		ITunnelService tunnelService,
		Func<SshTunnel, CancellationToken?, Task<bool>> startForward,
		Action<SshTunnel> stopForward)
	{
		ArgumentNullException.ThrowIfNull(model);
		ArgumentNullException.ThrowIfNull(startForward);
		ArgumentNullException.ThrowIfNull(stopForward);

		Model = model;
		State = tunnelService.GetState(model.Id);
		_tunnelService = tunnelService;
		_startForward = startForward;
		_stopForward = stopForward;

		SshServers = appData.Value.SshServers;
		PrivateKeys = appData.Value.PrivateKeys;
		RemoteServers = appData.Value.RemoteServers;

		// Assigning through the property triggers OnSelectedXxxChanged which mirrors the choice
		// onto the model. That's harmless here because Model already references the same instance,
		// but it keeps initialization on a single code path.
		SelectedSshServer = SshServers.FirstOrDefault(s => s.Id == model.SshServerId);
		SelectedRemoteServer = RemoteServers.FirstOrDefault(s => s.Id == model.RemoteServerId);
		SelectedPrivateKey = PrivateKeys.FirstOrDefault(s => s.Id == model.PrivateKeyId);

		Model.PropertyChanged += OnModelPropertyChanged;
		State.PropertyChanged += OnStatePropertyChanged;
	}

	#endregion

	#region Public Methods

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Commands

	[RelayCommand]
	public async Task ToggleTunnel()
	{
		if (State.IsRunning)
		{
			StopTunnel();
		}
		else if (State.IsConnecting && _connectingCts is not null)
		{
			await _connectingCts.CancelAsync();
		}
		else
		{
			await StartTunnel();
		}
	}

	#endregion

	#region Protected Methods

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;


		if (disposing)
		{
			// Detach from singleton models so this VM can be GC'd after the page is replaced.
			Model.PropertyChanged -= OnModelPropertyChanged;
			State.PropertyChanged -= OnStatePropertyChanged;

			UnsubscribeReferencedModel(SelectedSshServer);
			UnsubscribeReferencedModel(SelectedRemoteServer);
			UnsubscribeReferencedModel(SelectedPrivateKey);

			_connectingCts?.Cancel();
		}

		_disposed = true;
	}

	#endregion

	#region Private Methods

	private async Task<bool> StartTunnel()
	{
		using var cts = new CancellationTokenSource();
		_connectingCts = cts;

		try
		{
			return await _startForward(Model, cts.Token);
		}
		finally
		{
			if (ReferenceEquals(_connectingCts, cts))
				_connectingCts = null;
		}
	}

	private void StopTunnel()
	{
		_stopForward(Model);
	}

	private void ResubscribeReferencedModel(ObservableObject? oldValue, ObservableObject? newValue)
	{
		UnsubscribeReferencedModel(oldValue);

		newValue?.PropertyChanged += OnReferencedModelPropertyChanged;
	}

	private void UnsubscribeReferencedModel(ObservableObject? model)
	{
		model?.PropertyChanged -= OnReferencedModelPropertyChanged;
	}

	private async void OnReferencedModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (sender is RemoteServer)
			OnPropertyChanged(nameof(RemoteServerAlias));

		OnPropertyChanged(nameof(MiniToolTip));

		// Editing the connection details of a referenced model invalidates an active forward.
		if (AffectsForwarding(sender, e.PropertyName))
		{
			await RestartIfActive();
		}
	}

	private static bool AffectsForwarding(object? model, string? propertyName) => model switch
	{
		SshServer => propertyName is nameof(SshServer.User) or nameof(SshServer.Host) or nameof(SshServer.Port),
		RemoteServer => propertyName is nameof(RemoteServer.Host) or nameof(RemoteServer.Port),
		PrivateKey => propertyName is nameof(PrivateKey.FilePath),
		_ => false
	};

	private async Task RestartIfActive()
	{
		if (!State.IsRunning && !State.IsConnecting)
			return;

		// The same tunnel is shown by several view models (main and mini windows), so every
		// model change is observed more than once. Only the first observer restarts it,
		// otherwise the second one would tear down the forward the first one just started.
		if (!_tunnelService.TryBeginRestart(Model.Id))
			return;

		try
		{
			StopTunnel();
			await StartTunnel();
		}
		finally
		{
			_tunnelService.EndRestart(Model.Id);
		}
	}

	private async void OnModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Model.Name))
		{
			OnPropertyChanged(nameof(IsNameNullOrEmpty));
		}

		OnPropertyChanged(nameof(MiniToolTip));

		// Editing the forwarding parameters of the tunnel itself invalidates an active forward.
		if (e.PropertyName is nameof(Model.LocalPort)
			or nameof(Model.SshServerId)
			or nameof(Model.RemoteServerId)
			or nameof(Model.PrivateKeyId))
		{
			await RestartIfActive();
		}
	}

	private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(State.State))
		{
			OnPropertyChanged(nameof(IsDisconnected));
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "S3776:Cognitive Complexity of methods should not be too high", Justification = "It's okay here")]
	private string GetMiniToolTip()
	{
		var sb = new StringBuilder();

		sb.AppendLine($"Tunnel Name: {Model.Name ?? "-"}");
		sb.AppendLine($"Local Port: {Model.LocalPort?.ToString() ?? "-"}");

		sb.Append("SSH server: ");
		if (string.IsNullOrEmpty(SelectedSshServer?.Name)
			&& string.IsNullOrEmpty(SelectedSshServer?.User)
			&& string.IsNullOrEmpty(SelectedSshServer?.Host))
		{
			sb.AppendLine("-");
		}
		else
		{
			if (SelectedSshServer?.Name is not null)
			{
				sb.Append($"{SelectedSshServer?.Name}");
			}

			if (SelectedSshServer?.Host is not null)
			{
				if (SelectedSshServer?.Name is not null)
					sb.Append(" - ");

				sb.Append(SelectedSshServer?.User is null
					? SelectedSshServer?.Host
					: $"{SelectedSshServer?.User}@{SelectedSshServer?.Host}");

				if (SelectedSshServer?.Port is not null)
				{
					sb.Append($":{SelectedSshServer?.Port}");
				}
			}

			sb.AppendLine();
		}

		sb.Append("Private key: ");
		if (string.IsNullOrEmpty(SelectedPrivateKey?.Name)
			&& string.IsNullOrEmpty(SelectedPrivateKey?.FilePath))
		{
			sb.AppendLine("-");
		}
		else
		{
			if (SelectedPrivateKey?.Name is not null)
			{
				sb.Append($"{SelectedPrivateKey?.Name}");
			}

			if (SelectedPrivateKey?.FilePath is not null)
			{
				if (SelectedPrivateKey?.Name is not null)
					sb.Append(" - ");

				sb.Append(SelectedPrivateKey?.FilePath);
			}

			sb.AppendLine();
		}

		sb.Append("Remote server: ");
		if (string.IsNullOrEmpty(SelectedRemoteServer?.Name)
			&& string.IsNullOrEmpty(SelectedRemoteServer?.Host))
		{
			sb.Append('-');
		}
		else
		{
			if (SelectedRemoteServer?.Name is not null)
			{
				sb.Append($"{SelectedRemoteServer?.Name}");
			}

			if (SelectedRemoteServer?.Host is not null)
			{
				if (SelectedRemoteServer?.Name is not null)
					sb.Append(" - ");

				sb.Append(SelectedRemoteServer?.Port is null
					? SelectedRemoteServer?.Host
					: $"{SelectedRemoteServer?.Host}:{SelectedRemoteServer?.Port}");
			}
		}

		return sb.ToString();
	}

	#endregion
}
