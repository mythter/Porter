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

	public string? Password { get; set; }

	public string Message { get; set; }

	#endregion

	#region Constructors

	public PrivateKeyPasswordViewModel(PrivateKey privateKey)
	{
		Message = "Enter passphrase for private key ";
		Message += string.IsNullOrWhiteSpace(privateKey.Name) switch
		{
			true => Path.GetFileName(privateKey.FilePath),
			false => $"{privateKey.Name}, file name: {Path.GetFileName(privateKey.FilePath)}",
		};
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
