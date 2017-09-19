using System;
using System.IO;
using System.Windows;

namespace PassWinmenu
{
	enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}

	static class Log
	{
		private static StreamWriter writer;

		public static void Initialise()
		{
			try
			{
				writer = new StreamWriter(File.Open("pass-winmenu.log", FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
				writer.AutoFlush = true;
			}
			catch (Exception e)
			{
				MessageBox.Show($"The log file could not be created: an error occurred ({e.GetType().Name}: {e.Message})", "Failed to create log file");
			}
		}

		public static void Send(string message, LogLevel level = LogLevel.Debug)
		{
			var line = $"{DateTime.Now:HH:mm:ss.fff}: [{GetLevelString(level)}] {message}";
			Console.WriteLine(line);
			writer?.WriteLine(line);
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
