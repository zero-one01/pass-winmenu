using System.Diagnostics;
using System.IO;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.Windows
{
	internal class PasswordShellHelper
	{
		private readonly GpgInstallation installation;
		private readonly IGpgHomedirResolver homedirResolver;

		public PasswordShellHelper(GpgInstallation installation, IGpgHomedirResolver homedirResolver)
		{
			this.installation = installation;
			this.homedirResolver = homedirResolver;
		}

		public void OpenPasswordShell()
		{
			var powershell = new ProcessStartInfo
			{
				FileName = "powershell",
				WorkingDirectory = ConfigManager.Config.PasswordStore.Location,
				UseShellExecute = false
			};

			var gpgExe = installation.GpgExecutable.FullName;

			var homeDir = string.Empty;
			if (homedirResolver.GetConfiguredHomeDir() != null)
			{
				homeDir = $" --homedir \"{Path.GetFullPath(homedirResolver.GetConfiguredHomeDir())}\"";
			}
			powershell.Arguments = $"-NoExit -Command \"function gpg() {{ & '{gpgExe}'{homeDir} $args }}\"";
			Process.Start(powershell);
		}
	}
}
