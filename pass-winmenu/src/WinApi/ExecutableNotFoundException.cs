using System;

namespace PassWinmenu.WinApi
{
	public class ExecutableNotFoundException : Exception
	{
		public ExecutableNotFoundException() { }

		public ExecutableNotFoundException(string message) : base(message) { }

		public ExecutableNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
