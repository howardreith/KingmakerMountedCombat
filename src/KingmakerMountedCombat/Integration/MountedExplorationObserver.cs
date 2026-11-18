using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Exploration repair M1/S2 (diagnostics only). The owner reported a stale green destination marker and a horse
    // that falls behind during ordinary group travel; static reading of the installed contracts did not establish a
    // cause, so these keyed, low-volume observations are written to the game log for the next real session:
    //   - a mounted rider holding its own live move command (it can never execute: the rider's stock agent is off);
    //   - the ridden mount stopped short of a marked destination (what keeps its marker visible);
    //   - one pace summary per mount group order: limits, speeds, pauses and separation from the party.
    // Horse speed repair: the limit, speeds and gait are read when the leg is first sampled, which can precede the
    // native path completion that accelerates a long group move. The summary therefore also reports whether the mount's
    // order and the ordinary party members' orders were accelerated at any sample, their highest limits, the mount's
    // limit and gait at the end, and the selection the order came from. Times are unscaled seconds (pauses included).
    // It reads only; it writes no command, position, agent, marker or native state.
    // Known issues M04 closeout: in Default mode (exploration and real-time combat) each mounted pair's destination
    // pointers are classified (MountedMarkerOwnershipPolicy) at the same 4 Hz, and only an anomaly transition is written:
    // once per pair, generation, class and the identities of the commands that keep the pointers visible. Every
    // pointer-table entry of the rider and the mount is in that line, with the commands and the last recorded placement.
    // A failing observation or logger of one pair never stops another, and nothing is retained across a boundary.
    internal sealed class MountedExplorationObserver
    {
        private const float StallSeconds = 2.5f;
        private const float SampleSeconds = 0.25f;
        private readonly MountedWorldService world;
        private readonly IModLogger logger;
        private readonly Dictionary<GameMountedRelationshipService, Leg> legs = new Dictionary<GameMountedRelationshipService, Leg>();
        private readonly HashSet<UnitCommand> reported = new HashSet<UnitCommand>();
        private float nextSample;

        internal MountedExplorationObserver(MountedWorldService world, IModLogger logger)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal void Update()
        {
            try { Observe(); }
            catch (Exception error)
            {
                // A diagnostic must never affect play; drop its state and stay quiet.
                legs.Clear(); reported.Clear(); started.Clear(); ClearMarkers();
                try { logger.Exception("Mounted exploration observer stopped one sample", error); } catch (Exception) { }
            }
        }

        // Disable and disposal: no unit, command or placement survives.
        internal void Reset()
        {
            legs.Clear(); reported.Clear(); started.Clear(); ClearMarkers();
            MountedDestinationMarkers.ResetPlacements();
            MountedNativeUnitClickScope.Close();
        }

        private void Observe()
        {
            var game = Game.Instance;
            var now = Time.unscaledTime;
            if (now < nextSample) return;
            nextSample = now + SampleSeconds;
            if (game?.Player == null || game.CurrentMode != GameModeType.Default ||
                LoadingProcess.Instance == null || LoadingProcess.Instance.IsLoadingInProcess)
            {
                // Review finding 7: no command or unit reference outlives ordinary play of this world.
                legs.Clear(); reported.Clear(); started.Clear(); ClearMarkers();
                return;
            }
            var exploration = !game.Player.IsInCombat;
            if (!exploration) { legs.Clear(); reported.Clear(); started.Clear(); }
            var live = new HashSet<GameMountedRelationshipService>();
            var qualifying = new HashSet<UnitCommand>();
            foreach (var pair in world.Pairs)
            {
                var relationship = pair.Relationship;
                if (relationship.State != RelationshipState.Mounted || relationship.Rider == null || relationship.Mount == null) continue;
                live.Add(relationship);
                try { ObserveMarkers(relationship, game, now, qualifying); }
                catch (Exception error)
                {
                    markerReported.Remove(relationship);
                    try { logger.Exception("Mounted marker observation skipped one pair", error); } catch (Exception) { }
                }
                if (!exploration) continue;
                ObserveRiderCommands(relationship.Rider, relationship.Mount, now);
                ObserveMountLeg(relationship, game, now);
            }
            foreach (var stale in legs.Keys.Where(k => !live.Contains(k)).ToArray()) legs.Remove(stale);
            foreach (var stale in markerReported.Keys.Where(k => !live.Contains(k)).ToArray()) markerReported.Remove(stale);
            foreach (var gone in markerSeen.Keys.Where(c => !qualifying.Contains(c)).ToArray()) markerSeen.Remove(gone);
            if (reported.Count > 256) reported.Clear();
        }

        // ---- known issues M04 closeout: destination-marker ownership --------------------------------------------------
        private readonly Dictionary<UnitCommand, float> markerSeen = new Dictionary<UnitCommand, float>();
        private readonly Dictionary<GameMountedRelationshipService, string> markerReported = new Dictionary<GameMountedRelationshipService, string>();
        internal long MarkerAnomalyReportCount { get; private set; }

        private void ClearMarkers()
        {
            markerSeen.Clear();
            markerReported.Clear();
        }

        private void ObserveMarkers(GameMountedRelationshipService relationship, Game game, float now, HashSet<UnitCommand> qualifying)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var moving = mount.View?.MovementAgent != null && mount.View.MovementAgent.IsReallyMoving;
            var riderCommand = MountedDestinationMarkers.QualifyingCommand(rider);
            var mountCommand = MountedDestinationMarkers.QualifyingCommand(mount);
            var riderFacts = Facts(rider, riderCommand, now, moving, qualifying, out var riderText);
            var mountFacts = Facts(mount, mountCommand, now, moving, qualifying, out var mountText);
            var pairClass = MountedMarkerOwnershipPolicy.ClassifyPair(true, riderFacts, mountFacts);
            if (!MountedMarkerOwnershipPolicy.IsAnomaly(pairClass))
            {
                markerReported.Remove(relationship);
                return;
            }
            var key = relationship.MountedPairGeneration + ":" + pairClass + ":" + Identity(riderCommand) + ":" + Identity(mountCommand);
            if (markerReported.TryGetValue(relationship, out var last) && last == key) return;
            markerReported[relationship] = key;
            MarkerAnomalyReportCount++;
            var selected = game.UI?.SelectionManager?.SelectedUnits;
            try
            {
                logger.Info("Mounted marker observation: " + pairClass + "; disposition=" + Disposition(pairClass, riderCommand) +
                    "; riderId=" + rider.UniqueId + "; mountId=" + mount.UniqueId + "; generation=" + relationship.MountedPairGeneration +
                    "; rider={" + riderText + ";class=" + MountedMarkerOwnershipPolicy.ClassifyUnit(riderFacts) + "}; mount={" + mountText +
                    ";class=" + MountedMarkerOwnershipPolicy.ClassifyUnit(mountFacts) + "}; area=" + (game.CurrentlyLoadedArea?.name ?? "none") +
                    "; mode=" + game.CurrentMode + "; combat=" + game.Player.IsInCombat + "; selected=" +
                    (selected == null ? "unknown" : (selected.Contains(rider) ? "rider" : "") + (selected.Contains(mount) ? "+mount" : "") + "/" + selected.Count) +
                    "; mountMoving=" + moving + ".");
            }
            catch (Exception) { }
        }

        // One unit: its pointer-table entry, the command the native rule shows it for, and the last placement recorded.
        private MountedMarkerFacts Facts(UnitEntityData unit, UnitCommand command, float now, bool moving, HashSet<UnitCommand> qualifying, out string text)
        {
            var marks = Kingmaker.UI.ClickPointerManager.Instance?.UnitMarks;
            Kingmaker.UI.ClickPointerPrefab pointer = null;
            var entry = marks != null && marks.TryGetValue(unit, out pointer);
            var alive = entry && pointer != null;
            if (command != null)
            {
                qualifying.Add(command);
                if (!markerSeen.ContainsKey(command)) markerSeen[command] = now;
            }
            var seen = command != null ? now - markerSeen[command] : 0f;
            var placed = MountedDestinationMarkers.PlacedFor(unit, command, out var frame);
            var move = command as UnitMoveTo;
            var facts = new MountedMarkerFacts
            {
                PointerExists = alive,
                PointerVisible = alive && pointer.IsVisible,
                QualifyingCommandLive = command != null,
                QualifyingCommandUnordered = command != null && !command.CreatedByPlayer && command.AiAction == null && !command.Cutscene,
                PlacedForCurrentCommand = placed,
                SecondsSinceCommandSeen = seen,
                Moving = moving
            };
            text = "entry=" + entry + ";destroyed=" + (entry && !alive) + ";visible=" + facts.PointerVisible +
                ";at=" + (alive ? Format(pointer.transform.position) : "none") + ";command=" + (command == null ? "none" :
                command.GetType().Name + "#" + Identity(command) + ";executor=" + command.Executor?.UniqueId + ";target=" +
                Format(command.Target?.Point ?? Vector3.zero) + ";finished=" + command.IsFinished + ";createdByPlayer=" + command.CreatedByPlayer +
                ";marker=" + (move?.ShowTargetMarker ?? true) + ";queued=" + unit.Commands.Queue.Contains(command) + ";cutscene=" + command.Cutscene +
                ";ai=" + (command.AiAction != null)) + ";placedForCommand=" + placed + ";placementFrame=" + frame + ";seenSeconds=" + Metres(seen);
            return facts;
        }

        private static string Identity(UnitCommand command) =>
            command == null ? "none" : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(command).ToString("x", CultureInfo.InvariantCulture);

        // Where each anomalous class stands after the closeout (the classification is diagnostic; it changes nothing).
        private static string Disposition(MountedMarkerClass value, UnitCommand riderCommand)
        {
            switch (value)
            {
                case MountedMarkerClass.DuplicatePairMarkers:
                case MountedMarkerClass.RiderHeldWhileMountMoves:
                    return riderCommand != null && riderCommand.Cutscene ? "native: a scene's own command on the seated rider (C01 limit)" :
                        riderCommand is UnitAreaTransition ? "repaired path expected (carried entrance); unresolved if seen" :
                        "unresolved: a rider-held order outside the repaired paths";
                case MountedMarkerClass.InternalMoveRequestsMarker:
                    return "unresolved: a move no player ordered asks for a marker";
                default:
                    return "unresolved";
            }
        }

        private void ObserveRiderCommands(UnitEntityData rider, UnitEntityData mount, float now)
        {
            var commands = rider.Commands;
            if (commands == null) return;
            var candidates = new List<UnitCommand>();
            if (commands.Move != null) candidates.Add(commands.Move);
            candidates.AddRange(commands.Queue.OfType<UnitMoveTo>());
            var transition = commands.GetCommand<UnitAreaTransition>();
            if (transition != null) candidates.Add(transition);
            foreach (var command in candidates)
            {
                if (command == null || command.IsFinished || reported.Contains(command)) continue;
                var move = command as UnitMoveTo;
                if (move != null && !move.ShowTargetMarker && !(command is UnitAreaTransition)) continue;
                if (!started.TryGetValue(command, out var first)) { started[command] = now; continue; }
                if (now - first < StallSeconds) continue;
                reported.Add(command);
                started.Remove(command);
                logger.Info("Mounted exploration observation: rider-held move command while mounted; riderId=" + rider.UniqueId +
                    "; mountId=" + mount.UniqueId + "; command=" + command.GetType().Name + "; createdByPlayer=" + command.CreatedByPlayer +
                    "; marker=" + (move?.ShowTargetMarker ?? true) + "; queued=" + commands.Queue.Contains(command) +
                    "; target=" + Format(command.Target?.Point ?? Vector3.zero) + "; riderToTarget=" +
                    Metres(Distance2D(rider.Position, command.Target?.Point ?? rider.Position)) + "; ageSeconds=" + Metres(now - first) + ".");
            }
            foreach (var gone in started.Keys.Where(c => c.Executor == rider && (c.IsFinished || !candidates.Contains(c))).ToArray()) started.Remove(gone);
        }

        private readonly Dictionary<UnitCommand, float> started = new Dictionary<UnitCommand, float>();

        private void ObserveMountLeg(GameMountedRelationshipService relationship, Game game, float now)
        {
            var mount = relationship.Mount;
            var move = mount.Commands?.Move;
            legs.TryGetValue(relationship, out var leg);
            if (leg != null && !ReferenceEquals(leg.Command, move))
            {
                Summarise(relationship, leg, now);
                legs.Remove(relationship);
                leg = null;
            }
            if (move == null || move.IsFinished || !move.CreatedByPlayer || move.GetType() != typeof(UnitMoveTo)) return;
            var agent = mount.View?.AgentASP;
            var position = mount.Position;
            if (leg == null)
            {
                leg = new Leg { Command = move, StartTime = now, StartPosition = position, LastPosition = position, Target = move.Target,
                    SpeedLimit = move.SpeedLimit, ModifiedSpeed = mount.ModifiedSpeedMps, CurrentSpeed = mount.CurrentSpeedMps,
                    AgentMaxSpeed = agent != null ? agent.MaxSpeed : -1f, Corpulence = mount.View != null ? mount.View.Corpulence : -1f,
                    Party = PartyMovers(game, relationship), Selection = SelectionSummary(game, relationship) };
                SampleOrders(leg, move, agent);
                legs[relationship] = leg;
                return;
            }
            SampleOrders(leg, move, agent);
            var step = Distance2D(position, leg.LastPosition);
            leg.Travelled += step;
            leg.LastPosition = position;
            leg.Samples++;
            if (step < 0.02f) { leg.StillSamples++; leg.StillSince = leg.StillSince < 0f ? now : leg.StillSince; }
            else leg.StillSince = -1f;
            var centroid = Centroid(leg.Party);
            if (centroid.HasValue) leg.MaxBehind = Mathf.Max(leg.MaxBehind, Distance2D(position, centroid.Value));
            var remaining = Distance2D(position, leg.Target);
            if (leg.StillSince >= 0f && now - leg.StillSince >= StallSeconds && !leg.StallReported && remaining > move.ApproachRadius + 0.05f)
            {
                leg.StallReported = true;
                var nearest = NearestOther(game, relationship, leg.Target);
                logger.Info("Mounted exploration observation: mount stopped short of its marked destination; mountId=" + mount.UniqueId +
                    "; riderId=" + relationship.Rider.UniqueId + "; remaining=" + Metres(remaining) + "; approachRadius=" + Metres(move.ApproachRadius) +
                    "; mountCorpulence=" + Metres(leg.Corpulence) + "; riderCorpulence=" + Metres(relationship.Rider.View != null ? relationship.Rider.View.Corpulence : -1f) +
                    "; nearestOther=" + (nearest == null ? "<none>" : nearest.CharacterName + "/" + nearest.UniqueId + " at " +
                        Metres(Distance2D(nearest.Position, leg.Target)) + " m from the target, moveFinished=" + (nearest.Commands?.Move == null || nearest.Commands.Move.IsFinished)) +
                    "; agentMoving=" + (mount.View?.AgentASP != null && mount.View.AgentASP.IsReallyMoving) + ".");
            }
        }

        private void Summarise(GameMountedRelationshipService relationship, Leg leg, float now)
        {
            if (leg.Samples < 4) return;
            var elapsed = Mathf.Max(0.001f, now - leg.StartTime);
            var partySpeeds = leg.Party.Where(p => p.Unit != null).Select(p => Distance2D(p.Unit.Position, p.Start) / elapsed).ToArray();
            logger.Info("Mounted exploration observation: mount group-order pace; mountId=" + relationship.Mount.UniqueId +
                "; speedLimit=" + (leg.SpeedLimit.HasValue ? Metres(leg.SpeedLimit.Value) : "none") + "; modifiedSpeed=" + Metres(leg.ModifiedSpeed) +
                "; currentSpeed=" + Metres(leg.CurrentSpeed) + "; agentMaxSpeed=" + Metres(leg.AgentMaxSpeed) +
                "; straight=" + Metres(Distance2D(leg.StartPosition, leg.Target)) + "; travelled=" + Metres(leg.Travelled) +
                "; elapsed=" + Metres(elapsed) + "; meanSpeed=" + Metres(leg.Travelled / elapsed) +
                "; stillFraction=" + Metres(leg.StillSamples / (float)leg.Samples) + "; maxFromPartyCentroid=" + Metres(leg.MaxBehind) +
                "; partyMeanDisplacementSpeed=" + (partySpeeds.Length == 0 ? "n/a" : Metres(partySpeeds.Average())) +
                "; endRemaining=" + Metres(Distance2D(relationship.Mount.Position, leg.Target)) + "; stalled=" + leg.StallReported +
                "; selection=" + leg.Selection + "; mountAccelerated=" + leg.Accelerated + "; speedLimitMax=" + Limit(leg.MaxSpeedLimit) +
                "; speedLimitAtEnd=" + Limit(leg.EndSpeedLimit) + "; gaitAtEnd=" + leg.EndGait + "; agentMaxSpeedMax=" + Metres(leg.MaxAgentSpeed) +
                "; partyOrders=" + leg.Party.Count(p => p.Ordered) + "/" + leg.Party.Length + "; partyAccelerated=" + leg.Party.Count(p => p.Accelerated) +
                "; partySpeedLimitMax=" + Limit(leg.Party.Where(p => p.MaxSpeedLimit.HasValue).Select(p => p.MaxSpeedLimit).Max()) + ".");
        }

        // The mount's routed order and each ordinary party member's own player move at this sample.
        private static void SampleOrders(Leg leg, UnitMoveTo move, Kingmaker.View.UnitMovementAgentBase agent)
        {
            leg.Accelerated |= move.IsAccelerated;
            leg.MaxSpeedLimit = Higher(leg.MaxSpeedLimit, move.SpeedLimit);
            leg.EndSpeedLimit = move.SpeedLimit;
            leg.EndGait = move.MovementType.ToString();
            if (agent != null) leg.MaxAgentSpeed = Mathf.Max(leg.MaxAgentSpeed, agent.MaxSpeed);
            foreach (var member in leg.Party)
            {
                var order = member.Unit?.Commands?.Move;
                if (order == null || order.IsFinished || !order.CreatedByPlayer) continue;
                member.Ordered = true;
                member.Accelerated |= order.IsAccelerated;
                member.MaxSpeedLimit = Higher(member.MaxSpeedLimit, order.SpeedLimit);
            }
        }

        private static float? Higher(float? a, float? b) => !a.HasValue ? b : !b.HasValue ? a : Mathf.Max(a.Value, b.Value);

        private static string Limit(float? value) => value.HasValue ? Metres(value.Value) : "none";

        private static string SelectionSummary(Game game, GameMountedRelationshipService relationship)
        {
            var selected = game.UI?.SelectionManager?.SelectedUnits;
            if (selected == null) return "unknown";
            return selected.Count + "(rider=" + selected.Contains(relationship.Rider) + ",mount=" + selected.Contains(relationship.Mount) + ")";
        }

        private static PartyMover[] PartyMovers(Game game, GameMountedRelationshipService relationship) =>
            game.Player.Party.Where(u => u != null && u != relationship.Rider && u != relationship.Mount)
                .Select(u => new PartyMover { Unit = u, Start = u.Position }).ToArray();

        private static UnitEntityData NearestOther(Game game, GameMountedRelationshipService relationship, Vector3 point) =>
            game.Player.ControllableCharacters.Where(u => u != null && u != relationship.Rider && u != relationship.Mount)
                .OrderBy(u => Distance2D(u.Position, point)).FirstOrDefault();

        private static Vector3? Centroid(PartyMover[] party)
        {
            var units = party.Where(p => p.Unit != null).ToArray();
            if (units.Length == 0) return null;
            var sum = Vector3.zero;
            foreach (var p in units) sum += p.Unit.Position;
            return sum / units.Length;
        }

        private static float Distance2D(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        private static string Metres(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
        private static string Format(Vector3 v) => "(" + Metres(v.x) + "," + Metres(v.y) + "," + Metres(v.z) + ")";

        private sealed class PartyMover
        {
            internal UnitEntityData Unit; internal Vector3 Start;
            internal bool Ordered, Accelerated; internal float? MaxSpeedLimit;
        }

        private sealed class Leg
        {
            internal UnitCommand Command;
            internal float StartTime, ModifiedSpeed, CurrentSpeed, AgentMaxSpeed, Corpulence, Travelled, MaxBehind, StillSince = -1f;
            internal float? SpeedLimit, MaxSpeedLimit, EndSpeedLimit;
            internal float MaxAgentSpeed;
            internal bool Accelerated;
            internal string EndGait = "unknown", Selection = "unknown";
            internal Vector3 StartPosition, LastPosition, Target;
            internal int Samples, StillSamples;
            internal bool StallReported;
            internal PartyMover[] Party;
        }
    }
}
