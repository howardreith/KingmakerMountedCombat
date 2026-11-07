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
        private readonly Dictionary<int, SharedTack> shared = new Dictionary<int, SharedTack>();
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
                if (tack.Failure != null || tack.Mesh == null || tack.Material == null) return null;
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
        // Only the view's own parts: a mounted rider's whole view hangs under the mount's and must never be fitted.
        private static SkinnedMeshRenderer BodyRenderer(UnitEntityView view)
        {
            IEnumerable<SkinnedMeshRenderer> candidates = view.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(item => OwnedBy(item, view));
            var group = view.GetComponentsInChildren<LODGroup>(true).FirstOrDefault(item => OwnedBy(item, view));
            var lods = group == null ? null : group.GetLODs();
            if (lods != null && lods.Length > 0) candidates = lods[0].renderers.OfType<SkinnedMeshRenderer>().Where(item => OwnedBy(item, view));
            return candidates.Where(item => item != null && item.GetComponent<RidingDogTackBinding>() == null && item.sharedMesh != null &&
                    item.sharedMesh.isReadable && item.bones != null && item.bones.Length > 0)
                .OrderByDescending(item => item.sharedMesh.vertexCount).FirstOrDefault();
        }

        private static bool OwnedBy(Component item, UnitEntityView view) => item != null && item.GetComponentInParent<UnitEntityView>() == view;

        // One fit per body mesh. It is made from where the bones actually hold the body now, in mount-local space, and stored
        // as each vertex's pre-image under its own blended skin matrix, so any view of the same mesh carries it in any pose.
        private SharedTack Shared(UnitEntityView view, SkinnedMeshRenderer source)
        {
            Purge();
            var mesh = source.sharedMesh;
            var key = mesh.GetInstanceID();
            if (shared.TryGetValue(key, out var cached)) return cached;
            var tack = new SharedTack { SourceMesh = mesh };
            MeshBuilds++;
            // A fit is cached only once it is complete or has a recorded failure, so no later view adopts a half-built renderer.
            try { Fit(view, source, tack); }
            catch (Exception exception)
            {
                if (tack.Mesh != null) Object.Destroy(tack.Mesh);
                if (tack.Material != null) Object.Destroy(tack.Material);
                tack.Mesh = null; tack.Material = null; tack.Fit = null;
                tack.Failure = exception.GetType().Name + ": " + exception.Message;
                logger?.Warning("Riding Dog tack: " + tack.Failure + ".");
            }
            shared[key] = tack;
            return tack;
        }

        private void Fit(UnitEntityView view, SkinnedMeshRenderer source, SharedTack tack)
        {
            var mesh = source.sharedMesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var boneWeights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var bones = source.bones;
            if (normals.Length != vertices.Length || boneWeights.Length != vertices.Length || bindposes.Length != bones.Length || bones.Any(bone => bone == null) ||
                boneWeights.Any(w => w.boneIndex0 >= bones.Length || w.boneIndex1 >= bones.Length || w.boneIndex2 >= bones.Length || w.boneIndex3 >= bones.Length) || source.sharedMaterial == null)
            {
                tack.Failure = "the body mesh has no normals, bone weights, complete bones or material";
                logger?.Warning("Riding Dog tack: " + tack.Failure + ".");
                return;
            }
            var toRoot = view.transform.worldToLocalMatrix;
            var skin = new Matrix4x4[bones.Length];
            for (var b = 0; b < bones.Length; b++) skin[b] = toRoot * bones[b].localToWorldMatrix * bindposes[b];
            var body = new TackSourceMesh
            {
                Positions = new float[vertices.Length * 3], Normals = new float[vertices.Length * 3], Triangles = mesh.triangles,
                BoneIndices = new int[vertices.Length * 4], BoneWeights = new float[vertices.Length * 4]
            };
            for (var i = 0; i < vertices.Length; i++)
            {
                var weight = boneWeights[i];
                var matrix = Blend(skin, weight.boneIndex0, weight.weight0, weight.boneIndex1, weight.weight1, weight.boneIndex2, weight.weight2, weight.boneIndex3, weight.weight3);
                var position = matrix.MultiplyPoint3x4(vertices[i]);
                var normal = matrix.MultiplyVector(normals[i]).normalized;
                body.Positions[i * 3] = position.x; body.Positions[i * 3 + 1] = position.y; body.Positions[i * 3 + 2] = position.z;
                body.Normals[i * 3] = normal.x; body.Normals[i * 3 + 1] = normal.y; body.Normals[i * 3 + 2] = normal.z;
                body.BoneIndices[i * 4] = weight.boneIndex0; body.BoneIndices[i * 4 + 1] = weight.boneIndex1;
                body.BoneIndices[i * 4 + 2] = weight.boneIndex2; body.BoneIndices[i * 4 + 3] = weight.boneIndex3;
                body.BoneWeights[i * 4] = weight.weight0; body.BoneWeights[i * 4 + 1] = weight.weight1;
                body.BoneWeights[i * 4 + 2] = weight.weight2; body.BoneWeights[i * 4 + 3] = weight.weight3;
            }
            // Kept with the fit, accepted or refused, so a diagnostic specimen can replay exactly this input.
            tack.Body = body; tack.FitSkin = skin; tack.FitViewToWorld = view.transform.localToWorldMatrix;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var fit = RidingDogTackGeometry.Build(body, RidingDogTackGeometry.SeatForward, out var failure);
            tack.FitMilliseconds = clock.Elapsed.TotalMilliseconds;
            if (fit == null)
            {
                tack.Failure = failure;
                logger?.Warning("Riding Dog tack: " + failure + ".");
                return;
            }
            tack.Fit = fit;
            tack.Mesh = ToMesh(fit, skin, bindposes);
            tack.Material = TackMaterial(source.sharedMaterial, tack);
            logger?.Info("Riding Dog tack fitted: " + fit.VertexCount + " vertices, back " + fit.BackHeightAtSeat.ToString("F3") + ", seat " +
                fit.SeatTopAtSeat.ToString("F3") + ", cantle " + fit.CantleTop.ToString("F3") + ", pommel " + fit.PommelTop.ToString("F3") +
                ", " + fit.Misses + " of " + fit.Casts + " casts missed.");

        }

        private static Mesh ToMesh(TackMeshData fit, Matrix4x4[] skin, Matrix4x4[] bindposes)
        {
            var count = fit.VertexCount;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            var weights = new BoneWeight[count];
            for (var i = 0; i < count; i++)
            {
                var b = fit.BoneIndices; var w = fit.BoneWeights;
                var toMesh = Blend(skin, b[i * 4], w[i * 4], b[i * 4 + 1], w[i * 4 + 1], b[i * 4 + 2], w[i * 4 + 2], b[i * 4 + 3], w[i * 4 + 3]).inverse;
                vertices[i] = toMesh.MultiplyPoint3x4(new Vector3(fit.Positions[i * 3], fit.Positions[i * 3 + 1], fit.Positions[i * 3 + 2]));
                normals[i] = toMesh.MultiplyVector(new Vector3(fit.Normals[i * 3], fit.Normals[i * 3 + 1], fit.Normals[i * 3 + 2])).normalized;
                uvs[i] = new Vector2(fit.Uvs[i * 2], fit.Uvs[i * 2 + 1]);
                weights[i] = new BoneWeight
                {
                    boneIndex0 = b[i * 4], boneIndex1 = b[i * 4 + 1], boneIndex2 = b[i * 4 + 2], boneIndex3 = b[i * 4 + 3],
                    weight0 = w[i * 4], weight1 = w[i * 4 + 1], weight2 = w[i * 4 + 2], weight3 = w[i * 4 + 3]
                };
            }
            // Skinning places each vertex back where the fit put it, so the fit's outward winding is the rendered one.
            var result = new Mesh { name = ObjectName, vertices = vertices, normals = normals, uv = uvs, boneWeights = weights, bindposes = bindposes, triangles = (int[])fit.Triangles.Clone() };
            result.RecalculateTangents();
            result.RecalculateBounds();
            return result;
        }

        // Linear blend skinning's own matrix for one vertex.
        private static Matrix4x4 Blend(Matrix4x4[] skin, int b0, float w0, int b1, float w1, int b2, float w2, int b3, float w3)
        {
            var result = new Matrix4x4();
            for (var e = 0; e < 16; e++)
                result[e] = skin[b0][e] * w0 + (w1 > 0f ? skin[b1][e] * w1 : 0f) + (w2 > 0f ? skin[b2][e] * w2 : 0f) + (w3 > 0f ? skin[b3][e] * w3 : 0f);
            return result;
        }

        // A fit whose body mesh Unity has unloaded can never be used again; its own mesh and material go with it.
        private void Purge()
        {
            foreach (var key in shared.Where(entry => entry.Value.SourceMesh == null).Select(entry => entry.Key).ToList())
            {
                var stale = shared[key];
                if (stale.Mesh != null) Object.Destroy(stale.Mesh);
                if (stale.Material != null) Object.Destroy(stale.Material);
                shared.Remove(key);
            }
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
                // Out of the view first, so a renderer cache rebuilt before the deferred destroy cannot keep it.
                binding.transform.SetParent(null, false);
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
        internal Mesh Mesh, SourceMesh;
        internal Material Material;
        internal TackMeshData Fit;
        internal TackSourceMesh Body;
        internal Matrix4x4[] FitSkin;
        internal Matrix4x4 FitViewToWorld;
        internal double FitMilliseconds;
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
