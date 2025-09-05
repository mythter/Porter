using System;
using System.IO;
using System.Text.Json;

using Porter.Models;

namespace Porter.Services
{
	public static class CrashService
	{
		private static readonly string _crashFilePath = Path.Combine(AppContext.BaseDirectory, "crash.json");

		public static bool SetCrashData(Exception ex)
		{
			try
			{
				File.WriteAllText(_crashFilePath, JsonSerializer.Serialize(
					new CrashData(
						CrashDate: DateTimeOffset.UtcNow,
						ErrorMessage: ex.Message,
						StackTrace: ex.StackTrace ?? string.Empty,
						Source: ex.TargetSite?.ToString() ?? string.Empty))
					);

				File.WriteAllText("crash.log", ex.ToString());

				return true;
			}
			catch (Exception)
			{
				// TODO: Handle system message box or other way to inform user of crash
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
			catch (Exception)
			{
				// Ignored
			}
		}

		public static CrashData? GetCrashData()
		{
			try
			{
				if (File.Exists(_crashFilePath))
				{
					return JsonSerializer.Deserialize<CrashData>(File.ReadAllText(_crashFilePath));
				}
			}
			catch (Exception)
			{
				ClearCrashData();
			}

			return null;
		}

		public static void RemoveCrashData()
		{
			ClearCrashData();
		}
	}
}
