using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Porter.Behaviors;

/// <summary>
/// Attached property that focuses the control when it is first attached to the visual tree.
/// Replaces ad-hoc <c>AttachedToVisualTree</c> code-behind handlers.
/// </summary>
public static class AutoFocusBehavior
{
	public static readonly AttachedProperty<bool> EnabledProperty =
		AvaloniaProperty.RegisterAttached<InputElement, bool>("Enabled", typeof(AutoFocusBehavior));

	public static bool GetEnabled(InputElement element) => element.GetValue(EnabledProperty);

	public static void SetEnabled(InputElement element, bool value) => element.SetValue(EnabledProperty, value);

	static AutoFocusBehavior()
	{
		EnabledProperty.Changed.AddClassHandler<InputElement>(OnEnabledChanged);
	}

	private static void OnEnabledChanged(InputElement element, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.NewValue is true)
			element.AttachedToVisualTree += OnAttached;
		else
			element.AttachedToVisualTree -= OnAttached;
	}

	private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
	{
		if (sender is InputElement element)
			element.Focus();
	}
}
