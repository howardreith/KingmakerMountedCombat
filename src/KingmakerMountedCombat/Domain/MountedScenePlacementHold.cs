using System;

namespace KingmakerMountedCombat.Domain
{
    // What a native scene placement operation did to one mounted pair, from the positions captured around it.
    public enum MountedScenePlacementOperation
    {
        None,
        // The operation wrote the rider and not its mount, during a scene: the scene's placement of the rider.
        RiderPlacement,
        // The operation wrote the rider and the mount: the pair goes with the mount, as a whole-pair placement always has.
        WholePair,
        // Review R5 of 263ac3ea and R7 of e1c0c7ad: the operation turned the rider without moving it, during a scene. It
        // supersedes the orientation of a pending placement; with nothing pending it is held as the scene's placement of
        // the rider at its current position with the authored orientation, for the ordinary handoff.
        RiderTurn
    }

    public enum MountedScenePlacementChange
    {
        Unchanged,
        Held,
        Superseded,
        Cleared
    }

    // Known issues C02, review R2 of b30c24f8: a seated rider's pending scene placement, the one state the rider's
    // synchronizer (RiderMovementAgent) keeps for the pair's owner between a scene's write and the owner's poll. It holds
    // at most one placement, and it is always the scene's latest:
    // - a native operation's rider placement replaces a pending one (the final authored endpoint and orientation stand);
    // - a native operation that writes the whole pair makes a pending rider-only target obsolete: it is cleared, and the
    //   synchronizer resumes as after any whole-pair placement (nothing is reapplied);
    // - an unattributed write while one is held replaces it only when it would be held on its own: more than 3 m from the
    //   saddle and from the held target, during a scene (the unchanged distance rule, measured from where the rider was
    //   left);
    // - the owner's refusal releases it, and the next sample returns the rider to the saddle once;
    // - past the decision frames it is released the same way.
    // The instance lives in one rider synchronizer, which is created and configured per mounting, and is reset by
    // Configure and Deconfigure.
    public sealed class MountedScenePlacementHold
    {
        public const int DecisionFrames = 30;

        private bool pending;
        private bool correctNextWithoutDetection;
        private int heldAtFrame;

        public bool Pending => pending;
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public float Orientation { get; private set; }

        public long ObservedCount { get; private set; }
        public long AttributedCount { get; private set; }
        public long SupersededCount { get; private set; }
        public long ClearedCount { get; private set; }

        public void Reset()
        {
            pending = false;
            correctNextWithoutDetection = false;
        }

        public MountedScenePlacementChange Attribute(float x, float y, float z, float orientation, int frame)
        {
            var change = pending ? MountedScenePlacementChange.Superseded : MountedScenePlacementChange.Held;
            if (pending) SupersededCount++;
            else ObservedCount++;
            AttributedCount++;
            Hold(x, y, z, orientation, frame);
            correctNextWithoutDetection = false;
            return change;
        }

        // Review R5: an operation turned the rider in place. A held placement takes the scene's final orientation and keeps
        // its endpoint; with nothing held (review R7 of e1c0c7ad) the rider's current position and the authored
        // orientation are held as the scene's placement.
        public MountedScenePlacementChange Reorient(float x, float y, float z, float orientation, int frame)
        {
            if (!pending)
            {
                ObservedCount++;
                AttributedCount++;
                Hold(x, y, z, orientation, frame);
                correctNextWithoutDetection = false;
                return MountedScenePlacementChange.Held;
            }
            SupersededCount++;
            Orientation = orientation;
            heldAtFrame = frame;
            return MountedScenePlacementChange.Superseded;
        }

        public MountedScenePlacementChange ClearForWholePair()
        {
            if (!pending) return MountedScenePlacementChange.Unchanged;
            pending = false;
            correctNextWithoutDetection = false;
            ClearedCount++;
            return MountedScenePlacementChange.Cleared;
        }

        public void Release()
        {
            pending = false;
            correctNextWithoutDetection = true;
        }

        // The held placement as it stands now: an unattributed far write since it was held supersedes it. Asked by every
        // sample and by the owner's poll before it hands the pair off, so neither order reapplies an earlier target.
        public bool Refresh(int frame, bool sceneActive, float riderX, float riderY, float riderZ, float riderOrientation, float saddleX, float saddleZ)
        {
            if (!pending) return false;
            if (MountedSceneContinuityPolicy.HoldsUnattributedPlacement(sceneActive, true, Horizontal(riderX - saddleX, riderZ - saddleZ),
                Horizontal(riderX - X, riderZ - Z)))
            {
                SupersededCount++;
                Hold(riderX, riderY, riderZ, riderOrientation, frame);
            }
            return true;
        }

        // One synchronizer sample. True holds (the synchronizer writes nothing); false lets it write the rider to the saddle.
        public bool Sample(int frame, bool sceneActive, float riderX, float riderY, float riderZ, float riderOrientation,
            float saddleX, float saddleZ, bool hasLastWrite, float lastWriteX, float lastWriteZ)
        {
            var fromSaddle = Horizontal(riderX - saddleX, riderZ - saddleZ);
            if (pending)
            {
                if (frame - heldAtFrame > DecisionFrames)
                {
                    Release();
                    return false;
                }
                return Refresh(frame, sceneActive, riderX, riderY, riderZ, riderOrientation, saddleX, saddleZ);
            }
            if (correctNextWithoutDetection)
            {
                correctNextWithoutDetection = false;
                return false;
            }
            var fromLastWrite = hasLastWrite ? Horizontal(riderX - lastWriteX, riderZ - lastWriteZ) : 0f;
            if (!MountedSceneContinuityPolicy.HoldsUnattributedPlacement(sceneActive, hasLastWrite, fromSaddle, fromLastWrite)) return false;
            ObservedCount++;
            Hold(riderX, riderY, riderZ, riderOrientation, frame);
            return true;
        }

        private void Hold(float x, float y, float z, float orientation, int frame)
        {
            pending = true;
            X = x; Y = y; Z = z;
            Orientation = orientation;
            heldAtFrame = frame;
        }

        private static float Horizontal(float dx, float dz) => (float)Math.Sqrt(dx * dx + dz * dz);
    }
}
