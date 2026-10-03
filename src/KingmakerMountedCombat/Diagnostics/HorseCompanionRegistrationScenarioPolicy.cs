using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class HorseCompanionRegistrationScenarioPolicy
    {
        internal static bool SupportsScenario(string scenario)
        {
            // The Chunk 6A rows are named literally here: this policy is shared
            // with the component test project, which cannot compile the runtime
            // tranche because that type binds Kingmaker assemblies.
            return string.Equals(scenario, "chunk6a-combat-mount-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-combat-mount-tb", StringComparison.Ordinal) ||
                // The narrow save-backed Mount preamble: one native selected-ability
                // click, sixteen staged causal assertions, no mount and no combat.
                string.Equals(scenario, "chunk6a-mount-preamble", StringComparison.Ordinal) ||
                // The narrow real-time non-adjacent native-approach scenario.
                string.Equals(scenario, "chunk6a-paused-queue", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-policy-disabled", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-foreign-companion", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-wrong-creature-target", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-mount-selected", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-multiple-selection", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-refused-foreign-selection", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-mount-approach", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-stop-approach", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-combat-end-approach", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-disable-approach", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-command-replacement", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-repeated-mount-request", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-ownership-change", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-size-form-change", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-lost-direct-control", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-rider-incapacitated", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-mount-incapacitated", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-allocation-rider-first-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-allocation-mount-first-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-mount-spent-move-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-mount-spent-standard-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-mount-spent-all-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-rider-without-move-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-rider-other-action-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-unrelated-candidate-between-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-dismount-after-rider-expenditure-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-dismount-after-mount-expenditure-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-dismount-immediately-after-mount-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-auto-use-mount-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-auto-use-dismount-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-dismount-feature-disabled-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-dismount-policy-disabled-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-hotbar-approach", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-obstruction", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-geometry-change", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-adoption-compensation-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "chunk6a-adoption-compensation-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, "horse-companion-blueprint-registration", StringComparison.Ordinal) ||
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
                string.Equals(scenario, "chunk4-ground-arrival-rt", StringComparison.Ordinal) ||
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
