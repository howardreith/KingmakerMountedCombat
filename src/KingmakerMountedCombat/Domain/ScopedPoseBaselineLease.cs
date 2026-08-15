using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public sealed class ScopedPoseBaselineLease<TNode, TPosition, TRotation, TScale>
        where TNode : class
    {
        private readonly Func<TNode, TPosition> getLocalPosition;
        private readonly Func<TNode, TRotation> getLocalRotation;
        private readonly Func<TNode, TScale> getLocalScale;
        private readonly Action<TNode, TPosition> setLocalPosition;
        private readonly Action<TNode, TRotation> setLocalRotation;
        private readonly Action<TNode, TScale> setLocalScale;
        private readonly IEqualityComparer<TNode> nodeComparer;
        private readonly IEqualityComparer<TPosition> positionComparer;
        private readonly IEqualityComparer<TRotation> rotationComparer;
        private readonly IEqualityComparer<TScale> scaleComparer;
        private readonly List<Snapshot> acquisition = new List<Snapshot>();
        private readonly List<Snapshot> frame = new List<Snapshot>();

        public ScopedPoseBaselineLease(
            Func<TNode, TPosition> getLocalPosition,
            Func<TNode, TRotation> getLocalRotation,
            Func<TNode, TScale> getLocalScale,
            Action<TNode, TPosition> setLocalPosition,
            Action<TNode, TRotation> setLocalRotation,
            Action<TNode, TScale> setLocalScale,
            IEqualityComparer<TNode> nodeComparer = null,
            IEqualityComparer<TPosition> positionComparer = null,
            IEqualityComparer<TRotation> rotationComparer = null,
            IEqualityComparer<TScale> scaleComparer = null)
        {
            this.getLocalPosition = getLocalPosition ?? throw new ArgumentNullException(nameof(getLocalPosition));
            this.getLocalRotation = getLocalRotation ?? throw new ArgumentNullException(nameof(getLocalRotation));
            this.getLocalScale = getLocalScale ?? throw new ArgumentNullException(nameof(getLocalScale));
            this.setLocalPosition = setLocalPosition ?? throw new ArgumentNullException(nameof(setLocalPosition));
            this.setLocalRotation = setLocalRotation ?? throw new ArgumentNullException(nameof(setLocalRotation));
            this.setLocalScale = setLocalScale ?? throw new ArgumentNullException(nameof(setLocalScale));
            this.nodeComparer = nodeComparer ?? EqualityComparer<TNode>.Default;
            this.positionComparer = positionComparer ?? EqualityComparer<TPosition>.Default;
            this.rotationComparer = rotationComparer ?? EqualityComparer<TRotation>.Default;
            this.scaleComparer = scaleComparer ?? EqualityComparer<TScale>.Default;
        }

        public bool IsAcquired { get; private set; }

        public bool IsFrameActive { get; private set; }

        public bool LastRestoreVerified { get; private set; }

        public int NodeCount => acquisition.Count;

        public long FrameCaptureCount { get; private set; }

        public void Acquire(IEnumerable<TNode> nodes)
        {
            if (IsAcquired)
            {
                throw new InvalidOperationException("A pose baseline lease is already active.");
            }
            if (nodes == null) { throw new ArgumentNullException(nameof(nodes)); }

            acquisition.Clear();
            frame.Clear();
            foreach (var node in nodes)
            {
                if (node == null) { throw new ArgumentException("A pose baseline node is null.", nameof(nodes)); }
                foreach (var existing in acquisition)
                {
                    if (nodeComparer.Equals(existing.Node, node))
                    {
                        acquisition.Clear();
                        throw new ArgumentException("A pose baseline node is duplicated.", nameof(nodes));
                    }
                }
                acquisition.Add(Capture(node));
            }
            if (acquisition.Count == 0)
            {
                throw new ArgumentException("A pose baseline lease requires at least one node.", nameof(nodes));
            }

            IsAcquired = true;
            IsFrameActive = false;
            LastRestoreVerified = false;
            FrameCaptureCount = 0;
        }

        public void BeginFrame()
        {
            RequireAcquired();
            RestoreFrame();
            frame.Clear();
            foreach (var snapshot in acquisition)
            {
                frame.Add(Capture(snapshot.Node));
            }
            // Own the frame before caller mutation. A partial pose application is
            // therefore restorable by the next frame or ordinary cleanup.
            IsFrameActive = true;
            FrameCaptureCount++;
        }

        public void RestoreFrame()
        {
            if (!IsFrameActive)
            {
                return;
            }

            RestoreSnapshots(frame, "frame");
            frame.Clear();
            IsFrameActive = false;
        }

        public void Restore()
        {
            if (!IsAcquired)
            {
                return;
            }

            var failures = new List<Exception>();
            try { RestoreFrame(); }
            catch (Exception exception) { failures.Add(exception); }
            try { RestoreSnapshots(acquisition, "acquisition"); }
            catch (Exception exception) { failures.Add(exception); }

            if (failures.Count != 0)
            {
                throw new AggregateException("Pose baseline restoration retained residue.", failures);
            }

            acquisition.Clear();
            frame.Clear();
            IsAcquired = false;
            IsFrameActive = false;
            LastRestoreVerified = true;
        }

        private Snapshot Capture(TNode node)
        {
            return new Snapshot(node, getLocalPosition(node), getLocalRotation(node), getLocalScale(node));
        }

        private void RestoreSnapshots(IEnumerable<Snapshot> snapshots, string scope)
        {
            var failures = new List<Exception>();
            foreach (var snapshot in snapshots)
            {
                TryRestore(() => setLocalPosition(snapshot.Node, snapshot.Position), scope + " position", failures);
                TryRestore(() => setLocalRotation(snapshot.Node, snapshot.Rotation), scope + " rotation", failures);
                TryRestore(() => setLocalScale(snapshot.Node, snapshot.Scale), scope + " scale", failures);
            }
            foreach (var snapshot in snapshots)
            {
                TryVerify(() => positionComparer.Equals(getLocalPosition(snapshot.Node), snapshot.Position), scope + " position", failures);
                TryVerify(() => rotationComparer.Equals(getLocalRotation(snapshot.Node), snapshot.Rotation), scope + " rotation", failures);
                TryVerify(() => scaleComparer.Equals(getLocalScale(snapshot.Node), snapshot.Scale), scope + " scale", failures);
            }
            if (failures.Count != 0)
            {
                throw new AggregateException("Could not restore and verify the pose " + scope + " baseline.", failures);
            }
        }

        private void RequireAcquired()
        {
            if (!IsAcquired)
            {
                throw new InvalidOperationException("A pose baseline lease is not active.");
            }
        }

        private static void TryRestore(Action action, string field, ICollection<Exception> failures)
        {
            try { action(); }
            catch (Exception exception) { failures.Add(new InvalidOperationException("Could not restore pose " + field + ".", exception)); }
        }

        private static void TryVerify(Func<bool> predicate, string field, ICollection<Exception> failures)
        {
            try
            {
                if (!predicate()) { failures.Add(new InvalidOperationException("Pose " + field + " did not match its captured value.")); }
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException("Could not verify pose " + field + ".", exception));
            }
        }

        private sealed class Snapshot
        {
            public Snapshot(TNode node, TPosition position, TRotation rotation, TScale scale)
            {
                Node = node;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public TNode Node { get; }

            public TPosition Position { get; }

            public TRotation Rotation { get; }

            public TScale Scale { get; }
        }
    }
}
