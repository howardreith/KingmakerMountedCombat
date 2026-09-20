using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Engine-independent ownership and restoration logic for a temporary
    /// transform-parent lease. The adapter operations are injected so the exact
    /// parent, sibling, world pose, and local scale contract is deterministic-testable
    /// without loading Unity.
    /// </summary>
    public sealed class ScopedTransformAttachmentLease<TNode, TPosition, TRotation, TScale>
        where TNode : class
    {
        private readonly Func<TNode, TNode> getParent;
        private readonly Func<TNode, int> getSiblingIndex;
        private readonly Func<TNode, TPosition> getWorldPosition;
        private readonly Func<TNode, TRotation> getWorldRotation;
        private readonly Func<TNode, TScale> getLocalScale;
        private readonly Action<TNode, TNode, bool> setParent;
        private readonly Action<TNode, int> setSiblingIndex;
        private readonly Action<TNode, TPosition> setWorldPosition;
        private readonly Action<TNode, TRotation> setWorldRotation;
        private readonly Action<TNode, TScale> setLocalScale;
        private readonly IEqualityComparer<TNode> nodeComparer;
        private readonly IEqualityComparer<TPosition> positionComparer;
        private readonly IEqualityComparer<TRotation> rotationComparer;
        private readonly IEqualityComparer<TScale> scaleComparer;

        private TNode node;
        private TNode attachmentParent;
        private TNode originalParent;
        private int originalSiblingIndex;
        private TPosition originalWorldPosition;
        private TRotation originalWorldRotation;
        private TScale originalLocalScale;

        public ScopedTransformAttachmentLease(
            Func<TNode, TNode> getParent,
            Func<TNode, int> getSiblingIndex,
            Func<TNode, TPosition> getWorldPosition,
            Func<TNode, TRotation> getWorldRotation,
            Func<TNode, TScale> getLocalScale,
            Action<TNode, TNode, bool> setParent,
            Action<TNode, int> setSiblingIndex,
            Action<TNode, TPosition> setWorldPosition,
            Action<TNode, TRotation> setWorldRotation,
            Action<TNode, TScale> setLocalScale,
            IEqualityComparer<TNode> nodeComparer = null,
            IEqualityComparer<TPosition> positionComparer = null,
            IEqualityComparer<TRotation> rotationComparer = null,
            IEqualityComparer<TScale> scaleComparer = null)
        {
            this.getParent = getParent ?? throw new ArgumentNullException(nameof(getParent));
            this.getSiblingIndex = getSiblingIndex ?? throw new ArgumentNullException(nameof(getSiblingIndex));
            this.getWorldPosition = getWorldPosition ?? throw new ArgumentNullException(nameof(getWorldPosition));
            this.getWorldRotation = getWorldRotation ?? throw new ArgumentNullException(nameof(getWorldRotation));
            this.getLocalScale = getLocalScale ?? throw new ArgumentNullException(nameof(getLocalScale));
            this.setParent = setParent ?? throw new ArgumentNullException(nameof(setParent));
            this.setSiblingIndex = setSiblingIndex ?? throw new ArgumentNullException(nameof(setSiblingIndex));
            this.setWorldPosition = setWorldPosition ?? throw new ArgumentNullException(nameof(setWorldPosition));
            this.setWorldRotation = setWorldRotation ?? throw new ArgumentNullException(nameof(setWorldRotation));
            this.setLocalScale = setLocalScale ?? throw new ArgumentNullException(nameof(setLocalScale));
            this.nodeComparer = nodeComparer ?? EqualityComparer<TNode>.Default;
            this.positionComparer = positionComparer ?? EqualityComparer<TPosition>.Default;
            this.rotationComparer = rotationComparer ?? EqualityComparer<TRotation>.Default;
            this.scaleComparer = scaleComparer ?? EqualityComparer<TScale>.Default;
        }

        public bool IsAcquired { get; private set; }

        public bool LastRestoreVerified { get; private set; }

        public TNode OriginalParent => originalParent;

        public int OriginalSiblingIndex => originalSiblingIndex;

        public TPosition OriginalWorldPosition => originalWorldPosition;

        public TRotation OriginalWorldRotation => originalWorldRotation;

        public TScale OriginalLocalScale => originalLocalScale;

        public void Acquire(TNode target, TNode attachmentParent)
        {
            if (IsAcquired)
            {
                throw new InvalidOperationException("A transform attachment lease is already active.");
            }
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            if (attachmentParent == null) { throw new ArgumentNullException(nameof(attachmentParent)); }
            if (nodeComparer.Equals(target, attachmentParent))
            {
                throw new InvalidOperationException("A transform cannot be attached to itself.");
            }

            node = target;
            this.attachmentParent = attachmentParent;
            originalParent = getParent(target);
            originalSiblingIndex = getSiblingIndex(target);
            originalWorldPosition = getWorldPosition(target);
            originalWorldRotation = getWorldRotation(target);
            originalLocalScale = getLocalScale(target);
            LastRestoreVerified = false;
            // Own the lease before mutation. If SetParent throws after partial
            // engine work, ordinary rollback still has the complete snapshot.
            IsAcquired = true;
            setParent(target, attachmentParent, true);
        }

        public void Restore()
        {
            if (!IsAcquired)
            {
                return;
            }

            var failures = new List<Exception>();
            TryRestore(() => setParent(node, originalParent, true), "parent", failures);
            TryRestore(() => setSiblingIndex(node, originalSiblingIndex), "sibling index", failures);
            TryRestore(() => setWorldPosition(node, originalWorldPosition), "world position", failures);
            TryRestore(() => setWorldRotation(node, originalWorldRotation), "world rotation", failures);
            TryRestore(() => setLocalScale(node, originalLocalScale), "local scale", failures);

            TryVerify(() => nodeComparer.Equals(getParent(node), originalParent), "parent", failures);
            TryVerify(() => getSiblingIndex(node) == originalSiblingIndex, "sibling index", failures);
            TryVerify(() => positionComparer.Equals(getWorldPosition(node), originalWorldPosition), "world position", failures);
            TryVerify(() => rotationComparer.Equals(getWorldRotation(node), originalWorldRotation), "world rotation", failures);
            TryVerify(() => scaleComparer.Equals(getLocalScale(node), originalLocalScale), "local scale", failures);

            if (failures.Count != 0)
            {
                // Retain the lease and its snapshot so a coordinator cleanup
                // retry can repeat every idempotent restoration operation.
                throw new AggregateException("Transform attachment restoration retained residue.", failures);
            }

            IsAcquired = false;
            LastRestoreVerified = true;
            node = null;
            attachmentParent = null;
        }

        public bool ReleaseInheritedReplacement(TNode replacement)
        {
            if (!IsAcquired)
            {
                throw new InvalidOperationException("A replacement cannot inherit from an inactive attachment lease.");
            }
            if (replacement == null)
            {
                throw new ArgumentNullException(nameof(replacement));
            }
            if (!nodeComparer.Equals(getParent(replacement), attachmentParent))
            {
                return false;
            }

            var worldPosition = getWorldPosition(replacement);
            var worldRotation = getWorldRotation(replacement);
            setParent(replacement, originalParent, true);
            setSiblingIndex(replacement, originalSiblingIndex);
            if (!nodeComparer.Equals(getParent(replacement), originalParent) ||
                !positionComparer.Equals(getWorldPosition(replacement), worldPosition) ||
                !rotationComparer.Equals(getWorldRotation(replacement), worldRotation))
            {
                throw new InvalidOperationException("Inherited replacement did not leave the owned attachment parent with its world pose preserved.");
            }
            return true;
        }

        private static void TryRestore(Action action, string field, ICollection<Exception> failures)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException("Could not restore transform " + field + ".", exception));
            }
        }

        private static void TryVerify(Func<bool> predicate, string field, ICollection<Exception> failures)
        {
            try
            {
                if (!predicate())
                {
                    failures.Add(new InvalidOperationException("Transform " + field + " did not match its captured value after restoration."));
                }
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException("Could not verify restored transform " + field + ".", exception));
            }
        }
    }
}
