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

		public void EncryptPassword(PasswordFileContent fileContent, string path)
		{
			var fullPath = GetPasswordFilePath(path);
			gpg.Encrypt($"{fileContent.Password}\n{fileContent.ExtraContent}", fullPath + encryptedFileExtension, GetGpgIds(fullPath));
		}
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
		/// Encrypt a file. The path to the encrypted file (plus a .gpg extension)
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
		/// Searches the given directory tree for a gpg-id file.
		/// </summary>
		/// <param name="path">The top of the directory tree that should be searched.
		/// May be a filename or a directory name.</param>
		/// <returns>An array of GPG ids taken from the first .gpg-id file that is encountered.</returns>
		private string[] GetGpgIds(string path)
		{
			// Ensure the path does not contain any trailing slashes
			path = Helpers.NormaliseDirectory(path);

			// Find the directory closest to the given path
			var startDir = Directory.Exists(path) 
				? new DirectoryInfo(path) 
				: new DirectoryInfo(Path.GetDirectoryName(path));

			// Ensure the password file directory is actually located in the password store.
			if (!passwordStoreDirectory.IsParentOf(startDir))
			{
				throw new ArgumentException("The given directory is not a subdirectory of the password store.");
			}
			// Walk down from the topmost directory, 
			// stopping as soon as we encounter a .gpg-id file.
			var current = startDir;
			while (!current.ContainsFile(".gpg-id"))
			{
				if (current.Parent == null || current.PathEquals(passwordStoreDirectory))
				{
					return null;
				}
				current = current.Parent;
			}

			return File.ReadAllLines(Path.Combine(current.FullName, ".gpg-id"));
		}
	}
}
