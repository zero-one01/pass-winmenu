using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Windows;
using MessageBox = System.Windows.MessageBox;

namespace PassWinmenu
{
	internal class DialogCreator
	{
		private readonly INotificationService notificationService;

		public DialogCreator(INotificationService notifiWcationService)
		{
			this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
		}

		/// <summary>
		/// Opens a window where the user can choose the location for a new password file.
		/// </summary>
		/// <returns>The path to the file that the user has chosen</returns>
		public string ShowFileSelectionWindow()
		{
			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				notificationService.Raise(e.Message, Severity.Error);
				return null;
			}

			// Ask the user where the password file should be placed.
			var pathWindow = new FileSelectionWindow(ConfigManager.Config.PasswordStore.Location, windowConfig);
			pathWindow.ShowDialog();
			if (!pathWindow.Success)
			{
				return null;
			}
			return pathWindow.GetSelection();
		}

		/// <summary>
		/// Opens the password menu and displays it to the user, allowing them to choose an existing password file.
		/// </summary>
		/// <param name="options">A list of options the user can choose from.</param>
		/// <returns>One of the values contained in <paramref name="options"/>, or null if no option was chosen.</returns>
		public string ShowPasswordMenu(IEnumerable<string> options)
		{
			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				notificationService.Raise(e.Message, Severity.Error);
				return null;
			}

			var menu = new PasswordSelectionWindow(options, windowConfig);
			menu.ShowDialog();
			if (menu.Success)
			{
				return menu.GetSelection();
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Opens a password shell in which the user can manage their passwords.
		/// </summary>
		/// <param name="gpg">
		/// A reference to an existing GPG installation.
		/// A gpg() function will be created in the shell and pointed to this installation.
		/// </param>
		internal void OpenPasswordShell(GPG gpg)
		{
			var powershell = new ProcessStartInfo
			{
				FileName = "powershell",
				WorkingDirectory = ConfigManager.Config.PasswordStore.Location,
				UseShellExecute = false
			};

			var gpgLocation = gpg.GpgExePath;
			if (gpgLocation.Contains(Path.DirectorySeparatorChar.ToString()) || gpgLocation.Contains(Path.AltDirectorySeparatorChar.ToString()))
			{
				// gpgLocation is a path, so ensure it's absolute.
				gpgLocation = Path.GetFullPath(gpgLocation);
			}
			else if (gpgLocation == "gpg")
			{
				// This would conflict with our function name, so rename it to gpg.exe.
				gpgLocation = "gpg.exe";
			}

			var homeDir = gpg.GetConfiguredHomeDir();
			if (homeDir != null)
			{
				homeDir = $" --homedir \"{Path.GetFullPath(homeDir)}\"";
			}
			powershell.Arguments = $"-NoExit -Command \"function gpg() {{ & '{gpgLocation}'{homeDir} $args }}\"";
			Process.Start(powershell);
		}

		public void ShowDebugInfo(Git git, PasswordManager passwordManager)
		{
			var gitData = "";
			if (git != null)
			{
				gitData = $"\tbehind by:\t{git.GetTrackingDetails().BehindBy}\n" +
				          $"\tahead by:\t\t{git.GetTrackingDetails().AheadBy}\n";
			}
			// TODO: fetch GPG from somewhere else
			var gpg = (GPG)passwordManager.Crypto;

			var debugInfo = $"gpg.exe path:\t\t{gpg.GpgExePath}\n" +
			                $"gpg version:\t\t{gpg.GetVersion()}\n" +
			                $"gpg homedir:\t\t{gpg.GetConfiguredHomeDir()}\n" +
			                $"password store:\t\t{passwordManager.GetPasswordFilePath(".").TrimEnd('.')}\n" +
			                $"git enabled:\t\t{git != null}\n{gitData}";
			MessageBox.Show(debugInfo, "Debugging information", MessageBoxButton.OK, MessageBoxImage.None);
		}
	}
}
