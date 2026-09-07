using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class PairedActivationTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("paired grants require each native boundary and exactly one preparation", ThreeActivations);
            runner.Run("pair observations and selection cannot mint grants", ObservationsAreNotGrants);
            runner.Run("split preserves actor debt and rejects further paired grants", SplitRetainsDebt);
            runner.Run("pair cannot begin next activation while either actor is unended", UnendedActorBlocksNext);
        }
        private static void Prepare(PairedActivation<object, object> pair, object boundary)
        {
            TestRunner.Equal(true, pair.Begin(boundary), "native boundary");
            TestRunner.Equal(true, pair.BeginActorPreparation(pair.Principal, boundary), "rider grant");
            pair.FinishActorPreparation(pair.Principal);
            TestRunner.Equal(false, pair.Open, "partner must also prepare");
            TestRunner.Equal(true, pair.BeginActorPreparation(pair.Partner, boundary), "mount grant");
            pair.FinishActorPreparation(pair.Partner);
        }
        private static void ThreeActivations()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var first = new object();
            for (var i = 1; i <= 3; i++)
            {
                var boundary = i == 1 ? first : new object();
                Prepare(pair, boundary);
                TestRunner.Equal((long)i, pair.Sequence, "sequence");
                TestRunner.Equal(true, pair.CanAddress(pair.Partner, boundary), "partner addressable");
                TestRunner.Equal(false, pair.BeginActorPreparation(pair.Partner, boundary), "no second preparation");
                TestRunner.Equal(0f, pair.Mount.StandardSpent, "fresh grant has no previous debt");
                pair.Mount.Observe(6f, 3f, 0f);
                pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner);
            }
            TestRunner.Equal(false, pair.Begin(first), "old boundary cannot be replayed");
        }
        private static void ObservationsAreNotGrants()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object();
            pair.Begin(boundary);
            TestRunner.Equal(false, pair.CanAddress(pair.Partner, boundary), "no resources before native prepare");
            TestRunner.Equal(false, pair.BeginActorPreparation(pair.Partner, new object()), "selection is not a boundary");
            TestRunner.Equal(false, pair.BeginActorPreparation(new object(), boundary), "foreign actor");
            TestRunner.Equal(1L, pair.Sequence, "observation leaves identity unchanged");
        }
        private static void SplitRetainsDebt()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); Prepare(pair, boundary);
            pair.Mount.Observe(6f, 3f, 0f); pair.Mount.Observe(0f, 0f, 0f);
            pair.Detach(); pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner);
            TestRunner.Equal(6f, pair.Mount.StandardSpent, "decay/split cannot erase observed expenditure");
            TestRunner.Equal(false, pair.Begin(new object()), "new relationship cannot refresh old pair");
            TestRunner.Equal(false, pair.CanAddress(pair.Partner, boundary), "split closes partner admission");
        }
        private static void UnendedActorBlocksNext()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            Prepare(pair, new object()); pair.BeginEnding(); pair.EndActor(pair.Principal);
            var rejected = false;
            try { pair.Begin(new object()); } catch (InvalidOperationException) { rejected = true; }
            TestRunner.Equal(true, rejected, "mount completion is required");
        }
    }
}
