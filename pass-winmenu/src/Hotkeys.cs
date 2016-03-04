using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PassWinmenu
{
	internal partial class Program
	{
		private int hotkeyIdCounter = 0;
		private Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();

		[Flags]
		private enum ModifierKey
		{
			None = 0,
			Alt = 1,
			Control = 2,
			Shift = 4,
			Windows = 8
		}

		private void AddHotKey(ModifierKey mod, Keys key, Action action)
		{
			var success = NativeMethods.RegisterHotKey(Handle, hotkeyIdCounter, (int) mod, (int) key);
			if(!success) throw new InvalidOperationException($"Failed to set the hotkey. Win32 error code: {Marshal.GetLastWin32Error()}");
			hotkeyActions[hotkeyIdCounter] = action;
		}

		private void DisposeHotkeys()
		{
			foreach (var key in hotkeyActions.Keys)
			{
				NativeMethods.UnregisterHotKey(Handle, key);
			}
		}

		private const int WM_HOTKEY = 0x312;

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (m.Msg == WM_HOTKEY)
			{
				//var  key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
				//var modifier = (ModifierKey)((int)m.LParam & 0xFFFF);
				var id = m.WParam.ToInt32();
				hotkeyActions[id]();
			}
		}
	}
}
