using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargePolicyTests
    {
        private static MountedChargeRequest Lawful()
        {
            return new MountedChargeRequest
            {
                FeatureEnabled = true,
                RelationshipMounted = true,
                ExactPair = true,
                RiderDirectlyControllable = true,
                LifecycleBoundary = false,
                InCombat = true,
                TurnBased = false,
                RiderTurn = true,
                TurnActingOrPreparing = true,
                TurnActing = true,
                TurnTimeMoved = 0f,
                RiderCanActInCombat = true,
                RiderStandardCooldown = 0f,
                RiderMoveCooldown = 0f,
                DeliveringOwnShell = false,
                WeaponPresent = true,
                WeaponIsRanged = false,
                TargetValid = true,
                TargetVisible = true,
                TargetHostile = true,
                TargetAttackable = true,
                AlreadyActiveCommand = false,
                MountCommandsIdle = true,
                Distance = 9f,
                MinimumRange = 4.6f,
                MaximumRange = 30.48f,
                StraightRoute = true,
                LandingBlocked = false,
                MountAvoidanceDisabled = false
            };
        }

        private static MountedChargeAvailability Evaluate(System.Action<MountedChargeRequest> change)
        {
            var request = Lawful();
            if (change != null)
            {
                change(request);
            }
            return MountedChargePolicy.Evaluate(request);
        }

        private static void Refuses(System.Action<MountedChargeRequest> change, MountedCombatRejectionCode code, string what)
        {
            var result = Evaluate(change);
            TestRunner.True(!result.IsAllowed, "Mounted Charge admitted " + what + ".");
            TestRunner.True(result.RejectionCode == code, "Mounted Charge refused " + what + " with the wrong code: " + result.RejectionCode + ".");
            TestRunner.True(!string.IsNullOrWhiteSpace(result.Reason) && result.Reason != MountedChargePolicy.AllowedReason,
                "Mounted Charge refused " + what + " without an exact reason.");
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("mounted charge admits only the exact lawful mounted pair request", () =>
            {
                var allowed = MountedChargePolicy.Evaluate(Lawful());
                TestRunner.True(allowed.IsAllowed, "A lawful mounted charge was refused: " + allowed.Reason);
                TestRunner.True(allowed.Reason == MountedChargePolicy.AllowedReason, "A lawful mounted charge reported a refusal reason.");
                TestRunner.True(!allowed.RejectionCode.HasValue, "A lawful mounted charge reported a rejection code.");
                TestRunner.True(!MountedChargePolicy.Evaluate(null).IsAllowed, "A missing request was admitted.");
            });

            runner.Run("mounted charge is default-off and gated before every other check", () =>
            {
                var disabled = Evaluate(request =>
                {
                    request.FeatureEnabled = false;
                    request.RelationshipMounted = false;
                    request.InCombat = false;
                    request.TargetValid = false;
                });
                TestRunner.True(!disabled.IsAllowed, "The disabled feature was admitted.");
                TestRunner.True(disabled.RejectionCode == MountedCombatRejectionCode.FeatureDisabled,
                    "The feature gate did not precede every other refusal: " + disabled.RejectionCode + ".");
            });

            runner.Run("mounted charge requires the exact mounted pair and its controllable rider", () =>
            {
                Refuses(request => request.RelationshipMounted = false, MountedCombatRejectionCode.RelationshipInvalidated, "an unmounted rider");
                Refuses(request => request.ExactPair = false, MountedCombatRejectionCode.RelationshipInvalidated, "a foreign pair");
                Refuses(request => request.RiderDirectlyControllable = false, MountedCombatRejectionCode.WrongActorOrSelection, "an uncontrollable rider");
                Refuses(request => request.LifecycleBoundary = true, MountedCombatRejectionCode.LifecycleBoundary, "a lifecycle boundary");
                Refuses(request => request.InCombat = false, MountedCombatRejectionCode.NotInCombat, "a charge out of combat");
            });

            runner.Run("mounted charge requires a living visible hostile attackable target", () =>
            {
                Refuses(request => request.TargetValid = false, MountedCombatRejectionCode.TargetInvalid, "a dead target");
                Refuses(request => request.TargetVisible = false, MountedCombatRejectionCode.TargetNotVisible, "an unseen target");
                Refuses(request => request.TargetHostile = false, MountedCombatRejectionCode.TargetNotHostile, "a friendly target");
                Refuses(request => request.TargetAttackable = false, MountedCombatRejectionCode.TargetNotAttackable, "an unattackable target");
            });

            runner.Run("mounted charge requires a melee weapon and an idle pair", () =>
            {
                Refuses(request => request.WeaponPresent = false, MountedCombatRejectionCode.NoEligibleWeapon, "an empty hand");
                Refuses(request => request.WeaponIsRanged = true, MountedCombatRejectionCode.MountedRangedUnsupported, "a ranged weapon");
                Refuses(request => request.AlreadyActiveCommand = true, MountedCombatRejectionCode.AlreadyActiveCommand, "an active pair command");
                Refuses(request => request.MountCommandsIdle = false, MountedCombatRejectionCode.AlreadyActiveCommand, "a busy mount");
            });

            runner.Run("mounted charge requires the rider standard action in both modes", () =>
            {
                Refuses(request => request.RiderCanActInCombat = false, MountedCombatRejectionCode.WrongActionState, "a rider who cannot act");
                Refuses(request => request.RiderStandardCooldown = 3f, MountedCombatRejectionCode.WrongActionState, "a spent standard action");
                Refuses(request =>
                {
                    request.TurnBased = true;
                    request.RiderStandardCooldown = 3f;
                }, MountedCombatRejectionCode.WrongActionState, "a spent turn-based standard action");
            });

            runner.Run("turn-based mounted charge refuses every turn and delivery state without affecting real time", () =>
            {
                foreach (var own in new[] { false, true })
                foreach (var acting in new[] { false, true })
                foreach (var preparing in new[] { false, true })
                foreach (var delivering in new[] { false, true })
                {
                    var result = Evaluate(request =>
                    {
                        request.TurnBased = true;
                        request.RiderTurn = own;
                        request.TurnActing = acting;
                        request.TurnActingOrPreparing = preparing || acting;
                        request.DeliveringOwnShell = delivering;
                        request.RiderStandardCooldown = delivering ? 6f : 0f;
                        request.RiderMoveCooldown = delivering ? 3f : 0f;
                    });
                    TestRunner.True(!result.IsAllowed && result.RejectionCode == MountedCombatRejectionCode.WrongTurn &&
                        result.Reason == "Mounted Charge is not yet supported in turn-based mode.", "Unsafe TB admission: " + result.Reason);
                }
                TestRunner.True(Evaluate(request => request.TurnBased = false).IsAllowed, "RT charge was disabled.");
            });
            runner.Run("mounted charge delivery does not re-demand the action its own shell has spent", () =>
            {
                // The native full-round shell spends the rider action before it asks this mod to deliver.
                var realTime = Evaluate(request =>
                {
                    request.DeliveringOwnShell = true;
                    request.RiderStandardCooldown = 6f;
                });
                TestRunner.True(realTime.IsAllowed, "The delivery of the charge shell was refused the action it had spent: " + realTime.Reason);
                // The turn-based equivalent is not asserted here: increment 6B.3 is deferred with evidence
                // and the turn-based branch refuses outright, which its own test covers.
                // The exemption is exactly the two resources that shell pays for, and nothing else.
                Refuses(request =>
                {
                    request.DeliveringOwnShell = true;
                    request.RiderCanActInCombat = false;
                }, MountedCombatRejectionCode.WrongActionState, "a delivering shell whose rider cannot act");
                // The turn-based rule that prior movement is never exempted is unreachable while increment
                // 6B.3 is deferred, so it is not asserted here; the deferral refusal has its own test.
                Refuses(request =>
                {
                    request.DeliveringOwnShell = true;
                    request.AlreadyActiveCommand = true;
                }, MountedCombatRejectionCode.AlreadyActiveCommand, "a delivering shell over an active pair command");
                Refuses(request =>
                {
                    request.DeliveringOwnShell = true;
                    request.FeatureEnabled = false;
                }, MountedCombatRejectionCode.FeatureDisabled, "a delivering shell while the feature is off");
                // Without the proof of an executing shell the strict requirement still stands.
                Refuses(request => request.RiderStandardCooldown = 6f, MountedCombatRejectionCode.WrongActionState,
                    "a spent standard action outside its own shell");
            });

            runner.Run("mounted charge reads the stock charge geometry from the mount", () =>
            {
                Refuses(request => request.Distance = 3f, MountedCombatRejectionCode.OutsideSupportedRange, "a target inside the minimum charge distance");
                Refuses(request => request.Distance = 40f, MountedCombatRejectionCode.OutsideSupportedRange, "a target beyond the maximum charge distance");
                Refuses(request => request.StraightRoute = false, MountedCombatRejectionCode.NoPath, "an obstructed charge line");
                Refuses(request => request.LandingBlocked = true, MountedCombatRejectionCode.NoPath, "a blocked landing point");
                var exemption = Evaluate(request =>
                {
                    request.LandingBlocked = true;
                    request.MountAvoidanceDisabled = true;
                });
                TestRunner.True(exemption.IsAllowed, "The stock avoidance-disabled clearance exemption was refused: " + exemption.Reason);
                var boundaries = Evaluate(request => request.Distance = request.MinimumRange);
                TestRunner.True(boundaries.IsAllowed, "A target exactly at the minimum charge distance was refused.");
                var far = Evaluate(request => request.Distance = request.MaximumRange);
                TestRunner.True(far.IsAllowed, "A target exactly at the maximum charge distance was refused.");
            });
        }
    }
}
