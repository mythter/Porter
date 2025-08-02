using System;

namespace Porter.Models
{
	public class RemoteServer
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string? Name { get; set; }

		public string? Host { get; set; }

		public int? Port { get; set; }
	}
}
