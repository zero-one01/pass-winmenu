using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;

namespace PassWinmenu.Windows
{
	internal class DialogCreator
	{
		private readonly INotificationService notificationService;
		private readonly ISyncService syncService;
		private readonly PasswordManager passwordManager;
		private readonly ClipboardHelper clipboard = new ClipboardHelper();

		public DialogCreator(INotificationService notificationService, PasswordManager passwordManager, ISyncService syncService)
		{
			this.syncService = syncService;
			this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
			this.passwordManager = passwordManager ?? throw new ArgumentNullException(nameof(passwordManager));
		}

		private void EnsureStaThread()
		{
			
		}

		/// <summary>
		/// Opens a window where the user can choose the location for a new password file.
		/// </summary>
		/// <returns>The path to the file that the user has chosen</returns>
		public string ShowFileSelectionWindow()
		{
			SelectionWindowConfiguration windowConfig;
			try
			{
				windowConfig = SelectionWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
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
			SelectionWindowConfiguration windowConfig;
			try
			{
				windowConfig = SelectionWindowConfiguration.ParseMainWindowConfiguration(ConfigManager.Config);
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
			return null;
		}

		/// <summary>
		/// Opens a password shell in which the user can manage their passwords.
		/// A gpg() function will be created in the shell and pointed to the local GPG installation.
		/// </summary>
		internal void OpenPasswordShell()
		{
			var powershell = new ProcessStartInfo
			{
				FileName = "powershell",
				WorkingDirectory = ConfigManager.Config.PasswordStore.Location,
				UseShellExecute = false
			};

			// TODO: fetch GPG from somewhere else
			var gpg = (GPG)passwordManager.Crypto;
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
		/// Display some information about configured variables.
		/// </summary>
		public void ShowDebugInfo()
		{
			// TODO: fetch Git reference from somewhere else
			var git = (Git)syncService;
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

		/// <summary>
		/// Asks the user to choose a password file.
		/// </summary>
		/// <returns>
		/// The path to the chosen password file (relative to the password directory),
		/// or null if the user didn't choose anything.
		/// </returns>
		public string RequestPasswordFile()
		{
			EnsureStaThread();

			// Find GPG-encrypted password files
			var passFiles = passwordManager.GetPasswordFiles(ConfigManager.Config.PasswordStore.PasswordFileMatch).ToList();
			if (passFiles.Count == 0)
			{
				MessageBox.Show("Your password store doesn't appear to contain any passwords yet.", "Empty password store", MessageBoxButton.OK, MessageBoxImage.Information);
				return null;
			}
			// Build a dictionary mapping display names to relative paths
			var displayNameMap = passFiles.ToDictionary(val => val.Substring(0, val.Length - Program.EncryptedFileExtension.Length).Replace(Path.DirectorySeparatorChar.ToString(), ConfigManager.Config.Interface.DirectorySeparator));

			var selection = ShowPasswordMenu(displayNameMap.Keys);
			if (selection == null) return null;
			return displayNameMap[selection];
		}


		public void EditWithEditWindow(string selectedFile)
		{
			EnsureStaThread();
			string content;
			try
			{
				content = passwordManager.DecryptText(selectedFile);
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Unable to edit your password (decryption failed): {e.Message}");
				return;
			}
			using (var window = new EditWindow(selectedFile, content, ConfigManager.Config.PasswordStore.PasswordGeneration))
			{
				if (window.ShowDialog() ?? false)
				{
					try
					{
						File.Delete(passwordManager.GetPasswordFilePath(selectedFile));
						passwordManager.EncryptText(window.PasswordContent.Text, selectedFile);
						syncService?.EditPassword(selectedFile);
						if (ConfigManager.Config.Notifications.Types.PasswordUpdated)
						{
							notificationService.Raise($"Password file \"{selectedFile}\" has been updated.", Severity.Info);
						}
					}
					catch (Exception e)
					{
						notificationService.ShowErrorWindow($"Unable to save your password (encryption failed): {e.Message}");
					}
				}
			}
		}

		public void EditPassword()
		{
			var selectedFile = RequestPasswordFile();
			if (selectedFile == null) return;

			if (ConfigManager.Config.Interface.PasswordEditor.UseBuiltin)
			{
				EditWithEditWindow(selectedFile);
			}
			else
			{
				EditWithTextEditor(selectedFile);
			}
		}

		public void EditWithTextEditor(string selectedFile)
		{
			string decryptedFile, plaintextFile;
			try
			{
				decryptedFile = passwordManager.DecryptFile(selectedFile);
				plaintextFile = decryptedFile + Program.PlaintextFileExtension;
				// Add a plaintext extension to the decrypted file so it can be opened with a text editor
				File.Move(decryptedFile, plaintextFile);
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Unable to edit your password (decryption failed): {e.Message}");
				return;
			}

			// Open the file in the user's default editor
			try
			{
				Process.Start(plaintextFile);
			}
			catch (Win32Exception e)
			{
				notificationService.ShowErrorWindow($"Unable to open an editor to edit your password file ({e.Message}).");
				File.Delete(plaintextFile);
				return;
			}

			var result = MessageBox.Show(
				"Please keep this window open until you're done editing the password file.\n" +
				"Then click Yes to save your changes, or No to discard them.",
				$"Save changes to {Path.GetFileName(selectedFile)}?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Information);

			if (result == MessageBoxResult.Yes)
			{
				var selectedFilePath = passwordManager.GetPasswordFilePath(selectedFile);
				File.Delete(selectedFilePath);
				// Remove the plaintext extension again before re-encrypting the file
				File.Move(plaintextFile, decryptedFile);
				passwordManager.EncryptFile(decryptedFile);
				File.Delete(decryptedFile);
				syncService?.EditPassword(selectedFile);
				if (ConfigManager.Config.Notifications.Types.PasswordUpdated)
				{
					notificationService.Raise($"Password file \"{selectedFile}\" has been updated.", Severity.Info);
				}
			}
			else
			{
				File.Delete(plaintextFile);
			}
		}



		/// <summary>
		/// Adds a new password to the password store.
		/// </summary>
		public void AddPassword()
		{
			EnsureStaThread();

			var passwordFilePath = ShowFileSelectionWindow();
			// passwordFileName will be null if no file was selected
			if (passwordFilePath == null) return;

			// Display the password generation window.
			string password;
			string extraContent;
			using (var passwordWindow = new PasswordWindow(Path.GetFileName(passwordFilePath), ConfigManager.Config.PasswordStore.PasswordGeneration))
			{
				passwordWindow.ShowDialog();
				if (!passwordWindow.DialogResult.GetValueOrDefault())
				{
					return;
				}
				password = passwordWindow.Password.Text;
				extraContent = passwordWindow.ExtraContent.Text.Replace(Environment.NewLine, "\n");
			}

			try
			{
				passwordManager.EncryptPassword(new DecryptedPasswordFile(passwordFilePath + passwordManager.EncryptedFileExtension, password, extraContent));
			}
			catch (GpgException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			catch (ConfigurationException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			// Copy the newly generated password.
			clipboard.Place(password, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));

			if (ConfigManager.Config.Notifications.Types.PasswordGenerated)
			{
				notificationService.Raise($"The new password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
			}
			// Add the password to Git
			syncService?.AddPassword(passwordFilePath + passwordManager.EncryptedFileExtension);
		}


		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		public void DecryptPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			var selectedFile = RequestPasswordFile();
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

		public void DecryptMetadata(bool copyToClipboard, bool type)
		{
			var selectedFile = RequestPasswordFile();
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
				clipboard.Place(passFile.Metadata, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));
				if (ConfigManager.Config.Notifications.Types.PasswordCopied)
				{
					notificationService.Raise($"The key has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
				}
			}
			if (type)
			{
				KeyboardEmulator.EnterText(passFile.Metadata, ConfigManager.Config.Output.DeadKeys);
			}
		}

		public void GetKey(bool copyToClipboard, bool type, string key)
		{
			var selectedFile = RequestPasswordFile();

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

			if (string.IsNullOrWhiteSpace(key))
			{
				var keys = passFile.Keys.Select(k => k.Key).Distinct();
				var selection = ShowPasswordMenu(keys);
				if (selection == null) return;
				key = selection;
			}

			var values = passFile.Keys.Where(k => k.Key == key).ToList();

			if (values.Count == 0)
			{
				return;
			}

			string chosenValue;
			if (values.Count > 1)
			{
				chosenValue = ShowPasswordMenu(values.Select(v => v.Value));
				if (chosenValue == null)
				{
					return;
				}
			}
			else
			{
				chosenValue = values[0].Value;
			}


			if (copyToClipboard)
			{
				clipboard.Place(chosenValue, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));
				if (ConfigManager.Config.Notifications.Types.PasswordCopied)
				{
					notificationService.Raise($"The key has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
				}
			}
			if (type)
			{
				KeyboardEmulator.EnterText(chosenValue, ConfigManager.Config.Output.DeadKeys);
			}
		}
	}
}
