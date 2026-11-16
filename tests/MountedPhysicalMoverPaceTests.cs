using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues sprint 3 (M01, M02, M03). The production decisions are MountedGroupPacePolicy, MountedPaceActors,
    // MountedEntranceCarrierWatch, MountedEntranceCarryPolicy and CommandRouter. They are driven through a small model
    // of the installed native contracts (decompiled Kingmaker 2.1.7b, recorded as a summary):
    //   ClickGroundHandler.MoveSelectedUnitsToPoint: for more than one selected unit, SpeedLimit =
    //       max(min over the selected units of ModifiedSpeedMps, 30 ft per round = 3.048 m/s); otherwise no limit. The
    //       entrance click uses the same method with a runner that gives each selected unit a UnitAreaTransition.
    //   UnitMovementAgent.OnPathComplete: AccelerateIfPathIsLong on every command of the unit whose path completed.
    //   UnitCommand.AccelerateIfPathIsLong: at 12.7 m or more the trigger accelerates, then Accelerate() for every
    //       command in the slots of every selected unit.
    //   UnitCommand.Accelerate/CanAccelerate/Deaccelerate: as MountedGroupPacePolicyTests records them.
    //   InGameInputLayer.UpdateMovement: the stick-driven unit's continuous command accelerates at magnitude 0.95 or
    //       more and decelerates below it; at each repath the followers are re-ordered through
    //       MoveSelectedUnitsToPoint and every selected unit's Commands.Move then receives Accelerate() or
    //       Deaccelerate() according to the driver's command.
    // The model is a reading of the native code, not native evidence. SprintThree = false runs the same model with the
    // classification, follower list and speed inputs of the reviewed base, which is how each defect is shown here.
    // Those assertions, and the ones about the model's own native refusals, describe the model: no production change
    // can fail them. The failing-before evidence is the mutation runs and the reviewed-base build (sprint handoff).
    // The integration methods that gather these facts from the game (ClassifyMountGroupOrder and its callers) are
    // not executed here; scripts/Test-PhysicalMoverPaceContracts.ps1 reads their compiled wiring.
    internal static class MountedPhysicalMoverPaceTests
    {
        private const float Floor = 30f * 0.3048f / 3f;

        private static float Mps(float feetPerRound) => feetPerRound * 0.3048f / 3f;

        public static void Register(TestRunner runner)
        {
            runner.Run("known issues M01: a long entrance order accelerates the mount's carried member command with the party", EntranceOrderJoinsAcceleratedGroup);
            runner.Run("known issues M01: with the classification of the reviewed base the carried entrance command keeps the group walk", EntranceDefectOnReviewedBase);
            runner.Run("known issues M01: accelerated, finished, replaced, foreign and unselected entrance orders are untouched", EntranceOrdersLeftAlone);
            runner.Run("known issues M01: a short entrance path and every native refusal keep their behaviour", EntranceNativeRefusalsKept);
            runner.Run("known issues M01: repeated offers accelerate the carried command once", EntranceOfferNeverMultiplies);
            runner.Run("known issues M01: approaches, AI orders, foreign executors and turn-based play are never classified as the rider's order", OnlyTheRidersOrderIsClassified);
            runner.Run("known issues M01: the carrier watch matches one command, one group and one pair generation", CarrierWatchIdentity);
            runner.Run("known issues M01: an ordinary routed ground order is still carried", RoutedGroundOrderStillCarried);
            runner.Run("known issues M02: a held stick accelerates the mount executing the following rider's routed order", GamepadFollowerReachesMount);
            runner.Run("known issues M02: a slackened stick decelerates that order, and a released stick offers nothing", GamepadSlackAndRelease);
            runner.Run("known issues M02: replacement, selection change, dismount and combat retire the old order's authority", GamepadAuthorityRetires);
            runner.Run("known issues M02: two mounted pairs answer for their own riders only", GamepadTwoPairs);
            runner.Run("known issues M02: unmounted units and a selected mount are walked as native had them", GamepadNativeListKept);
            runner.Run("known issues M02: repeated passes never multiply the limit", GamepadNeverMultiplies);
            runner.Run("known issues M03: a fast rider on a slow horse paces the group by the horse", FastRiderSlowHorse);
            runner.Run("known issues M03: a slow rider on a fast horse no longer slows the group", SlowRiderFastHorse);
            runner.Run("known issues M03: rider-only and mount-only selections keep the native single-unit order", SingleSelectionsKeepNoLimit);
            runner.Run("known issues M03: rider and mount both selected count the mount once", RiderAndMountBothSelected);
            runner.Run("known issues M03: two mounted pairs and a companion are paced by the slowest physical mover", TwoPairsAndCompanion);
            runner.Run("known issues M03: an unmounted selection and turn-based play read the native inputs", NativeInputsKept);
            runner.Run("known issues M03: a condition on the mount changes the limit, and the native floor still applies", MountConditionAndFloor);
            runner.Run("known issues M03: the entrance order and the gamepad follow order take the same limit", SharedLimitSites);
        }

        // ---- M01 ---------------------------------------------------------------------------------------------------

        private static void EntranceOrderJoinsAcceleratedGroup()
        {
            var world = World.Party();
            world.ClickEntrance();
            var carrier = world.Horse.Slot;
            TestRunner.True(carrier != null && carrier.Kind == Kind.Entrance && !ReferenceEquals(carrier, world.Rider.Slot),
                "The entrance carry did not give the mount its own member command.");
            TestRunner.True(!carrier.Accelerated, "The carried command was accelerated before any native offer ran.");
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(carrier.Accelerated, "The carried entrance command did not receive the native group offer.");
            TestRunner.Equal(Gait.Charge, carrier.Gait, "The carried command did not take the native run gait.");
            TestRunner.Equal(world.Unit("Companion A").Slot.SpeedLimit, carrier.SpeedLimit, "The mount's entrance limit differs from its party's.");
            TestRunner.True(Math.Abs(carrier.SpeedLimit.Value - Floor * 1.8f) < 0.001f, "The entrance limit is not the native 1.8 x the group limit.");
            TestRunner.True(world.Rider.Slot.Accelerated, "The rider's own member command lost the native offer it always received.");
            TestRunner.True(ReferenceEquals(world.Rider.Slot.Group, carrier.Group) && !world.Rider.Slot.Finished && !carrier.Finished,
                "Group membership or completion of the entrance commands changed.");
            TestRunner.Equal(1, world.Offers(world.Pair), "The carry did not run exactly once.");
            TestRunner.True(Math.Abs(world.CurrentSpeed(world.Horse) - world.CurrentSpeed(world.Unit("Companion A"))) < 0.001f,
                "The mount's entrance pace differs from the party's.");
        }

        private static void EntranceDefectOnReviewedBase()
        {
            var world = World.Party();
            world.SprintThree = false;
            world.ClickEntrance();
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!world.Horse.Slot.Accelerated, "The reviewed classification should leave the carried entrance command unaccelerated.");
            TestRunner.True(Math.Abs(world.CurrentSpeed(world.Horse) - Floor) < 0.001f, "The uncarried mount should walk to the entrance at the group limit.");
            TestRunner.True(world.CurrentSpeed(world.Unit("Companion A")) > 1.7f * world.CurrentSpeed(world.Horse), "The party should outrun the uncarried mount.");
        }

        private static void EntranceOrdersLeftAlone()
        {
            // Already accelerated by its own long path: the later offer of another member changes nothing.
            var world = World.Party();
            world.ClickEntrance();
            world.PathCompletes(world.Horse, 60f);
            var carrier = world.Horse.Slot;
            var limit = carrier.SpeedLimit;
            TestRunner.True(carrier.Accelerated, "The mount's own long path no longer accelerates its member command natively.");
            world.PathCompletes(world.Unit("Companion B"), 60f);
            TestRunner.Equal(limit, carrier.SpeedLimit, "An accelerated carried command was offered again.");
            TestRunner.Equal(0, world.Offers(world.Pair), "The carry ran for an already accelerated command.");

            // Finished.
            world = World.Party();
            world.ClickEntrance();
            world.Horse.Slot.Finished = true;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0, "A finished carried command was touched.");

            // Replaced: the mount now holds another command; the old carrier is no longer in its slot.
            world = World.Party();
            world.ClickEntrance();
            var replaced = world.Horse.Slot;
            world.Horse.Slot = new Order { Kind = Kind.Entrance, Group = replaced.Group, Executor = world.Horse, SpeedLimit = Floor };
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!replaced.Accelerated && !world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0,
                "A replaced carrier, or the unbound command that replaced it, was accelerated.");

            // Foreign: a member command of another group, though the carrier of the rider's group was bound earlier.
            world = World.Party();
            world.ClickEntrance();
            var foreign = new Order { Kind = Kind.Entrance, Group = new object(), Executor = world.Horse, SpeedLimit = Floor };
            world.Horse.Slot = foreign;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!foreign.Accelerated && world.Offers(world.Pair) == 0, "A foreign entrance command was accelerated.");

            // The rider's own member command ended or was replaced: no order of the rider is left for the offer.
            world = World.Party();
            world.ClickEntrance();
            world.Rider.Slot.Finished = true;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0, "The carrier was accelerated for a rider order that had ended.");

            // Unselected rider: the native offer reaches selected units only.
            world = World.Party();
            world.ClickEntrance();
            world.Selected.Remove(world.Rider);
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0, "The mount of an unselected rider was accelerated.");

            // A selected mount is reached by the native offer itself, once.
            world = World.Party();
            world.ClickEntrance();
            world.Selected.Add(world.Horse);
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0, "A selected mount was offered by the carry as well as natively.");
        }

        private static void EntranceNativeRefusalsKept()
        {
            var world = World.Party();
            world.ClickEntrance();
            world.PathCompletes(world.Unit("Companion A"), 9f);
            TestRunner.True(world.AllOrders().All(o => !o.Accelerated) && world.Offers(world.Pair) == 0, "A short entrance path accelerated someone or ran the carry.");

            foreach (var refusal in new[] { "setting", "load", "affected", "combat" })
            {
                world = World.Party();
                if (refusal == "setting") world.AcceleratedMoveSetting = false;
                if (refusal == "load") world.HeavyLoad = true;
                if (refusal == "affected") { world.Horse.SpeedAffected = true; world.Horse.ModifiedSpeed = Mps(20f); }
                if (refusal == "combat") world.Horse.InCombat = true;
                world.ClickEntrance();
                world.PathCompletes(world.Unit("Companion A"), 60f);
                TestRunner.True(!world.Horse.Slot.Accelerated, "The carried command was accelerated past the native refusal: " + refusal + ".");
                if (refusal == "affected" || refusal == "combat")
                {
                    TestRunner.Equal(1, world.Offers(world.Pair), "The policy must offer; the native rule is what refuses (" + refusal + ").");
                    TestRunner.True(world.Unit("Companion A").Slot.Accelerated, "Unaffected members lost their native acceleration.");
                }
                if (refusal == "affected")
                    TestRunner.True(Math.Abs(world.CurrentSpeed(world.Horse) - Mps(20f)) < 0.001f, "The mount's own penalty no longer bounds its entrance pace.");
            }
        }

        private static void EntranceOfferNeverMultiplies()
        {
            var world = World.Party();
            world.ClickEntrance();
            world.PathCompletes(world.Unit("Companion A"), 60f);
            var limit = world.Horse.Slot.SpeedLimit;
            // Later long paths of other members: their own commands are accelerated already, so no native offer runs;
            // a member that deaccelerated and re-accelerates offers again, and the carried command is left as it is.
            world.PathCompletes(world.Unit("Companion B"), 60f);
            world.Deaccelerate(world.Unit("Companion B").Slot);
            world.PathCompletes(world.Unit("Companion B"), 60f);
            TestRunner.Equal(limit, world.Horse.Slot.SpeedLimit, "A repeated offer multiplied the carried command's limit.");
            TestRunner.Equal(1, world.Offers(world.Pair), "The carry accelerated the same command more than once.");
        }

        private static void OnlyTheRidersOrderIsClassified()
        {
            MountedGroupPaceOrder Classify(bool executorIsMount = true, bool createdByPlayer = true, bool ai = false, bool exactGround = false,
                bool boundGround = false, bool exactTransition = false, bool boundCarrier = false, bool riderMember = false) =>
                MountedGroupPacePolicy.ClassifyOrder(executorIsMount, createdByPlayer, ai, exactGround, boundGround, exactTransition, boundCarrier, riderMember);

            TestRunner.Equal(MountedGroupPaceOrder.RoutedGroundOrder, Classify(exactGround: true, boundGround: true), "The routed ground order was not classified.");
            TestRunner.Equal(MountedGroupPaceOrder.CarriedEntranceOrder, Classify(exactTransition: true, boundCarrier: true, riderMember: true),
                "The carried entrance command was not classified.");
            // An attack, spell, trap or recovery approach is an exact UnitMoveTo the ground click never bound.
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactGround: true), "An unbound approach move was classified as the rider's order.");
            // A door approach or a charge carrier is a derived or different command type.
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(boundGround: true), "A derived move type was classified as the routed ground order.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactGround: true, boundGround: true, ai: true), "An AI order was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactGround: true, boundGround: true, createdByPlayer: false), "A non-player order was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactGround: true, boundGround: true, executorIsMount: false), "Another executor's command was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactTransition: true, riderMember: true), "An entrance command the carry did not issue was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactTransition: true, boundCarrier: true), "A carrier whose rider holds no member command was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(boundCarrier: true, riderMember: true), "A derived transition type was classified.");
            TestRunner.Equal(MountedGroupPaceOrder.None, Classify(exactTransition: true, boundCarrier: true, riderMember: true, ai: true), "An AI transition was classified.");

            // Turn-based play: movement and its budgets are left alone whichever order the mount holds.
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, MountedGroupPacePolicy.Decide(true, true, true, true, false, true, false, false),
                "Turn-based movement received the entrance or ground carry.");
            var world = World.Party();
            world.ClickEntrance();
            world.TurnBased = true;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.Equal(0, world.Offers(world.Pair), "The carry ran in turn-based combat.");

            // An owned approach in the mount's slot while the party walks to an entrance.
            world = World.Party();
            world.ClickEntrance();
            var approach = new Order { Kind = Kind.Approach, Executor = world.Horse, SpeedLimit = Floor };
            world.Horse.Slot = approach;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!approach.Accelerated && world.Offers(world.Pair) == 0, "An owned approach move was accelerated.");

            // An AI order in the mount's slot, even if it were the bound command.
            world = World.Party();
            world.ClickGround();
            world.Horse.Slot.Ai = true;
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(!world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 0, "An AI order was accelerated.");
        }

        private static void CarrierWatchIdentity()
        {
            var watch = new MountedEntranceCarrierWatch();
            object carrier = new object(), group = new object();
            TestRunner.True(!watch.IsCurrent(carrier, group, 1), "An unbound watch matched a command.");
            watch.Bind(carrier, group, 4);
            TestRunner.True(watch.IsCurrent(carrier, group, 4), "The bound carrier was not recognised.");
            TestRunner.True(!watch.IsCurrent(new object(), group, 4), "Another command of the same group matched.");
            TestRunner.True(!watch.IsCurrent(carrier, new object(), 4), "The carrier matched under another group.");
            TestRunner.True(!watch.IsCurrent(carrier, group, 5), "The carrier matched for a later pair generation.");
            TestRunner.True(!watch.IsCurrent(null, group, 4) && !watch.IsCurrent(carrier, null, 4), "A missing command or group matched.");
            object later = new object(), laterGroup = new object();
            watch.Bind(later, laterGroup, 4);
            TestRunner.True(!watch.IsCurrent(carrier, group, 4) && watch.IsCurrent(later, laterGroup, 4), "A newer carrier did not replace the older one.");
            watch.Clear();
            TestRunner.True(!watch.IsCurrent(later, laterGroup, 4) && watch.Carrier == null && watch.Group == null, "A cleared watch kept its carrier.");

            // The controller's update lets go of a carrier that ended or that an earlier mounted pair held.
            TestRunner.True(!watch.ReleaseIfEnded(true, true, 4), "An empty watch reported a release.");
            watch.Bind(carrier, group, 4);
            TestRunner.True(!watch.ReleaseIfEnded(false, true, 4) && watch.IsCurrent(carrier, group, 4), "A live carrier of the mounted pair was released.");
            TestRunner.True(watch.ReleaseIfEnded(true, true, 4) && watch.Carrier == null && watch.Group == null, "A finished carrier was kept.");
            watch.Bind(carrier, group, 4);
            TestRunner.True(watch.ReleaseIfEnded(false, false, 4) && watch.Carrier == null, "A dismounted pair kept its carrier.");
            watch.Bind(carrier, group, 4);
            TestRunner.True(watch.ReleaseIfEnded(false, true, 5) && watch.Carrier == null, "A later pair generation kept the earlier carrier.");

            // The routed ground order answers for its own generation only.
            var ground = new MountedPlayerMoveWatch();
            var move = new object();
            ground.Bind(move, 4);
            TestRunner.True(ground.IsCurrent(move, 4) && !ground.IsCurrent(move, 5) && !ground.IsCurrent(new object(), 4) && !ground.IsCurrent(null, 4),
                "The watched ground order is not one command of one pair generation.");
        }

        private static void RoutedGroundOrderStillCarried()
        {
            var world = World.Party();
            world.ClickGround();
            TestRunner.True(world.Rider.Slot == null && world.Horse.Slot != null && world.Horse.Slot.Kind == Kind.Ground, "The ground click was not routed to the mount.");
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(world.Horse.Slot.Accelerated && world.Offers(world.Pair) == 1, "The routed ground order lost its carry.");
            TestRunner.Equal(world.Unit("Companion A").Slot.SpeedLimit, world.Horse.Slot.SpeedLimit, "The routed order's limit differs from the party's.");
        }

        // ---- M02 ---------------------------------------------------------------------------------------------------

        private static void GamepadFollowerReachesMount()
        {
            var world = World.Party();
            var driver = world.Unit("Companion A");
            world.StickFollow(driver, 1f);
            TestRunner.True(driver.Slot.Kind == Kind.Continuous && driver.Slot.Accelerated, "The stick-driven unit did not accelerate natively.");
            TestRunner.True(world.Horse.Slot != null && world.Horse.Slot.Kind == Kind.Ground && world.Horse.Slot.Accelerated,
                "The native follower offer did not reach the mount executing the rider's routed order.");
            TestRunner.True(world.Unit("Companion B").Slot.Accelerated, "An ordinary follower lost its native offer.");
            TestRunner.True(world.Rider.Slot == null, "The rider gained a command of its own.");
            TestRunner.Equal(world.Unit("Companion B").Slot.SpeedLimit, world.Horse.Slot.SpeedLimit, "The mount's follow limit differs from the other follower's.");

            var before = World.Party();
            before.SprintThree = false;
            before.StickFollow(before.Unit("Companion A"), 1f);
            TestRunner.True(!before.Horse.Slot.Accelerated && before.Unit("Companion B").Slot.Accelerated,
                "On the reviewed base the follower offer should miss the mount and reach the ordinary follower.");
            TestRunner.True(before.CurrentSpeed(before.Unit("Companion B")) > 1.7f * before.CurrentSpeed(before.Horse), "The party should outrun the uncarried mount.");
        }

        private static void GamepadSlackAndRelease()
        {
            var world = World.Party();
            var driver = world.Unit("Companion A");
            world.StickFollow(driver, 1f);
            var order = world.Horse.Slot;
            TestRunner.True(order.Accelerated, "Setup: the mount's order should be accelerated.");
            // The stick slackens and the pair is not re-ordered in this pass (a held unit keeps its order).
            world.StickFollow(driver, 0.5f, reissue: false);
            TestRunner.True(ReferenceEquals(order, world.Horse.Slot) && !order.Accelerated && order.Gait == Gait.Normal,
                "The native deceleration of the follower pass did not reach the mount's order.");
            TestRunner.True(Math.Abs(order.SpeedLimit.Value - Floor) < 0.001f, "Deceleration did not restore the native group limit.");
            // Released: native runs no pass at all, and the order is left to finish natively.
            world.StickFollow(driver, 1f);
            var held = world.Horse.Slot;
            var passes = world.FollowerPasses;
            world.StickRelease(driver);
            TestRunner.Equal(passes, world.FollowerPasses, "A released stick ran a follower pass.");
            TestRunner.True(ReferenceEquals(held, world.Horse.Slot) && held.Accelerated && !held.Finished, "Release changed the order that was already running.");
        }

        private static void GamepadAuthorityRetires()
        {
            var world = World.Party();
            var driver = world.Unit("Companion A");
            world.StickFollow(driver, 1f);
            var old = world.Horse.Slot;
            var oldLimit = old.SpeedLimit;

            // Replacement: the next repath gives the pair a new routed order; the old one is out of the slot.
            world.StickFollow(driver, 1f);
            TestRunner.True(!ReferenceEquals(old, world.Horse.Slot) && world.Horse.Slot.Accelerated, "The replacing order was not the one accelerated.");
            TestRunner.Equal(oldLimit, old.SpeedLimit, "The replaced order was touched again.");

            // An owned approach replaces the routed order: nothing of the old authority reaches it.
            var approach = new Order { Kind = Kind.Approach, Executor = world.Horse, SpeedLimit = Floor };
            world.Horse.Slot = approach;
            world.StickFollow(driver, 1f, reissue: false);
            TestRunner.True(!approach.Accelerated, "An owned approach inherited the routed order's authority.");

            // Selection change: the rider leaves the linked group.
            world = World.Party();
            driver = world.Unit("Companion A");
            world.StickFollow(driver, 0.5f);
            var unlinked = world.Horse.Slot;
            world.Selected.Remove(world.Rider);
            world.StickFollow(driver, 1f, reissue: false);
            TestRunner.True(!unlinked.Accelerated, "The mount of a rider outside the selection was accelerated.");

            // A newly selected unit inherits nothing: its own slot is what native reads.
            var newcomer = world.Add("Companion C", Mps(30f));
            world.Selected.Add(newcomer);
            world.StickFollow(driver, 1f, reissue: false);
            TestRunner.True(newcomer.Slot == null && !unlinked.Accelerated, "A newly selected unit, or the unlinked mount, received the old order's offer.");

            // Dismount: the rider is itself again; the horse's old order is not answered for.
            world = World.Party();
            driver = world.Unit("Companion A");
            world.StickFollow(driver, 0.5f);
            var afterDismount = world.Horse.Slot;
            world.Pair.Mounted = false;
            world.StickFollow(driver, 1f, reissue: false);
            TestRunner.True(!afterDismount.Accelerated, "A dismounted pair's horse was accelerated through its former rider.");

            // A pair that mounts again holds a new generation; the order watched for the earlier one is still in the
            // mount's slot and is no longer answered for, before any update has settled the watch.
            world.Pair.Mounted = true;
            world.Pair.Generation++;
            TestRunner.True(ReferenceEquals(world.Pair.Watch.Move, afterDismount) && ReferenceEquals(world.Horse.Slot, afterDismount),
                "Setup: the earlier order should still be watched and in the mount's slot.");
            world.StickFollow(driver, 1f, reissue: false);
            world.PathCompletes(world.Unit("Companion B"), 60f);
            TestRunner.True(!afterDismount.Accelerated && world.Offers(world.Pair) == 0, "A newly mounted pair inherited an order from before it mounted.");

            // Turn-based combat: the follower list is native. (The native pass itself returns before this loop whenever
            // the party is in combat, real time included; that gate is native and is not modelled here.)
            world = World.Party();
            world.StickFollow(world.Unit("Companion A"), 0.5f);
            world.TurnBased = true;
            TestRunner.True(world.OfferUnits().SequenceEqual(world.Selected), "The follower list was changed in turn-based combat.");
        }

        private static void GamepadTwoPairs()
        {
            var world = World.Party();
            var second = world.AddPair("Rider 2", Mps(30f), "Horse 2", Mps(50f));
            var driver = world.Unit("Companion A");
            world.StickFollow(driver, 1f);
            TestRunner.True(world.Horse.Slot.Accelerated && second.Mount.Slot.Accelerated, "A following pair's mount missed the offer.");
            var listed = world.OfferUnits();
            TestRunner.Equal(1, listed.Count(u => ReferenceEquals(u, world.Horse)), "The first mount is not listed exactly once.");
            TestRunner.Equal(1, listed.Count(u => ReferenceEquals(u, second.Mount)), "The second mount is not listed exactly once.");
            TestRunner.True(!listed.Contains(world.Rider) && !listed.Contains(second.Rider), "A carried rider was still walked.");

            // The second pair's order ends; the first pair is unaffected and the second rider is itself again.
            second.Mount.Slot.Finished = true;
            listed = world.OfferUnits();
            TestRunner.True(listed.Contains(second.Rider) && !listed.Contains(second.Mount) && listed.Contains(world.Horse),
                "One pair's finished order changed the other pair's answer.");
            // One pair's rider unselected: only that pair is left out.
            world.Selected.Remove(world.Rider);
            world.StickFollow(driver, 0.5f, reissue: false);
            TestRunner.True(world.Horse.Slot.Accelerated, "A pair outside the selection was decelerated by another pair's pass.");
        }

        private static void GamepadNativeListKept()
        {
            // No pair: the list holds the same instances in the same order.
            var world = World.Party();
            world.Pair.Mounted = false;
            TestRunner.True(world.OfferUnits().SequenceEqual(world.Selected), "An unmounted selection was changed.");

            // A selected mount (the console group can hold both): native reaches it itself, and the rider is kept.
            world = World.Party();
            world.Selected.Add(world.Horse);
            world.StickFollow(world.Unit("Companion A"), 1f);
            TestRunner.True(world.OfferUnits().SequenceEqual(world.Selected), "A selection holding the mount was changed.");
            TestRunner.True(world.Horse.Slot.Accelerated, "A selected mount lost its native follower offer.");

            // A null entry is kept where native had it.
            world = World.Party();
            world.Selected.Insert(1, null);
            var listed = world.OfferUnits();
            TestRunner.True(listed.Count == world.Selected.Count && listed[1] == null, "A null selection entry was dropped or moved.");

            // The mouse path is the AccelerateIfPathIsLong carry, which the gamepad list does not touch.
            world = World.Party();
            world.ClickGround();
            world.PathCompletes(world.Unit("Companion A"), 60f);
            TestRunner.True(world.Horse.Slot.Accelerated && world.FollowerPasses == 0, "Mouse movement depended on the gamepad follower pass.");
        }

        private static void GamepadNeverMultiplies()
        {
            var world = World.Party();
            var driver = world.Unit("Companion A");
            world.StickFollow(driver, 1f);
            var order = world.Horse.Slot;
            var limit = order.SpeedLimit;
            for (var i = 0; i < 5; i++) world.StickFollow(driver, 1f, reissue: false);
            TestRunner.True(ReferenceEquals(order, world.Horse.Slot), "The order was replaced without a re-order.");
            TestRunner.Equal(limit, order.SpeedLimit, "Repeated follower passes multiplied the limit.");
            TestRunner.True(Math.Abs(limit.Value - Floor * 1.8f) < 0.001f, "The accelerated limit is not the native 1.8 x the group limit.");
            // A long path completing as well: still once.
            world.PathCompletes(world.Unit("Companion B"), 60f);
            TestRunner.Equal(limit, order.SpeedLimit, "The mouse-path carry multiplied an order the gamepad pass had accelerated.");
        }

        // ---- M03 ---------------------------------------------------------------------------------------------------

        private static World Speeds(float riderFeet, float horseFeet, float companionFeet)
        {
            var world = World.Party();
            world.Rider.ModifiedSpeed = Mps(riderFeet);
            world.Horse.ModifiedSpeed = Mps(horseFeet);
            world.Unit("Companion A").ModifiedSpeed = Mps(companionFeet);
            world.Unit("Companion B").ModifiedSpeed = Mps(companionFeet);
            return world;
        }

        private static void Near(float expected, float? actual, string message) =>
            TestRunner.True(actual.HasValue && Math.Abs(expected - actual.Value) < 0.001f, message + " Expected " + expected + ", got " + (actual.HasValue ? actual.Value.ToString() : "none") + ".");

        private static void FastRiderSlowHorse()
        {
            var world = Speeds(40f, 35f, 40f);
            Near(Mps(35f), world.GroupLimit(), "The group limit ignores the slower horse that carries the fast rider.");
            world.ClickGround();
            TestRunner.True(Math.Abs(world.CurrentSpeed(world.Horse) - world.CurrentSpeed(world.Unit("Companion A"))) < 0.001f, "The party no longer keeps pace with the horse.");

            var before = Speeds(40f, 35f, 40f);
            before.SprintThree = false;
            Near(Mps(40f), before.GroupLimit(), "On the reviewed base the limit should come from the rider.");
            before.ClickGround();
            TestRunner.True(before.CurrentSpeed(before.Unit("Companion A")) > before.CurrentSpeed(before.Horse) + 0.4f, "On the reviewed base the party should leave the horse behind.");
        }

        private static void SlowRiderFastHorse()
        {
            var world = Speeds(20f, 50f, 40f);
            Near(Mps(40f), world.GroupLimit(), "A slow rider still lowers the limit of a group whose movers are all faster.");
            var before = Speeds(20f, 50f, 40f);
            before.SprintThree = false;
            Near(Floor, before.GroupLimit(), "On the reviewed base the rider's speed, floored natively, should set the limit.");
        }

        private static void SingleSelectionsKeepNoLimit()
        {
            var world = Speeds(20f, 50f, 30f);
            world.Selected.Clear(); world.Selected.Add(world.Rider);
            TestRunner.True(world.GroupLimit() == null, "A rider-only selection received a group limit.");
            world.ClickGround();
            TestRunner.True(world.Horse.Slot != null && world.Horse.Slot.SpeedLimit == null, "The single routed order carries a limit.");
            Near(Mps(50f), world.CurrentSpeed(world.Horse), "A lone pair does not travel at the horse's own speed.");

            world = Speeds(20f, 50f, 30f);
            world.Selected.Clear(); world.Selected.Add(world.Horse);
            TestRunner.True(world.GroupLimit() == null, "A mount-only selection received a group limit.");
            // Two ordinary units are a group; one is not.
            world = Speeds(30f, 40f, 40f);
            world.Selected.Clear(); world.Selected.Add(world.Unit("Companion A"));
            TestRunner.True(world.GroupLimit() == null, "A single ordinary unit received a group limit.");
            world.Selected.Add(world.Unit("Companion B"));
            Near(Mps(40f), world.GroupLimit(), "Two ordinary units lost the native group limit.");
        }

        private static void RiderAndMountBothSelected()
        {
            var world = Speeds(20f, 50f, 30f);
            world.Selected.Clear(); world.Selected.Add(world.Rider); world.Selected.Add(world.Horse);
            var inputs = world.SpeedActors();
            TestRunner.True(inputs.Count == 1 && ReferenceEquals(inputs[0], world.Horse), "The mount is not the one speed input of a rider-and-mount selection.");
            Near(Mps(50f), world.GroupLimit(), "The rider's speed still bounds a selection of the pair alone.");
            TestRunner.Equal(2, world.Selected.Count, "The selection itself was changed.");
            // Selection order does not matter, and the mount is still listed once.
            world.Selected.Reverse();
            TestRunner.Equal(1, world.SpeedActors().Count, "The mount was listed twice when it came first.");
        }

        private static void TwoPairsAndCompanion()
        {
            var world = World.Party();
            world.Rider.ModifiedSpeed = Mps(20f); world.Horse.ModifiedSpeed = Mps(50f);
            var second = world.AddPair("Rider 2", Mps(30f), "Horse 2", Mps(40f));
            world.Selected.Remove(world.Unit("Companion B"));
            world.Unit("Companion A").ModifiedSpeed = Mps(45f);
            var actors = world.SpeedActors();
            TestRunner.True(actors.Count == 3 && actors.Contains(world.Horse) && actors.Contains(second.Mount) && actors.Contains(world.Unit("Companion A")),
                "The speed inputs are not the two mounts and the companion, once each.");
            Near(Mps(40f), world.GroupLimit(), "The limit is not the slowest physical mover's.");
            second.Mount.ModifiedSpeed = Mps(25f);
            Near(Floor, world.GroupLimit(), "The native floor no longer applies to a slow physical mover.");
        }

        private static void NativeInputsKept()
        {
            var world = Speeds(20f, 50f, 30f);
            world.Pair.Mounted = false;
            world.Selected.Add(world.Horse);
            TestRunner.True(world.SpeedActors().SequenceEqual(world.Selected), "An unmounted party's speed inputs were changed.");
            Near(Floor, world.GroupLimit(), "An unmounted slow walker no longer meets the native floor.");

            world = Speeds(20f, 50f, 40f);
            world.TurnBased = true;
            TestRunner.True(world.SpeedActors().SequenceEqual(world.Selected), "Turn-based play no longer reads the native inputs.");
            Near(Floor, world.GroupLimit(), "The turn-based limit changed.");
        }

        private static void MountConditionAndFloor()
        {
            var world = Speeds(30f, 50f, 40f);
            Near(Mps(40f), world.GroupLimit(), "Setup: the companions should set the limit.");
            // Slowed, entangled or difficult terrain: native lowers the mount's modified speed.
            world.Horse.SpeedAffected = true;
            world.Horse.ModifiedSpeed = Mps(35f);
            Near(Mps(35f), world.GroupLimit(), "A condition on the mount did not change the limit.");
            world.Horse.ModifiedSpeed = Mps(15f);
            Near(Floor, world.GroupLimit(), "The native 30 ft floor was not applied to a slowed mount.");
            world.ClickGround();
            Near(Mps(15f), world.CurrentSpeed(world.Horse), "The slowed mount is not bounded by its own speed.");
            Near(Floor, world.CurrentSpeed(world.Unit("Companion A")), "The party waits below the native floor.");
            // The rider's own condition does not bound a pair the mount carries.
            world = Speeds(30f, 50f, 40f);
            world.Rider.ModifiedSpeed = Mps(15f);
            Near(Mps(40f), world.GroupLimit(), "A condition on the carried rider bounded the group.");
        }

        private static void SharedLimitSites()
        {
            var world = Speeds(20f, 50f, 40f);
            world.ClickEntrance();
            Near(Mps(40f), world.Unit("Companion A").Slot.SpeedLimit, "The entrance order did not take the corrected limit.");
            Near(Mps(40f), world.Horse.Slot.SpeedLimit, "The carried entrance command did not copy the rider's corrected limit.");
            world = Speeds(20f, 50f, 40f);
            world.StickFollow(world.Unit("Companion A"), 0.5f);
            Near(Mps(40f), world.Horse.Slot.SpeedLimit, "The gamepad follow order did not take the corrected limit.");
        }

        // ---- model -------------------------------------------------------------------------------------------------

        private enum Gait { Normal, Charge }
        private enum Kind { Ground, Entrance, Continuous, Approach }

        private sealed class Order
        {
            internal Kind Kind;
            internal float? SpeedLimit;
            internal Gait Gait = Gait.Normal;
            internal bool Accelerated, Finished, Ai, CreatedByPlayer = true;
            internal object Group;
            internal ModelUnit Executor;
        }

        private sealed class ModelUnit
        {
            internal string Name;
            internal float ModifiedSpeed;
            internal bool SpeedAffected, InCombat;
            internal Order Slot;   // the native Move slot: a ground move, a member command or a continuous command
        }

        private sealed class ModelPair
        {
            internal ModelUnit Rider, Mount;
            internal bool Mounted = true;
            internal long Generation = 1;
            internal readonly MountedPlayerMoveWatch Watch = new MountedPlayerMoveWatch();   // the routed ground order
            internal readonly MountedEntranceCarrierWatch Carrier = new MountedEntranceCarrierWatch();
        }

        private sealed class World
        {
            private readonly List<ModelUnit> units = new List<ModelUnit>();
            private readonly List<ModelPair> pairs = new List<ModelPair>();
            private readonly Dictionary<ModelPair, int> offers = new Dictionary<ModelPair, int>();
            internal readonly List<ModelUnit> Selected = new List<ModelUnit>();
            internal bool AcceleratedMoveSetting = true, HeavyLoad, TurnBased, SprintThree = true;
            internal int FollowerPasses;

            // The owner's party shape: two 30 ft companions, a 30 ft rider and a 40 ft horse; the horse is not selected.
            internal static World Party()
            {
                var world = new World();
                world.Selected.Add(world.Add("Companion A", Mps(30f)));
                world.Selected.Add(world.Add("Companion B", Mps(30f)));
                world.AddPair("Rider", Mps(30f), "Horse", Mps(40f));
                return world;
            }

            internal ModelUnit Add(string name, float speed)
            {
                var unit = new ModelUnit { Name = name, ModifiedSpeed = speed };
                units.Add(unit);
                return unit;
            }

            internal ModelPair AddPair(string rider, float riderSpeed, string mount, float mountSpeed)
            {
                var pair = new ModelPair { Rider = Add(rider, riderSpeed), Mount = Add(mount, mountSpeed) };
                pairs.Add(pair);
                Selected.Add(pair.Rider);
                return pair;
            }

            internal ModelPair Pair => pairs[0];
            internal ModelUnit Rider => pairs[0].Rider;
            internal ModelUnit Horse => pairs[0].Mount;
            internal ModelUnit Unit(string name) => units.Single(u => u.Name == name);
            internal int Offers(ModelPair pair) => offers.TryGetValue(pair, out var count) ? count : 0;
            internal IEnumerable<Order> AllOrders() => units.Where(u => u.Slot != null).Select(u => u.Slot);

            private ModelPair PairOf(ModelUnit unit) => pairs.FirstOrDefault(p => ReferenceEquals(p.Rider, unit) || ReferenceEquals(p.Mount, unit));

            // ---- M03: native MoveSelectedUnitsToPoint speed limit, with the production speed inputs ----

            internal List<ModelUnit> SpeedActors() => SprintThree
                ? MountedPaceActors.Project(Selected, unit =>
                {
                    var pair = PairOf(unit);
                    return pair != null && MountedGroupPacePolicy.SpeedInputIsMount(pair.Mounted, TurnBased, ReferenceEquals(unit, pair.Rider), pair.Mount != null)
                        ? pair.Mount : null;
                })
                : Selected.ToList();

            internal float? GroupLimit()
            {
                if (Selected.Count <= 1) return null;
                return Math.Max(SpeedActors().Select(u => u.ModifiedSpeed).Min(), Floor);
            }

            // ---- native orders ----

            private void RunGround(ModelUnit unit, float? limit)
            {
                var pair = PairOf(unit);
                var route = CommandRouter.RouteGroundMove(pair != null && pair.Mounted ? new MountedPair(pair.Rider.Name, pair.Mount.Name) : null, unit.Name);
                if (route.Action == CommandRoutingAction.SuppressDuplicateMount) return;
                var executor = route.Action == CommandRoutingAction.RewriteRiderToMount ? pair.Mount : unit;
                executor.Slot = new Order { Kind = Kind.Ground, SpeedLimit = limit, Executor = executor };
                if (route.Action == CommandRoutingAction.RewriteRiderToMount) pair.Watch.Bind(executor.Slot, pair.Generation);
            }

            internal void ClickGround()
            {
                var limit = GroupLimit();
                foreach (var unit in Selected.Where(u => u != null).ToArray()) RunGround(unit, limit);
            }

            internal void ClickEntrance()
            {
                var limit = GroupLimit();
                var group = new object();
                foreach (var unit in Selected.Where(u => u != null).ToArray())
                {
                    unit.Slot = new Order { Kind = Kind.Entrance, SpeedLimit = limit, Group = group, Executor = unit };
                    var pair = PairOf(unit);
                    if (pair == null) continue;
                    // The entrance carry, from the Run postfix of the rider's own member command.
                    var decision = MountedEntranceCarryPolicy.Decide(pair.Mounted, ReferenceEquals(unit, pair.Rider), true, false, true, true, true,
                        pair.Mount.Slot != null && pair.Mount.Slot.Kind == Kind.Entrance && ReferenceEquals(pair.Mount.Slot.Group, group) && !pair.Mount.Slot.Finished);
                    if (decision != MountedEntranceCarryDecision.Carry) continue;
                    pair.Mount.Slot = new Order { Kind = Kind.Entrance, SpeedLimit = unit.Slot.SpeedLimit, Group = group, Executor = pair.Mount, CreatedByPlayer = unit.Slot.CreatedByPlayer };
                    pair.Carrier.Bind(pair.Mount.Slot, group, pair.Generation);
                }
            }

            // ---- native acceleration ----

            private bool CanAccelerate(Order order) => order != null && !order.Accelerated && order.CreatedByPlayer && AcceleratedMoveSetting &&
                order.Gait == Gait.Normal && !order.Executor.SpeedAffected && !order.Executor.InCombat && !HeavyLoad;

            private void Accelerate(Order order)
            {
                if (!CanAccelerate(order)) return;
                order.Gait = Gait.Charge;
                if (order.SpeedLimit.HasValue) order.SpeedLimit *= 1.8f;
                order.Accelerated = true;
            }

            internal void Deaccelerate(Order order)
            {
                if (order == null || !order.Accelerated) return;
                order.Gait = Gait.Normal;
                if (order.SpeedLimit.HasValue) order.SpeedLimit /= 1.8f;
                order.Accelerated = false;
            }

            private MountedGroupPaceOrder Classify(ModelPair pair, out Order order)
            {
                order = pair.Mount.Slot;
                if (order == null) return MountedGroupPaceOrder.None;
                var riderMember = pair.Rider.Slot != null && pair.Rider.Slot.Kind == Kind.Entrance ? pair.Rider.Slot : null;
                var kind = MountedGroupPacePolicy.ClassifyOrder(
                    ReferenceEquals(order.Executor, pair.Mount), order.CreatedByPlayer, order.Ai,
                    order.Kind == Kind.Ground, pair.Watch.IsCurrent(order, pair.Generation),
                    order.Kind == Kind.Entrance, order.Kind == Kind.Entrance && pair.Carrier.IsCurrent(order, order.Group, pair.Generation),
                    order.Kind == Kind.Entrance && riderMember != null && !riderMember.Finished && ReferenceEquals(riderMember.Group, order.Group));
                // The reviewed base knew the routed ground order only.
                return !SprintThree && kind == MountedGroupPaceOrder.CarriedEntranceOrder ? MountedGroupPaceOrder.None : kind;
            }

            // One unit's own path completes: native OnPathComplete -> AccelerateIfPathIsLong, then the production postfix.
            internal void PathCompletes(ModelUnit trigger, float pathLength)
            {
                var order = trigger.Slot;
                if (order == null) return;
                var before = order.Accelerated;
                if (CanAccelerate(order) && pathLength >= 12.7f)
                {
                    Accelerate(order);
                    foreach (var unit in Selected.Where(u => u != null)) Accelerate(unit.Slot);
                }
                var offerRan = MountedGroupPacePolicy.NativeGroupOfferRan(before, order.Accelerated);
                foreach (var pair in pairs)
                {
                    var kind = Classify(pair, out var mountOrder);
                    var decision = MountedGroupPacePolicy.Decide(offerRan, pair.Mounted, TurnBased, Selected.Contains(pair.Rider), Selected.Contains(pair.Mount),
                        kind != MountedGroupPaceOrder.None, mountOrder != null && mountOrder.Finished, mountOrder != null && mountOrder.Accelerated);
                    if (decision != MountedGroupPaceCarry.OfferNativeAcceleration) continue;
                    offers[pair] = Offers(pair) + 1;
                    Accelerate(mountOrder);
                }
            }

            // ---- M02: native InGameInputLayer.UpdateMovement with the production follower list ----

            internal List<ModelUnit> OfferUnits() => SprintThree
                ? MountedPaceActors.Project(Selected, unit =>
                {
                    var pair = PairOf(unit);
                    if (pair == null) return null;
                    var kind = Classify(pair, out var order);
                    return MountedGroupPacePolicy.DecideGamepadFollower(pair.Mounted, TurnBased, ReferenceEquals(unit, pair.Rider), Selected.Contains(pair.Mount),
                        kind == MountedGroupPaceOrder.RoutedGroundOrder, order != null && order.Finished) == MountedGamepadFollowerOffer.OfferToMount ? pair.Mount : null;
                })
                : Selected.ToList();

            internal void StickFollow(ModelUnit driver, float magnitude, bool reissue = true)
            {
                if (driver.Slot == null || driver.Slot.Kind != Kind.Continuous) driver.Slot = new Order { Kind = Kind.Continuous, Executor = driver };
                if (magnitude >= 0.95f) Accelerate(driver.Slot); else Deaccelerate(driver.Slot);
                // The repath: MakeCompanionsFollow re-orders every selected unit but the stick-driven one.
                if (reissue)
                {
                    var limit = GroupLimit();
                    foreach (var unit in Selected.Where(u => u != null && !ReferenceEquals(u, driver)).ToArray()) RunGround(unit, limit);
                }
                FollowerPasses++;
                var accelerated = driver.Slot.Accelerated;
                foreach (var unit in OfferUnits())
                {
                    // Commands.Move: an unfinished UnitMoveTo in the Move slot.
                    var move = unit != null && unit.Slot != null && !unit.Slot.Finished && (unit.Slot.Kind == Kind.Ground || unit.Slot.Kind == Kind.Approach) ? unit.Slot : null;
                    if (move == null) continue;
                    if (accelerated) Accelerate(move); else Deaccelerate(move);
                }
            }

            // Stick at rest: native returns before any order or offer; the continuous command ends by itself.
            internal void StickRelease(ModelUnit driver)
            {
                if (driver.Slot != null && driver.Slot.Kind == Kind.Continuous) driver.Slot.Finished = true;
            }

            internal float CurrentSpeed(ModelUnit unit)
            {
                var order = unit.Slot;
                var speed = unit.ModifiedSpeed * (order != null && order.Accelerated ? 1.8f : 1f);
                return order?.SpeedLimit != null ? Math.Min(speed, order.SpeedLimit.Value) : speed;
            }
        }
    }
}
