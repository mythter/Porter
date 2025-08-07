using System;

namespace Porter.Models
{
	public class RemoteServer
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string? Name { get; set; }

		public string? Host { get; set; }

		public int? Port { get; set; }

		public override bool Equals(object? obj)
		{
			if (obj is not RemoteServer other)
				return false;

			return string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
				&& Port == other.Port;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				Host?.ToLowerInvariant(),
				Port
			);
		}
	}
}
