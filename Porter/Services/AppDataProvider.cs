using System;
using System.IO;
using System.Text.Json;

using Porter.Models;
using Porter.Services.Interfaces;

namespace Porter.Services;

public class AppDataProvider : IAppDataProvider<AppData>
{
	public AppData Value { get; set; } = new();

	public string FilePath { get; } = Path.Combine(AppContext.BaseDirectory, "settings.json");

	private static readonly JsonSerializerOptions _serializerOptions = new()
	{
		WriteIndented = true
	};

	public AppDataProvider()
	{
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
			Value = JsonSerializer.Deserialize<AppData>(json, _serializerOptions) ?? GetDefault();
		}
		catch
		{
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
			string json = JsonSerializer.Serialize(Value, _serializerOptions);
			File.WriteAllText(path, json);
		}
		catch
		{
			// Don't crash on failure to save.
		}
	}

	private static AppData GetDefault() => new();
}
