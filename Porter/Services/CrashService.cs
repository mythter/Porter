using System;
using System.IO;
using System.Text.Json;

using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.Services;

/// <summary>
/// Persists crash metadata to a marker JSON file used to (a) detect a previous-run crash on
/// startup so the UI can show the recovery dialog, and (b) protect against rapid restart loops.
/// Diagnostic text logging now lives in <see cref="ICrashLogger"/>.
/// </summary>
public static class CrashService
{
	#region Private Fields

	private static readonly string _crashFilePath = Path.Combine(AppContext.BaseDirectory, "crash.json");

	#endregion

	#region Public Methods

	public static bool SetCrashData(Exception ex)
	{
		try
		{
			var data = new CrashData(
				CrashDate: DateTimeOffset.UtcNow,
				ErrorMessage: ex.Message,
				StackTrace: ex.StackTrace ?? string.Empty,
				// ex.TargetSite requires preserved reflection metadata (IL2026 under trimming/AOT).
				// ex.Source carries the originating assembly name, which is sufficient for diagnostics.
				Source: ex.Source ?? ex.GetType().FullName ?? string.Empty);

			File.WriteAllText(_crashFilePath, JsonSerializer.Serialize(data, AppJsonContext.Default.CrashData));

			return true;
		}
		catch
		{
			// Failure to persist crash metadata is itself unrecoverable — give up silently.
		}

		return false;
	}

	public static void ClearCrashData()
	{
		try
		{
			if (File.Exists(_crashFilePath))
			{
				File.Delete(_crashFilePath);
			}
		}
		catch
		{
			// Ignored — leftover marker is harmless.
		}
	}

	public static CrashData? GetCrashData()
	{
		try
		{
			if (File.Exists(_crashFilePath))
			{
				return JsonSerializer.Deserialize(File.ReadAllText(_crashFilePath), AppJsonContext.Default.CrashData);
			}
		}
		catch
		{
			ClearCrashData();
		}

		return null;
	}

	public static void RemoveCrashData()
	{
		ClearCrashData();
	}

	#endregion
}
