using System;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 2 (DOOR-01, SPACE-01). The decisions behind one mounted door click, kept free of
    // engine types so the regressions run offline. The native shape being matched is the
    // 2.1.7b UnitInteractWithObject lifecycle: a new Standard command replaces the executor's
    // running player move, a repeated click on the same object merges into the accepted
    // command instead of restarting it, an unstarted command approaches its target and only
    // then starts, and a finished approach that still ends outside range is retried after the
    // native 0.2-0.6 s cooldown. The one deliberate difference is a bound: a retried approach
    // that makes no progress toward the interaction point is refused instead of looping.
    public enum MountedDoorAdmissionDisposition
    {
        Admit,
        // A pair attack, a rider-turn ground move or a pending turn-based ground admission owns the pair.
        RejectOtherPairCommand,
        // The clicked door is the one already being approached: the accepted command persists untouched.
        RetainActiveSameDoor,
        // Another door is clicked: the current approach yields to the new intent.
        ReplaceActiveDoor,
        // The mount is carrying out an ordinary real-time player move: it yields, as a native unit's own
        // move yields to the Standard command its player just gave it.
        ReplaceMountMovement,
        // The mount owns another command, a queue, or a turn-based ground move that cannot be replaced here.
        RejectMountBusy
    }

    public enum MountedDoorOccupancyDisposition
    {
        // The mount's logical position is on the navmesh after the door changed state.
        Valid,
        // Off the navmesh with a walkable node within the recovery radius: one native repath to it.
        RepathToNearest,
        // Off the navmesh with no walkable node nearby: reported, never transported.
        Unrecoverable
    }

    public static class MountedDoorApproachPolicy
    {
        // Native UnitCommand MinApproachCooldown / MaxApproachCooldown (2.1.7b).
        public const float NativeReapproachMinimumSeconds = 0.2f;
        public const float NativeReapproachMaximumSeconds = 0.6f;
        // A retried approach counts as progress only when it ends this much nearer the interaction point.
        public const float ProgressEpsilonMetres = 0.25f;
        // Approaches that end outside range without progress before the click is refused as unreachable.
        public const int MaximumApproachesWithoutProgress = 3;
        // Game seconds a running approach may hold the mount still before it is treated as stalled.
        public const float StalledSecondsLimit = 4f;
        // The farthest walkable node a post-interaction repath may target.
        public const float RecoveryRadiusMetres = 1.5f;
        // Displacement below this is "still" for the stall clock.
        public const float DisplacementEpsilonMetres = 0.02f;

        public static MountedDoorAdmissionDisposition DecideAdmission(
            bool otherPairCommandActive,
            bool activeDoorInteraction,
            bool activeDoorIsSameDoor,
            bool turnBasedCombat,
            bool mountMoveSlotOccupied,
            bool mountMoveIsPlayerMove,
            bool mountOtherSlotOccupied,
            bool mountQueueEmpty)
        {
            if (otherPairCommandActive)
            {
                return MountedDoorAdmissionDisposition.RejectOtherPairCommand;
            }
            if (activeDoorInteraction)
            {
                return activeDoorIsSameDoor
                    ? MountedDoorAdmissionDisposition.RetainActiveSameDoor
                    : MountedDoorAdmissionDisposition.ReplaceActiveDoor;
            }
            if (mountOtherSlotOccupied || !mountQueueEmpty)
            {
                return MountedDoorAdmissionDisposition.RejectMountBusy;
            }
            if (mountMoveSlotOccupied)
            {
                return mountMoveIsPlayerMove && !turnBasedCombat
                    ? MountedDoorAdmissionDisposition.ReplaceMountMovement
                    : MountedDoorAdmissionDisposition.RejectMountBusy;
            }
            return MountedDoorAdmissionDisposition.Admit;
        }

        public static bool ApproachProgressed(float bestDistanceMetres, float distanceNowMetres, float epsilonMetres)
        {
            if (!Finite(distanceNowMetres) || !Finite(epsilonMetres) || epsilonMetres < 0f)
            {
                return false;
            }
            // No recorded best yet: the first approach end is the baseline.
            if (float.IsPositiveInfinity(bestDistanceMetres))
            {
                return true;
            }
            if (!Finite(bestDistanceMetres))
            {
                return false;
            }
            return distanceNowMetres <= bestDistanceMetres - epsilonMetres;
        }

        public static bool ApproachExhausted(int approachesWithoutProgress, int maximum)
        {
            return maximum > 0 && approachesWithoutProgress >= maximum;
        }

        public static bool MovementStalled(float secondsWithoutDisplacement, float limitSeconds, bool approachRunning)
        {
            return approachRunning && Finite(secondsWithoutDisplacement) && Finite(limitSeconds) &&
                limitSeconds > 0f && secondsWithoutDisplacement > limitSeconds;
        }

        // The native re-approach delay for one uniform sample in [0, 1].
        public static float ReapproachDelaySeconds(float unitSample)
        {
            var clamped = !Finite(unitSample) ? 0f : Math.Max(0f, Math.Min(1f, unitSample));
            return NativeReapproachMinimumSeconds + (NativeReapproachMaximumSeconds - NativeReapproachMinimumSeconds) * clamped;
        }

        public static MountedDoorOccupancyDisposition DecideOccupancy(
            bool insideNavmesh, bool nearestNodeWalkable, float nearestNodeDistanceMetres, float recoveryRadiusMetres)
        {
            if (insideNavmesh)
            {
                return MountedDoorOccupancyDisposition.Valid;
            }
            if (nearestNodeWalkable && Finite(nearestNodeDistanceMetres) && nearestNodeDistanceMetres >= 0f &&
                nearestNodeDistanceMetres <= recoveryRadiusMetres)
            {
                return MountedDoorOccupancyDisposition.RepathToNearest;
            }
            return MountedDoorOccupancyDisposition.Unrecoverable;
        }

        public static string Feedback(MountedDoorAdmissionDisposition disposition, string mountDisplayName)
        {
            switch (disposition)
            {
                case MountedDoorAdmissionDisposition.RejectOtherPairCommand:
                    return "Mounted door interaction rejected: another pair command is active.";
                case MountedDoorAdmissionDisposition.RetainActiveSameDoor:
                    return "Mounted door interaction already in progress for this door; the accepted approach continues.";
                case MountedDoorAdmissionDisposition.ReplaceActiveDoor:
                    return "Mounted door interaction replaced: the earlier door approach was cancelled.";
                case MountedDoorAdmissionDisposition.ReplaceMountMovement:
                    return "Mounted door interaction accepted: " + mountDisplayName + " movement replaced by the door approach.";
                case MountedDoorAdmissionDisposition.RejectMountBusy:
                    return "Mounted door interaction rejected: " + mountDisplayName + " is busy; wait for its current command to finish.";
                default:
                    return "Mounted door interaction accepted: " + mountDisplayName + " approach, rider interaction.";
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
