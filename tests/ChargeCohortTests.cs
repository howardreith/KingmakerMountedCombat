using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class ChargeCohortTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("charge cohorts retain every independent RT behavior exactly once", () => {
                var expected = ("positive below-minimum beyond-maximum stock-rejected interrupted child-cleanup combat-ended " +
                    "obstructed-line blocked-clearance cancelled exception-cleanup action-failed-before-rule action-failed-after-rule " +
                    "lease-application-failed target-moved target-lost rider-incapacitated mount-incapacitated feature-disabled " +
                    "dismounted mode-changed duplicate new-landing-blocker relationship-invalidated view-replaced mount-dead rider-dead")
                    .Split(' ').Select(n => "C6B-CHARGE-" + n).OrderBy(n => n).ToArray();
                var batches = new[] { Chunk6bChargeCohorts.Core, Chunk6bChargeCohorts.Interruption, Chunk6bChargeCohorts.Lifecycle }
                    .Select(Chunk6bChargeCohorts.Select).ToArray();
                TestRunner.True(batches.All(b => b.Length <= 11 && b[0] == "C6B-CHARGE-default-off"), "Bounded independent setup lost.");
                var actual = batches.SelectMany(b => b.Skip(1)).OrderBy(n => n).ToArray();
                TestRunner.True(expected.SequenceEqual(actual), "A behavior was lost, duplicated or substituted.");
                TestRunner.True(batches.All(b => !b.Contains("C6B-CHARGE-spent-standard")), "Dependent repeat must stay inside positive.");
                TestRunner.Equal("C6B-CHARGE-rider-dead", batches[2].Last(), "Native death must be the terminal lifecycle row.");
            });
            runner.Run("charge cohorts reject near matches and return independent selections", () => {
                TestRunner.Equal(null, Chunk6bChargeCohorts.Select("chunk6b-charge-core-rt-extra"), "Near match admitted.");
                TestRunner.Equal(null, Chunk6bChargeCohorts.Select("CHUNK6B-CHARGE-CORE-RT"), "Case mismatch admitted.");
                var changed = Chunk6bChargeCohorts.Select(Chunk6bChargeCohorts.Core);
                changed[0] = "corrupted";
                TestRunner.Equal("C6B-CHARGE-default-off", Chunk6bChargeCohorts.Select(Chunk6bChargeCohorts.Core)[0], "Selection mutation leaked.");
                TestRunner.True(HorseCompanionRegistrationScenarioPolicy.SupportsScenario(Chunk6bChargeCohorts.Core), "Registered cohort missing.");
            });
        }
    }
}
