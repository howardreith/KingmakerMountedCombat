using System;
using System.IO;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class BoundaryScenarioEvidenceContractTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("boundary evidence suite order is exact", SuiteOrderIsExact);
            runner.Run("boundary evidence accepts complete simple and loading rows", CompleteRowsAreAccepted);
            runner.Run("boundary evidence rejects missing PASS phases", MissingPassPhaseIsRejected);
            runner.Run("boundary evidence rejects intermediate phase skips", IntermediatePhaseSkipIsRejected);
            runner.Run("boundary evidence permits truthful failed prefixes", TruthfulFailedPrefixIsAccepted);
            runner.Run("boundary evidence rejects row reorder and post-terminal append", ReorderAndPostTerminalAreRejected);
            runner.Run("boundary evidence permits only terminal suppressed rows", SuppressedRowsAreFailClosed);
            runner.Run("boundary evidence never reopens Working during active load phases", ActiveLoadIdentityUsesExplicitCache);
            runner.Run("boundary evidence journal is create-new append-close", JournalIsCreateNewAppendClose);
            runner.Run("boundary evidence journal fails closed after disappearance", JournalFailsClosedAfterDisappearance);
            runner.Run("boundary evidence journal rejects same-length mutation", JournalRejectsSameLengthMutation);
        }

        private static void SuiteOrderIsExact()
        {
            var rows = BoundaryScenarioEvidenceContract.SelectRows("boundary-suite");
            TestRunner.Equal(5, rows.Count, "Boundary suite row count changed.");
            TestRunner.Equal("mounted-pair-turn-based-entry-cleanup", rows[0], "Turn-based boundary was not first.");
            TestRunner.Equal("mounted-pair-realtime-entry-cleanup", rows[1], "Realtime boundary was not second.");
            TestRunner.Equal("mounted-pair-save-safety", rows[2], "Save boundary was not third.");
            TestRunner.Equal("mounted-pair-load-safety", rows[3], "Load boundary was not fourth.");
            TestRunner.Equal("mounted-pair-area-transition-safety", rows[4], "Area boundary was not fifth.");
            TestRunner.Equal(null, BoundaryScenarioEvidenceContract.SelectRows("mounted-pair-load-safety-near-match"),
                "A near-match boundary scenario was accepted.");

            TestRunner.True(ThrowsArgument(() => new BoundaryEvidenceSequenceGuard(new[] { rows[1], rows[0] })),
                "The sequence guard accepted a reordered partial suite selection.");
        }

        private static void CompleteRowsAreAccepted()
        {
            foreach (var row in new[]
            {
                "mounted-pair-save-safety",
                "mounted-pair-load-safety"
            })
            {
                var guard = new BoundaryEvidenceSequenceGuard(new[] { row });
                var phases = BoundaryScenarioEvidenceContract.PassPhases(row);
                foreach (var phase in phases)
                {
                    guard.Accept(row, phase, phase == "row-result" ? "PASS" : null, true, false);
                }
                TestRunner.True(guard.IsComplete, "Complete boundary evidence did not close its row.");
            }
        }

        private static void MissingPassPhaseIsRejected()
        {
            const string row = "mounted-pair-load-safety";
            var guard = new BoundaryEvidenceSequenceGuard(new[] { row });
            guard.Accept(row, "row-start", null, true, false);
            guard.Accept(row, "mounted", null, true, false);

            TestRunner.True(ThrowsInvalidOperation(() => guard.Accept(row, "row-result", "PASS", true, false)),
                "A PASS row omitted required load-boundary phases.");
        }

        private static void IntermediatePhaseSkipIsRejected()
        {
            const string row = "mounted-pair-save-safety";
            var guard = new BoundaryEvidenceSequenceGuard(new[] { row });
            guard.Accept(row, "row-start", null, true, false);

            TestRunner.True(ThrowsInvalidOperation(() => guard.Accept(row, "pre-boundary", null, true, false)),
                "Boundary evidence skipped the mounted phase.");
        }

        private static void TruthfulFailedPrefixIsAccepted()
        {
            const string row = "mounted-pair-load-safety";
            var guard = new BoundaryEvidenceSequenceGuard(new[] { row });
            guard.Accept(row, "row-start", null, true, false);
            guard.Accept(row, "mounted", null, true, false);
            guard.Accept(row, "row-result", "FAIL", true, false);

            TestRunner.True(guard.IsComplete, "A truthful executed FAIL could not close its observed evidence prefix.");
        }

        private static void ReorderAndPostTerminalAreRejected()
        {
            var rows = BoundaryScenarioEvidenceContract.SelectRows("boundary-suite");
            var reordered = new BoundaryEvidenceSequenceGuard(rows);
            TestRunner.True(ThrowsInvalidOperation(() => reordered.Accept(rows[1], "row-start", null, true, false)),
                "An out-of-order boundary row was accepted.");

            var single = new BoundaryEvidenceSequenceGuard(new[] { rows[0] });
            foreach (var phase in BoundaryScenarioEvidenceContract.PassPhases(rows[0]))
            {
                single.Accept(rows[0], phase, phase == "row-result" ? "PASS" : null, true, false);
            }
            TestRunner.True(ThrowsInvalidOperation(() => single.Accept(rows[0], "row-result", "PASS", true, false)),
                "Evidence was accepted after its selected rows were terminal.");
        }

        private static void SuppressedRowsAreFailClosed()
        {
            var rows = BoundaryScenarioEvidenceContract.SelectRows("boundary-suite");
            var guard = new BoundaryEvidenceSequenceGuard(rows);
            guard.Accept(rows[0], "row-start", null, true, false);
            guard.Accept(rows[0], "row-result", "FAIL", true, false);
            for (var index = 1; index < rows.Count; index++)
            {
                guard.Accept(rows[index], "row-result", "FAIL", false, true);
            }
            TestRunner.True(guard.IsComplete, "Fail-closed suppressed rows did not terminate the suite evidence.");

            var invalid = new BoundaryEvidenceSequenceGuard(new[] { rows[0] });
            TestRunner.True(ThrowsInvalidOperation(() => invalid.Accept(rows[0], "row-result", "PASS", false, true)),
                "A suppressed PASS row was accepted.");
        }

        private static void ActiveLoadIdentityUsesExplicitCache()
        {
            TestRunner.Equal(
                BoundaryWorkingIdentityObservation.CachedImmediatePreDispatch,
                BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                    "mounted-pair-load-safety", "cleanup-latch", true, false),
                "Load cleanup-latch would reopen Working after real LoadGame dispatch.");
            TestRunner.Equal(
                BoundaryWorkingIdentityObservation.CachedImmediatePreDispatch,
                BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                    "mounted-pair-load-safety", "loading-start", true, false),
                "Load loading-start would reopen Working during the active load.");
            TestRunner.Equal(
                BoundaryWorkingIdentityObservation.CachedRowStart,
                BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                    "mounted-pair-area-transition-safety", "loading-start", false, true),
                "Area loading-start would reopen Working during active world replacement.");
            TestRunner.Equal(
                BoundaryWorkingIdentityObservation.CaptureCurrent,
                BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                    "mounted-pair-load-safety", "loading-stop", true, false),
                "Load loading-stop did not require a fresh on-disk identity observation.");
            TestRunner.Equal(
                BoundaryWorkingIdentityObservation.CaptureCurrent,
                BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                    "mounted-pair-area-transition-safety", "cleanup-latch", false, false),
                "Area pre-reload cleanup latch was incorrectly labeled as a cached observation.");
        }

        private static void JournalIsCreateNewAppendClose()
        {
            var root = Path.Combine(Path.GetTempPath(), "kmc-boundary-evidence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, BoundaryScenarioEvidenceContract.EvidenceFileName);
            try
            {
                var journal = new BoundaryEvidenceJournal(path);
                journal.AppendSerializedRecord("{\"sequence\":0}");
                using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    TestRunner.True(reader.Length > 0, "The first boundary evidence record was not durably visible after close.");
                }

                journal.AppendSerializedRecord("{\"sequence\":1}");
                var lines = File.ReadAllLines(path);
                TestRunner.Equal(2, lines.Length, "Boundary journal did not append exactly one line per record.");
                TestRunner.Equal("{\"sequence\":0}", lines[0], "Boundary journal changed its CreateNew record.");
                TestRunner.Equal("{\"sequence\":1}", lines[1], "Boundary journal did not append the second record.");

                var duplicate = new BoundaryEvidenceJournal(path);
                TestRunner.True(ThrowsIOException(() => duplicate.AppendSerializedRecord("{\"sequence\":2}")),
                    "A second journal overwrote an existing boundary evidence file.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                if (Directory.Exists(root))
                {
                    Directory.Delete(root);
                }
            }
        }

        private static void JournalFailsClosedAfterDisappearance()
        {
            var root = Path.Combine(Path.GetTempPath(), "kmc-boundary-evidence-gap-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, BoundaryScenarioEvidenceContract.EvidenceFileName);
            try
            {
                var journal = new BoundaryEvidenceJournal(path);
                journal.AppendSerializedRecord("{\"sequence\":0}");
                File.Delete(path);

                TestRunner.True(ThrowsIOException(() => journal.AppendSerializedRecord("{\"sequence\":1}")),
                    "Boundary journal silently recreated evidence that disappeared between records.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                if (Directory.Exists(root))
                {
                    Directory.Delete(root);
                }
            }
        }

        private static void JournalRejectsSameLengthMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "kmc-boundary-evidence-mutation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, BoundaryScenarioEvidenceContract.EvidenceFileName);
            try
            {
                var journal = new BoundaryEvidenceJournal(path);
                journal.AppendSerializedRecord("{\"sequence\":0}");
                var bytes = File.ReadAllBytes(path);
                for (var index = 0; index < bytes.Length; index++)
                {
                    if (bytes[index] == (byte)'0')
                    {
                        bytes[index] = (byte)'9';
                        break;
                    }
                }
                File.WriteAllBytes(path, bytes);

                TestRunner.True(ThrowsIOException(() => journal.AppendSerializedRecord("{\"sequence\":1}")),
                    "Boundary journal appended after its committed prefix changed without changing length.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                if (Directory.Exists(root))
                {
                    Directory.Delete(root);
                }
            }
        }

        private static bool ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static bool ThrowsArgument(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        private static bool ThrowsIOException(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}
