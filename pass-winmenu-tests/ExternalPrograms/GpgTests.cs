using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PassWinmenu.WinApi;

namespace PassWinmenu.ExternalPrograms
{
	[TestClass]
	public class GpgTests
	{
		private const string Category = "External Programs: GPG";

		[TestMethod, TestCategory(Category)]
		public void FindGpgInstallation_SetsLocationFromResolver()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			resolverMock.Setup(r => r.Resolve(It.IsAny<string>())).Returns("C:\\Gpg\\gpg.exe");
			var gpg = new GPG(resolverMock.Object);

			gpg.FindGpgInstallation("C:\\Gpg\\gpg.exe");

			Assert.AreEqual("C:\\Gpg\\gpg.exe", gpg.GpgExePath);
		}

		[TestMethod, TestCategory(Category)]
		public void FindGpgInstallation_FileNotFound_ThrowsArgumentException()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			resolverMock.Setup(r => r.Resolve(It.IsAny<string>())).Throws<FileNotFoundException>();
			var gpg = new GPG(resolverMock.Object);

			Assert.ThrowsException<ArgumentException>(() => gpg.FindGpgInstallation("C:\\Gpg\\gpg.exe"));
		}

		[TestMethod, TestCategory(Category)]
		public void FindGpgInstallation_NullExePath_ReturnsDefaultLocation()
		{
			var resolverMock = new Mock<IExecutablePathResolver>();
			var gpg = new GPG(resolverMock.Object);

			gpg.FindGpgInstallation(null);
			var defaultPath = Path.Combine(GPG.GpgDefaultInstallDir, GPG.GpgExeName);

			Assert.AreEqual(defaultPath, gpg.GpgExePath);
		}
	}
}
