using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PassWinmenu.ExternalPrograms
{
	/// <summary>
	/// Simple wrapper over git.
	/// </summary>
	internal class Git
	{
		private readonly string executable;
		private readonly string repository;

		/// <summary>
		/// Initialises the wrapper.
		/// </summary>
		/// <param name="executable">The name of the git executable. Can be a full filename or the name of an executable contained in %PATH%.</param>
		public Git(string executable, string repository)
		{
			this.executable = executable;
			this.repository = repository;
		}

		/// <summary>
		/// Runs git with the given arguments, and returns everything it prints to its standard output.
		/// </summary>
		/// <param name="arguments">The arguments to be passed to git.</param>
		/// <returns>A (UTF-8 decoded) string containing the text returned by git.</returns>
		/// <exception cref="GpgException">Thrown when git returns a non-zero exit code.</exception>
		private string RunGit(string arguments)
		{
			var proc = Process.Start(new ProcessStartInfo
			{
				FileName = executable,
				Arguments = arguments,
				WorkingDirectory = repository,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				StandardOutputEncoding = Encoding.UTF8
			});
			proc.WaitForExit();
			var result = proc.StandardOutput.ReadToEnd();
			if (proc.ExitCode != 0)
			{
				throw new GitException(proc.ExitCode);
			}
			return result;
		}

		/// <summary>
		/// Updates the password store by running git pull.
		/// </summary>
		/// <returns>A message containing information about the files that were changed.</returns>
		public string Update()
		{
			var pull = RunGit("pull");
			var match = Regex.Match(pull, @"(\d*?) (file.?) changed");
			if (match.Success)
			{
				var have = match.Groups[2].Value == "files" ? "have" : "has";
				var sb = new StringBuilder();
				sb.AppendLine($"The password store has been updated.\n{match.Groups[1].Value} {match.Groups[2].Value} {have} been changed.");
				var lines = new List<string>();
				foreach (var line in pull.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries))
				{
					match = Regex.Match(line, @"create mode \d+ (.*)");
					if (match.Success)
					{
						lines.Add($"added {match.Groups[1].Value}");
					}
					match = Regex.Match(line, @"delete mode \d+ (.*)");
					if (match.Success)
					{
						lines.Add($"deleted {match.Groups[1].Value}");
					}
				}
				if(lines.Count > 0) sb.AppendLine("Changes: " + string.Join(", ", lines));

				return sb.ToString();
			}
			else if (Regex.IsMatch(pull, @"Already up-to-date\."))
			{
				return "The password store is up-to-date.";
			}
			else
			{
				return $"Git returned an unknown result: \n\"{pull}\"";
			}
		}

	}
	
	internal class GitException : Exception
	{
		public int ExitCode { get; }

		public GitException(int exitCode)
		{
			ExitCode = exitCode;
		}
	}
}
