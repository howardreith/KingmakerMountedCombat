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
    //   UnitCommand.Interrupt stamps the command before it calls OnEnded and does nothing on a stamped command.
    //       UnitMoveContiniously.OnEnded removes the override of the executor's current view.
    //   UnitCommands.Run(cmd) calls the internal Run(cmd, fromQueue, doNotClearQueue), which alone puts a command
    //       into a slot; queue promotion (RemoveFinishedAndUpdateQueue) calls that internal Run directly. A new Move
    //       or Standard order ends the commands in both of those slots; with an uninterruptible command present it is
    //       queued instead. InterruptMove ends only commands that have approached (IsMoveUnit).
    // The model is a reading of the native code, not native evidence. A unit and its view are separate objects here,
    // so a retained earlier view keeps its own override. Corrected = false keeps what the first reviewed implementation
    // (1223eb2f) did: the executor rewrite in the Init prefix alone, and the strict mount invariant.
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
            runner.Run("known issues M05: an in-range Standard order admitted on the rider ends the stick movement; a held stick takes the pair again, ends an approaching order and leaves a stationary one", StationaryRiderOrderAndTheStick);
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
            runner.Run("known issues M05: the retirement plan ends an unfinished command whether or not its agent is installed, and clears the captured agent only while it is installed", RetirementPlan);
            runner.Run("known issues M05 R1: a mount view replaced during stick movement keeps its unrelated override through the dismount and the rider-order release, while the owned command and agent are retired", ReplacedViewKeepsItsForeignOverride);
            runner.Run("known issues M05 R1: a foreign override that replaced the bound agent survives relationship cleanup and the owned command's later ticking and termination", ForeignOverrideSurvivesTheOwnedCommandsWholeLife);
            runner.Run("known issues M05 R1: an interrupt that fails leaves the exact command, view and agent as residue, and the next settlement retries it", InterruptFailureIsRetainedAndRetried);
            runner.Run("known issues M05 R1: a terminal command is not proof of a removed override: the agent is cleared on its own, and the native end callback is not repeated", TerminalCommandIsNotProofOfARemovedOverride);
            runner.Run("known issues M05 R1: an override removal that fails is retained and retried without touching the foreign override", OverrideRemovalFailureIsRetainedAndRetried);
            runner.Run("known issues M05 R1: a persistent retirement failure is retried a bounded number of times, reported once, never discarded, and recovered explicitly", PersistentFailureIsBoundedAndNeverDiscarded);
            runner.Run("known issues M05 R1: a new command that adopts the agent of unfinished residue owns it; the earlier retirement neither clears it nor lets its command's end clear it", ANewBindingTakesOverTheAgentOfUnfinishedResidue);
            runner.Run("known issues M05 R1: an end-event handler that comes back into the owner during the native end does not unblock the clearing of a foreign override, and nothing is retired twice", ReentrantEndEventHandler);
            runner.Run("known issues M05 R1: a new command bound while the earlier record is still bound retires that record instead of overwriting it, and keeps the agent it adopted", NewBindingSupersedesABoundRecord);
            runner.Run("known issues M05 R2: enqueueing a rider order leaves the stick movement; its later admission by queue promotion ends it exactly once", QueuedRiderOrderEndsTheDriveWhenItIsAdmitted);
            runner.Run("known issues M05 R2: a refused promotion, a Free order, an unrelated unit, another pair and a stale order leave the stick movement", RefusedPromotionAndOtherAdmissionsLeaveTheDrive);
            runner.Run("known issues M05 R2: direct admission still ends the stick movement, and nested public and internal Run calls end it once", NestedRunsEndTheDriveOnce);
            runner.Run("known issues M05 lifecycle: the cleanup-completion decisions report retained stick-movement cleanup, tell deliberate releases from per-frame failure paths, and keep a detached owner unsettled", CleanupCompletionDecisions);
            runner.Run("known issues M05 lifecycle A (root and subscriber sequence modelled; owner, policy and registry are production): a disable whose agent removal fails once is reported incomplete and retained; recovery removes the agent without touching the foreign override; the disable completes only then, and the terminal command is not ended again", DisableWithAOneShotAgentRemovalFailure);
            runner.Run("known issues M05 lifecycle B (root sequence modelled; owner and policy are production): runtime-error cleanup with a failed interrupt keeps the command owned and ordinary updates running; the agent is cleaned on its own, the command is retired by the recovery that follows, and the stop is then completed without being asked again", RuntimeErrorCleanupWithAnUnfinishedCommand);
            runner.Run("known issues M05 lifecycle C (root sequence modelled; owner, policy and registry are production): a persistent failure is attempted a bounded number of times and reported once; owner settlement, boundary reuse, guard removal and shutdown stay refused; a later deliberate recovery clears the obligation and every refusal", PersistentFailureAndTeardown);
            runner.Run("known issues M05 lifecycle: a record past the automatic limit gets no further automatic attempt, and still leaves once native has ended the command itself", OverLimitRecordLeavesWhenTheFactsEstablishIt);
            runner.Run("known issues M05 lifecycle D (root sequence modelled; owner, policy and registry are production): a healthy release completes the disable at once, and an unmounted or unrelated pair is unaffected by another pair's retained cleanup", HealthyLifecycleControls);
            runner.Run("known issues M05 (model only): the model follows the pinned native slot rules for InterruptMove, the Move/Standard pairing and an uninterruptible action", TheModelFollowsThePinnedNativeSlotRules);
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
            TestRunner.Equal(0, world.Pair.Cleared, "The owner removed an override native's own end had been allowed to remove.");
            TestRunner.Equal(0, world.TrackedCommands, "An ended command is still tracked for the native-end guard.");
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
            var ownedCommand = world.Mount.Slot;
            world.Stick = 0f;
            world.Validate(world.Pair);
            TestRunner.True(!world.Pair.Mounted, "A foreign override was accepted because a drive was bound.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy && world.Pair.Cleared == 0,
                "The mod cleared an override although the installed override is not the bound agent.");
            TestRunner.True(ownedCommand.Finished && world.Pair.Interrupted == 1 && world.Pair.Drive.ResidueCount == 0,
                "The owned command was not retired with its pair.");
            // Through the command's later life: nothing that follows removes the foreign override.
            for (var i = 0; i < 4; i++) world.Frame();
            world.Interrupt(ownedCommand, true);
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy && !foreign.Destroyed && ownedCommand.EndCallbacks == 1,
                "The foreign override did not survive the owned command's later life.");

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
            // Another unit's command, even while the mount carries a live continuous agent.
            world.Mount.Override = new Agent { Continuous = true };
            pair.Drive.Bind(new Command { Continuous = true, Executor = world.Companion });
            pair.Drive.Bind(new Command { Continuous = true, Executor = world.Rider });
            TestRunner.True(!pair.Drive.IsBound, "A command executing on another unit was bound.");
            // The mount's command, but no continuous agent on the mount's view.
            world.Mount.Override = null;
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
                    var notInstalled = MountedContinuousDrivePolicy.PlanRetirement(settlement, finished, false);
                    TestRunner.True(!notInstalled.ClearCapturedAgent, "An agent that is not installed on its captured view is cleared (" + settlement + ").");
                }
            foreach (var settlement in new[] { MountedContinuousDriveSettlement.None, MountedContinuousDriveSettlement.Keep })
            {
                var kept = MountedContinuousDrivePolicy.PlanRetirement(settlement, false, true);
                TestRunner.True(!kept.ClearCapturedAgent && !kept.InterruptCommand && !kept.RaiseEndEvent, "A kept drive is retired (" + settlement + ").");
            }
            foreach (var settlement in new[] { MountedContinuousDriveSettlement.Unowned, MountedContinuousDriveSettlement.InputGated, MountedContinuousDriveSettlement.Replaced })
                foreach (var installed in new[] { false, true })
                {
                    var ends = MountedContinuousDrivePolicy.PlanRetirement(settlement, false, installed);
                    TestRunner.True(ends.InterruptCommand && ends.RaiseEndEvent && ends.ClearCapturedAgent == installed,
                        "A running drive is not ended natively whether or not its agent is installed (" + settlement + ").");
                    var finished = MountedContinuousDrivePolicy.PlanRetirement(settlement, true, installed);
                    TestRunner.True(!finished.InterruptCommand && !finished.RaiseEndEvent && finished.ClearCapturedAgent == installed, "A finished command is interrupted (" + settlement + ").");
                }
            var orphan = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Orphaned, false, true);
            TestRunner.True(orphan.ClearCapturedAgent && orphan.InterruptCommand && !orphan.RaiseEndEvent, "An orphan is not ended quietly and cleared.");
            var ended = MountedContinuousDrivePolicy.PlanRetirement(MountedContinuousDriveSettlement.Ended, false, true);
            TestRunner.True(ended.ClearCapturedAgent && !ended.InterruptCommand, "An ended command is interrupted again, or its leftover agent kept.");
        }

        // ---- review of 5e27c9bd, R1: the owned command is retired without clearing a foreign override ----

        private static void ReplacedViewKeepsItsForeignOverride()
        {
            // Reproduction A, once for each release that exists: the pair's dismount and a rider order.
            foreach (var byDismount in new[] { true, false })
            {
                var world = new World();
                world.Current = world.Rider;
                world.Stick = 1f;
                world.Frame();
                var command = world.Mount.Slot;
                var boundView = world.Mount.View;
                var agent = boundView.Override;
                TestRunner.True(ReferenceEquals(world.Pair.Drive.View, boundView) && ReferenceEquals(world.Pair.Drive.Agent, agent), "Setup: the drive is bound to the mount's first view and its agent.");
                // The mount receives another view carrying an unrelated override. The first view and its agent stay alive.
                var foreign = new Agent { Continuous = true };
                var replacement = world.ReplaceView(world.Mount);
                replacement.Override = replacement.Component = foreign;
                world.Stick = 0f;
                if (byDismount) world.Dismount(world.Pair);
                else world.RiderOrder(standard: true);
                var how = byDismount ? " (dismount)" : " (rider order)";
                TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy && !foreign.Destroyed,
                    "Retiring the owned command cleared the unrelated override of the mount's current view" + how + ".");
                TestRunner.True(command.Finished && command.EndCallbacks == 1 && command.EndEventRaised, "The owned command was not ended natively, once" + how + ".");
                TestRunner.True(boundView.Override == null && agent.PendingDestroy, "The owned agent was left on the view it was bound on" + how + ".");
                TestRunner.Equal(1, world.Pair.Cleared, "The captured agent was not cleared exactly once, on its own view" + how + ".");
                TestRunner.True(!world.Pair.Drive.IsBound && world.Pair.Drive.ResidueCount == 0, "The retirement is not established" + how + ".");
                TestRunner.Equal(0, world.TrackedCommands, "A retired command is still tracked for the native-end guard" + how + ".");
                // Every later cleanup and tick leaves the foreign override alone.
                for (var i = 0; i < 4; i++) world.Frame();
                world.Pair.Drive.Release("again");
                TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy && !foreign.Destroyed,
                    "A later cleanup or tick cleared the unrelated override" + how + ".");
                TestRunner.Equal(1, world.Pair.Interrupted, "The owned command was interrupted again" + how + ".");
            }
        }

        private static void ForeignOverrideSurvivesTheOwnedCommandsWholeLife()
        {
            // Reproduction B: the mod's cleanup first, then the command's later native life.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var agent = world.Mount.Override;
            var foreign = new Agent { Continuous = false };
            world.Mount.Override = foreign;
            world.Stick = 0f;
            world.Validate(world.Pair);
            TestRunner.True(!world.Pair.Mounted && world.Pair.Fault != null, "A foreign override on the mount was accepted.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy, "Relationship cleanup cleared the foreign override.");
            TestRunner.True(command.Finished && world.Pair.Interrupted == 1, "The owned command was abandoned unfinished: it could later end natively and clear whatever override the mount then holds.");
            TestRunner.Equal(0, world.Pair.Cleared, "The owner cleared an override although the bound agent is not installed.");
            TestRunner.Equal(0, world.Pair.Drive.ResidueCount, "The retirement is not established.");
            TestRunner.True(!world.Pair.Drive.OwnsMountOverride(foreign) && !world.Pair.Drive.OwnsMountOverride(agent), "A foreign or retired agent is owned.");
            for (var i = 0; i < 4; i++) world.Frame();
            world.Interrupt(command, true);
            world.OnEnded(command, true);
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy && !foreign.Destroyed,
                "The original command's later ticking or termination cleared the foreign override.");
            TestRunner.Equal(1, command.EndCallbacks, "The command's end callback ran more than once.");

            // The other order: native ends the command (its own agent is gone) before the mod's cleanup runs.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            foreign = new Agent { Continuous = false };
            world.Mount.Override = foreign;
            world.Tick();
            TestRunner.True(command.Finished, "Setup: native ends a continuous command whose agent is gone.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy, "The owned command's own native end cleared the foreign override.");
            world.Validate(world.Pair);
            for (var i = 0; i < 3; i++) world.Frame();
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy, "Cleanup after the native end cleared the foreign override.");
            TestRunner.Equal(0, world.Pair.Cleared + world.Pair.Interrupted, "The owner acted although the command had ended and its agent was not installed.");

            // A foreign continuous agent keeps the command alive natively; the dismount still retires the command only.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            foreign = new Agent { Continuous = true, Direction = 1f };
            world.Mount.Override = foreign;
            world.Dismount(world.Pair);
            TestRunner.True(command.Finished && ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy, "The dismount cleared a foreign continuous agent, or left the owned command running.");

            TestRunner.True(MountedContinuousDrivePolicy.NativeEndMayClearOverride(false, false, false) && MountedContinuousDrivePolicy.NativeEndMayClearOverride(true, true, true) &&
                !MountedContinuousDrivePolicy.NativeEndMayClearOverride(true, false, true) && !MountedContinuousDrivePolicy.NativeEndMayClearOverride(true, true, false),
                "A pair-bound command may clear anything but its bound agent on its bound view, or an unbound command lost the native rule.");
        }

        private static void InterruptFailureIsRetainedAndRetried()
        {
            // The interrupt fails before the native call: the command is still running and still owns its agent's place.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var agent = world.Mount.Override;
            world.Stick = 0f;
            world.Pair.FailInterrupts = 1;
            world.Dismount(world.Pair);
            TestRunner.True(!command.Finished && ReferenceEquals(world.Mount.Slot, command), "Setup: the failed interrupt left the command running in the mount's slot.");
            TestRunner.True(world.Mount.Override == null && agent.PendingDestroy, "The captured agent was not cleaned on its own when the command's interrupt failed.");
            TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "The only cleanup record was discarded although its command is unfinished and still held.");
            TestRunner.True(!world.Pair.Drive.IsBound && !world.Pair.Drive.OwnsMountOverride(agent), "A retirement that failed is still an owned live drive.");
            TestRunner.Equal(1L, world.Pair.Drive.RetirementFailureCount, "The failure was not counted.");
            TestRunner.Equal(1, world.Pair.Warnings, "The failure was not reported.");
            // The next settlement retries with the same command.
            world.Validate(world.Pair);
            TestRunner.True(command.Finished && world.Mount.Override == null, "The retry did not retire the command.");
            TestRunner.Equal(0, world.Pair.Drive.ResidueCount, "The retirement is not established after the retry.");
            TestRunner.Equal(2, world.Pair.Interrupted, "The interrupt was not attempted exactly twice.");
            TestRunner.True(world.CanMount(world.Pair), "The pair cannot mount again after the retry.");

            // The same failure after a foreign override replaced the bound agent: nothing owned is installed, but the
            // unfinished command is still held by the mount, so its record stays. When native ends that command in the
            // meantime, the record is what keeps its end from clearing the foreign override.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            var foreign = new Agent { Continuous = false };
            world.Mount.Override = foreign;
            world.Stick = 0f;
            world.Pair.FailInterrupts = 1;
            world.Dismount(world.Pair);
            TestRunner.True(!command.Finished && world.Pair.Drive.ResidueCount == 1 && world.Pair.Cleared == 0, "An unfinished, still held command lost its record, or something was cleared.");
            world.Tick();
            TestRunner.True(command.Finished, "Setup: native ended the command whose own agent is gone.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, foreign) && !foreign.PendingDestroy, "The retained command's native end cleared the foreign override.");
            world.Validate(world.Pair);
            TestRunner.True(world.Pair.Drive.ResidueCount == 0 && world.Pair.Interrupted == 1 && world.Pair.Cleared == 0 && ReferenceEquals(world.Mount.Override, foreign),
                "The retirement was not established from the facts, or the retry touched the foreign override.");
        }

        private static void TerminalCommandIsNotProofOfARemovedOverride()
        {
            // Native Interrupt stamps the command, OnEnded marks it finished and then fails before removing the override.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var agent = world.Mount.Override;
            world.Stick = 0f;
            world.FailNextEndCallback = true;
            world.Dismount(world.Pair);
            TestRunner.True(command.Finished && command.InterruptStamped && command.EndCallbacks == 1, "Setup: the command is terminal and its end callback failed.");
            // The agent is not assumed gone because the command is terminal: the captured view is read and cleaned.
            TestRunner.Equal(1, world.Pair.Cleared, "The captured agent was not cleared on its own after the failed end callback.");
            TestRunner.True(world.Mount.Override == null && agent.PendingDestroy && world.Pair.Drive.ResidueCount == 0, "The owned override survived a terminal command.");
            TestRunner.Equal(1L, world.Pair.Drive.RetirementFailureCount, "The failed end callback was not counted.");
            TestRunner.True(world.CanMount(world.Pair), "The pair cannot mount again.");

            // The same, and the removal fails too: the command is terminal, its override is still installed, and the
            // record stays. Native Interrupt would do nothing on the stamped command, so the retry clears the agent.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            agent = world.Mount.Override;
            world.Stick = 0f;
            world.FailNextEndCallback = true;
            world.Pair.FailClears = 1;
            world.Dismount(world.Pair);
            TestRunner.True(command.Finished && ReferenceEquals(world.Mount.Override, agent) && !agent.PendingDestroy, "Setup: a terminal command whose override was not removed.");
            TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "A terminal command was taken as proof that its override is gone.");
            TestRunner.True(!world.CanMount(world.Pair), "Setup: the leftover override refuses a mount attempt.");
            world.Validate(world.Pair);
            TestRunner.Equal(1, world.Pair.Interrupted, "A terminal command was interrupted again.");
            TestRunner.Equal(1, command.EndCallbacks, "The native end callback was repeated.");
            TestRunner.Equal(2, world.Pair.Cleared, "The removal was not retried exactly once.");
            TestRunner.True(world.Mount.Override == null && agent.PendingDestroy && world.Pair.Drive.ResidueCount == 0, "The owned override survived the retry.");
            TestRunner.True(world.CanMount(world.Pair), "The pair cannot mount again.");
        }

        private static void OverrideRemovalFailureIsRetainedAndRetried()
        {
            // The view was replaced, so the command's native end cannot remove the agent; the removal itself then fails.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var boundView = world.Mount.View;
            var agent = boundView.Override;
            var foreign = new Agent { Continuous = true };
            var replacement = world.ReplaceView(world.Mount);
            replacement.Override = replacement.Component = foreign;
            world.Stick = 0f;
            world.Pair.FailClears = 1;
            world.Dismount(world.Pair);
            TestRunner.True(command.Finished, "Setup: the command was retired.");
            TestRunner.True(ReferenceEquals(boundView.Override, agent) && world.Pair.Drive.ResidueCount == 1, "The record of an agent that could not be removed was discarded.");
            TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy, "The failed removal touched the foreign override.");
            world.Validate(world.Pair);
            TestRunner.True(boundView.Override == null && agent.PendingDestroy && world.Pair.Drive.ResidueCount == 0, "The retry did not remove the captured agent.");
            TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy, "The retry touched the foreign override.");
            TestRunner.Equal(1, world.Pair.Interrupted, "A finished command was interrupted again on retry.");
            TestRunner.Equal(2, world.Pair.Cleared, "The removal was not attempted exactly twice.");
        }

        private static void PersistentFailureIsBoundedAndNeverDiscarded()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var agent = world.Mount.Override;
            world.Stick = 0f;
            world.Pair.FailInterrupts = 100;
            world.Pair.FailReports = true;
            world.Dismount(world.Pair);
            TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "A failing report undid the retention of the record.");
            TestRunner.True(world.Pair.Drive.ReportFailureCount >= 1, "The failing report was not contained and counted.");
            // Automatic retries stop at the limit; the record stays.
            for (var i = 0; i < 10; i++) world.Pair.Drive.Settle(false);
            TestRunner.Equal(MountedContinuousDriveOwner.AutomaticRetryLimit, world.Pair.Interrupted, "Automatic retries of a failing retirement are not bounded.");
            TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "The record was discarded after its automatic retries.");
            TestRunner.Equal(1, world.Pair.Warnings, "A persistent failure was reported more than once.");
            TestRunner.True(!command.Finished && world.Mount.Override == null && agent.PendingDestroy, "Setup: the command is still unfinished; its agent was cleaned on its own.");
            // A deliberate release retries beyond the automatic limit, and succeeds once the failure is gone.
            world.Pair.FailInterrupts = 1;
            world.Pair.Drive.Release("explicit recovery");
            TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "Explicit recovery did not retry and keep the record.");
            world.Pair.Drive.Release("explicit recovery");
            TestRunner.True(command.Finished && world.Mount.Override == null && world.Pair.Drive.ResidueCount == 0, "Explicit recovery did not establish the retirement.");

            TestRunner.True(MountedContinuousDrivePolicy.RetirementEstablished(true, true, false) && MountedContinuousDrivePolicy.RetirementEstablished(false, false, false) &&
                !MountedContinuousDrivePolicy.RetirementEstablished(true, true, true) && !MountedContinuousDrivePolicy.RetirementEstablished(true, false, true) &&
                !MountedContinuousDrivePolicy.RetirementEstablished(false, true, false),
                "A retirement is established although its agent is installed, or although its unfinished command is still held.");
        }

        private static void ANewBindingTakesOverTheAgentOfUnfinishedResidue()
        {
            // A refused command whose interrupt and agent removal both fail stays as residue with its agent installed.
            // The next push adopts that same component: it is the new command's agent now, and the residue no longer
            // clears it.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.RunRefused = true;
            world.Pair.FailInterrupts = 1;
            world.Pair.FailClears = 1;
            world.Frame();
            var agent = world.Mount.Override;
            TestRunner.True(agent != null && world.Pair.Drive.ResidueCount == 1, "Setup: the refused command's retirement failed and is retained.");
            world.RunRefused = false;
            world.Pair.FailInterrupts = 0;
            world.Frame(validate: false);
            var second = world.Mount.Slot;
            TestRunner.True(second != null && second.Continuous && ReferenceEquals(world.Pair.Drive.Command, second) && ReferenceEquals(world.Pair.Drive.Agent, agent),
                "Setup: the next push adopted the same agent.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, agent) && !agent.PendingDestroy, "Recovering the earlier retirement cleared the agent the new command adopted.");
            TestRunner.Equal(0, world.Pair.Drive.ResidueCount, "The earlier retirement is not established (its command is held by nobody and its agent has a new owner).");
            world.Frame();
            world.Frame();
            TestRunner.True(world.Pair.Mounted && world.Mount.Position > 0f && ReferenceEquals(world.Mount.Override, agent), "The pair is not driven by the adopted agent: " + world.Pair.Fault);
        }

        private static void ReentrantEndEventHandler()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var boundView = world.Mount.View;
            var foreign = new Agent { Continuous = true };
            var replacement = world.ReplaceView(world.Mount);
            replacement.Override = replacement.Component = foreign;
            world.Stick = 0f;
            // Native raises the end event inside OnEnded, before it removes the override.
            var reentered = 0;
            world.EndEventHandler = ended =>
            {
                reentered++;
                world.Pair.Drive.Settle(false);
                world.Pair.Drive.Release("re-entered from the end event");
            };
            world.Dismount(world.Pair);
            TestRunner.Equal(1, reentered, "Setup: the end event handler ran once.");
            TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy, "A re-entrant settlement let the native end clear the foreign override.");
            TestRunner.True(command.Finished && command.EndCallbacks == 1 && world.Pair.Interrupted == 1, "The command was not retired exactly once.");
            TestRunner.True(boundView.Override == null && world.Pair.Cleared == 1, "The captured agent was not cleared exactly once.");
            TestRunner.True(world.Pair.Drive.ResidueCount == 0 && world.TrackedCommands == 0, "The retirement is not established, or the command is still tracked.");
        }

        private static void NewBindingSupersedesABoundRecord()
        {
            // The first command is replaced on the mount and, before the mod's next settlement, the held stick pushes
            // again: the record is still bound to the first command when the second is bound.
            var world = new World { Gamepad = false };
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var first = world.Mount.Slot;
            var agent = world.Mount.Override;
            world.GroundOrder(30f, world.Rider);
            TestRunner.True(first.Finished && ReferenceEquals(world.Pair.Drive.Command, first), "Setup: the first command ended and its record is not yet settled.");
            world.Frame(validate: false);
            var second = world.Mount.Slot;
            TestRunner.True(second != null && second.Continuous && ReferenceEquals(world.Pair.Drive.Command, second) && ReferenceEquals(world.Pair.Drive.Agent, agent),
                "The second command was not bound with the agent it adopted.");
            TestRunner.Equal(0, world.Pair.Cleared + world.Pair.Interrupted, "Superseding a finished record cleared the adopted agent or interrupted something.");
            TestRunner.Equal(0, world.Pair.Drive.ResidueCount, "The superseded record was not established.");
            TestRunner.Equal(1, world.TrackedCommands, "The superseded command is still tracked, or the new one is not.");

            // The same when the earlier command is, against the native order of things, still running: it is ended, and
            // its native end may not clear the agent the new command adopted.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var running = world.Mount.Slot;
            var liveAgent = world.Mount.Override;
            var pair = world.Pair;
            var third = new Command { Continuous = true, Executor = world.Mount };
            pair.Drive.Bind(third);
            TestRunner.True(running.Finished && running.EndEventRaised && pair.Interrupted == 1, "A still running earlier command was overwritten, not retired.");
            TestRunner.True(ReferenceEquals(pair.Drive.Command, third) && ReferenceEquals(pair.Drive.Agent, liveAgent), "The new command was not bound with the agent it adopted.");
            TestRunner.True(ReferenceEquals(world.Mount.Override, liveAgent) && !liveAgent.PendingDestroy && pair.Cleared == 0, "Retiring the earlier command cleared the agent the new command adopted.");
            TestRunner.Equal(0, pair.Drive.ResidueCount, "The superseded record was not established.");
        }

        // ---- review of 5e27c9bd, R2: rider-order admission through queue promotion ----

        private static void QueuedRiderOrderEndsTheDriveWhenItIsAdmitted()
        {
            var world = new World();
            // The rider is busy with an action that cannot be interrupted, so native Run queues a new order.
            var busy = world.Order(world.Rider, standard: true, uninterruptible: true);
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            TestRunner.True(command != null && command.Continuous && !busy.Finished, "Setup: the pair is driven while the rider's own action runs.");
            // Merely enqueueing leaves the drive intact.
            var queued = world.RiderOrder(standard: true);
            TestRunner.True(world.Rider.Queue.Contains(queued) && !world.Rider.Holds(queued), "Setup: the order was queued, not admitted.");
            world.Frame();
            TestRunner.True(!command.Finished && world.Pair.Drive.IsBound && world.Pair.Interrupted == 0, "Enqueueing a rider order ended the stick movement.");
            // The busy action finishes; native promotes the queued order through the internal Run.
            busy.Finished = true;
            world.Stick = 0f;
            world.Tick();
            TestRunner.True(world.Rider.Holds(queued), "Setup: the queued order was promoted and admitted.");
            TestRunner.True(command.Finished && !world.Pair.Drive.IsBound, "An order admitted by queue promotion did not end the stick movement.");
            TestRunner.Equal(1, world.Pair.Interrupted, "The drive was not ended exactly once.");
            TestRunner.Equal(1, command.EndCallbacks, "The native end callback did not run exactly once.");
            TestRunner.Equal(1, world.Pair.EndEvents, "The native end event was not raised exactly once.");
            TestRunner.True(!queued.Finished, "The admitted order was ended.");
            for (var i = 0; i < 3; i++) world.Frame();
            TestRunner.Equal(1, world.Pair.Interrupted, "A later frame ended the drive again.");

            // The wiring of the reviewed head observed admission after the public Run only: promotion bypasses it.
            var reviewed = new World { ObserveAdmissionOnPublicRunOnly = true };
            busy = reviewed.Order(reviewed.Rider, standard: true, uninterruptible: true);
            reviewed.Current = reviewed.Rider;
            reviewed.Stick = 1f;
            reviewed.Frame();
            command = reviewed.Mount.Slot;
            queued = reviewed.RiderOrder(standard: true);
            busy.Finished = true;
            reviewed.Tick();
            TestRunner.True(reviewed.Rider.Holds(queued) && !command.Finished && reviewed.Pair.Drive.IsBound,
                "Setup (model of the reviewed wiring): a promoted order should not have been observed there.");
        }

        private static void RefusedPromotionAndOtherAdmissionsLeaveTheDrive()
        {
            // A promotion the mod's own prefix refuses, and one native refuses for an unconscious unit.
            foreach (var unconscious in new[] { false, true })
            {
                var world = new World();
                var busy = world.Order(world.Rider, standard: true, uninterruptible: true);
                world.Current = world.Rider;
                world.Stick = 1f;
                world.Frame();
                var command = world.Mount.Slot;
                var queued = world.RiderOrder(standard: true);
                busy.Finished = true;
                if (unconscious) world.Rider.Conscious = false;
                else world.PromotionRefused = true;
                world.Tick();
                TestRunner.True(!world.Rider.Holds(queued), "Setup: the promotion was refused.");
                TestRunner.True(!command.Finished && world.Pair.Drive.IsBound && world.Pair.Interrupted == 0, "A refused promotion ended the stick movement (unconscious=" + unconscious + ").");
            }

            // A queued Free order, another unit's promoted order and another pair's rider order change nothing.
            var other = new World();
            var second = other.AddPair("rider2", "mount2");
            var riderBusy = other.Order(other.Rider, standard: false, uninterruptible: true);
            var companionBusy = other.Order(other.Companion, standard: true, uninterruptible: true);
            var secondBusy = other.Order(second.Rider, standard: true, uninterruptible: true);
            other.Current = other.Rider;
            other.Stick = 1f;
            other.Frame();
            var drive = other.Mount.Slot;
            var free = other.Order(other.Rider, standard: false);
            var companionOrder = other.Order(other.Companion, standard: true);
            var secondOrder = other.Order(second.Rider, standard: true);
            riderBusy.Finished = companionBusy.Finished = secondBusy.Finished = true;
            other.Tick();
            TestRunner.True(other.Rider.Holds(free) && other.Companion.Holds(companionOrder) && second.Rider.Holds(secondOrder), "Setup: the three queued orders were promoted.");
            TestRunner.True(!drive.Finished && other.Pair.Drive.IsBound && other.Pair.Interrupted == 0 && second.Interrupted == 0,
                "A Free order, an unrelated unit's order or another pair's order ended the stick movement.");

            // A stale order: admitted when no drive is bound, nothing happens, then or later.
            var stale = new World();
            stale.RiderOrder(standard: true);
            stale.Tick();
            TestRunner.Equal(0, stale.Pair.Interrupted + stale.Pair.Cleared, "An order admitted without a bound drive retired something.");
        }

        private static void NestedRunsEndTheDriveOnce()
        {
            // Direct admission through the public Run reaches the internal Run once.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            world.Stick = 0f;
            var direct = world.RiderOrder(standard: true);
            TestRunner.True(world.Rider.Holds(direct) && command.Finished, "Direct admission no longer ends the stick movement.");
            TestRunner.True(world.Pair.Interrupted == 1 && command.EndCallbacks == 1 && world.Pair.EndEvents == 1, "Direct admission ended the drive more than once.");

            // A custom handler: the public Run of one order runs another first, nested, and queues the first. Both
            // internal Runs are observed; the drive ends once, when the nested order is admitted.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            world.Stick = 0f;
            var first = new Command { Executor = world.Rider, Standard = true };
            var outer = world.Order(world.Rider, standard: true, executeBefore: first);
            TestRunner.True(world.Rider.Holds(first) && world.Rider.Queue.Contains(outer), "Setup: the nested order was admitted and the outer one queued.");
            TestRunner.True(command.Finished && world.Pair.Interrupted == 1 && command.EndCallbacks == 1 && world.Pair.EndEvents == 1,
                "Nested Run calls ended the drive more than once, or not at all.");
            first.Finished = true;
            world.Tick();
            TestRunner.True(world.Rider.Holds(outer), "Setup: the outer order was promoted afterwards.");
            TestRunner.True(world.Pair.Interrupted == 1 && command.EndCallbacks == 1, "The later promotion ended an already ended drive again.");
        }

        private static void StationaryRiderOrderAndTheStick()
        {
            // An in-range Standard order admitted on the rider ends the drive. It needs no approach, so nothing but its
            // own admission can be what ended it.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            var order = world.RiderOrder(standard: true);
            TestRunner.True(!order.Approaching && world.Rider.Holds(order), "Setup: a stationary, admitted Standard order.");
            TestRunner.True(command.Finished && command.EndEventRaised && world.Mount.Override == null && !world.Pair.Drive.IsBound, "The rider's in-range order did not end the pair's stick movement.");
            // The stick is still held: it takes the mount again. Native InterruptMove ends approaching orders only, and
            // the Move/Standard pairing of native Run lives in the mount's container, so the rider's stationary order
            // runs on, as it does beside a routed ground order.
            world.Frame();
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && !ReferenceEquals(world.Mount.Slot, command), "The held stick did not take the pair again.");
            TestRunner.True(!order.Finished && world.Rider.Holds(order), "The stick ended the rider's stationary order.");
            TestRunner.True(world.Pair.Mounted, "The pair was dismounted: " + world.Pair.Fault);

            // An approaching order is different: a held stick ends it when it takes the pair over.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var approaching = world.RiderOrder(standard: true, approaching: true);
            world.Frame();
            TestRunner.True(approaching.Finished, "A held stick did not end the rider's approaching order.");
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && world.Pair.Mounted, "The held stick did not take the pair again.");

            // Released before the order: the order stays and the drive is gone.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Stick = 0f;
            approaching = world.RiderOrder(standard: true, approaching: true);
            world.Frame(); world.Frame();
            TestRunner.True(!approaching.Finished && world.Mount.Slot == null && world.Mount.Override == null, "A released stick cancelled the rider's order, or the drive lingered.");

            // A Free order replaces nothing, natively or here.
            world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            command = world.Mount.Slot;
            world.RiderOrder(standard: false);
            TestRunner.True(!command.Finished && world.Pair.Drive.IsBound, "An order that is neither Move nor Standard ended the stick movement.");

            // The stick starts while the rider already holds a stationary Standard order: the pair is driven and the
            // order runs on. (For one native unit the same push would end an interruptible Standard order through the
            // Move/Standard pairing; the pair keeps its established rule that the mount's movement does not.)
            world = new World();
            order = world.RiderOrder(standard: true);
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Frame();
            TestRunner.True(world.Mount.Slot != null && world.Mount.Slot.Continuous && world.Mount.Position > 0f && !order.Finished && world.Rider.Holds(order),
                "Starting stick movement ended the rider's stationary order, or did not drive the pair.");

            TestRunner.True(MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, true, true) && !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(false, true, true) &&
                !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, false, true) && !MountedContinuousDrivePolicy.RiderOrderReplacesDrive(true, true, false),
                "The replacement decision is not a bound drive and a Move or Standard order admitted on the rider's container.");
        }

        private static void TheModelFollowsThePinnedNativeSlotRules()
        {
            // These assert the model, not production: the native rules are pinned by the contract script, and the tests
            // above rely on the model following them.
            var world = new World();
            var stationary = world.Order(world.Companion, standard: true);
            var approaching = world.Order(world.Companion, standard: false, approaching: true);
            world.Current = world.Companion;
            world.Stick = 1f;
            world.Frame();
            TestRunner.True(approaching.Finished, "Model: InterruptMove did not end an approaching order.");
            TestRunner.True(stationary.Finished && world.Companion.Slot != null && world.Companion.Slot.Continuous,
                "Model: a Move order run on the same unit did not end its interruptible Standard order (the Move/Standard pairing).");
            // An uninterruptible action: native queues the continuous command and faults on the missing command.
            world = new World();
            var busy = world.Order(world.Companion, standard: true, uninterruptible: true);
            world.Current = world.Companion;
            world.Stick = 1f;
            world.Frame();
            TestRunner.True(!busy.Finished && world.Companion.Slot == null && world.Companion.Queue.Count == 1 && world.NativeFaults == 1,
                "Model: an uninterruptible action did not queue the continuous command.");
        }

        // ---- review of 25cbdbe2: retained cleanup and the lifecycle around the owner ----
        // The owner, its record and every decision named MountedCleanupCompletionPolicy are production code. The dismount
        // that composes the result, the lifecycle subscriber's per-pair cleanup, and the composition root's disable,
        // runtime-error cleanup and update loop are modelled (World.DismountResult, RequestDisable, RuntimeErrorCleanup,
        // RootUpdate); the real GameMountedRelationshipService.Dismount is executed by the contract script.

        private static void CleanupCompletionDecisions()
        {
            var clean = new TransitionResult(true, RelationshipState.Unmounted, CleanupTrigger.ModDisabled, new string[0], false, false);
            TestRunner.True(ReferenceEquals(MountedCleanupCompletionPolicy.WithRetainedStickMovement(clean, 0, null), clean), "A result without retained cleanup was changed.");
            var retained = MountedCleanupCompletionPolicy.WithRetainedStickMovement(clean, 2, "InvalidOperationException: x");
            TestRunner.True(!retained.Succeeded && retained.MovementAuthorityResidual && !retained.PresentationResidual, "Retained stick-movement cleanup is not reported as failed movement-authority residue.");
            TestRunner.True(retained.State == RelationshipState.Unmounted && retained.Trigger == CleanupTrigger.ModDisabled, "The coordinator's state or trigger was lost.");
            TestRunner.True(retained.Errors.Count == 1 && retained.Errors[0].Contains("2 retained obligation(s)") && retained.Errors[0].Contains("InvalidOperationException: x"),
                "The result does not say what is retained and why.");
            var withPresentation = MountedCleanupCompletionPolicy.WithRetainedStickMovement(
                new TransitionResult(false, RelationshipState.Faulted, CleanupTrigger.Exception, new[] { "earlier" }, false, true), 1, null);
            TestRunner.True(withPresentation.PresentationResidual && withPresentation.Errors.Count == 2 && withPresentation.Errors[0] == "earlier" && withPresentation.State == RelationshipState.Faulted,
                "An earlier failure of the same cleanup was not kept.");

            TestRunner.True(MountedCleanupCompletionPolicy.IsComplete(clean, RelationshipState.Unmounted) && MountedCleanupCompletionPolicy.IsWithoutResidue(clean), "A clean result is not complete.");
            TestRunner.True(!MountedCleanupCompletionPolicy.IsComplete(retained, RelationshipState.Unmounted) && !MountedCleanupCompletionPolicy.IsWithoutResidue(retained), "Retained cleanup counts as complete.");
            TestRunner.True(!MountedCleanupCompletionPolicy.IsComplete(clean, RelationshipState.Mounted) && !MountedCleanupCompletionPolicy.IsComplete(null, RelationshipState.Unmounted), "A mounted relationship or a missing result counts as complete.");
            TestRunner.True(!MountedCleanupCompletionPolicy.IsWithoutResidue(new TransitionResult(true, RelationshipState.Unmounted, CleanupTrigger.Exception, new string[0], true, false)) &&
                !MountedCleanupCompletionPolicy.IsWithoutResidue(new TransitionResult(true, RelationshipState.Unmounted, CleanupTrigger.Exception, new string[0], false, true)) &&
                !MountedCleanupCompletionPolicy.IsWithoutResidue(new TransitionResult(false, RelationshipState.Unmounted, CleanupTrigger.Exception, new string[0], false, false)),
                "A failed result, or one with movement or presentation residue, reads as without residue.");

            TestRunner.True(MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(CleanupTrigger.ModDisabled, false) && MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(CleanupTrigger.AreaUnloading, false) &&
                MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(CleanupTrigger.Manual, false), "A lifecycle boundary does not retry retained cleanup deliberately.");
            TestRunner.True(!MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(CleanupTrigger.Exception, false) && !MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(CleanupTrigger.ModDisabled, true),
                "A failure path that can run every frame retries retained cleanup beyond the automatic limit.");

            TestRunner.True(!MountedCleanupCompletionPolicy.OwnerRetainsCleanup(false, false, false) && MountedCleanupCompletionPolicy.OwnerRetainsCleanup(false, false, true) &&
                MountedCleanupCompletionPolicy.OwnerRetainsCleanup(true, false, false) && MountedCleanupCompletionPolicy.OwnerRetainsCleanup(false, true, false),
                "A detached owner with retained cleanup is settled, or a clean one is not.");
        }

        private static void DisableWithAOneShotAgentRemovalFailure()
        {
            foreach (var reportsRetainedCleanup in new[] { true, false })
            {
                var world = new World { LifecycleReportsRetainedCleanup = reportsRetainedCleanup };
                world.Current = world.Rider;
                world.Stick = 1f;
                world.Frame();
                var command = world.Mount.Slot;
                var boundView = world.Mount.View;
                var agent = boundView.Override;
                var foreign = new Agent { Continuous = true };
                var replacement = world.ReplaceView(world.Mount);
                replacement.Override = replacement.Component = foreign;
                world.Stick = 0f;
                world.Pair.FailClears = 1;
                var disabled = world.RequestDisable();
                TestRunner.True(command.Finished && ReferenceEquals(boundView.Override, agent) && world.Pair.Drive.ResidueCount == 1, "Setup: the command ended and its agent could not be removed.");
                TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy, "The failed removal touched the foreign override.");
                if (!reportsRetainedCleanup)
                {
                    // The wiring of the reviewed head 25cbdbe2 (a model of it): the dismount returned the coordinator's
                    // clean result, the disable completed, ordinary updates stopped, and nothing ever retried.
                    TestRunner.True(disabled && !world.ServicesEnabled, "Setup (model of the reviewed wiring): the disable should have completed over the retained obligation.");
                    for (var i = 0; i < 6; i++) world.RootUpdate();
                    TestRunner.True(ReferenceEquals(boundView.Override, agent) && world.Pair.Drive.ResidueCount == 1, "Setup (model of the reviewed wiring): nothing should have recovered the agent.");
                    continue;
                }
                TestRunner.True(!disabled && world.ServicesEnabled, "The disable completed although an owned agent is still installed.");
                TestRunner.True(!MountedCleanupCompletionPolicy.IsComplete(world.LastCleanup, RelationshipState.Unmounted) && world.LastCleanup.MovementAuthorityResidual &&
                    world.LastCleanup.Errors.Any(error => error.Contains("retained obligation")), "The cleanup result does not report the retained obligation.");
                TestRunner.True(!world.OwnerSettled(world.Pair), "The pair's owner counts as settled while its cleanup is retained.");
                // The ordinary pair update is held back (a save or an area change), so only the retry that precedes
                // the root's gates can recover.
                world.OrdinaryUpdateHeld = true;
                world.RootUpdate();
                world.OrdinaryUpdateHeld = false;
                TestRunner.True(boundView.Override == null && agent.PendingDestroy && world.Pair.Drive.ResidueCount == 0, "The recovery path did not remove the captured agent.");
                TestRunner.True(ReferenceEquals(replacement.Override, foreign) && !foreign.PendingDestroy, "The recovery touched the foreign override.");
                TestRunner.True(world.RequestDisable() && !world.ServicesEnabled, "The disable did not complete once the cleanup was established.");
                TestRunner.True(world.Pair.Interrupted == 1 && command.EndCallbacks == 1, "The terminal command was ended again.");
                TestRunner.Equal(2, world.Pair.Cleared, "The removal was not attempted exactly twice.");
                TestRunner.True(world.OwnerSettled(world.Pair), "The owner is not settled after the cleanup was established.");
            }
        }

        private static void RuntimeErrorCleanupWithAnUnfinishedCommand()
        {
            foreach (var reportsRetainedCleanup in new[] { true, false })
            {
                var world = new World { LifecycleReportsRetainedCleanup = reportsRetainedCleanup };
                world.Current = world.Rider;
                world.Stick = 1f;
                world.Frame();
                var command = world.Mount.Slot;
                var agent = world.Mount.Override;
                world.Stick = 0f;
                world.Current = null;
                world.Pair.FailInterrupts = 1;
                var stopped = world.RuntimeErrorCleanup();
                TestRunner.True(!command.Finished && ReferenceEquals(world.Mount.Slot, command), "Setup: the interrupt failed and the command is still held.");
                TestRunner.True(world.Mount.Override == null && agent.PendingDestroy, "The agent was not cleaned on its own when the interrupt failed.");
                TestRunner.Equal(1, world.Pair.Drive.ResidueCount, "The unfinished command is no longer owned for cleanup.");
                if (!reportsRetainedCleanup)
                {
                    TestRunner.True(stopped && !world.ServicesEnabled, "Setup (model of the reviewed wiring): the runtime-error cleanup should have stopped ordinary updates.");
                    world.TurnBasedHold = true;
                    for (var i = 0; i < 6; i++) world.RootUpdate();
                    TestRunner.True(!command.Finished && world.Pair.Drive.ResidueCount == 1 && world.Pair.Interrupted == 1,
                        "Setup (model of the reviewed wiring): nothing should have retried the command once updates stopped.");
                    continue;
                }
                TestRunner.True(!stopped && world.ServicesEnabled, "A successful-looking transition stopped ordinary updates over an unfinished owned command.");
                TestRunner.True(!MountedCleanupCompletionPolicy.IsWithoutResidue(world.LastCleanup), "The runtime-error cleanup result reads as complete.");
                // Native would not tick the command by itself here (turn-based hold); only the mod's recovery can retire it.
                world.TurnBasedHold = true;
                world.RootUpdate();
                TestRunner.True(command.Finished && world.Pair.Interrupted == 2 && world.Pair.Drive.ResidueCount == 0, "The retained command was not retired by the recovery that followed.");
                // The stop was held back by the stick-movement cleanup alone: the root takes it again by itself.
                TestRunner.True(!world.ServicesEnabled && !world.FailureStopPending, "The runtime-error stop was abandoned after its cleanup was established.");
                TestRunner.Equal(2, world.Pair.Interrupted, "The retired command was interrupted again.");
            }
        }

        private static void PersistentFailureAndTeardown()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            world.Stick = 0f;
            world.Current = null;
            world.TurnBasedHold = true;
            world.Pair.FailInterrupts = 1000;
            TestRunner.True(!world.RequestDisable() && world.ServicesEnabled, "The disable completed over a command that could not be retired.");
            // Ordinary frames, and a failure path that would run every frame: attempts stay within the automatic limit.
            for (var i = 0; i < 12; i++) world.RootUpdate();
            for (var i = 0; i < 6; i++) TestRunner.True(!world.RuntimeErrorCleanup(), "Runtime-error cleanup completed over the retained command.");
            TestRunner.Equal(MountedContinuousDriveOwner.AutomaticRetryLimit, world.Pair.Interrupted, "Per-frame recovery and per-frame failure cleanup are not bounded together.");
            TestRunner.True(world.FailureStopPending && world.ServicesEnabled, "The runtime-error stop is not waiting for the retained cleanup.");
            TestRunner.Equal(1, world.Pair.Warnings, "The persistent failure was reported more than once.");
            TestRunner.True(!command.Finished && world.Pair.Drive.ResidueCount == 1, "The obligation was discarded.");
            // Everything that would treat the pair as settled stays refused.
            TestRunner.True(!world.OwnerSettled(world.Pair), "The owner was settled, or its boundary offered for reuse, over retained cleanup.");
            TestRunner.True(!world.MayRemoveGuards(), "The Harmony guards could be removed while an owned command can still end natively.");
            TestRunner.True(!world.Shutdown(world.Pair), "Shutdown completed over retained cleanup.");
            TestRunner.True(world.ServicesEnabled && world.Pair.Drive.ResidueCount == 1, "A refused teardown changed the retained state.");
            // The failure goes away; a deliberate release recovers, and every refusal clears.
            world.Pair.FailInterrupts = 0;
            TestRunner.True(world.RequestDisable() && !world.ServicesEnabled, "A later deliberate recovery did not complete the disable.");
            TestRunner.True(command.Finished && world.Pair.Drive.ResidueCount == 0, "The obligation did not clear.");
            TestRunner.True(world.OwnerSettled(world.Pair) && world.MayRemoveGuards() && world.Shutdown(world.Pair), "A refusal stayed latched after the cleanup was established.");
        }

        private static void OverLimitRecordLeavesWhenTheFactsEstablishIt()
        {
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            world.Stick = 0f;
            world.Current = null;
            world.TurnBasedHold = true;
            world.Pair.FailInterrupts = 1000;
            TestRunner.True(!world.RequestDisable(), "Setup: the disable is refused over the command that could not be retired.");
            for (var i = 0; i < 6; i++) world.RootUpdate();
            TestRunner.Equal(MountedContinuousDriveOwner.AutomaticRetryLimit, world.Pair.Interrupted, "Setup: the automatic limit was not reached.");
            TestRunner.True(!world.OwnerSettled(world.Pair) && world.Pair.Drive.ResidueCount == 1, "Setup: the obligation is retained past the limit.");
            // Native ends the command itself (another order displaces it). Nothing is owed any more.
            world.Interrupt(command, false);
            world.RootUpdate();
            TestRunner.Equal(0, world.Pair.Drive.ResidueCount, "A record whose retirement the facts establish stayed latched past the automatic limit.");
            TestRunner.Equal(MountedContinuousDriveOwner.AutomaticRetryLimit, world.Pair.Interrupted, "A record past the limit got another automatic attempt.");
            TestRunner.True(world.OwnerSettled(world.Pair) && world.MayRemoveGuards(), "The refusals did not clear with the record.");
        }

        private static void HealthyLifecycleControls()
        {
            // An ordinary release during stick movement completes the disable at once.
            var world = new World();
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            var command = world.Mount.Slot;
            TestRunner.True(world.RequestDisable() && !world.ServicesEnabled, "A healthy release did not complete the disable.");
            TestRunner.True(command.Finished && world.Pair.Interrupted == 1 && world.Pair.Cleared == 0 && world.Pair.Drive.ResidueCount == 0, "A healthy release was not retired once by its own native end.");
            TestRunner.True(MountedCleanupCompletionPolicy.IsComplete(world.LastCleanup, RelationshipState.Unmounted) && world.LastCleanup.Errors.Count == 0, "A healthy cleanup result carries an obligation.");
            TestRunner.True(world.OwnerSettled(world.Pair) && world.MayRemoveGuards(), "A healthy pair is refused settlement or guard removal.");

            // No drive at all, mounted or not.
            world = new World();
            TestRunner.True(world.RuntimeErrorCleanup() && !world.ServicesEnabled && world.Pair.Interrupted + world.Pair.Cleared == 0, "A pair without stick movement did not clean up at once.");
            world = new World();
            world.Pair.Mounted = false;
            world.Rider.Override = null;
            TestRunner.True(world.RequestDisable() && world.Pair.Interrupted + world.Pair.Cleared == 0, "An unmounted pair did not disable at once.");

            // Another pair's retained cleanup refuses the disable, and leaves this pair alone and settled.
            world = new World();
            var second = world.AddPair("rider2", "mount2");
            world.Current = world.Rider;
            world.Stick = 1f;
            world.Frame();
            world.Stick = 0f;
            world.Current = null;
            world.TurnBasedHold = true;
            world.Pair.FailInterrupts = 1;
            TestRunner.True(!world.RequestDisable(), "Setup: the first pair's retained cleanup refuses the disable.");
            TestRunner.True(second.Interrupted + second.Cleared == 0 && second.Drive.ResidueCount == 0 && world.OwnerSettled(second), "An unrelated pair was touched or left unsettled.");
            TestRunner.True(!world.OwnerSettled(world.Pair), "The pair with retained cleanup is settled.");
            world.RootUpdate();
            TestRunner.True(world.RequestDisable() && second.Interrupted + second.Cleared == 0, "The disable did not complete after recovery, or it touched the unrelated pair.");
        }

        // ---- the model ----

        private sealed class Agent
        {
            public bool Continuous;
            public bool PendingDestroy;
            public bool Destroyed;
            public float Direction;
        }

        // A unit's view. A unit can receive another view while an earlier one, and what it carries, stays alive.
        private sealed class ViewModel
        {
            // UnitEntityView.AgentOverride (a reference that may point at a destroyed component) and the continuous
            // component GetComponent finds on the view.
            public Agent Override;
            public Agent Component;
        }

        private sealed class Command
        {
            public Unit Executor;
            public bool Continuous;
            public bool Standard;
            public bool Free;
            // Native IsMoveUnit: the command has approached its target. Only such commands end on InterruptMove.
            public bool Approaching;
            // Native IsInterruptible is false: Run queues a new Move or Standard order instead of running it.
            public bool Uninterruptible;
            // Native Interrupt stamps the result before OnEnded and does nothing on a stamped command.
            public bool InterruptStamped;
            public bool Finished;
            public bool Accelerated;
            public bool EndEventRaised;
            public int EndCallbacks;
            // A custom handler's order that native runs first, queueing this one.
            public Command ExecuteBefore;
            public float Destination;
        }

        private sealed class Unit
        {
            public string Name;
            // The Move slot, the Standard slot (paired with it in native Run) and the Free slot.
            public Command Slot;
            public Command StandardSlot;
            public Command FreeSlot;
            public readonly List<Command> Queue = new List<Command>();
            public ViewModel View = new ViewModel();
            public float Position;
            public bool Conscious = true;
            public bool HasView = true;
            public bool Hold;

            public Agent Override { get { return View.Override; } set { View.Override = value; } }

            public Agent Component { get { return View.Component; } set { View.Component = value; } }

            public IEnumerable<Command> Held => new[] { Slot, StandardSlot, FreeSlot }.Where(command => command != null);

            public bool Holds(Command command) => ReferenceEquals(Slot, command) || ReferenceEquals(StandardSlot, command) || ReferenceEquals(FreeSlot, command);
        }

        // A live component: Unity's == null is true for a destroyed one.
        private static Agent Live(Agent agent) => agent != null && !agent.Destroyed ? agent : null;

        // Native UnitEntityView.AgentOverride = null: the old component is destroyed at the end of the frame.
        private static void SetOverrideNull(ViewModel view)
        {
            if (view.Override != null) view.Override.PendingDestroy = true;
            view.Override = null;
        }

        // The model's stand-in for GameMountedRelationshipService.ContinuousDriveHost.
        private sealed class Pair : IMountedContinuousDriveHost
        {
            public World World;
            public Unit Rider, Mount;
            public bool Mounted = true;
            public long Generation = 1;
            public string Fault;
            public int Interrupted, EndEvents, Cleared, Warnings, Reports;
            // Failure injection: the adapter's native calls fail this many times, and its report always fails.
            public int FailInterrupts, FailClears;
            public bool FailReports;
            public readonly Agent RiderAgent = new Agent();
            public readonly MountedContinuousDriveOwner Drive;

            public Pair() { Drive = new MountedContinuousDriveOwner(this); }

            bool IMountedContinuousDriveHost.PairMounted => Mounted;
            long IMountedContinuousDriveHost.PairGeneration => Generation;
            int IMountedContinuousDriveHost.Frame => World.FrameNumber;
            bool IMountedContinuousDriveHost.StickInputGated => World.InCombat;
            object IMountedContinuousDriveHost.Mount => Mount;
            object IMountedContinuousDriveHost.ViewOf(object unit) => ((Unit)unit).HasView ? ((Unit)unit).View : null;
            object IMountedContinuousDriveHost.ExecutorOf(object command) => ((Command)command).Executor;
            bool IMountedContinuousDriveHost.IsFinished(object command) => ((Command)command).Finished;
            bool IMountedContinuousDriveHost.InMountMoveSlot(object command) => ReferenceEquals(Mount.Slot, command);
            bool IMountedContinuousDriveHost.IsHeld(object command)
            {
                var executor = ((Command)command).Executor;
                return executor != null && (executor.Holds((Command)command) || executor.Queue.Contains((Command)command));
            }
            object IMountedContinuousDriveHost.OverrideOf(object view) => ((ViewModel)view).Override;
            bool IMountedContinuousDriveHost.IsContinuousAgent(object agent) => Live(agent as Agent) != null && ((Agent)agent).Continuous;
            void IMountedContinuousDriveHost.Interrupt(object command, bool raiseEndEvent)
            {
                Interrupted++;
                if (FailInterrupts > 0) { FailInterrupts--; throw new InvalidOperationException("the interrupt failed before the native call"); }
                if (raiseEndEvent) EndEvents++;
                World.Interrupt((Command)command, raiseEndEvent);
            }
            void IMountedContinuousDriveHost.ClearOverride(object view)
            {
                Cleared++;
                if (FailClears > 0) { FailClears--; throw new InvalidOperationException("the override could not be removed"); }
                SetOverrideNull((ViewModel)view);
            }
            void IMountedContinuousDriveHost.ReleaseRiderHold() => Rider.Hold = false;
            void IMountedContinuousDriveHost.Forget(object command) => World.Forget((Command)command);
            void IMountedContinuousDriveHost.Report(MountedContinuousDriveSettlement settlement, string reason, bool warning)
            {
                Reports++;
                if (warning) Warnings++;
                if (FailReports) throw new InvalidOperationException("the logger failed");
            }
        }

        private sealed class World
        {
            public bool Corrected = true;
            public bool Gamepad = true;
            public bool ProjectionFaults;
            // The mod's prefix of the public Run refuses the command (a save in progress, a restoration).
            public bool RunRefused;
            // The mod's prefix of the internal Run refuses a queue promotion.
            public bool PromotionRefused;
            // The wiring of the reviewed head 5e27c9bd: admitted orders are observed after the public Run only.
            public bool ObserveAdmissionOnPublicRunOnly;
            // The next native OnEnded fails after it marked the command finished and before it removes the override.
            public bool FailNextEndCallback;
            // A handler of the native end event, which native raises inside OnEnded before it removes the override.
            public Action<Command> EndEventHandler;
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
            public readonly List<ViewModel> Views = new List<ViewModel>();
            private readonly Dictionary<Command, Pair> boundCommands = new Dictionary<Command, Pair>();

            // The model's stand-in for the adapter's table of pair-bound commands.
            public int TrackedCommands => boundCommands.Count;

            public void Forget(Command command) => boundCommands.Remove(command);
            public readonly Unit Companion;
            public readonly Pair Pair;

            public World()
            {
                Companion = NewUnit("companion");
                Pair = AddPair("rider", "mount");
            }

            public Unit Rider => Pair.Rider;

            public Unit Mount => Pair.Mount;

            private Unit NewUnit(string name)
            {
                var unit = new Unit { Name = name };
                Views.Add(unit.View);
                return unit;
            }

            public Pair AddPair(string rider, string mount)
            {
                var pair = new Pair { World = this, Rider = NewUnit(rider), Mount = NewUnit(mount) };
                pair.Rider.Override = pair.RiderAgent;
                Pairs.Add(pair);
                return pair;
            }

            // The unit receives another view. The earlier view and whatever it carries stay alive.
            public ViewModel ReplaceView(Unit unit)
            {
                unit.View = new ViewModel();
                Views.Add(unit.View);
                return unit.View;
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
                EndOfFrame();
            }

            public void EndOfFrame()
            {
                foreach (var view in Views)
                    if (view.Component != null && view.Component.PendingDestroy) { view.Component.Destroyed = true; view.Component = null; }
            }

            public void Tick()
            {
                foreach (var unit in Units.ToList())
                {
                    var command = unit.Slot;
                    if (command != null && !command.Finished && command.Continuous && !TurnBasedHold)
                    {
                        // UnitMoveContiniously.OnTick, executed for the command's executor.
                        var agent = ContinuousAgent(command.Executor);
                        if (agent == null || agent.Direction <= 0.01f) OnEnded(command, true);
                    }
                    PromoteQueue(unit);
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

            // Native UnitCommands.RemoveFinishedAndUpdateQueue: finished commands leave their slots, and the first
            // queued command is promoted through the internal Run, not the public one, once its slots are free.
            private void PromoteQueue(Unit unit)
            {
                if (unit.Slot != null && unit.Slot.Finished) unit.Slot = null;
                if (unit.StandardSlot != null && unit.StandardSlot.Finished) unit.StandardSlot = null;
                if (unit.FreeSlot != null && unit.FreeSlot.Finished) unit.FreeSlot = null;
                if (unit.Queue.Count == 0) return;
                var next = unit.Queue[0];
                var free = next.Free ? unit.FreeSlot == null : unit.Slot == null && unit.StandardSlot == null;
                if (!free) return;
                unit.Queue.RemoveAt(0);
                InternalRun(unit, next, true);
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
                    PublicRun(unit, command);
                    if (ContinuousAgent(unit) != null) unit.Override.Direction = Stick;
                }
                var live = LiveContinuous(unit);
                if (live == null) { NativeFaults++; return; }
                live.Accelerated = Stick >= 0.95f;
                if (repath) MakeCompanionsFollow(FormationUnit(unit));
            }

            // Native UnitCommands.InterruptMove: only commands that have approached (IsMoveUnit), whatever their slot.
            // A stationary order and a continuous command are not among them.
            private void InterruptMove(Unit unit)
            {
                foreach (var command in unit.Held.ToList())
                    if (!command.Finished && command.Approaching) Interrupt(command, true);
                if (unit.Slot != null && unit.Slot.Finished) unit.Slot = null;
                if (unit.StandardSlot != null && unit.StandardSlot.Finished) unit.StandardSlot = null;
                if (unit.FreeSlot != null && unit.FreeSlot.Finished) unit.FreeSlot = null;
            }

            private void Init(Command command, Unit executor)
            {
                // The mod's prefix of native Init. Both implementations rewrite a mounted rider to its mount; the
                // correction also ends the rider's own approaching orders, which the native InterruptMove of the
                // taken-over unit no longer reaches.
                var pair = PairOf(executor);
                if (pair != null && pair.Mounted)
                {
                    if (Corrected) InterruptMove(pair.Rider);
                    if (ReferenceEquals(executor, pair.Rider)) executor = pair.Mount;
                }
                // Native Init: GetComponent, else AddComponent, on the executor's current view.
                command.Executor = executor;
                if (executor.HasView)
                {
                    if (executor.View.Component == null) executor.View.Component = new Agent { Continuous = true };
                    executor.View.Override = executor.View.Component;
                }
                // The correction's postfix.
                if (!Corrected) return;
                pair = PairOf(command.Executor);
                if (pair == null) return;
                pair.Drive.Bind(command);
                if (ReferenceEquals(pair.Drive.Command, command)) boundCommands[command] = pair;
            }

            // Native UnitCommands.Run(UnitCommand), with the mod's prefix and postfix of that overload.
            public void PublicRun(Unit container, Command command)
            {
                if (!RunRefused) InternalRun(container, command, false);
                if (!Corrected) return;
                // This postfix runs even when the prefix refused the command.
                if (command.Continuous) { PairOf(command.Executor)?.Drive.ObserveRun(command); return; }
                if (ObserveAdmissionOnPublicRunOnly) ObserveOrder(container, command);
            }

            // Native UnitCommands.Run(UnitCommand, bool fromQueue, bool doNotClearQueue): the one overload that puts a
            // command into a slot. The public Run and queue promotion both reach it.
            private void InternalRun(Unit container, Command command, bool fromQueue)
            {
                if (!(fromQueue && PromotionRefused) && container.Conscious)
                {
                    if (!fromQueue && command.Executor != null && !ReferenceEquals(command.Executor, container)) RefusedRuns++;
                    else
                    {
                        if (command.Executor == null) command.Executor = container;
                        if (!fromQueue && command.ExecuteBefore != null)
                        {
                            // A custom handler: this order waits first in the queue and the other runs, nested.
                            container.Queue.Insert(0, command);
                            InternalRun(container, command.ExecuteBefore, false);
                        }
                        else if (!fromQueue && container.Held.Any(held => !held.Finished && held.Uninterruptible)) container.Queue.Add(command);
                        else Place(container, command);
                    }
                }
                // The correction's postfix of this overload.
                if (Corrected && !command.Continuous && !ObserveAdmissionOnPublicRunOnly) ObserveOrder(container, command);
            }

            private void Place(Unit container, Command command)
            {
                container.Hold = false;
                if (command.Free)
                {
                    if (container.FreeSlot != null && !container.FreeSlot.Finished) Interrupt(container.FreeSlot, true);
                    container.FreeSlot = command;
                    return;
                }
                // Move and Standard are paired: a new order of either type ends the command in both slots.
                foreach (var existing in new[] { container.Slot, container.StandardSlot })
                    if (existing != null && !existing.Finished) Interrupt(existing, true);
                container.Slot = null;
                container.StandardSlot = null;
                if (command.Standard) container.StandardSlot = command;
                else container.Slot = command;
            }

            // The mod's observation of an order after the native Run that admits commands: it counts only if the order
            // is now in a slot of the rider's own container.
            private void ObserveOrder(Unit container, Command command)
            {
                var pair = PairOf(container);
                if (pair == null || !pair.Drive.IsBound) return;
                pair.Drive.ObserveRiderOrder(ReferenceEquals(container, pair.Rider) && container.Holds(command), !command.Free);
            }

            // Native UnitCommand.Interrupt: the result is stamped first; a stamped or finished command is left alone.
            public void Interrupt(Command command, bool raiseEvent)
            {
                if (command.InterruptStamped || command.Finished) return;
                command.InterruptStamped = true;
                OnEnded(command, raiseEvent);
            }

            // Native OnEnded: the command is finished, then (UnitMoveContiniously) whatever live override the
            // executor's CURRENT view holds is removed. The correction's guard inside that method lets a pair-bound
            // command remove only the agent it was bound with, on the view it was bound on.
            public void OnEnded(Command command, bool raiseEvent)
            {
                if (command.Finished) return;
                command.Finished = true;
                command.EndCallbacks++;
                command.EndEventRaised = raiseEvent;
                if (raiseEvent && EndEventHandler != null) EndEventHandler(command);
                if (FailNextEndCallback) { FailNextEndCallback = false; throw new InvalidOperationException("the end callback failed after the command became terminal"); }
                if (!command.Continuous || command.Executor == null) return;
                var view = command.Executor.View;
                if (Live(view.Override) == null) return;
                Pair bound;
                var mayClear = !Corrected || !boundCommands.TryGetValue(command, out bound) || bound.Drive.NativeEndMayClearOverride(command, view, view.Override);
                if (mayClear) SetOverrideNull(view);
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

            public void Dismount(Pair pair) => DismountResult(pair, CleanupTrigger.Manual);

            // ---- the lifecycle around the owner: a model of GameMountedRelationshipService.Dismount, the lifecycle
            // subscriber's per-pair cleanup and the composition root. Every decision in it is production code. ----

            // The correction: the dismount result reports cleanup the owner retains, and the root retries retained cleanup
            // before its enabled gate. False is the wiring of the reviewed head 25cbdbe2.
            public bool LifecycleReportsRetainedCleanup = true;
            public bool ServicesEnabled = true;
            // A save, an area change or a combat restoration holds the ordinary pair update back.
            public bool OrdinaryUpdateHeld;
            // The runtime-error stop was held back by stick-movement cleanup alone; the root takes it again.
            public bool FailureStopPending;
            public TransitionResult LastCleanup;

            // The production ownership registry. A pair's owner is reserved and committed the first time the lifecycle
            // touches the pair, with the retained predicate the relationship service gives its own owner.
            public readonly MountedPairRegistry<Unit, Pair> Registry = new MountedPairRegistry<Unit, Pair>(unit => unit.Name);
            private readonly Dictionary<Pair, MountedPairOwner<Unit, Pair>> owners = new Dictionary<Pair, MountedPairOwner<Unit, Pair>>();

            private bool Reserve(Pair pair, out MountedPairOwner<Unit, Pair> owner)
            {
                string refusal;
                return Registry.TryReserve(this, "campaign", pair.Rider, pair.Mount, 1, pair,
                    () => MountedCleanupCompletionPolicy.OwnerRetainsCleanup(false, false, pair.Drive.ResidueCount != 0),
                    () => true, () => true, out owner, out refusal);
            }

            public TransitionResult DismountResult(Pair pair, CleanupTrigger trigger, bool automaticRetry = false)
            {
                MountedPairOwner<Unit, Pair> owner;
                if (!owners.TryGetValue(pair, out owner) && pair.Mounted && Reserve(pair, out owner))
                {
                    Registry.Commit(owner);
                    owners[pair] = owner;
                }
                if (Corrected) pair.Drive.Release("dismount:" + trigger, MountedCleanupCompletionPolicy.ReleaseRecoversExplicitly(trigger, automaticRetry));
                pair.Mounted = false;
                if (ReferenceEquals(pair.Rider.Override, pair.RiderAgent)) pair.Rider.Override = null;
                var result = new TransitionResult(true, RelationshipState.Unmounted, trigger, new string[0], false, false);
                if (LifecycleReportsRetainedCleanup)
                    result = MountedCleanupCompletionPolicy.WithRetainedStickMovement(result, pair.Drive.ResidueCount, pair.Drive.LastRetirementFailure);
                if (owner != null && owner.State == MountedPairOwnershipState.Active) Registry.Detach(owner);
                LastCleanup = result;
                return result;
            }

            // CompositionRoot.SetEnabled(false): every pair's cleanup, then ordinary updates stop only if all are complete.
            public bool RequestDisable()
            {
                var complete = true;
                TransitionResult incomplete = null;
                foreach (var pair in Pairs)
                {
                    var result = DismountResult(pair, CleanupTrigger.ModDisabled);
                    if (MountedCleanupCompletionPolicy.IsComplete(result, RelationshipState.Unmounted)) continue;
                    complete = false;
                    incomplete = result;
                }
                if (incomplete != null) LastCleanup = incomplete;
                if (complete) ServicesEnabled = false;
                return complete;
            }

            // CompositionRoot.PerformFailureCleanup: the root throws, before it stops ordinary updates, when a pair's
            // cleanup left something behind.
            public bool RuntimeErrorCleanup()
            {
                var held = false;
                foreach (var pair in Pairs)
                {
                    var result = DismountResult(pair, CleanupTrigger.Exception);
                    if (!MountedCleanupCompletionPolicy.IsWithoutResidue(result)) held = true;
                }
                // In this model stick-movement cleanup is the only thing a dismount can leave behind.
                FailureStopPending = held;
                if (held) return false;
                ServicesEnabled = false;
                return true;
            }

            // CompositionRoot.UpdateCore: retained cleanup is retried before the enabled gate; the ordinary pair update,
            // which settles the drive, runs only while the services are enabled.
            public void RootUpdate()
            {
                FrameNumber++;
                Tick();
                if (LifecycleReportsRetainedCleanup) foreach (var pair in Pairs) pair.Drive.RecoverRetained();
                if (FailureStopPending && Pairs.All(pair => pair.Drive.ResidueCount == 0)) RuntimeErrorCleanup();
                if (ServicesEnabled && !OrdinaryUpdateHeld)
                {
                    foreach (var pair in Pairs.ToList()) Validate(pair);
                    UpdateMovement(false);
                }
                EndOfFrame();
            }

            // The registry's settlement of the pair's detached owner (ownership completion), and whether its actors can be
            // reserved again (reuse). A pair the lifecycle never mounted has no owner and is settled.
            public bool OwnerSettled(Pair pair)
            {
                Registry.RetireSettled();
                MountedPairOwner<Unit, Pair> owner;
                if (!owners.TryGetValue(pair, out owner)) return true;
                var retired = owner.State == MountedPairOwnershipState.Retired;
                MountedPairOwner<Unit, Pair> again;
                var reusable = Reserve(pair, out again);
                if (reusable) { Registry.Detach(again); Registry.RetireSettled(); }
                TestRunner.True(retired == reusable, "The owner's retirement and the reuse of its actors disagree.");
                return retired;
            }

            // MountedPatchController.Dispose: the guards go only when every pair's boundary cleanup is complete.
            public bool MayRemoveGuards() => Pairs.All(pair => MountedCleanupCompletionPolicy.IsComplete(DismountResult(pair, CleanupTrigger.ModDisabled), RelationshipState.Unmounted));

            // GameMountedRelationshipService.Shutdown.
            public bool Shutdown(Pair pair) => MountedCleanupCompletionPolicy.IsWithoutResidue(DismountResult(pair, CleanupTrigger.ModDisabled));

            // The mount attempt requires both views to carry no override.
            public bool CanMount(Pair pair) => !pair.Mounted && Live(pair.Rider.Override) == null && Live(pair.Mount.Override) == null;

            public void MountUp(Pair pair)
            {
                if (!CanMount(pair)) throw new InvalidOperationException("The pair cannot mount.");
                if (pair.Rider.Slot != null) { Interrupt(pair.Rider.Slot, true); pair.Rider.Slot = null; }
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

            // An order the unit is given directly and holds itself (an interaction, an ability). In range it is
            // stationary; out of range it approaches.
            public Command Order(Unit unit, bool standard, bool approaching = false, bool uninterruptible = false, Command executeBefore = null)
            {
                var order = new Command { Executor = unit, Standard = standard, Free = !standard, Approaching = approaching, Uninterruptible = uninterruptible, ExecuteBefore = executeBefore };
                PublicRun(unit, order);
                return order;
            }

            public Command RiderOrder(bool standard, bool approaching = false) => Order(Rider, standard, approaching);

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
                    PublicRun(target, new Command { Executor = target, Destination = destination, Approaching = true });
                }
            }
        }
    }
}
