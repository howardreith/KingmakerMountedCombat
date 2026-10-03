using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the component of the original KMC Mounted Charge ability. The rider owns this
    // native full-round Standard shell and its cost; the mount owns the forced path; the engine owns every
    // rule. This component only asks the control service and reports exact reasons; it writes nothing.
    //
    // IsEngageUnit stays false, deliberately. The stock AbilityCustomCharge needs an engage-unit process
    // because one caster both moves and attacks, so the command must outlive the approach. Here the mover and
    // the attacker are two separate native commands: the shell commits the charge and queues the already
    // qualified pair attack transaction first on the rider with IgnoreCooldown, and that transaction owns the
    // mount-delegated approach, its bounded termination and its restoration. Holding a second long-lived
    // process would add a lifetime that could strand with nothing to end it.
    internal sealed class MountedChargeAbilityLogic : AbilityCustomLogic,
        IAbilityAvailabilityProvider,
        IAbilityTargetChecker,
        IAbilityMinRangeProvider,
        IAbilityVisibilityProvider
    {
        private string lastReason = "Mounted Charge is unavailable.";

        public bool IsAvailableFor(AbilityData ability)
        {
            var availability = NativeMountedAbilityBridge.Service?.Evaluate(NativeMountedControlKind.MountedCharge, ability?.Caster?.Unit);
            lastReason = availability?.Reason ?? "Mounted control services are unavailable.";
            return availability != null && availability.IsEnabled;
        }

        public string GetReason()
        {
            return lastReason;
        }

        public bool IsAbilityVisible(AbilityData ability)
        {
            return NativeMountedAbilityBridge.Service?.Evaluate(NativeMountedControlKind.MountedCharge, ability?.Caster?.Unit).IsVisible ?? false;
        }

        // The stock minimum charge distance, read from the mount because the mount is the mover.
        public float GetMinRangeMeters(UnitEntityData caster)
        {
            return NativeMountedAbilityBridge.Service?.GetMountedChargeMinimumRange(caster) ?? 0f;
        }

        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            return NativeMountedAbilityBridge.Service?.CanTarget(NativeMountedControlKind.MountedCharge, caster, target?.Unit) ?? false;
        }

        public override IEnumerator<AbilityDeliveryTarget> Deliver(
            AbilityExecutionContext context,
            TargetWrapper target)
        {
            var service = NativeMountedAbilityBridge.Service;
            if (service != null && service.TryDispatch(NativeMountedControlKind.MountedCharge, context?.Caster, target?.Unit, context))
            {
                yield return new AbilityDeliveryTarget(target);
            }
        }

        public override void Cleanup(AbilityExecutionContext context)
        {
            // The queued pair attack transaction owns the charge lease and its restoration; the shell has no
            // state of its own to clean up, and must not touch the mount agent after queuing.
        }
    }
}
