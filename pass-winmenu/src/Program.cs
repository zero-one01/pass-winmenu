using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using System.Windows;
using PassWinmenu.Windows;
using Application = System.Windows.Forms.Application;
using Clipboard = System.Windows.Clipboard;

namespace PassWinmenu
{
	delegate void ShowPasswordDelegate(bool copyToClipboard, bool typeUsername, bool typePassword);

	internal class Program : Form
	{
		private enum MainThreadAction
		{
			ShowSearch,
			Quit
		}

		private const string version = "0.3.2-git";
		private readonly NotifyIcon icon = new NotifyIcon();
		private readonly Hotkeys hotkeys;
		private readonly GPG gpg = new GPG(ConfigManager.Config.GpgPath);
		private readonly Git git = new Git(ConfigManager.Config.GitPath, ConfigManager.Config.PasswordStore);
		private readonly FileSystemWatcher configWatcher;

		public Program()
		{
			var result = ConfigManager.Load("pass-winmenu.yaml");
			switch (result)
			{
				case ConfigManager.LoadResult.ParseFailure:
					RaiseNotification($"The configuration file could not be loaded (Parse Error).\nPass-winmenu will fall back to its default settings.", ToolTipIcon.Error);
					break;
				case ConfigManager.LoadResult.FileCreationFailure:
					RaiseNotification($"A default configuration file was generated, but could not be saved.\nPass-winmenu will fall back to its default settings.", ToolTipIcon.Error);
					break;
			}

			// Try to reload the config file when the user edits it.
			configWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory(), "pass-winmenu.yaml");
			configWatcher.Changed += (s,a) =>
			{
				ConfigManager.Reload("pass-winmenu.yaml");
			};
			configWatcher.EnableRaisingEvents = true;

			CreateNotifyIcon();

			hotkeys = new Hotkeys(Handle);
			try
			{
				foreach (var hotkey in ConfigManager.Config.Hotkeys)
				{
					var keys = Hotkeys.Parse(hotkey.Hotkey);
					HotkeyAction action;
					try
					{
						action = hotkey.Action;
					}
					catch (ArgumentException)
					{
						RaiseNotification($"Invalid hotkey configuration in config.yaml.\nThe action \"{hotkey.ActionString}\" is not known.", ToolTipIcon.Error);
						continue;
					}
					switch (action)
					{
						case HotkeyAction.DecryptPassword:
							hotkeys.AddHotKey(keys, () => ShowPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
							break;
						case HotkeyAction.AddPassword:
							hotkeys.AddHotKey(keys, AddPassword);
							break;
					}
				}
			}
			catch (Exception e) when (e is ArgumentException || e is Hotkeys.HotkeyException)
			{
				RaiseNotification(e.Message, ToolTipIcon.Error);
				Application.Exit();
				Environment.Exit(1);
			}
			Name = "pass-winmenu (main window)";
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			hotkeys?.WndProc(ref m);
		}

