using System;

using Xunit.Sdk;

namespace PassWinmenu.Utilities
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public class TestCategoryAttribute : Attribute, ITraitAttribute
	{
		public TestCategoryAttribute(string categoryName)
		{
		}
	}
}
