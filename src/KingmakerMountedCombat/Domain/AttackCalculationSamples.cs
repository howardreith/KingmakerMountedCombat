using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // Diagnostic custody of samples taken at the native calculations nested inside an open attack (supplemental review PN8-F2).
    // Each calculation keeps its own sample, keyed by the calculation object, so a later calculation in the same attack (for
    // example the critical-confirmation AC) never replaces an earlier one; the attack's hit takes the sample of the exact
    // calculation it references (RuleAttackRoll.ACRule in Kingmaker 2.1.7b). Engine-free so the selection is tested offline.
    public sealed class AttackCalculationSamples<TAttack, TCalculation, TSample> where TAttack : class where TCalculation : class where TSample : class
    {
        private sealed class ByReference<T> : IEqualityComparer<T> where T : class
        {
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }

        private readonly List<TAttack> open = new List<TAttack>();
        private readonly Dictionary<TAttack, Dictionary<TCalculation, TSample>> samples =
            new Dictionary<TAttack, Dictionary<TCalculation, TSample>>(new ByReference<TAttack>());

        public int OpenCount => open.Count;

        public void Open(TAttack attack)
        {
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            open.Remove(attack);
            open.Add(attack);
            samples[attack] = new Dictionary<TCalculation, TSample>(new ByReference<TCalculation>());
        }

        public void Close(TAttack attack)
        {
            open.Remove(attack);
            samples.Remove(attack);
        }

        // The innermost open attack the calculation belongs to (the matching attack opened last), or null.
        public TAttack Owner(Func<TAttack, bool> matches) => open.LastOrDefault(matches);

        public void Record(TAttack attack, TCalculation calculation, TSample sample)
        {
            Dictionary<TCalculation, TSample> byCalculation;
            if (attack == null || calculation == null || sample == null || !samples.TryGetValue(attack, out byCalculation)) return;
            byCalculation[calculation] = sample;
        }

        // The sample taken at exactly this calculation of this attack, or null when none was taken there.
        public TSample Take(TAttack attack, TCalculation calculation)
        {
            Dictionary<TCalculation, TSample> byCalculation;
            TSample sample;
            if (attack == null || calculation == null || !samples.TryGetValue(attack, out byCalculation) || !byCalculation.TryGetValue(calculation, out sample)) return null;
            return sample;
        }

        public int Count(TAttack attack)
        {
            Dictionary<TCalculation, TSample> byCalculation;
            return attack != null && samples.TryGetValue(attack, out byCalculation) ? byCalculation.Count : 0;
        }
    }
}
