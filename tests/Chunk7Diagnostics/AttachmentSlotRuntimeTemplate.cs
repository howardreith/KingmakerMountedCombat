using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

// Facade of the exact Unity 2018.4 hierarchy semantics the actual runtime uses:
// destroyed objects compare equal to null, SetParent appends (no-op for the
// current parent), a parentless node joins the end of its hierarchy's scene
// roots, a sibling index is the node's resulting clamped position, new objects
// are roots of the active scene, and Destroy completes at frame end.
namespace UnityEngine
{
    public enum HideFlags { None = 0, HideAndDontSave = 61 }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float Distance(Vector3 a, Vector3 b) { var d = a - b; return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z); }
        public override string ToString() => "(" + x + "," + y + "," + z + ")";
    }

    public struct Quaternion
    {
        public float yaw;
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(Vector3 euler) => new Quaternion { yaw = euler.y };
        public static float Angle(Quaternion a, Quaternion b) => Math.Abs(a.yaw - b.yaw);
    }

    public class Object
    {
        private static int nextId;
        internal readonly int id = ++nextId;
        internal bool destroyed;
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() => id;
        private static bool IsNull(Object value) => ReferenceEquals(value, null) || value.destroyed;
        private static bool CompareBaseObjects(Object a, Object b)
        {
            var aNull = IsNull(a); var bNull = IsNull(b);
            if (aNull || bNull) return aNull && bNull;
            return a.id == b.id;
        }
        public static bool operator ==(Object a, Object b) => CompareBaseObjects(a, b);
        public static bool operator !=(Object a, Object b) => !CompareBaseObjects(a, b);
        public static implicit operator bool(Object value) => !IsNull(value);
        public override bool Equals(object other)
        {
            var otherObject = other as Object;
            if (otherObject == null && other != null && !(other is Object)) return false;
            return CompareBaseObjects(this, otherObject);
        }
        public override int GetHashCode() => id;
        public static void Destroy(Object target) => Engine.Destroy(target);
        public static void DontDestroyOnLoad(Object target) => Engine.DontDestroyOnLoad(target);
    }

    public sealed class GameObject : Object
    {
        internal readonly List<Component> components = new List<Component>();
        public GameObject(string name)
        {
            this.name = name;
            transform = new Transform(this);
            components.Add(transform);
            activeSelf = true;
            Engine.AddRoot(transform, Engine.Active);
        }
        public Transform transform { get; }
        public bool activeSelf { get; private set; }
        public void SetActive(bool value) { activeSelf = value; }
        public SceneManagement.Scene scene => Engine.SceneOf(transform);
        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            component.Bind(this);
            components.Add(component);
            return component;
        }
        public T[] GetComponents<T>() where T : Component => components.Where(item => item is T && !item.destroyed).Cast<T>().ToArray();
        public void GetComponents<T>(List<T> results) where T : Component { results.AddRange(GetComponents<T>()); }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; private set; }
        public Transform transform => gameObject.transform;
        internal void Bind(GameObject owner) { gameObject = owner; name = owner.name; }
        public T[] GetComponents<T>() where T : Component => gameObject.GetComponents<T>();
        public void GetComponents<T>(List<T> results) where T : Component => gameObject.GetComponents(results);
    }

    public class Behaviour : Component { public bool enabled { get; set; } = true; }

    public class MonoBehaviour : Behaviour { }

    public sealed class Transform : Component
    {
        internal Transform parentNode;
        internal readonly List<Transform> children = new List<Transform>();
        internal int rootScene;
        internal int failSiblingWrites;
        internal Transform(GameObject owner) { Bind(owner); }
        public Transform parent => parentNode;
        public int childCount => children.Count;
        public Transform GetChild(int index) => children[index];
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 lossyScale => localScale;
        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 TransformPoint(Vector3 point) => position + point;
        public Vector3 InverseTransformPoint(Vector3 point) => point - position;
        public void SetParent(Transform parent, bool worldPositionStays) => Engine.SetParent(this, parent);
        public int GetSiblingIndex() => Engine.Container(this).IndexOf(this);
        public void SetSiblingIndex(int index)
        {
            if (failSiblingWrites > 0) { failSiblingWrites--; throw new InvalidOperationException("injected sibling index failure"); }
            var list = Engine.Container(this);
            list.Remove(this);
            list.Insert(Math.Max(0, Math.Min(index, list.Count)), this);
        }
    }

    internal static class Engine
    {
        internal const int Active = 1, Area = 2, Mounts = 3, DontDestroy = 4;
        internal static readonly Dictionary<int, List<Transform>> Roots = new Dictionary<int, List<Transform>>();
        internal static readonly HashSet<int> Unloaded = new HashSet<int>();
        private static readonly List<Object> pending = new List<Object>();

        internal static void Reset()
        {
            Roots.Clear();
            Unloaded.Clear();
            pending.Clear();
            foreach (var handle in new[] { Active, Area, Mounts, DontDestroy }) Roots[handle] = new List<Transform>();
        }

        internal static List<Transform> SceneRoots(int handle) => Roots[handle];

        internal static void AddRoot(Transform transform, int scene)
        {
            transform.parentNode = null;
            transform.rootScene = scene;
            Roots[scene].Add(transform);
        }

        internal static SceneManagement.Scene SceneOf(Transform transform)
        {
            var root = transform;
            while (root.parentNode != null) root = root.parentNode;
            return new SceneManagement.Scene(root.rootScene);
        }

        internal static List<Transform> Container(Transform transform) =>
            transform.parentNode != null ? transform.parentNode.children : Roots[transform.rootScene];

        internal static void SetParent(Transform transform, Transform parent)
        {
            if (transform.destroyed) throw new InvalidOperationException("MissingReferenceException: " + transform.name);
            if (ReferenceEquals(parent, null) ? transform.parentNode == null : ReferenceEquals(parent, transform.parentNode)) return;
            if (!ReferenceEquals(parent, null) && parent.destroyed) throw new InvalidOperationException("Destroyed parent.");
            var scene = SceneOf(transform).handle;
            Container(transform).Remove(transform);
            if (ReferenceEquals(parent, null))
            {
                AddRoot(transform, scene);
                return;
            }
            transform.parentNode = parent;
            parent.children.Add(transform);
        }

        internal static void MoveToScene(GameObject gameObject, SceneManagement.Scene scene)
        {
            var transform = gameObject.transform;
            if (transform.parentNode != null) throw new ArgumentException("Gameobject is not a root in a scene");
            if (transform.rootScene == scene.handle) return;
            Roots[transform.rootScene].Remove(transform);
            AddRoot(transform, scene.handle);
        }

        internal static void DontDestroyOnLoad(Object target)
        {
            var gameObject = target as GameObject ?? (target as Component)?.gameObject;
            if (gameObject.transform.parentNode != null) throw new InvalidOperationException("DontDestroyOnLoad only works for root GameObjects.");
            MoveToScene(gameObject, new SceneManagement.Scene(DontDestroy));
        }

        internal static void Destroy(Object target)
        {
            if (!ReferenceEquals(target, null) && !target.destroyed && !pending.Contains(target)) pending.Add(target);
        }

        internal static void EndOfFrame()
        {
            foreach (var target in pending.ToArray())
            {
                var gameObject = target as GameObject;
                if (gameObject != null)
                {
                    Container(gameObject.transform).Remove(gameObject.transform);
                    DestroyHierarchy(gameObject.transform);
                }
                else
                {
                    target.destroyed = true;
                    ((Component)target).gameObject.components.Remove((Component)target);
                }
            }
            pending.Clear();
        }

        private static void DestroyHierarchy(Transform transform)
        {
            foreach (var child in transform.children.ToArray()) DestroyHierarchy(child);
            transform.gameObject.destroyed = true;
            foreach (var component in transform.gameObject.components) component.destroyed = true;
        }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        private readonly int value;
        internal Scene(int handle) { value = handle; }
        public int handle => value;
        public int rootCount => Engine.SceneRoots(value).Count;
        public bool IsValid() => Engine.Roots.ContainsKey(value);
        public bool isLoaded => IsValid() && !Engine.Unloaded.Contains(value);
        public static bool operator ==(Scene a, Scene b) => a.value == b.value;
        public static bool operator !=(Scene a, Scene b) => a.value != b.value;
        public override bool Equals(object other) => other is Scene && ((Scene)other).value == value;
        public override int GetHashCode() => value;
    }

    public static class SceneManager
    {
        public static void MoveGameObjectToScene(GameObject gameObject, Scene scene) => Engine.MoveToScene(gameObject, scene);
    }
}

