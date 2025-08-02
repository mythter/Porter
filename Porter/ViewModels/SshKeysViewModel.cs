using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Porter.ControlModels;
using Porter.Models;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class SshKeysViewModel : ViewModelBase
	{
		public MainViewModel MainViewModel { get; }

		public ObservableCollection<PrivateKeyControlModel> Items { get; }

		public SshKeysViewModel(MainViewModel mainViewModel)
		{
			MainViewModel = mainViewModel;

			Items = new ObservableCollection<PrivateKeyControlModel>(
				StorageManager.PrivateKeys.Select(CreatePrivateKeyControlModel));
		}

		[RelayCommand]
		public async Task AddPrivateKey()
		{
			var file = await MainViewModel.FileDialogService.ShowPrivateKeyOpenFileDialogAsync();

			if (file is null)
				return;

			var pk = new PrivateKey(file.Path.AbsolutePath);

			StorageManager.PrivateKeys.Add(pk);
			Items.Add(CreatePrivateKeyControlModel(pk));
		}

		private PrivateKeyControlModel CreatePrivateKeyControlModel(PrivateKey pk)
		{
			return new PrivateKeyControlModel(pk)
			{
				DeletePrivateKey = new RelayCommand<Guid>(DeletePrivateKey),
				ChangePrivateKeyFile = ChangePrivateKeyFile,
				ChangePrivateKeyName = ChangePrivateKeyName,
			};
		}

		private void DeletePrivateKey(Guid id)
		{
			if (StorageManager.PrivateKeys.FirstOrDefault(pk => pk.Id == id) is not { } privateKeyToDelete)
				return;

			StorageManager.PrivateKeys.Remove(privateKeyToDelete);

			if (Items.FirstOrDefault(i => i.Id == id) is { } item)
			{
				Items.Remove(item);
			}
		}

		private static void ChangePrivateKeyName(Guid id, string? newName)
		{
			if (StorageManager.PrivateKeys.FirstOrDefault(pk => pk.Id == id) is not { } privateKeyToUpdate)
				return;

			privateKeyToUpdate.Name = newName;

			StorageManager.SavePrivateKeys();
		}

		private async Task ChangePrivateKeyFile(Guid id)
		{
			if (StorageManager.PrivateKeys.FirstOrDefault(pk => pk.Id == id) is not { } privateKeyToUpdate)
				return;

			var file = await MainViewModel.FileDialogService.ShowPrivateKeyOpenFileDialogAsync();

			if (file is null)
				return;

			var filePath = file.Path.AbsolutePath;

			privateKeyToUpdate.FilePath = filePath;

			StorageManager.SavePrivateKeys();

			if (Items.FirstOrDefault(i => i.Id == id) is { } item)
			{
				item.FilePath = filePath;
			}
		}
	}
}
