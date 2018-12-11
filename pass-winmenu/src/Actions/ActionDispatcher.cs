using System;
using System.Diagnostics;
using System.Threading;
using LibGit2Sharp;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	class ActionDispatcher
	{
		private readonly INotificationService notificationService;
		private readonly IPasswordManager passwordManager;
		private readonly DialogCreator dialogCreator;
		private readonly ClipboardHelper clipboard;
		private readonly ISyncService syncService;

		public ActionDispatcher(INotificationService notificationService,
		                        IPasswordManager     passwordManager,
		                        DialogCreator        dialogCreator,
		                        ClipboardHelper      clipboard,
		                        ISyncService         syncService)
		{
			this.notificationService = notificationService;
			this.passwordManager = passwordManager;
			this.dialogCreator = dialogCreator;
			this.clipboard = clipboard;
			this.syncService = syncService;
		}

		public void OpenExplorer()
		{
			Process.Start(ConfigManager.Config.PasswordStore.Location);
		}

		public void AddPassword()
		{
			dialogCreator.AddPassword();
		}

		public void EditPassword()
		{
			dialogCreator.EditPassword();
		}

		/// <summary>
		/// Commits all local changes and pushes them to remote.
		/// Also pulls any upcoming changes from remote.
		/// </summary>
		public void CommitChanges()
		{
			if (syncService == null)
			{
				notificationService.Raise("Unable to commit your changes: pass-winmenu is not configured to use Git.", Severity.Warning);
				return;
			}
			// First, commit any uncommitted files
			syncService.Commit();
			// Now fetch the latest changes
			try
			{
				syncService.Fetch();
			}
			// FIXME: dependency on derived type
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
			var details = syncService.GetTrackingDetails();
			var local = details.AheadBy;
			var remote = details.BehindBy;
			try
			{
				syncService.Rebase();
			}
			catch (LibGit2SharpException e)
			{
				notificationService.ShowErrorWindow($"Unable to rebase your changes onto the tracking branch:\n{e.Message}");
				return;
			}
			syncService.Push();

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

		internal void CheckForUpdates()
		{
			throw new NotImplementedException();
		}

		internal void ShowDebugInfo()
		{
			dialogCreator.ShowDebugInfo();
		}

		internal void OpenPasswordShell()
		{
			dialogCreator.OpenPasswordShell();
		}

		/// <summary>
		/// Updates the password store so it's in sync with remote again.
		/// </summary>
		public void UpdatePasswordStore()
		{
			if (syncService == null)
			{
				notificationService.Raise("Unable to update the password store: pass-winmenu is not configured to use Git.", Severity.Warning);
				return;
			}
			try
			{
				syncService.Fetch();
				syncService.Rebase();
			}
			catch (LibGit2SharpException e) when (e.Message == "unsupported URL protocol")
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
					KeyboardEmulator.EnterText(username, ConfigManager.Config.Output.DeadKeys);
					usernameEntered = true;
				}
			}
			if (typePassword)
			{
				// If a username has also been entered, press Tab to switch to the password field.
				if (usernameEntered) KeyboardEmulator.EnterRawText("{TAB}");

				KeyboardEmulator.EnterText(passFile.Password, ConfigManager.Config.Output.DeadKeys);
			}
		}
	}
}
