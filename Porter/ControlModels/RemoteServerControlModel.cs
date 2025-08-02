using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;

namespace Porter.ControlModels
{
	public partial class RemoteServerControlModel : ObservableObject
	{
		[ObservableProperty]
		private string? name;

		[ObservableProperty]
		private string? host;

		[ObservableProperty]
		private int? port;

		public IRelayCommand<Guid>? DeleteRemoteServer { get; set; }

		public Action<Guid, string?>? ChangeRemoteServerName { get; set; }

		public Action<Guid, string?>? ChangeRemoteServerHost { get; set; }

		public Action<Guid, int?>? ChangeRemoteServerPort { get; set; }

		public Guid Id { get; set; }

		public RemoteServerControlModel(RemoteServer model)
		{
			Id = model.Id;
			name = model.Name;
			host = model.Host;
			port = model.Port;
		}

		public void OnNameLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeRemoteServerName?.Invoke(Id, Name);
		}

		public void OnNameKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeRemoteServerName?.Invoke(Id, Name);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnHostLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeRemoteServerHost?.Invoke(Id, Host);
		}

		public void OnHostKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeRemoteServerHost?.Invoke(Id, Host);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnPortLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeRemoteServerPort?.Invoke(Id, Port);
		}

		public void OnPortKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeRemoteServerPort?.Invoke(Id, Port);
				topLevel?.Focus();
				e.Handled = true;
			}
		}
	}
}
