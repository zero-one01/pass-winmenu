using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PassWinmenu.PasswordManagement;

namespace PassWinmenu.Tests
{
	[TestClass]
	public class EmbeddedResourcesTests
	{
		private const string Category = "Core: Embedded resources";

		[TestMethod, TestCategory(Category)]
		public void EmbeddedResources_ContainsVersionString()
		{
			EmbeddedResources.Load();
			Assert.IsFalse(string.IsNullOrWhiteSpace(EmbeddedResources.Version));
		}
	}
}
