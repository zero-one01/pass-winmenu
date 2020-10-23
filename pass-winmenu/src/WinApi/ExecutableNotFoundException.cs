using System;
using System.Collections.Generic;

namespace PassWinmenu.WinApi
{
	public class ExecutableNotFoundException : Exception
	{
		public ExecutableNotFoundException(string message) : base(message) { }
	}
}

