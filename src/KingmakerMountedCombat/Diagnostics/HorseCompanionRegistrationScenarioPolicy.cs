using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class HorseCompanionRegistrationScenarioPolicy
    {
        internal static bool SupportsScenario(string scenario)
        {
            return string.Equals(scenario, "horse-companion-blueprint-registration", StringComparison.Ordinal) ||
                string.Equals(scenario, "horse-companion-unmounted-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "horse-mounted-alpha-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "horse-native-controls-ux-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3d-unified-combat-rt-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3g-native-controls-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3g-native-controls-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3h-combat-loop-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3h-combat-loop-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "ordinary-attack-controls-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3d-unified-combat-tb-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3d-horse-presentation-suite", StringComparison.Ordinal);
        }
    }
}
