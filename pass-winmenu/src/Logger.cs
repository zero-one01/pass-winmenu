using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
				writer = new StreamWriter(File.Open("pass-winmenu.log", FileMode.Append, FileAccess.Write, FileShare.Read));
				writer.AutoFlush = true;
			}
			catch (Exception e)
			{
				MessageBox.Show($"The log file could not be created: an error occurred ({e.GetType().Name}: {e.Message})", "Failed to create log file");
			}
			AppDomain.CurrentDomain.UnhandledException += ReportException;
		}

		private static void ReportException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
		{
			SendRaw("An unhandled exception occurred. Stack trace:");
			LogExceptionAsText(unhandledExceptionEventArgs.ExceptionObject as Exception, 0);
		}

		private static void LogExceptionAsText(Exception e, int level)
		{
			var trace = new StackTrace(e, true);
			var bottomFrame = trace.GetFrame(0);
			var indents = string.Concat(Enumerable.Repeat("  ", level));

			var aggr = e as AggregateException;
			if (aggr != null)
			{
				// Don't log the stack trace, instead log all inner exceptions and log their stacktraces instead.
				SendRaw($"{indents}AggregateException in {bottomFrame?.GetFileName()}:{bottomFrame?.GetFileLineNumber()}:{bottomFrame?.GetFileColumnNumber()} - Sub-exceptions:");
				foreach (var inner in aggr.InnerExceptions)
				{
					LogExceptionAsText(inner, ++level);
				}
			}
			else
			{
				SendRaw($"{indents}{e.GetType().Name} ({e.Message}) in {bottomFrame?.GetFileName()}:{bottomFrame?.GetFileLineNumber()}:{bottomFrame?.GetFileColumnNumber()}");

				if (e.InnerException != null)
				{
					LogExceptionAsText(e.InnerException, ++level);
				}
				else
				{
					// Only log the stacktrace if there is no inner exception,
					// otherwise the inner exception can print it instead.
					var stackFrames = trace.GetFrames();
					if (stackFrames == null) return;

					foreach (var frame in stackFrames)
					{
						SendRaw($"{indents}  -> in {frame.GetMethod().ReflectedType?.FullName ?? "unknown"}.{frame.GetMethod().Name} -- {frame.GetFileName()}:{frame.GetFileLineNumber()}:{frame.GetFileColumnNumber()}");
					}
				}
			}
		}

		private static void SendRaw(string message)
		{
			var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
			Console.WriteLine(line);
			writer?.WriteLine(line);
		}

		public static void Send(string message, LogLevel level = LogLevel.Debug)
		{
			SendRaw($"[{GetLevelString(level)}] {message}");
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
