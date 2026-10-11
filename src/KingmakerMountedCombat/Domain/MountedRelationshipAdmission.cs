namespace KingmakerMountedCombat.Domain
{
    // The exact authority under which a mounted relationship may be attached.
    // Each mode is its own separately testable path so that neither a generic
    // restore flag nor a diagnostic entry point can authorize voluntary combat
    // mounting, and so that a voluntary combat admission cannot silently stand
    // in for the free exploration transition.
    public enum MountedRelationshipAdmission
    {
        // Ordinary out-of-combat mounting. Refuses any live encounter.
        Exploration = 0,

        // The player's voluntary combat Mount. Kingmaker's native Move shell is
        // the sole cost owner; this mode only authorizes the relationship. It
        // refuses when there is no live encounter.
        VoluntaryCombat = 1,

        // Restoration of a saved relationship by the persistence service. It
        // adopts whatever encounter state the archive was captured in and never
        // executes Mount, a turn, a preparation or a cost.
        SavedRestore = 2
    }
}
