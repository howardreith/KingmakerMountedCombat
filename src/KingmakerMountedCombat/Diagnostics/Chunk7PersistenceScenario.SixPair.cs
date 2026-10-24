using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.UnitSettings;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // The six-mounted native Manual producer and its separate-process cold consumer,
    // kept apart from every historical two-pair assertion. The producer mounts each
    // exact sourced role once by native selected-ability input, binds twelve owned
    // hotbar slots and writes one native Manual. The consumer restores that graph
    // with no native preparation or Mount replay. Both then route ordinary ground
    // movement through every pair before all-owner cleanup.
    internal sealed partial class Chunk7PersistenceScenario
    {
        private const int SixPairCount = 6;
        private bool SixPairCase => request.OwnedPairs?.SixPairPersistence == true;
        private readonly Guid[] sixOwnerIds = new Guid[SixPairCount];
        private readonly long[] sixGenerations = new long[SixPairCount];
        private int sixIndex, sixMounted;
        private JObject sixObservation;
        private UnitEntityData SixRider(int index) => actors[index * 2];
        private UnitEntityData SixMount(int index) => actors[index * 2 + 1];

        private void BeginSixPairMounts()
        {
            Require(SixPairCase && !Cold && inherited != null && inherited.Bound && actors?.Length == SixPairCount * 2 &&
                actors.Select(actor => actor.UniqueId).SequenceEqual(request.OwnedPairs.PairIds) &&
                GameMountedRelationshipService.NativePairCapacity == SixPairCount && world.Registry.Owners.Length == 0 &&
                sixMounted == 0 && expectedOwners.Count == 0 && inputs.Count == 0 &&
                controls.NativeCastRequestCount == 0 && controls.NativeRefusalCount == 0 && persistence.SnapshotCount == 0,
                "Six-mounted Manual requires twelve exact inherited roles, capacity six and no prior pair custody, input or save.");
            sixObservation = new JObject { ["contract"] = "six-horse-mounted-manual-producer-v1",
                ["fixtureRunId"] = Chunk7OwnedPairsSelection.SixPairSourceRun,
                ["requiredPairs"] = SixPairCount, ["requiredSlots"] = SixPairCount * 2,
                ["requiredMountInputs"] = SixPairCount, ["requiredGroundInputs"] = SixPairCount,
                ["nativeLoadRequests"] = 1, ["nativeSaveRequests"] = 1, ["fixtureSetupWrites"] = 0,
                ["performanceAccepted"] = false, ["requestFreeControlsProven"] = false };
            evidence["sixPairPersistence"] = sixObservation;
            sixIndex = 0; Next(300);
        }

        private void UpdateSixPairPersistence()
        {
            Require(SixPairCase && sixObservation != null && actors?.Length == SixPairCount * 2,
                "Six-pair persistence stage lost its declared twelve-actor scope.");
            switch (stage)
            {
                case 300:
                    if (!actors.All(SixIdle)) return;
                    AssertSixPairOwners();
                    Input(SixRider(sixIndex), SixMount(sixIndex), controls.MountAbility,
                        SixRider((sixIndex + 1) % SixPairCount), "six-mount-" + sixIndex);
                    Next(301); return;
                case 301:
                    if (!InputSettled() || !Mounted(SixRider(sixIndex), SixMount(sixIndex))) return;
                    FinishInput();
                    var owner = world.Registry.FindActive(SixRider(sixIndex));
                    Require(owner != null && !expectedOwners.ContainsKey(owner.Id),
                        "Native Mount did not mint one new exact six-pair owner.");
                    sixOwnerIds[sixIndex] = owner.Id; sixGenerations[sixIndex] = owner.Generation;
                    expectedOwners.Add(owner.Id, owner.Generation);
                    sixMounted++; sixIndex++; AssertSixPairOwners();
                    if (sixIndex < SixPairCount) { Next(300); return; }
                    AfterSixPairMounts(); return;
                case 310:
                    if (!actors.All(SixIdle)) return;
                    AssertSixPairOwners();
                    Move(SixRider(sixIndex), SixMount(sixIndex), SixMount(sixIndex).Position + Vector3.right * 2f,
                        SixRider((sixIndex + 1) % SixPairCount), "six-pair-" + sixIndex);
                    Next(311); return;
                case 311:
                    AssertSixPairOwners();
                    if (!MovementSettled()) return;
                    sixIndex++;
                    if (sixIndex < SixPairCount) { Next(310); return; }
                    Require(movementRows.Count == SixPairCount && inputs.Count == (Cold ? 0 : SixPairCount) &&
                        controls.NativeCastRequestCount == (Cold ? 0 : SixPairCount) && controls.NativeRefusalCount == 0 &&
                        persistence.SnapshotCount == (Cold ? 0 : 1), "Six-pair continuation performed undeclared native input or saves.");
                    sixObservation["nativeCastRequests"] = controls.NativeCastRequestCount;
                    sixObservation["nativeRefusals"] = controls.NativeRefusalCount;
                    sixObservation["groundInputs"] = movementRows.Count;
                    snapshots.Add(Capture("continuation-complete"));
                    Pass("C7-persistence-six-native-continuation"); BeginCleanup(); return;
                default: throw new InvalidOperationException("Unknown six-pair persistence stage " + stage + ".");
            }
        }

        private static bool SixIdle(UnitEntityData actor) => actor != null && actor.IsInState && actor.IsInGame &&
            actor.View?.AgentASP != null && actor.Descriptor.State.IsConscious && !actor.IsInCombat &&
            actor.Commands != null && actor.Commands.Empty;

        private void AfterSixPairMounts()
        {
            Require(sixMounted == SixPairCount && expectedOwners.Count == SixPairCount && inputs.Count == SixPairCount &&
                controls.NativeCastRequestCount == SixPairCount && controls.NativeRefusalCount == 0 && persistence.SnapshotCount == 0,
                "Six native Mounts did not admit exactly one input per sourced role.");
            // The fixture Manual308 held no owned controls. Any already on a hotbar would make
            // the twelve declared bindings ambiguous, so they are refused before binding.
            var managedBefore = controls.CaptureSnapshot().ManagedHotbarSlotCount;
            Require(controls.CapturePersistentSlots().Length == 0 && managedBefore == 0,
                "Six-pair native hotbars already hold KMC controls before the declared binding.");
            sixObservation["managedHotbarSlotsBeforeBinding"] = managedBefore;
            BindSixPairSlots(); snapshots.Add(Capture("before-native-save"));
            Pass("C7-persistence-six-native-mounts");
            DispatchCheckpoint();
        }

        // Each rider binds its own owned Dismount and Rider Primary into an empty native
        // hotbar slot; the persistent capture must then name exactly those twelve slots.
        private void BindSixPairSlots()
        {
            controls.Update();
            for (var index = 0; index < SixPairCount; index++)
            {
                var rider = SixRider(index);
                foreach (var blueprint in new[] { controls.DismountAbility, controls.RiderPrimaryAbility })
                {
                    var fact = rider.Descriptor.Abilities.GetAbility(blueprint); var nativeSlots = rider.UISettings.Slots;
                    var slot = Array.FindIndex(nativeSlots, value => value == null || value is MechanicActionBarSlotEmpty);
                    Require(fact != null && slot >= 0 && slot < 128, "Six-pair native hotbar lacks a declared empty owned slot.");
                    rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = fact.Data }, slot);
                }
            }
            slots = controls.CapturePersistentSlots();
            RequireSixPairSlots(slots);
        }

        private void RequireSixPairSlots(SavedMountedSlot[] value)
        {
            Require(value != null && value.Length == SixPairCount * 2 && Enumerable.Range(0, SixPairCount).All(index =>
                    value.Count(slot => slot.ActorId == SixRider(index).UniqueId) == 2 &&
                    value.Count(slot => slot.ActorId == SixRider(index).UniqueId && slot.Kind == (int)NativeMountedControlKind.Dismount) == 1 &&
                    value.Count(slot => slot.ActorId == SixRider(index).UniqueId && slot.Kind == (int)NativeMountedControlKind.RiderPrimary) == 1),
                "Six-pair native hotbar bindings are not exactly each rider's own Dismount and Rider Primary.");
        }

        // Exactly the first sixMounted roles are mounted, each by its recorded owner and
        // generation; every later role is unowned.
        private void AssertSixPairOwners()
        {
            inherited.RequirePreserved();
            Require(world.Registry.ActiveCount == sixMounted && world.Registry.Owners.Length == sixMounted &&
                expectedOwners.Count == sixMounted && !controls.HasUnsettledRelationshipTransition &&
                !world.UnifiedTurn.HasUnsettledPreparation, "Six-pair native ownership has unsettled or additional custody.");
            for (var index = 0; index < SixPairCount; index++)
            {
                var owner = world.Registry.FindOwner(SixRider(index));
                if (index < sixMounted)
                    Require(owner != null && owner.Id == sixOwnerIds[index] && owner.Generation == sixGenerations[index] &&
                        expectedOwners.TryGetValue(owner.Id, out var generation) && generation == owner.Generation &&
                        Mounted(SixRider(index), SixMount(index)) && ReferenceEquals(world.Registry.FindOwner(SixMount(index)), owner),
                        "Six-pair exact active owner, generation or routing changed at " + index + ".");
                else Require(owner == null && world.Registry.FindOwner(SixMount(index)) == null,
                    "Six-pair unmounted role gained native custody at " + index + ".");
            }
        }

        private JArray SixPairOwnerRows(MountedArchiveData archive) =>
            new JArray(Enumerable.Range(0, SixPairCount).Select(index =>
            {
                var pair = archive.Pairs.Single(value => value.Rider.Id == SixRider(index).UniqueId);
                return new JObject { ["index"] = index, ["riderId"] = pair.Rider.Id, ["mountId"] = pair.Mount.Id,
                    ["ownerId"] = Guid.Parse(pair.PairId).ToString("N"), ["generation"] = pair.Generation, ["profileId"] = pair.ProfileId };
            }));

        // The archive must hold every role exactly once under the owner, generation and
        // declared profile the live registry holds for that role.
        private void RequireSixPairArchiveRoles(MountedArchiveData archive)
        {
            Require(archive != null && archive.Pairs.Length == SixPairCount && archive.Participations.Length == 0 &&
                archive.Combat == null && archive.Slots.Length == SixPairCount * 2, "Six-pair archive lost its exploration graph.");
            for (var index = 0; index < SixPairCount; index++)
            {
                var rider = SixRider(index); var mount = SixMount(index);
                var rows = archive.Pairs.Where(value => value.Rider.Id == rider.UniqueId || value.Mount.Id == mount.UniqueId).ToArray();
                Require(rows.Length == 1 && rows[0].Rider.Id == rider.UniqueId && rows[0].Mount.Id == mount.UniqueId &&
                    Guid.Parse(rows[0].PairId) == sixOwnerIds[index] && rows[0].Generation == sixGenerations[index] &&
                    rows[0].ProfileId == SupportedMountedProfiles.Resolve(rider, mount)?.Id,
                    "Six-pair archive role, owner, generation or profile differs at " + index + ".");
            }
        }

        private void RecordSixPairSaved()
        {
            RequireSixPairArchiveRoles(saved); RequireSixPairSlots(saved.Slots);
            sixObservation["savedPairs"] = saved.Pairs.Length; sixObservation["savedSlots"] = saved.Slots.Length;
            sixObservation["owners"] = SixPairOwnerRows(saved);
            sixObservation["nativeCastRequestsBeforeSave"] = controls.NativeCastRequestCount;
            sixObservation["nativeRefusalsBeforeSave"] = controls.NativeRefusalCount;
            Pass("C7-persistence-six-mounted-archive");
        }

        // The host's unattended terminal FAIL admits only this engine's own committed,
        // hashed retained-cleanup failure of a six-pair case in its unchanged native world.
        // The same committed retained failure for a two-pair owned persistence run.
        internal void RequireRetainedFailureScope()
        {
            Require(!SixPairCase && captured && cleanup && !cleanupProven && pendingHostCustody && !disposed &&
                cleanupPendingReported && !string.IsNullOrEmpty(RetainedFailureArtifactSha256) &&
                errors.Count != 0 && results.Any(row => row.Status == "FAIL"),
                "No committed retained two-pair persistence failure owns this world.");
            Require(nativeScope != null, "Captured native world authority is absent.");
            // A refused area restore leaves the party in the declared destination (b195 x4c).
            // Only then is the destination world admitted, through the scope's existing exact
            // check: the same game, player, state, campaign and main character, the declared area
            // blueprint and scene state, and every tracked actor identity unreplaced.
            if (persistence.FailedAreaPairCount != 0 && replayDestination != null &&
                ReferenceEquals(Kingmaker.Game.Instance?.CurrentlyLoadedArea, replayDestination))
                nativeScope.RequireDeclaredAreaDestination(replayDestination.AssetGuidThreadSafe, replayDestination);
            else nativeScope.RequireCurrent();
        }
        internal void RequireRetainedSixFailureScope()
        {
            Require(SixPairCase && captured && cleanup && !cleanupProven && pendingHostCustody && !disposed &&
                cleanupPendingReported && !string.IsNullOrEmpty(RetainedFailureArtifactSha256) &&
                errors.Count != 0 && results.Any(row => row.Status == "FAIL"),
                "No committed retained six-pair persistence failure owns this world.");
            Require(nativeScope != null, "Captured native world authority is absent.");
            nativeScope.RequireCurrent();
        }

        private void BeginSixPairContinuation()
        {
            Require(SixPairCase && sixMounted == SixPairCount && movementRows.Count == 0, "Six-pair continuation started before all six pairs.");
            sixIndex = 0; Next(310);
        }

        private void ObserveSixPairCold()
        {
            saved = persistence.LoadedArchive;
            Require(saved != null && saved.SchemaVersion == 3 && saved.Pairs.Length == SixPairCount && saved.Participations.Length == 0 &&
                saved.Combat == null && saved.Slots.Length == SixPairCount * 2 && saved.CampaignId == request.PersistenceLoad.GameId &&
                saved.AreaId == ExpectedArchiveArea && persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "Entire six-pair exploration schema 3 graph was not restored from the selected native archive: " + persistence.Feedback);
            BindOwnedFixture(false);
            Require(actors?.Length == SixPairCount * 2 && actors.Select(actor => actor.UniqueId).SequenceEqual(request.OwnedPairs.PairIds) &&
                sixMounted == 0 && expectedOwners.Count == 0, "Six-pair cold consumer lost its twelve exact inherited roles.");
            for (var index = 0; index < SixPairCount; index++)
            {
                var pair = saved.Pairs.Single(value => value.Rider.Id == SixRider(index).UniqueId);
                sixOwnerIds[index] = Guid.Parse(pair.PairId); sixGenerations[index] = pair.Generation;
                expectedOwners.Add(sixOwnerIds[index], pair.Generation);
            }
            sixMounted = SixPairCount;
            RequireSixPairArchiveRoles(saved); AssertSixPairOwners();
            slots = saved.Slots; RequireSixPairSlots(slots); AssertSlots();
            Require(persistence.SemanticRestoreCount == SixPairCount * 2 && persistence.PresentationRestoreCount == SixPairCount &&
                persistence.ArchivePostLoadObservedCount == SixPairCount * 2 && controls.NativeCastRequestCount == 0 &&
                controls.NativeRefusalCount == 0 && persistence.SnapshotCount == 0,
                "Six-pair cold restore replayed attachment/native input or did not restore each actor exactly once.");
            var ids = actors.Select(actor => actor.UniqueId).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 && restoration.Count("restore-actor-before", ids) == 0 &&
                restoration.Count("restore-debt-before", ids) == SixPairCount * 2 &&
                restoration.Count("restore-debt-after", ids) == SixPairCount * 2,
                "Six-pair cold restoration invented preparation or lost its complete observer window.");
            AssertDebt(saved); evidence["coldRestoration"] = restoration.Capture();
            restoration.Dispose(); restoration = null;
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var descriptor = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
            sixObservation = new JObject { ["contract"] = "six-horse-mounted-manual-cold-v1",
                ["fixtureRunId"] = Chunk7OwnedPairsSelection.SixPairSourceRun,
                ["producerRunId"] = request.OwnedPairs.CheckpointSourceRunId,
                ["producerArtifactSha256"] = request.OwnedPairs.CheckpointSourceArtifactSha256,
                ["requiredPairs"] = SixPairCount, ["requiredSlots"] = SixPairCount * 2, ["requiredGroundInputs"] = SixPairCount,
                ["nativeLoadRequests"] = 1, ["nativeSaveRequests"] = 0,
                ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
                ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount, ["owners"] = SixPairOwnerRows(saved),
                ["performanceAccepted"] = false, ["requestFreeControlsProven"] = false };
            evidence["sixPairPersistence"] = sixObservation;
            snapshots.Add(Capture("cold-restored-before-input")); Pass("C7-persistence-cold-all-pair-identity-and-no-prepare");
        }
    }
}
