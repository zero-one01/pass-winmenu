using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.Configuration
{
	internal class ConfigurationException : Exception
	{
		public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
		public ConfigurationException(string message) : base(message) { }
	}
}
