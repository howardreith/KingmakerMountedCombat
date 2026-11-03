using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Fixture only. The native character merges its body parts into one unreadable mesh, so its rendered surface cannot be baked.
    // This skins the same source parts with the same live bones, in the frame of the pixels beside it, one declared space.
    internal static class Sprint5BootObservation
    {
        private static readonly Regex LegBone = new Regex("leg|foot|toe|knee|calf|thigh|ankle|shin|twist", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private const int MaximumBootSamples = 200;
        private const float SurfaceWindow = 0.05f;

        internal static JObject Observe(UnitEntityData rider, UnitEntityData mount, bool mounted)
        {
            var avatar = rider.View.CharacterAvatar;
            var space = mounted ? mount.View.transform : rider.View.transform;
            var root = avatar.transform;
            var live = Hierarchy(root);
            var row = new JObject { ["frame"] = Time.frameCount, ["space"] = mounted ? "mount-root-local" : "rider-view-local", ["skeleton"] = avatar.Skeleton?.name,
                ["mirror"] = avatar.Mirror, ["profile"] = SupportedMountedProfiles.Resolve(rider, mount)?.Id };
            var adapter = rider.View.GetComponent<MountedRiderPoseAdapter>();
            if (adapter != null && adapter.IsConfigured)
                row["adapter"] = new JObject { ["profile"] = adapter.ProfileId, ["healthy"] = adapter.IsHealthy, ["clamps"] = adapter.FootTargetClampCount, ["frames"] = adapter.PoseApplicationFrameCount,
                    ["footResidual"] = adapter.MaximumFootTargetResidualWorldUnits, ["kneeResidual"] = adapter.MaximumKneeTargetResidualWorldUnits, ["segmentResidual"] = adapter.MaximumSegmentLengthResidualWorldUnits };
            row["bones"] = Bones(root, space);
            row["renderers"] = new JArray(root.GetComponentsInChildren<Renderer>(true).Select(renderer => RendererRow(renderer, space)));
            row["combined"] = new JArray(avatar.Renderers.Where(renderer => renderer != null).Select(renderer => CombinedRow(renderer, live)));
            var leftLower = Sprint5Fixture.Bone(root, "L_leg"); var rightLower = Sprint5Fixture.Bone(root, "R_leg");
            var leftBoot = new List<Vector3>(); var rightBoot = new List<Vector3>();
            row["parts"] = new JArray(RenderedParts(avatar).Select(part => PartRow(part.Key, part.Value, avatar, live, space, leftLower, rightLower, leftBoot, rightBoot)));
            row["leftBoot"] = Extent(leftBoot); row["rightBoot"] = Extent(rightBoot);
            if (mounted)
            {
                var surfaces = new JArray();
                foreach (var renderer in mount.View.GetComponentsInChildren<Renderer>(true).Where(value => !value.transform.IsChildOf(rider.View.transform)))
                    surfaces.Add(SurfaceRow(renderer, space, leftBoot, rightBoot));
                row["mountSurfaces"] = surfaces;
            }
            return row;
        }

        // The native CacheHierarchy order: the first transform of a name wins.
        private static Dictionary<string, Transform> Hierarchy(Transform root)
        {
            var found = new Dictionary<string, Transform>();
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count != 0)
            {
                var node = stack.Pop();
                if (!found.ContainsKey(node.name)) found[node.name] = node;
                for (var index = 0; index < node.childCount; index++) stack.Push(node.GetChild(index));
            }
            return found;
        }

        // The native geometry choice: by layer, a higher layer's HideBodyParts hides a part, and the last part of a type wins.
        private static IEnumerable<KeyValuePair<BodyPart, EquipmentEntity>> RenderedParts(Character avatar)
        {
            var chosen = new List<KeyValuePair<BodyPart, EquipmentEntity>>();
            foreach (var entity in avatar.EquipmentEntities.Where(value => value != null && value.BodyParts.Count != 0).OrderBy(value => value.Layer))
            {
                BodyPartType hidden = 0;
                foreach (var other in avatar.EquipmentEntities)
                    if (other != null && other != entity && other.Layer > entity.Layer) hidden |= other.HideBodyParts;
                foreach (var part in entity.BodyParts)
                {
                    if (part == null || (hidden & part.Type) != 0 || part.SkinnedRenderer == null || part.Material == null) continue;
                    chosen.RemoveAll(pair => pair.Key.Type == part.Type);
                    chosen.Add(new KeyValuePair<BodyPart, EquipmentEntity>(part, entity));
                }
            }
            return chosen;
        }

        private static JArray Bones(Transform root, Transform space)
        {
            var pelvis = Sprint5Fixture.Bone(root, "Pelvis");
            var rows = new JArray();
            if (pelvis == null) return rows;
            foreach (var bone in pelvis.GetComponentsInChildren<Transform>(true).Where(node => node == pelvis || LegBone.IsMatch(node.name)))
                rows.Add(new JObject { ["name"] = bone.name, ["parent"] = bone.parent?.name, ["position"] = Sprint5Fixture.Vector(space.InverseTransformPoint(bone.position)),
                    ["forward"] = Sprint5Fixture.Vector(space.InverseTransformDirection(bone.forward)), ["up"] = Sprint5Fixture.Vector(space.InverseTransformDirection(bone.up)),
                    ["right"] = Sprint5Fixture.Vector(space.InverseTransformDirection(bone.right)), ["scale"] = Sprint5Fixture.Vector(bone.lossyScale) });
            return rows;
        }

        private static JObject RendererRow(Renderer renderer, Transform space) => new JObject { ["name"] = renderer.name, ["type"] = renderer.GetType().Name, ["enabled"] = renderer.enabled,
            ["active"] = renderer.gameObject.activeInHierarchy, ["visible"] = renderer.isVisible, ["material"] = renderer.sharedMaterial?.name, ["shader"] = renderer.sharedMaterial?.shader?.name,
            ["boundsCenter"] = Sprint5Fixture.Vector(space.InverseTransformPoint(renderer.bounds.center)), ["boundsSize"] = Sprint5Fixture.Vector(renderer.bounds.size) };

        private static JObject CombinedRow(SkinnedMeshRenderer renderer, Dictionary<string, Transform> live)
        {
            var bones = renderer.bones ?? new Transform[0];
            var legs = new JObject();
            foreach (var group in bones.Where(bone => bone != null && LegBone.IsMatch(bone.name)).GroupBy(bone => bone.name))
                legs[group.Key] = new JObject { ["slots"] = group.Count(), ["liveTransform"] = group.All(bone => live.TryGetValue(bone.name, out var same) && same == bone) };
            return new JObject { ["name"] = renderer.name, ["bones"] = bones.Length, ["nullBones"] = bones.Count(bone => bone == null), ["bindposes"] = renderer.sharedMesh?.bindposes?.Length ?? -1,
                ["readable"] = renderer.sharedMesh?.isReadable, ["vertices"] = renderer.sharedMesh?.vertexCount ?? -1, ["rootBone"] = renderer.rootBone?.name, ["updateOffscreen"] = renderer.updateWhenOffscreen, ["legSlots"] = legs };
        }

        private static JObject PartRow(BodyPart part, EquipmentEntity entity, Character avatar, Dictionary<string, Transform> live, Transform space,
            Transform leftLower, Transform rightLower, List<Vector3> leftBoot, List<Vector3> rightBoot)
        {
            var renderer = part.SkinnedRenderer; var mesh = renderer.sharedMesh;
            var row = new JObject { ["entity"] = entity.name, ["layer"] = entity.Layer, ["type"] = part.Type.ToString(), ["mesh"] = mesh?.name, ["readable"] = mesh?.isReadable, ["vertices"] = mesh?.vertexCount ?? -1 };
            if (mesh == null || !mesh.isReadable) return row;
            var sourceBones = renderer.bones; var bindposes = mesh.bindposes; var weights = mesh.boneWeights; var vertices = mesh.vertices;
            var matrices = new Matrix4x4[sourceBones.Length]; var bound = new Transform[sourceBones.Length];
            // The native merge stops mapping at the first bone this skeleton lacks, leaving the rest of the part on the merged list's first bone.
            var first = avatar.Renderers.FirstOrDefault(value => value != null)?.bones?.FirstOrDefault();
            var stopped = false;
            for (var index = 0; index < sourceBones.Length; index++)
            {
                if (!stopped && (sourceBones[index] == null || !live.TryGetValue(sourceBones[index].name, out bound[index])))
                {
                    stopped = true; row["mappingStoppedAt"] = index; row["missingBone"] = sourceBones[index]?.name;
                }
                if (stopped) bound[index] = first;
                matrices[index] = bound[index] == null ? Matrix4x4.identity : bound[index].localToWorldMatrix * bindposes[index];
            }
            var dominant = new Dictionary<string, List<Vector3>>();
            var perBone = new int[sourceBones.Length];
            for (var vertex = 0; vertex < vertices.Length && vertex < weights.Length; vertex++)
            {
                var weight = weights[vertex];
                var skinned = matrices[weight.boneIndex0].MultiplyPoint3x4(vertices[vertex]) * weight.weight0 + matrices[weight.boneIndex1].MultiplyPoint3x4(vertices[vertex]) * weight.weight1 +
                    matrices[weight.boneIndex2].MultiplyPoint3x4(vertices[vertex]) * weight.weight2 + matrices[weight.boneIndex3].MultiplyPoint3x4(vertices[vertex]) * weight.weight3;
                var top = weight.boneIndex0;
                var topWeight = weight.weight0;
                if (weight.weight1 > topWeight) { top = weight.boneIndex1; topWeight = weight.weight1; }
                if (weight.weight2 > topWeight) { top = weight.boneIndex2; topWeight = weight.weight2; }
                if (weight.weight3 > topWeight) { top = weight.boneIndex3; }
                perBone[top]++;
                var bone = bound[top];
                if (bone == null) continue;
                var local = space.InverseTransformPoint(skinned);
                if (LegBone.IsMatch(bone.name))
                {
                    List<Vector3> list;
                    if (!dominant.TryGetValue(bone.name, out list)) dominant[bone.name] = list = new List<Vector3>();
                    list.Add(local);
                }
                if (leftLower != null && (bone == leftLower || bone.IsChildOf(leftLower))) leftBoot.Add(local);
                if (rightLower != null && (bone == rightLower || bone.IsChildOf(rightLower))) rightBoot.Add(local);
            }
            row["legBones"] = new JObject(dominant.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new JProperty(pair.Key, Extent(pair.Value))));
            row["dominantBones"] = new JObject(Enumerable.Range(0, sourceBones.Length).Where(index => perBone[index] != 0).OrderByDescending(index => perBone[index]).Take(14)
                .Select(index => new JProperty(index + ":" + (sourceBones[index]?.name ?? "null") + ">" + (bound[index]?.parent?.name ?? "null"), perBone[index])));
            return row;
        }

        private static JObject Extent(List<Vector3> points)
        {
            if (points.Count == 0) return new JObject { ["count"] = 0 };
            return new JObject { ["count"] = points.Count,
                ["min"] = Sprint5Fixture.Vector(new Vector3(points.Min(point => point.x), points.Min(point => point.y), points.Min(point => point.z))),
                ["max"] = Sprint5Fixture.Vector(new Vector3(points.Max(point => point.x), points.Max(point => point.y), points.Max(point => point.z))) };
        }

        private static JObject SurfaceRow(Renderer renderer, Transform space, List<Vector3> leftBoot, List<Vector3> rightBoot)
        {
            var row = new JObject { ["name"] = renderer.name, ["type"] = renderer.GetType().Name, ["enabled"] = renderer.enabled, ["active"] = renderer.gameObject.activeInHierarchy };
            var points = SurfacePoints(renderer, space, row);
            if (points == null) return row;
            row["vertices"] = points.Length;
            row["left"] = AgainstBoot(points, leftBoot);
            row["right"] = AgainstBoot(points, rightBoot);
            return row;
        }

        private static Vector3[] SurfacePoints(Renderer renderer, Transform space, JObject row)
        {
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                if (skinned.sharedMesh == null) return null;
                row["readable"] = skinned.sharedMesh.isReadable;
                if (!skinned.sharedMesh.isReadable) return null;
                var mesh = new Mesh();
                try
                {
                    skinned.BakeMesh(mesh);
                    var toWorld = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                    return mesh.vertices.Select(vertex => space.InverseTransformPoint(toWorld.MultiplyPoint3x4(vertex))).ToArray();
                }
                finally { UnityEngine.Object.Destroy(mesh); }
            }
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return null;
            row["readable"] = filter.sharedMesh.isReadable;
            if (!filter.sharedMesh.isReadable) return null;
            var matrix = renderer.transform.localToWorldMatrix;
            return filter.sharedMesh.vertices.Select(vertex => space.InverseTransformPoint(matrix.MultiplyPoint3x4(vertex))).ToArray();
        }

        // For each sampled boot vertex: the mount's outermost vertex at that height and length on that side. A boot vertex nearer the centerline than it is under that surface.
        private static JObject AgainstBoot(Vector3[] surface, List<Vector3> boot)
        {
            var evaluated = 0; var under = 0; var outermost = 0f; var deepest = 0f;
            var step = Math.Max(1, boot.Count / MaximumBootSamples);
            for (var index = 0; index < boot.Count; index += step)
            {
                var point = boot[index];
                var found = false; var limit = 0f;
                foreach (var candidate in surface)
                {
                    if (Mathf.Abs(candidate.y - point.y) > SurfaceWindow || Mathf.Abs(candidate.z - point.z) > SurfaceWindow || candidate.x * point.x < 0f) continue;
                    found = true; limit = Mathf.Max(limit, Mathf.Abs(candidate.x));
                }
                if (!found) continue;
                evaluated++; outermost = Mathf.Max(outermost, limit);
                var depth = limit - Mathf.Abs(point.x);
                if (depth > 0f) { under++; deepest = Mathf.Max(deepest, depth); }
            }
            return new JObject { ["samples"] = Math.Min(boot.Count, boot.Count / step + 1), ["evaluated"] = evaluated, ["under"] = under, ["surfaceX"] = outermost, ["deepest"] = deepest };
        }
    }

}
