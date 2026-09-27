using System;
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
            runner.Run("native Move callbacks exclude Standard Swift and exploration costs", CallbackOwnership);
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
