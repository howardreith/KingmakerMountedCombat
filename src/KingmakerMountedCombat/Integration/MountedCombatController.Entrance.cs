using System;
using System.Linq;
using Kingmaker;
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
        internal MountedEntranceCarryDecision LastEntranceCarryDecision { get; private set; }

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
            if (admitted) EntranceCarryCount++;
            logger.Info("Mounted entrance approach: " + (admitted ? "the mount carries the rider as its native group member" :
                "the mount's member command was not admitted; the rider stays with the native group") + "; " + detail + ".");
        }

        private static string EntranceFormat(Vector3 v) => "(" + v.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.z.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ")";

        private static string EntranceMetres(Vector3 a, Vector3 b) =>
            new Vector2(a.x - b.x, a.z - b.z).magnitude.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
