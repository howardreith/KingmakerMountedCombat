using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedCombatActionKind
    {
        None,
        RiderMelee,
        MountPrimaryNatural
    }

    public enum MountedCombatActor
    {
        None,
        Rider,
        Mount
    }

    public enum NativeSingleAttackSlotKind
    {
        None,
        PrimaryHand,
        SecondaryHand,
        AdditionalLimb
    }

    public sealed class NativeSingleAttackSlotDecision
    {
        public NativeSingleAttackSlotDecision(NativeSingleAttackSlotKind kind, int additionalLimbIndex)
        {
            Kind = kind;
            AdditionalLimbIndex = additionalLimbIndex;
        }

        public NativeSingleAttackSlotKind Kind { get; }

        public int AdditionalLimbIndex { get; }

        public bool HasSelection => Kind != NativeSingleAttackSlotKind.None;
    }

    public static class NativeSingleAttackSlotPolicy
    {
        public static NativeSingleAttackSlotDecision Select(
            bool handsEnabled,
            bool primaryHasWeapon,
            int primaryMainAttacks,
            bool secondaryHasWeapon,
            int secondaryMainAttacks,
            IReadOnlyList<bool> additionalLimbHasWeapon)
        {
            if (primaryMainAttacks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryMainAttacks));
            }
            if (secondaryMainAttacks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondaryMainAttacks));
            }

            if (handsEnabled && primaryHasWeapon && primaryMainAttacks > 0)
            {
                return new NativeSingleAttackSlotDecision(NativeSingleAttackSlotKind.PrimaryHand, -1);
            }
            if (handsEnabled && secondaryHasWeapon && secondaryMainAttacks > 0)
            {
                return new NativeSingleAttackSlotDecision(NativeSingleAttackSlotKind.SecondaryHand, -1);
            }
            if (additionalLimbHasWeapon != null)
            {
                for (var index = 0; index < additionalLimbHasWeapon.Count; index++)
                {
                    if (additionalLimbHasWeapon[index])
                    {
                        return new NativeSingleAttackSlotDecision(NativeSingleAttackSlotKind.AdditionalLimb, index);
                    }
                }
            }
            return new NativeSingleAttackSlotDecision(NativeSingleAttackSlotKind.None, -1);
        }
    }

    public sealed class MountedCombatActionContext
    {
        public MountedCombatActionKind Action { get; set; }

        public bool FeatureEnabled { get; set; }

        public bool ExactMountedPair { get; set; }

        public bool SupportedMammothProfile { get; set; }

        public bool InCombat { get; set; }

        public bool RiderAliveAndConscious { get; set; }

        public bool MountAliveAndConscious { get; set; }

        public bool TargetExists { get; set; }

        public bool TargetAliveAndConscious { get; set; }

        public bool TargetIsVisibleEnemy { get; set; }

        public bool RiderOwnsCurrentTurnOrRealTime { get; set; }

        public bool RiderHasStandardAction { get; set; }

        public bool RiderWeaponIsSupportedMelee { get; set; }

        public bool MountPrimaryNaturalAttackIsExact { get; set; }

        public bool TransactionIdle { get; set; }

        public bool LoadingOrLifecycleBoundary { get; set; }
    }

    public sealed class MountedCombatActionAvailability
    {
        public MountedCombatActionAvailability(
            bool allowed,
            MountedCombatActor actor,
            IReadOnlyList<string> rejectionReasons)
        {
            IsAllowed = allowed;
            Actor = actor;
            RejectionReasons = rejectionReasons ?? throw new ArgumentNullException(nameof(rejectionReasons));
        }

        public bool IsAllowed { get; }

        public MountedCombatActor Actor { get; }

        public MountedCombatActor ResourceOwner => MountedCombatActor.Rider;

        public MountedCombatActor PathfindingOwner => MountedCombatActor.Mount;

        public bool IsSingleAttack => true;

        public IReadOnlyList<string> RejectionReasons { get; }

        public string Feedback => RejectionReasons.Count == 0
            ? "Select one visible enemy."
            : string.Join(" ", RejectionReasons);
    }

    public sealed class MountedPairLivenessSnapshot
    {
        private readonly string[] failedGateNames;

        public MountedPairLivenessSnapshot(
            bool relationshipMounted,
            bool relationshipRiderExact,
            bool relationshipMountExact,
            bool wrapperExecutorIsRider,
            bool targetInState,
            bool riderInState,
            bool mountInState,
            bool riderConscious,
            bool mountConscious,
            bool targetConsciousOrChildStarted,
            bool riderNotFinallyDead,
            bool mountNotFinallyDead,
            bool targetNotFinallyDead,
            bool riderHostileToTarget,
            bool riderCanAttackTarget)
        {
            var failures = new List<string>();
            AddFailure(failures, relationshipMounted, "relationship-mounted");
            AddFailure(failures, relationshipRiderExact, "relationship-rider-exact");
            AddFailure(failures, relationshipMountExact, "relationship-mount-exact");
            AddFailure(failures, wrapperExecutorIsRider, "wrapper-executor-is-rider");
            AddFailure(failures, targetInState, "target-in-state");
            AddFailure(failures, riderInState, "rider-in-state");
            AddFailure(failures, mountInState, "mount-in-state");
            AddFailure(failures, riderConscious, "rider-conscious");
            AddFailure(failures, mountConscious, "mount-conscious");
            AddFailure(failures, targetConsciousOrChildStarted, "target-conscious-or-child-started");
            AddFailure(failures, riderNotFinallyDead, "rider-not-finally-dead");
            AddFailure(failures, mountNotFinallyDead, "mount-not-finally-dead");
            AddFailure(failures, targetNotFinallyDead, "target-not-finally-dead");
            AddFailure(failures, riderHostileToTarget, "rider-hostile-to-target");
            AddFailure(failures, riderCanAttackTarget, "rider-can-attack-target");
            failedGateNames = failures.ToArray();
        }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        public static bool IsTargetConsciousnessAdmissible(
            bool targetConscious,
            int exactChildAttackStartCount)
        {
            return exactChildAttackStartCount >= 0 && exactChildAttackStartCount <= 1 &&
                (targetConscious || exactChildAttackStartCount == 1);
        }

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public static class MountedCombatActionEvaluator
    {
        public static MountedCombatActionAvailability Evaluate(MountedCombatActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var reasons = new List<string>();
            if (context.Action == MountedCombatActionKind.None)
            {
                reasons.Add("Choose Rider melee or Mammoth primary.");
            }
            if (!context.FeatureEnabled)
            {
                reasons.Add("The private-alpha mounted combat feature is disabled.");
            }
            if (!context.ExactMountedPair || !context.SupportedMammothProfile)
            {
                reasons.Add("Combat requires the exact active Medium-humanoid/Mammoth profile.");
            }
            if (!context.InCombat)
            {
                reasons.Add("Mounted attacks are available only in combat.");
            }
            if (!context.RiderAliveAndConscious || !context.MountAliveAndConscious)
            {
                reasons.Add("Rider and Mammoth must both be alive and conscious.");
            }
            if (!context.TargetExists || !context.TargetAliveAndConscious || !context.TargetIsVisibleEnemy)
            {
                reasons.Add("Choose one living, visible enemy.");
            }
            if (!context.RiderOwnsCurrentTurnOrRealTime)
            {
                reasons.Add("The rider must own the current turn.");
            }
            if (!context.RiderHasStandardAction)
            {
                reasons.Add("The rider has no Standard action available.");
            }
            if (!context.TransactionIdle)
            {
                reasons.Add("A mounted pair command is already active.");
            }
            if (context.LoadingOrLifecycleBoundary)
            {
                reasons.Add("Mounted attacks are blocked during lifecycle transitions.");
            }
            if (context.Action == MountedCombatActionKind.RiderMelee &&
                !context.RiderWeaponIsSupportedMelee)
            {
                reasons.Add("Rider mode requires one supported melee weapon; ranged attacks and spells are not authorized.");
            }
            if (context.Action == MountedCombatActionKind.MountPrimaryNatural &&
                !context.MountPrimaryNaturalAttackIsExact)
            {
                reasons.Add("The Mammoth primary natural attack could not be identified exactly.");
            }

            var actor = context.Action == MountedCombatActionKind.RiderMelee
                ? MountedCombatActor.Rider
                : context.Action == MountedCombatActionKind.MountPrimaryNatural
                    ? MountedCombatActor.Mount
                    : MountedCombatActor.None;
            return new MountedCombatActionAvailability(reasons.Count == 0, actor, reasons);
        }
    }
}
