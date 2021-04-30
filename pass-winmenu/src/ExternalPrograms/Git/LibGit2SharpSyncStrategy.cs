using System.Linq;

using LibGit2Sharp;

namespace PassWinmenu.ExternalPrograms
{
	internal class LibGit2SharpSyncStrategy : IGitSyncStrategy
	{
		private readonly Repository repo;
		private readonly PushOptions pushOptions;
		private readonly FetchOptions fetchOptions;

		public LibGit2SharpSyncStrategy(Repository repo)
		{
			this.repo = repo;
			fetchOptions = new FetchOptions();
			pushOptions = new PushOptions();
		}

		public void Fetch(Branch branch)
		{
			if (branch.RemoteName == null)
			{
				throw new GitException($"Branch \"{branch.FriendlyName}\" does not have a remote configured.");
			}
			
			var remote = repo.Network.Remotes[branch.RemoteName];

			Commands.Fetch(repo, branch.RemoteName, remote.FetchRefSpecs.Select(rs => rs.Specification), fetchOptions, null);
		}

		/// <summary>
		/// Pushes changes to remote.
		/// </summary>
		public void Push()
		{
			repo.Network.Push(repo.Head, pushOptions);
		}
	}
}
