using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Porter.Models;

namespace Porter.ViewModels
{
	public partial class PrivateKeyPasswordViewModel : ViewModelBase
	{
		[ObservableProperty]
		private bool isPasswordVisible;

		public char? PasswordChar => IsPasswordVisible ? null : '•';

		public string Message { get; set; }

		public PrivateKeyPasswordViewModel(PrivateKey privateKey)
		{
			Message = "Enter passphrase for private key ";
			Message += string.IsNullOrWhiteSpace(privateKey.Name) switch
			{
				true => Path.GetFileName(privateKey.FilePath),
				false => $"{privateKey.Name}, file name: {Path.GetFileName(privateKey.FilePath)}",
			};
		}

		partial void OnIsPasswordVisibleChanged(bool oldValue, bool newValue)
		{
			OnPropertyChanged(nameof(PasswordChar));
		}

		[RelayCommand]
		public void TogglePasswordVisibility()
		{
			IsPasswordVisible = !IsPasswordVisible;
		}
	}
}
