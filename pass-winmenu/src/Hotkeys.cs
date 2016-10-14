using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace PassWinmenu
{
	internal class Hotkeys
	{
		private int hotkeyIdCounter;
		private readonly Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();
		private readonly IntPtr handle;

		internal struct KeyCombination
		{
			public ModifierKeys ModifierKeys;
			public Keys Key;

			public override string ToString()
			{
				return ModifierKeys + ", " + Key;
			}
		}

		public Hotkeys(IntPtr hWnd)
		{
			handle = hWnd;
		}

		public static KeyCombination Parse(string str)
		{
			// Split and capitalise a whitespace-delimited list of keys.
			var combination = str.Split(new[] {' ', '\t', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries)
			                     .Select(key => key.Trim())
			                     .Select(key => key.First().ToString().ToUpper() + key.Substring(1))
			                     .Select(key => key == "Ctrl" ? "Control" : key)
			                     .Select(key => (key == "Win" || key == "Super") ? "Windows" : key);
			var mods = ModifierKeys.None;
			var keys = Keys.None;
			foreach (var keyStr in combination)
			{
				ModifierKeys parsedMod;
				Keys parsedKey;
				if (Enum.TryParse(keyStr, out parsedMod))
				{
					mods |= parsedMod;
				}
				else if (Enum.TryParse(keyStr, out parsedKey))
				{
					if (keys == Keys.None)
						keys = parsedKey;
					else
						throw new ArgumentException("A hotkey may not consist of multiple non-modifier keys.");
				}
				else
				{
					throw new ArgumentException($"Invalid key name: '{keyStr}'");
				}
			}
			return new KeyCombination {ModifierKeys = mods, Key = keys};
		}

		internal class HotkeyException : Exception
		{
			public HotkeyException(string message) : base(message) { }
		}

		public void AddHotKey(KeyCombination keys, Action action)
		{
			AddHotKey(keys.ModifierKeys, keys.Key, action);
		}

		public void AddHotKey(ModifierKeys mod, Keys key, Action action)
		{
			var success = NativeMethods.RegisterHotKey(handle, hotkeyIdCounter, (int)mod, (int)key);
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
			if (!success) throw new InvalidOperationException();
			hotkeyActions[hotkeyIdCounter] = action;
			hotkeyIdCounter++;
		}

		public void DisposeHotkeys()
		{
			foreach (var key in hotkeyActions.Keys)
			{
				NativeMethods.UnregisterHotKey(handle, key);
			}
		}
		public void WndProc(ref Message m)
		{
			if (m.Msg == NativeMethods.WM_HOTKEY)
			{
				//var  key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
				//var modifier = (ModifierKey)((int)m.LParam & 0xFFFF);
				var id = m.WParam.ToInt32();
				hotkeyActions[id]();
			}
		}
	}
}
