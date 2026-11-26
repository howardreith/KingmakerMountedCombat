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
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
        public static float Distance(Vector3 a, Vector3 b) { var d = a - b; return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z); }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public override string ToString() => "(" + x.ToString("0.###") + "," + z.ToString("0.###") + ")";
    }

    public static class Time { public static int frameCount; }
}

namespace Pathfinding
{
    public class Path
    {
        public List<Vector3> vectorPath = new List<Vector3>();
        public bool error;
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
    }

    public sealed class TurnController
    {
        public enum TurnStatus { Preparing, Acting, Ended }
        public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;
        public TurnStatus Status = TurnStatus.Preparing;
        public bool IsActing;
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
    }

    public sealed class GameModeFacade { private readonly string name; public GameModeFacade(string name) { this.name = name; } public override string ToString() => name; }
    public sealed class PlayerFacade { public bool IsInCombat = true; }

    public sealed class TimeControllerFacade { public TimeSpan GameTime; }

    public sealed class TurnBasedCombatControllerFacade { public TurnBased.Controllers.TurnController CurrentTurn; }
}

namespace Kingmaker.View
{
    public sealed class UnitMovementAgent
    {
        // Native route stand-in: the polyline the harness world returns for this agent (the mount) toward a destination.
        public Func<Vector3, Vector3, List<Vector3>> Route;
        public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;
        public int FindPathCount;
        public Pathfinding.Path FindPath(Vector3 destination, Pathfinding.OnPathDelegate callback, float? approachRadius = null, bool needLOS = false,
            int ignoreBlockerId = 0)
        {
            FindPathCount++;
            var path = new Pathfinding.Path { vectorPath = Route(Unit.Position, destination) };
            callback?.Invoke(path);
            return path;
        }
    }

    public sealed class UnitEntityView
    {
        public float Corpulence = 0.5f;
        public int StopMovingCount;
        public UnitMovementAgent AgentASP;
        public void StopMoving() { StopMovingCount++; }
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
        public Vector3 EyePosition => Position + new Vector3(0f, 1.6f, 0f);
        public Kingmaker.View.UnitEntityView View = new Kingmaker.View.UnitEntityView();
        public Kingmaker.UnitLogic.Commands.UnitCommands Commands;
        public UnitDescriptor Descriptor = new UnitDescriptor();
        public bool IsInState = true;
        public float CombatSpeedMps = 9f;
        // Native UnitCombatState.ManualTarget (set by UnitCommands.UpdateCombatTarget inside Run and by ClickUnitHandler.OnClick).
        public Kingmaker.Controllers.Combat.UnitCombatState CombatState = new Kingmaker.Controllers.Combat.UnitCombatState();
        public bool HoldState;
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
        public bool IsStandardActionRestricted() => false;
        public string CharacterName => UniqueId;
        public UnitEntityData(string id) { UniqueId = id; Commands = new Kingmaker.UnitLogic.Commands.UnitCommands(this); }
        public float DistanceTo(UnitEntityData other) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(Position, other.Position);
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
        public UnitEntityData ManualTarget;
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
        public AiActionFacade AiAction;
        // Native UnitCommand.IsMoveUnit (set by TickApproaching while the command approaches).
        public bool IsMoveUnit;
        public virtual UnitEntityData TargetUnit => null;
        // Native act event due on the next Tick (the attack animation's act, or its pretend-act time).
        public bool ActEventDue;
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
                if (result != ResultType.None) { Result = result; OnEnded(); return; }
            }
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
                AllAttacks.Add(new AttackHandInfo { WeaponRange = executor.WeaponRange, Weapon = executor.FirstWeapon, Hand = executor.PrimaryLimb });
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
        public readonly Vector3 Goal;
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
        public UnitCommand[] Raw => raw;
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
        public static Action<UnitCommands, UnitCommand> QueuePostfix;
        // Native Run (slot acceptance): the slot's previous occupant is interrupted and replaced; UpdateCombatTarget writes the manual
        // target for a MarkManualTarget command without AiAction.
        public void Run(UnitCommand command) => RunInternal(command, false);
        private void RunInternal(UnitCommand command, bool fromQueue)
        {
            if (fromQueue && command is HarnessRefusedCommand) { RunPostfix?.Invoke(this, command, true); return; }
            if (fromQueue && PromotionPrefix != null && !PromotionPrefix(command)) { RunPostfix?.Invoke(this, command, true); return; }
            if (command.Executor == null) command.Init(owner);
            var existing = raw[(int)command.Type];
            if (existing != null && existing != command && !existing.IsFinished) existing.Interrupt();
            raw[(int)command.Type] = command;
            RunCount++;
            Count(installs, command);
            if (command is UnitMoveTo) MoveSubmissions++;
            // Native Run clears its owner's Hold (NativeRunHoldBoundary; the pair's keep-Hold exception is not modelled here).
            owner.HoldState = false; Count(holdWrites, command);
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
        public void RemoveFinishedAndUpdateQueue()
        {
            for (var i = 0; i < raw.Length; i++) if (raw[i] != null && raw[i].IsFinished) raw[i] = null;
            var first = Queue.First?.Value;
            // Native promotion of a Standard command also requires the Move slot to be empty (Run pairs Move/Standard).
            if (first != null && raw[(int)first.Type] == null &&
                (first.Type != UnitCommand.CommandType.Standard || raw[(int)UnitCommand.CommandType.Move] == null)) { Queue.RemoveFirst(); RunInternal(first, true); }
        }
    }
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
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Logging;
    using TurnBased.Controllers;
    using Kingmaker;
    using Harness;

