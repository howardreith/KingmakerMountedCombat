using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Chunk 6B increment 6B.2: the charge conditions that are mutable during the transaction.
    //
    // Two properties matter as much as the individual rules. First, this policy must NOT re-demand the
    // rider's already-paid Standard action, and must NOT require an idle pair - a running charge owns the
    // mount's Move slot and the rider's Standard slot by design. Second, the minimum charge distance is an
    // admission condition only: arrival is necessarily inside it, so revalidating it would invalidate every
    // charge that was working.
    internal static class MountedChargeRevalidationTests
    {
        private static MountedChargeRevalidationRequest Valid(MountedChargeRevalidationPhase phase)
        {
            return new MountedChargeRevalidationRequest
            {
                Phase = phase,
                TargetValid = true,
                TargetVisible = true,
                TargetHostile = true,
                TargetAttackable = true,
                StraightRoute = true,
                LaneClear = true,
                LandingBlocked = false,
                MountAvoidanceDisabled = false,
                Distance = 6f,
                MaximumRange = 30.48f,
                RelationshipMounted = true,
                ExactPair = true,
                LifecycleBoundary = false,
                RiderOwnsAttackSlot = true,
                CarrierOwnsMountMoveSlot = phase != MountedChargeRevalidationPhase.BeforeAttackStart,
                MountQueueEmpty = true,
                FinalAttackAdmitted = true
            };
        }

        private static readonly MountedChargeRevalidationPhase[] AllPhases =
        {
            MountedChargeRevalidationPhase.BeforeRepath,
            MountedChargeRevalidationPhase.BeforeAttackTransition,
            MountedChargeRevalidationPhase.BeforeAttackStart
        };

        private static void InvalidAtEveryPhase(
            System.Action<MountedChargeRevalidationRequest> mutate,
            MountedCombatRejectionCode expected,
            string what)
        {
            foreach (var phase in AllPhases)
            {
                var request = Valid(phase);
                mutate(request);
                var outcome = MountedChargeRevalidation.Evaluate(request);
                TestRunner.True(!outcome.IsValid,
                    "A charge with " + what + " stayed valid at " + phase + ".");
                TestRunner.True(outcome.RejectionCode == expected,
                    "A charge with " + what + " gave " + outcome.RejectionCode + " at " + phase + ".");
            }
        }

        internal static void Register(TestRunner runner)
        {

            runner.Run("a forced line needs the lane as well as the centre, and a caller that omits it fails closed", () =>
            {
                var centreOnly = new MountedChargeRevalidationRequest();
                centreOnly.Phase = MountedChargeRevalidationPhase.BeforeRepath;
                centreOnly.StraightRoute = true; centreOnly.RelationshipMounted = true; centreOnly.ExactPair = true; centreOnly.TargetValid = true; centreOnly.TargetVisible = true;
                centreOnly.TargetHostile = true; centreOnly.TargetAttackable = true; centreOnly.RiderOwnsAttackSlot = true; centreOnly.CarrierOwnsMountMoveSlot = true; centreOnly.MountQueueEmpty = true;
                centreOnly.Distance = 5f; centreOnly.MaximumRange = 30f;
                var refused = MountedChargeRevalidation.Evaluate(centreOnly);
                TestRunner.True(!refused.IsValid && refused.RejectionCode == MountedCombatRejectionCode.NoPath, "A centre-clear, flank-unread line was authorized.");
                centreOnly.LaneClear = true;
                TestRunner.True(MountedChargeRevalidation.Evaluate(centreOnly).IsValid, "A clear lane was refused: " + MountedChargeRevalidation.Evaluate(centreOnly).Reason);
            });

            runner.Run("the lane decision reads the mount's own width along the line to the landing point", () =>
            {
                // A corridor 1.0 either side of the centre, narrowed to 0.5 between 3 and 4 metres along.
                System.Func<float, float, bool> corridor = (along, lateral) => lateral <= (along >= 3f && along <= 4f ? 0.5f : 1.0f);
                TestRunner.True(MountedChargeLane.Clear(10f, 2f, 0.5f, corridor), "A narrow mount was refused a gap it fits.");
                TestRunner.True(!MountedChargeLane.Clear(10f, 2f, 1.0f, corridor), "A wide mount was given a gap too narrow for its flanks.");
                TestRunner.True(MountedChargeLane.Clear(3f, 2f, 1.0f, (along, lateral) => along >= 0.9f), "The ground under the mount was asked.");
                TestRunner.True(MountedChargeLane.Clear(3f, 2f, 2f, (along, lateral) => false), "A stop inside the body's own length asked its own ground.");
                TestRunner.True(!MountedChargeLane.Clear(5f, 2f, 2f, (along, lateral) => along < 3f), "A stop past the body did not read its landing footprint.");
                TestRunner.True(MountedChargeLane.Clear(2f, 2f, 1f, (along, lateral) => false), "A target already in reach has no lane to read.");
                var sampled = new System.Collections.Generic.List<float>();
                MountedChargeLane.Clear(6f, 2f, 1f, (along, lateral) => { sampled.Add(along); return true; });
                TestRunner.True(sampled[0] == 1f && sampled[sampled.Count - 1] == 4f && sampled.Count == 7, "The lane was not sampled at half corpulence up to its landing point.");
            });

            runner.Run("only a stopped line on a live carrier asks to be forced again", () =>
            {
                TestRunner.True(MountedChargeLane.MustAuthorizeReforce(true, true, false), "A latched force mode with no path hid a stopped line.");
                TestRunner.True(MountedChargeLane.MustAuthorizeReforce(true, false, true), "A lost force mode was not read.");
                TestRunner.True(!MountedChargeLane.MustAuthorizeReforce(true, true, true), "A running line asked for a lane scan every tick.");
                TestRunner.True(!MountedChargeLane.MustAuthorizeReforce(false, false, false), "A finished carrier asked to be forced.");
            });

            runner.Run("a live charge is never destroyed by the route a request took", () =>
            {
                System.Func<MountedChargeRequestRoute, bool, bool, bool, bool, bool, MountedChargeRequestDecision> decide = (r, o, l, w, m, b) => MountedChargeRequestPolicy.Decide(r, o, l, w, m, b);
                TestRunner.True(MountedChargeRequestPolicy.Decide(MountedChargeRequestRoute.Run, false, true, false, true, false, true) == MountedChargeRequestDecision.RefuseMountEnqueue, "A mount Run that native would queue could mark the carrier to end.");
                TestRunner.True(MountedChargeRequestPolicy.Decide(MountedChargeRequestRoute.Run, true, true, false, true, false, true) == MountedChargeRequestDecision.Pass, "The carrier was refused by the same rule.");
                TestRunner.True(decide(MountedChargeRequestRoute.Enqueue, false, true, false, false, false) == MountedChargeRequestDecision.Pass, "A rider's pending follow-up did not pass.");
                TestRunner.True(decide(MountedChargeRequestRoute.Run, false, true, false, false, false) == MountedChargeRequestDecision.Pass, "A Run was not left to its later gates.");
                TestRunner.True(decide(MountedChargeRequestRoute.Enqueue, false, true, false, true, false) == MountedChargeRequestDecision.RefuseMountEnqueue, "A mount enqueue would be dropped by the next carrier.");
                TestRunner.True(decide(MountedChargeRequestRoute.Run, false, true, true, false, false) == MountedChargeRequestDecision.RefuseStarting, "A Run would clear the waiting attack.");
                TestRunner.True(decide(MountedChargeRequestRoute.Run, false, true, false, false, true) == MountedChargeRequestDecision.Refuse, "A brain proposal did not wait.");
                TestRunner.True(decide(MountedChargeRequestRoute.QueuePromotion, false, true, false, false, false) == MountedChargeRequestDecision.ReturnToQueue, "A promotion during a charge would be lost.");
                TestRunner.True(decide(MountedChargeRequestRoute.Run, true, true, true, true, false) == MountedChargeRequestDecision.Pass, "The charge's own command was refused.");
                foreach (MountedChargeRequestRoute route in System.Enum.GetValues(typeof(MountedChargeRequestRoute)))
                {
                    foreach (var waiting in new[] { false, true }) foreach (var mount in new[] { false, true }) foreach (var brain in new[] { false, true }) foreach (var owned in new[] { false, true })
                    {
                        var live = decide(route, owned, true, waiting, mount, brain);
                        TestRunner.True(live != MountedChargeRequestDecision.DrainThenPass && live != MountedChargeRequestDecision.DrainThenRefuse && live != MountedChargeRequestDecision.DrainElseReturnToQueue, "A live charge was drained: " + live);
                    }
                }
                TestRunner.True(decide(MountedChargeRequestRoute.QueuePromotion, false, false, false, false, false) == MountedChargeRequestDecision.DrainElseReturnToQueue, "A successor after the charge could be lost to its cleanup.");
                TestRunner.True(decide(MountedChargeRequestRoute.Run, true, false, false, false, false) == MountedChargeRequestDecision.DrainThenRefuse, "A retired shell or carrier could resume.");
            });
            runner.Run("a charge whose conditions still hold revalidates at every phase", () =>
            {
                foreach (var phase in AllPhases)
                {
                    var outcome = MountedChargeRevalidation.Evaluate(Valid(phase));
                    TestRunner.True(outcome.IsValid, "A valid charge was invalidated at " + phase + ": " + outcome.Reason);
                    TestRunner.True(outcome.Reason == MountedChargeRevalidation.ValidReason,
                        "A valid charge gave an unexpected reason at " + phase + ": " + outcome.Reason);
                    TestRunner.True(outcome.RejectionCode == null,
                        "A valid charge carried a rejection code at " + phase + ".");
                }
            });

            runner.Run("a charge revalidates the target at every phase", () =>
            {
                InvalidAtEveryPhase(r => r.TargetValid = false, MountedCombatRejectionCode.TargetInvalid, "a dead target");
                InvalidAtEveryPhase(r => r.TargetVisible = false, MountedCombatRejectionCode.TargetNotVisible, "an invisible target");
                InvalidAtEveryPhase(r => r.TargetHostile = false, MountedCombatRejectionCode.TargetNotHostile, "a friendly target");
                InvalidAtEveryPhase(r => r.TargetAttackable = false, MountedCombatRejectionCode.TargetNotAttackable, "an unattackable target");
            });

            runner.Run("a charge revalidates the exact pair and its lifecycle at every phase", () =>
            {
                InvalidAtEveryPhase(r => r.RelationshipMounted = false, MountedCombatRejectionCode.RelationshipInvalidated, "a dissolved relationship");
                InvalidAtEveryPhase(r => r.ExactPair = false, MountedCombatRejectionCode.RelationshipInvalidated, "another pair");
                InvalidAtEveryPhase(r => r.LifecycleBoundary = true, MountedCombatRejectionCode.LifecycleBoundary, "a lifecycle boundary");
            });

            runner.Run("a charge revalidates its own command ownership at every phase", () =>
            {
                InvalidAtEveryPhase(r => r.RiderOwnsAttackSlot = false, MountedCombatRejectionCode.AlreadyActiveCommand, "a replaced rider attack command");
                InvalidAtEveryPhase(r => r.MountQueueEmpty = false, MountedCombatRejectionCode.AlreadyActiveCommand, "a queued mount command");
            });

            runner.Run("a charge approaching revalidates geometry, and the attack boundary does not", () =>
            {
                foreach (var phase in new[]
                {
                    MountedChargeRevalidationPhase.BeforeRepath,
                    MountedChargeRevalidationPhase.BeforeAttackTransition
                })
                {
                    var obstructed = Valid(phase);
                    obstructed.StraightRoute = false;
                    var obstructedOutcome = MountedChargeRevalidation.Evaluate(obstructed);
                    TestRunner.True(!obstructedOutcome.IsValid && obstructedOutcome.RejectionCode == MountedCombatRejectionCode.NoPath,
                        "An obstructed line stayed valid at " + phase + ".");
                    TestRunner.True(obstructedOutcome.Reason == "The charge line to the target is obstructed.",
                        "The obstruction reason differs at " + phase + ": " + obstructedOutcome.Reason);

                    var blocked = Valid(phase);
                    blocked.LandingBlocked = true;
                    var blockedOutcome = MountedChargeRevalidation.Evaluate(blocked);
                    TestRunner.True(!blockedOutcome.IsValid && blockedOutcome.RejectionCode == MountedCombatRejectionCode.NoPath,
                        "A blocked landing stayed valid at " + phase + ".");

                    var fled = Valid(phase);
                    fled.Distance = fled.MaximumRange + 0.01f;
                    var fledOutcome = MountedChargeRevalidation.Evaluate(fled);
                    TestRunner.True(!fledOutcome.IsValid && fledOutcome.RejectionCode == MountedCombatRejectionCode.OutsideSupportedRange,
                        "A target beyond the maximum stayed valid at " + phase + ".");

                    var lostCarrier = Valid(phase);
                    lostCarrier.CarrierOwnsMountMoveSlot = false;
                    var lostOutcome = MountedChargeRevalidation.Evaluate(lostCarrier);
                    TestRunner.True(!lostOutcome.IsValid && lostOutcome.RejectionCode == MountedCombatRejectionCode.NoPath,
                        "A charge without its carrier stayed valid at " + phase + ".");
                }

                // At attack start the approach has consumed the straight route and the charge distance,
                // so neither is re-read. A newly blocking landing actor still invalidates, and so does
                // failing the engine's own final attack admission.
                var arrived = Valid(MountedChargeRevalidationPhase.BeforeAttackStart);
                arrived.StraightRoute = false;
                arrived.Distance = 1.2f;
                var arrivedOutcome = MountedChargeRevalidation.Evaluate(arrived);
                TestRunner.True(arrivedOutcome.IsValid,
                    "The attack boundary re-read consumed geometry: " + arrivedOutcome.Reason);

                var crowded = Valid(MountedChargeRevalidationPhase.BeforeAttackStart);
                crowded.LandingBlocked = true;
                var crowdedOutcome = MountedChargeRevalidation.Evaluate(crowded);
                TestRunner.True(!crowdedOutcome.IsValid && crowdedOutcome.RejectionCode == MountedCombatRejectionCode.NoPath,
                    "A newly blocking landing actor was allowed at attack start: " + crowdedOutcome.Reason);

                var unadmitted = Valid(MountedChargeRevalidationPhase.BeforeAttackStart);
                unadmitted.FinalAttackAdmitted = false;
                var unadmittedOutcome = MountedChargeRevalidation.Evaluate(unadmitted);
                TestRunner.True(!unadmittedOutcome.IsValid &&
                    unadmittedOutcome.RejectionCode == MountedCombatRejectionCode.OutsideSupportedRange,
                    "A charge that never reached native attack range was allowed to attack: " + unadmittedOutcome.Reason);

                // The approach phases must NOT demand the final attack admission.
                foreach (var approach in new[]
                {
                    MountedChargeRevalidationPhase.BeforeRepath,
                    MountedChargeRevalidationPhase.BeforeAttackTransition
                })
                {
                    var approaching = Valid(approach);
                    approaching.FinalAttackAdmitted = false;
                    TestRunner.True(MountedChargeRevalidation.Evaluate(approaching).IsValid,
                        "The approach demanded the final attack admission at " + approach + ".");
                }
            });

            runner.Run("a charge must have released the mount movement slot by attack start", () =>
            {
                var holding = Valid(MountedChargeRevalidationPhase.BeforeAttackStart);
                holding.CarrierOwnsMountMoveSlot = true;
                var outcome = MountedChargeRevalidation.Evaluate(holding);
                TestRunner.True(!outcome.IsValid && outcome.RejectionCode == MountedCombatRejectionCode.AlreadyActiveCommand,
                    "A charge still driving the mover was allowed to attack: " + outcome.Reason);
            });

            runner.Run("revalidation never re-demands the already-paid standard action or an idle pair", () =>
            {
                // The request type carries no action-cost input at all, which is the structural guarantee:
                // there is nothing here that could refuse the charge for the action that bought it. And the
                // pair is deliberately not idle - the running charge owns both slots - which every valid
                // case above asserts by having CarrierOwnsMountMoveSlot true while approaching.
                var approaching = Valid(MountedChargeRevalidationPhase.BeforeRepath);
                TestRunner.True(approaching.CarrierOwnsMountMoveSlot && approaching.RiderOwnsAttackSlot,
                    "The valid approaching case did not own both slots.");
                var outcome = MountedChargeRevalidation.Evaluate(approaching);
                TestRunner.True(outcome.IsValid,
                    "Owning both slots mid-charge was treated as a conflict: " + outcome.Reason);

                // The minimum charge distance must not be revalidated: arrival is inside it.
                var inside = Valid(MountedChargeRevalidationPhase.BeforeAttackTransition);
                inside.Distance = 0.4f;
                var insideOutcome = MountedChargeRevalidation.Evaluate(inside);
                TestRunner.True(insideOutcome.IsValid,
                    "A charge inside the minimum charge distance was invalidated at arrival: " + insideOutcome.Reason);
            });

            runner.Run("a charge with no revalidation request fails closed", () =>
            {
                var outcome = MountedChargeRevalidation.Evaluate(null);
                TestRunner.True(!outcome.IsValid, "A null revalidation request was treated as valid.");
            });
        }
    }
}
