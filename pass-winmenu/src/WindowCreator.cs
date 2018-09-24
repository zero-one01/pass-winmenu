using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Windows;
using System.Windows;
using NotifyIcon=System.Windows.Forms.NotifyIcon;
using ToolTipIcon=System.Windows.Forms.ToolTipIcon;

namespace PassWinmenu
{
	class WindowCreator
	{
		public NotifyIcon Icon { get; set; }
		
		/// <summary>
		/// Opens the password menu and displays it to the user, allowing them to choose an existing password file.
		/// </summary>
		/// <param name="options">A list of options the user can choose from.</param>
		/// <returns>One of the values contained in <paramref name="options"/>, or null if no option was chosen.</returns>
		internal string ShowPasswordMenu(IEnumerable<string> options)
		{
			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				RaiseNotification(e.Message, ToolTipIcon.Error);
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
		/// Opens the file selection menu and displays it to the user, allowing them to choose where they want to store a new password file.
		/// </summary>
		/// <returns>The path chosen by the user, or null if the action was aborted.</returns>
		internal string ShowFileSelectionWindow()
		{
			MainWindowConfiguration windowConfig;
			try
			{
				windowConfig = MainWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
			}
			catch (ConfigurationParseException e)
			{
				RaiseNotification(e.Message, ToolTipIcon.Error);
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

		/// <summary>
		/// Shows an error dialog to the user. Used for any error that results from an action initiated by a user and 
		/// prevents that action for being completed successfully, as well as any error that forces the application to exit.
		/// </summary>
		/// <param name="message">The error message to be displayed.</param>
		/// <param name="title">The window title of the error dialog.</param>
		internal void ShowErrorWindow(string message, string title = "An error occurred.")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}


		internal void ShowDebugInfo(Git git, PasswordManager passwordManager)
		{
			var gitData = "";
			if (git != null)
			{
				gitData = $"\tbehind by:\t{git.GetTrackingDetails().BehindBy}\n" +
				          $"\tahead by:\t\t{git.GetTrackingDetails().AheadBy}\n";
			}

			var debugInfo = $"gpg.exe path:\t\t{passwordManager.Gpg.GpgExePath}\n" +
			                $"gpg version:\t\t{passwordManager.Gpg.GetVersion()}\n" +
			                $"gpg homedir:\t\t{passwordManager.Gpg.GetConfiguredHomeDir()}\n" +
			                $"password store:\t\t{passwordManager.GetPasswordFilePath(".").TrimEnd('.')}\n" +
			                $"git enabled:\t\t{git != null}\n{gitData}";
			MessageBox.Show(debugInfo, "Debugging information", MessageBoxButton.OK, MessageBoxImage.None);
		}



		/// <summary>
		/// Presents a notification to the user, provided notifications are enabled.
		/// </summary>
		/// <param name="message">The message that should be displayed.</param>
		/// <param name="tipIcon">The type of icon that should be displayed next to the message.</param>
		/// <param name="timeout">The time period, in milliseconds, the notification should display.</param>
		internal void RaiseNotification(string message, ToolTipIcon tipIcon, int timeout = 5000)
		{
			if (ConfigManager.Config.Notifications.Enabled)
			{
				Icon.ShowBalloonTip(timeout, "pass-winmenu", message, tipIcon);
			}
		}
	}
}
