using System;

namespace Porter.Services.Interfaces;

/// <summary>
/// Single sink for unrecoverable error reports. Replaces the previous trio of
/// <c>crash.log</c>/<c>fatal.log</c>/<c>task.log</c> files with a single append-only log
/// that prefixes each entry with a UTC timestamp and a short category tag.
/// </summary>
public interface ICrashLogger
{
	void Log(string category, Exception exception);

	void Log(string category, string message);
}
