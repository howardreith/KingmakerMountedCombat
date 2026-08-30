using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    /// <summary>
    /// Supplies the exact native Horse SpecialAttack handle that Kingmaker does
    /// not classify for the Horse's additional-limb Bite. This adapter changes
    /// animation presentation only; the stock UnitAttack remains responsible
    /// for command execution, rules, timing, and damage.
    /// </summary>
    internal sealed class HorsePrimaryAttackAnimationAdapter
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly IModLogger logger;

        internal HorsePrimaryAttackAnimationAdapter(
            GameMountedRelationshipService relationship,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal int HandleCreateCount { get; private set; }

        internal int HandleRejectCount { get; private set; }

        internal string LastActionName { get; private set; } = "<none>";

        internal string LastActionType { get; private set; } = "<none>";

        internal void SupplyExact(
            MountedPairAttackCommand command,
            AttackHandInfo attack,
            UnitEntityData horse)
        {
            if (!IsExactHorsePrimaryContext(command, attack, horse) || attack.AnimationHandle != null)
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
                HandleRejectCount++;
                logger.Error("Exact KMC Horse primary animation rejected: expected one native SpecialAttack action, observed " +
                    actions.Length + ".");
                return;
            }

            var action = actions[0];
            var handle = manager.CreateHandle(action) as UnitAnimationActionHandle;
            if (handle == null)
            {
                HandleRejectCount++;
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
            HandleCreateCount++;
            LastActionName = action.name ?? "<unnamed>";
            LastActionType = action.Type.ToString();
            logger.Info("Supplied one native Horse SpecialAttack handle for the exact KMC Bite: action=" +
                LastActionName + "; variant=" + handle.Variant + ".");
        }

        private bool IsExactHorsePrimaryContext(
            MountedPairAttackCommand command,
            AttackHandInfo attack,
            UnitEntityData horse)
        {
            return relationship.State == RelationshipState.Mounted &&
                command != null && !command.IsFinished &&
                command.Action == MountedCombatActionKind.MountPrimaryNatural &&
                horse != null && ReferenceEquals(horse, relationship.Mount) &&
                string.Equals(horse.Blueprint?.AssetGuid,
                    HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal) &&
                command.ActionActor == horse && command.ChildAttack != null &&
                ReferenceEquals(command.ChildAttack.PlannedAttack, attack) &&
                attack?.Hand?.Owner?.Unit == horse &&
                ReferenceEquals(attack.Weapon, command.ChildAttack.PlannedAttack.Weapon);
        }
    }
}
