using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Helpers;
using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.ViewModels.Controls;

public partial class SshTunnelViewModel : ObservableObject
{
	public bool IsDisconnected => !Model.IsTunnelStarted && !Model.IsConnecting;

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

	private SshServer? _selectedSshServer;
	public SshServer? SelectedSshServer
	{
		get => _selectedSshServer;
		set
		{
			SetProperty(ref _selectedSshServer, value);
			Model.SshServer = value;
		}
	}

	private RemoteServer? _selectedRemoteServer;
	public RemoteServer? SelectedRemoteServer
	{
		get => _selectedRemoteServer;
		set
		{
			SetProperty(ref _selectedRemoteServer, value);
			Model.RemoteServer = value;
		}
	}

	private PrivateKey? _selectedPrivateKey;
	public PrivateKey? SelectedPrivateKey
	{
		get => _selectedPrivateKey;
		set
		{
			SetProperty(ref _selectedPrivateKey, value);
			Model.PrivateKey = value;
		}
	}

	public string MiniToolTip => GetMiniToolTip();

	public ObservableCollection<SshServer> SshServers { get; init; }

	public ObservableCollection<PrivateKey> PrivateKeys { get; init; }

	public ObservableCollection<RemoteServer> RemoteServers { get; init; }

	public Func<SshTunnel, Action<Exception>?, Func<Task<string?>>?, CancellationToken?, Task<bool>>? StartForward { get; set; }

	public Action<SshTunnel>? StopForward { get; set; }

	public SshTunnelViewModel(SshTunnel model, IAppDataProvider<AppData> appData)
	{
		Model = model;

		SshServers = appData.Value.SshServers;
		PrivateKeys = appData.Value.PrivateKeys;
		RemoteServers = appData.Value.RemoteServers;

		_selectedSshServer = SshServers.FirstOrDefault(s => s.Id == model.SshServer?.Id);
		_selectedRemoteServer = RemoteServers.FirstOrDefault(s => s.Id == model.RemoteServer?.Id);
		_selectedPrivateKey = PrivateKeys.FirstOrDefault(s => s.Id == model.PrivateKey?.Id);

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
			else if (e.PropertyName is nameof(Model.IsTunnelStarted) or nameof(Model.IsConnecting))
			{
				OnPropertyChanged(nameof(IsDisconnected));
			}

			OnPropertyChanged(nameof(MiniToolTip));
		};
	}

	[RelayCommand]
	public async Task ToggleTunnel()
	{
		if (Model.IsTunnelStarted || Model.IsConnecting)
		{
			StopTunnel();
		}
		else
		{
			await StartTunnel();
		}
	}

	public async Task<bool> StartTunnel(Func<Task<string?>>? promptPassphrase = null, CancellationToken? cancellationToken = null)
	{
		if (!Model.IsTunnelStarted && StartForward is not null)
		{
			Model.IsConnecting = true;
			if (await StartForward(Model, OnTunnelException, promptPassphrase, cancellationToken))
			{
				Model.IsTunnelStarted = true;
			}
			Model.IsConnecting = false;
		}

		return Model.IsTunnelStarted;
	}

	public void StopTunnel()
	{
		if ((Model.IsConnecting || Model.IsTunnelStarted) && StopForward is not null)
		{
			StopForward(Model);
			Model.IsTunnelStarted = false;
		}
	}

	public void OnTunnelException(Exception ex)
	{
		Model.IsTunnelStarted = false;
	}

	private string GetMiniToolTip()
	{
		var sb = new StringBuilder();

		sb.AppendLine($"Tunnel Name: {Model.Name ?? "-"}");
		sb.AppendLine($"Local Port: {Model.LocalPort?.ToString() ?? "-"}");

		sb.Append("SSH server: ");
		if (StringHelper.IsAllNullOrEmpty(
			Model.SshServer?.Name,
			Model.SshServer?.User,
			Model.SshServer?.Host))
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
		if (StringHelper.IsAllNullOrEmpty(
			Model.PrivateKey?.Name,
			Model.PrivateKey?.FilePath))
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
		if (StringHelper.IsAllNullOrEmpty(
			Model.RemoteServer?.Name,
			Model.RemoteServer?.Host))
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
