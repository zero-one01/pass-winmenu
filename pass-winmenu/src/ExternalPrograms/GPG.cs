using System;
using System.Diagnostics;
using System.Linq;
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
		internal string RunGPG(string arguments, string stdin=null)
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
		/// Decrypt a file to a plaintext file with GPG.
		/// </summary>
		/// <param name="encryptedFile">The path to the file to be decrypted.</param>
		/// <returns>The path to the generated plaintext file.</returns>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public string DecryptToFile(string encryptedFile)
		{
			// Ensure the plaintext file does not overwrite the input file
			string textFile;
			if (encryptedFile.EndsWith(".gpg"))
			{
				textFile = encryptedFile.Substring(0, encryptedFile.Length - 4) + ".plaintext.txt";
			}
			else
			{
				textFile = encryptedFile + ".plaintext.txt";
			}
			RunGPG($"--output {textFile} --decrypt \"{encryptedFile}\"");
			return textFile;
		}

		/// <summary>
		/// Encrypt a string with GPG.
		/// </summary>
		/// <param name="data">The text to be encrypted.</param>
		/// <param name="outputFile">The path to the output file.</param>
		/// <param name="recipient">The GPG ID of the recipient.</param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void Encrypt(string data, string outputFile, params string[] recipients)
		{
			var recipientsString = string.Join(" ", recipients.Select(r => $"--recipient \"{r}\""));
			RunGPG($"{recipientsString} --output \"{outputFile}\" --encrypt", data);
		}

		/// <summary>
		/// Encrypt a file with GPG.
		/// </summary>
		/// <param name="inputFile">The path to the file to be encrypted.</param>
		/// <param name="outputFile">The path to the output file.</param>
		/// <param name="recipient">The GPG ID of the recipient.</param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void EncryptFile(string inputFile, string outputFile, params string[] recipients)
		{
			var recipientsString = string.Join(" ", recipients.Select(r => $"--recipient \"{r}\""));
			RunGPG($"{recipientsString}  --output \"{outputFile}\" --encrypt {inputFile}");
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
