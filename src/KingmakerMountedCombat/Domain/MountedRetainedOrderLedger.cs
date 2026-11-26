using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Where native accepted a command into a pair actor's container (UnitCommands, Kingmaker 2.1.7b): a slot (the private
    // Run overload 060026B3 placed it, reached by the public Run, queue promotion and a custom ExecuteBefore) or the queue
    // (AddToQueueInternal 060026B8, reached by AddToQueue/AddToQueueFirst, Run's queue-instead path and custom handlers).
    // A refused submission (unconscious owner, foreign executor, a KMC admission refusal) or a merge into an already
    // running command reaches neither, so it is never observed as new work.
    public enum MountedAcceptedWorkRoute
    {
        Slot,
        Queue,
        // Native queue promotion (private Run with fromQueue): the same command, already accepted into the queue earlier.
        Promotion
    }

    // Where the private Run overload's postfix observed a command (review R5-F5, PR-1 review R1-F11): promotion (native fromQueue)
    // only when the promoted command is now installed in its slot (a refused promotion installed nothing and wrote nothing);
    // otherwise its slot or the queue (Run's queue-instead path); a refused or merged submission is in neither.
    public static class MountedAcceptedWorkRoutes
    {
        public static MountedAcceptedWorkRoute? AfterRun(bool fromQueue, bool installedInSlot, bool inQueue) =>
            fromQueue ? (installedInSlot ? (MountedAcceptedWorkRoute?)MountedAcceptedWorkRoute.Promotion : null)
            : installedInSlot ? MountedAcceptedWorkRoute.Slot : inQueue ? MountedAcceptedWorkRoute.Queue : (MountedAcceptedWorkRoute?)null;
    }

    public enum MountedAcceptedWorkClass
    {
        // The retained order's own wrapper or one of the exact delegated moves it issued (a repath replacement included).
        RetainedOwn,
        // Another pair attack command: the pair's own command, never a newer order against itself.
        PairCommand,
        // A native reaction (attack of opportunity).
        Reaction,
        // Brain (AI) or other non-player work.
        NotPlayerWork,
        // A Free or Swift action that is started or already in reach: it coexists with the attack and moves nobody.
        CoexistingFreeOrSwift,
        // Newer player work on either actor: it supersedes the retained order.
        Competing
    }

    public struct MountedAcceptedWorkFacts
    {
        public bool PairCommand;
        public bool Reaction;
        public bool AiOrigin;
        public bool PlayerOrder;
        public bool FreeOrSwift;
        // Not started and not yet close enough to its own target: native would move its actor (the mount carries the pair).
        public bool NeedsApproach;
    }

    // MMR-03 and the real-time order lifecycle (reviews R3-F3, R1-F5, R2-F5, R2-F1). One retained ordinary order (one stock
    // intent generation): its attack kind is fixed when the order is accepted, the exact commands it issued are recorded by
    // identity as it issues them, and every command native later ACCEPTS into either actor's container is classified once at
    // acceptance. Competing work latches for the order's lifetime, whether it stays queued, runs or has already finished.
    // PR-1 round-4 lifecycle correction (docs/missions/mmr-split/PR-1-lifecycle-record.md). What one actor's terminal means for
    // the order generation that issued it.
    public enum MountedOrderTerminalDisposition
    {
        // The terminal is not the current order's command (the intent no longer owns it).
        NotOrdersCommand,
        // The terminal kind keeps the order (MountedAttackTerminalPolicy.RetainsIntent).
        Keep,
        // Review R1-F3: an evidenced rider-only refusal with zero delivery: the rider attempt ends, the generation stays for one
        // independently legal horse recheck in the same activation; the rider is never redispatched.
        RiderEndedForHorse,
        // The order retires after its attempt (delivered, or failed for the order as a whole).
        Retire,
        // The order retires without execution (a player's order is answered once).
        RetireUnexecuted
    }

    public struct MountedOrderTerminalFacts
    {
        public bool OwnedByOrder, OrderCurrent, RetainsIntent, EndedWithoutExecution;
        public bool TurnBased, AutomaticOrder, MountActorOrder, MeleeOrder;
        public bool RiderMeleeCommand, CapabilityIsCause, TargetInvalid;
        // Delivery evidence of the ending wrapper: its own OnAction ran, an attack rule was raised, or attacks completed.
        // Native IsActed is NOT delivery: UnitCommand.ForceFinish sets it without calling OnAction (review R1-F3).
        public bool Delivered, AttackRuleObserved;
        public int CompletedAttacks;
    }

    // Review R2-F1/R3-F1/R3-F2: what retiring an order does to its exact wrapper.
    public enum MountedWrapperRetirement
    {
        // No live wrapper.
        None,
        // The first strike was delivered: native owns its completion; the order still retires, so it cannot repeat.
        LeaveToNative,
        // Unreleased and holding its slot (started or installed): marked retired and interrupted.
        InterruptInSlot,
        // Unreleased and only queued: marked retired, removed from the native queue; a late promotion is refused.
        RefuseQueued
    }

    // Review R1-F11: ownership of one actor's manual target by the order generation that wrote it. Any actual assignment by
    // anything else (the same target included) takes the selection over; a queue insertion or a refused promotion writes
    // nothing and changes nothing. A write made inside a click that issued this order (the click handler then assigns the same
    // target after its Run) stays the order's.
    public sealed class MountedManualTargetOwnership
    {
        public long Generation { get; private set; } = -1;
        public object Target { get; private set; }
        public long WrittenInClick { get; private set; } = -1;
        public bool Owned => Generation >= 0;

        // The pair's own write. An actor-specific write outside any order generation (generation < 0) owns nothing and ends any
        // earlier ownership: it is the actor's newest selection.
        public void RecordOwnWrite(long generation, object target, long clickSerial)
        {
            if (generation < 0 || target == null) { Release(); return; }
            Generation = generation; Target = target; WrittenInClick = clickSerial;
        }

        // An actual assignment of this actor's selection by a command that is not the order's own.
        public void ObserveForeignAssignment() => Release();

        // ClickUnitHandler.OnClick's own post-Run assignment: a selection equal to the clicked unit, written by a click that did
        // not issue this order, is the player's.
        public void ObserveClickAssignment(long clickSerial, object currentSelection, object clicked)
        {
            if (Owned && clicked != null && WrittenInClick != clickSerial && ReferenceEquals(currentSelection, clicked)) Release();
        }

        // Retirement of the generation clears the selection only if this generation still owns it and it is still that target.
        public bool ClearsOnRetirement(long generation, object currentSelection) =>
            Owned && Generation == generation && currentSelection != null && ReferenceEquals(Target, currentSelection);

        public void Release() { Generation = -1; Target = null; WrittenInClick = -1; }
    }

    public sealed class MountedRetainedOrderLedger
    {
        // Review R2-F4: the rider's evidenced refusal, bound to its generation. Horse admission does not settle it; the horse's
        // actual delivery settles it silently; a final retirement with neither actor delivering presents it once; every
        // replacement or cancellation (Begin/Clear) drops it.
        private long refusalGeneration = -1;
        private string pendingRefusal;
        public void HoldRiderRefusal(long generation, string message)
        {
            if (!IsCurrent(generation) || string.IsNullOrEmpty(message)) return;
            refusalGeneration = generation; pendingRefusal = message;
        }
        public bool HasPendingRefusal(long generation) => IsCurrent(generation) && refusalGeneration == generation && pendingRefusal != null;
        public void SettleRefusalDelivered(long generation) { if (refusalGeneration == generation) DropRefusal(); }
        public string TakeRefusalOnRetirement(long generation)
        {
            if (!HasPendingRefusal(generation)) return null;
            var message = pendingRefusal;
            DropRefusal();
            return message;
        }
        private void DropRefusal() { refusalGeneration = -1; pendingRefusal = null; }

        // Review R1-F3: the terminal disposition from the recorded native facts of the ending wrapper.
        public static MountedOrderTerminalDisposition DecideTerminal(MountedOrderTerminalFacts facts)
        {
            if (!facts.OwnedByOrder) return MountedOrderTerminalDisposition.NotOrdersCommand;
            if (facts.RetainsIntent) return MountedOrderTerminalDisposition.Keep;
            if (facts.TurnBased && !facts.AutomaticOrder && !facts.MountActorOrder && facts.RiderMeleeCommand && facts.CapabilityIsCause &&
                !facts.Delivered && !facts.AttackRuleObserved && facts.CompletedAttacks == 0 && !facts.TargetInvalid && facts.OrderCurrent &&
                facts.MeleeOrder)
                return MountedOrderTerminalDisposition.RiderEndedForHorse;
            return facts.EndedWithoutExecution ? MountedOrderTerminalDisposition.RetireUnexecuted : MountedOrderTerminalDisposition.Retire;
        }

        // Review R2-F1/R3-F1/R3-F2: one decision for every retirement path (turn-based supersession, real-time retirement,
        // perception loss): an unreleased wrapper is stopped (slot) or refused (queue), a released strike is left to native.
        public static MountedWrapperRetirement RetireWrapper(bool present, bool finished, bool released, bool holdsSlot)
        {
            if (!present || finished) return MountedWrapperRetirement.None;
            if (released) return MountedWrapperRetirement.LeaveToNative;
            return holdsSlot ? MountedWrapperRetirement.InterruptInSlot : MountedWrapperRetirement.RefuseQueued;
        }

        // PR-1 review R1-F3: the rider's attempt for this generation ended for the rider's own (evidenced) capability block: the
        // rider is not redispatched; an independently eligible horse may still follow through on the valid target.
        private long riderEndedGeneration = -1;
        public void MarkRiderEnded(long generation) { if (IsCurrent(generation)) riderEndedGeneration = generation; }
        public bool RiderEnded(long generation) => IsCurrent(generation) && riderEndedGeneration == generation;

        private readonly List<object> own = new List<object>();
        private int competingCount;

        public long Generation { get; private set; } = -1;
        public bool Automatic { get; private set; }
        // The order's accepted attack kind (a rider ranged order is not melee), captured at acceptance.
        public bool Melee { get; private set; }
        public bool Overridden { get; private set; }
        public string OverrideDetail { get; private set; }
        public int OwnCommandCount => own.Count;
        public int CompetingCount => competingCount;

        public bool IsCurrent(long generation) => generation >= 0 && generation == Generation;

        public void Begin(long generation, bool automatic, bool melee)
        {
            own.Clear();
            Generation = generation;
            Automatic = automatic;
            Melee = melee;
            Overridden = false;
            OverrideDetail = null;
            competingCount = 0;
            DropRefusal();
        }

        // A click on the automatic order's own target makes it the player's order (MountedStockIntent.PromoteToPlayer).
        public void PromoteToPlayer()
        {
            Automatic = false;
        }

        public void Clear()
        {
            own.Clear();
            Generation = -1;
            Automatic = false;
            Melee = false;
            Overridden = false;
            OverrideDetail = null;
            competingCount = 0;
            DropRefusal();
        }

        // The retained order's wrapper, or an exact move it is about to submit (recorded before native Run sees it).
        public void RecordOwnIssued(long generation, object command)
        {
            if (command == null || !IsCurrent(generation)) return;
            foreach (var item in own)
                if (ReferenceEquals(item, command)) return;
            own.Add(command);
        }

        // Review R4-F8: identities of commands that have finished (and whose acceptance and cleanup callbacks have therefore
        // run) are released; the live carrier, its live repath replacement and anything the caller keeps stay recognized.
        public int ReleaseFinished(System.Func<object, bool> finished)
        {
            if (finished == null) return 0;
            return own.RemoveAll(item => finished(item));
        }

        // Review R4-F3: what counts as newer player work against a retained order. An explicit order yields only to work
        // accepted AFTER it (the acceptance latch), so an older action already occupying a slot when the player clicked never
        // retires it; an automatic order also keeps the occupied-slot protection (any live player operation).
        public static bool NewerPlayerWork(bool automaticOrder, bool livePlayerOperation, bool acceptedAfterOrder) =>
            acceptedAfterOrder || automaticOrder && livePlayerOperation;

        public bool IsRetainedOwn(object command)
        {
            if (command == null || Generation < 0) return false;
            foreach (var item in own)
                if (ReferenceEquals(item, command)) return true;
            return false;
        }

        public static MountedAcceptedWorkClass Classify(bool retainedOwn, MountedAcceptedWorkFacts facts)
        {
            if (retainedOwn) return MountedAcceptedWorkClass.RetainedOwn;
            if (facts.PairCommand) return MountedAcceptedWorkClass.PairCommand;
            if (facts.Reaction) return MountedAcceptedWorkClass.Reaction;
            if (facts.AiOrigin || !facts.PlayerOrder) return MountedAcceptedWorkClass.NotPlayerWork;
            if (facts.FreeOrSwift && !facts.NeedsApproach) return MountedAcceptedWorkClass.CoexistingFreeOrSwift;
            return MountedAcceptedWorkClass.Competing;
        }

        // Called only after native accepted the command (slot or queue). Idempotent: a command observed by both seams (Run's
        // queue-instead path reaches AddToQueueInternal inside Run) latches once.
        public MountedAcceptedWorkClass ObserveAccepted(object command, MountedAcceptedWorkFacts facts, MountedAcceptedWorkRoute route,
            string detail)
        {
            var kind = Classify(IsRetainedOwn(command), facts);
            // Review R5-F5: promotion is not acceptance. Work queued before the order was accepted is older work; work queued
            // after it already latched at its own queue insertion.
            if (kind != MountedAcceptedWorkClass.Competing || Generation < 0 || route == MountedAcceptedWorkRoute.Promotion) return kind;
            if (!Overridden) OverrideDetail = route + ":" + (detail ?? "<command>");
            Overridden = true;
            competingCount++;
            return kind;
        }

        // Turn-based horse follow-through of this order: only a melee order, and only while no newer player work was accepted.
        public bool FollowThroughAllowed(long generation) => IsCurrent(generation) && Melee && !Overridden;

        public bool Superseded(long generation) => IsCurrent(generation) && Overridden;
    }
}
