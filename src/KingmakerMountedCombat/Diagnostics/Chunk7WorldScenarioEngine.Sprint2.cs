using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
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
        internal const string SpeedLegContract = "sprint2-speed-legs/v2";
        internal const float SpeedLegDistanceMetres = 9f;
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

        private JArray speedLegs;
        private JObject speedLeg;
        private List<SpeedLegMover> speedLegMovers;
        private double speedLegStartedWall;
        private TimeSpan speedLegStartedGame;
        private Vector3 speedLegStart, speedLegGoal, speedLegPreviousStart;
        private bool speedLegSkipped;
        private MovementScreenshotCaptureCoordinator doorClearanceShots;
        private bool doorClearanceRequested;

        private void BeginSpeedLeg(string label, SpeedLegSelection selection)
        {
            if (speedLegs == null) { speedLegs = new JArray(); evidence["speedLegContract"] = SpeedLegContract; evidence["speedLegs"] = speedLegs; }
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
                speedLegSkipped = true; speedLegMovers = new List<SpeedLegMover>();
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
            Require(speedLegMovers.Any(m => ReferenceEquals(m.Unit, mover)), "The native ground click did not move the speed leg's primary mover.");
            speedLeg["applicable"] = true;
            speedLegStartedWall = clock.Elapsed.TotalSeconds;
            Game.Instance.IsPaused = false;
        }

        private bool PollSpeedLeg()
        {
            Require(speedLeg != null && speedLegMovers != null, "No speed leg is running.");
            if (speedLegSkipped) { speedLeg = null; speedLegMovers = null; return true; }
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
            if (!allDone) return false;
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
            Require(primary.Move.Result == UnitCommand.ResultType.Success && primary.Sampler.PathLength >= SpeedLegDistanceMetres * 0.5f,
                "The speed leg's primary mover did not complete a native route of meaningful length: " + speedLeg["label"]);
            logger.Info("Sprint 2 speed leg " + speedLeg["label"] + ": primary effective " + ((double)primary.Record["effectiveSpeedMps"]).ToString("F2") +
                " m/s (v1 interval " + primary.Sampler.UnalignedEffectiveSpeed.ToString("F2") + ") over " + primary.Sampler.PathLength.ToString("F2") +
                " m; gaits=" + primary.Record["gaits"].ToString(Newtonsoft.Json.Formatting.None));
            speedLeg = null; speedLegMovers = null;
            return true;
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
            for (var i = 0; i < SpeedLegBearingOffsets.Length && !found; i++)
            {
                var heading = Quaternion.Euler(0f, SpeedLegBearingOffsets[i], 0f) * direction;
                var requested = start + heading * SpeedLegDistanceMetres;
                var nearest = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var candidate = new JObject { ["index"] = i, ["bearingOffset"] = SpeedLegBearingOffsets[i], ["requested"] = Point(requested), ["walkable"] = walkable };
                var reason = "no-walkable-native-node";
                if (walkable)
                {
                    var clamped = nearest.clampedPosition;
                    var clampResidual = Vector3.Distance(Flat(requested), Flat(clamped));
                    var traceResidual = Vector3.Distance(Flat(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, clamped)), Flat(clamped));
                    var doorDistance = doorPoint.HasValue ? Vector3.Distance(Flat(clamped), Flat(doorPoint.Value)) : float.PositiveInfinity;
                    var slack = double.PositiveInfinity;
                    foreach (var other in others)
                        slack = Math.Min(slack, Vector3.Distance(Flat(clamped), Flat(other.Position)) - (moverCorpulence + other.View.AgentASP.Corpulence + 0.5f));
                    candidate["clamped"] = Point(clamped); candidate["clampResidual"] = clampResidual; candidate["traceResidual"] = traceResidual;
                    candidate["doorDistance"] = doorDistance; candidate["minimumSlack"] = slack;
                    reason = clampResidual > SpeedLegClampResidualMetres ? "clamp-residual" : traceResidual > SpeedLegClampResidualMetres ? "trace-unreachable" :
                        doorDistance <= doorRadius + SpeedLegDoorClearanceMetres ? "inside-door-approach" : slack < 0d ? "blocked-by-unit" : "admitted";
                    if (reason == "admitted") { goal = clamped; found = true; }
                }
                candidate["reason"] = reason; candidates.Add(candidate);
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
}
