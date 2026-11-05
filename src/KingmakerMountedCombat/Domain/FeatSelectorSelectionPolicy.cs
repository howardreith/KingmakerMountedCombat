namespace KingmakerMountedCombat.Domain
{
    // Native Toggle.Set keeps the last active toggle of a no-switch-off group on, so a
    // light refresh cannot clear a selection that has no successor. A selected successor
    // needs nothing: native NotifyToggleOn already switches its predecessor off.
    internal static class FeatSelectorSelectionPolicy
    {
        internal static bool MustClearStaleRows(bool selectionOffersMountedCombat, bool currentSelectionHasItem, bool anyActiveRowOn)
        {
            return selectionOffersMountedCombat && !currentSelectionHasItem && anyActiveRowOn;
        }
    }
}
