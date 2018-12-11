using System;

namespace PassWinmenu
{
	internal static class Entry
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Program().EnterMainLoop();
		}
	}
}
