using System;
using System.IO;
using System.Text.Json;

using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.Services;

public class AppDataProvider : IAppDataProvider<AppData>
{
	#region Private Fields

	private readonly ICrashLogger? _logger;

	#endregion

	#region Public Properties

	public AppData Value { get; set; } = new();

	public string FilePath { get; } = GetDefaultSettingsPath();

	#endregion

	#region Constructors

	public AppDataProvider() : this(null) { }

	public AppDataProvider(ICrashLogger? logger)
	{
		_logger = logger;
		Load();
	}

	#endregion

	#region Public Methods

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

	#endregion

	#region Private Methods

	private static string GetDefaultSettingsPath()
	{
#if DEBUG
		// Development mode: store settings next to the executable for easy access and debugging.
		// This makes it simple to inspect/modify settings during development.
		var appFolder = AppContext.BaseDirectory;
#else
		// Publish mode: use ApplicationData (roaming) for user-specific settings that should:
		// - Not require admin rights
		// - Persist across app updates
		// - Be isolated per user
		// - Sync in domain roaming profiles (Windows)
		//
		// Windows: %APPDATA%\Porter\settings.json (e.g., C:\Users\YourName\AppData\Roaming\Porter)
		// Linux/macOS: ~/.config/Porter/settings.json
		var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var appFolder = Path.Combine(appDataFolder, "Porter");

		// Ensure the directory exists
		Directory.CreateDirectory(appFolder);
#endif

		return Path.Combine(appFolder, "settings.json");
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

	#endregion
}
