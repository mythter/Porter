using System;
using System.Collections.Generic;
using System.Linq;

namespace Porter.Extensions;

public static class CollectionExtensions
{
	/// <summary>
	/// Removes the first element matching <paramref name="predicate"/>. Materializes the
	/// search to a local before mutating, so it is safe even if the underlying collection
	/// raises change notifications during enumeration.
	/// </summary>
	public static bool Remove<T>(this ICollection<T> col, Predicate<T> predicate)
	{
		var match = col.FirstOrDefault(x => predicate(x));

		if (match is null)
			return false;

		return col.Remove(match);
	}
}
