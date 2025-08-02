using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Porter.ControlModels;

namespace Porter.Controls;

public partial class SshServerControl : UserControl
{
	public static readonly StyledProperty<SshServerControlModel> ModelProperty =
		AvaloniaProperty.Register<SshServerControl, SshServerControlModel>(nameof(Model));

	public SshServerControlModel Model
	{
		get => GetValue(ModelProperty);
		set => SetValue(ModelProperty, value);
	}

	public SshServerControl()
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

	private void OnUserLostFocus(object? sender, RoutedEventArgs e)
	{
		Model?.OnUserLostFocus(sender, e);
	}

	private void OnUserKeyDown(object? sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model?.OnUserKeyDown(sender, e, topLevel);
	}

	private void OnHostLostFocus(object? sender, RoutedEventArgs e)
	{
		Model?.OnHostLostFocus(sender, e);
	}

	private void OnHostKeyDown(object? sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model?.OnHostKeyDown(sender, e, topLevel);
	}

	private void OnPortLostFocus(object? sender, RoutedEventArgs e)
	{
		Model?.OnPortLostFocus(sender, e);
	}

	private void OnPortKeyDown(object? sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model?.OnPortKeyDown(sender, e, topLevel);
	}
}