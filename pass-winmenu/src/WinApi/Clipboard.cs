using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using PassWinmenu.Utilities;

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
			Helpers.AssertOnUiThread();

			var clipboardBackup = new Dictionary<string, object>();
			var dataObject = Clipboard.GetDataObject();
			if (dataObject != null)
			{
				Log.Send("Saving previous clipboard contents before storing the password");
				Log.Send($" - Formats: {string.Join(", ", dataObject.GetFormats(false))}");
				foreach (var format in dataObject.GetFormats(false))
				{
					clipboardBackup[format] = dataObject.GetData(format, false);
				}
			}
			Clipboard.SetText(text, TextDataFormat.UnicodeText);

			Task.Delay(timeout).ContinueWith(_ =>
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					try
					{
						// Only reset the clipboard to its previous contents if it still contains the text we copied to it.
						if (Clipboard.ContainsText() && Clipboard.GetText() == text)
						{
							// First clear the clipboard, to ensure our text is gone,
							// even if restoring the previous content fails.
							Clipboard.Clear();
							// Now try to restore the previous content.
							Log.Send($"Restoring previous clipboard contents:");
							foreach (var pair in clipboardBackup)
							{
								Log.Send($" - {pair.Key}");
								Clipboard.SetData(pair.Key, pair.Value);
							}
						}
					}
					catch (Exception e)
					{
						Log.Send($"Failed to restore previous clipboard contents: An exception occurred ({e.GetType().Name}: {e.Message})", LogLevel.Error);
					}
				});
			});
		}

		public string GetText()
		{
			return Clipboard.ContainsText() ? Clipboard.GetText() : null;
		}
	}
}
