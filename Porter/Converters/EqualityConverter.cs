using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Porter.Converters;

public class EqualityConverter : IMultiValueConverter
{
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count == 2)
			return Equals(values[0], values[1]);

		return false;
	}
}
