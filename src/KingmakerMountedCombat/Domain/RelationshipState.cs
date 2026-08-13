namespace KingmakerMountedCombat.Domain
{
    public enum RelationshipState
    {
        Unmounted,
        Validating,
        Mounting,
        Mounted,
        Dismounting,
        Faulted,
        Disposed
    }
}
