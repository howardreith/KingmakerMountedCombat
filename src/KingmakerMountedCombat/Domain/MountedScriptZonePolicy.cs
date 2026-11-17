namespace KingmakerMountedCombat.Domain
{
    // Known issues C04 (Candlemere, "wade through"). Native ScriptZoneController.Tick (060092A0) rechecks a unit's
    // script-zone membership only when the unit HasMotionThisTick (0600830F: Position != PreviousPosition), and
    // UnitMoveController.Tick (06009183) stamps PreviousPosition at the start of its tick. A seated rider's position is
    // written by the pair's synchronization outside that window (in the UnitMoveController prefix, before the stamp, and
    // in LateUpdate), so a rider carried into or out of a zone is never rechecked: its membership keeps whatever it was
    // when it mounted. Any authored condition that asks whether that unit is in a zone then answers from a stale place.
    // At Candlemere the crossing asks whether the interacting unit is in the near-side zone to choose its direction, so a
    // rider carried there is sent to the near-side enter point: a fade, and no crossing.
    public static class MountedScriptZonePolicy
    {
        // Unity's Vector3 equality: positions closer than this squared distance compare equal, as native
        // Position != PreviousPosition does.
        public const float SamePositionSquaredDistance = 9.99999944E-11f;

        public static bool PositionChanged(float dx, float dy, float dz) =>
            dx * dx + dy * dy + dz * dz >= SamePositionSquaredDistance;

        // Whether a pair's seated rider is checked against the active script zones after native ScriptZoneController.Tick,
        // with the native ScriptZone.CheckUnit and its own rules: the pair is mounted; native did not check the rider
        // itself this tick (no native motion); and its carried position moved since its membership was last checked, or
        // was never checked while mounted.
        public static bool RiderNeedsZoneRecheck(bool pairMounted, bool riderHasNativeMotionThisTick, bool checkedBefore,
            float dx, float dy, float dz) =>
            pairMounted && !riderHasNativeMotionThisTick && (!checkedBefore || PositionChanged(dx, dy, dz));
    }
}
