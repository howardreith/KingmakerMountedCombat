using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // An owned diagnostic Fact uses Kingmaker's native AddCondition component.
    // The lease never writes IsPanicked, direct control, commands, turns, or resources.
    internal sealed class NativePendingMountFearLease : IDisposable
    {
        private readonly UnitEntityData actor;
        private readonly BlueprintFeature blueprint;
        private readonly AddCondition templateComponent;
        private readonly AddCondition activeComponent;
        private readonly string blueprintId, templateId;
        private readonly int factObject, templateComponentObject, activeComponentObject;
        private bool disposed;

        internal NativePendingMountFearLease(UnitEntityData actor)
        {
            if (actor == null || !actor.IsInCombat || !actor.IsDirectlyControllable ||
                actor.Descriptor.State.IsPanicked || actor.Descriptor.State.HasCondition(UnitCondition.Frightened) ||
                actor.Descriptor.State.HasConditionImmunity(UnitCondition.Frightened))
                throw new InvalidOperationException("Pending-Mount fear fixture requires one directly controlled actor without existing fear state.");
            this.actor = actor;
            var templates = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.OfType<BlueprintFeature>()
                .Where(item => item.name == "RapidShot" || item.Name == "Rapid Shot").ToArray();
            if (templates.Length != 1)
                throw new InvalidOperationException("Expected one native unit-fact template; found " + templates.Length + ".");
            var template = templates[0];
            templateId = template.AssetGuid;
            blueprint = UnityEngine.Object.Instantiate(template);
            templateComponent = ScriptableObject.CreateInstance<AddCondition>();
            blueprint.name = "KMC_DiagnosticPendingMountFrightenedFact";
            blueprintId = Guid.NewGuid().ToString("N");
            blueprint.AssetGuid = blueprintId;
            templateComponent.name = "KMC_DiagnosticPendingMountFrightened";
            templateComponent.Condition = UnitCondition.Frightened;
            blueprint.ComponentsArray = new BlueprintComponent[] { templateComponent };
            templateComponentObject = templateComponent.GetInstanceID();
            try
            {
                var context = new MechanicsContext(actor, actor.Descriptor, blueprint);
                var fact = actor.Logic.AddFact(blueprint, context);
                if (fact == null || !actor.Logic.HasFact(blueprint))
                    throw new InvalidOperationException("Native Frightened fact did not activate on the exact rider.");
                activeComponent = fact.Components.OfType<AddCondition>().Single();
                activeComponentObject = activeComponent.GetInstanceID();
                factObject = RuntimeHelpers.GetHashCode(fact);
                if (!actor.Descriptor.State.HasCondition(UnitCondition.Frightened) || actor.Descriptor.State.IsPanicked ||
                    !actor.IsDirectlyControllable)
                    throw new InvalidOperationException("Native Frightened fact did not preserve control until UnitFearController processed it.");
            }
            catch { Dispose(); throw; }
        }

        internal JObject Capture()
        {
            var on = typeof(AddCondition).GetMethod("OnTurnOn", BindingFlags.Public | BindingFlags.Instance);
            var off = typeof(AddCondition).GetMethod("OnTurnOff", BindingFlags.Public | BindingFlags.Instance);
            return new JObject {
                ["contract"] = "owned-native-frightened-fact-awaits-unit-fear-controller",
                ["actorId"] = actor.UniqueId, ["blueprintId"] = blueprintId, ["templateId"] = templateId,
                ["factObject"] = factObject, ["templateComponentObject"] = templateComponentObject,
                ["activeComponentObject"] = activeComponentObject, ["componentType"] = typeof(AddCondition).FullName,
                ["condition"] = UnitCondition.Frightened.ToString(), ["activeFactCount"] = actor.Logic.Enumerable.Count(item => item.Blueprint == blueprint),
                ["conditionActive"] = actor.Descriptor.State.HasCondition(UnitCondition.Frightened),
                ["conditionImmune"] = actor.Descriptor.State.HasConditionImmunity(UnitCondition.Frightened),
                ["panicked"] = actor.Descriptor.State.IsPanicked, ["directlyControllable"] = actor.IsDirectlyControllable,
                ["onTurnOnToken"] = on == null ? null : on.MetadataToken.ToString("X8"),
                ["onTurnOffToken"] = off == null ? null : off.MetadataToken.ToString("X8"),
                ["moduleMvid"] = typeof(AddCondition).Assembly.ManifestModule.ModuleVersionId.ToString(),
                ["disposed"] = disposed
            };
        }

        public void Dispose()
        {
            if (disposed) return;
            foreach (var owned in actor.Logic.Enumerable.Where(item => item.Blueprint == blueprint).ToArray())
                actor.Logic.RemoveFact(owned);
            disposed = true;
            UnityEngine.Object.Destroy(blueprint);
            UnityEngine.Object.Destroy(templateComponent);
        }
    }

    // Read-only wrapper around the installed fear controller. It proves the
    // controller itself removes and later restores direct control around the exact command.
    internal sealed class NativeFearControlProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.PendingMountFearControl";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeFearControlProbe active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider;
        private readonly UnitUseAbility command;
        private readonly Func<JObject> state;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private JObject before;
        private int depth, lossCount, restorationCount;
        private bool disposed;

        internal bool LossObserved { get { return lossCount == 1; } }
        internal bool RestorationObserved { get { return restorationCount == 1; } }

        internal NativeFearControlProbe(UnitEntityData rider, UnitUseAbility command, Func<JObject> state)
        {
            if (active != null) throw new InvalidOperationException("A pending-Mount fear observer is already active.");
            if (rider == null || command == null || state == null || command.Executor != rider || command.IsStarted ||
                command.IsActed || command.IsFinished || command.ExecutionProcess != null ||
                !ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), command))
                throw new InvalidOperationException("Fear observation requires the exact pending unacted Mount in the rider Move slot.");
            if (typeof(UnitFearController).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Fear observation requires the pinned native assembly.");
            this.rider = rider; this.command = command; this.state = state;
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                var method = typeof(UnitFearController).GetMethods(Flags).Single(item => item.MetadataToken == 0x06009138);
                harmony.Patch(method,
                    new HarmonyMethod(typeof(Hooks).GetMethod("Before", Flags)) { prioritiy = Priority.First },
                    new HarmonyMethod(typeof(Hooks).GetMethod("After", Flags)) { prioritiy = Priority.Last });
                hooks.Add(new JObject { ["method"] = method.DeclaringType.FullName + "." + method.Name,
                    ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString(),
                    ["prefix"] = "Before", ["postfix"] = "After" });
            }
            catch { Dispose(); throw; }
        }

        private static int Id(object value) { return value == null ? 0 : RuntimeHelpers.GetHashCode(value); }
        private JObject Snapshot()
        {
            var move = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
            return new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["conditionActive"] = rider.Descriptor.State.HasCondition(UnitCondition.Frightened),
                ["panicked"] = rider.Descriptor.State.IsPanicked, ["directlyControllable"] = rider.IsDirectlyControllable,
                ["commandObject"] = Id(command), ["commandStarted"] = command.IsStarted, ["commandActed"] = command.IsActed,
                ["commandFinished"] = command.IsFinished, ["commandResult"] = command.Result.ToString(),
                ["commandProcessObject"] = Id(command.ExecutionProcess), ["moveSlotObject"] = Id(move),
                ["moveSlotType"] = move == null ? null : move.GetType().FullName, ["commandsEmpty"] = rider.Commands.Empty,
                ["visibleConsciousEnemies"] = new JArray(rider.Memory.Enemies.Where(item => item.Unit != null &&
                    item.Unit.Descriptor.State.IsConscious && rider.HasLOS(item.Unit)).Select(item => item.Unit.UniqueId)),
                ["state"] = state()
            };
        }
        private void BeforeTick()
        {
            if (depth != 0) { errors.Add("Native fear controller reentered for the exact rider."); return; }
            depth = 1; before = Snapshot();
        }
        private void AfterTick()
        {
            if (depth != 1 || before == null) { errors.Add("Native fear controller postfix lacked its exact prefix."); depth = 0; return; }
            var after = Snapshot();
            var loss = (bool)before["conditionActive"] && !(bool)before["panicked"] && (bool)before["directlyControllable"] &&
                (bool)after["panicked"] && !(bool)after["directlyControllable"];
            var restoration = !(bool)before["conditionActive"] && (bool)before["panicked"] && !(bool)before["directlyControllable"] &&
                !(bool)after["panicked"] && (bool)after["directlyControllable"];
            if (loss) lossCount++;
            if (restoration) restorationCount++;
            if (events.Count >= 24) errors.Add("Native fear-controller observation bound exceeded.");
            else events.Add(new JObject { ["ordinal"] = events.Count + 1, ["before"] = before, ["after"] = after,
                ["lossTransition"] = loss, ["restorationTransition"] = restoration });
            before = null; depth = 0;
        }
        private void Safe(Action action)
        {
            if (disposed) return;
            try { action(); }
            catch (Exception exception) { if (errors.Count == 0) errors.Add(exception.ToString()); }
        }
        internal JObject Capture()
        {
            return new JObject { ["contract"] = "installed-unit-fear-controller-removes-and-restores-direct-control",
                ["riderId"] = rider.UniqueId, ["commandObject"] = Id(command), ["observerHooks"] = hooks.DeepClone(),
                ["events"] = events.DeepClone(), ["lossCount"] = lossCount, ["restorationCount"] = restorationCount,
                ["lossObserved"] = LossObserved, ["restorationObserved"] = RestorationObserved,
                ["complete"] = !disposed && depth == 0 && errors.Count == 0, ["errors"] = errors.DeepClone() };
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; harmony.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
        }
        private static class Hooks
        {
            internal static void Before(UnitEntityData unit)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(unit, probe.rider)) return;
                probe.Safe(probe.BeforeTick);
            }
            internal static void After(UnitEntityData unit)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(unit, probe.rider)) return;
                probe.Safe(probe.AfterTick);
            }
        }
    }
}