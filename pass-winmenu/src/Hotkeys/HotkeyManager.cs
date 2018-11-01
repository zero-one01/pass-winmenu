using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using PassWinmenu.Configuration;
using PassWinmenu.UpdateChecking;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

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

		public void AssignHotkeys(IEnumerable<HotkeyConfig> config, DialogCreator dialogCreator, UpdateChecker updateChecker, INotificationService notificationService, Program program)
		{
			foreach (var hotkey in config)
			{
				var keys = KeyCombination.Parse(hotkey.Hotkey);
				HotkeyAction action;
				try
				{
					// Reading the Action variable will cause it to be parsed from hotkey.ActionString.
					// If this fails, an ArgumentException is thrown.
					action = hotkey.Action;
				}
				catch (ArgumentException)
				{
					notificationService.Raise($"Invalid hotkey configuration in config.yaml.\nThe action \"{hotkey.ActionString}\" is not known.", Severity.Error);
					continue;
				}
				switch (action)
				{
					case HotkeyAction.DecryptPassword:
						AddHotKey(keys, () => program.DecryptPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
						break;
					case HotkeyAction.AddPassword:
						AddHotKey(keys, dialogCreator.AddPassword);
						break;
					case HotkeyAction.EditPassword:
						AddHotKey(keys, dialogCreator.EditPassword);
						break;
					case HotkeyAction.GitPull:
						AddHotKey(keys, program.UpdatePasswordStore);
						break;
					case HotkeyAction.GitPush:
						AddHotKey(keys, program.CommitChanges);
						break;
					case HotkeyAction.OpenShell:
						AddHotKey(keys, dialogCreator.OpenPasswordShell);
						break;
					case HotkeyAction.ShowDebugInfo:
						AddHotKey(keys, dialogCreator.ShowDebugInfo);
						break;
					case HotkeyAction.CheckForUpdates:
						AddHotKey(keys, updateChecker.CheckForUpdates);
						break;
				}
			}
		}
	}
}
