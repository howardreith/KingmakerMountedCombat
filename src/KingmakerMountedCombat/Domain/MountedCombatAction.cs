using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedCombatActionKind
    {
        None,
        RiderMelee,
        RiderRanged,
        MountPrimaryNatural
    }

    public enum MountedCombatActor
    {
        None,
        Rider,
        Mount
    }

    public enum MountedCombatRejectionCode
    {
        FeatureDisabled,
        WrongActorOrSelection,
        RelationshipInvalidated,
        BodyProfileUnsupported,
        NotInCombat,
        PairLifeStateInvalid,
        TargetInvalid,
        TargetNotVisible,
        TargetNotHostile,
        TargetNotAttackable,
        WrongTurn,
        WrongActionState,
        AlreadyActiveCommand,
        LifecycleBoundary,
        NoEligibleWeapon,
        MountedRangedUnsupported,
        UnsupportedWeaponCategory,
        NoPath,
        OutsideSupportedRange,
        RangeOriginMismatch,
        CommandAdmissionFailure
    }

    public enum NativeSingleAttackSlotKind
    {
        None,
        PrimaryHand,
        SecondaryHand,
        AdditionalLimb
    }

    public static class MountedOpportunityIsolationPolicy
    {
        public static bool ShouldSuppressStockOpportunityAttack(
            bool relationshipMounted,
            bool mountedPairCommandActive,
            bool attackerIsExactRider,
            bool attackerIsExactMount,
            bool targetExists,
            bool experimentalIsolationEnabled)
        {
            return experimentalIsolationEnabled && relationshipMounted && mountedPairCommandActive && targetExists &&
                (attackerIsExactRider || attackerIsExactMount);
        }
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

    public static class NativePrimaryNaturalAttackPolicy
    {
        public static bool IsExact(
            NativeSingleAttackSlotKind kind,
            int additionalLimbIndex,
            bool weaponIsNatural,
            bool weaponIsRanged)
        {
            if (!weaponIsNatural || weaponIsRanged)
            {
                return false;
            }

            return (kind == NativeSingleAttackSlotKind.PrimaryHand && additionalLimbIndex == -1) ||
                (kind == NativeSingleAttackSlotKind.AdditionalLimb && additionalLimbIndex == 0);
        }
    }

    public static class NativeSingleAttackTerminalPolicy
    {
        public static bool ShouldAwaitNativeAnimation(
            bool isTurnBased,
            bool isActed,
            bool resultIsSuccess,
            bool attackRuleObserved,
            int attackCount,
            int completedAttackCount,
            bool hasPlannedAttack)
        {
            return isTurnBased &&
                isActed &&
                resultIsSuccess &&
                attackRuleObserved &&
                attackCount == 1 &&
                completedAttackCount == attackCount &&
                !hasPlannedAttack;
        }
    }

    public sealed class MountedCombatActionContext
    {
        public MountedCombatActionKind Action { get; set; }

        public bool FeatureEnabled { get; set; }

        public bool ExactMountedPair { get; set; }

        public bool ExactRiderSelection { get; set; }

        public bool SupportedMountProfile { get; set; }

        public string MountDisplayName { get; set; }

        public bool SupportedRiderBodyProfile { get; set; }

        public bool InCombat { get; set; }

        // Exact ordinary hostile input may initiate combat through native command/rule flow.
        // This never writes combat flags or admits an explicit/diagnostic action out of combat.
        public bool NativeHostileInitiation { get; set; }

        public bool RiderAliveAndConscious { get; set; }

        public bool MountAliveAndConscious { get; set; }

        public bool TargetExists { get; set; }

        public bool TargetAliveAndConscious { get; set; }

        public bool TargetIsVisibleEnemy { get; set; }

        public bool TargetVisible { get; set; }

        public bool TargetHostile { get; set; }

        public bool TargetAttackable { get; set; }

        public bool ActionActorOwnsCurrentTurnOrRealTime { get; set; }

        public bool ActionActorHasStandardAction { get; set; }

        public bool RiderWeaponIsSupportedMelee { get; set; }

        public bool RiderHasEligibleWeapon { get; set; }

        public bool RiderWeaponIsRanged { get; set; }

        public bool RiderWeaponCategorySupported { get; set; }

        public bool MountPrimaryNaturalAttackIsExact { get; set; }

        public bool TransactionIdle { get; set; }

        public bool LoadingOrLifecycleBoundary { get; set; }

        public bool PathKnownUnavailable { get; set; }

        public bool WithinSupportedRangeEnvelope { get; set; }

        public bool RangeOriginConsistent { get; set; }

        public bool CommandAdmissionReady { get; set; }
    }

    public sealed class MountedCombatActionAvailability
    {
        public MountedCombatActionAvailability(
            bool allowed,
            MountedCombatActor actor,
            IReadOnlyList<string> rejectionReasons,
            IReadOnlyList<MountedCombatRejectionCode> rejectionCodes)
        {
            IsAllowed = allowed;
            Actor = actor;
            RejectionReasons = rejectionReasons ?? throw new ArgumentNullException(nameof(rejectionReasons));
            RejectionCodes = rejectionCodes ?? throw new ArgumentNullException(nameof(rejectionCodes));
        }

        public bool IsAllowed { get; }

        public MountedCombatActor Actor { get; }

        public MountedCombatActor ResourceOwner => Actor;

        public MountedCombatActor PathfindingOwner => MountedCombatActor.Mount;

        public bool IsSingleAttack => true;

        public IReadOnlyList<string> RejectionReasons { get; }

        public IReadOnlyList<MountedCombatRejectionCode> RejectionCodes { get; }

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
            bool wrapperExecutorIsActionActor,
            bool targetInState,
            bool riderInState,
            bool mountInState,
            bool riderConscious,
            bool mountConscious,
            bool targetConsciousOrChildStarted,
            bool riderNotFinallyDead,
            bool mountNotFinallyDead,
            bool targetNotFinallyDead,
            bool actionActorHostileToTarget,
            bool actionActorCanAttackTarget)
        {
            var failures = new List<string>();
            AddFailure(failures, relationshipMounted, "relationship-mounted");
            AddFailure(failures, relationshipRiderExact, "relationship-rider-exact");
            AddFailure(failures, relationshipMountExact, "relationship-mount-exact");
            AddFailure(failures, wrapperExecutorIsActionActor, "wrapper-executor-is-action-actor");
            AddFailure(failures, targetInState, "target-in-state");
            AddFailure(failures, riderInState, "rider-in-state");
            AddFailure(failures, mountInState, "mount-in-state");
            AddFailure(failures, riderConscious, "rider-conscious");
            AddFailure(failures, mountConscious, "mount-conscious");
            AddFailure(failures, targetConsciousOrChildStarted, "target-conscious-or-child-started");
            AddFailure(failures, riderNotFinallyDead, "rider-not-finally-dead");
            AddFailure(failures, mountNotFinallyDead, "mount-not-finally-dead");
            AddFailure(failures, targetNotFinallyDead, "target-not-finally-dead");
            AddFailure(failures, actionActorHostileToTarget, "action-actor-hostile-to-target");
            AddFailure(failures, actionActorCanAttackTarget, "action-actor-can-attack-target");
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
            var codes = new List<MountedCombatRejectionCode>();
            var mountName = string.IsNullOrWhiteSpace(context.MountDisplayName)
                ? "mount"
                : context.MountDisplayName;
            if (context.Action == MountedCombatActionKind.None)
            {
                reasons.Add("Choose Rider Primary or " + mountName + " Primary.");
                codes.Add(MountedCombatRejectionCode.WrongActionState);
            }
            if (!context.FeatureEnabled)
            {
                reasons.Add("The private-alpha mounted combat feature is disabled.");
                codes.Add(MountedCombatRejectionCode.FeatureDisabled);
            }
            if (!context.ExactRiderSelection)
            {
                reasons.Add("Select only the mounted rider before using a mounted combat action.");
                codes.Add(MountedCombatRejectionCode.WrongActorOrSelection);
            }
            if (!context.ExactMountedPair)
            {
                reasons.Add("The exact mounted relationship is no longer valid.");
                codes.Add(MountedCombatRejectionCode.RelationshipInvalidated);
            }
            if (!context.SupportedMountProfile || !context.SupportedRiderBodyProfile)
            {
                reasons.Add("Combat requires the exact active Medium-humanoid/" + mountName + " profile.");
                codes.Add(MountedCombatRejectionCode.BodyProfileUnsupported);
            }
            if (!context.InCombat && !context.NativeHostileInitiation)
            {
                reasons.Add("Mounted attacks are available only in combat.");
                codes.Add(MountedCombatRejectionCode.NotInCombat);
            }
            if (!context.RiderAliveAndConscious || !context.MountAliveAndConscious)
            {
                reasons.Add("Rider and " + mountName + " must both be alive and conscious.");
                codes.Add(MountedCombatRejectionCode.PairLifeStateInvalid);
            }
            if (!context.TargetExists || !context.TargetAliveAndConscious)
            {
                reasons.Add("Choose one valid living target.");
                codes.Add(MountedCombatRejectionCode.TargetInvalid);
            }
            if (!context.TargetVisible)
            {
                reasons.Add("The target is not visible to the player.");
                codes.Add(MountedCombatRejectionCode.TargetNotVisible);
            }
            if (!context.TargetHostile)
            {
                reasons.Add("The target is not hostile to the action actor.");
                codes.Add(MountedCombatRejectionCode.TargetNotHostile);
            }
            if (!context.TargetAttackable || !context.TargetIsVisibleEnemy)
            {
                reasons.Add("The target is not currently attackable.");
                codes.Add(MountedCombatRejectionCode.TargetNotAttackable);
            }
            if (!context.ActionActorOwnsCurrentTurnOrRealTime)
            {
                reasons.Add("The action actor must own the current turn.");
                codes.Add(MountedCombatRejectionCode.WrongTurn);
            }
            if (!context.ActionActorHasStandardAction)
            {
                reasons.Add("The action actor has no Standard action available.");
                codes.Add(MountedCombatRejectionCode.WrongActionState);
            }
            if (!context.TransactionIdle)
            {
                reasons.Add("A mounted pair command is already active.");
                codes.Add(MountedCombatRejectionCode.AlreadyActiveCommand);
            }
            if (context.LoadingOrLifecycleBoundary)
            {
                reasons.Add("Mounted attacks are blocked during lifecycle transitions.");
                codes.Add(MountedCombatRejectionCode.LifecycleBoundary);
            }
            var riderAction = context.Action == MountedCombatActionKind.RiderMelee ||
                context.Action == MountedCombatActionKind.RiderRanged;
            if (riderAction && !context.RiderHasEligibleWeapon)
            {
                reasons.Add("The rider has no eligible equipped weapon.");
                codes.Add(MountedCombatRejectionCode.NoEligibleWeapon);
            }
            else if (context.Action == MountedCombatActionKind.RiderMelee && context.RiderWeaponIsRanged)
            {
                reasons.Add("Rider melee requires a melee weapon; the equipped primary weapon is ranged.");
                codes.Add(MountedCombatRejectionCode.UnsupportedWeaponCategory);
            }
            else if (context.Action == MountedCombatActionKind.RiderMelee &&
                (!context.RiderWeaponCategorySupported || !context.RiderWeaponIsSupportedMelee))
            {
                reasons.Add("Rider melee requires an ordinary native melee weapon.");
                codes.Add(MountedCombatRejectionCode.UnsupportedWeaponCategory);
            }
            else if (context.Action == MountedCombatActionKind.RiderRanged &&
                (!context.RiderWeaponIsRanged || !context.RiderWeaponCategorySupported))
            {
                reasons.Add("Rider ranged requires an ordinary native ranged weapon.");
                codes.Add(MountedCombatRejectionCode.UnsupportedWeaponCategory);
            }
            if (context.Action == MountedCombatActionKind.MountPrimaryNatural &&
                !context.MountPrimaryNaturalAttackIsExact)
            {
                reasons.Add("The " + mountName + " primary natural attack could not be identified exactly.");
                codes.Add(MountedCombatRejectionCode.NoEligibleWeapon);
            }
            if (context.PathKnownUnavailable)
            {
                reasons.Add("The " + mountName + " has no supported path to that target.");
                codes.Add(MountedCombatRejectionCode.NoPath);
            }
            if (!context.WithinSupportedRangeEnvelope)
            {
                reasons.Add("The target is outside the supported mounted approach range.");
                codes.Add(MountedCombatRejectionCode.OutsideSupportedRange);
            }
            if (!context.RangeOriginConsistent)
            {
                reasons.Add("The mounted " + mountName + "-origin and native rider range gates disagree.");
                codes.Add(MountedCombatRejectionCode.RangeOriginMismatch);
            }
            if (!context.CommandAdmissionReady)
            {
                reasons.Add("The mounted command could not enter the action actor's command slot.");
                codes.Add(MountedCombatRejectionCode.CommandAdmissionFailure);
            }

            var actor = context.Action == MountedCombatActionKind.RiderMelee ||
                context.Action == MountedCombatActionKind.RiderRanged
                ? MountedCombatActor.Rider
                : context.Action == MountedCombatActionKind.MountPrimaryNatural
                    ? MountedCombatActor.Mount
                    : MountedCombatActor.None;
            return new MountedCombatActionAvailability(reasons.Count == 0, actor, reasons, codes);
        }
    }
}
