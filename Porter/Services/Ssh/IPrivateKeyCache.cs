using System;
using System.Threading.Tasks;

using Renci.SshNet;

namespace Porter.Services.Ssh;

public interface IPrivateKeyCache : IDisposable
{
	/// <summary>
	/// Returns a cached <see cref="PrivateKeyFile"/> for the given file path, loading and decrypting it
	/// (asking for the passphrase if necessary) the first time it is requested. Subsequent calls return
	/// the cached instance so the user is not prompted again.
	/// </summary>
	/// <param name="filePath">Absolute path to the private key file.</param>
	/// <param name="promptPassphrase">Callback invoked when the key requires a passphrase.</param>
	/// <returns>The decrypted <see cref="PrivateKeyFile"/> or <c>null</c> if the key could not be loaded.</returns>
	Task<PrivateKeyFile?> GetAsync(string filePath, Func<Task<string?>>? promptPassphrase);

	/// <summary>
	/// Removes a cached key (e.g. when the file was deleted or the user changed it).
	/// </summary>
	void Invalidate(string filePath);
}
