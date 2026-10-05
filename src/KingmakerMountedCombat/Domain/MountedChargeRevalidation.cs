using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // Chunk 6B increment 6B.2: the charge conditions that are mutable DURING the transaction.
    //
    // The initial CanTarget is not enough. A charge takes time: the target can move, die, turn friendly or
    // become unattackable; the straight native route can be broken by a creature stepping into it; the
    // landing point can be occupied; the pair can be dissolved; and the command or carrier ownership the
    // charge depends on can be replaced. So the mutable conditions are re-evaluated before every repath or
    // re-force, before the approach-to-attack transition, and immediately before the native charge attack
    // starts.
    //
    // This is deliberately NOT MountedChargePolicy. That policy decides ADMISSION, and two of its rules are
    // actively wrong once the transaction is running:
    //
    //   * it requires an idle pair, while a running charge necessarily holds the mount's Move slot and the
    //     rider's Standard slot - here the same facts are checked with the opposite sense, as ownership;
    //   * it requires the rider's Standard action, which the enclosing full-round shell has already paid.
    //     Re-demanding it would refuse the charge for the very action that bought it.
    //
    // The minimum charge distance is also an admission condition only. Arrival is necessarily inside it, so
    // revalidating it mid-transaction would invalidate every charge that was working. The maximum is
    // revalidated, because a target that has fled beyond it is no longer a charge this transaction can
    // complete.
    //
    // A newly invalid charge terminates without an attack. Nothing here refunds anything: after native
    // commitment the cost the engine took stands, and this policy only ever reports.
    public enum MountedChargeRevalidationPhase
    {
        // About to recompute or re-force the straight path onto the carrier.
        BeforeRepath = 0,

        // About to leave the approach and enter attack range.
        BeforeAttackTransition = 1,

        // About to start the native charge attack.
        BeforeAttackStart = 2
    }

    public sealed class MountedChargeRevalidationRequest
    {
        public MountedChargeRevalidationPhase Phase;

        // The target, which is the most mutable thing in the transaction.
        public bool TargetValid;
        public bool TargetVisible;
        public bool TargetHostile;
        public bool TargetAttackable;

        // Geometry. StraightRoute is the native navmesh trace; LandingBlocked is the destination footprint.
        public bool StraightRoute;
        public bool LandingBlocked;
        public bool MountAvoidanceDisabled;
        public float Distance;
        public float MaximumRange;

        // The pair itself.
        public bool RelationshipMounted;
        public bool ExactPair;
        public bool LifecycleBoundary;

        // Ownership, checked with the opposite sense to admission: the charge must still own what it took.
        // The carrier owns the mount's Move slot while approaching and must have released it by attack start.
        public bool RiderOwnsAttackSlot;
        public bool CarrierOwnsMountMoveSlot;
        public bool MountQueueEmpty;
    }

    public sealed class MountedChargeRevalidationOutcome
    {
        public MountedChargeRevalidationOutcome(bool isValid, string reason, MountedCombatRejectionCode? code)
        {
            IsValid = isValid;
            Reason = reason;
            RejectionCode = code;
        }

        public bool IsValid { get; }

        public string Reason { get; }

        public MountedCombatRejectionCode? RejectionCode { get; }
    }

    public static class MountedChargeRevalidation
    {
        public const string ValidReason = "The mounted charge is still valid.";

        private static string Metres(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + " m";
        }

        private static MountedChargeRevalidationOutcome Invalid(string reason, MountedCombatRejectionCode code)
        {
            return new MountedChargeRevalidationOutcome(false, reason, code);
        }

        public static MountedChargeRevalidationOutcome Evaluate(MountedChargeRevalidationRequest request)
        {
            if (request == null)
            {
                return Invalid("The mounted charge has no revalidation request.",
                    MountedCombatRejectionCode.CommandAdmissionFailure);
            }

            // The pair first: without it there is no charge to revalidate.
            if (!request.RelationshipMounted || !request.ExactPair)
            {
                return Invalid("The mounted charge lost its exact mounted pair.",
                    MountedCombatRejectionCode.RelationshipInvalidated);
            }

            if (request.LifecycleBoundary)
            {
                return Invalid("The mounted charge crossed a mounted lifecycle boundary.",
                    MountedCombatRejectionCode.LifecycleBoundary);
            }

            // The target, in the same order the admission policy uses so the reasons stay recognisable.
            if (!request.TargetValid)
            {
                return Invalid("The mounted charge target is no longer alive.",
                    MountedCombatRejectionCode.TargetInvalid);
            }

            if (!request.TargetVisible)
            {
                return Invalid("The mounted charge target is no longer visible.",
                    MountedCombatRejectionCode.TargetNotVisible);
            }

            if (!request.TargetHostile)
            {
                return Invalid("The mounted charge target is no longer hostile.",
                    MountedCombatRejectionCode.TargetNotHostile);
            }

            if (!request.TargetAttackable)
            {
                return Invalid("The mounted charge target is no longer attackable.",
                    MountedCombatRejectionCode.TargetNotAttackable);
            }

            // Ownership. The charge must still own exactly what it took; this is the admission policy's
            // idle-pair rule read with the opposite sense.
            if (!request.RiderOwnsAttackSlot)
            {
                return Invalid("The mounted charge no longer owns the rider attack command.",
                    MountedCombatRejectionCode.AlreadyActiveCommand);
            }

            if (!request.MountQueueEmpty)
            {
                return Invalid("The mounted charge found a queued command on the mount.",
                    MountedCombatRejectionCode.AlreadyActiveCommand);
            }

            if (request.Phase == MountedChargeRevalidationPhase.BeforeAttackStart)
            {
                // By attack start the carrier must have released the mount's Move slot: the approach is over
                // and nothing may still be driving the mover.
                if (request.CarrierOwnsMountMoveSlot)
                {
                    return Invalid("The mounted charge still held the mount movement slot at attack start.",
                        MountedCombatRejectionCode.AlreadyActiveCommand);
                }

                // Geometry is not re-read at the attack boundary beyond the target's own state: the mount has
                // arrived, so the straight route and the landing point have already been consumed, and the
                // distance is attack range rather than charge range.
                return new MountedChargeRevalidationOutcome(true, ValidReason, null);
            }

            // While approaching, the carrier must still own the mount's Move slot, because the forced path
            // lives only as long as that command does.
            if (!request.CarrierOwnsMountMoveSlot)
            {
                return Invalid("The mounted charge lost the carrier that owns the mount movement slot.",
                    MountedCombatRejectionCode.NoPath);
            }

            // The maximum only. The minimum is an admission condition: arrival is necessarily inside it.
            if (request.Distance > request.MaximumRange)
            {
                return Invalid("The charge target moved farther than the maximum charge distance of " +
                    Metres(request.MaximumRange) + ".", MountedCombatRejectionCode.OutsideSupportedRange);
            }

            if (!request.StraightRoute)
            {
                return Invalid("The charge line to the target is obstructed.",
                    MountedCombatRejectionCode.NoPath);
            }

            if (request.LandingBlocked && !request.MountAvoidanceDisabled)
            {
                return Invalid("Another creature blocks the charge landing point.",
                    MountedCombatRejectionCode.NoPath);
            }

            return new MountedChargeRevalidationOutcome(true, ValidReason, null);
        }
    }
}
