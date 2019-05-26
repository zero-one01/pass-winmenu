using System;
using System.IO;
using System.Text;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenu.Utilities
{
	public class FakeProcessBuilder
	{
		private TimeSpan exitTime;
		private StreamReader standardError;

		public FakeProcess Build()
		{
			return new FakeProcess
			{
				ExitTime = exitTime,
				StandardError = standardError
			};
		}

		public FakeProcessBuilder WithStandardError(string standardError)
		{
			this.standardError = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(standardError)));
			return this;
		}

		public FakeProcessBuilder WithExitTime(TimeSpan exitTime)
		{
			this.exitTime = exitTime;
			return this;
		}
	}
}
