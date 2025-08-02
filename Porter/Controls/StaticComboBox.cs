using Avalonia;
using Avalonia.Controls;

namespace Porter.Controls
{
	public class StaticComboBox : ComboBox
	{
		public static readonly StyledProperty<object?> PlaceholderProperty =
			AvaloniaProperty.Register<StaticComboBox, object?>(nameof(Placeholder));

		public object? Placeholder
		{
			get => GetValue(PlaceholderProperty);
			set => SetValue(PlaceholderProperty, value);
		}
	}
}
