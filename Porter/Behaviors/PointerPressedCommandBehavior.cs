using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Behaviors;

/// <summary>
/// Attached behavior that executes a command when pointer is pressed on the control.
/// Native AOT-compatible replacement for EventTriggerBehavior with PointerPressed event.
/// </summary>
public static class PointerPressedCommandBehavior
{
	public static readonly AttachedProperty<ICommand?> CommandProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?>(
			"Command",
			typeof(PointerPressedCommandBehavior));

	public static readonly AttachedProperty<object?> CommandParameterProperty =
		AvaloniaProperty.RegisterAttached<Control, object?>(
			"CommandParameter",
			typeof(PointerPressedCommandBehavior));

	static PointerPressedCommandBehavior()
	{
		CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
	}

	public static void SetCommand(Control control, ICommand? value) => control.SetValue(CommandProperty, value);

	public static ICommand? GetCommand(Control control) => control.GetValue(CommandProperty);

	public static void SetCommandParameter(Control control, object? value) => control.SetValue(CommandParameterProperty, value);

	public static object? GetCommandParameter(Control control) => control.GetValue(CommandParameterProperty);

	private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs args)
	{
		control.PointerPressed -= OnPointerPressed;

		if (args.NewValue is not null)
		{
			control.PointerPressed += OnPointerPressed;
		}
	}

	private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control control)
			return;

		var command = GetCommand(control);
		if (command is null)
			return;

		var parameter = GetCommandParameter(control);

		if (command.CanExecute(parameter))
		{
			command.Execute(parameter);
		}
	}
}
