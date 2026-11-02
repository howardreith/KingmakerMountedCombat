using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 5 (POSE-01, SIZE-01, DOG-01) rows on the disposable Working fixture. The tranche's own Medium/Mammoth pair is the
    // neighbor control. One free party member becomes a Ranger through the native level-up pipeline and chooses a Horse or, in
    // its own launch, a Riding Dog from the real selection. Its Small bodies are disclosed setup (a native Doll and race, with
    // the native view rebuild a polymorph's end uses); nothing about a body is granted to the mount or to admission. The
    // reader owns PASS/FAIL, and a person inspects the captured images.
    // Shared by the Sprint 5 rows and the Riding Dog save route. Fixture setup and evidence only; never ordinary play.
    internal static class Sprint5Fixture
    {
        internal const string HalflingRace = "b0c3ef2729c498f47970bb50fa1acd30";

        // Disclosed setup on a disposable actor: a native Doll and race, then the view rebuild Polymorph.RestoreView uses.
        internal static void ApplyBody(UnitEntityData unit, string raceGuid, Gender gender)
        {
            var race = BlueprintRoot.Instance.Progression.CharacterRaces.Single(value => value.AssetGuid == raceGuid);
            var doll = new DollState();
            doll.SetRace(race); doll.SetGender(gender); doll.Validate();
            unit.Descriptor.Progression.SetRace(race);
            unit.Descriptor.State.Size = race.Size;
            unit.Descriptor.Doll = doll.CreateData();
            var old = unit.View;
            unit.AttachToViewOnLoad(null);
            unit.View.transform.SetParent(old.transform.parent, false);
            unit.View.transform.position = old.transform.position;
            unit.View.transform.rotation = old.transform.rotation;
            Game.Instance.UI.SelectionManager?.ForceCreateMarks();
            UnityEngine.Object.Destroy(old.gameObject);
        }

        internal static bool BodyReady(UnitEntityData unit)
        {
            var avatar = unit.View?.CharacterAvatar;
            return avatar != null && !avatar.IsDirty && avatar.Animator != null && unit.View.Animator != null;
        }

        internal static bool Click(NativeMountedControlService controls, UnitEntityData caster, BlueprintAbility blueprint, UnitEntityData target)
        {
            SelectionManager.Instance.SelectUnit(caster.View, true, true, false);
            controls.Update();
            var data = caster.Descriptor.Abilities.GetAbility(blueprint)?.Data;
            var handler = Game.Instance.SelectedAbilityHandler;
            var targetObject = target?.View?.gameObject;
            if (data == null || handler == null || targetObject == null) return false;
            handler.SetAbility(data);
            return handler.OnClick(targetObject, target.Position, 0, false, false);
        }

        internal static Transform Bone(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(node => node.name == name);

        internal static JObject Vector(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };

        // The gameplay camera is posed only for this render inside one end-of-frame and restored before anything else runs.
        internal static IEnumerator Capture(string evidenceRoot, JArray shots, string name, UnitEntityData focusUnit, UnitEntityData riderUnit, UnitEntityData mountUnit,
            float yaw, float height, float distance, float fieldOfViewDegrees, Action done)
        {
            yield return new WaitForEndOfFrame();
            var row = new JObject { ["name"] = name, ["frame"] = Time.frameCount };
            try
            {
                var camera = Game.GetCamera();
                var transform = camera.transform;
                var position = transform.position; var rotation = transform.rotation; var fieldOfView = camera.fieldOfView;
                var focus = focusUnit.View.transform;
                var look = focus.position + Vector3.up * 1.2f;
                var direction = Quaternion.Euler(0f, yaw, 0f) * focus.forward;
                byte[] bytes;
                try
                {
                    transform.position = focus.position + direction * distance + Vector3.up * height;
                    transform.rotation = Quaternion.LookRotation(look - transform.position);
                    camera.fieldOfView = fieldOfViewDegrees;
                    bytes = Screenshot.CapturePNG(camera);
                }
                finally { transform.position = position; transform.rotation = rotation; camera.fieldOfView = fieldOfView; }
                // The pelvis in the same frame as its pixels, after this frame's pose.
                if (riderUnit?.View?.CharacterAvatar != null && mountUnit?.View != null)
                {
                    var root = mountUnit.View.transform; var avatar = riderUnit.View.CharacterAvatar.transform;
                    var pelvis = Bone(avatar, "Pelvis"); var left = Bone(avatar, "L_foot"); var right = Bone(avatar, "R_foot");
                    if (pelvis != null && left != null && right != null)
                        row["seat"] = new JObject { ["pelvisMountLocal"] = Vector(root.InverseTransformPoint(pelvis.position)),
                            ["leftFootMountLocal"] = Vector(root.InverseTransformPoint(left.position)), ["rightFootMountLocal"] = Vector(root.InverseTransformPoint(right.position)) };
                }
                var folder = Path.Combine(evidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
                var file = "sprint5-" + name + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            shots.Add(row);
            done();
        }
    }

    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint5SmallRealTime = "sprint5-small-rt", Sprint5DogRealTime = "sprint5-dog-rt";
        internal static bool IsSprint5SmallScenario(string scenario) => scenario == Sprint5SmallRealTime || scenario == Sprint5DogRealTime;
        private bool IsSprint5Small => IsSprint5SmallScenario(request.Scenario);
        private bool IsSprint5Dog => request.Scenario == Sprint5DogRealTime;
        internal const int Sprint5SchemaVersion = 49;
        internal const double Sprint5ScenarioDeadlineSeconds = 300.0d, Sprint5HostDeadlineSeconds = 460.0d;
        internal static readonly string[] Sprint5HorseCases = { "S5-inventory", "S5-medium-control", "S5-acquire", "S5-rigs", "S5-actions" };
        internal static readonly string[] Sprint5DogCases = { "S5-inventory", "S5-medium-control", "S5-acquire", "S5-medium-refused", "S5-rigs", "S5-actions", "S5-size-change", "S5-growth", "S5-stock-dog", "S5-replacement" };
        private string[] Sprint5Cases => IsSprint5Dog ? Sprint5DogCases : Sprint5HorseCases;
        private const int Sprint5SettleFrames = 45;
        private const float Sprint5MoveMetres = 6f, Sprint5ShotSeconds = 0.5f, Sprint5AttackSeconds = 12f;
        private const string Sprint5CureLightWounds = "5590652e1c2225c4ca30c4a699ab3649";

        private sealed class Sprint5Rig
        {
            internal string Name, RaceGuid, Skeleton, HorseProfile, DogProfile; internal Gender Gender;
        }
        private static readonly Sprint5Rig[] Sprint5Rigs =
        {
            new Sprint5Rig { Name = "male-halfling", RaceGuid = "b0c3ef2729c498f47970bb50fa1acd30", Gender = Gender.Male, Skeleton = "MaleHalfling",
                HorseProfile = MountedRiderProfileCatalog.OriginalSmallHorseId, DogProfile = "small-halfling-male-ridingdog-v1" },
            new Sprint5Rig { Name = "female-halfling", RaceGuid = "b0c3ef2729c498f47970bb50fa1acd30", Gender = Gender.Female, Skeleton = "FemaleHalfling",
                HorseProfile = "small-halfling-female-horse-v1", DogProfile = "small-halfling-female-ridingdog-v1" },
            new Sprint5Rig { Name = "male-gnome", RaceGuid = "ef35a22c9a27da345a4528f0d5889157", Gender = Gender.Male, Skeleton = "MaleGnome",
                HorseProfile = "small-gnome-male-horse-v1", DogProfile = "small-gnome-male-ridingdog-v1" },
            new Sprint5Rig { Name = "female-gnome", RaceGuid = "ef35a22c9a27da345a4528f0d5889157", Gender = Gender.Female, Skeleton = "FemaleGnome",
                HorseProfile = "small-gnome-female-horse-v1", DogProfile = "small-gnome-female-ridingdog-v1" }
        };

        private int s5CaseIndex, s5Stage, s5Step, s5RigIndex, s5ShotIndex, s5Frames;
        private JObject s5Facts, s5Measure, s5RigRow;
        private JArray s5RigRows, s5Shots;
        private readonly Queue<Sprint5Shot> s5Queue = new Queue<Sprint5Shot>();
        private bool s5CaptureActive, s5Failed;
        private UnitEntityData s5Owner, s5Pet, s5Target;
        private DiagnosticCombatTargetService s5TargetService;
        private Chunk4IncomingRuleObserver s5Rules;
        private Kingmaker.Items.ItemEntity s5Armor;
        private Buff s5Buff;
        private float s5ShotAt, s5StepAt;
        private string Sprint5Case => Sprint5Cases[s5CaseIndex];
        private string Sprint5MountName => IsSprint5Dog ? "dog" : "horse";
        private BlueprintFeature Sprint5CompanionFeature => IsSprint5Dog ? horseService.RidingDogFeature : horseService.HorseFeature;

        private sealed class Sprint5Shot { internal string Name; internal UnitEntityData Focus; internal float Yaw, Height, Distance, FieldOfView; }

        private void BeginSprint5Small()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 5 rows require the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s5Shots = new JArray();
            s5Measure = new JObject {
                ["contract"] = "native-small-riders-riding-dog-v1", ["cases"] = new JArray(Sprint5Cases), ["shots"] = s5Shots,
                ["mount"] = Sprint5MountName, ["controlRider"] = rider.UniqueId, ["controlMount"] = horse.UniqueId,
                ["bodiesAreDisclosedSetup"] = true, ["policy"] = "Selections run the native level-up pipeline; each Mount and Dismount is the native ability click." };
            observations["sprint5Small"] = s5Measure;
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            CastingMeasurement["items"] = new JArray();
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureSprint5DeadlineProgress() => new JObject { ["stage"] = s5Stage, ["step"] = s5Step, ["caseIndex"] = s5CaseIndex, ["rig"] = s5RigIndex,
            ["case"] = s5CaseIndex < Sprint5Cases.Length ? Sprint5Case : null, ["owner"] = s5Owner?.UniqueId, ["facts"] = s5Facts?.DeepClone() };

        private void TickSprint5Small()
        {
            if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; return; }
            if (s5CaptureActive || s5Queue.Count != 0) { PumpSprint5Shots(); return; }
            if (s5Stage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "sprint5-control-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                s5Stage = 1; ResetLeafClock(); return;
            }
            if (s5Stage == 1)
            {
                s5Facts = new JObject { ["case"] = Sprint5Case };
                s5RigRows = new JArray(); s5Step = 0; s5RigIndex = 0; s5Frames = 0; s5Stage = 2; ResetLeafClock(); return;
            }
            switch (Sprint5Case)
            {
                case "S5-inventory": TickSprint5Inventory(); return;
                case "S5-medium-control": TickSprint5MediumControl(); return;
                case "S5-acquire": TickSprint5Acquire(); return;
                case "S5-medium-refused": TickSprint5MediumRefused(); return;
                case "S5-rigs": TickSprint5Rigs(); return;
                case "S5-actions": TickSprint5Actions(); return;
                case "S5-size-change": TickSprint5SizeChange(); return;
                case "S5-growth": TickSprint5Growth(); return;
                case "S5-stock-dog": TickSprint5StockDog(); return;
                case "S5-replacement": TickSprint5Replacement(); return;
                default: throw new InvalidOperationException("Unknown Sprint 5 case " + Sprint5Case);
            }
        }

        // A row that cannot leave its actors usable ends the run after its own facts are kept.
        private void FinishSprint5Row(bool passed, string detail, bool actorsUsable = true)
        {
            AddRow(Sprint5Case, passed, detail, s5Facts);
            s5CaseIndex++; s5Stage = 1;
            if (!actorsUsable) s5Failed = true;
            if (s5Failed || s5CaseIndex == Sprint5Cases.Length) { BeginCleanup(); return; }
            ResetLeafClock();
        }

        // ---- inventory ---------------------------------------------------------------------------------------------
        private void TickSprint5Inventory()
        {
            var library = ResourcesLibrary.LibraryObject;
            var dogFeature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.DogFeatureGuid);
            var stockAddPet = dogFeature.GetComponent<AddPet>();
            var service = horseService;
            s5Facts["registration"] = new JObject { ["ridingDogFailure"] = service.CaptureSnapshot().RidingDogFailure, ["state"] = service.State.ToString() };
            if (service.RidingDogFeature == null) { FinishSprint5Row(false, "The Riding Dog did not register.", false); return; }
            s5Facts["races"] = new JArray(BlueprintRoot.Instance.Progression.CharacterRaces.Select(race => new JObject {
                ["name"] = race.name, ["guid"] = race.AssetGuid, ["size"] = race.Size.ToString(), ["raceId"] = race.RaceId.ToString(),
                ["presets"] = new JArray((race.Presets ?? new BlueprintRaceVisualPreset[0]).Select(preset => new JObject {
                    ["guid"] = preset.AssetGuid, ["name"] = preset.name, ["male"] = preset.MaleSkeleton?.name, ["female"] = preset.FemaleSkeleton?.name })) }));
            s5Facts["dogSelections"] = new JArray(library.GetAllBlueprints().OfType<BlueprintFeatureSelection>()
                .Where(selection => selection.AllFeatures != null && selection.AllFeatures.Contains(dogFeature))
                .Select(selection => new JObject { ["name"] = selection.name, ["guid"] = selection.AssetGuid, ["count"] = selection.AllFeatures.Length,
                    ["ridingDogOffered"] = selection.AllFeatures.Contains(service.RidingDogFeature), ["features"] = selection.Features?.Length ?? -1 }));
            s5Facts["stockDog"] = Sprint5UnitBlueprint(stockAddPet.Pet);
            s5Facts["stockDogUpgrade"] = Sprint5Feature(stockAddPet.UpgradeFeature);
            s5Facts["stockUpgradeLevel"] = stockAddPet.UpgradeLevel;
            s5Facts["ridingDog"] = Sprint5UnitBlueprint(service.RidingDogUnit);
            s5Facts["ridingDogUpgrade"] = Sprint5Feature(service.RidingDogUpgrade);
            s5Facts["catalog"] = new JArray(MountedRiderProfileCatalog.Small.Select(profile => profile.Body.ProfileId));
            FinishSprint5Row(true, "Installed companion and race inventory recorded.");
        }

        private static JObject Sprint5UnitBlueprint(BlueprintUnit unit) => new JObject {
            ["name"] = unit.name, ["guid"] = unit.AssetGuid, ["size"] = unit.Size.ToString(), ["speed"] = unit.Speed.Value,
            ["strength"] = unit.Strength, ["dexterity"] = unit.Dexterity, ["constitution"] = unit.Constitution, ["intelligence"] = unit.Intelligence, ["wisdom"] = unit.Wisdom, ["charisma"] = unit.Charisma,
            ["prefab"] = unit.Prefab?.AssetId, ["bab"] = unit.BaseAttackBonus, ["maxHp"] = unit.MaxHP,
            ["limbs"] = new JArray((unit.Body?.AdditionalLimbs ?? new Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon[0]).Select(limb => limb == null ? null : limb.name + "/" + limb.AssetGuid)),
            ["emptyHand"] = unit.Body?.EmptyHandWeapon?.name, ["primaryHand"] = unit.Body?.PrimaryHand?.name, ["disableHands"] = unit.Body?.DisableHands,
            ["addFacts"] = new JArray((unit.AddFacts ?? new Kingmaker.Blueprints.Facts.BlueprintUnitFact[0]).Select(fact => fact == null ? null : fact.name + "/" + fact.AssetGuid)),
            ["components"] = new JArray((unit.ComponentsArray ?? new BlueprintComponent[0]).Select(component => component == null ? null : component.GetType().Name)) };

        private static JObject Sprint5Feature(BlueprintFeature feature)
        {
            var components = new JArray();
            foreach (var component in feature?.ComponentsArray ?? new BlueprintComponent[0])
            {
                var row = new JObject { ["type"] = component?.GetType().FullName };
                if (component is AddStatBonus bonus) { row["stat"] = bonus.Stat.ToString(); row["value"] = bonus.Value; row["descriptor"] = bonus.Descriptor.ToString(); }
                if (component is ChangeUnitSize size) { row["delta"] = size.SizeDelta; }
                components.Add(row);
            }
            return new JObject { ["name"] = feature?.name, ["guid"] = feature?.AssetGuid, ["components"] = components };
        }

        // ---- neighbor control --------------------------------------------------------------------------------------
        private void TickSprint5MediumControl()
        {
            if (s5Step == 0)
            {
                s5Facts["profile"] = SupportedMountedProfiles.Resolve(rider, horse)?.Id; s5Facts["state"] = relationship.State.ToString();
                s5Facts["sizes"] = Sprint5Sizes(rider, horse);
                QueueSprint5Views("medium-mammoth-idle", horse); s5Step = 1; return;
            }
            FinishSprint5Row(s5Facts.Value<string>("profile") == MountedRiderProfileCatalog.MediumMammothId && relationship.State == RelationshipState.Mounted, "Medium/Mammoth control seat.");
        }

        private static JObject Sprint5Sizes(UnitEntityData a, UnitEntityData b) => new JObject {
            ["riderOriginal"] = a.Descriptor.OriginalSize.ToString(), ["riderEffective"] = a.Descriptor.State.Size.ToString(),
            ["mountOriginal"] = b.Descriptor.OriginalSize.ToString(), ["mountEffective"] = b.Descriptor.State.Size.ToString(),
            ["mountViewScale"] = Sprint5Vector(b.View.transform.localScale), ["mountWorldScale"] = Sprint5Vector(b.View.transform.lossyScale),
            ["riderWorldScale"] = Sprint5Vector(a.View.transform.lossyScale) };

        // ---- acquisition -------------------------------------------------------------------------------------------
        // The Working campaign predates the Riding Dog, so this is the ordinary existing-campaign choice.
        private void TickSprint5Acquire()
        {
            switch (s5Step)
            {
                case 0:
                    var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
                    s5Owner = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).FirstOrDefault(member =>
                        member != null && member != rider && !member.Descriptor.IsPet && member.Descriptor.Pet == null &&
                        member.IsInGame && member.Descriptor.State.IsConscious && member.Descriptor.Progression.GetClassLevel(ranger) == 0);
                    if (s5Owner == null) { FinishSprint5Row(false, "No party member can make the native companion choice.", false); return; }
                    Sprint5EnsureLevels(s5Owner, 4);
                    var record = new JObject(); s5Facts["acquisition"] = record;
                    record["optionsOffered"] = new JArray(ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(HorseCompanionBlueprintService.RangerSelectionGuid)
                        .Items.Select(item => item.Feature?.AssetGuid));
                    var unitGuid = IsSprint5Dog ? HorseCompanionBlueprintService.RidingDogUnitGuid : HorseCompanionBlueprintService.UnitGuid;
                    var acquired = NativeHorseFixtureAcquisition.Create(s5Owner, horseService, logger, record, Sprint5CompanionFeature, unitGuid);
                    record["acquired"] = acquired; record["pet"] = s5Owner.Descriptor.Pet?.UniqueId;
                    record["petBlueprint"] = s5Owner.Descriptor.Pet?.Blueprint?.AssetGuid; record["owner"] = s5Owner.UniqueId;
                    record["rank"] = s5Owner.Descriptor.GetFact(horseService.LevelRank)?.GetRank() ?? 0;
                    if (!acquired) { FinishSprint5Row(false, "The native level-up did not select the companion.", false); return; }
                    s5Step = 1; ResetLeafClock(); return;
                case 1:
                    s5Pet = s5Owner.Descriptor.Pet;
                    if (s5Pet == null || !s5Pet.IsInState || s5Pet.View?.AgentASP == null) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["pet"] = Sprint5Unit(s5Pet); s5Facts["rig"] = Sprint5MountRig(s5Pet);
                    QueueSprint5Views(Sprint5MountName + "-unmounted", s5Pet); s5Step = 2; return;
                default:
                    var wanted = IsSprint5Dog ? Size.Medium : Size.Large;
                    FinishSprint5Row(s5Pet.Descriptor.State.Size == wanted, "Companion chosen through the native selection; effective size " + s5Pet.Descriptor.State.Size + ".", s5Pet.Descriptor.State.Size == wanted);
                    return;
            }
        }

        // Disclosed setup: enough native experience for ordinary levels. No level, feature or pet is granted here.
        private void Sprint5EnsureLevels(UnitEntityData owner, int levels)
        {
            var progression = owner.Descriptor.Progression;
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + levels);
            if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
            s5Facts["ownerXpSetup"] = new JObject { ["owner"] = owner.UniqueId, ["experience"] = progression.Experience, ["needed"] = needed };
        }

        private static JObject Sprint5Unit(UnitEntityData unit) => new JObject {
            ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint.AssetGuid, ["master"] = unit.Descriptor.Master.Value?.UniqueId,
            ["originalSize"] = unit.Descriptor.OriginalSize.ToString(), ["effectiveSize"] = unit.Descriptor.State.Size.ToString(),
            ["level"] = unit.Descriptor.Progression.CharacterLevel, ["hp"] = unit.MaxHP, ["ac"] = unit.Stats.AC.ModifiedValue,
            ["strength"] = unit.Stats.Strength.ModifiedValue, ["dexterity"] = unit.Stats.Dexterity.ModifiedValue, ["constitution"] = unit.Stats.Constitution.ModifiedValue,
            ["bab"] = unit.Stats.BaseAttackBonus.ModifiedValue, ["speed"] = unit.Stats.Speed.ModifiedValue, ["experience"] = unit.Descriptor.Progression.Experience,
            ["handWeapon"] = unit.Body.PrimaryHand?.MaybeWeapon?.Blueprint?.name, ["bite"] = unit.Body.PrimaryHand?.MaybeWeapon?.Blueprint?.BaseDamage.ToString(),
            ["limbs"] = new JArray(unit.Body.AdditionalLimbs.Select(slot => slot.MaybeItem?.Blueprint?.name)),
            ["facts"] = new JArray(unit.Descriptor.Progression.Features.Enumerable.Select(fact => fact.Blueprint.name).Take(40)),
            ["viewScale"] = unit.View == null ? null : Sprint5Vector(unit.View.transform.localScale) };

        private void TickSprint5MediumRefused()
        {
            if (s5Step == 0)
            {
                s5Facts["rigFacts"] = Sprint5RiderRig(s5Owner);
                s5Facts["sizes"] = Sprint5Sizes(s5Owner, s5Pet);
                s5Facts["refusal"] = SupportedMountedProfiles.DescribeRefusal(s5Owner, s5Pet);
                s5Facts["resolved"] = SupportedMountedProfiles.Resolve(s5Owner, s5Pet)?.Id;
                SelectionManager.Instance.SelectUnit(s5Owner.View, true, true, false);
                nativeControls.Update();
                s5Facts["mountFact"] = s5Owner.Descriptor.Abilities.GetAbility(nativeControls.MountAbility) != null;
                s5Facts["clicked"] = Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet);
                Game.Instance.SelectedAbilityHandler?.SetAbility(null);
                s5Frames = 0; s5Step = 1; return;
            }
            if (++s5Frames < Sprint5SettleFrames) return;
            s5Facts["stateAfter"] = Sprint5Pair()?.State.ToString() ?? "none";
            FinishSprint5Row(s5Facts.Value<string>("resolved") == null && Sprint5Pair()?.State != RelationshipState.Mounted, "A Medium rider is refused on a Medium dog.");
        }

        private GameMountedRelationshipService Sprint5Pair() => relationship.World.Registry.FindActive(s5Owner)?.Boundary;

        // ---- rigs --------------------------------------------------------------------------------------------------
        private void TickSprint5Rigs()
        {
            if (s5RigIndex == Sprint5Rigs.Length)
            {
                s5Facts["rigs"] = s5RigRows; s5Facts["mountRig"] = Sprint5MountRig(s5Pet);
                var allMounted = s5RigRows.All(row => row.Value<bool>("mounted"));
                FinishSprint5Row(allMounted, "Declared rigs mounted, moved and dismounted.", allMounted);
                return;
            }
            var rig = Sprint5Rigs[s5RigIndex];
            var name = Sprint5MountName + "-" + rig.Name;
            switch (s5Step)
            {
                case 0:
                    if (s5Pet == null || !s5Pet.IsInState || s5Pet.View?.AgentASP == null || Sprint5Pair()?.State == RelationshipState.Mounted) return;
                    Sprint5ApplyBody(s5Owner, rig);
                    s5RigRow = new JObject { ["rig"] = rig.Name, ["owner"] = s5Owner.UniqueId, ["pet"] = s5Pet.UniqueId, ["mounted"] = false };
                    s5RigRows.Add(s5RigRow); s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
                case 1:
                    if (!Sprint5BodyReady()) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5RigRow["rigFacts"] = Sprint5RiderRig(s5Owner);
                    s5RigRow["refusal"] = SupportedMountedProfiles.DescribeRefusal(s5Owner, s5Pet);
                    s5RigRow["expectedProfile"] = IsSprint5Dog ? rig.DogProfile : rig.HorseProfile;
                    s5RigRow["resolvedProfile"] = SupportedMountedProfiles.Resolve(s5Owner, s5Pet)?.Id;
                    s5Step = s5RigRow.Value<string>("resolvedProfile") == null ? 9 : 2; ResetLeafClock(); return;
                case 2:
                    if (!Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet)) return;
                    s5Frames = 0; s5Step = 3; ResetLeafClock(); return;
                case 3:
                    if (Sprint5Pair()?.State != RelationshipState.Mounted) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5RigRow["mounted"] = true; s5RigRow["sizes"] = Sprint5Sizes(s5Owner, s5Pet);
                    QueueSprint5Views(name + "-idle", s5Pet);
                    s5Step = 4; ResetLeafClock(); return;
                case 4:
                    SelectionManager.Instance.SelectUnit(s5Owner.View, true, true, false);
                    var point = FindWalkablePoint(s5Pet.Position, Sprint5MoveMetres, 1.5f);
                    ClickGroundHandler.MoveSelectedUnitsToPoint(point, false);
                    s5RigRow["moveGoal"] = CapturePosition(point); s5ShotIndex = 0; s5ShotAt = Time.time + Sprint5ShotSeconds; s5Step = 5; ResetLeafClock(); return;
                case 5:
                    if (s5ShotIndex < 2 && Time.time >= s5ShotAt)
                    {
                        s5ShotIndex++; s5ShotAt = Time.time + Sprint5ShotSeconds;
                        QueueSprint5Shot(name + "-move-" + s5ShotIndex, s5Pet, 90f, 1.3f, 9f, 24f);
                        return;
                    }
                    if (s5ShotIndex < 2 || s5Pet.View?.AgentASP?.IsReallyMoving == true || !s5Pet.Commands.Empty) return;
                    s5Step = 6; ResetLeafClock(); return;
                case 6:
                    if (!Sprint5Click(s5Owner, nativeControls.DismountAbility, s5Owner)) return;
                    s5Step = 7; ResetLeafClock(); return;
                case 7:
                    if (Sprint5Pair()?.State == RelationshipState.Mounted || !s5Owner.Commands.Empty) return;
                    s5RigRow["afterDismount"] = Sprint5Sizes(s5Owner, s5Pet); s5Step = 9; s5Frames = 0; return;
                default:
                    s5RigIndex++; s5Step = 0; s5Frames = 0; ResetLeafClock(); return;
            }
        }

        private bool Sprint5BodyReady() => Sprint5Fixture.BodyReady(s5Owner);

        // ---- actions on one rig: gear, cast, turn, attack, dismount, remount ----------------------------------------
        private void TickSprint5Actions()
        {
            var state = Sprint5Pair()?.State;
            var rig = Sprint5Rigs[0];
            var name = Sprint5MountName + "-action-";
            switch (s5Step)
            {
                case 0: Sprint5ApplyBody(s5Owner, rig); s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
                case 1:
                    if (!Sprint5BodyReady() || ++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["unmountedRig"] = Sprint5RiderRig(s5Owner);
                    s5Facts["profileBefore"] = SupportedMountedProfiles.Resolve(s5Owner, s5Pet)?.Id;
                    if (s5Facts.Value<string>("profileBefore") == null) { FinishSprint5Row(false, "The rig has no profile on this mount: " + SupportedMountedProfiles.DescribeRefusal(s5Owner, s5Pet), false); return; }
                    s5Step = 2; return;
                case 2: if (Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet)) { s5Frames = 0; s5Step = 3; ResetLeafClock(); } return;
                case 3:
                    if (state != RelationshipState.Mounted || ++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["remounted"] = true;
                    s5Armor = s5Owner.Body.Armor.MaybeItem;
                    s5Facts["armor"] = s5Armor?.Blueprint?.AssetGuid;
                    if (s5Armor != null) s5Owner.Body.Armor.RemoveItem();
                    s5Frames = 0; s5Step = 4; ResetLeafClock(); return;
                case 4:
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["gearRemoved"] = Sprint5PairFacts(state);
                    QueueSprint5Shot(name + "gear-removed", s5Pet, 90f, 1.3f, 9f, 24f);
                    if (s5Armor != null) s5Owner.Body.Armor.InsertItem(s5Armor);
                    s5Frames = 0; s5Step = 5; return;
                case 5:
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["gearRestored"] = Sprint5PairFacts(state);
                    QueueSprint5Shot(name + "gear-restored", s5Pet, 30f, 1.5f, 9f, 24f);
                    s5Step = 6; ResetLeafClock(); return;
                case 6:
                    // Setup only for the casting pose: casting and its costs were accepted in Sprint 3.
                    var cure = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(Sprint5CureLightWounds);
                    s5Owner.Commands.Run(UnitUseAbility.CreateCastCommand(new AbilityData(cure, s5Owner.Descriptor), s5Owner));
                    s5StepAt = Time.time + 0.5f; s5Step = 7; ResetLeafClock(); return;
                case 7:
                    if (Time.time < s5StepAt) return;
                    QueueSprint5Shot(name + "cast", s5Pet, 90f, 1.3f, 9f, 24f); s5Step = 8; return;
                case 8:
                    if (!s5Owner.Commands.Empty || s5Owner.AreHandsBusyWithAnimation) return;
                    SelectionManager.Instance.SelectUnit(s5Owner.View, true, true, false);
                    ClickGroundHandler.MoveSelectedUnitsToPoint(FindWalkablePoint(s5Pet.Position - s5Pet.View.transform.forward * 5f, 1f, 2f), false);
                    s5StepAt = Time.time + 0.4f; s5Step = 9; ResetLeafClock(); return;
                case 9:
                    if (Time.time < s5StepAt) return;
                    QueueSprint5Shot(name + "turn", s5Pet, 60f, 1.4f, 9f, 24f); s5Step = 10; return;
                case 10:
                    if (s5Pet.View?.AgentASP?.IsReallyMoving == true || !s5Pet.Commands.Empty) return;
                    Sprint5SpawnTarget(); s5Rules = new Chunk4IncomingRuleObserver(s5Owner, s5Pet); s5Rules.BeginCase(Sprint5Case);
                    s5Facts["cooldownsBefore"] = Sprint5Cooldowns();
                    SelectionManager.Instance.SelectUnit(s5Owner.View, true, true, false);
                    using (var input = new NativeOrdinaryAttackInput(s5Target)) s5Facts["attackClicked"] = input.Click();
                    s5StepAt = Time.time + Sprint5AttackSeconds; s5ShotIndex = 0; s5ShotAt = Time.time + 1f; s5Step = 11; ResetLeafClock(); return;
                case 11:
                    s5TargetService.ObserveTargetLifeState(); s5TargetService.RefreshBidirectionalCombatMemoryLease();
                    if (s5ShotIndex < 3 && Time.time >= s5ShotAt)
                    {
                        s5ShotIndex++; s5ShotAt = Time.time + 0.45f;
                        QueueSprint5Shot(name + "attack-" + s5ShotIndex, s5Pet, 90f, 1.3f, 9f, 24f); ResetLeafClock(); return;
                    }
                    if (Time.time < s5StepAt && !(s5ShotIndex == 3 && Sprint5AttackCount(s5Owner) >= 1)) return;
                    s5Facts["cooldownsAfterRider"] = Sprint5Cooldowns();
                    // The mount's own attack, by the ordinary selection and click.
                    SelectionManager.Instance.SelectUnit(s5Pet.View, true, true, false);
                    using (var input = new NativeOrdinaryAttackInput(s5Target)) s5Facts["mountAttackClicked"] = input.Click();
                    s5StepAt = Time.time + Sprint5AttackSeconds; s5ShotAt = Time.time + 0.35f; s5ShotIndex = 0; s5Step = 16; ResetLeafClock(); return;
                case 16:
                    s5TargetService.ObserveTargetLifeState(); s5TargetService.RefreshBidirectionalCombatMemoryLease();
                    if (s5ShotIndex == 0 && Time.time >= s5ShotAt) { s5ShotIndex = 1; QueueSprint5Shot(name + "mount-attack", s5Pet, 90f, 1.3f, 9f, 24f); ResetLeafClock(); return; }
                    if (Time.time < s5StepAt && Sprint5AttackCount(s5Pet) < 1) return;
                    s5Facts["cooldownsDuring"] = Sprint5Cooldowns();
                    s5Facts["rules"] = s5Rules.Capture(); s5Facts["riderAttacks"] = Sprint5AttackCount(s5Owner); s5Facts["mountAttacks"] = Sprint5AttackCount(s5Pet);
                    s5Facts["duringCombat"] = Sprint5PairFacts(state);
                    s5Owner.Commands.InterruptAll(); s5Pet.Commands.InterruptAll();
                    s5Step = 12; ResetLeafClock(); return;
                case 12:
                    if (!s5TargetService.DestroyAndVerify()) return;
                    s5TargetService.Dispose(); s5TargetService = null; s5Target = null;
                    s5Rules.Dispose(); s5Rules = null; s5Step = 13; ResetLeafClock(); return;
                case 13:
                    if (Game.Instance.Player.IsInCombat || !s5Owner.Commands.Empty || !s5Pet.Commands.Empty) return;
                    if (Sprint5Click(s5Owner, nativeControls.DismountAbility, s5Owner)) { s5Step = 14; ResetLeafClock(); } return;
                case 14:
                    if (state == RelationshipState.Mounted || !s5Owner.Commands.Empty) return;
                    s5Facts["afterDismount"] = Sprint5RiderRig(s5Owner);
                    QueueSprint5Shot(name + "dismounted", s5Pet, 90f, 1.3f, 9f, 24f); s5Step = 15; return;
                default:
                    FinishSprint5Row(true, "Gear, cast, turn, attack and dismount observed on one rig.");
                    return;
            }
        }

        private JObject Sprint5PairFacts(RelationshipState? state) => new JObject {
            ["state"] = state?.ToString(), ["resolved"] = SupportedMountedProfiles.Resolve(s5Owner, s5Pet)?.Id, ["sizes"] = Sprint5Sizes(s5Owner, s5Pet) };

        private JObject Sprint5Cooldowns() => new JObject {
            ["riderStandard"] = s5Owner.CombatState.Cooldown.StandardAction, ["riderMove"] = s5Owner.CombatState.Cooldown.MoveAction,
            ["mountStandard"] = s5Pet.CombatState.Cooldown.StandardAction, ["mountMove"] = s5Pet.CombatState.Cooldown.MoveAction };

        private int Sprint5AttackCount(UnitEntityData actor) => s5Rules == null ? 0 :
            ((JArray)s5Rules.Capture()["events"]).Count(e => (string)e["kind"] == "attack-before" && (string)e["actor"] == actor.UniqueId);

        private void Sprint5SpawnTarget()
        {
            var point = FindWalkablePoint(s5Pet.Position, 3f, 1f);
            s5TargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            s5Target = s5TargetService.Spawn(s5Owner, s5Pet, point, request.RunId + "-s5", true, true);
            if (!s5TargetService.PrepareForPlayerClick(s5Target) || !s5TargetService.QueueBidirectionalCombatMemory(s5Owner, s5Target))
                throw new InvalidOperationException("Sprint 5 hostile target visibility/combat-memory lease failed.");
            s5TargetService.BeginExpectedAttackDispatch(s5Target);
        }

        // ---- size ----------------------------------------------------------------------------------------------------
        // SIZE-01: a native Enlarge on the mounted Small rider makes it Medium on a Medium mount.
        private void TickSprint5SizeChange()
        {
            var state = Sprint5Pair()?.State;
            switch (s5Step)
            {
                case 0: if (Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet)) { s5Frames = 0; s5Step = 1; ResetLeafClock(); } return;
                case 1:
                    if (state != RelationshipState.Mounted || ++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["before"] = Sprint5PairFacts(state); s5Facts["positionBefore"] = CapturePosition(s5Owner.Position);
                    s5Facts["cooldownsBefore"] = Sprint5Cooldowns();
                    s5Buff = s5Owner.Buffs.AddBuff(RuntimePersistenceScenario.FindEligibilityBuff(), s5Owner, TimeSpan.FromMinutes(10));
                    s5Facts["enlarged"] = s5Buff != null;
                    s5Frames = 0; s5Step = 2; ResetLeafClock(); return;
                case 2:
                    if (state == RelationshipState.Mounted) { if (++s5Frames > 300) { s5Facts["detached"] = false; s5Step = 3; } return; }
                    s5Facts["detached"] = true; s5Facts["after"] = Sprint5PairFacts(state); s5Facts["refusal"] = SupportedMountedProfiles.DescribeRefusal(s5Owner, s5Pet);
                    s5Facts["positionAfter"] = CapturePosition(s5Owner.Position); s5Facts["cooldownsAfter"] = Sprint5Cooldowns();
                    s5Facts["ownership"] = s5Pet.Descriptor.Master.Value?.UniqueId;
                    QueueSprint5Shot(Sprint5MountName + "-enlarged-detached", s5Pet, 90f, 1.3f, 9f, 24f);
                    s5Step = 3; return;
                case 3:
                    if (s5Buff != null) { s5Owner.Buffs.RemoveFact(s5Buff); s5Buff = null; }
                    s5Frames = 0; s5Step = 4; ResetLeafClock(); return;
                default:
                    if (s5Owner.Descriptor.State.Size != Size.Small || ++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["restored"] = Sprint5PairFacts(state);
                    var detached = s5Facts.Value<bool?>("detached") == true;
                    FinishSprint5Row(detached && s5Facts["after"].Value<string>("resolved") == null && s5Facts.Value<string>("ownership") == s5Owner.UniqueId,
                        "A native Enlarge detached the pair without losing ownership.", detached);
                    return;
            }
        }

        // DOG-01: three ordinary Ranger levels reach rank 4; the Riding Dog's own advancement applies once and its size stays Medium.
        private void TickSprint5Growth()
        {
            switch (s5Step)
            {
                case 0:
                    s5Facts["before"] = Sprint5Unit(s5Pet); s5Facts["rankBefore"] = s5Owner.Descriptor.GetFact(horseService.LevelRank)?.GetRank() ?? 0;
                    Sprint5EnsureLevels(s5Owner, 3);
                    var record = new JObject(); s5Facts["levels"] = record;
                    record["committed"] = NativeHorseFixtureAcquisition.AddRangerLevels(s5Owner, 3, logger);
                    s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
                case 1:
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5Pet = s5Owner.Descriptor.Pet;
                    s5Facts["after"] = Sprint5Unit(s5Pet); s5Facts["rankAfter"] = s5Owner.Descriptor.GetFact(horseService.LevelRank)?.GetRank() ?? 0;
                    s5Facts["upgrade"] = s5Pet.Descriptor.GetFact(horseService.RidingDogUpgrade) != null;
                    s5Step = 2; return;
                case 2: if (Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet)) { s5Frames = 0; s5Step = 3; ResetLeafClock(); } return;
                case 3:
                    if (Sprint5Pair()?.State != RelationshipState.Mounted || ++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["grownMounted"] = Sprint5PairFacts(Sprint5Pair()?.State);
                    QueueSprint5Views("dog-grown", s5Pet); s5Step = 4; return;
                case 4: if (Sprint5Click(s5Owner, nativeControls.DismountAbility, s5Owner)) { s5Step = 5; ResetLeafClock(); } return;
                default:
                    if (Sprint5Pair()?.State == RelationshipState.Mounted || !s5Owner.Commands.Empty) return;
                    FinishSprint5Row(s5Facts.Value<bool?>("upgrade") == true && s5Pet.Descriptor.State.Size == Size.Medium, "Rank-4 advancement applied; the mount stays Medium with a valid seat.");
                    return;
            }
        }

        // The ordinary Dog beside the Riding Dog: its own blueprint values and its native render.
        private void TickSprint5StockDog()
        {
            if (s5Step == 0)
            {
                var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.DogFeatureGuid);
                var stock = feature.GetComponent<AddPet>().Pet;
                s5Facts["blueprint"] = Sprint5UnitBlueprint(stock);
                var point = FindWalkablePoint(s5Pet.Position, 3f, 1f);
                s5Target = Game.Instance.EntityCreator.SpawnUnit(stock, point, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
            }
            if (s5Step == 1)
            {
                if (s5Target.View == null || ++s5Frames < Sprint5SettleFrames) return;
                s5Facts["spawned"] = Sprint5Unit(s5Target);
                s5Facts["mountable"] = SupportedMountedProfiles.IsSupported(s5Target);
                s5Facts["ridingDog"] = Sprint5Unit(s5Pet);
                QueueSprint5Views("dog-beside-stock", s5Pet); s5Step = 2; return;
            }
            s5Target.Destroy(); s5Target = null;
            FinishSprint5Row(s5Facts.Value<bool>("mountable") == false, "The ordinary Dog is not a mount and keeps its own definition.");
        }

        // Replacement: the native fact removal a respec makes. The owned dog leaves with it and no orphan or second pet remains.
        private void TickSprint5Replacement()
        {
            if (s5Step == 0)
            {
                s5Facts["petBefore"] = s5Pet.UniqueId;
                s5Owner.Descriptor.RemoveFact(horseService.RidingDogFeature);
                s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
            }
            if (++s5Frames < Sprint5SettleFrames) return;
            s5Facts["ownerPet"] = s5Owner.Descriptor.Pet?.UniqueId;
            s5Facts["petDestroyed"] = s5Pet.Destroyed;
            s5Facts["orphans"] = Game.Instance.State.Units.Count(unit => unit.Blueprint?.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid);
            FinishSprint5Row(s5Facts.Value<string>("ownerPet") == null && s5Facts.Value<int>("orphans") == 0, "Replacing the companion left no orphan.");
        }

        // ---- fixture helpers -----------------------------------------------------------------------------------------
        private bool Sprint5Click(UnitEntityData caster, BlueprintAbility blueprint, UnitEntityData target) => Sprint5Fixture.Click(nativeControls, caster, blueprint, target);
        private static void Sprint5ApplyBody(UnitEntityData unit, Sprint5Rig rig) => Sprint5Fixture.ApplyBody(unit, rig.RaceGuid, rig.Gender);
        private static Transform Sprint5Bone(Transform root, string name) => Sprint5Fixture.Bone(root, name);

        private static JObject Sprint5RiderRig(UnitEntityData unit)
        {
            var avatar = unit.View.CharacterAvatar; var root = avatar.transform;
            var pelvis = Sprint5Bone(root, "Pelvis"); var left = Sprint5Bone(root, "L_Up_leg"); var right = Sprint5Bone(root, "R_Up_leg");
            var knee = Sprint5Bone(root, "L_leg"); var foot = Sprint5Bone(root, "L_foot");
            return new JObject { ["skeleton"] = avatar.Skeleton?.name, ["race"] = unit.Descriptor.Progression.Race?.AssetGuid, ["gender"] = unit.Descriptor.Doll?.Gender.ToString(),
                ["originalSize"] = unit.Descriptor.OriginalSize.ToString(), ["effectiveSize"] = unit.Descriptor.State.Size.ToString(), ["mirror"] = avatar.Mirror,
                ["avatarScale"] = Sprint5Vector(root.localScale), ["worldScale"] = Sprint5Vector(unit.View.transform.lossyScale), ["pelvisHeight"] = pelvis == null ? (float?)null : root.InverseTransformPoint(pelvis.position).y,
                ["hipWidth"] = left == null || right == null ? (float?)null : Vector3.Distance(left.position, right.position),
                ["thigh"] = left == null || knee == null ? (float?)null : Vector3.Distance(left.position, knee.position),
                ["shin"] = knee == null || foot == null ? (float?)null : Vector3.Distance(knee.position, foot.position) };
        }

        // The mount's bones and, when the mesh can be baked, its back surface in slices along its length.
        private static JObject Sprint5MountRig(UnitEntityData mount)
        {
            var root = mount.View.transform;
            var bones = new JArray(root.GetComponentsInChildren<Transform>(true).Take(160).Select(node => new JObject {
                ["name"] = node.name, ["parent"] = node.parent?.name, ["mountLocal"] = Sprint5Vector(root.InverseTransformPoint(node.position)) }));
            var surfaces = new JArray();
            foreach (var renderer in mount.View.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(value => value.sharedMesh == null ? 0 : value.sharedMesh.vertexCount).Take(2))
            {
                var row = new JObject { ["name"] = renderer.name, ["readable"] = renderer.sharedMesh?.isReadable,
                    ["boundsCenter"] = Sprint5Vector(root.InverseTransformPoint(renderer.bounds.center)), ["boundsSize"] = Sprint5Vector(renderer.bounds.size), ["vertices"] = renderer.sharedMesh?.vertexCount };
                try
                {
                    var mesh = new Mesh();
                    renderer.BakeMesh(mesh);
                    var toWorld = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
                    var points = mesh.vertices.Select(vertex => root.InverseTransformPoint(toWorld.MultiplyPoint3x4(vertex))).ToArray();
                    UnityEngine.Object.Destroy(mesh);
                    row["bakedVertices"] = points.Length;
                    if (points.Length != 0)
                    {
                        var minZ = points.Min(p => p.z); var maxZ = points.Max(p => p.z); const int slices = 24;
                        row["slices"] = new JArray(Enumerable.Range(0, slices).Select(index =>
                        {
                            var from = minZ + (maxZ - minZ) * index / slices; var to = minZ + (maxZ - minZ) * (index + 1) / slices;
                            var inSlice = points.Where(p => p.z >= from && p.z < to).ToArray();
                            return inSlice.Length == 0 ? new JObject { ["z"] = from } : new JObject { ["z"] = from, ["topY"] = inSlice.Max(p => p.y), ["bottomY"] = inSlice.Min(p => p.y),
                                ["minX"] = inSlice.Min(p => p.x), ["maxX"] = inSlice.Max(p => p.x), ["count"] = inSlice.Length };
                        }));
                    }
                }
                catch (Exception exception) { row["bakeError"] = exception.GetType().Name + ": " + exception.Message; }
                surfaces.Add(row);
            }
            return new JObject { ["bones"] = bones, ["surfaces"] = surfaces, ["worldScale"] = Sprint5Vector(root.lossyScale), ["localScale"] = Sprint5Vector(root.localScale) };
        }

        private static JObject Sprint5Vector(Vector3 value) => Sprint5Fixture.Vector(value);

        // ---- captures ----------------------------------------------------------------------------------------------
        private void QueueSprint5Views(string name, UnitEntityData focus)
        {
            QueueSprint5Shot(name + "-side", focus, 90f, 1.3f, 9f, 24f);
            QueueSprint5Shot(name + "-front", focus, 30f, 1.5f, 9f, 24f);
            QueueSprint5Shot(name + "-rear", focus, 150f, 1.9f, 9f, 24f);
            QueueSprint5Shot(name + "-other", focus, 270f, 1.3f, 9f, 24f);
        }

        private void QueueSprint5Shot(string name, UnitEntityData focus, float yaw, float height, float distance, float fieldOfView) =>
            s5Queue.Enqueue(new Sprint5Shot { Name = name, Focus = focus, Yaw = yaw, Height = height,
                Distance = IsSprint5Dog && focus != horse ? distance * 0.7f : distance, FieldOfView = fieldOfView });

        private void PumpSprint5Shots()
        {
            if (s5CaptureActive || s5Queue.Count == 0) return;
            var runner = UnityModManager.UI.Instance;
            if (runner == null) throw new InvalidOperationException("Sprint 5 capture has no end-of-frame runner.");
            s5CaptureActive = true;
            var shot = s5Queue.Dequeue();
            runner.StartCoroutine(Sprint5Fixture.Capture(request.EvidenceRoot, s5Shots, shot.Name, shot.Focus, s5Owner, s5Pet, shot.Yaw, shot.Height, shot.Distance, shot.FieldOfView, () =>
            {
                s5CaptureActive = false;
                ResetLeafClock();
            }));
        }

        private bool DrainSprint5Fixture()
        {
            if (s5CaptureActive) return false;
            s5Queue.Clear();
            if (s5Buff != null && s5Owner != null) { s5Owner.Buffs.RemoveFact(s5Buff); s5Buff = null; }
            if (s5TargetService != null)
            {
                if (!s5TargetService.DestroyAndVerify()) return false;
                s5TargetService.Dispose(); s5TargetService = null;
            }
            else if (s5Target != null) { s5Target.Destroy(); s5Target = null; }
            if (s5Rules != null) { s5Rules.Dispose(); s5Rules = null; }
            if (s5Owner != null && Sprint5Pair()?.State == RelationshipState.Mounted)
            {
                if (s5Owner.Commands.Empty) Sprint5Click(s5Owner, nativeControls.DismountAbility, s5Owner);
                return false;
            }
            return DrainCastingFixture();
        }
    }
}
