using System;
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
	}
}
