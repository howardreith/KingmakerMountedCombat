using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Entrance repair (2026-11-12): carry the rider to an area entrance (see MountedEntranceCarryPolicy for the native
    // contract). Runs from the UnitCommands.Run postfix after native Run executed the rider's own UnitAreaTransition.
    // The rider's command is never replaced, held or completed here; the horse receives the native member command it
    // would have received had selection not projected it onto the rider, in the same group and at the rider's slot,
    // so arrival, replacement, cancellation (UnitGroupCommand.OnEnded interrupts the group) and the single group
    // dispatch stay native.
    internal sealed partial class MountedCombatController
    {
        internal int EntranceCarryCount { get; private set; }
        internal int EntranceRiderPointerRetiredCount { get; private set; }
        internal long EntranceRiderPointerSkippedCount { get; private set; }
        internal MountedEntranceCarryDecision LastEntranceCarryDecision { get; private set; }

        // Known issues sprint 3 (M01): the exact member command issued below, for the native group acceleration offer
        // (MountedCombatController.GroupPace.cs). Identity only; the command stays native in every other respect.
        private readonly MountedEntranceCarrierWatch entranceCarrier = new MountedEntranceCarrierWatch();

        internal void CarryRiderEntranceTransition(UnitCommands container, UnitCommand command)
        {
            if (disposed || command == null || container == null) return;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var transition = command as UnitAreaTransition;
            var group = transition?.GroupCommand as AreaTransitionGroupCommand;
            var exact = transition != null && command.GetType() == typeof(UnitAreaTransition) && group != null;
            var riderContainer = rider != null && ReferenceEquals(container, rider.Commands);
            var mounted = relationship.State == RelationshipState.Mounted && mount != null;
            var mountCommands = mount?.Commands;
            var decision = MountedEntranceCarryPolicy.Decide(
                mounted, riderContainer, exact, Game.Instance?.Player?.IsInCombat ?? true,
                !command.IsFinished && (container.Contains(command) || container.Queue.Contains(command)),
                group != null && !group.IsStarted && !group.IsFinished,
                group?.Units != null && mount != null && group.Units.Contains(mount),
                mountCommands != null && mountCommands.Raw.Concat(mountCommands.Queue).OfType<UnitAreaTransition>()
                    .Any(c => c != null && !c.IsFinished && ReferenceEquals(c.GroupCommand, group)));
            if (decision == MountedEntranceCarryDecision.NotApplicable) return;
            LastEntranceCarryDecision = decision;
            var target = command.Target != null ? command.Target.Point : rider.Position;
            var detail = "riderId=" + rider.UniqueId + "; mountId=" + mount.UniqueId + "; members=" + (group.Units?.Count ?? 0) +
                "; transition=" + (transition.AreaTransition != null ? transition.AreaTransition.name : "none") +
                "; target=" + EntranceFormat(target) + "; riderToTarget=" + EntranceMetres(rider.Position, target) + "; mountToTarget=" + EntranceMetres(mount.Position, target);
            if (decision != MountedEntranceCarryDecision.Carry)
            {
                if (decision != MountedEntranceCarryDecision.NotApplicable)
                    logger.Info("Mounted entrance approach: no carrier issued (" + decision + "); " + detail + ".");
                return;
            }
            var carried = new UnitAreaTransition(group, target) { SpeedLimit = command.SpeedLimit, CreatedByPlayer = command.CreatedByPlayer };
            mountCommands.Run(carried);
            var admitted = !carried.IsFinished && (mountCommands.Contains(carried) || mountCommands.Queue.Contains(carried));
            if (admitted)
            {
                EntranceCarryCount++;
                entranceCarrier.Bind(carried, group, relationship.MountedPairGeneration);
            }
            // Known issues sprint 3 (M04): the rider keeps its own live member command while the mount walks, and
            // native shows a unit's old destination pointer, where it was last placed, for as long as the unit holds
            // one. The pair's marker is the mount's (MountedDestinationMarkerPolicy); the rider's own new placement
            // is answered by SkipCarriedRiderPointer below.
            var retired = false;
            try { retired = MountedDestinationMarkers.RetireRiderPointer(rider, admitted); }
            catch (Exception error) { logger.Exception("Mounted entrance approach: the rider's old destination pointer was not retired", error); }
            if (retired) EntranceRiderPointerRetiredCount++;
            logger.Info("Mounted entrance approach: " + (admitted ? "the mount carries the rider as its native group member" :
                "the mount's member command was not admitted; the rider stays with the native group") + "; " + detail +
                "; riderPointerRetired=" + retired + ".");
        }

        // From the controller's update: a carrier that ended, or that belonged to an earlier mounted pair, is let go.
        private void ReleaseEntranceCarrier()
        {
            var carrier = entranceCarrier.Carrier as UnitCommand;
            if (carrier == null) return;
            entranceCarrier.ReleaseIfEnded(carrier.IsFinished, relationship.State == RelationshipState.Mounted, relationship.MountedPairGeneration);
        }

        // Known issues sprint 3 (M04). From the prefix of native UnitEntityView.OnMovementStarted, whose whole body is
        // the placement of that unit's destination pointer. True skips it: the unit is this pair's rider and the mount
        // executes the member command the carry issued for the rider's own live member command.
        internal bool SkipCarriedRiderPointer(UnitEntityData unit)
        {
            if (disposed || unit == null) return false;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var isRider = ReferenceEquals(unit, rider);
            var skip = MountedDestinationMarkerPolicy.CarriedRiderPlacesNoPointer(
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                isRider,
                isRider && ClassifyMountGroupOrder(rider, mount, out var order) == MountedGroupPaceOrder.CarriedEntranceOrder && !order.IsFinished);
            if (skip) EntranceRiderPointerSkippedCount++;
            return skip;
        }

        private static string EntranceFormat(Vector3 v) => "(" + v.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.z.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ")";

        private static string EntranceMetres(Vector3 a, Vector3 b) =>
            new Vector2(a.x - b.x, a.z - b.z).magnitude.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
