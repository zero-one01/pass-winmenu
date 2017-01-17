using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;

namespace PassWinmenu
{
	internal struct PasswordFileContent
	{
		public string Password { get; }
		public string ExtraContent { get; }

		public PasswordFileContent(string password, string extraContent)
		{
			Password = password;
			ExtraContent = extraContent;
		}
	}
	internal class PasswordManager
	{
		internal const string GpgIdFileName = ".gpg-id";

		private readonly DirectoryInfo passwordStoreDirectory;
		private readonly GPG gpg;
		private readonly string encryptedFileExtension;

		public PasswordManager(string passwordStore, string encryptedFileExtension, GPG gpg)
		{
			var normalised = Helpers.NormaliseDirectory(passwordStore);
			passwordStoreDirectory = new DirectoryInfo(normalised);

			this.encryptedFileExtension = encryptedFileExtension;
			this.gpg = gpg;
		}

		/// <summary>
		/// Generates an encrypted password file at the specified path.
		/// If the path contains directories that do not exist, they will be created automatically.
		/// </summary>
		/// <param name="fileContent"></param>
		/// <param name="path"></param>
		public void EncryptPassword(PasswordFileContent fileContent, string path)
		{
			var fullPath = GetPasswordFilePath(path);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			gpg.Encrypt($"{fileContent.Password}\n{fileContent.ExtraContent}", fullPath + encryptedFileExtension, GetGpgIds(fullPath));
		}

		/// <summary>
		/// Get the content from an encrypted password file.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="passwordOnFirstLine">Should be true if the first line of the file contains the password.
		/// Any content in the remaining lines will be considered metadata.
		/// If set to false, the contents of the entire file are considered to be the password.</param>
		/// <returns></returns>
		public PasswordFileContent DecryptPassword(string path, bool passwordOnFirstLine)
		{
			var fullPath = GetPasswordFilePath(path);
			if (!File.Exists(fullPath)) throw new ArgumentException($"The password file \"{fullPath}\" does not exist.");
			var content = gpg.Decrypt(fullPath);

			if (passwordOnFirstLine)
			{
				// The first line contains the password, any other lines contain additional (contextual) content.
				var match = Regex.Match(content, @"(.*?)(?:\r\n|\n)(.*)", RegexOptions.Singleline);
				var password = match.Groups[1].Value;
				var extraContent = match.Groups[2].Value;

				return new PasswordFileContent(password, extraContent);
			}
			else
			{
				// Consider the contents of the entire file to be the password.
				return new PasswordFileContent(content, null);
			}

		}

		/// <summary>
		/// Encrypt a file. The path to the unencrypted file (plus a .gpg extension)
		/// is used to produce the path to the encrypted file.
		/// </summary>
		/// <param name="file">An absolute path pointing to the encrypted file.</param>
		public string EncryptFile(string file)
		{
			var fullFilePath = GetPasswordFilePath(file);
			if (!File.Exists(fullFilePath)) throw new ArgumentException($"The unencrypted file \"{fullFilePath}\" does not exist.");
			gpg.EncryptFile(fullFilePath, fullFilePath + encryptedFileExtension, GetGpgIds(file));

			return fullFilePath + encryptedFileExtension;
		}

		/// <summary>
		/// Decrypt a file. The encrypted file should have a .gpg extension, which
		/// is removed to produce the path to the decrypted file.
		/// </summary>
		/// <param name="path">The path, relative to the password store, to the encrypted file.</param>
		/// <returns>An absolute path pointing to the decrypted file.</returns>
		public string DecryptFile(string path)
		{
			if (!path.EndsWith(encryptedFileExtension))
			{
				throw new ArgumentException($"The encrypted file \"{path}\" should have a filename ending with {encryptedFileExtension}");
			}

			var encryptedFileName = GetPasswordFilePath(path);
			var decryptedFileName = encryptedFileName.Substring(0, encryptedFileName.Length - encryptedFileExtension.Length);
			
			if (!File.Exists(encryptedFileName)) throw new ArgumentException($"The encrypted file \"{encryptedFileName}\" does not exist.");
			gpg.DecryptToFile(encryptedFileName, decryptedFileName);
			return decryptedFileName;
		}
		
		/// <summary>
		/// Turns a relative (password store) path into an absolute path.
		/// Throws an exception if the password store path is not relative.
		/// </summary>
		/// <param name="relativePath">A relative path pointing to a file or directory in the password store.</param>
		/// <returns>The absolute path of that file or directory.</returns>
		private string GetPasswordFilePath(string relativePath)
		{
			// Ensure the directory separators are correct
			var normalised = Helpers.NormaliseDirectory(relativePath);
			// Only allow saving encrypted files to the password store
			if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Password file path must be relative to the password store directory.");
			var fullPath = Path.Combine(passwordStoreDirectory.FullName, normalised);
			return fullPath;
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
			if (!passwordStoreDirectory.IsParentOf(path))
			{
				throw new ArgumentException("The given directory is not a subdirectory of the password store.");
			}

			// Walk up from the innermost directory, and keep moving up until an existing directory 
			// containing a gpg-id file is found.
			var current = new DirectoryInfo(path);
			while (!current.Exists && !current.ContainsFile(GpgIdFileName))
			{
				if (current.Parent == null || current.PathEquals(passwordStoreDirectory))
				{
					return null;
				}
				current = current.Parent;
			}

			return File.ReadAllLines(Path.Combine(current.FullName, GpgIdFileName));
		}
	}
}
