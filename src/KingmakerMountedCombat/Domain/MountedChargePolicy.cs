using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: the availability and targeting policy of the pair-owned Mounted Charge.
    // Pure: every input is a value read elsewhere from live native state, so the decision is unit-testable
    // and the same decision serves availability, prediction, targeting, click admission and execution
    // revalidation. Fails closed with one exact reason; never writes or approximates anything.
    //
    // The geometry inputs are read from the MOUNT, which is the mover: the stock charge reads caster
    // position, speed and corpulence, and for the pair the mount owns all three. The minimum range is the
    // stock one (turn-based: five-foot-step metres + GameConsts.MinWeaponRange + both corpulences;
    // real-time: ten feet + both corpulences) and the maximum is the stock mount CombatSpeedMps * 6.
    public sealed class MountedChargeRequest
    {
        public bool FeatureEnabled;
        public bool RelationshipMounted;
        public bool ExactPair;
        public bool RiderDirectlyControllable;
        public bool LifecycleBoundary;
        public bool InCombat;
        public bool TurnBased;
        public bool RiderTurn;
        public bool TurnActingOrPreparing;
        public float TurnTimeMoved;
        public bool RiderCanActInCombat;
        public float RiderStandardCooldown;
        public float RiderMoveCooldown;
        public bool WeaponPresent;
        public bool WeaponIsRanged;
        public bool TargetValid;
        public bool TargetVisible;
        public bool TargetHostile;
        public bool TargetAttackable;
        public bool AlreadyActiveCommand;
        public bool MountCommandsIdle;
        public float Distance;
        public float MinimumRange;
        public float MaximumRange;
        public bool StraightRoute;
        public bool LandingBlocked;
        public bool MountAvoidanceDisabled;
    }

    public sealed class MountedChargeAvailability
    {
        public MountedChargeAvailability(bool isAllowed, string reason, MountedCombatRejectionCode? code)
        {
            IsAllowed = isAllowed;
            Reason = reason;
            RejectionCode = code;
        }

        public bool IsAllowed { get; }

        public string Reason { get; }

        public MountedCombatRejectionCode? RejectionCode { get; }
    }

    public static class MountedChargePolicy
    {
        public const string AllowedReason = "Mounted Charge is available.";

        private static string Metres(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + " m";
        }

        private static MountedChargeAvailability Refuse(string reason, MountedCombatRejectionCode code)
        {
            return new MountedChargeAvailability(false, reason, code);
        }

        public static MountedChargeAvailability Evaluate(MountedChargeRequest request)
        {
            if (request == null)
            {
                return Refuse("Mounted Charge has no request to evaluate.", MountedCombatRejectionCode.FeatureDisabled);
            }

            if (!request.FeatureEnabled)
            {
                return Refuse("Mounted Charge is disabled.", MountedCombatRejectionCode.FeatureDisabled);
            }

            if (!request.RelationshipMounted || !request.ExactPair)
            {
                return Refuse("Mounted Charge requires the exact mounted pair.", MountedCombatRejectionCode.RelationshipInvalidated);
            }

            if (!request.RiderDirectlyControllable)
            {
                return Refuse("Mounted Charge requires the directly controllable rider.", MountedCombatRejectionCode.WrongActorOrSelection);
            }

            if (request.LifecycleBoundary)
            {
                return Refuse("Mounted Charge is unavailable across a mounted lifecycle boundary.", MountedCombatRejectionCode.LifecycleBoundary);
            }

            if (!request.InCombat)
            {
                return Refuse("Mounted Charge requires combat.", MountedCombatRejectionCode.NotInCombat);
            }

            if (!request.TargetValid)
            {
                return Refuse("Mounted Charge requires a living target.", MountedCombatRejectionCode.TargetInvalid);
            }

            if (!request.TargetVisible)
            {
                return Refuse("Mounted Charge requires a visible target.", MountedCombatRejectionCode.TargetNotVisible);
            }

            if (!request.TargetHostile)
            {
                return Refuse("Mounted Charge requires a hostile target.", MountedCombatRejectionCode.TargetNotHostile);
            }

            if (!request.TargetAttackable)
            {
                return Refuse("Mounted Charge requires an attackable target.", MountedCombatRejectionCode.TargetNotAttackable);
            }

            if (!request.WeaponPresent)
            {
                return Refuse("Mounted Charge requires a weapon in the rider's hands.", MountedCombatRejectionCode.NoEligibleWeapon);
            }

            if (request.WeaponIsRanged)
            {
                return Refuse("Mounted Charge requires a melee weapon.", MountedCombatRejectionCode.MountedRangedUnsupported);
            }

            if (request.AlreadyActiveCommand || !request.MountCommandsIdle)
            {
                return Refuse("Mounted Charge requires an idle pair.", MountedCombatRejectionCode.AlreadyActiveCommand);
            }

            if (!request.RiderCanActInCombat)
            {
                return Refuse("Mounted Charge requires a rider who can act.", MountedCombatRejectionCode.WrongActionState);
            }

            if (request.RiderStandardCooldown > 0.001f)
            {
                return Refuse("Mounted Charge requires the rider's standard action.", MountedCombatRejectionCode.WrongActionState);
            }

            if (request.TurnBased)
            {
                if (!request.RiderTurn || !request.TurnActingOrPreparing)
                {
                    return Refuse("Charge the mounted pair during the rider's turn.", MountedCombatRejectionCode.WrongTurn);
                }

                if (request.RiderMoveCooldown > 0.001f)
                {
                    return Refuse("Mounted Charge is a full-round action and requires the rider's move action.", MountedCombatRejectionCode.WrongActionState);
                }

                if (request.TurnTimeMoved > 0.0001f)
                {
                    return Refuse("Mounted Charge requires the rider's turn before any movement.", MountedCombatRejectionCode.WrongActionState);
                }
            }

            if (request.Distance < request.MinimumRange)
            {
                return Refuse("The charge target is nearer than the minimum charge distance of " + Metres(request.MinimumRange) + ".", MountedCombatRejectionCode.OutsideSupportedRange);
            }

            if (request.Distance > request.MaximumRange)
            {
                return Refuse("The charge target is farther than the maximum charge distance of " + Metres(request.MaximumRange) + ".", MountedCombatRejectionCode.OutsideSupportedRange);
            }

            if (!request.StraightRoute)
            {
                return Refuse("The charge line to the target is obstructed.", MountedCombatRejectionCode.NoPath);
            }

            if (request.LandingBlocked && !request.MountAvoidanceDisabled)
            {
                return Refuse("Another creature blocks the charge landing point.", MountedCombatRejectionCode.NoPath);
            }

            return new MountedChargeAvailability(true, AllowedReason, null);
        }
    }
}
