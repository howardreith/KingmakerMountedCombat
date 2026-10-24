using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Engine-independent ownership and restoration logic for a temporary
    /// transform-parent lease. The adapter operations are injected so the exact
    /// parent, sibling slot, world pose, and local scale contract is
    /// deterministic-testable without loading Unity.
    /// </summary>
    /// <remarks>
    /// Several leases may borrow children of one shared native parent and return
    /// them in any order. A captured ordinal names the vacated slot only in the
    /// layout it was taken in: once other siblings leave or return it can point
    /// elsewhere, or past the end. Each lease therefore owns one inert
    /// reservation that occupies its target's exact slot from before detachment
    /// until the target is verified back in front of it.
    /// </remarks>
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
        private readonly Func<TNode, object> getContainer;
        private readonly Action<TNode, object> moveToContainer;
        private readonly Func<TNode> createReservation;
        private readonly Func<TNode, bool> reservationExists;
        private readonly Action<TNode> destroyReservation;
        private readonly IEqualityComparer<TNode> nodeComparer;
        private readonly IEqualityComparer<TPosition> positionComparer;
        private readonly IEqualityComparer<TRotation> rotationComparer;
        private readonly IEqualityComparer<TScale> scaleComparer;

        private TNode node;
        private TNode attachmentParent;
        private TNode originalParent;
        private object originalContainer;
        private int originalSiblingIndex;
        private TPosition originalWorldPosition;
        private TRotation originalWorldRotation;
        private TScale originalLocalScale;
        private TNode reservation;
        private bool reservationVerified;
        private bool detachStarted;
        private bool slotVerified;

        /// <param name="getContainer">
        /// Exact sibling container of a node: its parent, or for a parentless root
        /// the identity of the scene whose root list holds it. Compared with Equals.
        /// </param>
        /// <param name="moveToContainer">
        /// Makes a node the last sibling of a captured container, keeping its world pose.
        /// </param>
        /// <param name="createReservation">
        /// Creates one inert reservation node outside the target's container; creation
        /// alone never places it among the target's siblings.
        /// </param>
        /// <param name="reservationExists">False once a reservation was destroyed.</param>
        /// <param name="destroyReservation">
        /// Removes an existing reservation from its container within the call, then
        /// destroys it; actual object destruction may complete later.
        /// </param>
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
            Func<TNode, object> getContainer,
            Action<TNode, object> moveToContainer,
            Func<TNode> createReservation,
            Func<TNode, bool> reservationExists,
            Action<TNode> destroyReservation,
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
            this.getContainer = getContainer ?? throw new ArgumentNullException(nameof(getContainer));
            this.moveToContainer = moveToContainer ?? throw new ArgumentNullException(nameof(moveToContainer));
            this.createReservation = createReservation ?? throw new ArgumentNullException(nameof(createReservation));
            this.reservationExists = reservationExists ?? throw new ArgumentNullException(nameof(reservationExists));
            this.destroyReservation = destroyReservation ?? throw new ArgumentNullException(nameof(destroyReservation));
            this.nodeComparer = nodeComparer ?? EqualityComparer<TNode>.Default;
            this.positionComparer = positionComparer ?? EqualityComparer<TPosition>.Default;
            this.rotationComparer = rotationComparer ?? EqualityComparer<TRotation>.Default;
            this.scaleComparer = scaleComparer ?? EqualityComparer<TScale>.Default;
        }

        public bool IsAcquired { get; private set; }

        public bool LastRestoreVerified { get; private set; }

        /// <summary>False when the last verified restoration had to use the captured ordinal.</summary>
        public bool LastRestoreUsedReservation { get; private set; }

        /// <summary>
        /// True when the last returned target lost its reservation and could only be
        /// placed by the captured ordinal, whose exactness then assumes an unchanged layout.
        /// </summary>
        public bool LastRestoreUsedCapturedOrdinal { get; private set; }

        /// <summary>True while this lease owns a sibling slot reservation, placed or not.</summary>
        public bool HoldsSiblingReservation => reservation != null;

        /// <summary>
        /// Read-only observation: the owned reservation still exists in the captured
        /// container. False for an inactive lease or before the slot was verified.
        /// </summary>
        public bool SiblingReservationIntact
        {
            get
            {
                try { return IsAcquired && SlotReservation() != null; }
                catch { return false; }
            }
        }

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
            originalContainer = getContainer(target);
            originalSiblingIndex = getSiblingIndex(target);
            originalWorldPosition = getWorldPosition(target);
            originalWorldRotation = getWorldRotation(target);
            originalLocalScale = getLocalScale(target);
            reservation = null;
            reservationVerified = false;
            detachStarted = false;
            slotVerified = false;
            LastRestoreVerified = false;
            LastRestoreUsedReservation = false;
            LastRestoreUsedCapturedOrdinal = false;
            // Own the lease before mutation. If an engine call throws after partial
            // work, ordinary rollback still has the complete snapshot.
            IsAcquired = true;

            // Hold the exact slot before the target leaves it, so returning it can
            // never depend on which siblings are absent at that later moment.
            reservation = createReservation();
            if (reservation == null)
            {
                throw new InvalidOperationException("A sibling slot reservation could not be created.");
            }
            moveToContainer(reservation, originalContainer);
            setSiblingIndex(reservation, originalSiblingIndex + 1);
            if (!object.Equals(getContainer(reservation), originalContainer) ||
                getSiblingIndex(target) != originalSiblingIndex ||
                getSiblingIndex(reservation) != originalSiblingIndex + 1)
            {
                throw new InvalidOperationException("The sibling slot reservation did not settle immediately after its target.");
            }
            reservationVerified = true;

            detachStarted = true;
            setParent(target, attachmentParent, true);
            if (getSiblingIndex(reservation) != originalSiblingIndex)
            {
                throw new InvalidOperationException("The sibling slot reservation did not take over the vacated slot.");
            }
        }

        public void Restore()
        {
            if (!IsAcquired)
            {
                return;
            }

            var failures = new List<Exception>();
            var verifiedIndex = -1;
            if (!detachStarted)
            {
                // The target never left its slot and the lease wrote none of its
                // state: prove only its container, then withdraw the reservation.
                TryVerify(() => IsInOriginalContainer(node), "parent", failures);
            }
            else if (!slotVerified)
            {
                verifiedIndex = ReturnTargetToSlot(failures);
                RestorePose(failures);
            }
            else
            {
                // A target once verified in its slot is never repositioned: by now
                // the reservation may be gone and the captured ordinal stale. A
                // retry only proves the container and finishes what failed.
                TryVerify(() => IsInOriginalContainer(node), "parent", failures);
                RestorePose(failures);
            }
            if (failures.Count == 0)
            {
                // Release the reservation only behind a verified target, so a retry
                // never loses the slot it still has to return into.
                TryRestore(ReleaseReservation, "sibling slot reservation", failures);
            }
            if (failures.Count == 0)
            {
                TryVerify(() => IsInOriginalContainer(node), "parent", failures);
                if (verifiedIndex >= 0)
                {
                    TryVerify(() => getSiblingIndex(node) == verifiedIndex, "sibling index", failures);
                }
            }

            if (failures.Count != 0)
            {
                // Retain the lease, its snapshot and any reservation so a coordinator
                // cleanup retry can repeat every idempotent restoration operation.
                throw new AggregateException("Transform attachment restoration retained residue.", failures);
            }

            IsAcquired = false;
            LastRestoreVerified = true;
            detachStarted = false;
            slotVerified = false;
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
            var slot = SlotReservation();
            var slotIndex = slot == null ? -1 : getSiblingIndex(slot);
            // Native Polymorph.TryReplaceView appends its replacement as the last
            // child of the old view's parent. Give it that same placement in the
            // captured container: never inside or in front of the reserved slot,
            // where it would displace the old view's exact return position.
            moveToContainer(replacement, originalContainer);
            if (!IsInOriginalContainer(replacement) ||
                !positionComparer.Equals(getWorldPosition(replacement), worldPosition) ||
                !rotationComparer.Equals(getWorldRotation(replacement), worldRotation) ||
                slot != null && getSiblingIndex(slot) != slotIndex)
            {
                throw new InvalidOperationException("Inherited replacement did not leave the owned attachment parent with its world pose and the reserved slot preserved.");
            }
            return true;
        }

        // Returns the verified slot index of the target, or -1 when unverified.
        private int ReturnTargetToSlot(ICollection<Exception> failures)
        {
            var priorFailures = failures.Count;
            TNode slot = null;
            TryRestore(() => slot = SlotReservation(), "sibling slot reservation lookup", failures);
            LastRestoreUsedReservation = slot != null;
            LastRestoreUsedCapturedOrdinal = slot == null;
            TryRestore(() => moveToContainer(node, originalContainer), "parent", failures);
            TryRestore(() =>
            {
                // Only a target back in its container can take its slot there; the
                // container verification below reports any other outcome.
                if (!IsInOriginalContainer(node)) return;
                setSiblingIndex(node, slot == null ? originalSiblingIndex : IndexInFrontOf(slot));
            }, "sibling index", failures);

            var restoredIndex = -1;
            TryVerify(() => IsInOriginalContainer(node), "parent", failures);
            TryVerify(() =>
            {
                var index = getSiblingIndex(node);
                // The reservation is the authority for the exact slot. Without it,
                // only the captured ordinal remains and is held to exact equality.
                var exact = IsInOriginalContainer(node) && (slot == null
                    ? index == originalSiblingIndex
                    : IsInOriginalContainer(slot) && getSiblingIndex(slot) == index + 1);
                if (exact) restoredIndex = index;
                return exact;
            }, "sibling slot", failures);
            // Bind the slot only when every placement step succeeded. A later
            // failure then retries without ever repositioning this target.
            if (restoredIndex >= 0 && failures.Count == priorFailures)
            {
                slotVerified = true;
                return restoredIndex;
            }
            return -1;
        }

        private void RestorePose(ICollection<Exception> failures)
        {
            TryRestore(() => setWorldPosition(node, originalWorldPosition), "world position", failures);
            TryRestore(() => setWorldRotation(node, originalWorldRotation), "world rotation", failures);
            TryRestore(() => setLocalScale(node, originalLocalScale), "local scale", failures);
            TryVerify(() => positionComparer.Equals(getWorldPosition(node), originalWorldPosition), "world position", failures);
            TryVerify(() => rotationComparer.Equals(getWorldRotation(node), originalWorldRotation), "world rotation", failures);
            TryVerify(() => scaleComparer.Equals(getLocalScale(node), originalLocalScale), "local scale", failures);
        }

        private TNode SlotReservation()
        {
            // Only a reservation verified in the vacated slot and still present in
            // the captured container can locate that slot.
            if (reservation == null || !reservationVerified || !reservationExists(reservation))
            {
                return null;
            }
            return IsInOriginalContainer(reservation) ? reservation : null;
        }

        private int IndexInFrontOf(TNode slot)
        {
            var current = getSiblingIndex(node);
            var held = getSiblingIndex(slot);
            // A sibling index is the node's resulting position, so moving forward
            // past the reservation must land one before its present index.
            return current < held ? held - 1 : held;
        }

        private void ReleaseReservation()
        {
            if (reservation == null)
            {
                return;
            }
            if (reservationExists(reservation))
            {
                destroyReservation(reservation);
            }
            if (reservationExists(reservation) && IsInOriginalContainer(reservation))
            {
                throw new InvalidOperationException("A released sibling slot reservation still occupies its container.");
            }
            reservation = null;
            reservationVerified = false;
        }

        private bool IsInOriginalContainer(TNode value)
        {
            return object.Equals(getContainer(value), originalContainer) &&
                nodeComparer.Equals(getParent(value), originalParent);
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
