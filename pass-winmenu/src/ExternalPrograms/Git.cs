using System;
using System.Collections.Generic;
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
		/// <param name="repository">The repository git should operate on.</param>
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
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8
			});
			proc.WaitForExit();
			var result = proc.StandardOutput.ReadToEnd();
			if (proc.ExitCode != 0)
			{
				throw new GitException(proc.ExitCode, result, proc.StandardError.ReadToEnd());
			}
			return result;
		}

		public PullResult Pull()
		{
			RunGit("fetch");

			var head = RunGit("rev-parse --abbrev-ref HEAD").Replace("\n", string.Empty);
			var remote = RunGit("rev-parse --abbrev-ref --symbolic-full-name @{u}").Replace("\n", string.Empty);

			var log = RunGit($"log --name-status {head}..{remote}");

			var match = Regex.Match(log, @"(?:commit .*?\n\n    (.*?)\n\n(.*?\n)(\n|$))+", RegexOptions.Singleline);
			var commitMessages = match.Groups[1].Captures;
			var commitFiles = match.Groups[2].Captures;

			var commits = new List<Commit>();

			for (var i = 0; i < commitMessages.Count; i++)
			{
				var message = commitMessages[i].Value;
				var fileDetails = commitFiles[i].Value;

				var fileMatch = Regex.Match(fileDetails, @"(?:(D|M|A)\s+(.*)\n)+");
				var fileModes = fileMatch.Groups[1].Captures;
				var fileNames = fileMatch.Groups[2].Captures;

				var fileList = new List<GitFile>();

				for (var j = 0; j < fileModes.Count; j++)
				{
					GitFileStatus mode;
					switch (fileModes[j].Value)
					{
						case "D":
							mode = GitFileStatus.Deleted;
							break;
						case "M":
							mode = GitFileStatus.Modified;
							break;
						case "A":
							mode = GitFileStatus.NewFile;
							break;
						default:
							throw new InvalidOperationException($"Invalid file mode: \"{fileModes[j].Value}\"");

					}

					fileList.Add(new GitFile(fileNames[j].Value, mode));
				}
				commits.Add(new Commit(message, fileList));
			}
			// Change the commit order from newest first to oldest first.
			commits.Reverse();

			// Now rebase to apply these commits to HEAD
			var rebase = RunGit("rebase");

			return new PullResult(commits);
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
			match = Regex.Match(pull, @"Fast-forwarded master to (.*)\.");
			if (match.Success)
			{
				return $"Fast-forwarded master to {match.Groups[1].Value}";
			}
			else if (Regex.IsMatch(pull, @"Already up-to-date\.") || Regex.IsMatch(pull, @"Current branch .* is up to date\."))
			{
				return "The password store is already up to date.";
			}
			else
			{
				return $"Git returned an unknown result: \n\"{pull}\"";
			}
		}

		private GitStatus GetGitStatus()
		{
			var changes = RunGit("status");
			// Break up the result into three groups. Changes to be committed, unstaged changes and untracked files.
			var result = Regex.Match(changes, @"(?:Changes to be committed:(.*?))?(?:Changes not staged for commit:(.*?))?(?:Untracked files:(.*?))?$", RegexOptions.Singleline);

			var gitStatus = new GitStatus();
			// Extract the filenames for each group.
			// Added files
			{
				var added = result.Groups[1].Value;
				var match = Regex.Match(added, @"(?:\t(.*):\s+(.*)(?:\n|\r|\r\n))+");
				var fileStatus = match.Groups[1].Captures;
				var filename = match.Groups[2].Captures;
				var files = new List<GitFile>();
				for (var i = 0; i < fileStatus.Count; i++)
				{
					files.Add(new GitFile(filename[i].Value, GetStatus(fileStatus[i].Value)));
				}
				gitStatus.AddedFiles = files;
			}
			// Changed files
			{
				var changed = result.Groups[2].Value;
				var match = Regex.Match(changed, @"(?:\t(.*):\s+(.*)(?:\n|\r|\r\n))+");
				var fileStatus = match.Groups[1].Captures;
				var filename = match.Groups[2].Captures;
				var files = new List<GitFile>();
				for (var i = 0; i < fileStatus.Count; i++)
				{
					files.Add(new GitFile(filename[i].Value, GetStatus(fileStatus[i].Value)));
				}
				gitStatus.ChangedFiles = files;
			}
			// Untracked files
			{
				var untracked = result.Groups[3].Value;
				var match = Regex.Match(untracked, @"(?:\t(.*)(?:\n|\r|\r\n))+");
				var filename = match.Groups[1].Captures;
				var files = new List<GitFile>();
				for (var i = 0; i < filename.Count; i++)
				{
					files.Add(new GitFile(filename[i].Value, GitFileStatus.NewFile));
				}
				gitStatus.UntrackedFiles = files;
			}

			return gitStatus;
		}

		private GitFileStatus GetStatus(string status)
		{
			switch (status)
			{
				case "new file":
					return GitFileStatus.NewFile;
				case "modified":
					return GitFileStatus.Modified;
				case "deleted":
					return GitFileStatus.Deleted;
				default:
					throw new ArgumentException("Invalid git file status descriptor", nameof(status));
			}
		}

		public CommitResult Commit()
		{
			var status = GetGitStatus();

			// Some files have already been added.
			// Unstage them so they can be committed individually.
			if (status.AddedFiles.Count > 0)
			{
				foreach (var file in status.AddedFiles)
				{
					RunGit($"reset HEAD \"{file.Name}\"");
				}
				// Now try committing again.
				
				return Commit();
			}
			var changes = new List<GitFile>();
			// First, commit each changed file.
			foreach (var file in status.ChangedFiles)
			{
				RunGit($"add \"{file.Name}\"");
				RunGit($"commit -m \"{GetVerbFromGitFileStatus(file.Status)} password store file {file.Name}\n\nThis commit was automatically generated by pass-winmenu.\"");
				changes.Add(file);
			}
			// Now, commit each untracked file.
			foreach (var file in status.UntrackedFiles)
			{
				RunGit($"add \"{file.Name}\"");
				RunGit($"commit -m \"Add {file.Name} to password store\n\nThis commit was automatically generated by pass-winmenu.\"");
				changes.Add(file);
			}
			// Ensure we're up to date first.
			var updates = Pull();
			// Finally, push our commmits to remote.
			RunGit("push");
			return new CommitResult(updates, changes);
		}

		private string GetVerbFromGitFileStatus(GitFileStatus status)
		{
			switch (status)
			{
				case GitFileStatus.Deleted:
					return "Delete";
					case GitFileStatus.NewFile:
					return "Create";
					default:
					return "Update";
			}
		}
	}

	internal class CommitResult
	{
		public PullResult Pull { get; private set; }
		public List<GitFile> CommittedFiles { get; private set; }

		public CommitResult(PullResult pull, List<GitFile> committedFiles)
		{
			Pull = pull;
			CommittedFiles = committedFiles;
		}
	}

	internal class PullResult
	{
		public List<Commit> Commits { get; private set; }

		public PullResult(IEnumerable<Commit> commits)
		{
			Commits = commits.ToList();
		}
	}

	internal class Commit
	{
		public string Message { get; }
		public List<GitFile> Files { get; }

		public Commit(string message, List<GitFile> files)
		{
			Message = message;
			Files = files;
		}

		public override string ToString()
		{
			return Message;
		}
	}


	internal class GitFile
	{
		public string Name { get; }
		public GitFileStatus Status { get; }

		public GitFile(string name, GitFileStatus status)
		{
			Name = name;
			Status = status;
		}

		public override string ToString()
		{
			return $"{Name} ({Status})";
		}
	}

	internal enum GitFileStatus
	{
		NewFile,
		Modified,
		Deleted
	}

	internal class GitStatus
	{
		public List<GitFile> AddedFiles { get;  set; }
		public List<GitFile> ChangedFiles { get;  set; }
		public List<GitFile> UntrackedFiles { get;  set; }

		public GitStatus()
		{
			
		}

		public GitStatus(List<GitFile> addedFiles, List<GitFile> changedFiles, List<GitFile> untrackedFiles)
		{
			AddedFiles = addedFiles;
			ChangedFiles = changedFiles;
			UntrackedFiles = untrackedFiles;
		}

	}
	
	internal class GitException : Exception
	{
		public int ExitCode { get; }
		public string GitOutput { get; }
		public string GitError { get; }

		public GitException(int exitCode, string output, string error)
		{
			ExitCode = exitCode;
			GitOutput = output;
			GitError = error;
		}
	}
}
