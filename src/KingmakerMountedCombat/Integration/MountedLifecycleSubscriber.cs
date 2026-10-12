using System;
using System.Collections.Generic;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedLifecycleSubscriber : IUnitCombatHandler, IPartyCombatHandler, IUnitLifeStateChanged, IPartyLeaveAreaHandler, ITurnBasedModeEnabledHandler, IGameModeHandler, ISceneHandler, IAreaLoadingStagesHandler, IUnitViewAttachedUIHandler, IUnitFinallyDeadHandler, IUnitHandler, IPartyHandler, IInGameHandler, IDisposable
    {
        private readonly GameMountedRelationshipService service;
        private readonly NativeLifecycleDeliveryLedger ledger;
        private readonly MountedCombatController combat;
        private readonly UnifiedMountedTurnCoordinator unifiedTurn;
        private readonly MountedPersistenceService persistence;
        private readonly IDisposable subscription;
        private readonly object lifeTransitionGate = new object();
        private readonly List<NativePairLifeStateObservation> pairLifeTransitions = new List<NativePairLifeStateObservation>();
        private long pairLifeTransitionSequence;
        private bool disposed;

        public MountedLifecycleSubscriber(GameMountedRelationshipService service, NativeLifecycleDeliveryLedger ledger, MountedCombatController combat, UnifiedMountedTurnCoordinator unifiedTurn, MountedPersistenceService persistence)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.unifiedTurn = unifiedTurn ?? throw new ArgumentNullException(nameof(unifiedTurn));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            subscription = EventBus.Subscribe(this);
            combat.ChargeNativeModeResumed += CompleteNativeModeObservation;
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { Observe(NativeLifecycleBoundary.CombatStarted, "IUnitCombatHandler.HandleUnitJoinCombat"); }
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            var owner = service.ResolveActorOwner(unit);
            if (owner != null) { owner.CombatOwner.Cancel("unit left combat"); Observe(NativeLifecycleBoundary.CombatEnded, "IUnitCombatHandler.HandleUnitLeaveCombat"); }
        }

        public void HandlePartyCombatStateChanged(bool inCombat)
        {
            if (inCombat) { Observe(NativeLifecycleBoundary.CombatStarted, "IPartyCombatHandler.HandlePartyCombatStateChanged(true)"); }
            else { VisitCombatOwners(owner => owner.Cancel("party combat ended")); Observe(NativeLifecycleBoundary.CombatEnded, "IPartyCombatHandler.HandlePartyCombatStateChanged(false)"); }
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            if (!IsPairUnit(unit)) { return; }
            lock (lifeTransitionGate)
            {
                pairLifeTransitionSequence++;
                pairLifeTransitions.Add(new NativePairLifeStateObservation
                {
                    Sequence = pairLifeTransitionSequence,
                    ActorId = unit.UniqueId,
                    PreviousLifeState = prevLifeState.ToString(),
                    CurrentLifeState = unit.Descriptor.State.LifeState.ToString()
                });
                if (pairLifeTransitions.Count > 32) { pairLifeTransitions.RemoveAt(0); }
            }
            if (!unit.Descriptor.State.IsConscious) { CleanupActor(unit, NativeLifecycleBoundary.UnitIncapacitated, "IUnitLifeStateChanged.HandleUnitLifeStateChanged", CleanupTrigger.Incapacitated); }
        }

        public void HandlePartyLeaveArea(BlueprintArea currentArea, BlueprintAreaEnterPoint targetArea)
        {
            persistence.BeginAreaTransition(targetArea?.Area, null);
            SuspendArea("IPartyLeaveAreaHandler.HandlePartyLeaveArea");
        }

        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            // CombatController's exact outer prefix owns admission and its postfix
            // completes this notification. EventBus subscriber order cannot move
            // these observations before an unresolved charge boundary.
        }

        private void CompleteNativeModeObservation(bool enabled)
        {
            VisitCombatOwners(owner => owner.Cancel("real-time/turn-based mode changed"));
            var owners = service.Ownership.Owners;
            if (owners.Length == 0) service.ObserveNativeTurnBasedModeChanged(enabled);
            else foreach (var owner in owners) owner.Boundary.ObserveNativeTurnBasedModeChanged(enabled);
            unifiedTurn.ObserveModeChanged(enabled);
            Observe(
                enabled ? NativeLifecycleBoundary.TurnBasedEnabled : NativeLifecycleBoundary.RealtimeEnabled,
                "ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(" + enabled + ")");
        }

        public void OnGameModeStart(GameModeType gameMode)
        {
            ObserveOrCleanupGameMode(NativeLifecycleBoundary.GameModeStarted, "IGameModeHandler.OnGameModeStart(" + gameMode + ")", gameMode);
        }

        public void OnGameModeStop(GameModeType gameMode)
        {
            ObserveOrCleanupGameMode(NativeLifecycleBoundary.GameModeStopped, "IGameModeHandler.OnGameModeStop(" + gameMode + ")", gameMode);
        }

        public void OnAreaBeginUnloading()
        {
            SuspendArea("ISceneHandler.OnAreaBeginUnloading");
        }

        public void OnAreaDidLoad()
        {
            Observe(NativeLifecycleBoundary.AreaDidLoad, "ISceneHandler.OnAreaDidLoad");
        }

        public void OnAreaScenesLoaded()
        {
            Observe(NativeLifecycleBoundary.AreaScenesLoaded, "IAreaLoadingStagesHandler.OnAreaScenesLoaded");
        }

        public void OnAreaLoadingComplete()
        {
            Observe(NativeLifecycleBoundary.AreaLoadingComplete, "IAreaLoadingStagesHandler.OnAreaLoadingComplete");
        }

        public void HandleUnitViewAttached(UnitEntityData unit)
        {
            var owner = service.ResolveActorOwner(unit);
            if (owner != null)
            {
                var observation = owner.CapturePresentationObservation();
                var disposition = MountedViewAttachmentPolicy.Classify(
                    owner.State == RelationshipState.Mounted,
                    true,
                    owner.IsExactCapturedView(unit),
                    owner.IsChangedViewChildOfOwnedAnchor(unit));
                if (disposition == MountedViewAttachmentDisposition.ObserveExactView)
                {
                    Observe(NativeLifecycleBoundary.ViewAttached, "IUnitViewAttachedUIHandler.HandleUnitViewAttached(exact mounted view)", observation);
                }
                else if (disposition == MountedViewAttachmentDisposition.IgnoreNonPair)
                {
                    Observe(NativeLifecycleBoundary.ViewAttached, "IUnitViewAttachedUIHandler.HandleUnitViewAttached(inactive pair)", observation);
                }
                else
                {
                    CleanupOwner(owner, NativeLifecycleBoundary.ViewAttached, "IUnitViewAttachedUIHandler.HandleUnitViewAttached(" + disposition + ")", CleanupTrigger.ViewReplaced, observation);
                }
            }
            else if (IsCandidatePairUnit(unit))
            {
                Observe(NativeLifecycleBoundary.ViewAttached, "IUnitViewAttachedUIHandler.HandleUnitViewAttached(candidate pair)");
            }
        }

        public void HandleUnitSpawned(UnitEntityData entityData) { }

        public void HandleUnitDestroyed(UnitEntityData entityData)
        {
            var owner = service.Ownership.FindOwner(entityData);
            (owner?.Boundary.CombatOwner ?? combat).RetainDestroyedActorBoundary(entityData, () => CompleteUnitDestroyed(entityData, owner));
        }

        private void CompleteUnitDestroyed(UnitEntityData entityData,
            MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner)
        {
            unifiedTurn.RetireDestroyedActor(entityData);
            if (service.Ownership.Owns(owner)) { CleanupOwner(owner.Boundary, NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed, "IUnitHandler.HandleUnitDestroyed", CleanupTrigger.ViewDetached); }
            else if (IsCandidatePairUnit(entityData)) { Observe(NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed, "IUnitHandler.HandleUnitDestroyed(candidate pair)"); }
        }

        public void HandleUnitDeath(UnitEntityData entityData)
        {
            if (IsPairUnit(entityData)) { CleanupActor(entityData, NativeLifecycleBoundary.UnitDeath, "IUnitHandler.HandleUnitDeath", CleanupTrigger.Death); }
        }

        public void HandleUnitBecameFinallyDead(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { CleanupActor(unit, NativeLifecycleBoundary.UnitFinallyDead, "IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead", CleanupTrigger.Death); }
        }

        public void HandleAddCompanion(UnitEntityData unit) { }

        public void HandleCompanionActivated(UnitEntityData unit)
        {
            service.ResolveActorOwner(unit)?.ValidateActivePair();
        }

        public void HandleCompanionRemoved(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { CleanupActor(unit, NativeLifecycleBoundary.PartyRemoved, "IPartyHandler.HandleCompanionRemoved", CleanupTrigger.CompanionInvalidated); }
        }

        public void HandleObjectInGameChaged(EntityDataBase entityData)
        {
            var owner = service.ResolveActorOwner(entityData as UnitEntityData);
            if (owner != null)
            {
                var before = owner.State;
                owner.ValidateActivePair();
                var after = owner.State;
                var attempted = before == RelationshipState.Mounted && after != RelationshipState.Mounted;
                ledger.Record(
                    NativeLifecycleBoundary.InGameStateChanged,
                    "IInGameHandler.HandleObjectInGameChaged",
                    before,
                    after,
                    attempted ? (CleanupTrigger?)CleanupTrigger.CompanionInvalidated : null,
                    attempted,
                    !attempted || after == RelationshipState.Unmounted);
            }
        }

        internal bool HandleModDisable()
        {
            if (!service.AllTeardownOwnersReady || combat.ChargeAdmissionFenced) return false;
            var drained = true;
            var errors = service.Ownership.VisitOwners(owner =>
            {
                if (!owner.Boundary.CombatOwner.TryDrainChargeOwnership("mod disable")) drained = false;
            });
            if (errors.Length != 0 || !drained || service.Ownership.Owners.Length == 0 && !combat.TryDrainChargeOwnership("mod disable")) return false;
            unifiedTurn.EndPairedServiceParticipation();
            return Cleanup(NativeLifecycleBoundary.ModDisable, "UnityModManager.ModEntry.OnToggle(false)/shutdown", CleanupTrigger.ModDisabled);
        }

        internal IReadOnlyList<NativeLifecycleDeliveryRecord> SnapshotNativeDeliveries()
        {
            return ledger.Snapshot();
        }

        internal IReadOnlyList<NativePairLifeStateObservation> SnapshotPairLifeTransitions()
        {
            lock (lifeTransitionGate) { return pairLifeTransitions.ToArray(); }
        }

        public void Dispose()
        {
            if (disposed) { return; }
            subscription.Dispose();
            combat.ChargeNativeModeResumed -= CompleteNativeModeObservation;
            disposed = true;
        }

        private bool IsPairUnit(UnitEntityData unit)
        {
            return unit != null && service.ResolveActorOwner(unit) != null;
        }

        private static bool IsCandidatePairUnit(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }

            var unitIsSupportedMount = SupportedMountedProfiles.IsSupported(unit);
            var pet = unit.Descriptor?.Pet;
            var ownsSupportedMount = SupportedMountedProfiles.IsSupported(pet);
            return unitIsSupportedMount || ownsSupportedMount;
        }

        private void SuspendArea(string source)
        {
            if (persistence.SuspendAreaPair())
                Observe(NativeLifecycleBoundary.AreaBeginUnload, source + "(mounted area suspension)");
            else Cleanup(NativeLifecycleBoundary.AreaBeginUnload, source, CleanupTrigger.AreaUnloading);
        }

        private void VisitCombatOwners(Action<MountedCombatController> action)
        {
            if (service.Ownership.Owners.Length == 0) { action(combat); return; }
            var errors = service.Ownership.VisitOwners(owner => action(owner.Boundary.CombatOwner));
            if (errors.Length != 0) throw new AggregateException("Mounted owner boundary did not settle.", errors);
        }

        private bool CleanupActor(UnitEntityData actor, NativeLifecycleBoundary boundary, string source, CleanupTrigger trigger)
        {
            var owner = service.ResolveActorOwner(actor);
            return owner == null || CleanupOwner(owner, boundary, source, trigger);
        }

        private bool Cleanup(NativeLifecycleBoundary boundary, string source, CleanupTrigger trigger, string detail = null)
        {
            var succeeded = true;
            var owners = service.Ownership.Owners;
            if (owners.Length == 0) return CleanupOwner(service, boundary, source, trigger, detail);
            var errors = service.Ownership.VisitOwners(owner =>
            {
                if (!CleanupOwner(owner.Boundary, boundary, source, trigger, detail)) succeeded = false;
            });
            return succeeded && errors.Length == 0;
        }

        private bool CleanupOwner(GameMountedRelationshipService owner, NativeLifecycleBoundary boundary, string source,
            CleanupTrigger trigger, string detail = null)
        {
            var before = owner.State;
            var result = owner.Dismount(trigger);
            var succeeded = result.Succeeded && !result.MovementAuthorityResidual && !result.PresentationResidual &&
                owner.State == RelationshipState.Unmounted;
            ledger.Record(boundary, source, before, owner.State, trigger, true, succeeded, result.Errors, detail);
            return succeeded;
        }

        private void Observe(NativeLifecycleBoundary boundary, string source, string detail = null)
        {
            var state = service.State;
            ledger.Record(boundary, source, state, state, null, false, true, null, detail);
        }

        private void ObserveOrCleanupGameMode(NativeLifecycleBoundary boundary, string source, GameModeType gameMode)
        {
            if (service.SaveSerializationSuspended ||
                (gameMode == GameModeType.None && persistence.AreaTransitionPending) ||
                MountedGameModePolicy.CanRetainMountedRelationship(gameMode.ToString()) || service.State != RelationshipState.Mounted)
            {
                Observe(boundary, source, service.CapturePresentationObservation(false));
                return;
            }

            Cleanup(boundary, source, CleanupTrigger.GameModeBoundary, service.CapturePresentationObservation(false));
        }
    }

    internal sealed class NativePairLifeStateObservation
    {
        public long Sequence { get; set; }
        public string ActorId { get; set; }
        public string PreviousLifeState { get; set; }
        public string CurrentLifeState { get; set; }
    }
}
