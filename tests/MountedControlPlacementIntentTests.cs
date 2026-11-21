using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Toolbar persistence of the public Mount / Dismount control. The decisions and their order are production code
    // (MountedControlPlacementIntent.Reconcile / ObserveAtBoundary / Seed / AllowsSavedSlot / Revalidate /
    // TryGetArrangement and MountedControlSlotPolicy.AbsentArrangementRows). The Rider below is a labeled test double of
    // NativeMountedControlService and the native bar, following the service's event order: ReconcileFacts replaces a
    // missing or inactive desired fact before ReconcileChosenSlot; TryInitialPlacement runs after; a save scope observes
    // the bar, leases and empties the KMC slots, removes the facts, and on release re-leases, reconciles, then restores
    // its leases; leaving the roster empties the slots; native bad-slot cleanup empties a slot whose fact is gone.
    // Not native evidence.
    internal static class MountedControlPlacementIntentTests
    {
        private const string Control = "KMC_MountState";

        internal static void Register(TestRunner runner)
        {
            runner.Run("toolbar: chosen nondefault slot survives repeated city visits, interior transitions and save scopes", SurvivesTransitions);
            runner.Run("toolbar: a reserve rider's chosen slot survives a capital save and cold load (one slot row, one record)", SurvivesReserveSaveLoad);
            runner.Run("toolbar: a deliberate removal survives a reserve save and load as a record only and is never re-added", RemovalSurvivesReserveSaveLoad);
            runner.Run("toolbar: a move or removal made right before a save scope is what the save and the bar keep", BoundaryEditIsAuthoritative);
            runner.Run("toolbar: a native fact invalidation returns the control to its slot exactly once; a live removal stays removed", NativeInvalidation);
            runner.Run("toolbar: an older in-world replay never resurrects a control the player removed or moved since", ReplayNeverOverridesEdit);
            runner.Run("toolbar: permanent pet loss in reserve retires the arrangement; a kept pet round-trips", PermanentLossInReserve);
            runner.Run("toolbar: occupied slots, other riders and unrelated slots are never overwritten", OccupiedAndIndependent);
            runner.Run("toolbar: absent arrangement rows are exactly a slot row and a record, or a record alone", AbsentRows);
            runner.Run("toolbar: native cleanup with the fact active again before the next update still returns the slot once", NativeCleanupReactivated);
            runner.Run("toolbar: a save header before the next update keeps a just-invalidated binding; a live removal saves a record only", HeaderBeforeUpdate);
            runner.Run("toolbar: a move or removal right before a roster departure is what the reserve keeps", DepartureEditIsAuthoritative);
            runner.Run("toolbar: a party rider whose same pet link briefly does not resolve keeps its slot; a cleared link retires it", UnresolvedPetKeepsArrangement);
            runner.Run("toolbar: removing the shortcut right after a save scope returns it stays removed through save and load", RemovalAfterLeaseRelease);
        }

        private sealed class SavedRow { internal string Actor; internal int Index; internal int Kind; }

        // The service and native bar double for one rider.
        private sealed class Rider
        {
            internal readonly string Id;
            internal MountedControlPlacementIntent Intent;
            internal HashSet<string> Placed;
            internal readonly string[] Slots = new string[140];
            internal bool Candidate = true, OnRoster = true, FactLive, Autofill = true;
            // PetId: the persistent pet link (UnitDescriptor.PetUniqueId); PetResolves: the pet object and its reciprocal
            // master resolve this update (what ShouldLease needs).
            internal string PetId = "horse-1";
            internal bool PetResolves = true;
            private readonly List<int> leases = new List<int>();

            internal Rider(string id, MountedControlPlacementIntent intent, HashSet<string> placed)
            { Id = id; Intent = intent; Placed = placed; }

            internal bool Desired => PetId != null && PetResolves;
            internal int Find() => Array.IndexOf(Slots, Control);
            internal int Count() => Slots.Count(s => s == Control);

            // One NativeMountedControlService.Update for this rider.
            internal void Frame()
            {
                if (!Candidate) { Intent.Revalidate(Id, true, OnRoster, PetId); return; }
                var replaced = false;
                if (Desired && !FactLive) { FactLive = true; replaced = true; }
                if (!Desired) FactLive = false;
                var index = Intent.Reconcile(Id, FactLive, Desired, replaced, Find(), i => Slots[i] == null,
                    leases.Count != 0, Placed.Contains(Id), PetId);
                if (index >= 0) { Slots[index] = Control; Placed.Add(Id); Intent.ObserveBound(Id, index, PetId); }
                // TryInitialPlacement: once, first empty main-bar slot.
                if (FactLive && !Placed.Contains(Id) && Autofill)
                {
                    Placed.Add(Id);
                    var empty = Enumerable.Range(0, 14).FirstOrDefault(i => Slots[i] == null);
                    if (Slots[empty] == null && Find() < 0) Slots[empty] = Control;
                }
            }

            // Native bad-slot cleanup: the fact went inactive (or was replaced natively), native IsBad answered true for
            // the public slot (the production postfix records ObserveNativeSlotInvalidated) and the slot was emptied.
            // reactivated: the fact is active again before the next KMC update.
            internal void NativeInvalidate(bool reactivated = false)
            {
                var at = Find();
                if (at >= 0) { Intent.ObserveNativeSlotInvalidated(Id); Slots[at] = null; }
                FactLive = reactivated;
            }

            // KMC removes the facts and empties the KMC slots (area unload) after observing the bar at the boundary.
            internal void AreaUnload()
            {
                Intent.ObserveAtBoundary(Id, FactLive, Find(), Placed.Contains(Id), PetId);
                var at = Find(); if (at >= 0) Slots[at] = null;
                FactLive = false; Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
            }

            // The capital moves the rider to the reserve roster: the departure loop observes its bar, records the
            // absence, then KMC empties its slots and removes its facts.
            internal void ToReserve()
            {
                Intent.ObserveAtBoundary(Id, FactLive, Find(), Placed.Contains(Id), PetId);
                Intent.ObserveAbsent(Id, OnRoster ? MountedControlAbsence.Temporary : MountedControlAbsence.Permanent);
                var at = Find(); if (at >= 0) Slots[at] = null;
                Candidate = false; FactLive = false;
            }

            internal void ToParty() { Candidate = true; }

            // CapturePersistentSlots(includeAbsentIntent: true) at the archive header, before the save scope:
            // ClassifyArrangementsForCapture, CaptureLiveSlots, then CaptureAbsentIntent with one placement record.
            internal List<SavedRow> Capture()
            {
                var rows = new List<SavedRow>();
                var written = false;
                if (Candidate)
                {
                    Intent.ObserveAtBoundary(Id, FactLive, Find(), Placed.Contains(Id), PetId);
                    if (!FactLive && (Desired || Intent.Revalidate(Id, true, true, PetId)))
                        Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                    if (FactLive)
                    {
                        var at = Find();
                        if (at >= 0) { rows.Add(new SavedRow { Actor = Id, Index = at, Kind = MountedControlSlotPolicy.SavedMountState }); written = true; }
                        if (Placed.Contains(Id)) rows.Add(new SavedRow { Actor = Id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
                    }
                }
                else Intent.Revalidate(Id, true, OnRoster, PetId);
                int index;
                if (!written && Intent.TryGetArrangement(Id, out index))
                    foreach (var r in MountedControlSlotPolicy.AbsentArrangementRows(index))
                        if (r.Value != MountedControlSlotPolicy.SavedPlacementRecord || !rows.Any(x => x.Kind == MountedControlSlotPolicy.SavedPlacementRecord))
                            rows.Add(new SavedRow { Actor = Id, Index = r.Key, Kind = r.Value });
                return rows;
            }

            // A save, in production order: the archive header capture, then the save scope (boundary observation,
            // leases, fact removal), its release (re-lease and reconcile with the leases pending), then the lease
            // restore, which records the rebind. No extra update follows.
            internal List<SavedRow> Save()
            {
                var rows = Capture();
                if (Candidate)
                {
                    Intent.ObserveAtBoundary(Id, FactLive, Find(), Placed.Contains(Id), PetId);
                    var at = Find(); if (at >= 0) { leases.Add(at); Slots[at] = null; }
                    FactLive = false; Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                    Frame();
                    foreach (var leased in leases)
                        if (Slots[leased] == null && FactLive) { Slots[leased] = Control; Intent.ObserveBound(Id, leased, PetId); }
                    leases.Clear();
                }
                return rows;
            }

            // RestorePersistentSlots: records, seeding, Update, then the guarded slot-writing loop.
            internal void Restore(List<SavedRow> rows, bool loadedWorld)
            {
                var mine = rows.Where(r => r.Actor == Id).ToList();
                foreach (var r in mine.Where(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord)) Placed.Add(Id);
                var slotRow = mine.FirstOrDefault(r => r.Kind == MountedControlSlotPolicy.SavedMountState);
                if (slotRow != null) { Intent.Seed(Id, slotRow.Index, loadedWorld); Placed.Add(Id); }
                else if (mine.Any()) Intent.Seed(Id, MountedControlPlacementIntent.OffBar, loadedWorld);
                Frame();
                if (slotRow != null && Candidate && FactLive && Intent.AllowsSavedSlot(Id, slotRow.Index) &&
                    Slots[slotRow.Index] == null && Find() < 0)
                    Slots[slotRow.Index] = Control;
            }

            // A cold load: a new world (BeginLoadedWorld clears records), the bar as the native save left it (KMC slots
            // were blanked by the save scope), then the archive restore.
            internal Rider Load(List<SavedRow> rows)
            {
                var loaded = new Rider(Id, new MountedControlPlacementIntent(), new HashSet<string>(StringComparer.Ordinal))
                { Candidate = Candidate, OnRoster = OnRoster, PetId = PetId, PetResolves = PetResolves, Autofill = Autofill };
                for (var i = 0; i < Slots.Length; i++) if (Slots[i] != Control) loaded.Slots[i] = Slots[i];
                loaded.Restore(rows, true);
                return loaded;
            }
        }

        private static Rider NewRider(string id = "rider") =>
            new Rider(id, new MountedControlPlacementIntent(), new HashSet<string>(StringComparer.Ordinal));

        // Places the control by the native initial placement, then drags it to the chosen slot.
        private static Rider PlacedAt(int slot, string id = "rider")
        {
            var rider = NewRider(id);
            rider.Slots[0] = "Attack";
            rider.Frame();
            var at = rider.Find(); rider.Slots[at] = null; rider.Slots[slot] = Control;
            rider.Frame();
            return rider;
        }

        private static void SurvivesTransitions()
        {
            var rider = PlacedAt(9);
            rider.Slots[10] = "Spell";
            for (var visit = 0; visit < 3; visit++)
            {
                rider.AreaUnload();
                TestRunner.Equal(-1, rider.Find(), "The double did not model KMC emptying the slot at the area unload.");
                rider.Frame();
                TestRunner.Equal(9, rider.Find(), "The control did not return to slot 9 after area change " + visit + ".");
                rider.ToReserve(); rider.Frame(); rider.ToParty(); rider.Frame();
                TestRunner.Equal(9, rider.Find(), "The control did not return to slot 9 after the reserve roster " + visit + ".");
                rider.Save();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "A save scope moved or duplicated the control.");
            }
            TestRunner.True(rider.Slots[0] == "Attack" && rider.Slots[10] == "Spell", "An unrelated slot changed.");
        }

        private static void SurvivesReserveSaveLoad()
        {
            var rider = PlacedAt(12);
            rider.ToReserve(); rider.Frame();
            var rows = rider.Save();
            TestRunner.True(rows.Count(r => r.Kind == MountedControlSlotPolicy.SavedMountState && r.Index == 12) == 1 &&
                rows.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && rows.Count == 2,
                "A capital save did not carry the reserve rider's slot row and placement record exactly once.");
            var loaded = rider.Load(rows);
            TestRunner.Equal(-1, loaded.Find(), "A reserve rider's control appeared while it is still in reserve.");
            loaded.Frame(); loaded.ToParty(); loaded.Frame();
            TestRunner.True(loaded.Find() == 12 && loaded.Count() == 1, "Returning to the party after the load lost or duplicated slot 12.");
            // A save made in the field with the control live writes only its live row and record.
            var fieldRows = loaded.Save();
            TestRunner.True(fieldRows.Count == 2 && fieldRows.Any(r => r.Kind == 6 && r.Index == 12), "The live save rows changed.");
        }

        private static void RemovalSurvivesReserveSaveLoad()
        {
            var rider = PlacedAt(9);
            rider.Slots[9] = null; rider.Frame(); // the player removes the live control
            TestRunner.Equal(-1, rider.Find(), "A removed control was put back immediately.");
            rider.ToReserve(); rider.Frame();
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord,
                "A reserve rider's removal was not saved as one placement record without a slot row.");
            var loaded = rider.Load(rows);
            loaded.ToParty(); loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Autofill re-inserted a control the player had removed, after a city save and load.");
            loaded.AreaUnload(); loaded.Frame(); loaded.Save();
            TestRunner.Equal(-1, loaded.Find(), "A later transition re-added the removed control.");
        }

        private static void BoundaryEditIsAuthoritative()
        {
            // Move 9 -> 3 with no update between the edit and the save.
            var moved = PlacedAt(9);
            moved.Slots[9] = null; moved.Slots[3] = Control;
            var rows = moved.Save();
            TestRunner.True(moved.Find() == 3 && moved.Count() == 1 && moved.Slots[9] == null, "The bar after the save is not the moved arrangement.");
            TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The save did not record the moved slot.");
            TestRunner.Equal(3, moved.Load(rows).Find(), "Loading the save did not restore the moved slot.");
            // Remove with no update between the edit and the save.
            var removed = PlacedAt(9);
            removed.Slots[9] = null;
            var removedRows = removed.Save();
            TestRunner.Equal(-1, removed.Find(), "A removal just before the save was undone by the save scope.");
            TestRunner.True(removedRows.Count == 1 && removedRows[0].Kind == 7, "The save of a removal carried a slot row.");
            var removedLoaded = removed.Load(removedRows); removedLoaded.Frame();
            TestRunner.Equal(-1, removedLoaded.Find(), "Loading the save resurrected the removed control.");
        }

        private static void NativeInvalidation()
        {
            var rider = PlacedAt(9);
            rider.NativeInvalidate();
            TestRunner.Equal(-1, rider.Find(), "The double did not model native bad-slot cleanup.");
            rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The replacement fact did not return to slot 9 exactly once.");
            // Control: a continuously live control removed by the player is not re-added.
            rider.Slots[9] = null; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removal of a continuously live control was reversed.");
        }

        private static void ReplayNeverOverridesEdit()
        {
            // Rows captured when the pair was suspended (area transfer / retained remount custody).
            var rider = PlacedAt(9);
            var custody = new List<SavedRow> { new SavedRow { Actor = rider.Id, Index = 9, Kind = 6 } };
            // Forced dismount leaves the control live; the player removes it; the retained remount then replays.
            rider.Slots[9] = null; rider.Frame();
            rider.Restore(custody, false);
            TestRunner.Equal(-1, rider.Find(), "The retained remount resurrected a control removed while dismounted.");
            // Same for a move.
            var mover = PlacedAt(9, "mover");
            var moverCustody = new List<SavedRow> { new SavedRow { Actor = mover.Id, Index = 9, Kind = 6 } };
            mover.Slots[9] = null; mover.Slots[5] = Control; mover.Frame();
            mover.Restore(moverCustody, false);
            TestRunner.True(mover.Find() == 5 && mover.Count() == 1 && mover.Slots[9] == null, "The replay restored the old slot after a move.");
            // A replay for a rider with no arrangement yet still restores (the existing transfer behaviour).
            var fresh = NewRider("fresh"); fresh.Autofill = false; fresh.Frame();
            fresh.Restore(new List<SavedRow> { new SavedRow { Actor = "fresh", Index = 4, Kind = 6 } }, false);
            TestRunner.Equal(4, fresh.Find(), "A replay for a rider without an arrangement was refused.");
        }

        private static void PermanentLossInReserve()
        {
            var keeps = PlacedAt(9, "keeps");
            keeps.ToReserve(); keeps.Frame(); keeps.Frame();
            var keptRows = keeps.Save();
            TestRunner.True(keptRows.Any(r => r.Kind == 6 && r.Index == 9), "A reserve rider keeping its pet lost its slot row.");
            // The pet link is cleared while the rider is in reserve.
            var loses = PlacedAt(9, "loses");
            loses.ToReserve(); loses.Frame();
            loses.PetId = null; loses.Frame();
            TestRunner.True(loses.Save().Count == 0, "A rider that lost its pet in reserve still writes a restorable arrangement.");
            // A different companion later: the old arrangement is not reused.
            var replaces = PlacedAt(9, "replaces");
            replaces.ToReserve(); replaces.Frame();
            replaces.PetId = "horse-2"; replaces.Frame(); replaces.ToParty(); replaces.Frame();
            TestRunner.Equal(-1, replaces.Find(), "The old slot was reused for a different companion.");
            // A rider that cannot be resolved proves nothing: its record is kept.
            var intent = new MountedControlPlacementIntent();
            intent.ObserveBound("ghost", 7, "horse-1"); intent.ObserveAbsent("ghost", MountedControlAbsence.Temporary);
            TestRunner.True(intent.Revalidate("ghost", false, false, null) && intent.Has("ghost"), "An unresolved rider lost its arrangement.");
            TestRunner.True(!intent.Revalidate("ghost", true, false, "horse-1") && !intent.Has("ghost"), "A rider off every roster kept its arrangement.");
        }

        private static void OccupiedAndIndependent()
        {
            var intent = new MountedControlPlacementIntent();
            var placed = new HashSet<string>(StringComparer.Ordinal);
            var first = new Rider("first", intent, placed);
            var second = new Rider("second", intent, placed);
            first.Frame(); var a = first.Find(); first.Slots[a] = null; first.Slots[9] = Control; first.Frame();
            second.Frame(); var b = second.Find(); second.Slots[b] = null; second.Slots[2] = Control; second.Slots[9] = "Bow"; second.Frame();
            first.AreaUnload();
            first.Slots[9] = "Cure Light Wounds"; // the player filled the slot while the control was away
            first.Frame(); second.Frame();
            TestRunner.True(first.Slots[9] == "Cure Light Wounds" && first.Find() == -1, "An occupied slot was overwritten or the control moved elsewhere.");
            TestRunner.True(second.Find() == 2 && second.Slots[9] == "Bow", "Another rider's bar changed.");
            first.Slots[9] = null; first.AreaUnload(); first.Frame();
            TestRunner.Equal(-1, first.Find(), "A slot given up to another ability was used later.");
        }

        private static void NativeCleanupReactivated()
        {
            var rider = PlacedAt(9);
            rider.NativeInvalidate(reactivated: true);
            TestRunner.True(rider.Find() == -1 && rider.FactLive, "The double did not model an off/cleanup/on cycle before the update.");
            rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "A slot emptied by native cleanup was taken for a removal once the fact was back.");
            // Control: a manual removal on a continuously active fact stays removed.
            rider.Slots[9] = null; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A manual removal on a continuously active fact was reversed.");
        }

        private static void HeaderBeforeUpdate()
        {
            foreach (var reactivated in new[] { false, true })
            {
                var rider = PlacedAt(9);
                rider.NativeInvalidate(reactivated);
                var rows = rider.Save(); // the archive header runs before any control update
                TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                    "A save before the next update omitted the just-invalidated binding (reactivated=" + reactivated + ").");
                var loaded = rider.Load(rows); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "Cold restore did not return the control to slot 9 (reactivated=" + reactivated + ").");
            }
            var remover = PlacedAt(9);
            remover.Slots[9] = null; // live manual removal, no update before the save
            var removalRows = remover.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "A live manual removal saved a slot row.");
        }

        private static void DepartureEditIsAuthoritative()
        {
            var mover = PlacedAt(9);
            mover.Slots[9] = null; mover.Slots[3] = Control; // no update before the departure
            mover.ToReserve();
            var rows = mover.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The reserve save kept the old slot after a move.");
            mover.ToParty(); mover.Frame();
            TestRunner.True(mover.Find() == 3 && mover.Count() == 1 && mover.Slots[9] == null, "The returning rider did not get the moved slot.");
            var remover = PlacedAt(9, "remover");
            remover.Slots[9] = null;
            remover.ToReserve();
            var removalRows = remover.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "The reserve save of a removal carried a slot row.");
            remover.ToParty(); remover.Frame(); remover.Frame();
            TestRunner.Equal(-1, remover.Find(), "The removed control came back after the reserve roster.");
        }

        private static void UnresolvedPetKeepsArrangement()
        {
            var rider = PlacedAt(9);
            rider.PetResolves = false; // same PetUniqueId, the pet object or its reciprocal link unavailable this update
            rider.NativeInvalidate();
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The control stayed on the bar without a leaseable fact.");
            rider.PetResolves = true; rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The briefly unresolved pet lost the chosen slot.");
            // Confirmed permanent loss: the pet link is cleared while unresolved.
            var lost = PlacedAt(9, "lost");
            lost.PetResolves = false; lost.PetId = null; lost.NativeInvalidate(); lost.Frame();
            lost.PetId = "horse-1"; lost.PetResolves = true; lost.Frame(); lost.Frame();
            TestRunner.Equal(-1, lost.Find(), "A cleared pet link kept the old arrangement.");
        }

        private static void RemovalAfterLeaseRelease()
        {
            var rider = PlacedAt(9);
            rider.Save();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The save scope did not restore the leased slot.");
            rider.Slots[9] = null; // removed before the first control update after the scope
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removal right after the save scope was reversed.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The next save carried a slot row for a removed control.");
            var loaded = rider.Load(rows); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Loading resurrected the removed control.");
        }

        private static void AbsentRows()
        {
            var slot = MountedControlSlotPolicy.AbsentArrangementRows(9).ToArray();
            TestRunner.True(slot.Length == 2 && slot[0].Key == 9 && slot[0].Value == 6 && slot[1].Key == 0 && slot[1].Value == 7,
                "A remembered slot is not exactly a slot row and a placement record.");
            var offBar = MountedControlSlotPolicy.AbsentArrangementRows(MountedControlPlacementIntent.OffBar).ToArray();
            TestRunner.True(offBar.Length == 1 && offBar[0].Value == 7, "An off-bar arrangement is not a placement record alone.");
            var beyond = MountedControlSlotPolicy.AbsentArrangementRows(130).ToArray();
            TestRunner.True(beyond.Length == 1 && beyond[0].Value == 7, "A slot outside the saved bound produced an invalid row.");
            // The archive bound the validator applies: an exact fit is kept; 127 rows plus an absent rider's two rows are
            // refused by validation (the capture writes every required row and never truncates).
            TestRunner.True(MountedControlSlotPolicy.FitsArchive(MountedControlSlotPolicy.MaximumArchiveRows) &&
                MountedControlSlotPolicy.MaximumArchiveRows == 128, "The archive bound drifted or an exact fit is refused.");
            TestRunner.True(!MountedControlSlotPolicy.FitsArchive(127 + slot.Length), "127 rows plus an absent rider's arrangement fit the archive.");
            // Clearing (disable, disposal, a new load) leaves nothing to write.
            var intent = new MountedControlPlacementIntent();
            intent.ObserveBound("rider", 9, "horse-1"); intent.ObserveAbsent("rider", MountedControlAbsence.Temporary);
            intent.Clear();
            int index;
            TestRunner.True(!intent.TryGetArrangement("rider", out index), "A cleared arrangement is still written.");
        }
    }
}
