using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UI.Selection;
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
    // Chunk 6D staged move-cast-move / double-move families and the bounded Chunk 6E native
    // immediate/Swift reaction feasibility rows. One ordered sequence of ordinary native inputs per
    // row inside one rider principal turn (TB) or one settled window (RT), on the exact 6C disposable
    // fixture (AI leases before the exploration Mount, native hostile, rod/potion/scroll leases,
    // cost and cast traces, bounded cleanup). The producer records raw facts and structural
    // completeness only; the external readers own every PASS/FAIL. No TimeMoved, cooldown or
    // per-round write, no second Prepare, no synthetic grant, no movement replay, no production change.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6dStagedScenario(string scenario) =>
            scenario == "chunk6d-staged-rt" || scenario == "chunk6d-staged-tb";
        internal static bool IsChunk6eReactionScenario(string scenario) =>
            scenario == "chunk6e-reaction-rt" || scenario == "chunk6e-reaction-tb";
        internal static bool IsChunk6StagedFamilyScenario(string scenario) =>
            IsChunk6dStagedScenario(scenario) || IsChunk6eReactionScenario(scenario);
        // Every scenario that owns the shared disposable casting fixture, traces and cleanup.
        internal static bool IsCastingFixtureFamilyScenario(string scenario) =>
            IsChunk6cCastingScenario(scenario) || IsChunk6StagedFamilyScenario(scenario);
        private bool IsChunk6dStaged => IsChunk6dStagedScenario(request.Scenario);
        private bool IsChunk6eReaction => IsChunk6eReactionScenario(request.Scenario);
        private bool IsChunk6StagedFamily => IsChunk6StagedFamilyScenario(request.Scenario);
        private bool IsCastingFixtureFamily => IsCastingFixtureFamilyScenario(request.Scenario);
        internal static string CastingMeasurementKeyFor(string scenario) =>
            IsChunk6dStagedScenario(scenario) ? "chunk6dStaged" : IsChunk6eReactionScenario(scenario) ? "chunk6eReaction" : "chunk6cCasting";
        internal static int CastingFamilySchemaVersion(string scenario) =>
            IsChunk6dStagedScenario(scenario) ? 45 : IsChunk6eReactionScenario(scenario) ? 46 : 44;

        internal static readonly string[] Chunk6dStagedCases = { "C6D-move-cast-move", "C6D-cast-then-move",
            "C6D-double-move-ranged", "C6D-movement-exhausted", "C6D-auto-stop-boundary" };
        internal static readonly string[] Chunk6eReactionCases = { "C6E-swift-on-own-turn", "C6E-swift-out-of-turn",
            "C6E-attack-on-mount-observed", "C6E-reaction-window" };
        private const string Entangle = "0fd00984a2c0e0a429cf1a911b4ec5ca";
        private const float StagedShortLegMetres = 2f, StagedLongLegMetres = 7f, StagedExtendedLegMetres = 14f;

        // Ordered native inputs per row. "probe" movement expects the pair's own exhaustion refusal
        // in TB; "hostile-*" rows run the exact diagnostic hostile's own stock attack on the mount.
        internal static string[] StagedPlan(string caseId)
        {
            switch (caseId)
            {
                case "C6D-move-cast-move": return new[] { "move-short", "cast-standard-scroll", "move-short" };
                case "C6D-cast-then-move": return new[] { "cast-standard-scroll", "move-long" };
                case "C6D-double-move-ranged": return new[] { "move-two-moves", "attack-ranged", "cast-standard-scroll" };
                case "C6D-movement-exhausted": return new[] { "move-exhaust", "move-probe" };
                case "C6D-auto-stop-boundary": return new[] { "move-extended", "cast-standard-scroll", "move-short" };
                case "C6E-swift-on-own-turn": return new[] { "cast-swift" };
                case "C6E-swift-out-of-turn": return new[] { "foreign-window-swift" };
                case "C6E-attack-on-mount-observed": return new[] { "hostile-attack-mount" };
                case "C6E-reaction-window": return new[] { "hostile-attack-swift" };
                default: throw new InvalidOperationException("Unknown staged case " + caseId);
            }
        }
        private string[] StagedCases => IsChunk6dStaged ? Chunk6dStagedCases : Chunk6eReactionCases;
        private string StagedCase => StagedCases[stagedCaseIndex];
        private int stagedCaseIndex, stagedStage, stagedStepIndex, stagedStableFrames, stagedRowCostOffset, stagedRowEventOffset, stagedStepCostOffset, stagedStepEventOffset;
        private JObject stagedCaseFacts, stagedStep;
        private JArray stagedStepRecords;
        private UnitCommand stagedCommand;
        private UnitAttack stagedHostileAttack;
        private TurnController stagedRowTurn, stagedSeenTurn;
        private Chunk4IncomingRuleObserver stagedRules;
        private bool stagedSecondaryInputSent, stagedRangedReady;
        private int stagedLegCostOffset, stagedLegEventOffset;
        private JObject stagedLeg;
        // Bounded native budget legs: the mount's move cooldown (two Move actions, 6 s) is the exhaustion
        // measure (frozen 203 TB: HasMoveAction turned false after two legs while the Mammoth kept moving).
        private const int StagedMaxLegs = 8;
        private const float StagedExhaustedMoveSeconds = 5.95f;

        private void BeginChunk6Staged()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Staged families require the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            stagedRules = new Chunk4IncomingRuleObserver(rider, horse);
            observations[CastingMeasurementKeyFor(request.Scenario)] = new JObject {
                ["contract"] = IsChunk6dStaged ? "native-mounted-staged-actions-v1" : "native-mounted-reaction-feasibility-v1",
                ["mode"] = CastingTb ? "TB" : "RT", ["mounted"] = true, ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId,
                ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(StagedCases),
                ["plans"] = new JObject(StagedCases.Select(c => new JProperty(c, new JArray(StagedPlan(c))))),
                ["initialPrepared"] = CastingPreparedInventory(), ["snapshots"] = new JArray(),
                ["autoStopAfterFirstMoveAction"] = SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue,
                ["legMetres"] = new JObject { ["short"] = StagedShortLegMetres, ["long"] = StagedLongLegMetres, ["extended"] = StagedExtendedLegMetres },
                ["policy"] = "Stock Kingmaker owns every movement, action, resource, targeting and process; the mount's native Move budget carries the pair." };
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            castingRod = AcquireCastingItem(NativeCastingItemLease.LesserQuickenRod);
            castingPotion = AcquireCastingItem(NativeCastingItemLease.CurePotion, PotionStackCount);
            castingScroll = AcquireCastingItem(NativeCastingItemLease.CureScroll, ScrollStackCount);
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence));
            if (IsChunk6dStaged)
            {
                // The stock ranged weapon the native proficiency admits (6A CM03 precedent); the mounted
                // stock attack policy decides admission natively and the row records the exact outcome.
                rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                rangedWeaponLease.AcquireCompatibleRanged();
                CastingMeasurement["rangedWeapon"] = new JObject { ["blueprint"] = rider.GetFirstWeapon()?.Blueprint?.AssetGuid,
                    ["ranged"] = rider.GetFirstWeapon()?.Blueprint?.IsRanged, ["category"] = rider.GetFirstWeapon()?.Blueprint?.Category.ToString() };
            }
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }
        private bool StagedSettled() =>
            rider.Commands.Empty && horse.Commands.Empty && castingTrace.ProcessesSettled &&
            !NativeSaveEffectBoundary.HasUnresolvedAbilities() && !NativeSaveEffectBoundary.HasUnresolvedProjectiles() &&
            !rider.AreHandsBusyWithAnimation && horse.View?.AgentASP?.IsReallyMoving != true && !combat.HasActiveGroundMovement;
        private JObject StagedState()
        {
            var state = CastingState();
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            state["mountUsedOneMove"] = horse.UsedOneMoveAction(); state["mountUsedTwoMove"] = horse.UsedTwoMoveAction();
            state["mountHasMove"] = horse.HasMoveAction(); state["mountMoveRestricted"] = horse.IsMoveActionRestricted();
            state["riderHasStandard"] = rider.HasStandardAction(); state["riderHasSwift"] = rider.HasSwiftAction(); state["riderHasMove"] = rider.HasMoveAction();
            state["riderCanAct"] = rider.CombatState.CanActInCombat;
            state["turnActor"] = turn?.Unit?.UniqueId; state["turnIsActing"] = turn?.IsActing; state["turnTimeMoved"] = turn?.TimeMoved;
            state["mountPosition"] = CapturePosition(horse.Position); state["riderPosition"] = CapturePosition(rider.Position);
            state["rejectionCodes"] = new JArray(combat.LastRejectionCodes.Select(c => c.ToString()));
            return state;
        }
        private void TickChunk6Staged()
        {
            var game = Game.Instance; var controller = game.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (CastingTb && !ReferenceEquals(turn, stagedSeenTurn)) { stagedSeenTurn = turn; ResetLeafClock(); }
            CastingMeasurement["progress"] = new JObject { ["stage"] = stagedStage, ["caseIndex"] = stagedCaseIndex,
                ["case"] = stagedCaseIndex < StagedCases.Length ? StagedCase : null, ["step"] = stagedStepIndex,
                ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse) };
            if (stagedStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                // Same entry ordering as the qualified 6C fixture: both reversible AI leases before Mount.
                var isolationReady = PrepareUnmountedHorseAiIsolation() && PrepareCombatMountRiderAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "staged-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                if (IsChunk6dStaged && !stagedRangedReady)
                {
                    if (!rangedWeaponLease.IsReady || game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                    stagedRangedReady = true;
                }
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(CastingTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (CastingTb && pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                BeginTarget(6f, "staged-native-baseline"); ruleProbe.Arm(target, false);
                castingCosts.BeginEncounter(request.RunId + ":staged"); stagedStage = 1; ResetLeafClock(); return;
            }
            if (stagedStage == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat) return;
                if (WaitForNativeCastingPrincipal(CastingTb, turn?.Unit == rider,
                    rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                // A fresh pair round for every row: the previous row's turn must have ended natively.
                if (CastingTb && (ReferenceEquals(turn, stagedRowTurn) || !rider.HasStandardAction() || !rider.HasSwiftAction() ||
                    horse.CombatState.Cooldown.MoveAction > .001f || horse.CombatState.Cooldown.StandardAction > .001f))
                { EndCastingNativeTurn(turn); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider) || horse.View?.AgentASP?.IsReallyMoving == true) return;
                if (!CastingTb && (rider.CombatState.Cooldown.StandardAction > .001f || rider.CombatState.Cooldown.SwiftAction > .001f ||
                    rider.CombatState.Cooldown.MoveAction > .001f || horse.CombatState.Cooldown.MoveAction > .001f)) return;
                if (relationship.State != RelationshipState.Mounted)
                    throw new InvalidOperationException("Staged families require the exact mounted pair at every row start.");
                stagedRowTurn = turn; stagedStepIndex = 0; stagedStableFrames = 0; stagedCommand = null; stagedHostileAttack = null;
                stagedSecondaryInputSent = false; castingShell = null; castingAbility = null; castingPrepared = null;
                castingTrace.BeginCase(StagedCase); stagedRules.BeginCase(StagedCase);
                stagedRowCostOffset = castingCosts.EventCount; stagedRowEventOffset = castingTrace.EventCount;
                stagedStepRecords = new JArray();
                stagedCaseFacts = new JObject { ["case"] = StagedCase, ["plan"] = new JArray(StagedPlan(StagedCase)),
                    ["before"] = StagedState(), ["rowTurnActor"] = turn?.Unit?.UniqueId, ["hostileActor"] = target.UniqueId };
                stagedStage = 2; ResetLeafClock(); return;
            }
            if (stagedStage == 2)
            {
                // Issue the next ordered input, then settle it (stage 3).
                var plan = StagedPlan(StagedCase);
                if (stagedStepIndex >= plan.Length) { FinishStagedRow(); return; }
                var kind = plan[stagedStepIndex];
                stagedStep = new JObject { ["index"] = stagedStepIndex, ["kind"] = kind, ["frame"] = Time.frameCount };
                stagedStepCostOffset = castingCosts.EventCount; stagedStepEventOffset = castingTrace.EventCount;
                stagedCommand = null; stagedHostileAttack = null; stagedSecondaryInputSent = false; stagedStableFrames = 0; stagedLeg = null;
                if (!IssueStagedStep(kind, turn)) return; // waiting for the step's own native precondition
                stagedStepRecords.Add(stagedStep); stagedStage = 3; ResetLeafClock(); return;
            }
            if (stagedStage == 3)
            {
                ObserveStagedStep(turn);
                if (!StagedStepTerminal()) return;
                if (!StagedSettled()) { stagedStableFrames = 0; return; }
                if (++stagedStableFrames < 10) return;
                if (stagedLeg != null)
                {
                    stagedLeg["after"] = StagedState(); stagedLeg["terminal"] = CaptureStagedCommand(stagedCommand);
                    stagedLeg["costEvents"] = castingCosts.EventsSince(stagedLegCostOffset); stagedLeg["events"] = castingTrace.EventsSince(stagedLegEventOffset);
                    if (StagedStepWantsAnotherLeg())
                    {
                        stagedCommand = null; stagedStableFrames = 0;
                        IssueStagedGroundOrder(StagedLongLegMetres, false); ResetLeafClock(); return;
                    }
                }
                stagedStep["after"] = StagedState();
                stagedStep["costEvents"] = castingCosts.EventsSince(stagedStepCostOffset);
                stagedStep["events"] = castingTrace.EventsSince(stagedStepEventOffset);
                stagedStep["terminal"] = CaptureStagedCommand(stagedCommand);
                stagedStep["hostileAttackTerminal"] = CaptureStagedCommand(stagedHostileAttack);
                stagedLeg = null; stagedStepIndex++; stagedStage = 2; ResetLeafClock(); return;
            }
        }
        private JObject CaptureStagedCommand(UnitCommand command) => command == null ? null : new JObject {
            ["identity"] = castingTrace.Identity(command), ["costIdentity"] = castingCosts.ObjectIdentity(command), ["type"] = command.GetType().Name,
            ["executor"] = command.Executor?.UniqueId, ["createdByPlayer"] = command.CreatedByPlayer, ["started"] = command.IsStarted,
            ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["result"] = command.Result.ToString(),
            ["ignoreCooldown"] = command.IsIgnoreCooldown, ["commandType"] = command.Type.ToString(), ["timeSinceStart"] = command.TimeSinceStart };
        private bool StagedStepTerminal()
        {
            var kind = (string)stagedStep["kind"];
            if (kind == "hostile-attack-mount" || kind == "hostile-attack-swift")
                return stagedHostileAttack != null && stagedHostileAttack.IsFinished && (stagedCommand == null || stagedCommand.IsFinished);
            if (kind == "foreign-window-swift" && !stagedSecondaryInputSent) return false;
            return stagedCommand == null || stagedCommand.IsFinished;
        }
        // Returns false while the step waits for its native precondition (the row keeps its leaf clock).
        private bool IssueStagedStep(string kind, TurnController turn)
        {
            switch (kind)
            {
                case "move-short": IssueStagedGroundOrder(StagedShortLegMetres, false); return true;
                case "move-long": IssueStagedGroundOrder(StagedLongLegMetres, false); return true;
                case "move-extended": IssueStagedGroundOrder(StagedExtendedLegMetres, false); return true;
                case "move-probe": IssueStagedGroundOrder(StagedLongLegMetres, true); return true;
                // Native budget rows: long legs repeat until the mount itself reports the two-move or
                // exhausted state (TB; bounded), or exactly two legs in RT where no budget exists.
                case "move-two-moves": case "move-exhaust": IssueStagedGroundOrder(StagedLongLegMetres, false); return true;
                // Turn-based: a Standard or Swift step runs only on a rider turn that still holds that action
                // (frozen 203 TB: a scroll cast issued after the ranged pair command on the same turn
                // finished without any cast process); other fixture turns are ended until then.
                case "cast-standard-scroll":
                    if (CastingTb && !StagedRiderActionTurn(turn, rider.HasStandardAction())) return false;
                    IssueStagedCast(ExactCastingItemAbility(castingScroll), rider, null); return true;
                case "cast-swift":
                    if (CastingTb && !StagedRiderActionTurn(turn, rider.HasSwiftAction())) return false;
                    IssueStagedSwiftCast(); return true;
                case "attack-ranged":
                    if (CastingTb && !StagedRiderActionTurn(turn, rider.HasStandardAction())) return false;
                    IssueStagedRangedAttack(); return true;
                case "foreign-window-swift":
                    if (CastingTb)
                    {
                        // Out of turn: the rider's own turn ends natively first; the Swift input is issued on
                        // the first foreign native turn (the brain-leased hostile or an idle party member).
                        if (turn == null) return false;
                        if (turn.Unit == rider || turn.Unit == horse) { EndCastingNativeTurn(turn); return false; }
                        stagedStep["foreignTurnActor"] = turn.Unit?.UniqueId;
                        IssueStagedSwiftCast(); stagedSecondaryInputSent = true; return true;
                    }
                    // RT has no turn gate: the Swift input is issued while the rider's own Standard cast runs.
                    IssueStagedCast(ExactCastingItemAbility(castingScroll), rider, "primaryInput"); return true;
                case "hostile-attack-mount":
                case "hostile-attack-swift":
                    // Turn-based: the hostile acts only on its own native turn (frozen 203 TB: an attack run
                    // during the rider's turn never started and was interrupted); the exact fixture turns are
                    // ended until the hostile's turn and the attack is issued there.
                    if (CastingTb)
                    {
                        if (turn == null) return false;
                        if (turn.Unit != target) { EndCastingNativeTurn(turn); return false; }
                        stagedStep["hostileTurnStatus"] = turn.Status.ToString();
                    }
                    if (!target.Commands.Empty) throw new InvalidOperationException("Hostile owns an unrelated native command before the mount attack.");
                    stagedStep["before"] = StagedState();
                    stagedHostileAttack = new UnitAttack(horse) { CreatedByPlayer = true };
                    target.Commands.Run(stagedHostileAttack);
                    stagedStep["hostileAttackInputCount"] = 1; stagedStep["hostileActor"] = target.UniqueId;
                    stagedStep["hostileAttack"] = CaptureStagedCommand(stagedHostileAttack);
                    stagedStep["afterInput"] = StagedState();
                    return true;
                default: throw new InvalidOperationException("Unknown staged step " + kind);
            }
        }
        // Per-frame observation of a live step: TB turn progression for hostile rows and the exact
        // moment of the secondary Swift input.
        private void ObserveStagedStep(TurnController turn)
        {
            var kind = (string)stagedStep["kind"];
            if (kind == "hostile-attack-mount" || kind == "hostile-attack-swift")
            {
                if (CastingTb && stagedHostileAttack != null && !stagedHostileAttack.IsStarted && turn != null &&
                    (turn.Unit == rider || turn.Unit == horse)) EndCastingNativeTurn(turn);
                if (kind == "hostile-attack-swift" && !stagedSecondaryInputSent && stagedHostileAttack != null &&
                    stagedHostileAttack.IsStarted && !stagedHostileAttack.IsFinished)
                {
                    stagedStep["hostileAttackAtInput"] = CaptureStagedCommand(stagedHostileAttack);
                    stagedStep["foreignTurnActor"] = turn?.Unit?.UniqueId;
                    IssueStagedSwiftCast(); stagedSecondaryInputSent = true;
                }
                return;
            }
            if (kind == "foreign-window-swift" && !CastingTb && !stagedSecondaryInputSent)
            {
                var primary = stagedCommand as UnitUseAbility;
                if (primary != null && primary.IsStarted && !primary.IsFinished)
                {
                    stagedStep["primaryShellAtInput"] = castingTrace.Shell(primary);
                    var primaryCommand = stagedCommand;
                    IssueStagedSwiftCast(); stagedSecondaryInputSent = true;
                    // Both commands must finish before the step settles; keep the primary as the terminal owner.
                    if (stagedCommand == null) stagedCommand = primaryCommand;
                }
            }
        }
        // True on the rider's own native turn while it still holds the required action; otherwise the
        // exact fixture turn (rider, mount, idle lease member) is ended and the step waits.
        private bool StagedRiderActionTurn(TurnController turn, bool actionAvailable)
        {
            if (turn == null) return false;
            if (turn.Unit == rider && actionAvailable) return true;
            EndCastingNativeTurn(turn);
            return false;
        }
        private bool StagedStepWantsAnotherLeg()
        {
            var kind = (string)stagedStep["kind"]; var legs = ((JArray)stagedStep["legs"]).Count;
            if (kind != "move-two-moves" && kind != "move-exhaust") return false;
            if (!CastingTb) return legs < 2;
            if (legs >= StagedMaxLegs) return false;
            return kind == "move-two-moves" ? !horse.UsedTwoMoveAction() : horse.CombatState.Cooldown.MoveAction < StagedExhaustedMoveSeconds;
        }
        // One native ground order (one leg); every leg of a step keeps its own record and offsets.
        private void IssueStagedGroundOrder(float distance, bool probe)
        {
            if (stagedStep["legs"] == null) stagedStep["legs"] = new JArray();
            var leg = new JObject { ["index"] = ((JArray)stagedStep["legs"]).Count, ["frame"] = Time.frameCount };
            ((JArray)stagedStep["legs"]).Add(leg); stagedLeg = leg;
            stagedLegCostOffset = castingCosts.EventCount; stagedLegEventOffset = castingTrace.EventCount;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var destination = FindWalkablePoint(horse.Position, distance, .5f);
            leg["probe"] = probe; leg["requestedDistance"] = distance; leg["destination"] = CapturePosition(destination);
            leg["plannedDistance"] = HorizontalDistance(horse.Position, destination);
            leg["before"] = StagedState();
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var clicked = false; var cycles = 0; string inputPath;
            try
            {
                using (var input = new NativeOrdinaryAttackInput(destination))
                {
                    if (turn != null)
                    {
                        input.Predict();
                        while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                    }
                    leg["fiveFootStepMode"] = turn?.EnabledFiveFootStep; leg["singleActionMoveMode"] = turn?.EnabledSingleActionMove;
                    leg["ignoreClick"] = turn?.IgnoreClick();
                    clicked = input.Click(); inputPath = "pointer";
                }
            }
            catch (InvalidOperationException exception)
            {
                // The native pointer priority did not select the ground handler for the mounted pair; use
                // the same ground API the qualified 6C movement row used and record the exact reason.
                leg["pointerRefusal"] = exception.Message;
                ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false); clicked = true; inputPath = "ground-handler";
            }
            leg["inputPath"] = inputPath; leg["cursorCycles"] = cycles; leg["clicked"] = clicked; leg["inputCount"] = 1;
            var riderMove = rider.Commands.Move as UnitMoveTo; var mountMove = horse.Commands.Move as UnitMoveTo;
            stagedCommand = mountMove ?? riderMove;
            leg["carrier"] = CaptureStagedCommand(stagedCommand);
            leg["riderMoveSlot"] = castingTrace.Identity(riderMove); leg["mountMoveSlot"] = castingTrace.Identity(mountMove);
            leg["pairMovementAfterInput"] = combat.HasActiveGroundMovement;
            leg["afterInput"] = StagedState();
        }
        private void IssueStagedCast(AbilityData ability, UnitEntityData castTarget, string recordKey)
        {
            // A keyed record prepared by the caller (the Swift instrument and rod facts) is kept, never replaced.
            var record = recordKey == null ? stagedStep : (stagedStep[recordKey] as JObject ?? new JObject());
            if (recordKey != null) stagedStep[recordKey] = record;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            if (ability == null || ability.Caster.Unit != rider) throw new InvalidOperationException("Native ability caster is not the exact rider.");
            castingAbility = ability; castingPrepared = null;
            record["ability"] = castingTrace.Ability(ability); record["target"] = castTarget?.UniqueId;
            if (record["before"] == null) record["before"] = StagedState();
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            record["turnActor"] = turn?.Unit?.UniqueId; record["ignoreClick"] = turn?.IgnoreClick(); record["riderCanAct"] = rider.CombatState.CanActInCombat;
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(ability);
            record["selected"] = ReferenceEquals(handler.Ability, ability);
            var inputObject = castTarget?.View?.gameObject;
            // A ground instrument (Entangle, 20 ft area) is cast beyond the hostile, away from the pair:
            // frozen 203 RT cast it beside the hostile standing next to the mount and the pair had to save.
            var inputPoint = castTarget != null ? castTarget.Position : FindWalkablePointAwayFromTarget(target.Position, horse.Position, 12f);
            if (castTarget == null)
            {
                record["groundPoint"] = CapturePosition(inputPoint);
                record["groundDistanceToRider"] = HorizontalDistance(inputPoint, rider.Position);
                record["groundDistanceToMount"] = HorizontalDistance(inputPoint, horse.Position);
                record["groundDistanceToHostile"] = HorizontalDistance(inputPoint, target.Position);
            }
            var resolved = handler.GetTarget(inputObject, inputPoint, ability);
            record["resolvedTarget"] = resolved?.Unit?.UniqueId; record["canTarget"] = resolved != null && ability.CanTarget(resolved);
            record["clicked"] = handler.OnClick(inputObject, inputPoint, 0, false, false); record["inputCount"] = 1;
            var shells = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                .Where(c => c.Spell.Blueprint == ability.Blueprint).Distinct().ToArray();
            record["admittedShellCount"] = shells.Length;
            if (shells.Length > 1) throw new InvalidOperationException("One normal input admitted multiple same-spell shells.");
            var shell = shells.SingleOrDefault();
            record["shell"] = shell == null ? null : castingTrace.Shell(shell); record["costShell"] = castingCosts.ObjectIdentity(shell);
            record["queued"] = shell != null && rider.Commands.Queue.Contains(shell);
            record["selectionAfter"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(u => u.UniqueId));
            if (handler.Ability != null) handler.SetAbility(null);
            if (shell != null) { castingShell = shell; stagedCommand = shell; }
            record["afterInput"] = StagedState();
        }
        // The exact Swift-typed instrument: the equipped Lesser Quicken rod on the first still-available
        // memorized level-1 slot (CLW on the rider, Snowball on the hostile, Entangle near the hostile).
        private void IssueStagedSwiftCast()
        {
            var record = stagedStep["swift"] == null ? new JObject() : (JObject)stagedStep["swift"];
            stagedStep["swift"] = record;
            record["rod"] = castingRod.Snapshot();
            foreach (var instrument in new[] { new { Blueprint = Heal, Target = rider }, new { Blueprint = Snowball, Target = target }, new { Blueprint = Entangle, Target = (UnitEntityData)null } })
            {
                var slot = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                    .FirstOrDefault(s => s.Available && s.Spell.Blueprint.AssetGuid == instrument.Blueprint);
                if (slot == null) continue;
                record["instrument"] = instrument.Blueprint;
                IssueStagedCast(slot.Spell, instrument.Target, "swift");
                castingPrepared = slot;
                return;
            }
            throw new InvalidOperationException("No available memorized slot remains for the Swift instrument.");
        }
        // The mounted rider's stock ranged attack through the ordinary native unit click. The product's
        // mounted stock attack policy decides admission; the row records the exact native outcome.
        private void IssueStagedRangedAttack()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            stagedStep["before"] = StagedState();
            stagedStep["weapon"] = new JObject { ["blueprint"] = rider.GetFirstWeapon()?.Blueprint?.AssetGuid, ["ranged"] = rider.GetFirstWeapon()?.Blueprint?.IsRanged };
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Exact diagnostic target refused the expected native attack dispatch.");
            var cycles = 0; var clicked = false;
            using (var input = new NativeOrdinaryAttackInput(target))
            {
                input.Predict();
                if (turn != null) { while (turn.EnabledFullAttack && cycles++ < 8) { input.Click(button: 1); input.Predict(); } }
                stagedStep["fullAttackMode"] = turn?.EnabledFullAttack; stagedStep["ignoreClick"] = turn?.IgnoreClick();
                clicked = input.Click();
            }
            stagedStep["cursorCycles"] = cycles; stagedStep["clicked"] = clicked; stagedStep["inputCount"] = 1;
            var command = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitAttack>().FirstOrDefault(c => c.Target == target);
            stagedCommand = command;
            stagedStep["admitted"] = CaptureStagedCommand(command);
            stagedStep["rejectionCodes"] = new JArray(combat.LastRejectionCodes.Select(c => c.ToString()));
            stagedStep["rejectionFeedback"] = combat.LastFeedback;
            stagedStep["afterInput"] = StagedState();
        }
        private void FinishStagedRow()
        {
            stagedCaseFacts["after"] = StagedState();
            stagedCaseFacts["steps"] = stagedStepRecords;
            stagedCaseFacts["events"] = castingTrace.EventsSince(stagedRowEventOffset);
            stagedCaseFacts["costEvents"] = castingCosts.EventsSince(stagedRowCostOffset);
            stagedCaseFacts["ruleEvents"] = stagedRules.Capture();
            stagedCaseFacts["rulesComplete"] = stagedRules.AllAttacksResolved;
            ((JArray)CastingMeasurement["snapshots"]).Add(new JObject { ["case"] = StagedCase, ["frame"] = Time.frameCount });
            AddRow(StagedCase, castingTrace.Complete && castingCosts.Complete,
                "Observed one ordered sequence of normal native pair inputs and their terminal ownership.", stagedCaseFacts);
            stagedCaseIndex++; castingMountClick = false;
            if (stagedCaseIndex == StagedCases.Length) { BeginCleanup(); return; }
            stagedStage = 1; ResetLeafClock();
        }
        // Bounded raw facts for a staged leaf deadline. Observation only; nothing is mutated.
        private JToken CaptureChunk6StagedDeadlineProgress()
        {
            var progress = new JObject { ["family"] = IsChunk6dStaged ? "6D" : "6E", ["stage"] = stagedStage, ["caseIndex"] = stagedCaseIndex,
                ["case"] = stagedCaseIndex < StagedCases.Length ? StagedCase : null, ["stepIndex"] = stagedStepIndex };
            try
            {
                progress["step"] = stagedStep?.DeepClone(); progress["caseFacts"] = stagedCaseFacts?.DeepClone();
                if (castingTrace != null && castingCosts != null) progress["state"] = StagedState();
                progress["command"] = CaptureStagedCommand(stagedCommand); progress["hostileAttack"] = CaptureStagedCommand(stagedHostileAttack);
                progress["mountCommands"] = CaptureCastingCommandSurface(horse); progress["riderCommands"] = CaptureCastingCommandSurface(rider);
                progress["hostileCommands"] = target == null ? null : CaptureCastingCommandSurface(target);
            }
            catch (Exception exception) { progress["captureError"] = exception.ToString(); }
            return progress;
        }
        // Staged-family cleanup: the shared 6C drain plus the ranged lease and the rule observer.
        private bool DrainStagedFixture()
        {
            if (!DrainCastingFixture()) return false;
            // The shared tranche cleanup may already have released the ranged lease (frozen 203 RT: the key
            // was never written); the release fact is recorded either way with its owner.
            if (rangedWeaponLease != null) { rangedWeaponLease.Dispose(); rangedWeaponLease = null; CastingMeasurement["rangedWeaponReleasedBy"] = "staged-drain"; }
            else if (CastingMeasurement["rangedWeaponReleasedBy"] == null) CastingMeasurement["rangedWeaponReleasedBy"] = "shared-cleanup";
            CastingMeasurement["rangedWeaponReleased"] = rangedWeaponLease == null;
            if (stagedRules != null) { CastingMeasurement["ruleTrace"] = stagedRules.Capture(); stagedRules.Dispose(); stagedRules = null; }
            return true;
        }
    }
}
