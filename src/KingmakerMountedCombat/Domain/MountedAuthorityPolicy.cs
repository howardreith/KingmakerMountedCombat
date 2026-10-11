namespace KingmakerMountedCombat.Domain
{
    // The ONE typed decision about whether the qualified paired authority is the live
    // one. Combat Mount is supported only on the accepted architecture:
    //
    //   EnablePairedActivation      true
    //   EnableUnifiedMountedTurn    false
    //   EnablePairedCommandScheduler false
    //
    // Prediction and execution-time admission both ask this same function, so what the
    // player is shown and what the delivery admits can never disagree. Mounting outside
    // combat is unaffected: it does not adopt a running encounter and therefore does not
    // depend on the paired authority at all.
    //
    // The two retired authorities are excluded rather than merely unused. Either one
    // live alongside paired activation would mean two systems believe they own the
    // mounted turn, and a combat transition would be adopting an encounter whose
    // ownership is ambiguous.
    public static class MountedAuthorityPolicy
    {
        public static bool IsQualifiedForCombatMount(
            bool pairedActivationEnabled,
            bool unifiedMountedTurnEnabled,
            bool pairedCommandSchedulerEnabled)
        {
            return pairedActivationEnabled && !unifiedMountedTurnEnabled && !pairedCommandSchedulerEnabled;
        }

        // The exact obstacle, so a refusal never has to say "unavailable". Null when the
        // authority is qualified.
        public static string DescribeUnqualifiedCombatMount(
            bool pairedActivationEnabled,
            bool unifiedMountedTurnEnabled,
            bool pairedCommandSchedulerEnabled)
        {
            if (IsQualifiedForCombatMount(pairedActivationEnabled, unifiedMountedTurnEnabled, pairedCommandSchedulerEnabled))
            {
                return null;
            }
            if (!pairedActivationEnabled)
            {
                return "Mounting during combat requires paired activation to be enabled.";
            }
            if (unifiedMountedTurnEnabled && pairedCommandSchedulerEnabled)
            {
                return "Mounting during combat requires both retired turn experiments to be disabled.";
            }
            if (unifiedMountedTurnEnabled)
            {
                return "Mounting during combat requires the retired unified mounted turn to be disabled.";
            }
            return "Mounting during combat requires the retired paired command scheduler to be disabled.";
        }
    }
}
