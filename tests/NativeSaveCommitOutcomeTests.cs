using System;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Tests
{
    // What happened to a save's archive, decided from the recorded commit
    // boundary and never inferred from task state; and the one report shared by
    // every path a save can end on. No game types.
    internal static class NativeSaveCommitOutcomeTests
    {
        private const string Target = @"C:\saves\Manual_300_KMC_P01.zks";
        private const string Prepared = @"C:\saves\Manual_301_KMC_P01.zks";
        private const string Foreign = @"C:\saves\Manual_900_OTHER.zks";

        public static void Register(TestRunner runner)
        {
            runner.Run("no commit during the operation is not written", NoCommit);
            runner.Run("a commit on the requested archive is written", CommitOnTarget);
            runner.Run("a commit on the operation's own prepared leaf is written", CommitOnPrepared);
            runner.Run("a commit that then fails in cleanup is still written", PostCommitFault);
            runner.Run("a commit belonging to another operation is unconfirmed", ForeignCommit);
            runner.Run("a recorded commit whose archive is absent is unconfirmed", CommitAbsent);
            runner.Run("a commit with no recorded destination is unconfirmed", NoDestination);
            runner.Run("a first-ever save that never committed is not written", FirstEver);
            runner.Run("a second queued save is not credited with the first save's commit", QueuedAttribution);
            runner.Run("report: written even when later cleanup failed, and not counted as failed", ReportCommitted);
            runner.Run("report: unconfirmed never promises unchanged bytes", ReportUnconfirmed);
            runner.Run("report: unchanged previous save only when one existed and is still there", ReportPreviousUnchanged);
            runner.Run("report: earlier save no longer in place is said plainly", ReportPreviousGone);
            runner.Run("report: a first-ever save has no previous save to promise", ReportFirstEver);
        }

        private static Func<string, bool> Present(bool present) => path => present;

        private static void NoCommit()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 4, null, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.NotWritten, kind, "Nothing committed.");
        }

        private static void CommitOnTarget()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 5, Target, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Committed, kind, "Commit on the requested archive.");
        }

        private static void CommitOnPrepared()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 5, Prepared, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Committed, kind, "Commit on the operation's own prepared leaf.");
        }

        private static void PostCommitFault()
        {
            // The decision sees only the commit boundary. Whatever the worker did
            // afterwards -- faulting in cleanup, notification failing -- does not
            // change the bytes and must not change this answer.
            var kind = NativeSaveCommitOutcome.Decide(4, 5, Target, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Committed, kind, "A post-commit fault is still a written save.");
        }

        private static void ForeignCommit()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 5, Foreign, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Unconfirmed, kind, "Another operation's commit proves nothing here.");
        }

        private static void CommitAbsent()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 5, Target, Target, Prepared, Present(false));
            TestRunner.Equal(NativeSaveCommitKind.Unconfirmed, kind, "A recorded commit with no archive on disk is unconfirmed.");
        }

        private static void NoDestination()
        {
            var kind = NativeSaveCommitOutcome.Decide(4, 5, null, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Unconfirmed, kind, "A commit with no destination is unconfirmed.");
        }

        private static void FirstEver()
        {
            var kind = NativeSaveCommitOutcome.Decide(0, 0, null, null, null, Present(false));
            TestRunner.Equal(NativeSaveCommitKind.NotWritten, kind, "A first-ever save that never committed is not written.");
        }

        private static void QueuedAttribution()
        {
            // Two saves queued to the same slot. The first began at commit count
            // 4 and committed (5). The second BEGAN after that, at 5, and failed
            // before committing: the count it sees is still 5.
            var first = NativeSaveCommitOutcome.Decide(4, 5, Target, Target, Prepared, Present(true));
            var second = NativeSaveCommitOutcome.Decide(5, 5, Target, Target, Prepared, Present(true));
            TestRunner.Equal(NativeSaveCommitKind.Committed, first, "The first save committed.");
            TestRunner.Equal(NativeSaveCommitKind.NotWritten, second, "The second save must not be credited with the first's commit.");
        }

        private static void ReportCommitted()
        {
            var report = NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.Committed, true, true, "cleanup threw");
            TestRunner.True(!report.CountsAsFailed, "A committed save is not a failed save.");
            TestRunner.True(report.Message.StartsWith("The save was written", StringComparison.Ordinal), "Says it was written: " + report.Message);
            TestRunner.True(report.Message.Contains("cleanup threw"), "Carries the detail.");
            TestRunner.True(!report.Message.Contains("unchanged"), "Never talks about a previous save when this one was written.");
        }

        private static void ReportUnconfirmed()
        {
            var report = NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.Unconfirmed, true, true, null);
            TestRunner.True(report.CountsAsFailed, "Unconfirmed counts as failed.");
            TestRunner.True(!report.Message.Contains("unchanged"), "Unconfirmed must never promise unchanged bytes: " + report.Message);
            TestRunner.True(report.Message.Contains("could not be confirmed"), "Says so plainly.");
        }

        private static void ReportPreviousUnchanged()
        {
            var report = NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.NotWritten, true, true, "timed out");
            TestRunner.True(report.CountsAsFailed, "Not written counts as failed.");
            TestRunner.True(report.Message.Contains("the previous complete save is unchanged"), "Only now may unchanged bytes be claimed.");
            TestRunner.True(report.Message.EndsWith("timed out", StringComparison.Ordinal), "Carries the detail.");
        }

        private static void ReportPreviousGone()
        {
            var report = NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.NotWritten, true, false, null);
            TestRunner.True(report.CountsAsFailed, "Counts as failed.");
            TestRunner.True(report.Message.Contains("no longer in place"), "Says the earlier save is gone: " + report.Message);
            TestRunner.True(!report.Message.Contains("unchanged"), "Must not claim unchanged bytes.");
        }

        private static void ReportFirstEver()
        {
            var report = NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.NotWritten, false, false, null);
            TestRunner.True(report.CountsAsFailed, "Counts as failed.");
            TestRunner.True(report.Message.Contains("no earlier save in this slot"), "A first-ever save has no previous archive: " + report.Message);
            TestRunner.True(!report.Message.Contains("unchanged"), "Must not promise a previous save that never existed.");
        }
    }
}
