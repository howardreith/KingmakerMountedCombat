using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Horse speed repair (2026-11-12). The production decision is MountedGroupPacePolicy. The group scenarios drive it
    // through a small model of the installed native contract (decompiled Kingmaker 2.1.7b, recorded as a summary):
    //   ClickGroundHandler.MoveSelectedUnitsToPoint: multi-unit SpeedLimit = max(min selected ModifiedSpeedMps, 3.048).
    //   UnitCommand.AccelerateIfPathIsLong: the trigger accelerates, then Accelerate() for every selected unit's commands.
    //   UnitCommand.Accelerate/CanAccelerate: refuses when already accelerated, not player-made, not Normal gait, the
    //   setting is off, the executor is in combat, its speed is affected, or the party is heavily loaded/overloaded;
    //   otherwise Charge gait and SpeedLimit x1.8.
    //   UnitEntityData.CurrentSpeedMps out of combat: ModifiedSpeedMps (x1.8 when accelerated), capped by SpeedLimit.
    // The model is a reading of the native code, not native evidence; native acceptance is separate.
    internal static class MountedGroupPacePolicyTests
    {
        private const float GroupFloor = 30f * 0.3048f / 3f;

        public static void Register(TestRunner runner)
        {
            runner.Run("horse pace: the native group offer is recognised only when its trigger newly accelerated", OfferRecognition);
            runner.Run("horse pace: a long whole-party order accelerates the mount executing the selected rider's order with the party", MountJoinsAcceleratedGroup);
            runner.Run("horse pace: without the carry the mount keeps the unaccelerated group limit (the logged 3.05 against 5.49)", DefectReproducedWithoutCarry);
            runner.Run("horse pace: a speed-affected mount is still refused by native Accelerate and keeps its own slower pace", LegitimatePenaltyKept);
            runner.Run("horse pace: the accelerated-move setting off or a heavy party load keeps everyone, mount included, at walking pace", NativeSettingAndLoadKept);
            runner.Run("horse pace: a short order, or no long trigger, never accelerates the mount", ShortOrderUnchanged);
            runner.Run("horse pace: combat, unselected riders, selected mounts, owned approaches and settled orders are not carried", Refusals);
        }

        private static void OfferRecognition()
        {
            TestRunner.True(MountedGroupPacePolicy.NativeGroupOfferRan(false, true), "A newly accelerated trigger offered the group acceleration.");
            TestRunner.True(!MountedGroupPacePolicy.NativeGroupOfferRan(true, true), "An already accelerated trigger never reaches the native offer.");
            TestRunner.True(!MountedGroupPacePolicy.NativeGroupOfferRan(false, false), "A short or refused trigger made no offer.");
            TestRunner.True(!MountedGroupPacePolicy.NativeGroupOfferRan(true, false), "Deacceleration is not an offer.");
        }

        private static void MountJoinsAcceleratedGroup()
        {
            var world = World.MountedParty();
            world.ClickGround(pathLengthOfTrigger: 97.88f, trigger: "Companion A", carry: true);
            var mount = world.Order("Horse");
            var party = world.Order("Companion A");
            TestRunner.True(mount.Accelerated, "The mount executing the rider's order did not join the accelerated group move.");
            TestRunner.Equal(party.SpeedLimit, mount.SpeedLimit, "The mount kept a different group limit from its party.");
            TestRunner.Equal(Gait.Charge, mount.Gait, "The mount's accelerated order did not take the native run gait.");
            TestRunner.True(Math.Abs(world.CurrentSpeed("Horse") - world.CurrentSpeed("Companion A")) < 0.001f,
                "The mount's pace differs from the party's under the shared accelerated limit.");
            TestRunner.True(Math.Abs(mount.SpeedLimit.Value - GroupFloor * 1.8f) < 0.001f, "The group limit is not the native 1.8 x the native floor.");
            TestRunner.True(world.Order("Rider") == null, "The rider gained a command of its own; the mount stays the movement owner.");
        }

        private static void DefectReproducedWithoutCarry()
        {
            var world = World.MountedParty();
            world.ClickGround(pathLengthOfTrigger: 97.88f, trigger: "Companion A", carry: false);
            TestRunner.True(!world.Order("Horse").Accelerated, "The model no longer shows the native selection-only offer.");
            TestRunner.True(Math.Abs(world.CurrentSpeed("Horse") - GroupFloor) < 0.001f, "Without the carry the mount should walk at the group floor.");
            TestRunner.True(world.CurrentSpeed("Companion A") > 1.7f * world.CurrentSpeed("Horse"), "The party should outrun the uncarried mount.");
        }

        private static void LegitimatePenaltyKept()
        {
            var world = World.MountedParty();
            world.Unit("Horse").SpeedAffected = true;   // e.g. Slowed, Entangled, difficult terrain: native halves it
            world.Unit("Horse").ModifiedSpeed = 4.064f * 0.5f;
            world.ClickGround(pathLengthOfTrigger: 40f, trigger: "Companion A", carry: true);
            TestRunner.True(world.CarryOffered("Horse"), "The policy must offer the order; the native rule is what refuses it.");
            TestRunner.True(!world.Order("Horse").Accelerated, "A speed-affected mount was accelerated past the native refusal.");
            TestRunner.True(Math.Abs(world.CurrentSpeed("Horse") - 2.032f) < 0.001f, "The mount's own penalty no longer bounds its pace.");
            TestRunner.True(world.Order("Companion A").Accelerated, "Unaffected party members lost their native acceleration.");
        }

        private static void NativeSettingAndLoadKept()
        {
            foreach (var heavy in new[] { false, true })
            {
                var world = World.MountedParty();
                world.AcceleratedMoveSetting = heavy;     // setting off in one case, heavy load (setting on) in the other
                world.HeavyLoad = heavy;
                world.ClickGround(pathLengthOfTrigger: 40f, trigger: "Companion A", carry: true);
                foreach (var name in new[] { "Horse", "Companion A", "Companion B" })
                {
                    TestRunner.True(!world.Order(name).Accelerated, name + " accelerated against the native setting/encumbrance rule.");
                    TestRunner.True(Math.Abs(world.CurrentSpeed(name) - GroupFloor) < 0.001f, name + " left the native group walking pace.");
                }
            }
        }

        private static void ShortOrderUnchanged()
        {
            var world = World.MountedParty();
            world.ClickGround(pathLengthOfTrigger: 9f, trigger: "Companion A", carry: true);
            TestRunner.True(!world.Order("Horse").Accelerated && !world.Order("Companion A").Accelerated, "A short order accelerated someone.");
            TestRunner.True(!world.CarryOffered("Horse"), "The carry ran without a native group offer.");
            TestRunner.True(Math.Abs(world.CurrentSpeed("Horse") - GroupFloor) < 0.001f, "A short order changed the mount's group pace.");
        }

        private static void Refusals()
        {
            MountedGroupPaceCarry Decide(bool offer = true, bool mounted = true, bool tb = false, bool riderSelected = true,
                bool mountSelected = false, bool routed = true, bool finished = false, bool accelerated = false) =>
                MountedGroupPacePolicy.Decide(offer, mounted, tb, riderSelected, mountSelected, routed, finished, accelerated);
            TestRunner.Equal(MountedGroupPaceCarry.OfferNativeAcceleration, Decide(), "The routed group order was not offered.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(offer: false), "Carried without a native group offer.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(mounted: false), "An unmounted pair was carried.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(tb: true), "Turn-based movement received an exploration adjustment.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(riderSelected: false), "A pair outside the selection was accelerated.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(mountSelected: true), "A selected mount was offered twice.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(routed: false), "An owned approach (attack, door, trap, recovery) was accelerated.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(finished: true), "A finished order was touched.");
            TestRunner.Equal(MountedGroupPaceCarry.NotApplicable, Decide(accelerated: true), "An accelerated order was offered again.");
        }

        private enum Gait { Normal, Charge }

        private sealed class ModelUnit
        {
            internal string Name;
            internal float ModifiedSpeed;
            internal bool SpeedAffected;
            internal ModelOrder Order;
        }

        private sealed class ModelOrder
        {
            internal float? SpeedLimit;
            internal Gait Gait = Gait.Normal;
            internal bool Accelerated, CreatedByPlayer = true;
        }

        private sealed class World
        {
            private readonly Dictionary<string, ModelUnit> units = new Dictionary<string, ModelUnit>();
            private readonly HashSet<string> carryOffered = new HashSet<string>();
            internal bool AcceleratedMoveSetting = true, HeavyLoad;
            internal readonly List<string> Selected = new List<string>();

            // The owner's party shape: 30 ft companions, a 30 ft rider and a horse whose modified speed is 40 ft.
            internal static World MountedParty()
            {
                var world = new World();
                var names = new[] { "Companion A", "Companion B", "Rider", "Horse" };
                var speeds = new[] { 3.048f, 3.048f, 3.048f, 4.064f };
                for (var i = 0; i < names.Length; i++)
                    world.units[names[i]] = new ModelUnit { Name = names[i], ModifiedSpeed = speeds[i] };
                // Native selection projection: the horse is never in SelectedUnits while mounted in real time.
                world.Selected.AddRange(new[] { "Companion A", "Companion B", "Rider" });
                return world;
            }

            internal ModelUnit Unit(string name) => units[name];
            internal ModelOrder Order(string name) => units[name].Order;
            internal bool CarryOffered(string name) => carryOffered.Contains(name);

            internal void ClickGround(float pathLengthOfTrigger, string trigger, bool carry)
            {
                float? limit = null;
                if (Selected.Count > 1) limit = Math.Max(Selected.Min(n => units[n].ModifiedSpeed), GroupFloor);
                foreach (var name in Selected)
                {
                    // KMC's RunCommand prefix runs the rider's order on its mount.
                    var executor = name == "Rider" ? "Horse" : name;
                    units[executor].Order = new ModelOrder { SpeedLimit = limit };
                }
                // One unit's own long path completes: native AccelerateIfPathIsLong.
                var triggerOrder = units[trigger].Order;
                var before = triggerOrder.Accelerated;
                if (pathLengthOfTrigger >= 12.7f && CanAccelerate(units[trigger]))
                {
                    Accelerate(units[trigger]);
                    foreach (var name in Selected) if (units[name].Order != null) Accelerate(units[name]);
                }
                if (!carry) return;
                var offerRan = MountedGroupPacePolicy.NativeGroupOfferRan(before, triggerOrder.Accelerated);
                var horse = units["Horse"];
                var decision = MountedGroupPacePolicy.Decide(offerRan, true, false, Selected.Contains("Rider"), Selected.Contains("Horse"),
                    horse.Order != null, false, horse.Order != null && horse.Order.Accelerated);
                if (decision == MountedGroupPaceCarry.OfferNativeAcceleration)
                {
                    carryOffered.Add("Horse");
                    Accelerate(horse);
                }
            }

            internal float CurrentSpeed(string name)
            {
                var unit = units[name];
                var speed = unit.ModifiedSpeed * (unit.Order != null && unit.Order.Accelerated ? 1.8f : 1f);
                return unit.Order?.SpeedLimit != null ? Math.Min(speed, unit.Order.SpeedLimit.Value) : speed;
            }

            private bool CanAccelerate(ModelUnit unit) => unit.Order != null && !unit.Order.Accelerated && unit.Order.CreatedByPlayer &&
                AcceleratedMoveSetting && unit.Order.Gait == Gait.Normal && !unit.SpeedAffected && !HeavyLoad;

            private void Accelerate(ModelUnit unit)
            {
                if (!CanAccelerate(unit)) return;
                unit.Order.Gait = Gait.Charge;
                if (unit.Order.SpeedLimit.HasValue) unit.Order.SpeedLimit *= 1.8f;
                unit.Order.Accelerated = true;
            }
        }
    }
}
