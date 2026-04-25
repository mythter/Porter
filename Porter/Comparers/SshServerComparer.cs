using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Porter.Models;

namespace Porter.Comparers;

public class SshServerComparer : IEqualityComparer<SshServer>
{
	public bool Equals(SshServer? x, SshServer? y)
	{
		if (ReferenceEquals(x, y))
			return true;

		if (x is null || y is null)
			return false;

		return string.Equals(x.User, y.User, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase)
			&& x.Port == y.Port;
	}

	public int GetHashCode([DisallowNull] SshServer obj)
	{
		if (obj is null)
			return 0;

		return HashCode.Combine(
			obj.User?.ToLowerInvariant(),
			obj.Host?.ToLowerInvariant(),
			obj.Port
		);
	}
}
