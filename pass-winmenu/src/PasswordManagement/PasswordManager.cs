using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;

namespace PassWinmenu.PasswordManagement
{
	internal class PasswordManager : IPasswordManager
	{
		internal const string GpgIdFileName = ".gpg-id";

		private readonly PinentryWatcher pinentryWatcher = new PinentryWatcher();

		public DirectoryInfo PasswordStore { get; }
		public ICryptoService Crypto { get; }
		public bool PinentryFixEnabled { get; set; }
		public readonly string EncryptedFileExtension;

		public PasswordManager(string passwordStore, string encryptedFileExtension, GPG crypto)
		{
			var normalised = Helpers.NormaliseDirectory(passwordStore);
			PasswordStore = new DirectoryInfo(normalised);

			EncryptedFileExtension = encryptedFileExtension;
			Crypto = crypto;
		}

		/// <summary>
		/// Encrypts a password file at the specified path.
		/// If the path contains directories that do not exist, they will be created automatically.
		/// </summary>
		/// <param name="file">
		/// A <see cref="KeyedPasswordFile"/> instance specifying the contents
		/// of the password file to be encrypted.
		/// </param>
		/// <param name="overwrite">
		/// If this file already exists, should it be overwritten?
		/// </param>
		public PasswordFile EncryptPassword(DecryptedPasswordFile file, bool overwrite)
		{
			file.Directory.Create();
			Crypto.Encrypt(file.Content, file.FullPath, GetGpgIds(file.FullPath));
			return new PasswordFile(file.PasswordStore, file.RelativePath);
		}

		public string DecryptText(PasswordFile file)
		{
			if (!File.Exists(file.FullPath)) throw new ArgumentException($"The password file \"{file.FullPath}\" does not exist.");

			if(PinentryFixEnabled) pinentryWatcher.BumpPinentryWindow();
			return Crypto.Decrypt(file.FullPath);
		}

		/// <summary>
		/// Get the content from an encrypted password file.
		/// </summary>
		/// <param name="file">A <see cref="PasswordFile"/> specifying the password file to be decrypted.</param>
		/// <param name="passwordOnFirstLine">Should be true if the first line of the file contains the password.
		/// Any content in the remaining lines will be considered metadata.
		/// If set to false, the contents of the entire file are considered to be the password.</param>
		/// <returns></returns>
		public KeyedPasswordFile DecryptPassword(PasswordFile file, bool passwordOnFirstLine)
		{
			var content = DecryptText(file);
			return new PasswordFileParser().Parse(file, content, !passwordOnFirstLine);
		}

		/// <summary>
		/// Returns all password files that match a search pattern.
		/// </summary>
		/// <param name="pattern">The pattern against which the files should be matched.</param>
		/// <returns></returns>
		public IEnumerable<PasswordFile> GetPasswordFiles(string pattern)
		{
			var patternRegex = new Regex(pattern);

			var files = Directory.EnumerateFiles(PasswordStore.FullName, "*", SearchOption.AllDirectories);
			var matchingFiles = files.Where(f => patternRegex.IsMatch(Path.GetFileName(f)));
			var passwordFiles = matchingFiles.Select(n => new PasswordFile(PasswordStore, n));

			return passwordFiles;
		}

		/// <summary>
		/// Searches the given path for a gpg-id file.
		/// </summary>
		/// <param name="path">The path that should be searched. This path does not have to point to an
		/// existing file or directory, but it must be located within the password store.</param>
		/// <returns>An array of GPG ids taken from the first gpg-id file that is encountered,
		/// or null if no gpg-id file was found.</returns>
		private string[] GetGpgIds(string path)
		{
			// Ensure the path does not contain any trailing slashes or AltDirectorySeparatorChars
			path = Helpers.NormaliseDirectory(path);

			// Ensure the password file directory is actually located within the password store.
			if (!PasswordStore.IsParentOf(path))
			{
				throw new ArgumentException("The given directory is not a subdirectory of the password store.");
			}

			// Walk up from the innermost directory, and keep moving up until an existing directory 
			// containing a gpg-id file is found.
			var current = new DirectoryInfo(path);
			while (!current.Exists || !current.ContainsFile(GpgIdFileName))
			{
				if (current.Parent == null || current.PathEquals(PasswordStore))
				{
					return null;
				}
				current = current.Parent;
			}

			return File.ReadAllLines(Path.Combine(current.FullName, GpgIdFileName));
		}
	}
}
