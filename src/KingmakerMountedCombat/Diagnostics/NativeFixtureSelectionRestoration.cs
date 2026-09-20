using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeFixtureSelectionRestoration
    {
        internal static UnitEntityData[] Expected(UnitEntityData[] original, UnitEntityData observedFinalDeath)
        {
            if (observedFinalDeath != null && (!observedFinalDeath.Descriptor.State.IsFinallyDead ||
                !observedFinalDeath.Descriptor.State.IsDead || observedFinalDeath.IsDirectlyControllable))
                throw new InvalidOperationException("Fixture selection exclusion lost its exact native final-death condition.");
            var expected = original.Where(unit => unit != null && unit.IsInState && unit != observedFinalDeath).ToArray();
            if (expected.Length != 0 || observedFinalDeath == null || !original.Contains(observedFinalDeath)) return expected;
            var main = Game.Instance.Player.MainCharacter.Value;
            if (main == null || main == observedFinalDeath || !main.IsInState || !main.IsDirectlyControllable || main.View == null)
                throw new InvalidOperationException("Native final-death fixture lacks a legal surviving selection fallback.");
            return new[] { main };
        }
    }
}
