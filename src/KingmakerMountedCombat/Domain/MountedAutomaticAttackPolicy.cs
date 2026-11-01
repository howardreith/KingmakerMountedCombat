using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedAutomaticProposalDecision
    {
        Refuse,
        ContinueOwnIntent,
        Admit
    }

    // One occupied rider command slot as native UnitCommands.Raw holds it.
    public struct MountedAutomaticSlotFacts
    {
        public bool Finished;
        // Native UnitCommand.AiAction is set: the brain's own command, never a player order.
        public bool AiOrigin;
        // The intent's own pair command or a native reaction (attack of opportunity): neither is a newer order.
        public bool PairOwnedOrReaction;
    }

    // Everything the native party-brain proposal is admitted from, read by the controller from the live pair at the
    // UnitCommands.Run boundary.
    public struct MountedAutomaticProposalFacts
    {
        public bool AiOrigin;
        public bool ExactMountedPair;
        public bool TurnBased;
        public bool OwnerIsRider;
        public bool RiderDirectlyControllable;
        public bool RiderAiEnabled;
        public bool RiderBrainActive;
        public bool StopSuppressed;
        public bool MovementForbidden;
        public bool TargetInRiderReach;
        public bool TargetValid;
        public bool ArmedAction;
        public bool PlayerIntentLive;
        public bool AutomaticIntentLive;
        public bool AutomaticIntentOnThisTarget;
        public bool PairCommandActive;
        public bool MountOperationLive;
        public bool NewerPlayerOperation;
    }

    // AI-01. Installed 2.1.7b facts: AiBrainController.SelectAction proposes through UnitCommands.Run with AiAction set
    // and CreatedByPlayer false, about every 0.3 s while no command forbids it (AiCanInterrupt). A proposal's target comes
    // from native perception, hostility, reachability and scoring; nothing here scans or scores a target. The pair
    // controller stays the one dispatcher: an admitted proposal becomes the same intent a click makes, so full-attack
    // planning, cooldowns and the native strike sequence are unchanged, and a new target never buys a new routine.
    public static class MountedAutomaticAttackPolicy
    {
        public static MountedAutomaticProposalDecision Decide(MountedAutomaticProposalFacts facts, out string refusal)
        {
            refusal = FirstRefusal(facts);
            if (refusal != null) return MountedAutomaticProposalDecision.Refuse;
            return facts.AutomaticIntentLive
                ? MountedAutomaticProposalDecision.ContinueOwnIntent
                : MountedAutomaticProposalDecision.Admit;
        }

        private static string FirstRefusal(MountedAutomaticProposalFacts facts)
        {
            if (!facts.AiOrigin) return "not-native-ai-origin";
            if (!facts.ExactMountedPair) return "no-exact-mounted-pair";
            if (facts.TurnBased) return "turn-based";
            if (!facts.OwnerIsRider) return "not-rider-brain";
            // A live player intent is never displaced, whichever target the brain prefers.
            if (facts.PlayerIntentLive) return "player-intent-live";
            if (facts.AutomaticIntentLive)
                return facts.AutomaticIntentOnThisTarget ? null : "automatic-intent-holds-its-target";
            if (facts.ArmedAction) return "armed-action";
            if (facts.PairCommandActive) return "pair-command-active";
            if (facts.MountOperationLive) return "mount-operation-live";
            if (facts.NewerPlayerOperation) return "player-operation-live";
            if (!facts.RiderDirectlyControllable) return "rider-not-directly-controllable";
            if (!facts.RiderAiEnabled) return "ai-off";
            if (!facts.RiderBrainActive) return "brain-inactive";
            if (facts.StopSuppressed) return "stop-suppressed";
            if (!facts.TargetValid) return "target-invalid";
            if (facts.MovementForbidden && !facts.TargetInRiderReach) return "hold-out-of-reach";
            return null;
        }

        // A live rider order the brain did not make, or anything queued on either actor. A queued future order counts
        // for automatic admission: the proposal's native Run would clear that queue.
        public static bool HasPlayerOperation(IEnumerable<MountedAutomaticSlotFacts> riderSlots, int queuedCommands)
        {
            if (queuedCommands > 0) return true;
            foreach (var slot in riderSlots)
                if (!slot.Finished && !slot.AiOrigin && !slot.PairOwnedOrReaction) return true;
            return false;
        }

        // Why a live intent stops repeating. A newer player operation retires an intent of either origin (a repeating
        // dispatch would otherwise replace a cast waiting to start); the rest are the automatic intent's own boundaries,
        // re-read every update because they can change between two proposals.
        public static string RetireReason(
            bool realTime,
            bool automatic,
            bool riderDirectlyControllable,
            bool riderAiEnabled,
            bool riderBrainActive,
            bool stopSuppressed,
            bool movementForbidden,
            bool targetInRiderReach,
            bool playerOperation)
        {
            if (!realTime) return automatic ? "automatic intent left real time" : null;
            if (playerOperation) return "newer player operation";
            if (!automatic) return null;
            if (!riderDirectlyControllable) return "rider no longer directly controllable";
            if (!riderAiEnabled) return "ai off";
            if (!riderBrainActive) return "brain inactive";
            if (stopSuppressed) return "stop";
            if (movementForbidden && !targetInRiderReach) return "hold out of reach";
            return null;
        }
    }

    // The per-pair boundary of the automatic path. Native Stop (SelectionManagerBase.Stop) only clears Hold, unstarted
    // commands and both targets; it arms nothing, so a live brain would propose again within 0.4 s. The gate holds the
    // pair's proposals back until a deliberate new player order, an explicit AI off/on, or the end of the combat, mode
    // or relationship generation. Never persisted: a load starts clear.
    //
    // The player-move hold mirrors native UnitMoveTo.OnAction, which holds the unit that moved after a player order in
    // combat. The mount executes the pair's move, so the rider's own HoldState never records it. The mount's HoldState
    // cannot stand in for it, because the pair command's delegated approach is a UnitMoveTo as well.
    public sealed class MountedAutomaticAttackGate
    {
        private long stopGeneration;
        private bool stopArmed;
        private bool aiOffSinceStop;

        public bool PlayerMoveHold { get; private set; }

        public bool IsSuppressed(long generation)
        {
            return stopArmed && stopGeneration == generation;
        }

        public void ObserveStop(long generation)
        {
            stopArmed = true;
            stopGeneration = generation;
            aiOffSinceStop = false;
            PlayerMoveHold = false;
        }

        public void ObservePlayerMove()
        {
            stopArmed = false;
            PlayerMoveHold = true;
        }

        public void ObservePlayerOrder()
        {
            stopArmed = false;
            PlayerMoveHold = false;
        }

        public void Tick(long generation, bool exactMountedPair, bool inCombat, bool turnBased, bool aiEnabled)
        {
            if (!exactMountedPair || !inCombat || turnBased || stopArmed && stopGeneration != generation)
            {
                Clear();
                return;
            }
            if (!stopArmed) return;
            if (!aiEnabled) aiOffSinceStop = true;
            else if (aiOffSinceStop) stopArmed = false;
        }

        public void Clear()
        {
            stopArmed = false;
            aiOffSinceStop = false;
            PlayerMoveHold = false;
        }
    }
}
