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
        private readonly bool captureLifeSource;
        private readonly JArray events = new JArray();
        internal PairedConditionObserver(UnitEntityData rider, UnitEntityData mount, bool captureLifeSource = false)
        {
            this.rider = rider; this.mount = mount;
            this.captureLifeSource = captureLifeSource;
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
                ["move"] = actor.CombatState.Cooldown.MoveAction, ["damage"] = actor.Damage,
                ["nativeSource"] = captureLifeSource && kind == "native-life-state" ? CaptureLifeSource() : null });
        }
        private static JArray CaptureLifeSource()
        {
            var source = new JArray();
            foreach (var frame in new System.Diagnostics.StackTrace(false).GetFrames() ?? new System.Diagnostics.StackFrame[0])
            {
                var method = frame.GetMethod(); var type = method?.DeclaringType?.FullName;
                if (type != "Kingmaker.Controllers.Units.UnitLifeController" &&
                    type != "Kingmaker.Controllers.Units.UnitReturnToConsciousController") continue;
                source.Add(new JObject { ["type"] = type, ["method"] = method.Name,
                    ["token"] = method.MetadataToken.ToString("x8"), ["assemblyMvid"] = method.Module.ModuleVersionId.ToString("D") });
                if (source.Count == 8) break;
            }
            return source;
        }
        internal JObject Capture() => new JObject { ["events"] = events.DeepClone() };
        public void Dispose() { subscription.Dispose(); }
    }
}
