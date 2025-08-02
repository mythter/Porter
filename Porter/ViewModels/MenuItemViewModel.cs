using System.Collections.Generic;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Porter.ViewModels
{
	public class MenuItemViewModel : ObservableObject
	{
		public string? Header { get; set; }

		public IRelayCommand? Command { get; set; }

		public MenuItemToggleType ToggleType { get; set; }

		private bool _isChecked;

		public bool IsChecked
		{
			get => _isChecked;
			set => SetProperty(ref _isChecked, value);
		}

		public IReadOnlyList<MenuItemViewModel>? Items { get; set; }
	}
}
