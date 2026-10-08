using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Stock rider casting/item baseline. No mounted casting adapter, native cost
    // override, process replay or staged movement authority is introduced here.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6cCastingScenario(string scenario) =>
            scenario == "chunk6c-casting-rt" || scenario == "chunk6c-casting-tb" ||
            scenario == "chunk6c-casting-unmounted-rt" || scenario == "chunk6c-casting-unmounted-tb";
        private bool IsChunk6cCasting => IsChunk6cCastingScenario(request.Scenario);
        private bool CastingTb => request.Scenario.EndsWith("-tb", StringComparison.Ordinal);
        private bool CastingMounted => !request.Scenario.Contains("-unmounted-");
        private const string Guidance = "c3a8f31778c3980498d8f00c980be5f5";
        private const string Heal = "5590652e1c2225c4ca30c4a699ab3649";
        private const string Snowball = "9f10909f0be1f5141bf1c102041f93d9";
        internal static readonly string[] CastingCases = { "C6C-quickened-self", "C6C-standard-self",
            "C6C-standard-friendly", "C6C-standard-hostile", "C6C-prepared-interrupt-after",
            "C6C-invalid-target", "C6C-cancel-before", "C6C-interrupt-before",
            "C6C-potion-self", "C6C-scroll-friendly", "C6C-full-round", "C6C-movement-policy", "C6C-rider-incapacity", "C6C-mount-incapacity", "C6C-under-threat" };
        private NativeCastingItemTrace castingTrace;
        private NativeActorAllocationTrace castingCosts;
        private readonly List<NativeCastingItemLease> castingItems = new List<NativeCastingItemLease>();
        private NativeCastingItemLease castingRod, castingPotion, castingScroll;
        private NativeCastingOriginalSlots castingOriginalSlots;
        private int castingCaseIndex, castingStage, castingEventOffset, castingCostOffset, castingSetupCostOffset;
        private AbilityData castingAbility;
        private SpellSlot castingPrepared;
        private UnitUseAbility castingShell;
        private UnitEntityData castingTarget;
        private JObject castingCaseFacts;
        private bool castingMountClick, castingInterrupted, castingCleanupStarted;
        private int castingStableFrames;
        private TurnController castingEndedTurn, castingSeenTurn;
        private string CastingCase => CastingCases[castingCaseIndex];
        private JObject CastingMeasurement => (JObject)observations["chunk6cCasting"];

        private void BeginChunk6cCasting()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Casting baseline requires the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            castingCosts.BoundaryObserved += ObserveCastingNativeCost;
            CastingMeasurementInit();
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            // Retain each owner before native acquisition so partial equip failures
            // stay reachable through the existing bounded fixture cleanup path.
            castingRod = AcquireCastingItem(NativeCastingItemLease.LesserQuickenRod);
            castingPotion = AcquireCastingItem(NativeCastingItemLease.CurePotion);
            castingScroll = AcquireCastingItem(NativeCastingItemLease.CureScroll);
            observations["chunk6cCasting"]["items"] = new JArray(castingItems.Select(i => i.Evidence));
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }
        private void CastingMeasurementInit()
        {
            observations["chunk6cCasting"] = new JObject {
                ["contract"] = "native-mounted-casting-items-v1", ["mode"] = CastingTb ? "TB" : "RT",
                ["mounted"] = CastingMounted, ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId,
                ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(CastingCases),
                ["initialPrepared"] = CastingPreparedInventory(), ["snapshots"] = new JArray(),
                ["policy"] = "Stock UnitUseAbility owns every cost, resource, targeting and process."
            };
        }
        private NativeCastingItemLease AcquireCastingItem(string blueprint)
        {
            var lease = new NativeCastingItemLease(rider, castingTrace, blueprint, castingOriginalSlots);
            castingItems.Add(lease);
            lease.Acquire();
            return lease;
        }
        private JArray CastingPreparedInventory() => new JArray(rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
            .Select(s => new JObject { ["slot"] = castingTrace.Identity(s), ["available"] = s.Available,
                ["ability"] = castingTrace.Ability(s.Spell) }));
        private AbilityData ResolveCastingAbility(string blueprint, bool prepared)
        {
            castingPrepared = null;
            if (prepared)
            {
                var matches = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                    .Where(s => s.Available && s.Spell.Blueprint.AssetGuid == blueprint).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Fixture has no unique available native prepared spell: " + blueprint);
                castingPrepared = matches[0]; return castingPrepared.Spell;
            }
            var spells = rider.Descriptor.Spellbooks.SelectMany(b => b.GetKnownSpells(0))
                .Where(s => s.Blueprint.AssetGuid == blueprint).ToArray();
            if (spells.Length != 1) throw new InvalidOperationException("Fixture has no unique native cantrip: " + blueprint);
            return spells[0];
        }
        private JObject CastingState() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["relationship"] = relationship.State.ToString(), ["generation"] = relationship.MountedPairGeneration,
            ["relationshipRider"] = relationship.Rider?.UniqueId, ["relationshipMount"] = relationship.Mount?.UniqueId,
            ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse),
            ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = horse.Commands.Empty,
            ["activePairCommand"] = combat.HasActiveCommand, ["pairMovement"] = combat.HasActiveGroundMovement,
            ["projectilesPending"] = NativeSaveEffectBoundary.HasUnresolvedProjectiles(), ["riderLife"] = CaptureChunk6aPendingIncapacityActor(rider), ["mountLife"] = CaptureChunk6aPendingIncapacityActor(horse), ["prepared"] = CastingPreparedInventory(), ["ability"] = castingTrace.Ability(castingAbility),
            ["slotAvailable"] = castingPrepared?.Available, ["slot"] = castingTrace.Identity(castingPrepared),
            ["items"] = new JArray(castingItems.Select(i => i.Snapshot())),
            ["shell"] = castingShell == null ? null : castingTrace.Shell(castingShell),
            ["processesSettled"] = castingTrace.ProcessesSettled,
            ["nativeAbilitiesPending"] = NativeSaveEffectBoundary.HasUnresolvedAbilities()
        };
        private void TickChunk6cCasting()
        {
            var game = Game.Instance; var controller = game.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (CastingTb && !ReferenceEquals(turn, castingSeenTurn)) { castingSeenTurn = turn; ResetLeafClock(); }
            CastingMeasurement["progress"] = new JObject { ["stage"] = castingStage, ["caseIndex"] = castingCaseIndex,
                ["case"] = CastingCase, ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse) };
            if (castingStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (CastingMounted && relationship.State != RelationshipState.Mounted)
                {
                    if (!castingMountClick) castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "casting-exploration-mount");
                    return;
                }
                if (!CastingMounted && relationship.State != RelationshipState.Unmounted)
                    throw new InvalidOperationException("Unmounted casting control entered mounted.");
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(CastingTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (CastingTb && pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                BeginTarget(6f, "casting-native-baseline"); ruleProbe.Arm(target, false);
                castingCosts.BeginEncounter(request.RunId + ":casting"); castingStage = 1; ResetLeafClock(); return;
            }
            if (castingStage == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat) return;
                if (WaitForNativeCastingPrincipal(CastingTb, turn?.Unit == rider,
                    rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                if (CastingTb && (!rider.HasStandardAction() || !rider.HasSwiftAction()))
                { EndCastingNativeTurn(turn); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                if (!CastingTb && (rider.CombatState.Cooldown.StandardAction > .001f || rider.CombatState.Cooldown.SwiftAction > .001f)) return;
                if (CastingMounted && relationship.State != RelationshipState.Mounted)
                {
                    if (!castingMountClick) castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "casting-native-remount-after-life");
                    return;
                }
                ResetCastingBoundary(); castingMountClick = false;
                castingShell = null; castingAbility = null; castingPrepared = null;
                castingTrace.BeginCase(CastingCase); castingSetupCostOffset = castingCosts.EventCount;
                if (CastingMotionCase || CastingThreatCase) { StartCastingBoundarySetup(); castingStage = 4; ResetLeafClock(); return; }
                BeginCastingCase(); return;
            }
            if (castingStage == 4)
            {
                if (!CastingBoundaryReady()) { if (CastingTb && CastingThreatCase) EndCastingNativeTurn(turn); return; }
                if (CastingTb && CastingThreatCase && (turn?.Unit != rider || !rider.HasStandardAction())) { EndCastingNativeTurn(turn); return; }
                castingBoundary["setupCostEvents"] = castingCosts.EventsSince(castingSetupCostOffset);
                BeginCastingCase(); return;
            }
            if (castingStage == 2)
            {
                ObserveCastingMotion();
                if (CastingLifeCase && !castingLifeApplied && castingShell != null && castingShell.IsRunning && !castingShell.IsActed)
                    ApplyCastingIncapacity();
                ObserveCastingIncapacity();
                if (!castingInterrupted && CastingCase == "C6C-interrupt-before" && castingShell != null && castingShell.IsRunning && !castingShell.IsActed)
                {
                    castingCaseFacts["interruptionBefore"] = CastingState(); castingInterrupted = true;
                    castingShell.Interrupt(); castingCaseFacts["interruptionAfter"] = CastingState();
                }
                if (castingShell != null && !castingShell.IsFinished) return;
                if (!rider.Commands.Empty || !horse.Commands.Empty || !castingTrace.ProcessesSettled ||
                    NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles()) return;
                if (++castingStableFrames < 10) return;
                if (CastingLifeCase) {
                    castingBoundary["settledBeforeHealthRestore"] = CastingState();
                    if (!RestoreCastingIncapacity()) return;
                }
                castingCaseFacts["after"] = CastingState();
                castingCaseFacts["boundary"] = castingBoundary.DeepClone();
                castingCaseFacts["motionSamples"] = castingMotionSamples.DeepClone();
                castingCaseFacts["events"] = castingTrace.EventsSince(castingEventOffset);
                castingCaseFacts["costEvents"] = castingCosts.EventsSince(castingCostOffset);
                castingCaseFacts["interrupted"] = castingInterrupted;
                // The producer reports bounded facts and structural completeness;
                // the external reader owns substantive PASS/FAIL for every row.
                AddRow(CastingCase, castingTrace.Complete && castingCosts.Complete,
                    "Observed normal native rider casting/item input and terminal ownership.", castingCaseFacts);
                if (castingCaseIndex == 0) { castingRod.ReleaseOwnedItem(); CastingMeasurement["rodCleanup"] = castingRod.Evidence.DeepClone(); }
                castingCaseIndex++; castingMountClick = false;
                if (castingCaseIndex == CastingCases.Length) { BeginCleanup(); return; }
                castingStage = 1; ResetLeafClock();
            }
        }
        private void BeginCastingCase()
        {
            castingTrace.BeginCase(CastingCase); castingEventOffset = castingTrace.EventCount; castingCostOffset = castingCosts.EventCount;
            castingShell = null; castingPrepared = null; castingInterrupted = false; castingStableFrames = 0;
            castingTarget = CastingCase == "C6C-standard-hostile" || CastingCase == "C6C-invalid-target" ? target :
                CastingCase == "C6C-standard-friendly" || CastingCase == "C6C-prepared-interrupt-after" || CastingCase == "C6C-scroll-friendly" ? horse : rider;
            castingAbility = CastingCase == "C6C-standard-hostile" ? ResolveCastingAbility(Snowball, true) :
                CastingCase == "C6C-prepared-interrupt-after" ? ResolveCastingAbility(Heal, true) :
                CastingCase == "C6C-potion-self" ? castingPotion.Item.Ability?.Data :
                CastingCase == "C6C-scroll-friendly" ? castingScroll.Item.Ability?.Data : ResolveCastingAbility(Guidance, false);
            if (CastingFullRoundCase) castingAbility = ResolveNativeFullRound();
            if (CastingFullRoundCase && castingAbility == null)
            {
                castingCaseFacts = new JObject { ["case"] = CastingCase, ["availability"] = "none-native",
                    ["conversions"] = CastingMeasurement["fullRoundNativeConversions"].DeepClone(), ["inputCount"] = 0,
                    ["before"] = CastingState(), ["afterInput"] = CastingState() };
                castingStage = 2; ResetLeafClock(); return;
            }
            if (castingAbility == null || castingAbility.Caster.Unit != rider)
                throw new InvalidOperationException("Native ability/item caster is not the exact rider.");
            if (CastingCase == "C6C-prepared-interrupt-after" || CastingCase == "C6C-potion-self" || CastingCase == "C6C-scroll-friendly")
                WoundCastingSubject(castingTarget);
            castingCaseFacts = new JObject { ["case"] = CastingCase, ["target"] = castingTarget.UniqueId,
                ["before"] = CastingState(), ["inputCount"] = 0 };
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(castingAbility);
            var resolved = handler.GetTarget(castingTarget.View.gameObject, castingTarget.Position, castingAbility);
            castingCaseFacts["resolvedTarget"] = resolved?.Unit?.UniqueId; castingCaseFacts["resolvedPoint"] = resolved == null ? null : CapturePosition(resolved.Point); castingCaseFacts["targetPoint"] = CapturePosition(castingTarget.Position);
            castingCaseFacts["canTarget"] = resolved != null && castingAbility.CanTarget(resolved);
            castingCaseFacts["selected"] = ReferenceEquals(handler.Ability, castingAbility);
            castingCaseFacts["selectionAfter"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(u => u.UniqueId));
            if (CastingCase == "C6C-cancel-before")
            {
                handler.SetAbility(null); castingCaseFacts["cancelledSelection"] = handler.Ability == null;
            }
            else
            {
                castingCaseFacts["clicked"] = handler.OnClick(castingTarget.View.gameObject, castingTarget.Position, 0, false, false);
                castingCaseFacts["inputCount"] = 1;
                var shells = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .Where(c => c.Spell.Blueprint == castingAbility.Blueprint).Distinct().ToArray();
                castingCaseFacts["admittedShellCount"] = shells.Length;
                if (shells.Length > 1) throw new InvalidOperationException("One normal input admitted multiple same-spell shells.");
                castingShell = shells.SingleOrDefault(); castingCaseFacts["costShell"] = castingCosts.ObjectIdentity(castingShell);
            }
            castingCaseFacts["afterInput"] = CastingState();
            castingStage = 2; ResetLeafClock();
        }
        private void ObserveCastingNativeCost(string boundary, UnitEntityData actor, UnitCommand command)
        {
            if (cleanupStarted || castingCaseIndex >= CastingCases.Length || CastingCase != "C6C-prepared-interrupt-after" ||
                castingInterrupted || boundary != "cost-after" || actor != rider || command != castingShell) return;
            castingCaseFacts["interruptionBefore"] = CastingState();
            castingInterrupted = true;
            // The native acted transition has already charged. Interrupt only the
            // exact shell through its native API; never stop/tick/refund its process.
            command.Interrupt(); castingCaseFacts["interruptionAfter"] = CastingState();
        }
        private void WoundCastingSubject(UnitEntityData subject)
        {
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            if (!subject.Descriptor.State.IsConscious || subject.Damage < 0 || factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor))
                throw new InvalidOperationException("Heal stimulus lacks intact disposable subject and finite native difficulty.");
            if (subject.Damage > 0) return; // Reuse an existing nonlethal wound; never erase it to create a heal result.
            var amount = (int)Math.Ceiling(3d / factor);
            if (amount * factor >= subject.Stats.HitPoints.ModifiedValue - 2) throw new InvalidOperationException("Heal stimulus has no safe native margin.");
            Rulebook.Trigger(new RuleDealDamage(target, subject, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
        }
        // Fixture input progression only. A rider is normally unable to act on
        // another actor's native turn; that fact must not block its lawful End Turn.
        // The caller's existing input helper still validates the exact native actor.
        internal static bool WaitForNativeCastingPrincipal(bool turnBased, bool principalTurn,
            bool principalCanAct, Action advanceNativeTurn)
        {
            if (turnBased && !principalTurn) { advanceNativeTurn(); return true; }
            return !principalCanAct;
        }
        private void EndCastingNativeTurn(TurnController turn)
        {
            var actor = turn?.Unit;
            if (actor == null || ReferenceEquals(turn, castingEndedTurn) || !actor.IsDirectlyControllable ||
                !actor.Commands.Empty || actor.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() ||
                Game.Instance.TurnBasedCombatController.WaitingForUI || GetPendingNextUnit(Game.Instance.TurnBasedCombatController) != null) return;
            if (actor != rider && actor != horse && !(targetService.NonPairPartyAiLease.OwnsExactMember(actor) && targetService.NonPairPartyAiLease.ValidateActive()))
                throw new InvalidOperationException("Casting fixture refused End Turn on a foreign actor.");
            castingCosts.Record("casting-end-turn-input-before", actor);
            Game.Instance.PauseBind();
            castingCosts.Record("casting-end-turn-input-after", actor); castingEndedTurn = turn; ResetLeafClock();
        }
        private bool DrainCastingFixture()
        {
            if (castingTrace == null) return true;
            if (!castingCleanupStarted) { castingCleanupStarted = true; castingCosts.BoundaryObserved -= ObserveCastingNativeCost; }
            if (castingShell != null && !castingShell.IsFinished) castingShell.Interrupt();
            if (castingThreatAttack != null && !castingThreatAttack.IsFinished) castingThreatAttack.Interrupt();
            ObserveCastingIncapacity();
            if (!RestoreCastingIncapacity()) return false;
            if (!rider.Commands.Empty || !horse.Commands.Empty || !castingTrace.ProcessesSettled || NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles()) return false;
            foreach (var unit in castingTrace.Summons) { if (unit.IsInState) unit.Destroy(); }
            Game.Instance.EntityDestroyer.Tick();
            CastingMeasurement["summonCleanup"] = new JArray(castingTrace.Summons.Select(u => new JObject {
                ["unit"] = u.UniqueId, ["inState"] = u.IsInState,
                ["worldContains"] = Game.Instance.State.Units.Any(x => ReferenceEquals(x, u)) }));
            if (castingTrace.Summons.Any(u => u.IsInState || Game.Instance.State.Units.Any(x => ReferenceEquals(x, u)))) return false;
            var itemFailures = new List<Exception>();
            for (var i = castingItems.Count - 1; i >= 0; i--)
            {
                try { castingItems[i].ReleaseOwnedItem(); }
                catch (Exception exception) { itemFailures.Add(exception); }
            }
            // Every exact lease remains retained for the existing bounded retry.
            // One failed item cannot prevent independent item cleanup or its evidence.
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence.DeepClone()));
            if (itemFailures.Count != 0) throw new AggregateException("Casting fixture item cleanup remains incomplete.", itemFailures);
            castingOriginalSlots.Restore();
            foreach (var item in castingItems) item.Dispose();
            CastingMeasurement["originalSlots"] = castingOriginalSlots.Evidence.DeepClone();
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence.DeepClone()));
            CastingMeasurement["final"] = CastingState();
            castingCosts.Dispose(); castingTrace.Dispose();
            CastingMeasurement["costTrace"] = castingCosts.Capture(); CastingMeasurement["castTrace"] = castingTrace.Capture();
            castingCosts = null; castingTrace = null;
            return true;
        }
    }
}