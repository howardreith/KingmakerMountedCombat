using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    // The casting fixture's own native summons (the converted full-round row) are fixture residue
    // once their cast settles: a live summon owns a foreign turn-based turn and joins the leased
    // party group, so the next row's turn handling refuses a foreign actor or the lease reports
    // changed membership (frozen preview.202 TB stages 2 and 4). The row releases its exact summons
    // through native destruction and may advance only when none of them remains in state or in the
    // world; the drain applies the same predicate again at cleanup.
    internal static class CastingSummonResidue
    {
        internal static bool Remains<TUnit>(IReadOnlyList<TUnit> summons, Func<TUnit, bool> inState, Func<TUnit, bool> worldContains)
        {
            if (summons == null) throw new ArgumentNullException(nameof(summons));
            if (inState == null) throw new ArgumentNullException(nameof(inState));
            if (worldContains == null) throw new ArgumentNullException(nameof(worldContains));
            for (var index = 0; index < summons.Count; index++)
            {
                var unit = summons[index];
                if (unit == null) throw new InvalidOperationException("Fixture summon registry holds a null unit.");
                if (inState(unit) || worldContains(unit)) return true;
            }
            return false;
        }
    }
}
