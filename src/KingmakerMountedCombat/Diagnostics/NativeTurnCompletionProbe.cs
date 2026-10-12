using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Observes actual native completion entry/exit. No native writes or callback
    // invocation; callers own native input and the declared scenario contract.
    internal sealed class NativeTurnCompletionProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.TurnCompletion";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static NativeTurnCompletionProbe active;
        private HarmonyInstance harmony;
        private readonly NativeActorAllocationTrace trace;
        private readonly UnitEntityData rider, mount;
        private readonly MountedCombatController combat;
        private readonly MountedPairTurnParticipant owner;
        private readonly Dictionary<UnitEntityData, MountedPairServices> observedPairs;
        private readonly JArray calls = new JArray(), errors = new JArray(), hooks = new JArray();
        private int ordinal;
        private bool disposed, installed, installationStarted;
        internal NativeTurnCompletionProbe(NativeActorAllocationTrace trace, UnitEntityData rider, UnitEntityData mount, MountedCombatController combat)
        {
            this.trace = trace ?? throw new ArgumentNullException(nameof(trace));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            owner = (MountedPairTurnParticipant)typeof(MountedCombatController).GetField("unifiedTurn", Flags).GetValue(combat);
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeTurnCompletionProbe));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(TurnController).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new MissingMemberException("Pinned native turn completion MVID differs.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(TurnController), 0x06000C47, "ForceBefore", "After");
            Patch(typeof(TurnController), 0x06000C46, "EndBefore", "After");
            installed = true;
        }
        internal NativeTurnCompletionProbe(NativeActorAllocationTrace trace, MountedWorldService world) :
            this(trace, world.Primary.Relationship.Rider, world.Primary.Relationship.Mount, world.Primary.Combat)
        {
            observedPairs = new Dictionary<UnitEntityData, MountedPairServices>();
            foreach (var pair in world.Pairs)
                foreach (var actor in pair.Participant.ParticipationActors)
                    observedPairs.Add(actor, pair);
        }
        private MountedPairTurnParticipant OwnerFor(UnitEntityData actor) => observedPairs == null ? owner : observedPairs[actor].Participant;
        private MountedCombatController CombatFor(UnitEntityData actor) => observedPairs == null ? combat : observedPairs[actor].Combat;
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).Single(m => m.MetadataToken == token);
            harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["method"] = type.FullName + "." + method.Name, ["token"] = token.ToString("X8"),
                ["moduleMvid"] = method.Module.ModuleVersionId.ToString(), ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private JObject ForfeitState(UnitEntityData actor)
        {
            var activation = (PairedActivation<UnitEntityData, TurnController>)typeof(MountedPairTurnParticipant).GetField("activation", Flags).GetValue(OwnerFor(actor));
            var state = activation?.State(actor)?.Capture();
            return state == null ? null : new JObject { ["granted"] = state.Granted, ["prepared"] = state.Prepared,
                ["forfeitRecorded"] = state.ForfeitRecorded, ["forfeitSettled"] = state.ForfeitSettled, ["forfeitStandardAdded"] = state.ForfeitStandardAdded };
        }
        private JObject Snapshot(TurnController turn) => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = trace.EventCount, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["turnObject"] = RuntimeHelpers.GetHashCode(turn), ["actorId"] = turn.Unit.UniqueId, ["status"] = turn.Status.ToString(),
            ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["currentContext"] = ReferenceEquals(turn, Game.Instance.TurnBasedCombatController.CurrentTurn),
            ["partnerContext"] = ReferenceEquals(turn, CombatFor(turn.Unit).PairedPartnerContext), ["completionDebtOwned"] = OwnerFor(turn.Unit).OwnsCompletionDebt(turn.Unit.CombatState.Cooldown),
            ["conditionForfeitContext"] = ReferenceEquals(typeof(MountedPairTurnParticipant).GetField("nativeConditionForfeitContext", Flags).GetValue(OwnerFor(turn.Unit)), turn),
            ["forfeitState"] = ForfeitState(turn.Unit), ["pairedSequence"] = CombatFor(turn.Unit).PairedActivationSequence, ["stepMetres"] = turn.MetersMovedByFiveFootStep,
            ["stepLimit"] = TurnController.MetersOfFiveFootStep, ["resources"] = trace.Snapshot(turn.Unit)
        };
        private JObject Before(TurnController turn, string kind, bool? setCooldowns)
        {
            if (!installed || turn?.Unit == null || (observedPairs == null ? turn.Unit != rider && turn.Unit != mount : !observedPairs.ContainsKey(turn.Unit))) return null;
            try {
                if (calls.Count >= 64) throw new InvalidOperationException("Native completion observation bound exceeded.");
                trace.Record("completion-" + kind + "-before", turn.Unit, detail: "setCooldowns=" + setCooldowns, callback: turn);
                var call = new JObject { ["kind"] = kind, ["setCooldowns"] = setCooldowns,
                    ["enterOrdinal"] = ++ordinal, ["before"] = Snapshot(turn), ["complete"] = false };
                calls.Add(call); return call;
            } catch (Exception exception) { errors.Add(exception.ToString()); return null; }
        }
        private void After(TurnController turn, JObject call)
        {
            if (call == null) return;
            try { trace.Record("completion-" + (string)call["kind"] + "-after", turn.Unit, detail: "setCooldowns=" + (string)call["setCooldowns"], callback: turn); call["after"] = Snapshot(turn); call["exitOrdinal"] = ++ordinal; call["complete"] = true; }
            catch (Exception exception) { errors.Add(exception.ToString()); }
        }
        internal JObject Capture() => new JObject { ["contract"] = "observed-native-turn-completion-writes",
            ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId,
            ["observedActorIds"] = new JArray(observedPairs == null ? new[] { rider.UniqueId, mount.UniqueId } : observedPairs.Keys.Select(actor => actor.UniqueId)), ["observerHooks"] = hooks.DeepClone(),
            ["calls"] = calls.DeepClone(), ["errors"] = CaptureErrors(), ["traceComplete"] = installed && trace.Complete };
        private JArray CaptureErrors()
        {
            var result = (JArray)errors.DeepClone();
            if (!installed) result.Add("Native observer installation did not complete.");
            return result;
        }
        private static class Hooks
        {
            internal static void ForceBefore(TurnController __instance, bool setCooldowns, out JObject __state) { __state = active?.Before(__instance, "force-to-end", setCooldowns); }
            internal static void EndBefore(TurnController __instance, out JObject __state) { __state = active?.Before(__instance, "end", null); }
            internal static void After(TurnController __instance, JObject __state) { active?.After(__instance, __state); }
        }
        public void Dispose() { if (disposed) return; harmony?.UnpatchAll(HarmonyId); disposed = true; if (ReferenceEquals(active, this)) active = null; }
    }
}
