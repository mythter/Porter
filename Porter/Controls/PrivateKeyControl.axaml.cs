using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Controls;

public partial class PrivateKeyControl : UserControl
{
	public PrivateKeyControl()
	{
		InitializeComponent();
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			TopLevel.GetTopLevel(this)?.Focus();
			e.Handled = true;
		}
	}
}
