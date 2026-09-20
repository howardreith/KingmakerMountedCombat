using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class ActorAllocationLifetimeTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("allocation history survives repeated observations in one loaded campaign", SameSessionPreservesHistory);
            runner.Run("allocation history retires only the destroyed actor", DestroyedActorRetiresOnce);
            runner.Run("settled retirement preserves still owed actor history", SettlementIsActorLocal);
            runner.Run("new campaign retires old actors even with reused session object", CampaignReplacement);
            runner.Run("new loaded session retires old actor references in the same campaign", SessionReplacement);
        }

        private sealed class Fixture
        {
            internal readonly object Session = new object();
            internal readonly object Rider = new object();
            internal readonly object Mount = new object();
            internal readonly Dictionary<object, MountedMovementState> Records = new Dictionary<object, MountedMovementState>();
            internal readonly ActorAllocationLifetime<object, MountedMovementState> Lifetime;
            internal Fixture()
            {
                Lifetime = new ActorAllocationLifetime<object, MountedMovementState>(Records);
                Lifetime.SynchronizeSession(Session, "campaign-a");
                Records.Add(Rider, new MountedMovementState { TimeMoved = 1f });
                Records.Add(Mount, new MountedMovementState { TimeMoved = 3f, MetresStepped = 1f });
            }
        }

        private static void SameSessionPreservesHistory()
        {
            var fixture = new Fixture();
            var mountHistory = fixture.Records[fixture.Mount];
            for (var observation = 0; observation < 4; observation++)
                TestRunner.Equal(0, fixture.Lifetime.SynchronizeSession(fixture.Session, "campaign-a"), "Observation must not retire an allocation.");
            TestRunner.True(ReferenceEquals(mountHistory, fixture.Records[fixture.Mount]), "Mount history changed without a lifetime boundary.");
            TestRunner.Equal(3f, mountHistory.TimeMoved, "Delivered movement was lost.");
        }

        private static void DestroyedActorRetiresOnce()
        {
            var fixture = new Fixture();
            TestRunner.True(fixture.Lifetime.RetireDestroyedActor(fixture.Mount), "Destroyed mount retained its record.");
            TestRunner.True(!fixture.Lifetime.RetireDestroyedActor(fixture.Mount), "Repeated destruction retired another record.");
            TestRunner.Equal(1f, fixture.Records[fixture.Rider].TimeMoved, "Actor-local retirement removed the rider's history.");
        }

        private static void SettlementIsActorLocal()
        {
            var fixture = new Fixture();
            TestRunner.Equal(1, fixture.Lifetime.RetireSettledActors(new[] { fixture.Rider, fixture.Rider }), "Settlement was counted twice.");
            TestRunner.Equal(3f, fixture.Records[fixture.Mount].TimeMoved, "Still-owed mount movement was discarded with another actor.");
        }

        private static void CampaignReplacement()
        {
            var fixture = new Fixture();
            TestRunner.Equal(2, fixture.Lifetime.SynchronizeSession(fixture.Session, "campaign-b"), "Old campaign records leaked through a reused host.");
            TestRunner.Equal(0, fixture.Records.Count, "Old actor references remain.");
        }

        private static void SessionReplacement()
        {
            var fixture = new Fixture();
            TestRunner.Equal(2, fixture.Lifetime.SynchronizeSession(new object(), "campaign-a"), "Reload retained obsolete actor references.");
            TestRunner.Equal(0, fixture.Records.Count, "Reload did not retire the previous session.");
        }
    }
}