namespace KingmakerMountedCombat.Logging
{
    internal interface IModLogger { void Info(string message); void Warning(string message); }
}

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class DiagnosticSettings
    {
        public float RiderOffsetX, RiderOffsetY, RiderOffsetZ, RiderYawDegrees;
    }

    internal static class SixPairAttachmentObservation
    {
        internal static bool IsActive;
        internal static readonly List<string> Rows = new List<string>();
        internal static void Record(string phase, string rider, Transform node, Transform capturedParent, int capturedIndex, Transform anchor)
        {
            Rows.Add(phase + ":" + rider + ":" + capturedIndex + ":" + node.GetSiblingIndex());
        }
    }
}

namespace KingmakerMountedCombat.Integration
{
    using KingmakerMountedCombat.Diagnostics;
    using KingmakerMountedCombat.Logging;

    internal sealed class UnitEntityView : MonoBehaviour { }

    internal sealed class UnitEntityData
    {
        internal UnitEntityData(string id) { UniqueId = id; }
        public string UniqueId { get; }
        public UnitEntityView View { get; set; }
    }

    internal sealed class MountedPair
    {
        internal MountedPair(string rider, string mount) { RiderId = rider; MountId = mount; }
        public string RiderId { get; }
        public string MountId { get; }
    }

    internal sealed class MountedRiderPoseProfile { public string Id = "medium-humanoid-horse-v1"; }

