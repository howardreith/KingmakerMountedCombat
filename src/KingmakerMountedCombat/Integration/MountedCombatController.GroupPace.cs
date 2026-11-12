using System.Globalization;
using Kingmaker;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic.Commands;
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
    internal sealed partial class MountedCombatController
    {
        private UnitMoveTo lastGroupPaceReportedMove;

        internal long GroupPaceCarryCount { get; private set; }
        internal long GroupPaceNativeRefusalCount { get; private set; }

        internal void CarryNativeGroupAcceleration()
        {
            if (disposed) return;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var selected = Game.Instance?.UI?.SelectionManager?.SelectedUnits;
            var move = mount?.Commands?.Move;
            var routed = move != null && move.GetType() == typeof(UnitMoveTo) && ReferenceEquals(move, playerMove.Move) &&
                move.Executor == mount && move.CreatedByPlayer && move.AiAction == null;
            var decision = MountedGroupPacePolicy.Decide(
                true,
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                CombatController.IsInTurnBasedCombat(),
                rider != null && selected != null && selected.Contains(rider),
                mount != null && selected != null && selected.Contains(mount),
                routed,
                move != null && move.IsFinished,
                move != null && move.IsAccelerated);
            if (decision != MountedGroupPaceCarry.OfferNativeAcceleration) return;

            var limitBefore = move.SpeedLimit;
            var gaitBefore = move.MovementType;
            move.Accelerate();
            var carried = move.IsAccelerated;
            if (carried) GroupPaceCarryCount++;
            else GroupPaceNativeRefusalCount++;
            // One line per routed order: the native group offer can run again for later long paths of other units.
            if (ReferenceEquals(lastGroupPaceReportedMove, move)) return;
            lastGroupPaceReportedMove = move;
            logger.Info("Mounted group pace: " + (carried
                    ? "the native accelerated group move reached the mount executing the rider's order"
                    : "native Accelerate refused the mount's routed order; it keeps the group pace") +
                "; mountId=" + mount.UniqueId + "; riderId=" + rider.UniqueId + "; selected=" + selected.Count +
                "; speedLimit=" + Pace(limitBefore) + "->" + Pace(move.SpeedLimit) + "; gait=" + gaitBefore + "->" + move.MovementType +
                "; mountModifiedSpeed=" + Pace(mount.ModifiedSpeedMps) + "; mountCurrentSpeed=" + Pace(mount.CurrentSpeedMps) +
                "; mountSpeedAffected=" + mount.SpeedIsAffected + "; mountInCombat=" + mount.IsInCombat +
                "; acceleratedMoveSetting=" + (SettingsRoot.Instance?.AcceleratedMove?.CurrentValue.ToString() ?? "unknown") +
                "; encumbrance=" + (Game.Instance?.Player?.Encumbrance.ToString() ?? "unknown") + ".");
        }

        private static string Pace(float? value) =>
            value.HasValue ? value.Value.ToString("F2", CultureInfo.InvariantCulture) : "none";
    }
}
