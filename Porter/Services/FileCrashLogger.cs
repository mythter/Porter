using System;
using System.IO;

using Porter.Services.Interfaces;

namespace Porter.Services;

/// <summary>
/// File-backed <see cref="ICrashLogger"/>. Writes to <c>crash.log</c> in the application
/// base directory; failures to write are intentionally swallowed (the logger must never
/// itself throw during a crash).
/// </summary>
public sealed class FileCrashLogger : ICrashLogger
{
	private static readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "crash.log");

	private static readonly object _writeSync = new();

	public void Log(string category, Exception exception)
	{
		Write(category, exception.ToString());
	}

	public void Log(string category, string message)
	{
		Write(category, message);
	}

	private static void Write(string category, string body)
	{
		try
		{
			var line = $"[{DateTimeOffset.UtcNow:O}] [{category}] {body}{Environment.NewLine}";
			lock (_writeSync)
			{
				File.AppendAllText(_logFilePath, line);
			}
		}
		catch
		{
			// Logger must not throw — best-effort write.
		}
	}
}
