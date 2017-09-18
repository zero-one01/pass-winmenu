using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu
{
	enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}

	class Log
	{
		public static void Send(string message, LogLevel level = LogLevel.Debug)
		{
			Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: [{GetLevelString(level)}] {message}");
		}

		private static string GetLevelString(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Debug:
					return "DBG";
				case LogLevel.Info:
					return "INF";
				case LogLevel.Warning:
					return "WRN";
				case LogLevel.Error:
					return "ERR";
				default:
					return "UNKNOWN";
			}
		}
	}
}
