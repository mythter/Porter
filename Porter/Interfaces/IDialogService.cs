using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using Porter.Models;

namespace Porter.Interfaces
{
	public interface IDialogService
	{
		Task<IStorageFile?> ShowPrivateKeyOpenFileDialogAsync();

		Task<IStorageFile?> ShowSettingsOpenFileDialogAsync();

		Task ShowSettingsSaveFileDialogAsync();

		Task<string?> ShowPrivateKeyPasswordDialogAsync(PrivateKey privateKey);
	}
}
