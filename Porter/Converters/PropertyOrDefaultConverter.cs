using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Porter.Converters
{
	public class PropertyOrDefaultConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (parameter is not string paramStr)
				return "";

			// format: "PropertyName|DefaultText"
			var parts = paramStr.Split('|', 2);

			var propertyName = parts[0];
			var defaultValue = parts.Length > 1 ? parts[1] : "";

			if (value == null)
				return defaultValue;

			var prop = value.GetType().GetProperty(propertyName);
			if (prop == null)
				return defaultValue;

			var propValue = prop.GetValue(value);
			var str = propValue?.ToString();

			return string.IsNullOrWhiteSpace(str) ? defaultValue : str;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
