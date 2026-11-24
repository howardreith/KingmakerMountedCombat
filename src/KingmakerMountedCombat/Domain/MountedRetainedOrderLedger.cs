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
        Queue
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
    public sealed class MountedRetainedOrderLedger
    {
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
        }

        // The retained order's wrapper, or an exact move it is about to submit (recorded before native Run sees it).
        public void RecordOwnIssued(long generation, object command)
        {
            if (command == null || !IsCurrent(generation)) return;
            foreach (var item in own)
                if (ReferenceEquals(item, command)) return;
            own.Add(command);
        }

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
            if (kind != MountedAcceptedWorkClass.Competing || Generation < 0) return kind;
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
