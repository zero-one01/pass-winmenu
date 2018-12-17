using System;

namespace PassWinmenu.Configuration
{
	internal class UpdateCheckingConfig
	{
		public bool CheckForUpdates { get; set; } = true;
		public UpdateProvider UpdateProvider { get; set; } = UpdateProvider.GitHub;
		public int CheckInterval { get; set; } = (int)TimeSpan.FromHours(1).TotalSeconds;
		public int InitialDelay { get; set; } = (int)TimeSpan.FromHours(1).TotalSeconds;
	}
}
