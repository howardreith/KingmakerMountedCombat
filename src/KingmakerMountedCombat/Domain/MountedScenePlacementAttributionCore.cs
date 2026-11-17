using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // The exact native scene placement operation an attribution comes from (review R8 of e1c0c7ad: structured identity,
    // never a display string).
    public enum MountedScenePlacementSource
    {
        // TranslocateUnit.RunAction (06008FA2): one unit, its Unit evaluator's.
        TranslocateUnit,
        // CommandMoveUnit.IsFinished (06009E4E) on its taking-too-long branch: one unit, the command's.
        CommandMoveUnitTimedOut,
        // CommandTeleportParty.OnRun (06009E90): every unit of Player.GetCharactersList(m_UnitsList), each given Position and
        // Orientation unconditionally.
        CommandTeleportParty
    }

    // One mounted pair around one native scene placement operation: what it was before and is after, whether the
    // operation's proven target set held the rider and the mount, and the rider synchronizer's hold for that exact
    // mounting (null when that synchronizer is not configured, or when the pair's mounting or synchronizer changed inside
    // the operation).
    public sealed class MountedScenePlacementObservation
    {
        public object Pair;
        public MountedScenePlacementHold Hold;
        public bool PairMounted;
        public float RiderX0, RiderY0, RiderZ0, RiderOrientation0, MountX0, MountY0, MountZ0, MountOrientation0;
        public float RiderX, RiderY, RiderZ, RiderOrientation, MountX, MountY, MountZ, MountOrientation;
        // Only from an operation whose target set is proven (CommandTeleportParty); false for single-unit operations.
        public bool RiderTargeted, MountTargeted;
    }

    // Review R8 and R9 of e1c0c7ad: the attribution of one native operation to every captured pair. Each pair is
    // classified from its own writes and target evidence and applied to its own hold; a report that fails (a throwing
    // logger) never stops the remaining pairs.
    public static class MountedScenePlacementAttributionCore
    {
        public static MountedScenePlacementOperation Classify(MountedScenePlacementObservation o, bool sceneActive) =>
            MountedSceneContinuityPolicy.ClassifyOperationWrite(o.PairMounted, sceneActive,
                MountedSceneContinuityPolicy.PositionWritten(o.RiderX - o.RiderX0, o.RiderY - o.RiderY0, o.RiderZ - o.RiderZ0),
                MountedSceneContinuityPolicy.OrientationWritten(o.RiderOrientation0, o.RiderOrientation),
                MountedSceneContinuityPolicy.PositionWritten(o.MountX - o.MountX0, o.MountY - o.MountY0, o.MountZ - o.MountZ0),
                MountedSceneContinuityPolicy.OrientationWritten(o.MountOrientation0, o.MountOrientation),
                o.RiderTargeted, o.MountTargeted);

        // Returns how many holds changed.
        public static int Apply(IList<MountedScenePlacementObservation> observations, bool sceneActive, int frame,
            Action<MountedScenePlacementObservation, MountedScenePlacementOperation, MountedScenePlacementChange> report)
        {
            if (observations == null) return 0;
            var changed = 0;
            foreach (var o in observations)
            {
                if (o?.Hold == null) continue;
                var write = Classify(o, sceneActive);
                MountedScenePlacementChange change;
                switch (write)
                {
                    case MountedScenePlacementOperation.WholePair:
                        change = o.Hold.ClearForWholePair();
                        break;
                    case MountedScenePlacementOperation.RiderPlacement:
                        change = o.Hold.Attribute(o.RiderX, o.RiderY, o.RiderZ, o.RiderOrientation, frame);
                        break;
                    case MountedScenePlacementOperation.RiderTurn:
                        change = o.Hold.Reorient(o.RiderX, o.RiderY, o.RiderZ, o.RiderOrientation, frame);
                        break;
                    default:
                        continue;
                }
                if (change == MountedScenePlacementChange.Unchanged) continue;
                changed++;
                if (report == null) continue;
                try { report(o, write, change); }
                catch (Exception) { }
            }
            return changed;
        }
    }
}
