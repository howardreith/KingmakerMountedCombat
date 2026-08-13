namespace KingmakerMountedCombat.Domain
{
    public interface IMountedPairRuntime
    {
        void AcquireMovementAuthority(MountedPair pair);

        void AttachPresentation(MountedPair pair);

        void RestorePresentation(MountedPair pair);

        void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger);
    }
}
