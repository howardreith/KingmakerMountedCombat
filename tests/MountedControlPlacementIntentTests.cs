using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            runner.Run("toolbar: a save header while the pet briefly does not resolve keeps the live slot; a removal saves a record only", HeaderUnresolvedPet);
            runner.Run("toolbar: a removal made just before the fact lease is lost stays removed when the same pet returns", RemovalBeforeLeaseLoss);
            runner.Run("toolbar: confirmed pet loss clears an uninspected public slot; a different pet cannot revive it", PermanentLossClearsSlot);
            runner.Run("toolbar: area suspension classifies a departed rider's recent move or removal before emptying its bar", SuspensionObservesDeparted);
            runner.Run("toolbar: observations while a selected load's outgoing world is still current write nothing", LoadFenceProductionOrder);
            runner.Run("toolbar: a removal stays removed when native deactivates the fact before the next update", RemovalThenDeactivation);
            runner.Run("toolbar: a direct change of pets retires the old slot in update-first and save-first order", DirectPetChange);
            runner.Run("toolbar: an abandoned load returns reserve slots and deliberate removals the bar cannot reconstruct", AbandonedLoadRestores);
            runner.Run("toolbar: a superseded load releases only its own fence; an unstarted replacement leaves controls running", SupersededLoadFence);
            runner.Run("toolbar: native cleanup with a cleared pet link, then a new pet before any update, retires the old slot", ClearedLinkCleanupThenNewPet);
            runner.Run("toolbar: native cleanup during a load that is abandoned returns the slot in the surviving world", CleanupDuringAbandonedLoad);
            runner.Run("toolbar: a pet link cleared at area suspension, then a new pet before resume, leaves the old slot retired", SuspensionClearedLinkThenNewPet);
            runner.Run("toolbar: an area replay captured before a change of pets never gives the old slot to the new pet", ReplayAfterPetChangeDuringSuspension);
            runner.Run("toolbar: an older capital save's surviving reserve binding is saved as its slot, never serialized, and cleared on disable", OlderCapitalSaveReserveBinding);
            runner.Run("toolbar: an older reserve bar keeps the public slot over a legacy alias and migrates its Mounted Charge to stock Charge", OlderReserveBarMigration);
            runner.Run("toolbar: native cleanup while a faulted relationship still grants Dismount with an empty pet link keeps the slot", FaultedRiderKeepsInterruptedSlot);
            runner.Run("toolbar: before full registration only an identifiable KMC blueprint is cleaned up; items and ordinary abilities survive", UnregisteredCleanupKeepsUnrelatedSlots);
            runner.Run("toolbar: a save-scope lease does not restore a slot retired for a companion replaced during the scope", LeaseReleaseAfterCompanionChange);
            runner.Run("toolbar: reserve scan bookkeeping releases removed units and a disposed world, and never repeats a scan", ReserveScanLedgerLifetime);
            runner.Run("toolbar: a party rider's older alias survives a temporarily unresolved pet in update-first and save-first order", LegacyAliasWhilePetUnresolved);
            runner.Run("toolbar: saving a live older alias before migration writes the chosen index once with unique archive keys", LiveAliasSaveFirst);
            runner.Run("toolbar: a leased older alias is not restored for a companion that replaced the old one during the scope", AliasLeaseAfterCompanionChange);
            runner.Run("toolbar: native cleanup of an inactive older alias before any update keeps its index for the same pet", AliasNativeCleanupFirst);
            runner.Run("toolbar: a mixed bar saved first (live alias at 3, canonical at 9, current or stale) restores one control at 9", MixedBarSaveFirst);
            runner.Run("toolbar: a live alias with the canonical fact inactive saves its binding and its placement record", LiveAliasCanonicalInactive);
            runner.Run("toolbar: removing an alias the save scope returned, before the next update, stays removed through save and load", AliasRemovedAfterLeaseRelease);
            runner.Run("toolbar: a stale canonical binding before a current one is the slot the save carries and the load restores", StaleThenCurrentCanonical);
            runner.Run("toolbar: a second save keeps an untouched returned alias in the world and the archive; a removal before it stays", AliasSecondSave);
            runner.Run("toolbar: replacing the companion while a returned alias waits for migration retires it in both orders", AliasCompanionReplacement);
            runner.Run("toolbar: a native rebind of a stale canonical slot is the live arrangement; emptying still returns the slot", NativeRebindOutcome);
            runner.Run("toolbar: native emptying of a returned, recorded alias interrupts the placement; the same pet gets it back at 9", ReturnedAliasNativeEmptying);
            runner.Run("toolbar: a save before the first update never autofills a second copy of a manual placement", SaveReleaseNoAutofillCopy);
            runner.Run("toolbar: a remembered slot occupied by another live binding is saved off the bar with unique archive keys", RememberedSlotOccupied);
            runner.Run("toolbar: deferred native cleanup from a replaced world is discarded; an abandoned load still applies it", DeferredCleanupAcrossWorlds);
            runner.Run("toolbar: native emptying of a first manual shortcut satisfies placement; a filled slot gets no autofill copy", FirstManualCleanupNoAutofill);
            runner.Run("toolbar: one cleanup pass emptying a canonical slot then an older alias keeps the canonical index", MixedCleanupPassKeepsCanonical);
            runner.Run("toolbar: a slot given up as occupied at a save stays given up in the continuing game and after reload", OccupiedSlotGivenUpEverywhere);
            runner.Run("toolbar: a disabled service arms no load fence, keeps no outgoing world and records no native cleanup", DisabledLifecycle);
            runner.Run("toolbar: a native rebind during a fenced load is the live state an abandoned load returns", FencedRebindAbandoned);
            runner.Run("toolbar: a save between native reads of a canonical slot and an older alias keeps the canonical index", SaveBetweenNativeReads);
            runner.Run("toolbar: cleanup of two canonical bindings keeps the first, in one pass or slot by slot in either order", TwoCanonicalCleanup);
            runner.Run("toolbar: a save while disabled sanitizes without recording intent; another world starts with its own", DisabledSaveIsolation);
            runner.Run("toolbar: a surviving older alias never replaces an interrupted canonical choice through migration or capture", SurvivingAliasAfterCanonicalCleanup);
            runner.Run("toolbar: a save or a return between reads of two canonical copies keeps the first canonical index", SurvivingCanonicalCopyAfterCleanup);
            runner.Run("toolbar: a first automatic placement records its companion; a direct change of companions retires it", InitialPlacementCompanionChange);
            runner.Run("toolbar: a native rebind of the surviving older alias keeps the interrupted canonical choice", AliasRebindAfterCanonicalCleanup);
            runner.Run("toolbar: a live canonical copy present when the chosen slot was emptied never changes the saved slot", LiveCopyAfterCanonicalCleanup);
            runner.Run("toolbar: an occupied chosen slot gives the surviving copy the same arrangement live and in the save", OccupiedChosenSlotWithCopy);
            runner.Run("toolbar: a standalone IsBad query records nothing; a later removal stays removed", StandaloneIsBadQuery);
            runner.Run("toolbar: a fenced rebind after a change of companions stays retired when the load is abandoned", FencedRebindCompanionChange);
            runner.Run("toolbar: a new placement in a former copy's slot after native cleanup is the player's", NewPlacementInFormerCopySlot);
            runner.Run("toolbar: native cleanup of the fallback copy while the chosen slot is occupied keeps the fallback", FallbackCleanupWithOccupiedChoice);
            runner.Run("toolbar: reports left by an aborted native operation are dropped; a later removal stays removed", AbortedNativeOperation);
            runner.Run("toolbar: a cleanup batch for a retired rider cannot recreate the arrangement, fenced or not", RetiredRiderCleanupBatch);
            runner.Run("toolbar: repair removes every surviving canonical copy and leaves one shortcut at the chosen slot", RepairRemovesAllCopies);
            runner.Run("toolbar: emptying a secondary legacy alias is never attributed to the selected alias", SecondaryAliasReport);
            runner.Run("toolbar: copies read out of index order (3, 9, 7) leave a later placement at 9 the player's", OutOfOrderCopyReads);
            runner.Run("toolbar: an abandoned load keeps the fallback native emptied while the outgoing chosen slot is occupied", AbandonedLoadFallbackOccupied);
            runner.Run("toolbar: an aborted native operation never absorbs a later successful one or a write it completed", ExceptionalNativeOperations);
            runner.Run("toolbar: an occupied chosen slot keeps its occupant and exactly one of two surviving copies", OccupiedChosenSlotWithTwoCopies);
            runner.Run("toolbar: KMC's own alias migration while the pet is unresolved keeps the fallback for an occupied slot", FallbackBeforeOwnMigration);
            runner.Run("toolbar: a move past a surviving lower copy is the arrangement live, in the save and after reload", MoveBeyondSurvivingCopy);
            runner.Run("toolbar: native emptying of a new placement before any update keeps that placement, after retirement too", NativeEmptiesNewPlacement);
            runner.Run("toolbar: a departed rider's copy slots are released in the same world; its return restores the chosen index once", DepartedRiderReleasesCopies);
        }

        private sealed class SavedRow { internal string Actor; internal int Index; internal int Kind; }

        private sealed class World { }

        private sealed class Service
        {
            internal readonly MountedControlPlacementIntent Intent = new MountedControlPlacementIntent();
            internal readonly MountedControlLoadFence Fence = new MountedControlLoadFence();
            internal readonly HashSet<string> Placed = new HashSet<string>(StringComparer.Ordinal);
            internal World Current = new World();
            internal bool Accepts => Fence.Accepts(Current);
            internal readonly List<Rider> Riders = new List<Rider>();
            // observedUnits with the world each was first reconciled in (observedWorlds).
            internal readonly List<Rider> Observed = new List<Rider>();

            internal IEnumerable<Rider> CurrentWorld => Riders.Where(r => r.World == Current);
            internal List<Rider> Candidates => CurrentWorld.Where(r => r.InParty).ToList();

            // AdmitReserveBindings: once per rider object, a never-observed reserve rider's surviving public binding (an
            // older save) is recovered when its pet link names a companion; its KMC bindings are cleared either way.
            internal readonly HashSet<Rider> Scanned = new HashSet<Rider>();
            internal IEnumerable<Rider> Reserve => CurrentWorld.Where(r => !r.InParty && r.OnRoster);
            internal void AdmitReserve()
            {
                if (!Enabled || !Accepts) return;
                foreach (var reserve in Reserve.ToList())
                {
                    if (Observed.Contains(reserve) || !Scanned.Add(reserve)) continue;
                    if (reserve.KmcCount() == 0) continue;
                    var kinds = reserve.Slots.Select(KindOf).ToArray();
                    var at = MountedControlSlotPolicy.RecoveredPublicSlot(kinds);
                    if (at >= 0 && Intent.AdmitSurvivingBinding(reserve.Id, at, reserve.PetId)) Placed.Add(reserve.Id);
                    // Every KMC binding goes; an older Mounted Charge becomes the stock Charge when available and absent.
                    for (var i = 0; i < kinds.Length; i++)
                    {
                        if (kinds[i] == NativeMountedControlKind.None) continue;
                        reserve.Slots[i] = MountedControlSlotPolicy.RepairLiveSlot(kinds[i]) == MountedControlSlotRepair.StockCharge
                            ? reserve.ResolveStockCharge() : null;
                    }
                }
            }

            // Slot strings: "KMC_MountState#n" the public control, "KMC_Mount#n" / "KMC_Dismount#n" older aliases,
            // "KMC_Charge#n" the older Mounted Charge, "Charge" the rider's stock Charge.
            internal static NativeMountedControlKind KindOf(string slot)
            {
                if (slot == null) return NativeMountedControlKind.None;
                if (slot.StartsWith(Control, StringComparison.Ordinal)) return NativeMountedControlKind.MountState;
                if (slot.StartsWith("KMC_Mount#", StringComparison.Ordinal)) return NativeMountedControlKind.MountCompanion;
                if (slot.StartsWith("KMC_Dismount#", StringComparison.Ordinal)) return NativeMountedControlKind.Dismount;
                if (slot.StartsWith("KMC_Charge#", StringComparison.Ordinal)) return NativeMountedControlKind.MountedCharge;
                return NativeMountedControlKind.None;
            }

            // PendingSlotInvalidation: one native bad-slot report.
            internal sealed class NativeReport
            {
                internal World World; internal Rider Rider; internal int Index; internal string Slot;
                internal NativeMountedControlKind Kind; internal bool Alias, Copy, ChosenFree, Desired; internal string PetId;
                internal List<KeyValuePair<int, string>> CopySlots;
            }

            // The production observation scope of one native slot operation (nativeSlotScope; the owner is the rider's bar,
            // the frame Time.frameCount, advanced by each update here) and the riders a report of the current operation
            // retired (retiredInNativeOperation).
            internal readonly MountedNativeSlotScope<NativeReport> Scope = new MountedNativeSlotScope<NativeReport>();
            internal int FrameNo;
            private readonly HashSet<string> retiredInOperation = new HashSet<string>(StringComparer.Ordinal);

            // recordedCopies: copies of each rider's interrupted choice by identity, (index, slot object) pairs, for the
            // current world only. Slot strings are distinct instances per placement, so ReferenceEquals is object identity.
            private readonly Dictionary<string, List<KeyValuePair<int, string>>> copies = new Dictionary<string, List<KeyValuePair<int, string>>>(StringComparer.Ordinal);
            private World copiesWorld;

            internal List<KeyValuePair<int, string>> CopiesOf(string id)
            {
                if (!ReferenceEquals(Current, copiesWorld)) { copies.Clear(); copiesWorld = Current; }
                List<KeyValuePair<int, string>> list;
                return id != null && copies.TryGetValue(id, out list) ? list : null;
            }

            internal bool IsRecordedCopyAt(string id, int index, string slot)
            {
                var list = CopiesOf(id);
                return list != null && slot != null && list.Exists(pair => pair.Key == index && ReferenceEquals(pair.Value, slot));
            }

            internal void ForgetCopies(string id) { if (id != null) copies.Remove(id); }

            // BeginNativeBadSlotCleanup: a previous operation that never returned is closed first.
            internal void BeginNative(Rider rider, int index = -1)
            {
                CloseNative();
                foreach (var each in Riders) each.NativeEmptied.Clear();
                retiredInOperation.Clear();
                Scope.Begin(Enabled ? rider : null, index, FrameNo);
            }

            internal void EndNative(Rider rider) => ApplyConfirmed(Scope.End(rider));

            // CloseNativeSlotScope: an aborted operation's last report is applied only when the slot holds native's own write.
            internal void CloseNative()
            {
                var recovered = new List<NativeReport>();
                Scope.Close(NativeWrote, recovered);
                if (recovered.Count > 0 && Enabled) ApplyConfirmed(recovered);
                retiredInOperation.Clear();
            }

            // NativeWroteReportedSlot: native's emptying (the empty slot it binds to the rider; the player's removal writes a
            // plain null here, which carries no native tag) or its rebind to the live data of the same kind.
            private bool NativeWrote(NativeReport item)
            {
                var current = item.Rider.Slots[item.Index];
                if (ReferenceEquals(current, item.Slot)) return false;
                if (current == null) return item.Rider.NativeEmptied.Contains(item.Index);
                return MountedControlSlotPolicy.IsNativeRebind(item.Kind, KindOf(current), item.Rider.HoldsLiveData(item.Index));
            }

            // ObserveInvalidatedPublicSlot: IsBad answered true for the public binding at index.
            internal void ReportNative(Rider rider, int index)
            {
                var slot = rider.Slots[index];
                var kind = KindOf(slot);
                if (!Enabled || !MountedControlSlotPolicy.IsPublicControlKind(kind) || rider.World != Current || !rider.OnRoster) return;
                var settled = new List<NativeReport>();
                if (!Scope.Settle(rider, index, FrameNo, settled)) return;
                ApplyConfirmed(settled);
                int chosen;
                var hasChoice = Accepts ? Intent.TryGetInterruptedChoice(rider.Id, out chosen) : Fence.TryGetOutgoingInterruptedChoice(Current, rider.Id, out chosen);
                var alias = kind != NativeMountedControlKind.MountState;
                var recordedCopy = IsRecordedCopyAt(rider.Id, index, slot);
                var report = new NativeReport
                {
                    World = Current, Rider = rider, Index = index, Slot = slot, Kind = kind, Alias = alias, PetId = rider.PetId, Desired = rider.Desired,
                    Copy = recordedCopy || hasChoice && alias,
                    CopySlots = Enumerable.Range(0, rider.Slots.Length).Where(i => i != index && MountedControlSlotPolicy.IsPublicControlKind(KindOf(rider.Slots[i])))
                        .Select(i => new KeyValuePair<int, string>(i, rider.Slots[i])).ToList(),
                    ChosenFree = !hasChoice || rider.Slots[chosen] == null
                };
                if (!hasChoice && !recordedCopy && (alias ? rider.LegacyAliasIndex() != index :
                    !MountedControlSlotPolicy.IsSelectedPublicReport(rider.PublicIndex(), index))) return;
                Scope.Report(rider, index, FrameNo, report, null);
            }

            // ApplyConfirmedSlotReports.
            private void ApplyConfirmed(List<NativeReport> confirmed)
            {
                foreach (var item in confirmed)
                {
                    if (retiredInOperation.Contains(item.Rider.Id)) continue;
                    var current = item.Rider.Slots[item.Index];
                    if (ReferenceEquals(current, item.Slot)) continue;
                    var rebound = MountedControlSlotPolicy.IsNativeRebind(item.Kind, KindOf(current), current != null && item.Rider.HoldsLiveData(item.Index));
                    if (ApplySlotInvalidation(item, rebound, current)) retiredInOperation.Add(item.Rider.Id);
                }
            }

            // ApplySlotInvalidation: only in the originating world, for the same rider instance; the load scope's outgoing
            // records while fenced. Returns true when it retired the arrangement.
            private bool ApplySlotInvalidation(NativeReport item, bool rebound, string written)
            {
                var rider = item.Rider;
                if (!MountedControlSlotPolicy.DeferredObservationApplies(item.World, Current, rider.World == Current && rider.OnRoster)) return false;
                bool recorded;
                if (!Accepts)
                {
                    if (Fence.RetireOutgoingOnCompanionLoss(Current, rider.Id, item.PetId, item.Desired, rider.Slots.Any(s => MountedControlSlotPolicy.IsPublicControlKind(KindOf(s)))))
                    {
                        rider.ClearPublicSlots();
                        ForgetCopies(rider.Id);
                        return true;
                    }
                    recorded = item.Alias && !rebound
                        ? Fence.AdmitOutgoingSurvivingBinding(Current, rider.Id, item.Index, item.PetId, item.Desired, item.ChosenFree, item.Copy)
                        : Fence.ObserveOutgoingCleanupOutcome(Current, rider.Id, item.Index, item.PetId, item.Desired, rebound, item.Copy, item.ChosenFree);
                }
                else
                {
                    if (rider.RetireOnPetChange()) { ForgetCopies(rider.Id); return true; }
                    recorded = rebound
                        ? Intent.ObserveNativeCleanupOutcome(rider.Id, item.Index, item.PetId, item.Desired, true, item.Copy, item.ChosenFree)
                        : item.Alias
                            ? Intent.ObserveNativeAliasInvalidated(rider.Id, item.Index, item.PetId, item.Desired, item.ChosenFree, item.Copy)
                            : Intent.ObserveNativeSlotInvalidated(rider.Id, item.Index, item.PetId, item.Desired, item.Copy, item.ChosenFree);
                    if (recorded) Placed.Add(rider.Id);
                }
                TrackCopies(item, rebound, recorded, written);
                return false;
            }

            // TrackCopies.
            private void TrackCopies(NativeReport item, bool rebound, bool recorded, string written)
            {
                var id = item.Rider.Id;
                var list = CopiesOf(id);
                if (item.Copy)
                {
                    if (list == null) return;
                    var at = list.FindIndex(pair => pair.Key == item.Index && ReferenceEquals(pair.Value, item.Slot));
                    if (at < 0) return;
                    if (rebound && !recorded) list[at] = new KeyValuePair<int, string>(item.Index, written);
                    else list.RemoveAt(at);
                    return;
                }
                if (!recorded || rebound) return;   // a rebind of the player's own binding keeps the surviving copies
                copies[id] = new List<KeyValuePair<int, string>>(item.CopySlots);
            }

            // PruneCopies: a recorded copy whose index no longer holds that exact object is released.
            internal void PruneCopies(Rider rider)
            {
                var list = CopiesOf(rider.Id);
                if (list == null) return;
                list.RemoveAll(pair => !ReferenceEquals(rider.Slots[pair.Key], pair.Value));
                if (list.Count == 0) ForgetCopies(rider.Id);
            }

            // Test probe: the slot objects the registry still holds for a rider.
            internal int RecordedCopyCount(string id) => CopiesOf(id)?.Count ?? 0;

            // Native CleanupBadSlots empties the rider's public slot after IsBad reported it, then throws on a later slot:
            // its postfix never runs.
            internal void QueueNativeInvalidation(Rider rider)
            {
                var at = rider.Find();
                if (at < 0) return;
                BeginNative(rider);
                ReportNative(rider, at);
                rider.NativeWrite(at, null);
            }

            // The next update closes the aborted scope (CloseNativeSlotScope): native's completed write is applied, in the
            // originating world only; a report native never acted on is dropped.
            internal void ResolvePendingNative() => CloseNative();
            internal void DiscardAbortedNativeReports() => CloseNative();

            // KMC bindings on any current-world bar while the save serializes (after the leases).
            internal int SerializedKmcSlots = -1;

            // An in-world replay of rows captured earlier (area transfer): RestorePersistentSlots(rows, false) seeds,
            // runs an update, then writes the allowed slots.
            internal void Replay(List<SavedRow> rows)
            {
                foreach (var rider in CurrentWorld.ToList()) rider.Restore(rows, false);
                Frame();
                foreach (var rider in CurrentWorld.ToList()) rider.WriteRestoredSlot(rows);
            }

            // The live capture an area transfer keeps in memory (no absent intent).
            internal List<SavedRow> CaptureTransfer()
            {
                var rows = new List<SavedRow>();
                var written = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rider in Candidates) rider.CaptureLive(rows, written);
                return rows;
            }

            // SetEnabled(false): reserve bindings cleared, then every candidate and observed unit's facts and KMC slots.
            internal void Disable()
            {
                foreach (var reserve in Reserve.ToList()) reserve.ClearControlSlots();
                foreach (var rider in Candidates.Concat(Observed).Distinct().ToList()) rider.KmcRemove(clearSlots: true, interrupt: true);
                Observed.Clear(); Placed.Clear(); Intent.Clear(); Fence.Reset(); Scanned.Clear();
                // DropNativeSlotObservations.
                Scope.Close(); retiredInOperation.Clear(); copies.Clear(); copiesWorld = null;
                Enabled = false;
            }

            internal void Frame()
            {
                FrameNo++;
                // An open scope here was left by an exception (CloseNativeSlotScope); the copy registry keeps the current world.
                if (Scope.Open) CloseNative();
                CopiesOf(null);
                if (!Enabled) return; // Update returns at once while the service is disabled
                if (!Accepts) return; // Update returns before ReconcileFacts while a selected load's outgoing world is current
                AdmitReserve();
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
            // duringScope: something that happens while the save worker holds the scope (after the leases and fact
            // removal, before EndSaveSerializationScope's update and the lease release).
            internal List<SavedRow> Save(Action duringScope = null)
            {
                // CapturePersistentSlots (and the save scope) first account for an aborted operation's completed write.
                if (Scope.Open) CloseNative();
                AdmitReserve();
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
                    // The remembered slot belongs to another binding on the bar there now: saved off the bar.
                    var occupied = index >= 0 && owner.Slots[index] != null && !MountedControlSlotPolicy.IsPublicControlKind(KindOf(owner.Slots[index]));
                    var liveRows = rows.Where(x => x.Actor == id).Select(x => new KeyValuePair<int, int>(x.Index, x.Kind)).ToList();
                    // The same decision governs the continuing arrangement.
                    if (MountedControlSlotPolicy.RememberedSlotTaken(index, liveRows, occupied)) { Intent.GiveUpOccupiedSlot(id); Placed.Add(id); }
                    foreach (var r in MountedControlSlotPolicy.AbsentRowsAfterLive(index, liveRows, occupied))
                        rows.Add(new SavedRow { Actor = id, Index = r.Key, Kind = r.Value });
                }
                // The save scope over candidates and still-observed units (reserve riders' bindings are leased too).
                AdmitReserve();
                foreach (var rider in candidates) rider.ObserveBar();
                var scoped = candidates.Concat(Observed).Distinct().ToList();
                foreach (var rider in scoped.Concat(Reserve).Distinct().ToList()) rider.Lease();
                SerializedKmcSlots = CurrentWorld.Sum(r => r.KmcCount());
                foreach (var rider in scoped) rider.KmcRemove(clearSlots: false, interrupt: rider.World == Current);
                duringScope?.Invoke();
                Frame();
                foreach (var rider in scoped) rider.ReleaseLeases();
                return rows;
            }

            // SuspendAreaControls: departed riders classified, candidates observed, then every candidate and observed
            // unit has its facts removed and KMC slots emptied (RemoveAllManagedFacts(true) also clears observedUnits).
            internal void SuspendArea()
            {
                if (Scope.Open) CloseNative();
                var candidates = Candidates;
                foreach (var prior in Observed.Where(r => !candidates.Contains(r)).ToList()) ObserveDeparture(prior, candidates);
                foreach (var rider in candidates) rider.ObserveBar();
                foreach (var rider in candidates.Concat(Observed).Distinct().ToList()) rider.KmcRemove(clearSlots: true, interrupt: rider.World == Current);
                Observed.Clear();
            }

            // BeginSelectedLoad(owner) at the native iterator's first step: records moved aside, the outgoing world fenced.
            internal void BeginLoad(object owner = null) => Fence.Begin(Current, owner ?? new object(), Intent, Placed, Enabled);

            // NativeMountedControlService.enabled (SetEnabled); a disabled service observes no native toolbar cleanup.
            internal bool Enabled = true;

            // AbandonLoadedWorld(owner): the owning scope ended without native completion.
            internal bool AbandonLoad(object owner) => Fence.Abandon(owner, Current, Intent, Placed);

            // Native loading replaces the player object; the incoming world's riders are added by the caller first.
            internal void InstallAndRestore(World incoming, List<SavedRow> rows)
            {
                Current = incoming;
                foreach (var rider in CurrentWorld.ToList()) rider.Restore(rows, true);
                Frame();
                foreach (var rider in CurrentWorld.ToList()) rider.WriteRestoredSlot(rows);
            }

            // A cold load of rows into a new world.
            internal void Load(World incoming, List<SavedRow> rows) { BeginLoad(); InstallAndRestore(incoming, rows); }
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
            // A mounted or faulted relationship still grants the public control (Dismount), whatever the pet link says.
            internal bool RelationshipGrants;
            // The older Mount / Dismount shell fact's activity, independent of the canonical public fact when set.
            internal bool? ShellLiveOverride;
            internal bool ShellLive => ShellLiveOverride ?? FactLive;
            // An older Mounted Charge binding is callable live data (its fact is leased).
            internal bool ChargeLive;
            // The rider's own stock Charge fact is available.
            internal bool HasStockCharge = true;
            internal int DataVersion = 1;
            private readonly List<KeyValuePair<int, NativeMountedControlKind>> leases = new List<KeyValuePair<int, NativeMountedControlKind>>();

            internal Rider(string id, Service service, World world) { Id = id; S = service; World = world; S.Riders.Add(this); }

            internal MountedControlPlacementIntent Intent => S.Intent;
            internal string Data => Control + "#" + DataVersion;
            internal bool Desired => RelationshipGrants || PetId != null && PetResolves;
            internal int Find() => Array.FindIndex(Slots, s => s != null && s.StartsWith(Control, StringComparison.Ordinal));
            internal int Count() => Slots.Count(s => s != null && s.StartsWith(Control, StringComparison.Ordinal));
            internal int KmcCount() => Slots.Count(s => Service.KindOf(s) != NativeMountedControlKind.None);
            // ResolveStockChargeForSlot: the rider's own stock Charge unless it is already on the bar.
            internal string ResolveStockCharge() => HasStockCharge && !Slots.Contains("Charge") ? "Charge" : null;

            internal void Frame() => S.Frame();

            internal void ClearControlSlots()
            {
                for (var i = 0; i < Slots.Length; i++) if (Slots[i] != null && Slots[i].StartsWith(Control, StringComparison.Ordinal)) Slots[i] = null;
            }

            // One candidate's reconciliation in ReconcileFacts.
            // RetireOnCompanionLoss: a different pet link, or a cleared one while the rider no longer gets the control,
            // retires the arrangement and clears the public slots.
            internal bool RetireOnPetChange()
            {
                // Public bindings: the canonical control and any exact older alias standing for it.
                if (!Intent.RetireOnCompanionLoss(Id, PetId, Desired, Slots.Any(IsPublic))) return false;
                ClearPublicSlots();
                return true;
            }

            private static bool IsPublic(string slot) => MountedControlSlotPolicy.IsPublicControlKind(Service.KindOf(slot));

            // PublicBindingIndex: the canonical binding first, else the first exact older alias.
            internal int PublicIndex() => MountedControlSlotPolicy.RecoveredPublicSlot(Slots.Select(Service.KindOf).ToArray());

            // LegacyAliasIndex: the first exact older alias when no canonical binding is on the bar, else -1.
            internal int LegacyAliasIndex()
            {
                var kinds = Slots.Select(Service.KindOf).ToArray();
                if (kinds.Contains(NativeMountedControlKind.MountState)) return -1;
                return Array.FindIndex(kinds, k => MountedControlSlotPolicy.RepairLiveSlot(k) == MountedControlSlotRepair.MountState);
            }

            // IsPublicCopyAt: a copy of the interrupted choice by identity (the exact recorded binding at its index), or an
            // older alias.
            internal bool IsCopyAt(int i) => IsPublic(Slots[i]) && (S.IsRecordedCopyAt(Id, i, Slots[i]) ||
                Intent.IsSecondaryBinding(Id, i, Service.KindOf(Slots[i]) != NativeMountedControlKind.MountState));

            // EffectivePublicIndex (MountedControlSlotPolicy.EffectiveBindingIndex).
            internal int Effective(out bool onlyYieldingCopies) =>
                MountedControlSlotPolicy.EffectiveBindingIndex(Slots.Select(Service.KindOf).ToArray(), IsCopyAt, ChosenFree(), out onlyYieldingCopies);

            // ChosenSlotFree: the interrupted chosen slot is empty (a copy yields only to a free chosen slot).
            internal bool ChosenFree()
            {
                int chosen;
                return Intent.TryGetInterruptedChoice(Id, out chosen) && Slots[chosen] == null;
            }

            // The slot holds the live fact's own data of its kind (native's rebind writes exactly that).
            internal bool HoldsLiveData(int i)
            {
                var kind = Service.KindOf(Slots[i]);
                if (kind == NativeMountedControlKind.MountState) return FactLive && Slots[i] == Data;
                if (kind == NativeMountedControlKind.MountCompanion) return ShellLive && Slots[i] == "KMC_Mount#" + DataVersion;
                if (kind == NativeMountedControlKind.Dismount) return ShellLive && Slots[i] == "KMC_Dismount#" + DataVersion;
                return false;
            }

            // Indices native emptied in the current operation (native's empty slot bound to the rider).
            internal readonly HashSet<int> NativeEmptied = new HashSet<int>();

            internal void NativeWrite(int i, string value)
            {
                Slots[i] = value;
                if (value == null) NativeEmptied.Add(i); else NativeEmptied.Remove(i);
            }

            // ClearPublicControlSlots: the copy registry releases this rider's slot objects too.
            internal void ClearPublicSlots()
            {
                S.ForgetCopies(Id);
                for (var i = 0; i < Slots.Length; i++) if (IsPublic(Slots[i])) Slots[i] = null;
            }

            // IsCallableLiveBinding for the selected public slot.
            internal bool Callable(int i) => i >= 0 && Desired &&
                (Service.KindOf(Slots[i]) == NativeMountedControlKind.MountState ? FactLive && Slots[i] == Data : ShellLive);

            internal void Reconcile()
            {
                RetireOnPetChange();
                // The bar is observed before the public fact is withdrawn, replaced or re-leased (one fact here:
                // withdrawal of a live fact, or a missing/inactive fact the rider still gets).
                if ((FactLive && !Desired) || (!FactLive && Desired)) ObserveBar();
                var replaced = false;
                if (Desired && !FactLive) { FactLive = true; DataVersion++; replaced = true; }
                if (!Desired) FactLive = false;
                // ReconcilePublicControlSlots. Stale copy pairs are released, then the effective binding and the copies are
                // decided before the repair rebinds or migrates anything.
                S.PruneCopies(this);
                bool onlyYieldingCopies;
                var effective = Effective(out onlyYieldingCopies);
                var copies = Enumerable.Range(0, Slots.Length).Where(IsCopyAt).ToList();
                if (!FactLive)
                {
                    // A fallback copy (chosen slot occupied) becomes the choice before KMC's own migration clears it;
                    // otherwise an older alias is recorded (its index) before it is emptied.
                    if (effective >= 0 && copies.Contains(effective) && S.Accepts) { Intent.PromoteFallback(Id, effective, PetId); S.Placed.Add(Id); }
                    else RecoverAlias();
                }
                for (var i = 0; i < Slots.Length; i++)
                {
                    var kind = Service.KindOf(Slots[i]);
                    if (kind == NativeMountedControlKind.MountState)
                    {
                        if (!FactLive) continue;
                        S.Placed.Add(Id);
                        if (Slots[i] != Data) Slots[i] = Data; // a stale binding is rebound (a new slot object)
                        continue;
                    }
                    if (MountedControlSlotPolicy.RepairLiveSlot(kind) != MountedControlSlotRepair.MountState) continue;
                    Slots[i] = FactLive && Find() < 0 ? Data : null;
                    S.Placed.Add(Id);
                }
                var bound = Find();
                // Every copy other than the effective binding goes (one shortcut); copies that yield leave the control to
                // return to the free chosen slot below; the first copy is the fallback while that slot is occupied.
                if (FactLive && copies.Count > 0)
                {
                    foreach (var copy in copies) if (copy != effective && IsPublic(Slots[copy])) Slots[copy] = null;
                    bound = effective >= 0 && Service.KindOf(Slots[effective]) == NativeMountedControlKind.MountState ? effective
                        : onlyYieldingCopies ? -1 : Find();
                }
                // Confirmed permanent loss clears the public-control slots.
                if (!FactLive && !Desired && Intent.ProvesPermanentLoss(Id, PetId))
                    for (var i = 0; i < Slots.Length; i++) if (Slots[i] != null && Slots[i].StartsWith(Control, StringComparison.Ordinal)) Slots[i] = null;
                var index = Intent.Reconcile(Id, FactLive, Desired, replaced, bound, i => Slots[i] == null,
                    leases.Count != 0, S.Placed.Contains(Id), PetId);
                if (index >= 0) { Slots[index] = Data; S.Placed.Add(Id); Intent.ObserveBound(Id, index, PetId); }
                // Once no choice is interrupted, nothing is a copy of one.
                int chosen;
                if (S.Accepts && !Intent.TryGetInterruptedChoice(Id, out chosen)) S.ForgetCopies(Id);
                // TryInitialPlacement: a pending lease of a public binding already satisfies the one-time placement.
                if (leases.Any(lease => MountedControlSlotPolicy.IsPublicControlKind(lease.Value))) S.Placed.Add(Id);
                if (FactLive && !S.Placed.Contains(Id) && Autofill)
                {
                    S.Placed.Add(Id);
                    var empty = Enumerable.Range(0, 14).FirstOrDefault(i => Slots[i] == null);
                    // TryInitialPlacement records the placement with the companion it was made for.
                    if (Slots[empty] == null && Find() < 0) { Slots[empty] = Data; Intent.ObserveBound(Id, empty, PetId); }
                }
            }

            internal void ObserveBar()
            {
                if (!S.Enabled) return;            // a disabled service records nothing
                if (RetireOnPetChange()) return;
                RecoverAlias();
                // The effective binding (the decision live repair makes); copies that yield keep the interrupted choice.
                bool onlyYieldingCopies;
                var bound = Effective(out onlyYieldingCopies);
                if (bound >= 0 || onlyYieldingCopies) S.Placed.Add(Id); // an observed public binding satisfies the one-time placement
                if (bound >= 0) Intent.ObserveAtBoundary(Id, true, bound, S.Placed.Contains(Id), PetId);
                else if (!onlyYieldingCopies) Intent.ObserveAtBoundary(Id, FactLive, bound, S.Placed.Contains(Id), PetId);
            }

            // RecoverLegacyAlias: an older exact Mount / Dismount alias (no canonical public binding) is recorded as the
            // chosen slot before repair could empty it.
            internal void RecoverAlias()
            {
                if (!S.Accepts) return;
                var kinds = Slots.Select(Service.KindOf).ToArray();
                var at = MountedControlSlotPolicy.RecoveredPublicSlot(kinds);
                if (at < 0 || kinds[at] == NativeMountedControlKind.MountState) return;
                if (Intent.AdmitSurvivingBinding(Id, at, PetId, Desired)) S.Placed.Add(Id);
            }

            // Native deactivates the fact (no slot holds it, so native cleanup reports nothing).
            internal void NativeDeactivate() { FactLive = false; }

            // ClassifyArrangementsForCapture for a candidate.
            internal void Classify()
            {
                if (!S.Enabled) return;            // CapturePersistentSlots classifies only while enabled
                ObserveBar();
                if (FactLive)
                {
                    // A bound public slot the live capture will not write (stale data, or the rider no longer gets the
                    // control) is carried while the rider still gets it or its pet link names the same companion.
                    // The live writer's selected index and the callability of that exact slot.
                    bool onlyYieldingCopies;
                    var at = Effective(out onlyYieldingCopies);
                    var written = at >= 0 && at <= 127 && Callable(at);
                    if ((at >= 0 || onlyYieldingCopies) && !written && (Desired || Intent.Revalidate(Id, true, true, PetId)))
                        Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                    return;
                }
                if (Desired || Intent.Revalidate(Id, true, true, PetId)) Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
            }

            // CaptureLiveSlots: only callable live data is written.
            internal void CaptureLive(List<SavedRow> rows, HashSet<string> written)
            {
                // Production slot order and precedence (MountedControlSlotPolicy.LivePublicRows): the canonical binding is
                // callable when it holds the live data; an older alias when its own shell fact is live.
                var kinds = Slots.Select(Service.KindOf).ToArray();
                bool onlyYieldingCopies;
                var effective = Effective(out onlyYieldingCopies);
                foreach (var r in MountedControlSlotPolicy.LivePublicRows(kinds,
                    i => Desired && (kinds[i] == NativeMountedControlKind.MountState ? FactLive && Slots[i] == Data : ShellLive),
                    S.Placed.Contains(Id), FactLive, effective))
                {
                    rows.Add(new SavedRow { Actor = Id, Index = r.Key, Kind = r.Value });
                    if (r.Value == MountedControlSlotPolicy.SavedMountState) written.Add(Id);
                }
                // Another callable KMC binding (an older Mounted Charge awaiting migration) is written at its own index.
                for (var i = 0; i < Slots.Length && i <= 127; i++)
                    if (kinds[i] == NativeMountedControlKind.MountedCharge && ChargeLive)
                        rows.Add(new SavedRow { Actor = Id, Index = i, Kind = (int)NativeMountedControlKind.MountedCharge });
            }

            // CaptureAndClearManagedHotbarSlots: every KMC binding (the public control and an older alias) is leased.
            internal void Lease()
            {
                for (var i = 0; i < Slots.Length; i++)
                {
                    var kind = Service.KindOf(Slots[i]);
                    if (kind == NativeMountedControlKind.None) continue;
                    leases.Add(new KeyValuePair<int, NativeMountedControlKind>(i, kind));
                    Slots[i] = null;
                }
            }

            // ClearSerializationHotbarLeases: a leased binding returns while its fact is live; a public binding (the
            // control or an alias standing for it) only while the slot is still the arrangement for this companion.
            internal void ReleaseLeases()
            {
                foreach (var lease in leases)
                {
                    var at = lease.Key;
                    if (Slots[at] != null || !FactLive || World != S.Current || !InParty) continue;
                    if (MountedControlSlotPolicy.IsPublicControlKind(lease.Value) && !Intent.AllowsLeaseRestore(Id, at, PetId)) continue;
                    if (lease.Value == NativeMountedControlKind.MountedCharge) { if (ChargeLive) Slots[at] = "KMC_Charge#" + DataVersion; continue; }
                    if (lease.Value == NativeMountedControlKind.MountState) Slots[at] = Data;
                    else Slots[at] = (lease.Value == NativeMountedControlKind.MountCompanion ? "KMC_Mount#" : "KMC_Dismount#") + DataVersion;
                    // The public control, or the alias standing for it, is live in its slot again.
                    if (S.Enabled && MountedControlSlotPolicy.IsPublicControlKind(lease.Value)) Intent.ObserveBound(Id, at, PetId);
                }
                leases.Clear();
            }

            // RemoveManagedFacts: facts removed, the absence recorded for the current world, KMC slots emptied if asked.
            internal void KmcRemove(bool clearSlots, bool interrupt)
            {
                FactLive = false;
                if (interrupt && S.Enabled) Intent.ObserveAbsent(Id, MountedControlAbsence.Temporary);
                if (clearSlots)
                {
                    // ClearManagedHotbarSlots: the current world's registry releases this rider's slot objects.
                    if (World == S.Current) S.ForgetCopies(Id);
                    var at = Find(); if (at >= 0) Slots[at] = null;
                }
            }

            // Native bad-slot cleanup: IsBad answered true for the slot holding the control (the production postfix
            // reports that exact index), the slot is emptied; reactivated: the fact is active again before the update.
            // Native GetSlot(index): IsBad reports the slot to the production observer (ReportNative) inside the operation's
            // scope, native empties it (its empty slot bound to the rider), and the call returns unless it throws.
            internal void NativeGetSlot(int index, bool returns = true)
            {
                S.BeginNative(this, index);
                S.ReportNative(this, index);
                NativeWrite(index, null);
                if (returns) S.EndNative(this);
            }

            // Native bad-slot cleanup of the slot holding the control; reactivated: the fact is active again before the
            // update.
            internal void NativeInvalidate(bool reactivated = false)
            {
                var at = Find();
                if (at >= 0) NativeGetSlot(at);
                FactLive = reactivated;
            }

            // Native GetSlot(index) empties one bad canonical slot.
            internal void NativeInvalidateAt(int index)
            {
                if (Service.KindOf(Slots[index]) != NativeMountedControlKind.MountState) return;
                NativeGetSlot(index);
            }

            // Native GetSlot(index) on an older alias.
            internal void NativeInvalidateAliasAt(int index)
            {
                if (MountedControlSlotPolicy.RepairLiveSlot(Service.KindOf(Slots[index])) != MountedControlSlotRepair.MountState) return;
                NativeGetSlot(index);
            }

            // A native operation throws before its postfix (without reporting); a standalone IsBad read of the stale slot in
            // the same frame then falls inside its still-open scope.
            internal void AbortedOperationThenQuery()
            {
                S.BeginNative(this);
                var at = Find();
                if (at >= 0) S.ReportNative(this, at);
            }

            // A standalone IsBad query (outside GetSlot / CleanupBadSlots, for example a diagnostic reading the bar): no
            // scope is open, so the production observer records nothing and the slot stays.
            internal void QueryIsBadOnly() => ReportBad(Find(), insideNativeOperation: false);

            // An IsBad report: inside a native GetSlot (which then empties the slot and returns), or standalone.
            internal void ReportBad(int at, bool insideNativeOperation)
            {
                if (at < 0) return;
                if (insideNativeOperation) NativeGetSlot(at);
                else S.ReportNative(this, at);
            }

            // Native CleanupBadSlots repairs the surviving older alias with a live shell fact of its blueprint (a new binding
            // holding that fact's data); the production classifier reads it as a rebind.
            internal void NativeCleanupRebindAlias()
            {
                var at = Array.FindIndex(Slots, s => MountedControlSlotPolicy.RepairLiveSlot(Service.KindOf(s)) == MountedControlSlotRepair.MountState);
                if (at < 0) return;
                var prefix = Service.KindOf(Slots[at]) == NativeMountedControlKind.Dismount ? "KMC_Dismount#" : "KMC_Mount#";
                S.BeginNative(this);
                S.ReportNative(this, at);
                NativeWrite(at, prefix + DataVersion);               // the live shell's data
                S.EndNative(this);
            }

            // One native CleanupBadSlots pass over a bar whose public facts are all inactive: every public slot is bad and
            // is reported (ReportNative) and emptied in index order; the call returns unless it throws. throwAfter: the
            // pass throws on a later (non-KMC) slot after writing this index.
            // rebindLive: the public fact is live (a replacement), so only stale bindings are bad and native rebinds each
            // canonical one to the live data (a new binding object, CleanupBadSlots' rebind).
            internal void NativeCleanupPass(int throwAfter = -1, bool rebindLive = false)
            {
                S.BeginNative(this);
                for (var i = 0; i < Slots.Length; i++)
                {
                    if (!IsPublic(Slots[i])) continue;
                    if (rebindLive && HoldsLiveData(i)) continue;     // not bad
                    S.ReportNative(this, i);
                    NativeWrite(i, rebindLive && FactLive && Service.KindOf(Slots[i]) == NativeMountedControlKind.MountState ? Data : null);
                    if (i == throwAfter) return;
                }
                S.EndNative(this);
            }

            // Native CleanupBadSlots on the stale canonical binding: it writes a new binding holding the live fact's data
            // when a live fact of the same blueprint exists (its rebind), otherwise an empty slot; the production classifier
            // (MountedControlSlotPolicy.IsNativeRebind) reads the outcome.
            internal void NativeCleanupBadSlots()
            {
                var at = Find();
                if (at < 0 || FactLive && Slots[at] == Data) return;          // only a bad slot is handed over
                S.BeginNative(this);
                S.ReportNative(this, at);
                NativeWrite(at, FactLive ? Data : null);                      // native's rebind or emptying
                S.EndNative(this);
            }

            // Native GetSlot of an older alias whose shell fact is gone.
            internal void NativeInvalidateAlias()
            {
                var at = Array.FindIndex(Slots, s => MountedControlSlotPolicy.RepairLiveSlot(Service.KindOf(s)) == MountedControlSlotRepair.MountState);
                if (at < 0) return;
                NativeGetSlot(at);
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

            internal List<SavedRow> Save(Action duringScope = null) => S.Save(duringScope);

            internal void Restore(List<SavedRow> rows, bool loadedWorld)
            {
                var mine = rows.Where(r => r.Actor == Id).ToList();
                if (mine.Any(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord)) S.Placed.Add(Id);
                var slotRow = mine.FirstOrDefault(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind));
                if (slotRow != null) { Intent.Seed(Id, slotRow.Index, loadedWorld); S.Placed.Add(Id); }
                else if (mine.Any()) Intent.Seed(Id, MountedControlPlacementIntent.OffBar, loadedWorld);
            }

            internal void WriteRestoredSlot(List<SavedRow> rows)
            {
                var slotRow = rows.FirstOrDefault(r => r.Actor == Id && MountedControlSlotPolicy.IsPublicBindingRow(r.Kind));
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
                for (var i = 0; i < Slots.Length; i++) if (Service.KindOf(Slots[i]) == NativeMountedControlKind.None) loaded.Slots[i] = Slots[i];
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

        private static void HeaderUnresolvedPet()
        {
            var rider = PlacedAt(9);
            rider.PetResolves = false; // same PetUniqueId; the old public fact and slot data still live at the header
            TestRunner.True(rider.FactLive && rider.Slots[9] == rider.Data, "The fixture did not keep the live fact and slot data.");
            var rows = rider.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1 && rows.Count == 2,
                "The header dropped the live slot of a rider whose pet briefly did not resolve.");
            var loaded = rider.Load(rows);
            loaded.Frame();
            loaded.PetResolves = true; loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "Restoring the same pet did not bind slot 9 once.");
            var remover = PlacedAt(9, "remover");
            remover.Slots[9] = null; remover.PetResolves = false;
            var removalRows = remover.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "A deliberate removal saved a slot row.");
        }

        private static void RemovalBeforeLeaseLoss()
        {
            var rider = PlacedAt(9);
            rider.Slots[9] = null;      // live removal, not yet observed
            rider.PetResolves = false;  // the same pet stops resolving before the next update
            rider.Frame();
            rider.PetResolves = true; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The removal was undone when the same pet returned.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save after the removal carried a slot row.");
            var loaded = rider.Load(rows); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Loading resurrected the removed control.");
            // Control: native invalidation followed by the same lease loss still returns to slot 9.
            var cleaned = PlacedAt(9, "cleaned");
            cleaned.NativeInvalidate(reactivated: true);
            cleaned.PetResolves = false; cleaned.Frame();
            cleaned.PetResolves = true; cleaned.Frame(); cleaned.Frame();
            TestRunner.True(cleaned.Find() == 9 && cleaned.Count() == 1, "A native invalidation no longer returned to slot 9.");
        }

        private static void PermanentLossClearsSlot()
        {
            var rider = PlacedAt(9);   // an unselected party rider: native UI never inspects its bar
            rider.PetId = null;        // the pet link is cleared (companion released)
            rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The managed public slot survived a confirmed permanent loss.");
            TestRunner.True(rider.Slots[0] == "Attack", "An unrelated slot was cleared.");
            rider.PetId = "horse-2"; rider.Frame(); rider.Frame();
            TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null, "A different pet revived the old binding.");
            // Same-pet temporary absence keeps the binding on the bar and rebinds it when the pet returns.
            var stays = PlacedAt(9, "stays");
            stays.PetResolves = false; stays.Frame();
            TestRunner.True(stays.Slots[9] != null, "A temporary absence cleared the slot.");
            stays.PetResolves = true; stays.Frame();
            TestRunner.True(stays.Find() == 9 && stays.Count() == 1 && stays.Slots[9] == stays.Data, "The same pet did not rebind slot 9 once.");
        }

        private static void SuspensionObservesDeparted()
        {
            foreach (var remove in new[] { false, true })
            {
                var service = new Service();
                var mountedRider = PlacedAt(5, "mounted", service); // keeps area suspension relevant
                var second = PlacedAt(9, "second", service);
                if (remove) second.Slots[9] = null;
                else { var data = second.Slots[9]; second.Slots[9] = null; second.Slots[3] = data; }
                second.LeaveParty();          // reserve roster, departure cleanup not run yet
                service.SuspendArea();        // area unload before the departure loop
                service.Frame();              // resume
                second.ToParty(); service.Frame(); service.Frame();
                if (remove) TestRunner.Equal(-1, second.Find(), "Area suspension undid a removal of a departed rider.");
                else TestRunner.True(second.Find() == 3 && second.Count() == 1 && second.Slots[9] == null, "Area suspension restored the older slot.");
                TestRunner.True(mountedRider.Find() == 5 && mountedRider.Count() == 1, "The mounted rider's slot changed.");
            }
        }

        private static void LoadFenceProductionOrder()
        {
            foreach (var offBarRecord in new[] { false, true })
            {
                var service = new Service();
                var outgoing = PlacedAt(9, "rider", service);
                service.BeginLoad();                       // iterator entry: records cleared, outgoing player still current
                outgoing.Frame();                          // an update in the interval
                outgoing.NativeInvalidate(reactivated: true); // a slot invalidation in the interval
                TestRunner.True(!service.Intent.Has("rider") && service.Placed.Count == 0, "The outgoing world wrote into the cleared records.");
                var incoming = new World();
                var reserve = new Rider("rider", service, incoming) { InParty = false, Autofill = false };
                var rows = offBarRecord ? new List<SavedRow> { new SavedRow { Actor = "rider", Index = 0, Kind = 7 } } : new List<SavedRow>();
                service.InstallAndRestore(incoming, rows);  // native loading replaced the player object
                var saved = service.Save();
                TestRunner.True(!saved.Any(r => r.Kind == 6) && saved.Count == (offBarRecord ? 1 : 0), "Resave reproduced the outgoing slot (offBarRecord=" + offBarRecord + ").");
                reserve.ToParty(); service.Frame(); service.Frame();
                TestRunner.Equal(-1, reserve.Find(), "Party return reproduced the outgoing choice (offBarRecord=" + offBarRecord + ").");
            }
            // An abandoned load lifts the fence and the outgoing world continues normally.
            var abandoned = new Service();
            var stays = PlacedAt(9, "rider", abandoned);
            var owner = new object();
            abandoned.BeginLoad(owner);
            abandoned.AbandonLoad(owner);
            stays.Frame();
            TestRunner.True(abandoned.Intent.Has("rider") && stays.Find() == 9, "An abandoned load left the world fenced.");
        }

        private static void RemovalThenDeactivation()
        {
            var rider = PlacedAt(9);
            rider.Slots[9] = null;     // removed while the fact is live, not yet observed
            rider.NativeDeactivate();  // native deactivates the fact before the next update; no slot holds it, so no IsBad
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A removal was undone when the fact was deactivated before the update.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save after the removal carried a slot row.");
            // Control: native cleanup of a bound slot still restores it once.
            var cleaned = PlacedAt(9, "cleaned");
            cleaned.NativeInvalidate();
            cleaned.Frame(); cleaned.Frame();
            TestRunner.True(cleaned.Find() == 9 && cleaned.Count() == 1, "Native cleanup of slot 9 no longer restored it once.");
        }

        private static void DirectPetChange()
        {
            // Update first: the link changes A -> B (B not yet resolving), the old slot still on an uninspected bar.
            var rider = PlacedAt(9);
            rider.PetId = "horse-2"; rider.PetResolves = false;
            rider.Frame();
            TestRunner.True(rider.Find() == -1 && rider.Slots[0] == "Attack", "The old slot survived a direct change of pets, or another slot changed.");
            rider.PetResolves = true; rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The new pet inherited the old slot (update first).");
            // Save first: the header runs before any update after the change.
            var saver = PlacedAt(9, "saver");
            saver.PetId = "horse-2"; saver.PetResolves = false;
            var rows = saver.Save();
            TestRunner.True(!rows.Any(r => r.Kind == 6) && saver.Slots[0] == "Attack", "The save carried the old pet's slot or cleared another slot.");
            saver.PetResolves = true; saver.Frame(); saver.Frame();
            TestRunner.Equal(-1, saver.Find(), "The new pet inherited the old slot (save first).");
            // Control: the same pet id temporarily unresolved still restores slot 9.
            var same = PlacedAt(9, "same");
            same.PetResolves = false; same.Frame();
            same.PetResolves = true; same.Frame();
            TestRunner.True(same.Find() == 9 && same.Count() == 1, "The same pet lost slot 9.");
        }

        private static void AbandonedLoadRestores()
        {
            var service = new Service();
            var reserve = PlacedAt(9, "reserve", service);
            reserve.ToReserve();                       // its only record of slot 9 is the remembered arrangement
            var offBar = PlacedAt(4, "offbar", service);
            offBar.Slots[4] = null; offBar.Frame();    // deliberately off the bar, autofill enabled
            TestRunner.True(reserve.Find() == -1 && offBar.Autofill, "The fixture is not a bar-less reserve slot and an autofill off-bar rider.");
            var owner = new object();
            service.BeginLoad(owner);                  // load starts while the outgoing player is current
            TestRunner.True(!service.Intent.Has("reserve") && !service.Placed.Contains("offbar"), "The records were not moved aside.");
            TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => r.Actor == "reserve" && r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Actor == "offbar" && r.Kind == 6) == 0 &&
                rows.Count(r => r.Actor == "offbar" && r.Kind == 7) == 1, "The abandoned load lost the reserve slot or the deliberate removal.");
            reserve.ToParty(); service.Frame(); service.Frame();
            TestRunner.True(reserve.Find() == 9 && reserve.Count() == 1 && offBar.Find() == -1, "Party return or autofill reversed a choice after the abandoned load.");
        }

        private static void SupersededLoadFence()
        {
            // A started, B queued; A disposed (superseded), then B disposed without starting.
            var service = new Service();
            var reserve = PlacedAt(9, "reserve", service);
            reserve.ToReserve();
            var loadA = new object(); var loadB = new object();
            service.BeginLoad(loadA);
            TestRunner.True(service.AbandonLoad(loadA), "The superseded scope could not release its own fence.");
            // B is disposed without MoveNext: its begin never ran, so there is nothing for it to release.
            TestRunner.True(!service.Fence.Fenced, "Controls stayed fenced after the unstarted replacement.");
            reserve.ToParty(); service.Frame();
            TestRunner.True(reserve.Find() == 9 && reserve.Count() == 1, "Reconciliation did not resume after supersession.");
            // B starts before A is disposed: A's disposal cannot release B's fence.
            var second = new Service();
            var rider = PlacedAt(9, "rider", second);
            rider.ToReserve();
            var olderA = new object(); var newerB = new object();
            second.BeginLoad(olderA);
            second.BeginLoad(newerB);
            TestRunner.True(!second.AbandonLoad(olderA) && second.Fence.Fenced, "The older scope released the newer scope's fence.");
            rider.ToParty(); second.Frame();
            TestRunner.True(rider.Find() == -1 && !second.Intent.Has("rider"), "The outgoing world wrote while the newer scope was fenced.");
            TestRunner.True(second.AbandonLoad(newerB) && !second.Fence.Fenced, "The newer scope could not abandon its own load.");
            second.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The original records did not come back after the newer scope was abandoned.");
        }

        private static void ClearedLinkCleanupThenNewPet()
        {
            foreach (var reactivated in new[] { false, true })
            {
                // Pet A's control in slot 9; the link clears, native cleanup reports slot 9, pet B is linked, and only then
                // does the first update run.
                var rider = PlacedAt(9);
                rider.PetId = null; rider.PetResolves = false;
                rider.NativeInvalidate(reactivated);
                rider.PetId = "horse-2"; rider.PetResolves = true;
                rider.Frame(); rider.Frame();
                TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null && rider.Slots[0] == "Attack",
                    "Pet B inherited pet A's slot 9, or another slot changed (reactivated=" + reactivated + ").");
                var rows = rider.Save();
                TestRunner.True(!rows.Any(r => r.Kind == 6) && rows.Count(r => r.Kind == 7) == 1,
                    "The archive after the change carried a slot row for pet B (reactivated=" + reactivated + ").");
                TestRunner.Equal(-1, rider.Find(), "The save scope put pet A's slot back for pet B (reactivated=" + reactivated + ").");
            }
            // Control: the same persistent pet id, briefly unresolved through the same cleanup, still returns to slot 9 once.
            var same = PlacedAt(9, "same");
            same.PetResolves = false;
            same.NativeInvalidate();
            same.Frame();
            same.PetResolves = true; same.Frame(); same.Frame();
            TestRunner.True(same.Find() == 9 && same.Count() == 1, "The same pet id lost slot 9 after native cleanup.");
            var rows2 = same.Save();
            TestRunner.True(rows2.Count(r => r.Kind == 6 && r.Index == 9) == 1, "The same pet's archive lost the slot row at 9.");
        }

        private static void CleanupDuringAbandonedLoad()
        {
            foreach (var reactivated in new[] { true, false })
            {
                var service = new Service();
                var rider = PlacedAt(9, "rider", service);
                var owner = new object();
                service.BeginLoad(owner);                     // the outgoing player is still current
                rider.NativeInvalidate(reactivated);          // native cleanup empties slot 9 while fenced
                rider.Frame();                                // an update in the interval writes nothing
                TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
                rider.Frame(); rider.Frame();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The abandoned load lost the slot native cleanup emptied (reactivated=" + reactivated + ").");
                var rows = service.Save();
                TestRunner.True(rows.Count(r => r.Actor == "rider" && r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 6) == 1,
                    "The next save did not carry exactly one slot row at 9 (reactivated=" + reactivated + ").");
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The save scope changed the restored slot (reactivated=" + reactivated + ").");
            }
            // Control: a deliberate removal before the load (not yet seen by an update) stays removed after abandonment.
            var removals = new Service();
            var remover = PlacedAt(9, "remover", removals);
            remover.Slots[9] = null;
            var removalOwner = new object();
            removals.BeginLoad(removalOwner);
            TestRunner.True(removals.AbandonLoad(removalOwner), "The owning scope could not abandon its load.");
            remover.Frame(); remover.Frame();
            TestRunner.Equal(-1, remover.Find(), "An abandoned load re-added a deliberately removed control.");
            var removalRows = removals.Save();
            TestRunner.True(removalRows.Count == 1 && removalRows[0].Kind == 7, "The save after the removal carried a slot row.");
            // Control: the same cleanup followed by a successful replacement gives the loaded world only its own rows.
            var replaced = new Service();
            var outgoing = PlacedAt(9, "rider", replaced);
            replaced.BeginLoad(new object());
            outgoing.NativeInvalidate(reactivated: true);
            var incoming = new World();
            var reserve = new Rider("rider", replaced, incoming) { InParty = false, Autofill = false };
            replaced.InstallAndRestore(incoming, new List<SavedRow>());
            TestRunner.True(replaced.Save().Count == 0, "The loaded world kept the outgoing world's cleanup.");
            reserve.ToParty(); replaced.Frame(); replaced.Frame();
            TestRunner.Equal(-1, reserve.Find(), "The loaded world reproduced the outgoing slot.");
        }

        private static void SuspensionClearedLinkThenNewPet()
        {
            foreach (var resolvesAtResume in new[] { true, false })
            {
                var service = new Service();
                var mounted = PlacedAt(5, "mounted", service);  // the mounted pair keeps area suspension relevant
                var rider = PlacedAt(9, "rider", service);      // dismounted, pet A's control in slot 9
                rider.PetId = null; rider.PetResolves = false;  // the link clears before the area unloads
                service.SuspendArea();
                rider.PetId = "horse-2"; rider.PetResolves = resolvesAtResume; // pet B linked before resume
                service.Frame();                                 // resume
                rider.PetResolves = true; service.Frame(); service.Frame();
                TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null && rider.Slots[0] == "Attack",
                    "Pet B received pet A's slot 9 after suspension, or another slot changed (resolvesAtResume=" + resolvesAtResume + ").");
                var rows = service.Save();
                TestRunner.True(!rows.Any(r => r.Actor == "rider" && r.Kind == 6), "The next save carried a slot row for pet B (resolvesAtResume=" + resolvesAtResume + ").");
                TestRunner.True(mounted.Find() == 5 && mounted.Count() == 1, "The mounted rider's slot changed.");
            }
            // Control: pet A's unchanged id, briefly unresolved across the suspension, returns to slot 9 once.
            var same = new Service();
            var sameMounted = PlacedAt(5, "mounted", same);
            var sameRider = PlacedAt(9, "rider", same);
            sameRider.PetResolves = false;
            same.SuspendArea();
            same.Frame();
            sameRider.PetResolves = true; same.Frame(); same.Frame();
            TestRunner.True(sameRider.Find() == 9 && sameRider.Count() == 1, "The same pet id lost slot 9 across suspension.");
            var sameRows = same.Save();
            TestRunner.True(sameRows.Count(r => r.Actor == "rider" && r.Kind == 6 && r.Index == 9) == 1, "The same pet's save lost the row at 9.");
            TestRunner.True(sameMounted.Find() == 5 && sameMounted.Count() == 1, "The mounted rider's slot changed (control).");
        }

        private static void ReplayAfterPetChangeDuringSuspension()
        {
            foreach (var updateBeforeReplay in new[] { true, false })
            {
                var service = new Service();
                var mounted = PlacedAt(5, "mounted", service);
                var rider = PlacedAt(9, "rider", service);
                var rows = service.CaptureTransfer();           // the transfer capture holds pet A's row at 9
                TestRunner.True(rows.Count(r => r.Actor == "rider" && r.Kind == 6 && r.Index == 9) == 1, "The fixture capture lacks pet A's row.");
                service.SuspendArea();
                rider.PetId = "horse-2"; rider.PetResolves = false; // pet B linked during suspension, not resolving yet
                if (updateBeforeReplay) service.Frame();        // resume runs an update first
                service.Replay(rows);                           // then the transfer rows replay in the same world
                rider.PetResolves = true; service.Frame(); service.Frame();
                TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null,
                    "The replayed row gave pet A's slot 9 to pet B (updateBeforeReplay=" + updateBeforeReplay + ").");
                var saved = service.Save();
                TestRunner.True(!saved.Any(r => r.Actor == "rider" && r.Kind == 6), "The next save carried a slot row for pet B (updateBeforeReplay=" + updateBeforeReplay + ").");
                TestRunner.True(mounted.Find() == 5 && mounted.Count() == 1, "The mounted rider's slot changed.");
            }
            // Control: the same capture and replay with pet A's unchanged id briefly unresolved returns to slot 9 once.
            var same = new Service();
            PlacedAt(5, "mounted", same);
            var sameRider = PlacedAt(9, "rider", same);
            var sameRows = same.CaptureTransfer();
            same.SuspendArea();
            sameRider.PetResolves = false;
            same.Frame();
            same.Replay(sameRows);
            sameRider.PetResolves = true; same.Frame(); same.Frame();
            TestRunner.True(sameRider.Find() == 9 && sameRider.Count() == 1, "The same pet id lost slot 9 through the replay.");
            TestRunner.True(same.Save().Count(r => r.Actor == "rider" && r.Kind == 6 && r.Index == 9) == 1, "The same pet's save lost the row at 9.");
        }

        private static Rider StaleReserve(Service service, string petId = "horse-1")
        {
            // A fresh world: a reserve owner with its pet link, a native public-control binding left by an older save at 9,
            // no owned rows and no observation yet.
            var reserve = new Rider("reserve", service, service.Current) { InParty = false, Autofill = false, PetId = petId };
            reserve.Slots[0] = "Attack";
            reserve.Slots[9] = Control + "#0";
            return reserve;
        }

        private static void OlderCapitalSaveReserveBinding()
        {
            // Save before opening the rider's bar or returning it to the party (no update in between).
            var service = new Service();
            var reserve = StaleReserve(service);
            var rows = service.Save();
            TestRunner.True(rows.Count(r => r.Actor == "reserve" && r.Kind == 6 && r.Index == 9) == 1 &&
                rows.Count(r => r.Actor == "reserve" && r.Kind == 7) == 1 && rows.Count == 2,
                "The resave did not carry exactly the recovered slot row at 9 and one placement record.");
            TestRunner.Equal(0, service.SerializedKmcSlots, "A KMC binding was on a bar during serialization.");
            TestRunner.True(reserve.Find() == -1 && reserve.Slots[0] == "Attack", "The surviving binding stayed, or an unrelated slot changed.");
            // Cold return: the resave loaded into a new world, then the rider rejoins the party.
            var loaded = reserve.Load(rows);
            loaded.Frame();
            loaded.ToParty(); loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[0] == "Attack", "The cold return did not restore slot 9 once.");
            // In-world return after the first update admitted the binding.
            var inWorld = new Service();
            var returning = StaleReserve(inWorld);
            inWorld.Frame();
            TestRunner.True(returning.Find() == -1 && inWorld.Intent.Has("reserve"), "The first update did not recover and clear the binding.");
            returning.ToParty(); inWorld.Frame(); inWorld.Frame();
            TestRunner.True(returning.Find() == 9 && returning.Count() == 1, "The returning rider did not get slot 9 once.");
            // Disable before the rider rejoins clears the surviving binding and keeps unrelated slots.
            var disabled = new Service();
            var cleared = StaleReserve(disabled);
            disabled.Disable();
            TestRunner.True(cleared.Find() == -1 && cleared.Slots[0] == "Attack", "Disable left the surviving binding, or cleared an unrelated slot.");
            // Negative: a cleared pet link recovers nothing; the binding is still cleared.
            var noPet = new Service();
            var orphan = StaleReserve(noPet, null);
            var orphanRows = noPet.Save();
            TestRunner.True(orphanRows.Count == 0 && orphan.Find() == -1 && noPet.SerializedKmcSlots == 0,
                "A reserve binding without a companion was recovered or serialized.");
        }

        private static void OlderReserveBarMigration()
        {
            foreach (var chargeAlreadyBound in new[] { false, true })
            {
                // An unobserved reserve rider of an older save: legacy Mount at 3, legacy Mounted Charge at 5, the public
                // control at 9, its stock Charge available (optionally already at 7), no owned rows.
                var service = new Service();
                var reserve = new Rider("reserve", service, service.Current) { InParty = false, Autofill = false };
                reserve.Slots[0] = "Attack";
                reserve.Slots[3] = "KMC_Mount#0";
                reserve.Slots[5] = "KMC_Charge#0";
                reserve.Slots[9] = Control + "#0";
                if (chargeAlreadyBound) reserve.Slots[7] = "Charge";
                var rows = service.Save();                      // immediate city resave
                var label = " (chargeAlreadyBound=" + chargeAlreadyBound + ")";
                TestRunner.True(rows.Count(r => r.Actor == "reserve" && r.Kind == 6) == 1 && rows.Count(r => r.Actor == "reserve" && r.Kind == 6 && r.Index == 9) == 1 &&
                    rows.Count(r => r.Actor == "reserve" && r.Kind == 7) == 1, "The resave did not keep the public slot at 9 over the legacy alias" + label + ".");
                TestRunner.Equal(0, service.SerializedKmcSlots, "A KMC binding was on a bar during serialization" + label + ".");
                if (chargeAlreadyBound)
                    TestRunner.True(reserve.Slots[5] == null && reserve.Slots[7] == "Charge" && reserve.Slots.Count(s => s == "Charge") == 1,
                        "The existing stock Charge moved or was duplicated" + label + ".");
                else
                    TestRunner.True(reserve.Slots[5] == "Charge" && reserve.Slots.Count(s => s == "Charge") == 1,
                        "The legacy Mounted Charge did not become the stock Charge at 5" + label + ".");
                TestRunner.True(reserve.Slots[3] == null && reserve.Slots[0] == "Attack", "The legacy alias stayed, or an unrelated slot changed" + label + ".");
                // Reload and return: the public control at 9 once, the Charge where the migration put it.
                var loaded = reserve.Load(rows);
                loaded.Frame();
                loaded.ToParty(); loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[3] == null, "The return did not restore one public control at 9" + label + ".");
                TestRunner.True(loaded.Slots.Count(s => s == "Charge") == 1 && loaded.Slots[chargeAlreadyBound ? 7 : 5] == "Charge",
                    "The stock Charge did not survive the reload once" + label + ".");
            }
            // Fallback: with no public binding, the first exact legacy Mount / Dismount supplies the slot.
            var fallback = new Service();
            var aliasOnly = new Rider("alias", fallback, fallback.Current) { InParty = false, Autofill = false };
            aliasOnly.Slots[4] = "KMC_Dismount#0";
            var aliasRows = fallback.Save();
            TestRunner.True(aliasRows.Count(r => r.Kind == 6 && r.Index == 4) == 1 && aliasRows.Count(r => r.Kind == 6) == 1,
                "The legacy alias did not supply the slot when no public binding existed.");
            TestRunner.Equal(0, aliasOnly.KmcCount(), "The legacy alias stayed on the bar.");
            // No stock Charge available: the legacy Mounted Charge is only cleared.
            var noCharge = new Service();
            var bare = new Rider("bare", noCharge, noCharge.Current) { InParty = false, Autofill = false, HasStockCharge = false };
            bare.Slots[5] = "KMC_Charge#0";
            noCharge.Save();
            TestRunner.True(bare.Slots[5] == null && bare.KmcCount() == 0, "A legacy Mounted Charge without a stock Charge was not cleared.");
        }

        private static void FaultedRiderKeepsInterruptedSlot()
        {
            foreach (var reactivated in new[] { false, true })
            {
                // Pet A's control in slot 9; the pet link clears while the mounted / faulted relationship still grants
                // Dismount; native cleanup invalidates slot 9; the live control is reconciled.
                var rider = PlacedAt(9);
                rider.RelationshipGrants = true;
                rider.PetId = null; rider.PetResolves = false;
                rider.NativeInvalidate(reactivated);
                rider.Frame(); rider.Frame();
                var label = " (reactivated=" + reactivated + ")";
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The still-entitled rider lost slot 9 after native cleanup" + label + ".");
                var rows = rider.Save();
                TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 6) == 1,
                    "The save did not carry exactly one slot row at 9" + label + ".");
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The save scope changed the slot" + label + ".");
                // Entitlement ends with the link still cleared: the arrangement retires and the slot is cleared.
                rider.RelationshipGrants = false; rider.Frame(); rider.Frame();
                TestRunner.True(rider.Find() == -1 && !rider.Intent.Has(rider.Id), "A cleared link after entitlement ended kept the slot" + label + ".");
            }
            // Control: the same entitled rider whose link names a different companion still retires the old slot.
            var replaced = PlacedAt(9, "replaced");
            replaced.RelationshipGrants = true;
            replaced.PetId = "horse-2";
            replaced.NativeInvalidate();
            replaced.Frame(); replaced.Frame();
            TestRunner.Equal(-1, replaced.Find(), "A different companion inherited slot 9 while the relationship granted the control.");
        }

        // Bar slots for the identity rule: Blueprint null stands for an item or other non-ability slot.
        private sealed class BarSlot { internal object Blueprint; }

        private static void UnregisteredCleanupKeepsUnrelatedSlots()
        {
            var registeredState = new object();
            var registeredMount = new object();
            // Nothing registered, partially registered (public control only), and fully registered.
            var registrations = new[]
            {
                new object[] { null, null, null, null, null, null },
                new object[] { null, null, null, null, null, registeredState },
                new object[] { registeredMount, new object(), new object(), new object(), new object(), registeredState }
            };
            foreach (var r in registrations)
            {
                var item = new BarSlot { Blueprint = null };            // an item / non-ability shortcut
                var ordinary = new BarSlot { Blueprint = new object() }; // an ordinary ability
                var kmc = new BarSlot { Blueprint = registeredState };   // an exact registered KMC binding
                var bar = new[] { item, ordinary, kmc };
                // ClearManagedHotbarSlots / CaptureAndClearManagedHotbarSlots: only slots ResolveKind names are taken.
                for (var i = 0; i < bar.Length; i++)
                    if (MountedControlSlotPolicy.ResolveExactKind(bar[i].Blueprint, r[0], r[1], r[2], r[3], r[4], r[5]) != NativeMountedControlKind.None)
                        bar[i] = null;
                var label = " (registered=" + r.Count(x => x != null) + ")";
                TestRunner.True(ReferenceEquals(bar[0], item) && ReferenceEquals(bar[1], ordinary), "Cleanup replaced an item or an ordinary ability" + label + ".");
                if (r[5] != null) TestRunner.True(bar[2] == null, "Cleanup kept an exact registered KMC binding" + label + ".");
            }
            TestRunner.Equal(NativeMountedControlKind.MountCompanion,
                MountedControlSlotPolicy.ResolveExactKind(registeredMount, registeredMount, null, null, null, null, null),
                "An exact registered Mount binding was not identified.");
        }

        private static void LeaseReleaseAfterCompanionChange()
        {
            // Pet A's slot 9 is leased for serialization; the link changes to supported pet B before the scope ends.
            var rider = PlacedAt(9);
            rider.Save(() => { rider.PetId = "horse-2"; });
            TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null && rider.Slots[0] == "Attack",
                "The lease release restored pet A's slot 9 for pet B, or another slot changed.");
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "A later update returned the retired slot for pet B.");
            var rows = rider.Save();
            TestRunner.True(!rows.Any(r => r.Kind == 6), "The next archive kept a slot row for pet B.");
            // Control: pet A retained through the scope restores exactly one binding at 9.
            var same = PlacedAt(9, "same");
            same.Save(() => { });
            TestRunner.True(same.Find() == 9 && same.Count() == 1, "The same companion's lease did not restore slot 9 once.");
            TestRunner.True(same.Save().Count(r => r.Kind == 6 && r.Index == 9) == 1, "The same companion's next archive lost the row at 9.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] ArmLedger(MountedScanLedger<object> ledger)
        {
            var world = new object();
            var unit = new object();
            ledger.Synchronize(world, new[] { unit });
            TestRunner.True(ledger.MarkFirst(unit) && !ledger.MarkFirst(unit), "The first scan was not recorded exactly once.");
            return new[] { new WeakReference(world), new WeakReference(unit) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SyncLedger(MountedScanLedger<object> ledger, WeakReference world) =>
            ledger.Synchronize(world.Target, new object[0]); // the unit left the reserve roster; the world is current

        private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }

        private static void ReserveScanLedgerLifetime()
        {
            var ledger = new MountedScanLedger<object>();
            var refs = ArmLedger(ledger);
            SyncLedger(ledger, refs[0]);
            Collect();
            TestRunner.True(ledger.Count == 0 && !refs[1].IsAlive, "The ledger kept a unit that left the reserve roster.");
            TestRunner.True(ledger.HoldsWorld && refs[0].IsAlive, "The current world was dropped while it was still current.");
            ledger.Synchronize(null, null);                 // the world is disposed with no replacement player
            Collect();
            TestRunner.True(!ledger.HoldsWorld && !refs[0].IsAlive, "The ledger kept the disposed world alive.");
            // Control: repeated scans of an unchanged reserve bar never repeat the migration.
            var world = new object(); var unit = new object();
            var stable = new MountedScanLedger<object>();
            stable.Synchronize(world, new[] { unit });
            var first = stable.MarkFirst(unit);
            var repeats = 0;
            for (var i = 0; i < 3; i++) { stable.Synchronize(world, new[] { unit }); if (stable.MarkFirst(unit)) repeats++; }
            TestRunner.True(first && repeats == 0 && stable.Count == 1, "An unchanged reserve bar was scanned again.");
            // A new world starts afresh.
            stable.Synchronize(new object(), new[] { unit });
            TestRunner.True(stable.Count == 0 && stable.MarkFirst(unit), "A replaced world kept the previous world's scans.");
        }

        private static void LegacyAliasWhilePetUnresolved()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // An older native bar: legacy Mount alias at 9, no owned rows, the persistent pet id set but the pet unit
                // not resolving.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
                rider.Slots[0] = "Attack";
                rider.Slots[9] = "KMC_Mount#0";
                if (!saveFirst) { service.Frame(); service.Frame(); }
                var rows = service.Save();
                var label = " (saveFirst=" + saveFirst + ")";
                TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 6) == 1 && rows.Count(r => r.Kind == 7) == 1,
                    "The resave did not keep the alias index at 9" + label + ".");
                TestRunner.True(rider.KmcCount() == 0 && rider.Slots[0] == "Attack", "The alias stayed, or an unrelated slot changed" + label + ".");
                // The same pet resolves: the canonical control returns once at 9 (in this world, and after a reload).
                var loaded = rider.Load(rows);
                rider = loaded;
                rider.Frame();
                rider.PetResolves = true; rider.Frame(); rider.Frame();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The same pet's return did not restore the canonical control once" + label + ".");
            }
            // In-world return without a reload.
            var inWorld = new Service();
            var stays = new Rider("rider", inWorld, inWorld.Current) { PetResolves = false, Autofill = false };
            stays.Slots[9] = "KMC_Dismount#0";
            inWorld.Frame();
            stays.PetResolves = true; inWorld.Frame(); inWorld.Frame();
            TestRunner.True(stays.Find() == 9 && stays.Count() == 1, "The in-world return did not restore slot 9 once.");
            // A cleared link recovers nothing.
            var cleared = new Service();
            var orphan = new Rider("rider", cleared, cleared.Current) { PetId = null, PetResolves = false, Autofill = false };
            orphan.Slots[9] = "KMC_Mount#0";
            cleared.Frame();
            var orphanRows = cleared.Save();
            TestRunner.True(!orphanRows.Any(r => r.Kind == 6) && orphan.KmcCount() == 0, "A cleared link recovered the alias index.");
            orphan.PetId = "horse-1"; orphan.PetResolves = true; cleared.Frame(); cleared.Frame();
            TestRunner.Equal(-1, orphan.Find(), "A later pet received the alias slot recovered from a cleared link.");
        }

        // MountedArchiveData.ValidateSlots' key: actor and index (one placement record per actor).
        private static bool UniqueArchiveKeys(List<SavedRow> rows) =>
            rows.Select(r => MountedControlSlotPolicy.SavedRowKey(r.Actor, r.Index, r.Kind)).Distinct().Count() == rows.Count;

        private static void LiveAliasSaveFirst()
        {
            foreach (var alias in new[] { "KMC_Mount#1", "KMC_Dismount#1" })
            {
                // An eligible party rider, a live exact alias at 9 bound to its leased shell, no canonical binding and no
                // placement record; the archive header runs before any bar repair.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                rider.Slots[0] = "Attack";
                rider.Slots[9] = alias;
                var rows = service.Save();
                var label = " (" + alias + ")";
                TestRunner.True(UniqueArchiveKeys(rows), "The archive rows repeat an actor/index key" + label + ".");
                TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind) && r.Index == 9) == 1 && rows.Count(r => r.Kind == 7) == 1,
                    "The save did not hold one public binding at 9 and one placement record" + label + ".");
                service.Frame(); // the update after the scope migrates the restored alias
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.KmcCount() == 1, "The alias did not migrate to one public control at 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.FactLive = false;
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore one public binding at 9" + label + ".");
            }
        }

        private static void AliasLeaseAfterCompanionChange()
        {
            // Pet A temporarily unresolved, its older Mount alias at 9; save before the first update; pet B (eligible)
            // replaces A while the alias is leased; the scope releases; reconcile and resave.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
            rider.Slots[0] = "Attack";
            rider.Slots[9] = "KMC_Mount#0";
            rider.Save(() => { rider.PetId = "horse-2"; rider.PetResolves = true; });
            TestRunner.True(rider.Slots[9] == null && rider.KmcCount() == 0, "The leased alias came back for pet B.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == -1 && rider.KmcCount() == 0 && rider.Slots[0] == "Attack", "Pet B received pet A's slot 9.");
            var rows = service.Save();
            TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The resave carried a public binding for pet B.");
            // Control: pet A retained and now resolving gets exactly one public binding back at 9.
            var same = new Service();
            var kept = new Rider("rider", same, same.Current) { PetResolves = false, Autofill = false };
            kept.Slots[9] = "KMC_Mount#0";
            kept.Save(() => { kept.PetResolves = true; });
            same.Frame(); same.Frame();
            TestRunner.True(kept.Find() == 9 && kept.Count() == 1 && kept.KmcCount() == 1, "The same companion did not get one public binding at 9.");
            TestRunner.True(same.Save().Count(r => r.Kind == 6 && r.Index == 9) == 1, "The same companion's resave lost the row at 9.");
        }

        private static void AliasNativeCleanupFirst()
        {
            foreach (var alias in new[] { "KMC_Mount#0", "KMC_Dismount#0" })
            {
                // No owned rows, an unresolved linked pet, an inactive older alias at 9; native cleanup reaches it before
                // any update or capture and empties the slot.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
                rider.Slots[0] = "Attack";
                rider.Slots[9] = alias;
                rider.NativeInvalidateAlias();
                TestRunner.True(rider.Slots[9] == null, "The fixture did not empty the alias slot.");
                var label = " (" + alias + ")";
                var rows = service.Save();
                TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && UniqueArchiveKeys(rows), "The resave lost the alias index" + label + ".");
                rider.PetResolves = true; service.Frame(); service.Frame();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[0] == "Attack", "The same pet's return did not restore one public binding at 9" + label + ".");
            }
            // Control: a canonical binding elsewhere wins over the alias.
            var canonical = new Service();
            var both = new Rider("rider", canonical, canonical.Current) { PetResolves = false, Autofill = false };
            both.Slots[4] = Control + "#0";
            both.Slots[9] = "KMC_Mount#0";
            both.NativeInvalidateAlias();
            TestRunner.True(!canonical.Intent.Has("rider"), "The alias was recorded although a canonical binding was on the bar.");
            both.PetResolves = true; canonical.Frame(); canonical.Frame();
            TestRunner.True(both.Find() == 4 && both.Count() == 1 && both.Slots[9] == null, "The canonical binding did not win over the alias.");
            // Control: a cleared pet link recovers nothing.
            var cleared = new Service();
            var orphan = new Rider("rider", cleared, cleared.Current) { PetId = null, PetResolves = false, Autofill = false };
            orphan.Slots[9] = "KMC_Mount#0";
            orphan.NativeInvalidateAlias();
            TestRunner.True(!cleared.Save().Any(r => r.Kind == 6), "A cleared link recovered the alias index.");
            orphan.PetId = "horse-1"; orphan.PetResolves = true; cleared.Frame(); cleared.Frame();
            TestRunner.Equal(-1, orphan.Find(), "A later pet received the alias slot.");
            // Load fence: cleanup while a selected load fences the world, then the load is abandoned.
            var fenced = new Service();
            var outgoing = new Rider("rider", fenced, fenced.Current) { PetResolves = false, Autofill = false };
            outgoing.Slots[9] = "KMC_Dismount#0";
            var owner = new object();
            fenced.BeginLoad(owner);
            outgoing.NativeInvalidateAlias();
            TestRunner.True(!fenced.Intent.Has("rider"), "The fenced world wrote into the live records.");
            TestRunner.True(fenced.AbandonLoad(owner), "The owning scope could not abandon its load.");
            outgoing.PetResolves = true; fenced.Frame(); fenced.Frame();
            TestRunner.True(outgoing.Find() == 9 && outgoing.Count() == 1, "The abandoned load lost the alias index recorded while fenced.");
        }

        private static void MixedBarSaveFirst()
        {
            foreach (var stale in new[] { false, true })
            {
                // A party rider before migration: a callable older Mount at 3 (lower index), the canonical control at 9
                // (current data, or stale AbilityData from an earlier fact), unrelated slots around them.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                rider.Slots[0] = "Attack";
                rider.Slots[5] = "Potion";
                rider.Slots[3] = "KMC_Mount#1";
                rider.Slots[9] = stale ? Control + "#0" : rider.Data;
                var rows = service.Save();
                var label = " (stale=" + stale + ")";
                TestRunner.True(UniqueArchiveKeys(rows) && rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9, "The archive did not choose the canonical index 9" + label + ".");
                // The world that saved keeps the same choice after the scope and the next update.
                service.Frame();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[3] == null, "The saving world moved the control off 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[3] == null && loaded.KmcCount() == 1,
                    "The cold load did not restore one control at 9 with slot 3 empty" + label + ".");
                TestRunner.True(loaded.Slots[0] == "Attack" && loaded.Slots[5] == "Potion", "An unrelated slot changed" + label + ".");
            }
        }

        private static void LiveAliasCanonicalInactive()
        {
            foreach (var alias in new[] { "KMC_Mount#1", "KMC_Dismount#1" })
            {
                // The eligible rider's older shell is live while the canonical public fact is inactive or missing; no prior
                // placement record; the save header runs before migration.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = false, ShellLiveOverride = true, Autofill = false };
                rider.Slots[9] = alias;
                var rows = service.Save();
                var label = " (" + alias + ")";
                TestRunner.True(UniqueArchiveKeys(rows) && rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind) && r.Index == 9) == 1 &&
                    rows.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && rows.Count == 2,
                    "The archive did not hold one binding at 9 and one placement record" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore one control at 9" + label + ".");
            }
        }

        private static void AliasRemovedAfterLeaseRelease()
        {
            // The scope ends with the same pet and returns the alias at 9; the player removes it before the next update.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
            rider.Slots[0] = "Attack";
            rider.Slots[9] = "KMC_Mount#0";
            rider.Save(() => { rider.PetResolves = true; });
            TestRunner.True(Service.KindOf(rider.Slots[9]) == NativeMountedControlKind.MountCompanion, "The scope did not return the alias at 9.");
            rider.Slots[9] = null;                       // removed before migration
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == -1 && rider.KmcCount() == 0 && rider.Slots[0] == "Attack", "The next update put the removed shortcut back.");
            var rows = service.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord, "The save after the removal carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "Loading resurrected the removed shortcut.");
            // Control: the untouched returned alias migrates once at 9.
            var kept = new Service();
            var untouched = new Rider("rider", kept, kept.Current) { PetResolves = false, Autofill = false };
            untouched.Slots[9] = "KMC_Mount#0";
            untouched.Save(() => { untouched.PetResolves = true; });
            kept.Frame(); kept.Frame();
            TestRunner.True(untouched.Find() == 9 && untouched.Count() == 1 && untouched.KmcCount() == 1, "The untouched alias did not migrate once at 9.");
        }

        private static void StaleThenCurrentCanonical()
        {
            // A recorded rider saves before reconciliation with stale canonical data at 3 and the current data at 9.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
            service.Placed.Add("rider");
            rider.Slots[0] = "Attack";
            rider.Slots[3] = Control + "#0";
            rider.Slots[9] = rider.Data;
            var rows = service.Save();
            TestRunner.True(UniqueArchiveKeys(rows) && rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 3 && rows.Count(r => r.Kind == 7) == 1,
                "The save did not carry one binding at the selected index 3 and one placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 3 && loaded.Count() == 1 && loaded.Slots[0] == "Attack", "The load did not restore one control at 3.");
        }

        // A same-companion save scope that returns pet A's alias at 9 (A resolves during the scope).
        private static Rider ReturnedAlias(Service service)
        {
            var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
            rider.Slots[0] = "Attack";
            rider.Slots[9] = "KMC_Mount#0";
            rider.Save(() => { rider.PetResolves = true; });
            TestRunner.True(Service.KindOf(rider.Slots[9]) == NativeMountedControlKind.MountCompanion, "The scope did not return the alias at 9.");
            return rider;
        }

        private static void AliasSecondSave()
        {
            // Untouched: a second save before the next regular update, then reconcile and save once more.
            var service = new Service();
            var rider = ReturnedAlias(service);
            var second = service.Save();
            TestRunner.True(second.Count(r => r.Kind == 6 && r.Index == 9) == 1 && UniqueArchiveKeys(second), "The second save lost the alias index 9.");
            TestRunner.True(rider.Slots[9] != null && IsPublicSlot(rider.Slots[9]), "The second save did not return the shortcut to the continuing world.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.KmcCount() == 1, "The continuing world does not hold one control at 9.");
            var third = service.Save();
            TestRunner.True(third.Count(r => r.Kind == 6 && r.Index == 9) == 1, "The third save lost the row at 9.");
            var loaded = rider.Load(third);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore one control at 9.");
            // Removed before the second save: it stays removed.
            var removal = new Service();
            var remover = ReturnedAlias(removal);
            remover.Slots[9] = null;
            var removedRows = removal.Save();
            TestRunner.True(!removedRows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The second save carried a removed shortcut.");
            removal.Frame(); removal.Frame();
            TestRunner.True(remover.Find() == -1 && remover.KmcCount() == 0, "The removed shortcut came back in the continuing world.");
            var reloaded = remover.Load(removal.Save());
            reloaded.Frame(); reloaded.Frame();
            TestRunner.Equal(-1, reloaded.Find(), "The reload resurrected the removed shortcut.");
        }

        private static bool IsPublicSlot(string slot) => MountedControlSlotPolicy.IsPublicControlKind(Service.KindOf(slot));

        private static void AliasCompanionReplacement()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // Pet A's returned alias waits at 9; the link switches directly to eligible pet B before any repair.
                var service = new Service();
                var rider = ReturnedAlias(service);
                rider.Slots[5] = "Potion";
                rider.PetId = "horse-2";
                var label = " (saveFirst=" + saveFirst + ")";
                var rows = saveFirst ? service.Save() : null;
                service.Frame(); service.Frame();
                rows = rows ?? service.Save();
                TestRunner.True(rider.Find() == -1 && rider.KmcCount() == 0 && rider.Slots[9] == null, "Pet B inherited pet A's slot 9" + label + ".");
                TestRunner.True(rider.Slots[0] == "Attack" && rider.Slots[5] == "Potion", "An unrelated slot changed" + label + ".");
                TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The archive carried a binding for pet B" + label + ".");
            }
            // Control: the same pet keeps the returned alias, which migrates once at 9.
            var same = new Service();
            var kept = ReturnedAlias(same);
            same.Frame(); same.Frame();
            TestRunner.True(kept.Find() == 9 && kept.Count() == 1 && kept.KmcCount() == 1, "The same pet lost its returned shortcut.");
        }

        private static void NativeRebindOutcome()
        {
            // Native CleanupBadSlots rebinds the stale canonical slot at 9 to the live fact; the player removes the
            // repaired shortcut before the next service update.
            var rider = PlacedAt(9);
            rider.DataVersion++;                 // the fact was replaced: slot 9 now holds stale data
            rider.NativeCleanupBadSlots();       // native writes a new binding with the live fact's data
            TestRunner.True(rider.Slots[9] == rider.Data, "The fixture did not rebind slot 9.");
            rider.Slots[9] = null;
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The removed, natively repaired shortcut came back.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord, "The save after the removal carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "The load resurrected the removed shortcut.");
            // Control: native emptying (GetSlot) without a player edit returns one shortcut at 9.
            var emptied = PlacedAt(9, "emptied");
            emptied.DataVersion++;
            emptied.NativeInvalidate(reactivated: true);
            emptied.Frame(); emptied.Frame();
            TestRunner.True(emptied.Find() == 9 && emptied.Count() == 1, "Native emptying did not return one shortcut at 9.");
            // Control through the same classifier: CleanupBadSlots with no live fact empties the slot; the same pet's
            // return recovers one shortcut at 9.
            var inactive = PlacedAt(9, "inactive");
            inactive.PetResolves = false; inactive.FactLive = false;
            inactive.NativeCleanupBadSlots();
            TestRunner.True(inactive.Slots[9] == null, "The fixture did not empty slot 9.");
            inactive.Frame();
            inactive.PetResolves = true; inactive.Frame(); inactive.Frame();
            TestRunner.True(inactive.Find() == 9 && inactive.Count() == 1, "The emptied slot was not recovered once at 9.");
            // The classifier itself: a same-kind binding with live data is a rebind; an empty slot or another kind is not.
            TestRunner.True(MountedControlSlotPolicy.IsNativeRebind(NativeMountedControlKind.MountState, NativeMountedControlKind.MountState, true) &&
                !MountedControlSlotPolicy.IsNativeRebind(NativeMountedControlKind.MountState, NativeMountedControlKind.None, false) &&
                !MountedControlSlotPolicy.IsNativeRebind(NativeMountedControlKind.MountState, NativeMountedControlKind.MountState, false) &&
                !MountedControlSlotPolicy.IsNativeRebind(NativeMountedControlKind.MountCompanion, NativeMountedControlKind.MountState, true),
                "The native rebind classifier misjudged an outcome.");
        }

        private static void ReturnedAliasNativeEmptying()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // The same pet's alias was returned at 9 and recorded live; its shell goes stale and native GetSlot
                // empties it before migration, while the canonical control stays available.
                var service = new Service();
                var rider = ReturnedAlias(service);
                rider.ShellLiveOverride = false;
                rider.NativeInvalidateAlias();
                TestRunner.True(rider.Slots[9] == null, "The fixture did not empty the alias.");
                var label = " (saveFirst=" + saveFirst + ")";
                var rows = saveFirst ? service.Save() : null;
                service.Frame(); service.Frame();
                rows = rows ?? service.Save();
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The natively emptied alias did not return once at 9" + label + ".");
                TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind) && r.Index == 9) == 1 && UniqueArchiveKeys(rows),
                    "The archive lost the chosen index 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore one binding at 9" + label + ".");
            }
            // Control: the player removes the returned alias; it stays removed.
            var removal = new Service();
            var remover = ReturnedAlias(removal);
            remover.Slots[9] = null;
            removal.Frame(); removal.Frame();
            TestRunner.Equal(-1, remover.Find(), "A deliberate removal of the returned alias was undone.");
        }

        private static void SaveReleaseNoAutofillCopy()
        {
            // One manual canonical binding at 9, no placement record, autofill on, lower main-bar slots empty; the save
            // runs before ordinary reconciliation.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = true };
            rider.Slots[9] = rider.Data;
            service.Save();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The save release left more or less than one shortcut at 9.");
            service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The first update after the save added a copy.");
            service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The second update after the save added a copy.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6 && r.Index == 9) == 1 && rows.Count(r => r.Kind == 6) == 1 && rows.Count(r => r.Kind == 7) == 1,
                "The next save did not carry one shortcut at 9 and one placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore exactly one shortcut at 9.");
        }

        private static void RememberedSlotOccupied()
        {
            // Interrupted intent at 9 (native cleanup emptied it), then a callable older Mounted Charge occupies 9; the save
            // runs before reconciliation.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            rider.NativeInvalidate();
            rider.Slots[9] = "KMC_Charge#0";
            rider.ChargeLive = true;
            var rows = service.Save();
            TestRunner.True(UniqueArchiveKeys(rows), "The archive repeats an actor/index key.");
            TestRunner.True(rows.Count(r => r.Index == 9 && r.Kind == (int)NativeMountedControlKind.MountedCharge) == 1 &&
                !rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) && rows.Count(r => r.Kind == 7) == 1,
                "The archive did not keep the Charge at 9 with the shortcut off the bar and one placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "A mount shortcut was restored over the occupied slot.");
        }

        private static void DeferredCleanupAcrossWorlds()
        {
            // Native cleanup queues an invalidation for outgoing rider A at 9 and exits without its postfix; the player
            // world is replaced by a save in which the shortcut was removed, then the pending work is resolved.
            var service = new Service();
            var outgoing = PlacedAt(9, "rider", service);
            service.QueueNativeInvalidation(outgoing);
            var incoming = outgoing.Load(new List<SavedRow> { new SavedRow { Actor = "rider", Index = 0, Kind = 7 } });
            service.ResolvePendingNative();
            incoming.Frame(); incoming.Frame();
            TestRunner.Equal(-1, incoming.Find(), "The outgoing world's deferred cleanup restored a slot in the loaded world.");
            TestRunner.True(!service.Save().Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The loaded world's archive took the outgoing slot.");
            // Control: the load is abandoned and the outgoing world survives: the deferred cleanup still applies there.
            var kept = new Service();
            var rider = PlacedAt(9, "rider", kept);
            var owner = new object();
            kept.BeginLoad(owner);
            kept.QueueNativeInvalidation(rider);
            TestRunner.True(kept.AbandonLoad(owner), "The owning scope could not abandon its load.");
            kept.ResolvePendingNative();
            kept.Frame(); kept.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The surviving world lost the slot native cleanup emptied.");
        }

        private static void FirstManualCleanupNoAutofill()
        {
            // A manually placed canonical shortcut at 9 (no placement record, autofill on); the pet becomes unavailable and
            // native cleanup empties the slot before any regular observation; the player puts another ability at 9.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = true };
            rider.Slots[9] = rider.Data;
            rider.PetResolves = false;
            rider.NativeInvalidate();
            rider.Slots[9] = "Fireball";
            service.Frame();
            rider.PetResolves = true; service.Frame(); service.Frame();
            TestRunner.True(rider.Slots[9] == "Fireball" && rider.Count() == 0, "The shortcut came back elsewhere or over the new ability.");
            var rows = service.Save();
            TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) && rows.Count(r => r.Kind == 7) == 1,
                "The save carried a shortcut or no placement record.");
            service.Frame();
            TestRunner.True(rider.Count() == 0 && rider.Slots[9] == "Fireball", "The save scope added a shortcut.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Count() == 0 && loaded.Slots[9] == "Fireball", "The reload added a shortcut or lost the ability.");
        }

        private static void MixedCleanupPassKeepsCanonical()
        {
            // A stale canonical binding at 4 and a stale older Mount alias at 9, neither fact live; one native cleanup pass
            // empties 4 and then 9.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
            rider.Slots[4] = Control + "#0";
            rider.Slots[9] = "KMC_Mount#0";
            rider.NativeCleanupPass();
            TestRunner.True(rider.Slots[4] == null && rider.Slots[9] == null, "The fixture did not empty both slots.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 4, "The save did not keep the canonical index 4.");
            var loaded = rider.Load(rows);
            loaded.Frame();
            loaded.PetResolves = true; loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 4 && loaded.Count() == 1 && loaded.Slots[9] == null, "The reload did not restore one binding at 4.");
        }

        private static void OccupiedSlotGivenUpEverywhere()
        {
            foreach (var reload in new[] { false, true })
            {
                // The same pet is temporarily unavailable and its interrupted slot 9 holds another ability at the save.
                var service = new Service();
                var rider = PlacedAt(9, "rider", service);
                rider.PetResolves = false;
                rider.NativeInvalidate();
                rider.Slots[9] = "Fireball";
                var rows = service.Save();
                TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) && rows.Count(r => r.Kind == 7) == 1,
                    "The save did not record the shortcut off the bar.");
                var current = reload ? rider.Load(rows) : rider;
                current.Frame();
                current.Slots[9] = null;               // the player clears the occupant before the pet returns
                current.PetResolves = true; current.Frame(); current.Frame();
                TestRunner.Equal(-1, current.Find(), "The given-up shortcut came back (reload=" + reload + ").");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference BeginFence(MountedControlLoadFence fence, bool controlsActive)
        {
            var world = new object();
            fence.Begin(world, new object(), new MountedControlPlacementIntent(), new HashSet<string>(), controlsActive);
            return new WeakReference(world);
        }

        private static void DisabledLifecycle()
        {
            // The production fence for a disabled service arms nothing and holds no outgoing world.
            var fence = new MountedControlLoadFence();
            var outgoing = BeginFence(fence, false);
            Collect();
            TestRunner.True(!fence.Fenced && !outgoing.IsAlive, "A disabled service's load fence kept the outgoing world.");
            var enabledFence = new MountedControlLoadFence();
            var held = BeginFence(enabledFence, true);
            Collect();
            TestRunner.True(enabledFence.Fenced && held.IsAlive, "An enabled service's load fence did not hold the outgoing world.");
            // A disabled service completes a load from world A to world B without arming a fence.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            var rows = service.Save();
            service.Disable();
            rider.Load(rows);
            TestRunner.True(!service.Fence.Fenced, "The load while disabled left a fence on the outgoing world.");
            // Native cleanup while disabled records nothing.
            var quiet = new Service();
            var alias = new Rider("alias", quiet, quiet.Current) { PetResolves = false, Autofill = false };
            var canonical = new Rider("canonical", quiet, quiet.Current) { Autofill = false };
            quiet.Disable();
            alias.Slots[9] = "KMC_Mount#0";             // a surviving legacy slot seen while disabled
            canonical.FactLive = true;
            canonical.Slots[3] = canonical.Data;
            alias.NativeInvalidateAlias();
            canonical.NativeInvalidate();
            TestRunner.True(!quiet.Intent.Has("alias") && !quiet.Intent.Has("canonical") && quiet.Placed.Count == 0,
                "Native cleanup while disabled created a placement record.");
        }

        private static void FencedRebindAbandoned()
        {
            // Interrupted intent with stale data at 9; a selected load fences the world; native cleanup rebinds the slot;
            // the load is abandoned; the player removes the repaired shortcut before reconciliation.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            rider.DataVersion++;
            service.Intent.ObserveAbsent("rider", MountedControlAbsence.Temporary);
            var owner = new object();
            service.BeginLoad(owner);
            rider.NativeCleanupBadSlots();
            TestRunner.True(rider.Slots[9] == rider.Data && !service.Intent.Has("rider"), "The fixture did not rebind while fenced, or wrote live records.");
            TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
            rider.Slots[9] = null;
            service.Frame(); service.Frame();
            TestRunner.Equal(-1, rider.Find(), "The abandoned load reversed the removal of the repaired shortcut.");
            var rows = service.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save after the removal carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "The reload resurrected the removed shortcut.");
            // Isolation control: the same fenced rebind followed by a completed replacement leaves the new world its own.
            var replaced = new Service();
            var outgoing = PlacedAt(9, "rider", replaced);
            outgoing.DataVersion++;
            replaced.BeginLoad(new object());
            outgoing.NativeCleanupBadSlots();
            var incoming = new World();
            var reserve = new Rider("rider", replaced, incoming) { InParty = false, Autofill = false };
            replaced.InstallAndRestore(incoming, new List<SavedRow>());
            reserve.ToParty(); replaced.Frame(); replaced.Frame();
            TestRunner.Equal(-1, reserve.Find(), "The completed replacement took the outgoing world's rebind.");
        }

        private static Rider MixedOlderBar(Service service)
        {
            // A stale canonical binding at 4 and a stale older Mount alias at 9; the pet temporarily does not resolve.
            var rider = new Rider("rider", service, service.Current) { PetResolves = false, Autofill = false };
            rider.Slots[4] = Control + "#0";
            rider.Slots[9] = "KMC_Mount#0";
            return rider;
        }

        private static void SaveBetweenNativeReads()
        {
            // Native GetSlot empties only the canonical slot 4; a save starts before native reads the alias at 9.
            var service = new Service();
            var rider = MixedOlderBar(service);
            rider.NativeInvalidateAt(4);
            TestRunner.True(rider.Slots[4] == null && Service.KindOf(rider.Slots[9]) == NativeMountedControlKind.MountCompanion, "The fixture is not 4 emptied, 9 surviving.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 4, "The save replaced the canonical index 4 with the alias.");
            var loaded = rider.Load(rows);
            loaded.Frame();
            loaded.PetResolves = true; loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 4 && loaded.Count() == 1 && loaded.Slots[9] == null, "The reload did not restore one shortcut at 4.");
            // Controls: the full pass (MixedCleanupPassKeepsCanonical) and the alias-only bar (AliasNativeCleanupFirst)
            // still hold; the continuing world returns the shortcut at 4 too.
            var continuing = new Service();
            var stays = MixedOlderBar(continuing);
            stays.NativeInvalidateAt(4);
            continuing.Save();
            stays.PetResolves = true; continuing.Frame(); continuing.Frame();
            TestRunner.True(stays.Find() == 4 && stays.Count() == 1, "The continuing world did not return one shortcut at 4.");
        }

        private static void TwoCanonicalCleanup()
        {
            foreach (var order in new[] { "pass", "3-then-9", "9-then-3" })
            {
                // Canonical bindings at 3 and 9; the same pet becomes temporarily unavailable; native cleanup empties both
                // before capture.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = rider.Data;
                rider.Slots[9] = rider.Data;
                rider.PetResolves = false; rider.FactLive = false;
                if (order == "pass") rider.NativeCleanupPass();
                else if (order == "3-then-9") { rider.NativeInvalidateAt(3); rider.NativeInvalidateAt(9); }
                else { rider.NativeInvalidateAt(9); rider.NativeInvalidateAt(3); }
                TestRunner.True(rider.Slots[3] == null && rider.Slots[9] == null, "The fixture did not empty both slots (" + order + ").");
                var rows = service.Save();
                TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 3, "The save did not keep the first canonical index 3 (" + order + ").");
                var loaded = rider.Load(rows);
                loaded.Frame();
                loaded.PetResolves = true; loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 3 && loaded.Count() == 1, "The reload did not restore one shortcut at 3 (" + order + ").");
            }
            // Control: a single manually moved shortcut (live entry at 9, moved to 3 unobserved) is still recognized.
            var moved = PlacedAt(9, "moved");
            var data = moved.Slots[9]; moved.Slots[9] = null; moved.Slots[3] = data;
            moved.PetResolves = false; moved.FactLive = false;
            moved.NativeInvalidateAt(3);
            moved.PetResolves = true; moved.Frame(); moved.Frame();
            TestRunner.True(moved.Find() == 3 && moved.Count() == 1, "The moved shortcut did not return at its new slot 3.");
        }

        private static void DisabledSaveIsolation()
        {
            // KMC is disabled; an older world has a surviving canonical shortcut at 9 on a party rider (no fact behind it)
            // and the player saves before native UI cleanup.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { Autofill = false };
            service.Disable();
            rider.Slots[9] = Control + "#0";
            rider.Slots[0] = "Attack";
            service.Save();
            TestRunner.True(rider.Slots[9] == null && rider.Slots[0] == "Attack", "Disabled save sanitation left the binding or changed another slot.");
            TestRunner.True(service.Intent.Count == 0 && service.Placed.Count == 0, "Disabled save sanitation recorded placement state.");
            // Another world with the same rider and pet ids and no shortcut rows is loaded while still disabled; then KMC is
            // enabled with autofill off.
            service.Intent.Seed("rider", 9, true);   // stray outgoing state a disabled load must not carry
            service.Placed.Add("rider");
            var loaded = rider.Load(new List<SavedRow>());
            TestRunner.True(service.Intent.Count == 0 && service.Placed.Count == 0 && !service.Fence.Fenced,
                "The disabled load carried outgoing placement state or armed a fence.");
            service.Enabled = true;
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == -1 && loaded.Slots[9] == null, "The previous world's shortcut appeared in the loaded world.");
        }

        private static void SurvivingAliasAfterCanonicalCleanup()
        {
            // GetSlot empties the stale canonical binding at 4; the older alias at 9 survives; the same pet resolves and
            // KMC updates before any save.
            var service = new Service();
            var rider = MixedOlderBar(service);
            rider.NativeInvalidateAt(4);
            rider.PetResolves = true;
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 4 && rider.Count() == 1 && rider.Slots[9] == null && rider.KmcCount() == 1,
                "Migration made the surviving alias the chosen slot instead of 4.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 4, "The resave did not carry one public row at 4.");
            // Capture while the surviving alias is callable (its shell live) before any update.
            var capture = new Service();
            var callable = MixedOlderBar(capture);
            callable.NativeInvalidateAt(4);
            callable.PetResolves = true; callable.FactLive = true; callable.ShellLiveOverride = true;
            var captured = capture.Save();
            TestRunner.True(captured.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                captured.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 4 && UniqueArchiveKeys(captured),
                "Capture wrote the callable alias instead of the canonical choice 4.");
            capture.Frame(); capture.Frame();
            TestRunner.True(callable.Find() == 4 && callable.Count() == 1 && callable.Slots[9] == null, "The continuing world did not settle on one shortcut at 4.");
            // Control: an alias-only bar still migrates in place.
            var aliasOnly = new Service();
            var only = new Rider("rider", aliasOnly, aliasOnly.Current) { PetResolves = false, Autofill = false };
            only.Slots[9] = "KMC_Mount#0";
            aliasOnly.Frame();
            only.PetResolves = true; aliasOnly.Frame(); aliasOnly.Frame();
            TestRunner.True(only.Find() == 9 && only.Count() == 1, "An alias-only bar did not migrate at 9.");
        }

        private static void SurvivingCanonicalCopyAfterCleanup()
        {
            foreach (var saveFirst in new[] { true, false })
            {
                // Canonical bindings at 3 and 9; the same pet becomes unavailable; GetSlot empties 3 only.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = rider.Data;
                rider.Slots[9] = rider.Data;
                rider.PetResolves = false; rider.FactLive = false;
                rider.NativeInvalidateAt(3);
                var label = " (saveFirst=" + saveFirst + ")";
                if (saveFirst)
                {
                    var rows = service.Save();
                    TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The save between the reads recorded the secondary index" + label + ".");
                    var loaded = rider.Load(rows);
                    loaded.Frame();
                    loaded.PetResolves = true; loaded.Frame(); loaded.Frame();
                    TestRunner.True(loaded.Find() == 3 && loaded.Count() == 1, "The reload did not restore one shortcut at 3" + label + ".");
                }
                else
                {
                    rider.PetResolves = true; service.Frame(); service.Frame();
                    TestRunner.True(rider.Find() == 3 && rider.Count() == 1 && rider.Slots[9] == null, "The return did not settle on one shortcut at 3" + label + ".");
                    var rows = service.Save();
                    TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The resave did not carry 3" + label + ".");
                }
            }
        }

        private static void InitialPlacementCompanionChange()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // An eligible rider gets its first automatic placement for pet A; the link switches directly to eligible
                // pet B before the next reconciliation.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { Autofill = true };
                rider.Slots[0] = "Attack";
                service.Frame();
                var placedAt = rider.Find();
                TestRunner.True(placedAt >= 0, "The fixture did not auto-place the control.");
                rider.PetId = "horse-2";
                var label = " (saveFirst=" + saveFirst + ")";
                var rows = saveFirst ? service.Save() : null;
                service.Frame(); service.Frame();
                rows = rows ?? service.Save();
                TestRunner.True(rider.Find() == -1 && rider.Slots[0] == "Attack", "Pet B inherited pet A's automatic placement" + label + ".");
                TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The archive carried a public row for pet B" + label + ".");
            }
            // Control: the same pet A temporarily unavailable keeps its automatic placement and gets it back once.
            var same = new Service();
            var kept = new Rider("rider", same, same.Current) { Autofill = true };
            same.Frame();
            var at = kept.Find();
            kept.PetResolves = false; same.Frame();
            kept.PetResolves = true; same.Frame(); same.Frame();
            TestRunner.True(at >= 0 && kept.Find() == at && kept.Count() == 1, "The same pet lost its automatic placement.");
        }

        private static void AliasRebindAfterCanonicalCleanup()
        {
            // GetSlot empties the canonical slot 4; before KMC reconciles, native CleanupBadSlots repairs the older alias at
            // 9 with a live shell fact.
            var service = new Service();
            var rider = MixedOlderBar(service);
            rider.NativeInvalidateAt(4);
            rider.PetResolves = true; rider.FactLive = true;
            rider.NativeCleanupRebindAlias();
            TestRunner.True(Service.KindOf(rider.Slots[9]) == NativeMountedControlKind.MountCompanion, "The fixture did not rebind the alias at 9.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 4 && rider.Count() == 1 && rider.Slots[9] == null, "Reconciliation promoted the rebound alias instead of 4.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 4, "The save did not carry 4.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 4 && loaded.Count() == 1, "The reload did not restore one shortcut at 4.");
        }

        private static void LiveCopyAfterCanonicalCleanup()
        {
            // StaleThenCurrentCanonical's bar: stale data at 3, current data at 9; GetSlot empties 3 just before the save.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
            service.Placed.Add("rider");
            rider.Slots[0] = "Attack";
            rider.Slots[3] = Control + "#0";
            rider.Slots[9] = rider.Data;
            rider.NativeInvalidateAt(3);
            var rows = service.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The save took the live copy at 9 instead of 3.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 3 && rider.Count() == 1 && rider.Slots[9] == null, "The continuing world did not settle on one shortcut at 3.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 3 && loaded.Count() == 1 && loaded.Slots[0] == "Attack", "The reload did not restore one shortcut at 3.");
            // Control: a genuinely new placement made after the cleanup (live data, not a recorded copy) is the player's.
            var newer = new Service();
            var placer = PlacedAt(3, "placer", newer);
            placer.DataVersion++;                       // the fact was replaced: slot 3 is stale
            placer.NativeInvalidateAt(3);               // GetSlot empties it (no other public binding on the bar)
            placer.Slots[9] = placer.Data;              // the player drags the live control to 9
            var newerRows = newer.Save();
            TestRunner.True(newerRows.Count(r => r.Kind == 6) == 1 && newerRows.Single(r => r.Kind == 6).Index == 9, "A new player placement at 9 was not kept.");
        }

        private static void OccupiedChosenSlotWithCopy()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // GetSlot empties 4 on the mixed bar; the player puts Fireball at 4; the same pet returns.
                var service = new Service();
                var rider = MixedOlderBar(service);
                rider.NativeInvalidateAt(4);
                rider.Slots[4] = "Fireball";
                rider.PetResolves = true;
                var label = " (saveFirst=" + saveFirst + ")";
                List<SavedRow> rows;
                if (saveFirst) { rows = service.Save(); service.Frame(); service.Frame(); }
                else { service.Frame(); service.Frame(); rows = service.Save(); }
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[4] == "Fireball", "The continuing world did not keep one shortcut at 9" + label + ".");
                TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9, "The save did not carry 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[4] == "Fireball", "The reload did not match the continuing world" + label + ".");
                var resave = service.Save();
                TestRunner.True(resave.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    resave.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9, "The resave did not carry 9" + label + ".");
            }
        }

        private static void StandaloneIsBadQuery()
        {
            // A stale canonical slot at 9 with a live replacement fact is queried directly (IsBad outside GetSlot /
            // CleanupBadSlots) and left in place; the player then removes it before reconciliation.
            var rider = PlacedAt(9);
            rider.DataVersion++;
            rider.QueryIsBadOnly();
            TestRunner.True(rider.Slots[9] != null, "The fixture did not leave the slot in place.");
            rider.Slots[9] = null;
            rider.Frame(); rider.Frame();
            TestRunner.Equal(-1, rider.Find(), "The removal was undone after a standalone IsBad query.");
            var rows = rider.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save after the removal carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "The reload resurrected the removed shortcut.");
            // Control: an actual GetSlot emptying is recovered once.
            var emptied = PlacedAt(9, "emptied");
            emptied.DataVersion++;
            emptied.ReportBad(9, insideNativeOperation: true);
            emptied.Slots[9] = null;
            emptied.Frame(); emptied.Frame();
            TestRunner.True(emptied.Find() == 9 && emptied.Count() == 1, "An actual native emptying was not recovered.");
        }

        private static void FencedRebindCompanionChange()
        {
            // A's recorded shortcut at 9 (interrupted, stale data); the rider now links to eligible pet B; a selected load
            // fences the world; native cleanup rebinds the slot; the load is abandoned.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            rider.Slots[0] = "Attack";
            rider.DataVersion++;
            service.Intent.ObserveAbsent("rider", MountedControlAbsence.Temporary);
            rider.PetId = "horse-2";
            var owner = new object();
            service.BeginLoad(owner);
            rider.NativeCleanupBadSlots();
            TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == -1 && rider.Slots[9] == null && rider.Slots[0] == "Attack", "Pet B adopted pet A's retired shortcut.");
            var rows = service.Save();
            TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The save carried a public row for pet B.");
        }

        private static void NewPlacementInFormerCopySlot()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // Stale canonical bindings at 3 and 9; GetSlot empties 3 and then 9; before reconciliation the player
                // drags the current control into the now-empty slot 9.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = Control + "#0";
                rider.Slots[9] = Control + "#0";
                rider.NativeInvalidateAt(3);
                rider.NativeInvalidateAt(9);
                rider.Slots[9] = rider.Data;
                var label = " (saveFirst=" + saveFirst + ")";
                List<SavedRow> rows;
                if (saveFirst) { rows = service.Save(); service.Frame(); service.Frame(); }
                else { service.Frame(); service.Frame(); rows = service.Save(); }
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The player's placement at 9 was discarded" + label + ".");
                TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 9, "The save did not carry 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore one shortcut at 9" + label + ".");
            }
        }

        private static void FallbackCleanupWithOccupiedChoice()
        {
            // Mixed older bar: GetSlot empties 4, the player puts Fireball at 4, GetSlot empties the surviving stale alias at
            // 9 before any update or save header; the same pet returns.
            var service = new Service();
            var rider = MixedOlderBar(service);
            rider.NativeInvalidateAt(4);
            rider.Slots[4] = "Fireball";
            rider.NativeInvalidateAliasAt(9);
            TestRunner.True(rider.Slots[9] == null, "The fixture did not empty the alias.");
            rider.PetResolves = true;
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[4] == "Fireball", "The fallback at 9 was lost or Fireball moved.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9, "The save did not carry 9.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[4] == "Fireball", "The reload did not restore 9 with Fireball at 4.");
            var resave = service.Save();
            TestRunner.True(resave.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                resave.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9, "The resave did not carry 9.");
        }

        private static void AbortedNativeOperation()
        {
            // A native operation throws before its postfix; a standalone IsBad read on the stale shortcut at 9 falls inside
            // the still-open scope; the player clears 9 before the next update.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            rider.DataVersion++;
            rider.AbortedOperationThenQuery();
            rider.Slots[9] = null;
            service.DiscardAbortedNativeReports();   // the next update closes the scope and drops its reports
            service.Frame(); service.Frame();
            TestRunner.Equal(-1, rider.Find(), "The aborted operation's report restored the removed shortcut.");
            var rows = service.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "The reload resurrected the removed shortcut.");
            // Control: a completed native emptying is recovered once.
            var completed = PlacedAt(9, "completed");
            completed.DataVersion++;
            completed.ReportBad(9, insideNativeOperation: true);
            completed.Slots[9] = null;
            completed.Frame(); completed.Frame();
            TestRunner.True(completed.Find() == 9 && completed.Count() == 1, "A completed native emptying was not recovered.");
        }

        private static void RetiredRiderCleanupBatch()
        {
            foreach (var fenced in new[] { true, false })
            {
                // A's shortcut at 3 with a copy at 9; the rider now links to pet B (not yet available); one CleanupBadSlots
                // pass empties both stale bindings with no active replacement fact.
                var service = new Service();
                var rider = PlacedAt(3, "rider", service);
                rider.Autofill = false;
                rider.Slots[9] = rider.Data;
                rider.PetId = "horse-2"; rider.PetResolves = false; rider.FactLive = false;
                var owner = new object();
                if (fenced) service.BeginLoad(owner);
                rider.NativeCleanupPass();
                if (fenced) TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
                rider.PetResolves = true;
                service.Frame(); service.Frame();
                var label = " (fenced=" + fenced + ")";
                TestRunner.True(rider.Find() == -1 && rider.Slots[3] == null && rider.Slots[9] == null && rider.Slots[0] == "Attack",
                    "Pet B received pet A's shortcut, or another slot changed" + label + ".");
                var rows = service.Save();
                TestRunner.True(!rows.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The save carried a public row for pet B" + label + ".");
            }
        }

        private static void RepairRemovesAllCopies()
        {
            // The chosen stale binding at 3 and two live canonical copies at 7 and 9; GetSlot empties 3.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
            service.Placed.Add("rider");
            rider.Slots[3] = Control + "#0";
            rider.Slots[7] = rider.Data;
            rider.Slots[9] = rider.Data;
            rider.NativeInvalidateAt(3);
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 3 && rider.Count() == 1, "Repair left more than one shortcut, or not at 3.");
            var rows = service.Save();
            TestRunner.True(rows.Count(r => r.Kind == 6) == 1 && rows.Single(r => r.Kind == 6).Index == 3, "The save did not carry one row at 3.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 3 && loaded.Count() == 1, "The reload did not restore one shortcut at 3.");
            loaded.Slots[3] = null;                    // the player removes it
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == -1 && loaded.Count() == 0, "The removal did not stick.");
            var after = service.Save();
            TestRunner.True(!after.Any(r => r.Kind == 6), "The save after the removal carried a slot row.");
        }

        private static void SecondaryAliasReport()
        {
            // An older bar with a recorded live Mount alias at 4, a bad Dismount alias at 9 and no canonical binding. GetSlot
            // reads only 9; the player then removes 4.
            var service = new Service();
            var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
            service.Placed.Add("rider");
            rider.Slots[4] = "KMC_Mount#1";
            rider.Slots[9] = "KMC_Dismount#0";
            service.Intent.ObserveBound("rider", 4, "horse-1");
            rider.NativeInvalidateAliasAt(9);
            rider.Slots[4] = null;
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == -1 && rider.KmcCount() == 0, "The removal of the selected alias was undone.");
            var rows = service.Save();
            TestRunner.True(rows.Count == 1 && rows[0].Kind == 7, "The save after the removal carried more than the placement record.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.Equal(-1, loaded.Find(), "The reload resurrected the removed shortcut.");
            // Control: GetSlot actually emptying the selected alias at 4 is recovered.
            var recovered = new Service();
            var kept = new Rider("rider", recovered, recovered.Current) { FactLive = true, Autofill = false };
            recovered.Placed.Add("rider");
            kept.Slots[4] = "KMC_Mount#0";
            recovered.Intent.ObserveBound("rider", 4, "horse-1");
            kept.NativeInvalidateAliasAt(4);
            recovered.Frame(); recovered.Frame();
            TestRunner.True(kept.Find() == 4 && kept.Count() == 1, "An actual emptying of the selected alias was not recovered.");
        }

        private static int PublicRowIndex(List<SavedRow> rows) =>
            rows.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 ? rows.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index : -2;

        private static bool MarkerOnly(List<SavedRow> rows) => rows.Count == 1 && rows[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord;

        private static void OutOfOrderCopyReads()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // Stale canonical bindings at 3, 7 and 9; native GetSlot reads 3, then 9, then 7; the player then places the
                // current control at 9.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = Control + "#0";
                rider.Slots[7] = Control + "#0";
                rider.Slots[9] = Control + "#0";
                rider.NativeInvalidateAt(3);
                rider.NativeInvalidateAt(9);
                rider.NativeInvalidateAt(7);
                TestRunner.True(rider.Count() == 0, "The fixture did not empty all three bindings.");
                rider.Slots[9] = rider.Data;
                var label = " (saveFirst=" + saveFirst + ")";
                List<SavedRow> rows;
                if (saveFirst) { rows = service.Save(); service.Frame(); service.Frame(); }
                else { service.Frame(); service.Frame(); rows = service.Save(); }
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The placement at 9 was undone or duplicated" + label + ".");
                TestRunner.Equal(9, PublicRowIndex(rows), "The save did not carry exactly 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The cold restore did not give one shortcut at 9" + label + ".");
            }
        }

        private static void AbandonedLoadFallbackOccupied()
        {
            // Mixed older bar: GetSlot empties 4, the player puts Fireball at 4, a selected load starts, native empties the
            // alias at 9 while the outgoing records are fenced, and the load is abandoned.
            var service = new Service();
            var rider = MixedOlderBar(service);
            rider.NativeInvalidateAt(4);
            rider.Slots[4] = "Fireball";
            var owner = new object();
            service.BeginLoad(owner);
            rider.NativeInvalidateAliasAt(9);
            TestRunner.True(rider.Slots[9] == null, "The fixture did not empty the alias.");
            TestRunner.True(service.AbandonLoad(owner), "The owning scope could not abandon its load.");
            rider.PetResolves = true;
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[4] == "Fireball", "The fallback at 9 was lost or Fireball moved.");
            var rows = service.Save();
            TestRunner.Equal(9, PublicRowIndex(rows), "The save did not carry 9.");
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[4] == "Fireball", "The save scope changed the bar.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[4] == "Fireball", "The reload did not restore 9 with Fireball at 4.");
        }

        private static void ExceptionalNativeOperations()
        {
            // An operation reports the stale slot at 9 and throws before writing it or returning; a successful GetSlot(9)
            // follows before the next update.
            var service = new Service();
            var rider = PlacedAt(9, "rider", service);
            rider.DataVersion++;
            rider.AbortedOperationThenQuery();
            TestRunner.True(rider.Slots[9] != null, "The aborted operation wrote the slot.");
            rider.NativeInvalidateAt(9);
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The successful GetSlot after an aborted operation was not recovered.");
            var rows = service.Save();
            TestRunner.Equal(9, PublicRowIndex(rows), "The save after the recovered GetSlot did not carry 9.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore 9 after the recovered GetSlot.");
            // A CleanupBadSlots pass reports and empties 9, then throws on a later unrelated slot (its postfix never runs).
            var thrown = new Service();
            var passRider = PlacedAt(9, "rider", thrown);
            passRider.Slots[12] = "Bow";
            passRider.DataVersion++;
            passRider.NativeCleanupPass(throwAfter: 9);
            TestRunner.True(passRider.Slots[9] == null && thrown.Scope.Open, "The fixture did not leave the pass open after writing 9.");
            thrown.Frame(); thrown.Frame();
            TestRunner.True(passRider.Find() == 9 && passRider.Count() == 1 && passRider.Slots[12] == "Bow",
                "The write the pass completed before throwing was not recovered.");
            var passRows = thrown.Save();
            TestRunner.Equal(9, PublicRowIndex(passRows), "The save after the thrown pass did not carry 9.");
            var passLoaded = passRider.Load(passRows);
            passLoaded.Frame(); passLoaded.Frame();
            TestRunner.True(passLoaded.Find() == 9 && passLoaded.Count() == 1, "The reload did not restore 9 after the thrown pass.");
            // The same thrown pass, then a save at once: the archive header is the first boundary (no frame, no GetSlot).
            var immediate = new Service();
            var savedRider = PlacedAt(9, "rider", immediate);
            savedRider.Slots[12] = "Bow";
            savedRider.DataVersion++;
            savedRider.NativeCleanupPass(throwAfter: 9);
            var immediateRows = immediate.Save();
            TestRunner.True(immediateRows.Count(r => r.Kind == MountedControlSlotPolicy.SavedMountState && r.Index == 9) == 1 &&
                immediateRows.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && PublicRowIndex(immediateRows) == 9,
                "The immediate save did not carry kind 6 at 9 and one placement record.");
            var immediateLoaded = savedRider.Load(immediateRows);
            immediateLoaded.Frame(); immediateLoaded.Frame();
            TestRunner.True(immediateLoaded.Find() == 9 && immediateLoaded.Count() == 1, "The reload of the immediate save did not give one shortcut at 9.");
            // Control: the aborted operation's report, then the player's own removal (null, not native's empty slot), stays
            // a removal, after updates and in an immediate save alike.
            foreach (var saveAtOnce in new[] { false, true })
            {
                var control = new Service();
                var removed = PlacedAt(9, "rider", control);
                removed.DataVersion++;
                removed.AbortedOperationThenQuery();
                removed.Slots[9] = null;
                if (!saveAtOnce) { control.Frame(); control.Frame(); }
                var label = " (saveAtOnce=" + saveAtOnce + ")";
                TestRunner.True(MarkerOnly(control.Save()), "The save after the removal carried more than the placement record" + label + ".");
                control.Frame(); control.Frame();
                TestRunner.Equal(-1, removed.Find(), "The player's removal after an aborted operation was undone" + label + ".");
            }
        }

        private static void DepartedRiderReleasesCopies()
        {
            // A's chosen stale binding at 3 with a live copy at 9; GetSlot empties 3, recording the copy; the rider leaves the
            // travelling roster for the reserve before any repair, in the same player world.
            var service = new Service();
            var rider = PlacedAt(3, "rider", service);
            rider.Slots[9] = rider.Data;
            rider.DataVersion++;
            rider.NativeInvalidateAt(3);
            TestRunner.Equal(1, service.RecordedCopyCount("rider"), "The fixture did not record the copy at 9.");
            var world = service.Current;
            rider.LeaveParty();
            service.Frame();
            TestRunner.True(ReferenceEquals(world, service.Current) && rider.KmcCount() == 0, "The fixture did not clear the departed rider's bar in the same world.");
            TestRunner.Equal(0, service.RecordedCopyCount("rider"), "The registry kept the departed rider's discarded slot objects.");
            // Reserve-return control: the chosen index comes back exactly once.
            rider.ToParty();
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 3 && rider.Count() == 1, "The returning rider did not get one shortcut at 3.");
            TestRunner.Equal(3, PublicRowIndex(service.Save()), "The save after the return did not carry 3.");
            // A copy replaced by another ability while the control is not live is released at the next repair.
            var replaced = new Service();
            var other = PlacedAt(3, "rider", replaced);
            other.Slots[9] = other.Data;
            other.DataVersion++;
            other.NativeInvalidateAt(3);
            other.PetResolves = false;
            other.Slots[9] = "Fireball";
            replaced.Frame();
            TestRunner.Equal(0, replaced.RecordedCopyCount("rider"), "The registry kept a copy another ability replaced.");
        }

        private static void OccupiedChosenSlotWithTwoCopies()
        {
            foreach (var rebind in new[] { false, true })
            {
                // The chosen stale binding at 3 and two live canonical copies at 7 and 9; GetSlot empties 3 and the player
                // puts Fireball there before any update. rebind: a live replacement fact arrives and one CleanupBadSlots pass
                // rebinds both 7 and 9 (the first becomes the live fallback before the second is read).
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = Control + "#0";
                rider.Slots[7] = rider.Data;
                rider.Slots[9] = rider.Data;
                rider.NativeInvalidateAt(3);
                rider.Slots[3] = "Fireball";
                var label = " (rebind=" + rebind + ")";
                if (rebind)
                {
                    rider.DataVersion++;
                    rider.NativeCleanupPass(rebindLive: true);
                    TestRunner.True(rider.Slots[7] == rider.Data && rider.Slots[9] == rider.Data, "The fixture did not rebind 7 and 9.");
                }
                service.Frame(); service.Frame();
                TestRunner.True(rider.Count() == 1 && rider.Find() == 7 && rider.Slots[3] == "Fireball", "Repair did not leave exactly one fallback at 7 beside Fireball" + label + ".");
                var rows = service.Save();
                TestRunner.Equal(7, PublicRowIndex(rows), "The save did not carry the one fallback" + label + ".");
                TestRunner.True(rider.Count() == 1 && rider.Find() == 7, "The save scope changed the fallback" + label + ".");
                rider.Slots[7] = null;                    // the player removes the fallback
                service.Frame(); service.Frame();
                TestRunner.True(rider.Count() == 0 && rider.Slots[3] == "Fireball", "Removing the fallback promoted another copy or moved Fireball" + label + ".");
                var after = service.Save();
                TestRunner.True(MarkerOnly(after), "The save after removing the fallback carried more than the placement record" + label + ".");
                var loaded = rider.Load(after);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Count() == 0 && loaded.Slots[3] == "Fireball", "The reload resurrected a removed shortcut" + label + ".");
            }
        }

        private static void FallbackBeforeOwnMigration()
        {
            foreach (var saveFirst in new[] { false, true })
            {
                // Mixed older bar: GetSlot empties 4, the player puts Fireball there, and KMC runs an update (or a save) while
                // the pet is still unresolved, before native reads the alias at 9; the same pet then returns.
                var service = new Service();
                var rider = MixedOlderBar(service);
                rider.NativeInvalidateAt(4);
                rider.Slots[4] = "Fireball";
                if (saveFirst) service.Save(); else service.Frame();
                rider.PetResolves = true;
                service.Frame(); service.Frame();
                var label = " (saveFirst=" + saveFirst + ")";
                TestRunner.True(rider.Find() == 9 && rider.Count() == 1 && rider.Slots[4] == "Fireball", "The fallback at 9 was lost or Fireball moved" + label + ".");
                var rows = service.Save();
                TestRunner.Equal(9, PublicRowIndex(rows), "The save did not carry 9" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1 && loaded.Slots[4] == "Fireball", "The reload did not keep 9 and Fireball" + label + ".");
            }
        }

        private static void MoveBeyondSurvivingCopy()
        {
            foreach (var rebind in new[] { false, true })
            foreach (var mode in new[] { "update", "save" })
            {
                // A stale binding at 3 and live copies at 7 and 9; GetSlot empties 3; the player moves 9 to 11. rebind: a
                // live replacement fact arrives and one CleanupBadSlots pass rebinds the stale controls at 7 and 11.
                var service = new Service();
                var rider = new Rider("rider", service, service.Current) { FactLive = true, Autofill = false };
                service.Placed.Add("rider");
                rider.Slots[3] = Control + "#0";
                rider.Slots[7] = rider.Data;
                rider.Slots[9] = rider.Data;
                rider.NativeInvalidateAt(3);
                rider.Slots[11] = rider.Slots[9]; rider.Slots[9] = null;
                if (rebind)
                {
                    rider.DataVersion++;
                    rider.NativeCleanupPass(rebindLive: true);
                    TestRunner.True(rider.Slots[7] == rider.Data && rider.Slots[11] == rider.Data, "The fixture did not rebind 7 and 11.");
                }
                var label = " (" + mode + "-first, rebind=" + rebind + ")";
                List<SavedRow> rows;
                if (mode == "save") { rows = service.Save(); service.Frame(); service.Frame(); }
                else { service.Frame(); service.Frame(); rows = service.Save(); }
                TestRunner.True(rider.Find() == 11 && rider.Count() == 1 && rider.Slots[3] == null, "The move to 11 did not win" + label + ".");
                TestRunner.Equal(11, PublicRowIndex(rows), "The save did not carry 11" + label + ".");
                var loaded = rider.Load(rows);
                loaded.Frame(); loaded.Frame();
                TestRunner.True(loaded.Find() == 11 && loaded.Count() == 1, "The cold restore did not give one shortcut at 11" + label + ".");
            }
        }

        private static void NativeEmptiesNewPlacement()
        {
            // Native empties 3 (no other public binding), the player places the current control at 9, and native empties 9
            // too before any update.
            var service = new Service();
            var rider = PlacedAt(3, "rider", service);
            rider.DataVersion++;
            rider.NativeInvalidateAt(3);
            rider.Slots[9] = rider.Data;
            rider.DataVersion++;
            rider.NativeInvalidateAt(9);
            TestRunner.True(rider.Count() == 0, "The fixture did not empty both slots.");
            service.Frame(); service.Frame();
            TestRunner.True(rider.Find() == 9 && rider.Count() == 1, "The new placement at 9 was discarded for the older choice.");
            var rows = service.Save();
            TestRunner.Equal(9, PublicRowIndex(rows), "The save did not carry 9.");
            var loaded = rider.Load(rows);
            loaded.Frame(); loaded.Frame();
            TestRunner.True(loaded.Find() == 9 && loaded.Count() == 1, "The reload did not restore 9.");
            // After companion retirement: the rider now links to pet B and native empties pet A's stale shortcut at 3, which
            // retires A's arrangement; the player then places the current control at 9 by hand and a later native operation
            // empties it before any update. (The stale batch of A's own reports stays rejected: RetiredRiderCleanupBatch.)
            var fresh = new Service();
            var changed = PlacedAt(3, "rider", fresh);
            changed.PetId = "horse-2";
            changed.DataVersion++;
            changed.NativeInvalidateAt(3);
            TestRunner.True(changed.Count() == 0 && fresh.Intent.IsRetired("rider"), "The fixture did not retire pet A's shortcut.");
            changed.Slots[9] = changed.Data;
            changed.DataVersion++;
            changed.NativeInvalidateAt(9);
            TestRunner.True(changed.Count() == 0, "The fixture did not empty the new placement.");
            fresh.Frame(); fresh.Frame();
            TestRunner.True(changed.Find() == 9 && changed.Count() == 1, "Pet B's first manual placement was discarded.");
            var freshRows = fresh.Save();
            TestRunner.Equal(9, PublicRowIndex(freshRows), "The save did not carry pet B's 9.");
            var freshLoaded = changed.Load(freshRows);
            freshLoaded.Frame(); freshLoaded.Frame();
            TestRunner.True(freshLoaded.Find() == 9 && freshLoaded.Count() == 1, "The reload did not restore pet B's 9.");
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
