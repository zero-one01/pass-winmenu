using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PassWinmenu.Configuration;

namespace PassWinmenu.ExternalPrograms
{
	class GpgAgent
	{
		private const string gpgAgentConfigFileName = "gpg-agent.conf";
		private const string managedByPassWinmenuComment = "# This configuration key is automatically managed by pass-winmenu";

		private const string gpgAgentProcessName = "gpg-agent";
		private const string gpgAgentFileName = "gpg-agent.exe";
		private const string gpgConnectAgentFileName = "gpg-connect-agent.exe";
		private readonly TimeSpan agentConnectTimeout = TimeSpan.FromSeconds(2);
		private readonly string gpgInstallDir;


		public GpgAgent(string installDir)
		{
			gpgInstallDir = installDir;
		}

		public void EnsureAgentResponsive()
		{
			// In certain situations, gpg-agent may hang and become unresponsive to input.
			// This will cause any decryption attempts to hang indefinitely, without any indication of what's happening.
			// Since that's obviously not desirable, we need to check whether the gpg-agent is still responsive, and if not, kill and restart it.

			if (Process.GetProcessesByName(gpgAgentProcessName).Length == 0)
			{
				// If gpg-agent isn't running yet, it obviously can't be unresponsive, so we don't have to do anything here.
				return;
			}

			var connectAgent = Path.Combine(gpgInstallDir, gpgConnectAgentFileName);
			if (!File.Exists(connectAgent))
			{
				Log.Send($"Unable to confirm that gpg-agent is alive. No connect-agent found at \"{connectAgent}\".", LogLevel.Warning);
				return;
			}

			Process proc;
			try
			{
				// We have a gpg-agent, let's see if we can connect to it.
				proc = Process.Start(new ProcessStartInfo
				{
					FileName = connectAgent,
					Arguments = "/bye",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				});
			}
			catch (Exception e)
			{
				Log.Send($"Unable to launch connect-agent process ({e.GetType().Name}: {e.Message})", LogLevel.Warning);
				return;
			}

			
			// Now check what the process returns.
			var readTask = proc.StandardError.ReadLineAsync();
			// Give gpg-connect-agent a moment to see if it produces any output.
			if (readTask.Wait(agentConnectTimeout))
			{
				if (readTask.Result == null)
				{
					// Nothing was sent through stdout, which means we successfully connected to a running agent,
					// which means the agent is still responsive, so we won't have to do anything here.
					Log.Send("gpg-agent alive.");
					return;
				}
				else if (readTask.Result.Contains("waiting for agent"))
				{
					Log.Send("gpg-agent is not running, waiting for it to start...");
				}
				else if (readTask.Result.Contains("no running gpg-agent"))
				{
					Log.Send("gpg-agent is not running, waiting for it to start...");
				}
				else
				{
					Log.Send($"gpg-agent produced unexpected output: \"{readTask.Result}\"", LogLevel.Warning);
					Log.Send($"waiting for agent to start...");
				}
				proc.WaitForExit();
				Log.Send("gpg-agent ready.");
			}
			else
			{
				Log.Send($"gpg-connect-agent failed to produce any output within {agentConnectTimeout.TotalSeconds:F} seconds", LogLevel.Warning);
				Log.Send($"gpg-agent is most likely unresponsive and will be restarted", LogLevel.Warning);
				// First, kill the connect-agent process.
				proc.Kill();
				// Now try to find the correct gpg-agent process.
				// We'll start by looking for a process whose filename matches the installation directory we're working with.
				// This means that if there are several gpg-agents running, we will ignore those that our gpg process likely won't try to connect to.
				var matches = Process.GetProcesses().Where(p => p.MainModule.FileName == Path.Combine(Path.GetFullPath(gpgInstallDir), gpgAgentProcessName)).ToList();
				if (matches.Any())
				{
					Log.Send($"Agent process(es) found (\"{gpgInstallDir}\")");
					// This should normally only return one match at most, but in certain cases
					// GPG seems to be able to detect that the agent has become unresponsive, 
					// and will start a new one without killing the old process.
					foreach (var match in matches)
					{
						Log.Send($" > killing gpg-agent {match.Id}, path: \"{match.MainModule.FileName}\"");
						match.Kill();
					}
					// Now that we've killed the agent we presume to be unresponsive,
					// we'll need to re-run our check in order to see if we're able to connect again.
					// If that check fails, it'll fall back to the less surgically precise method below...
					Log.Send($"Agent(s) killed, re-running gpg-agent check.");
					EnsureAgentResponsive();
				}
				else
				{
					// We didn't find any direct matches, so let's widen our search.
					foreach (var match in Process.GetProcessesByName(gpgAgentProcessName))
					{
						Log.Send($" > killing gpg-agent {match.Id}, path: \"{match.MainModule.FileName}\"");
						match.Kill();
					}
				}
			}
		}

		/// <summary>
		/// Update the gpg-agent config file with the given keys.
		/// </summary>
		public void UpdateAgentConfig(Dictionary<string, string> keys, string homeDir)
		{
			var agentConf = Path.Combine(homeDir, gpgAgentConfigFileName);
			if (!File.Exists(agentConf))
			{
				using (File.Create(agentConf)) { }
			}
			var lines = File.ReadAllLines(agentConf);
			var keysToSet = keys.ToList();

			var newLines = UpdateAgentConfigKeyCollection(lines, keysToSet).ToArray();

			if (lines.SequenceEqual(newLines))
			{
				Log.Send("GPG agent config file already contains the correct settings; it'll be left untouched.");
			}
			else
			{
				Log.Send("Modifying GPG agent config file.");
				File.WriteAllLines(agentConf, newLines);
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
