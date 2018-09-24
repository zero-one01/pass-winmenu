using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PassWinmenu.Configuration;

namespace PassWinmenu.Actions
{
	internal class DecryptAction : IAction
	{
		public void Apply(List<Selection> selections, List<ItemOutput> outputs)
		{
			foreach (var output in outputs)
			{
				// Feed selections to the output
			}
		}
	}

	internal abstract class ItemOutput
	{
		public abstract void Process(List<Selection> selections);
	}

	/// <summary>
	/// Sends decrypted content to the clipboard
	/// </summary>
	internal class ClipboardOutput : ItemOutput
	{
		private TimeSpan timeout;

		public ClipboardOutput(TimeSpan timeout)
		{
			this.timeout = timeout;
		}

		public override void Process(List<Selection> selections)
		{
			// If we have only one selected item, place that on the clipboard
			if (selections.Count == 1)
			{
				new ClipboardHelper().Place(selections.First().Content, timeout);
				return;
			}
			// If we have multiple items, clip the password
			var password = selections.FirstOrDefault(s => s is PasswordSelection);
			if (password != null)
			{
				new ClipboardHelper().Place(password.Content, timeout);
			}

		}
	}

	/// <summary>
	/// Types decrypted content into a target window
	/// </summary>
	internal class TypeOutput : ItemOutput
	{
		public override void Process(List<Selection> selections)
		{
			
		}
	}

	internal abstract class Selection
	{
		public string Content { get; protected set; }
	}

	internal class RawSelection : Selection
	{
		public RawSelection(string rawText)
		{
			Content = rawText;
		}
	}

	internal class PasswordSelection : Selection
	{
		public PasswordSelection(PasswordFileContent file)
		{
			Content = file.Password;
		}
	}

	internal class MetadataSelection : Selection
	{
		public MetadataSelection(PasswordFileContent file)
		{
			Content = file.ExtraContent;
		}
	}

	internal class UsernameSelection : Selection
	{
		public UsernameSelection(PasswordFileContent file)
		{
			Content = GetUsername(file.Name, file.ExtraContent);
		}

		/// <summary>
		/// Attepts to retrieve the username from a password file.
		/// </summary>
		/// <param name="passwordFile">The name of the password file.</param>
		/// <param name="extraContent">The extra content of the password file.</param>
		/// <returns>A string containing the username if the password file contains one, null if no username was found.</returns>
		public static string GetUsername(string passwordFile, string extraContent)
		{
			var options = ConfigManager.Config.PasswordStore.UsernameDetection.Options;
			switch (ConfigManager.Config.PasswordStore.UsernameDetection.Method)
			{
				case UsernameDetectionMethod.FileName:
					return Path.GetFileName(passwordFile)?.Replace(Program.EncryptedFileExtension, "");
				case UsernameDetectionMethod.LineNumber:
					var extraLines = extraContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
					var lineNumber = options.LineNumber - 2;
					if (lineNumber <= 1) Notifications.Raise("Failed to read username from password file: username-detection.options.line-number must be set to 2 or higher.", Severity.Warning);
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
}
