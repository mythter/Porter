using Avalonia.Controls;

namespace Porter.Views;

public partial class PrivateKeyPasswordView : UserControl
{
	public PrivateKeyPasswordView()
	{
		InitializeComponent();

		PasswordTextBox.AttachedToVisualTree += (_, _) =>
		{
			PasswordTextBox.Focus();
		};
	}
}
