using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6A action-economy rows: nine isolated fresh turn-based transactions on the
    // allocation-order fixture. Every expenditure is an ordinary native actor command on the
    // actor's own native turn; every cost is Kingmaker's own and is only observed here.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6aActionEconomyScenario(string scenario) => NativeActionEconomyEvidence.VariantOf(scenario) != null;
        private NativeActionEconomyEvidence.Variant Chunk6aActionEconomyVariant => NativeActionEconomyEvidence.VariantOf(request.Scenario);
        private bool Chunk6aActionEconomyOnly => Chunk6aActionEconomyVariant != null;
        private bool Chunk6aAdjacentMount => Chunk6aActionEconomyVariant?.AdjacentMount == true;
        private bool Chunk6aRiderExhaustOnly => Chunk6aActionEconomyVariant?.RiderExhaust == true;
        // The pre-encounter initiative fixture is shared by the two allocation-order
        // scenarios and every action-economy variant.
        private bool Chunk6aOrderFixtureOnly => Chunk6aMountOrderOnly || Chunk6aActionEconomyOnly;
        private bool Chunk6aOrderFixtureRiderFirst => Chunk6aMountOrderOnly ? Chunk6aOrderRiderFirst : Chunk6aActionEconomyVariant.RiderFirst;
        private string Chunk6aEconomyTargetPlacement => Chunk6aActionEconomyVariant?.TargetPlacement ?? "default";
        private bool Chunk6aLaterTurnDismount => request.Scenario == "chunk6a-combat-mount-tb" ||
            Chunk6aActionEconomyVariant?.Dismount == "later-after-rider" || Chunk6aActionEconomyVariant?.Dismount == "later-after-mount";
        private bool Chunk6aLaterTurnRiderAttack => Chunk6aActionEconomyVariant?.Dismount == "later-after-rider";

        private JObject chunk6aEconomy;
        private NativeAutomaticEndProbe chunk6aEconomyAutoEnd;
        private UnitEntityData chunk6aEconomyUnrelated;
        private int chunk6aEconomyUnrelatedBase;
        private bool chunk6aEconomyUnrelatedOwned;
        private int chunk6aEconomyMountSlotStage, chunk6aEconomyEntryStage, chunk6aEconomyExhaustStage;
        private TurnController chunk6aEconomyMountSlotTurn, chunk6aEconomyEntryTurn, chunk6aEconomyExhaustTurn, chunk6aEconomyDismountTurn;
        private UnitCommand chunk6aEconomyMountSlotCommand, chunk6aEconomyEntryCommand, chunk6aEconomyExhaustCommand, chunk6aEconomyLaterAttack;
        private JObject chunk6aEconomyMountSlot, chunk6aEconomyEntry, chunk6aEconomyExhaustion, chunk6aEconomyRefusal, chunk6aEconomyDismount, chunk6aEconomyRelease;
        private readonly HashSet<TurnController> chunk6aEconomyReleaseSeen = new HashSet<TurnController>();
        private bool chunk6aEconomyEndClicked;
        private int chunk6aEconomyDismountRound;

        private void BeginChunk6aActionEconomy()
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null) return;
            chunk6aEconomy = new JObject
            {
                ["contract"] = NativeActionEconomyEvidence.Contract, ["variant"] = variant.Scenario, ["scenario"] = request.Scenario,
                ["row"] = variant.Row, ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                ["riderFirst"] = variant.RiderFirst, ["mountSlotKind"] = variant.MountSlot, ["riderEntryKind"] = variant.RiderEntry,
                ["dismountKind"] = variant.Dismount, ["targetPlacement"] = variant.TargetPlacement
            };
            observations["chunk6aActionEconomy"] = chunk6aEconomy;
            // The native automatic-End input preference decides whether an exhausted actor's
            // turn ends by itself. It is leased false for the whole transaction, exactly as the
            // Chunk 4 turn-based scenarios do, so every End below is one observed native input.
            chunk6aEconomyAutoEnd = new NativeAutomaticEndProbe(false);
            chunk6aEconomy["automaticEnd"] = new JObject { ["leased"] = true, ["temporaryValue"] = false, ["frame"] = Time.frameCount };
        }

        private JObject CaptureChunk6aEconomyBoundary()
        {
            var boundary = CaptureChunk6aDismountBoundary();
            boundary["pairIdle"] = Chunk6aIdle;
            return boundary;
        }

        private static JObject CaptureChunk6aSpent(UnitEntityData actor) => new JObject
        {
            ["actor"] = actor.UniqueId,
            ["usedOneMoveAction"] = actor.UsedOneMoveAction(), ["usedTwoMoveAction"] = actor.UsedTwoMoveAction(),
            ["usedStandardAction"] = actor.UsedStandardAction(), ["hasMoveAction"] = actor.HasMoveAction(),
            ["hasStandardAction"] = actor.HasStandardAction(), ["moveRestricted"] = actor.IsMoveActionRestricted(),
            ["standard"] = actor.CombatState.Cooldown.StandardAction, ["move"] = actor.CombatState.Cooldown.MoveAction,
            ["swift"] = actor.CombatState.Cooldown.SwiftAction
        };

        private JObject CaptureChunk6aWeapon(UnitEntityData actor)
        {
            var weapon = actor.GetFirstWeapon()?.Blueprint;
            return new JObject { ["ranged"] = weapon?.IsRanged == true, ["category"] = weapon?.Category.ToString(), ["guid"] = weapon?.AssetGuid,
                ["leaseReady"] = rangedWeaponLease != null && rangedWeaponLease.IsReady };
        }

        private bool EnsureChunk6aActorSelection(UnitEntityData actor, string row)
        {
            var manager = SelectionManager.Instance;
            if (manager != null && actor?.View != null) manager.SelectUnit(actor.View, true, true, false);
            var selected = manager?.SelectedUnits;
            var exact = selected != null && selected.Count == 1 && selected[0] == actor;
            observations["chunk6aSelection-" + row + "-" + (actor == rider ? "rider" : actor == horse ? "mount" : actor?.UniqueId)] = new JObject
            {
                ["actorId"] = actor?.UniqueId, ["selectedCount"] = selected?.Count ?? -1,
                ["selectedIds"] = selected == null ? null : new JArray(selected.Select(u => u?.UniqueId)), ["exact"] = exact, ["frame"] = Time.frameCount
            };
            if (exact) return true;
            FailCurrent(row, "Exact native actor selection failed before native input: " + actor?.UniqueId);
            BeginCleanup();
            return false;
        }

        // The ranged lease gives the Ranger rider one stock longbow in an empty hand set,
        // so its Standard action is a single ranged attack without an approach. Restored by
        // the shared cleanup after leaving combat.
        private bool PrepareChunk6aActionEconomyEquipment()
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null || !variant.Longbow) return true;
            if (rangedWeaponLease == null)
            {
                rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                rangedWeaponLease.Acquire(WeaponCategory.Longbow);
                chunk6aEconomy["weaponLease"] = new JObject { ["category"] = WeaponCategory.Longbow.ToString(), ["frame"] = Time.frameCount };
                return false;
            }
            if (!rangedWeaponLease.IsReady || !Chunk6aIdle) return false;
            chunk6aEconomy["weapon"] = CaptureChunk6aWeapon(rider);
            return true;
        }

        private void PrepareChunk6aEconomyUnrelatedFixture()
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null || !variant.Unrelated || chunk6aEconomyUnrelatedOwned) return;
            var unrelated = allocationFixtureParty.OrderBy(member => member.UniqueId, StringComparer.Ordinal)
                .FirstOrDefault(member => member != rider && member != horse && member.IsInState && member.IsDirectlyControllable &&
                    member.Descriptor.State.IsConscious && member.Stats.HitPoints.ModifiedValue - member.Damage > 6);
            if (unrelated == null) throw new InvalidOperationException("No idle disposable party member is available as the unrelated native candidate.");
            if (unrelated.IsInCombat || rider.IsInCombat) throw new InvalidOperationException("Unrelated candidate input requires a pre-encounter allocation.");
            chunk6aEconomyUnrelated = unrelated;
            chunk6aEconomyUnrelatedBase = unrelated.Stats.Initiative.BaseValue;
            var before = allocationTrace.Snapshot(unrelated);
            chunk6aEconomyUnrelatedOwned = true;
            unrelated.Stats.Initiative.BaseValue = NativeActionEconomyEvidence.UnrelatedInitiativeInput;
            var after = allocationTrace.Snapshot(unrelated);
            chunk6aEconomy["unrelated"] = new JObject
            {
                ["actorId"] = unrelated.UniqueId, ["actorObject"] = RuntimeHelpers.GetHashCode(unrelated),
                ["originalBase"] = chunk6aEconomyUnrelatedBase, ["inputBase"] = unrelated.Stats.Initiative.BaseValue,
                ["modifiedInput"] = unrelated.Stats.Initiative.ModifiedValue, ["outsideCombat"] = !unrelated.IsInCombat,
                ["beforeResources"] = before, ["afterResources"] = after, ["frame"] = Time.frameCount
            };
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Unrelated initiative input changed an allocation resource.");
        }

        // Runs after the allocation cleanup has returned the disposable party to its idle state.
        // Every lease is always released exactly; an anomaly is recorded and reported only after
        // the restorations so that no residue survives it.
        private void RestoreChunk6aActionEconomyFixture()
        {
            if (chunk6aEconomy == null) return;
            string anomaly = null;
            if (chunk6aEconomyUnrelatedOwned)
            {
                var unrelated = chunk6aEconomyUnrelated;
                var inCombatAtRestore = unrelated.IsInCombat;
                var leaseInputIntact = unrelated.Stats.Initiative.BaseValue == NativeActionEconomyEvidence.UnrelatedInitiativeInput;
                unrelated.Stats.Initiative.BaseValue = chunk6aEconomyUnrelatedBase;
                chunk6aEconomyUnrelatedOwned = false;
                var record = (JObject)chunk6aEconomy["unrelated"];
                record["restoration"] = new JObject { ["outsideCombat"] = !unrelated.IsInCombat, ["inCombatAtRestore"] = inCombatAtRestore,
                    ["leaseInputIntact"] = leaseInputIntact, ["base"] = unrelated.Stats.Initiative.BaseValue,
                    ["exact"] = unrelated.Stats.Initiative.BaseValue == chunk6aEconomyUnrelatedBase, ["frame"] = Time.frameCount };
                if (inCombatAtRestore || !leaseInputIntact)
                    anomaly = "Unrelated initiative restoration found live combat or a changed lease input; the original base was restored: inCombat=" + inCombatAtRestore + ", leaseInputIntact=" + leaseInputIntact + ".";
            }
            if (chunk6aEconomyAutoEnd != null)
            {
                var probe = chunk6aEconomyAutoEnd; chunk6aEconomyAutoEnd = null;
                try { probe.Dispose(); }
                finally { ((JObject)chunk6aEconomy["automaticEnd"])["restored"] = probe.Restored; }
            }
            if (anomaly != null) throw new InvalidOperationException(anomaly);
        }

        private Vector3 FindChunk6aEconomyStepDestination(UnitEntityData mover, UnitEntityData anchor, string name)
        {
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = mover.Position; var anchorPosition = anchor.Position;
            var separation = HorizontalDistance(origin, anchorPosition);
            var candidates = new JArray();
            observations[name] = new JObject
            {
                ["contract"] = "bounded-pair-relative-native-ground-search", ["moverId"] = mover.UniqueId, ["anchorId"] = anchor.UniqueId,
                ["origin"] = CapturePosition(origin), ["anchor"] = CapturePosition(anchorPosition), ["initialSeparation"] = separation,
                ["requestedTravel"] = 0.6f, ["travelTolerance"] = 0.15f, ["maximumSeparationIncrease"] = 0.15f, ["candidates"] = candidates
            };
            foreach (var proposal in NativeGroundFixturePolicy.ActingProposals(
                new PoseVector3(origin.x, origin.y, origin.z), new PoseVector3(anchorPosition.x, anchorPosition.y, anchorPosition.z)))
            {
                var requested = new Vector3(proposal.X, proposal.Y, proposal.Z);
                var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                var candidate = new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point),
                    ["walkable"] = nearest.node != null && nearest.node.Walkable, ["eligible"] = false };
                candidates.Add(candidate);
                if (nearest.node == null || !nearest.node.Walkable) continue;
                var routeEnd = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                var routeResidual = HorizontalDistance(routeEnd, point);
                var footprint = NativeGroundMovementObservation.CaptureFootprint(mover, point);
                var residuals = ((JArray)footprint["probes"]).Select(probe => (float)probe["residual"]).ToArray();
                var footprintResidual = residuals.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ? float.NaN : residuals.Max();
                var blockers = Game.Instance.State.Units.Where(unit => unit != mover && unit.IsInState && unit.View != null &&
                    HorizontalDistance(point, unit.Position) < mover.View.Corpulence + unit.View.Corpulence + 0.05f).Select(unit => unit.UniqueId).ToArray();
                var travel = HorizontalDistance(origin, point); var proposedSeparation = HorizontalDistance(point, anchorPosition);
                var eligible = NativeGroundFixturePolicy.IsActingStep(separation, proposedSeparation, travel, routeResidual, footprintResidual, blockers.Length != 0);
                candidate["travel"] = travel; candidate["separation"] = proposedSeparation; candidate["routeEnd"] = CapturePosition(routeEnd);
                candidate["routeResidual"] = routeResidual; candidate["footprint"] = footprint; candidate["blockers"] = new JArray(blockers); candidate["eligible"] = eligible;
                if (eligible) return point;
            }
            throw new InvalidOperationException("No bounded pair-relative native ground step satisfied unchanged separation and clear footprint constraints for " + mover.UniqueId + ".");
        }

        // One native five-foot step that ends inside the Mount envelope: a tangent step when the
        // pair is already adjacent, otherwise a short step toward the mount.
        private Vector3 FindChunk6aFiveFootStepDestination()
        {
            var geometry = CaptureChunk6aGeometry("five-foot-step-search");
            if ((bool)geometry["isAdjacent"]) return FindChunk6aEconomyStepDestination(rider, horse, "chunk6aFiveFootStepSearch");
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = rider.Position; var direction = horse.Position - origin; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Five-foot step direction is degenerate.");
            direction.Normalize();
            var envelope = (float)geometry["legalAdjacencyEnvelope"]; var corpulence = rider.View.Corpulence + horse.View.Corpulence;
            var candidates = new JArray();
            observations["chunk6aFiveFootStepSearch"] = new JObject { ["contract"] = "bounded-native-five-foot-step-toward-mount", ["origin"] = CapturePosition(origin),
                ["envelope"] = envelope, ["stepLimit"] = TurnController.MetersOfFiveFootStep, ["candidates"] = candidates };
            foreach (var step in new[] { 1.2f, 1.0f, 0.8f, 1.4f })
            {
                var requested = origin + direction * step;
                var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                var travel = HorizontalDistance(origin, point); var separation = HorizontalDistance(point, horse.Position);
                var footprint = NativeGroundMovementObservation.CaptureFootprint(rider, point);
                var residuals = ((JArray)footprint["probes"]).Select(probe => (float)probe["residual"]).ToArray();
                var footprintResidual = residuals.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ? float.NaN : residuals.Max();
                var routeResidual = HorizontalDistance(ObstacleAnalyzer.TraceAlongNavmesh(origin, point), point);
                var eligible = nearest.node != null && nearest.node.Walkable && travel >= 0.25f && travel <= TurnController.MetersOfFiveFootStep - 0.05f &&
                    separation <= envelope - 0.1f && separation >= corpulence + 0.1f && routeResidual <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance && footprintResidual < 0.001f;
                candidates.Add(new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point), ["travel"] = travel, ["separation"] = separation,
                    ["routeResidual"] = routeResidual, ["footprint"] = footprint, ["eligible"] = eligible });
                if (eligible) return point;
            }
            throw new InvalidOperationException("No bounded native five-foot step reaches the Mount envelope from the staged rider position.");
        }

        private void VerifyChunk6aMountReachTargetPlacement()
        {
            var placement = (JObject)observations["chunk6aDismountTargetPlacement"];
            placement["actualTargetId"] = target.UniqueId;
            placement["actualTargetPosition"] = CapturePosition(target.Position);
            placement["actualTargetCorpulence"] = target.View.Corpulence;
            var separation = HorizontalDistance(horse.Position, target.Position);
            placement["actualHorseSeparation"] = separation;
            var native = new UnitAttack(target); native.Init(horse);
            var ranges = native.CreateFullAttack().Select(attack => attack.WeaponRange).ToArray();
            var reach = ranges.Length == 0 ? 0f : horse.View.Corpulence + target.View.Corpulence + ranges.Min();
            placement["mountAttackRanges"] = new JArray(ranges); placement["mountReach"] = reach;
            placement["mountReachContract"] = "mount-attacks-the-stationary-target-from-its-own-slot-without-approach";
            if (ranges.Length == 0 || separation > reach - 0.05f || separation < horse.View.Corpulence + target.View.Corpulence + 0.05f)
                throw new InvalidOperationException("Near-mount target placement is outside the mount's exact native attack reach: " + placement.ToString(Formatting.None));
        }

        // CM03-mount-spent-move: the mount's own slot expenditure is a bounded 0.6 m ground step
        // around the mount, so the stationary target is placed beyond every candidate step's
        // mover-plus-target corpulence margin. The margin itself is a recorded fixture fact.
        internal const float MountStepClearSeparation = 3.4f, MountStepTravelBound = 0.75f, MountStepClearMargin = 0.2f;
        private float Chunk6aMountStepClearance(float targetCorpulence) =>
            horse.View.Corpulence + targetCorpulence + 0.05f + MountStepTravelBound + MountStepClearMargin;

        private Vector3 FindChunk6aMountStepClearTargetPosition()
        {
            if (AstarPath.active == null || rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
                throw new InvalidOperationException("Mount-step-clear target placement requires fresh exploration geometry.");
            const float targetCorpulence = 0.7f;
            var clearance = Chunk6aMountStepClearance(targetCorpulence);
            var direction = horse.Position - rider.Position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Mount-step-clear target direction is degenerate.");
            direction.Normalize();
            var candidates = new JArray();
            observations["chunk6aDismountTargetPlacement"] = new JObject {
                ["contract"] = "pre-combat-clear-target-beyond-the-mount-step-margin",
                ["horseOrigin"] = CapturePosition(horse.Position), ["riderOrigin"] = CapturePosition(rider.Position),
                ["horseCorpulence"] = horse.View.Corpulence, ["proposedTargetCorpulence"] = targetCorpulence,
                ["separation"] = MountStepClearSeparation, ["stepTravelBound"] = MountStepTravelBound, ["blockerMargin"] = 0.05f,
                ["margin"] = MountStepClearMargin, ["stepClearance"] = clearance, ["candidates"] = candidates };
            for (var index = 0; index < 24; index++)
            {
                var angle = index == 0 ? 0 : (index % 2 == 0 ? index : -index) * 15;
                var requested = horse.Position + Quaternion.Euler(0, angle, 0) * direction * MountStepClearSeparation;
                var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                var distance = HorizontalDistance(point, horse.Position);
                var blockers = Game.Instance.State.Units.Where(unit => unit.IsInState && unit.View != null &&
                    HorizontalDistance(point, unit.Position) < targetCorpulence + unit.View.Corpulence + 0.05f).Select(unit => unit.UniqueId).ToArray();
                var beyond = distance >= clearance;
                var clear = nearest.node != null && nearest.node.Walkable && blockers.Length == 0 && beyond &&
                    Math.Abs(distance - MountStepClearSeparation) <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance;
                candidates.Add(new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point), ["distance"] = distance,
                    ["riderDistance"] = HorizontalDistance(point, rider.Position), ["blockers"] = new JArray(blockers), ["beyondStepMargin"] = beyond, ["clear"] = clear });
                if (clear) return point;
            }
            throw new InvalidOperationException("No bounded clear native target placement lies beyond the mount's step margin before combat.");
        }

        private void VerifyChunk6aMountStepClearTargetPlacement()
        {
            var placement = (JObject)observations["chunk6aDismountTargetPlacement"];
            placement["actualTargetId"] = target.UniqueId;
            placement["actualTargetPosition"] = CapturePosition(target.Position);
            placement["actualTargetCorpulence"] = target.View.Corpulence;
            var separation = HorizontalDistance(horse.Position, target.Position);
            var clearance = Chunk6aMountStepClearance(target.View.Corpulence);
            placement["actualHorseSeparation"] = separation; placement["actualRiderSeparation"] = HorizontalDistance(rider.Position, target.Position);
            placement["actualStepClearance"] = clearance; placement["actualBeyondStepMargin"] = separation >= clearance;
            if (separation < clearance)
                throw new InvalidOperationException("Mount-step-clear target spawned inside the mount's step margin: " + placement.ToString(Formatting.None));
        }

        private JObject CaptureChunk6aEconomyInput(string kind, bool clicked, int cycles, JObject extra)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var input = new JObject { ["kind"] = kind, ["clicked"] = clicked, ["cursorCycles"] = cycles, ["frame"] = Time.frameCount,
                ["fullEnabled"] = turn?.EnabledFullAttack, ["fiveFootStep"] = turn?.EnabledFiveFootStep, ["singleActionMove"] = turn?.EnabledSingleActionMove,
                ["selectedIds"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(u => u.UniqueId)) };
            if (extra != null) foreach (var property in extra.Properties()) input[property.Name] = property.Value;
            return input;
        }

        // Issue one ordinary native attack by actor on the current native turn: single
        // (Standard only) or full (full-round) through the native right-click attack mode.
        private UnitAttack IssueChunk6aNativeAttack(UnitEntityData actor, bool full, JObject record)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            using (var input = new NativeOrdinaryAttackInput(target))
            {
                var before = CaptureChunk6aSpent(actor);
                input.Predict(); input.Predict();
                if (!JToken.DeepEquals(before, CaptureChunk6aSpent(actor))) throw new InvalidOperationException("Native hover prediction changed the actor's action state.");
                var cycles = 0;
                while (turn.EnabledFullAttack != full && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                if (turn.EnabledFullAttack != full) throw new InvalidOperationException("Native right-click did not choose the " + (full ? "full" : "single") + " attack mode.");
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Exact diagnostic target refused the expected native attack dispatch.");
                var clicked = input.Click();
                var command = actor.Commands.GetCommand(UnitCommand.CommandType.Standard) as UnitAttack;
                record["input"] = CaptureChunk6aEconomyInput(full ? "full-attack" : "single-attack", clicked, cycles, new JObject {
                    ["targetId"] = target.UniqueId, ["nativeEstimate"] = UnitAttack.EstimateFullAttacks(actor) });
                record["admitted"] = CaptureOrdinaryCommand(command);
                if (!clicked || command == null || command.Executor != actor || command.Target != target)
                    throw new InvalidOperationException("Native attack input admitted no exact command for " + actor.UniqueId + ": " + record.ToString(Formatting.None));
                return command;
            }
        }

        private UnitMoveTo IssueChunk6aNativeGroundOrder(UnitEntityData actor, Vector3 point, bool fiveFootStep, JObject record)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            using (var input = new NativeOrdinaryAttackInput(point))
            {
                input.Predict(); var cycles = 0;
                if (fiveFootStep) { while (!turn.EnabledFiveFootStep && cycles++ < 8) { input.Click(button: 1); input.Predict(); } }
                else { while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); } }
                if (turn.EnabledFiveFootStep != fiveFootStep || (!fiveFootStep && turn.EnabledSingleActionMove))
                    throw new InvalidOperationException("Native right-click did not choose the " + (fiveFootStep ? "five-foot step" : "ordinary movement") + " mode.");
                var clicked = input.Click();
                var command = actor.Commands.Move as UnitMoveTo;
                record["input"] = CaptureChunk6aEconomyInput(fiveFootStep ? "five-foot-step" : "ground", clicked, cycles, new JObject { ["point"] = CapturePosition(point) });
                record["admitted"] = CaptureOrdinaryCommand(command);
                if (!clicked || command == null || command.Executor != actor || !command.CreatedByPlayer)
                    throw new InvalidOperationException("Native ground input admitted no exact player command for " + actor.UniqueId + ": " + record.ToString(Formatting.None));
                return command;
            }
        }

        private void FinishChunk6aEconomyCommand(JObject record, UnitEntityData actor, UnitCommand command)
        {
            var before = (JObject)record["before"];
            record["after"] = CaptureChunk6aEconomyBoundary();
            record["terminal"] = CaptureOrdinaryCommand(command);
            record["events"] = new JArray(allocationTrace.EventsSince((int)before["allocationSequence"]).Take((int)record["after"]["allocationSequence"] - (int)before["allocationSequence"]));
            record["traceComplete"] = allocationTrace.Complete;
            record["spent"] = CaptureChunk6aSpent(actor);
            var attack = command as UnitAttack;
            if (attack != null) { record["nativeFull"] = attack.IsFullAttack; record["nativeSingle"] = attack.IsSingleAttack; record["planned"] = attack.AllAttacks.Count; record["completed"] = attack.GetAttackIndex(); record["weapon"] = CaptureChunk6aWeapon(actor); }
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            record["stepMetres"] = turn?.MetersMovedByFiveFootStep; record["stepLimit"] = TurnController.MetersOfFiveFootStep;
            record["timeMoved"] = turn?.TimeMoved;
        }

        private bool Chunk6aEconomyCommandSettled(UnitCommand command, UnitEntityData actor) =>
            command.IsFinished && Chunk6aIdle && actor.Commands.Empty && !actor.View.AgentASP.IsReallyMoving;

        // The mount's own native slot before the rider's: one ordinary command spends the
        // declared debt, then the native End of that slot keeps it. Returns true while the
        // mount's own turn is being handled by this step.
        private bool TickChunk6aMountSlotExpenditure(TurnController turn)
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null || variant.MountSlot == "none" || chunk6aEconomyMountSlotStage >= 3) return false;
            if (chunk6aEconomyMountSlotStage == 2 && turn?.Unit != horse)
            {
                var slotEnd = CaptureChunk6aEconomyBoundary();
                var after = (JObject)chunk6aEconomyMountSlot["after"];
                chunk6aEconomyMountSlot["slotEnd"] = slotEnd;
                chunk6aEconomyMountSlot["endEvents"] = new JArray(allocationTrace.EventsSince((int)after["allocationSequence"]).Take((int)slotEnd["allocationSequence"] - (int)after["allocationSequence"]));
                chunk6aEconomyMountSlotStage = 3;
                return false;
            }
            if (turn?.Unit != horse) return false;
            if (chunk6aEconomyMountSlotStage == 0)
            {
                if (!Chunk6aIdle || turn.Status != TurnController.TurnStatus.Preparing || Game.Instance.TurnBasedCombatController.WaitingForUI) return true;
                if (!EnsureChunk6aActorSelection(horse, variant.Row)) return true;
                chunk6aEconomyMountSlotTurn = turn;
                chunk6aEconomyMountSlot = new JObject { ["kind"] = variant.MountSlot, ["turnObject"] = RuntimeHelpers.GetHashCode(turn),
                    ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["before"] = CaptureChunk6aEconomyBoundary() };
                chunk6aEconomy["mountSlot"] = chunk6aEconomyMountSlot;
                if (variant.MountSlot == "ground")
                    chunk6aEconomyMountSlotCommand = IssueChunk6aNativeGroundOrder(horse, FindChunk6aEconomyStepDestination(horse, rider, "chunk6aMountSlotStepSearch"), false, chunk6aEconomyMountSlot);
                else chunk6aEconomyMountSlotCommand = IssueChunk6aNativeAttack(horse, variant.MountSlot == "full-attack", chunk6aEconomyMountSlot);
                chunk6aEconomyMountSlotStage = 1; ResetLeafClock(); return true;
            }
            if (!ReferenceEquals(turn, chunk6aEconomyMountSlotTurn)) throw new InvalidOperationException("The mount's own native slot changed before its command settled.");
            if (chunk6aEconomyMountSlotStage == 1)
            {
                if (!Chunk6aEconomyCommandSettled(chunk6aEconomyMountSlotCommand, horse)) return true;
                FinishChunk6aEconomyCommand(chunk6aEconomyMountSlot, horse, chunk6aEconomyMountSlotCommand);
                // The native terminal result and the slot status after the command are recorded
                // facts for the external validator; the slot ends through the fixture's native End.
                chunk6aEconomyMountSlotStage = 2; ResetLeafClock();
            }
            // The exact idle slot ends through the fixture's native End (ForceToEnd(false)).
            TryEndPhase3gFixtureTurn(turn);
            return true;
        }

        private bool PrepareChunk6aActingEntry(TurnController turn)
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null || variant.RiderEntry == "ground")
            {
                var ready = PrepareChunk6aNativeActingTurn(turn);
                if (ready && variant != null && chunk6aEconomy["riderEntry"] == null)
                    chunk6aEconomy["riderEntry"] = new JObject { ["kind"] = "ground", ["setupContract"] = chunk6aActingSetup?["contract"]?.DeepClone(),
                        ["turnObject"] = RuntimeHelpers.GetHashCode(turn) };
                return ready;
            }
            if (chunk6aEconomyEntryStage == 2) return turn.IsActing;
            if (chunk6aEconomyEntryStage == 0)
            {
                if (turn.Status != TurnController.TurnStatus.Preparing || !Chunk6aIdle || Game.Instance.TurnBasedCombatController.WaitingForUI) return false;
                if (!EnsureChunk6aRiderSelection(variant.Row)) return false;
                chunk6aEconomyEntryTurn = turn;
                chunk6aEconomyEntry = new JObject { ["kind"] = variant.RiderEntry, ["turnObject"] = RuntimeHelpers.GetHashCode(turn),
                    ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["before"] = CaptureChunk6aEconomyBoundary() };
                chunk6aEconomy["riderEntry"] = chunk6aEconomyEntry;
                chunk6aEconomyEntryCommand = variant.RiderEntry == "attack"
                    ? (UnitCommand)IssueChunk6aNativeAttack(rider, false, chunk6aEconomyEntry)
                    : IssueChunk6aNativeGroundOrder(rider, FindChunk6aFiveFootStepDestination(), true, chunk6aEconomyEntry);
                chunk6aEconomyEntryStage = 1; ResetLeafClock(); return false;
            }
            if (!ReferenceEquals(turn, chunk6aEconomyEntryTurn)) throw new InvalidOperationException("The rider's native turn changed before its Acting entry settled.");
            if (!Chunk6aEconomyCommandSettled(chunk6aEconomyEntryCommand, rider)) return false;
            FinishChunk6aEconomyCommand(chunk6aEconomyEntry, rider, chunk6aEconomyEntryCommand);
            // The native terminal result is a recorded fact for the external validator. Only the
            // allocation state gates the flow: the Mount request needs the rider's Acting turn.
            if (!turn.IsActing)
            {
                AddRow(variant.Row, false, "The rider's Acting entry command settled without the native turn entering Acting, so no Mount request can be issued on this allocation; the recorded entry evidence is retained.", chunk6aEconomy);
                BeginCleanup(); return false;
            }
            chunk6aEconomyEntryStage = 2; ResetLeafClock();
            return true;
        }

        // Captured in the same frame and allocation sequence as the Mount window's own
        // pre-click baseline, so the retention window closes exactly where the proof opens.
        private void CaptureChunk6aEconomyPreClick()
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant == null || variant.MountSlot == "none") return;
            if (chunk6aEconomyMountSlotStage != 3) throw new InvalidOperationException("The mount's own slot did not end before the rider's Mount request.");
            var slotEnd = (JObject)chunk6aEconomyMountSlot["slotEnd"];
            var preClick = CaptureChunk6aEconomyBoundary();
            chunk6aEconomy["retention"] = new JObject { ["contract"] = "mount-debt-retained-from-its-own-slot-end-to-the-rider-mount-request",
                ["before"] = slotEnd.DeepClone(), ["after"] = preClick,
                ["events"] = new JArray(allocationTrace.EventsSince((int)slotEnd["allocationSequence"]).Take((int)preClick["allocationSequence"] - (int)slotEnd["allocationSequence"])),
                ["traceComplete"] = allocationTrace.Complete,
                ["mountTurnsBetween"] = chunk6aOrderTurns.OfType<JObject>().Count(t => (string)t["currentActor"] == horse.UniqueId && (int)t["allocationSequence"] > (int)slotEnd["allocationSequence"]) };
        }

        private JObject Chunk6aEconomyProof(string window) => chunk6aCommandProofs.OfType<JObject>().SingleOrDefault(p => (string)p["window"] == window);

        private void FinishChunk6aActionEconomyMount(JObject mountProof)
        {
            var variant = Chunk6aActionEconomyVariant;
            if (variant.Unrelated)
            {
                var order = Game.Instance.TurnBasedCombatController.SortedUnits.ToList();
                var record = (JObject)chunk6aEconomy["unrelated"];
                record["rosterIndexAtMount"] = order.IndexOf(chunk6aEconomyUnrelated);
                record["riderRosterIndex"] = order.IndexOf(rider); record["mountRosterIndex"] = order.IndexOf(horse);
            }
            if (!variant.Dismounts) { FinishChunk6aMountOrder(mountProof); return; }
            RecordChunk6aMountOrderMounted(mountProof);
            chunk6aEconomyDismount = new JObject { ["kind"] = variant.Dismount, ["mountTurnObject"] = RuntimeHelpers.GetHashCode(chunk6aMountTurn), ["mountRound"] = chunk6aMountRound };
            chunk6aEconomy["dismount"] = chunk6aEconomyDismount;
            allocationTrace.ObserveReactionResources = true;
            chunk6aStage = 60; ResetLeafClock();
        }

        private void CompleteChunk6aActionEconomyOrder()
        {
            var variant = Chunk6aActionEconomyVariant;
            string failure = null;
            try
            {
                NativeMountOrderEvidence.AssertCore(chunk6aOrderEvidence, request.Scenario, variant.RiderFirst);
                NativeActionEconomyEvidence.AssertStructure(chunk6aEconomy);
            }
            catch (Exception exception) { failure = exception.Message; }
            AddRow(variant.Row, failure == null, failure ?? Chunk6aEconomyDetail(variant), chunk6aEconomy);
        }

        private static string Chunk6aEconomyDetail(NativeActionEconomyEvidence.Variant variant)
        {
            switch (variant.Row)
            {
                case "CM03-mount-spent-move": return "The mount spent its own native Move through an ordinary ground order on its own earlier slot; the later rider Mount adopted it without a cooldown clear, preparation replay, allocation refresh or second participation, and the prior Move debt was changed only by native time passage until the next lawful native preparation.";
                case "CM03-mount-spent-standard": return "The mount spent its own native Standard through a single attack on its own earlier slot; the later rider Mount adopted it without a cooldown clear, preparation replay, allocation refresh or second participation, and the prior Standard debt was changed only by native time passage until the next lawful native preparation.";
                case "CM03-mount-spent-all": return "The mount spent its whole participation through a native full attack on its own earlier slot; the later rider Mount adopted it granting nothing: no cooldown clear, preparation replay, allocation refresh or second participation, with the prior debt changed only by native time passage until the next lawful native preparation.";
                case "CM03-rider-other-action": return "The rider spent its native Standard through a single ranged attack, retained one lawful Move, and the Mount charged exactly that Move without changing the prior Standard debt.";
                case "CM03-unrelated-candidate-between": return "With an unrelated actor between rider and mount in the exact native roster, the Mount adopted the pair and the unrelated candidate was prepared and took its one native turn exactly once, neither skipped nor duplicated, before the next paired round.";
                default: return "The exact native variant evidence is complete.";
            }
        }

        // Rider-without-move: after the Acting setup ground order, one single ranged attack
        // spends the Standard so no Move remains; the Mount is then refused at availability
        // and native targeting before any command, shell, process, dispatch, cost or
        // transition exists, and the exhausted turn ends through one native End input.
        private void TickChunk6aRiderExhaustion()
        {
            var variant = Chunk6aActionEconomyVariant;
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (chunk6aEconomyExhaustStage == 3)
            {
                if (turn == null || ReferenceEquals(turn, chunk6aEconomyExhaustTurn)) return;
                ((JObject)chunk6aEconomyRefusal["end"])["afterEnd"] = CaptureChunk6aEconomyBoundary();
                string failure = null;
                try { NativeActionEconomyEvidence.AssertStructure(chunk6aEconomy); }
                catch (Exception exception) { failure = exception.Message; }
                AddRow(variant.Row, failure == null, failure ?? "A rider that spent its Standard and had no lawful Move left was refused combat Mount at availability and native targeting before any command, shell, process, dispatch, cost, generation change or transition; the exhausted native turn ended through one native End input.", chunk6aEconomy);
                chunk6aStage = 99; BeginCleanup(); return;
            }
            if (!Chunk6aIdle) return;
            if (turn?.Unit != rider || !turn.IsActing || !ReferenceEquals(turn, chunk6aMountTurn))
                throw new InvalidOperationException("Rider exhaustion lost the rider's exact Acting allocation.");
            if (chunk6aEconomyExhaustStage == 0)
            {
                if (!EnsureChunk6aRiderSelection(variant.Row)) return;
                chunk6aEconomyExhaustTurn = turn;
                chunk6aEconomyExhaustion = new JObject { ["kind"] = "single-attack", ["turnObject"] = RuntimeHelpers.GetHashCode(turn), ["round"] = controller.RoundNumber, ["before"] = CaptureChunk6aEconomyBoundary() };
                chunk6aEconomy["exhaustion"] = chunk6aEconomyExhaustion;
                chunk6aEconomyExhaustCommand = IssueChunk6aNativeAttack(rider, false, chunk6aEconomyExhaustion);
                chunk6aEconomyExhaustStage = 1; ResetLeafClock(); return;
            }
            if (chunk6aEconomyExhaustStage == 1)
            {
                if (!Chunk6aEconomyCommandSettled(chunk6aEconomyExhaustCommand, rider)) return;
                FinishChunk6aEconomyCommand(chunk6aEconomyExhaustion, rider, chunk6aEconomyExhaustCommand);
                // The terminal result and the remaining actions are recorded facts. The refusal is
                // requested only when no lawful Move remains: with a Move left the same native click
                // would commit a real Mount that this variant does not own.
                chunk6aEconomyExhaustion["riderHasMoveAfter"] = rider.HasMoveAction();
                if (rider.HasMoveAction())
                {
                    AddRow(variant.Row, false, "The rider still holds a native Move after its Standard and setup Move, so the refusal cannot be requested without committing a real Mount: " + CaptureChunk6aSpent(rider).ToString(Formatting.None), chunk6aEconomy);
                    chunk6aStage = 99; BeginCleanup(); return;
                }
                if (!EnsureChunk6aRiderSelection(variant.Row)) return;
                var before = CaptureChunk6aEconomyBoundary();
                var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                var data = rider.Descriptor.Abilities.GetAbility(nativeControls.MountAbility)?.Data;
                var controlsBefore = nativeControls.CaptureSnapshot(); var ledgerBefore = Chunk6aLedgerCounters();
                var shellsBefore = nativeControls.NativeRelationshipShellCount; var bindingsBefore = nativeControls.NativeRelationshipProcessBindingCount;
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-rider-without-move-click");
                var controlsAfter = nativeControls.CaptureSnapshot();
                var after = CaptureChunk6aEconomyBoundary();
                chunk6aEconomyRefusal = new JObject
                {
                    ["contract"] = "mount-refused-at-availability-and-native-targeting-before-commitment",
                    ["before"] = before,
                    ["availability"] = new JObject { ["visible"] = availability.IsVisible, ["enabled"] = availability.IsEnabled, ["transitionReady"] = availability.IsTransitionReady, ["reason"] = availability.Reason },
                    ["canTarget"] = nativeControls.CanTarget(NativeMountedControlKind.MountCompanion, rider, horse),
                    ["abilityAvailableForCast"] = data?.IsAvailableForCast == true,
                    ["targetRejection"] = playerAction.DescribeNativeMountTargetRejection(rider, horse),
                    ["click"] = observations["chunk6a-rider-without-move-click"]?.DeepClone(), ["clicked"] = clicked,
                    ["after"] = after,
                    ["controls"] = new JObject {
                        ["before"] = new JObject { ["shellCount"] = shellsBefore, ["processBindings"] = bindingsBefore, ["dispatchAccepted"] = controlsBefore.DispatchAcceptedCount, ["dispatchRejected"] = controlsBefore.DispatchRejectedCount, ["activationCount"] = controlsBefore.ActivationRecordCount },
                        ["after"] = new JObject { ["shellCount"] = nativeControls.NativeRelationshipShellCount, ["processBindings"] = nativeControls.NativeRelationshipProcessBindingCount, ["dispatchAccepted"] = controlsAfter.DispatchAcceptedCount, ["dispatchRejected"] = controlsAfter.DispatchRejectedCount, ["activationCount"] = controlsAfter.ActivationRecordCount } },
                    ["ledgerBefore"] = ledgerBefore, ["ledgerAfter"] = Chunk6aLedgerCounters(),
                    ["riderCommandsEmptyAfter"] = rider.Commands.Empty, ["mountCommandsEmptyAfter"] = horse.Commands.Empty,
                    ["events"] = new JArray(allocationTrace.EventsSince((int)before["allocationSequence"]).Take((int)after["allocationSequence"] - (int)before["allocationSequence"])),
                    ["traceComplete"] = allocationTrace.Complete
                };
                chunk6aEconomy["refusal"] = chunk6aEconomyRefusal;
                chunk6aEconomyExhaustStage = 2; ResetLeafClock(); return;
            }
            if (chunk6aEconomyExhaustStage == 2)
            {
                if (controller.WaitingForUI || GetPendingNextUnit(controller) != null || !turn.CanEndTurnAndNoActing()) return;
                if (!EnsureChunk6aRiderSelection(variant.Row)) return;
                chunk6aEconomyRefusal["end"] = new JObject { ["method"] = "Kingmaker.Game.PauseBind", ["token"] = "06000CB7",
                    ["moduleMvid"] = typeof(Game).Assembly.ManifestModule.ModuleVersionId.ToString(), ["count"] = 1, ["beforeEndInput"] = CaptureChunk6aEconomyBoundary() };
                chunk6aEconomyExhaustStage = 3;
                Game.Instance.PauseBind();
                ((JObject)chunk6aEconomyRefusal["end"])["afterEndInput"] = CaptureChunk6aEconomyBoundary();
                ResetLeafClock();
            }
        }

        // CM05 variants: the voluntary Dismount on the Mount's own allocation (immediate) or
        // on the next rider allocation after one rider or mount expenditure, then the
        // split-release observation through the mount's separate turn in the following round.
        private void TickChunk6aEconomyDismount()
        {
            var variant = Chunk6aActionEconomyVariant;
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (chunk6aStage == 60)
            {
                if (variant.Dismount == "immediate")
                {
                    if (!Chunk6aIdle) return;
                    if (!ReferenceEquals(turn, chunk6aMountTurn) || turn.Unit != rider || !turn.IsActing)
                        throw new InvalidOperationException("Immediate Dismount lost the Mount's own acting allocation.");
                }
                else if (!PrepareChunk6aDismountTurn(turn)) return;
                if (!Chunk6aIdle) return;
                var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
                if (!availability.IsEnabled)
                {
                    FailCurrent(variant.Row, "Combat Dismount was unavailable when the variant required it: \"" + availability.Reason + "\"; rider=" + CaptureChunk6aSpent(rider).ToString(Formatting.None));
                    BeginCleanup(); return;
                }
                if (!EnsureChunk6aRiderSelection(variant.Row)) return;
                if (variant.Dismount != "immediate") CaptureChunk6aDismountReady();
                var pre = CaptureChunk6aEconomyBoundary();
                chunk6aEconomyDismount["preDismount"] = pre;
                if (variant.Dismount == "immediate")
                {
                    var mountProof = Chunk6aEconomyProof("positive-mount");
                    var bridge = NativeRelationshipTerminalBridge.Capture(allocationTrace, mountProof, pre, true);
                    chunk6aEconomyDismount["mountTerminalBridge"] = bridge;
                    NativePassiveResourceEvidence.AssertComplete(bridge);
                }
                chunk6aPreDismount = CaptureChunk6aState("dismount-before");
                BeginChunk6aCommandWindow(nativeControls.DismountAbility.AssetGuid);
                chunk6aDismountLedgerBefore = Chunk6aLedgerCounters();
                chunk6aDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aDismountClicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "chunk6a-economy-dismount-click");
                chunk6aCommandWindow.ClickCompleted(chunk6aDismountClicked);
                if (!chunk6aDismountClicked)
                {
                    FailCurrent(variant.Row, "Exact native combat Dismount target click was not admitted.");
                    BeginCleanup(); return;
                }
                chunk6aEconomyDismountTurn = turn;
                chunk6aStage = 61; ResetLeafClock(); return;
            }
            if (chunk6aStage == 61)
            {
                if (relationship.State != RelationshipState.Unmounted || !Chunk6aIdle || chunk6aCommandWindow?.Terminal != true) return;
                var dismountProof = FinishChunk6aCommandWindow("combat-dismount", true, 0, false);
                chunk6aEconomyDismount["proofWindow"] = "combat-dismount";
                chunk6aEconomyDismount["proofPass"] = dismountProof["pass"];
                CaptureChunk6aState("dismount-after");
                chunk6aEconomyDismount["afterDismount"] = CaptureChunk6aEconomyBoundary();
                if (variant.Dismount != "immediate") chunk6aEconomyDismount["laterTurn"] = chunk6aDismountTurn.DeepClone();
                chunk6aEconomyDismountRound = controller.RoundNumber;
                chunk6aEconomyRelease = new JObject { ["contract"] = "split-release-observed-through-the-mount-separate-turn-of-the-following-round",
                    ["dismountRound"] = chunk6aEconomyDismountRound, ["turns"] = new JArray() };
                chunk6aEconomy["release"] = chunk6aEconomyRelease;
                chunk6aStage = 62; ResetLeafClock(); return;
            }
            if (chunk6aStage != 62) throw new InvalidOperationException("Economy Dismount stage differs.");
            if (!chunk6aEconomyEndClicked)
            {
                if (!ReferenceEquals(turn, chunk6aEconomyDismountTurn) || turn.Unit != rider)
                    throw new InvalidOperationException("The dismounted rider allocation ended before the one native End input.");
                if (!Chunk6aIdle || controller.WaitingForUI || GetPendingNextUnit(controller) != null || !turn.CanEndTurnAndNoActing()) return;
                if (!EnsureChunk6aRiderSelection(variant.Row)) return;
                chunk6aEconomyRelease["endInput"] = new JObject { ["method"] = "Kingmaker.Game.PauseBind", ["token"] = "06000CB7",
                    ["moduleMvid"] = typeof(Game).Assembly.ManifestModule.ModuleVersionId.ToString(), ["count"] = 1, ["beforeEndInput"] = CaptureChunk6aEconomyBoundary() };
                chunk6aEconomyEndClicked = true;
                Game.Instance.PauseBind();
                ((JObject)chunk6aEconomyRelease["endInput"])["afterEndInput"] = CaptureChunk6aEconomyBoundary();
                ResetLeafClock(); return;
            }
            if (!Game.Instance.Player.IsInCombat || !rider.IsInCombat || !horse.IsInCombat)
                throw new InvalidOperationException("The encounter ended before the split-release observation completed.");
            if (turn != null && chunk6aEconomyReleaseSeen.Add(turn))
            {
                var turns = (JArray)chunk6aEconomyRelease["turns"];
                if (turns.Count >= 128) throw new InvalidOperationException("Split-release observation bound exceeded.");
                turns.Add(CaptureChunk6aEconomyBoundary());
            }
            if (controller.RoundNumber > chunk6aEconomyDismountRound + 1)
            {
                // Bounded observation: every turn seen since the Dismount is retained with the row.
                chunk6aEconomyRelease["observationEnd"] = CaptureChunk6aEconomyBoundary();
                AddRow(variant.Row, false, "The mount's separate native turn did not occur in the round after the release round; the bounded release observation is retained.", chunk6aEconomy);
                chunk6aStage = 99; BeginCleanup(); return;
            }
            if (turn?.Unit == horse)
            {
                var afterDismount = (JObject)chunk6aEconomyDismount["afterDismount"];
                var mountTurn = CaptureChunk6aEconomyBoundary();
                chunk6aEconomyRelease["mountTurn"] = mountTurn;
                chunk6aEconomyRelease["events"] = new JArray(allocationTrace.EventsSince((int)afterDismount["allocationSequence"]).Take((int)mountTurn["allocationSequence"] - (int)afterDismount["allocationSequence"]));
                chunk6aEconomyRelease["traceComplete"] = allocationTrace.Complete;
                chunk6aEconomyRelease["duplicateMountTurn"] = controller.RoundNumber == chunk6aEconomyDismountRound;
                string failure = null;
                try { NativeActionEconomyEvidence.AssertStructure(chunk6aEconomy); }
                catch (Exception exception) { failure = exception.Message; }
                AddRow(variant.Row, failure == null, failure ?? Chunk6aEconomyDismountDetail(variant), chunk6aEconomy);
                chunk6aStage = 99; BeginCleanup(); return;
            }
            if (turn != null) TryEndPhase3gFixtureTurn(turn);
        }

        private static string Chunk6aEconomyDismountDetail(NativeActionEconomyEvidence.Variant variant)
        {
            switch (variant.Row)
            {
                case "CM05-immediately-after-mount": return "Immediately after a legal adjacent combat Mount on the same native allocation, the voluntary Dismount paid exactly its own native Move, retained every rider and mount debt, split the activation without a duplicate mount turn in the release round, and the mount's separate participation resumed with one native preparation in the following round.";
                case "CM05-after-rider-expenditure": return "After the rider spent its native Standard on the next allocation, the voluntary Dismount paid exactly its own native Move, retained the rider's Standard debt and the mount's debt, and the mount's separate participation resumed only in the following round.";
                default: return "After the mount spent native movement on the next allocation, the voluntary Dismount paid exactly the rider's own native Move, retained the mount's Move debt, and the mount's separate participation resumed only in the following round.";
            }
        }
    }
}
