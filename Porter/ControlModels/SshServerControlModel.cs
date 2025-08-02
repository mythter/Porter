using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;

namespace Porter.ControlModels
{
	public partial class SshServerControlModel : ObservableObject
	{
		[ObservableProperty]
		private string? name;

		[ObservableProperty]
		private string? user;

		[ObservableProperty]
		private string? host;

		[ObservableProperty]
		private int? port;

		public IRelayCommand<Guid>? DeleteSshServer { get; set; }

		public Action<Guid, string?>? ChangeSshServerName { get; set; }

		public Action<Guid, string?>? ChangeSshServerUser { get; set; }

		public Action<Guid, string?>? ChangeSshServerHost { get; set; }

		public Action<Guid, int?>? ChangeSshServerPort { get; set; }

		public Guid Id { get; set; }

		public SshServerControlModel(SshServer model)
		{
			Id = model.Id;
			name = model.Name;
			user = model.User;
			host = model.Host;
			port = model.Port;
		}

		public void OnNameLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshServerName?.Invoke(Id, Name);
		}

		public void OnNameKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshServerName?.Invoke(Id, Name);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnUserLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshServerUser?.Invoke(Id, User);
		}

		public void OnUserKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshServerUser?.Invoke(Id, User);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnHostLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshServerHost?.Invoke(Id, Host);
		}

		public void OnHostKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshServerHost?.Invoke(Id, Host);
				topLevel?.Focus();
				e.Handled = true;
			}
		}

		public void OnPortLostFocus(object? sender, RoutedEventArgs e)
		{
			ChangeSshServerPort?.Invoke(Id, Port);
		}

		public void OnPortKeyDown(object? sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangeSshServerPort?.Invoke(Id, Port);
				topLevel?.Focus();
				e.Handled = true;
			}
		}
	}
}
