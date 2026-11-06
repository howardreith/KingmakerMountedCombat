using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // F3: the mount's own allocation is the single locomotion entitlement that the
    // preview and execution share. The rider's spending never enters it.
    internal static class Finalize3MovementEconomyTests
    {
        private const float Speed = 5f;
        private const float Step = 2.286f;

        public static void Register(TestRunner runner)
        {
            runner.Run("F3 first in-budget mount move charges only the mount Move and keeps its Standard", FirstMoveKeepsStandard);
            runner.Run("F3 a deliberate second move is the native Standard conversion of the mount", SecondMoveConvertsStandard);
            runner.Run("F3 an unmoved mount keeps its whole allocation whatever the rider spent", UnmovedMountAllocation);
            runner.Run("F3 a spent five-foot step leaves no step and no ordinary locomotion", SpentStepClosesLocomotion);
            runner.Run("F3 D4 delivered charge travel closes pair travel and step without spending the mount Standard", ChargeClosesTravel);
            runner.Run("F3 refusal names the spent actor and points to an able partner only", RefusalNamesActor);
        }

        private static void FirstMoveKeepsStandard()
        {
            var mount = new MountedMovementState();
            float debit;
            var travelled = mount.Advance(1f, Speed, Step, 0f, false, false, false, false, false, false, false, out debit);
            TestRunner.Equal(1f, travelled, "In-budget travel was clamped.");
            TestRunner.Equal(1f, debit, "In-budget travel must debit only the mount Move time.");
            TestRunner.True(debit < 3f, "A first in-budget move must not reach the native Standard conversion.");
            TestRunner.Equal(2f, mount.Remaining(Speed, Step, debit, false, false, false, false),
                "The mount's first Move remainder is what the preview shows in green.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 3f, false, false, false, true),
                "Single-move mode keeps the mount Standard once the Move is spent.");
        }

        private static void SecondMoveConvertsStandard()
        {
            var mount = new MountedMovementState();
            float debit;
            mount.Advance(4f, Speed, Step, 0f, false, false, false, false, false, false, false, out debit);
            TestRunner.Equal(3f, debit, "One request is clamped to the remaining first Move window.");
            float second;
            mount.Advance(1f, Speed, Step, debit, false, false, false, false, false, false, false, out second);
            debit += second;
            TestRunner.True(debit > 3f, "A deliberate double move is native MoveAction above three seconds.");
            TestRunner.Equal(2f, mount.Remaining(Speed, Step, debit, false, false, false, false),
                "Only the converted remainder remains after a double move.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 3f, true, false, false, false),
                "A mount whose Standard is spent has no conversion left after its Move.");
        }

        private static void UnmovedMountAllocation()
        {
            // A rider full attack spends the rider's own cooldowns; the mount's
            // allocation only ever receives mount observations.
            var mount = new MountedMovementState();
            TestRunner.Equal(3f, mount.Remaining(Speed, Step, 0f, false, false, false, false),
                "The unmoved mount keeps its first Move for the preview.");
            TestRunner.Equal(Step / Speed, mount.Remaining(Speed, Step, 0f, false, false, true, false),
                "The unmoved mount keeps its own step.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 3f, true, false, false, false),
                "The mount's own spent actions stay spent.");
        }

        private static void SpentStepClosesLocomotion()
        {
            var mount = new MountedMovementState();
            float debit;
            mount.Advance(1f, Speed, Step, 0f, false, false, true, false, false, false, false, out debit);
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 0f, false, false, true, false), "A spent step left a step.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 0f, false, false, false, false), "A spent step left ordinary locomotion.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 0f, false, false, false, true), "A spent step left a single move.");
        }

        private static void ChargeClosesTravel()
        {
            var mount = new MountedMovementState();
            float debit;
            var travelled = mount.Advance(.6f, Speed * 2f, Step, 0f, false, false, false, false, true, false, false, out debit);
            TestRunner.Equal(.6f, travelled, "Forced charge travel is not clamped by the ordinary allowance.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, .6f, false, false, false, false),
                "After charge travel the retained mount Standard cannot become another move.");
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, .6f, false, false, true, false), "After charge travel no step remains.");
            TestRunner.Equal(0f, mount.Advance(1f, Speed, Step, .6f, false, false, false, false, false, false, false, out debit),
                "Execution refuses post-charge ordinary travel.");
            TestRunner.Equal(0f, debit, "A refused post-charge request spends nothing.");
            var restored = new MountedMovementState { TimeMoved = .6f, TimeForced = .6f };
            TestRunner.Equal(0f, restored.Remaining(Speed, Step, .6f, false, false, false, false),
                "The persisted forced-travel record keeps the restriction after reload.");
            var refused = new MountedMovementState();
            TestRunner.Equal(3f, refused.Remaining(Speed, Step, 0f, false, false, false, false),
                "A rejected charge changes nothing.");
        }

        private static void RefusalNamesActor()
        {
            TestRunner.Equal("Mounted Swordsman has no Standard action left this turn. Select Horse to attack with it.",
                UnifiedMountedStockAttackPolicy.NoStandardFeedback("Mounted Swordsman", "Horse", true), "Rider refusal wording.");
            TestRunner.Equal("Mounted Swordsman has no Standard action left this turn.",
                UnifiedMountedStockAttackPolicy.NoStandardFeedback("Mounted Swordsman", "Horse", false), "Spent partner was advertised.");
            TestRunner.Equal("This actor has no Standard action left this turn.",
                UnifiedMountedStockAttackPolicy.NoStandardFeedback(" ", null, true), "Missing names must not invent an actor.");
        }
    }
}
