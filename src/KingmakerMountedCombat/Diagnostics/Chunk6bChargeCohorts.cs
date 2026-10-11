using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Fixed bounded batches. Every historical RT case remains mandatory; the spent-Standard
    // repeat is emitted by positive without a new positioning or preparation boundary.
    internal static class Chunk6bChargeCohorts
    {
        internal const string Core = "chunk6b-charge-core-rt";
        internal const string Interruption = "chunk6b-charge-interruption-rt";
        internal const string Lifecycle = "chunk6b-charge-lifecycle-rt";

        internal static string[] Select(string scenario)
        {
            string[] names;
            switch (scenario)
            {
                case Core:
                    names = new[] { "default-off", "positive", "below-minimum", "beyond-maximum",
                        "stock-rejected", "obstructed-line", "blocked-clearance", "cancelled", "duplicate" };
                    break;
                case Interruption:
                    names = new[] { "default-off", "interrupted", "child-cleanup", "combat-ended",
                        "exception-cleanup", "action-failed-before-rule", "action-failed-after-rule",
                        "lease-application-failed", "target-moved", "target-lost", "new-landing-blocker" };
                    break;
                case Lifecycle:
                    names = new[] { "default-off", "rider-incapacitated", "mount-incapacitated",
                        "feature-disabled", "dismounted", "mode-changed", "relationship-invalidated",
                        "view-replaced", "mount-dead", "rider-dead" };
                    break;
                default: return null;
            }
            return Array.ConvertAll(names, name => "C6B-CHARGE-" + name);
        }
    }
}
