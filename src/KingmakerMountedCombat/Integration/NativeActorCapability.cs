using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // The only reading of an actor's native permission to act for a mounted attack. Decisions use condition state
    // alone; the fuller native readings in Describe are diagnostic and never decide anything.
    internal static class NativeActorCapability
    {
        internal static MountedActorCapabilityFacts Read(UnitEntityData actor)
        {
            var state = actor?.Descriptor?.State;
            if (state == null) return default(MountedActorCapabilityFacts);
            return new MountedActorCapabilityFacts
            {
                Known = true,
                Helpless = state.IsHelpless,
                Stunned = state.HasCondition(UnitCondition.Stunned),
                Dazed = state.HasCondition(UnitCondition.Dazed),
                CantAct = state.HasCondition(UnitCondition.CantAct),
                CanNotAttack = state.HasCondition(UnitCondition.CanNotAttack)
            };
        }

        // One bounded record of why native would not run this actor's command, taken when the command ends.
        internal static string Describe(UnitEntityData actor, UnitCommand command)
        {
            var facts = default(MountedActorCapabilityFacts);
            try { facts = Read(actor); }
            catch (Exception exception) { return "<unavailable:" + exception.GetType().Name + ">"; }
            if (!facts.Known) return "<unavailable>";
            return "block=" + MountedActorCapabilityPolicy.ExecutionBlock(facts) +
                ";helpless=" + facts.Helpless + ";stunned=" + facts.Stunned + ";dazed=" + facts.Dazed +
                ";cantAct=" + facts.CantAct + ";canNotAttack=" + facts.CanNotAttack +
                ";nauseated=" + Observe(() => actor.Descriptor.State.HasCondition(UnitCondition.Nauseated)) +
                ";prone=" + Observe(() => actor.Descriptor.State.Prone.Active) +
                ";nativeCanAct=" + Observe(() => actor.Descriptor.State.CanAct) +
                ";canActInCombat=" + Observe(() => actor.CombatState.CanActInCombat) +
                ";commandCooldown=" + Observe(() => command != null && actor.CombatState.HasCooldownForCommand(command)) +
                ";standard=" + Observe(() => actor.HasStandardAction()) +
                ";turnBased=" + Observe(() => CombatController.IsInTurnBasedCombat());
        }

        private static string Observe(Func<bool> read)
        {
            try { return read().ToString(); }
            catch (Exception exception) { return "<" + exception.GetType().Name + ">"; }
        }
    }
}
