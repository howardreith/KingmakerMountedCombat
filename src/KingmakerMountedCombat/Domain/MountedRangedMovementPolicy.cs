using System;

namespace KingmakerMountedCombat.Domain
{
    // Why a ranged weapon attack received no mounted-movement contribution, or that it received one.
    public enum MountedRangedMovementOutcome
    {
        Unavailable,
        NotTurnBased,
        NotActiveRider,
        NotQualifyingAttack,
        NoCurrentAllocation,
        WithinFirstMove,
        Applied
    }

    // The native facts read at one attack roll. Travel is the mount's normal locomotion in seconds of native movement
    // time (forced/charge and five-foot-step time excluded), read from its current allocation.
    public struct MountedRangedMovementFacts
    {
        public bool Available;
        public bool TurnBased;
        public bool ActiveRider;
        public bool QualifyingAttack;
        public bool CurrentAllocation;
        public float NormalTravelSeconds;
        public bool OwnsMountedArchery;
    }

    // The mounted ranged-motion rule as adapted to Kingmaker's turn-based movement allowance (MOUNTED-ARCHERY design).
    //
    // A native Move action is 3 s of movement time; TurnController.GetRemainingMovementTime bounds every tick of a
    // still-unspent first allowance at that 3 s mark. The one way the first allowance runs over is TickMovement's
    // "borrowed time": within 0.1 m of the path's end (UnitMovementAgent.NearTheEnd) a tick is not clamped. A tick is at
    // most TimeController's turn-based cap, PlayerTimeScale (clamped to 3) / 12 = 0.25 s, and that can happen once, at the
    // end of the move that reaches the boundary. Travel beyond 3.25 s therefore always contains genuine second-allowance
    // travel; travel up to it may be one move plus its endpoint correction, which this rule treats as one move.
    public static class MountedRangedMovementPolicy
    {
        public const float FirstMoveSeconds = 3f;
        public const float NativeMaximumTurnBasedTickSeconds = 3f / 12f;
        public const float SecondMoveThresholdSeconds = FirstMoveSeconds + NativeMaximumTurnBasedTickSeconds;
        public const int DoubleMovePenalty = -4;
        public const int MountedArcheryDoubleMovePenalty = -2;

        // Normal locomotion only. TurnController.TickMovement (and the delegated MountedMovementState.Advance, which mirrors
        // it) adds forced-mode time to both TimeMoved and TimeMovedInForceMode, and step time to both TimeMoved and
        // TimeMovedByFiveFootStep. A non-locomotion Move cost (standing up) never reaches TimeMoved.
        public static float NormalTravelSeconds(float timeMoved, float timeForced, float timeStepped)
        {
            if (!Finite(timeMoved) || !Finite(timeForced) || !Finite(timeStepped)) return 0f;
            return Math.Max(0f, timeMoved - timeForced - timeStepped);
        }

        // A ranged weapon attack: a ranged, non-natural weapon that the attacker actually wields (ItemEntity.Wielder is set
        // on equip and cleared on unequip). That covers bows, crossbows, slings, thrown weapons and firearms, whichever rule
        // delivers the roll: a native RuleAttackWithWeapon, or a mod's own attack rolls on the equipped weapon (Gunslinger's
        // Dead Shot probes have no RuleAttackWithWeapon parent). A firearm's touch AC is still a weapon attack. Spell rays,
        // touch spells and kinetic blasts use proxy weapons created for the delivery and never equipped; their proxy
        // categories are refused as well.
        public static bool QualifyingRangedWeaponAttack(bool wieldedByAttacker, bool rangedWeapon, bool naturalWeapon, bool spellProxyCategory) =>
            wieldedByAttacker && rangedWeapon && !naturalWeapon && !spellProxyCategory;

        // A calculation that decides a committed roll: it is triggered while the attacker's RuleAttackRoll against the same
        // target with the same weapon is on the rulebook stack and has not yet stored its attack-bonus rule. That is the
        // native calculation inside RuleAttackRoll.OnTrigger, and a confirmation a mod makes for that roll before its body
        // runs (Gunslinger's Dead Shot confirms an auto-hit delivery in an OnTrigger prefix). Character-sheet, tooltip and
        // other previews are triggered with no enclosing roll; a parry is the defender's, after the roll resolved.
        public static bool CommittedRollCalculation(bool enclosedByAttackRoll, bool sameAttackerTargetAndWeapon, bool rollAlreadyResolved) =>
            enclosedByAttackRoll && sameAttackerTargetAndWeapon && !rollAlreadyResolved;

        public static bool EnteredSecondMove(float normalTravelSeconds) =>
            Finite(normalTravelSeconds) && normalTravelSeconds > SecondMoveThresholdSeconds;

        // The pair's current paired activation grant: its identity when the activation is turn-based (a boundary exists),
        // not split, and has exactly this rider as principal and this mount as partner, with the mount granted in it. Null
        // otherwise. This, not the rider's own movement record, is the authority: a mid-encounter mount adopts the rider's
        // already-running turn without re-preparing it (PreparePartnerThisRound prepares the mount alone), so the rider's
        // record is absent or carries its earlier native grant while the mount's record carries the activation's identity.
        public static string CurrentPairedGrant<TActor, TBoundary>(PairedActivation<TActor, TBoundary> activation, TActor rider, TActor mount)
            where TActor : class where TBoundary : class
        {
            if (activation == null || rider == null || mount == null || activation.Split || activation.Sequence <= 0 ||
                !ReferenceEquals(activation.Principal, rider) || !ReferenceEquals(activation.Partner, mount) ||
                activation.Mount == null || !activation.Mount.Granted) return null;
            return activation.Identity;
        }

        // The mount's allocation is current for this rider's attack only when its travel belongs to the pair's current
        // mounted activation: with paired activation in use, the mount's grant equals that activation's identity (Delay into
        // a later round included). A mount's standalone native grant never matches: that travel was made on its own turn,
        // riderless or under another rider (a RetainPartnerParticipation mount keeps its own slot), so it is never charged to
        // this rider. The legacy turn modes grant nothing; there an ungranted record of the current round is current.
        public static bool IsAllocationCurrent(bool sameController, string mountGrant, string pairedGrant, bool pairedLifecycle, bool mountRoundIsCurrent)
        {
            if (!sameController) return false;
            if (pairedLifecycle) return pairedGrant != null && string.Equals(mountGrant, pairedGrant, StringComparison.Ordinal);
            return mountGrant == null && mountRoundIsCurrent;
        }

        public static MountedRangedMovementOutcome Evaluate(MountedRangedMovementFacts facts, out int contribution)
        {
            contribution = 0;
            if (!facts.Available) return MountedRangedMovementOutcome.Unavailable;
            if (!facts.TurnBased) return MountedRangedMovementOutcome.NotTurnBased;
            if (!facts.ActiveRider) return MountedRangedMovementOutcome.NotActiveRider;
            if (!facts.QualifyingAttack) return MountedRangedMovementOutcome.NotQualifyingAttack;
            if (!facts.CurrentAllocation) return MountedRangedMovementOutcome.NoCurrentAllocation;
            if (!EnteredSecondMove(facts.NormalTravelSeconds)) return MountedRangedMovementOutcome.WithinFirstMove;
            contribution = facts.OwnsMountedArchery ? MountedArcheryDoubleMovePenalty : DoubleMovePenalty;
            return MountedRangedMovementOutcome.Applied;
        }

        private static bool Finite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
