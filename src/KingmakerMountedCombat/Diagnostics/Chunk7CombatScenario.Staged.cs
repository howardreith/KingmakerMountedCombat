using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
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
    // 7E X1 F04 staged moving actions (owner-seed-staged, on the TB rider-first engine). The Chunk 6D
    // plans C6D-move-cast-move and C6D-double-move-ranged run on the owner-seed-derived mounted start
    // with rider A's own equipped ranged weapon and one native Cure Light Wounds scroll stack (four
    // factory units built on the equipped entity through the existing NativeCastingItemLease, so every
    // measured spend starts with at least two units). A third row moves the pair beyond weapon range,
    // spends the mount's whole native movement, probes the native exhaustion refusal, and on the next
    // turn shoots from beyond range through the RiderRanged pair command (mount approach, rider shot).
    // The native engine owns every movement, action, cost, check and turn; the scenario only clicks and
    // observes. Nothing is granted, refunded or written.
    internal sealed partial class Chunk7CombatScenario
    {
        internal const string StagedClwGuid = "5590652e1c2225c4ca30c4a699ab3649";
        private const int StagedScrollUnits = 4, StagedMaxLegs = Chunk7StagedGeometry.MaxLegs;
        private const float StagedExhaustedMove = (float)Chunk7StagedGeometry.ExhaustedMove;
        private sealed class StagedStepSpec { internal string Row, Kind; internal int Round; }
        private static StagedStepSpec Spec(string row, string kind, int round) => new StagedStepSpec { Row = row, Kind = kind, Round = round };
        // Ordered native inputs; Round is the offset from rider A's first prepared round N.
        private static readonly StagedStepSpec[] StagedSteps = {
            Spec("C6D-move-cast-move", "move-short", 0), Spec("C6D-move-cast-move", "cast-standard-scroll", 0), Spec("C6D-move-cast-move", "move-short", 0),
            Spec("C6D-double-move-ranged", "move-two-moves", 1), Spec("C6D-double-move-ranged", "attack-ranged", 1),
            Spec("C7-staged-ranged-approach", "move-beyond-range", 2), Spec("C7-staged-ranged-approach", "move-exhaust", 2),
            Spec("C7-staged-ranged-approach", "move-probe", 2),
            // The double-move row's scroll cast is its next-turn control: the ranged attack spent the Standard.
            Spec("C6D-double-move-ranged", "cast-standard-scroll", 2),
            Spec("C7-staged-ranged-approach", "attack-ranged-approach", 3) };
        private bool StagedCase => request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedStagedCase;
        private NativeCastingItemTrace stagedCasts;
        private NativeCastingOriginalSlots stagedSlots;
        private NativeCastingItemLease stagedScroll;
        private Chunk7PairRuleObserver stagedRules;
        private JObject staged, stagedStep, stagedLeg;
        private JArray stagedRecords;
        private UnitCommand stagedCommand;
        private int stagedIndex, stagedStage, stagedRound, stagedSettledFrames, stagedSide = 1;
        private float stagedEnvelope;
        private bool stagedReleased;
        // The up-front geometry plan: the selection (before any lease, placement or input), its record, the
        // selected target point, and the in-combat confirmed inputs and simulation the native legs follow.
        private readonly StagedNativeNavmesh stagedNav = new StagedNativeNavmesh();
        private StagedPlanSelection stagedSelection;
        private StagedPlanInputs stagedPlan;
        private StagedSimulation stagedConfirmation;
        private JObject stagedPlanRecord;
        private Vector3 stagedTarget;
        private static StagedPlanStep[] StagedPlanSteps => StagedSteps.Select(s => new StagedPlanStep(s.Kind, s.Round)).ToArray();
        private static StagedPoint Sp(Vector3 value) => new StagedPoint(value.x, value.y, value.z);
        private static Vector3 Vp(StagedPoint value) => new Vector3((float)value.X, (float)value.Y, (float)value.Z);
        private static JArray Point(StagedPoint value) => new JArray(value.X, value.Y, value.Z);
        // The two native navigation queries of every staged proposal (the same ones the earlier leg search used).
        private sealed class StagedNativeNavmesh : IStagedNavmesh
        {
            public bool Nearest(StagedPoint requested, out StagedPoint goal)
            {
                var nearest = AstarPath.active.GetNearest(Vp(requested));
                goal = Sp(nearest.clampedPosition); return nearest.node != null && nearest.node.Walkable;
            }
            public StagedPoint Trace(StagedPoint from, StagedPoint to) => Sp(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(Vp(from), Vp(to)));
        }
        // b201-b203 st: the leg-by-leg search walked into navmesh edges (round 3, Spider Cave): the exhaustion
        // band was unreachable from a 19.24 m edge point (b201/b202), then the beyond-range leg found no
        // walkable band point from 13.36 m (b203). The geometry of the WHOLE plan is now chosen up front,
        // before any lease, target placement or input: target placements around mount A's start in a fixed
        // recorded order, each admitted only when every declared leg (move-cast-move, double move and shot,
        // beyond range, exhaustion, probe, approach) simulates eligible from the start under the unchanged
        // bands, the stop-short margin, the native move budgets and the 8-leg limit. Every candidate and the
        // first refusing leg and term of each are recorded; none admitted refuses as uncovered on this fixture.
        private Vector3 SelectStagedGeometry()
        {
            Require(AstarPath.active != null, "Active native navigation graph is unavailable for the staged geometry plan.");
            var weapon = ar.GetFirstWeapon();
            Require(weapon != null && weapon.Blueprint.IsRanged,
                "Rider A has no equipped native ranged weapon; the F04 ranged rows are uncovered on this fixture.");
            var range = weapon.Blueprint.AttackRange.Meters;
            var envelope = am.View.Corpulence + Chunk7StagedGeometry.PlannedTargetCorpulence + range;
            var selection = Chunk7StagedGeometry.Select(Sp(am.Position), Sp(ar.Position), envelope, am.CurrentSpeedMps, StagedPlanSteps, stagedNav, StagedPlanRevision);
            stagedPlanRecord = JObject.Parse(Chunk7StagedGeometry.PlanJson(selection, range, am.View.Corpulence));
            if (selection.Chosen == null)
            {
                evidence["staged"] = new JObject { ["contract"] = StagedContract, ["geometryPlan"] = stagedPlanRecord, ["complete"] = false };
                throw new InvalidOperationException("Staged geometry refused before any input: no target placement admits the whole declared leg plan (" +
                    selection.Candidates.Count + " candidates recorded); owner-seed-staged is uncovered on this fixture.");
            }
            stagedSelection = selection;
            return Vp(selection.Chosen.Goal);
        }

        // Out of combat, before the encounter: rider A's own ranged weapon is verified and the one
        // disposable native scroll stack is built on its equipped quick-slot entity.
        private void PrepareStaged()
        {
            var weapon = ar.GetFirstWeapon();
            staged = new JObject { ["contract"] = StagedContract, ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId,
                ["weapon"] = weapon?.Blueprint.AssetGuid, ["weaponRanged"] = weapon?.Blueprint.IsRanged == true,
                ["weaponRangeMeters"] = weapon?.Blueprint.AttackRange.Meters, ["scrollUnits"] = StagedScrollUnits, ["geometryPlan"] = stagedPlanRecord,
                ["plan"] = new JArray(StagedSteps.Select(s => new JObject { ["row"] = s.Row, ["kind"] = s.Kind, ["round"] = s.Round })),
                ["defensiveCheck"] = new JObject { ["covered"] = false, ["reason"] = StagedDefensiveUncovered } };
            evidence["staged"] = staged;
            Require(weapon != null && weapon.Blueprint.IsRanged,
                "Rider A has no equipped native ranged weapon; the F04 ranged rows are uncovered on this fixture.");
            stagedCasts = new NativeCastingItemTrace(ar, am);
            stagedSlots = new NativeCastingOriginalSlots(ar, stagedCasts);
            stagedScroll = new NativeCastingItemLease(ar, stagedCasts, NativeCastingItemLease.CureScroll, stagedSlots);
            stagedScroll.Acquire(StagedScrollUnits);
            var ability = stagedScroll.Item.Ability?.Data;
            staged["scrollAbility"] = stagedCasts.Ability(ability);
            Require(ability != null && ReferenceEquals(ability.SourceItem, stagedScroll.Item) && ability.Blueprint.AssetGuid == StagedClwGuid &&
                ability.IsAvailableForCast, "Rider A cannot use the native Cure Light Wounds scroll; the F04 cast parts are uncovered on this fixture.");
        }
        internal const string StagedDefensiveUncovered = "The pair never stands inside the diagnostic target's threatened area during a cast " +
            "(target placed 7 to 10 m away by the up-front geometry plan), and a self-targeted Cure Light Wounds scroll acts at about 0.55 s, before the native one-second " +
            "casting-defensively window; the defensive check is not exercised here. Concentration runs natively only if damage lands during a cast.";
        private void BeginStaged()
        {
            Require(StagedCase && OwnerSeedDerived && SelectedScenario == RiderFirst && stagedScroll != null, "Staged admission differs.");
            stagedRules = new Chunk7PairRuleObserver(ar, am, br, bm, target);
            stagedEnvelope = am.View.Corpulence + target.View.Corpulence + ar.GetFirstWeapon().Blueprint.AttackRange.Meters;
            staged["envelope"] = stagedEnvelope; staged["mountCorpulence"] = am.View.Corpulence; staged["targetCorpulence"] = target.View.Corpulence;
            stagedRecords = new JArray(); staged["steps"] = stagedRecords;
            // Before any input: the plan holds for the spawned target, its actual envelope, the mount's in-combat
            // native speed and position (the selected plan simulated again and recorded), or the case refuses.
            Require(stagedSelection != null && Math.Abs(stagedEnvelope - stagedSelection.Envelope) <= .01,
                "The spawned target's weapon envelope differs from the planned one; the staged plan is void (refused before input).");
            Require(Flat(am.Position, Vp(stagedSelection.MountStart)) <= .05f,
                "Mount A moved between the geometry plan and combat start (refused before input).");
            Require(Flat(target.Position, stagedTarget) <= Chunk7StagedGeometry.PlacementMaxOffset,
                "The diagnostic target did not spawn at its planned placement (refused before input).");
            stagedPlan = Chunk7StagedGeometry.Inputs(Sp(target.Position), stagedEnvelope, am.CurrentSpeedMps, StagedPlanSteps, StagedPlanRevision);
            stagedSide = 1;
            stagedConfirmation = Chunk7StagedGeometry.Simulate(stagedPlan, stagedNav, Chunk7StagedGeometry.Start(Sp(am.Position)));
            staged["geometryConfirmation"] = JObject.Parse(Chunk7StagedGeometry.ConfirmationJson(Sp(am.Position), stagedPlan, stagedConfirmation));
            Require(stagedConfirmation.Feasible, "The selected staged plan is not feasible from the in-combat start (" + stagedConfirmation.Describe() +
                "); refused before input.");
            stagedIndex = 0; StagedStage(0);
        }
        private void StagedStage(int value) { stagedStage = value; stagedSettledFrames = 0; stepStarted = clock.Elapsed.TotalSeconds; }
        private JObject StagedState(string label)
        {
            var state = WorldSnapshot(label); var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            state["turnStatus"] = turn?.Status.ToString(); state["turn"] = trace.ObjectIdentity(turn);
            state["relationship"] = a.Relationship.State.ToString();
            state["mountUsedOneMove"] = am.UsedOneMoveAction(); state["mountUsedTwoMove"] = am.UsedTwoMoveAction();
            state["mountHasMove"] = am.HasMoveAction(); state["riderHasStandard"] = ar.HasStandardAction();
            state["riderCommandsEmpty"] = ar.Commands.Empty; state["mountCommandsEmpty"] = am.Commands.Empty;
            state["activePairCommand"] = a.Combat.HasActiveCommand; state["pairMovement"] = a.Combat.HasActiveGroundMovement;
            state["processesSettled"] = stagedCasts.ProcessesSettled;
            state["nativeAbilitiesPending"] = NativeSaveEffectBoundary.HasUnresolvedAbilities();
            state["projectilesPending"] = NativeSaveEffectBoundary.HasUnresolvedProjectiles();
            state["riderPosition"] = Point(ar.Position); state["mountPosition"] = Point(am.Position);
            state["targetDistance"] = Flat(am.Position, target.Position);
            state["riderEngaged"] = target.CombatState.IsEngage(ar); state["mountEngaged"] = target.CombatState.IsEngage(am);
            state["items"] = new JArray(stagedScroll.Snapshot());
            return state;
        }
        private static JObject StagedCommand(NativeActorAllocationTrace costs, UnitCommand command) => command == null ? null : new JObject {
            ["identity"] = costs.ObjectIdentity(command), ["type"] = command.GetType().Name, ["executor"] = command.Executor?.UniqueId,
            ["createdByPlayer"] = command.CreatedByPlayer, ["started"] = command.IsStarted, ["acted"] = command.IsActed,
            ["finished"] = command.IsFinished, ["result"] = command.Result.ToString(), ["commandType"] = command.Type.ToString(),
            ["timeSinceStart"] = command.TimeSinceStart };
        private bool StagedSettled()
        {
            var settled = (stagedCommand == null || stagedCommand.IsFinished) && !a.Combat.HasActiveCommand && !a.Combat.HasActiveGroundMovement &&
                ar.Commands.Empty && am.Commands.Empty && stagedCasts.ProcessesSettled && !NativeSaveEffectBoundary.HasUnresolvedAbilities() &&
                !NativeSaveEffectBoundary.HasUnresolvedProjectiles() && !ar.AreHandsBusyWithAnimation && !am.AreHandsBusyWithAnimation &&
                am.View?.AgentASP?.IsReallyMoving != true && target.Commands.Empty;
            if (!settled) { stagedSettledFrames = 0; return false; }
            return ++stagedSettledFrames >= 10;
        }
        private void TickStaged()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            staged["progress"] = new JObject { ["index"] = stagedIndex, ["stage"] = stagedStage, ["round"] = controller.RoundNumber,
                ["turnActor"] = turn?.Unit?.UniqueId };
            var spec = StagedSteps[stagedIndex];
            if (stagedStage == 0)
            {
                if (stagedIndex == 0)
                {
                    if (EndSurprisedRiderTurn(turn, staged)) return;
                    if (!ReadyPrincipal(a)) { if (turn?.Unit != target && turn?.Unit != ar) EndNative(turn); return; }
                    stagedRound = controller.RoundNumber; staged["firstRound"] = stagedRound; actingPair = a; actingIndex = 0; actingTurn = turn;
                }
                else if (StagedSteps[stagedIndex - 1].Round == spec.Round)
                {
                    // The same native activation continues: the turn must not have changed.
                    Require(ReferenceEquals(turn, actingTurn), "Staged step " + spec.Kind + " lost rider A's native turn inside its activation.");
                    if (!ReadyPrincipal(a)) return;
                }
                else
                {
                    if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                    Require(controller.RoundNumber <= stagedRound + spec.Round, "Staged step " + spec.Kind + " missed its native round.");
                    var dueRound = controller.RoundNumber == stagedRound + spec.Round;
                    if (!dueRound || !ReadyPrincipal(a)) { if (turn?.Unit != target && (turn?.Unit != ar || !dueRound)) EndNative(turn); return; }
                    actingTurn = turn;
                }
                IssueStagedStep(spec); StagedStage(1); return;
            }
            if (!StagedSettled()) return;
            if (stagedLeg != null)
            {
                FinishStagedLeg();
                if (StagedWantsAnotherLeg(spec)) { IssueStagedLeg(spec); StagedStage(1); return; }
            }
            FinishStagedStep(spec);
            if (stagedIndex == 2) Pass("C7-combat-staged-move-cast-move");
            if (stagedIndex == 8) Pass("C7-combat-staged-double-move-ranged");
            if (++stagedIndex < StagedSteps.Length) { StagedStage(0); return; }
            FinishStaged();
        }
        private void IssueStagedStep(StagedStepSpec spec)
        {
            stagedStep = new JObject { ["index"] = stagedIndex, ["row"] = spec.Row, ["kind"] = spec.Kind, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["before"] = StagedState(spec.Kind + "-before"), ["traceSequenceBefore"] = trace.EventCount, ["castEventsBefore"] = stagedCasts.EventCount,
                ["ruleCountBefore"] = stagedRules.EventCount };
            stagedRecords.Add(stagedStep); stagedCommand = null; stagedLeg = null;
            switch (spec.Kind)
            {
                case "cast-standard-scroll": IssueStagedCast(); break;
                case "attack-ranged": case "attack-ranged-approach": IssueStagedRanged(spec.Kind == "attack-ranged-approach"); break;
                default: stagedStep["legs"] = new JArray(); IssueStagedLeg(spec); break;
            }
        }
        // One native ground order (one leg) on rider A's selection; the mount executes it natively.
        private void IssueStagedLeg(StagedStepSpec spec)
        {
            var legs = (JArray)stagedStep["legs"];
            var leg = new JObject { ["index"] = legs.Count }; legs.Add(leg); stagedLeg = leg;
            // The native leg follows the confirmed plan: the same ordered search from the mount's actual position,
            // movement time and side, each proposal admitted only when the remainder of the declared plan still
            // simulates eligible from its goal (the plan continuation term; it subsumes the earlier exhaustion
            // lookahead). When the native result left the plan (a mover stopping short), the declared geometry
            // alone is searched again (recorded apart as fallbackProposals). Every proposal records its first
            // refusing term; the proposals are recorded before the search, so a refusal keeps every proposal.
            var start = am.Position; var proposals = new JArray(); leg["proposals"] = proposals;
            var cursor = new StagedCursor { Step = stagedIndex, Leg = legs.Count - 1, Time = am.CombatState.Cooldown.MoveAction, Side = stagedSide, Position = Sp(start) };
            stagedSide = -stagedSide;
            leg["cursor"] = new JObject { ["step"] = cursor.Step, ["leg"] = cursor.Leg, ["time"] = cursor.Time, ["side"] = cursor.Side };
            var planned = stagedConfirmation.Legs.FirstOrDefault(l => l.Step == cursor.Step && l.Leg == cursor.Leg && l.Chosen >= 0);
            leg["plannedGoal"] = planned == null ? null : Point(planned.Proposals[planned.Chosen].Goal);
            var search = Chunk7StagedGeometry.SearchLeg(stagedPlan, stagedNav, cursor, (c, p) => Chunk7StagedGeometry.Continuation(stagedPlan, stagedNav, c, p));
            StagedProposals(search, proposals); var recorded = proposals.Count;
            if (search.Chosen < 0)
            {
                // The plan is abandoned from here: the native result left it and no proposal continues it.
                var fallback = new JArray(); leg["fallbackProposals"] = fallback; leg["planAbandoned"] = true;
                search = Chunk7StagedGeometry.SearchLeg(stagedPlan, stagedNav, cursor, null);
                StagedProposals(search, fallback); recorded += fallback.Count;
            }
            if (search.Chosen < 0)
                throw new InvalidOperationException("No walkable native destination satisfied the declared geometry (" + recorded + " proposals recorded).");
            var destination = Vp(search.Proposals[search.Chosen].Goal);
            leg["matchesPlan"] = planned != null && Flat(destination, Vp(planned.Proposals[planned.Chosen].Goal)) <= .001f;
            leg["destination"] = Point(destination); leg["plannedDistance"] = Flat(start, destination);
            leg["before"] = StagedState("leg-before"); leg["traceSequenceBefore"] = trace.EventCount; leg["castEventsBefore"] = stagedCasts.EventCount;
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            Chunk7NativeFixture.Select(ar); Game.Instance.IsPaused = true;
            var clicked = false; var cycles = 0; string path;
            try
            {
                using (var input = new NativeOrdinaryAttackInput(destination))
                {
                    input.Predict();
                    while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                    leg["fiveFootStep"] = turn.EnabledFiveFootStep; leg["singleActionMove"] = turn.EnabledSingleActionMove;
                    clicked = input.Click(); path = "pointer";
                }
            }
            catch (InvalidOperationException error)
            {
                // The native pointer priority did not select the ground handler (an exhausted mount): the
                // same native ground API the 6D movement rows used, with the exact reason recorded.
                leg["pointerRefusal"] = error.Message; leg["fiveFootStep"] = turn.EnabledFiveFootStep; leg["singleActionMove"] = turn.EnabledSingleActionMove;
                ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false); clicked = true; path = "ground-handler";
            }
            Game.Instance.IsPaused = false;
            leg["inputPath"] = path; leg["cursorCycles"] = cycles; leg["clicked"] = clicked;
            var mountMove = am.Commands.Move as UnitMoveTo; var riderMove = ar.Commands.Move as UnitMoveTo;
            stagedCommand = (UnitCommand)mountMove ?? riderMove;
            if (stagedCommand != null) issuedCommands.Add(stagedCommand);
            leg["carrier"] = StagedCommand(trace, stagedCommand); leg["riderMoveSlot"] = riderMove != null;
            leg["afterInput"] = StagedState("leg-after-input");
        }
        private void FinishStagedLeg()
        {
            stagedLeg["after"] = StagedState("leg-after"); stagedLeg["terminal"] = StagedCommand(trace, stagedCommand);
            stagedLeg["traceSequenceAfter"] = trace.EventCount;
            stagedLeg["castEvents"] = stagedCasts.EventsSince((int)stagedLeg["castEventsBefore"]);
            stagedLeg["displacement"] = Flat(Vec((JArray)stagedLeg["before"]["mountPosition"]), am.Position);
            stagedCommand = null;
        }
        // The earlier per-proposal record shape, plus the plan continuation's first refusing leg and term.
        private static void StagedProposals(StagedLeg search, JArray into)
        {
            foreach (var p in search.Proposals)
                into.Add(new JObject { ["angle"] = p.Angle, ["length"] = p.Length, ["requested"] = Point(p.Requested), ["goal"] = Point(p.Goal),
                    ["walkable"] = p.Walkable, ["traceResidual"] = Chunk7StagedGeometry.Flat(p.Trace, p.Goal), ["geometryAccepted"] = p.Refusal == null,
                    ["geometryRefusal"] = p.BaseAccepted ? p.Refusal : null, ["refusal"] = p.Refusal, ["eligible"] = p.Refusal == null, ["continuation"] = p.Continuation });
        }
        private static Vector3 Vec(JArray p) => new Vector3((float)p[0], (float)p[1], (float)p[2]);
        // TB budget legs: legs repeat (bounded) until the mount itself reports its second Move or its
        // exhausted native movement budget; exhaustion is measured on the cooldown (frozen 203 TB).
        private bool StagedWantsAnotherLeg(StagedStepSpec spec)
        {
            var legs = ((JArray)stagedStep["legs"]).Count;
            if (legs >= StagedMaxLegs) return false;
            if (spec.Kind == "move-two-moves") return !am.UsedTwoMoveAction();
            if (spec.Kind == "move-exhaust") return am.CombatState.Cooldown.MoveAction < StagedExhaustedMove;
            return false;
        }
        private void IssueStagedCast()
        {
            stagedScroll.RequireExactlyEquipped("Staged scroll cast", 2);
            var ability = stagedScroll.Item.Ability?.Data;
            Require(ability != null && ReferenceEquals(ability.SourceItem, stagedScroll.Item) && ability.Caster.Unit == ar,
                "Native scroll ability does not source the exact equipped fixture stack on rider A.");
            Chunk7NativeFixture.Select(ar); Game.Instance.IsPaused = true;
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(ability);
            stagedStep["ability"] = stagedCasts.Ability(ability); stagedStep["target"] = ar.UniqueId;
            stagedStep["selected"] = ReferenceEquals(handler.Ability, ability);
            var resolved = handler.GetTarget(ar.View.gameObject, ar.Position, ability);
            stagedStep["canTarget"] = resolved != null && ability.CanTarget(resolved);
            try { stagedStep["clicked"] = handler.OnClick(ar.View.gameObject, ar.Position, 0, false, false); }
            finally { if (handler.Ability != null) handler.SetAbility(null); Game.Instance.IsPaused = false; }
            var shells = ar.Commands.Raw.Concat(ar.Commands.Queue).OfType<UnitUseAbility>()
                .Where(c => c.Spell.Blueprint == ability.Blueprint).Distinct().ToArray();
            stagedStep["admittedShellCount"] = shells.Length;
            Require(shells.Length <= 1, "One normal scroll input admitted multiple shells.");
            var shell = shells.SingleOrDefault();
            stagedStep["shell"] = shell == null ? null : stagedCasts.Shell(shell); stagedStep["costShell"] = trace.ObjectIdentity(shell);
            stagedCommand = shell; if (shell != null) issuedCommands.Add(shell);
            stagedStep["afterInput"] = StagedState("cast-after-input");
        }
        // The mounted rider's stock ranged click through the ordinary native unit input; the product's
        // RiderRanged pair command owns admission, any mount approach and the native shot.
        private void IssueStagedRanged(bool approach)
        {
            stagedStep["envelope"] = stagedEnvelope; stagedStep["startDistance"] = Flat(am.Position, target.Position);
            if (approach) Require(Flat(am.Position, target.Position) > stagedEnvelope + .5f, "The pair did not start its ranged approach beyond weapon range.");
            Require(targetService.BeginExpectedAttackDispatch(target), "Exact diagnostic target attack authorization refused.");
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            Chunk7NativeFixture.Select(ar); Game.Instance.IsPaused = true;
            var cycles = 0; bool clicked;
            using (var input = new NativeOrdinaryAttackInput(target))
            {
                input.Predict();
                while (turn.EnabledFullAttack && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                stagedStep["fullAttackMode"] = turn.EnabledFullAttack; clicked = input.Click();
            }
            Game.Instance.IsPaused = false;
            stagedStep["cursorCycles"] = cycles; stagedStep["clicked"] = clicked;
            var command = ar.Commands.Raw.Concat(ar.Commands.Queue).OfType<UnitAttack>().FirstOrDefault(c => ReferenceEquals(c.Target, target));
            stagedCommand = command; if (command != null) issuedCommands.Add(command);
            stagedStep["admitted"] = StagedCommand(trace, command);
            stagedStep["rejectionCodes"] = new JArray(a.Combat.LastRejectionCodes.Select(c => c.ToString()));
            stagedStep["rejectionFeedback"] = a.Combat.LastFeedback;
            stagedStep["afterInput"] = StagedState("ranged-after-input");
            Require(clicked && command != null, "Native ranged click on the target admitted no command.");
        }
        private void FinishStagedStep(StagedStepSpec spec)
        {
            var label = spec.Kind + "-after";
            stagedStep["after"] = StagedState(label); stagedStep["traceSequenceAfter"] = trace.EventCount;
            stagedStep["castEvents"] = stagedCasts.EventsSince((int)stagedStep["castEventsBefore"]);
            stagedStep["rules"] = stagedRules.Since((int)stagedStep["ruleCountBefore"]);
            // Recorded at the stop, before the terminal command is released (its native approach radius is part of the record).
            if (spec.Kind == "attack-ranged-approach") stagedStep["nativeRangeAtStop"] = StagedNativeRange();
            if (stagedStep["legs"] == null) { stagedStep["terminal"] = StagedCommand(trace, stagedCommand); stagedCommand = null; }
            stagedStep["endDistance"] = Flat(am.Position, target.Position);
            var before = (JObject)stagedStep["before"]; var after = (JObject)stagedStep["after"];
            // Pair B never spends inside the window (a finished turn's cooldowns may only decay natively).
            Require((string)after["relationship"] == "Mounted" && new[] { br, bm }.All(actor => NeverSpentNatively(before, after, actor)),
                "Staged step " + spec.Kind + " dismounted pair A or changed pair B's native budget.");
        }
        // F6 (staged v2): the up-front plan uses geometry revision 2 (beyond-range goal aimed at its band centre, a spare
        // planned proposal) and the approach shot records the native range quantities at its stop before any assertion.
        internal const string StagedContract = "owner-seed-staged-v2";
        private const int StagedPlanRevision = 2;
        private JObject StagedNativeRange()
        {
            var weapon = ar.GetFirstWeapon();
            return new JObject { ["contract"] = "owner-seed-staged-approach-range-v1",
                ["riderPosition"] = Point(ar.Position), ["mountPosition"] = Point(am.Position), ["targetPosition"] = Point(target.Position),
                ["mountTargetFlat"] = Flat(am.Position, target.Position), ["riderTargetFlat"] = Flat(ar.Position, target.Position),
                ["mountTargetMechanics"] = GeometryUtils.MechanicsDistance(am.Position, target.Position),
                ["riderTargetMechanics"] = GeometryUtils.MechanicsDistance(ar.Position, target.Position),
                ["riderCorpulence"] = ar.View.Corpulence, ["mountCorpulence"] = am.View.Corpulence, ["targetCorpulence"] = target.View.Corpulence,
                ["weaponRangeMeters"] = weapon?.Blueprint.AttackRange.Meters, ["commandApproachRadius"] = stagedCommand?.ApproachRadius };
        }
        private void FinishStaged()
        {
            var rules = stagedRules.Capture();
            Require((int)rules["dropped"] == 0 && ((JArray)rules["errors"]).Count == 0, "Staged rule observation dropped or failed.");
            staged["rules"] = rules; stagedRules.Dispose(); stagedRules = null;
            ReleaseStagedFixture();
            staged["complete"] = true; Pass("C7-combat-staged-ranged-approach");
            BeginCleanup();
        }
        // The exact disposable stack leaves its slot and the inventory, the original quick slots are
        // observed restored, then the cast observer retires. Also the cleanup path for an early failure.
        // The cast observer is retained on purpose while the lease is unreleased: the lease's own
        // evidence reads its identities, and a retained observer fails the next one closed.
        private void ReleaseStagedFixture()
        {
            if (stagedReleased || staged == null) return;
            Require(stagedCasts == null || stagedCasts.ProcessesSettled && ar.Commands.Empty && am.Commands.Empty,
                "The staged scroll stack is released only after the pair's casts and commands have settled.");
            if (stagedScroll != null)
            {
                stagedScroll.ReleaseOwnedItem(); stagedSlots.Restore(); stagedScroll.Dispose();
                staged["scroll"] = stagedScroll.Evidence; staged["originalSlots"] = stagedSlots.Evidence;
            }
            if (stagedCasts != null) { stagedCasts.Dispose(); staged["castTrace"] = stagedCasts.Capture(); }
            stagedReleased = true;
        }
    }
}
