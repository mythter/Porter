using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Porter.ControlModels;

namespace Porter.Controls;

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
