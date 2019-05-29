using System;
using System.IO;
using System.IO.Abstractions;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;

namespace PassWinmenu.PasswordManagement
{
	internal class PasswordFile
	{
		/// <summary>
		/// Represents the root password store directory this file lives in.
		/// </summary>
		public DirectoryInfoBase PasswordStore { get; }
		/// <summary>
		/// A <see cref="FileInfo"/> instance representing the password file.
		/// </summary>
		public FileInfoBase FileInfo { get; }

		/// <summary>
		/// Represents the directory containing the password file.
		/// </summary>
		public DirectoryInfoBase Directory => FileInfo.Directory;
		/// <summary>
		/// The full path to this file.
		/// </summary>
		public string FullPath => FileInfo.FullName;
		/// <summary>
		/// The base name of the password file, without its extension.
		/// </summary>
		public string FileNameWithoutExtension => FileInfo.Name.RemoveEnd(FileInfo.Extension);
		
		public PasswordFile(FileInfoBase file, DirectoryInfoBase directory)
		{
			FileInfo = file;
			PasswordStore = directory;
		}

		public PasswordFile(PasswordFile original)
		{
			FileInfo = original.FileInfo;
			PasswordStore = original.PasswordStore;
		}
	}
}
