using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics.Actions;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk4IncomingScenario(string scenario) =>
            scenario == "chunk4-targeting-rider-rt" || scenario == "chunk4-targeting-mount-rt" || scenario == "chunk4-targeting-area-unmounted-rt";
        private bool IsChunk4Incoming => IsChunk4IncomingScenario(request.Scenario);
        private bool Chunk4IncomingUnmountedArea => request.Scenario == "chunk4-targeting-area-unmounted-rt";
        private string Chunk4AreaId => Chunk4IncomingUnmountedArea ? "C4-TARGETING-area-unmounted" : "C4-TARGETING-area-both";
        private bool Chunk4IncomingMount => request.Scenario == "chunk4-targeting-mount-rt";
        private UnitEntityData Chunk4IncomingSubject => Chunk4IncomingMount ? horse : rider;
        private UnitEntityData Chunk4IncomingOther => Chunk4IncomingMount ? rider : horse;
        private string Chunk4IncomingId => Chunk4IncomingUnmountedArea ? Chunk4AreaId : "C4-TARGETING-" + (Chunk4IncomingMount ? "mount" : "rider");
        private int chunk4IncomingStage;
        private bool chunk4IncomingMountSent;
        private bool chunk4IncomingDismountSent;
        private UnitEntityData chunk4Caster;
        private SpellSlot chunk4HealSlot;
        private SpellSlot chunk4AreaSlot;
        private AreaEffectEntityData chunk4Area;
        private string chunk4AreaBlueprint;
        private string[] chunk4AreasBefore;
        private UnitAttack chunk4HostileAttack;
        private JObject chunk4IncomingEvidence;
        private double chunk4IncomingPausedAt;
        private long chunk4IncomingPausedTicks;
        private JObject chunk4IncomingPausedState;

        private void BeginChunk4Incoming()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Incoming targeting requires the accepted paired authority.");
            CaptureIdleFixturePartyForCleanup();
            var casters = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).Where(actor => actor != null &&
                actor != rider && actor != horse && actor.Descriptor.State.IsConscious && actor.Descriptor.Spellbooks.Any(book =>
                    book.GetAllMemorizedSpells().Any(slot => slot.Available && slot.Spell.Blueprint.AssetGuid == "5590652e1c2225c4ca30c4a699ab3649"))).ToArray();
            if (casters.Length != 1) throw new InvalidOperationException("Expected one actual caster with an unused prepared native Cure Light Wounds slot.");
            chunk4Caster = casters[0];
            chunk4HealSlot = chunk4Caster.Descriptor.Spellbooks.SelectMany(book => book.GetAllMemorizedSpells()).Single(slot =>
                slot.Available && slot.Spell.Blueprint.AssetGuid == "5590652e1c2225c4ca30c4a699ab3649");
            if (!Chunk4IncomingMount)
            {
                chunk4AreaSlot = chunk4Caster.Descriptor.Spellbooks.SelectMany(book => book.GetAllMemorizedSpells()).Single(slot =>
                    slot.Available && slot.Spell.Blueprint.AssetGuid == "0fd00984a2c0e0a429cf1a911b4ec5ca");
                var spawn = chunk4AreaSlot.Spell.Blueprint.GetComponents<AbilityEffectRunAction>()
                    .SelectMany(component => component.Actions.Actions).OfType<ContextActionSpawnAreaEffect>().Single();
                chunk4AreaBlueprint = spawn.AreaEffect.AssetGuid;
                observations["chunk4AreaDefinition"] = Chunk4NativeAreaObservation.CaptureDefinition(spawn.AreaEffect);
            }
            chunk4IncomingEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4IncomingId,
                ["mode"] = "RT", ["subject"] = Chunk4IncomingSubject.UniqueId, ["other"] = Chunk4IncomingOther.UniqueId,
                ["caster"] = chunk4Caster.UniqueId, ["inputKind"] = "native-selected-ability-and-enemy-command",
                ["healBlueprint"] = chunk4HealSlot.Spell.Blueprint.AssetGuid, ["areaBlueprint"] = chunk4AreaBlueprint,
                ["healSlotInitiallyAvailable"] = chunk4HealSlot.Available };
            observations["chunk4Incoming"] = chunk4IncomingEvidence;
            observations["chunk4NativeEffectInventory"] = Chunk4NativeEffectInventory.Capture(rider, horse);
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString(), chunk4Caster);
            ordinaryAttackTrace.BeginCase(Chunk4IncomingId);
            chunk4IncomingObserver = new Chunk4IncomingRuleObserver(rider, horse);
            chunk4IncomingObserver.BeginCase(Chunk4IncomingId);
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private JObject CaptureChunk4IncomingState() => new JObject {
            ["pair"] = CaptureOrdinaryLiveState(), ["casterStandard"] = chunk4Caster.CombatState.Cooldown.StandardAction,
            ["casterMove"] = chunk4Caster.CombatState.Cooldown.MoveAction, ["casterPosition"] = Chunk4IncomingPosition(chunk4Caster),
            ["subjectDamage"] = Chunk4IncomingSubject.Damage, ["otherDamage"] = Chunk4IncomingOther.Damage,
            ["riderReflex"] = rider.Stats.SaveReflex.ModifiedValue, ["mountReflex"] = horse.Stats.SaveReflex.ModifiedValue,
            ["healSlotAvailable"] = chunk4HealSlot.Available, ["areaSlotAvailable"] = chunk4AreaSlot?.Available,
            ["subjectLife"] = Chunk4IncomingSubject.Descriptor.State.LifeState.ToString(),
            ["otherLife"] = Chunk4IncomingOther.Descriptor.State.LifeState.ToString(),
            ["casterCommands"] = new JArray(chunk4Caster.Commands.Raw.Select(CaptureOrdinaryCommand)),
            ["casterQueue"] = new JArray(chunk4Caster.Commands.Queue.Select(CaptureOrdinaryCommand)) };
        private static JArray Chunk4IncomingPosition(UnitEntityData actor) => new JArray(actor.Position.x, actor.Position.y, actor.Position.z);
        private bool Chunk4CasterReady => chunk4Caster.Commands.Empty && !chunk4Caster.AreHandsBusyWithAnimation &&
            chunk4Caster.CombatState.CanActInCombat && chunk4Caster.CombatState.Cooldown.StandardAction <= .001f &&
            chunk4Caster.CombatState.Cooldown.MoveAction <= .001f && !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(chunk4Caster);
        private JObject[] Chunk4IncomingEvents(string kind) => ((JArray)chunk4IncomingObserver.Capture()["events"]).OfType<JObject>()
            .Where(item => (string)item["kind"] == kind).ToArray();

        private JObject ClickChunk4IncomingAbility(AbilityData spell, UnitEntityData subject)
        {
            var handler = Game.Instance.SelectedAbilityHandler;
            SelectionManager.Instance.SelectUnit(chunk4Caster.View, true, true, false);
            handler.SetAbility(spell);
            var resolved = handler.GetTarget(subject.View.gameObject, subject.Position, spell);
            var before = CaptureChunk4IncomingState();
            var available = spell.IsAvailableForCast; var canTarget = spell.CanTarget(resolved);
            var pure = JToken.DeepEquals(before, CaptureChunk4IncomingState());
            var clicked = handler.OnClick(subject.View.gameObject, subject.Position, 0, false, false);
            return new JObject { ["blueprint"] = spell.Blueprint.AssetGuid, ["caster"] = spell.Caster.Unit.UniqueId,
                ["clickedActor"] = subject.UniqueId, ["resolvedActor"] = resolved?.Unit?.UniqueId,
                ["resolvedPosition"] = resolved == null ? null : new JArray(resolved.Point.x, resolved.Point.y, resolved.Point.z),
                ["available"] = available, ["canTarget"] = canTarget, ["queryPure"] = pure, ["clicked"] = clicked,
                ["before"] = before, ["after"] = CaptureChunk4IncomingState() };
        }

        private void TickChunk4Incoming()
        {
            var game = Game.Instance;
            chunk4IncomingEvidence["progress"] = new JObject { ["stage"] = chunk4IncomingStage,
                ["state"] = CaptureChunk4IncomingState(), ["rules"] = chunk4IncomingObserver.Capture(),
                ["nativeAbilityCommands"] = new JArray(chunk4Caster.Commands.Raw.Concat(chunk4Caster.Commands.Queue)
                    .OfType<UnitUseAbility>().Distinct().Select(CaptureNativeAbilityShell)) };
            if (chunk4IncomingStage != 3 && game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4IncomingStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (!Chunk4IncomingUnmountedArea && relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4IncomingMountSent) chunk4IncomingMountSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk4-incoming-mount");
                    return;
                }
                if (Chunk4IncomingUnmountedArea && relationship.State != RelationshipState.Unmounted)
                {
                    if (!chunk4IncomingDismountSent)
                        chunk4IncomingDismountSent = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "chunk4-incoming-dismount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(9f, Chunk4IncomingId); ruleProbe.Arm(target, false);
                chunk4IncomingStage = 1; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 1)
            {
                if (!IsCombatReady(true) || CombatController.IsInTurnBasedCombat() || !Chunk4CasterReady || !Chunk4PairedPlayIdle) return;
                if (Chunk4IncomingUnmountedArea) { chunk4IncomingStage = 5; ResetLeafClock(); return; }
                var subject = Chunk4IncomingSubject;
                var difficulty = game.Player.Difficulty.DamageToParty;
                if (subject.Damage != 0 || subject.Stats.TemporaryHitPoints.ModifiedValue != 0 ||
                    !subject.Descriptor.State.IsConscious || difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty))
                    throw new InvalidOperationException("Targeted-heal fixture requires the intact disposable actor and unchanged finite damage difficulty.");
                var requested = (int)Math.Ceiling(3d / difficulty);
                if (requested * difficulty >= subject.Stats.HitPoints.ModifiedValue - 2)
                    throw new InvalidOperationException("The native damage stimulus has no safe nonlethal margin.");
                chunk4IncomingEvidence["beforeWound"] = CaptureChunk4IncomingState();
                var damage = Rulebook.Trigger(new RuleDealDamage(target, subject, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                chunk4IncomingEvidence["woundRequested"] = requested; chunk4IncomingEvidence["nativeWound"] = damage.Damage;
                chunk4IncomingEvidence["afterWound"] = CaptureChunk4IncomingState();
                if (damage.Damage <= 0 || subject.Damage <= 0 || !subject.Descriptor.State.IsConscious ||
                    Chunk4IncomingOther.Damage != (int)chunk4IncomingEvidence["beforeWound"]["otherDamage"])
                    throw new InvalidOperationException("Native targeted wound did not preserve the independent actor.");
                chunk4IncomingStage = 2; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 2)
            {
                if (!Chunk4CasterReady || !Chunk4PairedPlayIdle) return;
                game.IsPaused = true;
                var click = ClickChunk4IncomingAbility(chunk4HealSlot.Spell, Chunk4IncomingSubject);
                chunk4IncomingEvidence["healClick"] = click;
                if (!(bool)click["clicked"] || !(bool)click["canTarget"] || !(bool)click["available"] || !(bool)click["queryPure"] ||
                    (string)click["resolvedActor"] != Chunk4IncomingSubject.UniqueId)
                    throw new InvalidOperationException("Native targeted heal did not resolve the exact independently selected subject.");
                foreach (var key in new[] { "casterStandard", "casterMove", "subjectDamage", "otherDamage", "healSlotAvailable" })
                    if (!JToken.DeepEquals(click["before"][key], click["after"][key])) throw new InvalidOperationException("Paused native spell input spent or applied an effect.");
                if (!JToken.DeepEquals(click["before"]["pair"], click["after"]["pair"]))
                    throw new InvalidOperationException("Paused incoming spell input changed the pair.");
                chunk4IncomingPausedAt = clock.Elapsed.TotalSeconds; chunk4IncomingPausedTicks = game.TimeController.GameTime.Ticks;
                chunk4IncomingPausedState = CaptureChunk4IncomingState(); chunk4IncomingStage = 3; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 3)
            {
                var pausedState = CaptureChunk4IncomingState();
                if (!game.IsPaused || game.TimeController.GameTime.Ticks != chunk4IncomingPausedTicks)
                    throw new InvalidOperationException("Paused queued native heal advanced native time.");
                // Native admission may prepare or promote the queued command.
                // Pause must preserve position, actual effects, slots and costs.
                foreach (var key in new[] { "pair", "casterStandard", "casterMove", "casterPosition", "subjectDamage", "otherDamage", "healSlotAvailable" })
                    if (!JToken.DeepEquals(chunk4IncomingPausedState[key], pausedState[key]))
                        throw new InvalidOperationException("Paused queued native heal moved an actor, spent or applied an effect.");
                if (clock.Elapsed.TotalSeconds - chunk4IncomingPausedAt < .35d) return;
                chunk4IncomingEvidence["pauseDuration"] = clock.Elapsed.TotalSeconds - chunk4IncomingPausedAt;
                chunk4IncomingEvidence["pausedBegin"] = chunk4IncomingPausedState;
                chunk4IncomingEvidence["pausedEnd"] = pausedState;
                game.IsPaused = false; chunk4IncomingStage = 4; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 4)
            {
                var heals = Chunk4IncomingEvents("heal-after");
                if (heals.Length == 0 || !chunk4Caster.Commands.Empty || chunk4Caster.AreHandsBusyWithAnimation) return;
                chunk4IncomingEvidence["afterHeal"] = CaptureChunk4IncomingState();
                if (heals.Length != 1 || (string)heals[0]["actor"] != chunk4Caster.UniqueId ||
                    (string)heals[0]["target"] != Chunk4IncomingSubject.UniqueId || (int)heals[0]["value"] <= 0 ||
                    Chunk4IncomingSubject.Damage >= (int)chunk4IncomingEvidence["afterWound"]["subjectDamage"] ||
                    Chunk4IncomingOther.Damage != (int)chunk4IncomingEvidence["afterWound"]["otherDamage"] || chunk4HealSlot.Available)
                    throw new InvalidOperationException("Native heal did not resolve exactly once to its own actor and consume its actual prepared slot.");
                chunk4IncomingEvidence["healRules"] = new JArray(heals);
                chunk4IncomingEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4IncomingId);
                chunk4IncomingEvidence["caseId"] = Chunk4IncomingId + "-heal";
                AddRow(Chunk4IncomingId + "-heal", true, "Actual native prepared heal independently targeted the actor; paused query/input remained pure and native execution spent its slot.", (JObject)chunk4IncomingEvidence.DeepClone());
                chunk4IncomingStage = Chunk4IncomingMount ? 8 : 5; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 5)
            {
                if (!Chunk4CasterReady || !Chunk4PairedPlayIdle) return;
                chunk4AreasBefore = game.State.AreaEffects.Select(area => area.UniqueId).ToArray();
                chunk4IncomingEvidence["beforeArea"] = CaptureChunk4IncomingState();
                ordinaryAttackTrace.BeginCase(Chunk4AreaId);
                chunk4IncomingEvidence["areaClick"] = ClickChunk4IncomingAbility(chunk4AreaSlot.Spell, horse);
                if (!(bool)chunk4IncomingEvidence["areaClick"]["clicked"] || !(bool)chunk4IncomingEvidence["areaClick"]["queryPure"])
                    throw new InvalidOperationException("Native Entangle targeting was not admitted with pure prediction.");
                chunk4IncomingStage = 6; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 6)
            {
                var owned = game.State.AreaEffects.Where(area => !chunk4AreasBefore.Contains(area.UniqueId) &&
                    area.Blueprint.AssetGuid == chunk4AreaBlueprint && area.Context.MaybeCaster == chunk4Caster).ToArray();
                if (owned.Length == 0) return;
                if (owned.Length != 1) throw new InvalidOperationException("One native area spell created duplicate owned area effects.");
                chunk4Area = owned[0];
                var saves = Chunk4IncomingEvents("saving-throw");
                if (!chunk4Area.UnitsInside.Contains(rider) || !chunk4Area.UnitsInside.Contains(horse) ||
                    saves.All(item => (string)item["actor"] != rider.UniqueId) || saves.All(item => (string)item["actor"] != horse.UniqueId)) return;
                var entries = saves.Where(item => (string)item["nativeSource"]?["area"] == chunk4Area.UniqueId &&
                    item["nativeSource"]?["callbacks"] is JArray callbacks && callbacks.Count == 1 &&
                    (string)callbacks[0]["kind"] == "unit-enter" && (string)callbacks[0]["token"] == "06002ccd").ToArray();
                chunk4IncomingEvidence["areaObservedSources"] = new JArray(saves);
                if (saves.Any(item => (string)item["nativeSource"]?["area"] != chunk4Area.UniqueId ||
                    !(item["nativeSource"]?["callbacks"] is JArray callbacks) || callbacks.Count != 1 ||
                    (string)callbacks[0]["kind"] != "unit-enter" && (string)callbacks[0]["kind"] != "round"))
                    throw new InvalidOperationException("Area save lacks its exact native entry/round source; phase cannot be inferred from timestamp alone.");
                if (new[] { rider, horse }.Any(actor => entries.Count(item => (string)item["actor"] == actor.UniqueId) != 1))
                    throw new InvalidOperationException("Native area entry did not resolve exactly once per independent actor.");
                var first = new[] { rider, horse }.Select(actor => entries.Single(item => (string)item["actor"] == actor.UniqueId)).ToArray();
                if (saves.GroupBy(item => (string)item["actor"] + ":" + (string)item["nativeSource"]["callbacks"][0]["token"] + ":" + (long)item["gameTicks"])
                    .Any(group => group.Count() != 1))
                    throw new InvalidOperationException("One native area callback duplicated an actor's saving throw.");
                if (first.Any(item => (string)item["type"] != "Reflex" || (int)item["dc"] <= 0 ||
                    entries.Count(other => (string)other["actor"] == (string)item["actor"]) != 1) ||
                    chunk4AreaSlot.Available || Chunk4IncomingSubject.Damage != (int)chunk4IncomingEvidence["beforeArea"]["subjectDamage"] ||
                    Chunk4IncomingOther.Damage != (int)chunk4IncomingEvidence["beforeArea"]["otherDamage"])
                    throw new InvalidOperationException("Native area effect duplicated an actor's first save, spent no slot or unexpectedly changed health.");
                chunk4IncomingEvidence["area"] = new JObject { ["level"] = "NATIVE INTEGRATION", ["mode"] = "RT", ["caseId"] = Chunk4AreaId, ["mounted"] = !Chunk4IncomingUnmountedArea,
                    ["entity"] = chunk4Area.UniqueId, ["blueprint"] = chunk4Area.Blueprint.AssetGuid,
                    ["caster"] = chunk4Area.Context.MaybeCaster.UniqueId, ["unitsInside"] = new JArray(chunk4Area.UnitsInside.Select(unit => unit.UniqueId)),
                    ["firstSaves"] = new JArray(first), ["allSaves"] = new JArray(saves), ["state"] = CaptureChunk4IncomingState(),
                    ["ruleDrops"] = chunk4IncomingObserver.Capture()["dropped"],
                    ["before"] = chunk4IncomingEvidence["beforeArea"].DeepClone(),
                    ["definition"] = observations["chunk4AreaDefinition"].DeepClone(),
                    ["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4AreaId),
                    ["components"] = new JArray(chunk4Area.Blueprint.ComponentsArray.Select(component => component.GetType().FullName)) };
                AddRow(Chunk4AreaId, true, "One native area resolved an entry Reflex save once per actor; separately identified native round callbacks remain native behavior.", (JObject)chunk4IncomingEvidence["area"].DeepClone());
                chunk4Area.Destroy(); chunk4IncomingStage = 7; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 7)
            {
                if (chunk4Area.IsInState || game.State.AreaEffects.Contains(chunk4Area)) return;
                chunk4IncomingEvidence["ownedAreaRemoved"] = true; chunk4Area = null;
                if (Chunk4IncomingUnmountedArea) { BeginCleanup(); return; }
                chunk4IncomingStage = 8; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 8)
            {
                if (!Chunk4IncomingUnmountedArea && relationship.State != RelationshipState.Mounted)
                    throw new InvalidOperationException("Incoming hostile targeting lost the mounted condition before its native order.");
                if (!Chunk4CasterReady || !Chunk4PairedPlayIdle || !target.Commands.Empty || target.AreHandsBusyWithAnimation ||
                    !target.CombatState.CanActInCombat || target.CombatState.Cooldown.StandardAction > .001f || target.CombatState.Cooldown.MoveAction > .001f) return;
                chunk4IncomingEvidence["beforeHostile"] = CaptureChunk4IncomingState();
                chunk4HostileAttack = new UnitAttack(Chunk4IncomingSubject) { CreatedByPlayer = true };
                target.Commands.Run(chunk4HostileAttack);
                chunk4IncomingStage = 9; ResetLeafClock(); return;
            }
            if (chunk4IncomingStage == 9)
            {
                var attacks = Chunk4IncomingEvents("attack-before").Where(item => (string)item["actor"] == target.UniqueId &&
                    (string)item["target"] == Chunk4IncomingSubject.UniqueId).ToArray();
                if (!chunk4HostileAttack.IsFinished || attacks.Length == 0 || !chunk4IncomingObserver.AllAttacksResolved) return;
                var rolls = Chunk4IncomingEvents("attack-roll").Where(item => (string)item["actor"] == target.UniqueId &&
                    (string)item["target"] == Chunk4IncomingSubject.UniqueId).ToArray();
                if (rolls.Length != attacks.Length || attacks.Any(item => (bool)item["opportunity"]) ||
                    !chunk4HostileAttack.IsActed || chunk4HostileAttack.Executor != target ||
                    rolls.Any(item => (int)item["nativeAC"] <= 0))
                    throw new InvalidOperationException("Hostile native command did not resolve the exact actor's independent attack/defense pipeline.");
                chunk4IncomingEvidence["afterHostile"] = CaptureChunk4IncomingState();
                chunk4IncomingEvidence["hostileCommand"] = CaptureOrdinaryCommand(chunk4HostileAttack);
                chunk4IncomingEvidence["nativeRules"] = chunk4IncomingObserver.Capture();
                chunk4IncomingEvidence["caseId"] = Chunk4IncomingId + "-hostile";
                AddRow(Chunk4IncomingId + "-hostile", true, "A real enemy native UnitAttack selected and resolved this actor's independent defenses; resulting injury or life state was preserved.", (JObject)chunk4IncomingEvidence.DeepClone());
                BeginCleanup();
            }
        }

        private void CleanupChunk4IncomingArea()
        {
            if (chunk4Area != null && chunk4Area.IsInState) chunk4Area.Destroy();
            if (IsChunk4Incoming && Game.Instance.IsPaused) Game.Instance.IsPaused = false;
        }
    }
}
