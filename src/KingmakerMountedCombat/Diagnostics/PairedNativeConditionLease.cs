using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A labelled condition stimulus delivered by the existing native round fact.
    // Only the confusion D100 choice is deterministic. Native preparation,
    // command creation, timing, damage, costs and turn completion remain real.
    internal sealed class PairedNativeConditionLease : IGlobalRulebookHandler<RuleRollDice>,
        IGlobalRulebookHandler<RuleDealDamage>, IDisposable
    {
        private readonly UnitEntityData actor;
        private readonly MountedCombatController combat;
        private readonly NativeActorAllocationTrace trace;
        private readonly NativeAllocationRoundFactLease fact;
        private readonly PairedConditionRoundAction action;
        private readonly IDisposable subscription;
        private readonly int choice;
        private bool applied;
        private bool disposed;
        internal readonly JObject Evidence = new JObject { ["inputKind"] = "native-round-fact-condition-stimulus",
            ["conditionApplications"] = 0, ["choiceOverrides"] = 0, ["nativeSelfDamageRules"] = 0 };

        internal PairedNativeConditionLease(UnitEntityData actor, MountedCombatController combat,
            NativeActorAllocationTrace trace, NativeAllocationRoundFactLease fact, int choice)
        {
            if (actor.IsInCombat || actor.Descriptor.State.HasCondition(UnitCondition.Confusion) || fact.Actor != actor)
                throw new InvalidOperationException("Condition fixture requires its idle disposable actor and exact native round fact.");
            this.actor = actor; this.combat = combat; this.trace = trace; this.fact = fact; this.choice = choice;
            Evidence["actor"] = actor.UniqueId; Evidence["choice"] = choice;
            action = ScriptableObject.CreateInstance<PairedConditionRoundAction>();
            action.Owner = this;
            fact.SetDiagnosticRoundAction(action);
            Evidence["factBinding"] = fact.CaptureDiagnosticBinding(action);
            subscription = EventBus.Subscribe(this);
        }

        internal void ApplyFromNativeFact()
        {
            if (applied || disposed) return;
            Evidence["nativeFactVisits"] = (int?)Evidence["nativeFactVisits"] + 1 ?? 1;
            Evidence["preparingAtFact"] = combat.IsPreparingPairedActor(actor);
            if (!combat.IsPreparingPairedActor(actor)) return;
            if (actor.Descriptor.State.HasCondition(UnitCondition.Confusion))
                throw new InvalidOperationException("An existing condition would be overwritten by the fixture.");
            trace.Record("native-condition-fact-stimulus", actor, detail: "Confusion;D100-choice=" + choice);
            Evidence["activation"] = combat.PairedActivationIdentity;
            Evidence["frame"] = Time.frameCount;
            Evidence["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks;
            Evidence["before"] = trace.Snapshot(actor);
            actor.Descriptor.State.AddCondition(UnitCondition.Confusion);
            applied = true; Evidence["conditionApplications"] = 1;
        }
        public void OnEventAboutToTrigger(RuleRollDice evt)
        {
            if (!applied || disposed || !combat.IsPreparingPairedActor(actor) || evt.Initiator != actor ||
                evt.DiceFormula.Rolls != 1 || evt.DiceFormula.Dice != DiceType.D100) return;
            if ((int)Evidence["choiceOverrides"] != 0) throw new InvalidOperationException("Duplicate native condition choice in one grant.");
            evt.Override(choice); Evidence["choiceOverrides"] = 1;
        }
        public void OnEventDidTrigger(RuleRollDice evt) { }
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!applied || disposed || evt.Initiator != actor || evt.Target != actor) return;
            Evidence["nativeSelfDamageRules"] = (int)Evidence["nativeSelfDamageRules"] + 1;
            Evidence["nativeSelfDamage"] = evt.Damage;
        }
        public void Dispose()
        {
            if (disposed) return;
            fact.SetDiagnosticRoundAction(null);
            subscription.Dispose();
            if (applied) actor.Descriptor.State.RemoveCondition(UnitCondition.Confusion);
            Evidence["ownedConditionRestored"] = !actor.Descriptor.State.HasCondition(UnitCondition.Confusion);
            UnityEngine.Object.Destroy(action); disposed = true;
        }
    }

    internal sealed class PairedConditionRoundAction : ContextAction
    {
        internal PairedNativeConditionLease Owner;
        public override string GetCaption() => "KMC disposable native condition stimulus";
        public override void RunAction() { Owner.ApplyFromNativeFact(); }
    }
}