    internal sealed class SupportedMountedProfile
    {
        public string Id = "horse", DisplayName = "Horse", SourceAnchorName = "Spine";
        public bool UsesDiagnosticMammothOffsets, UsesAnimatedSaddle;
        public PoseVector3 SourceAnchorOffset, RiderEulerOffset, MountRootPositionOffset, AnimatedSeatCorrection;
        public MountedRiderPoseProfile RiderPoseProfile = new MountedRiderPoseProfile();
    }

    internal sealed class RiderMovementAgent : MonoBehaviour
    {
        internal int Deconfigures;
        public void Configure(UnitEntityData mount, Transform anchor, Vector3 offset, Vector3 euler) { }
        public void Deconfigure() { Deconfigures++; }
    }

    internal sealed class MountedRiderPoseAdapter : MonoBehaviour
    {
        internal int Deconfigures;
        private bool configured, restored;
        public bool IsConfigured => configured;
        public bool IsHealthy => configured;
        public string LastFailure => null;
        public bool HasPoseResidue => configured;
        public bool BaselineRestoreVerified => !configured && restored;
        public string ProfileId { get; private set; }
        public int BoneCount => 7;
        public void Configure(UnitEntityView view, MountedRiderPoseProfile profile) { configured = true; restored = false; ProfileId = profile.Id; }
        public void ConfigureAnimatedSaddle(Transform root, Transform source, Vector3 correction) { }
        public void Deconfigure() { Deconfigures++; configured = false; restored = true; }
    }

