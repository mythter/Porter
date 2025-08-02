using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Porter.ControlModels;

namespace Porter.Controls;

public partial class PrivateKeyControl : UserControl
{
	public static readonly StyledProperty<PrivateKeyControlModel> ModelProperty =
		AvaloniaProperty.Register<PrivateKeyControl, PrivateKeyControlModel>(nameof(Model));

	public PrivateKeyControlModel Model
	{
		get => GetValue(ModelProperty);
		set => SetValue(ModelProperty, value);
	}

	public PrivateKeyControl()
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

	private async void OnFilePathClick(object? sender, PointerPressedEventArgs e)
	{
		await Model.OnFilePathClick(sender, e);
	}

	private void OnFileNameLostFocus(object sender, RoutedEventArgs e)
	{
		Model.OnFileNameLostFocus(sender, e);
	}

	private void OnFileNameKeyDown(object sender, KeyEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		Model.OnFileNameKeyDown(sender, e, topLevel);
	}
}
