using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using LibGit2Sharp;
using McSherry.SemanticVersioning;
using PassWinmenu.Hotkeys;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.UpdateChecking;
using PassWinmenu.UpdateChecking.GitHub;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;
using YamlDotNet.Core;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace PassWinmenu
{
	internal sealed class Program : Form
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
		private void Initialise()
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
			passwordManager = new PasswordManager(ConfigManager.Config.PasswordStore.Location, EncryptedFileExtension, gpg);
			passwordManager.PinentryFixEnabled = ConfigManager.Config.Gpg.PinentryFix;
			dialogCreator = new DialogCreator(notificationService, passwordManager, git);

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
				hotkeyManager.AssignHotkeys(
					ConfigManager.Config.Hotkeys ?? new HotkeyConfig[0],
					dialogCreator,
					updateChecker,
					notificationService,
					this);
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
			menu.Items.Add("Add new Password", null, (sender, args) => Task.Run((Action)dialogCreator.AddPassword));
			menu.Items.Add("Edit Password File", null, (sender, args) => Task.Run((Action)dialogCreator.EditPassword));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Push to Remote", null, (sender, args) => Task.Run((Action)CommitChanges));
			menu.Items.Add("Pull from Remote", null, (sender, args) => Task.Run((Action)UpdatePasswordStore));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Open Explorer", null, (sender, args) => Process.Start(ConfigManager.Config.PasswordStore.Location));
			// TODO: Fetch GPG from somewhere else
			menu.Items.Add("Open Shell", null, (sender, args) => Task.Run(() => dialogCreator.OpenPasswordShell()));
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
		public void CommitChanges()
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
		/// Updates the password store so it's in sync with remote again.
		/// </summary>
		public void UpdatePasswordStore()
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


		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		public void DecryptPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			// We need to be on the main thread for this.
			if (InvokeRequired)
			{
				Invoke((Action<bool, bool, bool>)DecryptPassword, copyToClipboard, typeUsername, typePassword);
				return;
			}

			var selectedFile = dialogCreator.RequestPasswordFile();
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
