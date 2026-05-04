using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Behaviors;

/// <summary>
/// Attached behavior that executes a command when a key is pressed.
/// Native AOT-compatible replacement for EventTriggerBehavior with KeyDown event.
/// </summary>
public static class KeyDownCommandBehavior
{
	public static readonly AttachedProperty<ICommand?> CommandProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?>(
			"Command",
			typeof(KeyDownCommandBehavior));

	public static readonly AttachedProperty<object?> CommandParameterProperty =
		AvaloniaProperty.RegisterAttached<Control, object?>(
			"CommandParameter",
			typeof(KeyDownCommandBehavior));

	public static readonly AttachedProperty<bool> PassEventArgsToCommandProperty =
		AvaloniaProperty.RegisterAttached<Control, bool>(
			"PassEventArgsToCommand",
			typeof(KeyDownCommandBehavior),
			defaultValue: false);

	static KeyDownCommandBehavior()
	{
		CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
	}

	public static void SetCommand(Control control, ICommand? value) => control.SetValue(CommandProperty, value);

	public static ICommand? GetCommand(Control control) => control.GetValue(CommandProperty);

	public static void SetCommandParameter(Control control, object? value) => control.SetValue(CommandParameterProperty, value);

	public static object? GetCommandParameter(Control control) => control.GetValue(CommandParameterProperty);

	public static void SetPassEventArgsToCommand(Control control, bool value) => control.SetValue(PassEventArgsToCommandProperty, value);

	public static bool GetPassEventArgsToCommand(Control control) => control.GetValue(PassEventArgsToCommandProperty);

	private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs args)
	{
		control.KeyDown -= OnKeyDown;

		if (args.NewValue is not null)
		{
			control.KeyDown += OnKeyDown;
		}
	}

	private static void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is not Control control)
			return;

		var command = GetCommand(control);
		if (command is null)
			return;

		var passEventArgs = GetPassEventArgsToCommand(control);
		var parameter = passEventArgs ? e : GetCommandParameter(control);

		if (command.CanExecute(parameter))
		{
			command.Execute(parameter);
		}
	}
}
