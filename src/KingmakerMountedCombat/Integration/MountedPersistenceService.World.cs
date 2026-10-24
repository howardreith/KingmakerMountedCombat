using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        private MountedWorldService mountedWorld;
        private MountedArchiveReadResult loadedArchive;
        private SaveScope admittedSave;
        private SaveScope releasingSave;
        private readonly Dictionary<string, UnitEntityData> archiveActors =
            new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
        private readonly Dictionary<string, MountedPairServices> archiveOwners =
            new Dictionary<string, MountedPairServices>(StringComparer.Ordinal);
        private readonly Dictionary<string, UnitEntityData> archivePostLoadObserved =
            new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
        internal string ArchiveRestorePhase { get; private set; } = "not-selected";
        internal int ArchivePostLoadObservedCount => archivePostLoadObserved.Count;
        private bool archiveGraphReady;
        private bool archiveOwnersReady;
        private RetainedPendingReentry retainedPendingReentry;
        private sealed class RetainedPendingReentry
        {
            internal Player World;
            internal TurnBased.Controllers.CombatController Controller;
            internal SavedCombatData Combat;
            internal long LoadSequence;
            internal RetainedPendingRider[] Riders;
        }
        private sealed class RetainedPendingRider
        {
            internal UnitEntityData Rider, Mount;
            internal MountedPairServices Pair;
            internal Guid OwnerId;
            internal long Generation;
        }
        internal bool HasSaveAdmissionFence => admittedSave != null;
        internal MountedArchiveData LoadedArchive => loadedArchive?.Data;
        // Read-only observation of the selected archive read (Missing for a save made without KMC).
        internal MountedSaveReadKind? LoadedArchiveKind => loadedArchive?.Kind;
        internal bool LoadedArchiveCanRestore => NativeMountedSaveFormat.CanRestore(loadedArchive);
        private bool ArchiveNativeEnabled => mountedWorld != null && NativeMountedSaveFormat.ArchiveNativeSupportEnabled;
        private MountedArchiveData RestorableArchive => ArchiveNativeEnabled && NativeMountedSaveFormat.CanRestore(loadedArchive)
            ? loadedArchive.Data : null;
        private SavedCombatData SelectedCombat => RestorableArchive?.Combat ?? loaded?.Data?.Combat;
        private GameMountedRelationshipService[] WorldRelationships => mountedWorld == null
            ? new[] { relationship }.Where(r => r != null).ToArray()
            : mountedWorld.Pairs.Select(p => p.Relationship).ToArray();
        private MountedCombatController[] WorldCombats => mountedWorld == null
            ? new[] { combat }.Where(c => c != null).ToArray()
            : mountedWorld.Pairs.Select(p => p.Combat).ToArray();

        internal MountedPersistenceService(MountedWorldService world, DiagnosticSettings settings, IModLogger logger)
            : this(world.Primary.Relationship, world.NativeControls, world.UnifiedTurn, settings, logger, world.Primary.Combat)
        { mountedWorld = world ?? throw new ArgumentNullException(nameof(world)); }

        private bool AnyChargeFence => WorldCombats.Any(c => c.ChargeAdmissionFenced);
        private bool DrainWorldCharge(string reason)
        {
            var settled = true; var errors = new List<Exception>();
            foreach (var controller in WorldCombats)
                try { if (!controller.TryDrainChargeOwnership(reason)) settled = false; }
                catch (Exception error) { errors.Add(error); settled = false; }
            if (errors.Count != 0) throw new AggregateException("Some mounted owners could not drain Charge custody.", errors);
            return settled;
        }

        private bool AcquireWorldSaveFence(SaveScope scope)
        {
            if (admittedSave != null && !ReferenceEquals(admittedSave, scope))
                throw new InvalidOperationException("A different native save owns world admission.");
            admittedSave = scope;
            var settled = true; var errors = new List<Exception>();
            // Pending Mount groups may acquire their registration while the save
            // waits. Reconcile every probe; freeze only at the header barrier.
            foreach (var controller in WorldCombats)
            {
                if (!scope.ChargeOwners.Contains(controller)) scope.ChargeOwners.Add(controller);
                try { if (!controller.AcquireChargeSaveFence(scope)) settled = false; }
                catch (Exception error) { errors.Add(error); settled = false; }
            }
            if (errors.Count != 0) throw new AggregateException("Mounted save admission could not fence every owner.", errors);
            return settled;
        }

        private bool ContinueWorldSaveFence(SaveScope scope)
        {
            var ready = true; var errors = new List<Exception>();
            foreach (var owner in scope.ChargeOwners)
                try { if (!owner.ContinueChargeSaveFence(scope)) ready = false; }
                catch (Exception error) { errors.Add(error); ready = false; }
            if (errors.Count != 0) throw new AggregateException("Mounted save continuation retained unfinished owner fences.", errors);
            return ready;
        }
        private void ReleaseWorldSaveFence(SaveScope scope)
        {
            var errors = new List<Exception>();
            foreach (var controller in scope.ChargeOwners.ToArray())
                try { controller.ReleaseChargeSaveFence(scope); scope.ChargeOwners.Remove(controller); }
                catch (Exception error) { errors.Add(error); }
            if (errors.Count != 0) throw new AggregateException("Mounted save release retained failed owner fences.", errors);
            if (ReferenceEquals(admittedSave, scope)) admittedSave = null;
        }

        private void ReleaseUnstartedSaveFence(SaveScope scope)
        {
            try { ReleaseWorldSaveFence(scope); }
            catch { releasingSave = scope; throw; }
        }

        private void RetrySaveRelease()
        {
            if (releasingSave == null) return;
            var scope = releasingSave;
            var drained = ReferenceEquals(drainingSave, scope);
            try
            {
                ReleaseSaveScope(scope);
                if (drained) ReleaseWorldAfterDrain();
            }
            catch { /* Original failure remains reported; custody stays fenced for retry. */ }
        }

        private void PinWorldSaveScope(SaveScope scope)
        {
            if (!SaveEffectsReady() || !AcquireWorldSaveFence(scope))
                throw new InvalidOperationException("The complete mounted world is not settled at the native snapshot barrier.");
            scope.Relationships = WorldRelationships;
            foreach (var owner in scope.Relationships) owner.SaveSerializationSuspended = true;
        }

        private void SuspendWorldSaveAi(SaveScope scope)
        {
            foreach (var owner in scope.Relationships)
            {
                var restore = owner.Runtime.SuspendSerializedAiLease();
                if (restore != null) scope.RestoreAiOwners.Add(restore);
            }
        }

        private MountedArchiveData CaptureArchive()
        {
            var game = Game.Instance;
            if (!ArchiveNativeEnabled || game?.Player == null || game.CurrentlyLoadedArea == null)
                throw new InvalidOperationException("No selected all-pair native world exists at the save barrier.");
            var groups = mountedWorld.Pairs;
            var mounted = groups.Where(g => g.Relationship.State == RelationshipState.Mounted).ToArray();
            if (mounted.Length != 0 && (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler))
                throw new InvalidOperationException("Mounted saving requires the supported paired policy.");
            if (!game.Player.IsInCombat)
            {
                var retained = false;
                foreach (var participant in unifiedTurn.SnapshotParticipants)
                    if (!participant.TrySettleExplorationParticipation()) retained = true;
                if (retained) throw new InvalidOperationException("An ended native combat still retains mounted participation; saving cannot omit it.");
            }
            if (!Enabled && groups.Any(g => g.Relationship.PairOwner != null))
                throw new InvalidOperationException("Disabled mounted operation still owns native custody; cleanup must settle before saving.");
            var savedCombat = Enabled && game.Player.IsInCombat ? CaptureCombat(true) : null;
            var result = new MountedArchiveData {
                SchemaVersion = MountedArchiveData.Schema, CampaignId = game.Player.GameId,
                AreaId = game.CurrentlyLoadedArea.AssetGuidThreadSafe, GameTimeTicks = game.TimeController.GameTime.Ticks,
                Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
                Slots = controls.CapturePersistentSlots(), Combat = savedCombat,
                Pairs = Enabled ? mounted.Select(g => {
                    var owner = g.Relationship.PairOwner;
                    if (!g.Relationship.IsCurrentPairOwner(owner))
                        throw new InvalidOperationException("Mounted capture has no exact active custody.");
                    return new SavedMountedPair { PairId = owner.Id.ToString("N"), Generation = owner.Generation,
                        ProfileId = g.Relationship.Runtime.MountProfileId,
                        Rider = CaptureActor(owner.Rider), Mount = CaptureActor(owner.Mount) };
                }).ToArray() : new SavedMountedPair[0]
            };
            var participations = new List<SavedPairParticipation>();
            if (savedCombat != null) foreach (var group in groups)
            {
                var state = group.Participant.CaptureParticipation();
                if (state == null) continue;
                var owner = group.Relationship.PairOwner;
                if (owner == null || !mountedWorld.Registry.Owns(owner))
                    throw new InvalidOperationException("Retained native participation has no exact registered custody.");
                participations.Add(new SavedPairParticipation {
                    OwnerId = owner.Id.ToString("N"), Generation = owner.Generation,
                    MountedPairId = owner.State == MountedPairOwnershipState.Active ? owner.Id.ToString("N") : null, State = state });
            }
            result.Participations = participations.ToArray();
            result.Validate();
            return result;
        }

        private void ClearArchiveRestoration()
        {
            ClearArchivePresentationReadiness();
            loadedArchive = null; archiveGraphReady = false; archiveOwnersReady = false;
            retainedPendingReentry = null;
            archiveActors.Clear(); archiveOwners.Clear(); archivePostLoadObserved.Clear(); ArchiveRestorePhase = "not-selected";
        }

        internal void ReleaseRetainedPendingReentry() { retainedPendingReentry = null; }

        internal bool AllowNativeJoinCombat(UnitEntityData unit)
        {
            var guard = retainedPendingReentry;
            if (guard == null) return true;
            var selected = guard.Riders.SingleOrDefault(row => ReferenceEquals(row.Rider, unit));
            if (selected == null) return true;
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            if (!Enabled || game?.Player == null || !ReferenceEquals(game.Player, guard.World) ||
                !ReferenceEquals(controller, guard.Controller) || restoreLoad?.Sequence != guard.LoadSequence ||
                ArchiveRestorePhase != "all-pair-presentation-restored" || !game.Player.IsInCombat ||
                !TurnBased.Controllers.CombatController.IsInTurnBasedCombat() || !controller.Initialized ||
                !RetainedNativeReentryPolicy.SameRetainedRound(guard.Combat, controller.RoundNumber,
                    NativeCombatTurnPersistence.CurrentStartTicks(controller), controller.RoundStartTime.Ticks))
            {
                retainedPendingReentry = null;
                return true;
            }
            var relationship = selected.Pair.Relationship;
            var owner = relationship.PairOwner;
            if (owner == null || owner.Id != selected.OwnerId || owner.Generation != selected.Generation ||
                !relationship.IsCurrentPairOwner(owner) || !ReferenceEquals(relationship.Rider, unit) ||
                !ReferenceEquals(relationship.Mount, selected.Mount) ||
                !selected.Pair.Participant.HasExactPendingSplit(selected.Mount, guard.Combat.Round))
            {
                guard.Riders = guard.Riders.Where(row => !ReferenceEquals(row, selected)).ToArray();
                if (guard.Riders.Length == 0) retainedPendingReentry = null;
                return true;
            }
            if (unit.IsInCombat || unit.CombatState.Prepared)
            {
                guard.Riders = guard.Riders.Where(row => !ReferenceEquals(row, selected)).ToArray();
                if (guard.Riders.Length == 0) retainedPendingReentry = null;
                return true;
            }
            // The archive owns this exact rider's out-of-combat, unprepared
            // state until the native encounter advances. Other actors join and
            // prepare normally; no cooldown or Prepared flag is written here.
            return false;
        }

        private bool ResolveArchiveGraph()
        {
            if (archiveGraphReady) return true;
            var data = RestorableArchive;
            var game = Game.Instance;
            if (data == null || game?.State == null || game.Player == null) return false;
            data.Validate();
            var snapshots = (data.Combat?.Actors.Select(a => a.Native) ?? Enumerable.Empty<SavedNativeActor>())
                .Concat(data.Pairs.SelectMany(p => new[] { p.Rider, p.Mount })).ToArray();
            var required = snapshots.Select(a => a.Id).Concat(data.Slots.Select(s => s.ActorId)).Distinct(StringComparer.Ordinal).ToArray();
            var units = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null).Distinct().ToArray();
            var resolved = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (var id in required)
            {
                var matches = units.Where(u => u.UniqueId == id).ToArray();
                if (matches.Length > 1) throw new InvalidDataException("Saved native actor does not resolve uniquely: " + id);
                if (matches.Length == 0) { ArchiveRestorePhase = "waiting-for-actor:" + id; return false; }
                resolved.Add(id, matches[0]);
            }
            // Installed semantic references are validated only after every saved
            // actor has completed its own native PostLoad; a not-yet-built brain
            // is a loading phase, not evidence of an incompatible save.
            foreach (var item in resolved) archiveActors.Add(item.Key, item.Value);
            archiveGraphReady = true; ArchiveRestorePhase = "actor-graph-resolved";
            return true;
        }

        private static string OwnerKey(string id) => Guid.Parse(id).ToString("N");

        private bool PrepareArchiveOwners()
        {
            if (archiveOwnersReady) return true;
            var data = RestorableArchive;
            if (data == null || !ResolveArchiveGraph()) return false;
            ValidateArchiveActorIdentity();
            foreach (var pair in data.Pairs)
            {
                var group = mountedWorld.CreateRestoredBoundary();
                group.Relationship.ImportSavedOwnership(archiveActors[pair.Rider.Id], archiveActors[pair.Mount.Id],
                    pair.PairId, pair.Generation, true);
                archiveOwners.Add(OwnerKey(pair.PairId), group);
            }
            foreach (var record in data.Participations.Where(p => p.MountedPairId == null))
            {
                var group = mountedWorld.CreateRestoredBoundary();
                var riderId = record.State.RiderId;
                var mountId = record.State.MountId ?? record.State.PendingSplitId;
                group.Relationship.ImportSavedOwnership(riderId == null ? null : archiveActors[riderId],
                    mountId == null ? null : archiveActors[mountId], record.OwnerId, record.Generation, false);
                archiveOwners.Add(OwnerKey(record.OwnerId), group);
            }
            archiveOwnersReady = true;
            return true;
        }

        private void RestoreArchiveActorAfterPostLoad(UnitEntityData unit)
        {
            var data = RestorableArchive;
            if (!Enabled || data == null || unit == null || combatRestoreFailure != null || restoreLoad == null ||
                restoreLoad.Sequence != loadSequence || !restoreLoad.World.TryBind(Game.Instance?.Player)) return;
            var snapshots = (data.Combat?.Actors.Select(a => a.Native) ?? Enumerable.Empty<SavedNativeActor>())
                .Concat(data.Pairs.SelectMany(p => new[] { p.Rider, p.Mount })).GroupBy(a => a.Id, StringComparer.Ordinal)
                .Select(g => g.First()).ToArray();
            if (!snapshots.Any(a => a.Id == unit.UniqueId)) return;
            try
            {
                if (archivePostLoadObserved.TryGetValue(unit.UniqueId, out var previous) && !ReferenceEquals(previous, unit))
                    throw new InvalidDataException("Conflicting native PostLoad callbacks identify the same saved actor.");
                archivePostLoadObserved[unit.UniqueId] = unit;
                if (!ResolveArchiveGraph()) return;
                // Earlier callbacks may arrive before every saved actor resolves.
                // Their exact objects must still own the completed graph; an ID
                // alone cannot credit a replacement with native PostLoad.
                foreach (var observed in archivePostLoadObserved)
                    if (!archiveActors.TryGetValue(observed.Key, out var exact) || !ReferenceEquals(exact, observed.Value))
                        throw new InvalidDataException("A native PostLoad callback identifies a different saved actor instance.");
                if (snapshots.Any(a => !archivePostLoadObserved.ContainsKey(a.Id)))
                { ArchiveRestorePhase = "waiting-for-all-native-postload"; return; }
                if (snapshots.All(a => restoredActors.ContainsKey(a.Id))) return;
                // CombatPreparePrefix and the existing world admission fence keep
                // native preparation disabled until this whole graph is rebound.
                // Now every actor has an initialized brain; validate all semantic
                // references before mutating the first actor's native state.
                ValidateArchiveActorIdentity();
                if (data.Combat != null) foreach (var actor in data.Combat.Actors)
                    NativeCombatActorPersistence.ValidateActorReferences(archiveActors[actor.Native.Id], actor);
                ArchiveRestorePhase = "validated-before-native-mutation";
                foreach (var saved in snapshots)
                {
                    var target = archiveActors[saved.Id];
                    var combatActor = data.Combat?.Actors.SingleOrDefault(a => a.Native.Id == saved.Id);
                    if (combatActor != null) NativeCombatActorPersistence.RestoreActor(target, combatActor, data.GameTimeTicks);
                    else NativeCombatActorPersistence.RestoreDebt(target, saved);
                    restoredActors.Add(saved.Id, target); SemanticRestoreCount++;
                }
                ArchiveRestorePhase = "all-native-actor-state-restored";
            }
            catch (InvalidDataException error) { BlockCombatRestoration("The saved actor graph is incompatible: " + error.Message); }
        }

        private void ValidateArchiveActorIdentity()
        {
            var game = Game.Instance;
            var units = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null).Distinct().ToArray();
            foreach (var actor in archiveActors)
            {
                var matches = units.Where(u => u.UniqueId == actor.Key).ToArray();
                if (matches.Length != 1 || !ReferenceEquals(matches[0], actor.Value))
                    throw new InvalidDataException("The resolved native actor graph changed before restoration: " + actor.Key);
            }
        }

        private void TryRestoreArchiveCombat()
        {
            var data = RestorableArchive;
            if (combatRestored || !CombatSemanticRestorationPending || combatRestoreFailure != null || data?.Combat == null || restoreLoad == null) return;
            var game = Game.Instance;
            if (game?.Player == null || game.Player.GameId != selectedCampaign || !restoreLoad.World.TryBind(game.Player) ||
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe != data.AreaId) return;
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                data.Combat.TurnBased != Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue)
            { BlockCombatRestoration("Saved combat policy differs from the active paired/mode settings."); return; }
            var controller = game.TurnBasedCombatController;
            var nativeComplete = restoreLoad.World.NativeCompleted && !LoadingProcess.Instance.IsLoadingInProcess;
            if (data.Combat.Actors.Any(a => !restoredActors.ContainsKey(a.Native.Id)))
            {
                if (nativeComplete) BlockCombatRestoration("A saved combat actor did not complete native PostLoad restoration.");
                return;
            }
            if (data.Combat.TurnBased ? !controller.Initialized : !game.Player.IsInCombat)
            {
                if (nativeComplete) BlockCombatRestoration("The saved native combat world did not become available.");
                return;
            }
            if (data.Combat.Actors.Any(a => archiveActors[a.Native.Id].View?.AgentASP == null)) return;
            try
            {
                if (!PrepareArchiveOwners()) return;
                // One roster/current-turn/clock and one allocation ledger. Pair
                // restore below cannot replay these world mutations or prepare.
                NativeCombatActorPersistence.RestoreReferences(data.Combat, archiveActors);
                NativeCombatTurnPersistence.Restore(controller, data.Combat, archiveActors);
                unifiedTurn.RestoreSharedPersistence(data.Combat, archiveActors);
                foreach (var record in data.Participations)
                    archiveOwners[OwnerKey(record.OwnerId)].Participant.RestoreParticipation(record.State, archiveActors, data.Combat.TurnBased);
                foreach (var owner in archiveOwners.Values) owner.Relationship.CompleteSavedParticipationImport();
                combatRestored = true; ArchiveRestorePhase = "shared-and-pair-participation-restored";
                Report("Saved native world rebound once with " + data.Participations.Length +
                    " participation owners; round=" + controller.RoundNumber + "; current=" +
                    (controller.CurrentTurn?.Unit.UniqueId ?? "none") + "; no Prepare or End.");
            }
            catch (Exception error)
            {
                BlockCombatRestoration("All-pair combat rebind failed: " + error.Message);
                throw;
            }
        }

        private bool RestoreArchivePresentation()
        {
            var data = RestorableArchive;
            if (data == null || !PreflightArchivePresentation() || !PrepareArchiveOwners()) return false;
            foreach (var pair in data.Pairs)
            {
                if (!restoredActors.TryGetValue(pair.Rider.Id, out var rider) || !restoredActors.TryGetValue(pair.Mount.Id, out var mount) ||
                    !ReferenceEquals(rider, archiveActors[pair.Rider.Id]) || !ReferenceEquals(mount, archiveActors[pair.Mount.Id]))
                    throw new InvalidDataException("Saved attached actors did not complete exact native PostLoad restoration.");
                var result = archiveOwners[OwnerKey(pair.PairId)].Relationship.RestoreSavedPair(rider, mount, pair.ProfileId);
                if (!result.Succeeded) throw new InvalidDataException("Saved relationship was not eligible: " + string.Join("; ", result.Errors));
                PresentationRestoreCount++;
            }
            foreach (var owner in archiveOwners.Values) owner.Relationship.CompleteSavedParticipationImport();
            controls.RestorePersistentSlots(data.Slots);
            ClearArchivePresentationReadiness();
            var retainedRiders = RetainedNativeReentryPolicy.SpentPendingRiders(data);
            retainedPendingReentry = retainedRiders.Length == 0 ? null : new RetainedPendingReentry {
                World = Game.Instance.Player, Controller = Game.Instance.TurnBasedCombatController,
                Combat = data.Combat, LoadSequence = loadSequence,
                Riders = retainedRiders.Select(id => {
                    var pair = data.Pairs.Single(row => row.Rider.Id == id);
                    return new RetainedPendingRider { Rider = archiveActors[id], Mount = archiveActors[pair.Mount.Id],
                        Pair = archiveOwners[OwnerKey(pair.PairId)], OwnerId = Guid.Parse(pair.PairId),
                        Generation = pair.Generation };
                }).ToArray()
            };
            ArchiveRestorePhase = "all-pair-presentation-restored";
            Report("Saved mounted world restored all " + data.Pairs.Length + " attachments and " + data.Participations.Length +
                " participation owners without Mount execution or new native preparation.");
            return true;
        }
    }
}
