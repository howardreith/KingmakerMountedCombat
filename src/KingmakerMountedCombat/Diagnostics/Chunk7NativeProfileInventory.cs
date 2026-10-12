using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Visual.CharacterSystem;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Bounded metadata observation only. This does not create a fixture, select a
    // profile, load an equipment entity, or alter native race, size or progression.
    internal static class Chunk7NativeProfileInventory
    {
        private static readonly string[] PoseBones =
            { "Pelvis", "L_Up_leg", "L_leg", "L_foot", "R_Up_leg", "R_leg", "R_foot" };

        internal static JObject Capture(IEnumerable<UnitEntityData> actors)
        {
            var result = new JObject
            {
                ["schemaVersion"] = 1,
                ["captureKind"] = "installed-native-small-body-catalogue",
                ["captureStatus"] = "FAIL",
                ["gameplayAssemblyMvid"] = typeof(BlueprintRoot).Module.ModuleVersionId.ToString("D"),
                ["coordinateConvention"] = "Native Unity units; local values are relative to the recorded parent; world values are unconverted.",
                ["profileAdmissionChanged"] = false,
                ["nativeFixtureCreated"] = false,
                ["poseQualified"] = false,
                ["ordinaryAcquisitionProven"] = false,
                ["requiredPoseBones"] = new JArray(PoseBones)
            };
            try
            {
                var nativeRoot = BlueprintRoot.Instance;
                if (nativeRoot?.Progression?.CharacterRaces == null || nativeRoot.CharGen == null)
                    throw new InvalidOperationException("The installed native character-creation catalogue is unavailable.");
                var races = nativeRoot.Progression.CharacterRaces;
                if (races.Length > 64) throw new InvalidOperationException("Native race catalogue exceeds the bounded inventory limit.");
                var selectedActors = (actors ?? Enumerable.Empty<UnitEntityData>()).ToArray();
                if (selectedActors.Length > 24 || selectedActors.Any(x => x == null))
                    throw new InvalidOperationException("The native party inventory is null or exceeds 24 exact actors.");
                result["nativeRaceCount"] = races.Length;
                result["smallRaces"] = new JArray(races.Select((race, index) => new { race, index })
                    .Where(x => x.race != null && x.race.Size == Size.Small)
                    .Select(x => CaptureRace(x.race, x.index)));
                result["baseMaleDoll"] = CaptureCharacter(nativeRoot.CharGen.MaleDoll);
                result["baseFemaleDoll"] = CaptureCharacter(nativeRoot.CharGen.FemaleDoll);
                result["party"] = new JArray(selectedActors.Select(CaptureActor));
                result["captureStatus"] = "PASS";
            }
            catch (Exception exception)
            {
                result["error"] = exception.GetType().FullName + ": " + exception.Message;
            }
            return result;
        }

        private static JObject CaptureRace(BlueprintRace race, int index)
        {
            var presets = race.Presets ?? new Kingmaker.Blueprints.CharGen.BlueprintRaceVisualPreset[0];
            if (presets.Length > 32) throw new InvalidOperationException("Native Small race presets exceed the bounded inventory limit.");
            return new JObject
            {
                ["rootIndex"] = index, ["raceGuid"] = race.AssetGuid, ["raceName"] = race.name,
                ["raceId"] = race.RaceId.ToString(), ["size"] = race.Size.ToString(),
                ["presets"] = new JArray(presets.Select((preset, presetIndex) => preset == null ?
                    new JObject { ["presetIndex"] = presetIndex, ["present"] = false } :
                    new JObject
                    {
                        ["presetIndex"] = presetIndex, ["present"] = true,
                        ["presetGuid"] = preset.AssetGuid, ["presetName"] = preset.name,
                        ["raceId"] = preset.RaceId.ToString(), ["skinReferencePresent"] = preset.Skin != null,
                        ["maleSkeleton"] = CaptureSkeleton(preset.MaleSkeleton),
                        ["femaleSkeleton"] = CaptureSkeleton(preset.FemaleSkeleton)
                    }))
            };
        }

        private static JObject CaptureActor(UnitEntityData actor)
        {
            var descriptor = actor.Descriptor;
            var doll = descriptor.Doll;
            var level = descriptor.Progression.CharacterLevel;
            var character = actor.View == null ? null : actor.View.GetComponent<Character>();
            var equipment = doll?.EquipmentEntityIds?.ToArray() ?? new string[0];
            if (equipment.Length > 128) throw new InvalidOperationException("Native doll equipment IDs exceed the bounded inventory limit.");
            return new JObject
            {
                ["id"] = actor.UniqueId, ["blueprintGuid"] = actor.Blueprint?.AssetGuid,
                ["raceGuid"] = descriptor.Progression.Race?.AssetGuid,
                ["raceId"] = descriptor.Progression.Race?.RaceId.ToString(),
                ["originalSize"] = descriptor.OriginalSize.ToString(), ["effectiveSize"] = descriptor.State.Size.ToString(),
                ["polymorphed"] = actor.GetActivePolymorph() != null,
                ["petId"] = descriptor.Pet?.UniqueId, ["masterId"] = descriptor.Master.Value?.UniqueId,
                ["dollPresent"] = doll != null, ["dollGender"] = doll?.Gender.ToString(),
                ["dollPresetGuid"] = doll?.RacePreset?.AssetGuid, ["dollPresetName"] = doll?.RacePreset?.name,
                ["dollRaceId"] = doll?.RacePreset?.RaceId.ToString(), ["dollLeftHanded"] = doll?.LeftHanded,
                ["dollEquipmentEntityIds"] = new JArray(equipment),
                ["viewName"] = actor.View?.name, ["character"] = CaptureCharacter(character),
                ["nativeLevelUpReadiness"] = new JObject
                {
                    ["characterLevel"] = level, ["experience"] = descriptor.Progression.Experience,
                    ["nextLevelExperience"] = level < 20 ? (JToken)new JValue(Game.Instance.BlueprintRoot.Progression.XPTable.GetBonus(level + 1)) : JValue.CreateNull(),
                    ["canLevelUp"] = LevelUpController.CanLevelUp(descriptor),
                    ["isDead"] = descriptor.State.IsDead, ["partyInCombat"] = Game.Instance.Player.IsInCombat,
                    ["controllerCreated"] = false, ["completeChoicesObserved"] = false, ["ordinaryAcquisitionProven"] = false
                }
            };
        }

        private static JObject CaptureCharacter(Character character)
        {
            if (character == null) return new JObject { ["present"] = false };
            return new JObject
            {
                ["present"] = true, ["name"] = character.name,
                ["instanceId"] = character.GetInstanceID(), ["mirrored"] = character.Mirror,
                ["root"] = CaptureTransform(character.transform, character.transform),
                ["skeleton"] = CaptureSkeleton(character.Skeleton),
                ["poseTransforms"] = CapturePoseTransforms(character.transform)
            };
        }

        private static JObject CaptureSkeleton(Skeleton skeleton)
        {
            if (skeleton == null) return new JObject { ["present"] = false };
            var bones = skeleton.Bones ?? new List<Skeleton.Bone>();
            if (bones.Count > 2048) throw new InvalidOperationException("Native skeleton exceeds the bounded bone inventory limit.");
            return new JObject
            {
                ["present"] = true, ["name"] = skeleton.name, ["instanceId"] = skeleton.GetInstanceID(),
                ["declaredBoneCount"] = bones.Count,
                ["raceHierarchyPresent"] = skeleton.RaceBoneHierarchyObject != null,
                ["ragdollOverridePresent"] = skeleton.RagdollSkeletonOverride != null,
                ["poseBones"] = new JArray(PoseBones.Select(name => new JObject
                {
                    ["name"] = name,
                    ["matches"] = new JArray(bones.Where(bone => bone != null && string.Equals(bone.Name, name, StringComparison.Ordinal))
                        .Select(bone => new JObject { ["scale"] = Vector(bone.Scale), ["offset"] = Vector(bone.Offset), ["applyOffset"] = bone.ApplyOffset }))
                })),
                ["raceHierarchyTransforms"] = skeleton.RaceBoneHierarchyObject == null ? new JArray() :
                    CapturePoseTransforms(skeleton.RaceBoneHierarchyObject.transform)
            };
        }

        private static JArray CapturePoseTransforms(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            if (transforms.Length > 4096) throw new InvalidOperationException("Native view exceeds the bounded transform inventory limit.");
            return new JArray(PoseBones.Select(name => new JObject
            {
                ["name"] = name,
                ["matches"] = new JArray(transforms.Where(item => string.Equals(item.name, name, StringComparison.Ordinal))
                    .Select(item => CaptureTransform(item, root)))
            }));
        }

        private static JObject CaptureTransform(Transform transform, Transform root)
        {
            return new JObject
            {
                ["name"] = transform.name, ["instanceId"] = transform.GetInstanceID(),
                ["parentName"] = transform.parent?.name, ["parentInstanceId"] = transform.parent == null ? (int?)null : transform.parent.GetInstanceID(),
                ["rootName"] = root.name, ["rootInstanceId"] = root.GetInstanceID(),
                ["localPosition"] = Vector(transform.localPosition), ["localEulerDegrees"] = Vector(transform.localEulerAngles),
                ["localScale"] = Vector(transform.localScale), ["worldPosition"] = Vector(transform.position),
                ["worldScale"] = Vector(transform.lossyScale), ["positionInObservedRoot"] = Vector(root.InverseTransformPoint(transform.position))
            };
        }

        private static JObject Vector(Vector3 value)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z))
                throw new InvalidOperationException("Native rig metadata contains a non-finite vector.");
            return new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        }
    }
}
