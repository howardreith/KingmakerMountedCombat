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

    public sealed class UnitCombatState { public UnitEntityData ManualTarget; }

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
        public UnitCombatState CombatState = new UnitCombatState();
        public bool HoldState;
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
        public object AiAction;
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
            if (ActEventDue && !IsActed)
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
        public override UnitEntityData TargetUnit => Target;
        public override bool MarkManualTarget => true;
        // Native UnitAttack.OnAction: the attack rule is triggered (delivery).
        public static int NativeAttackRules;
        protected override ResultType OnAction() { NativeAttackRules++; LastAttackRule = new object(); return ResultType.None; }
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
            if (command.MarkManualTarget && command.AiAction == null && command.TargetUnit != null) owner.CombatState.ManualTarget = command.TargetUnit;
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
        private void ReportTerminalOnce() { TerminalReports++; }
        private void TryContinueRoutineOnNextTarget() { }
        private void CreateAndValidateChildAttack() { ChildCreations++; }
        private void TryLogInfo(string line) { try { logger.Info(line); } catch (Exception) { } }
        private void TryLogException(string phase, Exception exception) { try { logger.Exception(phase, exception); } catch (Exception) { } }
        private static float HorizontalDistance(Vector3 first, Vector3 second) => GeometryUtils.MechanicsDistance(first, second);

        internal int DelegatedMoveStarts => delegatedMoveStartCount;
        internal bool ChargeMode => chargeMode;
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
    internal sealed class HarnessRelationship { public RelationshipState State = RelationshipState.Mounted; public UnitEntityData Rider, Mount; }
    internal sealed class HarnessScheduler { public int ExternalInterrupts; public void ObserveExternalInterrupt(MountedPairAttackCommand command, string reason) { ExternalInterrupts++; } }

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

        internal MountedCombatController(UnitEntityData rider, UnitEntityData mount) { relationship.Rider = rider; relationship.Mount = mount; }
        private bool IsPairPlayerOrder(UnitCommand command) => command != null && command.CreatedByPlayer;
        private static string DescribeCommand(UnitCommand command) => command.GetType().Name;
        private bool TryDrainChargeOwnership(string reason) => true;
        private void TryLogException(string phase, Exception exception) { }

        // ---- harness entry points: the same calls the production drive makes ----
        internal void BeginOrder(UnitEntityData target, bool automatic)
        {
            stockIntent.Begin(target, null, false, true, relationship.Rider, relationship.Mount, null, null, 0, automatic);
            retainedOrder.Begin(stockIntent.Generation, automatic, true);
        }
        // TryExecuteAction's acceptance of the order's wrapper (dispatch): the wrapper is the order's own, carries the supersession
        // check, and its actor's selection is the order's write.
        internal void AcceptWrapper(MountedPairAttackCommand wrapper, UnitEntityData actor, UnitEntityData target)
        {
            retainedOrder.RecordOwnIssued(stockIntent.Generation, wrapper);
            wrapper.RetainedOrderSupersededCheck = RetainedOrderSuperseded;
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

        // A move in the mount's Move slot, ticked as native ticks a point command. Native proximity first (UnitCommand.
        // IsUnitEnoughClose: within ApproachRadius of ApproachPoint, NeedLoS false): a close move starts and completes WITHOUT
        // moving. Otherwise native TickApproaching follows its ForcedPath when it has one (a straight point move otherwise),
        // 0.25 m per tick, the mount turning to its travel direction; the rider is carried at its mount-local offset, which
        // turns with the mount (Unity yaw: Quaternion.Euler(0, turn, 0) applied to the offset).
        private static void TickMove(World world, UnitMoveTo move)
        {
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
            foreach (var kase in new[] { "just outside", "left reach before start", "in reach" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(kase == "in reach" ? 1.5f : 2.41f, 0f, 0f), null, true);
                var order = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
                order.ForbidApproach();
                world.Horse.Commands.Run(order);
                if (kase == "left reach before start") world.Target.Position = new Vector3(3.5f, 0f, 0f);
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

        // Case 1 (R1-F3): terminal handling followed by the next dispatch, for an unstarted turn-based force-finish and an interrupted
        // first windup; controls: a delivered rider strike, and a spent or ended horse.
        private static void Round4RiderRefusal()
        {
            foreach (var kase in new[] { "unstarted force-finish", "interrupted first windup", "delivered control" })
            foreach (var horseState in new[] { "eligible", "spent", "ended" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), null, true);
                var controller = Controller(world);
                controller.BeginOrder(world.Target, false);
                var rider = Order(world, MountedCombatActionKind.RiderMelee, false);
                world.Rider.Commands.Run(rider);
                controller.AcceptWrapper(rider, world.Rider, world.Target);
                if (kase == "unstarted force-finish") rider.ForceFinishForTurnBased(UnitCommand.ResultType.Success);
                else
                {
                    NativeTick(world, world.Rider);
                    if (kase == "delivered control") { rider.ActEventDue = true; NativeTick(world, world.Rider); }
                    rider.Interrupt();
                }
                var generation = controller.Generation;
                var disposition = MountedRetainedOrderLedger.DecideTerminal(new MountedOrderTerminalFacts
                {
                    OwnedByOrder = true, OrderCurrent = controller.Ledger.IsCurrent(generation), TurnBased = true, MeleeOrder = controller.Ledger.Melee,
                    RiderMeleeCommand = true, CapabilityIsCause = kase != "delivered control", Delivered = rider.StrikeReleased,
                    AttackRuleObserved = rider.LastAttackRule != null, CompletedAttacks = 0
                });
                if (disposition == MountedOrderTerminalDisposition.RiderEndedForHorse)
                {
                    controller.Ledger.MarkRiderEnded(generation);
                    controller.Ledger.HoldRiderRefusal(generation, "rider cannot act");
                }
                // The next drive: the dispatcher's own inputs (riderEligible excludes an ended rider attempt; the horse's eligibility is
                // its Standard and addressability; mountInReach from the production planned-strike probe).
                var riderEligible = !controller.Ledger.RiderEnded(generation);
                var mountEligible = horseState == "eligible";
                var addressable = horseState != "ended";
                var decision = disposition == MountedOrderTerminalDisposition.RiderEndedForHorse
                    ? (MountedStockAttackDecision?)UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, riderEligible, mountEligible, false, true, true,
                        UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, false, false, true, false, addressable, true,
                            controller.Ledger.Superseded(generation), controller.HorseFollowThroughAdmissible(world.Target)))
                    : null;
                var deliveries = UnitAttack.NativeAttackRules;
                if (kase == "delivered control")
                    Check(disposition == MountedOrderTerminalDisposition.Retire && deliveries == 1 && horseState != null,
                        "PR-1 R1-F3 delivered control (" + horseState + " horse): a delivered rider strike retires the order; no horse recheck");
                else if (horseState == "eligible")
                    Check(disposition == MountedOrderTerminalDisposition.RiderEndedForHorse && rider.IsActed == (kase == "unstarted force-finish") &&
                        deliveries == 0 && !riderEligible && decision == MountedStockAttackDecision.DispatchMount,
                        "PR-1 R1-F3 " + kase + ": zero rider deliveries (IsActed " + rider.IsActed + " is not delivery); the next dispatch is one horse attempt, not the rider");
                else
                {
                    var refusal = controller.Ledger.TakeRefusalOnRetirement(generation);
                    Check(disposition == MountedOrderTerminalDisposition.RiderEndedForHorse && deliveries == 0 && decision != MountedStockAttackDecision.DispatchMount &&
                        decision != MountedStockAttackDecision.DispatchRider && refusal == "rider cannot act" && controller.Ledger.TakeRefusalOnRetirement(generation) == null,
                        "PR-1 R1-F3/R2-F4 " + kase + ", " + horseState + " horse: no horse or rider attempt (" + decision + "); the rider's refusal is the answer once");
                }
            }
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
                    controller.BeginClickObservation();
                    controller.Own(world.Rider, world.Target, controller.Generation);
                    world.Rider.CombatState.ManualTarget = world.Target;   // ClickUnitHandler.OnClick's own post-Run assignment
                    controller.EndClickObservation(world.Target);
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
                        controller.BeginClickObservation();
                        world.Rider.Commands.Run(new HarnessPlayerCommand(world.Target, UnitCommand.CommandType.Move) { WritesNoTarget = true });
                        world.Rider.CombatState.ManualTarget = world.Target;
                        controller.EndClickObservation(world.Target);
                        expectOwned = false;
                    }
                }
                controller.Clear("order retired");
                var selection = world.Rider.CombatState.ManualTarget;
                Check(expectOwned ? selection == null : selection == world.Target,
                    "PR-1 R1-F11 " + kase + ": after retirement the actor's selection is " + (selection == null ? "<none> (the order's own write was released)" : selection.UniqueId + " (the newer assignment survives)"));
            }
        }

        // Case 5 (R3-F4): horse reach through the production dispatch seam (the spliced planned-strike probe feeding MountJoinsIntent and
        // DecideNext), then the dispatched follow-through's own start without approach authority.
        private static void Round4HorseReach()
        {
            foreach (var kase in new[] { "initially outside", "inside then outside", "continuously inside" })
            {
                var world = NewWorld(new Vector3(0f, 0f, 0f), new Vector3(kase == "initially outside" ? 2.41f : 2.0f, 0f, 0f), null, true);
                var controller = Controller(world);
                controller.BeginOrder(world.Target, false);
                controller.Ledger.MarkRiderEnded(controller.Generation);
                var inReach = controller.HorseFollowThroughAdmissible(world.Target);
                var decision = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, false, true, false, true, true,
                    UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, false, false, true, false, true, true, false, inReach));
                var dispatched = decision == MountedStockAttackDecision.DispatchMount;
                MountedPairAttackCommand horse = null;
                if (dispatched)
                {
                    horse = Order(world, MountedCombatActionKind.MountPrimaryNatural, false);
                    horse.ForbidApproach();   // the turn-based follow-through has no approach authority (allowApproach false at dispatch)
                    world.Horse.Commands.Run(horse);
                    if (kase == "inside then outside") world.Target.Position = new Vector3(3.5f, 0f, 0f);
                    for (var tick = 0; tick < 4 && !horse.IsFinished && UnitAttack.NativeAttackStarts == 0; tick++) NativeTick(world, world.Horse);
                }
                var carriers = horse == null ? 0 : horse.DelegatedMoveStarts;
                var starts = UnitAttack.NativeAttackStarts;
                Console.WriteLine("ROUND4 horse reach " + kase + ": probes=" + controller.PlannedStrikeProbeCount + " dispatch=" + (dispatched ? 1 : 0) + " carriers=" + carriers + " starts=" + starts);
                if (kase == "initially outside") Check(!dispatched && carriers == 0 && starts == 0, "PR-1 R3-F4 initially outside: no dispatch, carrier or start");
                if (kase == "inside then outside") Check(dispatched && carriers == 0 && starts == 0 && horse.IsFinished && world.Horse.View.AgentASP.FindPathCount == 0,
                    "PR-1 R3-F4 inside then outside after the observed dispatch: the follow-through settles with no movement and no attack");
                if (kase == "continuously inside") Check(dispatched && carriers == 0 && starts == 1, "PR-1 R3-F4 continuously inside: one dispatch, no carrier, one start");
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

        // Case 8 (R2-F4): feedback through the generation-bound refusal.
        private static void Round4Feedback()
        {
            var ledger = new MountedRetainedOrderLedger();
            ledger.Begin(1, false, true);
            ledger.MarkRiderEnded(1);
            ledger.HoldRiderRefusal(1, "rider dazed");
            var spent = ledger.TakeRefusalOnRetirement(1);
            Check(spent == "rider dazed" && ledger.TakeRefusalOnRetirement(1) == null, "PR-1 R2-F4 spent horse (entirely unexecuted): the refusal is presented once");
            ledger.Begin(2, false, true);
            ledger.HoldRiderRefusal(2, "rider dazed");
            // An admitted horse that loses reach before start ends undelivered: its terminal retires the order (DecideTerminal Retire).
            var lostReach = MountedRetainedOrderLedger.DecideTerminal(new MountedOrderTerminalFacts { OwnedByOrder = true, OrderCurrent = true, TurnBased = true, MeleeOrder = true });
            Check(ledger.HasPendingRefusal(2) && lostReach == MountedOrderTerminalDisposition.Retire && ledger.TakeRefusalOnRetirement(2) == "rider dazed",
                "PR-1 R2-F4 admitted horse that loses reach before start: admission did not settle the refusal; the final retirement presents it once");
            ledger.Begin(3, false, true);
            ledger.HoldRiderRefusal(3, "rider dazed");
            ledger.SettleRefusalDelivered(3);
            Check(ledger.TakeRefusalOnRetirement(3) == null, "PR-1 R2-F4 successful follow-through: the horse's delivery settles the refusal; no warning");
            ledger.Begin(4, false, true);
            ledger.HoldRiderRefusal(4, "rider dazed");
            ledger.Begin(5, false, true);
            Check(ledger.TakeRefusalOnRetirement(5) == null && ledger.TakeRefusalOnRetirement(4) == null, "PR-1 R2-F4 replacement: the later generation receives no stale message");
            ledger.HoldRiderRefusal(5, "rider dazed");
            ledger.Clear();
            Check(!ledger.HasPendingRefusal(5), "PR-1 R2-F4 cancellation clears the pending refusal");
        }

        private static int Main()
        {
            try { R3F1(); } catch (Exception exception) { Check(false, "R3-F1 threw " + exception); }
            try { R4F2(); } catch (Exception exception) { Check(false, "R4-F2 threw " + exception); }
            try { PR1R1F4(); } catch (Exception exception) { Check(false, "PR-1 R1-F4 threw " + exception); }
            try { PR1R1F6(); } catch (Exception exception) { Check(false, "PR-1 R1-F6 threw " + exception); }
            try { PR1R1F7(); } catch (Exception exception) { Check(false, "PR-1 R1-F7 threw " + exception); }
            try { PR1Round2(); } catch (Exception exception) { Check(false, "PR-1 round 2 threw " + exception); }
            try { Round4RiderRefusal(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 1 threw " + exception); }
            try { Round4DeliveryAndRetirement(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 2 threw " + exception); }
            try { Round4QueueLifecycle(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 3 threw " + exception); }
            try { Round4ManualTargets(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 4 threw " + exception); }
            try { Round4HorseReach(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 5 threw " + exception); }
            try { Round4Approach(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 6 threw " + exception); }
            try { Round4WaitAndClocks(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 7 threw " + exception); }
            try { Round4Feedback(); } catch (Exception exception) { Check(false, "PR-1 round 4 case 8 threw " + exception); }
            Console.WriteLine("TOTAL PASS=" + passes + " FAIL=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
