namespace KingmakerMountedCombat.Domain
{
    // Known issues sprint 3 continuation. The group-pace and stick-movement hooks run many times a second and fall back
    // to native behaviour when something inside them fails. Their fault report is made once and can never undo that
    // fallback: a sink (the logger) that itself fails is contained.
    public sealed class MountedBoundedFaultReport
    {
        public bool Reported { get; private set; }

        public bool SinkFailed { get; private set; }

        public void Report(System.Action<string, System.Exception> sink, string message, System.Exception error)
        {
            if (Reported) return;
            Reported = true;
            if (sink == null) return;
            try { sink(message, error); }
            catch (System.Exception) { SinkFailed = true; }
        }
    }
}
