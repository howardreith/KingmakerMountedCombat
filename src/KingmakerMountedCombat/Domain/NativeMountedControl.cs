using System;

namespace KingmakerMountedCombat.Domain
{
    public enum NativeMountedControlKind
    {
        None,
        MountCompanion,
        Dismount,
        RiderPrimary,
        MountPrimary
    }

    public sealed class NativeMountedControlAvailability
    {
        public NativeMountedControlAvailability(bool visible, bool enabled, string reason)
            : this(visible, enabled, enabled, reason)
        {
        }

        public NativeMountedControlAvailability(
            bool visible, bool enabled, bool transitionReady, string reason)
        {
            IsVisible = visible;
            IsEnabled = enabled;
            IsTransitionReady = transitionReady;
            Reason = reason ?? string.Empty;
        }

        public bool IsVisible { get; }

        /// <summary>
        /// APPROACH admission: the control may be offered, armed and targeted, and
        /// Kingmaker may create its own Move-typed command for it.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// TRANSITION admission. Equal to <see cref="IsEnabled"/> for every control that
        /// defers nothing; combat Mount defers distance to the native approach, so this is
        /// false while the pair is still outside the envelope even though the control is
        /// enabled and targetable.
        /// </summary>
        public bool IsTransitionReady { get; }

        public string Reason { get; }
    }

    public static class NativeMountedControlPolicy
    {
        public static bool OwnsPendingControl(NativeMountedControlKind kind, bool exactCaster,
            bool started, bool finished)
        {
            return exactCaster && !started && !finished &&
                (kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount);
        }

        public static bool IsExpectedPrimaryCaster(
            NativeMountedControlKind kind,
            bool turnBased,
            bool casterIsRider,
            bool casterIsMount)
        {
            return IsExpectedPrimaryCaster(
                kind,
                turnBased,
                false,
                casterIsRider,
                casterIsMount);
        }

        public static bool ShouldPreparePrimaryIntentShell(
            NativeMountedControlKind kind,
            bool featureEnabled,
            bool relationshipMounted,
            bool exactManagedAbility,
            bool casterIsExactRider)
        {
            return featureEnabled && relationshipMounted && exactManagedAbility && casterIsExactRider &&
                (kind == NativeMountedControlKind.RiderPrimary ||
                 kind == NativeMountedControlKind.MountPrimary);
        }

        public static bool IsExpectedPrimaryCaster(
            NativeMountedControlKind kind,
            bool turnBased,
            bool unifiedMountedTurn,
            bool casterIsRider,
            bool casterIsMount)
        {
            if (kind == NativeMountedControlKind.RiderPrimary)
            {
                return casterIsRider;
            }

            if (kind != NativeMountedControlKind.MountPrimary)
            {
                return false;
            }

            if (unifiedMountedTurn)
            {
                return casterIsRider;
            }

            return turnBased ? casterIsMount : casterIsRider;
        }

        // The ONE escape decision. A mounted or faulted rider must always keep its own
        // native Dismount while the mod service itself is active, whatever the movement
        // feature, paired activation or either retired authority says — otherwise a live
        // pair has no lawful way to separate and the player is stranded mounted.
        //
        // Only the exact rider qualifies; the mount never does; an unmounted rider never
        // does. Mount and the mounted attack controls stay feature-gated. This decides
        // leasing, availability, targeting and delivery alike, so a fact can never be
        // visible-but-disabled: that combination is exactly how a rider gets stranded.
        public static bool IsDismountEscape(
            NativeMountedControlKind kind,
            bool relationshipMounted,
            bool relationshipFaulted,
            bool unitIsRider)
        {
            return kind == NativeMountedControlKind.Dismount &&
                (relationshipMounted || relationshipFaulted) && unitIsRider;
        }

        public static bool ShouldLease(
            NativeMountedControlKind kind,
            bool featureEnabled,
            bool ownerHasSupportedMount,
            bool relationshipMounted,
            bool relationshipFaulted,
            bool unitIsRider,
            bool unitIsMount)
        {
            return ShouldLease(
                kind,
                featureEnabled,
                false,
                ownerHasSupportedMount,
                relationshipMounted,
                relationshipFaulted,
                unitIsRider,
                unitIsMount);
        }

        public static bool ShouldLease(
            NativeMountedControlKind kind,
            bool featureEnabled,
            bool unifiedMountedTurn,
            bool ownerHasSupportedMount,
            bool relationshipMounted,
            bool relationshipFaulted,
            bool unitIsRider,
            bool unitIsMount)
        {
            // The escape hatch, and it comes before every feature gate. One typed
            // decision, shared by leasing, availability, targeting and delivery.
            if (IsDismountEscape(kind, relationshipMounted, relationshipFaulted, unitIsRider))
            {
                return true;
            }

            if (!featureEnabled)
            {
                return false;
            }

            if (!relationshipMounted && !relationshipFaulted)
            {
                return kind == NativeMountedControlKind.MountCompanion && ownerHasSupportedMount;
            }

            if (kind == NativeMountedControlKind.Dismount)
            {
                return unitIsRider;
            }

            if (relationshipFaulted)
            {
                return false;
            }

            if (kind == NativeMountedControlKind.RiderPrimary ||
                kind == NativeMountedControlKind.MountPrimary)
            {
                if (unifiedMountedTurn)
                {
                    return unitIsRider;
                }

                return unitIsRider || unitIsMount;
            }

            return false;
        }

        public static string WrongTurnReason(NativeMountedControlKind kind, string mountName)
        {
            return WrongTurnReason(kind, mountName, false);
        }

        public static string WrongTurnReason(
            NativeMountedControlKind kind,
            string mountName,
            bool unifiedMountedTurn)
        {
            var exactMountName = string.IsNullOrWhiteSpace(mountName) ? "mount" : mountName;
            if (kind == NativeMountedControlKind.RiderPrimary)
            {
                return "Rider primary belongs to the rider's turn.";
            }

            if (kind == NativeMountedControlKind.MountPrimary)
            {
                if (unifiedMountedTurn)
                {
                    return exactMountName + " primary belongs to the rider-led shared turn.";
                }

                return exactMountName + " primary belongs to the " + exactMountName + "'s turn.";
            }

            throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
