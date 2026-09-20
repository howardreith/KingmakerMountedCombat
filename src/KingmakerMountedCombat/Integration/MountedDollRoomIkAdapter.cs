using System;
using Kingmaker;
using Kingmaker.Visual.Animation;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedDollRoomIkSnapshot
    {
        public int ExactBindingCount { get; set; }
        public int ExactSetupStartCount { get; set; }
        public int ExactSetupCompleteCount { get; set; }
        public string LastUnitId { get; set; }

        public string LastUnitRole { get; set; }
    }

    /// <summary>
    /// Repairs only the stock DollRoom clone's missing UnitEntityView back-link
    /// while one member of the exact KMC mounted pair is previewed. The stock
    /// FBBIK setup then runs unchanged and any exception still propagates normally.
    /// </summary>
    internal sealed class MountedDollRoomIkAdapter
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly IModLogger logger;

        internal MountedDollRoomIkAdapter(
            GameMountedRelationshipService relationship,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal int ExactBindingCount { get; private set; }

        internal int ExactSetupStartCount { get; private set; }

        internal int ExactSetupCompleteCount { get; private set; }

        internal string LastUnitId { get; private set; } = "<none>";

        internal string LastUnitRole { get; private set; } = "<none>";

        internal void BindExactMountedRiderIfRequired(IKController controller)
        {
            if (controller == null || !controller.IsDollRoom || controller.CharacterUnitEntity != null ||
                relationship.State != RelationshipState.Mounted)
            {
                return;
            }

            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var dollRoom = Game.Instance?.UI?.Common?.DollRoom;
            var unit = dollRoom?.Unit;
            if (unit == null ||
                (!ReferenceEquals(unit, rider) && !ReferenceEquals(unit, mount)) ||
                unit.View == null)
            {
                return;
            }

            controller.CharacterUnitEntity = unit.View;
            ExactBindingCount++;
            LastUnitId = unit.UniqueId;
            LastUnitRole = ReferenceEquals(unit, rider) ? "rider" : "mount";
            logger.Info("Bound the exact mounted pair UnitEntityView to the stock DollRoom IK clone before FBBIK setup: role=" +
                LastUnitRole + "; unitId=" + LastUnitId + ".");
        }

        internal bool BeginExactFbbikObservation(IKController controller)
        {
            var exact = IsExactMountedRiderDollRoom(controller);
            if (exact)
            {
                ExactSetupStartCount++;
            }
            return exact;
        }

        internal void CompleteExactFbbikObservation(bool exact)
        {
            if (exact)
            {
                ExactSetupCompleteCount++;
            }
        }

        internal MountedDollRoomIkSnapshot CaptureSnapshot()
        {
            return new MountedDollRoomIkSnapshot
            {
                ExactBindingCount = ExactBindingCount,
                ExactSetupStartCount = ExactSetupStartCount,
                ExactSetupCompleteCount = ExactSetupCompleteCount,
                LastUnitId = LastUnitId,
                LastUnitRole = LastUnitRole
            };
        }

        private bool IsExactMountedRiderDollRoom(IKController controller)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var unit = Game.Instance?.UI?.Common?.DollRoom?.Unit;
            return controller != null && controller.IsDollRoom &&
                relationship.State == RelationshipState.Mounted &&
                unit?.View != null &&
                (ReferenceEquals(unit, rider) || ReferenceEquals(unit, mount)) &&
                controller.CharacterUnitEntity == unit.View;
        }
    }
}
