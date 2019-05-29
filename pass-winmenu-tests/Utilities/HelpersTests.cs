using System;
using PassWinmenu.Utilities;
using Xunit;

namespace PassWinmenuTests.Utilities
{
	/// <summary>
	/// Tests the <see cref="Disposable"/> class.
	/// </summary>
		public class HelpersTests
	{
		private const string Category = "Utilities: Helpers";

		[Fact, TestCategory(Category)]
		public void GetRelativePath_MakesRelative()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "C:\\base\\path";
			var relative = Helpers.GetRelativePath(fileSpec, root);

			Assert.Equal("test\\file", relative);
		}

		[Fact, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfNotRelative()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "C:\\other\\path";

			Assert.Throws<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}

		[Fact, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfRootNotAbsolute()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "base\\path";

			Assert.Throws<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}

		[Fact, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfFileSpecNotAbsolute()
		{
			var fileSpec = "base\\path\\test\\file";
			var root = "base\\path";
			Assert.Throws<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}
	}
}
