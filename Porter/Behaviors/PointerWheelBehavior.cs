using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Porter.Behaviors;

/// <summary>
/// Attached behavior that suppresses pointer-wheel events on a control during the tunnel routing
/// phase (typical use: prevent ComboBoxes from changing selection on accidental scroll).
/// </summary>
public static class PointerWheelBehavior
{
	public static readonly AttachedProperty<bool> SuppressProperty =
		AvaloniaProperty.RegisterAttached<Control, bool>(
			"Suppress",
			typeof(PointerWheelBehavior));

	static PointerWheelBehavior()
	{
		SuppressProperty.Changed.AddClassHandler<Control>(OnSuppressChanged);
	}

	public static void SetSuppress(Control control, bool value) => control.SetValue(SuppressProperty, value);

	public static bool GetSuppress(Control control) => control.GetValue(SuppressProperty);

	private static void OnSuppressChanged(Control control, AvaloniaPropertyChangedEventArgs args)
	{
		control.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);

		if (args.NewValue is true)
		{
			control.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
		}
	}

	private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		e.Handled = true;
	}
}
