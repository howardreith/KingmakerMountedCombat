namespace KingmakerMountedCombat.Domain
{
    public enum CleanupTrigger
    {
        Manual = 0,
        DestinationCancelled = 10,
        UnexpectedCommand = 20,
        TurnBasedModeChanged = 30,
        RealtimeModeChanged = 31,
        SaveRequested = 40,
        LoadRequested = 41,
        GameModeBoundary = 45,
        // Exploration repair: a scripted event (dialog, locked cutscene), camp or world-map travel suspends the
        // attachment while the pair's mounted intent is retained and restored without a Mount action afterwards.
        EventSuspension = 46,
        ViewDetached = 50,
        ViewReplaced = 51,
        CompanionInvalidated = 60,
        // Compensating cleanup for a combat Mount whose paired encounter adoption
        // could not be completed. The relationship must not stand without its
        // activation, so the attachment is undone and the transition is reported
        // as failed. It writes no native resource and refunds no committed Move.
        AdoptionRefused = 65,
        Incapacitated = 70,
        // FALL-02: the exact ridden mount newly fell prone. The rider is unseated and the soft fall resolves.
        MountFellProne = 75,
        Death = 80,
        CombatStarted = 90,
        AreaUnloading = 100,
        AreaSuspension = 101,
        ModDisabled = 110,
        Exception = 120,
        ProcessTeardown = 130
    }

    public static class CleanupTriggerPriority
    {
        public static CleanupTrigger Higher(CleanupTrigger first, CleanupTrigger second)
        {
            return (int)first >= (int)second ? first : second;
        }
    }
}
