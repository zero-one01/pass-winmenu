using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PassWinmenu.Configuration
{
	internal class ConfigManager
	{
		public static Config Config { get; private set; } = new Config();

		public enum LoadResult
		{
			Success,
			NeedsUpgrade,
			FileCreationFailure,
			NewFileCreated
		}

		public static LoadResult Load(string fileName)
		{
			if (!File.Exists(fileName))
			{
				try
				{
					using (var defaultConfig = EmbeddedResources.DefaultConfig)
					using (var configFile = File.Create(fileName))
					{
						defaultConfig.CopyTo(configFile);
					}
				}
				catch (Exception e) when (e is FileNotFoundException || e is FileLoadException || e is IOException)
				{
					return LoadResult.FileCreationFailure;
				}

				return LoadResult.NewFileCreated;
			}

			var deserialiser = new Deserializer(namingConvention: new HyphenatedNamingConvention());

			using (var reader = File.OpenText(fileName))
			{
				var versionCheck = deserialiser.Deserialize<Dictionary<string, object>>(reader);

				if (!versionCheck.ContainsKey("config-version"))
				{
					return LoadResult.NeedsUpgrade;
				}
				if (versionCheck["config-version"] as string != Program.LastConfigVersion) return LoadResult.NeedsUpgrade;
			}

			using (var reader = File.OpenText(fileName))
			{
				Config = deserialiser.Deserialize<Config>(reader);
			}

			return LoadResult.Success;
		}

		public static void Reload(string fileName)
		{
			try
			{
				var deserialiser = new Deserializer(namingConvention: new HyphenatedNamingConvention());
				using (var reader = File.OpenText(fileName))
				{
					var newConfig = deserialiser.Deserialize<Config>(reader);
					Config = newConfig;
				}
			}
			catch (Exception)
			{
				// No need to do anything, we can simply continue using the old configuration.
			}
		}

		public static string Backup(string fileName)
		{
			var extension = Path.GetExtension(fileName);
			var name = Path.GetFileNameWithoutExtension(fileName);
			var directory = Path.GetDirectoryName(fileName);

			// Find an unused name to which we can rename the old configuration file.
			var root = string.IsNullOrEmpty(directory) ? name : Path.Combine(directory, name);
			var newFileName = $"{root}-backup{extension}";
			var counter = 2;
			while (File.Exists(newFileName))
			{
				newFileName =$"{root}-backup-{counter}{extension}";
			}

			File.Move(fileName, newFileName);
		
			using (var defaultConfig = EmbeddedResources.DefaultConfig)
			using (var configFile = File.Create(fileName))
			{
				defaultConfig.CopyTo(configFile);
			}
			return newFileName;
		}
	}
}
