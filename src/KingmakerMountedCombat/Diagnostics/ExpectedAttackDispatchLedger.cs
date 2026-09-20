namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Records each independently validated expected-attack dispatch in a bounded
    /// diagnostic sequence. Started remains sticky so incoming traffic can still
    /// be classified as before or after the first deliberate dispatch.
    /// </summary>
    internal sealed class ExpectedAttackDispatchLedger
    {
        public bool Started { get; private set; }

        public int MarkCount { get; private set; }

        public bool Mark()
        {
            Started = true;
            MarkCount++;
            return true;
        }
    }
}
