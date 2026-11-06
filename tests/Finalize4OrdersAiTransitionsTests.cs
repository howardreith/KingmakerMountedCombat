using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // F4 (WORLD-01/02/03, AI-01/02, GAIT-01): the offline decisions behind each correction. The engine seams (the
    // native click, party re-initialisation, routine and chase, and the animation tick) are exercised natively.
    internal static class Finalize4OrdersAiTransitionsTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("F4 WORLD-03 a rider's exact chest or loot order is carried; doors, devices and derived kinds stay native", ContainerOrdersCarried);
            runner.Run("F4 WORLD-03 the terminal kind text keeps its own exhausted wording", TerminalKindWording);
            runner.Run("F4 WORLD-02 the native trap-click stop reaches the mount only inside an accepted real click", TrapGroupStopScope);
            runner.Run("F4 WORLD-03 a new real-time chest order replaces only the pair's own player travel, on its first tick", ContainerOrderReplacesOwnTravel);
            runner.Run("F4 WORLD-01 a removal inside native party re-initialisation waits and keeps a member back in the party", PartyReinitialisationRetention);
            runner.Run("F4 AI-02 a chase the pair is closing does not exhaust the bounded repath allowance; no progress and Charge still do", ProgressingChase);
            runner.Run("F4 AI-02 the elapsed bound grows only by the travel time to the initial gap, capped, and never for Charge", ApproachAllowance);
            runner.Run("F4 AI-01 a started routine continues on another target only after its target is down", RoutineContinuation);
            runner.Run("F4 GAIT-01 only the unimpaired party-paced Horse shows its run cycle; every other regime keeps the native gait", HorseGait);
        }

        private static void ContainerOrdersCarried()
        {
            TestRunner.Equal(MountedCarriedCommandKind.ContainerInteraction, MountedCarriedApproachPolicy.Classify(false, MountedCarriedInteractionClass.Container), "A chest or loot order was not classified.");
            TestRunner.Equal(MountedCarriedCommandKind.TrapInteraction, MountedCarriedApproachPolicy.Classify(false, MountedCarriedInteractionClass.TrapDisarm), "The trap classification changed.");
            TestRunner.Equal(MountedCarriedCommandKind.Ability, MountedCarriedApproachPolicy.Classify(true, MountedCarriedInteractionClass.NotInteraction), "The ability classification changed.");
            foreach (var other in new[] { MountedCarriedInteractionClass.Door, MountedCarriedInteractionClass.Other, MountedCarriedInteractionClass.NotInteraction })
                TestRunner.Equal(MountedCarriedCommandKind.None, MountedCarriedApproachPolicy.Classify(false, other), "A non-container interaction was carried: " + other + ".");
            TestRunner.Equal(MountedCarriedApproachDisposition.Carry, MountedCarriedApproachPolicy.Decide(true, true, false, false,
                MountedCarriedCommandKind.ContainerInteraction, false, false, false, false), "The rider's own container order was not carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, MountedCarriedApproachPolicy.Decide(true, true, false, false,
                MountedCarriedCommandKind.ContainerInteraction, false, false, false, true), "A simulated (hover) container order was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, MountedCarriedApproachPolicy.Decide(true, false, false, false,
                MountedCarriedCommandKind.ContainerInteraction, false, false, false, false), "Another unit's container order was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, MountedCarriedApproachPolicy.Decide(true, true, true, false,
                MountedCarriedCommandKind.ContainerInteraction, false, false, false, false), "A started (in-range) container order was carried.");
            TestRunner.True(MountedCarriedApproachPolicy.Feedback(MountedCarriedCommandKind.ContainerInteraction, "Horse").Contains("Horse"), "The accepted feedback must name the mount.");
        }

        private static void TerminalKindWording()
        {
            TestRunner.Equal(MountedCarriedCommandKind.ContainerInteraction, MountedCarriedApproachPolicy.ParseKind("ContainerInteraction"), "Container kind text did not parse.");
            TestRunner.Equal(MountedCarriedCommandKind.TrapInteraction, MountedCarriedApproachPolicy.ParseKind("TrapInteraction"), "Trap kind text did not parse.");
            TestRunner.Equal(MountedCarriedCommandKind.Ability, MountedCarriedApproachPolicy.ParseKind("unexpected"), "An unknown kind must keep the ability wording.");
            TestRunner.True(MountedCarriedApproachPolicy.ExhaustedFeedback(MountedCarriedCommandKind.ContainerInteraction, "Horse", "no path").StartsWith("Mounted interaction cancelled"),
                "An unreachable container must refuse with its own wording.");
        }

        private static void TrapGroupStopScope()
        {
            TestRunner.True(MountedTrapGroupStopPolicy.ShouldForward(true, false, true, true), "The accepted trap click must stop the selected rider's mount.");
            TestRunner.True(!MountedTrapGroupStopPolicy.ShouldForward(false, false, true, true), "An InterruptMove outside an accepted trap click must not reach the mount.");
            TestRunner.True(!MountedTrapGroupStopPolicy.ShouldForward(true, true, true, true), "A hover or prediction simulation must not stop the mount.");
            TestRunner.True(!MountedTrapGroupStopPolicy.ShouldForward(true, false, false, true), "An unmounted pair has no mount travel to stop.");
            TestRunner.True(!MountedTrapGroupStopPolicy.ShouldForward(true, false, true, false), "Another unit's stop must not reach this pair's mount.");
            TestRunner.True(MountedTrapGroupStopPolicy.IsAcceptedTrapClickScope(true, 10, 10, true), "The open same-frame trap scope was not recognised.");
            TestRunner.True(!MountedTrapGroupStopPolicy.IsAcceptedTrapClickScope(true, 9, 10, true), "A scope left open from an earlier frame must not count.");
            TestRunner.True(!MountedTrapGroupStopPolicy.IsAcceptedTrapClickScope(true, 10, 10, false), "A chest or other object click is not a trap stop.");
            TestRunner.True(!MountedTrapGroupStopPolicy.IsAcceptedTrapClickScope(false, 10, 10, true), "A closed scope must not count.");
            TestRunner.True(MountedTrapGroupStopPolicy.OpensTrapClickScope(true, false, false), "A real disarm click opens the stop scope.");
            TestRunner.True(MountedTrapGroupStopPolicy.OpensTrapClickScope(false, true, false), "A real click on an object with a visible trap opens the stop scope.");
            TestRunner.True(!MountedTrapGroupStopPolicy.OpensTrapClickScope(true, true, true), "A hover or turn-based prediction simulation never opens it.");
            TestRunner.True(!MountedTrapGroupStopPolicy.OpensTrapClickScope(false, false, false), "An ordinary chest or object click never opens it.");
        }

        private static void ContainerOrderReplacesOwnTravel()
        {
            bool Replace(MountedCarriedCommandKind kind = MountedCarriedCommandKind.ContainerInteraction, bool first = true, bool tb = false,
                bool player = true, bool ownTravel = true, bool otherSlot = false, bool queueEmpty = true) =>
                MountedCarriedApproachPolicy.NewOrderReplacesOwnTravel(kind, first, tb, player, ownTravel, otherSlot, queueEmpty);
            TestRunner.True(Replace(), "A new real-time chest order must replace the pair's own player travel.");
            TestRunner.True(!Replace(first: false), "A waiting order never cancels a later player move.");
            TestRunner.True(!Replace(tb: true), "Turn-based movement is never replaced by an object order.");
            TestRunner.True(!Replace(player: false), "A non-player order never replaces player travel.");
            TestRunner.True(!Replace(ownTravel: false), "Only the pair's own player-created UnitMoveTo yields.");
            TestRunner.True(!Replace(otherSlot: true), "A mount with another live slot keeps it.");
            TestRunner.True(!Replace(queueEmpty: false), "A queued mount order keeps its movement.");
            TestRunner.True(!Replace(kind: MountedCarriedCommandKind.Ability) && !Replace(kind: MountedCarriedCommandKind.TrapInteraction),
                "Spells keep the existing deferral; traps get the native group stop instead.");
        }

        private static void PartyReinitialisationRetention()
        {
            TestRunner.Equal(PartyRemovalDisposition.DeferUntilReinitialisationEnds, PartyReinitialisationRetentionPolicy.OnRemoved(true, true),
                "A member that stays in game during party selection must wait for the re-add.");
            TestRunner.Equal(PartyRemovalDisposition.CleanupNow, PartyReinitialisationRetentionPolicy.OnRemoved(true, false),
                "A member left out of the new group must be cleaned up at once.");
            TestRunner.Equal(PartyRemovalDisposition.CleanupNow, PartyReinitialisationRetentionPolicy.OnRemoved(false, true),
                "An ordinary removal outside party selection must be cleaned up at once.");
            TestRunner.True(PartyReinitialisationRetentionPolicy.RetainAtEnd(true, true), "A member back in the party must keep its pair.");
            TestRunner.True(!PartyReinitialisationRetentionPolicy.RetainAtEnd(false, true), "A member not re-added must be cleaned up.");
            TestRunner.True(!PartyReinitialisationRetentionPolicy.RetainAtEnd(true, false), "A member out of game must be cleaned up.");
        }

        private static void ProgressingChase()
        {
            TestRunner.True(MountedChaseProgressPolicy.IsProgressingChase(false, true, 12f, 10f), "A closing chase must count as progress.");
            TestRunner.True(!MountedChaseProgressPolicy.IsProgressingChase(false, true, 12f, 11.9f), "Less than the epsilon is not progress.");
            TestRunner.True(!MountedChaseProgressPolicy.IsProgressingChase(true, true, 12f, 10f), "A Charge repath always counts.");
            TestRunner.True(!MountedChaseProgressPolicy.IsProgressingChase(false, false, 12f, 10f), "An unadmitted-after-move repath always counts.");
            TestRunner.True(!MountedChaseProgressPolicy.IsProgressingChase(false, true, float.NaN, 10f), "No baseline distance is not progress.");

            // Before F4 a target walking toward the pair faulted the approach on its fifth drift repath.
            var before = Approaching();
            var faulted = false;
            for (var i = 0; i < MountedCombatTransaction.MaximumRepaths + 1; i++) faulted |= !before.TryRepath("t");
            TestRunner.True(faulted, "The counted allowance must still be bounded.");

            var chase = Approaching();
            for (var i = 0; i < 12; i++) TestRunner.True(chase.TryRepath("t", true), "A closing chase repath was refused at " + i + ".");
            TestRunner.Equal(0, chase.RepathCount, "Progressing repaths must not consume the counted allowance.");
            TestRunner.Equal(12, chase.ProgressingRepathCount, "Progressing repaths must be recorded.");
            for (var i = 0; i < MountedCombatTransaction.MaximumRepaths; i++) TestRunner.True(chase.TryRepath("t", false), "A counted repath was refused early.");
            TestRunner.True(!chase.TryRepath("t", false), "A no-progress chase must still exhaust the allowance.");

            var bounded = Approaching();
            for (var i = 0; i < MountedCombatTransaction.MaximumProgressingRepaths; i++) bounded.TryRepath("t", true);
            TestRunner.True(bounded.TryRepath("t", true) && bounded.RepathCount == 1, "Beyond the progressing bound a chase repath counts again.");
            TestRunner.True(!Approaching().TryRepath("other", true), "A repath for another target must be refused.");
        }

        private static void ApproachAllowance()
        {
            TestRunner.Equal(2f, MountedChaseProgressPolicy.ApproachAllowanceSeconds(false, true, 10f, 5f), "The allowance is the travel time to the gap.");
            TestRunner.Equal(MountedChaseProgressPolicy.MaximumApproachAllowanceSeconds, MountedChaseProgressPolicy.ApproachAllowanceSeconds(false, true, 200f, 5f), "The allowance is capped.");
            TestRunner.Equal(0f, MountedChaseProgressPolicy.ApproachAllowanceSeconds(true, true, 10f, 5f), "Charge keeps its bound.");
            TestRunner.Equal(0f, MountedChaseProgressPolicy.ApproachAllowanceSeconds(false, false, 10f, 5f), "No approach, no allowance.");
            TestRunner.Equal(0f, MountedChaseProgressPolicy.ApproachAllowanceSeconds(false, true, -1f, 5f), "No gap, no allowance.");
        }

        private static void RoutineContinuation()
        {
            TestRunner.True(MountedRoutineContinuationPolicy.MayContinue(false, true, true, true, true), "A started routine whose target died must continue natively.");
            TestRunner.True(!MountedRoutineContinuationPolicy.MayContinue(false, true, true, true, false), "A standing target out of reach keeps the existing termination.");
            TestRunner.True(!MountedRoutineContinuationPolicy.MayContinue(false, true, true, false, true), "No remaining planned attack: nothing to continue.");
            TestRunner.True(!MountedRoutineContinuationPolicy.MayContinue(false, true, false, true, true), "A routine that delivered nothing does not continue.");
            TestRunner.True(!MountedRoutineContinuationPolicy.MayContinue(true, true, true, true, true), "Charge never continues on another target.");
            TestRunner.True(!MountedRoutineContinuationPolicy.MayContinue(false, false, true, true, true), "Only an attacking transaction continues.");
            TestRunner.True(MountedRoutineContinuationPolicy.WithinNativeContinuationRange(31f, 0.5f, 0.5f, 30.48f), "The native five-foot margin plus bow range admits the next goblin.");
            TestRunner.True(!MountedRoutineContinuationPolicy.WithinNativeContinuationRange(3.6f, 0.5f, 0.5f, 1.0f), "A melee continuation is limited to reach plus five feet.");

            var transaction = new MountedCombatTransaction();
            transaction.Arm(MountedCombatActionKind.RiderRanged);
            transaction.AcceptTarget("a", false);
            TestRunner.True(!transaction.TryContinueOnTarget("a", "b"), "A continuation before the routine started must be refused.");
            transaction.TryStartSingleAttack("a");
            TestRunner.True(!transaction.TryContinueOnTarget("x", "b"), "A continuation from a foreign target must be refused.");
            TestRunner.True(!transaction.TryContinueOnTarget("a", "a"), "A continuation onto the same target must be refused.");
            TestRunner.True(transaction.TryContinueOnTarget("a", "b") && transaction.TargetId == "b" && transaction.ContinuationTargetCount == 1, "The continuation did not move the transaction.");
            TestRunner.True(transaction.Complete("b"), "The continued routine must complete against its current target.");
        }

        private static void HorseGait()
        {
            const float Horse = 5.08f;
            bool Trot(bool horse = true, NativeGait gait = NativeGait.Slow, NativeGait type = NativeGait.Normal, bool overrideSpeed = false,
                bool combat = false, bool tb = false, float? limit = 3.048f, float own = Horse) =>
                MountedHorseGaitPolicy.ShouldShowTrot(horse, gait, type, overrideSpeed, combat, tb, limit, own, Horse);
            TestRunner.True(Trot(), "A party-paced unimpaired Horse must show its run cycle.");
            TestRunner.True(!Trot(gait: NativeGait.Normal), "A Horse already at Normal is left alone.");
            TestRunner.True(!Trot(type: NativeGait.Slow), "An explicit slow move keeps walking.");
            TestRunner.True(!Trot(gait: NativeGait.Stealth, type: NativeGait.Normal), "Stealth keeps its gait.");
            TestRunner.True(!Trot(gait: NativeGait.Charge, type: NativeGait.Charge), "Charge keeps its gait.");
            TestRunner.True(!Trot(own: 0.5f * Horse), "An impaired Horse (encumbrance, terrain, conditions) keeps walking.");
            TestRunner.True(!Trot(limit: null), "A solo Horse below its speed for another reason keeps walking.");
            TestRunner.True(!Trot(limit: 1.5f), "A pace below the trot floor keeps walking.");
            TestRunner.True(!Trot(combat: true), "Combat movement keeps the native gait.");
            TestRunner.True(!Trot(tb: true), "Turn-based movement keeps the native gait.");
            TestRunner.True(!Trot(overrideSpeed: true), "An overridden command speed keeps the native gait.");
            TestRunner.True(!Trot(horse: false), "Only the exact KMC Horse is presented differently.");
        }

        private static MountedCombatTransaction Approaching()
        {
            var transaction = new MountedCombatTransaction();
            transaction.Arm(MountedCombatActionKind.RiderMelee);
            transaction.AcceptTarget("t", true);
            return transaction;
        }
    }
}
