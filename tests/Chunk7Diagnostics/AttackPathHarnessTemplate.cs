using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using UnityEngine;

// Production-path harness for the mounted pair attack's approach and start (reviews R3-F1, R4-F2, R4-F1). The marked members
// below are replaced by Test-MountedAlpha2AttackPathHarness.ps1 with the ACTUAL source text of MountedPairAttackCommand,
// MountedPairSingleAttack, MountedPatchController.MountedAttackApproachPrefix, PairedAttackClickProbe.Evaluate and
// MountedCombatController.RevalidateAdmittedClickRoute, compiled with every real Domain source. This file supplies only a
// facade of the native surface those members touch: positions (mechanics distance on X/Z), corpulence, weapon range, a line
// of sight blocker that is consulted exactly when native consults it (NeedLoS), unit command slots and queue with native
// promotion, a native command tick (approach while unstarted, start only when ready and close, tick when started), and a
// native-route stand-in. Offline evidence only: nativeProof=false.
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
        public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;
    }
}

namespace Kingmaker
{
    public sealed class Game
    {
        public static Game Instance = new Game();
        public TimeControllerFacade TimeController = new TimeControllerFacade();
        public TurnBasedCombatControllerFacade TurnBasedCombatController = new TurnBasedCombatControllerFacade();
    }

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
        public bool IsConscious = true, IsDead, IsFinallyDead, CanMove = true;
        public bool IsUnconscious => !IsConscious;
    }

    public sealed class UnitDescriptor { public UnitState State = new UnitState(); }

    public sealed class UnitEntityData
    {
        public string UniqueId;
        public Vector3 Position;
        public float WeaponRange = 1.5f;
        public Vector3 EyePosition => Position + new Vector3(0f, 1.6f, 0f);
        public Kingmaker.View.UnitEntityView View = new Kingmaker.View.UnitEntityView();
        public Kingmaker.UnitLogic.Commands.UnitCommands Commands;
        public UnitDescriptor Descriptor = new UnitDescriptor();
        public bool IsInState = true;
        public float CombatSpeedMps = 9f;
        public string CharacterName => UniqueId;
        public UnitEntityData(string id) { UniqueId = id; Commands = new Kingmaker.UnitLogic.Commands.UnitCommands(this); }
        public float DistanceTo(UnitEntityData other) => Kingmaker.Utility.GeometryUtils.MechanicsDistance(Position, other.Position);
        public bool IsEnemy(UnitEntityData other) => other != null && other != this && other.UniqueId.StartsWith("hostile", StringComparison.Ordinal);
        public bool CanAttack(UnitEntityData other) => IsEnemy(other);
        public void ForceLookAt(Vector3 point) { }
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
        public virtual int GetTargetLOSObjectId() => 7;
        public bool IsUnitEnoughClose => IsUnitCloseEnough(ApproachPoint, Executor.Position, Executor.EyePosition, ApproachRadius, NeedLoS, GetTargetLOSObjectId());
        public static bool IsUnitCloseEnough(Vector3 approachPoint, Vector3 position, Vector3 eyePosition, float approachRadius, bool nedLOS, int ignoreBlockerId) =>
            Kingmaker.Utility.GeometryUtils.SqrMechanicsDistance(approachPoint, position) <= approachRadius * approachRadius &&
            (!nedLOS || !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(eyePosition, approachPoint, ignoreBlockerId));
        // Native UnitCommand.Start (after native ShouldStartCommand passed).
        public void Start() { IsStarted = true; OnStart(); }
        public void Tick() { TimeSinceStart += 0.1f; OnTick(); }
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

    public sealed class AttackHandInfo { public float WeaponRange; }

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
        public AttackHandInfo PlannedAttack => AllAttacks.Count == 0 ? null : AllAttacks[0];
        public int GetAttackIndex() => 0;
        // Native UnitAttack.Init: the attack plan, then the native approach radius.
        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            AllAttacks = new List<AttackHandInfo> { new AttackHandInfo { WeaponRange = executor.WeaponRange } };
            ApproachRadius = executor.View.Corpulence + Target.View.Corpulence + executor.WeaponRange;
        }
        // Native UnitAttack.OnStart: the attack (release) begins; counted as the single native attack start.
        protected override void OnStart() { NativeAttackStarts++; }
    }

    // A stationary prerequisite (a self-targeted Standard action already in reach): it never approaches.
    public sealed class HarnessStationaryCommand : UnitCommand
    {
        public override CommandType Type => CommandType.Standard;
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
        // Native Run (slot acceptance): the slot's previous occupant is interrupted and replaced.
        public void Run(UnitCommand command)
        {
            if (command.Executor == null) command.Init(owner);
            var existing = raw[(int)command.Type];
            if (existing != null && existing != command && !existing.IsFinished) existing.Interrupt();
            raw[(int)command.Type] = command;
            RunCount++;
        }
        // Native AddToQueueInternal (queue acceptance).
        public void Enqueue(UnitCommand command)
        {
            if (command.Executor == null) command.Init(owner);
            Queue.AddLast(command);
        }
        // Native RemoveFinishedAndUpdateQueue: finished slots are cleared; the first queued command is promoted when its slot is free.
        public void RemoveFinishedAndUpdateQueue()
        {
            for (var i = 0; i < raw.Length; i++) if (raw[i] != null && raw[i].IsFinished) raw[i] = null;
            var first = Queue.First?.Value;
            if (first != null && raw[(int)first.Type] == null) { Queue.RemoveFirst(); Run(first); }
        }
    }
}

