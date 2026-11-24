using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KingmakerMountedCombat.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

// MMR-01 production seat attachment harness (review R1-F12). The ACTUAL KingmakerMountedPairRuntime attach/restore/
// replacement members and the ACTUAL MountedRiderPoseAdapter rig resolution, baseline lease, saddle calibration, frame,
// seat and deconfiguration members are spliced in by scripts/Test-MountedAlpha2SeatAttachment.ps1 and compiled with the
// real Domain sources (ScopedTransformAttachmentLease, ScopedPoseBaselineLease, AnimatedSaddlePosition,
// AnimatedSaddleCalibration, MountedRiderPoseProfile, PoseVector3) and the actual SupportedMountedProfile declared-seat
// expression. Only Unity itself is a facade: the hierarchy semantics of AttachmentSlotRuntimeTemplate.cs plus
// translation-only world positions. The one adapter member that is not actual is ApplyPose (its pelvis profile offsets and
// leg IK need Unity rotation math); its stub performs only the actual animated-seat write. Offline evidence only: it
// proves the attachment wiring and seat arithmetic, never the rendered pose.

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
        // Translation-only hierarchy (MMR-01 seat harness): a world position is the parent's world position plus the local
        // position, as Unity's for unrotated, unscaled parents. Rotation and scale stay per node, unchained.
        private Vector3 local;
        public Vector3 localPosition { get { return local; } set { local = value; } }
        public Vector3 position
        {
            get { return parentNode == null ? local : parentNode.position + local; }
            set { local = parentNode == null ? value : value - parentNode.position; }
        }
        public Quaternion rotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 lossyScale => localScale;
        public Quaternion localRotation { get; set; }
        public Vector3 TransformPoint(Vector3 point) => position + point;
        public Vector3 InverseTransformPoint(Vector3 point) => point - position;
        public bool IsChildOf(Transform parent)
        {
            for (var node = this; node != null; node = node.parentNode) if (ReferenceEquals(node, parent)) return true;
            return false;
        }
        public void SetParent(Transform parent, bool worldPositionStays)
        {
            var world = position;
            Engine.SetParent(this, parent);
            if (worldPositionStays) position = world;
        }
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
        internal static void Record(string phase, string rider, Transform node, Transform capturedParent, int capturedIndex, Transform anchor) { }
    }
}

namespace KingmakerMountedCombat.Integration
{
    using KingmakerMountedCombat.Diagnostics;
    using KingmakerMountedCombat.Logging;

    // Facade of the native humanoid surface the adapter reads: a live Animator and the CharacterAvatar under which the rig is.
    internal sealed class Character : MonoBehaviour { }

    internal sealed class UnitEntityView : MonoBehaviour
    {
        public object Animator => this;
        public Character CharacterAvatar { get; set; }
    }

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

    internal sealed class DeclaredSmallRiderBody { }

    internal static class SupportedMountedProfiles
    {
        /* HORSE_GUID */
    }

    // The fields AttachPresentation reads, with the production Horse/Mammoth/Small values supplied by the program; the
    // declared seat is the ACTUAL SupportedMountedProfile.DeclaredSeatFromSource expression.
    internal sealed class SupportedMountedProfile
    {
        public string Id, BlueprintGuid, DisplayName, SourceAnchorName;
        public bool UsesDiagnosticMammothOffsets, UsesAnimatedSaddle;
        public PoseVector3 SourceAnchorOffset, RiderEulerOffset, MountRootPositionOffset, AnimatedSeatCorrection;
        public MountedRiderPoseProfile RiderPoseProfile;
        public DeclaredSmallRiderBody SmallRiderBody;
        /* DECLARED_SEAT */
    }

    internal sealed class SmallRiderRigCompletion : IDisposable
    {
        internal static int Live;
        internal SmallRiderRigCompletion(Character avatar, IModLogger logger) { Live++; }
        public void Dispose() { Live--; }
    }

    internal sealed class RiderMovementAgent : MonoBehaviour
    {
        internal int Deconfigures;
        public void Configure(UnitEntityData mount, Transform anchor, Vector3 offset, Vector3 euler) { }
        public void Deconfigure() { Deconfigures++; }
    }

