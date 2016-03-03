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
	/// Simple wrapper over GPG.
	/// </summary>
	internal class GPG
	{
		private readonly string executable;

		/// <summary>
		/// Initialises the wrapper.
		/// </summary>
		/// <param name="executable">The name of the GPG executable. Can be a full filename or the name of an executable contained in %PATH%.</param>
		public GPG(string executable)
		{
			this.executable = executable;
		}

		/// <summary>
		/// Runs GPG with the given arguments, and returns everything it prints to its standard output.
		/// </summary>
		/// <param name="arguments">The arguments to be passed to GPG.</param>
		/// <returns>A (UTF-8 decoded) string containing the text returned by GPG.</returns>
		/// <exception cref="GpgException">Thrown when GPG returns a non-zero exit code.</exception>
		private string RunGPG(string arguments)
		{
			var proc = Process.Start(new ProcessStartInfo
			{
				FileName = executable,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				StandardOutputEncoding = Encoding.UTF8
			});
			proc.WaitForExit();
			var result = proc.StandardOutput.ReadToEnd();
			if (proc.ExitCode != 0)
			{
				throw new GpgException(proc.ExitCode);
			}
			return result;
		}

		/// <summary>
		/// Decrypt a file with GPG.
		/// </summary>
		/// <param name="file">The path to the file to be decrypted.</param>
		/// <returns>The contents of the decrypted file.</returns>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public string Decrypt(string file)
		{
			return RunGPG($"--decrypt \"{file}\"");
		}
	}

	internal class GpgException : Exception
	{
		public int ExitCode { get; }

		public GpgException(int exitCode)
		{
			ExitCode = exitCode;
		}
	}
}
