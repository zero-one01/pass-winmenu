using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenu.Actions
{
	class OpenExplorerAction : IAction
	{
		private readonly IProcesses processes;
		private readonly PasswordStoreConfig passwordStore;

		public OpenExplorerAction(IProcesses processes, PasswordStoreConfig passwordStore)
		{
			this.processes = processes;
			this.passwordStore = passwordStore;
		}

		public void Execute()
		{
			processes.Start(new ProcessStartInfo(passwordStore.Location));
		}
	}
}
