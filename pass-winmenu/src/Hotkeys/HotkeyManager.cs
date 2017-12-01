using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace PassWinmenu.Hotkeys
{
	internal class HotkeyManager : IDisposable
	{
		private int hotkeyIdCounter;
		private readonly Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();
		private readonly IntPtr handle;

		/// <summary>
		/// Create a new hotkey manager.
		/// </summary>
		/// <param name="hWnd">The window handle of the window to which the hotkeys should be assigned.</param>
		public HotkeyManager(IntPtr hWnd)
		{
			handle = hWnd;
		}

		/// <summary>
		/// Register a new hotkey with Windows.
		/// </summary>
		/// <param name="keys">A KeyCombination object representing the keys to be pressed.</param>
		/// <param name="action">The action to be executed when the hotkey is pressed.</param>
		public void AddHotKey(KeyCombination keys, Action action)
		{
			AddHotKey(keys.ModifierKeys, keys.Key, action);
		}

		/// <summary>
		/// Register a new hotkey with Windows.
		/// </summary>
		/// <param name="mod">The modifier keys that should be pressed.</param>
		/// <param name="key">The keys that should be pressed.</param>
		/// <param name="action">The action to be executed when the hotkey is pressed.</param>
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

		/// <summary>
		/// WndProc handler. This must be called from the WndProc handler of the
		/// window to which the hotkeys are registered.
		/// </summary>
		/// <param name="message"></param>
		public void HandleWndProc(ref Message message)
		{
			if (message.Msg == NativeMethods.WM_HOTKEY)
			{
				var id = message.WParam.ToInt32();
				if (hotkeyActions.ContainsKey(id))
				{
					hotkeyActions[id]();
				}
			}
		}

		public void Dispose()
		{
			foreach (var key in hotkeyActions.Keys)
			{
				NativeMethods.UnregisterHotKey(handle, key);
			}
		}
	}
}
