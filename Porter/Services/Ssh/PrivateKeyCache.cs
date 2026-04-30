using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Renci.SshNet;
using Renci.SshNet.Common;

namespace Porter.Services.Ssh;

/// <summary>
/// Thread-safe singleton cache of decrypted <see cref="PrivateKeyFile"/> instances. Keeps decrypted
/// keys alive for the lifetime of the application so the user is not asked for a passphrase repeatedly
/// when stopping/restarting tunnels.
/// </summary>
public sealed class PrivateKeyCache : IPrivateKeyCache
{
	private readonly ConcurrentDictionary<string, PrivateKeyFile> _keys = new(StringComparer.OrdinalIgnoreCase);

	private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

	private bool _disposed;

	public async Task<PrivateKeyFile?> GetAsync(string filePath, Func<Task<string?>>? promptPassphrase)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (_keys.TryGetValue(filePath, out var cached))
			return cached;

		var semaphore = _locks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

		await semaphore.WaitAsync().ConfigureAwait(false);
		try
		{
			// Double-checked: another caller may have populated the cache while we were waiting.
			if (_keys.TryGetValue(filePath, out cached))
				return cached;

			var loaded = await LoadAsync(filePath, promptPassphrase).ConfigureAwait(false);
			if (loaded is null)
				return null;

			_keys[filePath] = loaded;
			return loaded;
		}
		finally
		{
			semaphore.Release();
		}
	}

	public void Invalidate(string filePath)
	{
		if (_keys.TryRemove(filePath, out var key))
		{
			key.Dispose();
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		foreach (var key in _keys.Values)
		{
			try { key.Dispose(); }
			catch { /* best-effort cleanup on shutdown */ }
		}
		_keys.Clear();

		foreach (var semaphore in _locks.Values)
		{
			semaphore.Dispose();
		}
		_locks.Clear();
	}

	private static async Task<PrivateKeyFile?> LoadAsync(string filePath, Func<Task<string?>>? promptPassphrase)
	{
		string? passphrase = null;

		while (true)
		{
			try
			{
				return passphrase is null
					? new PrivateKeyFile(filePath)
					: new PrivateKeyFile(filePath, passphrase);
			}
			catch (SshException ex) when (ex is SshPassPhraseNullOrEmptyException ||
										  // Renci.SshNet throws a generic SshException for encrypted PuTTY keys.
										  ex.Message.Contains("putty", StringComparison.OrdinalIgnoreCase))
			{
				if (promptPassphrase is null)
					return null;

				passphrase = await promptPassphrase().ConfigureAwait(false);
				if (passphrase is null)
					return null;
			}
			catch
			{
				return null;
			}
		}
	}
}
