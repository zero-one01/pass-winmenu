using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PassWinmenu
{
	class PasswordFileParser
	{
		/// <summary>
		/// Extracts the username and any possible metadata from a password file
		/// by auto-detecting the correct line-endings.
		/// </summary>
		/// <param name="content">Content of the password file</param>
		/// <param name="entireFile">If set to true, any line endings are considered to be part of the password.</param>
		/// <returns>A <see cref="PasswordFileContent"/> structure containing the password and metadata</returns>
		public PasswordFileContent Parse(string content, bool entireFile)
		{
			if (entireFile)
			{
				// If the password contains any line endings, there is no additional metadata available.
				return new PasswordFileContent(content, null);
			}
			else
			{
				// The first line contains the password, any other lines contain additional (contextual) content.
				var match = Regex.Match(content, @"([^\n\r]*)(?:(?:\r\n|\n|\r)(.*))?", RegexOptions.Singleline);
				var password = match.Groups[1].Value;
				var extraContent = match.Groups[2].Value;

				return new PasswordFileContent(password, extraContent);
			}
		}
	}
}
