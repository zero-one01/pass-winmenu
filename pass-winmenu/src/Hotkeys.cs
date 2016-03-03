using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassWinmenu
{
	internal partial class Program
	{
		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private int hotkeyIdCounter = 0;
		private Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();

		[Flags]
		enum ModifierKey
		{
			None = 0,
			Alt = 1,
			Control = 2,
			Shift = 4,
			Windows = 8
		}

		private void AddHotKey(ModifierKey mod, Keys key, Action action)
		{
			var success = RegisterHotKey(Handle, hotkeyIdCounter, (int) mod, (int) key);
			if(!success) throw new InvalidOperationException($"Failed to set the hotkey. Win32 error code: {Marshal.GetLastWin32Error()}");
			hotkeyActions[hotkeyIdCounter] = action;
		}

		private void DisposeHotkeys()
		{
			foreach (var key in hotkeyActions.Keys)
			{
				UnregisterHotKey(Handle, key);
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
