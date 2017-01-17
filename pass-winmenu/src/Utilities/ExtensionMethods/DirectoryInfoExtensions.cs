using System.IO;
using System.Linq;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class DirectoryInfoExtensions
	{

		/// <summary>
		/// Checks whether the current directory is a child of the given directory.
		/// </summary>
		/// <returns>True if the current directory is a parent of the given directory,
		/// or if they're the same directory. False otherwise.</returns>
		internal static bool IsChildOf(this DirectoryInfo child, DirectoryInfo parent)
		{
			for (var current = child; !current.PathEquals(parent); current = current.Parent)
			{
				if(current.Parent == null)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Checks whether the current directory is a child of the given directory.
		/// </summary>
		/// <returns>True if the current directory is a parent of the given directory,
		/// or if they're the same directory. False otherwise.</returns>
		internal static bool IsChildOf(this DirectoryInfo child, string parent)
		{
			return IsChildOf(child, new DirectoryInfo(parent));
		}

		/// <summary>
		/// Checks whether the current directory is a parent of the given directory.
		/// </summary>
		/// <returns>True if the current directory is a parent of the given directory, 
		/// or if they're the same directory. False otherwise.</returns>
		internal static bool IsParentOf(this DirectoryInfo parent, DirectoryInfo child)
		{
			return IsChildOf(child, parent);
		}

		/// <summary>
		/// Checks whether the current directory is a parent of the given directory.
		/// </summary>
		/// <returns>True if the current directory is a parent of the given directory, 
		/// or if they're the same directory. False otherwise.</returns>
		internal static bool IsParentOf(this DirectoryInfo parent, string child)
		{
			return IsChildOf(new DirectoryInfo(child), parent);
		}

		/// <summary>
		/// Checks whether the directory contains the given file.
		/// </summary>
		/// <returns>True if the directory contains the file, false otherwise.</returns>
		internal static bool ContainsFile(this DirectoryInfo directory, string name)
		{
			return directory.EnumerateFiles(name).Any();
		}

		/// <summary>
		/// Checks for path equality between two DirectoryInfo objects.
		/// Unlike a direct comparison of their FullName properties,
		/// this method ignores trailing slashes.
		/// </summary>
		/// <returns>True if both DirectoryInfo objects reference the same directory, false otherwise.</returns>
		internal static bool PathEquals(this DirectoryInfo a, DirectoryInfo b)
		{
			var pathA = Helpers.NormaliseDirectory(a.FullName);
			var pathB = Helpers.NormaliseDirectory(b.FullName);
			return pathA == pathB;
		}
	}
}
