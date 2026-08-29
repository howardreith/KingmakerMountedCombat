using System;
using System.Globalization;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedAnimationSnapshot
    {
        public int DelegatedLocomotionRestoreCount { get; set; }
        public string LastDelegatedLocomotionSource { get; set; }
        public float LastDelegatedLocomotionSpeed { get; set; }
        public int HorsePrimaryHandleCreateCount { get; set; }
        public int HorsePrimaryHandleRejectCount { get; set; }
        public string LastHorsePrimaryActionName { get; set; }
        public string LastHorsePrimaryActionType { get; set; }
    }

    /// <summary>
    /// Pair-scoped animation bridges for two exact Kingmaker gaps: delegated
    /// mount locomotion on the rider's TB turn, and the native Horse's
    /// additional-limb Bite having no stock attack-animation classification.
    /// Neither bridge changes rules, commands, timing, blueprints, or assets.
    /// </summary>
    internal sealed class MountedAnimationAdapter
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedCombatController combat;
        private readonly IModLogger logger;
        private UnitMoveTo lastLoggedLocomotionMove;

        internal MountedAnimationAdapter(
            GameMountedRelationshipService relationship,
            MountedCombatController combat,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal int DelegatedLocomotionRestoreCount { get; private set; }

        internal string LastDelegatedLocomotionSource { get; private set; } = "<none>";

        internal float LastDelegatedLocomotionSpeed { get; private set; }

        internal int HorsePrimaryHandleCreateCount { get; private set; }

        internal int HorsePrimaryHandleRejectCount { get; private set; }

        internal string LastHorsePrimaryActionName { get; private set; } = "<none>";

        internal string LastHorsePrimaryActionType { get; private set; } = "<none>";

        internal void RestoreExactDelegatedMountLocomotion(UnitAnimationManager manager)
        {
            UnitMoveTo move;
            string source;
            var mount = manager?.View?.EntityData;
            if (!combat.TryGetExactRiderTurnDelegatedMoveForAnimation(mount, out move, out source))
            {
                return;
            }

            var view = relationship.Mount?.View;
            var agent = view?.MovementAgent;
            if (view == null || agent == null ||
                !agent.IsReallyMoving && !agent.WantsToMove ||
                relationship.Mount?.Descriptor?.State == null ||
                !relationship.Mount.Descriptor.State.CanMove)
            {
                return;
            }

            var speed = agent.Speed * view.GetSpeedAnimationCoeff(manager.WalkSpeedType, manager.IsInCombat);
            if (!float.IsNaN(speed) && !float.IsInfinity(speed) && speed > 0f)
            {
                manager.Speed = speed;
                DelegatedLocomotionRestoreCount++;
                LastDelegatedLocomotionSource = source;
                LastDelegatedLocomotionSpeed = speed;
                if (!ReferenceEquals(lastLoggedLocomotionMove, move))
                {
                    lastLoggedLocomotionMove = move;
                    logger.Info("Restored exact delegated mount TB locomotion animation: source=" +
                        source + "; mountId=" + mount.UniqueId + "; speed=" +
                        speed.ToString("R", CultureInfo.InvariantCulture) + ".");
                }
            }
        }

        internal void SupplyExactHorsePrimaryAnimation(AttackHandInfo attack)
        {
            MountedPairAttackCommand command;
            UnitEntityData horse;
            if (attack == null || attack.AnimationHandle != null ||
                !combat.TryGetExactHorsePrimaryAnimationContext(attack, out command, out horse))
            {
                return;
            }

            var manager = horse.View?.AnimationManager;
            var actions = manager?.ActionSet
                .OfType<UnitAnimationActionSpecialAttack>()
                .Where(candidate => candidate != null)
                .ToArray() ?? new UnitAnimationActionSpecialAttack[0];
            if (actions.Length != 1)
            {
                HorsePrimaryHandleRejectCount++;
                logger.Error("Exact KMC Horse primary animation rejected: expected one native SpecialAttack action, observed " +
                    actions.Length + ".");
                return;
            }

            var action = actions[0];
            var handle = manager.CreateHandle(action) as UnitAnimationActionHandle;
            if (handle == null)
            {
                HorsePrimaryHandleRejectCount++;
                logger.Error("Exact KMC Horse primary animation rejected: native SpecialAttack handle creation returned null.");
                return;
            }

            handle.SpecialAttackCount = 0;
            if (handle.VariantsCount > 0)
            {
                handle.Variant = Math.Abs(Time.frameCount) % handle.VariantsCount;
            }
            attack.AnimationHandle = handle;
            command.RecordHorsePrimaryAnimation(handle, action);
            HorsePrimaryHandleCreateCount++;
            LastHorsePrimaryActionName = action.name ?? "<unnamed>";
            LastHorsePrimaryActionType = action.Type.ToString();
            logger.Info("Supplied one native Horse SpecialAttack handle for the exact KMC Bite: action=" +
                LastHorsePrimaryActionName + "; variant=" + handle.Variant + ".");
        }

        internal MountedAnimationSnapshot CaptureSnapshot()
        {
            return new MountedAnimationSnapshot
            {
                DelegatedLocomotionRestoreCount = DelegatedLocomotionRestoreCount,
                LastDelegatedLocomotionSource = LastDelegatedLocomotionSource,
                LastDelegatedLocomotionSpeed = LastDelegatedLocomotionSpeed,
                HorsePrimaryHandleCreateCount = HorsePrimaryHandleCreateCount,
                HorsePrimaryHandleRejectCount = HorsePrimaryHandleRejectCount,
                LastHorsePrimaryActionName = LastHorsePrimaryActionName,
                LastHorsePrimaryActionType = LastHorsePrimaryActionType
            };
        }
    }
}
