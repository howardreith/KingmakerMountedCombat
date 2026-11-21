using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Why a rider's public Mount / Dismount control is not live right now.
    public enum MountedControlAbsence
    {
        // The rider is still eligible: an area change, a save scope, the capital moving it to the reserve roster, its
        // fact replaced or switched off while it is out of the game. The arrangement is kept for its return.
        Temporary,
        // The rider lost eligibility for good (no supported companion any more, or it left the roster): forget it.
        Permanent
    }

    // The player's arrangement of one rider's public Mount / Dismount control, kept per actor id while the runtime fact
    // behind it is temporarily absent. Native UnitUISettings.GetSlot / CleanupBadSlots empty a slot whose ability fact is
    // gone, so without this record the control could not come back to where the player put it. An arrangement is either
    // a chosen slot or "off the bar" (the player removed the live control, or filled its slot meanwhile); both mean the
    // rider already had its one automatic placement. Holds slot indices and ids only, never a native reference.
    // Pure decisions; the control service reads and writes the native slots.
    public sealed class MountedControlPlacementIntent
    {
        public const int OffBar = -1;

        private sealed class Entry
        {
            // The chosen slot, or OffBar.
            internal int Index;
            // The control stopped being live at least once since this arrangement was last observed live.
            internal bool Interrupted;
            // The pet link (native UnitDescriptor.PetUniqueId) the arrangement was observed with; null when it came from
            // a saved row and has not been observed live yet.
            internal string PetId;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public int Count => entries.Count;

        public bool Has(string actorId) => actorId != null && entries.ContainsKey(actorId);

        // The live control was seen in this slot: this is the player's current arrangement (a move updates it).
        public void ObserveBound(string actorId, int index, string petId)
        {
            if (actorId == null || index < 0) return;
            entries[actorId] = new Entry { Index = index, Interrupted = false, PetId = petId };
        }

        // The live control is on no slot by the player's choice (or the placement found a full bar).
        public void ObserveOffBar(string actorId, string petId)
        {
            if (actorId == null) return;
            entries[actorId] = new Entry { Index = OffBar, Interrupted = false, PetId = petId };
        }

        // The control is not live for this actor. Temporary absence keeps the arrangement; permanent loss forgets it.
        public void ObserveAbsent(string actorId, MountedControlAbsence absence)
        {
            if (actorId == null) return;
            if (absence == MountedControlAbsence.Permanent) { entries.Remove(actorId); return; }
            Entry entry;
            if (entries.TryGetValue(actorId, out entry)) entry.Interrupted = true;
        }

        // Native bad-slot cleanup decided the public-control slot at slotIndex on this actor's bar is bad (its fact was
        // gone or inactive): that slot is the player's current choice (a move or first manual placement since the last
        // update included), and its emptying is a temporary absence, whatever the fact's state by the next update.
        public void ObserveNativeSlotInvalidated(string actorId, int slotIndex, string petId)
        {
            if (actorId == null || slotIndex < 0) return;
            ObserveBound(actorId, slotIndex, petId);
            ObserveAbsent(actorId, MountedControlAbsence.Temporary);
        }

        // A saved arrangement: a slot row (index >= 0) or a placement record without a slot (OffBar). A freshly loaded
        // world takes it as is. A replay of rows captured earlier in the same world (area transfer, retained remount)
        // never overrides what has been observed since: it is used only for an actor with no arrangement yet.
        public void Seed(string actorId, int index, bool loadedWorld)
        {
            if (actorId == null || index < OffBar) return;
            if (!loadedWorld && entries.ContainsKey(actorId)) return;
            entries[actorId] = new Entry { Index = index, Interrupted = true, PetId = null };
        }

        // Whether a saved slot row may be written into this slot now: only when it is the actor's current arrangement
        // (or the actor has none). A later observed move or removal wins over an older row.
        public bool AllowsSavedSlot(string actorId, int index)
        {
            Entry entry;
            return actorId != null && (!entries.TryGetValue(actorId, out entry) || entry.Index == index);
        }

        // The control is live but no slot holds it. Returns the slot to put it back in, or -1. Only a return from
        // temporary absence restores it, and only into an empty slot. The player removing the live control, or filling
        // that slot meanwhile, makes the arrangement OffBar (still placed, never re-added). An actor with no arrangement
        // gets one only if it already had its placement (alreadyPlaced), as OffBar.
        public int ResolveUnbound(string actorId, Func<int, bool> slotIsEmpty, bool alreadyPlaced, string petId)
        {
            Entry entry;
            if (actorId == null) return -1;
            if (!entries.TryGetValue(actorId, out entry))
            {
                if (alreadyPlaced) ObserveOffBar(actorId, petId);
                return -1;
            }
            if (entry.Index == OffBar || !entry.Interrupted || slotIsEmpty == null || !slotIsEmpty(entry.Index))
            {
                ObserveOffBar(actorId, petId);
                return -1;
            }
            return entry.Index;
        }

        // One reconciliation of a reconciled rider, in the service's order (NativeMountedControlService.ReconcileFacts
        // then ReconcileChosenSlot). controlLive: the public fact is present and active after leasing; controlDesired:
        // the rider still gets it (ShouldLease); factReplaced: this update replaced a missing, inactive or duplicated
        // desired fact (native toolbar cleanup may already have emptied its slot); boundIndex: the first slot holding
        // the control after the stale-binding repair, or -1; leasePending: a save scope's own lease will put an emptied
        // slot back. Returns the slot the caller must bind to the live control (and then report with ObserveBound), or -1.
        public int Reconcile(string actorId, bool controlLive, bool controlDesired, bool factReplaced, int boundIndex,
            Func<int, bool> slotIsEmpty, bool leasePending, bool alreadyPlaced, string petId)
        {
            if (actorId == null) return -1;
            if (!controlLive)
            {
                // Not leaseable now is not proof of loss: a pet that does not resolve this update, or whose reciprocal
                // link is momentarily unavailable, keeps the arrangement while the persistent pet link (petId) still
                // names the same companion. A cleared or different link retires it (Revalidate).
                if (!controlDesired && !Revalidate(actorId, true, true, petId)) return -1;
                ObserveAbsent(actorId, MountedControlAbsence.Temporary);
                return -1;
            }
            // Recorded before the arrangement is read: a replacement is a return from absence, never a removal.
            if (factReplaced) ObserveAbsent(actorId, MountedControlAbsence.Temporary);
            if (boundIndex >= 0) { ObserveBound(actorId, boundIndex, petId); return -1; }
            if (leasePending) return -1;
            // A different companion than the one the arrangement was made with is not the same arrangement.
            Revalidate(actorId, true, true, petId);
            return ResolveUnbound(actorId, slotIsEmpty, alreadyPlaced, petId);
        }

        // At a boundary that empties the slots (a save scope or an area unload), before they are emptied: the
        // arrangement on the bar now is authoritative (a move or removal since the last update included).
        // Also at a roster departure, before the departing rider's KMC slots are emptied. A slot still holding the public
        // control is the player's choice even if its fact is momentarily inactive. No slot holding it is the player's
        // removal unless an absence is already recorded: native bad-slot cleanup is observed as an absence when it
        // decides (ObserveNativeSlotInvalidated), and KMC's own slot clearing records one, so neither reaches here as a
        // removal. controlLive is kept for the caller's record; the decision does not depend on it.
        public void ObserveAtBoundary(string actorId, bool controlLive, int boundIndex, bool alreadyPlaced, string petId)
        {
            if (actorId == null) return;
            if (boundIndex >= 0) ObserveBound(actorId, boundIndex, petId);
            else ObserveUnboundAtBoundary(actorId, alreadyPlaced, petId);
        }

        // At a boundary that empties the slots (a save scope), a live control on no slot: a continuously live control
        // was removed by the player (OffBar); a return from absence still pending is left for its own reconciliation.
        public void ObserveUnboundAtBoundary(string actorId, bool alreadyPlaced, string petId)
        {
            Entry entry;
            if (actorId == null) return;
            if (!entries.TryGetValue(actorId, out entry)) { if (alreadyPlaced) ObserveOffBar(actorId, petId); return; }
            if (!entry.Interrupted) ObserveOffBar(actorId, petId);
        }

        // Positive evidence of permanent loss for an actor that is not reconciled now (for example on the reserve
        // roster): it left the player's roster, or its pet link is cleared or names another unit than the one the
        // arrangement was observed with. An actor that cannot be resolved at all proves nothing and keeps its record.
        // Returns false when the record was forgotten.
        public bool Revalidate(string actorId, bool resolved, bool onPlayerRoster, string currentPetId)
        {
            Entry entry;
            if (actorId == null || !entries.TryGetValue(actorId, out entry)) return false;
            if (!resolved) return true;
            if (!onPlayerRoster || string.IsNullOrEmpty(currentPetId) ||
                entry.PetId != null && !string.Equals(entry.PetId, currentPetId, StringComparison.Ordinal))
            {
                entries.Remove(actorId);
                return false;
            }
            if (entry.PetId == null) entry.PetId = currentPetId;
            return true;
        }

        // The arrangement a save made now must carry for an actor whose control is not live: a slot (index >= 0) or
        // OffBar (a placement record only). False when there is nothing to carry.
        public bool TryGetArrangement(string actorId, out int index)
        {
            Entry entry;
            index = OffBar;
            if (actorId == null || !entries.TryGetValue(actorId, out entry)) return false;
            index = entry.Interrupted ? entry.Index : OffBar;
            // A live-observed slot that was never interrupted is written by the live capture, not from here.
            return entry.Interrupted || entry.Index == OffBar;
        }

        // Positive evidence, for a reconciled rider that no longer gets the control, that the loss is permanent: its pet
        // link is cleared, or names another unit than the one its arrangement was observed with. The caller then also
        // clears the rider's public-control slots, so a later companion cannot revive the old binding.
        public bool ProvesPermanentLoss(string actorId, string currentPetId)
        {
            Entry entry;
            if (string.IsNullOrEmpty(currentPetId)) return true;
            return actorId != null && entries.TryGetValue(actorId, out entry) && entry.PetId != null &&
                !string.Equals(entry.PetId, currentPetId, StringComparison.Ordinal);
        }

        // The persistent pet link now names a different, nonempty companion than the one this arrangement was observed
        // with (a direct change of pets, with no cleared link in between). Checked before any observation can overwrite
        // the stored link; the caller then retires the arrangement and clears the rider's public-control slots.
        public bool PetLinkChanged(string actorId, string currentPetId)
        {
            Entry entry;
            return actorId != null && !string.IsNullOrEmpty(currentPetId) && entries.TryGetValue(actorId, out entry) &&
                entry.PetId != null && !string.Equals(entry.PetId, currentPetId, StringComparison.Ordinal);
        }

        // An exact copy of every arrangement (for a load boundary that may be abandoned).
        public MountedControlPlacementIntent Copy()
        {
            var copy = new MountedControlPlacementIntent();
            foreach (var pair in entries)
                copy.entries[pair.Key] = new Entry { Index = pair.Value.Index, Interrupted = pair.Value.Interrupted, PetId = pair.Value.PetId };
            return copy;
        }

        // Replaces every arrangement with an exact copy of another record set.
        public void ReplaceWith(MountedControlPlacementIntent other)
        {
            entries.Clear();
            if (other == null) return;
            foreach (var pair in other.entries)
                entries[pair.Key] = new Entry { Index = pair.Value.Index, Interrupted = pair.Value.Interrupted, PetId = pair.Value.PetId };
        }

        public IEnumerable<string> ActorIds => entries.Keys;

        public void Forget(string actorId)
        {
            if (actorId != null) entries.Remove(actorId);
        }

        public void Clear() => entries.Clear();
    }

    // The load boundary of the placement records. A selected load clears them when its native iterator starts
    // (NativeMountedControlService.BeginLoadedWorld), while the outgoing player object is still current; native loading
    // replaces Game.Instance.State.PlayerState only later (2.1.7b ThreadedGameLoader deserializes a new Player). Until
    // that replacement no observation may write into the cleared records: the outgoing world's riders still pass
    // every current-world check. The fence lifts by itself once the current world differs from the fenced one, or when
    // the load is abandoned and the outgoing world stays.
    //
    // The fence belongs to the load scope that armed it (owner). Its outgoing records are kept, not discarded: an
    // abandoned load whose outgoing world is still current gets them back (reserve slots and deliberate removals the bar
    // cannot reconstruct); a replaced world drops them. Only the owning scope can abandon its fence, so an older scope's
    // disposal never releases a newer scope's fence, and a superseded scope releases its own.
    public sealed class MountedControlLoadFence
    {
        private object fencedWorld, owner;
        private MountedControlPlacementIntent outgoingIntent;
        private HashSet<string> outgoingPlaced;

        public bool Fenced => fencedWorld != null;

        // Arms the fence for one load scope and moves the outgoing records aside (the live sets are cleared).
        public void Begin(object outgoingWorld, object loadOwner, MountedControlPlacementIntent intent, ISet<string> placed)
        {
            if (fencedWorld != null && ReferenceEquals(fencedWorld, outgoingWorld))
            {
                // A newer scope starts while an older one still fences the same world: the records moved aside by the
                // older scope are still the outgoing world's; the newer scope takes ownership of them.
                intent?.Clear(); placed?.Clear();
                owner = loadOwner;
                return;
            }
            outgoingIntent = intent?.Copy();
            outgoingPlaced = placed == null ? null : new HashSet<string>(placed, StringComparer.Ordinal);
            intent?.Clear(); placed?.Clear();
            fencedWorld = outgoingWorld; owner = loadOwner;
            if (fencedWorld == null) Release();
        }

        // The owning scope ended without native completion. If its outgoing world is still current, the outgoing records
        // come back exactly; otherwise they are dropped. Returns false (and changes nothing) for any other scope.
        public bool Abandon(object loadOwner, object currentWorld, MountedControlPlacementIntent intent, ISet<string> placed)
        {
            if (fencedWorld == null || !ReferenceEquals(owner, loadOwner)) return false;
            if (ReferenceEquals(currentWorld, fencedWorld))
            {
                intent?.ReplaceWith(outgoingIntent);
                if (placed != null)
                {
                    placed.Clear();
                    if (outgoingPlaced != null) foreach (var id in outgoingPlaced) placed.Add(id);
                }
            }
            Release();
            return true;
        }

        // False while the fenced outgoing world is still current; a different current world releases the fence and drops
        // the outgoing records (the incoming world owns its own).
        public bool Accepts(object currentWorld)
        {
            if (fencedWorld != null && !ReferenceEquals(currentWorld, fencedWorld)) Release();
            return fencedWorld == null;
        }

        public void Reset() => Release();

        private void Release() { fencedWorld = null; owner = null; outgoingIntent = null; outgoingPlaced = null; }
    }
}
