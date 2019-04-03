using LibGit2Sharp;

using PassWinmenu.Configuration;

namespace PassWinmenu.ExternalPrograms
{
	internal class SyncServiceFactory
	{
		// TODO: Stop relying on a concrete implementation
		public Git BuildSyncService(GitConfig config, string passwordStorePath)
		{
			var repository = new Repository(passwordStorePath);

			IGitSyncStrategy strategy;
			if (config.SyncMode == SyncMode.NativeGit)
			{
				strategy = new NativeGitSyncStrategy(config.GitPath);
			}
			else
			{
				strategy = new LibGit2SharpSyncStrategy(repository);
			}

			return new Git(repository, strategy);
		}
	}
}
