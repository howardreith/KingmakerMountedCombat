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
                string.Equals(scenario, "unmounted-attack-controls-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-rider-incapacitation-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-rider-death-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-mount-death-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-targeting-rider-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-targeting-mount-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-targeting-area-unmounted-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-obstruction-ranged-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-horse-strike-comparison-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-ranged-native-control-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-interrupt-melee-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-interrupt-ranged-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-inspection-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-session-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-session-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-sustained-melee-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-sustained-ranged-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-sustained-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-charge-safety-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk4-charge-safety-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "actor-allocation-rider-first-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "actor-allocation-mount-first-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "actor-allocation-rider-first-unmounted-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "actor-allocation-mount-first-unmounted-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "ordinary-attack-controls-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3d-unified-combat-tb-suite", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3d-horse-presentation-suite", StringComparison.Ordinal);
        }
    }
}
