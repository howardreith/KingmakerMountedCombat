using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: compensation for a charge that failed after it entered the rider's queue.
    //
    // Once the charge command is queued on the rider, a later refusal or exception cannot simply clear the
    // controller's activeCommand field and return: the command is live native state. Every owner has to be
    // resolved - the scheduler registration abandoned, the exact command interrupted and removed from its
    // exact queue or slot, and any applied or partially applied charge lease restored - before the field is
    // cleared, or a refused charge executes anyway.
    //
    // This is deliberately NOT a rollback transaction like MountedChargeApplicationTransaction. There the
    // first failure stops the walk and undoes the prefix. Here every step must run even when an earlier one
    // throws, because each resolves a different native owner and skipping one strands it. Failures are
    // recorded against their step and the sequence never throws: compensation is the last line of defence,
    // so it cannot be the thing that fails.
    public sealed class MountedChargeCompensationStep
    {
        public MountedChargeCompensationStep(string name, Action run)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A charge compensation step needs an exact name.", nameof(name));
            }

            Name = name;
            Run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public string Name { get; }

        public Action Run { get; }
    }

    // A native fact that must be observed true for compensation to be complete. Distinct from a step: a
    // step does something, a postcondition proves the owner is actually gone.
    public sealed class MountedChargePostcondition
    {
        public MountedChargePostcondition(string name, Func<bool> satisfied)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A compensation postcondition needs an exact name.", nameof(name));
            }

            Name = name;
            Satisfied = satisfied ?? throw new ArgumentNullException(nameof(satisfied));
        }

        public string Name { get; }

        public Func<bool> Satisfied { get; }
    }

    public sealed class MountedChargeCompensation
    {
        private readonly List<MountedChargeCompensationStep> steps = new List<MountedChargeCompensationStep>();
        private readonly List<string> completed = new List<string>();
        private readonly List<string> failures = new List<string>();
        private readonly List<string> unmet = new List<string>();
        private bool postconditionsConfirmed;

        public MountedChargeCompensation(string reason, IEnumerable<MountedChargeCompensationStep> orderedSteps)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException("Compensation needs the exact reason it was triggered.", nameof(reason));
            }

            if (orderedSteps == null)
            {
                throw new ArgumentNullException(nameof(orderedSteps));
            }

            Reason = reason;
            foreach (var step in orderedSteps)
            {
                if (step == null)
                {
                    throw new ArgumentException("A charge compensation step was null.", nameof(orderedSteps));
                }

                steps.Add(step);
            }

            if (steps.Count == 0)
            {
                throw new ArgumentException("Compensation needs at least one step.", nameof(orderedSteps));
            }
        }

        public string Reason { get; }

        public bool Ran { get; private set; }

        // Complete means every step ran cleanly AND every declared native postcondition was observed
        // true. Until the postconditions have been confirmed, compensation is not complete whatever the
        // steps did: the controller must not release its last reference to live native state on the
        // strength of "nothing threw".
        public bool Complete => Ran && postconditionsConfirmed && failures.Count == 0 && unmet.Count == 0;

        public bool PostconditionsConfirmed => postconditionsConfirmed;

        // Declared postconditions that were not observed true. This is what keeps a faulted cleanup owner.
        public IList<string> UnmetPostconditions => unmet;

        public IList<string> CompletedSteps => completed;

        public IList<string> Failures => failures;

        public int StepCount => steps.Count;

        // Runs every step in order. Never throws: a step that fails is recorded and the rest still run.
        public void Run()
        {
            if (Ran)
            {
                throw new InvalidOperationException("Charge compensation runs exactly once.");
            }

            Ran = true;
            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                try
                {
                    step.Run();
                    completed.Add(step.Name);
                }
                catch (Exception exception)
                {
                    failures.Add(step.Name + ":" + exception.GetType().Name);
                }
            }
        }

        // Observes the native facts that prove every owner is gone. Safe to call again after a retry.
        public void ConfirmPostconditions(IEnumerable<MountedChargePostcondition> postconditions)
        {
            if (postconditions == null)
            {
                throw new ArgumentNullException(nameof(postconditions));
            }

            if (!Ran)
            {
                throw new InvalidOperationException("Compensation postconditions were confirmed before it ran.");
            }

            unmet.Clear();
            var any = false;
            foreach (var postcondition in postconditions)
            {
                if (postcondition == null)
                {
                    throw new ArgumentException("A compensation postcondition was null.", nameof(postconditions));
                }

                any = true;
                bool satisfied;
                try
                {
                    satisfied = postcondition.Satisfied();
                }
                catch (Exception exception)
                {
                    // A postcondition that cannot be evaluated is unmet. The alternative would be to
                    // treat an unanswerable question as a clean answer.
                    unmet.Add(postcondition.Name + ":" + exception.GetType().Name);
                    continue;
                }

                if (!satisfied)
                {
                    unmet.Add(postcondition.Name);
                }
            }

            if (!any)
            {
                throw new ArgumentException("Compensation needs at least one postcondition.", nameof(postconditions));
            }

            postconditionsConfirmed = true;
        }

        public string Describe()
        {
            return "reason=" + Reason + ";ran=" + Ran + ";complete=" + Complete +
                ";postconditionsConfirmed=" + postconditionsConfirmed +
                ";completed=" + string.Join("|", completed.ToArray()) +
                ";failures=" + string.Join("|", failures.ToArray()) +
                ";unmet=" + string.Join("|", unmet.ToArray());
        }
    }
}
