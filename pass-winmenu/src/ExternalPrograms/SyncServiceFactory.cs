using System;
using LibGit2Sharp;

using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.ExternalPrograms
{
	internal class SyncServiceFactory
	{
		private readonly GitConfig config;
		private readonly string passwordStorePath;
		private readonly ISignService signService;

		public SyncServiceStatus Status { get; private set; }
		public Exception Exception { get; private set; }

		public SyncServiceFactory(GitConfig config, string passwordStorePath, ISignService signService)
		{
			this.config = config;
			this.passwordStorePath = passwordStorePath;
			this.signService = signService;
		}
		
		public ISyncService BuildSyncService()
		{
			if (config.UseGit)
			{
				try
				{
					var repository = new Repository(passwordStorePath);

					var strategy = ChooseSyncStrategy(repository);
					var git = new Git(repository, strategy, signService);
					Status = SyncServiceStatus.GitSupportEnabled;
					return git;
				}
				catch (RepositoryNotFoundException)
				{
					// Password store doesn't appear to be a Git repository.
					// Git support will be disabled.
				}
				catch (TypeInitializationException e) when (e.InnerException is DllNotFoundException)
				{
					Status = SyncServiceStatus.GitLibraryNotFound;
				}
				catch (Exception e)
				{
					Exception = e;
					Status = SyncServiceStatus.GitRepositoryNotFound;
				}
			}
			else
			{
				Status = SyncServiceStatus.GitSupportDisabled;
			}

			return null;
		}

		private IGitSyncStrategy ChooseSyncStrategy(Repository repository)
		{
			if (config.SyncMode == SyncMode.NativeGit)
			{
				return new NativeGitSyncStrategy(config.GitPath, passwordStorePath);
			}
			else
			{
				return new LibGit2SharpSyncStrategy(repository);
			}
		}
	}

	internal enum SyncServiceStatus
	{
		GitSupportEnabled,
		GitLibraryNotFound,
		GitRepositoryNotFound,
		GitSupportDisabled,
	}
}
