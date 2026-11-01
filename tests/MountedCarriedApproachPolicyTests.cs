using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 3 (TRAP-01, SPELL-01): the offline decisions behind the mount's carried approach for the rider's own
    // native command. These are pure policy regressions; the engine seams (native OnTickApproaching, the delegated
    // move, the trap trigger-zone probe) are exercised natively.
    internal static class MountedCarriedApproachPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("carried approach: the mounted rider's own unstarted ability or trap interaction is carried", CarriesRiderAbilityAndTrap);
            runner.Run("carried approach: started, finished, unmounted, mount-executed and simulated commands are never carried", NeverCarriesForeignOrSettledCommands);
            runner.Run("carried approach: relationship shells, the charge shell and AI-originated commands stay native", NeverCarriesOwnedShellsOrAi);
            runner.Run("carried approach: a short move retries by the real-time lifecycle and signals the native end in turn-based combat", MoveEndSemanticsByMode);
            runner.Run("carried approach: replanning follows the recipient only beyond the epsilon and rejects non-finite drift", ReplanBounds);
            runner.Run("carried approach: a request refused by a later gate releases nothing (wrong-turn door, refused stock command)", RefusedRequestReleasesNothing);
            runner.Run("carried approach: an admitted replacement that displaced the carried command releases it exactly once", AdmittedReplacementReleasesOnce);
            runner.Run("carried approach: the carrier's own move, the carried command's own run and a non-displacing request keep it", OwnAndNonDisplacingRequestsKeep);
            runner.Run("carried approach: an admitted mount order releases the movement; only a player order ends the rider's command", MountOrderAuthority);
            runner.Run("carried approach: a native boundary inside Run, a non-pair container and a missing ticket never release", NativeBoundaryAndForeignRequests);
            runner.Run("carried approach: cleanup while the carrier still owns its Move slot stops its own movement", CleanupOwnedMove);
            runner.Run("carried approach: cleanup of a finished move whose slot holds a successor leaves the successor and its path", CleanupLeavesSuccessor);
            runner.Run("carried approach: a successor the cleanup sweep activates keeps its path", CleanupSweepActivatesSuccessor);
            runner.Run("carried approach: repeated cleanup is idempotent and never stops a later successor", RepeatedCleanup);
            runner.Run("carried approach: the driver only replaces the native approach while it should approach (offline lifecycle)", LifecycleDrivesOnlyWhileApproaching);
        }

        private static MountedCarriedApproachDisposition Decide(
            bool mounted = true, bool rider = true, bool started = false, bool finished = false,
            MountedCarriedCommandKind kind = MountedCarriedCommandKind.Ability,
            bool shell = false, bool charge = false, bool ai = false, bool simulating = false)
        {
            return MountedCarriedApproachPolicy.Decide(mounted, rider, started, finished, kind, shell, charge, ai, simulating);
        }

        private static void CarriesRiderAbilityAndTrap()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.Carry, Decide(), "A rider ability was not carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.Carry, Decide(kind: MountedCarriedCommandKind.TrapInteraction), "A rider trap interaction was not carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(kind: MountedCarriedCommandKind.None), "An unrelated command was carried.");
        }

        private static void NeverCarriesForeignOrSettledCommands()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(started: true), "A started command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(finished: true), "A finished command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(mounted: false), "An unmounted rider's command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(rider: false), "A command of another actor (the mount or a third unit) was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(simulating: true), "A cursor-simulated command was carried.");
        }

        private static void NeverCarriesOwnedShellsOrAi()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(shell: true), "A Mount/Dismount relationship shell was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(charge: true), "The mounted Charge shell was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(ai: true), "An AI-originated cast was carried (general mounted AI is a later sprint).");
        }

        private static void MoveEndSemanticsByMode()
        {
            TestRunner.Equal(MountedCarriedMoveEndAction.LifecycleRetry, MountedCarriedApproachPolicy.OnDelegatedMoveEnded(false), "Real time did not retry by the lifecycle.");
            TestRunner.Equal(MountedCarriedMoveEndAction.SignalNativeApproachEnd, MountedCarriedApproachPolicy.OnDelegatedMoveEnded(true), "Turn-based combat did not defer to the native approach-end rule.");
        }

        private static void ReplanBounds()
        {
            var e = MountedCarriedApproachPolicy.ReplanEpsilonMetres;
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(0f, e), "A still recipient was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(e, e), "Drift at the epsilon was replanned.");
            TestRunner.True(MountedCarriedApproachPolicy.ShouldReplan(e + 0.01f, e), "Drift beyond the epsilon was not replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(float.NaN, e), "NaN drift was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(float.PositiveInfinity, e), "Infinite drift was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(5f, -1f), "A negative epsilon replanned.");
        }

        // ---- Review correction A: admission before destructive release ------------------------------------------
        // The fake models the pair's live carried approach and what native UnitCommands.Run did to it. The flow under
        // test is the one the Run prefix and postfix call (MountedCarriedRequestFlow.Admit / Complete).
        private sealed class FakeRequestHost : IMountedCarriedRequestHost
        {
            internal readonly object Approach = new object(), Carried = new object(), Move = new object();
            internal readonly object RiderContainer = new object(), MountContainer = new object(), OtherContainer = new object();
            internal bool CarriedDisplaced, MountDisplaced, PlayerRequest = true, ApproachReleased;
            internal object ReboundApproach;
            internal int Releases, Interrupts;
            internal string LastReason;
            public object LiveApproach => ReboundApproach ?? (ApproachReleased ? null : Approach);
            public bool IsRiderContainer(object container) => ReferenceEquals(container, RiderContainer);
            public bool IsMountContainer(object container) => ReferenceEquals(container, MountContainer);
            public bool IsCarriedCommand(object approach, object command) => ReferenceEquals(approach, Approach) && ReferenceEquals(command, Carried);
            public bool IsDelegatedMove(object approach, object command) => ReferenceEquals(approach, Approach) && ReferenceEquals(command, Move);
            public bool IsPlayerRequest(object command) => PlayerRequest;
            public bool CarriedCommandDisplaced(object approach) => CarriedDisplaced;
            public bool MountMovementDisplaced(object approach) => MountDisplaced;
            public void Release(object approach, string reason) { if (ReferenceEquals(approach, LiveApproach)) { Releases++; ApproachReleased = true; LastReason = reason; } }
            public void InterruptCarriedCommand(object approach) { Interrupts++; }
        }

        private static Func<bool> Gate(List<string> calls, string name, bool result) => () => { calls.Add(name); return result; };

        private static void RefusedRequestReleasesNothing()
        {
            // A rider door click on the wrong turn: the door gate refuses after the request entered the prefix.
            var host = new FakeRequestHost();
            var calls = new List<string>();
            var request = new object();
            var admitted = MountedCarriedRequestFlow.Admit(host, host.RiderContainer, request,
                new[] { Gate(calls, "door", false), Gate(calls, "stockAttack", true), Gate(calls, "stockCommand", true) }, out var ticket);
            TestRunner.True(!admitted && ticket == null, "A refused request was admitted or received a release ticket.");
            TestRunner.True(calls.Count == 1 && calls[0] == "door", "Gates after the refusing gate ran.");
            // The Run postfix still runs (Harmony), with no ticket: native Run never executed the request.
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, request), "A refused request changed the approach.");
            TestRunner.True(host.Releases == 0 && host.Interrupts == 0 && ReferenceEquals(host.LiveApproach, host.Approach),
                "A refused request released the carried approach, its delegated move or its command.");
            // Refused by the last gate (a rejected stock command) after the first two admitted.
            calls.Clear();
            admitted = MountedCarriedRequestFlow.Admit(host, host.RiderContainer, request,
                new[] { Gate(calls, "door", true), Gate(calls, "stockAttack", true), Gate(calls, "stockCommand", false) }, out ticket);
            TestRunner.True(!admitted && ticket == null && calls.Count == 3, "A request refused by the stock-command gate was admitted.");
            TestRunner.True(host.Releases == 0 && host.Interrupts == 0, "A request refused by the last gate released the approach.");
        }

        private static void AdmittedReplacementReleasesOnce()
        {
            var host = new FakeRequestHost();
            var replacement = new object();
            var admitted = MountedCarriedRequestFlow.Admit(host, host.RiderContainer, replacement, new[] { (Func<bool>)(() => true) }, out var ticket);
            TestRunner.True(admitted && ticket != null, "An admitted rider request received no ticket.");
            TestRunner.True(host.Releases == 0, "Admission released before native Run executed.");
            host.CarriedDisplaced = true; // native Run replaced the same-slot carried command
            TestRunner.Equal(MountedCarriedRequestAction.Release, MountedCarriedRequestFlow.Complete(host, ticket, replacement), "A displacing replacement did not release.");
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, replacement), "A duplicate completion acted again.");
            TestRunner.True(host.Releases == 1 && host.Interrupts == 0 && host.LastReason == "rider command admitted",
                "An admitted rider replacement did not release exactly once, or interrupted the native command itself.");
        }

        private static void OwnAndNonDisplacingRequestsKeep()
        {
            // The carrier's own delegated move run on the mount: never a release, even though the mount's slot changes.
            var host = new FakeRequestHost { MountDisplaced = true, CarriedDisplaced = true };
            MountedCarriedRequestFlow.Admit(host, host.MountContainer, host.Move, new Func<bool>[0], out var ticket);
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, host.Move), "The carrier's own move cancelled itself.");
            // The carried command's own (re)run on the rider.
            MountedCarriedRequestFlow.Admit(host, host.RiderContainer, host.Carried, new Func<bool>[0], out ticket);
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, host.Carried), "The carried command's own run released it.");
            // An admitted rider command native Run queued or ran in another slot without displacing the carried command.
            host = new FakeRequestHost { CarriedDisplaced = false };
            MountedCarriedRequestFlow.Admit(host, host.RiderContainer, new object(), new Func<bool>[0], out ticket);
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, new object()), "A non-displacing rider command released the approach.");
            TestRunner.True(host.Releases == 0 && host.Interrupts == 0, "A keeping request touched the approach.");
        }

        private static void MountOrderAuthority()
        {
            var host = new FakeRequestHost { MountDisplaced = true, PlayerRequest = true };
            var order = new object();
            MountedCarriedRequestFlow.Admit(host, host.MountContainer, order, new Func<bool>[0], out var ticket);
            TestRunner.Equal(MountedCarriedRequestAction.ReleaseAndInterrupt, MountedCarriedRequestFlow.Complete(host, ticket, order), "A player mount order did not redirect the pair.");
            TestRunner.True(host.Releases == 1 && host.Interrupts == 1, "A player mount order did not release once and end the rider's pending command once.");
            host = new FakeRequestHost { MountDisplaced = true, PlayerRequest = false };
            MountedCarriedRequestFlow.Admit(host, host.MountContainer, order, new Func<bool>[0], out ticket);
            TestRunner.Equal(MountedCarriedRequestAction.Release, MountedCarriedRequestFlow.Complete(host, ticket, order), "An AI mount request was not limited to the movement.");
            TestRunner.True(host.Releases == 1 && host.Interrupts == 0, "An AI mount request ended the rider's native command.");
            host = new FakeRequestHost { MountDisplaced = false };
            MountedCarriedRequestFlow.Admit(host, host.MountContainer, order, new Func<bool>[0], out ticket);
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, order), "A mount request that left the movement with the carrier released it.");
        }

        private static void NativeBoundaryAndForeignRequests()
        {
            // Native Run already ended the carried command and the controller re-bound to the new command.
            var host = new FakeRequestHost { CarriedDisplaced = true };
            var request = new object();
            MountedCarriedRequestFlow.Admit(host, host.RiderContainer, request, new Func<bool>[0], out var ticket);
            host.ReboundApproach = new object();
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, ticket, request), "A settled approach was released a second time.");
            TestRunner.True(host.Releases == 0, "A re-bound approach was released by the old request.");
            // Another unit's container never receives a ticket.
            host = new FakeRequestHost { CarriedDisplaced = true, MountDisplaced = true };
            TestRunner.True(MountedCarriedRequestFlow.Admit(host, host.OtherContainer, request, new Func<bool>[0], out ticket) && ticket == null,
                "Another unit's admitted request received a release ticket.");
            TestRunner.Equal(MountedCarriedRequestAction.Keep, MountedCarriedRequestFlow.Complete(host, null, request), "A missing ticket released the approach.");
            TestRunner.True(host.Releases == 0 && host.Interrupts == 0, "A foreign request touched the approach.");
            TestRunner.Equal(MountedCarriedRequestAction.Keep,
                MountedCarriedApproachPolicy.AfterAdmittedRequest(false, false, false, false, true, true, true), "A non-pair request released the approach.");
        }

        // ---- Review correction B: release only movement still owned by the carrier ------------------------------
        // A small model of the native mount command state: four slots (Move = 1), the queue, finished flags, and which
        // command's path the agent follows. SweepFinished follows native RemoveFinishedAndUpdateQueue: finished commands
        // leave their slots, then the queue head runs when its slot is free (and acquires the agent).
        private sealed class FakeCommand
        {
            internal bool Finished;
            internal int Slot = 1;
        }

        private sealed class FakeMoveHost : IMountedOwnMoveHost
        {
            internal readonly FakeCommand[] Slots = new FakeCommand[4];
            internal readonly List<FakeCommand> Queue = new List<FakeCommand>();
            internal object AgentPathOwner;
            internal int AgentStops, Sweeps, Interrupts;
            public object MoveSlot => Slots[1];
            public bool Holds(object command) => command is FakeCommand c && ReferenceEquals(Slots[c.Slot], c);
            public bool HasOtherLiveCommand(object own) => Queue.Count != 0 || Slots.Any(c => c != null && !c.Finished && !ReferenceEquals(c, own));
            public bool IsFinished(object command) => !(command is FakeCommand c) || c.Finished;
            public void Interrupt(object command) { if (command is FakeCommand c && !c.Finished) { c.Finished = true; Interrupts++; } }
            public void SweepFinished()
            {
                Sweeps++;
                for (var i = 0; i < Slots.Length; i++) if (Slots[i] != null && Slots[i].Finished) Slots[i] = null;
                if (Queue.Count != 0 && Slots[Queue[0].Slot] == null)
                {
                    var next = Queue[0];
                    Queue.RemoveAt(0);
                    Slots[next.Slot] = next;
                    AgentPathOwner = next; // native Run of the queue head starts its own path
                }
            }
            public void StopAgent() { AgentStops++; AgentPathOwner = null; }
        }

        private static void CleanupOwnedMove()
        {
            var host = new FakeMoveHost();
            var own = new FakeCommand();
            host.Slots[1] = own;
            host.AgentPathOwner = own;
            var result = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(result.Interrupted && result.Swept && result.AgentStopped && result.OwnReleased && !result.SuccessorRetained,
                "Cleanup did not end, sweep and stop the carrier's own move.");
            TestRunner.True(host.Slots[1] == null && host.AgentPathOwner == null && host.AgentStops == 1, "The carrier's own movement survived its cleanup.");
        }

        private static void CleanupLeavesSuccessor()
        {
            var host = new FakeMoveHost();
            var own = new FakeCommand { Finished = true };       // replaced or detached before the delayed cleanup
            var successor = new FakeCommand();
            host.Slots[1] = successor;
            host.AgentPathOwner = successor;
            var result = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(!result.Interrupted && !result.Swept && !result.AgentStopped && result.SuccessorRetained && result.OwnReleased,
                "Cleanup of a replaced move touched the successor or misreported its own release.");
            TestRunner.True(ReferenceEquals(host.Slots[1], successor) && !successor.Finished && ReferenceEquals(host.AgentPathOwner, successor) && host.AgentStops == 0,
                "The successor's command or path was stopped by the old move's cleanup.");
            // A successor in another slot (a Standard approach) owns the agent just the same.
            host = new FakeMoveHost();
            own = new FakeCommand { Finished = true };
            var standard = new FakeCommand { Slot = 0 };
            host.Slots[1] = own;
            host.Slots[0] = standard;
            host.AgentPathOwner = standard;
            result = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(result.Swept && !result.AgentStopped && result.SuccessorRetained && result.OwnReleased && host.Slots[1] == null &&
                ReferenceEquals(host.Slots[0], standard) && ReferenceEquals(host.AgentPathOwner, standard), "A successor in another slot lost its path.");
        }

        private static void CleanupSweepActivatesSuccessor()
        {
            var host = new FakeMoveHost();
            var own = new FakeCommand();
            var queued = new FakeCommand();
            host.Slots[1] = own;
            host.Queue.Add(queued);
            host.AgentPathOwner = own;
            var result = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(result.Interrupted && result.Swept && result.SuccessorRetained && !result.AgentStopped && result.OwnReleased,
                "The cleanup sweep's successor was not recognised before the agent was stopped.");
            TestRunner.True(ReferenceEquals(host.Slots[1], queued) && !queued.Finished && ReferenceEquals(host.AgentPathOwner, queued) && host.Queue.Count == 0 && host.AgentStops == 0,
                "The successor activated by the sweep lost its command, path or queue position.");
        }

        private static void RepeatedCleanup()
        {
            var host = new FakeMoveHost();
            var own = new FakeCommand();
            host.Slots[1] = own;
            host.AgentPathOwner = own;
            MountedOwnMovePolicy.Release(host, own);
            var second = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(!second.Interrupted && !second.Swept && second.OwnReleased && host.Interrupts == 1 && host.Sweeps == 1,
                "Repeated cleanup ended or swept the carrier's move again.");
            // A successor that arrived after the first cleanup is never stopped by a repeated one.
            var successor = new FakeCommand();
            host.Slots[1] = successor;
            host.AgentPathOwner = successor;
            var stops = host.AgentStops;
            var third = MountedOwnMovePolicy.Release(host, own);
            TestRunner.True(third.SuccessorRetained && !third.AgentStopped && host.AgentStops == stops && ReferenceEquals(host.AgentPathOwner, successor),
                "A repeated cleanup stopped a later successor.");
            TestRunner.True(MountedOwnMovePolicy.Release(host, null).OwnReleased && host.AgentStops == stops, "Cleanup without an own move touched the mount.");
        }

        // The driver reuses the accepted Sprint 2 lifecycle unchanged: one move while out of range, no move once in range.
        private static void LifecycleDrivesOnlyWhileApproaching()
        {
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            Func<bool, MountedDoorApproachMoveState, float, MountedDoorApproachObservation> obs = (inRange, move, distance) => new MountedDoorApproachObservation
            {
                NowSeconds = clock.Observe(frame++, 1f / 60f), InRange = inRange, Move = move, DistanceToPoint = distance,
                DisplacementSinceLastTick = 0.1f, StallClockRuns = move == MountedDoorApproachMoveState.Live, ReapproachSample = 0.5f
            };
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(obs(false, MountedDoorApproachMoveState.None, 12f)), "Out of range did not begin the mount's move.");
            TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(obs(false, MountedDoorApproachMoveState.Live, 8f)), "A live move was not waited for.");
            TestRunner.Equal(MountedDoorApproachAction.Satisfied, life.Tick(obs(true, MountedDoorApproachMoveState.Live, 2f)), "In range did not satisfy.");
            TestRunner.Equal(MountedDoorApproachAction.Satisfied, life.Tick(obs(false, MountedDoorApproachMoveState.None, 2f)), "A satisfied lifecycle began another move.");
            TestRunner.Equal(1, life.MoveStarts, "More than one move was started for one in-range arrival.");
            TestRunner.True(!string.IsNullOrEmpty(MountedCarriedApproachPolicy.Feedback(MountedCarriedCommandKind.TrapInteraction, "Horse")) &&
                MountedCarriedApproachPolicy.ExhaustedFeedback(MountedCarriedCommandKind.Ability, "Horse", "no progress").Contains("cannot reach"),
                "Feedback strings are empty or lost the unreachable reason.");
        }
    }
}
