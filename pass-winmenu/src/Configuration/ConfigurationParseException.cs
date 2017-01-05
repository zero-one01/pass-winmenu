using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.Configuration
{
	internal class ConfigurationParseException : ConfigurationException
	{
		public ConfigurationParseException(string message, Exception innerException) : base(message, innerException) { }
		public ConfigurationParseException(string message) : base(message) { }
	}
}
