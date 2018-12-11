using System.IO;

namespace PassWinmenu.PasswordManagement
{
	internal class PasswordFile
	{
		public string FullPath => Path.GetFullPath(RelativePath);
		public string RelativePath { get; }
		public string Name => Path.GetFileName(RelativePath);

		public PasswordFile(string relativePath)
		{
			RelativePath = relativePath;
		}
	}
}
