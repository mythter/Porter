using System;

namespace Porter.Models
{
	public class SshTunnel
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string? Name { get; set; }

		public int? LocalPort { get; set; }

		public SshServer? SshServer { get; set; }

		public PrivateKey? PrivateKey { get; set; }

		public RemoteServer? RemoteServer { get; set; }
	}
}
