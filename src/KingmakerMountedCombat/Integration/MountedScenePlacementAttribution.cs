using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Known issues C02: the native scene placement operations write a unit's Position directly (TranslocateUnit.RunAction
    // 06008FA2, CommandMoveUnit.IsFinished 06009E4E when the move took too long, CommandTeleportParty.OnRun 06009E90). Around
    // each, every mounted pair's rider and mount positions are captured; a rider the operation wrote, with its mount left
    // where it was, is handed to the rider's synchronizer as the scene's own placement, whatever its distance from the
    // saddle. Nothing is written here; the existing scene-staging handoff decides what happens to the pair.
    internal static class MountedScenePlacementAttribution
    {
        internal sealed class Snapshot
        {
            internal GameMountedRelationshipService Owner;
            // Review R2: the mounting instance and its rider synchronizer the positions were captured for.
            internal MountedPair Instance;
            internal RiderMovementAgent Agent;
            internal Vector3 Rider, Mount;
            // Review R5: the installed operations also write orientation.
            internal float RiderOrientation, MountOrientation;
            // Review R8: whether the operation's proven target set holds the rider and the mount (CommandTeleportParty).
            internal bool RiderTargeted, MountTargeted;
        }

        // What the prefix hands the postfix: the exact operation and the captured pairs.
        internal sealed class Capture
        {
            internal MountedScenePlacementSource Source;
            internal List<Snapshot> Snapshots;
        }

        internal static long AttributedCount { get; private set; }

        // Null when no pair is mounted: the native operation then runs without any work here. targets: the operation's
        // proven target set (CommandTeleportParty), or null for a single-unit operation.
        internal static Capture Take(MountedScenePlacementSource source, IEnumerable<GameMountedRelationshipService> owners,
            ICollection<UnitEntityData> targets)
        {
            List<Snapshot> snapshots = null;
            foreach (var owner in owners)
            {
                if (owner == null || owner.State != RelationshipState.Mounted || owner.Rider == null || owner.Mount == null) continue;
                (snapshots = snapshots ?? new List<Snapshot>()).Add(new Snapshot { Owner = owner, Instance = owner.MountingInstance,
                    Agent = owner.Runtime?.MovementAgent, Rider = owner.Rider.Position, Mount = owner.Mount.Position,
                    RiderOrientation = owner.Rider.Orientation, MountOrientation = owner.Mount.Orientation,
                    RiderTargeted = targets != null && targets.Contains(owner.Rider), MountTargeted = targets != null && targets.Contains(owner.Mount) });
            }
            return snapshots == null ? null : new Capture { Source = source, Snapshots = snapshots };
        }

        // Review R8 of e1c0c7ad: the exact CommandTeleportParty target set, read as native reads it at OnRun
        // (Player.GetCharactersList(m_UnitsList), evaluated before the operation; its setters do not change the lists).
        internal static ICollection<UnitEntityData> TeleportPartyTargets(Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty command)
        {
            var player = Game.Instance?.Player;
            if (command == null || player == null || UnitsListField == null) return null;
            var list = (Player.CharactersList)UnitsListField.GetValue(command);
            return new HashSet<UnitEntityData>(player.GetCharactersList(list).Where(unit => unit != null));
        }

        private static readonly System.Reflection.FieldInfo UnitsListField =
            typeof(Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty).GetField("m_UnitsList",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        internal static void Attribute(Capture capture, IModLogger logger)
        {
            if (capture?.Snapshots == null) return;
            var game = Game.Instance;
            var sceneActive = game != null && MountedSceneContinuityPolicy.SceneActive(game.CutsceneLock,
                game.IsModeActive(GameModeType.Dialog), game.IsModeActive(GameModeType.Cutscene));
            var observations = new List<MountedScenePlacementObservation>(capture.Snapshots.Count);
            foreach (var snapshot in capture.Snapshots)
            {
                var owner = snapshot.Owner;
                var rider = owner.Rider; var mount = owner.Mount;
                if (rider == null || mount == null) continue;
                // Only the mounting instance (and its synchronizer) the positions were captured for: a pair that was
                // dismounted, or dismounted and mounted again, inside the operation is not attributed anything.
                var agent = owner.Runtime?.MovementAgent;
                if (snapshot.Instance == null || !ReferenceEquals(owner.MountingInstance, snapshot.Instance) || agent == null ||
                    !ReferenceEquals(agent, snapshot.Agent)) continue;
                Vector3 r = rider.Position, m = mount.Position;
                observations.Add(new MountedScenePlacementObservation
                {
                    Pair = snapshot, Hold = agent.ConfiguredSceneHold, PairMounted = owner.State == RelationshipState.Mounted,
                    RiderX0 = snapshot.Rider.x, RiderY0 = snapshot.Rider.y, RiderZ0 = snapshot.Rider.z, RiderOrientation0 = snapshot.RiderOrientation,
                    MountX0 = snapshot.Mount.x, MountY0 = snapshot.Mount.y, MountZ0 = snapshot.Mount.z, MountOrientation0 = snapshot.MountOrientation,
                    RiderX = r.x, RiderY = r.y, RiderZ = r.z, RiderOrientation = rider.Orientation,
                    MountX = m.x, MountY = m.y, MountZ = m.z, MountOrientation = mount.Orientation,
                    RiderTargeted = snapshot.RiderTargeted, MountTargeted = snapshot.MountTargeted
                });
            }
            // Review R9 of e1c0c7ad: the core applies every pair's own writes; a throwing logger never stops the rest.
            MountedScenePlacementAttributionCore.Apply(observations, sceneActive, Time.frameCount, (o, write, change) =>
            {
                AttributedCount++;
                var pair = (Snapshot)o.Pair;
                logger?.Info("Scene placement of a seated rider: " + Describe(capture.Source) + " " + write + " -> " + change +
                    ": rider=" + pair.Owner.Rider?.UniqueId + "; mount=" + pair.Owner.Mount?.UniqueId + "; target=(" + o.RiderX.ToString("0.00") + ", " +
                    o.RiderZ.ToString("0.00") + "); orientation=" + o.RiderOrientation.ToString("0.0") + "; targeted rider=" + o.RiderTargeted +
                    ", mount=" + o.MountTargeted + ".");
            });
        }

        internal static string Describe(MountedScenePlacementSource source)
        {
            switch (source)
            {
                case MountedScenePlacementSource.CommandMoveUnitTimedOut: return "CommandMoveUnit (timed out)";
                case MountedScenePlacementSource.CommandTeleportParty: return "CommandTeleportParty";
                default: return "TranslocateUnit";
            }
        }
    }
}
