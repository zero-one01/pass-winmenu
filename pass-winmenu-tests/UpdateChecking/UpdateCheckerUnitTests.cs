using System;
using System.Collections.Generic;
using System.Threading;
using McSherry.SemanticVersioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PassWinmenu.UpdateChecking.Dummy;

namespace PassWinmenu.UpdateChecking
{
	[TestClass]
	public class UpdateCheckerUnitTests
	{
		private const string Category = "Updates: update sources";

		[TestMethod, TestCategory(Category)]
		public void UpdateChecker_TriggersOnNewerVersion()
		{
			var raisesMajor = RaisesEvent(new SemanticVersion(1, 1, 1), new SemanticVersion(2, 0, 0));
			Assert.IsTrue(raisesMajor);
			var raisesMinor = RaisesEvent(new SemanticVersion(1, 1, 1), new SemanticVersion(1, 2, 0));
			Assert.IsTrue(raisesMinor);
			var raisesPatch = RaisesEvent(new SemanticVersion(1, 1, 1), new SemanticVersion(1, 1, 2));
			Assert.IsTrue(raisesPatch);
		}

		[TestMethod, TestCategory(Category)]
		public void UpdateChecker_IgnoresEqualVersion()
		{
			var raisesEqual = RaisesEvent(new SemanticVersion(1, 0, 0), new SemanticVersion(1, 0, 0));
			Assert.IsFalse(raisesEqual);
		}

		[TestMethod, TestCategory(Category)]
		public void UpdateChecker_IgnoresOlderVersion()
		{
			var raisesMajor = RaisesEvent(new SemanticVersion(2, 2, 2), new SemanticVersion(1, 3, 3));
			Assert.IsFalse(raisesMajor);
			var raisesMinor = RaisesEvent(new SemanticVersion(2, 2, 2), new SemanticVersion(2, 1, 3));
			Assert.IsFalse(raisesMinor);
			var raisesPatch = RaisesEvent(new SemanticVersion(2, 2, 2), new SemanticVersion(2, 2, 1));
			Assert.IsFalse(raisesPatch);
		}

		[TestMethod, TestCategory(Category)]
		public void UpdateChecker_RaisesAfterSpecifiedTime()
		{
			const int checkBeforeMs = 800;
			const int raiseAfterMs = 1000;
			const int checkAgainMs = 400;
			const int checkAttempts = 10;

			var source = new DummyUpdateSource
			{
				Versions = new List<ProgramVersion>
				{
					new ProgramVersion
					{
						VersionNumber = new SemanticVersion(1, 0, 1)
					}
				}
			};

			var checker = new UpdateChecker(source, new SemanticVersion(1, 0, 0), false, TimeSpan.FromMilliseconds(raiseAfterMs));

			var raised = false;
			checker.UpdateAvailable += (sender, args) => { raised = true; };
			checker.Start();

			// Validate that the event is not raised before the time specified in raiseAfterMs has expired.
			Thread.Sleep(checkBeforeMs);
			Assert.IsFalse(raised, "Notification was raised before update interval expired");

			// Validate that the event has been raised now.
			Thread.Sleep(checkAgainMs);
			for (int i = 0; i < checkAttempts && !raised; i++)
			{
				Thread.Sleep(checkAgainMs);
			}

			Assert.IsTrue(raised, "Notification was not raised");
		}

		[TestMethod, TestCategory(Category)]
		public void UpdateChecker_ProvidesCorrectReleaseType()
		{
			var sourceWithNewerPrerelease = new DummyUpdateSource
			{
				Versions = new List<ProgramVersion>
				{
					new ProgramVersion
					{
						VersionNumber = new SemanticVersion(3, 0, 0),
						IsPrerelease = true
					},
					new ProgramVersion
					{
						VersionNumber = new SemanticVersion(2, 0, 0),
					}
				}
			};
			var sourceWithOlderPrerelease = new DummyUpdateSource
			{
				Versions = new List<ProgramVersion>
				{
					new ProgramVersion
					{
						VersionNumber = new SemanticVersion(3, 0, 0),
					},
					new ProgramVersion
					{
						VersionNumber = new SemanticVersion(2, 0, 0),
						IsPrerelease = true
					},
					new ProgramVersion
					{
					VersionNumber = new SemanticVersion(1, 0, 0)
					}
				}
			};

			// An update checker that may not return prereleases should return the latest non-prerelease version.
			using (var checker = new UpdateChecker(sourceWithNewerPrerelease, new SemanticVersion(1, 0, 0), false, TimeSpan.FromMilliseconds(1)))
			{
				AssertUpdateCheck(checker, (sender, args) =>
				{
					Assert.IsFalse(args.Version.IsPrerelease);
					Assert.AreEqual(args.Version.VersionNumber, new SemanticVersion(2, 0, 0));
				});
			}

			// An update checker that may return prereleases should return a prerelease if it's the latest version.
			using (var checker = new UpdateChecker(sourceWithNewerPrerelease, new SemanticVersion(1, 0, 0), true, TimeSpan.FromMilliseconds(1)))
			{
				AssertUpdateCheck(checker, (sender, args) =>
				{
					Assert.IsTrue(args.Version.IsPrerelease);
					Assert.AreEqual(args.Version.VersionNumber, new SemanticVersion(3, 0, 0));
				});
			}


			// An update checker that may return prereleases should not return a prerelease if there's a newer general release.
			using (var checker = new UpdateChecker(sourceWithOlderPrerelease, new SemanticVersion(1, 0, 0), true, TimeSpan.FromMilliseconds(1)))
			{
				AssertUpdateCheck(checker, (sender, args) =>
				{
					Assert.IsFalse(args.Version.IsPrerelease);
					Assert.AreEqual(args.Version.VersionNumber, new SemanticVersion(3, 0, 0));
				});
			}
		}

		private void AssertUpdateCheck(UpdateChecker checker, EventHandler<UpdateAvailableEventArgs> handler)
		{

			var raised = false;
			checker.UpdateAvailable += handler;
			checker.UpdateAvailable += (sender, args) => raised = true;
			checker.Start();

			// Wait a while to see if the event is raised.
			for (var i = 0; i < 20; i++)
			{
				Thread.Sleep(50);
				if (raised) return;
			}
			Assert.Fail("Update checker did not raise event.");
		}

		/// <summary>
		/// Checks whether supplying a given update version against a given base version raises an update-available event.
		/// </summary>
		private bool RaisesEvent(SemanticVersion currentVersion, SemanticVersion updateVersion)
		{
			var source = new DummyUpdateSource
			{
				Versions = new List<ProgramVersion>
				{
					new ProgramVersion
					{
						VersionNumber = updateVersion
					}
				}
			};

			var checker = new UpdateChecker(source, currentVersion, false, TimeSpan.FromMilliseconds(1));

			var raised = false;
			checker.UpdateAvailable += (sender, args) =>
			{
				raised = true;
				checker.Dispose();
			};
			checker.Start();

			// Wait a while to see if the event is raised.
			for (var i = 0; i < 20; i++)
			{
				Thread.Sleep(50);
				if (raised) return true;
			}

			return raised;
		}
	}
}
