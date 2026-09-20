using System;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Clicks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Stateless admission and execution checks: a queued request never retains
    // an earlier relationship decision. Native commands retain executed costs.
    internal sealed class MountedChargeSafetyService
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly Action<string> feedback;

        internal MountedChargeSafetyService(GameMountedRelationshipService relationship, Action<string> feedback)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        internal string RejectionReason(AbilityData ability, bool alreadyActed = false)
        {
            var blueprint = ability?.Blueprint;
            if (blueprint == null || blueprint.AssetGuid != MountedChargeSafetyPolicy.ChargeBlueprintId) return null;
            return MountedChargeSafetyPolicy.ShouldReject(relationship.State,
                relationship.IsExactActivePairUnit(ability.Caster?.Unit), blueprint.AssetGuid,
                blueprint.GetComponent<AbilityCustomCharge>()?.GetType() == typeof(AbilityCustomCharge), alreadyActed)
                ? MountedChargeSafetyPolicy.Feedback : null;
        }

        internal bool AllowClick(AbilityData ability, bool simulate, bool muteEvents)
        {
            var reason = RejectionReason(ability);
            if (reason == null) return true;
            if (!simulate && !muteEvents && !PointerController.SimulatingClick) feedback(reason);
            return false;
        }

        internal bool AllowAdmission(UnitCommand command)
        {
            var cast = command as UnitUseAbility;
            var reason = cast == null ? null : RejectionReason(cast.Spell, cast.IsActed);
            if (reason == null) return true;
            if (!PointerController.SimulatingClick)
            {
                if (cast.Executor != null && !cast.IsFinished) cast.Interrupt();
                feedback(reason);
            }
            return false;
        }

        internal bool AllowExecution(UnitCommand command)
        {
            // Interrupt before native Tick can mark a failed OnAction as acted.
            // Do not touch a delivered charge, its native cleanup or its costs.
            var cast = command as UnitUseAbility;
            if (cast == null || RejectionReason(cast.Spell, cast.IsActed) == null) return true;
            return cast.IsFinished ? false : AllowAdmission(cast);
        }
    }
}
