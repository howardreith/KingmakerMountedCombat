using System.Collections.Generic;
using System.Globalization;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Horse speed repair (2026-11-12), see MountedGroupPacePolicy for the native contract. Runs from the postfix of
    // native UnitCommand.AccelerateIfPathIsLong, immediately after that call offered Accelerate() to every command of
    // every selected unit. The mount executes its selected rider's routed ground order, so it receives the same native
    // Accelerate() call the rider's own order would have received. Nothing here computes a speed, writes SpeedLimit or
    // MovementType, or touches the agent: native Accelerate()/CanAccelerate decide and apply everything, including every
    // legitimate refusal. Completion, interruption, replacement and arrival stay with the native command.
    // Known issues sprint 3: the same offer for the member command the entrance carry issued (M01), and the mount as
    // the answer to the gamepad follower pass for its selected rider (M02).
    internal sealed partial class MountedCombatController
    {
        private UnitCommand lastGroupPaceReportedOrder;

        internal long GroupPaceCarryCount { get; private set; }
        internal long GroupPaceNativeRefusalCount { get; private set; }
        internal long EntrancePaceCarryCount { get; private set; }
        internal long GamepadFollowerOfferCount { get; private set; }

        // The mount's current Move-slot command and what it is to the pair (MountedGroupPacePolicy.ClassifyOrder).
        private MountedGroupPaceOrder ClassifyMountGroupOrder(UnitEntityData rider, UnitEntityData mount, out UnitCommand order)
        {
            order = mount?.Commands?.GetCommand(UnitCommand.CommandType.Move);
            if (order == null) return MountedGroupPaceOrder.None;
            var transition = order as UnitAreaTransition;
            var exactTransition = transition != null && order.GetType() == typeof(UnitAreaTransition);
            var riderMember = exactTransition ? rider?.Commands?.GetCommand<UnitAreaTransition>() : null;
            return MountedGroupPacePolicy.ClassifyOrder(
                order.Executor == mount,
                order.CreatedByPlayer,
                order.AiAction != null,
                order.GetType() == typeof(UnitMoveTo),
                playerMove.IsCurrent(order, relationship.MountedPairGeneration),
                exactTransition,
                exactTransition && entranceCarrier.IsCurrent(order, transition.GroupCommand, relationship.MountedPairGeneration),
                exactTransition && riderMember != null && !riderMember.IsFinished &&
                    ReferenceEquals(riderMember.GroupCommand, transition.GroupCommand));
        }

        internal void CarryNativeGroupAcceleration()
        {
            if (disposed) return;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var selected = Game.Instance?.UI?.SelectionManager?.SelectedUnits;
            var kind = ClassifyMountGroupOrder(rider, mount, out var order);
            var decision = MountedGroupPacePolicy.Decide(
                true,
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                CombatController.IsInTurnBasedCombat(),
                rider != null && selected != null && selected.Contains(rider),
                mount != null && selected != null && selected.Contains(mount),
                kind != MountedGroupPaceOrder.None,
                order != null && order.IsFinished,
                order != null && order.IsAccelerated);
            if (decision != MountedGroupPaceCarry.OfferNativeAcceleration) return;

            var limitBefore = order.SpeedLimit;
            var gaitBefore = order.MovementType;
            order.Accelerate();
            var carried = order.IsAccelerated;
            var entrance = kind == MountedGroupPaceOrder.CarriedEntranceOrder;
            if (!carried) GroupPaceNativeRefusalCount++;
            else if (entrance) EntrancePaceCarryCount++;
            else GroupPaceCarryCount++;
            // One line per order: the native group offer can run again for later long paths of other units.
            if (ReferenceEquals(lastGroupPaceReportedOrder, order)) return;
            lastGroupPaceReportedOrder = order;
            logger.Info("Mounted group pace: " + (carried
                    ? "the native accelerated group move reached the mount executing the rider's " + (entrance ? "entrance order" : "order")
                    : "native Accelerate refused the mount's " + (entrance ? "entrance" : "routed") + " order; it keeps the group pace") +
                "; order=" + kind + "; mountId=" + mount.UniqueId + "; riderId=" + rider.UniqueId + "; selected=" + selected.Count +
                "; speedLimit=" + Pace(limitBefore) + "->" + Pace(order.SpeedLimit) + "; gait=" + gaitBefore + "->" + order.MovementType +
                "; mountModifiedSpeed=" + Pace(mount.ModifiedSpeedMps) + "; mountCurrentSpeed=" + Pace(mount.CurrentSpeedMps) +
                "; mountSpeedAffected=" + mount.SpeedIsAffected + "; mountInCombat=" + mount.IsInCombat +
                "; acceleratedMoveSetting=" + (SettingsRoot.Instance?.AcceleratedMove?.CurrentValue.ToString() ?? "unknown") +
                "; encumbrance=" + (Game.Instance?.Player?.Encumbrance.ToString() ?? "unknown") + ".");
        }

        // M02. Called for one unit of the selection the gamepad follower pass is about to walk. Answers the mount when
        // that unit is this pair's rider and the mount executes the rider's routed ground order; null keeps the unit.
        // Native then calls Accelerate() or Deaccelerate() on the mount's Commands.Move itself. No state is kept and
        // nothing is logged: the pass repeats several times a second while the stick is held.
        internal UnitEntityData GamepadFollowerOfferMover(UnitEntityData unit, List<UnitEntityData> selected)
        {
            if (disposed || unit == null) return null;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var kind = ClassifyMountGroupOrder(rider, mount, out var order);
            var decision = MountedGroupPacePolicy.DecideGamepadFollower(
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                CombatController.IsInTurnBasedCombat(),
                ReferenceEquals(unit, rider),
                mount != null && selected != null && selected.Contains(mount),
                kind == MountedGroupPaceOrder.RoutedGroundOrder,
                order != null && order.IsFinished);
            if (decision != MountedGamepadFollowerOffer.OfferToMount) return null;
            GamepadFollowerOfferCount++;
            return mount;
        }

        private static string Pace(float? value) =>
            value.HasValue ? value.Value.ToString("F2", CultureInfo.InvariantCulture) : "none";
    }
}
