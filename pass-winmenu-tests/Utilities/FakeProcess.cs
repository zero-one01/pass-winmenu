using System;
using System.IO;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenu.Utilities
{
	public class FakeProcess : IProcess
	{

		public void Kill()
		{
		}

		public bool WaitForExit(TimeSpan timeout)
		{
			return ExitTime < timeout;
		}

		public StreamWriter StandardInput { get; } = new StreamWriter(Stream.Null);
		public StreamReader StandardOutput { get; } = new StreamReader(Stream.Null);
		public StreamReader StandardError { get; set; } = new StreamReader(Stream.Null);
		public string MainModuleName { get; } = string.Empty;
		public int Id { get; } = 1;
		public DateTime StartTime { get; } = DateTime.Now;
		public int ExitCode { get; } = 0;

		public TimeSpan ExitTime { get; set; }
	}
}
