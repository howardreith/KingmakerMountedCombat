using System;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedLifecycleSubscriber : IUnitCombatHandler, IPartyCombatHandler, IUnitLifeStateChanged, IPartyLeaveAreaHandler, ITurnBasedModeEnabledHandler, IGameModeHandler, ISceneHandler, IUnitHandler, IPartyHandler, IInGameHandler, IDisposable
    {
        private readonly GameMountedRelationshipService service;
        private readonly IDisposable subscription;
        private bool disposed;

        public MountedLifecycleSubscriber(GameMountedRelationshipService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            subscription = EventBus.Subscribe(this);
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { service.Dismount(CleanupTrigger.CombatStarted); }
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit) { }

        public void HandlePartyCombatStateChanged(bool inCombat)
        {
            if (inCombat) { service.Dismount(CleanupTrigger.CombatStarted); }
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            if (IsPairUnit(unit) && !unit.Descriptor.State.IsConscious) { service.Dismount(CleanupTrigger.Incapacitated); }
        }

        public void HandlePartyLeaveArea(BlueprintArea currentArea, BlueprintAreaEnterPoint targetArea)
        {
            service.Dismount(CleanupTrigger.AreaUnloading);
        }

        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            service.Dismount(enabled ? CleanupTrigger.TurnBasedModeChanged : CleanupTrigger.RealtimeModeChanged);
        }

        public void OnGameModeStart(GameModeType gameMode)
        {
            if (gameMode != GameModeType.Default) { service.Dismount(CleanupTrigger.AreaUnloading); }
        }

        public void OnGameModeStop(GameModeType gameMode)
        {
            if (gameMode != GameModeType.Default) { service.Dismount(CleanupTrigger.AreaUnloading); }
        }

        public void OnAreaBeginUnloading()
        {
            service.Dismount(CleanupTrigger.AreaUnloading);
        }

        public void OnAreaDidLoad() { }

        public void HandleUnitSpawned(UnitEntityData entityData) { }

        public void HandleUnitDestroyed(UnitEntityData entityData)
        {
            if (IsPairUnit(entityData)) { service.Dismount(CleanupTrigger.ViewDetached); }
        }

        public void HandleUnitDeath(UnitEntityData entityData)
        {
            if (IsPairUnit(entityData)) { service.Dismount(CleanupTrigger.Death); }
        }

        public void HandleAddCompanion(UnitEntityData unit) { }

        public void HandleCompanionActivated(UnitEntityData unit)
        {
            service.ValidateActivePair();
        }

        public void HandleCompanionRemoved(UnitEntityData unit)
        {
            if (IsPairUnit(unit)) { service.Dismount(CleanupTrigger.CompanionInvalidated); }
        }

        public void HandleObjectInGameChaged(EntityDataBase entityData)
        {
            if (IsPairUnit(entityData as UnitEntityData)) { service.ValidateActivePair(); }
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
    }
}
