using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Integration
{
    // A saved pending split can have an already-spent rider outside native combat
    // while its mount and the rest of the party retain the same turn allocation.
    // Native group joining must not give that rider a second preparation before
    // the retained round ends. This policy identifies only that archived shape;
    // it never changes saved Prepared or any native resource.
    internal static class RetainedNativeReentryPolicy
    {
        internal static string[] SpentPendingRiders(MountedArchiveData archive)
        {
            var combat = archive?.Combat;
            if (combat?.TurnBased != true || combat.Round <= 0) return new string[0];
            var selected = new List<string>();
            foreach (var pair in archive.Pairs)
            {
                var pending = archive.Participations.SingleOrDefault(row => row.OwnerId == pair.PairId &&
                    row.MountedPairId == pair.PairId && row.State != null && row.State.RiderId == null && row.State.MountId == null &&
                    row.State.Activation == null && row.State.PendingSplitId == pair.Mount.Id &&
                    row.State.PendingSplitRound == combat.Round);
                if (pending == null) continue;
                var rider = combat.Actors.SingleOrDefault(row => row.Native.Id == pair.Rider.Id);
                var mount = combat.Actors.SingleOrDefault(row => row.Native.Id == pair.Mount.Id);
                var riderGrant = combat.Allocations.SingleOrDefault(row => row.ActorId == pair.Rider.Id);
                var mountGrant = combat.Allocations.SingleOrDefault(row => row.ActorId == pair.Mount.Id);
                if (rider == null || mount == null || rider.InCombat || rider.Prepared || !mount.InCombat ||
                    combat.Roster.Any(row => row.ActorId == pair.Rider.Id) ||
                    riderGrant == null || mountGrant == null || !riderGrant.Prepared || !mountGrant.Prepared ||
                    riderGrant.Round != combat.Round || mountGrant.Round != combat.Round ||
                    riderGrant.RoundStartTicks != combat.RoundStartTicks ||
                    mountGrant.RoundStartTicks != combat.RoundStartTicks ||
                    riderGrant.GrantId == null || riderGrant.GrantId != mountGrant.GrantId ||
                    !(rider.Native.Standard > 0 || rider.Native.Move > 0 || rider.Native.Swift > 0)) continue;
                selected.Add(pair.Rider.Id);
            }
            return selected.ToArray();
        }

        internal static bool SameRetainedRound(SavedCombatData saved, int nativeRound,
            long nativeStartTicks, long nativeRoundStartTicks) =>
            saved != null && saved.TurnBased && nativeRound == saved.Round &&
            nativeStartTicks == saved.StartTicks && nativeRoundStartTicks == saved.RoundStartTicks;
    }
}
