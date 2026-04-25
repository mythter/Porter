using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Myth.Avalonia.Services.Abstractions;
using Myth.Avalonia.Services.Extensions;

using Porter.Enums;
using Porter.Extensions;
using Porter.Messages;
using Porter.Models;
using Porter.Services.Interfaces;
using Porter.Storage;

namespace Porter.ViewModels
{
	public partial class PrivateKeysPageViewModel : PageViewModel, IDialogContext
	{
		private readonly IMessenger _messenger;

		private readonly AppData _appData;

		public ObservableCollection<PrivateKey> Items { get; }

		public PrivateKeysPageViewModel(IMessenger messenger, IAppDataProvider<AppData> appData)
		{
			PageName = PageNames.PrivateKeys;

			_messenger = messenger;
			_appData = appData.Value;

			Items = _appData.PrivateKeys;
		}

		[RelayCommand]
		public void GoToTunnels() => _messenger.Send(new NavigateMessage(PageNames.Tunnels));

		[RelayCommand]
		public async Task AddPrivateKey()
		{
			var filePath = await ShowPrivateKeyOpenFileDialogAsync();

			if (filePath is null)
				return;

			var pk = new PrivateKey(filePath);

			_appData.PrivateKeys.Add(pk);
		}

		[RelayCommand]
		private void Delete(PrivateKey pk)
		{
			ArgumentNullException.ThrowIfNull(pk);

			_appData.PrivateKeys.Remove(x => x.Id == pk.Id);
		}

		[RelayCommand]
		private async Task OpenPrivateKeyFile(PrivateKey pk)
		{
			var filePath = await ShowPrivateKeyOpenFileDialogAsync();

			if (filePath is null)
				return;

			pk.FilePath = filePath;
		}

		private Task<string?> ShowPrivateKeyOpenFileDialogAsync()
		{
			return this.ShowOpenFileDialogAsync(
				"Choose private key file",
				new Dictionary<string, string[]> { ["Private Keys"] = ["*.pem", "*.ppk"] }
			);
		}
	}
}
