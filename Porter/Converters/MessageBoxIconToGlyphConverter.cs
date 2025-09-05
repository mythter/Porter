using System;
using System.Globalization;

using Avalonia.Data.Converters;

using Porter.Enums;

namespace Porter.Converters
{
	internal class MessageBoxIconToGlyphConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is MessageBoxIcon icon)
			{
				return icon switch
				{
					MessageBoxIcon.Info => "\uE2CE",
					MessageBoxIcon.Warning => "\uE4E0",
					MessageBoxIcon.Error => "\uE4E4",
					MessageBoxIcon.Question => "\uE3E8",
					_ => "\uE2CE"
				};
			}
			return "\uE4E0";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
