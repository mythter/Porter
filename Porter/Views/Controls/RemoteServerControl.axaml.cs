using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Views.Controls;

public partial class RemoteServerControl : UserControl
{
	public RemoteServerControl()
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
