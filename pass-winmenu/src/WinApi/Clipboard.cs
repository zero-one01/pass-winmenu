using System;
using System.Threading.Tasks;
using System.Windows;

namespace PassWinmenu.WinApi
{
	public class ClipboardHelper
	{
		/// <summary>
		/// Copies a string to the clipboard. If it still exists on the clipboard after the amount of time
		/// specified in <paramref name="timeout"/>, it will be removed again.
		/// </summary>
		/// <param name="text">The text to add to the clipboard.</param>
		/// <param name="timeout">The amount of time, in seconds, the text should remain on the clipboard.</param>
		public void Place(string text, TimeSpan timeout)
		{
			//Clipboard.Flush();
			var previousData = Clipboard.GetDataObject();
			Log.Send("Saving previous clipboard contents before storing the password");
			Log.Send($" - Formats: {string.Join(", ", previousData.GetFormats())}");

			Clipboard.Clear();
			Clipboard.SetDataObject(text);

			Task.Delay(timeout).ContinueWith(_ =>
			{
				// TODO: Invoke this
				try
				{
					// Only reset the clipboard to its previous contents if it still contains the text we copied to it.
					// If the clipboard did not previously contain any text, it is simply cleared.
					if (Clipboard.ContainsText() && Clipboard.GetText() == text)
					{
						Log.Send("Restoring previous clipboard contents");
						Clipboard.SetDataObject(previousData);
					}
				}
				catch (Exception e)
				{
					Log.Send($"Failed to restore previous clipboard contents: An exception occurred ({e.GetType().Name}: {e.Message})", LogLevel.Error);
				}
			});
		}

		public string GetText()
		{
			return Clipboard.ContainsText() ? Clipboard.GetText() : null;
		}
	}
}
