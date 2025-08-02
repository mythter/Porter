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
	}
}
