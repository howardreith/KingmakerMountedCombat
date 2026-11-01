using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 3 (TRAP-01, SPELL-01) native rows on the shared disposable casting fixture (Mammoth pair). Every input is
    // the ordinary one: the map-object click handler for the trap, the selected-ability click handler for spells and
    // the native Stop. The product's carried approach supplies the mount's movement; the native command keeps every
    // check, cast, charge, roll, spend and effect, and the trace observes them. The reader owns PASS/FAIL.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsSprint3ApproachScenario(string scenario) =>
            scenario == "sprint3-approach-rt" || scenario == "sprint3-approach-rt-neighbor" || scenario == "sprint3-approach-rt-trap" ||
            scenario == "sprint3-approach-tb";
        private bool IsSprint3Approach => IsSprint3ApproachScenario(request.Scenario);
        internal const int Sprint3SchemaVersion = 47;
        internal static readonly string[] Sprint3RealTimeCases = { "S3-trap-disarm", "S3-touch-friendly-distant",
            "S3-touch-held-charge", "S3-touch-hostile", "S3-ranged-distant" };
        // The insufficient-movement row runs first: its recipient is placed beyond one turn's pair movement before the
        // encounter (a recipient's own in-combat move reaches only its budget, r10: 15.7 m against a 24.4 m allowance).
        internal static readonly string[] Sprint3TurnBasedCases = { "S3-tb-insufficient-movement", "S3-tb-touch-friendly" };
        // PR #8 review: the named ownership neighbor runs one existing row (Stop, second ordinary click, the carrier's own
        // delegated move through the new Run admission and cleanup); it is a selected subset, never the full real-time set.
        internal static readonly string[] Sprint3NeighborCases = { "S3-touch-held-charge" };
        // PR #8 second review: the trap row alone (with its declared door preparation), so the passed spell rows are not rerun.
        internal static readonly string[] Sprint3TrapCases = { "S3-trap-disarm" };
        private string[] Sprint3Cases => CastingTb ? Sprint3TurnBasedCases : request.Scenario == "sprint3-approach-rt-neighbor" ? Sprint3NeighborCases :
            request.Scenario == "sprint3-approach-rt-trap" ? Sprint3TrapCases : Sprint3RealTimeCases;
        private string Sprint3Case => Sprint3Cases[s3CaseIndex];
        private const float Sprint3FriendlyDistantMetres = 12f, Sprint3FriendlyHeldMetres = 6f, Sprint3HostileSpawnMetres = 8f,
            Sprint3RangedRetreatMetres = 10f, Sprint3TurnBasedFriendlyMetres = 9f, Sprint3PathLengthBoundMetres = 160f;
        private int s3CaseIndex, s3Stage, s3StableFrames, s3EventOffset, s3CostOffset, s3Step;
        private JObject s3Facts;
        private UnitEntityData s3Recipient;
        private TrapObjectView s3Trap;
        private UnitCommand s3Command;
        private Vector3 s3MountStart, s3RiderStart;
        private bool s3Stopped, s3SecondInputSent, s3CarriedSeen, s3ReleasedSeen, s3TrickeryGranted, s3PerceptionGranted;
        private int s3TrickeryBefore, s3PerceptionBefore;
        private float s3MountTravelled;
        private Vector3 s3LastMountSample;
        private Sprint3TrapObserver s3TrapObserver;
        private Chunk4IncomingRuleObserver s3Rules;
        private TurnController s3RowTurn, s3SeenTurn, s3RecipientTurn;
        private int s3TurnsSeen;

        private void BeginSprint3Approach()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 3 rows require the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            s3TrapObserver = new Sprint3TrapObserver(rider);
            observations["sprint3Approach"] = new JObject {
                ["contract"] = "native-mounted-carried-approach-v1", ["mode"] = CastingTb ? "TB" : "RT", ["mounted"] = true,
                ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid,
                ["cases"] = new JArray(Sprint3Cases), ["initialPrepared"] = CastingPreparedInventory(), ["snapshots"] = new JArray(),
                ["riderLevel"] = rider.Descriptor.Progression.CharacterLevel, ["riderSpeedMps"] = rider.CurrentSpeedMps,
                ["mountCombatSpeedMps"] = horse.CombatSpeedMps, ["riderThieveryBaseBefore"] = rider.Stats.SkillThievery.BaseValue, ["riderPerceptionBaseBefore"] = rider.Stats.SkillPerception.BaseValue,
                ["policy"] = "Ordinary native inputs; the product's carried approach moves the mount; stock Kingmaker owns every check, cast, charge, roll, spend and effect." };
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            // Only the CLW scroll stack: the 6C quicken rod would spend its own charge on every memorized cast (r02).
            castingScroll = AcquireCastingItem(NativeCastingItemLease.CureScroll, ScrollStackCount);
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence));
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject Sprint3State()
        {
            var state = CastingState();
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var touch = rider.Get<UnitPartTouch>();
            state["heldTouch"] = touch == null ? null : new JObject { ["blueprint"] = touch.Ability?.Blueprint?.AssetGuid, ["castedThisRound"] = touch.IsCastedInThisRound };
            state["carriedLive"] = combat.HasLiveCarriedApproach; state["carriedBinds"] = combat.CarriedApproachBindCount;
            state["carriedReleases"] = combat.CarriedApproachReleaseCount; state["carriedObservation"] = combat.LastCarriedApproachObservation;
            state["mountPosition"] = CapturePosition(horse.Position); state["riderPosition"] = CapturePosition(rider.Position);
            state["mountMoving"] = horse.View?.AgentASP?.IsReallyMoving == true;
            state["riderHasStandard"] = rider.HasStandardAction(); state["riderHasMove"] = rider.HasMoveAction(); state["mountHasMove"] = horse.HasMoveAction();
            state["turnActor"] = turn?.Unit?.UniqueId; state["turnStatus"] = turn?.Status.ToString(); state["turnDeliverTouch"] = turn?.EnableDeliverTouch;
            state["recipient"] = s3Recipient == null ? null : new JObject { ["id"] = s3Recipient.UniqueId, ["damage"] = s3Recipient.Damage,
                ["hp"] = s3Recipient.HPLeft, ["position"] = CapturePosition(s3Recipient.Position), ["distanceToMount"] = HorizontalDistance(s3Recipient.Position, horse.Position),
                ["inCombat"] = s3Recipient.IsInCombat, ["commandsEmpty"] = s3Recipient.Commands.Empty };
            state["hostile"] = target == null ? null : new JObject { ["id"] = target.UniqueId, ["damage"] = target.Damage, ["hp"] = target.HPLeft,
                ["position"] = CapturePosition(target.Position), ["distanceToMount"] = HorizontalDistance(target.Position, horse.Position) };
            state["trap"] = s3Trap == null ? null : new JObject { ["id"] = s3Trap.UniqueId, ["active"] = s3Trap.TrapActive,
                ["distanceToMount"] = HorizontalDistance(s3Trap.transform.position, horse.Position) };
            state["rejectionCodes"] = new JArray(combat.LastRejectionCodes.Select(c => c.ToString()));
            return state;
        }

        private static JObject Sprint3CarriedOutcome(MountedCarriedApproachOutcome o) => o == null ? null : new JObject {
            ["kind"] = o.Kind, ["commandType"] = o.CommandType, ["rider"] = o.RiderId, ["mount"] = o.MountId, ["target"] = o.TargetId,
            ["approachRadius"] = o.ApproachRadius, ["moveStarts"] = o.DelegatedMoveStartCount, ["moveTicks"] = o.DelegatedMoveTickCount,
            ["replans"] = o.ReplanCount, ["withoutProgress"] = o.ApproachesWithoutProgress, ["satisfied"] = o.Satisfied,
            ["nativeApproachEndSignalled"] = o.NativeApproachEndSignalled, ["trapSafePointApplied"] = o.TrapSafePointApplied,
            ["distanceAtSatisfaction"] = o.DistanceAtSatisfaction, ["lineOfSightAtSatisfaction"] = o.LineOfSightAtSatisfaction,
            ["operationalSeconds"] = o.ApproachElapsedOperationalSeconds, ["slotRestored"] = o.MountMoveSlotRestored, ["successorRetained"] = o.SuccessorRetained, ["yieldedToMountOwner"] = o.YieldedToMountOwner,
            ["terminalReason"] = o.TerminalReason, ["result"] = o.Result };

        private JObject Sprint3Command(UnitCommand command) => command == null ? null : new JObject {
            ["identity"] = castingTrace.Identity(command), ["type"] = command.GetType().Name, ["executor"] = command.Executor?.UniqueId,
            ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["result"] = command.Result.ToString(),
            ["commandType"] = command.Type.ToString(), ["approachRadius"] = command.ApproachRadius, ["needLoS"] = command.NeedLoS,
            ["ignoreCooldown"] = command.IsIgnoreCooldown, ["target"] = command.TargetUnit?.UniqueId,
            ["spell"] = command is UnitUseAbility ability ? castingTrace.Ability(ability.Spell) : null,
            ["interaction"] = command is UnitInteractWithObject interaction ? interaction.Interaction?.GetType().Name : null,
            ["overrideTarget"] = command is UnitInteractWithObject withOverride && withOverride.OverrideTarget.HasValue ? CapturePosition(withOverride.OverrideTarget.Value) : null };

        private void ObserveSprint3Carried()
        {
            if (combat.HasLiveCarriedApproach) s3CarriedSeen = true;
            else if (s3CarriedSeen) s3ReleasedSeen = true;
            var moving = horse.View?.AgentASP?.IsReallyMoving == true;
            var travelled = HorizontalDistance(horse.Position, s3LastMountSample);
            if (travelled > 0.01f) { s3MountTravelled += travelled; s3LastMountSample = horse.Position; }
            // A long native approach is progress, not a stall: the leaf clock restarts while the mount really moves.
            if (moving && travelled > 0.01f) ResetLeafClock();
            var outcome = combat.LastCarriedApproachOutcome;
            if (outcome != null && s3Facts != null)
            {
                var list = (JArray)(s3Facts["carriedOutcomes"] ?? (s3Facts["carriedOutcomes"] = new JArray()));
                if (list.Count == 0 || (int)list[list.Count - 1]["serial"] != RuntimeHelpersSerial(outcome))
                {
                    var record = Sprint3CarriedOutcome(outcome); record["serial"] = RuntimeHelpersSerial(outcome); record["frame"] = Time.frameCount;
                    list.Add(record);
                }
            }
        }
        private static int RuntimeHelpersSerial(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);

        private bool Sprint3Settled() =>
            rider.Commands.Empty && horse.Commands.Empty && castingTrace.ProcessesSettled && !combat.HasLiveCarriedApproach &&
            !NativeSaveEffectBoundary.HasUnresolvedAbilities() && !NativeSaveEffectBoundary.HasUnresolvedProjectiles() &&
            !rider.AreHandsBusyWithAnimation && horse.View?.AgentASP?.IsReallyMoving != true && !combat.HasActiveGroundMovement;

        private void BeginSprint3Row()
        {
            s3Facts = new JObject { ["case"] = Sprint3Case, ["before"] = Sprint3State(), ["inputCount"] = 0 };
            s3EventOffset = castingTrace.EventCount; s3CostOffset = castingCosts.EventCount;
            castingTrace.BeginCase(Sprint3Case); s3Rules.BeginCase(Sprint3Case); s3TrapObserver.BeginCase(Sprint3Case);
            s3Command = null; s3Stopped = false; s3SecondInputSent = false; s3CarriedSeen = false; s3ReleasedSeen = false;
            s3MountTravelled = 0f; s3LastMountSample = horse.Position; s3MountStart = horse.Position; s3RiderStart = rider.Position;
            s3StableFrames = 0; s3Step = 0; castingShell = null; castingAbility = null; castingPrepared = null;
            ResetLeafClock();
        }

        private void FinishSprint3Row(bool passed, string detail)
        {
            s3Facts["after"] = Sprint3State();
            s3Facts["events"] = castingTrace.EventsSince(s3EventOffset); s3Facts["costEvents"] = castingCosts.EventsSince(s3CostOffset);
            s3Facts["rules"] = Sprint3RowRules(); s3Facts["trapEvents"] = s3TrapObserver.Capture();
            s3Facts["command"] = Sprint3Command(s3Command);
            s3Facts["carriedSeen"] = s3CarriedSeen; s3Facts["carriedReleasedSeen"] = s3ReleasedSeen;
            s3Facts["mountTravelledMetres"] = s3MountTravelled;
            s3Facts["mountDisplacement"] = HorizontalDistance(s3MountStart, horse.Position); s3Facts["riderDisplacement"] = HorizontalDistance(s3RiderStart, rider.Position);
            AddRow(Sprint3Case, passed, detail, s3Facts);
            s3CaseIndex++; s3Stage = 1; s3RowTurn = null;
            if (s3CaseIndex == Sprint3Cases.Length) { BeginCleanup(); return; }
            ResetLeafClock();
        }

        private void FailSprint3Row(string why) { s3Facts["failure"] = why; FinishSprint3Row(false, why); }

        // The rule observer spans the scenario; a row reports only its own case's rules (r02 counted earlier rows).
        private JObject Sprint3RowRules()
        {
            var capture = s3Rules.Capture();
            return new JObject { ["dropped"] = capture["dropped"],
                ["events"] = new JArray(((JArray)capture["events"]).Where(e => (string)e["caseId"] == Sprint3Case).Select(e => e.DeepClone())) };
        }
        private JToken[] Sprint3RiderAttackRolls(UnitEntityData onTarget) =>
            ((JArray)Sprint3RowRules()["events"]).Where(e => (string)e["kind"] == "attack-roll" && (string)e["actor"] == rider.UniqueId &&
                (string)e["target"] == onTarget.UniqueId).ToArray();

        private void TickSprint3Approach()
        {
            var game = Game.Instance; var controller = game.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (CastingTb && !ReferenceEquals(turn, s3SeenTurn)) { s3SeenTurn = turn; if (++s3TurnsSeen <= CastingTurnResetLimit * 4) ResetLeafClock(); }
            CastingMeasurement["progress"] = new JObject { ["stage"] = s3Stage, ["caseIndex"] = s3CaseIndex, ["step"] = s3Step,
                ["case"] = s3CaseIndex < Sprint3Cases.Length ? Sprint3Case : null, ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse) };
            if (s3Stage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation() && PrepareCombatMountRiderAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "sprint3-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                s3Recipient = allocationFixtureParty.FirstOrDefault(member => member.IsInState && member.Descriptor.State.IsConscious && !member.Descriptor.IsPet)
                    ?? allocationFixtureParty.FirstOrDefault(member => member.IsInState && member.Descriptor.State.IsConscious);
                CastingMeasurement["recipient"] = s3Recipient == null ? null : new JObject { ["id"] = s3Recipient.UniqueId, ["blueprint"] = s3Recipient.Blueprint.AssetGuid,
                    ["pet"] = s3Recipient.Descriptor.IsPet, ["hp"] = s3Recipient.HPLeft, ["maxHp"] = s3Recipient.MaxHP };
                s3Stage = 1; ResetLeafClock(); return;
            }
            if (s3Stage == 1)
            {
                // Exploration rows run before the encounter; the combat rows (and every turn-based row) after it.
                var caseNeedsCombat = CastingTb || Sprint3Case == "S3-touch-hostile" || Sprint3Case == "S3-ranged-distant";
                if (caseNeedsCombat && target == null)
                {
                    if (!Sprint3Settled()) return;
                    if (CastingTb && s3Recipient != null && !PlaceSprint3Recipient(horse.CombatSpeedMps * 6f + 8f)) return;
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(CastingTb);
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                    if (CastingTb && pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                    BeginTarget(Sprint3HostileSpawnMetres, "sprint3-native-encounter"); ruleProbe.Arm(target, false);
                    castingCosts.BeginEncounter(request.RunId + ":sprint3"); ResetLeafClock(); return;
                }
                if (caseNeedsCombat)
                {
                    if (!rider.IsInCombat || !horse.IsInCombat) return;
                    if (WaitForNativeCastingPrincipal(CastingTb, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                    if (CastingTb && (ReferenceEquals(turn, s3RowTurn) || !rider.HasStandardAction() || horse.CombatState.Cooldown.MoveAction > .001f))
                    { EndCastingNativeTurn(turn); return; }
                    if (!CastingTb && (rider.CombatState.Cooldown.StandardAction > .001f || horse.CombatState.Cooldown.MoveAction > .001f)) return;
                }
                if (!Sprint3Settled() || game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                if (relationship.State != RelationshipState.Mounted) throw new InvalidOperationException("Sprint 3 rows require the exact mounted pair at every row start.");
                s3RowTurn = turn; BeginSprint3Row(); s3Stage = 2; return;
            }
            if (s3Stage == 2)
            {
                ObserveSprint3Carried();
                switch (Sprint3Case)
                {
                    case "S3-trap-disarm": TickSprint3Trap(); return;
                    case "S3-touch-friendly-distant": TickSprint3Friendly(Sprint3FriendlyDistantMetres, false); return;
                    case "S3-touch-held-charge": TickSprint3Friendly(Sprint3FriendlyHeldMetres, true); return;
                    case "S3-touch-hostile": TickSprint3Hostile(); return;
                    case "S3-ranged-distant": TickSprint3Ranged(); return;
                    case "S3-tb-touch-friendly": TickSprint3TurnBasedFriendly(turn, false); return;
                    case "S3-tb-insufficient-movement": TickSprint3TurnBasedFriendly(turn, true); return;
                    default: throw new InvalidOperationException("Unknown Sprint 3 case " + Sprint3Case);
                }
            }
        }

        // ---- TRAP-01 -------------------------------------------------------------------------------------------
        private JObject Sprint3TrapFacts(TrapObjectView t) => new JObject { ["id"] = t.UniqueId, ["name"] = t.name,
            ["position"] = CapturePosition(t.transform.position), ["distance"] = HorizontalDistance(t.transform.position, horse.Position),
            ["blueprint"] = t.Blueprint?.AssetGuid, ["disableDc"] = t.Data?.DisableDC, ["scriptZone"] = t.ScriptZoneTrigger != null,
            ["active"] = t.TrapActive, ["perceptionPassed"] = t.Data?.IsPerceptionCheckPassed, ["revealed"] = t.Data?.IsRevealed,
            ["inGame"] = t.Data?.IsInGame, ["interactable"] = t.Interactions.Any(i => i is DisableTrapInteractionComponent && i.CanInteract()),
            ["perceptionRadius"] = t.PerceptionCheckComponent == null ? (float?)null : t.PerceptionCheckComponent.Radius,
            ["perceptionDc"] = t.PerceptionCheckComponent == null ? (int?)null : t.PerceptionCheckComponent.DC,
            ["reachable"] = Sprint3PathPossible(horse.Position, t.transform.position), ["inFogOfWar"] = t.Data?.IsInFogOfWar };

        private TrapObjectView[] s3TrapCandidates;
        private int s3TrapCandidateIndex, s3TrapDiscoveryFrames;

        // The native PartyPerceptionController's own gates for one trap (observation only): fog, line of sight, the one
        // roll per character per rank, the recorded ranks and the party's Perception values.
        private JObject Sprint3TrapGates(TrapObjectView trap)
        {
            var data = trap.Data; var units = new[] { rider, horse, s3Recipient }.Where(u => u != null).ToArray();
            return new JObject {
                ["inFogOfWar"] = data.IsInFogOfWar, ["perceptionPassed"] = data.IsPerceptionCheckPassed, ["revealed"] = data.IsRevealed,
                ["lastPerceptionRollRankEntries"] = data.LastPerceptionRollRank?.Count ?? 0,
                ["units"] = new JArray(units.Select(u => new JObject { ["id"] = u.UniqueId, ["rollAllowed"] = data.IsPerceptionRollAllowed(u),
                    ["hasLos"] = u.HasLOS(trap.transform), ["distance"] = HorizontalDistance(u.Position, trap.transform.position),
                    ["perceptionBase"] = u.Stats.SkillPerception.BaseValue, ["perceptionModified"] = u.Stats.SkillPerception.ModifiedValue,
                    ["controllable"] = Game.Instance.Player.ControllableCharacters.Contains(u), ["motionThisTick"] = u.HasMotionThisTick })) };
        }

        // Per-frame sampling during a leg: how many ticks each party member spent moving inside the perception radius with
        // the trap out of fog (the native roll condition), so a missing roll is attributable.
        private void ObserveSprint3TrapMotion(JObject leg)
        {
            if (s3Trap == null) return;
            var radius = s3Trap.PerceptionCheckComponent == null ? 6f : s3Trap.PerceptionCheckComponent.Radius;
            var sampling = (JObject)(leg["motionSampling"] ?? (leg["motionSampling"] = new JObject { ["frames"] = 0, ["riderMovingInRadius"] = 0, ["mountMovingInRadius"] = 0, ["fogFrames"] = 0, ["riderLosFrames"] = 0 }));
            sampling["frames"] = (int)sampling["frames"] + 1;
            var fog = s3Trap.Data.IsInFogOfWar; if (fog) sampling["fogFrames"] = (int)sampling["fogFrames"] + 1;
            if (!fog && rider.HasMotionThisTick && HorizontalDistance(rider.Position, s3Trap.transform.position) < radius)
            {
                sampling["riderMovingInRadius"] = (int)sampling["riderMovingInRadius"] + 1;
                if (rider.HasLOS(s3Trap.transform)) sampling["riderLosFrames"] = (int)sampling["riderLosFrames"] + 1;
            }
            if (!fog && horse.HasMotionThisTick && HorizontalDistance(horse.Position, s3Trap.transform.position) < radius)
                sampling["mountMovingInRadius"] = (int)sampling["mountMovingInRadius"] + 1;
        }
        private const int Sprint3TrapCandidateLimit = 2, Sprint3TrapDiscoveryWaitFrames = 90, Sprint3TrapLegReorderLimit = 3;
        // The native radius test is a 3D distance (PartyPerceptionController: DistanceTo(transform.position)); the carried
        // rider sits about 1.6 m above the mount's ground, so the leg ends three metres inside the radius (r08: a leg that
        // settled 7.9 m from an 8 m trap never put the rider inside it).
        private const float Sprint3TrapLegInsideRadiusMetres = 3f, Sprint3TrapLegShortMetres = 2f;

        // Fixture setup on the disposable rider only, before a leg: the native controller allows one Perception roll per
        // character per rank (StaticEntityData.LastPerceptionRollRank persists in the save). When the fixture's saved
        // rolls already consumed the pair's current ranks against this trap, one Perception rank on the rider re-opens
        // exactly one native roll; the roll, DC and result stay native and the rank is restored at cleanup.
        private void EnsureSprint3PerceptionRollAllowed(TrapObjectView trap, JObject leg)
        {
            var data = trap.Data;
            var allowed = new[] { rider, horse }.Any(u => Game.Instance.Player.ControllableCharacters.Contains(u) && data.IsPerceptionRollAllowed(u));
            var grant = new JObject { ["allowedBefore"] = allowed, ["riderBaseBefore"] = rider.Stats.SkillPerception.BaseValue, ["granted"] = false };
            if (!allowed)
            {
                if (!s3PerceptionGranted) { s3PerceptionBefore = rider.Stats.SkillPerception.BaseValue; s3PerceptionGranted = true; }
                rider.Stats.SkillPerception.BaseValue = rider.Stats.SkillPerception.BaseValue + 1;
                grant["granted"] = true;
            }
            grant["riderBaseAfter"] = rider.Stats.SkillPerception.BaseValue; grant["riderModifiedAfter"] = rider.Stats.SkillPerception.ModifiedValue;
            grant["allowedAfter"] = data.IsPerceptionRollAllowed(rider);
            leg["perceptionRank"] = grant;
        }

        // Begins the pair's ordinary exploration leg toward the current candidate: one ground order to a route point that
        // is inside the trap's native perception radius, outside its trigger zone and at least four metres short of it. The
        // native PartyPerceptionController rolls each moving character's Perception once per trap (one roll per rank);
        // the row never sets the discovery flag itself. Returns false with the row failed when no bounded route exists.
        // A walkable navmesh point about the requested distance from the trap, outside its trigger zone: the pair's own
        // walk to it crosses the perception radius while moving, which is when the native controller rolls. Null when
        // no bearing around the trap offers one (r07: the trap's nearest navmesh node lay 12 m off its transform).
        private Vector3? FindSprint3TrapApproachPoint(TrapObjectView trap, float distance)
        {
            if (global::AstarPath.active == null) return null;
            var zone = trap.ScriptZoneTrigger; var origin = trap.transform.position; Vector3? best = null; var bestError = float.PositiveInfinity;
            for (var index = 0; index < 32; index++)
            {
                var direction = Quaternion.Euler(0f, index * 11.25f, 0f) * Vector3.forward;
                var nearest = global::AstarPath.active.GetNearest(origin + direction * distance);
                if (nearest.node == null || !nearest.node.Walkable) continue;
                var point = nearest.clampedPosition; var actual = HorizontalDistance(origin, point);
                if (actual < 3.5f || (zone != null && zone.ContainsPosition(point))) continue;
                var error = Math.Abs(actual - distance);
                if (error < bestError) { bestError = error; best = point; }
            }
            return best;
        }

        private bool BeginSprint3TrapLeg()
        {
            s3Trap = s3TrapCandidates[s3TrapCandidateIndex];
            var radius = s3Trap.PerceptionCheckComponent == null ? 6f : s3Trap.PerceptionCheckComponent.Radius;
            var legs = (JArray)(s3Facts["explorationLegs"] ?? (s3Facts["explorationLegs"] = new JArray()));
            var leg = new JObject { ["candidate"] = Sprint3TrapFacts(s3Trap), ["perceptionRadius"] = radius };
            legs.Add(leg);
            var approach = FindSprint3TrapApproachPoint(s3Trap, Math.Max(4f, radius - Sprint3TrapLegInsideRadiusMetres));
            if (!approach.HasValue) { leg["skipped"] = "no walkable point inside the perception radius outside the trigger zone"; return false; }
            var legPoint = approach.Value;
            var path = Pathfinding.ABPath.Construct(horse.Position, legPoint);
            global::AstarPath.StartPath(path); global::AstarPath.WaitForPath(path);
            var length = path.error ? float.PositiveInfinity : path.GetTotalLength();
            leg["routeError"] = path.error; leg["routeLength"] = length;
            if (path.error || length > Sprint3PathLengthBoundMetres) { leg["skipped"] = "no bounded native route"; return false; }
            leg["nativeGatesAtStart"] = Sprint3TrapGates(s3Trap);
            EnsureSprint3PerceptionRollAllowed(s3Trap, leg);
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(legPoint, false);
            leg["destination"] = CapturePosition(legPoint); leg["destinationToTrap"] = HorizontalDistance(legPoint, s3Trap.transform.position); leg["ordered"] = true;
            leg["perceptionBefore"] = s3Trap.Data.IsPerceptionCheckPassed; leg["revealedBefore"] = s3Trap.Data.IsRevealed;
            leg["reorders"] = 0; leg["settles"] = new JArray();
            s3TrapLegPoint = legPoint; s3TrapDiscoveryFrames = 0;
            return true;
        }
        private Vector3 s3TrapLegPoint;
        private StandardDoor s3TrapDoor;
        private MapObjectView s3TrapSecret;
        private UnitCommand s3TrapSecretCommand;
        private int s3TrapSecretRuleOffset;

        // The nearest map object whose interaction is the native skill-check (a secret passage), usable by one ordinary
        // click right now; its facts are recorded whether or not it is usable (attribution).
        private MapObjectView FindSprint3SecretPassage(float radius, JObject leg)
        {
            var candidates = (UnityEngine.Object.FindObjectsOfType<MapObjectView>() ?? new MapObjectView[0])
                .Where(m => m != null && !(m is TrapObjectView) && m.Interactions.Any(i => i is InteractionSkillCheck) && HorizontalDistance(m.transform.position, horse.Position) <= radius)
                .OrderBy(m => HorizontalDistance(m.transform.position, horse.Position)).ToArray();
            leg["secretPassages"] = new JArray(candidates.Select(m => { var check = m.Interactions.OfType<InteractionSkillCheck>().First(); return new JObject {
                ["name"] = m.name, ["distance"] = HorizontalDistance(m.transform.position, horse.Position), ["skill"] = check.Skill.ToString(), ["dc"] = check.DC,
                ["canInteract"] = check.CanInteract(), ["availableInteractions"] = ClickMapObjectHandler.HasAvailableInteractions(m.gameObject),
                ["inFogOfWar"] = m.Data?.IsInFogOfWar, ["revealed"] = m.Data?.IsRevealed, ["perceptionPassed"] = m.Data?.IsPerceptionCheckPassed,
                ["perceptionRadius"] = m.PerceptionCheckComponent == null ? (float?)null : m.PerceptionCheckComponent.Radius,
                ["perceptionDc"] = m.PerceptionCheckComponent == null ? (int?)null : m.PerceptionCheckComponent.DC,
                ["riderRollAllowed"] = m.Data?.IsPerceptionRollAllowed(rider), ["mountRollAllowed"] = m.Data?.IsPerceptionRollAllowed(horse) }; }));
            return candidates.FirstOrDefault(m => m.Interactions.OfType<InteractionSkillCheck>().First().CanInteract() && ClickMapObjectHandler.HasAvailableInteractions(m.gameObject));
        }
        private const int Sprint3TrapDoorLimit = 2;
        private const float Sprint3TrapDoorScanMetres = 12f;

        // The native graph's own connectivity test between the mount's node and a point (A* area ids): a closed door's
        // navmesh cut or a separate island makes a partial path whose walk ends short (r12: both legs settled at the
        // same spot, the trap still in fog, every re-order a no-op).
        private bool? Sprint3PathPossible(Vector3 from, Vector3 to)
        {
            if (global::AstarPath.active == null) return null;
            var a = global::AstarPath.active.GetNearest(from).node; var b = global::AstarPath.active.GetNearest(to).node;
            if (a == null || b == null) return null;
            return Pathfinding.PathUtilities.IsPathPossible(a, b);
        }

        // The nearest closed door one ordinary click could open (the Sprint 2 door predicate; nothing is mutated here).
        private StandardDoor FindSprint3ClosedDoor(float radius)
        {
            var mounts = new[] { horse };
            return (UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0])
                .Where(d => d != null && !d.IsOpen && HorizontalDistance(d.transform.position, horse.Position) <= radius)
                .Where(d => { var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(d, mounts); return facts.Active && facts.DataPresent && facts.ClosedClickable; })
                .OrderBy(d => HorizontalDistance(d.transform.position, horse.Position)).FirstOrDefault();
        }

        // A leg that settled short of its point (r08: 2.5 m and 20 m short) is re-ordered through the same ordinary ground
        // click, a bounded number of times; when the point is cut off from the mount's node, the nearest closed clickable
        // door is clicked once (the ordinary map-object click the Sprint 2 mounted door route serves) and the leg is
        // re-ordered after the door opened. The pair's command surfaces at each settle are recorded for attribution.
        private bool ReorderSprint3TrapLeg(JObject leg)
        {
            var remaining = HorizontalDistance(horse.Position, s3TrapLegPoint);
            var reachable = Sprint3PathPossible(horse.Position, s3TrapLegPoint);
            var settles = (JArray)leg["settles"];
            settles.Add(new JObject { ["frame"] = Time.frameCount, ["distanceToPoint"] = remaining, ["distanceToTrap"] = HorizontalDistance(s3Trap.transform.position, horse.Position),
                ["pointReachable"] = reachable, ["trapInFog"] = s3Trap.Data.IsInFogOfWar, ["doorPending"] = s3TrapDoor != null, ["doorOpen"] = s3TrapDoor?.IsOpen,
                ["riderPosition"] = CapturePosition(rider.Position), ["mountPosition"] = CapturePosition(horse.Position),
                ["mountCommands"] = CaptureCastingCommandSurface(horse), ["riderCommands"] = CaptureCastingCommandSurface(rider),
                ["rejectionCodes"] = Sprint3State()["rejectionCodes"]?.DeepClone(), ["doorOutcome"] = Sprint3DoorOutcome() });
            var doors = (JArray)(leg["doors"] ?? (leg["doors"] = new JArray()));
            if (s3TrapDoor != null)
            {
                var opened = s3TrapDoor.IsOpen;
                var record = (JObject)doors[doors.Count - 1];
                record["openAfter"] = opened; record["distanceAfter"] = HorizontalDistance(s3TrapDoor.transform.position, horse.Position);
                record["reachableAfter"] = Sprint3PathPossible(horse.Position, s3TrapLegPoint);
                s3TrapDoor = null;
                if (!opened) return false;
            }
            else if (s3TrapSecret != null)
            {
                // The native skill-check interaction ran (or was refused) on the secret passage; the graph tells whether it opened.
                var record = (JObject)doors[doors.Count - 1];
                var after = Sprint3PathPossible(horse.Position, s3TrapLegPoint);
                record["reachableAfter"] = after; record["openAfter"] = after == true; record["distanceAfter"] = HorizontalDistance(s3TrapSecret.transform.position, horse.Position);
                record["canInteractAfter"] = s3TrapSecret.Interactions.OfType<InteractionSkillCheck>().FirstOrDefault()?.CanInteract();
                record["command"] = Sprint3Command(s3TrapSecretCommand); record["checkEvents"] = s3TrapObserver.EventsSince(s3TrapSecretRuleOffset);
                s3TrapSecret = null; s3TrapSecretCommand = null;
                if (after != true) return false;
            }
            else if (remaining <= Sprint3TrapLegShortMetres) return false;
            else if (reachable == false)
            {
                // PR #8 second review: the trap selection and any declared door preparation never chain into further
                // doors or secret passages.
                if (s3DoorPrepared || request.Scenario == "sprint3-approach-rt-trap") return false;
                if (doors.Count >= Sprint3TrapDoorLimit) return false;
                var door = FindSprint3ClosedDoor(Sprint3TrapDoorScanMetres);
                if (door == null)
                {
                    // r15: the cut-off room's only way in was a secret passage 1.5 m from the settle (a MapObjectView whose
                    // interaction is the native InteractionSkillCheck); one ordinary click lets the engine roll its own check.
                    var secret = FindSprint3SecretPassage(6f, leg);
                    if (secret == null)
                    {
                        leg["noDoor"] = "the point is cut off from the mount's node and no closed clickable door or usable secret passage stands within " + Sprint3TrapDoorScanMetres + " m";
                        leg["obstacleInventory"] = Sprint3ObstacleInventory(s3TrapLegPoint);
                        return false;
                    }
                    var check = secret.Interactions.OfType<InteractionSkillCheck>().First();
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    s3TrapSecretRuleOffset = s3TrapObserver.EventCount;
                    var secretClicked = new ClickMapObjectHandler().OnClick(secret.gameObject, secret.transform.position, 0, false, false);
                    s3TrapSecretCommand = rider.Commands.Standard;
                    doors.Add(new JObject { ["kind"] = "secret-passage", ["name"] = secret.name, ["position"] = CapturePosition(secret.transform.position),
                        ["distance"] = HorizontalDistance(secret.transform.position, horse.Position), ["skill"] = check.Skill.ToString(), ["dc"] = check.DC,
                        ["openBefore"] = false, ["clicked"] = secretClicked, ["frame"] = Time.frameCount, ["inputPath"] = "ClickMapObjectHandler.OnClick",
                        ["admitted"] = Sprint3Command(s3TrapSecretCommand) });
                    if (!secretClicked) return false;
                    s3TrapSecret = secret; return true;
                }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var clicked = new ClickMapObjectHandler().OnClick(door.gameObject, door.transform.position, 0, false, false);
                doors.Add(new JObject { ["name"] = door.name, ["position"] = CapturePosition(door.transform.position), ["distance"] = HorizontalDistance(door.transform.position, horse.Position),
                    ["openBefore"] = door.IsOpen, ["notInCombat"] = door.NotInCombat, ["clicked"] = clicked, ["frame"] = Time.frameCount, ["inputPath"] = "ClickMapObjectHandler.OnClick" });
                if (!clicked) return false;
                s3TrapDoor = door; return true;
            }
            if ((int)leg["reorders"] >= Sprint3TrapLegReorderLimit) return false;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(s3TrapLegPoint, false);
            leg["reorders"] = (int)leg["reorders"] + 1;
            return true;
        }

        private JToken Sprint3DoorOutcome()
        {
            var outcome = combat.LastDoorInteractionOutcome;
            return outcome == null ? null : new JObject { ["result"] = outcome.Result, ["terminalReason"] = outcome.TerminalReason,
                ["doorStateChanged"] = outcome.DoorStateChanged, ["interactions"] = outcome.InteractionCount, ["moveStarts"] = outcome.DelegatedMoveStartCount };
        }
        // Attribution only (r13: every trap cut off from the mount's node, no closed clickable door within 12 m): the
        // native graph areas of the mount's and the point's nodes, every door and map object near the settle with the
        // Sprint 2 door predicate's own facts, and the graph connectivity from each controllable party member.
        private JObject Sprint3ObstacleInventory(Vector3 point)
        {
            var inventory = new JObject();
            try
            {
                var graph = global::AstarPath.active;
                if (graph != null)
                {
                    var mountNode = graph.GetNearest(horse.Position).node; var pointNode = graph.GetNearest(point).node;
                    inventory["mountNode"] = mountNode == null ? null : new JObject { ["area"] = mountNode.Area, ["walkable"] = mountNode.Walkable, ["position"] = CapturePosition((Vector3)mountNode.position) };
                    inventory["pointNode"] = pointNode == null ? null : new JObject { ["area"] = pointNode.Area, ["walkable"] = pointNode.Walkable, ["position"] = CapturePosition((Vector3)pointNode.position) };
                    inventory["graphs"] = graph.graphs?.Length;
                }
                var mounts = new[] { horse };
                inventory["doors"] = new JArray((UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0])
                    .Where(d => d != null && HorizontalDistance(d.transform.position, horse.Position) <= 30f)
                    .OrderBy(d => HorizontalDistance(d.transform.position, horse.Position))
                    .Select(d => { var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(d, mounts); return new JObject {
                        ["name"] = d.name, ["position"] = CapturePosition(d.transform.position), ["distance"] = HorizontalDistance(d.transform.position, horse.Position),
                        ["open"] = d.IsOpen, ["active"] = facts.Active, ["dataPresent"] = facts.DataPresent, ["interactable"] = facts.Interactable, ["availableInteractions"] = facts.AvailableInteractions,
                        ["revealed"] = facts.Revealed, ["restrictions"] = facts.RestrictionCount, ["cutPresent"] = facts.CutPresent, ["notInCombat"] = facts.NotInCombat,
                        ["hasDialog"] = facts.HasDialog, ["closedClickable"] = facts.ClosedClickable, ["inFogOfWar"] = d.GetComponentInParent<MapObjectView>()?.Data?.IsInFogOfWar }; }));
                inventory["mapObjects"] = new JArray((UnityEngine.Object.FindObjectsOfType<MapObjectView>() ?? new MapObjectView[0])
                    .Where(m => m != null && !(m is TrapObjectView) && !m.Interactions.Any(i => i is StandardDoor) && HorizontalDistance(m.transform.position, horse.Position) <= 20f)
                    .OrderBy(m => HorizontalDistance(m.transform.position, horse.Position)).Take(24)
                    .Select(m => new JObject { ["name"] = m.name, ["type"] = m.GetType().Name, ["distance"] = HorizontalDistance(m.transform.position, horse.Position),
                        ["inFogOfWar"] = m.Data?.IsInFogOfWar, ["interactions"] = new JArray(m.Interactions.Select(i => i.GetType().Name)) }));
                inventory["partyReach"] = new JArray(Game.Instance.Player.ControllableCharacters.Select(u => new JObject { ["id"] = u.UniqueId, ["reachable"] = Sprint3PathPossible(u.Position, point) }));
            }
            catch (Exception exception) { inventory["error"] = exception.ToString(); }
            return inventory;
        }

        // PR #8 second review: the one declared Working-fixture door. The r15 inventory recorded this closed
        // dwarf_dungeon_door_01 as revealed and closed-clickable from its second settle (no restriction, no dialog, no door
        // trap, not combat-gated) while every trap lay outside the pair's navmesh area. It is prepared at most once: one
        // ordinary ground walk by the unmounted party member lets the native fog reveal it, one ordinary map-object click
        // by that member lets native UnitInteractWithObject open it, and the graph is re-read only after the native cut
        // settled. Never a second door, a secret passage, a forced reveal or unlock, a teleport or a combat.
        private const string Sprint3DeclaredDoorName = "dwarf_dungeon_door_01", Sprint3DeclaredDoorArea = "9d1278a2f599b2a4daab53abdfe88d2e";
        private static readonly Vector3 Sprint3DeclaredDoorPosition = new Vector3(40f, 5.975f, 60f);
        private const float Sprint3DeclaredDoorToleranceMetres = 0.75f, Sprint3DoorPrepHostileMetres = 20f;
        private const int Sprint3DoorPrepSettleFrameLimit = 600;
        private StandardDoor s3PrepDoor; private bool s3DoorPrepared; private int s3PrepFrames, s3PrepSettledFrame;

        // PR #8 review at a8ca1603: r19 refused on the one native group beyond the declared door; the owner's mission selects
        // clearing exactly these two units of the disposable copy as fixture setup (never evidence of combat). Each takes one
        // self-initiated native lethal RuleDealDamage (no party actor, command or resource takes part); the native
        // UnitLifeController applies the death and the native combat cleanup settles; the unchanged hostile guard is then
        // re-read before the ordinary walk. Any other hostile still refuses.
        private const string Sprint3DeclaredHostileGroup = "aaac19a9-5e3b-4adc-b506-b0cdaa3492c7";
        private static readonly Dictionary<string, Vector3> Sprint3DeclaredHostiles = new Dictionary<string, Vector3> {
            ["a350a897-1800-4bc6-855f-f8c157c51340"] = new Vector3(57.81f, 6.06f, 52.31f),
            ["90fdc0ef-f893-43de-bf8c-f52b3854a9c6"] = new Vector3(63.56f, 6.06f, 62.64f) };
        private const float Sprint3DeclaredHostileToleranceMetres = 2f;
        private const int Sprint3HostileSetupSettleFrames = 60, Sprint3HostileSetupFrameLimit = 900;
        private const float Sprint3HostileSetupWakeSeconds = 2f;
        private bool s3HostileSetupPending; private StandardDoor s3SetupDoor;

        private UnitEntityData[] Sprint3DoorPrepHostiles(StandardDoor door, UnitEntityData opener) => Game.Instance.State.Units.Where(u => u != null &&
            u.IsInState && u.IsPlayersEnemy && u.Descriptor.State.IsConscious && (HorizontalDistance(u.Position, door.transform.position) <= Sprint3DoorPrepHostileMetres ||
            HorizontalDistance(u.Position, opener.Position) <= Sprint3DoorPrepHostileMetres)).ToArray();

        private JObject CaptureSprint3SetupHostile(UnitEntityData unit) => new JObject { ["id"] = unit.UniqueId, ["group"] = unit.GroupId,
            ["blueprint"] = unit.Blueprint?.AssetGuid, ["position"] = CapturePosition(unit.Position), ["inState"] = unit.IsInState, ["enemy"] = unit.IsPlayersEnemy,
            ["playerFaction"] = unit.IsPlayerFaction, ["pet"] = unit.Descriptor.IsPet, ["inCombat"] = unit.IsInCombat, ["lifeState"] = unit.Descriptor.State.LifeState.ToString(),
            ["conscious"] = unit.Descriptor.State.IsConscious, ["dead"] = unit.Descriptor.State.IsDead, ["awake"] = Game.Instance.State.AwakeUnits.Contains(unit), ["damage"] = unit.Damage,
            ["hitPoints"] = unit.Stats.HitPoints.ModifiedValue, ["constitution"] = unit.Stats.Constitution.ModifiedValue, ["groupSize"] = unit.Group?.Count };

        private JArray CaptureSprint3SetupParty() => new JArray(Game.Instance.Player.ControllableCharacters.Select(u => new JObject { ["id"] = u.UniqueId,
            ["inCombat"] = u.IsInCombat, ["conscious"] = u.Descriptor.State.IsConscious, ["damage"] = u.Damage, ["experience"] = u.Descriptor.Progression.Experience,
            ["position"] = CapturePosition(u.Position) }));

        // Returns null after one native lethal dispatch per declared unit; otherwise the refusal (nothing dispatched unless
        // every identity and precondition held, and never a second dispatch).
        private string BeginSprint3DeclaredHostileSetup(UnitEntityData[] hostiles, JObject prep)
        {
            var game = Game.Instance;
            var setup = new JObject { ["kind"] = "declared-fixture-setup", ["group"] = Sprint3DeclaredHostileGroup,
                ["contract"] = "one-self-initiated-native-lethal-ruledealdamage-per-declared-unit-then-native-death-and-combat-cleanup",
                ["declared"] = new JArray(Sprint3DeclaredHostiles.Keys), ["frame"] = Time.frameCount, ["playerInCombatBefore"] = game.Player.IsInCombat };
            prep["hostileSetup"] = setup;
            var declared = game.State.Units.Where(u => u != null && Sprint3DeclaredHostiles.ContainsKey(u.UniqueId)).ToArray();
            setup["before"] = new JArray(declared.Select(CaptureSprint3SetupHostile));
            setup["partyBefore"] = CaptureSprint3SetupParty();
            var undeclared = hostiles.Where(u => !Sprint3DeclaredHostiles.ContainsKey(u.UniqueId)).ToArray();
            if (undeclared.Length != 0) return "an undeclared conscious hostile stands near the door or the opener: " + string.Join(",", undeclared.Select(u => u.UniqueId));
            if (declared.Length != Sprint3DeclaredHostiles.Count) return "the declared hostiles are not all present in the loaded world";
            if (game.Player.IsInCombat) return "combat is active before the declared hostile setup";
            foreach (var unit in declared)
                if (unit.GroupId != Sprint3DeclaredHostileGroup || unit.Group == null || unit.Group.Count != Sprint3DeclaredHostiles.Count || !unit.IsInState ||
                    !unit.IsPlayersEnemy || unit.IsPlayerFaction || unit.Descriptor.IsPet || unit.IsInCombat || !unit.Descriptor.State.IsConscious ||
                    (bool)unit.Descriptor.State.IsRegenerate || HorizontalDistance(unit.Position, Sprint3DeclaredHostiles[unit.UniqueId]) > Sprint3DeclaredHostileToleranceMetres)
                    return "declared hostile " + unit.UniqueId + " does not match its recorded group, position or out-of-combat state";
            // r20: the native rule scales every enemy-initiated damage, self-inflicted included, by the difficulty's
            // DamageToParty after reduction; the request is sized through that same native factor (the Chunk 6A lethal
            // pattern) and the scaled result must still reach the native death threshold.
            var difficulty = game.Player.Difficulty.DamageToParty;
            setup["damageToParty"] = difficulty;
            if (difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty)) return "no finite positive native damage scale for an enemy-initiated rule";
            var dispatched = new JArray(); setup["dispatched"] = dispatched;
            foreach (var unit in declared)
            {
                var needed = checked(unit.Stats.HitPoints.ModifiedValue + unit.Stats.Constitution.ModifiedValue + unit.Stats.TemporaryHitPoints.ModifiedValue - unit.Damage + 1);
                var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
                var damage = Rulebook.Trigger(new RuleDealDamage(unit, unit, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                dispatched.Add(new JObject { ["id"] = unit.UniqueId, ["initiator"] = unit.UniqueId, ["needed"] = needed, ["requested"] = requested, ["nativeDamage"] = damage.Damage,
                    ["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty, ["damageAfter"] = unit.Damage, ["frame"] = Time.frameCount });
                if (damage.Damage < needed) return "the native rule did not carry the lethal damage to declared hostile " + unit.UniqueId;
            }
            setup["dispatches"] = dispatched.Count;
            // r23: SleepingUnitsController keeps a fogged, idle, out-of-combat unit asleep (no life tick) unless its AwakeTimer
            // runs, and the closed door kept both in fog; the engine's own UnitEntityData.Wake starts that timer once per
            // declared unit so the native UnitLifeController applies each recorded death.
            foreach (var unit in declared) unit.Wake(Sprint3HostileSetupWakeSeconds);
            setup["wake"] = new JObject { ["method"] = "UnitEntityData.Wake", ["seconds"] = Sprint3HostileSetupWakeSeconds, ["units"] = new JArray(declared.Select(u => u.UniqueId)) };
            return null;
        }

        // Waits (bounded) until both declared units are natively dead and no combat remains, then re-reads the unchanged
        // guard and orders the ordinary walk.
        private void TickSprint3DeclaredHostileSetup(JObject prep)
        {
            var setup = (JObject)prep["hostileSetup"]; var game = Game.Instance;
            var declared = game.State.Units.Where(u => u != null && Sprint3DeclaredHostiles.ContainsKey(u.UniqueId)).ToArray();
            var combat = game.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat || s3Recipient.IsInCombat;
            if (combat) setup["combatObserved"] = true;
            if (++s3PrepFrames > Sprint3HostileSetupFrameLimit)
            {
                setup["after"] = new JArray(declared.Select(CaptureSprint3SetupHostile)); setup["combatAtLimit"] = combat;
                prep["refusal"] = "the declared hostile setup did not settle (native death and combat cleanup)";
                FailSprint3Row("The declared hostile setup did not settle."); return;
            }
            ResetLeafClock();
            // r21: the native UnitLifeController (BaseUnitController) ticks only State.AwakeUnits, so a sleeping unit at its
            // death threshold dies natively on its first awake tick. Such a unit is settled only as "death pending on wake";
            // the ordinary walk wakes it, and the click requires both natively dead (no readiness while a death is pending).
            if (declared.Length != Sprint3DeclaredHostiles.Count || combat || declared.Any(u => !u.Descriptor.State.IsDead && !Sprint3DeathPendingOnWake(u)))
            { s3StableFrames = 0; return; }
            if (++s3StableFrames < Sprint3HostileSetupSettleFrames) return;
            setup["after"] = new JArray(declared.Select(CaptureSprint3SetupHostile)); setup["partyAfter"] = CaptureSprint3SetupParty();
            setup["combatAtSettle"] = combat; setup["settledFrame"] = Time.frameCount; setup["settleFrames"] = s3PrepFrames;
            setup["deathPendingOnWake"] = new JArray(declared.Where(u => !u.Descriptor.State.IsDead).Select(u => u.UniqueId));
            s3HostileSetupPending = false; s3StableFrames = 0;
            var door = s3SetupDoor; s3SetupDoor = null;
            var hostiles = Sprint3DoorPrepHostiles(door, s3Recipient).Where(u => !(Sprint3DeclaredHostiles.ContainsKey(u.UniqueId) && Sprint3DeathPendingOnWake(u))).ToArray();
            prep["hostilesNearAfterSetup"] = new JArray(hostiles.Select(u => u.UniqueId));
            var refusal = hostiles.Length != 0 ? "a conscious hostile still stands within " + Sprint3DoorPrepHostileMetres + " m after the declared setup: " +
                string.Join(",", hostiles.Select(u => u.UniqueId)) : OrderSprint3DoorPrepWalk(door, s3Recipient, prep);
            if (refusal != null) { prep["refusal"] = refusal; FailSprint3Row("The declared door preparation stopped after the hostile setup: " + refusal); return; }
            ResetLeafClock();
        }

        private static bool Sprint3DeathPendingOnWake(UnitEntityData unit) => !unit.Descriptor.State.IsDead && !Game.Instance.State.AwakeUnits.Contains(unit) &&
            !(bool)unit.Descriptor.State.IsRegenerate && unit.Damage >= unit.Stats.HitPoints.ModifiedValue + unit.Stats.Constitution.ModifiedValue;

        // Orders the opener's one ordinary walk beside the door; returns the refusal when no trap-clear route exists.
        private string OrderSprint3DoorPrepWalk(StandardDoor door, UnitEntityData opener, JObject prep)
        {
            var walk = FindSprint3DoorPrepWalkPoint(door, opener, prep);
            if (!walk.HasValue) return "no walkable point beside the door is reachable from the opener on a route clear of every active trap zone";
            s3PrepDoor = door; s3PrepFrames = 0; s3PrepSettledFrame = -1;
            SelectionManager.Instance.SelectUnit(opener.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(walk.Value, false);
            prep["walk"] = new JObject { ["destination"] = CapturePosition(walk.Value), ["ordered"] = true, ["inputPath"] = "ClickGroundHandler.MoveSelectedUnitsToPoint",
                ["frame"] = Time.frameCount, ["distanceToDoor"] = HorizontalDistance(walk.Value, door.transform.position) };
            return null;
        }

        private JObject Sprint3TrapReach() => new JObject((s3TrapCandidates ?? new TrapObjectView[0]).Select(t => new JProperty(t.UniqueId, new JObject {
            ["name"] = t.name, ["fromMount"] = Sprint3PathPossible(horse.Position, t.transform.position),
            ["fromOpener"] = s3Recipient == null ? null : Sprint3PathPossible(s3Recipient.Position, t.transform.position) })));

        // Begins the declared preparation when its exact door and every safety precondition hold; otherwise records why
        // not and returns false (the row then refuses as before).
        private bool BeginSprint3DoorPreparation()
        {
            var prep = new JObject { ["declared"] = new JObject { ["name"] = Sprint3DeclaredDoorName, ["area"] = Sprint3DeclaredDoorArea,
                ["position"] = CapturePosition(Sprint3DeclaredDoorPosition), ["source"] = "r15 obstacle inventory, second settle" } };
            s3Facts["doorPreparation"] = prep;
            s3DoorPrepared = true;
            var game = Game.Instance;
            var area = game.CurrentlyLoadedArea?.AssetGuidThreadSafe;
            prep["area"] = area;
            var matches = (UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0])
                .Where(d => d != null && d.name == Sprint3DeclaredDoorName && HorizontalDistance(d.transform.position, Sprint3DeclaredDoorPosition) <= Sprint3DeclaredDoorToleranceMetres).ToArray();
            prep["matches"] = matches.Length;
            var opener = s3Recipient;
            prep["opener"] = opener == null ? null : new JObject { ["id"] = opener.UniqueId, ["pet"] = opener.Descriptor.IsPet,
                ["conscious"] = opener.Descriptor.State.IsConscious, ["inCombat"] = opener.IsInCombat, ["position"] = CapturePosition(opener.Position) };
            string refusal = null;
            if (area != Sprint3DeclaredDoorArea) refusal = "the loaded area is not the declared Working-fixture area";
            else if (matches.Length != 1) refusal = "the declared door is not exactly one object at its recorded position";
            else if (opener == null || opener == rider || opener == horse || opener.Descriptor.IsPet || !opener.Descriptor.State.IsConscious || opener.IsInCombat)
                refusal = "no conscious, unmounted, out-of-combat party member is available to open the door";
            else if (rider.IsInCombat || horse.IsInCombat) refusal = "the pair is in combat";
            if (refusal == null)
            {
                var door = matches[0];
                var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(door, new[] { horse });
                var hostiles = Sprint3DoorPrepHostiles(door, opener);
                prep["doorBefore"] = Sprint3DoorPrepFacts(door, facts);
                prep["hostilesNear"] = new JArray(hostiles.Select(u => u.UniqueId));
                if (door.IsOpen) refusal = "the declared door is already open";
                else if (!facts.Active || !facts.DataPresent || !facts.CutPresent || facts.RestrictionCount != 0 || facts.HasDialog || facts.ViewTriggerActions || facts.HasVisibleTrap || facts.TrapActive)
                    refusal = "the declared door carries a restriction, dialog, designer trigger, trap or no native cut";
                else if (hostiles.Length != 0 && request.Scenario == "sprint3-approach-rt-trap")
                {
                    refusal = BeginSprint3DeclaredHostileSetup(hostiles, prep);
                    if (refusal == null) { s3SetupDoor = door; s3HostileSetupPending = true; s3PrepFrames = 0; return true; }
                }
                else if (hostiles.Length != 0) refusal = "a conscious hostile stands within " + Sprint3DoorPrepHostileMetres + " m of the door or the opener";
                else
                {
                    refusal = OrderSprint3DoorPrepWalk(door, opener, prep);
                    if (refusal == null) return true;
                }
            }
            prep["refusal"] = refusal;
            return false;
        }

        private JObject Sprint3DoorPrepFacts(StandardDoor door, Chunk7NativeDoorProbe.DoorFacts facts) => new JObject {
            ["name"] = door.name, ["id"] = door.GetComponentInParent<MapObjectView>()?.UniqueId, ["position"] = CapturePosition(door.transform.position),
            ["open"] = door.IsOpen, ["active"] = facts.Active, ["dataPresent"] = facts.DataPresent, ["interactable"] = facts.Interactable,
            ["availableInteractions"] = facts.AvailableInteractions, ["revealed"] = facts.Revealed, ["restrictions"] = facts.RestrictionCount,
            ["cutPresent"] = facts.CutPresent, ["notInCombat"] = facts.NotInCombat, ["hasDialog"] = facts.HasDialog, ["viewTriggerActions"] = facts.ViewTriggerActions,
            ["hasVisibleTrap"] = facts.HasVisibleTrap, ["trapActive"] = facts.TrapActive, ["closedClickable"] = facts.ClosedClickable,
            ["inFogOfWar"] = door.GetComponentInParent<MapObjectView>()?.Data?.IsInFogOfWar };

        // A walkable point 2.5 to 3.5 m from the door in the opener's own navmesh area, whose native route from the opener
        // stays outside every active trap's trigger zone (sampled along the path), nearest the door first.
        private Vector3? FindSprint3DoorPrepWalkPoint(StandardDoor door, UnitEntityData opener, JObject prep)
        {
            if (global::AstarPath.active == null) return null;
            var zones = (UnityEngine.Object.FindObjectsOfType<TrapObjectView>() ?? new TrapObjectView[0])
                .Where(t => t != null && t.TrapActive && t.ScriptZoneTrigger != null).Select(t => t.ScriptZoneTrigger).ToArray();
            var origin = door.transform.position; var tried = 0;
            foreach (var distance in new[] { 2.5f, 3.5f })
            {
                for (var index = 0; index < 16; index++)
                {
                    var nearest = global::AstarPath.active.GetNearest(origin + Quaternion.Euler(0f, index * 22.5f, 0f) * Vector3.forward * distance);
                    if (nearest.node == null || !nearest.node.Walkable) continue;
                    var point = nearest.clampedPosition;
                    if (Sprint3PathPossible(opener.Position, point) != true) continue;
                    tried++;
                    var path = Pathfinding.ABPath.Construct(opener.Position, point);
                    global::AstarPath.StartPath(path); global::AstarPath.WaitForPath(path);
                    if (path.error || path.GetTotalLength() > Sprint3PathLengthBoundMetres) continue;
                    var points = path.vectorPath; var crosses = false;
                    for (var p = 1; p < points.Count && !crosses; p++)
                        for (var s = 0; s <= 8 && !crosses; s++)
                            crosses = zones.Any(z => z.ContainsPosition(Vector3.Lerp(points[p - 1], points[p], s / 8f)));
                    if (crosses) continue;
                    prep["walkRouteLength"] = path.GetTotalLength(); prep["walkCandidatesTried"] = tried;
                    return point;
                }
            }
            prep["walkCandidatesTried"] = tried;
            return null;
        }

        private bool Sprint3OpenerSettled() => s3Recipient.Commands.Empty && s3Recipient.View?.AgentASP?.IsReallyMoving != true;

        // Steps 30 to 32: the walk settles, one ordinary click, the native open and cut settle; then the trap selection
        // re-runs once (step 0) on the updated graph.
        private void TickSprint3DoorPreparation()
        {
            var prep = (JObject)s3Facts["doorPreparation"];
            if (s3HostileSetupPending) { TickSprint3DeclaredHostileSetup(prep); return; }
            if (rider.IsInCombat || horse.IsInCombat || s3Recipient.IsInCombat) { prep["refusal"] = "combat started during the door preparation"; FailSprint3Row("The declared door preparation met combat."); return; }
            if (s3Step == 30)
            {
                // The walk is bounded by the native route bound (160 m); the leaf clock only guards a stalled walk.
                if (!Sprint3OpenerSettled()) { s3StableFrames = 0; if (s3Recipient.View?.AgentASP?.IsReallyMoving == true && ++s3PrepFrames < 3600) ResetLeafClock(); return; }
                if (++s3StableFrames < 10) return;
                if (prep["hostileSetup"] != null)
                {
                    // Every declared unit must be natively dead and the unchanged guard empty before the click.
                    var declaredAtClick = Game.Instance.State.Units.Where(u => u != null && Sprint3DeclaredHostiles.ContainsKey(u.UniqueId)).ToArray();
                    var nearAtClick = Sprint3DoorPrepHostiles(s3PrepDoor, s3Recipient);
                    prep["hostileSetup"]["atClick"] = new JArray(declaredAtClick.Select(CaptureSprint3SetupHostile));
                    prep["hostilesNearAtClick"] = new JArray(nearAtClick.Select(u => u.UniqueId));
                    if (declaredAtClick.Length != Sprint3DeclaredHostiles.Count || declaredAtClick.Any(u => !u.Descriptor.State.IsDead) || nearAtClick.Length != 0)
                    {
                        prep["refusal"] = "a declared hostile was not natively dead, or a conscious hostile stood near, when the opener reached the door";
                        FailSprint3Row("The declared hostile setup was not natively settled at the door; the route stops here."); return;
                    }
                }
                var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(s3PrepDoor, new[] { horse });
                prep["walk"]["arrived"] = CapturePosition(s3Recipient.Position); prep["walk"]["distanceToDoorAfter"] = HorizontalDistance(s3Recipient.Position, s3PrepDoor.transform.position);
                prep["doorAtClick"] = Sprint3DoorPrepFacts(s3PrepDoor, facts);
                if (s3PrepDoor.IsOpen || !facts.ClosedClickable)
                {
                    prep["refusal"] = "the declared door was not closed and clickable after the ordinary walk (native reveal)";
                    FailSprint3Row("The declared door was not revealed as a closed clickable door by the ordinary walk; the route stops here."); return;
                }
                SelectionManager.Instance.SelectUnit(s3Recipient.View, true, true, false);
                var clicked = new ClickMapObjectHandler().OnClick(s3PrepDoor.gameObject, s3PrepDoor.transform.position, 0, false, false);
                prep["click"] = new JObject { ["clicked"] = clicked, ["inputPath"] = "ClickMapObjectHandler.OnClick", ["frame"] = Time.frameCount,
                    ["executor"] = s3Recipient.UniqueId, ["admitted"] = Sprint3Command(s3Recipient.Commands.Standard) };
                if (!clicked) { prep["refusal"] = "the ordinary door click was refused"; FailSprint3Row("The ordinary click on the declared door was refused."); return; }
                s3StableFrames = 0; s3Step = 31; ResetLeafClock(); return;
            }
            if (s3Step == 31)
            {
                if (!Sprint3OpenerSettled()) { s3StableFrames = 0; return; }
                if (++s3StableFrames < 10) return;
                if (!s3PrepDoor.IsOpen)
                {
                    prep["doorAfter"] = Sprint3DoorPrepFacts(s3PrepDoor, Chunk7NativeDoorProbe.DoorFacts.Capture(s3PrepDoor, new[] { horse }));
                    prep["refusal"] = "the native interaction did not open the declared door";
                    FailSprint3Row("The native interaction did not open the declared door."); return;
                }
                prep["openedFrame"] = Time.frameCount; s3PrepFrames = 0; s3PrepSettledFrame = -1;
                s3Step = 32; ResetLeafClock(); return;
            }
            if (s3Step == 32)
            {
                // The native cut settle (the Sprint 2 door predicate): no queued graph update and a frame after the last tile update.
                var cuts = s3PrepDoor.GetComponentsInChildren<Pathfinding.NavmeshCut>();
                var pending = global::AstarPath.active == null || global::AstarPath.active.IsAnyGraphUpdatesQueued ||
                    Time.frameCount <= Pathfinding.Util.TileHandler.LastUpdateFrame || cuts.Any(c => c.enabled && c.RequiresUpdate());
                if (++s3PrepFrames > Sprint3DoorPrepSettleFrameLimit) { prep["refusal"] = "the navmesh did not settle after the door opened"; FailSprint3Row("The navmesh did not settle after the declared door opened."); return; }
                if (pending) { s3PrepSettledFrame = -1; return; }
                if (s3PrepSettledFrame < 0) { s3PrepSettledFrame = Time.frameCount; return; }
                if (Time.frameCount <= s3PrepSettledFrame + 2) return;
                prep["doorAfter"] = Sprint3DoorPrepFacts(s3PrepDoor, Chunk7NativeDoorProbe.DoorFacts.Capture(s3PrepDoor, new[] { horse }));
                prep["settledFrame"] = Time.frameCount; prep["settleFrames"] = s3PrepFrames;
                prep["reachAfter"] = Sprint3TrapReach();
                prep["obstacleInventoryAfter"] = Sprint3ObstacleInventory(s3TrapCandidates[0].transform.position);
                s3PrepDoor = null; s3StableFrames = 0; s3Step = 0; ResetLeafClock(); return;
            }
        }

        // The pair returns to where the row began through one ordinary ground order before the row finishes, so the
        // party stays together for the later rows and the tranche cleanup (r07: a 46 m walk-back overran it).
        private bool s3TrapVerdict; private string s3TrapDetail; private Vector3 s3TrapOrigin;
        private void BeginSprint3TrapReturn(bool passed, string detail)
        {
            s3TrapVerdict = passed; s3TrapDetail = detail;
            if (!Sprint3Settled() || HorizontalDistance(horse.Position, s3TrapOrigin) < 2f) { FinishSprint3Row(passed, detail); return; }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(s3TrapOrigin, false);
            s3Facts["returnLeg"] = new JObject { ["origin"] = CapturePosition(s3TrapOrigin), ["distance"] = HorizontalDistance(horse.Position, s3TrapOrigin), ["ordered"] = true };
            s3StableFrames = 0; s3Step = 20; ResetLeafClock();
        }

        private void TickSprint3Trap()
        {
            if (s3Step >= 30) { TickSprint3DoorPreparation(); return; }
            if (s3Step == 0)
            {
                var all = (UnityEngine.Object.FindObjectsOfType<TrapObjectView>() ?? new TrapObjectView[0]).Where(t => t != null && t.Data != null).ToArray();
                s3Facts["traps"] = new JArray(all.Select(Sprint3TrapFacts));
                // Active traps nearest first. Discovery (the native Perception roll) and the fog reveal are native facts the
                // pair's own exploration legs supply; the fixture's saved discovery state does not survive its load (r06).
                s3TrapCandidates = all.Where(t => t.TrapActive && t.Interactions.Any(i => i is DisableTrapInteractionComponent))
                    .OrderByDescending(t => Sprint3PathPossible(horse.Position, t.transform.position) == true)
                    .ThenBy(t => HorizontalDistance(t.transform.position, horse.Position)).Take(Sprint3TrapCandidateLimit).ToArray();
                if (s3TrapCandidates.Length == 0) { FailSprint3Row("No active trap with a disarm interaction exists in the loaded fixture area."); return; }
                // PR #8 review: a trap outside the pair's own navmesh area (the native graph connectivity test) is refused before
                // any exploration leg is ordered; the obstacle inventory records why (r12 to r16: every Working-fixture trap is cut off).
                // The second review allows the one declared door preparation first, once; its outcome is re-read here.
                if (!s3TrapCandidates.Any(t => Sprint3PathPossible(horse.Position, t.transform.position) == true))
                {
                    s3Facts["obstacleInventory"] = Sprint3ObstacleInventory(s3TrapCandidates[0].transform.position);
                    if (!s3DoorPrepared)
                    {
                        s3Facts["reachBeforeDoorPreparation"] = Sprint3TrapReach();
                        if (BeginSprint3DoorPreparation()) { s3StableFrames = 0; s3Step = 30; ResetLeafClock(); return; }
                        FailSprint3Row("No active disarmable trap shares the mounted pair's navmesh area and the declared door preparation was refused: " +
                            (string)s3Facts["doorPreparation"]["refusal"]); return;
                    }
                    FailSprint3Row(s3Facts["doorPreparation"] == null
                        ? "No active disarmable trap shares the mounted pair's navmesh area; no exploration leg was ordered."
                        : "The declared door opened but no active disarmable trap joined the mounted pair's navmesh area; the route stops here."); return;
                }
                var discovered = s3TrapCandidates.FirstOrDefault(t => t.Data.IsPerceptionCheckPassed && t.Data.IsRevealed &&
                    t.Interactions.Any(i => i is DisableTrapInteractionComponent && i.CanInteract()) && HorizontalDistance(t.transform.position, horse.Position) >= 4f);
                s3TrapOrigin = horse.Position;
                if (discovered != null) { s3Trap = discovered; s3Step = 11; return; }
                s3TrapCandidateIndex = 0;
                if (!BeginSprint3TrapLeg()) { FailSprint3Row("The nearest trap has no bounded native route from the fixture pair."); return; }
                s3Step = 10; ResetLeafClock(); return;
            }
            if (s3Step == 10)
            {
                var legs = (JArray)s3Facts["explorationLegs"]; var leg = (JObject)legs[legs.Count - 1];
                ObserveSprint3TrapMotion(leg);
                if (!Sprint3Settled()) { s3StableFrames = 0; return; }
                if (++s3StableFrames < 10) return;
                if (s3TrapDiscoveryFrames == 0 && !s3Trap.Data.IsPerceptionCheckPassed && ReorderSprint3TrapLeg(leg)) { s3StableFrames = 0; ResetLeafClock(); return; }
                leg["distanceAfter"] = HorizontalDistance(s3Trap.transform.position, horse.Position);
                leg["perceptionAfter"] = s3Trap.Data.IsPerceptionCheckPassed; leg["revealedAfter"] = s3Trap.Data.IsRevealed;
                leg["perceptionChecks"] = s3TrapObserver.PerceptionChecks(s3Trap.UniqueId);
                leg["nativeGatesAtArrival"] = Sprint3TrapGates(s3Trap);
                var usable = s3Trap.Data.IsPerceptionCheckPassed && s3Trap.Data.IsRevealed && s3Trap.Interactions.Any(i => i is DisableTrapInteractionComponent && i.CanInteract());
                // The native rolls happen on moving ticks inside the radius; a bounded still wait lets a late reveal settle.
                if (!usable && ++s3TrapDiscoveryFrames < Sprint3TrapDiscoveryWaitFrames) { s3StableFrames = 10; return; }
                if (!usable)
                {
                    leg["outcome"] = "not discovered natively";
                    if (++s3TrapCandidateIndex >= s3TrapCandidates.Length || !BeginSprint3TrapLeg())
                    {
                        s3Facts["failure"] = "No trap was discovered natively by the party's own Perception within the bounded exploration legs.";
                        BeginSprint3TrapReturn(false, (string)s3Facts["failure"]); return;
                    }
                    s3StableFrames = 0; ResetLeafClock(); return;
                }
                leg["outcome"] = "discovered";
                s3StableFrames = 0; s3MountTravelled = 0f; s3LastMountSample = horse.Position; s3MountStart = horse.Position; s3RiderStart = rider.Position;
                s3Step = 11; ResetLeafClock(); return;
            }
            if (s3Step == 11)
            {
                s3Facts["trapAtClick"] = Sprint3TrapFacts(s3Trap);
                if (!s3Trap.Data.IsPerceptionCheckPassed || !s3Trap.Data.IsRevealed) { FailSprint3Row("The trap is not discovered and revealed at the click; the native click handler refuses it."); return; }
                if (HorizontalDistance(s3Trap.transform.position, horse.Position) < 3f) { FailSprint3Row("The pair stands inside immediate interaction range of the trap; no approach to prove."); return; }
                // Fixture setup on the disposable rider only: the native rank cap (character level) in Trickery, restored at
                // cleanup. The check, DC, roll and consequence stay native; nobody on the Working fixture has a rank.
                s3TrickeryBefore = rider.Stats.SkillThievery.BaseValue;
                if (s3TrickeryBefore <= 0)
                {
                    rider.Stats.SkillThievery.BaseValue = Math.Max(1, rider.Descriptor.Progression.CharacterLevel);
                    s3TrickeryGranted = true;
                }
                s3Facts["thievery"] = new JObject { ["baseBefore"] = s3TrickeryBefore, ["baseGranted"] = rider.Stats.SkillThievery.BaseValue,
                    ["modified"] = rider.Stats.SkillThievery.ModifiedValue, ["granted"] = s3TrickeryGranted, ["mountBase"] = horse.Stats.SkillThievery.BaseValue };
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                s3Facts["trap"] = new JObject { ["id"] = s3Trap.UniqueId, ["name"] = s3Trap.name, ["position"] = CapturePosition(s3Trap.transform.position),
                    ["distanceAtClick"] = HorizontalDistance(s3Trap.transform.position, horse.Position), ["disableDc"] = s3Trap.Data.DisableDC,
                    ["blueprintDc"] = s3Trap.Blueprint?.DisableDC, ["triggerMargin"] = s3Trap.Blueprint?.DisableTriggerMargin,
                    ["notInCombat"] = s3Trap.Interactions.OfType<DisableTrapInteractionComponent>().FirstOrDefault()?.NotInCombat };
                var clicked = new ClickMapObjectHandler().OnClick(s3Trap.gameObject, s3Trap.transform.position, 0, false, false);
                s3Facts["clicked"] = clicked; s3Facts["inputCount"] = 1; s3Facts["inputPath"] = "ClickMapObjectHandler.OnClick";
                s3Command = rider.Commands.Standard as UnitInteractWithObject;
                s3Facts["admitted"] = Sprint3Command(s3Command); s3Facts["afterInput"] = Sprint3State();
                if (!clicked || s3Command == null) { FailSprint3Row("The ordinary trap click did not admit a native UnitInteractWithObject on the rider."); return; }
                s3Step = 1; ResetLeafClock(); return;
            }
            if (s3Step == 20)
            {
                if (!Sprint3Settled()) { s3StableFrames = 0; return; }
                if (++s3StableFrames < 10) return;
                ((JObject)s3Facts["returnLeg"])["distanceAfter"] = HorizontalDistance(horse.Position, s3TrapOrigin);
                FinishSprint3Row(s3TrapVerdict, s3TrapDetail); return;
            }
            if (s3Command != null && !s3Command.IsFinished) return;
            if (!Sprint3Settled()) { s3StableFrames = 0; return; }
            if (++s3StableFrames < 10) return;
            var check = s3TrapObserver.Checks;
            var disarm = s3TrapObserver.Disarms;
            s3Facts["trapAfter"] = new JObject { ["active"] = s3Trap.TrapActive, ["distanceToMount"] = HorizontalDistance(s3Trap.transform.position, horse.Position),
                ["distanceToRider"] = HorizontalDistance(s3Trap.transform.position, rider.Position) };
            var passed = s3Command != null && s3Command.IsActed && s3Command.Result == UnitCommand.ResultType.Success && s3CarriedSeen &&
                s3MountTravelled >= 1f && check == 1 && disarm == 1;
            // The row's verdict is fixed here; the pair's return to the row origin runs before the row is reported.
            s3Facts["command"] = Sprint3Command(s3Command); s3Facts["carriedSeenAtJudge"] = s3CarriedSeen; s3Facts["mountTravelledAtJudge"] = s3MountTravelled;
            BeginSprint3TrapReturn(passed, passed ? "Ordinary trap click: mount approach, one native rider Trickery check, native disarm outcome."
                : "Trap row incomplete: acted=" + s3Command?.IsActed + "; result=" + s3Command?.Result + "; carried=" + s3CarriedSeen +
                  "; travelled=" + s3MountTravelled.ToString("F2") + "; checks=" + check + "; disarmEvents=" + disarm + ".");
        }

        // ---- SPELL-01 (real time) -------------------------------------------------------------------------------
        // Moves the recipient to a walkable point the requested distance from the mount through its own ordinary ground
        // order; returns true once it stands still there. Fixture positioning of the recipient only, never of the pair.
        private bool PlaceSprint3Recipient(float distance)
        {
            if (s3Recipient == null) return true;
            var current = HorizontalDistance(s3Recipient.Position, horse.Position);
            if (s3Facts != null && s3Facts["recipientPlacement"] == null || s3Facts == null && CastingMeasurement["recipientPlacement"] == null)
            {
                var record = new JObject { ["requestedDistance"] = distance, ["distanceBefore"] = current };
                if (Math.Abs(current - distance) > 1.5f)
                {
                    // A long placement (beyond one turn's pair movement) takes the nearest walkable bearing the fixture area
                    // offers at the requested distance or a little short of it; the row later checks the actual distance.
                    Vector3 destination; var tolerance = distance > 12f ? 2f : 1f;
                    try { destination = FindWalkablePoint(horse.Position, distance, tolerance, p => HorizontalDistance(p, horse.Position) >= distance - tolerance); }
                    catch (InvalidOperationException) when (distance > 12f)
                    {
                        destination = FindWalkablePoint(horse.Position, distance - 4f, 3f, p => HorizontalDistance(p, horse.Position) >= distance - 7f);
                        record["fallbackDistance"] = distance - 4f;
                    }
                    SelectionManager.Instance.SelectUnit(s3Recipient.View, true, true, false);
                    ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    record["destination"] = CapturePosition(destination); record["ordered"] = true;
                }
                else record["ordered"] = false;
                if (s3Facts != null) s3Facts["recipientPlacement"] = record; else CastingMeasurement["recipientPlacement"] = record;
                return false;
            }
            if (!s3Recipient.Commands.Empty || s3Recipient.View?.AgentASP?.IsReallyMoving == true) return false;
            var record2 = (JObject)(s3Facts != null ? s3Facts["recipientPlacement"] : CastingMeasurement["recipientPlacement"]);
            record2["distanceAfter"] = HorizontalDistance(s3Recipient.Position, horse.Position);
            return true;
        }

        private void WoundSprint3Recipient(UnitEntityData subject)
        {
            if (subject.Damage > 0) return;
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            if (!subject.Descriptor.State.IsConscious || factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor))
                throw new InvalidOperationException("Heal stimulus lacks an intact disposable subject and finite native difficulty.");
            var amount = (int)Math.Ceiling(3d / factor);
            if (amount * factor >= subject.Stats.HitPoints.ModifiedValue - 2) throw new InvalidOperationException("Heal stimulus has no safe native margin.");
            Rulebook.Trigger(new RuleDealDamage(subject, subject, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
        }

        private UnitUseAbility IssueSprint3Cast(AbilityData ability, UnitEntityData castTarget, string recordKey)
        {
            var record = Chunk6StagedScenarioRecord(recordKey);
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            if (ability == null || ability.Caster.Unit != rider) throw new InvalidOperationException("Native ability caster is not the exact rider.");
            castingAbility = ability;
            record["ability"] = castingTrace.Ability(ability); record["target"] = castTarget?.UniqueId;
            record["distanceMountToTarget"] = castTarget == null ? (float?)null : HorizontalDistance(castTarget.Position, horse.Position);
            record["before"] = Sprint3State();
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(ability);
            record["selected"] = ReferenceEquals(handler.Ability, ability);
            var inputObject = castTarget?.View?.gameObject; var inputPoint = castTarget?.Position ?? Vector3.zero;
            var resolved = handler.GetTarget(inputObject, inputPoint, ability);
            record["resolvedTarget"] = resolved?.Unit?.UniqueId; record["canTarget"] = resolved != null && ability.CanTarget(resolved);
            record["clicked"] = handler.OnClick(inputObject, inputPoint, 0, false, false); record["inputPath"] = "ClickWithSelectedAbilityHandler.OnClick";
            s3Facts["inputCount"] = (int)s3Facts["inputCount"] + 1;
            var shells = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>().Distinct().ToArray();
            var shell = shells.FirstOrDefault(c => c.Spell.Blueprint == ability.Blueprint) ??
                shells.FirstOrDefault(c => c.Spell.Blueprint == ability.Blueprint.StickyTouch?.TouchDeliveryAbility);
            record["admittedShellCount"] = shells.Length; record["shell"] = Sprint3Command(shell);
            record["queued"] = shell != null && rider.Commands.Queue.Contains(shell);
            if (handler.Ability != null) handler.SetAbility(null);
            record["afterInput"] = Sprint3State();
            if (shell != null) { castingShell = shell; s3Command = shell; }
            return shell;
        }
        private JObject Chunk6StagedScenarioRecord(string key) => Chunk6StagedScenarioRecordFor(s3Facts, key);
        private static JObject Chunk6StagedScenarioRecordFor(JObject owner, string key)
        {
            var existing = owner[key] as JObject; if (existing != null) return existing;
            var created = new JObject(); owner[key] = created; return created;
        }

        private UnitUseAbility CurrentSprint3Shell()
        {
            var raw = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>().Where(c => !c.IsFinished).ToArray();
            return raw.FirstOrDefault(c => !c.IsStarted) ?? raw.FirstOrDefault();
        }

        private void TickSprint3Friendly(float distance, bool heldChargeCase)
        {
            if (s3Recipient == null) { FailSprint3Row("The fixture party has no eligible recipient besides the pair."); return; }
            if (s3Step == 0)
            {
                if (!PlaceSprint3Recipient(distance)) return;
                WoundSprint3Recipient(s3Recipient);
                s3Facts["recipientWounded"] = new JObject { ["damage"] = s3Recipient.Damage, ["hp"] = s3Recipient.HPLeft };
                // The reported spell: the memorized Cure Light Wounds slot on the distant row; the equipped CLW scroll stack
                // (the same sticky-touch path, item-sourced) where the one memorized slot is already spent.
                var slot = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells()).FirstOrDefault(s => s.Available && s.Spell.Blueprint.AssetGuid == Heal);
                var ability = slot != null && !heldChargeCase ? slot.Spell : ExactCastingItemAbility(castingScroll);
                castingPrepared = slot != null && !heldChargeCase ? slot : null;
                s3Facts["instrument"] = slot != null && !heldChargeCase ? "memorized-slot" : "scroll-stack";
                if (IssueSprint3Cast(ability, s3Recipient, "cast") == null) { FailSprint3Row("The ordinary ability click did not admit a native cast shell."); return; }
                s3Step = 1; ResetLeafClock(); return;
            }
            if (s3Step == 1 && heldChargeCase && !s3Stopped)
            {
                // The held-charge case: once the cast has acted and the queued delivery is being carried, the native Stop
                // interrupts the delivery (the charge stays held natively); the recipient is then clicked again.
                var delivery = CurrentSprint3Shell();
                var held = rider.Get<UnitPartTouch>();
                if (held != null && delivery != null && !delivery.IsStarted && combat.HasLiveCarriedApproach && s3MountTravelled >= 0.5f)
                {
                    s3Facts["stop"] = new JObject { ["before"] = Sprint3State(), ["deliveryShell"] = Sprint3Command(delivery), ["heldBlueprint"] = held.Ability?.Blueprint?.AssetGuid };
                    SelectionManager.Instance.Stop(); s3Stopped = true; s3Facts["inputCount"] = (int)s3Facts["inputCount"] + 1;
                    ((JObject)s3Facts["stop"])["afterInput"] = Sprint3State();
                    s3Step = 2; ResetLeafClock(); return;
                }
                if (held == null && delivery == null && castingShell != null && castingShell.IsFinished && Sprint3Settled())
                { FailSprint3Row("The cast settled without a held touch charge to stop and re-deliver."); return; }
                return;
            }
            if (s3Step == 2)
            {
                if (!Sprint3Settled()) { s3StableFrames = 0; return; }
                if (++s3StableFrames < 10) return;
                var held = rider.Get<UnitPartTouch>();
                ((JObject)s3Facts["stop"])["settled"] = Sprint3State();
                ((JObject)s3Facts["stop"])["chargeHeldAfterStop"] = held != null;
                if (held == null) { FailSprint3Row("The native Stop did not retain the held touch charge."); return; }
                // The ordinary re-delivery input is the held touch itself (the action-bar entry the engine shows while a
                // charge is held); re-clicking the scroll would read a fresh scroll cast (r02).
                var redelivery = IssueSprint3Cast(held.Ability.Data, s3Recipient, "redeliver");
                ((JObject)s3Facts["redeliver"])["resolvedToHeldTouch"] = redelivery != null && redelivery.Spell.Blueprint == held.Ability.Blueprint;
                if (redelivery == null) { FailSprint3Row("The second ordinary click did not admit the held touch delivery."); return; }
                s3StableFrames = 0; s3Step = 3; ResetLeafClock(); return;
            }
            var live = CurrentSprint3Shell();
            if (live != null && !live.IsFinished) return;
            if (!Sprint3Settled()) { s3StableFrames = 0; return; }
            if (++s3StableFrames < 10) return;
            var events = castingTrace.EventsSince(s3EventOffset);
            var heals = events.Count(e => (string)e["kind"] == "heal" && (string)e["target"] == s3Recipient.UniqueId);
            var casts = events.Count(e => (string)e["kind"] == "cast-after");
            var itemSpends = events.Count(e => (string)e["kind"] == "item-spend-after" && e["result"]?.Value<bool>() == true);
            var touchLeft = rider.Get<UnitPartTouch>() != null;
            var passed = heals == 1 && casts >= 2 && s3CarriedSeen && s3MountTravelled >= 1f && !touchLeft &&
                (s3Facts["instrument"].ToString() == "memorized-slot" ? itemSpends == 0 : itemSpends == 1) && s3Recipient.Damage < (int)s3Facts["recipientWounded"]["damage"] &&
                (!heldChargeCase || s3Stopped);
            s3Facts["summary"] = new JObject { ["heals"] = heals, ["casts"] = casts, ["itemSpends"] = itemSpends, ["heldTouchLeft"] = touchLeft };
            FinishSprint3Row(passed, passed ? "Ordinary Cure Light Wounds on a distant ally: mount approach, native cast, held touch delivered once."
                : "Friendly touch row incomplete: heals=" + heals + "; casts=" + casts + "; itemSpends=" + itemSpends + "; carried=" + s3CarriedSeen +
                  "; travelled=" + s3MountTravelled.ToString("F2") + "; heldLeft=" + touchLeft + "; stopped=" + s3Stopped + ".");
        }

        private void TickSprint3Hostile()
        {
            if (s3Step == 0)
            {
                if (!target.Commands.Empty || target.View?.AgentASP?.IsReallyMoving == true) return;
                if (IssueSprint3Cast(ExactCastingItemAbility(castingScroll), target, "cast") == null) { FailSprint3Row("The ordinary ability click did not admit a native cast shell on the hostile."); return; }
                s3Step = 1; ResetLeafClock(); return;
            }
            var live = CurrentSprint3Shell();
            if (live != null && !live.IsFinished) return;
            if (!Sprint3Settled()) { s3StableFrames = 0; return; }
            if (++s3StableFrames < 10) return;
            var rolls = Sprint3RiderAttackRolls(target);
            var events = castingTrace.EventsSince(s3EventOffset);
            var casts = events.Count(e => (string)e["kind"] == "cast-after");
            var itemSpends = events.Count(e => (string)e["kind"] == "item-spend-after" && e["result"]?.Value<bool>() == true);
            var hit = rolls.Any(r => ((string)r["result"]).StartsWith("Hit", StringComparison.Ordinal) || (string)r["result"] == "CriticalHit");
            var effects = events.Count(e => ((string)e["kind"] == "heal" || (string)e["kind"] == "damage") && (string)e["target"] == target.UniqueId);
            var heldAfter = rider.Get<UnitPartTouch>() != null;
            // Native touch delivery: a hit consumes the charge and delivers one effect; a legal miss delivers nothing and the
            // charge stays held (r06 missed on a native roll of 6 against AC 12).
            var passed = casts >= 2 && rolls.Length == 1 && itemSpends == 1 && s3CarriedSeen && s3MountTravelled >= 1f &&
                (hit ? effects == 1 && !heldAfter : effects == 0 && heldAfter);
            s3Facts["summary"] = new JObject { ["casts"] = casts, ["attackRolls"] = rolls.Length, ["hit"] = hit, ["effects"] = effects, ["itemSpends"] = itemSpends, ["heldAfter"] = heldAfter };
            FinishSprint3Row(passed, passed ? "Ordinary touch delivery on a hostile: mount approach, native touch attack roll, one expenditure."
                : "Hostile touch row incomplete: casts=" + casts + "; rolls=" + rolls.Length + "; hit=" + hit + "; effects=" + effects + "; itemSpends=" + itemSpends +
                  "; carried=" + s3CarriedSeen + "; travelled=" + s3MountTravelled.ToString("F2") + ".");
        }

        private void TickSprint3Ranged()
        {
            if (s3Step == 0)
            {
                // The diagnostic hostile retreats through its own native move; nothing moves the pair.
                var destination = FindWalkablePointAwayFromTarget(target.Position, horse.Position, Sprint3RangedRetreatMetres);
                target.Commands.Run(new UnitMoveTo(destination) { CreatedByPlayer = true });
                s3Facts["hostileRetreat"] = new JObject { ["destination"] = CapturePosition(destination), ["distanceBefore"] = HorizontalDistance(target.Position, horse.Position) };
                s3Step = 1; ResetLeafClock(); return;
            }
            if (s3Step == 1)
            {
                if (!target.Commands.Empty || target.View?.AgentASP?.IsReallyMoving == true) return;
                ((JObject)s3Facts["hostileRetreat"])["distanceAfter"] = HorizontalDistance(target.Position, horse.Position);
                var ability = ResolveCastingAbility(Snowball, true);
                s3Facts["rangeMetres"] = ability.GetApproachDistance(target);
                if (IssueSprint3Cast(ability, target, "cast") == null) { FailSprint3Row("The ordinary ability click did not admit the ranged cast shell."); return; }
                s3Step = 2; ResetLeafClock(); return;
            }
            var live = CurrentSprint3Shell();
            if (live != null && !live.IsFinished) return;
            if (!Sprint3Settled()) { s3StableFrames = 0; return; }
            if (++s3StableFrames < 10) return;
            var events = castingTrace.EventsSince(s3EventOffset);
            var cast = events.FirstOrDefault(e => (string)e["kind"] == "cast-after");
            var rolls = Sprint3RiderAttackRolls(target).Count(r => (string)r["attackType"] == "RangedTouch" || (string)r["attackType"] == "Ranged");
            var outcome = (JArray)s3Facts["carriedOutcomes"];
            var distanceAtCast = outcome != null && outcome.Count > 0 ? (float?)outcome[outcome.Count - 1]["distanceAtSatisfaction"] : null;
            var range = (float)s3Facts["rangeMetres"];
            var passed = cast != null && rolls == 1 && s3CarriedSeen && s3MountTravelled >= 1f && distanceAtCast.HasValue &&
                distanceAtCast.Value <= range + 0.05f && distanceAtCast.Value > 3f;
            s3Facts["summary"] = new JObject { ["cast"] = cast != null, ["attackRolls"] = rolls, ["distanceAtCast"] = distanceAtCast, ["range"] = range };
            FinishSprint3Row(passed, passed ? "Ordinary ranged spell beyond range: mount approach to the spell's own range, native cast, projectile attack roll."
                : "Ranged row incomplete: cast=" + (cast != null) + "; rolls=" + rolls + "; carried=" + s3CarriedSeen + "; distanceAtCast=" + distanceAtCast + "; range=" + range + ".");
        }

        // ---- SPELL-01 (turn-based) -----------------------------------------------------------------------------
        private void TickSprint3TurnBasedFriendly(TurnController turn, bool insufficientCase)
        {
            if (s3Recipient == null) { FailSprint3Row("The fixture party has no eligible recipient besides the pair."); return; }
            if (s3Step == 0)
            {
                // The insufficient-movement row (first) uses the recipient placed before the encounter, beyond one turn's pair
                // movement; the friendly row (second) repositions the recipient on the recipient's own turn through its ordinary
                // ground order within its own budget. The rider's row begins on the next rider turn.
                var allowance = horse.CombatSpeedMps * 6f;
                if (insufficientCase && s3Facts["recipientPlacement"] == null)
                {
                    var placement = (JObject)(CastingMeasurement["recipientPlacement"]?.DeepClone() ?? new JObject { ["ordered"] = false });
                    placement["distanceAtRow"] = HorizontalDistance(s3Recipient.Position, horse.Position);
                    s3Facts["recipientPlacement"] = placement;
                    if ((float)placement["distanceAtRow"] <= allowance + 1f)
                    { FailSprint3Row("The fixture area offered no placement beyond one turn's pair movement (" + ((float)placement["distanceAtRow"]).ToString("F1") + " m against " + allowance.ToString("F1") + " m)."); return; }
                }
                if (!insufficientCase && s3Facts["recipientPlacement"] == null)
                {
                    if (turn?.Unit != s3Recipient) { EndCastingNativeTurn(turn); return; }
                    // The point must share the mount's graph area (r14: a point on the cut-off island left the recipient 3.7 m away).
                    var destination = FindWalkablePoint(horse.Position, Sprint3TurnBasedFriendlyMetres, 1.5f, p => HorizontalDistance(p, horse.Position) >= Sprint3TurnBasedFriendlyMetres - 1.5f && Sprint3PathPossible(horse.Position, p) == true);
                    SelectionManager.Instance.SelectUnit(s3Recipient.View, true, true, false);
                    ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    s3Facts["recipientPlacement"] = new JObject { ["requestedDistance"] = Sprint3TurnBasedFriendlyMetres, ["destination"] = CapturePosition(destination),
                        ["distanceBefore"] = HorizontalDistance(s3Recipient.Position, horse.Position), ["recipientTurn"] = true };
                    s3RecipientTurn = turn; return;
                }
                if (!insufficientCase && ReferenceEquals(turn, s3RecipientTurn))
                {
                    if (!s3Recipient.Commands.Empty || s3Recipient.View?.AgentASP?.IsReallyMoving == true) return;
                    ((JObject)s3Facts["recipientPlacement"])["distanceAfter"] = HorizontalDistance(s3Recipient.Position, horse.Position);
                    EndCastingNativeTurn(turn); return;
                }
                if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
                if (!rider.HasStandardAction() || horse.CombatState.Cooldown.MoveAction > .001f) { EndCastingNativeTurn(turn); return; }
                WoundSprint3Recipient(s3Recipient);
                s3Facts["recipientWounded"] = new JObject { ["damage"] = s3Recipient.Damage, ["hp"] = s3Recipient.HPLeft };
                var slot = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells()).FirstOrDefault(s => s.Available && s.Spell.Blueprint.AssetGuid == Heal);
                var ability = slot != null ? slot.Spell : ExactCastingItemAbility(castingScroll);
                castingPrepared = slot;
                s3Facts["instrument"] = slot != null ? "memorized-slot" : "scroll-stack";
                s3Facts["rowTurnActor"] = turn.Unit.UniqueId; s3Facts["allowanceMetres"] = horse.CombatSpeedMps * 6f;
                if (IssueSprint3Cast(ability, s3Recipient, "cast") == null) { FailSprint3Row("The ordinary ability click did not admit a native cast shell."); return; }
                s3RowTurn = turn; s3Step = 1; ResetLeafClock(); return;
            }
            if (s3Step == 1)
            {
                // The rider's turn: the cast acts, the queued delivery is carried; the turn ends natively once the pair is settled.
                var live = CurrentSprint3Shell();
                if (turn?.Unit == rider && (live != null && !live.IsFinished || horse.View?.AgentASP?.IsReallyMoving == true || !rider.Commands.Empty)) return;
                if (turn?.Unit == rider && ReferenceEquals(turn, s3RowTurn))
                {
                    // The first rider turn ends here; after the next-turn re-delivery the same step ends the second turn
                    // and the row is judged (r11: re-entering the held-charge step a turn after the delivery failed the row).
                    s3Facts[s3SecondInputSent ? "secondTurnEnd" : "firstTurnEnd"] = Sprint3State();
                    s3Facts[s3SecondInputSent ? "secondTurnEvents" : "firstTurnEvents"] = castingTrace.EventsSince(s3EventOffset);
                    EndCastingNativeTurn(turn);
                    s3Step = insufficientCase && !s3SecondInputSent ? 2 : 3; ResetLeafClock(); return;
                }
                if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
                return;
            }
            if (s3Step == 2)
            {
                // The next rider turn: the charge is still held; the recipient is clicked again and the delivery is carried.
                if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
                if (ReferenceEquals(turn, s3RowTurn)) return;
                var held = rider.Get<UnitPartTouch>();
                s3Facts["secondTurn"] = new JObject { ["before"] = Sprint3State(), ["heldCharge"] = held != null, ["deliverTouchMode"] = turn.EnableDeliverTouch };
                if (held == null) { FailSprint3Row("The held touch charge did not survive the native turn boundary."); return; }
                if (IssueSprint3Cast(held.Ability.Data, s3Recipient, "redeliver") == null) { FailSprint3Row("The second ordinary click did not admit the held touch delivery."); return; }
                ((JObject)s3Facts["redeliver"])["resolvedToHeldTouch"] = s3Command is UnitUseAbility second && second.Spell.Blueprint == held.Ability.Blueprint;
                s3RowTurn = turn; s3Step = 1; s3SecondInputSent = true; ResetLeafClock(); return;
            }
            // Step 3: settle across the turn boundary and judge.
            if (turn?.Unit == rider && ReferenceEquals(turn, s3RowTurn)) return;
            if (!Sprint3Settled()) { s3StableFrames = 0; return; }
            if (++s3StableFrames < 10) return;
            var events = castingTrace.EventsSince(s3EventOffset);
            var heals = events.Count(e => (string)e["kind"] == "heal" && (string)e["target"] == s3Recipient.UniqueId);
            var casts = events.Count(e => (string)e["kind"] == "cast-after");
            var itemSpends = events.Count(e => (string)e["kind"] == "item-spend-after" && e["result"]?.Value<bool>() == true);
            var standardCosts = castingCosts.EventsSince(s3CostOffset).Count(e => (string)e["boundary"] == "cost-after" && (string)e["state"]?["actor"] == rider.UniqueId);
            var outcomes = (JArray)s3Facts["carriedOutcomes"] ?? new JArray();
            var signalled = outcomes.Any(o => o["nativeApproachEndSignalled"]?.Value<bool>() == true);
            var passed = heals == 1 && casts >= 2 && s3CarriedSeen && s3MountTravelled >= 1f && rider.Get<UnitPartTouch>() == null &&
                (s3Facts["instrument"].ToString() == "memorized-slot" ? itemSpends == 0 : itemSpends == 1) && (!insufficientCase || signalled && s3SecondInputSent);
            s3Facts["summary"] = new JObject { ["heals"] = heals, ["casts"] = casts, ["itemSpends"] = itemSpends, ["riderStandardCostEvents"] = standardCosts,
                ["nativeApproachEndSignalled"] = signalled, ["secondInput"] = s3SecondInputSent };
            FinishSprint3Row(passed, passed ? (insufficientCase
                    ? "Turn-based touch with insufficient movement: native cast, carried approach ended by the native rule, charge held across the turn, delivered on the next rider turn."
                    : "Turn-based touch on a distant ally: native cast, carried mount movement on the rider's turn, held touch delivered once.")
                : "Turn-based row incomplete: heals=" + heals + "; casts=" + casts + "; itemSpends=" + itemSpends + "; carried=" + s3CarriedSeen +
                  "; travelled=" + s3MountTravelled.ToString("F2") + "; signalled=" + signalled + "; secondInput=" + s3SecondInputSent + ".");
        }

        private JObject CaptureSprint3DeadlineProgress()
        {
            var progress = new JObject { ["stage"] = s3Stage, ["step"] = s3Step, ["caseIndex"] = s3CaseIndex,
                ["case"] = s3CaseIndex < Sprint3Cases.Length ? Sprint3Case : null };
            try
            {
                progress["facts"] = s3Facts?.DeepClone();
                if (castingTrace != null && castingCosts != null) progress["state"] = Sprint3State();
                progress["command"] = Sprint3Command(s3Command);
                progress["mountCommands"] = CaptureCastingCommandSurface(horse); progress["riderCommands"] = CaptureCastingCommandSurface(rider);
                progress["carried"] = Sprint3CarriedOutcome(combat.LastCarriedApproachOutcome);
            }
            catch (Exception exception) { progress["captureError"] = exception.ToString(); }
            return progress;
        }

        // Sprint 3 cleanup: the shared casting drain, the rider's Trickery fixture restoration and the observers.
        private bool DrainSprint3Fixture()
        {
            if (s3TrickeryGranted)
            {
                rider.Stats.SkillThievery.BaseValue = s3TrickeryBefore;
                CastingMeasurement["thieveryRestored"] = rider.Stats.SkillThievery.BaseValue == s3TrickeryBefore;
                s3TrickeryGranted = false;
            }
            if (s3PerceptionGranted)
            {
                rider.Stats.SkillPerception.BaseValue = s3PerceptionBefore;
                CastingMeasurement["perceptionRestored"] = rider.Stats.SkillPerception.BaseValue == s3PerceptionBefore;
                CastingMeasurement["riderPerceptionBaseAfter"] = rider.Stats.SkillPerception.BaseValue;
                s3PerceptionGranted = false;
            }
            if (!DrainCastingFixture()) return false;
            if (s3Rules != null) { CastingMeasurement["ruleTrace"] = s3Rules.Capture(); s3Rules.Dispose(); s3Rules = null; }
            if (s3TrapObserver != null) { CastingMeasurement["trapTrace"] = s3TrapObserver.Capture(); s3TrapObserver.Dispose(); s3TrapObserver = null; }
            CastingMeasurement["riderThieveryBaseAfter"] = rider.Stats.SkillThievery.BaseValue;
            return true;
        }

        // Observation only: the native Trickery check and the disarm handler events of the fixture rider.
        private sealed class Sprint3TrapObserver : IDisposable, IGlobalRulebookHandler<RuleSkillCheck>, IDisarmTrapHandler
        {
            private readonly UnitEntityData rider;
            private readonly IDisposable subscription;
            private readonly JArray events = new JArray();
            private string caseId;
            internal Sprint3TrapObserver(UnitEntityData rider) { this.rider = rider; subscription = EventBus.Subscribe(this); }
            internal int Checks => events.Count(e => (string)e["kind"] == "skill-check" && (string)e["caseId"] == caseId);
            internal int Disarms => events.Count(e => ((string)e["kind"]).StartsWith("disarm-", StringComparison.Ordinal) && (string)e["caseId"] == caseId);
            internal void BeginCase(string value) { caseId = value; }
            internal JObject Capture() => new JObject { ["events"] = events.DeepClone() };
            internal int EventCount => events.Count;
            internal JArray EventsSince(int offset) => new JArray(events.Skip(Math.Max(0, offset)).Select(e => e.DeepClone()));
            // The party's native Perception rolls against one trap (PartyPerceptionController: one roll per moving character per rank).
            internal JArray PerceptionChecks(string trapId) => new JArray(events.Where(e => (string)e["kind"] == "perception-check" &&
                (trapId == null || (string)e["trap"] == trapId)).Select(e => e.DeepClone()));
            private JObject Record(string kind, UnitEntityData actor) { var e = new JObject { ["kind"] = kind, ["caseId"] = caseId, ["actor"] = actor?.UniqueId, ["frame"] = Time.frameCount }; if (events.Count < 256) events.Add(e); return e; }
            public void OnEventAboutToTrigger(RuleSkillCheck evt) { }
            public void OnEventDidTrigger(RuleSkillCheck evt)
            {
                if (evt.StatType == StatType.SkillPerception)
                {
                    var reason = evt.Reason.SourceEntity as Kingmaker.EntitySystem.StaticEntityData;
                    if (reason == null || !(reason.View is TrapObjectView)) return;
                    var p = Record("perception-check", evt.Initiator); p["trap"] = ((TrapObjectView)reason.View).UniqueId; p["dc"] = evt.DC;
                    p["d20"] = (int)evt.D20; p["rollResult"] = evt.RollResult; p["statValue"] = evt.StatValue; p["passed"] = evt.IsPassed;
                    return;
                }
                if (evt.Initiator != rider) return;
                // "skill-check" stays the trap row's Trickery count; any other rider check (a secret passage) is "rider-check".
                var e = Record(evt.StatType == StatType.SkillThievery ? "skill-check" : "rider-check", evt.Initiator); e["stat"] = evt.StatType.ToString(); e["dc"] = evt.DC; e["d20"] = (int)evt.D20;
                e["rollResult"] = evt.RollResult; e["statValue"] = evt.StatValue; e["passed"] = evt.IsPassed;
            }
            public void HandleDisarmTrapSuccess(UnitEntityData unit, TrapObjectView trap) { var e = Record("disarm-success", unit); e["trap"] = trap?.UniqueId; }
            public void HandleDisarmTrapFail(UnitEntityData unit, TrapObjectView trap) { var e = Record("disarm-fail", unit); e["trap"] = trap?.UniqueId; }
            public void HandleDisarmTrapCriticalFail(UnitEntityData unit, TrapObjectView trap) { var e = Record("disarm-critical-fail", unit); e["trap"] = trap?.UniqueId; }
            public void Dispose() { subscription?.Dispose(); }
        }
    }
}
