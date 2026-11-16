using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Known issues sprint 3 continuation (M05): native gamepad stick movement of this pair. The decisions, the
    // ownership record and the bind/settle/retire sequence are MountedContinuousDrivePolicy and
    // MountedContinuousDriveOwner; this file answers their questions from the live pair and applies the native calls
    // they ask for.
    internal sealed partial class GameMountedRelationshipService
    {
        private readonly MountedContinuousDriveOwner continuousDrive;

        internal long ContinuousDriveBindCount => continuousDrive.BindCount;

        internal long ContinuousDriveRetiredCount => continuousDrive.RetiredCount;

        internal long ContinuousDriveOrphanCount => continuousDrive.OrphanCount;

        internal string LastContinuousDriveSettlement => continuousDrive.LastSettlement;

        private MountedContinuousDriveOwner CreateContinuousDrive() => new MountedContinuousDriveOwner(new ContinuousDriveHost(this));

        private bool ContinuousDrivePairMounted =>
            !disposed && coordinator.State == RelationshipState.Mounted && coordinator.ActivePair != null;

        // The unit the native stick drives for this selected unit: the pair's mount for its rider; null keeps the unit.
        internal UnitEntityData ContinuousDriveMover(UnitEntityData selected) =>
            MountedContinuousDrivePolicy.DrivesPhysicalMover(ContinuousDrivePairMounted,
                selected != null && ReferenceEquals(selected, runtime.Rider), runtime.Mount != null) ? runtime.Mount : null;

        // The unit the companions' formation is anchored on while the stick drives this mount: the selected rider.
        internal UnitEntityData ContinuousDriveFormationUnit(UnitEntityData unit, UnitEntityData currentSelection) =>
            MountedContinuousDrivePolicy.FormationAnchorIsRider(ContinuousDrivePairMounted,
                unit != null && ReferenceEquals(unit, runtime.Mount),
                runtime.Rider != null && ReferenceEquals(currentSelection, runtime.Rider)) ? runtime.Rider : null;

        // Native UpdateMovement interrupts the moving orders of the unit it takes over. The stick takes the mount, so
        // the rider's own orders, which that native call no longer reaches, end here in the same way.
        internal void EndRiderOrdersForContinuousDrive()
        {
            if (!ContinuousDrivePairMounted) return;
            CombatOwner?.InterruptRiderApproachingOrders("stick movement");
            runtime.Rider?.Commands?.InterruptMove();
        }

        // After native UnitMoveContiniously.Init.
        internal void BindContinuousDrive(UnitMoveContiniously command) => continuousDrive.Bind(command);

        // After native UnitCommands.Run was called with a continuous command, on whichever container.
        internal void ObserveContinuousDriveRun(UnitCommand command) => continuousDrive.ObserveRun(command);

        // After native UnitCommands.Run was called with any other command on one of this pair's containers.
        internal void ObserveOrderDuringContinuousDrive(UnitCommands container, UnitCommand command)
        {
            if (!continuousDrive.IsBound || container == null || command == null) return;
            var rider = runtime.Rider;
            continuousDrive.ObserveRiderOrder(
                rider != null && ReferenceEquals(container, rider.Commands) && container.Contains(command),
                command.Type == UnitCommand.CommandType.Move || command.Type == UnitCommand.CommandType.Standard);
        }

        private void SettleContinuousDrive() => continuousDrive.Settle(false);

        // Dismount and disposal.
        internal void ReleaseContinuousDrive(string reason) => continuousDrive.Release(reason);

        private sealed class ContinuousDriveHost : IMountedContinuousDriveHost
        {
            private readonly GameMountedRelationshipService owner;

            internal ContinuousDriveHost(GameMountedRelationshipService owner) { this.owner = owner; }

            public bool PairMounted => owner.ContinuousDrivePairMounted;

            public long PairGeneration => owner.mountedPairGeneration;

            public int Frame => Time.frameCount;

            public bool StickInputGated => Game.Instance?.Player != null && Game.Instance.Player.IsInCombat;

            public object Mount => owner.runtime.Mount;

            public object MountView => owner.runtime.Mount?.View;

            public object ExecutorOf(object command) => (command as UnitCommand)?.Executor;

            public bool IsFinished(object command) => (command as UnitCommand)?.IsFinished != false;

            public bool InMountMoveSlot(object command)
            {
                var commands = owner.runtime.Mount?.Commands;
                return command != null && commands != null && ReferenceEquals(commands.GetCommand(UnitCommand.CommandType.Move), command);
            }

            public object OverrideOf(object view)
            {
                var unitView = view as UnitEntityView;
                return ReferenceEquals(unitView, null) ? null : unitView.AgentOverride;
            }

            public bool IsContinuousAgent(object agent) => agent as UnitMovementAgentContinious != null;

            public void Interrupt(object command, bool raiseEndEvent) => (command as UnitCommand)?.Interrupt(raiseEndEvent);

            public void ClearOverride(object view)
            {
                var unitView = view as UnitEntityView;
                if (!ReferenceEquals(unitView, null)) unitView.AgentOverride = null;
            }

            public void ReleaseRiderHold()
            {
                var rider = owner.runtime.Rider;
                if (rider != null) rider.HoldState = false;
            }

            public void Report(MountedContinuousDriveSettlement settlement, string reason, bool warning)
            {
                var text = "Mounted stick movement was retired: settlement=" + settlement + "; reason=" + (reason ?? "<observed>") +
                    "; mountId=" + owner.runtime.Mount?.UniqueId + ".";
                if (warning) owner.logger.Warning(text + " Its command was not in the mount's Move slot; further such retirements are counted, not logged.");
                else owner.logger.Info(text);
            }
        }
    }
}