		protected override void SetVisibleCore(bool value)
		{
			base.SetVisibleCore(false);
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
			Screen selectedScreen;
			if (ConfigManager.Config.FollowCursor)
			{
				// Find the screen that currently contains the mouse cursor.
				selectedScreen = Screen.AllScreens.First(screen => screen.Bounds.Contains(Cursor.Position));
			}
			else
			{
				selectedScreen = Screen.PrimaryScreen;
			}

			double left, top, width, height;
			try
			{
				// The menu position may either be specified in pixels or percentage values.
				// ParseSize takes care of parsing both into a double (representing pixel values).
				left = selectedScreen.ParseSize(ConfigManager.Config.Style.OffsetLeft, Direction.Horizontal);
				top = selectedScreen.ParseSize(ConfigManager.Config.Style.OffsetTop, Direction.Vertical);
			}
			catch (Exception e) when (e is ArgumentNullException || e is FormatException || e is OverflowException)
			{
				RaiseNotification($"Unable to parse the menu position from the config file (reason: {e.Message})", ToolTipIcon.Error);
				return null;
			}
			try
			{
				width = selectedScreen.ParseSize(ConfigManager.Config.Style.Width, Direction.Horizontal);
				height = selectedScreen.ParseSize(ConfigManager.Config.Style.Height, Direction.Vertical);
			}
			catch (Exception e) when (e is ArgumentNullException || e is FormatException || e is OverflowException)
			{
				RaiseNotification($"Unable to parse the menu dimensions from the config file (reason: {e.Message})", ToolTipIcon.Error);
				return null;
			}

			System.Windows.Controls.Orientation orientation;

			if (!Enum.TryParse(ConfigManager.Config.Style.Orientation, true, out orientation))
			{
				RaiseNotification($"Unable to parse the menu orientation from the config file.", ToolTipIcon.Error);
				return null;
			}

			var menu = new Windows.MainWindow(options, new Vector(left + selectedScreen.Bounds.Left, top + selectedScreen.Bounds.Top), new Vector(width, height), orientation);
			menu.ShowDialog();
			if (menu.Success)
			{
				return (string) menu.Selected.Content;
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
		private void CopyToClipboard(string value, double timeout)
		{
			Clipboard.SetText(value);
			Task.Delay(TimeSpan.FromSeconds(timeout)).ContinueWith(_ =>
			{
				Invoke((MethodInvoker) (() =>
				{
					// Only clear the clipboard if it still contains the text we copied to it.
					if (Clipboard.ContainsText() && Clipboard.GetText() == value)
					{
						Clipboard.Clear();
					}
				}));
			});
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			// Unregister all hotkeys before closing the form.
			hotkeys.DisposeHotkeys();
			base.OnFormClosed(e);
		}

		private void CreateShortcut()
		{
			// Open the startup folder in the default file explorer (usually Windows Explorer).
			Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Startup));

			var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "pass-winmenu.lnk");

