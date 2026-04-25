using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Porter.Views.Controls;

public partial class SshTunnelControl : UserControl
{
	public SshTunnelControl()
	{
		InitializeComponent();

		SshServersComboBox.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanging, RoutingStrategies.Tunnel);
		PrivateKeysComboBox.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanging, RoutingStrategies.Tunnel);
		RemoteServersComboBox.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanging, RoutingStrategies.Tunnel);
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			TopLevel.GetTopLevel(this)?.Focus();
			e.Handled = true;
		}
	}

	private static void OnPointerWheelChanging(object? sender, PointerWheelEventArgs e)
	{
		e.Handled = true;
	}
}
