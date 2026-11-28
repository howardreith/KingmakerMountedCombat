using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // PR #70 R13-F3 (round 17): the movement fact of an EXACT carrier that survives between the rider's observations. The delegated movement
    // entry (MountedMovementStateAdapter.TickDelegated) records, at its debit, that it returned a positive tick while this command held the
    // mount's Move slot: native moved the mount along that command's issued path. Keyed weakly by the command object, so a finished or
    // replaced carrier leaves nothing behind and a new carrier starts without the fact. Engine-free (the key is the command object).
    public static class MountedCarrierMovementLedger
    {
        private static readonly ConditionalWeakTable<object, object> moved = new ConditionalWeakTable<object, object>();
        private static readonly object Marker = new object();

        public static void RecordPositiveTick(object moveSlotCommand)
        {
            if (moveSlotCommand == null) return;
            object existing;
            if (!moved.TryGetValue(moveSlotCommand, out existing)) moved.Add(moveSlotCommand, Marker);
        }

        public static bool HasPositiveTick(object carrier)
        {
            object existing;
            return carrier != null && moved.TryGetValue(carrier, out existing);
        }
    }
}
