using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Toolbar persistence of the public Mount / Dismount control. The decisions are production code
    // (MountedControlPlacementIntent and MountedControlSlotPolicy). Service and Rider below are labeled test doubles of
    // NativeMountedControlService, the native bar and the player's rosters, following the service's event order:
    // - an update reconciles each candidate (ReconcileFacts replaces a missing/inactive desired fact first, a stale
    //   binding to older data of the public control is rebound, then ReconcileChosenSlot, then TryInitialPlacement), then
    //   the departure loop handles still-observed units that are no longer candidates (ObserveDeparture: current world
    //   only, bar observed, absence classified, slots emptied);
    // - native bad-slot cleanup reports the exact slot on the rider's bar (ObserveNativeSlotInvalidated) and empties it;
    // - a save runs the archive header capture first (ClassifyArrangementsForCapture, CaptureLiveSlots,
    //   CaptureAbsentIntent), then the save scope (boundary observation, leases, fact removal), its release (an update
    //   with the leases pending) and the lease restore (which records the rebind);
    // - a load replaces the world and the unit objects (BeginLoadedWorld clears the records; the earlier world's objects
    //   stay observed until cleanup), then RestorePersistentSlots(rows, loadedWorld: true).
    // Slot strings are AbilityData identities: "KMC_MountState#n" is the public control bound to fact data n.
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
            runner.Run("toolbar: native cleanup after a move or a first manual placement returns the cleaned slot, not an older one", NativeCleanupAfterMove);
            runner.Run("toolbar: a save header before the next update keeps a just-invalidated binding; a live removal saves a record only", HeaderBeforeUpdate);
            runner.Run("toolbar: a save header with a replacement fact and a stale slot keeps that slot and restores the new data once", HeaderStaleBinding);
            runner.Run("toolbar: a move or removal right before a roster departure is what the reserve keeps", DepartureEditIsAuthoritative);
            runner.Run("toolbar: a reserve save between the roster change and departure cleanup keeps the last arrangement", HeaderBeforeDepartureCleanup);
            runner.Run("toolbar: an earlier world's rider object never authors an arrangement in the loaded world", OutgoingWorldAuthorsNothing);
            runner.Run("toolbar: a party rider whose same pet link briefly does not resolve keeps its slot; a cleared link retires it", UnresolvedPetKeepsArrangement);
            runner.Run("toolbar: removing the shortcut right after a save scope returns it stays removed through save and load", RemovalAfterLeaseRelease);
        }

        private sealed class SavedRow { internal string Actor; internal int Index; internal int Kind; }

        private sealed class World { }

        private sealed class Service
        {
            internal readonly MountedControlPlacementIntent Intent = new MountedControlPlacementIntent();
            internal readonly HashSet<string> Placed = new HashSet<string>(StringComparer.Ordinal);
            internal World Current = new World();
            internal readonly List<Rider> Riders = new List<Rider>();
            // observedUnits with the world each was first reconciled in (observedWorlds).
            internal readonly List<Rider> Observed = new List<Rider>();

            internal IEnumerable<Rider> CurrentWorld => Riders.Where(r => r.World == Current);
            internal List<Rider> Candidates => CurrentWorld.Where(r => r.InParty).ToList();

            internal void Frame()
            {
                var candidates = Candidates;
                foreach (var rider in candidates) { rider.Reconcile(); if (!Observed.Contains(rider)) Observed.Add(rider); }
                foreach (var prior in Observed.Where(r => !candidates.Contains(r)).ToList())
                {
                    var recorded = ObserveDeparture(prior, candidates);
                    prior.KmcRemove(clearSlots: true, interrupt: recorded);
                    Observed.Remove(prior);
                }
                foreach (var reserve in CurrentWorld.Where(r => !r.InParty)) Intent.Revalidate(reserve.Id, true, reserve.OnRoster, reserve.PetId);
            }

            internal bool ObserveDeparture(Rider prior, List<Rider> candidates)
            {
                if (prior.World != Current || candidates.Any(c => c.Id == prior.Id)) return false;
                prior.ObserveBar();
                Intent.ObserveAbsent(prior.Id, prior.OnRoster ? MountedControlAbsence.Temporary : MountedControlAbsence.Permanent);
                return true;
            }

            // A save in production order; returns the owned archive rows.
            internal List<SavedRow> Save()
            {
                var candidates = Candidates;
                foreach (var prior in Observed.Where(r => !candidates.Contains(r)).ToList()) ObserveDeparture(prior, candidates);
                foreach (var rider in candidates) rider.Classify();
                var rows = new List<SavedRow>();
                var written = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rider in candidates) rider.CaptureLive(rows, written);
                foreach (var id in Intent.ActorIds.OrderBy(i => i, StringComparer.Ordinal).ToArray())
                {
                    int index;
                    if (written.Contains(id) || CurrentWorld.Count(r => r.Id == id) != 1) continue;
                    var owner = CurrentWorld.Single(r => r.Id == id);
                    if (!owner.InParty && !Intent.Revalidate(id, true, owner.OnRoster, owner.PetId)) continue;
                    if (!Intent.TryGetArrangement(id, out index)) continue;
                    foreach (var r in MountedControlSlotPolicy.AbsentArrangementRows(index))
                        if (r.Value != MountedControlSlotPolicy.SavedPlacementRecord || !rows.Any(x => x.Actor == id && x.Kind == r.Value))
                            rows.Add(new SavedRow { Actor = id, Index = r.Key, Kind = r.Value });
                }
                // The save scope over candidates and still-observed units.
                foreach (var rider in candidates) Intent.ObserveAtBoundary(rider.Id, rider.FactLive, rider.Find(), Placed.Contains(rider.Id), rider.PetId);
                var scoped = candidates.Concat(Observed).Distinct().ToList();
                foreach (var rider in scoped) rider.Lease();
                foreach (var rider in scoped) rider.KmcRemove(clearSlots: false, interrupt: rider.World == Current);
                Frame();
                foreach (var rider in scoped) rider.ReleaseLeases();
                return rows;
            }

            // A cold load of rows into a new world; the riders of the new world are added by the caller first.
            internal void Load(World incoming, List<SavedRow> rows)
            {
                Current = incoming;
                Intent.Clear(); Placed.Clear();
                foreach (var rider in CurrentWorld.ToList()) rider.Restore(rows, true);
                Frame();
                foreach (var rider in CurrentWorld.ToList()) rider.WriteRestoredSlot(rows);
            }
        }

        private sealed class Rider
        {
            internal readonly string Id;
            internal readonly Service S;
            internal readonly World World;
            internal readonly string[] Slots = new string[140];
            internal bool InParty = true, OnRoster = true, FactLive, Autofill = true;
            internal string PetId = "horse-1";
            internal bool PetResolves = true;
            internal int DataVersion = 1;
            private readonly List<int> leases = new List<int>();

            internal Rider(string id, Service service, World world) { Id = id; S = service; World = world; S.Riders.Add(this); }

            internal MountedControlPlacementIntent Intent => S.Intent;
            internal string Data => Control + "#" + DataVersion;
            internal bool Desired => PetId != null && PetResolves;
            internal int Find() => Array.FindIndex(Slots, s => s != null && s.StartsWith(Control, StringComparison.Ordinal));
            internal int Count() => Slots.Count(s => s != null && s.StartsWith(Control, StringComparison.Ordinal));

            internal void Frame() => S.Frame();

            // One candidate's reconciliation in ReconcileFacts.
            internal void Reconcile()
            {
                var replaced = false;
                if (Desired && !FactLive) { FactLive = true; DataVersion++; replaced = true; }
                if (!Desired) FactLive = false;
                var bound = Find();
                if (FactLive && bound >= 0 && Slots[bound] != Data) Slots[bound] = Data; // stale binding rebound
                var index = Intent.Reconcile(Id, FactLive, Desired, replaced, Find(), i => Slots[i] == null,
                    leases.Count != 0, S.Placed.Contains(Id), PetId);
                if (index >= 0) { Slots[index] = Data; S.Placed.Add(Id); Intent.ObserveBound(Id, index, PetId); }
                if (FactLive && !S.Placed.Contains(Id) && Autofill)
                {
                    S.Placed.Add(Id);
                    var empty = Enumerable.Range(0, 14).FirstOrDefault(i => Slots[i] == null);
                    if (Slots[empty] == null && Find() < 0) Slots[empty] = Data;
                }
            }

            internal void ObserveBar() => Intent.ObserveAtBoundary(Id, FactLive, Find(), S.Placed.Contains(Id), PetId);

            // ClassifyArrangementsForCapture for a candidate.
            internal void Classify()
            {
                ObserveBar();
                if (FactLive)
                {
                    var at = Find();
                    if (at >= 0 && Slots[at] != Data) Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                    return;
                }
                if (Desired || Intent.Revalidate(Id, true, true, PetId)) Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
            }

            // CaptureLiveSlots: only callable live data is written.
            internal void CaptureLive(List<SavedRow> rows, HashSet<string> written)
            {
                if (!FactLive) return;
                var at = Find();
                if (at >= 0 && Slots[at] == Data) { rows.Add(new SavedRow { Actor = Id, Index = at, Kind = MountedControlSlotPolicy.SavedMountState }); written.Add(Id); }
                if (S.Placed.Contains(Id)) rows.Add(new SavedRow { Actor = Id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
            }

            internal void Lease() { var at = Find(); if (at >= 0) { leases.Add(at); Slots[at] = null; } }

            internal void ReleaseLeases()
            {
                foreach (var at in leases)
                    if (Slots[at] == null && FactLive && World == S.Current && InParty)
                    { Slots[at] = Data; Intent.ObserveBound(Id, at, PetId); }
                leases.Clear();
            }

            // RemoveManagedFacts: facts removed, the absence recorded for the current world, KMC slots emptied if asked.
            internal void KmcRemove(bool clearSlots, bool interrupt)
            {
                FactLive = false;
                if (interrupt) Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                if (clearSlots) { var at = Find(); if (at >= 0) Slots[at] = null; }
            }

            // Native bad-slot cleanup: IsBad answered true for the slot holding the control (the production postfix
            // reports that exact index), the slot is emptied; reactivated: the fact is active again before the update.
            internal void NativeInvalidate(bool reactivated = false)
            {
                var at = Find();
                if (at >= 0) { Intent.ObserveNativeSlotInvalidated(Id, at, PetId); Slots[at] = null; }
                FactLive = reactivated;
            }

            internal void AreaUnload()
            {
                ObserveBar();
                KmcRemove(clearSlots: true, interrupt: true);
            }

            // The capital moves the rider to the reserve roster (roster change only); departure cleanup runs on the next update.
            internal void LeaveParty() { InParty = false; }
            internal void ToReserve() { LeaveParty(); Frame(); }
            internal void ToParty() { InParty = true; }

            internal List<SavedRow> Save() => S.Save();

            internal void Restore(List<SavedRow> rows, bool loadedWorld)
            {
                var mine = rows.Where(r => r.Actor == Id).ToList();
                if (mine.Any(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord)) S.Placed.Add(Id);
                var slotRow = mine.FirstOrDefault(r => r.Kind == MountedControlSlotPolicy.SavedMountState);
                if (slotRow != null) { Intent.Seed(Id, slotRow.Index, loadedWorld); S.Placed.Add(Id); }
                else if (mine.Any()) Intent.Seed(Id, MountedControlPlacementIntent.OffBar, loadedWorld);
            }

            internal void WriteRestoredSlot(List<SavedRow> rows)
            {
                var slotRow = rows.FirstOrDefault(r => r.Actor == Id && r.Kind == MountedControlSlotPolicy.SavedMountState);
                if (slotRow != null && InParty && FactLive && Intent.AllowsSavedSlot(Id, slotRow.Index) && Slots[slotRow.Index] == null && Find() < 0)
                    Slots[slotRow.Index] = Data;
            }

            // An in-world replay (area transfer, retained remount).
            internal void Replay(List<SavedRow> rows) { Restore(rows, false); Frame(); WriteRestoredSlot(rows); }

            // A cold load into a new world with a new object for this rider (same id); the native save blanked KMC slots.
            internal Rider Load(List<SavedRow> rows)
            {
                var incoming = new World();
                var loaded = new Rider(Id, S, incoming) { InParty = InParty, OnRoster = OnRoster, PetId = PetId, PetResolves = PetResolves, Autofill = Autofill };
                for (var i = 0; i < Slots.Length; i++) if (Slots[i] == null || !Slots[i].StartsWith(Control, StringComparison.Ordinal)) loaded.Slots[i] = Slots[i];
                S.Load(incoming, rows);
                return loaded;
            }
        }

        private static Rider Rider0(string id, Service service)
        {
            var rider = new Rider(id, service, service.Current);
            return rider;
        }

        // Places the control by the native initial placement, then drags it to the chosen slot.
        private static Rider PlacedAt(int slot, string id = "rider", Service service = null)
        {
            service = service ?? new Service();
            var rider = Rider0(id, service);
            rider.Slots[0] = "Attack";
            rider.Frame();
            var at = rider.Find(); var data = rider.Slots[at]; rider.Slots[at] = null; rider.Slots[slot] = data;
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
                rider.ToReserve(); rider.ToParty(); rider.Frame();
                TestRunner.Equal(9, rider.Find(), "The control did not return to slot 9 after the reserve roster " + visit + ".");
                rider.Save();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "A save scope moved or duplicated the control.");
            }
            TestRunner.True(rider.Slots[0] == "Attack" && rider.Slots[10] == "Spell", "An unrelated slot changed.");
        }

        private static void SurvivesReserveSaveLoad()
        {
            var rider = PlacedAt(12);
            rider.ToReserve();
            var rows = rider.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 12) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                "A capital save did not carry the reserve rider's slot row and placement record exactly once.");
            var loaded = rider.Load(rows);
            TestRunner.Equal(-1, loaded.Find(), "A reserve rider's control appeared while it is still in reserve.");
            loaded.ToParty(); loaded.Frame();
            TestRunner.True(loaded.Find() == 12 && loaded.Count() == 1, "Returning to the party after the load lost or duplicated slot 12.");
            var fieldRows = loaded.Save();
            TestRunner.True(fieldRows.Count == 2 && fieldRows.Any(r => r.Kind == 6 && r.Index == 12), "The live save rows changed.");
        }

        private static void RemovalSurvivesReserveSaveLoad()
        {
            var rider = PlacedAt(9);
            rider.Slots[9] = null; rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removed control was put back immediately.");
            rider.ToReserve();
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "A reserve rider's removal was not saved as one placement record.");
            var loaded = rider.Load(rows);
            loaded.ToParty(); loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Autofill re-inserted a control the player had removed, after a city save and load.");
            loaded.AreaUnload(); loaded.Frame(); loaded.Save();
            TestRunner.Equal(-1, loaded.Find(), "A later transition re-added the removed control.");
        }

        private static void BoundaryEditIsAuthoritative()
        {
            var moved = PlacedAt(9);
            var data = moved.Slots[9]; moved.Slots[9] = null; moved.Slots[3] = data;
            var rows = moved.Save();
            TestRunner.True(moved.Find() == 3 && moved.Count() == 1 && moved.Slots[9] == null, "The bar after the save is not the moved arrangement.");
            TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The save did not record the moved slot.");
            TestRunner.Equal(3, moved.Load(rows).Find(), "Loading the save did not restore the moved slot.");
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
            rider.Slots[9] = null; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removal of a continuously live control was reversed.");
        }

        private static void ReplayNeverOverridesEdit()
        {
            var rider = PlacedAt(9);
            var custody = new List<SavedRow> { new SavedRow { Actor = rider.Id, Index = 9, Kind = 6 } };
            rider.Slots[9] = null; rider.Frame();
            rider.Replay(custody);
            TestRunner.Equal(-1, rider.Find(), "The retained remount resurrected a control removed while dismounted.");
            var mover = PlacedAt(9, "mover");
            var moverCustody = new List<SavedRow> { new SavedRow { Actor = mover.Id, Index = 9, Kind = 6 } };
            var data = mover.Slots[9]; mover.Slots[9] = null; mover.Slots[5] = data; mover.Frame();
            mover.Replay(moverCustody);
            TestRunner.True(mover.Find() == 5 && mover.Count() == 1 && mover.Slots[9] == null, "The replay restored the old slot after a move.");
            var fresh = Rider0("fresh", new Service()); fresh.Autofill = false; fresh.Frame();
            fresh.Replay(new List<SavedRow> { new SavedRow { Actor = "fresh", Index = 4, Kind = 6 } });
            TestRunner.Equal(4, fresh.Find(), "A replay for a rider without an arrangement was refused.");
        }

        private static void PermanentLossInReserve()
        {
            var keeps = PlacedAt(9, "keeps");
            keeps.ToReserve(); keeps.Frame();
            TestRunner.True(keeps.Save().Any(r => r.Kind == 6 && r.Index == 9), "A reserve rider keeping its pet lost its slot row.");
            var loses = PlacedAt(9, "loses");
            loses.ToReserve();
            loses.PetId = null; loses.Frame();
            TestRunner.True(loses.Save().Count == 0, "A rider that lost its pet in reserve still writes a restorable arrangement.");
            var replaces = PlacedAt(9, "replaces");
            replaces.ToReserve();
            replaces.PetId = "horse-2"; replaces.Frame(); replaces.ToParty(); replaces.Frame();
            TestRunner.Equal(-1, replaces.Find(), "The old slot was reused for a different companion.");
            var intent = new MountedControlPlacementIntent();
            intent.ObserveBound("ghost", 7, "horse-1"); intent.ObserveAbsent("ghost", MountedControlAbsence.Temporary);
            TestRunner.True(intent.Revalidate("ghost", false, false, null) && intent.Has("ghost"), "An unresolved rider lost its arrangement.");
            TestRunner.True(!intent.Revalidate("ghost", true, false, "horse-1") && !intent.Has("ghost"), "A rider off every roster kept its arrangement.");
        }

        private static void OccupiedAndIndependent()
        {
            var service = new Service();
            var first = PlacedAt(9, "first", service);
            var second = Rider0("second", service);
            second.Frame(); var b = second.Find(); var data = second.Slots[b]; second.Slots[b] = null; second.Slots[2] = data; second.Slots[9] = "Bow"; second.Frame();
            first.AreaUnload();
            first.Slots[9] = "Cure Light Wounds";
            first.Frame();
            TestRunner.True(first.Slots[9] == "Cure Light Wounds" && first.Find() == -1, "An occupied slot was overwritten or the control moved elsewhere.");
            TestRunner.True(second.Find() == 2 && second.Slots[9] == "Bow", "Another rider's bar changed.");
            first.Slots[9] = null; first.AreaUnload(); first.Frame();
            TestRunner.Equal(-1, first.Find(), "A slot given up to another ability was used later.");
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
            TestRunner.True(MountedControlSlotPolicy.FitsArchive(MountedControlSlotPolicy.MaximumArchiveRows) &&
                MountedControlSlotPolicy.MaximumArchiveRows == 128, "The archive bound drifted or an exact fit is refused.");
            TestRunner.True(!MountedControlSlotPolicy.FitsArchive(127 + slot.Length), "127 rows plus an absent rider's arrangement fit the archive.");
            var intent = new MountedControlPlacementIntent();
            intent.ObserveBound("rider", 9, "horse-1"); intent.ObserveAbsent("rider", MountedControlAbsence.Temporary);
            intent.Clear();
            int index;
            TestRunner.True(!intent.TryGetArrangement("rider", out index), "A cleared arrangement is still written.");
        }

        private static void NativeCleanupReactivated()
        {
            var rider = PlacedAt(9);
            rider.NativeInvalidate(reactivated: true);
            TestRunner.True(rider.Find() == -1 && rider.FactLive, "The double did not model an off/cleanup/on cycle before the update.");
            rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "A slot emptied by native cleanup was taken for a removal once the fact was back.");
            rider.Slots[9] = null; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A manual removal on a continuously active fact was reversed.");
        }

        private static void NativeCleanupAfterMove()
        {
            var mover = PlacedAt(9);
            var data = mover.Slots[9]; mover.Slots[9] = null; mover.Slots[3] = data; // moved, no update yet
            mover.NativeInvalidate(reactivated: true);
            mover.Frame(); mover.Frame();
            TestRunner.True(mover.Find() == 3 && mover.Count() == 1 && mover.Slots[9] == null, "Native cleanup after a move restored the older slot.");
            // A first manual placement (no automatic placement, no record yet), cleaned before any update.
            var manual = Rider0("manual", new Service()); manual.Autofill = false; manual.Frame();
            TestRunner.Equal(-1, manual.Find(), "The fixture placed the control automatically.");
            manual.Slots[5] = manual.Data; // dragged from the ability drawer
            manual.NativeInvalidate(reactivated: true);
            manual.Frame(); manual.Frame();
            TestRunner.True(manual.Find() == 5 && manual.Count() == 1, "A first manual placement cleaned by native was lost.");
            // Control: a manual removal on a continuously active fact stays removed.
            manual.Slots[5] = null; manual.Frame(); manual.Frame();
            TestRunner.Equal(-1, manual.Find(), "A manual removal on a continuously active fact was reversed.");
        }

        private static void HeaderBeforeUpdate()
        {
            foreach (var reactivated in new[] { false, true })
            {
                var rider = PlacedAt(9);
                rider.NativeInvalidate(reactivated);
                var rows = rider.Save();
                TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                    "A save before the next update omitted the just-invalidated binding (reactivated=" + reactivated + ").");
                var loaded = rider.Load(rows); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "Cold restore did not return the control to slot 9 (reactivated=" + reactivated + ").");
            }
            var remover = PlacedAt(9);
            remover.Slots[9] = null;
            var removalRows = remover.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "A live manual removal saved a slot row.");
        }

        private static void HeaderStaleBinding()
        {
            var rider = PlacedAt(9);
            var old = rider.Slots[9];
            rider.DataVersion++; // the fact was replaced: new AbilityData, active, slot 9 still holds the old data
            TestRunner.True(rider.FactLive && rider.Slots[9] == old && old != rider.Data, "The fixture does not hold distinct old and new data.");
            var rows = rider.Save(); // header before ReconcilePublicControlSlots rebinds slot 9
            TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                "The header omitted a stale but recoverable slot.");
            var loaded = rider.Load(rows); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[9] == loaded.Data, "Cold restore did not bind the current data at 9 once.");
            var remover = PlacedAt(9);
            remover.DataVersion++; remover.Slots[9] = null; // deliberate removal
            var removalRows = remover.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "A deliberate removal saved a slot row.");
        }

        private static void DepartureEditIsAuthoritative()
        {
            var mover = PlacedAt(9);
            var data = mover.Slots[9]; mover.Slots[9] = null; mover.Slots[3] = data;
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

        private static void HeaderBeforeDepartureCleanup()
        {
            // Unchanged slot 9: only the roster membership changes, then the header runs before any update.
            var stays = PlacedAt(9);
            stays.LeaveParty();
            var rows = stays.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                "A reserve save before departure cleanup omitted slot 9.");
            var stayLoaded = stays.Load(rows); stayLoaded.ToParty(); stayLoaded.Frame();
            TestRunner.True(stayLoaded.Find() == 9 && stayLoaded.Count() == 1, "The cold return lost slot 9.");
            // A move to 3 just before the roster change.
            var mover = PlacedAt(9, "mover");
            var data = mover.Slots[9]; mover.Slots[9] = null; mover.Slots[3] = data;
            mover.LeaveParty();
            var moverRows = mover.Save();
            TestRunner.True(moverRows.Count(r => r.Kind == 6) == 1 && moverRows.Single(r => r.Kind == 6).Index == 3, "The header kept the older slot after a move.");
            var moverLoaded = mover.Load(moverRows); moverLoaded.ToParty(); moverLoaded.Frame();
            TestRunner.True(moverLoaded.Find() == 3 && moverLoaded.Count() == 1, "The cold return did not get the moved slot.");
            // A deliberate removal just before the roster change.
            var remover = PlacedAt(9, "remover");
            remover.Slots[9] = null;
            remover.LeaveParty();
            var removerRows = remover.Save();
            TestRunner.True(removerRows.Count == 1 && removerRows[0].Kind == 7, "The header saved a slot row for a removal.");
            var removerLoaded = remover.Load(removerRows); removerLoaded.ToParty(); removerLoaded.Frame(); removerLoaded.Frame();
            TestRunner.Equal(-1, removerLoaded.Find(), "The cold return resurrected the removed control.");
        }

        private static void OutgoingWorldAuthorsNothing()
        {
            foreach (var offBarRecord in new[] { false, true })
            {
                var service = new Service();
                var outgoing = PlacedAt(9, "rider", service); // bound at 9 in the outgoing world, still observed
                var incoming = new World();
                var reserve = new Rider("rider", service, incoming) { InParty = false, Autofill = false };
                var rows = offBarRecord ? new List<SavedRow> { new SavedRow { Actor = "rider", Index = 0, Kind = 7 } } : new List<SavedRow>();
                service.Load(incoming, rows);
                TestRunner.True(!service.Observed.Contains(outgoing), "The outgoing object was not cleaned up.");
                var saved = service.Save();
                TestRunner.True(!saved.Any(r => r.Kind == 6), "Saving again reproduced the outgoing slot (offBarRecord=" + offBarRecord + ").");
                TestRunner.True(saved.Count == (offBarRecord ? 1 : 0), "The loaded world's rows changed (offBarRecord=" + offBarRecord + ").");
                reserve.ToParty(); service.Frame(); service.Frame();
                TestRunner.Equal(-1, reserve.Find(), "Returning the rider reproduced the outgoing choice (offBarRecord=" + offBarRecord + ").");
            }
        }

        private static void UnresolvedPetKeepsArrangement()
        {
            var rider = PlacedAt(9);
            rider.PetResolves = false;
            rider.NativeInvalidate();
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The control stayed on the bar without a leaseable fact.");
            rider.PetResolves = true; rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The briefly unresolved pet lost the chosen slot.");
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
            rider.Slots[9] = null;
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removal right after the save scope was reversed.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The next save carried a slot row for a removed control.");
            var loaded = rider.Load(rows); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Loading resurrected the removed control.");
        }
    }
}
