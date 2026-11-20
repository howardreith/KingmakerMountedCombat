using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues sprint 3 (M04): the extra or stale green destination marker. No cause of the owner's report is
    // established; these tests exercise routing, arrival, Stop, replacement and blocked arrival through the production
    // decisions (CommandRouter, MountedPlayerMoveWatch, MountedEntranceCarryPolicy, MountedGroupPacePolicy.ClassifyOrder,
    // MountedEntranceCarrierWatch, MountedDestinationMarkerPolicy) against a small model of the installed native marker
    // contract (decompiled Kingmaker 2.1.7b, as a summary):
    //   ClickPointerManager: one pointer per unit, never removed. Update shows it while the unit holds a live UnitMoveTo
    //       with ShowTargetMarker or a live UnitAreaTransition, and hides it otherwise.
    //   UnitEntityView.OnMovementStarted: AddPointer for a directly controllable unit whose current move asks for a
    //       marker or that holds a UnitAreaTransition. The unit's own stock agent calls it when a path it requested
    //       completes.
    //   UnitCommand.TickApproaching -> UnitEntityView.MoveTo -> AgentASP.PathTo: a command that is not close enough is
    //       planned by the stock agent whether or not that agent is enabled. So a mounted rider, which cannot walk,
    //       still gets its own pointer placed for any command it holds itself.
    // The model is a reading of the native code, not native evidence. SprintThree = false keeps the behaviours this
    // sprint changed as they were on the reviewed base.
    internal static class MountedDestinationMarkerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues M04: one routed ground click shows one marker, the mount's, at the mount's destination", RoutedClickShowsOneMarker);
            runner.Run("known issues M04: rider and mount both selected still yield one order and one marker", DuplicateMountOrderSuppressed);
            runner.Run("known issues M04: arrival, Stop and replacement leave no marker behind for the ended order", EndedOrdersLeaveNoMarker);
            runner.Run("known issues M04: a blocked arrival keeps the mount's marker while its order is live, and only that marker", BlockedArrivalKeepsLegitimateMarker);
            runner.Run("known issues M04: a rider-held order that bypassed routing keeps the marker native places for it (not repaired, reported by the observer)", BypassedRiderOrderKeepsNativeMarker);
            runner.Run("known issues M04: a carried entrance approach shows the mount's marker only (the rider's second marker on the reviewed base)", EntranceApproachShowsOneMarkerForThePair);
            runner.Run("known issues M04: a pointer the rider had from an earlier walk is retired when the mount takes the entrance order", EarlierRiderPointerRetired);
            runner.Run("known issues M04: a rider command the mount does not carry keeps what native shows for it", UncarriedRiderCommandUntouched);
            runner.Run("known issues M04: the rider's placement is skipped only while the mount executes that command's carrier", SkipEndsWithTheCarrier);
            runner.Run("known issues M04: the scene regroup walk asks for no marker (the native default showed one on the reviewed base)", RegroupWalkShowsNoMarker);
            runner.Run("known issues M04: no other unit's pointer is ever skipped or retired", OtherPointersUntouched);
        }

        private static void RoutedClickShowsOneMarker()
        {
            var world = new World();
            world.ClickGround(40f, world.Rider, world.Companion);
            world.Frame();
            TestRunner.Equal(2, world.Visible().Count, "A routed click with one companion should show two markers.");
            TestRunner.True(world.MarkerAt(world.Mount) == 40f && world.IsVisible(world.Mount), "The pair's marker is not the mount's, at the mount's destination.");
            TestRunner.True(!world.HasPointer(world.Rider), "The carried rider gained a pointer of its own.");
            TestRunner.True(world.Rider.Slot == null, "The rider holds a command after a routed click.");
        }

        private static void DuplicateMountOrderSuppressed()
        {
            var world = new World();
            world.ClickGround(40f, world.Rider, world.Mount);
            world.Frame();
            TestRunner.Equal(1, world.Orders, "Rider and mount both selected should run one order.");
            TestRunner.Equal(1, world.Visible().Count, "Rider and mount both selected should show one marker.");
        }

        private static void EndedOrdersLeaveNoMarker()
        {
            // Arrival.
            var world = new World();
            world.ClickGround(40f, world.Rider);
            world.Frame();
            world.Arrive(world.Mount);
            world.Frame();
            TestRunner.Equal(0, world.Visible().Count, "A marker stayed after the mount arrived.");
            TestRunner.Equal(MountedPlayerMoveOutcome.CompletedNotHeld, world.ObserveWatch(), "The arrived order was not settled as completed.");

            // Stop: the forwarded Stop interrupts the mount's move.
            world = new World();
            world.ClickGround(40f, world.Rider);
            world.Frame();
            world.Interrupt(world.Mount);
            world.Frame();
            TestRunner.Equal(0, world.Visible().Count, "A marker stayed after Stop.");
            TestRunner.Equal(MountedPlayerMoveOutcome.Interrupted, world.ObserveWatch(), "The stopped order was not settled as interrupted.");

            // Replacement: one marker, moved to the new destination; the old order is ended, not left live.
            world = new World();
            world.ClickGround(40f, world.Rider);
            world.Frame();
            var old = world.Mount.Slot;
            world.ClickGround(70f, world.Rider);
            world.Frame();
            TestRunner.True(old.Finished && !ReferenceEquals(old, world.Mount.Slot), "The replaced order was left live.");
            TestRunner.Equal(1, world.Visible().Count, "Replacement left two markers.");
            TestRunner.True(world.MarkerAt(world.Mount) == 70f, "The marker did not move to the replacing order's destination.");
            TestRunner.True(!world.HasPointer(world.Rider), "Replacement gave the rider a pointer.");
        }

        private static void BlockedArrivalKeepsLegitimateMarker()
        {
            var world = new World();
            world.ClickGround(40f, world.Rider, world.Companion);
            world.Frame();
            world.Arrive(world.Companion);
            // The mount stops short and its order stays live: native keeps that marker, and it is the only one.
            for (var i = 0; i < 5; i++) world.Frame();
            TestRunner.True(world.IsVisible(world.Mount) && !world.Mount.Slot.Finished, "A live order's marker was hidden.");
            TestRunner.Equal(1, world.Visible().Count, "A blocked arrival shows more than the mount's own marker.");
            TestRunner.Equal(MountedPlayerMoveOutcome.Pending, world.ObserveWatch(), "A live blocked order was settled.");
            // It ends only when the order does.
            world.Interrupt(world.Mount);
            world.Frame();
            TestRunner.Equal(0, world.Visible().Count, "The marker outlived the blocked order.");
        }

        private static void BypassedRiderOrderKeepsNativeMarker()
        {
            // An order that reached the mounted rider's own slot without routing. The rider cannot walk it, but its
            // stock agent plans it, so native places and keeps the rider's marker for as long as the order is live.
            var world = new World();
            world.RunOnRiderUnrouted(60f);
            for (var i = 0; i < 5; i++) world.Frame();
            TestRunner.True(world.IsVisible(world.Rider) && world.MarkerAt(world.Rider) == 60f, "The marker of an order nothing carries was hidden.");
            TestRunner.True(!world.Rider.Slot.Finished, "The stuck order was ended or completed for the rider.");
            TestRunner.Equal(0, world.Skipped + world.Retired, "A pointer was skipped or retired for an order nothing carries.");
            TestRunner.True(!MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(true, true, false), "The rider's placement is skipped without a carrier.");
        }

        private static void EntranceApproachShowsOneMarkerForThePair()
        {
            foreach (var sprintThree in new[] { false, true })
            {
                var world = new World { SprintThree = sprintThree };
                world.ClickEntrance(90f, world.Rider, world.Companion);
                world.Frame();
                TestRunner.True(world.Rider.Slot != null && world.Rider.Slot.Transition && !world.Rider.Slot.Finished, "The rider's own member command was not left live.");
                TestRunner.True(world.IsVisible(world.Mount) && world.MarkerAt(world.Mount) == 90f, "The mount's entrance marker is missing.");
                if (sprintThree)
                {
                    TestRunner.True(!world.HasPointer(world.Rider), "The carried rider's own plan still placed a second marker.");
                    TestRunner.Equal(2, world.Visible().Count, "The entrance approach shows a marker beyond the mount's and the companion's.");
                    TestRunner.Equal(1, world.Skipped, "The rider's placement was not skipped exactly once.");
                }
                else
                {
                    TestRunner.True(world.IsVisible(world.Rider), "On the reviewed base the rider's own plan should place a second marker for the pair.");
                    TestRunner.Equal(3, world.Visible().Count, "On the reviewed base the entrance approach should show the extra marker.");
                }
                // The commands are untouched either way, and they finish natively.
                world.Arrive(world.Mount); world.Arrive(world.Rider); world.Arrive(world.Companion);
                world.Frame();
                TestRunner.Equal(0, world.Visible().Count, "A marker outlived the entrance commands.");
            }
        }

        private static void EarlierRiderPointerRetired()
        {
            var world = new World();
            world.WalkOnFoot(world.Rider, 5f);
            world.ClickEntrance(90f, world.Rider, world.Companion);
            world.Frame();
            TestRunner.True(!world.HasPointer(world.Rider), "The rider's earlier pointer was left to be shown at its old place during the approach.");
            TestRunner.Equal(1, world.Retired, "The earlier pointer was not retired exactly once.");
            TestRunner.Equal(2, world.Visible().Count, "The approach shows a marker beyond the mount's and the companion's.");

            // After a dismount the rider's own agent places a new pointer as natively.
            world.Arrive(world.Mount); world.Arrive(world.Rider); world.Arrive(world.Companion);
            world.Mounted = false;
            world.ClickGround(20f, world.Rider);
            world.Frame();
            TestRunner.True(world.IsVisible(world.Rider) && world.MarkerAt(world.Rider) == 20f, "A dismounted rider no longer receives its own native marker.");

            TestRunner.True(MountedDestinationMarkerPolicy.RetireRiderPointer(true, true) && !MountedDestinationMarkerPolicy.RetireRiderPointer(true, false) &&
                !MountedDestinationMarkerPolicy.RetireRiderPointer(false, true), "The retirement decision is not the admitted carrier and an existing pointer.");
        }

        private static void UncarriedRiderCommandUntouched()
        {
            // The mount is not a member of the group: no carrier, so the rider's command keeps the marker native
            // places for it, and an earlier pointer is moved by native, not retired.
            var world = new World { MountIsGroupMember = false };
            world.WalkOnFoot(world.Rider, 5f);
            world.ClickEntrance(90f, world.Rider);
            world.Frame();
            TestRunner.True(world.Mount.Slot == null, "A carrier was issued to a mount outside the group.");
            TestRunner.True(world.IsVisible(world.Rider) && world.MarkerAt(world.Rider) == 90f, "The marker of a rider command nothing carries was skipped or hidden.");
            TestRunner.Equal(0, world.Skipped + world.Retired, "A pointer was skipped or retired without an admitted carrier.");
        }

        private static void SkipEndsWithTheCarrier()
        {
            // The carrier ends (interrupted) while the rider's command is still live: the next plan is native again.
            var world = new World();
            world.ClickEntrance(90f, world.Rider);
            world.Frame();
            TestRunner.True(!world.HasPointer(world.Rider), "Setup: the carried rider should have no pointer.");
            world.Interrupt(world.Mount);
            world.Replan(world.Rider);
            world.Frame();
            TestRunner.True(world.IsVisible(world.Rider), "The rider's placement stayed skipped after its carrier ended.");

            // Another group's member command in the mount's slot is not this rider command's carrier.
            world = new World();
            world.ClickEntrance(90f, world.Rider);
            world.Mount.Slot = new Order { Destination = 90f, Transition = true, Group = new object() };
            world.Replan(world.Rider);
            TestRunner.True(world.HasPointer(world.Rider), "A foreign member command suppressed the rider's placement.");

            // A later pair generation does not inherit the earlier carrier.
            world = new World();
            world.ClickEntrance(90f, world.Rider);
            world.Generation++;
            world.Replan(world.Rider);
            TestRunner.True(world.HasPointer(world.Rider), "A remounted pair inherited the earlier carrier's authority.");

            TestRunner.True(MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(true, true, true) &&
                !MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(false, true, true) &&
                !MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(true, false, true) &&
                !MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(true, true, false), "The skip decision is not the mounted pair's rider with a live carrier.");
        }

        private static void RegroupWalkShowsNoMarker()
        {
            foreach (var sprintThree in new[] { false, true })
            {
                var world = new World { SprintThree = sprintThree };
                world.Regroup(12f);
                world.Frame();
                if (sprintThree) TestRunner.Equal(0, world.Visible().Count, "The regroup walk, which no player ordered, shows a destination marker.");
                else TestRunner.True(world.IsVisible(world.Mount) && world.MarkerAt(world.Mount) == 12f, "On the reviewed base the regroup walk should show a marker under the rider.");
                TestRunner.True(world.Mount.Slot != null && !world.Mount.Slot.Finished, "The regroup walk itself was not issued.");
            }
            TestRunner.True(!MountedDestinationMarkerPolicy.UnorderedMountMoveShowsMarker, "A move no player ordered asks for a marker.");
        }

        private static void OtherPointersUntouched()
        {
            var world = new World();
            world.WalkOnFoot(world.Rider, 5f);
            world.WalkOnFoot(world.Companion, 6f);
            world.WalkOnFoot(world.Mount, 7f);
            world.ClickEntrance(90f, world.Rider, world.Companion);
            world.Frame();
            TestRunner.True(world.HasPointer(world.Companion) && world.HasPointer(world.Mount) && !world.HasPointer(world.Rider),
                "A pointer other than the carried rider's was skipped or retired.");
            TestRunner.True(world.MarkerAt(world.Companion) == 90f && world.MarkerAt(world.Mount) == 90f, "A live order's marker was not placed by its own unit.");
            TestRunner.True(!MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(true, false, true), "A unit that is not the rider had its placement skipped.");
        }

        // ---- model -------------------------------------------------------------------------------------------------

        private sealed class Order
        {
            internal float Destination;
            internal bool Transition, Finished, Acted, ShowMarker = true;
            internal object Group;
        }

        private sealed class ModelUnit
        {
            internal string Name;
            internal Order Slot;   // the native Move slot
        }

        private sealed class Pointer { internal float At; internal bool Visible; }

        private sealed class World
        {
            private readonly Dictionary<ModelUnit, Pointer> marks = new Dictionary<ModelUnit, Pointer>();
            private readonly MountedPlayerMoveWatch watch = new MountedPlayerMoveWatch();
            private readonly MountedEntranceCarrierWatch carrier = new MountedEntranceCarrierWatch();
            internal readonly ModelUnit Rider = new ModelUnit { Name = "rider" }, Mount = new ModelUnit { Name = "mount" }, Companion = new ModelUnit { Name = "companion" };
            internal bool Mounted = true, SprintThree = true, MountIsGroupMember = true;
            internal long Generation = 1;
            internal int Orders, Retired, Skipped;

            // The production classification of the mount's Move-slot command, as SkipCarriedRiderPointer reads it.
            private bool MountExecutesCarrierOfRidersCommand()
            {
                var order = Mount.Slot;
                var member = Rider.Slot != null && Rider.Slot.Transition ? Rider.Slot : null;
                if (order == null) return false;
                return MountedGroupPacePolicy.ClassifyOrder(true, true, false, false, false, order.Transition,
                    order.Transition && carrier.IsCurrent(order, order.Group, Generation),
                    order.Transition && member != null && !member.Finished && ReferenceEquals(member.Group, order.Group)) == MountedGroupPaceOrder.CarriedEntranceOrder &&
                    !order.Finished;
            }

            // A completed plan of the unit's own stock agent: UnitMovementAgent.OnPathComplete ->
            // UnitEntityView.OnMovementStarted -> ClickPointerManager.AddPointer. The agent plans whether or not it is
            // enabled, so this also runs for a mounted rider that holds a command of its own.
            private void PathCompleted(ModelUnit unit)
            {
                var order = unit.Slot;
                if (order == null || order.Finished) return;
                if (SprintThree && MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(Mounted, ReferenceEquals(unit, Rider),
                        ReferenceEquals(unit, Rider) && MountExecutesCarrierOfRidersCommand()))
                {
                    Skipped++;
                    return;
                }
                if (!(order.Transition || order.ShowMarker)) return;
                if (!marks.TryGetValue(unit, out var pointer)) marks[unit] = pointer = new Pointer();
                pointer.At = order.Destination;
                pointer.Visible = true;
            }

            // The unit's command is planned again (native re-requests a path while the command is not close enough).
            internal void Replan(ModelUnit unit) => PathCompleted(unit);

            // ClickPointerManager.Update.
            internal void Frame()
            {
                foreach (var mark in marks)
                {
                    var order = mark.Key.Slot;
                    var move = order != null && !order.Transition ? order : null;
                    var transition = order != null && order.Transition ? order : null;
                    mark.Value.Visible = !((move == null || move.Finished || !move.ShowMarker) && (transition == null || transition.Finished));
                }
            }

            internal List<ModelUnit> Visible() => marks.Where(m => m.Value.Visible).Select(m => m.Key).ToList();
            internal bool HasPointer(ModelUnit unit) => marks.ContainsKey(unit);
            internal bool IsVisible(ModelUnit unit) => marks.TryGetValue(unit, out var pointer) && pointer.Visible;
            internal float MarkerAt(ModelUnit unit) => marks.TryGetValue(unit, out var pointer) ? pointer.At : float.NaN;

            // Native Run: the unit's earlier command in the slot is interrupted. The plan completes afterwards.
            private Order Run(ModelUnit unit, Order order)
            {
                if (unit.Slot != null && !unit.Slot.Finished) unit.Slot.Finished = true;
                unit.Slot = order;
                Orders++;
                return order;
            }

            internal void WalkOnFoot(ModelUnit unit, float destination)
            {
                var mounted = Mounted;
                Mounted = false;
                Run(unit, new Order { Destination = destination });
                PathCompleted(unit);
                Arrive(unit);
                Frame();
                Mounted = mounted;
            }

            internal void RunOnRiderUnrouted(float destination)
            {
                Run(Rider, new Order { Destination = destination });
                PathCompleted(Rider);
            }

            internal void ClickGround(float destination, params ModelUnit[] selected)
            {
                var ran = new List<ModelUnit>();
                foreach (var unit in selected)
                {
                    var route = CommandRouter.RouteGroundMove(Mounted ? new MountedPair(Rider.Name, Mount.Name) : null, unit.Name);
                    if (route.Action == CommandRoutingAction.SuppressDuplicateMount) continue;
                    var executor = route.Action == CommandRoutingAction.RewriteRiderToMount ? Mount : unit;
                    var order = Run(executor, new Order { Destination = destination });
                    if (route.Action == CommandRoutingAction.RewriteRiderToMount) watch.Bind(order, 1);
                    ran.Add(executor);
                }
                foreach (var unit in ran) PathCompleted(unit);
            }

            internal void ClickEntrance(float destination, params ModelUnit[] selected)
            {
                var group = new object();
                var ran = new List<ModelUnit>();
                foreach (var unit in selected)
                {
                    Run(unit, new Order { Destination = destination, Transition = true, Group = group });
                    ran.Add(unit);
                    if (!ReferenceEquals(unit, Rider)) continue;
                    // The entrance carry, from the Run postfix of the rider's own member command.
                    var decision = MountedEntranceCarryPolicy.Decide(Mounted, true, true, false, true, true, MountIsGroupMember,
                        Mount.Slot != null && Mount.Slot.Transition && !Mount.Slot.Finished && ReferenceEquals(Mount.Slot.Group, group));
                    var admitted = decision == MountedEntranceCarryDecision.Carry;
                    if (admitted)
                    {
                        var carried = Run(Mount, new Order { Destination = destination, Transition = true, Group = group });
                        carrier.Bind(carried, group, Generation);
                        ran.Add(Mount);
                    }
                    if (SprintThree && MountedDestinationMarkerPolicy.RetireRiderPointer(admitted, marks.ContainsKey(Rider)))
                    {
                        marks.Remove(Rider);
                        Retired++;
                    }
                }
                // The plans complete after the click, the rider's own included.
                foreach (var unit in ran) PathCompleted(unit);
            }

            // The scene regroup walk: a move the pair gives its mount for no player order.
            internal void Regroup(float destination)
            {
                Run(Mount, new Order { Destination = destination, ShowMarker = SprintThree ? MountedDestinationMarkerPolicy.UnorderedMountMoveShowsMarker : true });
                PathCompleted(Mount);
            }

            internal void Arrive(ModelUnit unit)
            {
                if (unit.Slot == null) return;
                unit.Slot.Acted = true;
                unit.Slot.Finished = true;
            }

            internal void Interrupt(ModelUnit unit)
            {
                if (unit.Slot != null) unit.Slot.Finished = true;
            }

            internal MountedPlayerMoveOutcome ObserveWatch()
            {
                var order = watch.Move as Order;
                if (order == null) return MountedPlayerMoveOutcome.None;
                return watch.Observe(1, Mounted, order.Finished, order.Acted, false, ReferenceEquals(Mount.Slot, order));
            }
        }
    }
}
