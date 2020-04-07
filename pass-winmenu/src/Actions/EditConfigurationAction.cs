using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;

namespace PassWinmenu.Actions
{
	class EditConfigurationAction : IAction
	{
		public void Execute()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "explorer", 
				Arguments = Program.ConfigFileName,
			};
			Process.Start(startInfo);
		}

		public HotkeyAction ActionType => HotkeyAction.EditConfiguration;
	}
}
