using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.ExternalPrograms
{
	interface IProcessStarter
	{
		Process Start(ProcessStartInfo psi);
	}
}
