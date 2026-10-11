using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: the legal order of a pair-owned charge transaction.
    //
    // The order is not decoration. The transition revalidation asks whether the carrier still owns the
    // mount's Move slot, so it has to run WHILE the carrier owns it; releasing the carrier first makes a
    // lawful charge reject itself with "lost the carrier that owns the mount movement slot". The attack-start
    // revalidation asks the opposite - that the carrier has released the slot - so it has to run after the
    // release is proven. And the lease may not be applied until exact carrier ownership is established,
    // because its forced path lives only as long as that carrier.
    //
    // This type is the command's own order-keeper, not a parallel model of it: the command observes each step
    // here as it takes it, and a step taken out of order is recorded as a violation that fails the charge.
    // That is what makes an ordering test meaningful.
    public enum MountedChargeTransactionStep
    {
        // The native UnitMoveTo has been created and run on the mount.
        CarrierAdmitted = 0,

        // The carrier is proven to be the exact mount's, in the exact Move slot, unqueued, with an empty
        // mount queue and the rider wrapper still the exact parent.
        CarrierOwnershipProven = 1,

        // The first in-transaction revalidation, before any lease mutation exists.
        InitialRevalidation = 2,

        // The charge lease's mutations are applied.
        LeaseApplied = 3,

        // A revalidation before a repath or a re-force.
        RepathRevalidation = 4,

        // The revalidation at the approach-to-attack boundary, which requires the carrier still owned.
        TransitionRevalidation = 5,

        // The carrier is stopped and removed for the attack.
        CarrierReleasedForAttack = 6,

        // The carrier and the mount Move slot are proven released.
        CarrierReleaseProven = 7,

        // The transaction has entered attack range.
        Arrived = 8,

        // The final revalidation, which requires the carrier released.
        AttackStartRevalidation = 9,

        // The native rider attack has started.
        AttackStarted = 10
    }

    public sealed class MountedChargeTransactionSequence
    {
        private readonly List<MountedChargeTransactionStep> observed = new List<MountedChargeTransactionStep>();
        private readonly List<string> violations = new List<string>();
        private bool carrierOwned;
        private bool carrierReleasedForAttack;
        private bool leaseApplied;
        private bool transitionRevalidated;
        private bool releaseProven;
        private bool arrived;
        private bool attackStartRevalidated;

        public IList<MountedChargeTransactionStep> Observed => observed;

        public IList<string> Violations => violations;

        public bool Lawful => violations.Count == 0;

        public bool CarrierOwned => carrierOwned;

        public MountedChargeTransactionStep? Last =>
            observed.Count == 0 ? (MountedChargeTransactionStep?)null : observed[observed.Count - 1];

        // Returns true when the step was legal at this point. An illegal step is still recorded, so the
        // evidence shows exactly what the transaction did, but it is reported as a violation.
        public bool Observe(MountedChargeTransactionStep step)
        {
            var violation = Check(step);
            observed.Add(step);
            if (violation != null)
            {
                violations.Add(step + ":" + violation);
                return false;
            }

            Apply(step);
            return true;
        }

        private string Check(MountedChargeTransactionStep step)
        {
            switch (step)
            {
                case MountedChargeTransactionStep.CarrierAdmitted:
                    if (carrierOwned)
                    {
                        return "a carrier was admitted while one was still owned";
                    }

                    if (carrierReleasedForAttack)
                    {
                        return "a carrier was admitted after the attack release";
                    }

                    return null;

                case MountedChargeTransactionStep.CarrierOwnershipProven:
                    return Last == MountedChargeTransactionStep.CarrierAdmitted
                        ? null
                        : "carrier ownership was proven without an immediately preceding admission";

                case MountedChargeTransactionStep.InitialRevalidation:
                    if (!carrierOwned)
                    {
                        return "the initial revalidation ran without a proven carrier";
                    }

                    return leaseApplied ? "the initial revalidation ran after the lease was applied" : null;

                case MountedChargeTransactionStep.LeaseApplied:
                    if (leaseApplied)
                    {
                        return "the lease was applied twice";
                    }

                    if (!carrierOwned)
                    {
                        return "the lease was applied without a proven carrier";
                    }

                    return Last == MountedChargeTransactionStep.InitialRevalidation
                        ? null
                        : "the lease was applied without an immediately preceding revalidation";

                case MountedChargeTransactionStep.RepathRevalidation:
                    return carrierReleasedForAttack
                        ? "a repath revalidation ran after the attack release"
                        : null;

                case MountedChargeTransactionStep.TransitionRevalidation:
                    // The rule this whole type exists for.
                    if (!carrierOwned)
                    {
                        return "the transition revalidation ran after the carrier was released";
                    }

                    return null;

                case MountedChargeTransactionStep.CarrierReleasedForAttack:
                    if (!carrierOwned)
                    {
                        return "the carrier was released for the attack without being owned";
                    }

                    if (!transitionRevalidated)
                    {
                        return "the carrier was released before the transition revalidation";
                    }

                    return null;

                case MountedChargeTransactionStep.CarrierReleaseProven:
                    return Last == MountedChargeTransactionStep.CarrierReleasedForAttack
                        ? null
                        : "the carrier release was proven without an immediately preceding release";

                case MountedChargeTransactionStep.Arrived:
                    return releaseProven ? null : "arrival was recorded before the carrier release was proven";

                case MountedChargeTransactionStep.AttackStartRevalidation:
                    if (carrierOwned)
                    {
                        return "the attack-start revalidation ran while the carrier was still owned";
                    }

                    return arrived ? null : "the attack-start revalidation ran before arrival";

                case MountedChargeTransactionStep.AttackStarted:
                    if (!attackStartRevalidated)
                    {
                        return "the native attack started without a final revalidation";
                    }

                    return Last == MountedChargeTransactionStep.AttackStartRevalidation
                        ? null
                        : "the native attack started without an immediately preceding final revalidation";

                default:
                    return "unknown charge transaction step";
            }
        }

        private void Apply(MountedChargeTransactionStep step)
        {
            switch (step)
            {
                case MountedChargeTransactionStep.CarrierOwnershipProven:
                    carrierOwned = true;
                    break;
                case MountedChargeTransactionStep.InitialRevalidation:
                    break;
                case MountedChargeTransactionStep.LeaseApplied:
                    leaseApplied = true;
                    break;
                case MountedChargeTransactionStep.TransitionRevalidation:
                    transitionRevalidated = true;
                    break;
                case MountedChargeTransactionStep.CarrierReleasedForAttack:
                    carrierOwned = false;
                    carrierReleasedForAttack = true;
                    break;
                case MountedChargeTransactionStep.CarrierReleaseProven:
                    releaseProven = true;
                    break;
                case MountedChargeTransactionStep.Arrived:
                    arrived = true;
                    break;
                case MountedChargeTransactionStep.AttackStartRevalidation:
                    attackStartRevalidated = true;
                    break;
            }
        }

        // A carrier released for a repath rather than for the attack: ownership ends without consuming the
        // attack release, so the approach cycle can legally begin again.
        public void ObserveCarrierReleasedForRepath()
        {
            carrierOwned = false;
        }

        public string Describe()
        {
            return "lawful=" + Lawful + ";carrierOwned=" + carrierOwned +
                ";steps=" + string.Join("|", Array.ConvertAll(observed.ToArray(), step => step.ToString())) +
                ";violations=" + string.Join("|", violations.ToArray());
        }
    }
}
