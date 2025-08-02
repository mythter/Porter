using System.Collections.Generic;

namespace Porter.Models
{
	public class AppData
	{
		public Settings Settings { get; set; }

		public List<SshServer> SshServers { get; set; } = [];

		public List<RemoteServer> RemoteServers { get; set; } = [];

		public List<PrivateKey> PrivateKeys { get; set; } = [];

		public List<SshTunnel> SshTunnels { get; set; } = [];
	}
}
