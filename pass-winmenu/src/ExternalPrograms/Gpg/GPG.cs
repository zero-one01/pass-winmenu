using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PassWinmenu.WinApi;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Simple wrapper over GPG.
	/// </summary>
	internal class GPG : ICryptoService
	{
		private readonly IExecutablePathResolver executablePathResolver;
		private readonly GpgTransport gpgTransport;
		private readonly GpgHomedirResolver homedirResolver;
		private readonly GpgAgent gpgAgent;

		public GPG(IExecutablePathResolver executablePathResolver, GpgTransport gpgTransport, GpgHomedirResolver homedirResolver, GpgAgent gpgAgent)
		{
			this.executablePathResolver = executablePathResolver;
			this.gpgTransport = gpgTransport;
			this.homedirResolver = homedirResolver;
			this.gpgAgent = gpgAgent;
		}

		/// <summary>
		/// Decrypt a file with GPG.
		/// </summary>
		/// <param name="file">The path to the file to be decrypted.</param>
		/// <returns>The contents of the decrypted file.</returns>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public string Decrypt(string file)
		{
			gpgAgent?.EnsureAgentResponsive();
			var result = gpgTransport.CallGpg($"--decrypt \"{file}\"");
			VerifyDecryption(result);
			return result.Stdout;
		}

		private void VerifyDecryption(GpgResult result)
		{
			if (result.HasStatusCodes(GpgStatusCode.FAILURE, GpgStatusCode.NODATA))
			{
				throw new GpgError("The file to be decrypted does not look like a valid GPG file.");
			}
			if (result.HasStatusCodes(GpgStatusCode.DECRYPTION_FAILED, GpgStatusCode.NO_SECKEY))
			{
				var keyIds = result.StatusMessages.Where(m => m.StatusCode == GpgStatusCode.NO_SECKEY);

				throw new GpgError("None of your private keys appear to be able to decrypt this file.\n" +
				                   $"The file was encrypted for the following (sub)key(s): {string.Join(", ", keyIds.Select(m => m.Message))}");
			}
			if (result.HasStatusCodes(GpgStatusCode.DECRYPTION_FAILED) && result.StderrMessages.Any(m => m.Contains("Operation cancelled")))
			{
				throw new GpgError("Operation cancelled.");
			}

			if (result.HasStatusCodes(GpgStatusCode.DECRYPTION_FAILED))
			{
				throw new GpgError("GPG Couldn't decrypt this file. The following information may contain more details about the error that occurred:\n\n" +
				                   $"{string.Join("\n", result.StderrMessages)}");
			}
			if (result.HasStatusCodes(GpgStatusCode.FAILURE))
			{
				result.GenerateError();
			}

			result.EnsureNonZeroExitCode();
		}

		private void VerifyEncryption(GpgResult result)
		{
			if (result.HasStatusCodes(GpgStatusCode.FAILURE, GpgStatusCode.INV_RECP, GpgStatusCode.KEYEXPIRED))
			{
				var failedrcps = result.StatusMessages.Where(m => m.StatusCode == GpgStatusCode.INV_RECP).Select(m => m.Message.Substring(m.Message.IndexOf(" ", StringComparison.Ordinal)));
				throw new GpgError($"Invalid/unknown recipient(s): {string.Join(", ", failedrcps)}\n" +
				                   "The key(s) belonging to this recipient may have expired.");
			}

			if (result.HasStatusCodes(GpgStatusCode.FAILURE, GpgStatusCode.INV_RECP))
			{
				var failedrcps = result.StatusMessages.Where(m => m.StatusCode == GpgStatusCode.INV_RECP).Select(m => m.Message.Substring(m.Message.IndexOf(" ", StringComparison.Ordinal)));
				throw new GpgError($"Invalid/unknown recipient(s): {string.Join(", ", failedrcps)}\n" +
				                   "Make sure that you have imported and trusted the keys belonging to those recipients.");
			}
			result.EnsureNonZeroExitCode();
		}

		/// <summary>
		/// Encrypt a string with GPG.
		/// </summary>
		/// <param name="data">The text to be encrypted.</param>
		/// <param name="outputFile">The path to the output file.</param>
		/// <param name="recipients">An array of GPG ids for which the file should be encrypted.</param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void Encrypt(string data, string outputFile, params string[] recipients)
		{
			if (recipients == null) recipients = new string[0];
			var recipientList = string.Join(" ", recipients.Select(r => $"--recipient \"{r}\""));

			var result = gpgTransport.CallGpg($"--output \"{outputFile}\" {recipientList} --encrypt", data);
			VerifyEncryption(result);
		}

		private void ListSecretKeys()
		{
			gpgAgent?.EnsureAgentResponsive();
			var result = gpgTransport.CallGpg("--list-secret-keys");
			if (result.Stdout.Length == 0)
			{
				throw new GpgError("No private keys found. Pass-winmenu will not be able to decrypt your passwords.");
			}
			// At some point in the future we might have a use for this data,
			// But for now, all we really use this method for is to ensure the GPG agent is started.
			//Log.Send("Secret key IDs: ");
			//Log.Send(result.Stdout);
		}

		public void StartAgent()
		{
			// Looking up a private key will start the GPG agent.
			ListSecretKeys();
		}

		public string GetVersion()
		{
			var output = gpgTransport.CallGpg("--version");
			return output.Stdout.Split(new []{"\r\n"}, StringSplitOptions.RemoveEmptyEntries).First();
		}

		public void UpdateAgentConfig(Dictionary<string, string> configKeys)
		{
			gpgAgent?.UpdateAgentConfig(configKeys, homedirResolver.GetHomeDir());
		}
	}
}
