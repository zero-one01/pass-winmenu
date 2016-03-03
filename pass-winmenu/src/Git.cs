using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassWinmenu
{
	/// <summary>
	/// Simple wrapper over git.
	/// </summary>
	internal class Git
	{
		private readonly string executable;
		private readonly string repository;

		/// <summary>
		/// Initialises the wrapper.
		/// </summary>
		/// <param name="executable">The name of the git executable. Can be a full filename or the name of an executable contained in %PATH%.</param>
		public Git(string executable, string repository)
		{
			this.executable = executable;
			this.repository = repository;
		}

		/// <summary>
		/// Runs git with the given arguments, and returns everything it prints to its standard output.
		/// </summary>
		/// <param name="arguments">The arguments to be passed to git.</param>
		/// <returns>A (UTF-8 decoded) string containing the text returned by git.</returns>
		/// <exception cref="GpgException">Thrown when git returns a non-zero exit code.</exception>
		private string RunGit(string arguments)
		{
			var proc = Process.Start(new ProcessStartInfo
			{
				FileName = executable,
				Arguments = arguments,
				WorkingDirectory = repository,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				StandardOutputEncoding = Encoding.UTF8
			});
			proc.WaitForExit();
			var result = proc.StandardOutput.ReadToEnd();
			if (proc.ExitCode != 0)
			{
				throw new GitException(proc.ExitCode);
			}
			return result;
		}

		public bool Update()
		{
			var result = RunGit("pull");
			return true;
		}
	}

	internal class GitException : Exception
	{
		public int ExitCode { get; }

		public GitException(int exitCode)
		{
			ExitCode = exitCode;
		}
	}
}
