using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 2 (MOVE-01): the mounted speed matched set, measured through ordinary ground clicks on the
    // owner-seed exploration fixture. Four legs: the mounted pair moving with the whole party (party pacing),
    // the mounted pair selected alone, the same Horse unmounted and alone, and the same rider on foot and
    // alone. Every leg records the mover's native speed facts before the click, per-frame samples of the
    // native agent speed, the entity's CurrentSpeedMps and the animation gait, and the distance actually
    // travelled over game time. Nothing here changes a speed, a command or a setting; a leg with no clear
    // nine-metre route is recorded as not applicable rather than forced.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        // v3 (Sprint 2 completion, review correction A): a mounted rider issues no locomotion command of its own (the
        // ground click is routed to its mount), so each leg records the carried riders passively: the owner, the rider's
        // own Move slot at the click (no new player command), its sampled path beside the mount and its attachment
        // through the leg. Nothing moves the rider and no mover row is fabricated for it.
        // v4 (r13 observation on the owner's surface: every selected mover was interrupted together after climbing about
        // 1 to 1.7 m toward a goal recorded at the start height): a candidate route is admitted only when the navmesh
        // height sampled along the heading stays level with the start, and the native state at the leg's end (game mode,
        // pause, combat, loading, area) is recorded so an interrupted leg is classified rather than guessed.
        // v5 (review 2026-10-31): the v4 "level" samples measured nothing (GetNearestNode keeps the requested height) and are
        // retained with that limitation; a failed leg no longer aborts the sequence: it is recorded (accepted=false, failure),
        // the later legs continue for diagnosis and the aggregate speedLegsAccepted stays false until every applicable leg
        // is accepted; the first native UnitCommand.Interrupt of each issued speed-leg Move is observed before cleanup
        // (actor, command, callers, mode, position, native path status, nearby map objects) through a run-scoped hook.
        // v6 (r17 causal result, 2026-10-31): every party mover was interrupted by the native path-completion chain
        // (UnitMovementAgent.TickMovement > CompleteMovement > InterruptMovement > UnitEntityView.OnMovementInterrupted >
        // UnitCommand.Interrupt) at the end of a two-vertex path that ended 1.2-1.5 m short of a goal recorded 1.7 m BELOW the
        // raised surface: GetNearestNode's clampedPosition keeps the requested height, so the goal was a point under the
        // navmesh and the nearest node to it (in 3D) lay short of it. The goal and the level samples now take the navmesh
        // node's own height (node.position.y / 1000, the basis of the game's own GetNearestNode height check); nothing is
        // raycast and no terrain rule is changed. The v5 "heights" of r17 are retained with the v4 limitation.
        internal const string SpeedLegContract = "sprint2-speed-legs/v6";
        private const string SpeedLegHeightBasis = "navmesh node centre height (GetNearestNode node.position.y*0.001, the game's own height-check basis); clampedPosition keeps the requested height";
        private const int SpeedLegInterruptRecordLimit = 8;
        private const float SpeedLegLevelToleranceMetres = 0.5f;
        internal const float SpeedLegDistanceMetres = 9f;
        // v6 (r19): the preferred length first, then the shorter meaningful lengths on the same bearing; five metres is the floor.
        private static readonly float[] SpeedLegDistancesMetres = { SpeedLegDistanceMetres, 7f, 5f };
        private const float SpeedLegClampResidualMetres = 0.5f;
        private const float SpeedLegDoorClearanceMetres = 1.5f;
        private const double SpeedLegWallSecondsLimit = 40d;
        private static readonly float[] SpeedLegBearingOffsets = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };

        internal enum SpeedLegSelection { Party, SecondRiderAlone, SecondMountAlone }

        private sealed class SpeedLegMover
        {
            internal UnitEntityData Unit;
            internal UnitMoveTo Move;
            internal JObject Record;
            // v2: path and motion time over the same samples (SpeedLegSampler); the v1 figure is kept beside it.
            internal readonly SpeedLegSampler Sampler = new SpeedLegSampler();
            internal long Samples, MovingSamples;
            internal double AgentSpeedSum, MaximumAgentSpeed, EntitySpeedSum, MaximumEntitySpeed;
            internal Dictionary<string, long> Gaits = new Dictionary<string, long>(StringComparer.Ordinal);
            internal bool Accelerated;
            internal string FinalMovementType;
        }

        // v3: a rider carried by a moving mount, observed only (owner identity, its own Move slot, its position beside the mount).
        private sealed class SpeedLegCarried
        {
            internal UnitEntityData Rider, Mount;
            internal object Owner;
            internal long Generation;
            internal JObject Record;
            internal readonly SpeedLegSampler Sampler = new SpeedLegSampler();
            internal UnitMoveTo RiderMoveAtClick;
            internal bool AttachedThroughout = true;
            internal double MaximumOffset;
        }

        private JArray speedLegs;
        private JObject speedLeg;
        private readonly List<string> speedLegFailures = new List<string>();
        private readonly List<UnitCommand> speedLegInterruptedCommands = new List<UnitCommand>();
        private JArray speedLegInterruptions;
        private JObject speedLegInterruptSummary;
        private SpeedLegInterruptObserver speedLegInterruptObserver;
        private List<SpeedLegMover> speedLegMovers;
        private List<SpeedLegCarried> speedLegCarried;
        private double speedLegStartedWall;
        private TimeSpan speedLegStartedGame;
        private Vector3 speedLegStart, speedLegGoal, speedLegPreviousStart;
        private bool speedLegSkipped;
        private MovementScreenshotCaptureCoordinator doorClearanceShots;
        private bool doorClearanceRequested;

        private void BeginSpeedLeg(string label, SpeedLegSelection selection)
        {
            if (speedLegs == null)
            {
                speedLegs = new JArray(); evidence["speedLegContract"] = SpeedLegContract; evidence["speedLegs"] = speedLegs; evidence["speedLegsAccepted"] = false;
                // v5: the run-scoped interrupt observation, installed once before the first leg and removed with the engine.
                speedLegInterruptions = new JArray(); speedLegInterruptSummary = new JObject();
                speedLegInterruptObserver = new SpeedLegInterruptObserver(ObserveNativeInterrupt);
                evidence["speedLegInterruptions"] = speedLegInterruptions;
                evidence["speedLegInterruptObserver"] = speedLegInterruptObserver.Describe(SpeedLegInterruptRecordLimit, speedLegInterruptSummary);
            }
            Require(speedLeg == null && speedLegMovers == null, "A speed leg is already running.");
            Require(!Game.Instance.Player.IsInCombat, "Speed legs require exploration.");
            var mover = selection == SpeedLegSelection.SecondMountAlone ? secondMount : secondMount != null && Mounted(secondRider, secondMount) ? secondMount : secondRider;
            Require(mover != null && mover.View?.AgentASP != null && Chunk7NativeFixture.Idle(mover), "The speed leg mover is not an idle native actor.");
            speedLeg = new JObject { ["label"] = label, ["selection"] = selection.ToString(), ["primaryMoverId"] = mover.UniqueId,
                ["distance"] = SpeedLegDistanceMetres, ["applicable"] = false };
            speedLegs.Add(speedLeg);
            speedLegSkipped = false;
            // Legs alternate direction: the first leg heads away from the door (or forward), the next returns toward
            // the previous start, so the pairs stay inside the fixture's bounded area.
            var legIndex = speedLegs.Count - 1;
            Vector3 preferred;
            if (legIndex % 2 == 1) preferred = speedLegPreviousStart - mover.Position;
            else if (doorProbe != null && doorProbe.DoorPosition.HasValue) preferred = mover.Position - doorProbe.DoorPosition.Value;
            else preferred = Vector3.forward;
            preferred.y = 0f;
            if (preferred.sqrMagnitude < 1e-4f) preferred = Vector3.forward;
            var excluded = SelectionUnits(selection, mover).ToArray();
            var goal = SearchSpeedLegGoal(mover, preferred.normalized, excluded, out var found);
            if (!found)
            {
                speedLeg["reason"] = "No walkable nine-metre route clear of other units and the door lies in any bearing; this leg is not applicable on this fixture.";
                speedLegSkipped = true; speedLegMovers = new List<SpeedLegMover>(); speedLegCarried = new List<SpeedLegCarried>();
                logger.Info("Sprint 2 speed leg " + label + " skipped: no clear route.");
                return;
            }
            speedLegStart = mover.Position; speedLegGoal = goal; speedLegPreviousStart = mover.Position;
            speedLeg["start"] = Point(speedLegStart); speedLeg["goal"] = Point(goal);
            speedLeg["environment"] = CaptureSpeedEnvironment();
            // Selection through the native selection manager, exactly as the player's own party selection.
            Game.Instance.IsPaused = true;
            var views = excluded.Where(u => u.View != null).Select(u => u.View).ToArray();
            // A mounted pair is selected through its rider: the native selection manager projects the mount to the rider.
            if (selection == SpeedLegSelection.Party) SelectionManager.Instance.MultiSelect(views, false);
            else Chunk7NativeFixture.Select(selection == SpeedLegSelection.SecondRiderAlone ? secondRider : mover);
            var selected = SelectionManager.Instance.SelectedUnits.ToArray();
            speedLeg["selectedIds"] = new JArray(selected.Select(u => u.UniqueId));
            Require(selected.Length >= 1 && (selection != SpeedLegSelection.Party || selected.Length >= 2),
                "Native selection did not take the intended units for the speed leg.");
            var candidates = Game.Instance.Player.ControllableCharacters.Where(u => u != null && u.IsInState && u.View?.AgentASP != null).ToArray();
            var before = candidates.ToDictionary(u => u, u => u.Commands.Move);
            var moverFacts = new JArray();
            foreach (var unit in candidates)
            {
                if (selected.Contains(unit) || ReferenceEquals(unit, mover) || world.Registry.FindActive(unit) != null)
                    moverFacts.Add(CaptureSpeedFacts(unit));
            }
            speedLeg["factsBeforeClick"] = moverFacts;
            speedLegStartedGame = Game.Instance.TimeController.GameTime;
            ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false);
            speedLegMovers = new List<SpeedLegMover>();
            foreach (var unit in candidates)
            {
                var move = unit.Commands.Move;
                if (move == null || ReferenceEquals(move, before[unit]) || !move.CreatedByPlayer) continue;
                if (!issuedCommands.Any(x => ReferenceEquals(x, move))) issuedCommands.Add(move);
                var owner = world.Registry.FindActive(unit);
                var role = owner != null && ReferenceEquals(owner.Mount, unit) ? "mount" : owner != null && ReferenceEquals(owner.Rider, unit) ? "rider" :
                    unit.Descriptor.IsPet ? "pet" : "on-foot";
                var record = new JObject { ["id"] = unit.UniqueId, ["role"] = role, ["commandObject"] = Id(move),
                    ["executorId"] = move.Executor?.UniqueId, ["speedLimitAtClick"] = move.SpeedLimit,
                    ["movementTypeAtClick"] = move.MovementType.ToString(), ["start"] = Point(unit.Position) };
                var speedMover = new SpeedLegMover { Unit = unit, Move = move, Record = record };
                // The at-rest baseline sample at the click: the motion interval can open no earlier than this.
                speedMover.Sampler.Observe(speedLegStartedGame.TotalSeconds, unit.Position.x, unit.Position.z);
                speedLegMovers.Add(speedMover);
            }
            speedLeg["movers"] = new JArray(speedLegMovers.Select(m => m.Record));
            // v3: every rider carried by a moving mount, observed from pair ownership; the rider's own Move slot is read, never written.
            speedLegCarried = new List<SpeedLegCarried>();
            foreach (var m in speedLegMovers)
            {
                var owner = world.Registry.FindActive(m.Unit);
                if (owner == null || !ReferenceEquals(owner.Mount, m.Unit) || owner.Rider == null || owner.Boundary.State != RelationshipState.Mounted) continue;
                var rider = owner.Rider;
                UnitMoveTo riderMoveBefore; before.TryGetValue(rider, out riderMoveBefore);
                var riderMove = rider.Commands.Move;
                var offset = Vector3.Distance(Flat(rider.Position), Flat(m.Unit.Position));
                var carried = new SpeedLegCarried { Rider = rider, Mount = m.Unit, Owner = owner, Generation = owner.Generation, RiderMoveAtClick = riderMove, MaximumOffset = offset,
                    Record = new JObject { ["riderId"] = rider.UniqueId, ["mountId"] = m.Unit.UniqueId, ["ownerId"] = owner.Id.ToString(), ["generation"] = owner.Generation,
                        ["relationshipState"] = owner.Boundary.State.ToString(), ["riderMoveCommandAtClick"] = Id(riderMove),
                        ["riderNewMoveCommand"] = riderMove != null && !ReferenceEquals(riderMove, riderMoveBefore) && riderMove.CreatedByPlayer,
                        ["riderAgentEnabled"] = rider.View?.AgentASP != null && rider.View.AgentASP.enabled,
                        ["start"] = Point(rider.Position), ["startOffsetToMount"] = offset } };
                carried.Sampler.Observe(speedLegStartedGame.TotalSeconds, rider.Position.x, rider.Position.z);
                speedLegCarried.Add(carried);
            }
            speedLeg["carried"] = new JArray(speedLegCarried.Select(c => c.Record));
            Require(speedLegMovers.Any(m => ReferenceEquals(m.Unit, mover)), "The native ground click did not move the speed leg's primary mover.");
            speedLeg["applicable"] = true;
            speedLegStartedWall = clock.Elapsed.TotalSeconds;
            Game.Instance.IsPaused = false;
        }

        private bool PollSpeedLeg()
        {
            Require(speedLeg != null && speedLegMovers != null, "No speed leg is running.");
            if (speedLegSkipped) { speedLeg = null; speedLegMovers = null; speedLegCarried = null; return true; }
            Require(clock.Elapsed.TotalSeconds - speedLegStartedWall <= SpeedLegWallSecondsLimit, "Speed leg exceeded its wall-clock bound: " + speedLeg["label"]);
            var now = Game.Instance.TimeController.GameTime;
            var allDone = true;
            foreach (var m in speedLegMovers)
            {
                var unit = m.Unit; var agent = unit.View?.AgentASP;
                m.Sampler.Observe(now.TotalSeconds, unit.Position.x, unit.Position.z);
                var agentSpeed = agent != null ? agent.Speed : 0f;
                m.Samples++;
                if (agentSpeed > 0.05f)
                {
                    m.MovingSamples++; m.AgentSpeedSum += agentSpeed; m.MaximumAgentSpeed = Math.Max(m.MaximumAgentSpeed, agentSpeed);
                    var entitySpeed = unit.CurrentSpeedMps; m.EntitySpeedSum += entitySpeed; m.MaximumEntitySpeed = Math.Max(m.MaximumEntitySpeed, entitySpeed);
                    var gait = unit.View?.AnimationManager != null ? unit.View.AnimationManager.WalkSpeedType.ToString() : "none";
                    m.Gaits[gait] = (m.Gaits.TryGetValue(gait, out var count) ? count : 0) + 1;
                }
                if (m.Move.IsAccelerated) m.Accelerated = true;
                m.FinalMovementType = m.Move.MovementType.ToString();
                if (!m.Move.IsFinished || agent == null || agent.IsReallyMoving) allDone = false;
            }
            foreach (var c in speedLegCarried)
            {
                c.Sampler.Observe(now.TotalSeconds, c.Rider.Position.x, c.Rider.Position.z);
                c.MaximumOffset = Math.Max(c.MaximumOffset, Vector3.Distance(Flat(c.Rider.Position), Flat(c.Mount.Position)));
                var owner = world.Registry.FindActive(c.Rider);
                if (owner == null || !ReferenceEquals(owner, c.Owner) || owner.Generation != c.Generation || !ReferenceEquals(owner.Mount, c.Mount) ||
                    owner.Boundary.State != RelationshipState.Mounted || !ReferenceEquals(world.Registry.FindActive(c.Mount), owner)) c.AttachedThroughout = false;
                var riderMove = c.Rider.Commands.Move;
                if (riderMove != null && !ReferenceEquals(riderMove, c.RiderMoveAtClick) && riderMove.CreatedByPlayer) c.Record["riderNewMoveCommand"] = true;
            }
            if (!allDone) return false;
            // v4: the native state when the last mover settled, recorded before any assertion (classifies an interrupt).
            speedLeg["endState"] = new JObject { ["gameMode"] = Game.Instance.CurrentMode.ToString(), ["paused"] = Game.Instance.IsPaused,
                ["inCombat"] = Game.Instance.Player.IsInCombat, ["loading"] = LoadingProcess.Instance.IsLoadingInProcess,
                ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuid, ["results"] = new JObject(speedLegMovers.Select(m => new JProperty(m.Unit.UniqueId, m.Move.Result.ToString()))) };
            foreach (var c in speedLegCarried)
            {
                var r = c.Record;
                r["end"] = Point(c.Rider.Position); r["endOffsetToMount"] = Vector3.Distance(Flat(c.Rider.Position), Flat(c.Mount.Position));
                r["maximumOffsetToMount"] = c.MaximumOffset; r["pathLength"] = c.Sampler.PathLength; r["attachedThroughout"] = c.AttachedThroughout;
                r["riderMoveCommandAtEnd"] = Id(c.Rider.Commands.Move); r["riderCommandsEmptyAtEnd"] = c.Rider.Commands.Empty;
            }
            var legGameSeconds = (now - speedLegStartedGame).TotalSeconds;
            speedLeg["gameSeconds"] = legGameSeconds;
            foreach (var m in speedLegMovers)
            {
                var r = m.Record;
                r["end"] = Point(m.Unit.Position); r["result"] = m.Move.Result.ToString();
                r["pathLength"] = m.Sampler.PathLength; r["motionGameSeconds"] = m.Sampler.MotionSeconds;
                r["effectiveSpeedMps"] = m.Sampler.EffectiveSpeed;
                // v2: the interval is aligned to the path (opened at the last at-rest sample); the v1 figure beside it.
                r["samplingContract"] = SpeedLegSampler.Contract;
                r["firstMotionGameSeconds"] = m.Sampler.Moved ? (JToken)(m.Sampler.FirstMotionSeconds.Value - speedLegStartedGame.TotalSeconds) : null;
                r["unalignedMotionGameSeconds"] = m.Sampler.UnalignedMotionSeconds;
                r["effectiveSpeedMpsUnaligned"] = m.Sampler.UnalignedEffectiveSpeed;
                r["samples"] = m.Samples; r["movingSamples"] = m.MovingSamples;
                r["meanAgentSpeedMps"] = m.MovingSamples > 0 ? m.AgentSpeedSum / m.MovingSamples : 0d;
                r["maximumAgentSpeedMps"] = m.MaximumAgentSpeed;
                r["meanEntitySpeedMps"] = m.MovingSamples > 0 ? m.EntitySpeedSum / m.MovingSamples : 0d;
                r["maximumEntitySpeedMps"] = m.MaximumEntitySpeed;
                r["gaits"] = new JObject(m.Gaits.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new JProperty(x.Key, x.Value)));
                r["accelerated"] = m.Accelerated; r["movementTypeAtEnd"] = m.FinalMovementType;
                r["goalResidual"] = Vector3.Distance(Flat(m.Unit.Position), Flat((Vector3)m.Move.Target));
                r["factsAfter"] = CaptureSpeedFacts(m.Unit);
            }
            var primary = speedLegMovers.First(m => (string)m.Record["id"] == (string)speedLeg["primaryMoverId"]);
            // v5: a failed leg is recorded and the sequence continues for diagnosis; the aggregate acceptance stays false
            // (RequireSpeedLegsAccepted at the end of the sequence) and the failed leg is never waived.
            // The meaningful length is half the admitted route length (r20: the engine still compared against the nine-metre constant).
            var admittedDistance = (float)speedLeg["distance"];
            var accepted = primary.Move.Result == UnitCommand.ResultType.Success && primary.Sampler.PathLength >= admittedDistance * 0.5f;
            speedLeg["accepted"] = accepted;
            speedLeg["interruptRecords"] = speedLegInterruptions == null ? 0 : speedLegInterruptions.Count(r => (string)r["legLabel"] == (string)speedLeg["label"]);
            if (!accepted)
            {
                var failure = "The speed leg's primary mover did not complete a native route of meaningful length: " + speedLeg["label"] +
                    " (result " + primary.Move.Result + ", path " + primary.Sampler.PathLength.ToString("F2") + " m, goal residual " +
                    ((float)primary.Record["goalResidual"]).ToString("F2") + " m).";
                speedLeg["failure"] = failure; speedLegFailures.Add(failure);
                logger.Warning("Sprint 2 speed leg " + speedLeg["label"] + " NOT accepted (later legs continue for diagnosis only): " + failure);
                // Later legs continue only on a quiet default surface: a leg ended by a dialog, cutscene, pause, load or
                // combat stops the sequence here (no input is issued inside such a mode).
                var es = (JObject)speedLeg["endState"];
                var quiet = (string)es["gameMode"] == "Default" && !(bool)es["paused"] && !(bool)es["inCombat"] && !(bool)es["loading"];
                if (!quiet) { speedLeg["continuationStopped"] = "the leg ended outside the quiet default mode: " + es.ToString(Newtonsoft.Json.Formatting.None); RequireSpeedLegsAccepted(); }
            }
            else
            {
                logger.Info("Sprint 2 speed leg " + speedLeg["label"] + ": primary effective " + ((double)primary.Record["effectiveSpeedMps"]).ToString("F2") +
                    " m/s (v1 interval " + primary.Sampler.UnalignedEffectiveSpeed.ToString("F2") + ") over " + primary.Sampler.PathLength.ToString("F2") +
                    " m; gaits=" + primary.Record["gaits"].ToString(Newtonsoft.Json.Formatting.None));
            }
            speedLeg = null; speedLegMovers = null; speedLegCarried = null;
            return true;
        }

        // v5: the aggregate acceptance of the leg sequence, decided once after the last leg. A failed leg fails the row
        // here with every retained failure; nothing is waived because later legs ran.
        private void RequireSpeedLegsAccepted()
        {
            var accepted = speedLegs != null && speedLegs.Count >= 1 && speedLegFailures.Count == 0;
            if (speedLegs != null) evidence["speedLegsAccepted"] = accepted;
            Require(accepted, speedLegs == null ? "No speed leg was recorded." :
                "Speed legs not accepted (" + speedLegFailures.Count + " failed leg(s); the later legs ran for diagnosis only): " + string.Join(" | ", speedLegFailures.ToArray()));
        }

        // v5: the first native UnitCommand.Interrupt of each issued speed-leg Move, observed before cleanup. Interrupts of
        // other commands, and every interrupt during cleanup, are counted by phase only (no stack). Observation only.
        private void ObserveNativeInterrupt(UnitCommand command)
        {
            if (command == null || speedLegInterruptSummary == null) return;
            var legLabel = speedLeg != null && speedLegMovers != null ? (string)speedLeg["label"] : null;
            var phase = cleanupStarted ? "cleanup" : legLabel != null ? "speed-leg" : "between-legs";
            var mover = cleanupStarted || speedLegMovers == null ? null : speedLegMovers.FirstOrDefault(m => ReferenceEquals(m.Move, command));
            // Only the FIRST interrupt of a mover's command is described; a repeated Interrupt on the same command is counted.
            if (mover != null && speedLegInterruptedCommands.Any(x => ReferenceEquals(x, command))) { mover = null; phase += ":repeat"; }
            else if (mover != null) speedLegInterruptedCommands.Add(command);
            var kind = mover != null ? "leg-mover" : issuedCommands.Any(x => ReferenceEquals(x, command)) ? "issued-command" : "other";
            var key = phase + ":" + kind;
            var count = speedLegInterruptSummary[key];
            speedLegInterruptSummary[key] = (count == null ? 0 : (int)count) + 1;
            if (mover == null) return;
            if (speedLegInterruptions.Count >= SpeedLegInterruptRecordLimit) { var t = speedLegInterruptSummary["truncated"]; speedLegInterruptSummary["truncated"] = (t == null ? 0 : (int)t) + 1; return; }
            try { speedLegInterruptions.Add(DescribeInterrupt(legLabel, mover, command)); }
            catch (Exception error)
            {
                var f = speedLegInterruptSummary["observerFailures"]; speedLegInterruptSummary["observerFailures"] = (f == null ? 0 : (int)f) + 1;
                speedLegInterruptions.Add(new JObject { ["legLabel"] = legLabel, ["moverId"] = mover.Unit.UniqueId, ["commandObject"] = Id(command), ["observerError"] = error.GetType().Name + ": " + error.Message });
            }
        }

        private JObject DescribeInterrupt(string legLabel, SpeedLegMover mover, UnitCommand command)
        {
            var unit = mover.Unit; var agent = unit.View?.AgentASP; var game = Game.Instance; var path = agent?.Path;
            bool truncated; var callers = SpeedLegInterruptObserver.Callers(40, out truncated);
            return new JObject { ["phase"] = "speed-leg", ["legLabel"] = legLabel, ["moverId"] = unit.UniqueId, ["role"] = mover.Record["role"], ["commandObject"] = Id(command),
                ["frame"] = Time.frameCount, ["gameTicks"] = game.TimeController.GameTime.Ticks,
                ["gameSecondsSinceClick"] = (game.TimeController.GameTime - speedLegStartedGame).TotalSeconds, ["wallSecondsSinceClick"] = clock.Elapsed.TotalSeconds - speedLegStartedWall,
                ["commandStarted"] = command.IsStarted, ["commandActed"] = command.IsActed, ["commandFinishedBefore"] = command.IsFinished, ["resultBefore"] = command.Result.ToString(),
                ["position"] = Point(unit.Position), ["target"] = Point(mover.Move.Target), ["flatDistanceToTarget"] = Vector3.Distance(Flat(unit.Position), Flat(mover.Move.Target)),
                ["pathLengthSoFar"] = mover.Sampler.PathLength,
                ["agent"] = agent == null ? null : new JObject { ["enabled"] = agent.enabled, ["isReallyMoving"] = agent.IsReallyMoving, ["wantsToMove"] = agent.WantsToMove,
                    ["pathFailed"] = agent.PathFailed, ["nearTheEnd"] = agent.NearTheEnd, ["speed"] = agent.Speed, ["hasPath"] = path != null,
                    ["pathError"] = path == null ? (JToken)null : path.error, ["pathDone"] = path == null ? (JToken)null : path.IsDone(),
                    ["pathVectorCount"] = path?.vectorPath == null ? (JToken)null : path.vectorPath.Count },
                ["gameMode"] = game.CurrentMode.ToString(), ["paused"] = game.IsPaused, ["inCombat"] = game.Player.IsInCombat, ["unitInCombat"] = unit.IsInCombat,
                ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["loading"] = LoadingProcess.Instance.IsLoadingInProcess, ["area"] = game.CurrentlyLoadedArea?.AssetGuid,
                ["unitCanMove"] = unit.Descriptor.State.CanMove, ["unitCanAct"] = unit.Descriptor.State.CanAct,
                ["callers"] = callers, ["callersTruncated"] = truncated, ["nearbyMapObjects"] = NearbyMapObjects(unit.Position, 8f, 6) };
        }

        // Bounded read-only neighbourhood: the nearest map-object views within the radius (name, view type, distance).
        private static JArray NearbyMapObjects(Vector3 origin, float radius, int limit)
        {
            var rows = new JArray();
            var views = UnityEngine.Object.FindObjectsOfType<MapObjectView>() ?? new MapObjectView[0];
            foreach (var view in views.Where(v => v != null).Select(v => new { View = v, Distance = Vector3.Distance(Flat(v.transform.position), Flat(origin)) })
                .Where(x => x.Distance <= radius).OrderBy(x => x.Distance).Take(limit))
            {
                rows.Add(new JObject { ["name"] = view.View.name, ["viewType"] = view.View.GetType().Name, ["distance"] = view.Distance,
                    ["position"] = Point(view.View.transform.position), ["interactionTypes"] = new JArray(view.View.GetComponents<InteractionComponent>().Select(c => c.GetType().Name)) });
            }
            return rows;
        }

        private void DisposeSpeedLegInterruptObserver()
        {
            var observer = speedLegInterruptObserver; speedLegInterruptObserver = null;
            if (observer == null) return;
            try { observer.Dispose(); if (speedLegInterruptSummary != null) speedLegInterruptSummary["observerRemoved"] = true; }
            catch (Exception error) { if (speedLegInterruptSummary != null) speedLegInterruptSummary["observerRemoveError"] = error.GetType().Name + ": " + error.Message; }
        }

        private IEnumerable<UnitEntityData> SelectionUnits(SpeedLegSelection selection, UnitEntityData mover)
        {
            switch (selection)
            {
                case SpeedLegSelection.Party:
                    foreach (var unit in Game.Instance.Player.ControllableCharacters)
                        if (unit != null && unit.IsInState && unit.IsDirectlyControllable && !unit.Descriptor.IsPet) yield return unit;
                    break;
                case SpeedLegSelection.SecondRiderAlone:
                    yield return secondRider; if (Mounted(secondRider, secondMount)) yield return secondMount; break;
                default:
                    yield return mover; break;
            }
        }

        // Read-only bounded search: twelve bearings around the preferred direction at the leg distance; a candidate
        // is admitted when its nearest native node is walkable within the clamp residual, the navmesh trace from the
        // mover reaches it, it keeps clear of the door's approach radius, and it keeps corpulence-sum clearance from
        // every unit outside the moving set. Every candidate is recorded before any assertion.
        private Vector3 SearchSpeedLegGoal(UnitEntityData mover, Vector3 direction, UnitEntityData[] movingSet, out bool found)
        {
            found = false; var goal = mover.Position; var start = mover.Position;
            var candidates = new JArray();
            var search = new JObject { ["start"] = Point(start), ["direction"] = Point(direction), ["distance"] = SpeedLegDistanceMetres,
                ["candidates"] = candidates, ["selectedIndex"] = -1 };
            speedLeg["goalSearch"] = search;
            var moverCorpulence = mover.View.AgentASP.Corpulence;
            var others = Game.Instance.State.Units.Where(u => u != null && u.IsInState && u.View?.AgentASP != null && !movingSet.Contains(u) &&
                Vector3.Distance(Flat(u.Position), Flat(start)) <= 30f).Take(256).ToArray();
            var doorPoint = doorProbe != null ? doorProbe.DoorPosition : null; var doorRadius = doorProbe != null ? doorProbe.DoorRadius : 0f;
            // v6: heights come from the navmesh node itself; the start's own node height is the level reference.
            var startInfo = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(start);
            var startNodeHeight = startInfo.node != null ? (float)startInfo.node.position.y * 0.001f : start.y;
            search["startNodeHeight"] = startNodeHeight; search["heightBasis"] = SpeedLegHeightBasis;
            // v6 (r19, r20): the hub surface is cramped. Every bearing is tried at the preferred nine metres first; only when
            // none is admitted is the whole sweep repeated at the shorter meaningful lengths (seven, then five metres), so a
            // long route on any bearing is preferred to a short one (r20 took a five-metre first bearing over r19's nine-metre
            // route). The admitted length becomes the leg's recorded distance. Each pass records the previous passes' reasons.
            var previousPasses = new JArray[SpeedLegBearingOffsets.Length];
            foreach (var distance in SpeedLegDistancesMetres)
            {
            candidates = new JArray(); search["candidates"] = candidates; search["distance"] = distance;
            for (var i = 0; i < SpeedLegBearingOffsets.Length && !found; i++)
            {
                var heading = Quaternion.Euler(0f, SpeedLegBearingOffsets[i], 0f) * direction;
                var requested = start + heading * distance;
                var nearest = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var candidate = new JObject { ["index"] = i, ["bearingOffset"] = SpeedLegBearingOffsets[i], ["requested"] = Point(requested), ["walkable"] = walkable,
                    ["distance"] = distance, ["previousPasses"] = previousPasses[i] ?? new JArray() };
                var reason = "no-walkable-native-node";
                if (walkable)
                {
                    // v6: the goal sits on the navmesh node's own height; the clamped position's height is the requested one.
                    var nodeHeight = (float)nearest.node.position.y * 0.001f;
                    var clamped = new Vector3(nearest.clampedPosition.x, nodeHeight, nearest.clampedPosition.z);
                    candidate["clampedHeight"] = nearest.clampedPosition.y; candidate["nodeHeight"] = nodeHeight;
                    var clampResidual = Vector3.Distance(Flat(requested), Flat(clamped));
                    var traceResidual = Vector3.Distance(Flat(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, clamped)), Flat(clamped));
                    var doorDistance = doorPoint.HasValue ? Vector3.Distance(Flat(clamped), Flat(doorPoint.Value)) : float.PositiveInfinity;
                    var slack = double.PositiveInfinity;
                    foreach (var other in others)
                        slack = Math.Min(slack, Vector3.Distance(Flat(clamped), Flat(other.Position)) - (moverCorpulence + other.View.AgentASP.Corpulence + 0.5f));
                    candidate["clamped"] = Point(clamped); candidate["clampResidual"] = clampResidual; candidate["traceResidual"] = traceResidual;
                    candidate["doorDistance"] = doorDistance; candidate["minimumSlack"] = slack;
                    // v4/v6: the navmesh height sampled every 1.5 m along the heading must stay level with the start (no stairs
                    // or ledge); v6 reads the node's own height (v4 read clampedPosition, which keeps the requested height).
                    var heights = new JArray(); var level = true;
                    for (var s = 1.5f; s <= distance + 0.01f; s += 1.5f)
                    {
                        var sampleInfo = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(start + heading * s);
                        var sampleWalkable = sampleInfo.node != null && sampleInfo.node.Walkable;
                        var sampleHeight = sampleWalkable ? (float)sampleInfo.node.position.y * 0.001f : float.NaN;
                        var rise = sampleWalkable ? sampleHeight - startNodeHeight : float.NaN;
                        heights.Add(new JObject { ["along"] = s, ["walkable"] = sampleWalkable, ["nodeHeight"] = sampleWalkable ? (JToken)sampleHeight : null, ["rise"] = sampleWalkable ? (JToken)rise : null });
                        if (!sampleWalkable || Math.Abs(rise) > SpeedLegLevelToleranceMetres) level = false;
                    }
                    candidate["heights"] = heights; candidate["level"] = level;
                    reason = clampResidual > SpeedLegClampResidualMetres ? "clamp-residual" : traceResidual > SpeedLegClampResidualMetres ? "trace-unreachable" :
                        doorDistance <= doorRadius + SpeedLegDoorClearanceMetres ? "inside-door-approach" : slack < 0d ? "blocked-by-unit" : !level ? "elevation-change" : "admitted";
                    if (reason == "admitted") { goal = clamped; found = true; speedLeg["distance"] = distance; }
                }
                candidate["reason"] = reason; candidates.Add(candidate);
                if (previousPasses[i] == null) previousPasses[i] = new JArray();
                previousPasses[i].Add(new JObject { ["distance"] = distance, ["reason"] = reason });
            }
            if (found) break;
            }
            search["selectedIndex"] = found ? candidates.Count - 1 : -1;
            return goal;
        }

        private static JObject CaptureSpeedFacts(UnitEntityData unit)
        {
            var state = unit.Descriptor.State; var view = unit.View; var move = unit.Commands.Move;
            return new JObject { ["id"] = unit.UniqueId, ["name"] = unit.CharacterName,
                ["blueprintSpeedFeet"] = unit.Blueprint.Speed.Value, ["blueprintSpeedMps"] = unit.Blueprint.Speed.Meters / 3f,
                ["statsSpeedFeet"] = unit.Stats.Speed.ModifiedValue, ["statsSpeedMps"] = unit.Stats.Speed.Mps,
                ["baseSpeedMps"] = unit.BaseSpeedMps, ["modifiedSpeedMps"] = unit.ModifiedSpeedMps, ["currentSpeedMps"] = unit.CurrentSpeedMps,
                ["speedIsAffected"] = unit.SpeedIsAffected, ["isInCombat"] = unit.IsInCombat, ["isPet"] = unit.Descriptor.IsPet,
                ["size"] = state.Size.ToString(), ["corpulence"] = view != null ? view.Corpulence : 0f,
                ["agentMaxSpeedMps"] = view?.AgentASP != null ? view.AgentASP.MaxSpeed : 0f,
                ["agentOverride"] = view?.AgentOverride != null, ["stockAgentEnabled"] = view?.AgentASP != null && view.AgentASP.enabled,
                ["difficultTerrain"] = state.HasCondition(UnitCondition.DifficultTerrain), ["slowed"] = state.HasCondition(UnitCondition.Slowed),
                ["entangled"] = state.HasCondition(UnitCondition.Entangled), ["staggered"] = state.HasCondition(UnitCondition.Staggered),
                ["stealth"] = state.IsInStealth, ["canMove"] = state.CanMove,
                ["gait"] = view?.AnimationManager != null ? view.AnimationManager.WalkSpeedType.ToString() : null,
                ["moveMovementType"] = move?.MovementType.ToString(), ["moveAccelerated"] = move != null && move.IsAccelerated,
                ["moveSpeedLimit"] = move?.SpeedLimit };
        }

        private static JObject CaptureSpeedEnvironment()
        {
            var player = Game.Instance.Player;
            return new JObject { ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuid, ["isCapital"] = Game.Instance.CurrentlyLoadedArea?.IsCapital,
                ["partyEncumbrance"] = player.Encumbrance.ToString(), ["encumbranceSlowdown"] = player.Difficulty.EncumbranceSlowdown,
                ["weather"] = player.Weather.ActualWeather.ToString(), ["acceleratedMoveSetting"] = SettingsRoot.Instance.AcceleratedMove.CurrentValue,
                ["gameMode"] = Game.Instance.CurrentMode.ToString(), ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
        }

        // Sprint 2 (SPACE-01): one end-of-frame capture of the first pair at the opened door, for the owner's
        // visual review of doorway clearance. It is a frame, not proof: the reader checks the file only exists.
        private void PumpDoorClearanceCapture()
        {
            if (doorProbe == null) return;
            if (!doorClearanceRequested && doorProbe.CompletedInteractions >= 1)
            {
                doorClearanceRequested = true;
                doorClearanceShots = new MovementScreenshotCaptureCoordinator((capture, bytes) => {
                    var folder = Path.Combine(request.EvidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
                    var file = Path.Combine(folder, "sprint2-door-clearance.png");
                    if (File.Exists(file)) throw new InvalidOperationException("Sprint 2 door clearance capture already exists.");
                    File.WriteAllBytes(file, bytes);
                    evidence["doorClearanceCapture"] = new JObject { ["file"] = "movement-visuals/sprint2-door-clearance.png", ["frame"] = Time.frameCount };
                }, (capture, error) => { evidence["doorClearanceCapture"] = new JObject { ["file"] = null, ["error"] = error }; }, logger);
                doorClearanceShots.Enqueue("C7-world-selection-routing", "door-clearance", Time.frameCount + 1);
            }
            doorClearanceShots?.Pump(Time.frameCount);
        }
    }

    // Sprint 2 (speed legs v5, review 2026-10-31): the run-scoped observer of native UnitCommand.Interrupt (the same exact
    // token the actor-allocation trace observes), installed before the first speed leg and removed with the engine. It
    // calls one engine callback and never changes the native result; nothing here interrupts, finishes or moves anything.
    internal sealed class SpeedLegInterruptObserver : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.SpeedLegInterrupts";
        private const int InterruptToken = 0x060027AC;
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Action<UnitCommand> active;
        private static SpeedLegInterruptObserver current;
        private readonly HarmonyInstance harmony;
        private readonly MethodInfo target;
        private bool disposed;

        internal SpeedLegInterruptObserver(Action<UnitCommand> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            // A stale observer (an engine whose disposal was interrupted) is removed before a new one is installed.
            var stale = current; if (stale != null) stale.Dispose();
            target = typeof(UnitCommand).GetMethods(Flags).SingleOrDefault(m => m.MetadataToken == InterruptToken);
            if (target == null || target.Name != "Interrupt") throw new MissingMethodException(typeof(UnitCommand).FullName, InterruptToken.ToString("X8"));
            active = observer; current = this;
            harmony = HarmonyInstance.Create(HarmonyId);
            harmony.Patch(target, new HarmonyMethod(typeof(SpeedLegInterruptObserver).GetMethod(nameof(Before), Flags)) { prioritiy = Priority.First });
        }

        internal JObject Describe(int recordLimit, JObject summary) => new JObject {
            ["scope"] = "first native UnitCommand.Interrupt of each issued speed-leg Move command, recorded before cleanup with its callers; interrupts of other commands and every cleanup-phase interrupt are counted by phase only",
            ["method"] = target.DeclaringType.FullName + "." + target.Name, ["token"] = InterruptToken.ToString("X8"), ["moduleMvid"] = target.Module.ModuleVersionId.ToString(),
            ["callerCapture"] = "one System.Diagnostics.StackTrace per recorded interrupt (method names only, no per-frame tracing)", ["recordLimit"] = recordLimit, ["counts"] = summary };

        private static void Before(UnitCommand __instance)
        {
            var observer = active;
            if (observer == null || __instance == null) return;
            try { observer(__instance); } catch (Exception) { }
        }

        // The native caller chain of the current interrupt (Harmony and this observer's own frames skipped).
        internal static string Callers(int maximum, out bool truncated)
        {
            truncated = true;
            try
            {
                var stack = new System.Diagnostics.StackTrace(1, false);
                var names = new List<string>(); truncated = false;
                for (var index = 0; index < stack.FrameCount; index++)
                {
                    var method = stack.GetFrame(index)?.GetMethod();
                    if (method == null) continue;
                    var name = (method.DeclaringType == null ? "" : method.DeclaringType.FullName + ".") + method.Name;
                    if (name.StartsWith("Harmony", StringComparison.Ordinal) || name.StartsWith("KingmakerMountedCombat.Diagnostics.", StringComparison.Ordinal)) continue;
                    if (names.Count == maximum) { truncated = true; break; }
                    names.Add(name);
                }
                return string.Join(" < ", names.ToArray());
            }
            catch (Exception exception) { truncated = true; return "unavailable:" + exception.GetType().Name; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ReferenceEquals(current, this)) { active = null; current = null; }
            harmony.UnpatchAll(HarmonyId);
        }
    }
}