    /* ADMISSION_ENUM */

    /* CHILD_ATTACK_CLASS */

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
        public override CommandType Type => CommandType.Standard;
        public override Vector3 ApproachPoint => Vector3.zero;
        public void TickMountedApproach() { }
    }

    internal sealed class HarnessCarrierHost { public bool TryTickCarriedApproach(UnitCommand command, bool shell) => false; }

    internal static class PatchBridge
    {
        public static KingmakerMountedCombat.Logging.IModLogger Logger = new HarnessLogger();
        public static HarnessCarrierHost CombatFor(UnitEntityData unit) => null;
    }

    internal static class PatchMethods
    {
        internal static bool ChargeExecutionPrefix(UnitCommand __instance) => true;
        private static bool IsRelationshipControlShell(UnitCommand command) => false;
        /* APPROACH_PREFIX */
        /* ADMIT_EXPLICIT_PROMOTION */
        // The pairs' controllers (production: PatchBridge.Pairs); set by the click cases.
        internal static List<Harness.MountedCombatController> Controllers = new List<Harness.MountedCombatController>();
        private static IEnumerable<Harness.MountedCombatController> ClickControllers() => Controllers;
        /* CLICK_ASSIGNMENT_HOOK */
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
        public void CancelMountMovement()
        {
            if (Mount == null) return;
            foreach (var command in Mount.Commands.Raw) if (command != null && !command.IsFinished && command.IsMoveUnit) { MoveInterrupts++; command.Interrupt(); }
        }
    }
    internal sealed class HarnessRelationship
    {
        public RelationshipState State = RelationshipState.Mounted; public UnitEntityData Rider, Mount; public long MountedPairGeneration = 1;
        public readonly HarnessRuntime Runtime = new HarnessRuntime();
        public bool IsExactCapturedView(UnitEntityData unit) => unit != null;
    }
    internal sealed class MountedDoorOccupancySettlement { public void Cancel(string reason) { } }
    // Native capability reads (production NativeActorCapability.Read builds MountedActorCapabilityFacts from the unit's state).
    internal static class NativeActorCapability
    {
        public static MountedActorCapabilityFacts Read(UnitEntityData unit) =>
            unit == null ? new MountedActorCapabilityFacts() : new MountedActorCapabilityFacts { Known = true, CantAct = !unit.Descriptor.State.CanAct, CanNotAttack = unit.Descriptor.State.CanNotAttack };
        // A native read inside the terminal description that fails for these units (the fallback outcome path).
        public static Func<UnitEntityData, bool> DescribeFaults;
        public static string Describe(UnitEntityData unit, object command)
        {
            if (unit != null && DescribeFaults != null && DescribeFaults(unit)) throw new InvalidOperationException("simulated native capability read failure");
            return unit == null ? "<none>" : "canAct=" + unit.Descriptor.State.CanAct;
        }
    }
    // Native SelectionManager.Instance.SelectedUnits.
    internal sealed class SelectionManager { public static SelectionManager Instance = new SelectionManager(); public List<UnitEntityData> SelectedUnits = new List<UnitEntityData>(); }
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
        public void ObserveExternalInterrupt(MountedPairAttackCommand command, string reason) { ExternalInterrupts++; }
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
            public bool CanAddressActor(UnitEntityData actor, TurnBased.Controllers.TurnController turn) => actor != Unaddressable && (turn == null || turn.Unit == null || true);
            public void RememberPairedMovementInput() { }
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
        private bool HasActiveGroundMovement => false;
        private bool riderTurnGroundMoveAdmissionPending;
        private bool HasMountOperationBesidesAttack(UnitEntityData mount) => false;
        private void ObservePlayerMoveOutcome() { }
        private void CancelPendingDoorSettlement(string reason) { }
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
        internal MountedCombatActionKind ArmedAction { get; private set; }
        internal MountedCombatActionKind RiderPrimaryAction = MountedCombatActionKind.RiderMelee;
        public bool HasActiveCommand => activeCommand != null && !activeCommand.IsFinished;
        private MountedCombatActionKind ResolveRiderPrimaryAction() => RiderPrimaryAction;
        private void ClearObservedStockRequest() { }
        private void ClearProjectedNativeMountTurnGroundMove() { }
        private void ReleaseCarriedApproach(string reason) { }
        private void CancelRiderTurnGroundMovement() { }
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
        internal bool MovementForbidden;
        private readonly MountedAutomaticAttackGate automaticGate = new MountedAutomaticAttackGate();
        // The player-facing warning (production: EventBus IWarningNotificationUIHandler.HandleWarning(LastFeedback)), captured here.
        private void ShowStockFeedback() => Warnings.Add(LastFeedback);
        private void TryLog(Func<string> line) { }
        private bool AutomaticMovementForbidden(UnitEntityData rider) => MovementForbidden;
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
        private bool TryDrainChargeOwnership(string reason) => true;
        private void TryLogException(string phase, Exception exception) { }

        // ---- harness entry points: the same calls the production drive makes ----
        internal void BeginOrder(UnitEntityData target, bool automatic)
        {
            // PR-1 round 7 (R4-F5): bound to the fixture's current turn in turn-based combat, as the production route binds it.
            stockIntent.Begin(target, TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ? Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn : null,
                false, true, relationship.Rider, relationship.Mount, null, null, 0, automatic);
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
            activeCommand = wrapper; stockIntentCommand = wrapper;
            OwnManualTarget(actor, target, stockIntent.Automatic ? -1 : stockIntent.Generation);
        }
        // TryRetireStockIntent's turn-based branch and real-time tail (production: MountedCombatController.TryRetireStockIntent).
        internal bool RetireTurnBased() => RetainedOrderSuperseded() && RetireSupersededPlayerOrder();
        internal void RetireRealTime(string reason) { var command = stockIntentCommand; ClearStockAttackIntent(reason, true); RetireOrderWrapper(command, reason); }
        internal void Own(UnitEntityData actor, UnitEntityData target, long generation) => OwnManualTarget(actor, target, generation);
        internal void Clear(string reason) => ClearStockAttackIntent(reason, true);
        internal bool HorseFollowThroughAdmissible(UnitEntityData target) => FollowThroughStrikeAdmissibleNow(target);

        /* CONTROLLER_RETIREMENT */
        /* CONTROLLER_RETAINED_SUPERSEDED */
        /* CONTROLLER_ADMIT_EXPLICIT */
        /* CONTROLLER_ADMIT_AUTOMATIC */
        // The production fields beside the decision (the proposing native action and its generation), typed on the facade's AI action.
        private Kingmaker.UnitLogic.Commands.AiActionFacade automaticOrderAction;
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
        }

        private static World NewWorld(Vector3 horse, Vector3 target, Func<Vector3, Vector3, bool> blocker, bool turnBased)
        {
            Kingmaker.Game.Instance = new Kingmaker.Game();
            TurnBased.Controllers.CombatController.TurnBased = turnBased;
            LineOfSightGeometry.Instance = new LineOfSightGeometry { Blocked = blocker ?? ((from, to) => false) };
            UnitAttack.NativeAttackStarts = 0;
            UnitAttack.NativeAttackRules = 0;
            UnitCommand.NativeDeadlineInterrupts = 0;
            UnitCommands.PromotionPrefix = null; UnitCommands.RunPostfix = null; UnitCommands.QueuePostfix = null;
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
            }
            else goal = move.Point;
            var length = Kingmaker.Utility.GeometryUtils.MechanicsDistance(goal, mover.Position);
            var step = Math.Min(0.25f, length);
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
                    Check(released && controller.AutomaticAdmissionRetiredCount == 0 && controller.LastAutomaticRetirement != "automatic target outside native admission" &&
                        cause != MountedAttackEndCause.Cancelled,
                        "PR-1 R10-F1 control: an already released strike keeps native completion; the admission decision does not retire it");
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
                controller.MovementForbidden = true;   // production AutomaticMovementForbidden: the player's Hold on the mount
                world.Rider.AiMovementForbidden = false;
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
                world.Rider.HasStandard = true; world.Horse.HasStandard = true;
                DriveToStrike(world, controller, 12);
                Console.WriteLine("  R12 mount-only Hold (" + (bLeaves ? "B leaves stationary reach" : "control: B in reach") + "): selected=" + selected?.UniqueId + " accepted=" + accepted +
                    " intent=" + after?.UniqueId + " retirement=" + retirement + " bWork=" + bWork + " moves=" + (UnitCommands.MoveSubmissions - moves) +
                    " displaced=" + !world.Horse.Position.Equals(horseBefore) + " struck=" + Struck(world));
                if (bLeaves)
                    Check(selected == b && accepted && after == world.Target && retirement == "hold out of reach" && !bWork && UnitCommands.MoveSubmissions == moves &&
                        world.Horse.Position.Equals(horseBefore) && Struck(world) == world.Target.UniqueId,
                        "PR-1 R11-F2 a mount-only Hold: retained B outside stationary reach cannot veto in-reach A; A's proposal is accepted before the drive and A is struck without travel; B gets no movement or spending");
                else
                    Check(selected == b && !accepted && after == b && Struck(world) == b.UniqueId && UnitCommands.MoveSubmissions == moves,
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
            try { Round7DeliveredOutcome(); } catch (Exception exception) { Check(false, "PR-1 round 7 delivered outcome threw " + exception); }
            try { Round7TerminalToDispatch(); } catch (Exception exception) { Check(false, "PR-1 round 7 terminal to dispatch threw " + exception); }
            try { Round7RestrictedProposal(); } catch (Exception exception) { Check(false, "PR-1 round 7 restricted proposal threw " + exception); }
            try { Round7TieBreak(); } catch (Exception exception) { Check(false, "PR-1 round 7 tie-break threw " + exception); }
            try { Round7InvalidationParity(); } catch (Exception exception) { Check(false, "PR-1 round 7 invalidation parity threw " + exception); }
            Console.WriteLine("TOTAL PASS=" + passes + " FAIL=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