namespace KingmakerMountedCombat.Logging
{
    public interface IModLogger { void Info(string line); void Exception(string phase, Exception exception); }
}

namespace KingmakerMountedCombat.Integration
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands;
    using Kingmaker.UnitLogic.Commands.Base;
    using Kingmaker.Utility;
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Logging;
    using TurnBased.Controllers;
    using Kingmaker;

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
        public static HarnessCarrierHost CombatFor(UnitEntityData unit) => null;
    }

    internal static class PatchMethods
    {
        internal static bool ChargeExecutionPrefix(UnitCommand __instance) => true;
        private static bool IsRelationshipControlShell(UnitCommand command) => false;
        /* APPROACH_PREFIX */
    }

    /* PROBE_CLASS */

    internal sealed class HarnessRelationship { public UnitEntityData Rider, Mount; }

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
        internal List<Vector3> Revalidate(UnitEntityData target) => RevalidateAdmittedClickRoute(target);
        /* REVALIDATE */
        internal string LastRouteRevalidation { get; private set; }
    }

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
            IModLogger logger, bool allowApproach = true)
            : base(target, rider, mount, action != MountedCombatActionKind.MountPrimaryNatural, true)
        {
            this.rider = rider; this.mount = mount; attackTarget = target; this.action = action; this.logger = logger;
            this.allowApproach = allowApproach;
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
        private void ReportTerminalOnce() { TerminalReports++; }
        private void TryContinueRoutineOnNextTarget() { }
        private void CreateAndValidateChildAttack() { ChildCreations++; }
        private void TryLogInfo(string line) { try { logger.Info(line); } catch (Exception) { } }
        private void TryLogException(string phase, Exception exception) { try { logger.Exception(phase, exception); } catch (Exception) { } }
        private static float HorizontalDistance(Vector3 first, Vector3 second) => GeometryUtils.MechanicsDistance(first, second);

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
        /* START_CHILD */
        /* OBSERVE_ADMISSION */
    }
}

namespace Harness
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Commands;
    using Kingmaker.UnitLogic.Commands.Base;
    using Kingmaker.Visual.FogOfWar;
    using KingmakerMountedCombat.Integration;

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
            public int NativeApproachSteps;
        }

        private static World NewWorld(Vector3 horse, Vector3 target, Func<Vector3, Vector3, bool> blocker, bool turnBased)
        {
            Kingmaker.Game.Instance = new Kingmaker.Game();
            TurnBased.Controllers.CombatController.TurnBased = turnBased;
            LineOfSightGeometry.Instance = new LineOfSightGeometry { Blocked = blocker ?? ((from, to) => false) };
            UnitAttack.NativeAttackStarts = 0;
            var world = new World();
            world.Rider = new UnitEntityData("rider") { Position = horse, WeaponRange = 1.5f };
            world.Rider.View.Corpulence = 0.4f;
            world.Horse = new UnitEntityData("horse") { Position = horse, WeaponRange = 1.0f };
            world.Horse.View.Corpulence = 0.9f;
            world.Horse.View.AgentASP = new Kingmaker.View.UnitMovementAgent { Unit = world.Horse, Route = (from, to) => new List<Vector3> { from, to } };
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
                    if (PatchMethods.MountedAttackApproachPrefix(command)) NativeApproach(world, command);
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

        // A move in the mount's Move slot: follows its ForcedPath when it has one, otherwise a straight native point move;
        // finishes successfully at its end / within its radius. The rider is carried with the horse.
        private static void TickMove(World world, UnitMoveTo move)
        {
            var mover = move.Executor;
            var points = move.ForcedPath?.vectorPath;
            Vector3 goal;
            if (points != null && points.Count > 0)
            {
                move.FollowedForcedPath = true;
                while (move.PathIndex < points.Count && Kingmaker.Utility.GeometryUtils.MechanicsDistance(points[move.PathIndex], mover.Position) <= 0.001f) move.PathIndex++;
                if (move.PathIndex >= points.Count) { move.FinishSuccess(); return; }
                goal = points[move.PathIndex];
            }
            else
            {
                if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(move.Point, mover.Position) <= move.ApproachRadius) { move.FinishSuccess(); return; }
                goal = move.Point;
            }
            var length = Kingmaker.Utility.GeometryUtils.MechanicsDistance(goal, mover.Position);
            var step = Math.Min(0.25f, length);
            var offset = goal - mover.Position; offset.y = 0f;
            var before = mover.Position;
            mover.Position = mover.Position + offset * (step / length);
            move.Travelled.Add(mover.Position);
            if (mover == world.Horse) world.Rider.Position = world.Rider.Position + (mover.Position - before);
        }

        private static MountedPairAttackCommand Order(World world, MountedCombatActionKind action, bool automatic)
        {
            var command = new MountedPairAttackCommand(world.Rider, world.Horse, world.Target, action, world.Logger);
            if (automatic) { command.MarkAutomaticOrigin(); command.AutomaticTargetPerceived = () => world.Perceived; }
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
                order.RouteRevalidator = () => controller.Revalidate(world.Target);
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
            expired.RouteRevalidator = () => shortController.Revalidate(short_.Target);
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

        private static int Main()
        {
            try { R3F1(); } catch (Exception exception) { Check(false, "R3-F1 threw " + exception); }
            try { R4F2(); } catch (Exception exception) { Check(false, "R4-F2 threw " + exception); }
            try { R4F1(); } catch (Exception exception) { Check(false, "R4-F1 threw " + exception); }
            Console.WriteLine("TOTAL PASS=" + passes + " FAIL=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
