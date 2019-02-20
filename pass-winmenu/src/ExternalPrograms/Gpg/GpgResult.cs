using System.Collections.Generic;
using System.Linq;

namespace PassWinmenu.ExternalPrograms
{
	internal class GpgResult
	{
		public StatusMessage[] StatusMessages { get; }
		public string[] StderrMessages { get; }
		public string Stdout { get; }
		public int ExitCode { get; }

		private IEnumerable<GpgStatusCode> StatusCodes => StatusMessages.Select(m => m.StatusCode);

		public GpgResult(int exitCode, string stdout, IEnumerable<StatusMessage> statusMessages, IEnumerable<string> stderrMessages)
		{
			ExitCode = exitCode;
			Stdout = stdout;
			StatusMessages = statusMessages.ToArray();
			StderrMessages = stderrMessages.ToArray();
		}

		public void GenerateError()
		{
			throw new GpgException($"GPG returned the following errors: \n{string.Join("\n", StderrMessages.Select(m => "    " + m))}");
		}

		public void EnsureNonZeroExitCode()
		{
			if (ExitCode != 0)
			{
				throw new GpgException($"GPG exited with status {ExitCode}\n\nOutput:\n{string.Join("\n", StderrMessages)}");
			}
		}

		public bool HasStatusCodes(params GpgStatusCode[] required)
		{
			return required.All(StatusCodes.Contains);
		}

		public void EnsureSuccess(GpgStatusCode[] requiredCodes, GpgStatusCode[] disallowedCodes)
		{
			var missing = requiredCodes.Where(c => !StatusCodes.Contains(c)).ToList();
			if (missing.Count > 0)
			{
				throw new GpgException($"Expected status(es) \"{string.Join(", ", missing)}\" not returned by GPG");
			}
			var present = disallowedCodes.Where(c => StatusCodes.Contains(c)).ToList();
			if (present.Count > 0)
			{
				throw new GpgException($"GPG returned disallowes status(es) \"{string.Join(", ", present)}\"");
			}
		}
	}
}
