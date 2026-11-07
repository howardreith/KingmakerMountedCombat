using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // FALL-01/FALL-02 (D5): a living rider actually mounted when that exact mount newly dies, collapses or falls prone
    // is unseated once and attempts one Mobility check against DC 15 in place of Ride. Success lands safely; failure,
    // or a rider whose native condition permits no check, takes one native 1d6 fall and native prone. This is not the
    // Mounted Combat reaction: no feat, preference, Swift action or defensive use is involved.
    internal enum MountedRiderFallCause
    {
        None,
        MountDied,
        // Conscious to Unconscious: the mount collapses without dying.
        MountCollapsed,
        MountFellProne
    }

    internal enum MountedRiderFallRefusal
    {
        None,
        // Rider-only incapacity, voluntary dismount, party removal, area, load, view, disable and stale-pair cleanup
        // never reach the fall: only these three causes on the exact ridden mount do.
        NotAFallCause,
        NotTheMountOfAMountedPair,
        // A naturally dead rider receives no soft-fall operation.
        RiderDead,
        // Loading or restoring a saved state is not a new collapse.
        NotGameplay,
        // This relationship generation already fell; later prone/death/detach callbacks of that fall replay nothing.
        AlreadyFell
    }

    internal enum MountedRiderFallInability
    {
        None,
        Unconscious,
        Helpless,
        Stunned,
        Dazed,
        CannotAct
    }

    internal enum MountedRiderFallOutcome
    {
        None,
        SafeLanding,
        HardLanding
    }

    internal sealed class MountedRiderFallFacts
    {
        internal MountedRiderFallCause Cause { get; set; }
        internal bool ExactMountOfMountedPair { get; set; }
        internal bool RiderAlive { get; set; }
        internal bool Gameplay { get; set; }
    }

    internal sealed class MountedRiderConditionFacts
    {
        internal bool Conscious { get; set; }
        internal bool Helpless { get; set; }
        internal bool Stunned { get; set; }
        internal bool Dazed { get; set; }
        internal bool CantAct { get; set; }
    }

    internal static class MountedRiderFallPolicy
    {
        internal const int DifficultyClass = 15;
        internal const int DamageDice = 1, DamageDieSides = 6;

        internal static MountedRiderFallRefusal Admit(MountedRiderFallFacts facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (facts.Cause == MountedRiderFallCause.None) return MountedRiderFallRefusal.NotAFallCause;
            if (!facts.ExactMountOfMountedPair) return MountedRiderFallRefusal.NotTheMountOfAMountedPair;
            if (!facts.Gameplay) return MountedRiderFallRefusal.NotGameplay;
            return facts.RiderAlive ? MountedRiderFallRefusal.None : MountedRiderFallRefusal.RiderDead;
        }

        // A native life transition is a fall only when it leaves Conscious. Unconscious to Dead after the rider was
        // already unseated is the same fall's continuation, not a second one.
        internal static MountedRiderFallCause CauseOfLifeTransition(string previousLifeState, string currentLifeState)
        {
            if (!string.Equals(previousLifeState, "Conscious", StringComparison.Ordinal)) return MountedRiderFallCause.None;
            if (string.Equals(currentLifeState, "Dead", StringComparison.Ordinal)) return MountedRiderFallCause.MountDied;
            return string.Equals(currentLifeState, "Unconscious", StringComparison.Ordinal) ? MountedRiderFallCause.MountCollapsed : MountedRiderFallCause.None;
        }

        // Native conditions, not animation state: a rider mid-swing can still twist into a landing.
        internal static MountedRiderFallInability Inability(MountedRiderConditionFacts rider)
        {
            if (rider == null) throw new ArgumentNullException(nameof(rider));
            if (!rider.Conscious) return MountedRiderFallInability.Unconscious;
            if (rider.Helpless) return MountedRiderFallInability.Helpless;
            if (rider.Stunned) return MountedRiderFallInability.Stunned;
            if (rider.Dazed) return MountedRiderFallInability.Dazed;
            return rider.CantAct ? MountedRiderFallInability.CannotAct : MountedRiderFallInability.None;
        }

        // The native RuleSkillCheck passes a result that meets the DC (after its own difficulty adjustment); the service reads
        // IsPassed and records whether this comparison agrees.
        internal static bool Passes(int rollResult, int dc) => rollResult >= dc;

        internal static MountedRiderFallOutcome Outcome(MountedRiderFallInability inability, bool checkPassed) =>
            inability == MountedRiderFallInability.None && checkPassed ? MountedRiderFallOutcome.SafeLanding : MountedRiderFallOutcome.HardLanding;

        internal static bool DealsDamage(MountedRiderFallOutcome outcome) => outcome == MountedRiderFallOutcome.HardLanding;

        // Prone goes through the native knock-off rule: an immune or already prone rider is left as the native rule
        // leaves it, and a safe landing never clears an existing prone.
        internal static bool ImposesProne(MountedRiderFallOutcome outcome, bool nativeCanBeKnockedOff) =>
            outcome == MountedRiderFallOutcome.HardLanding && nativeCanBeKnockedOff;

        internal static string Describe(MountedRiderFallCause cause)
        {
            switch (cause)
            {
                case MountedRiderFallCause.MountDied: return "dies";
                case MountedRiderFallCause.MountCollapsed: return "collapses";
                case MountedRiderFallCause.MountFellProne: return "falls prone";
                default: return "falls";
            }
        }

        internal static string Describe(MountedRiderFallInability inability)
        {
            switch (inability)
            {
                case MountedRiderFallInability.Unconscious: return "unconscious";
                case MountedRiderFallInability.Helpless: return "helpless";
                case MountedRiderFallInability.Stunned: return "stunned";
                case MountedRiderFallInability.Dazed: return "dazed";
                case MountedRiderFallInability.CannotAct: return "unable to act";
                default: return null;
            }
        }

        // The one explanatory combat-log line; the native check and damage keep their own entries.
        internal static string LogText(string mountName, string riderName, MountedRiderFallCause cause, MountedRiderFallInability inability,
            int checkResult, int dc, MountedRiderFallOutcome outcome, int? damage, bool prone, string proneRefusal)
        {
            var head = mountName + " " + Describe(cause) + "; " + riderName + " is thrown";
            var check = inability != MountedRiderFallInability.None
                ? " and cannot attempt a soft landing (" + Describe(inability) + ")"
                : ": Mobility " + checkResult + " vs DC " + dc;
            if (outcome == MountedRiderFallOutcome.SafeLanding) return head + check + " - lands safely.";
            var result = " - falls hard";
            if (damage.HasValue) result += ", " + damage.Value + " damage";
            if (prone) result += ", knocked prone";
            else if (proneRefusal != null) result += ", not knocked prone (" + proneRefusal + ")";
            return head + check + result + ".";
        }
    }

    // One fall per relationship generation. The event is captured before ordinary detachment loses the association
    // and resolved only once the rider is actually unseated; a remount advances the generation, so a later collapse
    // is a new fall.
    internal sealed class MountedRiderFallLedger<TPair> where TPair : class
    {
        // The last seat that fell per relationship. The seat is the exact mounted custody object: a remount and a saved pair
        // restored by a load are new seats even where a restored generation number repeats. Custody objects compare by
        // reference; a value key (relationship and generation) is the fallback when no custody object exists.
        private readonly Dictionary<TPair, object> fallenSeat = new Dictionary<TPair, object>();
        private readonly List<MountedRiderFallEvent<TPair>> pending = new List<MountedRiderFallEvent<TPair>>();
        private long sequence;

        internal int PendingCount => pending.Count;
        internal long BegunCount => sequence;

        internal MountedRiderFallRefusal Begin(TPair pair, long generation, object seat, MountedRiderFallFacts facts, string riderId, string mountId,
            out MountedRiderFallEvent<TPair> fall)
        {
            fall = null;
            var refusal = MountedRiderFallPolicy.Admit(facts);
            if (refusal != MountedRiderFallRefusal.None) return refusal;
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            if (seat == null) throw new ArgumentNullException(nameof(seat));
            object fallen;
            if (fallenSeat.TryGetValue(pair, out fallen) && Equals(fallen, seat)) return MountedRiderFallRefusal.AlreadyFell;
            fallenSeat[pair] = seat;
            fall = new MountedRiderFallEvent<TPair>(++sequence, pair, generation, facts.Cause, riderId, mountId);
            pending.Add(fall);
            return MountedRiderFallRefusal.None;
        }

        // Loading or restoring replaces the world: nothing captured before it may land afterwards.
        internal MountedRiderFallEvent<TPair>[] AbandonAll(string reason)
        {
            var taken = pending.ToArray();
            foreach (var fall in taken) fall.AbandonedReason = reason;
            pending.Clear();
            return taken;
        }

        // Returns the pending falls of this pair, in capture order, and retires them: each resolves exactly once.
        internal MountedRiderFallEvent<TPair>[] TakeUnseated(TPair pair, Func<MountedRiderFallEvent<TPair>, bool> unseated)
        {
            if (unseated == null) throw new ArgumentNullException(nameof(unseated));
            var taken = new List<MountedRiderFallEvent<TPair>>();
            for (var index = 0; index < pending.Count; index++)
            {
                var fall = pending[index];
                if ((pair == null || ReferenceEquals(fall.Pair, pair)) && unseated(fall)) taken.Add(fall);
            }
            foreach (var fall in taken) pending.Remove(fall);
            return taken.ToArray();
        }

        // A pending fall whose relationship moved on without unseating this rider (remounted, disposed) is abandoned with
        // its reason rather than resolved against a different seat.
        internal MountedRiderFallEvent<TPair>[] TakeAbandoned(Func<MountedRiderFallEvent<TPair>, string> abandonedReason)
        {
            if (abandonedReason == null) throw new ArgumentNullException(nameof(abandonedReason));
            var taken = new List<MountedRiderFallEvent<TPair>>();
            foreach (var fall in pending)
            {
                var reason = abandonedReason(fall);
                if (reason != null) { fall.AbandonedReason = reason; taken.Add(fall); }
            }
            foreach (var fall in taken) pending.Remove(fall);
            return taken.ToArray();
        }
    }

    internal sealed class MountedRiderFallEvent<TPair> where TPair : class
    {
        internal MountedRiderFallEvent(long sequence, TPair pair, long generation, MountedRiderFallCause cause, string riderId, string mountId)
        {
            Sequence = sequence; Pair = pair; Generation = generation; Cause = cause; RiderId = riderId; MountId = mountId;
        }

        internal long Sequence { get; }
        internal TPair Pair { get; }
        internal long Generation { get; }
        internal MountedRiderFallCause Cause { get; }
        internal string RiderId { get; }
        internal string MountId { get; }
        internal string AbandonedReason { get; set; }
    }
}
