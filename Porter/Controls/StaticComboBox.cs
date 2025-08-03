using Avalonia;
using Avalonia.Media;

namespace Porter.Controls
{
	public class StaticComboBox : CustomComboBox
	{
		public static readonly StyledProperty<object?> PlaceholderProperty =
			AvaloniaProperty.Register<StaticComboBox, object?>(nameof(Placeholder));

		public object? Placeholder
		{
			get => GetValue(PlaceholderProperty);
			set => SetValue(PlaceholderProperty, value);
		}

		public static readonly StyledProperty<IBrush?> DefaultTitleForegroundProperty =
			AvaloniaProperty.Register<StaticComboBox, IBrush?>(nameof(DefaultTitleForeground));

		public IBrush? DefaultTitleForeground
		{
			get => GetValue(DefaultTitleForegroundProperty);
			set => SetValue(DefaultTitleForegroundProperty, value);
		}
	}
}
