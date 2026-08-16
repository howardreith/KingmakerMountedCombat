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
