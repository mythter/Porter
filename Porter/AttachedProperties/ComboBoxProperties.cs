using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Porter.AttachedProperties
{
	public static class ComboBoxProperties
	{
		public static readonly AttachedProperty<TextTrimming> TextTrimmingProperty =
			AvaloniaProperty.RegisterAttached<ComboBox, TextTrimming>(
				"TextTrimming",
				typeof(ComboBoxProperties),
				TextTrimming.None);

		public static void SetTextTrimming(AvaloniaObject element, TextTrimming value)
			=> element.SetValue(TextTrimmingProperty, value);

		public static TextTrimming GetTextTrimming(AvaloniaObject element)
			=> element.GetValue(TextTrimmingProperty);
	}
}
