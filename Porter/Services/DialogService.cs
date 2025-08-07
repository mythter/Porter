using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Porter.Interfaces;
using Porter.Models;
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

		public async Task<string?> ShowPrivateKeyPasswordDialogAsync(PrivateKey privateKey)
		{
			var dialog = new PrivateKeyPasswordWindow
			{
				DataContext = new PrivateKeyPasswordViewModel(privateKey)
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
	}
}
