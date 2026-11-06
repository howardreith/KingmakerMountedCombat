using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace KingmakerMountedCombat.Integration
{
    // PET-02: cosmetic tack on the KMC Riding Dog only. One skinned renderer per live view, built from the dog's own bones and
    // body mesh, added under the view and handed to the view's native renderer list, so fog, fade, highlight and visibility
    // treat it as part of the dog. It is never saved, carries no fact or stat, and goes with the view when the view goes.
    internal sealed class RidingDogTackService : IUnitViewAttachedUIHandler, ISceneHandler, IDisposable
    {
        internal const string ObjectName = "KMC_RidingDogTack";
        internal static RidingDogTackService Current { get; private set; }
        private readonly IModLogger logger;
        private readonly Dictionary<string, SharedTack> shared = new Dictionary<string, SharedTack>(StringComparer.Ordinal);
        private readonly List<RidingDogTackBinding> live = new List<RidingDogTackBinding>();
        private Texture2D atlas, flatNormal;
        private bool disposed;

        internal RidingDogTackService(IModLogger logger)
        {
            this.logger = logger;
            EventBus.Subscribe(this);
            Current = this;
        }

        internal int Attachments { get; private set; }
        internal int MeshBuilds { get; private set; }

        public void HandleUnitViewAttached(UnitEntityData unit) => TryAttach(unit);

        public void OnAreaBeginUnloading() { }

        // Views that attached before this service subscribed, such as on the first load after startup.
        public void OnAreaDidLoad()
        {
            foreach (var unit in Game.Instance.State.Units.Where(IsRidingDog).ToList()) TryAttach(unit);
        }

        internal static bool IsRidingDog(UnitEntityData unit) =>
            unit?.Blueprint?.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid;

        internal static RidingDogTackBinding[] Bindings(UnitEntityView view) =>
            view == null ? new RidingDogTackBinding[0] : view.GetComponentsInChildren<RidingDogTackBinding>(true);

        internal RidingDogTackBinding TryAttach(UnitEntityData unit)
        {
            if (disposed || !IsRidingDog(unit)) return null;
            var view = unit.View;
            // A polymorphed dog wears another body; its own view comes back when the form ends and is fitted then.
            if (view == null || unit.Descriptor.Body.IsPolymorphed) return null;
            try
            {
                var existing = Bindings(view);
                if (existing.Length > 0) return existing[0];
                var source = BodyRenderer(view);
                if (source == null) { logger?.Warning("Riding Dog tack: no readable body renderer on " + unit.UniqueId + "."); return null; }
                var tack = Shared(view, source);
                if (tack.Failure != null) return null;
                var gameObject = new GameObject(ObjectName) { layer = source.gameObject.layer };
                gameObject.transform.SetParent(source.transform.parent, false);
                gameObject.transform.localPosition = source.transform.localPosition;
                gameObject.transform.localRotation = source.transform.localRotation;
                gameObject.transform.localScale = source.transform.localScale;
                var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = tack.Mesh;
                renderer.bones = source.bones;
                renderer.rootBone = source.rootBone;
                renderer.sharedMaterial = tack.Material;
                renderer.quality = source.quality;
                renderer.updateWhenOffscreen = source.updateWhenOffscreen;
                renderer.shadowCastingMode = source.shadowCastingMode;
                renderer.receiveShadows = source.receiveShadows;
                renderer.localBounds = source.localBounds;
                var binding = gameObject.AddComponent<RidingDogTackBinding>();
                binding.Bind(unit.UniqueId, view, source, renderer, tack);
                live.RemoveAll(item => item == null);
                live.Add(binding);
                Attachments++;
                // The native renderer cache is rebuilt on the view's next LateUpdate and the tack joins it.
                view.MarkRenderersAndCollidersAreUpdated();
                return binding;
            }
            catch (Exception exception)
            {
                logger?.Warning("Riding Dog tack failed for " + unit.UniqueId + ": " + exception.GetType().Name + ": " + exception.Message);
                return null;
            }
        }

        // The dog's main body: the readable skinned renderer with the most vertices, from the finest LOD when there is a group.
        private static SkinnedMeshRenderer BodyRenderer(UnitEntityView view)
        {
            IEnumerable<SkinnedMeshRenderer> candidates = view.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var group = view.GetComponentInChildren<LODGroup>(true);
            var lods = group == null ? null : group.GetLODs();
            if (lods != null && lods.Length > 0) candidates = lods[0].renderers.OfType<SkinnedMeshRenderer>();
            return candidates.Where(item => item != null && item.GetComponent<RidingDogTackBinding>() == null && item.sharedMesh != null &&
                    item.sharedMesh.isReadable && item.bones != null && item.bones.Length > 0)
                .OrderByDescending(item => item.sharedMesh.vertexCount).FirstOrDefault();
        }

        private SharedTack Shared(UnitEntityView view, SkinnedMeshRenderer source)
        {
            var toRoot = view.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            var key = source.sharedMesh.GetInstanceID() + "|" + string.Join(",", Enumerable.Range(0, 16).Select(i => toRoot[i].ToString("F3")).ToArray());
            if (shared.TryGetValue(key, out var cached)) return cached;
            var tack = new SharedTack();
            shared[key] = tack;
            MeshBuilds++;
            var mesh = source.sharedMesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var boneWeights = mesh.boneWeights;
            if (normals.Length != vertices.Length || boneWeights.Length != vertices.Length)
            {
                tack.Failure = "the body mesh has no normals or bone weights";
                logger?.Warning("Riding Dog tack: " + tack.Failure + ".");
                return tack;
            }
            var body = new TackSourceMesh
            {
                Positions = new float[vertices.Length * 3], Normals = new float[vertices.Length * 3], Triangles = mesh.triangles,
                BoneIndices = new int[vertices.Length * 4], BoneWeights = new float[vertices.Length * 4]
            };
            for (var i = 0; i < vertices.Length; i++)
            {
                var position = toRoot.MultiplyPoint3x4(vertices[i]);
                var normal = toRoot.MultiplyVector(normals[i]).normalized;
                body.Positions[i * 3] = position.x; body.Positions[i * 3 + 1] = position.y; body.Positions[i * 3 + 2] = position.z;
                body.Normals[i * 3] = normal.x; body.Normals[i * 3 + 1] = normal.y; body.Normals[i * 3 + 2] = normal.z;
                var weight = boneWeights[i];
                body.BoneIndices[i * 4] = weight.boneIndex0; body.BoneIndices[i * 4 + 1] = weight.boneIndex1;
                body.BoneIndices[i * 4 + 2] = weight.boneIndex2; body.BoneIndices[i * 4 + 3] = weight.boneIndex3;
                body.BoneWeights[i * 4] = weight.weight0; body.BoneWeights[i * 4 + 1] = weight.weight1;
                body.BoneWeights[i * 4 + 2] = weight.weight2; body.BoneWeights[i * 4 + 3] = weight.weight3;
            }
            var fit = RidingDogTackGeometry.Build(body, RidingDogTackGeometry.SeatForward, out var failure);
            if (fit == null)
            {
                tack.Failure = failure;
                logger?.Warning("Riding Dog tack: " + failure + ".");
                return tack;
            }
            tack.Fit = fit;
            tack.Mesh = ToMesh(fit, toRoot.inverse, toRoot.determinant < 0f, mesh.bindposes);
            tack.Material = TackMaterial(source.sharedMaterial, tack);
            logger?.Info("Riding Dog tack fitted: " + fit.VertexCount + " vertices, back " + fit.BackHeightAtSeat.ToString("F3") + ", seat " +
                fit.SeatTopAtSeat.ToString("F3") + ", cantle " + fit.CantleTop.ToString("F3") + ", pommel " + fit.PommelTop.ToString("F3") +
                ", " + fit.Misses + " of " + fit.Casts + " casts missed.");
            return tack;
        }

        private static Mesh ToMesh(TackMeshData fit, Matrix4x4 toMesh, bool mirrored, Matrix4x4[] bindposes)
        {
            var count = fit.VertexCount;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            var weights = new BoneWeight[count];
            for (var i = 0; i < count; i++)
            {
                vertices[i] = toMesh.MultiplyPoint3x4(new Vector3(fit.Positions[i * 3], fit.Positions[i * 3 + 1], fit.Positions[i * 3 + 2]));
                normals[i] = toMesh.MultiplyVector(new Vector3(fit.Normals[i * 3], fit.Normals[i * 3 + 1], fit.Normals[i * 3 + 2])).normalized;
                uvs[i] = new Vector2(fit.Uvs[i * 2], fit.Uvs[i * 2 + 1]);
                weights[i] = new BoneWeight
                {
                    boneIndex0 = fit.BoneIndices[i * 4], boneIndex1 = fit.BoneIndices[i * 4 + 1], boneIndex2 = fit.BoneIndices[i * 4 + 2], boneIndex3 = fit.BoneIndices[i * 4 + 3],
                    weight0 = fit.BoneWeights[i * 4], weight1 = fit.BoneWeights[i * 4 + 1], weight2 = fit.BoneWeights[i * 4 + 2], weight3 = fit.BoneWeights[i * 4 + 3]
                };
            }
            var triangles = (int[])fit.Triangles.Clone();
            if (mirrored)
                for (var t = 0; t < triangles.Length; t += 3) { var swap = triangles[t + 1]; triangles[t + 1] = triangles[t + 2]; triangles[t + 2] = swap; }
            var result = new Mesh { name = ObjectName, vertices = vertices, normals = normals, uv = uvs, boneWeights = weights, bindposes = bindposes, triangles = triangles };
            result.RecalculateTangents();
            result.RecalculateBounds();
            return result;
        }

        // The dog's own material and shader, so native lighting, fog and effects match; only its surface textures are replaced.
        private Material TackMaterial(Material dog, SharedTack tack)
        {
            var material = new Material(dog) { name = ObjectName };
            if (atlas == null) atlas = RidingDogTackTexture.CreateAtlas();
            if (flatNormal == null) flatNormal = RidingDogTackTexture.CreateFlatNormal();
            var replaced = new List<string>();
            var kept = new List<string>();
            foreach (var property in material.GetTexturePropertyNames())
            {
                if (property == "_MainTex") { material.SetTexture(property, atlas); material.SetTextureScale(property, Vector2.one); material.SetTextureOffset(property, Vector2.zero); replaced.Add(property); }
                else if (property == "_BumpMap") { material.SetTexture(property, flatNormal); material.SetTextureScale(property, Vector2.one); material.SetTextureOffset(property, Vector2.zero); replaced.Add(property); }
                else kept.Add(property);
            }
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            tack.Shader = material.shader == null ? null : material.shader.name;
            tack.ReplacedTextures = replaced.ToArray();
            tack.KeptTextures = kept.ToArray();
            return material;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            EventBus.Unsubscribe(this);
            foreach (var binding in live.Where(item => item != null).ToList())
            {
                var view = binding.View;
                Object.Destroy(binding.gameObject);
                if (view != null) view.MarkRenderersAndCollidersAreUpdated();
            }
            live.Clear();
            foreach (var tack in shared.Values)
            {
                if (tack.Mesh != null) Object.Destroy(tack.Mesh);
                if (tack.Material != null) Object.Destroy(tack.Material);
            }
            shared.Clear();
            if (atlas != null) Object.Destroy(atlas);
            if (flatNormal != null) Object.Destroy(flatNormal);
            if (ReferenceEquals(Current, this)) Current = null;
        }
    }

    internal sealed class SharedTack
    {
        internal Mesh Mesh;
        internal Material Material;
        internal TackMeshData Fit;
        internal string Failure, Shader;
        internal string[] ReplacedTextures, KeptTextures;
    }

    // Marks the one tack object of a view; Unity destroys it with the view.
    internal sealed class RidingDogTackBinding : MonoBehaviour
    {
        internal string UnitId { get; private set; }
        internal UnitEntityView View { get; private set; }
        internal SkinnedMeshRenderer Source { get; private set; }
        internal SkinnedMeshRenderer Renderer { get; private set; }
        internal SharedTack Tack { get; private set; }

        internal void Bind(string unitId, UnitEntityView view, SkinnedMeshRenderer source, SkinnedMeshRenderer renderer, SharedTack tack)
        {
            UnitId = unitId; View = view; Source = source; Renderer = renderer; Tack = tack;
        }

        internal bool AdoptedByView => View != null && View.Renderers != null && View.Renderers.Contains(Renderer);
    }
}
