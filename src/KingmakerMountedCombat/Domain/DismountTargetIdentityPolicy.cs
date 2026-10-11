using System;

namespace KingmakerMountedCombat.Domain
{
    // A voluntary Dismount targets its own rider and nothing else.
    //
    // The native command records its target at its own Init boundary, and the
    // delivery presents a target again. Both must still be the exact caster, and
    // that caster must still be the live relationship's rider. Kept here, free of
    // engine types, so every rejected condition is directly testable: a missing
    // target, a foreign target, a target that changed after the command was
    // created, a relationship whose rider changed, and a stale generation.
    public static class DismountTargetIdentityPolicy
    {
        public static string Refuse(
            bool targetIsPresent,
            bool targetIsExactCaster,
            string shellTargetId,
            string casterId,
            bool liveRiderIsPresent,
            bool liveRiderIsExactCaster,
            long shellGenerationAtInit,
            long currentRelationshipGeneration)
        {
            if (!targetIsPresent || !targetIsExactCaster)
            {
                return "A dismount must target its own rider.";
            }
            if (string.IsNullOrEmpty(casterId) ||
                !string.Equals(shellTargetId, casterId, StringComparison.Ordinal))
            {
                return "This dismount command was created for a different rider.";
            }
            if (!liveRiderIsPresent || !liveRiderIsExactCaster)
            {
                return "The mounted relationship's rider changed after this dismount was requested.";
            }
            if (shellGenerationAtInit != currentRelationshipGeneration)
            {
                return "The mounted relationship changed after this transition was requested.";
            }
            return null;
        }
    }
}
