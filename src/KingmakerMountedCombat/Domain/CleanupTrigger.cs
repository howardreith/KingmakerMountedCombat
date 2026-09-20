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
        ViewDetached = 50,
        ViewReplaced = 51,
        CompanionInvalidated = 60,
        Incapacitated = 70,
        Death = 80,
        CombatStarted = 90,
        AreaUnloading = 100,
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
