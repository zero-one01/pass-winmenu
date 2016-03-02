using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PassWinmenu
{
	internal class ConfigManager
	{
		public static Config Config { get; private set; } = new Config();

		public enum LoadResult
		{
			Success,
			Failure,
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
					return LoadResult.Failure;
				}

				return LoadResult.NewFileCreated;
			}

			var deserialiser = new Deserializer(namingConvention: new HyphenatedNamingConvention(), ignoreUnmatched: false);
			using (var reader = File.OpenText(fileName))
			{
				Config = deserialiser.Deserialize<Config>(reader);
			}
			return LoadResult.Success;
		}
	}
}
