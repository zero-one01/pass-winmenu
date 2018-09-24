using System;
using System.Windows;

namespace PassWinmenu
{
	internal class ClipboardHelper
	{
		public void Place(string text, TimeSpan timeout)
		{
			Clipboard.Flush();
			var previousData = Clipboard.GetDataObject();

			Clipboard.Clear();
		}
	}
}
