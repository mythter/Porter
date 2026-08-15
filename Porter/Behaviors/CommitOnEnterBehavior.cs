using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Behaviors;

/// <summary>
/// Attached behavior: when set to <c>true</c>, pressing Enter on the control moves focus to the
/// nearest top-level (commits any pending text-input binding).
/// </summary>
public static class CommitOnEnterBehavior
{
	public static readonly AttachedProperty<bool> EnabledProperty =
		AvaloniaProperty.RegisterAttached<Control, bool>(
			"Enabled",
			typeof(CommitOnEnterBehavior));

	static CommitOnEnterBehavior()
	{
		EnabledProperty.Changed.AddClassHandler<Control>(OnEnabledChanged);
	}

	public static void SetEnabled(Control control, bool value) => control.SetValue(EnabledProperty, value);

	public static bool GetEnabled(Control control) => control.GetValue(EnabledProperty);

	private static void OnEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
	{
		control.KeyDown -= OnKeyDown;

		if (args.NewValue is true)
		{
			control.KeyDown += OnKeyDown;
		}
	}

	private static void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter || sender is not Control control)
			return;

		TopLevel.GetTopLevel(control)?.Focus();
		e.Handled = true;
	}
}
