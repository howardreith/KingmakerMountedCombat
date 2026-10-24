using System;
using System.Linq;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Private staged adapter. It observes currently loaded native objects only;
    // no asset loading, fields, race, size, equipment or transforms are changed.
    internal static class NativeSmallRiderBody
    {
        internal static SmallRiderBodyObservation Capture(UnitEntityData rider, UnitEntityData mount)
        {
            if (rider?.Descriptor == null || mount?.Descriptor == null) return null;
            var descriptor = rider.Descriptor;
            var doll = descriptor.Doll;
            var race = descriptor.Progression.Race;
            var preset = doll?.RacePreset;
            var avatar = rider.View?.CharacterAvatar;
            var declaredSkeleton = doll == null || preset == null ? null :
                doll.Gender == Kingmaker.Blueprints.Gender.Male ? preset.MaleSkeleton : preset.FemaleSkeleton;
            var nativeRaces = BlueprintRoot.Instance?.Progression?.CharacterRaces;
            var scale = avatar == null ? new UnityEngine.Vector3(float.NaN, float.NaN, float.NaN) : avatar.transform.localScale;
            var currentEquipment = descriptor.Body?.CurrentEquipmentSlots.Take(13).ToArray();
            var visual = avatar?.EquipmentEntities?.Take(129).ToArray();
            return new SmallRiderBodyObservation
            {
                OriginalSizeOrdinal = (int)descriptor.OriginalSize,
                EffectiveSizeOrdinal = (int)descriptor.State.Size,
                RiderPolymorphed = rider.GetActivePolymorph() != null || descriptor.Body?.IsPolymorphed != false,
                MountPolymorphed = mount.GetActivePolymorph() != null || mount.Descriptor.Body?.IsPolymorphed != false,
                MountBlueprintGuid = mount.Blueprint?.AssetGuid, MountSizeOrdinal = (int)mount.Descriptor.State.Size,
                RaceGuid = race?.AssetGuid, PresetGuid = preset?.AssetGuid, Gender = doll?.Gender.ToString(),
                SkeletonName = avatar?.Skeleton?.name, LeftHanded = doll?.LeftHanded ?? false, AvatarMirrored = avatar?.Mirror ?? false,
                NativeRaceAndPresetIdentityExact = race != null && preset != null && nativeRaces != null &&
                    nativeRaces.Count(value => ReferenceEquals(value, race)) == 1 && preset.RaceId == race.RaceId &&
                    race.Presets != null && race.Presets.Count(value => ReferenceEquals(value, preset)) == 1,
                NativeSkeletonIsExactPreset = declaredSkeleton != null && avatar != null && ReferenceEquals(avatar.Skeleton, declaredSkeleton),
                NativeRigAvailable = avatar != null && rider.View.Animator != null && avatar.Animator != null,
                AvatarLocalScale = new PoseVector3(scale.x, scale.y, scale.z),
                DollEquipmentEntityIds = doll?.EquipmentEntityIds?.Take(129).ToArray(),
                CurrentEquipmentBlueprints = currentEquipment == null || currentEquipment.Length != 12 || currentEquipment.Any(value => value == null)
                    ? null : currentEquipment.Select(value => value.MaybeItem?.Blueprint?.AssetGuid).ToArray(),
                ActiveVisualEquipmentNames = visual == null || visual.Any(value => value == null) ? null : visual.Select(value => value.name).ToArray(),
                AdditionalLimbCount = descriptor.Body?.AdditionalLimbs?.Count ?? -1
            };
        }
    }
}
