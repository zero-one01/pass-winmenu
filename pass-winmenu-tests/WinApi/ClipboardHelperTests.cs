using Microsoft.VisualStudio.TestTools.UnitTesting;
using PassWinmenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassWinmenu.WinApi
{
	[TestClass()]
	public class ClipboardHelperTests
	{
		private const string Category = "Windows API: Clipboard Helper";


		[TestMethod, TestCategory(Category)]
		public void ClipboardHelper_PlacesText()
		{
			var clipboard = new ClipboardHelper();
			var content = Guid.NewGuid().ToString();

			Assert.AreNotEqual(clipboard.GetText(), content);

			clipboard.Place(content, TimeSpan.FromSeconds(1));

			Assert.AreEqual(clipboard.GetText(), content);
		}

		[TestMethod, TestCategory(Category)]
		public void ClipboardHelper_RemovesText()
		{
			var clipboard = new ClipboardHelper();
			var content = Guid.NewGuid().ToString();

			var beforeText = clipboard.GetText();

			clipboard.Place(content, TimeSpan.FromSeconds(1));
			Assert.AreNotEqual(beforeText, clipboard.GetText());

			Thread.Sleep(1100);
			Assert.AreEqual(beforeText, clipboard.GetText());
		}
	}
}
