using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Windows;
using Application = System.Windows.Forms.Application;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace PassWinmenu
{
	internal class Program : Form
	{
		private const string version = "1.0.1-git-3";
		private readonly NotifyIcon icon = new NotifyIcon();
		private readonly Hotkeys hotkeys;
		private readonly GPG gpg = new GPG(ConfigManager.Config.GpgPath);
		private readonly Git git = new Git(ConfigManager.Config.GitPath, ConfigManager.Config.PasswordStore);

		public Program()
		{
			var result = ConfigManager.Load("pass-winmenu.yaml");
			switch (result)
			{
				case ConfigManager.LoadResult.ParseFailure:
					RaiseNotification("The configuration file could not be loaded (Parse Error).\nPass-winmenu will fall back to its default settings.", ToolTipIcon.Error);
					break;
				case ConfigManager.LoadResult.FileCreationFailure:
					RaiseNotification("A default configuration file was generated, but could not be saved.\nPass-winmenu will fall back to its default settings.", ToolTipIcon.Error);
					break;
				case ConfigManager.LoadResult.NewFileCreated:
					MessageBox.Show("A new configuration file has been generated. Please modify it according to your preferences and restart the application.");
					Close();
					Application.Exit();
					Environment.Exit(0);
					return;
			}

			// Try to reload the config file when the user edits it.
			var configWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory(), "pass-winmenu.yaml");
			configWatcher.Changed += (s, a) =>
			{
				ConfigManager.Reload("pass-winmenu.yaml");
			};
			configWatcher.EnableRaisingEvents = true;

			if (ConfigManager.Config.PreloadGpgAgent)
			{
				// Running GPG with these arguments will start the gpg-agent,
				// but it will also cause GPG to throw an error, which we can
				// safely ignore.
				Task.Run(() => gpg.RunGPG("--passphrase \"1\" --batch -ce"));
			}

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
						// Reading the Action variable will cause it to be parsed from hotkey.ActionString.
						// If this fails, an ArgumentException is thrown.
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
							hotkeys.AddHotKey(keys, () => DecryptPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
							break;
						case HotkeyAction.AddPassword:
							hotkeys.AddHotKey(keys, AddPassword);
							break;
						case HotkeyAction.GitPull:
							hotkeys.AddHotKey(keys, UpdatePasswordStore);
							break;
						case HotkeyAction.GitPush:
							hotkeys.AddHotKey(keys, CommitChanges);
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
			// Pass window messages on to the hotkey handler.
			hotkeys?.WndProc(ref m);
		}

		protected override void SetVisibleCore(bool value)
		{
			// Do not allow this window to be made visible.
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
		/// Opens the password menu and displays it to the user, allowing them to choose an option.
		/// </summary>
		/// <param name="options">A list of options the user can choose from.</param>
		/// <returns>One of the values contained in <paramref name="options"/>, or null if no option was chosen.</returns>
		private string ShowPasswordMenu(IEnumerable<string> options)
		{
			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				RaiseNotification(e.Message, ToolTipIcon.Error);
				return null;
			}

			var menu = new PasswordSelectionWindow(options, windowConfig);
			menu.ShowDialog();
			if (menu.Success)
			{
				return menu.GetSelection();
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
			// Try to save the current contents of the clipboard and restore them after the password is removed.
			string previousText = "";
			if (Clipboard.ContainsText())
			{
				previousText = Clipboard.GetText();
			}
			//Clipboard.SetText(value);
			Clipboard.SetDataObject(value);
			Task.Delay(TimeSpan.FromSeconds(timeout)).ContinueWith(_ =>
			{
				Invoke((MethodInvoker)(() =>
			   {
					// Only reset the clipboard to its previous contents if it still contains the text we copied to it.
					// If the clipboard did not previously contain any text, it is simply cleared.
					if (Clipboard.ContainsText() && Clipboard.GetText() == value)
				   {
					   Clipboard.SetText(previousText);
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

		/// <summary>
		/// Creates a shortcut to the application, overwriting any existing shortcuts with the same name.
		/// </summary>
		private void CreateShortcut()
		{
			// Open the startup folder in the default file explorer (usually Windows Explorer).
			Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Startup));

			var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "pass-winmenu.lnk");

			var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
			dynamic shell = Activator.CreateInstance(t);
			try
			{
				// Overwrite any previous links that might exist
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

		/// <summary>
		/// Creates a notification area icon for the application.
		/// </summary>
		private void CreateNotifyIcon()
		{
			icon.Icon = EmbeddedResources.Icon;
			icon.Visible = true;
			var menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripLabel("pass-winmenu v" + version));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Decrypt Password", null, (sender, args) => Task.Run(() => DecryptPassword(true, false, false)));
			menu.Items.Add("Add new Password", null, (sender, args) => Task.Run((Action)AddPassword));
			menu.Items.Add("Edit Password File", null, (sender, args) => Task.Run(() => EditPassword()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Push to Remote", null, (sender, args) => Task.Run((Action)CommitChanges));
			menu.Items.Add("Pull from Remote", null, (sender, args) => Task.Run((Action)UpdatePasswordStore));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Open Explorer", null, (sender, args) => Process.Start(ConfigManager.Config.PasswordStore));
			menu.Items.Add("Open Shell", null, (sender, args) =>
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "powershell",
					WorkingDirectory = ConfigManager.Config.PasswordStore
				});
			});
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Start with Windows", null, (sender, args) => CreateShortcut());
			menu.Items.Add("About", null, (sender, args) => Process.Start("https://github.com/Baggykiin/pass-winmenu"));
			menu.Items.Add("Quit", null, (sender, args) => Close());
			icon.ContextMenuStrip = menu;
		}

		/// <summary>
		/// Commits all local changes and pushes them to remote.
		/// Also pulls any upcoming changes from remote.
		/// </summary>
		private void CommitChanges()
		{
			try
			{
				var changes = git.Commit();

				var sb = new StringBuilder();
				if (changes.CommittedFiles.Count == 0)
				{
					sb.AppendLine("Nothing to commit (no changes since last pushed commit).");
					if (changes.Pull.Commits.Count > 1)
					{
						sb.AppendLine($"{changes.Pull.Commits.Count} new commits were pulled from remote.");
					}
					else if (changes.Pull.Commits.Count == 1)
					{
						sb.AppendLine("1 new commit was pulled from remote.");
					}

					RaiseNotification(sb.ToString(), ToolTipIcon.Info);
				}
				else
				{
					sb.AppendLine($"Pushed {changes.CommittedFiles.Count} changed file{(changes.CommittedFiles.Count > 1 ? "s" : "")} to remote.");
					if (changes.Pull.Commits.Count > 1)
					{
						sb.AppendLine($"Additionally, {changes.Pull.Commits.Count} new commits were pulled from remote.");
					}
					else if (changes.Pull.Commits.Count == 1)
					{
						sb.AppendLine("Additionally, 1 new commit was pulled from remote.");
					}
					RaiseNotification(sb.ToString(), ToolTipIcon.Info);
				}

			}
			catch (GitException e)
			{
				RaiseNotification($"Failed to push your changes. Git returned an error (exit code {e.ExitCode}): {e.GitError}", ToolTipIcon.Error);
			}
			catch (Exception e)
			{
				RaiseNotification($"Failed to push your changes. An unknown error occurred. Error details:\n{e.GetType().Name}: {e.Message}", ToolTipIcon.Error);
			}
		}

		/// <summary>
		/// Adds a new password to the password store.
		/// </summary>
		private void AddPassword()
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)AddPassword);
				return;
			}

			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				RaiseNotification(e.Message, ToolTipIcon.Error);
				return;
			}

			// Ask the user where the password file should be placed.
			var pathWindow = new FileSelectionWindow(ConfigManager.Config.PasswordStore, windowConfig);
			pathWindow.ShowDialog();
			if (!pathWindow.Success)
			{
				return;
			}
			var path = pathWindow.GetSelection();

			// Display the password generation window.
			var passwordWindow = new PasswordWindow(Path.GetFileName(path));
			passwordWindow.ShowDialog();
			if (passwordWindow.DialogResult == null || !passwordWindow.DialogResult.Value)
			{
				return;
			}
			var password = passwordWindow.Password.Text;
			var extraContent = passwordWindow.ExtraContent.Text.Replace(Environment.NewLine, "\n");
			var fullPassword = $"{password}\n{extraContent}";

			// Build up the full path to the password file. GetFullPath ensures that
			// all directory separators match.
			var fullPath = Path.GetFullPath(Path.Combine(ConfigManager.Config.PasswordStore, path));
			// Ensure the file's parent directory exists.
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			try
			{
				gpg.Encrypt(fullPassword, GetGpgId(), fullPath + ".gpg");
			}
			catch (GpgException e)
			{
				RaiseNotification($"Unable to encrypt your password. Error details:\n{e.Message} ({e.GpgOutput})", ToolTipIcon.Error);
				return;
			}


			// Copy the newly generated password.
			CopyToClipboard(password, ConfigManager.Config.ClipboardTimeout);
			RaiseNotification($"The new password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.ClipboardTimeout:0.##} seconds.", ToolTipIcon.Info);
		}

		private string GetGpgId()
		{
			using (var reader = new StreamReader(Path.Combine(ConfigManager.Config.PasswordStore, ".gpg-id")))
			{
				return reader.ReadLine();
			}
		}

		/// <summary>
		/// Updates the password store so it's in sync with remote again.
		/// </summary>
		private void UpdatePasswordStore()
		{
			try
			{
				var result = git.Update();
				RaiseNotification(result, ToolTipIcon.Info);
			}
			catch (GitException)
			{
				RaiseNotification("Failed to update the password store.\nYou might have to update it manually.", ToolTipIcon.Error);
			}
		}

		private string RequestPasswordFile()
		{
			if (InvokeRequired)
			{
				return (string)Invoke((Func<string>)RequestPasswordFile);
			}
			// Find GPG-encrypted password files
			var passFiles = GetPasswordFiles(ConfigManager.Config.PasswordStore, ConfigManager.Config.PasswordFileMatch);
			// We should display relative paths to the user, so we'll use a dictionary to map these relative paths to absolute paths.
			var shortNames = passFiles.ToDictionary(file => GetRelativePath(Path.GetFullPath(file), Path.GetFullPath(ConfigManager.Config.PasswordStore)).Replace("\\", ConfigManager.Config.DirectorySeparator).Replace(".gpg", ""), file => file);

			var selection = ShowPasswordMenu(shortNames.Keys);
			if (selection == null) return null;
			var selectedFile = shortNames[selection];
			return selectedFile;
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		private void DecryptPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			// We need to be on the main thread for this.
			if (InvokeRequired)
			{
				Invoke((Action<bool, bool, bool>)DecryptPassword, copyToClipboard, typeUsername, typePassword);
				return;
			}

			var selectedFile = RequestPasswordFile();
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selectedFile == null) return;

			string password;
			try
			{
				password = gpg.Decrypt(selectedFile);
			}
			catch (GpgException e)
			{
				if (string.IsNullOrEmpty(e.GpgError))
				{
					RaiseNotification($"Password decryption failed. GPG returned exit code {e.ExitCode}", ToolTipIcon.Error);
				}
				else
				{
					RaiseNotification("Password decryption failed:\n" + e.GpgError, ToolTipIcon.Error);
				}
				return;
			}
			// The extra content begins after the first line.
			var extraContent = Regex.Match(password, @".*?(?:\r\n|\n)(.*)", RegexOptions.Singleline).Groups[1].Value;

			if (ConfigManager.Config.FirstLineOnly)
			{
				password = password.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).First();
			}
			if (copyToClipboard)
			{
				CopyToClipboard(password, ConfigManager.Config.ClipboardTimeout);
				RaiseNotification($"The password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.ClipboardTimeout:0.##} seconds.", ToolTipIcon.Info);
			}
			var usernameEntered = false;
			if (typeUsername)
			{
				var username = GetUsername(selectedFile, extraContent);
				if (username != null)
				{
					EnterText(username);
					usernameEntered = true;
				}
			}
			if (typePassword)
			{
				// If a username has also been entered, press Tab to switch to the password field.
				if (usernameEntered) SendKeys.Send("{TAB}");

				EnterText(password);
			}
		}

		private void EditPassword()
		{
			var selectedFile = RequestPasswordFile();
			if (selectedFile == null) return;
			var plaintextFile = gpg.DecryptToFile(selectedFile);

			var startTime = DateTime.Now;

			// Open the file in the user's default editor
			var proc = Process.Start(plaintextFile);

			var result = MessageBox.Show("Please keep this window open until you're done editing the password file.\nThen click Yes to save your changes, or No to discard them.", $"Save changes to {Path.GetFileName(selectedFile)}?", MessageBoxButton.YesNo, MessageBoxImage.Information);

			if (result != MessageBoxResult.Yes)
			{
				File.Delete(plaintextFile);
			}
			else
			{
				File.Delete(selectedFile);
				gpg.EncryptFile(plaintextFile, selectedFile, GetGpgId());
				File.Delete(plaintextFile);
			}
		}

		/// <summary>
		/// Attepts to retrieve the username from a password file.
		/// </summary>
		/// <param name="passwordFile">The name of the password file.</param>
		/// <param name="contents">The extra content of the password file.</param>
		/// <returns>A string containing the username if the password file contains one, null if no username was found.</returns>
		private string GetUsername(string passwordFile, string contents)
		{
			var options = ConfigManager.Config.UsernameDetection.Options;
			switch (ConfigManager.Config.UsernameDetection.Method)
			{
				case UsernameDetectionMethod.FileName:
					return Path.GetFileName(passwordFile).Replace(".gpg", "");
				case UsernameDetectionMethod.LineNumber:
					var extraLines = contents.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
					var lineNumber = options.LineNumber - 2;
					if (lineNumber <= 1) RaiseNotification("Failed to read username from password file: username-detection.options.line-number must be set to 2 or higher.", ToolTipIcon.Warning);
					return lineNumber < extraLines.Length ? extraLines[lineNumber] : null;
				case UsernameDetectionMethod.Regex:
					var rgxOptions = options.RegexOptions.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
					rgxOptions = rgxOptions | (options.RegexOptions.Multiline ? RegexOptions.Multiline : RegexOptions.None);
					rgxOptions = rgxOptions | (options.RegexOptions.Singleline ? RegexOptions.Singleline : RegexOptions.None);
					var match = Regex.Match(contents, options.Regex, rgxOptions);
					return match.Groups["username"].Success ? match.Groups["username"].Value : null;
				default:
					throw new ArgumentOutOfRangeException("username-detection.method", "Invalid username detection method.");
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
				var deadKeys = new[] { "\"", "'", "`", "~", "^" };
				text = deadKeys.Aggregate(text, (current, key) => current.Replace(key, key + " "));
			}

			// SendKeys.Send expects special characters to be escaped by wrapping them with curly braces.
			var specialCharacters = new[] { '{', '}', '[', ']', '(', ')', '+', '^', '%', '~' };
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

		protected override void OnClosing(CancelEventArgs e)
		{
			icon.Visible = false;
			icon.Dispose();
			hotkeys.DisposeHotkeys();
		}
	}
}
