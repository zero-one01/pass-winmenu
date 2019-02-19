using System;
using System.Diagnostics;
using System.Threading;
using LibGit2Sharp;
using McSherry.SemanticVersioning;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.PasswordManagement;
using PassWinmenu.UpdateChecking;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	class ActionDispatcher
	{
		private readonly INotificationService notificationService;
		private readonly DialogCreator dialogCreator;
		private readonly ISyncService syncService;
		private readonly UpdateChecker updateChecker;

		public ActionDispatcher(INotificationService notificationService,
		                        DialogCreator        dialogCreator,
		                        ISyncService         syncService,
		                        UpdateChecker        updateChecker)
		{
			this.notificationService = notificationService;
			this.dialogCreator = dialogCreator;
			this.syncService = syncService;
			this.updateChecker = updateChecker;
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
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		public void DecryptPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			dialogCreator.DecryptPassword(copyToClipboard, typeUsername, typePassword);
		}

		public void DecryptMetadata(bool copyToClipboard, bool type)
		{
			dialogCreator.DecryptMetadata(copyToClipboard, type);
		}

		public void DecryptPasswordField(bool copyToClipboard, bool type, string fieldName = null)
		{
			dialogCreator.GetKey(copyToClipboard, type, fieldName);
		}

		/// <summary>
		/// Commits all local changes and pushes them to remote.
		/// Also pulls any upcoming changes from remote.
		/// </summary>
		public void CommitChanges()
		{
			if (syncService == null)
			{
				notificationService.Raise("Unable to commit your changes: pass-winmenu is not configured to use Git.",
				                          Severity.Warning);
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
				notificationService.ShowErrorWindow(
					"Unable to push your changes: Remote uses an unknown protocol.\n\n" +
					"If your remote URL is an SSH URL, try setting sync-mode to native-git in your configuration file.");
				return;
			}
			catch (GitException e)
			{
				if (e.GitError != null)
				{
					notificationService.ShowErrorWindow(
						$"Unable to fetch the latest changes: Git returned an error.\n\n{e.GitError}");
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
				notificationService.ShowErrorWindow(
					$"Unable to rebase your changes onto the tracking branch:\n{e.Message}");
				return;
			}

			syncService.Push();

			if (!ConfigManager.Config.Notifications.Types.GitPush) return;
			if (local > 0 && remote > 0)
			{
				notificationService.Raise($"All changes pushed to remote ({local} pushed, {remote} pulled)",
				                          Severity.Info);
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
				notificationService.Raise($"Nothing to commit. {remote} changes were pulled from remote.",
				                          Severity.Info);
			}
		}

		internal void CheckForUpdates()
		{
			if (updateChecker == null)
			{
				notificationService.Raise($"Update checking is disabled in the configuration file.",
				                          Severity.Info);
				return;
			}

			if (!updateChecker.CheckForUpdates())
			{
				var latest = updateChecker.LatestVersion;
				if (latest == null)
				{
					notificationService.Raise($"Unable to find update information.",
					                          Severity.Info);
				}
				else
				{
					notificationService.Raise($"No new updates available (latest available version is " +
					                          $"{latest.Value}).",
					                          Severity.Info);
				}

			}
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
				notificationService.Raise(
					"Unable to update the password store: pass-winmenu is not configured to use Git.",
					Severity.Warning);
				return;
			}

			try
			{
				syncService.Fetch();
				syncService.Rebase();
			}
			catch (LibGit2SharpException e) when (e.Message == "unsupported URL protocol")
			{
				notificationService.ShowErrorWindow(
					"Unable to update the password store: Remote uses an unknown protocol.\n\n" +
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
					notificationService.ShowErrorWindow(
						$"Unable to fetch the latest changes: Git returned an error.\n\n{e.GitError}");
				}
				else
				{
					notificationService.ShowErrorWindow($"Unable to fetch the latest changes: {e.Message}");
				}
			}
		}

		public void ViewLogs()
		{
			dialogCreator.ViewLogs();
		}

		public void EditConfiguration()
		{
			Process.Start(Program.ConfigFileName);
		}
	}
}
