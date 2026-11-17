using System.Collections.Generic;
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
        }

        internal static long AttributedCount { get; private set; }

        // Null when no pair is mounted: the native operation then runs without any work here.
        internal static List<Snapshot> Capture(IEnumerable<GameMountedRelationshipService> owners)
        {
            List<Snapshot> snapshots = null;
            foreach (var owner in owners)
            {
                if (owner == null || owner.State != RelationshipState.Mounted || owner.Rider == null || owner.Mount == null) continue;
                (snapshots = snapshots ?? new List<Snapshot>()).Add(new Snapshot { Owner = owner, Instance = owner.MountingInstance,
                    Agent = owner.Runtime?.MovementAgent, Rider = owner.Rider.Position, Mount = owner.Mount.Position,
                    RiderOrientation = owner.Rider.Orientation, MountOrientation = owner.Mount.Orientation });
            }
            return snapshots;
        }

        internal static void Attribute(List<Snapshot> snapshots, string operation, IModLogger logger)
        {
            if (snapshots == null) return;
            var game = Game.Instance;
            var sceneActive = game != null && MountedSceneContinuityPolicy.SceneActive(game.CutsceneLock,
                game.IsModeActive(GameModeType.Dialog), game.IsModeActive(GameModeType.Cutscene));
            foreach (var snapshot in snapshots)
            {
                var owner = snapshot.Owner;
                var rider = owner.Rider; var mount = owner.Mount;
                if (rider == null || mount == null) continue;
                // Only the mounting instance (and its synchronizer) the positions were captured for: a pair that was
                // dismounted, or dismounted and mounted again, inside the operation is not attributed anything.
                var agent = owner.Runtime?.MovementAgent;
                if (snapshot.Instance == null || !ReferenceEquals(owner.MountingInstance, snapshot.Instance) || agent == null ||
                    !ReferenceEquals(agent, snapshot.Agent)) continue;
                var r = rider.Position - snapshot.Rider; var m = mount.Position - snapshot.Mount;
                var write = MountedSceneContinuityPolicy.ClassifyOperationWrite(owner.State == RelationshipState.Mounted, sceneActive,
                    MountedSceneContinuityPolicy.PositionWritten(r.x, r.y, r.z),
                    MountedSceneContinuityPolicy.OrientationWritten(snapshot.RiderOrientation, rider.Orientation),
                    MountedSceneContinuityPolicy.PositionWritten(m.x, m.y, m.z),
                    MountedSceneContinuityPolicy.OrientationWritten(snapshot.MountOrientation, mount.Orientation));
                if (write == MountedScenePlacementOperation.RiderTurn)
                {
                    if (agent.ReorientScenePlacement(rider.Orientation) == MountedScenePlacementChange.Superseded)
                        logger?.Info("Pending scene placement of a seated rider reoriented by " + operation + ": rider=" + rider.UniqueId +
                            "; orientation=" + rider.Orientation.ToString("0.0") + ".");
                    continue;
                }
                if (write == MountedScenePlacementOperation.WholePair)
                {
                    if (agent.SupersedeScenePlacementWithWholePair() == MountedScenePlacementChange.Cleared)
                        logger?.Info("Pending scene placement of a seated rider superseded by " + operation + " placing the whole pair: rider=" +
                            rider.UniqueId + "; mount=" + mount.UniqueId + "; the pair stays together at the mount's placement.");
                    continue;
                }
                if (write != MountedScenePlacementOperation.RiderPlacement) continue;
                var change = agent.AttributeScenePlacement(rider.Position, rider.Orientation);
                if (change == MountedScenePlacementChange.Unchanged) continue;
                AttributedCount++;
                logger?.Info("Scene placement of a seated rider attributed to " + operation + ": rider=" + rider.UniqueId +
                    "; mount=" + mount.UniqueId + "; target=" + rider.Position + "; orientation=" + rider.Orientation.ToString("0.0") +
                    "; from saddle " + new Vector2(rider.Position.x - mount.Position.x, rider.Position.z - mount.Position.z).magnitude.ToString("0.00") +
                    " m" + (change == MountedScenePlacementChange.Superseded ? "; supersedes the earlier pending placement" : "") + ".");
            }
        }
    }
}
