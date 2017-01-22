using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gpg.NET;
using PassWinmenu.Configuration;
using PassWinmenu.Windows;

namespace PassWinmenu.ExternalPrograms
{
	/// <summary>
	/// Simple wrapper over GPG.
	/// </summary>
	internal class GPG
	{
		private readonly GpgContext context;

		/// <summary>
		/// Initialises the wrapper.
		/// </summary>
		/// <param name="installDir">The GPG installation directory. When set to null, Gpg.NET will attempt to use the default installation directory.</param>
		public GPG(string installDir = null)
		{
			GpgNet.Initialise(installDir, minGpgVersion: "2.1.0");
			GpgNet.EnsureProtocol(GpgMeProtocol.OpenPgp);
			context = GpgContext.CreateContext();
		}

		/// <summary>
		/// Decrypt a file with GPG.
		/// </summary>
		/// <param name="file">The path to the file to be decrypted.</param>
		/// <returns>The contents of the decrypted file.</returns>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public string Decrypt(string file)
		{
			return context.DecryptFile(file);
		}

		/// <summary>
		/// Decrypt a file to a plaintext file with GPG.
		/// </summary>
		/// <param name="encryptedFile">The path to the file to be decrypted.</param>
		/// <param name="outputFile">The path where the decrypted file should be placed.</param>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public void DecryptToFile(string encryptedFile, string outputFile)
		{
			context.DecryptFile(encryptedFile, outputFile);
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
			var rcpKeys = context.GetKeys(recipients);
			context.EncryptString(data, outputFile, rcpKeys);
		}

		/// <summary>
		/// Encrypt a file with GPG.
		/// </summary>
		/// <param name="inputFile">The path to the file to be encrypted.</param>
		/// <param name="outputFile">The path to the output file.</param>
		/// <param name="recipients">An array of GPG ids for which the file should be encrypted.</param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void EncryptFile(string inputFile, string outputFile, params string[] recipients)
		{
			var rcpKeys = context.GetKeys(recipients);
			context.EncryptFile(inputFile, outputFile, rcpKeys);
		}

		public void StartAgent()
		{
			// Looking up a private key will start the GPG agent.
			context.FindKey(null, true);
		}
	}
}
