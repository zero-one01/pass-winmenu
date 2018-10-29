using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using LibGit2Sharp;
using McSherry.SemanticVersioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PassWinmenu.Hotkeys;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.UpdateChecking;
using PassWinmenu.UpdateChecking.GitHub;
using PassWinmenu.Windows;
using YamlDotNet.Core;
using Application = System.Windows.Forms.Application;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace PassWinmenu
{
	internal class Program : Form
	{
		public static string Version => EmbeddedResources.Version;
		public const string LastConfigVersion = "1.7";
		public const string EncryptedFileExtension = ".gpg";
		public const string PlaintextFileExtension = ".txt";
		private readonly NotifyIcon icon = new NotifyIcon();
		private readonly HotkeyManager hotkeys;
		private readonly StartupLink startupLink = new StartupLink("pass-winmenu");
		private readonly ClipboardHelper clipboard = new ClipboardHelper();
		private DialogCreator dialogCreator;
		private UpdateChecker updateChecker;
		private Git git;
		private PasswordManager passwordManager;
		private INotificationService notificationService;

		public Program()
		{
			var h = Handle; // magic, do not touch
			hotkeys = new HotkeyManager();
			Name = "pass-winmenu (main window)";

			try
			{
				Initialise();
				RunInitialCheck();
			}
			catch (Exception e)
			{
				Log.Send("Could not start pass-winmenu: An exception occurred.", LogLevel.Error);
				Log.ReportException(e);
				notificationService.ShowErrorWindow($"pass-winmenu failed to start ({e.GetType().Name}: {e.Message})");
				Exit();
			}

		}

		/// <summary>
		/// Loads all required resources.
		/// </summary>
		protected void Initialise()
		{
			EmbeddedResources.Load();
			CreateNotifyIcon();
			LoadConfigFile();

#if DEBUG
			Log.Initialise();
#else
			if (ConfigManager.Config.CreateLogFile)
			{
				Log.Initialise();
			}
#endif
			Log.Send("------------------------------");
			Log.Send($"Starting pass-winmenu {Version}");
			Log.Send("------------------------------");

			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			
			var gpg = new GPG();
			gpg.FindGpgInstallation(ConfigManager.Config.Gpg.GpgPath);
			if (ConfigManager.Config.Gpg.GpgAgent.Config.AllowConfigManagement)
			{
				gpg.UpdateAgentConfig(ConfigManager.Config.Gpg.GpgAgent.Config.Keys);
			}

			notificationService = new Notifications(icon);
			dialogCreator = new DialogCreator(notificationService);
			passwordManager = new PasswordManager(ConfigManager.Config.PasswordStore.Location, EncryptedFileExtension, gpg);
			passwordManager.PinentryFixEnabled = ConfigManager.Config.Gpg.PinentryFix;

			if (ConfigManager.Config.Git.UseGit)
			{
				try
				{
					if (ConfigManager.Config.Git.SyncMode == SyncMode.NativeGit)
					{
						git = new Git(ConfigManager.Config.PasswordStore.Location, ConfigManager.Config.Git.GitPath);
					}
					else
					{
						git = new Git(ConfigManager.Config.PasswordStore.Location);
					}
				}
				catch (RepositoryNotFoundException)
				{
					// Password store doesn't appear to be a Git repository.
					// Git support will be disabled.
				}
				catch (TypeInitializationException e) when (e.InnerException is DllNotFoundException)
				{
					notificationService.ShowErrorWindow("The git2 DLL could not be found. Git support will be disabled.");
				}
				catch (Exception e)
				{
					notificationService.ShowErrorWindow($"Failed to open the password store Git repository ({e.GetType().Name}: {e.Message}). Git support will be disabled.");
				}
			}
			InitialiseUpdateChecker();

			AssignHotkeys(hotkeys);
		}

		private void InitialiseUpdateChecker()
		{
			if (!ConfigManager.Config.Application.UpdateChecking.CheckForUpdates) return;

#if CHOCOLATEY
			var updateSource = new ChocolateyUpdateSource();
#else
			var updateSource = new GitHubUpdateSource();
#endif
			var versionString = Version.Split('-').First();

			updateChecker = new UpdateChecker(updateSource, SemanticVersion.Parse(versionString, ParseMode.Lenient));
			updateChecker.UpdateAvailable += (sender, args) =>
			{
				// If the update contains important vulnerability fixes, always display a notification.
				if (ConfigManager.Config.Notifications.Types.UpdateAvailable || args.Version.Important)
				{
					icon.ContextMenuStrip.Items[2].Visible = true;
					notificationService.Raise($"A new update ({args.Version.VersionNumber.ToString(SemanticVersionFormat.Concise)}) is available.", Severity.Info);
				}
			};
			updateChecker.Start();
		}

		/// <summary>
		/// Checks if all components are configured correctly.
		/// </summary>
		private void RunInitialCheck()
		{
			if (!Directory.Exists(ConfigManager.Config.PasswordStore.Location))
			{
				notificationService.ShowErrorWindow($"Could not find the password store at {Path.GetFullPath(ConfigManager.Config.PasswordStore.Location)}. Please make sure it exists.");
				Exit();
				return;
			}
			try
			{
				// TODO: fetch GPG from somewhere else
				((GPG)passwordManager.Crypto).GetVersion();
			}
			catch (Win32Exception)
			{
				notificationService.ShowErrorWindow("Could not find GPG. Make sure your gpg-path is set correctly.");
				Exit();
				return;
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Failed to initialise GPG. {e.GetType().Name}: {e.Message}");
				Exit();
				return;
			}
			if (ConfigManager.Config.Gpg.GpgAgent.Preload)
			{
				Task.Run(() =>
				{
					try
					{
						// TODO: fetch GPG from somewhere else
						((GPG)passwordManager.Crypto).StartAgent();
					}
					catch (GpgError err)
					{
						notificationService.ShowErrorWindow(err.Message);
					}
					// Ignore other exceptions. If it turns out GPG is misconfigured,
					// these errors will surface upon decryption/encryption.
					// The reason we catch GpgErrors here is so we can notify the user
					// if we don't detect any decryption keys.
				});
			}
		}

		private void LoadConfigFile()
		{
			const string configFileName = "pass-winmenu.yaml";
			ConfigManager.LoadResult result;
			try
			{
				result = ConfigManager.Load(configFileName);
			}
			catch (Exception e) when (e.InnerException != null)
			{
				notificationService.ShowErrorWindow(
					$"The configuration file could not be loaded. An unhandled exception occurred.\n{e.InnerException.GetType().Name}: {e.InnerException.Message}",
					"Unable to load configuration file.");
				Exit();
				return;
			}
			catch (SemanticErrorException e)
			{
				notificationService.ShowErrorWindow(
					$"The configuration file could not be loaded. An unhandled exception occurred.\n{e.GetType().Name}: {e.Message}",
					"Unable to load configuration file.");
				Exit();
				return;
			}
			catch (YamlException e)
			{
				notificationService.ShowErrorWindow(
					$"The configuration file could not be loaded. An unhandled exception occurred.\n{e.GetType().Name}: {e.Message}",
					"Unable to load configuration file.");
				Exit();
				return;
			}

			switch (result)
			{
				case ConfigManager.LoadResult.FileCreationFailure:
					notificationService.Raise("A default configuration file was generated, but could not be saved.\nPass-winmenu will fall back to its default settings.", Severity.Error);
					break;
				case ConfigManager.LoadResult.NewFileCreated:
					var open = MessageBox.Show("A new configuration file has been generated. Please modify it according to your preferences and restart the application.\n\n" +
					                           "Would you like to open it now?", "New configuration file created", MessageBoxButton.YesNo);
					if (open == MessageBoxResult.Yes) Process.Start(configFileName);
					Exit();
					return;
				case ConfigManager.LoadResult.NeedsUpgrade:
					var backedUpFile = ConfigManager.Backup(configFileName);
					var openBoth = MessageBox.Show("The current configuration file is out of date. A new configuration file has been created, and the old file has been backed up.\n" +
					                               "Please edit the new configuration file according to your preferences and restart the application.\n\n" +
					                               "Would you like to open both files now?", "Configuration file out of date", MessageBoxButton.YesNo);
					if (openBoth == MessageBoxResult.Yes)
					{
						Process.Start(configFileName);
						Process.Start(backedUpFile);
					}
					Exit();
					return;
			}
		}

		private void Exit()
		{
			Close();
			Application.Exit();
			Environment.Exit(0);
		}

		protected override void SetVisibleCore(bool value)
		{
			// Do not allow this window to be made visible.
			base.SetVisibleCore(false);
		}

		/// <summary>
		/// Loads keybindings from the configuration file and registers them with Windows.
		/// </summary>
		private void AssignHotkeys(HotkeyManager hotkeyManager)
		{
			try
			{
				foreach (var hotkey in ConfigManager.Config.Hotkeys ?? new HotkeyConfig[]{})
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
							hotkeyManager.AddHotKey(keys, () => DecryptPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
							break;
						case HotkeyAction.AddPassword:
							hotkeyManager.AddHotKey(keys, AddPassword);
							break;
						case HotkeyAction.EditPassword:
							hotkeyManager.AddHotKey(keys, EditPassword);
							break;
						case HotkeyAction.GitPull:
							hotkeyManager.AddHotKey(keys, UpdatePasswordStore);
							break;
						case HotkeyAction.GitPush:
							hotkeyManager.AddHotKey(keys, CommitChanges);
							break;
						case HotkeyAction.OpenShell:
							// TODO: fetch GPG from somewhere else
							hotkeyManager.AddHotKey(keys, () => dialogCreator.OpenPasswordShell((GPG)passwordManager.Crypto));
							break;
						case HotkeyAction.ShowDebugInfo:
							hotkeyManager.AddHotKey(keys, () => dialogCreator.ShowDebugInfo(git, passwordManager));
							break;
						case HotkeyAction.CheckForUpdates:
							hotkeyManager.AddHotKey(keys, updateChecker.CheckForUpdates);
							break;
					}
				}
			}
			catch (Exception e) when (e is ArgumentException || e is HotkeyException)
			{
				notificationService.Raise(e.Message, Severity.Error);
				Exit();
			}
		}

		

		protected override void Dispose(bool disposing)
		{
			git?.Dispose();
			icon?.Dispose();
			hotkeys?.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>
		/// Creates a notification area icon for the application.
		/// </summary>
		private void CreateNotifyIcon()
		{
			icon.Icon = EmbeddedResources.Icon;
			icon.Visible = true;
			var menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripLabel("pass-winmenu " + Version));
			menu.Items.Add(new ToolStripSeparator());

			var downloadUpdate = new ToolStripMenuItem("Download Update");
			downloadUpdate.Click += (sender, args) => Process.Start(updateChecker.LatestVersion.Value.ReleaseNotes.ToString());
			downloadUpdate.BackColor = Color.Beige;
			downloadUpdate.Visible = false;

			menu.Items.Add(downloadUpdate);
			menu.Items.Add("Decrypt Password", null, (sender, args) => Task.Run(() => DecryptPassword(true, false, false)));
			menu.Items.Add("Add new Password", null, (sender, args) => Task.Run((Action)AddPassword));
			menu.Items.Add("Edit Password File", null, (sender, args) => Task.Run(() => EditPassword()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Push to Remote", null, (sender, args) => Task.Run((Action)CommitChanges));
			menu.Items.Add("Pull from Remote", null, (sender, args) => Task.Run((Action)UpdatePasswordStore));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Open Explorer", null, (sender, args) => Process.Start(ConfigManager.Config.PasswordStore.Location));
			// TODO: Fetch GPG from somewhere else
			menu.Items.Add("Open Shell", null, (sender, args) => Task.Run(() => dialogCreator.OpenPasswordShell((GPG)passwordManager.Crypto)));
			menu.Items.Add(new ToolStripSeparator());
			var startWithWindows = new ToolStripMenuItem("Start with Windows")
			{
				Checked = startupLink.Exists
			};
			startWithWindows.Click += (sender, args) =>
			{
				var target = Assembly.GetExecutingAssembly().Location;
				var workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
				startupLink.Toggle(target, workingDirectory);
				startWithWindows.Checked = startupLink.Exists;
			};

			menu.Items.Add(startWithWindows);
			menu.Items.Add("About", null, (sender, args) => Process.Start("https://github.com/Baggykiin/pass-winmenu#readme"));
			menu.Items.Add("Quit", null, (sender, args) => Close());
			icon.ContextMenuStrip = menu;
		}

		/// <summary>
		/// Commits all local changes and pushes them to remote.
		/// Also pulls any upcoming changes from remote.
		/// </summary>
		private void CommitChanges()
		{
			if (git == null)
			{
				notificationService.Raise("Unable to commit your changes: pass-winmenu is not configured to use Git.", Severity.Warning);
				return;
			}
			// First, commit any uncommitted files
			git.Commit();
			// Now fetch the latest changes
			try
			{
				git.Fetch();
			}
			catch (LibGit2SharpException e) when (e.Message == "unsupported URL protocol")
			{
				notificationService.ShowErrorWindow("Unable to push your changes: Remote uses an unknown protocol.\n\n" +
				                "If your remote URL is an SSH URL, try setting sync-mode to native-git in your configuration file.");
				return;
			}
			catch (GitException e)
			{
				if (e.GitError != null)
				{
					notificationService.ShowErrorWindow($"Unable to fetch the latest changes: Git returned an error.\n\n{e.GitError}");
				}
				else
				{
					notificationService.ShowErrorWindow($"Unable to fetch the latest changes: {e.Message}");
				}
			}
			var details = git.GetTrackingDetails();
			var local = details.AheadBy;
			var remote = details.BehindBy;
			try
			{
				git.Rebase();
			}
			catch (LibGit2SharpException e)
			{
				notificationService.ShowErrorWindow($"Unable to rebase your changes onto the tracking branch:\n{e.Message}");
				return;
			}
			git.Push();

			if (!ConfigManager.Config.Notifications.Types.GitPush) return;
			if (local > 0 && remote > 0)
			{
				notificationService.Raise($"All changes pushed to remote ({local} pushed, {remote} pulled)", Severity.Info);
			}
			else if (local.GetValueOrDefault() == 0 && remote.GetValueOrDefault() == 0)
			{
				notificationService.Raise("Nothing to commit.", Severity.Info);
			}
			else if (local > 0)
			{
				notificationService.Raise($"{local} changes have been pushed.", Severity.Info);
			}
			else if (remote > 0)
			{
				notificationService.Raise($"Nothing to commit. {remote} changes were pulled from remote.", Severity.Info);
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
			var passwordFilePath = dialogCreator.ShowFileSelectionWindow();
			// passwordFileName will be null if no file was selected
			if (passwordFilePath == null) return;

			// Display the password generation window.
			string password;
			string extraContent;
			using (var passwordWindow = new PasswordWindow(Path.GetFileName(passwordFilePath)))
			{
				passwordWindow.ShowDialog();
				if (!passwordWindow.DialogResult.GetValueOrDefault())
				{
					return;
				}
				password = passwordWindow.Password.Text;
				extraContent = passwordWindow.ExtraContent.Text.Replace(Environment.NewLine, "\n");
			}

			try
			{
				passwordManager.EncryptPassword(new DecryptedPasswordFile(passwordFilePath + passwordManager.EncryptedFileExtension, password, extraContent));
			}
			catch (GpgException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			catch (ConfigurationException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			// Copy the newly generated password.
			clipboard.Place(password, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));

			if (ConfigManager.Config.Notifications.Types.PasswordGenerated)
			{
				notificationService.Raise($"The new password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
			}
			// Add the password to Git
			git?.AddPassword(passwordFilePath + passwordManager.EncryptedFileExtension);
		}

		/// <summary>
		/// Updates the password store so it's in sync with remote again.
		/// </summary>
		private void UpdatePasswordStore()
		{
			if (git == null)
			{
				notificationService.Raise("Unable to update the password store: pass-winmenu is not configured to use Git.", Severity.Warning);
				return;
			}
			try
			{
				git.Fetch();
				git.Rebase();
			}
			catch (LibGit2SharpException e) when(e.Message == "unsupported URL protocol")
			{
				notificationService.ShowErrorWindow("Unable to update the password store: Remote uses an unknown protocol.\n\n" +
				                              "If your remote URL is an SSH URL, try setting sync-mode to native-git in your configuration file.");
			}
			catch (LibGit2SharpException e)
			{
				notificationService.ShowErrorWindow($"Unable to update the password store:\n{e.Message}");
			}
			catch (GitException e)
			{
				if (e.GitError != null)
				{
					notificationService.ShowErrorWindow($"Unable to fetch the latest changes: Git returned an error.\n\n{e.GitError}");
				}
				else
				{
					notificationService.ShowErrorWindow($"Unable to fetch the latest changes: {e.Message}");
				}
			}
		}


		private void EditPassword()
		{
			var selectedFile = RequestPasswordFile();
			if (selectedFile == null) return;

			if (ConfigManager.Config.Interface.PasswordEditor.UseBuiltin)
			{
				EditWithEditWindow(selectedFile);
			}
			else
			{
				EditWithTextEditor(selectedFile);
			}
		}

		private void EditWithEditWindow(string selectedFile)
		{
			if (InvokeRequired)
			{
				Invoke((Action<string>)EditWithEditWindow, selectedFile);
				return;
			}

			var content = passwordManager.DecryptText(selectedFile);
			using (var window = new EditWindow(selectedFile, content))
			{
				if (window.ShowDialog() ?? false)
				{
					try
					{
						File.Delete(passwordManager.GetPasswordFilePath(selectedFile));
						passwordManager.EncryptText(window.PasswordContent.Text, selectedFile);
						git?.EditPassword(selectedFile);
						if (ConfigManager.Config.Notifications.Types.PasswordUpdated)
						{
							notificationService.Raise($"Password file \"{selectedFile}\" has been updated.", Severity.Info);
						}
					}
					catch (Exception e)
					{
						notificationService.ShowErrorWindow($"Unable to save your password (encryption failed): {e.Message}");
					}
				}
			}
		}

		private void EditWithTextEditor(string selectedFile)
		{
			string decryptedFile, plaintextFile;
			try
			{
				decryptedFile = passwordManager.DecryptFile(selectedFile);
				plaintextFile = decryptedFile + PlaintextFileExtension;
				// Add a plaintext extension to the decrypted file so it can be opened with a text editor
				File.Move(decryptedFile, plaintextFile);
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Unable to edit your password (decryption failed): {e.Message}");
				return;
			}

			// Open the file in the user's default editor
			try
			{
				Process.Start(plaintextFile);
			}
			catch (Win32Exception e)
			{
				notificationService.ShowErrorWindow($"Unable to open an editor to edit your password file ({e.Message}).");
				File.Delete(plaintextFile);
				return;
			}

			var result = MessageBox.Show(
				"Please keep this window open until you're done editing the password file.\n" +
				"Then click Yes to save your changes, or No to discard them.",
				$"Save changes to {Path.GetFileName(selectedFile)}?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Information);

			if (result == MessageBoxResult.Yes)
			{
				var selectedFilePath = passwordManager.GetPasswordFilePath(selectedFile);
				File.Delete(selectedFilePath);
				// Remove the plaintext extension again before re-encrypting the file
				File.Move(plaintextFile, decryptedFile);
				passwordManager.EncryptFile(decryptedFile);
				File.Delete(decryptedFile);
				git?.EditPassword(selectedFile);
				if (ConfigManager.Config.Notifications.Types.PasswordUpdated)
				{
					notificationService.Raise($"Password file \"{selectedFile}\" has been updated.", Severity.Info);
				}
			}
			else
			{
				File.Delete(plaintextFile);
			}
		}

		/// <summary>
		/// Asks the user to choose a password file.
		/// </summary>
		/// <returns>
		/// The path to the chosen password file (relative to the password directory),
		/// or null if the user didn't choose anything.
		/// </returns>
		private string RequestPasswordFile()
		{
			if (InvokeRequired)
			{
				return (string)Invoke((Func<string>)RequestPasswordFile);
			}
			// Find GPG-encrypted password files
			var passFiles = passwordManager.GetPasswordFiles(ConfigManager.Config.PasswordStore.PasswordFileMatch).ToList();
			if (passFiles.Count == 0)
			{
				MessageBox.Show("Your password store doesn't appear to contain any passwords yet.", "Empty password store", MessageBoxButton.OK, MessageBoxImage.Information);
				return null;
			}
			// Build a dictionary mapping display names to relative paths
			var displayNameMap = passFiles.ToDictionary(val => val.Substring(0, val.Length - EncryptedFileExtension.Length).Replace(Path.DirectorySeparatorChar.ToString(), ConfigManager.Config.Interface.DirectorySeparator));

			var selection = dialogCreator.ShowPasswordMenu(displayNameMap.Keys);
			if (selection == null) return null;
			return displayNameMap[selection];
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

			DecryptedPasswordFile passFile;
			try
			{
				passFile = passwordManager.DecryptPassword(selectedFile, ConfigManager.Config.PasswordStore.FirstLineOnly);
			}
			catch (GpgError e)
			{
				notificationService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (GpgException e)
			{
				notificationService.ShowErrorWindow("Password decryption failed. " + e.Message);
				return;
			}
			catch (ConfigurationException e)
			{
				notificationService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Password decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
				return;
			}

			if (copyToClipboard)
			{
				clipboard.Place(passFile.Password, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));
				if (ConfigManager.Config.Notifications.Types.PasswordCopied)
				{
					notificationService.Raise($"The password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
				}
			}
			var usernameEntered = false;
			if (typeUsername)
			{
				var username = new PasswordFileParser().GetUsername(selectedFile, passFile.Metadata);
				if (username != null)
				{
					WindowsUtilities.EnterText(username, ConfigManager.Config.Output.DeadKeys);
					usernameEntered = true;
				}
			}
			if (typePassword)
			{
				// If a username has also been entered, press Tab to switch to the password field.
				if (usernameEntered) SendKeys.Send("{TAB}");

				WindowsUtilities.EnterText(passFile.Password, ConfigManager.Config.Output.DeadKeys);
			}
		}



		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Program());
		}
	}
}
