using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
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
using PassWinmenu.PasswordManagement;
using PassWinmenu.Actions;

namespace PassWinmenu
{
	internal sealed class Program : IDisposable
	{
		public static string Version => EmbeddedResources.Version;

		public const string LastConfigVersion = "1.7";
		public const string EncryptedFileExtension = ".gpg";
		public const string PlaintextFileExtension = ".txt";

		private ClipboardHelper clipboard;
		private ActionDispatcher actionDispatcher;
		private HotkeyManager hotkeys;
		private DialogCreator dialogCreator;
		private UpdateChecker updateChecker;
		private Git git;
		private GPG gpg;
		private PasswordManager passwordManager;
		private Notifications notificationService;

		public Program()
		{
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
#if DEBUG
			Log.Initialise();
#else
			if (ConfigManager.Config.CreateLogFile)
			{
				Log.Initialise();
			}
#endif
			// Load compiled-in resources.
			EmbeddedResources.Load();

			Log.Send("------------------------------");
			Log.Send($"Starting pass-winmenu {Version}");
			Log.Send("------------------------------");

			// Set the security protocol to TLS 1.2 only.
			// We only use this for update checking (Git push over HTTPS is not handled by .NET).
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			// Create the notification service first, so it's available if initialisation fails.
			notificationService = Notifications.Create();

			// Now load the configuration options that we'll need 
			// to continue initialising the rest of the applications.
			LoadConfigFile();

			// Create the GPG wrapper.
			gpg = new GPG();
			gpg.FindGpgInstallation(ConfigManager.Config.Gpg.GpgPath);
			if (ConfigManager.Config.Gpg.GpgAgent.Config.AllowConfigManagement)
			{
				gpg.UpdateAgentConfig(ConfigManager.Config.Gpg.GpgAgent.Config.Keys);
			}
			// Create the Git wrapper, if enabled.
			InitialiseGit(ConfigManager.Config.Git, ConfigManager.Config.PasswordStore.Location);

			// Initialise the internal password manager.
			passwordManager = new PasswordManager(ConfigManager.Config.PasswordStore.Location, EncryptedFileExtension, gpg);
			passwordManager.PinentryFixEnabled = ConfigManager.Config.Gpg.PinentryFix;

			clipboard = new ClipboardHelper();
			dialogCreator = new DialogCreator(notificationService, passwordManager, git);
			actionDispatcher = new ActionDispatcher(notificationService, passwordManager, dialogCreator, clipboard, git);

			notificationService.AddMenuActions(actionDispatcher);

			// Assign our hotkeys.
			hotkeys = new HotkeyManager();
			AssignHotkeys(hotkeys);

			InitialiseUpdateChecker();
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
				notificationService.HandleUpdateAvailable(args);
			};
			updateChecker.Start();
		}

		private void InitialiseGit(GitConfig config, string passwordStorePath)
		{
			if (config.UseGit)
			{
				try
				{
					if (config.SyncMode == SyncMode.NativeGit)
					{
						git = new Git(passwordStorePath, config.GitPath);
					}
					else
					{
						git = new Git(passwordStorePath);
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
				Log.Send("Using GPG version " + gpg.GetVersion());
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
						gpg.StartAgent();
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

		/// <summary>
		/// Loads keybindings from the configuration file and registers them with Windows.
		/// </summary>
		private void AssignHotkeys(HotkeyManager hotkeyManager)
		{
			try
			{
				hotkeyManager.AssignHotkeys(
					ConfigManager.Config.Hotkeys ?? new HotkeyConfig[0],
					actionDispatcher,
					notificationService);
			}
			catch (Exception e) when (e is ArgumentException || e is HotkeyException)
			{
				notificationService.ShowErrorWindow(e.Message, "Could not register hotkeys");
				Exit();
			}
		}

		public static void Exit()
		{
			Environment.Exit(0);
		}

		public void Dispose()
		{
			git?.Dispose();
			notificationService?.Dispose();
			hotkeys?.Dispose();
		}
	}
}
