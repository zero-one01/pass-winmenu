using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenuTests.Utilities
{
	internal class GpgInstallationBuilder
	{
		private readonly MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{@"C:\gpg\bin\gpg.exe", MockFileData.NullObject },
			{@"C:\gpg\bin\gpg-agent.exe", MockFileData.NullObject },
			{@"C:\gpg\bin\gpg-connect-agent.exe", MockFileData.NullObject }
		});

		public GpgInstallation Build()
		{
			return new GpgInstallation
			{
				InstallDirectory = fileSystem.DirectoryInfo.FromDirectoryName(@"C:\gpg\bin"),
				GpgExecutable = fileSystem.FileInfo.FromFileName(@"C:\gpg\bin\gpg.exe"),
				GpgAgentExecutable = fileSystem.FileInfo.FromFileName(@"C:\gpg\bin\gpg-agent.exe"),
				GpgConnectAgentExecutable = fileSystem.FileInfo.FromFileName(@"C:\gpg\bin\gpg-connect-agent.exe")
			};
		}
	}
}
