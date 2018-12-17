using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PassWinmenu.WinApi
{
	[TestClass]
	public class ClipboardHelperTests
	{
		private const string Category = "Windows API: Clipboard Helper";

		[TestMethod, TestCategory(Category)]
		public void ClipboardHelper_PlacesText()
		{
			// TODO: Find a way to test this.
			// The code below doesn't work because we have no Application reference.

			//var clipboard = new ClipboardHelper();
			//var content = Guid.NewGuid().ToString();

			//Assert.AreNotEqual(clipboard.GetText(), content);

			//clipboard.Place(content, TimeSpan.FromSeconds(1));

			//Assert.AreEqual(clipboard.GetText(), content);
		}

		[TestMethod, TestCategory(Category)]
		public void ClipboardHelper_RemovesText()
		{
			// TODO: Find a way to test this.
			// The code below doesn't work because we have no Application reference.

			//var clipboard = new ClipboardHelper();
			//var content = Guid.NewGuid().ToString();

			//var beforeText = clipboard.GetText();

			//clipboard.Place(content, TimeSpan.FromSeconds(1));
			//Assert.AreNotEqual(beforeText, clipboard.GetText());
			//Thread.Sleep(1100);

			//var waitAttempts = 20;
			//for (var i = 0; i < waitAttempts && clipboard.GetText() == content; i++)
			//{
			//	Thread.Sleep(100);
			//}
			//var clipText = clipboard.GetText();

			//Assert.AreNotEqual(clipText, content, "Clipboard was not cleared.");

			//Assert.AreEqual(beforeText, clipText);
		}
	}
}
