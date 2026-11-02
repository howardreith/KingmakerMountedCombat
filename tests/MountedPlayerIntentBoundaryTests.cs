using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Hold and move-completion behavior over the production ledger, watch and gate. NativeActor is a small behavior model of
    // the installed UnitCommands: Run clears Hold for a command without AiAction, queues behind a running command on the
    // same target, runs a custom handler's prerequisite with doNotClearQueue, and runs the queue head on a later update
    // through the private overload only. The two decisions it delegates are the exact calls the Harmony adapters make.
    internal static class MountedPlayerIntentBoundaryTests
    {
        private sealed class Command
        {
            public bool AiOrigin;
            public bool AutomaticOrigin;
            public bool Started;
            public bool Finished;
            public object Target;
            public bool Interrupted;
            public bool IsAttack;
            public bool Merged;
        }

        private sealed class NativeActor
        {
            private readonly MountedAutomaticRunLedger ledger;
            private readonly long generation;
            public bool Hold;
            public bool Mounted = true;
            public Command Slot;
            public readonly LinkedList<Command> Queue = new LinkedList<Command>();
            public int Refused;

            public NativeActor(MountedAutomaticRunLedger ledger, long generation)
            {
                this.ledger = ledger;
                this.generation = generation;
            }

            public void Run(Command command, bool busyBlocks = true)
            {
                Queue.Clear();
                // TryMergeInto precedes every queue rule: a running attack absorbs a new attack in reach.
                if (command.IsAttack && Slot != null && Slot.IsAttack && Slot.Started && !Slot.Finished)
                {
                    Slot.Target = command.Target;
                    command.Merged = true;
                    return;
                }
                if (busyBlocks && Slot != null && Slot.Started && !Slot.Finished && Equals(Slot.Target, command.Target))
                {
                    Queue.AddLast(command);
                    return;
                }
                Execute(command, false, false);
            }

            // TryUseCustomHandler: the command waits at the head and its prerequisite runs first.
            public void RunWithPrerequisite(Command command, Command prerequisite)
            {
                Queue.Clear();
                prerequisite.AiOrigin = command.AiOrigin;
                Queue.AddFirst(command);
                Execute(prerequisite, false, true);
            }

            public void AdvanceQueue()
            {
                if (Slot != null && Slot.Finished) Slot = null;
                if (Slot != null || Queue.Count == 0) return;
                var head = Queue.First.Value;
                Queue.RemoveFirst();
                var decision = ledger.DecideQueuedRun(head, head.AutomaticOrigin, head.Finished, Mounted, generation);
                if (decision == MountedQueuedAutomaticRun.RefuseFinished) { Refused++; return; }
                if (decision == MountedQueuedAutomaticRun.RefuseStale) { Refused++; head.Finished = head.Interrupted = true; return; }
                Execute(head, true, false);
            }

            private void Execute(Command command, bool fromQueue, bool doNotClearQueue)
            {
                if (Slot != null && !Slot.Finished) Slot.Finished = Slot.Interrupted = true;
                Slot = command;
                if (command.AiOrigin) return;
                var firstQueued = Queue.First?.Value;
                if (!ledger.NativeRunKeepsHold(command, firstQueued, doNotClearQueue, generation)) Hold = false;
            }

            // SelectionManagerBase.Stop: clears Hold and interrupts every unstarted command, the queue included.
            public void Stop()
            {
                Hold = false;
                if (Slot != null && !Slot.Started) { Slot.Finished = Slot.Interrupted = true; Slot = null; }
                foreach (var queued in Queue) queued.Finished = queued.Interrupted = true;
                Queue.Clear();
            }
        }

        public static void Register(TestRunner runner)
        {
            runner.Run("an immediate automatic run keeps the executing actor's Hold", ImmediateRunKeepsHold);
            runner.Run("a queued automatic command keeps Hold at its later native run", QueuedRunKeepsHold);
            runner.Run("a Hold pressed between the two runs is kept and one cleared stays clear", HoldFollowsThePlayerBetweenRuns);
            runner.Run("a click on the queued target gives the later run the native player effect", PromotedQueuedRunClearsHold);
            runner.Run("player, reaction-free, unmounted and foreign commands keep every native Hold write", OtherCommandsStayNative);
            runner.Run("a custom handler's prerequisite keeps Hold only for the exact queued command", PrerequisiteOfExactCommandOnly);
            runner.Run("a stopped queued command never runs", StoppedQueuedCommandNeverRuns);
            runner.Run("a running attack merges a new attack without queuing it or touching Hold", RunningAttackMergesInsteadOfQueuing);
            runner.Run("the unselected mount's queued command, interrupted by the controller, is refused at its later run", UnselectedMountQueuedCommandRefused);
            runner.Run("a retired, replaced or stale queued command is refused at its later run", StaleQueuedRunRefused);
            runner.Run("a dropped unstarted automatic command is recognized and nothing else is", OrphanOnlyForDroppedAutomatic);
            runner.Run("a move completed in combat holds the pair", MoveCompletedInCombatHolds);
            runner.Run("a move issued in exploration and completed in combat holds the pair", ExplorationMoveArrivingInCombatHolds);
            runner.Run("a move completed out of combat leaves no hold", MoveCompletedOutOfCombatLeavesNoHold);
            runner.Run("an interrupted move leaves no hold", InterruptedMoveLeavesNoHold);
            runner.Run("a refused order changes no watched move and arms nothing", RefusedOrderArmsNothing);
            runner.Run("a replaced move answers only through its successor", ReplacedMoveAnswersThroughSuccessor);
            runner.Run("a queued move the queue dropped never holds", DroppedQueuedMoveNeverHolds);
            runner.Run("delegated approaches are not explicit player moves", DelegatedApproachesDoNotCount);
            runner.Run("dismount, generation and another pair keep their move state apart", MoveOwnershipBoundaries);
            runner.Run("issuing a move supersedes Stop without holding the pair", IssuingSupersedesStopOnly);
        }

        private static Command Automatic(MountedAutomaticRunLedger ledger, long generation, object target)
        {
            var command = new Command { AutomaticOrigin = true, Target = target };
            ledger.Bind(command, generation);
            return command;
        }

        private static void ImmediateRunKeepsHold()
        {
            foreach (var executing in new[] { "rider", "mount" })
            {
                var ledger = new MountedAutomaticRunLedger();
                var actor = new NativeActor(ledger, 7) { Hold = true };
                actor.Run(Automatic(ledger, 7, new object()));
                TestRunner.True(actor.Hold, "An immediate automatic run cleared the " + executing + "'s Hold.");
            }
        }

        private static void QueuedRunKeepsHold()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var automatic = Automatic(ledger, 7, target);
            actor.Run(automatic);
            TestRunner.True(actor.Queue.Contains(automatic) && actor.Hold, "The command did not wait in the queue with Hold intact.");
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(ReferenceEquals(actor.Slot, automatic), "The queued command never ran.");
            TestRunner.True(actor.Hold, "The later native run cleared Hold.");
        }

        private static void HoldFollowsThePlayerBetweenRuns()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var actor = new NativeActor(ledger, 7) { Slot = new Command { Started = true, Target = target } };
            actor.Run(Automatic(ledger, 7, target));
            actor.Hold = true;
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(actor.Hold, "A Hold pressed while the command waited was cleared.");

            var second = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            second.Run(Automatic(ledger, 7, target));
            second.Hold = false;
            second.Slot.Finished = true;
            second.AdvanceQueue();
            TestRunner.True(!second.Hold, "An old snapshot restored a Hold the player had cleared.");
        }

        private static void PromotedQueuedRunClearsHold()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var automatic = Automatic(ledger, 7, target);
            actor.Run(automatic);
            automatic.AutomaticOrigin = false;
            ledger.Clear();
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(ReferenceEquals(actor.Slot, automatic), "The player's own promoted command was refused.");
            TestRunner.True(!actor.Hold, "A player order did not have the native effect on Hold.");
        }

        private static void OtherCommandsStayNative()
        {
            var ledger = new MountedAutomaticRunLedger();
            var bound = Automatic(ledger, 7, new object());
            var actor = new NativeActor(ledger, 7) { Hold = true };
            actor.Run(new Command());
            TestRunner.True(!actor.Hold, "A player command kept Hold while an automatic command was bound.");

            actor.Hold = true;
            actor.Run(new Command { AiOrigin = true });
            TestRunner.True(actor.Hold, "A native AI command gained a Hold write it never had.");

            var unmounted = new NativeActor(new MountedAutomaticRunLedger(), 7) { Hold = true };
            unmounted.Run(bound);
            TestRunner.True(!unmounted.Hold, "Another pair's ledger kept a command it does not own.");

            var staleGeneration = new NativeActor(ledger, 8) { Hold = true };
            staleGeneration.Run(bound);
            TestRunner.True(!staleGeneration.Hold, "A command of a replaced generation kept Hold.");
        }

        private static void PrerequisiteOfExactCommandOnly()
        {
            var ledger = new MountedAutomaticRunLedger();
            var actor = new NativeActor(ledger, 7) { Hold = true };
            var automatic = Automatic(ledger, 7, new object());
            var activation = new Command();
            actor.RunWithPrerequisite(automatic, activation);
            TestRunner.True(actor.Hold, "A blade activation for the automatic command cleared Hold.");
            activation.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(ReferenceEquals(actor.Slot, automatic) && actor.Hold, "The waiting command lost Hold at its own run.");

            var ordinary = new NativeActor(ledger, 7) { Hold = true };
            ordinary.RunWithPrerequisite(new Command(), new Command());
            TestRunner.True(!ordinary.Hold, "A player's own prerequisite kept Hold.");
        }

        private static void StoppedQueuedCommandNeverRuns()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var automatic = Automatic(ledger, 7, target);
            actor.Run(automatic);
            actor.Stop();
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(automatic.Finished && automatic.Interrupted && actor.Slot == null, "A stopped command ran later.");
            TestRunner.True(!actor.Hold, "Stop's own native effect on Hold was undone.");
        }

        private static void RunningAttackMergesInsteadOfQueuing()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var running = new Command { Started = true, Target = target, IsAttack = true };
            var actor = new NativeActor(ledger, 7) { Hold = true, Slot = running };
            var automatic = Automatic(ledger, 7, target);
            automatic.IsAttack = true;
            actor.Run(automatic);
            TestRunner.True(automatic.Merged && actor.Queue.Count == 0 && ReferenceEquals(actor.Slot, running),
                "An attack behind a running attack was modeled as queued; the controller must see it in neither place.");
            TestRunner.True(actor.Hold, "A merge touched Hold.");
        }

        // Native Stop reaches selected units only, so the rider's Stop leaves the mount's queue: the controller's cancel
        // interrupts the command there, and native queue advancement still reaches it.
        private static void UnselectedMountQueuedCommandRefused()
        {
            var ledger = new MountedAutomaticRunLedger();
            var target = new object();
            var mount = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var automatic = Automatic(ledger, 7, target);
            mount.Run(automatic);
            automatic.Finished = automatic.Interrupted = true;
            ledger.Clear();
            mount.Slot.Finished = true;
            mount.AdvanceQueue();
            TestRunner.True(mount.Slot == null && mount.Refused == 1, "A cancelled command reached the mount's slot.");
            TestRunner.True(mount.Hold, "A refused run still wrote the mount's Hold.");
        }

        private static void StaleQueuedRunRefused()
        {
            var target = new object();
            // Retired: the controller interrupted it, and native queue advancement still reaches it.
            var ledger = new MountedAutomaticRunLedger();
            var actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var retired = Automatic(ledger, 7, target);
            actor.Run(retired);
            retired.Finished = retired.Interrupted = true;
            ledger.Clear();
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(actor.Slot == null && actor.Refused == 1 && actor.Hold, "A retired command ran from the queue.");

            // Its intent ended without an interrupt reaching it.
            ledger = new MountedAutomaticRunLedger();
            actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var abandoned = Automatic(ledger, 7, target);
            actor.Run(abandoned);
            ledger.Clear();
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(actor.Slot == null && abandoned.Interrupted, "A command without an owner ran from the queue.");

            // Dismounted, and a replaced generation.
            ledger = new MountedAutomaticRunLedger();
            actor = new NativeActor(ledger, 7) { Hold = true, Slot = new Command { Started = true, Target = target } };
            var dismounted = Automatic(ledger, 7, target);
            actor.Run(dismounted);
            actor.Mounted = false;
            actor.Slot.Finished = true;
            actor.AdvanceQueue();
            TestRunner.True(actor.Slot == null && dismounted.Interrupted, "A dismounted pair's command ran from the queue.");
            TestRunner.True(ledger.DecideQueuedRun(dismounted, true, false, true, 8) == MountedQueuedAutomaticRun.RefuseStale,
                "A replaced generation's command stayed admissible.");
            TestRunner.True(ledger.DecideQueuedRun(new object(), false, false, true, 7) == MountedQueuedAutomaticRun.NotAutomatic,
                "An ordinary queued command was gated by the automatic path.");
        }

        private static void OrphanOnlyForDroppedAutomatic()
        {
            TestRunner.True(MountedAutomaticRunLedger.IsOrphaned(true, false, false, false, false), "A dropped automatic command stayed active.");
            TestRunner.True(!MountedAutomaticRunLedger.IsOrphaned(false, false, false, false, false), "A click-owned command was swept.");
            TestRunner.True(!MountedAutomaticRunLedger.IsOrphaned(true, false, false, false, true), "A waiting command was swept.");
            TestRunner.True(!MountedAutomaticRunLedger.IsOrphaned(true, false, false, true, false), "A slotted command was swept.");
            TestRunner.True(!MountedAutomaticRunLedger.IsOrphaned(true, true, false, false, false), "A started command was swept.");
        }

        // The controller's own settlement of one move's native result.
        private static void Settle(MountedPlayerMoveWatch watch, MountedAutomaticAttackGate gate, long generation, bool finished, bool acted, bool held, bool inSlot, bool mounted = true)
        {
            if (watch.Observe(generation, mounted, finished, acted, held, inSlot) == MountedPlayerMoveOutcome.CompletedHeld) gate.ObservePlayerMoveCompleted();
        }

        private static void MoveCompletedInCombatHolds()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            var move = new Command();
            watch.Bind(move, 7);
            Settle(watch, gate, 7, false, false, false, true);
            TestRunner.True(!gate.PlayerMoveHold && ReferenceEquals(watch.Move, move), "A move still under way held the pair.");
            Settle(watch, gate, 7, true, true, true, false);
            gate.Tick(7, true, true, false, true);
            TestRunner.True(gate.PlayerMoveHold && watch.Move == null, "A move completed in combat did not hold the pair.");
            gate.ObservePlayerOrder();
            TestRunner.True(!gate.PlayerMoveHold, "A new order did not release the hold.");
        }

        private static void ExplorationMoveArrivingInCombatHolds()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            watch.Bind(new Command(), 7);
            for (var tick = 0; tick < 5; tick++)
            {
                gate.Tick(7, true, false, false, true);
                Settle(watch, gate, 7, false, false, false, true);
            }
            gate.Tick(7, true, true, false, true);
            Settle(watch, gate, 7, false, false, false, true);
            TestRunner.True(watch.Move != null && !gate.PlayerMoveHold, "The exploration ticks lost or armed the move.");
            gate.Tick(7, true, true, false, true);
            Settle(watch, gate, 7, true, true, true, false);
            TestRunner.True(gate.PlayerMoveHold, "A move arriving in combat could chase away from its destination.");
        }

        private static void MoveCompletedOutOfCombatLeavesNoHold()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            watch.Bind(new Command(), 7);
            TestRunner.True(watch.Observe(7, true, true, true, false, false) == MountedPlayerMoveOutcome.CompletedNotHeld, "An exploration arrival was misread.");
            TestRunner.True(!gate.PlayerMoveHold, "An exploration arrival held the pair.");
        }

        private static void InterruptedMoveLeavesNoHold()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            watch.Bind(new Command(), 7);
            gate.ObservePlayerOrder();
            // Any Hold the mover holds here is not this move's: native OnAction never ran for it.
            var outcome = watch.Observe(7, true, true, false, true, false);
            TestRunner.True(outcome == MountedPlayerMoveOutcome.Interrupted && !gate.PlayerMoveHold, "An interrupted move manufactured a successful arrival.");
        }

        private static void RefusedOrderArmsNothing()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            var live = new Command();
            watch.Bind(live, 7);
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(true, true, true, false, true, false), "A move native Run never took became the watched move.");
            gate.ObservePlayerOrder();
            TestRunner.True(ReferenceEquals(watch.Move, live) && !gate.PlayerMoveHold, "A refused order disturbed the live move or armed the hold.");
        }

        private static void ReplacedMoveAnswersThroughSuccessor()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            var first = new Command();
            var second = new Command();
            watch.Bind(first, 7);
            watch.Bind(second, 7);
            TestRunner.True(ReferenceEquals(watch.Move, second), "The successor did not replace the watched move.");
            Settle(watch, gate, 7, false, false, false, true);
            TestRunner.True(!gate.PlayerMoveHold, "The replaced move's interruption or the live successor held the pair.");
            Settle(watch, gate, 7, true, true, true, false);
            TestRunner.True(gate.PlayerMoveHold, "The successor's own arrival did not hold the pair.");
        }

        private static void DroppedQueuedMoveNeverHolds()
        {
            var gate = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            watch.Bind(new Command(), 7);
            TestRunner.True(watch.Observe(7, true, false, false, false, true) == MountedPlayerMoveOutcome.Pending, "A queued move was treated as ended.");
            TestRunner.True(watch.Observe(7, true, false, false, true, false) == MountedPlayerMoveOutcome.Lost && !gate.PlayerMoveHold, "A dropped move held the pair.");
            TestRunner.True(watch.Move == null, "A dropped move stayed watched.");
        }

        private static void DelegatedApproachesDoNotCount()
        {
            TestRunner.True(MountedPlayerMoveWatch.IsExplicitGroundMove(true, true, true, false, true, true), "The ground click's own move was refused.");
            // A pair attack, carried spell or trap approach runs a UnitMoveTo with CreatedByPlayer for native routing.
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(false, true, true, false, true, true), "A delegated approach counted as a player move.");
            // MountedDoorApproachMove derives from UnitMoveTo; the door's recovery move carries CreatedByPlayer false.
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(true, false, true, false, true, true), "A derived door approach counted as a player move.");
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(true, true, false, false, true, true), "A recovery move counted as a player move.");
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(true, true, true, true, true, true), "A brain move counted as a player move.");
            TestRunner.True(!MountedPlayerMoveWatch.IsExplicitGroundMove(true, true, true, false, false, true), "A move on another unit counted as this pair's.");
        }

        private static void MoveOwnershipBoundaries()
        {
            var gate = new MountedAutomaticAttackGate();
            var other = new MountedAutomaticAttackGate();
            var watch = new MountedPlayerMoveWatch();
            var otherWatch = new MountedPlayerMoveWatch();
            watch.Bind(new Command(), 7);
            otherWatch.Bind(new Command(), 7);
            Settle(watch, gate, 7, true, true, true, false);
            TestRunner.True(gate.PlayerMoveHold && !other.PlayerMoveHold && otherWatch.Move != null, "One pair's arrival reached another pair.");

            var stale = new MountedPlayerMoveWatch();
            stale.Bind(new Command(), 7);
            TestRunner.True(stale.Observe(8, true, true, true, true, false) == MountedPlayerMoveOutcome.Lost, "A replaced generation's arrival held its successor.");

            var dismounted = new MountedPlayerMoveWatch();
            dismounted.Bind(new Command(), 7);
            TestRunner.True(dismounted.Observe(7, false, true, true, true, false) == MountedPlayerMoveOutcome.Lost && dismounted.Move == null,
                "A dismounted pair's pending move leaked into a future operation.");
        }

        private static void IssuingSupersedesStopOnly()
        {
            var gate = new MountedAutomaticAttackGate();
            gate.ObserveStop(7);
            gate.ObservePlayerOrder();
            TestRunner.True(!gate.IsSuppressed(7), "A new move order did not supersede Stop.");
            TestRunner.True(!gate.PlayerMoveHold, "Issuing an order held the pair before any arrival.");
        }
    }
}
