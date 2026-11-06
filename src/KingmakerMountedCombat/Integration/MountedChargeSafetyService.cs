using System;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Clicks;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Stateless admission and execution checks: a queued request never retains
    // an earlier relationship decision. Native commands retain executed costs.
    //
    // F2 CTRL-01: for the mounted rider the stock Charge reads availability, reason and targets from the mounted Charge,
    // and its slot click selects the mounted shell (NativeMountedControlService.ResolvePublicSlotDelivery). A stock
    // Charge COMMAND for a mounted pair is still cancelled at admission and execution, never converted.
    internal sealed class MountedChargeSafetyService
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly NativeMountedControlService nativeControls;
        private readonly Action<string> feedback;

        internal MountedChargeSafetyService(GameMountedRelationshipService relationship,
            NativeMountedControlService nativeControls, Action<string> feedback)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.nativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        internal MountedChargeRoute Route(AbilityData ability)
        {
            var blueprint = ability?.Blueprint;
            if (blueprint == null || blueprint.AssetGuid != MountedChargeSafetyPolicy.ChargeBlueprintId) return MountedChargeRoute.Native;
            var caster = ability.Caster?.Unit;
            var owner = relationship.ResolveActorOwner(caster, true);
            return MountedChargeSafetyPolicy.Route(owner?.State ?? RelationshipState.Unmounted,
                owner?.IsExactActivePairUnit(caster) == true, caster != null && caster == owner?.Rider, blueprint.AssetGuid,
                IsExactNativeCharge(ability));
        }

        internal static bool IsExactNativeCharge(AbilityData ability) =>
            ability?.Blueprint != null && ability.Blueprint.AssetGuid == MountedChargeSafetyPolicy.ChargeBlueprintId &&
            ability.Blueprint.GetComponent<AbilityCustomCharge>()?.GetType() == typeof(AbilityCustomCharge);

        // Availability for the ordinary Charge slot: null leaves the native decision in place.
        internal NativeMountedControlAvailability PublicAvailability(AbilityData ability)
        {
            switch (Route(ability))
            {
                case MountedChargeRoute.Mounted:
                    // The native surprise-round exemption names AbilityCustomCharge, which the mounted shell does not
                    // carry: the slot must not advertise a Charge its click cannot run.
                    if (ability.Caster.Unit.IsSurprising())
                        return new NativeMountedControlAvailability(true, false, MountedChargeSafetyPolicy.SurpriseFeedback);
                    var mounted = nativeControls.Evaluate(NativeMountedControlKind.MountedCharge, ability.Caster.Unit);
                    return new NativeMountedControlAvailability(mounted.IsVisible, mounted.IsEnabled, mounted.IsTransitionReady,
                        MountedChargeSafetyPolicy.PresentPublicReason(mounted.Reason));
                case MountedChargeRoute.Refused:
                    return new NativeMountedControlAvailability(true, false, MountedChargeSafetyPolicy.MountFeedback);
                default:
                    return null;
            }
        }

        // Target check for the ordinary Charge: null leaves the native decision in place.
        internal bool? PublicCanTarget(AbilityData ability, UnitEntityData target)
        {
            switch (Route(ability))
            {
                case MountedChargeRoute.Mounted:
                    return nativeControls.CanTarget(NativeMountedControlKind.MountedCharge, ability.Caster.Unit, target);
                case MountedChargeRoute.Refused:
                    return false;
                default:
                    return null;
            }
        }

        internal string RejectionReason(AbilityData ability, bool alreadyActed = false)
        {
            var blueprint = ability?.Blueprint;
            if (blueprint == null || blueprint.AssetGuid != MountedChargeSafetyPolicy.ChargeBlueprintId) return null;
            var caster = ability.Caster?.Unit;
            var owner = relationship.ResolveActorOwner(caster, true);
            if (!MountedChargeSafetyPolicy.ShouldReject(owner?.State ?? RelationshipState.Unmounted,
                owner?.IsExactActivePairUnit(caster) == true, blueprint.AssetGuid,
                IsExactNativeCharge(ability), alreadyActed)) return null;
            return caster != null && caster == owner?.Rider ? MountedChargeSafetyPolicy.Feedback : MountedChargeSafetyPolicy.MountFeedback;
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
                // Automatic use is refused quietly; a player's own order gets the reason.
                if (cast.AiAction == null) feedback(reason);
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
