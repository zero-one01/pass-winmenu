using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.ExternalPrograms
{
	class ProcessStarter : IProcessStarter
	{
		public Process Start(ProcessStartInfo psi)
		{
			return Process.Start(psi);
		}
	}
}
