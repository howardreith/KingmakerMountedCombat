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
        {
            IsVisible = visible;
            IsEnabled = enabled;
            Reason = reason ?? string.Empty;
        }

        public bool IsVisible { get; }

        public bool IsEnabled { get; }

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
