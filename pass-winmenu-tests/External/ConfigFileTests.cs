using System.IO;

using PassWinmenu.Utilities;

using Xunit;

using YamlDotNet.Serialization;

namespace PassWinmenu.External
{
		public class ConfigFileTests
	{
		private const string Category = "External: Configuration File";

		[Fact, TestCategory(Category)]
		public void ConfigFile_IsValidYaml()
		{
			var des = new DeserializerBuilder()
				.Build();

			// Will throw an exception if the file does not contain valid YAML
			des.Deserialize(File.OpenText(@"..\..\..\pass-winmenu\embedded\default-config.yaml"));
		}
	}
}
