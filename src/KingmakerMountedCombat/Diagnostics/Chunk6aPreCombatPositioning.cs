using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private UnitMoveTo chunk6aPositioningCommand;
        private UnitEntityData chunk6aPositioningMover;
        private Vector3 chunk6aPositioningDestination;
        private JObject chunk6aPositioningEvidence, chunk6aJointPositioning;
        private NativeOutsideCombatGroundProbe chunk6aPositioningResources;
        private NativeCommandPathProbe chunk6aPositioningPath;
        private bool chunk6aPositioningComplete, chunk6aJointGroundComplete;
        private bool chunk6aPositioningObserving, chunk6aPositioningPriorObservation;

        private JObject CaptureChunk6aOriginNavigation(UnitEntityData actor = null)
        {
            actor = actor ?? rider;
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = actor.Position; var nearest = AstarPath.active.GetNearest(origin);
            return new JObject { ["position"] = CapturePosition(origin), ["nearest"] = CapturePosition(nearest.clampedPosition),
                ["projectionResidual"] = HorizontalDistance(origin, nearest.clampedPosition),
                ["walkable"] = nearest.node != null && nearest.node.Walkable,
                ["footprint"] = NativeGroundMovementObservation.CaptureFootprint(actor, origin),
                ["actingClearance"] = NativeGroundMovementObservation.CaptureFootprint(actor, origin,
                    Math.Max(0.5f, actor.View.Corpulence) + 0.75f) };
        }
        private JObject FindChunk6aPreCombatPosition()
        {
            var plan = NativePreCombatGroundPlan.Search(rider, horse, horse.Position,
                Math.Max(0.5f, rider.View.Corpulence) + 0.75f, false);
            NativePreCombatGroundEvidence.AssertSearch(plan); return plan;
        }
        private JObject Chunk6aPositioningSelection(UnitEntityData actor)
        {
            var manager = SelectionManager.Instance;
            if (manager != null && actor?.View != null) manager.SelectUnit(actor.View, true, true, false);
            var selected = manager?.SelectedUnits;
            var evidence = new JObject { ["actorId"] = actor?.UniqueId,
                ["selectedIds"] = selected == null ? null : new JArray(selected.Select(u => u?.UniqueId)),
                ["exact"] = selected != null && selected.Count == 1 && selected[0] == actor,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
            observations["chunk6aGroundSelection"] = evidence;
            if (!(bool)evidence["exact"]) throw new InvalidOperationException("Exact ground mover selection failed before native input: " + evidence);
            return evidence;
        }
        private void BeginChunk6aGroundPositioning(UnitEntityData mover, JObject plan, string contract)
        {
            var selected = NativePreCombatGroundEvidence.AssertSearch(plan);
            if (selected < 0) throw new InvalidOperationException("No eligible bounded native ground plan.");
            var selection = Chunk6aPositioningSelection(mover);
            chunk6aPositioningMover = mover;
            chunk6aPositioningDestination = NativePreCombatGroundPlan.Position(plan["candidates"][selected]["point"]);
            chunk6aPositioningEvidence = new JObject { ["contract"] = contract,
                ["moverId"] = mover.UniqueId, ["selection"] = selection, ["plan"] = plan.DeepClone(),
                ["frameBefore"] = Time.frameCount, ["gameTicksBefore"] = Game.Instance.TimeController.GameTime.Ticks,
                ["beforeInCombat"] = rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat,
                ["before"] = CaptureChunk6aCausalState(), ["originNavigation"] = CaptureChunk6aOriginNavigation(mover),
                ["minimumTravel"] = 0.25f, ["maximumTravel"] = 4f,
                ["clearanceRadius"] = plan["clearanceRadius"].DeepClone(), ["candidates"] = plan["candidates"].DeepClone(),
                ["destination"] = CapturePosition(chunk6aPositioningDestination) };
            if (mover == rider) observations["chunk6aPreCombatPositioning"] = chunk6aPositioningEvidence;
            else chunk6aJointPositioning["ground"] = chunk6aPositioningEvidence;
            if (!chunk6aPositioningObserving) { chunk6aPositioningPriorObservation = allocationTrace.ObserveReactionResources; allocationTrace.ObserveReactionResources = true; chunk6aPositioningObserving = true; }
            chunk6aPositioningResources = new NativeOutsideCombatGroundProbe(allocationTrace, rider, horse, mover);
            chunk6aPositioningPath = new NativeCommandPathProbe(mover);
            ClickGroundHandler.MoveSelectedUnitsToPoint(chunk6aPositioningDestination, false);
            chunk6aPositioningCommand = mover.Commands.Move as UnitMoveTo;
            chunk6aPositioningEvidence["admittedCommand"] = CaptureOrdinaryCommand(chunk6aPositioningCommand);
            chunk6aPositioningResources.Bind(chunk6aPositioningCommand);
            chunk6aPositioningPath.Bind(chunk6aPositioningCommand);
        }
        private bool FinishChunk6aGroundPositioning()
        {
            chunk6aPositioningEvidence["path"] = chunk6aPositioningPath.Capture();
            chunk6aPositioningEvidence["resourceWindow"] = chunk6aPositioningResources.Capture();
            if (!chunk6aPositioningCommand.IsFinished || !Chunk6aIdle || chunk6aPositioningMover.View.AgentASP.IsReallyMoving) return false;
            var path = chunk6aPositioningPath.Capture(); chunk6aPositioningPath.Dispose(); chunk6aPositioningPath = null;
            var resources = chunk6aPositioningResources.Finish(); chunk6aPositioningResources.Dispose(); chunk6aPositioningResources = null;
            var after = CaptureChunk6aCausalState();
            chunk6aPositioningEvidence["after"] = after;
            chunk6aPositioningEvidence["resourceWindow"] = resources;
            chunk6aPositioningEvidence["frameAfter"] = Time.frameCount;
            chunk6aPositioningEvidence["gameTicksAfter"] = Game.Instance.TimeController.GameTime.Ticks;
            chunk6aPositioningEvidence["afterInCombat"] = rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat;
            chunk6aPositioningEvidence["createdByPlayer"] = chunk6aPositioningCommand.CreatedByPlayer;
            chunk6aPositioningEvidence["terminalCommand"] = CaptureOrdinaryCommand(chunk6aPositioningCommand);
            chunk6aPositioningEvidence["terminalNavigation"] = CaptureChunk6aOriginNavigation(chunk6aPositioningMover);
            chunk6aPositioningEvidence["events"] = resources["events"].DeepClone();
            chunk6aPositioningEvidence["traceComplete"] = allocationTrace.Complete;
            chunk6aPositioningEvidence["residual"] = HorizontalDistance(chunk6aPositioningMover.Position, chunk6aPositioningDestination);
            var before = chunk6aPositioningEvidence["before"];
            var events = ((JArray)path["events"]).OfType<JObject>().ToArray();
            var pass = chunk6aPositioningCommand.Result == UnitCommand.ResultType.Success && (bool)path["complete"] &&
                (bool)resources["pass"] && events.Any(e => (string)e["boundary"] == "path-request") &&
                events.Any(e => (string)e["boundary"] == "path-complete-after") && events.Any(e => (string)e["boundary"] == "command-ended") &&
                (float)chunk6aPositioningEvidence["residual"] <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance &&
                JToken.DeepEquals(before["ledger"], after["ledger"]) && JToken.DeepEquals(before["generation"], after["generation"]) &&
                (string)before["relationshipState"] == "Unmounted" && (string)after["relationshipState"] == "Unmounted" &&
                JToken.DeepEquals(before["selectedIds"], after["selectedIds"]);
            chunk6aPositioningEvidence["pass"] = pass;
            if (!pass) throw new InvalidOperationException("Pre-combat positioning did not prove exact native arrival, action/reaction accounting, terminal state and unchanged relationship; complete evidence retained.");
            try { NativePreCombatGroundEvidence.AssertTransaction(chunk6aPositioningEvidence, rider.UniqueId, horse.UniqueId); }
            catch (Exception exception) { chunk6aPositioningEvidence["pass"] = false; chunk6aPositioningEvidence["failure"] = exception.Message; throw; }
            chunk6aPositioningCommand = null; return true;
        }
        // A failed original 72-proposal search is retained. The fallback performs
        // one independently planned mount ground transaction, then recomputes the
        // original rider search from actual geometry. Every move retains its own
        // unchanged 30-second leaf deadline and 0.06m native arrival tolerance.
        private bool TickChunk6aPreCombatPositioning()
        {
            if (chunk6aPositioningComplete) return true;
            if (rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
                throw new InvalidOperationException("Pre-combat positioning entered an encounter.");
            if (chunk6aPositioningCommand != null) {
                if (!FinishChunk6aGroundPositioning()) return false;
                if (chunk6aPositioningMover == rider) {
                    if (chunk6aJointPositioning != null) NativePreCombatGroundEvidence.AssertJoint(chunk6aJointPositioning, chunk6aPositioningEvidence, rider.UniqueId, horse.UniqueId);
                    RestoreChunk6aGroundObservation(); chunk6aPositioningComplete = true; ResetLeafClock(); return true;
                }
                chunk6aJointGroundComplete = true; ResetLeafClock();
            }
            if (!Chunk6aIdle) return false;
            if (!EnsureChunk6aRiderSelection("CM01-combat-mount-setup")) return false;
            var plan = FindChunk6aPreCombatPosition();
            if ((int)plan["selectedIndex"] >= 0) {
                BeginChunk6aGroundPositioning(rider, plan, "native-ground-positioning-before-fresh-encounter");
                return false;
            }
            if (chunk6aJointGroundComplete) {
                chunk6aJointPositioning["actualRiderSearchFailure"] = plan;
                throw new InvalidOperationException("Joint mount ground arrival did not yield an eligible actual rider staging position; new failed search retained.");
            }
            chunk6aJointPositioning = new JObject { ["contract"] = "bounded-joint-plan-before-two-separate-native-ground-inputs",
                ["initialRiderFailure"] = plan, ["initialState"] = CaptureChunk6aCausalState(),
                ["maximumMountCandidates"] = 72, ["maximumRiderCandidatesPerMount"] = 72,
                ["maximumJointCandidates"] = 5256 };
            observations["chunk6aPreCombatJointPositioning"] = chunk6aJointPositioning;
            var joint = NativePreCombatGroundPlan.Search(horse, rider, rider.Position, Math.Max(0.5f, horse.View.Corpulence), true);
            chunk6aJointPositioning["jointPlan"] = joint;
            var choice = NativePreCombatGroundEvidence.AssertSearch(joint);
            if (choice < 0) throw new InvalidOperationException("No bounded joint mount/rider ground plan has clear native routes and unchanged staging bounds; every proposal retained.");
            BeginChunk6aGroundPositioning(horse, joint, "native-mount-ground-positioning-before-rider-staging");
            return false;
        }
        private void RestoreChunk6aGroundObservation() { if (!chunk6aPositioningObserving) return; allocationTrace.ObserveReactionResources = chunk6aPositioningPriorObservation; chunk6aPositioningObserving = false; }
        private void CleanupChunk6aPreCombatPositioning()
        {
            try {
                if (chunk6aPositioningPath != null) chunk6aPositioningEvidence["abortedPath"] = chunk6aPositioningPath.Capture();
                if (chunk6aPositioningResources != null) chunk6aPositioningEvidence["abortedResources"] = chunk6aPositioningResources.Capture();
            } finally {
                try { chunk6aPositioningPath?.Dispose(); } finally {
                    chunk6aPositioningPath = null;
                    try { chunk6aPositioningResources?.Dispose(); } finally { chunk6aPositioningResources = null; try { chunk6aPositioningCommand?.Interrupt(); } finally { RestoreChunk6aGroundObservation(); } }
                }
            }
        }
    }
}
