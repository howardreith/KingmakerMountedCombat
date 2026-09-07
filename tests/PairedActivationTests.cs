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
            runner.Run("native delay resumes the existing paired grant once without a refresh", DelayResumesGrant);
            runner.Run("pair delay rejects either actor expenditure and split cannot resume", DelayConservesParticipation);
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
        private static void DelayResumesGrant()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var first = new object(); Prepare(pair, first);
            var identity = pair.Identity;
            TestRunner.Equal(true, pair.Suspend(first), "unused native delay");
            TestRunner.Equal(false, pair.CanAddress(pair.Partner, first), "suspension closes command admission");
            TestRunner.Equal(false, pair.Resume(first), "disposed old boundary cannot resume");
            var resumed = new object();
            TestRunner.Equal(true, pair.Resume(resumed), "actual later native boundary");
            TestRunner.Equal(identity, pair.Identity, "resume is the same grant");
            TestRunner.Equal(false, pair.BeginActorPreparation(pair.Partner, resumed), "round and resources are not prepared again");
            TestRunner.Equal(true, pair.CanAddress(pair.Partner, resumed), "existing partner resources remain usable");
            TestRunner.Equal(false, pair.Resume(new object()), "only the outstanding delay may resume");
        }
        private static void DelayConservesParticipation()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); Prepare(pair, boundary);
            pair.Mount.Observe(0f, 0.2f, 0f);
            TestRunner.Equal(false, pair.Suspend(boundary), "mount motion prevents principal-only delay");
            pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner);
            boundary = new object(); Prepare(pair, boundary);
            pair.Rider.Observe(0f, 0f, 1f);
            TestRunner.Equal(false, pair.Suspend(boundary), "rider action also prevents delay");
            pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner);
            boundary = new object(); Prepare(pair, boundary);
            TestRunner.Equal(true, pair.Suspend(boundary), "fresh boundary can delay");
            pair.Detach();
            TestRunner.Equal(false, pair.Resume(new object()), "dismount cannot rebind a suspended paired grant");
        }
    }
}
