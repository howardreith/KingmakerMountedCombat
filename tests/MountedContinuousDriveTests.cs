using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues sprint 3 continuation (M05): gamepad stick movement of a mounted pair.
    //
    // What these tests execute is production code: MountedContinuousDriveOwner (bind after Init, settle after Run and
    // before the invariant, release with the pair, retire only the bound agent), MountedContinuousDriveWatch (the
    // ownership record), MountedContinuousDrivePolicy (which unit the stick drives, which unit anchors the formation,
    // when the mount's override is admissible, what may be retired), CommandRouter and MountedGroupPacePolicy. The game adapter that answers the owner's questions from live units
    // (GameMountedRelationshipService.ContinuousDriveHost) is not executed here; a model host stands in for it. The
    // production code is driven through a small model of the installed native stick chain (decompiled Kingmaker
    // 2.1.7b, as a summary):
    //   InGameInputLayer.UpdateMovement: reads the current unit once; if that unit holds no live continuous command it
    //       interrupts the unit's moving orders, makes a UnitMoveContiniously, Init(unit), unit.Commands.Run(command),
    //       and then dereferences unit.Commands.MoveContiniously to Accelerate (stick at 0.95 or more) or Deaccelerate.
    //   UnitMoveContiniously.Init: installs the continuous agent as the executor view's AgentOverride.
    //       OnTick ends the command when that override is gone or the stick direction is zero; OnEnded removes whatever
    //       override the executor's view holds.
    //   UnitCommands.Run: refuses a command whose executor is not the container's owner.
    //   ClickGroundHandler.MoveSelectedUnitsToPoint: in gamepad mode the current unit gets no ground order while its
    //       AgentOverride is not null ("it is being stick-driven").
    //   UnitEntityView.AgentOverride = null destroys the old component at the end of the frame; until then
    //       GetComponent still finds it, and afterwards a reference to it compares equal to null.
    // The model is a reading of the native code, not native evidence. Corrected = false keeps what the reviewed
    // implementation (1223eb2f) did: the executor rewrite in the Init prefix alone, and the strict mount invariant.
    internal static class MountedContinuousDriveTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues M05: on the reviewed implementation a stick-driven mounted rider moves nothing, leaves the mount an unowned override, dismounts the pair and cannot remount", ReviewedImplementationFails);
            runner.Run("known issues M05: a selected mounted rider drives the pair: the command is initialised, run, looked up and ended on the mount, and the pair stays mounted", SelectedRiderDrivesThePair);
            runner.Run("known issues M05: held input keeps one command; the stick threshold accelerates and decelerates it natively; release ends it and native removes the override", HeldAcceleratedReleased);
            runner.Run("known issues M05: a ground order replaces the stick movement, and a new push binds a new command; the earlier command and agent are no longer owned", CommandReplacement);
            runner.Run("known issues M05: changing the selected unit ends the pair's stick movement natively and the pair then follows as a companion", SelectionReplacement);
            runner.Run("known issues M05: a command native Run does not admit leaves no override: it is retired at once and the pair stays mounted (native still faults on the missing command, as it does for any unit); the next push works", AdmissionFailureLeavesNothing);
            runner.Run("known issues M05: a stick held through refused admissions is retired every time and reported once", RepeatedRefusalIsReportedOnce);
            runner.Run("known issues M05: when the party enters combat the stick can no longer feed the pair's movement, and the owned command is ended instead of holding the mount's Move slot", CombatEndsTheOwnedDrive);
            runner.Run("known issues M05: an order given to the rider during stick movement ends that movement, as native Run ends a unit's own; a stick still held then cancels the order", RiderOrderReplacesTheDrive);
            runner.Run("known issues M05: stick movement clears the rider's Hold, as a new order does", StickMovementClearsRiderHold);
            runner.Run("known issues M05: a push in the frame its predecessor ended adopts the dying agent; once that is destroyed nothing is owned or admitted through it, and the next push works", DyingAgentAdoptedInTheSameFrame);
            runner.Run("known issues M05: when the driven-unit hook faults, the rider's rewritten command is refused by native Run and its override is still retired", ProjectionFaultStillRetires);
            runner.Run("known issues M05: an initialisation that installs no agent binds nothing", InitialisationWithoutAgentBindsNothing);
            runner.Run("known issues M05: dismounting during stick movement ends the owned command and override, and the pair can mount and be driven again", DismountCleanupAndRemount);
            runner.Run("known issues M05: two pairs own their own stick movement only", TwoPairs);
            runner.Run("known issues M05: an unmounted unit, a free pet and a companion are driven exactly as native drives them", UnrelatedActorsAndUnmountedControls);
            runner.Run("known issues M05: a ground order reaches a mounted rider that is not being stick-driven (skipped in gamepad mode on the reviewed implementation), and mouse play is unchanged", GroundOrdersReachTheRider);
            runner.Run("known issues M05: while the pair is stick-driven the follower pass gives the pair no formation order and offers it no follower pace", FollowerPassSkipsTheDrivenPair);
            runner.Run("known issues M05: a foreign override, a stale command, a stale agent and another pair generation are never owned and never retired", ForeignAndStaleOwnershipRejected);
            runner.Run("known issues M05: the ownership record decides admission within the binding frame only", AdmissionWindowIsOneFrame);
            runner.Run("known issues M05: binding needs this pair's mount as the executor and a live continuous agent on the mount's view", BindingRequiresTheMountAndItsAgent);
            runner.Run("known issues M05: the retirement plan touches nothing unless the exact bound agent is still installed", RetirementPlan);
        }

        private static void ReviewedImplementationFails()
        {
            var world = new World { Corrected = false };
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            TestRunner.Equal(1, world.RefusedRuns, "Native Run should refuse the rewritten command on the rider's own slots.");
            TestRunner.True(world.Rider.Slot == null && world.Mount.Slot == null, "A continuous command ran although its executor and its container differ.");
            TestRunner.True(world.NativeFaults >= 1, "Native dereferences the rider's missing continuous command.");
            TestRunner.True(world.Mount.Override != null && world.Mount.Override.Continuous, "Setup: Init leaves the mount its continuous agent.");
            world.Frame();
            TestRunner.True(!world.Pair.Mounted && world.Pair.Fault != null, "The strict invariant should have dismounted the pair.");
            TestRunner.Equal(0f, world.Mount.Position, "The mount should not have moved.");
            // Nothing owns that override, so nothing ever removes it, and the mount can no longer be mounted.
            world.Stick = 0f;
            for (var i = 0; i < 5; i++) world.Frame();
            TestRunner.True(world.Mount.Override != null, "The unowned override was removed on the reviewed implementation.");
            TestRunner.True(!world.CanMount(world.Pair), "The pair could remount although the mount still carries an override.");
        }

        private static void SelectedRiderDrivesThePair()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 0.5f;
            world.Frame();
            var command = world.Mount.Slot;
            TestRunner.True(command != null && command.Continuous && ReferenceEquals(command.Executor, world.Mount), "The command is not the mount's, in the mount's Move slot.");
            TestRunner.True(world.Rider.Slot == null, "The rider holds a movement command of its own.");
            TestRunner.Equal(0, world.RefusedRuns + world.NativeFaults, "Native refused or faulted.");
            TestRunner.True(ReferenceEquals(world.Rider.Override, world.Pair.RiderAgent), "The rider's own mounted agent was replaced.");
            TestRunner.True(ReferenceEquals(world.Pair.Drive.Command, command) && ReferenceEquals(world.Pair.Drive.Agent, world.Mount.Override),
                "The ownership record is not the exact command and the exact agent its Init installed.");
            world.Frame();
            world.Frame();
            TestRunner.True(world.Pair.Mounted && world.Pair.Fault == null, "The pair was dismounted while its own stick movement ran: " + world.Pair.Fault);
            TestRunner.True(world.Mount.Position > 0f && world.Rider.Position == world.Mount.Position, "The mount did not move, or the rider did not follow it.");
            TestRunner.Equal(0L, world.Pair.Drive.RetiredCount + world.Pair.Interrupted, "The mod retired or interrupted a healthy drive.");
            TestRunner.True(MountedContinuousDrivePolicy.DrivesPhysicalMover(true, true, true) && !MountedContinuousDrivePolicy.DrivesPhysicalMover(false, true, true) &&
                !MountedContinuousDrivePolicy.DrivesPhysicalMover(true, false, true) && !MountedContinuousDrivePolicy.DrivesPhysicalMover(true, true, false),
                "The driven unit is not the mount exactly for a mounted pair's rider with a mount.");
        }

        private static void HeldAcceleratedReleased()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 0.5f;
            world.Frame();
            var command = world.Mount.Slot;
            for (var i = 0; i < 4; i++) world.Frame();
            TestRunner.True(ReferenceEquals(world.Mount.Slot, command), "Held input replaced its own command.");
            TestRunner.Equal(1, world.Created, "Held input created more than one command.");
            TestRunner.True(!command.Accelerated, "A half stick accelerated.");
            world.Stick = 1f;
            world.Frame();
            TestRunner.True(command.Accelerated, "A full stick did not accelerate the mount's command.");
            world.Stick = 0.6f;
            world.Frame();
            TestRunner.True(!command.Accelerated, "Easing the stick did not decelerate the mount's command.");
            world.Stick = 0f;
            world.Frame();
            world.Frame();
            TestRunner.True(command.Finished && world.Mount.Override == null, "Release did not end the command and remove its override natively.");
            TestRunner.True(!world.Pair.Drive.IsBound, "The ownership record outlived its command.");
            TestRunner.Equal(0L, world.Pair.Drive.RetiredCount + world.Pair.Interrupted, "The mod retired or interrupted what native had already ended.");
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted on release: " + world.Pair.Fault);
        }

        private static void CommandReplacement()
        {
            var world = new World { Gamepad = false };
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var first = world.Mount.Slot;
            var firstAgent = world.Mount.Override;
            // A ground order for the rider is routed to the mount; native Run replaces the continuous command there.
            world.Stick = 0f;
            world.GroundOrder(30f, world.Rider);
            TestRunner.True(first.Finished && world.Mount.Override == null, "The replaced stick movement was not ended natively.");
            TestRunner.True(world.Mount.Slot != null && !world.Mount.Slot.Continuous, "The routed ground order is not in the mount's Move slot.");
            world.Frame();
            TestRunner.True(world.Pair.Mounted && !world.Pair.Drive.IsBound, "The replaced drive was not settled: " + world.Pair.Fault);
            // A new push replaces the ground order and binds a new command and a new agent.
            world.Stick = 1f;
            world.Frame();
            var second = world.Mount.Slot;
            TestRunner.True(second != null && second.Continuous && !ReferenceEquals(second, first), "The new push did not create a new command.");
            TestRunner.True(ReferenceEquals(world.Pair.Drive.Command, second), "The record is not the new command.");
            TestRunner.True(!world.Pair.Drive.OwnsMountOverride(firstAgent) && world.Pair.Drive.OwnsMountOverride(world.Mount.Override),
                "The earlier, removed agent is still owned, or the new one is not.");
            world.Frame();
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted after a replacement: " + world.Pair.Fault);
        }

        private static void SelectionReplacement()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Selected.AddRange(new[] { world.Rider, world.Companion });
            world.Stick = 1f;
            world.Frame();
            var pairCommand = world.Mount.Slot;
            world.Current = world.Companion;
            for (var i = 0; i < 4; i++) world.Frame(repath: true);
            TestRunner.True(pairCommand.Finished, "The pair's stick movement outlived the selection change.");
            TestRunner.True(world.Companion.Slot != null && world.Companion.Slot.Continuous && ReferenceEquals(world.Companion.Slot.Executor, world.Companion),
                "The newly selected unit is not driven natively.");
            TestRunner.True(world.Mount.Override == null && !world.Pair.Drive.IsBound, "The pair kept an override or a record after the selection change.");
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted by a selection change: " + world.Pair.Fault);
            // As a follower the rider receives the formation order, which is routed to the mount as in mouse play.
            TestRunner.True(world.Mount.Slot != null && !world.Mount.Slot.Continuous && world.Rider.Slot == null, "The following pair did not receive its routed formation order.");
        }

        private static void AdmissionFailureLeavesNothing()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.RunRefused = true;
            world.Frame();
            TestRunner.True(world.Mount.Slot == null, "Setup: Run should not have admitted the command.");
            TestRunner.True(world.Mount.Override == null, "A command that was never admitted left its override on the mount.");
            TestRunner.Equal(1L, world.Pair.Drive.RetiredCount, "The owned override was not retired exactly once.");
            TestRunner.True(world.Pair.Mounted && world.Pair.Fault == null, "The pair was dismounted by a failed admission: " + world.Pair.Fault);
            TestRunner.True(world.Rider.Slot == null && ReferenceEquals(world.Rider.Override, world.Pair.RiderAgent), "The rider was touched by a failed admission.");
            TestRunner.Equal(1, world.NativeFaults, "Native dereferences the command its Run did not admit, for the pair as for any unit; that is not repaired.");
            TestRunner.Equal(1, world.Pair.Interrupted, "The unadmitted command was not ended.");
            TestRunner.True(world.Pair.EndEvents == 0, "A command that never ran raised an end event.");
            // The next push is admitted and works.
            world.RunRefused = false;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && world.Mount.Position > 0f, "The pair could not be driven after a failed admission.");
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted after recovering: " + world.Pair.Fault);

            // An unconscious mount: native Run returns at once.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Mount.Conscious = false;
            world.Frame(validate: false);
            TestRunner.True(world.Mount.Slot == null && world.Mount.Override == null && world.Pair.Drive.RetiredCount == 1, "An unconscious mount kept an override.");
        }

        private static void RepeatedRefusalIsReportedOnce()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.RunRefused = true;
            for (var i = 0; i < 6; i++) world.Frame();
            TestRunner.Equal(6L, world.Pair.Drive.OrphanCount, "Each refused command should have been retired.");
            TestRunner.Equal(1, world.Pair.Warnings, "A held stick with refused admissions was reported every frame.");
            TestRunner.True(world.Pair.Mounted && world.Mount.Override == null, "The pair did not survive repeated refusals: " + world.Pair.Fault);
            // An admitted drive ends the streak; a later refusal is reported again.
            world.RunRefused = false;
            world.Frame();
            world.Stick = 0f;
            world.Frame(); world.Frame();
            world.RunRefused = true;
            world.Stick = 1f;
            world.Frame();
            TestRunner.Equal(2, world.Pair.Warnings, "A new streak of refusals was not reported.");
        }

        private static void CombatEndsTheOwnedDrive()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            // Turn-based combat: native neither feeds the stick nor ticks the mount's command outside its turn.
            world.InCombat = true;
            world.TurnBasedHold = true;
            world.Frame();
            TestRunner.True(command.Finished && command.EndEventRaised, "The owned command still holds the mount's Move slot in combat.");
            TestRunner.True(world.Mount.Override == null && !world.Pair.Drive.IsBound, "The pair kept its override or record in combat.");
            TestRunner.True(world.Pair.Mounted && world.Pair.Fault == null, "Entering combat dismounted the pair: " + world.Pair.Fault);
            world.Frame();
            TestRunner.True(world.Mount.Slot == null, "The mount's Move slot is not free for the pair's combat orders.");
            TestRunner.Equal(1, world.Created, "The stick created a command while the party is in combat.");
            // After combat the stick drives the pair again.
            world.InCombat = false;
            world.TurnBasedHold = false;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && world.Pair.Drive.IsBound && world.Pair.Mounted, "The pair is not driven again after combat.");
        }

        private static void RiderOrderReplacesTheDrive()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            // The rider is given an order of its own (an interaction it carries itself, not a routed ground move).
            var order = world.RiderOrder(standard: true);
            TestRunner.True(command.Finished && command.EndEventRaised && world.Mount.Override == null, "The rider's new order did not end the pair's stick movement.");
            TestRunner.True(!world.Pair.Drive.IsBound, "The record outlived the replaced movement.");
            // The stick is still held: native creates a new command and, taking the unit over, ends the rider's order.
            world.Frame();
            TestRunner.True(order.Finished, "A held stick did not cancel the rider's order.");
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && !ReferenceEquals(world.Mount.Slot, command), "The held stick did not take the pair again.");
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted: " + world.Pair.Fault);
            // Released before the order: the order stays.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Stick = 0f;
            order = world.RiderOrder(standard: true);
            world.Frame(); world.Frame();
            TestRunner.True(!order.Finished && world.Mount.Slot == null && world.Mount.Override == null, "A released stick cancelled the rider's order, or the drive lingered.");
            // A Free order replaces nothing, natively or here.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            world.RiderOrder(standard: false);
            TestRunner.True(!command.Finished && world.Pair.Drive.IsBound, "An order that is neither Move nor Standard ended the stick movement.");
            TestRunner.True(MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, true, true) && !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(false, true, true) &&
                !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, false, true) && !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, true, false),
                "The replacement decision is not a bound drive and a Move or Standard order admitted on the rider's container.");
        }

        private static void StickMovementClearsRiderHold()
        {
            var world = new World();
            world.Rider.Hold = true;
            world.Mount.Hold = true;
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            TestRunner.True(!world.Mount.Hold, "Setup: native Run clears Hold on the unit it runs the command on.");
            TestRunner.True(!world.Rider.Hold, "The selected rider stayed on Hold while it is stick-driven.");
            // A refused admission clears nothing.
            world = new World();
            world.Rider.Hold = true;
            world.RunRefused = true;
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            TestRunner.True(world.Rider.Hold, "A refused command cleared the rider's Hold.");
        }

        private static void DyingAgentAdoptedInTheSameFrame()
        {
            var world = new World { Gamepad = false };
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var first = world.Mount.Slot;
            var dying = world.Mount.Override;
            // The movement is replaced and, in the same frame, the held stick pushes again: Init finds the component
            // that is only destroyed at the end of the frame.
            world.GroundOrder(30f, world.Rider);
            world.Frame();
            var second = world.Mount.Slot;
            TestRunner.True(first.Finished && second != null && second.Continuous && !ReferenceEquals(first, second), "Setup: a second command in the frame the first ended.");
            TestRunner.True(ReferenceEquals(world.Pair.Drive.Agent, dying) && dying.Destroyed, "Setup: the second command adopted the dying agent, now destroyed.");
            TestRunner.True(!world.Pair.Drive.OwnsMountOverride(dying), "A destroyed agent is owned.");
            // Native ends the second command (its agent is gone) and leaves the dead reference; the owner clears it.
            world.Frame();
            TestRunner.True(second.Finished && world.Pair.Mounted && world.Pair.Fault == null, "The pair was dismounted over a destroyed agent: " + world.Pair.Fault);
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && !ReferenceEquals(world.Mount.Slot, second) &&
                world.Mount.Override != null && !world.Mount.Override.Destroyed && !ReferenceEquals(world.Mount.Override, dying),
                "The next push did not get a fresh agent.");
            world.Frame();
            TestRunner.True(world.Pair.Mounted && world.Pair.Drive.OwnsMountOverride(world.Mount.Override) && world.Mount.Position > 0f, "The pair is not driven after the hiccup.");
        }

        private static void ProjectionFaultStillRetires()
        {
            var world = new World { ProjectionFaults = true };
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            TestRunner.Equal(1, world.RefusedRuns, "Setup: without the projection the rewritten command is refused on the rider's slots.");
            TestRunner.True(world.Mount.Override == null && world.Pair.Drive.RetiredCount == 1, "The orphaned override survived the projection fault.");
            world.Stick = 0f;
            world.Frame();
            TestRunner.True(world.Pair.Mounted && world.Pair.Fault == null, "The pair was dismounted although the override was retired: " + world.Pair.Fault);
            TestRunner.True(ReferenceEquals(world.Rider.Override, world.Pair.RiderAgent), "The rider's own mounted agent was replaced.");
            TestRunner.True(world.CanMount(world.Pair) || world.Pair.Mounted, "The pair cannot remain or become mounted after the fault.");
        }

        private static void InitialisationWithoutAgentBindsNothing()
        {
            var world = new World();
            world.Mount.HasView = false;
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame(validate: false);
            TestRunner.True(!world.Pair.Drive.IsBound && world.Pair.Drive.RetiredCount == 0 && world.Mount.Override == null, "Something was bound or retired without an installed agent.");
        }

        private static void DismountCleanupAndRemount()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            world.Dismount(world.Pair);
            TestRunner.True(command.Finished && command.EndEventRaised, "The owned command was not ended natively at the dismount.");
            TestRunner.True(world.Mount.Override == null && !world.Pair.Drive.IsBound, "The dismount left the mount an override or the pair a record.");
            TestRunner.True(world.CanMount(world.Pair), "The pair cannot remount after a dismount during stick movement.");
            // The stick, still held, now drives the rider on foot, natively.
            world.Frame();
            TestRunner.True(world.Rider.Slot != null && world.Rider.Slot.Continuous && ReferenceEquals(world.Rider.Slot.Executor, world.Rider), "The dismounted rider is not driven natively.");
            TestRunner.True(world.Mount.Slot == null || world.Mount.Slot.Finished, "The free mount is still driven.");
            // Mounted again: a new pair generation is driven and owned afresh.
            world.Stick = 0f;
            world.Frame(); world.Frame();
            world.MountUp(world.Pair);
            world.Stick = 1f;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Pair.Mounted && world.Mount.Slot != null && world.Mount.Slot.Continuous && ReferenceEquals(world.Pair.Drive.Command, world.Mount.Slot),
                "The remounted pair is not driven and owned: " + world.Pair.Fault);
        }

        private static void TwoPairs()
        {
            var world = new World();
            var second = world.AddPair("rider2", "mount2");
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Pair.Drive.IsBound && !second.Drive.IsBound, "The second pair gained a record from the first pair's stick movement.");
            TestRunner.True(second.Mount.Slot == null && second.Mount.Override == null && second.Mount.Position == 0f, "The second pair's mount was driven.");
            TestRunner.True(world.Pair.Mounted && second.Mounted, "A pair was dismounted.");
            // The first pair's agent on the second pair's mount is foreign there: still a fault, and nothing retires it.
            second.Mount.Override = world.Mount.Override;
            world.Validate(second);
            TestRunner.True(!second.Mounted && second.Fault != null, "A foreign override on the second pair's mount was accepted.");
            TestRunner.Equal(0L, second.Drive.RetiredCount, "The second pair retired an agent it does not own.");
            TestRunner.True(world.Pair.Mounted && world.Mount.Override != null, "The first pair lost its drive to the second pair's fault.");
        }

        private static void UnrelatedActorsAndUnmountedControls()
        {
            var world = new World();
            // A companion with no pair.
            world.Current = world.Companion;
            world.Stick = 1f;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Companion.Slot != null && ReferenceEquals(world.Companion.Slot.Executor, world.Companion) && world.Companion.Position > 0f,
                "An unrelated unit is not driven natively.");
            TestRunner.True(world.Mount.Slot == null && world.Mount.Override == null && !world.Pair.Drive.IsBound, "The pair was touched by another unit's stick movement.");
            // The same pair's units when they are not mounted.
            world = new World();
            world.Pair.Mounted = false;
            world.Rider.Override = null;
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Rider.Slot != null && ReferenceEquals(world.Rider.Slot.Executor, world.Rider) && world.Rider.Override != null && world.Rider.Override.Continuous,
                "An unmounted rider is not driven natively.");
            TestRunner.True(world.Mount.Slot == null && world.Mount.Override == null && !world.Pair.Drive.IsBound, "The free mount was driven for its unmounted rider.");
            world.Current = world.Mount;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Mount.Slot != null && world.Mount.Override != null && !world.Pair.Drive.IsBound, "A free pet is not driven natively, or it was bound.");
            world.Stick = 0f;
            world.Frame(); world.Frame();
            TestRunner.True(world.Mount.Override == null && world.Pair.Drive.RetiredCount == 0, "A free pet's override was not left to native.");
        }

        private static void GroundOrdersReachTheRider()
        {
            // Gamepad mode, the mounted rider is the current unit and nothing is stick-driven.
            var reviewed = new World { Corrected = false };
            reviewed.Current = reviewed.Rider;
            reviewed.Selected.Add(reviewed.Rider);
            reviewed.GroundOrder(30f);
            TestRunner.True(reviewed.Mount.Slot == null, "Setup: on the reviewed implementation native takes the rider's own mounted agent for stick movement and skips its order.");

            var world = new World();
            world.Current = world.Rider;
            world.Selected.Add(world.Rider);
            world.GroundOrder(30f);
            TestRunner.True(world.Mount.Slot != null && !world.Mount.Slot.Continuous && world.Mount.Slot.Destination == 30f, "The ground order did not reach the pair's mount.");
            TestRunner.True(world.Rider.Slot == null, "The rider holds the ground order itself.");
            world.Frame();
            TestRunner.True(world.Pair.Mounted && !world.Pair.Drive.IsBound, "A ground order disturbed the pair: " + world.Pair.Fault);

            // Mouse play never reads the stick-driven test: the same order, the same routing, before and after.
            foreach (var corrected in new[] { false, true })
            {
                var mouse = new World { Corrected = corrected, Gamepad = false };
                mouse.Current = mouse.Rider;
                mouse.Selected.AddRange(new[] { mouse.Rider, mouse.Companion });
                mouse.GroundOrder(30f);
                mouse.Frame();
                TestRunner.True(mouse.Mount.Slot != null && mouse.Mount.Slot.Destination == 30f && mouse.Companion.Slot != null && mouse.Rider.Slot == null && mouse.Pair.Mounted,
                    "Mouse ground orders changed (corrected=" + corrected + ").");
            }
        }

        private static void FollowerPassSkipsTheDrivenPair()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Selected.AddRange(new[] { world.Rider, world.Mount, world.Companion });
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            for (var i = 0; i < 3; i++) world.Frame(repath: true);
            TestRunner.True(ReferenceEquals(world.Mount.Slot, command) && !command.Finished, "The follower pass replaced the driven pair's own command.");
            TestRunner.True(world.Companion.Slot != null && !world.Companion.Slot.Continuous, "The companion received no formation order.");
            TestRunner.True(world.FormationAnchors.Count > 0 && world.FormationAnchors.All(anchor => ReferenceEquals(anchor, world.Rider)),
                "The formation is not anchored on the selected rider.");
            // The follower pace pass (M02) answers for a routed ground order only; a stick-driven mount holds none.
            TestRunner.Equal(MountedGamepadFollowerOffer.KeepSelectedUnit,
                MountedGroupPacePolicy.DecideGamepadFollower(true, false, true, true, false, false), "The follower pass offered pace to a stick-driven pair.");
            TestRunner.True(MountedContinuousDrivePolicy.FormationAnchorIsRider(true, true, true) && !MountedContinuousDrivePolicy.FormationAnchorIsRider(true, true, false) &&
                !MountedContinuousDrivePolicy.FormationAnchorIsRider(true, false, true) && !MountedContinuousDrivePolicy.FormationAnchorIsRider(false, true, true),
                "The formation anchor is not the rider exactly when the stick drives the mount of the selected rider.");
        }

        private static void ForeignAndStaleOwnershipRejected()
        {
            // A foreign override on the mount, with no drive at all.
            var world = new World();
            world.Mount.Override = world.Mount.Component = new Agent { Continuous = true };
            world.Validate(world.Pair);
            TestRunner.True(!world.Pair.Mounted && world.Pair.Fault != null, "An unowned continuous override on the mount was accepted.");
            TestRunner.True(world.Mount.Override != null && world.Pair.Drive.RetiredCount == 0, "An override nobody bound was retired.");

            // A foreign override that replaced the owned one while the command runs.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var foreign = new Agent { Continuous = false };
            world.Mount.Override = foreign;
            world.Validate(world.Pair);
            TestRunner.True(!world.Pair.Mounted, "A foreign override was accepted because a drive was bound.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && world.Pair.Drive.RetiredCount == 0 && world.Pair.Interrupted == 0,
                "The mod retired or ended something although the installed override is not the bound agent.");

            // The record itself.
            var watch = new MountedContinuousDriveWatch();
            object command = new object(), agent = new object(), view = new object();
            watch.Bind(command, agent, view, 7, 100);
            TestRunner.True(watch.Owns(agent, 7, true, false, true, 105), "The bound live drive is not owned.");
            TestRunner.True(!watch.Owns(new object(), 7, true, false, true, 105), "Another agent instance is owned.");
            TestRunner.True(!watch.Owns(agent, 8, true, false, true, 105), "Another pair generation owns the agent.");
            TestRunner.True(!watch.Owns(agent, 7, false, false, true, 105), "An unmounted pair owns the agent.");
            TestRunner.True(!watch.Owns(agent, 7, true, true, true, 105), "A finished command still owns the agent.");
            TestRunner.True(!watch.Owns(agent, 7, true, false, false, 105), "A command outside the mount's Move slot owns the agent after its frame.");
            TestRunner.True(!watch.Owns(null, 7, true, false, true, 105), "A missing agent is owned.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Unowned, watch.Settle(8, true, false, true, false, false, 105), "Another generation was kept.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Unowned, watch.Settle(7, false, false, true, false, false, 105), "An unmounted pair was kept.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Ended, watch.Settle(7, true, true, false, false, false, 105), "A finished command was not settled as ended.");
            TestRunner.Equal(MountedContinuousDriveSettlement.InputGated, watch.Settle(7, true, false, true, false, true, 105), "A running command the stick cannot feed was kept.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Orphaned, watch.Settle(7, true, false, false, false, true, 105), "A command outside the slot was kept because input is gated.");
            watch.Clear();
            TestRunner.True(!watch.IsBound && !watch.Owns(agent, 7, true, false, true, 105), "A cleared record still owns.");
            TestRunner.Equal(MountedContinuousDriveSettlement.None, watch.Settle(7, true, false, true, false, false, 105), "An empty record settled something.");

            TestRunner.True(MountedContinuousDrivePolicy.MountOverrideAdmissible(false, false) && MountedContinuousDrivePolicy.MountOverrideAdmissible(true, true) &&
                !MountedContinuousDrivePolicy.MountOverrideAdmissible(true, false), "The invariant admits an override that is not the owned live drive.");
        }

        private static void AdmissionWindowIsOneFrame()
        {
            var watch = new MountedContinuousDriveWatch();
            object command = new object(), agent = new object();
            watch.Bind(command, agent, new object(), 1, 50);
            // Between native Init and native Run, in the binding frame.
            TestRunner.True(watch.Owns(agent, 1, true, false, false, 50), "The agent is not owned between Init and Run.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Keep, watch.Settle(1, true, false, false, false, false, 50), "The record did not wait for Run in its frame.");
            // The next frame, still not in the slot: an orphan.
            TestRunner.True(!watch.Owns(agent, 1, true, false, false, 51), "The agent is owned a frame after an unobserved Run.");
            TestRunner.Equal(MountedContinuousDriveSettlement.Orphaned, watch.Settle(1, true, false, false, false, false, 51), "An unadmitted command was kept a frame later.");
            // Run observed and refused, in the same frame: an orphan at once.
            watch.Bind(command, agent, new object(), 1, 60);
            TestRunner.Equal(MountedContinuousDriveSettlement.Orphaned, watch.Settle(1, true, false, false, true, false, 60), "A refused Run was kept.");
            TestRunner.True(!watch.Owns(agent, 1, true, false, false, 60), "The agent is owned after its Run was refused.");
            // Run observed and admitted.
            watch.Bind(command, agent, new object(), 1, 70);
            TestRunner.Equal(MountedContinuousDriveSettlement.Keep, watch.Settle(1, true, false, true, true, false, 70), "An admitted Run was not kept.");
            TestRunner.True(watch.Owns(agent, 1, true, false, true, 90), "An admitted drive is not owned later.");
            // Later displaced from the slot without ending: an orphan, not a grace period.
            TestRunner.Equal(MountedContinuousDriveSettlement.Orphaned, watch.Settle(1, true, false, false, false, false, 70), "A displaced command was kept in its binding frame.");
        }

        private static void BindingRequiresTheMountAndItsAgent()
        {
            var world = new World();
            var pair = world.Pair;
            // Another unit's command.
            pair.Drive.Bind(new Command { Continuous = true, Executor = world.Companion });
            pair.Drive.Bind(new Command { Continuous = true, Executor = world.Rider });
            TestRunner.True(!pair.Drive.IsBound, "A command executing on another unit was bound.");
            // The mount's command, but no continuous agent on the mount's view.
            var command = new Command { Continuous = true, Executor = world.Mount };
            pair.Drive.Bind(command);
            world.Mount.Override = new Agent { Continuous = false };
            pair.Drive.Bind(command);
            world.Mount.Override = new Agent { Continuous = true, Destroyed = true };
            pair.Drive.Bind(command);
            TestRunner.True(!pair.Drive.IsBound, "A command was bound without a live continuous agent on the mount.");
            // An unmounted pair binds nothing.
            world.Mount.Override = new Agent { Continuous = true };
            pair.Mounted = false;
            pair.Drive.Bind(command);
            TestRunner.True(!pair.Drive.IsBound, "An unmounted pair bound a command.");
            pair.Mounted = true;
            pair.Drive.Bind(null);
            pair.Drive.Bind(command);
            TestRunner.True(pair.Drive.IsBound && ReferenceEquals(pair.Drive.Agent, world.Mount.Override) && pair.Drive.BindCount == 1, "The mount's own command and agent were not bound once.");
            pair.Drive.Bind(command);
            TestRunner.Equal(1L, pair.Drive.BindCount, "The same command was bound twice.");
            // Observing a Run of some other command settles nothing.
            pair.Drive.ObserveRun(new Command { Continuous = true, Executor = world.Mount });
            TestRunner.True(pair.Drive.IsBound, "Another command's Run settled the bound drive.");
        }

        private static void RetirementPlan()
        {
            foreach (MountedContinuousDriveSettlement settlement in Enum.GetValues(typeof(MountedContinuousDriveSettlement)))
                foreach (var finished in new[] { false, true })
                {
                    var untouched = MountedContinuousDrivePolicy.PlanRetirement(settlement, false, finished);
                    TestRunner.True(!untouched.ClearOverride && !untouched.InterruptCommand && !untouched.RaiseEndEvent,
                        "Something is retired although the bound agent is no longer installed (" + settlement + ").");
                }
            var kept = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Keep, true, false);
            TestRunner.True(!kept.ClearOverride && !kept.InterruptCommand, "A kept drive is retired.");
            foreach (var settlement in new[] { MountedContinuousDriveSettlement.InputGated, MountedContinuousDriveSettlement.Replaced })
            {
                var ends = MountedContinuousDrivePolicy.PlanRetirement(settlement, true, false);
                TestRunner.True(ends.ClearOverride && ends.InterruptCommand && ends.RaiseEndEvent, "A running drive is not ended natively and cleared (" + settlement + ").");
            }
            var orphan = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Orphaned, true, false);
            TestRunner.True(orphan.ClearOverride && orphan.InterruptCommand && !orphan.RaiseEndEvent, "An orphan is not ended quietly and cleared.");
            var unowned = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Unowned, true, false);
            TestRunner.True(unowned.ClearOverride && unowned.InterruptCommand && unowned.RaiseEndEvent, "A drive that lost its pair is not ended natively and cleared.");
            var ended = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Ended, true, true);
            TestRunner.True(ended.ClearOverride && !ended.InterruptCommand, "An ended command is interrupted again, or its leftover agent kept.");
            var finishedUnowned = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Unowned, true, true);
            TestRunner.True(finishedUnowned.ClearOverride && !finishedUnowned.InterruptCommand, "A finished command is interrupted.");
        }

        // ---- the model ----

        private sealed class Agent
        {
            public bool Continuous;
            public bool PendingDestroy;
            public bool Destroyed;
            public float Direction;
        }

        private sealed class Command
        {
            public Unit Executor;
            public bool Continuous;
            public bool Standard;
            public bool Free;
            public bool Finished;
            public bool Accelerated;
            public bool EndEventRaised;
            public float Destination;
        }

        private sealed class Unit
        {
            public string Name;
            public Command Slot;
            public Command OtherSlot;
            // UnitEntityView.AgentOverride (a reference that may point at a destroyed component) and the continuous
            // component GetComponent finds on the view.
            public Agent Override;
            public Agent Component;
            public float Position;
            public bool Conscious = true;
            public bool HasView = true;
            public bool Hold;
        }

        // A live component: Unity's == null is true for a destroyed one.
        private static Agent Live(Agent agent) => agent != null && !agent.Destroyed ? agent : null;

        // Native UnitEntityView.AgentOverride = null: the old component is destroyed at the end of the frame.
        private static void SetOverrideNull(Unit unit)
        {
            if (unit.Override != null) unit.Override.PendingDestroy = true;
            unit.Override = null;
        }

        // The model's stand-in for GameMountedRelationshipService.ContinuousDriveHost.
        private sealed class Pair : IMountedContinuousDriveHost
        {
            public World World;
            public Unit Rider, Mount;
            public bool Mounted = true;
            public long Generation = 1;
            public string Fault;
            public int Interrupted, EndEvents, Warnings, Reports;
            public readonly Agent RiderAgent = new Agent();
            public readonly MountedContinuousDriveOwner Drive;

            public Pair() { Drive = new MountedContinuousDriveOwner(this); }

            bool IMountedContinuousDriveHost.PairMounted => Mounted;
            long IMountedContinuousDriveHost.PairGeneration => Generation;
            int IMountedContinuousDriveHost.Frame => World.FrameNumber;
            bool IMountedContinuousDriveHost.StickInputGated => World.InCombat;
            object IMountedContinuousDriveHost.Mount => Mount;
            object IMountedContinuousDriveHost.MountView => Mount.HasView ? Mount : null;
            object IMountedContinuousDriveHost.ExecutorOf(object command) => ((Command)command).Executor;
            bool IMountedContinuousDriveHost.IsFinished(object command) => ((Command)command).Finished;
            bool IMountedContinuousDriveHost.InMountMoveSlot(object command) => ReferenceEquals(Mount.Slot, command);
            object IMountedContinuousDriveHost.OverrideOf(object view) => ((Unit)view).Override;
            bool IMountedContinuousDriveHost.IsContinuousAgent(object agent) => Live(agent as Agent) != null && ((Agent)agent).Continuous;
            void IMountedContinuousDriveHost.Interrupt(object command, bool raiseEndEvent)
            {
                Interrupted++;
                if (raiseEndEvent) EndEvents++;
                World.End((Command)command, raiseEndEvent);
            }
            void IMountedContinuousDriveHost.ClearOverride(object view) => SetOverrideNull((Unit)view);
            void IMountedContinuousDriveHost.ReleaseRiderHold() => Rider.Hold = false;
            void IMountedContinuousDriveHost.Report(MountedContinuousDriveSettlement settlement, string reason, bool warning)
            {
                Reports++;
                if (warning) Warnings++;
            }
        }

        private sealed class World
        {
            public bool Corrected = true;
            public bool Gamepad = true;
            public bool ProjectionFaults;
            public bool RunRefused;
            public bool InCombat;
            // Turn-based combat outside the unit's own turn: its commands are not ticked.
            public bool TurnBasedHold;
            public float Stick;
            public Unit Current;
            public int FrameNumber = 10;
            public int Created, RefusedRuns, NativeFaults;
            public readonly List<Unit> Selected = new List<Unit>();
            public readonly List<Unit> FormationAnchors = new List<Unit>();
            public readonly List<Pair> Pairs = new List<Pair>();
            public readonly Unit Companion = new Unit { Name = "companion" };
            public readonly Pair Pair;

            public World() { Pair = AddPair("rider", "mount"); }

            public Unit Rider => Pair.Rider;

            public Unit Mount => Pair.Mount;

            public Pair AddPair(string rider, string mount)
            {
                var pair = new Pair { World = this, Rider = new Unit { Name = rider }, Mount = new Unit { Name = mount } };
                pair.Rider.Override = pair.RiderAgent;
                Pairs.Add(pair);
                return pair;
            }

            private IEnumerable<Unit> Units => Pairs.SelectMany(pair => new[] { pair.Rider, pair.Mount }).Concat(new[] { Companion });

            private Pair PairOf(Unit unit) => Pairs.FirstOrDefault(pair => ReferenceEquals(pair.Rider, unit) || ReferenceEquals(pair.Mount, unit));

            // Production: the unit the stick drives for the selected unit.
            private Unit DrivenUnit(Unit selected)
            {
                if (!Corrected || ProjectionFaults) return selected;
                var pair = PairOf(selected);
                return pair != null && MountedContinuousDrivePolicy.DrivesPhysicalMover(pair.Mounted, ReferenceEquals(selected, pair.Rider), pair.Mount != null)
                    ? pair.Mount : selected;
            }

            // Production: the unit the formation is anchored on.
            private Unit FormationUnit(Unit unit)
            {
                if (!Corrected) return unit;
                var pair = PairOf(unit);
                return pair != null && MountedContinuousDrivePolicy.FormationAnchorIsRider(pair.Mounted, ReferenceEquals(unit, pair.Mount), ReferenceEquals(Current, pair.Rider))
                    ? pair.Rider : unit;
            }

            // Native "is the current unit being stick-driven": its override is not null. Production answers for a
            // mounted rider with its mount's override.
            private bool StickDriven(Unit current)
            {
                var read = current;
                if (Corrected)
                {
                    var pair = PairOf(current);
                    if (pair != null && MountedContinuousDrivePolicy.DrivesPhysicalMover(pair.Mounted, ReferenceEquals(current, pair.Rider), pair.Mount != null)) read = pair.Mount;
                }
                return Live(read.Override) != null;
            }

            private static Agent ContinuousAgent(Unit unit) => Live(unit.Override) != null && unit.Override.Continuous ? unit.Override : null;

            private static Command LiveContinuous(Unit unit) =>
                unit.Slot != null && !unit.Slot.Finished && unit.Slot.Continuous ? unit.Slot : null;

            // One frame: the game tick, the mod's update and validation, the stick layer (a LateUpdate), then Unity's
            // end-of-frame destruction.
            public void Frame(bool repath = false, bool validate = true)
            {
                FrameNumber++;
                Tick();
                if (validate) foreach (var pair in Pairs.ToList()) Validate(pair);
                UpdateMovement(repath);
                foreach (var unit in Units)
                    if (unit.Component != null && unit.Component.PendingDestroy) { unit.Component.Destroyed = true; unit.Component = null; }
            }

            private void Tick()
            {
                foreach (var unit in Units)
                {
                    if (unit.OtherSlot != null && unit.OtherSlot.Finished) unit.OtherSlot = null;
                    var command = unit.Slot;
                    if (command == null) continue;
                    if (command.Finished) { unit.Slot = null; continue; }
                    if (!command.Continuous || TurnBasedHold) continue;
                    // UnitMoveContiniously.OnTick, executed for the command's executor.
                    var agent = ContinuousAgent(command.Executor);
                    if (agent == null || agent.Direction <= 0.01f) End(command, true);
                }
                foreach (var unit in Units)
                {
                    // UnitMoveController.Tick: the unit moves through its override when one is installed.
                    var agent = ContinuousAgent(unit);
                    if (agent == null || agent.Direction <= 0.01f) continue;
                    unit.Position += agent.Direction;
                    agent.Direction *= 0.1f;
                }
                foreach (var pair in Pairs) if (pair.Mounted) pair.Rider.Position = pair.Mount.Position;
            }

            private void UpdateMovement(bool repath)
            {
                // Native returns before it reads the unit while the party is in combat.
                if (Current == null || InCombat) return;
                var unit = DrivenUnit(Current);
                var driven = ContinuousAgent(unit);
                var wasMoving = driven != null && driven.Direction > 0.01f;
                if (driven != null) driven.Direction = Stick;
                if (Stick <= 0.01f)
                {
                    if (wasMoving) MakeCompanionsFollow(FormationUnit(unit));
                    return;
                }
                if (LiveContinuous(unit) == null)
                {
                    InterruptMove(unit);
                    var command = new Command { Continuous = true };
                    Created++;
                    Init(command, unit);
                    Run(unit, command);
                    if (ContinuousAgent(unit) != null) unit.Override.Direction = Stick;
                }
                var live = LiveContinuous(unit);
                if (live == null) { NativeFaults++; return; }
                live.Accelerated = Stick >= 0.95f;
                if (repath) MakeCompanionsFollow(FormationUnit(unit));
            }

            // Only orders that are approaching (IsMoveUnit); a continuous command is not one of them.
            private void InterruptMove(Unit unit)
            {
                if (unit.Slot != null && !unit.Slot.Finished && !unit.Slot.Continuous) { End(unit.Slot, true); unit.Slot = null; }
                if (unit.OtherSlot != null && !unit.OtherSlot.Finished && !unit.OtherSlot.Free) { End(unit.OtherSlot, true); unit.OtherSlot = null; }
            }

            private void Init(Command command, Unit executor)
            {
                // The mod's prefix of native Init. Both implementations rewrite a mounted rider to its mount; the
                // correction also ends the rider's own moving orders, which native no longer reaches.
                var pair = PairOf(executor);
                if (pair != null && pair.Mounted)
                {
                    if (Corrected) InterruptMove(pair.Rider);
                    if (ReferenceEquals(executor, pair.Rider)) executor = pair.Mount;
                }
                // Native Init: GetComponent, else AddComponent.
                command.Executor = executor;
                if (executor.HasView)
                {
                    if (executor.Component == null) executor.Component = new Agent { Continuous = true };
                    executor.Override = executor.Component;
                }
                // The correction's postfix.
                if (Corrected) PairOf(command.Executor)?.Drive.Bind(command);
            }

            private void Run(Unit container, Command command)
            {
                var admitted = false;
                if (!RunRefused && container.Conscious)
                {
                    if (!ReferenceEquals(command.Executor, container)) RefusedRuns++;
                    else
                    {
                        admitted = true;
                        container.Hold = false;
                        if (command.Free) container.OtherSlot = command;
                        else
                        {
                            // A Move or Standard order ends the unit's own Move-slot command (they are paired).
                            if (container.Slot != null && !container.Slot.Finished) End(container.Slot, true);
                            if (command.Standard) { container.Slot = null; container.OtherSlot = command; }
                            else container.Slot = command;
                        }
                    }
                }
                // The correction's Run postfix.
                if (!Corrected) return;
                if (command.Continuous) { PairOf(command.Executor)?.Drive.ObserveRun(command); return; }
                var pair = PairOf(container);
                if (pair != null && pair.Drive.IsBound) pair.Drive.ObserveRiderOrder(admitted && ReferenceEquals(container, pair.Rider), !command.Free);
            }

            // Native OnEnded: the command is finished and whatever live override its executor's view holds is removed.
            public void End(Command command, bool raiseEvent)
            {
                if (command.Finished) return;
                command.Finished = true;
                command.EndEventRaised = raiseEvent;
                if (command.Continuous && command.Executor != null && Live(command.Executor.Override) != null) SetOverrideNull(command.Executor);
            }

            // The mod's per-frame validation of one pair: settle the drive, then the mounted invariant.
            public void Validate(Pair pair)
            {
                if (Corrected) pair.Drive.Settle(false);
                if (!pair.Mounted) return;
                var mountOverride = Live(pair.Mount.Override);
                var admissible = Corrected
                    ? MountedContinuousDrivePolicy.MountOverrideAdmissible(mountOverride != null, mountOverride != null && pair.Drive.OwnsMountOverride(mountOverride))
                    : mountOverride == null;
                if (admissible) return;
                pair.Fault = "The authoritative mount movement agent changed.";
                Dismount(pair);
            }

            public void Dismount(Pair pair)
            {
                if (Corrected) pair.Drive.Release("dismount");
                pair.Mounted = false;
                if (ReferenceEquals(pair.Rider.Override, pair.RiderAgent)) pair.Rider.Override = null;
            }

            // The mount attempt requires both views to carry no override.
            public bool CanMount(Pair pair) => !pair.Mounted && Live(pair.Rider.Override) == null && Live(pair.Mount.Override) == null;

            public void MountUp(Pair pair)
            {
                if (!CanMount(pair)) throw new InvalidOperationException("The pair cannot mount.");
                if (pair.Rider.Slot != null) { End(pair.Rider.Slot, true); pair.Rider.Slot = null; }
                pair.Mounted = true;
                pair.Generation++;
                pair.Fault = null;
                pair.Rider.Override = pair.RiderAgent;
            }

            private void MakeCompanionsFollow(Unit anchor)
            {
                FormationAnchors.Add(anchor);
                GroundOrder(anchor.Position);
            }

            // An order the rider is given directly and holds itself (an interaction, an ability).
            public Command RiderOrder(bool standard)
            {
                var order = new Command { Executor = Rider, Standard = standard, Free = !standard };
                Run(Rider, order);
                return order;
            }

            // ClickGroundHandler.MoveSelectedUnitsToPoint with the mod's routing of each unit's order.
            public void GroundOrder(float destination, params Unit[] only)
            {
                var units = only.Length != 0 ? only.ToList() : Selected.ToList();
                var skipCurrent = Gamepad && Current != null && StickDriven(Current);
                foreach (var unit in units)
                {
                    if (skipCurrent && ReferenceEquals(unit, Current)) continue;
                    var target = unit;
                    var pair = PairOf(unit);
                    if (pair != null && pair.Mounted)
                    {
                        var route = CommandRouter.RouteGroundMove(new MountedPair(pair.Rider.Name, pair.Mount.Name), unit.Name);
                        if (route.Action == CommandRoutingAction.SuppressDuplicateMount) continue;
                        if (route.Action == CommandRoutingAction.RewriteRiderToMount) target = pair.Mount;
                    }
                    var move = new Command { Executor = target, Destination = destination };
                    Run(target, move);
                }
            }
        }
    }
}
