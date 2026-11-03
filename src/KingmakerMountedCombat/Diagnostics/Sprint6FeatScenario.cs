using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // FEAT-01 rows. The feat comes through the native level-up controller. Attacks are the hostile's native UnitAttack, the native engagement
    // controller's attack of opportunity, or a plain native RuleAttackWithWeapon where the row needs a hit in an exact phase; each
    // rolls natively. The check is native, with its d20 replaced only here, so success, equality and failure are exact without
    // rerolling for a convenient die. The reader owns PASS/FAIL.
    internal sealed class Sprint6RuleObserver : IDisposable, IAttackHandler,
        IGlobalRulebookHandler<RuleAttackRoll>, IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleSkillCheck>
    {
        private readonly HashSet<UnitEntityData> watched;
        private readonly IDisposable subscription;
        private readonly Dictionary<RuleAttackRoll, JObject> rows = new Dictionary<RuleAttackRoll, JObject>();
        internal readonly JArray Attacks = new JArray();
        internal readonly JArray SkillChecks = new JArray();
        internal Sprint6RuleObserver(params UnitEntityData[] units) { watched = new HashSet<UnitEntityData>(units); subscription = EventBus.Subscribe(this); }
        internal void Watch(UnitEntityData unit) => watched.Add(unit);
        public void Dispose() => subscription.Dispose();
        public void HandleAttackHitRoll(RuleAttackRoll rule)
        {
            if (!watched.Contains(rule.Target)) return;
            JObject row;
            if (!rows.TryGetValue(rule, out row))
            {
                row = new JObject { ["identity"] = RuntimeHelpers.GetHashCode(rule), ["attacker"] = rule.Initiator.UniqueId, ["target"] = rule.Target.UniqueId, ["frame"] = Time.frameCount,
                    ["d20"] = (int)rule.Roll, ["bonus"] = rule.AttackBonus, ["ac"] = rule.TargetAC, ["autoHit"] = rule.AutoHit, ["opportunity"] = rule.RuleAttackWithWeapon?.IsAttackOfOpportunity == true, ["attackerCommand"] = rule.Initiator.Commands.Standard?.GetType().Name,
                    ["resultAtEvent"] = rule.Result.ToString(), ["isHitAtEvent"] = rule.IsHit, ["criticalAtEvent"] = rule.IsCriticalConfirmed, ["sneakAtEvent"] = rule.IsSneakAttack, ["damageRules"] = 0, ["damage"] = 0 };
                rows[rule] = row; if (Attacks.Count < 256) Attacks.Add(row);
            }
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt) { JObject row; if (rows.TryGetValue(evt, out row)) { row["resultAfterRule"] = evt.Result.ToString(); row["isHitAfterRule"] = evt.IsHit; } }
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            JObject row;
            if (evt.AttackRoll != null && rows.TryGetValue(evt.AttackRoll, out row)) { row["damageRules"] = (int)row["damageRules"] + 1; row["targetDamageBefore"] = evt.Target.Damage; }
        }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            JObject row;
            if (evt.AttackRoll != null && rows.TryGetValue(evt.AttackRoll, out row)) row["damage"] = evt.Target.Damage - (int)row["targetDamageBefore"];
        }
        public void OnEventAboutToTrigger(RuleSkillCheck evt) { }
        public void OnEventDidTrigger(RuleSkillCheck evt)
        {
            if (evt.StatType != StatType.SkillMobility || !watched.Contains(evt.Initiator) || SkillChecks.Count >= 64) return;
            SkillChecks.Add(new JObject { ["actor"] = evt.Initiator.UniqueId, ["d20"] = (int)evt.D20, ["stat"] = evt.StatValue, ["bonus"] = (int)evt.Bonus, ["result"] = evt.RollResult, ["dc"] = evt.DC, ["passed"] = evt.IsPassed, ["frame"] = Time.frameCount });
        }
    }

    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint6FeatRealTime = "sprint6-feat-rt", Sprint6FeatTurnBased = "sprint6-feat-tb", Sprint6FeatForcedHit = "sprint6-feat-forced-rt";
        internal static bool IsSprint6FeatScenario(string scenario) => scenario == Sprint6FeatRealTime || scenario == Sprint6FeatTurnBased || scenario == Sprint6FeatForcedHit;
        private bool IsSprint6Feat => IsSprint6FeatScenario(request.Scenario);
        private bool Sprint6Tb => request.Scenario == Sprint6FeatTurnBased;
        internal const int Sprint6SchemaVersion = 50;
        internal const double Sprint6ScenarioDeadlineSeconds = 300.0d, Sprint6HostDeadlineSeconds = 460.0d;
        internal static readonly string[] Sprint6RealTimeCases = { "S6-selection", "S6-rt-defense", "S6-rt-isolation" };
        internal static readonly string[] Sprint6TurnBasedCases = { "S6-rider-feat", "S6-tb-economy" };
        internal static readonly string[] Sprint6ForcedHitCases = { "S6-rider-feat", "S6-forced-hit" };
        private bool Sprint6Forced => request.Scenario == Sprint6FeatForcedHit;
        private string[] Sprint6Cases => Sprint6Forced ? Sprint6ForcedHitCases : Sprint6Tb ? Sprint6TurnBasedCases : Sprint6RealTimeCases;
        private string Sprint6Case => Sprint6Cases[s6CaseIndex];
        private const string Sprint6FighterClassGuid = "48ac8db94d5de7645906c7d0ad3bcfbd";
        private const int Sprint6SettleFrames = 30;
        private const float Sprint6StepBoundSeconds = 45f;

        private int s6CaseIndex, s6Stage, s6Step, s6Frames, s6StrikesThisStep, s6AttemptsAtStep, s6BoundStep = -1;
        private float s6BoundAt;
        private JObject s6Facts, s6Measure;
        private JArray s6Records;
        private UnitEntityData s6Owner, s6OwnerPet, s6Recipient;
        private Sprint6RuleObserver s6Rules;
        private MountedCombatDefenseService s6Service;
        private string s6NextPlan;
        private TurnController s6Turn;
        private int s6OpportunityFrame;
        private const int Sprint6OpportunityFrames = 20, Sprint6HostileAttackSetup = 20;

        private void BeginSprint6Feat()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 6 rows require the accepted single paired authority.");
            s6Service = MountedCombatDefenseService.Current ?? throw new InvalidOperationException("The Mounted Combat service is unavailable.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            s6Records = new JArray();
            s6Rules = new Sprint6RuleObserver(rider, horse);
            s6Service.Observed += Sprint6Observed;
            s6Service.ScenarioD20 = Sprint6ForcedD20;
            s6Measure = new JObject { ["contract"] = "native-mounted-combat-feat-v1", ["mode"] = Sprint6Tb ? "TB" : "RT", ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId,
                ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(Sprint6Cases), ["records"] = s6Records, ["attackRules"] = s6Rules.Attacks, ["skillChecks"] = s6Rules.SkillChecks,
                ["checkD20IsControlled"] = true, ["experienceIsDisclosedSetup"] = true,
                ["policy"] = "The feat comes through the native level-up controller; each attack rolls natively; a row's forced d20 sets only the check's die." };
            observations["sprint6Feat"] = s6Measure;
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            if (Sprint6Tb) castingRod = AcquireCastingItem(NativeCastingItemLease.LesserQuickenRod);
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence));
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureSprint6DeadlineProgress() => new JObject { ["stage"] = s6Stage, ["step"] = s6Step, ["caseIndex"] = s6CaseIndex,
            ["case"] = s6CaseIndex < Sprint6Cases.Length ? Sprint6Case : null, ["plan"] = s6NextPlan, ["facts"] = s6Facts?.DeepClone(), ["records"] = s6Records?.DeepClone(), ["state"] = Sprint6State(rider) };

        private bool DrainSprint6Fixture()
        {
            if (s6Service != null) { s6Service.ScenarioD20 = null; s6Service.Observed -= Sprint6Observed; s6Service = null; }
            if (s6Rules != null) { s6Rules.Dispose(); s6Rules = null; }
            var second = s6Owner == null ? null : relationship.World.Registry.FindActive(s6Owner)?.Boundary;
            if (second?.State == RelationshipState.Mounted)
            {
                if (s6Owner.Commands.Empty) Sprint5Fixture.Click(nativeControls, s6Owner, nativeControls.DismountAbility, s6Owner);
                return false;
            }
            return DrainSprint4Fixture();
        }

        // The d20 that makes the check's result land exactly where the step's plan says. Null lets the die roll.
        private int? Sprint6ForcedD20(MountedCombatDefenseRecord record)
        {
            if (s6NextPlan == null || s6NextPlan == "natural") return null;
            var delta = s6NextPlan == "success" ? 1 : s6NextPlan == "equal" ? 0 : -1;
            return record.AttackResult + delta - RiderOf(record).Stats.GetStat(StatType.SkillMobility).ModifiedValue;
        }

        private UnitEntityData RiderOf(MountedCombatDefenseRecord record) => record.Rider == rider.UniqueId ? rider : s6Owner;

        private void Sprint6Observed(MountedCombatDefenseRecord r)
        {
            s6Records.Add(new JObject { ["case"] = s6CaseIndex < Sprint6Cases.Length ? Sprint6Case : null, ["step"] = s6Step, ["plan"] = s6NextPlan, ["ruleIdentity"] = RuntimeHelpers.GetHashCode(r.Rule),
                ["rider"] = r.Rider, ["mountId"] = r.Mount, ["attacker"] = r.Attacker, ["weapon"] = r.Weapon, ["mode"] = r.Mode, ["round"] = r.Round, ["frame"] = r.Frame,
                ["attackD20"] = r.AttackD20, ["attackBonus"] = r.AttackBonus, ["attackResult"] = r.AttackResult, ["targetAc"] = r.TargetAc, ["resultBefore"] = r.ResultBefore, ["resultAfter"] = r.ResultAfter,
                ["criticalBefore"] = r.CriticalBefore, ["criticalAfter"] = r.CriticalAfter, ["opportunity"] = r.AttackOfOpportunity, ["riderTurn"] = r.RiderTurn,
                ["abilityGuid"] = r.AbilityGuid, ["forcedHitContext"] = r.ForcedHitContext, ["refusal"] = r.Refusal.ToString(), ["commitment"] = r.Commitment.ToString(), ["attempted"] = r.Attempted, ["forced"] = r.Forced, ["negated"] = r.Negated,
                ["checkD20"] = r.CheckD20, ["checkStat"] = r.CheckStat, ["checkBonus"] = r.CheckBonus, ["checkResult"] = r.CheckResult, ["checkDc"] = r.CheckDc,
                ["swiftBefore"] = r.SwiftBefore, ["swiftAfter"] = r.SwiftAfter, ["stateBefore"] = Sprint6StateOf(r.StateBefore), ["stateAfter"] = Sprint6StateOf(r.StateAfter),
                ["mountSwiftBefore"] = r.MountSwiftBefore, ["mountSwiftAfter"] = r.MountSwiftAfter, ["mountDamageAtRecord"] = horse.Damage });
            ResetLeafClock();
        }

        private static JObject Sprint6StateOf(MountedCombatDefenseState s) => new JObject { ["spent"] = s.Spent, ["owed"] = s.Owed, ["window"] = s.Window };

        private JObject Sprint6State(UnitEntityData unit)
        {
            if (unit == null || s6Service == null) return null;
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var state = Sprint6StateOf(s6Service.ReadState(unit));
            state["swift"] = unit.CombatState.Cooldown.SwiftAction; state["hasSwift"] = unit.HasSwiftAction(); state["standard"] = unit.CombatState.Cooldown.StandardAction; state["move"] = unit.CombatState.Cooldown.MoveAction;
            state["swiftGateBlocked"] = unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift);
            state["turnUnit"] = turn?.Unit?.UniqueId; state["turnStatus"] = turn?.Status.ToString(); state["round"] = Game.Instance.TurnBasedCombatController.RoundNumber; state["frame"] = Time.frameCount;
            state["inCombat"] = unit.IsInCombat; state["hp"] = unit.HPLeft;
            state["charges"] = s6Service.SwiftChargeCount; state["attempts"] = s6Service.AttemptCount; state["negated"] = s6Service.NegatedCount;
            var window = unit.Descriptor.Buffs.GetFact(horseService.MountedCombatWindowBuff) as Kingmaker.UnitLogic.Buffs.Buff;
            state["windowEndTicks"] = window?.EndTime.Ticks; state["windowIdentity"] = window == null ? 0 : RuntimeHelpers.GetHashCode(window);
            return state;
        }

        private JObject Sprint6Step(string name)
        {
            var record = new JObject { ["rider"] = Sprint6State(rider), ["mountSwift"] = horse.CombatState.Cooldown.SwiftAction, ["mountDamage"] = horse.Damage, ["frame"] = Time.frameCount };
            ((JObject)s6Facts["steps"])[name] = record;
            return record;
        }

        private void FinishSprint6Row(bool passed, string detail)
        {
            s6Facts["after"] = Sprint6State(rider);
            AddRow(Sprint6Case, passed, detail, s6Facts);
            s6CaseIndex++; s6Stage = s6CaseIndex < Sprint6Cases.Length ? 1 : 3; s6Step = 0; s6NextPlan = null;
            if (s6CaseIndex == Sprint6Cases.Length || !passed) { BeginCleanup(); return; }
            ResetLeafClock();
        }

        private void FailSprint6Row(string why) { s6Facts["failure"] = why; FinishSprint6Row(false, why); }

        private void TickSprint6Feat()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            CastingMeasurement["progress"] = new JObject { ["stage"] = s6Stage, ["caseIndex"] = s6CaseIndex, ["step"] = s6Step, ["plan"] = s6NextPlan };
            if (s6Stage == 0 && s6Facts == null) s6Facts = new JObject { ["case"] = Sprint6Case, ["steps"] = new JObject() };
            if (s6Stage == 0) { TickSprint6Acquire(); return; }
            if (s6Stage == 1)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "sprint6-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                s6Facts = new JObject { ["case"] = Sprint6Case, ["steps"] = new JObject() };
                s6Step = 0; s6BoundStep = -1; s6Stage = 2; ResetLeafClock(); return;
            }
            // Each step of a row that keeps attacking restarts the leaf clock, so each has its own bound.
            if (s6Step != s6BoundStep) { s6BoundStep = s6Step; s6BoundAt = Time.time; s6StrikesThisStep = 0; s6AttemptsAtStep = s6Records.Count; }
            else if (Time.time - s6BoundAt > Sprint6StepBoundSeconds) { FailSprint6Row("Step " + s6Step + " exceeded its bound."); return; }
            targetService?.ObserveTargetLifeState(); targetService?.RefreshBidirectionalCombatMemoryLease();
            // Setup only: the mount stands for many measured hits.
            if (horse.HPLeft < horse.MaxHP / 2) horse.Damage = 0;
            switch (Sprint6Case)
            {
                case "S6-rt-defense": TickSprint6RealTimeDefense(); return;
                case "S6-rt-isolation": TickSprint6RealTimeIsolation(); return;
                case "S6-tb-economy": TickSprint6TurnBased(turn); return;
                case "S6-forced-hit": TickSprint6ForcedHit(); return;
                default: throw new InvalidOperationException("Unknown Sprint 6 case " + Sprint6Case);
            }
        }

        // ---- acquisition ---------------------------------------------------------------------------------------------
        private void TickSprint6Acquire()
        {
            switch (s6Step)
            {
                case 0:
                    if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                    Sprint6Inventory();
                    if (horseService.MountedCombatFeature == null) { FailSprint6Row("The feat did not register: " + horseService.MountedCombatFailure); return; }
                    if (Sprint6Case == "S6-rider-feat") { s6Step = 3; return; }
                    s6Owner = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).Where(member =>
                        member != null && member != rider && !member.Descriptor.IsPet && member.Descriptor.Pet == null && member.IsInGame && member.Descriptor.State.IsConscious &&
                        member.Descriptor.State.Size == Size.Medium && member.Descriptor.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid)) == 0)
                        .OrderBy(member => member.Stats.SkillMobility.BaseValue).FirstOrDefault();
                    if (s6Owner == null) { FailSprint6Row("No Medium party member without a companion can make the ordinary choice."); return; }
                    // Setup on the disposable fixture: the choice is measured from zero trained ranks.
                    s6Facts["ownerRanksSetup"] = new JObject { ["unit"] = s6Owner.UniqueId, ["ranksBefore"] = s6Owner.Stats.SkillMobility.BaseValue };
                    s6Owner.Stats.SkillMobility.BaseValue = 0;
                    s6Rules.Watch(s6Owner);
                    s6Step = 1; return;
                case 1:
                    // The ordinary list is offered on odd character levels.
                    Sprint6EnsureLevels(s6Owner, 6);
                    var plain = new JArray(); s6Facts["ordinaryPreparation"] = plain;
                    if ((s6Owner.Descriptor.Progression.CharacterLevel + 1) % 2 == 0) plain.Add(Sprint6CommitLevel(s6Owner, null, 0));
                    s6Facts["noCompanionAtChoice"] = s6Owner.Descriptor.Pet == null;
                    s6Facts["ordinary"] = Sprint6CommitLevel(s6Owner, "BasicFeatSelection", 20);
                    if (s6Facts["ordinary"].Value<bool>("committed") != true || s6Owner.Descriptor.Progression.Features.GetFact(horseService.MountedCombatFeature) == null) { FailSprint6Row("The ordinary native choice did not commit the feat."); return; }
                    s6Step = 2; return;
                case 2:
                    var acquisition = new JObject(); s6Facts["ownerCompanion"] = acquisition;
                    var acquired = NativeHorseFixtureAcquisition.Create(s6Owner, horseService, logger, acquisition);
                    acquisition["acquired"] = acquired;
                    if (!acquired) { FailSprint6Row("The second pair's native companion did not commit."); return; }
                    s6Frames = 0; s6Step = 3; return;
                case 3:
                    Sprint6EnsureLevels(rider, 1);
                    s6Facts["riderCombatChoice"] = Sprint6CommitLevel(rider, "FighterFeatSelection", 0);
                    if (s6Facts["riderCombatChoice"].Value<bool>("committed") != true || rider.Descriptor.Progression.Features.GetFact(horseService.MountedCombatFeature) == null) { FailSprint6Row("The combat-bonus native choice did not commit the feat."); return; }
                    var toggle = rider.Descriptor.ActivatableAbilities.GetFact(horseService.MountedCombatToggle) as ActivatableAbility;
                    s6Facts["riderToggle"] = new JObject { ["present"] = toggle != null, ["on"] = toggle?.IsOn };
                    s6Facts["riderMobility"] = new JObject { ["ranks"] = rider.Stats.SkillMobility.BaseValue, ["total"] = rider.Stats.SkillMobility.ModifiedValue };
                    s6Frames = 0; s6Step = 4; return;
                default:
                    if (++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "The feat was chosen through the native controller.");
                    return;
            }
        }

        private void Sprint6Inventory()
        {
            s6Facts["registration"] = new JObject { ["failure"] = horseService.MountedCombatFailure, ["state"] = horseService.State.ToString(),
                ["groups"] = new JArray(horseService.MountedCombatFeature?.Groups.Select(group => group.ToString()) ?? new string[0]), ["ranks"] = horseService.MountedCombatFeature?.Ranks,
                ["prerequisite"] = horseService.MountedCombatFeature?.ComponentsArray.OfType<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteStatValue>().Select(p => p.Stat + "=" + p.Value).FirstOrDefault() };
            s6Facts["selections"] = new JArray(horseService.MountedCombatSelections.Select(selection => new JObject { ["name"] = selection.name, ["guid"] = selection.AssetGuid, ["count"] = selection.AllFeatures.Length,
                ["lastIsFeat"] = ReferenceEquals(selection.AllFeatures.Last(), horseService.MountedCombatFeature), ["occurrences"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, horseService.MountedCombatFeature)) }));
        }

        private void Sprint6EnsureLevels(UnitEntityData owner, int levels)
        {
            var progression = owner.Descriptor.Progression;
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + levels);
            if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
        }

        // One native level. When a selection is named, the feat is refused before a trained rank and accepted after one.
        private JObject Sprint6CommitLevel(UnitEntityData unit, string selectionName, int disclosedBonus)
        {
            var fighter = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Sprint6FighterClassGuid);
            var record = new JObject { ["unit"] = unit.UniqueId, ["selection"] = selectionName, ["characterLevelBefore"] = unit.Descriptor.Progression.CharacterLevel, ["committed"] = false, ["canLevelUp"] = LevelUpController.CanLevelUp(unit.Descriptor) };
            if (record.Value<bool>("canLevelUp") != true) return record;
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            var choices = new JArray(); record["choices"] = choices;
            LevelUpController controller = null;
            var committed = false;
            var bonus = disclosedBonus == 0 ? null : unit.Stats.SkillMobility.AddModifier(disclosedBonus, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Competence);
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                controller = LevelUpController.StartWithoutAssigningStaticInstance(unit.Descriptor);
                if (!controller.SelectClass(fighter)) { record["refused"] = "The native controller refused the class."; return record; }
                while (controller.State.AttributePoints > 0) { if (!controller.SpendAttributePoint(StatType.Strength)) break; choices.Add("attribute:Strength"); }
                if (selectionName != null)
                {
                    var selectionState = controller.State.Selections.FirstOrDefault(state => (state.Selection as BlueprintScriptableObject)?.name == selectionName && !state.Selected);
                    if (selectionState == null) { record["refused"] = "The named selection was not offered."; return record; }
                    var selection = (BlueprintFeatureSelection)selectionState.Selection;
                    var item = selection.Items.Single(candidate => ReferenceEquals(candidate.Feature, horseService.MountedCombatFeature));
                    record["offeredLast"] = ReferenceEquals(selection.AllFeatures.Last(), horseService.MountedCombatFeature);
                    record["preview"] = Sprint6Mobility(controller.Preview);
                    record["eligibleBeforeRank"] = selection.CanSelect(controller.Preview, controller.State, selectionState, item);
                    if (controller.Preview.Stats.SkillMobility.BaseValue == 0)
                    {
                        record["selectAcceptedBeforeRank"] = controller.SelectFeature(selectionState, item);
                        if (!controller.SpendSkillPoint(StatType.SkillMobility)) { record["refused"] = "The native controller refused the Mobility rank."; return record; }
                        choices.Add("skill:SkillMobility");
                    }
                    record["previewAfterRank"] = Sprint6Mobility(controller.Preview);
                    record["eligibleAfterRank"] = selection.CanSelect(controller.Preview, controller.State, selectionState, item);
                    if (!controller.SelectFeature(selectionState, item)) { record["refused"] = "The native controller refused the trained choice."; return record; }
                    choices.Add("selection:" + selectionName + "=MountedCombat");
                }
                NativeHorseFixtureAcquisition.SpendPetSkills(controller, fighter, choices);
                for (var pass = 0; pass < 12 && NativeHorseFixtureAcquisition.MakeNextPetSelection(controller, choices); pass++) { }
                NativeHorseFixtureAcquisition.SpendPetSkills(controller, fighter, choices);
                record["complete"] = controller.State.IsComplete();
                if (controller.State.IsComplete() != true) { record["refused"] = "The native level was not complete."; return record; }
                controller.Commit();
                committed = true; record["committed"] = true;
            }
            finally
            {
                if (controller != null && !committed) controller.Cancel();
                SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup;
                bonus?.Remove();
            }
            record["characterLevelAfter"] = unit.Descriptor.Progression.CharacterLevel;
            record["after"] = Sprint6Mobility(unit.Descriptor);
            record["fact"] = unit.Descriptor.Progression.Features.GetFact(horseService.MountedCombatFeature) != null;
            return record;
        }

        private static JObject Sprint6Mobility(UnitDescriptor unit) => new JObject { ["ranks"] = unit.Stats.SkillMobility.BaseValue, ["total"] = unit.Stats.SkillMobility.ModifiedValue };

        // ---- strikes ------------------------------------------------------------------------------------------------
        private bool Sprint6Command(UnitEntityData victim)
        {
            if (!target.Commands.Empty || target.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard)) return false;
            target.Commands.Run(new UnitAttack(victim) { CreatedByPlayer = true });
            s6StrikesThisStep++;
            return true;
        }

        private void Sprint6RuleStrike(UnitEntityData victim)
        {
            var weapon = target.GetThreatHand()?.Weapon ?? target.Body.EmptyHandWeapon;
            Rulebook.Trigger(new RuleAttackWithWeapon(target, victim, weapon, 0));
            s6StrikesThisStep++;
        }

        // The native engagement controller's own attack of opportunity where the hostile is engaged, then a plain rule strike if
        // that one missed or was not delivered, so a step always reaches a hit in its exact phase.
        private void Sprint6OpportunityStrike(UnitEntityData victim)
        {
            if (s6StrikesThisStep == 0 && target.CombatState.IsEngage(victim))
            {
                Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(target, victim);
                s6OpportunityFrame = Time.frameCount; s6StrikesThisStep++;
                return;
            }
            if (Time.frameCount - s6OpportunityFrame < Sprint6OpportunityFrames) return;
            Sprint6RuleStrike(victim);
        }

        // The latest defense record since this step began, once a hit produced one.
        private JObject Sprint6RecordSinceStep() => s6Records.Count > s6AttemptsAtStep ? (JObject)s6Records.Last : null;

        private void Sprint6SpawnTarget(string suffix)
        {
            BeginTarget(3.5f, suffix); ruleProbe.Arm(target, false);
            // Setup only: the fixture hostile hits AC 23 on a natural 20 alone (r04). The die still rolls and still decides.
            target.Stats.AdditionalAttackBonus.AddModifier(Sprint6HostileAttackSetup, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Competence);
            s6Measure["hostileAttackSetup"] = Sprint6HostileAttackSetup;
            s6Rules.Watch(target);
            castingCosts.BeginEncounter(request.RunId + ":" + suffix);
        }

        private bool Sprint6RealTimeUsageFree(UnitEntityData unit)
        {
            var state = s6Service.ReadState(unit);
            return !state.Any && unit.HasSwiftAction();
        }

        // ---- real time ----------------------------------------------------------------------------------------------
        private void TickSprint6RealTimeDefense()
        {
            switch (s6Step)
            {
                // The second pair mounts in exploration, the ordinary way, once the first pair's AI isolation holds (r05).
                case 0: s6Frames = 0; s6Step = 20; return;
                case 20:
                    s6OwnerPet = s6Owner.Descriptor.Pet;
                    if (s6OwnerPet == null || !s6OwnerPet.IsInState || s6OwnerPet.View?.AgentASP == null || !s6Owner.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    s6Rules.Watch(s6OwnerPet);
                    if (Sprint5Fixture.Click(nativeControls, s6Owner, nativeControls.MountAbility, s6OwnerPet)) { s6Frames = 0; s6Step = 21; ResetLeafClock(); }
                    return;
                case 21:
                    if (relationship.World.Registry.FindActive(s6Owner)?.Boundary?.State != RelationshipState.Mounted || ++s6Frames < Sprint6SettleFrames) return;
                    Sprint6SpawnTarget("s6-rt"); s6Step = 1; return;
                case 1: if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return; s6NextPlan = "success"; s6Step = 2; return;
                // A hostile's ordinary attack, first legal hit: the check succeeds and the hit is negated.
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("negated"); s6NextPlan = "natural"; s6Step = 3; return;
                // A further hit in the same round is refused without a roll, and stands.
                case 3:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    Sprint6Step("sameRoundRefused"); s6Step = 4; return;
                case 4: if (!Sprint6RealTimeUsageFree(rider)) return; Sprint6Step("renewedByNativeWindow"); s6NextPlan = "equal"; s6Step = 5; return;
                case 5:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("equal"); s6Step = 6; return;
                case 6: if (!Sprint6RealTimeUsageFree(rider)) return; s6NextPlan = "low"; s6Step = 7; return;
                case 7:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("low"); s6Step = 8; return;
                case 8: if (!Sprint6RealTimeUsageFree(rider)) return; s6NextPlan = "natural"; s6Step = 9; return;
                case 9:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("naturalDie");
                    // The preference never touches a running use.
                    Sprint6SetPreference(false, "off-during-use"); Sprint6SetPreference(true, "on-during-use");
                    Sprint6Step("preferenceKeptUse"); s6Step = 10; return;
                case 10: if (!Sprint6RealTimeUsageFree(rider)) return; Sprint6SetPreference(false, "off"); s6NextPlan = "natural"; s6Step = 11; return;
                case 11:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("preferenceOffRefused"); Sprint6SetPreference(true, "on"); s6NextPlan = "success"; s6Step = 12; return;
                case 12:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("preferenceOnAttempted"); s6Frames = 0; s6Step = 13; return;
                default:
                    if (!target.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Real-time defense observations recorded.");
                    return;
            }
        }

        private void Sprint6SetPreference(bool on, string name)
        {
            var toggle = (ActivatableAbility)rider.Descriptor.ActivatableAbilities.GetFact(horseService.MountedCombatToggle);
            toggle.IsOn = on;
            ((JObject)s6Facts["steps"])["preference-" + name] = new JObject { ["on"] = toggle.IsOn, ["running"] = toggle.IsRunning, ["state"] = Sprint6State(rider) };
        }

        private void TickSprint6RealTimeIsolation()
        {
            switch (s6Step)
            {
                case 0: if (!Sprint6RealTimeUsageFree(rider)) return; s6Facts["attemptsBeforeUnrelated"] = s6Records.Count; s6NextPlan = "natural"; s6Frames = 0; s6Step = 1; return;
                // Attacks on the rider and on an unrelated party member make no attempt.
                case 1:
                    s6Recipient = allocationFixtureParty.FirstOrDefault(member => member != s6Owner && member != rider && member.IsInState && member.Descriptor.State.IsConscious && !member.Descriptor.IsPet);
                    if (s6Recipient != null) { s6Rules.Watch(s6Recipient); Sprint6RuleStrike(s6Recipient); s6Recipient.Damage = 0; }
                    Sprint6RuleStrike(rider); rider.Damage = 0;
                    if (++s6Frames < 4) return;
                    s6Facts["attemptsAfterUnrelated"] = s6Records.Count; s6Facts["unrelated"] = s6Recipient?.UniqueId; Sprint6Step("unrelated"); s6Step = 2; return;
                // An incapacitated rider cannot react.
                case 2:
                    rider.Descriptor.State.AddCondition(UnitCondition.Stunned); s6Facts["stunApplied"] = !rider.Descriptor.State.CanAct; s6Step = 3; return;
                case 3:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    rider.Descriptor.State.RemoveCondition(UnitCondition.Stunned); Sprint6Step("incapacitatedRefused"); s6Step = 4; return;
                // Neither can a flat-footed rider, by the native flat-footed rule.
                case 4:
                    if (!rider.Descriptor.State.CanAct) return;
                    rider.Descriptor.State.AddCondition(UnitCondition.LoseDexterityToAC); s6Step = 5; return;
                case 5:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    rider.Descriptor.State.RemoveCondition(UnitCondition.LoseDexterityToAC); Sprint6Step("flatFootedRefused"); s6Step = 6; return;
                // The second pair, independent of the first.
                case 6:
                    if (relationship.World.Registry.FindActive(s6Owner)?.Boundary?.State != RelationshipState.Mounted) { FailSprint6Row("The second pair is no longer mounted."); return; }
                    s6Frames = 0; s6Step = 7; return;
                case 7:
                    if (++s6Frames < Sprint6SettleFrames || !Sprint6RealTimeUsageFree(rider) || !s6Owner.CombatState.CanActInCombat) return;
                    s6Facts["secondPair"] = new JObject { ["rider"] = s6Owner.UniqueId, ["mount"] = s6OwnerPet.UniqueId, ["ownerStateBefore"] = Sprint6State(s6Owner) };
                    s6NextPlan = "success"; s6Step = 8; return;
                case 8:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("firstRiderUsed");
                    s6Facts["ownerAfterFirstRider"] = Sprint6State(s6Owner); s6Step = 9; return;
                case 9:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(s6OwnerPet); return; }
                    s6Facts["ownerAfterOwnUse"] = Sprint6State(s6Owner); Sprint6Step("secondRiderUsed"); s6Step = 10; return;
                // A dismount leaves the same running use. The rider's state is on the rider.
                case 10:
                    if (!rider.Commands.Empty) return;
                    s6Facts["beforeDismount"] = Sprint6State(rider);
                    if (Sprint5Fixture.Click(nativeControls, rider, nativeControls.DismountAbility, rider)) { s6Frames = 0; s6Step = 11; ResetLeafClock(); }
                    return;
                case 11:
                    if (relationship.State == RelationshipState.Mounted) return;
                    s6Facts["afterDismount"] = Sprint6State(rider); s6Facts["attemptsAtDismount"] = s6Records.Count; s6Step = 12; return;
                case 12:
                    Sprint6RuleStrike(horse); s6Facts["attemptsAfterDismountedStrike"] = s6Records.Count; s6Step = 13; return;
                case 13:
                    if (!rider.Commands.Empty) return;
                    if (rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move)) return;
                    if (Sprint5Fixture.Click(nativeControls, rider, nativeControls.MountAbility, horse)) { s6Frames = 0; s6Step = 14; ResetLeafClock(); }
                    return;
                case 14:
                    if (relationship.State != RelationshipState.Mounted) return;
                    s6Facts["afterRemount"] = Sprint6State(rider); s6Frames = 0; s6Step = 15; return;
                default:
                    if (++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Isolation observations recorded.");
                    return;
            }
        }

        // ---- forced hit -------------------------------------------------------------------------------------------------
        // One ranged touch ability cast twice by the hostile: as an ordinary cast, and with UnitUseAbility.ForceAlwaysHit as
        // CommandUnitCastSpell sets it. Melee success and equality stand beside them as the unchanged control.
        private BlueprintAbility s6Ray;
        private float s6CastAt;

        private static BlueprintAbility Sprint6FindRay()
        {
            var rays = ResourcesLibrary.LibraryObject.GetAllBlueprints().OfType<BlueprintAbility>().Where(ability =>
            {
                var delivery = ability.GetComponent<AbilityDeliverProjectile>();
                return delivery != null && delivery.NeedAttackRoll && delivery.Type == AbilityProjectileType.Simple && !delivery.IsHandOfTheApprentice && delivery.Weapon != null;
            }).OrderBy(ability => ability.name, StringComparer.Ordinal).ToArray();
            return rays.FirstOrDefault(ability => ability.name == "RayOfFrost") ?? rays.FirstOrDefault();
        }

        private void Sprint6CastRay(bool forced)
        {
            var cast = new UnitUseAbility(new AbilityData(s6Ray, target.Descriptor), new TargetWrapper(horse)) { NeedLoS = false, ForceAlwaysHit = forced };
            cast.IgnoreCooldown();
            target.Commands.Run(cast);
            s6CastAt = Time.time; s6StrikesThisStep++;
        }

        private JObject Sprint6RayRecord() => s6Records.Skip(s6AttemptsAtStep).OfType<JObject>().LastOrDefault(row => (string)row["abilityGuid"] == s6Ray.AssetGuid);

        private bool Sprint6HostileSettled() => target.Commands.Empty && !NativeSaveEffectBoundary.HasUnresolvedAbilities() && !NativeSaveEffectBoundary.HasUnresolvedProjectiles();

        private void TickSprint6ForcedHit()
        {
            switch (s6Step)
            {
                case 0:
                    s6Ray = Sprint6FindRay();
                    if (s6Ray == null) { FailSprint6Row("The library has no ranged touch ability with a rolled attack."); return; }
                    s6Facts["ability"] = new JObject { ["name"] = s6Ray.name, ["guid"] = s6Ray.AssetGuid };
                    Sprint6SpawnTarget("s6-forced"); s6Step = 1; return;
                case 1: if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return; s6NextPlan = "success"; s6Step = 2; return;
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("meleeNegated"); s6Step = 3; return;
                case 3: if (!target.Commands.Empty || !Sprint6RealTimeUsageFree(rider)) return; s6NextPlan = "equal"; s6Step = 4; return;
                case 4:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    Sprint6Step("meleeEqual"); s6Step = 5; return;
                case 5: if (!Sprint6HostileSettled() || !Sprint6RealTimeUsageFree(rider)) return; s6NextPlan = "success"; Sprint6Step("beforeOrdinaryRay"); s6Step = 6; return;
                // The ordinary cast rolls, commits and negates: the projectile takes its miss path.
                case 6:
                    if (Sprint6RayRecord() != null) { Sprint6Step("ordinaryRayCommitted"); s6Step = 7; return; }
                    if (Sprint6HostileSettled() && Time.time - s6CastAt > 2f) Sprint6CastRay(false);
                    return;
                case 7: if (!Sprint6HostileSettled() || !Sprint6RealTimeUsageFree(rider)) return; s6Facts["attemptsBeforeForced"] = s6Service.AttemptCount; s6Facts["chargesBeforeForced"] = s6Service.SwiftChargeCount; Sprint6Step("beforeForcedRay"); s6Frames = 0; s6Step = 8; return;
                // The forced cast commits nothing: no check, no use and no Swift, and the engine's hit stands.
                case 8:
                    if (Sprint6RayRecord() != null) { Sprint6Step("forcedRayRefused"); s6Frames = 0; s6Step = 9; return; }
                    if (Sprint6HostileSettled() && Time.time - s6CastAt > 2f) Sprint6CastRay(true);
                    return;
                case 9:
                    if (!Sprint6HostileSettled() || ++s6Frames < Sprint6SettleFrames) return;
                    s6Facts["attemptsAfterForced"] = s6Service.AttemptCount; s6Facts["chargesAfterForced"] = s6Service.SwiftChargeCount;
                    Sprint6Step("forcedRaySettled");
                    FinishSprint6Row(true, "Forced-hit observations recorded.");
                    return;
            }
        }

        // ---- turn-based ---------------------------------------------------------------------------------------------
        private bool Sprint6RiderTurn(TurnController turn) => turn?.Unit == rider && !ReferenceEquals(turn, s6Turn) && Chunk7NativeFixture.ReadyForTurnInput(turn) && rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation;
        private bool Sprint6AdvanceTo(TurnController turn, UnitEntityData wanted)
        {
            if (turn?.Unit == wanted) return true;
            EndCastingNativeTurn(turn);
            return false;
        }

        private void TickSprint6TurnBased(TurnController turn)
        {
            switch (s6Step)
            {
                case 0:
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                    if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                    Sprint6SpawnTarget("s6-tb"); s6Step = 1; return;
                // Own turn: the reaction is this turn's Swift action, and no debt is left.
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat) return;
                    if (WaitForNativeCastingPrincipal(true, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                    if (!rider.HasSwiftAction() || !Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; Sprint6Step("ownTurnBefore"); s6NextPlan = "success"; s6Step = 2; return;
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    Sprint6Step("ownTurnNegated"); s6NextPlan = "natural"; s6Step = 3; return;
                case 3:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    Sprint6Step("ownTurnSecondRefused"); s6Step = 4; return;
                // The hostile's own turn in the same round window: still spent, by its ordinary attack.
                case 4:
                    if (!Sprint6AdvanceTo(turn, target)) return;
                    s6Step = 5; return;
                case 5:
                    if (Sprint6RecordSinceStep() != null) { Sprint6Step("hostileTurnStillSpent"); s6Step = 6; return; }
                    Sprint6HostileTurnStrike(turn);
                    return;
                // Renewal: the next preparation, with no debt, leaves the Swift action free.
                case 6:
                    if (turn?.Unit != rider) { Sprint6AdvanceTo(turn, rider); return; }
                    if (!Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; Sprint6Step("renewedNoDebt"); stagedStep = new JObject(); s6Facts["otherSwift"] = stagedStep;
                    IssueStagedSwiftCast(); s6Step = 7; return;
                case 7:
                    if (!rider.Commands.Empty || castingTrace == null || !castingTrace.ProcessesSettled || rider.AreHandsBusyWithAnimation) return;
                    Sprint6Step("otherSwiftSpent"); s6Step = 8; return;
                // Off turn, after a Swift action spent on the last turn: this borrows the next one.
                case 8:
                    if (!Sprint6AdvanceTo(turn, target)) return;
                    s6NextPlan = "equal"; s6Step = 9; return;
                case 9:
                    if (Sprint6RecordSinceStep() != null) { Sprint6Step("offTurnEqual"); s6NextPlan = "natural"; s6Step = 10; return; }
                    Sprint6HostileTurnStrike(turn);
                    return;
                case 10:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("offTurnSecondRefused"); s6Step = 11; return;
                // The next preparation charges the borrowed action through the native writer: the debt's consumer.
                case 11:
                    if (turn?.Unit != rider) { Sprint6AdvanceTo(turn, rider); return; }
                    if (!Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; Sprint6Step("debtPaidAtPreparation"); s6NextPlan = "natural"; s6Step = 12; return;
                // On that turn no reaction is available, and the use is not lost by the refusal.
                case 12:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("payingTurnRefused");
                    var paying = Sprint6Step("payingTurnSurface"); paying["canDelay"] = turn.CanDelay(); paying["canMoveCursor"] = turn.CanMove; paying["acted"] = turn.IsActed();
                    s6Step = 21; return;
                // The paying turn's Swift is gone; the turn itself still moves by the ordinary ground click.
                case 21:
                    if (turn?.Unit != rider || !rider.Commands.Empty || !horse.Commands.Empty) return;
                    var from = horse.Position; var goal = FindWalkablePoint(horse.Position, 3f, .75f);
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false);
                    s6Facts["payingTurnMove"] = new JObject { ["from"] = CapturePosition(from), ["goal"] = CapturePosition(goal), ["frame"] = Time.frameCount };
                    s6Frames = 0; s6Step = 22; return;
                case 22:
                    if (++s6Frames < 10 || horse.View?.AgentASP?.IsReallyMoving == true || combat.HasActiveGroundMovement || !horse.Commands.Empty) return;
                    var moved = (JObject)s6Facts["payingTurnMove"];
                    moved["to"] = CapturePosition(horse.Position); moved["sameTurn"] = ReferenceEquals(turn, s6Turn); moved["mounted"] = relationship.State == RelationshipState.Mounted;
                    moved["metres"] = HorizontalDistance(horse.Position, new Vector3((float)moved["from"]["x"], (float)moved["from"]["y"], (float)moved["from"]["z"]));
                    s6Step = 13; return;
                // Positive renewal after the debt: a further off-turn reaction, negating the hit.
                case 13:
                    if (!Sprint6AdvanceTo(turn, target)) return;
                    s6NextPlan = "success"; s6Step = 14; return;
                case 14:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    Sprint6Step("offTurnRenewedNegated"); s6Frames = 0; s6Step = 15; return;
                default:
                    if (!target.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Turn-based economy observations recorded.");
                    return;
            }
        }

        // The hostile's ordinary attack on its own turn. One that misses leaves no record, so the same phase is struck again by rule.
        private void Sprint6HostileTurnStrike(TurnController turn)
        {
            if (turn?.Unit == rider) { EndCastingNativeTurn(turn); return; }
            if (s6StrikesThisStep == 0) { if (turn?.Unit == target && !Sprint6Command(horse) && target.Commands.Empty) Sprint6RuleStrike(horse); return; }
            if (target.Commands.Empty) Sprint6RuleStrike(horse);
        }
    }
}
