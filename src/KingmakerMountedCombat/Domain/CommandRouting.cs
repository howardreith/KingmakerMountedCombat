using System;

namespace KingmakerMountedCombat.Domain
{
    public enum CommandRoutingAction
    {
        Unchanged,
        RewriteRiderToMount,
        SuppressDuplicateMount
    }

    public sealed class CommandRoutingDecision
    {
        public CommandRoutingDecision(CommandRoutingAction action, string effectiveUnitId)
        {
            Action = action;
            EffectiveUnitId = effectiveUnitId;
        }

        public CommandRoutingAction Action { get; }

        public string EffectiveUnitId { get; }
    }

    public static class CommandRouter
    {
        public static CommandRoutingDecision RouteGroundMove(MountedPair activePair, string originalUnitId)
        {
            if (activePair == null || string.IsNullOrEmpty(originalUnitId))
            {
                return new CommandRoutingDecision(CommandRoutingAction.Unchanged, originalUnitId);
            }

            if (string.Equals(activePair.RiderId, originalUnitId, StringComparison.Ordinal))
            {
                return new CommandRoutingDecision(CommandRoutingAction.RewriteRiderToMount, activePair.MountId);
            }

            if (string.Equals(activePair.MountId, originalUnitId, StringComparison.Ordinal))
            {
                return new CommandRoutingDecision(CommandRoutingAction.SuppressDuplicateMount, null);
            }

            return new CommandRoutingDecision(CommandRoutingAction.Unchanged, originalUnitId);
        }
    }
}
