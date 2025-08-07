using System;

namespace Porter.Models
{
	public class SshServer
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string? Name { get; set; }

		public string? User { get; set; }

		public string? Host { get; set; }

		public int? Port { get; set; }

		public override bool Equals(object? obj)
		{
			if (obj is not SshServer other)
				return false;

			return string.Equals(User, other.User, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
				&& Port == other.Port;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				User?.ToLowerInvariant(),
				Host?.ToLowerInvariant(),
				Port
			);
		}
	}
}
