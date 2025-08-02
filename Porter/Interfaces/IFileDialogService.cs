using System.Threading.Tasks;

using Avalonia.Platform.Storage;

namespace Porter.Interfaces
{
	public interface IFileDialogService
	{
		Task<IStorageFile?> ShowPrivateKeyOpenFileDialogAsync();
	}
}
