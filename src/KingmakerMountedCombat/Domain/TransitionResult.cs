using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public sealed class TransitionResult
    {
        public TransitionResult(bool succeeded, RelationshipState state, CleanupTrigger? trigger, IReadOnlyList<string> errors, bool movementAuthorityResidual, bool presentationResidual)
        {
            Succeeded = succeeded;
            State = state;
            Trigger = trigger;
            Errors = errors;
            MovementAuthorityResidual = movementAuthorityResidual;
            PresentationResidual = presentationResidual;
        }

        public bool Succeeded { get; }

        public RelationshipState State { get; }

        public CleanupTrigger? Trigger { get; }

        public IReadOnlyList<string> Errors { get; }

        public bool MovementAuthorityResidual { get; }

        public bool PresentationResidual { get; }
    }
}
