using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 4 (ATTACK-01, AI-01) native rows on the shared disposable casting fixture. Every order is the ordinary one:
    // the hostile click handler, the ground click handler, the native selected-ability click, native Stop and Hold, and
    // the AI flag the action bar's AI toggle writes. A target dies through a rule the pair's own attack triggered, after
    // the row left it one hit point as bounded setup. The second target is never clicked, dispatched or injected by the
    // row: the native party brain chooses it. The reader owns PASS/FAIL.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsSprint4NaturalScenario(string scenario) => scenario == "sprint4-natural-rt" || scenario == "sprint4-natural-tb" || scenario == "sprint4-intent-rt";
        private bool IsSprint4Intent => request.Scenario == "sprint4-intent-rt";
        private bool IsSprint4Natural => IsSprint4NaturalScenario(request.Scenario);
        internal const int Sprint4SchemaVersion = 48;
        // Inside the host deadline, so a stalled row still writes its evidence.
        internal const double Sprint4ScenarioDeadlineSeconds = 420.0d, Sprint4HostDeadlineSeconds = 460.0d;
        internal static readonly string[] Sprint4RealTimeCases = { "S4-ui-clean", "S4-ranged-retarget", "S4-controls", "S4-melee-retarget", "S4-hold-no-chase" };
        internal static readonly string[] Sprint4TurnBasedCases = { "S4-tb-ordinary-actors" };
        internal static readonly string[] Sprint4IntentCases = { "S4-move-into-combat", "S4-queued-hold", "S4-intent-neighbors" };
        private string[] Sprint4Cases => CastingTb ? Sprint4TurnBasedCases : IsSprint4Intent ? Sprint4IntentCases : Sprint4RealTimeCases;
        private string Sprint4Case => Sprint4Cases[s4CaseIndex];
        private const float Sprint4ObservationSeconds = 3f, Sprint4StopGraceSeconds = 1f, Sprint4RecipientMetres = 12f;
        private const int Sprint4RequiredAutomaticDispatches = 2;

        private sealed class Sprint4Target
        {
            internal DiagnosticCombatTargetService Service;
            internal UnitEntityData Unit;
        }

        private readonly List<Sprint4Target> s4Targets = new List<Sprint4Target>();
        private int s4CaseIndex, s4Stage, s4Step, s4Sub, s4StableFrames, s4EventOffset, s4CostOffset, s4LastRuleCount;
        private JObject s4Facts;
        private JArray s4Commands;
        private UnitEntityData s4First, s4Second, s4Third, s4Recipient;
        private MountedPairAttackCommand s4TrackedCommand;
        private JObject s4TrackedRecord;
        private MountedPairAttackOutcome s4LastOutcome;
        private Vector3 s4HoldPosition;
        private float s4WaitUntil;
        private Stopwatch s4Pause;
        private long s4Baseline, s4ProposalBaseline;
        private bool s4RiderAiBefore, s4RiderAiCaptured, s4StaleInjected;
        private int s4StaleIndex = -1;
        private TurnController s4Turn;

        private void BeginSprint4Natural()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 4 rows require the accepted single paired authority.");
            if (nativeControls.RetiredPrimaryHelpersSelected)
                throw new InvalidOperationException("Sprint 4 rows run the ordinary controls and never select the retired helpers.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            observations["sprint4Natural"] = new JObject {
                ["contract"] = "native-natural-attacks-retargeting-v1", ["mode"] = CastingTb ? "TB" : "RT", ["mounted"] = true,
                ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid,
                ["cases"] = new JArray(Sprint4Cases), ["retiredHelpersSelected"] = nativeControls.RetiredPrimaryHelpersSelected,
                ["companionsAiRoot"] = Game.Instance.BlueprintRoot.CompanionsAI,
                ["limitedAi"] = Kingmaker.Controllers.Brain.AiBrainController.LimitedAI,
                ["policy"] = "Ordinary native inputs; native brain chooses every automatic target; the pair controller is the one dispatcher." };
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            castingScroll = AcquireCastingItem(NativeCastingItemLease.CureScroll, ScrollStackCount);
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence));
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject Sprint4Controller() => new JObject {
            ["hasIntent"] = combat.HasStockAttackIntent, ["intentAutomatic"] = combat.StockIntentIsAutomatic,
            ["intentTarget"] = combat.CurrentStockIntentTarget?.UniqueId, ["stopSuppressed"] = combat.AutomaticSuppressedByStop,
            ["nativeRequests"] = combat.StockAttackNativeRequestCount, ["intentStarts"] = combat.StockAttackIntentStartCount,
            ["riderDispatches"] = combat.StockAttackRiderDispatchCount, ["mountDispatches"] = combat.StockAttackMountDispatchCount,
            ["duplicateDispatches"] = combat.StockAttackDuplicateDispatchCount, ["proposals"] = combat.AutomaticProposalCount,
            ["admitted"] = combat.AutomaticAdmittedCount, ["continued"] = combat.AutomaticContinuedCount,
            ["refused"] = combat.AutomaticRefusedCount, ["automaticDispatches"] = combat.AutomaticDispatchCount,
            ["retired"] = combat.AutomaticRetiredCount, ["lastRefusal"] = combat.LastAutomaticRefusal,
            ["lastRetirement"] = combat.LastAutomaticRetirement, ["lastObservation"] = combat.LastStockAttackObservation,
            ["holdKept"] = combat.AutomaticHoldKeptCount, ["queuedHoldKept"] = combat.AutomaticQueuedHoldKeptCount, ["queuedRuns"] = combat.AutomaticQueuedRunCount,
            ["queuedRefused"] = combat.AutomaticQueuedRefusedCount, ["playerMoveHold"] = combat.PlayerMoveHoldArmed, ["moveWatched"] = combat.WatchedPlayerMove != null,
            ["lastMoveOutcome"] = combat.LastPlayerMoveOutcome.ToString() };

        private JObject Sprint4State() {
            var state = CastingState();
            state["controller"] = Sprint4Controller();
            state["riderAiEnabled"] = rider.IsAIEnabled; state["riderBrainActive"] = rider.IsBrainActive; state["riderHold"] = rider.HoldState; state["mountHold"] = horse.HoldState;
            state["manualTarget"] = rider.CombatState.ManualTarget?.UniqueId; state["lastTarget"] = rider.CombatState.LastTarget?.UniqueId;
            state["mountManualTarget"] = horse.CombatState.ManualTarget?.UniqueId;
            state["riderPosition"] = CapturePosition(rider.Position); state["mountPosition"] = CapturePosition(horse.Position);
            state["riderInCombat"] = rider.IsInCombat; state["riderQueue"] = rider.Commands.Queue.Count; state["mountQueue"] = horse.Commands.Queue.Count;
            state["targets"] = new JArray(s4Targets.Select(t => Sprint4UnitFacts(t.Unit)));
            if (target != null) ((JArray)state["targets"]).Add(Sprint4UnitFacts(target));
            return state;
        }

        private JObject Sprint4UnitFacts(UnitEntityData unit) => new JObject {
            ["id"] = unit.UniqueId, ["dead"] = unit.Descriptor.State.IsDead, ["conscious"] = unit.Descriptor.State.IsConscious,
            ["hp"] = unit.HPLeft, ["damage"] = unit.Damage, ["distanceToMount"] = HorizontalDistance(unit.Position, horse.Position), ["hasLos"] = rider.HasLOS(unit) };

        private void BeginSprint4Row()
        {
            s4Facts = new JObject { ["case"] = Sprint4Case, ["before"] = Sprint4State(), ["inputs"] = new JArray() };
            s4Commands = new JArray(); s4Facts["pairCommands"] = s4Commands; s4Facts["steps"] = new JObject();
            s4EventOffset = castingTrace.EventCount; s4CostOffset = castingCosts.EventCount;
            castingTrace.BeginCase(Sprint4Case); s3Rules.BeginCase(Sprint4Case);
            s4Step = 0; s4Sub = 0; s4StableFrames = 0; s4TrackedCommand = null; s4TrackedRecord = null; s4LastRuleCount = 0;
            ResetLeafClock();
        }

        private void FinishSprint4Row(bool passed, string detail)
        {
            s4Facts["after"] = Sprint4State();
            s4Facts["events"] = castingTrace.EventsSince(s4EventOffset); s4Facts["costEvents"] = castingCosts.EventsSince(s4CostOffset);
            s4Facts["rules"] = Sprint4RowRules();
            AddRow(Sprint4Case, passed, detail, s4Facts);
            s4CaseIndex++; s4Stage = 1; s4Step = 0;
            if (s4CaseIndex == Sprint4Cases.Length) { BeginCleanup(); return; }
            ResetLeafClock();
        }

        private void FailSprint4Row(string why) { s4Facts["failure"] = why; FinishSprint4Row(false, why); }

        private JObject Sprint4RowRules()
        {
            var capture = s3Rules.Capture();
            return new JObject { ["dropped"] = capture["dropped"],
                ["events"] = new JArray(((JArray)capture["events"]).Where(e => (string)e["caseId"] == Sprint4Case).Select(e => e.DeepClone())) };
        }

        private JToken[] Sprint4Attacks(UnitEntityData actor, UnitEntityData onTarget) =>
            ((JArray)Sprint4RowRules()["events"]).Where(e => (string)e["kind"] == "attack-before" && (string)e["actor"] == actor.UniqueId &&
                (string)e["target"] == onTarget.UniqueId).ToArray();

        private JObject Sprint4Step(string name)
        {
            var record = new JObject { ["state"] = Sprint4State(), ["frame"] = Time.frameCount };
            ((JObject)s4Facts["steps"])[name] = record;
            return record;
        }

        // One record per exact pair command with its native plan and terminal; a strike or a repeated notification is
        // never counted as a command here, and a progressing routine restarts the leaf clock.
        private void ObserveSprint4Commands()
        {
            var active = combat.CurrentPairCommand;
            if (active != null && !ReferenceEquals(active, s4TrackedCommand))
            {
                s4TrackedCommand = active;
                s4TrackedRecord = new JObject {
                    ["identity"] = castingTrace.Identity(active), ["action"] = active.Action.ToString(), ["actor"] = active.ActionActor.UniqueId,
                    ["target"] = active.TargetUnit?.UniqueId, ["frame"] = Time.frameCount, ["automaticIntent"] = combat.StockIntentIsAutomatic,
                    ["createdByPlayer"] = active.CreatedByPlayer, ["aiAction"] = active.AiAction != null,
                    ["riderStandardCooldown"] = rider.CombatState.Cooldown.StandardAction, ["mountStandardCooldown"] = horse.CombatState.Cooldown.StandardAction,
                    ["manualTargetAtDispatch"] = rider.CombatState.ManualTarget?.UniqueId, ["plannedAttacks"] = 0, ["fullAttack"] = false };
                s4Commands.Add(s4TrackedRecord);
                ResetLeafClock();
            }
            if (active != null && ReferenceEquals(active, s4TrackedCommand))
            {
                var child = active.ChildAttack;
                if (child != null && child.AllAttacks.Count > (int)s4TrackedRecord["plannedAttacks"])
                {
                    s4TrackedRecord["plannedAttacks"] = child.AllAttacks.Count; s4TrackedRecord["fullAttack"] = child.IsFullAttack;
                    s4TrackedRecord["plannedWeapons"] = new JArray(child.AllAttacks.Select(a => a.Weapon?.Blueprint?.AssetGuid));
                }
            }
            var outcome = combat.LastOutcome;
            if (outcome != null && !ReferenceEquals(outcome, s4LastOutcome))
            {
                s4LastOutcome = outcome;
                if (s4TrackedRecord != null)
                {
                    s4TrackedRecord["result"] = outcome.Result; s4TrackedRecord["terminalReason"] = outcome.TerminalReason;
                    s4TrackedRecord["childAttackStarts"] = outcome.ChildAttackStartCount; s4TrackedRecord["rangedTail"] = outcome.NativeRangedTailTermination;
                    s4TrackedRecord["endFrame"] = Time.frameCount;
                }
            }
            var count = ((JArray)Sprint4RowRules()["events"]).Count;
            if (count != s4LastRuleCount) { s4LastRuleCount = count; ResetLeafClock(); }
            if (horse.View?.AgentASP?.IsReallyMoving == true) ResetLeafClock();
            foreach (var extra in s4Targets) { extra.Service.ObserveTargetLifeState(); extra.Service.RefreshBidirectionalCombatMemoryLease(); }
        }

        private bool Sprint4Settled() =>
            rider.Commands.Empty && horse.Commands.Empty && castingTrace.ProcessesSettled && !combat.HasLiveCarriedApproach && !combat.HasStockAttackIntent &&
            !NativeSaveEffectBoundary.HasUnresolvedAbilities() && !NativeSaveEffectBoundary.HasUnresolvedProjectiles() &&
            !rider.AreHandsBusyWithAnimation && horse.View?.AgentASP?.IsReallyMoving != true && !combat.HasActiveGroundMovement;

        private bool Sprint4Click(UnitEntityData hostile, string name)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            bool clicked;
            using (var input = new NativeOrdinaryAttackInput(hostile))
            {
                // The native cursor requires fresh predictions in turn-based combat (TurnController.IgnoreClick).
                if (CastingTb) input.Predict();
                clicked = input.Click();
            }
            ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "hostile-click", ["name"] = name, ["target"] = hostile.UniqueId,
                ["clicked"] = clicked, ["frame"] = Time.frameCount, ["inputPath"] = "ClickUnitHandler.OnClick" });
            return clicked;
        }

        private void Sprint4Input(string kind, string name, Action action)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            action();
            ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = kind, ["name"] = name, ["frame"] = Time.frameCount });
        }

        // An exact second native target through the same reviewed lease as BeginTarget. It never overlaps another target.
        private UnitEntityData Sprint4Spawn(float distance, string suffix, bool durable, Func<Vector3, bool> accepts = null)
        {
            var taken = s4Targets.Select(t => t.Unit.Position).Concat(target == null ? new Vector3[0] : new[] { target.Position }).ToArray();
            var point = FindWalkablePoint(rider.Position, distance, distance >= 10f ? 1f : .5f,
                candidate => taken.All(position => HorizontalDistance(candidate, position) >= 5f) && (accepts == null || accepts(candidate)));
            var service = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            var unit = service.Spawn(rider, horse, point, request.RunId + "-" + suffix, true, durable);
            if (!service.PrepareForPlayerClick(unit) || !service.QueueBidirectionalCombatMemory(rider, unit))
                throw new InvalidOperationException("Sprint 4 hostile target visibility/combat-memory lease failed.");
            service.BeginExpectedAttackDispatch(unit);
            s4Targets.Add(new Sprint4Target { Service = service, Unit = unit });
            return unit;
        }

        // Setup only: the target is left one hit point, so the first native hit is the kill. Nothing here decides a hit.
        // The stock Mammoth carries the dying condition (r02: a hit at one point left it unconscious), so the disposable
        // target gets the native rule of a monster without it: UnitLifeController kills at zero hit points.
        private void Sprint4LeaveOneHitPoint(UnitEntityData unit)
        {
            unit.Descriptor.State.AllowDyingCondition.ReleaseAll();
            unit.Damage = Math.Max(0, unit.MaxHP - 1);
            if (unit.HPLeft != 1 || !unit.Descriptor.State.IsConscious)
                throw new InvalidOperationException("The low target did not settle at one hit point.");
        }

        private bool Sprint4FirstTargetDied()
        {
            var state = s4First.Descriptor.State;
            if (state.IsDead) return true;
            if (!state.IsConscious) throw new InvalidOperationException("The low target fell unconscious instead of dying natively.");
            return false;
        }

        private bool Sprint4DestroyTargets()
        {
            for (var index = s4Targets.Count - 1; index >= 0; index--)
            {
                if (!s4Targets[index].Service.DestroyAndVerify()) return false;
                s4Targets[index].Service.Dispose();
                s4Targets.RemoveAt(index);
            }
            return true;
        }

        private void Sprint4SetAi(bool value, string name)
        {
            if (!s4RiderAiCaptured) { s4RiderAiBefore = rider.IsAIEnabled; s4RiderAiCaptured = true; }
            // The exact write the native AI toggle makes (QuickBarButton.InitAIToggle).
            rider.IsAIEnabled = value;
            ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "ai-toggle", ["name"] = name, ["value"] = value, ["frame"] = Time.frameCount });
        }

        private void TickSprint4Natural()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (s4Pause == null && game.IsPaused) { game.IsPaused = false; return; }
            CastingMeasurement["progress"] = new JObject { ["stage"] = s4Stage, ["caseIndex"] = s4CaseIndex, ["step"] = s4Step, ["sub"] = s4Sub,
                ["case"] = s4CaseIndex < Sprint4Cases.Length ? Sprint4Case : null, ["controller"] = Sprint4Controller() };
            if (s4Stage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "sprint4-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                s4Recipient = allocationFixtureParty.FirstOrDefault(member => member.IsInState && member.Descriptor.State.IsConscious && !member.Descriptor.IsPet)
                    ?? allocationFixtureParty.FirstOrDefault(member => member.IsInState && member.Descriptor.State.IsConscious);
                s3Recipient = s4Recipient;
                s4Stage = 1; ResetLeafClock(); return;
            }
            if (s4Stage == 1)
            {
                // A row that continues an encounter or ends it itself starts with the automatic path still attacking.
                var continuesEncounter = Sprint4Case == "S4-controls" || Sprint4Case == "S4-melee-retarget" || Sprint4Case == "S4-hold-no-chase" ||
                    Sprint4Case == "S4-queued-hold" || Sprint4Case == "S4-intent-neighbors";
                if (!continuesEncounter && (!Sprint4Settled() || game.HandsEquipmentController.IsUpdateScheduledFor(rider))) return;
                if (relationship.State != RelationshipState.Mounted) throw new InvalidOperationException("Sprint 4 rows require the exact mounted pair at every row start.");
                BeginSprint4Row(); s4Stage = 2; return;
            }
            ObserveSprint4Commands();
            switch (Sprint4Case)
            {
                case "S4-ui-clean": TickSprint4UiClean(); return;
                case "S4-ranged-retarget": TickSprint4Ranged(); return;
                case "S4-controls": TickSprint4Controls(); return;
                case "S4-melee-retarget": TickSprint4Melee(); return;
                case "S4-hold-no-chase": TickSprint4Hold(); return;
                case "S4-tb-ordinary-actors": TickSprint4TurnBased(turn); return;
                case "S4-move-into-combat": TickSprint4MoveIntoCombat(); return;
                case "S4-queued-hold": TickSprint4QueuedHold(); return;
                case "S4-intent-neighbors": TickSprint4IntentNeighbors(); return;
                default: throw new InvalidOperationException("Unknown Sprint 4 case " + Sprint4Case);
            }
        }

        // ---- ATTACK-01 -----------------------------------------------------------------------------------------
        private JObject Sprint4AbilitySurface(UnitEntityData unit)
        {
            var abilities = unit.Descriptor.Abilities.Enumerable.ToArray();
            return new JObject { ["unit"] = unit.UniqueId,
                // The list the native action bar groups read (ActionBarGroupElement: every non-hidden ability).
                ["listed"] = new JArray(abilities.Where(a => !a.Hidden).Select(a => a.Blueprint.AssetGuid)),
                ["retiredFacts"] = abilities.Count(a => a.Blueprint.AssetGuid == NativeMountedControlService.RiderPrimaryAbilityGuid || a.Blueprint.AssetGuid == NativeMountedControlService.MountPrimaryAbilityGuid),
                ["dismountFacts"] = abilities.Count(a => a.Blueprint.AssetGuid == NativeMountedControlService.DismountAbilityGuid),
                ["mountFacts"] = abilities.Count(a => a.Blueprint.AssetGuid == NativeMountedControlService.MountAbilityGuid),
                ["slots"] = Sprint4Slots(unit), ["autoUse"] = unit.AutoUseAbility?.Blueprint?.AssetGuid };
        }

        private static JArray Sprint4Slots(UnitEntityData unit)
        {
            var slots = unit.UISettings?.Slots; var result = new JArray();
            if (slots == null) return result;
            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null || slots[index] is MechanicActionBarSlotEmpty) continue;
                result.Add(new JObject { ["index"] = index, ["type"] = slots[index].GetType().Name,
                    ["blueprint"] = (slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint?.AssetGuid });
            }
            return result;
        }

        private void TickSprint4UiClean()
        {
            if (s4Step == 0)
            {
                var record = Sprint4Step("surface");
                record["rider"] = Sprint4AbilitySurface(rider); record["mount"] = Sprint4AbilitySurface(horse);
                record["blueprintsResolve"] = new JObject {
                    ["riderPrimary"] = ReferenceEquals(Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(NativeMountedControlService.RiderPrimaryAbilityGuid), nativeControls.RiderPrimaryAbility),
                    ["mountPrimary"] = ReferenceEquals(Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(NativeMountedControlService.MountPrimaryAbilityGuid), nativeControls.MountPrimaryAbility) };
                record["slotsClearedBefore"] = nativeControls.RetiredPrimarySlotsCleared; record["autoUseClearedBefore"] = nativeControls.RetiredPrimaryAutoUseCleared;
                // The stale references an older save could restore: one exact hotbar entry beside the unrelated slots
                // and one auto-use selection, neither with a fact behind it.
                var slots = rider.UISettings.Slots;
                for (var index = 0; index < slots.Length; index++)
                    if (slots[index] == null || slots[index] is MechanicActionBarSlotEmpty) { s4StaleIndex = index; break; }
                if (s4StaleIndex < 0) { FailSprint4Row("The rider's hotbar has no empty slot for the stale reference."); return; }
                record["unrelatedSlotsBefore"] = Sprint4Slots(rider);
                var stale = new AbilityData(nativeControls.RiderPrimaryAbility, rider.Descriptor);
                rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = stale }, s4StaleIndex);
                rider.AutoUseAbility = stale; s4StaleInjected = true;
                record["staleIndex"] = s4StaleIndex; record["staleSlotsAtInjection"] = Sprint4Slots(rider);
                s4StableFrames = 0; s4Step = 1; return;
            }
            if (++s4StableFrames < 30) return;
            var after = Sprint4Step("reconciled");
            after["rider"] = Sprint4AbilitySurface(rider); after["mount"] = Sprint4AbilitySurface(horse);
            after["slotsCleared"] = nativeControls.RetiredPrimarySlotsCleared; after["autoUseCleared"] = nativeControls.RetiredPrimaryAutoUseCleared;
            after["staleIndex"] = s4StaleIndex; after["staleInjected"] = s4StaleInjected;
            FinishSprint4Row(true, "Ordinary surface and stale references recorded.");
        }

        // ---- AI-01: ranged, with the native full attack ----------------------------------------------------------
        private void TickSprint4Ranged()
        {
            var game = Game.Instance;
            if (s4Step == 0)
            {
                if (rangedWeaponLease == null)
                {
                    rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                    rangedWeaponLease.AcquireCompatibleRanged();
                    AcquirePhase3hRapidShot();
                    ordinaryVariation = new NativeAttackFixtureVariation(rider, phase3hRapidToggle, true, null, true);
                    return;
                }
                if (!rangedWeaponLease.IsReady || game.HandsEquipmentController.IsUpdateScheduledFor(rider) || rider.AreHandsBusyWithAnimation) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                Sprint4SetAi(false, "before-encounter");
                s4First = Sprint4Spawn(9f, "s4-ranged-low", false);
                s4Second = Sprint4Spawn(11f, "s4-ranged-next", true);
                Sprint4LeaveOneHitPoint(s4First);
                castingCosts.BeginEncounter(request.RunId + ":sprint4-ranged");
                s4Facts["variation"] = ordinaryVariation.Capture();
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return;
                if (rider.CombatState.Cooldown.StandardAction > .001f || !rider.Commands.Empty || !horse.Commands.Empty) return;
                var plan = new UnitAttack(s4Second); plan.Init(rider);
                var attacks = plan.CreateFullAttack();
                s4Facts["nativePlan"] = new JObject { ["count"] = attacks.Count, ["weapons"] = new JArray(attacks.Select(a => a.Weapon?.Blueprint?.AssetGuid)),
                    ["ranged"] = attacks.Count(a => a.Weapon?.Blueprint?.IsRanged == true), ["approachRadius"] = plan.ApproachRadius, ["enoughClose"] = plan.IsUnitEnoughClose };
                s4Facts["mountStart"] = CapturePosition(horse.Position); s4HoldPosition = horse.Position;
                Sprint4SetAi(true, "before-first-click");
                if (!Sprint4Click(s4First, "first-target")) { FailSprint4Row("The ordinary hostile click was refused."); return; }
                Sprint4Step("afterFirstClick");
                s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (!Sprint4FirstTargetDied()) return;
                Sprint4Step("firstTargetDead"); s4Facts["killedAtFrame"] = Time.frameCount;
                s4Baseline = combat.AutomaticDispatchCount; s4Step = 3; ResetLeafClock(); return;
            }
            // Two routines on the unclicked target: the first has fully completed when the second begins.
            if (combat.AutomaticDispatchCount - s4Baseline < Sprint4RequiredAutomaticDispatches) return;
            if (combat.CurrentPairCommand?.ChildAttack == null || combat.CurrentPairCommand.ActionActor != rider && combat.StockAttackRiderDispatchCount < 2) return;
            Sprint4Step("retargeted");
            s4Facts["mountEnd"] = CapturePosition(horse.Position); s4Facts["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
            s4Facts["firstTarget"] = s4First.UniqueId; s4Facts["secondTarget"] = s4Second.UniqueId;
            FinishSprint4Row(true, "Ranged retarget observations recorded.");
        }

        // ---- Stop, AI toggle, manual replacement, pause and a cast, on the same encounter ------------------------------
        private void TickSprint4Controls()
        {
            if (s4Step == 0)
            {
                // Stop must cancel the automatic intent without the live brain recreating it.
                Sprint4Step("beforeStop"); s4Baseline = combat.AutomaticDispatchCount; s4ProposalBaseline = combat.AutomaticRefusedCount;
                Sprint4Input("stop", "stop", () => SelectionManager.Instance.Stop());
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (Time.time < s4WaitUntil) return;
                var record = Sprint4Step("afterStop");
                record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; record["refusedDuring"] = combat.AutomaticRefusedCount - s4ProposalBaseline;
                s4Baseline = combat.AutomaticDispatchCount;
                Sprint4SetAi(false, "after-stop"); s4StableFrames = 0; s4Step = 2; return;
            }
            if (s4Step == 2)
            {
                if (++s4StableFrames < 5) return;
                Sprint4SetAi(true, "explicit-re-enable"); s4Step = 3; ResetLeafClock(); return;
            }
            if (s4Step == 3)
            {
                if (combat.AutomaticDispatchCount == s4Baseline) return;
                Sprint4Step("afterReEnable");
                s4Third = Sprint4Spawn(9f, "s4-manual", true); s4Step = 4; ResetLeafClock(); return;
            }
            if (s4Step == 4)
            {
                if (!s4Third.IsInCombat) return;
                Sprint4Step("beforeManual");
                if (!Sprint4Click(s4Third, "manual-replacement")) { FailSprint4Row("The manual replacement click was refused."); return; }
                Sprint4Step("afterManual"); s4Step = 5; ResetLeafClock(); return;
            }
            if (s4Step == 5)
            {
                if (Sprint4Attacks(rider, s4Third).Length == 0 && Sprint4Attacks(horse, s4Third).Length == 0) return;
                var record = Sprint4Step("manualAttacked");
                record["intentOwnedByPlayer"] = combat.HasStockAttackIntent && !combat.StockIntentIsAutomatic && combat.CurrentStockIntentTarget == s4Third;
                s4Baseline = Sprint4Attacks(rider, s4Second).Length + Sprint4Attacks(horse, s4Second).Length;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 6; return;
            }
            if (s4Step == 6)
            {
                if (Time.time < s4WaitUntil) return;
                var record = Sprint4Step("manualHeld");
                record["attacksOnOldTargetSinceManual"] = Sprint4Attacks(rider, s4Second).Length + Sprint4Attacks(horse, s4Second).Length - s4Baseline;
                record["manualTargetStillOwned"] = combat.CurrentStockIntentTarget == s4Third && !combat.StockIntentIsAutomatic;
                s4ProposalBaseline = combat.AutomaticProposalCount; s4Baseline = combat.AutomaticDispatchCount;
                Game.Instance.IsPaused = true; s4Pause = Stopwatch.StartNew(); s4Step = 7; return;
            }
            if (s4Step == 7)
            {
                if (s4Pause.Elapsed.TotalSeconds < 1d) return;
                var record = Sprint4Step("paused");
                record["pausedObserved"] = Game.Instance.IsPaused;
                record["proposalsDuring"] = combat.AutomaticProposalCount - s4ProposalBaseline; record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline;
                Game.Instance.IsPaused = false; s4Pause = null; ResetLeafClock();
                // Back to the automatic path, so the cast below has a live automatic intent to take precedence over.
                Sprint4Input("stop", "before-cast", () => SelectionManager.Instance.Stop());
                Sprint4SetAi(false, "before-cast"); s4StableFrames = 0; s4Step = 8; return;
            }
            if (s4Step == 8)
            {
                if (++s4StableFrames < 5) return;
                Sprint4SetAi(true, "before-cast"); s4Baseline = combat.AutomaticDispatchCount; s4Step = 9; ResetLeafClock(); return;
            }
            if (s4Step == 9)
            {
                if (combat.AutomaticDispatchCount == s4Baseline || !combat.StockIntentIsAutomatic) return;
                if (s4Recipient == null) { FailSprint4Row("The fixture party has no eligible recipient besides the pair."); return; }
                if (!PlaceSprint3Recipient(Sprint4RecipientMetres)) return;
                WoundSprint3Recipient(s4Recipient);
                var record = Sprint4Step("beforeCast"); record["recipientDamage"] = s4Recipient.Damage;
                s3Facts = s4Facts;
                s4Facts["inputCount"] = 0;
                castingAbility = null; castingShell = null;
                if (IssueSprint3Cast(ExactCastingItemAbility(castingScroll), s4Recipient, "cast") == null) { FailSprint4Row("The ordinary ability click did not admit a native cast shell."); return; }
                ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "cast", ["name"] = "scroll", ["frame"] = Time.frameCount, ["inputPath"] = "ClickWithSelectedAbilityHandler.OnClick" });
                Sprint4Step("afterCastInput"); s4Baseline = combat.AutomaticDispatchCount; s4Step = 10; ResetLeafClock(); return;
            }
            if (s4Step == 10)
            {
                var carried = combat.HasLiveCarriedApproach;
                if (carried) s4Facts["carriedSeen"] = true;
                if (carried || CurrentSprint3Shell() != null) { s4Facts["dispatchesDuringCast"] = combat.AutomaticDispatchCount - s4Baseline; ResetLeafClock(); return; }
                if (s4Recipient.Damage != 0 && rider.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() != null) return;
                var record = Sprint4Step("castDone"); record["recipientDamage"] = s4Recipient.Damage;
                record["dispatchesDuringCast"] = combat.AutomaticDispatchCount - s4Baseline;
                s4Baseline = combat.AutomaticDispatchCount; s4Step = 11; ResetLeafClock(); return;
            }
            // Nothing is holding the pair back now, so the brain's next proposal resumes the automatic path.
            if (s4Step == 11)
            {
                if (combat.AutomaticDispatchCount == s4Baseline) return;
                Sprint4Step("resumedAfterCast");
                // The bow is in reach, so a native Hold leaves the brain proposing: a stationary dispatch that keeps Hold.
                Sprint4Input("hold", "hold-in-bow-reach", () => SelectionManager.Instance.Hold());
                s4HoldPosition = horse.Position; s4Baseline = combat.AutomaticDispatchCount; s4Step = 12; ResetLeafClock(); return;
            }
            if (s4Step == 12)
            {
                if (combat.AutomaticDispatchCount == s4Baseline || !combat.StockIntentIsAutomatic) return;
                var record = Sprint4Step("heldInBowReach");
                record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; record["holdKept"] = rider.HoldState;
                record["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
                // AI off with the automatic intent live: the intent ends without any other order.
                s4ProposalBaseline = combat.AutomaticRetiredCount;
                Sprint4SetAi(false, "live-automatic-intent"); s4Baseline = combat.AutomaticDispatchCount;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 13; ResetLeafClock(); return;
            }
            if (Time.time < s4WaitUntil) return;
            var off = Sprint4Step("aiOffRetired");
            off["retiredDuring"] = combat.AutomaticRetiredCount - s4ProposalBaseline; off["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline;
            Sprint4SetAi(true, "after-ai-off");
            FinishSprint4Row(true, "Stop, re-enable, manual replacement, pause, cast, Hold and AI-off observations recorded.");
        }

        // ---- AI-01: melee, with the mount's own participation --------------------------------------------------------
        private void TickSprint4Melee()
        {
            var game = Game.Instance;
            if (s4Step == 0)
            {
                if (rider.IsInCombat || horse.IsInCombat)
                {
                    if (!Sprint4DestroyTargets()) return;
                    return;
                }
                if (!Sprint4DestroyTargets()) return;
                if (rangedWeaponLease != null) { rangedWeaponLease.Dispose(); rangedWeaponLease = null; ordinaryVariation?.Dispose(); ordinaryVariation = null; RestorePhase3hRapidShot(); return; }
                if (game.HandsEquipmentController.IsUpdateScheduledFor(rider) || rider.AreHandsBusyWithAnimation || !Sprint4Settled()) return;
                var weapon = rider.GetFirstWeapon();
                s4Facts["riderWeapon"] = new JObject { ["blueprint"] = weapon?.Blueprint?.AssetGuid, ["ranged"] = weapon?.Blueprint?.IsRanged, ["natural"] = weapon?.Blueprint?.IsNatural };
                if (weapon?.Blueprint == null || weapon.Blueprint.IsRanged) { FailSprint4Row("The fixture rider has no melee weapon after the ranged lease."); return; }
                Sprint4SetAi(false, "before-encounter");
                BeginTarget(14f, "s4-melee-next"); ruleProbe.Arm(target, false);
                s4Second = target;
                s4First = Sprint4Spawn(9f, "s4-melee-low", false);
                Sprint4LeaveOneHitPoint(s4First);
                castingCosts.BeginEncounter(request.RunId + ":sprint4-melee");
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return;
                if (rider.CombatState.Cooldown.StandardAction > .001f || !rider.Commands.Empty || !horse.Commands.Empty) return;
                var plan = new UnitAttack(s4Second); plan.Init(rider);
                s4Facts["nativePlan"] = new JObject { ["count"] = plan.CreateFullAttack().Count };
                Sprint4SetAi(true, "before-first-click");
                if (!Sprint4Click(s4First, "first-target")) { FailSprint4Row("The ordinary hostile click was refused."); return; }
                Sprint4Step("afterFirstClick"); s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (!Sprint4FirstTargetDied()) return;
                Sprint4Step("firstTargetDead"); s4Facts["firstTarget"] = s4First.UniqueId; s4Facts["secondTarget"] = s4Second.UniqueId;
                s4Step = 3; ResetLeafClock(); return;
            }
            if (Sprint4Attacks(rider, s4Second).Length == 0 || Sprint4Attacks(horse, s4Second).Length == 0) return;
            Sprint4Step("retargeted");
            FinishSprint4Row(true, "Melee retarget observations recorded.");
        }

        // ---- Hold and no-chase: a completed player move, then the native Hold --------------------------------------
        private void TickSprint4Hold()
        {
            if (s4Step == 0)
            {
                var away = FindWalkablePoint(s4Second.Position, 14f, 4f, candidate => Sprint3PathPossible(horse.Position, candidate) == true);
                s4Facts["moveTarget"] = CapturePosition(away);
                bool clicked;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                using (var input = new NativeOrdinaryAttackInput(away)) clicked = input.Click();
                ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "ground-click", ["name"] = "move-away", ["clicked"] = clicked, ["frame"] = Time.frameCount });
                Sprint4Step("afterMoveInput");
                if (!clicked) { FailSprint4Row("The ordinary ground click was refused."); return; }
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (!horse.Commands.Empty || !rider.Commands.Empty || horse.View?.AgentASP?.IsReallyMoving == true) return;
                var record = Sprint4Step("afterMoveArrival");
                record["distanceToTarget"] = HorizontalDistance(horse.Position, s4Second.Position);
                s4HoldPosition = horse.Position; s4Baseline = combat.AutomaticDispatchCount; s4ProposalBaseline = combat.AutomaticRefusedCount;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (Time.time < s4WaitUntil) return;
                var record = Sprint4Step("heldAfterMove");
                record["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
                record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; record["refusedDuring"] = combat.AutomaticRefusedCount - s4ProposalBaseline;
                // A deliberate click releases the hold exactly as it does for an unmounted unit.
                if (!Sprint4Click(s4Second, "attack-after-move")) { FailSprint4Row("The hostile click after the move was refused."); return; }
                s4Baseline = Sprint4Attacks(rider, s4Second).Length; s4Step = 3; ResetLeafClock(); return;
            }
            if (s4Step == 3)
            {
                if (Sprint4Attacks(rider, s4Second).Length == s4Baseline) return;
                Sprint4Step("attackAfterMove");
                Sprint4Input("hold", "hold", () => SelectionManager.Instance.Hold());
                Sprint4Step("afterHold"); s4HoldPosition = horse.Position;
                s4Baseline = combat.AutomaticDispatchCount; s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 4; ResetLeafClock(); return;
            }
            if (Time.time < s4WaitUntil) return;
            var held = Sprint4Step("heldInReach");
            held["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
            held["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; held["holdKept"] = rider.HoldState;
            held["manualTarget"] = rider.CombatState.ManualTarget?.UniqueId;
            FinishSprint4Row(true, "Hold and no-chase observations recorded.");
        }

        // ---- Player intent: the exact move's own completion, and a queued automatic command keeping Hold ----------------
        private UnitMoveTo s4Move, s4InterruptedMove;
        private Vector3 s4MoveStart;
        private MountedPairAttackCommand s4QueuedCommand;
        private Sprint4BusyAttack s4Busy;
        private int s4QueueAttempts;
        private const int Sprint4QueueAttemptLimit = 3;

        // A native attack on the mount's Standard slot that spends nothing, the way a native spell-combat attack does. It
        // is bounded setup: UnitCommands.Run then queues the pair's dispatch behind this running command by its own rules
        // (a running command on the same target), which an ordinary fixture cannot reach because an actor's own attack
        // leaves it on cooldown for longer than the attack lasts.
        private sealed class Sprint4BusyAttack : UnitAttack
        {
            internal Sprint4BusyAttack(UnitEntityData target) : base(target)
            {
                IsSingleAttack = true;
                IgnoreCooldown();
            }
        }

        private JObject Sprint4MoveFacts(UnitMoveTo move, JObject record)
        {
            record["moveIdentity"] = castingTrace.Identity(move); record["moveType"] = move.GetType().Name; record["createdByPlayer"] = move.CreatedByPlayer;
            record["executor"] = move.Executor?.UniqueId; record["started"] = move.IsStarted; record["acted"] = move.IsActed; record["finished"] = move.IsFinished;
            record["result"] = move.Result.ToString(); record["riderInCombat"] = rider.IsInCombat; record["mountInCombat"] = horse.IsInCombat;
            record["turnBased"] = CombatController.IsInTurnBasedCombat(); record["generation"] = relationship.MountedPairGeneration;
            record["position"] = CapturePosition(horse.Position);
            return record;
        }

        private bool Sprint4GroundMove(Vector3 point, string name)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            bool clicked;
            using (var input = new NativeOrdinaryAttackInput(point)) clicked = input.Click();
            ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "ground-click", ["name"] = name, ["clicked"] = clicked, ["frame"] = Time.frameCount,
                ["target"] = CapturePosition(point), ["inputPath"] = "ClickGroundHandler.OnClick" });
            if (!clicked) { FailSprint4Row("The ordinary ground click was refused."); return false; }
            var watched = combat.WatchedPlayerMove as UnitMoveTo;
            if (watched == null || !ReferenceEquals(horse.Commands.Move, watched)) { FailSprint4Row("The ground click left no exact watched move on the mount."); return false; }
            s4Move = watched; s4MoveStart = horse.Position;
            return true;
        }

        private void TickSprint4MoveIntoCombat()
        {
            var game = Game.Instance;
            if (s4Step == 0)
            {
                if (rangedWeaponLease == null) { rangedWeaponLease = new Phase3dRangedWeaponLease(rider); rangedWeaponLease.AcquireCompatibleRanged(); return; }
                if (!rangedWeaponLease.IsReady || game.HandsEquipmentController.IsUpdateScheduledFor(rider) || rider.AreHandsBusyWithAnimation) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (rider.IsInCombat || horse.IsInCombat) { FailSprint4Row("The move row must start before combat."); return; }
                Sprint4SetAi(true, "before-move");
                var point = FindWalkablePoint(horse.Position, 30f, 8f, candidate => Sprint3PathPossible(horse.Position, candidate) == true);
                s4Facts["moveTarget"] = CapturePosition(point); s4HoldPosition = point;
                if (!Sprint4GroundMove(point, "exploration-move")) return;
                Sprint4MoveFacts(s4Move, Sprint4Step("moveIssued"));
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (s4Move.IsFinished) { FailSprint4Row("The exploration move ended before its encounter began."); return; }
                if (horse.View?.AgentASP?.IsReallyMoving != true) return;
                // The encounter begins behind the moving pair, well beyond the rider's reach from the destination.
                var destination = s4HoldPosition;
                s4First = Sprint4Spawn(9f, "s4-move-far", true, candidate => HorizontalDistance(candidate, destination) >= 18f);
                s4Facts["firstTarget"] = s4First.UniqueId;
                s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (s4Move.IsFinished && (!rider.IsInCombat || !horse.IsInCombat)) { FailSprint4Row("The exploration move ended before combat began."); return; }
                if (!rider.IsInCombat || !horse.IsInCombat) return;
                Sprint4MoveFacts(s4Move, Sprint4Step("combatWhileMoving"));
                s4Step = 3; ResetLeafClock(); return;
            }
            if (s4Step == 3)
            {
                // The controller has settled the exact move's native outcome, and the proposals since it are refused.
                if (combat.WatchedPlayerMove != null) return;
                var record = Sprint4MoveFacts(s4Move, Sprint4Step("arrival"));
                record["distanceToTarget"] = HorizontalDistance(horse.Position, s4First.Position);
                s4HoldPosition = horse.Position; s4Baseline = combat.AutomaticDispatchCount; s4ProposalBaseline = combat.AutomaticRefusedCount;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 4; ResetLeafClock(); return;
            }
            if (s4Step == 4)
            {
                if (Time.time < s4WaitUntil) return;
                var record = Sprint4Step("heldAfterArrival");
                record["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
                record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; record["refusedDuring"] = combat.AutomaticRefusedCount - s4ProposalBaseline;
                // The interruption control: a new move, stopped natively before it arrives, never holds the pair.
                var far = s4First; var here = HorizontalDistance(horse.Position, far.Position);
                var away = FindWalkablePoint(horse.Position, 8f, 3f, candidate => HorizontalDistance(candidate, far.Position) >= here && Sprint3PathPossible(horse.Position, candidate) == true);
                if (!Sprint4GroundMove(away, "move-to-interrupt")) return;
                s4InterruptedMove = s4Move;
                Sprint4MoveFacts(s4InterruptedMove, Sprint4Step("secondMoveIssued"));
                s4Step = 5; ResetLeafClock(); return;
            }
            if (s4Step == 5)
            {
                if (s4InterruptedMove.IsFinished) { FailSprint4Row("The second move ended before the interruption control."); return; }
                if (horse.View?.AgentASP?.IsReallyMoving != true || HorizontalDistance(s4MoveStart, horse.Position) < .5f) return;
                // The exact native call SelectionManager.Hold makes for a selected mover.
                horse.Commands.InterruptMove();
                ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "native-interrupt-move", ["frame"] = Time.frameCount });
                s4Step = 6; ResetLeafClock(); return;
            }
            if (s4Step == 6)
            {
                if (combat.WatchedPlayerMove != null) return;
                var record = Sprint4MoveFacts(s4InterruptedMove, Sprint4Step("interrupted"));
                record["distanceToTarget"] = HorizontalDistance(horse.Position, s4First.Position);
                s4HoldPosition = horse.Position; s4Baseline = combat.AutomaticDispatchCount;
                s4Step = 7; ResetLeafClock(); return;
            }
            // Nothing holds the pair: the native brain's proposal reaches the far target with the mount carrying the approach.
            if (combat.AutomaticDispatchCount == s4Baseline || HorizontalDistance(s4HoldPosition, horse.Position) < 1f) return;
            var resumed = Sprint4Step("resumedAfterInterruption");
            resumed["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; resumed["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
            FinishSprint4Row(true, "Move completion and interruption observations recorded.");
        }

        private void TickSprint4QueuedHold()
        {
            if (s4Step == 0)
            {
                Sprint4SetAi(false, "before-queued-target");
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (combat.HasStockAttackIntent || combat.CurrentPairCommand != null || !horse.Commands.Empty) return;
                s4Second = Sprint4Spawn(3.5f, "s4-queued-near", true);
                s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (!s4Second.IsInCombat || rider.Commands.Standard != null || !rider.HasLOS(s4Second)) return;
                // Only the near target remains for the native brain to choose, inside the rider's own bow reach.
                var previous = s4Targets.First(t => t.Unit == s4First);
                if (!previous.Service.DestroyAndVerify()) return;
                previous.Service.Dispose(); s4Targets.Remove(previous);
                s4Facts["secondTarget"] = s4Second.UniqueId; s4Facts["mountStart"] = CapturePosition(horse.Position); s4HoldPosition = horse.Position;
                Sprint4Input("hold", "hold-before-automatic", () => SelectionManager.Instance.Hold());
                Sprint4SetAi(true, "queued-target");
                Sprint4Step("heldBeforeAutomatic");
                s4Step = 3; ResetLeafClock(); return;
            }
            if (s4Step == 3)
            {
                if (!combat.StockIntentIsAutomatic || combat.CurrentStockIntentTarget != s4Second || !horse.Commands.Empty || horse.Commands.Queue.Count != 0) return;
                var active = combat.CurrentPairCommand;
                var mountCooldown = horse.CombatState.Cooldown.StandardAction;
                // The pair's next dispatch is the mount's: either the rider's routine is about to end with the mount ready,
                // or the mount's own cooldown is about to expire with the rider spent. The busy command starts before it.
                var riderEnding = active != null && active.ActionActor == rider && active.IsActed && mountCooldown <= 0f;
                var mountExpiring = active == null && !rider.HasStandardAction() && mountCooldown > .15f && mountCooldown < .6f;
                if (!riderEnding && !mountExpiring) return;
                s4Busy = new Sprint4BusyAttack(s4Second);
                horse.Commands.Run(s4Busy);
                if (!horse.Commands.Contains(s4Busy) || !s4Busy.IsUnitEnoughClose) { FailSprint4Row("The mount's busy attack was not admitted in reach."); return; }
                // Native Run cleared the mount's Hold for this setup command. The Hold under test is written the way the
                // native Hold input writes it for a selected unit, while the setup command runs.
                horse.HoldState = true;
                var record = Sprint4Step("busyStarted" + s4QueueAttempts);
                record["riderEnding"] = riderEnding; record["mountExpiring"] = mountExpiring; record["mountCooldown"] = mountCooldown;
                s4QueuedCommand = null; s4Baseline = combat.AutomaticQueuedRunCount; s4ProposalBaseline = combat.AutomaticQueuedHoldKeptCount;
                s4Step = 4; ResetLeafClock(); return;
            }
            if (s4Step == 4)
            {
                var active = combat.CurrentPairCommand;
                if (s4QueuedCommand == null)
                {
                    if (active == null || active.ActionActor != horse)
                    {
                        if (!s4Busy.IsFinished) return;
                        // The busy attack ended before the dispatch: nothing was queued, and nothing is claimed for it.
                        if (++s4QueueAttempts >= Sprint4QueueAttemptLimit) { FailSprint4Row("No automatic command reached the native queue within the bounded attempts."); return; }
                        s4Step = 3; return;
                    }
                    if (!horse.Commands.Queue.Contains(active))
                    {
                        if (++s4QueueAttempts >= Sprint4QueueAttemptLimit) { FailSprint4Row("The automatic command ran immediately on every bounded attempt."); return; }
                        s4Step = 3; return;
                    }
                    s4QueuedCommand = active;
                    var queued = Sprint4Step("queued");
                    queued["identity"] = castingTrace.Identity(active); queued["automaticOrigin"] = active.IsAutomaticOrigin; queued["createdByPlayer"] = active.CreatedByPlayer;
                    queued["aiAction"] = active.AiAction != null; queued["busyRunning"] = s4Busy.IsRunning; queued["busyActed"] = s4Busy.IsActed; queued["actor"] = active.ActionActor.UniqueId;
                    queued["inStandardSlot"] = horse.Commands.Contains(active); queued["started"] = active.IsStarted;
                    return;
                }
                if (!horse.Commands.Contains(s4QueuedCommand)) { if (s4QueuedCommand.IsFinished) FailSprint4Row("The queued automatic command ended without its later native Run."); return; }
                var ran = Sprint4Step("ranFromQueue");
                ran["identity"] = castingTrace.Identity(s4QueuedCommand); ran["queueAfter"] = horse.Commands.Queue.Count; ran["busyFinished"] = s4Busy.IsFinished;
                ran["queuedRuns"] = combat.AutomaticQueuedRunCount - s4Baseline; ran["queuedHoldKept"] = combat.AutomaticQueuedHoldKeptCount - s4ProposalBaseline;
                s4Step = 5; ResetLeafClock(); return;
            }
            if (s4Step == 5)
            {
                if (!s4QueuedCommand.IsFinished) return;
                var done = Sprint4Step("queuedDone");
                done["result"] = s4QueuedCommand.Result.ToString(); done["identity"] = castingTrace.Identity(s4QueuedCommand);
                done["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
                done["mountAttacks"] = Sprint4Attacks(horse, s4Second).Length; done["mountStandardCooldown"] = horse.CombatState.Cooldown.StandardAction;
                // A later target beyond every reach: the kept Hold is what stops the chase.
                s4Third = Sprint4Spawn(19f, "s4-queued-far", true);
                s4Step = 6; ResetLeafClock(); return;
            }
            if (s4Step == 6)
            {
                if (!s4Third.IsInCombat || combat.CurrentPairCommand != null) return;
                var near = s4Targets.First(t => t.Unit == s4Second);
                if (!near.Service.DestroyAndVerify()) return;
                near.Service.Dispose(); s4Targets.Remove(near);
                s4Step = 7; ResetLeafClock(); return;
            }
            if (s4Step == 7)
            {
                if (combat.HasStockAttackIntent || combat.CurrentPairCommand != null) return;
                s4HoldPosition = horse.Position; s4Baseline = combat.AutomaticDispatchCount; s4ProposalBaseline = combat.AutomaticRefusedCount;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 8; ResetLeafClock(); return;
            }
            if (Time.time < s4WaitUntil) return;
            var held = Sprint4Step("farTargetHeld");
            held["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position); held["distanceToTarget"] = HorizontalDistance(horse.Position, s4Third.Position);
            held["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; held["refusedDuring"] = combat.AutomaticRefusedCount - s4ProposalBaseline;
            FinishSprint4Row(true, "Queued automatic command and kept Hold observations recorded.");
        }

        private void TickSprint4IntentNeighbors()
        {
            if (s4Step == 0)
            {
                Sprint4Step("beforeStop"); s4HoldPosition = horse.Position;
                s4Baseline = combat.AutomaticDispatchCount; s4ProposalBaseline = combat.AutomaticRefusedCount;
                Sprint4Input("stop", "stop", () => SelectionManager.Instance.Stop());
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (Time.time < s4WaitUntil) return;
                var record = Sprint4Step("afterStop");
                record["dispatchesDuring"] = combat.AutomaticDispatchCount - s4Baseline; record["refusedDuring"] = combat.AutomaticRefusedCount - s4ProposalBaseline;
                record["mountDisplacement"] = HorizontalDistance(s4HoldPosition, horse.Position);
                s4Baseline = combat.AutomaticDispatchCount;
                Sprint4SetAi(false, "after-stop"); s4StableFrames = 0; s4Step = 2; return;
            }
            if (s4Step == 2)
            {
                if (++s4StableFrames < 5) return;
                Sprint4SetAi(true, "explicit-re-enable"); s4Step = 3; ResetLeafClock(); return;
            }
            if (s4Step == 3)
            {
                if (combat.AutomaticDispatchCount == s4Baseline) return;
                Sprint4Step("afterReEnable");
                s4First = Sprint4Spawn(8f, "s4-neighbor-manual", true);
                s4Step = 4; ResetLeafClock(); return;
            }
            if (s4Step == 4)
            {
                if (!s4First.IsInCombat) return;
                Sprint4Step("beforeManual");
                if (!Sprint4Click(s4First, "manual-replacement")) { FailSprint4Row("The manual replacement click was refused."); return; }
                Sprint4Step("afterManual"); s4Step = 5; ResetLeafClock(); return;
            }
            if (s4Step == 5)
            {
                if (Sprint4Attacks(rider, s4First).Length == 0 && Sprint4Attacks(horse, s4First).Length == 0) return;
                var record = Sprint4Step("manualAttacked");
                record["intentOwnedByPlayer"] = combat.HasStockAttackIntent && !combat.StockIntentIsAutomatic && combat.CurrentStockIntentTarget == s4First;
                s4Baseline = Sprint4Attacks(rider, s4Third).Length + Sprint4Attacks(horse, s4Third).Length;
                s4WaitUntil = Time.time + Sprint4ObservationSeconds; s4Step = 6; ResetLeafClock(); return;
            }
            if (Time.time < s4WaitUntil) return;
            var manual = Sprint4Step("manualHeld");
            manual["attacksOnOldTargetSinceManual"] = Sprint4Attacks(rider, s4Third).Length + Sprint4Attacks(horse, s4Third).Length - s4Baseline;
            manual["manualTargetStillOwned"] = combat.CurrentStockIntentTarget == s4First && !combat.StockIntentIsAutomatic;
            FinishSprint4Row(true, "Stop, re-enable and manual replacement observations recorded.");
        }

        // ---- Turn-based neighbor: ordinary stock attacks by each actor, no automatic attack and no automatic End Turn ---
        private void TickSprint4TurnBased(TurnController turn)
        {
            if (s4Step == 0)
            {
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                BeginTarget(3.5f, "s4-tb"); ruleProbe.Arm(target, false);
                castingCosts.BeginEncounter(request.RunId + ":sprint4-tb");
                s4Step = 1; ResetLeafClock(); return;
            }
            if (s4Step == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat) return;
                if (WaitForNativeCastingPrincipal(true, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                if (!rider.HasStandardAction() || !Sprint4TurnReady()) return;
                s4Turn = turn; s4Facts["riderBefore"] = castingCosts.Snapshot(rider); s4Facts["mountBefore"] = castingCosts.Snapshot(horse);
                if (!Sprint4Click(target, "rider-attack")) { FailSprint4Row("The rider's ordinary click was refused."); return; }
                Sprint4Step("riderInput"); s4Step = 2; ResetLeafClock(); return;
            }
            if (s4Step == 2)
            {
                if (Sprint4Attacks(rider, target).Length == 0) return;
                if (!ReferenceEquals(turn, s4Turn)) { FailSprint4Row("The rider's turn ended without End Turn."); return; }
                if (combat.HasActiveCommand || rider.Commands.Standard != null) return;
                Sprint4Step("riderAttacked"); s4Facts["riderAfter"] = castingCosts.Snapshot(rider); s4Facts["mountAfterRider"] = castingCosts.Snapshot(horse);
                s4StableFrames = 0; s4Step = 3; return;
            }
            if (s4Step == 3)
            {
                if (!ReferenceEquals(turn, s4Turn)) { FailSprint4Row("The rider's turn ended without End Turn."); return; }
                if (++s4StableFrames < 60) return;
                Sprint4Step("noAutomaticAfterRider");
                // The mount acts through ordinary selection and the same hostile click, on its own resources.
                SelectionManager.Instance.SelectUnit(horse.View, true, true, false);
                bool clicked;
                using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); clicked = input.Click(); }
                ((JArray)s4Facts["inputs"]).Add(new JObject { ["kind"] = "hostile-click", ["name"] = "mount-attack", ["target"] = target.UniqueId, ["clicked"] = clicked, ["frame"] = Time.frameCount });
                Sprint4Step("mountInput");
                if (!clicked) { FailSprint4Row("The mount's ordinary click was refused."); return; }
                s4Step = 4; ResetLeafClock(); return;
            }
            if (s4Step == 4)
            {
                if (Sprint4Attacks(horse, target).Length == 0) return;
                if (combat.HasActiveCommand || horse.Commands.Standard != null) return;
                Sprint4Step("mountAttacked"); s4Facts["mountAfter"] = castingCosts.Snapshot(horse); s4Facts["riderAfterMount"] = castingCosts.Snapshot(rider);
                s4StableFrames = 0; s4Step = 5; return;
            }
            if (++s4StableFrames < 60) return;
            var settled = Sprint4Step("settled");
            settled["sameTurn"] = ReferenceEquals(turn, s4Turn);
            FinishSprint4Row(true, "Turn-based neighbor observations recorded.");
        }

        private bool Sprint4TurnReady() => rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation;

        private JObject CaptureSprint4DeadlineProgress()
        {
            var progress = new JObject { ["stage"] = s4Stage, ["step"] = s4Step, ["caseIndex"] = s4CaseIndex,
                ["case"] = s4CaseIndex < Sprint4Cases.Length ? Sprint4Case : null };
            try
            {
                progress["facts"] = s4Facts?.DeepClone();
                if (castingTrace != null && castingCosts != null) progress["state"] = Sprint4State();
            }
            catch (Exception exception) { progress["captureError"] = exception.GetType().Name + ": " + exception.Message; }
            return progress;
        }

        private bool DrainSprint4Fixture()
        {
            if (Game.Instance.IsPaused && s4Pause != null) { Game.Instance.IsPaused = false; s4Pause = null; }
            if (rangedWeaponLease != null) { rangedWeaponLease.Dispose(); rangedWeaponLease = null; }
            if (ordinaryVariation != null) { ordinaryVariation.Dispose(); ordinaryVariation = null; }
            RestorePhase3hRapidShot();
            if (IsSprint4Intent) { rider.HoldState = false; horse.HoldState = false; }
            if (s4RiderAiCaptured) { rider.IsAIEnabled = s4RiderAiBefore; s4RiderAiCaptured = false; CastingMeasurement["riderAiRestored"] = rider.IsAIEnabled == s4RiderAiBefore; }
            if (!Sprint4DestroyTargets()) return false;
            if (!DrainCastingFixture()) return false;
            if (s3Rules != null) { CastingMeasurement["ruleTrace"] = s3Rules.Capture(); s3Rules.Dispose(); s3Rules = null; }
            return true;
        }
    }
}
