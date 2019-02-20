using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace PassWinmenu.WinApi
{
	[TestClass]
	public class ExecutablePathResolverTests
	{
		private const string Category = "Windows API: Executable Path Resolver";

		private readonly IFileSystem fileSystem;

		public ExecutablePathResolverTests()
		{
			fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
			{
				{ "C:\\file.exe", new MockFileData("") },
				{ "C:\\bin\\file.exe", new MockFileData("") }
			},
			"C:\\program");
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_WithDirectorySeparatorChar_UsesGivenLocation()
		{
			var environment = new Mock<IEnvironment>();
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			var location = resolver.Resolve("C:\\file.exe");

			Assert.AreEqual("C:\\file.exe", location);
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_WithRelativePath_ReturnsAbsolutePath()
		{
			var environment = new Mock<IEnvironment>();
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			var location = resolver.Resolve("..\\file.exe");

			Assert.AreEqual("C:\\file.exe", location);
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_NonexistentFile_ThrowsExecutableNotFoundException()
		{
			var environment = new Mock<IEnvironment>();
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			Assert.ThrowsException<ExecutableNotFoundException>(() => resolver.Resolve("C:\\non-existent-file.exe"));
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_WithoutDirectorySeparators_GetsLocationFromPath()
		{
			var environment = new Mock<IEnvironment>();
			environment.Setup(e => e.GetEnvironmentVariable("PATH")).Returns("C:\\bin");
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			var location = resolver.Resolve("file.exe");

			environment.Verify(e => e.GetEnvironmentVariable("PATH"), Times.Once);
			Assert.AreEqual("C:\\bin\\file.exe", location);
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_WithoutDirectorySeparatorsAndExtension_GetsLocationFromPath()
		{
			var environment = new Mock<IEnvironment>();
			environment.Setup(e => e.GetEnvironmentVariable("PATH")).Returns("C:\\bin");
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			var location = resolver.Resolve("file");

			environment.Verify(e => e.GetEnvironmentVariable("PATH"), Times.Once);
			Assert.AreEqual("C:\\bin\\file.exe", location);
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_NotInPath_ThrowsExecutableNotFoundException()
		{
			var environment = new Mock<IEnvironment>();
			environment.Setup(e => e.GetEnvironmentVariable("PATH")).Returns("C:\\bin");
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			Assert.ThrowsException<ExecutableNotFoundException>(() => resolver.Resolve("non-existent-file"));
			environment.Verify(e => e.GetEnvironmentVariable("PATH"), Times.Once);
		}

		[TestMethod, TestCategory(Category)]
		public void Resolve_WithNullPath_ThrowsExecutableNotFoundException()
		{
			var environment = new Mock<IEnvironment>();
			environment.Setup(e => e.GetEnvironmentVariable("PATH")).Returns((string)null);
			var resolver = new ExecutablePathResolver(fileSystem, environment.Object);

			Assert.ThrowsException<ExecutableNotFoundException>(() => resolver.Resolve("file.exe"));
			environment.Verify(e => e.GetEnvironmentVariable("PATH"), Times.Once);
		}
	}
}
