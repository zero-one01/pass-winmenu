using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace PassWinmenu.Hotkeys
{
	internal class HotkeyManager : IDisposable
	{
		private readonly List<IDisposable> registrations = new List<IDisposable>();

		private readonly IHotkeyRegistrar registrar;

		/// <summary>
		/// Create a new hotkey manager.
		/// </summary>
		public HotkeyManager()
		{
			registrar = HotkeyRegistrars.Windows;
		}

		/// <summary>
		/// Register a new hotkey with Windows.
		/// </summary>
		/// <param name="keys">A KeyCombination object representing the keys to be pressed.</param>
		/// <param name="action">The action to be executed when the hotkey is pressed.</param>
		public void AddHotKey(KeyCombination keys, Action action)
		{
			var reg = registrar.Register(keys.ModifierKeys, keys.Key, false, (sender, args) =>
			{
				action.Invoke();
			});
			registrations.Add(reg);
		}

		public void Dispose()
		{
			foreach (var reg in registrations)
			{
				reg.Dispose();
			}
		}
	}
}