    internal sealed class MountedRiderPoseAdapter : MonoBehaviour
    {
        /* ADAPTER_STATE */
        /* ADAPTER_RESTORE_STATE */
        /* ADAPTER_RESIDUE */
        /* ADAPTER_CONFIGURE */
        /* ADAPTER_PRIME */
        /* ADAPTER_SADDLE */
        /* ADAPTER_CALIBRATION */
        /* ADAPTER_SCALE */
        /* ADAPTER_DECONFIGURE */
        /* ADAPTER_UPDATE */
        /* ADAPTER_LATE_UPDATE */
        /* ADAPTER_SEAT */
        /* ADAPTER_VALIDATE */
        /* ADAPTER_LATCH */
        /* ADAPTER_RESOLVE */
        /* ADAPTER_RESOLVE_LEG */
        /* ADAPTER_FIND_UNIQUE */
        /* ADAPTER_FIND */
        /* ADAPTER_TO_POSE */
        /* ADAPTER_TO_UNITY */
        /* ADAPTER_TICKS */
        /* ADAPTER_CREATE_LEASE */
        /* ADAPTER_LEG_BINDING */
        /* ADAPTER_VECTOR_COMPARER */
        /* ADAPTER_QUATERNION_COMPARER */

        // Harness stub (not actual): see the file header.
        private void ApplyPose()
        {
            if (animatedSaddleRequired) ApplyAnimatedSeat();
        }

        // One engine frame: Update before the animator, the animator's own pelvis write, then LateUpdate.
        internal void HarnessFrame(Action animator)
        {
            Update();
            animator();
            LateUpdate();
        }
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
        private SmallRiderRigCompletion smallRiderRig;

        /* CONSTRUCTOR */

        // Test seam replacing AcquireMovementAuthority's view and override capture (as the slot harness does).
        internal void Seed(UnitEntityData riderUnit, UnitEntityData mountUnit, SupportedMountedProfile profile)
        {
            rider = riderUnit; mount = mountUnit; supportedProfile = profile;
            riderView = rider.View; mountView = mount.View;
            riderOverride = riderView.gameObject.AddComponent<RiderMovementAgent>();
        }

        internal MountedRiderPoseAdapter PoseAdapter => poseAdapter;
        internal bool PoseComponentOwned => poseComponentOwned;
        internal bool ReplacementReleased => replacementRiderViewReleaseVerified;

