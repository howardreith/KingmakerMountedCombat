using KingmakerMountedCombat.Diagnostics;
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
            runner.Run("F3 a rider full attack forces no limit on the unmoved mount; explicit ground choices carry", MirrorsOnlyGroundChoices);
            runner.Run("F3 locomotion context follows the pair's selected actor, never an unrelated pair or unmounted actor", LocomotionContextOwnership);
            runner.Run("F3 owned cleanup closes EscMode once and only through its identified native owner", CleanupEscModeDecision);
            runner.Run("F3 movement-facing panel slots describe the mount only in a filled rider-selected ground preview", PanelSlotOwnership);
        }

        private static void PanelSlotOwnership()
        {
            TestRunner.True(PairedMovementInputPolicy.SlotsDescribeMount(true, true, true, true), "A filled rider-selected ground preview must show the mount.");
            TestRunner.True(!PairedMovementInputPolicy.SlotsDescribeMount(true, true, false, true), "A target, object or ability preview is the rider's.");
            TestRunner.True(!PairedMovementInputPolicy.SlotsDescribeMount(true, true, true, false), "Unfilled mount states were advertised.");
            TestRunner.True(!PairedMovementInputPolicy.SlotsDescribeMount(false, true, true, true), "A mount-selected or other-selected panel was redirected.");
            TestRunner.True(!PairedMovementInputPolicy.SlotsDescribeMount(true, false, true, true), "An unaddressable partner (other pair, unmounted, ended) was shown.");
            // A started double move keeps exactly its remaining conversion travel.
            TestRunner.True(PairedMovementInputPolicy.ConversionTravelRemains(4.2f, false, false, 9f), "Remaining conversion travel was hidden.");
            TestRunner.True(!PairedMovementInputPolicy.ConversionTravelRemains(3f, false, false, 9f), "An unstarted conversion is native's own Standard.");
            TestRunner.True(!PairedMovementInputPolicy.ConversionTravelRemains(4.2f, true, false, 9f), "A Standard the mount spent on an action was revived.");
            TestRunner.True(!PairedMovementInputPolicy.ConversionTravelRemains(4.2f, false, true, 9f), "A step or delivered Charge restriction was bypassed.");
            TestRunner.True(!PairedMovementInputPolicy.ConversionTravelRemains(6f, false, false, 0f), "Exhausted conversion travel was advertised.");
        }

        private static void CleanupEscModeDecision()
        {
            TestRunner.Equal(Finalize3EscModeAction.None, Finalize3CleanupEscModePolicy.Decide(false, false, true, false, true, false, false),
                "EscMode was closed before the fixture's own restorations finished.");
            TestRunner.Equal(Finalize3EscModeAction.None, Finalize3CleanupEscModePolicy.Decide(true, true, true, false, true, false, false),
                "A second intervention was attempted.");
            TestRunner.Equal(Finalize3EscModeAction.None, Finalize3CleanupEscModePolicy.Decide(true, false, false, false, true, false, true),
                "A window was closed outside EscMode.");
            TestRunner.Equal(Finalize3EscModeAction.None, Finalize3CleanupEscModePolicy.Decide(true, false, true, true, false, null, true),
                "A paused game was touched; the pause restoration owns it.");
            TestRunner.Equal(Finalize3EscModeAction.CloseTutorial, Finalize3CleanupEscModePolicy.Decide(true, false, true, false, true, false, true),
                "A shown full-screen tutorial owns EscMode first.");
            TestRunner.Equal(Finalize3EscModeAction.RefuseTutorialSetting, Finalize3CleanupEscModePolicy.Decide(true, false, true, false, true, true, false),
                "Closing with don't-show ticked would change the player's tutorial setting.");
            TestRunner.Equal(Finalize3EscModeAction.RefuseTutorialSetting, Finalize3CleanupEscModePolicy.Decide(true, false, true, false, true, null, false),
                "An unreadable toggle must not be assumed off.");
            TestRunner.Equal(Finalize3EscModeAction.CloseEscMenu, Finalize3CleanupEscModePolicy.Decide(true, false, true, false, false, null, true),
                "A shown Esc menu closes through its own window.");
            TestRunner.Equal(Finalize3EscModeAction.RefuseUnidentified, Finalize3CleanupEscModePolicy.Decide(true, false, true, false, false, null, false),
                "An unidentified EscMode owner must not be closed blindly.");
        }

        private enum Limit { TwoActions, FiveFootStep, OneAction }

        private static void MirrorsOnlyGroundChoices()
        {
            Limit mirrored = default(Limit);
            var has = false;
            // Native forces FiveFootStep on a rider whose full attack spent its Move: not a ground choice.
            var forced = PairedMovementInputPolicy.IsGroundLimitChoice(false, false);
            TestRunner.True(!forced, "A spent rider's forced step limit was taken as a ground choice.");
            TestRunner.Equal(Limit.TwoActions, PairedMovementInputPolicy.MountLimit(Limit.FiveFootStep, forced, ref mirrored, ref has, Limit.TwoActions),
                "The unmoved mount must keep its unrestricted limit after a rider full attack.");
            var mount = new MountedMovementState();
            TestRunner.Equal(3f, mount.Remaining(Speed, Step, 0f, false, false, false, false),
                "That limit exposes the mount's untouched first Move.");
            // A hovered target's smart variant rewrites the rider limit: not a ground choice either.
            TestRunner.Equal(Limit.TwoActions, PairedMovementInputPolicy.MountLimit(Limit.OneAction,
                PairedMovementInputPolicy.IsGroundLimitChoice(true, true), ref mirrored, ref has, Limit.TwoActions), "A hover limit poisoned the mount.");
            // The player's explicit step and single-move choices carry, and survive a later forced limit.
            TestRunner.Equal(Limit.FiveFootStep, PairedMovementInputPolicy.MountLimit(Limit.FiveFootStep,
                PairedMovementInputPolicy.IsGroundLimitChoice(true, false), ref mirrored, ref has, Limit.TwoActions), "Explicit step choice was dropped.");
            TestRunner.Equal(Limit.FiveFootStep, PairedMovementInputPolicy.MountLimit(Limit.TwoActions, forced, ref mirrored, ref has, Limit.TwoActions),
                "A forced rider limit replaced the explicit choice.");
            TestRunner.Equal(Limit.OneAction, PairedMovementInputPolicy.MountLimit(Limit.OneAction,
                PairedMovementInputPolicy.IsGroundLimitChoice(true, false), ref mirrored, ref has, Limit.TwoActions), "Explicit single-move choice was dropped.");
            // The mount's own spending still restricts it: single-move mode after its Move is spent.
            TestRunner.Equal(0f, mount.Remaining(Speed, Step, 3f, false, false, false, true), "The mount's own spent Move was ignored.");
            // A new activation (partner context disposed) starts with no carried choice.
            has = false;
            TestRunner.Equal(Limit.TwoActions, PairedMovementInputPolicy.MountLimit(Limit.FiveFootStep, forced, ref mirrored, ref has, Limit.TwoActions),
                "A previous activation's choice leaked into the next.");
        }

        private static void LocomotionContextOwnership()
        {
            TestRunner.True(PairedMovementInputPolicy.MountOwnsLocomotion(true, true, false), "Rider-selected locomotion must read the mount.");
            TestRunner.True(PairedMovementInputPolicy.MountOwnsLocomotion(true, false, true), "Mount-selected locomotion must read the mount.");
            TestRunner.True(!PairedMovementInputPolicy.MountOwnsLocomotion(true, false, false), "An unrelated selected actor was given the pair's mount.");
            TestRunner.True(!PairedMovementInputPolicy.MountOwnsLocomotion(false, true, false), "An unaddressable (other pair, dismounted, ended) mount was used.");
            TestRunner.True(PairedMovementInputPolicy.MirrorsRiderInput(true, true, false), "Rider input must reach the idle mount.");
            TestRunner.True(!PairedMovementInputPolicy.MirrorsRiderInput(false, true, false), "Mount-selected input is the mount's own; nothing to mirror.");
            TestRunner.True(!PairedMovementInputPolicy.MirrorsRiderInput(true, true, true), "Input was rewritten under a moving mount.");
            TestRunner.True(!PairedMovementInputPolicy.MirrorsRiderInput(true, false, false), "Input was mirrored onto a mount that is not directly controllable.");
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
