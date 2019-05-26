using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgAgentConfigUpdater
	{
		private const string managedByPassWinmenuComment = "# This configuration key is automatically managed by pass-winmenu";

		private readonly GpgAgentConfigReader reader;

		public GpgAgentConfigUpdater(GpgAgentConfigReader reader)
		{
			this.reader = reader;
		}

		/// <summary>
		/// Update the gpg-agent config file with the given keys.
		/// </summary>
		public void UpdateAgentConfig(Dictionary<string, string> keys)
		{
			string[] lines;
			try
			{
				lines = reader.ReadConfigLines();
			}
			catch (Exception e)
			{
				Log.Send("Could not read agent config file. Updating it will not be possible.", LogLevel.Warning);
				Log.ReportException(e);
				return;
			}

			var newLines = UpdateAgentConfigKeyCollection(lines, keys.ToList()).ToArray();

			if (lines.SequenceEqual(newLines))
			{
				Log.Send("GPG agent config file already contains the correct settings; it'll be left untouched.");
				return;
			}

			Log.Send($"Modifying GPG agent config file ({string.Join(", ", keys.Keys)})");
			try
			{
				reader.WriteConfigLines(lines);
			}
			catch (Exception e)
			{
				Log.Send("Could not update agent config file.", LogLevel.Warning);
				Log.ReportException(e);
			}
		}

		/// <summary>
		/// Iterates over a list of config lines, adding or replacing the given config keys.
		/// </summary>
		private IEnumerable<string> UpdateAgentConfigKeyCollection(string[] existingLines, List<KeyValuePair<string, string>> keysToSet)
		{
			var configKeyRegex = new Regex(@"^(\s*([^#^\s][^\s]*)\s+)(.*)$");
			for (var i = 0; i < existingLines.Length; i++)
			{
				var line = existingLines[i];
				var match = configKeyRegex.Match(line);
				if (match.Success)
				{
					// This looks like a config key, let's see if we're supposed to change it.
					var key = match.Groups[2].Value;
					var matchedPair = keysToSet.FirstOrDefault(k => k.Key == key);
					if (matchedPair.Key == key)
					{
						// This key will need to be changed. Let's remove it from the list first.
						keysToSet.RemoveAll(k => k.Key == key);

						// Insert a comment explaining that we're managing this key,
						// unless such a comment already exists.
						if (i == 0 || existingLines[i - 1] != managedByPassWinmenuComment)
						{
							yield return managedByPassWinmenuComment;
						}
						// Now return the updated key-value pair.
						yield return $"{matchedPair.Key} {matchedPair.Value}";
					}
					else
					{
						yield return line;
					}
				}
				// TODO: should we not yield the line here?
			}

			while (keysToSet.Any())
			{
				// Looks like some of the keys we need to set aren't in the config file yet, so let's add them.
				var next = keysToSet[0];
				keysToSet.RemoveAt(0);
				yield return managedByPassWinmenuComment;
				yield return $"{next.Key} {next.Value}";
			}
		}
	}
}