        /* HAS_RESIDUE */
        /* NO_RESIDUE */
        /* CUSTODY */
        /* ATTACH */
        /* RESTORE */
        /* OBSERVE */
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

internal static class Program
{
    private static int pass, fail;
    private const float Tolerance = 0.00001f;

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
            Console.WriteLine("FAIL " + name + ": " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= Tolerance;
    private static Vector3 U(PoseVector3 value) => new Vector3(value.X, value.Y, value.Z);

    // Production profile values (SupportedMountedProfiles.Horse/Mammoth and a declared Small profile); the compiled values
    // themselves are checked by Test-MountedAlpha2NativeContract.ps1.
    private static KingmakerMountedCombat.Integration.SupportedMountedProfile Horse() => new KingmakerMountedCombat.Integration.SupportedMountedProfile
    {
        Id = "medium-humanoid-horse-v1", BlueprintGuid = KingmakerMountedCombat.Integration.SupportedMountedProfiles.HorseBlueprintGuid,
        DisplayName = "Horse", SourceAnchorName = "Chest",
        MountRootPositionOffset = MountedRiderPoseProfiles.MediumHumanoidOnHorseMountRootPositionOffset,
        UsesAnimatedSaddle = true, AnimatedSeatCorrection = MountedRiderPoseProfiles.HorseAnimatedSeatCorrection,
        RiderPoseProfile = MountedRiderPoseProfiles.MediumHumanoidOnHorse
    };

    private static KingmakerMountedCombat.Integration.SupportedMountedProfile Mammoth() => new KingmakerMountedCombat.Integration.SupportedMountedProfile
    {
        Id = "medium-humanoid-mammoth-v1", BlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9", DisplayName = "Mammoth",
        SourceAnchorName = "Spine", MountRootPositionOffset = MountedRiderPoseProfiles.MediumHumanoidOnMammothMountRootPositionOffset,
        UsesDiagnosticMammothOffsets = true, UsesAnimatedSaddle = false, RiderPoseProfile = MountedRiderPoseProfiles.MediumHumanoidOnMammoth
    };

    private static readonly PoseVector3 SmallCorrection = new PoseVector3(0f, -0.05f, 0.02f);

    private static KingmakerMountedCombat.Integration.SupportedMountedProfile Small() => new KingmakerMountedCombat.Integration.SupportedMountedProfile
    {
        Id = "small-harness-v1", BlueprintGuid = "small-harness-mount", DisplayName = "Small", SourceAnchorName = "Chest",
        UsesAnimatedSaddle = true, AnimatedSeatCorrection = SmallCorrection,
        RiderPoseProfile = MountedRiderPoseProfiles.MediumHumanoidOnHorse,
        SmallRiderBody = new KingmakerMountedCombat.Integration.DeclaredSmallRiderBody()
    };

    private sealed class Rider
    {
        internal KingmakerMountedCombat.Integration.UnitEntityData Unit;
        internal Transform Pelvis;
    }

    // A rider view with its native humanoid surface: CharacterAvatar > pelvis > thigh > lower leg > foot per side, named by
    // the production pose profile (the adapter's actual rig resolution finds them).
    private static Rider NewRider(string id, MountedRiderPoseProfile profile, Vector3 root, float pelvisHeight)
    {
        var view = new GameObject(id);
        SceneManager.MoveGameObjectToScene(view, new Scene(Engine.Area));
        view.transform.position = root;
        var avatar = new GameObject(id + "-avatar");
        avatar.transform.SetParent(view.transform, false);
        var pelvis = new GameObject(profile.PelvisBoneName);
        pelvis.transform.SetParent(avatar.transform, false);
        pelvis.transform.localPosition = new Vector3(0f, pelvisHeight, 0f);
        foreach (var leg in new[] { profile.LeftLeg, profile.RightLeg })
        {
            var thigh = new GameObject(leg.ThighBoneName); thigh.transform.SetParent(pelvis.transform, false);
            thigh.transform.localPosition = new Vector3(0.1f, -0.1f, 0f);
            var lower = new GameObject(leg.LowerLegBoneName); lower.transform.SetParent(thigh.transform, false);
            lower.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            var foot = new GameObject(leg.FootBoneName); foot.transform.SetParent(lower.transform, false);
            foot.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        }
        var unit = new KingmakerMountedCombat.Integration.UnitEntityData(id) { View = view.AddComponent<KingmakerMountedCombat.Integration.UnitEntityView>() };
        unit.View.CharacterAvatar = avatar.AddComponent<KingmakerMountedCombat.Integration.Character>();
        return new Rider { Unit = unit, Pelvis = pelvis.transform };
    }

    private sealed class Mount
    {
        internal KingmakerMountedCombat.Integration.UnitEntityData Unit;
        internal Transform Root;
    }

    private static readonly Vector3 ChestLocal = new Vector3(0f, 1.5024f, 0.4674f);

    private static Mount NewMount(string id, string sourceName, Vector3 root)
    {
        var view = new GameObject(id);
        SceneManager.MoveGameObjectToScene(view, new Scene(Engine.Mounts));
        view.transform.position = root;
        var source = new GameObject(sourceName);
        source.transform.SetParent(view.transform, false);
        source.transform.localPosition = ChestLocal;
        return new Mount { Unit = new KingmakerMountedCombat.Integration.UnitEntityData(id) { View = view.AddComponent<KingmakerMountedCombat.Integration.UnitEntityView>() }, Root = view.transform };
    }

    private static int Adapters(GameObject view) => view.GetComponents<KingmakerMountedCombat.Integration.MountedRiderPoseAdapter>().Length;
    private static int ConfiguredAdapters(GameObject view) =>
        view.GetComponents<KingmakerMountedCombat.Integration.MountedRiderPoseAdapter>().Count(adapter => adapter.IsConfigured);

    // Engine frames while mounted: the animator writes its own pelvis pose (cast, walk, idle) before LateUpdate.
    private static Vector3 Frames(KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime runtime, Transform pelvis, params float[] animatedHeights)
    {
        foreach (var height in animatedHeights)
            runtime.PoseAdapter.HarnessFrame(() => pelvis.localPosition = new Vector3(0f, height, 0f));
        return pelvis.position;
    }

    private static Vector3 ExpectedHorseSeat(Mount mount) =>
        mount.Root.position + ChestLocal + U(AnimatedSaddleCalibration.MediumHumanoidHorseSeatFromSource) +
        U(MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);

    private static KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime NewRuntime() =>
        new KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime(new KingmakerMountedCombat.Integration.Logger(),
            new KingmakerMountedCombat.Diagnostics.DiagnosticSettings());

    private static KingmakerMountedCombat.Integration.MountedPair PairOf(Rider rider, Mount mount) =>
        new KingmakerMountedCombat.Integration.MountedPair(rider.Unit.UniqueId, mount.Unit.UniqueId);

    private static int Main()
    {
        Engine.Reset();
        var mountRoot = new Vector3(10f, 0f, 5f);

        Check("first mount from a cast pose and from an idle pose: the adapter receives the declared Horse basis and both seat identically", () =>
        {
            var mount = NewMount("horse-a", "Chest", mountRoot);
            var cast = NewRider("rider-cast", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.20f);
            var idle = NewRider("rider-idle", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(11f, 0f, 6f), 0.95f);
            var castRuntime = NewRuntime();
            castRuntime.Seed(cast.Unit, mount.Unit, Horse());
            castRuntime.AttachPresentation(PairOf(cast, mount));
            var castSeat = Frames(castRuntime, cast.Pelvis, 1.20f, 1.31f, 1.05f);
            var castObserved = castRuntime.PoseAdapter.CalibrationObservedSeatFromSource;
            var castDeclared = castRuntime.PoseAdapter.CalibrationUsedDeclaredSeat;
            castRuntime.RestorePresentation(PairOf(cast, mount));
            Engine.EndOfFrame();
            var idleRuntime = NewRuntime();
            idleRuntime.Seed(idle.Unit, mount.Unit, Horse());
            idleRuntime.AttachPresentation(PairOf(idle, mount));
            var idleSeat = Frames(idleRuntime, idle.Pelvis, 0.95f, 0.97f, 0.93f);
            var idleObserved = idleRuntime.PoseAdapter.CalibrationObservedSeatFromSource;
            var idleDeclared = idleRuntime.PoseAdapter.CalibrationUsedDeclaredSeat;
            Console.WriteLine("  castSeat=" + castSeat + " idleSeat=" + idleSeat + " expected=" + ExpectedHorseSeat(mount) +
                " observedSeatFromSource.Y cast=" + castObserved.Y + " idle=" + idleObserved.Y);
            idleRuntime.RestorePresentation(PairOf(idle, mount));
            Engine.EndOfFrame();
            return castDeclared && idleDeclared && Near(castSeat, idleSeat) && Near(castSeat, ExpectedHorseSeat(mount)) &&
                Math.Abs(castObserved.Y - idleObserved.Y) > 0.2f;
        });

        Check("dismount through the runtime restores the exact pelvis baseline and rider placement and destroys the adapter", () =>
        {
            var mount = NewMount("horse-b", "Chest", mountRoot);
            var rider = NewRider("rider-b", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(8f, 0f, 3f), 0.95f);
            var parent = rider.Unit.View.transform.parent;
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var mounted = Adapters(rider.Unit.View.gameObject) == 1 && runtime.PoseAdapter.IsHealthy &&
                rider.Unit.View.transform.parent != parent;
            Frames(runtime, rider.Pelvis, 1.1f, 0.9f);
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return mounted && Near(rider.Pelvis.localPosition, new Vector3(0f, 0.95f, 0f)) &&
                Near(rider.Unit.View.transform.position, new Vector3(8f, 0f, 3f)) && rider.Unit.View.transform.parent == parent &&
                Adapters(rider.Unit.View.gameObject) == 0 && !runtime.HasPresentationAttachmentResidue && runtime.NoPreparedPairResidue;
        });

        Check("remount at the same place from a different baseline seats exactly as the first mount, with one adapter", () =>
        {
            var mount = NewMount("horse-c", "Chest", mountRoot);
            var rider = NewRider("rider-c", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.25f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var first = Frames(runtime, rider.Pelvis, 1.25f, 1.25f);
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var restoredFirst = Near(rider.Pelvis.localPosition, new Vector3(0f, 1.25f, 0f));
            rider.Pelvis.localPosition = new Vector3(0f, 0.80f, 0f);
            rider.Unit.View.transform.position = new Vector3(12f, 0f, 7f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var second = Frames(runtime, rider.Pelvis, 0.80f, 0.86f);
            var oneAdapter = Adapters(rider.Unit.View.gameObject) == 1 && ConfiguredAdapters(rider.Unit.View.gameObject) == 1;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return restoredFirst && oneAdapter && Near(first, second) && Near(second, ExpectedHorseSeat(mount)) &&
                Near(rider.Pelvis.localPosition, new Vector3(0f, 0.80f, 0f)) && Adapters(rider.Unit.View.gameObject) == 0;
        });

        Check("view replacement restores the old view exactly; remounting the new view leaves one configured adapter and the same seat", () =>
        {
            var mount = NewMount("horse-d", "Chest", mountRoot);
            var rider = NewRider("rider-d", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(7f, 0f, 2f), 0.95f);
            var oldView = rider.Unit.View.gameObject;
            var oldParent = oldView.transform.parent;
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var before = Frames(runtime, rider.Pelvis, 1.0f, 1.1f);
            // Native Polymorph.TryReplaceView parents the replacement under the old view's parent: the owned anchor.
            var replacement = NewRider("rider-d-replacement", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(0f, 0f, 0f), 1.05f);
            replacement.Unit.View.transform.SetParent(oldView.transform.parent, false);
            var replacementWorld = replacement.Unit.View.transform.position;
            rider.Unit.View = replacement.Unit.View;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var oldRestored = oldView.transform.parent == oldParent && Near(oldView.transform.position, new Vector3(7f, 0f, 2f)) &&
                Near(rider.Pelvis.localPosition, new Vector3(0f, 0.95f, 0f)) && Adapters(oldView) == 0 && runtime.ReplacementReleased &&
                replacement.Unit.View.transform.parent == oldParent && Near(replacement.Unit.View.transform.position, replacementWorld);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var after = Frames(runtime, replacement.Pelvis, 1.05f, 0.9f);
            var single = ConfiguredAdapters(replacement.Unit.View.gameObject) == 1 && Adapters(replacement.Unit.View.gameObject) == 1 &&
                Adapters(oldView) == 0 && runtime.PoseAdapter.CalibrationUsedDeclaredSeat;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return oldRestored && single && Near(before, after) && Near(after, ExpectedHorseSeat(mount)) &&
                Near(replacement.Pelvis.localPosition, new Vector3(0f, 1.05f, 0f)) && !runtime.HasPresentationAttachmentResidue;
        });

        Check("the actual adapter refuses a second configuration of the same rider view while mounted", () =>
        {
            var mount = NewMount("horse-e", "Chest", mountRoot);
            var rider = NewRider("rider-e", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 0.95f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var refused = false;
            try { runtime.PoseAdapter.Configure(rider.Unit.View, MountedRiderPoseProfiles.MediumHumanoidOnHorse); }
            catch (InvalidOperationException) { refused = true; }
            var stillOne = Adapters(rider.Unit.View.gameObject) == 1 && runtime.PoseAdapter.IsHealthy;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return refused && stillOne && Adapters(rider.Unit.View.gameObject) == 0;
        });

        Check("unaffected profiles: the Mammoth has no animated saddle (the pelvis keeps its animation) and a Small profile keeps its fixed-from-source seat", () =>
        {
            var mammoth = NewMount("mammoth-f", "Spine", mountRoot);
            var rider = NewRider("rider-f", MountedRiderPoseProfiles.MediumHumanoidOnMammoth, new Vector3(9f, 0f, 4f), 0.95f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mammoth.Unit, Mammoth());
            runtime.AttachPresentation(PairOf(rider, mammoth));
            var animated = Frames(runtime, rider.Pelvis, 1.17f);
            var mammothFollows = Near(rider.Pelvis.localPosition, new Vector3(0f, 1.17f, 0f)) && Near(animated, rider.Pelvis.parent.position + new Vector3(0f, 1.17f, 0f)) &&
                !runtime.PoseAdapter.CalibrationUsedDeclaredSeat && runtime.PoseAdapter.AnimatedSaddleSampleCount == 0;
            runtime.RestorePresentation(PairOf(rider, mammoth));
            Engine.EndOfFrame();
            var small = NewMount("small-f", "Chest", mountRoot);
            var smallRider = NewRider("rider-small", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 0.70f);
            var smallRuntime = NewRuntime();
            smallRuntime.Seed(smallRider.Unit, small.Unit, Small());
            smallRuntime.AttachPresentation(PairOf(smallRider, small));
            var smallSeat = Frames(smallRuntime, smallRider.Pelvis, 0.70f, 0.9f);
            var smallFixed = !smallRuntime.PoseAdapter.CalibrationUsedDeclaredSeat &&
                KingmakerMountedCombat.Integration.SmallRiderRigCompletion.Live == 1 &&
                Near(smallSeat, small.Root.position + ChestLocal + U(SmallCorrection));
            smallRuntime.RestorePresentation(PairOf(smallRider, small));
            Engine.EndOfFrame();
            return mammothFollows && smallFixed && KingmakerMountedCombat.Integration.SmallRiderRigCompletion.Live == 0 &&
                Adapters(smallRider.Unit.View.gameObject) == 0;
        });

        Console.WriteLine("TOTAL PASS=" + pass + " FAIL=" + fail);
        return fail == 0 ? 0 : 1;
    }
}
