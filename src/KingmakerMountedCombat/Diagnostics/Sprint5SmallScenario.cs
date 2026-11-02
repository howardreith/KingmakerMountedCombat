using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
using Kingmaker.UnitLogic.Abilities.Blueprints;
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
    // neighbor control. Two other party members become Rangers through the native level-up pipeline and choose a Horse and a
    // Riding Dog from the real selection. Their Small bodies are disclosed setup (a native Doll and race, with the native view
    // rebuild a polymorph's end uses); nothing about a body is granted to the mount or to admission. The reader owns PASS/FAIL.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint5SmallRealTime = "sprint5-small-rt";
        internal static bool IsSprint5SmallScenario(string scenario) => scenario == Sprint5SmallRealTime;
        private bool IsSprint5Small => IsSprint5SmallScenario(request.Scenario);
        internal const int Sprint5SchemaVersion = 49;
        internal const double Sprint5ScenarioDeadlineSeconds = 300.0d, Sprint5HostDeadlineSeconds = 460.0d;
        internal static readonly string[] Sprint5Cases = { "S5-inventory", "S5-medium-control", "S5-horse-rigs", "S5-dog-acquire", "S5-dog-rigs" };
        private const int Sprint5SettleFrames = 45;
        private const float Sprint5MoveMetres = 6f, Sprint5ShotSeconds = 0.5f;

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
        private JObject s5Facts, s5Measure;
        private JArray s5RigRows, s5Shots;
        private JObject s5RigRow;
        private readonly Queue<Sprint5Shot> s5Queue = new Queue<Sprint5Shot>();
        private bool s5CaptureActive;
        private UnitEntityData s5Owner, s5Pet, s5HorseOwner, s5DogOwner;
        private float s5ShotAt;
        private string Sprint5Case => Sprint5Cases[s5CaseIndex];

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
                ["controlRider"] = rider.UniqueId, ["controlMount"] = horse.UniqueId,
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
                case "S5-horse-rigs": TickSprint5Rigs(true); return;
                case "S5-dog-acquire": TickSprint5DogAcquire(); return;
                case "S5-dog-rigs": TickSprint5Rigs(false); return;
                default: throw new InvalidOperationException("Unknown Sprint 5 case " + Sprint5Case);
            }
        }

        private void FinishSprint5Row(bool passed, string detail)
        {
            AddRow(Sprint5Case, passed, detail, s5Facts);
            s5CaseIndex++; s5Stage = 1;
            if (s5CaseIndex == Sprint5Cases.Length) { BeginCleanup(); return; }
            ResetLeafClock();
        }

        // ---- inventory ---------------------------------------------------------------------------------------------
        private void TickSprint5Inventory()
        {
            var library = ResourcesLibrary.LibraryObject;
            var dogFeature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.DogFeatureGuid);
            var stockAddPet = dogFeature.GetComponent<AddPet>();
            var service = horseService;
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
            FinishSprint5Row(true, "Installed native companion and race inventory recorded.");
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
        private UnitEntityData Sprint5EligibleOwner(UnitEntityData except)
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            return Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).FirstOrDefault(member =>
                member != null && member != rider && member != except && !member.Descriptor.IsPet && member.Descriptor.Pet == null &&
                member.IsInGame && member.Descriptor.State.IsConscious && member.Descriptor.Progression.GetClassLevel(ranger) == 0);
        }

        // Disclosed setup: enough native experience for four ordinary levels. No level, feature or pet is granted here.
        private void Sprint5EnsureFourLevels(UnitEntityData owner)
        {
            var progression = owner.Descriptor.Progression;
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 4);
            if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
            s5Facts["ownerXpSetup"] = new JObject { ["owner"] = owner.UniqueId, ["experience"] = progression.Experience, ["needed"] = needed };
        }

        private bool Sprint5Acquire(UnitEntityData owner, BlueprintFeature feature, string unitGuid, string name)
        {
            Sprint5EnsureFourLevels(owner);
            var record = new JObject(); s5Facts[name] = record;
            record["optionsOffered"] = new JArray(ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(HorseCompanionBlueprintService.RangerSelectionGuid)
                .Items.Select(item => item.Feature?.AssetGuid));
            var acquired = NativeHorseFixtureAcquisition.Create(owner, horseService, logger, record, feature, unitGuid);
            record["acquired"] = acquired; record["pet"] = owner.Descriptor.Pet?.UniqueId;
            record["petBlueprint"] = owner.Descriptor.Pet?.Blueprint?.AssetGuid; record["owner"] = owner.UniqueId;
            record["rank"] = owner.Descriptor.GetFact(horseService.LevelRank)?.GetRank() ?? 0;
            return acquired;
        }

        private void TickSprint5DogAcquire()
        {
            switch (s5Step)
            {
                case 0:
                    s5DogOwner = Sprint5EligibleOwner(s5HorseOwner);
                    if (s5DogOwner == null) { FinishSprint5Row(false, "No party member can make the native Riding Dog choice."); return; }
                    if (!Sprint5Acquire(s5DogOwner, horseService.RidingDogFeature, HorseCompanionBlueprintService.RidingDogUnitGuid, "acquisition"))
                    { FinishSprint5Row(false, "The native level-up did not select the Riding Dog."); return; }
                    s5Step = 1; ResetLeafClock(); return;
                case 1:
                    s5Pet = s5DogOwner.Descriptor.Pet;
                    if (s5Pet == null || !s5Pet.IsInState || s5Pet.View?.AgentASP == null) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5Facts["pet"] = Sprint5Unit(s5Pet); s5Facts["rig"] = Sprint5MountRig(s5Pet);
                    QueueSprint5Views("riding-dog-unmounted", s5Pet); s5Step = 2; return;
                default:
                    FinishSprint5Row(s5Pet.Descriptor.State.Size == Size.Medium && s5Pet.Blueprint.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid,
                        "Riding Dog chosen through the native selection; effective size " + s5Pet.Descriptor.State.Size + ".");
                    return;
            }
        }

        private static JObject Sprint5Unit(UnitEntityData unit) => new JObject {
            ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint.AssetGuid, ["master"] = unit.Descriptor.Master.Value?.UniqueId,
            ["originalSize"] = unit.Descriptor.OriginalSize.ToString(), ["effectiveSize"] = unit.Descriptor.State.Size.ToString(),
            ["level"] = unit.Descriptor.Progression.CharacterLevel, ["hp"] = unit.MaxHP, ["ac"] = unit.Stats.AC.ModifiedValue,
            ["strength"] = unit.Stats.Strength.ModifiedValue, ["dexterity"] = unit.Stats.Dexterity.ModifiedValue, ["constitution"] = unit.Stats.Constitution.ModifiedValue,
            ["bab"] = unit.Stats.BaseAttackBonus.ModifiedValue, ["speed"] = unit.Stats.Speed.ModifiedValue,
            ["limbs"] = new JArray(unit.Body.AdditionalLimbs.Select(slot => slot.MaybeItem?.Blueprint?.name)),
            ["facts"] = new JArray(unit.Descriptor.Progression.Features.Enumerable.Select(fact => fact.Blueprint.name).Take(40)),
            ["viewScale"] = unit.View == null ? null : Sprint5Vector(unit.View.transform.localScale) };

        // ---- rigs --------------------------------------------------------------------------------------------------
        private void TickSprint5Rigs(bool onHorse)
        {
            if (onHorse && s5HorseOwner == null)
            {
                s5HorseOwner = Sprint5EligibleOwner(null);
                if (s5HorseOwner == null) { FinishSprint5Row(false, "No party member can make the native Horse choice."); return; }
                if (!Sprint5Acquire(s5HorseOwner, horseService.HorseFeature, HorseCompanionBlueprintService.UnitGuid, "acquisition"))
                { FinishSprint5Row(false, "The native level-up did not select the Horse."); return; }
                ResetLeafClock(); return;
            }
            s5Owner = onHorse ? s5HorseOwner : s5DogOwner;
            s5Pet = s5Owner.Descriptor.Pet;
            if (s5RigIndex == Sprint5Rigs.Length)
            {
                s5Facts["rigs"] = s5RigRows; s5Facts["mountRig"] = Sprint5MountRig(s5Pet);
                FinishSprint5Row(s5RigRows.All(row => row.Value<bool>("mounted")), "Declared rigs mounted, moved and dismounted.");
                return;
            }
            var rig = Sprint5Rigs[s5RigIndex];
            var pair = relationship.World.Registry.FindActive(s5Owner)?.Boundary;
            switch (s5Step)
            {
                case 0:
                    if (s5Pet == null || !s5Pet.IsInState || s5Pet.View?.AgentASP == null || pair?.State == RelationshipState.Mounted) return;
                    Sprint5ApplyBody(s5Owner, rig);
                    s5RigRow = new JObject { ["rig"] = rig.Name, ["owner"] = s5Owner.UniqueId, ["pet"] = s5Pet.UniqueId, ["mounted"] = false };
                    s5RigRows.Add(s5RigRow); s5Frames = 0; s5Step = 1; ResetLeafClock(); return;
                case 1:
                    var avatar = s5Owner.View?.CharacterAvatar;
                    if (avatar == null || avatar.IsDirty || avatar.Animator == null || s5Owner.View.Animator == null) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5RigRow["rigFacts"] = Sprint5RiderRig(s5Owner);
                    s5RigRow["refusal"] = SupportedMountedProfiles.DescribeRefusal(s5Owner, s5Pet);
                    s5RigRow["expectedProfile"] = onHorse ? rig.HorseProfile : rig.DogProfile;
                    s5RigRow["resolvedProfile"] = SupportedMountedProfiles.Resolve(s5Owner, s5Pet)?.Id;
                    QueueSprint5Views("unmounted-" + (onHorse ? "horse-" : "dog-") + rig.Name, s5Pet);
                    s5Step = s5RigRow["resolvedProfile"].Type == JTokenType.Null ? 9 : 2; ResetLeafClock(); return;
                case 2:
                    if (!Sprint5Click(s5Owner, nativeControls.MountAbility, s5Pet)) return;
                    s5Frames = 0; s5Step = 3; ResetLeafClock(); return;
                case 3:
                    if (pair?.State != RelationshipState.Mounted) return;
                    if (++s5Frames < Sprint5SettleFrames) return;
                    s5RigRow["mounted"] = true; s5RigRow["sizes"] = Sprint5Sizes(s5Owner, s5Pet); s5RigRow["idleSeat"] = Sprint5Seat(s5Owner, s5Pet);
                    QueueSprint5Views((onHorse ? "horse-" : "dog-") + rig.Name + "-idle", s5Pet);
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
                        QueueSprint5Shot((onHorse ? "horse-" : "dog-") + rig.Name + "-move-" + s5ShotIndex, s5Pet, 90f, 1.3f, 9f, 24f);
                        s5RigRow["moveSeat" + s5ShotIndex] = Sprint5Seat(s5Owner, s5Pet);
                        return;
                    }
                    if (s5ShotIndex < 2 || s5Pet.View?.AgentASP?.IsReallyMoving == true || !s5Pet.Commands.Empty) return;
                    s5Step = 6; ResetLeafClock(); return;
                case 6:
                    if (!Sprint5Click(s5Owner, nativeControls.DismountAbility, s5Owner)) return;
                    s5Step = 7; ResetLeafClock(); return;
                case 7:
                    if (pair?.State == RelationshipState.Mounted || !s5Owner.Commands.Empty) return;
                    s5RigRow["afterDismount"] = Sprint5Sizes(s5Owner, s5Pet); s5Step = 9; s5Frames = 0; return;
                default:
                    s5RigIndex++; s5Step = 0; s5Frames = 0; ResetLeafClock(); return;
            }
        }

        private bool Sprint5Click(UnitEntityData caster, BlueprintAbility blueprint, UnitEntityData target)
        {
            SelectionManager.Instance.SelectUnit(caster.View, true, true, false);
            nativeControls.Update();
            var data = caster.Descriptor.Abilities.GetAbility(blueprint)?.Data;
            var handler = Game.Instance.SelectedAbilityHandler;
            var targetObject = target?.View?.gameObject;
            if (data == null || handler == null || targetObject == null) return false;
            handler.SetAbility(data);
            return handler.OnClick(targetObject, target.Position, 0, false, false);
        }

        // Disclosed setup on a disposable actor: a native Doll and race, then the view rebuild Polymorph.RestoreView uses.
        private void Sprint5ApplyBody(UnitEntityData unit, Sprint5Rig rig)
        {
            var race = BlueprintRoot.Instance.Progression.CharacterRaces.Single(value => value.AssetGuid == rig.RaceGuid);
            var doll = new DollState();
            doll.SetRace(race); doll.SetGender(rig.Gender); doll.Validate();
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

        private static Transform Sprint5Bone(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(node => node.name == name);

        private static JObject Sprint5RiderRig(UnitEntityData unit)
        {
            var avatar = unit.View.CharacterAvatar; var root = avatar.transform;
            var pelvis = Sprint5Bone(root, "Pelvis"); var left = Sprint5Bone(root, "L_Up_leg"); var right = Sprint5Bone(root, "R_Up_leg");
            var knee = Sprint5Bone(root, "L_leg"); var foot = Sprint5Bone(root, "L_foot"); var chest = Sprint5Bone(root, "Spine_2") ?? Sprint5Bone(root, "Spine");
            return new JObject { ["skeleton"] = avatar.Skeleton?.name, ["race"] = unit.Descriptor.Progression.Race?.AssetGuid, ["gender"] = unit.Descriptor.Doll?.Gender.ToString(),
                ["originalSize"] = unit.Descriptor.OriginalSize.ToString(), ["effectiveSize"] = unit.Descriptor.State.Size.ToString(), ["mirror"] = avatar.Mirror,
                ["avatarScale"] = Sprint5Vector(root.localScale), ["pelvisHeight"] = pelvis == null ? (float?)null : root.InverseTransformPoint(pelvis.position).y,
                ["hipWidth"] = left == null || right == null ? (float?)null : Vector3.Distance(left.position, right.position),
                ["thigh"] = left == null || knee == null ? (float?)null : Vector3.Distance(left.position, knee.position),
                ["shin"] = knee == null || foot == null ? (float?)null : Vector3.Distance(knee.position, foot.position) };
        }

        private static JObject Sprint5Seat(UnitEntityData riderUnit, UnitEntityData mount)
        {
            var root = mount.View.transform; var avatar = riderUnit.View.CharacterAvatar.transform;
            var pelvis = Sprint5Bone(avatar, "Pelvis"); var left = Sprint5Bone(avatar, "L_foot"); var right = Sprint5Bone(avatar, "R_foot");
            return new JObject { ["pelvisMountLocal"] = Sprint5Vector(root.InverseTransformPoint(pelvis.position)),
                ["leftFootMountLocal"] = Sprint5Vector(root.InverseTransformPoint(left.position)), ["rightFootMountLocal"] = Sprint5Vector(root.InverseTransformPoint(right.position)),
                ["riderWorldScale"] = Sprint5Vector(riderUnit.View.transform.lossyScale), ["mountWorldScale"] = Sprint5Vector(root.lossyScale), ["frame"] = Time.frameCount };
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

        private static JObject Sprint5Vector(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };

        // ---- captures ----------------------------------------------------------------------------------------------
        private void QueueSprint5Views(string name, UnitEntityData focus)
        {
            QueueSprint5Shot(name + "-side", focus, 90f, 1.3f, 9f, 24f);
            QueueSprint5Shot(name + "-front", focus, 30f, 1.5f, 9f, 24f);
            QueueSprint5Shot(name + "-rear", focus, 150f, 1.9f, 9f, 24f);
        }

        private void QueueSprint5Shot(string name, UnitEntityData focus, float yaw, float height, float distance, float fieldOfView) =>
            s5Queue.Enqueue(new Sprint5Shot { Name = name, Focus = focus, Yaw = yaw, Height = height, Distance = distance, FieldOfView = fieldOfView });

        private void PumpSprint5Shots()
        {
            if (s5CaptureActive || s5Queue.Count == 0) return;
            var runner = UnityModManager.UI.Instance;
            if (runner == null) throw new InvalidOperationException("Sprint 5 capture has no end-of-frame runner.");
            s5CaptureActive = true;
            runner.StartCoroutine(CaptureSprint5AtEndOfFrame(s5Queue.Dequeue()));
        }

        // The gameplay camera is posed only for this render inside one end-of-frame and restored before anything else runs.
        private IEnumerator CaptureSprint5AtEndOfFrame(Sprint5Shot shot)
        {
            yield return new WaitForEndOfFrame();
            var row = new JObject { ["name"] = shot.Name, ["frame"] = Time.frameCount };
            try
            {
                var camera = Game.GetCamera();
                var transform = camera.transform;
                var position = transform.position; var rotation = transform.rotation; var fieldOfView = camera.fieldOfView;
                var focus = shot.Focus.View.transform;
                var look = focus.position + Vector3.up * 1.2f;
                var direction = Quaternion.Euler(0f, shot.Yaw, 0f) * focus.forward;
                byte[] bytes;
                try
                {
                    transform.position = focus.position + direction * shot.Distance + Vector3.up * shot.Height;
                    transform.rotation = Quaternion.LookRotation(look - transform.position);
                    camera.fieldOfView = shot.FieldOfView;
                    bytes = Screenshot.CapturePNG(camera);
                }
                finally { transform.position = position; transform.rotation = rotation; camera.fieldOfView = fieldOfView; }
                var folder = Path.Combine(request.EvidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
                var file = "sprint5-" + shot.Name + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            s5Shots.Add(row);
            s5CaptureActive = false;
            ResetLeafClock();
        }

        private bool DrainSprint5Fixture()
        {
            if (s5CaptureActive) return false;
            s5Queue.Clear();
            foreach (var owner in new[] { s5HorseOwner, s5DogOwner })
            {
                if (owner == null) continue;
                var pair = relationship.World.Registry.FindActive(owner)?.Boundary;
                if (pair?.State != RelationshipState.Mounted) continue;
                if (owner.Commands.Empty) Sprint5Click(owner, nativeControls.DismountAbility, owner);
                return false;
            }
            return DrainCastingFixture();
        }
    }
}
