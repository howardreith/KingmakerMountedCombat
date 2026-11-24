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
    }
}
