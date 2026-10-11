using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: the ordered, reversible application of the charge lease's mutations.
    //
    // Why this exists as a pure type. The lease applies five mutations to live native state - the rider's
    // charge buff, the mount agent's charging flag, its doubled speed override, the rider's charging state
    // and the forced straight path. Before this type, those happened in sequence with no record of which had
    // completed, and the restore path was gated on a single Applied flag that only became true after all of
    // them: a failure at any boundary left the mount charging at double speed on a forced path with nothing
    // to undo it. Sequencing and rollback are now decided here, where they can be fault-injected at every
    // boundary without a live game, and the native calls stay in the lease.
    //
    // The contract: steps are applied in order; the first failure undoes every completed step in reverse
    // order and the original exception is rethrown unchanged. An undo that itself fails is recorded and does
    // not stop the remaining undos, because a second fault must not strand the first mutation. Nothing here
    // writes, clears, refunds or synthesises anything - an undo only returns the exact field its own step
    // changed to the value captured before the attempt.
    public sealed class MountedChargeApplicationStep
    {
        public MountedChargeApplicationStep(string name, Action apply, Action undo)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A charge application step needs an exact name.", nameof(name));
            }

            Name = name;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Undo = undo ?? throw new ArgumentNullException(nameof(undo));
        }

        public string Name { get; }

        public Action Apply { get; }

        public Action Undo { get; }
    }

    public sealed class MountedChargeApplicationTransaction
    {
        private readonly List<MountedChargeApplicationStep> steps = new List<MountedChargeApplicationStep>();
        private readonly List<string> attempted = new List<string>();
        private readonly List<string> completed = new List<string>();
        private readonly List<string> undone = new List<string>();
        private readonly List<string> undoFailures = new List<string>();

        public MountedChargeApplicationTransaction(IEnumerable<MountedChargeApplicationStep> orderedSteps)
        {
            if (orderedSteps == null)
            {
                throw new ArgumentNullException(nameof(orderedSteps));
            }

            foreach (var step in orderedSteps)
            {
                if (step == null)
                {
                    throw new ArgumentException("A charge application step was null.", nameof(orderedSteps));
                }

                steps.Add(step);
            }

            if (steps.Count == 0)
            {
                throw new ArgumentException("A charge application needs at least one step.", nameof(orderedSteps));
            }
        }

        // True only when every step completed.
        public bool Applied { get; private set; }

        // True when a step failed and the completed prefix was undone.
        public bool RolledBack { get; private set; }

        public string FailureReason { get; private set; }

        public string FailedStep { get; private set; }

        // The steps that completed, in application order: their Apply returned without throwing.
        public IList<string> CompletedSteps => completed;

        // The steps that were entered, in application order. This is what the rollback walks, because a
        // step can take its mutation and then throw, and that mutation must still be returned.
        public IList<string> AttemptedSteps => attempted;

        // The steps undone, in the order they were undone, which is the reverse of completion.
        public IList<string> UndoneSteps => undone;

        // An undo that threw. Recorded, never swallowed silently, and never allowed to stop the others.
        public IList<string> UndoFailures => undoFailures;

        public int StepCount => steps.Count;

        public void Apply()
        {
            if (Applied || RolledBack)
            {
                throw new InvalidOperationException("A charge application transaction runs exactly once.");
            }

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                attempted.Add(step.Name);
                try
                {
                    step.Apply();
                }
                catch (Exception exception)
                {
                    FailedStep = step.Name;
                    FailureReason = exception.Message;
                    Rollback();
                    throw;
                }

                completed.Add(step.Name);
            }

            Applied = true;
        }

        private void Rollback()
        {
            // Reverse order over every step that was entered: the last mutation to be taken is the first
            // to be returned, and the step that failed is included because it may already have taken its
            // mutation before throwing.
            for (var index = attempted.Count - 1; index >= 0; index--)
            {
                var name = attempted[index];
                var step = steps.Find(candidate => candidate.Name == name);
                if (step == null)
                {
                    undoFailures.Add(name + ":step-missing");
                    continue;
                }

                try
                {
                    step.Undo();
                    undone.Add(name);
                }
                catch (Exception exception)
                {
                    undoFailures.Add(name + ":" + exception.GetType().Name);
                }
            }

            RolledBack = true;
        }

        public string Describe()
        {
            return "applied=" + Applied + ";rolledBack=" + RolledBack +
                ";failedStep=" + (FailedStep ?? "<none>") +
                ";attempted=" + string.Join("|", attempted.ToArray()) +
                ";completed=" + string.Join("|", completed.ToArray()) +
                ";undone=" + string.Join("|", undone.ToArray()) +
                ";undoFailures=" + string.Join("|", undoFailures.ToArray());
        }
    }
}
