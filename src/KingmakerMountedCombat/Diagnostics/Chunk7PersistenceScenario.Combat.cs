using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private Chunk7CombatCheckpointDriver checkpoint;
        private bool combatCheckpointReady, legacyWriterHeld, selectedArchiveCapability;
        private bool CombatCase => IsCombatCheckpoint(request.PersistenceCase);
        private bool TurnBasedCase => CombatCase && request.PersistenceCase != "multi-rt-debt-manual";
        private bool OwnerSeedCurrentPendingSource => OwnerSeedCheckpointSave && !Cold && LegacyPendingCase;
        private int ExpectedCombatSourceSchema => LegacyPendingCase && !OwnerSeedCurrentPendingSource ? 2 : 3;
        private bool LegacyPendingCase => request.PersistenceCase == "legacy-mounted-pending-tb-manual";
        private bool PendingOnlyCase => LegacyPendingCase || OwnedSchema2CurrentCase;
        private bool ZeroMountedCase => request.PersistenceCase == "zero-mounted-tb-retained-manual";
        private int RequiredOwners => PendingOnlyCase ? 1 : 2;
        private int RequiredMounted => ZeroMountedCase && (Cold || combatCheckpointReady) ? 0 : RequiredOwners;
        private Chunk7CombatCheckpointKind CheckpointKind => PendingOnlyCase ? Chunk7CombatCheckpointKind.MountedPendingOnly :
            ZeroMountedCase ? Chunk7CombatCheckpointKind.ZeroMountedRetained :
            TurnBasedCase ? Chunk7CombatCheckpointKind.TurnBasedDebts : Chunk7CombatCheckpointKind.RealTimeDebts;
        private static bool IsCombatCheckpoint(string value) => value == "multi-rt-debt-manual" || value == "multi-tb-debt-manual" ||
            value == "zero-mounted-tb-retained-manual" || value == "legacy-mounted-pending-tb-manual" || value == "owned-schema2-pending-current-manual";

        private void AfterNativeMounts()
        {
            RememberOwners(); AssertOwners(); BindSlots(); snapshots.Add(Capture("before-native-save"));
            Pass(LegacyPendingCase ? "C7-persistence-one-native-mount-legacy-source" : "C7-persistence-two-native-mounts");
            DispatchCheckpoint();
        }
        // The selected checkpoint after a mounted start: the native Mounts above, or the owner-seed
        // checkpoint producer's verified restored start (AdmitOwnerSeedRestoredStart).
        private void DispatchCheckpoint()
        {
            if (CombatCase)
            {
                checkpoint = new Chunk7CombatCheckpointDriver(request, world, controls, logger, CheckpointKind, inherited);
                checkpoint.StartSource(actors); Next(5);
            }
            else if (OwnedSchema1SourceCase) BeginOwnedSchema1Source();
            else if (PendingMountSave) BeginPendingMountSave();
            else if (WorkerCancelSave) BeginWorkerCancelSave();
            else if (AreaAutosaveCase) BeginAreaAutosave();
            else { RequestSave(); Next(4); }
        }
        private void PrepareCombatSave()
        {
            combatCheckpointReady = true;
            AssertCombatOwners();
            slots = controls.CapturePersistentSlots();
            Require(slots.Length == (ZeroMountedCase ? 0 : RequiredOwners * 2), "Native checkpoint changed its declared owned control slots.");
            evidence["worldBeforeNativeSave"] = checkpoint.Capture();
            if (LegacyPendingCase && !OwnerSeedCurrentPendingSource)
            {
                Require(world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 &&
                    ReferenceEquals(world.Primary.Relationship.Rider, firstRider) && ReferenceEquals(world.Primary.Relationship.Mount, firstMount) &&
                    !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !NativePersistenceIsolation.HasPendingWrites,
                    "Legacy migration source requires the singular Primary owner before its private native write.");
                selectedArchiveCapability = NativeMountedSaveFormat.ArchiveNativeSupportEnabled;
                Require(selectedArchiveCapability, "Selected all-pair capability was already changed.");
                NativeMountedSaveFormat.ArchiveNativeSupportEnabled = false; legacyWriterHeld = true;
                evidence["legacyWriterScope"] = new JObject { ["selectedOnlyForCase"] = request.PersistenceCase,
                    ["sourceSchema"] = NativeMountedSaveFormat.SelectedWriterSchema, ["owners"] = world.Registry.Owners.Length,
                    ["restorationEnabledOnColdProcess"] = true };
            }
        }
        private void RestoreSelectedNativeFormat()
        {
            if (!legacyWriterHeld) return;
            Require(!persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !NativePersistenceIsolation.HasPendingWrites,
                "A native save worker still owns the legacy writer selection.");
            NativeMountedSaveFormat.ArchiveNativeSupportEnabled = selectedArchiveCapability; legacyWriterHeld = false;
            if (evidence["legacyWriterScope"] is JObject scope) scope["selectedCapabilityRestored"] = NativeMountedSaveFormat.ArchiveNativeSupportEnabled;
        }
        private void AssertCombatOwners()
        {
            Require(world.Registry.Owners.Length == RequiredOwners && world.Registry.ActiveCount == RequiredMounted &&
                world.Registry.Owners.All(o => expectedOwners.TryGetValue(o.Id, out var generation) && generation == o.Generation),
                "Native checkpoint changed exact owner identities, generations or retained custody.");
            if (RequiredMounted != 0)
                Require(Mounted(firstRider, firstMount) && (PendingOnlyCase || Mounted(secondRider, secondMount)),
                    "Native mounted checkpoint lost an exact relationship.");
            else
                Require(world.Registry.Owners.All(o => o.State == MountedPairOwnershipState.Detached) &&
                    world.Pairs.All(p => p.Relationship.State == RelationshipState.Unmounted),
                    "Zero-mounted checkpoint silently retired a required detached obligation.");
        }
        private void BindCombatActors()
        {
            if (request.OwnedPairs != null)
            {
                BindOwnedFixture(true);
                Require(actors.All(actor => saved.Combat.Actors.Any(row => row.Native.Id == actor.UniqueId)), "Cold archive omitted inherited native actors.");
                return;
            }
            firstRider = Exact(Chunk7WorldScenarioEngine.MammothRiderId); firstMount = Exact(Chunk7WorldScenarioEngine.MammothId);
            secondRider = Exact(Chunk7WorldScenarioEngine.HorseRiderId); secondMount = secondRider.Descriptor.Pet;
            Require(secondMount?.Blueprint?.AssetGuid == HorseCompanionBlueprintService.UnitGuid &&
                ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider) &&
                ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider), "Cold combat lost its saved native companions.");
            actors = new[] { firstRider, firstMount, secondRider, secondMount };
            TrackNativeActors();
            Require(actors.All(a => saved.Combat.Actors.Any(x => x.Native.Id == a.UniqueId)), "Cold archive omitted a selected fixture actor.");
        }
        private void ObserveCombatCold()
        {
            saved = persistence.LoadedArchive;
            Require(saved?.SchemaVersion == 3 && saved.Combat != null && saved.Combat.TurnBased == TurnBasedCase &&
                persistence.ArchiveRestorePhase == "all-pair-presentation-restored" && !persistence.CombatRestorationPending,
                "Complete native combat graph was not restored: " + persistence.Feedback);
            if (NativeArchiveRestorationProbe.IsDebtDiagnosticRequest(request))
                RecordCombatDebtObservation(saved, "cold-observe-before-binding", Game.Instance.TimeController.GameTime.Ticks);
            BindCombatActors();
            foreach (var pair in saved.Pairs) expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation);
            foreach (var participant in saved.Participations)
                if (!expectedOwners.ContainsKey(Guid.Parse(participant.OwnerId))) expectedOwners.Add(Guid.Parse(participant.OwnerId), participant.Generation);
            AssertCombatOwners(); slots = saved.Slots; AssertSlots();
            checkpoint = new Chunk7CombatCheckpointDriver(request, world, controls, logger, CheckpointKind, inherited);
            checkpoint.BindCold(actors, saved); checkpoint.ValidateArchive(saved);
            Require(persistence.SemanticRestoreCount == saved.Combat.Actors.Length && persistence.ArchivePostLoadObservedCount == saved.Combat.Actors.Length &&
                persistence.PresentationRestoreCount == saved.Pairs.Length && controls.NativeCastRequestCount == 0 && persistence.SnapshotCount == 0,
                "Cold combat replayed commands, omitted an actor or restored owner state more than once.");
            var ids = saved.Combat.Actors.Select(x => x.Native.Id).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 && restoration.Count("restore-actor-before", ids) == ids.Length &&
                restoration.Count("restore-actor-after", ids) == ids.Length && restoration.Count("restore-debt-before", ids) == ids.Length &&
                restoration.Count("restore-debt-after", ids) == ids.Length, "Cold native combat invented preparation or lost complete per-actor restoration observation.");
            evidence["coldRestoration"] = restoration.Capture();
            if (!Schema2ResaveCase) { restoration.Dispose(); restoration = null; }
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var descriptor = request.PersistenceLoad;
            var info = Game.Instance.SaveManager.SingleOrDefault(s => s.FileName == descriptor.FileName);
            Require(info != null && Hash(info.FolderName) == descriptor.Sha256, "Cold native source bytes differ from the exact selected archive.");
            var read = NativeMountedSaveStorage.ReadArchive(info.Saver);
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == ExpectedCombatSourceSchema &&
                JToken.DeepEquals(JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()), JObject.FromObject(saved, MountedSaveCodec.CreateSerializer())),
                "Cold restored graph differs from the native versioned source.");
            evidence["loadedSourceSchema"] = read.SourceSchema;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
            AssertCombatDebt(saved);
            evidence["worldAfterNativeRestore"] = Chunk7ArchiveNativeSnapshot.Capture(world, saved);
            snapshots.Add(Capture("cold-restored-before-input")); Pass("C7-persistence-cold-all-pair-identity-and-no-prepare");
        }
        private void ObserveCombatSaved(MountedArchiveReadResult read)
        {
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == ExpectedCombatSourceSchema && saved?.Combat != null &&
                saved.CampaignId == request.SelectedChunk7Fixture.GameId && saved.AreaId == request.SelectedChunk7Fixture.Area,
                "Native combat archive version or campaign differs from the selected checkpoint.");
            if (OwnerSeedCurrentPendingSource)
                evidence["nativeSourceContract"] = "owner-seed-current-pending-source-v1";
            RestoreSelectedNativeFormat();
            checkpoint.ValidateArchive(saved); AssertCombatOwners(); AssertSlots(); AssertCombatDebt(saved);
            if (!LegacyPendingCase || OwnerSeedCurrentPendingSource)
                Require(saved.Pairs.All(p => expectedOwners.TryGetValue(Guid.Parse(p.PairId), out var generation) && generation == p.Generation) &&
                    saved.Participations.All(p => expectedOwners.TryGetValue(Guid.Parse(p.OwnerId), out var generation) && generation == p.Generation),
                    "Native schema-3 archive changed stable owner identity or generation.");
            Require(saved.Slots.Length == slots.Length && slots.All(s => saved.Slots.Any(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind)),
                "Native combat readback omitted owned controls.");
            Require(callback && persistence.SnapshotCount == snapshotBefore + 1 && controls.NativeCastRequestCount == castBefore &&
                !controls.SerializationSuspended && !persistence.SaveSuspended, "Native combat save replayed input or retained serialization custody.");
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["sourceSchema"] = read.SourceSchema, ["nativeType"] = savedDescriptor.Type.ToString(), ["nativeName"] = savedDescriptor.Name,
                ["nativeCallback"] = callback, ["operation"] = savedDescriptor.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeMetadata"] = JObject.Parse(read.OriginalJson), ["archive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            evidence["worldAfterNativeSave"] = Chunk7ArchiveNativeSnapshot.Capture(world, saved);
            snapshots.Add(Capture("native-write-complete")); Pass("C7-persistence-native-category-all-pair-archive");
        }
        private void AssertCombatDebt(MountedArchiveData archive)
        {
            var observedTicks = Game.Instance.TimeController.GameTime.Ticks;
            var observed = NativeArchiveRestorationProbe.IsDebtDiagnosticRequest(request) ?
                RecordCombatDebtObservation(archive, "assert-combat-debt", observedTicks) : null;
            var elapsed = (observedTicks - archive.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
            Require(elapsed >= 0 && elapsed <= 30, "Native combat debt comparison escaped its observation window.");
            for (var index = 0; index < archive.Combat.Actors.Length; index++)
            {
                var before = archive.Combat.Actors[index].Native;
                var after = observed == null ? MountedPersistenceService.CaptureActor(Exact(before.Id)) : observed[index];
                var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
                var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
                Require(before.ReactionsRemaining == after.ReactionsRemaining && before.LastSurpriseTicks == after.LastSurpriseTicks &&
                    Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001 && b[i] + elapsed * 1.1 + 0.001 >= a[i]),
                    "Native save/load changed an actor's spent remainder beyond elapsed native time.");
            }
        }
        private SavedNativeActor[] RecordCombatDebtObservation(MountedArchiveData archive, string phase, long observedTicks)
        {
            var nativeActors = archive.Combat.Actors.Select(row => Exact(row.Native.Id)).ToArray();
            var observed = nativeActors.Select(MountedPersistenceService.CaptureActor).ToArray();
            var record = CaptureCombatDebtComparison(archive, observed, observedTicks, UnityEngine.Time.frameCount, phase);
            record["paused"] = Game.Instance.IsPaused;
            record["archiveRestorePhase"] = persistence.ArchiveRestorePhase;
            record["restorationPending"] = persistence.CombatRestorationPending;
            record["actorStates"] = new JArray(nativeActors.Select(actor => new JObject {
                ["actorId"] = actor.UniqueId, ["inCombat"] = actor.IsInCombat, ["prepared"] = actor.CombatState.Prepared }));
            AppendCombatDebtObservation(evidence, record);
            return observed;
        }
        internal static void AppendCombatDebtObservation(JObject observations, JObject value)
        {
            Require(observations != null && value != null && (observations["combatDebtChecks"] == null ||
                observations["combatDebtChecks"] is JArray), "Combat debt observation journal is invalid.");
            var checks = observations["combatDebtChecks"] as JArray;
            if (checks == null) { checks = new JArray(); observations["combatDebtChecks"] = checks; }
            Require(checks.Count < 32, "Combat debt observation journal exceeded its bounded count.");
            checks.Add(value);
        }
        internal static JObject CaptureCombatDebtComparison(MountedArchiveData archive, SavedNativeActor[] observed,
            long observedTicks, int frame, string phase)
        {
            Require(archive?.Combat?.Actors != null && archive.Combat.Actors.Length > 0 && archive.Combat.Actors.Length <= 64 &&
                archive.Combat.Actors.All(row => row?.Native != null) && observed != null && observed.Length == archive.Combat.Actors.Length &&
                observed.All(row => row != null) && observed.Select(row => row.Id).Distinct(StringComparer.Ordinal).Count() == observed.Length &&
                archive.Combat.Actors.Select(row => row.Native.Id).SequenceEqual(observed.Select(row => row.Id)),
                "Combat debt observation omitted or substituted its exact ordered native actor rows.");
            var elapsed = (observedTicks - archive.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
            var fields = new[] { "Standard", "Move", "Swift", "Initiative", "Reaction" };
            var rows = new JArray();
            for (var index = 0; index < observed.Length; index++)
            {
                var before = archive.Combat.Actors[index].Native; var after = observed[index];
                var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
                var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
                var comparisons = new JArray(Enumerable.Range(0, a.Length).Select(i => new JObject {
                    ["field"] = fields[i], ["saved"] = a[i], ["actual"] = b[i],
                    ["passed"] = b[i] <= a[i] + 0.001 && b[i] + elapsed * 1.1 + 0.001 >= a[i] }));
                comparisons.Add(new JObject { ["field"] = "ReactionsRemaining", ["saved"] = before.ReactionsRemaining,
                    ["actual"] = after.ReactionsRemaining, ["passed"] = before.ReactionsRemaining == after.ReactionsRemaining });
                comparisons.Add(new JObject { ["field"] = "LastSurpriseTicks", ["saved"] = before.LastSurpriseTicks,
                    ["actual"] = after.LastSurpriseTicks, ["passed"] = before.LastSurpriseTicks == after.LastSurpriseTicks });
                rows.Add(new JObject { ["actorId"] = before.Id, ["saved"] = JObject.FromObject(before, MountedSaveCodec.CreateSerializer()),
                    ["actual"] = JObject.FromObject(after, MountedSaveCodec.CreateSerializer()), ["fields"] = comparisons,
                    ["passed"] = comparisons.All(item => (bool)item["passed"]) });
            }
            return new JObject { ["contract"] = "chunk7-native-combat-debt-observation-v1", ["phase"] = phase,
                ["frame"] = frame, ["gameTicks"] = observedTicks, ["savedGameTicks"] = archive.GameTimeTicks,
                ["elapsedSeconds"] = elapsed, ["windowAccepted"] = elapsed >= 0 && elapsed <= 30,
                ["actors"] = rows, ["passed"] = elapsed >= 0 && elapsed <= 30 && rows.All(row => (bool)row["passed"]) };
        }
        private void BeginCombatContinuation()
        {
            checkpoint.BeginContinuation(saved);
        }
    }
}
