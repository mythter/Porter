using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

using Porter.Models;

namespace Porter.Storage
{
	public static class StorageManager
	{
		private static readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

		private static readonly JsonSerializerOptions _serializerOptions = new()
		{
			WriteIndented = true
		};

		private static AppData _data = null!;
		private static ObservableCollection<SshServer> _sshServers = null!;
		private static ObservableCollection<RemoteServer> _remoteServers = null!;
		private static ObservableCollection<PrivateKey> _privateKeys= null!;
		private static ObservableCollection<SshTunnel> _sshTunnels= null!;

		public static AppData AppData => _data;

		public static AppSettings Settings => _data.Settings ??= new();

		public static WindowSettings WindowSettings => _data.WindowSettings ??= new();

		public static ObservableCollection<SshServer> SshServers => _sshServers;

		public static ObservableCollection<RemoteServer> RemoteServers => _remoteServers;

		public static ObservableCollection<PrivateKey> PrivateKeys => _privateKeys;

		public static ObservableCollection<SshTunnel> SshTunnels => _sshTunnels;

		static StorageManager()
		{
			InitData();
		}

		public static void SaveSettings(AppSettings? settings = null)
		{
			_data.Settings = settings ?? _data.Settings;
			Save();
		}

		public static void Save()
		{
			SaveData(_data, _filePath);
		}

		public static void Export(string filePath)
		{
			SaveData(_data, filePath);
		}

		public static bool Import(string filePath)
		{
			AppData? data;

			try
			{
				var json = File.ReadAllText(filePath);
				data = JsonSerializer.Deserialize<AppData>(json, _serializerOptions);
			}
			catch
			{
				return false;
			}

			if (data?.Settings is not null)
			{
				SaveData(data, _filePath);
				InitData();
				return true;
			}

			return false;
		}

		private static void InitData()
		{
			_data = LoadData();

			_sshServers = new(_data.SshServers);
			_remoteServers = new(_data.RemoteServers);
			_privateKeys = new(_data.PrivateKeys);
			_sshTunnels = new(_data.SshTunnels);
		}

		private static AppData LoadData()
		{
			if (!File.Exists(_filePath))
			{
				var newData = new AppData();
				SaveData(newData, _filePath);
				return newData;
			}

			try
			{
				string json = File.ReadAllText(_filePath);
				return JsonSerializer.Deserialize<AppData>(json, _serializerOptions) ?? new AppData();
			}
			catch
			{
				return new AppData();
			}
		}

		private static void SaveData(AppData data, string filePath)
		{
			string json = JsonSerializer.Serialize(data, _serializerOptions);
			File.WriteAllText(filePath, json);
		}
	}
}
