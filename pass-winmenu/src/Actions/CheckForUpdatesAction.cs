using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.UpdateChecking;
using PassWinmenu.WinApi;

namespace PassWinmenu.Actions
{
	class CheckForUpdatesAction : IAction
	{
		private readonly UpdateChecker updateChecker;
		private readonly INotificationService notificationService;

		public CheckForUpdatesAction(UpdateChecker updateChecker, INotificationService notificationService)
		{
			this.updateChecker = updateChecker;
			this.notificationService = notificationService;
		}

		public void Execute()
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
	}
}
