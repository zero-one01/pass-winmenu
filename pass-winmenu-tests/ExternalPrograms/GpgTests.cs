using System;
using System.IO;

using Moq;

using PassWinmenu.Utilities;
using PassWinmenu.WinApi;

using Xunit;

namespace PassWinmenu.ExternalPrograms
{
		public class GpgTests
	{
		private const string Category = "External Programs: GPG";

		[Fact, TestCategory(Category)]
		public void FindGpgInstallation_SetsLocationFromResolver()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			resolverMock.Setup(r => r.Resolve(It.IsAny<string>())).Returns("C:\\Gpg\\gpg.exe");
			var gpg = new GPG(resolverMock.Object);

			gpg.FindGpgInstallation("C:\\Gpg\\gpg.exe");

			Assert.Equal("C:\\Gpg\\gpg.exe", gpg.GpgExePath);
		}

		[Fact, TestCategory(Category)]
		public void FindGpgInstallation_FileNotFound_ThrowsArgumentException()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			resolverMock.Setup(r => r.Resolve(It.IsAny<string>())).Throws<FileNotFoundException>();
			var gpg = new GPG(resolverMock.Object);

			Assert.Throws<ArgumentException>(() => gpg.FindGpgInstallation("C:\\Gpg\\gpg.exe"));
		}

		[Fact, TestCategory(Category)]
		public void FindGpgInstallation_NullExePath_ReturnsDefaultLocation()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			var gpg = new GPG(resolverMock.Object);

			gpg.FindGpgInstallation(null);
			var defaultPath = Path.Combine(GPG.GpgDefaultInstallDir, GPG.GpgExeName);

			Assert.Equal(defaultPath, gpg.GpgExePath);
		}
	}
}
