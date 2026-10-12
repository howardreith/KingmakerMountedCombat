using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Observation only: no transform writes, asset loads, guessed Small bone
    // names, numeric pose overrides or visual-acceptance threshold.
    internal static class Chunk7SmallPoseObservation
    {
        internal static JObject Capture(UnitEntityData rider, UnitEntityData mount,
            DeclaredSmallMountedProfile declaration, KingmakerMountedPairRuntime runtime)
        {
            if (rider?.View?.CharacterAvatar == null || mount?.View == null || declaration == null)
                throw new InvalidOperationException("Exact live Small rider/Horse rig is unavailable.");
            var root = rider.View.CharacterAvatar.transform; var horse = mount.View.transform;
            var pose = declaration.Pose;
            var bones = new[] { pose.PelvisBoneName, pose.LeftLeg.ThighBoneName, pose.LeftLeg.LowerLegBoneName, pose.LeftLeg.FootBoneName,
                pose.RightLeg.ThighBoneName, pose.RightLeg.LowerLegBoneName, pose.RightLeg.FootBoneName };
            var riderNodes = bones.Select(name => Exact(root, name)).ToArray();
            var chest = Exact(horse, "Chest"); var left = Exact(horse, "L_Stirrup"); var right = Exact(horse, "R_Stirrup");
            var lf = riderNodes[3]; var rf = riderNodes[6];
            return new JObject { ["frame"] = Time.frameCount, ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId,
                ["profileId"] = declaration.Body.ProfileId, ["inventorySha256"] = declaration.Body.NativeInventorySha256,
                ["declarationSha256"] = declaration.NativeCalibrationSha256, ["coordinateConvention"] = "native-Unity",
                ["qualificationStatus"] = "IN PROGRESS", ["visualAcceptanceProven"] = false,
                ["body"] = JObject.FromObject(NativeSmallRiderBody.Capture(rider, mount)),
                ["riderRoot"] = Transform(root), ["horseRoot"] = Transform(horse),
                ["riderBones"] = new JArray(riderNodes.Select(Transform)),
                ["horseAnchors"] = new JArray(new[] { chest, left, right }.Select(Transform)),
                ["pelvisFromChestMountLocal"] = Vector(horse.InverseTransformVector(riderNodes[0].position - chest.position)),
                ["leftFootToLeftStirrup"] = Vector3.Distance(lf.position, left.position),
                ["leftFootToRightStirrup"] = Vector3.Distance(lf.position, right.position),
                ["rightFootToLeftStirrup"] = Vector3.Distance(rf.position, left.position),
                ["rightFootToRightStirrup"] = Vector3.Distance(rf.position, right.position),
                ["leftUpperLength"] = Vector3.Distance(riderNodes[1].position, riderNodes[2].position),
                ["leftLowerLength"] = Vector3.Distance(riderNodes[2].position, lf.position),
                ["rightUpperLength"] = Vector3.Distance(riderNodes[4].position, riderNodes[5].position),
                ["rightLowerLength"] = Vector3.Distance(riderNodes[5].position, rf.position),
                ["runtime"] = runtime == null ? null : new JObject { ["profileId"] = runtime.MountProfileId,
                    ["poseProfileId"] = runtime.PoseProfileId, ["configured"] = runtime.PoseConfigured, ["healthy"] = runtime.PoseHealthy,
                    ["frameApplied"] = runtime.PoseFrameApplied, ["applications"] = runtime.PoseApplicationFrameCount,
                    ["footClamps"] = runtime.PoseFootTargetClampCount, ["maximumFootResidual"] = runtime.PoseMaximumFootTargetResidualWorldUnits,
                    ["maximumKneeResidual"] = runtime.PoseMaximumKneeTargetResidualWorldUnits,
                    ["maximumSegmentResidual"] = runtime.PoseMaximumSegmentLengthResidualWorldUnits,
                    ["componentCount"] = runtime.PoseComponentCount, ["boneCount"] = runtime.PoseBoneCount, ["failure"] = runtime.PoseFailure } };
        }
        private static Transform Exact(Transform root, string name)
        {
            var nodes = root.GetComponentsInChildren<Transform>(true);
            if (nodes.Length > 4096) throw new InvalidOperationException("Native rig observation exceeds the bounded transform inventory.");
            var matches = nodes.Where(node => node != null && node.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Exact native rig transform is absent or ambiguous: " + name);
            return matches[0];
        }
        private static JObject Transform(Transform node) => new JObject { ["name"] = node.name, ["instanceId"] = node.GetInstanceID(),
            ["parentId"] = node.parent == null ? 0 : node.parent.GetInstanceID(), ["localPosition"] = Vector(node.localPosition),
            ["localEuler"] = Vector(node.localEulerAngles), ["localScale"] = Vector(node.localScale),
            ["worldPosition"] = Vector(node.position), ["worldScale"] = Vector(node.lossyScale) };
        private static JObject Vector(Vector3 value)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z))
                throw new InvalidOperationException("Native rig produced a nonfinite vector.");
            return new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        }
    }
}
