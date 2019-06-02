using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using LibGit2Sharp;

using McSherry.SemanticVersioning;

using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Hotkeys;
using PassWinmenu.PasswordManagement;
using PassWinmenu.UpdateChecking;
using PassWinmenu.UpdateChecking.Chocolatey;
using PassWinmenu.UpdateChecking.Dummy;
using PassWinmenu.UpdateChecking.GitHub;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

using YamlDotNet.Core;
using IContainer = Autofac.IContainer;

namespace PassWinmenu
{
	internal sealed class Program : IDisposable
	{
		public static string Version => EmbeddedResources.Version;

		public const string LastConfigVersion = "1.7";
		public const string EncryptedFileExtension = ".gpg";
		public const string PlaintextFileExtension = ".txt";
		public const string ConfigFileName = @".\pass-winmenu.yaml";

		private ActionDispatcher actionDispatcher;
		private HotkeyManager hotkeys;
		private DialogCreator dialogCreator;
		private UpdateChecker updateChecker;
		private ISyncService git;
		private PasswordManager passwordManager;
		private Notifications notificationService;

		private IContainer container;

		public Program()
		{
			try
			{
				Initialise();
				RunInitialCheck();
			}
			catch (Exception e)
			{
				Log.EnableFileLogging();
				Log.Send("Could not start pass-winmenu: An exception occurred.", LogLevel.Error);
				Log.ReportException(e);

				string errorMessage = $"pass-winmenu failed to start ({e.GetType().Name}: {e.Message})";
				if (notificationService == null)
				{
					// We have no notification service yet. Instantiating one is risky,
					// so we'll make do with a call to MessageBox.Show() instead.
					MessageBox.Show(errorMessage, "An error occurred.", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					notificationService.ShowErrorWindow(errorMessage);
				}
				Exit();
			}
		}

		/// <summary>
		/// Loads all required resources.
		/// </summary>
		private void Initialise()
		{
			// Load compiled-in resources.
			EmbeddedResources.Load();

			Log.Send("------------------------------");
			Log.Send($"Starting pass-winmenu {Version}");
			Log.Send("------------------------------");

			// Set the security protocol to TLS 1.2 only.
			// We only use this for update checking (Git push over HTTPS is not handled by .NET).
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			// Initialise the DI Container builder.
			var builder = new ContainerBuilder();


			// Create the notification service first, so it's available if initialisation fails.
			notificationService = Notifications.Create();
			builder.Register(_ => notificationService)
				.AsImplementedInterfaces()
				.SingleInstance();
			
			// Now load the configuration options that we'll need 
			// to continue initialising the rest of the applications.
			LoadConfigFile();

			builder.Register(_ => ConfigManager.Config).AsSelf();
			builder.Register(_ => ConfigManager.Config.Gpg).AsSelf();

#if DEBUG
			Log.EnableFileLogging();
#else
			if (ConfigManager.Config.CreateLogFile)
			{
				Log.EnableFileLogging();
			}
#endif

			// Register environment wrappers
			builder.RegisterTypes(
				typeof(FileSystem),
				typeof(SystemEnvironment),
				typeof(Processes),
				typeof(ExecutablePathResolver)
			).AsImplementedInterfaces();

			// Register GPG types
			builder.RegisterTypes(
					typeof(GpgInstallationFinder),
					typeof(GpgHomedirResolver),
					typeof(GpgAgentConfigReader),
					typeof(GpgAgentConfigUpdater),
					typeof(GpgTransport),
					typeof(GpgAgent),
					typeof(GpgResultVerifier)
				).AsImplementedInterfaces()
				.AsSelf();

			// Register GPG installation
			builder.Register(context => context.Resolve<GpgInstallationFinder>().FindGpgInstallation(ConfigManager.Config.Gpg.GpgPath));

			// Register GPG
			builder.Register(context => new GPG(
					context.Resolve<IGpgTransport>(),
					context.Resolve<IGpgAgent>(),
					context.Resolve<IGpgResultVerifier>(),
					context.Resolve<GpgConfig>().PinentryFix))
				.AsImplementedInterfaces()
				.AsSelf();

			container = builder.Build();

			var gpgConfig = container.Resolve<GpgConfig>();
			if (gpgConfig.GpgAgent.Config.AllowConfigManagement)
			{
				container.Resolve<GpgAgentConfigUpdater>().UpdateAgentConfig(gpgConfig.GpgAgent.Config.Keys);
			}

			var fileSystem = container.Resolve<IFileSystem>();

			// Create the Git wrapper, if enabled.
			InitialiseGit(ConfigManager.Config.Git, ConfigManager.Config.PasswordStore.Location);

			// Initialise the internal password manager.
			var passwordStore = fileSystem.DirectoryInfo.FromDirectoryName(ConfigManager.Config.PasswordStore.Location);
			var recipientFinder = new GpgRecipientFinder(passwordStore);
			passwordManager = new PasswordManager(passwordStore, container.Resolve<ICryptoService>(), recipientFinder);

			dialogCreator = new DialogCreator(notificationService, passwordManager, git);
			InitialiseUpdateChecker();

			var actions = new Dictionary<HotkeyAction, IAction>
			{
			};

			actionDispatcher = new ActionDispatcher(dialogCreator, actions);

			notificationService.AddMenuActions(actionDispatcher);

			// Assign our hotkeys.
			hotkeys = new HotkeyManager();
			AssignHotkeys(hotkeys);
		}

		private void InitialiseUpdateChecker()
		{
			var updateCfg = ConfigManager.Config.Application.UpdateChecking;
			if (!updateCfg.CheckForUpdates) return;

			IUpdateSource updateSource;
			switch (updateCfg.UpdateSource)
			{
				case UpdateSource.GitHub:
					updateSource = new GitHubUpdateSource();
					break;
				case UpdateSource.Chocolatey:
					updateSource = new ChocolateyUpdateSource();
					break;
				case UpdateSource.Dummy:
					updateSource = new DummyUpdateSource
					{
						Versions = new List<ProgramVersion>
						{
							new ProgramVersion
							{
								VersionNumber = new SemanticVersion(10, 0, 0),
								Important = true,
							},
							new ProgramVersion
							{
								VersionNumber = SemanticVersion.Parse("v11.0-pre1", ParseMode.Lenient),
								IsPrerelease = true,
							},
						}
					};
					break;
				default:
					throw new ArgumentOutOfRangeException(null, "Invalid update provider.");
			}
			var versionString = Version.Split('-').First();

			updateChecker = new UpdateChecker(updateSource,
			                                  SemanticVersion.Parse(versionString, ParseMode.Lenient),
			                                  updateCfg.AllowPrereleases,
			                                  updateCfg.CheckIntervalTimeSpan,
			                                  updateCfg.InitialDelayTimeSpan);

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
					git = new SyncServiceFactory().BuildSyncService(config, passwordStorePath);
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
			var gpg = container.Resolve<GPG>();

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
			catch (System.ComponentModel.Win32Exception)
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
			LoadResult result;
			try
			{
				result = ConfigManager.Load(ConfigFileName);
			}
			catch (Exception e) when (e.InnerException != null)
			{
				if (e is YamlException)
				{
					notificationService.ShowErrorWindow(
						$"The configuration file could not be loaded: {e.Message}\n\n{e.InnerException.GetType().Name}: {e.InnerException.Message}",
						"Unable to load configuration file.");
				}
				else
				{
					notificationService.ShowErrorWindow(
						$"The configuration file could not be loaded. An unhandled exception occurred.\n{e.InnerException.GetType().Name}: {e.InnerException.Message}",
						"Unable to load configuration file.");
				}

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
				case LoadResult.FileCreationFailure:
					notificationService.Raise("A default configuration file was generated, but could not be saved.\nPass-winmenu will fall back to its default settings.", Severity.Error);
					break;
				case LoadResult.NewFileCreated:
					var open = MessageBox.Show("A new configuration file has been generated. Please modify it according to your preferences and restart the application.\n\n" +
					                           "Would you like to open it now?", "New configuration file created", MessageBoxButton.YesNo);
					if (open == MessageBoxResult.Yes) Process.Start(ConfigFileName);
					Exit();
					return;
				case LoadResult.NeedsUpgrade:
					var backedUpFile = ConfigManager.Backup(ConfigFileName);
					var openBoth = MessageBox.Show("The current configuration file is out of date. A new configuration file has been created, and the old file has been backed up.\n" +
					                               "Please edit the new configuration file according to your preferences and restart the application.\n\n" +
					                               "Would you like to open both files now?", "Configuration file out of date", MessageBoxButton.YesNo);
					if (openBoth == MessageBoxResult.Yes)
					{
						Process.Start(ConfigFileName);
						Process.Start(backedUpFile);
					}
					Exit();
					return;
			}
			if (ConfigManager.Config.Application.ReloadConfig)
			{
				ConfigManager.EnableAutoReloading(ConfigFileName);
				Log.Send("Config reloading enabled");
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
				Log.Send("Failed to register hotkeys", LogLevel.Error);
				Log.ReportException(e);

				notificationService.ShowErrorWindow(e.Message, "Could not register hotkeys");
				Exit();
			}
		}

		public static void Exit()
		{
			Log.Send("Shutting down.");
			Environment.Exit(0);
		}

		public void Dispose()
		{
			notificationService?.Dispose();
			hotkeys?.Dispose();
			updateChecker?.Dispose();
		}
	}
}
