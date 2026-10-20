using System;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Tests
{
    public static class SmallRiderBodyFingerprintTests
    {
        public static int Run()
        {
            var body = new SmallRiderBodyObservation {
                OriginalSizeOrdinal = 3, EffectiveSizeOrdinal = 3,
                MountBlueprintGuid = DeclaredSmallRiderBody.HorseBlueprintGuid, MountSizeOrdinal = 5,
                RaceGuid = "race", PresetGuid = "preset", Gender = "Male", SkeletonName = "MaleHalfling",
                NativeRaceAndPresetIdentityExact = true, NativeSkeletonIsExactPreset = true,
                NativeRigAvailable = true, AvatarLocalScale = new PoseVector3(1f, 1f, 1f),
                DollEquipmentEntityIds = new[] { "doll" },
                CurrentEquipmentBlueprints = new[] { "sword", null },
                ActiveVisualEquipmentNames = new[] { "glove" }
            };
            var first = SmallRiderBodyFingerprint.Capture(body);
            var json = JObject.Parse(first);
            Require(typeof(SmallRiderBodyObservation).GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal)
                .SequenceEqual(json.Properties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal)),
                "Every native body field must participate in the view identity.");
            Require(json["AvatarLocalScale"]["X"].Value<float>() == 1f &&
                json["AvatarLocalScale"]["Y"].Value<float>() == 1f && json["AvatarLocalScale"]["Z"].Value<float>() == 1f &&
                ((JObject)json["AvatarLocalScale"]).Properties().Count() == 3,
                "The finite scale must have exactly X/Y/Z and no recursive computed getters.");
            Require(json["CurrentEquipmentBlueprints"][1].Type == JTokenType.Null,
                "An empty native equipment slot must keep its identity.");
            Require(SmallRiderBodyFingerprint.Capture(body) == first, "Unchanged native body changed its view fingerprint.");
            body.AvatarLocalScale = new PoseVector3(1.1f, 1f, 1f);
            Require(SmallRiderBodyFingerprint.Capture(body) != first, "Changed native scale retained an old view fingerprint.");
            body.AvatarLocalScale = new PoseVector3(1f, 1f, 1f);
            body.CurrentEquipmentBlueprints[0] = "bow";
            Require(SmallRiderBodyFingerprint.Capture(body) != first, "Changed equipment retained an old view fingerprint.");
            body.CurrentEquipmentBlueprints = null;
            Require(JObject.Parse(SmallRiderBodyFingerprint.Capture(body))["CurrentEquipmentBlueprints"].Type == JTokenType.Null,
                "An incomplete native equipment inventory must remain distinct from empty equipment.");
            var refused = false;
            try { SmallRiderBodyFingerprint.Capture(null); } catch (ArgumentNullException) { refused = true; }
            Require(refused, "A missing native body must not be fingerprinted as a valid rig.");
            return 8;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
