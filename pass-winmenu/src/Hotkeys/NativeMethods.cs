using System;
using System.Runtime.InteropServices;

namespace PassWinmenu.Hotkeys
{
	internal static class NativeMethods
	{
		internal const int WM_HOTKEY = 0x312;
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
		[DllImport("user32.dll")]
		internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
	}
}
