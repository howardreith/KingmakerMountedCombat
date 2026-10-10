using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6 closeout: the two turn-based boundaries of the Chunk 6A sequence.
    //
    //   chunk6a-turn-end-approach-tb  CM04-turn-end   the one native End Turn input (Game.PauseBind 06000CB7, the same
    //                                                 input CM03-early-end-turn uses) during the exact measured turn-based
    //                                                 Mount approach; the pending shell must retire with valid actors and
    //                                                 no residue.
    //   chunk6a-mode-exit-tb          CM04-mode-exit  the native turn-based mode exit (the exact registered settings
    //                                                 callback and EventBus path, as the 6B lifecycle's native-mode-change
    //                                                 case) while the pair is mounted on its adopted turn; the paired
    //                                                 activation must be forfeited exactly once with no duplicate cost.
    //
    // Both ride the unchanged turn-based positive Mount flow (pointer click, stage 2). The scenario performs the
    // boundary, records what the engine and the product did at every step and whether the sequence settled;
    // which outcome shapes are lawful is the external validator's decision. Nothing here writes a cooldown,
    // refunds a cost, forces a turn or interrupts the native command itself.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aTurnEndApproachScenario = "chunk6a-turn-end-approach-tb";
        internal const string Chunk6aModeExitScenario = "chunk6a-mode-exit-tb";
        private const int Chunk6aTurnBoundarySettleFrames = 10;
        private const int Chunk6aTurnBoundarySampleLimit = 600;

        internal static bool IsChunk6aTurnBoundaryScenario(string scenario) =>
            string.Equals(scenario, Chunk6aTurnEndApproachScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aModeExitScenario, StringComparison.Ordinal);

        private bool Chunk6aTurnEndOnly => string.Equals(request.Scenario, Chunk6aTurnEndApproachScenario, StringComparison.Ordinal);
        private bool Chunk6aModeExitOnly => string.Equals(request.Scenario, Chunk6aModeExitScenario, StringComparison.Ordinal);

        private JObject chunk6aTurnEndEvidence;
        private UnitCommand chunk6aTurnEndCommand;
        private TurnController chunk6aTurnEndTurn;
        private JObject chunk6aTurnEndLedgerBefore;
        private long chunk6aTurnEndGenerationBefore, chunk6aTurnEndDispatchesBefore, chunk6aTurnEndRejectionsBefore, chunk6aTurnEndShellsBefore;
        private int chunk6aTurnEndTraceStart, chunk6aTurnEndSettled, chunk6aTurnEndRound;
        private bool chunk6aTurnEndIssued;
        private readonly JArray chunk6aTurnEndSamples = new JArray();

        private JObject chunk6aModeExitEvidence;
        private NativeModeTransitionProbe chunk6aModeExitProbe;
        private JObject chunk6aModeExitLedgerBefore;
        private int chunk6aModeExitTraceStart, chunk6aModeExitSettled;
        private bool chunk6aModeExitRestored;
        private readonly JArray chunk6aModeExitSamples = new JArray();

        private JObject CaptureChunk6aTurnBoundaryState(string kind, UnitCommand command)
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var previous = chunk6aLifecycleCommand;
            chunk6aLifecycleCommand = command;
            JObject state;
            try { state = CaptureChunk6aLifecycleState(kind); }
            finally { chunk6aLifecycleCommand = previous; }
            state["turnBasedSetting"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue;
            state["turnBasedCombat"] = CombatController.IsInTurnBasedCombat();
            state["controllerInitialized"] = controller != null && controller.Initialized;
            state["round"] = controller == null ? 0 : controller.RoundNumber;
            state["turnObject"] = turn == null ? 0 : RuntimeHelpers.GetHashCode(turn);
            state["turnActor"] = turn?.Unit?.UniqueId;
            state["turnStatus"] = turn?.Status.ToString();
            state["turnIsActing"] = turn?.IsActing;
            // CanEndTurn is the UI End Turn admission (InGameInputLayerView.OnTurnBasedEndTurn 06005D30, the in-game menu's
            // m_CanEndTurn 06003E87); CanEndTurnAndNoActing additionally requires an empty command queue and is the Space-key
            // admission (Game.PauseBind 06000CB7), which speeds the game up instead while the unit still acts.
            state["turnCanEnd"] = turn?.CanEndTurn();
            state["turnCanEndAndNoActing"] = turn?.CanEndTurnAndNoActing();
            state["waitingForUi"] = controller != null && (bool)controller.WaitingForUI;
            state["pairedSequence"] = combat.PairedActivationSequence;
            state["pairedSplit"] = combat.PairedActivationSplit;
            state["pairedFinalized"] = combat.PairedActivationFinalized;
            state["lifetimeRetirements"] = combat.PairedLifetimeRetirementCount;
            state["lifetimeRetirementsDeferred"] = combat.PairedLifetimeRetirementDeferredCount;
            state["lastLifetimeRetirement"] = combat.LastPairedLifetimeRetirement;
            state["exitAiLeaseArmed"] = relationship.NativeTurnBasedExitAiLeaseReassertionArmedCount;
            state["exitAiLeaseAttempts"] = relationship.NativeTurnBasedExitAiLeaseReassertionAttemptCount;
            state["exitAiLeaseMutations"] = relationship.NativeTurnBasedExitAiLeaseReassertionMutationCount;
            return state;
        }

        // ---- CM04-turn-end -----------------------------------------------------------------------------

        private void TickChunk6aTurnEndApproach(TurnController turn)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            var command = lastNativeAbilityShell;
            if (chunk6aTurnEndEvidence == null)
            {
                chunk6aTurnEndCommand = command;
                chunk6aTurnEndTurn = turn;
                chunk6aTurnEndRound = controller.RoundNumber;
                chunk6aTurnEndLedgerBefore = Chunk6aLedgerCounters();
                chunk6aTurnEndGenerationBefore = relationship.MountedPairGeneration;
                chunk6aTurnEndDispatchesBefore = nativeControls.DispatchAcceptedCount;
                chunk6aTurnEndRejectionsBefore = nativeControls.DispatchRejectedCount;
                chunk6aTurnEndShellsBefore = nativeControls.NativeRelationshipShellCount;
                chunk6aTurnEndTraceStart = allocationTrace.EventCount;
                chunk6aTurnEndEvidence = new JObject
                {
                    ["contract"] = "native-end-turn-during-exact-turn-based-mount-approach",
                    ["boundary"] = "turn-end",
                    ["start"] = chunk6aApproachStart,
                    ["before"] = CaptureChunk6aTurnBoundaryState("after-click", command),
                    ["samples"] = chunk6aTurnEndSamples,
                    ["endInputCount"] = 0
                };
                observations["chunk6aTurnEndApproach"] = chunk6aTurnEndEvidence;
                if (command == null || turn == null || turn.Unit != rider)
                {
                    FailCurrent("CM04-turn-end", "The turn-based Mount click left no exact pending shell on the rider's own turn.");
                    BeginCleanup();
                    return;
                }
            }
            if (chunk6aTurnEndSamples.Count < Chunk6aTurnBoundarySampleLimit) chunk6aTurnEndSamples.Add(CaptureChunk6aTurnBoundaryState("tick", command));
            if (!chunk6aTurnEndIssued)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished ||
                    (command as UnitUseAbility)?.ExecutionProcess != null || relationship.State != RelationshipState.Unmounted)
                {
                    FailCurrent("CM04-turn-end", "The exact Mount left pending approach before the native End Turn input.");
                    BeginCleanup();
                    return;
                }
                if (!ReferenceEquals(turn, chunk6aTurnEndTurn) || turn.Unit != rider)
                {
                    FailCurrent("CM04-turn-end", "The rider's native turn changed before the End Turn input could be issued.");
                    BeginCleanup();
                    return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aApproachStart["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("turn-end-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command))
                {
                    FailCurrent("CM04-turn-end", "The End Turn boundary missed the exact non-adjacent pending Mount approach.");
                    BeginCleanup();
                    return;
                }
                // The native End Turn input while the rider still acts is the UI's: InGameInputLayerView.OnTurnBasedEndTurn
                // (06005D30) and the in-game menu button (m_CanEndTurn 06003E87) admit it through TurnController.CanEndTurn
                // and deliver TurnController.ForceToEnd(true) (06000C47), which forfeits the turn (native Standard/Move/Swift
                // debt) and interrupts the running commands. The Space key (Game.PauseBind) requires an empty command queue
                // and only speeds the game up during the approach (preview.206 stage 8). The readiness facts are recorded on
                // every refused frame so the bounded wait names itself.
                if (controller.WaitingForUI || GetPendingNextUnit(controller) != null || !turn.CanEndTurn())
                {
                    chunk6aTurnEndEvidence["endInputWait"] = new JObject
                    {
                        ["frame"] = Time.frameCount, ["waitingForUi"] = (bool)controller.WaitingForUI,
                        ["pendingNextUnit"] = GetPendingNextUnit(controller) != null, ["canEnd"] = turn.CanEndTurn(),
                        ["canEndAndNoActing"] = turn.CanEndTurnAndNoActing()
                    };
                    return;
                }
                if (!EnsureChunk6aRiderSelection("CM04-turn-end")) return;
                chunk6aTurnEndEvidence["trigger"] = CaptureChunk6aInvalidationTrigger(command, slot, geometry, moved);
                chunk6aTurnEndEvidence["beforeEndInput"] = CaptureChunk6aTurnBoundaryState("before-end-input", command);
                chunk6aTurnEndIssued = true;
                chunk6aTurnEndEvidence["endInputCount"] = 1;
                turn.ForceToEnd(true);
                chunk6aTurnEndEvidence["endInput"] = new JObject
                {
                    ["method"] = "TurnBased.Controllers.TurnController.ForceToEnd", ["token"] = "06000C47", ["argument"] = true,
                    ["uiCallers"] = new JArray("06005D30", "06003E87"), ["admission"] = "TurnBased.Controllers.TurnController.CanEndTurn", ["admissionToken"] = "06000C4A",
                    ["moduleMvid"] = typeof(Game).Assembly.ManifestModule.ModuleVersionId.ToString(), ["count"] = 1,
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks
                };
                chunk6aTurnEndEvidence["afterEndInput"] = CaptureChunk6aTurnBoundaryState("after-end-input", command);
                ResetLeafClock();
                return;
            }
            // The rider's own turn must have ended natively and the shell must have reached its terminal.
            var currentTurn = controller.CurrentTurn;
            var turnEnded = currentTurn == null || !ReferenceEquals(currentTurn, chunk6aTurnEndTurn);
            if (!command.IsFinished || !turnEnded || !Chunk6aIdle) { chunk6aTurnEndSettled = 0; return; }
            if (chunk6aTurnEndEvidence["terminal"] == null)
            {
                chunk6aTurnEndEvidence["terminal"] = CaptureChunk6aTurnBoundaryState("terminal", command);
                chunk6aTurnEndEvidence["terminalCommand"] = CaptureOrdinaryCommand(command);
            }
            if (++chunk6aTurnEndSettled < Chunk6aTurnBoundarySettleFrames) return;
            var acted = command.IsActed;
            var proof = chunk6aCommandWindow.Capture();
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            var after = CaptureChunk6aTurnBoundaryState("after", command);
            var events = allocationTrace.EventsSince(chunk6aTurnEndTraceStart);
            var pairEvents = events.OfType<JObject>().Where(e =>
                (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId).ToArray();
            Func<JObject, string, bool> boundaryStarts = (e, prefix) => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal);
            var ledgerDelta = new JObject();
            var ledgerNow = Chunk6aLedgerCounters();
            foreach (var item in chunk6aTurnEndLedgerBefore.Properties())
                ledgerDelta[item.Name] = (long)ledgerNow[item.Name] - (long)item.Value;
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && rider.Commands.Empty && horse.Commands.Empty &&
                nativeControls.NativeRelationshipShellCount == chunk6aTurnEndShellsBefore + 1 &&
                relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null &&
                combat.PairedActivationIdentity == null && combat.PairedPartnerContext == null;
            chunk6aTurnEndEvidence["commandWindow"] = proof;
            chunk6aTurnEndEvidence["after"] = after;
            chunk6aTurnEndEvidence["allocationEvents"] = events;
            chunk6aTurnEndEvidence["allocationTraceComplete"] = allocationTrace.Complete;
            chunk6aTurnEndEvidence["observerHooks"] = allocationTrace.ObserverHooks;
            chunk6aTurnEndEvidence["interrupts"] = new JArray(events.OfType<JObject>()
                .Where(e => (string)e["boundary"] == "command-interrupt-before").Select(e => e.DeepClone()));
            chunk6aTurnEndEvidence["pairCostCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "cost-") || boundaryStarts(e, "actor-cost-"));
            chunk6aTurnEndEvidence["pairPrepareCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "prepare-"));
            chunk6aTurnEndEvidence["ledgerDelta"] = ledgerDelta;
            chunk6aTurnEndEvidence["generationDelta"] = relationship.MountedPairGeneration - chunk6aTurnEndGenerationBefore;
            chunk6aTurnEndEvidence["dispatchAcceptedDelta"] = nativeControls.DispatchAcceptedCount - chunk6aTurnEndDispatchesBefore;
            chunk6aTurnEndEvidence["dispatchRejectedDelta"] = nativeControls.DispatchRejectedCount - chunk6aTurnEndRejectionsBefore;
            chunk6aTurnEndEvidence["relationshipShellsDelta"] = nativeControls.NativeRelationshipShellCount - chunk6aTurnEndShellsBefore;
            chunk6aTurnEndEvidence["roundDelta"] = controller.RoundNumber - chunk6aTurnEndRound;
            chunk6aTurnEndEvidence["noResidue"] = noResidue;
            chunk6aTurnEndEvidence["outcome"] = relationship.State == RelationshipState.Mounted ? "delivered" :
                acted ? "acted-not-mounted" : "unacted-" + command.Result;
            chunk6aTurnEndEvidence["settledFrames"] = chunk6aTurnEndSettled;
            chunk6aTurnEndEvidence["feedback"] = playerAction.LastFeedback;
            chunk6aTurnEndEvidence["lastShellRefusal"] = nativeControls.LastRelationshipShellRefusal;
            var complete = chunk6aTurnEndEvidence["trigger"] != null && chunk6aTurnEndEvidence["terminal"] != null &&
                chunk6aTurnEndIssued && command.IsFinished && turnEnded && allocationTrace.Complete;
            AddRow("CM04-turn-end", complete,
                complete
                    ? "One native End Turn input was issued during the exact measured turn-based Mount approach and the pending sequence settled on the engine's own turn change; the recorded outcome, residue and resource facts are the external validator's to judge."
                    : "The turn-end sequence did not record every observation.",
                chunk6aTurnEndEvidence);
            chunk6aStage = 99;
            BeginCleanup();
        }

        // ---- CM04-mode-exit ----------------------------------------------------------------------------

        private void BeginChunk6aModeExit(JObject mountProof)
        {
            chunk6aModeExitLedgerBefore = Chunk6aLedgerCounters();
            chunk6aModeExitTraceStart = allocationTrace.EventCount;
            chunk6aModeExitEvidence = new JObject
            {
                ["contract"] = "native-turn-based-mode-exit-while-mounted-on-adopted-turn",
                ["boundary"] = "mode-exit",
                ["mountWindow"] = mountProof["window"]?.DeepClone(),
                ["before"] = CaptureChunk6aTurnBoundaryState("before-exit", null),
                ["samples"] = chunk6aModeExitSamples,
                ["exitInputCount"] = 0
            };
            observations["chunk6aModeExit"] = chunk6aModeExitEvidence;
            if (relationship.State != RelationshipState.Mounted || !CombatController.IsInTurnBasedCombat() || combat.PairedActivationIdentity == null)
            {
                FailCurrent("CM04-mode-exit", "The mode exit requires the mounted pair on its adopted turn-based activation.");
                BeginCleanup();
                return;
            }
            // The exact native settings callback and EventBus path (NativeModeTransitionProbe), as the 6B lifecycle
            // native-mode-change case; the probe restores the raw cache and dispatches the original value afterwards.
            chunk6aModeExitProbe = new NativeModeTransitionProbe(false);
            chunk6aModeExitEvidence["exitInputCount"] = 1;
            chunk6aModeExitProbe.DispatchTemporaryValue();
            chunk6aModeExitEvidence["exitInput"] = new JObject
            {
                ["method"] = "SettingsEntityBase.OnInvokeUpdateCallback", ["token"] = "06003359",
                ["cacheToken"] = "04002275", ["temporaryValue"] = false,
                ["settingAfter"] = chunk6aModeExitProbe.CurrentValue, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks
            };
            chunk6aModeExitEvidence["afterExitInput"] = CaptureChunk6aTurnBoundaryState("after-exit-input", null);
            chunk6aStage = 57;
            ResetLeafClock();
        }

        private void TickChunk6aModeExit()
        {
            if (chunk6aModeExitSamples.Count < Chunk6aTurnBoundarySampleLimit) chunk6aModeExitSamples.Add(CaptureChunk6aTurnBoundaryState("tick", null));
            if (!chunk6aModeExitRestored)
            {
                // The native controller leaves turn-based combat on its own schedule after the callback.
                if (CombatController.IsInTurnBasedCombat() || !Chunk6aIdle) { chunk6aModeExitSettled = 0; return; }
                if (chunk6aModeExitEvidence["terminal"] == null)
                    chunk6aModeExitEvidence["terminal"] = CaptureChunk6aTurnBoundaryState("terminal", null);
                if (++chunk6aModeExitSettled < Chunk6aTurnBoundarySettleFrames) return;
                var after = CaptureChunk6aTurnBoundaryState("after", null);
                var events = allocationTrace.EventsSince(chunk6aModeExitTraceStart);
                var pairEvents = events.OfType<JObject>().Where(e =>
                    (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId).ToArray();
                Func<JObject, string, bool> boundaryStarts = (e, prefix) => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal);
                var ledgerDelta = new JObject();
                var ledgerNow = Chunk6aLedgerCounters();
                foreach (var item in chunk6aModeExitLedgerBefore.Properties())
                    ledgerDelta[item.Name] = (long)ledgerNow[item.Name] - (long)item.Value;
                chunk6aModeExitEvidence["after"] = after;
                chunk6aModeExitEvidence["allocationEvents"] = events;
                chunk6aModeExitEvidence["allocationTraceComplete"] = allocationTrace.Complete;
                chunk6aModeExitEvidence["observerHooks"] = allocationTrace.ObserverHooks;
                chunk6aModeExitEvidence["pairCostCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "cost-") || boundaryStarts(e, "actor-cost-"));
                chunk6aModeExitEvidence["pairPrepareCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "prepare-"));
                chunk6aModeExitEvidence["ledgerDelta"] = ledgerDelta;
                chunk6aModeExitEvidence["noResidue"] = relationship.State == RelationshipState.Mounted && relationship.Rider == rider &&
                    relationship.Mount == horse && rider.Commands.Empty && horse.Commands.Empty && !playerAction.HasVoluntaryTransitionInFlight &&
                    combat.PairedActivationIdentity == null && combat.PairedPartnerContext == null;
                chunk6aModeExitEvidence["settledFrames"] = chunk6aModeExitSettled;
                chunk6aModeExitEvidence["feedback"] = combat.LastFeedback;
                // Restore the declared turn-based configuration through the same exact native path before the
                // shared cleanup, so the encounter and the paired fixture settle under their original mode.
                chunk6aModeExitProbe.DispatchRestoreAndRestoreRawCache();
                chunk6aModeExitEvidence["restoreInput"] = new JObject
                {
                    ["frame"] = Time.frameCount, ["settingAfter"] = chunk6aModeExitProbe.CurrentValue,
                    ["persistedValueUnchanged"] = chunk6aModeExitProbe.PersistedValueUnchanged,
                    ["restoreDeliveryCompleted"] = chunk6aModeExitProbe.RestoreDeliveryCompleted
                };
                chunk6aModeExitRestored = true;
                chunk6aModeExitSettled = 0;
                ResetLeafClock();
                return;
            }
            if (!CombatController.IsInTurnBasedCombat() || !Chunk6aIdle) { chunk6aModeExitSettled = 0; return; }
            if (++chunk6aModeExitSettled < Chunk6aTurnBoundarySettleFrames) return;
            chunk6aModeExitEvidence["afterRestore"] = CaptureChunk6aTurnBoundaryState("after-restore", null);
            chunk6aModeExitProbe.Dispose(); chunk6aModeExitProbe = null;
            var complete = chunk6aModeExitEvidence["terminal"] != null && chunk6aModeExitEvidence["after"] != null &&
                chunk6aModeExitRestored && allocationTrace.Complete;
            AddRow("CM04-mode-exit", complete,
                complete
                    ? "The native turn-based mode exit was delivered through the exact settings callback while the pair was mounted on its adopted activation, the sequence settled in real time and the declared mode was restored through the same path; the recorded forfeit, residue and resource facts are the external validator's to judge."
                    : "The mode-exit sequence did not record every observation.",
                chunk6aModeExitEvidence);
            chunk6aStage = 99;
            BeginCleanup();
        }

        private bool CaptureChunk6aTurnBoundaryDeadline()
        {
            if (Chunk6aTurnEndOnly && chunk6aStage == 2 && chunk6aTurnEndEvidence != null)
            {
                observations["chunk6aTurnEndDeadline"] = new JObject
                {
                    ["window"] = chunk6aCommandWindow?.Capture(),
                    ["state"] = CaptureChunk6aTurnBoundaryState("deadline", chunk6aTurnEndCommand),
                    ["endInputIssued"] = chunk6aTurnEndIssued
                };
                FailCurrent("CM04-turn-end", "The exact approach, native End Turn input or pending-shell terminal did not settle at the unchanged 30-second deadline.");
                return true;
            }
            if (Chunk6aModeExitOnly && chunk6aStage == 57)
            {
                observations["chunk6aModeExitDeadline"] = new JObject
                {
                    ["state"] = CaptureChunk6aTurnBoundaryState("deadline", null), ["restored"] = chunk6aModeExitRestored
                };
                FailCurrent("CM04-mode-exit", "The native mode exit, its settlement or its restoration did not settle at the unchanged 30-second deadline.");
                return true;
            }
            return false;
        }

        // The declared turn-based configuration must never outlive its own row: the exit probe restores on
        // every abort path, before the shared paired-mode probe restores the user's original value.
        private void CleanupChunk6aTurnBoundary()
        {
            if (chunk6aModeExitProbe == null) return;
            try
            {
                if (!chunk6aModeExitRestored && chunk6aModeExitProbe.TemporaryDeliveryAttempted)
                    chunk6aModeExitProbe.DispatchRestoreAndRestoreRawCache();
                chunk6aModeExitProbe.Dispose();
            }
            finally
            {
                if (chunk6aModeExitEvidence != null) chunk6aModeExitEvidence["cleanupRestore"] = chunk6aModeExitProbe.CurrentValue;
                chunk6aModeExitProbe = null;
            }
        }
    }
}
