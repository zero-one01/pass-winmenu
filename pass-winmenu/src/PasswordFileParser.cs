using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordManagement;

namespace PassWinmenu
{
	internal class PasswordFileParser
	{
		/// <summary>
		/// Extracts the username and any possible metadata from a password file
		/// by auto-detecting the correct line-endings.
		/// </summary>
		/// <param name="file">A <see cref="PasswordFile"/> specifying the location of the file to be decrypted.</param>
		/// <param name="content">Content of the password file</param>
		/// <param name="entireFile">If set to true, any line endings are considered to be part of the password.</param>
		/// <returns>A <see cref="DecryptedPasswordFile"/> structure containing the password and metadata</returns>
		public DecryptedPasswordFile Parse(PasswordFile file, string content, bool entireFile)
		{
			if (entireFile)
			{
				// If the password contains any line endings, there is no additional metadata available.
				return new DecryptedPasswordFile(file.RelativePath, content, null);
			}
			else
			{
				// The first line contains the password, any other lines contain additional (contextual) content.
				var match = Regex.Match(content, @"([^\n\r]*)(?:(?:\r\n|\n|\r)(.*))?", RegexOptions.Singleline);
				var password = match.Groups[1].Value;
				var extraContent = match.Groups[2].Value;

				var keys = ExtractKeys(extraContent);

				return new DecryptedPasswordFile(file.RelativePath, password, extraContent, keys.ToList());
			}
		}

		private IEnumerable<KeyValuePair<string, string>> ExtractKeys(string metadata)
		{
			var matches = Regex.Matches(metadata, @"([A-z0-9-_]+): (.*?)([\r\n]+|$)", RegexOptions.Singleline);
			foreach (Match match in matches)
			{
				var key = match.Groups[1].Value;
				var value = match.Groups[2].Value;
				yield return new KeyValuePair<string, string>(key, value);
			}
		}

		/// <summary>
		/// Attempts to retrieve the username from a password file.
		/// </summary>
		/// <param name="passwordFile">The name of the password file.</param>
		/// <param name="extraContent">The extra content of the password file.</param>
		/// <returns>A string containing the username if the password file contains one, null if no username was found.</returns>
		public string GetUsername(string passwordFile, string extraContent)
		{
			var options = ConfigManager.Config.PasswordStore.UsernameDetection.Options;
			switch (ConfigManager.Config.PasswordStore.UsernameDetection.Method)
			{
				case UsernameDetectionMethod.FileName:
					return Path.GetFileName(passwordFile)?.Replace(Program.EncryptedFileExtension, "");
				case UsernameDetectionMethod.LineNumber:
					var extraLines = extraContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
					var lineNumber = options.LineNumber - 2;
					if (lineNumber <= 1) throw new PasswordParseException("Invalid line number for username detection.");
					return lineNumber < extraLines.Length ? extraLines[lineNumber] : null;
				case UsernameDetectionMethod.Regex:
					var rgxOptions = options.RegexOptions.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
					rgxOptions = rgxOptions | (options.RegexOptions.Multiline ? RegexOptions.Multiline : RegexOptions.None);
					rgxOptions = rgxOptions | (options.RegexOptions.Singleline ? RegexOptions.Singleline : RegexOptions.None);
					var match = Regex.Match(extraContent, options.Regex, rgxOptions);
					return match.Groups["username"].Success ? match.Groups["username"].Value : null;
				default:
					throw new ArgumentOutOfRangeException("username-detection.method", "Invalid username detection method.");
			}
		}
	}

	internal class PasswordParseException : Exception
	{
		public PasswordParseException(string message) : base(message)
		{
			
		}
	}
}
