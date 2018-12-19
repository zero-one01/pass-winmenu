using System;
using System.IO;
using PassWinmenu.Utilities;

namespace PassWinmenu.PasswordManagement
{
	internal class PasswordFile
	{
		/// <summary>
		/// Represents the root password store directory this file lives in.
		/// </summary>
		public DirectoryInfo PasswordStore { get; }
		/// <summary>
		/// Represents the directory containing the password file.
		/// </summary>
		public DirectoryInfo Directory { get; }
		/// <summary>
		/// Represents the path to the file, relative to the root of the password store.
		/// </summary>
		public string RelativePath { get; }
		/// <summary>
		/// The full path to this file.
		/// </summary>
		public string FullPath { get; }

		/// <summary>
		/// The name of the password file, including its extension.
		/// </summary>
		public string FileName => Path.GetFileName(FullPath);

		/// <summary>
		/// The base name of the password file, without its extension.
		/// </summary>
		public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FullPath);

		/// <summary>
		/// Creates a new <see cref="PasswordFile"/> representing a password file at the given location.
		/// </summary>
		/// <param name="passwordStore">A <see cref="DirectoryInfo"/> object representing the location of the password store.</param>
		/// <param name="path">A path pointing to the password file. If the path is relative, it is considered to be relative to the password store.</param>
		public PasswordFile(DirectoryInfo passwordStore, string path)
		{
			PasswordStore = passwordStore ?? throw new ArgumentNullException(nameof(passwordStore));
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (Path.IsPathRooted(path))
			{
				RelativePath = Helpers.GetRelativePath(path, passwordStore.FullName);
			}
			else
			{
				RelativePath = path;
			}
			FullPath = Path.Combine(PasswordStore.FullName, RelativePath);
			var directory = Path.GetDirectoryName(FullPath);
			if (directory == null)
			{
				throw new ArgumentException("Invalid password store path.");
			}
			Directory = new DirectoryInfo(directory);
		}
	}
}
