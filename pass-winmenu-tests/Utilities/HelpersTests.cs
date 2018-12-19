using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PassWinmenu.Utilities
{
	/// <summary>
	/// Tests the <see cref="Disposable"/> class.
	/// </summary>
	[TestClass]
	public class HelpersTests
	{
		private const string Category = "Utilities: Helpers";

		[TestMethod, TestCategory(Category)]
		public void GetRelativePath_MakesRelative()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "C:\\base\\path";
			var relative = Helpers.GetRelativePath(fileSpec, root);

			Assert.AreEqual(relative, "test\\file");
		}

		[TestMethod, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfNotRelative()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "C:\\other\\path";

			Assert.ThrowsException<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}

		[TestMethod, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfRootNotAbsolute()
		{
			var fileSpec = "C:\\base\\path\\test\\file";
			var root = "base\\path";

			Assert.ThrowsException<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}

		[TestMethod, TestCategory(Category)]
		public void GetRelativePath_ThrowsIfFileSpecNotAbsolute()
		{
			var fileSpec = "base\\path\\test\\file";
			var root = "base\\path";
			Assert.ThrowsException<ArgumentException>(() => Helpers.GetRelativePath(fileSpec, root));
		}
	}
}
