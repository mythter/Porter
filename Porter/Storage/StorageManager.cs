using System;
using System.Collections.Generic;
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

		private static AppData _data = LoadData();
		private static ObservableCollection<SshServer> _sshServers = new(_data.SshServers);
		private static ObservableCollection<RemoteServer> _remoteServers = new(_data.RemoteServers);
		private static ObservableCollection<PrivateKey> _privateKeys = new(_data.PrivateKeys);
		private static ObservableCollection<SshTunnel> _sshTunnels = new(_data.SshTunnels);

		public static Settings Settings => _data.Settings ??= new();

		public static WindowSettings WindowSettings => _data.WindowSettings ??= new();

		public static ObservableCollection<SshServer> SshServers => _sshServers;

		public static ObservableCollection<RemoteServer> RemoteServers => _remoteServers;

		public static ObservableCollection<PrivateKey> PrivateKeys => _privateKeys;

		public static ObservableCollection<SshTunnel> SshTunnels => _sshTunnels;

		static StorageManager()
		{
			_sshServers.CollectionChanged += (s, e) => SaveSshServers();
			_remoteServers.CollectionChanged += (s, e) => SaveRemoteServers();
			_privateKeys.CollectionChanged += (s, e) => SavePrivateKeys();
			_sshTunnels.CollectionChanged += (s, e) => SaveSshTunnels();
		}

		public static void SaveSshServers(List<SshServer>? sshServers = null)
		{
			_data.SshServers = sshServers ?? [.. _sshServers];
			Save();
		}

		public static void SaveRemoteServers(List<RemoteServer>? remoteServers = null)
		{
			_data.RemoteServers = remoteServers ?? [.. _remoteServers];
			Save();
		}

		public static void SavePrivateKeys(List<PrivateKey>? privateKeys = null)
		{
			_data.PrivateKeys = privateKeys ?? [.. _privateKeys];
			Save();
		}

		public static void SaveSshTunnels(List<SshTunnel>? sshTunnels = null)
		{
			_data.SshTunnels = sshTunnels ?? [.. _sshTunnels];
			Save();
		}

		public static void SaveSettings(Settings? settings = null)
		{
			_data.Settings = settings ?? _data.Settings;
			Save();
		}

		public static void SaveWindowSettings(WindowSettings? windowSettings = null)
		{
			_data.WindowSettings = windowSettings ?? _data.WindowSettings;
			Save();
		}

		public static void Save()
		{
			SaveData(_data);
		}

		private static AppData LoadData()
		{
			if (!File.Exists(_filePath))
			{
				var newData = new AppData();
				SaveData(newData);
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

		private static void SaveData(AppData data)
		{
			string json = JsonSerializer.Serialize(data, _serializerOptions);
			File.WriteAllText(_filePath, json);
		}
	}
}
