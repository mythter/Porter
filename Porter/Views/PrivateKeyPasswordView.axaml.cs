using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Porter.Views;

public partial class PrivateKeyPasswordView : UserControl
{
	public Window? Owner => this.GetVisualRoot() as Window;

	public PrivateKeyPasswordView()
	{
		InitializeComponent();

		PasswordTextBox.AttachedToVisualTree += (_, _) =>
		{
			PasswordTextBox.Focus();
		};
	}

	private void OnOkClick(object? sender, RoutedEventArgs e)
	{
		Owner?.Close(PasswordTextBox.Text ?? string.Empty);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Owner?.Close(null);
	}

	private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			Owner?.Close(PasswordTextBox.Text ?? string.Empty);
		}
	}
}