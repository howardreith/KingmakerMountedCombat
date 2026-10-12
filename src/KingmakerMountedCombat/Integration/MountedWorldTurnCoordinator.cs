using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // The single native-world dispatcher. Pair participants own their contexts,
    // never another roster, allocation ledger, archive worker or global clock.
    internal sealed class UnifiedMountedTurnCoordinator : IDisposable
    {
        private readonly MountedMovementStateAdapter movementState = new MountedMovementStateAdapter();
        private readonly List<MountedPairTurnParticipant> participants = new List<MountedPairTurnParticipant>();
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private bool disposed;
        private bool selectingLegacyCandidate;

        internal UnifiedMountedTurnCoordinator(GameMountedRelationshipService relationship,
            DiagnosticSettings settings, MountedPairCommandScheduler scheduler, IModLogger logger)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AddParticipant(relationship, scheduler);
        }

        internal MountedPairTurnParticipant Primary => participants.Count == 0 ? null : participants[0];
        internal MountedPairTurnParticipant[] SnapshotParticipants => participants.ToArray();
        internal Action<MountedPairTurnParticipant, Exception> ParticipantUpdateFailed { get; set; }
        internal MountedPairTurnParticipant AddParticipant(GameMountedRelationshipService relationship,
            MountedPairCommandScheduler scheduler)
        {
            var participant = CreateUnpublishedParticipant(relationship, scheduler);
            PublishParticipant(participant);
            return participant;
        }
        internal MountedPairTurnParticipant CreateUnpublishedParticipant(GameMountedRelationshipService relationship,
            MountedPairCommandScheduler scheduler)
        {
            if (disposed) throw new ObjectDisposedException(nameof(UnifiedMountedTurnCoordinator));
            if (relationship == null || scheduler == null) throw new ArgumentNullException();
            if (participants.Any(p => ReferenceEquals(p.Relationship, relationship)))
                throw new InvalidOperationException("A relationship already has a native turn participant.");
            if (participants.Count >= MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>.MaximumOwners)
                throw new InvalidOperationException("Native turn participant capacity is exhausted.");
            return new MountedPairTurnParticipant(relationship, settings, scheduler, logger, movementState, this);
        }
        internal void PublishParticipant(MountedPairTurnParticipant participant)
        {
            if (disposed || participant == null || participant.IsDisposed || !participant.BelongsToWorld(this) ||
                participants.Count >= MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>.MaximumOwners ||
                participants.Any(p => ReferenceEquals(p.Relationship, participant.Relationship)))
                throw new InvalidOperationException("Only one complete participant from this world may be published.");
            participants.Add(participant);
        }
        internal void AbandonUnpublishedParticipant(MountedPairTurnParticipant participant)
        {
            if (participant == null) return;
            if (!participant.BelongsToWorld(this) || participant.Relationship.PairOwner != null ||
                !participant.IsDisposed && !participant.CanReleaseBoundary)
                throw new InvalidOperationException("Unfinished pair participation cannot be discarded as construction rollback.");
            participant.Dispose();
            participants.Remove(participant);
        }

        internal bool RemoveParticipant(MountedPairTurnParticipant participant)
        {
            if (participant == null || ReferenceEquals(participant, Primary) || !participants.Contains(participant) ||
                !participant.CanReleaseBoundary || participant.Relationship.PairOwner != null) return false;
            participant.Dispose();
            participants.Remove(participant);
            return true;
        }

        // Registry membership wins over obsolete local identity markers. Every
        // fallback still matches exact actor/context references; selection is absent.
        internal MountedPairTurnParticipant Find(UnitEntityData actor)
        {
            if (disposed || actor == null) return null;
            var boundary = Primary?.Relationship.ResolveActorOwner(actor);
            return boundary != null ? participants.SingleOrDefault(p => !p.IsDisposed && ReferenceEquals(p.Relationship, boundary)) :
                Unique(participants.Where(p => p.OwnsActor(actor)));
        }
        internal MountedPairTurnParticipant Find(TurnController context) =>
            Find(context, Game.Instance?.TurnBasedCombatController?.CurrentTurn);
        internal MountedPairTurnParticipant Find(TurnController context, TurnController nativeCurrent)
        {
            if (disposed || context == null) return null;
            var exact = Unique(participants.Where(p => p.OwnsContext(context)));
            if (exact != null) return exact;
            // A new principal context binds only at the actual native current
            // turn. An old/foreign context cannot claim today's pair by actor ID.
            return ReferenceEquals(context, nativeCurrent) ? Find(context.Unit) : null;
        }
        private MountedPairTurnParticipant Find(UnitCommands commands) => commands == null || disposed ? null :
            Unique(participants.Where(p => p.OwnsCommands(commands)));
        private MountedPairTurnParticipant Find(UnitCombatState.Cooldowns cooldown) => cooldown == null || disposed ? null :
            Unique(participants.Where(p => p.OwnsCompletionDebt(cooldown)));
        private static MountedPairTurnParticipant Unique(IEnumerable<MountedPairTurnParticipant> matches) =>
            matches.SingleOrDefault();

        internal void BindCombat(MountedCombatController combat)
        {
            if (combat == null || !participants.Contains(combat.TurnParticipant))
                throw new InvalidOperationException("Combat must bind to a participant of this world.");
            combat.TurnParticipant.BindCombat(combat);
        }
        internal bool Enabled => !disposed && settings.UseLegacyUnifiedTurn;
        internal bool PairedLifecycleEnabled => !disposed && settings.EnablePairedActivation;
        internal bool HasUnsettledPreparation => movementState.HasUnsettledPreparation || participants.Any(p => p.HasUnsettledPreparation);
        internal int TrackedAllocationActorCount => movementState.TrackedActorCount;
        internal bool CanConfigurePairedActivation => !disposed && participants.All(p => p.CanConfigurePairedActivation);
        internal string PairedConfigurationFeedback { get; private set; }
        internal bool TryConfigurePairedActivation(bool enabled)
        {
            if (enabled == settings.EnablePairedActivation) return true;
            if (!CanConfigurePairedActivation)
            {
                PairedConfigurationFeedback = "Configure paired activation outside combat while every pair is dismounted.";
                return false;
            }
            if (enabled && (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler))
            {
                PairedConfigurationFeedback = "Disable both legacy turn experiments before enabling paired activation.";
                return false;
            }
            settings.EnablePairedActivation = enabled;
            PairedConfigurationFeedback = enabled ? "Paired activation enabled." : "Paired activation disabled.";
            logger.Info(PairedConfigurationFeedback);
            return true;
        }
        internal bool RetainsPairParticipation(UnitEntityData rider, UnitEntityData mount) =>
            participants.Any(p => p.RetainsPairParticipation(rider, mount));

        internal void Update()
        {
            if (disposed) return;
            movementState.MaintainLifetimes();
            var failures = new List<Exception>();
            foreach (var participant in SnapshotParticipants)
            {
                try { participant.Update(); }
                catch (Exception exception)
                {
                    if (ParticipantUpdateFailed == null) failures.Add(exception);
                    else
                    {
                        try { ParticipantUpdateFailed(participant, exception); }
                        catch (Exception cleanup) { failures.Add(new AggregateException(exception, cleanup)); }
                    }
                }
            }
            if (failures.Count != 0) throw new AggregateException("Native pair participation update did not settle.", failures);
        }

        // An actor's real Prepare has exactly one dispatcher and one allocation
        // observation, including allocated actors whose attachment has ended.
        internal bool HandleTurnPreparing(TurnController turn) =>
            HandleTurnPreparingAtCurrent(turn, Game.Instance?.TurnBasedCombatController?.CurrentTurn);
        // Native observation is captured once at the callback boundary. Detached
        // probes provide that observation; it is never ambient pair selection.
        internal bool HandleTurnPreparingAtCurrent(TurnController turn, TurnController nativeCurrent)
        {
            if (turn == null || disposed) return true;
            var participant = Find(turn, nativeCurrent);
            if (!ReferenceEquals(turn, nativeCurrent) &&
                (participant == null || !participant.IsPartnerContext(turn)))
            {
                // Native StartTurn assigns CurrentTurn before Start/Prepare. A
                // retained or foreign context cannot refresh a known actor.
                return participant == null && Find(turn.Unit) == null && !movementState.Owns(turn.Unit);
            }
            if (participant != null) return participant.HandleTurnPreparing(turn);
            if (PairedLifecycleEnabled)
            {
                if (movementState.Owns(turn.Unit)) movementState.BeginGrantedPreparation(turn, "native:" + Guid.NewGuid().ToString("N"));
            }
            else if (!Enabled) movementState.BeginPreparation(turn, null);
            return true;
        }
        internal void HandleTurnPrepared(TurnController turn)
        {
            var participant = Find(turn);
            if (participant != null) participant.HandleTurnPrepared(turn);
            else if (!disposed) movementState.EndPreparation(turn);
        }
        internal void HandleNativeRoundState(UnitCombatState state)
        { if (!disposed && !Enabled) movementState.BeforeNativeRoundState(state); }
        internal void ObserveNativeMovement(TurnController turn)
        {
            if (disposed || turn == null) return;
            var participant = Find(turn);
            if (!ReferenceEquals(turn, Game.Instance?.TurnBasedCombatController?.CurrentTurn) &&
                (participant == null || !participant.IsPartnerContext(turn))) return;
            if (participant != null) participant.ObserveNativeMovement(turn);
            else if (movementState.Owns(turn.Unit)) movementState.ObserveNativeMovement(turn, turn.Unit);
        }
        internal void ObserveNativeActionCost(UnitCommand command)
        {
            var participant = Find(command?.Executor);
            if (participant != null) participant.ObserveNativeActionCost(command);
            else if (!disposed) movementState.ObserveAction(command);
        }
        internal void RetireDestroyedActor(UnitEntityData actor)
        {
            Find(actor)?.BeforePairedActorRemoval(actor);
            if (!disposed) movementState.RetireDestroyedActor(actor);
        }
        internal void BeginNativeEncounter(CombatController controller, bool changed) =>
            Visit(p => p.BeginNativeEncounter(controller, changed));
        internal void ObserveModeChanged(bool enabled) => Visit(p => p.ObserveModeChanged(enabled));
        internal void BeforeNativeModeExit(CombatController controller) => Visit(p => p.BeforeNativeModeExit(controller));
        internal void BeforePairedActorRemoval(UnitEntityData actor) => Find(actor)?.BeforePairedActorRemoval(actor);
        internal void EndPairedServiceParticipation() => Visit(p => p.EndPairedServiceParticipation());

        internal bool SuppressPairedCandidate(CombatController.TBUnitInfo candidate) =>
            Find(candidate?.Unit)?.SuppressPairedCandidate(candidate) ?? false;
        internal void HandleChooseNextUnit(CombatController controller) => HandleLegacyCandidate(controller, false);
        internal void HandleCombatControllerTickCompleted(CombatController controller) => HandleLegacyCandidate(controller, true);
        private void HandleLegacyCandidate(CombatController controller, bool tick)
        {
            if (!Enabled || controller == null || selectingLegacyCandidate) return;
            var participant = Find(MountedPairTurnParticipant.NativeNextCandidate(controller));
            if (participant == null) return;
            selectingLegacyCandidate = true;
            try
            {
                if (tick) participant.HandleCombatControllerTickCompleted(controller);
                else participant.HandleChooseNextUnit(controller);
            }
            finally { selectingLegacyCandidate = false; }
        }
        internal void MirrorInitiativeEvent(RuleInitiativeRoll rule) => Find(rule?.Initiator)?.MirrorInitiativeEvent(rule);
        internal bool BeginTrackerProjection() => Primary?.BeginTrackerProjection() ?? false;
        internal void EndTrackerProjection(bool entered) => Primary?.EndTrackerProjection(entered);
        internal void FilterTrackerSortedUnits(ref IEnumerable<UnitEntityData> units)
        { foreach (var participant in SnapshotParticipants) participant.FilterTrackerSortedUnits(ref units); }
        internal void AdmitExactMountCommand(UnitCommand command, ref bool result)
        { Find(command?.Executor)?.AdmitExactMountCommand(command, ref result); }
        internal bool ShouldSuppressStepOpportunity(UnitEntityData target) => Find(target)?.ShouldSuppressStepOpportunity(target) ?? false;
        internal bool MaySelectPairedPartner(UnitEntityData actor) => Find(actor)?.MaySelectPairedPartner(actor) ?? false;
        internal bool PreservePartnerSelection(TurnController turn) => Find(turn)?.PreservePartnerSelection(turn) ?? false;
        internal TurnController SelectedNativeInputContext(TurnController turn) => Find(turn)?.SelectedNativeInputContext(turn) ?? turn;
        internal TurnController NativeActorActionContext(TurnController turn, UnitEntityData actor) =>
            Find(actor)?.NativeActorActionContext(turn, actor) ?? turn;
        internal bool IsNativeActionContextActor(UnitEntityData actor) => Find(actor)?.IsNativeActionContextActor(actor) ?? actor.IsCurrentUnit();
        internal bool ShouldRunNativeIgnoreClick(TurnController context, ref bool result)
        {
            var participant = Find(context);
            return participant == null || participant.ShouldRunNativeIgnoreClick(context, ref result);
        }
        internal bool PrepareNativeInputPrediction(TurnController context) => Find(context)?.PrepareNativeInputPrediction(context) ?? true;
        internal bool IsPartnerContext(TurnController turn) => Find(turn)?.IsPartnerContext(turn) ?? false;
        internal bool IsPairedResume(TurnController turn) => Find(turn)?.IsPairedResume(turn) ?? false;
        internal bool CanDelayPaired(TurnController turn) => Find(turn)?.CanDelayPaired(turn) ?? true;
        internal bool BeginPairedDelay(TurnController turn, UnitEntityData target) => Find(turn)?.BeginPairedDelay(turn, target) ?? true;
        internal bool NativeActorEligibleForCommand(UnitEntityData actor, UnitCommand command) =>
            Find(actor)?.NativeActorEligibleForCommand(actor, command) ?? actor.IsCurrentUnit();
        internal bool HasPairedActivity(TurnController turn) => Find(turn)?.HasPairedActivity(turn) ?? turn.IsActed();
        internal bool IsPairedProneActor(UnitEntityData actor) => Find(actor)?.IsPairedProneActor(actor) ?? actor.IsCurrentUnit();
        internal float PairedNativeReadiness(UnitEntityData actor) => Find(actor)?.PairedNativeReadiness(actor) ?? actor.GetTimeToNextTurn();
        internal float NativeCompletionValue(UnitCombatState.Cooldowns cooldown, float current, float native) =>
            Find(cooldown)?.NativeCompletionValue(cooldown, current, native) ?? native;
        internal float NativeFinalStandardValue(UnitCombatState.Cooldowns cooldown, float current, float native) =>
            Find(cooldown)?.NativeFinalStandardValue(cooldown, current, native) ?? native;
        internal void SynchronizePartnerPhase(TurnController turn) => Find(turn)?.SynchronizePartnerPhase(turn);
        internal void TickPairedNativeState(TurnController turn) => Find(turn)?.TickPairedNativeState(turn);
        internal void ExtendPairedWaiting(TurnController turn, ref bool result)
        { Find(turn)?.ExtendPairedWaiting(turn, ref result); }
        internal void ExtendTurnIfMountActionable(TurnController turn, ref bool result)
        { Find(turn)?.ExtendTurnIfMountActionable(turn, ref result); }
        internal void ForfeitPairedActivation(TurnController turn, bool costs) => Find(turn)?.ForfeitPairedActivation(turn, costs);
        internal void FinishPairedActivation(TurnController turn) => Find(turn)?.FinishPairedActivation(turn);
        internal void TickPreparationConfusion(UnitConfusionController controller, TurnController turn)
        {
            var participant = Find(turn);
            if (participant == null) controller.Tick(); else participant.TickPreparationConfusion(controller, turn);
        }
        internal bool OwnsPartnerRoundEffects(UnitEntityData current, UnitEntityData actor) =>
            Find(actor)?.OwnsPartnerRoundEffects(current, actor) ?? false;
        internal bool AdmitNativePreparationCommand(UnitCommands commands, UnitCommand command) =>
            Find(commands)?.AdmitNativePreparationCommand(commands, command) ?? false;
        internal bool MayStartNativePreparationDuringSave(UnitCommand command) =>
            Find(command?.Executor)?.MayStartNativePreparationDuringSave(command) ?? false;
        internal bool IsNativeConditionCommandActor(UnitEntityData actor, UnitCommand command) =>
            Find(actor)?.IsNativeConditionCommandActor(actor, command) ?? actor.IsCurrentUnit();
        internal void ForfeitNativeConditionActor(TurnController turn, bool costs, UnitCommand command)
        {
            var participant = Find(command?.Executor) ?? Find(turn);
            if (participant == null) turn.ForceToEnd(costs); else participant.ForfeitNativeConditionActor(turn, costs, command);
        }
        internal bool CompleteNativeConditionForfeit(TurnController context) => Find(context)?.CompleteNativeConditionForfeit(context) ?? false;
        internal static void CompleteNativeForfeitPhase(TurnController context) => MountedPairTurnParticipant.CompleteNativeForfeitPhase(context);
        internal bool TryMoveNativePreparationActor(UnitMovementAgent agent, ref float deltaTime, out bool result)
        {
            var participant = Find(agent?.Unit?.EntityData);
            if (participant != null) return participant.TryMoveNativePreparationActor(agent, ref deltaTime, out result);
            result = false; return false;
        }

        internal IEnumerable<UnitEntityData> PersistenceActors => movementState.PersistenceActors
            .Concat(participants.SelectMany(p => p.ParticipationActors)).Where(a => a != null).Distinct();
        internal void CaptureSharedPersistence(SavedCombatData saved)
        {
            saved.Allocations = movementState.CapturePersistence();
            saved.Paired = null;
        }
        internal void CapturePersistence(SavedCombatData saved)
        {
            CaptureSharedPersistence(saved);
            var states = participants.Select(p => p.CaptureParticipation()).Where(p => p != null).ToArray();
            if (states.Length > 1) throw new InvalidOperationException("The legacy native adapter cannot capture multiple pair participations.");
            saved.Paired = states.SingleOrDefault();
        }
        internal void RestoreSharedPersistence(SavedCombatData saved, IDictionary<string, UnitEntityData> actors)
        {
            DiscardPersistenceWorld();
            movementState.RestorePersistence(saved.Allocations, actors);
        }
        internal void RestorePersistence(SavedCombatData saved, IDictionary<string, UnitEntityData> actors)
        {
            RestoreSharedPersistence(saved, actors);
            Primary.RestoreParticipation(saved.Paired, actors, saved.TurnBased);
        }
        internal void DiscardPersistenceWorld()
        {
            Visit(p => p.DiscardParticipation());
            movementState.Clear();
        }
        private void Visit(Action<MountedPairTurnParticipant> action)
        {
            var failures = new List<Exception>();
            foreach (var participant in SnapshotParticipants)
            {
                try { action(participant); }
                catch (Exception exception) { failures.Add(exception); }
            }
            if (failures.Count != 0) throw new AggregateException("Pair participation boundary did not settle.", failures);
        }
        public void Dispose()
        {
            if (disposed) return;
            EndPairedServiceParticipation();
            Visit(p => p.Dispose());
            movementState.Clear(); participants.Clear(); disposed = true;
        }

        // Historical one-pair diagnostics only. Execution above never uses this
        // presentation projection as a fallback owner.
        internal string ActivationIdentity => Primary?.ActivationIdentity;
        internal TurnController PartnerContext => Primary?.PartnerContext;
        internal UnifiedMountedTurnSnapshot CaptureSnapshot() => Primary?.CaptureSnapshot();
    }
}
