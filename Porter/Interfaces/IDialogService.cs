using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using Porter.Models;

namespace Porter.Interfaces
{
	public interface IDialogService
	{
		Task ShowInfoAsync(string message, string? title = null);

		Task ShowWarningAsync(string message, string? title = null);

		Task ShowErrorAsync(string message, string? title = null);

		Task<IStorageFile?> ShowPrivateKeyOpenFileDialogAsync();

		Task<IStorageFile?> ShowSettingsOpenFileDialogAsync();

		Task ShowSettingsSaveFileDialogAsync();

		Task<string?> ShowPrivateKeyPasswordDialogAsync(PrivateKey privateKey);
	}
}
