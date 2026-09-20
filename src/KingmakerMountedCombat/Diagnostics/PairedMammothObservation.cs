using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeCombatScenarioEngine
    {
        private NativeActorAllocationTrace pairedMammothTrace;
        private readonly HashSet<TurnController> pairedMammothVisits = new HashSet<TurnController>();
        private readonly HashSet<TurnController> pairedMammothEndedTurns = new HashSet<TurnController>();
        private readonly JObject pairedMammothEvidence = new JObject();
        private TurnController pairedMammothFirstTurn;
        private bool pairedMammothAwaitingRenewal;
        private bool UsesPairedMammothActivation => IsTurnBasedRow && IsMammothPrimaryRow && settings.EnablePairedActivation;
        private bool UsesLegacyMammothScheduler => UsesDistinctSharedTurnPrincipal && !UsesPairedMammothActivation;

        // Call after exact actor resolution and before pre-combat Mount.
        private void BeginPairedMammothObservation()
        {
            if (!UsesPairedMammothActivation) return;
            if (rider.IsInCombat || mount.IsInCombat || Game.Instance.Player.IsInCombat ||
                Game.Instance.TurnBasedCombatController.Initialized || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Mammoth paired fixture requires its exact idle pair and sole authority.");
            pairedMammothTrace = new NativeActorAllocationTrace(rider, mount, combat);
            pairedMammothTrace.BeginEncounter(request.RunId + ":" + currentRow);
            pairedMammothEvidence["level"] = "NATIVE INTEGRATION";
            pairedMammothEvidence["outsideCombat"] = true;
            pairedMammothEvidence["enablePairedActivation"] = settings.EnablePairedActivation;
            pairedMammothEvidence["enableUnifiedMountedTurn"] = settings.EnableUnifiedMountedTurn;
            pairedMammothEvidence["enablePairedCommandScheduler"] = settings.EnablePairedCommandScheduler;
            pairedMammothEvidence["enableDiagnosticOverlay"] = settings.EnableDiagnosticOverlay;
            pairedMammothEvidence["rider"] = rider.UniqueId;
            pairedMammothEvidence["mount"] = mount.UniqueId;
            pairedMammothEvidence["visits"] = new JArray();
            pairedMammothEvidence["endInputs"] = new JArray();
            pairedMammothEvidence["beforeEncounter"] = CapturePairedMammothSample("mammoth-before-encounter");
        }

        private JObject CapturePairedMammothSample(string kind)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            pairedMammothTrace.Record(kind, rider);
            var trace = pairedMammothTrace.Capture();
            var events = (JArray)trace["events"];
            var last = events.Last;
            return new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                ["traceSequence"] = last["sequence"], ["gameTicks"] = last["gameTicks"],
                ["identity"] = combat.PairedActivationIdentity, ["sequence"] = combat.PairedActivationSequence,
                ["currentActor"] = controller.CurrentTurn?.Unit.UniqueId, ["round"] = controller.RoundNumber,
                ["rider"] = pairedMammothTrace.Snapshot(rider), ["mount"] = pairedMammothTrace.Snapshot(mount),
                ["riderPreparations"] = pairedMammothTrace.GrantCount(rider), ["mountPreparations"] = pairedMammothTrace.GrantCount(mount) };
        }

        // Call before general dispatch readiness returns while waiting for the
        // principal. Intervening fixture friends need real native End input.
        private bool ObservePairedMammothPrincipal()
        {
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            if (turn == null) return false;
            if (pairedMammothVisits.Add(turn))
            {
                pairedMammothTrace.Record("mammoth-turn-observed", turn.Unit);
                ((JArray)pairedMammothEvidence["visits"]).Add(new JObject {
                    ["actor"] = turn.Unit.UniqueId, ["round"] = controller.RoundNumber,
                    ["frame"] = Time.frameCount, ["friendly"] = turn.Unit.Group == rider.Group });
            }
            if (turn.Unit == mount) throw new InvalidOperationException("Mammoth received a duplicate independent native turn.");
            if (turn.Unit != rider) { EndPairedMammothObservedTurn(turn); return false; }
            if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing ||
                combat.PairedPartnerContext?.Unit != mount || !rider.CombatState.Prepared || !mount.CombatState.Prepared)
                return false;
            if (pairedMammothFirstTurn == null)
            {
                if (combat.PairedActivationSequence != 1 || pairedMammothTrace.GrantCount(rider) != 1 || pairedMammothTrace.GrantCount(mount) != 1)
                    throw new InvalidOperationException("Mammoth did not receive exactly one native paired preparation.");
                pairedMammothFirstTurn = turn;
                pairedMammothEvidence["firstGrant"] = CapturePairedMammothSample("mammoth-first-native-grant");
                nativeActionActorTurnStarted = true; // Observed; never StartTurn().
            }
            return true;
        }

        private void EndPairedMammothObservedTurn(TurnController turn)
        {
            var actor = turn.Unit;
            var controller = Game.Instance.TurnBasedCombatController;
            if (pairedMammothEndedTurns.Contains(turn) || !actor.IsDirectlyControllable || !actor.Commands.Empty || actor.AreHandsBusyWithAnimation ||
                !turn.CanEndTurnAndNoActing() || controller.WaitingForUI || combat.HasActiveCommand || combat.HasActiveGroundMovement) return;
            if (actor != rider && !(targetService.NonPairPartyAiLease.OwnsExactMember(actor) && targetService.NonPairPartyAiLease.ValidateActive()))
                throw new InvalidOperationException("Mammoth fixture refused End Turn on an unowned actor.");
            ((JArray)pairedMammothEvidence["endInputs"]).Add(new JObject { ["actor"] = actor.UniqueId,
                ["frame"] = Time.frameCount, ["kind"] = "Game.PauseBind" });
            pairedMammothTrace.Record("mammoth-native-end-input", actor);
            Game.Instance.PauseBind();
            pairedMammothEndedTurns.Add(turn);
        }

        // Invoke once after existing native attack/cost assertions, retaining
        // the existing AwaitOutcome step. Its next update calls the renewal leg.
        private void BeginPairedMammothRenewal()
        {
            pairedMammothEvidence["afterAttack"] = CapturePairedMammothSample("mammoth-native-primary-complete");
            pairedMammothAwaitingRenewal = true;
        }

        private void TickPairedMammothRenewal()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (ReferenceEquals(turn, pairedMammothFirstTurn)) { EndPairedMammothObservedTurn(turn); return; }
            if (!ObservePairedMammothPrincipal()) return;
            var next = CapturePairedMammothSample("mammoth-next-native-paired-grant");
            if (combat.PairedActivationSequence != 2 || pairedMammothTrace.GrantCount(rider) != 2 || pairedMammothTrace.GrantCount(mount) != 2 ||
                rider.CombatState.Cooldown.StandardAction != 0f || rider.CombatState.Cooldown.MoveAction != 0f ||
                mount.CombatState.Cooldown.StandardAction != 0f || mount.CombatState.Cooldown.MoveAction != 0f ||
                !((JArray)pairedMammothEvidence["visits"]).OfType<JObject>().Any(item => (string)item["actor"] != rider.UniqueId))
                throw new InvalidOperationException("Mammoth paired renewal lost preparation, debt or unrelated participation.");
            pairedMammothEvidence["nextGrant"] = next;
            pairedMammothEvidence["trace"] = pairedMammothTrace.Capture();
            pairedMammothEvidence["passed"] = true;
            pairedMammothAwaitingRenewal = false;
            BeginCleanup();
        }

        private void CleanupPairedMammothObservation()
        {
            if (pairedMammothTrace == null) return;
            // Keep the measured endpoint separate from native cleanup callbacks.
            if (pairedMammothEvidence["trace"] == null) pairedMammothEvidence["trace"] = pairedMammothTrace.Capture();
            pairedMammothTrace.Dispose(); pairedMammothTrace = null;
        }
    }
}
