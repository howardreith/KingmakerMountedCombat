using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using UnityEngine;

// Production-path harness for the mounted pair attack's approach and start (reviews R3-F1, R4-F2, R4-F1, R5-F2, R6-F1, R6-F2). The marked members
// below are replaced by Test-MountedAlpha2AttackPathHarness.ps1 with the ACTUAL source text of MountedPairAttackCommand,
// MountedPairSingleAttack, MountedPatchController.MountedAttackApproachPrefix, PairedAttackClickProbe.Evaluate and
// MountedCombatController.RevalidateAdmittedClickRoute, compiled with every real Domain source. This file supplies only a
// facade of the native surface those members touch: positions (mechanics distance on X/Z), corpulence, weapon range, a line
// of sight blocker that is consulted exactly when native consults it (NeedLoS), unit command slots and queue with native
// promotion, a native command tick (approach while unstarted, start only when ready and close, tick when started), and a
// native-route stand-in, native's point-command proximity rule for moves (within ApproachRadius of ApproachPoint the move
// completes without moving; otherwise it follows its ForcedPath) and a rider that is carried at its mount-local offset (the
// offset turns with the mount's heading). Offline evidence only: nativeProof=false.
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 right => new Vector3(1f, 0f, 0f);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
        public static float Distance(Vector3 a, Vector3 b) { var d = a - b; return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z); }
        // PR #70 R14-F1 (round 15): Unity's Vector3.Lerp clamps its parameter to [0, 1] (the facade no longer extrapolates).
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = t < 0f ? 0f : t > 1f ? 1f : t; return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t); }
        public override string ToString() => "(" + x.ToString("0.###") + "," + z.ToString("0.###") + ")";
    }

    public static class Time { public static int frameCount; }
    // PR #70 R12-F4 (round 16): the Unity members KingmakerMountedPairRuntime.RestoreMovementAuthority reads (yaw rotation, Vector3.right, Destroy).
    public struct Quaternion
    {
        private float yaw;
        public static Quaternion Euler(float x, float y, float z) => new Quaternion { yaw = y };
        public static Vector3 operator *(Quaternion q, Vector3 v) { var r = q.yaw * Math.PI / 180.0; return new Vector3((float)(v.x * Math.Cos(r) + v.z * Math.Sin(r)), v.y, (float)(-v.x * Math.Sin(r) + v.z * Math.Cos(r))); }
    }
    public class Object { public static int DestroyCount; public static void Destroy(object target) { DestroyCount++; } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }

    // PR #70 R5-F2 (round 6): a unit view's GameObject, the identity native ClickUnitHandler.OnClick receives (engine boundary).
    public sealed class GameObject { }
}

namespace Kingmaker.Controllers.Clicks
{
    // Native IClickEventHandler.OnClick(GameObject, Vector3, int button, bool simulate, bool muteEvents) (engine boundary).
    public interface IClickEventHandler { bool OnClick(UnityEngine.GameObject gameObject, UnityEngine.Vector3 worldPosition, int button, bool simulate, bool muteEvents); }
}

// Native PathCompleteState (Assembly-CSharp-firstpass, global namespace): the outcome of a path request.
public enum PathCompleteState { NotCalculated, Error, Complete, Partial }

namespace Pathfinding
{
    public class Path
    {
        public List<Vector3> vectorPath = new List<Vector3>();
        public bool error;
        public PathCompleteState CompleteState = PathCompleteState.Complete;
        public bool IsDone() => true;
    }

    public class ForcedPath : Path
    {
        public ForcedPath(List<Vector3> points) { vectorPath = new List<Vector3>(points); }
    }

    public delegate void OnPathDelegate(Path path);
}

public static class AstarPath
{
    // PR #70 R12-F4 (round 16): no active A* graph offline (RestoreMovementAuthority then places the rider beside the mount without FreePlaceSelector).
    public static object active;
    public static int WaitCount;
    public static void WaitForPath(Pathfinding.Path path) { WaitCount++; }
}

namespace Kingmaker.Visual.FogOfWar
{
    public sealed class LineOfSightGeometry
    {
        public static LineOfSightGeometry Instance = new LineOfSightGeometry();
        public Func<Vector3, Vector3, bool> Blocked = (from, to) => false;
        public int Queries;
        public bool HasObstacle(Vector3 from, Vector3 to, int ignoreBlockerId) { Queries++; return Blocked(from, to); }
    }
}

namespace Kingmaker.Utility
{
    public static class FreePlaceSelector
    {
        public static void PlaceSpawnPlaces(int count, float radius, Vector3 near) { throw new InvalidOperationException("no A* graph offline"); }
        public static Vector3 GetRelaxedPosition(int index, bool projectOnGround) { throw new InvalidOperationException("no A* graph offline"); }
    }
    public static class GeometryUtils
    {
        public static float SqrMechanicsDistance(Vector3 a, Vector3 b) { var dx = a.x - b.x; var dz = a.z - b.z; return dx * dx + dz * dz; }
        public static float MechanicsDistance(Vector3 a, Vector3 b) => (float)Math.Sqrt(SqrMechanicsDistance(a, b));
    }
}

namespace TurnBased.Controllers
{
    public static class CombatController
    {
        public static bool TurnBased;
        public static bool IsInTurnBasedCombat() => TurnBased;
        // The native turn-candidate record the initiative loop hands to the pair's suppression (engine boundary).
        public sealed class TBUnitInfo { public Kingmaker.EntitySystem.Entities.UnitEntityData Unit; }
    }

    public sealed class TurnController
    {
        public enum TurnStatus { Preparing, Acting, Ended }
        public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;
        public TurnStatus Status = TurnStatus.Preparing;
        // PR #70 R5-F3 (round 7): installed TurnController.get_IsActing (0x06000C24) is `Status == Acting`; it is never set separately.
        public bool IsActing => Status == TurnStatus.Acting;
        // PR #70 R1-F6 (round 7): installed TurnController.Tick (0x06000C34), its Preparing branch: Status becomes Acting when IsActed() (time moved
        // or a Standard/Move/Swift cooldown on the turn unit), when m_Commands (= Unit.Commands, set in the constructor) is not Empty, or when the
        // unit is not IsAbleToAct. TimeMoved and Swift are not modelled (no case uses them). Engine boundary; nothing else changes Status here.
        public void NativeTick()
        {
            if (Status != TurnStatus.Preparing || Unit == null) return;
            var acted = Unit.CombatState.Cooldown.StandardAction > 0f || Unit.CombatState.Cooldown.MoveAction > 0f;
            if (acted || !Unit.Commands.Empty || !Unit.IsAbleToAct()) Status = TurnStatus.Acting;
        }
        // PR #70 round 2: the native input facts click admission and the movement executor read (engine boundary).
        public const float MetersOfFiveFootStep = 1.524f;
        public bool IsOverTerrain;
        public TurnController() { }
        public TurnController(Kingmaker.EntitySystem.Entities.UnitEntityData unit) { Unit = unit; }
        // R11-F1 (round 15): native Dispose (event unsubscription; engine boundary, no state here).
        public void Dispose() { }
        // PR #70 round 4 (R1-F6/R1-F10): the installed movement-input state and rules (decomposition/pr2/r4/turncontroller-input-il.txt):
        // MovementLimit {TwoActions 0, FiveFootStep 1, OneAction 2}; EnabledFiveFootStep = limit FiveFootStep or the step modifier;
        // EnabledSingleActionMove = limit OneAction (panic is not modelled); HasNormalMovement = no step/forced travel and GetRemainingTime > 0;
        // SetMovementLimit refuses while the unit's view is moving; TrySelectMovementLimit (directly controllable branch) as installed.
        public enum MovementLimit { TwoActions = 0, FiveFootStep = 1, OneAction = 2 }
        private MovementLimit m_MovementLimit;
        private bool m_ModifyMovementCurrentValue, m_ModifyMovementRequestValue, m_AIUsedFiveFootStep;
        private object m_HighlightedObject;
        // R11-F1 (round 14): the installed movement members CopyGrantedMovementToContext writes by reflection (properties and two fields).
        public float MetersMovedByFiveFootStep { get; set; }
        public float TimeMovedInForceMode { get; set; }
        public float TimeMoved { get; set; }
        public float TimeMovedByFiveFootStep { get; set; }
        public bool ImmuneAttackOfOpportunityOnDisengage { get; set; }
        private bool m_AutoStopAfterFirstMoveAction;
        public bool AutoStopAfterFirstMoveActionForHarness => m_AutoStopAfterFirstMoveAction;
        // The player's own input on this context (the step key / the cycled ground limit).
        public bool EnabledFiveFootStep { get { return m_MovementLimit == MovementLimit.FiveFootStep || m_ModifyMovementCurrentValue; } set { m_ModifyMovementCurrentValue = m_ModifyMovementRequestValue = value; } }
        public bool EnabledSingleActionMove { get { return m_MovementLimit == MovementLimit.OneAction; } set { m_MovementLimit = value ? MovementLimit.OneAction : MovementLimit.TwoActions; } }
        private bool ShouldRestrictNormalMovement() => m_AIUsedFiveFootStep || MetersMovedByFiveFootStep > 0f || TimeMovedInForceMode > 0f;
        private float GetRemainingTime() => Unit == null ? 0f : Math.Max(0f, Math.Min(6f, 3f * ((Unit.IsMoveActionRestricted() ? 1f : 2f) -
            (Unit.UsedStandardAction() ? 1f : 0f)) - Unit.CombatState.Cooldown.MoveAction));
        public bool HasNormalMovement() => !ShouldRestrictNormalMovement() && GetRemainingTime() > 0f;
        public bool HasFiveFootStep() => MetersMovedByFiveFootStep < MetersOfFiveFootStep;
        private bool SetMovementLimit(MovementLimit limit) { if (Unit?.View != null && Unit.View.IsMoving()) return false; m_MovementLimit = limit; return true; }
        private void TrySelectMovementLimit()
        {
            if (Unit?.View != null && Unit.View.IsMoving()) return;
            if (m_HighlightedUnit != null) return;
            var hasStep = HasFiveFootStep();
            var canSingle = HasNormalMovement() && Unit.CombatState.Cooldown.MoveAction < 3f && !Unit.UsedStandardAction();
            if (!HasNormalMovement() && hasStep) SetMovementLimit(MovementLimit.FiveFootStep);
            if (m_MovementLimit == MovementLimit.FiveFootStep && !hasStep) SetMovementLimit(MovementLimit.OneAction);
            if (m_MovementLimit == MovementLimit.OneAction && !canSingle) SetMovementLimit(MovementLimit.TwoActions);
        }
        private Kingmaker.EntitySystem.Entities.UnitEntityData m_HighlightedUnit;
        private Kingmaker.UnitLogic.Commands.UnitCommands m_PredictionCommands;
        private bool m_NeedNewPredictions;
        public void SetNativeClick(Kingmaker.EntitySystem.Entities.UnitEntityData highlighted, Kingmaker.UnitLogic.Commands.UnitCommands predictions, bool needNew)
        { m_HighlightedUnit = highlighted; m_PredictionCommands = predictions; m_NeedNewPredictions = needNew; }
        // PR #70 R5-F2 (round 6): native IgnoreClick on this context with the product's postfix (engine boundary: the case supplies native's
        // own result and runs the ACTUAL AdmitPairedAttackClick on it).
        public Func<bool> IgnoreClickHook;
        public bool IgnoreClick() => IgnoreClickHook != null && IgnoreClickHook();
    }
}

namespace Kingmaker
{
    public sealed class Game
    {
        public static Game Instance = new Game();
        public TimeControllerFacade TimeController = new TimeControllerFacade();
        public TurnBasedCombatControllerFacade TurnBasedCombatController = new TurnBasedCombatControllerFacade();
        // Native Game.CurrentMode (its name is what MountedGameModePolicy reads) and Player.IsInCombat.
        public GameModeFacade CurrentMode = new GameModeFacade("Default");
        public PlayerFacade Player = new PlayerFacade();
        public SelectedAbilityHandlerFacade SelectedAbilityHandler = new SelectedAbilityHandlerFacade();
    }

    public sealed class SelectedAbilityHandlerFacade { public object Ability; }

    public sealed class GameModeFacade { private readonly string name; public GameModeFacade(string name) { this.name = name; } public override string ToString() => name; }
    public sealed class PlayerFacade { public bool IsInCombat = true; public string GameId = "harness-campaign"; }

    public sealed class TimeControllerFacade { public TimeSpan GameTime; public float DeltaTime; }

    public sealed class TurnBasedCombatControllerFacade { public TurnBased.Controllers.TurnController CurrentTurn; public int RoundNumber = 1; public TimeSpan RoundStartTime; }
}

namespace Kingmaker.View
{
    public sealed class UnitMovementAgent
    {
        // Native route stand-in: the polyline the harness world returns for this agent (the mount) toward a destination.
        public Func<Vector3, Vector3, List<Vector3>> Route;
        // PR #70 R1-F5: the native outcome of each request (null: Complete), and every requested destination.
        public Func<Vector3, Vector3, PathCompleteState> Outcome;
        public readonly List<Vector3> Queries = new List<Vector3>();
        public bool NearTheEnd;
        public bool HonorCutoff;
        // Native UnitMovementAgent.IsReallyMoving: the facade agent is moving while the harness steps it.
        public bool IsReallyMoving = true;
        // PR #70 R6-F2 (round 8): the agent's OWN steering state (native UnitMovementAgent.Path, m_NextPointIndex, m_NextWaypoint), separate from
        // the command's ForcedPath: the carrier's approach copies its path in once (PathOwner), and the mover advances the cursor.
        public Pathfinding.Path Path;
        // R13-F4 (round 14): installed UnitMovementAgent.get_WantsToMove (060018BE) is Path != null || RequestedPath != null. Round 16: RequestedPath
        // (non-public in 2.1.7b; the product reads it only through WantsToMove) is the request in flight before native delivers the Path.
        public Pathfinding.Path RequestedPath { get { return m_RequestedPath; } set { m_RequestedPath = value; } }
        // Round 19 (R17-F1, R18-F2): installed m_RequestedPath (0400118E), the field the ACTUAL MountedCarrierPathObserver reads by reflection.
        private Pathfinding.Path m_RequestedPath;
        public bool WantsToMove => Path != null || RequestedPath != null;
        // PR #70 R12-F4 (round 16): the rider's stock agent lease (Behaviour.enabled) and its avoidance guard.
        public bool enabled = true;
        public bool AvoidanceDisabled;
        public int NextPointIndex;
        public UnityEngine.Vector2 NextWaypoint;
        public object PathOwner;
        // PR #70 R8-F6 (round 9): the installed shift contract's two inputs: MaxSpeed (here the unit's speed times SpeedFactor) and MoveDirection.
        public float SpeedFactor = 1f;
        // R9-F5 (round 10): a one-shot failing MaxSpeed getter (injected).
        public static bool MaxSpeedFault;
        // R11-F9 (round 12): native MaxSpeedOverride (when set, MaxSpeed reads it instead of the unit's CurrentSpeedMps).
        public float? MaxSpeedOverride;
        // Round 13 (R12-F5): every MaxSpeed read is counted; MaxSpeedFaultAtRead makes exactly that read throw once.
        public static int MaxSpeedReads; public static int MaxSpeedFaultAtRead = -1;
        public float MaxSpeed { get { MaxSpeedReads++; if (MaxSpeedFaultAtRead >= 0 && MaxSpeedReads == MaxSpeedFaultAtRead) { MaxSpeedFaultAtRead = -1; throw new InvalidOperationException("simulated MaxSpeed getter failure (counted)"); } if (MaxSpeedFault) { MaxSpeedFault = false; throw new InvalidOperationException("simulated MaxSpeed getter failure"); } return MaxSpeedOverride ?? (Unit == null ? 0f : Unit.CurrentSpeedMps) * SpeedFactor; } }
        public UnityEngine.Vector2 MoveDirection;
        // R8-F6 (round 10): the installed combat steering facts (m_CombatAngularSpeed default 720, m_Speed, m_FollowPrecomputedPath).
        public float CombatAngularSpeed = 720f, CurrentSpeedField;
        public bool FollowPrecomputedPath = true;
        public bool ForceModeForHarness => m_IsInForceMode;
        public readonly List<Vector3> Endpoints = new List<Vector3>();
        private static List<Vector3> Cutoff(List<Vector3> points, Vector3 destination, float radius, bool needLos, int blocker)
        {
            var kept = new List<Vector3> { points[0] };
            for (var i = 1; i < points.Count; i++)
            {
                var from = points[i - 1]; var to = points[i];
                var length = Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, to);
                for (var d = 0f; d <= length; d += 0.01f)
                {
                    var p = Vector3.Lerp(from, to, length <= 0f ? 1f : d / length);
                    if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, destination) <= radius &&
                        (!needLos || !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(p + new Vector3(0f, 1.6f, 0f), destination, blocker)))
                    { kept.Add(p); return kept; }
                }
                kept.Add(to);
            }
            return kept;
        }
        private bool m_IsInForceMode;
        private float? m_SavedMinSpeed;
        private float m_MinSpeed = 0.3f, m_WarmupTime = 0.2f, m_SlowDownTime = 0.2f;
        public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;
        public int FindPathCount;
        public Pathfinding.Path FindPath(Vector3 destination, Pathfinding.OnPathDelegate callback, float? approachRadius = null, bool needLOS = false,
            int ignoreBlockerId = 0)
        {
            FindPathCount++;
            Queries.Add(destination);
            var outcome = Outcome == null ? PathCompleteState.Complete : Outcome(Unit.Position, destination);
            var points = Route(Unit.Position, destination);
            // PR #70 R1-F4 (round 3): native FindPath with an approach radius and needLOS ends where the MOVER is within the radius and its own
            // eye sees the destination (the line of sight is checked against ignoreBlockerId), sampled every centimetre along the route.
            if (HonorCutoff && approachRadius.HasValue && points.Count >= 2) points = Cutoff(points, destination, approachRadius.Value, needLOS, ignoreBlockerId);
            Endpoints.Add(points[points.Count - 1]);
            var path = new Pathfinding.Path { vectorPath = points, CompleteState = outcome, error = outcome == PathCompleteState.Error };
            callback?.Invoke(path);
            return path;
        }
    }

    public sealed class UnitEntityView
    {
        public float Corpulence = 0.5f;
        // PR #70 R12-F4 (round 16): the rider leases RestoreMovementAuthority restores (AgentOverride, ForbidRotation).
        public KingmakerMountedCombat.Integration.RiderMovementAgent AgentOverride;
        public bool ForbidRotation;
        public bool Moving;
        public bool IsMoving() => Moving;
        public int StopMovingCount;
        public UnitMovementAgent AgentASP;
        public readonly UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        // PR #70 R6-F7 (round 11): a one-shot failing native StopMoving for one view (injected).
        public static Func<UnitEntityView, bool> StopMovingFault;
        // Round 12 (R6-F7): native StopMoving stops the agent, releasing the path it steers (its path ownership is observable independently of any
        // command's Result).
        public void StopMoving() { if (StopMovingFault != null && StopMovingFault(this)) { StopMovingFault = null; throw new InvalidOperationException("simulated StopMoving failure"); } StopMovingCount++; if (AgentASP != null) { AgentASP.Path = null; AgentASP.PathOwner = null; } }
    }
}

namespace Kingmaker.EntitySystem.Entities
{
    public sealed class UnitState
    {
        public bool IsConscious = true, IsDead, IsFinallyDead, CanMove = true, CanAct = true;
        // A separate native condition (UnitCondition.CanNotAttack), not implied by CanAct.
        public bool CanNotAttack;
        public bool IsUnconscious => !IsConscious;
    }

    public sealed class UnitDescriptor
    {
        public UnitState State = new UnitState();
        public UnitEntityData Pet;
        public MasterReference Master = new MasterReference();
    }
    public sealed class MasterReference { public UnitEntityData Value; }

    public sealed class CooldownFacade { public float StandardAction, MoveAction; }
    public sealed class AnimationManagerFacade { public bool IsPreventingMovement; }
    public sealed class AiDataFacade { public UnitEntityData DislikedTarget; public readonly HashSet<UnitEntityData> UnreachableUnits = new HashSet<UnitEntityData>(); }
    // Native UnitGroup.Memory: the units the group perceives (null Perceives: everything in the world is perceived).
    public sealed class GroupFacade { public MemoryFacade Memory = new MemoryFacade(); }
    public sealed class MemoryFacade { public Func<UnitEntityData, bool> Perceives; public bool Contains(UnitEntityData unit) => Perceives == null || Perceives(unit); }
    public sealed class WeaponTypeFacade { public string AssetGuid = "weapon-type"; }
    public sealed class WeaponBlueprintFacade { public bool IsRanged, IsNatural; public string AssetGuid = "weapon"; public WeaponTypeFacade Type = new WeaponTypeFacade(); }
    // Native WeaponSlot (a hand or an additional limb).
    public sealed class WeaponSlotFacade { }
    public sealed class ItemWeaponFacade { public WeaponBlueprintFacade Blueprint = new WeaponBlueprintFacade(); }

    public sealed class UnitEntityData
    {
        public string UniqueId;
        public Vector3 Position;
        // Native UnitEntityData.Orientation: yaw in degrees (0 faces +Z, 90 faces +X).
        public float Orientation;
        public float WeaponRange = 1.5f;
        // PR #70 R12-F4 (round 16): native Corpulence and Translocate (rider placement after cleanup).
        public float Corpulence => View.Corpulence;
        public int TranslocateCount;
        public void Translocate(Vector3 position, float orientation) { Position = position; Orientation = orientation; TranslocateCount++; }
        public Vector3 EyePosition => Position + new Vector3(0f, 1.6f, 0f);
        public Kingmaker.View.UnitEntityView View = new Kingmaker.View.UnitEntityView();
        public Kingmaker.UnitLogic.Commands.UnitCommands Commands;
        public UnitDescriptor Descriptor = new UnitDescriptor();
        public bool IsInState = true;
        public float CombatSpeedMps = 9f;
        // Native UnitCombatState.ManualTarget (set by UnitCommands.UpdateCombatTarget inside Run and by ClickUnitHandler.OnClick).
        public Kingmaker.Controllers.Combat.UnitCombatState CombatState = new Kingmaker.Controllers.Combat.UnitCombatState();
        // PR #70 R6-F7 (round 9): native HoldState; a write can be made to fail (injected) for one unit.
        public static Func<UnitEntityData, bool> HoldWriteFault;
        private bool holdState;
        public bool HoldState { get { return holdState; } set { if (HoldWriteFault != null && HoldWriteFault(this)) throw new InvalidOperationException("simulated Hold write failure"); holdState = value; } }
        public bool IsDirectlyControllable = true, IsAIEnabled = true, IsBrainActive = true;
        // PR-1 round 7 engine reads: combat and visibility flags, polymorph, perception, first weapon, the brain's cached sight
        // (native UnitEntityData.HasLOS, which can admit a target that direct command sight rejects), the native attack plan size.
        public bool IsInCombat = true, IsVisibleForPlayer = true;
        public object GetActivePolymorph() => null;
        public GroupFacade Group = new GroupFacade();
        public ItemWeaponFacade FirstWeapon = new ItemWeaponFacade();
        public ItemWeaponFacade GetFirstWeapon() => FirstWeapon;
        public Func<UnitEntityData, bool> CachedSight;
        public bool HasLOS(UnitEntityData unit) => CachedSight == null || CachedSight(unit);
        public int AttackCount = 1;
        // Native UnitEntityData.AiMovementForbidden (the rider held in place) and the unit's first limb slot (its primary attack's hand).
        public bool AiMovementForbidden;
        public WeaponSlotFacade PrimaryLimb = new WeaponSlotFacade();
        // Native Standard availability (spent at the first native act of an attack, UnitActionController.UpdateCooldowns).
        public bool HasStandard = true;
        public bool HasStandardAction() => HasStandard;
        // PR #70 round 2: the native movement and action facts the paired allocation reads.
        // R11-F9 (round 12): a one-shot failing CurrentSpeedMps getter for one unit (injected).
        public static Func<UnitEntityData, bool> CurrentSpeedFault;
        public float CurrentSpeedMps { get { if (CurrentSpeedFault != null && CurrentSpeedFault(this)) { CurrentSpeedFault = null; throw new InvalidOperationException("simulated CurrentSpeedMps getter failure"); } return CombatSpeedMps; } }
        public bool MoveRestricted;
        public bool IsMoveActionRestricted() => MoveRestricted;
        public bool UsedStandardAction() => !HasStandard || CombatState.Cooldown.StandardAction > 0f;
        public bool IsAbleToAct() => Descriptor.State.CanAct;
        // PR #70 R1-F9: an ordinary (full) plan whose first attack uses another weapon than a single attack would.
        public ItemWeaponFacade FullAttackFirstWeapon;
        public float FullAttackFirstRange = 1.5f;
        public bool IsStandardActionRestricted() => false;
        public string CharacterName => UniqueId;
        public UnitEntityData(string id) { UniqueId = id; Commands = new Kingmaker.UnitLogic.Commands.UnitCommands(this); }
        public float DistanceTo(UnitEntityData other) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(Position, other.Position);
        // PR #70 R1-F6 (round 7): the native facts the movement hook and command eligibility read (the agent's unit view is this unit here).
        public UnitEntityData EntityData => this;
        public bool IsCommandsPreventMovement;
        public AnimationManagerFacade AnimationManager;
        public bool IsCurrentUnit() => Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit == this;
        public bool IsEnemy(UnitEntityData other) => other != null && other != this && other.UniqueId.StartsWith("hostile", StringComparison.Ordinal);
        public bool CanAttack(UnitEntityData other) => IsEnemy(other);
        public void ForceLookAt(Vector3 point) { }
    }
}

namespace Kingmaker.Controllers.Combat
{
    using Kingmaker.EntitySystem.Entities;
    // Native UnitCombatState (Kingmaker.Controllers.Combat): ManualTarget, the cooldowns and the brain's AI data (disliked and unreachable targets).
    public sealed class UnitCombatState
    {
        // PR #70 R9-F1 (round 10): a manual-target write can be made to fail (injected) for one combat state.
        public static Func<UnitCombatState, bool> ManualTargetWriteFault;
        // PR #70 R14-F4/R11-F8 (round 15): a manual-target READ can be made to fail (injected; the predicate decides, e.g. one-shot).
        public static Func<UnitCombatState, bool> ManualTargetReadFault;
        private UnitEntityData manualTarget;
        public UnitEntityData ManualTarget { get { if (ManualTargetReadFault != null && ManualTargetReadFault(this)) throw new InvalidOperationException("simulated manual-target read failure"); return manualTarget; } set { if (ManualTargetWriteFault != null && ManualTargetWriteFault(this)) throw new InvalidOperationException("simulated manual-target write failure"); manualTarget = value; } }
        public CooldownFacade Cooldown = new CooldownFacade();
        public AiDataFacade AIData = new AiDataFacade();
    }
}

namespace Kingmaker.UnitLogic.Commands.Base
{
    using Kingmaker.EntitySystem.Entities;

    public abstract class UnitCommand
    {
        public enum CommandType { Free, Standard, Swift, Move }
        public enum ResultType { None, Success, Fail, Interrupt }
        public const float InfiniteRange = 1000000f;
        public UnitEntityData Executor { get; private set; }
        public abstract CommandType Type { get; }
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsActed { get; protected set; }
        public bool IsRunning => IsStarted && !IsFinished;
        public ResultType Result { get; private set; }
        public float ApproachRadius;
        public float MaxApproachRadius;
        public bool NeedLoS { get; set; }
        public bool CreatedByPlayer;
        public bool ShowTargetMarker;
        public TimeSpan NextApproachTime;
        public Pathfinding.Path ForcedPath { get; set; }
        public float TimeSinceStart { get; private set; }
        public abstract Vector3 ApproachPoint { get; }
        public virtual void Init(UnitEntityData executor) { Executor = executor; }
        public virtual bool CanMoveAfterStart => false;
        public virtual bool MarkManualTarget => false;
        public virtual bool IsIgnoreCooldown => false;
        public AiActionFacade AiAction;
        // Native UnitCommand.IsMoveUnit (set by TickApproaching while the command approaches).
        public bool IsMoveUnit;
        // Round 19: native UnitCommand.DontInterruptInTurnBasedMode (read by CanMoveInTurnBased's denial branch; the product's carriers never set it).
        public bool DontInterruptInTurnBasedMode;
        public virtual UnitEntityData TargetUnit => null;
        // Native act event due on the next Tick (the attack animation's act, or its pretend-act time).
        public bool ActEventDue;
        // Native UnitCommand.Animation still playing (PR-1 round 14): after the act sets a Result, installed UnitCommand.Tick calls OnEnded
        // only once Animation is null or finished (0x060027A7, IL_01BC..IL_01DB); until then the command keeps ticking unfinished.
        public bool AnimationPlaying;
        public static int NativeDeadlineInterrupts;
        public virtual int GetTargetLOSObjectId() => 7;
        public bool IsUnitEnoughClose => IsUnitCloseEnough(ApproachPoint, Executor.Position, Executor.EyePosition, ApproachRadius, NeedLoS, GetTargetLOSObjectId());
        public static bool IsUnitCloseEnough(Vector3 approachPoint, Vector3 position, Vector3 eyePosition, float approachRadius, bool nedLOS, int ignoreBlockerId) =>
            Kingmaker.Utility.GeometryUtils.SqrMechanicsDistance(approachPoint, position) <= approachRadius * approachRadius &&
            (!nedLOS || !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(eyePosition, approachPoint, ignoreBlockerId));
        // Native UnitCommand.Start (after native ShouldStartCommand passed).
        public void Start() { IsStarted = true; OnStart(); }
        // Native UnitCommand.Tick (Assembly-CSharp 2.1.7b): not running -> Interrupt; TimeSinceStart += dt; OnTick(); return if
        // finished; at the act: Result = OnAction(), then IsActed = true; a result ends the command; finally a started command whose
        // TimeSinceStart exceeds 9 s is interrupted (UnitMoveContiniously aside, not modelled here).
        public void Tick()
        {
            if (!IsRunning) { Interrupt(); return; }
            TimeSinceStart += 0.1f;
            OnTick();
            if (IsFinished) return;
            if (ActEventDue && (!IsActed || RepeatActAllowed))
            {
                ActEventDue = false;
                var result = OnAction();
                IsActed = true;
                if (result != ResultType.None) Result = result;
            }
            if (Result != ResultType.None && !AnimationPlaying) { OnEnded(); return; }
            if (TimeSinceStart > 9f) { NativeDeadlineInterrupts++; Interrupt(); }
        }
        // Native UnitCommand.ForceFinishForTurnBased -> ForceFinish: IsActed set without OnAction, the result, then OnEnded.
        public void ForceFinishForTurnBased(ResultType result) { if (!IsFinished) { IsActed = true; Result = result; OnEnded(); } }
        protected virtual ResultType OnAction() => ResultType.None;
        public virtual bool RepeatActAllowed => false;
        public void Interrupt(bool raiseEvent = true) { if (Result != ResultType.Interrupt && !IsFinished) { Result = ResultType.Interrupt; OnEnded(raiseEvent); } }
        public void ForceFinish(ResultType result) { if (!IsFinished) { Result = result; OnEnded(); } }
        public void FinishSuccess() { if (!IsFinished) { Result = ResultType.Success; OnEnded(); } }
        protected void SetTimeSinceStart(float value) { TimeSinceStart = value; }
        protected virtual void OnStart() { }
        protected virtual void OnTick() { }
        protected virtual void OnEnded(bool raiseEvent = true) { IsFinished = true; }
        public void TurnToTarget() { }
    }
}

namespace Kingmaker.UnitLogic.Commands
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands.Base;

    // Native BlueprintAiAction (the brain's action that proposed this command).
    public sealed class AiActionFacade { public string AssetGuid = "ai-attack"; }

    public sealed class AttackHandInfo { public float WeaponRange; public Kingmaker.EntitySystem.Entities.ItemWeaponFacade Weapon; public Kingmaker.EntitySystem.Entities.WeaponSlotFacade Hand; }

    public class UnitAttack : UnitCommand
    {
        public static int NativeAttackStarts;
        public UnitAttack(UnitEntityData target) { Target = target; NeedLoS = true; }
        public UnitEntityData Target { get; private set; }
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => Target.Position;
        public bool IsSingleAttack;
        public bool IsFullAttack;
        public bool IsCharge;
        public object LastAttackRule;
        public List<AttackHandInfo> AllAttacks = new List<AttackHandInfo>();
        // Native m_AttackIndex: the next attack of the plan; PlannedAttack is AllAttacks[m_AttackIndex] (installed UnitAttack).
        private int m_AttackIndex;
        public AttackHandInfo PlannedAttack => m_AttackIndex < AllAttacks.Count ? AllAttacks[m_AttackIndex] : null;
        public int GetAttackIndex() => m_AttackIndex;
        // The targets each delivered attack struck, in order (the attack rule's target).
        public readonly List<UnitEntityData> StruckTargets = new List<UnitEntityData>();
        // Native UnitAttack.Init: the attack plan (its executor's attacks this round), then the native approach radius.
        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            // Native UnitAttack.Init: an explicit single attack plans one attack; an ordinary attack plans the executor's routine.
            AllAttacks = new List<AttackHandInfo>();
            for (var i = 0; i < (IsSingleAttack ? 1 : Math.Max(1, executor.AttackCount)); i++)
            {
                var full = !IsSingleAttack && i == 0 && executor.FullAttackFirstWeapon != null;
                AllAttacks.Add(new AttackHandInfo { WeaponRange = full ? executor.FullAttackFirstRange : executor.WeaponRange,
                    Weapon = full ? executor.FullAttackFirstWeapon : executor.FirstWeapon, Hand = executor.PrimaryLimb });
            }
            ApproachRadius = executor.View.Corpulence + Target.View.Corpulence + executor.WeaponRange;
        }
        // Native UnitAttack.OnStart: the attack (release) begins; counted as the single native attack start.
        protected override void OnStart() { NativeAttackStarts++; }
        public override UnitEntityData TargetUnit => Target;
        public override bool MarkManualTarget => true;
        // Native UnitAttack.OnAction: the attack rule is triggered (delivery).
        public static int NativeAttackRules;
        // Installed UnitAttack.OnAction (0x06002681): unless the target is dead, trigger the planned attack's rule; advance m_AttackIndex;
        // return None while attacks remain (the routine continues to its next act), Success after the last.
        protected override ResultType OnAction()
        {
            if (!Target.Descriptor.State.IsDead)
            {
                NativeAttackRules++; LastAttackRule = new object(); StruckTargets.Add(Target);
                int count; acts.TryGetValue(Executor, out count); acts[Executor] = count + 1;
                if (Executor.HasStandard) StandardSpends++;
                Executor.HasStandard = false;   // the native cooldown write at the first act
            }
            m_AttackIndex++;
            return m_AttackIndex < AllAttacks.Count ? ResultType.None : ResultType.Success;
        }
        public static int StandardSpends;
        // A routine with attacks remaining acts again (native: the next attack's animation act).
        public override bool RepeatActAllowed => m_AttackIndex > 0 && m_AttackIndex < AllAttacks.Count;
        private static readonly Dictionary<UnitEntityData, int> acts = new Dictionary<UnitEntityData, int>();
        public static int Acts(UnitEntityData unit) { int count; return unit != null && acts.TryGetValue(unit, out count) ? count : 0; }
        public static void ResetActs() => acts.Clear();
    }

    // An unstarted Standard command whose target is out of its reach: native steers its executor toward it.
    public sealed class HarnessApproachingCommand : UnitCommand
    {
        public Vector3 Goal;   // round 18 (R16-F1): the target can move (into reach) between native action ticks
        public HarnessApproachingCommand(Vector3 goal) { Goal = goal; ApproachRadius = 0.5f; }
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => Goal;
    }
    // A stationary prerequisite (a self-targeted Standard action already in reach): it never approaches.
    public sealed class HarnessStationaryCommand : UnitCommand
    {
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => Executor.Position;
    }

    // A player command (MarkManualTarget, no AiAction): native UpdateCombatTarget makes its target its actor's manual target when
    // Run installs it. WritesNoTarget models a click's Run of a command that writes nothing (the click handler then assigns).
    public sealed class HarnessPlayerCommand : UnitCommand
    {
        private readonly UnitEntityData target;
        private readonly CommandType type;
        public bool WritesNoTarget;
        public HarnessPlayerCommand(UnitEntityData target, CommandType type) { this.target = target; this.type = type; CreatedByPlayer = true; ApproachRadius = InfiniteRange; }
        public override CommandType Type => type;
        public override Vector3 ApproachPoint => target == null ? Executor.Position : target.Position;
        public override UnitEntityData TargetUnit => WritesNoTarget ? null : target;
        public override bool MarkManualTarget => !WritesNoTarget;
    }
    // A queued player command whose native promotion is refused (its Run prefix returns false): nothing is installed or written.
    public sealed class HarnessRefusedCommand : UnitCommand
    {
        private readonly UnitEntityData target;
        public HarnessRefusedCommand(UnitEntityData target) { this.target = target; CreatedByPlayer = true; }
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => target.Position;
        public override UnitEntityData TargetUnit => target;
        public override bool MarkManualTarget => true;
    }

    public sealed class UnitAttackOfOpportunity : UnitCommand
    {
        public override CommandType Type => CommandType.Free;
        public override Vector3 ApproachPoint => Executor.Position;
    }

    public sealed class UnitMoveTo : UnitCommand
    {
        public readonly Vector3 Point;
        public UnitMoveTo(Vector3 point, float approachRadius) { Point = point; ApproachRadius = approachRadius; }
        public override CommandType Type => CommandType.Move;
        public override Vector3 ApproachPoint => Point;
        public List<Vector3> Travelled = new List<Vector3>();
        public bool FollowedForcedPath;
        public int PathIndex;
        // Native proximity decisions: the first one taken for this command, and whether it completed before moving at all.
        public bool? FirstProximityDecision;
        public bool CompletedWithoutMoving;
        public float TravelledMetres;
    }

    public sealed class UnitCommands
    {
        private readonly UnitEntityData owner;
        private readonly UnitCommand[] raw = new UnitCommand[4];
        public readonly LinkedList<UnitCommand> Queue = new LinkedList<UnitCommand>();
        public object GroupCommand;
        public UnitCommand PreviousCommand;
        public int RunCount;
        public UnitCommands(UnitEntityData owner) { this.owner = owner; }
        public UnitEntityData OwnerForHarness => owner;
        public UnitCommand[] Raw => raw;
        public bool Empty => raw.All(command => command == null) && Queue.Count == 0;
        public UnitCommand Standard => raw[(int)UnitCommand.CommandType.Standard];
        public UnitCommand GetCommand(UnitCommand.CommandType type) => raw[(int)type];
        public bool Contains(UnitCommand command) => command != null && raw[(int)command.Type] == command;
        // The pair's native seams (InternalRunPrefix for a promotion, the Run postfix, AddToQueueInternal's postfix).
        public static Func<UnitCommand, bool> PromotionPrefix;
        private static readonly Dictionary<UnitCommand, int> installs = new Dictionary<UnitCommand, int>(), holdWrites = new Dictionary<UnitCommand, int>(), targetWrites = new Dictionary<UnitCommand, int>();
        public static int MoveSubmissions;
        private static void Count(Dictionary<UnitCommand, int> map, UnitCommand command) { int value; map.TryGetValue(command, out value); map[command] = value + 1; }
        private static int Read(Dictionary<UnitCommand, int> map, UnitCommand command) { int value; return map.TryGetValue(command, out value) ? value : 0; }
        public static int Installs(UnitCommand command) => Read(installs, command);
        public static int HoldWrites(UnitCommand command) => Read(holdWrites, command);
        public static int TargetWrites(UnitCommand command) => Read(targetWrites, command);
        public static void ResetCounters() { installs.Clear(); holdWrites.Clear(); targetWrites.Clear(); MoveSubmissions = 0; }
        public static Action<UnitCommands, UnitCommand, bool> RunPostfix;
        // PR #70 R6-F1 (round 7): the transpiled Hold write (PatchMethods.NativeRunHoldWrite) asks the product whether this Run keeps its owner's Hold.
        public static Func<UnitEntityData, UnitCommand, bool, bool> NativeRunHoldHook;
        // PR #70 R6-F7 (round 7): a native Run (or a callback inside it) that throws before the command is installed.
        public static Func<UnitCommand, bool> RunThrowsBeforeInstall;
        public static Action<UnitCommands, UnitCommand> QueuePostfix;
        // Native Run (slot acceptance): the slot's previous occupant is interrupted and replaced; UpdateCombatTarget writes the manual
        // target for a MarkManualTarget command without AiAction.
        public void Run(UnitCommand command) => RunInternal(command, false);
        private void RunInternal(UnitCommand command, bool fromQueue)
        {
            if (fromQueue && command is HarnessRefusedCommand) { RunPostfix?.Invoke(this, command, true); return; }
            if (fromQueue && PromotionPrefix != null && !PromotionPrefix(command)) { RunPostfix?.Invoke(this, command, true); return; }
            if (RunThrowsBeforeInstall != null && RunThrowsBeforeInstall(command)) throw new InvalidOperationException("simulated native Run failure before installation");
            if (command.Executor == null) command.Init(owner);
            var existing = raw[(int)command.Type];
            if (existing != null && existing != command && !existing.IsFinished) existing.Interrupt();
            raw[(int)command.Type] = command;
            RunCount++;
            Count(installs, command);
            if (command is UnitMoveTo) MoveSubmissions++;
            // Native Run clears its owner's Hold (NativeRunHoldBoundary) unless the product's keep-Hold seam keeps it (when connected).
            if (NativeRunHoldHook == null || !NativeRunHoldHook(owner, command, fromQueue)) owner.HoldState = false;
            Count(holdWrites, command);
            if (command.MarkManualTarget && command.AiAction == null && command.TargetUnit != null) { owner.CombatState.ManualTarget = command.TargetUnit; Count(targetWrites, command); }
            RunPostfix?.Invoke(this, command, fromQueue);
        }
        // Native AddToQueueInternal (queue acceptance).
        public void Enqueue(UnitCommand command)
        {
            if (command.Executor == null) command.Init(owner);
            Queue.AddLast(command);
            QueuePostfix?.Invoke(this, command);
        }
        // Native RemoveFinishedAndUpdateQueue: finished slots are cleared; the first queued command is promoted when its slot is free.
        // PR #70 R6-F7 (round 10): one slot cleanup made to fail (injected) for one container, after native completion was stamped.
        public static Func<UnitCommands, bool> RemoveFault;
        // PR #70 R12-F4 (round 16): native UnitCommands.InterruptMove (the reading the round-15 finding traced): every unfinished command in the Move
        // slot, and every other unfinished command approaching (IsMoveUnit), is interrupted; counted per container.
        public int InterruptMoveCalls;
        public void InterruptMove()
        {
            InterruptMoveCalls++;
            foreach (var command in raw.ToArray()) if (command != null && !command.IsFinished && (command.Type == UnitCommand.CommandType.Move || command.IsMoveUnit)) command.Interrupt();
        }
        public void RemoveFinishedAndUpdateQueue()
        {
            if (RemoveFault != null && RemoveFault(this)) { RemoveFault = null; throw new InvalidOperationException("simulated slot cleanup failure"); }
            for (var i = 0; i < raw.Length; i++) if (raw[i] != null && raw[i].IsFinished) raw[i] = null;
            var first = Queue.First?.Value;
            // Native promotion of a Standard command also requires the Move slot to be empty (Run pairs Move/Standard).
            if (first != null && raw[(int)first.Type] == null &&
                (first.Type != UnitCommand.CommandType.Standard || raw[(int)UnitCommand.CommandType.Move] == null)) { Queue.RemoveFirst(); RunInternal(first, true); }
        }
    }
}

// PR #70 R11-F1 (round 12): the row capture/restore the persistence adapters use. Round 14: the saved-row DTOs, their validation, the archive
// envelope and the SELECTED archive codec are compiled from their whole production files (SavedCombatData.cs, MountedSaveData.cs,
// MountedArchiveData.cs, MountedArchiveCodec.cs, ...) with the installed Newtonsoft.Json.
namespace KingmakerMountedCombat.Integration
{
    using System.IO;
    /* PERSISTENCE_ROWS */
}

namespace KingmakerMountedCombat.Logging
{
    public interface IModLogger { void Info(string line); void Exception(string phase, Exception exception); }
}

namespace KingmakerMountedCombat.Integration
{
    using Kingmaker.Controllers.Combat;
    using System.Reflection;
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands;
    using Kingmaker.UnitLogic.Commands.Base;
    using Kingmaker.Utility;
    using Kingmaker.View;
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Logging;
    using TurnBased.Controllers;
    using Kingmaker;
    using Harness;

    /* ADMISSION_ENUM */

    /* CHILD_ATTACK_CLASS */

    /* CARRIER_CLEANUP_CLASS */

    /* CARRIER_PATH_OBSERVER */

    /* ACCEPTED_WORK_ROUTING */

    internal sealed class HarnessLogger : IModLogger
    {
        public readonly List<string> Lines = new List<string>();
        public void Info(string line) { Lines.Add(line); }
        public void Exception(string phase, Exception exception) { Lines.Add("EXCEPTION " + phase + ": " + exception.GetType().Name + " " + exception.Message); }
    }

    // Charge machinery is never engaged by these cases (chargeMode=false); its types exist only so the actual members compile.
    internal sealed class MountedChargeLease
    {
        public MountedChargeLease(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IModLogger logger) { }
        public Func<bool> ForeignMoverActive;
        public string ApplyFailureReason, ApplyFailedStep, UnresolvedCleanup, CleanupFailures;
        public bool ApplyRolledBack, ForcedPathOutstanding, Restored = true, RollbackComplete = true;
        public void Apply() { }
        public void Maintain(bool live) { }
        public bool NeedsReforce(bool live) => false;
        public bool TryRestore() => true;
        public void HandBuffToNativeDuration() { }
    }

    internal sealed class MountedDoorInteractionCommand : UnitCommand
    {
        public UnitMoveTo DelegatedMove;
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => Vector3.zero;
        public void TickMountedApproach() { }
    }

    internal sealed class HarnessCarrierHost { public bool TryTickCarriedApproach(UnitCommand command, bool shell) => false; }

    internal static class PatchBridge
    {
        public static KingmakerMountedCombat.Logging.IModLogger Logger = new HarnessLogger();
        public static HarnessCarrierHost CombatFor(UnitEntityData unit) => null;
        // PR #70 R11-F8 (round 15): the ACTUAL custody and registry lookups the production InternalRunPostfix reads, spliced from
        // MountedPatchController.PatchBridge: World (Service?.World), Owner(commands) (the world pair whose relationship's PairOwner holds the
        // container's actor), CombatFor(commands) (its CombatOwner) and Pairs (World?.Pairs, the production MountedWorldService roster, which only
        // grows). PairOwner is the real Domain MountedPairOwner of the real MountedPairRegistry, moved by the relationship's spliced production
        // ownership members (CommitOwnership, CompleteOwnershipAttempt, RefreshOwnership, RefreshRetiredOwnership).
        internal static GameMountedRelationshipService Service;
        /* PATCH_BRIDGE_WORLD */
        /* PATCH_BRIDGE_OWNER_COMMANDS */
        /* PATCH_BRIDGE_COMBAT_FOR_COMMANDS */
        /* PATCH_BRIDGE_PAIRS */
    }
    // The production MountedPairServices fields the lookups read.
    internal sealed class MountedPairServices { internal GameMountedRelationshipService Relationship; internal MountedCombatController Combat; }
    // The production MountedWorldService roster (pairs only grow; spliced Pairs).
    internal sealed class MountedWorldService
    {
        private readonly List<MountedPairServices> pairs = new List<MountedPairServices>();
        /* WORLD_PAIRS */
        internal void PublishForHarness(MountedPairServices pair) => pairs.Add(pair);
        // PR #70 R12-F4 (round 16): the production world visit and teardown drain (spliced) over this roster; no admission fence offline.
        private MountedPairServices[] PairSnapshot => pairs.ToArray();
        internal bool AdmissionFenced() => false;
        /* WORLD_VISIT */
        /* WORLD_SETTLE_OUTSTANDING */
    }
    // PR #70 R12-F4 (round 16): the teardown cases bind the REAL Domain MountedRelationshipCoordinator (Real) over the spliced runtime restoration;
    // the other cases keep only a state.
    internal sealed class HarnessCoordinator
    {
        public MountedRelationshipCoordinator Real;
        private RelationshipState state = RelationshipState.Unmounted;
        public RelationshipState State { get { return Real != null ? Real.State : state; } set { state = value; } }
        public MountedPair ActivePair => Real?.ActivePair;
        public TransitionResult Dismount(CleanupTrigger trigger)
        {
            if (Real == null) throw new InvalidOperationException("The harness relationship has no real coordinator bound.");
            return Real.Dismount(trigger);
        }
    }
    // The production GameMountedRelationshipService ownership surface: its registry (the world's real Domain MountedPairRegistry), coordinator
    // state, PairOwner and the spliced production ownership transitions. The reservation is the registry's TryReserve with the predicate the
    // production TryReserveOwnership binds (retainedCommands: MountedWorldService's pair.Combat.HasRetainedPairCommands; the harness has no
    // stick-movement or participation obligations); the mount's success commits it (CommitOwnership); the production Dismount tail sets the
    // coordinator Unmounted and runs CompleteOwnershipAttempt (detach, then the retirement check of every detached owner).
    internal sealed class GameMountedRelationshipService
    {
        internal MountedWorldService World;
        internal MountedCombatController CombatOwner;
        internal readonly MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> Ownership;
        private readonly HarnessCoordinator coordinator = new HarnessCoordinator();
        private bool importedMountedOwnership, importedParticipationOwnership;
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> PairOwner { get; private set; }
        internal GameMountedRelationshipService(MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> registry) { Ownership = registry; }
        /* RELATIONSHIP_COMMIT_OWNERSHIP */
        /* RELATIONSHIP_COMPLETE_OWNERSHIP */
        /* RELATIONSHIP_REFRESH_OWNERSHIP */
        /* RELATIONSHIP_REFRESH_RETIRED */
        internal void MountForHarness(UnitEntityData rider, UnitEntityData mount, Func<bool> retainedCommands)
        {
            RefreshOwnership();
            MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner; string refusal;
            if (!Ownership.TryReserve(Kingmaker.Game.Instance.Player, Kingmaker.Game.Instance.Player.GameId, rider, mount, 1, this,
                retainedCommands, () => true, () => true, out owner, out refusal)) throw new InvalidOperationException(refusal);
            PairOwner = owner;
            coordinator.State = RelationshipState.Mounted;
            CommitOwnership();
            CompleteOwnershipAttempt();
        }
        internal void DismountForHarness() { coordinator.State = RelationshipState.Unmounted; CompleteOwnershipAttempt(); }
        // PR #70 R12-F4 (round 16): the production relationship cleanup (Dismount, spliced whole) and its collaborators. The Dismounting subscribers
        // are the ones each case binds (the combat owner's HandleDismounting cancellation); the stick-movement owner has no residue offline; Record
        // keeps every result.
        private bool disposed, automaticCleanupRetry, cleanupRetryRequired;
        private CleanupTrigger cleanupRetryTrigger = CleanupTrigger.Exception;
        private TransitionResult attachmentCleanup;
        internal event Action<CleanupTrigger> Dismounting;
        internal Func<CleanupTrigger, bool> ChargeCleanupBarrier { get; set; }
        private readonly HarnessLogger logger = new HarnessLogger();
        internal readonly HarnessContinuousDrive continuousDrive = new HarnessContinuousDrive();
        internal KingmakerMountedPairRuntime runtime;
        internal readonly List<TransitionResult> Recorded = new List<TransitionResult>();
        public RelationshipState State => coordinator.State;
        private TransitionResult Record(TransitionResult result) { Recorded.Add(result); return result; }
        internal void ReleaseContinuousDrive(string reason, bool explicitRecovery) { continuousDrive.Releases++; }
        private void ObserveCleanupState(TransitionResult result) { }
        /* RELATIONSHIP_CANCEL_IMPORT */
        /* RELATIONSHIP_TEARDOWN_READY */
        /* RELATIONSHIP_MOUNT_COMMAND_OWNED */
        /* RELATIONSHIP_DISMOUNT */
        // The production constructor binds runtime.MountCommandOwned = MountCommandOwnedByPair (the lifecycle contract checks that binding); the
        // pair is mounted through the REAL coordinator (validation, AcquireMovementAuthority, AttachPresentation) and the registry custody.
        internal void MountWithRealCoordinatorForHarness(UnitEntityData rider, UnitEntityData mount, Func<bool> retainedCommands, KingmakerMountedPairRuntime pairRuntime,
            Func<bool> teardownReady)
        {
            runtime = pairRuntime;
            runtime.MountCommandOwned = MountCommandOwnedByPair;
            coordinator.Real = new MountedRelationshipCoordinator(runtime);
            RefreshOwnership();
            MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner; string refusal;
            if (!Ownership.TryReserve(Kingmaker.Game.Instance.Player, Kingmaker.Game.Instance.Player.GameId, rider, mount, 1, this,
                retainedCommands, () => true, teardownReady, out owner, out refusal)) throw new InvalidOperationException(refusal);
            PairOwner = owner;
            var result = coordinator.Real.Mount(new MountedPairCandidate(rider.UniqueId, mount.UniqueId)
            {
                RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
                MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
                MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, SafeMovementMode = true
            });
            if (!result.Succeeded) throw new InvalidOperationException("The real coordinator refused the harness pair: " + string.Join("; ", result.Errors.ToArray()));
            CommitOwnership();
            CompleteOwnershipAttempt();
        }
        internal string OwnerStateForHarness => PairOwner == null ? "none" : PairOwner.State.ToString();
        internal void ObserveOrderDuringContinuousDrive(UnitCommands commands, UnitCommand command) { }
    }
    // PR #70 R12-F4 (round 16): the stick-movement owner's surface the relationship's Dismount and ownership read (no residue offline).
    internal sealed class HarnessContinuousDrive
    {
        public int Releases, Recoveries;
        public int ResidueCount => 0;
        public string LastRetirementFailure => null;
        public void RecoverRetained() { Recoveries++; }
        public void RecoverRetainedDeliberately() { Recoveries++; }
        public bool OwnsCommand(object command) => false;
    }
    public sealed class RiderMovementAgent { }
    // PR #70 R12-F4 (round 16): KingmakerMountedPairRuntime with its ACTUAL RestoreMovementAuthority, the round-16 successor members,
    // ClearPreparedPairWhenUnmounted, TryReleasePreparedReferences and ShouldPlaceRiderAfterCleanup spliced. AcquireMovementAuthority stands in for
    // the mount-time leases (it captures and takes the same leases: rider stock agent disabled, avoidance guard, ForbidRotation, the rider
    // AgentOverride, the mount's AI flag, the pre-mount rider pose); AttachPresentation/RestorePresentation stand in for the attachment (no
    // attachment residue offline). No A* graph is active, so the rider is placed beside the mount.
    internal sealed class KingmakerMountedPairRuntime : IMountedPairRuntime
    {
        private UnitEntityData rider, mount; private Kingmaker.View.UnitEntityView riderView, mountView;
        private object supportedProfile;
        private bool movementAuthorityConfigured, presentationConfigured, mountAiLeaseOwned, mountAiBackingWasEnabled, avoidanceLeaseOwned, riderAvoidanceWasDisabled,
            riderForbidRotationLeaseOwned, riderForbidRotationWasEnabled, overrideInstalled, overrideComponentOwned, riderStockAgentWasEnabled, poseComponentOwned;
        private RiderMovementAgent riderOverride; private Kingmaker.View.UnitMovementAgent riderStockAgent;
        private object positionAnchorObject, positionAnchor, poseAdapter;
        private bool AttachmentLeaseCustodyRetained => false;
        private string PresentationAttachmentRiskState => "none";
        private Vector3 preMountRiderPosition; private float preMountRiderOrientation;
        private static readonly FieldInfo MammothAiBackingField = typeof(UnitEntityData).GetField("IsAIEnabled");
        public int RestoreMovementCalls;
        internal KingmakerMountedPairRuntime(UnitEntityData rider, UnitEntityData mount) { this.rider = rider; this.mount = mount; }
        public void AcquireMovementAuthority(MountedPair pair)
        {
            riderView = rider.View; mountView = mount.View;
            riderStockAgent = riderView.AgentASP ?? (riderView.AgentASP = new Kingmaker.View.UnitMovementAgent { Unit = rider });
            riderStockAgentWasEnabled = riderStockAgent.enabled; riderStockAgent.enabled = false;
            riderAvoidanceWasDisabled = riderStockAgent.AvoidanceDisabled; riderStockAgent.AvoidanceDisabled = true; avoidanceLeaseOwned = true;
            mountAiBackingWasEnabled = mount.IsAIEnabled; mount.IsAIEnabled = false; mountAiLeaseOwned = true;
            riderForbidRotationWasEnabled = riderView.ForbidRotation; riderView.ForbidRotation = true; riderForbidRotationLeaseOwned = true;
            riderOverride = new RiderMovementAgent(); riderView.AgentOverride = riderOverride; overrideInstalled = true; overrideComponentOwned = true;
            preMountRiderPosition = rider.Position; preMountRiderOrientation = rider.Orientation;
            movementAuthorityConfigured = true;
        }
        public void AttachPresentation(MountedPair pair) { presentationConfigured = true; }
        public void RestorePresentation(MountedPair pair) { presentationConfigured = false; TryReleasePreparedReferences(); }
        internal string Leases => "leases{configured " + movementAuthorityConfigured + ", mount AI owned " + mountAiLeaseOwned + ", avoidance owned " + avoidanceLeaseOwned +
            ", rotation owned " + riderForbidRotationLeaseOwned + ", override installed " + overrideInstalled + "}";
        internal bool LeasesReleased => !movementAuthorityConfigured && !mountAiLeaseOwned && !avoidanceLeaseOwned && !riderForbidRotationLeaseOwned && !overrideInstalled && !overrideComponentOwned;
        /* RUNTIME_SUCCESSOR */
        /* RUNTIME_RESTORE_MOVEMENT */
        /* RUNTIME_CLEAR_PREPARED */
        /* RUNTIME_RELEASE_REFERENCES */
        /* RUNTIME_SHOULD_PLACE */
        /* RUNTIME_CANCEL_MOVE */
        /* RUNTIME_CANCEL_MOVE_TRIGGER */
    }
    internal enum NativeLifecycleBoundary { ModDisable }
    internal sealed class HarnessLifecycleLedger
    {
        public readonly List<string> Records = new List<string>();
        public void Record(NativeLifecycleBoundary boundary, string source, RelationshipState before, RelationshipState after, CleanupTrigger? trigger, bool attempted, bool succeeded,
            IEnumerable<string> errors, string detail = null) =>
            Records.Add(boundary + " " + before + "->" + after + " " + trigger + " succeeded=" + succeeded + (errors == null ? "" : " errors=" + string.Join(" | ", errors.ToArray())));
    }
    internal sealed class HarnessUnifiedTurn { public int Ended; public void EndPairedServiceParticipation() { Ended++; } }
    internal sealed class HarnessPersistenceFacade { public int Dropped; public void DropRetainedIntent(string reason) { Dropped++; } }
    // PR #70 R12-F4 (round 16): MountedLifecycleSubscriber with its ACTUAL HandleModDisable, ModDisableSteps (drains, readiness, charge drain and
    // CleanupPairs), Cleanup (the world visit) and CleanupOwner (the relationship Dismount and the completion policy) spliced. The unified turn's
    // participation end and the persistence's retained-intent drop are recorded stand-ins.
    internal sealed class MountedLifecycleSubscriber
    {
        private readonly GameMountedRelationshipService service; private readonly MountedCombatController combat;
        private readonly HarnessUnifiedTurn unifiedTurn = new HarnessUnifiedTurn(); private readonly HarnessPersistenceFacade persistence = new HarnessPersistenceFacade();
        internal readonly HarnessLifecycleLedger ledger = new HarnessLifecycleLedger();
        internal int UnifiedTurnEndsForHarness => unifiedTurn.Ended;
        internal MountedLifecycleSubscriber(GameMountedRelationshipService service, MountedCombatController combat) { this.service = service; this.combat = combat; }
        /* LIFECYCLE_MOD_DISABLE */
        /* LIFECYCLE_CLEANUP */
        /* LIFECYCLE_CLEANUP_OWNER */
    }
    internal static class PointerController { public static bool SimulatingClick; }
    internal abstract class UnitMoveContiniously : UnitCommand { }

    internal static class PatchMethods
    {
        internal static bool ChargeExecutionPrefix(UnitCommand __instance) => true;
        private static void ReportGroupPaceFault(string message, Exception error) => PatchBridge.Logger?.Exception(message, error);
        // PR #70 R11-F8 (round 14): the ACTUAL native Run postfix (accepted-work routing through the custody and registry lookups).
        /* INTERNAL_RUN_POSTFIX */
        private static bool IsRelationshipControlShell(UnitCommand command) => false;
        /* APPROACH_PREFIX */
        /* ADMIT_EXPLICIT_PROMOTION */
        // Round 19 (R17-F1, R18-F2): the ACTUAL production hooks on installed UnitMovementAgent.PathTo and OnPathComplete.
        /* CARRIER_PATH_HOOKS */
        // The pairs' controllers (production: PatchBridge.Pairs); set by the click cases.
        internal static List<Harness.MountedCombatController> Controllers = new List<Harness.MountedCombatController>();
        private static IEnumerable<Harness.MountedCombatController> ClickControllers() => Controllers;
        /* CLICK_ASSIGNMENT_HOOK */
        // PR #70 R5-F2 (round 6): the ACTUAL pointer dispatch bracket and the direct input's target binding.
        /* POINTER_TICK_PREFIX */
        internal static int PointerDispatchClosures;
        /* POINTER_TICK_POSTFIX */
        /* BIND_SCOPED_TARGET */
    }

    // PR #70 R5-F2 (round 6): the ACTUAL MountedPatchController members the admission-aware direct input calls.
    internal static class MountedPatchController
    {
        /* BEGIN_SCOPED_DISPATCH */
        /* END_SCOPED_DISPATCH */
    }
}

namespace KingmakerMountedCombat.Diagnostics
{
    using Kingmaker;
    using Kingmaker.Controllers.Clicks;
    using Kingmaker.EntitySystem.Entities;

    // PR #70 R5-F2 (round 6): the ACTUAL NativeOrdinaryAttackInput.Click over the adapter's stored target and native simulated handler
    // (engine boundary: the handler slot the adapter's constructor selected through native pointer priority).
    internal sealed class NativeOrdinaryAttackInput
    {
        internal sealed class HandlerSlot { public IClickEventHandler Handler; public object GetValue(object owner) => Handler; }
        private readonly HandlerSlot SimulatedHandler = new HandlerSlot();
        private readonly object pointer = new object();
        private readonly UnitEntityData target;
        private readonly Vector3 point;
        internal NativeOrdinaryAttackInput(UnitEntityData target, IClickEventHandler handler) { this.target = target; point = target.Position; SimulatedHandler.Handler = handler; }
        /* NATIVE_INPUT_CLICK */
    }
}

namespace KingmakerMountedCombat.Integration
{
    using Kingmaker.Controllers.Combat;
    using System.Reflection;
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands;
    using Kingmaker.UnitLogic.Commands.Base;
    using Kingmaker.Utility;
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Logging;
    using TurnBased.Controllers;
    using Kingmaker;
    using Harness;

    /* PROBE_CLASS */

    internal sealed class HarnessRelationship { public UnitEntityData Rider, Mount; }

    // PR #70 R6-F2 (round 8): the engine boundary of MountedAgentSteering.TryRead (production reads UnitMovementAgent.Path and the private cursor
    // fields by reflection): the facade agent's own steering state.
    internal static class MountedAgentSteering
    {
        internal static bool TryRead(Kingmaker.View.UnitMovementAgent agent, out List<Vector3> path, out int nextIndex, out UnityEngine.Vector2 nextWaypoint)
        {
            path = agent?.Path?.vectorPath; nextIndex = agent == null ? -1 : agent.NextPointIndex; nextWaypoint = agent == null ? default(UnityEngine.Vector2) : agent.NextWaypoint;
            return path != null;
        }
        internal static float PhysicalSpeed(Kingmaker.View.UnitMovementAgent agent) => agent == null ? float.NaN : agent.MaxSpeed;
        internal static bool TryReadSteering(Kingmaker.View.UnitMovementAgent agent, out UnityEngine.Vector2 moveDirection, out float combatAngularSpeed, out float currentSpeed,
            out bool followPrecomputedPath, out bool forceMode, out bool inCombat)
        {
            moveDirection = agent?.MoveDirection ?? default(UnityEngine.Vector2); combatAngularSpeed = agent?.CombatAngularSpeed ?? float.NaN; currentSpeed = agent?.CurrentSpeedField ?? float.NaN;
            followPrecomputedPath = agent != null && agent.FollowPrecomputedPath; forceMode = agent != null && agent.ForceModeForHarness; inCombat = agent?.Unit != null && agent.Unit.IsInCombat;
            return agent != null;
        }
        internal static UnityEngine.Vector2 MoveDirection(Kingmaker.View.UnitMovementAgent agent) => agent == null ? default(UnityEngine.Vector2) : agent.MoveDirection;
    }

    internal sealed class HarnessTurnParticipant
    {
        public float GrantMetres;
        internal float PairedMountGrantedMetres(TurnController turn, out bool step, out bool singleMove, out string reason)
        {
            step = false; singleMove = false; reason = GrantMetres > 0f ? "granted" : "mount-movement-spent";
            return GrantMetres;
        }
    }

    // The controller members the revalidation uses, around the ACTUAL RevalidateAdmittedClickRoute.
    internal sealed class HarnessController
    {
        private readonly HarnessRelationship relationship = new HarnessRelationship();
        internal readonly HarnessTurnParticipant unifiedTurn = new HarnessTurnParticipant();
        private bool disposed;
        public readonly List<string> Log = new List<string>();
        public HarnessController(UnitEntityData rider, UnitEntityData mount) { relationship.Rider = rider; relationship.Mount = mount; }
        private void TryLog(Func<string> line) { Log.Add(line()); }
        // PR #70 R1-F9: the click's plan (null: the order's own, as an unchanged order has) and the established destination (review R7-F1).
        internal PairedAttackClickProbe.PlanIdentity? ClickPlan;
        internal Vector3? Established;
        internal PairedAttackClickProbe.Approach Revalidate(UnitEntityData target, PairedAttackClickProbe.PlanIdentity dispatch) =>
            RevalidateAdmittedClickRoute(target, Established, ClickPlan ?? dispatch, dispatch);
        /* REVALIDATE */
        internal string LastRouteRevalidation { get; private set; }
        internal string LastRoutePlans { get; private set; }
    }

    // PR #70 R1-F10 (round 4): the paired participant around the ACTUAL production members CanAddressActor, IsPartnerContext,
    // PreservePartnerSelection, SelectedNativeInputContext, PairedMovementContext, BeginPairedPreparation, RequireMountFirstTick,
    // BindMountFirstTickGate, SuppressPairedCandidate (PairedActivationLifecycle / PairedActivationControls), AdmitPairedAttackClick and PairedMountGrantedMetres,
    // over the ACTUAL domain PairedActivation. Engine boundary (simulated, labelled): native TurnController.Prepare of a context (whose
    // prefix runs BeginPairedPreparation), the allocation's native round-state reconciliation (PrepareForHarness), and
    // CompletePairedPreparation's FinishActorPreparation and partner-context creation. Nothing writes addressability, mode flags or grants.
    internal sealed class HarnessPairRelationship
    {
        public MountedCombatController CombatOwner; public RelationshipState State = RelationshipState.Mounted; public UnitEntityData Rider, Mount;
    }
    internal sealed class HarnessPairParticipant
    {
        // PR #70 R11-F1 (round 15): not readonly: the production RestoreParticipation (spliced below) rebinds the saved activation.
        private PairedActivation<UnitEntityData, TurnController> activation;
        private TurnController partnerContext;
        private readonly HarnessPairRelationship relationship = new HarnessPairRelationship();
        private readonly IModLogger logger = new HarnessLogger();
        internal readonly MountedMovementStateAdapter movementState = new MountedMovementStateAdapter();
        private readonly List<UnitCommand> nativePreparationCommands = new List<UnitCommand>();
        private TurnController resumingContext;
        internal string LastInitiativeObservation { get; private set; }
        private bool disposed;
        internal bool PairedLifecycleEnabled => !disposed;
        public bool PartnerCanGetUp;
        private static readonly FieldInfo NativeHighlightedUnit = typeof(TurnController).GetField("m_HighlightedUnit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativeHighlightedObject = typeof(TurnController).GetField("m_HighlightedObject", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativePredictionCommands = typeof(TurnController).GetField("m_PredictionCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativeNeedNewPredictions = typeof(TurnController).GetField("m_NeedNewPredictions", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativeModifyRequested = typeof(TurnController).GetField("m_ModifyMovementRequestValue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativeModifyCurrent = typeof(TurnController).GetField("m_ModifyMovementCurrentValue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NativeMovementLimit = typeof(TurnController).GetField("m_MovementLimit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo NativeSelectMovementLimit = typeof(TurnController).GetMethod("TrySelectMovementLimit", BindingFlags.Instance | BindingFlags.NonPublic);
        private TurnController.MovementLimit mirroredGroundLimit;
        private bool hasMirroredGroundLimit;
        internal HarnessPairParticipant(UnitEntityData rider, UnitEntityData mount, MountedCombatController combat)
        {
            activation = new PairedActivation<UnitEntityData, TurnController>(rider, mount);
            relationship.Rider = rider; relationship.Mount = mount; relationship.CombatOwner = combat;
        }
        internal PairedActivation<UnitEntityData, TurnController> Activation => activation;
        internal TurnController PartnerContextForHarness => partnerContext;
        // Engine boundary: native Prepare of this context; its prefix is the ACTUAL BeginPairedPreparation.
        internal bool NativePrepare(TurnController turn) => BeginPairedPreparation(turn);
        // Engine boundary: CompletePairedPreparation for the boundary context: the principal's reserved grant is prepared, the private partner
        // context is created and natively prepared (its prefix: the ACTUAL BeginPairedPreparation partner branch, which reserves the mount's grant
        // and opens its allocation under the activation identity), the allocation's native round state is reconciled, the partner is prepared.
        internal void CompleteBoundaryPreparation(TurnController turn)
        {
            activation.FinishActorPreparation(turn.Unit);
            partnerContext = new TurnController(activation.Partner);
            if (!BeginPairedPreparation(partnerContext)) throw new InvalidOperationException("The partner's paired preparation was refused.");
            movementState.PrepareForHarness(activation.Partner);
            activation.FinishActorPreparation(activation.Partner);
        }
        // Production mid-encounter adoption after the horse already acted this round: AdoptRunningBoundary with RetainPartnerParticipation
        // (the partner is granted, prepared and ENDED; no partner context exists).
        internal bool AdoptAfterPartnerActed(TurnController turn) => activation.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation) && partnerContext == null;
        // Engine boundary: an actor's allocation forfeited natively (EndActor).
        internal void EndActorForHarness(UnitEntityData actor) => activation.EndActor(actor);
        internal IModLogger Logger => logger;
        /* PAIR_CAN_ADDRESS */
        /* PAIR_IS_PARTNER */
        /* PAIR_PRESERVE_SELECTION */
        /* PAIR_SELECTED_INPUT */
        /* PAIR_MOVEMENT_CONTEXT */
        /* PAIR_BEGIN_PREPARATION */
        /* PAIR_BIND_FIRST_TICK_GATE */
        /* PAIR_SUPPRESS */
        // PR #70 R1-F6 (round 7): the ACTUAL NativeActorEligibleForCommand (native command eligibility of an off-turn actor) and its reads.
        private MountedCombatController combat => relationship.CombatOwner;
        private bool OwnsNativePreparationCommand(UnitEntityData actor, UnitCommand command, TurnController turn) => false;
        /* PAIR_ELIGIBLE */
        private UnitEntityData pendingSplitMount;
        private int pendingSplitRound = -1, splitReleaseRound = -1;
        // PR #70 R11-F1 (round 15): the participation persistence binding. The ACTUAL CaptureParticipation and RestoreParticipation
        // (PairedActivationPersistence.cs) are spliced: capture of the ACTIVE activation and partner turn, and the restore that rebinds the saved
        // activation to the native current boundary, sets partnerContext from the saved partner turn and copies the restored allocation into it.
        // Their state fields are the production ones; DiscardParticipation below is the housekeeping reset the restore begins with (its production
        // forensic capture/logging is not modelled), and NativeTurnPersistence is the installed field copy (engine boundary, below this class).
        private UnitEntityData armedRider, armedMount;
        private object activationSession;
        private bool lastTurnBased;
        private long pairedRenewalNotBefore;
        private TurnController preparedRiderTurn;
        internal long PersistenceWorldDiscardCount { get; private set; }
        internal void DiscardParticipation()
        {
            PersistenceWorldDiscardCount++;
            partnerContext = null; nativePreparationCommands.Clear();
            activation = null; armedRider = null; armedMount = null; activationSession = null; preparedRiderTurn = null; resumingContext = null;
            pendingSplitMount = null; pendingSplitRound = -1; splitReleaseRound = -1; pairedRenewalNotBefore = 0;
        }
        /* PAIR_CAPTURE_PARTICIPATION */
        /* PAIR_RESTORE_PARTICIPATION */
        /* PAIR_CAPTURE_ACTOR_PARTICIPATION */
        internal long RedundantMountTurnSkipCount { get; private set; }
        internal string LastTurnCandidateObservation { get; private set; }
        /* PAIR_ADMIT_CLICK */
        internal int AdmittedAttackClickCount { get; private set; }
        internal string LastAttackClickDecision { get; private set; }
        /* PAIR_GRANT */
        /* PAIR_STEP_FINAL */
        /* PAIR_GRANT_PRICED */
        private TurnController pairedMovementInputContext;
        /* PAIR_REMEMBER_INPUT */
        /* PAIR_MOVEMENT_INPUT */
        internal TurnController MovementInputContextForHarness(TurnController turn) => MovementInputContext(turn);
    }

    // PR #70 R11-F1 (round 15), engine boundary: the installed TurnController field copy of NativeTurnPersistence (production reflects over the
    // installed private fields by metadata token; the facade has the same members): the movement record (TimeMoved, TimeMovedInForceMode,
    // TimeMovedByFiveFootStep, MetersMovedByFiveFootStep, the disengage immunity, AI step and auto-stop), status, movement limit and modifier.
    internal static class NativeTurnPersistence
    {
        private static readonly FieldInfo Limit = typeof(TurnController).GetField("m_MovementLimit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Modify = typeof(TurnController).GetField("m_ModifyMovementCurrentValue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Requested = typeof(TurnController).GetField("m_ModifyMovementRequestValue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AiStep = typeof(TurnController).GetField("m_AIUsedFiveFootStep", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AutoStop = typeof(TurnController).GetField("m_AutoStopAfterFirstMoveAction", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static long Captures, Restores;
        internal static SavedTurnContext Capture(TurnController turn)
        {
            if (turn == null) return null;
            Captures++;
            return new SavedTurnContext
            {
                ActorId = turn.Unit.UniqueId, Status = (int)turn.Status,
                Movement = new SavedMovementValues { TimeMoved = turn.TimeMoved, TimeForced = turn.TimeMovedInForceMode, TimeStepped = turn.TimeMovedByFiveFootStep,
                    MetresStepped = turn.MetersMovedByFiveFootStep, StepImmune = turn.ImmuneAttackOfOpportunityOnDisengage,
                    AiStep = (bool)AiStep.GetValue(turn), AutoStop = (bool)AutoStop.GetValue(turn) },
                MovementLimit = Convert.ToInt32(Limit.GetValue(turn)), ModifyCurrent = (bool)Modify.GetValue(turn), ModifyRequested = (bool)Requested.GetValue(turn),
                SmartIndex = -1
            };
        }
        internal static TurnController Restore(SavedTurnContext saved, IDictionary<string, UnitEntityData> actors)
        {
            if (saved == null) return null;
            Restores++;
            var turn = new TurnController(actors[saved.ActorId]) { Status = (TurnController.TurnStatus)saved.Status, TimeMoved = saved.Movement.TimeMoved,
                TimeMovedInForceMode = saved.Movement.TimeForced, TimeMovedByFiveFootStep = saved.Movement.TimeStepped,
                MetersMovedByFiveFootStep = saved.Movement.MetresStepped, ImmuneAttackOfOpportunityOnDisengage = saved.Movement.StepImmune };
            AiStep.SetValue(turn, saved.Movement.AiStep); AutoStop.SetValue(turn, saved.Movement.AutoStop);
            Limit.SetValue(turn, Enum.ToObject(Limit.FieldType, saved.MovementLimit));
            Modify.SetValue(turn, saved.ModifyCurrent); Requested.SetValue(turn, saved.ModifyRequested);
            return turn;
        }
    }

    // PR #70 R1-F10: the mount's paired allocation, around the ACTUAL MountedMovementStateAdapter members (allocation, Get, TickDelegated,
    // RemainingOrderMetres, HasGrantedMovement). The agent pacing fields TickDelegated writes are the facade agent's.
    internal sealed class MountedMovementStateAdapter
    {
        /* ADAPTER_ALLOCATION */
        private static readonly FieldInfo ForceModeField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_IsInForceMode", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SavedMinSpeedField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_SavedMinSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MinSpeedField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_MinSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo WarmupField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_WarmupTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SlowdownField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_SlowDownTime", BindingFlags.Instance | BindingFlags.NonPublic);
        internal void MaintainLifetimes() { }
        // R11-F1 (round 14): the production persistence members of this adapter (CapturePersistence, RestorePersistence, CopyGrantedMovementToContext
        // and the installed turn members it writes), spliced; the allocation lifetime is the production Domain class.
        private ActorAllocationLifetime<UnitEntityData, Allocation> lifetimeField;
        private ActorAllocationLifetime<UnitEntityData, Allocation> lifetime => lifetimeField ?? (lifetimeField = new ActorAllocationLifetime<UnitEntityData, Allocation>(allocations));
        internal void Clear() { preparingTurn = null; lifetime.Clear(); }
        /* ADAPTER_CAPTURE_PERSISTENCE */
        /* ADAPTER_RESTORE_PERSISTENCE */
        /* ADAPTER_TURN_MEMBERS */
        /* ADAPTER_TURN_PROPERTY */
        /* ADAPTER_TURN_FIELD */
        /* ADAPTER_COPY_TO_CONTEXT */
        private TurnController preparingTurn;
        internal string AllocationGrantIdentity(UnitEntityData mount) => Get(mount).GrantIdentity;
        /* ADAPTER_BEGIN_GRANTED */
        /* ADAPTER_REMAINING_GRANTED */
        // Engine boundary: native Prepare of this allocation (production Prepared also writes the native turn fields).
        internal void PrepareForHarness(UnitEntityData mount)
        {
            var allocation = Get(mount);
            allocation.Prepared = true;
            allocation.MoveUsed = mount.CombatState.Cooldown.MoveAction;
            allocation.StandardUsed = mount.CombatState.Cooldown.StandardAction > 0f;
        }
        internal float AllocationMoveUsed(UnitEntityData mount) => Get(mount).MoveUsed;
        internal float AllocationMetresStepped(UnitEntityData mount) => Get(mount).Movement.MetresStepped;
        // Round 11 (R9-F3): the physical step ledger after the last tick's steered displacement settled (what any later read sees).
        internal float AllocationMetresStepTravelled(UnitEntityData mount) { var allocation = Get(mount); allocation.SettleStepTravel(mount.Position); return allocation.Movement.MetresStepTravelled; }
        internal float AllocationTimeStepped(UnitEntityData mount) => Get(mount).Movement.TimeStepped;
        // Round 12 (R11-F1): the allocation's movement record as the production capture reads it (settled first), and its replacement by a restored record.
        internal MountedMovementState AllocationMovementForHarness(UnitEntityData mount) { var allocation = Get(mount); allocation.SettleStepTravel(mount.Position); return allocation.Movement; }
        internal void RestoreMovementForHarness(UnitEntityData mount, MountedMovementState movement) => Get(mount).Movement = movement;
        // Engine boundary: the allocation's provenance. Rider first in initiative: the activation's explicit grant at the rider's boundary;
        // mount first: the mount's own native epoch before pairing (its earlier native movement stays on its cooldown).
        internal void SetGrantIdentity(UnitEntityData mount, string identity) => Get(mount).GrantIdentity = identity;
        // Round 12 (R11-F2): a partially used step with distinct native (nominal) and physical ledgers.
        internal void SetStepLedger(UnitEntityData mount, float nominalMetres, float physicalMetres) { var m = Get(mount).Movement; m.MetresStepped = nominalMetres; m.MetresStepTravelled = physicalMetres; m.TimeStepped = m.TimeMoved = nominalMetres / mount.CombatSpeedMps; }
        internal void SetStepped(UnitEntityData mount, float metres) { var m = Get(mount).Movement; m.MetresStepped = metres; m.TimeStepped = m.TimeMoved = metres / mount.CurrentSpeedMps; }
        /* ADAPTER_GET */
        /* ADAPTER_TICK */
        /* ADAPTER_STEP_ENDPOINT */
        /* ADAPTER_ORDER */
        /* ADAPTER_PHYSICAL_SPEED */
        /* ADAPTER_NOMINAL_SPEED */
        /* ADAPTER_HAS */
        /* ADAPTER_STEP_FINAL */
        /* ADAPTER_FIRST_TICK */
    }

    // Native NativeSingleAttackWeaponResolver: the mount's primary natural attack (the Horse Bite in its first additional limb).
    // Native NativeSingleAttackWeaponResolver: the mount's primary natural attack, its actual weapon in its actual first limb slot.
    internal sealed class NativeSingleAttackWeaponSelection
    {
        public NativeSingleAttackSlotKind Kind = NativeSingleAttackSlotKind.AdditionalLimb;
        public int AdditionalLimbIndex;
        public Kingmaker.EntitySystem.Entities.ItemWeaponFacade Weapon;
        public Kingmaker.EntitySystem.Entities.WeaponSlotFacade Slot;
    }
    internal static class NativeSingleAttackWeaponResolver
    {
        public static NativeSingleAttackWeaponSelection Resolve(UnitEntityData mount) =>
            mount == null ? null : new NativeSingleAttackWeaponSelection { Weapon = mount.FirstWeapon, Slot = mount.PrimaryLimb };
    }
    internal sealed class SupportedMountedProfile { public string Id = "horse"; }
    internal static class SupportedMountedProfiles { public static SupportedMountedProfile Resolve(UnitEntityData rider, UnitEntityData mount) => rider != null && mount != null ? new SupportedMountedProfile() : null; }
    internal static class MountedNativeRunScope { internal static bool EndedInsideReplacingRun(Kingmaker.UnitLogic.Commands.UnitCommands container, UnitCommand ended) => false; }
    internal sealed class HorsePrimaryAttackAnimationAdapter { public void SupplyExact(MountedPairAttackCommand command, AttackHandInfo attack, UnitEntityData horse) { } }
    // The production native terminal hooks (NativeCommandTerminalBranches, spliced): the engine model calls them where the transpiled
    // native sites do, with native's own CanAct observation.
    internal static class NativeCommandTerminalBranches
    {
        /* TERMINAL_CAPTURE_CAN_ACT */
        /* TERMINAL_TB_FORCE_FINISH */
        /* TERMINAL_RUNNING_SITE */
        /* TERMINAL_RECORD */
    }
    internal sealed class UnitAnimationActionHandle { public bool IsActed, IsFinished, IsInterrupted; }

    /* OUTCOME_CLASS */
    /* CLICK_RESULT_ENUM */

    internal sealed class MountedPairAttackCommand : MountedPairSingleAttack
    {
        internal bool NativePartnerMovement { get; set; } = true;
        private const float TargetRepathDistance = 0.75f;
        private const float MaximumElapsedSeconds = 8.5f;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private UnitEntityData attackTarget;
        private float lastChaseDistance = float.NaN;
        private float approachAllowanceSeconds;
        private readonly UnitEntityData actionActor;
        private readonly MountedCombatActionKind action;
        private readonly IModLogger logger;
        private bool allowApproach;
        private readonly bool chargeMode;
        private bool chargeCleanupRequested;
        private bool chargeCarrierMotionOwned;
        private Kingmaker.View.UnitEntityView chargeCarrierView;
        private Kingmaker.View.UnitMovementAgent chargeCarrierAgent;
        private readonly List<KeyValuePair<UnitCommands, UnitMoveTo>> chargeCarriers = new List<KeyValuePair<UnitCommands, UnitMoveTo>>();
        private MountedChargeLease chargeLease;
        private bool chargeLeaseApplicationFailed, chargeLeaseRolledBackOnFailure, carrierTerminatedAfterLeaseFailure, chargeRevalidationFailed;
        private string chargeLeaseApplicationFailure, chargeLeaseApplicationFailedStep, chargeRevalidationFailurePhase, chargeRevalidationFailureReason;
        private string chargeCleanupDebtAtEnd = string.Empty;
        private bool carrierReleaseProvenForAttack;
        private readonly MountedChargeTransactionSequence chargeSequence = new MountedChargeTransactionSequence();
        private readonly MountedCombatTransaction transaction = new MountedCombatTransaction();
        private MountedPairSingleAttack childAttack => this;
        private UnitMoveTo delegatedMove;
        private bool admittingDelegatedMove;
        private Vector3 targetSnapshot;
        private MountedAttackEndCause endCause;
        private string endCauseDetail;
        private MountedActorCapabilityBlock endCauseCapabilityBlock;
        private bool approachRequiredAtStart;
        private MountedPairNativeAdmissionState initialNativeAdmissionState;
        private bool nativeLineOfSightRecoveryObserved;
        private float delegatedMoveApproachRadius;
        private int delegatedMoveStartCount;
        private int delegatedMoveTickCount;
        private Vector3 delegatedMoveOrigin;
        private readonly List<string> repathObservations = new List<string>();
        private string delegatedMoveExecutorId;
        private bool delegatedMoveExecutorIsExactMount = true;
        private bool delegatedMoveNeverQueuedOnMount = true;
        private bool delegatedMoveOwnedByMountMoveSlot = true;
        private bool mountMoveSlotUnreplacedThroughoutApproach = true;
        private bool mountQueueEmptyThroughoutApproach = true;
        private bool delegatedMoveFinishedSuccessfully;
        private bool delegatedMoveStoppedAtLegalRange;
        private string delegatedMoveResultBeforeLegalRangeStop;
        private float delegatedMovePairDistanceAtLegalRangeStop;
        private bool delegatedMoveDrivenByStockController;
        private bool mountMoveSlotRestoredAfterApproach;
        private Vector3 riderPositionAtCommandStart, mountPositionAtCommandStart, targetPositionAtCommandStart;
        private float initialPairDistance, pairDistanceAtAttackStart, riderDisplacementAtAttackStart, mountDisplacementAtAttackStart, targetDisplacementAtAttackStart;
        private bool nativeSequenceTick, nativeMeleeTailRangeRejected;
        internal bool NativeRangedTailTermination { get; private set; }
        private UnitEntityData unconsciousOrderedTarget;

        // Harness construction (the production constructor additionally binds the relationship owner, animation adapter and
        // terminal callback, none of which these approach/start paths read).
        internal MountedPairAttackCommand(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, MountedCombatActionKind action,
            IModLogger logger, bool allowApproach = true, bool singleAttack = false)
            : base(target, rider, mount, action != MountedCombatActionKind.MountPrimaryNatural, singleAttack)
        {
            this.rider = rider; this.mount = mount; attackTarget = target; this.action = action; this.logger = logger;
            this.allowApproach = allowApproach;
            initialTargetId = target?.UniqueId;
            // As TryExecuteAction resolves it (CaptureContext): the mount's exact primary natural attack.
            if (action == MountedCombatActionKind.MountPrimaryNatural) expectedMountPrimary = NativeSingleAttackWeaponResolver.Resolve(mount);
            terminal = (command, outcome) => command.Deliver(outcome, null);
            actionActor = action == MountedCombatActionKind.MountPrimaryNatural ? mount : rider;
            ApproachRadius = InfiniteRange; MaxApproachRadius = InfiniteRange; NeedLoS = false; CreatedByPlayer = true;
            if (!transaction.Arm(action)) throw new InvalidOperationException("arm");
        }

        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            ApproachRadius = InfiniteRange;
            NeedLoS = false;
        }

        internal MountedCombatTransaction Transaction => transaction;
        internal bool CarrierReleaseProven => carrierReleaseProvenForAttack;
        internal bool CarrierStoppedAtLegalRange => delegatedMoveStoppedAtLegalRange;
        internal MountedAttackEndCause EndCause => endCause;
        internal string EndCauseDetail => endCauseDetail;
        internal int TerminalReports;
        internal int ChildCreations;

        // ---- inert stubs (charge, diagnostics, pair liveness): not on the paths under test ----
        private void RequireLiveExactPair() { }
        private void ObserveApproachInvariants() { }
        private bool TerminateIfActionActorBlocked() => false;
        private bool DriveDelegatedMoveOnRiderTurn() => false;
        private string DescribeRepath(string cause) => cause;
        private bool TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase phase, bool readLane = true) => false;
        private bool ObserveChargeStep(MountedChargeTransactionStep step) => false;
        private bool ChargeTransactionStopped() => false;
        internal bool ChargeTransactionDelegating => false;
        internal void TryDrainChargeCarrier() { }
        internal bool TryDischargeChargeCleanupDebt() => true;
        private bool ForeignPathActive() => false;
        private bool IsNativeRangedTailTermination() => false;
        internal Action<MountedPairAttackCommand> TerminalCallback;
        // PR-1 round 7: the production ReportTerminalOnce (spliced) delivers the production outcome (DescribeOutcome, or its fallback)
        // through this terminal: by default the legacy hook; the controller's production HandleCommandTerminal for a wrapper made by
        // TryExecuteAction. DeliveredOutcome is the outcome actually delivered.
        private bool terminalReported;
        private Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal;
        internal MountedPairAttackOutcome DeliveredOutcome;
        private void Deliver(MountedPairAttackOutcome outcome, Action<MountedPairAttackCommand, MountedPairAttackOutcome> next)
        {
            DeliveredOutcome = outcome; TerminalReports++;
            if (next != null) next(this, outcome); else TerminalCallback?.Invoke(this);
        }
        // Production constructor shape (TryExecuteAction): the relationship, the resolved mount primary and the terminal handler.
        internal MountedPairAttackCommand(Harness.HarnessRelationship relationship, UnitEntityData rider, UnitEntityData mount, UnitEntityData target,
            MountedCombatActionKind action, NativeSingleAttackWeaponSelection expectedMountPrimary, HorsePrimaryAttackAnimationAdapter animation,
            IModLogger logger, Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal, bool allowApproach, bool explicitRequest)
            : this(rider, mount, target, action, logger, allowApproach, explicitRequest)
        {
            this.expectedMountPrimary = expectedMountPrimary;
            this.terminal = (command, outcome) => command.Deliver(outcome, terminal);
        }
        // Binds a harness-built wrapper to the controller's production terminal handler.
        internal void BindTerminal(Action<MountedPairAttackCommand, MountedPairAttackOutcome> handler) { terminal = (command, outcome) => command.Deliver(outcome, handler); }
        // Diagnostic fields the production DescribeOutcome copies (native capture paths not modelled here leave their defaults).
        private UnitAnimationActionHandle horsePrimaryAnimationHandle;
        private string horsePrimaryAnimationHandleSource, horsePrimaryAnimationActionName, horsePrimaryAnimationActionType;
        private MountedNativeTerminalBranch nativeTerminalBranch;
        private MountedActorCapabilityBlock nativeTerminalBlock;
        private bool wrapperCommandRetainedThroughoutApproach = true, riderStockAgentSuppressedThroughoutApproach = true, poseHealthyThroughoutApproach = true,
            mountStockAgentAuthoritativeThroughoutApproach = true, delegatedMoveDrivenByRiderTurnAdapter;
        private string retainedAttackWeaponTypeBlueprintId, retainedAttackWeaponBlueprintId, reloadStateBefore, ammunitionStateBefore, preStartInterruptBoundary;
        private bool retainedAttackWeaponIsRanged, retainedAttackWeaponIsNatural;
        private string initialTargetId;
        private int delegatedMoveProgressObservationCount, approachObservationCount;
        private readonly List<string> continuationTargetIds = new List<string>();
        private NativeSingleAttackWeaponSelection expectedMountPrimary;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation = new HorsePrimaryAttackAnimationAdapter();
        /* COMMAND_OPTIONAL_RANGED */
        /* COMMAND_ADMIT_UNCONSCIOUS */
        /* COMMAND_RECORD_NATIVE_TERMINAL */
        /* COMMAND_TARGET_INVALID_NOW */
        /* COMMAND_REPORT_TERMINAL */
        /* COMMAND_DESCRIBE_OUTCOME */
        /* COMMAND_TARGET_REASON */
        /* COMMAND_RECORD_TARGET_INVALIDATION */
        private void TryContinueRoutineOnNextTarget() { }
        /* COMMAND_CREATE_VALIDATE_CHILD */
        private void TryLogInfo(string line) { try { logger.Info(line); } catch (Exception) { } }
        private void TryLogException(string phase, Exception exception) { try { logger.Exception(phase, exception); } catch (Exception) { } }
        private static float HorizontalDistance(Vector3 first, Vector3 second) => GeometryUtils.MechanicsDistance(first, second);

        internal int DelegatedMoveStarts => delegatedMoveStartCount;
        internal bool ChargeMode => chargeMode;
        internal MountedCombatActionKind Action => action;
        internal UnitEntityData ActionActor => actionActor;
        /* ON_ACTION */
        // Harness-only: mark the child attack as delivered (an attack rule raised), the native release evidence.
        internal void MarkDeliveredForHarness() { if (childAttack != null) childAttack.LastAttackRule = new object(); }
        /* APPROACH_FAILURE */
        /* RECORD_END_CAUSE */
        /* END_AFTER_FAULT */
        /* TARGET_LIFE */
        /* OWN_MOVES_AND_PRE_START */
        /* CAN_MOVE_AFTER_START */
        /* ON_START */
        /* ON_TICK */
        /* TARGET_INVALIDATION */
        /* ON_ENDED */
        /* TICK_APPROACH */
        /* REPATH */
        /* BEGIN_DELEGATED */
        /* STOP_DELEGATED */
        /* SUCCESSOR_STEERS */
        /* FORBID_APPROACH */
        /* DEFERRED_CARRIER */
        /* APPROACHING_BEFORE_ATTACK */
        /* START_CHILD */
        /* OBSERVE_ADMISSION */
    }
}

namespace Harness
{
    using Kingmaker;
    using Kingmaker.View;
    using TurnBased.Controllers;
    using KingmakerMountedCombat.Logging;
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands;
    using Kingmaker.UnitLogic.Commands.Base;
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Integration;

    // PR-1 round 4: the controller's lifecycle members (retirement, promotion admission, accepted-work and selection observation,
    // intent cleanup, the planned-strike probe) are spliced from the ACTUAL MountedCombatController source into this facade, whose
    // fields are the ones those members read. The surrounding drive (TryDriveStockAttackIntent, HandleCommandTerminal) is not
    // spliced: its decisions are the production domain functions the cases call with the facts it assembles (compiled-wiring checks
    // in scripts/Test-MmrAttackOrdersLifecycleContract.ps1 bind that glue).
    // Native GameMountedRelationshipService.Runtime.CancelMountMovement -> mount.Commands.InterruptMove(): every unfinished mount command
    // whose IsMoveUnit is set is interrupted (native TickApproaching sets that flag on an approaching attack).
    internal sealed class HarnessRuntime
    {
        public UnitEntityData Mount; public int MoveInterrupts;
        public bool PoseHealthy = true; public string MountProfileId = "horse"; public string MountDisplayName = "Horse";
        // PR #70 R12-F4 (round 16): the teardown cases bind the spliced production runtime, so the combat owner's cancellation runs its
        // CancelMountMovement overloads (InterruptMove and the mount view's StopMoving, or the mod-disable successor yield).
        public KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime Production;
        public void CancelMountMovement(KingmakerMountedCombat.Domain.CleanupTrigger trigger)
        {
            if (Production != null) { MoveInterrupts++; Production.CancelMountMovement(trigger); return; }
            CancelMountMovement();
        }
        public void CancelMountMovement()
        {
            if (Production != null) { MoveInterrupts++; Production.CancelMountMovement(); return; }
            if (Mount == null) return;
            foreach (var command in Mount.Commands.Raw) if (command != null && !command.IsFinished && command.IsMoveUnit) { MoveInterrupts++; command.Interrupt(); }
        }
    }
    internal sealed class HarnessGroundOwner { public HarnessRelationship Relationship; public UnitEntityData Mount => Relationship.Mount; }
    internal sealed class HarnessRelationship
    {
        public RelationshipState State = RelationshipState.Mounted; public UnitEntityData Rider, Mount; public long MountedPairGeneration = 1;
        public readonly HarnessRuntime Runtime = new HarnessRuntime();
        private HarnessGroundOwner pairOwner;
        public HarnessGroundOwner PairOwner => pairOwner ?? (pairOwner = new HarnessGroundOwner { Relationship = this });
        public bool IsCurrentPairOwner(HarnessGroundOwner owner) => owner != null && ReferenceEquals(owner, PairOwner);
        public bool IsExactCapturedView(UnitEntityData unit) => unit != null;
    }
    internal sealed class MountedDoorOccupancySettlement { public UnitMoveTo RecoveryMove; public bool IsFinished, Cancelled; public string CancelReason; public void Cancel(string reason) { Cancelled = true; CancelReason = reason; } }
    internal sealed class HarnessCarriedApproach { public UnitMoveTo DelegatedMove; }
    // Native capability reads (production NativeActorCapability.Read builds MountedActorCapabilityFacts from the unit's state).
    internal static class NativeActorCapability
    {
        // PR #70 R6-F7 (round 8): a native capability read that throws (injected).
        public static Func<UnitEntityData, bool> ReadFaults;
        public static MountedActorCapabilityFacts Read(UnitEntityData unit)
        {
            if (unit != null && ReadFaults != null && ReadFaults(unit)) throw new InvalidOperationException("simulated native capability read failure");
            return unit == null ? new MountedActorCapabilityFacts() : new MountedActorCapabilityFacts { Known = true, CantAct = !unit.Descriptor.State.CanAct, CanNotAttack = unit.Descriptor.State.CanNotAttack };
        }
        // A native read inside the terminal description that fails for these units (the fallback outcome path).
        public static Func<UnitEntityData, bool> DescribeFaults;
        public static string Describe(UnitEntityData unit, object command)
        {
            if (unit != null && DescribeFaults != null && DescribeFaults(unit)) throw new InvalidOperationException("simulated native capability read failure");
            return unit == null ? "<none>" : "canAct=" + unit.Descriptor.State.CanAct;
        }
    }
    // Native SelectionManager.Instance.SelectedUnits.
    internal sealed class SelectionManager { public static SelectionManager Instance = new SelectionManager(); public List<UnitEntityData> SelectedUnits = new List<UnitEntityData>();
        public UnitEntityData SingleSelectedUnit => SelectedUnits.Count == 1 ? SelectedUnits[0] : null; }
    // Native SettingsRoot (the auto-stop-after-first-Move setting the movement executor reads).
    internal sealed class SettingsRoot { public static SettingsRoot Instance = new SettingsRoot(); public SettingBool AutoStopAfterFirstMoveAction = new SettingBool(); }
    internal sealed class SettingBool { public bool CurrentValue; }
    // Native brain ranking (production NativeBrainRankingReader reads the rider brain's live candidate list as native ranked it).
    internal static class NativeBrainRankingReader
    {
        internal struct Candidate { public UnitEntityData Unit; public float Weight; public float Distance; public float ActionRangeWithCorpulence; public bool InLos; }
        // Native GameHelper.CheckLOS from the engine model (cached sight).
        internal static bool NativeLos(UnitEntityData unit, UnitEntityData target) => unit != null && target != null && unit.HasLOS(target);
        public static List<Candidate> Ranked;
        // The production reader's native facts, from the engine model: BlueprintAiAttack.GetRange (the unit's primary weapon range) plus
        // both bodies (DecisionContext.ActionRangeWithCorpulence), and native line of sight (TargetInfo.InLos -> CheckLOS: cached sight).
        internal static List<Candidate> Candidates(UnitEntityData unit, Kingmaker.UnitLogic.Commands.AiActionFacade action) =>
            Ranked?.ConvertAll(c => new Candidate { Unit = c.Unit, Weight = c.Weight, Distance = c.Distance,
                ActionRangeWithCorpulence = unit.WeaponRange + unit.View.Corpulence + c.Unit.View.Corpulence, InLos = unit.HasLOS(c.Unit) });
        // Native BlueprintAiAttack.GetRange for a fresh DecisionContext of the unit, from the engine model: the unit's current primary weapon range
        // (read at every call, so a changed weapon or reach is seen); NaN without an action, as the production reader returns when unreadable.
        internal static float CurrentActionRange(UnitEntityData unit, Kingmaker.UnitLogic.Commands.AiActionFacade action) =>
            unit == null || action == null ? float.NaN : unit.WeaponRange;
        internal static string Describe(UnitEntityData rider, UnitEntityData proposed) => "harness-ranking;proposed=" + proposed?.UniqueId;
    }
    internal sealed class HarnessEngagement
    {
        public int Proposals, Dispatches;
        public void ObserveProposal(double now, string nativeProposed, string selected, string ranking, bool admitted, long generation, string refusal) { Proposals++; }
        public void ObserveDispatch(long generation) { Dispatches++; }
    }
    internal sealed class HarnessOwnership { public void Retire() { } }
    internal sealed class HarnessChargeOwner { public MountedPairAttackCommand Command; public HarnessOwnership Ownership = new HarnessOwnership(); }
    internal sealed class HarnessScheduler
    {
        public int ExternalInterrupts, Registrations, TerminalObservations;
        // PR #70 R6-F7 (round 9): a failure injected into the cleanup's scheduler observation.
        public static bool InterruptFault;
        public void ObserveExternalInterrupt(MountedPairAttackCommand command, string reason) { if (InterruptFault) { InterruptFault = false; throw new InvalidOperationException("simulated cleanup failure"); } ExternalInterrupts++; }
        // The paired scheduler's lease applies to legacy turn-based Mount Primary only (not exercised here).
        public bool RequiresLease(MountedCombatActionKind action) => false;
        public bool TryRegister(MountedPairAttackCommand command, out string reason) { reason = null; Registrations++; return true; }
        public bool ConfirmAdmission(MountedPairAttackCommand command, out string reason) { reason = null; return true; }
        public void AbandonRegistration(MountedPairAttackCommand command, string reason) { }
        public void ObserveTerminal(MountedPairAttackCommand command, string result, string reason) { TerminalObservations++; }
        public bool CanRegisterCurrentPair(out string reason) { reason = null; return true; }
    }

    internal sealed class MountedCombatController
    {
        private readonly HarnessRelationship relationship = new HarnessRelationship();
        private readonly HarnessScheduler pairedCommandScheduler = new HarnessScheduler();
        private readonly KingmakerMountedCombat.Logging.IModLogger logger = new HarnessLogger();
        private bool disposed;
        private MountedPairAttackCommand activeCommand;
        private MountedPairAttackCommand finishedCommandPendingSweep;
        private MountedPairAttackCommand stockIntentCommand;
        private readonly MountedAttackIntent<UnitEntityData, TurnBased.Controllers.TurnController> stockIntent = new MountedAttackIntent<UnitEntityData, TurnBased.Controllers.TurnController>();
        private UnitEntityData stockIntentTarget => stockIntent.Target;
        private readonly MountedAutomaticRunLedger automaticRuns = new MountedAutomaticRunLedger();
        private readonly MountedRetainedOrderLedger retainedOrder = new MountedRetainedOrderLedger();
        internal bool HasStockAttackIntent => stockIntentTarget != null;
        internal string LastStockAttackObservation;
        internal long StockAttackIntentCancelCount;
        internal long AutomaticQueuedRefusedCount;
        internal int PlannedStrikeProbeCount;
        internal string LastCompetingWorkDetail { get; private set; }
        internal MountedRetainedOrderLedger Ledger => retainedOrder;
        internal MountedPairAttackCommand Active => activeCommand;
        internal MountedPairAttackCommand IntentCommand => stockIntentCommand;
        internal long Generation => stockIntent.Generation;
        internal int ExternalInterruptCount => pairedCommandScheduler.ExternalInterrupts;

        // ---- round 6: the full cancellation, the drive's dispatch operation, the stationary admission; their native-facing stubs ----
        internal sealed class HarnessGuard { public void Clear() { } }
        internal sealed class HarnessSettings { public bool UseLegacyUnifiedTurn; public bool EnablePairedActivation = true; public bool UsePairedTurnControls = true; public bool EnableUnsafeMovementExperiment = true; }
        internal sealed class HarnessUnifiedTurn
        {
            public UnitEntityData Unaddressable;
            public bool CanAddressActor(UnitEntityData actor, TurnBased.Controllers.TurnController turn) =>
                Participant != null ? Participant.CanAddressActor(actor, turn) : actor != Unaddressable && (turn == null || turn.Unit == null || true);

            // PR #70 R8-F5 (round 9): the production movement-context cache (the ACTUAL RememberPairedMovementInput / MovementInputContext).
            public void RememberPairedMovementInput() { Participant?.RememberPairedMovementInput(); }

            // PR #70 R1-F10: the paired participant whose ACTUAL PairedMountGrantedMetres the revalidation reads.
            public HarnessPairParticipant Participant;
            internal bool PairedMountStepCounterFinal(float tick) => Participant != null && Participant.PairedMountStepCounterFinal(tick);
            internal float PairedMountGrantedMetres(TurnController turn, out bool step, out bool singleMove, out string reason, TurnController movementInput = null)
            {
                if (Participant != null) return Participant.PairedMountGrantedMetres(turn, out step, out singleMove, out reason, movementInput);
                step = false; singleMove = false; reason = "no-participant"; return 0f;
            }
            // PR #70 R1-F6 (round 6): the production participant's first-tick gate binding (the ACTUAL BindMountFirstTickGate).
            internal void BindMountFirstTickGate(IMountedFirstTickGate gate, bool bind) { Participant?.BindMountFirstTickGate(gate, bind); }
            // PR #70 R1-F6 (round 7): UnifiedMountedTurnCoordinator.TickMountMovement (an unaddressable mount gets no time; otherwise the ACTUAL
            // TickDelegated on the mount's movement context). LastMovementObservation is its result.
            public string LastMovementObservation;
            internal void TickMountMovement(TurnController turn, ref float deltaTime)
            {
                var mount = Participant?.Activation.Partner;
                if (Participant == null || !Participant.CanAddressActor(mount, turn)) { deltaTime = 0f; LastMovementObservation = "mount-not-addressable"; return; }
                LastMovementObservation = Participant.movementState.TickDelegated(Participant.MovementInputContextForHarness(turn), mount, ref deltaTime);
            }
        }
        internal sealed class HarnessAvailability { public bool IsAllowed; public string Feedback; }
        private readonly HarnessGuard overlayWorldInputGuard = new HarnessGuard();
        private readonly HarnessSettings settings = new HarnessSettings();
        internal readonly HarnessUnifiedTurn unifiedTurn = new HarnessUnifiedTurn();
        private HarnessChargeOwner chargeOwner;
        // ---- round 7: the production drive's and proposal's controller state, counters and native-facing stubs ----
        private const string AutomaticInputSurface = "automatic-native-target";
        internal MountedCombatRejectionCode[] LastRejectionCodes { get; private set; }
        internal MountedPairAttackOutcome LastOutcome { get; private set; }
        internal readonly HarnessEngagement engagement = new HarnessEngagement();
        private TurnBased.Controllers.TurnController stockIntentTurn => stockIntent.Turn;
        internal void Drive() => TryDriveStockAttackIntent();
        internal void Propose(UnitAttack proposal, bool turnBased) => ObserveAutomaticProposal(proposal, true, turnBased);
        internal Action<MountedPairAttackCommand, MountedPairAttackOutcome> ProductionTerminal => HandleCommandTerminal;
        internal UnitEntityData IntentTarget => stockIntentTarget;
        private UnitEntityData automaticApproachFailedTarget;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation = new HorsePrimaryAttackAnimationAdapter();
        private bool HasFaultedChargeCleanupOwner => false;
        private bool HasChargeOwnership => false;   // charge ownership is not modelled by this harness
        private bool HasActiveGroundMovement => false;
        private bool riderTurnGroundMoveAdmissionPending;
        private bool HasMountOperationBesidesAttack(UnitEntityData mount) => false;
        private void ObservePlayerMoveOutcome() { }
        // The production CancelPendingDoorSettlement (an unfinished settlement is cancelled and released).
        private void CancelPendingDoorSettlement(string reason) { var yielding = pendingDoorSettlement; pendingDoorSettlement = null; if (yielding != null && !yielding.IsFinished) yielding.Cancel(reason); }
        internal MountedDoorOccupancySettlement PendingDoorSettlement { get { return pendingDoorSettlement; } set { pendingDoorSettlement = value; } }
        private void ReleaseChargeManualTarget(HarnessChargeOwner owner) { }
        internal long AutomaticProposalCount { get; private set; }
        internal long AutomaticTieBreakCount { get; private set; }
        internal long AutomaticRefusedCount { get; private set; }
        internal long AutomaticContinuedCount { get; private set; }
        internal long AutomaticAdmittedCount { get; private set; }
        internal long AutomaticDispatchCount { get; private set; }
        internal long StockAttackRiderDispatchCount { get; private set; }
        internal long StockAttackMountDispatchCount { get; private set; }
        internal long StockAttackDuplicateDispatchCount { get; private set; }
        internal long StockAttackDispatchBeforeReadinessCount { get; private set; }
        internal string LastAutomaticTieBreak { get; private set; }
        internal string LastAutomaticRefusal { get; private set; }
        internal string LastAutomaticProposalRanking { get; private set; }
        /* CONTROLLER_CAPTURE_CONTEXT */
        /* CONTROLLER_IN_COMBAT */
        /* CONTROLLER_TRY_CONTAIN */
        /* CONTROLLER_CANCEL */
        private string ChargeCleanupStatus => "none";
        private MountedDoorInteractionCommand activeDoorInteraction;
        private MountedDoorOccupancySettlement pendingDoorSettlement;
        // ---- PR #70 round 7: the state the ACTUAL OwnsExactPairedNativeCommand, TryOverrideMountTurnMovement and NativeRunKeepsHold read ----
        private UnitMoveTo activeRiderTurnGroundMove;
        private HarnessCarriedApproach carriedApproach;
        private bool HasLiveCarriedApproachMove => false;
        private bool HasLiveCarriedApproach => false;
        private int mountMovementOpportunityFrame;
        internal bool IsMountOwnSwiftAbility(UnitCommand command) => false;
        internal long AutomaticHoldKeptCount { get; private set; }
        internal long AutomaticQueuedHoldKeptCount { get; private set; }
        /* CONTROLLER_OWNS_UNIFIED */
        /* CONTROLLER_OWNS_PAIRED */
        /* CONTROLLER_OWNS_MOTION */
        internal bool ChargeAdmissionFenced => false;
        /* CONTROLLER_OVERRIDE_MOUNT */
        /* CONTROLLER_RUN_KEEPS_HOLD */
        /* CONTROLLER_COMMIT_PROVISIONAL */
        /* CONTROLLER_APPLY_EFFECTS */
        internal MountedCombatActionKind ArmedAction { get; private set; }
        internal MountedCombatActionKind RiderPrimaryAction = MountedCombatActionKind.RiderMelee;
        public bool HasActiveCommand => activeCommand != null && !activeCommand.IsFinished;
        private MountedCombatActionKind ResolveRiderPrimaryAction() => RiderPrimaryAction;
        // PR #70 R6-F7 (round 9): a failure injected inside the commit's transfer (after the previous order's state was retired, before the new
        // order owns its command and carrier).
        internal static bool FaultInTransfer;
        internal static string TransferFaultObservation;
        private void ClearObservedStockRequest()
        {
            if (!FaultInTransfer) return;
            TransferFaultObservation = "intent=" + (stockIntentTarget != null) + ";generation=" + stockIntent.Generation + ";ledger=" + retainedOrder.Generation +
                ";riderSelectionGeneration=" + riderSelection.Generation;
            throw new InvalidOperationException("simulated failure between retirement and transfer");
        }
        internal long RiderSelectionGeneration => riderSelection.Generation;
        internal long MountSelectionGeneration => mountSelection.Generation;
        private void ClearProjectedNativeMountTurnGroundMove() { }
        // PR #70 R6-F7 (round 8): a fault injected inside CancelCore after the previous generation was retired (between acceptance mutations).
        internal static bool FaultInRetirement;
        private void ReleaseCarriedApproach(string reason) { if (FaultInRetirement) throw new InvalidOperationException("simulated failure between acceptance mutations"); }
        // PR #70 R12-F4 (round 16): the production ground-order admission completion, its cancellation, the mod-disable release and the
        // Dismounting handler (spliced), over the fields they read.
        private bool groundOrderOpen; private UnitCommand groundOrderClickApproach; private HarnessGroundOwner groundMoveOwner;
        private int activeGroundMoveDriveCount; private float groundMoveRiderMoveBefore, groundMoveMountMoveBefore;
        internal string LastGroundMoveResult { get; private set; }
        internal string LastGroundMoveExecutorId { get; private set; }
        internal bool LastGroundMoveUsedRiderTurnAdapter { get; private set; }
        internal bool LastGroundMoveSlotRestored { get; private set; }
        /* CONTROLLER_GROUND_COMPLETE */
        /* CONTROLLER_GROUND_RELEASE */
        /* CONTROLLER_GROUND_CANCEL */
        /* CONTROLLER_HANDLE_DISMOUNTING */
        // TryAdmitGroundCommand's AdmitRiderTurnMovement disposition (groundMoveOwner, the pending admission) and GroundCommandPrefix's OpenGroundOrder.
        internal void AdmitRiderTurnGroundOrderForHarness() { groundMoveOwner = relationship.PairOwner; riderTurnGroundMoveAdmissionPending = true; groundOrderOpen = true; }
        internal UnitMoveTo ActiveRiderTurnGroundMoveForHarness => activeRiderTurnGroundMove;
        internal void HandleDismountingForHarness(CleanupTrigger trigger) => HandleDismounting(trigger);
        internal HarnessRelationship RelationshipForHarness => relationship;
        private bool IsMountAlreadyInPrimaryRange(UnitEntityData target) => true;
        // Native admission of an actor for the order (production EvaluateStockActor reads its Standard, capability and target rules).
        /* CONTROLLER_EVALUATE_ACTOR */
        internal void FullCancel(string reason, bool ordinaryRetarget) => CancelCore(reason, ordinaryRetarget);
        internal bool InvalidateIfTargetInvalid(bool targetValid) => RetireOrderIfTargetInvalid(targetValid);
        internal MountedDispatchPlan PlanDispatch(UnitEntityData target, bool turnBased, bool stationary)
        {
            MountedCombatActionKind earlyAction; bool early;
            return PlanStockDispatch(relationship.Rider, relationship.Mount, target, turnBased, true, ResolveRiderPrimaryAction(),
                ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged, stationary, out earlyAction, out early);
        }
        internal bool StationaryAdmissible(UnitEntityData target) => StationaryStrikeAdmissible(target);
        internal int MoveInterrupts => relationship.Runtime.MoveInterrupts;
        /* CONTROLLER_CANCEL_CORE */
        /* CONTROLLER_MOUNT_LEFT */
        /* CONTROLLER_IF_INVALID */
        /* CONTROLLER_PLAN_DISPATCH */
        /* CONTROLLER_STATIONARY */
        // ---- round 7: the production terminal handler, drive, submission, automatic proposal and tie-break ----
        /* CONTROLLER_HANDLE_TERMINAL */
        /* CONTROLLER_DESCRIBE_FAILURE */
        /* CONTROLLER_DESCRIBE_UNEXECUTED */
        /* CONTROLLER_DRIVE */
        /* CONTROLLER_EXECUTE */
        /* CONTROLLER_PROPOSAL */
        /* CONTROLLER_TIE */

        // ---- round 5: terminal settlement, cancellation and retirement entry points; the warning boundary ----
        internal string LastFeedback;
        internal string LastStandardRefusalTrace;
        internal readonly List<string> Warnings = new List<string>();
        internal long RiderSpecificTerminalCount, PendingRefusalPresentedCount, StockAttackUnexecutedRetirementCount, AutomaticRetiredCount;
        internal string LastAutomaticRetirement;
        // PR-1 round 13 (R12-F2): pair movement is decided by the production AutomaticMovementForbidden (spliced) from the simulated units: the
        // rider's AiMovementForbidden, a completed player move, the mount's CanMove and the mount's HoldState through MountedHoldProvenance.
        // A fixture that wants restricted pair travel holds the horse (the player's Hold on the mount).
        private readonly MountedHoldProvenance mountHold = new MountedHoldProvenance();
        internal bool MovementForbidden { get { return relationship.Mount != null && relationship.Mount.HoldState; } set { if (relationship.Mount != null) relationship.Mount.HoldState = value; } }
        private readonly MountedAutomaticAttackGate automaticGate = new MountedAutomaticAttackGate();
        // The player-facing warning (production: EventBus IWarningNotificationUIHandler.HandleWarning(LastFeedback)), captured here.
        private void ShowStockFeedback() => Warnings.Add(LastFeedback);
        private void TryLog(Func<string> line) { }
        /* CONTROLLER_MOVEMENT_FORBIDDEN */
        private bool HasRiderPlayerOperation(UnitEntityData rider, UnitEntityData mount) => false;
        private bool IsTargetInRiderReach(UnitEntityData target) => false;
        internal void CancelForNewOrder() => RetireOrderForCancel("new stock hostile attack", true);
        internal void HardCancel(string reason) => RetireOrderForCancel(reason, false);
        internal void InvalidateTarget() => RetireOrderOnTargetInvalidation();
        internal bool TryRetire(bool turnBased) => TryRetireStockIntent(relationship.Rider, relationship.Mount, turnBased);
        internal void FinishTurnBased(string feedback) => FinishTurnBasedIntent(feedback, "harness");
        // HandleCommandTerminal's ownership prelude, its terminal classification over the wrapper's own facts (production
        // MountedAttackTerminalPolicy), then the production SettleOrderTerminal. capabilityIsCause is the native branch fact the
        // production transpiler records (TurnBasedCannotAct / RunningCannotAct).
        internal void Terminal(MountedPairAttackCommand command, bool capabilityIsCause, MountedAttackEndCause? causeOverride = null)
        {
            var owned = ReferenceEquals(stockIntentCommand, command);
            if (owned) stockIntentCommand = null;
            finishedCommandPendingSweep = command;
            if (activeCommand == command) activeCommand = null;
            var rule = command.LastAttackRule != null;
            var terminal = MountedAttackTerminalPolicy.Classify(new MountedAttackTerminalFacts
            {
                NativeSuccess = command.Result == UnitCommand.ResultType.Success,
                TransactionCompleted = command.Transaction.State == MountedCombatTransactionState.Completed,
                NativeAttackRuleObserved = rule,
                CompletedAttackCount = rule && command.Result == UnitCommand.ResultType.Success ? 1 : 0
            });
            SettleOrderTerminal(command, owned, terminal, causeOverride ?? command.EndCause, capabilityIsCause, rule,
                rule && command.Result == UnitCommand.ResultType.Success ? 1 : 0, command.Result.ToString(), "harness", "harness", null);
        }
        /* CONTROLLER_SETTLE_TERMINAL */
        /* CONTROLLER_FINISH_TB */
        /* CONTROLLER_CANCEL_RETIRE */
        /* CONTROLLER_INVALIDATION */
        /* CONTROLLER_TRY_RETIRE */
        /* CONTROLLER_STRIKE_ADMISSIBLE */

        internal MountedCombatController(UnitEntityData rider, UnitEntityData mount) { relationship.Rider = rider; relationship.Mount = mount; relationship.Runtime.Mount = mount; }
        private bool IsPairPlayerOrder(UnitCommand command) => command != null && command.CreatedByPlayer;
        private static string DescribeCommand(UnitCommand command) => command.GetType().Name;
        internal bool TryDrainChargeOwnership(string reason) => true;
        private void TryLogException(string phase, Exception exception) { }

        // ---- harness entry points: the same calls the production drive makes ----
        internal void BeginOrder(UnitEntityData target, bool automatic, bool targetUnconsciousWhenOrdered = false)
        {
            // PR-1 round 7 (R4-F5): bound to the fixture's current turn in turn-based combat, as the production route binds it.
            stockIntent.Begin(target, TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ? Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn : null,
                false, true, relationship.Rider, relationship.Mount, null, null, 0, automatic, targetUnconsciousWhenOrdered: targetUnconsciousWhenOrdered);
            retainedOrder.Begin(stockIntent.Generation, automatic, true);
        }
        // TryExecuteAction's acceptance of the order's wrapper (dispatch): the wrapper is the order's own, carries the supersession
        // check, and its actor's selection is the order's write.
        internal void AcceptWrapper(MountedPairAttackCommand wrapper, UnitEntityData actor, UnitEntityData target)
        {
            retainedOrder.RecordOwnIssued(stockIntent.Generation, wrapper);
            var generation = stockIntent.Generation;
            wrapper.OwnCommandIssuing = issued => retainedOrder.RecordOwnIssued(generation, issued);
            wrapper.RetainedOrderSupersededCheck = () => retainedOrder.Superseded(generation);   // exactly as TryExecuteAction wires it
            if (stockIntent.Automatic) wrapper.AutomaticOrderAdmissionLost = RetireAutomaticOrderIfNotAdmitted;   // and an automatic order's decision
            // TryExecuteAction for an ordinary order: the order's own bound proof (PR #70 R1-F7), and the explicit unconscious-target admission.
            if (!stockIntent.Automatic) AttachClickProof(wrapper, actor == relationship.Mount ? MountedCombatActionKind.MountPrimaryNatural : MountedCombatActionKind.RiderMelee, target);
            if (!stockIntent.Automatic && stockIntent.TargetUnconsciousWhenOrdered) wrapper.AdmitTargetUnconsciousWhenOrdered();
            activeCommand = wrapper; stockIntentCommand = wrapper;
            OwnManualTarget(actor, target, stockIntent.Automatic ? -1 : stockIntent.Generation);
        }
        // TryRetireStockIntent's turn-based branch and real-time tail (production: MountedCombatController.TryRetireStockIntent).
        internal bool RetireTurnBased() => RetainedOrderSuperseded() && RetireSupersededPlayerOrder();
        internal void RetireRealTime(string reason) { var command = stockIntentCommand; ClearStockAttackIntent(reason, true); RetireOrderWrapper(command, reason); }
        internal void Own(UnitEntityData actor, UnitEntityData target, long generation) => OwnManualTarget(actor, target, generation);
        internal void Clear(string reason) => ClearStockAttackIntent(reason, true);
        // Round 12 (R11-F8): the relationship's dismount (production: the order is cancelled, then the runtime's mount reference is cleared) and a
        // later binding of another mount.
        // R11-F8 (round 14): the production ownership-retention term (MountedWorldService binds it as the relationship's ownership readiness).
        /* CONTROLLER_RETAINED_PAIR_COMMANDS */
        // R11-F8 (round 14): the relationship currently holds the actor (null: any actor, i.e. the pair is mounted).
        internal bool HoldsActorForHarness(UnitEntityData actor) => relationship.State == RelationshipState.Mounted && (actor == null || ReferenceEquals(relationship.Rider, actor) || ReferenceEquals(relationship.Mount, actor));
        internal void SimulateDismountForHarness() { Cancel("dismount"); relationship.State = RelationshipState.Unmounted; relationship.Mount = null; relationship.Runtime.Mount = null; }
        internal void RebindMountForHarness(UnitEntityData mount) { relationship.Mount = mount; relationship.Runtime.Mount = mount; relationship.State = RelationshipState.Mounted; }
        internal bool HorseFollowThroughAdmissible(UnitEntityData target) => FollowThroughStrikeAdmissibleNow(target);

        /* CONTROLLER_RETIREMENT */
        /* CONTROLLER_RETAINED_SUPERSEDED */
        /* CONTROLLER_ADMIT_EXPLICIT */
        /* CONTROLLER_ADMIT_AUTOMATIC */
        // The production fields beside the decision (the proposing native action and its generation), typed on the facade's AI action.
        private Kingmaker.UnitLogic.Commands.AiActionFacade automaticOrderAction;
        // PR #70 round 2 (R1-F7..F10): the ACTUAL click-proof members (admission, take, bind, attach), the rider's click admission with the
        // fresh target-life rule and the revalidation; the harness drives them through the turn-based entry points below.
        /* CONTROLLER_CLICK_PROOF */
        /* CONTROLLER_FRESH_LIFE */
        /* CONTROLLER_RIDER_CLICK_ADMITTED */
        /* CONTROLLER_REVALIDATE */
        internal string LastRouteRevalidation { get; private set; }
        internal string LastRoutePlans { get; private set; }
        // PR #70 R1-F10 (round 3): the ACTUAL stock-request members (TryRouteMountedStockAttack, ObserveStockClick, the native request
        // observation and its Run binding); the drive and dispatch below them (TryDriveStockAttackIntent, TryExecuteAction) are the
        // spliced production members as well. A native brain proposal is not exercised by these cases.
        private readonly ObservedPlayerAttackRequest<UnitEntityData, UnitCommand> observedStockRequest = new ObservedPlayerAttackRequest<UnitEntityData, UnitCommand>();
        internal long StockAttackNativeRequestCount { get; private set; }
        internal long StockAttackIntentStartCount { get; private set; }
        internal long StockAttackFreshAfterTerminalCount { get; private set; }
        // PR #70 R6-F7 (round 7): fault injection at the commit's first step (before any retirement) and at its last step (after every effect).
        internal static bool FaultBeforeRetirement, FaultAfterEffects;
        private string MountDisplayName { get { if (FaultAfterEffects) throw new InvalidOperationException("simulated failure after the commit's effects"); return relationship.Runtime.MountDisplayName ?? "Mount"; } }
        private void ObserveDeliberateOrder() { if (FaultBeforeRetirement) throw new InvalidOperationException("simulated failure before retirement"); ObservePlayerMoveOutcome(); }
        /* CONTROLLER_STOCK_ROUTE */
        /* CONTROLLER_ACCEPT_REQUEST */
        /* CONTROLLER_STOCK_CLICK */
        /* CONTROLLER_STOCK_LIVE */
        /* CONTROLLER_STOCK_REQUESTED */
        /* CONTROLLER_BIND_REQUEST */
        private long automaticOrderActionGeneration = -1;
        /* CONTROLLER_ADMISSION_DECISION */
        // The production retirement entry an automatic wrapper is given (TryExecuteAction: command.AutomaticOrderAdmissionLost).
        internal Func<MountedPairAttackCommand, bool> AdmissionHook => RetireAutomaticOrderIfNotAdmitted;
        // ObserveAutomaticProposal's assignment at admission, for orders the harness begins directly (BeginOrder).
        internal void AdoptProposalAction(Kingmaker.UnitLogic.Commands.AiActionFacade action) { automaticOrderAction = action; automaticOrderActionGeneration = stockIntent.Generation; }
        internal long AutomaticQueuedRunCount;
        // The production queue admission's binding of the pair's current automatic command (MountedCombatController, automaticRuns.Bind).
        internal void BindAutomaticRun(MountedPairSingleAttack command) => automaticRuns.Bind(command, relationship.MountedPairGeneration);
        /* CONTROLLER_OBSERVE_ACCEPTED */
        /* CONTROLLER_OBSERVE_OUTSTANDING */
        /* CONTROLLER_OBSERVES_ACTOR */
        /* CONTROLLER_COMMAND_NEEDS_APPROACH */
        /* CONTROLLER_OWN_DELEGATED */
        /* CONTROLLER_CLICK */
        /* CONTROLLER_OWNERSHIP */
        /* CONTROLLER_CLEAR_INTENT */
        /* CONTROLLER_END_QUEUED */
        /* CONTROLLER_INTERRUPT_OWNED */
        /* CONTROLLER_SWEEP */
        /* CONTROLLER_PROBE */
        /* CONTROLLER_FOLLOW_THROUGH */
    }

    internal static class Program
    {
        private static int passes, failures;
        private static void Check(bool value, string label) { if (value) { passes++; Console.WriteLine("PASS " + label); } else { failures++; Console.WriteLine("FAIL " + label); } }

        private sealed class World
        {
            public UnitEntityData Rider, Horse, Target, Successor;
            public HarnessLogger Logger = new HarnessLogger();
            public readonly Dictionary<UnitEntityData, bool> Ready = new Dictionary<UnitEntityData, bool>();
            public bool Perceived = true;
            // R4-F2: the first carrier destination fails without displacement (native pathing refuses it).
            public bool FailFirstDestination;
            public int FailedDestinations;
            public int NativeApproachSteps;
            // PR #70 R1-F10: the mount's movement passes through the ACTUAL paired allocation (TickDelegated) when set.
            public Func<float, float> MountMoveTick;
            public int MountStalls;
            // R13-F4 (round 14): the installed denial branch of UnitMovementAgent.CanMoveInTurnBased (060018A9): a tick that grants no movement (the
            // paired hook returned zero) completes the agent's movement (CompleteMovement -> InterruptMovement/Stop: the path is released), so
            // WantsToMove becomes false. Enabled by the cases that exercise exhaustion and route loss.
            public bool NativeDenialCompletesMovement;
            // PR #70 R13-F3 (round 15): native pathfinding for a move without a ForcedPath (the native-accepted order's approach): the agent takes
            // the path native's pathfinder would return in this open fixture (the agent's Route stand-in, else the straight line to the move's point).
            public bool PathfindUnforcedMoves;
            // Round 16 (R13-F3 path pooling): native requests a path first (RequestedPath, in flight) and delivers it on a later command tick
            // (Path set, RequestedPath cleared); DeliverRequests false keeps the request in flight; ReusePath is the pooled Path object A* hands back.
            public bool RequestPathFirst, DeliverRequests = true;
            public int ZeroMountTicks;
            public Pathfinding.Path ReusePath;
            public UnitMoveTo RequestedFor;
            // Round 19 (R17-F1, R18-F2): the destination of the agent's outstanding request (installed m_Destination as PathTo compares it), and
            // the latest result of the ACTUAL TryOverrideMountTurnMovement (false: production MountMovementPrefix returns true and native
            // CanMoveInTurnBased runs).
            public Vector3? RequestedDestination;
            public bool LastHookHandled, LastHookResult;
            public int NativeDenials;
            // PR #70 R1-F6 (round 4): the engine frame's tick length for the mount (0: the fixed 0.25 m step of earlier cases).
            public float TickSeconds;
            public string LastMountTick;
            // PR #70 R6-F2 (round 7): the step the harness mover itself requested on its latest mount tick (capped at its current waypoint).
            public float LastRequestedMetres;
            // PR #70 R8-F6 (round 10): the delta the production movement hook returns to native movement for a requested frame delta.
            public Func<float, float> MountMoveDelta;
        }

        private static World NewWorld(Vector3 horse, Vector3 target, Func<Vector3, Vector3, bool> blocker, bool turnBased)
        {
            Kingmaker.Game.Instance = new Kingmaker.Game();
            TurnBased.Controllers.CombatController.TurnBased = turnBased;
            Kingmaker.View.UnitEntityView.StopMovingFault = null; UnitEntityData.CurrentSpeedFault = null;
            LineOfSightGeometry.Instance = new LineOfSightGeometry { Blocked = blocker ?? ((from, to) => false) };
            UnitAttack.NativeAttackStarts = 0;
            UnitAttack.NativeAttackRules = 0;
            UnitCommand.NativeDeadlineInterrupts = 0;
            UnitCommands.PromotionPrefix = null; UnitCommands.RunPostfix = null; UnitCommands.QueuePostfix = null;
            UnitCommands.NativeRunHoldHook = null; UnitCommands.RunThrowsBeforeInstall = null;
            Harness.NativeActorCapability.ReadFaults = null; MountedCombatController.FaultInRetirement = false;
            MountedCombatController.FaultInTransfer = false; UnitEntityData.HoldWriteFault = null; HarnessScheduler.InterruptFault = false;
            Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null; Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = null; UnitCommands.RemoveFault = null; Kingmaker.View.UnitMovementAgent.MaxSpeedFault = false;
            MountedCombatController.TransferFaultObservation = null;
            MountedCombatController.FaultBeforeRetirement = false; MountedCombatController.FaultAfterEffects = false;
            UnitCommands.ResetCounters();
            UnitAttack.ResetActs();
            UnitAttack.StandardSpends = 0;
            Harness.NativeBrainRankingReader.Ranked = null;
            Harness.SelectionManager.Instance = new Harness.SelectionManager();
            var world = new World();
            // Native-shaped attack plans (UnitAttack.Init): two attacks each, so a delivered routine continues to its next act.
            world.Rider = new UnitEntityData("rider") { Position = horse, WeaponRange = 1.5f, AttackCount = 2 };
            world.Rider.View.Corpulence = 0.4f;
            world.Horse = new UnitEntityData("horse") { Position = horse, WeaponRange = 1.0f, AttackCount = 2 };
            world.Horse.View.Corpulence = 0.9f;
            world.Horse.View.AgentASP = new Kingmaker.View.UnitMovementAgent { Unit = world.Horse, Route = (from, to) => new List<Vector3> { from, to } };
            world.Rider.Descriptor.Pet = world.Horse; world.Horse.Descriptor.Master.Value = world.Rider;   // the native pet/master link
            world.Horse.FirstWeapon = new ItemWeaponFacade { Blueprint = new WeaponBlueprintFacade { IsNatural = true, AssetGuid = "bite" } };
            PatchMethods.Controllers.Clear();
            world.Target = new UnitEntityData("hostile-1") { Position = target };
            world.Target.View.Corpulence = 0.5f;
            Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn = new TurnBased.Controllers.TurnController { Unit = world.Rider };
            world.Ready[world.Rider] = true; world.Ready[world.Horse] = true;
            return world;
        }

        // The native UnitActionController tick of one unit (UnitActionController.TickCommand order): every slot command in turn;
        // an unstarted one first runs native TickApproaching (through the ACTUAL MountedAttackApproachPrefix), then starts when
        // native ShouldStartCommand passes (ready, close, unstarted, no result); a started one ticks; then queue promotion.
        private static void NativeTick(World world, UnitEntityData unit)
        {
            Kingmaker.Game.Instance.TimeController.GameTime += TimeSpan.FromSeconds(0.1);
            foreach (var command in unit.Commands.Raw.ToArray())
            {
                if (command == null || command.IsFinished) continue;
                var move = command as UnitMoveTo;
                if (move != null) { TickMove(world, move); continue; }
                if (!command.IsStarted)
                {
                    if (PatchMethods.MountedAttackApproachPrefix(command)) { command.IsMoveUnit = true; NativeApproach(world, command); }
                    PatchMethods.MountedAttackApproachPostfix(command);   // round 19 follow-up: Harmony's postfix ends the TickApproaching scope
                    if (!command.IsFinished && world.Ready[command.Executor] && command.Result == UnitCommand.ResultType.None && command.IsUnitEnoughClose)
                        command.Start();
                }
                else command.Tick();
            }
            unit.Commands.RemoveFinishedAndUpdateQueue();
        }

        // Native approach of an unstarted command by its own executor (UnitCommand.TickApproaching -> View.MoveTo).
        private static void NativeApproach(World world, UnitCommand command)
        {
            if (command.IsUnitEnoughClose) return;
            var executor = command.Executor;
            var length = Kingmaker.Utility.GeometryUtils.MechanicsDistance(command.ApproachPoint, executor.Position);
            if (length <= command.ApproachRadius)
            {
                // In distance but out of sight: native's line-of-sight route steps around the obstruction (stand-in: north).
                executor.Position = executor.Position + new Vector3(0f, 0f, 0.25f);
                world.NativeApproachSteps++;
                return;
            }
            var offset = command.ApproachPoint - executor.Position; offset.y = 0f;
            var step = Math.Min(0.25f, length);
            if (step <= 0f) return;
            executor.Position = executor.Position + offset * (step / length);
            world.NativeApproachSteps++;
        }

        // A move in the mount's Move slot, ticked as native ticks a point command. Native proximity first (UnitCommand.
        // IsUnitEnoughClose: within ApproachRadius of ApproachPoint, NeedLoS false): a close move starts and completes WITHOUT
        // moving. Otherwise native TickApproaching follows its ForcedPath when it has one (a straight point move otherwise),
        // 0.25 m per tick, the mount turning to its travel direction; the rider is carried at its mount-local offset, which
        // turns with the mount (Unity yaw: Quaternion.Euler(0, turn, 0) applied to the offset).
        private static void TickMove(World world, UnitMoveTo move)
        {
            if (world.FailFirstDestination && world.FailedDestinations == 0 && move.Travelled.Count == 0 && !move.Point.Equals(world.Target.Position))
            {
                world.FailedDestinations++;
                move.ForceFinish(UnitCommand.ResultType.Fail);
                return;
            }
            var mover = move.Executor;
            var close = move.IsUnitEnoughClose;
            if (move.FirstProximityDecision == null) move.FirstProximityDecision = close;
            if (close) { move.CompletedWithoutMoving = move.Travelled.Count == 0; move.FinishSuccess(); return; }
            var points = move.ForcedPath?.vectorPath;
            Vector3 goal;
            if (points != null && points.Count > 0)
            {
                move.FollowedForcedPath = true;
                while (move.PathIndex < points.Count && Kingmaker.Utility.GeometryUtils.MechanicsDistance(points[move.PathIndex], mover.Position) <= 0.001f) move.PathIndex++;
                if (move.PathIndex >= points.Count) { move.FinishSuccess(); return; }
                goal = points[move.PathIndex];
                // PR #70 round 9: the agent's own steering state follows this mover (its path copied once, its cursor and heading kept in step).
                var steering = mover.View.AgentASP;
                if (steering != null)
                {
                    // Round 20 (R18-F2): native TickApproaching -> FollowPrecomputedPath -> OnPathComplete through the ACTUAL scope and hooks.
                    if (!ReferenceEquals(steering.PathOwner, move))
                    { PatchMethods.MountedAttackApproachPrefix(move); TakePath(mover, move); PatchMethods.MountedAttackApproachPostfix(move); }
                    steering.NextPointIndex = move.PathIndex; steering.NextWaypoint = new UnityEngine.Vector2(goal.x, goal.z);
                    var dx = goal.x - mover.Position.x; var dz = goal.z - mover.Position.z; var dl = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (dl > 1e-6f) steering.MoveDirection = new UnityEngine.Vector2(dx / dl, dz / dl);
                }
            }
            else
            {
                goal = move.Point;
                // PR #70 R13-F3 (round 17): native follows an unforced move along the path its pathfinder delivered, so the agent holds that path
                // (WantsToMove) while this mover moves it; the path is the straight line in this open fixture.
                var steering = mover.View.AgentASP;
                // Round 20: native PathTo(this command) and its delivery through the ACTUAL hooks.
                if (steering != null && !ReferenceEquals(steering.PathOwner, move))
                {
                    var requested = NativePathTo(world, mover, move, goal, new List<Vector3> { mover.Position, goal });
                    if (requested != null) NativeOnPathComplete(mover, requested, world.RequestedFor);
                    steering.NextPointIndex = 1;
                }
            }
            var length = Kingmaker.Utility.GeometryUtils.MechanicsDistance(goal, mover.Position);
            var step = Math.Min(mover == world.Horse && world.TickSeconds > 0f ? mover.CurrentSpeedMps * world.TickSeconds : 0.25f, length);
            if (mover == world.Horse && world.MountMoveTick != null)
            {
                world.LastRequestedMetres = step;
                step = world.MountMoveTick(step);
                if (!(step > 0f))
                {
                    world.MountStalls++;
                    if (world.NativeDenialCompletesMovement && mover.View.AgentASP != null) mover.View.AgentASP.Path = null;
                    return;
                }
            }
            var offset = goal - mover.Position; offset.y = 0f;
            var before = mover.Position;
            var yawBefore = mover.Orientation;
            mover.Position = mover.Position + offset * (step / length);
            mover.Orientation = (float)(Math.Atan2(offset.x, offset.z) * 180d / Math.PI);
            move.Travelled.Add(mover.Position);
            move.TravelledMetres += step;
            if (mover == world.Horse)
            {
                var carried = world.Rider.Position - before;
                var radians = (mover.Orientation - yawBefore) * Math.PI / 180d;
                var cos = Math.Cos(radians); var sin = Math.Sin(radians);
                var turned = new Vector3((float)(carried.x * cos + carried.z * sin), carried.y, (float)(-carried.x * sin + carried.z * cos));
                world.Rider.Position = mover.Position + turned;
                world.Rider.Orientation = mover.Orientation;
            }
        }

        private static MountedPairAttackCommand Order(World world, MountedCombatActionKind action, bool automatic)
        {
            var command = new MountedPairAttackCommand(world.Rider, world.Horse, world.Target, action, world.Logger);
            if (automatic)
            {
                // The production admission decision of a controller holding this exact automatic order (its generation, target and command);
                // the world's perception fact is the rider group's memory.
                command.MarkAutomaticOrigin();
                world.Rider.Group.Memory.Perceives = unit => world.Perceived || !ReferenceEquals(unit, world.Target);
                var owner = new MountedCombatController(world.Rider, world.Horse);
                owner.BeginOrder(world.Target, true);
                owner.AcceptWrapper(command, action == MountedCombatActionKind.MountPrimaryNatural ? world.Horse : world.Rider, world.Target);
            }
            command.PreStartMovementForbidden = () => false;
            return command;
        }

        private static void R3F1()
        {
            // Obstruction: the horse's line from the west sees the target only from z >= 0.6 (a wall north-west of it).
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f;
            foreach (var obstructed in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0f, 0f), obstructed ? wall : null, false);
                // A prerequisite occupies the horse's Standard slot; the horse-selected order is accepted only into the queue.
                var prerequisite = new HarnessStationaryCommand();
                world.Horse.Commands.Run(prerequisite);
                prerequisite.ApproachRadius = 0.1f;
                var order = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
                order.AllowPreStartApproach();
                world.Horse.Commands.Enqueue(order);
                var label = obstructed ? "obstructed" : "unobstructed control";
                var start = world.Horse.Position;
                world.Ready[world.Horse] = false;
                NativeTick(world, world.Horse);
                Check(world.Horse.Position.Equals(start) && world.NativeApproachSteps == 0 && order.CurrentMovementOwner == null,
                    "R3-F1 " + label + ": no movement while the order is only queued");
                // Promotion: the prerequisite ends; native promotes the order into the Standard slot.
                prerequisite.FinishSuccess();
                world.Horse.Commands.RemoveFinishedAndUpdateQueue();
                Check(world.Horse.Commands.Standard == order, "R3-F1 " + label + ": native promotion placed the order in its slot");
                LineOfSightGeometry.Instance.Queries = 0;
                NativeTick(world, world.Horse); // first pre-start tick after promotion (strike not yet ready)
                if (obstructed)
                {
                    Check(order.NeedLoS && LineOfSightGeometry.Instance.Queries > 0 && order.CurrentMovementOwner == order && world.NativeApproachSteps == 1 &&
                        order.PreStartMoveIssueCount == 1, "R3-F1 obstructed: the first tick classifies with the strike's sight, native approach runs and the command owns the movement");
                    var safety = 0;
                    while (!order.IsStarted && safety++ < 40) { if (safety == 6) world.Ready[world.Horse] = true; NativeTick(world, world.Horse); }
                    Check(order.IsStarted && UnitAttack.NativeAttackStarts == 1 && world.Horse.Position.z >= 0.6f && order.CurrentMovementOwner == null,
                        "R3-F1 obstructed: once sight clears and the strike is ready, native starts exactly once and the approach ownership ends");
                }
                else
                {
                    Check(world.NativeApproachSteps == 0 && order.CurrentMovementOwner == null && order.PreStartMoveIssueCount == 0,
                        "R3-F1 unobstructed control: admitted on the first tick, no approach");
                    world.Ready[world.Horse] = true;
                    NativeTick(world, world.Horse);
                    Check(order.IsStarted && UnitAttack.NativeAttackStarts == 1, "R3-F1 unobstructed control: one native start");
                }
            }
            // Cancellation during the horse's native pre-start approach releases ownership and restores the unbounded start inputs.
            var cancel = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0f, 0f), (from, to) => from.z < 0.6f, false);
            var queued = Order(cancel, MountedCombatActionKind.MountPrimaryNatural, false);
            queued.AllowPreStartApproach();
            cancel.Horse.Commands.Run(queued);
            cancel.Ready[cancel.Horse] = false;
            NativeTick(cancel, cancel.Horse);
            var stopsBefore = cancel.Horse.View.StopMovingCount;
            TurnBased.Controllers.CombatController.TurnBased = true; // leaving the real-time pre-start phase
            NativeTick(cancel, cancel.Horse);
            Check(queued.CurrentMovementOwner == null && !queued.NeedLoS && queued.ApproachRadius >= UnitCommand.InfiniteRange &&
                cancel.Horse.View.StopMovingCount == stopsBefore + 1, "R3-F1: leaving pre-start stops the horse once and restores the unbounded start inputs");
        }

        private static void R4F2()
        {
            // 1. In-range start: the hidden target is moved into reach immediately before OnStart.
            foreach (var automatic in new[] { true, false })
            foreach (var perceived in new[] { false, true })
            {
                if (!automatic && perceived) continue;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(6f, 0f, 0f), null, false);
                var order = Order(world, MountedCombatActionKind.RiderMelee, automatic);
                world.Rider.Commands.Run(order);
                world.Target.Position = new Vector3(1.5f, 0f, 0f);
                world.Perceived = perceived;
                order.Start();
                var label = (automatic ? "automatic" : "explicit") + (perceived ? " perceived" : " hidden");
                if (automatic && !perceived)
                    Check(UnitAttack.NativeAttackStarts == 0 && order.IsFinished && order.Transaction.State == MountedCombatTransactionState.Cancelled &&
                        order.EndCause == MountedAttackEndCause.Cancelled, "R4-F2 in-range start, automatic hidden target: zero attack starts, cancelled with its cause");
                else
                    Check(UnitAttack.NativeAttackStarts == 1 && !order.IsFinished, "R4-F2 in-range start control (" + label + "): one attack start");
            }
            // 2. A started approach reaching arrival after the target was hidden and moved into reach: the exact live carrier is
            //    released (one stop) and nothing is started.
            foreach (var automatic in new[] { true, false })
            foreach (var perceived in new[] { false, true })
            {
                if (!automatic && perceived) continue;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(6f, 0f, 0f), null, false);
                var order = Order(world, MountedCombatActionKind.RiderMelee, automatic);
                world.Rider.Commands.Run(order);
                order.Start(); // out of reach: approach with the exact carrier
                var carrier = order.DelegatedMove;
                NativeTick(world, world.Horse);
                world.Target.Position = new Vector3(world.Horse.Position.x + 1.5f, 0f, 0f);
                world.Perceived = perceived;
                var stops = world.Horse.View.StopMovingCount;
                NativeTick(world, world.Rider); // started approach tick reaches arrival
                var label = (automatic ? "automatic" : "explicit") + (perceived ? " perceived" : " hidden");
                if (automatic && !perceived)
                    Check(carrier != null && carrier.IsFinished && carrier.Result == UnitCommand.ResultType.Interrupt && UnitAttack.NativeAttackStarts == 0 &&
                        order.IsFinished && order.EndCause == MountedAttackEndCause.Cancelled && world.Horse.View.StopMovingCount == stops + 1,
                        "R4-F2 arrival, automatic hidden target: zero attack starts, only the exact carrier released");
                else
                {
                    if (!order.IsFinished && order.Transaction.State == MountedCombatTransactionState.Attacking) NativeTick(world, world.Rider);
                    Check(UnitAttack.NativeAttackStarts == 1, "R4-F2 arrival control (" + label + "): one attack start");
                }
            }
            // 3. A successor that replaced the carrier keeps moving when the automatic order ends for the hidden target.
            var succ = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(6f, 0f, 0f), null, false);
            var replaced = Order(succ, MountedCombatActionKind.RiderMelee, true);
            succ.Rider.Commands.Run(replaced);
            replaced.Start();
            var successor = new UnitMoveTo(new Vector3(-5f, 0f, 0f), 0.1f);
            succ.Horse.Commands.Run(successor); // a newer order replaces the carrier in the Move slot
            succ.Perceived = false;
            var stopsBeforeEnd = succ.Horse.View.StopMovingCount;
            NativeTick(succ, succ.Rider);
            Check(replaced.IsFinished && !successor.IsFinished && succ.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == successor &&
                succ.Horse.View.StopMovingCount == stopsBeforeEnd, "R4-F2: ending for a hidden target never stops a successor's movement");
        }

        // PR-1 review R1-F4: an out-of-reach real-time rider attack starts while an older stationary horse action holds the horse's
        // Standard (and older queued work is promoted before start). The carrier waits; the older actions survive; the order
        // progresses once they clear. Movement becoming forbidden during the wait ends the order cleanly with no carrier.
        private static void PR1R1F4()
        {
            foreach (var forbidden in new[] { false, true })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var cast = new HarnessStationaryCommand();
                world.Horse.Commands.Run(cast);
                var queued = new HarnessStationaryCommand();
                world.Horse.Commands.Enqueue(queued);
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                var forbid = false;
                order.PreStartMovementForbidden = () => forbid;
                world.Rider.Commands.Run(order);
                NativeTick(world, world.Rider);
                var label = forbidden ? "movement forbidden during the wait" : "older work clears";
                Check(order.IsStarted && !order.IsFinished && order.CarrierDeferred && order.DelegatedMove == null && !cast.IsFinished &&
                    world.Horse.Commands.Queue.Contains(queued), "PR-1 R1-F4 " + label + ": the started rider order waits behind the older horse action (no throw, no carrier)");
                if (forbidden)
                {
                    forbid = true;
                    NativeTick(world, world.Rider);
                    Check(order.IsFinished && order.DelegatedMove == null && !cast.IsFinished && UnitAttack.NativeAttackStarts == 0 &&
                        world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null,
                        "PR-1 R1-F4 " + label + ": the order ends cleanly, no carrier, the older action untouched");
                    continue;
                }
                cast.FinishSuccess();
                world.Horse.Commands.RemoveFinishedAndUpdateQueue();
                NativeTick(world, world.Rider);
                Check(!order.IsFinished && order.CarrierDeferred && world.Horse.Commands.Standard == queued,
                    "PR-1 R1-F4 " + label + ": the promoted older queued action still holds the horse; the order keeps waiting");
                queued.FinishSuccess();
                world.Horse.Commands.RemoveFinishedAndUpdateQueue();
                var safety = 0;
                while (!order.IsFinished && order.Transaction.State != MountedCombatTransactionState.Attacking && safety++ < 80)
                { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                if (order.Transaction.State == MountedCombatTransactionState.Attacking) NativeTick(world, world.Rider);
                Check(order.CarrierDeferralCount == 1 && !order.CarrierDeferred && UnitAttack.NativeAttackStarts == 1,
                    "PR-1 R1-F4 " + label + ": once the older work cleared the carrier was admitted and one strike was released");
            }
        }

        // PR-1 review R1-F6: the pair's carrier is released while an out-of-range Standard successor steers the horse (installed
        // directly, or promoted by the native queue sweep): cleanup must not stop the successor's movement. Control: no successor.
        private static void PR1R1F6()
        {
            foreach (var mode in new[] { "direct", "queue-promoted", "control" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Rider.Commands.Run(order);
                NativeTick(world, world.Rider);
                var carrier = order.DelegatedMove;
                var successor = new HarnessApproachingCommand(new Vector3(-6f, 0f, 0f));
                if (mode == "direct") world.Horse.Commands.Run(successor);
                if (mode == "queue-promoted")
                {
                    world.Horse.Commands.Enqueue(successor);
                    world.Horse.Commands.RemoveFinishedAndUpdateQueue();
                    Check(world.Horse.Commands.Queue.Contains(successor) && world.Horse.Commands.Standard != successor,
                        "PR-1 R1-F6 queue-promoted: before cleanup the successor is queue-only (the live carrier holds Move)");
                }
                var stopsBefore = world.Horse.View.StopMovingCount;
                order.Interrupt();
                var stops = world.Horse.View.StopMovingCount - stopsBefore;
                if (mode == "control")
                    Check(carrier != null && carrier.IsFinished && order.DelegatedMove == null && stops == 1,
                        "PR-1 R1-F6 control: with no successor the released carrier's motion is stopped once (" + stops + ")");
                else
                    Check(carrier != null && carrier.IsFinished && order.DelegatedMove == null && stops == 0 && !successor.IsFinished &&
                        world.Horse.Commands.Standard == successor,
                        "PR-1 R1-F6 " + mode + ": the carrier is released and the successor approaching from Standard keeps its movement (stops " + stops + ")");
            }
        }

        // PR-1 review R1-F7 (round 2): the horse sees the hostile point while the rider's own eye line is blocked (the rider sits
        // 0.3 m behind the horse's origin; sight requires an eye at z >= 0.6). The point carrier's distance-only arrival is
        // satisfied at the start. Production selects a goal by the rider's strike admission at candidate mount positions; the
        // facade moves straight to it (no hard-coded detour). Zero strikes while blocked or cooling; one legal start after.
        private static void PR1R1F7()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f;
            var world = NewWorld(new Vector3(1.3f, 0f, 0.7f), new Vector3(2.5f, 0f, 0.7f), wall, false);
            world.Rider.Position = world.Horse.Position + new Vector3(0f, 0f, -0.3f);
            world.Ready[world.Rider] = false;
            var horseSees = !LineOfSightGeometry.Instance.HasObstacle(world.Horse.EyePosition, world.Target.Position, 0);
            var riderSees = !LineOfSightGeometry.Instance.HasObstacle(world.Rider.EyePosition, world.Target.Position, 7);
            Check(horseSees && !riderSees, "PR-1 R1-F7 setup: the horse sees the hostile point, the rider's strike line is blocked");
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            order.AllowPreStartApproach();
            world.Rider.Commands.Run(order);
            Check(!order.IsApproachingBeforeAttack, "PR-1 R1-F8: a waiting order with no movement is not an approach (stationary windups keep it)");
            NativeTick(world, world.Rider);
            var carrier = world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
            var goal = carrier == null ? default(Vector3) : carrier.Point;
            Check(carrier != null && !carrier.NeedLoS && order.SightGoalCount == 1 && !goal.Equals(world.Target.Position) && order.IsApproachingBeforeAttack,
                "PR-1 R1-F7: production chose a goal " + goal + " by the rider's strike admission (not the hostile's point); the live pre-start movement is an approach for a group stop");
            for (var tick = 0; tick < 12; tick++) { NativeTick(world, world.Horse); NativeTick(world, world.Rider); }
            Check(carrier.TravelledMetres > 0f && !carrier.CompletedWithoutMoving && UnitAttack.NativeAttackStarts == 0 && world.Rider.EyePosition.z >= 0.6f,
                "PR-1 R1-F7: the carrier travelled " + carrier.TravelledMetres.ToString("R") + " m; the rider's eye now sees (" + world.Rider.EyePosition + "); nothing struck while cooling");
            world.Ready[world.Rider] = true;
            for (var tick = 0; tick < 6 && UnitAttack.NativeAttackStarts == 0; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
            Check(UnitAttack.NativeAttackStarts == 1, "PR-1 R1-F7: one strike starts once the rider's sight and readiness allow it");
            var control = NewWorld(new Vector3(1.3f, 0f, 0.7f), new Vector3(2.5f, 0f, 0.7f), wall, false);
            control.Rider.Position = control.Horse.Position + new Vector3(0f, 0f, -0.3f);
            var strike = new MountedPairSingleAttack(control.Target, control.Rider, control.Horse, true);
            strike.Init(control.Rider);
            var old = new UnitMoveTo(control.Target.Position, strike.DelegatedMoveApproachRadius) { NeedLoS = true };
            control.Horse.Commands.Run(old);
            NativeTick(control, control.Horse);
            Check(old.IsFinished && old.CompletedWithoutMoving && control.Rider.EyePosition.z < 0.6f,
                "PR-1 R1-F7 control: a point move needing only the horse's sight of the hostile completes without moving; the rider stays blind");
        }

        // PR-1 round-2 review: R1-F4 adoption, R1-F1 authority, R2-F1 windup, R2-F2 controller-first end, R2-F3 long wait.
        private static void PR1Round2()
        {
            // R1-F4: a live pre-start carrier, movement revoked at the start handoff: no continued or replacement carrier, no strike.
            foreach (var revoke in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                world.Ready[world.Rider] = false;
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                var forbid = false;
                order.PreStartMovementForbidden = () => forbid;
                order.AllowPreStartApproach();
                world.Rider.Commands.Run(order);
                NativeTick(world, world.Rider);
                var carrier = world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
                forbid = revoke;
                world.Ready[world.Rider] = true;
                for (var tick = 0; tick < 30 && !order.IsFinished && UnitAttack.NativeAttackStarts == 0; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                if (revoke)
                    Check(carrier != null && order.IsFinished && carrier.IsFinished && world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                        order.DelegatedMove == null && UnitAttack.NativeAttackStarts == 0,
                        "PR-1 R1-F4 adoption: movement revoked at the start handoff ends the order; its exact pre-start carrier is released, no replacement, no strike");
                else
                    Check(carrier != null && order.PreStartMoveAdoptedAtStart && UnitAttack.NativeAttackStarts == 1,
                        "PR-1 R1-F4 adoption control: with movement permitted the live carrier is adopted and one strike follows");
            }
            // R1-F1: a horse order without approach authority (follow-through) settles when its target is outside the exact strike
            // radius, also after leaving reach before start; an in-reach control strikes once.
            foreach (var kase in new[] { "just outside", "outside, moved farther before start", "in reach" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(kase == "in reach" ? 1.5f : 2.41f, 0f, 0f), null, true);
                var order = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
                order.ForbidApproach();
                world.Horse.Commands.Run(order);
                if (kase == "outside, moved farther before start") world.Target.Position = new Vector3(3.5f, 0f, 0f);
                order.Start();
                for (var tick = 0; tick < 3 && !order.IsFinished; tick++) NativeTick(world, world.Horse);
                if (kase == "in reach")
                    Check(UnitAttack.NativeAttackStarts == 1 && order.DelegatedMove == null, "PR-1 R1-F1 in-reach control: one horse strike, no carrier");
                else
                    Check(order.IsFinished && order.DelegatedMove == null && world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                        UnitAttack.NativeAttackStarts == 0 && world.Horse.View.AgentASP.FindPathCount == 0,
                        "PR-1 R1-F1 " + kase + ": the follow-through settles with no carrier and no strike");
            }
            // R2-F1: supersession and perception loss during the first strike's windup stop it before delivery; a delivered strike
            // keeps native's handling.
            foreach (var kase in new[] { "superseded", "unperceived", "delivered control" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var order = Order(world, MountedCombatActionKind.RiderMelee, kase == "unperceived");
                var superseded = false;
                order.RetainedOrderSupersededCheck = () => superseded;
                world.Rider.Commands.Run(order);
                NativeTick(world, world.Rider);
                var started = UnitAttack.NativeAttackStarts == 1 && order.Transaction.State == MountedCombatTransactionState.Attacking && !order.StrikeReleased;
                if (kase == "delivered control") { order.MarkDeliveredForHarness(); superseded = true; }
                if (kase == "superseded") superseded = true;
                if (kase == "unperceived") world.Perceived = false;
                NativeTick(world, world.Rider);
                if (kase == "delivered control")
                    Check(started && !order.IsFinished, "PR-1 R2-F1 delivered control: a delivered strike keeps running under native handling");
                else
                    Check(started && order.IsFinished && !order.StrikeReleased, "PR-1 R2-F1 " + kase + " during windup: the wrapper ends before its first delivery");
            }
            // R2-F2: the horse is the automatic striker and approaches natively; the controller ends the wrapper first (its
            // InterruptOwnedCommand): the horse's path stops, no strike; a successor already steering keeps moving.
            foreach (var successor in new[] { false, true })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0f, 0f), (from, to) => from.z < 0.6f, false);
                var order = Order(world, MountedCombatActionKind.MountPrimaryNatural, true);
                order.AllowPreStartApproach();
                world.Horse.Commands.Run(order);
                world.Ready[world.Horse] = false;
                NativeTick(world, world.Horse);
                var approaching = order.CurrentMovementOwner == order;
                if (successor) world.Horse.Commands.Run(new UnitMoveTo(new Vector3(-5f, 0f, 0f), 0.5f));
                var stopsBefore = world.Horse.View.StopMovingCount;
                world.Perceived = false;
                order.RecordEndCause(MountedAttackEndCause.Cancelled, "automatic target left perception");
                order.Interrupt();
                var stops = world.Horse.View.StopMovingCount - stopsBefore;
                Check(approaching && order.IsFinished && UnitAttack.NativeAttackStarts == 0 && order.CurrentMovementOwner == null && stops == (successor ? 0 : 1),
                    "PR-1 R2-F2 " + (successor ? "successor control" : "controller-first end") + ": the horse's own native approach is released (stops " + stops + ")");
            }
            // R2-F3: older horse work holds the slot longer than the chase allowance; the target moves meanwhile; once free, one carrier
            // toward the current target and one strike.
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var cast = new HarnessStationaryCommand();
                world.Horse.Commands.Run(cast);
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Rider.Commands.Run(order);
                for (var tick = 0; tick < 160; tick++) NativeTick(world, world.Rider);
                var waited = !order.IsFinished && order.CarrierDeferred;
                world.Target.Position = new Vector3(4f, 0f, 2f);
                cast.FinishSuccess();
                world.Horse.Commands.RemoveFinishedAndUpdateQueue();
                NativeTick(world, world.Rider);
                var carrier = order.DelegatedMove;
                var towardCurrent = carrier != null && carrier.ApproachPoint.Equals(world.Target.Position);
                var safety = 0;
                while (!order.IsFinished && order.Transaction.State != MountedCombatTransactionState.Attacking && safety++ < 80)
                { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                if (order.Transaction.State == MountedCombatTransactionState.Attacking) NativeTick(world, world.Rider);
                Check(waited && towardCurrent && UnitAttack.NativeAttackStarts == 1,
                    "PR-1 R2-F3: a 16 s wait behind older work is not chase time; the carrier heads for the target's current position and one strike follows");
            }
        }

        // ===================== PR-1 round 4 (owner instruction 2026-11-26 section 5) =====================
        private static MountedCombatController Controller(World world)
        {
            var controller = new MountedCombatController(world.Rider, world.Horse);
            KingmakerMountedCombat.Integration.PatchMethods.Controllers.Add(controller);   // the pair the click assignment hook reports to
            // Native InternalRunPrefix routes an explicit pair wrapper's queue promotion to AdmitQueuedExplicitRun; the Run postfix
            // observes what native installed (MountedAcceptedWorkRoutes.AfterRun) for the pair's two containers.
            UnitCommands.PromotionPrefix = command => !(command is MountedPairSingleAttack) || controller.AdmitQueuedExplicitRun((MountedPairSingleAttack)command);
            UnitCommands.RunPostfix = (container, command, fromQueue) =>
            {
                var route = MountedAcceptedWorkRoutes.AfterRun(fromQueue, container.Contains(command), container.Queue.Contains(command));
                if (route.HasValue) controller.ObserveAcceptedWork(container, command, route.Value);
            };
            UnitCommands.QueuePostfix = (container, command) => controller.ObserveAcceptedWork(container, command, MountedAcceptedWorkRoute.Queue);
            return controller;
        }

        // Case 2 (R2-F1): a positive control that delivers through native OnAction, supersession before the first delivery, and
        // controller retirement after delivery, in that event order through the real wrapper and the spliced controller members.
        private static void Round4DeliveryAndRetirement()
        {
            foreach (var kase in new[] { "positive control", "superseded before delivery", "retired after delivery" })
            foreach (var turnBased in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, turnBased);
                var controller = Controller(world);
                controller.BeginOrder(world.Target, false);
                var rider = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Rider.Commands.Run(rider);
                controller.AcceptWrapper(rider, world.Rider, world.Target);
                NativeTick(world, world.Rider);
                var windup = UnitAttack.NativeAttackStarts == 1 && !rider.StrikeReleased;
                if (kase == "superseded before delivery")
                {
                    // Newer player work accepted into the horse's container during the windup; the act event is due in the same tick.
                    world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                    rider.ActEventDue = true;
                    NativeTick(world, world.Rider);
                    var retired = turnBased ? controller.RetireTurnBased() : RetireRealTimeIfSuperseded(controller);
                    Check(windup && retired && !controller.HasStockAttackIntent && rider.IsFinished && UnitAttack.NativeAttackRules == 0 && !rider.StrikeReleased,
                        "PR-1 R2-F1 " + (turnBased ? "turn-based" : "real-time") + " supersession before delivery: OnTick ends the wrapper before native OnAction (deliveries " +
                        UnitAttack.NativeAttackRules + ")");
                    continue;
                }
                rider.ActEventDue = true;
                NativeTick(world, world.Rider);
                var delivered = rider.StrikeReleased && UnitAttack.NativeAttackRules == 1;
                if (kase == "positive control")
                {
                    Check(windup && delivered && !rider.IsFinished, "PR-1 R2-F1 positive control (" + (turnBased ? "turn-based" : "real-time") + "): native OnAction delivered once");
                    continue;
                }
                world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                var interruptsBefore = controller.ExternalInterruptCount;
                if (turnBased)
                {
                    // Turn-based supersession applies only to unreleased work (RetainedOrderSuperseded): the delivered strike and its order
                    // complete natively.
                    var retiredTurnBased = controller.RetireTurnBased();
                    NativeTick(world, world.Rider);
                    Check(delivered && !retiredTurnBased && !rider.IsFinished && controller.ExternalInterruptCount == interruptsBefore && UnitAttack.NativeAttackRules == 1,
                        "PR-1 R2-F1 turn-based newer work after delivery: no retirement; the released strike completes natively (no interrupt, one delivery)");
                }
                else
                {
                    controller.RetireRealTime("newer accepted player work supersedes the order");
                    NativeTick(world, world.Rider);
                    Check(delivered && !rider.IsFinished && controller.ExternalInterruptCount == interruptsBefore && !controller.HasStockAttackIntent &&
                        controller.ReleasedStrikeLeftToNativeCount == 1 && UnitAttack.NativeAttackRules == 1,
                        "PR-1 R2-F1 real-time controller retirement after delivery: the order retires (it cannot repeat); the released strike is left to native completion (no interrupt, one delivery)");
                }
            }
        }

        private static bool RetireRealTimeIfSuperseded(MountedCombatController controller)
        {
            if (!controller.Ledger.Superseded(controller.Generation)) return false;
            controller.RetireRealTime("newer accepted player work supersedes the order");
            return true;
        }

        // Case 3 (R3-F1, R3-F2): an explicit rider wrapper queued behind older rider work; newer horse work supersedes the order.
        // Controller-first and promotion-first orderings, real time and turn-based; an unrelated queued successor is preserved.
        private static void Round4QueueLifecycle()
        {
            foreach (var ordering in new[] { "controller-first", "promotion-first" })
            foreach (var turnBased in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, turnBased);
                var controller = Controller(world);
                world.Successor = new UnitEntityData("hostile-2") { Position = new Vector3(2f, 0f, 1f) };
                var older = new HarnessStationaryCommand();
                world.Rider.Commands.Run(older);
                world.Rider.CombatState.ManualTarget = null;
                controller.BeginOrder(world.Target, false);
                var wrapper = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Rider.Commands.Enqueue(wrapper);
                controller.AcceptWrapper(wrapper, world.Rider, world.Target);
                var successor = new HarnessPlayerCommand(world.Successor, UnitCommand.CommandType.Free);
                world.Rider.Commands.Enqueue(successor);
                world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                world.Rider.CombatState.ManualTarget = world.Successor;   // the player's own later selection
                var holdBefore = world.Rider.HoldState;
                if (ordering == "controller-first")
                {
                    if (turnBased) controller.RetireTurnBased(); else RetireRealTimeIfSuperseded(controller);
                }
                older.FinishSuccess();
                world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                var installed = world.Rider.Commands.Standard == wrapper || world.Rider.Commands.Contains(wrapper);
                Check(!installed && wrapper.IsFinished && wrapper.OrderRetired && !wrapper.IsStarted && UnitAttack.NativeAttackStarts == 0 &&
                    world.Rider.CombatState.ManualTarget == world.Successor && world.Rider.HoldState == holdBefore &&
                    world.Rider.Commands.Contains(successor) && !controller.HasStockAttackIntent && controller.Active == null && controller.IntentCommand == null,
                    "PR-1 R3-F1 " + ordering + " (" + (turnBased ? "turn-based" : "real-time") + "): the retired wrapper never installs or writes the selection/Hold; the unrelated successor runs; the controller keeps no wrapper reference");
            }
            // R3-F2: slot-only and queue-only retirement with no later callback: retirement lives on the wrapper; a late promotion of a
            // retired wrapper (it was re-queued by a foreign path) is still refused.
            foreach (var placement in new[] { "slot-only", "queue-only" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, true);
                var controller = Controller(world);
                controller.BeginOrder(world.Target, false);
                var wrapper = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Ready[world.Rider] = false;
                if (placement == "slot-only") world.Rider.Commands.Run(wrapper); else { world.Rider.Commands.Run(new HarnessStationaryCommand()); world.Rider.Commands.Enqueue(wrapper); }
                controller.AcceptWrapper(wrapper, world.Rider, world.Target);
                world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                controller.RetireTurnBased();
                var refused = !controller.AdmitQueuedExplicitRun(wrapper);
                Check(wrapper.IsFinished && wrapper.OrderRetired && refused && controller.Active == null && controller.IntentCommand == null &&
                    !world.Rider.Commands.Queue.Contains(wrapper),
                    "PR-1 R3-F2 " + placement + ": retirement is held on the wrapper itself (the controller keeps no reference); a late promotion is still refused");
            }
        }

        // Case 4 (R1-F11): ownership follows the actual assignment.
        private static void Round4ManualTargets()
        {
            foreach (var kase in new[] { "same-target coexisting write", "queue insertion without a write", "refused promotion", "native click after Run",
                                          "click issuing the pair's own order", "actor-specific selection before an automatic order" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller(world);
                controller.BeginOrder(world.Target, kase.StartsWith("actor-specific", StringComparison.Ordinal));
                var expectOwned = true;
                if (kase == "actor-specific selection before an automatic order")
                {
                    controller.Own(world.Rider, world.Target, -1);   // an actor-specific action outside any generation
                    expectOwned = false;
                }
                else if (kase == "click issuing the pair's own order")
                {
                    controller.BeginClickObservation(new[] { world.Rider }, false);
                    controller.Own(world.Rider, world.Target, controller.Generation);
                    // ClickUnitHandler.OnClick's own post-Run assignment, through the production call-site hook.
                    KingmakerMountedCombat.Integration.PatchMethods.ClickManualTargetAssignment(world.Rider.CombatState, world.Target);
                    controller.EndClickObservation(world.Target, true);
                }
                else
                {
                    controller.Own(world.Rider, world.Target, controller.Generation);
                    if (kase == "same-target coexisting write") { world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Free)); expectOwned = false; }
                    if (kase == "queue insertion without a write") world.Rider.Commands.Enqueue(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                    if (kase == "refused promotion")
                    {
                        world.Rider.Commands.Run(new HarnessStationaryCommand());
                        var queued = new HarnessRefusedCommand(world.Target);
                        world.Rider.Commands.Enqueue(queued);
                        ((UnitCommand)world.Rider.Commands.Standard).FinishSuccess();
                        world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                    }
                    if (kase == "native click after Run")
                    {
                        controller.BeginClickObservation(new[] { world.Rider }, false);
                        world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Move) { WritesNoTarget = true });
                        KingmakerMountedCombat.Integration.PatchMethods.ClickManualTargetAssignment(world.Rider.CombatState, world.Target);
                        controller.EndClickObservation(world.Target, true);
                        expectOwned = false;
                    }
                }
                controller.Clear("order retired");
                var selection = world.Rider.CombatState.ManualTarget;
                Check(expectOwned ? selection == null : selection == world.Target,
                    "PR-1 R1-F11 " + kase + ": after retirement the actor's selection is " + (selection == null ? "<none> (the order's own write was released)" : selection.UniqueId + " (the newer assignment survives)"));
            }
        }

        // Case 6 (R1-F7, R3-F3): the sight goal already inside the carrier's arrival radius (the reviewer's horse x 1.12 fixture),
        // pre-start and started; movement revoked on the tick the stationary strike becomes legal; still-out-of-range control.
        private static void Round4Approach()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f;
            foreach (var path in new[] { "pre-start", "started" })
            {
                var world = NewWorld(new Vector3(1.12f, 0f, 0.7f), new Vector3(2.5f, 0f, 0.7f), wall, false);
                world.Rider.Position = world.Horse.Position + new Vector3(0f, 0f, -0.3f);
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Ready[world.Rider] = path == "started";
                if (path == "pre-start") order.AllowPreStartApproach();
                world.Rider.Commands.Run(order);
                var startsWhileBlocked = 0;
                UnitMoveTo carrier = null;
                for (var tick = 0; tick < 24 && !order.IsFinished; tick++)
                {
                    NativeTick(world, world.Rider);
                    carrier = (world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo) ?? carrier;
                    NativeTick(world, world.Horse);
                    if (world.Rider.EyePosition.z < 0.6f && UnitAttack.NativeAttackStarts > 0) startsWhileBlocked++;
                    if (path == "pre-start" && world.Rider.EyePosition.z >= 0.6f) world.Ready[world.Rider] = true;
                    if (UnitAttack.NativeAttackStarts > 0) break;
                }
                Check(order.SightGoalCount >= 1 && carrier != null && carrier.TravelledMetres > MountedPairAttackCommand.SightGoalMinimumTravelMetres - 0.0001f &&
                    world.Rider.EyePosition.z >= 0.6f && startsWhileBlocked == 0 && UnitAttack.NativeAttackStarts == 1,
                    "PR-1 R1-F7 " + path + " (horse x 1.12): the goal needs movement (travelled " + (carrier == null ? "-" : carrier.TravelledMetres.ToString("R")) +
                    " m), the rider's eye clears, zero starts while blocked, one start");
            }
            foreach (var kase in new[] { "strike becomes legal", "still out of range" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                var forbid = false;
                order.PreStartMovementForbidden = () => forbid;
                world.Rider.Commands.Run(order);
                for (var tick = 0; tick < 3; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                var carrier = order.DelegatedMove;
                if (kase == "strike becomes legal") world.Target.Position = world.Horse.Position + new Vector3(1.5f, 0f, 0f);
                forbid = true;
                var before = world.Horse.Position;
                NativeTick(world, world.Rider);
                NativeTick(world, world.Horse);
                for (var tick = 0; tick < 3 && !order.IsFinished && UnitAttack.NativeAttackStarts == 0; tick++) NativeTick(world, world.Rider);
                var travelled = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, world.Horse.Position);
                if (kase == "strike becomes legal")
                    Check(carrier != null && carrier.IsFinished && order.DelegatedMove == null && travelled < 0.0001f && UnitAttack.NativeAttackStarts == 1,
                        "PR-1 R3-F3 movement revoked on the tick the strike becomes legal: the exact carrier is released, no further travel, one legal start");
                else
                    Check(carrier != null && carrier.IsFinished && order.IsFinished && order.DelegatedMove == null && UnitAttack.NativeAttackStarts == 0,
                        "PR-1 R3-F3 still out of range: movement revoked cancels the approach; the carrier is released; zero starts");
            }
        }

        // Case 7 (R2-F3): a wait beyond the native 9 s started-command deadline (represented in the facade's Tick), a moved target and a
        // freed slot; then a deliberately stalled windup reaches the ordinary bound, not extended or shortened by the wait.
        private static void Round4WaitAndClocks()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
            var cast = new HarnessStationaryCommand();
            world.Horse.Commands.Run(cast);
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            world.Rider.Commands.Run(order);
            for (var tick = 0; tick < 120; tick++) NativeTick(world, world.Rider);
            var waited = !order.IsFinished && order.CarrierDeferred && order.TimeSinceStart < 1f && order.DeferredSeconds > 11f && UnitCommand.NativeDeadlineInterrupts == 0;
            world.Target.Position = new Vector3(4f, 0f, 2f);
            cast.FinishSuccess();
            world.Horse.Commands.RemoveFinishedAndUpdateQueue();
            NativeTick(world, world.Rider);
            var carrier = order.DelegatedMove;
            var towardCurrent = carrier != null && carrier.ApproachPoint.Equals(world.Target.Position);
            var safety = 0;
            while (!order.IsFinished && UnitAttack.NativeAttackStarts == 0 && safety++ < 80) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
            Check(waited && towardCurrent && UnitAttack.NativeAttackStarts == 1 && carrier.IsFinished,
                "PR-1 R2-F3: a 12 s wait (native deadline represented) neither faults nor counts; one carrier to the target's current position; one legal start");
            var stalledTicks = 0;
            while (!order.IsFinished && stalledTicks < 200) { NativeTick(world, world.Rider); stalledTicks++; }
            var seconds = stalledTicks * 0.1f;
            Check(order.IsFinished && seconds > 8.0f && seconds < 9.0f && UnitCommand.NativeDeadlineInterrupts == 0 && UnitAttack.NativeAttackRules == 0,
                "PR-1 R2-F3: the stalled windup ends at its ordinary bound after " + seconds.ToString("0.0") + " s (8.5 s; no negative elapsed time, no carried wait)");
        }

        // ===================== PR-1 round 5 =====================
        private static bool detached;

        private static MountedCombatController Controller5(World world)
        {
            detached = false;
            var controller = Controller(world);
            // Production InternalRunPrefix: a pair wrapper's promotion goes through PatchMethods.AdmitExplicitPromotion (spliced),
            // with no owner once the pair is detached.
            UnitCommands.PromotionPrefix = command => !(command is MountedPairSingleAttack) ||
                PatchMethods.AdmitExplicitPromotion((MountedPairSingleAttack)command, detached ? null : controller);
            return controller;
        }

        private static MountedPairAttackCommand Wrapper(World world, MountedCombatController controller, MountedCombatActionKind action, bool queue = false)
        {
            var wrapper = Order(world, action, false);
            var executor = action == MountedCombatActionKind.MountPrimaryNatural ? world.Horse : world.Rider;
            if (queue) executor.Commands.Enqueue(wrapper); else executor.Commands.Run(wrapper);
            controller.AcceptWrapper(wrapper, executor, world.Target);
            return wrapper;
        }

        // The next drive's dispatch through the production plan; a horse dispatch submits a wrapper carrying the plan's approach authority.
        private static MountedPairAttackCommand NextDispatch(World world, MountedCombatController controller, bool turnBased, bool riderStandard, bool mountEligible,
            bool mountAddressable, bool pairCommandActive, bool melee, out MountedDispatchPlan plan)
        {
            plan = MountedDispatchPlanner.Plan(new MountedDispatchFacts
            {
                ExactPair = true, TargetValid = true, PairCommandActive = pairCommandActive, TurnBased = turnBased,
                RiderEligible = riderStandard && !controller.Ledger.RiderEnded(controller.Generation), MountEligible = mountEligible,
                Ranged = !melee, MountAlreadyInRange = true, PairedActivation = true, MountAddressable = mountAddressable, MeleeOrder = melee,
                Superseded = controller.Ledger.Superseded(controller.Generation), MountInReach = () => controller.HorseFollowThroughAdmissible(world.Target)
            });
            if (plan.Decision == MountedStockAttackDecision.CompleteTurnBasedIntent) controller.FinishTurnBased("no standard action left");
            if (!plan.HorseAction) return null;
            var horse = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
            if (!plan.AllowApproach) horse.ForbidApproach();
            world.Horse.Commands.Run(horse);
            controller.AcceptWrapper(horse, world.Horse, world.Target);
            horse.TerminalCallback = command => controller.Terminal(command, false);
            return horse;
        }

        private static void Deliver(World world, MountedPairAttackCommand command, UnitEntityData unit)
        {
            for (var tick = 0; tick < 4 && !command.IsStarted; tick++) NativeTick(world, unit);
            command.ActEventDue = true;
            NativeTick(world, unit);
        }

        // Cases 1 and 8 (R1-F3, R4-F5, R2-F4): the rider terminal runs the production settlement, then the production plan dispatches;
        // submissions and warnings are counted at the facade boundary.
        private static void Round5TerminalAndFeedback()
        {
            foreach (var kase in new[] { "unstarted force-finish", "interrupted first windup", "delivered control" })
            foreach (var horseState in new[] { "eligible", "spent", "ended" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var rider = Wrapper(world, controller, MountedCombatActionKind.RiderMelee);
                rider.TerminalCallback = command => controller.Terminal(command, kase != "delivered control");
                controller.LastFeedback = "rider cannot act";   // the rider terminal's evidenced refusal text
                if (kase == "unstarted force-finish") rider.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                else if (kase == "interrupted first windup") { NativeTick(world, world.Rider); rider.Interrupt(); }
                else { Deliver(world, rider, world.Rider); rider.FinishSuccess(); }
                var riderDeliveries = UnitAttack.NativeAttackRules;
                var submissionsBefore = world.Horse.Commands.RunCount;
                MountedDispatchPlan plan;
                var horse = kase == "delivered control" ? null : NextDispatch(world, controller, true, false, horseState == "eligible", horseState != "ended", false, true, out plan);
                var submissions = world.Horse.Commands.RunCount - submissionsBefore;
                if (horse != null) { Deliver(world, horse, world.Horse); horse.FinishSuccess(); }
                var horseDeliveries = UnitAttack.NativeAttackRules - riderDeliveries;
                var label = "PR-1 R1-F3/R4-F5/R2-F4 " + kase + ", " + horseState + " horse: rider deliveries " + riderDeliveries + ", horse submissions " + submissions +
                    ", warnings [" + string.Join("|", controller.Warnings.ToArray()) + "]";
                if (kase == "delivered control")
                    Check(riderDeliveries == 1 && submissions == 0 && controller.Warnings.Count == 0 && !controller.Ledger.RiderEnded(controller.Generation), label + " (a delivered rider attempt keeps the order for native's next budget; no horse recheck, no warning)");
                else if (horseState == "eligible")
                    Check(riderDeliveries == 0 && submissions == 1 && horseDeliveries == 1 && controller.Warnings.Count == 0 && !controller.Ledger.HasPendingRefusal(controller.Generation), label + " (one horse submission; its delivery settles the refusal)");
                else
                    Check(riderDeliveries == 0 && submissions == 0 && controller.Warnings.Count == 1 && controller.Warnings[0] == "rider cannot act" && !controller.HasStockAttackIntent,
                        label + " (no submission; the rider's refusal is the answer once)");
            }
            // R2-F4: the refusal pending for an admitted horse: wrapper-first supersession, controller-first supersession, replacement,
            // and a horse that loses reach before start.
            foreach (var kase in new[] { "wrapper-first supersession", "controller-first supersession", "replacement", "horse loses reach before start" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var rider = Wrapper(world, controller, MountedCombatActionKind.RiderMelee);
                rider.TerminalCallback = command => controller.Terminal(command, true);
                controller.LastFeedback = "rider cannot act";
                rider.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                world.Ready[world.Horse] = kase != "horse loses reach before start";
                MountedDispatchPlan plan;
                var horse = NextDispatch(world, controller, true, false, true, true, false, true, out plan);
                if (kase == "horse loses reach before start")
                {
                    world.Target.Position = new Vector3(3.5f, 0f, 0f);
                    world.Ready[world.Horse] = true;
                    for (var tick = 0; tick < 4 && !horse.IsFinished; tick++) NativeTick(world, world.Horse);
                }
                else
                {
                    NativeTick(world, world.Horse);   // the horse's windup has begun
                    if (kase == "replacement")
                    {
                        horse.TerminalCallback = command => controller.Terminal(command, false, MountedAttackEndCause.Replaced);
                        world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard));
                    }
                    else
                    {
                        world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Move));
                        if (kase == "controller-first supersession") controller.RetireTurnBased();
                        NativeTick(world, world.Horse);
                    }
                }
                var expected = kase == "horse loses reach before start" ? 1 : 0;
                Check(horse != null && horse.IsFinished && UnitAttack.NativeAttackRules == 0 && controller.Warnings.Count == expected &&
                    (expected == 0 || controller.Warnings[0] == "rider cannot act") && !controller.HasStockAttackIntent,
                    "PR-1 R2-F4 " + kase + ": warnings [" + string.Join("|", controller.Warnings.ToArray()) + "] (" +
                    (expected == 1 ? "the unresolved rider refusal, once" : "newer work drops the old refusal") + ")");
            }
        }

        // R3-F1 / R4-F1: general cancellation, target invalidation, a late promotion after detachment, and a fresh rider click after the
        // horse's first delivery. Installations, Hold writes and selection writes are counted per command at native Run.
        private static void Round5Cancellation()
        {
            foreach (var kase in new[] { "hard cancellation", "target invalidation", "late promotion after detachment" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                world.Successor = new UnitEntityData("hostile-2") { Position = new Vector3(2f, 0f, 1f) };
                var older = new HarnessStationaryCommand();
                world.Rider.Commands.Run(older);
                controller.BeginOrder(world.Target, false);
                var wrapper = Wrapper(world, controller, MountedCombatActionKind.RiderMelee, true);
                var successor = new HarnessPlayerCommand(world.Successor, UnitCommand.CommandType.Free);
                world.Rider.Commands.Enqueue(successor);
                if (kase == "target invalidation") controller.InvalidateTarget(); else controller.HardCancel("relationship cleanup");
                if (kase == "late promotion after detachment")
                {
                    detached = true;   // the pair is gone; a stale queue entry for the retired wrapper is promoted late
                    world.Rider.Commands.Queue.AddFirst(wrapper);
                }
                older.FinishSuccess();
                world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                Check(UnitCommands.Installs(wrapper) == 0 && UnitCommands.HoldWrites(wrapper) == 0 && UnitCommands.TargetWrites(wrapper) == 0 && wrapper.OrderRetired &&
                    wrapper.IsFinished && UnitCommands.Installs(successor) == 1 && world.Rider.Commands.Contains(successor),
                    "PR-1 R3-F1 " + kase + ": the queue-only wrapper is installed 0 times (Hold writes 0, selection writes 0); the unrelated successor is installed once");
            }
            foreach (var ordinary in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var horse = Wrapper(world, controller, MountedCombatActionKind.MountPrimaryNatural);
                horse.TerminalCallback = command => controller.Terminal(command, false);
                Deliver(world, horse, world.Horse);
                var interruptsBefore = controller.ExternalInterruptCount;
                if (ordinary) { controller.CancelForNewOrder(); controller.BeginOrder(world.Successor ?? new UnitEntityData("hostile-2"), false); }
                else controller.HardCancel("dismount");
                var stillRunning = !horse.IsFinished;
                if (ordinary) horse.FinishSuccess();   // native completion of the released routine
                MountedDispatchPlan plan;
                if (ordinary)
                    Check(stillRunning && controller.ExternalInterruptCount == interruptsBefore && UnitAttack.NativeAttackRules == 1 && controller.Active == null &&
                        controller.Ledger.IsCurrent(controller.Generation) && !controller.Ledger.RiderEnded(controller.Generation),
                        "PR-1 R4-F1 fresh rider click after the horse's first delivery: zero KMC interruptions, native completion, the retired generation does not repeat");
                else
                    Check(!stillRunning && controller.ExternalInterruptCount == interruptsBefore + 1, "PR-1 R4-F1 hard-boundary control: a dismount still ends the released command");
            }
        }

        // R1-F11: a companion's click on the pair's target assigns no pair actor; a genuine same-target rider click does.
        private static void Round5ClickAttribution()
        {
            foreach (var riderCommanded in new[] { false, true })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                var companion = new UnitEntityData("companion");
                controller.BeginOrder(world.Target, false);
                controller.Own(world.Rider, world.Target, controller.Generation);
                controller.BeginClickObservation(riderCommanded ? new[] { world.Rider } : new[] { companion }, false);
                // The installed loop: Run the unit's command (observed by the production Run prefix), then assign its ManualTarget.
                KingmakerMountedCombat.Integration.PatchMethods.ClickManualTargetAssignment(riderCommanded ? world.Rider.CombatState : companion.CombatState, world.Target);
                controller.EndClickObservation(world.Target, true);
                controller.Clear("order retired");
                var selection = world.Rider.CombatState.ManualTarget;
                Check(riderCommanded ? selection == world.Target : selection == null,
                    "PR-1 R1-F11 " + (riderCommanded ? "genuine same-target rider click" : "companion click on the pair's target") + ": after retirement the rider's selection is " +
                    (selection == null ? "<none> (the pair's own write was released)" : selection.UniqueId + " (the player's assignment survives)"));
            }
        }

        // R4-F2, R4-F3, R4-F4: approach corrections.
        private static void Round5Approach()
        {
            // R4-F2: before start, the first sight destination fails without displacement; the next attempt uses a different goal.
            {
                Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f;
                var world = NewWorld(new Vector3(1.3f, 0f, 0.7f), new Vector3(2.5f, 0f, 0.7f), wall, false);
                world.Rider.Position = world.Horse.Position + new Vector3(0f, 0f, -0.3f);
                world.Ready[world.Rider] = false;
                var failed = new List<Vector3>();
                var goals = new List<Vector3>();
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                order.AllowPreStartApproach();
                world.Rider.Commands.Run(order);
                world.FailFirstDestination = true;
                var startsEarly = 0;
                for (var tick = 0; tick < 120 && UnitAttack.NativeAttackStarts == 0; tick++)
                {
                    NativeTick(world, world.Rider);
                    var carrier = world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
                    if (carrier != null && (goals.Count == 0 || !goals[goals.Count - 1].Equals(carrier.Point))) goals.Add(carrier.Point);
                    NativeTick(world, world.Horse);
                    Kingmaker.Game.Instance.TimeController.GameTime += TimeSpan.FromSeconds(0.4);
                    if (world.Rider.EyePosition.z >= 0.6f) world.Ready[world.Rider] = true; else if (UnitAttack.NativeAttackStarts > 0) startsEarly++;
                }
                Check(goals.Count >= 2 && !goals[0].Equals(goals[1]) && world.FailedDestinations == 1 && order.FailedSightGoalCount >= 1 && startsEarly == 0 && UnitAttack.NativeAttackStarts == 1,
                    "PR-1 R4-F2: the failed pre-start sight goal " + (goals.Count > 0 ? goals[0].ToString() : "-") + " is not chosen again (next " + (goals.Count > 1 ? goals[1].ToString() : "-") +
                    "); movement on the alternative, zero early starts, one start after readiness");
            }
            // R4-F3: controller-first restricted automatic movement and a carrier-first interrupted move, each with an out-of-range control.
            foreach (var ordering in new[] { "controller-first automatic", "carrier-first interrupted move" })
            foreach (var inReach in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var controller = Controller5(world);
                var automatic = ordering == "controller-first automatic";
                controller.BeginOrder(world.Target, automatic);
                var order = Order(world, MountedCombatActionKind.RiderMelee, automatic);
                var forbid = false;
                order.PreStartMovementForbidden = () => forbid;
                world.Rider.Commands.Run(order);
                controller.AcceptWrapper(order, world.Rider, world.Target);
                for (var tick = 0; tick < 3; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                var carrier = order.DelegatedMove;
                if (inReach) world.Target.Position = world.Horse.Position + new Vector3(1.5f, 0f, 0f);
                forbid = true; controller.MovementForbidden = true;
                var before = world.Horse.Position;
                if (automatic) controller.TryRetire(false); else carrier.Interrupt();
                for (var tick = 0; tick < 4 && !order.IsFinished && UnitAttack.NativeAttackStarts == 0; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                var travelled = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, world.Horse.Position);
                Check(carrier != null && carrier.IsFinished && order.DelegatedMove == null && travelled < 0.0001f && UnitAttack.NativeAttackStarts == (inReach ? 1 : 0),
                    "PR-1 R4-F3 " + ordering + (inReach ? " (target in reach)" : " (out-of-range control)") + ": further travel " + travelled.ToString("0.###") +
                    " m, exact carrier released, rider starts " + UnitAttack.NativeAttackStarts);
            }
            // R4-F4: competing work accepted while the old wrapper waits (pre-start, mount slot free) and while its started carrier is deferred.
            foreach (var kase in new[] { "waiting to start", "deferred carrier" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                HarnessStationaryCommand cast = null;
                if (kase == "deferred carrier") { cast = new HarnessStationaryCommand(); world.Horse.Commands.Run(cast); }
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Ready[world.Rider] = kase == "deferred carrier";
                if (kase == "waiting to start") order.AllowPreStartApproach();
                world.Rider.Commands.Run(order);
                controller.AcceptWrapper(order, world.Rider, world.Target);
                if (kase == "deferred carrier") NativeTick(world, world.Rider);
                var newer = new HarnessPlayerCommand(world.Successor ?? world.Target, UnitCommand.CommandType.Standard);
                world.Rider.Commands.Enqueue(newer);
                if (cast != null) { cast.FinishSuccess(); world.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                var movesBefore = UnitCommands.MoveSubmissions;
                NativeTick(world, world.Rider);   // the wrapper ticks before any controller update
                Check(order.IsFinished && UnitCommands.MoveSubmissions == movesBefore && (world.Rider.Commands.Queue.Contains(newer) || world.Rider.Commands.Contains(newer)) && UnitAttack.NativeAttackStarts == 0,
                    "PR-1 R4-F4 " + kase + ": the superseded wrapper submits no carrier (" + (UnitCommands.MoveSubmissions - movesBefore) + ") and the newer command is preserved (queued or promoted)");
            }
        }

        // R3-F4 / R3-F5: the production dispatch plan creates the follow-through and its approach authority; probe counts per case.
        private static void Round5Dispatch()
        {
            foreach (var kase in new[] { "initially outside", "inside then outside", "continuously inside" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(kase == "initially outside" ? 2.41f : 2.0f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                controller.Ledger.MarkRiderEnded(controller.Generation);
                var submissionsBefore = world.Horse.Commands.RunCount;
                MountedDispatchPlan plan;
                var horse = NextDispatch(world, controller, true, false, true, true, false, true, out plan);
                var dispatched = world.Horse.Commands.RunCount - submissionsBefore;
                if (horse != null)
                {
                    if (kase == "inside then outside") world.Target.Position = new Vector3(3.5f, 0f, 0f);
                    for (var tick = 0; tick < 4 && !horse.IsFinished && UnitAttack.NativeAttackStarts == 0; tick++) NativeTick(world, world.Horse);
                }
                var carriers = horse == null ? 0 : horse.DelegatedMoveStarts;
                Console.WriteLine("ROUND5 dispatch " + kase + ": probes=" + controller.PlannedStrikeProbeCount + " dispatch=" + dispatched + " allowApproach=" + plan.AllowApproach +
                    " carriers=" + carriers + " starts=" + UnitAttack.NativeAttackStarts);
                if (kase == "initially outside") Check(dispatched == 0 && carriers == 0 && UnitAttack.NativeAttackStarts == 0, "PR-1 R3-F4 initially outside: no dispatch, carrier or start");
                if (kase == "inside then outside") Check(dispatched == 1 && !plan.AllowApproach && carriers == 0 && UnitAttack.NativeAttackStarts == 0 && horse.IsFinished,
                    "PR-1 R3-F4 inside then outside: the production plan dispatched a follow-through without approach authority; no carrier, no attack");
                if (kase == "continuously inside") Check(dispatched == 1 && carriers == 0 && UnitAttack.NativeAttackStarts == 1, "PR-1 R3-F4 continuously inside: one dispatch, no carrier, one start");
            }
            foreach (var kase in new[] { "active wrapper", "ranged order", "spent horse", "ended horse", "rider eligible", "eligible follow-through" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.0f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                if (kase != "rider eligible") controller.Ledger.MarkRiderEnded(controller.Generation);
                MountedDispatchPlan plan;
                NextDispatch(world, controller, true, kase == "rider eligible", kase != "spent horse", kase != "ended horse", kase == "active wrapper", kase != "ranged order", out plan);
                var expected = kase == "eligible follow-through" ? 1 : 0;
                Check(controller.PlannedStrikeProbeCount == expected && plan.MountReachProbed == (expected == 1),
                    "PR-1 R3-F5 " + kase + ": horse strike-plan probes " + controller.PlannedStrikeProbeCount);
            }
        }

        // ===================== PR-1 round 6 =====================
        // The drive's dispatch operation (spliced PlanStockDispatch) and the submission it makes (production TryExecuteAction builds the
        // wrapper; here the plan's actor and approach authority are submitted to native Run). Counts both actors' submissions.
        private static void DriveOnce(World world, MountedCombatController controller, UnitEntityData target, bool turnBased, bool stationary,
            ref int riderSubmissions, ref int horseSubmissions, out MountedDispatchPlan plan)
        {
            plan = controller.PlanDispatch(target, turnBased, stationary);
            if (plan.Decision == MountedStockAttackDecision.CompleteTurnBasedIntent) controller.FinishTurnBased("no standard action left");
            if (plan.Decision != MountedStockAttackDecision.DispatchRider && plan.Decision != MountedStockAttackDecision.DispatchMount) return;
            var action = plan.HorseAction ? MountedCombatActionKind.MountPrimaryNatural : MountedCombatActionKind.RiderMelee;
            var wrapper = new MountedPairAttackCommand(world.Rider, world.Horse, target, action, world.Logger);
            wrapper.PreStartMovementForbidden = () => false;
            if (!plan.AllowApproach) wrapper.ForbidApproach();
            var executor = plan.HorseAction ? world.Horse : world.Rider;
            executor.Commands.Run(wrapper);
            controller.AcceptWrapper(wrapper, executor, target);
            wrapper.TerminalCallback = command => controller.Terminal(command, false);
            if (plan.HorseAction) horseSubmissions++; else riderSubmissions++;
        }

        // R4-F5 (with R1-F3, R2-F4): every terminal control enters the same production drive operation afterwards; the rider's Standard
        // stays unspent after a force-finish or an interrupted windup and its capability recovers before the drive.
        private static void Round6TerminalThroughDrive()
        {
            foreach (var kase in new[] { "unstarted force-finish", "interrupted first windup", "delivered control", "retired control" })
            foreach (var horseState in new[] { "eligible", "spent", "ended" })
            {
                if ((kase == "delivered control" || kase == "retired control") && horseState != "eligible") continue;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var rider = Wrapper(world, controller, MountedCombatActionKind.RiderMelee);
                var capability = kase == "unstarted force-finish" || kase == "interrupted first windup";
                rider.TerminalCallback = command => controller.Terminal(command, capability);
                controller.LastFeedback = "rider cannot act";
                if (capability) world.Rider.Descriptor.State.CanAct = false;
                if (kase == "unstarted force-finish") rider.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                else if (kase == "interrupted first windup") { world.Rider.Descriptor.State.CanAct = true; NativeTick(world, world.Rider); world.Rider.Descriptor.State.CanAct = false; rider.Interrupt(); }
                else if (kase == "delivered control") { Deliver(world, rider, world.Rider); rider.FinishSuccess(); }
                else { NativeTick(world, world.Rider); world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Move)); controller.RetireTurnBased(); }
                world.Rider.Descriptor.State.CanAct = true;   // capability recovered; a force-finish and an undelivered windup spent nothing
                if (horseState == "spent") world.Horse.HasStandard = false;
                if (horseState == "ended") controller.unifiedTurn.Unaddressable = world.Horse;
                var riderDeliveries = UnitAttack.Acts(world.Rider);
                int riderSubmissions = 0, horseSubmissions = 0;
                MountedDispatchPlan plan;
                DriveOnce(world, controller, world.Target, true, false, ref riderSubmissions, ref horseSubmissions, out plan);
                var horse = world.Horse.Commands.Standard as MountedPairAttackCommand;
                if (horse != null && !horse.IsFinished) { Deliver(world, horse, world.Horse); horse.FinishSuccess(); }
                var label = "PR-1 R4-F5 " + kase + ", " + horseState + " horse (rider Standard " + (world.Rider.HasStandard ? "unspent" : "spent") + "): rider submissions " + riderSubmissions +
                    ", horse submissions " + horseSubmissions + ", rider deliveries " + riderDeliveries + ", horse deliveries " + UnitAttack.Acts(world.Horse) +
                    ", warnings [" + string.Join("|", controller.Warnings.ToArray()) + "] (" + plan.Decision + ")";
                if (kase == "retired control")
                    Check(riderSubmissions == 0 && horseSubmissions == 0 && controller.Warnings.Count == 0 && plan.Decision == MountedStockAttackDecision.Wait, label);
                else if (kase == "delivered control")
                    Check(riderDeliveries == 1 && riderSubmissions == 0 && horseSubmissions == 1 && UnitAttack.Acts(world.Horse) == 1 && controller.Warnings.Count == 0, label + " (paired activation: the horse joins the delivered rider order)");
                else if (horseState == "eligible")
                    Check(riderDeliveries == 0 && world.Rider.HasStandard && riderSubmissions == 0 && horseSubmissions == 1 && UnitAttack.Acts(world.Horse) == 1 && controller.Warnings.Count == 0,
                        label + " (the ended rider attempt is not redispatched; one horse attempt)");
                else
                    Check(riderDeliveries == 0 && riderSubmissions == 0 && horseSubmissions == 0 && controller.Warnings.Count == 1 && controller.Warnings[0] == "rider cannot act" && !controller.HasStockAttackIntent,
                        label + " (no submission; the rider's refusal is the answer once)");
            }
        }

        // R4-F6: the emitted Standard-charge attribution at terminal reporting, against the observed native acts.
        private static void Round6Charges()
        {
            foreach (var kase in new[] { "unstarted rider force-finish", "delivered rider", "delivered horse" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var horseStrike = kase == "delivered horse";
                var wrapper = Wrapper(world, controller, horseStrike ? MountedCombatActionKind.MountPrimaryNatural : MountedCombatActionKind.RiderMelee);
                MountedStandardCharge emitted = default(MountedStandardCharge);
                wrapper.TerminalCallback = command => emitted = command.DescribeStandardCharge();
                if (kase == "unstarted rider force-finish") wrapper.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                else { Deliver(world, wrapper, horseStrike ? world.Horse : world.Rider); wrapper.FinishSuccess(); }
                var actor = horseStrike ? world.Horse : world.Rider;
                var acted = UnitAttack.Acts(actor) == 1;
                Check(emitted.ActorId == actor.UniqueId && emitted.ActionStandardCharged == acted && emitted.RiderStandardCharged == (acted && !horseStrike) && wrapper.IsActed,
                    "PR-1 R4-F6 " + kase + ": emitted actor " + emitted.ActorId + ", riderStandardCharged " + emitted.RiderStandardCharged + ", actionStandardCharged " +
                    emitted.ActionStandardCharged + " against observed native acts " + UnitAttack.Acts(actor) + " (IsActed " + wrapper.IsActed + ")");
            }
        }

        // R4-F1: a horse Standard that approached (native TickApproaching set IsMoveUnit) and delivered; the rider clicks another hostile
        // through the complete cancellation path (CancelCore, including the mount-wide move cleanup); hard-cancellation control.
        private static void Round6Retarget()
        {
            foreach (var ordinary in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.2f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                world.Ready[world.Horse] = false;
                var horse = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
                horse.AllowPreStartApproach();
                world.Horse.Commands.Run(horse);
                controller.AcceptWrapper(horse, world.Horse, world.Target);
                horse.TerminalCallback = command => controller.Terminal(command, false);
                // The horse approaches through native TickApproaching (which sets its IsMoveUnit) until its strike is in reach, then delivers.
                for (var tick = 0; tick < 20 && !horse.IsStarted; tick++) { NativeTick(world, world.Horse); if (horse.IsMoveUnit && Kingmaker.Utility.GeometryUtils.MechanicsDistance(world.Horse.Position, world.Target.Position) <= 2.3f) world.Ready[world.Horse] = true; }
                Deliver(world, horse, world.Horse);
                var flagged = horse.IsMoveUnit && UnitAttack.Acts(world.Horse) == 1;
                var external = controller.ExternalInterruptCount;
                var moveInterrupts = controller.MoveInterrupts;
                controller.FullCancel(ordinary ? "new stock hostile attack" : "dismount", ordinary);
                if (!ordinary)
                {
                    Check(horse.IsFinished && controller.ExternalInterruptCount + controller.MoveInterrupts > external + moveInterrupts, "PR-1 R4-F1 hard-cancellation control: the delivered horse command is ended");
                    continue;
                }
                var target2 = new UnitEntityData("hostile-2") { Position = new Vector3(0f, 0f, 1.5f) };
                controller.BeginOrder(target2, false);
                var interruptedByCancel = horse.IsFinished;
                horse.FinishSuccess();   // native completion of the remaining legal routine
                int riderSubmissions = 0, horseSubmissions = 0;
                MountedDispatchPlan plan;
                DriveOnce(world, controller, target2, false, false, ref riderSubmissions, ref horseSubmissions, out plan);
                var next = world.Rider.Commands.Standard as MountedPairAttackCommand;
                Check(flagged && !interruptedByCancel && controller.ExternalInterruptCount == external && controller.MoveInterrupts == moveInterrupts && horse.Result == UnitCommand.ResultType.Success &&
                    riderSubmissions == 1 && next != null && next.TargetUnit == target2,
                    "PR-1 R4-F1 fresh rider click through CancelCore after the horse (IsMoveUnit " + flagged + ") delivered: interruptions 0 (KMC " + (controller.ExternalInterruptCount - external) +
                    ", move " + (controller.MoveInterrupts - moveInterrupts) + "), native completion, then one dispatch for the new target");
            }
        }

        // R5-F1 / R5-F2: observed invalidation of an installed unreleased wrapper (eligibility restored before it ticks), the delivered control,
        // and the invalidation of a replacement order while an earlier delivered horse command finishes.
        private static void Round6Invalidation()
        {
            foreach (var kase in new[] { "waiting wrapper", "first windup", "delivered control" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(kase == "waiting wrapper" ? 4f : 1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var rider = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Ready[world.Rider] = kase != "waiting wrapper";
                if (kase == "waiting wrapper") rider.AllowPreStartApproach();
                world.Rider.Commands.Run(rider);
                controller.AcceptWrapper(rider, world.Rider, world.Target);
                if (kase != "waiting wrapper") NativeTick(world, world.Rider);
                if (kase == "delivered control") { rider.ActEventDue = true; NativeTick(world, world.Rider); }
                world.Target.IsInState = false;   // the controller observes the invalid target first
                var retired = controller.InvalidateIfTargetInvalid(false);
                world.Target.IsInState = true;    // eligibility returns before the wrapper ticks
                var moves = UnitCommands.MoveSubmissions;
                world.Ready[world.Rider] = true;
                for (var tick = 0; tick < 4; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                if (kase == "delivered control")
                    Check(retired && !rider.IsFinished && UnitAttack.Acts(world.Rider) == 1 && rider.DescribeStandardCharge().ActionStandardCharged,
                        "PR-1 R5-F1 delivered control: the delivered strike keeps its native terminal handling");
                else
                    Check(retired && rider.IsFinished && UnitCommands.MoveSubmissions == moves && UnitAttack.Acts(world.Rider) == 0 && !rider.DescribeStandardCharge().ActionStandardCharged,
                        "PR-1 R5-F1 " + kase + ": the observed invalidation retired the installed wrapper; after eligibility returned: carriers " + (UnitCommands.MoveSubmissions - moves) +
                        ", deliveries " + UnitAttack.Acts(world.Rider) + ", spending none");
            }
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                var horse = Wrapper(world, controller, MountedCombatActionKind.MountPrimaryNatural);
                horse.TerminalCallback = command => controller.Terminal(command, false);
                Deliver(world, horse, world.Horse);
                controller.FullCancel("new stock hostile attack", true);
                var targetB = new UnitEntityData("hostile-2") { Position = new Vector3(0f, 0f, 1.5f) };
                controller.BeginOrder(targetB, false);
                var external = controller.ExternalInterruptCount; var moveInterrupts = controller.MoveInterrupts;
                var retired = controller.InvalidateIfTargetInvalid(false);
                var aRunning = !horse.IsFinished;
                horse.FinishSuccess();
                int riderSubmissions = 0, horseSubmissions = 0;
                MountedDispatchPlan plan;
                DriveOnce(world, controller, targetB, false, false, ref riderSubmissions, ref horseSubmissions, out plan);
                Check(retired && aRunning && controller.ExternalInterruptCount == external && controller.MoveInterrupts == moveInterrupts && horse.Result == UnitCommand.ResultType.Success &&
                    riderSubmissions + horseSubmissions == 0 && !controller.HasStockAttackIntent,
                    "PR-1 R5-F2 invalidating replacement order B while A's delivered horse command finishes: A interrupted 0 times and completes; B dispatched 0 times");
            }
        }

        // R1-F11 (round 6): simulated and unhandled clicks assign nothing; a genuine same-target rider assignment does.
        private static void Round6ClickAttribution()
        {
            // PR-1 round 8: the installed loop (ClickUnitHandler.OnClick IL_01D3..IL_036F) per selected unit either skips it, or Runs its
            // command and then assigns its ManualTarget at one call site (IL_0363), observed by the production hook the OnClick transpiler
            // installs there (PatchMethods.ClickManualTargetAssignment). Any other Run on the actor is not an assignment.
            foreach (var kase in new[] { "simulated click", "unhandled click", "genuine same-target rider click", "handled click that skips the selected rider",
                                          "skipped rider receiving an unrelated non-writing Run", "rider submission consumed by the cannot-attack router" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.BeginOrder(world.Target, false);
                controller.Own(world.Rider, world.Target, controller.Generation);
                world.Rider.CombatState.ManualTarget = world.Target;   // the order's own write: the rider already targets T
                controller.BeginClickObservation(new[] { world.Rider }, kase == "simulated click");
                if (kase == "skipped rider receiving an unrelated non-writing Run")
                    world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Free) { WritesNoTarget = true });   // not the click's assignment
                else if (kase == "rider submission consumed by the cannot-attack router")
                {
                    // The router consumes the rider's Run (the move goes to the mount); OnClick still assigns the rider's ManualTarget.
                    world.Horse.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Move) { WritesNoTarget = true });
                    KingmakerMountedCombat.Integration.PatchMethods.ClickManualTargetAssignment(world.Rider.CombatState, world.Target);
                }
                else if (kase != "handled click that skips the selected rider" && kase != "simulated click" && kase != "unhandled click")
                {
                    world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Standard) { WritesNoTarget = true });
                    KingmakerMountedCombat.Integration.PatchMethods.ClickManualTargetAssignment(world.Rider.CombatState, world.Target);
                }
                controller.EndClickObservation(world.Target, kase != "unhandled click");
                controller.Clear("order retired");
                var selection = world.Rider.CombatState.ManualTarget;
                var expectRetained = kase == "genuine same-target rider click" || kase == "rider submission consumed by the cannot-attack router";
                Check(expectRetained ? selection == world.Target : selection == null,
                    "PR-1 R1-F11 " + kase + " with the rider selected: after retirement the selection is " + (selection == null ? "<none> (the pair's own write was released)" : selection.UniqueId + " (the player's assignment survives)"));
            }
        }

        // R5-F3: restricted automatic travel, no wrapper yet: the order's stationary admission is the planned mounted melee strike.
        private static void Round6StationaryAutomatic()
        {
            foreach (var distance in new[] { 2.6f, 3.2f })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(distance, 0f, 0f), null, false);
                var controller = Controller5(world);
                controller.MovementForbidden = true;
                controller.BeginOrder(world.Target, true);
                var admissible = controller.StationaryAdmissible(world.Target);
                world.Ready[world.Rider] = false;
                var retired = controller.TryRetire(false);
                var before = world.Horse.Position;
                int riderSubmissions = 0, horseSubmissions = 0;
                MountedDispatchPlan plan = default(MountedDispatchPlan);
                if (!retired) DriveOnce(world, controller, world.Target, false, true, ref riderSubmissions, ref horseSubmissions, out plan);
                var startsBeforeReadiness = 0;
                for (var tick = 0; tick < 3; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); startsBeforeReadiness += UnitAttack.NativeAttackStarts; }
                world.Ready[world.Rider] = true;
                for (var tick = 0; tick < 3 && UnitAttack.NativeAttackStarts == 0; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                var travelled = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, world.Horse.Position);
                if (distance < 3f)
                    Check(admissible && !retired && riderSubmissions == 1 && !plan.AllowApproach && startsBeforeReadiness == 0 && UnitAttack.NativeAttackStarts == 1 && travelled < 0.0001f,
                        "PR-1 R5-F3 target at 2.6 m (inside the mounted strike, outside the rider-only shortcut): the order is retained, no travel, one strike only after readiness");
                else
                    Check(!admissible && retired && riderSubmissions == 0 && UnitAttack.NativeAttackStarts == 0 && travelled < 0.0001f,
                        "PR-1 R5-F3 outside-reach control (3.2 m): restricted automatic travel retires the order; no travel, no strike");
            }
        }

        // ---- PR-1 round 7: production decisions from their real entry points ----
        // The drive is production TryDriveStockAttackIntent -> PlanStockDispatch -> TryExecuteAction (CaptureContext and the production
        // evaluator; the wrapper is built and Run there and its terminal is production HandleCommandTerminal). Terminals are the
        // production ReportTerminalOnce over the production DescribeOutcome (or its fallback). Native facts come from the engine model.
        private static MountedCombatController Controller7(World world, bool turnBased)
        {
            var controller = Controller5(world);
            if (turnBased) Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn = new TurnBased.Controllers.TurnController { Unit = world.Rider, Status = TurnBased.Controllers.TurnController.TurnStatus.Preparing };
            return controller;
        }

        private static long Dispatches(MountedCombatController controller) => controller.StockAttackRiderDispatchCount + controller.StockAttackMountDispatchCount;

        // R4-F1: a legal two-attack horse routine on A. After its first delivery the rider replaces the order (production CancelCore and a new
        // order on B); the horse's remaining attack is delivered on A by the native routine, whose production OnEnded completes the
        // transaction (index == plan); one Standard spent; then the production drive dispatches once for valid B, zero for invalid B.
        // Control: a hard cancellation ends the routine after one delivery.
        private static void Round7RemainingRoutine()
        {
            foreach (var kase in new[] { "valid replacement B", "invalid replacement B", "hard cancellation" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.BeginOrder(world.Target, false);
                var horse = Wrapper(world, controller, MountedCombatActionKind.MountPrimaryNatural);
                horse.BindTerminal(controller.ProductionTerminal);
                Deliver(world, horse, world.Horse);
                var firstDelivered = horse.StruckTargets.Count == 1 && !horse.IsFinished && horse.GetAttackIndex() == 1 && horse.AllAttacks.Count == 2 && !horse.IsSingleAttack;
                var interrupts = controller.ExternalInterruptCount; var moveInterrupts = controller.MoveInterrupts;
                if (kase == "hard cancellation")
                {
                    controller.HardCancel("dismount");
                    Check(firstDelivered && horse.IsFinished && horse.StruckTargets.Count == 1 && horse.Transaction.State != MountedCombatTransactionState.Completed,
                        "PR-1 R4-F1 hard-cancellation control: a dismount ends the routine after its first delivery (no second attack, transaction not completed)");
                    continue;
                }
                var targetB = new UnitEntityData("hostile-2") { Position = new Vector3(0f, 0f, 1.5f) };
                if (kase == "invalid replacement B") targetB.IsInState = false;
                controller.FullCancel("new stock hostile attack", true);
                controller.BeginOrder(targetB, false);
                var dispatchesBefore = Dispatches(controller);
                controller.Drive();   // the horse still owns its Standard command: nothing dispatches yet
                var waitingDispatches = Dispatches(controller) - dispatchesBefore;
                horse.ActEventDue = true;
                NativeTick(world, world.Horse);   // the native routine's second act
                var outcome = horse.DeliveredOutcome;
                var spends = UnitAttack.StandardSpends;
                controller.Drive();
                var next = world.Rider.Commands.Standard as MountedPairAttackCommand;
                var dispatched = Dispatches(controller) - dispatchesBefore;
                var common = firstDelivered && waitingDispatches == 0 && horse.StruckTargets.Count == 2 && horse.StruckTargets.TrueForAll(t => t == world.Target) &&
                    horse.IsFinished && horse.Result == UnitCommand.ResultType.Success && horse.Transaction.State == MountedCombatTransactionState.Completed &&
                    outcome != null && outcome.TransactionCompleted && spends == 1 && controller.ExternalInterruptCount == interrupts && controller.MoveInterrupts == moveInterrupts;
                Console.WriteLine("  R4-F1 " + kase + ": struck=" + horse.StruckTargets.Count + " index=" + horse.GetAttackIndex() + "/" + horse.AllAttacks.Count +
                    " transaction=" + horse.Transaction.State + " delivered.completed=" + outcome?.TransactionCompleted + " spends=" + spends + " dispatched=" + dispatched +
                    " next=" + (next == null ? "<none>" : next.TargetUnit?.UniqueId) + " feedback=" + controller.LastFeedback);
                if (kase == "valid replacement B")
                    Check(common && dispatched == 1 && next != null && next.TargetUnit == targetB,
                        "PR-1 R4-F1 valid B: the horse's remaining attack lands on A through the native routine, its transaction completes (production OnEnded), one Standard spent, then one dispatch for B");
                else
                    Check(common && dispatched == 0 && !controller.HasStockAttackIntent,
                        "PR-1 R4-F1 invalid B: the horse's routine completes on A as above, then the production drive retires B with zero dispatches");
            }
        }

        // R4-F1 control: an explicit single-attack horse order (singleAttack forwarded) plans one attack; the production plan check
        // (CreateAndValidateChildAttack) accepts it with the exact mount weapon and slot; one delivery completes it.
        private static void Round8ExplicitSingleAttack()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
            var controller = Controller7(world, false);
            controller.BeginOrder(world.Target, false);
            var horse = new MountedPairAttackCommand(world.Rider, world.Horse, world.Target, MountedCombatActionKind.MountPrimaryNatural, world.Logger, true, true);
            horse.PreStartMovementForbidden = () => false;
            world.Horse.Commands.Run(horse);
            controller.AcceptWrapper(horse, world.Horse, world.Target);
            horse.BindTerminal(controller.ProductionTerminal);
            Deliver(world, horse, world.Horse);
            Console.WriteLine("  R4-F1 explicit single attack: plan=" + horse.AllAttacks.Count + " struck=" + horse.StruckTargets.Count + " result=" + horse.Result + " transaction=" + horse.Transaction.State);
            Check(horse.IsSingleAttack && horse.AllAttacks.Count == 1 && horse.StruckTargets.Count == 1 && horse.Result == UnitCommand.ResultType.Success &&
                horse.Transaction.State == MountedCombatTransactionState.Completed,
                "PR-1 R4-F1 explicit single-attack control: one planned attack, the production plan check passes, one delivery completes the order");
        }

        // R4-F6: the outcome actually delivered to the controller's production HandleCommandTerminal (production ReportTerminalOnce), for the
        // detailed description and its fallback (a native read inside DescribeOutcome fails): ActorId and both charge flags against the
        // observed native acts and the actor's Standard.
        private static void Round7DeliveredOutcome()
        {
            foreach (var fallback in new[] { false, true })
            foreach (var kase in new[] { "unstarted rider force-finish", "delivered rider attack", "delivered horse attack" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller7(world, true);
                var horseStrike = kase == "delivered horse attack";
                var actor = horseStrike ? world.Horse : world.Rider;
                controller.BeginOrder(world.Target, false);
                if (horseStrike) world.Rider.HasStandard = false;   // the rider has spent its Standard: the production plan offers the horse
                controller.Drive();
                var wrapper = actor.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R4-F6 " + kase + ": the production drive dispatched no wrapper to " + actor.UniqueId + " (" + controller.LastFeedback + ")"); continue; }
                Harness.NativeActorCapability.DescribeFaults = fallback ? (Func<UnitEntityData, bool>)(unit => unit == actor) : null;
                if (kase == "unstarted rider force-finish") wrapper.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                else if (!fallback) { Deliver(world, wrapper, actor); wrapper.ActEventDue = true; NativeTick(world, actor); }   // the whole routine, completed
                else { Deliver(world, wrapper, actor); wrapper.Interrupt(); }   // delivered once, then interrupted: DescribeOutcome reads native capability
                Harness.NativeActorCapability.DescribeFaults = null;
                var delivered = wrapper.DeliveredOutcome;
                var acted = UnitAttack.Acts(actor) > 0;
                var spent = !actor.HasStandard;
                var fallbackTaken = delivered != null && delivered.TargetId == null;
                Console.WriteLine("  R4-F6 " + kase + (fallback ? " (fallback)" : " (detailed)") + ": delivered actor=" + delivered?.ActorId + " riderCharged=" + delivered?.RiderStandardCharged +
                    " actionCharged=" + delivered?.ActionStandardCharged + " acts=" + UnitAttack.Acts(actor) + " standardSpent=" + spent + " fallbackTaken=" + fallbackTaken + " reports=" + wrapper.TerminalReports);
                Check(delivered != null && ReferenceEquals(delivered, controller.LastOutcome) && wrapper.TerminalReports == 1 && fallbackTaken == fallback &&
                    delivered.ActorId == actor.UniqueId && delivered.ActionStandardCharged == acted && delivered.ActionStandardCharged == spent &&
                    delivered.RiderStandardCharged == (acted && !horseStrike),
                    "PR-1 R4-F6 " + kase + " (" + (fallback ? "fallback" : "detailed") + " outcome): the delivered outcome names the actor and its charges match the observed acts and Standard");
            }
        }

        // R4-F5: turn-based orders bound to the fixture turn. A rider terminal (force-finish before start, or an interrupted first windup)
        // goes through production ReportTerminalOnce/HandleCommandTerminal; the unspent rider recovers; the next production drive submits
        // (or finally refuses) from the current paired activation: eligible horse, spent horse, ended horse; delivered and retired controls.
        private static void Round7TerminalToDispatch()
        {
            var terminals = new[] { "force-finish before start", "interrupted first windup", "delivered control", "retired control" };
            foreach (var terminal in terminals)
            foreach (var horseState in new[] { "eligible horse", "spent horse", "ended horse" })
            {
                if ((terminal == "delivered control" || terminal == "retired control") && horseState != "eligible horse") continue;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller7(world, true);
                if (horseState == "spent horse") world.Horse.HasStandard = false;
                if (horseState == "ended horse") controller.unifiedTurn.Unaddressable = world.Horse;
                controller.BeginOrder(world.Target, false);
                controller.Drive();
                var rider = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (rider == null) { Check(false, "PR-1 R4-F5 " + terminal + "/" + horseState + ": no rider dispatch (" + controller.LastFeedback + ")"); continue; }
                var riderDispatches = controller.StockAttackRiderDispatchCount;
                var warningsBefore = controller.Warnings.Count;   // the window opens before the terminal
                // The installed engine's sites for an actor that cannot act: native reads unit.CanAct and the transpiled site hands that
                // observation to the production hook (NativeCommandTerminalBranches.CaptureCanAct), which classifies and records the branch
                // (TurnBasedForceFinish / InterruptAtRunningSite) before native ends the command.
                if (terminal == "force-finish before start")
                {
                    world.Rider.Descriptor.State.CanAct = false;
                    var observation = KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.CaptureCanAct(world.Rider.Descriptor.State.CanAct);
                    KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.TurnBasedForceFinish(rider, UnitCommand.ResultType.Success, observation);
                }
                else if (terminal == "interrupted first windup")
                {
                    NativeTick(world, world.Rider);
                    world.Rider.Descriptor.State.CanAct = false;
                    var observation = KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.CaptureCanAct(world.Rider.Descriptor.State.CanAct);
                    KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.InterruptAtRunningSite(rider, true, observation);
                }
                else if (terminal == "delivered control") { Deliver(world, rider, world.Rider); rider.ActEventDue = true; NativeTick(world, world.Rider); }
                else controller.FullCancel("dismount", false);
                world.Rider.Descriptor.State.CanAct = true;   // the unspent rider recovers before the next drive
                controller.Drive();
                var horse = world.Horse.Commands.Standard as MountedPairAttackCommand;
                if (horse != null && horseState == "eligible horse") { Deliver(world, horse, world.Horse); horse.ActEventDue = true; NativeTick(world, world.Horse); }
                var routine = world.Horse.AttackCount;   // a full native routine delivers its whole plan
                var riderActs = UnitAttack.Acts(world.Rider); var horseActs = UnitAttack.Acts(world.Horse);
                var warnings = controller.Warnings.Count - warningsBefore;   // from before the terminal until after the next drive
                var riderResubmissions = controller.StockAttackRiderDispatchCount - riderDispatches;
                var delivered = rider.DeliveredOutcome;
                Console.WriteLine("  R4-F5 " + terminal + " / " + horseState + ": riderTerminal=" + delivered?.Result + "/" + delivered?.EndCause + "/" + delivered?.NativeTerminalBranch +
                    " riderResubmissions=" + riderResubmissions + " riderSubmissions=" + riderDispatches +
                    " horseSubmissions=" + controller.StockAttackMountDispatchCount + " riderActs=" + riderActs + " horseActs=" + horseActs + " warnings=" + warnings +
                    " feedback=" + controller.LastFeedback + " intent=" + controller.HasStockAttackIntent);
                if (terminal == "retired control")
                    Check(controller.StockAttackMountDispatchCount == 0 && horseActs == 0 && riderActs == 0 && !controller.HasStockAttackIntent,
                        "PR-1 R4-F5 retired control: a hard cancellation before delivery submits nothing further (horse submissions 0, deliveries 0)");
                else if (terminal == "delivered control")
                    Check(riderActs == world.Rider.AttackCount && controller.StockAttackMountDispatchCount == 1 && horseActs == routine && warnings == 0,
                        "PR-1 R4-F5 delivered control: the delivered rider's terminal is followed by one horse submission and one horse delivery, no warning");
                else if (horseState == "eligible horse")
                    Check(riderActs == 0 && riderResubmissions == 0 && controller.StockAttackMountDispatchCount == 1 && horseActs == routine && warnings == 0 && delivered != null &&
                        !delivered.RiderStandardCharged && delivered.NativeTerminalBranch != MountedNativeTerminalBranch.None,
                        "PR-1 R4-F5 " + terminal + " / eligible horse: the unspent rider is not charged; the production drive submits the horse once and it delivers once; no warning");
                else
                    Check(riderActs == 0 && riderResubmissions == 0 && controller.StockAttackMountDispatchCount == 0 && horseActs == 0 && warnings == 1 && !controller.HasStockAttackIntent,
                        "PR-1 R4-F5 " + terminal + " / " + horseState + ": no horse submission or delivery; the production refusal is presented once as a warning and the order ends");
            }
        }

        // R5-F3: travel-restricted automatic proposals start at the production decision (ObserveAutomaticProposal), with no order supplied. The
        // rider's Standard is unavailable for the first drive (no wrapper may strike), then readiness returns and the first act follows.
        private static void Round7RestrictedProposal()
        {
            foreach (var distance in new[] { 2.6f, 3.2f })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(distance, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = true;
                // Neither actor's Standard is available yet (both cooldowns pending): no strike may land before readiness.
                world.Rider.HasStandard = false; world.Horse.HasStandard = false;
                world.Ready[world.Rider] = false; world.Ready[world.Horse] = false;
                var before = world.Horse.Position;
                controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
                var admitted = controller.AutomaticAdmittedCount == 1; var refused = controller.AutomaticRefusedCount == 1;
                Console.WriteLine("  R5-F3 trace after proposal: " + controller.LastStockAttackObservation + " feedback=" + controller.LastFeedback);
                var actsBeforeReadiness = 0;
                for (var tick = 0; tick < 3; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); controller.Drive(); actsBeforeReadiness += UnitAttack.Acts(world.Rider); }
                var retained = controller.HasStockAttackIntent;
                world.Rider.HasStandard = true; world.Ready[world.Rider] = true;
                world.Horse.HasStandard = true; world.Ready[world.Horse] = true;
                for (var tick = 0; tick < 6 && UnitAttack.Acts(world.Rider) == 0; tick++)
                {
                    controller.Drive();
                    var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                    if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var travelled = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, world.Horse.Position);
                Console.WriteLine("  R5-F3 " + distance + " m: admitted=" + admitted + " refused=" + refused + " (" + controller.LastAutomaticRefusal + ") retained=" + retained +
                    " actsBeforeReadiness=" + actsBeforeReadiness + " acts=" + UnitAttack.Acts(world.Rider) + " travelled=" + travelled);
                if (distance < 3f)
                    Check(admitted && !refused && retained && actsBeforeReadiness == 0 && UnitAttack.Acts(world.Rider) == 1 && travelled < 0.0001f,
                        "PR-1 R5-F3 2.6 m: the production proposal decision accepts; the order is retained through unavailability; no travel; one stationary delivery after readiness");
                else
                    Check(!admitted && refused && !controller.HasStockAttackIntent && UnitAttack.Acts(world.Rider) == 0 && travelled < 0.0001f,
                        "PR-1 R5-F3 3.2 m: the production proposal decision refuses; no travel and no delivery");
            }
        }

        // R6-F2: two equal-ranked perceived targets under restricted travel. The nearer one has the brain's cached sight but blocked command
        // sight; the farther, native-proposed one has clear command sight: the farther is kept and struck. With both clear, the nearer wins.
        private static void Round7TieBreak()
        {
            foreach (var nearerBlocked in new[] { true, false })
            {
                var proposedPosition = new Vector3(2.2f, 0f, 0f);
                var nearerPosition = new Vector3(0f, 0f, 1.6f);
                Func<Vector3, Vector3, bool> blocker = (from, to) => nearerBlocked && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, nearerPosition) < 0.3f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), proposedPosition, blocker, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = true;
                var nearer = new UnitEntityData("hostile-near") { Position = nearerPosition };
                world.Rider.CachedSight = unit => true;   // the brain's cached sight admits both
                Harness.NativeBrainRankingReader.Ranked = new List<Harness.NativeBrainRankingReader.Candidate>
                {
                    new Harness.NativeBrainRankingReader.Candidate { Unit = nearer, Weight = 0f, Distance = world.Horse.DistanceTo(nearer) },
                    new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Horse.DistanceTo(world.Target) }
                };
                controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
                var selected = controller.IntentTarget;
                for (var tick = 0; tick < 6 && UnitAttack.NativeAttackRules == 0; tick++)
                {
                    controller.Drive();
                    var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                    if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
                Console.WriteLine("  R6-F2 nearerBlocked=" + nearerBlocked + ": selected=" + selected?.UniqueId + " tieBreaks=" + controller.AutomaticTieBreakCount +
                    " struck=" + (struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray())) + " refusal=" + controller.LastAutomaticRefusal);
                if (nearerBlocked)
                    Check(selected == world.Target && controller.AutomaticTieBreakCount == 0 && struck != null && struck.Count == 1 && struck[0] == world.Target,
                        "PR-1 R6-F2 nearer candidate with cached sight but blocked command sight: the legal native proposal is kept and struck once from where the pair stands");
                else
                    Check(selected == nearer && controller.AutomaticTieBreakCount == 1 && struck != null && struck.Count == 1 && struck[0] == nearer,
                        "PR-1 R6-F2 control, both clear: the nearer equal-ranked candidate is selected and struck once");
            }
        }

        // R7-F1 / R7-F2: two equal-ranked perceived candidates in reach; production ObserveAutomaticProposal with ResolveEqualRankTie and
        // the domain NativeAttackTieBreak.Choose. Native sight (cached rider.HasLOS) applies where the rider is held or the target is marked
        // unreachable; the stationary mounted strike (command sight) applies under restricted pair travel.
        private static void Round8TieBreakSight()
        {
            var cases = new[] {
                "R7-F1 native proposes the nearer blocked target; the farther passes both sights",
                "R7-F1 both blocked",
                "R7-F2 held rider: cached sight false only for the nearer",
                "R7-F2 mount-only restriction: cached sight false for the nearer",
                "R7-F2 held rider: both clear" };
            foreach (var kase in cases)
            {
                var fartherPosition = new Vector3(2.2f, 0f, 0f);
                var nearerPosition = new Vector3(0f, 0f, 1.6f);
                var commandBlockedNearer = kase.StartsWith("R7-F1", StringComparison.Ordinal);
                var commandBlockedFarther = kase == "R7-F1 both blocked";
                Func<Vector3, Vector3, bool> blocker = (from, to) =>
                    commandBlockedNearer && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, nearerPosition) < 0.3f ||
                    commandBlockedFarther && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, fartherPosition) < 0.3f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), fartherPosition, blocker, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = true;   // restricted pair travel
                world.Rider.AiMovementForbidden = kase.Contains("held rider");
                var nearer = new UnitEntityData("hostile-near") { Position = nearerPosition };
                world.Rider.CachedSight = unit => !(kase.Contains("cached sight false") && unit == nearer);
                Harness.NativeBrainRankingReader.Ranked = new List<Harness.NativeBrainRankingReader.Candidate>
                {
                    new Harness.NativeBrainRankingReader.Candidate { Unit = nearer, Weight = 0f, Distance = world.Horse.DistanceTo(nearer) },
                    new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Horse.DistanceTo(world.Target) }
                };
                var proposed = kase.StartsWith("R7-F1", StringComparison.Ordinal) ? nearer : world.Target;
                controller.Propose(new UnitAttack(proposed) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
                var selected = controller.IntentTarget;
                for (var tick = 0; tick < 6 && UnitAttack.NativeAttackRules == 0 && controller.HasStockAttackIntent; tick++)
                {
                    controller.Drive();
                    var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                    if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
                var struckText = struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray());
                Console.WriteLine("  " + kase + ": selected=" + (selected?.UniqueId ?? "<none>") + " struck=" + struckText + " refused=" + controller.AutomaticRefusedCount + " (" + controller.LastAutomaticRefusal + ")");
                UnitEntityData expected = kase == "R7-F1 both blocked" ? null :
                    kase == "R7-F2 mount-only restriction: cached sight false for the nearer" || kase == "R7-F2 held rider: both clear" ? nearer : world.Target;
                if (expected == null)
                    Check(selected == null && controller.AutomaticRefusedCount == 1 && UnitAttack.NativeAttackRules == 0, "PR-1 " + kase + ": the proposal is refused; no delivery");
                else
                    Check(selected == expected && struck != null && struck.Count >= 1 && struck[0] == expected && Kingmaker.Utility.GeometryUtils.MechanicsDistance(world.Horse.Position, Vector3.zero) < 0.0001f,
                        "PR-1 " + kase + ": " + expected.UniqueId + " is selected and receives the stationary strike");
            }
        }

        // R8-F1: production ObserveAutomaticProposal with the brain's equal-ranked pair. A mobile real-time pair; native proposes reachable A
        // (its disliked target) and B is marked unreachable with cached sight. Out of action range, B fails native admission (range AND
        // sight) and cannot replace A: A owns the order and gets the dispatch and approach, B nothing. In range, B is valid (the in-range
        // unreachable case) and, nearer, takes the order and the strike.
        private static void Round9NativeAdmission()
        {
            foreach (var kase in new[] { "out of range: disliked reachable A proposed, unreachable B in sight", "in range: unreachable B in sight is valid and nearer" })
            {
                var inRange = kase.StartsWith("in range", StringComparison.Ordinal);
                var aPosition = inRange ? new Vector3(2.2f, 0f, 0f) : new Vector3(5f, 0f, 0f);
                var bPosition = inRange ? new Vector3(0f, 0f, 1.6f) : new Vector3(0f, 0f, 5f);
                var world = NewWorld(new Vector3(0f, 0f, 0f), aPosition, null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;   // ordinary automatic advance
                var b = new UnitEntityData("hostile-b") { Position = bPosition };
                world.Rider.CachedSight = unit => true;
                world.Rider.CombatState.AIData.UnreachableUnits.Add(b);
                if (!inRange) world.Rider.CombatState.AIData.DislikedTarget = world.Target;
                var weight = inRange ? 0f : -0.001f * 5f;   // native FillWeight: in range 0, outside -0.001 x distance (equal here)
                Harness.NativeBrainRankingReader.Ranked = new List<Harness.NativeBrainRankingReader.Candidate>
                {
                    new Harness.NativeBrainRankingReader.Candidate { Unit = b, Weight = weight, Distance = world.Rider.DistanceTo(b) },
                    new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = weight, Distance = world.Rider.DistanceTo(world.Target) }
                };
                var moves = UnitCommands.MoveSubmissions;
                controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
                var selected = controller.IntentTarget;
                var wrapperTargets = new List<string>();
                for (var tick = 0; tick < 8 && UnitAttack.NativeAttackRules == 0 && controller.HasStockAttackIntent; tick++)
                {
                    controller.Drive();
                    foreach (var actor in new[] { world.Rider, world.Horse })
                    {
                        var wrapper = actor.Commands.Standard as MountedPairAttackCommand;
                        if (wrapper != null && !wrapperTargets.Contains(actor.UniqueId + ">" + wrapper.TargetUnit?.UniqueId)) wrapperTargets.Add(actor.UniqueId + ">" + wrapper.TargetUnit?.UniqueId);
                        if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                    }
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
                var struckText = struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray());
                var bDispatched = wrapperTargets.Exists(t => t.EndsWith(">" + b.UniqueId, StringComparison.Ordinal));
                Console.WriteLine("  R8-F1 " + kase + ": selected=" + selected?.UniqueId + " tieBreaks=" + controller.AutomaticTieBreakCount + " wrappers=" + string.Join(";", wrapperTargets.ToArray()) +
                    " moves=" + (UnitCommands.MoveSubmissions - moves) + " horse=" + world.Horse.Position + " struck=" + struckText);
                if (!inRange)
                    Check(selected == world.Target && controller.AutomaticTieBreakCount == 0 && !bDispatched && wrapperTargets.Count >= 1 &&
                        wrapperTargets.TrueForAll(t => t.EndsWith(">" + world.Target.UniqueId, StringComparison.Ordinal)) && (struck == null || !struck.Contains(b)) &&
                        world.Horse.DistanceTo(b) >= 5f - 0.0001f,
                        "PR-1 R8-F1 out-of-range unreachable B with sight cannot replace the brain's reachable (disliked) proposal A: A owns the order and its dispatch, B gets no dispatch, carrier or approach");
                else
                    Check(selected == b && controller.AutomaticTieBreakCount == 1 && struck != null && struck.Count >= 1 && struck[0] == b && UnitCommands.MoveSubmissions == moves,
                        "PR-1 R8-F1 control: an unreachable target already within native action range and sight stays valid; nearer, it takes the order and is struck without travel");
            }
        }

        // R9-F1: the round-9 in-range control with both Standards cooling: unreachable B (in action range and sight, nearer) takes the order.
        // Before the first dispatch B moves outside its action range while still perceived and marked unreachable: the production drive
        // releases the order with no wrapper, carrier or spending for B, and a fresh proposal of reachable A is then accepted and driven.
        private static void Round10RetainedNativeAdmission()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0f, 0f), null, false);
            var controller = Controller7(world, false);
            controller.MovementForbidden = false;
            var b = new UnitEntityData("hostile-b") { Position = new Vector3(0f, 0f, 1.6f) };
            world.Rider.CachedSight = unit => true;
            world.Rider.CombatState.AIData.UnreachableUnits.Add(b);
            world.Rider.HasStandard = false; world.Horse.HasStandard = false;   // both Standards cooling
            Func<List<Harness.NativeBrainRankingReader.Candidate>> ranking = () => new List<Harness.NativeBrainRankingReader.Candidate>
            {
                new Harness.NativeBrainRankingReader.Candidate { Unit = b, Weight = 0f, Distance = world.Rider.DistanceTo(b) },
                new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Rider.DistanceTo(world.Target) }
            };
            Harness.NativeBrainRankingReader.Ranked = ranking();
            controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
            var selected = controller.IntentTarget;
            controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse);   // cooling: nothing dispatched yet
            var waitingWrapper = world.Rider.Commands.Standard as MountedPairAttackCommand ?? world.Horse.Commands.Standard as MountedPairAttackCommand;
            var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends;
            b.Position = new Vector3(0f, 0f, 4f);   // outside the action range, still perceived and still marked unreachable
            world.Rider.HasStandard = true; world.Horse.HasStandard = true;   // readiness returns
            controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse);
            var wrapperForB = new[] { world.Rider, world.Horse }.Any(actor => (actor.Commands.Standard as MountedPairAttackCommand)?.TargetUnit == b && !(actor.Commands.Standard as MountedPairAttackCommand).IsFinished);
            var released = !controller.HasStockAttackIntent;
            var bMoves = UnitCommands.MoveSubmissions - moves; var bSpends = UnitAttack.StandardSpends - spends;
            var retirement = controller.LastAutomaticRetirement;
            Harness.NativeBrainRankingReader.Ranked = new List<Harness.NativeBrainRankingReader.Candidate>
            {
                new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Rider.DistanceTo(world.Target) },
                new Harness.NativeBrainRankingReader.Candidate { Unit = b, Weight = -0.004f, Distance = world.Rider.DistanceTo(b) }
            };
            controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
            var reselected = controller.IntentTarget;
            for (var tick = 0; tick < 6 && UnitAttack.NativeAttackRules == 0 && controller.HasStockAttackIntent; tick++)
            {
                controller.Drive();
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                NativeTick(world, world.Rider); NativeTick(world, world.Horse);
            }
            var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
            Console.WriteLine("  R9-F1: selected=" + selected?.UniqueId + " waitingWrapper=" + (waitingWrapper?.TargetUnit?.UniqueId ?? "<none>") + " released=" + released + " (" + retirement + ") wrapperForB=" + wrapperForB +
                " moves=" + bMoves + " spends=" + bSpends + " reselected=" + reselected?.UniqueId + " struck=" + (struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray())));
            Check(selected == b && waitingWrapper == null && released && retirement == "automatic target outside native admission" && !wrapperForB && bMoves == 0 && bSpends == 0 &&
                reselected == world.Target && struck != null && struck.Count >= 1 && struck[0] == world.Target,
                "PR-1 R9-F1 a retained automatic order on an unreachable target that leaves its native action range is released before dispatch (no wrapper, carrier or spending for it); reachable A is then accepted and struck");
        }

        // R9-F2: a held pair, two equal-ranked perceived enemies in range with clear cached sight; native proposes non-disliked A. With A's
        // mounted command sight blocked and disliked B clear, B is the only legal choice: selected and struck once without travel. With A
        // clear again, the legal non-disliked proposal A keeps priority over disliked B.
        private static void Round10DislikeAfterAdmission()
        {
            foreach (var aBlocked in new[] { true, false })
            {
                var aPosition = new Vector3(2.2f, 0f, 0f);
                var bPosition = new Vector3(0f, 0f, 1.6f);
                Func<Vector3, Vector3, bool> blocker = (from, to) => aBlocked && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, aPosition) < 0.3f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), aPosition, blocker, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = true;   // the held pair
                var b = new UnitEntityData("hostile-disliked") { Position = bPosition };
                world.Rider.CachedSight = unit => true;
                world.Rider.CombatState.AIData.DislikedTarget = b;
                Harness.NativeBrainRankingReader.Ranked = new List<Harness.NativeBrainRankingReader.Candidate>
                {
                    new Harness.NativeBrainRankingReader.Candidate { Unit = b, Weight = 0f, Distance = world.Rider.DistanceTo(b) },
                    new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Rider.DistanceTo(world.Target) }
                };
                var moves = UnitCommands.MoveSubmissions;
                controller.Propose(new UnitAttack(world.Target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false }, false);
                var selected = controller.IntentTarget;
                for (var tick = 0; tick < 6 && UnitAttack.NativeAttackRules == 0 && controller.HasStockAttackIntent; tick++)
                {
                    controller.Drive();
                    var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                    if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
                Console.WriteLine("  R9-F2 aBlocked=" + aBlocked + ": selected=" + selected?.UniqueId + " tieBreaks=" + controller.AutomaticTieBreakCount + " struck=" +
                    (struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray())) + " moves=" + (UnitCommands.MoveSubmissions - moves) + " refusal=" + controller.LastAutomaticRefusal);
                if (aBlocked)
                    Check(selected == b && controller.AutomaticTieBreakCount == 1 && struck != null && struck.Count == 1 && struck[0] == b && UnitCommands.MoveSubmissions == moves,
                        "PR-1 R9-F2 held pair, A's command sight blocked: the disliked B is the only legal stationary choice and is struck once without travel");
                else
                    Check(selected == world.Target && controller.AutomaticTieBreakCount == 0 && struck != null && struck.Count == 1 && struck[0] == world.Target,
                        "PR-1 R9-F2 control, A clear: the legal non-disliked native proposal A keeps priority over the nearer disliked B");
            }
        }

        // R8-F2: an automatic stationary attack (restricted travel) waits to deliver; its target stays in group memory but becomes unconscious
        // and is displaced outside stationary reach. Controller first (production drive), wrapper first (its own tick) and queued promotion
        // (production AdmitQueuedAutomaticRun) all record TargetInvalid with the same reason and code, no movement, no spending. A valid
        // target that merely leaves reach keeps the hold-out-of-reach retirement.
        private static void Round9InvalidationBeforeGeometry()
        {
            var results = new Dictionary<string, string>();
            foreach (var kase in new[] { "controller first", "wrapper first", "queued promotion", "valid target out of reach (control)" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = true;   // restricted pair travel: a stationary strike
                MountedPairAttackCommand wrapper;
                controller.BeginOrder(world.Target, true);
                if (kase == "queued promotion")
                {
                    // Production queue promotion: InternalRunPrefix routes an automatic-origin pair command to AdmitQueuedAutomaticRun.
                    UnitCommands.PromotionPrefix = command => !(command is MountedPairSingleAttack) || (((MountedPairSingleAttack)command).IsAutomaticOrigin
                        ? controller.AdmitQueuedAutomaticRun((MountedPairSingleAttack)command)
                        : KingmakerMountedCombat.Integration.PatchMethods.AdmitExplicitPromotion((MountedPairSingleAttack)command, controller));
                    var blocker = new HarnessStationaryCommand();
                    world.Rider.Commands.Run(blocker);
                    wrapper = Order(world, MountedCombatActionKind.RiderMelee, false);
                    wrapper.MarkAutomaticOrigin();
                    world.Rider.Commands.Enqueue(wrapper);
                    controller.AcceptWrapper(wrapper, world.Rider, world.Target);
                    controller.BindAutomaticRun(wrapper);
                    wrapper.BindTerminal(controller.ProductionTerminal);   // the same production terminal handler as a dispatched wrapper
                    blocker.FinishSuccess();
                }
                else
                {
                    world.Ready[world.Rider] = false;   // installed and waiting in its native slot, not yet started
                    controller.Drive();
                    wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                    if (wrapper == null) { Check(false, "PR-1 R8-F2 " + kase + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                    NativeTick(world, world.Rider);
                }
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends;
                if (kase != "valid target out of reach (control)") world.Target.Descriptor.State.IsConscious = false;   // unconscious (still in group memory)
                world.Target.Position = new Vector3(4f, 0f, 0f);   // displaced outside stationary reach
                if (kase == "controller first" || kase == "valid target out of reach (control)") controller.Drive();
                else if (kase == "wrapper first") { world.Ready[world.Rider] = true; NativeTick(world, world.Rider); }
                else world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                var outcome = wrapper.DeliveredOutcome;
                var codes = controller.LastRejectionCodes == null ? "<none>" : string.Join(",", Array.ConvertAll(controller.LastRejectionCodes, c => c.ToString()));
                results[kase] = outcome == null ? "<no outcome>" : outcome.EndCause + "|" + outcome.TerminalReason + "|" + codes;
                Console.WriteLine("  R8-F2 " + kase + ": cause|reason|codes=" + results[kase] + " moves=" + (UnitCommands.MoveSubmissions - moves) + " spends=" + (UnitAttack.StandardSpends - spends) +
                    " retirement=" + controller.LastAutomaticRetirement + " intent=" + controller.HasStockAttackIntent);
                if (kase == "valid target out of reach (control)")
                    Check(outcome != null && wrapper.IsFinished && outcome.EndCause != MountedAttackEndCause.TargetInvalid && controller.LastAutomaticRetirement == "hold out of reach" &&
                        UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && !controller.HasStockAttackIntent,
                        "PR-1 R8-F2 control: a valid target that merely leaves stationary reach keeps the hold-out-of-reach retirement; no movement, no spending");
                else
                    Check(outcome != null && wrapper.IsFinished && outcome.EndCause == MountedAttackEndCause.TargetInvalid && outcome.TerminalReason != null &&
                        outcome.TerminalReason.StartsWith("Expected target invalidation:", StringComparison.Ordinal) && outcome.TerminalReason.Contains("unconscious") &&
                        UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && UnitAttack.Acts(world.Rider) == 0 && !controller.HasStockAttackIntent,
                        "PR-1 R8-F2 " + kase + ": an unconscious remembered target outside stationary reach is retired as target invalidation (cause, reason); no movement, no spending");
            }
            // R9-F3: the complete result, compared with ordinal equality to the expected literal (an appended CommandCancelled code fails).
            const string expectedInvalidation = "TargetInvalid|Expected target invalidation: target became unconscious|TargetInvalid";
            Check(results.Count == 4 && string.Equals(results["controller first"], expectedInvalidation, StringComparison.Ordinal) &&
                string.Equals(results["wrapper first"], expectedInvalidation, StringComparison.Ordinal) && string.Equals(results["queued promotion"], expectedInvalidation, StringComparison.Ordinal),
                "PR-1 R8-F2/R9-F3 controller-first, wrapper-first and queued-promotion observation of the same invalid target emit the same cause, reason and controller rejection code (TargetInvalid)");
        }

        // R6-F1: an unreleased waiting wrapper loses its target before delivery; the controller observes it first (production drive), or the
        // wrapper does (its own tick). Both record the same cause, terminal reason and rejection code, with no spending and no movement.
        private static void Round7InvalidationParity()
        {
            var results = new Dictionary<string, string>();
            foreach (var automatic in new[] { false, true })
            foreach (var first in new[] { "controller first", "wrapper first" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                world.Ready[world.Rider] = false;   // installed and waiting in its native slot, not yet started
                controller.BeginOrder(world.Target, automatic);
                controller.Drive();
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R6-F1 " + first + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                NativeTick(world, world.Rider);
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends;
                world.Target.IsInState = false;   // the target despawns
                if (automatic) world.Rider.Group.Memory.Perceives = unit => unit != world.Target;   // and leaves the rider group's memory
                if (first == "controller first") controller.Drive(); else { world.Ready[world.Rider] = true; NativeTick(world, world.Rider); }
                var outcome = wrapper.DeliveredOutcome;
                var codes = controller.LastRejectionCodes == null ? "<none>" : string.Join(",", Array.ConvertAll(controller.LastRejectionCodes, c => c.ToString()));
                results[(automatic ? "automatic " : "explicit ") + first] = outcome == null ? "<no outcome>" : outcome.EndCause + "|" + outcome.TerminalReason + "|" + codes;
                Console.WriteLine("  R6-F1 " + (automatic ? "automatic " : "explicit ") + first + ": cause|reason|codes=" + results[(automatic ? "automatic " : "explicit ") + first] + " moves=" + (UnitCommands.MoveSubmissions - moves) + " spends=" + (UnitAttack.StandardSpends - spends));
                Check(outcome != null && wrapper.IsFinished && outcome.EndCause == MountedAttackEndCause.TargetInvalid && outcome.TerminalReason == "Expected target invalidation: target despawned" &&
                    UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && UnitAttack.Acts(world.Rider) == 0,
                    "PR-1 R6-F1 " + (automatic ? "automatic order (target also out of perception) " : "explicit order ") + first + ": target loss before delivery records TargetInvalid with the target reason; no spending, no movement");
            }
            Check(results.Count == 4 && results["explicit controller first"] == results["explicit wrapper first"] && results["automatic controller first"] == results["automatic wrapper first"] &&
                results["automatic controller first"] == results["explicit controller first"],
                "PR-1 R6-F1 controller-first and wrapper-first target loss emit the same cause, terminal reason and rejection code, for explicit and automatic orders");
        }


        private static UnitAttack Proposal(UnitEntityData target) =>
            new UnitAttack(target) { AiAction = new Kingmaker.UnitLogic.Commands.AiActionFacade(), CreatedByPlayer = false };

        private static List<Harness.NativeBrainRankingReader.Candidate> Rank(World world, params UnitEntityData[] units)
        {
            var list = new List<Harness.NativeBrainRankingReader.Candidate>();
            foreach (var unit in units) list.Add(new Harness.NativeBrainRankingReader.Candidate { Unit = unit, Weight = 0f, Distance = world.Rider.DistanceTo(unit) });
            return list;
        }

        private static MountedPairAttackCommand PairWrapperOn(World world, UnitEntityData target)
        {
            foreach (var actor in new[] { world.Rider, world.Horse })
            {
                var wrapper = actor.Commands.Standard as MountedPairAttackCommand;
                if (wrapper != null && wrapper.TargetUnit == target && !wrapper.IsFinished) return wrapper;
            }
            return null;
        }

        private static string Struck(World world)
        {
            var struck = (world.Rider.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
            return struck == null ? "<none>" : string.Join(",", struck.ConvertAll(u => u.UniqueId).ToArray());
        }

        // Drives the order to its first native strike (controller drive, act events, native ticks of both actors).
        private static void DriveToStrike(World world, MountedCombatController controller, int ticks)
        {
            for (var tick = 0; tick < ticks && UnitAttack.NativeAttackRules == 0 && controller.HasStockAttackIntent; tick++)
            {
                controller.Drive();
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper != null && wrapper.IsStarted && !wrapper.IsActed) wrapper.ActEventDue = true;
                NativeTick(world, world.Rider); NativeTick(world, world.Horse);
            }
        }

        // PR-1 round 11, owner instruction 2026-11-26d section 5, cases 1, 4 and 5 (R10-F4, R10-F3): a retained automatic order on unreachable B
        // with no wrapper (both Standards cooling). B loses native admission while stationary and perceived, by moving out of the action's range,
        // by the rider's effective reach shrinking, or with the optional ranking unavailable; the next replacement proposal of reachable A
        // arrives BEFORE any controller drive. Controls: B still admitted keeps its order (a valid retained target is protected).
        private static void Round11RetainedAdmission()
        {
            foreach (var kase in new[] { "B leaves range", "rider reach shortens", "ranking unavailable, B leaves range", "control: B still admitted" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;   // a mobile pair
                var b = new UnitEntityData("hostile-b") { Position = new Vector3(0f, 0f, 1.6f) };
                world.Rider.CachedSight = unit => true;
                world.Rider.CombatState.AIData.UnreachableUnits.Add(b);
                world.Rider.HasStandard = false; world.Horse.HasStandard = false;   // both Standards cooling: no dispatch, no wrapper
                var noRanking = kase.StartsWith("ranking unavailable", StringComparison.Ordinal);
                Harness.NativeBrainRankingReader.Ranked = noRanking ? null : Rank(world, b, world.Target);
                controller.Propose(Proposal(noRanking ? b : world.Target), false);   // without a ranking native's own proposal is B
                var selected = controller.IntentTarget;
                controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                var waitingWrapper = PairWrapperOn(world, b);
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends; var horseBefore = world.Horse.Position;
                var admittedBefore = controller.AutomaticAdmittedCount;
                if (kase == "rider reach shortens") world.Rider.WeaponRange = 0.5f;   // B (1.6 m) now beyond 0.5 + both bodies (0.9 m)
                else if (kase != "control: B still admitted") b.Position = new Vector3(0f, 0f, 4f);
                // The replacement proposal arrives before the controller's drive.
                Harness.NativeBrainRankingReader.Ranked = noRanking ? null : new List<Harness.NativeBrainRankingReader.Candidate>
                {
                    new Harness.NativeBrainRankingReader.Candidate { Unit = world.Target, Weight = 0f, Distance = world.Rider.DistanceTo(world.Target) },
                    new Harness.NativeBrainRankingReader.Candidate { Unit = b, Weight = -0.004f, Distance = world.Rider.DistanceTo(b) }
                };
                controller.Propose(Proposal(world.Target), false);
                var thisProposalAdmitted = controller.AutomaticAdmittedCount == admittedBefore + 1;
                var after = controller.IntentTarget;
                var retirement = controller.LastAutomaticRetirement;
                var bWork = PairWrapperOn(world, b) != null;
                var bMoves = UnitCommands.MoveSubmissions - moves; var bSpends = UnitAttack.StandardSpends - spends; var displaced = !world.Horse.Position.Equals(horseBefore);
                world.Rider.HasStandard = true; world.Horse.HasStandard = true;
                DriveToStrike(world, controller, 40);
                Console.WriteLine("  R11 retained (" + kase + "): selected=" + selected?.UniqueId + " waitingWrapper=" + (waitingWrapper != null) + " proposalAdmitted=" + thisProposalAdmitted +
                    " intent=" + after?.UniqueId + " retirement=" + retirement + " bWork=" + bWork + " moves=" + bMoves + " spends=" + bSpends + " displaced=" + displaced +
                    " admissionRetired=" + controller.AutomaticAdmissionRetiredCount + " struck=" + Struck(world) + " refusal=" + controller.LastAutomaticRefusal);
                if (kase == "control: B still admitted")
                    Check(selected == b && waitingWrapper == null && !thisProposalAdmitted && after == b && controller.AutomaticAdmissionRetiredCount == 0 && bMoves == 0 && bSpends == 0,
                        "PR-1 R10-F4 control: a retained automatic target still inside native admission keeps its order against the replacement proposal");
                else
                    Check(selected == b && waitingWrapper == null && thisProposalAdmitted && after == world.Target && retirement == "automatic target outside native admission" &&
                        controller.AutomaticAdmissionRetiredCount == 1 && !bWork && bMoves == 0 && bSpends == 0 && !displaced && Struck(world).StartsWith(world.Target.UniqueId, StringComparison.Ordinal),
                        "PR-1 " + (kase == "rider reach shortens" ? "R10-F3" : noRanking ? "R10-F3 (ranking unavailable)" : "R10-F4") + " " + kase +
                        ": the same replacement proposal, arriving before any drive, is accepted; B gets no wrapper, movement or spending; reachable A is then struck");
            }
        }

        // PR-1 round 11, section 5 cases 2 and 6 (R10-F1, binding): an automatic wrapper on unreachable B is installed and waiting for native start
        // (both Standards available, the rider not ready). B then leaves the action's range while perceived and marked unreachable. A stale command
        // that is not this order's own asks the decision first and changes nothing; then the wrapper's own pre-start tick runs BEFORE any controller
        // drive. Controls: ordinary reachable approach (B not restricted) and an already released strike.
        private static void Round11InstalledWrapper()
        {
            foreach (var kase in new[] { "restricted B leaves range", "control: reachable approach", "control: released strike" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;
                var b = new UnitEntityData("hostile-b") { Position = new Vector3(0f, 0f, 1.6f) };
                world.Rider.CachedSight = unit => true;
                if (kase != "control: reachable approach") world.Rider.CombatState.AIData.UnreachableUnits.Add(b);
                Harness.NativeBrainRankingReader.Ranked = Rank(world, b, world.Target);
                world.Ready[world.Rider] = kase == "control: released strike";
                controller.Propose(Proposal(world.Target), false);
                var wrapper = PairWrapperOn(world, b);
                if (wrapper == null) { Check(false, "PR-1 R10-F1 " + kase + ": no installed wrapper (" + controller.LastFeedback + ")"); continue; }
                if (kase == "control: released strike") { Deliver(world, wrapper, world.Rider); }
                else NativeTick(world, world.Rider);   // awaiting native start
                var released = wrapper.StrikeReleased;
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends; var horseBefore = world.Horse.Position;
                b.Position = new Vector3(0f, 0f, 4f);   // outside the action's range; still perceived
                // A command that is not this order's own asks the decision: nothing is inspected, retired or cleaned up.
                var stale = Order(world, MountedCombatActionKind.RiderMelee, false); stale.MarkAutomaticOrigin();
                var staleRetired = controller.AdmissionHook(stale);
                var orderAfterStale = controller.HasStockAttackIntent && !wrapper.IsFinished;
                NativeTick(world, world.Rider);   // the wrapper's own tick, before any controller drive
                var endedByOwnTick = wrapper.IsFinished;
                var cause = wrapper.EndCause;
                var ownTickMoves = UnitCommands.MoveSubmissions - moves; var ownTickDisplaced = !world.Horse.Position.Equals(horseBefore);
                controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse); controller.Drive();
                var outcome = wrapper.DeliveredOutcome;
                Console.WriteLine("  R11 installed (" + kase + "): released=" + released + " staleRetired=" + staleRetired + " orderAfterStale=" + orderAfterStale + " endedByOwnTick=" + endedByOwnTick +
                    " cause=" + cause + " reason=" + (outcome == null ? "<none>" : outcome.TerminalReason) + " ownTickMoves=" + ownTickMoves + " displaced=" + ownTickDisplaced +
                    " moves=" + (UnitCommands.MoveSubmissions - moves) + " spends=" + (UnitAttack.StandardSpends - spends) + " retirement=" + controller.LastAutomaticRetirement +
                    " admissionRetired=" + controller.AutomaticAdmissionRetiredCount + " intent=" + controller.HasStockAttackIntent);
                if (kase == "restricted B leaves range")
                    Check(!released && !staleRetired && orderAfterStale && endedByOwnTick && cause == MountedAttackEndCause.Cancelled && ownTickMoves == 0 && !ownTickDisplaced &&
                        UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && UnitAttack.Acts(world.Rider) == 0 && !controller.HasStockAttackIntent &&
                        controller.LastAutomaticRetirement == "automatic target outside native admission" && controller.AutomaticAdmissionRetiredCount == 1,
                        "PR-1 R10-F1 an installed automatic wrapper awaiting start, ticking before the controller after B left native admission: no move submission, displacement, delivery or spending; retired once by the order's own decision; a stale command's query changed nothing");
                else if (kase == "control: reachable approach")
                    Check(!staleRetired && !endedByOwnTick && ownTickMoves >= 1 && controller.HasStockAttackIntent && controller.AutomaticAdmissionRetiredCount == 0,
                        "PR-1 R10-F1 control: an unrestricted target that moved away is approached normally (pre-start move submitted, order kept)");
                else
                    Check(released && cause != MountedAttackEndCause.Cancelled && !controller.HasStockAttackIntent && controller.AutomaticAdmissionRetiredCount == 1,
                        "PR-1 R10-F1/R12-F1 control: an already released strike records no admission-loss cause at its own tick (sampled before the retiring drive) and the controller then releases its generation; the post-retirement state is asserted by the round-14 control (R13-F2)");
            }
        }

        // PR-1 round 11, section 5 case 3 (R10-F1): a queued automatic wrapper on unreachable B; B leaves the action's range; native queue promotion
        // (production AdmitQueuedAutomaticRun) runs BEFORE any controller drive: the wrapper is refused, never started, no movement or spending.
        private static void Round11QueuedPromotion()
        {
            foreach (var restricted in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;
                world.Rider.CachedSight = unit => true;
                if (restricted) world.Rider.CombatState.AIData.UnreachableUnits.Add(world.Target);
                controller.BeginOrder(world.Target, true);
                controller.AdoptProposalAction(new Kingmaker.UnitLogic.Commands.AiActionFacade());
                UnitCommands.PromotionPrefix = command => !(command is MountedPairSingleAttack) || (((MountedPairSingleAttack)command).IsAutomaticOrigin
                    ? controller.AdmitQueuedAutomaticRun((MountedPairSingleAttack)command)
                    : KingmakerMountedCombat.Integration.PatchMethods.AdmitExplicitPromotion((MountedPairSingleAttack)command, controller));
                var blocker = new HarnessStationaryCommand();
                world.Rider.Commands.Run(blocker);
                var wrapper = Order(world, MountedCombatActionKind.RiderMelee, false);
                wrapper.MarkAutomaticOrigin();
                wrapper.AllowPreStartApproach();
                world.Rider.Commands.Enqueue(wrapper);
                controller.AcceptWrapper(wrapper, world.Rider, world.Target);
                controller.BindAutomaticRun(wrapper);
                wrapper.BindTerminal(controller.ProductionTerminal);
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends; var horseBefore = world.Horse.Position;
                world.Target.Position = new Vector3(4f, 0f, 0f);
                blocker.FinishSuccess();
                world.Rider.Commands.RemoveFinishedAndUpdateQueue();   // native promotion, before any controller drive
                var promoted = world.Rider.Commands.Standard == wrapper;
                Console.WriteLine("  R11 queued (" + (restricted ? "restricted" : "control: unrestricted") + "): promoted=" + promoted + " finished=" + wrapper.IsFinished + " started=" + wrapper.IsStarted +
                    " retirement=" + controller.LastAutomaticRetirement + " moves=" + (UnitCommands.MoveSubmissions - moves) + " intent=" + controller.HasStockAttackIntent);
                if (restricted)
                    Check(!promoted && wrapper.IsFinished && !wrapper.IsStarted && UnitCommands.MoveSubmissions == moves && world.Horse.Position.Equals(horseBefore) &&
                        UnitAttack.StandardSpends == spends && !controller.HasStockAttackIntent && controller.LastAutomaticRetirement == "automatic target outside native admission" &&
                        controller.AutomaticAdmissionRetiredCount == 1,
                        "PR-1 R10-F1 queued promotion before the drive refuses the automatic wrapper whose restricted target left native admission: never started, no movement or spending, retired once");
                else
                    Check(promoted && !wrapper.IsFinished && controller.HasStockAttackIntent && controller.AutomaticAdmissionRetiredCount == 0,
                        "PR-1 R10-F1 control: an unrestricted target that moved away is promoted for ordinary approach");
            }
        }

        // PR-1 round 11, section 5 case 6 (R10-F2, R9-F3): a mobile pair, a remembered target marked unreachable and an unreleased automatic wrapper.
        // The target becomes unconscious and at the same time leaves the action's range or native sight. Controller first (production drive), wrapper
        // first (its own tick) and queued promotion (production AdmitQueuedAutomaticRun) must each produce exactly the literal
        // TargetInvalid | "Expected target invalidation: target became unconscious" | [TargetInvalid] (ordinal equality; one rejection code),
        // with no movement and no spending.
        private static void Round11InvalidationWithAdmissionLoss()
        {
            const string expected = "TargetInvalid|Expected target invalidation: target became unconscious|TargetInvalid";
            foreach (var loss in new[] { "range", "sight" })
            foreach (var first in new[] { "controller first", "wrapper first", "queued promotion" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;
                world.Rider.CachedSight = unit => true;
                world.Rider.CombatState.AIData.UnreachableUnits.Add(world.Target);
                MountedPairAttackCommand wrapper;
                if (first == "queued promotion")
                {
                    controller.BeginOrder(world.Target, true);
                    controller.AdoptProposalAction(new Kingmaker.UnitLogic.Commands.AiActionFacade());
                    UnitCommands.PromotionPrefix = command => !(command is MountedPairSingleAttack) || (((MountedPairSingleAttack)command).IsAutomaticOrigin
                        ? controller.AdmitQueuedAutomaticRun((MountedPairSingleAttack)command)
                        : KingmakerMountedCombat.Integration.PatchMethods.AdmitExplicitPromotion((MountedPairSingleAttack)command, controller));
                    var blocker = new HarnessStationaryCommand();
                    world.Rider.Commands.Run(blocker);
                    wrapper = Order(world, MountedCombatActionKind.RiderMelee, false);
                    wrapper.MarkAutomaticOrigin();
                    world.Rider.Commands.Enqueue(wrapper);
                    controller.AcceptWrapper(wrapper, world.Rider, world.Target);
                    controller.BindAutomaticRun(wrapper);
                    wrapper.BindTerminal(controller.ProductionTerminal);
                    world.Rider.CombatState.ManualTarget = null;
                    var moves0 = UnitCommands.MoveSubmissions; var spends0 = UnitAttack.StandardSpends;
                    world.Target.Descriptor.State.IsConscious = false;
                    if (loss == "range") world.Target.Position = new Vector3(4f, 0f, 0f); else world.Rider.CachedSight = unit => unit != world.Target;
                    blocker.FinishSuccess();
                    world.Rider.Commands.RemoveFinishedAndUpdateQueue();
                    Record(world, controller, wrapper, loss + " " + first, expected, moves0, spends0);
                    continue;
                }
                world.Ready[world.Rider] = false;   // installed and waiting for native start
                Harness.NativeBrainRankingReader.Ranked = null;
                controller.Propose(Proposal(world.Target), false);
                wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R10-F2 " + loss + " " + first + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                NativeTick(world, world.Rider);
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends;
                world.Target.Descriptor.State.IsConscious = false;   // unconscious, still remembered
                if (loss == "range") world.Target.Position = new Vector3(4f, 0f, 0f); else world.Rider.CachedSight = unit => unit != world.Target;
                if (first == "controller first") controller.Drive();
                else { world.Ready[world.Rider] = true; NativeTick(world, world.Rider); }
                Record(world, controller, wrapper, loss + " " + first, expected, moves, spends);
            }
        }


        // PR-1 round 12 (R11-F1): an automatic first windup has started (child attack started, nothing delivered); the rider's AI is switched off
        // without another command; the delivery tick runs before any controller drive. Controls: AI kept on, and an explicit order with AI off.
        private static void Round12AiOffBeforeDelivery()
        {
            foreach (var kase in new[] { "automatic, AI off", "control: automatic, AI on", "control: explicit, AI off" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;
                var automatic = kase.Contains("automatic");
                if (automatic) { Harness.NativeBrainRankingReader.Ranked = null; controller.Propose(Proposal(world.Target), false); }
                else { controller.BeginOrder(world.Target, false); controller.Drive(); }
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R11-F1 " + kase + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                for (var tick = 0; tick < 4 && !wrapper.IsStarted; tick++) NativeTick(world, world.Rider);
                var windup = wrapper.IsStarted && !wrapper.StrikeReleased && UnitAttack.NativeAttackRules == 0;
                var spends = UnitAttack.StandardSpends; var rules = UnitAttack.NativeAttackRules;
                if (kase.EndsWith("AI off", StringComparison.Ordinal)) world.Rider.IsAIEnabled = false;
                wrapper.ActEventDue = true;
                NativeTick(world, world.Rider);   // the delivery tick, before any controller drive
                var delivered = UnitAttack.NativeAttackRules - rules;
                Console.WriteLine("  R12 AI off (" + kase + "): windup=" + windup + " delivered=" + delivered + " spends=" + (UnitAttack.StandardSpends - spends) + " finished=" + wrapper.IsFinished +
                    " retirement=" + controller.LastAutomaticRetirement + " intent=" + controller.HasStockAttackIntent + " struck=" + Struck(world));
                if (kase == "automatic, AI off")
                    Check(windup && delivered == 0 && UnitAttack.StandardSpends == spends && wrapper.IsFinished && controller.LastAutomaticRetirement == "ai off" &&
                        !controller.HasStockAttackIntent && controller.AutomaticAdmissionRetiredCount == 1,
                        "PR-1 R11-F1 an automatic first windup whose rider's AI is switched off is retired for AI off by its own pre-delivery check: zero deliveries, zero spending");
                else
                    Check(windup && delivered == 1 && controller.AutomaticAdmissionRetiredCount == 0,
                        "PR-1 R11-F1 " + kase + ": the strike is delivered once");
            }
        }

        // PR-1 round 12 (R11-F2): only the mount is held (pair movement forbidden through the mount's Hold; the rider's own AiMovementForbidden is
        // false and no target is marked unreachable, so native admission does not apply). Retained B leaves stationary strike reach; in-reach A
        // is proposed before the drive. Control: B still in stationary reach keeps its order.
        private static void Round12MountOnlyHold()
        {
            foreach (var bLeaves in new[] { true, false })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                world.Horse.HoldState = true;   // the player's Hold on the horse only; production AutomaticMovementForbidden reads it
                world.Rider.AiMovementForbidden = false;   // the rider stays mobile; neither target is marked unreachable
                var b = new UnitEntityData("hostile-b") { Position = new Vector3(0f, 0f, 1.6f) };
                world.Rider.CachedSight = unit => true;
                world.Rider.HasStandard = false; world.Horse.HasStandard = false;   // both Standards cooling: no wrapper yet
                Harness.NativeBrainRankingReader.Ranked = Rank(world, b, world.Target);
                controller.Propose(Proposal(world.Target), false);
                var selected = controller.IntentTarget;
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends; var horseBefore = world.Horse.Position;
                var admittedBefore = controller.AutomaticAdmittedCount;
                if (bLeaves) b.Position = new Vector3(0f, 0f, 4f);   // outside stationary mounted-strike reach
                Harness.NativeBrainRankingReader.Ranked = Rank(world, world.Target, b);
                controller.Propose(Proposal(world.Target), false);   // before any drive
                var accepted = controller.AutomaticAdmittedCount == admittedBefore + 1;
                var after = controller.IntentTarget; var retirement = controller.LastAutomaticRetirement;
                var bWork = PairWrapperOn(world, b) != null;
                var preReadinessSpends = UnitAttack.StandardSpends - spends;
                var forbiddenByHorseHold = controller.MovementForbidden && !world.Rider.AiMovementForbidden && world.Rider.CombatState.AIData.UnreachableUnits.Count == 0;
                world.Rider.HasStandard = true; world.Horse.HasStandard = true;
                DriveToStrike(world, controller, 12);
                var deliveries = UnitAttack.NativeAttackRules;
                Console.WriteLine("  R12 mount-only Hold (" + (bLeaves ? "B leaves stationary reach" : "control: B in reach") + "): horseHold=" + world.Horse.HoldState + " forbiddenByHorseHold=" + forbiddenByHorseHold +
                    " preReadinessSpends=" + preReadinessSpends + " deliveries=" + deliveries + " selected=" + selected?.UniqueId + " accepted=" + accepted +
                    " intent=" + after?.UniqueId + " retirement=" + retirement + " bWork=" + bWork + " moves=" + (UnitCommands.MoveSubmissions - moves) +
                    " displaced=" + !world.Horse.Position.Equals(horseBefore) + " struck=" + Struck(world));
                if (bLeaves)
                    Check(forbiddenByHorseHold && preReadinessSpends == 0 && selected == b && accepted && after == world.Target && retirement == "hold out of reach" && !bWork &&
                        UnitCommands.MoveSubmissions == moves && world.Horse.Position.Equals(horseBefore) && Struck(world) == world.Target.UniqueId && deliveries == 1,
                        "PR-1 R11-F2 a mount-only Hold: retained B outside stationary reach cannot veto in-reach A; A's proposal is accepted before the drive and A is struck without travel; B gets no movement or spending");
                else
                    Check(forbiddenByHorseHold && preReadinessSpends == 0 && selected == b && !accepted && after == b && Struck(world) == b.UniqueId && deliveries == 1 &&
                        UnitCommands.MoveSubmissions == moves,
                        "PR-1 R11-F2 control: retained B still inside stationary reach keeps its order and is struck without travel");
            }
        }

        // PR-1 round 12 (R11-F3): after automatic admission the rider's group, or its memory, becomes unavailable; the installed wrapper runs its
        // pre-start tick before the next drive. Controls: a perceived target, and an explicit order with the group removed.
        private static void Round12MissingMemory()
        {
            foreach (var kase in new[] { "group removed", "memory removed", "control: perceived", "control: explicit, group removed" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                controller.MovementForbidden = false;
                world.Ready[world.Rider] = false;   // installed, waiting for native start; the target needs an approach
                var explicitOrder = kase.Contains("explicit");
                if (explicitOrder) { controller.BeginOrder(world.Target, false); controller.Drive(); }
                else { Harness.NativeBrainRankingReader.Ranked = null; controller.Propose(Proposal(world.Target), false); }
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R11-F3 " + kase + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends;
                if (kase.EndsWith("group removed", StringComparison.Ordinal)) world.Rider.Group = null;
                else if (kase == "memory removed") world.Rider.Group.Memory = null;
                NativeTick(world, world.Rider);   // the wrapper's pre-start tick, before the next drive
                Console.WriteLine("  R12 memory (" + kase + "): finished=" + wrapper.IsFinished + " moves=" + (UnitCommands.MoveSubmissions - moves) + " spends=" + (UnitAttack.StandardSpends - spends) +
                    " retirement=" + controller.LastAutomaticRetirement + " intent=" + controller.HasStockAttackIntent);
                if (kase == "group removed" || kase == "memory removed")
                    Check(wrapper.IsFinished && UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && !controller.HasStockAttackIntent &&
                        controller.LastAutomaticRetirement == "automatic target left perception",
                        "PR-1 R11-F3 " + kase + " after automatic admission: the wrapper's own tick retires the order as perception loss; zero move submissions, zero spending");
                else
                    Check(!wrapper.IsFinished && UnitCommands.MoveSubmissions > moves && controller.HasStockAttackIntent,
                        "PR-1 R11-F3 " + kase + ": the order advances normally (a pre-start move is submitted)");
            }
        }


        // PR-1 round 13 (R12-F1): an automatic two-attack routine delivers its first act; the rider's AI is switched off and the controller drives
        // once: the generation is released while the wrapper keeps running. AI is re-enabled before the remaining act, which completes normally
        // with one Standard spent; drives with an eligible horse then dispatch nothing until a fresh proposal.
        private static void Round13AiOffAfterDelivery()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
            var controller = Controller7(world, false);
            Harness.NativeBrainRankingReader.Ranked = null;
            controller.Propose(Proposal(world.Target), false);
            var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
            if (wrapper == null) { Check(false, "PR-1 R12-F1: no wrapper (" + controller.LastFeedback + ")"); return; }
            wrapper.BindTerminal(controller.ProductionTerminal);
            Deliver(world, wrapper, world.Rider);
            var firstDelivered = wrapper.StruckTargets.Count == 1 && !wrapper.IsFinished && wrapper.StrikeReleased;
            world.Rider.IsAIEnabled = false;
            var dispatchesBefore = Dispatches(controller);
            controller.Drive();
            var released = !controller.HasStockAttackIntent; var running = !wrapper.IsFinished; var retirement = controller.LastAutomaticRetirement;
            world.Rider.IsAIEnabled = true;
            wrapper.ActEventDue = true;
            NativeTick(world, world.Rider);   // the native routine's remaining act
            var completed = wrapper.IsFinished && wrapper.Result == UnitCommand.ResultType.Success && wrapper.StruckTargets.Count == 2 &&
                wrapper.Transaction.State == MountedCombatTransactionState.Completed;
            var spends = UnitAttack.StandardSpends;
            world.Horse.HasStandard = true;
            for (var tick = 0; tick < 3; tick++) { controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
            var newDispatches = Dispatches(controller) - dispatchesBefore;
            Harness.NativeBrainRankingReader.Ranked = null;
            controller.Propose(Proposal(world.Target), false);
            var fresh = controller.HasStockAttackIntent;
            Console.WriteLine("  R13 AI off after delivery: firstDelivered=" + firstDelivered + " released=" + released + " running=" + running + " retirement=" + retirement +
                " completed=" + completed + " struck=" + wrapper.StruckTargets.Count + " spends=" + spends + " newDispatches=" + newDispatches + " freshProposalAdmitted=" + fresh);
            Check(firstDelivered && released && running && retirement == "ai off" && completed && spends == 1 && newDispatches == 0 && fresh,
                "PR-1 R12-F1 AI off after the first delivery: the drive releases the repeating generation while the delivered routine runs on to native completion (one Standard); no further dispatch until a fresh proposal");
        }

        // PR-1 round 13 (R12-F3): an automatic order is approaching with a live carrier; the player holds the horse (the target is outside
        // stationary reach). Controller first (drive) and wrapper first (its own approach tick) record the same retirement once, release the exact
        // carrier and deliver nothing.
        private static void Round13HeldDuringApproach()
        {
            var results = new List<string>();
            foreach (var first in new[] { "controller first", "wrapper first" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                Harness.NativeBrainRankingReader.Ranked = null;
                controller.Propose(Proposal(world.Target), false);
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R12-F3 " + first + ": no wrapper"); continue; }
                for (var tick = 0; tick < 3 && (wrapper.DelegatedMove == null || !wrapper.IsStarted); tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                var carrier = wrapper.DelegatedMove;
                var live = carrier != null && !carrier.IsFinished;
                world.Horse.HoldState = true;   // the player's Hold on the horse
                if (first == "controller first") controller.Drive(); else NativeTick(world, world.Rider);
                var line = controller.LastAutomaticRetirement + "|" + controller.AutomaticAdmissionRetiredCount;
                results.Add(line);
                Console.WriteLine("  R13 held during approach (" + first + "): liveCarrier=" + live + " retirement|count=" + line + " carrierFinished=" + (carrier != null && carrier.IsFinished) +
                    " wrapperFinished=" + wrapper.IsFinished + " delivered=" + UnitAttack.NativeAttackRules);
                Check(live && line == "hold out of reach|1" && carrier.IsFinished && wrapper.IsFinished && UnitAttack.NativeAttackRules == 0,
                    "PR-1 R12-F3 " + first + ": a horse Hold during a live automatic approach retires the order once as hold out of reach, releases the exact carrier, no delivery");
            }
        }

        // PR-1 round 13 (R12-F4): a held, cooling stationary order is driven once: the stationary strike probe is built once in that drive; the same
        // target is retained, nothing moves or is spent.
        private static void Round13OneProbePerDrive()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
            var controller = Controller7(world, false);
            world.Horse.HoldState = true;
            world.Rider.HasStandard = false; world.Horse.HasStandard = false;
            Harness.NativeBrainRankingReader.Ranked = null;
            controller.Propose(Proposal(world.Target), false);
            var moves = UnitCommands.MoveSubmissions; var spends = UnitAttack.StandardSpends; var probes = controller.PlannedStrikeProbeCount;
            controller.Drive();
            var delta = controller.PlannedStrikeProbeCount - probes;
            Console.WriteLine("  R13 probes per held cooling drive: " + delta + " intent=" + controller.IntentTarget?.UniqueId + " moves=" + (UnitCommands.MoveSubmissions - moves) + " spends=" + (UnitAttack.StandardSpends - spends));
            Check(delta == 1 && controller.IntentTarget == world.Target && UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends,
                "PR-1 R12-F4 one held cooling drive builds the stationary strike probe once and retains the target with zero movement or spending");
        }


        // PR-1 round 14 (R13-F1): only the horse is held and the target stays eligible in stationary reach. The rider's automatic two-attack
        // routine delivers its final act while its animation is still playing (native UnitCommand.Tick: Result=Success, the plan exhausted,
        // IsFinished false until the animation finishes). A controller drive in that interval keeps the same generation and target with no
        // retirement and no travel; when the animation finishes, the eligible horse follows through on the same target without a fresh
        // proposal; one Standard per acting unit. Control: the target genuinely leaving stationary reach in that interval is retired.
        private static void Round14HeldFinalActRecovery()
        {
            foreach (var leaves in new[] { false, true })
            {
                var label = leaves ? "control: target leaves stationary reach" : "target stays eligible";
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, false);
                var controller = Controller7(world, false);
                world.Horse.HoldState = true;   // only the horse is held
                Harness.NativeBrainRankingReader.Ranked = null;
                controller.Propose(Proposal(world.Target), false);
                var wrapper = world.Rider.Commands.Standard as MountedPairAttackCommand;
                if (wrapper == null) { Check(false, "PR-1 R13-F1 " + label + ": no wrapper (" + controller.LastFeedback + ")"); continue; }
                var generation = controller.Generation; var admitted = controller.AutomaticAdmittedCount;
                Deliver(world, wrapper, world.Rider);   // first act
                wrapper.AnimationPlaying = true;        // the final act's animation keeps playing after its OnAction
                wrapper.ActEventDue = true; NativeTick(world, world.Rider);
                var interval = wrapper.Result == UnitCommand.ResultType.Success && wrapper.GetAttackIndex() == wrapper.AllAttacks.Count && !wrapper.IsFinished;
                var moves = UnitCommands.MoveSubmissions; var horseBefore = world.Horse.Position; var retired = controller.AutomaticAdmissionRetiredCount;
                if (leaves) world.Target.Position = new Vector3(5f, 0f, 0f);
                controller.Drive();   // during the finishing animation
                var sameOrder = controller.HasStockAttackIntent && controller.Generation == generation && controller.IntentTarget == world.Target;
                var retiredInInterval = controller.AutomaticAdmissionRetiredCount - retired; var retirement = controller.LastAutomaticRetirement;
                wrapper.AnimationPlaying = false; NativeTick(world, world.Rider);   // the animation finishes: native OnEnded
                for (var tick = 0; tick < 6 && UnitAttack.Acts(world.Horse) == 0; tick++)
                {
                    controller.Drive();
                    var horseWrapper = world.Horse.Commands.Standard as MountedPairAttackCommand;
                    if (horseWrapper != null && horseWrapper.IsStarted && !horseWrapper.IsActed) horseWrapper.ActEventDue = true;
                    NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                }
                var horseStruck = (world.Horse.Commands.Standard as MountedPairAttackCommand)?.StruckTargets;
                Console.WriteLine("  R14 held final act (" + label + "): interval=" + interval + " sameOrder=" + sameOrder + " retiredInInterval=" + retiredInInterval + " retirement=" + retirement +
                    " moves=" + (UnitCommands.MoveSubmissions - moves) + " displaced=" + !world.Horse.Position.Equals(horseBefore) + " riderEnded=" + wrapper.IsFinished + "/" + wrapper.Result +
                    " riderActs=" + UnitAttack.Acts(world.Rider) + " horseActs=" + UnitAttack.Acts(world.Horse) + " horseStruck=" + (horseStruck == null ? "<none>" : string.Join(",", horseStruck.ConvertAll(u => u.UniqueId).ToArray())) +
                    " spends=" + UnitAttack.StandardSpends + " freshProposals=" + (controller.AutomaticAdmittedCount - admitted) + " generation=" + controller.Generation + "/" + generation);
                if (!leaves)
                    Check(interval && sameOrder && retiredInInterval == 0 && UnitCommands.MoveSubmissions == moves && world.Horse.Position.Equals(horseBefore) &&
                        wrapper.IsFinished && wrapper.Result == UnitCommand.ResultType.Success && UnitAttack.Acts(world.Rider) == 2 && UnitAttack.Acts(world.Horse) >= 1 &&
                        horseStruck != null && horseStruck.Count >= 1 && horseStruck.TrueForAll(u => u == world.Target) && UnitAttack.StandardSpends == 2 &&
                        controller.AutomaticAdmittedCount == admitted && controller.Generation == generation,
                        "PR-1 R13-F1 held horse, eligible target: a drive while the rider's final act is still animating keeps the same generation and target (no retirement, no travel); after the animation the horse follows through on the same target without a fresh proposal; one Standard per acting unit");
                else
                    Check(interval && !sameOrder && retiredInInterval == 1 && retirement == "hold out of reach" && UnitCommands.MoveSubmissions == moves && UnitAttack.Acts(world.Horse) == 0,
                        "PR-1 R13-F1 control: a target that genuinely leaves stationary reach during the finishing animation is retired (hold out of reach); no horse strike, no travel");
            }
        }

        // PR-1 round 14 (R13-F2): the released-strike range-loss control. An automatic two-attack routine on restricted B delivered its first act;
        // B leaves the action's range; the controller drive retires the generation. The wrapper is sampled immediately after that drive, before
        // any engine tick: its cause, transaction state and ownership of the remaining native routine. B then returns to legal reach before the
        // next act; the routine completes natively with the original Standard cost and the retired generation dispatches nothing more.
        private static void Round14ReleasedStrikeRangeLoss()
        {
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0f, 0f), null, false);
            var controller = Controller7(world, false);
            var b = new UnitEntityData("hostile-b") { Position = new Vector3(0f, 0f, 1.6f) };
            world.Rider.CachedSight = unit => true;
            world.Rider.CombatState.AIData.UnreachableUnits.Add(b);
            Harness.NativeBrainRankingReader.Ranked = Rank(world, b, world.Target);
            controller.Propose(Proposal(world.Target), false);
            var wrapper = PairWrapperOn(world, b);
            if (wrapper == null) { Check(false, "PR-1 R13-F2: no wrapper on B (" + controller.LastFeedback + ")"); return; }
            Deliver(world, wrapper, world.Rider);
            var spendsAfterFirst = UnitAttack.StandardSpends; var dispatches = Dispatches(controller);
            b.Position = new Vector3(0f, 0f, 4f);   // outside the action's range
            controller.Drive();                      // the retiring drive
            // Sampled now, before any engine tick.
            var causeNow = wrapper.EndCause; var transactionNow = wrapper.Transaction.State; var finishedNow = wrapper.IsFinished;
            var ownsSlot = world.Rider.Commands.Standard == wrapper; var remaining = wrapper.AllAttacks.Count - wrapper.GetAttackIndex();
            var retired = !controller.HasStockAttackIntent; var retirement = controller.LastAutomaticRetirement;
            Console.WriteLine("  R14 released range loss, after the retiring drive: retired=" + retired + " (" + retirement + ") cause=" + causeNow + " transaction=" + transactionNow +
                " finished=" + finishedNow + " ownsSlot=" + ownsSlot + " remainingAttacks=" + remaining);
            b.Position = new Vector3(0f, 0f, 1.6f);  // legal reach again before the next act
            wrapper.ActEventDue = true; NativeTick(world, world.Rider);
            for (var tick = 0; tick < 3; tick++) { controller.Drive(); NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
            Console.WriteLine("  R14 released range loss, completion: result=" + wrapper.Result + " finished=" + wrapper.IsFinished + " struck=" + wrapper.StruckTargets.Count +
                " transaction=" + wrapper.Transaction.State + " spends=" + UnitAttack.StandardSpends + " (after first act " + spendsAfterFirst + ") redispatches=" + (Dispatches(controller) - dispatches));
            Check(retired && retirement == "automatic target outside native admission" && causeNow == MountedAttackEndCause.Unrecorded && transactionNow == MountedCombatTransactionState.Attacking &&
                !finishedNow && ownsSlot && remaining == 1,
                "PR-1 R13-F2 immediately after the retiring drive (no engine tick): the generation is released, the delivered wrapper has no cause recorded, its transaction is still Attacking and it still owns the remaining native routine in its slot");
            Check(wrapper.IsFinished && wrapper.Result == UnitCommand.ResultType.Success && wrapper.StruckTargets.Count == 2 && wrapper.StruckTargets.TrueForAll(u => u == b) &&
                wrapper.Transaction.State == MountedCombatTransactionState.Completed && UnitAttack.StandardSpends == spendsAfterFirst && spendsAfterFirst == 1 &&
                Dispatches(controller) == dispatches && !controller.HasStockAttackIntent,
                "PR-1 R13-F2 with legal reach restored before the next act, the routine completes natively (Success, both acts on B, transaction Completed) with the original Standard cost and zero redispatches from the retired generation");
        }

        private static void Record(World world, MountedCombatController controller, MountedPairAttackCommand wrapper, string label, string expected, long moves, long spends)
        {
            var outcome = wrapper.DeliveredOutcome;
            var codes = controller.LastRejectionCodes;
            var triple = outcome == null ? "<no outcome>" : outcome.EndCause + "|" + outcome.TerminalReason + "|" +
                (codes == null ? "<none>" : string.Join(",", Array.ConvertAll(codes, c => c.ToString())));
            Console.WriteLine("  R11 invalidation (" + label + "): cause|reason|codes=" + triple + " moves=" + (UnitCommands.MoveSubmissions - moves) +
                " spends=" + (UnitAttack.StandardSpends - spends) + " retirement=" + controller.LastAutomaticRetirement + " retiredCount=" + controller.AutomaticAdmissionRetiredCount);
            // PR-1 round 12 (R11-F4): every observer leaves the same automatic-retirement diagnostics: one retirement of the order, its reason.
            Check(controller.LastAutomaticRetirement == "automatic target invalid" && controller.AutomaticAdmissionRetiredCount == 1,
                "PR-1 R11-F4 " + label + ": the order's retirement is recorded once with its reason, whichever observer saw the loss first");
            Check(string.Equals(triple, expected, StringComparison.Ordinal) && codes != null && codes.Length == 1 && codes[0] == MountedCombatRejectionCode.TargetInvalid &&
                wrapper.IsFinished && UnitCommands.MoveSubmissions == moves && UnitAttack.StandardSpends == spends && UnitAttack.Acts(world.Rider) == 0 && !controller.HasStockAttackIntent,
                "PR-1 R10-F2/R9-F3 " + label + ": an unconscious remembered target that also lost native admission ends exactly as TargetInvalid | target became unconscious | one TargetInvalid code; no movement, no spending");
        }

        // R9-F3: the exact-result assertion must reject an appended CommandCancelled code (the assertion itself, applied to a literal).
        private static void Round11ExactAssertionRejectsAppendedCode()
        {
            const string expected = "TargetInvalid|Expected target invalidation: target became unconscious|TargetInvalid";
            Check(!string.Equals(expected + ",CommandCancelled", expected, StringComparison.Ordinal) && string.Equals(expected, expected, StringComparison.Ordinal),
                "PR-1 R9-F3 the ordinal full-result comparison rejects a TargetInvalid result with an appended CommandCancelled code");
        }

        private static void R4F1()
        {
            // Corner: the target sits beyond the corner; within reach but out of sight until the route has turned it.
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f && from.x > 1.0f;
            var turnPoint = new Vector3(2f, 0f, 0f);
            Func<Vector3, Vector3, List<Vector3>> route = (from, to) => new List<Vector3> { from, turnPoint, new Vector3(2f, 0f, 1.5f) };
            List<Vector3> firstRoute = null;
            foreach (var shift in new[] { 0f, 0.1f, 0.3f })
            {
                var world = NewWorld(new Vector3(-shift, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
                world.Horse.View.AgentASP.Route = route;
                var controller = new HarnessController(world.Rider, world.Horse);
                controller.unifiedTurn.GrantMetres = 9f;
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                order.ValidatedApproachRoute = new List<Vector3> { new Vector3(0f, 0f, 0f), turnPoint, new Vector3(2f, 0f, 0.6f) }; // the click-time route
                order.RouteRevalidator = plan => controller.Revalidate(world.Target, plan);
                world.Rider.Commands.Run(order);
                order.Start();
                var carrier = order.DelegatedMove;
                var forced = carrier?.ForcedPath?.vectorPath;
                var label = "shift " + shift.ToString("0.0") + " m";
                Check(carrier != null && forced != null && Kingmaker.Utility.GeometryUtils.MechanicsDistance(forced[0], world.Horse.Position) < 1e-4f &&
                    order.RouteRevalidationCount == 1 && controller.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal),
                    "R4-F1 " + label + ": the submitted carrier's route was recomputed from the current origin (" + controller.LastRouteRevalidation + ")");
                var end = forced == null ? default(Vector3) : forced[forced.Count - 1];
                Check(forced != null && Math.Abs(end.x - 2f) < 0.01f && end.z >= 0.6f - 0.01f && end.z < 0.7f,
                    "R4-F1 " + label + ": the route ends at the first strike-admissible point past the corner, not the point-move end");
                if (shift == 0f) firstRoute = forced;
                var safety = 0;
                while (!order.IsFinished && order.Transaction.State == MountedCombatTransactionState.Approaching && safety++ < 80)
                { NativeTick(world, world.Horse); NativeTick(world, world.Rider); }
                if (order.Transaction.State == MountedCombatTransactionState.Attacking) NativeTick(world, world.Rider);
                Check(carrier.FollowedForcedPath && UnitAttack.NativeAttackStarts == 1 && order.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted,
                    "R4-F1 " + label + ": the carrier followed the validated route and the strike was admitted once at its end");
            }
            // A grant that no longer covers the recomputed route ends the approach cleanly: no carrier, no native fallback.
            var short_ = NewWorld(new Vector3(-0.3f, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
            short_.Horse.View.AgentASP.Route = route;
            var shortController = new HarnessController(short_.Rider, short_.Horse);
            shortController.unifiedTurn.GrantMetres = 2.75f; // covers the unshifted 2.6 m route, not the shifted 2.9 m one
            var expired = Order(short_, MountedCombatActionKind.RiderMelee, false);
            expired.ValidatedApproachRoute = new List<Vector3> { new Vector3(0f, 0f, 0f), turnPoint, new Vector3(2f, 0f, 0.6f) };
            expired.RouteRevalidator = plan => shortController.Revalidate(short_.Target, plan);
            short_.Rider.Commands.Run(expired);
            var runsBefore = short_.Horse.Commands.RunCount;
            expired.Start();
            Check(expired.IsFinished && expired.DelegatedMove == null && short_.Horse.Commands.RunCount == runsBefore &&
                short_.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && expired.RouteProofExpiredCount == 1 &&
                expired.Transaction.State == MountedCombatTransactionState.Cancelled && UnitAttack.NativeAttackStarts == 0,
                "R4-F1: an expired proof ends the approach cleanly (no carrier submitted, no native fallback, no attack)");
            // Control: a non-route-bound order keeps native routing (no forced route, no revalidation).
            var native = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
            var plain = Order(native, MountedCombatActionKind.RiderMelee, false);
            native.Rider.Commands.Run(plain);
            plain.Start();
            Check(plain.DelegatedMove != null && plain.DelegatedMove.ForcedPath == null && plain.RouteRevalidationCount == 0,
                "R4-F1 control: a non-route-bound order keeps native routing");
        }

        // Runs an admitted route-bound order from its start until the attack begins (or the approach ends), returning the
        // carrier the order submitted and the mount position at which the strike was admitted.
        private static UnitMoveTo RunRouteBound(World world, HarnessController controller, MountedPairAttackCommand order, out Vector3 admittedAt,
            out MountedPairNativeAdmissionState admissionAtRelease)
        {
            order.ValidatedApproachRoute = new List<Vector3> { world.Horse.Position, world.Horse.Position };
            order.RouteRevalidator = plan => controller.Revalidate(world.Target, plan);
            world.Rider.Commands.Run(order);
            order.Start();
            var carrier = order.DelegatedMove;
            admittedAt = default(Vector3);
            admissionAtRelease = MountedPairNativeAdmissionState.Unavailable;
            var safety = 0;
            while (!order.IsFinished && order.Transaction.State == MountedCombatTransactionState.Approaching && safety++ < 120)
            {
                NativeTick(world, world.Horse);
                NativeTick(world, world.Rider);
            }
            if (order.Transaction.State == MountedCombatTransactionState.Attacking)
            {
                admittedAt = world.Horse.Position;
                admissionAtRelease = order.EvaluateCurrentNativeAdmission();
                NativeTick(world, world.Rider);
            }
            return carrier;
        }

        // Review R5-F2: a legal step endpoint inside a sight window narrower than the walk's sample spacing.
        private static void R5F2()
        {
            // Straight route east toward the target; the rider sees it only through a slot 1.412..1.428 m along (between the
            // regular samples at 1.40 and 1.45). In pair reach from x = 1.0 on.
            Func<Vector3, Vector3, bool> slot = (from, to) => !(from.x >= 1.412f && from.x <= 1.428f);
            foreach (var grant in new[] { 1.42f, 1.40f })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), slot, true);
                var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, grant);
                string reason;
                var decision = PairedAttackClickAdmissionPolicy.Decide(true, true, true, true, true, true, false, false, true, true,
                    probe.AdmissibleNow, probe.RouteComplete, probe.RouteProven, probe.RouteMetres, grant, out reason);
                var label = "grant " + grant.ToString("0.00") + " m";
                if (grant == 1.42f)
                {
                    var end = probe.Route == null ? default(Vector3) : probe.Route[probe.Route.Count - 1];
                    Check(decision == PairedAttackClickAdmission.AdmitRoute && probe.Route != null && end.x >= 1.412f && end.x <= 1.42f &&
                        probe.RouteMetres <= grant, "R5-F2 " + label + ": the click whose grant ends inside the sight window is admitted (" +
                        decision + ":" + reason + ", endpoint " + end + ", cost " + probe.RouteMetres.ToString("R") + ")");
                    var controller = new HarnessController(world.Rider, world.Horse);
                    controller.unifiedTurn.GrantMetres = grant;
                    var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                    Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
                    var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
                    var forcedEnd = carrier?.ForcedPath?.vectorPath?.Last() ?? default(Vector3);
                    Check(carrier != null && controller.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal) &&
                        Math.Abs(forcedEnd.x - end.x) < 1e-4f && carrier.ApproachPoint.Equals(forcedEnd) &&
                        Math.Abs(admittedAt.x - end.x) < 1e-3f && atRelease == MountedPairNativeAdmissionState.Admitted &&
                        UnitAttack.NativeAttackStarts == 1 && carrier.TravelledMetres <= grant + 1e-4f,
                        "R5-F2 " + label + ": revalidation carries the verified endpoint to the carrier, which stops there (travelled " +
                        (carrier?.TravelledMetres ?? -1f).ToString("R") + " m) and one strike is admitted");
                }
                else
                {
                    Check(decision == PairedAttackClickAdmission.NativeDecision && probe.Route == null,
                        "R5-F2 " + label + ": a grant ending before the sight window keeps native's refusal, so the prior order stands (" +
                        decision + ":" + reason + ", probe " + probe.Detail + ")");
                }
            }
        }

        // Review R6-F1: the carrier's completion must agree with strike admission. Native completes a point command anywhere within
        // its ApproachRadius of ApproachPoint (horizontal mechanics distance), so the destination lies INSIDE the opening and the
        // radius is proven: every position in the arrival disc admits the strike. Three flanks: the original (sight from z >= 0.6,
        // mount starting inside the enemy point-command radius), the reviewer's z = 0.54 boundary, and a mount that starts within
        // 0.05 m of the opening's first point with sight blocked. Each against the round-6 carrier (first legal point, 0.05 m).
        private static void R6F1()
        {
            var target = new Vector3(2.5f, 0f, 0f);
            Func<Vector3, Vector3, List<Vector3>> flank = (from, to) => from.x <= 0.5001f
                ? new List<Vector3> { from, new Vector3(0.5f, 0f, 1.5f) }
                : new List<Vector3> { from, new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 0f, 1.5f) };
            foreach (var flankCase in new[] { new { Sight = 0.6f, Start = new Vector3(1.3f, 0f, 0f), Name = "z>=0.60 flank" },
                                              new { Sight = 0.54f, Start = new Vector3(1.3f, 0f, 0f), Name = "z>=0.54 flank" },
                                              new { Sight = 0.54f, Start = new Vector3(0.5f, 0f, 0.5f), Name = "start 0.04 m before the opening" } })
            {
                var sight = flankCase.Sight;
                Func<Vector3, Vector3, bool> wall = (from, to) => from.z < sight;
                var world = NewWorld(flankCase.Start, target, wall, true);
                world.Horse.View.AgentASP.Route = flank;
                var controller = new HarnessController(world.Rider, world.Horse);
                controller.unifiedTurn.GrantMetres = 9f;
                var strike = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true);
                strike.Init(world.Rider);
                var startAdmission = strike.EvaluateCurrentNativeAdmission();
                Check(startAdmission == MountedPairNativeAdmissionState.BlockedLineOfSight,
                    "R6-F1 " + flankCase.Name + " setup: the rider's sight is blocked at the start (" + startAdmission + ")");
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
                var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
                var forced = carrier?.ForcedPath?.vectorPath;
                var destination = forced == null ? default(Vector3) : forced[forced.Count - 1];
                var radius = carrier?.ApproachRadius ?? float.NaN;
                // The proven disc: every sampled position native could accept as arrival admits the strike.
                var discAdmits = carrier != null && PairedAttackClickProbe.DiscAdmits(destination, radius, new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 0f, 1.5f),
                    (point, from, to) => strike.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted);
                Check(forced != null && Math.Abs(destination.x - 0.5f) < 1e-3f && destination.z - radius >= sight - 1e-4f && radius > 0f && radius <= 0.05f &&
                    carrier.ApproachPoint.Equals(destination) && !carrier.NeedLoS && discAdmits && carrier.FirstProximityDecision == false,
                    "R6-F1 " + flankCase.Name + ": the carrier targets " + destination + " inside the opening with a proven arrival radius " + radius.ToString("R") +
                    "; native's first proximity decision is 'not close'");
                Check(carrier != null && carrier.FollowedForcedPath && !carrier.CompletedWithoutMoving && atRelease == MountedPairNativeAdmissionState.Admitted &&
                    admittedAt.z >= sight - 1e-4f && order.CarrierReleaseProven && order.DelegatedMove == null &&
                    world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && UnitAttack.NativeAttackStarts == 1 &&
                    order.RouteArrivalContradictionCount == 0,
                    "R6-F1 " + flankCase.Name + ": the horse moved to legal sight (" + admittedAt + ", travelled " + (carrier?.TravelledMetres ?? -1f).ToString("R") +
                    " m), the exact carrier was released and one strike was admitted");
                // Control: the round-6 carrier (the opening's first point, 0.05 m) completes short of legal sight.
                var control = NewWorld(flankCase.Start, target, wall, true);
                var oldEnd = new Vector3(0.5f, 0f, sight);
                var old = new UnitMoveTo(oldEnd, 0.05f) { NeedLoS = false };
                old.ForcedPath = new Pathfinding.ForcedPath(flank(flankCase.Start, oldEnd).Take(flankCase.Start.x <= 0.5001f ? 1 : 2).Concat(new[] { oldEnd }).ToList());
                control.Horse.Commands.Run(old);
                var guard = 0;
                while (!old.IsFinished && guard++ < 40) NativeTick(control, control.Horse);
                // The original z >= 0.60 geometry happens to stop the round-6 carrier exactly on the boundary (why round 6 passed);
                // the reviewer's z >= 0.54 boundary and the near start expose the defect: completion short of sight.
                if (sight < 0.6f)
                    Check(old.IsFinished && control.Horse.Position.z < sight && control.Rider.EyePosition.z < sight,
                        "R6-F1 " + flankCase.Name + " control: the round-6 carrier completes at " + control.Horse.Position + ", short of sight (blind)");
                else
                    Check(old.IsFinished && control.Horse.Position.z >= sight - 1e-4f,
                        "R6-F1 " + flankCase.Name + " control: in the original geometry the round-6 carrier stops on the boundary at " + control.Horse.Position);
            }
        }

        // Review R7-F1: affordable openings between the round-6 walk's samples, exercised through click admission and carrier
        // revalidation; the measured cost of a successful walk is used. Straight 3 m route east; the target is in pair reach from
        // x = 1.0 on; the rider sees it only through the opening.
        private static void R7F1()
        {
            foreach (var spec in new[] { new { Lo = 1.445f, Hi = 1.455f, Grant = 1.524f, Admit = true, Name = "opening 1.445-1.455 m, grant 1.524 m" },
                                         new { Lo = 1.412f, Hi = 1.428f, Grant = 1.42f, Admit = true, Name = "opening 1.412-1.428 m, grant 1.42 m" },
                                         new { Lo = 1.412f, Hi = 1.428f, Grant = 1.50f, Admit = true, Name = "opening 1.412-1.428 m, grant 1.50 m" },
                                         new { Lo = 1.412f, Hi = 1.428f, Grant = 1.40f, Admit = false, Name = "control: grant ending before the opening" },
                                         new { Lo = 9f, Hi = 9f, Grant = 3f, Admit = false, Name = "control: route fully blocked" } })
            {
                var lo = spec.Lo; var hi = spec.Hi;
                Func<Vector3, Vector3, bool> opening = (from, to) => !(from.x >= lo && from.x <= hi);
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), opening, true);
                var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, spec.Grant);
                string reason;
                var decision = PairedAttackClickAdmissionPolicy.Decide(true, true, true, true, true, true, false, false, true, true,
                    probe.AdmissibleNow, probe.RouteComplete, probe.RouteProven, probe.RouteMetres, spec.Grant, out reason);
                if (!spec.Admit)
                {
                    Check(decision == PairedAttackClickAdmission.NativeDecision && probe.Route == null,
                        "R7-F1 " + spec.Name + ": refused, native decision kept (" + decision + ":" + reason + ", " + probe.Detail + ")");
                    continue;
                }
                var end = probe.Route == null ? default(Vector3) : probe.Route[probe.Route.Count - 1];
                Check(decision == PairedAttackClickAdmission.AdmitRoute && end.x >= lo && end.x <= hi && end.x <= spec.Grant + 1e-4f &&
                    Math.Abs(probe.RouteMetres - end.x) < 1e-3f && probe.RouteMetres <= spec.Grant,
                    "R7-F1 " + spec.Name + ": click admitted with its measured cost (" + decision + ":" + reason + ", destination " + end + ", cost " +
                    probe.RouteMetres.ToString("R") + ", " + probe.Detail + ")");
                var controller = new HarnessController(world.Rider, world.Horse);
                controller.unifiedTurn.GrantMetres = spec.Grant;
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
                var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
                Check(carrier != null && controller.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal) && admittedAt.x >= lo && admittedAt.x <= hi &&
                    atRelease == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 && carrier.TravelledMetres <= spec.Grant + 1e-4f,
                    "R7-F1 " + spec.Name + ": carrier revalidation proves it again, the horse stops at " + admittedAt + " and one strike is admitted");
            }
        }


        // ======== PR #70 rounds 2-3 (owner instruction 2026-11-26f section 6) ========
        private sealed class Connected
        {
            public World World; public MountedCombatController Combat; public HarnessPairParticipant Pair; public TurnController Context; public string Arrangement, MountSlot;
            // PR #70 round 7: the sight opening (mutable), the action-stage order and the connected scheduler's counters.
            public float[] Opening; public bool HorseFirst; public int CarrierTicks, IneligibleCarrierFrames, NotSteeringFrames;
            public Action BeforeDrive, ActionStage;
            // Round 19 (R17-F1): runs once before the next frame's UnitMoveController stage (e.g. a movement-prevention guard on that tick).
            public Action BeforeMountTick;
            // Round 10: the horse's position and the agent's direction at the decision frame's controller drive (the executed first tick).
            public Vector3 HorseAtDecision; public UnityEngine.Vector2 DirectionAtDecision;
        }

        // PR #70 R1-F10 (round 4): an established pair is paired through PRODUCTION preparation at the RIDER's native boundary in either
        // initiative arrangement. Mount first: the mount's own context reaches the ACTUAL BeginPairedPreparation first, which reserves nothing
        // there (the activation owns the partner's slot); then the rider's context begins the activation (Begin, the rider's reserved grant,
        // its allocation) and CompletePairedPreparation's partner context receives the mount's grant under the activation identity
        // (BeginGrantedPreparation). The harness writes no addressability, mode flag, grant identity or prior expenditure.
        private static Connected Connect(World world, bool mountFirst, bool preparing = false, bool viaHook = false)
        {
            var combat = Controller5(world);
            var pair = new HarnessPairParticipant(world.Rider, world.Horse, combat);
            combat.unifiedTurn.Participant = pair;
            var controller = Kingmaker.Game.Instance.TurnBasedCombatController;
            // PR #70 R1-F10 (round 5): the native initiative loop hands each candidate to the ACTUAL SuppressPairedCandidate; only a candidate it
            // does not suppress becomes a turn and is natively prepared. A lost exclusion makes the horse a turn and fails the fixture.
            var roster = mountFirst ? new[] { world.Horse, world.Rider } : new[] { world.Rider, world.Horse };
            var considered = new List<string>();
            TurnController context = null;
            foreach (var unit in roster)
            {
                var suppressed = pair.SuppressPairedCandidate(new TurnBased.Controllers.CombatController.TBUnitInfo { Unit = unit });
                considered.Add(unit.UniqueId + (suppressed ? ":suppressed" : ":turn"));
                if (suppressed) continue;
                if (unit != world.Rider) throw new InvalidOperationException("The established partner was not excluded from the initiative loop: " + string.Join(",", considered));
                context = new TurnController(world.Rider) { Status = TurnController.TurnStatus.Preparing };
                controller.CurrentTurn = context;
                if (!pair.NativePrepare(context)) throw new InvalidOperationException("The rider's paired preparation was refused.");
                pair.CompleteBoundaryPreparation(context);
                // PR #70 R5-F3 (round 7): IsActing is derived from Status; a round-7 fixture keeps the fresh Preparing turn (preparing).
                if (!preparing) context.Status = TurnController.TurnStatus.Acting;
            }
            var mountSlot = "roster " + string.Join(" -> ", considered) + "; skips=" + pair.RedundantMountTurnSkipCount;
            Harness.SelectionManager.Instance.SelectedUnits.Clear();
            Harness.SelectionManager.Instance.SelectedUnits.Add(world.Rider);
            world.MountMoveTick = metres =>
            {
                var speed = world.Horse.CurrentSpeedMps;
                var dt = world.TickSeconds > 0f ? world.TickSeconds : metres / speed;
                world.LastMountTick = pair.movementState.TickDelegated(pair.PairedMovementContext(context), world.Horse, ref dt);
                return Math.Min(dt * speed, metres);
            };
            if (viaHook)
            {
                // PR #70 R1-F6 (round 7): the mount's movement tick reaches the product only through the ACTUAL TryOverrideMountTurnMovement (production
                // MountMovementPrefix); a refusal there is native's denial for the off-turn mount (no movement). Native Run's Hold write asks the ACTUAL
                // NativeRunKeepsHold (PatchMethods.NativeRunHoldWrite).
                UnitCommands.NativeRunHoldHook = (owner, command, fromQueue) => combat.NativeRunKeepsHold(owner, command, fromQueue, false);
                world.MountMoveDelta = requested =>
                {
                    var dt = requested;
                    combat.unifiedTurn.LastMovementObservation = null;
                    bool moved;
                    var admitted = combat.TryOverrideMountTurnMovement(world.Horse.View.AgentASP, ref dt, out moved);
                    world.LastHookHandled = admitted; world.LastHookResult = moved;
                    world.LastMountTick = combat.unifiedTurn.LastMovementObservation ?? "hook-not-admitted";
                    return admitted && moved ? dt : 0f;
                };
                world.MountMoveTick = metres =>
                {
                    var speed = world.Horse.CurrentSpeedMps;
                    var dt = world.TickSeconds > 0f ? world.TickSeconds : metres / speed;
                    combat.unifiedTurn.LastMovementObservation = null;
                    bool moved;
                    var admitted = combat.TryOverrideMountTurnMovement(world.Horse.View.AgentASP, ref dt, out moved);
                    world.LastMountTick = combat.unifiedTurn.LastMovementObservation ?? "hook-not-admitted";
                    // The installed shift: the agent's MaxSpeed times the delta the hook returned (MountedAgentSteering).
                    return admitted && moved ? Math.Min(dt * world.Horse.View.AgentASP.MaxSpeed, metres) : 0f;
                };
            }
            return new Connected { World = world, Combat = combat, Pair = pair, Context = context, MountSlot = mountSlot,
                Arrangement = mountFirst ? "mount-first initiative" : "rider-first initiative" };
        }

        // Production adoption after the horse already acted this round (its own slot ran before the pair existed): the rider's running
        // boundary is adopted with RetainPartnerParticipation; the horse is ended for this allocation and has no partner context.
        private static Connected ConnectAdoptedAfterHorseActed(World world)
        {
            var combat = Controller5(world);
            var pair = new HarnessPairParticipant(world.Rider, world.Horse, combat);
            combat.unifiedTurn.Participant = pair;
            var context = new TurnController(world.Rider) { Status = TurnController.TurnStatus.Acting };
            Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn = context;
            var adopted = pair.AdoptAfterPartnerActed(context);
            Harness.SelectionManager.Instance.SelectedUnits.Clear();
            Harness.SelectionManager.Instance.SelectedUnits.Add(world.Rider);
            return new Connected { World = world, Combat = combat, Pair = pair, Context = context, Arrangement = "adopted after the horse acted",
                MountSlot = "adopted=" + adopted + ",partnerEnded=" + (pair.Activation.State(world.Horse)?.Ended == true) + ",partnerContext=" + (pair.PartnerContextForHarness != null) };
        }

        // An exact mount grant in metres: speed 1 m/s, Standard spent (no conversion), Move cooldown leaving exactly the grant.
        private static void GrantMetres(World world, float metres)
        {
            world.Horse.CombatSpeedMps = 1f;
            world.Horse.CombatState.Cooldown.StandardAction = 6f;
            world.Horse.CombatState.Cooldown.MoveAction = 3f - metres;
        }

        private sealed class ClickOutcome { public bool Admitted, NativeIgnoredAfterPostfix, RunPassedToNative; public string Decision; public MountedPairAttackCommand Order; }

        // One native PointerController.Tick on the target: IgnoreClick, whose ACTUAL postfix decides on a dropped click (a click native still
        // ignores ends there); otherwise ClickUnitHandler.OnClick's bracket around the native hostile-click request and the UnitAttack's
        // synchronous Run, whose first step binds the request and whose ACTUAL TryRouteMountedStockAttack routes it (and drives the ACTUAL
        // dispatch). handled=false: the bracket runs but no Run reaches the pair (an abandoned click).
        private static ClickOutcome ClickTick(Connected c, UnitEntityData target, bool nativeIgnored = true, Action<UnitAttack> alterPrediction = null, bool handled = true)
        {
            var outcome = new ClickOutcome();
            var predictions = new UnitCommands(c.World.Rider);
            var predicted = new UnitAttack(target);
            predicted.Init(c.World.Rider);
            alterPrediction?.Invoke(predicted);
            predictions.Raw[(int)UnitCommand.CommandType.Standard] = predicted;
            c.Context.SetNativeClick(target, predictions, false);
            c.Combat.BeginPointerDispatch();   // the ACTUAL PointerController.Tick prefix
            try
            {
                var result = nativeIgnored;
                c.Pair.AdmitPairedAttackClick(c.Context, ref result);
                outcome.NativeIgnoredAfterPostfix = result;
                outcome.Admitted = nativeIgnored && !result;
                outcome.Decision = c.Pair.LastAttackClickDecision;
                if (result) return outcome;
                Request(c, target, handled, outcome);
                return outcome;
            }
            finally { c.Combat.EndPointerDispatch(); }   // the ACTUAL PointerController.Tick postfix
        }

        // ClickUnitHandler.OnClick's bracket alone (no IgnoreClick before it in this frame).
        private static void Request(Connected c, UnitEntityData target, bool handled = true, ClickOutcome outcome = null)
        {
            c.Combat.BeginClickObservation(Harness.SelectionManager.Instance.SelectedUnits, false);
            if (handled)
            {
                c.Combat.ObserveStockAttackRequested(c.World.Rider, target);
                var attack = new UnitAttack(target) { CreatedByPlayer = true };
                c.Combat.BindObservedStockRequest(c.World.Rider.Commands, attack);
                var passed = c.Combat.TryRouteMountedStockAttack(c.World.Rider.Commands, attack);
                if (outcome != null) outcome.RunPassedToNative = passed;
                // PR #70 R6-F1 (round 7): native OnClick assigns the clicked unit after the Run returns, through the ACTUAL transpiled hook.
                foreach (var unit in Harness.SelectionManager.Instance.SelectedUnits.ToArray()) PatchMethods.ClickManualTargetAssignment(unit.CombatState, target);
            }
            c.Combat.EndClickObservation(target, handled);
            if (outcome != null) outcome.Order = c.World.Rider.Commands.Standard as MountedPairAttackCommand;
        }

        private sealed class Spend
        {
            public float MountMove, MountStepped; public bool RiderStandard;
            public override string ToString() => "mountMove=" + MountMove.ToString("0.####") + "s,mountStepped=" + MountStepped.ToString("0.####") + "m,riderStandard=" + (RiderStandard ? "unspent" : "spent");
        }
        private static Spend Observe(Connected c) => new Spend { MountMove = c.World.Horse.CombatState.Cooldown.MoveAction,
            MountStepped = c.Pair.movementState.AllocationMetresStepped(c.World.Horse), RiderStandard = c.World.Rider.HasStandard };

        // Native ticks until the carrier is released (the transaction attacks) and then the strike's first act.
        private static void RunToStrike(Connected c, MountedPairAttackCommand order, out Spend atRelease, out Spend atStrike, out MountedPairNativeAdmissionState admission)
        {
            atRelease = null; atStrike = null; admission = MountedPairNativeAdmissionState.Unavailable;
            var safety = 0;
            while (order != null && !order.IsFinished && order.Transaction.State != MountedCombatTransactionState.Attacking && safety++ < 200)
            { NativeTick(c.World, c.World.Horse); NativeTick(c.World, c.World.Rider); }
            if (order == null || order.Transaction.State != MountedCombatTransactionState.Attacking) return;
            atRelease = Observe(c);
            admission = order.EvaluateCurrentNativeAdmission();
            for (var tick = 0; tick < 8 && order.StruckTargets.Count == 0; tick++)
            {
                if (order.IsStarted && !order.IsFinished) order.ActEventDue = true;
                NativeTick(c.World, c.World.Rider);
            }
            atStrike = Observe(c);
        }

        private static string Spent(Spend before, Spend release, Spend strike) => "before travel {" + before + "}, at carrier release {" + release + "}, at strike {" + strike + "}";
        private static string LastLine(Connected c) => ((HarnessLogger)c.Pair.Logger).Lines.LastOrDefault() ?? "";
        private static string Field(string line, string name) { var at = line.IndexOf(name + "=", StringComparison.Ordinal); if (at < 0) return "?"; var end = line.IndexOf(';', at); return line.Substring(at + name.Length + 1, (end < 0 ? line.Length : end) - at - name.Length - 1); }

        private static World Corner()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f && from.x > 1.0f;
            var turnPoint = new Vector3(2f, 0f, 0f);
            var w = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
            w.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, turnPoint, new Vector3(2f, 0f, 1.5f) };
            return w;
        }

        // R1-F10: both initiative arrangements, connected through the ACTUAL click admission, the ACTUAL stock request and dispatch, the proof,
        // the mount's allocation (grant read on the mount's own context and per-tick debit through TickDelegated), carrier release and strike;
        // ordinary movement, a five-foot step, a changed grant delivered through the proof's established position, exhausted or ended actors,
        // cancellation and retargeting.
        private static void R2F10Connected()
        {
            foreach (var mountFirst in new[] { false, true })
            {
                var world = Corner();
                var c = Connect(world, mountFirst);
                var before = Observe(c);
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                var order = click.Order;
                var attached = c.Combat.LastClickProofDisposition;
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, order, out atRelease, out atStrike, out admission);
                var debited = atRelease == null ? -1f : atRelease.MountMove - before.MountMove;
                Check(click.Admitted && !click.RunPassedToNative && order != null && order.RouteBound && attached.StartsWith("attached:", StringComparison.Ordinal) &&
                    c.Combat.StockAttackIntentStartCount == 1 && Field(line, "mountStepMode") == "False" && Field(line, "mountSingleMove") == "False",
                    "R1-F10 " + c.Arrangement + ": the ACTUAL admission (" + click.Decision + ", mode step=" + Field(line, "mountStepMode") + " single=" + Field(line, "mountSingleMove") +
                    ", grant " + Field(line, "mountGrantedMetres") + " m on the mount's own context) and the ACTUAL stock request and dispatch bound the order (" + attached + ")");
                Check(atRelease != null && atStrike != null && before.RiderStandard && atRelease.RiderStandard && !atStrike.RiderStandard && debited > 0f &&
                    atStrike.MountMove == atRelease.MountMove && Math.Abs(c.Pair.movementState.AllocationMoveUsed(world.Horse) - atRelease.MountMove) < 1e-4f &&
                    admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 && UnitAttack.StandardSpends == 1,
                    "R1-F10 " + c.Arrangement + ": " + Spent(before, atRelease, atStrike) + "; the mount paid " + debited.ToString("0.####") + " s through its allocation, the rider's Standard once at the strike");
            }
            // A five-foot step whose legal stopping point lies in the step's final portion (sight from x = 1.45; step grant 1.524 m at 9 m/s).
            {
                Func<Vector3, Vector3, bool> late = (from, to) => from.x < 1.45f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.5f, 0f, 0f), late, true);
                var c = Connect(world, false);
                c.Context.EnabledFiveFootStep = true;
                var before = Observe(c);
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(click.Admitted && Field(line, "mountStepMode") == "True" && Math.Abs(float.Parse(Field(line, "mountGrantedMetres"), System.Globalization.CultureInfo.InvariantCulture) - 1.524f) < 1e-4f &&
                    atRelease != null && atRelease.MountStepped >= 1.45f && atRelease.MountStepped <= 1.524f + 1e-4f && atRelease.MountMove == before.MountMove &&
                    admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 && !atStrike.RiderStandard,
                    "R1-F10/R2-F1 five-foot step: grant " + Field(line, "mountGrantedMetres") + " m (step mode), stopping point in the step's final portion; " + Spent(before, atRelease, atStrike));
                var spent = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.5f, 0f, 0f), late, true);
                var cs = Connect(spent, false);
                cs.Context.EnabledFiveFootStep = true;
                cs.Pair.movementState.SetStepped(spent.Horse, 1.524f);
                var refused = ClickTick(cs, spent.Target);
                Check(!refused.Admitted && refused.Order == null && Field(LastLine(cs), "mountGrant") == "mount-movement-spent",
                    "R1-F10/R2-F1 exhausted step control: refused (" + refused.Decision + "; mountGrant=" + Field(LastLine(cs), "mountGrant") + ")");
            }
            // A changed grant: the click proves a 7 mm window (1.5205-1.5275 m, no 1 cm grid point) through the step's grant point (1.524 m);
            // before the carrier the player chooses ordinary movement (no step taken yet), so the grant is no longer that point; the proof
            // delivers the established destination to the order's revalidation, which proves the window again.
            {
                Func<Vector3, Vector3, bool> window = (from, to) => !(from.x >= 1.5205f && from.x <= 1.5275f);
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.5f, 0f, 0f), window, true);
                var c = Connect(world, false);
                c.Context.EnabledFiveFootStep = true;
                var click = ClickTick(c, world.Target);
                var established = c.Combat.LastAttackClickProbe?.Route?.Last();
                c.Context.EnabledFiveFootStep = false;
                var without = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 54f);
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(click.Admitted && established.HasValue && c.Combat.LastRouteRevalidation != null &&
                    c.Combat.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal) && admission == MountedPairNativeAdmissionState.Admitted &&
                    world.Horse.Position.x >= 1.5205f && world.Horse.Position.x <= 1.5275f && UnitAttack.NativeAttackStarts == 1 && atRelease.MountMove > 0f,
                    "R1-F10 changed grant (step 1.524 m -> ordinary): the established destination " + established + " reached the revalidation through the proof (" +
                    Field(c.Combat.LastRouteRevalidation, "grant") + " m grant; without it: " + without.Detail + "); one strike; " + atRelease + " [first debit: " + c.Combat.LastFirstTickDecision + "]");
            }
            // Exhausted or ended actors keep native's refusal: no proof, no order.
            foreach (var spec in new[] { "mount movement spent", "rider Standard spent", "mount cannot act", "rider not addressable" })
            {
                var world = Corner();
                if (spec == "mount movement spent") { world.Horse.CombatState.Cooldown.MoveAction = 3f; world.Horse.CombatState.Cooldown.StandardAction = 6f; }
                if (spec == "rider Standard spent") world.Rider.HasStandard = false;
                if (spec == "mount cannot act") world.Horse.Descriptor.State.CanAct = false;
                var c = Connect(world, false);
                if (spec == "rider not addressable") c.Pair.EndActorForHarness(world.Rider);
                var click = ClickTick(c, world.Target);
                Check(!click.Admitted && click.NativeIgnoredAfterPostfix && click.Order == null && c.Combat.PendingClickProofForDiagnostics == null && !c.Combat.HasStockAttackIntent,
                    "R1-F10 " + spec + ": native refusal kept, no proof, no order (" + (click.Decision ?? "<not a rider melee input>") + "; mountGrant=" + Field(LastLine(c), "mountGrant") + ")");
            }
            // Cancellation: the live carrier stops, the proof is gone, the mount pays only what it travelled, no strike.
            {
                var world = Corner();
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                var carrier = click.Order?.DelegatedMove;
                NativeTick(world, world.Horse);
                var paid = Observe(c);
                c.Combat.Cancel("player cancelled");
                for (var tick = 0; tick < 12; tick++) { NativeTick(world, world.Horse); NativeTick(world, world.Rider); }
                var after = Observe(c);
                Check(click.Admitted && carrier != null && carrier.IsFinished && click.Order.IsFinished && c.Combat.IntentClickProofForDiagnostics == null &&
                    c.Combat.PendingClickProofForDiagnostics == null && !c.Combat.HasStockAttackIntent && after.MountMove == paid.MountMove && paid.MountMove > 0f &&
                    after.RiderStandard && UnitAttack.NativeAttackStarts == 0,
                    "R1-F10 cancellation: the carrier stopped, the proof is gone; " + paid + " when cancelled, unchanged after (" + after + "); no strike");
            }
            // Retargeting while the first order is live: the busy rider's click on another hostile is native's (round-1 rule) and native
            // accepts it; the ACTUAL request handler ends the first order and its carrier; no proof passes; only the new target is struck.
            {
                var world = Corner();
                var c = Connect(world, false);
                var first = ClickTick(c, world.Target).Order;
                NativeTick(world, world.Rider); NativeTick(world, world.Horse);
                var firstCarrier = first?.DelegatedMove;
                var second = new UnitEntityData("hostile-2") { Position = new Vector3(3.6f, 0f, 1.9f) };
                second.View.Corpulence = 0.5f;
                UnityEngine.Time.frameCount++;
                var click = ClickTick(c, second, false);
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(first != null && first.IsFinished && firstCarrier != null && firstCarrier.IsFinished && !click.NativeIgnoredAfterPostfix && click.Order != null &&
                    !click.Order.RouteBound && c.Combat.IntentClickProofForDiagnostics == null && click.Order.StruckTargets.Count >= 1 &&
                    click.Order.StruckTargets.All(t => t == second) && first.StruckTargets.Count == 0,
                    "R1-F10 retarget (native-accepted click on another hostile; postfix left native's result " + click.NativeIgnoredAfterPostfix + "): the first order and its carrier ended, no proof passed, only the new target was struck" +
                    " [order " + (click.Order == null ? "-" : click.Order.IsFinished + "/" + click.Order.EndCause + "(" + click.Order.EndCauseDetail + "); carriers issued " + click.Order.DelegatedMoveStarts + "; strikes " + click.Order.StruckTargets.Count) + "]");
            }
        }

        // R1-F1: the reviewer's 1.412 m opening with a 1.410 m grant, through the ACTUAL admission with the grant read from the allocation.
        private static void R2F1BeyondGrant()
        {
            Func<Vector3, Vector3, bool> slot = (from, to) => !(from.x >= 1.412f && from.x <= 1.428f);
            foreach (var grant in new[] { 1.410f, 1.42f })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), slot, true);
                GrantMetres(world, grant);
                var c = Connect(world, false);
                c.Combat.BeginOrder(world.Target, false);
                var prior = new MountedPairAttackCommand(world.Rider, world.Horse, world.Target, MountedCombatActionKind.RiderMelee, world.Logger);
                c.Combat.AcceptWrapper(prior, world.Rider, world.Target);
                var priorGeneration = c.Combat.Generation;
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                var label = "grant " + grant.ToString("0.000") + " m";
                if (grant < 1.412f)
                {
                    Check(!click.Admitted && click.NativeIgnoredAfterPostfix && click.Decision == "NativeDecision:no-proven-route" && c.Combat.PendingClickProofForDiagnostics == null &&
                        c.Combat.Generation == priorGeneration && ReferenceEquals(c.Combat.IntentCommand, prior) && !prior.IsFinished &&
                        world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && Field(line, " diagnosticReachMetres").StartsWith("1.41", StringComparison.Ordinal),
                        "R1-F1 " + label + ": native refusal and the prior order kept, no replacement carrier; the diagnostic reach (" + Field(line, " diagnosticReachMetres") + " m) only reported");
                    continue;
                }
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(click.Admitted && prior.IsFinished && click.Order != null && admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 &&
                    world.Horse.Position.x <= grant + 1e-4f && atRelease != null && atRelease.MountMove <= 3f + 1e-4f,
                    "R1-F1 affordable control (" + label + "): admitted (" + click.Decision + "), the horse stopped at " + world.Horse.Position + ", one strike; " + atRelease);
            }
        }

        // R1-F7: proofs belong to the exact click bracket. Continuation of an undispatched order (the horse still owns its Standard command),
        // a native-accepted click on the live carrier, and the abandoned and cancelled clicks followed by another same-target request.
        private static void R2F7ProofOwnership()
        {
            var world = Corner();
            var c = Connect(world, false);
            // An undispatched order: the horse's earlier two-attack routine on another hostile is still live (delivered once), so after each
            // click starts or continues the new order the ACTUAL drive waits (a pair command is active).
            var near = new UnitEntityData("hostile-0") { Position = new Vector3(1.5f, 0f, 0f) };
            near.View.Corpulence = 0.5f;
            c.Combat.BeginOrder(near, false);
            var routine = new MountedPairAttackCommand(world.Rider, world.Horse, near, MountedCombatActionKind.MountPrimaryNatural, world.Logger);
            routine.PreStartMovementForbidden = () => false;
            world.Horse.Commands.Run(routine);
            c.Combat.AcceptWrapper(routine, world.Horse, near);
            routine.BindTerminal(c.Combat.ProductionTerminal);
            Deliver(world, routine, world.Horse);
            var first = ClickTick(c, world.Target);
            var firstProof = c.Combat.IntentClickProofForDiagnostics;
            UnityEngine.Time.frameCount++;
            var second = ClickTick(c, world.Target);
            var secondProof = c.Combat.IntentClickProofForDiagnostics;
            var undispatched = first.Order == null && second.Order == null;
            routine.ActEventDue = true;
            NativeTick(world, world.Horse);   // the routine's second act ends it
            c.Combat.Drive();
            var order = world.Rider.Commands.Standard as MountedPairAttackCommand;
            var attached = c.Combat.LastClickProofDisposition;
            NativeTick(world, world.Rider); NativeTick(world, world.Horse);
            var carrier = order?.DelegatedMove;
            UnityEngine.Time.frameCount++;
            var accepted = ClickTick(c, world.Target, false);
            // While the earlier routine (a pair command) is active, the dispatcher's own admission refuses a further rider melee click
            // (rider-attack-refused): the second same-target click keeps native's refusal and leaves no proof, so the undispatched order keeps
            // the first click's proof, bound to its generation, and its first carrier carries exactly that proof.
            Check(first.Admitted && !second.Admitted && second.NativeIgnoredAfterPostfix && second.Decision == "NativeDecision:rider-attack-refused" && undispatched &&
                firstProof != null && ReferenceEquals(secondProof, firstProof) && order != null && order.RouteBound && attached == "attached:#" + firstProof.Sequence &&
                carrier != null && ReferenceEquals(order.DelegatedMove, carrier) && !carrier.IsFinished && !accepted.NativeIgnoredAfterPostfix && c.Combat.PendingClickProofForDiagnostics == null,
                "R1-F7: an overridden click started an undispatched order (proof #" + firstProof?.Sequence + "); a second same-target click on it was refused by the actual admission (" +
                second.Decision + ", no proof); the first carrier carries the first click's proof (" + attached + "); a native-accepted click (postfix left native's result " +
                accepted.NativeIgnoredAfterPostfix + ") kept the live carrier");
            Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
            RunToStrike(c, order, out atRelease, out atStrike, out admission);
            Check(admission == MountedPairNativeAdmissionState.Admitted && order.StruckTargets.Count >= 1 && order.StruckTargets.All(t => t == world.Target),
                "R1-F7: the kept carrier was released and the order struck its own target");
            foreach (var spec in new[] { "abandoned", "cancelled" })
            {
                var w = Corner();
                var cc = Connect(w, false);
                ClickOutcome overridden;
                if (spec == "abandoned") overridden = ClickTick(cc, w.Target, true, null, false);
                else
                {
                    var predictions = new UnitCommands(w.Rider); var predicted = new UnitAttack(w.Target); predicted.Init(w.Rider);
                    predictions.Raw[(int)UnitCommand.CommandType.Standard] = predicted;
                    cc.Context.SetNativeClick(w.Target, predictions, false);
                    var result = true;
                    cc.Combat.BeginPointerDispatch();   // round 5 (R4-F2): an override exists only inside an input dispatch
                    cc.Pair.AdmitPairedAttackClick(cc.Context, ref result);
                    overridden = new ClickOutcome { Admitted = !result };
                    cc.Combat.Cancel("player cancelled");
                    cc.Combat.EndPointerDispatch();
                }
                var disposition = cc.Combat.LastClickProofDisposition;
                var later = new ClickOutcome();
                Request(cc, w.Target, true, later);
                Check(overridden.Admitted && later.Order != null && !later.Order.RouteBound && cc.Combat.IntentClickProofForDiagnostics == null,
                    "R1-F7 " + spec + " overridden click (" + disposition + "), then another same-target request in the same turn and frame through the ACTUAL stock handler: it got no earlier proof (native routing)");
            }
        }

        // R1-F8: a fresh click on an already unconscious, living, attackable enemy that needs a short approach; death and loss of
        // consciousness after ordering invalidate.
        private static void R2F8FreshTargetLife()
        {
            foreach (var spec in new[] { "unconscious when ordered", "falls unconscious after ordering", "dies after ordering" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.2f, 0f, 0f), null, true);
                if (spec == "unconscious when ordered") world.Target.Descriptor.State.IsConscious = false;
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                if (spec == "falls unconscious after ordering") world.Target.Descriptor.State.IsConscious = false;
                if (spec == "dies after ordering") world.Target.Descriptor.State.IsDead = true;
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                for (var tick = 0; tick < 6; tick++) { NativeTick(world, world.Horse); NativeTick(world, world.Rider); }
                if (spec == "unconscious when ordered")
                    Check(click.Admitted && click.Order != null && click.Order.RouteBound && UnitAttack.NativeAttackStarts == 1 && click.Order.StruckTargets.Contains(world.Target) &&
                        !UnifiedMountedStockAttackPolicy.TargetLifeAdmits(false, false, false, false),
                        "R1-F8 " + spec + ": the fresh click's own fact admitted it (" + click.Decision + "), short approach and one strike");
                else
                    Check(click.Admitted && click.Order != null && click.Order.IsFinished && UnitAttack.NativeAttackStarts == 0 && click.Order.StruckTargets.Count == 0,
                        "R1-F8 " + spec + ": the order ended without a strike (" + click.Order?.EndCause + ")");
            }
        }

        // R1-F5: a nonempty partial result whose last point appears admissible never authorizes movement; complete and error controls.
        private static void R2F5PathOutcome()
        {
            foreach (var outcome in new[] { PathCompleteState.Partial, PathCompleteState.Complete, PathCompleteState.Error })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.2f, 0f, 0f), null, true);
                var state = outcome;
                world.Horse.View.AgentASP.Outcome = (from, to) => state;
                var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 9f);
                string reason;
                var decision = PairedAttackClickAdmissionPolicy.Decide(true, true, true, true, true, true, false, false, true, true,
                    probe.AdmissibleNow, probe.RouteComplete, probe.RouteProven, probe.RouteMetres, 9f, out reason);
                var lastPoint = world.Horse.View.AgentASP.Route(world.Horse.Position, world.Target.Position).Last();
                var strike = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
                strike.Init(world.Rider);
                var endAdmits = strike.EvaluateNativeAdmissionAtMountPosition(lastPoint, lastPoint - world.Horse.Position) == MountedPairNativeAdmissionState.Admitted;
                if (outcome == PathCompleteState.Complete)
                    Check(decision == PairedAttackClickAdmission.AdmitRoute && probe.RouteProven, "R1-F5 complete control: a complete route is admitted (" + decision + ":" + reason + ", " + probe.Detail + ")");
                else
                    Check(decision == PairedAttackClickAdmission.NativeDecision && probe.Route == null && probe.Path.ToString() == outcome.ToString() && (outcome == PathCompleteState.Error || endAdmits),
                        "R1-F5 " + outcome + ": refused although " + (endAdmits ? "its last point admits the strike" : "no route") + " (" + decision + ":" + reason + ", " + probe.Detail + ")");
            }
        }

        // R1-F3: a first opening too narrow for any arrival is passed; the 7 mm window through admission and revalidation, with a changed grant
        // that needs the destination established at planning; a 5 mm window is refused.
        private static void R2F3Discovery()
        {
            Func<Vector3, Vector3, bool> twoOpenings = (from, to) => !(from.x >= 1.218f && from.x <= 1.222f || from.x >= 1.40f && from.x <= 1.50f);
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), twoOpenings, true);
            var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 3f);
            var end = probe.Route == null ? default(Vector3) : probe.Route.Last();
            Check(probe.RouteProven && probe.Openings == 2 && end.x >= 1.40f + probe.ArrivalRadius && end.x <= 1.50f,
                "R1-F3 two openings: discovery passed the 4 mm opening and chose destination " + end + ", cost " + probe.RouteMetres.ToString("R") + ", radius " + probe.ArrivalRadius.ToString("R") + " (" + probe.Detail + ")");
            var controller = new HarnessController(world.Rider, world.Horse);
            controller.unifiedTurn.GrantMetres = 3f;
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
            var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
            Check(carrier != null && admittedAt.x >= 1.40f && admittedAt.x <= 1.50f && atRelease == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1,
                "R1-F3 two openings: carrier revalidation proved it again, the horse stopped at " + admittedAt + " and one strike was admitted");
            // The 7 mm window (1.4115..1.4185 m, no 1 cm grid point): found at a 1.418 m grant through its grant point.
            Func<Vector3, Vector3, bool> window = (from, to) => !(from.x >= 1.4115f && from.x <= 1.4185f);
            var w7 = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), window, true);
            var click = PairedAttackClickProbe.Evaluate(w7.Rider, w7.Horse, w7.Target, 1.418f);
            string reason;
            var decision = PairedAttackClickAdmissionPolicy.Decide(true, true, true, true, true, true, false, false, true, true,
                click.AdmissibleNow, click.RouteComplete, click.RouteProven, click.RouteMetres, 1.418f, out reason);
            var established = click.Route == null ? (Vector3?)null : click.Route.Last();
            Check(decision == PairedAttackClickAdmission.AdmitRoute && click.ArrivalRadius <= 0.003125f + 1e-6f && established.Value.x >= 1.4115f && established.Value.x <= 1.4185f,
                "R1-F3 7 mm window: click admitted at a 1.418 m grant (" + decision + ":" + reason + ", destination " + established + ", cost " + click.RouteMetres.ToString("R") + ", radius " + click.ArrivalRadius.ToString("R") + ")");
            // The grant changes to 1.50 m before the carrier: the established destination keeps the window; without it the walk misses it.
            var without = PairedAttackClickProbe.Evaluate(w7.Rider, w7.Horse, w7.Target, 1.50f);
            var revalidator = new HarnessController(w7.Rider, w7.Horse) { Established = established };
            revalidator.unifiedTurn.GrantMetres = 1.50f;
            var o7 = Order(w7, MountedCombatActionKind.RiderMelee, false);
            var c7 = RunRouteBound(w7, revalidator, o7, out admittedAt, out atRelease);
            Check(!without.RouteProven && c7 != null && revalidator.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal) && admittedAt.x >= 1.4115f &&
                admittedAt.x <= 1.4185f && atRelease == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1,
                "R1-F3 changed grant (1.418 -> 1.50 m): revalidation with the established destination proves the window again (without it: " + without.Detail +
                "); the horse stopped at " + admittedAt + " and one strike was admitted");
            // Refusal control: a 5 mm window holds no supported arrival disc.
            Func<Vector3, Vector3, bool> narrow = (from, to) => !(from.x >= 1.4175f && from.x <= 1.4225f);
            var w5 = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), narrow, true);
            var refused = PairedAttackClickProbe.Evaluate(w5.Rider, w5.Horse, w5.Target, 3f);
            Check(!refused.RouteProven && refused.Detail.Contains("arrival-unverifiable"),
                "R1-F3 5 mm window control: refused (" + refused.Detail + ")");
        }

        // R1-F2: a bend whose incoming segment enters the round-7 arrival disc while the rider is blind; the outgoing heading lets the rider see.
        private static void R2F2Bend()
        {
            var vertex = new Vector3(2f, 0f, 0f);
            var outgoing = new Vector3(1.0f, 0f, 0.3f);
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z > 0.1f;
            Func<World> make = () =>
            {
                var w = NewWorld(new Vector3(-0.05f, 0f, 0f), new Vector3(4.2f, 0f, -1.0f), wall, true);
                w.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, vertex, outgoing };
                w.Horse.Orientation = 0f;
                w.Rider.Position = w.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                return w;
            };
            var world = make();
            var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 9f);
            var points = new List<Vector3> { world.Horse.Position, vertex, outgoing };
            var destination = probe.Route == null ? default(Vector3) : probe.Route.Last();
            var beyondVertex = Kingmaker.Utility.GeometryUtils.MechanicsDistance(vertex, destination);
            Check(probe.RouteProven && beyondVertex * 0.287f > probe.ArrivalRadius,
                "R1-F2: the destination " + destination + " is " + beyondVertex.ToString("0.###") + " m past the bend, radius " + probe.ArrivalRadius.ToString("R") +
                ": no incoming position lies inside its disc (" + probe.Detail + ")");
            var controller = new HarnessController(world.Rider, world.Horse);
            controller.unifiedTurn.GrantMetres = 9f;
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
            var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
            Check(carrier != null && carrier.FollowedForcedPath && carrier.TravelledMetres > 2.05f && carrier.Travelled.Any(p => Math.Abs(p.x - 1.95f) < 1e-3f && Math.Abs(p.z) < 1e-4f) &&
                atRelease == MountedPairNativeAdmissionState.Admitted && order.CarrierReleaseProven && UnitAttack.NativeAttackStarts == 1 && order.RouteArrivalContradictionCount == 0,
                "R1-F2: an incoming tick at (1.95,0) passed while the rider was blind; the horse travelled through the turn (" + carrier?.TravelledMetres.ToString("0.###") +
                " m), the exact carrier was released at " + admittedAt + " and one strike was admitted");
            // Control: the round-7 choice (the opening's start + radius + 1 mm, checked only at the final heading) completes on the incoming leg.
            var control = make();
            Vector3 f, t; List<Vector3> truncated;
            var oldDestination = PairedAttackClickProbe.PointAt(points, 2.05f + 0.051f, out f, out t, out truncated);
            var old = new UnitMoveTo(oldDestination, 0.05f) { NeedLoS = false };
            old.ForcedPath = new Pathfinding.ForcedPath(truncated);
            control.Horse.Commands.Run(old);
            var guard = 0;
            while (!old.IsFinished && guard++ < 40) NativeTick(control, control.Horse);
            var blind = new MountedPairSingleAttack(control.Target, control.Rider, control.Horse, true, false);
            blind.Init(control.Rider);
            var blindState = blind.EvaluateCurrentNativeAdmission();
            Check(old.IsFinished && control.Horse.Position.x < 2f && Math.Abs(control.Horse.Position.z) < 1e-4f && blindState != MountedPairNativeAdmissionState.Admitted,
                "R1-F2 control: the round-7 carrier (" + oldDestination + ", 0.05 m) completed on the incoming leg at " + control.Horse.Position + " with the rider blind (" + blindState + ")");
        }

        // R1-F4: the mount-centred route ends where only the horse could see; a bounded native query to a reachable rider-admissible position.
        private static void R2F4Lateral()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z > 0.2f;
            foreach (var closed in new[] { false, true })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), wall, true);
                world.Horse.Orientation = 0f;
                world.Rider.Position = world.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                var target = world.Target.Position;
                if (closed) world.Horse.View.AgentASP.Outcome = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, target) < 1e-4f ? PathCompleteState.Complete : PathCompleteState.Error;
                var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 9f);
                if (closed)
                {
                    Check(!probe.RouteProven && probe.LateralQueries >= 1 && probe.LateralQueries <= PairedAttackClickProbe.MaxLateralQueries,
                        "R1-F4 closed lateral path control: refused after " + probe.LateralQueries + " bounded queries (" + probe.Detail + ")");
                    continue;
                }
                var end = probe.Route == null ? default(Vector3) : probe.Route.Last();
                Check(probe.RouteProven && probe.Lateral && probe.LateralQueries >= 1 && end.z < 0f,
                    "R1-F4: the mount-centred route never admitted; lateral query " + probe.LateralQueries + " reached " + end + " (cost " + probe.RouteMetres.ToString("R") + ", " + probe.Detail + ")");
                var controller = new HarnessController(world.Rider, world.Horse);
                controller.unifiedTurn.GrantMetres = 9f;
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
                var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
                Check(carrier != null && admittedAt.z < 0f && atRelease == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1,
                    "R1-F4: the revalidated lateral route was followed to " + admittedAt + " and one strike was admitted");
            }
        }

        // R1-F9: executor, ordinary mode, first planned weapon and reach agree at prediction, revalidation and dispatch; a changed plan ends the proof.
        private static void R2F9Plan()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f && from.x > 1.0f;
            var turnPoint = new Vector3(2f, 0f, 0f);
            foreach (var attacks in new[] { 1, 2 })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
                world.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, turnPoint, new Vector3(2f, 0f, 1.5f) };
                world.Rider.AttackCount = attacks;
                var predicted = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 9f);
                var controller = new HarnessController(world.Rider, world.Horse) { ClickPlan = predicted.Plan };
                controller.unifiedTurn.GrantMetres = 9f;
                var order = Order(world, MountedCombatActionKind.RiderMelee, false);
                Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
                var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
                Check(predicted.Plan.Valid && !predicted.Plan.SingleAttack && carrier != null && UnitAttack.NativeAttackStarts == 1 &&
                    predicted.Plan.Matches(PairedAttackClickProbe.PlanIdentity.Of(order)) && controller.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal),
                    "R1-F9 " + (attacks == 1 ? "single-capable" : "full-capable") + " ordinary order: " + controller.LastRoutePlans);
            }
            // Changed plan: the ordinary routine's first attack uses another weapon after the click (same reach).
            var w = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.6f, 0f, 1.6f), wall, true);
            w.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, turnPoint, new Vector3(2f, 0f, 1.5f) };
            var atClick = PairedAttackClickProbe.Evaluate(w.Rider, w.Horse, w.Target, 9f);
            w.Rider.FullAttackFirstWeapon = new ItemWeaponFacade();
            var single = PairedAttackClickProbe.Evaluate(w.Rider, w.Horse, w.Target, 9f, null, true);
            var ordinary = PairedAttackClickProbe.Evaluate(w.Rider, w.Horse, w.Target, 9f);
            var changed = new HarnessController(w.Rider, w.Horse) { ClickPlan = atClick.Plan };
            changed.unifiedTurn.GrantMetres = 9f;
            var expired = Order(w, MountedCombatActionKind.RiderMelee, false);
            Vector3 at; MountedPairNativeAdmissionState rel;
            RunRouteBound(w, changed, expired, out at, out rel);
            Check(!single.Plan.Matches(ordinary.Plan) && expired.IsFinished && expired.RouteProofExpiredCount == 1 && expired.DelegatedMove == null &&
                UnitAttack.NativeAttackStarts == 0 && changed.LastRouteRevalidation.StartsWith("proven=False;plan-changed", StringComparison.Ordinal),
                "R1-F9 changed-plan control: the forced-single probe's plan differs from the ordinary one; the order ended cleanly (" + changed.LastRouteRevalidation + ")");
        }


        // R1-F3 (round 3): the 6.5 mm window (1.4467-1.4532 m) holds the smallest supported disc only around its grid point 1.45; through the
        // ACTUAL click admission and the order's carrier revalidation with a grant beyond 1.450 m.
        private static void R3F3SixPointFive()
        {
            Func<Vector3, Vector3, bool> window = (from, to) => !(from.x >= 1.4467f && from.x <= 1.4532f);
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), window, true);
            var c = Connect(world, false);
            var click = ClickTick(c, world.Target);
            var probe = c.Combat.LastAttackClickProbe;
            Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
            RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
            // Round 2 placed centres only in [start + r + 0.2 mm, end - r - 0.2 mm] = [1.450025, 1.449875]: empty.
            Check(click.Admitted && probe != null && probe.RouteMetres >= 1.4467f + 0.003125f - 1e-5f && probe.RouteMetres <= 1.4532f - 0.003125f + 1e-5f && Math.Abs(probe.ArrivalRadius - 0.003125f) < 1e-6f &&
                c.Combat.LastRouteRevalidation.StartsWith("proven=True", StringComparison.Ordinal) && admission == MountedPairNativeAdmissionState.Admitted &&
                UnitAttack.NativeAttackStarts == 1 && world.Horse.Position.x >= 1.4467f && world.Horse.Position.x <= 1.4532f,
                "R1-F3 6.5 mm window: admitted at centre " + probe?.RouteMetres.ToString("R") + " m, radius " + probe?.ArrivalRadius.ToString("R") +
                " (grant " + Field(LastLine(c), "mountGrantedMetres") + " m); revalidated, the horse stopped at " + world.Horse.Position + ", one strike");
        }

        // R2-F2: a complete path ending 5 mm inside a legal area that continues beyond it, lateral paths unavailable; refusal control: the path
        // ends 5 mm before the legal area.
        private static void R3PathEnd()
        {
            foreach (var end in new[] { 1.405f, 1.395f })
            {
                Func<Vector3, Vector3, bool> legal = (from, to) => from.x < 1.4f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), legal, true);
                var target = world.Target.Position;
                var stop = end;
                world.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, new Vector3(stop, 0f, 0f) };
                world.Horse.View.AgentASP.Outcome = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, target) < 1e-4f ? PathCompleteState.Complete : PathCompleteState.Error;
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var probe = c.Combat.LastAttackClickProbe;
                if (end < 1.4f)
                {
                    Check(!click.Admitted && click.NativeIgnoredAfterPostfix && click.Order == null,
                        "R2-F2 control (path ends at " + end + " m, before the legal area): refused (" + click.Decision + "; " + probe?.Detail + ")");
                    continue;
                }
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(click.Admitted && probe != null && probe.RouteProven && probe.RouteMetres <= end + 1e-5f && admission == MountedPairNativeAdmissionState.Admitted &&
                    UnitAttack.NativeAttackStarts == 1,
                    "R2-F2: a complete path ending 5 mm inside the continuing legal area is admitted (destination " + probe?.RouteMetres.ToString("R") + " m, radius " +
                    probe?.ArrivalRadius.ToString("R") + ", " + probe?.Detail + "); released at " + world.Horse.Position + ", one strike [first debit: " + c.Combat.LastFirstTickDecision + "]");
            }
        }

        // R1-F2 (round 3): (a) the numerically stable chord: on 20 m and 200 m segments passing 49.9 mm from a 50 mm disc's centre (a 6.32 mm
        // section inside it), a blind sample inside that section is found; at 50.1 mm nothing is inside. (b) A blind stationary start inside the
        // first proposed disc: the mount's current pose is blind, the first segment's heading would see; that centre is rejected, the horse
        // travels to legal sight, the exact carrier is released and one strike is admitted. Control: a carrier to that first centre completes at
        // once, without moving, with the rider blind.
        private static void R3F2StartPose()
        {
            foreach (var length in new[] { 20f, 200f })
                foreach (var offset in new[] { 0.0499f, 0.0501f })
                {
                    var centre = new Vector3(3.7f, 0f, 0.3f);
                    var from = new Vector3(centre.x - length * 0.5f, 0f, centre.z + offset);
                    var to = new Vector3(centre.x + length * 0.5f, 0f, centre.z + offset);
                    var points = new List<Vector3> { from, to };
                    // blind wherever the position is more than 0.05 mm inside the disc's 50 mm rim (a 4.5 mm stretch of the 49.9 mm chord)
                    var passes = PairedAttackClickProbe.RouteInsideDiscAdmits(points, length, centre, 0.05f, (p, a, b) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, centre) >= 0.04995f);
                    var expected = offset > 0.05f;
                    Check(passes == expected, "R1-F2 stable chord: a " + length + " m segment " + (offset * 1000f).ToString("0.0") + " mm from the centre: " +
                        (expected ? "no position inside the disc" : "the 6.32 mm section inside the disc is sampled and its blind position found"));
                }
            Func<Vector3, Vector3, bool> wall = (from, to) => from.x < -0.3f;
            var vertex = new Vector3(0.02f, 0f, 0f);
            var back = new Vector3(-1.0f, 0f, 0.3f);
            Func<World> make = () =>
            {
                var w = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0f, -1.5f), wall, true);
                w.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, vertex, back };
                w.Horse.Orientation = 0f;
                w.Rider.Position = w.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                return w;
            };
            var world = make();
            var startStrike = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
            startStrike.Init(world.Rider);
            var startState = startStrike.EvaluateCurrentNativeAdmission();
            var probe = PairedAttackClickProbe.Evaluate(world.Rider, world.Horse, world.Target, 9f);
            var destination = probe.Route == null ? default(Vector3) : probe.Route.Last();
            var startToDestination = Kingmaker.Utility.GeometryUtils.MechanicsDistance(world.Horse.Position, destination);
            var controller = new HarnessController(world.Rider, world.Horse);
            controller.unifiedTurn.GrantMetres = 9f;
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
            var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
            Check(startState == MountedPairNativeAdmissionState.BlockedLineOfSight && probe.RouteProven && startToDestination > probe.ArrivalRadius &&
                carrier != null && carrier.FirstProximityDecision == false && carrier.TravelledMetres > 0f && atRelease == MountedPairNativeAdmissionState.Admitted &&
                order.CarrierReleaseProven && UnitAttack.NativeAttackStarts == 1,
                "R1-F2 blind start pose (" + startState + "): the first centres containing the start were rejected; destination " + destination + " is " +
                startToDestination.ToString("0.###") + " m away (radius " + probe.ArrivalRadius.ToString("R") + "); the horse travelled " + carrier?.TravelledMetres.ToString("0.###") +
                " m, the carrier was released at " + admittedAt + ", one strike");
            // Control: the first proposed centre (the opening's start plus the radius) contains the start; a carrier to it completes at once.
            var control = make();
            var points2 = new List<Vector3> { control.Horse.Position, vertex, back };
            Vector3 f, t; List<Vector3> truncated;
            var first = PairedAttackClickProbe.PointAt(points2, 0.05f, out f, out t, out truncated);
            var old = new UnitMoveTo(first, 0.05f) { NeedLoS = false };
            old.ForcedPath = new Pathfinding.ForcedPath(truncated);
            control.Horse.Commands.Run(old);
            var guard = 0;
            while (!old.IsFinished && guard++ < 10) NativeTick(control, control.Horse);
            var blind = new MountedPairSingleAttack(control.Target, control.Rider, control.Horse, true, false);
            blind.Init(control.Rider);
            Check(old.IsFinished && old.CompletedWithoutMoving && blind.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.BlockedLineOfSight,
                "R1-F2 blind start control: a carrier to the first proposed centre " + first + " completes at once without moving, the rider blind");
        }

        // R1-F4 (round 3): the first native response honors the requested pair radius and the MOUNT's own line of sight, ending where the
        // horse sees the target while the rider (carried at its offset) does not; the bounded lateral search then finds a route. Closed
        // lateral paths keep native's refusal. R2-F3: the nearest lateral candidate is blind under the direct heading and admits under the
        // returned path's final heading; every other path is unavailable.
        private static void R3F4Lateral()
        {
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z > 0.2f;
            foreach (var closed in new[] { false, true })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), wall, true);
                world.Horse.Orientation = 0f;
                world.Rider.Position = world.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                var agent = world.Horse.View.AgentASP;
                agent.HonorCutoff = true;
                var target = world.Target.Position;
                if (closed) agent.Outcome = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, target) < 1e-4f ? PathCompleteState.Complete : PathCompleteState.Error;
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var probe = c.Combat.LastAttackClickProbe;
                var firstEnd = agent.Endpoints.Count > 0 ? agent.Endpoints[0] : default(Vector3);
                var horseSees = !LineOfSightGeometry.Instance.HasObstacle(firstEnd + new Vector3(0f, 1.6f, 0f), target, 7);
                var riderAt = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
                riderAt.Init(world.Rider);
                var riderThere = riderAt.EvaluateNativeAdmissionAtMountPosition(firstEnd, firstEnd - world.Horse.Position);
                if (closed)
                {
                    Check(!click.Admitted && click.NativeIgnoredAfterPostfix && probe != null && probe.LateralQueries >= 1,
                        "R1-F4 closed lateral paths: first endpoint " + firstEnd + " (horse sees=" + horseSees + ", rider " + riderThere + "); native refusal kept (" + probe?.Detail + ")");
                    continue;
                }
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(horseSees && riderThere != MountedPairNativeAdmissionState.Admitted && Math.Abs(firstEnd.x - (4f - probe.PairRadius)) < 0.02f && click.Admitted &&
                    probe.Lateral && probe.RouteProven && admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 && order(click) != null,
                    "R1-F4: the first native route ended at " + firstEnd + " where the horse sees the target and the rider does not (" + riderThere + "); lateral query " +
                    probe.LateralQueries + " gave cost " + probe.RouteMetres.ToString("R") + " m (" + probe.Detail + "); the exact carrier was released at " + world.Horse.Position + ", one strike");
            }
            // R2-F3
            {
                Func<Vector3, Vector3, bool> west = (from, to) => from.x < 1.3f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), west, true);
                world.Horse.Orientation = 0f;
                world.Rider.Position = world.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                var candidate = new Vector3(4f - 2.8f * (float)Math.Cos(Math.PI / 8d), 0f, 2.8f * (float)Math.Sin(Math.PI / 8d));
                var corner = new Vector3(candidate.x, 0f, 1.6f);
                world.Horse.View.AgentASP.Route = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f
                    ? new List<Vector3> { from, corner, to } : new List<Vector3> { from, to };
                world.Horse.View.AgentASP.Outcome = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f ? PathCompleteState.Complete : PathCompleteState.Error;
                var direct = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
                direct.Init(world.Rider);
                var directState = direct.EvaluateNativeAdmissionAtMountPosition(candidate, candidate - world.Horse.Position);
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var probe = c.Combat.LastAttackClickProbe;
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(directState != MountedPairNativeAdmissionState.Admitted && click.Admitted && probe.Lateral && admission == MountedPairNativeAdmissionState.Admitted &&
                    UnitAttack.NativeAttackStarts == 1 && world.Horse.Position.z > 1.0f,
                    "R2-F3: the nearest lateral candidate " + candidate + " is blind under the direct heading (" + directState + ") and admits under the returned path's final heading; " +
                    "affordable approach (cost " + probe.RouteMetres.ToString("R") + " m), released at " + world.Horse.Position + ", one strike");
            }
        }
        private static MountedPairAttackCommand order(ClickOutcome click) => click.Order;

        // R1-F9 (round 3): the click's ACTUAL native prediction is compared with the probe's plan; a prediction whose first weapon, reach or mode
        // differs keeps native's refusal. Identities for single- and full-capable orders. A plan changed after the click while the target came
        // into range before dispatch is refused cleanly on the direct-to-strike path.
        private static void R3F9Prediction()
        {
            foreach (var attacks in new[] { 1, 2 })
            {
                var world = Corner();
                world.Rider.AttackCount = attacks;
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                Check(click.Admitted && line.Contains("plan={") && Field(line, " plan").Length > 0,
                    "R1-F9 " + (attacks == 1 ? "single-capable" : "full-capable") + " order: probe {" + line.Split(new[] { "plan={" }, StringSplitOptions.None)[1].Split('}')[0] +
                    "} = prediction {" + line.Split(new[] { "predicted={" }, StringSplitOptions.None)[1].Split('}')[0] + "}");
            }
            foreach (var spec in new[] { "first weapon", "reach", "mode" })
            {
                var world = Corner();
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target, true, p =>
                {
                    if (spec == "first weapon") p.AllAttacks[0].Weapon = new ItemWeaponFacade();
                    if (spec == "reach") p.AllAttacks[0].WeaponRange = 2.0f;
                    if (spec == "mode") p.IsSingleAttack = true;
                });
                Check(!click.Admitted && click.NativeIgnoredAfterPostfix && click.Decision == "NativeDecision:prediction-plan-mismatch" && click.Order == null,
                    "R1-F9 prediction with another " + spec + ": native refusal kept (" + click.Decision + ")");
            }
            {
                var world = Corner();
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                world.Target.Position = new Vector3(1.5f, 0f, 0f);
                world.Rider.FullAttackFirstWeapon = new ItemWeaponFacade();
                for (var tick = 0; tick < 12 && !click.Order.IsFinished; tick++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                Check(click.Admitted && click.Order.IsFinished && click.Order.RouteProofExpiredCount >= 1 && UnitAttack.NativeAttackStarts == 0 && click.Order.StruckTargets.Count == 0,
                    "R1-F9 in-range dispatch with a changed plan: the target came into range before dispatch and the routine's first weapon changed; the order ended cleanly without a strike (" +
                    click.Order.EndCause + ")");
            }
        }

        // Review R6-F2: a rider carried at a horizontal mount-local offset turns with the mount at a right-angle corner.
        private static void R6F2()
        {
            // Target beyond a corner; sight is blocked for any eye with z < 0.6 east of x = 1.0. The rider sits 0.4 m to the
            // horse's left (horse facing +Z, so at -X). Travelling east the rider is on the north side (+Z); after turning north
            // it is west again. Translating the start offset instead puts the eye west of the wall early on the east leg.
            Func<Vector3, Vector3, bool> wall = (from, to) => from.z < 0.6f && from.x > 1.0f;
            Func<Vector3, Vector3, List<Vector3>> route = (from, to) => new List<Vector3> { from, new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 1.5f) };
            var target = new Vector3(3.6f, 0f, 1.6f);
            var offset = new Vector3(-0.4f, 0f, 0f);
            Func<World> make = () =>
            {
                var w = NewWorld(new Vector3(0f, 0f, 0f), target, wall, true);
                w.Horse.View.AgentASP.Route = route;
                w.Horse.Orientation = 0f;
                w.Rider.Position = w.Horse.Position + offset;
                return w;
            };
            var world = make();
            var controller = new HarnessController(world.Rider, world.Horse);
            controller.unifiedTurn.GrantMetres = 9f;
            var order = Order(world, MountedCombatActionKind.RiderMelee, false);
            Vector3 admittedAt; MountedPairNativeAdmissionState atRelease;
            var carrier = RunRouteBound(world, controller, order, out admittedAt, out atRelease);
            var forced = carrier?.ForcedPath?.vectorPath;
            var predicted = forced == null ? default(Vector3) : forced[forced.Count - 1];
            Check(forced != null && Math.Abs(predicted.x - 2f) < 1e-3f && predicted.z >= 0.6f && predicted.z < 0.7f,
                "R6-F2: the rotated-offset prediction ends inside the opening past the corner at " + predicted);
            var riderOffsetAfter = world.Rider.Position - world.Horse.Position;
            Check(Kingmaker.Utility.GeometryUtils.MechanicsDistance(admittedAt, predicted) < 1e-3f && atRelease == MountedPairNativeAdmissionState.Admitted &&
                UnitAttack.NativeAttackStarts == 1 && Math.Abs(riderOffsetAfter.x + 0.4f) < 1e-3f && Math.Abs(riderOffsetAfter.z) < 1e-3f,
                "R6-F2: after rider synchronisation the strike is admitted exactly at the predicted endpoint (" + admittedAt + ", rider offset " + riderOffsetAfter + ")");
            // Control: the same production walk with the start offset only translated (no heading: travel vector zero) predicts
            // an endpoint on the east leg; carrying the rider there with the mount's turn, the actual admission disagrees.
            var cw = make();
            var probe = new MountedPairSingleAttack(cw.Target, cw.Rider, cw.Horse, true);
            probe.Init(cw.Rider);
            List<Vector3> translatedRoute; float translatedCost;
            var translatedFound = MountedRouteWalker.Walk(route(cw.Horse.Position, target), (a, b) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(a, b),
                Vector3.Lerp, (point, from, to) => probe.EvaluateNativeAdmissionAtMountPosition(point, Vector3.zero) == MountedPairNativeAdmissionState.Admitted,
                9f, out translatedRoute, out translatedCost);
            var translatedEnd = translatedFound ? translatedRoute[translatedRoute.Count - 1] : default(Vector3);
            var follow = new UnitMoveTo(translatedEnd, 0.05f) { NeedLoS = false };
            if (translatedFound) follow.ForcedPath = new Pathfinding.ForcedPath(translatedRoute);
            cw.Horse.Commands.Run(follow);
            var guard = 0;
            while (translatedFound && !follow.IsFinished && guard++ < 60) NativeTick(cw, cw.Horse);
            var actual = probe.EvaluateCurrentNativeAdmission();
            Check(translatedFound && translatedEnd.z < 0.01f && translatedEnd.x < 2f && Kingmaker.Utility.GeometryUtils.MechanicsDistance(cw.Horse.Position, translatedEnd) < 1e-3f &&
                actual != MountedPairNativeAdmissionState.Admitted,
                "R6-F2 control: a translated-offset prediction (" + translatedEnd + ") disagrees with admission after synchronisation (" + actual + ")");
        }

        // ======== PR #70 round 4 (owner instructions 2026-11-27a section 6 and 2026-11-27b section 3) ========
        private static void R4F10Pairing()
        {
            foreach (var mountFirst in new[] { false, true })
            {
                var world = Corner();
                var c = Connect(world, mountFirst);
                var identity = c.Pair.Activation.Identity;
                var grantIdentity = c.Pair.movementState.AllocationGrantIdentity(world.Horse);
                var before = Observe(c);
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                var mountSlotOk = c.MountSlot.Contains("horse:suppressed") && c.MountSlot.Contains("rider:turn") && !c.MountSlot.Contains("horse:turn") &&
                    c.MountSlot.StartsWith(mountFirst ? "roster horse:suppressed -> rider:turn" : "roster rider:turn -> horse:suppressed", StringComparison.Ordinal);
                Check(mountSlotOk && grantIdentity != null && grantIdentity == identity && c.Pair.CanAddressActor(world.Horse, c.Context) && click.Admitted && click.Order != null &&
                    Field(line, "mountStepMode") == "False" && Field(line, "mountSingleMove") == "False" && atRelease != null && atStrike != null &&
                    before.RiderStandard && atRelease.RiderStandard && !atStrike.RiderStandard && atRelease.MountMove > before.MountMove &&
                    admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1,
                    "R1-F10 (round 4) " + c.Arrangement + ": " + c.MountSlot + "; production activation " + identity + " granted the mount's allocation (" + grantIdentity +
                    "); production CanAddressActor and PairedMovementContext selected step=" + Field(line, "mountStepMode") + " single=" + Field(line, "mountSingleMove") +
                    "; " + Spent(before, atRelease, atStrike));
                // The rider's native step choice reaches the mount through the production mirroring and the native limit selection.
                var stepWorld = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(3.5f, 0f, 0f), (from, to) => from.x < 1.45f, true);
                var cs = Connect(stepWorld, mountFirst);
                cs.Context.EnabledFiveFootStep = true;
                var stepClick = ClickTick(cs, stepWorld.Target);
                var stepLine = LastLine(cs);
                Check(stepClick.Admitted && Field(stepLine, "mountStepMode") == "True" && Field(stepLine, "mountSingleMove") == "False" &&
                    Math.Abs(float.Parse(Field(stepLine, "mountGrantedMetres"), System.Globalization.CultureInfo.InvariantCulture) - 1.524f) < 1e-4f,
                    "R1-F10 (round 4) " + cs.Arrangement + " step: the production movement context selected the step (grant " + Field(stepLine, "mountGrantedMetres") + " m)");
            }
            {
                var world = Corner();
                world.Horse.CombatState.Cooldown.MoveAction = 1.0f;   // the horse's own native turn this round already moved it part way
                var c = ConnectAdoptedAfterHorseActed(world);
                var click = ClickTick(c, world.Target);
                var line = LastLine(c);
                var carriers = world.Horse.Commands.Raw.Count(command => command is UnitMoveTo);
                Check(c.MountSlot.Contains("adopted=True") && c.MountSlot.Contains("partnerEnded=True") && c.MountSlot.Contains("partnerContext=False") &&
                    !c.Pair.CanAddressActor(world.Horse, c.Context) && !click.Admitted && click.Order == null && carriers == 0 && Field(line, "mountGrant") == "mount-not-addressable" &&
                    c.Combat.StockAttackIntentStartCount == 0 && world.Horse.CombatState.Cooldown.MoveAction == 1.0f,
                    "R1-F10 (round 4) adoption after the horse acted: " + c.MountSlot + "; production CanAddressActor refuses the ended horse (" + click.Decision + "; mountGrant=" +
                    Field(line, "mountGrant") + "); zero replacement carriers, no order, no expenditure");
            }
        }

        // ===================== PR-2 R1-F6 / R5-F3 / R6-F1..F7 (round 7): the connected provisional-order lifecycle =====================
        // Fixture: restricted mount at 9 m/s, untouched Move, unused Standard, the rider's single-move ground choice selected and mirrored by
        // production PairedMovementContext (nothing rewrites the limit); sight opens at Opening[0] along +x (target at x = 2). The pair is prepared
        // through production preparation in either initiative arrangement; its turn is left PREPARING (fresh) or reaches Acting through the
        // installed Tick transition (a rider command; ActNatively), never by assignment. Every frame (Frame) runs the installed per-frame order:
        // the turn's Tick (its Preparing branch), UnitMoveController (the mount's carrier, if native command eligibility admits it: TickMove ->
        // the ACTUAL TryOverrideMountTurnMovement -> TickMountMovement -> the ACTUAL TickDelegated and its gate), UnitActionController (each
        // unit's commands after the ACTUAL NativeActorEligibleForCommand; rider-first or horse-first), then the controller's per-frame drive
        // (TryDriveStockAttackIntent -> SweepFirstTickProposal). No case calls the deciding tick or sets the turn status. Native Run's Hold write
        // goes through the ACTUAL NativeRunKeepsHold and the click's post-Run assignment through the ACTUAL ClickManualTargetAssignment.
        private static Connected FirstTickPair(float opening, float tick, bool mountFirst = false, bool restricted = true, bool step = false,
            bool standardSpent = false, bool acting = false, bool sightOnlyToTarget = false)
        {
            var sight = new[] { opening };
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.0f, 0f, 0f), (from, to) => (!sightOnlyToTarget || to.x > 1.9f) && from.x < sight[0], true);
            world.Horse.CombatSpeedMps = 9f;
            world.Horse.MoveRestricted = restricted;
            if (standardSpent) world.Horse.CombatState.Cooldown.StandardAction = 6f;
            var c = Connect(world, mountFirst, true, true);
            c.Opening = sight;
            if (step) c.Context.EnabledFiveFootStep = true; else c.Context.EnabledSingleActionMove = true;
            c.Pair.PairedMovementContext(c.Context);   // the per-frame production mirroring of the rider's ground choice
            world.TickSeconds = tick;
            Kingmaker.Game.Instance.TimeController.DeltaTime = tick;   // round 11 (R10-F2): the click frame's TimeController.DeltaTime
            if (acting) ActNatively(c);
            return c;
        }

        // The installed Preparing -> Acting transition (TurnController.Tick: the turn unit's command container is not empty): the rider runs and
        // finishes a Free player command; the turn's own Tick then reads Acting. No action budget is touched.
        private static void ActNatively(Connected c)
        {
            var free = new HarnessPlayerCommand(null, UnitCommand.CommandType.Free);
            c.World.Rider.Commands.Run(free);
            c.Context.NativeTick();
            free.FinishSuccess();
            c.World.Rider.Commands.RemoveFinishedAndUpdateQueue();
            if (!c.Context.IsActing) throw new InvalidOperationException("The installed Tick transition did not reach Acting.");
        }

        private static UnitCommand LiveMountCarrier(Connected c)
        {
            var command = c.World.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
            return command != null && !command.IsFinished ? command : null;
        }

        private static string Id(object o) => o == null ? "none" : o.GetType().Name + "@" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o).ToString("x");

        private static bool Eligible(Connected c, UnitEntityData unit, UnitCommand command) => unit.IsCurrentUnit() || c.Pair.NativeActorEligibleForCommand(unit, command);

        // One engine frame in the installed order (see the fixture note).
        private static void Frame(Connected c)
        {
            var world = c.World;
            UnityEngine.Time.frameCount++;
            Kingmaker.Game.Instance.TimeController.GameTime += TimeSpan.FromSeconds(0.1);
            Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn?.NativeTick();
            var beforeMountTick = c.BeforeMountTick; c.BeforeMountTick = null; beforeMountTick?.Invoke();
            var carrier = world.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
            if (carrier != null && !carrier.IsFinished)
            {
                if (Eligible(c, world.Horse, carrier)) { c.CarrierTicks++; AgentMove(c, carrier); }
                else c.IneligibleCarrierFrames++;
            }
            world.Horse.Commands.RemoveFinishedAndUpdateQueue();
            var stage = c.ActionStage; c.ActionStage = null; stage?.Invoke();
            foreach (var unit in c.HorseFirst ? new[] { world.Horse, world.Rider } : new[] { world.Rider, world.Horse }) ActionTick(c, unit);
            Harness.NativeActorCapability.ReadFaults = null; MountedCombatController.FaultInTransfer = false; UnitEntityData.HoldWriteFault = null;
            Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null; Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = null;
            MountedCombatController.FaultBeforeRetirement = false; MountedCombatController.FaultAfterEffects = false; MountedCombatController.FaultInRetirement = false;
            c.BeforeDrive?.Invoke();
            c.Combat.Drive();
        }

        // UnitMoveController for the mount's carrier: the engine stand-in of installed UnitMovementAgent.TickMovement (round 10, R8-F6), steered by
        // the AGENT's own path, cursor, MoveDirection and speed and run with the delta the production movement hook RETURNS (never with the
        // product's prediction): waypoint skip while (follow-precomputed, not last) distance < m_Speed x delta; combat steering
        // RotateTowards(m, Lerp(m, d, 0.5), combatAngular x delta) with the last-segment angular raise; shift = new direction x MaxSpeed x delta
        // (TB combat: no SlowDown, MinSpeed 1); after the move a waypoint passed (or within 0.05 m of the end on the last segment) advances the
        // cursor or, on the last segment, puts the agent on the path's end and completes the movement.
        // Round 19 (R18-F2): installed UnitMoveController ticks the agent whenever it is really moving (IsReallyMoving 060018C0: a delivered Path
        // with the cursor past its first waypoint) and CanMove, whatever command's request that path answered: the hook is reached with a
        // predecessor's path still steering. PathOwner (whose request the path answered) is only observed here; the harness's own carrier paths
        // keep their existing cursor convention, and the carrier is finished by its own path only.
        private static void AgentMove(Connected c, UnitMoveTo carrier)
        {
            var w = c.World; var horse = w.Horse; var agent = horse.View.AgentASP;
            var own = ReferenceEquals(agent.PathOwner, carrier);
            if (agent.Path == null || !own && agent.NextPointIndex <= 0) { c.NotSteeringFrames++; return; }
            if (own && carrier.IsUnitEnoughClose) { carrier.FinishSuccess(); return; }
            var points = agent.Path.vectorPath;
            AdvanceCursor(agent, horse.Position);
            if (agent.NextPointIndex >= points.Count) { if (own) carrier.FinishSuccess(); else { agent.Path = null; agent.PathOwner = null; } return; }
            // Round 18 (R17-F1): ZeroMountTicks makes the next carrier ticks zero-duration frames (the hook then returns zero: native's denial).
            var requested = w.ZeroMountTicks > 0 ? 0f : w.TickSeconds > 0f ? w.TickSeconds : 0.1f;
            if (w.ZeroMountTicks > 0) w.ZeroMountTicks--;
            var delta = w.MountMoveDelta(requested);
            w.LastRequestedMetres = (horse.CurrentSpeedMps * agent.SpeedFactor) * requested;
            if (!(delta > 0f))
            {
                w.MountStalls++;
                if (w.NativeDenialCompletesMovement) NativeTurnBasedDenial(c);   // installed CanMoveInTurnBased denial branch (R13-F4)
                return;
            }
            SteerAgent(c, carrier, delta);
        }

        // Round 19 (R17-F1): installed UnitMovementAgent.CanMoveInTurnBased (060018A9) once the paired prefix returned to native (the ACTUAL
        // TryOverrideMountTurnMovement returned false: the guard route, a refused tick, or a zero tick): for the mount, which is not CurrentTurn,
        // its movement flag is false; outside force mode and cutscene control, a Move slot that is empty or whose command lacks
        // DontInterruptInTurnBasedMode completes the movement (CompleteMovement -> Stop or InterruptMovement -> Stop: Path and RequestedPath
        // cleared, m_Destination cleared); otherwise m_NoMoreTurnBasedMovementForUinterruptableCommand. No movement either way. PathOwner stays a
        // test-only observation (unchanged, as before).
        private static void NativeTurnBasedDenial(Connected c)
        {
            var agent = c.World.Horse.View.AgentASP;
            if (agent.ForceModeForHarness) return;
            var move = c.World.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
            if (move != null && move.DontInterruptInTurnBasedMode) return;
            agent.Path = null; agent.RequestedPath = null; c.World.RequestedDestination = null;
            c.World.NativeDenials++;
        }

        // Round 19 (R17-F1, R18-F2): installed UnitMovementAgent.PathTo (060018A3) wrapped by the ACTUAL production hooks as Harmony runs them
        // (prefix, body, postfix with the prefix's __state). Body: an outstanding request toward the same destination is kept (IL_0087: no new
        // request); otherwise A* hands out a Path object (the fixture's pooled object when it is not the agent's claimed Path: set_Path and
        // set_RequestedPath claim) as m_RequestedPath for the exact command PathTo carries. Asynchronous A* completion is the later
        // NativeOnPathComplete call.
        private static Pathfinding.Path NativePathTo(World w, UnitEntityData unit, UnitMoveTo command, Vector3 destination, List<Vector3> found)
        {
            var agent = unit.View.AgentASP;
            Pathfinding.Path state;
            PatchMethods.CarrierPathRequestPrefix(agent, out state);
            if (!(agent.RequestedPath != null && w.RequestedDestination.HasValue && w.RequestedDestination.Value.Equals(destination)))
            {
                var requested = w.ReusePath != null && !ReferenceEquals(agent.Path, w.ReusePath) ? w.ReusePath : new Pathfinding.Path();
                requested.vectorPath = new List<Vector3>(found);
                agent.RequestedPath = requested; w.RequestedFor = command; w.RequestedDestination = destination;
            }
            PatchMethods.CarrierPathRequestPostfix(agent, command, state);
            return agent.RequestedPath;
        }

        // Round 19 (R17-F1, R18-F2): installed UnitMovementAgent.OnPathComplete (060018B9) wrapped by the ACTUAL production hooks. Body: a
        // completion that is not m_RequestedPath is ignored; otherwise m_RequestedPath is cleared and, unless the path failed (error: OnPathNotFound,
        // Path unchanged), p becomes the agent's Path with its cursor at the first waypoint ahead (SetWaypoint(1)). PathOwner records, for
        // observation only, whose request the delivered path answered.
        private static void NativeOnPathComplete(UnitEntityData unit, Pathfinding.Path p, object requestedBy)
        {
            var agent = unit.View.AgentASP;
            int state;
            PatchMethods.CarrierPathCompletePrefix(agent, p, out state);
            if (ReferenceEquals(p, agent.RequestedPath))
            {
                agent.RequestedPath = null;
                if (!p.error) { agent.Path = p; agent.PathOwner = requestedBy; agent.NextPointIndex = 0; AdvanceCursor(agent, unit.Position); }
            }
            PatchMethods.CarrierPathCompletePostfix(agent, p, state);
        }

        // The installed TickMovement step itself for the agent's own path with a returned delta (shared by the carrier tick and the ground move).
        private static void SteerAgent(Connected c, UnitMoveTo carrier, float delta)
        {
            var w = c.World; var horse = w.Horse; var agent = horse.View.AgentASP;
            var points = agent.Path.vectorPath;
            var position = new UnityEngine.Vector2(horse.Position.x, horse.Position.z);
            Func<int, UnityEngine.Vector2> point = i => new UnityEngine.Vector2(points[i].x, points[i].z);
            Func<UnityEngine.Vector2, UnityEngine.Vector2, float> distance = (p, q) => (float)Math.Sqrt((p.x - q.x) * (p.x - q.x) + (p.y - q.y) * (p.y - q.y));
            var dist = distance(point(agent.NextPointIndex), position);
            while (agent.FollowPrecomputedPath && agent.NextPointIndex < points.Count - 1 && dist < agent.CurrentSpeedField * delta)
            { agent.NextPointIndex++; dist = distance(point(agent.NextPointIndex), position); }
            var waypoint = point(agent.NextPointIndex);
            var onLast = agent.NextPointIndex >= points.Count - 1;
            var speed = agent.MaxSpeed;
            var m = agent.MoveDirection;
            var d = new UnityEngine.Vector2((waypoint.x - position.x) / Math.Max(dist, 0.0001f), (waypoint.y - position.y) / Math.Max(dist, 0.0001f));
            var target = new UnityEngine.Vector2((m.x + d.x) * 0.5f, (m.y + d.y) * 0.5f);
            var angular = agent.CombatAngularSpeed;
            Func<UnityEngine.Vector2, UnityEngine.Vector2, double> angle = (u, v) =>
            {
                var den = Math.Sqrt((u.x * u.x + u.y * u.y) * (double)(v.x * v.x + v.y * v.y));
                return den < 1e-15 ? 0d : Math.Acos(Math.Max(-1d, Math.Min(1d, (u.x * v.x + u.y * v.y) / den))) * 180d / Math.PI;
            };
            if (onLast)
            {
                var turnTime = angle(m, target) / angular; var arrival = dist / Math.Max(speed, 0.01f);
                if (turnTime * 2d > arrival) angular = (float)(angular * 2d * turnTime / arrival);
            }
            // Unity RotateTowards in the plane: rotate by at most angular x delta, move the magnitude by at most 1; MoveTowards for near-zero or
            // parallel vectors.
            var mLen = (float)Math.Sqrt(m.x * m.x + m.y * m.y); var tLen = (float)Math.Sqrt(target.x * target.x + target.y * target.y);
            UnityEngine.Vector2 next;
            var cosine = mLen > 1e-5f && tLen > 1e-5f ? (m.x * target.x + m.y * target.y) / (mLen * tLen) : 1f;
            if (mLen > 1e-5f && tLen > 1e-5f && cosine <= 1f - 1e-6f)
            {
                var full = Math.Acos(Math.Max(-1f, Math.Min(1f, cosine)));
                var turn = Math.Min(angular * delta * Math.PI / 180d, full) * (m.x * target.y - m.y * target.x >= 0f ? 1d : -1d);
                var mag = mLen + Math.Max(-1f, Math.Min(1f, tLen - mLen));
                next = new UnityEngine.Vector2((float)((m.x / mLen * Math.Cos(turn) - m.y / mLen * Math.Sin(turn)) * mag), (float)((m.x / mLen * Math.Sin(turn) + m.y / mLen * Math.Cos(turn)) * mag));
            }
            else
            {
                var gap = distance(target, m);
                next = gap <= 1f || gap < 1e-12f ? target : new UnityEngine.Vector2(m.x + (target.x - m.x) / gap, m.y + (target.y - m.y) / gap);
            }
            var shift = new Vector3(next.x * speed * delta, 0f, next.y * speed * delta);
            horse.Position = horse.Position + shift; w.Rider.Position = w.Rider.Position + shift;
            agent.MoveDirection = next; agent.CurrentSpeedField = speed;
            carrier.Travelled.Add(horse.Position); carrier.TravelledMetres += (float)Math.Sqrt(shift.x * shift.x + shift.z * shift.z);
            var now = new UnityEngine.Vector2(horse.Position.x, horse.Position.z);
            var previous = agent.NextPointIndex > 0 ? point(agent.NextPointIndex - 1) : position;
            var passed = dist <= 0.0001f || (waypoint.x - previous.x) * (waypoint.x - now.x) + (waypoint.y - previous.y) * (waypoint.y - now.y) < 0f;
            if (onLast && (passed || distance(now, waypoint) < 0.05f))
            {
                var end = points[points.Count - 1];
                var snap = new Vector3(end.x - horse.Position.x, 0f, end.z - horse.Position.z);
                horse.Position = horse.Position + snap; w.Rider.Position = w.Rider.Position + snap;
                agent.NextPointIndex = points.Count;
                if (ReferenceEquals(agent.PathOwner, carrier)) carrier.FinishSuccess(); else { agent.Path = null; agent.PathOwner = null; }   // round 19: a predecessor's path ends (Stop)
            }
            else if (passed) { agent.NextPointIndex++; if (agent.NextPointIndex < points.Count) agent.NextWaypoint = point(agent.NextPointIndex); }
        }

        // Native TickApproaching of a move: the agent takes its own copy of the move's path once (the cursor starts past the points it stands on).
        // Round 20 (R19-F5): a top-level TickApproaching whose native body throws after the ACTUAL prefix entered its scope (Harmony runs no postfix).
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference ThrowingApproachScope()
        {
            var command = new UnitMoveTo(new Vector3(9f, 0f, 9f), 0.1f);
            PatchMethods.MountedAttackApproachPrefix(command);
            try { throw new InvalidOperationException("simulated TickApproaching failure"); }
            catch (InvalidOperationException) { }
            return new WeakReference(command);
        }

        // The production ModDisable sequence with every step settled (R19-F5 teardown evidence).
        private sealed class ReadyModDisableSteps : MountedServiceTeardownSequence.IModDisableSteps
        {
            public bool TeardownOwnersReady => true;
            public bool AdmissionFenced => false;
            public bool DrainOutstandingPairCleanup() => true;
            public bool DrainChargeOwnership() => true;
            public bool CleanupPairs() => true;
        }

        // Round 19: installed TickApproaching -> FollowPrecomputedPath (060018B6) sets m_RequestedPath to a ForcedPath copy and calls OnPathComplete
        // synchronously, without PathTo; the ACTUAL completion hooks see it (a delivery no admitted carrier requested).
        private static void TakePath(UnitEntityData unit, UnitMoveTo move)
        {
            var agent = unit.View.AgentASP;
            var forced = new Pathfinding.ForcedPath(new List<Vector3>(move.ForcedPath.vectorPath));
            PatchMethods.CarrierForcedPathRequestPrefix(agent, forced);   // round 19 follow-up: the ACTUAL FollowPrecomputedPath prefix
            agent.RequestedPath = forced;
            NativeOnPathComplete(unit, forced, move);
            agent.PathOwner = move; agent.NextPointIndex = 0;
            AdvanceCursor(agent, unit.Position);
            // Fixture assumption (round 9): the agent faces its first waypoint when it takes the path (cases turn it explicitly to test R8-F6).
            if (agent.NextPointIndex < agent.Path.vectorPath.Count)
            {
                var first = agent.Path.vectorPath[agent.NextPointIndex];
                var fx = first.x - unit.Position.x; var fz = first.z - unit.Position.z; var fl = (float)Math.Sqrt(fx * fx + fz * fz);
                if (fl > 1e-6f) agent.MoveDirection = new UnityEngine.Vector2(fx / fl, fz / fl);
            }
        }

        // Round 12 (R6-F7): the old carrier's path is released (stopped) or replaced by a successor's own path; measured on the agent itself,
        // independently of any command's Result or IsFinished.
        // Round 13 (R6-F7): the agent's path identity and content, owner, cursor and cumulative motion stops, captured without writing anything.
        private sealed class PathSnap
        {
            public object Path, Owner; public string Points; public int Cursor, Stops;
            public override string ToString() => "path " + Id(Path) + " " + Points + ", owner " + Id(Owner) + ", cursor " + Cursor + ", stops " + Stops;
        }
        private static PathSnap Snap(UnitEntityData horse)
        {
            var agent = horse.View.AgentASP;
            var points = new List<string>();
            if (agent.Path != null) foreach (var v in agent.Path.vectorPath) points.Add("(" + v.x.ToString("0.###") + "," + v.z.ToString("0.###") + ")");
            return new PathSnap { Path = agent.Path, Owner = agent.PathOwner, Points = agent.Path == null ? "none" : string.Join(" ", points.ToArray()), Cursor = agent.NextPointIndex, Stops = horse.View.StopMovingCount };
        }
        private static bool SafeClose(UnitCommand command) { try { return command.IsUnitEnoughClose; } catch (Exception) { return false; } }
        private static bool SuccessorPathKept(PathSnap before, PathSnap after, UnitCommand successor) =>
            before != null && after != null && before.Path != null && ReferenceEquals(before.Owner, successor) && ReferenceEquals(after.Path, before.Path) &&
            ReferenceEquals(after.Owner, successor) && after.Points == before.Points && after.Cursor == before.Cursor && after.Stops == before.Stops;

        private static bool OldPathReleased(Connected c, UnitMoveTo oldCarrier) =>
            !(ReferenceEquals(c.World.Horse.View.AgentASP.PathOwner, oldCarrier) && c.World.Horse.View.AgentASP.Path != null);

        // Round 12 (R6-F7): a later lawful ground move of the mount through the same mover: a ground move takes its own path, the ACTUAL
        // TickDelegated returns and debits the delta, and the agent is stepped with the installed rule; displacement is measured on the horse.
        private static float MoverGroundTick(Connected c, float metres, out float debit, out string line)
        {
            var w = c.World; var horse = w.Horse;
            var start = horse.Position;
            var ground = new UnitMoveTo(start + new Vector3(1f, 0f, 0f), 0.01f) { ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { start, start + new Vector3(1f, 0f, 0f) }) };
            TakePath(horse, ground);
            var before = horse.CombatState.Cooldown.MoveAction;
            var dt = metres / horse.CombatSpeedMps;
            line = c.Pair.movementState.TickDelegated(c.Pair.PairedMovementContext(c.Context), horse, ref dt);
            debit = horse.CombatState.Cooldown.MoveAction - before;
            if (dt > 0f) SteerAgent(c, ground, dt);
            return Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, horse.Position);
        }

        private static void AdvanceCursor(Kingmaker.View.UnitMovementAgent agent, Vector3 position)
        {
            var points = agent.Path.vectorPath;
            while (agent.NextPointIndex < points.Count && Kingmaker.Utility.GeometryUtils.MechanicsDistance(points[agent.NextPointIndex], position) <= 0.001f) agent.NextPointIndex++;
            if (agent.NextPointIndex < points.Count) agent.NextWaypoint = new UnityEngine.Vector2(points[agent.NextPointIndex].x, points[agent.NextPointIndex].z);
        }

        // UnitActionController for one unit: eligibility first; a carrier's own command tick completes a path it has finished; an unstarted
        // command approaches (the ACTUAL approach prefix) and starts when ready and close; a started one ticks (its strike acts when started).
        private static void ActionTick(Connected c, UnitEntityData unit)
        {
            foreach (var command in unit.Commands.Raw.ToArray())
            {
                if (command == null || command.IsFinished) continue;
                if (!Eligible(c, unit, command)) continue;
                var move = command as UnitMoveTo;
                if (move != null)
                {
                    // Native TickApproaching of the carrier: its approach gives the agent its own copy of the path once (the cursor starts past the
                    // points the agent already stands on); a carrier whose agent path is consumed completes.
                    var agent = unit.View.AgentASP;
                    // Round 19 follow-up: native TickApproaching of the carrier, scoped by the ACTUAL prefix/postfix (FollowPrecomputedPath's issuer).
                    if (agent != null && !ReferenceEquals(agent.PathOwner, move) && move.ForcedPath != null)
                    { PatchMethods.MountedAttackApproachPrefix(move); TakePath(unit, move); PatchMethods.MountedAttackApproachPostfix(move); }
                    else if (agent != null && !ReferenceEquals(agent.PathOwner, move) && move.ForcedPath == null && c.World.PathfindUnforcedMoves)
                    {
                        var goal = move.ApproachPoint;
                        var found = agent.Route != null ? agent.Route(unit.Position, goal) : new List<Vector3> { unit.Position, goal };
                        // Round 19: native TickApproaching -> UnitEntityView.MoveTo -> PathTo(this command) and A*'s later OnPathComplete, each
                        // through the ACTUAL production hooks. Pooled fixtures: the earlier move's Path object is released to the pool before A*
                        // hands it back (a claimed Path is never recycled); RequestPathFirst keeps the request in flight until a later approach
                        // tick (DeliverRequests false: indefinitely).
                        if (c.World.ReusePath != null && ReferenceEquals(agent.Path, c.World.ReusePath) && !ReferenceEquals(agent.PathOwner, move)) { agent.Path = null; agent.PathOwner = null; }
                        var requestBefore = agent.RequestedPath;
                        var requested = NativePathTo(c.World, unit, move, goal, found);
                        var issuedNow = requested != null && !ReferenceEquals(requested, requestBefore);
                        if (requested != null && (!c.World.RequestPathFirst || !issuedNow && c.World.DeliverRequests))
                            NativeOnPathComplete(unit, requested, c.World.RequestedFor);
                    }
                    // Round 19: a path the denial cleared (PathOwner is only an observation of whose request it answered) completes nothing.
                    else if (agent != null && ReferenceEquals(agent.PathOwner, move) && agent.Path != null && agent.NextPointIndex >= agent.Path.vectorPath.Count) move.FinishSuccess();
                    continue;
                }
                if (!command.IsStarted)
                {
                    if (PatchMethods.MountedAttackApproachPrefix(command)) { command.IsMoveUnit = true; NativeApproach(c.World, command); }
                    PatchMethods.MountedAttackApproachPostfix(command);   // round 19 follow-up: Harmony's postfix ends the TickApproaching scope
                    if (!command.IsFinished && c.World.Ready[command.Executor] && command.Result == UnitCommand.ResultType.None && command.IsUnitEnoughClose)
                        command.Start();
                }
                else
                {
                    var pair = command as MountedPairAttackCommand;
                    if (pair != null && pair.Transaction.ChildAttackStartCount > 0) pair.ActEventDue = true;
                    command.Tick();
                }
            }
            unit.Commands.RemoveFinishedAndUpdateQueue();
        }

        // The player's own earlier state: hostile A selected by an earlier click's native assignment (the ACTUAL hook) and the horse held.
        private static UnitEntityData PriorSelectionAndHold(Connected c)
        {
            var a = new UnitEntityData("hostile-prior") { Position = new Vector3(-1.5f, 0f, 0.6f) };
            a.View.Corpulence = 0.5f;
            c.Combat.BeginClickObservation(Harness.SelectionManager.Instance.SelectedUnits, false);
            PatchMethods.ClickManualTargetAssignment(c.World.Rider.CombatState, a);
            c.Combat.EndClickObservation(a, true);
            c.World.Horse.HoldState = true;
            return a;
        }

        // R5-F3 (round 8): a previously accepted order A, reached through supported transitions: the player's native-accepted click on A (production
        // acceptance and dispatch of A's wrapper), then native's turn-based force-finish of that unstarted wrapper on the rider's capability branch
        // (the ACTUAL NativeCommandTerminalBranches hooks): zero delivery, so the production SettleOrderTerminal keeps A's generation for the horse's
        // recheck (RiderEndedForHorse) with the rider's slots empty; the rider's condition then clears. The horse is held.
        // PR #70 R11-F8 (round 15): the production custody of this pair: the world's real Domain MountedPairRegistry, the relationship's reserved and
        // committed owner for (rider, horse) with the production retained-commands predicate, and the world roster the spliced PatchBridge reads.
        private static KingmakerMountedCombat.Integration.GameMountedRelationshipService ProductionCustody(Connected c)
        {
            var registry = new MountedPairRegistry<UnitEntityData, KingmakerMountedCombat.Integration.GameMountedRelationshipService>(unit => unit.UniqueId);
            var world = new KingmakerMountedCombat.Integration.MountedWorldService();
            var service = new KingmakerMountedCombat.Integration.GameMountedRelationshipService(registry) { World = world, CombatOwner = c.Combat };
            world.PublishForHarness(new KingmakerMountedCombat.Integration.MountedPairServices { Relationship = service, Combat = c.Combat });
            service.MountForHarness(c.World.Rider, c.World.Horse, () => c.Combat.HasRetainedPairCommands);
            KingmakerMountedCombat.Integration.PatchBridge.Service = service;
            return service;
        }

        private static UnitEntityData RetainedOrderAndHold(Connected c)
        {
            var a = new UnitEntityData("hostile-prior") { Position = new Vector3(-1.5f, 0f, 0.6f) };
            a.View.Corpulence = 0.5f;
            ClickTick(c, a, false);
            var wrapperA = c.World.Rider.Commands.Standard as MountedPairAttackCommand;
            if (wrapperA == null) throw new InvalidOperationException("A's order was not dispatched: " + c.Combat.LastStockAttackObservation);
            c.World.Rider.Descriptor.State.CanAct = false;
            var observation = KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.CaptureCanAct(c.World.Rider.Descriptor.State.CanAct);
            KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.TurnBasedForceFinish(wrapperA, UnitCommand.ResultType.Success, observation);
            c.World.Rider.Descriptor.State.CanAct = true;
            c.World.Rider.Commands.RemoveFinishedAndUpdateQueue();
            if (!c.Combat.HasStockAttackIntent || !ReferenceEquals(c.Combat.IntentTarget, a) || !c.Combat.Ledger.RiderEnded(c.Combat.Generation) || !c.World.Rider.Commands.Empty || c.Combat.Active != null)
                throw new InvalidOperationException("The RiderEndedForHorse retention was not reached: " + c.Combat.LastStockAttackObservation);
            c.World.Horse.HoldState = true;
            return a;
        }

        private sealed class Run7
        {
            public ClickOutcome Click; public string Line, Decision = "<none>"; public MountedCombatController.FirstTickProposal Proposal;
            public MountedPairAttackCommand Order; public UnitMoveTo Carrier; public TurnController.TurnStatus StatusAtClick, StatusAfterFrame1;
            public Spend Before, AfterDecision, AtStrike; public float Displacement, Requested; public bool PendingOk, GateBound, SlotEmptyAtStrike, Struck;
            public long GenBefore, GenAfter; public UnitEntityData IntentBefore, IntentAfter, TargetBefore, TargetPending, TargetAfter;
            public bool HoldBefore, HoldPending, HoldAtCarrier, HoldAfter; public int Starts;
            // Round 8: the executed stop after the decision tick, and the prior state at the decision (before that frame's ordinary drive).
            public string StopAfterDecision; public bool Captured, HoldAtDecision; public long GenAtDecision; public UnitEntityData IntentAtDecision, TargetAtDecision;
            public string Facts(Connected c) => "generation " + GenBefore + "->" + GenAfter + ", intent " + Name(IntentBefore) + "->" + Name(IntentAfter) +
                ", manual target " + Name(TargetBefore) + " (pending " + Name(TargetPending) + ") -> " + Name(TargetAfter) + ", horse Hold " + HoldBefore +
                " (pending " + HoldPending + ", at carrier issue " + HoldAtCarrier + ") -> " + HoldAfter + ", turn " + StatusAtClick + "->" + StatusAfterFrame1 +
                ", provisional order " + Id(Order) + ", carrier " + Id(Carrier) + ", decision " + Decision + ", mover step " + Requested.ToString("0.####") +
                " m, displacement " + Displacement.ToString("0.####") + " m, spending before {" + Before + "} after the tick {" + AfterDecision + "} final {" + AtStrike +
                "}, strikes released " + (Order == null ? 0 : Order.StruckTargets.Count) + ", attack starts " + Starts +
                " [order end " + (Order == null ? "-" : Order.IsFinished + "/" + Order.EndCause + "(" + Order.EndCauseDetail + ")/" + Order.Transaction.State) + "; revalidation " +
                c.Combat.LastRouteRevalidation + "; feedback " + c.Combat.LastFeedback + "]";
        }
        private static string Name(UnitEntityData unit) => unit == null ? "none" : unit.UniqueId;

        // Round 12 (R10-F2, R11-F5, R11-F10): a short ordinary click (its ordinary allowance could end within its last two debits at any frame) is
        // Round 13 (owner amendment 2026-11-27g): a short ordinary click is an ACCEPTED order (native-equivalent admission): click, frame 1 (its
        // carrier binds the accepted order's first-tick gate), [afterBinding], frames until its first debit decides, then frames until its strike
        // or end. schedule: the frame duration of each carrier tick from the deciding one on (the installed mover runs each with its own delta);
        // beforeTick runs before each of those frames. Ticks records every executed tick: frame, returned and debited duration, stop.
        private sealed class ShortRun
        {
            public bool Pending, Bound; public string ClickLine = "", Decision = "<none>", Approved = ""; public Vector3 Start, Stop; public UnityEngine.Vector2 Direction;
            public UnitEntityData TargetAtDecision, TargetAtClick; public bool HoldAtDecision; public long GenBefore, GenAtClick, GenAtDecision; public float MoveBefore, MoveAfterDecision, MoveFinal;
            public float RiderStandardBefore, RiderStandardFinal; public bool Monotonic = true;
            public MountedPairAttackCommand Order; public readonly List<string> Stops = new List<string>(); public readonly List<string> Ticks = new List<string>(); public int Strikes;
        }
        private static ShortRun ShortOrdinaryRun(Connected c, UnitEntityData prior, Action afterBinding, int frames = 14, float[] schedule = null, Action<int> beforeTick = null)
        {
            var w = c.World; var r = new ShortRun { MoveBefore = w.Horse.CombatState.Cooldown.MoveAction, GenBefore = c.Combat.Generation, RiderStandardBefore = w.Rider.CombatState.Cooldown.StandardAction };
            var click = ClickTick(c, w.Target);
            r.ClickLine = LastLine(c);
            r.Order = w.Rider.Commands.Standard as MountedPairAttackCommand;
            r.GenAtClick = c.Combat.Generation; r.TargetAtClick = w.Rider.CombatState.ManualTarget;
            Frame(c);
            var p = c.Combat.FirstTickProposalForDiagnostics;
            r.Pending = click.Admitted && r.Order != null && !r.Order.ProvisionalOrder && r.GenAtClick != r.GenBefore && ReferenceEquals(r.TargetAtClick, w.Target);
            r.Bound = p != null && !p.Replacement && p.CarrierMove != null && ReferenceEquals(c.Pair.movementState.FirstTickGate, p);
            afterBinding?.Invoke();
            r.Start = w.Horse.Position;
            c.BeforeDrive = () =>
            {
                var d = c.Combat.LastFirstTickDecision;
                if (r.Decision != "<none>" || d.StartsWith("pending")) return;
                r.Decision = d; r.Stop = w.Horse.Position; r.Direction = w.Horse.View.AgentASP.MoveDirection; r.Approved = w.LastMountTick ?? "";
                r.TargetAtDecision = w.Rider.CombatState.ManualTarget; r.HoldAtDecision = w.Horse.HoldState; r.GenAtDecision = c.Combat.Generation;
            };
            var tick = 0; var lastPos = w.Horse.Position; var lastMove = w.Horse.CombatState.Cooldown.MoveAction;
            Action step = () =>
            {
                var frame = w.TickSeconds;
                if (schedule != null && tick < schedule.Length) { frame = schedule[tick]; w.TickSeconds = frame; Kingmaker.Game.Instance.TimeController.DeltaTime = frame; }
                beforeTick?.Invoke(tick);
                Frame(c);
                tick++;
                var move = w.Horse.CombatState.Cooldown.MoveAction;
                if (move < lastMove - 1e-7f) r.Monotonic = false;
                if (move > lastMove + 1e-7f || Kingmaker.Utility.GeometryUtils.MechanicsDistance(lastPos, w.Horse.Position) > 1e-6f)
                    r.Ticks.Add("frame " + frame.ToString("0.#####") + " s: returned/debited " + (move - lastMove).ToString("0.#####") + " s, stop (" + w.Horse.Position.x.ToString("0.#####") + "," + w.Horse.Position.z.ToString("0.#####") + ")");
                lastMove = move; lastPos = w.Horse.Position;
            };
            for (var i = 0; i < 3 && r.Decision == "<none>"; i++) step();
            c.BeforeDrive = null;
            r.MoveAfterDecision = w.Horse.CombatState.Cooldown.MoveAction;
            var last = w.Horse.Position;
            for (var i = 0; i < frames && r.Order != null && !r.Order.IsFinished && r.Order.StruckTargets.Count == 0; i++)
            {
                step();
                if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(last, w.Horse.Position) > 1e-6f) { last = w.Horse.Position; r.Stops.Add("(" + last.x.ToString("0.####") + "," + last.z.ToString("0.####") + ")"); }
            }
            r.Strikes = r.Order == null ? 0 : r.Order.StruckTargets.Count;
            r.MoveFinal = w.Horse.CombatState.Cooldown.MoveAction;
            r.RiderStandardFinal = w.Rider.CombatState.Cooldown.StandardAction;
            return r;
        }

        // Click, frame 1 (the turn's Tick, the provisional order starts and issues its carrier), [beforeDecision], frame 2 (the carrier's first
        // movement tick), then frames until the rider's strike is released or its order ends.
        private static Run7 RunProvisional(Connected c, UnitEntityData target, Action beforeFrame1 = null, Action beforeDecision = null, int frames = 14)
        {
            var w = c.World;
            var r = new Run7 { GenBefore = c.Combat.Generation, IntentBefore = c.Combat.IntentTarget, TargetBefore = w.Rider.CombatState.ManualTarget, HoldBefore = w.Horse.HoldState };
            r.Before = Observe(c);
            r.StatusAtClick = c.Context.Status;
            var startsBefore = UnitAttack.NativeAttackStarts;
            r.Click = ClickTick(c, target);
            r.Line = LastLine(c);
            r.Proposal = c.Combat.FirstTickProposalForDiagnostics;
            r.Order = w.Rider.Commands.Standard as MountedPairAttackCommand;
            r.TargetPending = w.Rider.CombatState.ManualTarget; r.HoldPending = w.Horse.HoldState;
            r.PendingOk = r.Click.Admitted && r.Proposal != null && r.Proposal.Replacement && r.Proposal.State == MountedFirstTickProposalState.Pending &&
                r.Order != null && ReferenceEquals(r.Proposal.Command, r.Order) && ReferenceEquals(c.Combat.Active, r.Order) && !r.Order.IsStarted &&
                LiveMountCarrier(c) == null && c.Combat.Generation == r.GenBefore && ReferenceEquals(c.Combat.IntentTarget, r.IntentBefore) &&
                ReferenceEquals(r.TargetPending, r.TargetBefore) && r.HoldPending == r.HoldBefore && c.Pair.movementState.FirstTickGate == null &&
                Field(r.Line, "mountGrant") == "single-move-first-tick";
            beforeFrame1?.Invoke();
            Frame(c);
            r.StatusAfterFrame1 = c.Context.Status;
            r.Carrier = (r.Proposal?.CarrierMove ?? LiveMountCarrier(c)) as UnitMoveTo;
            r.HoldAtCarrier = w.Horse.HoldState;
            r.GateBound = r.Proposal != null && r.Carrier != null && ReferenceEquals(LiveMountCarrier(c), r.Carrier) && ReferenceEquals(c.Pair.movementState.FirstTickGate, r.Proposal) &&
                ReferenceEquals(r.Order?.DelegatedMove, r.Carrier);
            beforeDecision?.Invoke();
            var decisionStart = w.Horse.Position;
            var previousHook = c.BeforeDrive;
            c.BeforeDrive = () =>
            {
                previousHook?.Invoke();
                if (r.Captured || c.Combat.LastFirstTickDecision.StartsWith("pending")) return;
                r.Captured = true; r.GenAtDecision = c.Combat.Generation; r.IntentAtDecision = c.Combat.IntentTarget; r.TargetAtDecision = w.Rider.CombatState.ManualTarget; r.HoldAtDecision = w.Horse.HoldState;
                r.StopAfterDecision = w.Horse.Position.ToString();
                c.HorseAtDecision = w.Horse.Position; c.DirectionAtDecision = w.Horse.View.AgentASP.MoveDirection;
            };
            // The carrier's first movement tick: the frame after the agent took its path (horse-first processing takes it one frame later).
            for (var k = 0; k < 3 && r.Carrier != null && !r.Captured; k++) Frame(c);
            c.BeforeDrive = previousHook;
            if (!r.Captured) { r.Captured = true; r.GenAtDecision = c.Combat.Generation; r.IntentAtDecision = c.Combat.IntentTarget; r.TargetAtDecision = w.Rider.CombatState.ManualTarget; r.HoldAtDecision = w.Horse.HoldState; r.StopAfterDecision = w.Horse.Position.ToString(); }
            MountedCombatController.FaultBeforeRetirement = false; MountedCombatController.FaultAfterEffects = false;
            r.Decision = c.Combat.LastFirstTickDecision;
            r.Requested = w.LastRequestedMetres;
            r.Displacement = Kingmaker.Utility.GeometryUtils.MechanicsDistance(decisionStart, w.Horse.Position);
            r.AfterDecision = Observe(c);
            for (var i = 0; i < frames && r.Order != null && !r.Order.IsFinished && r.Order.StruckTargets.Count == 0; i++)
            {
                Frame(c);
                if (!r.Struck && r.Order.Transaction.ChildAttackStartCount > 0 && !r.SlotEmptyAtStrike)
                    r.SlotEmptyAtStrike = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && (r.Carrier == null || r.Carrier.IsFinished);
                r.Struck = r.Order.StruckTargets.Count > 0;
            }
            r.Starts = UnitAttack.NativeAttackStarts - startsBefore;
            r.AtStrike = Observe(c);
            r.GenAfter = c.Combat.Generation; r.IntentAfter = c.Combat.IntentTarget; r.TargetAfter = w.Rider.CombatState.ManualTarget; r.HoldAfter = w.Horse.HoldState;
            return r;
        }

        private static bool Committed(Connected c, Run7 r, UnitEntityData target, float tick, bool held, bool inPlace = false) =>
            r.PendingOk && (inPlace || r.GateBound) && r.StatusAfterFrame1 == TurnController.TurnStatus.Acting && r.Decision.StartsWith("committed:replacement:") &&
            r.GenAfter > r.GenBefore && r.Proposal.CommittedGeneration == r.GenAfter && ReferenceEquals(r.IntentAfter, target) && ReferenceEquals(r.TargetAfter, target) &&
            (r.Carrier == null || c.Combat.Ledger.IsRetainedOwn(r.Carrier)) && c.Combat.Ledger.IsRetainedOwn(r.Order) &&
            (!held || !r.HoldAfter) && r.Order.StruckTargets.Count == 1 && r.Starts == 1 && r.SlotEmptyAtStrike && !r.AtStrike.RiderStandard && r.Before.RiderStandard &&
            r.AfterDecision.RiderStandard && (inPlace ? Math.Abs(r.AfterDecision.MountMove - r.Before.MountMove) < 1e-6f && r.Displacement < 1e-6f :
            // Round 10: the time charged is the returned delta (granted metres / speed); the executed shift, including native's last-segment
            // arrival onto the path's end, equals the predicted travel and stop.
            Math.Abs(r.AfterDecision.MountMove - r.Before.MountMove - ParseFloat(Field(r.Decision, "grantedMetres")) / 9f) < 1e-4f &&
            Math.Abs(r.Displacement - ParseFloat(Field(r.Decision, "travel"))) < 1e-4f && r.StopAfterDecision == Field(r.Decision, "stop")) &&
            c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null;

        // expectedMove: the mount's Move when a case itself set it after binding (R12-F3 short grant); otherwise the value before the click.
        private static bool RefusedKeptPrior(Connected c, Run7 r, string reason, bool standardChangedByCase = false, float? expectedMove = null) =>
            r.PendingOk && r.Decision.StartsWith("refused:" + reason) && r.Displacement < 1e-6f && r.AfterDecision.MountMove == (expectedMove ?? r.Before.MountMove) &&
            r.AtStrike.MountMove == (expectedMove ?? r.Before.MountMove) && (standardChangedByCase || r.AtStrike.RiderStandard == r.Before.RiderStandard) && r.Starts == 0 &&
            r.GenAtDecision == r.GenBefore && ReferenceEquals(r.IntentAtDecision, r.IntentBefore) && ReferenceEquals(r.TargetAtDecision, r.TargetBefore) && r.HoldAtDecision == r.HoldBefore &&
            r.Order != null && r.Order.IsFinished && (r.Carrier == null || r.Carrier.IsFinished) && LiveMountCarrier(c) == null && c.Combat.Active == null &&
            (c.World.Rider.Commands.Standard == null || c.World.Rider.Commands.Standard.IsFinished && ReferenceEquals(c.World.Rider.Commands.Standard, r.Order)) &&
            c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null;

        private static float ParseFloat(string text) { float value; return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) ? value : float.NaN; }

        // A later legitimate ground tick of the mount on the rider's turn (UnifiedMountedTurnCoordinator.TickMountMovement for the rider-turn ground
        // move): the ACTUAL TickDelegated on the mount's movement context.
        private static float GroundTick(Connected c, float metres, out float debit, out string line)
        {
            var before = c.World.Horse.CombatState.Cooldown.MoveAction;
            var dt = metres / c.World.Horse.CurrentSpeedMps;
            line = c.Pair.movementState.TickDelegated(c.Pair.PairedMovementContext(c.Context), c.World.Horse, ref dt);
            debit = c.World.Horse.CombatState.Cooldown.MoveAction - before;
            return dt * c.World.Horse.CurrentSpeedMps;
        }

        // Round 6's design for the regression: a BARE mount carrier with a gate and no rider command.
        private sealed class RecordingGate : IMountedFirstTickGate
        {
            public object Carrier { get; set; }
            public int Decisions;
            public MountedFirstTickVerdict Decide(object movementContext, float allowedTick, float deltaTime, float speed, MountedSteeringFacts steering, float stepTravelLeft, out float approvedTick, out string detail) { Decisions++; approvedTick = allowedTick; detail = "recorded"; return MountedFirstTickVerdict.Commit; }
        }

        // PR #70 R12-F4 (round 16): disable and unload through the PRODUCTION cleanup chain: MountedServiceTeardownSequence (BeginDisable/Unload,
        // spliced) -> MountedLifecycleSubscriber.HandleModDisable/ModDisableSteps.CleanupPairs/Cleanup/CleanupOwner (spliced) -> the world visit
        // (MountedWorldService.Visit, spliced) -> GameMountedRelationshipService.Dismount (spliced: the Dismounting subscribers, the stick-movement
        // release, the coordinator, the completion policy, ClearPreparedPairWhenUnmounted, CompleteOwnershipAttempt) -> the REAL Domain
        // MountedRelationshipCoordinator -> KingmakerMountedPairRuntime.RestoreMovementAuthority (spliced: the stops, the mount AI lease, the rider
        // override, rotation, stock agent and avoidance leases, rider placement). The Dismounting subscriber is the combat owner's HandleDismounting
        // body (Cancel("relationship cleanup " + trigger)) over the real controller; the teardown readiness is the registry's AllTeardownReady over
        // the production retained-commands predicate. Substituted: the engine (InterruptMove, StopMoving, the agent, Translocate, no A* graph), the
        // mount-time lease acquisition and the presentation attachment, the unified turn's participation end, the retained-intent drop, the Shield
        // release and the Harmony unpatch (RemoveHooksAndServices clears HooksPatched).
        private static void R16ProductionDismountTeardown()
        {
            Action clearFaults = () => Kingmaker.View.UnitEntityView.StopMovingFault = null;
            foreach (var kase in new[] { "steering successor through save/load fences, disable and unload",
                "ground-routed successor (the admitted rider-turn ground order, activeRiderTurnGroundMove) through save/load fences, disable and unload",
                "promoted Standard successor (a newer unowned Standard command native promoted after the exact carrier's removal, approaching; Move slot empty) through save/load fences, disable and unload",
                "promoted Standard successor whose target enters reach after it acquired the path, before its next native action tick, through save/load fences, disable and unload",
                "failed mount stop in the restoration (no successor)",
                "cancellation-created carrier stop (no successor)", "player dismount control (Manual trigger, steering successor)" })
            {
                var groundRouted = kase.StartsWith("ground-routed");
                var promotedStandard = kase.StartsWith("promoted");
                var targetInReach = kase.Contains("enters reach");
                var successorCase = kase.StartsWith("steering") || kase.StartsWith("player") || groundRouted || promotedStandard;
                var cancelling = kase.StartsWith("cancellation");
                var failingRestore = kase.StartsWith("failed");
                var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                PriorSelectionAndHold(c);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var wrapper = click.Order;
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                Frame(c);
                Action armStop = () => Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                // The production custody: the registry, the world roster, the relationship with the REAL coordinator over the spliced runtime.
                var registry = new MountedPairRegistry<UnitEntityData, KingmakerMountedCombat.Integration.GameMountedRelationshipService>(unit => unit.UniqueId);
                var world = new KingmakerMountedCombat.Integration.MountedWorldService();
                var service = new KingmakerMountedCombat.Integration.GameMountedRelationshipService(registry) { World = world, CombatOwner = c.Combat };
                world.PublishForHarness(new KingmakerMountedCombat.Integration.MountedPairServices { Relationship = service, Combat = c.Combat });
                var runtime = new KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime(w.Rider, w.Horse);
                // the production MountedWorldService.BindOwnershipReadiness terms available offline (no paired preparation or shell is unsettled)
                service.MountWithRealCoordinatorForHarness(w.Rider, w.Horse, () => c.Combat.HasRetainedPairCommands, runtime,
                    () => !c.Combat.ChargeAdmissionFenced && !c.Combat.HasOutstandingCleanup);
                c.Combat.RelationshipForHarness.Runtime.Production = runtime;   // the combat owner's CancelMountMovement is the production runtime's
                var engineFailing = cancelling || failingRestore;
                service.Dismounting += trigger =>
                {
                    // MountedCombatController.HandleDismounting (spliced); while the engine is failing its stop keeps failing after the cancellation.
                    try { c.Combat.HandleDismountingForHarness(trigger); }
                    finally { if (engineFailing) armStop(); }
                };
                var lifecycle = new KingmakerMountedCombat.Integration.MountedLifecycleSubscriber(service, c.Combat);
                var steps = new ProductionTeardownSteps(world, service, lifecycle);
                var handled = false; var log = new List<string>(); var ok = true; UnitCommand successor = null; PathSnap before = null;
                Func<string> state = () =>
                {
                    var snap = successor != null ? Snap(w.Horse) : null;
                    return "relationship " + service.State + ", " + runtime.Leases + ", owed " + c.Combat.HasOutstandingCleanup + ", enabled " + steps.Enabled + ", hooks " + steps.HooksPatched +
                        ", drains " + steps.DrainCalls + ", participation ends " + lifecycle.UnifiedTurnEndsForHarness + ", horse InterruptMove " + w.Horse.Commands.InterruptMoveCalls +
                        ", horse stops " + w.Horse.View.StopMovingCount + ", successor preserved " + runtime.PreservedMountSuccessorCount +
                        ", ground move tracked " + Id(c.Combat.ActiveRiderTurnGroundMoveForHarness) + " (released " + c.Combat.ModDisableGroundMoveReleases + "), retained pair commands " + c.Combat.HasRetainedPairCommands + ", registry owners " + registry.Owners.Length + ", horse path owner " + Id(w.Horse.View.AgentASP.PathOwner) +
                        (successor != null ? ", Move slot " + Id(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move)) + ", Standard slot " + Id(w.Horse.Commands.Standard) + " (successor " + Id(successor) + " " + successor.Type +
                            ", finished " + successor.IsFinished + ", interrupted " + (successor.Result == UnitCommand.ResultType.Interrupt) + ", IsMoveUnit " + successor.IsMoveUnit +
                            ", close " + SafeClose(successor) + ", started " + successor.IsStarted + "), {" + snap + "}" : "");
                };
                Func<int, int, bool> successorKept = (interrupts, stops) =>
                    successor != null && !successor.IsFinished && ReferenceEquals(w.Horse.Commands.GetCommand(successor.Type), successor) &&
                    SuccessorPathKept(before, Snap(w.Horse), successor) && w.Horse.Commands.InterruptMoveCalls == interrupts && w.Horse.View.StopMovingCount == stops;
                if (!cancelling) { armStop(); c.ActionStage = () => w.Target.Descriptor.State.IsDead = true; }
                c.BeforeDrive = () =>
                {
                    if (handled || wrapper == null) return;
                    if (cancelling ? (wrapper.IsFinished || LiveMountCarrier(c) == null) : !wrapper.IsFinished) return;
                    handled = true;
                    if (promotedStandard)
                    {
                        // R16-F1 (round 17): queued behind the exact carrier, promoted by native's sweep once that carrier left the Move slot; native
                        // TickApproaching gives the agent its approach path and marks it approaching (IsMoveUnit).
                        var at = w.Horse.Position;
                        var standard = new HarnessApproachingCommand(at + new Vector3(-6f, 0f, 0f));
                        w.Horse.Commands.Enqueue(standard);
                        w.Horse.Commands.RemoveFinishedAndUpdateQueue();
                        var agent = w.Horse.View.AgentASP;
                        agent.Path = new Pathfinding.Path { vectorPath = new List<Vector3> { at, at + new Vector3(-6f, 0f, 0f) } };
                        agent.PathOwner = standard; agent.NextPointIndex = 1;
                        standard.IsMoveUnit = true;
                        successor = standard;
                        before = Snap(w.Horse);
                        ok &= ReferenceEquals(w.Horse.Commands.Standard, standard) && w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && !standard.IsStarted && !standard.IsUnitEnoughClose;
                        // Round 18 (R16-F1): its target moves into reach after the path was acquired and before native's next action tick: proximity
                        // now says close, while native has not started it (IsMoveUnit kept, it still owns the agent's path).
                        if (targetInReach) { standard.Goal = at + new Vector3(-0.3f, 0f, 0f); ok &= standard.IsUnitEnoughClose && standard.IsMoveUnit && !standard.IsStarted; }
                    }
                    else if (successorCase)
                    {
                        var at = w.Horse.Position;
                        successor = new UnitMoveTo(at + new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(-0.5f, 0f, 0f) }) };
                        if (groundRouted) c.Combat.AdmitRiderTurnGroundOrderForHarness();
                        w.Horse.Commands.Run(successor);
                        if (groundRouted) c.Combat.CompleteGroundCommandAdmission(w.Horse);   // the production GroundCommandPostfix step
                        TakePath(w.Horse, (UnitMoveTo)successor);
                        successor.IsMoveUnit = true;   // native TickApproaching marks the approaching move (InterruptMove's selector)
                        before = Snap(w.Horse);
                        if (groundRouted) ok &= ReferenceEquals(c.Combat.ActiveRiderTurnGroundMoveForHarness, successor) && c.Combat.HasRetainedPairCommands;
                    }
                    var interrupts0 = w.Horse.Commands.InterruptMoveCalls; var stops0 = w.Horse.View.StopMovingCount;
                    log.Add("at the request: " + state());
                    MountedServiceTeardownSequence.Verdict v;
                    if (kase.StartsWith("player"))
                    {
                        // The player's ordinary dismount reaches the same relationship cleanup with CleanupTrigger.Manual: it still stops the mount.
                        clearFaults();
                        var result = service.Dismount(CleanupTrigger.Manual);
                        ok &= result.Succeeded && service.State == RelationshipState.Unmounted && successor.IsFinished && w.Horse.Commands.InterruptMoveCalls == interrupts0 + 2 &&
                            w.Horse.View.StopMovingCount > stops0 && runtime.PreservedMountSuccessorCount == 0 && runtime.LeasesReleased;   // (InterruptMove: the cancellation's and the restoration's)
                        log.Add("Manual dismount: " + (result.Succeeded ? "succeeded" : "failed") + "; " + state());
                        return;
                    }
                    if (successorCase)
                    {
                        armStop();   // a tripwire: any StopMoving of the horse view during teardown would throw
                        steps.Save = true;
                        v = steps.SetEnabledFalse();
                        ok &= v == MountedServiceTeardownSequence.Verdict.RefusedSaveOrOwners && steps.DrainCalls == 0 && service.State == RelationshipState.Mounted && steps.HooksPatched && steps.Enabled && successorKept(interrupts0, stops0);
                        log.Add("disable inside the save fence: " + v + "; " + state());
                        steps.Save = false; steps.Load = true;
                        v = steps.Unload();
                        ok &= v == MountedServiceTeardownSequence.Verdict.RefusedLoad && steps.DrainCalls == 0 && lifecycle.UnifiedTurnEndsForHarness == 0 && service.State == RelationshipState.Mounted && steps.HooksPatched && successorKept(interrupts0, stops0);
                        log.Add("unload inside the load fence: " + v + "; " + state());
                        steps.Load = false;
                        v = steps.SetEnabledFalse();
                        ok &= v == MountedServiceTeardownSequence.Verdict.Ready && !steps.Enabled && steps.HooksPatched && service.State == RelationshipState.Unmounted && !c.Combat.HasOutstandingCleanup &&
                            runtime.LeasesReleased && runtime.PreservedMountSuccessorCount == 2 && c.Combat.ActiveRiderTurnGroundMoveForHarness == null && !c.Combat.HasRetainedPairCommands &&
                            c.Combat.ModDisableGroundMoveReleases == (groundRouted ? 1 : 0) && w.Horse.IsAIEnabled && w.Rider.View.AgentASP.enabled && !w.Rider.View.AgentASP.AvoidanceDisabled &&
                            w.Rider.View.AgentOverride == null && !w.Rider.View.ForbidRotation && w.Rider.TranslocateCount == 1 && successorKept(interrupts0, stops0);
                        log.Add("disable: " + v + "; rider placed " + w.Rider.Position + " (mount " + w.Horse.Position + "), horse AI " + w.Horse.IsAIEnabled + ", rider agent enabled " + w.Rider.View.AgentASP.enabled +
                            ", avoidance disabled " + w.Rider.View.AgentASP.AvoidanceDisabled + ", override " + (w.Rider.View.AgentOverride == null ? "none" : "installed") + ", ForbidRotation " + w.Rider.View.ForbidRotation + "; " + state());
                        v = steps.Unload();
                        ok &= v == MountedServiceTeardownSequence.Verdict.Ready && !steps.HooksPatched && service.State == RelationshipState.Unmounted && successorKept(interrupts0, stops0);
                        log.Add("unload: " + v + "; " + state());
                        clearFaults();
                        return;
                    }
                    // Controls without a successor: the restoration's own mount stop fails (or the cancellation's stop failed), so the relationship keeps
                    // its movement residue; the request is refused with hooks and owners retained; the retry after recovery settles.
                    if (failingRestore) clearFaults();
                    else armStop();
                    v = steps.SetEnabledFalse();
                    ok &= v == MountedServiceTeardownSequence.Verdict.RefusedResidue && steps.Enabled && steps.HooksPatched && service.State == RelationshipState.Faulted && world.Pairs.Length == 1 && ReferenceEquals(world.Pairs[0].Relationship, service);
                    log.Add("disable (engine failing): " + v + "; ledger " + string.Join(" / ", lifecycle.ledger.Records.ToArray()) + "; " + state());
                    engineFailing = false; clearFaults();
                    if (cancelling)
                    {
                        // UpdateCore's retry once ordinary updates stopped, then a second disable request
                        steps.Enabled = false;
                        var attempted = MountedServiceTeardownSequence.RetryWhileDisabled(steps);
                        ok &= attempted && steps.HooksPatched && service.State == RelationshipState.Faulted;
                        log.Add("UpdateCore retry: attempted " + attempted + "; " + state());
                        v = steps.Unload();
                    }
                    else v = steps.SetEnabledFalse();
                    ok &= v == MountedServiceTeardownSequence.Verdict.Ready && service.State == RelationshipState.Unmounted && runtime.LeasesReleased && !c.Combat.HasOutstandingCleanup &&
                        w.Horse.View.AgentASP.PathOwner == null && (cancelling ? !steps.HooksPatched : !steps.Enabled && steps.HooksPatched);
                    log.Add((cancelling ? "unload" : "disable") + " (engine recovered): " + v + "; " + state());
                    if (!cancelling)
                    {
                        v = steps.Unload();
                        ok &= v == MountedServiceTeardownSequence.Verdict.Ready && !steps.HooksPatched;
                        log.Add("unload: " + v + "; " + state());
                    }
                };
                for (var i = 0; i < 4 && !handled; i++) Frame(c);
                c.BeforeDrive = null; clearFaults();
                if (successor != null && !successor.IsFinished) { successor.Interrupt(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                ok &= handled;
                Check(ok, "R12-F4 (round 16, production dismount cleanup) " + kase + ": " + string.Join("; ", log.ToArray()));
            }
        }

        // PR #70 R12-F4 / R14-F5 (round 15): the PRODUCTION teardown sequence (MountedServiceTeardownSequence, spliced whole: what
        // CompositionRoot.SetEnabled(false), CompositionRoot.UpdateCore's disabled-state retry, CompositionRoot.Dispose (UMM unload) and
        // MountedLifecycleSubscriber.HandleModDisable delegate to), with the production world drain (MountedWorldService.SettleOutstandingPairCleanup,
        // spliced) over the real controller's TrySettleOutstandingCleanup. The steps (HarnessTeardownSteps) stand for the services the root binds:
        // the persistence fences are flags, TeardownOwnersReady is the pair term the relationship reads (!HasOutstandingCleanup; the other
        // relationship terms are ready here), the lifecycle cleanup's cancellation is the controller's real Cancel, and RemoveHooksAndServices
        // (patches.Dispose and the release after it) only clears the observable HooksPatched flag. Not executed offline: the Harmony unpatch,
        // the UMM callbacks (Main.OnUnload/OnToggle), the relationship's Dismount and the Mounted Shield release.
        private static void R15TeardownLifecycle()
        {
            Action clearFaults = () => { Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null; Kingmaker.View.UnitEntityView.StopMovingFault = null; };
            // 1. A deferred target clear that fails: disable, update recovery, unload during an owned load (R14-F5), unload, recovery, unload.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                c.Combat.Own(w.Horse, a, c.Combat.Generation);
                var steps = new HarnessTeardownSteps(new HarnessWorldService(c.Combat), c.Combat, () => c.Combat.Cancel("mod disable"));
                Action arm = () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState;
                var handled = false; var log = new List<string>(); var ok = true;
                c.BeforeDrive = () =>
                {
                    if (handled || !c.Combat.HasOutstandingCleanup) return;
                    handled = true;
                    arm();
                    var v = steps.SetEnabledFalse();
                    ok &= v == MountedServiceTeardownSequence.Verdict.RefusedSaveOrOwners && steps.Enabled && steps.HooksPatched && c.Combat.HasOutstandingCleanup && ReferenceEquals(w.Horse.CombatState.ManualTarget, a);
                    log.Add("disable (clear failing): " + v + ", enabled " + steps.Enabled + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup + ", horse target " + Name(w.Horse.CombatState.ManualTarget));
                    // An update failure's PerformFailureCleanup stops the ordinary pair update (IsEnabled false); UpdateCore's retry takes over.
                    steps.Enabled = false;
                    arm();
                    var attempted = MountedServiceTeardownSequence.RetryWhileDisabled(steps);
                    ok &= attempted && c.Combat.HasOutstandingCleanup;
                    log.Add("UpdateCore retry (clear failing): attempted " + attempted + ", owed " + c.Combat.HasOutstandingCleanup);
                    // R14-F5: an owned load in flight, the engine healthy again: unload must neither drain nor cancel (the clear would succeed if it did).
                    clearFaults();
                    steps.Load = true;
                    var drainsBefore = steps.DrainCalls; var cleanupsBefore = steps.CleanupCalls;
                    v = steps.Unload();
                    var fenced = MountedServiceTeardownSequence.RetryWhileDisabled(steps);
                    ok &= v == MountedServiceTeardownSequence.Verdict.RefusedLoad && !fenced && steps.DrainCalls == drainsBefore && steps.CleanupCalls == cleanupsBefore &&
                        steps.HooksPatched && c.Combat.HasOutstandingCleanup && ReferenceEquals(w.Horse.CombatState.ManualTarget, a);
                    log.Add("unload during the owned load (engine healthy): " + v + ", drains " + (steps.DrainCalls - drainsBefore) + ", cleanups " + (steps.CleanupCalls - cleanupsBefore) +
                        ", UpdateCore retry inside the load fence " + fenced + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup + ", horse target " + Name(w.Horse.CombatState.ManualTarget));
                    steps.Load = false;
                    arm();
                    v = steps.Unload();
                    ok &= v == MountedServiceTeardownSequence.Verdict.RefusedResidue && steps.HooksPatched && c.Combat.HasOutstandingCleanup && ReferenceEquals(w.Horse.CombatState.ManualTarget, a);
                    log.Add("unload after the load (clear failing): " + v + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup);
                    clearFaults();
                    attempted = MountedServiceTeardownSequence.RetryWhileDisabled(steps);
                    ok &= attempted && !c.Combat.HasOutstandingCleanup && w.Horse.CombatState.ManualTarget == null && steps.HooksPatched;
                    log.Add("UpdateCore retry (engine recovered): attempted " + attempted + ", owed " + c.Combat.HasOutstandingCleanup + ", horse target " + Name(w.Horse.CombatState.ManualTarget));
                    v = steps.Unload();
                    ok &= v == MountedServiceTeardownSequence.Verdict.Ready && !steps.HooksPatched;
                    log.Add("unload: " + v + ", hooks " + steps.HooksPatched);
                    arm();
                };
                var r = RunProvisional(c, w.Target, null, arm);
                c.BeforeDrive = null; clearFaults();
                ok &= handled && r.Decision.StartsWith("committed:replacement:");
                Check(ok, "R12-F4/R14-F5 (round 15, production teardown sequence) deferred target clear: " + r.Decision + "; " + string.Join("; ", log.ToArray()));
            }
            // 2-4. Carrier cleanup: an unfinished carrier whose stop fails; the same with an already-steering successor; and a stop that fails in the
            // cancellation the lifecycle's own cleanup performs (work created by the cancellation itself).
            foreach (var kase in new[] { "unfinished carrier", "steering successor", "cancellation-created carrier stop" })
            {
                var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                PriorSelectionAndHold(c);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var wrapper = click.Order;
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                Frame(c);
                Action armStop = () => Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                var cancelling = kase.StartsWith("cancellation");
                var steps = new HarnessTeardownSteps(new HarnessWorldService(c.Combat), c.Combat, () =>
                {
                    // the lifecycle cleanup's cancellation; in the cancellation case the engine's stop keeps failing through the drain after it
                    try { c.Combat.Cancel("mod disable"); }
                    finally { if (cancelling) armStop(); }
                });
                var handled = false; var log = new List<string>(); var ok = true; UnitMoveTo successor = null; PathSnap before = null, after = null;
                if (!cancelling) { armStop(); c.ActionStage = () => w.Target.Descriptor.State.IsDead = true; }
                c.BeforeDrive = () =>
                {
                    if (handled || wrapper == null) return;
                    if (cancelling ? (wrapper.IsFinished || LiveMountCarrier(c) == null) : !wrapper.IsFinished) return;
                    handled = true;
                    log.Add("at the request: owed " + c.Combat.HasOutstandingCleanup + " (retained carrier records " + c.Combat.RetainedCarrierCleanupCount + "), carrier " + (cancelling ? "moving" : "finished"));
                    if (kase == "unfinished carrier") ok &= c.Combat.HasOutstandingCleanup && c.Combat.RetainedCarrierCleanupCount == 1;
                    if (kase == "steering successor")
                    {
                        var at = w.Horse.Position;
                        successor = new UnitMoveTo(at + new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(-0.5f, 0f, 0f) }) };
                        w.Horse.Commands.Run(successor);
                        TakePath(w.Horse, successor);
                        before = Snap(w.Horse);
                    }
                    if (cancelling) ok &= !c.Combat.HasOutstandingCleanup && c.Combat.RetainedCarrierCleanupCount == 0;
                    armStop();
                    var v = steps.SetEnabledFalse();
                    var expectDisable = kase == "steering successor" ? MountedServiceTeardownSequence.Verdict.Ready
                        : cancelling ? MountedServiceTeardownSequence.Verdict.RefusedResidue : MountedServiceTeardownSequence.Verdict.RefusedSaveOrOwners;
                    ok &= v == expectDisable && steps.HooksPatched && (kase == "steering successor" ? !steps.Enabled : steps.Enabled && c.Combat.HasOutstandingCleanup);
                    log.Add("disable: " + v + " (cleanups run " + steps.CleanupCalls + ", cleanup error " + steps.LastCleanupError + ", carrier " + (LiveMountCarrier(c) == null ? "none" : "live") + ", path owner " + Id(w.Horse.View.AgentASP.PathOwner) + "), enabled " + steps.Enabled + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup +
                        " (retained carrier records " + c.Combat.RetainedCarrierCleanupCount + ")");
                    if (kase != "steering successor")
                    {
                        armStop();
                        v = steps.Unload();
                        ok &= v == MountedServiceTeardownSequence.Verdict.RefusedResidue && steps.HooksPatched && c.Combat.HasOutstandingCleanup;
                        log.Add("unload (stop failing): " + v + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup);
                        // ordinary updates stopped (update failure): UpdateCore's retry, the engine recovered
                        steps.Enabled = false; clearFaults();
                        var attempted = MountedServiceTeardownSequence.RetryWhileDisabled(steps);
                        ok &= attempted && !c.Combat.HasOutstandingCleanup && c.Combat.RetainedCarrierCleanupCount == 0 && steps.HooksPatched;
                        log.Add("UpdateCore retry (engine recovered): attempted " + attempted + ", owed " + c.Combat.HasOutstandingCleanup + ", agent path owner " + Id(w.Horse.View.AgentASP.PathOwner));
                    }
                    armStop();
                    var stopsBefore = w.Horse.View.StopMovingCount;
                    v = steps.Unload();
                    after = successor != null ? Snap(w.Horse) : null;
                    ok &= v == MountedServiceTeardownSequence.Verdict.Ready && !steps.HooksPatched && !c.Combat.HasOutstandingCleanup;
                    if (successor != null) ok &= SuccessorPathKept(before, after, successor) && ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move), successor) && w.Horse.View.StopMovingCount == stopsBefore;
                    else ok &= w.Horse.View.AgentASP.PathOwner == null;
                    log.Add("unload: " + v + ", hooks " + steps.HooksPatched + ", owed " + c.Combat.HasOutstandingCleanup + (successor != null ? ", successor {" + before + "} -> {" + after + "}" : ", agent path owner " + Id(w.Horse.View.AgentASP.PathOwner)));
                    clearFaults();
                };
                for (var i = 0; i < 4 && !handled; i++) Frame(c);
                c.BeforeDrive = null; clearFaults();
                if (successor != null) { successor.Interrupt(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                ok &= handled;
                Check(ok, "R12-F4 (round 15, production teardown sequence) " + kase + ": " + string.Join("; ", log.ToArray()));
            }
        }

        private static void R7ProvisionalLifecycle()
        {
            // (1) the 9 m/s openings at both first-tick lengths, from a fresh Preparing turn and from an Acting turn, with and without the player's
            // prior state (selection A + held horse); rider-first initiative.
            foreach (var tick in new[] { 1f / 60f, 0.1f })
                foreach (var acting in new[] { false, true })
                    foreach (var prior in new[] { false, true })
                        foreach (var opening in new[] { 0.12f, 1.2f })
                        {
                            var c = FirstTickPair(opening, tick, false, acting: acting);
                            var a = prior ? PriorSelectionAndHold(c) : null;
                            var r = RunProvisional(c, c.World.Target);
                            var label = "R1-F6 (round 7) tick " + tick.ToString("0.####") + " s, " + (acting ? "Acting turn (installed Tick transition)" : "fresh Preparing turn") +
                                ", " + (prior ? "prior selection " + a.UniqueId + " and held horse" : "no prior state") + ", opening " + opening.ToString("0.##") + " m: ";
                            if (opening < 1f)
                                Check(Committed(c, r, c.World.Target, tick, prior) && (!prior || c.Combat.DeferredClickTargetWriteCount == 1 && c.Combat.DeferredHoldWriteCount >= 1),
                                    label + "committed at the carrier's first tick, one strike; " + r.Facts(c) + "; deferred target writes " + c.Combat.DeferredClickTargetWriteCount +
                                    ", deferred Hold writes " + c.Combat.DeferredHoldWriteCount + ", carrier release proven " + r.Order.CarrierReleaseProven);
                            else
                                Check(RefusedKeptPrior(c, r, "no-affordable-strike-position") && c.Combat.FirstTickRetiredCommandCount == 1,
                                    label + "refused before any debit, provisional order and carrier retired, prior state kept; " + r.Facts(c) +
                                    "; retired commands " + c.Combat.FirstTickRetiredCommandCount);
                        }

            // (1b) mount-first initiative (production pairing), fresh Preparing turn.
            foreach (var opening in new[] { 0.12f, 1.2f })
            {
                var c = FirstTickPair(opening, 1f / 60f, true);
                var r = RunProvisional(c, c.World.Target);
                Check(opening < 1f ? Committed(c, r, c.World.Target, 1f / 60f, false) : RefusedKeptPrior(c, r, "no-affordable-strike-position"),
                    "R1-F6 (round 7) " + c.Arrangement + " (" + c.MountSlot + "), opening " + opening.ToString("0.##") + " m: " + r.Facts(c));
            }

            // (R1-F6 regression) round 6's mount-only provisional carrier: with no rider command the Preparing turn never reaches Acting, native
            // eligibility refuses the off-turn carrier and the movement hook refuses a non-Acting turn, so the gate is never asked.
            foreach (var acting in new[] { false, true })
            {
                var c = FirstTickPair(0.12f, 1f / 60f, false, acting: acting);
                bool stepMode, single; string reason;
                var grant = c.Pair.PairedMountGrantedMetres(c.Context, out stepMode, out single, out reason);
                var probe = PairedAttackClickProbe.Evaluate(c.World.Rider, c.World.Horse, c.World.Target, grant);
                var bare = new UnitMoveTo(probe.Route[probe.Route.Count - 1], probe.ArrivalRadius) { CreatedByPlayer = true };
                bare.ForcedPath = new Pathfinding.ForcedPath(new List<Vector3>(probe.Route));
                c.World.Horse.Commands.Run(bare);
                var gate = new RecordingGate { Carrier = bare };
                c.Pair.BindMountFirstTickGate(gate, true);
                var start = c.World.Horse.Position;
                for (var i = 0; i < 8; i++) Frame(c);
                Check(gate.Decisions == 0 && c.World.Horse.Position.Equals(start) && c.World.Horse.CombatState.Cooldown.MoveAction == 0f && !bare.IsFinished &&
                    !c.Pair.NativeActorEligibleForCommand(c.World.Horse, bare) && c.IneligibleCarrierFrames == 8 && (acting || c.Context.Status == TurnController.TurnStatus.Preparing),
                    "R1-F6 (round 7) regression, " + (acting ? "Acting" : "Preparing") + " turn: a mount-only provisional carrier (round 6's design; grant " + reason +
                    ") never reaches its first tick: gate decisions " + gate.Decisions + ", ineligible carrier frames " + c.IneligibleCarrierFrames + ", turn " + c.Context.Status +
                    ", displacement 0, mount Move " + c.World.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s");
            }

            // (2) player state: a genuinely newer assignment made while the order is pending survives its commit.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var newer = new UnitEntityData("hostile-newer") { Position = new Vector3(1.0f, 0f, -0.6f) };
                newer.View.Corpulence = 0.5f;
                var r = RunProvisional(c, c.World.Target, null, () =>
                {
                    var assign = new HarnessPlayerCommand(newer, UnitCommand.CommandType.Free);
                    c.World.Rider.Commands.Run(assign);   // native Run's UpdateCombatTarget: the player's newer selection
                    assign.FinishSuccess(); c.World.Rider.Commands.RemoveFinishedAndUpdateQueue();
                });
                Check(r.Decision.StartsWith("committed:replacement:") && ReferenceEquals(c.World.Rider.CombatState.ManualTarget, newer) && ReferenceEquals(r.IntentAfter, c.World.Target) &&
                    r.GenAfter > r.GenBefore && r.Proposal.CommittedGeneration == r.GenAfter && !r.HoldAfter && r.Order.StruckTargets.Count == 1,
                    "R6-F1 (round 7) a newer selection " + newer.UniqueId + " assigned while B was pending is kept at the commit (intent " + Name(r.IntentAfter) + ", manual target " +
                    Name(c.World.Rider.CombatState.ManualTarget) + ", prior " + a.UniqueId + "); " + c.Combat.LastStockAttackObservation);
            }

            // (4) live rider entitlement and the click-time target-life fact.
            foreach (var kase in new[] { "rider Standard spent", "rider CanNotAttack", "target falls during the wait", "target already down at the click" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                if (kase.Contains("already down")) w.Target.Descriptor.State.IsConscious = false;
                Action change = kase.StartsWith("rider Standard") ? (Action)(() => w.Rider.HasStandard = false) : kase.Contains("CanNotAttack") ? (Action)(() => w.Rider.Descriptor.State.CanNotAttack = true) :
                    kase.Contains("falls") ? (Action)(() => w.Target.Descriptor.State.IsConscious = false) : null;
                var r = RunProvisional(c, w.Target, null, change);
                var ok = kase.Contains("already down") ? Committed(c, r, w.Target, 1f / 60f, true) && c.Combat.LastStockAttackObservation.Contains("targetUnconsciousWhenOrdered=True") :
                    RefusedKeptPrior(c, r, kase.Contains("falls") ? "target-invalid-under-click-facts" : "rider-not-entitled", kase.StartsWith("rider Standard"));
                Check(ok, "R6-F3 (round 7) " + kase + ": " + r.Facts(c) + "; prior " + a.UniqueId + "; " + c.Combat.LastStockAttackObservation);
            }

            // (5) an ordinarily admitted route-bound order whose mount becomes first-tick-only before its carrier is created (R6-F4).
            foreach (var opening in new[] { 0.12f, 1.2f })
            {
                var c = FirstTickPair(opening, 1f / 60f, false, false);
                var w = c.World;
                var before = Observe(c);
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                var generation = c.Combat.Generation;
                w.Horse.MoveRestricted = true;   // the mount's movement becomes restricted (a native condition) before its carrier exists
                Frame(c);
                var carrier = LiveMountCarrier(c);
                var gate = c.Pair.movementState.FirstTickGate as MountedCombatController.FirstTickProposal;
                var gated = gate != null && !gate.Replacement && ReferenceEquals(gate.CarrierMove, carrier) && (c.Combat.LastRouteRevalidation ?? "").Contains("firstTick=True");
                var start = w.Horse.Position;
                Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                var displaced = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, w.Horse.Position);
                var afterTick = Observe(c);
                for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var ok = click.Admitted && order != null && Field(line, "mountGrant") == "granted" && gated && (opening < 1f
                    ? decision.StartsWith("committed:order:") && order.StruckTargets.Count == 1 && Math.Abs(afterTick.MountMove - 1f / 60f) < 1e-4f && displaced > 0.1f && c.Combat.Generation == generation
                    : decision.StartsWith("refused:no-affordable-strike-position") && displaced < 1e-6f && afterTick.MountMove == before.MountMove && order.IsFinished &&
                      order.StruckTargets.Count == 0 && LiveMountCarrier(c) == null && !c.Combat.HasStockAttackIntent && w.Rider.HasStandard);
                Check(ok, "R6-F4 (round 7) ordinary order, opening " + opening.ToString("0.##") + " m: admitted with grant " + Field(line, "mountGrant") + " (" + Field(line, "mountGrantedMetres") +
                    " m), revalidation " + c.Combat.LastRouteRevalidation + "; carrier " + Id(carrier) + " gated=" + gated + "; decision " + decision + "; displacement " +
                    displaced.ToString("0.####") + " m, mount Move " + afterTick.MountMove.ToString("0.####") + " s; strikes " + (order == null ? 0 : order.StruckTargets.Count) +
                    "; intent kept " + c.Combat.HasStockAttackIntent + " (generation " + c.Combat.Generation + ")");
            }

            // (6) A -> pending B -> accepted continuation of A: the newer accepted click retires B's provisional order and its exact carrier.
            foreach (var kase in new[] { "prior selection A (no live order): a native-accepted click on A", "retained undispatched intent A (dispatch refused by the game mode; not player-reachable): same-target continuation" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var w = c.World;
                var a = PriorSelectionAndHold(c);
                if (kase.StartsWith("retained"))
                {
                    Kingmaker.Game.Instance.CurrentMode = new Kingmaker.GameModeFacade("Cutscene");
                    ClickTick(c, a, false);
                    Kingmaker.Game.Instance.CurrentMode = new Kingmaker.GameModeFacade("Default");
                }
                var generationA = c.Combat.Generation;
                var intentA = c.Combat.IntentTarget;
                var clickB = ClickTick(c, w.Target);
                var proposal = c.Combat.FirstTickProposalForDiagnostics;
                var orderB = w.Rider.Commands.Standard as MountedPairAttackCommand;
                Frame(c);   // B's carrier exists
                var carrierB = LiveMountCarrier(c);
                var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                ClickTick(c, a, false);   // the newer native-accepted click on A
                var orderA = w.Rider.Commands.Standard as MountedPairAttackCommand;
                var continued = (c.Combat.LastStockAttackObservation ?? "").StartsWith("intent-continued");
                var ok = clickB.Admitted && proposal != null && proposal.Replacement && carrierB != null && ReferenceEquals(proposal.CarrierMove, carrierB) &&
                    proposal.State == MountedFirstTickProposalState.Refused && proposal.Detail == "a newer accepted click" && orderB.IsFinished && carrierB.IsFinished &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && w.Horse.CombatState.Cooldown.MoveAction == moveBefore &&
                    ReferenceEquals(c.Combat.IntentTarget, a) && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState && orderB.StruckTargets.Count == 0 &&
                    (kase.StartsWith("retained") ? continued && c.Combat.Generation == generationA && ReferenceEquals(intentA, a) : c.Combat.Generation > generationA && orderA != null && !ReferenceEquals(orderA, orderB));
                Check(ok, "R6-F5 (round 7) " + kase + ": B's provisional order " + Id(orderB) + " and exact carrier " + Id(carrierB) + " retired (" + proposal?.State + ":" + proposal?.Detail +
                    "); A generation " + generationA + "->" + c.Combat.Generation + ", intent " + Name(c.Combat.IntentTarget) + ", manual target " + Name(w.Rider.CombatState.ManualTarget) +
                    ", horse Hold " + w.Horse.HoldState + ", mount Move debit " + (w.Horse.CombatState.Cooldown.MoveAction - moveBefore).ToString("0.####") + " s; " + c.Combat.LastStockAttackObservation);
            }

            // (7) rider-first and horse-first processing after the decision, including commit in place.
            foreach (var horseFirst in new[] { false, true })
                foreach (var kase in new[] { "moving commit", "commit in place at the first tick", "in place before any tick" })
                {
                    var c = FirstTickPair(0.12f, 1f / 60f);
                    c.HorseFirst = horseFirst;
                    Action open = () => c.Opening[0] = -10f;
                    var r = RunProvisional(c, c.World.Target, kase == "in place before any tick" ? open : null, kase == "commit in place at the first tick" ? open : null);
                    var inPlace = kase != "moving commit";
                    Check(Committed(c, r, c.World.Target, 1f / 60f, false, inPlace) && (!inPlace || r.Decision.StartsWith("committed:replacement:in-range")) &&
                        (kase != "in place before any tick" || r.Carrier == null),
                        "R6-F6 (round 7) " + (horseFirst ? "horse-first" : "rider-first") + ", " + kase + ": " + r.Facts(c) + "; carrier release proven " + r.Order.CarrierReleaseProven);
                }

            // (8) installation and acceptance failures, repeated cleanup, and a later legitimate ground move.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                UnitCommands.RunThrowsBeforeInstall = command => command is MountedPairAttackCommand;
                var click = ClickTick(c, c.World.Target);
                UnitCommands.RunThrowsBeforeInstall = null;
                var last = c.Combat.LastFirstTickProposal;
                for (var i = 0; i < 3; i++) Frame(c);
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                Check(click.Admitted && last != null && last.State == MountedFirstTickProposalState.Refused && last.Detail.StartsWith("provisional-order-not-installed") &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && c.Combat.Active == null && c.World.Rider.Commands.Empty &&
                    LiveMountCarrier(c) == null && ReferenceEquals(c.World.Rider.CombatState.ManualTarget, a) && c.World.Horse.HoldState && !c.Combat.HasStockAttackIntent &&
                    Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f && !line.Contains("first-tick"),
                    "R6-F7 (round 7) native Run failure before installation: settled " + last?.State + ":" + last?.Detail + "; no gate, no active command, rider slots empty; selection " +
                    Name(c.World.Rider.CombatState.ManualTarget) + ", horse Hold " + c.World.Horse.HoldState + "; a later ground tick moved " + moved.ToString("0.####") + " m, debited " +
                    debit.ToString("0.####") + " s (" + line + ")");
            }
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var r = RunProvisional(c, c.World.Target);
                var keptFirst = RefusedKeptPrior(c, r, "no-affordable-strike-position");
                var refusals = c.Combat.FirstTickRefusalCount; var retired = c.Combat.FirstTickRetiredCommandCount;
                var newer = new UnitMoveTo(new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true };
                c.World.Horse.Commands.Run(newer);   // newer work on the mount after the settlement
                for (var i = 0; i < 3; i++) c.Combat.SweepFirstTickProposal();
                c.Combat.Drive(); c.Combat.Drive();
                var newerLive = !newer.IsFinished && ReferenceEquals(LiveMountCarrier(c), newer);
                newer.Interrupt();
                c.World.Horse.Commands.RemoveFinishedAndUpdateQueue();
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                Check(keptFirst && newerLive &&
                    c.Combat.FirstTickRefusalCount == refusals && c.Combat.FirstTickRetiredCommandCount == retired && retired == 1 && c.Combat.FirstTickCommitCount == 0 &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && r.Proposal.State == MountedFirstTickProposalState.Refused &&
                    Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f && !line.Contains("first-tick"),
                    "R6-F7 (round 7) repeated settlement: three sweeps and two drives after the refusal revive nothing (refusals " + c.Combat.FirstTickRefusalCount + ", retired commands " +
                    c.Combat.FirstTickRetiredCommandCount + ", commits " + c.Combat.FirstTickCommitCount + "), newer mount work " + Id(newer) + " kept live=" + newerLive +
                    "; a later ground tick moved " + moved.ToString("0.####") + " m, debited " + debit.ToString("0.####") + " s (" + line + "); prior " + a.UniqueId);
            }

            // ===== round 8 =====
            // R6-F2: the agent's own path and cursor decide; a command path that is not the agent's is refused.
            foreach (var kase in new[] { "shift -0.05 m after the agent advanced its cursor", "short waypoint at 0.05 m", "command path rewritten after the agent took its own" })
                foreach (var tick in new[] { 1f / 60f, 0.1f })
                {
                    var c = FirstTickPair(0.12f, tick);
                    var agent = c.World.Horse.View.AgentASP;
                    if (kase.StartsWith("short")) agent.Route = (from, to) => new List<Vector3> { from, new Vector3(0.05f, 0f, 0f), to };
                    var cursor = -1;
                    var r = RunProvisional(c, c.World.Target, null, () =>
                    {
                        cursor = agent.NextPointIndex;
                        if (kase.StartsWith("shift")) { var d = new Vector3(-0.05f, 0f, 0f); c.World.Horse.Position = c.World.Horse.Position + d; c.World.Rider.Position = c.World.Rider.Position + d; }
                        if (kase.StartsWith("command")) ((UnitMoveTo)LiveMountCarrier(c)).ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(0.6f, 0f, 0f) });
                    });
                    var expectCommit = kase.StartsWith("shift") && tick > 0.05f;
                    var reason = kase.StartsWith("command") ? "agent-path-not-carrier" : "no-affordable-strike-position";
                    // Round 18 (R17-F2, changed expectation for the short waypoint): native keeps the 0.05 m waypoint and the shift carries the mount
                    // beyond it (no snap back on an interior segment); that native stop is now evaluated, so the first visible stop (0.12 m) commits
                    // with its own duration, and the executed stop, the debit and the strike agree with the prediction.
                    var shortCommit = kase.StartsWith("short");
                    var ok = cursor == 1 && (expectCommit ? Committed(c, r, c.World.Target, tick, false) && r.StopAfterDecision == "(0.17,0)" :
                        shortCommit ? r.PendingOk && r.Decision.StartsWith("committed:replacement:first-tick-interior(") && r.Decision.Contains("waypoint#1=(0.05,0)(interior)") &&
                            r.Decision.Contains("passesWaypoint=True") && r.StopAfterDecision == Field(r.Decision, "stop") &&
                            Math.Abs(r.AfterDecision.MountMove - r.Before.MountMove - ParseFloat(Field(r.Decision, "approvedTick"))) < 1e-7f && r.Order.StruckTargets.Count == 1 :
                        RefusedKeptPrior(c, r, reason) && (!kase.StartsWith("shift") || r.Decision.Contains("waypoint#1")));
                    Check(ok, "R6-F2 (round 8) " + kase + ", tick " + tick.ToString("0.####") + " s: agent cursor " + cursor + "; predicted stop " + Field(r.Decision, "stop") + " (travel " +
                        Field(r.Decision, "travel") + " m, granted " + Field(r.Decision, "grantedMetres") + " m); executed stop " + r.StopAfterDecision + ", displacement " + r.Displacement.ToString("0.####") +
                        " m, mount Move debit " + (r.AfterDecision.MountMove - r.Before.MountMove).ToString("0.####") + " s; strikes " + (r.Order == null ? 0 : r.Order.StruckTargets.Count) + "; " + r.Decision);
                }

            // R7-F1: the movement mode in force at the carrier's first debit decides, before that debit.
            foreach (var kase in new[] { "restriction lifted before carrier creation", "restriction lifted after binding" })
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                Action lift = () => c.World.Horse.MoveRestricted = false;
                var r = RunProvisional(c, c.World.Target, kase.Contains("before") ? lift : null, kase.Contains("after") ? lift : null, 40);
                var ok = r.PendingOk && r.GateBound && r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Before.MountMove == 0f &&
                    Math.Abs(r.AfterDecision.MountMove - 1f / 60f) < 1e-4f && r.Proposal.CommittedGeneration == r.GenAfter && r.Order.StruckTargets.Count == 1 && c.World.Horse.Position.x > 1.19f;
                Check(ok, "R7-F1 (round 8) " + kase + ", 1.2 m: the gate bound to the provisional carrier decided before its first debit (" + r.Decision + "; mount Move 0 -> " +
                    r.AfterDecision.MountMove.ToString("0.####") + " s after the decision tick); generation " + r.GenBefore + "->" + r.GenAfter + "; approach completed at " + c.World.Horse.Position +
                    ", strikes " + r.Order.StruckTargets.Count + ", final mount Move " + r.AtStrike.MountMove.ToString("0.####") + " s");
            }
            foreach (var opening in new[] { 0.12f, 1.2f })
            {
                var c = FirstTickPair(opening, 1f / 60f, false, false);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);   // the ordinary carrier is installed (revalidated with the ordinary grant) and bound
                var carrier = LiveMountCarrier(c);
                var gate = c.Pair.movementState.FirstTickGate as MountedCombatController.FirstTickProposal;
                w.Horse.MoveRestricted = true;   // the restriction arrives after installation, before the first debit
                var start = w.Horse.Position;
                Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                var displaced = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, w.Horse.Position);
                var debit = w.Horse.CombatState.Cooldown.MoveAction;
                for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var ok = click.Admitted && order != null && gate != null && !gate.Replacement && ReferenceEquals(gate.CarrierMove, carrier) && (opening < 1f
                    ? decision.StartsWith("committed:order:first-tick") && Math.Abs(debit - ParseFloat(Field(decision, "grantedMetres")) / 9f) < 1e-4f && order.StruckTargets.Count == 1
                    : decision.StartsWith("refused:no-affordable-strike-position") && displaced < 1e-6f && debit == 0f && order.IsFinished && !c.Combat.HasStockAttackIntent);
                Check(ok, "R7-F1 (round 8) ordinary carrier, restriction applied after installation, opening " + opening.ToString("0.##") + " m: carrier " + Id(carrier) + " decided at its first tick (" +
                    decision + "); displacement " + displaced.ToString("0.####") + " m, mount Move " + debit.ToString("0.####") + " s; strikes " + (order == null ? 0 : order.StruckTargets.Count));
            }

            // R7-F2: the live wrapper's three plans must agree before any commit.
            foreach (var kase in new[] { "live wrapper reach changed after the carrier", "live wrapper weapon changed before an in-place strike" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var inPlace = kase.Contains("in-place");
                Action mutate = () =>
                {
                    var order = (MountedPairAttackCommand)c.World.Rider.Commands.Standard;
                    if (inPlace) { c.Opening[0] = -10f; order.AllAttacks[0].Weapon = new ItemWeaponFacade(); } else order.AllAttacks[0].WeaponRange = 0.5f;
                };
                var r = RunProvisional(c, c.World.Target, inPlace ? mutate : null, inPlace ? null : mutate);
                var ok = r.PendingOk && c.Combat.FirstTickCommitCount == 0 && r.Starts == 0 && r.Displacement < 1e-6f && r.AtStrike.MountMove == 0f && r.GenAfter == r.GenBefore &&
                    ReferenceEquals(r.TargetAfter, a) && r.HoldAfter && r.Order.IsFinished && r.Order.StruckTargets.Count == 0 &&
                    (inPlace ? r.Order.EndCauseDetail == "live wrapper plan changed" : r.Decision.StartsWith("refused:plan-changed(live wrapper plan changed)"));
                Check(ok, "R7-F2 (round 8) " + kase + ": zero commits (" + c.Combat.FirstTickCommitCount + "), decision " + r.Decision + ", order end " + r.Order.EndCause + "(" + r.Order.EndCauseDetail +
                    "); displacement 0, mount Move " + r.AtStrike.MountMove.ToString("0.####") + " s, strikes 0; generation " + r.GenBefore + "->" + r.GenAfter + ", target " + Name(r.TargetAfter) + ", horse Hold " + r.HoldAfter);
            }

            // R6-F1: a newer assignment of the SAME target A is observed (installed-command hook) and kept at the commit.
            foreach (var opening in new[] { 0.12f, 1.2f })
            {
                var c = FirstTickPair(opening, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var r = RunProvisional(c, c.World.Target, null, () =>
                {
                    var assign = new HarnessPlayerCommand(a, UnitCommand.CommandType.Free);
                    c.World.Rider.Commands.Run(assign);
                    assign.FinishSuccess(); c.World.Rider.Commands.RemoveFinishedAndUpdateQueue();
                });
                string afterRetire = "-";
                var ok = opening < 1f
                    ? r.Decision.StartsWith("committed:replacement:") && ReferenceEquals(r.IntentAfter, c.World.Target) && ReferenceEquals(c.World.Rider.CombatState.ManualTarget, a) &&
                      c.Combat.LastStockAttackObservation.Contains("newerTargetKept=True") && r.Order.StruckTargets.Count == 1
                    : RefusedKeptPrior(c, r, "no-affordable-strike-position") && ReferenceEquals(c.World.Rider.CombatState.ManualTarget, a);
                if (opening < 1f) { c.Combat.Cancel("owner cancelled after the strike"); afterRetire = Name(c.World.Rider.CombatState.ManualTarget); ok &= ReferenceEquals(c.World.Rider.CombatState.ManualTarget, a); }
                Check(ok, "R6-F1 (round 8) the player assigned " + a.UniqueId + " again while B was pending, opening " + opening.ToString("0.##") + " m: " + r.Decision + "; intent " + Name(r.IntentAfter) +
                    ", manual target " + Name(r.TargetAfter) + " (after B's retirement: " + afterRetire + "); " + c.Combat.LastStockAttackObservation);
            }

            // R7-F3: newer competing work accepted on either participant supersedes the pending provisional order.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var orderB = w.Rider.Commands.Standard as MountedPairAttackCommand;
                Frame(c);
                var carrierB = LiveMountCarrier(c);
                var t2 = new UnitEntityData("hostile-successor") { Position = new Vector3(1.0f, 0f, -0.6f) };
                var successor = new HarnessPlayerCommand(t2, UnitCommand.CommandType.Standard);
                w.Rider.Commands.Enqueue(successor);   // newer competing rider work, queued while B holds Standard
                var refusedAtQueue = p.State == MountedFirstTickProposalState.Refused;
                for (var i = 0; i < 3; i++) Frame(c);
                Check(click.Admitted && refusedAtQueue && (p.Detail ?? "").StartsWith("superseded-by-newer-work(rider:") && orderB.IsFinished && carrierB != null && carrierB.IsFinished &&
                    ReferenceEquals(w.Rider.Commands.Standard, successor) && !successor.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == 0f && orderB.StruckTargets.Count == 0 &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null,
                    "R7-F3 (round 8) rider queue: B (" + Id(orderB) + ", carrier " + Id(carrierB) + ") settled " + p.State + ":" + p.Detail + "; successor " + Id(successor) + " holds the rider's Standard; " +
                    "mount Move 0 s, no strike; prior A afterwards: " + c.Combat.LastStockAttackObservation + " (intent " + Name(c.Combat.IntentTarget) + ")");
            }
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var orderB = w.Rider.Commands.Standard as MountedPairAttackCommand;
                var newer = new UnitMoveTo(new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true };
                w.Horse.Commands.Run(newer);   // newer competing mount work before B can issue a carrier
                for (var i = 0; i < 3; i++) Frame(c);
                Check(click.Admitted && (p.Detail ?? "").StartsWith("superseded-by-newer-work(mount:") && orderB.IsFinished && orderB.DelegatedMove == null && !newer.IsFinished &&
                    ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move), newer) && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && ReferenceEquals(w.Rider.CombatState.ManualTarget, a),
                    "R7-F3 (round 8) mount work before the carrier: B settled " + p.State + ":" + p.Detail + " (order end " + orderB.EndCause + "); successor " + Id(newer) + " kept in the Move slot; mount Move 0 s; " +
                    "the drive runs on (pending=" + (c.Combat.FirstTickProposalForDiagnostics != null) + ")");
            }

            // R7-F4: a moving approach needs the mount's current movement authority; a stationary strike needs only the rider's own entitlement.
            foreach (var kase in new[] { "mount CanMove revoked", "mount must stand", "in-range control with CanMove revoked" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var r = RunProvisional(c, c.World.Target, null, () =>
                {
                    if (kase.StartsWith("mount must")) c.Pair.PartnerCanGetUp = true; else c.World.Horse.Descriptor.State.CanMove = false;
                    if (kase.StartsWith("in-range")) c.Opening[0] = -10f;
                });
                var ok = kase.StartsWith("in-range") ? Committed(c, r, c.World.Target, 1f / 60f, true, true) && r.Decision.StartsWith("committed:replacement:in-range")
                    : RefusedKeptPrior(c, r, "mount-cannot-move(" + (kase.StartsWith("mount must") ? "mount-must-stand" : "mount-cannot-move") + ")");
                Check(ok, "R7-F4 (round 8) " + kase + ": " + r.Decision + "; displacement " + r.Displacement.ToString("0.####") + " m, mount Move " + r.AtStrike.MountMove.ToString("0.####") + " s, strikes " +
                    (r.Order == null ? 0 : r.Order.StruckTargets.Count) + "; prior " + a.UniqueId + " kept=" + ReferenceEquals(r.TargetAfter, a));
            }

            // R7-F5: a pending door settlement is never cancelled by a provisional installation; an accepted retarget cancels it normally.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var settlement = new MountedDoorOccupancySettlement();
                c.Combat.PendingDoorSettlement = settlement;
                var click = ClickTick(c, c.World.Target);
                var last = c.Combat.LastFirstTickProposal;
                var kept = ReferenceEquals(c.Combat.PendingDoorSettlement, settlement) && !settlement.Cancelled;
                settlement.IsFinished = true;   // the settlement completes on its own afterwards
                var o = FirstTickPair(0.12f, 1f / 60f, false, false);
                var other = new MountedDoorOccupancySettlement();
                o.Combat.PendingDoorSettlement = other;
                var accepted = ClickTick(o, o.World.Target);
                Check(click.Admitted && last != null && last.Detail == "door-settlement-pending" && kept && c.World.Rider.Commands.Empty && c.Combat.Active == null &&
                    accepted.Admitted && accepted.Order != null && other.Cancelled && o.Combat.PendingDoorSettlement == null,
                    "R7-F5 (round 8) door settlement: the provisional request was declined (" + last?.State + ":" + last?.Detail + "), the settlement stayed pending uncancelled=" + kept +
                    " and could finish; an accepted ordinary retarget cancelled its settlement (" + other.CancelReason + ")");
            }

            // R6-F7: failures inside the decision and between acceptance mutations, over a real retained generation with both Holds set.
            foreach (var kase in new[] { "failure in the live entitlement read", "failure in the native order effect before retirement", "failure in a staged read (mount name)" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                c.World.Rider.HoldState = true;
                var gA = c.Combat.Generation;
                var rider = c.World.Rider;
                var r = RunProvisional(c, c.World.Target, null, () =>
                {
                    if (kase.Contains("entitlement")) Harness.NativeActorCapability.ReadFaults = unit => unit == rider;
                    else if (kase.Contains("native order effect")) MountedCombatController.FaultBeforeRetirement = true;
                    else if (kase.Contains("staged read")) MountedCombatController.FaultAfterEffects = true;
                    else MountedCombatController.FaultInRetirement = true;
                });
                var between = kase.Contains("between");
                var riderHold = c.World.Rider.HoldState; var horseHold = c.World.Horse.HoldState;
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                var reason = kase.Contains("entitlement") ? "decision-failed(InvalidOperationException)" :
                    "commit-failed(InvalidOperationException);previousIntent=" + (between ? "retired" : "kept");
                var ok = r.PendingOk && r.Decision.StartsWith("refused:" + reason) && r.Proposal.State == MountedFirstTickProposalState.Refused && c.Combat.FirstTickCommitCount == 0 &&
                    riderHold && horseHold && r.Displacement < 1e-6f && r.AfterDecision.MountMove == 0f && r.Starts == 0 && r.Order.IsFinished && (r.Carrier == null || r.Carrier.IsFinished) &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null &&
                    (between || r.GenAtDecision == gA && ReferenceEquals(r.IntentAtDecision, a) && ReferenceEquals(r.TargetAtDecision, a)) &&
                    Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f && !line.Contains("first-tick");
                Check(ok, "R6-F7 (round 8) " + kase + ": " + r.Decision + "; rider Hold " + riderHold + ", horse Hold " + horseHold + "; A at the decision: generation " + r.GenAtDecision +
                    ", intent " + Name(r.IntentAtDecision) + ", manual target " + Name(r.TargetAtDecision) + " (after A's own next drive: generation " + r.GenAfter + ", intent " + Name(r.IntentAfter) +
                    ", " + c.Combat.LastStockAttackObservation + "); gate released, B's command and carrier ended; a later ground tick moved " + moved.ToString("0.####") +
                    " m, debited " + debit.ToString("0.####") + " s");
            }

            // ===== round 9 =====
            // R5-F3: the actor-specific terminal runs in UnitActionController, the controller drive follows in the same frame, and the player's
            // pointer event comes in a later frame. Horse able to follow: the drive dispatches the horse's follow-through (A stays the order, its
            // horse strikes) and B cannot install (the pair is busy). Horse unable to follow: the drive completes A, and B is a fresh request.
            foreach (var horseFollows in new[] { true, false })
                foreach (var opening in new[] { 0.12f, 1.2f })
                {
                    var c = FirstTickPair(opening, 1f / 60f, false, sightOnlyToTarget: horseFollows);
                    var w = c.World;
                    var a = new UnitEntityData("hostile-prior") { Position = horseFollows ? new Vector3(1.0f, 0f, 0.8f) : new Vector3(-1.5f, 0f, 0.6f) };
                    a.View.Corpulence = 0.5f;
                    var order = new List<string>();
                    ClickTick(c, a, false);   // frame k-1's pointer event: A accepted and dispatched
                    var wrapperA = w.Rider.Commands.Standard as MountedPairAttackCommand;
                    var gA = c.Combat.Generation;
                    order.Add("k-1 pointer: A accepted, generation " + gA + ", wrapper " + Id(wrapperA));
                    c.ActionStage = () =>
                    {
                        w.Rider.Descriptor.State.CanAct = false;
                        var observation = KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.CaptureCanAct(w.Rider.Descriptor.State.CanAct);
                        KingmakerMountedCombat.Integration.NativeCommandTerminalBranches.TurnBasedForceFinish(wrapperA, UnitCommand.ResultType.Success, observation);
                        w.Rider.Descriptor.State.CanAct = true;
                        order.Add("k action: terminal (" + c.Combat.LastStockAttackObservation + ")");
                    };
                    c.BeforeDrive = () => { c.BeforeDrive = null; order.Add("k drive: starts with intent " + Name(c.Combat.IntentTarget) + " generation " + c.Combat.Generation); };
                    Frame(c);
                    var afterDrive = c.Combat.LastStockAttackObservation;
                    var horseWrapper = w.Horse.Commands.Standard as MountedPairAttackCommand;
                    order.Add("k drive: " + afterDrive + " (intent " + Name(c.Combat.IntentTarget) + ", generation " + c.Combat.Generation + ", horse order " + Id(horseWrapper) + ")");
                    if (horseFollows)
                    {
                        var before = Observe(c);
                        var click = ClickTick(c, w.Target);   // frame k+1's pointer event
                        var last = c.Combat.LastFirstTickProposal;
                        order.Add("k+1 pointer: B " + (click.Admitted ? "admitted" : "not admitted") + ", provisional " + (last == null ? "none" : last.State + ":" + last.Detail));
                        for (var i = 0; i < 8 && horseWrapper != null && horseWrapper.StruckTargets.Count == 0; i++) Frame(c);
                        var after = Observe(c);
                        Check(gA > 0 && horseWrapper != null && c.Combat.Generation == gA && ReferenceEquals(c.Combat.IntentTarget, a) && last != null && last.State == MountedFirstTickProposalState.Refused &&
                            (last.Detail == "pair-work-live" || last.Detail == "mount-holds-live-work") && horseWrapper.StruckTargets.Count == 1 && after.MountMove == before.MountMove && after.RiderStandard &&
                            LiveMountCarrier(c) == null,
                            "R5-F3 (round 9) horse follows, B opening " + opening.ToString("0.##") + " m: " + string.Join(" | ", order.ToArray()) + "; A generation " + gA + " kept, its horse struck " +
                            horseWrapper?.StruckTargets.Count + "; B installed nothing, mount Move " + after.MountMove.ToString("0.####") + " s, rider Standard " + (after.RiderStandard ? "unspent" : "spent"));
                    }
                    else
                    {
                        var completed = !c.Combat.HasStockAttackIntent;
                        var r = RunProvisional(c, w.Target);
                        order.Add("k+1 pointer: B provisional " + (r.Proposal == null ? "none" : "#" + Id(r.Proposal.Command)) + ", decision " + r.Decision);
                        var ok = completed && afterDrive.StartsWith("tb-intent-complete") && r.GenBefore > gA && r.IntentBefore == null &&
                            (opening < 1f ? Committed(c, r, w.Target, 1f / 60f, false) : RefusedKeptPrior(c, r, "no-affordable-strike-position"));
                        Check(ok, "R5-F3 (round 9) horse cannot follow, B opening " + opening.ToString("0.##") + " m: " + string.Join(" | ", order.ToArray()) + "; B carrier " + Id(r.Carrier) +
                            ", displacement " + r.Displacement.ToString("0.####") + " m; spending before {" + r.Before + "} after {" + r.AtStrike + "}");
                    }
                }

            // R6-F7: the transfer keeps the previous generation until the new order owns its command and carrier; deferred effects and cleanup are
            // tracked and retried. Ledger state from the RiderEndedForHorse terminal (a setup, not a reachability claim; see R5-F3), both Holds set.
            foreach (var kase in new[] { "fault between retirement and transfer", "fault during the deferred Hold write", "fault during cleanup" })
            {
                var c = FirstTickPair(kase.Contains("cleanup") ? 1.2f : 0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                w.Rider.HoldState = true;
                var gA = c.Combat.Generation;
                string between = null;
                var outstandingSeen = false;
                c.BeforeDrive = () =>
                {
                    if (c.Combat.SettlingProposalForDiagnostics != null && c.Combat.SettlingProposalForDiagnostics.OutstandingEffects.Count > 0) outstandingSeen = true;
                    if (between == null && c.Combat.FirstTickCleanupFailureCount == 1)
                        between = "after the failed cleanup: request owned=" + (c.Combat.FirstTickProposalForDiagnostics != null) + ", gate bound=" + (c.Pair.movementState.FirstTickGate != null) +
                            ", B finished=" + (c.Combat.FirstTickProposalForDiagnostics?.Command?.IsFinished);
                };
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    if (kase.Contains("transfer")) MountedCombatController.FaultInTransfer = true;
                    else if (kase.Contains("Hold")) UnitEntityData.HoldWriteFault = unit => unit == w.Rider;
                    else HarnessScheduler.InterruptFault = true;
                });
                var riderHold = w.Rider.HoldState; var horseHold = w.Horse.HoldState;
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                bool ok;
                if (kase.Contains("transfer"))
                    ok = r.Decision.StartsWith("refused:commit-failed(InvalidOperationException);previousIntent=kept;generation=" + gA + "->" + gA) && r.GenAtDecision == gA &&
                        ReferenceEquals(r.IntentAtDecision, a) && ReferenceEquals(r.TargetAtDecision, a) && riderHold && horseHold && r.Displacement < 1e-6f && r.AfterDecision.MountMove == 0f &&
                        r.Order.IsFinished && r.Order.ProvisionalOrder && (r.Carrier == null || r.Carrier.IsFinished) && c.Combat.FirstTickCommitCount == 0;
                else if (kase.Contains("Hold"))
                    ok = r.Decision.Contains("effects=outstanding(rider-hold)") && outstandingSeen && c.Combat.SettlingProposalForDiagnostics == null && !riderHold && !horseHold &&
                        ReferenceEquals(r.IntentAfter, w.Target) && r.Order.StruckTargets.Count == 1;
                else
                    ok = r.Decision.StartsWith("refused:no-affordable-strike-position") && between != null && between.Contains("request owned=True, gate bound=True, B finished=False") &&
                        c.Combat.FirstTickCleanupFailureCount == 1 && c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && r.Order.IsFinished &&
                        r.Carrier.IsFinished && r.GenAtDecision == gA && riderHold && horseHold && r.AfterDecision.MountMove == 0f;
                // A refused request leaves the single move unspent for a later ordinary ground move; the committed one spent it on its own tick.
                if (!kase.Contains("Hold")) ok &= Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f && !line.Contains("first-tick");
                Check(ok, "R6-F7 (round 9) " + kase + ": " + r.Decision + "; owner at the decision: intent " + Name(r.IntentAtDecision) + " generation " + r.GenAtDecision + " (A was " + gA +
                    "), manual target " + Name(r.TargetAtDecision) + "; final intent " + Name(c.Combat.IntentTarget) + " generation " + c.Combat.Generation + ", rider Hold " + riderHold +
                    ", horse Hold " + horseHold + "; rider Standard slot " + Id(w.Rider.Commands.Standard) + ", mount Move slot " + Id(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move)) +
                    (between == null ? "" : "; " + between) + "; outstanding effects seen=" + outstandingSeen + "; a later ground tick moved " + moved.ToString("0.####") + " m, debited " + debit.ToString("0.####") + " s");
            }

            // R8-F1: the ordinary first-debit check uses the agent's live route and the strike's admission at arrival.
            foreach (var kase in new[] { "destination sight closed", "agent path differs from the command", "affordable control past a waypoint" })
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var agent = c.World.Horse.View.AgentASP;
                if (kase.StartsWith("affordable")) agent.Route = (from, to) => new List<Vector3> { from, new Vector3(0.05f, 0f, 0f), to };
                var r = RunProvisional(c, c.World.Target, null, () =>
                {
                    c.World.Horse.MoveRestricted = false;   // the restriction lifts after binding: the ordinary branch decides
                    if (kase.StartsWith("destination")) c.Opening[0] = 10f;
                    else if (kase.StartsWith("agent")) ((UnitMoveTo)LiveMountCarrier(c)).ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(0.6f, 0f, 0f) });
                    else
                    {
                        var d = new Vector3(0.06f, 0f, 0f); c.World.Horse.Position = d; c.World.Rider.Position = d;
                        agent.NextPointIndex = 2; var p2 = agent.Path.vectorPath[2]; agent.NextWaypoint = new UnityEngine.Vector2(p2.x, p2.z);
                    }
                }, 40);
                var ok = kase.StartsWith("affordable")
                    ? r.Decision.StartsWith("committed:replacement:ordinary-movement(remaining-route(cursor=2") && r.Order.StruckTargets.Count == 1 && c.World.Horse.Position.x > 1.19f
                    : RefusedKeptPrior(c, r, kase.StartsWith("destination") ? "route-arrival-not-admitted" : "agent-path-not-carrier");
                Check(ok, "R8-F1 (round 9) " + kase + ": " + r.Decision + "; displacement after the decision tick " + r.Displacement.ToString("0.####") + " m, mount Move " +
                    r.AfterDecision.MountMove.ToString("0.####") + " s; final position " + c.World.Horse.Position + ", strikes " + r.Order.StruckTargets.Count + "; prior " + a.UniqueId);
            }

            // R8-F2: an ordinary gate reads its generation's supersession and the accepted-work rule; compatible mount work coexists.
            foreach (var kase in new[] { "newer rider Standard queued", "in-range mount Swift beside the carrier" })
            {
                var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var orderA = click.Order;
                Frame(c);
                var carrier = LiveMountCarrier(c);
                UnitCommand other;
                if (kase.StartsWith("newer")) { other = new HarnessPlayerCommand(new UnitEntityData("hostile-successor") { Position = new Vector3(1f, 0f, -0.6f) }, UnitCommand.CommandType.Standard); w.Rider.Commands.Enqueue(other); }
                else { other = new HarnessPlayerCommand(null, UnitCommand.CommandType.Swift); w.Horse.Commands.Run(other); }
                var start = w.Horse.Position;
                Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                var displaced = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, w.Horse.Position);
                var debit = w.Horse.CombatState.Cooldown.MoveAction;
                for (var i = 0; i < 30 && orderA != null && !orderA.IsFinished && orderA.StruckTargets.Count == 0; i++) Frame(c);
                var ok = kase.StartsWith("newer")
                    ? decision.StartsWith("refused:superseded-by-newer-work(order generation") && displaced < 1e-6f && debit == 0f && orderA.IsFinished && orderA.StruckTargets.Count == 0 &&
                      carrier != null && carrier.IsFinished && ReferenceEquals(w.Rider.Commands.Standard, other) && !other.IsFinished
                    : decision.StartsWith("committed:order:ordinary-movement") && orderA.StruckTargets.Count == 1 && !other.IsFinished && ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Swift), other);
                Check(ok, "R8-F2 (round 9) " + kase + ": carrier " + Id(carrier) + ", decision " + decision + "; displacement " + displaced.ToString("0.####") + " m, mount Move " + debit.ToString("0.####") +
                    " s at the decision; order " + Id(orderA) + " strikes " + orderA.StruckTargets.Count + "; successor " + Id(other) + " live=" + !other.IsFinished);
            }

            // R8-F3: a refused request's command issues no further carrier (no Run: Hold and the retained order untouched).
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                var gA = c.Combat.Generation;
                var click = ClickTick(c, w.Target);
                Frame(c);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var orderB = w.Rider.Commands.Standard as MountedPairAttackCommand;
                var first = LiveMountCarrier(c) as UnitMoveTo;
                UnityEngine.Time.frameCount++;
                AgentMove(c, first);   // the decision frame's UnitMoveController stage: refused, tick zeroed
                first.Interrupt();     // native's denial branch ends the zeroed carrier before the controller sweep
                w.Horse.Commands.RemoveFinishedAndUpdateQueue();
                var runsBefore = w.Horse.Commands.RunCount;
                ActionTick(c, w.Rider);   // B's command finds its carrier ended and would replan
                var second = LiveMountCarrier(c);
                var holdBeforeDrive = w.Horse.HoldState; var supersededBeforeDrive = c.Combat.Ledger.Superseded(gA);
                c.Combat.Drive();
                Check(click.Admitted && p != null && p.State == MountedFirstTickProposalState.Refused && second == null && w.Horse.Commands.RunCount == runsBefore && holdBeforeDrive &&
                    !supersededBeforeDrive && orderB.IsFinished && orderB.EndCauseDetail == "first-tick request refused" && first.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null,
                    "R8-F3 (round 9) replan after a refusal: no second carrier (mount Runs " + runsBefore + " -> " + w.Horse.Commands.RunCount + "; B ended: " + orderB.EndCauseDetail + "); before the next drive horse Hold " +
                    holdBeforeDrive + ", A generation " + gA + " superseded=" + supersededBeforeDrive + "; cleanup exact, mount Move 0 s; A afterwards " + c.Combat.LastStockAttackObservation);
            }

            // R8-F4: an ordinary order's own release of its carrier for a stationary strike settles its gate; genuine carrier loss still refuses.
            foreach (var kase in new[] { "rider-first", "horse-first", "genuine carrier loss" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f, false, false);
                var w = c.World;
                c.HorseFirst = kase == "horse-first";
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                var carrier = LiveMountCarrier(c);
                if (kase.StartsWith("genuine")) { carrier.Interrupt(); c.Combat.Drive(); }
                else c.Opening[0] = -10f;   // the target becomes admissible before the carrier's first movement callback
                for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                var ok = kase.StartsWith("genuine")
                    ? decision.StartsWith("refused:carrier-lost-before-first-tick") && order.IsFinished && order.StruckTargets.Count == 0 && w.Horse.CombatState.Cooldown.MoveAction == 0f
                    : decision.StartsWith("committed:order:") && order.StruckTargets.Count == 1 && carrier.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                      w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move) == null && c.Combat.FirstTickProposalForDiagnostics == null;
                Check(ok, "R8-F4 (round 9) " + kase + ": carrier " + Id(carrier) + "; " + decision + "; strikes " + order.StruckTargets.Count + ", order end " + order.EndCause + "(" + order.EndCauseDetail +
                    "); mount Move " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s");
            }

            // R8-F5: the decision reads the input context the executing adapter debits with (the cached partner context), not the current selection.
            foreach (var opening in new[] { 0.12f, 1.2f })
            {
                var c = FirstTickPair(opening, 1f / 60f);
                var w = c.World;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    c.Context.EnabledSingleActionMove = false;            // the rider's own limit becomes two actions
                    Harness.SelectionManager.Instance.SelectedUnits.Clear();   // the pair is no longer singly selected
                }, 16);
                var firstDebit = r.AfterDecision.MountMove;
                var ok = r.Decision.Contains("first-tick(") && (opening < 1f
                    ? r.Decision.StartsWith("committed:replacement:") && Math.Abs(firstDebit - ParseFloat(Field(r.Decision, "grantedMetres")) / 9f) < 1e-4f && r.AtStrike.MountMove == firstDebit && r.Order.StruckTargets.Count == 1
                    : RefusedKeptPrior(c, r, "no-affordable-strike-position") && r.AtStrike.MountMove == 0f);
                Check(ok, "R8-F5 (round 9) rider limit two actions, mount single action, selection cleared after binding, opening " + opening.ToString("0.##") + " m: " + r.Decision +
                    "; mount Move after the decision tick " + firstDebit.ToString("0.####") + " s, final " + r.AtStrike.MountMove.ToString("0.####") + " s; strikes " + r.Order.StruckTargets.Count);
            }

            // R8-F6: the restricted tick uses the agent's own speed (the installed shift contract) and refuses when the agent would turn.
            foreach (var kase in new[] { "agent speed 6 m/s (nominal 9), tick 1/60 s", "agent speed 6 m/s (nominal 9), tick 0.1 s", "agent heading turned 90 degrees" })
            {
                var tick = kase.Contains("0.1 s") ? 0.1f : 1f / 60f;
                var c = FirstTickPair(0.12f, tick);
                var agent = c.World.Horse.View.AgentASP;
                if (kase.StartsWith("agent speed")) agent.SpeedFactor = 6f / 9f;
                var r = RunProvisional(c, c.World.Target, null, () => { if (kase.Contains("turned")) agent.MoveDirection = new UnityEngine.Vector2(0f, 1f); });
                bool ok;
                if (kase.Contains("turned")) ok = RefusedKeptPrior(c, r, "no-affordable-strike-position") && r.Decision.Contains("turnToTarget=45");
                else if (tick < 0.05f) ok = RefusedKeptPrior(c, r, "no-affordable-strike-position") && r.Decision.Contains("travel=0.1") && r.Decision.Contains("allowedMetres=0.1");
                else ok = r.Decision.StartsWith("committed:replacement:") && r.Decision.Contains("speed=6") && r.StopAfterDecision == Field(r.Decision, "stop") &&
                    Math.Abs(r.Displacement - ParseFloat(Field(r.Decision, "grantedMetres"))) < 1e-4f && Math.Abs(r.AfterDecision.MountMove - r.Displacement / 6f) < 1e-4f && r.Order.StruckTargets.Count == 1;
                Check(ok, "R8-F6 (round 9) " + kase + ": " + r.Decision + "; executed stop " + r.StopAfterDecision + ", displacement " + r.Displacement.ToString("0.####") + " m, mount Move " +
                    r.AfterDecision.MountMove.ToString("0.####") + " s; strikes " + (r.Order == null ? 0 : r.Order.StruckTargets.Count) + " (nominal 9 m/s travel at this tick would be " + (9f * tick).ToString("0.###") + " m)");
            }

            // ===== round 10 =====
            // R6-F7: the transfer fault fires AFTER the previous order's retirement mutations (stockIntent.Cancel, ledger cleared, ownership released);
            // the observation proves they ran, then the snapshot restores the previous generation exactly.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                w.Rider.HoldState = true;
                var gA = c.Combat.Generation; var selectionA = c.Combat.RiderSelectionGeneration;
                var r = RunProvisional(c, w.Target, null, () => MountedCombatController.FaultInTransfer = true);
                var seen = MountedCombatController.TransferFaultObservation ?? "";
                var mutated = seen.Contains("intent=False;generation=" + (gA + 1) + ";ledger=-1") && seen.Contains("riderSelectionGeneration=-1");
                Check(mutated && r.Decision.StartsWith("refused:commit-failed(InvalidOperationException);previousIntent=kept;generation=" + gA + "->" + gA) && r.GenAtDecision == gA &&
                    ReferenceEquals(r.IntentAtDecision, a) && ReferenceEquals(r.TargetAtDecision, a) && selectionA == gA && w.Rider.HoldState && w.Horse.HoldState && r.AfterDecision.MountMove == 0f &&
                    r.Order.IsFinished && LiveMountCarrier(c) == null,
                    "R6-F7 (round 10) transfer fault after the retirement mutations: at the fault " + seen + " (A was generation " + gA + ", selection owned by " + selectionA +
                    "); " + r.Decision + "; at the decision A restored: generation " + r.GenAtDecision + ", intent " + Name(r.IntentAtDecision) + ", manual target " + Name(r.TargetAtDecision) +
                    "; rider Hold " + w.Rider.HoldState + ", horse Hold " + w.Horse.HoldState + "; mount Move 0 s; B ended, mount Move slot empty");
            }
            // R6-F7: the slot cleanup fails after native completion was stamped (the wrapper and its carrier finished, the carrier still in the Move slot);
            // the next sweep finishes only that unfinished work; with an intervening successor it leaves the successor alone; repeated sweeps change nothing.
            foreach (var successorKind in new[] { "none", "Move", "queued Standard" })
            {
                var successorCase = successorKind != "none";
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                var gA = c.Combat.Generation;
                var armed = false;
                c.BeforeDrive = () => { if (!armed && c.Combat.LastFirstTickDecision.StartsWith("refused")) { armed = true; UnitCommands.RemoveFault = commands => commands == w.Horse.Commands; } };
                var r = RunProvisional(c, w.Target);
                c.BeforeDrive = null;
                var carrier = r.Carrier;
                var afterFault = "wrapper finished=" + r.Order.IsFinished + ", carrier finished=" + carrier.IsFinished + ", carrier still in Move slot=" +
                    ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move), carrier) + ", request owned=" + (c.Combat.FirstTickProposalForDiagnostics != null) +
                    ", cleanup failures=" + c.Combat.FirstTickCleanupFailureCount;
                var stopsBefore = w.Horse.View.StopMovingCount;
                var terminalsBefore = r.Order.TerminalReports;
                var holdBeforeSuccessor = w.Horse.HoldState;
                UnitCommand successor = null;
                if (successorKind == "Move") { successor = new UnitMoveTo(new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true }; w.Horse.Commands.Run(successor); }
                // Round 11: an out-of-range Standard queued on the mount, promoted by the retried slot cleanup itself (native steers the mount from it).
                var far = new UnitEntityData("hostile-far") { Position = new Vector3(-6f, 0f, 0f) };
                if (successorKind == "queued Standard") { successor = new HarnessPlayerCommand(far, UnitCommand.CommandType.Standard) { ApproachRadius = 0.1f }; w.Horse.Commands.Enqueue(successor); }
                for (var i = 0; i < 3; i++) c.Combat.SweepFirstTickProposal();
                var slot = successorKind == "queued Standard" ? w.Horse.Commands.Standard : w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                var stops = w.Horse.View.StopMovingCount - stopsBefore;
                var ok = afterFault == "wrapper finished=True, carrier finished=True, carrier still in Move slot=True, request owned=True, cleanup failures=1" &&
                    c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null && r.Order.TerminalReports == terminalsBefore &&
                    r.GenAtDecision == gA && ReferenceEquals(r.TargetAtDecision, a) && holdBeforeSuccessor && (successorCase || w.Horse.HoldState) && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                    !w.Horse.Commands.Contains(carrier) &&
                    (successorCase ? ReferenceEquals(slot, successor) && !successor.IsFinished && stops == 0 : slot == null && stops == 1);
                if (successorCase) { successor.Interrupt(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                ok &= Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f;
                Check(ok, "R6-F7 (round 11) slot cleanup failure after native completion" + (successorKind == "Move" ? ", then a successor in the Move slot" :
                    successorKind == "queued Standard" ? ", then an out-of-range Standard queued (promoted by the retried slot cleanup)" : "") + ": " + afterFault +
                    "; after three sweeps: " + (successorKind == "queued Standard" ? "Standard slot " : "Move slot ") + Id(slot) + ", carrier out of the slots " + !w.Horse.Commands.Contains(carrier) + ", motion stops issued " + stops + ", request released, no replayed terminal (reports " + r.Order.TerminalReports +
                    "); A generation " + r.GenAtDecision + " target " + Name(r.TargetAtDecision) + ", horse Hold " + holdBeforeSuccessor + " before any successor (" + w.Horse.HoldState + " after; a successor's own native Run clears it); a later ground tick moved " +
                    moved.ToString("0.####") + " m, debited " + debit.ToString("0.####") + " s");
            }

            // R8-F1 (round 11): first-debit revalidation with the FULL native arrival radius on a carrier whose ApproachPoint is the bent route's end.
            // The incoming leg passes 49.9 mm from that end (a 6.3 mm chord inside the 50 mm disc) and sight on that chord closes after binding,
            // while every final-heading disc sample stays admissible; the 50.1 mm leg never enters the disc.
            // Round 14 (R12-F3): run with the long grant (27 m) and with a short ordinary grant (1.8 m over the 1.0534 m route) at the first debit.
            foreach (var grantKase in new[] { "27 m grant", "1.8 m ordinary grant" })
            foreach (var kase in new[] { "49.9 mm, chord blind after binding", "49.9 mm, chord visible (control)", "50.1 mm, same blind spot (control)" })
            {
                var grazing = kase.StartsWith("49.9") ? 0.0499f : 0.0501f;
                var closed = !kase.Contains("visible");
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                // the vertex off the 1 cm centre grid, so the click's destination is the route's end itself
                var vertex = new Vector3(1.0035f, 0f, 0f); var end = new Vector3(1.0035f, 0f, grazing);
                agent.Route = (from, to) => new List<Vector3> { from, vertex, end };
                var q = new Vector3(1.0035f - 0.00266f, 0f, 0f);
                const float block = 0.0012f;
                var chordClosed = false;
                // sight only within 50.2 mm of the end (the 50 mm disc and its ring admit), and the chord point Q closed after binding
                LineOfSightGeometry.Instance.Blocked = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, end) > 0.0502f ||
                    chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, q) < block;
                var clearance = float.MaxValue;
                foreach (var ring in new[] { 0.05f, 0.025f })
                    for (var k = 0; k < 16; k++)
                    {
                        var sample = new Vector3(end.x + ring * (float)Math.Cos(k * Math.PI / 8d), 0f, end.z + ring * (float)Math.Sin(k * Math.PI / 8d));
                        clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, sample));
                    }
                var qInDisc = Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, end) < 0.05f;
                var r = RunProvisional(c, w.Target, null, () => { w.Horse.MoveRestricted = false; if (grantKase.StartsWith("1.8")) w.Horse.CombatState.Cooldown.MoveAction = 3f - 0.2f; chordClosed = closed; }, 40);
                var approachPoint = r.Carrier == null ? "none" : r.Carrier.ApproachPoint.ToString();
                var bound = r.Carrier != null && r.Carrier.ForcedPath.vectorPath.Count == 3 && Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Carrier.ApproachPoint, end) < 1e-5f &&
                    Math.Abs(r.Carrier.ApproachRadius - 0.05f) < 1e-6f;
                var ok = bound && clearance > block && block > PairedAttackClickProbe.InDiscSampleMetres && qInDisc == kase.StartsWith("49.9") &&
                    (kase.StartsWith("49.9 mm, chord blind")
                        ? RefusedKeptPrior(c, r, "route-arrival-not-admitted", false, grantKase.StartsWith("1.8") ? (float?)(3f - 0.2f) : null) && r.Decision.Contains("arrivalDisc=0.05;")
                        : r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Order.StruckTargets.Count == 1);
                ok &= grantKase.StartsWith("27") || r.Decision.StartsWith("refused:route-arrival-not-admitted") || r.Decision.Contains(";grant=1.");
                Check(ok, "R8-F1 (round 11) / R12-F3 (round 14) " + grantKase + " (provisional request: the prior order is owned until the decision), " + kase + ": carrier ApproachPoint " + approachPoint + " (bent route end " + end + ", radius " + (r.Carrier == null ? float.NaN : r.Carrier.ApproachRadius).ToString("R") +
                    "), blind spot " + q + " radius " + block.ToString("0.####") + " m (inside the disc " + qInDisc + ", clearance from every final-heading disc sample " + clearance.ToString("0.####") +
                    " m); " + r.Decision + "; displacement after the tick " + r.Displacement.ToString("0.####") + " m, mount Move " + r.AfterDecision.MountMove.ToString("0.####") + " s; strikes " +
                    (r.Order == null ? 0 : r.Order.StruckTargets.Count) + "; prior " + Name(r.TargetAtDecision) + ", horse Hold " + r.HoldAtDecision);
            }

            // R8-F6: the installed combat steering at the returned delta (engine stand-in run independently): a 10-degree turn the rule completes, a
            // sub-degree half-turn beyond a shortened tick's capacity; predicted and executed stop and direction, sight, charge and outcome.
            foreach (var kase in new[] { "10-degree heading, turn completed", "1.8-degree heading, half-turn beyond the shortened tick's capacity" })
            {
                var small = kase.StartsWith("1.8");
                var c = FirstTickPair(small ? 0.006f : 0.06f, 1f / 60f);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var waypointX = small ? 0.008f : 0.1f;
                agent.Route = (from, to) => new List<Vector3> { from, new Vector3(waypointX, 0f, 0f), to };
                var degrees = small ? 1.8 : 10.0;
                var r = RunProvisional(c, w.Target, null, () => agent.MoveDirection = new UnityEngine.Vector2((float)Math.Cos(degrees * Math.PI / 180d), (float)Math.Sin(degrees * Math.PI / 180d)));
                var predictedX = ParseFloat(Field(r.Decision, "stopX")); var predictedZ = ParseFloat(Field(r.Decision, "stopZ"));
                var turnNeeded = ParseFloat(Field(r.Decision, "turnToTarget")); var capacity = ParseFloat(Field(r.Decision, "turnCapacity"));
                var executed = r.StopAfterDecision;
                var executedX = 0f; var executedZ = 0f;
                // the executed stop: the horse position right after the decision tick (StopAfterDecision is its formatted value)
                var ok = r.Decision.StartsWith("committed:replacement:first-tick") && r.Decision.Contains("waypoint#1") &&
                    Math.Abs(r.AfterDecision.MountMove - ParseFloat(Field(r.Decision, "grantedMetres")) / 9f) < 1e-5f && r.Order.StruckTargets.Count == 1 &&
                    (small ? turnNeeded > capacity : turnNeeded <= capacity && turnNeeded > 1f);
                executedX = c.HorseAtDecision.x; executedZ = c.HorseAtDecision.z;
                ok &= Math.Abs(executedX - predictedX) < 1e-4f && Math.Abs(executedZ - predictedZ) < 1e-4f;
                Check(ok, "R8-F6 (round 10) " + kase + ": predicted stop (" + predictedX.ToString("0.#####") + "," + predictedZ.ToString("0.#####") + "), executed stop (" +
                    executedX.ToString("0.#####") + "," + executedZ.ToString("0.#####") + "); half-turn " + turnNeeded.ToString("0.###") + " deg vs capacity " + capacity.ToString("0.###") +
                    " deg at the returned delta; predicted direction " + Field(r.Decision, "direction") + ", executed direction (" + c.DirectionAtDecision.x.ToString("R") + "," + c.DirectionAtDecision.y.ToString("R") +
                    "); charged " + r.AfterDecision.MountMove.ToString("0.######") + " s; " + r.Decision.Substring(0, Math.Min(60, r.Decision.Length)) + "...; strikes " + r.Order.StruckTargets.Count);
            }

            // R9-F1: a real compatible assignment on that actor between a failed deferred target operation and its retry supersedes the operation;
            // the newer selection survives the retry and B's retirement. Rider (deferred write of B) and mount (deferred clear of A's target).
            foreach (var actorCase in new[] { "rider", "mount" })
                foreach (var assign in new[] { "different target", "same target", "control: no assignment" })
                {
                    var c = FirstTickPair(0.12f, 1f / 60f);
                    var a = RetainedOrderAndHold(c);
                    var w = c.World;
                    var gA = c.Combat.Generation;
                    if (actorCase == "mount") c.Combat.Own(w.Horse, a, gA);   // A's generation owns the mount's target too
                    var actor = actorCase == "rider" ? w.Rider : w.Horse;
                    var other = new UnitEntityData("hostile-newer") { Position = new Vector3(1.0f, 0f, -0.6f) };
                    other.View.Corpulence = 0.5f;
                    var same = actorCase == "rider" ? w.Target : a;
                    var newer = assign.StartsWith("different") ? other : assign.StartsWith("same") ? same : null;
                    var outstanding = "";
                    var assigned = false;
                    c.BeforeDrive = () =>
                    {
                        var settling = c.Combat.SettlingProposalForDiagnostics;
                        if (assigned || settling == null || settling.OutstandingEffects.Count == 0) return;
                        assigned = true;
                        outstanding = string.Join(",", settling.OutstandingEffects.ToArray());
                        if (newer == null) return;
                        var command = new HarnessPlayerCommand(newer, actorCase == "rider" ? UnitCommand.CommandType.Free : UnitCommand.CommandType.Swift);
                        actor.Commands.Run(command);   // native Run's UpdateCombatTarget: a real compatible assignment
                        command.FinishSuccess(); actor.Commands.RemoveFinishedAndUpdateQueue();
                    };
                    var r = RunProvisional(c, w.Target, null, () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == actor.CombatState);
                    c.BeforeDrive = null;
                    var afterRetry = actor.CombatState.ManualTarget;
                    c.Combat.Cancel("B retired");
                    var afterRetire = actor.CombatState.ManualTarget;
                    var expectedOutstanding = actorCase == "rider" ? "rider-target" : "mount-target";
                    var ok = r.Decision.StartsWith("committed:replacement:") && outstanding == expectedOutstanding && c.Combat.SettlingProposalForDiagnostics == null &&
                        (newer != null ? ReferenceEquals(afterRetry, newer) && ReferenceEquals(afterRetire, newer)
                            : actorCase == "rider" ? ReferenceEquals(afterRetry, w.Target) && afterRetire == null : afterRetry == null && afterRetire == null);
                    Check(ok, "R9-F1 (round 10) " + actorCase + ", " + assign + ": deferred operation failed (" + outstanding + "); after the retry the " + actorCase + "'s target is " + Name(afterRetry) +
                        ", after B's retirement " + Name(afterRetire) + (newer == null ? " (the retry applied B's own operation)" : " (the newer assignment survives)"));
                }

            // R9-F2: an ordinary stationary release requires the rider's current entitlement before the initial child starts, in both processing orders.
            foreach (var horseFirst in new[] { false, true })
            {
                var c = FirstTickPair(0.12f, 1f / 60f, false, false);
                var w = c.World;
                c.HorseFirst = horseFirst;
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);   // bound while entitled
                var startsBefore = UnitAttack.NativeAttackStarts;
                w.Rider.HasStandard = false;   // the rider's Standard is exhausted
                c.Opening[0] = -10f;           // the strike becomes spatially admissible before any movement
                for (var i = 0; i < 8 && order != null && !order.IsFinished; i++) Frame(c);
                Check(click.Admitted && order != null && order.IsFinished && order.Transaction.ChildAttackStartCount == 0 && UnitAttack.NativeAttackStarts == startsBefore &&
                    order.StruckTargets.Count == 0 && w.Horse.CombatState.Cooldown.MoveAction == 0f && c.Combat.LastFirstTickDecision.StartsWith("refused:rider-not-entitled"),
                    "R9-F2 (round 10) " + (horseFirst ? "horse-first" : "rider-first") + ": Standard exhausted after binding, strike admissible before movement: " + c.Combat.LastFirstTickDecision +
                    "; child starts " + order.Transaction.ChildAttackStartCount + ", native attack starts +" + (UnitAttack.NativeAttackStarts - startsBefore) + ", mount Move 0 s, order end " + order.EndCause);
            }

            // R9-F3/R9-F4: click, revalidation and debit price ordinary Move with the agent's own speed; a step stays a distance.
            {
                // 0.1 s left, nominal 9 m/s, agent 6 m/s: the real click for a 0.75 m route is refused (0.6 m physical); prior target and Hold kept.
                var c = FirstTickPair(0.75f, 1f / 60f, false, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.Horse.CombatState.Cooldown.MoveAction = 2.9f;
                w.Horse.View.AgentASP.SpeedFactor = 6f / 9f;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                Check(!click.Admitted && click.Order == null && LiveMountCarrier(c) == null && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState &&
                    Math.Abs(w.Horse.CombatState.Cooldown.MoveAction - 2.9f) < 1e-6f && Math.Abs(ParseFloat(Field(line, "mountGrantedMetres")) - 0.6f) < 1e-3f,
                    "R9-F4 (round 10) 0.1 s left, nominal 9 / agent 6 m/s, 0.75 m route: click refused (" + click.Decision + "; grant " + Field(line, "mountGrantedMetres") + " m, route " +
                    Field(line, "routeMetres") + " m); target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState + ", no carrier, mount Move 2.9 s");
            }
            {
                // faster agent (18 m/s), 0.1 s left: the 1.25 m route is affordable (1.8 m physical) and strikes.
                var c = FirstTickPair(1.2f, 1f / 60f, false, false, false, true);
                var w = c.World;
                w.Horse.CombatState.Cooldown.MoveAction = 2.9f;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                for (var i = 0; i < 20 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                Check(click.Admitted && order != null && order.StruckTargets.Count == 1 && w.Horse.Position.x > 1.19f && w.Horse.CombatState.Cooldown.MoveAction <= 3f + 1e-4f,
                    "R9-F4 (round 10) faster agent 18 m/s, 0.1 s left: click admitted (grant " + Field(line, "mountGrantedMetres") + " m, route " + Field(line, "routeMetres") + " m), first debit " +
                    c.Combat.LastFirstTickDecision.Substring(0, Math.Min(50, c.Combat.LastFirstTickDecision.Length)) + "..., arrived " + w.Horse.Position + ", one strike, mount Move " +
                    w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s");
            }
            {
                // a 2 m ordinary route bound, then a fresh 1.524 m step selected on the executing context with a faster agent: refused, nothing spent.
                var c = FirstTickPair(1.95f, 1f / 60f, false, false);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                c.Pair.MovementInputContextForHarness(c.Context).EnabledFiveFootStep = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                Check(click.Admitted && order != null && decision.StartsWith("refused:route-beyond-current-grant") && decision.Contains("grant=1.524") && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                    c.Pair.movementState.AllocationMetresStepped(w.Horse) == 0f && w.Horse.Position.x == 0f,
                    "R9-F3 (round 10) 2 m route switched to a fresh 1.524 m step with an 18 m/s agent before the first debit: " + decision + "; mount Move 0 s, stepped 0 m, no movement");
            }
            {
                // an affordable step with an 18 m/s agent: metres travelled equal metres charged to the step ledger, through every debit.
                var c = FirstTickPair(1.2f, 1f / 60f, false, false, true);
                var w = c.World;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                var start = w.Horse.Position;
                for (var i = 0; i < 20 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var travelled = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, w.Horse.Position);
                var stepped = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                var physical = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var time = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                Check(click.Admitted && order != null && order.StruckTargets.Count == 1 && Math.Abs(travelled - physical) < 1e-3f && Math.Abs(stepped - 9f * time) < 1e-4f &&
                    Math.Abs(stepped - travelled * 0.5f) < 1e-3f && physical <= 1.524f + 1e-4f && w.Horse.CombatState.Cooldown.MoveAction == 0f,
                    "R9-F3 (round 11) affordable straight step, nominal 9 / agent 18 m/s: travelled " + travelled.ToString("0.####") + " m = physical step ledger " + physical.ToString("0.####") +
                    " m (limit 1.524); native step counter " + stepped.ToString("0.####") + " m = CurrentSpeedMps 9 x " + time.ToString("0.#####") + " s; mount Move " +
                    w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s, one strike");
            }

            // R9-F5: a one-shot throwing MaxSpeed getter at the production movement entry is contained and refuses; a later ground move is lawful.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var r = RunProvisional(c, c.World.Target, null, () => Kingmaker.View.UnitMovementAgent.MaxSpeedFault = true);
                float debit; string line;
                var moved = GroundTick(c, 0.15f, out debit, out line);
                Check(RefusedKeptPrior(c, r, "agent-speed-unreadable") && !Kingmaker.View.UnitMovementAgent.MaxSpeedFault && Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f,
                    "R9-F5 (round 10) throwing MaxSpeed getter at the first debit: " + r.Decision + "; displacement 0 m, mount Move 0 s, prior " + Name(r.TargetAtDecision) + " and Hold " + r.HoldAtDecision +
                    " kept, gate and carrier released; a later ground tick moved " + moved.ToString("0.####") + " m, debited " + debit.ToString("0.####") + " s");
            }

            // ===== round 11 =====
            // R9-F3: a slower-than-nominal step (nominal 9, agent 6 m/s): the installed counter (CurrentSpeedMps x time) limits it to 1.524 / 9 s, i.e.
            // 1.016 m of travel. A 1.2 m route fits only the physical 1.524 m and is refused at the real click (prior target and Hold kept); a 0.9 m
            // route fits both and is struck, with the returned time, the actual travel and the native step units recorded at every debit.
            foreach (var routeMetres in new[] { 1.2f, 0.9f })
            {
                var c = FirstTickPair(routeMetres - 0.05f, 1f / 60f, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.Horse.View.AgentASP.SpeedFactor = 6f / 9f;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                var debits = new List<string>();
                var before = w.Horse.Position; var sumDisplacement = 0f;
                for (var i = 0; i < 30 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                {
                    var t0 = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                    Frame(c);
                    var moved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, w.Horse.Position); before = w.Horse.Position; sumDisplacement += moved;
                    var t1 = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                    if (t1 > t0) debits.Add("dt=" + (t1 - t0).ToString("0.#####") + "s,moved=" + moved.ToString("0.####") + "m,native=" + c.Pair.movementState.AllocationMetresStepped(w.Horse).ToString("0.####") +
                        "m,physical=" + c.Pair.movementState.AllocationMetresStepTravelled(w.Horse).ToString("0.####") + "m");
                }
                var grant = ParseFloat(Field(line, "mountGrantedMetres"));
                var native = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                var physical = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var time = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                var ok = Math.Abs(grant - 1.524f * 6f / 9f) < 1e-3f && (routeMetres > 1.1f
                    ? !click.Admitted && order == null && LiveMountCarrier(c) == null && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState && native == 0f && w.Horse.Position.x == 0f
                    : click.Admitted && order != null && order.StruckTargets.Count == 1 && Math.Abs(physical - sumDisplacement) < 1e-3f && Math.Abs(native - 9f * time) < 1e-4f &&
                      native <= 1.524f + 1e-4f && physical <= 6f * time + 1e-4f && debits.Count >= 2);
                Check(ok, "R9-F3 (round 11) slower step, nominal 9 / agent 6 m/s, " + routeMetres.ToString("0.##") + " m route: click " + (click.Admitted ? "admitted" : "refused") + " (grant " +
                    Field(line, "mountGrantedMetres") + " m = 1.524 / 9 s x 6 m/s; route " + Field(line, "routeMetres") + " m); " + (routeMetres > 1.1f
                        ? "target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState + ", no carrier, no step spent"
                        : "debits [" + string.Join("; ", debits.ToArray()) + "]; travelled " + sumDisplacement.ToString("0.####") + " m = physical ledger " + physical.ToString("0.####") +
                          " m; native counter " + native.ToString("0.####") + " m = 9 x " + time.ToString("0.#####") + " s (limit 1.524); strikes " + order.StruckTargets.Count));
            }
            // R9-F3: a turning step (a bend mid-route, away from the endpoint correction) with an 18 m/s agent: the physical ledger is the steered
            // displacement, below MaxSpeed x time while the mount turns; the native counter is CurrentSpeedMps x time.
            {
                var c = FirstTickPair(0.9f, 1f / 60f, false, false, true);
                var w = c.World;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                w.Horse.View.AgentASP.Route = (from, to) => new List<Vector3> { from, new Vector3(0.4f, 0f, 0.25f), new Vector3(0.7f, 0f, 0f), to };
                var click = ClickTick(c, w.Target);
                var clickLine = LastLine(c);
                var order = click.Order;
                var debits = new List<string>();
                var before = w.Horse.Position; var sumDisplacement = 0f;
                for (var i = 0; i < 30 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                {
                    var t0 = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                    Frame(c);
                    var moved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, w.Horse.Position); before = w.Horse.Position; sumDisplacement += moved;
                    var t1 = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                    if (t1 > t0) debits.Add("dt=" + (t1 - t0).ToString("0.#####") + ",moved=" + moved.ToString("0.####") + ",bound=" + (18f * (t1 - t0)).ToString("0.####"));
                }
                var native = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                var physical = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var time = c.Pair.movementState.AllocationTimeStepped(w.Horse);
                Check(click.Admitted && order != null && order.StruckTargets.Count == 1 && Math.Abs(physical - sumDisplacement) < 1e-3f && physical < 18f * time - 0.01f &&
                    Math.Abs(native - 9f * time) < 1e-4f && physical <= 1.524f + 1e-4f && native <= 1.524f + 1e-4f,
                    "R9-F3 (round 11) turning step, nominal 9 / agent 18 m/s: click " + click.Decision + " {grant " + Field(clickLine, "mountGrantedMetres") + ", route " + Field(clickLine, "routeMetres") + ", probe " + Field(clickLine, "probe") + "}; debits [" + string.Join("; ", debits.ToArray()) + "]; steered travel " + sumDisplacement.ToString("0.####") +
                    " m = physical ledger " + physical.ToString("0.####") + " m < MaxSpeed x time " + (18f * time).ToString("0.####") + " m; native counter " + native.ToString("0.####") +
                    " m = 9 x " + time.ToString("0.#####") + " s; strikes " + order.StruckTargets.Count);
            }

            // R10-F1: the restricted tick resolves the waypoint and the returned delta together. Current agent speed (m_Speed) 6, MaxSpeed 9, 1/60 s,
            // an aligned interior waypoint 0.08 m ahead: at the returned 0.00889 s native's skip threshold is 0.053 m, so the waypoint is kept and the
            // tick stops there. Control: turned 60 degrees with m_Speed 9 an interior waypoint 0.1 m ahead IS skipped at its own returned delta, and
            // native steers to the next point (the prediction follows it).
            foreach (var kase in new[] { "aligned 0.08 m interior waypoint, m_Speed 6", "control: turned 60 degrees, 0.1 m interior waypoint skipped, m_Speed 9" })
            {
                var control = kase.StartsWith("control");
                var c = FirstTickPair(0.06f, 1f / 60f);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var waypointX = control ? 0.1f : 0.08f;
                agent.Route = (from, to) => new List<Vector3> { from, new Vector3(waypointX, 0f, 0f), to };
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    agent.CurrentSpeedField = control ? 9f : 6f;
                    if (control) agent.MoveDirection = new UnityEngine.Vector2(0.5f, (float)Math.Sqrt(0.75));
                });
                var predictedX = ParseFloat(Field(r.Decision, "stopX")); var predictedZ = ParseFloat(Field(r.Decision, "stopZ"));
                var delta = ParseFloat(Field(r.Decision, "delta"));
                var ok = r.Decision.StartsWith("committed:replacement:first-tick") && r.Decision.Contains("cursor=1;waypoint#" + (control ? "2" : "1") + "=") &&
                    Math.Abs(c.HorseAtDecision.x - predictedX) < 1e-4f && Math.Abs(c.HorseAtDecision.z - predictedZ) < 1e-4f &&
                    Math.Abs(r.AfterDecision.MountMove - delta) < 1e-5f && r.Order.StruckTargets.Count == 1 &&
                    (control ? waypointX < 9f * delta : Math.Abs(delta - 0.08f / 9f) < 1e-5f && waypointX >= 6f * delta);
                Check(ok, "R10-F1 (round 11) " + kase + ": " + r.Decision.Substring(0, Math.Min(r.Decision.IndexOf(";segment=") > 0 ? r.Decision.IndexOf(";segment=") : 80, r.Decision.Length)) +
                    "; returned delta " + delta.ToString("0.######") + " s (skip threshold m_Speed x delta " + ((control ? 9f : 6f) * delta).ToString("0.####") + " m vs waypoint " + waypointX.ToString("0.###") +
                    " m); predicted stop (" + predictedX.ToString("0.#####") + "," + predictedZ.ToString("0.#####") + "), executed (" + c.HorseAtDecision.x.ToString("0.#####") + "," + c.HorseAtDecision.z.ToString("0.#####") +
                    "); debit " + r.AfterDecision.MountMove.ToString("0.######") + " s; strikes " + r.Order.StruckTargets.Count);
            }

            // R10-F2: one ordinary tick left (1/60 s at 9 m/s; the frame is 1/50 s), the arrival 0.145 m ahead, sight from x = 0.095 m. The click is a
            // provisional request (its grant ends within one tick at the frame's delta); the carrier's first debit predicts the decisive final tick with
            // the installed steering. Aligned: the tick reaches the arrival, one strike. Perpendicular: the tick stops at (0.075, 0.075), blind: refused
            // before any debit, prior target and Hold kept. R10-F5: the perpendicular row reports the raised last-segment capacity.
            foreach (var heading in new[] { "aligned", "perpendicular" })
            {
                var c = FirstTickPair(0.095f, 1f / 50f, false, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.Horse.CombatState.Cooldown.MoveAction = 3f - 1f / 60f;
                var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                var gBefore = c.Combat.Generation;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                // Round 13 (amendment): the click is native-equivalent admission, an accepted order (generation and target written at acceptance).
                var pending = click.Admitted && order != null && !order.ProvisionalOrder && c.Combat.Generation != gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target);
                var gAccepted = c.Combat.Generation;
                Frame(c);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var bound = p != null && !p.Replacement && p.CarrierMove != null && ReferenceEquals(c.Pair.movementState.FirstTickGate, p);
                if (heading == "perpendicular") w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                var start = w.Horse.Position;
                string decision = null; Vector3 stop = start; UnityEngine.Vector2 direction = default(UnityEngine.Vector2); UnitEntityData targetAtDecision = null; bool holdAtDecision = false;
                c.BeforeDrive = () =>
                {
                    if (decision != null || c.Combat.LastFirstTickDecision.StartsWith("pending")) return;
                    decision = c.Combat.LastFirstTickDecision; stop = w.Horse.Position; direction = w.Horse.View.AgentASP.MoveDirection;
                    targetAtDecision = w.Rider.CombatState.ManualTarget; holdAtDecision = w.Horse.HoldState;
                };
                for (var i = 0; i < 3 && decision == null; i++) Frame(c);
                c.BeforeDrive = null;
                var moveAfterDecision = w.Horse.CombatState.Cooldown.MoveAction;
                for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                decision = decision ?? "<none>";
                var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, stop);
                bool ok;
                if (heading == "aligned")
                    ok = pending && bound && decision.StartsWith("committed:order:final-ordinary-tick:first-tick(") && stop.ToString() == Field(decision, "stop") &&
                        Math.Abs(moveAfterDecision - moveBefore - ParseFloat(Field(decision, "grantedMetres")) / 9f) < 1e-5f && order.StruckTargets.Count == 1 &&
                        ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target);
                else
                    ok = pending && bound && decision.StartsWith("refused:final-ordinary-tick-no-affordable-strike-position(") && travel < 1e-6f && moveAfterDecision == moveBefore &&
                        w.Horse.CombatState.Cooldown.MoveAction == moveBefore && !ReferenceEquals(targetAtDecision, a) && !ReferenceEquals(w.Rider.CombatState.ManualTarget, a) &&
                        c.Combat.Generation >= gAccepted && order != null && order.IsFinished && order.StruckTargets.Count == 0 && LiveMountCarrier(c) == null &&
                        Math.Abs(ParseFloat(Field(decision, "stopX")) - 0.075f) < 1e-3f && Math.Abs(ParseFloat(Field(decision, "stopZ")) - 0.075f) < 1e-3f &&
                        ParseFloat(Field(decision, "turnCapacity")) >= 45f && ParseFloat(Field(decision, "baseTurnCapacity")) < 45f;
                Check(ok, "R10-F2 (round 13 expectation) one ordinary tick left, " + heading + " heading: click " + (pending ? "accepted order (generation " + gBefore + " -> " + gAccepted + ", grant " + Field(line, "mountGrantedMetres") + " m, route " +
                    Field(line, "routeMetres") + " m)" : "NOT accepted: " + click.Decision + " / " + line) + "; " + decision + "; executed stop " + stop + " (travel " + travel.ToString("0.####") +
                    " m, direction " + direction + "); mount Move " + moveBefore.ToString("0.#####") + " -> " + moveAfterDecision.ToString("0.#####") + " s; target " + Name(w.Rider.CombatState.ManualTarget) +
                    ", horse Hold " + w.Horse.HoldState + "; strikes " + (order == null ? 0 : order.StruckTargets.Count) +
                    (heading == "perpendicular" ? "; R10-F5 turn capacity " + Field(decision, "turnCapacity") + " deg (raised; base " + Field(decision, "baseTurnCapacity") + " deg) for the predicted direction " + Field(decision, "direction") : ""));
            }

            // R10-F3: an accepted route-bound order whose first debit committed while the rider was entitled; the rider's Standard is exhausted during
            // the remaining approach and the strike then becomes admissible: no child starts and nothing is spent, in both processing orders; the
            // entitled control strikes once.
            foreach (var horseFirst in new[] { false, true })
                foreach (var exhaust in new[] { true, false })
                {
                    var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                    var w = c.World;
                    c.HorseFirst = horseFirst;
                    var click = ClickTick(c, w.Target);
                    var order = click.Order;
                    for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                    var decision = c.Combat.LastFirstTickDecision;
                    var startsBefore = UnitAttack.NativeAttackStarts;
                    var mountStandardBefore = w.Horse.CombatState.Cooldown.StandardAction;
                    var xAtExhaust = w.Horse.Position.x;
                    if (exhaust) w.Rider.HasStandard = false;
                    for (var i = 0; i < 20 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    var ok = click.Admitted && order != null && decision.StartsWith("committed:order:ordinary-movement") && xAtExhaust > 0f && xAtExhaust < 1.2f && w.Horse.Position.x >= 1.2f - 1e-3f &&
                        (exhaust
                            ? order.IsFinished && order.Transaction.ChildAttackStartCount == 0 && UnitAttack.NativeAttackStarts == startsBefore && order.StruckTargets.Count == 0 &&
                              c.Combat.LastFirstTickDecision.StartsWith("refused-at-first-child:rider-not-entitled") && c.Combat.FirstChildEntitlementRefusalCount == 1 &&
                              w.Horse.CombatState.Cooldown.StandardAction == mountStandardBefore
                            : order.StruckTargets.Count == 1 && c.Combat.FirstChildEntitlementRefusalCount == 0);
                    Check(ok, "R10-F3 (round 11) " + (horseFirst ? "horse-first" : "rider-first") + ", " + (exhaust ? "rider Standard exhausted after the committed first debit" : "control, rider entitled") +
                        ": first debit " + decision.Substring(0, Math.Min(40, decision.Length)) + "...; mount at x=" + xAtExhaust.ToString("0.###") + " when the Standard " + (exhaust ? "was exhausted" : "stayed") +
                        ", arrived x=" + w.Horse.Position.x.ToString("0.###") + "; " + c.Combat.LastFirstTickDecision.Substring(0, Math.Min(60, c.Combat.LastFirstTickDecision.Length)) + "; child starts " +
                        (order == null ? -1 : order.Transaction.ChildAttackStartCount) + ", native attack starts +" + (UnitAttack.NativeAttackStarts - startsBefore) + ", strikes " + (order == null ? 0 : order.StruckTargets.Count) +
                        ", mount Standard " + w.Horse.CombatState.Cooldown.StandardAction.ToString("0.#"));
                }

            // R10-F4: the retired generation's mount-target clear fails at the commit; B then ends before the first retry (cancelled, or its target
            // dies and its wrapper ends by its own target-invalidation callback). A's owned selection still clears with no intervening assignment; an
            // actual newer assignment on the mount survives.
            foreach (var ending in new[] { "B cancelled", "B's target invalidated" })
                foreach (var assign in new[] { "control: no assignment", "newer assignment" })
                {
                    var c = FirstTickPair(0.12f, 1f / 60f);
                    var a = RetainedOrderAndHold(c);
                    var w = c.World;
                    var gA = c.Combat.Generation;
                    c.Combat.Own(w.Horse, a, gA);
                    var other = new UnitEntityData("hostile-newer") { Position = new Vector3(1.0f, 0f, -0.6f) };
                    other.View.Corpulence = 0.5f;
                    var outstanding = ""; var handled = false; var bCurrentAtRetry = true; UnitEntityData mountTargetBeforeRetry = null;
                    c.BeforeDrive = () =>
                    {
                        var settling = c.Combat.SettlingProposalForDiagnostics;
                        if (handled || settling == null || settling.OutstandingEffects.Count == 0) return;
                        handled = true;
                        outstanding = string.Join(",", settling.OutstandingEffects.ToArray());
                        if (ending == "B cancelled") c.Combat.Cancel("B cancelled before the retry");
                        if (assign == "newer assignment")
                        {
                            var command = new HarnessPlayerCommand(other, UnitCommand.CommandType.Swift);
                            w.Horse.Commands.Run(command);
                            command.FinishSuccess(); w.Horse.Commands.RemoveFinishedAndUpdateQueue();
                        }
                        bCurrentAtRetry = c.Combat.HasStockAttackIntent && c.Combat.Generation == settling.CommittedGeneration && ReferenceEquals(c.Combat.IntentTarget, w.Target);
                        mountTargetBeforeRetry = w.Horse.CombatState.ManualTarget;
                    };
                    var r = RunProvisional(c, w.Target, null, () =>
                    {
                        Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState;
                        if (ending != "B cancelled") c.ActionStage = () => w.Target.Descriptor.State.IsDead = true;
                    });
                    c.BeforeDrive = null;
                    var afterRetry = w.Horse.CombatState.ManualTarget;
                    var ok = r.Decision.StartsWith("committed:replacement:") && outstanding == "mount-target" && !bCurrentAtRetry && c.Combat.SettlingProposalForDiagnostics == null &&
                        (assign == "newer assignment" ? ReferenceEquals(mountTargetBeforeRetry, other) && ReferenceEquals(afterRetry, other)
                            : ReferenceEquals(mountTargetBeforeRetry, a) && afterRetry == null);
                    Check(ok, "R10-F4 (round 11) " + ending + ", " + assign + ": deferred clear of A's mount target failed (" + outstanding + "); B current at the retry " + bCurrentAtRetry +
                        "; mount target before the retry " + Name(mountTargetBeforeRetry) + ", after " + Name(afterRetry) + (assign == "newer assignment" ? " (the newer assignment survives)" : " (A's owned selection cleared)"));
                }

            // ===== round 12 =====
            // R6-F7 / R11-F7: the order's wrapper is ended by its own target-invalidation callback (native interruption) and its carrier cleanup faults at
            // the motion stop (View.StopMoving) after the carrier was interrupted and removed. Provisional (refused request: the sweep resumes it) and
            // accepted after its committed first debit (terminal settlement retains and resumes it). The agent's own path ownership is recorded at the
            // fault and after the retry, independently of any command's Result; the later ground move runs through the same mover.
            foreach (var accepted in new[] { false, true })
                foreach (var successorKind in new[] { "none", "Move" })
                {
                    var c = accepted ? FirstTickPair(1.2f, 1f / 60f, false, false) : FirstTickPair(1.2f, 1f / 60f);
                    var a = PriorSelectionAndHold(c);
                    var w = c.World;
                    MountedPairAttackCommand wrapper = null; UnitMoveTo carrier = null; UnitMoveTo successor = null;
                    var atFault = ""; var stopsBefore = 0; var reportsAtFault = -1; var releasedAtFault = true; var retainedAtFault = -1;
                    PathSnap oldAtFault = null, successorBefore = null;
                    string decision;
                    c.BeforeDrive = () =>
                    {
                        if (atFault != "" || wrapper == null || !wrapper.IsFinished) return;
                        releasedAtFault = OldPathReleased(c, carrier);
                        retainedAtFault = c.Combat.RetainedCarrierCleanupCount;
                        atFault = "wrapper finished=" + wrapper.IsFinished + ", end " + wrapper.EndCause + ", carrier finished=" + carrier.IsFinished + ", carrier in Move slot=" + w.Horse.Commands.Contains(carrier) +
                            ", carrier cleanup {" + wrapper.CarrierCleanupState + "}, agent path owner " + Id(w.Horse.View.AgentASP.PathOwner) + " (the carrier " + Id(carrier) + ", path " +
                            (w.Horse.View.AgentASP.Path == null ? "none" : w.Horse.View.AgentASP.Path.vectorPath.Count + " points") + ")";
                        stopsBefore = w.Horse.View.StopMovingCount; reportsAtFault = wrapper.TerminalReports;
                        oldAtFault = Snap(w.Horse);
                        if (successorKind == "Move")
                        {
                            var at = w.Horse.Position;
                            successor = new UnitMoveTo(at + new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(-0.5f, 0f, 0f) }) };
                            w.Horse.Commands.Run(successor);
                            // Round 13 (R6-F7 evidence): the successor's own native approach takes its path and cursor NOW, before any cleanup retry.
                            TakePath(w.Horse, successor);
                            successorBefore = Snap(w.Horse);
                        }
                    };
                    if (accepted)
                    {
                        var click = ClickTick(c, w.Target);
                        wrapper = click.Order;
                        for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                        decision = c.Combat.LastFirstTickDecision;
                        carrier = wrapper?.DelegatedMove;
                        Frame(c);   // the remaining approach after the committed first debit
                        Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                        c.ActionStage = () => w.Target.Descriptor.State.IsDead = true;
                        for (var i = 0; i < 3 && atFault == ""; i++) Frame(c);
                    }
                    else
                    {
                        var r = RunProvisional(c, w.Target, null, () =>
                        {
                            wrapper = c.Combat.FirstTickProposalForDiagnostics?.Command; carrier = c.Combat.FirstTickProposalForDiagnostics?.CarrierMove;
                            Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                            c.ActionStage = () => w.Target.Descriptor.State.IsDead = true;
                        });
                        decision = r.Decision;
                    }
                    c.BeforeDrive = null;
                    var stops = w.Horse.View.StopMovingCount - stopsBefore;
                    var slot = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                    for (var i = 0; i < 2; i++) c.Combat.SweepFirstTickProposal();
                    // Inspected immediately after every retry, before any helper can write the agent's path.
                    var afterRetries = Snap(w.Horse);
                    var successorKept = successor != null && SuccessorPathKept(successorBefore, afterRetries, successor);
                    // Negative controls: the same assertion rejects a cleared path and the old carrier's path retained.
                    var clearedControl = successor != null && SuccessorPathKept(successorBefore, new PathSnap { Path = null, Owner = null, Points = "none", Cursor = successorBefore.Cursor, Stops = successorBefore.Stops + 1 }, successor);
                    var wrongControl = successor != null && SuccessorPathKept(successorBefore, oldAtFault, successor);
                    var releasedAfter = OldPathReleased(c, carrier);
                    var ownerAfter = w.Horse.View.AgentASP.PathOwner;
                    var ok = atFault.StartsWith("wrapper finished=True") && atFault.Contains("carrier in Move slot=False") && atFault.Contains("interrupted=True,slotRemoved=True,motionSettled=False") &&
                        !releasedAtFault && releasedAfter && Kingmaker.View.UnitEntityView.StopMovingFault == null && c.Combat.FirstTickProposalForDiagnostics == null &&
                        c.Pair.movementState.FirstTickGate == null && c.Combat.RetainedCarrierCleanupCount == 0 && (accepted ? retainedAtFault == 1 : true) &&
                        wrapper.CarrierCleanupState == "interrupted=True,slotRemoved=True,motionSettled=True" && wrapper.TerminalReports == reportsAtFault && reportsAtFault == 1 &&
                        (accepted ? decision.StartsWith("committed:order:ordinary-movement") : decision.StartsWith("refused:")) &&
                        (successorKind == "Move" ? ReferenceEquals(slot, successor) && !successor.IsFinished && stops == 0 && ReferenceEquals(ownerAfter, successor) && successorKept && !clearedControl && !wrongControl
                            : slot == null && stops == 1 && ownerAfter == null);
                    if (successor != null) { successor.Interrupt(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                    var moveBeforeGround = w.Horse.CombatState.Cooldown.MoveAction;
                    float debit; string groundLine;
                    var moved = MoverGroundTick(c, 0.15f, out debit, out groundLine);
                    ok &= Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f;
                    Check(ok, "R6-F7/R11-F7 (round 13 evidence) " + (accepted ? "accepted order after its committed first debit" : "refused provisional order") + ", wrapper ended by its target-invalidation callback, StopMoving fault" +
                        (successorKind == "Move" ? ", then a Move successor that took its own path before the retry: before the retries {" + successorBefore + "}, after all retries {" + afterRetries +
                            "}; kept " + successorKept + "; negative controls (cleared path " + clearedControl + ", old carrier path retained " + wrongControl + ")" : "") + ": at the fault " + atFault + "; negative control: the old-path-released assertion at the fault is " + releasedAtFault +
                        (accepted ? "; retained by terminal settlement " + retainedAtFault : "") + "; after the retry: carrier cleanup {" + (wrapper == null ? "-" : wrapper.CarrierCleanupState) + "}, Move slot " + Id(slot) +
                        ", agent path owner " + Id(ownerAfter) + " (old path released " + releasedAfter + "), motion stops issued " + stops + ", terminal reports " + (wrapper == null ? -1 : wrapper.TerminalReports) +
                        "; first decision " + decision.Substring(0, Math.Min(50, decision.Length)) + "...; prior target " + Name(w.Rider.CombatState.ManualTarget) + "; later ground move through the mover: displacement " +
                        moved.ToString("0.####") + " m, one debit " + debit.ToString("0.####") + " s (mount Move " + moveBeforeGround.ToString("0.####") + " -> " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s)");
                }

            // R10-F2 (round 13, owner amendment 2026-11-27g section 4): the variable-frame examples as EXECUTION and ACCOUNTING controls. A short
            // ordinary click is an accepted order (native-equivalent admission, no forecast of later frames); only a genuinely single-tick allowance is
            // decided before its debit. One tick left (1/60 s at 9 m/s, 0.145 m route, 0.02 s debit frame): aligned strikes; perpendicular is refused
            // before any debit (no movement, debit or strike; the prior order is not resurrected). The review's schedules (perpendicular start, 1/30 s
            // left, 0.23 m endpoint, sight from x = 0.18): four 1/120 s ticks reach a visible stop and strike; 1/120 then 1/40 s ends blind with the
            // allowance exhausted; round 14 (R13-F4, the installed trace): the next tick grants no movement, CanMoveInTurnBased completes the agent's
            // movement (WantsToMove false) and TickCommandTurnBased force-finishes the approach that is not close enough: the order ends without a
            // strike, a Standard or a further carrier, and the player's newer ground order then holds the mount's Move slot; a later NearTheEnd tick
            // takes native's borrowed time (installed TickMovement skips its clamp then). Every executed tick is recorded.
            foreach (var kase in new[] { "one tick left, aligned", "one tick left, perpendicular", "1/30 s left, 4 x 1/120 s", "1/30 s left, 1/120 then 1/40 s", "1/30 s left, 1/120 then NearTheEnd 1/30 s" })
            {
                var oneTick = kase.StartsWith("one");
                var c = oneTick ? FirstTickPair(0.095f, 0.02f, false, false, false, true) : FirstTickPair(0.18f, 1f / 120f, false, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.NativeDenialCompletesMovement = !oneTick;   // the one-tick cases are decided before any movement (no exhaustion tick follows)
                w.Horse.CombatState.Cooldown.MoveAction = 3f - (oneTick ? 1f / 60f : 1f / 30f);
                var perpendicular = !kase.EndsWith("aligned");
                var nearEndCase = kase.Contains("NearTheEnd");
                float[] schedule = oneTick ? null : kase.Contains("4 x") ? new[] { 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f } :
                    nearEndCase ? new[] { 1f / 120f, 1f / 30f, 1f / 60f, 1f / 60f } : new[] { 1f / 120f, 1f / 40f, 1f / 60f, 1f / 60f };
                Action<int> beforeTick = nearEndCase ? (Action<int>)(i => w.Horse.View.AgentASP.NearTheEnd = i == 1) : null;
                var r = ShortOrdinaryRun(c, a, perpendicular ? (Action)(() => w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f)) : null, 6, schedule, beforeTick);
                w.Horse.View.AgentASP.NearTheEnd = false;
                var waiting = r.Order != null && !r.Order.IsFinished;
                var disposition = r.Strikes == 1 ? "strike" : waiting ? "waiting (order and carrier live, zero-delta ticks)" : "ended (" + (r.Order == null ? "-" : r.Order.EndCause.ToString() +
                    "; native force-finish disposition " + r.Order.NativeApproachEndedCount + "; route revalidations " + r.Order.RouteRevalidationCount + "; mount agent wants to move " + w.Horse.View.AgentASP.WantsToMove) + ")";
                var common = r.Pending && r.Bound && r.Monotonic && r.Strikes <= 1 && (r.Strikes == 1 || r.RiderStandardFinal == r.RiderStandardBefore) &&
                    !ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && r.GenAtDecision == r.GenAtClick;
                bool ok; var successorDetail = "";
                if (kase == "one tick left, aligned")
                    ok = common && r.Decision.StartsWith("committed:order:final-ordinary-tick:") && r.Strikes == 1 && r.MoveFinal <= 3f + 1e-5f;
                else if (kase == "one tick left, perpendicular")
                    ok = common && r.Decision.StartsWith("refused:final-ordinary-tick-no-affordable-strike-position") && Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Start, r.Stop) < 1e-6f &&
                        r.MoveAfterDecision == r.MoveBefore && r.MoveFinal == r.MoveBefore && r.Strikes == 0 && r.Order != null && r.Order.IsFinished && LiveMountCarrier(c) == null;
                else if (kase.Contains("4 x"))
                    ok = common && r.Decision.StartsWith("committed:order:ordinary-movement") && r.Strikes == 1 && r.MoveFinal <= 3f + 1e-5f && w.Horse.Position.x >= 0.18f;
                else if (nearEndCase)
                    ok = common && r.Decision.StartsWith("committed:order:ordinary-movement") && Math.Abs(r.MoveFinal - (3f + 1f / 120f)) < 1e-4f;
                else
                {
                    ok = common && r.Decision.StartsWith("committed:order:ordinary-movement") && r.Strikes == 0 && Math.Abs(r.MoveFinal - 3f) < 1e-5f && w.Horse.Position.x < 0.18f &&
                        r.Order != null && r.Order.IsFinished && r.Order.NativeApproachEndedCount == 1 && r.Order.RouteRevalidationCount == 1 && r.Order.EndCause == MountedAttackEndCause.Cancelled &&
                        LiveMountCarrier(c) == null && !w.Horse.View.AgentASP.WantsToMove;
                    // the player's newer ground order on the mount after the exhaustion
                    var at = w.Horse.Position; var moveBeforeSuccessor = w.Horse.CombatState.Cooldown.MoveAction;
                    var successor = new UnitMoveTo(at + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, -0.5f) }) };
                    w.Horse.Commands.Run(successor);
                    Frame(c); Frame(c);
                    var slot = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                    ok &= ReferenceEquals(slot, successor) && !successor.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == moveBeforeSuccessor && (r.Order == null || r.Order.StruckTargets.Count == 0);
                    successorDetail = "; the player's newer ground order then holds the Move slot " + ReferenceEquals(slot, successor) + " (interrupted " + successor.IsFinished + "), no further debit (mount Move " +
                        w.Horse.CombatState.Cooldown.MoveAction.ToString("0.#####") + " s), old order " + (r.Order == null ? "-" : r.Order.IsFinished ? "ended" : "live");
                }
                Check(ok, "R10-F2 (round 13 execution/accounting control; round 14 R13-F4 disposition) " + kase + ": click accepted " + r.Pending + " (generation " + r.GenBefore + " -> " + r.GenAtClick + ", target " + Name(r.TargetAtClick) +
                    ", grant " + Field(r.ClickLine, "mountGrantedMetres") + " m, route " + Field(r.ClickLine, "routeMetres") + " m), gate bound " + r.Bound + "; first debit " + r.Decision.Substring(0, Math.Min(140, r.Decision.Length)) +
                    "; first executed stop (" + r.Stop.x.ToString("0.#####") + "," + r.Stop.z.ToString("0.#####") + "); ticks [" + string.Join("; ", r.Ticks.ToArray()) + "]; mount Move " + r.MoveBefore.ToString("0.#####") + " -> " +
                    r.MoveAfterDecision.ToString("0.#####") + " -> " + r.MoveFinal.ToString("0.#####") + " s (never decreasing " + r.Monotonic + "); rider Standard " + r.RiderStandardBefore + " -> " + r.RiderStandardFinal +
                    "; target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState + " (decision: " + r.HoldAtDecision + "); disposition " + disposition + "; strikes " + r.Strikes + successorDetail);
            }

            // R13-F3 (round 14): after a lawful first debit the accepted ordinary turn-based order's carrier finishes out of sight of the stationary
            // target (sight closes in a band around its endpoint after the commit) with movement left and another visible stretch further along the
            // route. Installed disposition (R13-F4 trace): one issued turn-based path, no re-path; the approach that is not close enough and whose
            // agent no longer moves is force-finished (no strike, no Standard). The mounted order ends the same way: no second carrier or route
            // revalidation, no further debit, no strike, the rider's Standard unspent, and the player's newer ground order then owns the mount's Move
            // slot. Control: the carrier finishes visible and the order strikes.
            foreach (var kase in new[] { "carrier finished out of sight, another path available", "control: carrier finished in sight" })
            {
                var closedCase = !kase.StartsWith("control");
                var c = FirstTickPair(0.6f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                var endpoint = Vector3.zero; var closed = false;
                LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.6f || closed && Math.Abs(from.x - endpoint.x) < 0.12f;
                c.BeforeDrive = () => { if (closedCase && !closed && c.Combat.LastFirstTickDecision.StartsWith("committed")) closed = true; };
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    w.Horse.MoveRestricted = false;
                    var carrier = (UnitMoveTo)LiveMountCarrier(c);
                    endpoint = carrier.ApproachPoint;
                }, 40);
                c.BeforeDrive = null;
                var moveAtEnd = w.Horse.CombatState.Cooldown.MoveAction;
                Frame(c); Frame(c);
                var moveAfter = w.Horse.CombatState.Cooldown.MoveAction;
                var order = r.Order;
                var revalidations = order == null ? -1 : order.RouteRevalidationCount;
                var endedNatively = order != null && order.IsFinished && order.NativeApproachEndedCount == 1 && order.EndCause == MountedAttackEndCause.Cancelled;
                var successorOk = true; var successorDetail = "";
                var carrierCleared = LiveMountCarrier(c) == null;
                if (closedCase)
                {
                    var at = w.Horse.Position;
                    var successor = new UnitMoveTo(at + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, -0.5f) }) };
                    w.Horse.Commands.Run(successor);
                    Frame(c);
                    var slot = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                    successorOk = ReferenceEquals(slot, successor) && order.StruckTargets.Count == 0;
                    successorDetail = "; the player's newer ground order then owns the Move slot " + ReferenceEquals(slot, successor) + " (interrupted " + successor.IsFinished + ")";
                }
                var ok = r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.AfterDecision.MountMove > r.Before.MountMove && (closedCase
                    ? endedNatively && revalidations == 1 && order.StruckTargets.Count == 0 && r.Starts == 0 && r.AtStrike.RiderStandard == r.Before.RiderStandard && moveAfter == moveAtEnd &&
                        carrierCleared && !w.Horse.View.AgentASP.WantsToMove && successorOk
                    : order != null && order.StruckTargets.Count >= 1 && order.NativeApproachEndedCount == 0 && revalidations == 1);
                Check(ok, "R13-F3 (round 14) " + kase + ": first debit " + r.Decision.Substring(0, Math.Min(90, r.Decision.Length)) + "...; carrier endpoint " + endpoint + ", sight closed around it after the commit " + closed +
                    "; mount at " + w.Horse.Position + "; route revalidations (carriers issued) " + revalidations + "; disposition " + (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + " (native force-finish disposition " + order.NativeApproachEndedCount + ")" : "live") +
                    "; mount Move " + r.Before.MountMove.ToString("0.####") + " -> " + r.AfterDecision.MountMove.ToString("0.####") + " -> " + moveAtEnd.ToString("0.####") + " -> " + moveAfter.ToString("0.####") +
                    " s; rider Standard " + r.Before.RiderStandard + " -> " + r.AtStrike.RiderStandard + "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + "; mount agent wants to move " + w.Horse.View.AgentASP.WantsToMove + successorDetail);
            }

            // R11-F10 (round 13 expectation): the short ordinary click is an accepted order. An older compatible in-range Swift on the mount does not
            // drop it; it finishes and one approach and strike follow. Control: behind a competing older mount order the accepted order's carrier is
            // deferred (no movement, no debit, no strike) and the older order is not interrupted.
            foreach (var kase in new[] { "older in-range Swift", "control: older competing mount order" })
            {
                var c = FirstTickPair(0.095f, 1f / 50f, false, false, false, true);
                var w = c.World;
                w.Horse.CombatState.Cooldown.MoveAction = 3f - 1f / 60f;
                var other = new UnitEntityData("hostile-swift") { Position = new Vector3(0.5f, 0f, -0.5f) };
                other.View.Corpulence = 0.5f;
                var older = new HarnessPlayerCommand(other, kase.StartsWith("control") ? UnitCommand.CommandType.Standard : UnitCommand.CommandType.Swift);
                w.Horse.Commands.Run(older);
                var a = PriorSelectionAndHold(c);
                bool ok; string detail;
                if (kase.StartsWith("control"))
                {
                    var click = ClickTick(c, w.Target);
                    var moveAtClick = w.Horse.CombatState.Cooldown.MoveAction;
                    for (var i = 0; i < 3; i++) Frame(c);
                    ok = click.Admitted && click.Order != null && !older.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == moveAtClick && click.Order.StruckTargets.Count == 0 &&
                        ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Standard), older);
                    detail = "click " + click.Decision + "; after three frames the older order live " + !older.IsFinished + ", mount Move " + moveAtClick.ToString("0.#####") + " -> " +
                        w.Horse.CombatState.Cooldown.MoveAction.ToString("0.#####") + " s, strikes " + click.Order.StruckTargets.Count + ", first-tick decision " + c.Combat.LastFirstTickDecision;
                }
                else
                {
                    var swiftLiveAtClick = !older.IsFinished && ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Swift), older);
                    var r = ShortOrdinaryRun(c, a, () => { older.FinishSuccess(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); });
                    ok = swiftLiveAtClick && r.Pending && r.Decision.StartsWith("committed:order:final-ordinary-tick:") && r.Strikes == 1 && older.IsFinished;
                    detail = "Swift live at the click " + swiftLiveAtClick + "; click accepted " + r.Pending + ", bound " + r.Bound + "; Swift finished after binding; " + r.Decision.Substring(0, Math.Min(90, r.Decision.Length)) +
                        "...; executed stops " + string.Join(" ", r.Stops.ToArray()) + "; strikes " + r.Strikes;
                }
                Check(ok, "R11-F10 (round 13 expectation) short ordinary click (one tick left, aligned 0.145 m), " + kase + ": " + detail);
            }

            // R11-F5: NearTheEnd set on the connected mover, the frame (1/60 s) longer than the remaining 5/512 s; the approved perpendicular tick stops at
            // (0.04395, 0.04395) (visible), the full frame would stop at (0.075, 0.075) (z > 0.06: blind). The approved duration is returned and debited.
            {
                var c = FirstTickPair(0.035f, 1f / 60f, false, false, false, true);
                var w = c.World;
                LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.035f || from.z > 0.06f;
                w.Horse.CombatState.Cooldown.MoveAction = 3f - 5f / 512f;
                w.Horse.View.AgentASP.NearTheEnd = true;
                var a = PriorSelectionAndHold(c);
                var r = ShortOrdinaryRun(c, a, () => w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f));
                var forecast = ParseFloat(Field(r.Decision, "delta"));
                var returned = r.MoveAfterDecision - r.MoveBefore;
                var ok = r.Pending && r.Bound && r.Decision.StartsWith("committed:order:final-ordinary-tick:") && Math.Abs(forecast - 5f / 512f) < 1e-5f && Math.Abs(returned - forecast) < 1e-5f &&
                    r.Approved.Contains("approvedTick=True") && Math.Abs(r.Stop.x - ParseFloat(Field(r.Decision, "stopX"))) < 1e-4f && Math.Abs(r.Stop.z - ParseFloat(Field(r.Decision, "stopZ"))) < 1e-4f &&
                    r.Stop.z < 0.06f && r.Strikes == 1;
                Check(ok, "R11-F5 (round 12) NearTheEnd with a 1/60 s frame and 5/512 s left, perpendicular: forecast delta " + forecast.ToString("0.######") + " s, returned and debited " + returned.ToString("0.######") +
                    " s (" + Field(r.Approved, "physicalDelta") + "), predicted stop (" + Field(r.Decision, "stopX") + "," + Field(r.Decision, "stopZ") + "), executed (" + r.Stop.x.ToString("0.#####") + "," + r.Stop.z.ToString("0.#####") +
                    "); strikes " + r.Strikes);
            }

            // R11-F4: sight closed at the 0.08 m waypoint and open at the 0.145 m endpoint (m_Speed 6, MaxSpeed 9, 1/60 s): the kept-waypoint stop is blind,
            // so the longer alternative (native skips the waypoint at 0.01611 s because 0.08 < 6 x 0.01611) is evaluated and reaches the visible endpoint.
            {
                var c = FirstTickPair(0.095f, 1f / 60f);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                agent.Route = (from, to) => new List<Vector3> { from, new Vector3(0.08f, 0f, 0f), to };
                var r = RunProvisional(c, w.Target, null, () => agent.CurrentSpeedField = 6f);
                var delta = ParseFloat(Field(r.Decision, "delta"));
                var ok = r.Decision.StartsWith("committed:replacement:first-tick") && r.Decision.Contains("cursor=1;waypoint#2=") && r.Decision.Contains("alternative=1") &&
                    Math.Abs(delta - 0.145f / 9f) < 1e-5f && 0.08f < 6f * delta && Math.Abs(c.HorseAtDecision.x - 0.145f) < 1e-4f && Math.Abs(r.AfterDecision.MountMove - delta) < 1e-5f && r.Order.StruckTargets.Count == 1;
                Check(ok, "R11-F4 (round 12) blind kept waypoint, visible endpoint: " + r.Decision.Substring(0, Math.Min(r.Decision.IndexOf(";segment=") > 0 ? r.Decision.IndexOf(";segment=") : 80, r.Decision.Length)) +
                    "; returned delta " + delta.ToString("0.######") + " s (skip threshold " + (6f * delta).ToString("0.####") + " m > 0.08 m); executed stop (" + c.HorseAtDecision.x.ToString("0.#####") + "," +
                    c.HorseAtDecision.z.ToString("0.#####") + "); debit " + r.AfterDecision.MountMove.ToString("0.######") + " s; strikes " + r.Order.StruckTargets.Count + "; shorter alternative " +
                    (r.Decision.Contains("shorterRefused={") ? "refused (blind at the waypoint)" : "?"));
            }

            // R11-F3: the mount is displaced sideways after binding (same agent path and cursor); sight closes only on the LIVE incoming chord inside the
            // disc (away from the planned leg and from every final-heading disc sample): refused before any debit; the visible control strikes.
            foreach (var closed in new[] { true, false })
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var chordClosed = false; var p = Vector3.zero; const float block = 0.004f;
                LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 1.2f || chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, p) < block;
                var clearance = float.MaxValue; var planned = float.MaxValue; var end = Vector3.zero;
                var displaced = new Vector3(0.8f, 0f, 0.18f);
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    w.Horse.MoveRestricted = false;
                    end = ((UnitMoveTo)LiveMountCarrier(c)).ApproachPoint;
                    var dx = displaced.x - end.x; var dz = displaced.z - end.z; var dl = (float)Math.Sqrt(dx * dx + dz * dz);
                    p = new Vector3(end.x + 0.03f * dx / dl, 0f, end.z + 0.03f * dz / dl);
                    foreach (var ring in new[] { 0.05f, 0.025f })
                        for (var k = 0; k < 16; k++)
                            clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, new Vector3(end.x + ring * (float)Math.Cos(k * Math.PI / 8d), 0f, end.z + ring * (float)Math.Sin(k * Math.PI / 8d))));
                    planned = Math.Abs(p.z);   // distance from the planned leg (along z = 0)
                    w.Horse.Position = displaced; w.Rider.Position = displaced;
                    chordClosed = closed;
                }, 40);
                var ok = clearance > block && planned > block && block > PairedAttackClickProbe.InDiscSampleMetres && (closed
                    ? RefusedKeptPrior(c, r, "route-arrival-not-admitted") && r.Decision.Contains("liveLegMetres=")
                    : r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Decision.Contains("liveLegMetres=") && r.Order.StruckTargets.Count == 1);
                Check(ok, "R11-F3 (round 12) mount displaced to " + displaced + " after binding (cursor kept), " + (closed ? "live incoming chord blind" : "control, chord visible") + ": blind spot " + p +
                    " radius " + block + " m (from the planned leg " + planned.ToString("0.####") + " m, from every final-heading disc sample " + clearance.ToString("0.####") + " m); " + r.Decision +
                    "; displacement after the tick " + r.Displacement.ToString("0.####") + " m, mount Move " + r.AfterDecision.MountMove.ToString("0.####") + " s; strikes " + r.Order.StruckTargets.Count);
            }

            // R11-F2: a partially used step (physical 1.374 m, native 0.687 m; nominal 9 / agent 18 m/s) with 0.15 physical metres left and a
            // perpendicular start toward the 0.145 m endpoint: the step is not a final time tick; the first tick moves about 0.106 m, the settled
            // remainder lets a second tick cross x = 0.095, one strike. Then the reserved-distance preflight: after that first tick reserved the whole
            // remainder, the order is cancelled and the normal eligibility entry still grants the settled remainder.
            foreach (var kase in new[] { "partial turning step through its strike", "preflight after a reserving tick" })
            {
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                var stops = new List<string>(); var last = w.Horse.Position;
                bool ok; string detail;
                if (kase.StartsWith("partial"))
                {
                    for (var i = 0; i < 14 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        Frame(c);
                        if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(last, w.Horse.Position) > 1e-6f) { stops.Add("(" + w.Horse.Position.x.ToString("0.####") + "," + w.Horse.Position.z.ToString("0.####") + ")"); last = w.Horse.Position; }
                    }
                    var decision = c.Combat.LastFirstTickDecision;
                    ok = click.Admitted && decision.StartsWith("committed:order:ordinary-movement") && !decision.Contains("final-step-counter") && order != null && order.StruckTargets.Count == 1 && stops.Count >= 2 &&
                        c.Pair.movementState.AllocationMetresStepTravelled(w.Horse) <= 1.524f + 1e-4f;
                    detail = "click grant " + Field(line, "mountGrantedMetres") + " m; first debit " + decision.Substring(0, Math.Min(45, decision.Length)) + "...; stops " + string.Join(" ", stops.ToArray()) +
                        "; physical ledger " + c.Pair.movementState.AllocationMetresStepTravelled(w.Horse).ToString("0.####") + " m, native counter " + c.Pair.movementState.AllocationMetresStepped(w.Horse).ToString("0.####") + " m; strikes " + order.StruckTargets.Count;
                }
                else
                {
                    Frame(c);   // the committed first tick reserves the whole 0.15 m remainder
                    var moved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(last, w.Horse.Position);
                    c.Combat.Cancel("player cancelled the order");
                    bool step, single; string reason;
                    var grant = c.Pair.PairedMountGrantedMetres(c.Context, out step, out single, out reason);
                    ok = click.Admitted && moved > 0.09f && moved < 0.12f && step && reason == "granted" && Math.Abs(grant - (0.15f - moved)) < 1e-3f;
                    detail = "first tick moved " + moved.ToString("0.####") + " m (reserved 0.15); after the cancel the eligibility entry reports " + reason + " with " + grant.ToString("0.####") + " m";
                }
                Check(ok, "R11-F2 (round 12) " + kase + ": " + detail);
            }

            // R11-F6: an accepted ordinary wrapper whose carrier is deferred behind older mount work; the rider's Standard is exhausted, then sight opens
            // before any carrier is issued: no child starts and nothing is spent. Entitled control: one strike.
            foreach (var exhaust in new[] { true, false })
            {
                var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                var w = c.World;
                var cast = new HarnessStationaryCommand();
                w.Horse.Commands.Run(cast);
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                for (var i = 0; i < 3; i++) Frame(c);
                var deferred = order != null && !order.IsFinished && order.CarrierDeferred && order.DelegatedMove == null;
                var startsBefore = UnitAttack.NativeAttackStarts;
                var mountStandardBefore = w.Horse.CombatState.Cooldown.StandardAction;
                if (exhaust) w.Rider.HasStandard = false;
                c.Opening[0] = -10f;
                for (var i = 0; i < 10 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var ok = click.Admitted && deferred && (exhaust
                    ? order.IsFinished && order.Transaction.ChildAttackStartCount == 0 && UnitAttack.NativeAttackStarts == startsBefore && order.StruckTargets.Count == 0 &&
                      c.Combat.LastFirstTickDecision.StartsWith("refused-at-first-child:rider-not-entitled") && w.Horse.CombatState.Cooldown.StandardAction == mountStandardBefore
                    : order.StruckTargets.Count == 1);
                Check(ok, "R11-F6 (round 12) carrier deferred behind older mount work, " + (exhaust ? "rider Standard exhausted, then sight opens" : "control, rider entitled") + ": deferred " + deferred +
                    "; " + c.Combat.LastFirstTickDecision.Substring(0, Math.Min(60, c.Combat.LastFirstTickDecision.Length)) + "; child starts " + (order == null ? -1 : order.Transaction.ChildAttackStartCount) +
                    ", native attack starts +" + (UnitAttack.NativeAttackStarts - startsBefore) + ", strikes " + (order == null ? 0 : order.StruckTargets.Count));
            }

            // R11-F8: A's mount-target clear fails at B's commit; the pair dismounts before the retry: the clear stays bound to the ORIGINAL mount (and
            // a replacement mount holding the same target keeps it); a later proposal with its own failing effect does not displace the earlier clear.
            foreach (var kase in new[] { "dismount before the retry", "dismount, then a replacement mount holding the same target" })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                var gA = c.Combat.Generation;
                c.Combat.Own(w.Horse, a, gA);
                var replacement = new UnitEntityData("horse-replacement") { Position = w.Horse.Position };
                var outstanding = ""; var handled = false;
                c.BeforeDrive = () =>
                {
                    var settling = c.Combat.SettlingProposalForDiagnostics;
                    if (handled || settling == null || settling.OutstandingEffects.Count == 0) return;
                    handled = true;
                    outstanding = string.Join(",", settling.OutstandingEffects.ToArray());
                    c.Combat.SimulateDismountForHarness();
                    if (kase.Contains("replacement")) { replacement.CombatState.ManualTarget = a; c.Combat.RebindMountForHarness(replacement); }
                };
                var r = RunProvisional(c, w.Target, null, () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState);
                c.BeforeDrive = null;
                c.Combat.SweepFirstTickProposal();
                var ok = r.Decision.StartsWith("committed:replacement:") && outstanding == "mount-target" && w.Horse.CombatState.ManualTarget == null && c.Combat.SettlingProposalCount == 0 &&
                    (!kase.Contains("replacement") || ReferenceEquals(replacement.CombatState.ManualTarget, a));
                Check(ok, "R11-F8 (round 12) " + kase + ": deferred clear of A's target failed (" + outstanding + "); after the retry the original mount's target is " + Name(w.Horse.CombatState.ManualTarget) +
                    (kase.Contains("replacement") ? ", the replacement mount's target " + Name(replacement.CombatState.ManualTarget) : "") + "; outstanding requests " + c.Combat.SettlingProposalCount);
            }
            {
                // Round 13 (changed expectation under the amendment): a short ordinary click is now an accepted order whose effects apply at its
                // acceptance, so the former two-provisional-requests case is not reachable (provisional requests exist only for the restricted
                // single tick, once per Move). Re-expressed: B (a restricted provisional request) commits while A's mount clear keeps failing; B is
                // cancelled before its strike; A's clear stays owned after B retires and settles on the original mount when the write succeeds.
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                c.Combat.Own(w.Horse, a, c.Combat.Generation);
                Action arm = () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState;
                var bOutstanding = ""; var heldAfterCancel = -1; UnitEntityData horseBeforeRetry = null; var handled = false;
                c.BeforeDrive = () =>
                {
                    var settling = c.Combat.SettlingProposalForDiagnostics;
                    if (handled || settling == null || settling.OutstandingEffects.Count == 0) return;
                    handled = true;
                    bOutstanding = string.Join(",", settling.OutstandingEffects.ToArray());
                    arm();
                    c.Combat.Cancel("B cancelled before its strike");
                    heldAfterCancel = c.Combat.SettlingProposalCount;
                    horseBeforeRetry = w.Horse.CombatState.ManualTarget;
                    arm();   // this frame's sweep fails again; the next frame's write succeeds
                };
                var r = RunProvisional(c, w.Target, null, arm);
                c.BeforeDrive = null;
                Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null;
                c.Combat.SweepFirstTickProposal();
                var ok = r.Decision.StartsWith("committed:replacement:") && bOutstanding == "mount-target" && heldAfterCancel == 1 && ReferenceEquals(horseBeforeRetry, a) &&
                    w.Horse.CombatState.ManualTarget == null && c.Combat.SettlingProposalCount == 0;
                Check(ok, "R11-F8 (round 13 expectation) B commits with A's mount clear failing, B cancelled before its strike: outstanding (" + bOutstanding + "), requests held after the cancel " + heldAfterCancel +
                    "; after the write succeeds the original mount's target " + Name(horseBeforeRetry) + " -> " + Name(w.Horse.CombatState.ManualTarget) + ", outstanding " + c.Combat.SettlingProposalCount);
            }

            // R11-F1 (round 14): the saved step commitments through the ACTUAL persistence chain: production CapturePersistence of the paired allocation
            // (the archive writer's capture: CaptureSharedPersistence, no single-field fold), the archive envelope with the writer's travel rows
            // (StepTravel = SavedStepTravel.Detach(Combat.Allocations), MountedPersistenceService.World.cs), the SELECTED archive codec
            // (MountedArchiveCodec.Encode/Decode, the production files compiled with the installed Newtonsoft.Json), production RestorePersistence,
            // production CopyGrantedMovementToContext into the restored partner turn, whose installed step field (MetersMovedByFiveFootStep) is read
            // before the real click. Straight 1.2 m physical / 0.6 m native (agent 18, nominal 9): the partner turn holds 0.6 and the grant stays
            // 0.324 m (0.3 m route admitted). Turning 0.45 m native / 0.42426 m physical (nominal 9, physical 12): 1.09974 m before and after, so a
            // 1.085 m route (between it and the former single-value 1.074 m) is admitted. Historical: the same archive rewritten as schema 3 with a
            // single stored value (1.25 m) restores both limits from it (legacy interpretation). Malformed: the current archive without its travel row
            // is refused by the codec before restoration (allocation and partner turn untouched).
            // Round 15 (R11-F1): the archive's participation is the production CaptureParticipation of the ACTIVE pair (its activation, current
            // boundary and partner turn; Split false), its identities are the live world's (GUID actors from before the pair exists; the grant is
            // the activation's own identity), and the restore is the production order: RestorePersistence, then the production RestoreParticipation,
            // which rebinds the activation to the native current turn, sets partnerContext from the saved partner turn and copies the restored
            // allocation into it. The click then runs on that restored participation (its PairedMovementContext is the restored partner turn).
            foreach (var kase in new[] { "straight 1.2 m physical / 0.6 m native, 0.3 m route", "turning 0.45 m native / 0.42426 m physical, 1.085 m route",
                "historical schema-3 archive (single value 1.25 m)", "malformed current archive without the travel row" })
            {
                var turning = kase.StartsWith("turning"); var historical = kase.StartsWith("historical"); var malformed = kase.StartsWith("malformed");
                var route = turning ? 1.085f : 0.3f;
                // R11-F1 (round 15): the world's actors carry GUID identities from before the pair exists (as native actors do); nothing is
                // substituted afterwards. The pair, its activation, partner turn and the allocation's grant identity are the production ones
                // (Connect: BeginPairedPreparation/BeginGrantedPreparation under the activation identity).
                var sight = new[] { route - 0.05f };
                var w = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(2.0f, 0f, 0f), (from, to) => from.x < sight[0], true);
                w.Rider.UniqueId = Guid.NewGuid().ToString("N"); w.Horse.UniqueId = Guid.NewGuid().ToString("N");
                w.Horse.CombatSpeedMps = 9f; w.Horse.MoveRestricted = false;
                var c = Connect(w, false, true, true);
                c.Opening = sight; c.Context.EnabledFiveFootStep = true; c.Pair.PairedMovementContext(c.Context);
                w.TickSeconds = 1f / 60f; Kingmaker.Game.Instance.TimeController.DeltaTime = 1f / 60f;
                var a = PriorSelectionAndHold(c);
                w.Horse.View.AgentASP.SpeedFactor = turning ? 12f / 9f : 2f;
                if (turning) c.Pair.movementState.SetStepLedger(w.Horse, 0.45f, 0.42426f); else c.Pair.movementState.SetStepLedger(w.Horse, 0.6f, 1.2f);
                var originalActivation = c.Pair.Activation; var originalPartner = c.Pair.PartnerContextForHarness;
                var originalIdentity = originalActivation.Identity; var originalSequence = originalActivation.Sequence;
                var grantId = c.Pair.movementState.AllocationGrantIdentity(w.Horse);
                // A stale value in the live partner turn's own step field: the saved partner turn carries it, and the production restore must
                // replace it with the restored allocation's native counter (CopyGrantedMovementToContext inside RestoreParticipation).
                originalPartner.MetersMovedByFiveFootStep = 0.9f;
                bool st, sm; string reason;
                var before = c.Pair.PairedMountGrantedMetres(c.Context, out st, out sm, out reason);
                var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var captured = c.Pair.movementState.CapturePersistence();
                var row = captured.Single(x => x.ActorId == w.Horse.UniqueId);
                var capturedRound = row.Round; var capturedGrant = row.GrantId;
                var participation = c.Pair.CaptureParticipation();   // production capture of the ACTIVE participation (activation, current boundary, partner turn)
                var envelope = Chunk7PersistenceTests.ArchiveForHarness(w.Rider.UniqueId, w.Horse.UniqueId);
                envelope.Combat.Current = NativeTurnPersistence.Capture(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn);
                envelope.Combat.Allocations = captured;
                envelope.StepTravel = SavedStepTravel.Detach(envelope.Combat.Allocations);
                envelope.Participations[0].State = participation;
                var json = MountedArchiveCodec.Encode(envelope);
                if (historical || malformed)
                {
                    var tree = Newtonsoft.Json.Linq.JObject.Parse(json);
                    if (historical)
                    {
                        tree.Remove("StepTravel"); tree["SchemaVersion"] = 3;
                        foreach (var allocation in (Newtonsoft.Json.Linq.JArray)tree["Combat"]["Allocations"]) allocation["Movement"]["MetresStepped"] = 1.25f;
                    }
                    else tree["StepTravel"] = new Newtonsoft.Json.Linq.JArray();
                    json = tree.ToString(Newtonsoft.Json.Formatting.None);
                }
                var read = MountedArchiveCodec.Decode(json);
                var savedParticipation = read.Data == null ? null : read.Data.Participations[0].State;
                var activationText = "saved activation " + (participation.Activation == null ? "none" : participation.Activation.EncounterId.Substring(0, 8) + "..:" + participation.Activation.Sequence +
                    " split " + participation.Activation.Split) + ", boundary current " + participation.BoundaryIsCurrent + ", saved partner turn step field " + participation.Partner.Movement.MetresStepped;
                if (malformed)
                {
                    var untouched = c.Pair.movementState.AllocationMetresStepped(w.Horse) == nativeBefore && c.Pair.movementState.AllocationMetresStepTravelled(w.Horse) == physicalBefore;
                    var ok0 = read.Kind == MountedSaveReadKind.Invalid && read.Data == null && untouched && ReferenceEquals(c.Pair.Activation, originalActivation) &&
                        ReferenceEquals(c.Pair.PartnerContextForHarness, originalPartner) && originalPartner.MetersMovedByFiveFootStep == 0.9f;
                    Check(ok0, "R11-F1 (round 15) " + kase + ": codec verdict " + read.Kind + " (" + read.Feedback + "); no restoration: allocation " + nativeBefore + " / " + physicalBefore +
                        " m unchanged " + untouched + ", the active activation and partner turn are the same objects (partner step field " + originalPartner.MetersMovedByFiveFootStep + " m untouched)");
                    continue;
                }
                var actors = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal) { { w.Horse.UniqueId, w.Horse }, { w.Rider.UniqueId, w.Rider } };
                var decodedRow = read.Data.Combat.Allocations.Single(x => x.ActorId == w.Horse.UniqueId);
                // The production restore order (MountedPersistenceService.World: RestoreSharedPersistence, then each owner's RestoreParticipation).
                c.Pair.movementState.RestorePersistence(read.Data.Combat.Allocations, actors);
                var restoresBefore = NativeTurnPersistence.Restores;
                c.Pair.RestoreParticipation(savedParticipation, actors, read.Data.Combat.TurnBased);
                var restoredActivation = c.Pair.Activation; var restoredPartner = c.Pair.PartnerContextForHarness;
                var partnerStep = restoredPartner == null ? -1f : restoredPartner.MetersMovedByFiveFootStep;
                var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                // The click's movement input is the restored participation's partner turn (the per-frame PairedMovementContext the grant reads).
                var clickInput = c.Pair.PairedMovementContext(c.Context);
                var after = c.Pair.PairedMountGrantedMetres(c.Context, out st, out sm, out reason);
                var restoredBinding = restoredActivation != null && !ReferenceEquals(restoredActivation, originalActivation) && restoredPartner != null && !ReferenceEquals(restoredPartner, originalPartner) &&
                    NativeTurnPersistence.Restores == restoresBefore + 1 && ReferenceEquals(restoredPartner.Unit, w.Horse) && restoredActivation.Identity == originalIdentity &&
                    restoredActivation.Sequence == originalSequence && !restoredActivation.Split && ReferenceEquals(restoredActivation.Principal, w.Rider) && ReferenceEquals(restoredActivation.Partner, w.Horse) &&
                    ReferenceEquals(clickInput, restoredPartner) && c.Pair.CanAddressActor(w.Horse, c.Context) && ReferenceEquals(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn, c.Context);
                var identities = read.Kind == MountedSaveReadKind.Current && decodedRow.ActorId == w.Horse.UniqueId && decodedRow.Round == capturedRound && decodedRow.GrantId == capturedGrant &&
                    capturedGrant == grantId && grantId == originalIdentity && c.Pair.movementState.AllocationGrantIdentity(w.Horse) == grantId && read.Data.Pairs[0].Mount.Id == w.Horse.UniqueId &&
                    read.Data.Pairs[0].Rider.Id == w.Rider.UniqueId && savedParticipation.RiderId == w.Rider.UniqueId && savedParticipation.MountId == w.Horse.UniqueId &&
                    savedParticipation.Partner.ActorId == w.Horse.UniqueId && read.Data.Combat.Current.ActorId == w.Rider.UniqueId && participation.Activation != null && !participation.Activation.Split;
                var identityText = "actor " + decodedRow.ActorId.Substring(0, 8) + ".. (the horse " + (decodedRow.ActorId == w.Horse.UniqueId) + "), rider " + w.Rider.UniqueId.Substring(0, 8) + ".., pair " +
                    read.Data.Pairs[0].PairId.Substring(0, 8) + ".., " + activationText + "; restored activation " + restoredActivation.Identity.Substring(0, 8) + "..:" + restoredActivation.Sequence + " (identity equal " +
                    (restoredActivation.Identity == originalIdentity) + ", new object " + !ReferenceEquals(restoredActivation, originalActivation) + ", split " + restoredActivation.Split + "), partner turn new object " +
                    !ReferenceEquals(restoredPartner, originalPartner) + " for the horse " + ReferenceEquals(restoredPartner.Unit, w.Horse) + ", click input is the restored partner turn " + ReferenceEquals(clickInput, restoredPartner) +
                    ", round " + capturedRound + " -> " + decodedRow.Round + ", grant " + capturedGrant.Substring(0, 15) + ".. (the activation's own identity " + (grantId == originalIdentity) + ") -> " +
                    (c.Pair.movementState.AllocationGrantIdentity(w.Horse) ?? "none").Substring(0, 15) + "..";
                bool ok; string detail;
                if (historical)
                {
                    ok = identities && restoredBinding && read.SourceSchema == 3 && decodedRow.Movement.MetresStepped == 1.25f && decodedRow.StepTravelled == 1.25f && nativeAfter == 1.25f && physicalAfter == 1.25f &&
                        partnerStep == 1.25f && Math.Abs(after - 0.274f) < 1e-3f;
                    detail = "codec source schema " + read.SourceSchema + " -> " + MountedArchiveData.Schema + "; " + identityText + "; restored native counter " + nativeAfter + " m and physical travel " + physicalAfter +
                        " m (legacy interpretation); restored partner turn step field " + partnerStep + " m; grant " + after.ToString("0.####") + " m";
                }
                else
                {
                    var click = ClickTick(c, w.Target);
                    var line = LastLine(c);
                    var single = Math.Max(nativeBefore, physicalBefore);
                    var formerRemainder = Math.Min((1.524f - single) / 9f, (1.524f - single) / (turning ? 12f : 18f)) * (turning ? 12f : 18f);
                    ok = identities && restoredBinding && read.SourceSchema == MountedArchiveData.Schema && read.Data.StepTravel.Length == captured.Length && read.Data.StepTravel.Single(x => x.ActorId == w.Horse.UniqueId).MetresStepTravelled == physicalBefore &&
                        nativeAfter == nativeBefore && physicalAfter == physicalBefore && partnerStep == nativeBefore && reason == "granted" && Math.Abs(after - before) < 1e-5f &&
                        (turning ? Math.Abs(before - 1.09974f) < 2e-3f && route < before && route > formerRemainder : partnerStep == 0.6f && physicalAfter == 1.2f && Math.Abs(before - 0.324f) < 1e-3f) &&
                        click.Admitted && click.Order != null && ReferenceEquals(c.Pair.PartnerContextForHarness, restoredPartner) && Field(line, "mountStepMode") == "True";
                    detail = "codec " + read.Kind + " schema " + read.SourceSchema + " (" + System.Text.Encoding.UTF8.GetByteCount(json) + " bytes, travel rows " + read.Data.StepTravel.Length + " for " + captured.Length + " allocations, the horse's {" + read.Data.StepTravel.Single(x => x.ActorId == w.Horse.UniqueId).MetresStepTravelled +
                        " m}); " + identityText + "; native counter " + nativeBefore + " -> " + nativeAfter + " m, physical travel " + physicalBefore + " -> " + physicalAfter + " m; restored partner turn step field " +
                        partnerStep + " m (0.9 in the saved turn, replaced by the production restore's copy); grant before " + before.ToString("0.#####") + " m, after " + after.ToString("0.#####") + " m (the former single value " + single + " m would give " +
                        formerRemainder.ToString("0.#####") + " m); click on the restored participation " + click.Decision + " (step mode " + Field(line, "mountStepMode") + ", route " + Field(line, "routeMetres") + " m, grant " + Field(line, "mountGrantedMetres") + " m)";
                }
                Check(ok, "R11-F1 (round 15) " + kase + ": " + detail);
            }
            // R13-F1 (round 15): the real codec's step-travel controls on this candidate (valid two-allocation round trip, one of two travel rows
            // removed: refused on decode and on encode; the schema-3 historical read and the future-version control).
            Console.WriteLine("R13-F1 (round 15): Chunk7PersistenceTests.RunStepTravelControls (production MountedArchiveCodec):");
            try { Chunk7PersistenceTests.RunStepTravelControls(); Check(true, "R13-F1 (round 15) the step-travel codec controls above ran on the candidate without a failure"); }
            catch (Exception error) { Check(false, "R13-F1 (round 15) step-travel codec controls: " + error.Message); }

            // R11-F9: a one-shot CurrentSpeedMps failure with a readable MaxSpeedOverride (12 m/s). Ordinary Move (time) at the click and at the first
            // debit: priced with the override, moving and striking. A step needs the nominal speed for its native counter: refused at the click
            // (prior kept) and, for an accepted step, refused at the debit before any movement or spending.
            foreach (var kase in new[] { "ordinary, failure at the click and at the first debit", "step, failure at the click", "step, failure at the first debit" })
            {
                var step = kase.StartsWith("step");
                var c = FirstTickPair(1.2f, 1f / 60f, false, false, step);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                w.Horse.View.AgentASP.MaxSpeedOverride = 12f;
                if (!kase.EndsWith("first debit") || !step) UnitEntityData.CurrentSpeedFault = u => u == w.Horse;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var armedAtClick = !kase.EndsWith("first debit") || !step;
                var clickConsumed = armedAtClick && UnitEntityData.CurrentSpeedFault == null;
                UnitEntityData.CurrentSpeedFault = null;
                var order = click.Order;
                bool ok; string detail;
                if (kase == "step, failure at the click")
                {
                    ok = clickConsumed && !click.Admitted && Field(line, "mountGrant") == "mount-movement-spent" && order == null && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState;
                    detail = "click " + click.Decision + " (mountGrant " + Field(line, "mountGrant") + "); target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState;
                }
                else
                {
                    Frame(c);
                    UnitEntityData.CurrentSpeedFault = u => u == w.Horse;
                    var before = w.Horse.Position;
                    var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                    Frame(c);
                    var debitConsumed = UnitEntityData.CurrentSpeedFault == null;
                    UnitEntityData.CurrentSpeedFault = null;
                    var firstMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(before, w.Horse.Position);
                    var firstDebit = w.Horse.CombatState.Cooldown.MoveAction - moveBefore;
                    var decision = c.Combat.LastFirstTickDecision;
                    var tickLine = w.LastMountTick ?? "";
                    for (var i = 0; i < 14 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    if (!step)
                        ok = clickConsumed && debitConsumed && click.Admitted && decision.StartsWith("committed:order:ordinary-movement") && Math.Abs(firstMoved - 0.2f) < 1e-4f &&
                            Math.Abs(firstDebit - 1f / 60f) < 1e-5f && !tickLine.Contains("speed-unreadable") && order.StruckTargets.Count == 1;
                    else
                        ok = debitConsumed && click.Admitted && decision.StartsWith("refused:") && decision.Contains("allowedMetres=NaN") && firstMoved == 0f && w.Horse.CombatState.Cooldown.MoveAction == 0f &&
                            c.Pair.movementState.AllocationMetresStepped(w.Horse) == 0f && order.IsFinished && order.StruckTargets.Count == 0;
                    detail = "click " + click.Decision + " (grant " + Field(line, "mountGrantedMetres") + " m, fault " + (armedAtClick ? "consumed " + clickConsumed : "not armed at the click") + "); first debit " + decision.Substring(0, Math.Min(70, decision.Length)) +
                        "... (fault consumed " + debitConsumed + "; tick " + tickLine.Substring(0, Math.Min(60, tickLine.Length)) + "); first tick moved " + firstMoved.ToString("0.####") + " m, debited " +
                        firstDebit.ToString("0.#####") + " s; strikes " + (order == null ? 0 : order.StruckTargets.Count);
                }
                Check(ok, "R11-F9 (round 12) " + kase + ": " + detail);
            }

            // ===== round 13 =====
            // R11-F3 (round 13): the live connecting leg is always validated (no displacement threshold). (a) Sub-millimetre grazing: the planned incoming
            // leg passes 50.2 mm from the destination (outside the 50 mm disc); the mount is moved 0.8 mm sideways after binding, so its live leg to the
            // cursor point enters the disc; a blind patch on that live chord, 3 mm from every disc sample, refuses before any debit; visible control
            // commits. (b) Reverse approach: the mount is displaced 80 mm past the truncated endpoint along the native route's extension; the live
            // return leg (heading back) crosses a blind patch inside the disc: refused; visible control commits.
            foreach (var kase in new[] { "sub-millimetre grazing, live chord blind", "sub-millimetre grazing, visible control", "reverse approach past the truncated endpoint, blind", "reverse approach, visible control" })
            {
                var closed = kase.Contains("blind");
                var grazingCase = kase.StartsWith("sub");
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var end = new Vector3(1.0035f, 0f, 0.0502f); var v1 = new Vector3(1.1035f, 0f, 0f);
                const float block = 0.0012f;
                var chordClosed = false; var q = Vector3.zero; var displaced = Vector3.zero; var farLimit = float.MaxValue;
                if (grazingCase)
                {
                    agent.Route = (from, to) => new List<Vector3> { from, v1, end };
                    displaced = new Vector3(0.9035f, 0f, 0.0008f);
                    q = new Vector3(1.0005f, 0f, 0.0008f * (v1.x - 1.0005f) / (v1.x - displaced.x));
                    LineOfSightGeometry.Instance.Blocked = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, end) > 0.0501f ||
                        chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, q) < block;
                }
                else LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 1.2f || from.x > farLimit || chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, q) < block;
                var clearance = float.MaxValue; var centre = end;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    w.Horse.MoveRestricted = false;
                    var carrier = (UnitMoveTo)LiveMountCarrier(c);
                    if (!grazingCase)
                    {
                        centre = carrier.ApproachPoint;
                        displaced = centre + new Vector3(0.08f, 0f, 0f);
                        q = centre + new Vector3(0.035f, 0f, 0f);
                        farLimit = centre.x + 0.06f;   // the displaced pose itself is blind (no strike in place); the disc stays visible
                    }
                    else centre = carrier.ApproachPoint;
                    foreach (var ring in new[] { 0.05f, 0.025f })
                        for (var k = 0; k < 16; k++)
                            clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, new Vector3(centre.x + ring * (float)Math.Cos(k * Math.PI / 8d), 0f, centre.z + ring * (float)Math.Sin(k * Math.PI / 8d))));
                    clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre));
                    w.Horse.Position = displaced; w.Rider.Position = displaced;
                    chordClosed = closed;
                }, 40);
                var qInDisc = Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre) < 0.05f;
                var bound = !grazingCase || r.Carrier != null && Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Carrier.ApproachPoint, end) < 1e-3f;
                var ok = bound && qInDisc && clearance > block && block > PairedAttackClickProbe.InDiscSampleMetres && (closed
                    ? RefusedKeptPrior(c, r, "route-arrival-not-admitted") && r.Decision.Contains("liveLegMetres=")
                    : r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Decision.Contains("liveLegMetres="));
                Check(ok, "R11-F3 (round 13) " + kase + ": bound " + bound + ", refused-kept-prior " + (closed && RefusedKeptPrior(c, r, "route-arrival-not-admitted")) + "; destination " + centre + ", mount moved after binding to " + displaced + "; blind patch " + q + " radius " + block + " m (inside the disc " + qInDisc +
                    ", clearance from every disc sample and the centre " + clearance.ToString("0.####") + " m); " + r.Decision + "; displacement after the tick " + r.Displacement.ToString("0.####") +
                    " m, mount Move " + r.AfterDecision.MountMove.ToString("0.####") + " s; strikes " + (r.Order == null ? 0 : r.Order.StruckTargets.Count));
            }

            // R11-F3 (round 14): the reviewer's counterexample. The planned 0.2 m incoming leg passes 50.004 mm from the 50 mm disc centre (it misses
            // the disc); the mount's start is moved INWARD by 0.016 mm after binding (below the removed 20 micrometre identity), so the live leg passes
            // about 49.996 mm from the centre with a ~1.26 mm chord, longer than the 1 mm sampling. The configuration is rotated by 11.25 degrees so
            // the chord's normal lies midway between two ring samples: a blind patch on that chord is invisible to every disc sample and the centre.
            // Blind: refused before any debit, prior order kept; visible control: committed.
            foreach (var kase in new[] { "16-micrometre inward displacement, live chord blind between ring samples", "16-micrometre inward displacement, visible control" })
            {
                var closed = kase.Contains("blind");
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var theta = 11.25d * Math.PI / 180d;
                Func<double, double, Vector3> rot = (x, z) => new Vector3((float)(x * Math.Cos(theta) - z * Math.Sin(theta)), 0f, (float)(x * Math.Sin(theta) + z * Math.Cos(theta)));
                w.Target.Position = rot(2.0d, 0d);
                // The click's destination lies a boundary margin before the route end on the last segment, 0.0446 mm nearer the incoming leg (round-14
                // measurement); the route end is placed so that the DESTINATION (the disc centre) is 50.004 mm from the planned leg.
                var end = rot(1.0035d, 0.0500486d); var v1 = rot(1.1035d, 0d);
                var displaced = rot(0.9035d, 0.000016d);
                var q = rot(1.0035d, 0.000008d);
                const float block = 0.0012f;
                var chordClosed = false;
                agent.Route = (from, to) => new List<Vector3> { from, v1, end };
                LineOfSightGeometry.Instance.Blocked = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, end) > 0.0501f ||
                    chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, q) < block;
                var clearance = float.MaxValue; var centre = end; var plannedLegDistance = 0d; var liveLegDistance = 0d;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    w.Horse.MoveRestricted = false;
                    var carrier = (UnitMoveTo)LiveMountCarrier(c);
                    centre = carrier.ApproachPoint;
                    foreach (var ring in new[] { 0.05f, 0.025f })
                        for (var k = 0; k < 16; k++)
                            clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, new Vector3(centre.x + ring * (float)Math.Cos(k * Math.PI / 8d), 0f, centre.z + ring * (float)Math.Sin(k * Math.PI / 8d))));
                    clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre));
                    Func<Vector3, Vector3, double> lineDistance = (p0, p1) =>
                    {
                        double ux = p1.x - p0.x, uz = p1.z - p0.z, l = Math.Sqrt(ux * ux + uz * uz);
                        return Math.Abs((centre.x - p0.x) * uz - (centre.z - p0.z) * ux) / l;
                    };
                    plannedLegDistance = lineDistance(Vector3.zero, v1);
                    liveLegDistance = lineDistance(displaced, v1);
                    w.Horse.Position = displaced; w.Rider.Position = displaced;
                    chordClosed = closed;
                }, 40);
                var qInDisc = Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre) < 0.05f;
                var bound = r.Carrier != null && Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Carrier.ApproachPoint, end) < 1e-3f;
                var chordText = Field(r.Decision, "liveChordMetres");
                double chord;
                var chordOk = double.TryParse(chordText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out chord) &&
                    chord > PairedAttackClickProbe.InDiscSampleMetres && chord < 0.0014d;
                var geometry = plannedLegDistance > 0.05d && liveLegDistance < 0.05d && chordOk && qInDisc && clearance > 5f * block;
                var ok = bound && geometry && (closed
                    ? RefusedKeptPrior(c, r, "route-arrival-not-admitted") && r.Decision.Contains("onPlannedSegment=False") && r.Decision.Contains("refusedOnLiveChordAt=")
                    : r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Decision.Contains("onPlannedSegment=False") && r.Decision.Contains("liveSamples="));
                Check(ok, "R11-F3 (round 14) " + kase + ": bound " + bound + "; planned leg " + (plannedLegDistance * 1000d).ToString("0.####") + " mm and live leg " + (liveLegDistance * 1000d).ToString("0.####") +
                    " mm from the destination " + centre + " (disc 50 mm), live chord " + chordText + " m; mount moved after binding to " + displaced + "; blind patch " + q + " radius " + block +
                    " m (inside the disc " + qInDisc + ", clearance from every ring sample and the centre " + clearance.ToString("0.####") + " m); " + r.Decision + "; displacement after the tick " +
                    r.Displacement.ToString("0.####") + " m, mount Move " + r.AfterDecision.MountMove.ToString("0.####") + " s; strikes " + (r.Order == null ? 0 : r.Order.StruckTargets.Count) +
                    (closed ? "; refused-kept-prior " + RefusedKeptPrior(c, r, "route-arrival-not-admitted") : ""));
            }

            // ===== round 15 =====
            // PR #70 R14-F1 (round 15): the reviewer's oblique counterexample. The planned approach runs along x to the destination (route [start, end]
            // on the x axis); after binding the mount stands at destination + (-0.001, 0.2) m, so its live leg to the cursor point (the destination)
            // is almost perpendicular to the planned segment: its 50 mm in-disc chord projects onto only 0.25 mm of the planned segment (the round-14
            // sampler checked only the endpoint there). A blind patch on the live chord 35 mm from the destination lies between the 25 mm and 50 mm
            // ring samples. Blind: refused before any debit with the prior order kept (sampled at the live chord's own 1 mm spacing); visible control:
            // committed. The facade's Vector3.Lerp now clamps like Unity's; the round-13 reverse leg (projecting beyond the truncated endpoint) above
            // runs with it, and LiveLegAdmits no longer interpolates with Lerp at all.
            {
                var lerpClamps = Vector3.Lerp(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), 1.5f).x == 1f && Vector3.Lerp(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), -0.5f).x == 0f;
                Check(lerpClamps, "R14-F1 (round 15) the harness facade's Vector3.Lerp clamps its parameter to [0, 1] as Unity's does (Lerp(0, 1, 1.5) = 1, Lerp(0, 1, -0.5) = 0)");
            }
            foreach (var kase in new[] { "oblique live chord from (-0.001, 0.2) m, blind patch at z = 35 mm between ring samples", "oblique live chord from (-0.001, 0.2) m, visible control" })
            {
                var closed = kase.Contains("blind");
                var c = FirstTickPair(1.2f, 1f / 60f);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var end = new Vector3(1.0035f, 0f, 0f);
                const float block = 0.0012f;
                var chordClosed = false; var q = Vector3.zero; var displaced = Vector3.zero; var centre = end; var clearance = float.MaxValue;
                agent.Route = (from, to) => new List<Vector3> { from, end };
                LineOfSightGeometry.Instance.Blocked = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, end) > 0.0501f ||
                    chordClosed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(from, q) < block;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    w.Horse.MoveRestricted = false;
                    var carrier = (UnitMoveTo)LiveMountCarrier(c);
                    centre = carrier.ApproachPoint;
                    displaced = centre + new Vector3(-0.001f, 0f, 0.2f);
                    q = centre + new Vector3(-0.001f * 0.035f / 0.2f, 0f, 0.035f);
                    foreach (var ring in new[] { 0.05f, 0.025f })
                        for (var k = 0; k < 16; k++)
                            clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, new Vector3(centre.x + ring * (float)Math.Cos(k * Math.PI / 8d), 0f, centre.z + ring * (float)Math.Sin(k * Math.PI / 8d))));
                    clearance = Math.Min(clearance, Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre));
                    w.Horse.Position = displaced; w.Rider.Position = displaced;
                    chordClosed = closed;
                }, 40);
                var qInDisc = Kingmaker.Utility.GeometryUtils.MechanicsDistance(q, centre) < 0.05f;
                var bound = r.Carrier != null && Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Carrier.ApproachPoint, end) < 1e-3f;
                var chordText = Field(r.Decision, "liveChordMetres");
                var ok = bound && qInDisc && clearance > 5f * block && Field(r.Decision, "liveSamples") != "?" && (closed
                    ? RefusedKeptPrior(c, r, "route-arrival-not-admitted") && r.Decision.Contains("onPlannedSegment=False") && r.Decision.Contains("refusedOnLiveChordAt=")
                    : r.Decision.StartsWith("committed:replacement:ordinary-movement") && r.Decision.Contains("onPlannedSegment=False"));
                Check(ok, "R14-F1 (round 15) " + kase + ": bound " + bound + "; destination " + centre + ", live start " + displaced + " (live chord " + chordText + " m, live samples " +
                    Field(r.Decision, "liveSamples") + ", refused point " + Field(r.Decision, "refusedPoint") + ", planned-parameter samples " + Field(r.Decision, "atPlannedParameters") + "); blind patch " + q +
                    " radius " + block + " m (inside the disc " + qInDisc + ", clearance from every ring sample and the centre " + clearance.ToString("0.####") + " m); " + r.Decision +
                    "; displacement after the tick " + r.Displacement.ToString("0.####") + " m, mount Move " + r.Before.MountMove.ToString("0.####") + " -> " + r.AfterDecision.MountMove.ToString("0.####") +
                    " s; strikes " + (r.Order == null ? 0 : r.Order.StruckTargets.Count) + (closed ? "; refused-kept-prior " + RefusedKeptPrior(c, r, "route-arrival-not-admitted") : ""));
            }

            // R12-F1 (round 13): a step whose NATIVE counter this tick makes final is decided on its executable stop before the debit. Equal speeds
            // (nominal and physical 9 m/s), 0.140625 m left on both counters, a 1/64 s frame, 0.136 m endpoint, sight from x = 0.086: aligned commits
            // (final-step-counter) and strikes; perpendicular stops near (0.0703, 0.0703), blind: refused before any debit (both ledgers unchanged,
            // no displacement, no strike). The faster turning step (physical 18 vs nominal 9: the physical reservation binds, so the counter is not
            // final) keeps its route checks and gets its second tick (the R11-F2 partial turning step case, ledgers recorded there).
            foreach (var heading in new[] { "aligned", "perpendicular" })
            {
                var c = FirstTickPair(0.086f, 1f / 64f, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                c.Pair.movementState.SetStepLedger(w.Horse, 1.524f - 0.140625f, 1.524f - 0.140625f);
                var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var gBefore = c.Combat.Generation; var riderHoldBefore = w.Rider.HoldState; var horseHoldBefore = w.Horse.HoldState;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                Frame(c);
                if (heading == "perpendicular") w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                var start = w.Horse.Position; string decision = null; var stop = start;
                c.BeforeDrive = () => { if (decision != null || c.Combat.LastFirstTickDecision.StartsWith("pending")) return; decision = c.Combat.LastFirstTickDecision; stop = w.Horse.Position; };
                for (var i = 0; i < 3 && decision == null; i++) Frame(c);
                c.BeforeDrive = null;
                decision = decision ?? "<none>";
                var nativeAtDecision = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalAtDecision = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                var displacement = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, stop);
                var strikes = order == null ? 0 : order.StruckTargets.Count;
                // Round 14 (R12-F1): the click is a provisional request (finalStepPossible): its replacement effects wait for the before-debit decision.
                bool ok = click.Admitted && order != null && Field(line, "finalStepPossible") == "True";
                if (heading == "aligned")
                    ok &= decision.StartsWith("committed:replacement:final-step-counter:") && strikes == 1 && nativeAtDecision <= 1.524f + 1e-4f && nativeAtDecision > nativeBefore &&
                        c.Combat.Generation != gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target);
                else
                    ok &= decision.StartsWith("refused:final-step-counter-no-affordable-strike-position") && displacement < 1e-6f && nativeAtDecision == nativeBefore && physicalAtDecision == physicalBefore &&
                        strikes == 0 && order.IsFinished && LiveMountCarrier(c) == null && c.Combat.Generation == gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) &&
                        w.Rider.HoldState == riderHoldBefore && w.Horse.HoldState == horseHoldBefore;
                Check(ok, "R12-F1 (round 14) equal-speed final-counter step, " + heading + ": click " + click.Decision + " (grant " + Field(line, "mountGrantedMetres") + " m, route " + Field(line, "routeMetres") +
                    " m); first debit " + decision.Substring(0, Math.Min(150, decision.Length)) + "; native counter " + nativeBefore.ToString("0.######") + " -> " + nativeAtDecision.ToString("0.######") +
                    " m, physical travel " + physicalBefore.ToString("0.######") + " -> " + physicalAtDecision.ToString("0.######") + " m (settled " + c.Pair.movementState.AllocationMetresStepTravelled(w.Horse).ToString("0.######") +
                    " m); displacement at the decision " + displacement.ToString("0.#####") + " m, stop (" + stop.x.ToString("0.#####") + "," + stop.z.ToString("0.#####") + "); attack disposition " +
                    (strikes == 1 ? "strike" : "no strike, order ended " + order.IsFinished) + "; generation " + gBefore + " -> " + c.Combat.Generation + "; target " + Name(w.Rider.CombatState.ManualTarget) +
                    " (prior " + Name(a) + "); rider Hold " + riderHoldBefore + " -> " + w.Rider.HoldState + ", horse Hold " + horseHoldBefore + " -> " + w.Horse.HoldState);
            }

            // R11-F8 (round 15): assignment observation outlives pair custody through the PRODUCTION custody path: the real Domain MountedPairRegistry
            // owner reserved and committed for (rider, horse) with the production retained-commands predicate (HasRetainedPairCommands), the spliced
            // relationship transitions (the Dismount tail's CompleteOwnershipAttempt, the per-frame RefreshOwnership retirement check) and the spliced
            // PatchBridge lookups (Owner/CombatFor(commands) over World.Pairs, Pairs = the world roster) that the ACTUAL InternalRunPostfix reads.
            // A's deferred mount-target clear fails during B's commit; the pair dismounts (the controller's Dismounting cancel, then the relationship's
            // ownership tail); an intervening retry fails on a target READ after ApplyAcceptanceEffects emptied its diagnostic list, so the list stays
            // EMPTY while the clear is owed; then a compatible native-shaped command assigns the same target on the ORIGINAL horse through the postfix;
            // the next retry keeps the newer assignment. Control: no reassignment; the retry clears A's owned target. Afterwards the owner retires.
            foreach (var reassign in new[] { true, false })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                c.Combat.Own(w.Horse, a, c.Combat.Generation);
                var custody = ProductionCustody(c);
                var custodyMounted = ReferenceEquals(PatchBridge.CombatFor(w.Horse.Commands), c.Combat) ? "pair (owner " + custody.OwnerStateForHarness + ")" : "other";
                UnitCommands.RunPostfix = (container, command, fromQueue) => PatchMethods.InternalRunPostfix(container, command, fromQueue);
                var outstanding = ""; var handled = false; var custodyAfterDismount = "?"; var ownerAfterDismount = "?"; var rosterAfterDismount = false;
                var failedRetry = false; var listEmptyWhileOwed = false; var ownerAfterFailedRetry = "?"; var observer = "?"; var listAtCommand = -1;
                UnitEntityData horseAfterCommand = null;
                c.BeforeDrive = () =>
                {
                    var settling = c.Combat.SettlingProposalForDiagnostics;
                    if (handled || settling == null || settling.OutstandingEffects.Count == 0) return;
                    handled = true;
                    outstanding = string.Join(",", settling.OutstandingEffects.ToArray());
                    c.Combat.SimulateDismountForHarness();   // the controller's Dismounting handler (Cancel) and its relationship view
                    custody.DismountForHarness();            // the production Dismount tail: coordinator Unmounted, CompleteOwnershipAttempt
                    ownerAfterDismount = custody.OwnerStateForHarness;
                    custodyAfterDismount = ReferenceEquals(PatchBridge.CombatFor(w.Horse.Commands), c.Combat) ? "the pair (detached owner)" : PatchBridge.CombatFor(w.Horse.Commands) == null ? "none" : "other";
                    rosterAfterDismount = PatchBridge.Pairs.Any(pair => ReferenceEquals(pair.Combat, c.Combat));
                    // an intervening failed retry: a one-shot READ fault on the horse's target (the supersession read after the list was emptied)
                    var readFaults = 0;
                    Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = state => state == w.Horse.CombatState && readFaults++ == 0;
                    failedRetry = !c.Combat.TrySettleOutstandingCleanup() && readFaults > 0;
                    Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = null;   // the engine recovers before the native command
                    listEmptyWhileOwed = settling.OutstandingEffects.Count == 0 && settling.EffectMountTargetClear && c.Combat.HasOutstandingCleanup && c.Combat.SettlingProposalCount == 1 &&
                        ReferenceEquals(w.Horse.CombatState.ManualTarget, a);
                    custody.RefreshOwnership();               // the per-frame retirement check: the owed clear retains the detached owner
                    ownerAfterFailedRetry = custody.OwnerStateForHarness;
                    observer = ReferenceEquals(PatchBridge.CombatFor(w.Horse.Commands), c.Combat) && c.Combat.ObservesOutstandingActor(w.Horse.Commands) ? "custody lookup (PairOwner " + custody.OwnerStateForHarness + ")" :
                        PatchBridge.Pairs.Any(pair => ReferenceEquals(pair.Combat, c.Combat) && pair.Combat.ObservesOutstandingActor(w.Horse.Commands)) ? "roster enumeration" : "none";
                    listAtCommand = settling.OutstandingEffects.Count;
                    if (reassign) w.Horse.Commands.Run(new HarnessPlayerCommand(a, UnitCommand.CommandType.Standard));
                    horseAfterCommand = w.Horse.CombatState.ManualTarget;
                };
                var r = RunProvisional(c, w.Target, null, () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState);
                c.BeforeDrive = null;
                Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null; Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = null;
                c.Combat.SweepFirstTickProposal();
                custody.RefreshOwnership();
                var ownerAfterSettle = custody.OwnerStateForHarness;
                var custodyAfterSettle = PatchBridge.CombatFor(w.Horse.Commands) == null ? "none" : "still held";
                var rosterAfterSettle = PatchBridge.Pairs.Any(pair => ReferenceEquals(pair.Combat, c.Combat));
                UnitCommands.RunPostfix = null; PatchBridge.Service = null;
                var ok = r.Decision.StartsWith("committed:replacement:") && outstanding == "mount-target" && custodyMounted == "pair (owner Active)" && ownerAfterDismount == "Detached" &&
                    custodyAfterDismount == "the pair (detached owner)" && rosterAfterDismount && failedRetry && listEmptyWhileOwed && ownerAfterFailedRetry == "Detached" && listAtCommand == 0 &&
                    observer.StartsWith("custody lookup") && c.Combat.SettlingProposalCount == 0 && !c.Combat.ObservesOutstandingActor(w.Horse.Commands) && !c.Combat.HasOutstandingCleanup &&
                    ownerAfterSettle == "none" && custodyAfterSettle == "none" && rosterAfterSettle &&
                    (reassign ? ReferenceEquals(horseAfterCommand, a) && ReferenceEquals(w.Horse.CombatState.ManualTarget, a) : w.Horse.CombatState.ManualTarget == null);
                Check(ok, "R11-F8 (round 15) " + (reassign ? "same-target reassignment on the original horse after dismount, routed by the production InternalRunPostfix over the production custody lookup" : "control, no reassignment") +
                    ": deferred clear failed (" + outstanding + "); custody while mounted " + custodyMounted + "; after the dismount the owner is " + ownerAfterDismount + " and the custody lookup for the horse returns " +
                    custodyAfterDismount + ", world roster keeps the pair " + rosterAfterDismount + "; an intervening retry failed on a target read " + failedRetry + " leaving the diagnostic list empty while the clear is owed " +
                    listEmptyWhileOwed + " (owner " + ownerAfterFailedRetry + "); observer selected " + observer + " with " + listAtCommand + " listed effects" +
                    (reassign ? "; the newer native-shaped command assigned " + Name(horseAfterCommand) : "") + "; after the successful retry the original horse's target is " + Name(w.Horse.CombatState.ManualTarget) +
                    "; outstanding requests " + c.Combat.SettlingProposalCount + "; owner " + ownerAfterSettle + ", custody " + custodyAfterSettle + ", roster entry kept " + rosterAfterSettle + " (no longer observing)");
            }

            // R14-F4 (round 15): a one-shot read fault on the horse's manual target at the first read AFTER B's transfer (the generation changed).
            // The commit staged the exact actors and targets before the transfer, so the transfer completes; the initial application throws at its
            // first engine read, is contained, and the request is retained from its effect FLAGS (the diagnostic list is empty). The next retry
            // clears only A's retired target (control) or keeps a later same-target reassignment on the horse routed by the production postfix.
            foreach (var reassign in new[] { true, false })
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var a = RetainedOrderAndHold(c);
                var w = c.World;
                c.Combat.Own(w.Horse, a, c.Combat.Generation);
                var custody = ProductionCustody(c);
                UnitCommands.RunPostfix = (container, command, fromQueue) => PatchMethods.InternalRunPostfix(container, command, fromQueue);
                var gA = c.Combat.Generation; var fired = false; var handled = false;
                long genAtCapture = -1; UnitEntityData intentAtCapture = null, horseAtCapture = null; var owedAtCapture = "?"; var listAtCapture = -1; var registered = false;
                UnitEntityData horseAfterCommand = null;
                c.BeforeDrive = () =>
                {
                    if (handled || c.Combat.Generation == gA) return;
                    handled = true;
                    var settling = c.Combat.SettlingProposalForDiagnostics;
                    genAtCapture = c.Combat.Generation; intentAtCapture = c.Combat.IntentTarget; horseAtCapture = w.Horse.CombatState.ManualTarget;
                    registered = settling != null && c.Combat.SettlingProposalCount == 1 && settling.EffectMountTargetClear && settling.CommittedGeneration == genAtCapture;
                    owedAtCapture = settling == null ? "unowned" : settling.DescribeEffects();
                    listAtCapture = settling == null ? -1 : settling.OutstandingEffects.Count;
                    if (reassign) w.Horse.Commands.Run(new HarnessPlayerCommand(a, UnitCommand.CommandType.Standard));
                    horseAfterCommand = w.Horse.CombatState.ManualTarget;
                };
                var r = RunProvisional(c, w.Target, null, () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = state =>
                {
                    if (fired || state != w.Horse.CombatState || c.Combat.Generation == gA) return false;
                    fired = true; return true;
                });
                c.BeforeDrive = null;
                Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetReadFault = null;
                c.Combat.SweepFirstTickProposal();
                UnitCommands.RunPostfix = null; PatchBridge.Service = null;
                var ok = fired && r.Decision.StartsWith("committed:replacement:") && r.Decision.Contains("effects=outstanding(mount-target") && genAtCapture != gA &&
                    ReferenceEquals(intentAtCapture, w.Target) && ReferenceEquals(horseAtCapture, a) && registered && listAtCapture == 0 && owedAtCapture.StartsWith("outstanding(mount-target") &&
                    c.Combat.SettlingProposalCount == 0 && !c.Combat.HasOutstandingCleanup &&
                    (reassign ? ReferenceEquals(horseAfterCommand, a) && ReferenceEquals(w.Horse.CombatState.ManualTarget, a)
                              : w.Horse.CombatState.ManualTarget == null && c.Combat.Generation == genAtCapture && ReferenceEquals(c.Combat.IntentTarget, w.Target) &&
                                ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target) && !w.Horse.HoldState);
                Check(ok, "R14-F4 (round 15) " + (reassign ? "later same-target reassignment on the horse" : "control, no reassignment") + ": target read fault after the transfer fired " + fired +
                    "; decision effects=" + Field(r.Decision, "effects") + " (" + (r.Decision.StartsWith("committed:replacement:") ? "accepted" : r.Decision) + "); generation " + gA + " -> " + genAtCapture +
                    ", intent " + Name(intentAtCapture) + "; request retained from its flags " + registered + " owing " + owedAtCapture + " with " + listAtCapture + " listed; horse target before the retry " +
                    Name(horseAtCapture) + (reassign ? ", newer assignment " + Name(horseAfterCommand) : "") + "; after the retry horse target " + Name(w.Horse.CombatState.ManualTarget) +
                    ", rider target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState + ", generation " + c.Combat.Generation + ", outstanding requests " + c.Combat.SettlingProposalCount);
            }

            // Round 14 (R12-F4, honest scope): these cases execute the CONTROLLER drain (TrySettleOutstandingCleanup) that the entry points reach; they
            // do not execute CompositionRoot.SetEnabled(false), its UpdateCore drain or Dispose (the composition root needs the game's services). The
            // entry points' order (drain before readiness, the disabled-update drain, both drain verdicts consumed) is checked on the compiled
            // product by scripts/Test-MountedAlpha2LifecycleContract.ps1.
            // R12-F4 (round 13): outstanding owned cleanup and teardown readiness. Between a failed engine operation and its retry, disable and unload
            // ask the controller's teardown drain (TrySettleOutstandingCleanup: what MountedLifecycleSubscriber.HandleModDisable runs through
            // MountedWorldService.SettleOutstandingPairCleanup before the readiness check, and again after the disable's own cleanup, all before
            // patches are removed); HasOutstandingCleanup is the term HasRetainedPairCommands and the pair's teardown predicate read. While the
            // engine still fails the request is a retained, retryable refusal; when it succeeds the next drain completes it on the original actor.
            // Cases: A's mount-target clear (dismounted between the requests); an accepted order's unfinished carrier cleanup (StopMoving fault, no
            // successor); the same with an already-steering Move successor (completion: the successor keeps its path, no stop is needed).
            foreach (var kase in new[] { "deferred target clear", "unfinished carrier cleanup", "unfinished carrier, steering successor" })
            {
                bool ok; string detail;
                if (kase == "deferred target clear")
                {
                    var c = FirstTickPair(0.12f, 1f / 60f);
                    var a = RetainedOrderAndHold(c);
                    var w = c.World;
                    c.Combat.Own(w.Horse, a, c.Combat.Generation);
                    var outstandingAfterCommit = false; var disableReady = true; var unloadReady = true; var retained = false; var handled = false;
                    Action arm = () => Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = state => state == w.Horse.CombatState;
                    c.BeforeDrive = () =>
                    {
                        if (handled || !c.Combat.HasOutstandingCleanup) return;
                        handled = true;
                        outstandingAfterCommit = true;
                        arm(); disableReady = c.Combat.TrySettleOutstandingCleanup();
                        c.Combat.SimulateDismountForHarness();
                        arm(); unloadReady = c.Combat.TrySettleOutstandingCleanup();
                        retained = c.Combat.HasOutstandingCleanup && ReferenceEquals(w.Horse.CombatState.ManualTarget, a);
                        arm();
                    };
                    var r = RunProvisional(c, w.Target, null, arm);
                    c.BeforeDrive = null;
                    Kingmaker.Controllers.Combat.UnitCombatState.ManualTargetWriteFault = null;
                    var finalReady = c.Combat.TrySettleOutstandingCleanup();
                    ok = r.Decision.StartsWith("committed:replacement:") && outstandingAfterCommit && !disableReady && !unloadReady && retained && finalReady && !c.Combat.HasOutstandingCleanup &&
                        w.Horse.CombatState.ManualTarget == null;
                    detail = "outstanding after the commit " + outstandingAfterCommit + "; teardown drain (the controller step SettleOutstandingPairCleanup runs for disable) while the clear fails: settled " + disableReady + "; the same drain again after the dismount (the unload path) while it fails: settled " +
                        unloadReady + " (retained " + retained + ", horse target " + Name(a) + "); after the engine recovers: ready " + finalReady + ", original horse target " + Name(w.Horse.CombatState.ManualTarget);
                }
                else
                {
                    var withSuccessor = kase.Contains("successor");
                    var c = FirstTickPair(1.2f, 1f / 60f, false, false);
                    PriorSelectionAndHold(c);
                    var w = c.World;
                    var click = ClickTick(c, w.Target);
                    var wrapper = click.Order;
                    for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                    Frame(c);
                    UnitMoveTo successor = null; var faulted = false; var outstandingAtFault = false; var retainedRecords = -1; PathSnap snapBefore = null;
                    var disableReady = false; var unloadReady = false;
                    Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                    c.ActionStage = () => w.Target.Descriptor.State.IsDead = true;
                    c.BeforeDrive = () =>
                    {
                        if (faulted || wrapper == null || !wrapper.IsFinished) return;
                        faulted = true;
                        outstandingAtFault = c.Combat.HasOutstandingCleanup;
                        retainedRecords = c.Combat.RetainedCarrierCleanupCount;
                        if (withSuccessor)
                        {
                            var at = w.Horse.Position;
                            successor = new UnitMoveTo(at + new Vector3(-0.5f, 0f, 0f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(-0.5f, 0f, 0f) }) };
                            w.Horse.Commands.Run(successor);
                            TakePath(w.Horse, successor);
                            snapBefore = Snap(w.Horse);
                        }
                        // the disable request, then the unload request, while the engine operation still fails (no successor: StopMoving throws)
                        if (!withSuccessor) Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                        disableReady = c.Combat.TrySettleOutstandingCleanup();
                        if (!withSuccessor) Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                        unloadReady = c.Combat.TrySettleOutstandingCleanup();
                        if (!withSuccessor) Kingmaker.View.UnitEntityView.StopMovingFault = view => view == w.Horse.View;
                    };
                    for (var i = 0; i < 3 && !faulted; i++) Frame(c);
                    c.BeforeDrive = null;
                    Kingmaker.View.UnitEntityView.StopMovingFault = null;
                    var finalReady = c.Combat.TrySettleOutstandingCleanup();
                    var snapAfter = withSuccessor ? Snap(w.Horse) : null;
                    ok = faulted && outstandingAtFault && retainedRecords == 1 && finalReady && !c.Combat.HasOutstandingCleanup && c.Combat.RetainedCarrierCleanupCount == 0 &&
                        wrapper.CarrierCleanupState == "interrupted=True,slotRemoved=True,motionSettled=True" &&
                        (withSuccessor ? disableReady && unloadReady && SuccessorPathKept(snapBefore, snapAfter, successor) && ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move), successor)
                            : !disableReady && !unloadReady && w.Horse.View.AgentASP.PathOwner == null);
                    detail = "outstanding at the fault " + outstandingAtFault + " (retained carrier records " + retainedRecords + "); controller teardown drain (as for disable): settled " + disableReady + "; again (as for unload): settled " + unloadReady +
                        "; after the engine recovers: ready " + finalReady + ", carrier cleanup {" + wrapper.CarrierCleanupState + "}" + (withSuccessor ? ", successor path {" + snapAfter + "}" : ", agent path owner " + Id(w.Horse.View.AgentASP.PathOwner));
                    if (successor != null) { successor.Interrupt(); w.Horse.Commands.RemoveFinishedAndUpdateQueue(); }
                }
                Check(ok, "R12-F4 (round 13, offline readiness members) " + kase + ": " + detail);
            }

            // R12-F5 (round 13): the click no longer rereads the speed after its grant (the frame-bound classification is gone): the grant's reads are
            // the click's only MaxSpeed reads. A one-shot MaxSpeed failure armed for the first read after the grant is therefore not consumed by the
            // click (the accepted order is the clean click's); the perpendicular first debit is then decided on its actual tick (refused before any
            // movement or debit). Control: the failure on the grant's pricing read keeps native's decision (prior target and Hold, nothing installed).
            {
                var probeC = FirstTickPair(0.095f, 1f / 60f, false, false, false, true);
                PriorSelectionAndHold(probeC);
                probeC.World.Horse.CombatState.Cooldown.MoveAction = 3f - 1f / 60f;
                var readsBefore = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                bool pst, psm; string preason;
                probeC.Pair.PairedMountGrantedMetres(probeC.Context, out pst, out psm, out preason);
                var grantReads = Kingmaker.View.UnitMovementAgent.MaxSpeedReads - readsBefore;
                readsBefore = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                var probeClick = ClickTick(probeC, probeC.World.Target);
                var clickReads = Kingmaker.View.UnitMovementAgent.MaxSpeedReads - readsBefore;
                foreach (var kase in new[] { "failure armed after the grant read, perpendicular", "control: failure on the grant's pricing read" })
                {
                    var c = FirstTickPair(0.095f, 1f / 60f, false, false, false, true);
                    var a = PriorSelectionAndHold(c);
                    var w = c.World;
                    w.Horse.CombatState.Cooldown.MoveAction = 3f - 1f / 60f;
                    var moveBefore = w.Horse.CombatState.Cooldown.MoveAction; var gBefore = c.Combat.Generation; var holdBefore = w.Horse.HoldState;
                    var start = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                    Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead = kase.StartsWith("control") ? start + grantReads : start + grantReads + 1;
                    var click = ClickTick(c, w.Target);
                    var consumedAtClick = Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead < 0;
                    Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead = -1;
                    bool ok; string detail;
                    if (kase.StartsWith("control"))
                    {
                        ok = consumedAtClick && !click.Admitted && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState == holdBefore && c.Combat.Generation == gBefore && LiveMountCarrier(c) == null;
                        detail = "consumed by the click's grant read " + consumedAtClick + "; click " + click.Decision + "; target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + w.Horse.HoldState + ", generation " + c.Combat.Generation;
                    }
                    else
                    {
                        var order = click.Order;
                        var genAtClick = c.Combat.Generation;
                        Frame(c);
                        w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                        var startPos = w.Horse.Position;
                        for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                        var decision = c.Combat.LastFirstTickDecision;
                        ok = grantReads == clickReads && probeClick.Admitted && !consumedAtClick && click.Admitted && click.Decision == probeClick.Decision && order != null && genAtClick != gBefore &&
                            decision.StartsWith("refused:final-ordinary-tick-no-affordable-strike-position") && Kingmaker.Utility.GeometryUtils.MechanicsDistance(startPos, w.Horse.Position) < 1e-6f &&
                            w.Horse.CombatState.Cooldown.MoveAction == moveBefore && order.StruckTargets.Count == 0 && order.IsFinished && LiveMountCarrier(c) == null;
                        detail = "MaxSpeed reads: the grant " + grantReads + ", the whole click " + clickReads + "; armed for the first read after the grant: consumed at the click " + consumedAtClick + "; click " + click.Decision +
                            " (the clean click: " + probeClick.Decision + "; generation " + gBefore + " -> " + genAtClick + ", target " + Name(w.Rider.CombatState.ManualTarget) + "); first debit " + decision.Substring(0, Math.Min(70, decision.Length)) +
                            "; displacement " + Kingmaker.Utility.GeometryUtils.MechanicsDistance(startPos, w.Horse.Position).ToString("0.######") + " m, mount Move " + moveBefore.ToString("0.#####") + " -> " +
                            w.Horse.CombatState.Cooldown.MoveAction.ToString("0.#####") + " s, strikes " + order.StruckTargets.Count + ", carrier " + Id(LiveMountCarrier(c));
                    }
                    Check(ok, "R12-F5 (round 13) " + kase + ": " + detail);
                }
            }

            // ===== round 14 =====
            // R12-F2 (round 14): a larger ordinary grant keeps an executable shortened strike. Same frame (1/60 s), perpendicular heading, 0.085 m
            // endpoint, sight from x >= 0.035 and z <= 0.065: 5/512 s (single final tick) and 1/60 s + 0.001 s (ordinary branch, whose full tick
            // would stop blind near (0.075, 0.075)): each returns the shortened duration, stops at the visible point and strikes once.
            foreach (var kase in new[] { "5/512 s allowance", "1/60 s + 0.001 s allowance" })
            {
                var c = FirstTickPair(0.035f, 1f / 60f, false, false, false, true);
                var w = c.World;
                LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.035f || from.z > 0.065f;
                w.Horse.CombatState.Cooldown.MoveAction = 3f - (kase.StartsWith("5/512") ? 5f / 512f : 1f / 60f + 0.001f);
                var a = PriorSelectionAndHold(c);
                var r = ShortOrdinaryRun(c, a, () => w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f));
                var returned = r.MoveAfterDecision - r.MoveBefore;
                var ok = r.Pending && r.Strikes == 1 && returned < 1f / 60f - 1e-4f && r.Stop.x >= 0.035f && r.Stop.z <= 0.065f &&
                    (kase.StartsWith("5/512") ? r.Decision.StartsWith("committed:order:final-ordinary-tick:") : r.Decision.StartsWith("committed:order:ordinary-movement-shortened-strike("));
                Check(ok, "R12-F2 (round 14) " + kase + ", perpendicular: first debit " + r.Decision.Substring(0, Math.Min(60, r.Decision.Length)) + "...; returned and debited " + returned.ToString("0.######") +
                    " s (frame 1/60 s); stop (" + r.Stop.x.ToString("0.#####") + "," + r.Stop.z.ToString("0.#####") + "); mount Move " + r.MoveBefore.ToString("0.#####") + " -> " + r.MoveFinal.ToString("0.#####") + " s; strikes " + r.Strikes);
            }

            // ===== round 15 (lead's movement findings) =====
            // R12-F2 (round 15): the tighter sight window (z <= 0.052) with the same frame, heading and endpoint: the full 1/60 s tick and its
            // waypoint-distance candidate stop blind; the interior-stop search of the current steered tick finds the visible stop at the 1 mm
            // resolution and returns that duration. The 5/512 s allowance stops at (0.04395, 0.04395), visible, as before.
            foreach (var kase in new[] { "5/512 s allowance", "1/60 s + 0.001 s allowance" })
            {
                var c = FirstTickPair(0.035f, 1f / 60f, false, false, false, true);
                var w = c.World;
                LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.035f || from.z > 0.052f;
                w.Horse.CombatState.Cooldown.MoveAction = 3f - (kase.StartsWith("5/512") ? 5f / 512f : 1f / 60f + 0.001f);
                var a = PriorSelectionAndHold(c);
                var r = ShortOrdinaryRun(c, a, () => w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f));
                var returned = r.MoveAfterDecision - r.MoveBefore;
                var ok = r.Pending && r.Strikes == 1 && r.Stop.x >= 0.035f && r.Stop.z <= 0.052f && (kase.StartsWith("5/512")
                    ? r.Decision.StartsWith("committed:order:final-ordinary-tick:") && Math.Abs(returned - 5f / 512f) < 1e-5f
                    : r.Decision.StartsWith("committed:order:ordinary-movement-shortened-strike(first-tick-interior(") && returned < 1f / 60f - 1e-4f);
                Check(ok, "R12-F2 (round 15) sight z <= 0.052, " + kase + ", perpendicular: first debit " + r.Decision.Substring(0, Math.Min(150, r.Decision.Length)) + "...; returned and debited " +
                    returned.ToString("0.######") + " s (frame 1/60 s); executed stop (" + r.Stop.x.ToString("0.#####") + "," + r.Stop.z.ToString("0.#####") + "); mount Move " + r.MoveBefore.ToString("0.#####") +
                    " -> " + r.MoveAfterDecision.ToString("0.#####") + " -> " + r.MoveFinal.ToString("0.#####") + " s; strikes " + r.Strikes);
            }

            // R13-F5 (round 15): the connected faster turning step (physical 18 m/s, native 9 m/s; the physical reservation binds, an accepted order)
            // through a later NearTheEnd tick whose native endpoint completion would exceed the physical remainder: after the first debit the mount
            // stands 65 mm before the path's end on its last segment, aligned, with 43.9 mm of physical step travel left. A 43.9 mm shift would end
            // 21.1 mm from the end, inside native's 50 mm completion distance (travel 65 mm); the adapter shortens the tick so the shift stays
            // outside it, the settled travel stays within the step and the next tick has no lawful movement left (native's denial branch completes
            // the movement; the order ends with the force-finish disposition, no strike: the end is the only visible part). Control: 70 mm left,
            // the 65 mm completion is lawful (the mount reaches the end and strikes).
            foreach (var kase in new[] { "endpoint 65 mm ahead, 43.9 mm of physical step travel left", "control: endpoint 65 mm ahead, 70 mm left" })
            {
                var control = kase.StartsWith("control");
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var firstDebit = c.Combat.LastFirstTickDecision;
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || order.IsFinished || order.StruckTargets.Count > 0)
                {
                    ok = false;
                    detail = "setup: first debit " + firstDebit + "; path " + (points != null) + "; order " + (order == null ? "none" : order.IsFinished ? "finished" : "struck");
                }
                else
                {
                    var end = points[points.Count - 1];
                    c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);   // the first tick's travel is settled before the pose is set
                    var from = w.Horse.Position;
                    double dx = end.x - from.x, dz = end.z - from.z, dl = Math.Sqrt(dx * dx + dz * dz);
                    var dir = new UnityEngine.Vector2((float)(dx / dl), (float)(dz / dl));
                    var pose = new Vector3(end.x - dir.x * 0.065f, end.y, end.z - dir.y * 0.065f);
                    w.Horse.Position = pose; w.Rider.Position = pose;
                    agent.MoveDirection = dir; agent.NextPointIndex = points.Count - 1; agent.NearTheEnd = true;
                    var left = control ? 0.07f : 0.0439f;
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    c.Pair.movementState.SetStepLedger(w.Horse, nativeBefore, 1.524f - left);
                    var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(f, end) > 0.02f;
                    var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                    Frame(c);
                    var tickLine = w.LastMountTick ?? "";
                    var after = w.Horse.Position;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, after);
                    var toEndAfter = Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, end);
                    var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var stalls = w.MountStalls;
                    // Round 18 (R16-F3): later ticks keep a verified positive displacement of at least 0.01 mm inside the completion margin, so the
                    // step may move up to the 0.1 mm margin in a few short ticks before native's denial branch ends it; the bound is unchanged
                    // (observed: 2 later frames, 0.1 mm of extra travel, then the native force-finish).
                    var laterFrames = 0;
                    for (var i = 0; i < 10 && !order.IsFinished && order.StruckTargets.Count == 0; i++) { Frame(c); laterFrames++; }
                    var strikes = order.StruckTargets.Count;
                    var finalPos = w.Horse.Position;
                    var physicalFinal = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    ok = firstDebit.StartsWith("committed:order:") && Field(line, "finalStepPossible") == "False" && physicalAfter <= 1.524f + 1e-4f && nativeAfter <= 1.524f + 1e-4f && (control
                        ? Math.Abs(travel - 0.065f) < 1e-3f && toEndAfter < 1e-4f && strikes == 1
                        : travel <= left + 1e-4f && toEndAfter >= PairedAttackClickProbe.NativeLastSegmentArrivalMetres && tickLine.Contains("stepExecutable(") && strikes == 0 && order.IsFinished &&
                          order.NativeApproachEndedCount == 1 && laterFrames <= 3 && Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, finalPos) <= PairedAttackClickProbe.StepMarginMetres + 1e-5f &&
                          Kingmaker.Utility.GeometryUtils.MechanicsDistance(finalPos, end) >= PairedAttackClickProbe.NativeLastSegmentArrivalMetres && physicalFinal <= 1.524f + 1e-5f);
                    detail = "first debit " + firstDebit.Substring(0, Math.Min(45, firstDebit.Length)) + "...; pose " + pose + " (" + (Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, end) * 1000f).ToString("0.###") +
                        " mm before the end " + end + ", NearTheEnd); physical step travel " + physicalBefore.ToString("0.######") + " -> " + physicalAfter.ToString("0.######") + " m (left " + left.ToString("0.####") +
                        " m), native counter " + nativeBefore.ToString("0.######") + " -> " + nativeAfter.ToString("0.######") + " m; returned duration " + Field(tickLine, "physicalDelta") + " s" +
                        (tickLine.Contains("stepExecutable(") ? " (" + tickLine.Substring(tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal)) + ")" : "") + "; after native completion: position " + after +
                        " (travel " + (travel * 1000f).ToString("0.###") + " mm, " + (toEndAfter * 1000f).ToString("0.###") + " mm from the end); mount Move " + moveBefore.ToString("0.#####") + " -> " +
                        w.Horse.CombatState.Cooldown.MoveAction.ToString("0.#####") + " s; later ticks: " + laterFrames + " frames, stalls " + stalls + " -> " + w.MountStalls + ", extra travel " +
                        (Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, finalPos) * 1000f).ToString("0.######") + " mm, final position " + finalPos + " (" +
                        (Kingmaker.Utility.GeometryUtils.MechanicsDistance(finalPos, end) * 1000f).ToString("0.######") + " mm from the end, physical travel " + physicalFinal.ToString("0.######") + " m); disposition " +
                        (strikes == 1 ? "strike" : order.IsFinished ? "ended " + order.EndCause + " (native force-finish " + order.NativeApproachEndedCount + "), no strike" : "live");
                }
                Check(ok, "R13-F5 (round 15) faster turning step, later NearTheEnd tick, " + kase + ": " + detail);
            }

            // R14-F2 (round 15): the step-counter classification is carried with the priced grant (the same speed observations). A one-shot MaxSpeed
            // failure armed for the first read after the successful pricing of the 0.140625 m equal-speed step is not consumed by the click (its
            // reads equal the grant's), the click is still the provisional request (finalStepPossible) and the perpendicular first debit is then
            // decided on its own tick: refused before any movement, the prior generation, target, both Holds and both balances unchanged.
            {
                Func<Connected> make = () => { var made = FirstTickPair(0.086f, 1f / 64f, false, false, true); made.Pair.movementState.SetStepLedger(made.World.Horse, 1.524f - 0.140625f, 1.524f - 0.140625f); return made; };
                var probeC = make();
                PriorSelectionAndHold(probeC);
                var readsBefore = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                bool pst, psm; string preason;
                probeC.Pair.PairedMountGrantedMetres(probeC.Context, out pst, out psm, out preason);
                var grantReads = Kingmaker.View.UnitMovementAgent.MaxSpeedReads - readsBefore;
                readsBefore = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                var probeClick = ClickTick(probeC, probeC.World.Target);
                var clickReads = Kingmaker.View.UnitMovementAgent.MaxSpeedReads - readsBefore;
                var c = make();
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                var gBefore = c.Combat.Generation; var riderHoldBefore = w.Rider.HoldState; var horseHoldBefore = w.Horse.HoldState;
                var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var start = Kingmaker.View.UnitMovementAgent.MaxSpeedReads;
                Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead = start + grantReads + 1;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var consumedAtClick = Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead < 0;
                var genAtClick = c.Combat.Generation; var targetAtClick = w.Rider.CombatState.ManualTarget; var holdsAtClick = w.Rider.HoldState == riderHoldBefore && w.Horse.HoldState == horseHoldBefore;
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                var startPos = w.Horse.Position;
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var decision = c.Combat.LastFirstTickDecision;
                var consumedLater = Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead < 0;
                Kingmaker.View.UnitMovementAgent.MaxSpeedFaultAtRead = -1;
                var displacement = Kingmaker.Utility.GeometryUtils.MechanicsDistance(startPos, w.Horse.Position);
                var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                var ok = grantReads == clickReads && probeClick.Admitted && click.Admitted && !consumedAtClick && Field(line, "finalStepPossible") == "True" && genAtClick == gBefore &&
                    ReferenceEquals(targetAtClick, a) && holdsAtClick && decision.StartsWith("refused:") && displacement < 1e-6f && nativeAfter == nativeBefore && physicalAfter == physicalBefore &&
                    c.Combat.Generation == gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Rider.HoldState == riderHoldBefore && w.Horse.HoldState == horseHoldBefore &&
                    order != null && order.IsFinished && order.StruckTargets.Count == 0;
                Check(ok, "R14-F2 (round 15) one-shot MaxSpeed failure armed after the step's successful pricing (0.140625 m, equal speeds), perpendicular: MaxSpeed reads: the grant " + grantReads +
                    ", the whole click " + clickReads + "; failure consumed at the click " + consumedAtClick + " (later " + consumedLater + "); click " + click.Decision + " (finalStepPossible " + Field(line, "finalStepPossible") +
                    "); generation at the click " + gBefore + " -> " + genAtClick + ", target " + Name(targetAtClick) + " (prior " + Name(a) + "), Holds unchanged " + holdsAtClick + "; first debit " +
                    decision.Substring(0, Math.Min(110, decision.Length)) + "; displacement " + displacement.ToString("0.######") + " m; native counter " + nativeBefore.ToString("0.######") + " -> " +
                    nativeAfter.ToString("0.######") + " m, physical " + physicalBefore.ToString("0.######") + " -> " + physicalAfter.ToString("0.######") + " m; generation " + c.Combat.Generation +
                    ", target " + Name(w.Rider.CombatState.ManualTarget) + ", rider Hold " + w.Rider.HoldState + ", horse Hold " + w.Horse.HoldState + "; strikes " + (order == null ? 0 : order.StruckTargets.Count));
            }

            // R14-F3 (round 15): a fresh equal-speed step (1.524 m on both counters: the native counter binds, so the click is a provisional request)
            // to a route several frames long while the mount runs an older in-range Swift action. The existing accepted-work classification lets the
            // compatible Swift coexist (the request is not refused and not retired); the Swift finishes and the original click moves and strikes
            // once. Control: an older competing mount order still refuses the step request (prior order kept). The refused final step is the R12-F1
            // perpendicular case above.
            foreach (var kase in new[] { "older in-range Swift on the mount", "control: older competing mount order" })
            {
                var competing = kase.StartsWith("control");
                var c = FirstTickPair(0.4f, 1f / 60f, false, false, true);
                var w = c.World;
                var other = new UnitEntityData("hostile-swift") { Position = new Vector3(0.5f, 0f, -0.5f) };
                other.View.Corpulence = 0.5f;
                var older = new HarnessPlayerCommand(other, competing ? UnitCommand.CommandType.Standard : UnitCommand.CommandType.Swift);
                w.Horse.Commands.Run(older);
                var a = PriorSelectionAndHold(c);
                var gBefore = c.Combat.Generation;
                var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order ?? w.Rider.Commands.Standard as MountedPairAttackCommand;
                var decisionAtClick = c.Combat.LastFirstTickDecision;
                for (var i = 0; i < 2; i++) Frame(c);
                var olderLive = !older.IsFinished;
                var carrierWhileOlder = LiveMountCarrier(c) != null;
                bool ok; string detail;
                if (competing)
                {
                    ok = click.Admitted && Field(line, "finalStepPossible") == "True" && decisionAtClick == "refused:mount-holds-live-work" && olderLive && (order == null || order.IsFinished) &&
                        c.Combat.Generation == gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && ReferenceEquals(w.Horse.Commands.GetCommand(UnitCommand.CommandType.Standard), older);
                    detail = "click " + click.Decision + " (finalStepPossible " + Field(line, "finalStepPossible") + "); request " + decisionAtClick + "; older order live " + olderLive + "; generation " + gBefore + " -> " +
                        c.Combat.Generation + ", target " + Name(w.Rider.CombatState.ManualTarget);
                }
                else
                {
                    older.FinishSuccess(); w.Horse.Commands.RemoveFinishedAndUpdateQueue();
                    var stops = new List<string>(); var last = w.Horse.Position;
                    for (var i = 0; i < 30 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        Frame(c);
                        if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(last, w.Horse.Position) > 1e-6f) { last = w.Horse.Position; stops.Add("(" + last.x.ToString("0.####") + "," + last.z.ToString("0.####") + ")"); }
                    }
                    var decision = c.Combat.LastFirstTickDecision;
                    ok = click.Admitted && Field(line, "finalStepPossible") == "True" && decisionAtClick.StartsWith("pending:") && olderLive && order != null && order.StruckTargets.Count == 1 &&
                        stops.Count >= 2 && decision.StartsWith("committed:replacement:") && c.Combat.Generation != gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target) &&
                        c.Pair.movementState.AllocationMetresStepped(w.Horse) <= 1.524f + 1e-4f;
                    detail = "click " + click.Decision + " (finalStepPossible " + Field(line, "finalStepPossible") + ", grant " + Field(line, "mountGrantedMetres") + " m, route " + Field(line, "routeMetres") +
                        " m); request at the click " + decisionAtClick + "; Swift live for two frames " + olderLive + " (carrier issued meanwhile " + carrierWhileOlder + "); Swift finished; first debit " +
                        decision.Substring(0, Math.Min(60, decision.Length)) + "...; executed stops " + string.Join(" ", stops.ToArray()) + "; native counter " + nativeBefore.ToString("0.####") + " -> " +
                        c.Pair.movementState.AllocationMetresStepped(w.Horse).ToString("0.####") + " m; generation " + gBefore + " -> " + c.Combat.Generation + "; strikes " + (order == null ? 0 : order.StruckTargets.Count);
                }
                Check(ok, "R14-F3 (round 15) fresh equal-speed multi-frame step, " + kase + ": " + detail);
            }

            // R13-F3 (round 15): (a) the accepted override-route order's carrier ends out of sight with movement left and another visible stretch,
            // and on that same tick the target drifts 0.8 m (beyond the 0.75 m re-path distance): the loss of the issued approach is resolved first
            // (force-finish disposition), so the drift issues no second carrier. (b) The native-accepted ordinary rider click (no override route):
            // its issued carrier ends out of sight with another route available; the same disposition, no re-path. Both: one carrier issued, no
            // further debit, no strike, the rider's Standard unspent, and the player's newer ground order then owns the mount's Move slot.
            foreach (var kase in new[] { "override route, target drifts 0.8 m on the tick the carrier ends", "native-accepted ordinary rider click" })
            {
                var drift = kase.StartsWith("override");
                var c = FirstTickPair(0.6f, 1f / 60f, false, drift);
                var a = drift ? PriorSelectionAndHold(c) : null;
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                var endpoint = Vector3.zero; var closed = false; var drifted = false;
                // (a) a band around the route-bound carrier's endpoint closes after the commit; (b) the native-accepted carrier approaches the target
                // to its approach radius through the blind stretch x < 1.2 (it ends there, blind) while x >= 1.2 is visible and within reach.
                w.PathfindUnforcedMoves = !drift;
                LineOfSightGeometry.Instance.Blocked = drift ? (Func<Vector3, Vector3, bool>)((from, to) => from.x < 0.6f || closed && Math.Abs(from.x - endpoint.x) < 0.12f)
                    : (from, to) => from.x < 1.2f;
                MountedPairAttackCommand order = null;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                string firstDebit = "-";
                if (drift)
                {
                    c.BeforeDrive = () =>
                    {
                        if (!closed && c.Combat.LastFirstTickDecision.StartsWith("committed")) closed = true;
                        c.ActionStage = () =>
                        {
                            var carrier = order?.DelegatedMove;
                            if (!drifted && closed && carrier != null && carrier.IsFinished) { w.Target.Position = w.Target.Position + new Vector3(0f, 0f, 0.8f); drifted = true; }
                        };
                    };
                    var r = RunProvisional(c, w.Target, null, () =>
                    {
                        w.Horse.MoveRestricted = false;
                        order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                        endpoint = ((UnitMoveTo)LiveMountCarrier(c)).ApproachPoint;
                    }, 40);
                    firstDebit = r.Decision;
                    order = order ?? r.Order;
                }
                else
                {
                    var click = ClickTick(c, w.Target, false);
                    order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                    for (var i = 0; i < 40 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        if (order.DelegatedMove != null) { closed = true; endpoint = order.DelegatedMove.ApproachPoint; }
                        Frame(c);
                    }
                    firstDebit = "native-accepted click (admitted override " + click.Admitted + ", route-bound " + (order != null && order.RouteBound) + "; carrier ticks " + c.CarrierTicks + ", ineligible frames " +
                        c.IneligibleCarrierFrames + ", carrier " + Id(order?.DelegatedMove) + ", observation " + c.Combat.LastStockAttackObservation + ")";
                }
                c.BeforeDrive = null;
                var moveAtEnd = w.Horse.CombatState.Cooldown.MoveAction;
                Frame(c); Frame(c);
                var moveAfter = w.Horse.CombatState.Cooldown.MoveAction;
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var endedNatively = order != null && order.IsFinished && order.NativeApproachEndedCount == 1 && order.EndCause == MountedAttackEndCause.Cancelled;
                var carrierCleared = LiveMountCarrier(c) == null;
                var at = w.Horse.Position;
                var successor = new UnitMoveTo(at + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, -0.5f) }) };
                w.Horse.Commands.Run(successor);
                Frame(c);
                var slot = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                var ok = closed && (!drift || drifted) && endedNatively && carriers == 1 && order.StruckTargets.Count == 0 && moveAfter == moveAtEnd && carrierCleared &&
                    w.Rider.CombatState.Cooldown.StandardAction == standardBefore && ReferenceEquals(slot, successor) && (drift ? order.RouteBound : !order.RouteBound);
                Check(ok, "R13-F3 (round 15) " + kase + ": " + firstDebit.Substring(0, Math.Min(drift ? 90 : 400, firstDebit.Length)) + "; carrier endpoint " + endpoint + ", sight closed around it " + closed +
                    (drift ? ", target drifted on the ending tick " + drifted : "") + "; mount at " + at + "; carriers issued " + carriers + "; disposition " +
                    (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + "(" + order.EndCauseDetail + ") (native force-finish disposition " + order.NativeApproachEndedCount + ")" : "live") + "; mount Move " +
                    moveAtEnd.ToString("0.####") + " -> " + moveAfter.ToString("0.####") + " s after the end; rider Standard " + standardBefore + " -> " + w.Rider.CombatState.Cooldown.StandardAction +
                    "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + "; the player's newer ground order owns the Move slot " + ReferenceEquals(slot, successor));
            }

            // ===== round 16 (owner authorization 2026-11-28a section 3: R13-F5, R15-F1, R15-F2, R15-F3) =====
            Func<string, string> selectedOf = text => { var at = (text ?? "").IndexOf("waypoint#", StringComparison.Ordinal); if (at < 0) return "?"; var end = text.IndexOfAny(new[] { '=', ',', ';' }, at); return text.Substring(at, (end < 0 ? text.Length : end) - at); };
            Func<string, string> returnedOf = line => { var delta = Field(line ?? "", "physicalDelta"); var arrow = delta.IndexOf("->", StringComparison.Ordinal); return arrow < 0 ? delta : delta.Substring(arrow + 2); };

            // R13-F5 / R15-F2 (round 16): a LATER delegated step tick of the accepted faster turning step (physical 18 m/s, native 9 m/s: the
            // physical reservation binds) through the real TickDelegated and the connected mover. After the first debit the agent's path is set to
            // the reviewer's polyline from the mount's settled position (cursor on the first waypoint; the engine stand-in for a native path).
            // (a) Multi-skip: path (0,0) -> (0.100,-0.065) -> (0.192,-0.144) -> (0.300,0), heading (0.6,0.8), 720 deg/s, m_Speed 18 m/s, 1.279 m of
            // physical step travel used (0.245 m left): the full permitted tick would skip both intermediate waypoints, cross the final plane and
            // snap 0.300 m (1.579 m). The executable tick stays below the second waypoint's skip distance: native selects waypoint #2 (interior),
            // no completion, settled travel within the 1.524 m step. (b) Interior corner: path (0,0) -> (0.30,0) -> (0.12,0.05), facing the first
            // waypoint, 0.10 m left: native cannot skip the 0.30 m waypoint, so the final plane does not apply; the full 0.10 m is executed.
            foreach (var kase in new[] { "multi-skip, 0.245 m of physical step travel left", "interior corner, 0.10 m left" })
            {
                var corner = kase.StartsWith("interior");
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var firstDebit = c.Combat.LastFirstTickDecision;
                var agent = w.Horse.View.AgentASP;
                bool ok; string detail;
                if (agent.Path == null || order == null || order.IsFinished || order.StruckTargets.Count > 0)
                {
                    ok = false;
                    detail = "setup: first debit " + firstDebit + "; path " + (agent.Path != null) + "; order " + (order == null ? "none" : order.IsFinished ? "finished" : "struck");
                }
                else
                {
                    c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);   // the first tick's travel is settled before the path is set
                    var o = w.Horse.Position;
                    var offsets = corner ? new[] { 0f, 0f, 0.30f, 0f, 0.12f, 0.05f } : new[] { 0f, 0f, 0.100f, -0.065f, 0.192f, -0.144f, 0.300f, 0f };
                    var points = agent.Path.vectorPath;
                    points.Clear();
                    for (var i = 0; i < offsets.Length; i += 2) points.Add(new Vector3(o.x + offsets[i], o.y, o.z + offsets[i + 1]));
                    var end = points[points.Count - 1];
                    agent.NextPointIndex = 1; agent.NextWaypoint = new UnityEngine.Vector2(points[1].x, points[1].z); agent.NearTheEnd = false;
                    agent.MoveDirection = corner ? new UnityEngine.Vector2(1f, 0f) : new UnityEngine.Vector2(0.6f, 0.8f);
                    agent.CombatAngularSpeed = 720f; agent.FollowPrecomputedPath = true; agent.CurrentSpeedField = agent.MaxSpeed;
                    var left = corner ? 0.10f : 0.245f;
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    c.Pair.movementState.SetStepLedger(w.Horse, nativeBefore, 1.524f - left);
                    var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => true;   // no strike interferes with the measured tick
                    var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                    Frame(c);
                    var tickLine = w.LastMountTick ?? "";
                    var after = w.Horse.Position;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(o, after);
                    var toEndAfter = Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, end);
                    var cursorAfter = agent.NextPointIndex;
                    var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    float returned; float.TryParse(returnedOf(tickLine), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out returned);
                    for (var i = 0; i < 6 && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    var selected = selectedOf(tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal))));
                    ok = firstDebit.StartsWith("committed:order:") && tickLine.Contains("stepExecutable(") && physicalAfter <= 1.524f + 1e-5f && nativeAfter <= 1.524f + 1e-5f &&
                        travel <= left + 1e-5f && returned > 0f && (corner
                        ? selected == "waypoint#1(interior)" && cursorAfter == 1 && travel > 0.0999f && Math.Abs(returned - left / 18f) < 1e-6f
                        : selected == "waypoint#2(interior)" && cursorAfter == 2 && toEndAfter > PairedAttackClickProbe.NativeLastSegmentArrivalMetres && travel > 0.15f && returned < left / 18f);
                    detail = "first debit " + firstDebit.Substring(0, Math.Min(45, firstDebit.Length)) + "...; later tick from " + o + ": returned duration " + returned.ToString("R") + " s (permitted " +
                        (left / 18f).ToString("R") + " s), native selects " + selected + " (cursor after the tick " + cursorAfter + "), settled stop " + after + " (travel " + travel.ToString("0.######") +
                        " m, " + toEndAfter.ToString("0.######") + " m from the end); physical step travel " + physicalBefore.ToString("0.######") + " -> " + physicalAfter.ToString("0.######") +
                        " m (left " + left.ToString("0.###") + " m), native counter " + nativeBefore.ToString("0.######") + " -> " + nativeAfter.ToString("0.######") + " m; mount Move " +
                        moveBefore.ToString("0.#####") + " -> " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.#####") + " s; disposition " +
                        (order.StruckTargets.Count > 0 ? "strike" : order.IsFinished ? "ended " + order.EndCause + " (native force-finish " + order.NativeApproachEndedCount + "), no strike" : "live") +
                        " [" + tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal))) + "]";
                }
                Check(ok, "R13-F5/R15-F2 (round 16) later delegated step tick, " + kase + ": " + detail);
            }

            // R15-F1 (round 16): an affordable provisional final-step click (equal speeds 9 m/s, 43.9 mm left on both counters: the native counter
            // binds, finalStepPossible), its carrier bound, then the horse displaced before the first debit so the aligned endpoint is 65 mm away;
            // sight only within 20 mm of the endpoint. The executable tick (before the decision) stops outside the completion distance (a 43.9 mm
            // shift would snap 65 mm), so the decision receives that executable 14.8 mm tick, which no longer makes the native counter final: the
            // step keeps its route checks, the displaced 65 mm route exceeds the 43.9 mm grant and the request refuses before any effect: returned
            // duration 0, generation, manual target, both Holds, the rider's and mount's commands and both step counters unchanged (before round
            // 16 the decision predicted the endpoint snap with the 43.9 mm allowance and committed, then the adapter shortened the approved tick to
            // a blind stop 50.1 mm away). Control: displaced to 40 mm,
            // the snap is affordable: the decision commits the replacement on the completed endpoint, the tick snaps there and the order strikes once.
            foreach (var kase in new[] { "endpoint displaced to 65 mm, 43.9 mm left on both counters", "control: endpoint displaced to 40 mm (affordable snap)" })
            {
                var control = kase.StartsWith("control");
                var c = FirstTickPair(0.03f, 1f / 64f, false, false, true);
                var a = PriorSelectionAndHold(c);
                var w = c.World;
                c.Pair.movementState.SetStepLedger(w.Horse, 1.524f - 0.0439f, 1.524f - 0.0439f);
                var gBefore = c.Combat.Generation; var riderHoldBefore = w.Rider.HoldState; var horseHoldBefore = w.Horse.HoldState;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                Frame(c);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var bound = p != null && p.Replacement && p.CarrierMove != null && ReferenceEquals(c.Pair.movementState.FirstTickGate, p);
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || !bound || !c.Combat.LastFirstTickDecision.StartsWith("pending"))
                {
                    ok = false;
                    detail = "setup: click " + click.Decision + "; decision " + c.Combat.LastFirstTickDecision + "; bound " + bound + "; path " + (points != null);
                }
                else
                {
                    var end = points[points.Count - 1];
                    var from = w.Horse.Position;
                    double dx = end.x - from.x, dz = end.z - from.z, dl = Math.Sqrt(dx * dx + dz * dz);
                    var dir = dl > 1e-6 ? new UnityEngine.Vector2((float)(dx / dl), (float)(dz / dl)) : new UnityEngine.Vector2(1f, 0f);
                    var gap = control ? 0.040f : 0.065f;
                    var pose = new Vector3(end.x - dir.x * gap, end.y, end.z - dir.y * gap);
                    w.Horse.Position = pose; w.Rider.Position = pose;
                    agent.MoveDirection = dir; agent.NextPointIndex = points.Count - 1; agent.NextWaypoint = new UnityEngine.Vector2(end.x, end.z);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(f, end) > 0.02f;
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                    var genAtClick = c.Combat.Generation; var targetAtClick = w.Rider.CombatState.ManualTarget;
                    var carrier = p.CarrierMove;
                    string decision = null, tickLine = null; var stop = pose; var nativeAtDecision = 0f; var physicalAtDecision = 0f;
                    c.BeforeDrive = () =>
                    {
                        if (decision != null || c.Combat.LastFirstTickDecision.StartsWith("pending")) return;
                        decision = c.Combat.LastFirstTickDecision; tickLine = w.LastMountTick ?? ""; stop = w.Horse.Position;
                        nativeAtDecision = c.Pair.movementState.AllocationMetresStepped(w.Horse); physicalAtDecision = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    };
                    for (var i = 0; i < 3 && decision == null; i++) Frame(c);
                    c.BeforeDrive = null;
                    decision = decision ?? "<none>"; tickLine = tickLine ?? "";
                    var mountMoveAfter = LiveMountCarrier(c);
                    for (var i = 0; i < 12 && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    // After the request is settled: the rider holds no live command other than (before the sweep) the refused provisional order.
                    var riderCommandAfter = w.Rider.Commands.Standard != null && !w.Rider.Commands.Standard.IsFinished ? w.Rider.Commands.Standard : null;
                    var strikes = order.StruckTargets.Count;
                    var displacement = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, stop);
                    var returned = decision.StartsWith("refused") ? 0f : (float)((nativeAtDecision - nativeBefore) / 9.0);
                    ok = click.Admitted && Field(line, "finalStepPossible") == "True" && genAtClick == gBefore && ReferenceEquals(targetAtClick, a);
                    if (control)
                        ok &= decision.StartsWith("committed:replacement:final-step-counter:") && strikes == 1 && Kingmaker.Utility.GeometryUtils.MechanicsDistance(stop, end) < 1e-4f &&
                            physicalAtDecision <= 1.524f + 1e-5f && nativeAtDecision <= 1.524f + 1e-5f && Math.Abs(displacement - gap) < 1e-3f && c.Combat.Generation != gBefore &&
                            ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target);
                    else
                        ok &= decision.StartsWith("refused:route-beyond-current-grant(") && tickLine.StartsWith("mountMove=first-tick-refused") && tickLine.Contains("stepExecutable(") &&
                            displacement < 1e-6f &&
                            nativeAtDecision == nativeBefore && physicalAtDecision == physicalBefore && w.Horse.CombatState.Cooldown.MoveAction == moveBefore && strikes == 0 && order.IsFinished &&
                            LiveMountCarrier(c) == null && riderCommandAfter == null && mountMoveAfter == null && carrier.IsFinished && c.Combat.Generation == gBefore &&
                            ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Rider.HoldState == riderHoldBefore && w.Horse.HoldState == horseHoldBefore;
                    detail = "click " + click.Decision + " (grant " + Field(line, "mountGrantedMetres") + " m, route " + Field(line, "routeMetres") + " m, finalStepPossible " + Field(line, "finalStepPossible") +
                        "); displaced before the first debit to " + pose + ", " + (gap * 1000f).ToString("0.#") + " mm before the endpoint " + end + "; first debit " + decision.Substring(0, Math.Min(control ? 120 : 400, decision.Length)) +
                        "; tick {" + tickLine.Substring(0, Math.Min(control ? 120 : 260, tickLine.Length)) + "}; returned duration " + returned.ToString("R") + " s; selected " + selectedOf(decision) +
                        "; settled stop " + stop + " (displacement " + displacement.ToString("0.######") + " m, " + Kingmaker.Utility.GeometryUtils.MechanicsDistance(stop, end).ToString("0.######") +
                        " m from the end); native counter " + nativeBefore.ToString("0.######") + " -> " + nativeAtDecision.ToString("0.######") + " m, physical travel " + physicalBefore.ToString("0.######") +
                        " -> " + physicalAtDecision.ToString("0.######") + " m; mount Move " + moveBefore + " -> " + w.Horse.CombatState.Cooldown.MoveAction + " s; generation " + gBefore + " -> " + c.Combat.Generation +
                        "; target " + Name(w.Rider.CombatState.ManualTarget) + " (prior " + Name(a) + "); rider Hold " + riderHoldBefore + " -> " + w.Rider.HoldState + ", horse Hold " + horseHoldBefore + " -> " +
                        w.Horse.HoldState + "; rider live command after settlement " + Id(riderCommandAfter) + ", mount carrier after the decision " + Id(mountMoveAfter) + " (provisional carrier finished " +
                        carrier.IsFinished + "); disposition " +
                        (strikes == 1 ? "one strike" : "no strike, order ended " + order.IsFinished + " (" + order.EndCause + ")");
                }
                Check(ok, "R15-F1 (round 16) provisional final-step click, " + kase + ": " + detail);
            }

            // R15-F3 (round 16): the same post-binding blind arrival stripe under ordinary grants on opposite sides of the final-tick branch. The
            // accepted ordinary order (9 m/s, 1/60 s frame: 0.15 m per tick), aligned, its endpoint 0.13 m away (sight from x = 0.08, arrival radius
            // 0.05 m); after binding sight closes on a 2 mm stripe 35 mm before the endpoint (inside the arrival radius), the endpoint visible. The
            // 0.14 m grant (final ordinary tick) and the 0.16 m grant (ordinary branch) both refuse the blind arrival path before any movement.
            // Visible-route control: the same orders without the stripe commit and strike once. Lawful-shortened-stop control (sight from
            // x = 0.035, z <= 0.052, perpendicular heading: the full tick stops blind, a visible interior stop exists, the route is visible): both
            // grants commit on the shortened stop (returned duration below the grant) and strike once.
            foreach (var grant in new[] { 0.14f, 0.16f })
                foreach (var kase in new[] { "blind stripe 35 mm before the endpoint", "visible-route control", "lawful-shortened-stop control" })
                {
                    var shortenedCase = kase.StartsWith("lawful");
                    var stripe = kase.StartsWith("blind");
                    var c = FirstTickPair(shortenedCase ? 0.035f : 0.08f, 1f / 60f, false, false, false, true);
                    var w = c.World;
                    if (shortenedCase) LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.035f || from.z > 0.052f;
                    w.Horse.CombatState.Cooldown.MoveAction = 3f - grant / 9f;
                    var a = PriorSelectionAndHold(c);
                    var endpoint = Vector3.zero; var toEndpoint = float.NaN;
                    var r = ShortOrdinaryRun(c, a, () =>
                    {
                        var path = w.Horse.View.AgentASP.Path == null ? null : w.Horse.View.AgentASP.Path.vectorPath;
                        if (path != null) { endpoint = path[path.Count - 1]; toEndpoint = Kingmaker.Utility.GeometryUtils.MechanicsDistance(w.Horse.Position, endpoint); }
                        if (stripe) { var e = endpoint; LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.08f || Math.Abs(from.x - (e.x - 0.035f)) <= 0.001f; }
                        if (shortenedCase) w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                    });
                    var returned = r.MoveAfterDecision - r.MoveBefore;
                    var moved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(r.Start, r.Stop);
                    var finalBranch = grant <= 9f / 60f;
                    var ok = r.Pending && r.Bound;
                    if (stripe)
                        ok &= r.Decision.StartsWith(finalBranch ? "refused:final-ordinary-tick-route-arrival-not-admitted(" : "refused:route-arrival-not-admitted(") && moved < 1e-6f && returned == 0f &&
                            r.Strikes == 0 && Math.Abs(toEndpoint - 0.13f) < 1e-3f;
                    else if (!shortenedCase)
                        ok &= r.Decision.StartsWith(finalBranch ? "committed:order:final-ordinary-tick:" : "committed:order:ordinary-movement") && r.Decision.Contains("remaining-route(") && r.Strikes == 1 &&
                            Math.Abs(toEndpoint - 0.13f) < 1e-3f;
                    else
                        ok &= r.Decision.StartsWith(finalBranch ? "committed:order:final-ordinary-tick:first-tick-interior(" : "committed:order:ordinary-movement-shortened-strike(first-tick-interior(") &&
                            r.Strikes == 1 && returned < grant / 9f - 1e-4f && r.Stop.x >= 0.035f && r.Stop.z <= 0.052f && r.Decision.Contains("remaining-route(");
                    Check(ok, "R15-F3 (round 16) ordinary grant " + grant.ToString("0.00") + " m (" + (finalBranch ? "final ordinary tick" : "ordinary branch") + "), " + kase + ": endpoint " + endpoint +
                        " (" + toEndpoint.ToString("0.#####") + " m from the bound mount); first debit " + r.Decision.Substring(0, Math.Min(stripe ? 200 : 110, r.Decision.Length)) + "...; returned and debited " +
                        returned.ToString("0.######") + " s (grant " + (grant / 9f).ToString("0.######") + " s, frame 1/60 s); selected " + selectedOf(r.Decision) + "; executed stop (" + r.Stop.x.ToString("0.#####") + "," +
                        r.Stop.z.ToString("0.#####") + "), moved " + moved.ToString("0.######") + " m; mount Move " + r.MoveBefore.ToString("0.#####") + " -> " + r.MoveAfterDecision.ToString("0.#####") + " -> " +
                        r.MoveFinal.ToString("0.#####") + " s; strikes " + r.Strikes + "; order " + (r.Order == null ? "-" : r.Order.IsFinished ? "ended (" + r.Order.EndCause + ")" : "live"));
                }

            // ===== round 17 (R16-F2, R16-F3, R16-F4) =====
            // R16-F4 (round 17): the decision works in the adapter's own float duration. Restricted single tick of 0.0139f s, MaxSpeed 9, agent
            // speed (m_Speed) 8: native's strict skip threshold is 8 x 0.0139f = 0.1111999974 m. After binding the carrier's and agent's path is
            // origin -> interior waypoint (0.1112f, 0) -> bent end (0.1112f, 0.06); sight only at the end (3 mm). The interior waypoint is retained
            // at 0.0139f (the tick stops on the interior segment, blind): refused before any effect or debit (before round 17 the duration
            // reconstructed from metres skipped it and approved the visible end snap). The adjacent 0.0140f tick legitimately skips the waypoint:
            // predicted and executed waypoint (the end), duration, stop, effects and debit agree, one strike.
            foreach (var frame in new[] { 0.0139f, 0.0140f })
            {
                var skips = frame > 0.01395f;
                var c = FirstTickPair(0.10f, frame);
                PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var w1 = new Vector3(0.1112f, 0f, 0f); var w2 = new Vector3(0.1112f, 0f, 0.06f);
                var mutated = false;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    var carrier = LiveMountCarrier(c) as UnitMoveTo;
                    if (agent.Path == null || carrier == null || carrier.ForcedPath == null) return;
                    var origin = w.Horse.Position;
                    foreach (var list in new[] { agent.Path.vectorPath, carrier.ForcedPath.vectorPath }) { list.Clear(); list.Add(origin); list.Add(w1); list.Add(w2); }
                    agent.NextPointIndex = 1; agent.NextWaypoint = new UnityEngine.Vector2(w1.x, w1.z); agent.MoveDirection = new UnityEngine.Vector2(1f, 0f);
                    agent.CurrentSpeedField = 8f; agent.FollowPrecomputedPath = true;
                    LineOfSightGeometry.Instance.Blocked = (f, t) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(f, w2) > 0.003f;
                    mutated = origin.x == 0f && origin.z == 0f;
                });
                var approved = ParseFloat(Field(r.Decision, "approvedTick"));
                var debit = r.AfterDecision.MountMove - r.Before.MountMove;
                var executedStop = c.HorseAtDecision;
                var threshold = 8f * frame;
                bool ok;
                if (!skips)
                    ok = mutated && RefusedKeptPrior(c, r, "no-affordable-strike-position") && r.Decision.Contains("waypoint#1=") && r.Decision.Contains("(interior)") &&
                        !r.Decision.Contains("snapped=True") && debit == 0f && r.Order.StruckTargets.Count == 0;
                else
                    ok = mutated && r.PendingOk && r.Decision.StartsWith("committed:replacement:first-tick(") && r.Decision.Contains("waypoint#2=") && r.Decision.Contains("snapped=True") &&
                        approved == frame && debit == approved && Kingmaker.Utility.GeometryUtils.MechanicsDistance(executedStop, w2) < 1e-6f && r.Order.StruckTargets.Count == 1 &&
                        c.Combat.Generation != r.GenBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, w.Target);
                Check(ok, "R16-F4 (round 17) restricted tick " + frame.ToString("R") + " s, MaxSpeed 9, agent speed 8 (skip threshold " + threshold.ToString("R") + " m), interior waypoint " +
                    w1.x.ToString("R") + " m, bent end " + w2 + ": " + (skips ? "native skips the waypoint" : "native keeps the waypoint") + "; decision " + r.Decision.Substring(0, Math.Min(skips ? 260 : 330, r.Decision.Length)) +
                    "...; approved duration " + (float.IsNaN(approved) ? "none" : approved.ToString("R")) + " s; executed stop " + executedStop + " (displacement " + r.Displacement.ToString("0.######") +
                    " m); debit " + debit.ToString("R") + " s; generation " + r.GenBefore + " -> " + c.Combat.Generation + ", target " + Name(w.Rider.CombatState.ManualTarget) + ", horse Hold " + r.HoldBefore +
                    " -> " + w.Horse.HoldState + "; strikes " + r.Order.StruckTargets.Count);
            }

            // R16-F2 (round 17): the displaced 65 mm fixture (provisional final-step click, 43.9 mm left on both counters, displaced before the
            // first debit to 65 mm before the endpoint) with the current pose blind, the arrival disc visible (sight within 20 mm of the endpoint)
            // and sight also open around 14 mm of travel. The live route and arrival geometry passes; the executable 14.8 mm tick contains a
            // visible stop at 14 mm, tested before the 65 mm destination cost is compared with the 43.9 mm grant: one strike from that shortened
            // stop, both balances within the step. (The R15-F1 65 mm case, every affordable stop blind, keeps its refusal.)
            {
                var c = FirstTickPair(0.03f, 1f / 64f, false, false, true);
                PriorSelectionAndHold(c);
                var w = c.World;
                c.Pair.movementState.SetStepLedger(w.Horse, 1.524f - 0.0439f, 1.524f - 0.0439f);
                var gBefore = c.Combat.Generation;
                var click = ClickTick(c, w.Target);
                var line = LastLine(c);
                var order = click.Order;
                Frame(c);
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || !c.Combat.LastFirstTickDecision.StartsWith("pending"))
                {
                    ok = false;
                    detail = "setup: click " + click.Decision + "; decision " + c.Combat.LastFirstTickDecision;
                }
                else
                {
                    var end = points[points.Count - 1];
                    var from = w.Horse.Position;
                    double dx = end.x - from.x, dz = end.z - from.z, dl = Math.Sqrt(dx * dx + dz * dz);
                    var dir = dl > 1e-6 ? new UnityEngine.Vector2((float)(dx / dl), (float)(dz / dl)) : new UnityEngine.Vector2(1f, 0f);
                    var pose = new Vector3(end.x - dir.x * 0.065f, end.y, end.z - dir.y * 0.065f);
                    w.Horse.Position = pose; w.Rider.Position = pose;
                    agent.MoveDirection = dir; agent.NextPointIndex = points.Count - 1; agent.NextWaypoint = new UnityEngine.Vector2(end.x, end.z);
                    var window = new Vector3(pose.x + dir.x * 0.014f, pose.y, pose.z + dir.y * 0.014f);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(f, end) > 0.02f && Kingmaker.Utility.GeometryUtils.MechanicsDistance(f, window) > 0.0006f;
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    string decision = null, tickLine = null; var stop = pose; var nativeAtDecision = 0f; var physicalAtDecision = 0f;
                    c.BeforeDrive = () =>
                    {
                        if (decision != null || c.Combat.LastFirstTickDecision.StartsWith("pending")) return;
                        decision = c.Combat.LastFirstTickDecision; tickLine = w.LastMountTick ?? ""; stop = w.Horse.Position;
                        nativeAtDecision = c.Pair.movementState.AllocationMetresStepped(w.Horse); physicalAtDecision = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    };
                    for (var i = 0; i < 3 && decision == null; i++) Frame(c);
                    c.BeforeDrive = null;
                    decision = decision ?? "<none>"; tickLine = tickLine ?? "";
                    for (var i = 0; i < 12 && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    var strikes = order.StruckTargets.Count;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, stop);
                    var approved = ParseFloat(Field(decision, "approvedTick"));
                    ok = click.Admitted && Field(line, "finalStepPossible") == "True" && decision.StartsWith("committed:replacement:ordinary-movement-shortened-strike(first-tick-interior(") &&
                        decision.Contains("remaining-route(") && strikes == 1 && Kingmaker.Utility.GeometryUtils.MechanicsDistance(stop, window) <= 0.0006f && travel < 0.0148f &&
                        nativeAtDecision <= 1.524f + 1e-5f && physicalAtDecision <= 1.524f + 1e-5f && Math.Abs(nativeAtDecision - nativeBefore - approved * 9f) < 1e-5f;
                    detail = "displaced to " + pose + " (65 mm before the endpoint " + end + "), pose blind, sight within 20 mm of the endpoint and 0.6 mm around " + window + "; first debit " +
                        decision.Substring(0, Math.Min(150, decision.Length)) + "...; approved duration " + approved.ToString("R") + " s (executable tick " + Field(tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal))), "tick") +
                        " s); shortened stop " + stop + " (travel " + travel.ToString("0.######") + " m); native counter " + nativeBefore.ToString("0.######") + " -> " + nativeAtDecision.ToString("0.######") +
                        " m, physical travel " + physicalBefore.ToString("0.######") + " -> " + physicalAtDecision.ToString("0.######") + " m; generation " + gBefore + " -> " + c.Combat.Generation + "; strikes " + strikes;
                }
                Check(ok, "R16-F2 (round 17) displaced 65 mm step, affordable visible stop at 14 mm of travel: " + detail);
            }

            // R16-F3 (round 17): a later delegated step tick of the accepted step (first debit as in the round-15 R13-F5 fixture; this tick at
            // MaxSpeed 9 = CurrentSpeedMps), aligned on the last segment. (a) 50.6 mm from the final waypoint with 8.9 mm of physical step travel
            // left: every scan sample from 8.9 mm down to 0.9 mm of shift would snap 50.6 mm; the interval below the last sample is resolved: a
            // positive duration (about 0.4 mm of shift) leaves the mount outside the completion margins, unsnapped, both balances within the step.
            // (b) 45 mm from the end (already inside the 50 mm completion radius): every positive tick would snap 45 mm > 8.9 mm: zero.
            // Round 19 (R18-F1): (c) 50.112 mm from the end: the lawful shifts lie between the 0.01 mm displacement floor and the 0.1 mm
            // completion margin (about 0.010-0.012 mm); the round-18 bisection sampled 0.014067 mm (too long) then 0.007033 mm (too short) and
            // discarded that interval. Every case records the returned duration, the actual unsnapped displacement, the floor and margin
            // compliance, both balances (physical step travel and native step counter) and the order disposition after the following frames:
            // a step tick that has no executable displacement meeting the floor returns zero, and native's denial ends the step promptly.
            foreach (var gap in new[] { 0.0506f, 0.05015f, 0.050112f, 0.045f })
            {
                var inside = gap < 0.05f;
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var firstDebit = c.Combat.LastFirstTickDecision;
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || order.IsFinished || order.StruckTargets.Count > 0)
                {
                    ok = false;
                    detail = "setup: first debit " + firstDebit;
                }
                else
                {
                    // The agent's path is set to an aligned last segment [pose, pose + gap] from the mount's settled position (the engine stand-in for
                    // a native path), so the carrier's own approach radius around its destination plays no part in this tick.
                    c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var pose = w.Horse.Position;
                    var dir = new UnityEngine.Vector2(1f, 0f);
                    var end = new Vector3(pose.x + gap, pose.y, pose.z);
                    points.Clear(); points.Add(pose); points.Add(end);
                    agent.SpeedFactor = 1f; agent.CurrentSpeedField = agent.MaxSpeed;
                    agent.MoveDirection = dir; agent.NextPointIndex = points.Count - 1; agent.NextWaypoint = new UnityEngine.Vector2(end.x, end.z); agent.NearTheEnd = false;
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    c.Pair.movementState.SetStepLedger(w.Horse, nativeBefore, 1.524f - 0.0089f);
                    var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => true;
                    Frame(c);
                    var tickLine = w.LastMountTick ?? "";
                    var after = w.Horse.Position;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, after);
                    var toEndAfter = Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, end);
                    var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var returned = ParseFloat(returnedOf(tickLine));
                    var floorMet = travel >= PairedAttackClickProbe.StepMinimumDisplacementMetres;
                    var marginMet = toEndAfter >= PairedAttackClickProbe.NativeLastSegmentArrivalMetres + PairedAttackClickProbe.StepMarginMetres;
                    // the following frames until the order ends (at most 6): each later tick's returned duration, then the disposition
                    var later = new List<string>();
                    for (var i = 0; i < 6 && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        var beforeLater = w.Horse.Position;
                        Frame(c);
                        later.Add(returnedOf(w.LastMountTick ?? "") + " s/" + (Kingmaker.Utility.GeometryUtils.MechanicsDistance(beforeLater, w.Horse.Position) * 1000f).ToString("0.######") + " mm");
                    }
                    var disposition = order.StruckTargets.Count > 0 ? "strike" : !order.IsFinished ? "live" : "ended (" + order.EndCause + "(" + order.EndCauseDetail + "); native force-finish disposition " + order.NativeApproachEndedCount + "; route revalidations " + order.RouteRevalidationCount + ", arrival contradictions " + order.RouteArrivalContradictionCount + "; rider Standard " + w.Rider.CombatState.Cooldown.StandardAction.ToString("0.###") + " s; mount Move " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.###") + " s)";
                    var nativeEnded = order.IsFinished && order.StruckTargets.Count == 0 && order.NativeApproachEndedCount == 1;
                    ok = firstDebit.StartsWith("committed:order:") && tickLine.Contains("stepExecutable(") && physicalAfter <= 1.524f + 1e-5f && nativeAfter <= 1.524f + 1e-5f && (inside
                        ? returned == 0f && travel < 1e-7f && physicalAfter == physicalBefore && nativeAfter == nativeBefore
                        : returned > 0f && travel > 0f && travel < 0.0006f && toEndAfter > PairedAttackClickProbe.NativeLastSegmentArrivalMetres + PairedAttackClickProbe.StepMarginMetres - 1e-5f);
                    // both balances advance by this tick alone (physical by the settled travel, native by the returned duration x MaxSpeed 9), and native's
                    // denial ends the order without a strike once no executable displacement meeting the floor remains
                    ok &= Math.Abs((physicalAfter - physicalBefore) - travel) < 1e-6f && Math.Abs((nativeAfter - nativeBefore) - returned * 9f) < 1e-6f && nativeEnded;
                    if (gap == 0.050112f) ok &= returned > 0f && floorMet && marginMet && toEndAfter > 1e-4f;
                    detail = "pose " + pose + " (" + (gap * 1000f).ToString("0.#") + " mm before the end " + end + "), 8.9 mm of physical step travel left, 9 m/s: returned duration " + returned.ToString("R") +
                        " s; stop " + after + " (travel " + (travel * 1000f).ToString("0.######") + " mm, " + (toEndAfter * 1000f).ToString("0.######") + " mm from the end, " + (toEndAfter < 1e-4f ? "snapped" : "unsnapped") +
                        "); physical step travel " + physicalBefore.ToString("0.######") + " -> " + physicalAfter.ToString("0.######") + " m, native counter " + nativeBefore.ToString("0.######") + " -> " +
                        nativeAfter.ToString("0.######") + " m (physical +" + ((physicalAfter - physicalBefore) * 1000f).ToString("0.######") + " mm, native +" + ((nativeAfter - nativeBefore) * 1000f).ToString("0.######") +
                        " mm, returned x 9 m/s = " + (returned * 9f * 1000f).ToString("0.######") + " mm); floor 0.01 mm met " + floorMet + ", completion margin 50.1 mm met " + marginMet +
                        "; later frames [" + string.Join("; ", later.ToArray()) + "]; disposition " + disposition + " [" + tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal))) + "]";
                }
                Check(ok, "R16-F3 (round 17), R18-F1 (round 19) later delegated step tick, " + (inside ? "inside the completion radius (every positive tick overspends)" : "lawful interval below the last scan sample, " + (gap * 1000f).ToString("0.###") + " mm") + ": " + detail);
            }

            // R19-F1 (round 20): the same connected later step tick at X = 256 m (adjacent float positions along +X are 0.030518 mm apart), both
            // speeds 9 m/s, native counter 0.762 m, the path's end 125 mm ahead on an aligned last segment. Physical step travel 1.523995 m
            // (about 0.005 mm left: the permitted shift rounds back onto the origin) and 1.523980 m (about 0.020 mm left: it rounds forward by
            // 0.030518 mm, beyond the remainder): no representable legal displacement, so zero is returned, neither balance moves and native's
            // denial ends the order. Positive control 1.523960 m (about 0.040 mm left): the stop rounds to one position spacing (0.030518 mm),
            // within the remainder and above the 0.01 mm floor: a positive duration moves the mount by exactly that representable displacement.
            foreach (var travelled in new[] { 1.523995f, 1.52398f, 1.52396f })
            {
                var control = travelled == 1.52396f;
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var firstDebit = c.Combat.LastFirstTickDecision;
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || order.IsFinished || order.StruckTargets.Count > 0)
                {
                    ok = false;
                    detail = "setup: first debit " + firstDebit;
                }
                else
                {
                    c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var settledPose = w.Horse.Position;
                    var offset = new Vector3(256f - settledPose.x, 0f, 0f);
                    var pose = new Vector3(256f, settledPose.y, settledPose.z);
                    w.Horse.Position = pose; w.Rider.Position = pose; w.Target.Position = w.Target.Position + offset;
                    var dir = new UnityEngine.Vector2(1f, 0f);
                    var end = new Vector3(pose.x + 0.125f, pose.y, pose.z);
                    points.Clear(); points.Add(pose); points.Add(end);
                    agent.SpeedFactor = 1f; agent.CurrentSpeedField = agent.MaxSpeed;
                    agent.MoveDirection = dir; agent.NextPointIndex = points.Count - 1; agent.NextWaypoint = new UnityEngine.Vector2(end.x, end.z); agent.NearTheEnd = false;
                    c.Pair.movementState.SetStepLedger(w.Horse, 0.762f, travelled);
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var physicalLeft = 1.524f - physicalBefore;
                    LineOfSightGeometry.Instance.Blocked = (f, t) => true;
                    Frame(c);
                    var tickLine = w.LastMountTick ?? "";
                    var after = w.Horse.Position;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, after);
                    var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var returned = ParseFloat(returnedOf(tickLine));
                    var later = new List<string>();
                    for (var i = 0; i < 6 && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        var beforeLater = w.Horse.Position;
                        Frame(c);
                        later.Add(returnedOf(w.LastMountTick ?? "") + " s/" + (Kingmaker.Utility.GeometryUtils.MechanicsDistance(beforeLater, w.Horse.Position) * 1000f).ToString("0.######") + " mm");
                    }
                    var disposition = order.StruckTargets.Count > 0 ? "strike" : !order.IsFinished ? "live" : "ended (" + order.EndCause + "(" + order.EndCauseDetail + "); native force-finish disposition " + order.NativeApproachEndedCount + "; route revalidations " + order.RouteRevalidationCount + ", arrival contradictions " + order.RouteArrivalContradictionCount + "; rider Standard " + w.Rider.CombatState.Cooldown.StandardAction.ToString("0.###") + " s; mount Move " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.###") + " s)";
                    var nativeEnded = order.IsFinished && order.StruckTargets.Count == 0 && order.NativeApproachEndedCount == 1;
                    // the no-move cases end on native's denial. The control's order ends within the moving frame through the product's target-drift repath:
                    // the fixture translated the target with the pose (256 m), which the order observes as target displacement beyond TargetRepathDistance
                    // (0.75 m) on its next approach tick; Repath -> BeginDelegatedMove revalidates the route from the current origin and grant, finds no
                    // legal route within the mount's remaining movement and ends the order (Cancelled, "validated approach expired"): the movement used
                    // stays spent, no strike, the rider's Standard unspent (owner amendment 1 terminal disposition), before any native force-finish.
                    ok = firstDebit.StartsWith("committed:order:") && tickLine.Contains("stepExecutable(") && (control ? order.IsFinished && order.StruckTargets.Count == 0 && order.EndCause == MountedAttackEndCause.Cancelled && order.EndCauseDetail == "validated approach expired" &&
                        order.NativeApproachEndedCount == 0 && order.RouteArrivalContradictionCount == 0 && w.Rider.CombatState.Cooldown.StandardAction == 0f : nativeEnded) &&
                        physicalAfter <= 1.524f + 1e-5f && nativeAfter <= 1.524f + 1e-5f &&
                        Math.Abs((physicalAfter - physicalBefore) - travel) < 1e-6f && Math.Abs((nativeAfter - nativeBefore) - returned * 9f) < 1e-6f && (control
                        ? returned > 0f && travel >= PairedAttackClickProbe.StepMinimumDisplacementMetres && travel <= physicalLeft && after.x > pose.x
                        : returned == 0f && travel == 0f && physicalAfter == physicalBefore && nativeAfter == nativeBefore);
                    detail = "pose (" + pose.x.ToString("R") + "," + pose.z.ToString("R") + "), end 125 mm ahead, 9 m/s, physical step travel " + physicalBefore.ToString("R") + " m (" + (physicalLeft * 1000f).ToString("0.######") +
                        " mm left), native counter " + nativeBefore.ToString("R") + " m: returned duration " + returned.ToString("R") + " s (x 9 m/s = " + (returned * 9f * 1000f).ToString("0.######") + " mm); stop (" + after.x.ToString("R") + "," +
                        after.z.ToString("R") + ") (settled travel " + (travel * 1000f).ToString("0.######") + " mm); physical step travel " + physicalBefore.ToString("R") + " -> " + physicalAfter.ToString("R") + " m, native counter " +
                        nativeBefore.ToString("R") + " -> " + nativeAfter.ToString("R") + " m; later frames [" + string.Join("; ", later.ToArray()) + "]; disposition " + disposition + " [" +
                        tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal))) + "]";
                }
                Check(ok, "R19-F1 (round 20) later delegated step tick at X = 256 m, " + (control ? "positive control (one representable position spacing within the remainder)" : "no representable legal displacement") + ": " + detail);
            }

            // R20-F1 (round 21): later delegated step ticks at X = 256 m whose path END lies within the physical remainder, through TickDelegated
            // (both speeds 9 m/s). (A) interior: an aligned interior waypoint at X = 256.3125 and the end at (0.0625, 0.05) m from the mount
            // (0.080039 m away); physical travel 1.424008 m (0.099992 m left), native counter 0.762 m: the full reservation would carry the mount
            // 0.1000061 m toward the interior waypoint (over the remainder); a shorter executable duration on the same interior waypoint is returned
            // and the settled travel stays within the remainder. (B) native-limited: a straight 0.125 m path, native counter 1.523995 m, physical
            // travel 0.762 m: the permitted 5.5631e-7 s would round the stop back onto X = 256: zero, no balance moves, native's denial ends the
            // order. (C) positive control: a straight path whose end is 0.08 m ahead, physical travel 1.424008 m, native counter 0.762 m: the
            // affordable completion executes normally (the permitted duration, the mount on the end, 0.08 m of physical travel).
            foreach (var kase in new[] { "A interior waypoint, affordable end", "B native counter nearly exhausted", "C affordable completion (control)" })
            {
                var c = FirstTickPair(0.095f, 1f / 60f, false, false, true);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.Horse.View.AgentASP.SpeedFactor = 2f;
                c.Pair.movementState.SetStepLedger(w.Horse, 0.687f, 1.374f);
                var click = ClickTick(c, w.Target);
                var order = click.Order;
                Frame(c);
                w.Horse.View.AgentASP.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                for (var i = 0; i < 4 && !c.Combat.LastFirstTickDecision.StartsWith("refused") && !c.Combat.LastFirstTickDecision.StartsWith("committed"); i++) Frame(c);
                var firstDebit = c.Combat.LastFirstTickDecision;
                var agent = w.Horse.View.AgentASP;
                var points = agent.Path == null ? null : agent.Path.vectorPath;
                bool ok; string detail;
                if (points == null || order == null || order.IsFinished || order.StruckTargets.Count > 0)
                {
                    ok = false;
                    detail = "setup: first debit " + firstDebit;
                }
                else
                {
                    c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var settledPose = w.Horse.Position;
                    var offset = new Vector3(256f - settledPose.x, 0f, 0f);
                    var pose = new Vector3(256f, settledPose.y, settledPose.z);
                    w.Horse.Position = pose; w.Rider.Position = pose; w.Target.Position = w.Target.Position + offset;
                    var dir = new UnityEngine.Vector2(1f, 0f);
                    points.Clear(); points.Add(pose);
                    if (kase.StartsWith("A")) { points.Add(new Vector3(256.3125f, pose.y, pose.z)); points.Add(new Vector3(pose.x + 0.0625f, pose.y, pose.z + 0.05f)); }
                    else points.Add(new Vector3(pose.x + (kase.StartsWith("B") ? 0.125f : 0.08f), pose.y, pose.z));
                    var end = points[points.Count - 1];
                    agent.SpeedFactor = 1f; agent.CurrentSpeedField = agent.MaxSpeed;
                    agent.MoveDirection = dir; agent.NextPointIndex = 1; agent.NextWaypoint = new UnityEngine.Vector2(points[1].x, points[1].z); agent.NearTheEnd = false;
                    if (kase.StartsWith("B")) c.Pair.movementState.SetStepLedger(w.Horse, 1.523995f, 0.762f);
                    else c.Pair.movementState.SetStepLedger(w.Horse, 0.762f, 1.424008f);
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var physicalLeft = 1.524f - physicalBefore;
                    var toEndBefore = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, end);
                    LineOfSightGeometry.Instance.Blocked = (f, t) => true;
                    Frame(c);
                    var tickLine = w.LastMountTick ?? "";
                    var after = w.Horse.Position;
                    var travel = Kingmaker.Utility.GeometryUtils.MechanicsDistance(pose, after);
                    var toEndAfter = Kingmaker.Utility.GeometryUtils.MechanicsDistance(after, end);
                    var physicalAfter = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var nativeAfter = c.Pair.movementState.AllocationMetresStepped(w.Horse);
                    var returned = ParseFloat(returnedOf(tickLine));
                    var stepPart = tickLine.Substring(Math.Max(0, tickLine.IndexOf("stepExecutable(", StringComparison.Ordinal)));
                    var selectedAt = stepPart.IndexOf(";selected=", StringComparison.Ordinal);
                    var selectedWaypoint = selectedAt < 0 ? "-" : stepPart.Substring(selectedAt + 10, Math.Min(20, stepPart.Length - selectedAt - 10)).Split(',')[0];
                    var later = new List<string>();
                    for (var i = 0; i < 6 && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                    {
                        var beforeLater = w.Horse.Position;
                        Frame(c);
                        later.Add(returnedOf(w.LastMountTick ?? "") + " s/" + (Kingmaker.Utility.GeometryUtils.MechanicsDistance(beforeLater, w.Horse.Position) * 1000f).ToString("0.######") + " mm");
                    }
                    var disposition = order.StruckTargets.Count > 0 ? "strike" : !order.IsFinished ? "live" : "ended (" + order.EndCause + "(" + order.EndCauseDetail + "); native force-finish disposition " + order.NativeApproachEndedCount + "; route revalidations " + order.RouteRevalidationCount + ", arrival contradictions " + order.RouteArrivalContradictionCount + "; rider Standard " + w.Rider.CombatState.Cooldown.StandardAction.ToString("0.###") + " s; mount Move " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.###") + " s)";
                    ok = firstDebit.StartsWith("committed:order:") && tickLine.Contains("stepExecutable(") && tickLine.Contains(";affordableEnd)") && physicalAfter <= 1.524f && nativeAfter <= 1.524f &&
                        Math.Abs((physicalAfter - physicalBefore) - travel) < 1e-6f && Math.Abs((nativeAfter - nativeBefore) - returned * 9f) < 1e-6f && order.StruckTargets.Count == 0;
                    // (A) ends within the moving frame through the product's target-drift repath (the fixture's 256 m translation of the target exceeds
                    // TargetRepathDistance): BeginDelegatedMove finds no legal route within the remaining movement: Cancelled, "validated approach expired",
                    // movement used spent, no strike, the rider's Standard unspent (owner amendment 1), no native force-finish.
                    if (kase.StartsWith("A")) ok &= returned > 0f && selectedWaypoint == "waypoint#1(interior)" && travel >= PairedAttackClickProbe.StepMinimumDisplacementMetres && travel <= physicalLeft && after.x > pose.x &&
                        order.IsFinished && order.StruckTargets.Count == 0 && order.EndCause == MountedAttackEndCause.Cancelled && order.EndCauseDetail == "validated approach expired" &&
                        order.NativeApproachEndedCount == 0 && order.RouteArrivalContradictionCount == 0 && w.Rider.CombatState.Cooldown.StandardAction == 0f;
                    else if (kase.StartsWith("B")) ok &= returned == 0f && travel == 0f && physicalAfter == physicalBefore && nativeAfter == nativeBefore && order.IsFinished && order.NativeApproachEndedCount == 1;
                    else ok &= returned > 0f && toEndAfter < 1e-6f && travel <= physicalLeft && Math.Abs(travel - toEndBefore) < 1e-6f;
                    detail = "pose (" + pose.x.ToString("R") + "," + pose.z.ToString("R") + "), end (" + end.x.ToString("R") + "," + end.z.ToString("R") + ") " + (toEndBefore * 1000f).ToString("0.######") + " mm away, 9 m/s, physical step travel " +
                        physicalBefore.ToString("R") + " m (" + (physicalLeft * 1000f).ToString("0.######") + " mm left), native counter " + nativeBefore.ToString("R") + " m: selected " + selectedWaypoint + "; returned duration " +
                        returned.ToString("R") + " s (x 9 m/s = " + (returned * 9f * 1000f).ToString("0.######") + " mm); stop (" + after.x.ToString("R") + "," + after.z.ToString("R") + ") (settled travel " + (travel * 1000f).ToString("0.######") +
                        " mm, " + (toEndAfter * 1000f).ToString("0.######") + " mm from the end); physical step travel " + physicalBefore.ToString("R") + " -> " + physicalAfter.ToString("R") + " m, native counter " + nativeBefore.ToString("R") +
                        " -> " + nativeAfter.ToString("R") + " m; later frames [" + string.Join("; ", later.ToArray()) + "]; disposition " + disposition + " [" + stepPart + "]";
                }
                Check(ok, "R20-F1 (round 21) later delegated step tick at X = 256 m with the end within the physical remainder, " + kase + ": " + detail);
            }

            // ===== round 18 (R16-F3, R17-F2) =====
            // R17-F2 (round 18): the restricted 0.0139f s threshold fixture (MaxSpeed 9, agent speed 8: threshold 0.1111999974 m) on a collinear path
            // origin -> (0.1112f, 0) -> (0.25, 0): native keeps the interior waypoint at every candidate duration, and a shift longer than 111.2 mm
            // carries the mount beyond it (no snap back on an interior segment; the cursor advances). Sight open only for 115-121 mm of travel: the
            // first-tick search evaluates the native settled stop beyond the waypoint and returns that duration; executed stop, selected waypoint,
            // debit and the strike agree. Blind control (no sight anywhere on the tick): refused before any effect or debit.
            foreach (var open in new[] { true, false })
            {
                var c = FirstTickPair(0.10f, 0.0139f);
                PriorSelectionAndHold(c);
                var w = c.World;
                var agent = w.Horse.View.AgentASP;
                var w1 = new Vector3(0.1112f, 0f, 0f); var w2 = new Vector3(0.25f, 0f, 0f);
                var mutated = false; var cursorAfter = -1;
                var r = RunProvisional(c, w.Target, null, () =>
                {
                    var carrier = LiveMountCarrier(c) as UnitMoveTo;
                    if (agent.Path == null || carrier == null || carrier.ForcedPath == null) return;
                    var origin = w.Horse.Position;
                    foreach (var list in new[] { agent.Path.vectorPath, carrier.ForcedPath.vectorPath }) { list.Clear(); list.Add(origin); list.Add(w1); list.Add(w2); }
                    agent.NextPointIndex = 1; agent.NextWaypoint = new UnityEngine.Vector2(w1.x, w1.z); agent.MoveDirection = new UnityEngine.Vector2(1f, 0f);
                    agent.CurrentSpeedField = 8f; agent.FollowPrecomputedPath = true;
                    LineOfSightGeometry.Instance.Blocked = (f, t) => !open || f.x < 0.115f || f.x > 0.121f || Math.Abs(f.z) > 0.001f;
                    mutated = origin.x == 0f && origin.z == 0f;
                    var previous = c.BeforeDrive;
                    c.BeforeDrive = () => { previous?.Invoke(); if (cursorAfter < 0 && !c.Combat.LastFirstTickDecision.StartsWith("pending")) cursorAfter = agent.NextPointIndex; };
                });
                var approved = ParseFloat(Field(r.Decision, "approvedTick"));
                var debit = r.AfterDecision.MountMove - r.Before.MountMove;
                var executedStop = c.HorseAtDecision;
                bool ok;
                if (open)
                    ok = mutated && r.PendingOk && r.Decision.StartsWith("committed:replacement:first-tick-interior(") && r.Decision.Contains("waypoint#1=") && r.Decision.Contains("(interior)") &&
                        r.Decision.Contains("passesWaypoint=True") && approved > 0f && approved < 0.0139f && debit == approved && executedStop.x > w1.x && executedStop.x >= 0.115f &&
                        executedStop.x <= 0.121f && Math.Abs(executedStop.x - ParseFloat(Field(r.Decision, "stopX"))) < 1e-6f && cursorAfter == 2 && r.Order.StruckTargets.Count == 1;
                else
                    ok = mutated && RefusedKeptPrior(c, r, "no-affordable-strike-position") && debit == 0f && r.Order.StruckTargets.Count == 0;
                Check(ok, "R17-F2 (round 18) restricted tick 0.0139 s, agent speed 8, MaxSpeed 9, collinear waypoint " + w1.x.ToString("R") + " m, " + (open ? "sight open 115-121 mm" : "blind control") +
                    ": decision " + r.Decision.Substring(0, Math.Min(open ? 330 : 160, r.Decision.Length)) + "...; approved duration " + (float.IsNaN(approved) ? "none" : approved.ToString("R")) + " s; executed stop " +
                    executedStop + " (stopX " + executedStop.x.ToString("R") + ", displacement " + r.Displacement.ToString("0.######") + " m, cursor after the tick " + cursorAfter + "); debit " + debit.ToString("R") +
                    " s; generation " + r.GenBefore + " -> " + c.Combat.Generation + ", target " + Name(w.Rider.CombatState.ManualTarget) + "; strikes " + r.Order.StruckTargets.Count);
            }

            // Round 16 (counter-final step, the R15-F3 rule for steps): the same post-binding blind arrival stripe under a step whose native counter
            // this tick makes final (equal speeds 9 m/s, 0.140625 m left on both counters, 1/64 s frame: the provisional request,
            // final-step-counter) and under a step whose physical reservation binds instead (physical 18 m/s, native 9 m/s, the same 0.140625 m
            // grant: not counter-final, the route branch). Endpoint 0.136 m away (sight from x = 0.086, arrival radius 0.05 m), aligned; after
            // binding sight closes on a 2 mm stripe 35 mm before the endpoint (inside the arrival radius), the endpoint visible. Both refuse the
            // blind arrival path before any movement (both step counters unchanged); the visible controls commit and strike once.
            foreach (var counterFinal in new[] { true, false })
                foreach (var stripe in new[] { true, false })
                {
                    var c = FirstTickPair(0.086f, 1f / 64f, false, false, true);
                    var a = PriorSelectionAndHold(c);
                    var w = c.World;
                    if (!counterFinal) w.Horse.View.AgentASP.SpeedFactor = 2f;
                    c.Pair.movementState.SetStepLedger(w.Horse, 1.524f - 0.140625f, 1.524f - 0.140625f);
                    var nativeBefore = c.Pair.movementState.AllocationMetresStepped(w.Horse); var physicalBefore = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    var gBefore = c.Combat.Generation; var horseHoldBefore = w.Horse.HoldState;
                    var click = ClickTick(c, w.Target);
                    var line = LastLine(c);
                    var order = click.Order;
                    Frame(c);
                    var path = w.Horse.View.AgentASP.Path == null ? null : w.Horse.View.AgentASP.Path.vectorPath;
                    var endpoint = path == null ? Vector3.zero : path[path.Count - 1];
                    var toEndpoint = Kingmaker.Utility.GeometryUtils.MechanicsDistance(w.Horse.Position, endpoint);
                    if (stripe) LineOfSightGeometry.Instance.Blocked = (from, to) => from.x < 0.086f || Math.Abs(from.x - (endpoint.x - 0.035f)) <= 0.001f;
                    var start = w.Horse.Position; string decision = null; var stop = start; var nativeAtDecision = 0f; var physicalAtDecision = 0f;
                    c.BeforeDrive = () =>
                    {
                        if (decision != null || c.Combat.LastFirstTickDecision.StartsWith("pending")) return;
                        decision = c.Combat.LastFirstTickDecision; stop = w.Horse.Position;
                        nativeAtDecision = c.Pair.movementState.AllocationMetresStepped(w.Horse); physicalAtDecision = c.Pair.movementState.AllocationMetresStepTravelled(w.Horse);
                    };
                    for (var i = 0; i < 3 && decision == null; i++) Frame(c);
                    c.BeforeDrive = null;
                    decision = decision ?? "<none>";
                    for (var i = 0; i < 12 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                    var strikes = order == null ? 0 : order.StruckTargets.Count;
                    var moved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, stop);
                    var ok = click.Admitted && order != null && Field(line, "finalStepPossible") == (counterFinal ? "True" : "False") && Math.Abs(toEndpoint - 0.136f) < 1e-3f;
                    if (stripe)
                        ok &= decision.StartsWith(counterFinal ? "refused:final-step-counter-route-arrival-not-admitted(" : "refused:route-arrival-not-admitted(") && moved < 1e-6f &&
                            nativeAtDecision == nativeBefore && physicalAtDecision == physicalBefore && strikes == 0 && order.IsFinished && LiveMountCarrier(c) == null &&
                            (!counterFinal || c.Combat.Generation == gBefore && ReferenceEquals(w.Rider.CombatState.ManualTarget, a) && w.Horse.HoldState == horseHoldBefore);
                    else
                        ok &= decision.StartsWith(counterFinal ? "committed:replacement:final-step-counter:" : "committed:") && decision.Contains("remaining-route(") && strikes == 1 &&
                            nativeAtDecision <= 1.524f + 1e-5f && physicalAtDecision <= 1.524f + 1e-5f;
                    Check(ok, "Round 16 step " + (counterFinal ? "whose native counter becomes final (equal speeds)" : "whose physical reservation binds (not counter-final)") + ", " +
                        (stripe ? "blind stripe 35 mm before the endpoint" : "visible control") + ": click " + click.Decision + " (grant " + Field(line, "mountGrantedMetres") + " m, finalStepPossible " +
                        Field(line, "finalStepPossible") + "); endpoint " + endpoint + " (" + toEndpoint.ToString("0.#####") + " m); first debit " + decision.Substring(0, Math.Min(stripe ? 200 : 120, decision.Length)) +
                        "...; moved " + moved.ToString("0.######") + " m to (" + stop.x.ToString("0.#####") + "," + stop.z.ToString("0.#####") + "); native counter " + nativeBefore.ToString("0.######") + " -> " +
                        nativeAtDecision.ToString("0.######") + " m, physical travel " + physicalBefore.ToString("0.######") + " -> " + physicalAtDecision.ToString("0.######") + " m; generation " + gBefore + " -> " +
                        c.Combat.Generation + "; target " + Name(w.Rider.CombatState.ManualTarget) + "; horse Hold " + horseHoldBefore + " -> " + w.Horse.HoldState + "; strikes " + strikes + "; order " +
                        (order == null ? "-" : order.IsFinished ? "ended (" + order.EndCause + ")" : "live"));
                }

            // R13-F3 (round 16): the native-accepted ordinary rider click (no override route) whose LIVE carrier has acquired its path and moved the
            // mount; the issued path is then cleared (installed CompleteMovement/Stop: Path released, no further request) while UnitMoveTo has not
            // reported finished, and on that same frame, before the rider's command tick, the target drifts 0.8 m (beyond the 0.75 m re-path
            // distance). The issued approach has ended out of reach: the force-finish disposition, one carrier, the movement already spent stays
            // spent, no strike, the rider's Standard unspent, and the player's newer ground order then owns the mount's Move slot. Control
            // (pending first path): the same click whose carrier has NOT acquired a path (native has not delivered one) when the target drifts is
            // not an ended approach: no native disposition, and the existing drift re-path still serves the order.
            // Round 16 follow-up: (b) the same with native's request-then-deliver and the delivered Path being the SAME pooled object the agent held
            // when the carrier was issued (A* PathPool reuse): the observed request resolving into a Path is the acquisition. The pending control's
            // request stays in flight (WantsToMove, no Path): not acquired.
            foreach (var kase in new[] { "native-accepted click, issued path cleared while the carrier is unfinished, target drifts 0.8 m",
                "native-accepted click, pooled Path object reused for the delivered path, issued path cleared, target drifts 0.8 m",
                "native-accepted click (rider-first), path delivered, debited and cleared before the rider observes it, target drifts 0.8 m",
                "native-accepted click (rider-first), pooled Path object reused without an observed pending frame, cleared before the rider observes it, target drifts 0.8 m",
                "native-accepted click (rider-first), path delivered, first tick denied (zero duration) and cleared before the rider observes it, target drifts 0.8 m",
                "pending-first-path control (request in flight), target drifts 0.8 m" })
            {
                // Round 17 (R13-F3): the unobserved cases clear the issued path in the action stage of the first frame whose delegated tick moved the
                // mount, before the rider's command tick: the rider never samples a Path; only the exact carrier's movement fact
                // (MountedCarrierMovementLedger, recorded at the TickDelegated debit) remains.
                // Round 19 (R17-F1, R18-F2): the delivery is now recorded where native delivers it (OnPathComplete, through the ACTUAL production
                // hooks: MountedCarrierApproachLedger) and the movement fact only for the carrier whose own delivered path moved the mount.
                var unobserved = kase.Contains("before the rider observes");
                // Round 18 (R17-F1): the carrier's first delegated tick is a zero-duration frame: the hook returns zero, native's denial
                // (CompleteMovement) clears the delivered path, and the action stage of that frame drifts the target before the rider's tick.
                var denied = kase.Contains("first tick denied");
                var pending = kase.StartsWith("pending");
                var pooled = kase.Contains("pooled");
                var c = FirstTickPair(0.6f, 1f / 60f, false, false);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.PathfindUnforcedMoves = true;
                Pathfinding.Path pooledPath = null;
                if (pooled && !unobserved || pending) w.RequestPathFirst = true;
                if (pending) w.DeliverRequests = false;
                if (pooled)
                {
                    // the agent still holds the path of an earlier move when the click issues the carrier; A* later hands the same object back
                    pooledPath = new Pathfinding.Path { vectorPath = new List<Vector3> { w.Horse.Position, w.Horse.Position + new Vector3(0f, 0f, 0.3f) } };
                    w.Horse.View.AgentASP.Path = pooledPath; w.ReusePath = pooledPath;
                }
                LineOfSightGeometry.Instance.Blocked = (from, to) => true;   // the target is never in sight: no strike can become legal
                if (denied) w.ZeroMountTicks = 1;
                var stallsBefore = w.MountStalls;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target, false);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                UnitMoveTo first = null; var cleared = false; var acquiredAtClear = false; var origin = w.Horse.Position; var moveAtClear = -1f; var movedAtClear = 0f;
                object pathAtClear = null; var requestInFlightAtClear = false;
                var targetBefore = w.Target.Position;
                Action clearNow = null;
                for (var i = 0; i < 40 && order != null && !order.IsFinished && !cleared; i++)
                {
                    var carrier = order.DelegatedMove;
                    if (carrier != null && first == null) { first = carrier; origin = w.Horse.Position; }
                    var ready = carrier != null && !carrier.IsFinished && (pending ? i >= 3 : Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin, w.Horse.Position) > 0.05f);
                    if (unobserved && first != null)
                    {
                        c.ActionStage = () =>
                        {
                            var live = order.DelegatedMove;
                            if (!cleared && live != null && !live.IsFinished &&
                                (denied ? w.MountStalls > stallsBefore : Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin, w.Horse.Position) > 0f)) clearNow();
                        };
                    }
                    else if (ready)
                    {
                        c.ActionStage = () => clearNow();
                    }
                    if (clearNow == null)
                    {
                        clearNow = () =>
                        {
                            var agent = w.Horse.View.AgentASP;
                            pathAtClear = agent.Path; requestInFlightAtClear = agent.RequestedPath != null;
                            if (!pending)
                            {
                                agent.Path = null; agent.RequestedPath = null; agent.PathOwner = null; w.PathfindUnforcedMoves = false;   // the issued path is released; native requests no other
                            }
                            acquiredAtClear = order.CarrierAcquiredPath;
                            moveAtClear = w.Horse.CombatState.Cooldown.MoveAction;
                            movedAtClear = Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin, w.Horse.Position);
                            w.Target.Position = w.Target.Position + new Vector3(0f, 0f, 0.8f);
                            cleared = true;
                        };
                    }
                    Frame(c);
                }
                var positiveTickRecorded = MountedCarrierApproachLedger.MovedAlongDelivery(first);
                var deliveryRecorded = MountedCarrierApproachLedger.HasDelivery(first);
                var unfinishedAtClear = first != null && (pending ? !first.IsFinished || order.DelegatedMoveStarts > 1 : true);
                Frame(c); Frame(c);
                var moveAfter = w.Horse.CombatState.Cooldown.MoveAction;
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                bool ok; string successorDetail = "";
                if (!pending)
                {
                    var endedNatively = order != null && order.IsFinished && order.NativeApproachEndedCount == 1 && order.EndCause == MountedAttackEndCause.Cancelled;
                    var carrierCleared = LiveMountCarrier(c) == null;
                    var at = w.Horse.Position;
                    var successor = new UnitMoveTo(at + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, -0.5f) }) };
                    // the player's turn-based ground order reaches the mount through the ground admission (TryAdmitGroundCommand's admitted
                    // disposition stand-in, native Run on the mount, the production CompleteGroundCommandAdmission)
                    c.Combat.AdmitRiderTurnGroundOrderForHarness();
                    w.Horse.Commands.Run(successor);
                    c.Combat.CompleteGroundCommandAdmission(w.Horse);
                    var admittedAsGroundMove = ReferenceEquals(c.Combat.ActiveRiderTurnGroundMoveForHarness, successor);
                    Frame(c);
                    var slot = w.Horse.Commands.GetCommand(UnitCommand.CommandType.Move);
                    // Round 16 follow-up: the successor is usable: further frames steer the mount along its own path.
                    var ownerAfterTake = w.Horse.View.AgentASP.PathOwner;
                    for (var f = 0; f < 4 && !successor.IsFinished; f++) Frame(c);
                    var successorMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(at, w.Horse.Position);
                    var carriersAfterSuccessor = order.DelegatedMoveStarts;
                    ok = cleared && acquiredAtClear && order.CarrierPathObservations == (unobserved ? 0 : 1) && deliveryRecorded && (denied ? !positiveTickRecorded : positiveTickRecorded) && endedNatively && carriers == 1 && order.StruckTargets.Count == 0 &&
                        (denied ? moveAtClear == 0f : moveAtClear > 0f) && moveAfter == moveAtClear && carrierCleared && w.Rider.CombatState.Cooldown.StandardAction == standardBefore &&
                        admittedAsGroundMove && ReferenceEquals(slot, successor) && ReferenceEquals(ownerAfterTake, successor) && successorMoved > 0f && carriersAfterSuccessor == 1 &&
                        !order.RouteBound && !click.Admitted && (!pooled || ReferenceEquals(pathAtClear, pooledPath));
                    successorDetail = "; exact carrier's positive delegated tick recorded " + positiveTickRecorded + ", delivery recorded " + deliveryRecorded + (denied ? ", denied first ticks " + (w.MountStalls - stallsBefore) : "") + (pooled ? "; delivered path is the pooled object held at issue " + ReferenceEquals(pathAtClear, pooledPath) : "") +
                        "; the player's newer ground order (admitted as the rider-turn ground move " + admittedAsGroundMove + ") owns the Move slot " + ReferenceEquals(slot, successor) + ", the agent steers its own path " + ReferenceEquals(ownerAfterTake, successor) +
                        ", mount moved " + successorMoved.ToString("0.###") + " m by it (now at " + w.Horse.Position + "), attack carriers still " + carriersAfterSuccessor;
                }
                else
                {
                    successorDetail = "; request in flight at the drift " + requestInFlightAtClear + ", positive delegated tick recorded " + positiveTickRecorded + ", delivery recorded " + deliveryRecorded;
                    ok = cleared && requestInFlightAtClear && !positiveTickRecorded && !deliveryRecorded && !acquiredAtClear && order != null && order.CarrierPathObservations == 0 && order.NativeApproachEndedCount == 0 &&
                        !(order.IsFinished && order.EndCauseDetail != null && order.EndCauseDetail.Contains("native turn-based approach ended")) && carriers == 2 &&
                        order.StruckTargets.Count == 0 && w.Rider.CombatState.Cooldown.StandardAction == standardBefore && !order.RouteBound;
                }
                Check(ok, "R13-F3 (round 16) " + kase + ": admitted override " + click.Admitted + ", route-bound " + (order != null && order.RouteBound) + "; first carrier " + Id(first) +
                    " unfinished at the clear " + unfinishedAtClear + ", path acquired " + acquiredAtClear + " (observations " + (order == null ? 0 : order.CarrierPathObservations) + "), mount moved " +
                    movedAtClear.ToString("0.###") + " m; target " + targetBefore + " -> " + w.Target.Position + "; carriers issued " + carriers + "; disposition " +
                    (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + "(" + order.EndCauseDetail + ") (native force-finish disposition " + order.NativeApproachEndedCount + ")" : "live (native force-finish disposition " + order.NativeApproachEndedCount + ")") +
                    "; mount Move " + moveAtClear.ToString("0.####") + " at the clear -> " + moveAfter.ToString("0.####") + " s; rider Standard " + standardBefore + " -> " + w.Rider.CombatState.Cooldown.StandardAction +
                    "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + successorDetail);
            }

            // ===================== Round 19 (R17-F1, R18-F2): exact-carrier issuance and delivery across both native denial routes =====================
            // R17-F1: rider-first; the native-accepted click's carrier receives its first path (the modeled installed PathTo and OnPathComplete, each
            // through the ACTUAL production hooks) between the rider's observations. On the carrier's first movement tick a movement-prevention guard
            // is active, so the ACTUAL TryOverrideMountTurnMovement returns false BEFORE the delegated adapter (production MountMovementPrefix then
            // returns true and native runs) and the modeled installed CanMoveInTurnBased denial completes the movement (CompleteMovement: Path and
            // RequestedPath cleared); the action stage of that frame drifts the target 0.8 m. The control runs the same frame without the guard.
            foreach (var guard in new[] { "IsCommandsPreventMovement", "AnimationManager.IsPreventingMovement", "no guard (control)" })
            {
                var control = guard.StartsWith("no guard");
                var c = FirstTickPair(0.6f, 1f / 60f, false, false);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.PathfindUnforcedMoves = true;
                LineOfSightGeometry.Instance.Blocked = (from, to) => true;   // the target is never in sight: no strike can become legal
                var agent = w.Horse.View.AgentASP;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target, false);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                UnitMoveTo first = null; var armed = false; var drifted = false; var origin = w.Horse.Position; var targetBefore = w.Target.Position;
                var phaseBeforeTick = MountedCarrierApproachPhase.NotAdmitted; var phaseAfterTick = MountedCarrierApproachPhase.NotAdmitted;
                var observedBeforeTick = -1; var handled = true; var result = true; object pathAfterTick = null, requestAfterTick = null, deliveredPath = null;
                var denialsBefore = 0; var denials = 0; var moveAtTick = -1f; var movedAtTick = -1f; var movedAlongAtTick = false;
                for (var i = 0; i < 40 && order != null && !order.IsFinished && !drifted; i++)
                {
                    if (first == null && order.DelegatedMove != null) { first = order.DelegatedMove; origin = w.Horse.Position; }
                    if (first != null && !armed && order.CarrierApproachPhase == MountedCarrierApproachPhase.Delivered)
                    {
                        armed = true; phaseBeforeTick = order.CarrierApproachPhase; observedBeforeTick = order.CarrierPathObservations; deliveredPath = agent.Path;
                        c.BeforeMountTick = () =>
                        {
                            if (guard == "IsCommandsPreventMovement") w.Horse.IsCommandsPreventMovement = true;
                            else if (!control) w.Horse.AnimationManager = new AnimationManagerFacade { IsPreventingMovement = true };
                            denialsBefore = w.NativeDenials;
                        };
                        c.ActionStage = () =>
                        {
                            handled = w.LastHookHandled; result = w.LastHookResult; denials = w.NativeDenials - denialsBefore;
                            pathAfterTick = agent.Path; requestAfterTick = agent.RequestedPath; phaseAfterTick = order.CarrierApproachPhase;
                            movedAlongAtTick = MountedCarrierApproachLedger.MovedAlongDelivery(first);
                            moveAtTick = w.Horse.CombatState.Cooldown.MoveAction; movedAtTick = Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin, w.Horse.Position);
                            if (!control) w.Target.Position = w.Target.Position + new Vector3(0f, 0f, 0.8f);
                            drifted = true;
                        };
                    }
                    Frame(c);
                }
                if (control)
                {
                    var okControl = armed && drifted && handled && result && denials == 0 && pathAfterTick != null && ReferenceEquals(pathAfterTick, deliveredPath) &&
                        phaseAfterTick == MountedCarrierApproachPhase.Delivered && movedAlongAtTick && movedAtTick > 0f && moveAtTick > 0f && order != null && !order.IsFinished &&
                        order.NativeApproachEndedCount == 0 && order.DelegatedMoveStarts == 1;
                    Check(okControl, "Round 19 (R17-F1) control, same carrier's first movement tick without a guard: ACTUAL TryOverrideMountTurnMovement handled " + handled +
                        " result " + result + " (the hook moved the mount " + movedAtTick.ToString("0.####") + " m, debit " + moveAtTick.ToString("0.#####") + " s), native denials " + denials +
                        "; path still the delivered one " + ReferenceEquals(pathAfterTick, deliveredPath) + ", phase " + phaseAfterTick + ", movement credited to the carrier " + movedAlongAtTick +
                        "; order " + (order == null ? "-" : order.IsFinished ? "ended" : "live") + ", carriers " + (order == null ? -1 : order.DelegatedMoveStarts));
                    continue;
                }
                var deliveryAtTick = MountedCarrierApproachLedger.HasDelivery(first);
                Frame(c); Frame(c);
                var moveAfter = w.Horse.CombatState.Cooldown.MoveAction;
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var endedNatively = order != null && order.IsFinished && order.NativeApproachEndedCount == 1 && order.EndCause == MountedAttackEndCause.Cancelled;
                var carrierCleared = LiveMountCarrier(c) == null;
                // the guard ends; the player's newer turn-based ground order reaches the mount through the ground admission and moves it
                w.Horse.IsCommandsPreventMovement = false; w.Horse.AnimationManager = null;
                var at = w.Horse.Position;
                var successor = new UnitMoveTo(at + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, -0.5f) }) };
                c.Combat.AdmitRiderTurnGroundOrderForHarness();
                w.Horse.Commands.Run(successor);
                c.Combat.CompleteGroundCommandAdmission(w.Horse);
                var admittedAsGroundMove = ReferenceEquals(c.Combat.ActiveRiderTurnGroundMoveForHarness, successor);
                for (var f = 0; f < 5 && !successor.IsFinished; f++) Frame(c);
                var successorMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(at, w.Horse.Position);
                var ok = armed && drifted && !handled && observedBeforeTick == 0 && phaseBeforeTick == MountedCarrierApproachPhase.Delivered && denials == 1 &&
                    pathAfterTick == null && requestAfterTick == null && phaseAfterTick == MountedCarrierApproachPhase.EndedByNative && deliveryAtTick && !movedAlongAtTick &&
                    movedAtTick == 0f && moveAtTick == 0f && endedNatively && carriers == 1 && order.StruckTargets.Count == 0 && moveAfter == 0f && carrierCleared &&
                    w.Rider.CombatState.Cooldown.StandardAction == standardBefore && admittedAsGroundMove && successorMoved > 0f && order.DelegatedMoveStarts == 1 && !order.RouteBound && !click.Admitted;
                Check(ok, "Round 19 (R17-F1) rider-first, carrier's first path delivered between rider observations (rider observations " + observedBeforeTick + ", phase " + phaseBeforeTick +
                    "), guard " + guard + " active on its first movement tick: ACTUAL TryOverrideMountTurnMovement handled " + handled + " (production MountMovementPrefix returns to native), modeled " +
                    "CanMoveInTurnBased denial ran " + denials + "x; path after denial " + (pathAfterTick == null ? "none" : Id(pathAfterTick)) + ", request " + (requestAfterTick == null ? "none" : Id(requestAfterTick)) +
                    ", carrier phase " + phaseAfterTick + ", delivery recorded " + deliveryAtTick + ", movement credited " + movedAlongAtTick + "; target " + targetBefore + " -> " + w.Target.Position +
                    "; carriers issued " + carriers + "; disposition " + (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + "(" + order.EndCauseDetail + ") (native force-finish disposition " +
                    order.NativeApproachEndedCount + ")" : "live") + "; mount Move " + moveAtTick.ToString("0.####") + " at the tick -> " + moveAfter.ToString("0.####") + " s; rider Standard " + standardBefore +
                    " -> " + w.Rider.CombatState.Cooldown.StandardAction + "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + "; successor ground order admitted " + admittedAsGroundMove +
                    ", moved the mount " + successorMoved.ToString("0.###") + " m, attack carriers still " + (order == null ? -1 : order.DelegatedMoveStarts));
            }

            // R18-F2: a predecessor's path still steers (really moving, its request answered for an earlier move) when the native-accepted click's new
            // carrier takes the Move slot. Rider-first: the carrier's first request is issued and in flight (the predecessor's Path stays until a delivery
            // replaces it); the movement hook runs on the predecessor's path BEFORE the carrier has received any path, and that tick is denied (the
            // adapter's zero tick, or the guard before the adapter): the modeled CanMoveInTurnBased denial clears the predecessor's path and the
            // carrier's pending request. The new carrier acquires nothing from the old path, is not ended, re-requests and receives its own first path,
            // and then proceeds normally. The third variant first lets the predecessor's path move the mount one positive tick while the carrier's
            // request is still in flight: that movement credits no carrier.
            foreach (var kase in new[] { "adapter zero tick", "guard before the adapter", "predecessor's path moves the mount one positive tick first, then the adapter zero tick" })
            {
                var moving = kase.StartsWith("predecessor");
                var c = FirstTickPair(0.6f, 1f / 60f, false, false);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                w.PathfindUnforcedMoves = true;
                w.RequestPathFirst = true;
                if (moving) w.DeliverRequests = false;
                var agent = w.Horse.View.AgentASP;
                var start = w.Horse.Position;
                var earlier = new UnitMoveTo(start + new Vector3(0f, 0f, 0.6f), 0.1f) { CreatedByPlayer = true };
                var predecessorPath = new Pathfinding.Path { vectorPath = new List<Vector3> { start, start + new Vector3(0f, 0f, 0.6f) } };
                agent.Path = predecessorPath; agent.PathOwner = earlier; agent.NextPointIndex = 1; agent.MoveDirection = new UnityEngine.Vector2(0f, 1f);
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target, false);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                UnitMoveTo first = null; var predecessorFrame = false; var denialArmed = false; var denialDone = false;
                var phaseAtAdmission = MountedCarrierApproachPhase.NotAdmitted; var steeringAtAdmission = false; var requestsAtAdmission = 0;
                var predecessorMoved = 0f; var creditedByPredecessor = false; var deliveredAfterPredecessor = false; var phaseAfterPredecessor = MountedCarrierApproachPhase.NotAdmitted;
                var handled = true; var denials = 0; var denialsBefore = 0; object pathBeforeDenial = null, pathAfterDenial = null, requestAfterDenial = null;
                var phaseAfterDenial = MountedCarrierApproachPhase.NotAdmitted; var deliveredAfterDenial = true; var endedAfterDenial = -1; var moveBeforeDenial = 0f;
                var ownPathFrame = -1; var at = start; var movedAfterOwnPath = 0f; var deliveredPath = (object)null;
                Action armDenial = () =>
                {
                    denialArmed = true;
                    c.BeforeMountTick = () =>
                    {
                        if (kase.StartsWith("guard")) w.Horse.IsCommandsPreventMovement = true; else w.ZeroMountTicks = 1;
                        denialsBefore = w.NativeDenials; pathBeforeDenial = agent.Path; moveBeforeDenial = w.Horse.CombatState.Cooldown.MoveAction;
                    };
                    c.ActionStage = () =>
                    {
                        handled = w.LastHookHandled; denials = w.NativeDenials - denialsBefore;
                        pathAfterDenial = agent.Path; requestAfterDenial = agent.RequestedPath; phaseAfterDenial = order.CarrierApproachPhase;
                        deliveredAfterDenial = MountedCarrierApproachLedger.HasDelivery(first);
                        w.Horse.IsCommandsPreventMovement = false; w.DeliverRequests = true; denialDone = true;
                    };
                };
                for (var i = 0; i < 60 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                {
                    Frame(c);
                    if (denialDone && endedAfterDenial < 0) endedAfterDenial = order.NativeApproachEndedCount;
                    if (first == null && order.DelegatedMove != null)
                    {
                        first = order.DelegatedMove; phaseAtAdmission = order.CarrierApproachPhase; steeringAtAdmission = ReferenceEquals(agent.Path, predecessorPath);
                        requestsAtAdmission = MountedCarrierApproachLedger.RequestCount(first); at = w.Horse.Position;
                        if (moving) predecessorFrame = true; else armDenial();
                        continue;
                    }
                    if (predecessorFrame)
                    {
                        predecessorFrame = false;
                        predecessorMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(at, w.Horse.Position);
                        creditedByPredecessor = MountedCarrierApproachLedger.MovedAlongDelivery(first); deliveredAfterPredecessor = MountedCarrierApproachLedger.HasDelivery(first);
                        phaseAfterPredecessor = order.CarrierApproachPhase;
                        armDenial();
                        continue;
                    }
                    if (denialDone && ownPathFrame < 0 && first != null && order.CarrierApproachPhase == MountedCarrierApproachPhase.Delivered)
                    { ownPathFrame = i; at = w.Horse.Position; deliveredPath = agent.Path; }
                }
                if (ownPathFrame >= 0) movedAfterOwnPath = Kingmaker.Utility.GeometryUtils.MechanicsDistance(at, w.Horse.Position);
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var strikes = order == null ? 0 : order.StruckTargets.Count;
                var ok = first != null && phaseAtAdmission == MountedCarrierApproachPhase.RequestPending && steeringAtAdmission && requestsAtAdmission == 1 &&
                    (!moving || predecessorMoved > 0f && !creditedByPredecessor && !deliveredAfterPredecessor && phaseAfterPredecessor == MountedCarrierApproachPhase.RequestPending) &&
                    denialDone && !handled && denials == 1 && ReferenceEquals(pathBeforeDenial, predecessorPath) && pathAfterDenial == null && requestAfterDenial == null &&
                    phaseAfterDenial == MountedCarrierApproachPhase.RequestUndelivered && !deliveredAfterDenial && endedAfterDenial == 0 &&
                    ownPathFrame >= 0 && deliveredPath != null && !ReferenceEquals(deliveredPath, predecessorPath) && MountedCarrierApproachLedger.HasDelivery(first) &&
                    MountedCarrierApproachLedger.RequestCount(first) == 2 && MountedCarrierApproachLedger.MovedAlongDelivery(first) && movedAfterOwnPath > 0f &&
                    carriers == 1 && order.NativeApproachEndedCount == 0 && strikes == 1 && !order.RouteBound && !click.Admitted;
                Check(ok, "Round 19 (R18-F2) predecessor's path steering when the new carrier takes the Move slot, " + kase + ": carrier " + Id(first) + " at admission phase " + phaseAtAdmission +
                    " (requests " + requestsAtAdmission + ", predecessor path still steering " + steeringAtAdmission + ")" +
                    (moving ? "; predecessor's positive tick moved the mount " + predecessorMoved.ToString("0.####") + " m, credited to the carrier " + creditedByPredecessor + ", carrier delivery " + deliveredAfterPredecessor + ", phase " + phaseAfterPredecessor : "") +
                    "; denied tick: ACTUAL TryOverrideMountTurnMovement handled " + handled + ", modeled CanMoveInTurnBased denial ran " + denials + "x on path " + (ReferenceEquals(pathBeforeDenial, predecessorPath) ? "(the predecessor's)" : Id(pathBeforeDenial)) +
                    ", path after " + (pathAfterDenial == null ? "none" : Id(pathAfterDenial)) + ", request after " + (requestAfterDenial == null ? "none" : Id(requestAfterDenial)) + ", carrier phase " + phaseAfterDenial +
                    ", delivery " + deliveredAfterDenial + ", native disposition after the denial " + endedAfterDenial + "; the carrier's own path delivered " + (ownPathFrame >= 0) + " (requests " +
                    MountedCarrierApproachLedger.RequestCount(first) + ", not the predecessor's " + !ReferenceEquals(deliveredPath, predecessorPath) + "), mount moved " + movedAfterOwnPath.ToString("0.###") +
                    " m along it (credited " + MountedCarrierApproachLedger.MovedAlongDelivery(first) + "); carriers issued " + carriers + "; native disposition " + (order == null ? -1 : order.NativeApproachEndedCount) +
                    "; strikes " + strikes + "; mount Move " + moveBeforeDenial.ToString("0.####") + " before the denial -> " + w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") +
                    " s; rider Standard " + standardBefore + " -> " + w.Rider.CombatState.Cooldown.StandardAction + "; order " + (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + "(" + order.EndCauseDetail + ")" : "live"));
            }

            // Round 19 follow-up (R17-F1, route-bound): the accepted override click's route-bound carrier receives its forced path between the rider's
            // observations (native TickApproaching -> FollowPrecomputedPath -> OnPathComplete: the ACTUAL TickApproaching prefix/postfix scope, the
            // ACTUAL FollowPrecomputedPath prefix and the ACTUAL completion hooks). Its first movement tick is denied before any displacement (the
            // guard before the adapter, or the adapter's zero tick), the modeled CanMoveInTurnBased denial clears the path, and the action stage
            // drifts the target 0.8 m. The guard route ends with the installed force-finish disposition. The carrier is gated (a first-tick
            // carrier, MountedMovementStateAdapter.FirstTickGate), so the adapter's zero tick is that first-tick decision's refusal: the existing
            // refusal path governs (no further carrier is issued, nothing is spent). Control: a predecessor command's own TickApproaching issues a
            // forced path on the same agent after the carrier's delivery; it is not attributed to the carrier (observed right after the
            // predecessor's synchronous delivery, before the movement hook).
            foreach (var kase in new[] { "guard before the adapter (IsCommandsPreventMovement)", "adapter zero tick", "control: a predecessor's forced path" })
            {
                var control = kase.StartsWith("control");
                var c = FirstTickPair(0.6f, 1f / 60f, false, false);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                var agent = w.Horse.View.AgentASP;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target);
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                UnitMoveTo first = null; var armed = false; var done = false; var origin = w.Horse.Position; var targetBefore = w.Target.Position;
                var phaseBeforeTick = MountedCarrierApproachPhase.NotAdmitted; var phaseAfterTick = MountedCarrierApproachPhase.NotAdmitted;
                var observedBeforeTick = -1; var handled = true; object pathAfterTick = null, requestAfterTick = null, predecessorPath = null;
                var denialsBefore = 0; var denials = 0; var moveAtTick = -1f; var movedAtTick = -1f; var movedAlongAtTick = true; var deliveryAtTick = false;
                var requestsBeforeTick = -1; var requestsAfterTick = -1; var firstTickDecision = "-";
                for (var i = 0; i < 40 && order != null && !order.IsFinished && !done; i++)
                {
                    if (first == null && order.DelegatedMove != null) { first = order.DelegatedMove; origin = w.Horse.Position; }
                    // armed on the harness's observation of native's delivery (the agent steers the carrier's forced path), not on the record under test
                    if (first != null && !armed && agent.Path != null && ReferenceEquals(agent.PathOwner, first))
                    {
                        armed = true; phaseBeforeTick = order.CarrierApproachPhase; observedBeforeTick = order.CarrierPathObservations;
                        requestsBeforeTick = MountedCarrierApproachLedger.RequestCount(first);
                        c.BeforeMountTick = () =>
                        {
                            denialsBefore = w.NativeDenials;
                            if (kase.StartsWith("guard")) w.Horse.IsCommandsPreventMovement = true;
                            else if (!control) w.ZeroMountTicks = 1;
                            else
                            {
                                // a predecessor command's native TickApproaching on the same agent (its own scope) follows its own forced path
                                var at = w.Horse.Position;
                                var earlier = new UnitMoveTo(at + new Vector3(0f, 0f, 0.6f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { at, at + new Vector3(0f, 0f, 0.6f) }) };
                                PatchMethods.MountedAttackApproachPrefix(earlier); TakePath(w.Horse, earlier); PatchMethods.MountedAttackApproachPostfix(earlier);
                                predecessorPath = agent.Path; requestsAfterTick = MountedCarrierApproachLedger.RequestCount(first); phaseAfterTick = order.CarrierApproachPhase;
                                movedAlongAtTick = MountedCarrierApproachLedger.MovedAlongDelivery(first); deliveryAtTick = MountedCarrierApproachLedger.HasDelivery(first);
                            }
                        };
                        c.ActionStage = () =>
                        {
                            handled = w.LastHookHandled; denials = w.NativeDenials - denialsBefore;
                            pathAfterTick = agent.Path; requestAfterTick = agent.RequestedPath; firstTickDecision = c.Combat.LastFirstTickDecision ?? "-";
                            if (!control)
                            {
                                phaseAfterTick = order.CarrierApproachPhase; movedAlongAtTick = MountedCarrierApproachLedger.MovedAlongDelivery(first);
                                deliveryAtTick = MountedCarrierApproachLedger.HasDelivery(first); requestsAfterTick = MountedCarrierApproachLedger.RequestCount(first);
                            }
                            moveAtTick = w.Horse.CombatState.Cooldown.MoveAction; movedAtTick = Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin, w.Horse.Position);
                            if (!control) w.Target.Position = w.Target.Position + new Vector3(0f, 0f, 0.8f);
                            done = true;
                        };
                    }
                    Frame(c);
                }
                if (control)
                {
                    var okControl = armed && done && order != null && order.RouteBound && click.Admitted && requestsBeforeTick == 1 && requestsAfterTick == 1 &&
                        predecessorPath != null && predecessorPath is Pathfinding.ForcedPath && phaseAfterTick == MountedCarrierApproachPhase.EndedByNative && deliveryAtTick && !movedAlongAtTick;
                    Check(okControl, "Round 19 follow-up (R17-F1, route-bound) " + kase + ": carrier " + Id(first) + " delivered its own forced path (requests " + requestsBeforeTick + ", phase " + phaseBeforeTick +
                        "); a predecessor command's own TickApproaching then delivered its forced path " + Id(predecessorPath) + " on the same agent: carrier requests still " + requestsAfterTick +
                        ", phase " + phaseAfterTick + " (the agent no longer holds the carrier's delivery; nothing attributed), carrier's own earlier delivery still recorded " + deliveryAtTick +
                        ", movement credited " + movedAlongAtTick + "; route-bound " + (order != null && order.RouteBound) + ", admitted override " + click.Admitted);
                    continue;
                }
                Frame(c); Frame(c);
                var moveAfter = w.Horse.CombatState.Cooldown.MoveAction;
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var endedNatively = order != null && order.IsFinished && order.NativeApproachEndedCount == 1 && order.EndCause == MountedAttackEndCause.Cancelled;
                var carrierCleared = LiveMountCarrier(c) == null;
                w.Horse.IsCommandsPreventMovement = false;
                var successorAt = w.Horse.Position;
                var successor = new UnitMoveTo(successorAt + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { successorAt, successorAt + new Vector3(0f, 0f, -0.5f) }) };
                c.Combat.AdmitRiderTurnGroundOrderForHarness();
                w.Horse.Commands.Run(successor);
                c.Combat.CompleteGroundCommandAdmission(w.Horse);
                var admittedAsGroundMove = ReferenceEquals(c.Combat.ActiveRiderTurnGroundMoveForHarness, successor);
                for (var f = 0; f < 5 && !successor.IsFinished; f++) Frame(c);
                var successorMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(successorAt, w.Horse.Position);
                var zero = kase.StartsWith("adapter");
                var refusedByGate = order != null && order.IsFinished && order.NativeApproachEndedCount == 0 && order.EndCause == MountedAttackEndCause.Cancelled &&
                    order.EndCauseDetail == "first-tick request refused" && firstTickDecision.StartsWith("refused");
                var ok = armed && done && !handled && observedBeforeTick == 0 && phaseBeforeTick == MountedCarrierApproachPhase.Delivered && requestsBeforeTick == 1 && denials == 1 &&
                    pathAfterTick == null && requestAfterTick == null && phaseAfterTick == MountedCarrierApproachPhase.EndedByNative && deliveryAtTick && !movedAlongAtTick &&
                    movedAtTick == 0f && (zero ? refusedByGate : endedNatively) && carriers == 1 && order.StruckTargets.Count == 0 && moveAfter == moveAtTick && carrierCleared &&
                    w.Rider.CombatState.Cooldown.StandardAction == standardBefore && admittedAsGroundMove && successorMoved > 0f && order.DelegatedMoveStarts == 1 && order.RouteBound && click.Admitted;
                Check(ok, "Round 19 follow-up (R17-F1, route-bound) rider-first, forced path delivered between rider observations (rider observations " + observedBeforeTick + ", phase " + phaseBeforeTick +
                    ", requests " + requestsBeforeTick + "), " + kase + " on its first movement tick: ACTUAL TryOverrideMountTurnMovement handled " + handled + ", modeled CanMoveInTurnBased denial ran " + denials +
                    "x; path after denial " + (pathAfterTick == null ? "none" : Id(pathAfterTick)) + ", request " + (requestAfterTick == null ? "none" : Id(requestAfterTick)) + ", carrier phase " + phaseAfterTick +
                    ", delivery recorded " + deliveryAtTick + ", movement credited " + movedAlongAtTick + ", mount displaced " + movedAtTick.ToString("0.####") + " m; target " + targetBefore + " -> " + w.Target.Position +
                    "; first-tick decision " + firstTickDecision.Substring(0, Math.Min(60, firstTickDecision.Length)) + "; carriers issued " + carriers + "; disposition " + (order == null ? "-" : order.IsFinished ? "ended " + order.EndCause + "(" + order.EndCauseDetail + ") (native force-finish disposition " +
                    order.NativeApproachEndedCount + ")" : "live") + "; mount Move " + moveAtTick.ToString("0.####") + " at the tick -> " + moveAfter.ToString("0.####") + " s; rider Standard " + standardBefore +
                    " -> " + w.Rider.CombatState.Cooldown.StandardAction + "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + "; successor ground order admitted " + admittedAsGroundMove +
                    ", moved the mount " + successorMoved.ToString("0.###") + " m; route-bound " + (order != null && order.RouteBound) + ", admitted override " + click.Admitted);
            }

            // ===================== Round 20 (R18-F2, R19-F2, R19-F5) =====================
            // R18-F2: horse-first (the horse's action pass precedes the rider's), a provisional replacement over the player's prior selection and
            // Hold. The rider's pass issues the route-bound carrier and binds its first-tick gate; at the next movement pass the agent is still
            // steering a predecessor's path (MATCHING the carrier's route coordinates, or DIFFERING) and the carrier is AwaitingFirstRequest with no
            // request. The ACTUAL TickDelegated reaches the ACTUAL decision, which defers (no decision, no debit, no refusal); the zero tick's modeled
            // native denial clears the predecessor's path; the proposal and the prior input are unchanged. The carrier then receives its own forced
            // path, the request decides on it, the mount moves along it and the strike lands.
            foreach (var kase in new[] { "matching waypoints", "differing waypoints" })
            {
                var matching = kase.StartsWith("matching");
                var c = FirstTickPair(0.12f, 1f / 60f, true);
                c.HorseFirst = true;   // the horse's action pass runs before the rider's in every frame
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                var a = PriorSelectionAndHold(c);
                var agent = w.Horse.View.AgentASP;
                var genBefore = c.Combat.Generation; var intentBefore = c.Combat.IntentTarget; var targetBefore = w.Rider.CombatState.ManualTarget; var holdBefore = w.Horse.HoldState;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                Frame(c);
                var carrier = p?.CarrierMove;
                var phaseAtIssue = order == null ? MountedCarrierApproachPhase.NotAdmitted : order.CarrierApproachPhase;
                var requestsAtIssue = MountedCarrierApproachLedger.RequestCount(carrier);
                var gateBound = p != null && ReferenceEquals(c.Pair.movementState.FirstTickGate, p);
                var earlier = new UnitMoveTo(w.Horse.Position + new Vector3(0f, 0f, 0.6f), 0.1f) { CreatedByPlayer = true };
                Pathfinding.Path predecessor = null; var phaseAtHook = MountedCarrierApproachPhase.NotAdmitted; var requestsAtHook = -1; var deferralsBefore = 0; var denialsBefore = 0;
                var start = w.Horse.Position; string tickLine = "-", decisionAtHook = "-"; var stateAtHook = MountedFirstTickProposalState.Committed; var gateAtHook = false;
                var moveAtHook = -1f; var displacedAtHook = -1f; var deferrals = 0; var denials = 0; object pathAfterHook = "unset"; var genAtHook = -1L; object targetAtHook = null, intentAtHook = null; var holdAtHook = !holdBefore;
                if (carrier != null)
                {
                    c.BeforeMountTick = () =>
                    {
                        var at = w.Horse.Position;
                        var points = matching ? new List<Vector3>(carrier.ForcedPath.vectorPath) : new List<Vector3> { at, at + new Vector3(0f, 0f, 0.6f) };
                        predecessor = new Pathfinding.Path { vectorPath = points };
                        agent.Path = predecessor; agent.PathOwner = earlier; agent.NextPointIndex = 0; AdvanceCursor(agent, at);
                        var next = points[Math.Min(agent.NextPointIndex, points.Count - 1)]; var dx = next.x - at.x; var dz = next.z - at.z; var dl = (float)Math.Sqrt(dx * dx + dz * dz);
                        if (dl > 1e-6f) agent.MoveDirection = new UnityEngine.Vector2(dx / dl, dz / dl);
                        phaseAtHook = order.CarrierApproachPhase; requestsAtHook = MountedCarrierApproachLedger.RequestCount(carrier);
                        deferralsBefore = c.Combat.FirstTickDeferralCount; denialsBefore = w.NativeDenials; start = at;
                    };
                    c.ActionStage = () =>
                    {
                        tickLine = w.LastMountTick ?? "-"; deferrals = c.Combat.FirstTickDeferralCount - deferralsBefore; denials = w.NativeDenials - denialsBefore;
                        stateAtHook = p.State; decisionAtHook = c.Combat.LastFirstTickDecision; gateAtHook = ReferenceEquals(c.Pair.movementState.FirstTickGate, p);
                        moveAtHook = w.Horse.CombatState.Cooldown.MoveAction; displacedAtHook = Kingmaker.Utility.GeometryUtils.MechanicsDistance(start, w.Horse.Position);
                        pathAfterHook = agent.Path; genAtHook = c.Combat.Generation; targetAtHook = w.Rider.CombatState.ManualTarget; intentAtHook = c.Combat.IntentTarget; holdAtHook = w.Horse.HoldState;
                    };
                    Frame(c);
                }
                var seenOwn = false; var requestsAtOwn = -1; object ownPath = null;
                for (var i = 0; i < 20 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++)
                {
                    if (!seenOwn && order.CarrierApproachPhase == MountedCarrierApproachPhase.Delivered) { seenOwn = true; requestsAtOwn = MountedCarrierApproachLedger.RequestCount(carrier); ownPath = agent.Path; }
                    Frame(c);
                }
                var decision = c.Combat.LastFirstTickDecision;
                var movedAlong = MountedCarrierApproachLedger.MovedAlongDelivery(carrier);
                var strikes = order == null ? 0 : order.StruckTargets.Count;
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var ok = click.Admitted && p != null && p.Replacement && carrier != null && gateBound && phaseAtIssue == MountedCarrierApproachPhase.AwaitingFirstRequest && requestsAtIssue == 0 &&
                    phaseAtHook == MountedCarrierApproachPhase.AwaitingFirstRequest && requestsAtHook == 0 &&
                    deferrals == 1 && tickLine.StartsWith("mountMove=first-tick-deferred") && stateAtHook == MountedFirstTickProposalState.Pending && gateAtHook && decisionAtHook.StartsWith("pending") &&
                    moveAtHook == 0f && displacedAtHook == 0f && denials == 1 && pathAfterHook == null &&
                    genAtHook == genBefore && ReferenceEquals(targetAtHook, targetBefore) && ReferenceEquals(intentAtHook, intentBefore) && holdAtHook == holdBefore && ReferenceEquals(targetBefore, a) &&
                    seenOwn && requestsAtOwn == 1 && ownPath is Pathfinding.ForcedPath && !ReferenceEquals(ownPath, predecessor) &&
                    decision.StartsWith("committed:replacement:") && movedAlong && strikes == 1 && carriers == 1 && c.Combat.Generation > genBefore;
                Check(ok, "Round 20 (R18-F2) horse-first, predecessor's path with " + kase + " steering when the route-bound carrier " + Id(carrier) + " holds the Move slot (phase " + phaseAtIssue +
                    ", requests " + requestsAtIssue + ", gate bound " + gateBound + "): at the movement hook phase " + phaseAtHook + ", requests " + requestsAtHook + "; " +
                    tickLine.Substring(0, Math.Min(150, tickLine.Length)) + "; deferrals " + deferrals + ", proposal " + stateAtHook + ", gate still bound " + gateAtHook + ", decision " + decisionAtHook +
                    ", mount Move " + moveAtHook.ToString("0.####") + " s, displaced " + displacedAtHook.ToString("0.####") + " m, modeled native denial " + denials + "x, path after " + (pathAfterHook == null ? "none" : Id(pathAfterHook)) +
                    "; prior input kept: generation " + genBefore + " -> " + genAtHook + ", target " + Name(targetBefore) + " -> " + Name(targetAtHook as UnitEntityData) + ", Hold " + holdBefore + " -> " + holdAtHook +
                    "; then the carrier's own path delivered " + seenOwn + " (requests " + requestsAtOwn + ", forced " + (ownPath is Pathfinding.ForcedPath) + "), decision " + decision.Substring(0, Math.Min(60, decision.Length)) +
                    ", moved along its own delivery " + movedAlong + ", strikes " + strikes + ", carriers " + carriers + ", generation now " + c.Combat.Generation + "; rider Standard " + standardBefore + " -> " + w.Rider.CombatState.Cooldown.StandardAction);
            }

            // R19-F2: rider-first provisional replacement over the player's prior selection and Hold; its route-bound carrier receives its own forced
            // path; on its first delivered tick a movement-prevention guard is active, so the ACTUAL TryOverrideMountTurnMovement returns false before
            // the deciding adapter and the modeled native denial clears the path while the carrier is unfinished; the target then drifts 0.8 m. The
            // exact ended-delivery observation (EndedByNative of the carrier's own delivery) settles the pending replacement through its refusal
            // path before any target-drift re-path: one carrier, refusal, the prior generation, target and Hold, zero costs; a successor ground
            // order then moves the mount.
            {
                var c = FirstTickPair(0.12f, 1f / 60f);
                var w = c.World;
                w.NativeDenialCompletesMovement = true;
                var a = PriorSelectionAndHold(c);
                var agent = w.Horse.View.AgentASP;
                var genBefore = c.Combat.Generation; var intentBefore = c.Combat.IntentTarget; var targetBefore = w.Rider.CombatState.ManualTarget; var holdBefore = w.Horse.HoldState;
                var standardBefore = w.Rider.CombatState.Cooldown.StandardAction;
                var click = ClickTick(c, w.Target);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                UnitMoveTo carrier = null; var armed = false; var done = false; var handled = true; var denials = 0; var denialsBefore = 0;
                object pathAfter = "unset", requestAfter = "unset"; var phaseAfter = MountedCarrierApproachPhase.NotAdmitted; var unfinishedAtDenial = false; var stateAtDenial = MountedFirstTickProposalState.Committed;
                var targetPos = w.Target.Position;
                for (var i = 0; i < 10 && order != null && !order.IsFinished && !done; i++)
                {
                    if (carrier == null && p != null) carrier = p.CarrierMove;
                    if (carrier != null && !armed && order.CarrierApproachPhase == MountedCarrierApproachPhase.Delivered)
                    {
                        armed = true;
                        c.BeforeMountTick = () => { w.Horse.IsCommandsPreventMovement = true; denialsBefore = w.NativeDenials; };
                        c.ActionStage = () =>
                        {
                            handled = w.LastHookHandled; denials = w.NativeDenials - denialsBefore; pathAfter = agent.Path; requestAfter = agent.RequestedPath;
                            phaseAfter = order.CarrierApproachPhase; unfinishedAtDenial = !carrier.IsFinished; stateAtDenial = p.State;
                            w.Target.Position = w.Target.Position + new Vector3(0f, 0f, 0.8f);
                            done = true;
                        };
                    }
                    Frame(c);
                }
                Frame(c); Frame(c);
                var carriers = order == null ? -1 : order.DelegatedMoveStarts;
                var refused = p != null && p.State == MountedFirstTickProposalState.Refused && p.Detail != null && p.Detail.StartsWith("native-ended-first-approach");
                var keptPrior = c.Combat.Generation == genBefore && ReferenceEquals(c.Combat.IntentTarget, intentBefore) && ReferenceEquals(w.Rider.CombatState.ManualTarget, targetBefore) &&
                    w.Horse.HoldState == holdBefore && ReferenceEquals(targetBefore, a);
                var moveCost = w.Horse.CombatState.Cooldown.MoveAction;
                var strikes = order == null ? 0 : order.StruckTargets.Count;
                var endedCount = order == null ? -1 : order.ProvisionalApproachEndedCount;
                var orderEnded = order != null && order.IsFinished;
                var gateReleased = c.Pair.movementState.FirstTickGate == null;
                var holdKept = w.Horse.HoldState;   // before the successor ground order
                w.Horse.IsCommandsPreventMovement = false;
                var successorAt = w.Horse.Position;
                var successor = new UnitMoveTo(successorAt + new Vector3(0f, 0f, -0.5f), 0.1f) { CreatedByPlayer = true, ForcedPath = new Pathfinding.ForcedPath(new List<Vector3> { successorAt, successorAt + new Vector3(0f, 0f, -0.5f) }) };
                c.Combat.AdmitRiderTurnGroundOrderForHarness();
                w.Horse.Commands.Run(successor);
                c.Combat.CompleteGroundCommandAdmission(w.Horse);
                var admittedAsGroundMove = ReferenceEquals(c.Combat.ActiveRiderTurnGroundMoveForHarness, successor);
                for (var f = 0; f < 5 && !successor.IsFinished; f++) Frame(c);
                var successorMoved = Kingmaker.Utility.GeometryUtils.MechanicsDistance(successorAt, w.Horse.Position);
                var ok = click.Admitted && p != null && p.Replacement && armed && done && !handled && denials == 1 && pathAfter == null && requestAfter == null &&
                    phaseAfter == MountedCarrierApproachPhase.EndedByNative && unfinishedAtDenial && stateAtDenial == MountedFirstTickProposalState.Pending &&
                    refused && endedCount == 1 && carriers == 1 && keptPrior && moveCost == 0f && w.Rider.CombatState.Cooldown.StandardAction == standardBefore && strikes == 0 &&
                    orderEnded && gateReleased && admittedAsGroundMove && successorMoved > 0f;
                Check(ok, "Round 20 (R19-F2) rider-first provisional replacement, its own forced path delivered, guard on its first delivered tick: ACTUAL TryOverrideMountTurnMovement handled " + handled +
                    ", modeled denial " + denials + "x, path after " + (pathAfter == null ? "none" : Id(pathAfter)) + ", request after " + (requestAfter == null ? "none" : Id(requestAfter)) +
                    ", carrier phase " + phaseAfter + ", carrier unfinished " + unfinishedAtDenial + ", proposal " + stateAtDenial + " at the denial; target " + targetPos + " -> " + w.Target.Position +
                    "; proposal " + (p == null ? "-" : p.State + "(" + p.Detail + ")") + ", ended-approach refusals " + endedCount + ", carriers issued " + carriers + ", order ended " + orderEnded +
                    ", gate released " + gateReleased + "; prior generation " + genBefore + " -> " + c.Combat.Generation + ", target " + Name(targetBefore) + " -> " + Name(w.Rider.CombatState.ManualTarget) +
                    ", Hold " + holdBefore + " -> " + holdKept + "; mount Move " + moveCost.ToString("0.####") + " s, rider Standard " + standardBefore + " -> " + w.Rider.CombatState.Cooldown.StandardAction +
                    ", strikes " + strikes + "; successor ground order admitted " + admittedAsGroundMove + ", moved the mount " + successorMoved.ToString("0.###") + " m");
            }

            // R19-F5: the forced-path issuer scope through the ACTUAL TickApproaching prefix/postfix and FollowPrecomputedPath prefix: a throwing top-level
            // approach (prefix ran, the native body threw, no postfix), then balanced unrelated and nested approaches whose forced paths keep their
            // actual issuers; the failed command is not retained; 40 nested scopes keep every outer issuer; a stale scope is discarded by the next
            // frame's first scope; the production ModDisable teardown leaves no scope.
            {
                UnityEngine.Time.frameCount++;
                MountedCarrierPathObserver.ReleaseScopes();
                var failed = ThrowingApproachScope();
                var retainedAfterThrow = MountedCarrierPathObserver.RetainedScopeCount;
                var unrelated = new UnitMoveTo(new Vector3(1f, 0f, 0f), 0.1f);
                PatchMethods.MountedAttackApproachPrefix(unrelated); var unrelatedIssuer = MountedCarrierPathObserver.CurrentIssuer; PatchMethods.MountedAttackApproachPostfix(unrelated);
                var horse = new UnitEntityData("horse-r19f5");
                var scopeAgent = new Kingmaker.View.UnitMovementAgent { Unit = horse };
                horse.View.AgentASP = scopeAgent;
                var outer = new UnitMoveTo(new Vector3(2f, 0f, 0f), 0.1f); var inner = new UnitMoveTo(new Vector3(3f, 0f, 0f), 0.1f);
                MountedCarrierApproachLedger.Admit(outer, scopeAgent); MountedCarrierApproachLedger.Admit(inner, scopeAgent);
                PatchMethods.MountedAttackApproachPrefix(outer);
                PatchMethods.MountedAttackApproachPrefix(inner);
                var innerIssuer = MountedCarrierPathObserver.CurrentIssuer;
                PatchMethods.CarrierForcedPathRequestPrefix(scopeAgent, new Pathfinding.ForcedPath(new List<Vector3> { Vector3.zero, new Vector3(3f, 0f, 0f) }));
                PatchMethods.MountedAttackApproachPostfix(inner);
                var outerIssuer = MountedCarrierPathObserver.CurrentIssuer;
                PatchMethods.CarrierForcedPathRequestPrefix(scopeAgent, new Pathfinding.ForcedPath(new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f) }));
                PatchMethods.MountedAttackApproachPostfix(outer);
                var innerRequests = MountedCarrierApproachLedger.RequestCount(inner); var outerRequests = MountedCarrierApproachLedger.RequestCount(outer);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                var failedReleased = !failed.IsAlive;
                var balanced = new UnitMoveTo(new Vector3(4f, 0f, 0f), 0.1f);
                PatchMethods.MountedAttackApproachPrefix(balanced); PatchMethods.MountedAttackApproachPostfix(balanced);
                var retainedAfterBalanced = MountedCarrierPathObserver.RetainedScopeCount;
                var deep = new List<UnitMoveTo>(); var deepOk = true;
                for (var d = 0; d < 40; d++) { var m = new UnitMoveTo(new Vector3(d, 0f, 1f), 0.1f); deep.Add(m); PatchMethods.MountedAttackApproachPrefix(m); }
                for (var d = deep.Count - 1; d >= 0; d--) { deepOk &= ReferenceEquals(MountedCarrierPathObserver.CurrentIssuer, deep[d]); PatchMethods.MountedAttackApproachPostfix(deep[d]); }
                var keep = new UnitMoveTo(new Vector3(5f, 0f, 0f), 0.1f);
                PatchMethods.MountedAttackApproachPrefix(keep);   // throws: no postfix (strongly held here, so only the frame rule can discard it)
                var retainedSameFrame = MountedCarrierPathObserver.RetainedScopeCount;
                UnityEngine.Time.frameCount++;
                var next = new UnitMoveTo(new Vector3(6f, 0f, 0f), 0.1f);
                PatchMethods.MountedAttackApproachPrefix(next);
                var retainedNextFrame = MountedCarrierPathObserver.RetainedScopeCount; var nextIssuer = MountedCarrierPathObserver.CurrentIssuer;
                PatchMethods.MountedAttackApproachPostfix(next);
                PatchMethods.MountedAttackApproachPrefix(keep);   // throws again
                var retainedBeforeTeardown = MountedCarrierPathObserver.RetainedScopeCount;
                var teardownReady = MountedServiceTeardownSequence.ModDisable(new ReadyModDisableSteps());
                var retainedAfterTeardown = MountedCarrierPathObserver.RetainedScopeCount;
                var ok = retainedAfterThrow == 1 && ReferenceEquals(unrelatedIssuer, unrelated) && ReferenceEquals(innerIssuer, inner) && ReferenceEquals(outerIssuer, outer) &&
                    innerRequests == 1 && outerRequests == 1 && failedReleased && retainedAfterBalanced == 0 && deepOk && retainedSameFrame == 1 && retainedNextFrame == 1 &&
                    ReferenceEquals(nextIssuer, next) && retainedBeforeTeardown == 1 && teardownReady && retainedAfterTeardown == 0;
                GC.KeepAlive(keep);
                Check(ok, "Round 20 (R19-F5) forced-path issuer scope: throwing top-level approach left " + retainedAfterThrow + " scope; balanced unrelated issuer exact " + ReferenceEquals(unrelatedIssuer, unrelated) +
                    "; nested issuers inner " + ReferenceEquals(innerIssuer, inner) + " / outer " + ReferenceEquals(outerIssuer, outer) + ", forced requests attributed inner " + innerRequests + " / outer " + outerRequests +
                    "; failed command released (collected) " + failedReleased + ", scopes after the next balanced approach " + retainedAfterBalanced + "; 40 nested scopes keep each outer issuer " + deepOk +
                    "; a same-frame stale scope " + retainedSameFrame + " is discarded by the next frame's scope (retained " + retainedNextFrame + ", issuer exact " + ReferenceEquals(nextIssuer, next) +
                    "); production ModDisable ready " + teardownReady + ", scopes " + retainedBeforeTeardown + " -> " + retainedAfterTeardown);
            }

            // R13-F5 (round 14): NearTheEnd's borrowed time is ordinary movement's only. A step tick inside NearTheEnd stays within both step limits
            // (0.0439 m of physical travel left at 18 m/s: a 1/60 s request is cut to 0.0439/18 s, not the 0.30 m frame); the ordinary control keeps
            // the full requested tick (native's borrowed time). Domain arithmetic (MountedMovementState.Advance, the debit the adapter applies).
            {
                var stepState = new MountedMovementState { MetresStepped = 0.7f, MetresStepTravelled = 1.524f - 0.0439f, TimeStepped = 0.1f, TimeMoved = 0.1f };
                float stepDebit;
                var stepAllowed = stepState.Advance(1f / 60f, 18f, 9f, 1.524f, 0f, false, false, true, false, false, true, false, out stepDebit);
                var ordinary = new MountedMovementState { TimeMoved = 2.99f };
                float ordinaryDebit;
                var ordinaryAllowed = ordinary.Advance(1f / 60f, 9f, 9f, 1.524f, 2.99f, false, false, false, false, false, true, false, out ordinaryDebit);
                var ok = Math.Abs(stepAllowed - 0.0439f / 18f) < 1e-6f && stepState.MetresStepTravelled <= 1.524f + 1e-5f && stepState.MetresStepped <= 1.524f &&
                    Math.Abs(ordinaryAllowed - 1f / 60f) < 1e-7f && Math.Abs(ordinaryDebit - 1f / 60f) < 1e-7f;
                Check(ok, "R13-F5 (round 14) NearTheEnd: step tick returned " + stepAllowed.ToString("0.#######") + " s (physical remainder 0.0439 m at 18 m/s), physical travel " + stepState.MetresStepTravelled.ToString("0.#####") +
                    " m, native counter " + stepState.MetresStepped.ToString("0.#####") + " m; ordinary borrowed-time control returned " + ordinaryAllowed.ToString("0.#####") + " s with 0.01 s left (debit " + ordinaryDebit.ToString("0.#####") + " s)");
            }

            // (8e) a refused request keeps refusing every carrier its command issues before the sweep settles it: native's denial branch ends the
            // zeroed carrier, the provisional order replans in the same frame, and that replan carrier is decided (refused), never moved ungated.
            {
                var c = FirstTickPair(1.2f, 1f / 60f);
                var w = c.World;
                var click = ClickTick(c, w.Target);
                Frame(c);
                var p = c.Combat.FirstTickProposalForDiagnostics;
                var order = w.Rider.Commands.Standard as MountedPairAttackCommand;
                var first = LiveMountCarrier(c) as UnitMoveTo;
                UnityEngine.Time.frameCount++;
                TickMove(w, first);   // the decision frame's UnitMoveController stage alone: the gate refuses and zeroes the tick
                var refusedFirst = p != null && p.State == MountedFirstTickProposalState.Refused && w.Horse.Position.Equals(new Vector3(0f, 0f, 0f));
                first.Interrupt();    // native CanMoveInTurnBased's denial branch ends the zeroed path before the controller's per-frame sweep
                w.Horse.Commands.RemoveFinishedAndUpdateQueue();
                ActionTick(c, w.Rider);   // the provisional order sees its carrier ended without admission and replans
                var second = LiveMountCarrier(c) as UnitMoveTo;
                var start = w.Horse.Position; var moveBefore = w.Horse.CombatState.Cooldown.MoveAction;
                if (second != null) TickMove(w, second);
                var gatedSecond = second == null || ReferenceEquals(p.CarrierMove, second) && ReferenceEquals(c.Pair.movementState.FirstTickGate, p) &&
                    w.Horse.Position.Equals(start) && w.Horse.CombatState.Cooldown.MoveAction == moveBefore;
                c.Combat.Drive();
                Check(click.Admitted && refusedFirst && gatedSecond && c.Combat.FirstTickProposalForDiagnostics == null && c.Pair.movementState.FirstTickGate == null &&
                    LiveMountCarrier(c) == null && order != null && order.IsFinished && w.Horse.CombatState.Cooldown.MoveAction == 0f && order.StruckTargets.Count == 0,
                    "R6-F4/R6-F7 (round 7) replan after a refusal: first carrier " + Id(first) + " refused (" + c.Combat.LastFirstTickDecision + "); replan carrier " + Id(second) +
                    (second == null ? " not issued (" + order?.EndCause + ")" : " bound to the refused request and refused, displacement 0") + "; settled by the sweep, mount Move " +
                    w.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s");
            }

            // Controls: five-foot step, ordinary (unrestricted) single move and a spent mount Standard are ordinary orders, not provisional ones.
            foreach (var kase in new[] { "five-foot step", "ordinary single move", "spent mount Standard" })
            {
                var c = kase == "five-foot step" ? FirstTickPair(0.12f, 1f / 60f, false, true, true) :
                    kase == "ordinary single move" ? FirstTickPair(0.12f, 1f / 60f, false, false) : FirstTickPair(0.12f, 1f / 60f, false, false, false, true);
                var click = ClickTick(c, c.World.Target);
                var line = LastLine(c);
                var order = click.Order;
                for (var i = 0; i < 16 && order != null && !order.IsFinished && order.StruckTargets.Count == 0; i++) Frame(c);
                // Round 8 (R7-F1): the ordinary order's carrier is decided at its first tick in the mode then in force (an ordinary grant here).
                // Round 14 (R12-F1, changed expectation for the step control): an equal-speed step's native counter binds, so a debit of it can be
                // final; its click is a provisional request whose replacement commits at the before-debit decision. The other controls are unchanged.
                var stepCase = kase == "five-foot step";
                Check(click.Admitted && order != null && !order.ProvisionalOrder && c.Combat.LastFirstTickProposal != null && c.Combat.LastFirstTickProposal.Replacement == stepCase &&
                    c.Combat.LastFirstTickDecision.StartsWith(stepCase ? "committed:replacement:ordinary-movement" : "committed:order:ordinary-movement") && Field(line, "mountGrant") == "granted" &&
                    Field(line, "finalStepPossible") == (stepCase ? "True" : "False") && order.StruckTargets.Count == 1,
                    "R1-F6 (round 7; step expectation round 14) control, " + kase + ": " + (stepCase ? "a provisional step request committed at its first debit" : "an ordinary order") + " (step=" + Field(line, "mountStepMode") + ", single=" + Field(line, "mountSingleMove") + ", grant " +
                    Field(line, "mountGrantedMetres") + " m), no provisional order, one strike through the connected frames [" + c.Combat.LastFirstTickDecision + "; order " +
                    (order == null ? "-" : order.IsFinished + "/" + order.EndCause + "(" + order.EndCauseDetail + ")") + "; strikes " + (order == null ? 0 : order.StruckTargets.Count) + "]; mount Move " +
                    c.World.Horse.CombatState.Cooldown.MoveAction.ToString("0.####") + " s");
            }
        }

        // R4-F1 (round 5): no first-tick state remains on the mount: a carrier cancelled before its first tick leaves nothing behind, and an
        // affordable ground move in the same activation moves and is debited only for itself.
        private static void R5F1NoResidue()
        {
            var world = Corner();
            world.Horse.CombatSpeedMps = 9f;
            var c = Connect(world, false);
            var click = ClickTick(c, world.Target);
            var order = click.Order;
            world.TickSeconds = 1f / 60f;
            NativeTick(world, world.Rider);   // dispatch: the carrier is issued
            c.Combat.Cancel("owner cancelled before the first mount tick");
            NativeTick(world, world.Rider);
            var before = world.Horse.CombatState.Cooldown.MoveAction;
            var moved = world.MountMoveTick(0.15f);
            var debit = world.Horse.CombatState.Cooldown.MoveAction - before;
            Check(order != null && order.IsFinished && Math.Abs(moved - 0.15f) < 1e-4f && Math.Abs(debit - 1f / 60f) < 1e-4f && !(world.LastMountTick ?? "").Contains("refused"),
                "R4-F1 (round 5): after cancelling before the first mount tick, a later ground move of 0.15 m moved " + moved.ToString("0.####") + " m and was debited " +
                debit.ToString("0.####") + " s (its own tick only; " + world.LastMountTick + ")");
        }

        // R1-F7 / R4-F2 (round 5): the proof belongs to its producing input dispatch; the dispatch closes on a normal return AND on an exception
        // (the production transpiler's try/finally; here the same finally), and an unscoped IgnoreClick read keeps native's result.
        private static void R5F7Dispatch()
        {
            foreach (var kase in new[] { "standalone IgnoreClick read (no dispatch)", "dispatch whose handler never began", "dispatch that threw before its handler" })
            {
                var world = Corner();
                var c = Connect(world, false);
                var predictions = new UnitCommands(world.Rider);
                var predicted = new UnitAttack(world.Target);
                predicted.Init(world.Rider);
                predictions.Raw[(int)UnitCommand.CommandType.Standard] = predicted;
                c.Context.SetNativeClick(world.Target, predictions, false);
                var result = true;
                var overridden = false;
                if (kase.StartsWith("standalone")) { c.Pair.AdmitPairedAttackClick(c.Context, ref result); overridden = !result; }
                else
                {
                    c.Combat.BeginPointerDispatch();
                    try
                    {
                        c.Pair.AdmitPairedAttackClick(c.Context, ref result);
                        overridden = !result && c.Combat.PendingClickProofForDiagnostics != null;
                        if (kase.Contains("threw")) throw new InvalidOperationException("simulated failure inside PointerController.Tick before its handler");
                    }
                    catch (InvalidOperationException) { }
                    finally { c.Combat.EndPointerDispatch(); }
                }
                var outcome = new ClickOutcome();
                Request(c, world.Target, true, outcome);   // a separate same-target request reaching the stock handler in the same frame
                var order = outcome.Order;
                var expectOverride = !kase.StartsWith("standalone");
                Check(overridden == expectOverride && (order == null || !order.RouteBound) && c.Combat.IntentClickProofForDiagnostics == null && c.Combat.PendingClickProofForDiagnostics == null,
                    "R1-F7/R4-F2 (round 5) " + kase + ": " + (expectOverride ? "the postfix produced an override proof" : "native's result kept (" + c.Pair.LastAttackClickDecision + ")") +
                    "; the later same-target request received no authority (" + c.Combat.LastClickProofDisposition + "; order " + (order == null ? "none" : "route-bound=" + order.RouteBound) + ")");
            }
            {
                // R4-F2: the admission-aware direct input as its own scoped dispatch (NativeOrdinaryAttackInput.Click(true)): its IgnoreClick read and
                // its handler run inside one bracket, so its override reaches its own handler as a route-bound order.
                var world = Corner();
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                Check(click.Admitted && click.Order != null && click.Order.RouteBound,
                    "R4-F2/R1-F7 (round 5) control: the dispatch's own handler (pointer or scoped direct input) takes its proof as a route-bound order (" + c.Combat.LastClickProofDisposition + ")");
            }
        }

        // R1-F9 (round 4): the live wrapper's initialized plan changes (weapon or reach) while the click and a fresh native forecast still agree.
        private static void R4F9LivePlan()
        {
            foreach (var change in new[] { "reach", "weapon" })
                foreach (var inRange in new[] { false, true })
                {
                    var world = Corner();
                    var c = Connect(world, false);
                    var before = Observe(c);
                    var click = ClickTick(c, world.Target);
                    var start = world.Horse.Position;
                    if (change == "reach") click.Order.AllAttacks[0].WeaponRange = 0.5f; else click.Order.AllAttacks[0].Weapon = new ItemWeaponFacade();
                    if (inRange) world.Target.Position = new Vector3(1.5f, 0f, 0f);
                    for (var t = 0; t < 12 && !click.Order.IsFinished; t++) { NativeTick(world, world.Rider); NativeTick(world, world.Horse); }
                    var after = Observe(c);
                    Check(click.Admitted && click.Order.IsFinished && click.Order.EndCause.ToString().Contains("Cancel") && UnitAttack.NativeAttackStarts == 0 &&
                        world.Horse.Position.Equals(start) && after.MountMove == before.MountMove && after.RiderStandard,
                        "R1-F9 (round 4) live wrapper " + change + " changed, " + (inRange ? "in-range" : "moving") + " dispatch: cancelled before " + (inRange ? "the attack" : "movement") +
                        " (" + click.Order.EndCause + "); no movement, no debit, no strike");
                }
        }

        // R2-F3 (round 4): the nearest reachable lateral candidate is entered with a final heading of 101.25 degrees, between the sampled
        // headings; the rider sees the target only through a 25 mm opening around its eye for that heading; every other path is closed.
        private static void R4F3Heading()
        {
            foreach (var closed in new[] { false, true })
            {
                var candidate = new Vector3(4f - 2.8f * (float)Math.Cos(Math.PI / 8d), 0f, 2.8f * (float)Math.Sin(Math.PI / 8d));
                var heading = 101.25d * Math.PI / 180d;
                var corner = candidate - new Vector3((float)Math.Sin(heading), 0f, (float)Math.Cos(heading));
                var eye = candidate + new Vector3(-0.4f * (float)Math.Cos(heading), 0f, 0.4f * (float)Math.Sin(heading));
                Func<Vector3, Vector3, bool> opening = (from, to) => Math.Sqrt((from.x - eye.x) * (from.x - eye.x) + (from.z - eye.z) * (from.z - eye.z)) > 0.025;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), opening, true);
                world.Horse.Orientation = 0f;
                world.Rider.Position = world.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                var agent = world.Horse.View.AgentASP;
                agent.Route = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f ? new List<Vector3> { from, corner, to } : new List<Vector3> { from, to };
                agent.Outcome = (from, to) => !closed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f ? PathCompleteState.Complete : PathCompleteState.Error;
                var sampled = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
                sampled.Init(world.Rider);
                var guessedAdmits = 0;
                for (var h = 0; h < 16; h++)
                {
                    var a = h * 2d * Math.PI / 16;
                    if (sampled.EvaluateNativeAdmissionAtMountPosition(candidate, new Vector3((float)Math.Sin(a), 0f, (float)Math.Cos(a))) == MountedPairNativeAdmissionState.Admitted) guessedAdmits++;
                }
                var actual = sampled.EvaluateNativeAdmissionAtMountPosition(candidate, candidate - corner);
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var probe = c.Combat.LastAttackClickProbe;
                var queried = agent.Endpoints.Any(p => Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, candidate) < 1e-3f) || probe != null && probe.LateralQueries > 0;
                if (closed)
                {
                    Check(!click.Admitted && click.Order == null && probe != null && probe.LateralQueries >= 1 && probe.LateralQueries <= PairedAttackClickProbe.MaxLateralQueries,
                        "R2-F3 (round 4) control: every path closed: native refusal kept after " + probe?.LateralQueries + " bounded queries (" + probe?.Detail + ")");
                    continue;
                }
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(guessedAdmits == 0 && actual == MountedPairNativeAdmissionState.Admitted && click.Admitted && probe.Lateral && queried && probe.LateralQueries <= PairedAttackClickProbe.MaxLateralQueries &&
                    admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1,
                    "R2-F3 (round 4): no sampled heading admits at the candidate (0 of 16), the actual 101.25-degree arrival does; the candidate was queried (" + probe.LateralQueries +
                    " queries, " + probe.Detail + "), the exact carrier released at " + world.Horse.Position + ", one strike");
            }
        }

        // R4-F3 (round 5): six blind nearby ring points must not crowd out a farther flank whose sight does not depend on the heading.
        // Mount (0,0), target (4,0), ring 2.8 m, rider at the mount (aligned offsets); sight opens at z >= 2.70; only (4, 2.8) is reachable.
        private static void R5F3Crowding()
        {
            var flank = new Vector3(4f, 0f, 2.8f);
            var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), (from, to) => from.z < 2.70f, true);
            var agent = world.Horse.View.AgentASP;
            agent.Outcome = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, flank) < 0.05f ? PathCompleteState.Complete : PathCompleteState.Error;
            var c = Connect(world, false);
            var click = ClickTick(c, world.Target);
            var probe = c.Combat.LastAttackClickProbe;
            Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
            RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
            Check(click.Admitted && probe.Lateral && probe.LateralQueries <= PairedAttackClickProbe.MaxLateralQueries && admission == MountedPairNativeAdmissionState.Admitted &&
                UnitAttack.NativeAttackStarts == 1 && world.Horse.Position.z >= 2.69f,
                "R4-F3 (round 5): the heading-independent flank was queried (" + probe.LateralQueries + " queries, " + probe.Detail + "), route " + probe.RouteMetres.ToString("0.##") +
                " m within the grant, exact release at " + world.Horse.Position + ", one strike");
        }

        // R5-F1 (round 6): the reviewer's R4F3Heading variation. All path outcomes kept; sight is added wherever the projected eye's x is at
        // least 4.5 m, so the farther ring points pass the sampled headings (their paths all return Error), while the only reachable endpoint
        // (1.413, 1.072) admits only on its actual 101.25-degree arrival. The tier share keeps nearby unknown-heading candidates inside the
        // six-query set. Control: every path closed (refusal within the cap).
        private static void R6F1Interleave()
        {
            foreach (var closed in new[] { false, true })
            {
                var candidate = new Vector3(4f - 2.8f * (float)Math.Cos(Math.PI / 8d), 0f, 2.8f * (float)Math.Sin(Math.PI / 8d));
                var heading = 101.25d * Math.PI / 180d;
                var corner = candidate - new Vector3((float)Math.Sin(heading), 0f, (float)Math.Cos(heading));
                var eye = candidate + new Vector3(-0.4f * (float)Math.Cos(heading), 0f, 0.4f * (float)Math.Sin(heading));
                Func<Vector3, Vector3, bool> blocked = (from, to) => Math.Sqrt((from.x - eye.x) * (from.x - eye.x) + (from.z - eye.z) * (from.z - eye.z)) > 0.025 && from.x < 4.5f;
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), blocked, true);
                world.Horse.Orientation = 0f;
                world.Rider.Position = world.Horse.Position + new Vector3(-0.4f, 0f, 0f);
                var agent = world.Horse.View.AgentASP;
                agent.Route = (from, to) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f ? new List<Vector3> { from, corner, to } : new List<Vector3> { from, to };
                agent.Outcome = (from, to) => !closed && Kingmaker.Utility.GeometryUtils.MechanicsDistance(to, candidate) < 1e-3f ? PathCompleteState.Complete : PathCompleteState.Error;
                // The sampled-visible tier, computed with the production strike admission over the probe's own ring and 16 headings.
                var sampler = new MountedPairSingleAttack(world.Target, world.Rider, world.Horse, true, false);
                sampler.Init(world.Rider);
                var ring = sampler.PairApproachRadius - PairedAttackClickProbe.LateralInsetMetres;
                var visible = new List<Vector3>();
                for (var k = 0; k < PairedAttackClickProbe.LateralDirections; k++)
                {
                    var angle = k * 2d * Math.PI / PairedAttackClickProbe.LateralDirections;
                    var point = new Vector3(4f + ring * (float)Math.Cos(angle), 0f, ring * (float)Math.Sin(angle));
                    for (var h = 0; h < 16; h++)
                    {
                        var a = h * 2d * Math.PI / 16;
                        if (sampler.EvaluateNativeAdmissionAtMountPosition(point, new Vector3((float)Math.Sin(a), 0f, (float)Math.Cos(a))) == MountedPairNativeAdmissionState.Admitted) { visible.Add(point); break; }
                    }
                }
                var candidateSampled = visible.Any(p => Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, candidate) < 1e-3f);
                var c = Connect(world, false);
                var click = ClickTick(c, world.Target);
                var probe = c.Combat.LastAttackClickProbe;
                var lateralEndpoints = agent.Endpoints.Skip(1).ToList();   // the first query is the target-centred route
                var candidateIndex = lateralEndpoints.FindIndex(p => Kingmaker.Utility.GeometryUtils.MechanicsDistance(p, candidate) < 0.05f);
                var visibleBefore = (candidateIndex < 0 ? lateralEndpoints : lateralEndpoints.Take(candidateIndex).ToList()).Count(p => visible.Any(v => Kingmaker.Utility.GeometryUtils.MechanicsDistance(v, p) < 0.05f));
                var queried = string.Join(" ", lateralEndpoints.Select(p => p.ToString()).ToArray());
                if (closed)
                {
                    Check(!click.Admitted && click.Order == null && probe != null && probe.LateralQueries == PairedAttackClickProbe.MaxLateralQueries && c.World.Rider.HasStandard,
                        "R5-F1 (round 6) control: every path closed: native refusal kept after " + probe?.LateralQueries + " bounded queries (" + queried + "; " + probe?.Detail + ")");
                    continue;
                }
                Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                RunToStrike(c, click.Order, out atRelease, out atStrike, out admission);
                Check(visible.Count > PairedAttackClickProbe.MaxLateralQueries && !candidateSampled && click.Admitted && probe.Lateral && candidateIndex >= 0 &&
                    probe.LateralQueries == candidateIndex + 1 && probe.LateralQueries <= PairedAttackClickProbe.MaxLateralQueries && visibleBefore <= PairedAttackClickProbe.LateralTierShare &&
                    admission == MountedPairNativeAdmissionState.Admitted && UnitAttack.NativeAttackStarts == 1 && atRelease != null &&
                    Kingmaker.Utility.GeometryUtils.MechanicsDistance(world.Horse.Position, candidate) <= probe.ArrivalRadius + 1e-3f,
                    "R5-F1 (round 6): " + visible.Count + " sampled-visible ring points (the pre-round-6 order queried all of them first, exhausting the cap); the unknown-heading candidate " +
                    candidate + " was lateral query " + (candidateIndex + 1) + " of at most " + PairedAttackClickProbe.MaxLateralQueries + " after " + visibleBefore +
                    " visible-but-unpathable endpoints (queried: " + queried + "); " + probe.Detail + "; exact carrier release at " + world.Horse.Position + " (radius " +
                    probe.ArrivalRadius.ToString("0.###") + " m); one strike; " + Spend3(atRelease, atStrike));
            }
        }

        private static string Spend3(Spend atRelease, Spend atStrike) => "release{" + atRelease + "} strike{" + atStrike + "}";

        // R5-F2 (round 6): the ACTUAL NativeOrdinaryAttackInput.Click(true) with highlighted/predicted A and the adapter's stored target B: the
        // scoped dispatch is bound to B, so A's paired override is refused before dispatch and B receives no order. Controls: a matching target
        // gets a route-bound order whose carrier revalidates and strikes once; Click(false) is ordinary unscoped input.
        private sealed class DelegateClickHandler : Kingmaker.Controllers.Clicks.IClickEventHandler
        {
            public Func<UnityEngine.GameObject, bool> Handle;
            public bool OnClick(UnityEngine.GameObject gameObject, Vector3 worldPosition, int button, bool simulate, bool muteEvents) => Handle(gameObject);
        }

        private static void R6F2DirectTarget()
        {
            foreach (var kase in new[] { "predicted A, stored B", "predicted A, stored A", "Click(false) on B" })
            {
                var world = Corner();
                var c = Connect(world, false);
                var a = world.Target;
                var b = new UnitEntityData("hostile-b") { Position = new Vector3(3.6f, 0f, -1.6f) };
                b.View.Corpulence = 0.5f;
                var predictions = new UnitCommands(world.Rider);
                var predicted = new UnitAttack(a);
                predicted.Init(world.Rider);
                predictions.Raw[(int)UnitCommand.CommandType.Standard] = predicted;
                c.Context.SetNativeClick(a, predictions, false);
                // Native IgnoreClick drops the click (its path check fails); the ACTUAL postfix decides on native's result.
                c.Context.IgnoreClickHook = () => { var result = true; c.Pair.AdmitPairedAttackClick(c.Context, ref result); return result; };
                var outcome = new ClickOutcome();
                var handled = new List<string>();
                var handler = new DelegateClickHandler { Handle = go =>
                {
                    var unit = ReferenceEquals(go, a.View.gameObject) ? a : ReferenceEquals(go, b.View.gameObject) ? b : null;
                    handled.Add(unit == null ? "<none>" : unit.UniqueId);
                    Request(c, unit, true, outcome);
                    return true;
                } };
                var before = Observe(c);
                var generation = c.Combat.StockAttackIntentStartCount;
                var closures = PatchMethods.PointerDispatchClosures;
                var input = new KingmakerMountedCombat.Diagnostics.NativeOrdinaryAttackInput(kase.Contains("stored A") ? a : b, handler);
                var dispatched = input.Click(!kase.StartsWith("Click(false)"));
                var decision = c.Pair.LastAttackClickDecision;
                if (kase == "predicted A, stored B")
                {
                    var after = Observe(c);
                    Check(!dispatched && handled.Count == 0 && outcome.Order == null && world.Rider.Commands.Standard == null && c.Combat.StockAttackIntentStartCount == generation &&
                        decision == "NativeDecision:dispatch-target-mismatch" && c.Combat.PendingClickProofForDiagnostics == null && c.Combat.IntentClickProofForDiagnostics == null &&
                        PatchMethods.PointerDispatchClosures == closures + 1 && after.RiderStandard && after.MountMove == before.MountMove,
                        "R5-F2 (round 6) " + kase + ": production Click(true) bound its dispatch to B; A's override refused before dispatch (" + decision + "); handler calls " +
                        handled.Count + ", no order, intent starts " + generation + " -> " + c.Combat.StockAttackIntentStartCount + ", no proof; " + after);
                    continue;
                }
                if (kase == "predicted A, stored A")
                {
                    var order = outcome.Order;
                    Spend atRelease, atStrike; MountedPairNativeAdmissionState admission;
                    RunToStrike(c, order, out atRelease, out atStrike, out admission);
                    Check(dispatched && handled.SequenceEqual(new[] { a.UniqueId }) && order != null && order.RouteBound && admission == MountedPairNativeAdmissionState.Admitted &&
                        UnitAttack.NativeAttackStarts == 1 && (c.Combat.LastRouteRevalidation ?? "").StartsWith("proven=True") && PatchMethods.PointerDispatchClosures == closures + 1,
                        "R5-F2 (round 6) " + kase + ": the matching target's override reached its own handler as a route-bound order (" + c.Combat.LastClickProofDisposition +
                        "); carrier revalidation " + (c.Combat.LastRouteRevalidation ?? "<none>").Split(';')[0] + "; one strike; " + Spend3(atRelease, atStrike));
                    continue;
                }
                Check(dispatched && handled.SequenceEqual(new[] { b.UniqueId }) && outcome.Order != null && !outcome.Order.RouteBound && PatchMethods.PointerDispatchClosures == closures,
                    "R5-F2 (round 6) " + kase + ": unscoped ordinary input dispatched B's own click (no IgnoreClick read, no dispatch bracket, order route-bound=" +
                    (outcome.Order == null ? "none" : outcome.Order.RouteBound.ToString()) + ")");
            }
        }

        private static int Main()
        {
            try { R3F1(); } catch (Exception exception) { Check(false, "R3-F1 threw " + exception); }
            try { R4F2(); } catch (Exception exception) { Check(false, "R4-F2 threw " + exception); }
            try { PR1R1F4(); } catch (Exception exception) { Check(false, "PR-1 R1-F4 threw " + exception); }
            try { PR1R1F6(); } catch (Exception exception) { Check(false, "PR-1 R1-F6 threw " + exception); }
            try { PR1R1F7(); } catch (Exception exception) { Check(false, "PR-1 R1-F7 threw " + exception); }
            try { PR1Round2(); } catch (Exception exception) { Check(false, "PR-1 round 2 threw " + exception); }
            try { Round4DeliveryAndRetirement(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 2 threw " + exception); }
            try { Round4QueueLifecycle(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 3 threw " + exception); }
            try { Round4ManualTargets(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 4 threw " + exception); }
            try { Round4Approach(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 6 threw " + exception); }
            try { Round4WaitAndClocks(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 7 threw " + exception); }
            try { Round5TerminalAndFeedback(); } catch (Exception exception) { Check(false, "PR-1 round 5 terminal/feedback threw " + exception); }
            try { Round5Cancellation(); } catch (Exception exception) { Check(false, "PR-1 round 5 cancellation threw " + exception); }
            try { Round5ClickAttribution(); } catch (Exception exception) { Check(false, "PR-1 round 5 click attribution threw " + exception); }
            try { Round5Approach(); } catch (Exception exception) { Check(false, "PR-1 round 5 approach threw " + exception); }
            try { Round5Dispatch(); } catch (Exception exception) { Check(false, "PR-1 round 5 dispatch threw " + exception); }
            try { Round6TerminalThroughDrive(); } catch (Exception exception) { Check(false, "PR-1 round 6 terminal drive threw " + exception); }
            try { Round6Charges(); } catch (Exception exception) { Check(false, "PR-1 round 6 charges threw " + exception); }
            try { Round6Retarget(); } catch (Exception exception) { Check(false, "PR-1 round 6 retarget threw " + exception); }
            try { Round6Invalidation(); } catch (Exception exception) { Check(false, "PR-1 round 6 invalidation threw " + exception); }
            try { Round6ClickAttribution(); } catch (Exception exception) { Check(false, "PR-1 round 6 click attribution threw " + exception); }
            try { Round6StationaryAutomatic(); } catch (Exception exception) { Check(false, "PR-1 round 6 stationary automatic threw " + exception); }
            try { Round7RemainingRoutine(); } catch (Exception exception) { Check(false, "PR-1 round 7 remaining routine threw " + exception); }
            try { Round8ExplicitSingleAttack(); } catch (Exception exception) { Check(false, "PR-1 round 8 explicit single attack threw " + exception); }
            try { Round8TieBreakSight(); } catch (Exception exception) { Check(false, "PR-1 round 8 tie-break sight threw " + exception); }
            try { Round9NativeAdmission(); } catch (Exception exception) { Check(false, "PR-1 round 9 native admission threw " + exception); }
            try { Round9InvalidationBeforeGeometry(); } catch (Exception exception) { Check(false, "PR-1 round 9 invalidation before geometry threw " + exception); }
            try { Round10RetainedNativeAdmission(); } catch (Exception exception) { Check(false, "PR-1 round 10 retained native admission threw " + exception); }
            try { Round10DislikeAfterAdmission(); } catch (Exception exception) { Check(false, "PR-1 round 10 dislike after admission threw " + exception); }
            try { Round11RetainedAdmission(); } catch (Exception exception) { Check(false, "PR-1 round 11 retained admission threw " + exception); }
            try { Round11InstalledWrapper(); } catch (Exception exception) { Check(false, "PR-1 round 11 installed wrapper threw " + exception); }
            try { Round11QueuedPromotion(); } catch (Exception exception) { Check(false, "PR-1 round 11 queued promotion threw " + exception); }
            try { Round11InvalidationWithAdmissionLoss(); } catch (Exception exception) { Check(false, "PR-1 round 11 invalidation with admission loss threw " + exception); }
            try { Round11ExactAssertionRejectsAppendedCode(); } catch (Exception exception) { Check(false, "PR-1 round 11 exact assertion threw " + exception); }
            try { Round12AiOffBeforeDelivery(); } catch (Exception exception) { Check(false, "PR-1 round 12 AI off threw " + exception); }
            try { Round12MountOnlyHold(); } catch (Exception exception) { Check(false, "PR-1 round 12 mount-only Hold threw " + exception); }
            try { Round12MissingMemory(); } catch (Exception exception) { Check(false, "PR-1 round 12 missing memory threw " + exception); }
            try { Round13AiOffAfterDelivery(); } catch (Exception exception) { Check(false, "PR-1 round 13 AI off after delivery threw " + exception); }
            try { Round13HeldDuringApproach(); } catch (Exception exception) { Check(false, "PR-1 round 13 held during approach threw " + exception); }
            try { Round13OneProbePerDrive(); } catch (Exception exception) { Check(false, "PR-1 round 13 one probe per drive threw " + exception); }
            try { Round14HeldFinalActRecovery(); } catch (Exception exception) { Check(false, "PR-1 round 14 held final act threw " + exception); }
            try { Round14ReleasedStrikeRangeLoss(); } catch (Exception exception) { Check(false, "PR-1 round 14 released range loss threw " + exception); }
            try { Round7DeliveredOutcome(); } catch (Exception exception) { Check(false, "PR-1 round 7 delivered outcome threw " + exception); }
            try { Round7TerminalToDispatch(); } catch (Exception exception) { Check(false, "PR-1 round 7 terminal to dispatch threw " + exception); }
            try { Round7RestrictedProposal(); } catch (Exception exception) { Check(false, "PR-1 round 7 restricted proposal threw " + exception); }
            try { Round7TieBreak(); } catch (Exception exception) { Check(false, "PR-1 round 7 tie-break threw " + exception); }
            try { Round7InvalidationParity(); } catch (Exception exception) { Check(false, "PR-1 round 7 invalidation parity threw " + exception); }
            try { R4F1(); } catch (Exception exception) { Check(false, "R4-F1 threw " + exception); }
            try { R5F2(); } catch (Exception exception) { Check(false, "R5-F2 threw " + exception); }
            try { R6F1(); } catch (Exception exception) { Check(false, "R6-F1 threw " + exception); }
            try { R7F1(); } catch (Exception exception) { Check(false, "R7-F1 threw " + exception); }
            try { R6F2(); } catch (Exception exception) { Check(false, "R6-F2 threw " + exception); }
            try { R2F10Connected(); } catch (Exception exception) { Check(false, "PR-2 R1-F10 threw " + exception); }
            try { R2F1BeyondGrant(); } catch (Exception exception) { Check(false, "PR-2 R1-F1 threw " + exception); }
            try { R2F7ProofOwnership(); } catch (Exception exception) { Check(false, "PR-2 R1-F7 threw " + exception); }
            try { R2F8FreshTargetLife(); } catch (Exception exception) { Check(false, "PR-2 R1-F8 threw " + exception); }
            try { R2F5PathOutcome(); } catch (Exception exception) { Check(false, "PR-2 R1-F5 threw " + exception); }
            try { R2F3Discovery(); } catch (Exception exception) { Check(false, "PR-2 R1-F3 threw " + exception); }
            try { R2F2Bend(); } catch (Exception exception) { Check(false, "PR-2 R1-F2 threw " + exception); }
            try { R2F4Lateral(); } catch (Exception exception) { Check(false, "PR-2 R1-F4 threw " + exception); }
            try { R2F9Plan(); } catch (Exception exception) { Check(false, "PR-2 R1-F9 threw " + exception); }
            try { R3F3SixPointFive(); } catch (Exception exception) { Check(false, "PR-2 R1-F3 (round 3) threw " + exception); }
            try { R3PathEnd(); } catch (Exception exception) { Check(false, "PR-2 R2-F2 threw " + exception); }
            try { R3F2StartPose(); } catch (Exception exception) { Check(false, "PR-2 R1-F2 (round 3) threw " + exception); }
            try { R3F4Lateral(); } catch (Exception exception) { Check(false, "PR-2 R1-F4/R2-F3 threw " + exception); }
            try { R3F9Prediction(); } catch (Exception exception) { Check(false, "PR-2 R1-F9 (round 3) threw " + exception); }
            try { R4F10Pairing(); } catch (Exception exception) { Check(false, "PR-2 R1-F10 (round 4) threw " + exception); }
            try { R7ProvisionalLifecycle(); } catch (Exception exception) { Check(false, "PR-2 R1-F6/R5-F3/R6-F1..F7 (round 7) threw " + exception); }
            try { R15TeardownLifecycle(); } catch (Exception exception) { Check(false, "PR-2 R12-F4/R14-F5 (round 15, production teardown sequence) threw " + exception); }
            try { R16ProductionDismountTeardown(); } catch (Exception exception) { Check(false, "PR-2 R12-F4 (round 16, production dismount cleanup) threw " + exception); }
            try { R5F1NoResidue(); } catch (Exception exception) { Check(false, "PR-2 R4-F1 (round 5) threw " + exception); }
            try { R5F7Dispatch(); } catch (Exception exception) { Check(false, "PR-2 R1-F7/R4-F2 (round 5) threw " + exception); }
            try { R5F3Crowding(); } catch (Exception exception) { Check(false, "PR-2 R4-F3 (round 5) threw " + exception); }
            try { R4F9LivePlan(); } catch (Exception exception) { Check(false, "PR-2 R1-F9 (round 4) threw " + exception); }
            try { R4F3Heading(); } catch (Exception exception) { Check(false, "PR-2 R2-F3 (round 4) threw " + exception); }
            try { R6F1Interleave(); } catch (Exception exception) { Check(false, "PR-2 R5-F1 (round 6) threw " + exception); }
            try { R6F2DirectTarget(); } catch (Exception exception) { Check(false, "PR-2 R5-F2 (round 6) threw " + exception); }
            Console.WriteLine("TOTAL PASS=" + passes + " FAIL=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
namespace KingmakerMountedCombat.Integration
{
    // PR #70 R12-F4 / R14-F5 (round 15): the ACTUAL production teardown sequence (CompositionRoot and MountedLifecycleSubscriber delegate to it).
    /* TEARDOWN_SEQUENCE */
}
namespace Harness
{
    using System;
    using System.Linq;
    using KingmakerMountedCombat.Integration;

    // The world registry's drain: the ACTUAL MountedWorldService.SettleOutstandingPairCleanup over this harness's pair snapshot.
    internal sealed class HarnessWorldService
    {
        private sealed class TeardownPair { public MountedCombatController Combat; }
        private readonly TeardownPair[] pairs;
        internal HarnessWorldService(params MountedCombatController[] combats) { pairs = combats.Select(combat => new TeardownPair { Combat = combat }).ToArray(); }
        private TeardownPair[] PairSnapshot => pairs;
        /* WORLD_SETTLE_OUTSTANDING */
    }

    // PR #70 R12-F4 (round 16): CompositionRoot.TeardownSteps over the production collaborators: the world drain (spliced), the relationship's
    // registry readiness (spliced), the lifecycle's HandleModDisable (spliced); the persistence fences are flags; the Shield release succeeds;
    // RemoveHooksAndServices (patches.Dispose and the release after it) clears the observable HooksPatched flag.
    internal sealed class ProductionTeardownSteps : MountedServiceTeardownSequence.ISteps
    {
        private readonly MountedWorldService world; private readonly GameMountedRelationshipService relationship; private readonly MountedLifecycleSubscriber lifecycle;
        public bool Enabled = true, Save, Load, Worker, Area, Restoration, HooksPatched = true;
        public int DrainCalls;
        internal ProductionTeardownSteps(MountedWorldService world, GameMountedRelationshipService relationship, MountedLifecycleSubscriber lifecycle) { this.world = world; this.relationship = relationship; this.lifecycle = lifecycle; }
        public bool IsEnabled => Enabled;
        public bool SaveSuspended => Save;
        public bool LoadInFlight => Load;
        public bool ActiveSaveWorkerRunning => Worker;
        public bool AreaTransitionPending => Area;
        public bool CombatRestorationPending => Restoration;
        public bool ChargeFenced => false;
        public bool TeardownOwnersReady => relationship.AllTeardownOwnersReady;
        public bool DrainOutstandingPairCleanup() { DrainCalls++; return world.SettleOutstandingPairCleanup(); }
        public bool OwnedWorkerCanStillCommit() => Worker;
        public bool HandleModDisable() => lifecycle.HandleModDisable();
        public bool ReleaseForUnload() => true;
        public void RemoveHooksAndServices() { HooksPatched = false; }
        internal MountedServiceTeardownSequence.Verdict SetEnabledFalse()
        {
            var verdict = MountedServiceTeardownSequence.BeginDisable(this);
            if (verdict == MountedServiceTeardownSequence.Verdict.Ready) Enabled = false;
            return verdict;
        }
        internal MountedServiceTeardownSequence.Verdict Unload() => MountedServiceTeardownSequence.Unload(this);
    }
    // The services CompositionRoot.TeardownSteps and MountedLifecycleSubscriber.ModDisableSteps bind, as observable harness state (see
    // R15TeardownLifecycle for what each stands for).
    internal sealed class HarnessTeardownSteps : MountedServiceTeardownSequence.ISteps, MountedServiceTeardownSequence.IModDisableSteps
    {
        private readonly HarnessWorldService world; private readonly MountedCombatController combat; private readonly Action cancellation;
        public bool Enabled = true, Save, Load, Worker, Area, Restoration, HooksPatched = true;
        public int DrainCalls, CleanupCalls;
        internal HarnessTeardownSteps(HarnessWorldService world, MountedCombatController combat, Action cancellation) { this.world = world; this.combat = combat; this.cancellation = cancellation; }
        public bool IsEnabled => Enabled;
        public bool SaveSuspended => Save;
        public bool LoadInFlight => Load;
        public bool ActiveSaveWorkerRunning => Worker;
        public bool AreaTransitionPending => Area;
        public bool CombatRestorationPending => Restoration;
        public bool ChargeFenced => false;
        public bool AdmissionFenced => false;
        public bool TeardownOwnersReady => !combat.HasOutstandingCleanup;
        public bool DrainOutstandingPairCleanup() { DrainCalls++; return world.SettleOutstandingPairCleanup(); }
        public bool OwnedWorkerCanStillCommit() => Worker;
        public bool HandleModDisable() => MountedServiceTeardownSequence.ModDisable(this);
        public bool DrainChargeOwnership() => true;
        public string LastCleanupError = "none";
        public bool CleanupPairs() { CleanupCalls++; try { cancellation(); return true; } catch (Exception error) { LastCleanupError = error.GetType().Name + ": " + error.Message + " at " + string.Join(" < ", (error.StackTrace ?? "").Split(new[] { (char)10 }, StringSplitOptions.RemoveEmptyEntries).Take(6).Select(line => line.Trim().Split(' ')[1]).ToArray()); return false; } }
        public bool ReleaseForUnload() => true;
        public void RemoveHooksAndServices() { HooksPatched = false; }
        // CompositionRoot.SetEnabled(false): the sequence, then (only when it is Ready) IsEnabled = false; disable never unpatches.
        internal MountedServiceTeardownSequence.Verdict SetEnabledFalse()
        {
            var verdict = MountedServiceTeardownSequence.BeginDisable(this);
            if (verdict == MountedServiceTeardownSequence.Verdict.Ready) Enabled = false;
            return verdict;
        }
        // CompositionRoot.Dispose: the sequence (a refusal throws to Main.OnUnload, which reports false to UMM; the root stays).
        internal MountedServiceTeardownSequence.Verdict Unload() => MountedServiceTeardownSequence.Unload(this);
    }
}
