using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6 closeout: the seven remaining Chunk 6A boundaries during the exact real-time combat Mount
    // approach. Each isolated case performs one exact native (or, where no native path exists, one exact
    // owned diagnostic) stimulus after the measured non-adjacent approach began, records what the engine
    // and the product did at every step, restores its own stimulus where restoration is possible, and
    // reports whether the sequence settled. Which outcome shapes are lawful is the external validator's
    // decision. Nothing here writes a cooldown, refunds a cost, forces a turn, assigns a position or
    // interrupts the native command itself.
    //
    //   chunk6a-left-area          CM02-left-area          the mount leaves the area (EntityDataBase.set_IsInGame 06007E95)
    //   chunk6a-view-agent-lost    CM02-view-agent-lost    the rider's view is replaced natively (authored Beast Shape I)
    //   chunk6a-loading-cutscene   CM02-loading-cutscene   a native Cutscene game mode starts (Game.StartMode 06000CBD)
    //   chunk6a-generation-change  CM02-generation-change  the exact owned diagnostic generation invalidation
    //   chunk6a-injected-exception CM04-injected-exception the exact owned diagnostic exception at the Mount execution boundary
    //   chunk6a-rider-death-approach CM04-rider-death      real native lethal RuleDealDamage to the rider
    //   chunk6a-mount-death-approach CM04-mount-death      real native lethal RuleDealDamage to the mount
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aLeftAreaScenario = "chunk6a-left-area";
        internal const string Chunk6aViewAgentLostScenario = "chunk6a-view-agent-lost";
        internal const string Chunk6aLoadingCutsceneScenario = "chunk6a-loading-cutscene";
        internal const string Chunk6aGenerationChangeScenario = "chunk6a-generation-change";
        internal const string Chunk6aInjectedExceptionScenario = "chunk6a-injected-exception";
        internal const string Chunk6aRiderDeathApproachScenario = "chunk6a-rider-death-approach";
        internal const string Chunk6aMountDeathApproachScenario = "chunk6a-mount-death-approach";
        internal static readonly string[] Chunk6aInvalidationScenarios =
        {
            Chunk6aLeftAreaScenario, Chunk6aViewAgentLostScenario, Chunk6aLoadingCutsceneScenario, Chunk6aGenerationChangeScenario,
            Chunk6aInjectedExceptionScenario, Chunk6aRiderDeathApproachScenario, Chunk6aMountDeathApproachScenario
        };
        private const int Chunk6aInvalidationSettleFrames = 10;
        private const int Chunk6aInvalidationSampleLimit = 600;

        internal static bool IsChunk6aInvalidationScenario(string scenario) =>
            Array.IndexOf(Chunk6aInvalidationScenarios, scenario) >= 0;

        internal static string Chunk6aInvalidationRowFor(string scenario)
        {
            switch (scenario)
            {
                case Chunk6aLeftAreaScenario: return "CM02-left-area";
                case Chunk6aViewAgentLostScenario: return "CM02-view-agent-lost";
                case Chunk6aLoadingCutsceneScenario: return "CM02-loading-cutscene";
                case Chunk6aGenerationChangeScenario: return "CM02-generation-change";
                case Chunk6aInjectedExceptionScenario: return "CM04-injected-exception";
                case Chunk6aRiderDeathApproachScenario: return "CM04-rider-death";
                case Chunk6aMountDeathApproachScenario: return "CM04-mount-death";
                default: return null;
            }
        }

        internal static string Chunk6aInvalidationContractFor(string scenario)
        {
            switch (scenario)
            {
                case Chunk6aLeftAreaScenario: return "native-area-departure-during-exact-mount-approach";
                case Chunk6aViewAgentLostScenario: return "native-view-replacement-during-exact-mount-approach";
                case Chunk6aLoadingCutsceneScenario: return "native-cutscene-mode-during-exact-mount-approach";
                case Chunk6aGenerationChangeScenario: return "diagnostic-generation-invalidation-before-exact-mount-delivery";
                case Chunk6aInjectedExceptionScenario: return "diagnostic-exception-at-exact-mount-execution-boundary";
                case Chunk6aRiderDeathApproachScenario: return "native-rider-death-during-exact-mount-approach";
                case Chunk6aMountDeathApproachScenario: return "native-mount-death-during-exact-mount-approach";
                default: return null;
            }
        }

        private bool Chunk6aInvalidationOnly => IsChunk6aInvalidationScenario(request.Scenario);
        private bool Chunk6aInvalidationDeath => request.Scenario == Chunk6aRiderDeathApproachScenario || request.Scenario == Chunk6aMountDeathApproachScenario;
        private string Chunk6aInvalidationRow => Chunk6aInvalidationRowFor(request.Scenario);
        private UnitEntityData Chunk6aInvalidationDeathSubject => request.Scenario == Chunk6aRiderDeathApproachScenario ? rider : horse;

        private JObject chunk6aInvalidationEvidence;
        private UnitCommand chunk6aInvalidationCommand;
        private JObject chunk6aInvalidationLedgerBefore;
        private long chunk6aInvalidationGenerationBefore, chunk6aInvalidationDispatchesBefore, chunk6aInvalidationRejectionsBefore,
            chunk6aInvalidationCastsBefore, chunk6aInvalidationShellsBefore;
        private int chunk6aInvalidationTraceStart, chunk6aInvalidationSettled, chunk6aInvalidationTerminalFrame = -1, chunk6aInvalidationStimulusFrame = -1;
        private int chunk6aInvalidationStimulusCount, chunk6aInvalidationRestoreCount, chunk6aInvalidationCleanupInterruptCount;
        private bool chunk6aInvalidationTriggered, chunk6aInvalidationRestored, chunk6aInvalidationHorseLeftArea, chunk6aInvalidationCutsceneStarted, chunk6aInvalidationResurrected;
        // The native cutscene lock is held while the exact pending shell runs; if the engine freezes the party's commands
        // under the lock (no terminal arrives), the lock is released after this bounded hold and the shell is left to
        // reach its own terminal in the Default mode. Both shapes are recorded; the external validator decides.
        private const int Chunk6aInvalidationCutsceneHoldFrames = 180;
        private NativeChargeViewLease chunk6aInvalidationViewLease;
        private NativeDeathPolicyLease chunk6aInvalidationDeathPolicy;
        private PairedConditionObserver chunk6aInvalidationDeathObserver;
        private readonly JArray chunk6aInvalidationSamples = new JArray();

        private JObject CaptureChunk6aInvalidationActor(UnitEntityData actor)
        {
            var view = actor?.View;
            return actor == null ? null : new JObject
            {
                ["id"] = actor.UniqueId,
                ["inGame"] = actor.IsInGame,
                ["inState"] = actor.IsInState,
                ["viewPresent"] = view != null,
                ["viewObject"] = view == null ? 0 : view.GetInstanceID(),
                ["viewActive"] = view != null && view.gameObject.activeSelf,
                ["viewBound"] = view != null && ReferenceEquals(view.EntityData, actor),
                ["agentPresent"] = view?.AgentASP != null,
                ["agentEnabled"] = view?.AgentASP != null && view.AgentASP.enabled,
                ["agentOverride"] = view?.AgentOverride == null ? 0 : RuntimeHelpers.GetHashCode(view.AgentOverride),
                ["groupId"] = actor.GroupId,
                ["directlyControllable"] = actor.IsDirectlyControllable,
                ["conscious"] = actor.Descriptor.State.IsConscious,
                ["dead"] = actor.Descriptor.State.IsDead,
                ["finallyDead"] = actor.Descriptor.State.IsFinallyDead,
                ["damage"] = actor.Damage,
                ["hitPoints"] = actor.Stats.HitPoints.ModifiedValue,
                ["polymorphed"] = actor.Body.IsPolymorphed,
                ["commandsEmpty"] = actor.Commands.Empty,
                ["reallyMoving"] = view?.AgentASP?.IsReallyMoving ?? false,
                ["cooldowns"] = Chunk6aCooldowns(actor)
            };
        }

        private JObject CaptureChunk6aInvalidationState(string kind)
        {
            var game = Game.Instance;
            var state = CaptureChunk6aLifecycleState(kind);
            state["currentMode"] = game.CurrentMode.ToString();
            state["cutsceneLock"] = game.CutsceneLock;
            state["loadingInProcess"] = Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess;
            state["riderActor"] = CaptureChunk6aInvalidationActor(rider);
            state["mountActor"] = CaptureChunk6aInvalidationActor(horse);
            state["lastShellRefusal"] = nativeControls.LastRelationshipShellRefusal;
            state["diagnosticGenerationInvalidations"] = relationship.DiagnosticGenerationInvalidationCount;
            state["mountExecutionFaultArmed"] = MountedRelationshipDeliveryFault.BeforeMountExecution != null;
            state["generationFaultArmed"] = MountedRelationshipDeliveryFault.BeforeShellGenerationCheck != null;
            return state;
        }

        private void TickChunk6aApproachInvalidation()
        {
            if (!Chunk6aInvalidationOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("An approach invalidation case requires its isolated real-time transaction.");
            if (chunk6aStage != 51 && chunk6aInvalidationEvidence == null)
            {
                FailCurrent(Chunk6aInvalidationRow, "The approach invalidation tick was entered at stage " + chunk6aStage + " before its click stage 51.");
                BeginCleanup();
                return;
            }
            if (chunk6aStage == 51)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aInvalidationRow)) return;
                var start = CaptureChunk6aGeometry("invalidation-pre-click");
                if ((bool)start["isAdjacent"]) throw new InvalidOperationException("The approach invalidation Mount must start outside transition reach.");
                if (!Chunk6aInvalidationFixtureReady(out var fixtureRefusal))
                {
                    observations["chunk6aApproachInvalidation"] = new JObject { ["contract"] = Chunk6aInvalidationContractFor(request.Scenario), ["fixtureRefusal"] = fixtureRefusal };
                    FailCurrent(Chunk6aInvalidationRow, "The approach invalidation fixture is not exact: " + fixtureRefusal);
                    BeginCleanup();
                    return;
                }
                chunk6aInvalidationLedgerBefore = Chunk6aLedgerCounters();
                chunk6aInvalidationGenerationBefore = relationship.MountedPairGeneration;
                chunk6aInvalidationDispatchesBefore = nativeControls.DispatchAcceptedCount;
                chunk6aInvalidationRejectionsBefore = nativeControls.DispatchRejectedCount;
                chunk6aInvalidationCastsBefore = nativeControls.NativeCastRequestCount;
                chunk6aInvalidationShellsBefore = nativeControls.NativeRelationshipShellCount;
                chunk6aInvalidationTraceStart = allocationTrace.EventCount;
                chunk6aInvalidationEvidence = new JObject
                {
                    ["contract"] = Chunk6aInvalidationContractFor(request.Scenario),
                    ["scenario"] = request.Scenario,
                    ["row"] = Chunk6aInvalidationRow,
                    ["diagnosticStimulus"] = request.Scenario == Chunk6aGenerationChangeScenario || request.Scenario == Chunk6aInjectedExceptionScenario,
                    // Every stimulus this family owns is restored in-process (a dead subject through the engine's own
                    // UnitDescriptor.ResurrectAndFullRestore after its death was observed and recorded).
                    ["externalRestorationRequired"] = false,
                    ["start"] = start,
                    ["before"] = CaptureChunk6aInvalidationState("before-click"),
                    ["samples"] = chunk6aInvalidationSamples,
                    ["stimulusCount"] = 0,
                    ["restorationCount"] = 0,
                    ["diagnosticInterruptCount"] = 0
                };
                observations["chunk6aApproachInvalidation"] = chunk6aInvalidationEvidence;
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-approach-invalidation-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                chunk6aInvalidationCommand = chunk6aCommandWindow.Command;
                chunk6aLifecycleCommand = chunk6aInvalidationCommand;
                chunk6aInvalidationEvidence["click"] = observations["chunk6a-approach-invalidation-click"]?.DeepClone();
                chunk6aInvalidationEvidence["afterClick"] = CaptureChunk6aInvalidationState("after-click");
                if (!clicked || chunk6aInvalidationCommand == null)
                {
                    FailCurrent(Chunk6aInvalidationRow, "Exact rider native Mount click was refused: " + playerAction.LastFeedback);
                    BeginCleanup();
                    return;
                }
                chunk6aStage = 52;
                ResetLeafClock();
                return;
            }

            var command = chunk6aInvalidationCommand;
            if (chunk6aInvalidationSamples.Count < Chunk6aInvalidationSampleLimit) chunk6aInvalidationSamples.Add(CaptureChunk6aInvalidationState("tick"));
            if (!chunk6aInvalidationTriggered)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished ||
                    (command as UnitUseAbility)?.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aInvalidationRow, "The exact Mount left pending approach before the invalidation stimulus.");
                    BeginCleanup();
                    return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aInvalidationEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("invalidation-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider)
                {
                    FailCurrent(Chunk6aInvalidationRow, "The invalidation missed the exact selected rider's non-adjacent pending Mount approach.");
                    BeginCleanup();
                    return;
                }
                var trigger = CaptureChunk6aInvalidationTrigger(command, slot, geometry, moved);
                trigger["state"] = CaptureChunk6aInvalidationState("trigger");
                chunk6aInvalidationEvidence["trigger"] = trigger;
                chunk6aInvalidationTriggered = true;
                string stimulusRefusal;
                var stimulus = ApplyChunk6aInvalidationStimulus(command, out stimulusRefusal);
                chunk6aInvalidationEvidence["stimulus"] = stimulus;
                chunk6aInvalidationEvidence["stimulusCount"] = chunk6aInvalidationStimulusCount;
                if (stimulusRefusal != null)
                {
                    FailCurrent(Chunk6aInvalidationRow, "The exact invalidation stimulus was not delivered: " + stimulusRefusal);
                    BeginCleanup();
                    return;
                }
                ResetLeafClock();
                return;
            }
            // Bounded cutscene hold: a shell the engine froze under the native lock never reaches a terminal; release
            // the lock once after the hold and record the frame so the validator can see that no delivery happened
            // while the cutscene was active.
            if (request.Scenario == Chunk6aLoadingCutsceneScenario && chunk6aInvalidationCutsceneStarted && Game.Instance.CutsceneLock &&
                !command.IsFinished && chunk6aInvalidationStimulusFrame >= 0 && Time.frameCount - chunk6aInvalidationStimulusFrame >= Chunk6aInvalidationCutsceneHoldFrames &&
                chunk6aInvalidationEvidence["cutsceneHoldExpired"] == null)
            {
                chunk6aInvalidationEvidence["cutsceneHoldExpired"] = new JObject
                {
                    ["frame"] = Time.frameCount, ["holdFrames"] = Chunk6aInvalidationCutsceneHoldFrames,
                    ["command"] = CaptureOrdinaryCommand(command), ["state"] = CaptureChunk6aInvalidationState("hold-expired")
                };
                RestoreChunk6aInvalidationStimulus(false);
                ResetLeafClock();
                return;
            }
            if (!command.IsFinished || !Chunk6aIdle) { chunk6aInvalidationSettled = 0; return; }
            // The death guard waits for the native life state only until the fixture's own resurrection has been issued;
            // afterwards the restoration is re-evaluated every frame until UnitLifeController.TickOnUnit (06009162) has
            // recomputed Conscious: Resurrect (06001F13) clears FinallyDead, the damage and the conditions but never sets
            // LifeState itself (preview.207 stages 6/7 evaluated the restoration in the resurrection frame and never re-entered).
            if (Chunk6aInvalidationDeath && !chunk6aInvalidationResurrected && !Chunk6aInvalidationDeathSubject.Descriptor.State.IsDead) { chunk6aInvalidationSettled = 0; return; }
            if (chunk6aInvalidationTerminalFrame < 0)
            {
                chunk6aInvalidationTerminalFrame = Time.frameCount;
                chunk6aInvalidationEvidence["terminal"] = CaptureChunk6aInvalidationState("terminal");
                chunk6aInvalidationEvidence["terminalCommand"] = CaptureOrdinaryCommand(command);
            }
            if (++chunk6aInvalidationSettled < Chunk6aInvalidationSettleFrames) return;
            if (!chunk6aInvalidationRestored)
            {
                if (!RestoreChunk6aInvalidationStimulus(false)) return;
                chunk6aInvalidationRestored = true;
                chunk6aInvalidationSettled = 0;
                ResetLeafClock();
                return;
            }
            CompleteChunk6aApproachInvalidation();
        }

        private bool Chunk6aInvalidationFixtureReady(out string refusal)
        {
            refusal = null;
            switch (request.Scenario)
            {
                case Chunk6aLeftAreaScenario:
                    if (!horse.IsInGame || horse.View == null || !rider.IsInGame) refusal = "the pair is not both in game with live views";
                    break;
                case Chunk6aViewAgentLostScenario:
                    if (rider.View == null || rider.GetActivePolymorph() != null || rider.Body.IsPolymorphed) refusal = "the rider is not the stock unpolymorphed rider";
                    break;
                case Chunk6aLoadingCutsceneScenario:
                    if (Game.Instance.CurrentMode != GameModeType.Default || Game.Instance.CutsceneLock) refusal = "the game is not in the Default mode without a cutscene lock";
                    break;
                case Chunk6aGenerationChangeScenario:
                    if (MountedRelationshipDeliveryFault.BeforeShellGenerationCheck != null || relationship.DiagnosticGenerationInvalidationCount != 0) refusal = "a diagnostic generation invalidation is already armed or consumed";
                    break;
                case Chunk6aInjectedExceptionScenario:
                    if (MountedRelationshipDeliveryFault.BeforeMountExecution != null) refusal = "a diagnostic Mount execution fault is already armed";
                    break;
                case Chunk6aRiderDeathApproachScenario:
                case Chunk6aMountDeathApproachScenario:
                    var subject = Chunk6aInvalidationDeathSubject;
                    if (!subject.Descriptor.State.IsConscious || subject.Descriptor.State.IsDead || subject.Descriptor.IsEssentialForGame ||
                        subject.Descriptor.State.Immortality || subject == Game.Instance.Player.MainCharacter.Value ||
                        target == null || !target.IsInState || !target.Descriptor.State.IsConscious || !target.IsPlayersEnemy || target.IsPlayerFaction)
                        refusal = "the death case lacks a disposable conscious non-essential subject and a live native enemy source";
                    break;
            }
            return refusal == null;
        }

        private JObject ApplyChunk6aInvalidationStimulus(UnitCommand command, out string refusal)
        {
            refusal = null;
            var game = Game.Instance;
            var stimulus = new JObject
            {
                ["frameBefore"] = Time.frameCount, ["gameTicksBefore"] = game.TimeController.GameTime.Ticks,
                ["commandBefore"] = CaptureOrdinaryCommand(command),
                ["riderBefore"] = CaptureChunk6aInvalidationActor(rider), ["mountBefore"] = CaptureChunk6aInvalidationActor(horse)
            };
            chunk6aInvalidationStimulusCount++;
            switch (request.Scenario)
            {
                case Chunk6aLeftAreaScenario:
                    stimulus["contract"] = "one-native-entity-is-in-game-false-on-the-exact-mount";
                    stimulus["method"] = "Kingmaker.EntitySystem.EntityDataBase.set_IsInGame"; stimulus["token"] = "06007E95";
                    horse.IsInGame = false;
                    chunk6aInvalidationHorseLeftArea = true;
                    if (horse.IsInGame) refusal = "the mount remained in game";
                    break;
                case Chunk6aViewAgentLostScenario:
                    stimulus["contract"] = "one-authored-beast-shape-view-replacement-on-the-exact-rider";
                    try
                    {
                        chunk6aInvalidationViewLease = new NativeChargeViewLease(rider);
                        chunk6aInvalidationViewLease.Apply();
                        stimulus["lease"] = chunk6aInvalidationViewLease.Evidence.DeepClone();
                    }
                    catch (InvalidOperationException exception) { refusal = exception.Message; }
                    break;
                case Chunk6aLoadingCutsceneScenario:
                    // The engine's own cutscene entry (CommandLockControls.OnRun -> Game.SetCutsceneLock 06000CE6): the
                    // counting-guard lock starts the Cutscene game mode and holds it; a bare StartMode(Cutscene) without the
                    // lock reverted to Default within one frame (preview.206 stage 3).
                    stimulus["contract"] = "one-native-cutscene-lock-with-its-cutscene-game-mode";
                    stimulus["method"] = "Kingmaker.Game.SetCutsceneLock"; stimulus["token"] = "06000CE6";
                    stimulus["modeBefore"] = game.CurrentMode.ToString(); stimulus["lockBefore"] = game.CutsceneLock;
                    game.SetCutsceneLock(true);
                    chunk6aInvalidationCutsceneStarted = true;
                    stimulus["lockAfterRequest"] = game.CutsceneLock; stimulus["modeAfterRequest"] = game.CurrentMode.ToString();
                    if (!game.CutsceneLock) refusal = "the native cutscene lock did not engage";
                    break;
                case Chunk6aGenerationChangeScenario:
                    stimulus["contract"] = "one-owned-diagnostic-generation-invalidation-before-the-shell-generation-check";
                    stimulus["generationBefore"] = relationship.MountedPairGeneration;
                    MountedRelationshipDeliveryFault.BeforeShellGenerationCheck = () =>
                        relationship.InvalidateMountedPairGenerationForDiagnostics(Chunk6aGenerationChangeScenario);
                    stimulus["armed"] = true;
                    break;
                case Chunk6aInjectedExceptionScenario:
                    stimulus["contract"] = "one-owned-diagnostic-exception-at-the-native-mount-execution-boundary";
                    MountedRelationshipDeliveryFault.BeforeMountExecution = () =>
                        throw new InvalidOperationException("Diagnostic Chunk 6A native Mount execution fault.");
                    stimulus["armed"] = true;
                    break;
                case Chunk6aRiderDeathApproachScenario:
                case Chunk6aMountDeathApproachScenario:
                    stimulus["contract"] = "one-native-lethal-ruledeal-damage-under-permanent-death-policy";
                    var subject = Chunk6aInvalidationDeathSubject;
                    chunk6aInvalidationDeathPolicy = new NativeDeathPolicyLease(true);
                    stimulus["policy"] = chunk6aInvalidationDeathPolicy.Capture();
                    chunk6aInvalidationDeathObserver = new PairedConditionObserver(rider, horse, true);
                    var difficulty = game.Player.Difficulty.DamageToParty;
                    var threshold = checked(subject.Stats.HitPoints.ModifiedValue + subject.Stats.Constitution.ModifiedValue);
                    var needed = checked(threshold + 1 - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue);
                    if (difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty) || needed <= 0)
                    {
                        refusal = "no finite positive native lethal damage window";
                        break;
                    }
                    var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
                    stimulus["subjectId"] = subject.UniqueId; stimulus["sourceId"] = target.UniqueId;
                    stimulus["damageToParty"] = difficulty; stimulus["deathThreshold"] = threshold;
                    stimulus["requestedDamage"] = requested; stimulus["damageDispatches"] = 1;
                    var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                        new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                    stimulus["nativeDamage"] = damage.Damage;
                    stimulus["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
                    if (damage.DamageBeforeDifficulty != requested) refusal = "the native rule did not carry the exact requested damage";
                    break;
            }
            stimulus["count"] = chunk6aInvalidationStimulusCount;
            chunk6aInvalidationStimulusFrame = Time.frameCount;
            stimulus["frameAfter"] = Time.frameCount; stimulus["gameTicksAfter"] = game.TimeController.GameTime.Ticks;
            stimulus["commandAfter"] = CaptureOrdinaryCommand(command);
            stimulus["riderAfter"] = CaptureChunk6aInvalidationActor(rider); stimulus["mountAfter"] = CaptureChunk6aInvalidationActor(horse);
            return stimulus;
        }

        // Restores only what this case changed and only through the same native or owned path, once; a dead subject is
        // restored in-process through the engine's own resurrection entry after its death was recorded.
        private bool RestoreChunk6aInvalidationStimulus(bool cleanup)
        {
            var game = Game.Instance;
            JObject record = chunk6aInvalidationEvidence == null ? null : (JObject)chunk6aInvalidationEvidence[cleanup ? "cleanupRestoration" : "restoration"];
            if (record == null)
            {
                record = new JObject { ["cleanup"] = cleanup, ["attempts"] = 0, ["frameFirst"] = Time.frameCount };
                if (chunk6aInvalidationEvidence != null) chunk6aInvalidationEvidence[cleanup ? "cleanupRestoration" : "restoration"] = record;
            }
            record["attempts"] = (int)record["attempts"] + 1;
            var restored = true;
            switch (request.Scenario)
            {
                case Chunk6aLeftAreaScenario:
                    if (chunk6aInvalidationHorseLeftArea)
                    {
                        if (!horse.IsInGame) { horse.IsInGame = true; chunk6aInvalidationRestoreCount++; }
                        restored = horse.IsInGame && horse.View != null && horse.IsInState;
                        if (restored) chunk6aInvalidationHorseLeftArea = false;
                    }
                    break;
                case Chunk6aViewAgentLostScenario:
                    if (chunk6aInvalidationViewLease != null)
                    {
                        if ((int)record["attempts"] == 1) chunk6aInvalidationRestoreCount++;
                        restored = chunk6aInvalidationViewLease.RestoreWhenSettled();
                        record["lease"] = chunk6aInvalidationViewLease.Evidence.DeepClone();
                        if (restored) chunk6aInvalidationViewLease = null;
                    }
                    break;
                case Chunk6aLoadingCutsceneScenario:
                    if (chunk6aInvalidationCutsceneStarted)
                    {
                        if (game.CutsceneLock)
                        {
                            // The same exact native entry releases the lock and stops the Cutscene mode (deferred by the
                            // engine to its next mode tick when modes are ticking).
                            record["method"] = "Kingmaker.Game.SetCutsceneLock"; record["token"] = "06000CE6";
                            record["releaseFrame"] = Time.frameCount; record["releaseGameTicks"] = game.TimeController.GameTime.Ticks;
                            record["commandAtRelease"] = chunk6aInvalidationCommand == null ? null : CaptureOrdinaryCommand(chunk6aInvalidationCommand);
                            game.SetCutsceneLock(false);
                            chunk6aInvalidationRestoreCount++;
                        }
                        restored = !game.CutsceneLock && game.CurrentMode == GameModeType.Default;
                        record["lockNow"] = game.CutsceneLock; record["modeNow"] = game.CurrentMode.ToString();
                        if (restored) chunk6aInvalidationCutsceneStarted = false;
                    }
                    break;
                case Chunk6aGenerationChangeScenario:
                    record["armedAtRestoration"] = MountedRelationshipDeliveryFault.BeforeShellGenerationCheck != null;
                    MountedRelationshipDeliveryFault.BeforeShellGenerationCheck = null;
                    break;
                case Chunk6aInjectedExceptionScenario:
                    record["armedAtRestoration"] = MountedRelationshipDeliveryFault.BeforeMountExecution != null;
                    MountedRelationshipDeliveryFault.BeforeMountExecution = null;
                    break;
                case Chunk6aRiderDeathApproachScenario:
                case Chunk6aMountDeathApproachScenario:
                    if (chunk6aInvalidationDeathObserver != null)
                    {
                        record["nativeLifeEvents"] = chunk6aInvalidationDeathObserver.Capture();
                        chunk6aInvalidationDeathObserver.Dispose(); chunk6aInvalidationDeathObserver = null;
                    }
                    if (chunk6aInvalidationDeathPolicy != null)
                    {
                        chunk6aInvalidationDeathPolicy.Dispose();
                        record["policy"] = chunk6aInvalidationDeathPolicy.Capture();
                        chunk6aInvalidationDeathPolicy = null;
                        chunk6aInvalidationRestoreCount++;
                    }
                    // The fixture's own lethal stimulus is restored in-process through the engine's resurrection entry
                    // (UnitDescriptor.ResurrectAndFullRestore 06001F12, the path the Horse engine already uses) once the
                    // death was observed and recorded; restoration is complete only when the subject is conscious, undamaged,
                    // in state with a live view and no pending command.
                    var deathSubject = Chunk6aInvalidationDeathSubject;
                    if (record["subjectDeadBefore"] == null) record["subjectDeadBefore"] = deathSubject.Descriptor.State.IsDead;
                    if (!chunk6aInvalidationResurrected && deathSubject.Descriptor.State.IsDead)
                    {
                        record["resurrection"] = new JObject
                        {
                            ["method"] = "Kingmaker.UnitLogic.UnitDescriptor.ResurrectAndFullRestore", ["token"] = "06001F12",
                            ["frame"] = Time.frameCount, ["damageBefore"] = deathSubject.Damage, ["finallyDeadBefore"] = deathSubject.Descriptor.State.IsFinallyDead
                        };
                        deathSubject.Descriptor.ResurrectAndFullRestore();
                        chunk6aInvalidationResurrected = true;
                    }
                    restored = chunk6aInvalidationResurrected && !deathSubject.Descriptor.State.IsDead && deathSubject.Descriptor.State.IsConscious &&
                        deathSubject.Damage == 0 && deathSubject.IsInState && deathSubject.View != null && deathSubject.Commands.Empty;
                    record["resurrected"] = chunk6aInvalidationResurrected;
                    record["subjectDead"] = deathSubject.Descriptor.State.IsDead;
                    record["subjectConscious"] = deathSubject.Descriptor.State.IsConscious;
                    record["subjectDamage"] = deathSubject.Damage;
                    record["subjectInState"] = deathSubject.IsInState;
                    break;
            }
            record["restored"] = restored;
            record["frameLast"] = Time.frameCount;
            record["riderAfter"] = CaptureChunk6aInvalidationActor(rider); record["mountAfter"] = CaptureChunk6aInvalidationActor(horse);
            if (chunk6aInvalidationEvidence != null) chunk6aInvalidationEvidence["restorationCount"] = chunk6aInvalidationRestoreCount;
            return restored;
        }

        private void CompleteChunk6aApproachInvalidation()
        {
            var command = chunk6aInvalidationCommand;
            var acted = command.IsActed;
            var proof = chunk6aCommandWindow.Capture();
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            var after = CaptureChunk6aInvalidationState("after");
            var events = allocationTrace.EventsSince(chunk6aInvalidationTraceStart);
            var pairEvents = events.OfType<JObject>().Where(e =>
                (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId).ToArray();
            Func<JObject, string, bool> boundaryStarts = (e, prefix) => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal);
            var ledgerDelta = new JObject();
            var ledgerNow = Chunk6aLedgerCounters();
            foreach (var item in chunk6aInvalidationLedgerBefore.Properties())
                ledgerDelta[item.Name] = (long)ledgerNow[item.Name] - (long)item.Value;
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && rider.Commands.Empty && horse.Commands.Empty &&
                nativeControls.NativeRelationshipShellCount == chunk6aInvalidationShellsBefore + 1 &&
                relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null &&
                combat.PairedActivationIdentity == null && combat.PairedPartnerContext == null;
            chunk6aInvalidationEvidence["commandWindow"] = proof;
            chunk6aInvalidationEvidence["terminalCommand"] = CaptureOrdinaryCommand(command);
            chunk6aInvalidationEvidence["after"] = after;
            chunk6aInvalidationEvidence["allocationEvents"] = events;
            chunk6aInvalidationEvidence["allocationTraceComplete"] = allocationTrace.Complete;
            chunk6aInvalidationEvidence["observerHooks"] = allocationTrace.ObserverHooks;
            chunk6aInvalidationEvidence["interrupts"] = new JArray(events.OfType<JObject>()
                .Where(e => (string)e["boundary"] == "command-interrupt-before").Select(e => e.DeepClone()));
            chunk6aInvalidationEvidence["pairCostCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "cost-") || boundaryStarts(e, "actor-cost-"));
            chunk6aInvalidationEvidence["pairPrepareCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "prepare-"));
            chunk6aInvalidationEvidence["ledgerDelta"] = ledgerDelta;
            chunk6aInvalidationEvidence["generationDelta"] = relationship.MountedPairGeneration - chunk6aInvalidationGenerationBefore;
            chunk6aInvalidationEvidence["dispatchAcceptedDelta"] = nativeControls.DispatchAcceptedCount - chunk6aInvalidationDispatchesBefore;
            chunk6aInvalidationEvidence["dispatchRejectedDelta"] = nativeControls.DispatchRejectedCount - chunk6aInvalidationRejectionsBefore;
            chunk6aInvalidationEvidence["castRequestDelta"] = nativeControls.NativeCastRequestCount - chunk6aInvalidationCastsBefore;
            chunk6aInvalidationEvidence["relationshipShellsDelta"] = nativeControls.NativeRelationshipShellCount - chunk6aInvalidationShellsBefore;
            chunk6aInvalidationEvidence["diagnosticInterruptCount"] = chunk6aInvalidationCleanupInterruptCount;
            chunk6aInvalidationEvidence["noResidue"] = noResidue;
            chunk6aInvalidationEvidence["outcome"] = relationship.State == RelationshipState.Mounted ? "delivered" :
                acted ? "acted-not-mounted" : "unacted-" + command.Result;
            chunk6aInvalidationEvidence["settledFrames"] = chunk6aInvalidationSettled;
            chunk6aInvalidationEvidence["feedback"] = playerAction.LastFeedback;
            chunk6aInvalidationEvidence["lastShellRefusal"] = nativeControls.LastRelationshipShellRefusal;
            chunk6aInvalidationEvidence["diagnosticGenerationInvalidations"] = relationship.DiagnosticGenerationInvalidationCount;
            chunk6aInvalidationEvidence["lastDiagnosticGenerationInvalidation"] = relationship.LastDiagnosticGenerationInvalidation;
            var complete = chunk6aInvalidationEvidence["trigger"] != null && chunk6aInvalidationEvidence["terminal"] != null &&
                chunk6aInvalidationEvidence["stimulus"] != null && command.IsFinished && allocationTrace.Complete &&
                chunk6aInvalidationStimulusCount == 1 && chunk6aInvalidationRestored;
            AddRow(Chunk6aInvalidationRow, complete,
                complete
                    ? "The exact stimulus was delivered during the measured Mount approach and the sequence settled with its stimulus restored where restoration is possible; the recorded outcome, residue and resource facts are the external validator's to judge."
                    : "The approach invalidation sequence did not record every observation.",
                chunk6aInvalidationEvidence);
            chunk6aStage = 99;
            BeginCleanup();
        }

        private bool CaptureChunk6aApproachInvalidationDeadline()
        {
            if (!Chunk6aInvalidationOnly || chunk6aStage < 51 || chunk6aStage > 52) return false;
            observations["chunk6aApproachInvalidationDeadline"] = new JObject
            {
                ["window"] = chunk6aCommandWindow?.Capture(),
                ["state"] = chunk6aInvalidationEvidence == null ? null : CaptureChunk6aInvalidationState("deadline"),
                ["triggered"] = chunk6aInvalidationTriggered,
                ["restored"] = chunk6aInvalidationRestored
            };
            FailCurrent(Chunk6aInvalidationRow, "The exact approach, invalidation stimulus, terminal or restoration did not settle at the unchanged 30-second deadline.");
            return true;
        }

        // Every stimulus this family owns is disarmed or restored on every abort path so it can never outlive
        // its own row: the in-game flag, the authored view lease, the cutscene mode, both diagnostic seams and
        // the death policy lease. A still-live exact pending command is interrupted and counted.
        private void CleanupChunk6aApproachInvalidation()
        {
            if (!Chunk6aInvalidationOnly || !chunk6aInvalidationTriggered) return;
            if (chunk6aInvalidationCommand != null && !chunk6aInvalidationCommand.IsFinished)
            {
                chunk6aInvalidationCommand.Interrupt(); chunk6aInvalidationCleanupInterruptCount++;
                if (chunk6aInvalidationEvidence != null) chunk6aInvalidationEvidence["cleanupInterruptCount"] = chunk6aInvalidationCleanupInterruptCount;
            }
            MountedRelationshipDeliveryFault.BeforeShellGenerationCheck = null;
            MountedRelationshipDeliveryFault.BeforeMountExecution = null;
            if (chunk6aInvalidationRestored) return;
            var restored = RestoreChunk6aInvalidationStimulus(true);
            if (!restored && request.Scenario == Chunk6aViewAgentLostScenario && chunk6aInvalidationViewLease != null)
            {
                // The lease restores only when the native replacement has settled; the shared cleanup may
                // run before that frame. Dispose asserts restoration and names the remaining ownership.
                chunk6aInvalidationViewLease.Dispose(); chunk6aInvalidationViewLease = null; restored = true;
            }
            if (!restored) throw new InvalidOperationException("Approach invalidation cleanup did not restore its exact stimulus: " + request.Scenario);
        }
    }
}
