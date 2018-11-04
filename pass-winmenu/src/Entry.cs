using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace PassWinmenu
{
	internal static class Entry
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Program());
		}
	}
}
