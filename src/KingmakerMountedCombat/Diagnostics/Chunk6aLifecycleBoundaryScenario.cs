using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Two lifecycle boundaries during the exact combat Mount approach: the encounter ends
    // (its one enemy leaves and is removed natively, the party then leaves combat on
    // Kingmaker's own clock) or the registered UMM toggle disables the feature and later
    // re-enables it. The scenario performs the boundary, records what the engine and the
    // product did at every step and whether the sequence settled; which outcome shapes are
    // lawful is the external validator's decision. Nothing here writes a cooldown, refunds a
    // cost, forces a turn or interrupts the native command itself.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aCombatEndApproachScenario = "chunk6a-combat-end-approach";
        internal const string Chunk6aDisableApproachScenario = "chunk6a-disable-approach";
        private const int Chunk6aLifecycleSettleFrames = 10;
        private const int Chunk6aLifecycleSampleLimit = 600;
        private bool Chunk6aCombatEndOnly => request.Scenario == Chunk6aCombatEndApproachScenario;
        private bool Chunk6aDisableOnly => request.Scenario == Chunk6aDisableApproachScenario;
        private bool Chunk6aLifecycleBoundaryOnly => Chunk6aCombatEndOnly || Chunk6aDisableOnly;
        private string Chunk6aLifecycleRow => Chunk6aCombatEndOnly ? "CM04-combat-end" : "CM04-disable-unload";
        private JObject chunk6aLifecycleEvidence;
        private UnitCommand chunk6aLifecycleCommand;
        private JObject chunk6aLifecycleLedgerBefore;
        private long chunk6aLifecycleGenerationBefore;
        private long chunk6aLifecycleDispatchesBefore;
        private long chunk6aLifecycleRejectionsBefore;
        private long chunk6aLifecycleCastsBefore;
        private long chunk6aLifecycleShellsBefore;
        private int chunk6aLifecycleTraceStart;
        private bool chunk6aLifecycleTriggered;
        private bool chunk6aLifecycleReEnabled;
        private int chunk6aLifecycleSettled;
        private int chunk6aLifecycleTerminalFrame = -1;
        private readonly JArray chunk6aLifecycleSamples = new JArray();

        private JObject CaptureChunk6aLifecycleState(string kind)
        {
            var command = chunk6aLifecycleCommand;
            JObject controlState = null;
            try { controlState = JObject.FromObject(nativeControls.CaptureSnapshot(), JsonSerializer.Create(JsonSettings)); }
            catch (Exception exception) { controlState = new JObject { ["error"] = exception.GetType().Name + ": " + exception.Message }; }
            return new JObject
            {
                ["kind"] = kind,
                ["frame"] = Time.frameCount,
                ["seconds"] = clock.Elapsed.TotalSeconds,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["partyInCombat"] = Game.Instance.Player.IsInCombat,
                ["riderInCombat"] = rider.IsInCombat,
                ["horseInCombat"] = horse.IsInCombat,
                ["targetPresent"] = target != null && target.IsInState,
                ["targetInCombat"] = target != null && target.IsInState && target.IsInCombat,
                ["relationshipState"] = relationship.State.ToString(),
                ["generation"] = relationship.MountedPairGeneration,
                ["transitionInFlight"] = playerAction.HasVoluntaryTransitionInFlight,
                ["transitionLedger"] = playerAction.TransitionLedger.Describe(),
                ["ledger"] = Chunk6aLedgerCounters(),
                ["relationshipShells"] = nativeControls.NativeRelationshipShellCount,
                ["shellState"] = nativeControls.DescribeRelationshipShellState(rider),
                ["dispatchAccepted"] = nativeControls.DispatchAcceptedCount,
                ["dispatchRejected"] = nativeControls.DispatchRejectedCount,
                ["castRequests"] = nativeControls.NativeCastRequestCount,
                ["pairedIdentity"] = combat.PairedActivationIdentity,
                ["partnerContextActor"] = combat.PairedPartnerContext?.Unit?.UniqueId,
                ["adoptionCount"] = combat.MidEncounterAdoptionCount,
                ["adoptionRollbacks"] = combat.AdoptionRollbackCount,
                ["compensatedMounts"] = relationship.AdoptionCompensatedMountCount,
                ["riderCommandsEmpty"] = rider.Commands.Empty,
                ["horseCommandsEmpty"] = horse.Commands.Empty,
                ["riderReallyMoving"] = rider.View?.AgentASP?.IsReallyMoving,
                ["controls"] = controlState,
                ["rider"] = Chunk6aCooldowns(rider),
                ["mount"] = Chunk6aCooldowns(horse),
                ["command"] = command == null ? null : CaptureOrdinaryCommand(command),
                ["allocationSequence"] = allocationTrace.EventCount,
                ["combatFeedback"] = combat.LastFeedback,
                ["playerActionFeedback"] = playerAction.LastFeedback
            };
        }

        private void TickChunk6aLifecycleBoundary()
        {
            if (!Chunk6aLifecycleBoundaryOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("A lifecycle boundary case requires its isolated real-time transaction.");
            // Both boundary scenarios enter here at the click stage; preview.152 dispatched the disable
            // scenario to stage 72 and its first trigger-wait tick failed without a click (CM04-disable-unload).
            if (chunk6aStage != 70 && chunk6aLifecycleEvidence == null)
            {
                FailCurrent(Chunk6aLifecycleRow, "The lifecycle boundary tick was entered at stage " + chunk6aStage + " before its click stage 70.");
                BeginCleanup();
                return;
            }
            if (chunk6aStage == 70)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aLifecycleRow)) return;
                var start = CaptureChunk6aGeometry("lifecycle-pre-click");
                if ((bool)start["isAdjacent"]) throw new InvalidOperationException("The lifecycle boundary Mount must start outside transition reach.");
                chunk6aLifecycleLedgerBefore = Chunk6aLedgerCounters();
                chunk6aLifecycleGenerationBefore = relationship.MountedPairGeneration;
                chunk6aLifecycleDispatchesBefore = nativeControls.DispatchAcceptedCount;
                chunk6aLifecycleRejectionsBefore = nativeControls.DispatchRejectedCount;
                chunk6aLifecycleCastsBefore = nativeControls.NativeCastRequestCount;
                chunk6aLifecycleShellsBefore = nativeControls.NativeRelationshipShellCount;
                chunk6aLifecycleTraceStart = allocationTrace.EventCount;
                chunk6aLifecycleEvidence = new JObject
                {
                    ["contract"] = Chunk6aCombatEndOnly
                        ? "native-combat-end-during-exact-mount-approach"
                        : "registered-disable-during-exact-mount-approach",
                    ["boundary"] = Chunk6aCombatEndOnly ? "combat-end" : "mod-disable",
                    ["start"] = start,
                    ["before"] = CaptureChunk6aLifecycleState("before-click"),
                    ["samples"] = chunk6aLifecycleSamples
                };
                observations["chunk6aLifecycleBoundary"] = chunk6aLifecycleEvidence;
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-lifecycle-boundary-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                chunk6aLifecycleCommand = chunk6aCommandWindow.Command;
                chunk6aLifecycleEvidence["click"] = observations["chunk6a-lifecycle-boundary-click"]?.DeepClone();
                chunk6aLifecycleEvidence["afterClick"] = CaptureChunk6aLifecycleState("after-click");
                if (!clicked || chunk6aLifecycleCommand == null)
                {
                    FailCurrent(Chunk6aLifecycleRow, "Exact rider native Mount click was refused: " + playerAction.LastFeedback);
                    BeginCleanup();
                    return;
                }
                chunk6aStage = 71;
                ResetLeafClock();
                return;
            }
            var command = chunk6aLifecycleCommand;
            if (chunk6aLifecycleSamples.Count < Chunk6aLifecycleSampleLimit) chunk6aLifecycleSamples.Add(CaptureChunk6aLifecycleState("tick"));
            if (!chunk6aLifecycleTriggered)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished ||
                    (command as UnitUseAbility)?.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aLifecycleRow, "The exact Mount left pending approach before the lifecycle boundary.");
                    BeginCleanup();
                    return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aLifecycleEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("lifecycle-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command))
                {
                    FailCurrent(Chunk6aLifecycleRow, "The lifecycle boundary missed the exact non-adjacent pending Mount approach.");
                    BeginCleanup();
                    return;
                }
                chunk6aLifecycleEvidence["trigger"] = new JObject
                {
                    ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["approachObserved"] = true,
                    ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving,
                    ["riderDisplacement"] = moved,
                    ["commandObject"] = RuntimeHelpers.GetHashCode(command),
                    ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slot),
                    ["started"] = command.IsStarted,
                    ["acted"] = command.IsActed,
                    ["finished"] = command.IsFinished,
                    ["geometry"] = geometry,
                    ["state"] = CaptureChunk6aLifecycleState("trigger")
                };
                chunk6aLifecycleTriggered = true;
                if (Chunk6aCombatEndOnly) BeginChunk6aCombatEndBoundary(); else BeginChunk6aDisableBoundary();
                ResetLeafClock();
                return;
            }
            if (Chunk6aCombatEndOnly && !TickChunk6aCombatEndBoundary()) return;
            if (!command.IsFinished || !Chunk6aIdle) { chunk6aLifecycleSettled = 0; return; }
            if (chunk6aLifecycleTerminalFrame < 0)
            {
                chunk6aLifecycleTerminalFrame = Time.frameCount;
                chunk6aLifecycleEvidence["terminal"] = CaptureChunk6aLifecycleState("terminal");
            }
            if (++chunk6aLifecycleSettled < Chunk6aLifecycleSettleFrames) return;
            if (Chunk6aDisableOnly && !chunk6aLifecycleReEnabled) { ReEnableChunk6aServices(); return; }
            CompleteChunk6aLifecycleBoundary();
        }

        // The one enemy leaves and is removed natively; the party then leaves combat on
        // Kingmaker's own clock. The diagnostic combat-memory lease ends with the target.
        private void BeginChunk6aCombatEndBoundary()
        {
            var detail = new JObject
            {
                ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["targetId"] = target?.UniqueId,
                ["targetInCombatBefore"] = target != null && target.IsInState && target.IsInCombat
            };
            TryLeaveCombat(target);
            detail["destroyRequested"] = targetService != null && targetService.DestroyAndVerify();
            detail["requested"] = CaptureChunk6aLifecycleState("combat-end-requested");
            chunk6aLifecycleEvidence["combatEnd"] = detail;
        }

        private bool TickChunk6aCombatEndBoundary()
        {
            var detail = (JObject)chunk6aLifecycleEvidence["combatEnd"];
            if (detail["destroyVerified"] == null)
            {
                if (targetService != null && !targetService.DestroyAndVerify()) return false;
                detail["destroyVerified"] = true;
                detail["destroyVerifiedFrame"] = Time.frameCount;
                targetService?.Dispose(); targetService = null; target = null;
            }
            if (detail["partyLeftCombatFrame"] == null)
            {
                if (Game.Instance.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat) return false;
                detail["partyLeftCombatFrame"] = Time.frameCount;
                detail["partyLeftCombatGameTicks"] = Game.Instance.TimeController.GameTime.Ticks;
                detail["partyLeftCombat"] = CaptureChunk6aLifecycleState("party-left-combat");
                ResetLeafClock();
            }
            return true;
        }

        // The exact registered UMM toggle, not an internal shortcut. The unload delegate is not
        // invoked here: outside a live owned write it disposes the composition root, and its
        // refusal during such a write is the Chunk 5 P07-disable-reenable observation that the
        // CM08-disable-removal-readiness composite binds.
        private void BeginChunk6aDisableBoundary()
        {
            var detail = new JObject { ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["before"] = CaptureChunk6aLifecycleState("disable-before") };
            bool result; string error = null;
            try { result = Main.InvokeRegisteredToggleForAutomation(false); }
            catch (Exception exception) { result = false; error = exception.GetType().Name + ": " + exception.Message; }
            detail["disableResult"] = result;
            detail["disableError"] = error;
            detail["after"] = CaptureChunk6aLifecycleState("disable-after");
            detail["unloadProbe"] = "not-performed: the registered unload disposes the composition root when no owned save is in flight; " +
                "its refusal during a live owned write is the Chunk 5 P07-disable-reenable observation bound through CM08-disable-removal-readiness";
            chunk6aLifecycleEvidence["disable"] = detail;
        }

        private void ReEnableChunk6aServices()
        {
            var detail = new JObject { ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["before"] = CaptureChunk6aLifecycleState("re-enable-before") };
            bool result; string error = null;
            try { result = Main.InvokeRegisteredToggleForAutomation(true); }
            catch (Exception exception) { result = false; error = exception.GetType().Name + ": " + exception.Message; }
            chunk6aLifecycleReEnabled = true;
            nativeControls.Update();
            detail["reEnableResult"] = result;
            detail["reEnableError"] = error;
            detail["after"] = CaptureChunk6aLifecycleState("re-enable-after");
            var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
            detail["mountAvailability"] = new JObject
            {
                ["visible"] = availability.IsVisible,
                ["enabled"] = availability.IsEnabled,
                ["transitionReady"] = availability.IsTransitionReady,
                ["reason"] = availability.Reason
            };
            chunk6aLifecycleEvidence["reEnable"] = detail;
            chunk6aLifecycleSettled = 0;
            ResetLeafClock();
        }

        private void CompleteChunk6aLifecycleBoundary()
        {
            var command = chunk6aLifecycleCommand;
            var acted = command.IsActed;
            JObject proof; string windowKind;
            if (acted && chunk6aCommandWindow.Terminal)
            {
                proof = chunk6aCommandWindow.Finish(rider.IsInCombat, false, 0, true);
                windowKind = "positive";
            }
            else
            {
                proof = chunk6aCommandWindow.Capture();
                windowKind = acted ? "acted-without-process-terminal" : "unacted";
            }
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            var after = CaptureChunk6aLifecycleState("after");
            var events = allocationTrace.EventsSince(chunk6aLifecycleTraceStart);
            var pairEvents = events.OfType<JObject>().Where(e =>
                (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId).ToArray();
            Func<JObject, string, bool> boundaryStarts = (e, prefix) => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal);
            var ledgerDelta = new JObject();
            var ledgerNow = Chunk6aLedgerCounters();
            foreach (var item in chunk6aLifecycleLedgerBefore.Properties())
                ledgerDelta[item.Name] = (long)ledgerNow[item.Name] - (long)item.Value;
            // NativeRelationshipShellCount is a monotonic registration counter (one increment per admitted
            // relationship shell, never decremented): the one click of the sequence registers exactly one.
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && rider.Commands.Empty && horse.Commands.Empty &&
                nativeControls.NativeRelationshipShellCount == chunk6aLifecycleShellsBefore + 1 &&
                (relationship.State == RelationshipState.Mounted ||
                    combat.PairedActivationIdentity == null && combat.PairedPartnerContext == null);
            chunk6aLifecycleEvidence["commandWindowKind"] = windowKind;
            chunk6aLifecycleEvidence["commandWindow"] = proof;
            chunk6aLifecycleEvidence["terminalCommand"] = CaptureOrdinaryCommand(command);
            chunk6aLifecycleEvidence["after"] = after;
            chunk6aLifecycleEvidence["allocationEvents"] = events;
            chunk6aLifecycleEvidence["allocationTraceComplete"] = allocationTrace.Complete;
            chunk6aLifecycleEvidence["observerHooks"] = allocationTrace.ObserverHooks;
            chunk6aLifecycleEvidence["interrupts"] = new JArray(events.OfType<JObject>()
                .Where(e => (string)e["boundary"] == "command-interrupt-before").Select(e => e.DeepClone()));
            chunk6aLifecycleEvidence["pairCostCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "cost-") || boundaryStarts(e, "actor-cost-"));
            chunk6aLifecycleEvidence["pairPrepareCallbacks"] = pairEvents.Count(e => boundaryStarts(e, "prepare-"));
            chunk6aLifecycleEvidence["ledgerDelta"] = ledgerDelta;
            chunk6aLifecycleEvidence["generationDelta"] = relationship.MountedPairGeneration - chunk6aLifecycleGenerationBefore;
            chunk6aLifecycleEvidence["dispatchAcceptedDelta"] = nativeControls.DispatchAcceptedCount - chunk6aLifecycleDispatchesBefore;
            chunk6aLifecycleEvidence["dispatchRejectedDelta"] = nativeControls.DispatchRejectedCount - chunk6aLifecycleRejectionsBefore;
            chunk6aLifecycleEvidence["castRequestDelta"] = nativeControls.NativeCastRequestCount - chunk6aLifecycleCastsBefore;
            chunk6aLifecycleEvidence["relationshipShellsDelta"] = nativeControls.NativeRelationshipShellCount - chunk6aLifecycleShellsBefore;
            chunk6aLifecycleEvidence["noResidue"] = noResidue;
            chunk6aLifecycleEvidence["outcome"] = relationship.State == RelationshipState.Mounted ? "delivered" :
                acted ? "acted-not-mounted" : "unacted-" + command.Result;
            chunk6aLifecycleEvidence["settledFrames"] = chunk6aLifecycleSettled;
            // The row is complete when every boundary of the sequence was observed and settled;
            // whether the observed outcome is lawful is the external validator's decision.
            var boundaryObserved = Chunk6aCombatEndOnly
                ? ((JObject)chunk6aLifecycleEvidence["combatEnd"])["partyLeftCombatFrame"] != null
                : chunk6aLifecycleEvidence["reEnable"] != null;
            var complete = chunk6aLifecycleEvidence["trigger"] != null && chunk6aLifecycleEvidence["terminal"] != null &&
                command.IsFinished && allocationTrace.Complete && boundaryObserved;
            AddRow(Chunk6aLifecycleRow, complete,
                complete
                    ? "The lifecycle boundary was delivered during the exact measured Mount approach and the sequence settled; the recorded outcome, residue and resource facts are the external validator's to accept."
                    : "The lifecycle boundary sequence did not record every observation.",
                chunk6aLifecycleEvidence);
            chunk6aStage = 99;
            BeginCleanup();
        }

        private bool CaptureChunk6aLifecycleBoundaryDeadline()
        {
            if (!Chunk6aLifecycleBoundaryOnly || chunk6aStage < 70 || chunk6aStage > 74) return false;
            observations["chunk6aLifecycleBoundaryDeadline"] = new JObject
            {
                ["window"] = chunk6aCommandWindow?.Capture(),
                ["state"] = chunk6aLifecycleEvidence == null ? null : CaptureChunk6aLifecycleState("deadline")
            };
            FailCurrent(Chunk6aLifecycleRow, "The exact approach, lifecycle boundary or terminal did not settle at the unchanged 30-second deadline.");
            return true;
        }

        // A disabled service must never outlive its own row: the registered toggle is restored
        // on every abort path so the shared cleanup and restoration run against live services.
        private void CleanupChunk6aLifecycleBoundary()
        {
            if (!Chunk6aDisableOnly || !chunk6aLifecycleTriggered || chunk6aLifecycleReEnabled) return;
            chunk6aLifecycleReEnabled = true;
            var restored = Main.InvokeRegisteredToggleForAutomation(true);
            if (chunk6aLifecycleEvidence != null) chunk6aLifecycleEvidence["cleanupReEnable"] = restored;
            if (!restored) throw new InvalidOperationException("Registered services could not be re-enabled during cleanup.");
        }
    }
}
