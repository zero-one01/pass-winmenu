using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Utilities;

namespace PassWinmenu.PasswordManagement
{
	internal class PasswordManager : IPasswordManager
	{
		private readonly DirectoryInfoBase passwordStore;
		private readonly ICryptoService cryptoService;
		private readonly IRecipientFinder recipientFinder;

		private IFileSystem FileSystem => passwordStore.FileSystem;

		public PasswordManager(DirectoryInfoBase passwordStore, ICryptoService cryptoService, IRecipientFinder recipientFinder)
		{
			this.recipientFinder = recipientFinder;
			this.passwordStore = passwordStore;
			this.cryptoService = cryptoService;
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
			if (overwrite && file.FileInfo.Exists)
			{
				file.FileInfo.Delete();
			}
			cryptoService.Encrypt(file.Content, file.FullPath, recipientFinder.FindRecipients(file));
			return new PasswordFile(file);
		}

		public PasswordFile AddPassword(string path, string password, string metadata)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = PasswordFileFromPath(path);
			var parsed = new ParsedPasswordFile(file, password, metadata);
			return EncryptPassword(parsed, false);
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
			if (!file.FileInfo.Exists) throw new ArgumentException($"The password file \"{file.FullPath}\" does not exist.");

			var content = cryptoService.Decrypt(file.FullPath);
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

			var files = passwordStore.EnumerateFiles("*", SearchOption.AllDirectories);
			var matchingFiles = files.Where(f => patternRegex.IsMatch(f.Name));
			var passwordFiles = matchingFiles.Select(CreatePasswordFile);

			return passwordFiles;
		}

		private PasswordFile CreatePasswordFile(FileInfoBase file)
		{
			return new PasswordFile(file, passwordStore);
		}

		private PasswordFile PasswordFileFromPath(string path)
		{
			var relativePath = FileSystem.Path.IsPathRooted(path) 
				? Helpers.GetRelativePath(path, passwordStore.FullName)
				: path;

			var fullPath = FileSystem.Path.Combine(passwordStore.FullName, relativePath);
			if (FileSystem.Path.GetDirectoryName(fullPath) == null)
			{
				throw new ArgumentException("Invalid password store path.");
			}

			return new PasswordFile(FileSystem.FileInfo.FromFileName(fullPath), passwordStore);
		}
	}
}
