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
            if (frame.Capacity < acquisition.Count)
            {
                frame.Capacity = acquisition.Count;
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

        public void PrimeFrame(Action apply)
        {
            if (apply == null) { throw new ArgumentNullException(nameof(apply)); }

            BeginFrame();
            try
            {
                apply();
            }
            finally
            {
                // Priming exercises the exact reversible frame path without
                // allowing its temporary mutation to enter normal animation.
                RestoreFrame();
            }
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

        private void RestoreSnapshots(List<Snapshot> snapshots, string scope)
        {
            List<Exception> failures = null;
            foreach (var snapshot in snapshots)
            {
                try { setLocalPosition(snapshot.Node, snapshot.Position); }
                catch (Exception exception) { AddFailure(ref failures, "Could not restore pose " + scope + " position.", exception); }
                try { setLocalRotation(snapshot.Node, snapshot.Rotation); }
                catch (Exception exception) { AddFailure(ref failures, "Could not restore pose " + scope + " rotation.", exception); }
                try { setLocalScale(snapshot.Node, snapshot.Scale); }
                catch (Exception exception) { AddFailure(ref failures, "Could not restore pose " + scope + " scale.", exception); }
            }
            foreach (var snapshot in snapshots)
            {
                try
                {
                    if (!positionComparer.Equals(getLocalPosition(snapshot.Node), snapshot.Position))
                    {
                        AddFailure(ref failures, "Pose " + scope + " position did not match its captured value.");
                    }
                }
                catch (Exception exception) { AddFailure(ref failures, "Could not verify pose " + scope + " position.", exception); }
                try
                {
                    if (!rotationComparer.Equals(getLocalRotation(snapshot.Node), snapshot.Rotation))
                    {
                        AddFailure(ref failures, "Pose " + scope + " rotation did not match its captured value.");
                    }
                }
                catch (Exception exception) { AddFailure(ref failures, "Could not verify pose " + scope + " rotation.", exception); }
                try
                {
                    if (!scaleComparer.Equals(getLocalScale(snapshot.Node), snapshot.Scale))
                    {
                        AddFailure(ref failures, "Pose " + scope + " scale did not match its captured value.");
                    }
                }
                catch (Exception exception) { AddFailure(ref failures, "Could not verify pose " + scope + " scale.", exception); }
            }
            if (failures != null)
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

        private static void AddFailure(ref List<Exception> failures, string message, Exception innerException = null)
        {
            if (failures == null)
            {
                failures = new List<Exception>();
            }
            failures.Add(innerException == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, innerException));
        }

        private readonly struct Snapshot
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
