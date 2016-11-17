using System;

namespace PassWinmenu.Hotkeys
{
	internal class HotkeyException : Exception
	{
		public HotkeyException(string message) : base(message) { }
	}
}