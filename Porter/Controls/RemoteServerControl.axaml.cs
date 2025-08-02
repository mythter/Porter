using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Porter.ControlModels;

namespace Porter.Controls;

public partial class RemoteServerControl : UserControl
{
	public static readonly StyledProperty<RemoteServerControlModel> ModelProperty =
		AvaloniaProperty.Register<RemoteServerControl, RemoteServerControlModel>(nameof(Model));

	public RemoteServerControlModel Model
	{
		get => GetValue(ModelProperty);
		set => SetValue(ModelProperty, value);
	}

	public RemoteServerControl()
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