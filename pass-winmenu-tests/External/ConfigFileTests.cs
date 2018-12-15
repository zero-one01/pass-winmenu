using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;

namespace PassWinmenu.External
{
	[TestClass]
	public class ConfigFileTests
	{
		private const string Category = "External: Configuration File";

		[TestMethod, TestCategory(Category)]
		public void ConfigFile_IsValidYaml()
		{
			var des = new DeserializerBuilder()
				.Build();

			// Will throw an exception if the file does not contain valid YAML
			des.Deserialize(File.OpenText(@"..\..\..\pass-winmenu\embedded\default-config.yaml"));
		}
	}
}
