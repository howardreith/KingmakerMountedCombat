using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: retry-safe cleanup of the charge lease's mutations.
    //
    // The first form of this work marked the lease restored before cleaning anything up and cleared each
    // ownership flag whether or not the native undo had succeeded. So a Buff.Remove that threw, or a mount
    // agent that had gone away, produced a lease that reported itself restored while the charging flag, the
    // doubled speed override or the buff were still live. "The undo loop ran" is not the same fact as "the
    // mutation is gone".
    //
    // The contract here is the opposite. Ownership is retained until the native undo has succeeded AND the
    // exact postcondition has been observed; only then is it released. A step that throws, or whose
    // postcondition does not hold, stays owned and stays unresolved - that is cleanup debt, and the ledger
    // can be attempted again later to discharge it. Completion means: every step resolved, nothing still
    // owned, and no failure in the final attempt.
    public sealed class MountedChargeCleanupStep
    {
        public MountedChargeCleanupStep(string name, Func<bool> owned, Func<bool> tryUndo)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A cleanup step needs an exact name.", nameof(name));
            }

            Name = name;
            Owned = owned ?? throw new ArgumentNullException(nameof(owned));
            TryUndo = tryUndo ?? throw new ArgumentNullException(nameof(tryUndo));
        }

        public string Name { get; }

        // Whether this mutation is still owned and therefore still needs undoing.
        public Func<bool> Owned { get; }

        // Attempts the native undo. Must return true only when the undo succeeded and its exact
        // postcondition was observed, and must release its own ownership only in that case.
        public Func<bool> TryUndo { get; }
    }

    public sealed class MountedChargeCleanupLedger
    {
        private readonly List<MountedChargeCleanupStep> steps = new List<MountedChargeCleanupStep>();
        private readonly List<string> resolved = new List<string>();
        private readonly List<string> failures = new List<string>();

        public MountedChargeCleanupLedger(IEnumerable<MountedChargeCleanupStep> orderedSteps)
        {
            if (orderedSteps == null)
            {
                throw new ArgumentNullException(nameof(orderedSteps));
            }

            foreach (var step in orderedSteps)
            {
                if (step == null)
                {
                    throw new ArgumentException("A cleanup step was null.", nameof(orderedSteps));
                }

                steps.Add(step);
            }

            if (steps.Count == 0)
            {
                throw new ArgumentException("Cleanup needs at least one step.", nameof(orderedSteps));
            }
        }

        public int AttemptCount { get; private set; }

        public bool Attempted => AttemptCount > 0;

        // Every step resolved, nothing still owned, and the last attempt recorded no failure.
        public bool Complete => Attempted && failures.Count == 0 && Unresolved.Count == 0;

        // Steps resolved across all attempts, in the order they were resolved.
        public IList<string> Resolved => resolved;

        // Failures from the LAST attempt only: an earlier failure that a later attempt discharged is not
        // outstanding debt, and reporting it as such would make the debt impossible to clear.
        public IList<string> Failures => failures;

        // Steps whose mutation is still owned. This is the cleanup debt.
        public IList<string> Unresolved
        {
            get
            {
                var outstanding = new List<string>();
                foreach (var step in steps)
                {
                    bool owned;
                    try
                    {
                        owned = step.Owned();
                    }
                    catch (Exception)
                    {
                        // An ownership probe that cannot answer is treated as still owned: debt is the safe
                        // assumption, because the alternative is reporting a mutation gone that may be live.
                        owned = true;
                    }

                    if (owned)
                    {
                        outstanding.Add(step.Name);
                    }
                }

                return outstanding;
            }
        }

        // Attempts every still-owned step. Safe to call repeatedly: a step already resolved is skipped, and
        // a step that failed before is tried again.
        public void Attempt()
        {
            AttemptCount++;
            failures.Clear();
            foreach (var step in steps)
            {
                bool owned;
                try
                {
                    owned = step.Owned();
                }
                catch (Exception exception)
                {
                    failures.Add(step.Name + ":owned:" + exception.GetType().Name);
                    continue;
                }

                if (!owned)
                {
                    continue;
                }

                try
                {
                    if (step.TryUndo())
                    {
                        resolved.Add(step.Name);
                    }
                    else
                    {
                        failures.Add(step.Name + ":postcondition");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(step.Name + ":" + exception.GetType().Name);
                }
            }
        }

        public string Describe()
        {
            return "attempts=" + AttemptCount + ";complete=" + Complete +
                ";resolved=" + string.Join("|", resolved.ToArray()) +
                ";unresolved=" + string.Join("|", new List<string>(Unresolved).ToArray()) +
                ";failures=" + string.Join("|", failures.ToArray());
        }
    }
}
