using System;
using System.Linq;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    // Only the observed body fields participate in the transient native-view
    // identity. Json.NET must not walk PoseVector3's computed Normalized getter:
    // that getter returns another PoseVector3 without a finite object depth.
    internal static class SmallRiderBodyFingerprint
    {
        internal static string Capture(SmallRiderBodyObservation body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            var scale = body.AvatarLocalScale;
            var value = new JObject {
                ["OriginalSizeOrdinal"] = body.OriginalSizeOrdinal,
                ["EffectiveSizeOrdinal"] = body.EffectiveSizeOrdinal,
                ["RiderPolymorphed"] = body.RiderPolymorphed,
                ["MountPolymorphed"] = body.MountPolymorphed,
                ["MountBlueprintGuid"] = body.MountBlueprintGuid,
                ["MountSizeOrdinal"] = body.MountSizeOrdinal,
                ["RaceGuid"] = body.RaceGuid,
                ["PresetGuid"] = body.PresetGuid,
                ["Gender"] = body.Gender,
                ["SkeletonName"] = body.SkeletonName,
                ["LeftHanded"] = body.LeftHanded,
                ["AvatarMirrored"] = body.AvatarMirrored,
                ["NativeRaceAndPresetIdentityExact"] = body.NativeRaceAndPresetIdentityExact,
                ["NativeSkeletonIsExactPreset"] = body.NativeSkeletonIsExactPreset,
                ["NativeRigAvailable"] = body.NativeRigAvailable,
                ["AvatarLocalScale"] = new JObject { ["X"] = scale.X, ["Y"] = scale.Y, ["Z"] = scale.Z },
                ["DollEquipmentEntityIds"] = ArrayOrNull(body.DollEquipmentEntityIds),
                ["CurrentEquipmentBlueprints"] = ArrayOrNull(body.CurrentEquipmentBlueprints),
                ["ActiveVisualEquipmentNames"] = ArrayOrNull(body.ActiveVisualEquipmentNames),
                ["AdditionalLimbCount"] = body.AdditionalLimbCount
            };
            return value.ToString(Formatting.None);
        }

        private static JToken ArrayOrNull(string[] values) => values == null
            ? (JToken)JValue.CreateNull()
            : new JArray(values.Select(value => value == null ? JValue.CreateNull() : new JValue(value)));
    }
}
