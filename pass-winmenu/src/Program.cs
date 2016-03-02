using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassWinmenu
{
	internal class Program : IDisposable
	{
		private enum MainThreadAction
		{
			ShowSearch,
			Quit
		}
		private readonly NotifyIcon icon = new NotifyIcon();
		private readonly GPG gpg = new GPG(ConfigManager.Config.GpgPath);
		private readonly int hotkeyId;
		private readonly AutoResetEvent hotkeyEvent = new AutoResetEvent(false);
		private MainThreadAction nextMainThreadAction;

		public Program()
		{
			ConfigManager.Load("pass-winmenu.yaml");

			hotkeyId = HotKeyManager.RegisterHotKey(Keys.P, KeyModifiers.Control);
			HotKeyManager.HotKeyPressed += (_, __) => NotifyMainThread();
		}

		/// <summary>
		/// Presents a notification to the user.
		/// </summary>
		/// <param name="message">The message that should be displayed.</param>
		/// <param name="tipIcon">The type of icon that should be displayed next to the message.</param>
		/// <param name="timeout">The time period, in milliseconds, the notification should display.</param>
		private void RaiseNotification(string message, ToolTipIcon tipIcon, int timeout = 5000)
		{
			icon.ShowBalloonTip(timeout, "pass-winmenu", message, tipIcon);
		}

		/// <summary>
		/// Opens the menu and displays it to the user, allowing them to choose an option.
		/// </summary>
		/// <param name="options">A list of options the user can choose from.</param>
		/// <returns>One of the values contained in <paramref name="options"/>, or null if no option was chosen.</returns>
		private string OpenMenu(IEnumerable<string> options)
		{
			var menu = new Windows.MainWindow(options);
			menu.ShowDialog();

			if (menu.Success)
			{
				var result = (string)menu.Selected.Content;
				return result;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Copies a string to the clipboard. If it still exists on the clipboard after the amount of time
		/// specified in <paramref name="timeout"/>, it will be removed again.
		/// </summary>
		/// <param name="value">The text to add to the clipboard.</param>
		/// <param name="timeout">The amount of time, in seconds, the text should remain on the clipboard.</param>
		private static void CopyToClipboard(string value, double timeout)
		{
			Clipboard.SetText(value);
			Task.Delay(TimeSpan.FromSeconds(timeout)).ContinueWith(_ =>
			{
				// Only clear the clipboard if it still contains the text we copied to it.
				if (Clipboard.ContainsText() && Clipboard.GetText() == value)
				{
					Clipboard.Clear();
				}
			});
		}

		/// <summary>
		/// Notifies the main thread that it should perform an action.
		/// </summary>
		private void NotifyMainThread()
		{
			hotkeyEvent.Set();
		}

		private void CreateShortcut()
		{
			Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Startup));

			var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
			dynamic shell = Activator.CreateInstance(t);
			try
			{
				var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "pass-winmenu.lnk");
				if (File.Exists(shortcutPath))
				{
					File.Delete(shortcutPath);
				}
				var lnk = shell.CreateShortcut(shortcutPath);
				try
				{
					lnk.TargetPath = Assembly.GetExecutingAssembly().Location;
					lnk.IconLocation = "shell32.dll, 1";
					lnk.Save();
				}
				finally
				{
					Marshal.FinalReleaseComObject(lnk);
				}
			}
			finally
			{
				Marshal.FinalReleaseComObject(shell);
			}
		}

		/// <summary>
		/// Waits notifications from the hotkey thread, showing the search window as soon as it receives one.
		/// </summary>
		public void Run()
		{
			new Thread(() =>
			{
				icon.Icon = EmbeddedResources.Icon;
				icon.Visible = true;
				var menu = new ContextMenuStrip();
				menu.Items.Add(new ToolStripLabel("pass-winmenu v0.1"));
				menu.Items.Add(new ToolStripSeparator());
				menu.Items.Add("Decrypt Password");
				menu.Items.Add("Start with Windows");
				menu.Items.Add("About");
				menu.Items.Add("Quit");
				menu.ItemClicked += (sender, args) =>
				{
					switch (args.ClickedItem.Text)
					{
						case "Decrypt Password":
							nextMainThreadAction = MainThreadAction.ShowSearch;
							NotifyMainThread();
							break;
						case "Start with Windows":
							CreateShortcut();
							break;
						case "About":
							Process.Start("https://github.com/Baggykiin/pass-winmenu");
							break;
						case "Quit":
							nextMainThreadAction = MainThreadAction.Quit;
							NotifyMainThread();
							break;

					}
				};
				icon.ContextMenuStrip = menu;
				Application.Run();
			}).Start();

			while (nextMainThreadAction != MainThreadAction.Quit)
			{
				hotkeyEvent.WaitOne();
				switch (nextMainThreadAction)
				{
					case MainThreadAction.ShowSearch:
						Show();
						break;
				}
			}
			Application.Exit();
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		private void Show()
		{
			var passFiles = GetPasswordFiles(ConfigManager.Config.PasswordStore, ConfigManager.Config.PasswordFileMatch);

			// We should display relative paths to the user, so we'll use a dictionary to map these relative paths to absolute paths.
			var shortNames = passFiles.ToDictionary(
				file => GetRelativePath(file, ConfigManager.Config.PasswordStore)
					.Replace("\\", ConfigManager.Config.DirectorySeparator)
					.Replace(".gpg", ""),
				file => file);

			var selection = OpenMenu(shortNames.Keys);
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selection == null) return;

			var result = shortNames[selection];
			try
			{
				var password = gpg.Decrypt(result);
				if (ConfigManager.Config.FirstLineOnly)
				{
					password = password.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).First();
				}

				CopyToClipboard(password, ConfigManager.Config.ClipboardTimeout);
				RaiseNotification($"The password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.ClipboardTimeout:0.##} seconds.", ToolTipIcon.Info);
			}
			catch (GpgException e)
			{
				// Not the most descriptive of error messages, but it'll have to do for now.
				RaiseNotification($"Password decryption failed. GPG returned exit code {e.ExitCode}", ToolTipIcon.Error);
			}
		}

		/// <summary>
		/// Returns the path of a file relative to a specified root directory.
		/// </summary>
		/// <param name="filespec">The path to the file for which the relative path should be calculated.</param>
		/// <param name="directory">The root directory relative to which the relative path should be calculated.</param>
		/// <returns></returns>
		private static string GetRelativePath(string filespec, string directory)
		{
			var pathUri = new Uri(filespec);

			// The directory URI must end with a directory separator char.
			if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				directory += Path.DirectorySeparatorChar;
			}
			var directoryUri = new Uri(directory);
			return Uri.UnescapeDataString(directoryUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

		/// <summary>
		/// Returns all password files in a directory that match a search pattern.
		/// </summary>
		/// <param name="directory">The directory to search in.</param>
		/// <param name="pattern">The pattern against which the files should be matched.</param>
		/// <returns></returns>
		private static IEnumerable<string> GetPasswordFiles(string directory, string pattern)
		{
			return Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories);
		}

		[STAThread]
		public static void Main(string[] args)
		{
			using (var program = new Program())
			{
				program.Run();
			}
		}

		public void Dispose()
		{
			icon.Dispose();
			HotKeyManager.UnregisterHotKey(hotkeyId);
		}
	}
}
