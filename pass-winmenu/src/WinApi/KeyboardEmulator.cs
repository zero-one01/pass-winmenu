using System.Linq;
using System.Windows.Forms;

namespace PassWinmenu.WinApi
{
	static class KeyboardEmulator
	{
		/// <summary>
		/// Sends text directly to the topmost window, as if it was entered by the user.
		/// This method automatically escapes all characters with special meaning, 
		/// then calls SendKeys.Send().
		/// </summary>
		/// <param name="text">The text to be sent to the active window.</param>
		/// <param name="escapeDeadKeys">Whether dead keys should be escaped or not. 
		/// If true, inserts a space after every dead key in order to prevent it from being combined with the next character.</param>
		internal static void EnterText(string text, bool escapeDeadKeys)
		{
			if (escapeDeadKeys)
			{
				// If dead keys are enabled, insert a space directly after each dead key to prevent
				// it from being combined with the character following it.
				// See https://en.wikipedia.org/wiki/Dead_key
				var deadKeys = new[] { "\"", "'", "`", "~", "^" };
				text = deadKeys.Aggregate(text, (current, key) => current.Replace(key, key + " "));
			}

			// SendKeys.Send expects special characters to be escaped by wrapping them with curly braces.
			var specialCharacters = new[] { '{', '}', '[', ']', '(', ')', '+', '^', '%', '~' };
			var escaped = string.Concat(text.Select(c => specialCharacters.Contains(c) ? $"{{{c}}}" : c.ToString()));
			SendKeys.Send(escaped);
		}

		internal static void EnterRawText(string text)
		{
			SendKeys.Send(text);
		}
	}
}
