using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using Porter.Enums;

namespace Porter.Converters
{
	public class MessageBoxIconToColorConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is MessageBoxIcon icon)
			{
				return icon switch
				{
					MessageBoxIcon.Info => new SolidColorBrush(Color.Parse("#cabcf6")),
					MessageBoxIcon.Warning => new SolidColorBrush(Color.Parse("#ffd800")),
					MessageBoxIcon.Error => new SolidColorBrush(Color.Parse("#c01b1b")),
					MessageBoxIcon.Question => new SolidColorBrush(Color.Parse("#2f98d8")),
					_ => Brushes.White
				};
			}
			return Brushes.Black;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
