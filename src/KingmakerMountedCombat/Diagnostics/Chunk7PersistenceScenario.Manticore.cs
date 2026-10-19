using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.UnitSettings;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool ManticoreFixtureCase => OwnerSeedCheckpointSave && !Cold &&
            request.PersistenceCase == Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint;
        private BlueprintAreaEnterPoint manticoreEntry;
        private JObject manticorePreparation;
        private Guid[] manticoreOriginalOwners;
        private SavedMountedSlot[] manticoreOriginalSlots;
        private RetainedObjectIdentity manticoreIdentities;
        private bool manticoreDispatched, manticoreScopeAdopted;
        private int manticoreLoadingTicks;
        private ManticoreHotbarPin[] manticoreHotbars;

        private sealed class ManticoreHotbarPin
        {
            internal UnitEntityData Actor;
            internal object Owner;
            internal object[] Collection, Elements;
            internal bool[] Managed;
        }

        private void BeginManticoreFixture()
        {
            Require(ManticoreFixtureCase && manticorePreparation == null && inherited?.Bound == true &&
                inputs.Count == 0 && snapshots.Count == 0 && expectedOwners.Count == 2 && slots?.Length == 4,
                "Manticore fixture preparation requires its restored start before any source mutation.");
            RequireManticoreReady(); AssertOwners(); AssertSlots();
            JObject prerequisite;
            manticoreEntry = Chunk7OwnerSeedManticoreEntry.Resolve(Game.Instance, request, out prerequisite);
            manticoreOriginalOwners = expectedOwners.Keys.ToArray(); manticoreOriginalSlots = slots.ToArray();
            manticoreIdentities = new RetainedObjectIdentity(4096);
            manticoreHotbars = inherited.Party.Select(actor => {
                var collection = actor.UISettings?.Slots;
                Require(collection == null || collection.Length <= 140, "Manticore hotbar exceeds the installed native array capacity.");
                return new ManticoreHotbarPin { Actor = actor, Owner = actor.UISettings, Collection = collection,
                    Elements = collection?.Cast<object>().ToArray(), Managed = collection?.Select(value => ManticoreControlKind(value) != 0).ToArray() };
            }).ToArray();
            manticorePreparation = new JObject { ["contract"] = "owner-seed-manticore-fixture-v1", ["checkpoint"] = request.PersistenceCase,
                ["prerequisite"] = prerequisite, ["transferRequests"] = 0, ["nativeMode"] = "None",
                ["navigationAssisted"] = true, ["ordinaryTravelProof"] = false,
                ["nativeAreaCallbackExpected"] = false, ["explicitSaveGameDispatches"] = 0,
                ["originalSlots"] = JArray.FromObject(manticoreOriginalSlots, MountedSaveCodec.CreateSerializer()) };
            evidence["manticoreFixturePreparation"] = manticorePreparation;
            manticorePreparation["source"] = CaptureManticoreWorld("source-mounted");
            Input(firstRider, firstRider, controls.DismountAbility, secondRider, "manticore-dismount-first-pair"); Next(90);
        }

        // This owns the asynchronous entry interval before the ordinary captured
        // source scope can be used again. None means no transition save is requested.
        private bool UpdateManticoreFixture()
        {
            if (!ManticoreFixtureCase || stage < 90 || stage > 95) return false;
            if (stage == 92)
            {
                nativeScope.RequireTransferWorld();
                if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any())
                { manticoreLoadingTicks++; return true; }
                AdoptManticoreFixtureWorld();
                RequireManticoreReady(); RequireManticoreUnmounted(); AssertManticoreHotbars();
                AssertAreaNativeDebt();
                Require(manticoreLoadingTicks > 0, "Manticore entry missed its native loading boundary.");
                manticorePreparation["destinationUnmounted"] = CaptureManticoreWorld("destination-unmounted");
                manticorePreparation["loadingTicks"] = manticoreLoadingTicks;
                Input(firstRider, firstMount, controls.MountAbility, secondRider, "manticore-mount-first-pair"); Next(93); return true;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) return true;
            if (stage == 95)
            {
                RequireNativeWorld(); inherited.RequirePreserved();
                if (!callback || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return true;
                ObserveSaved(); RequireManticoreReady(); AssertManticoreHotbars();
                manticorePreparation["afterSave"] = CaptureManticoreWorld("native-manual-complete");
                manticorePreparation["completedArchiveSha256"] = evidence["nativeWrite"]?["sha256"];
                Pass("C7-persistence-manticore-fixture-native-manual"); Next(10); return true;
            }
            RequireManticoreReady(false);
            if (!InputSettled() || !inherited.Party.All(Chunk7NativeFixture.Idle)) return true;
            FinishInput(); AssertManticoreHotbars();
            if (stage == 90)
            {
                var survivor = world.Registry.FindActive(secondRider);
                Require(world.Registry.FindOwner(firstRider) == null && world.Registry.FindOwner(firstMount) == null &&
                    world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 && Mounted(secondRider, secondMount) &&
                    survivor != null && expectedOwners.TryGetValue(survivor.Id, out var generation) && survivor.Generation == generation,
                    "The first ordinary Dismount did not retire only its original owner.");
                Input(secondRider, secondRider, controls.DismountAbility, firstRider, "manticore-dismount-second-pair"); Next(91);
            }
            else if (stage == 91)
            {
                RequireManticoreUnmounted();
                Require(controls.NativeCastRequestCount == 2 && inputs.Count == 2 && persistence.SnapshotCount == 0,
                    "Unmounted fixture setup must spend exactly its two native Dismount inputs before entry.");
                expectedOwners.Clear();
                manticorePreparation["sourceUnmounted"] = CaptureManticoreWorld("source-unmounted");
                areaActorDebt = actors.Select(MountedPersistenceService.CaptureActor).ToArray();
                areaNativeTicks = Game.Instance.TimeController.GameTime.Ticks;
                inherited.BeginManticorePreparation(manticoreEntry.Area);
                Next(92); manticoreDispatched = true;
                manticorePreparation["transferRequests"] = 1;
                Game.Instance.LoadArea(manticoreEntry, AutoSaveMode.None);
                manticorePreparation["dispatchReturned"] = true;
            }
            else if (stage == 93)
            {
                Require(Mounted(firstRider, firstMount) && world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 &&
                    world.Registry.FindOwner(secondRider) == null && world.Registry.FindOwner(secondMount) == null &&
                    !manticoreOriginalOwners.Contains(world.Registry.FindActive(firstRider).Id),
                    "The first destination Mount acquired another actor or retained the second owner.");
                Input(secondRider, secondMount, controls.MountAbility, firstRider, "manticore-mount-second-pair"); Next(94);
            }
            else if (stage == 94)
            {
                Require(controls.NativeCastRequestCount == 4 && inputs.Count == 4 && expectedOwners.Count == 0 &&
                    world.Registry.Owners.Length == 2 && world.Registry.Owners.All(owner => !manticoreOriginalOwners.Contains(owner.Id)),
                    "Destination Mounts must create exactly two fresh native owner identities.");
                RememberOwners(); AssertOwners(); RebindManticoreSlots(); RequireManticoreReady();
                snapshots.Add(Capture("before-native-save"));
                manticorePreparation["remounted"] = CaptureManticoreWorld("destination-remounted");
                manticorePreparation["reboundSlots"] = JArray.FromObject(slots, MountedSaveCodec.CreateSerializer());
                manticorePreparation["controlRebindings"] = 4;
                manticorePreparation["explicitSaveGameDispatches"] = 1;
                RequestSave(); Next(95);
            }
            return true;
        }

        private void AdoptManticoreFixtureWorld()
        {
            if (manticoreScopeAdopted) { RequireNativeWorld(); return; }
            Require(ManticoreFixtureCase && manticoreDispatched && stage == 92 && manticoreEntry?.Area != null &&
                !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any(),
                "Manticore destination custody cannot be adopted before its sole completed native entry.");
            inherited.AdoptManticorePreparation(manticoreEntry.Area);
            nativeScope = inherited.Scope; manticoreScopeAdopted = true;
        }

        private void RequireManticoreReady(bool idle = true)
        {
            RequireNativeWorld(); inherited.RequirePreserved();
            Require(Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat &&
                Game.Instance.CurrentlyLoadedArea != null && !Game.Instance.CurrentlyLoadedArea.IsCapital &&
                inherited.Party.All(actor => actor.IsDirectlyControllable && actor.IsInState && actor.IsInGame &&
                    !actor.IsInCombat && actor.Descriptor.State.IsConscious && actor.View != null &&
                    ReferenceEquals(actor.View.EntityData, actor) && (!idle || Chunk7NativeFixture.Idle(actor) && actor.Commands.Queue.Count == 0)) &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && persistence.FailedAreaPairCount == 0 &&
                !persistence.AreaTransitionPending && persistence.NativeWorldDisposalCount == 0 &&
                replaySaveAuthority.AdmittedNativeLoadRequests == 1 && replayAuthorization.AuthorizedLoadCount == 1 &&
                replaySaveAuthority.AdmittedNativeWriteRequests <= 1 && replayAuthorization.AuthorizedWriteCount <= 1,
                "Manticore fixture escaped its native default-world, full-party control, restoration or sole-load/write boundary.");
        }

        private void RequireManticoreUnmounted() => Require(world.Registry.Owners.Length == 0 && world.Registry.ActiveCount == 0 &&
            !world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition &&
            world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null &&
                pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasRetainedPairCommands) &&
            controls.CapturePersistentSlots().Length == 0,
            "Both original pairs must be completely natively dismounted before the unmounted fixture entry.");

        private int ManticoreControlKind(object value)
        {
            var blueprint = (value as MechanicActionBarSlotAbility)?.Ability?.Blueprint;
            var kinds = new[] { controls.MountAbility, controls.DismountAbility, controls.RiderPrimaryAbility, controls.MountPrimaryAbility, controls.MountedChargeAbility };
            return blueprint == null ? 0 : Array.FindIndex(kinds, item => ReferenceEquals(item, blueprint)) + 1;
        }

        private void AssertManticoreHotbars()
        {
            foreach (var pin in manticoreHotbars)
            {
                var current = pin.Actor.UISettings?.Slots;
                Require(ReferenceEquals(pin.Owner, pin.Actor.UISettings) && ReferenceEquals(pin.Collection, current) &&
                    (pin.Elements == null ? current == null : current != null && current.Length == pin.Elements.Length),
                    "Fixture preparation replaced an unrelated native hotbar owner or collection.");
                if (current == null) continue;
                for (var index = 0; index < current.Length; index++)
                    Require(pin.Managed[index] ? current[index] == null ||
                        current[index] is MechanicActionBarSlotEmpty empty && ReferenceEquals(empty.Unit, pin.Actor) ||
                        current[index] is MechanicActionBarSlotAbility ability && ReferenceEquals(ability.Unit, pin.Actor) && ManticoreControlKind(ability) != 0 :
                        ReferenceEquals(pin.Elements[index], current[index]),
                        "Fixture preparation changed an unrelated native hotbar element.");
            }
        }

        private void RebindManticoreSlots()
        {
            controls.Update(); AssertManticoreHotbars();
            var prior = controls.CapturePersistentSlots();
            Require(prior.Length <= 4 && prior.All(current => manticoreOriginalSlots.Count(binding =>
                current.ActorId == binding.ActorId && current.Index == binding.Index && current.Kind == binding.Kind) == 1),
                "Fixture remount found an unexpected live binding outside the four original managed positions.");
            foreach (var binding in manticoreOriginalSlots)
            {
                var actor = Exact(binding.ActorId); var native = actor.UISettings?.Slots;
                var blueprint = binding.Kind == 2 ? controls.DismountAbility : binding.Kind == 3 ? controls.RiderPrimaryAbility : null;
                Require(native != null && binding.Index >= 0 && binding.Index < native.Length && blueprint != null &&
                    ManticoreControlKind(native[binding.Index]) == binding.Kind,
                    "Fixture control binding refuses to overwrite another native hotbar element.");
                var fact = actor.Descriptor.Abilities.GetAbility(blueprint);
                Require(fact != null && fact.Active && fact.Data != null && ReferenceEquals(fact.Data.Caster?.Unit, actor),
                    "Fixture remount has no exact active native control fact to bind.");
                actor.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = actor, Ability = fact.Data }, binding.Index);
            }
            slots = controls.CapturePersistentSlots();
            Require(slots.Length == 4 && manticoreOriginalSlots.All(binding => slots.Count(current =>
                current.ActorId == binding.ActorId && current.Index == binding.Index && current.Kind == binding.Kind) == 1),
                "Fixture remount did not retain exactly the four original managed slot positions.");
            AssertSlots(); AssertManticoreHotbars();
        }

        private JObject CaptureManticoreWorld(string label)
        {
            var game = Game.Instance;
            return new JObject { ["label"] = label, ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["game"] = manticoreIdentities.Get(game), ["player"] = manticoreIdentities.Get(game.Player),
                ["state"] = manticoreIdentities.Get(game.State), ["areaState"] = manticoreIdentities.Get(game.State.LoadedAreaState),
                ["mainState"] = manticoreIdentities.Get(game.State.LoadedAreaState.MainState), ["worldScope"] = manticoreIdentities.Get(nativeScope),
                ["mode"] = game.CurrentMode.ToString(), ["capital"] = game.CurrentlyLoadedArea.IsCapital,
                ["directlyControllable"] = new JArray(inherited.Party.Select(actor => actor.UniqueId)),
                ["field"] = inherited.CaptureReplayField(manticoreIdentities.Get, "replay-field"), ["native"] = Capture(label),
                ["activeSlots"] = JArray.FromObject(controls.CapturePersistentSlots(), MountedSaveCodec.CreateSerializer()),
                ["controlFacts"] = CaptureOwnerSeedControlFacts(),
                ["authority"] = OwnedAuthoritySnapshot(),
                ["hotbars"] = new JArray(manticoreHotbars.Select(pin => new JObject {
                    ["actorId"] = pin.Actor.UniqueId, ["owner"] = manticoreIdentities.Get(pin.Actor.UISettings),
                    ["collection"] = manticoreIdentities.Get(pin.Actor.UISettings?.Slots),
                    ["elements"] = pin.Actor.UISettings?.Slots == null ? null : new JArray(pin.Actor.UISettings.Slots.Select(value => manticoreIdentities.Get(value))),
                    ["managed"] = pin.Managed == null ? null : new JArray(pin.Managed) })) };
        }

        private void ReleaseManticoreObservation()
        {
            if (manticoreIdentities == null || manticoreIdentities.Released) return;
            Require(!NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence,
                "Manticore fixture observation still owns a native save boundary.");
            var ids = manticoreIdentities.Ids; manticoreIdentities.Dispose();
            if (manticorePreparation != null) manticorePreparation["identityRetention"] = new JObject { ["capacity"] = manticoreIdentities.Capacity,
                ["ids"] = new JArray(ids), ["faults"] = manticoreIdentities.FaultCount,
                ["released"] = manticoreIdentities.Released, ["retainedAfterRelease"] = manticoreIdentities.RetainedCount };
            manticoreHotbars = null;
        }
    }
}
