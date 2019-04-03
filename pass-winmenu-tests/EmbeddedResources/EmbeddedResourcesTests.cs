using PassWinmenu.Utilities;

using Xunit;

namespace PassWinmenu.Tests
{
		public class EmbeddedResourcesTests
	{
		private const string Category = "Core: Embedded resources";

		[Fact, TestCategory(Category)]
		public void EmbeddedResources_ContainsVersionString()
		{
			EmbeddedResources.Load();
			Assert.False(string.IsNullOrWhiteSpace(EmbeddedResources.Version));
		}
	}
}
