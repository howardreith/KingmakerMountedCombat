namespace KingmakerMountedCombat.Domain
{
    // The native condition that stops one actor from performing an attack.
    public enum MountedActorCapabilityBlock
    {
        None,
        Helpless,
        Stunned,
        Dazed,
        CantAct,
        CanNotAttack
    }

    // One actor's native condition state, read at a decision boundary (installed 2.1.7b). Helpless, Dazed, Stunned and
    // CantAct are the condition terms of UnitState.CanAct; CanNotAttack is what UnitAttack.ShouldBeInterrupted reads.
    // CanAct's other terms (prone, a blocking animation) are execution delays native resolves itself, and the action
    // budget, turn ownership and initiative are separate questions with their own gates: none of them is read here.
    public struct MountedActorCapabilityFacts
    {
        // False when the actor's state could not be read: nothing is refused on an unknown state.
        public bool Known;
        public bool Helpless;
        public bool Stunned;
        public bool Dazed;
        public bool CantAct;
        public bool CanNotAttack;
    }

    // Applied to the actor performing the action only. The partner's state never enters, so a blocked rider leaves the
    // mount's own attack untouched and the reverse.
    public static class MountedActorCapabilityPolicy
    {
        // A started routine about to release its attack. Native interrupts a running command whose executor cannot act
        // or attack on every tick, in both combat modes (UnitActionController.TickCommand).
        public static MountedActorCapabilityBlock ExecutionBlock(MountedActorCapabilityFacts facts)
        {
            if (!facts.Known) return MountedActorCapabilityBlock.None;
            if (facts.CanNotAttack) return MountedActorCapabilityBlock.CanNotAttack;
            if (facts.Helpless) return MountedActorCapabilityBlock.Helpless;
            if (facts.Stunned) return MountedActorCapabilityBlock.Stunned;
            if (facts.Dazed) return MountedActorCapabilityBlock.Dazed;
            return facts.CantAct ? MountedActorCapabilityBlock.CantAct : MountedActorCapabilityBlock.None;
        }

        // A new attack order. In turn-based combat native ends an unstarted command of such an actor at once, with
        // Success and nothing executed (UnitActionController.TickCommandTurnBased), so the order is refused here.
        // Real time has no such terminal and is left alone: native keeps the order of an actor that cannot act pending
        // and starts it when the condition ends, and interrupts an attack-banned actor's order (never Success), which
        // already retires it.
        public static MountedActorCapabilityBlock AdmissionBlock(MountedActorCapabilityFacts facts, bool turnBased)
        {
            return turnBased ? ExecutionBlock(facts) : MountedActorCapabilityBlock.None;
        }

        public static string Feedback(string actorName, MountedActorCapabilityBlock block)
        {
            var actor = string.IsNullOrWhiteSpace(actorName) ? "This actor" : actorName;
            switch (block)
            {
                case MountedActorCapabilityBlock.Helpless: return actor + " cannot act right now (helpless).";
                case MountedActorCapabilityBlock.Stunned: return actor + " cannot act right now (stunned).";
                case MountedActorCapabilityBlock.Dazed: return actor + " cannot act right now (dazed).";
                case MountedActorCapabilityBlock.CantAct: return actor + " cannot act right now.";
                case MountedActorCapabilityBlock.CanNotAttack: return actor + " cannot attack right now.";
                default: return null;
            }
        }
    }

    public enum MountedAttackTerminalKind
    {
        // Native Success and the mounted transaction completed: every planned attack was delivered.
        Completed,
        // The ranged attacks of a mixed routine were delivered and its melee tail was out of reach.
        RangedTailDelivered,
        // Native Success with delivered attacks, but the mounted transaction did not record completion.
        DeliveredWithoutCompletion,
        // Native Success without one delivered attack: the command ended and nothing was executed.
        EndedWithoutExecution,
        // Every other native result.
        Failed
    }

    // What one finished pair command reports about itself. The native result is one fact among these, never the verdict.
    public struct MountedAttackTerminalFacts
    {
        public bool NativeSuccess;
        public bool TransactionCompleted;
        public bool NativeRangedTailTermination;
        // The native attack rule ran at least once; a miss or a hit for no damage ran it as well.
        public bool NativeAttackRuleObserved;
        public int CompletedAttackCount;
    }

    // Installed 2.1.7b: UnitCommand.ForceFinish stamps Success (and IsActed) on a command that never started, and
    // TickCommandTurnBased does that to an unstarted command whose executor cannot act, waits for initiative or has no
    // action for it. The retained order must not survive such a terminal, or every later update dispatches it again.
    public static class MountedAttackTerminalPolicy
    {
        public static MountedAttackTerminalKind Classify(MountedAttackTerminalFacts facts)
        {
            if (facts.NativeRangedTailTermination) return MountedAttackTerminalKind.RangedTailDelivered;
            if (!facts.NativeSuccess) return MountedAttackTerminalKind.Failed;
            if (facts.TransactionCompleted) return MountedAttackTerminalKind.Completed;
            return facts.NativeAttackRuleObserved && facts.CompletedAttackCount > 0
                ? MountedAttackTerminalKind.DeliveredWithoutCompletion
                : MountedAttackTerminalKind.EndedWithoutExecution;
        }

        // Only a command that executed keeps its order alive for the next native budget.
        public static bool RetainsIntent(MountedAttackTerminalKind kind)
        {
            return kind == MountedAttackTerminalKind.Completed ||
                kind == MountedAttackTerminalKind.RangedTailDelivered ||
                kind == MountedAttackTerminalKind.DeliveredWithoutCompletion;
        }

        public static string UnexecutedFeedback(string actorName, MountedActorCapabilityBlock block)
        {
            var blocked = MountedActorCapabilityPolicy.Feedback(actorName, block);
            if (blocked != null) return blocked + " The attack was not performed.";
            var actor = string.IsNullOrWhiteSpace(actorName) ? "This actor" : actorName;
            return actor + " could not perform the attack; the order was dropped.";
        }
    }
}
