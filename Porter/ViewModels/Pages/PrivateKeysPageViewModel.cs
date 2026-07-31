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

namespace Porter.ViewModels.Pages;

public partial class PrivateKeysPageViewModel : PageViewModel, IDialogContext, IReorderableViewModel
{
	#region Private Fields

	private readonly IMessenger _messenger;

	private readonly AppData _appData;

	#endregion

	#region Public Properties

	public ObservableCollection<PrivateKey> Items { get; }

	#endregion

	#region Constructors

	public PrivateKeysPageViewModel(IMessenger messenger, IAppDataProvider<AppData> appData)
	{
		PageName = PageNames.PrivateKeys;

		_messenger = messenger;
		_appData = appData.Value;

		Items = _appData.PrivateKeys;
	}

	#endregion

	#region Public Methods

	public void MoveItem(int oldIndex, int newIndex) => _appData.PrivateKeys.Move(oldIndex, newIndex);

	#endregion

	#region Commands

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

	#endregion

	#region Private Methods

	private Task<string?> ShowPrivateKeyOpenFileDialogAsync()
	{
		return this.ShowOpenFileDialogAsync(
			"Choose private key file",
			new Dictionary<string, string[]> { ["Private Keys"] = ["*.pem", "*.ppk"] }
		);
	}

	#endregion
}
