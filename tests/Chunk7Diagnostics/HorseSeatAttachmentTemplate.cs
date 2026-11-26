using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KingmakerMountedCombat.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

// MMR-01 production seat attachment harness (reviews R1-F12, R5-F3, R5-F4). The facade's RiderMovementAgent keeps the rider
// root on the owned position anchor as production's does (InitialConfiguration in Configure, then every frame before the
// adapter's LateUpdate), the Horse Chest may animate per frame, and Unity's Time.deltaTime and the native AnimationManager's
// Disabled flag are facades so paused (time scale 0) and disabled animation can be driven. The ACTUAL KingmakerMountedPairRuntime attach/restore/
// replacement members and the ACTUAL MountedRiderPoseAdapter rig resolution, baseline lease, saddle calibration, frame,
// seat and deconfiguration members are spliced in by scripts/Test-MountedAlpha2SeatAttachment.ps1 and compiled with the
// real Domain sources (ScopedTransformAttachmentLease, ScopedPoseBaselineLease, AnimatedSaddlePosition,
// AnimatedSaddleCalibration with SettledSeatSampler, MountedRiderPoseProfile, PoseVector3) and the actual
// SupportedMountedProfile settled-seat expression. Only Unity itself is a facade: the hierarchy semantics of AttachmentSlotRuntimeTemplate.cs plus
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
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion { yaw = a.yaw + b.yaw };
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

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
        public bool isActiveAndEnabled => enabled && gameObject.activeSelf;
    }

    // Unity's frame delta: 0 while Kingmaker's pause suspends Time.timeScale (TimeController.Suspend).
    public static class Time
    {
        public static float deltaTime = 1f / 60f;
    }

    // The installed Unity enum the adapter compares (Kingmaker sets AlwaysAnimate for every unit view).
    public enum AnimatorCullingMode { AlwaysAnimate = 0, CullUpdateTransforms = 1, CullCompletely = 2 }

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

    // Facade of the native UnitAnimationManager surface the adapter reads, with the installed member shapes: Disabled,
    // enabled/active as a Behaviour, its Animator (enabled, cullingMode), its PlayableGraph (IsValid, IsPlaying),
    // CanRunIdleAction and ActiveAnimations (Handle.IsReleased, Handle.Action.IsAdditive, GetWeight). CanRunIdleAction is
    // modelled on the installed IL pinned by Test-MmrHorseSeatNativeContract.ps1 (Speed <= 0 and no active action other than
    // the locomotion handle, an additive or a released one); the harness drives its inputs, not its result.
    internal sealed class HarnessAnimationManager : MonoBehaviour
    {
        public bool Disabled { get; set; }
        public HarnessAnimator Animator { get; } = new HarnessAnimator();
        public bool GraphPlaying = true;
        public HarnessPlayableGraph PlayableGraph => new HarnessPlayableGraph(GraphPlaying);
        public float Speed;
        public bool ActionActive;
        public bool CanRunIdleAction() => !(Speed > 0f) && !ActionActive;
        public float BlendingOutWeight;
        // PR-3 R1-F1: a newly attached view whose graph plays but whose base animation does not yet contribute (no active animations).
        public bool NoBaseAnimation;
        public List<HarnessAnimation> ActiveAnimations => NoBaseAnimation ? new List<HarnessAnimation>() : BlendingOutWeight > 0f
            ? new List<HarnessAnimation> { HarnessAnimation.Locomotion, new HarnessAnimation(new HarnessHandle(true, false), BlendingOutWeight) }
            : new List<HarnessAnimation> { HarnessAnimation.Locomotion };
    }

    internal sealed class HarnessAnimator
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled;
        public UnityEngine.AnimatorCullingMode cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    }

    internal struct HarnessPlayableGraph
    {
        private readonly bool playing;
        public HarnessPlayableGraph(bool playing) { this.playing = playing; }
        public bool IsValid() => true;
        public bool IsPlaying() => playing;
    }

    internal sealed class HarnessAction { public bool IsAdditive; }

    internal sealed class HarnessHandle
    {
        public HarnessHandle(bool released, bool additive) { IsReleased = released; Action = new HarnessAction { IsAdditive = additive }; }
        public bool IsReleased { get; }
        public HarnessAction Action { get; }
    }

    internal sealed class HarnessAnimation
    {
        public static readonly HarnessAnimation Locomotion = new HarnessAnimation(new HarnessHandle(false, false), 1f);
        private readonly float weight;
        public HarnessAnimation(HarnessHandle handle, float weight) { Handle = handle; this.weight = weight; }
        public HarnessHandle Handle { get; }
        public float GetWeight() => weight;
    }

    internal sealed class UnitEntityView : MonoBehaviour
    {
        public object Animator => this;
        public Character CharacterAvatar { get; set; }
        public HarnessAnimationManager AnimationManager { get; set; }
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
    // settled-seat flag is the ACTUAL SupportedMountedProfile.SettlesSeatFromIdlePose expression.
    internal sealed class SupportedMountedProfile
    {
        public string Id, BlueprintGuid, DisplayName, SourceAnchorName;
        public bool UsesDiagnosticMammothOffsets, UsesAnimatedSaddle;
        public PoseVector3 SourceAnchorOffset, RiderEulerOffset, MountRootPositionOffset, AnimatedSeatCorrection;
        public MountedRiderPoseProfile RiderPoseProfile;
        public DeclaredSmallRiderBody SmallRiderBody;
        /* SETTLED_SEAT */
    }

    internal sealed class SmallRiderRigCompletion : IDisposable
    {
        internal static int Live;
        internal SmallRiderRigCompletion(Character avatar, IModLogger logger) { Live++; }
        public void Dispose() { Live--; }
    }

    // Production RiderMovementAgent.Synchronize writes the rider root to anchor.TransformPoint(offset) at configuration and in
    // its own Update/LateUpdate, before the pose adapter's LateUpdate (execution order 32000).
    internal sealed class RiderMovementAgent : MonoBehaviour
    {
        internal int Deconfigures;
        private Transform anchor;
        private Vector3 anchorOffset;
        public void Configure(UnitEntityData mount, Transform mountAnchor, Vector3 offset, Vector3 euler)
        {
            anchor = mountAnchor; anchorOffset = offset; Sync();
        }
        internal void Sync() { if (anchor != null) transform.position = anchor.TransformPoint(anchorOffset); }
        public void Deconfigure() { Deconfigures++; anchor = null; }
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
        /* ADAPTER_SETTLE */
        /* ADAPTER_NORMALIZED */
        /* ADAPTER_ADVANCE */
        /* ADAPTER_REST */
        /* ADAPTER_NOTIFY */
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

        /* ADAPTER_APPLY_POSE */
        // Harness stub (not actual): the two-bone leg IK needs Unity rotation math; it is counted, in the actual ApplyPose order.
        internal int HarnessLegWrites;
        private void ApplyLeg(LegBinding binding, MountedRiderLegPoseProfile legProfile) { HarnessLegWrites++; }

        // One engine frame: Update before the animator, the animator's own pelvis write, then LateUpdate.
        internal void HarnessFrame(Action animator)
        {
            Update();
            animator();
            LateUpdate();
        }
    }

    // Records what the runtime logs, so the seat lines are read from the actual logging path.
    internal sealed class Logger : IModLogger
    {
        internal static readonly List<string> Lines = new List<string>();
        public void Info(string message) { Lines.Add(message); }
        public void Warning(string message) { Lines.Add(message); }
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
        // Harness: the frame's rider-root synchronization by the movement agent (production order: before the adapter LateUpdate).
        internal void HarnessSync() => riderOverride?.Sync();
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
    // Settled seats are the mean of a stable window: exact for a stationary idle, within 0.25 mm for a breathing one.
    private const float SeatTolerance = 0.00025f;
    private static bool NearSeat(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= SeatTolerance;
    // Stable idle variation (a breathing idle, 0.3 mm), used only where variation itself is under test.
    private const float Breath = 0.0003f;
    private static float Breathe(float height, int frame) => height + Breath * (float)Math.Sin(frame * 0.35);
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
        unit.View.AnimationManager = view.AddComponent<KingmakerMountedCombat.Integration.HarnessAnimationManager>();
        return new Rider { Unit = unit, Pelvis = pelvis.transform };
    }

    private sealed class Mount
    {
        internal KingmakerMountedCombat.Integration.UnitEntityData Unit;
        internal Transform Root;
        internal Transform Chest;
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
        return new Mount { Unit = new KingmakerMountedCombat.Integration.UnitEntityData(id) { View = view.AddComponent<KingmakerMountedCombat.Integration.UnitEntityView>() }, Root = view.transform, Chest = source.transform };
    }

    private static int Adapters(GameObject view) => view.GetComponents<KingmakerMountedCombat.Integration.MountedRiderPoseAdapter>().Length;
    private static int ConfiguredAdapters(GameObject view) =>
        view.GetComponents<KingmakerMountedCombat.Integration.MountedRiderPoseAdapter>().Count(adapter => adapter.IsConfigured);

    // One frame of the full rig: the Horse Chest height (mount-root-local Y), the rider's animated pelvis height, the frame delta
    // (0 = paused: Kingmaker suspends Time.timeScale) and the native state the adapter reads this frame: whether the Animator
    // evaluated (disabled = not), whether an action is active (attack, cast, idle variant), whether the pair moves (animation
    // Speed > 0) and the weight of a released action still blending out.
    private struct RigFrame
    {
        public float Pelvis, ChestY, Delta, Blend;
        public bool Unevaluated, Action, Moving, NoBase;
        public RigFrame(float pelvis, float chestY, float delta) { Pelvis = pelvis; ChestY = chestY; Delta = delta; Blend = 0f; Unevaluated = false; Action = false; Moving = false; NoBase = false; }
    }

    private static void Apply(KingmakerMountedCombat.Integration.HarnessAnimationManager manager, RigFrame frame)
    {
        manager.Animator.enabled = !frame.Unevaluated;
        manager.ActionActive = frame.Action;
        manager.Speed = frame.Moving ? 1f : 0f;
        manager.BlendingOutWeight = frame.Blend;
        manager.NoBaseAnimation = frame.NoBase;
    }

    // The rider view's native animation manager facade: the nearest UnitEntityView above the pelvis.
    private static KingmakerMountedCombat.Integration.HarnessAnimationManager ManagerOf(Transform pelvis)
    {
        for (var node = pelvis; node != null; node = node.parent)
        {
            var views = node.GetComponents<KingmakerMountedCombat.Integration.UnitEntityView>();
            if (views.Length > 0) return views[0].AnimationManager;
        }
        throw new InvalidOperationException("No rider view above the pelvis.");
    }

    private static void Rest(KingmakerMountedCombat.Integration.HarnessAnimationManager manager) => Apply(manager, new RigFrame(0f, 0f, Dt));

    // Engine frames while mounted: the movement agent keeps the rider root on its anchor, then the animator writes its own
    // pelvis pose before the adapter's LateUpdate. Plain heights are resting frames.
    private static Vector3 Frames(KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime runtime, Transform pelvis, IEnumerable<float> animatedHeights) =>
        Frames(runtime, pelvis, animatedHeights.Select(height => new RigFrame(height, 0f, Dt)));

    private static Vector3 Frames(KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime runtime, Transform pelvis, IEnumerable<RigFrame> frames)
    {
        var manager = ManagerOf(pelvis);
        foreach (var frame in frames)
        {
            Time.deltaTime = frame.Delta;
            Apply(manager, frame);
            var f = frame;
            runtime.PoseAdapter.HarnessFrame(() => { runtime.HarnessSync(); pelvis.localPosition = new Vector3(0f, f.Pelvis, 0f); });
        }
        Time.deltaTime = Dt; Rest(manager);
        return pelvis.position;
    }

    private static Vector3 Drive(KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime runtime, Transform pelvis, Mount mount, IEnumerable<RigFrame> frames)
    {
        var manager = ManagerOf(pelvis);
        foreach (var frame in frames)
        {
            Time.deltaTime = frame.Delta;
            Apply(manager, frame);
            mount.Chest.localPosition = new Vector3(ChestLocal.x, frame.ChestY, ChestLocal.z);
            var f = frame;
            runtime.PoseAdapter.HarnessFrame(() => { runtime.HarnessSync(); pelvis.localPosition = new Vector3(0f, f.Pelvis, 0f); });
        }
        Time.deltaTime = Dt; Rest(manager);
        return pelvis.position;
    }

    private const float Dt = 1f / 60f;
    // A stationary resting pose (no motion at all): the positive case the sampler must accept.
    private static IEnumerable<RigFrame> Rig(float pelvis, float chestY, int frames, float delta = Dt) =>
        Enumerable.Range(0, frames).Select(frame => new RigFrame(pelvis, chestY, delta));

    private static IEnumerable<RigFrame> Breathing(float pelvis, float chestY, int frames) =>
        Enumerable.Range(0, frames).Select(frame => new RigFrame(Breathe(pelvis, frame), chestY, Dt));

    // Not evaluated: the game clock advances but the rider's Animator does not evaluate (the pose stays where it was).
    private static IEnumerable<RigFrame> Unevaluated(float pelvis, float chestY, int frames) =>
        Enumerable.Range(0, frames).Select(frame => new RigFrame(pelvis, chestY, Dt) { Unevaluated = true });

    // An evaluated action pose (attack, cast, idle variant), lightly animated around the held height.
    private static IEnumerable<RigFrame> Acting(float pelvis, float chestY, int frames) =>
        Enumerable.Range(0, frames).Select(frame => new RigFrame(Breathe(pelvis, frame), chestY, Dt) { Action = true });

    private static IEnumerable<float> Hold(float height, int frames) => Enumerable.Repeat(height, frames);

    // A Mount Companion cast: an active action animating for the given frames, then its release blending into a stationary idle.
    private static IEnumerable<RigFrame> CastThenIdle(float cast, float idle, int castFrames, int idleFrames, float chestY = 0f)
    {
        if (chestY == 0f) chestY = ChestLocal.y;
        for (var frame = 0; frame < castFrames; frame++) yield return new RigFrame(cast + 0.04f * (float)Math.Sin(frame * 0.7), chestY, Dt) { Action = true };
        for (var frame = 1; frame <= 10; frame++) yield return new RigFrame(cast + (idle - cast) * frame / 10f, chestY, Dt) { Blend = 1f - frame / 10f + 0.05f };
        for (var frame = 0; frame < idleFrames; frame++) yield return new RigFrame(idle, chestY, Dt);
    }

    private const float IdleHeight = 0.95f;

    // The settled seat is the rider's own idle pelvis (its world position in the anchored rider frame) plus the accepted
    // Horse correction: derived from the rider pose fed to the animator, not from any literal basis.
    private static Vector3 ExpectedSettledSeat(Transform pelvis, float idleHeight) =>
        pelvis.parent.position + new Vector3(0f, idleHeight, 0f) + U(MountedRiderPoseProfiles.MediumHumanoidOnHorse.PelvisPositionOffset) +
        U(MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);

    private static KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime NewRuntime() =>
        new KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime(new KingmakerMountedCombat.Integration.Logger(),
            new KingmakerMountedCombat.Diagnostics.DiagnosticSettings());

    private static KingmakerMountedCombat.Integration.MountedPair PairOf(Rider rider, Mount mount) =>
        new KingmakerMountedCombat.Integration.MountedPair(rider.Unit.UniqueId, mount.Unit.UniqueId);

    private static List<string> SeatLines(string riderId) =>
        KingmakerMountedCombat.Integration.Logger.Lines.Where(line => line.StartsWith("Horse seatCalibration:", StringComparison.Ordinal) &&
            line.Contains("rider=" + riderId + ";")).ToList();

    private static int Main()
    {
        Engine.Reset();
        var mountRoot = new Vector3(10f, 0f, 5f);
        var minimumFrames = (int)Math.Ceiling(SettledSeatSampler.MinimumAnimationSeconds / Dt);
        var settleBudget = (int)Math.Ceiling((SettledSeatSampler.MinimumAnimationSeconds + SettledSeatSampler.StableWindowSeconds) / Dt) + 60;

        Check("first mount from an animated cast pose settles on the rider's own idle pose: provisional then settled seat, both logged", () =>
        {
            var mount = NewMount("horse-a", "Chest", mountRoot);
            var cast = NewRider("rider-cast", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.20f);
            var runtime = NewRuntime();
            runtime.Seed(cast.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(cast, mount));
            var provisional = Frames(runtime, cast.Pelvis, Hold(1.20f, 1));
            var settling = runtime.PoseAdapter.SeatCalibrationSettles && !runtime.PoseAdapter.SeatCalibrationSettled;
            var settled = Frames(runtime, cast.Pelvis, CastThenIdle(1.20f, IdleHeight, minimumFrames + 10, settleBudget));
            var expected = ExpectedSettledSeat(cast.Pelvis, IdleHeight);
            var lines = SeatLines("rider-cast");
            Console.WriteLine("  provisional=" + provisional + " settled=" + settled + " expected=" + expected +
                " frames=" + runtime.PoseAdapter.SeatSettleFrames + " lines=" + string.Join(" | ", lines));
            var ok = settling && runtime.PoseAdapter.SeatCalibrationSettled && NearSeat(settled, expected) && provisional.y - settled.y > 0.2f &&
                runtime.PoseAdapter.SeatSettleFrames > minimumFrames + 10 &&
                lines.Count == 2 && lines[0].Contains("phase=attach") && lines[0].Contains("observedSeatFromSource=") &&
                lines[0].Contains("appliedSeatFromSource=") && lines[1].Contains("phase=settled") && lines[1].Contains("frames=" + runtime.PoseAdapter.SeatSettleFrames) &&
                Math.Abs(runtime.PoseAdapter.CalibrationAppliedSeatFromSource.Y - runtime.PoseAdapter.CalibrationObservedSeatFromSource.Y + 0.25f) < 0.0005f;
            runtime.RestorePresentation(PairOf(cast, mount));
            Engine.EndOfFrame();
            return ok;
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
            Frames(runtime, rider.Pelvis, Hold(0.95f, settleBudget));
            var settled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return mounted && settled && Near(rider.Pelvis.localPosition, new Vector3(0f, 0.95f, 0f)) &&
                Near(rider.Unit.View.transform.position, new Vector3(8f, 0f, 3f)) && rider.Unit.View.transform.parent == parent &&
                Adapters(rider.Unit.View.gameObject) == 0 && !runtime.HasPresentationAttachmentResidue && runtime.NoPreparedPairResidue;
        });

        Check("first mount (cast), remount from idle at a different baseline and a mounted load's bind-pose first frame settle on one seat, one adapter", () =>
        {
            var mount = NewMount("horse-c", "Chest", mountRoot);
            var rider = NewRider("rider-c", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.25f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var first = Frames(runtime, rider.Pelvis, CastThenIdle(1.25f, IdleHeight, minimumFrames, settleBudget));
            var firstSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var restoredFirst = Near(rider.Pelvis.localPosition, new Vector3(0f, 1.25f, 0f));
            // Remount from an idle stance with a different pelvis baseline. The facade does not model RiderMovementAgent snapping
            // the rider root to the saddle anchor, so the rider stands where it stood for the first mount (the snap's result).
            rider.Pelvis.localPosition = new Vector3(0f, 0.80f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var remount = Frames(runtime, rider.Pelvis, Hold(IdleHeight, settleBudget));
            var oneAdapter = Adapters(rider.Unit.View.gameObject) == 1 && ConfiguredAdapters(rider.Unit.View.gameObject) == 1 &&
                runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // A mounted load: the view's first evaluated frame is the bind pose (pelvis high), then the animator's idle.
            rider.Pelvis.localPosition = new Vector3(0f, 1.30f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var load = Frames(runtime, rider.Pelvis, new[] { 1.30f }.Concat(Hold(IdleHeight, settleBudget)));
            var loadSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var expected = ExpectedSettledSeat(rider.Pelvis, IdleHeight);
            Console.WriteLine("  first=" + first + " remount=" + remount + " load=" + load + " expected=" + expected + " restoredFirst=" + restoredFirst + " firstSettled=" + firstSettled + " oneAdapter=" + oneAdapter + " loadSettled=" + loadSettled);
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return restoredFirst && firstSettled && oneAdapter && loadSettled && NearSeat(first, remount) && NearSeat(remount, load) && NearSeat(load, expected) &&
                Near(rider.Pelvis.localPosition, new Vector3(0f, 1.30f, 0f)) && Adapters(rider.Unit.View.gameObject) == 0;
        });

        Check("view replacement restores the old view exactly; the new view settles on its own rider-frame idle seat with one configured adapter", () =>
        {
            var mount = NewMount("horse-d", "Chest", mountRoot);
            var rider = NewRider("rider-d", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(7f, 0f, 2f), 0.95f);
            var oldView = rider.Unit.View.gameObject;
            var oldParent = oldView.transform.parent;
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var before = Frames(runtime, rider.Pelvis, Hold(IdleHeight, settleBudget));
            var beforeExpected = ExpectedSettledSeat(rider.Pelvis, IdleHeight);
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
            var after = Frames(runtime, replacement.Pelvis, new[] { 1.05f }.Concat(Hold(IdleHeight, settleBudget)));
            var afterExpected = ExpectedSettledSeat(replacement.Pelvis, IdleHeight);
            Console.WriteLine("  before=" + before + " beforeExpected=" + beforeExpected + " after=" + after + " afterExpected=" + afterExpected);
            var single = ConfiguredAdapters(replacement.Unit.View.gameObject) == 1 && Adapters(replacement.Unit.View.gameObject) == 1 &&
                Adapters(oldView) == 0 && runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // Each view settles on its own rider-frame idle seat (the facade places the replacement at the anchor, the old view
            // where it stood), so each is compared with its own expected settled seat.
            return oldRestored && single && NearSeat(before, beforeExpected) && NearSeat(after, afterExpected) &&
                Near(replacement.Pelvis.localPosition, new Vector3(0f, 1.05f, 0f)) &&
                !runtime.HasPresentationAttachmentResidue;
        });

        Check("a pose that never holds still keeps the provisional attachment seat and logs that it did not settle", () =>
        {
            var mount = NewMount("horse-g", "Chest", mountRoot);
            var rider = NewRider("rider-g", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.10f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var restless = Enumerable.Range(0, (int)Math.Ceiling(SettledSeatSampler.UnsettledNoticeSeconds / Dt) + 5).Select(frame => (frame % 2) == 0 ? 1.10f : 1.11f);
            var seat = Frames(runtime, rider.Pelvis, restless);
            var expectedProvisional = rider.Pelvis.parent.position + new Vector3(0f, 1.10f, 0f) + U(MountedRiderPoseProfiles.MediumHumanoidOnHorse.PelvisPositionOffset) +
                U(MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
            var lines = SeatLines("rider-g");
            var noticed = !runtime.PoseAdapter.SeatCalibrationSettled && Near(seat, expectedProvisional) &&
                lines.Count == 2 && lines[1].Contains("phase=unsettled");
            // Not exhausted: a resting idle after the notice still settles, once.
            var later = Frames(runtime, rider.Pelvis, Hold(IdleHeight, settleBudget));
            lines = SeatLines("rider-g");
            var ok = noticed && runtime.PoseAdapter.SeatCalibrationSettled && NearSeat(later, ExpectedSettledSeat(rider.Pelvis, IdleHeight)) &&
                lines.Count == 3 && lines[2].Contains("phase=settled");
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return ok;
        });

        // R5-F3. The pelvis relative to the Chest once both rigs reach identical idle geometry (Chest at its idle height, rider
        // pelvis at its idle height), in the mount root frame.
        Func<Mount, Rider, Vector3> pelvisFromChest = (m, r) => r.Pelvis.position - m.Chest.position;
        var expectedFromChest = U(MountedRiderPoseProfiles.MediumHumanoidOnHorseMountRootPositionOffset) + new Vector3(0f, IdleHeight, 0f) +
            U(MountedRiderPoseProfiles.MediumHumanoidOnHorse.PelvisPositionOffset) + U(MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
        Check("R5-F3: two attachments with the Chest at different heights, rider root synchronized as production does, settle on the same pelvis relative to the Chest", () =>
        {
            var mount = NewMount("horse-h", "Chest", mountRoot);
            var rider = NewRider("rider-h", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight);
            var runtime = NewRuntime();
            // First attachment: the Horse breathes in (Chest 6 cm high) in the frame Mount Companion fires.
            mount.Chest.localPosition = ChestLocal + new Vector3(0f, 0.06f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var anchorHigh = rider.Unit.View.transform.position;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var high = pelvisFromChest(mount, rider);
            var highSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // Second attachment: the Chest 4 cm low at attachment; afterwards both rigs run the identical idle geometry.
            mount.Chest.localPosition = ChestLocal - new Vector3(0f, 0.04f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            var anchorLow = rider.Unit.View.transform.position;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var low = pelvisFromChest(mount, rider);
            var lowSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var lines = SeatLines("rider-h");
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // The counterexample's raw input: the synchronized rider roots (anchors) differ by the 10 cm Chest displacement, which a
            // raw pelvis-minus-current-Chest sample would have carried into the seat.
            var anchorDifference = anchorHigh.y - anchorLow.y;
            Console.WriteLine("  high=" + high + " low=" + low + " expected=" + expectedFromChest + " anchorDifference=" + anchorDifference +
                " lines=" + string.Join(" | ", lines));
            return highSettled && lowSettled && NearSeat(high, low) && NearSeat(high, expectedFromChest) && Math.Abs(anchorDifference - 0.10f) < 0.0001f &&
                lines.Count == 4 && lines.All(line => line.Contains("anchorFromSource=(0.0000,-0.0800,0.0000)")) &&
                Near(rider.Pelvis.localPosition, new Vector3(0f, IdleHeight, 0f));
        });

        Check("R5-F3: after view replacement the new view, attached with yet another Chest height, settles on the same pelvis relative to the Chest", () =>
        {
            var mount = NewMount("horse-i", "Chest", mountRoot);
            var rider = NewRider("rider-i", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(7f, 0f, 2f), IdleHeight);
            var oldView = rider.Unit.View.gameObject;
            var runtime = NewRuntime();
            mount.Chest.localPosition = ChestLocal + new Vector3(0f, 0.05f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var before = pelvisFromChest(mount, rider);
            var replacement = NewRider("rider-i-replacement", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(0f, 0f, 0f), 1.05f);
            replacement.Unit.View.transform.SetParent(oldView.transform.parent, false);
            rider.Unit.View = replacement.Unit.View;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            mount.Chest.localPosition = ChestLocal - new Vector3(0f, 0.03f, 0f);
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, replacement.Pelvis, mount, new[] { new RigFrame(1.05f, ChestLocal.y, Dt) }.Concat(Rig(IdleHeight, ChestLocal.y, settleBudget)));
            var after = pelvisFromChest(mount, replacement);
            var single = ConfiguredAdapters(replacement.Unit.View.gameObject) == 1 && Adapters(oldView) == 0 && runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            Console.WriteLine("  before=" + before + " after=" + after + " expected=" + expectedFromChest);
            return single && NearSeat(before, expectedFromChest) && NearSeat(after, expectedFromChest) && !runtime.HasPresentationAttachmentResidue;
        });

        Check("R5-F4: a high cast held through 120 paused updates never settles; resumed to idle it settles there and matches the remount control", () =>
        {
            var mount = NewMount("horse-j", "Chest", mountRoot);
            var rider = NewRider("rider-j", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.20f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            // Paused (time scale 0) through 120 render updates with the cast pose held, then 60 more with the animation manager disabled.
            Drive(runtime, rider.Pelvis, mount, Rig(1.20f, ChestLocal.y, 120, 0f));
            rider.Unit.View.AnimationManager.Disabled = true;
            Drive(runtime, rider.Pelvis, mount, Rig(1.20f, ChestLocal.y, 60));
            rider.Unit.View.AnimationManager.Disabled = false;
            var pausedSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var ignored = runtime.PoseAdapter.SeatIgnoredFrames;
            var held = pelvisFromChest(mount, rider);
            // Resumed: the cast animates on, blends, then idle.
            Drive(runtime, rider.Pelvis, mount, CastThenIdle(1.20f, IdleHeight, 60, settleBudget));
            var resumed = pelvisFromChest(mount, rider);
            var resumedSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // Remount control from idle.
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var control = pelvisFromChest(mount, rider);
            var controlSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            Console.WriteLine("  held=" + held + " ignored=" + ignored + " resumed=" + resumed + " control=" + control);
            return !pausedSettled && ignored == 180 && held.y - expectedFromChest.y > 0.2f && resumedSettled && controlSettled &&
                NearSeat(resumed, control) && NearSeat(resumed, expectedFromChest);
        });

        Check("R5-F4: sub-2 mm per-frame pelvis drift does not settle while drifting; once still it settles there and matches the remount control", () =>
        {
            var mount = NewMount("horse-k", "Chest", mountRoot);
            var rider = NewRider("rider-k", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight + 0.36f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            // 1.5 mm per frame for 240 frames (4 s of animation, well past the minimum), sinking to idle.
            var drift = Enumerable.Range(0, 240).Select(frame => new RigFrame(IdleHeight + 0.0015f * (240 - frame), ChestLocal.y, Dt));
            Drive(runtime, rider.Pelvis, mount, drift);
            var driftSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var still = pelvisFromChest(mount, rider);
            var stillSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // PR-3 review R1-F2: compared directly with an actual remount that settles on the stationary idle.
            var remountRuntime = NewRuntime();
            remountRuntime.Seed(rider.Unit, mount.Unit, Horse());
            remountRuntime.AttachPresentation(PairOf(rider, mount));
            Drive(remountRuntime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var remount = pelvisFromChest(mount, rider);
            var remountSettled = remountRuntime.PoseAdapter.SeatCalibrationSettled;
            remountRuntime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            Console.WriteLine("  driftSettled=" + driftSettled + " still=" + still + " remount=" + remount + " expected=" + expectedFromChest);
            return !driftSettled && stillSettled && remountSettled && NearSeat(still, remount) && NearSeat(remount, expectedFromChest);
        });

        // Remount control shared by the cases below: a fresh attachment that settles on the stationary idle.
        Func<Rider, Mount, KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime, Vector3> remountControl = (r, m, rt) =>
        {
            rt.Seed(r.Unit, m.Unit, Horse());
            rt.AttachPresentation(PairOf(r, m));
            Drive(rt, r.Pelvis, m, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var control = pelvisFromChest(m, r);
            var settled = rt.PoseAdapter.SeatCalibrationSettled;
            rt.RestorePresentation(PairOf(r, m));
            Engine.EndOfFrame();
            return settled ? control : new Vector3(float.NaN, float.NaN, float.NaN);
        };
        var longFrames = (int)Math.Ceiling(31.5f / Dt);

        Check("R5-F4: 31.5 s without skeleton evaluation (Animator disabled, game time advancing) at a high pose: no settle, nothing consumed; evaluation and rest return and it settles on idle matching the remount control", () =>
        {
            var mount = NewMount("horse-l", "Chest", mountRoot);
            var rider = NewRider("rider-l", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.20f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Unevaluated(1.20f, ChestLocal.y, longFrames));
            var frozenSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var ignored = runtime.PoseAdapter.SeatIgnoredFrames;
            var held = pelvisFromChest(mount, rider);
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var resumed = pelvisFromChest(mount, rider);
            var resumedSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var lines = SeatLines("rider-l");
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var control = remountControl(rider, mount, runtime);
            Console.WriteLine("  ignored=" + ignored + " held=" + held + " resumed=" + resumed + " control=" + control + " lines=" + string.Join(" | ", lines));
            return !frozenSettled && ignored == longFrames && held.y - expectedFromChest.y > 0.2f && resumedSettled &&
                NearSeat(resumed, control) && NearSeat(resumed, expectedFromChest) && lines.Count == 2 && lines[1].Contains("phase=settled");
        });

        Check("R5-F4: 31.5 s of evaluated cast pose (an action active) logs unsettled once without exhausting calibration; the later resting idle settles and matches the remount control", () =>
        {
            var mount = NewMount("horse-m", "Chest", mountRoot);
            var rider = NewRider("rider-m", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.20f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Acting(1.20f, ChestLocal.y, longFrames));
            var castSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            var ineligible = runtime.PoseAdapter.SeatIneligibleFrames;
            Drive(runtime, rider.Pelvis, mount, CastThenIdle(1.20f, IdleHeight, 0, settleBudget));
            var resumed = pelvisFromChest(mount, rider);
            var lines = SeatLines("rider-m");
            var settled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var control = remountControl(rider, mount, runtime);
            Console.WriteLine("  ineligible=" + ineligible + " resumed=" + resumed + " control=" + control + " lines=" + string.Join(" | ", lines));
            return !castSettled && ineligible == longFrames && settled && NearSeat(resumed, control) &&
                lines.Count == 3 && lines[1].Contains("phase=unsettled") && lines[2].Contains("phase=settled");
        });

        Check("R7-F2: a cast followed by a completely stationary evaluated idle (pelvis fixed) settles on it; first mount equals the remount control; a frozen-high unevaluated pose and a breathing idle are the controls", () =>
        {
            var mount = NewMount("horse-o", "Chest", mountRoot);
            var rider = NewRider("rider-o", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.25f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, CastThenIdle(1.25f, IdleHeight, 90, settleBudget));
            var first = pelvisFromChest(mount, rider);
            var firstSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var control = remountControl(rider, mount, runtime);
            // Negative control: the same high pose never evaluated for the same span does not settle.
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Unevaluated(1.25f, ChestLocal.y, settleBudget));
            var frozenSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // Breathing idle: stable variation settles on its mean.
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Breathing(IdleHeight, ChestLocal.y, settleBudget));
            var breathing = pelvisFromChest(mount, rider);
            var breathingSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            Console.WriteLine("  first=" + first + " control=" + control + " breathing=" + breathing + " frozenSettled=" + frozenSettled);
            return firstSettled && Near(first, control) && Near(first, expectedFromChest) && !frozenSettled && breathingSettled && NearSeat(breathing, expectedFromChest);
        });

        Check("R7-F3: a confirmed idle seat survives two held action poses (1.20 m then 1.10 m, lightly animated, one second each) and the return to idle; it matches the idle remount control", () =>
        {
            var mount = NewMount("horse-p", "Chest", mountRoot);
            var rider = NewRider("rider-p", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var confirmed = runtime.PoseAdapter.CalibrationAppliedSeatFromSource;
            Drive(runtime, rider.Pelvis, mount, Acting(1.20f, ChestLocal.y, 60).Concat(Acting(1.10f, ChestLocal.y, 60)));
            var afterActions = runtime.PoseAdapter.CalibrationAppliedSeatFromSource;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var final = pelvisFromChest(mount, rider);
            var lines = SeatLines("rider-p");
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var control = remountControl(rider, mount, runtime);
            Console.WriteLine("  confirmed=" + U(confirmed) + " afterActions=" + U(afterActions) + " final=" + final + " control=" + control);
            return Near(U(confirmed), U(afterActions)) && Near(final, control) && lines.Count == 2 && lines[1].Contains("phase=settled");
        });

        Check("rest eligibility from native stages: a waiting order (no action, not moving) settles; the pair moving on an unstarted approach and a released action blending out do not, and the seat settles once both end", () =>
        {
            var mount = NewMount("horse-q", "Chest", mountRoot);
            var rider = NewRider("rider-q", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight);
            var runtime = NewRuntime();
            // Waiting: an order waits in its slot while the rider plays no action and the pair stands: rest, settles.
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var waiting = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            // Moving: an approach that has not started its attack already moves the pair (Speed > 0) with a steady pelvis.
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(1.02f, ChestLocal.y, settleBudget).Select(f => { f.Moving = true; return f; }));
            var movingSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            // A released action still blending out with a steady pelvis is a transition, not rest.
            Drive(runtime, rider.Pelvis, mount, Rig(1.08f, ChestLocal.y, settleBudget).Select(f => { f.Blend = 0.4f; return f; }));
            var blendSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var after = pelvisFromChest(mount, rider);
            var settled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            return waiting && !movingSettled && !blendSettled && settled && Near(after, expectedFromChest);
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

        Check("PR-3 R1-F1: a high bind pose held beyond the settle interval with the graph playing, positive game delta and no contributing base animation never settles; the contributing stationary idle then settles and matches a remount", () =>
        {
            var mount = NewMount("horse-b1", "Chest", mountRoot);
            var rider = NewRider("rider-b1", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 1.30f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Drive(runtime, rider.Pelvis, mount, Rig(1.30f, ChestLocal.y, settleBudget * 3).Select(f => { f.NoBase = true; return f; }));
            var heldSettled = runtime.PoseAdapter.SeatCalibrationSettled;
            Drive(runtime, rider.Pelvis, mount, Rig(IdleHeight, ChestLocal.y, settleBudget));
            var idle = pelvisFromChest(mount, rider);
            var settled = runtime.PoseAdapter.SeatCalibrationSettled;
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var control = remountControl(rider, mount, runtime);
            Console.WriteLine("  heldSettled=" + heldSettled + " settled=" + settled + " idle=" + idle + " control=" + control);
            return !heldSettled && settled && NearSeat(idle, control);
        });

        Check("PR-3 R1-F2: the shipping ApplyPose order on first mount, remount and replacement: native pelvis plus the Horse profile offset once, the calibration, the seat as the final pelvis write before the legs; dismount restores the local pelvis", () =>
        {
            var mount = NewMount("horse-b2", "Chest", mountRoot);
            var rider = NewRider("rider-b2", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight);
            var ok = true;
            for (var attach = 0; attach < 3; attach++)
            {
                var runtime = NewRuntime();
                runtime.Seed(rider.Unit, mount.Unit, Horse());
                runtime.AttachPresentation(PairOf(rider, mount));
                var seat = Frames(runtime, rider.Pelvis, Hold(IdleHeight, settleBudget));
                var adapter = runtime.PoseAdapter;
                var expected = ExpectedSettledSeat(rider.Pelvis, IdleHeight);
                var finalWrite = Near(rider.Pelvis.position, adapter.LastAnimatedSeatPosition);
                var legs = adapter.HarnessLegWrites;
                var settledHere = adapter.SeatCalibrationSettled;
                runtime.RestorePresentation(PairOf(rider, mount));
                Engine.EndOfFrame();
                var restored = Near(rider.Pelvis.localPosition, new Vector3(0f, IdleHeight, 0f));
                Console.WriteLine("  attach" + attach + ": seat=" + seat + " expected(offset once)=" + expected + " finalWrite=" + finalWrite + " legWrites=" + legs + " restored=" + restored);
                ok &= settledHere && NearSeat(seat, expected) && finalWrite && legs > 0 && legs % 2 == 0 && restored;
            }
            return ok;
        });

        Check("PR-3 R1-F4: the presentation seat-calibration fields: a settled Medium Horse reports its measured values, a Small fixed seat its nonzero fixed basis, the Mammoth no animated saddle", () =>
        {
            var mount = NewMount("horse-b4", "Chest", mountRoot);
            var rider = NewRider("rider-b4", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), IdleHeight);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mount.Unit, Horse());
            runtime.AttachPresentation(PairOf(rider, mount));
            Frames(runtime, rider.Pelvis, Hold(IdleHeight, settleBudget));
            var horse = KingmakerMountedCombat.Integration.MountedRiderPoseAdapter.FormatSeatCalibration(runtime.PoseAdapter);
            runtime.RestorePresentation(PairOf(rider, mount));
            Engine.EndOfFrame();
            var small = NewMount("small-b4", "Chest", mountRoot);
            var smallRider = NewRider("rider-small-b4", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 0.70f);
            var smallRuntime = NewRuntime();
            smallRuntime.Seed(smallRider.Unit, small.Unit, Small());
            smallRuntime.AttachPresentation(PairOf(smallRider, small));
            Frames(smallRuntime, smallRider.Pelvis, new[] { 0.70f, 0.9f });
            var smallText = KingmakerMountedCombat.Integration.MountedRiderPoseAdapter.FormatSeatCalibration(smallRuntime.PoseAdapter);
            var smallBasis = smallRuntime.PoseAdapter.CalibrationAppliedSeatFromSource;
            smallRuntime.RestorePresentation(PairOf(smallRider, small));
            Engine.EndOfFrame();
            var mammoth = NewMount("mammoth-b4", "Spine", mountRoot);
            var mammothRider = NewRider("rider-m-b4", MountedRiderPoseProfiles.MediumHumanoidOnMammoth, new Vector3(9f, 0f, 4f), 0.95f);
            var mammothRuntime = NewRuntime();
            mammothRuntime.Seed(mammothRider.Unit, mammoth.Unit, Mammoth());
            mammothRuntime.AttachPresentation(PairOf(mammothRider, mammoth));
            Frames(mammothRuntime, mammothRider.Pelvis, Hold(1.17f, 1));
            var mammothText = KingmakerMountedCombat.Integration.MountedRiderPoseAdapter.FormatSeatCalibration(mammothRuntime.PoseAdapter);
            mammothRuntime.RestorePresentation(PairOf(mammothRider, mammoth));
            Engine.EndOfFrame();
            Console.WriteLine("  horse=" + horse + " small=" + smallText + " mammoth=" + mammothText);
            return horse.StartsWith("settled;observedSeatFromSource=", StringComparison.Ordinal) && smallText.StartsWith("fixed;observedSeatFromSource=<not-measured>;appliedSeatFromSource=", StringComparison.Ordinal) &&
                smallBasis.Magnitude > 0.01f && mammothText == "none(no animated saddle)";
        });

        Check("unaffected profiles: the Mammoth has no animated saddle (the pelvis keeps its animation) and a Small profile keeps its fixed-from-source seat", () =>
        {
            var mammoth = NewMount("mammoth-f", "Spine", mountRoot);
            var rider = NewRider("rider-f", MountedRiderPoseProfiles.MediumHumanoidOnMammoth, new Vector3(9f, 0f, 4f), 0.95f);
            var runtime = NewRuntime();
            runtime.Seed(rider.Unit, mammoth.Unit, Mammoth());
            runtime.AttachPresentation(PairOf(rider, mammoth));
            var animated = Frames(runtime, rider.Pelvis, Hold(1.17f, 1));
            // The actual ApplyPose adds the Mammoth profile's pelvis offset to the animated pelvis (no animated saddle, no seat write).
            var mammothPelvis = new Vector3(0f, 1.17f, 0f) + U(MountedRiderPoseProfiles.MediumHumanoidOnMammoth.PelvisPositionOffset);
            Console.WriteLine("  mammoth local=" + rider.Pelvis.localPosition + " expected=" + mammothPelvis + " animated=" + animated);
            var mammothFollows = Near(rider.Pelvis.localPosition, mammothPelvis) && Near(animated, rider.Pelvis.parent.position + mammothPelvis) &&
                !runtime.PoseAdapter.SeatCalibrationSettles && runtime.PoseAdapter.AnimatedSaddleSampleCount == 0 && SeatLines("rider-f").Count == 0;
            runtime.RestorePresentation(PairOf(rider, mammoth));
            Engine.EndOfFrame();
            var small = NewMount("small-f", "Chest", mountRoot);
            var smallRider = NewRider("rider-small", MountedRiderPoseProfiles.MediumHumanoidOnHorse, new Vector3(9f, 0f, 4f), 0.70f);
            var smallRuntime = NewRuntime();
            smallRuntime.Seed(smallRider.Unit, small.Unit, Small());
            smallRuntime.AttachPresentation(PairOf(smallRider, small));
            var smallSeat = Frames(smallRuntime, smallRider.Pelvis, new[] { 0.70f, 0.9f });
            Console.WriteLine("  smallSeat=" + smallSeat + " expected=" + (small.Root.position + ChestLocal + U(SmallCorrection)) + " live=" + KingmakerMountedCombat.Integration.SmallRiderRigCompletion.Live);
            var smallFixed = !smallRuntime.PoseAdapter.SeatCalibrationSettles &&
                KingmakerMountedCombat.Integration.SmallRiderRigCompletion.Live == 1 &&
                Near(smallSeat, small.Root.position + ChestLocal + U(SmallCorrection)) && SeatLines("rider-small").Count == 0;
            smallRuntime.RestorePresentation(PairOf(smallRider, small));
            Engine.EndOfFrame();
            return mammothFollows && smallFixed && KingmakerMountedCombat.Integration.SmallRiderRigCompletion.Live == 0 &&
                Adapters(smallRider.Unit.View.gameObject) == 0;
        });

        Console.WriteLine("TOTAL PASS=" + pass + " FAIL=" + fail);
        return fail == 0 ? 0 : 1;
    }
}
