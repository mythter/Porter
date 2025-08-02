using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Porter.ControlModels;

namespace Porter.Controls;

public partial class SshTunnelControl : UserControl
{
	public static readonly StyledProperty<SshTunnelControlModel> ModelProperty =
		AvaloniaProperty.Register<SshTunnelControl, SshTunnelControlModel>(nameof(Model));

	public SshTunnelControlModel Model
	{
		get => GetValue(ModelProperty);
		set => SetValue(ModelProperty, value);
	}

	public SshTunnelControl()
	{
		InitializeComponent();

		PropertyChanged += (_, e) =>
		{
			if (e.Property == ModelProperty)
			{
				DataContext = Model;
			}
		};
	}

	private void OnNameLostFocus(object? sender, RoutedEventArgs e)
	{
		Model?.OnNameLostFocus(sender, e);
	}

	private void OnNameKeyDown(object? sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model?.OnNameKeyDown(sender, e, topLevel);
	}

	private void OnLocalPortLostFocus(object? sender, RoutedEventArgs e)
	{
		Model?.OnLocalPortLostFocus(sender, e);
	}

	private void OnLocalPortKeyDown(object? sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model?.OnLocalPortKeyDown(sender, e, topLevel);
	}

	private void OnSshServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		Model?.OnSshServerSelectionChanged(sender, e);
	}

	private void OnPrivateKeySelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		Model?.OnPrivateKeySelectionChanged(sender, e);
	}

	private void OnRemoteServerSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		Model?.OnRemoteServerSelectionChanged(sender, e);
	}
}