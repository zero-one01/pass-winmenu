using System;
using System.IO;

namespace PassWinmenu.Utilities
{
	internal static class Helpers
	{
		/// <summary>
		/// Normalises a directory, replacing all AltDirectorySeparatorChars with DirectorySeparatorChars
		/// and stripping any trailing directory separators.
		/// </summary>
		/// <param name="directory">The directory to be normalised.</param>
		/// <returns>The normalised directory.</returns>
		internal static string NormaliseDirectory(string directory)
		{
			var normalised = directory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			var stripped = normalised.TrimEnd(Path.DirectorySeparatorChar);
			return stripped;
		}
		
		/// <summary>
		/// Returns the path of a file relative to a specified root directory.
		/// </summary>
		/// <param name="filespec">The path to the file for which the relative path should be calculated.</param>
		/// <param name="root">The root directory relative to which the relative path should be calculated.</param>
		/// <returns></returns>
		internal static string GetRelativePath(string filespec, string root)
		{
			var pathUri = new Uri(filespec);

			// The directory URI must end with a directory separator char.
			if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				root += Path.DirectorySeparatorChar;
			}
			var directoryUri = new Uri(root);
			return Uri.UnescapeDataString(directoryUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
