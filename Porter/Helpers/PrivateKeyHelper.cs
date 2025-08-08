using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using Renci.SshNet;

using Renci.SshNet.Common;

namespace Porter.Helpers
{
	public static class PrivateKeyHelper
	{
		// private key path - passphrase
		private static readonly ConcurrentDictionary<string, string?> _passphraseCache = new();

		public static async Task<PrivateKeyFile> GetPrivateKeyFile(string privateKeyPath, Func<Task<string>> promptPassphrase)
		{
			if (!_passphraseCache.TryGetValue(privateKeyPath, out var passphrase))
			{
				try
				{
					await using var stream = new FileStream(privateKeyPath, FileMode.Open, FileAccess.Read);
					var keyFile = new PrivateKeyFile(stream);
					_passphraseCache[privateKeyPath] = null;
					return keyFile;
				}
				catch (SshException ex) when (ex.Message.Contains("encrypted") || ex.Message.Contains("passphrase"))
				{
					passphrase = await promptPassphrase();
					using var stream = new FileStream(privateKeyPath, FileMode.Open, FileAccess.Read);
					var keyFile = new PrivateKeyFile(stream, passphrase);
					_passphraseCache[privateKeyPath] = passphrase;
					return keyFile;
				}
			}

			await using var stream2 = new FileStream(privateKeyPath, FileMode.Open, FileAccess.Read);

			return string.IsNullOrEmpty(passphrase)
				? new PrivateKeyFile(stream2)
				: new PrivateKeyFile(stream2, passphrase);
		}
	}
}
