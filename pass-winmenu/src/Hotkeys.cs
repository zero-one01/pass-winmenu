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

		internal class HotkeyException : Exception
		{
			public HotkeyException(string message) : base(message){ }
		}

		private void AddHotKey(ModifierKey mod, Keys key, Action action)
		{
			var success = NativeMethods.RegisterHotKey(Handle, hotkeyIdCounter, (int) mod, (int) key);
			if (!success)
			{
				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode == 1409)
				{
					throw new HotkeyException($"Failed to register the hotkey \"{mod}, {key}\". This hotkey has already been registered by a different application.");
				}
				else
				{
					throw new HotkeyException($"Failed to register the hotkey \"{mod} + {key}\". An unknown error (Win32 error code {errorCode}) occurred.");
				}
			}
			if(!success) throw new InvalidOperationException();
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
