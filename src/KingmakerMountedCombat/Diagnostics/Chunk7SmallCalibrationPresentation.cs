using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallCalibrationScenario
    {
        private NativeCastingItemTrace presentationTrace;
        private NativeCastingOriginalSlots presentationOriginalSlots;
        private readonly List<NativeCastingItemLease> presentationItems = new List<NativeCastingItemLease>();
        private NativeCastingItemLease presentationScroll, presentationPotion, presentationCurrent;
        private UnitUseAbility presentationShell;
        private JObject presentationInput;
        private readonly JArray presentationInputs = new JArray();
        private int presentationEventOffset, presentationCostOffset;
        private bool presentationActivePose;

        private void BeginPresentationFixture()
        {
            Require(calibration.UsesPresentation && presentationTrace == null && presentationItems.Count == 0,
                "The distinct Small presentation fixture was already acquired.");
            presentationTrace = new NativeCastingItemTrace(actor, pet);
            presentationTrace.ActionBoundaryObserved = ObservePresentationActionBoundary;
            presentationOriginalSlots = new NativeCastingOriginalSlots(actor, presentationTrace);
            evidence["presentation"] = new JObject { ["contract"] = "small-native-item-presentation-v1",
                ["fixtureSetupOnly"] = true, ["spellSlotWrites"] = 0, ["nativePreparationManufactured"] = false,
                ["inputs"] = presentationInputs, ["originalSlots"] = presentationOriginalSlots.Evidence };
            // Equip only newly created items in originally empty slots. The two-unit
            // stack stays in its exact slot after one native spend, so foreign
            // quick-slot refill cannot displace another party member's item.
            presentationScroll = AcquirePresentationItem(NativeCastingItemLease.CureScroll);
            presentationPotion = AcquirePresentationItem(NativeCastingItemLease.CurePotion);
        }

        private NativeCastingItemLease AcquirePresentationItem(string blueprint)
        {
            var lease = new NativeCastingItemLease(actor, presentationTrace, blueprint, presentationOriginalSlots);
            presentationItems.Add(lease); // Retain partial acquisition for cleanup.
            lease.Acquire(2);
            return lease;
        }

        private void WoundPresentationSubject(UnitEntityData subject)
        {
            Require(subject != null && target != null && subject.Descriptor.State.IsConscious &&
                subject.Damage >= 0 && Game.Instance.Player.Difficulty.DamageToParty > 0,
                "The native healing input lacks a safe exact disposable subject.");
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            Require(!float.IsNaN(factor) && !float.IsInfinity(factor), "Native party damage multiplier is nonfinite.");
            var before = subject.Damage;
            if (before == 0)
            {
                var amount = (int)Math.Ceiling(1d / factor);
                Require(amount > 0 && amount * factor < subject.Stats.HitPoints.ModifiedValue - 2,
                    "The disposable native healing stimulus has no safe health margin.");
                Rulebook.Trigger(new RuleDealDamage(target, subject,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
            }
            Require(subject.Damage > 0 && subject.Damage < subject.Stats.HitPoints.ModifiedValue - 2,
                "The native healing target did not retain a safe, positive wound.");
            presentationInput["woundBefore"] = before;
            presentationInput["woundAtInput"] = subject.Damage;
            presentationInput["woundStimulusWasNativeRule"] = before == 0;
        }

        private void BeginPresentationInput(NativeCastingItemLease lease, UnitEntityData subject, string kind)
        {
            AssertAction();
            Require(CommandsSettled() && presentationTrace != null && lease != null && presentationCurrent == null,
                "Small presentation requires settled exact native command and item custody.");
            lease.RequireExactlyEquipped(kind, 2);
            var ability = lease.Item.Ability?.Data;
            Require(ability != null && ReferenceEquals(ability.SourceItem, lease.Item) &&
                ReferenceEquals(ability.Caster?.Unit, actor),
                "The exact native item ability does not belong to the Small rider and source item.");
            presentationCurrent = lease;
            presentationTrace.BeginCase(kind);
            presentationEventOffset = presentationTrace.EventCount;
            presentationCostOffset = trace.EventCount;
            presentationActivePose = false;
            presentationInput = new JObject { ["kind"] = kind, ["actorId"] = actor.UniqueId,
                ["targetId"] = subject.UniqueId, ["itemBlueprint"] = lease.Item.Blueprint.AssetGuid,
                ["abilityGuid"] = ability.Blueprint.AssetGuid, ["beforeItem"] = lease.Snapshot(),
                ["eventOffset"] = presentationEventOffset, ["costOffset"] = presentationCostOffset,
                ["beforeResources"] = State(), ["beforePose"] = Chunk7SmallPoseObservation.Capture(actor, pet,
                    declaration, pair.Relationship.Runtime, calibration.UsesFixtureManualField) };
            presentationInputs.Add(presentationInput);
            Game.Instance.IsPaused = true;
            WoundPresentationSubject(subject);
            Require(ability.IsAvailableForCast, "The wounded target still has no available native item ability.");
            var prior = ControlledActors.ToDictionary(value => value, value => value.Commands.Raw.Concat(value.Commands.Queue).ToArray());
            Chunk7NativeFixture.Select(actor);
            var handler = Game.Instance.SelectedAbilityHandler;
            Require(handler != null, "The native selected-ability handler is unavailable.");
            try
            {
                handler.SetAbility(ability);
                try { presentationInput["clicked"] = handler.OnClick(subject.View.gameObject, subject.Position, 0, false, false); }
                finally { handler.DropAbility(); }
            }
            finally { CaptureChangedCommands(prior); }
            var shells = issued.OfType<UnitUseAbility>().Where(value => !prior[actor].Contains(value) &&
                value.Executor == actor && ReferenceEquals(value.Spell?.Blueprint, ability.Blueprint) &&
                ReferenceEquals(value.Spell?.SourceItem, lease.Item) && value.Target?.Unit == subject).ToArray();
            Require((bool)presentationInput["clicked"] && shells.Length == 1 &&
                ControlledActors.Where(value => value != actor).All(value =>
                    value.Commands.Raw.Concat(value.Commands.Queue).SequenceEqual(prior[value])),
                "One native Small item click did not retain the exact rider shell and source item.");
            presentationShell = shells[0];
            presentationInput["shell"] = trace.ObjectIdentity(presentationShell);
            Chunk7NativeFixture.Select(originalActors[0]);
            presentationInput["selectionAfterInput"] = originalActors[0].UniqueId;
            Game.Instance.IsPaused = false;
        }

        private bool FinishPresentationInput()
        {
            Require(presentationCurrent != null && presentationShell != null, "Small presentation has no exact active native item shell.");
            if (presentationShell.IsRunning && !presentationActivePose) ObservePresentationActionBoundary(presentationShell);
            if (!CommandsSettled() || !presentationTrace.ProcessesSettled ||
                NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles()) return false;
            var events = presentationTrace.EventsSince(presentationEventOffset);
            var itemSpends = events.Children<JObject>().Where(value =>
                (string)value["kind"] == "item-spend-before" || (string)value["kind"] == "item-spend-after").ToArray();
            var beforeSpends = itemSpends.Where(value => (string)value["kind"] == "item-spend-before").ToArray();
            var afterSpends = itemSpends.Where(value => (string)value["kind"] == "item-spend-after").ToArray();
            var costs = trace.EventsSince(presentationCostOffset);
            var nativeCosts = costs.Children<JObject>().Where(value =>
                ((string)value["boundary"] == "cost-before" || (string)value["boundary"] == "cost-after") &&
                ((string)value["state"]?["actor"] == actor.UniqueId || (string)value["state"]?["actor"] == pet.UniqueId)).ToArray();
            var rootIdentity = trace.ObjectIdentity(presentationShell);
            var rootCosts = nativeCosts.Where(value => (int?)value["command"] == rootIdentity).ToArray();
            var beforeCosts = rootCosts.Where(value => (string)value["boundary"] == "cost-before").ToArray();
            var afterCosts = rootCosts.Where(value => (string)value["boundary"] == "cost-after").ToArray();
            var actionType = presentationShell.Type.ToString();
            var resource = actionType == "Standard" ? "standard" : actionType == "Move" ? "move" :
                actionType == "Swift" ? "swift" : null;
            var exactSpends = beforeSpends.Length == 1 && afterSpends.Length == 1 &&
                new[] { beforeSpends[0], afterSpends[0] }.All(value =>
                    (int?)value["identity"] == presentationTrace.Identity(presentationCurrent.Item) &&
                    (string)value["actor"] == actor.UniqueId &&
                    (string)value["blueprint"] == presentationCurrent.Item.Blueprint.AssetGuid) &&
                (int?)beforeSpends[0]["count"] == 2 && (int?)afterSpends[0]["count"] == 1 &&
                (bool?)afterSpends[0]["result"] == true;
            var exactCosts = resource != null && beforeCosts.Length == 1 && afterCosts.Length == 1 &&
                new[] { beforeCosts[0], afterCosts[0] }.All(value =>
                    (int?)value["command"] == rootIdentity &&
                    (string)value["commandType"] == typeof(UnitUseAbility).FullName &&
                    (string)value["actionType"] == actionType &&
                    (string)value["state"]?["actor"] == actor.UniqueId &&
                    (string)value["commandActor"] == actor.UniqueId && (bool?)value["acted"] == true &&
                    (bool?)value["ignoreCooldown"] == false && (bool?)value["nativeTurnBased"] == false) &&
                (int?)afterCosts[0]["sequence"] > (int?)beforeCosts[0]["sequence"] &&
                (double?)afterCosts[0]["state"]?[resource] > (double?)beforeCosts[0]["state"]?[resource] &&
                new[] { "standard", "move", "swift", "reactionCooldown", "reactions" }
                    .Where(value => value != resource).All(value => JToken.DeepEquals(
                        beforeCosts[0]["state"]?[value], afterCosts[0]["state"]?[value]));
            var childIdentity = 0;
            var exactDelivery = presentationInput.Value<string>("kind") == "scroll-friendly"
                ? ExactNativeTouchDelivery(events, nativeCosts, rootIdentity, afterCosts, out childIdentity)
                : nativeCosts.Length == 2;
            Require(presentationTrace.Complete && presentationShell.IsActed && presentationShell.IsFinished &&
                ReferenceEquals(presentationShell.Spell?.SourceItem, presentationCurrent.Item) && exactSpends &&
                presentationCurrent.IsExactlyEquipped(1) && presentationCurrent.Item.Count == 1 &&
                trace.Complete && exactCosts && exactDelivery &&
                presentationActivePose,
                "Native Small item use did not spend one exact unit and one native rider action, settle its process or show an active pose.");
            var healed = presentationShell.Target?.Unit;
            Require(healed != null && healed.UniqueId == (string)presentationInput["targetId"] &&
                healed.Damage < (int)presentationInput["woundAtInput"] && presentationShell.Result.ToString() == "Success",
                "Native Small item spell spent but did not heal its exact target successfully.");
            presentationInput["afterItem"] = presentationCurrent.Snapshot();
            presentationInput["afterResources"] = State();
            presentationInput["afterPose"] = Chunk7SmallPoseObservation.Capture(actor, pet, declaration,
                pair.Relationship.Runtime, calibration.UsesFixtureManualField);
            presentationInput["nativeEvents"] = events;
            presentationInput["nativeCosts"] = costs;
            presentationInput["nativeFollowupShell"] = childIdentity;
            presentationInput["nativeFollowupAbilityGuid"] = childIdentity == 0 ? null :
                presentationTrace.Shells.Single(value => trace.ObjectIdentity(value) == childIdentity).Spell.Blueprint.AssetGuid;
            presentationInput["nativeResult"] = presentationShell.Result.ToString();
            presentationInput["terminal"] = true;
            presentationInput["processTerminal"] = presentationShell.ExecutionProcess == null || presentationShell.ExecutionProcess.IsEnded;
            presentationInput["woundAfter"] = healed.Damage;
            presentationCurrent = null;
            presentationShell = null;
            return true;
        }
        private bool ExactNativeTouchDelivery(JArray events, JObject[] nativeCosts, int rootIdentity,
            JObject[] rootAfterCosts, out int childIdentity)
        {
            childIdentity = 0;
            var sticky = presentationShell.Spell?.Blueprint?.GetComponent<AbilityEffectStickyTouch>();
            var delivery = sticky?.TouchDeliveryAbility;
            var starts = events.Children<JObject>().Where(value => (string)value["kind"] == "start-before" &&
                (string)value["actor"] == actor.UniqueId && (int?)value["identity"] != rootIdentity).ToArray();
            if (delivery == null || starts.Length != 1 || nativeCosts.Length != 4 || rootAfterCosts.Length != 1)
                return false;
            var child = presentationTrace.Shells.SingleOrDefault(value =>
                presentationTrace.Identity(value) == (int?)starts[0]["identity"]);
            if (child == null || child == presentationShell || child.Executor != actor ||
                child.Target?.Unit != pet || child.Spell?.Blueprint != delivery ||
                child.Spell.SourceItem != null || !child.IsIgnoreCooldown || !child.IsActed ||
                !child.IsFinished || child.Result.ToString() != "Success" ||
                child.ExecutionProcess == null || !child.ExecutionProcess.IsEnded)
                return false;
            childIdentity = trace.ObjectIdentity(child);
            var exactChildIdentity = childIdentity;
            var childCosts = nativeCosts.Where(value => (int?)value["command"] == exactChildIdentity).ToArray();
            var before = childCosts.SingleOrDefault(value => (string)value["boundary"] == "cost-before");
            var after = childCosts.SingleOrDefault(value => (string)value["boundary"] == "cost-after");
            if (childCosts.Length != 2 || before == null || after == null ||
                (int?)before["sequence"] <= (int?)rootAfterCosts[0]["sequence"] ||
                (int?)after["sequence"] <= (int?)before["sequence"])
                return false;
            return new[] { before, after }.All(value =>
                    (string)value["commandType"] == typeof(UnitUseAbility).FullName &&
                    (string)value["actionType"] == child.Type.ToString() &&
                    (string)value["state"]?["actor"] == actor.UniqueId &&
                    (string)value["commandActor"] == actor.UniqueId &&
                    (bool?)value["acted"] == true && (bool?)value["ignoreCooldown"] == true &&
                    (bool?)value["nativeTurnBased"] == false) &&
                new[] { "standard", "move", "swift", "reactionCooldown", "reactions" }.All(value =>
                    JToken.DeepEquals(before["state"]?[value], after["state"]?[value]));
        }
        private void ObservePresentationActionBoundary(UnitUseAbility observed)
        {
            if (presentationActivePose || !ReferenceEquals(observed, presentationShell)) return;
            presentationInput["activePose"] = Chunk7SmallPoseObservation.Capture(actor, pet, declaration,
                pair.Relationship.Runtime, calibration.UsesFixtureManualField);
            presentationActivePose = true;
        }

        private bool ReleasePresentationFixture()
        {
            if (presentationTrace == null) return true;
            if (!CommandsSettled() || !presentationTrace.ProcessesSettled ||
                NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles()) return false;
            var failures = new List<Exception>();
            for (var i = presentationItems.Count - 1; i >= 0; i--)
                try { presentationItems[i].ReleaseOwnedItem(); } catch (Exception error) { failures.Add(error); }
            if (failures.Count != 0) throw new AggregateException("Small presentation native item custody remains.", failures);
            presentationOriginalSlots?.Restore();
            foreach (var item in presentationItems) item.Dispose();
            var result = evidence["presentation"] as JObject;
            if (result != null)
            {
                result["items"] = new JArray(presentationItems.Select(item => item.Evidence.DeepClone()));
                result["originalSlots"] = presentationOriginalSlots?.Evidence.DeepClone();
            }
            presentationTrace.Dispose();
            if (result != null) { result["castTrace"] = presentationTrace.Capture(); result["released"] = true; }
            presentationTrace = null;
            presentationCurrent = null;
            presentationShell = null;
            return true;
        }
    }
}
