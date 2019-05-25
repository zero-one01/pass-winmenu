using System;
using System.IO;
using PassWinmenu.Configuration;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	class GpgHomedirResolver
	{
		/// <summary>
		/// Returns the path GPG will use as its home directory.
		/// </summary>
		/// <returns></returns>
		public string GetHomeDir() => GetConfiguredHomeDir() ?? GetDefaultHomeDir();

		/// <summary>
		/// Returns the home directory as configured by the user, or null if no home directory has been defined.
		/// </summary>
		/// <returns></returns>
		public string GetConfiguredHomeDir()
		{
			if (ConfigManager.Config.Gpg.GnupghomeOverride != null)
			{
				return ConfigManager.Config.Gpg.GnupghomeOverride;
			}
			return Environment.GetEnvironmentVariable("GNUPGHOME");
		}

		/// <summary>
		/// Returns the default home directory used by GPG when no user-defined home directory is available.
		/// </summary>
		/// <returns></returns>
		public string GetDefaultHomeDir()
		{
			var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path.Combine(appdata, "gnupg");
		}
	}
}
