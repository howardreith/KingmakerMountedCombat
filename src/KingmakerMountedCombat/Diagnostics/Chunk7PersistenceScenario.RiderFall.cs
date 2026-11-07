using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // F6 (FALL-01/FALL-02) persistence route on the F5 named-companion world. Producer: the F5 naming rows (both pets keep
    // the duplicate Unicode name), both pairs mounted, then in real time the Riding Dog pair's voluntary Dismount through
    // its public control and an ordinary remount (no fall), then the Riding Dog knocked prone by the native knock-off
    // writer with the soft fall labelled to fail: one native check, 1d6 and prone for that rider only, while the
    // identically named Mastodon pair stays mounted. Both stand natively; the post-fall world (one mounted pair, the
    // fallen rider unmounted and hurt) is saved through the ordinary native Manual. Consumer, in a fresh process: the
    // archive's one pair is restored, the unseated rider stays unmounted with its saved damage, no fall, check or damage is
    // replayed, and the ordinary Mount control seats it again before the F5 sheet rows.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string RiderFallCaseName = "rider-fall-integration-manual";
        private const string RiderFallContract = "finalize6-rider-fall-persistence-v1";
        private bool RiderFallCase => request.PersistenceCase == RiderFallCaseName;
        private readonly JObject f6 = new JObject();
        private bool f6Unseated;
        private long f6ResolvedAt, f6BegunAt;

        private static MountedRiderFallService FallService =>
            MountedRiderFallService.Current ?? throw new InvalidOperationException("The rider fall service is not present.");

        private static JObject FallServiceState()
        {
            var service = FallService;
            return new JObject { ["begun"] = service.BegunCount, ["resolved"] = service.ResolvedCount, ["abandoned"] = service.AbandonedCount,
                ["pending"] = service.PendingCount, ["records"] = new JArray(service.Records.Select(Phase3dHorseScenarioTranche.F6Record)) };
        }

        private JObject FallPair(UnitEntityData pairRider)
        {
            var owner = world.Registry.FindOwner(pairRider);
            var relation = world.Pairs.FirstOrDefault(pair => ReferenceEquals(pair.Relationship.Rider, pairRider) || owner != null && ReferenceEquals(pair.Relationship.PairOwner, owner))?.Relationship;
            return new JObject { ["mounted"] = owner != null && Mounted(pairRider, pairRider == firstRider ? firstMount : secondMount),
                ["ownerId"] = owner?.Id.ToString("N"), ["generation"] = owner?.Generation, ["state"] = owner?.Boundary?.State.ToString() ?? "None",
                ["relationshipGeneration"] = relation?.MountedPairGeneration };
        }

        private JObject FallWorld(string label) => new JObject { ["label"] = label, ["frame"] = Time.frameCount, ["service"] = FallServiceState(),
            ["firstPair"] = FallPair(firstRider), ["secondPair"] = FallPair(secondRider), ["firstRider"] = Phase3dHorseScenarioTranche.F6Unit(firstRider),
            ["firstMount"] = Phase3dHorseScenarioTranche.F6Unit(firstMount), ["secondRider"] = Phase3dHorseScenarioTranche.F6Unit(secondRider),
            ["secondMount"] = Phase3dHorseScenarioTranche.F6Unit(secondMount), ["log"] = Phase3dHorseScenarioTranche.F6LogTailRows(10) };

        private bool Disclose(string action)
        {
            ((JArray)f6["disclosedSetup"]).Add(new JObject { ["frame"] = Time.frameCount, ["action"] = action });
            return true;
        }

        // The rider's own public Mount / Dismount control on its bar, else the hidden Dismount shell (the F3 route), recorded.
        private bool VoluntaryDismount()
        {
            var control = controls.MountStateAbility; var slots = secondRider.UISettings?.Slots; var index = -1;
            if (control != null && slots != null)
                for (var i = 0; i < slots.Length && index < 0; i++)
                    if (ReferenceEquals((slots[i] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, control)) index = i;
            Chunk7NativeFixture.Select(secondRider);
            controls.Update();
            if (index >= 0)
            {
                f6["voluntaryRoute"] = "public-control"; f6["voluntaryIndex"] = index;
                (slots[index] as MechanicActionBarSlotAbility)?.OnClick();
                var handler = Game.Instance.SelectedAbilityHandler;
                if (handler?.Ability != null) handler.DropAbility();
                return true;
            }
            f6["voluntaryRoute"] = "dismount-shell";
            return Sprint5Fixture.Click(controls, secondRider, controls.DismountAbility, secondRider);
        }

        private bool KnockOffDog()
        {
            var service = FallService;
            f6ResolvedAt = service.ResolvedCount; f6BegunAt = service.BegunCount;
            var id = secondRider.UniqueId;
            service.ScenarioOutcome = record => record.RiderId == id ? false : (bool?)null;
            Disclose("The Riding Dog pair's soft fall is labelled to fail (the d20 one below the native DC).");
            var allowed = secondMount.CanBeKnockedOff();
            if (allowed) secondMount.Descriptor.State.Prone.ShouldBeActive = true;
            Disclose("The Riding Dog was knocked prone by the native knock-off writer; allowed=" + allowed + ".");
            f6["knockOff"] = new JObject { ["canBeKnockedOff"] = allowed, ["frame"] = Time.frameCount };
            return true;
        }

        private bool DogFallResolved()
        {
            if (Mounted(secondRider, secondMount) || FallService.ResolvedCount <= f6ResolvedAt) return false;
            f6["fallAfter"] = FallWorld("fall-after");
            f6["fall"] = Phase3dHorseScenarioTranche.F6Record(FallService.Records.LastOrDefault(record => record.RiderId == secondRider.UniqueId));
            f6["placement"] = new JObject { ["riderToMount"] = Vector3.Distance(secondRider.Position, secondMount.Position) };
            FallService.ScenarioOutcome = null;
            return true;
        }

        private bool BothStanding() => (!secondRider.Descriptor.State.Prone.Active && !secondMount.Descriptor.State.Prone.Active && f5Frames > 30) || f5Frames > 900;

        // Producer: after the F5 rows and both native mounts, the real-time fall rows, then the ordinary Manual.
        private void BeginRiderFallRows()
        {
            f6["contract"] = RiderFallContract; f6["disclosedSetup"] = new JArray(); evidence["riderFall"] = f6;
            f6["policy"] = new JObject { ["dc"] = MountedRiderFallPolicy.DifficultyClass, ["damageDice"] = MountedRiderFallPolicy.DamageDice,
                ["damageDieSides"] = MountedRiderFallPolicy.DamageDieSides, ["stat"] = "SkillMobility" };
            f6["start"] = FallWorld("start");
            var steps = new List<KeyValuePair<string, Func<bool>>>();
            Step(steps, "voluntary-dismount", () => ActorsIdle() && Record6("voluntaryBefore", FallWorld("voluntary-before")) && VoluntaryDismount());
            Step(steps, "voluntary-settled", () => !Mounted(secondRider, secondMount) && ActorsIdle() && Settled(20) && Record6("voluntaryAfter", FallWorld("voluntary-after")));
            Step(steps, "remount", () => Sprint5Fixture.Click(controls, secondRider, controls.MountAbility, secondMount));
            Step(steps, "remount-settled", () => Mounted(secondRider, secondMount) && ActorsIdle() && Settled(45) && Record6("remounted", FallWorld("remounted")));
            Step(steps, "knock-off", () => Record6("fallBefore", FallWorld("fall-before")) && KnockOffDog());
            Step(steps, "fall-resolved", () => DogFallResolved());
            Step(steps, "fall-settled", () => Settled(30) && Record6("fallSettled", FallWorld("fall-settled")));
            Step(steps, "fall-shots", () => Ui("f6-rt-fall-log") && World("f6-rt-fall-side", 90f));
            Step(steps, "standing", () => BothStanding() && Record6("standing", FallWorld("standing")));
            StartSteps(steps, AfterRiderFall);
            Next(177);
        }

        private bool Record6(string key, JToken value) { f6[key] = value; return true; }

        private void AfterRiderFall()
        {
            f6Unseated = true;
            // The bar as it stands at the save; the archive must carry exactly these controls.
            slots = controls.CapturePersistentSlots(); RequirePostF2Slots();
            f6["slotsAtSave"] = new JArray(slots.Select(s => new JObject { ["actor"] = s.ActorId, ["index"] = s.Index, ["kind"] = s.Kind }));
            AssertOwners();
            f6["atSave"] = FallWorld("at-save");
            snapshots.Add(Capture("rider-fall-before-native-save"));
            Pass("F6-rider-fall-rt-producer");
            RequestSave(); Next(4);
        }

        // One mounted pair (the Mastodon's, with its owner and generation unchanged) and the unseated Riding Dog pair with no owner.
        private void AssertRiderFallOwners()
        {
            Require(Mounted(firstRider, firstMount) && !Mounted(secondRider, secondMount) && world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 &&
                world.Registry.Owners.All(owner => expectedOwners.TryGetValue(owner.Id, out var generation) && generation == owner.Generation) &&
                world.Registry.FindOwner(secondRider) == null && world.Registry.FindOwner(secondMount) == null,
                "The post-fall world has another mounted owner, a changed generation or a retained owner for the unseated pair.");
        }

        // Consumer: the archive's single pair, then the unseated pair's actual native state.
        private void ObserveRiderFallCold()
        {
            saved = persistence.LoadedArchive;
            Require(saved != null && saved.SchemaVersion == 3 && saved.Pairs.Length == 1 && saved.Participations.Length == 0 &&
                saved.Combat == null && saved.AreaId == ColdInputArea && persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "The post-fall exploration graph was not restored from the selected native archive: " + persistence.Feedback);
            var pair = saved.Pairs.Single();
            Require(pair.ProfileId == SupportedMountedProfiles.Mammoth.Id && pair.Rider.Id == Chunk7WorldScenarioEngine.MammothRiderId &&
                pair.Mount.Id == Chunk7WorldScenarioEngine.MammothId, "The post-fall archive pair is not the Mastodon pair.");
            firstRider = Exact(pair.Rider.Id); firstMount = Exact(pair.Mount.Id);
            secondRider = Exact(Chunk7WorldScenarioEngine.HorseRiderId); secondMount = secondRider.Descriptor.Pet;
            Require(secondMount != null && secondMount.Blueprint?.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid &&
                ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) && ReferenceEquals(firstRider.Descriptor.Pet, firstMount) &&
                ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider), "Cold load lost reciprocal native companion ownership.");
            actors = new[] { firstRider, firstMount, secondRider, secondMount };
            TrackNativeActors();
            expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation);
            f6Unseated = true;
            AssertOwners(); slots = saved.Slots; AssertSlots();
            Require(persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1 && persistence.ArchivePostLoadObservedCount == 2 &&
                controls.NativeCastRequestCount == 0 && persistence.SnapshotCount == 0,
                "Cold restore replayed attachment or native input, or restored the unseated pair.");
            var mainIds = new[] { firstRider.UniqueId, firstMount.UniqueId }; var otherIds = new[] { secondRider.UniqueId, secondMount.UniqueId };
            var ids = mainIds.Concat(otherIds).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 && restoration.Count("round-before", ids) == 0 &&
                restoration.Count("restore-debt-before", mainIds) == 2 && restoration.Count("restore-debt-after", mainIds) == 2 &&
                restoration.Count("restore-debt-before", otherIds) == 0 && restoration.Count("restore-debt-after", otherIds) == 0,
                "Cold native actor restoration invented preparation or restored the unseated pair.");
            AssertDebt(saved); evidence["coldRestoration"] = restoration.Capture();
            restoration.Dispose(); restoration = null;
            f6["contract"] = RiderFallContract; f6["disclosedSetup"] = new JArray(); evidence["riderFall"] = f6;
            f6["cold"] = FallWorld("cold-restored-before-input");
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var descriptor = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
            snapshots.Add(Capture("cold-restored-before-input")); Pass("C7-persistence-cold-post-fall-identity-and-no-prepare");
        }

        // After the Mastodon pair's ordinary continuation: the unseated rider's ordinary Mount control, then the F5 sheet rows.
        private void BeginRiderFallCold()
        {
            RecordCompanionNamesCold();
            var steps = new List<KeyValuePair<string, Func<bool>>>();
            Step(steps, "cold-shot", () => Ui("f6-cold-post-fall") && World("f6-cold-unseated-side", 90f));
            Step(steps, "cold-remount", () => ActorsIdle() && Record6("coldBeforeRemount", FallWorld("cold-before-remount")) &&
                Sprint5Fixture.Click(controls, secondRider, controls.MountAbility, secondMount));
            Step(steps, "cold-remounted", () => Mounted(secondRider, secondMount) && ActorsIdle() && Settled(45) && Record6("coldRemounted", FallWorld("cold-remounted")));
            Step(steps, "cold-remount-shot", () => World("f6-cold-remounted-side", 90f));
            StartSteps(steps, BeginCompanionNamesColdSheet);
            Next(177);
        }
    }
}
