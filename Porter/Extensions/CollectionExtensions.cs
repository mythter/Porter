using System;
using System.Collections.Generic;

namespace Porter.Extensions;

public static class CollectionExtensions
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "Let it be")]
	public static void Remove<T>(this ICollection<T> col, Predicate<T> predicate)
	{
		foreach (var item in col)
		{
			if (predicate(item))
			{
				col.Remove(item);
				return;
			}
		}
	}
}
