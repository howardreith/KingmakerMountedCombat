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
        private readonly IDisposable subscription;
        private readonly object lifeTransitionGate = new object();
        private readonly List<NativePairLifeStateObservation> pairLifeTransitions = new List<NativePairLifeStateObservation>();
        private long pairLifeTransitionSequence;
        private bool disposed;

        public MountedLifecycleSubscriber(GameMountedRelationshipService service, NativeLifecycleDeliveryLedger ledger, MountedCombatController combat)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            subscription = EventBus.Subscribe(this);
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { Observe(NativeLifecycleBoundary.CombatStarted, "IUnitCombatHandler.HandleUnitJoinCombat"); }
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { combat.Cancel("unit left combat"); Observe(NativeLifecycleBoundary.CombatEnded, "IUnitCombatHandler.HandleUnitLeaveCombat"); }
        }

        public void HandlePartyCombatStateChanged(bool inCombat)
        {
            if (inCombat) { Observe(NativeLifecycleBoundary.CombatStarted, "IPartyCombatHandler.HandlePartyCombatStateChanged(true)"); }
            else { combat.Cancel("party combat ended"); Observe(NativeLifecycleBoundary.CombatEnded, "IPartyCombatHandler.HandlePartyCombatStateChanged(false)"); }
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
            if (!unit.Descriptor.State.IsConscious) { Cleanup(NativeLifecycleBoundary.UnitIncapacitated, "IUnitLifeStateChanged.HandleUnitLifeStateChanged", CleanupTrigger.Incapacitated); }
        }

        public void HandlePartyLeaveArea(BlueprintArea currentArea, BlueprintAreaEnterPoint targetArea)
        {
            Cleanup(NativeLifecycleBoundary.AreaBeginUnload, "IPartyLeaveAreaHandler.HandlePartyLeaveArea", CleanupTrigger.AreaUnloading);
        }

        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            combat.Cancel("real-time/turn-based mode changed");
            service.ObserveNativeTurnBasedModeChanged(enabled);
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
            Cleanup(NativeLifecycleBoundary.AreaBeginUnload, "ISceneHandler.OnAreaBeginUnloading", CleanupTrigger.AreaUnloading);
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
            if (IsPairUnit(unit))
            {
                var observation = service.CapturePresentationObservation();
                var disposition = MountedViewAttachmentPolicy.Classify(
                    service.State == RelationshipState.Mounted,
                    true,
                    service.IsExactCapturedView(unit),
                    service.IsChangedViewChildOfOwnedAnchor(unit));
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
                    Cleanup(NativeLifecycleBoundary.ViewAttached, "IUnitViewAttachedUIHandler.HandleUnitViewAttached(" + disposition + ")", CleanupTrigger.ViewReplaced, observation);
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
            if (IsPairUnit(entityData)) { Cleanup(NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed, "IUnitHandler.HandleUnitDestroyed", CleanupTrigger.ViewDetached); }
            else if (IsCandidatePairUnit(entityData)) { Observe(NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed, "IUnitHandler.HandleUnitDestroyed(candidate pair)"); }
        }

        public void HandleUnitDeath(UnitEntityData entityData)
        {
            if (IsPairUnit(entityData)) { Cleanup(NativeLifecycleBoundary.UnitDeath, "IUnitHandler.HandleUnitDeath", CleanupTrigger.Death); }
        }

        public void HandleUnitBecameFinallyDead(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { Cleanup(NativeLifecycleBoundary.UnitFinallyDead, "IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead", CleanupTrigger.Death); }
        }

        public void HandleAddCompanion(UnitEntityData unit) { }

        public void HandleCompanionActivated(UnitEntityData unit)
        {
            service.ValidateActivePair();
        }

        public void HandleCompanionRemoved(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { Cleanup(NativeLifecycleBoundary.PartyRemoved, "IPartyHandler.HandleCompanionRemoved", CleanupTrigger.CompanionInvalidated); }
        }

        public void HandleObjectInGameChaged(EntityDataBase entityData)
        {
            if (IsPairUnit(entityData as UnitEntityData))
            {
                var before = service.State;
                service.ValidateActivePair();
                var after = service.State;
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
            disposed = true;
        }

        private bool IsPairUnit(UnitEntityData unit)
        {
            return unit != null && (unit == service.Rider || unit == service.Mount);
        }

        private static bool IsCandidatePairUnit(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }

            var unitIsMammoth = unit.Blueprint != null && string.Equals(
                unit.Blueprint.AssetGuid,
                KingmakerMountedPairRuntime.MammothBlueprintGuid,
                StringComparison.Ordinal);
            var pet = unit.Descriptor?.Pet;
            var ownsMammoth = pet?.Blueprint != null && string.Equals(
                pet.Blueprint.AssetGuid,
                KingmakerMountedPairRuntime.MammothBlueprintGuid,
                StringComparison.Ordinal);
            return unitIsMammoth || ownsMammoth;
        }

        private bool Cleanup(NativeLifecycleBoundary boundary, string source, CleanupTrigger trigger, string detail = null)
        {
            var before = service.State;
            var result = service.Dismount(trigger);
            var succeeded = result.Succeeded && !result.MovementAuthorityResidual && !result.PresentationResidual &&
                service.State == RelationshipState.Unmounted;
            ledger.Record(boundary, source, before, service.State, trigger, true, succeeded, result.Errors, detail);
            return succeeded;
        }

        private void Observe(NativeLifecycleBoundary boundary, string source, string detail = null)
        {
            var state = service.State;
            ledger.Record(boundary, source, state, state, null, false, true, null, detail);
        }

        private void ObserveOrCleanupGameMode(NativeLifecycleBoundary boundary, string source, GameModeType gameMode)
        {
            if (MountedGameModePolicy.CanRetainMountedRelationship(gameMode.ToString()) || service.State != RelationshipState.Mounted)
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