    internal sealed class Logger : IModLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
    }

    internal sealed class KingmakerMountedPairRuntime
    {
        private readonly IModLogger logger;
        private readonly DiagnosticSettings settings;
        private readonly ScopedTransformAttachmentLease<Transform, Vector3, Quaternion, Vector3> riderAttachmentLease;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private SupportedMountedProfile supportedProfile;
        private UnitEntityView riderView;
        private UnitEntityView mountView;
        private object riderStockAgent;
        private bool movementAuthorityConfigured;
        private bool mountAiLeaseOwned;
        private bool avoidanceLeaseOwned;
        private bool riderForbidRotationLeaseOwned;
        private RiderMovementAgent riderOverride;
        private bool overrideComponentOwned;
        private bool overrideInstalled;
        private Transform sourceAnchor;
        private GameObject positionAnchorObject;
        private Transform positionAnchor;
        private MountedRiderPoseAdapter poseAdapter;
        private bool poseComponentOwned;
        private bool poseBaselineRestoreVerified;
        private bool replacementRiderViewReleaseVerified;
        private bool presentationConfigured;

        /* CONSTRUCTOR */

        // Test seam replacing AcquireMovementAuthority's view and override capture.
        internal void Seed(UnitEntityData riderUnit, UnitEntityData mountUnit)
        {
            rider = riderUnit; mount = mountUnit; supportedProfile = new SupportedMountedProfile();
            riderView = rider.View; mountView = mount.View;
            riderOverride = riderView.gameObject.AddComponent<RiderMovementAgent>();
        }

        internal MountedRiderPoseAdapter PoseAdapter => poseAdapter;
        internal bool PoseComponentOwned => poseComponentOwned;
        internal bool HasRiderView => !ReferenceEquals(riderView, null);
        internal bool ReplacementReleased => replacementRiderViewReleaseVerified;
        internal ScopedTransformAttachmentLease<Transform, Vector3, Quaternion, Vector3> Lease => riderAttachmentLease;

        /* HAS_RESIDUE */
        /* NO_RESIDUE */
        /* CUSTODY */
        /* RISK_STATE */
        /* ATTACH */
        /* RESTORE */
        /* OBSERVE */
        /* CLEAR_PREPARED */
        /* RELEASE_REFERENCES */
        /* RELEASE_REPLACEMENT */
        /* REQUIRE_PAIR */
        /* FIND_TRANSFORM */
        /* TO_UNITY */
        /* SIBLING_CONTAINER */
        /* MOVE_TO_CONTAINER */
        /* CREATE_RESERVATION */
        /* RESERVATION_EXISTS */
        /* DESTROY_RESERVATION */
        /* VECTOR_COMPARER */
        /* QUATERNION_COMPARER */
    }
}

namespace KingmakerMountedCombat.Domain
{
    public struct PoseVector3 { public float X, Y, Z; }
}

internal static class Program
{
    private static int pass, fail;

    private static void Check(string name, Func<bool> check)
    {
        try
        {
            if (!check()) throw new Exception("predicate false");
            pass++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception exception)
        {
            fail++;
            Console.WriteLine("FAIL " + name + ": " + exception.Message);
        }
    }

    private sealed class Pair
    {
        internal readonly KingmakerMountedCombat.Integration.UnitEntityData Rider, Mount;
        internal readonly KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime Runtime;
        internal readonly KingmakerMountedCombat.Integration.MountedPair Domain;
        internal Pair(string name, Transform riderParent, int mountScene)
        {
            Rider = Unit(name + "-rider", riderParent, Engine.Area);
            Mount = Unit(name + "-mount", null, mountScene);
            var spine = new GameObject("Spine");
            spine.transform.SetParent(Mount.View.transform, false);
            Runtime = new KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime(new KingmakerMountedCombat.Integration.Logger(),
                new KingmakerMountedCombat.Diagnostics.DiagnosticSettings());
            Domain = new KingmakerMountedCombat.Integration.MountedPair(Rider.UniqueId, Mount.UniqueId);
        }
        // A verified restoration releases the prepared references; every native
        // mount prepares the pair again before attaching its presentation.
        internal void Attach()
        {
            Runtime.Seed(Rider, Mount);
            Runtime.AttachPresentation(Domain);
        }
        internal void Restore() => Runtime.RestorePresentation(Domain);
        internal bool Throws(Action action) { try { action(); return false; } catch (InvalidOperationException) { return true; } }
    }

    private static KingmakerMountedCombat.Integration.UnitEntityData Unit(string id, Transform parent, int scene)
    {
        var gameObject = new GameObject(id);
        if (!ReferenceEquals(parent, null)) gameObject.transform.SetParent(parent, true);
        else if (scene != Engine.Active) SceneManager.MoveGameObjectToScene(gameObject, new Scene(scene));
        gameObject.transform.position = new Vector3(id.Length, 0f, 1f);
        return new KingmakerMountedCombat.Integration.UnitEntityData(id) { View = gameObject.AddComponent<KingmakerMountedCombat.Integration.UnitEntityView>() };
    }

    private static int LiveSlots() =>
        Engine.Roots.Values.SelectMany(roots => roots).Concat(Engine.Roots.Values.SelectMany(roots => roots).SelectMany(Descendants))
            .Count(transform => transform.name == "KMC_RiderSiblingSlot" && !transform.destroyed);

    private static IEnumerable<Transform> Descendants(Transform transform) => transform.children.SelectMany(child => new[] { child }.Concat(Descendants(child)));

