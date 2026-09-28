using System;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class RelationshipCommandEvidenceTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("relationship evidence rejects a second command with matching actor and ability", IdentityIsExact);
            runner.Run("relationship evidence retains the exact process and context at delivery", ProcessIsExact);
            runner.Run("an unobserved acted boundary cannot be replaced by a charged endpoint", ActedIsMandatory);
            runner.Run("resource windows distinguish RT decay from refund and extra charge", RealTimeDebt);
            runner.Run("TB resource windows reject both refunds and second charges", TurnBasedDebt);
            runner.Run("reaction events conserve discrete allowance and distinguish cooldowns from ordering", ReactionEvents);
            runner.Run("unacted obstruction identity cannot qualify a positive process", UnactedIdentity);
            runner.Run("native Move callbacks exclude Standard Swift and exploration costs", CallbackOwnership);
            runner.Run("native TB setup proposals preserve measured separation at arbitrary orientation", ActingFixture);
            runner.Run("native ground fixtures require the full footprint and reachable endpoint", GroundFixture);
            runner.Run("pre-combat positioning bounds cannot relax the later Acting step", PreCombatPositioning);
        }
        private static RelationshipCommandIdentity Make(object command, object process, object context,
            string control = "shell:1", string caster = "rider", string target = "horse", long generation = 4,
            string action = "Move", string ability = "mount-guid") =>
            new RelationshipCommandIdentity(command, control, process, context, caster, target, generation, action, ability);
        private static void IdentityIsExact()
        {
            var command = new object(); var process = new object(); var context = new object();
            var identity = Make(command, process, context);
            TestRunner.True(identity.Matches(Make(command, null, null), true), "pre-process observation binds the same command");
            TestRunner.True(!identity.Matches(Make(new object(), null, null), true), "same actor and blueprint cannot substitute another command");
            foreach (var sample in new[] { Make(command, process, context, "shell:2"),
                Make(command, process, context, caster: "foreign"), Make(command, process, context, target: "other"),
                Make(command, process, context, generation: 5), Make(command, process, context, action: "Standard"),
                Make(command, process, context, ability: "other-guid") })
                TestRunner.True(!identity.Matches(sample, false), "every immutable identity component is required");
        }
        private static void ProcessIsExact()
        {
            var command = new object(); var process = new object(); var context = new object();
            var identity = Make(command, process, context);
            TestRunner.True(identity.Matches(Make(command, process, context), false), "same native objects match");
            TestRunner.True(!identity.Matches(Make(command, new object(), context), false), "different process refuses");
            TestRunner.True(!identity.Matches(Make(command, process, new object()), false), "different context refuses");
            TestRunner.True(!identity.Matches(Make(command, null, null), false), "delivery cannot omit its process");
            TestRunner.True(!Make(command, null, null).Complete, "a click does not invent a future process");
        }
        private static void ActedIsMandatory()
        {
            TestRunner.True(!NativeResourceWindowPolicy.ExactMoveCallback(false, true, false, 0, 0, 3, 0, 0, 0, 0),
                "a correct cooldown alone is insufficient");
            TestRunner.True(NativeResourceWindowPolicy.ExactMoveCallback(true, true, true, 0, 3, 6, 2, 2, 1, 1), "one exact TB Move");
        }
        private static void RealTimeDebt()
        {
            TestRunner.True(NativeResourceWindowPolicy.EndpointConserved(4.5, 3.25, 1.25, false, 0.0001), "ordinary decay conserved");
            TestRunner.True(!NativeResourceWindowPolicy.EndpointConserved(4.5, 0, 1.25, false, 0.0001), "refund rejected even though after is lower");
            TestRunner.True(!NativeResourceWindowPolicy.EndpointConserved(4.5, 4, 1.25, false, 0.0001), "extra debt rejected even though after is lower");
            TestRunner.True(NativeResourceWindowPolicy.EndpointConserved(0.5, 0, 1.25, false, 0.0001), "zero floor is native decay");
            TestRunner.True(!NativeResourceWindowPolicy.EndpointConserved(4.5, 3, -1, false, 0.0001), "reversed clock refused");
        }
        private static void TurnBasedDebt()
        {
            TestRunner.True(NativeResourceWindowPolicy.EndpointConserved(3, 3, 10, true, 0.0001), "static TB debt retained");
            TestRunner.True(!NativeResourceWindowPolicy.EndpointConserved(3, 6, 10, true, 0.0001), "second cost rejected");
            TestRunner.True(!NativeResourceWindowPolicy.EndpointConserved(3, 0, 10, true, 0.0001), "refund rejected");
        }
        private static void ReactionEvents()
        {
            var spent = new NativeReactionResources(0, 0.25, 0, 12);
            TestRunner.True(spent.Tick(0.25, false, true, false, false, false, 1).Matches(new NativeReactionResources(1, 0, 0, 12)), "RT expiry has one native refresh");
            TestRunner.True(spent.Tick(0.25, true, true, true, false, false, 1).Matches(new NativeReactionResources(0, 0, 0, 12)), "TB passing does not replenish allowance");
            TestRunner.True(spent.Tick(1, true, true, false, false, false, 1).Matches(spent), "active TB turn does not decay");
            var waiting = new NativeReactionResources(0, 3, 1, 12);
            TestRunner.True(waiting.Tick(0.25, false, true, false, false, true, 1).Matches(new NativeReactionResources(0, 3, 0.75, 12)), "RT waiting initiative delays AoO decay");
            TestRunner.True(waiting.Tick(1.25, true, true, true, false, true, 1).Matches(new NativeReactionResources(0, 2.75, 0, 12)), "TB passing consumes initiative delay first");
            TestRunner.True(waiting.Clear().Matches(new NativeReactionResources(0, 0, 0, 12)), "Clear preserves discrete allowance and ordering");
            TestRunner.True(waiting.Prepare(1).Matches(new NativeReactionResources(1, 0, 0, 12)), "declared Prepare refreshes allowance");
            TestRunner.True(new NativeReactionResources(2, 3, 1, 12).Prepare(1).Matches(new NativeReactionResources(2, 0, 0, 12)), "Prepare preserves above-normal allowance");
            TestRunner.True(!spent.Matches(new NativeReactionResources(1, 0.25, 0, 12)), "reaction-only change rejected");
            TestRunner.True(!spent.Matches(new NativeReactionResources(0, 0, 0, 12)), "AoO-only refund rejected");
            TestRunner.True(!spent.Matches(new NativeReactionResources(0, 0.25, 0, 13)), "ordering cannot be relabeled as cooldown");
        }
        private static void UnactedIdentity()
        {
            var command = new object();
            var request = Make(command, null, null);
            TestRunner.True(request.UnactedRequestComplete && request.MatchesUnacted(Make(command, null, null)), "unacted command and shell are exact");
            TestRunner.True(!request.Complete && !request.Matches(request, true), "process-free evidence cannot qualify the positive contract");
            TestRunner.True(!request.MatchesUnacted(Make(new object(), null, null)), "second unacted command rejected");
            TestRunner.True(!request.MatchesUnacted(Make(command, new object(), new object())), "acted process cannot qualify unacted failure");
            TestRunner.True(!request.MatchesUnacted(Make(command, null, null, generation: 5)), "unacted generation mismatch rejected");
        }
        private static void ActingFixture()
        {
            // Captured full-TB121 geometry, plus its rotations and the adjacent compensation case.
            foreach (var separation in new[] { 1.12, 2.45, 6.411576 })
                for (var angle = 0; angle < 360; angle += 11)
                {
                    var radians = angle * Math.PI / 180;
                    var mount = new PoseVector3(3.71811056f, 6.096037f, 51.4622421f);
                    var rider = mount + new PoseVector3((float)(Math.Cos(radians) * separation), 0,
                        (float)(Math.Sin(radians) * separation));
                    var actualSeparation = (rider - mount).Magnitude;
                    var points = NativeGroundFixturePolicy.ActingProposals(rider, mount).ToArray();
                    TestRunner.Equal(18, points.Length, "bounded proposals cover both sides of the annulus");
                    foreach (var point in points)
                        TestRunner.True(NativeGroundFixturePolicy.IsActingStep(actualSeparation, (point - mount).Magnitude,
                            (point - rider).Magnitude, 0, 0, false), "each unobstructed proposal satisfies unchanged bounds");
                }
            TestRunner.Equal(0, NativeGroundFixturePolicy.ActingProposals(new PoseVector3(float.NaN, 0, 0),
                new PoseVector3(0, 0, 0)).Count(), "invalid geometry admits nothing");
            foreach (var bad in new[] {
                new[] { 6.4, 0.6, 0.0, 0.0 }, new[] { 6.7, 0.6, 0.0, 0.0 },
                new[] { 6.55, 0.4, 0.0, 0.0 }, new[] { 6.55, 0.8, 0.0, 0.0 },
                new[] { 6.55, 0.6, 0.1, 0.0 }, new[] { 6.55, 0.6, 0.0, 0.1 },
                new[] { 6.55, 0.6, double.NaN, 0.0 } })
                TestRunner.True(!NativeGroundFixturePolicy.IsActingStep(6.5, bad[0], bad[1], bad[2], bad[3], false),
                    "projection, route or footprint failure remains refused");
            TestRunner.True(!NativeGroundFixturePolicy.IsActingStep(6.5, 6.55, 0.6, 0, 0, true), "occupied point refused");
        }

        private static void PreCombatPositioning()
        {
            TestRunner.True(NativeGroundFixturePolicy.IsPreCombatPosition(2.3, 2.3, 2, 0, 0, false), "clear bounded staging route");
            foreach (var bad in new[] { new[] { 1.9, 2.0, 0.0, 0.0 }, new[] { 2.7, 2.0, 0.0, 0.0 },
                new[] { 2.3, 4.01, 0.0, 0.0 }, new[] { 2.3, 0.1, 0.0, 0.0 },
                new[] { 2.3, 2.0, 0.01, 0.0 }, new[] { 2.3, 2.0, 0.0, 0.01 }, new[] { 2.3, 2.0, double.NaN, 0.0 } })
                TestRunner.True(!NativeGroundFixturePolicy.IsPreCombatPosition(bad[0], bad[0], bad[1], bad[2], bad[3], false), "invalid staging geometry refused");
            TestRunner.True(!NativeGroundFixturePolicy.IsPreCombatPosition(2.3, 2.3, 2, 0, 0, true), "occupied staging destination refused");
            TestRunner.True(!NativeGroundFixturePolicy.IsActingStep(2.3, 2.3, 2, 0, 0, false), "staging movement cannot substitute for bounded Acting step");
        }

        private static void GroundFixture()
        {
            TestRunner.True(NativeGroundFixturePolicy.IsClear(6, 6, 4, 0, 0, false), "clear full-footprint endpoint admitted");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 6, 4, 0, 0.1, false), "walkable center with clipped footprint refused");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 6, 4, 0.5, 0, false), "truncated route refused");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 2, 4, 0, 0, false), "projected endpoint cannot collapse separation");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 6, 4, 0, 0, true), "another actor occupies the endpoint");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 6, 0, 0, 0, false), "an unchanged endpoint is not setup movement");
            TestRunner.True(!NativeGroundFixturePolicy.IsClear(6, 6, 4, double.NaN, 0, false), "missing route evidence refused");
        }
        private static void CallbackOwnership()
        {
            TestRunner.True(NativeResourceWindowPolicy.ExactMoveCallback(true, true, false, 0.1, 0, 2.9, 2, 2, 1, 1), "native RT cost exactly matches its command age");
            TestRunner.True(!NativeResourceWindowPolicy.ExactMoveCallback(true, true, false, 0.1, 0, 2.9, 2, 6, 1, 1), "Standard cost refused");
            TestRunner.True(!NativeResourceWindowPolicy.ExactMoveCallback(true, true, false, 0.1, 0, 2.9, 2, 2, 1, 6), "Swift cost refused");
            TestRunner.True(NativeResourceWindowPolicy.ExactMoveCallback(true, false, false, 0, 0, 0, 0, 0, 0, 0), "exploration callback writes nothing");
            TestRunner.True(!NativeResourceWindowPolicy.ExactMoveCallback(true, false, false, 0, 0, 3, 0, 0, 0, 0), "exploration charge refused");
        }
    }
}
