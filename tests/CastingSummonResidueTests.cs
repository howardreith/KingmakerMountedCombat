using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // Frozen preview.202 TB stages 2/4: the full-round row's live native summon owned a foreign
    // turn-based turn and changed the leased party membership before the next row began.
    internal static class CastingSummonResidueTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("full-round row cannot advance while its exact native summon is still in state", LiveSummonRemains);
            runner.Run("a destroyed summon the world still lists is residue", WorldListedSummonRemains);
            runner.Run("released summons leave no residue and an empty registry is clean", ReleasedSummonsClear);
            runner.Run("a null summon registry entry is a fixture fault, never clean residue", NullEntryFaults);
            runner.Run("summon residue refuses missing predicates", MissingPredicatesRefused);
        }

        private sealed class Unit { internal bool InState; internal bool InWorld; }

        private static void LiveSummonRemains()
        {
            var summons = new[] { new Unit { InState = false, InWorld = false }, new Unit { InState = true, InWorld = true } };
            TestRunner.True(CastingSummonResidue.Remains(summons, u => u.InState, u => u.InWorld), "A live summon did not count as residue.");
        }

        private static void WorldListedSummonRemains()
        {
            var summons = new[] { new Unit { InState = false, InWorld = true } };
            TestRunner.True(CastingSummonResidue.Remains(summons, u => u.InState, u => u.InWorld), "A world-listed summon did not count as residue.");
        }

        private static void ReleasedSummonsClear()
        {
            var summons = new[] { new Unit { InState = false, InWorld = false }, new Unit { InState = false, InWorld = false } };
            TestRunner.True(!CastingSummonResidue.Remains(summons, u => u.InState, u => u.InWorld), "Released summons counted as residue.");
            TestRunner.True(!CastingSummonResidue.Remains(new Unit[0], u => u.InState, u => u.InWorld), "An empty registry counted as residue.");
        }

        private static void NullEntryFaults()
        {
            var summons = new Unit[] { null };
            var faulted = false;
            try { CastingSummonResidue.Remains(summons, u => u.InState, u => u.InWorld); }
            catch (InvalidOperationException) { faulted = true; }
            TestRunner.True(faulted, "A null registry entry did not fault.");
        }

        private static void MissingPredicatesRefused()
        {
            var summons = new[] { new Unit() };
            var refused = 0;
            try { CastingSummonResidue.Remains<Unit>(null, u => u.InState, u => u.InWorld); } catch (ArgumentNullException) { refused++; }
            try { CastingSummonResidue.Remains(summons, null, u => u.InWorld); } catch (ArgumentNullException) { refused++; }
            try { CastingSummonResidue.Remains(summons, u => u.InState, null); } catch (ArgumentNullException) { refused++; }
            TestRunner.True(refused == 3, "A missing registry or predicate was accepted.");
        }
    }
}