    private static bool SameOrder(Transform parent, Transform[] expected) => parent.children.SequenceEqual(expected);

    // Batch187's six native first-acquire ordinals {0,7,5,4,8,9} and ten others.
    private static Transform[] SharedParent(Transform parent, Pair[] pairs)
    {
        foreach (var pair in pairs) pair.Rider.View.transform.SetParent(null, true);
        for (var index = 0; index < 10; index++) new GameObject("native-other-" + index).transform.SetParent(parent, true);
        var ordinals = new[] { 0, 7, 5, 4, 8, 9 };
        for (var index = 5; index >= 0; index--)
        {
            pairs[index].Rider.View.transform.SetParent(parent, true);
            pairs[index].Rider.View.transform.SetSiblingIndex(ordinals[index]);
        }
        return parent.children.ToArray();
    }

    private static Pair[] SixPairs(out Transform parent, out Transform[] original)
    {
        Engine.Reset();
        parent = new GameObject("[cross-area objects]").transform;
        SceneManager.MoveGameObjectToScene(parent.gameObject, new Scene(Engine.Area));
        var root = parent;
        var pairs = Enumerable.Range(0, 6).Select(index => new Pair("p" + index, root, Engine.Area)).ToArray();
        original = SharedParent(parent, pairs);
        return pairs;
    }

    private static int Main()
    {
        Check("actual runtime restores the batch187 six-rider layout exactly over three FIFO cycles", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            for (var cycle = 0; cycle < 3; cycle++)
            {
                foreach (var pair in pairs) pair.Attach();
                if (parent.childCount != 16 || LiveSlots() != 6) return false;
                foreach (var pair in pairs) pair.Restore();
                Engine.EndOfFrame();
                if (!SameOrder(parent, original) || LiveSlots() != 0) return false;
                if (pairs.Any(pair => pair.Runtime.HasPresentationAttachmentResidue || !pair.Runtime.NoPreparedPairResidue)) return false;
            }
            return true;
        });

