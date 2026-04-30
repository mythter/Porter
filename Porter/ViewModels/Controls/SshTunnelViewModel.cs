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

public partial class SshTunnelViewModel : ObservableObject
{
	private CancellationTokenSource? _connectingCts;

	public bool IsNameNullOrEmpty => string.IsNullOrEmpty(Model.Name) && RemoteServerAlias is not null;

	public string? RemoteServerAlias => Model.RemoteServer?.Host switch
	{
		not null => Model.RemoteServer.Port switch
		{
			not null => $"{Model.RemoteServer.Host}:{Model.RemoteServer.Port}",
			_ => Model.RemoteServer.Host
		},
		_ => null
	};

	public SshTunnel Model { get; }

	public SshTunnelState State { get; }

	public bool IsDisconnected => !State.IsRunning && !State.IsConnecting;

	[ObservableProperty]
	public partial SshServer? SelectedSshServer { get; set; }

	[ObservableProperty]
	public partial RemoteServer? SelectedRemoteServer { get; set; }

	[ObservableProperty]
	public partial PrivateKey? SelectedPrivateKey { get; set; }

	partial void OnSelectedSshServerChanged(SshServer? value) => Model.SshServer = value;

	partial void OnSelectedRemoteServerChanged(RemoteServer? value) => Model.RemoteServer = value;

	partial void OnSelectedPrivateKeyChanged(PrivateKey? value) => Model.PrivateKey = value;

	public string MiniToolTip => GetMiniToolTip();

	public ObservableCollection<SshServer> SshServers { get; init; }

	public ObservableCollection<PrivateKey> PrivateKeys { get; init; }

	public ObservableCollection<RemoteServer> RemoteServers { get; init; }

	public Func<SshTunnel, CancellationToken?, Task<bool>>? StartForward { get; set; }

	public Action<SshTunnel>? StopForward { get; set; }

	public SshTunnelViewModel(SshTunnel model, IAppDataProvider<AppData> appData, ITunnelService tunnelService)
	{
		Model = model;
		State = tunnelService.GetState(model.Id);

		SshServers = appData.Value.SshServers;
		PrivateKeys = appData.Value.PrivateKeys;
		RemoteServers = appData.Value.RemoteServers;

		// Assigning through the property triggers OnSelectedXxxChanged which mirrors the choice
		// onto the model. That's harmless here because Model already references the same instance,
		// but it keeps initialization on a single code path.
		SelectedSshServer = SshServers.FirstOrDefault(s => s.Id == model.SshServer?.Id);
		SelectedRemoteServer = RemoteServers.FirstOrDefault(s => s.Id == model.RemoteServer?.Id);
		SelectedPrivateKey = PrivateKeys.FirstOrDefault(s => s.Id == model.PrivateKey?.Id);

		Model.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(Model.Name))
			{
				OnPropertyChanged(nameof(IsNameNullOrEmpty));
			}
			else if (e.PropertyName == nameof(Model.RemoteServer))
			{
				OnPropertyChanged(nameof(RemoteServerAlias));
			}

			OnPropertyChanged(nameof(MiniToolTip));
		};

		State.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(State.State))
			{
				OnPropertyChanged(nameof(IsDisconnected));
			}
		};
	}

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

	private async Task<bool> StartTunnel()
	{
		if (StartForward is null)
			return false;

		using var cts = new CancellationTokenSource();
		_connectingCts = cts;

		try
		{
			return await StartForward(Model, cts.Token);
		}
		finally
		{
			if (ReferenceEquals(_connectingCts, cts))
				_connectingCts = null;
		}
	}

	private void StopTunnel()
	{
		if (StopForward is not null)
		{
			StopForward(Model);
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "S3776:Cognitive Complexity of methods should not be too high", Justification = "It's okay here")]
	private string GetMiniToolTip()
	{
		var sb = new StringBuilder();

		sb.AppendLine($"Tunnel Name: {Model.Name ?? "-"}");
		sb.AppendLine($"Local Port: {Model.LocalPort?.ToString() ?? "-"}");

		sb.Append("SSH server: ");
		if (string.IsNullOrEmpty(Model.SshServer?.Name)
			&& string.IsNullOrEmpty(Model.SshServer?.User)
			&& string.IsNullOrEmpty(Model.SshServer?.Host))
		{
			sb.AppendLine("-");
		}
		else
		{
			if (Model.SshServer?.Name is not null)
			{
				sb.Append($"{Model.SshServer?.Name}");
			}

			if (Model.SshServer?.Host is not null)
			{
				if (Model.SshServer?.Name is not null)
					sb.Append(" - ");

				sb.Append(Model.SshServer?.User is null
					? Model.SshServer?.Host
					: $"{Model.SshServer?.User}@{Model.SshServer?.Host}");

				if (Model.SshServer?.Port is not null)
				{
					sb.Append($":{Model.SshServer?.Port}");
				}
			}

			sb.AppendLine();
		}

		sb.Append("Private key: ");
		if (string.IsNullOrEmpty(Model.PrivateKey?.Name)
			&& string.IsNullOrEmpty(Model.PrivateKey?.FilePath))
		{
			sb.AppendLine("-");
		}
		else
		{
			if (Model.PrivateKey?.Name is not null)
			{
				sb.Append($"{Model.PrivateKey?.Name}");
			}

			if (Model.PrivateKey?.FilePath is not null)
			{
				if (Model.PrivateKey?.Name is not null)
					sb.Append(" - ");

				sb.Append(Model.PrivateKey?.FilePath);
			}

			sb.AppendLine();
		}

		sb.Append("Remote server: ");
		if (string.IsNullOrEmpty(Model.RemoteServer?.Name)
			&& string.IsNullOrEmpty(Model.RemoteServer?.Host))
		{
			sb.Append('-');
		}
		else
		{
			if (Model.RemoteServer?.Name is not null)
			{
				sb.Append($"{Model.RemoteServer?.Name}");
			}

			if (Model.RemoteServer?.Host is not null)
			{
				if (Model.RemoteServer?.Name is not null)
					sb.Append(" - ");

				sb.Append(Model.RemoteServer?.Port is null
					? Model.RemoteServer?.Host
					: $"{Model.RemoteServer?.Host}:{Model.RemoteServer?.Port}");
			}
		}

		return sb.ToString();
	}
}
