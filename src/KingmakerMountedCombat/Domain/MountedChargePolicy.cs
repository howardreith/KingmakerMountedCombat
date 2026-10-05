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

        // The pair's movement delegation, qualified in Chunk 6A, engages while the rider's turn is
        // acting, and - since increment 6B.3's bounded seam - also while it is preparing, for the
        // pair's own live charge transaction alone. Both halves are observed from the live turn, and
        // the preparing half is why a turn-based charge can now be delivered rather than refused.
        public bool TurnActing;
        public float TurnTimeMoved;
        public bool RiderCanActInCombat;
        public float RiderStandardCooldown;
        public float RiderMoveCooldown;

        // True only while this mod's own full-round charge shell is the ability being delivered for this
        // rider, observed from the live execution context. The native shell is the sole cost owner and has
        // already spent the rider's action through AbilityData.Spend by the time it asks this policy to
        // deliver, so the spent action must stop disqualifying the very action that spent it. Nothing is
        // written, cleared or refunded; absent proof of the executing shell the strict requirement stands.
        public bool DeliveringOwnShell;
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

            // Availability, prediction and targeting are asked before the cast and require a free action.
            // Delivery is asked from inside the shell that has already paid, and must not re-demand it.
            if (!request.DeliveringOwnShell && request.RiderStandardCooldown > 0.001f)
            {
                return Refuse("Mounted Charge requires the rider's standard action.", MountedCombatRejectionCode.WrongActionState);
            }

            if (request.TurnBased)
            {
                // Increment 6B.3 is no longer a blanket refusal. The four rules below are the ones
                // preview.167 wrote out in the comment that replaced them, restored unchanged, and the
                // delegation seam they depend on is opened for exactly this transaction and only while
                // the rider own turn has not moved. Nothing is written, cleared, refunded or
                // synthesised; a turn-based charge that cannot satisfy these is refused with its own
                // exact reason and costs the player nothing.
                if (!request.RiderTurn)
                {
                    return Refuse("Mounted Charge belongs to the rider's own turn.",
                        MountedCombatRejectionCode.WrongTurn);
                }

                if (!request.TurnActingOrPreparing)
                {
                    return Refuse("Mounted Charge requires the rider's own acting or preparing turn.",
                        MountedCombatRejectionCode.WrongTurn);
                }

                // The charge carries the pair across the field, so the turn must still have its
                // movement: a turn that has already moved cannot buy the approach.
                if (request.TurnTimeMoved > 0.0001f)
                {
                    return Refuse("Mounted Charge requires a turn that has not moved yet.",
                        MountedCombatRejectionCode.WrongTurn);
                }

                // As with the standard action, the mod own full-round shell has already paid the move
                // action by the time it asks this policy to deliver, so delivery must not re-demand it.
                if (!request.DeliveringOwnShell && request.RiderMoveCooldown > 0.001f)
                {
                    return Refuse("Mounted Charge requires the rider's move action.",
                        MountedCombatRejectionCode.WrongActionState);
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
