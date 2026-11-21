using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Why a rider's public Mount / Dismount control is not live right now.
    public enum MountedControlAbsence
    {
        // The rider is still eligible: an area change, a save scope, the capital moving it to the reserve roster, or
        // its abilities switched off while it is out of the game. The chosen slot is kept for its return.
        Temporary,
        // The rider lost eligibility for good (no supported companion any more, or it left the roster): forget the slot.
        Permanent
    }

    // The player's chosen toolbar slot for one rider's public Mount / Dismount control, kept per actor id while the
    // runtime fact behind it is temporarily absent. Native UnitUISettings.GetSlot / CleanupBadSlots empty a slot whose
    // ability fact is gone, so without this record the control could not come back to where the player put it.
    // Holds slot indices and actor ids only, never a native reference. Pure decisions; the control service reads and
    // writes the native slots.
    public sealed class MountedControlPlacementIntent
    {
        private sealed class Entry
        {
            internal int Index;
            // The control stopped being live at least once since it was last seen in this slot.
            internal bool Interrupted;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public int Count => entries.Count;

        // The live control was seen in this slot: this is the player's current arrangement (a move updates it).
        public void ObserveBound(string actorId, int index)
        {
            if (actorId == null || index < 0) return;
            entries[actorId] = new Entry { Index = index, Interrupted = false };
        }

        // The control is not live for this actor. Temporary absence keeps the chosen slot; permanent loss forgets it.
        public void ObserveAbsent(string actorId, MountedControlAbsence absence)
        {
            if (actorId == null) return;
            if (absence == MountedControlAbsence.Permanent) { entries.Remove(actorId); return; }
            Entry entry;
            if (entries.TryGetValue(actorId, out entry)) entry.Interrupted = true;
        }

        // A saved or carried row that could not be bound now: remembered as interrupted, so the control returns to it.
        public void Seed(string actorId, int index)
        {
            if (actorId == null || index < 0) return;
            entries[actorId] = new Entry { Index = index, Interrupted = true };
        }

        // The control is live but no slot holds it. Returns the slot to put it back in, or -1. Only a return from
        // temporary absence restores it, and only into an empty slot: the player removing the control while it was
        // live, or putting something else in that slot meanwhile, is respected and the record is forgotten.
        public int ResolveUnbound(string actorId, Func<int, bool> slotIsEmpty)
        {
            Entry entry;
            if (actorId == null || !entries.TryGetValue(actorId, out entry)) return -1;
            if (!entry.Interrupted || slotIsEmpty == null || !slotIsEmpty(entry.Index))
            {
                entries.Remove(actorId);
                return -1;
            }
            return entry.Index;
        }

        // A slot remembered across a temporary absence, which a save made now must carry for this actor.
        public bool TryGetInterrupted(string actorId, out int index)
        {
            Entry entry;
            index = -1;
            if (actorId == null || !entries.TryGetValue(actorId, out entry) || !entry.Interrupted) return false;
            index = entry.Index;
            return true;
        }

        public IEnumerable<string> ActorIds => entries.Keys;

        public void Forget(string actorId)
        {
            if (actorId != null) entries.Remove(actorId);
        }

        public void Clear() => entries.Clear();
    }
}
