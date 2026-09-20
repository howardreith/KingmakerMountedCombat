namespace KingmakerMountedCombat.Domain
{
    public static class AvoidanceRestorationExpectation
    {
        public static bool Matches(bool capturedEffectiveState, bool riderIsConscious, bool actualEffectiveState)
        {
            var expectedEffectiveState = capturedEffectiveState || !riderIsConscious;
            return actualEffectiveState == expectedEffectiveState;
        }
    }
}
