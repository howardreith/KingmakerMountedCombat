using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPairSingleAttack : UnitAttack
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly bool usesMountedRiderReach;

        public MountedPairSingleAttack(
            UnitEntityData target,
            UnitEntityData rider,
            UnitEntityData mount,
            bool usesMountedRiderReach)
            : base(target)
        {
            this.rider = rider;
            this.mount = mount;
            this.usesMountedRiderReach = usesMountedRiderReach;
            IsSingleAttack = true;
            CreatedByPlayer = true;
            IgnoreCooldown();
        }

        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            float pairRadius;
            if (TryCalculatePairApproachRadius(Target, out pairRadius))
            {
                ApproachRadius = pairRadius;
            }
        }

        internal bool TryCalculatePairApproachRadius(UnitEntityData target, out float radius)
        {
            radius = 0f;
            if (!usesMountedRiderReach ||
                Executor != rider ||
                rider == null ||
                mount == null ||
                target == null ||
                rider.View == null ||
                mount.View == null ||
                target.View == null ||
                PlannedAttack == null)
            {
                return false;
            }

            radius = mount.View.Corpulence + target.View.Corpulence + PlannedAttack.WeaponRange;
            return radius >= 0f;
        }

        internal float PairApproachRadius => ApproachRadius;
    }
}
