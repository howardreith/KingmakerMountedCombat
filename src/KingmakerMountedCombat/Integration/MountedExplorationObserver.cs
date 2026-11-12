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
    // It reads only; it writes no command, position, agent, marker or native state.
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
                legs.Clear(); reported.Clear(); started.Clear();
                logger.Exception("Mounted exploration observer stopped one sample", error);
            }
        }

        private void Observe()
        {
            var game = Game.Instance;
            var now = Time.unscaledTime;
            if (now < nextSample) return;
            nextSample = now + SampleSeconds;
            if (game?.Player == null || game.CurrentMode != GameModeType.Default || game.Player.IsInCombat ||
                LoadingProcess.Instance == null || LoadingProcess.Instance.IsLoadingInProcess)
            {
                // Review finding 7: no command or unit reference outlives ordinary play of this world.
                legs.Clear(); reported.Clear(); started.Clear();
                return;
            }
            var live = new HashSet<GameMountedRelationshipService>();
            foreach (var pair in world.Pairs)
            {
                var relationship = pair.Relationship;
                if (relationship.State != RelationshipState.Mounted || relationship.Rider == null || relationship.Mount == null) continue;
                live.Add(relationship);
                ObserveRiderCommands(relationship.Rider, relationship.Mount, now);
                ObserveMountLeg(relationship, game, now);
            }
            foreach (var stale in legs.Keys.Where(k => !live.Contains(k)).ToArray()) legs.Remove(stale);
            if (reported.Count > 256) reported.Clear();
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
                    Party = PartyMovers(game, relationship) };
                legs[relationship] = leg;
                return;
            }
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
                "; endRemaining=" + Metres(Distance2D(relationship.Mount.Position, leg.Target)) + "; stalled=" + leg.StallReported + ".");
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

        private sealed class PartyMover { internal UnitEntityData Unit; internal Vector3 Start; }

        private sealed class Leg
        {
            internal UnitCommand Command;
            internal float StartTime, ModifiedSpeed, CurrentSpeed, AgentMaxSpeed, Corpulence, Travelled, MaxBehind, StillSince = -1f;
            internal float? SpeedLimit;
            internal Vector3 StartPosition, LastPosition, Target;
            internal int Samples, StillSamples;
            internal bool StallReported;
            internal PartyMover[] Party;
        }
    }
}
