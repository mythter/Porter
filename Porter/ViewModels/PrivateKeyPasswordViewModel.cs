using System.IO;

using Avalonia.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Myth.Avalonia.Services.Abstractions;
using Myth.Avalonia.Services.Extensions;

using Porter.Models;

namespace Porter.ViewModels;

public partial class PrivateKeyPasswordViewModel : ViewModelBase, IDialogContext
{
	#region Public Properties

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(PasswordChar))]
	public partial bool IsPasswordVisible { get; set; }

	public char? PasswordChar => IsPasswordVisible ? null : '•';

	[ObservableProperty]
	public partial string? Password { get; set; }

	public string Message { get; set; }

	#endregion

	#region Constructors

	public PrivateKeyPasswordViewModel(PrivateKey privateKey)
	{
		var fileName = Path.GetFileName(privateKey.FilePath);

		Message = "Enter passphrase for private key ";

		Message += string.IsNullOrWhiteSpace(privateKey.Name)
			? fileName
			: $"{privateKey.Name}, file name: {fileName}";
	}

	#endregion

	#region Commands

	[RelayCommand]
	private void TogglePasswordVisibility()
	{
		IsPasswordVisible = !IsPasswordVisible;
	}

	[RelayCommand]
	private void ReturnResult()
	{
		this.ReturnResultFromDialogWindow(Password ?? string.Empty);
	}

	[RelayCommand]
	private void Cancel()
	{
		this.ReturnResultFromDialogWindow(null);
	}

	[RelayCommand]
	public void PasswordKeyDown(KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			this.ReturnResultFromDialogWindow(Password ?? string.Empty);
		}
	}

	#endregion
}
