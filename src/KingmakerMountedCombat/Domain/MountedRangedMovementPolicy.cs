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

        // A ranged weapon attack: a native RuleAttackWithWeapon roll (bows, crossbows, slings, thrown weapons and
        // weapon-delivered firearms, whose touch AC is still a weapon attack) with a ranged, non-natural weapon. Spell rays,
        // touch spells and kinetic blasts are delivered by AbilityDeliverProjectile/AbilityDeliverTouch as bare
        // RuleAttackRolls with a proxy weapon; the proxy categories are refused as well in case another path wraps them.
        public static bool QualifyingRangedWeaponAttack(bool weaponAttackRule, bool rangedWeapon, bool naturalWeapon, bool spellProxyCategory) =>
            weaponAttackRule && rangedWeapon && !naturalWeapon && !spellProxyCategory;

        public static bool EnteredSecondMove(float normalTravelSeconds) =>
            Finite(normalTravelSeconds) && normalTravelSeconds > SecondMoveThresholdSeconds;

        // The mount's allocation is current for this rider's attack only when its travel belongs to this rider's mounted
        // activation: the mount's grant is the rider's own paired grant (one activation, including a Delay into a later
        // round), or neither has a grant (the legacy turn modes' round allocations) and it is of the current round. A
        // mount's standalone native grant is never current for a rider: that travel was made on the mount's own turn,
        // possibly riderless or under another rider before a mid-round mount (MidEncounterAdoptionPolicy retains the
        // mount's own slot), so it cannot be attributed to this rider's shot.
        public static bool IsAllocationCurrent(bool sameController, string mountGrant, string riderGrant, bool mountRoundIsCurrent)
        {
            if (!sameController) return false;
            if (mountGrant != null) return riderGrant != null && string.Equals(mountGrant, riderGrant, StringComparison.Ordinal);
            return riderGrant == null && mountRoundIsCurrent;
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
