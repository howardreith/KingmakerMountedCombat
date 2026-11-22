using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        internal int DeferredSaveCount { get; private set; }
        internal int FailedSaveCount { get; private set; }

        private SaveWaitFault diagnosticWait;

        // The fixture owns one exact already-authorized native save request.
        // This does not authorize storage or synthesize gameplay/load state.
        internal IDisposable ArmOwnedSaveWait(SaveInfo save)
        {
            NativePersistenceIsolation.RequireOwnedWrite(save);
            if (diagnosticWait != null) throw new InvalidOperationException("An owned wait fault is already armed.");
            diagnosticWait = new SaveWaitFault(this, save);
            return diagnosticWait;
        }

        private sealed class SaveWaitFault : IDisposable
        {
            private readonly MountedPersistenceService owner;
            internal readonly SaveInfo Save;
            internal bool Active = true;
            private bool claimed;
            internal SaveWaitFault(MountedPersistenceService owner, SaveInfo save) { this.owner = owner; Save = save; }
            internal bool TryClaim(SaveInfo request)
            {
                if (!Active || claimed || !ReferenceEquals(Save, request)) return false;
                claimed = true;
                return true;
            }
            public void Dispose()
            {
                Active = false;
                if (ReferenceEquals(owner.diagnosticWait, this)) owner.diagnosticWait = null;
            }
        }

        // One production readiness result. SaveEffectsReady, the header barrier
        // (PinWorldSaveScope) and the deferred wait's diagnostics all read it; the
        // diagnostics never re-derive a term. Subject is read in the same frame
        // only and never stored.
        internal struct SaveReadiness
        {
            internal SaveWaitBlockerKind Kind;
            internal object Subject;
            internal UnitEntityData Unit;
            internal bool Queued;
            internal string Reason;
            internal bool Ready => Kind == SaveWaitBlockerKind.None;
        }

        internal bool SaveEffectsReady() => EvaluateSaveEffects().Ready;

        // Movement/approach intent is transient; its position and accrued
        // native debt are captured at the real header barrier. A running
        // action or undelivered projectile must first resolve natively.
        //
        // The two relationship-transition terms are exact queries, not an
        // argument from game-thread atomicity: an unsettled Mount/Dismount shell
        // defers the save even while it is only queued, and the transition
        // ledger's own admit-to-settle window defers it as well. Together they
        // make the documented claim true by construction rather than by
        // reasoning about when a frame boundary can be observed.
        //
        // The terms and their order are those of the former single expression;
        // the first refusing term is returned with the object that refused.
        internal SaveReadiness EvaluateSaveEffects()
        {
            var game = Game.Instance;
            if (game?.Player == null || game.State == null)
                throw new InvalidOperationException("Native save has no active world.");
            var unready = relationship.FirstUnreadySaveOwner;
            if (unready != null)
                return new SaveReadiness { Kind = SaveWaitBlockerKind.PairOwnership, Subject = unready, Reason = unready.Boundary?.PairSaveRefusal };
            if (unifiedTurn.HasUnsettledPreparation)
                return new SaveReadiness { Kind = SaveWaitBlockerKind.NativePreparation };
            var transitionSettled = !controls.HasUnsettledRelationshipTransition;
            if (!transitionSettled)
                return new SaveReadiness { Kind = SaveWaitBlockerKind.RelationshipTransition };
            var shellsSettled = !WorldRelationships.Any(controls.HasUnsettledShellFor);
            if (!shellsSettled)
                return new SaveReadiness
                {
                    Kind = SaveWaitBlockerKind.RelationshipShell, Subject = WorldRelationships.FirstOrDefault(controls.HasUnsettledShellFor)
                };
            foreach (var unit in game.State.Units)
            {
                var command = unit.Commands.Raw.Concat(unit.Commands.Queue).FirstOrDefault(NativeSaveEffectBoundary.CommandNeedsSettlement);
                if (command != null)
                    return new SaveReadiness { Kind = SaveWaitBlockerKind.Command, Subject = command, Unit = unit, Queued = unit.Commands.Queue.Contains(command) };
            }
            foreach (var unit in game.State.Units)
            {
                if (!unit.Commands.Raw.Concat(unit.Commands.Queue).Any(controls.OwnsUnsettledRelationshipShell)) continue;
                var shell = unit.Commands.Raw.Concat(unit.Commands.Queue).First(controls.OwnsUnsettledRelationshipShell);
                return new SaveReadiness { Kind = SaveWaitBlockerKind.RelationshipShellCommand, Subject = shell, Unit = unit, Queued = unit.Commands.Queue.Contains(shell) };
            }
            var projectile = NativeSaveEffectBoundary.FirstUnresolvedProjectile(out var pending);
            if (projectile != null)
                return new SaveReadiness { Kind = SaveWaitBlockerKind.Projectile, Subject = projectile, Queued = pending };
            var process = NativeSaveEffectBoundary.FirstUnresolvedAbility();
            if (process != null)
                return new SaveReadiness { Kind = SaveWaitBlockerKind.Ability, Subject = process };
            return default(SaveReadiness);
        }

        // Player-facing short name and full log detail of one readiness result.
        // Called only when the observed blocker changes, in the frame it was read.
        private string SummarizeReadiness(SaveReadiness readiness)
        {
            switch (readiness.Kind)
            {
                case SaveWaitBlockerKind.AdmissionFence: return "Mounted Charge cleanup";
                case SaveWaitBlockerKind.DiagnosticFault: return "a diagnostic wait fault";
                case SaveWaitBlockerKind.PairOwnership:
                    var owner = readiness.Subject as MountedPairOwner<UnitEntityData, GameMountedRelationshipService>;
                    return (owner?.Rider?.CharacterName ?? "a rider") + "'s mounted pair to settle (" + (readiness.Reason ?? "unnamed refusal") + ")";
                case SaveWaitBlockerKind.NativePreparation: return "native turn preparation";
                case SaveWaitBlockerKind.RelationshipTransition: return "a Mount or Dismount to finish";
                case SaveWaitBlockerKind.RelationshipShell: return "a Mount or Dismount command to settle";
                case SaveWaitBlockerKind.Command:
                case SaveWaitBlockerKind.RelationshipShellCommand:
                    return NativeSaveEffectBoundary.SummarizeCommand((UnitCommand)readiness.Subject);
                case SaveWaitBlockerKind.Projectile:
                    return NativeSaveEffectBoundary.SummarizeProjectile((Kingmaker.Controllers.Projectiles.Projectile)readiness.Subject);
                case SaveWaitBlockerKind.Ability:
                    return NativeSaveEffectBoundary.SummarizeAbility((Kingmaker.Controllers.AbilityExecutionProcess)readiness.Subject);
                default: return "current actions";
            }
        }

        private string DescribeReadiness(SaveReadiness readiness)
        {
            switch (readiness.Kind)
            {
                case SaveWaitBlockerKind.AdmissionFence:
                    return string.Join(" | ", WorldCombats.Select((c, i) => "owner" + i + ": chargeOwnership=" + c.HasChargeOwnership +
                        "; nativeBoundaryPending=" + c.ChargeNativeBoundaryPending + "; fenced=" + c.ChargeAdmissionFenced).ToArray());
                case SaveWaitBlockerKind.PairOwnership:
                    var owner = readiness.Subject as MountedPairOwner<UnitEntityData, GameMountedRelationshipService>;
                    return owner == null ? "owner=<none>" : "pair=" + owner.Id.ToString("N") + "; generation=" + owner.Generation +
                        "; state=" + owner.State + "; rider=" + NativeSaveEffectBoundary.NameOf(owner.Rider) + "; mount=" +
                        NativeSaveEffectBoundary.NameOf(owner.Mount) + "; refusal=" + (readiness.Reason ?? "<unnamed>");
                case SaveWaitBlockerKind.NativePreparation:
                    return "unifiedTurn.HasUnsettledPreparation=True";
                case SaveWaitBlockerKind.RelationshipTransition:
                    return "controls.HasUnsettledRelationshipTransition=True";
                case SaveWaitBlockerKind.RelationshipShell:
                    var shellOwner = readiness.Subject as GameMountedRelationshipService;
                    return "rider=" + NativeSaveEffectBoundary.NameOf(shellOwner?.Rider) + "; mount=" + NativeSaveEffectBoundary.NameOf(shellOwner?.Mount);
                case SaveWaitBlockerKind.Command:
                case SaveWaitBlockerKind.RelationshipShellCommand:
                    return NativeSaveEffectBoundary.DescribeCommand(readiness.Unit, (UnitCommand)readiness.Subject, readiness.Queued);
                case SaveWaitBlockerKind.Projectile:
                    return NativeSaveEffectBoundary.DescribeProjectile((Kingmaker.Controllers.Projectiles.Projectile)readiness.Subject, readiness.Queued);
                case SaveWaitBlockerKind.Ability:
                    return NativeSaveEffectBoundary.DescribeAbility((Kingmaker.Controllers.AbilityExecutionProcess)readiness.Subject);
                default: return readiness.Kind.ToString();
            }
        }

        // The refusing condition of one owner, without timing: a change here is a
        // reported transition even when the owner object stays the same (review of
        // 0b4b8fa4, F1); the timed detail is refreshed without a line.
        private string ConditionOfReadiness(SaveReadiness readiness)
        {
            switch (readiness.Kind)
            {
                case SaveWaitBlockerKind.PairOwnership: return "refusal=" + (readiness.Reason ?? "<unnamed>");
                case SaveWaitBlockerKind.AdmissionFence:
                    return string.Join(" | ", WorldCombats.Select((c, i) => "owner" + i + ": " + c.HasChargeOwnership + "/" +
                        c.ChargeNativeBoundaryPending).ToArray());
                case SaveWaitBlockerKind.RelationshipShell:
                    var shellOwner = readiness.Subject as GameMountedRelationshipService;
                    return "pairState=" + (shellOwner?.State.ToString() ?? "<none>");
                case SaveWaitBlockerKind.Command:
                case SaveWaitBlockerKind.RelationshipShellCommand:
                    return NativeSaveEffectBoundary.ConditionOfCommand((UnitCommand)readiness.Subject, readiness.Queued);
                case SaveWaitBlockerKind.Projectile:
                    return NativeSaveEffectBoundary.ConditionOfProjectile((Kingmaker.Controllers.Projectiles.Projectile)readiness.Subject, readiness.Queued);
                case SaveWaitBlockerKind.Ability:
                    return "ended=" + ((Kingmaker.Controllers.AbilityExecutionProcess)readiness.Subject).IsEnded;
                default: return readiness.Kind.ToString();
            }
        }

        // Diagnostics must never change the save's outcome: every failure here is contained.
        private void ObserveSaveWait(SaveWaitObservation observation, SaveReadiness readiness, double now)
        {
            try
            {
                var line = observation.Observe(readiness.Kind,
                    readiness.Subject == null ? 0 : RuntimeHelpers.GetHashCode(readiness.Subject),
                    () => SummarizeReadiness(readiness), () => ConditionOfReadiness(readiness), () => DescribeReadiness(readiness), now);
                if (line != null) logger.Info(line);
            }
            catch { /* An unloggable observation leaves the save decision untouched. */ }
        }

        private void RequireChargeSaveBarrier(SaveScope scope)
        {
            if (scope.Released || scope.EnumerationRetired)
                throw new InvalidOperationException("A retired save operation cannot resume native serialization.");
            // After this exact scope captured its header, queued lifecycle notifications
            // must wait for serialization to finish. They must not abort its worker or
            // start cleanup against the live graph. New captures still require no queue.
            if (!(scope.Json == null ? AcquireWorldSaveFence(scope) : ContinueWorldSaveFence(scope)))
                throw new InvalidOperationException("Save deferred: charge cleanup ownership is unresolved.");
        }

        private long saveWaitSequence;

        // Seconds on one monotonic clock per request, started when it is wrapped.
        // The deferred-save regression substitutes a deterministic clock here.
        private static Func<double> SaveWaitClock()
        {
            var timer = Stopwatch.StartNew();
            return () => timer.Elapsed.TotalSeconds;
        }

        private IEnumerator<object> DeferNativeSave(IEnumerator<object> routine, SaveWaitFault fault, SaveScope scope)
        {
            var clock = SaveWaitClock();
            var wrappedAt = clock();
            Player world = null;
            var restorePause = false;
            var request = ++saveWaitSequence;
            var label = (scope.Requested?.Type.ToString() ?? "none") + ":" + (scope.Requested?.Name ?? "none");
            // Created at activation (the first readiness read, inside Activate), so
            // every wait time is measured as the timeout measures it, not from the
            // time this request spent queued behind another (review of 0b4b8fa4, F3).
            SaveWaitObservation observation = null;
            SaveWaitOutcome? refused = null;
            string refusedReason = null;
            DeferredSaveEnumerator<object> operation = null;
            Action tick = () =>
            {
                var game = Game.Instance;
                var mode = game.CurrentMode;
                var manualScreen = LoadingProcess.Instance.IsManualLoadingScreenActive;
                // The same refusal as before (world replaced, manual loading screen,
                // or any mode but Default/Pause); only its reason is now named.
                var state = SaveWaitWorldPolicy.Decide(world != null, ReferenceEquals(game.Player, world), manualScreen,
                    mode == GameModeType.Default || mode == GameModeType.Pause);
                if (state != SaveWaitWorldState.Unchanged)
                {
                    refused = SaveWaitWorldPolicy.Outcome(state);
                    refusedReason = "mode=" + mode + "; manualLoadingScreen=" + manualScreen + "; worldBound=" + (world != null);
                    throw new InvalidOperationException(SaveWaitWorldPolicy.FailureMessage(state, mode.ToString(), observation?.WaitingFor));
                }
                if (game.IsPaused) { restorePause = true; game.IsPaused = false; }
            };
            Func<bool> ready = () =>
            {
                var now = clock();
                if (observation == null) observation = new SaveWaitObservation(request, label, wrappedAt, now);
                if (world != null && !ReferenceEquals(Game.Instance?.Player, world))
                {
                    refused = SaveWaitOutcome.WorldReplaced;
                    refusedReason = "player replaced";
                    throw new InvalidOperationException(SaveWaitWorldPolicy.FailureMessage(
                        SaveWaitWorldState.WorldReplaced, null, observation.WaitingFor));
                }
                // Same order and short-circuit as before: the admission fence, then
                // the diagnostic fault, then the effect terms. An exception here ends
                // the wait as Faulted through the operation's recorded Failure.
                var readiness = !AcquireWorldSaveFence(scope) ? new SaveReadiness { Kind = SaveWaitBlockerKind.AdmissionFence } :
                    fault?.Active == true ? new SaveReadiness { Kind = SaveWaitBlockerKind.DiagnosticFault } :
                    EvaluateSaveEffects();
                ObserveSaveWait(observation, readiness, now);
                return readiness.Ready;
            };
            operation = new DeferredSaveEnumerator<object>(routine, ready, clock,
                () =>
                {
                    world = Game.Instance.Player;
                    DeferredSaveCount++;
                    NotifySaveStatus("Saving after current actions and projectiles finish; new actions are briefly held.");
                    tick();
                }, tick, () =>
                {
                    if (restorePause && ReferenceEquals(Game.Instance?.Player, world)) Game.Instance.IsPaused = true;
                    world = null;
                }, SaveWaitWorldPolicy.MaximumWaitSeconds, () =>
                {
                    // Every disposal reaches here once, after a fault has been recorded
                    // on the operation. A wait that reached serialization was already
                    // reported as ReadyToSerialize, so this reports only the other ends.
                    ReportSaveWaitEnd(observation, operation, refused, refusedReason, clock());
                    // ready() precedes the inner scope's begin. Cancellation/timeout there
                    // still releases this exact operation's fence. A started worker retains it
                    // through ReleaseSaveScope/the existing deferred-worker drain.
                    if (!scope.Began) ReleaseUnstartedSaveFence(scope);
                }, () => SaveWaitWorldPolicy.TimeoutMessage(SaveWaitWorldPolicy.MaximumWaitSeconds, observation?.WaitingFor),
                () => ReportSaveWaitEnd(observation, operation, refused, refusedReason, clock()));
            return operation;
        }

        // Called after native serialization actually started (ReadyToSerialize) and
        // from the operation's terminal (every other end). The outcome describes the
        // wait only; the archive's outcome stays with the existing commit machinery.
        // Precedence: a recorded refusal, the bounded timeout, a recorded fault, and
        // only then a cancellation nobody faulted. Contained: a logging failure
        // cannot skip the fence release that follows it.
        private void ReportSaveWaitEnd(SaveWaitObservation observation, DeferredSaveEnumerator<object> operation,
            SaveWaitOutcome? refused, string refusedReason, double now)
        {
            try
            {
                if (observation == null || observation.Ended || operation == null) return;
                SaveWaitOutcome outcome;
                string reason = null;
                if (operation.SerializationStarted && !operation.Disposed) outcome = SaveWaitOutcome.ReadyToSerialize;
                else if (refused != null) { outcome = refused.Value; reason = refusedReason; }
                else if (operation.TimedOut) outcome = SaveWaitOutcome.TimedOut;
                else if (operation.Failure != null)
                {
                    outcome = SaveWaitOutcome.Faulted;
                    // The native screen start is a reflected call: name what actually failed.
                    var failure = operation.Failure;
                    while (failure is System.Reflection.TargetInvocationException && failure.InnerException != null) failure = failure.InnerException;
                    reason = failure.GetType().Name + ": " + failure.Message;
                }
                else outcome = SaveWaitOutcome.Cancelled;
                LastSaveWaitOutcome = outcome;
                var line = observation.End(outcome, reason, now);
                if (line != null) logger.Info(line);
            }
            catch { /* The wait's cleanup continues regardless. */ }
        }

        internal SaveWaitOutcome? LastSaveWaitOutcome { get; private set; }

        // The ordinary completion path's failure report. What is said depends on
        // what happened to THIS operation's archive, decided at the commit
        // boundary: a worker that committed and then failed in cleanup has still
        // written the save, and "the previous save is unchanged" is only said
        // when one existed and is still there.
        internal void ReportFailedSave(Exception exception, object operation)
        {
            var report = DescribeFailedOperation(operation, exception);
            if (report.CountsAsFailed) FailedSaveCount++; else CommittedThenFailedCount++;
            NotifySaveStatus(report.Message);
        }

        private void NotifySaveStatus(string message)
        {
            Report(message);
            try { Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IWarningNotificationUIHandler>(
                handler => handler.HandleWarning(message, true)); }
            catch (Exception display) { logger.Exception("Save failure notification could not be displayed", display); }
        }

        internal void ReportAbandonedSave(Exception exception) =>
            Report("Native operation was canceled; owned save/load cleanup reported: " + exception.Message);
    }
}
