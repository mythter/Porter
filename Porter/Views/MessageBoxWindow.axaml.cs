using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Porter.Views;

public partial class MessageBoxWindow : Window
{
	public MessageBoxWindow()
	{
		InitializeComponent();
	}

	private void OnOkClick(object? sender, RoutedEventArgs e)
	{
		CloseWindow();
	}

	private void OnCloseClick(object? sender, RoutedEventArgs e)
	{
		CloseWindow();
	}

	private void OnHeaderPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
	{
		if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			BeginMoveDrag(e);
		}
	}

	private void CloseWindow()
	{
		(this.GetVisualRoot() as Window)?.Close();
	}
}