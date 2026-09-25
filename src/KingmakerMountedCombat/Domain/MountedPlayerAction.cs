using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedPlayerActionKind
    {
        None,
        Mount,
        Dismount
    }

    public sealed class MountedPlayerActionContext
    {
        public RelationshipState RelationshipState { get; set; }

        public bool GameAvailable { get; set; }

        public bool FeatureEnabled { get; set; }

        public bool ExactlyOneRiderSelected { get; set; }

        public bool RiderIsExactlyMedium { get; set; }

        public bool RiderBodyProfileSupported { get; set; }

        public bool ExactActiveOwnedSupportedMount { get; set; }

        public string MountDisplayName { get; set; }

        public bool MountIsStrictlyLarger { get; set; }

        public bool RiderIsAliveAndConscious { get; set; }

        public bool MountIsAliveAndConscious { get; set; }

        public bool RiderIsDirectlyControllableAndInGame { get; set; }

        public bool MountIsDirectlyControllableAndInGame { get; set; }

        public bool ConflictingMountedRelationship { get; set; }

        public bool UnsupportedPolymorphOrSizeState { get; set; }

        public bool LoadingTransitionOrCutscene { get; set; }

        public bool InCombat { get; set; }

        public bool CombatTurnEligible { get; set; }

        // The exact reason the turn is not eligible, so the Preparing boundary is
        // refused with its own message instead of the generic wrong-turn one.
        public string CombatTurnIneligibilityReason { get; set; }

        public bool RiderHasMoveAction { get; set; }

        public bool NativeMoveActionShellAdmitted { get; set; }

        // A KMC-owned voluntary relationship transition is already in flight. A
        // second native control must not be offered, so repeated input cannot
        // create a second committed Move shell.
        public bool RelationshipTransitionInFlight { get; set; }

        // In combat the paired lifecycle must be able to take over the running
        // encounter unambiguously. When it cannot, the transition is refused
        // before any native commitment rather than guessed at.
        public bool PairedAdoptionAvailable { get; set; } = true;

        public string PairedAdoptionUnavailableReason { get; set; }

        // Combat Mount is supported only on the accepted architecture: paired
        // activation live and both retired turn authorities off. One typed policy
        // decides it for prediction and for execution-time admission alike.
        public bool CombatMountAuthorityQualified { get; set; } = true;

        public string CombatMountAuthorityReason { get; set; }

        public bool PairAdjacent { get; set; }

        public bool SafeGameMode { get; set; }

        public bool ViewsAndStockAgentsAvailable { get; set; }

        public bool StockAgentsReady { get; set; }

        public bool AgentOverridesAvailable { get; set; }
    }

    public sealed class MountedPlayerActionAvailability
    {
        public MountedPlayerActionAvailability(
            bool visible,
            bool enabled,
            MountedPlayerActionKind action,
            string label,
            IReadOnlyList<string> unavailableReasons)
        {
            IsVisible = visible;
            IsEnabled = enabled;
            Action = action;
            Label = label ?? string.Empty;
            UnavailableReasons = unavailableReasons ?? throw new ArgumentNullException(nameof(unavailableReasons));
        }

        public bool IsVisible { get; }

        public bool IsEnabled { get; }

        public MountedPlayerActionKind Action { get; }

        public string Label { get; }

        public IReadOnlyList<string> UnavailableReasons { get; }

        public string Feedback => UnavailableReasons.Count == 0
            ? (Action == MountedPlayerActionKind.Mount ? "Ready to mount." : "Mounted relationship is active.")
            : string.Join(" ", UnavailableReasons);
    }

    public static class MountedPlayerActionEvaluator
    {
        public static MountedPlayerActionAvailability Evaluate(MountedPlayerActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.RelationshipState == RelationshipState.Disposed)
            {
                return Hidden();
            }

            if (context.RelationshipState == RelationshipState.Mounted ||
                context.RelationshipState == RelationshipState.Faulted)
            {
                if (context.RelationshipState == RelationshipState.Faulted)
                {
                    return new MountedPlayerActionAvailability(
                        true,
                        true,
                        MountedPlayerActionKind.Dismount,
                        "Clear mounted state",
                        Array.Empty<string>());
                }

                var dismountReasons = new List<string>();
                if (context.RelationshipTransitionInFlight)
                {
                    dismountReasons.Add("A mounted transition is already in flight.");
                }
                if (context.InCombat && !context.CombatTurnEligible)
                {
                    dismountReasons.Add(string.IsNullOrWhiteSpace(context.CombatTurnIneligibilityReason)
                        ? "Dismount during turn-based combat belongs to the rider-led current turn."
                        : context.CombatTurnIneligibilityReason);
                }
                if (context.InCombat && !context.RiderHasMoveAction &&
                    !context.NativeMoveActionShellAdmitted)
                {
                    dismountReasons.Add("The rider has no Move action available to dismount.");
                }
                return new MountedPlayerActionAvailability(
                    true,
                    dismountReasons.Count == 0,
                    MountedPlayerActionKind.Dismount,
                    "Dismount",
                    dismountReasons);
            }

            if (!context.GameAvailable)
            {
                return Hidden();
            }

            if (context.RelationshipState != RelationshipState.Unmounted)
            {
                return Unavailable(
                    MountedPlayerActionKind.Mount,
                    "Mount",
                    "A mounted relationship transition is already in progress.");
            }

            var reasons = new List<string>();
            var mountName = string.IsNullOrWhiteSpace(context.MountDisplayName)
                ? "supported mount"
                : context.MountDisplayName;
            if (!context.FeatureEnabled)
            {
                reasons.Add("Enable the private-alpha mounted movement feature in this mod's settings.");
            }
            if (context.RelationshipTransitionInFlight)
            {
                reasons.Add("A mounted transition is already in flight.");
            }

            if (!context.ExactlyOneRiderSelected)
            {
                reasons.Add("Select exactly one prospective rider.");
                return new MountedPlayerActionAvailability(
                    true,
                    false,
                    MountedPlayerActionKind.Mount,
                    "Mount",
                    reasons);
            }

            if (!context.RiderIsExactlyMedium)
            {
                reasons.Add("The selected rider must currently be Medium.");
            }
            if (!context.RiderBodyProfileSupported)
            {
                reasons.Add("The selected rider's current body rig is not supported by the private-alpha pose profile.");
            }
            if (!context.ExactActiveOwnedSupportedMount)
            {
                reasons.Add("The selected rider must own an exact active supported mount; nearby creatures are never inferred.");
            }
            if (context.ExactActiveOwnedSupportedMount && !context.MountIsStrictlyLarger)
            {
                reasons.Add("The active " + mountName + " must currently be larger than the rider.");
            }
            if (!context.RiderIsAliveAndConscious || !context.MountIsAliveAndConscious)
            {
                reasons.Add("Rider and " + mountName + " must both be alive and conscious.");
            }
            if (!context.RiderIsDirectlyControllableAndInGame || !context.MountIsDirectlyControllableAndInGame)
            {
                reasons.Add("Rider and " + mountName + " must both be directly controllable in the active area.");
            }
            if (context.ConflictingMountedRelationship)
            {
                reasons.Add("A conflicting mounted relationship is already active.");
            }
            if (context.UnsupportedPolymorphOrSizeState)
            {
                reasons.Add("Polymorphed or otherwise unsupported size states cannot mount.");
            }
            if (context.LoadingTransitionOrCutscene)
            {
                reasons.Add("Mounting is blocked during loading, area transitions, and cutscenes.");
            }
            if (context.InCombat && !context.CombatMountAuthorityQualified)
            {
                reasons.Add(string.IsNullOrWhiteSpace(context.CombatMountAuthorityReason)
                    ? "Mounting during combat requires the qualified paired authority."
                    : context.CombatMountAuthorityReason);
            }
            if (context.InCombat && !context.PairedAdoptionAvailable)
            {
                // The paired lifecycle could not dispose of this round's
                // participation unambiguously. Availability must not advertise a
                // Move shell whose delivery would have to guess.
                reasons.Add(string.IsNullOrWhiteSpace(context.PairedAdoptionUnavailableReason)
                    ? "The mounted pair cannot take over this encounter's activation yet."
                    : context.PairedAdoptionUnavailableReason);
            }
            if (context.InCombat && !context.PairAdjacent)
            {
                reasons.Add("Rider and " + mountName + " must be adjacent to mount during combat.");
            }
            if (context.InCombat && !context.CombatTurnEligible)
            {
                reasons.Add(string.IsNullOrWhiteSpace(context.CombatTurnIneligibilityReason)
                    ? "Mount Companion during turn-based combat belongs to the rider's current turn."
                    : context.CombatTurnIneligibilityReason);
            }
            if (context.InCombat && !context.RiderHasMoveAction &&
                !context.NativeMoveActionShellAdmitted)
            {
                reasons.Add("The rider has no Move action available to mount.");
            }
            if (!context.SafeGameMode)
            {
                reasons.Add("Mounting is available only during ordinary exploration in the active world view.");
            }
            if (!context.ViewsAndStockAgentsAvailable)
            {
                reasons.Add("Rider and " + mountName + " views and stock movement agents must be attached.");
            }
            if (!context.StockAgentsReady)
            {
                reasons.Add("Rider and " + mountName + " stock movement agents must be enabled before mounting.");
            }
            if (!context.AgentOverridesAvailable)
            {
                reasons.Add("Another system already owns an incompatible movement-agent override.");
            }

            return new MountedPlayerActionAvailability(
                true,
                reasons.Count == 0,
                MountedPlayerActionKind.Mount,
                "Mount",
                reasons);
        }

        private static MountedPlayerActionAvailability Hidden()
        {
            return new MountedPlayerActionAvailability(
                false,
                false,
                MountedPlayerActionKind.None,
                string.Empty,
                Array.Empty<string>());
        }

        private static MountedPlayerActionAvailability Unavailable(
            MountedPlayerActionKind action,
            string label,
            string reason)
        {
            return new MountedPlayerActionAvailability(
                true,
                false,
                action,
                label,
                new[] { reason });
        }
    }

    public static class CombatMountDismountPolicy
    {
        public const float NativeAdjacentReachMeters = 1.5f;
        private const float ApproachStopMarginMeters = 0.05f;

        public static bool TryGetMountApproachRadius(float nativeRadius, float riderCorpulence,
            float mountCorpulence, out float radius)
        {
            radius = 0f;
            // AbilityRange.Unlimited is positive infinity in this native build.
            // It is a ceiling to clamp, not an invalid target/body measurement.
            if (float.IsNaN(nativeRadius) || nativeRadius < 0f || !IsFiniteNonNegative(riderCorpulence) ||
                !IsFiniteNonNegative(mountCorpulence)) { return false; }
            // Stop just inside the execution envelope; never enlarge native permission.
            radius = Math.Min(nativeRadius, riderCorpulence + mountCorpulence +
                NativeAdjacentReachMeters - ApproachStopMarginMeters);
            return true;
        }

        // A relationship transition in turn-based combat requires the exact rider's
        // turn to be ACTING, not merely Preparing.
        //
        // Kingmaker's TurnController runs Prepare() at the actor's own initiative
        // slot and only then advances the turn to Acting; a player command is
        // delivered by UnitCommands.Tick inside Acting. Admitting a transition
        // while the turn is still Preparing would settle the transition ledger and
        // the adopted paired grant against a turn whose native preparation has not
        // finished, and there is no exact native evidence that no callback or
        // ledger step is skipped in that window. Preparing is therefore refused
        // rather than retained on an assumption.
        public static bool IsTurnEligible(
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            bool turnActing)
        {
            return !turnBasedCombat || currentTurnIsExactRider && turnActing;
        }

        // The exact reason a turn-based transition is not eligible, so the Preparing
        // boundary is refused with its own message instead of the generic one.
        public static string DescribeTurnIneligibility(
            string actionName,
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            bool turnPreparing,
            bool turnActing)
        {
            if (IsTurnEligible(turnBasedCombat, currentTurnIsExactRider, turnActing))
            {
                return null;
            }
            if (!currentTurnIsExactRider)
            {
                return actionName + " during turn-based combat belongs to the rider's current turn.";
            }
            if (turnPreparing)
            {
                return actionName + " waits until the rider's turn has finished preparing.";
            }
            return actionName + " requires the rider's turn to be acting.";
        }

        public static bool IsAdjacent(
            float centerDistance,
            float riderCorpulence,
            float mountCorpulence)
        {
            if (!IsFiniteNonNegative(centerDistance) ||
                !IsFiniteNonNegative(riderCorpulence) ||
                !IsFiniteNonNegative(mountCorpulence))
            {
                return false;
            }

            return centerDistance <= riderCorpulence + mountCorpulence +
                NativeAdjacentReachMeters;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
