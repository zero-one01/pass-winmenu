using System;

namespace PassWinmenu.Configuration
{
	[Serializable]
	public class RuntimeConfigurationError : Exception
	{
		public RuntimeConfigurationError(string message) : base(message)
		{
		}
	}
}
