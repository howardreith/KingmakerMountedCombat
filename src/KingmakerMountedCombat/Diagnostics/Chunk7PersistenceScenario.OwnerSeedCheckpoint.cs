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
        private bool OwnerSeedSingleOwnerPreparation => OwnerSeedCheckpointSave && (PendingMountSave || LegacyPendingCase || OwnedSchema1SourceCase);
        private MountedPairServices ownerSeedPreparationPair;
        private Guid ownerSeedReleasedOwner;
        private JObject ownerSeedPreparation;

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
            Require(world.Registry.Owners.All(o => expectedOwners.TryGetValue(o.Id, out var generation) && generation == o.Generation),
                "The restored start changed its recorded owner identities or generations.");
            slots = saved.Slots; AssertSlots(); AssertDebt(saved);
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
            Pass("C7-persistence-owner-seed-restored-start");
            if (OwnerSeedSingleOwnerPreparation) { BeginOwnerSeedCheckpointPreparation(); return; }
            if (ManticoreFixtureCase) { BeginManticoreFixture(); return; }
            snapshots.Add(Capture("before-native-save"));
            DispatchCheckpoint();
        }

        private void BeginOwnerSeedCheckpointPreparation()
        {
            Require(OwnerSeedSingleOwnerPreparation && ownerSeedPreparation == null && inputs.Count == 0 &&
                expectedOwners.Count == 2 && slots.Length == 4 && actors.All(Chunk7NativeFixture.Idle),
                "Single-owner preparation requires the admitted two-pair start, once, before native input.");
            var owner = world.Registry.FindActive(secondRider);
            ownerSeedPreparationPair = world.ForActor(secondRider);
            Require(owner != null && ownerSeedPreparationPair != null && ReferenceEquals(owner.Boundary, ownerSeedPreparationPair.Relationship) &&
                expectedOwners.TryGetValue(owner.Id, out var generation) && generation == owner.Generation,
                "The second restored owner cannot be captured for native preparation.");
            ownerSeedReleasedOwner = owner.Id;
            ownerSeedPreparation = new JObject { ["contract"] = "owner-seed-checkpoint-native-preparation-v1",
                ["checkpoint"] = request.PersistenceCase, ["before"] = Capture("owner-seed-before-dismount-second-pair"),
                ["slotsBefore"] = JArray.FromObject(slots, MountedSaveCodec.CreateSerializer()) };
            ownerSeedPreparation["controlFactsBefore"] = CaptureOwnerSeedControlFacts();
            evidence["ownerSeedCheckpointPreparation"] = ownerSeedPreparation;
            Input(secondRider, secondRider, controls.DismountAbility, firstRider, "owner-seed-native-dismount-second-pair");
            Next(6);
        }

        private void PollOwnerSeedCheckpointPreparation()
        {
            Require(OwnerSeedSingleOwnerPreparation && ownerSeedPreparation != null && ownerSeedPreparationPair != null && inputs.Count == 1 &&
                !Game.Instance.Player.IsInCombat && !world.UnifiedTurn.HasUnsettledPreparation && persistence.SnapshotCount == snapshotBefore,
                "Native preparation escaped its exploration input/save boundary.");
            if (!InputSettled() || !actors.All(Chunk7NativeFixture.Idle) || world.Registry.FindOwner(secondRider) != null ||
                world.Registry.FindOwner(secondMount) != null || ownerSeedPreparationPair.Relationship.State != Domain.RelationshipState.Unmounted ||
                !ownerSeedPreparationPair.Relationship.Runtime.NoPreparedPairResidue || !ownerSeedPreparationPair.Participant.CanReleaseBoundary ||
                ownerSeedPreparationPair.Combat.HasRetainedPairCommands) return;
            Require(world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 && Mounted(firstRider, firstMount) &&
                !controls.HasUnsettledRelationshipTransition && controls.NativeCastRequestCount == 1 &&
                !world.SerializationBusy && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !NativePersistenceIsolation.HasPendingWrites &&
                expectedOwners.Count == 2 && expectedOwners.ContainsKey(ownerSeedReleasedOwner),
                "Native Dismount did not retire only the captured second owner.");
            var owner = world.Registry.FindActive(firstRider);
            Require(owner != null && owner.Id != ownerSeedReleasedOwner && expectedOwners.TryGetValue(owner.Id, out var generation) && generation == owner.Generation,
                "Native preparation changed the first restored owner or generation.");
            var remaining = controls.CapturePersistentSlots();
            var retained = slots.Where(s => s.ActorId == firstRider.UniqueId).ToArray();
            Require(retained.Length == 2 && remaining.Length == 2 && retained.All(s => remaining.Count(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind) == 1),
                "Native Dismount changed the surviving first-owner bindings or retained second-owner controls.");
            AssertDebt(saved); inherited.RequirePreserved();
            FinishInput(); ownerSeedPreparation["input"] = inputs[0].DeepClone();
            var after = Capture("owner-seed-after-dismount-second-pair");
            after["secondRiderOwnerAbsent"] = world.Registry.FindOwner(secondRider) == null;
            after["secondMountOwnerAbsent"] = world.Registry.FindOwner(secondMount) == null;
            after["secondPairUnmounted"] = ownerSeedPreparationPair.Relationship.State == Domain.RelationshipState.Unmounted;
            after["secondPairNoPreparedResidue"] = ownerSeedPreparationPair.Relationship.Runtime.NoPreparedPairResidue;
            after["secondPairCanRelease"] = ownerSeedPreparationPair.Participant.CanReleaseBoundary;
            after["unsettledPreparation"] = world.UnifiedTurn.HasUnsettledPreparation;
            after["unsettledTransition"] = controls.HasUnsettledRelationshipTransition;
            after["actorsIdle"] = actors.All(Chunk7NativeFixture.Idle);
            after["secondPairCommandsRetained"] = ownerSeedPreparationPair.Combat.HasRetainedPairCommands;
            after["worldSerializationBusy"] = world.SerializationBusy;
            after["saveBusy"] = persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || NativePersistenceIsolation.HasPendingWrites;
            ownerSeedPreparation["after"] = after;
            ownerSeedPreparation["controlFactsAfter"] = CaptureOwnerSeedControlFacts();
            ownerSeedPreparation["slotsAfter"] = JArray.FromObject(remaining, MountedSaveCodec.CreateSerializer());
            expectedOwners.Remove(ownerSeedReleasedOwner); slots = remaining; AssertSlots();
            snapshots.Add(Capture("before-native-save"));
            DispatchCheckpoint();
        }

        private void RequireOwnerSeedPreparedSlots()
        {
            var first = world.Registry.FindActive(firstRider);
            Require(OwnerSeedSingleOwnerPreparation && ownerSeedPreparation?["after"] is JObject && expectedOwners.Count == 1 &&
                !expectedOwners.ContainsKey(ownerSeedReleasedOwner) && world.Registry.Owners.Length == 1 && world.Registry.ActiveCount == 1 &&
                first != null && expectedOwners.TryGetValue(first.Id, out var generation) && generation == first.Generation &&
                Mounted(firstRider, firstMount) && world.Registry.FindOwner(secondRider) == null && world.Registry.FindOwner(secondMount) == null &&
                slots.Length == 2 && slots.All(s => s.ActorId == firstRider.UniqueId),
                "Pending Mount must adopt the exact settled first-owner controls after native second-owner retirement.");
            AssertSlots();
        }

        private void RememberOwnerSeedRemountedSecondPair()
        {
            var first = world.Registry.FindActive(firstRider); var second = world.Registry.FindActive(secondRider);
            Require(OwnerSeedSingleOwnerPreparation && PendingMountSave && ownerSeedPreparation?["after"] is JObject &&
                first != null && second != null && Mounted(firstRider, firstMount) && Mounted(secondRider, secondMount) &&
                world.Registry.Owners.Length == 2 && world.Registry.ActiveCount == 2 && expectedOwners.Count == 1 &&
                expectedOwners.TryGetValue(first.Id, out var generation) && generation == first.Generation &&
                second.Id != ownerSeedReleasedOwner && !expectedOwners.ContainsKey(second.Id),
                "The pending native Mount did not retain A and acquire one distinct new B owner.");
            expectedOwners.Add(second.Id, second.Generation);
            ownerSeedPreparation["remountedSecondOwner"] = new JObject { ["ownerId"] = second.Id.ToString("N"),
                ["generation"] = second.Generation, ["riderId"] = secondRider.UniqueId, ["mountId"] = secondMount.UniqueId };
        }
    }
}
