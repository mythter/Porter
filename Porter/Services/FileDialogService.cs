using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Porter.Interfaces;

namespace Porter.Services
{
	public class FileDialogService : IFileDialogService
	{
		private readonly Window _window;

		public FileDialogService(Window window)
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
	}
}
