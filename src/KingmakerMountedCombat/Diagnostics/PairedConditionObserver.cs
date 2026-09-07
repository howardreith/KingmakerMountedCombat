using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only native condition event evidence for the guarded fixture.
    internal sealed class PairedConditionObserver : IUnitGetUpHandler, IUnitLifeStateChanged,
        IUnitCommandActHandler, IDisposable
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly IDisposable subscription;
        private readonly JArray events = new JArray();
        internal PairedConditionObserver(UnitEntityData rider, UnitEntityData mount)
        {
            this.rider = rider; this.mount = mount;
            subscription = EventBus.Subscribe(this);
        }
        public void HandleUnitWillGetUp(UnitEntityData unit) { Record("native-get-up", unit); }
        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState previous)
        { Record("native-life-state", unit, previous.ToString()); }
        public void HandleUnitCommandDidAct(UnitCommand command)
        { Record("native-command-act", command?.Executor, command?.GetType().FullName); }
        private void Record(string kind, UnitEntityData actor, string detail = null)
        {
            if (actor != rider && actor != mount) return;
            events.Add(new JObject { ["kind"] = kind, ["actor"] = actor.UniqueId, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
                ["lifeState"] = actor.Descriptor.State.LifeState.ToString(), ["detail"] = detail,
                ["commandRunning"] = actor.Commands.IsRunning(),
                ["standard"] = actor.CombatState.Cooldown.StandardAction,
                ["move"] = actor.CombatState.Cooldown.MoveAction, ["damage"] = actor.Damage });
        }
        internal JObject Capture() => new JObject { ["events"] = events.DeepClone() };
        public void Dispose() { subscription.Dispose(); }
    }
}
