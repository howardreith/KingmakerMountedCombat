namespace KingmakerMountedCombat.Domain
{
    public enum MountedFamiliarFollowSource
    {
        // The owner is not a mounted rider: the native follow rule runs unchanged.
        NativeMaster,
        // The ridden mount has a complete path: follow its end point, which stays fixed during one move,
        // as the unmounted master's own complete path end does natively.
        MountPathEnd,
        // The ridden mount is idle or its path is not complete: follow the mount's ground position.
        MountGround
    }

    // Exploration repair P1. An item familiar (Kingmaker.Visual.Critters.Familiar, 2.1.7b) follows its master by
    // reading the master's STOCK movement agent path and entity position. A mounted rider's stock agent is stopped
    // (no path) and its entity position is the moving saddle point, so the native rule re-paths every frame to a
    // random free spot near a moving point. The physical mover of a mounted rider is its mount; the familiar
    // follows the mount's path end or ground position instead. Nothing about the rider changes.
    public static class MountedFamiliarFollowPolicy
    {
        // Native Familiar.MaxDistanceToMaster: the familiar starts following when the follow point is this far
        // from its current destination (or its own position when it has none).
        public const float NativeMaxDistanceToMasterMetres = 10f;

        public static MountedFamiliarFollowSource Source(bool masterIsMountedRider, bool mountPathComplete)
        {
            if (!masterIsMountedRider) return MountedFamiliarFollowSource.NativeMaster;
            return mountPathComplete ? MountedFamiliarFollowSource.MountPathEnd : MountedFamiliarFollowSource.MountGround;
        }

        // The native trigger, unchanged: already following, or the follow point left the leash distance.
        public static bool ShouldFollow(bool alreadyFollowing, float followPointToDestinationMetres)
        {
            return alreadyFollowing || followPointToDestinationMetres > NativeMaxDistanceToMasterMetres;
        }

        // Native TeleportToMaster copies the master view's parent. A mounted rider's view parent is the pair's owned
        // attachment anchor (a child of the mount view), so the familiar must take the mount view's own container
        // instead; any other parent is native and is left alone.
        public static bool MustLeaveOwnedAnchor(bool masterIsMountedRider, bool parentIsRiderViewParent)
        {
            return masterIsMountedRider && parentIsRiderViewParent;
        }
    }
}
