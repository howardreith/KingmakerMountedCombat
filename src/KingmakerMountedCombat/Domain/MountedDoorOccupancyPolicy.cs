using System;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 2 (SPACE-01). The decisions behind the pair's occupancy after a door changes state,
    // kept free of engine types so the regressions run offline. The engine facts they consume:
    // the mount's logical footprint is a circle of its movement-agent corpulence around its
    // entity position (the carried rider has no occupancy of its own; its rendered offset is
    // presentation), the door's collision change lands on the navmesh only when the A* tile
    // handler has applied the door's NavmeshCut (RequiresUpdate false, no graph update queued,
    // and a frame after the last tile update), and a destination is safe only when every
    // footprint sample is on the navmesh and reachable along the mesh from the current
    // position without crossing a wall or a disconnected navmesh region.
    public struct MountedFootprintSample
    {
        public MountedFootprintSample(bool centerInside, int ringSamples, int ringInside, int ringReachable)
        {
            CenterInside = centerInside;
            RingSamples = ringSamples;
            RingInside = ringInside;
            RingReachable = ringReachable;
        }

        public bool CenterInside { get; }
        public int RingSamples { get; }
        public int RingInside { get; }
        public int RingReachable { get; }

        // Body clearance, not a center predicate: the center and every ring sample must be on the
        // navmesh and reachable from the center along it.
        public bool IsClear => CenterInside && RingSamples > 0 && RingInside == RingSamples && RingReachable == RingSamples;
    }

    public sealed class MountedDoorRecoveryCandidateFacts
    {
        public bool Walkable { get; set; }
        public float ClampResidualMetres { get; set; }
        public float TraceResidualMetres { get; set; }
        public bool SameNavmeshArea { get; set; }
        public bool FootprintClear { get; set; }
        public bool InsideDoorCut { get; set; }
        public float DistanceMetres { get; set; }
    }

    public enum MountedDoorRecoveryDisposition
    {
        // The settled footprint is clear: nothing to recover.
        NotNeeded,
        // One owned native move to the admitted candidate.
        Admitted,
        // Turn-based combat outside the rider's acting turn: no off-turn movement is manufactured.
        RefusedTurn,
        // The mount owns another command: nothing is replaced for a recovery.
        RefusedMountBusy,
        // No safe, reachable candidate: reported, never transported.
        Unrecoverable
    }

    public enum MountedDoorRecoveryTerminal
    {
        None,
        Completed,
        Interrupted,
        Replaced,
        Stalled,
        PairLost,
        Cancelled
    }

    public static class MountedDoorOccupancyPolicy
    {
        // Ring samples around the footprint circle.
        public const int FootprintRingSamples = 8;
        // A sample is reachable when the navmesh trace from the center ends within this residual.
        public const float ReachabilityEpsilonMetres = 0.05f;
        // A requested candidate whose nearest node sits farther than this is not that point.
        public const float ClampEpsilonMetres = 0.1f;
        // The farthest admitted recovery destination from the mount.
        public const float RecoveryRadiusMetres = 2.5f;
        public static readonly float[] RecoveryRings = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
        public const int RecoveryBearings = 16;
        public static int RecoveryCandidateCount => RecoveryRings.Length * RecoveryBearings;
        // Game seconds the settlement waits for the door's navmesh change before evaluating anyway.
        public const float SettleGameSecondsLimit = 3f;
        // Game seconds a running recovery move may hold the mount still before it is interrupted.
        public const float RecoveryStallSecondsLimit = 4f;
        public const float DisplacementEpsilonMetres = 0.02f;

        // The door's collision change has landed: the door reports the expected state, its cut needs
        // no update, the graph has no queued update and this frame follows the last tile update.
        public static bool NavmeshSettled(bool doorInExpectedState, bool cutRequiresUpdate, bool graphUpdatesQueued, bool frameAfterLastTileUpdate)
        {
            return doorInExpectedState && !cutRequiresUpdate && !graphUpdatesQueued && frameAfterLastTileUpdate;
        }

        // Prevention before the interaction: a door whose cut is written on state changes blocks its
        // doorway again when closed, so closing it while the footprint overlaps the cut embeds the pair.
        // Opening never adds collision; a door whose cut the door never writes changes nothing.
        public static bool ClosingWouldEmbed(bool cutWrittenOnStateChange, bool doorOpenNow, bool footprintIntersectsCut)
        {
            return cutWrittenOnStateChange && doorOpenNow && footprintIntersectsCut;
        }

        public static void Offset(int index, out float ring, out float bearing, out float dx, out float dz)
        {
            if (index < 0 || index >= RecoveryCandidateCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            ring = RecoveryRings[index / RecoveryBearings];
            bearing = 360f / RecoveryBearings * (index % RecoveryBearings);
            var radians = bearing * Math.PI / 180d;
            dx = (float)(Math.Sin(radians) * ring);
            dz = (float)(Math.Cos(radians) * ring);
        }

        public static string CandidateReason(MountedDoorRecoveryCandidateFacts facts)
        {
            if (facts == null || !facts.Walkable) return "no-walkable-native-node";
            if (!Finite(facts.ClampResidualMetres) || facts.ClampResidualMetres > ClampEpsilonMetres) return "off-navmesh";
            if (!Finite(facts.DistanceMetres) || facts.DistanceMetres < 0f || facts.DistanceMetres > RecoveryRadiusMetres) return "beyond-recovery-radius";
            if (!facts.SameNavmeshArea) return "different-navmesh-area";
            if (!Finite(facts.TraceResidualMetres) || facts.TraceResidualMetres > ReachabilityEpsilonMetres) return "trace-unreachable";
            if (facts.InsideDoorCut) return "inside-door-cut";
            if (!facts.FootprintClear) return "footprint-blocked";
            return "admitted";
        }

        public static MountedDoorRecoveryDisposition DecideRecovery(
            bool footprintClear, bool candidateAdmitted, bool turnBasedCombat, bool riderTurnActing, bool mountIdle)
        {
            if (footprintClear) return MountedDoorRecoveryDisposition.NotNeeded;
            if (!candidateAdmitted) return MountedDoorRecoveryDisposition.Unrecoverable;
            if (turnBasedCombat && !riderTurnActing) return MountedDoorRecoveryDisposition.RefusedTurn;
            if (!mountIdle) return MountedDoorRecoveryDisposition.RefusedMountBusy;
            return MountedDoorRecoveryDisposition.Admitted;
        }

        // The recovery move's terminal observation. Ownership is the mount's Move slot still holding the
        // exact move; a player's own order replaces it natively, which ends the recovery as Replaced.
        public static MountedDoorRecoveryTerminal RecoveryTerminal(
            bool pairIntact, bool moveFinished, bool moveStillOwned, bool moveSucceeded, float stalledSeconds)
        {
            if (!pairIntact) return MountedDoorRecoveryTerminal.PairLost;
            if (moveFinished) return moveSucceeded ? MountedDoorRecoveryTerminal.Completed : MountedDoorRecoveryTerminal.Interrupted;
            if (!moveStillOwned) return MountedDoorRecoveryTerminal.Replaced;
            if (Finite(stalledSeconds) && stalledSeconds > RecoveryStallSecondsLimit) return MountedDoorRecoveryTerminal.Stalled;
            return MountedDoorRecoveryTerminal.None;
        }

        // The legacy outcome word kept for the v3 reader: "none" when nothing was needed.
        public static string LegacyRecoveryWord(MountedDoorRecoveryDisposition disposition)
        {
            switch (disposition)
            {
                case MountedDoorRecoveryDisposition.NotNeeded: return "none";
                case MountedDoorRecoveryDisposition.Admitted: return "repath";
                case MountedDoorRecoveryDisposition.RefusedTurn: return "repath-refused-turn";
                case MountedDoorRecoveryDisposition.RefusedMountBusy: return "repath-refused-mount-busy";
                default: return "unrecoverable";
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