			var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
			dynamic shell = Activator.CreateInstance(t);
			try
			{
				if (File.Exists(shortcutPath))
				{
					File.Delete(shortcutPath);
				}
				var lnk = shell.CreateShortcut(shortcutPath);
				try
				{
					lnk.TargetPath = Assembly.GetExecutingAssembly().Location;
					lnk.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
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

		private void CreateNotifyIcon()
		{
			icon.Icon = EmbeddedResources.Icon;
			icon.Visible = true;
			var menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripLabel("pass-winmenu v" + version));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Decrypt Password");
			menu.Items.Add("Update Password Store");
			menu.Items.Add("Add new Password");
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Start with Windows");
			menu.Items.Add("About");
			menu.Items.Add("Quit");
			menu.ItemClicked += (sender, args) =>
			{
				switch (args.ClickedItem.Text)
				{
					case "Decrypt Password":
						ShowPassword(true, false, false);
						break;
					case "Update Password Store":
						Task.Run((Action) UpdatePasswordStore);
						break;
					case "Add new Password":
						AddPassword();
						break;
					case "Start with Windows":
						CreateShortcut();
						break;
					case "About":
						Process.Start("https://github.com/Baggykiin/pass-winmenu");
						break;
					case "Quit":
						Close();
						break;
				}
			};
			icon.ContextMenuStrip = menu;
		}

		private void AddPassword()
		{
			var pathWindow = new PathWindow();
			var passwordWindow = new PasswordWindow();

			pathWindow.ShowDialog();
			if (pathWindow.DialogResult == null || !pathWindow.DialogResult.Value)
			{
				return;
			}
			var path = pathWindow.Path.Text;

			passwordWindow.ShowDialog();
			if (passwordWindow.DialogResult == null || !passwordWindow.DialogResult.Value)
			{
				return;
			}
			var password = passwordWindow.Password.Text;

			// Build up the full path to the password file. GetFullPath ensures that
			// all directory separators match.
			var fullPath = Path.GetFullPath(Path.Combine(ConfigManager.Config.PasswordStore, path));
			// Ensure the file's parent directory exists.
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			using (var writer = new StreamWriter(File.OpenWrite(fullPath)))
			{
				writer.Write(password);
			}
			try
			{
				new GPG(ConfigManager.Config.GpgPath).Encrypt(fullPath);
			}
			catch (GpgException e)
			{
				RaiseNotification($"Unable to encrypt your password. Error details:\n{e.Message} ({e.GpgOutput})", ToolTipIcon.Error);
				return;
			}
			finally
			{
				File.Delete(fullPath);
			}

			// Copy the newly generated password.
			CopyToClipboard(password, ConfigManager.Config.ClipboardTimeout);
			RaiseNotification($"The new password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.ClipboardTimeout:0.##} seconds.", ToolTipIcon.Info);
		}

		private void UpdatePasswordStore()
		{
			try
			{
				var result = git.Update();
				RaiseNotification(result, ToolTipIcon.Info);
			}
			catch (GitException e)
			{
				RaiseNotification("Failed to update the password store.\nYou might have to update it manually.", ToolTipIcon.Error);
			}
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		private void ShowPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			if (InvokeRequired)
			{
				Invoke((ShowPasswordDelegate) ShowPassword, copyToClipboard, typeUsername, typePassword);
			}

			var passFiles = GetPasswordFiles(ConfigManager.Config.PasswordStore, ConfigManager.Config.PasswordFileMatch);

			// We should display relative paths to the user, so we'll use a dictionary to map these relative paths to absolute paths.
			var shortNames = passFiles.ToDictionary(file => GetRelativePath(file, ConfigManager.Config.PasswordStore).Replace("\\", ConfigManager.Config.DirectorySeparator).Replace(".gpg", ""), file => file);

			var selection = OpenMenu(shortNames.Keys);
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selection == null) return;

			var result = shortNames[selection];
			try
			{
				var password = gpg.Decrypt(result);
				if (ConfigManager.Config.FirstLineOnly)
				{
					password = password.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None).First();
				}
				if (copyToClipboard)
				{
					CopyToClipboard(password, ConfigManager.Config.ClipboardTimeout);
					RaiseNotification($"The password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.ClipboardTimeout:0.##} seconds.", ToolTipIcon.Info);
				}
				if (typeUsername)
				{
					// Enter the username and press Tab.
					EnterText(Path.GetFileName(result).Replace(".gpg", ""));
				}
				if (typePassword)
				{
					// If a username has also been entered, press Tab to switch to the password field.
					if (typeUsername) SendKeys.Send("{TAB}");

					EnterText(password);
				}
			}
			catch (GpgException e)
			{
				// Not the most descriptive of error messages, but it'll have to do for now.
				RaiseNotification($"Password decryption failed. GPG returned exit code {e.ExitCode}", ToolTipIcon.Error);
			}
		}

		/// <summary>
		/// Sends text directly to the topmost window, as if it was entered by the user.
		/// This method automatically escapes all characters with special meaning, 
		/// then calls SendKeys.Send().
		/// </summary>
		/// <param name="text">The text to be sent to the active window.</param>
		private void EnterText(string text)
		{
			if (ConfigManager.Config.Output.DeadKeys)
			{
				// If dead keys are enabled, insert a space directly after each dead key to prevent
				// it from being combined with the character following it.
				// See https://en.wikipedia.org/wiki/Dead_key
				var deadKeys = new[] {"\"", "'", "`", "~", "^"};
				text = deadKeys.Aggregate(text, (current, key) => current.Replace(key, key + " "));
			}

			// SendKeys.Send expects special characters to be escaped by wrapping them with curly braces.
			var specialCharacters = new[] {'{', '}', '[', ']', '(', ')', '+', '^', '%', '~'};
			var escaped = string.Concat(text.Select(c => specialCharacters.Contains(c) ? $"{{{c}}}" : c.ToString()));
			SendKeys.Send(escaped);
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
			var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

			var matches = files.Where(f => Regex.IsMatch(Path.GetFileName(f), pattern)).ToArray();

			return matches;
		}

		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Program());
		}

		protected override void OnClosed(EventArgs e)
		{
			icon.Dispose();
			//HotKeyManager.UnregisterHotKey(hotkeyId);
			base.OnClosed(e);
		}
	}
}
