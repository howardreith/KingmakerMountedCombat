using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class PairedActivationTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("saved paired remainder binds new actors without granting a new activation", RestoreRemainder);
            runner.Run("saved ended actors and condition settlement cannot act or settle twice", RestoreForfeiture);
            runner.Run("saved suspension and split retain the current participation identity", RestoreSuspension);
            runner.Run("invalid saved grants and absent boundary fail without fabricating readiness", RestoreRejectsInvalid);
            runner.Run("paired grants require each native boundary and exactly one preparation", ThreeActivations);
            runner.Run("pair observations and selection cannot mint grants", ObservationsAreNotGrants);
            runner.Run("split preserves actor debt and rejects further paired grants", SplitRetainsDebt);
            runner.Run("pair cannot begin next activation while either actor is unended", UnendedActorBlocksNext);
            runner.Run("actor forfeit cannot complete or refresh the shared activation", ActorForfeitKeepsBoundary);
            runner.Run("partial preparation retirement grants no missing partner resources", PartialPreparationRetirement);
            runner.Run("native delay resumes the existing paired grant once without a refresh", DelayResumesGrant);
            runner.Run("pair delay rejects either actor expenditure and split cannot resume", DelayConservesParticipation);
            runner.Run("native preparation effects require the exact pending actor grant", PreparationEffectOwnership);
            runner.Run("split retains pending native effects but cannot grant another preparation", SplitDuringPreparation);
            runner.Run("native condition End settles only its observed forfeiture once", NativeForfeitSettlement);
            runner.Run("native condition settlement preserves other debt and new grants carry no settlement", NativeForfeitConservation);
        }
        private static void RestoreRemainder()
        {
            var original = new PairedActivation<object, object>(new object(), new object());
            var oldBoundary = new object(); Prepare(original, oldBoundary);
            original.Mount.Observe(0f, 1.5f, 0f);
            var snapshot = original.Capture();
            original.EndActor(original.Partner);
            var rider = new object(); var mount = new object(); var current = new object();
            var restored = PairedActivation<object, object>.Restore(snapshot, rider, mount, current);
            TestRunner.Equal(original.Identity, restored.Identity, "load retains encounter and activation identity");
            TestRunner.Equal(true, restored.CanAddress(rider, current), "unused rider work survives");
            TestRunner.Equal(true, restored.CanAddress(mount, current), "snapshot is independent of later original End");
            TestRunner.Equal(1.5f, restored.Mount.MoveSpent, "movement commitment survives");
            TestRunner.Equal(false, restored.CanAddress(original.Principal, current), "old actor cannot address new world");
            TestRunner.Equal(false, restored.CanAddress(rider, oldBoundary), "old process boundary is retired");
            TestRunner.Equal(false, restored.Begin(current), "restored boundary cannot be begun again");
            TestRunner.Equal(false, restored.BeginActorPreparation(rider, current), "completed rider preparation cannot replay");
            TestRunner.Equal(false, restored.BeginActorPreparation(mount, current), "completed mount preparation cannot replay");
            for (var sequence = 2; sequence <= 3; sequence++)
            {
                restored.EndActor(rider); restored.EndActor(mount); restored.FinalizeActivation();
                current = new object(); Prepare(restored, current);
                TestRunner.Equal((long)sequence, restored.Sequence, "true next activation advances once");
                TestRunner.Equal(0f, restored.Mount.MoveSpent, "old commitment cannot starve next grant");
                TestRunner.Equal(false, restored.BeginActorPreparation(mount, current), "new grant is still exactly once");
            }
        }

        private static void RestoreForfeiture()
        {
            var original = new PairedActivation<object, object>(new object(), new object());
            Prepare(original, new object()); original.EndActor(original.Partner);
            original.Mount.RecordNativeStandardForfeit(2f, 6f);
            original.Mount.Observe(18f, 3f, 0f);
            var current = new object();
            var restored = PairedActivation<object, object>.Restore(original.Capture(), new object(), new object(), current);
            TestRunner.Equal(false, restored.CanAddress(restored.Partner, current), "ended partner remains ended");
            TestRunner.Equal(true, restored.CanAddress(restored.Principal, current), "rider is not conservatively forfeited");
            TestRunner.Equal(14f, restored.Mount.SettleNativeStandardForfeit(18f, 6f), "only outstanding native settlement survives");
            var again = PairedActivation<object, object>.Restore(restored.Capture(), new object(), new object(), new object());
            TestRunner.Equal(18f, again.Mount.SettleNativeStandardForfeit(18f, 6f), "load cannot replay a settled credit");
            TestRunner.Equal(18f, again.Mount.StandardSpent, "historical observation is preserved separately from current debt");
        }

        private static void RestoreSuspension()
        {
            var original = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); Prepare(original, boundary); original.Suspend(boundary);
            var restoredBoundary = new object();
            var restored = PairedActivation<object, object>.Restore(original.Capture(), new object(), new object(), restoredBoundary);
            TestRunner.Equal(false, restored.CanAddress(restored.Principal, restoredBoundary), "suspended grant stays closed");
            TestRunner.Equal(false, restored.Resume(restoredBoundary), "resume requires the actual later boundary");
            var resumedBoundary = new object();
            TestRunner.Equal(true, restored.Resume(resumedBoundary), "same-round native resume uses saved grant");
            TestRunner.Equal(original.Identity, restored.Identity, "resume does not create an activation");
            TestRunner.Equal(false, restored.BeginActorPreparation(restored.Partner, resumedBoundary), "resume cannot repeat round effects");
            restored.Detach();
            var split = PairedActivation<object, object>.Restore(restored.Capture(), new object(), new object(), new object());
            TestRunner.Equal(true, split.Split, "split participation survives");
            TestRunner.Equal(false, split.Begin(new object()), "split cannot grant another paired activation");
        }

        private static void RestoreRejectsInvalid()
        {
            var armed = new PairedActivation<object, object>(new object(), new object());
            var restored = PairedActivation<object, object>.Restore(armed.Capture(), new object(), new object(), null);
            TestRunner.Equal(armed.Identity, restored.Identity, "unstarted encounter is not a new identity");
            TestRunner.Equal(0L, restored.Sequence, "unstarted encounter creates no grant");
            TestRunner.Equal(false, restored.Open, "no readiness without native preparation");
            var rejected = false;
            try { new PairedActorSnapshot(false, true, false, 0f, 0f, 0f, false, false, 0f); }
            catch (ArgumentException) { rejected = true; }
            TestRunner.Equal(true, rejected, "prepared actor without grant is rejected");
            rejected = false;
            try { new PairedActorSnapshot(true, true, false, float.NaN, 0f, 0f, false, false, 0f); }
            catch (ArgumentException) { rejected = true; }
            TestRunner.Equal(true, rejected, "nonfinite debt is rejected");
            Prepare(armed, new object()); rejected = false;
            try { PairedActivation<object, object>.Restore(armed.Capture(), new object(), new object(), null); }
            catch (ArgumentException) { rejected = true; }
            TestRunner.Equal(true, rejected, "active grant cannot silently become an unstarted activation");
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
        private static void NativeForfeitSettlement()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); Prepare(pair, boundary);
            TestRunner.Equal(12f, pair.Mount.SettleNativeStandardForfeit(12f, 6f), "ordinary debt has no normalization credit");
            pair.EndActor(pair.Partner);
            pair.Mount.RecordNativeStandardForfeit(0f, 6f);
            pair.Mount.Observe(12f, 3f, 6f);
            TestRunner.Equal(6f, pair.Mount.SettleNativeStandardForfeit(12f, 6f), "native SelfHarm transient total settles at End");
            TestRunner.Equal(12f, pair.Mount.SettleNativeStandardForfeit(12f, 6f), "settlement cannot be replayed");
            TestRunner.Equal(12f, pair.Mount.StandardSpent, "original native cost observations remain evidence");
            TestRunner.Equal(true, pair.CanAddress(pair.Principal, boundary), "partner settlement cannot end principal input");
        }
        private static void NativeForfeitConservation()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            Prepare(pair, new object()); pair.EndActor(pair.Partner);
            pair.Mount.RecordNativeStandardForfeit(2f, 6f);
            TestRunner.Equal(14f, pair.Mount.SettleNativeStandardForfeit(18f, 6f), "remove only the observed four-unit forfeiture");
            var rejected = false;
            try { pair.Mount.RecordNativeStandardForfeit(0f, 6f); } catch (InvalidOperationException) { rejected = true; }
            TestRunner.Equal(true, rejected, "a second forfeiture cannot add another settlement");
            pair.EndActor(pair.Principal); pair.FinalizeActivation();
            Prepare(pair, new object()); pair.EndActor(pair.Partner);
            TestRunner.Equal(12f, pair.Mount.SettleNativeStandardForfeit(12f, 6f), "new grant cannot reuse prior settlement");
        }
        private static void PreparationEffectOwnership()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); pair.Begin(boundary);
            TestRunner.Equal(false, pair.IsPreparingActor(pair.Partner, boundary), "ungranted actor has no effect admission");
            pair.BeginActorPreparation(pair.Principal, boundary);
            TestRunner.Equal(true, pair.IsPreparingActor(pair.Principal, boundary), "pending native rider preparation");
            TestRunner.Equal(false, pair.IsPreparingActor(pair.Principal, new object()), "foreign boundary is not ownership");
            TestRunner.Equal(false, pair.IsPreparingActor(new object(), boundary), "foreign actor is not ownership");
            pair.FinishActorPreparation(pair.Principal);
            pair.BeginActorPreparation(pair.Partner, boundary);
            TestRunner.Equal(false, pair.IsPreparingActor(pair.Principal, boundary), "completed effects cannot replay");
            TestRunner.Equal(true, pair.IsPreparingActor(pair.Partner, boundary), "principal completion retains partner effects");
            pair.EndActor(pair.Partner);
            TestRunner.Equal(false, pair.IsPreparingActor(pair.Partner, boundary), "actor forfeit closes preparation admission");
        }
        private static void SplitDuringPreparation()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); pair.Begin(boundary);
            pair.BeginActorPreparation(pair.Principal, boundary);
            pair.FinishActorPreparation(pair.Principal);
            pair.BeginActorPreparation(pair.Partner, boundary);
            var identity = pair.Identity;
            pair.Detach();
            TestRunner.Equal(true, pair.IsPreparingActor(pair.Partner, boundary), "native effect control loss retains granted work");
            pair.FinishActorPreparation(pair.Partner);
            TestRunner.Equal(false, pair.IsPreparingActor(pair.Partner, boundary), "effect completion is once only");
            TestRunner.Equal(false, pair.CanAddress(pair.Partner, boundary), "split does not enable ordinary partner input");
            TestRunner.Equal(false, pair.BeginActorPreparation(pair.Partner, boundary), "split cannot repeat preparation");
            TestRunner.Equal(false, pair.Begin(new object()), "split cannot create a new paired grant");
            TestRunner.Equal(identity, pair.Identity, "split preserves the original grant identity");
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
                pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner); pair.FinalizeActivation();
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
            pair.Detach(); pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner); pair.FinalizeActivation();
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
        private static void ActorForfeitKeepsBoundary()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); Prepare(pair, boundary);
            pair.Mount.Observe(6f, 3f, 6f); pair.EndActor(pair.Partner);
            TestRunner.Equal(false, pair.CanAddress(pair.Partner, boundary), "forfeited actor has no new input");
            TestRunner.Equal(true, pair.CanAddress(pair.Principal, boundary), "principal grant remains live");
            var rejected = false;
            try { pair.FinalizeActivation(); } catch (InvalidOperationException) { rejected = true; }
            TestRunner.Equal(true, rejected, "one actor cannot finalize the pair");
            pair.EndActor(pair.Principal);
            rejected = false;
            try { pair.Begin(new object()); } catch (InvalidOperationException) { rejected = true; }
            TestRunner.Equal(true, rejected, "two local forfeits do not manufacture a fresh boundary");
            TestRunner.Equal(true, pair.FinalizeActivation(), "native shared End finalizes once");
            TestRunner.Equal(false, pair.FinalizeActivation(), "completion is idempotent");
            TestRunner.Equal(6f, pair.Mount.StandardSpent, "forfeit is not a refund");
            Prepare(pair, new object());
            TestRunner.Equal(2L, pair.Sequence, "one later native grant");
            TestRunner.Equal(false, pair.Finalized, "fresh boundary has its own completion");
        }
        private static void PartialPreparationRetirement()
        {
            var pair = new PairedActivation<object, object>(new object(), new object());
            var boundary = new object(); pair.Begin(boundary);
            pair.BeginActorPreparation(pair.Principal, boundary);
            pair.EndActor(pair.Principal);
            TestRunner.Equal(true, pair.FinalizeActivation(), "removal may retire the partial preparation");
            TestRunner.Equal(false, pair.Mount.Granted, "retirement never gives absent partner entitlement");
            TestRunner.Equal(false, pair.Mount.Prepared, "missing partner effects are not fabricated");
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
            pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner); pair.FinalizeActivation();
            boundary = new object(); Prepare(pair, boundary);
            pair.Rider.Observe(0f, 0f, 1f);
            TestRunner.Equal(false, pair.Suspend(boundary), "rider action also prevents delay");
            pair.BeginEnding(); pair.EndActor(pair.Principal); pair.EndActor(pair.Partner); pair.FinalizeActivation();
            boundary = new object(); Prepare(pair, boundary);
            TestRunner.Equal(true, pair.Suspend(boundary), "fresh boundary can delay");
            pair.Detach();
            TestRunner.Equal(false, pair.Resume(new object()), "dismount cannot rebind a suspended paired grant");
        }
    }
}
