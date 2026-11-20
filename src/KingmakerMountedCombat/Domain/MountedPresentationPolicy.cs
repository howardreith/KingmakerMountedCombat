using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Known issues Sprint 7 (U01): what the movement prediction slots may show once the pair's own movement authority has
    // closed locomotion for the activation. A presentation projection only: it reads the facts the authority already
    // reads and changes no cooldown, TimeMoved, allocation, path or action state.
    public static class MountedMovementDisplayPolicy
    {
        // The exact fact MountedMovementState.Remaining refuses further travel on (forced travel recorded), on the
        // allocation granted to this very activation. An allocation of another activation, an unprepared allocation or
        // no live activation never closes anything.
        public static bool LocomotionClosed(string liveActivation, string allocationGrant, bool allocationPrepared, bool forcedTravelRecorded) =>
            !string.IsNullOrEmpty(liveActivation) && allocationPrepared && forcedTravelRecorded &&
            string.Equals(liveActivation, allocationGrant, StringComparison.Ordinal);

        // The usable distance a slot may show: zero while closed (an absent value stays absent), native otherwise.
        public static float? UsableDistanceFeet(bool closed, float? native) => closed && native.HasValue ? 0f : native;

        // A five-foot step is promised only where native offers one and the activation has not closed travel.
        public static bool PromisesFiveFootStep(bool closed, bool nativeHasStep) => !closed && nativeHasStep;
    }

    // Known issues Sprint 7 (U02): the exact visible public slot whose click handed native target selection to a hidden
    // delivery ability. Every field is an identity, never a name, title, icon or bare index.
    public sealed class MountedTargetingSlotIdentity : IEquatable<MountedTargetingSlotIdentity>
    {
        public readonly long Slot;            // the mechanic slot object (its weak serial)
        public readonly long SlotBinding;     // the public AbilityData the slot holds (its weak serial)
        public readonly Guid PairOwner;       // the relationship owner of the action owner, or empty
        public readonly long PairGeneration;  // that relationship's generation
        public readonly int PairState;        // that relationship's state (a dismount ends the mapping)
        public readonly string ActionOwner;   // the unit's unique id
        public readonly string PublicAbility; // public blueprint asset id
        public readonly string DeliveryAbility; // delivery blueprint asset id
        public readonly long Delivery;        // the delivery AbilityData (its weak serial)
        public readonly long Operation;       // the native target-selection operation (selection serial)

        public MountedTargetingSlotIdentity(long slot, long slotBinding, Guid pairOwner, long pairGeneration, int pairState, string actionOwner,
            string publicAbility, string deliveryAbility, long delivery, long operation)
        {
            Slot = slot; SlotBinding = slotBinding; PairOwner = pairOwner; PairGeneration = pairGeneration; PairState = pairState;
            ActionOwner = actionOwner; PublicAbility = publicAbility; DeliveryAbility = deliveryAbility;
            Delivery = delivery; Operation = operation;
        }

        public bool Equals(MountedTargetingSlotIdentity other) => other != null && Slot == other.Slot &&
            SlotBinding == other.SlotBinding && PairOwner == other.PairOwner && PairGeneration == other.PairGeneration && PairState == other.PairState &&
            string.Equals(ActionOwner, other.ActionOwner, StringComparison.Ordinal) &&
            string.Equals(PublicAbility, other.PublicAbility, StringComparison.Ordinal) &&
            string.Equals(DeliveryAbility, other.DeliveryAbility, StringComparison.Ordinal) &&
            Delivery == other.Delivery && Operation == other.Operation;

        public override bool Equals(object obj) => Equals(obj as MountedTargetingSlotIdentity);
        public override int GetHashCode() => Slot.GetHashCode() ^ (Operation.GetHashCode() * 397) ^ Delivery.GetHashCode();
    }

    public static class MountedTargetingGlowPolicy
    {
        // A mapping is recorded only for the click that swapped in a delivery, ran native's click to its end, got its
        // public identity back, and left native target selection holding exactly that delivery.
        public static bool MayRecord(bool deliverySwapped, bool publicIdentityRestored, bool selectionHoldsDelivery, bool serialsKnown) =>
            deliverySwapped && publicIdentityRestored && selectionHoldsDelivery && serialsKnown;

        // The queried slot glows only when every identity read live now equals the recorded one.
        public static bool Glows(MountedTargetingSlotIdentity recorded, MountedTargetingSlotIdentity live) =>
            recorded != null && recorded.Equals(live);

        // The targeting lifetime ended (another selection operation, a drop, a delivery no longer selected): the mapping
        // goes. A different slot being drawn is not an end.
        public static bool Retains(MountedTargetingSlotIdentity recorded, long currentOperation, long selectedDelivery) =>
            recorded != null && recorded.Operation == currentOperation && recorded.Delivery == selectedDelivery;
    }

    // Known issues Sprint 7 (U04): one live relationship as presentation reads it. Built afresh from the relationship each
    // time; never saved.
    public sealed class MountedPairPresentationFact
    {
        public readonly Guid Owner;
        public readonly long Generation;
        public readonly string RiderId;
        public readonly string MountId;
        public readonly bool Mounted;
        // The mount's own native tracker entry is folded into the pair's live paired activation right now.
        public readonly bool MountEntryFolded;

        public MountedPairPresentationFact(Guid owner, long generation, string riderId, string mountId, bool mounted, bool mountEntryFolded)
        {
            Owner = owner; Generation = generation; RiderId = riderId; MountId = mountId; Mounted = mounted;
            MountEntryFolded = mountEntryFolded;
        }
    }

    public sealed class MountedPairPresentationIdentity : IEquatable<MountedPairPresentationIdentity>
    {
        public readonly Guid Owner;
        public readonly long Generation;
        public readonly string RiderId;
        public readonly string MountId;

        public MountedPairPresentationIdentity(Guid owner, long generation, string riderId, string mountId)
        {
            Owner = owner; Generation = generation; RiderId = riderId; MountId = mountId;
        }

        public bool Equals(MountedPairPresentationIdentity other) => other != null && Owner == other.Owner &&
            Generation == other.Generation && string.Equals(RiderId, other.RiderId, StringComparison.Ordinal) &&
            string.Equals(MountId, other.MountId, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as MountedPairPresentationIdentity);
        public override int GetHashCode() => Owner.GetHashCode() ^ Generation.GetHashCode();
    }

    public static class MountedPairPresentationPolicy
    {
        // The badge on one portrait: the single live mounted relationship whose rider is exactly that unit.
        public static MountedPairPresentationIdentity Badge(string portraitUnit, IEnumerable<MountedPairPresentationFact> pairs)
        {
            if (string.IsNullOrEmpty(portraitUnit) || pairs == null) return null;
            MountedPairPresentationIdentity found = null;
            foreach (var pair in pairs)
            {
                if (pair == null || !pair.Mounted || pair.Owner == Guid.Empty || string.IsNullOrEmpty(pair.MountId) ||
                    !string.Equals(pair.RiderId, portraitUnit, StringComparison.Ordinal)) continue;
                if (found != null) return null;
                found = new MountedPairPresentationIdentity(pair.Owner, pair.Generation, pair.RiderId, pair.MountId);
            }
            return found;
        }

        // The tracker entry a hover of a folded mount highlights: the index of its own rider's existing entry, only when
        // native found no entry of its own for the hovered unit, exactly one live mounted relationship folds that mount,
        // and exactly one entry is that rider's. -1 leaves native's hover untouched.
        public static int FoldedMountHoverEntry(string hoveredUnit, bool nativeEntryFound,
            IEnumerable<MountedPairPresentationFact> pairs, IList<string> entryUnits)
        {
            if (nativeEntryFound || string.IsNullOrEmpty(hoveredUnit) || pairs == null || entryUnits == null) return -1;
            MountedPairPresentationFact folded = null;
            foreach (var pair in pairs)
            {
                if (pair == null || !pair.Mounted || !pair.MountEntryFolded || pair.Owner == Guid.Empty ||
                    !string.Equals(pair.MountId, hoveredUnit, StringComparison.Ordinal)) continue;
                if (folded != null) return -1;
                folded = pair;
            }
            if (folded == null || string.IsNullOrEmpty(folded.RiderId)) return -1;
            var index = -1;
            for (var i = 0; i < entryUnits.Count; i++)
            {
                if (string.Equals(entryUnits[i], hoveredUnit, StringComparison.Ordinal)) return -1;
                if (!string.Equals(entryUnits[i], folded.RiderId, StringComparison.Ordinal)) continue;
                if (index >= 0) return -1;
                index = i;
            }
            return index;
        }

        // Review of 89ace80f (R5): a pair's cleanup ends the active hover redirect only when it is exactly the pair the
        // redirect was taken for: the same pair owner, mounted-pair generation, rider and mount. An unestablished identity
        // (null, or an owner that is not set) ends nothing.
        public static bool CleanupOwnsRedirect(MountedPairPresentationIdentity cleanupPair, MountedPairPresentationIdentity redirectPair) =>
            cleanupPair != null && redirectPair != null && cleanupPair.Owner != Guid.Empty &&
            !string.IsNullOrEmpty(cleanupPair.RiderId) && !string.IsNullOrEmpty(cleanupPair.MountId) && cleanupPair.Equals(redirectPair);
    }
}
