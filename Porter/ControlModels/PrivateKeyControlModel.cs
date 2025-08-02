using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;

namespace Porter.ControlModels
{
	public partial class PrivateKeyControlModel : ObservableObject
	{
		[ObservableProperty]
		private string? name;

		[ObservableProperty]
		private string filePath;

		public IRelayCommand<Guid>? DeletePrivateKey { get; set; }

		public Func<Guid, Task>? ChangePrivateKeyFile { get; set; }

		public Action<Guid, string?>? ChangePrivateKeyName { get; set; }

		public Guid Id { get; set; }

		public PrivateKeyControlModel(PrivateKey model)
		{
			Id = model.Id;
			name = model.Name;
			filePath = model.FilePath;
		}

		public async Task OnFilePathClick(object? sender, PointerPressedEventArgs e)
		{
			if (ChangePrivateKeyFile is not null)
			{
				await ChangePrivateKeyFile.Invoke(Id);
			}
		}

		public void OnFileNameLostFocus(object sender, RoutedEventArgs e)
		{
			ChangePrivateKeyName?.Invoke(Id, Name);
		}

		public void OnFileNameKeyDown(object sender, KeyEventArgs e, TopLevel? topLevel)
		{
			if (e.Key == Key.Enter)
			{
				ChangePrivateKeyName?.Invoke(Id, Name);
				topLevel?.Focus();
				e.Handled = true;
			}
		}
	}
}
