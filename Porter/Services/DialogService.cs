using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Porter.Enums;
using Porter.Interfaces;
using Porter.Models;
using Porter.Storage;
using Porter.ViewModels;
using Porter.Views;

namespace Porter.Services
{
	public class DialogService : IDialogService
	{
		private readonly Window _window;

		public DialogService(Window window)
		{
			_window = window;
		}

		public async Task<IStorageFile?> ShowPrivateKeyOpenFileDialogAsync()
		{
			var options = new FilePickerOpenOptions
			{
				Title = "Choose private key file",
				AllowMultiple = false,
				FileTypeFilter =
				[
					new FilePickerFileType("Private Keys") { Patterns = [ "*.pem", "*.ppk" ] }
				]
			};

			var result = await _window.StorageProvider.OpenFilePickerAsync(options);
			return result.FirstOrDefault();
		}

		public async Task<IStorageFile?> ShowSettingsOpenFileDialogAsync()
		{
			var options = new FilePickerOpenOptions
			{
				Title = "Choose settings file",
				AllowMultiple = false,
				FileTypeFilter =
				[
					new FilePickerFileType("JSON files") { Patterns = [ "*.json" ] }
				]
			};

			var result = await _window.StorageProvider.OpenFilePickerAsync(options);
			return result.FirstOrDefault();
		}

		public async Task ShowSettingsSaveFileDialogAsync()
		{
			var options = new FilePickerSaveOptions
			{
				Title = "Export settings",
				SuggestedFileName = "settings.json",
				FileTypeChoices =
				[
					new FilePickerFileType("JSON files") { Patterns = ["*.json"] }
				]
			};

			var file = await _window.StorageProvider.SaveFilePickerAsync(options);

			if (file != null)
			{
				StorageManager.Export(file.Path.AbsolutePath);
			}
		}

		public async Task<string?> ShowPrivateKeyPasswordDialogAsync(PrivateKey privateKey)
		{
			var dialog = new PrivateKeyPasswordWindow
			{
				DataContext = new PrivateKeyPasswordViewModel(privateKey),
				WindowStartupLocation = WindowStartupLocation.CenterScreen
			};

			var windowVisible = _window.IsVisible;
			var windowState = _window.WindowState;

			if (!windowVisible)
			{
				_window.WindowState = WindowState.Minimized;
				_window.Show();
			}

			var password = await dialog.ShowDialog<string>(_window);

			if (!windowVisible)
			{
				_window.Hide();
				_window.WindowState = windowState;
			}

			return password;
		}

		public Task ShowInfoAsync(string message, string? title = null)
		{
			return ShowMessageBoxAsync(message, title ?? "Info", MessageBoxIcon.Info);
		}

		public Task ShowWarningAsync(string message, string? title = null)
		{
			return ShowMessageBoxAsync(message, title ?? "Warning", MessageBoxIcon.Warning);
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
}
