using System;
using System.IO;
using System.Text.Json;

using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.Services;

public class AppDataProvider : IAppDataProvider<AppData>
{
	private readonly ICrashLogger? _logger;

	public AppData Value { get; set; } = new();

	public string FilePath { get; } = Path.Combine(AppContext.BaseDirectory, "settings.json");

	public AppDataProvider() : this(null) { }

	public AppDataProvider(ICrashLogger? logger)
	{
		_logger = logger;
		Load();
	}

	public AppData Load()
	{
		return Load(FilePath);
	}

	public AppData Load(string path)
	{
		if (!File.Exists(path))
		{
			Value = GetDefault();
			return Value;
		}

		try
		{
			string json = File.ReadAllText(path);
			Value = JsonSerializer.Deserialize(json, PorterJsonContext.Default.AppData) ?? GetDefault();
		}
		catch (Exception ex)
		{
			// Settings file is unreadable — preserve the corrupt copy as a backup so the user
			// can recover any tunnel definitions manually before we reset to defaults.
			TryBackup(path);
			_logger?.Log("AppData.Load", ex);
			Value = GetDefault();
		}

		return Value;
	}

	public void Save()
	{
		Save(FilePath);
	}

	public void Save(string path)
	{
		try
		{
			string json = JsonSerializer.Serialize(Value, PorterJsonContext.Default.AppData);
			File.WriteAllText(path, json);
		}
		catch (Exception ex)
		{
			_logger?.Log("AppData.Save", ex);
		}
	}

	private static void TryBackup(string path)
	{
		try
		{
			var backup = path + ".bak";
			File.Copy(path, backup, overwrite: true);
		}
		catch
		{
			// Best-effort — if we can't make a backup, we still proceed with defaults.
		}
	}

	private static AppData GetDefault() => new();
}
