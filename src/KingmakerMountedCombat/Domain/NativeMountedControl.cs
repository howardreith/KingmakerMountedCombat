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
        public static bool IsExpectedPrimaryCaster(
            NativeMountedControlKind kind,
            bool turnBased,
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
                return unitIsRider || unitIsMount;
            }

            return false;
        }

        public static string WrongTurnReason(NativeMountedControlKind kind, string mountName)
        {
            var exactMountName = string.IsNullOrWhiteSpace(mountName) ? "mount" : mountName;
            if (kind == NativeMountedControlKind.RiderPrimary)
            {
                return "Rider primary belongs to the rider's turn.";
            }

            if (kind == NativeMountedControlKind.MountPrimary)
            {
                return exactMountName + " primary belongs to the " + exactMountName + "'s turn.";
            }

            throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
