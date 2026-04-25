using System.Threading.Tasks;

using Avalonia.Controls;

using Porter.Enums;
using Porter.Services.Interfaces;
using Porter.ViewModels;
using Porter.Views;

namespace Porter.Services;

public class DialogService : IDialogService
{
	private readonly Window _window;

	public DialogService(Window window)
	{
		_window = window;
	}

	public  Task ShowErrorAsync(string message, string? title = null)
	{
		return ShowMessageBoxAsync(message, title ?? "Error", MessageBoxIcon.Error);
	}

	private Task ShowMessageBoxAsync(string message, string title, MessageBoxIcon icon)
	{
		if (!_window.IsVisible)
		{
			_window.Show();
		}

		var dialog = new MessageBoxWindow
		{
			DataContext = new MessageBoxViewModel(title, message, icon),
		};

		return dialog.ShowDialog(_window);
	}
}
