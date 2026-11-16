using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Horse speed repair (2026-11-12). Native group pace, from the installed Kingmaker 2.1.7b contracts:
    //   - ClickGroundHandler.MoveSelectedUnitsToPoint gives every member of a multi-unit selection the same
    //     UnitMoveTo.SpeedLimit, the slowest selected ModifiedSpeedMps floored at 30 ft per round (3.048 m/s).
    //   - The only mouse-play route to the accelerated ("running") group move is UnitMovementAgent.OnPathComplete ->
    //     UnitCommand.AccelerateIfPathIsLong. The first command whose new path is at least 12.7 m accelerates itself
    //     (Charge gait, SpeedLimit x1.8) and then offers Accelerate() to every command of every selected unit.
    //     Each Accelerate() re-applies the native CanAccelerate refusals: the Accelerated Move setting, player origin,
    //     a Normal-gait order, combat, a speed-affecting condition (SpeedIsAffected) and party encumbrance.
    // Selection projects a mounted pair's mount onto its rider, and the ground click's RunCommand runs the rider's
    // order on the mount. The native offer therefore reaches the rider's empty command slots and never the mount that
    // executes the order, which kept the unaccelerated group limit while the party ran ahead. This policy decides
    // when the pair's routed ground order receives the same native offer; the offer itself stays native.
    //
    // Known issues sprint 3 (2026-11-16), same installed contracts:
    //   M01  The entrance click (AreaTransitionController.StartAreaTransition, and the gamepad's own copy) runs
    //        MoveSelectedUnitsToPoint with a runner that gives each selected unit a player-made UnitAreaTransition.
    //        OnPathComplete calls AccelerateIfPathIsLong on every command of the unit, whatever its type, so the
    //        same offer exists for entrance orders. The mount's carried member command (MountedEntranceCarryPolicy)
    //        sits outside the selection exactly as the routed ground order does.
    //   M02  InGameInputLayer.UpdateMovement (gamepad stick): at each repath of the stick-driven unit, every selected
    //        unit's Commands.Move receives Accelerate() or Deaccelerate() after the driver's own continuous command.
    //        A following pair's order runs on its mount, which is not in that selection.
    //   M03  The group limit is computed from the selected units' ModifiedSpeedMps. A selected rider does not move;
    //        its mount does.
    public enum MountedGroupPaceCarry
    {
        NotApplicable,
        OfferNativeAcceleration
    }

    // Which order of its selected rider the mount executes. Nothing else the mount holds is ever offered acceleration.
    public enum MountedGroupPaceOrder
    {
        None,
        RoutedGroundOrder,
        CarriedEntranceOrder
    }

    public enum MountedGamepadFollowerOffer
    {
        KeepSelectedUnit,
        OfferToMount
    }

    public static class MountedGroupPacePolicy
    {
        // AccelerateIfPathIsLong offers the group acceleration exactly when its own command became accelerated in
        // that call: CanAccelerate requires a command that was not yet accelerated, and only the long-path branch sets it.
        public static bool NativeGroupOfferRan(bool triggerAcceleratedBefore, bool triggerAcceleratedAfter)
        {
            return !triggerAcceleratedBefore && triggerAcceleratedAfter;
        }

        // The command in the mount's Move slot, classified by exact identity:
        //   - the ground order the ground click routed from the rider (bound by the run that click made), or
        //   - the member command the entrance carry issued for the rider's own live member command of the same group.
        // Attack, Charge, spell, door, trap and recovery approaches, AI orders, a foreign or replaced command and an
        // entrance command the mount holds for any other reason are None.
        public static MountedGroupPaceOrder ClassifyOrder(
            bool executorIsMount,
            bool createdByPlayer,
            bool aiOrigin,
            bool exactGroundMove,
            bool boundRoutedGroundOrder,
            bool exactAreaTransition,
            bool boundEntranceCarrier,
            bool riderHoldsMemberCommandOfSameGroup)
        {
            if (!executorIsMount || !createdByPlayer || aiOrigin) return MountedGroupPaceOrder.None;
            if (exactGroundMove && boundRoutedGroundOrder) return MountedGroupPaceOrder.RoutedGroundOrder;
            // The native offer walks the selected rider's own command slots. Once the rider's member command has
            // ended or been replaced there is no order of the rider left for the offer to have reached.
            if (exactAreaTransition && boundEntranceCarrier && riderHoldsMemberCommandOfSameGroup)
                return MountedGroupPaceOrder.CarriedEntranceOrder;
            return MountedGroupPaceOrder.None;
        }

        public static MountedGroupPaceCarry Decide(
            bool nativeGroupOfferRan,
            bool pairMounted,
            bool turnBasedCombat,
            bool riderSelected,
            bool mountSelected,
            bool mountExecutesRiderGroupOrder,
            bool orderFinished,
            bool orderAccelerated)
        {
            // The native offer reaches selected units only; a pair whose rider was not selected received no order to carry.
            if (!nativeGroupOfferRan || !pairMounted || !riderSelected) return MountedGroupPaceCarry.NotApplicable;
            // A selected mount (the native turn-based paired-partner selection) already receives the native offer itself.
            if (mountSelected) return MountedGroupPaceCarry.NotApplicable;
            // Exploration only: turn-based movement keeps its budgets and the native in-combat refusal.
            if (turnBasedCombat) return MountedGroupPaceCarry.NotApplicable;
            // Only the exact order of the rider that the mount executes (ClassifyOrder). An already accelerated order
            // is left as it is.
            if (!mountExecutesRiderGroupOrder || orderFinished || orderAccelerated) return MountedGroupPaceCarry.NotApplicable;
            return MountedGroupPaceCarry.OfferNativeAcceleration;
        }

        // M02. The gamepad's follower offer reads each selected unit's Commands.Move. A selected rider whose routed
        // ground order runs on its unselected mount is answered by that mount, so the native Accelerate() or
        // Deaccelerate() of that same pass lands on the command that moves the pair. Everything else is the unit itself.
        public static MountedGamepadFollowerOffer DecideGamepadFollower(
            bool pairMounted,
            bool turnBasedCombat,
            bool unitIsRider,
            bool mountSelected,
            bool mountExecutesRoutedGroundOrder,
            bool orderFinished)
        {
            if (!pairMounted || !unitIsRider || turnBasedCombat) return MountedGamepadFollowerOffer.KeepSelectedUnit;
            // A selected mount is reached by the native loop itself.
            if (mountSelected) return MountedGamepadFollowerOffer.KeepSelectedUnit;
            if (!mountExecutesRoutedGroundOrder || orderFinished) return MountedGamepadFollowerOffer.KeepSelectedUnit;
            return MountedGamepadFollowerOffer.OfferToMount;
        }

        // M03. Whether a selected unit's speed input is its mount's: the rider of a mounted pair never walks a ground
        // order, its mount does. Turn-based play is left exactly as native computes it.
        public static bool SpeedInputIsMount(bool pairMounted, bool turnBasedCombat, bool unitIsRider, bool mountAvailable)
        {
            return pairMounted && !turnBasedCombat && unitIsRider && mountAvailable;
        }
    }

    // The actors a native per-selection pass is answered with. Each selected unit is replaced by the mover the map
    // names for it (null keeps the unit), and every resulting actor is listed once, in the selection's order. A null
    // entry is kept as native had it. The selection itself is never changed.
    public static class MountedPaceActors
    {
        public static List<T> Project<T>(IEnumerable<T> selected, Func<T, T> mover) where T : class
        {
            var actors = new List<T>();
            if (selected == null) return actors;
            foreach (var unit in selected)
            {
                if (unit == null) { actors.Add(null); continue; }
                var actor = (mover != null ? mover(unit) : null) ?? unit;
                var listed = false;
                foreach (var known in actors)
                    if (ReferenceEquals(known, actor)) { listed = true; break; }
                if (!listed) actors.Add(actor);
            }
            return actors;
        }
    }

    // M01. The one member command the entrance carry issued to the mount, with the group it belongs to and the pair
    // generation it was issued for. Identity only: a later command, another group or a remounted pair never matches.
    public sealed class MountedEntranceCarrierWatch
    {
        private long generation;

        public object Carrier { get; private set; }
        public object Group { get; private set; }

        public void Bind(object carrier, object group, long pairGeneration)
        {
            Carrier = carrier;
            Group = group;
            generation = pairGeneration;
        }

        public void Clear()
        {
            Carrier = null;
            Group = null;
        }

        public bool IsCurrent(object command, object group, long pairGeneration)
        {
            return command != null && group != null && ReferenceEquals(command, Carrier) && ReferenceEquals(group, Group) &&
                generation == pairGeneration;
        }

        // Lets go of a carrier that has ended, or whose pair is no longer the mounted pair it was issued for, so
        // no command, group or unit of an earlier approach or area is kept. True when something was released.
        public bool ReleaseIfEnded(bool carrierFinished, bool pairMounted, long pairGeneration)
        {
            if (Carrier == null) return false;
            if (!carrierFinished && pairMounted && generation == pairGeneration) return false;
            Clear();
            return true;
        }
    }
}