        Check("actual runtime keeps the deconfigured pose component until the slot verifies, then retries exactly", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            foreach (var pair in pairs) pair.Attach();
            var failing = pairs[1];
            var pose = failing.Runtime.PoseAdapter;
            failing.Rider.View.transform.failSiblingWrites = 1;
            var threw = failing.Throws(failing.Restore);
            Engine.EndOfFrame();
            var retained = threw && failing.Runtime.PoseComponentOwned && pose && !pose.IsConfigured && pose.Deconfigures == 1 &&
                failing.Runtime.Lease.IsAcquired && failing.Runtime.Lease.HoldsSiblingReservation &&
                failing.Runtime.HasPresentationAttachmentResidue && !failing.Runtime.NoPreparedPairResidue &&
                failing.Runtime.PresentationAttachmentRiskState == "presentation teardown retained attachment custody after verified pose baseline restoration";
            foreach (var pair in pairs.Where(value => value != failing)) pair.Restore();
            failing.Restore();
            var retriedSameComponent = pose.Deconfigures == 2;
            Engine.EndOfFrame();
            return retained && retriedSameComponent && !pose && !failing.Runtime.PoseComponentOwned && SameOrder(parent, original) && LiveSlots() == 0 &&
                !failing.Runtime.HasPresentationAttachmentResidue;
        });

        Check("actual runtime retry accepts a pose component destroyed only after verified baseline restoration", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            var pair = pairs[2];
            pair.Attach();
            pair.Rider.View.transform.failSiblingWrites = 1;
            var threw = pair.Throws(pair.Restore);
            UnityEngine.Object.Destroy(pair.Runtime.PoseAdapter);
            Engine.EndOfFrame();
            pair.Restore();
            Engine.EndOfFrame();
            return threw && !pair.Runtime.PoseComponentOwned && SameOrder(parent, original) && LiveSlots() == 0 && !pair.Runtime.HasPresentationAttachmentResidue;
        });

        Check("actual runtime keeps residue when the pose component is destroyed before verification", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            var pair = pairs[3];
            pair.Attach();
            UnityEngine.Object.Destroy(pair.Runtime.PoseAdapter);
            Engine.EndOfFrame();
            var threw = pair.Throws(pair.Restore);
            return threw && pair.Runtime.Lease.IsAcquired && pair.Runtime.Lease.HoldsSiblingReservation &&
                pair.Rider.View.transform.parent != parent && pair.Runtime.HasPresentationAttachmentResidue && pair.Runtime.HasRiderView;
        });

        Check("actual runtime returns a scene-root rider to its exact scene root slot", () =>
        {
            Engine.Reset();
            var first = new GameObject("area-first");
            SceneManager.MoveGameObjectToScene(first, new Scene(Engine.Area));
            var pair = new Pair("root", null, Engine.Mounts);
            var last = new GameObject("area-last");
            SceneManager.MoveGameObjectToScene(last, new Scene(Engine.Area));
            var roots = Engine.SceneRoots(Engine.Area);
            var original = roots.ToArray();
            pair.Attach();
            var held = roots.Count == original.Length && roots[1].name == "KMC_RiderSiblingSlot";
            pair.Restore();
            var exact = roots.SequenceEqual(original) && pair.Rider.View.gameObject.scene == new Scene(Engine.Area);
            var leftScene = Engine.SceneRoots(Engine.DontDestroy).Any(transform => transform.name == "KMC_RiderSiblingSlot");
            Engine.EndOfFrame();
            return held && exact && leftScene && LiveSlots() == 0 && !pair.Runtime.HasPresentationAttachmentResidue;
        });

        Check("actual runtime refuses an unloaded root scene before unparenting the rider", () =>
        {
            Engine.Reset();
            var first = new GameObject("area-first");
            SceneManager.MoveGameObjectToScene(first, new Scene(Engine.Area));
            var pair = new Pair("root", null, Engine.Mounts);
            var roots = Engine.SceneRoots(Engine.Area);
            var original = roots.ToArray();
            pair.Attach();
            var anchor = pair.Rider.View.transform.parent;
            Engine.Unloaded.Add(Engine.Area);
            var refused = pair.Throws(pair.Restore);
            var stayed = pair.Rider.View.transform.parent == anchor && pair.Runtime.Lease.IsAcquired && pair.Runtime.HasPresentationAttachmentResidue;
            Engine.Unloaded.Remove(Engine.Area);
            pair.Restore();
            Engine.EndOfFrame();
            return refused && stayed && roots.SequenceEqual(original) && LiveSlots() == 0 && !pair.Runtime.HasPresentationAttachmentResidue;
        });

        Check("actual runtime releases a native polymorph replacement last and restores the old view exactly", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            var pair = pairs[4];
            pair.Attach();
            var oldView = pair.Rider.View;
            var replacement = new GameObject("polymorph-replacement");
            // Native Polymorph.TryReplaceView parents the replacement under the old
            // view's parent, which is the owned anchor while mounted.
            replacement.transform.SetParent(oldView.transform.parent, false);
            pair.Rider.View = replacement.AddComponent<KingmakerMountedCombat.Integration.UnitEntityView>();
            pair.Restore();
            var expected = original.Concat(new[] { replacement.transform }).ToArray();
            var exact = SameOrder(parent, expected) && pair.Runtime.ReplacementReleased;
            Engine.EndOfFrame();
            return exact && LiveSlots() == 0 && !pair.Runtime.HasPresentationAttachmentResidue;
        });

        Check("actual runtime records ordered six-pair observation boundaries", () =>
        {
            Transform parent; Transform[] original;
            var pairs = SixPairs(out parent, out original);
            KingmakerMountedCombat.Diagnostics.SixPairAttachmentObservation.Rows.Clear();
            KingmakerMountedCombat.Diagnostics.SixPairAttachmentObservation.IsActive = true;
            try
            {
                pairs[0].Attach();
                pairs[0].Restore();
            }
            finally { KingmakerMountedCombat.Diagnostics.SixPairAttachmentObservation.IsActive = false; }
            var phases = KingmakerMountedCombat.Diagnostics.SixPairAttachmentObservation.Rows.Select(row => row.Split(':')[0]).ToArray();
            return phases.SequenceEqual(new[] { "acquire-before", "acquire-after", "restore-before", "restore-after" });
        });

        Console.WriteLine("TOTAL PASS=" + pass + " FAIL=" + fail);
        return fail == 0 ? 0 : 1;
    }
}
