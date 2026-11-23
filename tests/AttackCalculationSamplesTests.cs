using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The selection the Sprint 6 rule observer uses to bind the mount's shield terms to an attack (supplemental review PN8-F2):
    // a second calculation inside the same attack (the critical-confirmation AC) keeps its own sample and never replaces the
    // primary one, even with an equal total and different shield terms. Not native evidence: the observer binds the same
    // class to RuleAttackRoll, RuleCalculateAC and RuleAttackRoll.ACRule.
    internal static class AttackCalculationSamplesTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("attack calculation samples: the referenced calculation's sample survives a later calculation in the same attack", SecondCalculationKept);
            runner.Run("attack calculation samples: equal totals with different shield terms stay distinct by calculation", EqualTotalsDistinct);
            runner.Run("attack calculation samples: the innermost matching open attack owns a calculation; closing forgets it", InnermostOwner);
        }

        private sealed class Attack { public string Attacker; public string Target; }
        private sealed class Calculation { public int Result; }
        private sealed class Sample { public int Result; public int OtherShield; }

        private static void SecondCalculationKept()
        {
            var samples = new AttackCalculationSamples<Attack, Calculation, Sample>();
            var attack = new Attack { Attacker = "hostile", Target = "mount" };
            samples.Open(attack);
            var primary = new Calculation { Result = 23 }; var critical = new Calculation { Result = 25 };
            samples.Record(attack, primary, new Sample { Result = 23, OtherShield = 0 });
            samples.Record(attack, critical, new Sample { Result = 25, OtherShield = 0 });
            var taken = samples.Take(attack, primary);
            TestRunner.True(taken != null && taken.Result == 23 && samples.Count(attack) == 2, "The primary calculation's sample is taken after the critical calculation.");
        }

        private static void EqualTotalsDistinct()
        {
            var samples = new AttackCalculationSamples<Attack, Calculation, Sample>();
            var attack = new Attack { Attacker = "hostile", Target = "mount" };
            samples.Open(attack);
            var primary = new Calculation { Result = 23 }; var critical = new Calculation { Result = 23 };
            samples.Record(attack, primary, new Sample { Result = 23, OtherShield = 0 });
            samples.Record(attack, critical, new Sample { Result = 23, OtherShield = 2 });
            TestRunner.True(samples.Take(attack, primary).OtherShield == 0 && samples.Take(attack, critical).OtherShield == 2,
                "Equal totals keep each calculation's own shield terms.");
            TestRunner.True(samples.Take(attack, new Calculation { Result = 23 }) == null, "A calculation without a sample yields none, never a neighbour's.");
        }

        private static void InnermostOwner()
        {
            var samples = new AttackCalculationSamples<Attack, Calculation, Sample>();
            var outer = new Attack { Attacker = "hostile", Target = "mount" }; var inner = new Attack { Attacker = "hostile", Target = "mount" };
            var other = new Attack { Attacker = "rider", Target = "hostile" };
            samples.Open(outer); samples.Open(inner); samples.Open(other);
            TestRunner.True(ReferenceEquals(samples.Owner(a => a.Attacker == "hostile" && a.Target == "mount"), inner), "The innermost matching attack owns the calculation.");
            samples.Close(inner);
            TestRunner.True(ReferenceEquals(samples.Owner(a => a.Attacker == "hostile" && a.Target == "mount"), outer) && samples.Count(inner) == 0, "Closing an attack forgets it and its samples.");
            samples.Close(outer); samples.Close(other);
            TestRunner.True(samples.OpenCount == 0, "Nothing stays open.");
        }
    }
}
