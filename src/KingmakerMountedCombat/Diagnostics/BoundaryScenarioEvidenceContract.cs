using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum BoundaryWorkingIdentityObservation
    {
        CaptureCurrent,
        CachedImmediatePreDispatch,
        CachedRowStart
    }

    internal static class BoundaryScenarioEvidenceContract
    {
        public const int SchemaVersion = 1;
        public const string ArtifactKind = "boundary-scenario-evidence";
        public const string EvidenceFileName = "boundary-scenario-evidence.jsonl";
        public const string CachedImmediatePreDispatchSource = "cached-immediate-pre-dispatch";
        public const string CachedRowStartSource = "cached-row-start";

        private static readonly string[] Rows =
        {
            "mounted-pair-turn-based-entry-cleanup",
            "mounted-pair-realtime-entry-cleanup",
            "mounted-pair-save-safety",
            "mounted-pair-load-safety",
            "mounted-pair-area-transition-safety"
        };

        private static readonly string[] SimplePassPhases =
        {
            "row-start",
            "mounted",
            "pre-boundary",
            "cleanup-latch",
            "post-boundary",
            "row-result"
        };

        private static readonly string[] LoadingPassPhases =
        {
            "row-start",
            "mounted",
            "pre-boundary",
            "cleanup-latch",
            "loading-start",
            "loading-stop",
            "fresh-world",
            "row-result"
        };

        public static IReadOnlyList<string> SelectRows(string scenario)
        {
            if (string.Equals(scenario, "boundary-suite", StringComparison.Ordinal))
            {
                return (string[])Rows.Clone();
            }

            foreach (var row in Rows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }

            return null;
        }

        public static IReadOnlyList<string> PassPhases(string row)
        {
            if (!IsRow(row))
            {
                throw new ArgumentException("Unknown boundary row.", nameof(row));
            }

            return IsLoadingRow(row)
                ? (IReadOnlyList<string>)LoadingPassPhases
                : SimplePassPhases;
        }

        public static bool IsLoadingRow(string row)
        {
            return string.Equals(row, "mounted-pair-load-safety", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-area-transition-safety", StringComparison.Ordinal);
        }

        public static bool IsRow(string row)
        {
            foreach (var candidate in Rows)
            {
                if (string.Equals(candidate, row, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsExactSelection(IReadOnlyList<string> selectedRows)
        {
            if (selectedRows == null)
            {
                return false;
            }
            if (selectedRows.Count == 1)
            {
                return IsRow(selectedRows[0]);
            }
            if (selectedRows.Count != Rows.Length)
            {
                return false;
            }

            for (var index = 0; index < Rows.Length; index++)
            {
                if (!string.Equals(selectedRows[index], Rows[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public static BoundaryWorkingIdentityObservation SelectWorkingIdentityObservation(
            string row,
            string phase,
            bool realWorkingLoadDispatched,
            bool realAreaReloadDispatched)
        {
            if (string.Equals(row, "mounted-pair-load-safety", StringComparison.Ordinal) &&
                realWorkingLoadDispatched &&
                (string.Equals(phase, "cleanup-latch", StringComparison.Ordinal) ||
                 string.Equals(phase, "loading-start", StringComparison.Ordinal)))
            {
                return BoundaryWorkingIdentityObservation.CachedImmediatePreDispatch;
            }

            if (string.Equals(row, "mounted-pair-area-transition-safety", StringComparison.Ordinal) &&
                realAreaReloadDispatched && string.Equals(phase, "loading-start", StringComparison.Ordinal))
            {
                return BoundaryWorkingIdentityObservation.CachedRowStart;
            }

            return BoundaryWorkingIdentityObservation.CaptureCurrent;
        }
    }

    /// <summary>
    /// Rejects reordered, duplicated, post-terminal, or falsely complete
    /// boundary evidence before it reaches the append-only journal.
    /// </summary>
    internal sealed class BoundaryEvidenceSequenceGuard
    {
        private readonly IReadOnlyList<string> selectedRows;
        private int rowIndex;
        private int lastPhaseIndex = -1;
        private bool completed;

        public BoundaryEvidenceSequenceGuard(IReadOnlyList<string> selectedRows)
        {
            if (!BoundaryScenarioEvidenceContract.IsExactSelection(selectedRows))
            {
                throw new ArgumentException(
                    "Boundary evidence requires one exact row or the exact five-row suite order.",
                    nameof(selectedRows));
            }

            this.selectedRows = selectedRows;
        }

        public bool IsComplete => completed;

        public int CurrentRowIndex => rowIndex;

        public void Accept(string row, string phase, string rowStatus, bool executed, bool suppressed)
        {
            if (completed)
            {
                throw new InvalidOperationException("Boundary evidence cannot be appended after every selected row is terminal.");
            }
            if (rowIndex >= selectedRows.Count || !string.Equals(row, selectedRows[rowIndex], StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Boundary evidence row order differed from the selected scenario contract.");
            }

            var phases = BoundaryScenarioEvidenceContract.PassPhases(row);
            if (suppressed)
            {
                if (executed || !string.Equals(phase, "row-result", StringComparison.Ordinal) ||
                    !string.Equals(rowStatus, "FAIL", StringComparison.Ordinal) || lastPhaseIndex != -1)
                {
                    throw new InvalidOperationException("A suppressed boundary row must contain only one unexecuted FAIL row-result.");
                }
                CompleteRow();
                return;
            }
            if (!executed)
            {
                throw new InvalidOperationException("A non-suppressed boundary record must be marked executed.");
            }

            var phaseIndex = IndexOf(phases, phase);
            var terminalFailure = string.Equals(phase, "row-result", StringComparison.Ordinal) &&
                string.Equals(rowStatus, "FAIL", StringComparison.Ordinal);
            if (phaseIndex < 0 || (!terminalFailure && phaseIndex != lastPhaseIndex + 1) ||
                (terminalFailure && phaseIndex <= lastPhaseIndex))
            {
                throw new InvalidOperationException("Boundary evidence phases were unknown, skipped, duplicated, or out of order.");
            }

            if (string.Equals(phase, "row-result", StringComparison.Ordinal))
            {
                if (!string.Equals(rowStatus, "PASS", StringComparison.Ordinal) &&
                    !string.Equals(rowStatus, "FAIL", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A boundary row-result must have PASS or FAIL status.");
                }
                if (string.Equals(rowStatus, "PASS", StringComparison.Ordinal) && phaseIndex != phases.Count - 1)
                {
                    throw new InvalidOperationException("A PASS boundary row-result did not use the terminal phase.");
                }
                if (string.Equals(rowStatus, "PASS", StringComparison.Ordinal) && lastPhaseIndex != phases.Count - 2)
                {
                    throw new InvalidOperationException("A PASS boundary row omitted one or more required evidence phases.");
                }

                lastPhaseIndex = phaseIndex;
                CompleteRow();
                return;
            }

            if (rowStatus != null)
            {
                throw new InvalidOperationException("Only row-result evidence may carry a terminal row status.");
            }

            lastPhaseIndex = phaseIndex;
        }

        private static int IndexOf(IReadOnlyList<string> values, string value)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private void CompleteRow()
        {
            rowIndex++;
            lastPhaseIndex = -1;
            completed = rowIndex == selectedRows.Count;
        }
    }

    /// <summary>
    /// A write-once JSONL journal. Each record receives its own CreateNew or
    /// append/flush/close transaction so no live StreamWriter survives a game
    /// boundary that can dispose the current world.
    /// </summary>
    internal sealed class BoundaryEvidenceJournal
    {
        private readonly string path;
        private bool created;
        private long committedLength;
        private string committedSha256;

        public BoundaryEvidenceJournal(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Boundary evidence path is required.", nameof(path));
            }

            this.path = path;
        }

        public bool IsCreated => created;

        public void AppendSerializedRecord(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.IndexOf('\r') >= 0 || json.IndexOf('\n') >= 0)
            {
                throw new ArgumentException("Boundary evidence must be one non-empty JSON line.", nameof(json));
            }

            var mode = created ? FileMode.Append : FileMode.CreateNew;
            if (created)
            {
                var existing = new FileInfo(path);
                if (!existing.Exists || (existing.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    existing.Length != committedLength ||
                    !string.Equals(ComputeSha256(path), committedSha256, StringComparison.Ordinal))
                {
                    throw new IOException("Boundary evidence disappeared, became a reparse point, or changed between records.");
                }
            }
            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
                {
                    writer.WriteLine(json);
                    writer.Flush();
                }
                stream.Flush(true);
                committedLength = stream.Length;
            }

            committedSha256 = ComputeSha256(path);
            created = true;
        }

        private static string ComputeSha256(string filePath)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
