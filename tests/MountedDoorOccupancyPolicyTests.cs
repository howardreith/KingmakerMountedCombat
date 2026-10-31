using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 2 SPACE-01: footprint clearance, settled boundaries, reachable recovery and owned terminals.
    internal static class MountedDoorOccupancyPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted door footprint distinguishes a center on the navmesh from body overlap", FootprintIsBodyNotCenter);
            runner.Run("mounted door navmesh settlement waits for the delayed cut update", SettlementWaitsForCut);
            runner.Run("mounted door recovery rejects a nearby but unreachable destination", RecoveryCandidateReasons);
            runner.Run("mounted door recovery is admitted only for an idle mount on a legal turn", RecoveryDispositions);
            runner.Run("mounted door recovery terminal distinguishes completed, interrupted and replaced", RecoveryTerminals);
            runner.Run("mounted door closing prevention applies only to a written cut while open", ClosingPrevention);
            runner.Run("mounted door recovery candidate offsets stay inside the recovery radius", RecoveryOffsets);
        }

        private static void FootprintIsBodyNotCenter()
        {
            TestRunner.True(new MountedFootprintSample(true, 8, 8, 8).IsClear, "A fully clear footprint was not clear.");
            TestRunner.True(!new MountedFootprintSample(true, 8, 7, 8).IsClear, "A center on the navmesh with one ring sample off it counted as clear.");
            TestRunner.True(!new MountedFootprintSample(true, 8, 8, 7).IsClear, "A ring sample behind a wall (on the mesh but unreachable) counted as clear.");
            TestRunner.True(!new MountedFootprintSample(false, 8, 8, 8).IsClear, "An off-mesh center counted as clear.");
            TestRunner.True(!new MountedFootprintSample(true, 0, 0, 0).IsClear, "A footprint without ring samples counted as clear.");
        }

        private static void SettlementWaitsForCut()
        {
            TestRunner.True(MountedDoorOccupancyPolicy.NavmeshSettled(true, false, false, true), "A settled cut was not settled.");
            TestRunner.True(!MountedDoorOccupancyPolicy.NavmeshSettled(true, true, false, true), "A cut still requiring its update counted as settled (delayed occupancy change).");
            TestRunner.True(!MountedDoorOccupancyPolicy.NavmeshSettled(true, false, true, true), "A queued graph update counted as settled.");
            TestRunner.True(!MountedDoorOccupancyPolicy.NavmeshSettled(true, false, false, false), "The tile update frame itself counted as settled.");
            TestRunner.True(!MountedDoorOccupancyPolicy.NavmeshSettled(false, false, false, true), "A door not yet in its expected state counted as settled.");
            TestRunner.Equal(3f, MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit, "The settle bound changed.");
        }

        private static MountedDoorRecoveryCandidateFacts Facts(bool walkable = true, float clamp = 0f, float trace = 0f, bool sameArea = true,
            bool clear = true, bool insideCut = false, float distance = 1f)
        {
            return new MountedDoorRecoveryCandidateFacts { Walkable = walkable, ClampResidualMetres = clamp, TraceResidualMetres = trace,
                SameNavmeshArea = sameArea, FootprintClear = clear, InsideDoorCut = insideCut, DistanceMetres = distance };
        }

        private static void RecoveryCandidateReasons()
        {
            TestRunner.Equal("admitted", MountedDoorOccupancyPolicy.CandidateReason(Facts()), "A safe reachable candidate was not admitted.");
            TestRunner.Equal("no-walkable-native-node", MountedDoorOccupancyPolicy.CandidateReason(Facts(walkable: false)), "An unwalkable node was admitted.");
            TestRunner.Equal("off-navmesh", MountedDoorOccupancyPolicy.CandidateReason(Facts(clamp: 0.3f)), "A clamped-away point was admitted.");
            TestRunner.Equal("trace-unreachable", MountedDoorOccupancyPolicy.CandidateReason(Facts(trace: 0.4f)),
                "A nearby point behind a wall (navmesh trace stops short) was admitted.");
            TestRunner.Equal("different-navmesh-area", MountedDoorOccupancyPolicy.CandidateReason(Facts(sameArea: false)),
                "A point in a disconnected navmesh region was admitted.");
            TestRunner.Equal("footprint-blocked", MountedDoorOccupancyPolicy.CandidateReason(Facts(clear: false)), "A destination without body clearance was admitted.");
            TestRunner.Equal("inside-door-cut", MountedDoorOccupancyPolicy.CandidateReason(Facts(insideCut: true)), "A destination inside the door's cut was admitted.");
            TestRunner.Equal("beyond-recovery-radius", MountedDoorOccupancyPolicy.CandidateReason(Facts(distance: 2.6f)), "A far destination was admitted.");
            TestRunner.Equal("admitted", MountedDoorOccupancyPolicy.CandidateReason(Facts(distance: 2.5f)), "The recovery radius boundary was excluded.");
            TestRunner.Equal("no-walkable-native-node", MountedDoorOccupancyPolicy.CandidateReason(null), "A missing candidate was admitted.");
            TestRunner.Equal("beyond-recovery-radius", MountedDoorOccupancyPolicy.CandidateReason(Facts(distance: float.NaN)), "A non-finite distance was admitted.");
        }

        private static void RecoveryDispositions()
        {
            TestRunner.Equal(MountedDoorRecoveryDisposition.NotNeeded, MountedDoorOccupancyPolicy.DecideRecovery(true, false, false, false, false),
                "A clear footprint asked for recovery.");
            TestRunner.Equal(MountedDoorRecoveryDisposition.Admitted, MountedDoorOccupancyPolicy.DecideRecovery(false, true, false, false, true),
                "A real-time recovery to an admitted candidate was refused.");
            TestRunner.Equal(MountedDoorRecoveryDisposition.Admitted, MountedDoorOccupancyPolicy.DecideRecovery(false, true, true, true, true),
                "A rider-turn recovery was refused.");
            TestRunner.Equal(MountedDoorRecoveryDisposition.RefusedTurn, MountedDoorOccupancyPolicy.DecideRecovery(false, true, true, false, true),
                "An off-turn recovery manufactured turn-based movement.");
            TestRunner.Equal(MountedDoorRecoveryDisposition.RefusedMountBusy, MountedDoorOccupancyPolicy.DecideRecovery(false, true, false, false, false),
                "A recovery replaced the mount's own command.");
            TestRunner.Equal(MountedDoorRecoveryDisposition.Unrecoverable, MountedDoorOccupancyPolicy.DecideRecovery(false, false, false, false, true),
                "A footprint with no admitted candidate was transported.");
            TestRunner.Equal("none", MountedDoorOccupancyPolicy.LegacyRecoveryWord(MountedDoorRecoveryDisposition.NotNeeded), "The legacy word for a clear footprint changed.");
            TestRunner.Equal("repath", MountedDoorOccupancyPolicy.LegacyRecoveryWord(MountedDoorRecoveryDisposition.Admitted), "The legacy word for a recovery changed.");
        }

        private static void RecoveryTerminals()
        {
            TestRunner.Equal(MountedDoorRecoveryTerminal.None, MountedDoorOccupancyPolicy.RecoveryTerminal(true, false, true, false, 1f), "A running owned recovery ended early.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.Completed, MountedDoorOccupancyPolicy.RecoveryTerminal(true, true, true, true, 0f), "A finished successful recovery was not completed.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.Interrupted, MountedDoorOccupancyPolicy.RecoveryTerminal(true, true, true, false, 0f), "A finished interrupted recovery was not interrupted.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.Replaced, MountedDoorOccupancyPolicy.RecoveryTerminal(true, false, false, false, 0f),
                "A recovery replaced by the player's own order was not reported as replaced.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.Stalled, MountedDoorOccupancyPolicy.RecoveryTerminal(true, false, true, false, 4.5f), "A stalled recovery kept running.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.None, MountedDoorOccupancyPolicy.RecoveryTerminal(true, false, true, false, 4f), "The stall boundary ended a recovery.");
            TestRunner.Equal(MountedDoorRecoveryTerminal.PairLost, MountedDoorOccupancyPolicy.RecoveryTerminal(false, false, true, false, 0f), "A lost pair kept its recovery.");
        }

        private static void ClosingPrevention()
        {
            TestRunner.True(MountedDoorOccupancyPolicy.ClosingWouldEmbed(true, true, true), "Closing a written-cut door over the footprint was not refused.");
            TestRunner.True(!MountedDoorOccupancyPolicy.ClosingWouldEmbed(true, false, true), "Opening a door was refused.");
            TestRunner.True(!MountedDoorOccupancyPolicy.ClosingWouldEmbed(false, true, true), "A static-cut door was refused.");
            TestRunner.True(!MountedDoorOccupancyPolicy.ClosingWouldEmbed(true, true, false), "Closing a door clear of the footprint was refused.");
        }

        private static void RecoveryOffsets()
        {
            TestRunner.Equal(80, MountedDoorOccupancyPolicy.RecoveryCandidateCount, "The recovery candidate count changed.");
            for (var i = 0; i < MountedDoorOccupancyPolicy.RecoveryCandidateCount; i++)
            {
                MountedDoorOccupancyPolicy.Offset(i, out var ring, out var bearing, out var dx, out var dz);
                var distance = Math.Sqrt(dx * dx + dz * dz);
                TestRunner.True(Math.Abs(distance - ring) < 1e-4 && ring <= MountedDoorOccupancyPolicy.RecoveryRadiusMetres && bearing >= 0f && bearing < 360f,
                    "Recovery candidate " + i + " left its ring or the recovery radius.");
            }
            MountedDoorOccupancyPolicy.Offset(0, out var firstRing, out var firstBearing, out _, out _);
            TestRunner.True(Math.Abs(firstRing - 0.5f) < 1e-6 && firstBearing == 0f, "The nearest ring is not searched first.");
            var threw = false;
            try { MountedDoorOccupancyPolicy.Offset(MountedDoorOccupancyPolicy.RecoveryCandidateCount, out _, out _, out _, out _); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            TestRunner.True(threw, "An index beyond the candidate count was accepted.");
        }
    }
}
