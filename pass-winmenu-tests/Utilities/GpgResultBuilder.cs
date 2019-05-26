using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.Utilities
{
	class GpgResultBuilder
	{
		private List<string> stderrMessages = new List<string>();
		private List<StatusMessage> statusMessages = new List<StatusMessage>();
		private string stdout;
		private int exitCode;

		public GpgResultBuilder WithExitCode(int exitCode)
		{
			this.exitCode = exitCode;
			return this;
		}

		public GpgResultBuilder WithStdout(string stdout)
		{
			this.stdout = stdout;
			return this;
		}

		public GpgResultBuilder WithStdErrMessage(string message)
		{
			stderrMessages.Add(message);
			return this;
		}

		public GpgResultBuilder WithStatusMessage(GpgStatusCode code, string message)
		{
			statusMessages.Add(new StatusMessage(code, message));
			return this;
		}

		public GpgResult Build()
		{
			return new GpgResult(exitCode, stdout, statusMessages, stderrMessages);
		}
	}
}
