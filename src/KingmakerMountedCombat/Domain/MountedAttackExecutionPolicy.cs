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

        // Review of fa533e97 (R18): the condition behind native's UnitState.CanAct being false, for a native branch that
        // ended a command because its executor could not act. An attack ban is not a CanAct term and is never named here;
        // a CanAct term this reader does not name (prone, a blocking animation, nausea in turn-based) reads None.
        public static MountedActorCapabilityBlock CanActBlock(MountedActorCapabilityFacts facts)
        {
            if (!facts.Known) return MountedActorCapabilityBlock.None;
            if (facts.Helpless) return MountedActorCapabilityBlock.Helpless;
            if (facts.Stunned) return MountedActorCapabilityBlock.Stunned;
            if (facts.Dazed) return MountedActorCapabilityBlock.Dazed;
            return facts.CantAct ? MountedActorCapabilityBlock.CantAct : MountedActorCapabilityBlock.None;
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

    // Known issues Sprint 6 (A02). Why one pair command ended, recorded where it was ended, apart from the actor's
    // capability snapshot read when it ended. The snapshot is diagnostic evidence; it is the cause only when this says so.
    public enum MountedAttackEndCause
    {
        // Nothing recorded a cause: native ended it (its own interruption, a force-finish, a native order).
        Unrecorded,
        // The command's own execution boundary found its action actor blocked (TerminateIfActionActorBlocked).
        ActorCapability,
        // The pair controller cancelled it: Stop or Hold, a new order, dismount, a load, area or combat boundary, retirement.
        Cancelled,
        // A newer native order on the same actor replaced it (native UnitCommands.Run ended it inside that Run).
        Replaced,
        // The command faulted.
        Fault,
        // Its target became invalid (died, despawned, changed hostility, became unattackable).
        TargetInvalid,
        // Review of 2ae4cc8b (finding 1): its delegated approach failed at the exact movement boundary (the bounded repath
        // allowance exhausted, or the bounded time exceeded while still approaching after a delegated move began).
        ApproachFailed
    }

    // Review of fa533e97 (R18). The exact native branch that ended a command, captured at that branch from native's own
    // computed values (installed 2.1.7b, UnitActionController):
    // - TickCommandTurnBased (0600911D) force-finishes an unstarted command with Success when
    //   !(CanAct && !HasCondition(Nauseated)) or when it has no cooldown slot; the first ForceFinishForTurnBased is that
    //   branch, the second is the approach-time branch (not captured).
    // - TickCommand (0600911E) interrupts at three sites: the state site (ShouldBeInterrupted, the target not in state,
    //   untargetable or a stealthed enemy), the unstarted InterruptAsSoonAsPossible site, and the running site
    //   ((InterruptAsSoonAsPossible && IsInterruptible) or !CanAct, in that order).
    // - UnitAttack.ShouldBeInterrupted (0600266D) is (!IsStarted && Target.IsDead) || Executor.HasCondition(CanNotAttack).
    // Every other native end (the paused previous command, InterruptMove, InterruptAll, a force-finish elsewhere) records
    // None, and None is never a capability cause.
    public enum MountedNativeTerminalBranch
    {
        None,
        TurnBasedCannotAct,
        TurnBasedNoCommandSlot,
        AttackBanned,
        AttackTargetDead,
        TargetUnavailable,
        InterruptAsSoonAsPossible,
        RunningInterruptAsSoonAsPossible,
        RunningCannotAct
    }

    // Review of dffd3e72 (R21). What native computed, as an exact per-invocation fact. The transpiled native method keeps
    // each value in a local of its own invocation (dup, Observe..., stloc right after native computes it) and passes that
    // local to its own terminal call: the facts live and die with the native frame, belong to its own command (ldarg.1),
    // and are never stored statically, so nothing outlives the tick and no other command or nested observation can read
    // them. NotObserved, the local's zero default, is the conservative answer: never a capability.
    public static class MountedNativeTerminalObservation
    {
        public const int NotObserved = 0;
        public const int CanActTrue = 1;
        public const int CanActFalse = 2;
        public const int NotInterrupted = 1;
        public const int InterruptedByOtherTerm = 2;
        public const int InterruptedByAttackBan = 3;

        public static int ObserveCanAct(bool canAct) => canAct ? CanActTrue : CanActFalse;

        // UnitAttack's CanNotAttack term counts only when it returned the true value of this very ShouldBeInterrupted call.
        public static int ObserveShouldBeInterrupted(bool value, bool attackBanTerm) =>
            !value ? NotInterrupted : attackBanTerm ? InterruptedByAttackBan : InterruptedByOtherTerm;
    }

    public static class MountedNativeTerminalBranchPolicy
    {
        // The turn-based force-finish site: native's own canAct value, observed in this invocation where native branched on it.
        public static MountedNativeTerminalBranch TurnBasedForceFinish(int canActObservation) =>
            canActObservation == MountedNativeTerminalObservation.CanActFalse
                ? MountedNativeTerminalBranch.TurnBasedCannotAct : MountedNativeTerminalBranch.TurnBasedNoCommandSlot;

        // The TickCommand state site: this invocation's ShouldBeInterrupted value and whether UnitAttack's CanNotAttack term
        // returned it.
        public static MountedNativeTerminalBranch StateSite(int shouldBeInterruptedObservation)
        {
            if (shouldBeInterruptedObservation == MountedNativeTerminalObservation.InterruptedByAttackBan) return MountedNativeTerminalBranch.AttackBanned;
            if (shouldBeInterruptedObservation == MountedNativeTerminalObservation.InterruptedByOtherTerm) return MountedNativeTerminalBranch.AttackTargetDead;
            return MountedNativeTerminalBranch.TargetUnavailable;
        }

        // The TickCommand running site: !CanAct only when native evaluated CanAct in this invocation and read false; otherwise
        // the site was reached through InterruptAsSoonAsPossible && IsInterruptible, which short-circuits before CanAct.
        public static MountedNativeTerminalBranch RunningSite(int canActObservation) =>
            canActObservation == MountedNativeTerminalObservation.CanActFalse
                ? MountedNativeTerminalBranch.RunningCannotAct : MountedNativeTerminalBranch.RunningInterruptAsSoonAsPossible;

        public static bool IsCapability(MountedNativeTerminalBranch branch) =>
            branch == MountedNativeTerminalBranch.TurnBasedCannotAct || branch == MountedNativeTerminalBranch.AttackBanned ||
            branch == MountedNativeTerminalBranch.RunningCannotAct;

        public static bool IsTarget(MountedNativeTerminalBranch branch) =>
            branch == MountedNativeTerminalBranch.AttackTargetDead || branch == MountedNativeTerminalBranch.TargetUnavailable;

        // The block that branch proves, read at the branch: the attack ban itself, or the condition behind CanAct.
        public static MountedActorCapabilityBlock Block(MountedNativeTerminalBranch branch, MountedActorCapabilityFacts facts)
        {
            if (branch == MountedNativeTerminalBranch.AttackBanned) return MountedActorCapabilityBlock.CanNotAttack;
            if (branch == MountedNativeTerminalBranch.TurnBasedCannotAct || branch == MountedNativeTerminalBranch.RunningCannotAct)
                return MountedActorCapabilityPolicy.CanActBlock(facts);
            return MountedActorCapabilityBlock.None;
        }
    }

    public static class MountedAttackTerminalCausePolicy
    {
        // A recorded cause wins; otherwise an end inside a replacing native Run on the command's own actor is a replacement.
        public static MountedAttackEndCause Resolve(MountedAttackEndCause recorded, bool endedInsideReplacingNativeRun)
        {
            if (recorded != MountedAttackEndCause.Unrecorded) return recorded;
            return endedInsideReplacingNativeRun ? MountedAttackEndCause.Replaced : MountedAttackEndCause.Unrecorded;
        }

        // The capability block that ended the command, or None (review of fa533e97, R18). A recorded ActorCapability cause
        // carries the exact block read at the command's own boundary, whatever the later snapshot. With no recorded cause,
        // only a native capability branch captured where native took it (MountedNativeTerminalBranch) is causal, with the
        // block read at that branch. Neither the terminal result, nor whether the command started, nor the later terminal
        // snapshot ever makes a condition the cause; that snapshot stays diagnostic.
        public static MountedActorCapabilityBlock CausalCapability(MountedAttackEndCause cause, MountedActorCapabilityBlock recordedBlock,
            MountedNativeTerminalBranch nativeBranch, MountedActorCapabilityBlock nativeBranchBlock)
        {
            if (cause == MountedAttackEndCause.ActorCapability) return recordedBlock;
            if (cause != MountedAttackEndCause.Unrecorded) return MountedActorCapabilityBlock.None;
            return MountedNativeTerminalBranchPolicy.IsCapability(nativeBranch) ? nativeBranchBlock : MountedActorCapabilityBlock.None;
        }

        public static bool CapabilityIsCause(MountedAttackEndCause cause, MountedActorCapabilityBlock recordedBlock,
            MountedNativeTerminalBranch nativeBranch, MountedActorCapabilityBlock nativeBranchBlock) =>
            CausalCapability(cause, recordedBlock, nativeBranch, nativeBranchBlock) != MountedActorCapabilityBlock.None;

        // The player text for an explicitly ended command; null when the cause has no text of its own.
        public static string CancellationFeedback(MountedAttackEndCause cause)
        {
            switch (cause)
            {
                case MountedAttackEndCause.Cancelled: return "Mounted pair attack cancelled.";
                case MountedAttackEndCause.Replaced: return "Mounted pair attack replaced by a newer order.";
                default: return null;
            }
        }

        // AI-01's reach-only marking is for an automatic approach that failed (known issues Sprint 6, A05, as corrected by the
        // review of 2ae4cc8b). Only the typed ApproachFailed cause, recorded at the exact movement boundary, counts, and only
        // with the approach's own evidence: an approach was required at start, a delegated move began, no attack started,
        // and the terminal was neither a success nor a ranged tail. Unrecorded is never evidence of a failed approach;
        // target invalidation, capability, cancellation, replacement and faults never mark a target unreachable.
        public static bool RecordsAutomaticApproachFailure(bool automatic, int childAttackStarts, bool rangedTail, bool nativeSuccess,
            MountedAttackEndCause cause, bool approachRequiredAtStart, int delegatedMoveStarts)
        {
            return automatic && cause == MountedAttackEndCause.ApproachFailed && approachRequiredAtStart && delegatedMoveStarts > 0 &&
                childAttackStarts == 0 && !rangedTail && !nativeSuccess;
        }
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
