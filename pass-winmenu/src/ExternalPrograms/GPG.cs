using System;
using System.Diagnostics;
using System.Text;

namespace PassWinmenu.ExternalPrograms
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
		/// <param name="stdin">A text string to be sent to GPG's standard input.</param>
		/// <returns>A (UTF-8 decoded) string containing the text returned by GPG.</returns>
		/// <exception cref="GpgException">Thrown when GPG returns a non-zero exit code.</exception>
		private string RunGPG(string arguments, string stdin=null)
		{
			var info = new ProcessStartInfo
			{
				FileName = executable,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8
			};
			if (stdin != null)
			{
				info.RedirectStandardInput = true;
			}
			var proc = Process.Start(info);
			if (stdin != null)
			{
				proc.StandardInput.Write(stdin);
				proc.StandardInput.Close();
			}
			proc.WaitForExit();

			var result = proc.StandardOutput.ReadToEnd();
			var error = proc.StandardError.ReadToEnd();
			if (proc.ExitCode != 0)
			{
				throw new GpgException(proc.ExitCode, result, error);
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

		/// <summary>
		/// Encrypt a file with GPG.
		/// </summary>
		/// <param name="data">The text to be encrypted</param>
		/// <param name="recipient"></param>
		/// <param name="outputFile"></param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void Encrypt(string data, string recipient, string outputFile)
		{
			RunGPG($"--recipient \"{recipient}\"  --output \"{outputFile}.gpg\" --encrypt", data);
		}
	}
	
	internal class GpgException : Exception
	{
		public int ExitCode { get; }
		public string GpgOutput { get; }
		public string GpgError { get; }
		public override string Message { get; }

		public GpgException(int exitCode, string output, string error)
		{
			ExitCode = exitCode;
			GpgOutput = output;
			GpgError = error;
			Message = "GPG exited with code " + exitCode;
		}
	}
}
