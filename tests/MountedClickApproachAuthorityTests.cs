using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues M04 closeout, review of b1a88b4e.
    //   R1: which command may be given to the pair as the native cannot-attack click approach. b1a88b4e authorised every
    //       eligible UnitMoveTo run on a mounted rider while a frame-scoped flag was open (opened by the unit-click prefix,
    //       closed by its postfix). The correction replaces only the one UnitCommands.Run in ClickUnitHandler.OnClick that
    //       runs the click command's local (a transpiler, executed against the installed IL by
    //       Test-MarkerOwnershipContracts.ps1), so the authority is that call's own command and container: nothing is
    //       stored, borrowed, consumed twice or left behind.
    //   R2: what runs on the mount. b1a88b4e ran a replacement built from six of the native move's values with
    //       CreatedByPlayer forced true; the correction runs the original native command object, which native never
    //       marks CreatedByPlayer, and the open ground order recognises it by identity.
    //   R3: when a placement is recorded. b1a88b4e inferred it after OnMovementStarted from a visible pointer; the
    //       correction records from the postfix of the AddPointer call itself.
    // Production code executed: MountedDestinationMarkerPolicy.RoutesRiderClickApproach, CommandRouter.RouteGroundMove,
    // MountedPlayerMoveWatch (IsExplicitGroundMove, Bind, IsCurrent), ObservedPlayerAttackRequest (the hostile-click
    // observation), MountedPointerPlacementLedger and MountedMarkerOwnershipPolicy. The click, native Run, the stock agent's
    // placement and the ground-order lifecycle are modelled from the installed IL (ClickUnitHandler.OnClick 060093ED,
    // UnitCommands.Run, UnitEntityView.OnMovementStarted 0600184E, ClickPointerManager.AddPointer 06002D62); the model is
    // not native evidence. Authority.FrameScope is b1a88b4e's mechanism; RecordAtAddPointer = false is b1a88b4e's record.
    internal static class MountedClickApproachAuthorityTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("M04 closeout R1/R2: the exact cannot-attack command C is routed once, as itself, to the mount; the rider's Run never happens; one pair marker; b1a88b4e ran a CreatedByPlayer copy instead", ExactCommandRoutedOnceAsItself);
            runner.Run("M04 closeout R1: an unrelated synchronous rider move U before C stays native and C still routes once; b1a88b4e rerouted U", UnrelatedSynchronousCommandStaysNative);
            runner.Run("M04 closeout R1: nested clicks each route only their own command, and the inner click's end takes nothing from the outer; b1a88b4e left the outer command on the rider", NestedClicksKeepTheirOwnAuthority);
            runner.Run("M04 closeout R1: a native click that throws leaves no authority for a later command in the frame; b1a88b4e rerouted it", ThrowingClickLeavesNoAuthority);
            runner.Run("M04 closeout R1: another mounted pair cannot consume the authority and an unrelated selected unit is untouched; b1a88b4e rerouted the other pair's order", PairsAndUnrelatedUnitsAreIsolated);
            runner.Run("M04 closeout R1: controls stay native: simulated click, a click native ends before its branch, AI order, UnitMoveTo subclass, unmounted rider, scene precedence, and the ground, entrance, door, trap, loot and scene paths", ControlsStayNative);
            runner.Run("M04 closeout R2: refused admission leaves neither the original nor a replacement live, and admission and completion stay balanced", RefusedAdmissionLeavesNothingLive);
            runner.Run("M04 closeout R3: a placement is recorded for the command AddPointer actually ran for, at the same coordinates too", AddPointerRecordsItsCommand);
            runner.Run("M04 closeout R3: OnMovementStarted without AddPointer leaves the earlier record, so the old pointer is PlacedForEarlierCommand past the bound; b1a88b4e attributed it to the new command", DeclinedPlacementKeepsEarlierRecord);
            runner.Run("M04 closeout R3: a preview, a missing pointer and the cleanup boundaries record nothing and keep nothing alive", PreviewMissingPointerAndCleanup);
        }

        private enum Authority { FrameScope, CallSite }

        // ---- R1 / R2 ------------------------------------------------------------------------------------------------

        private static void ExactCommandRoutedOnceAsItself()
        {
            foreach (var mode in new[] { Authority.CallSite, Authority.FrameScope })
            {
                var world = new World(mode);
                var c = world.Click(55f, world.A.Rider).Single();
                var routed = world.A.Mount.Slot;
                TestRunner.Equal(1, world.Routed, mode + ": the click approach was not routed exactly once.");
                TestRunner.True(world.A.Rider.Slot == null && world.A.Rider.Pointer == null, mode + ": the rider's Run happened or its marker was placed.");
                TestRunner.True(routed != null && routed.Destination == 55f && world.A.Mount.Pointer == 55f && world.VisibleMarkers() == 1,
                    mode + ": the pair does not show exactly the mount's marker at the clicked unit.");
                TestRunner.True(world.A.Rider.ManualTarget == world.Enemy, mode + ": the native manual target after the Run changed.");
                TestRunner.Equal(world.A.Admissions, world.A.Completions, mode + ": ground-order admission and completion are unbalanced.");
                if (mode == Authority.CallSite)
                {
                    TestRunner.True(ReferenceEquals(routed, c) && c.Inits == 1 && c.Executor == world.A.Mount, "The original command is not what the mount runs, or it was initialised other than once.");
                    TestRunner.True(!c.CreatedByPlayer, "CreatedByPlayer differs from native (native never sets it on this command).");
                    TestRunner.True(world.A.Watch.IsCurrent(c, world.A.Generation), "The watched ground move is not the original command.");
                    TestRunner.True(world.A.Observed.IsPlayerCommand(c) && world.A.Observed.IsPlayerCommand(routed), "The hostile-click observation is not attached to the routed command.");
                }
                else
                {
                    TestRunner.True(!ReferenceEquals(routed, c) && c.Inits == 0 && routed.CreatedByPlayer, "Setup (b1a88b4e): a CreatedByPlayer copy should have run instead of the original.");
                    TestRunner.True(!world.A.Watch.IsCurrent(c, world.A.Generation) && world.A.Observed.IsPlayerCommand(c) && !world.A.Observed.IsPlayerCommand(routed),
                        "Setup (b1a88b4e): the observation should have stayed on the original, which never ran, and the watch on the copy.");
                }
            }
        }

        private static void UnrelatedSynchronousCommandStaysNative()
        {
            foreach (var mode in new[] { Authority.CallSite, Authority.FrameScope })
            {
                var world = new World(mode);
                Cmd u = null;
                // A click subscriber (or another patch) moves the rider synchronously, before the click's own Run.
                var c = world.Click(55f, new[] { world.A.Rider }, (unit, command) => world.NativeRun(world.A.Rider, u = new Cmd { Name = "U", Destination = 70f })).Single();
                if (mode == Authority.CallSite)
                {
                    TestRunner.True(u.Executor == world.A.Rider && u.Inits == 1 && world.A.Rider.Slot == u && world.A.Rider.Pointer == 70f,
                        "The unrelated rider move was rerouted, suppressed or lost its native marker.");
                    TestRunner.True(ReferenceEquals(world.A.Mount.Slot, c) && world.Routed == 1, "The click approach did not route once.");
                }
                else
                {
                    TestRunner.True(world.A.Rider.Slot != u && u.Inits == 0 && world.Routed == 2, "Setup (b1a88b4e): the unrelated move should have been rerouted by the open scope.");
                }
            }
        }

        private static void NestedClicksKeepTheirOwnAuthority()
        {
            foreach (var mode in new[] { Authority.CallSite, Authority.FrameScope })
            {
                var world = new World(mode, 2);
                Cmd c2 = null;
                var c1 = world.Click(55f, new[] { world.A.Rider }, (unit, command) => c2 = world.Click(30f, world.B.Rider).Single()).Single();
                TestRunner.True(world.B.Rider.Slot == null && world.B.Mount.Slot != null && world.B.Mount.Slot.Destination == 30f,
                    mode + ": the inner click's own command was not routed to its own pair.");
                if (mode == Authority.CallSite)
                {
                    TestRunner.True(ReferenceEquals(world.A.Mount.Slot, c1) && ReferenceEquals(world.B.Mount.Slot, c2) && c1.Inits == 1 && c2.Inits == 1 && world.Routed == 2,
                        "The outer command lost its authority to the inner click, or a command was given to the wrong pair.");
                    TestRunner.True(world.A.Rider.Slot == null && world.A.Rider.Pointer == null, "The outer command stayed on the rider.");
                    TestRunner.True(world.A.Watch.IsCurrent(c1, 1) && world.B.Watch.IsCurrent(c2, 1) && !world.A.Watch.IsCurrent(c2, 1), "A pair watches the other pair's command.");
                }
                else
                {
                    TestRunner.True(world.A.Rider.Slot == c1 && world.A.Rider.Pointer == 55f && world.Routed == 1,
                        "Setup (b1a88b4e): the inner click's end should have closed the scope under the outer command.");
                }
            }
        }

        private static void ThrowingClickLeavesNoAuthority()
        {
            foreach (var mode in new[] { Authority.CallSite, Authority.FrameScope })
            {
                var world = new World(mode);
                List<Cmd> made = null;
                try { world.Click(55f, new[] { world.A.Rider }, (unit, command) => made = new List<Cmd> { command }, throwAfterRequest: true); }
                catch (InvalidOperationException) { }
                var c = made.Single();
                // Later in the same frame, an unrelated system moves the rider.
                var u = new Cmd { Name = "U", Destination = 70f };
                world.NativeRun(world.A.Rider, u);
                TestRunner.True(c.Executor == null && c.Inits == 0 && world.A.Mount.Slot != c && world.A.Rider.Slot != c, mode + ": the command of the throwing click became live.");
                if (mode == Authority.CallSite)
                    TestRunner.True(world.Routed == 0 && u.Executor == world.A.Rider && world.A.Rider.Slot == u, "Authority survived the throwing click.");
                else
                    TestRunner.True(world.Routed == 1 && u.Inits == 0, "Setup (b1a88b4e): the scope left open by the throw should have rerouted the later move.");
            }
        }

        private static void PairsAndUnrelatedUnitsAreIsolated()
        {
            foreach (var mode in new[] { Authority.CallSite, Authority.FrameScope })
            {
                var world = new World(mode, 2);
                Cmd other = null;
                var made = world.Click(55f, new[] { world.A.Rider, world.Companion }, (unit, command) =>
                {
                    if (unit == world.A.Rider) world.NativeRun(world.B.Rider, other = new Cmd { Name = "B's order", Destination = 20f });
                });
                var companion = made[1];
                TestRunner.True(world.Companion.Slot == companion && companion.Inits == 1 && world.Companion.Pointer == 55f, mode + ": the unrelated selected unit's own approach was touched.");
                TestRunner.True(ReferenceEquals(world.A.Mount.Slot, mode == Authority.CallSite ? made[0] : world.A.Mount.Slot) && world.A.Rider.Slot == null, mode + ": the pair's own approach was not routed.");
                if (mode == Authority.CallSite)
                    TestRunner.True(world.B.Rider.Slot == other && world.B.Mount.Slot == null && other.Inits == 1 && world.Routed == 1, "Another pair consumed the click's authority.");
                else
                    TestRunner.True(world.B.Mount.Slot != null && world.B.Rider.Slot == null && other.Inits == 0, "Setup (b1a88b4e): the other pair's order should have been rerouted.");
            }
        }

        private static void ControlsStayNative()
        {
            var policy = new Func<bool, bool, bool, bool, bool, bool, bool, bool>(MountedDestinationMarkerPolicy.RoutesRiderClickApproach);
            TestRunner.True(policy(true, true, true, false, true, false, false), "The exact click approach was not routed.");
            TestRunner.True(!policy(false, true, true, false, true, false, false), "An unmounted rider's approach was routed.");
            TestRunner.True(!policy(true, false, true, false, true, false, false), "Another unit's command was routed.");
            TestRunner.True(!policy(true, true, false, false, true, false, false), "A UnitMoveTo subclass or another command was routed.");
            TestRunner.True(!policy(true, true, true, true, true, false, false), "An AI order was routed.");
            TestRunner.True(!policy(true, true, true, false, false, false, false), "A Run other than the native click's own was routed.");
            TestRunner.True(!policy(true, true, true, false, true, true, false), "A simulated click was routed.");
            TestRunner.True(!policy(true, true, true, false, true, false, true), "A command while a scene may own the rider was routed (staging keeps its precedence).");

            Func<World, Cmd> riderNative = w => w.A.Rider.Slot;
            var simulated = new World(Authority.CallSite) { Simulating = true };
            var sc = simulated.Click(55f, simulated.A.Rider).Single();
            TestRunner.True(riderNative(simulated) == sc && simulated.Routed == 0, "A simulated click's command was routed.");
            var gated = new World(Authority.CallSite);
            TestRunner.True(gated.Click(55f, new[] { gated.A.Rider }, nativeReachesBranch: false).Count == 0 && gated.Routed == 0 && gated.A.Mount.Slot == null,
                "A click native ends before its cannot-attack branch (right-click inspect, turn-based cursor) routed something.");
            var ai = new World(Authority.CallSite);
            var ac = ai.Click(55f, new[] { ai.A.Rider }, (u, c) => c.Ai = true).Single();
            TestRunner.True(riderNative(ai) == ac && ai.Routed == 0, "An AI order was routed.");
            var subclass = new World(Authority.CallSite);
            var xc = subclass.Click(55f, new[] { subclass.A.Rider }, (u, c) => c.ExactMove = false).Single();
            TestRunner.True(riderNative(subclass) == xc && subclass.Routed == 0, "A UnitMoveTo subclass was routed.");
            var unmounted = new World(Authority.CallSite);
            unmounted.A.Mounted = false;
            var uc = unmounted.Click(55f, unmounted.A.Rider).Single();
            TestRunner.True(riderNative(unmounted) == uc && unmounted.A.Mount.Slot == null && unmounted.Routed == 0, "An unmounted rider's approach was routed.");
            var scene = new World(Authority.CallSite) { SceneOwnsRider = true };
            var nc = scene.Click(55f, scene.A.Rider).Single();
            TestRunner.True(riderNative(scene) == nc && scene.Routed == 0, "A command while a scene may own the rider was routed.");

            // Other paths never pass through the click's Run, so the hook is never asked.
            var paths = new World(Authority.CallSite);
            var ground = paths.ClickGround(40f);
            TestRunner.True(ReferenceEquals(paths.A.Mount.Slot, ground) && ground.CreatedByPlayer && paths.A.Watch.IsCurrent(ground, 1) && paths.Routed == 0,
                "The ground click's own routing changed.");
            foreach (var other in new[] { "entrance member command", "door interaction", "trap interaction", "loot command", "scene command" })
            {
                var command = new Cmd { Name = other, ExactMove = false, ShowMarker = other == "entrance member command" };
                paths.NativeRun(paths.A.Rider, command);
                TestRunner.True(paths.A.Rider.Slot == command && command.Inits == 1 && paths.Routed == 0, "The " + other + " passed through the click-approach routing.");
            }
        }

        private static void RefusedAdmissionLeavesNothingLive()
        {
            var world = new World(Authority.CallSite) { AdmitGroundOrder = false };
            var c = world.Click(55f, world.A.Rider).Single();
            TestRunner.True(c.Executor == null && c.Inits == 0 && world.A.Rider.Slot == null && world.A.Mount.Slot == null && world.VisibleMarkers() == 0,
                "A refused admission left the original or a replacement live.");
            TestRunner.True(world.A.Admissions == 1 && world.A.Completions == 1 && !world.A.GroundOrderOpen && world.A.GroundOrderClickApproach == null,
                "Admission and completion are unbalanced, or the order's identity outlived it.");
        }

        // ---- R3 -----------------------------------------------------------------------------------------------------

        private static void AddPointerRecordsItsCommand()
        {
            foreach (var atAddPointer in new[] { true, false })
            {
                var world = new PointerWorld { RecordAtAddPointer = atAddPointer };
                var unit = new Unit { Name = "mount" };
                var a = world.Order(unit, 40f);
                world.MovementStarted(unit, nativePlaces: true);
                TestRunner.True(world.Ledger.PlacedFor(unit, a, out _), atAddPointer + ": the actual placement was not recorded for A.");
                var b = world.Order(unit, 40f);   // the same coordinates as A
                world.MovementStarted(unit, nativePlaces: true);
                TestRunner.True(world.Ledger.PlacedFor(unit, b, out var frame) && frame == world.Frame && !world.Ledger.PlacedFor(unit, a, out _),
                    atAddPointer + ": a placement for B at A's coordinates was not recorded for B.");
                TestRunner.Equal(MountedMarkerClass.ExpectedPhysicalMover, world.Classify(unit, 4f), atAddPointer + ": B's own pointer is not the physical mover's.");
            }
        }

        private static void DeclinedPlacementKeepsEarlierRecord()
        {
            foreach (var atAddPointer in new[] { true, false })
            {
                var world = new PointerWorld { RecordAtAddPointer = atAddPointer };
                var unit = new Unit { Name = "mount" };
                var a = world.Order(unit, 40f);
                world.MovementStarted(unit, nativePlaces: true);
                var b = world.Order(unit, 70f);
                world.Update(unit);   // B qualifies, so native shows the pointer, still at A's place
                // B's plan completes; OnMovementStarted runs but native declines AddPointer.
                world.MovementStarted(unit, nativePlaces: false);
                TestRunner.True(unit.PointerVisible && unit.Pointer == 40f, "Setup: the pointer should still show at A's place for B.");
                if (atAddPointer)
                {
                    TestRunner.True(world.Ledger.PlacedFor(unit, a, out _) && !world.Ledger.PlacedFor(unit, b, out _), "A declined placement was attributed to B.");
                    TestRunner.Equal(MountedMarkerClass.PlacementPending, world.Classify(unit, 1f), "Inside the bound the old pointer is not a pending transition.");
                    TestRunner.Equal(MountedMarkerClass.PlacedForEarlierCommand, world.Classify(unit, MountedMarkerOwnershipPolicy.PlacementTransitionSeconds + 1f),
                        "Past the bound the old pointer is not PlacedForEarlierCommand.");
                }
                else
                {
                    TestRunner.True(world.Ledger.PlacedFor(unit, b, out _) && world.Classify(unit, 4f) == MountedMarkerClass.ExpectedPhysicalMover,
                        "Setup (b1a88b4e): the visible old pointer should have been attributed to B, hiding the anomaly.");
                }
            }
        }

        private static void PreviewMissingPointerAndCleanup()
        {
            foreach (var atAddPointer in new[] { true, false })
            {
                var world = new PointerWorld { RecordAtAddPointer = atAddPointer };
                var unit = new Unit { Name = "mount" };
                var a = world.Order(unit, 40f);
                world.MovementStarted(unit, nativePlaces: true);
                var b = world.Order(unit, 70f);
                world.MovementStarted(unit, nativePlaces: true, preview: true);
                TestRunner.True(world.Ledger.PlacedFor(unit, a, out _) && !world.Ledger.PlacedFor(unit, b, out _), atAddPointer + ": a preview became a gameplay placement.");
            }
            var bare = new PointerWorld();
            var lone = new Unit { Name = "no pointer" };
            var order = bare.Order(lone, 10f);
            bare.MovementStarted(lone, nativePlaces: false);
            TestRunner.True(lone.Pointer == null && !bare.Ledger.PlacedFor(lone, order, out _), "A unit without a pointer gained a record.");
            bare.Ledger.Record(lone, order, 3);
            bare.Ledger.Reset();
            TestRunner.True(!bare.Ledger.PlacedFor(lone, order, out _), "A record survived the cleanup boundary.");
            var ledger = new MountedPointerPlacementLedger<Unit, Cmd>();
            var references = RecordAndForget(ledger);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            TestRunner.True(!references[0].IsAlive && !references[1].IsAlive, "The placement record kept a unit or a command alive.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] RecordAndForget(MountedPointerPlacementLedger<Unit, Cmd> ledger)
        {
            var unit = new Unit { Name = "replaced world" };
            var command = new Cmd { Name = "old command" };
            ledger.Record(unit, command, 1);
            return new[] { new WeakReference(unit), new WeakReference(command) };
        }

        // ---- model --------------------------------------------------------------------------------------------------

        private sealed class Cmd
        {
            internal string Name;
            internal float Destination;
            internal bool ExactMove = true, Ai, CreatedByPlayer, ShowMarker = true, Finished;
            internal Unit Executor;
            internal int Inits;
        }

        private sealed class Unit
        {
            internal string Name;
            internal Cmd Slot;
            internal float? Pointer;
            internal bool PointerVisible;
            internal Unit ManualTarget;
        }

        private sealed class Pair
        {
            internal Unit Rider, Mount;
            internal bool Mounted = true;
            internal long Generation = 1;
            internal readonly MountedPlayerMoveWatch Watch = new MountedPlayerMoveWatch();
            internal readonly ObservedPlayerAttackRequest<Unit, Cmd> Observed = new ObservedPlayerAttackRequest<Unit, Cmd>();
            internal bool GroundOrderOpen;
            internal Cmd GroundOrderClickApproach;
            internal int Admissions, Completions;
        }

        private sealed class World
        {
            private readonly Authority mode;
            internal readonly List<Pair> Pairs = new List<Pair>();
            internal readonly Unit Companion = new Unit { Name = "companion" }, Enemy = new Unit { Name = "enemy" };
            internal int Frame = 10, OpenFrame = -1;   // b1a88b4e's MountedNativeUnitClickScope
            internal bool Simulating, SceneOwnsRider, AdmitGroundOrder = true;
            internal int Routed;

            internal World(Authority mode, int pairs = 1)
            {
                this.mode = mode;
                for (var i = 0; i < pairs; i++) Pairs.Add(new Pair { Rider = new Unit { Name = "rider" + i }, Mount = new Unit { Name = "mount" + i } });
            }

            internal Pair A => Pairs[0];
            internal Pair B => Pairs[1];

            private Pair OwnerOf(Unit container) => Pairs.FirstOrDefault(p => p.Rider == container || p.Mount == container);

            internal int VisibleMarkers() => Pairs.SelectMany(p => new[] { p.Rider, p.Mount }).Concat(new[] { Companion })
                .Count(u => u.Pointer != null && u.Slot != null && !u.Slot.Finished && u.Slot.ShowMarker);

            // Native UnitCommands.Run behind the product's command-run prefix (which first binds the hostile-click
            // observation). The unit's own stock agent then plans the command and AddPointer places a marker move's pointer.
            internal void NativeRun(Unit container, Cmd cmd)
            {
                var owner = OwnerOf(container);
                owner?.Observed.Bind(container, cmd, Frame);
                if (mode == Authority.FrameScope && FrameScopeReroute(owner, container, cmd)) return;
                if (cmd.Executor == null) { cmd.Executor = container; cmd.Inits++; }
                else if (cmd.Executor != container) return;   // native: "Invalid command, can't run"
                if (container.Slot != null && !container.Slot.Finished) container.Slot.Finished = true;
                container.Slot = cmd;
                if (cmd.ShowMarker) container.Pointer = cmd.Destination;
                if (owner != null && owner.GroundOrderOpen && container == owner.Mount) ObserveGroundOrderRun(owner, cmd);   // its postfix
            }

            // b1a88b4e: UnitCommandRunPrefix asked RerouteRiderClickApproach of every rider Run while the scope was open.
            private bool FrameScopeReroute(Pair owner, Unit container, Cmd cmd)
            {
                if (!MountedDestinationMarkerPolicy.RoutesRiderClickApproach(owner != null && owner.Mounted, owner != null && container == owner.Rider,
                        cmd.ExactMove, cmd.Ai, OpenFrame >= 0 && OpenFrame == Frame, Simulating, SceneOwnsRider)) return false;
                var copy = new Cmd { Name = cmd.Name + " (copy)", Destination = cmd.Destination, ShowMarker = cmd.ShowMarker, CreatedByPlayer = true };
                GroundOrder(owner, () => NativeRun(owner.Mount, copy), null);
                Routed++;
                return true;
            }

            // The corrected hook: the one Run in OnClick that runs the click command's local, with that call's own command.
            private void SiteRun(Unit container, Cmd cmd)
            {
                var owner = OwnerOf(container);
                if (mode == Authority.FrameScope || !MountedDestinationMarkerPolicy.RoutesRiderClickApproach(owner != null && owner.Mounted,
                        owner != null && container == owner.Rider, cmd.ExactMove, cmd.Ai, true, Simulating, SceneOwnsRider))
                {
                    NativeRun(container, cmd);
                    return;
                }
                owner.Observed.Bind(container, cmd, Frame);
                var route = CommandRouter.RouteGroundMove(new MountedPair(owner.Rider.Name, owner.Mount.Name), container.Name);
                if (route.Action != CommandRoutingAction.RewriteRiderToMount) { NativeRun(container, cmd); return; }
                GroundOrder(owner, () => NativeRun(owner.Mount, cmd), cmd);
                Routed++;
            }

            // GroundCommandPrefix (admission, routing, OpenGroundOrder) ... GroundCommandPostfix in a finally
            // (CompleteGroundCommandAdmission, which clears the order's identity).
            private void GroundOrder(Pair owner, Action runOnMount, Cmd clickApproach)
            {
                owner.Admissions++;
                try
                {
                    if (!AdmitGroundOrder) return;
                    owner.GroundOrderOpen = true;
                    if (clickApproach != null) owner.GroundOrderClickApproach = clickApproach;
                    runOnMount();
                }
                finally
                {
                    owner.GroundOrderOpen = false;
                    owner.GroundOrderClickApproach = null;
                    owner.Completions++;
                }
            }

            private static void ObserveGroundOrderRun(Pair owner, Cmd cmd)
            {
                if (!MountedPlayerMoveWatch.IsExplicitGroundMove(owner.GroundOrderOpen, cmd.ExactMove,
                        cmd.CreatedByPlayer || ReferenceEquals(cmd, owner.GroundOrderClickApproach), cmd.Ai, true, owner.Mount.Slot == cmd)) return;
                owner.Watch.Bind(cmd, owner.Generation);
            }

            internal List<Cmd> Click(float at, params Unit[] selected) => Click(at, selected, null, false, true);

            // ClickUnitHandler.OnClick for selected units that cannot attack: b1a88b4e's prefix opened its scope; each unit
            // gets new UnitMoveTo(target, corpulence * 1.1); the attack request event (OnAttackRequested, observed for pair
            // actors); the one Run; then ManualTarget. b1a88b4e's postfix closed the scope, never reached on a throw.
            internal List<Cmd> Click(float at, Unit[] selected, Action<Unit, Cmd> onRequest = null, bool throwAfterRequest = false, bool nativeReachesBranch = true)
            {
                var made = new List<Cmd>();
                if (mode == Authority.FrameScope) OpenFrame = Frame;
                if (nativeReachesBranch)
                    foreach (var unit in selected)
                    {
                        var c = new Cmd { Name = "C:" + unit.Name, Destination = at };
                        made.Add(c);
                        OwnerOf(unit)?.Observed.Observe(unit, Enemy, Frame);
                        onRequest?.Invoke(unit, c);
                        if (throwAfterRequest) throw new InvalidOperationException("the native click threw");
                        SiteRun(unit, c);
                        unit.ManualTarget = Enemy;
                    }
                if (mode == Authority.FrameScope) OpenFrame = -1;
                return made;
            }

            // A routed ground click: the rider's player-created UnitMoveTo given to the mount (CommandRouter).
            internal Cmd ClickGround(float at)
            {
                var order = new Cmd { Name = "ground", Destination = at, CreatedByPlayer = true };
                GroundOrder(A, () => NativeRun(A.Mount, order), null);
                return order;
            }
        }

        // The native pointer contract for one unit: OnMovementStarted may call AddPointer (placement) or decline; Update
        // shows the pointer while the unit holds a qualifying command. RecordAtAddPointer = false is b1a88b4e's postfix on
        // OnMovementStarted, recording whenever a visible pointer exists afterwards.
        private sealed class PointerWorld
        {
            internal bool RecordAtAddPointer = true;
            internal readonly MountedPointerPlacementLedger<Unit, Cmd> Ledger = new MountedPointerPlacementLedger<Unit, Cmd>();
            internal int Frame = 1;

            internal Cmd Order(Unit unit, float at)
            {
                if (unit.Slot != null) unit.Slot.Finished = true;
                Frame++;
                return unit.Slot = new Cmd { Name = "order@" + at, Destination = at, CreatedByPlayer = true };
            }

            private static Cmd Qualifying(Unit unit) => unit.Slot != null && !unit.Slot.Finished && unit.Slot.ShowMarker ? unit.Slot : null;

            internal void Update(Unit unit) => unit.PointerVisible = unit.Pointer != null && Qualifying(unit) != null;

            internal void MovementStarted(Unit unit, bool nativePlaces, bool preview = false)
            {
                if (!preview && nativePlaces) AddPointer(unit, unit.Slot.Destination);
                // A preview goes to AddPreviewPointer (the preview pointer, not the unit's).
                if (!RecordAtAddPointer && !preview && unit.Pointer != null && unit.PointerVisible) Ledger.Record(unit, Qualifying(unit), Frame);
            }

            private void AddPointer(Unit unit, float at)
            {
                unit.Pointer = at;
                unit.PointerVisible = true;
                if (RecordAtAddPointer) Ledger.Record(unit, Qualifying(unit), Frame);
            }

            internal MountedMarkerClass Classify(Unit unit, float seen)
            {
                Update(unit);
                var command = Qualifying(unit);
                return MountedMarkerOwnershipPolicy.ClassifyUnit(new MountedMarkerFacts
                {
                    PointerExists = unit.Pointer != null,
                    PointerVisible = unit.PointerVisible,
                    QualifyingCommandLive = command != null,
                    PlacedForCurrentCommand = Ledger.PlacedFor(unit, command, out _),
                    SecondsSinceCommandSeen = seen,
                    Moving = true
                });
            }
        }
    }
}
