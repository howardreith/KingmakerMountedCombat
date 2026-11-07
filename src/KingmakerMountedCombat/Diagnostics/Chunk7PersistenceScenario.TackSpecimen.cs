using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KingmakerMountedCombat.Diagnostics
{
    // F5 PET-02 diagnostics only: a private replay specimen of the Riding Dog's tack, written into the run's own evidence folder.
    // The fit file holds exactly what the fit was given and what it made (once per process); each pose file holds a same-frame
    // native snapshot (BakeMesh) of the body and the tack with every matrix needed to bring them into one space. Spaces:
    // mesh = the body mesh's bind space; fit = the view's local space when the fit ran; renderer = that renderer's local space.
    // Matrices are row-major. Nothing here runs outside this scenario or changes what is rendered.
    internal sealed partial class Chunk7PersistenceScenario
    {
        private readonly JArray f5Specimens = new JArray();
        private bool f5FitWritten;

        private bool Specimen(string label)
        {
            var binding = RidingDogTackService.Bindings(secondMount.View).FirstOrDefault();
            var row = new JObject { ["label"] = label, ["frame"] = Time.frameCount };
            try
            {
                var tack = binding?.Tack;
                if (binding == null || tack == null || binding.Source == null) throw new InvalidOperationException("no bound tack on the Riding Dog");
                if (!f5FitWritten) { row["fit"] = WriteSpecimen("tack-specimen-fit.json", FitSpecimen(binding, tack)); f5FitWritten = true; }
                row["pose"] = WriteSpecimen("tack-specimen-" + label + ".json", PoseSpecimen(label, binding));
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            f5Specimens.Add(row);
            f5["specimens"] = f5Specimens;
            return true;
        }

        private JObject WriteSpecimen(string name, JObject content)
        {
            var folder = Path.Combine(request.EvidenceRoot, "tack-specimen"); Directory.CreateDirectory(folder);
            var bytes = new UTF8Encoding(false).GetBytes(content.ToString(Formatting.None));
            File.WriteAllBytes(Path.Combine(folder, name), bytes);
            using (var algorithm = SHA256.Create())
                return new JObject { ["file"] = "tack-specimen/" + name, ["bytes"] = bytes.Length, ["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() };
        }

        private JObject FitSpecimen(RidingDogTackBinding binding, SharedTack tack)
        {
            var source = binding.Source;
            var mesh = source.sharedMesh;
            var weights = mesh.boneWeights;
            var fit = tack.Fit;
            var body = tack.Body;
            return new JObject
            {
                ["schema"] = 1, ["runId"] = request.RunId, ["commit"] = request.Commit, ["dllSha256"] = request.DllSha256, ["dllMvid"] = request.DllMvid,
                ["unit"] = binding.UnitId, ["view"] = binding.View?.name, ["renderer"] = source.name, ["rendererPath"] = PathOf(source.transform),
                ["mesh"] = mesh.name, ["meshInstance"] = mesh.GetInstanceID(), ["meshVertices"] = mesh.vertexCount, ["subMeshes"] = mesh.subMeshCount,
                ["quality"] = source.quality.ToString(), ["tackQuality"] = binding.Renderer.quality.ToString(), ["blendWeights"] = QualitySettings.blendWeights.ToString(),
                ["rootBone"] = source.rootBone?.name, ["bones"] = new JArray(source.bones.Select(bone => bone == null ? null : bone.name)),
                ["bonePaths"] = new JArray(source.bones.Select(bone => bone == null ? null : PathOf(bone))),
                ["renderers"] = Renderers(binding), ["materials"] = new JArray(MaterialRow(source.sharedMaterial), MaterialRow(binding.Renderer.sharedMaterial)),
                ["failure"] = tack.Failure, ["seatForward"] = RidingDogTackGeometry.SeatForward,
                ["fitViewToWorld"] = Matrix(tack.FitViewToWorld),
                ["fitSkin"] = tack.FitSkin == null ? null : new JArray(tack.FitSkin.Select(Matrix)),
                ["bindposes"] = new JArray(mesh.bindposes.Select(Matrix)),
                ["meshRaw"] = new JObject
                {
                    ["vertices"] = Floats(mesh.vertices), ["normals"] = Floats(mesh.normals), ["triangles"] = new JArray(mesh.triangles),
                    ["boneIndices"] = new JArray(weights.SelectMany(w => new[] { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 })),
                    ["boneWeights"] = new JArray(weights.SelectMany(w => new[] { w.weight0, w.weight1, w.weight2, w.weight3 }))
                },
                ["body"] = body == null ? null : new JObject
                {
                    ["positions"] = new JArray(body.Positions), ["normals"] = new JArray(body.Normals), ["triangles"] = new JArray(body.Triangles),
                    ["boneIndices"] = new JArray(body.BoneIndices), ["boneWeights"] = new JArray(body.BoneWeights)
                },
                ["tack"] = fit == null ? null : new JObject
                {
                    ["positions"] = new JArray(fit.Positions), ["normals"] = new JArray(fit.Normals), ["uvs"] = new JArray(fit.Uvs), ["triangles"] = new JArray(fit.Triangles),
                    ["boneIndices"] = new JArray(fit.BoneIndices), ["boneWeights"] = new JArray(fit.BoneWeights),
                    ["padVertexEnd"] = fit.PadVertexEnd, ["seatVertexEnd"] = fit.SeatVertexEnd, ["maxRise"] = fit.MaxRiseAboveBack, ["backAtSeat"] = fit.BackHeightAtSeat,
                    ["minClearance"] = Finite(fit.MinClearance), ["maxClearance"] = Finite(fit.MaxClearance), ["unsupported"] = fit.Unsupported, ["fitMilliseconds"] = tack.FitMilliseconds
                },
                ["tackMesh"] = tack.Mesh == null ? null : new JObject
                {
                    ["vertices"] = Floats(tack.Mesh.vertices), ["normals"] = Floats(tack.Mesh.normals), ["triangles"] = new JArray(tack.Mesh.triangles)
                }
            };
        }

        private JObject PoseSpecimen(string label, RidingDogTackBinding binding)
        {
            var view = binding.View;
            return new JObject
            {
                ["schema"] = 1, ["label"] = label, ["frame"] = Time.frameCount, ["time"] = Time.time,
                ["moving"] = view?.AgentASP?.IsReallyMoving ?? false, ["mounted"] = secondRider.View != null && secondRider.View.transform.IsChildOf(view.transform),
                ["viewToWorld"] = Matrix(view.transform.localToWorldMatrix),
                ["bodyToWorld"] = Matrix(binding.Source.transform.localToWorldMatrix), ["bodyScale"] = Floats(new[] { binding.Source.transform.lossyScale }),
                ["tackToWorld"] = Matrix(binding.Renderer.transform.localToWorldMatrix), ["tackScale"] = Floats(new[] { binding.Renderer.transform.lossyScale }),
                ["boneToWorld"] = new JArray(binding.Source.bones.Select(bone => Matrix(bone.localToWorldMatrix))),
                ["body"] = Baked(binding.Source), ["tack"] = Baked(binding.Renderer),
                ["tackVisible"] = binding.Renderer.isVisible, ["bodyVisible"] = binding.Source.isVisible, ["renderers"] = Renderers(binding)
            };
        }

        private static JObject Baked(SkinnedMeshRenderer renderer)
        {
            var mesh = new Mesh();
            try
            {
                renderer.BakeMesh(mesh);
                return new JObject { ["vertices"] = Floats(mesh.vertices), ["normals"] = Floats(mesh.normals) };
            }
            finally { Object.Destroy(mesh); }
        }

        // Every renderer the dog's own view owns, so the one that is drawn can be told from the one that was fitted.
        private static JArray Renderers(RidingDogTackBinding binding)
        {
            var view = binding.View;
            var group = view.GetComponentsInChildren<LODGroup>(true).FirstOrDefault(item => item.GetComponentInParent<Kingmaker.View.UnitEntityView>() == view);
            var lods = group == null ? new LOD[0] : group.GetLODs();
            var rows = new JArray();
            foreach (var renderer in view.GetComponentsInChildren<Renderer>(true).Where(item => item.GetComponentInParent<Kingmaker.View.UnitEntityView>() == view))
            {
                var skinned = renderer as SkinnedMeshRenderer;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = skinned != null ? skinned.sharedMesh : filter?.sharedMesh;
                rows.Add(new JObject
                {
                    ["name"] = renderer.name, ["type"] = renderer.GetType().Name, ["path"] = PathOf(renderer.transform), ["mesh"] = mesh?.name, ["vertices"] = mesh?.vertexCount ?? 0,
                    ["readable"] = mesh != null && mesh.isReadable, ["enabled"] = renderer.enabled, ["active"] = renderer.gameObject.activeInHierarchy, ["visible"] = renderer.isVisible,
                    ["lod"] = Array.FindIndex(lods, lod => lod.renderers.Contains(renderer)), ["selected"] = renderer == binding.Source, ["tack"] = renderer == binding.Renderer,
                    ["adopted"] = view.Renderers != null && view.Renderers.Contains(renderer), ["shader"] = renderer.sharedMaterial?.shader?.name
                });
            }
            return rows;
        }

        private static JObject MaterialRow(UnityEngine.Material material)
        {
            if (material == null) return null;
            var row = new JObject { ["name"] = material.name, ["shader"] = material.shader?.name, ["renderQueue"] = material.renderQueue, ["keywords"] = new JArray(material.shaderKeywords) };
            foreach (var property in new[] { "_Cull", "_Cutoff", "_Mode", "_ZWrite", "_SrcBlend", "_DstBlend" })
                if (material.HasProperty(property)) row[property] = material.GetFloat(property);
            return row;
        }

        private static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (var item = transform; item != null; item = item.parent) names.Add(item.name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static JArray Matrix(Matrix4x4 matrix)
        {
            var values = new JArray();
            for (var r = 0; r < 4; r++) for (var c = 0; c < 4; c++) values.Add(matrix[r, c]);
            return values;
        }

        private static JArray Floats(IEnumerable<Vector3> vectors)
        {
            var values = new JArray();
            foreach (var vector in vectors) { values.Add(vector.x); values.Add(vector.y); values.Add(vector.z); }
            return values;
        }
    }
}
