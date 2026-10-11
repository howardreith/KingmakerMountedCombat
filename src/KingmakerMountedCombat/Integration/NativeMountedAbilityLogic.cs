using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal static class NativeMountedAbilityBridge
    {
        internal static NativeMountedControlService Service { get; set; }
    }

    internal sealed class NativeMountedAbilityLogic : AbilityCustomLogic,
        IAbilityAvailabilityProvider,
        IAbilityTargetChecker,
        IAbilityVisibilityProvider
    {
        public NativeMountedControlKind Kind;

        private string lastReason = "Mounted control is unavailable.";

        public bool IsAvailableFor(AbilityData ability)
        {
            var availability = NativeMountedAbilityBridge.Service?.Evaluate(Kind, ability?.Caster?.Unit);
            lastReason = availability?.Reason ?? "Mounted control services are unavailable.";
            return availability != null && availability.IsEnabled;
        }

        public string GetReason()
        {
            return lastReason;
        }

        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            return NativeMountedAbilityBridge.Service?.CanTarget(Kind, caster, target?.Unit) ?? false;
        }

        public bool IsAbilityVisible(AbilityData ability)
        {
            return NativeMountedAbilityBridge.Service?.Evaluate(Kind, ability?.Caster?.Unit).IsVisible ?? false;
        }

        public override IEnumerator<AbilityDeliveryTarget> Deliver(
            AbilityExecutionContext context,
            TargetWrapper target)
        {
            var service = NativeMountedAbilityBridge.Service;
            // The exact execution context is the binding Deliver owns: the command that
            // created it has usually already completed by now, so ownership cannot be
            // rediscovered through the caster's current Move slot.
            if (service != null && service.TryDispatch(Kind, context?.Caster, target?.Unit, context))
            {
                yield return new AbilityDeliveryTarget(target);
            }
        }

        public override void Cleanup(AbilityExecutionContext context)
        {
        }
    }
}
