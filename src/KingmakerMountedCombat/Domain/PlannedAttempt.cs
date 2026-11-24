namespace KingmakerMountedCombat.Domain
{
    // Diagnostic custody of one planned check total (supplemental review PN10-F2): armed for exactly one rider, mount and attacker,
    // consumed by the first qualifying attempt and then disarmed, so a later hit (for example one arriving while the driver waits
    // for the use to renew) rolls naturally; another pair's hit never consumes it. Engine-free so it is tested offline.
    public sealed class PlannedAttempt
    {
        private int? delta;
        private string rider, mount, attacker;

        public bool Armed => delta.HasValue;
        public string Plan { get; private set; }
        // The attempt that consumed the last plan (its record's rule identity), or null.
        public int? ConsumedBy { get; private set; }

        public void Arm(string plan, int checkMinusAttack, string riderId, string mountId, string attackerId)
        {
            Plan = plan; delta = checkMinusAttack; rider = riderId; mount = mountId; attacker = attackerId; ConsumedBy = null;
        }

        // The planned total for this attempt, once: null for any other pair or attacker, and after the plan was consumed.
        public int? Consume(string riderId, string mountId, string attackerId, int attackResult, int attemptIdentity)
        {
            if (!delta.HasValue || riderId != rider || mountId != mount || attackerId != attacker) return null;
            var total = attackResult + delta.Value;
            delta = null;
            ConsumedBy = attemptIdentity;
            return total;
        }

        public void Clear() { delta = null; rider = mount = attacker = null; Plan = null; }

        // The record that completes a step, as an index into the records observed since the step's boundary (supplemental review
        // PN11-F3): a planned step completes only on exactly the attempt that consumed its plan (nothing before it is consumed,
        // whatever refusals or follow-ups arrive around it); any other step on the first record of the measured pair. -1: none.
        public static int CompletingIndex(System.Collections.Generic.IList<int> identities, System.Collections.Generic.IList<bool> measured, bool plannedStep, int? consumedBy)
        {
            for (var index = 0; index < identities.Count; index++)
            {
                if (!measured[index]) continue;
                if (!plannedStep) return index;
                if (consumedBy.HasValue && identities[index] == consumedBy.Value) return index;
            }
            return -1;
        }
    }
}
