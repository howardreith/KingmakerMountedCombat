using System;
using System.Linq;
using Kingmaker;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed checkpoint producer (Directive 4): a disposable copy of the accepted owner-seed-derived
    // Manual is cold-loaded with both pairs restored mounted. Instead of the native Mounts and new hotbar
    // bindings, the producer verifies the restored graph against the Manual producer's read-back and
    // runtime (VerifyRestoredOwnerSeedPairs), adopts the restored owners, generations and slots as they
    // are, and then dispatches its one selected genuine native checkpoint through the generic path.
    // Nothing is mounted, granted, written or relabelled by this admission.
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool OwnerSeedCheckpointSave => request.OwnedPairs?.IsOwnerSeedCheckpointSave == true;

        private bool OwnerSeedRestoredStartReady()
        {
            if (persistence.ArchiveRestorePhase != "all-pair-presentation-restored" || persistence.CombatRestorationPending ||
                controls.HasUnsettledRelationshipTransition || Game.Instance.Player.IsInCombat) return false;
            return actors.All(Chunk7NativeFixture.Idle);
        }

        private void AdmitOwnerSeedRestoredStart()
        {
            Require(OwnerSeedCheckpointSave && !Cold && inherited?.Bound == true && expectedOwners.Count == 0 && slots == null,
                "Restored-start admission belongs only to the owner-seed checkpoint producer, once.");
            inherited.RequirePreserved();
            saved = persistence.LoadedArchive; var input = request.PersistenceLoad;
            Require(saved != null && saved.SchemaVersion == MountedArchiveData.Schema && saved.SchemaVersion == NativeMountedSaveFormat.SelectedWriterSchema &&
                saved.Pairs.Length == 2 && saved.Participations.Length == 0 && saved.Combat == null && saved.CampaignId == input.GameId && saved.AreaId == input.Area &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                controls.NativeCastRequestCount == 0 && persistence.SnapshotCount == snapshotBefore && persistence.SnapshotCount == 0,
                "The owner-seed checkpoint producer requires the exact current-schema two-pair exploration graph restored once, with no native input or snapshot.");
            Require(Mounted(firstRider, firstMount) && Mounted(secondRider, secondMount) && world.Registry.ActiveCount == 2 && world.Registry.Owners.Length == 2 &&
                ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider) &&
                ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider),
                "Both owner-seed-derived pairs were not restored mounted with reciprocal native custody.");
            var profiles = new JArray();
            foreach (var pair in new[] { new[] { firstRider, firstMount }, new[] { secondRider, secondMount } })
            {
                var profile = SupportedMountedProfiles.Resolve(pair[0], pair[1]);
                var record = saved.Pairs.SingleOrDefault(p => p.Rider.Id == pair[0].UniqueId && p.Mount.Id == pair[1].UniqueId);
                Require(profile != null && profile.Id == SupportedMountedProfiles.Horse.Id && record != null && record.ProfileId == profile.Id,
                    "A restored owner-seed-derived pair lost its exact Horse profile.");
                profiles.Add(new JObject { ["riderId"] = pair[0].UniqueId, ["mountId"] = pair[1].UniqueId, ["profileId"] = profile.Id });
            }
            // The cold consumer's shared check: graph equals the Manual producer read-back, owners,
            // generations and slots equal the Manual producer runtime at its save boundary.
            var restored = inherited.VerifyRestoredOwnerSeedPairs(saved, world, controls.CapturePersistentSlots());
            foreach (var pair in saved.Pairs) expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation);
            AssertOwners(); slots = saved.Slots; AssertSlots(); AssertDebt(saved);
            var s = request.OwnedPairs;
            evidence["mountedStart"] = new JObject { ["contract"] = "owner-seed-checkpoint-restored-start-v1", ["restoredFromColdLoad"] = true,
                ["nativeMountInputs"] = 0, ["newSlotBindings"] = 0, ["archivePhase"] = persistence.ArchiveRestorePhase, ["schemaVersion"] = saved.SchemaVersion,
                ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
                ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount, ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["snapshots"] = persistence.SnapshotCount, ["profiles"] = profiles, ["owners"] = restored["owners"], ["slots"] = restored["slots"],
                ["loadedEqualsProducerReadback"] = restored["loadedEqualsProducerReadback"], ["producerProcessId"] = restored["producerProcessId"],
                ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["debtRetained"] = true,
                ["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            evidence["ownerSeedCheckpointSource"] = new JObject { ["contract"] = "owner-seed-checkpoint-source-v1", ["role"] = "producer",
                ["ancestry"] = Chunk7WorldScenarioEngine.OwnerSeedDerivedAncestry, ["acquisitionProof"] = false, ["ordinaryProducerAncestry"] = false,
                ["manualProducerRunId"] = s.SourceRunId, ["manualProducerArtifactSha256"] = s.SourceArtifactSha256, ["manualSha256"] = s.SourceArchiveSha256,
                ["acceptedConsumerRunId"] = s.CheckpointSourceRunId, ["acceptedConsumerArtifactSha256"] = s.CheckpointSourceArtifactSha256,
                ["seedId"] = s.SeedId, ["seedSha256"] = s.SeedSha256, ["checkpoint"] = request.PersistenceCase,
                ["candidates"] = inherited.OwnerSeedCandidates?.DeepClone() };
            snapshots.Add(Capture("before-native-save"));
            Pass("C7-persistence-owner-seed-restored-start");
            DispatchCheckpoint();
        }
    }
}
