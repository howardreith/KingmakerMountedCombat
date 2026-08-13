using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MountedRelationshipCoordinator : IDisposable
    {
        private readonly IMountedPairRuntime runtime;
        private MountedPair pair;
        private bool movementAuthorityAcquired;
        private bool presentationAttached;
        private bool invalidationRequested;
        private CleanupTrigger invalidationTrigger;

        public MountedRelationshipCoordinator(IMountedPairRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            State = RelationshipState.Unmounted;
        }

        public RelationshipState State { get; private set; }

        public MountedPair ActivePair => pair;

        public TransitionResult Mount(MountedPairCandidate candidate)
        {
            if (State == RelationshipState.Disposed)
            {
                return Failure("Coordinator is disposed.");
            }

            if (State != RelationshipState.Unmounted)
            {
                return Failure("A Phase 1 pair is already active or transitioning.");
            }

            State = RelationshipState.Validating;
            var validationError = candidate == null ? "Pair candidate is required." : candidate.Validate();
            if (validationError != null)
            {
                State = RelationshipState.Unmounted;
                return Failure(validationError);
            }

            pair = new MountedPair(candidate.RiderId, candidate.MountId);
            State = RelationshipState.Mounting;
            invalidationRequested = false;
            invalidationTrigger = CleanupTrigger.Manual;
            try
            {
                movementAuthorityAcquired = true;
                runtime.AcquireMovementAuthority(pair);
                ThrowIfInvalidatedDuringMount();
                presentationAttached = true;
                runtime.AttachPresentation(pair);
                ThrowIfInvalidatedDuringMount();
                State = RelationshipState.Mounted;
                return Success(null);
            }
            catch (Exception exception)
            {
                State = RelationshipState.Faulted;
                var trigger = invalidationRequested ? invalidationTrigger : CleanupTrigger.Exception;
                return Cleanup(trigger, exception);
            }
        }

        public TransitionResult Dismount(CleanupTrigger trigger)
        {
            if (State == RelationshipState.Disposed)
            {
                return new TransitionResult(true, State, trigger, new string[0], false, false);
            }

            if (State == RelationshipState.Unmounted && pair == null && !movementAuthorityAcquired && !presentationAttached)
            {
                return Success(trigger);
            }

            if (State == RelationshipState.Mounting || State == RelationshipState.Validating)
            {
                invalidationRequested = true;
                invalidationTrigger = CleanupTriggerPriority.Higher(invalidationTrigger, trigger);
                return new TransitionResult(true, State, invalidationTrigger, new string[0], movementAuthorityAcquired, presentationAttached);
            }

            return Cleanup(trigger, null);
        }

        public void Dispose()
        {
            if (State == RelationshipState.Disposed)
            {
                return;
            }

            var result = Cleanup(CleanupTrigger.ModDisabled, null);
            if (!result.MovementAuthorityResidual && !result.PresentationResidual)
            {
                State = RelationshipState.Disposed;
            }
        }

        private TransitionResult Cleanup(CleanupTrigger trigger, Exception originalException)
        {
            State = RelationshipState.Dismounting;
            var errors = new List<string>();
            var cleanupFailed = false;
            if (originalException != null)
            {
                errors.Add(originalException.GetType().Name + ": " + originalException.Message);
            }

            if (presentationAttached)
            {
                try
                {
                    runtime.RestorePresentation(pair);
                    presentationAttached = false;
                }
                catch (Exception exception)
                {
                    cleanupFailed = true;
                    errors.Add("RestorePresentation: " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            if (movementAuthorityAcquired)
            {
                try
                {
                    runtime.RestoreMovementAuthority(pair, trigger);
                    movementAuthorityAcquired = false;
                }
                catch (Exception exception)
                {
                    cleanupFailed = true;
                    errors.Add("RestoreMovementAuthority: " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            invalidationRequested = false;
            var hasResidue = movementAuthorityAcquired || presentationAttached;
            if (!hasResidue)
            {
                pair = null;
            }

            State = !cleanupFailed && !hasResidue ? RelationshipState.Unmounted : RelationshipState.Faulted;
            var transitionSucceeded = originalException == null && State == RelationshipState.Unmounted;
            return new TransitionResult(transitionSucceeded, State, trigger, errors, movementAuthorityAcquired, presentationAttached);
        }

        private void ThrowIfInvalidatedDuringMount()
        {
            if (invalidationRequested)
            {
                throw new InvalidOperationException("Pair invalidated during mounting: " + invalidationTrigger + ".");
            }
        }

        private TransitionResult Success(CleanupTrigger? trigger)
        {
            return new TransitionResult(true, State, trigger, new string[0], movementAuthorityAcquired, presentationAttached);
        }

        private TransitionResult Failure(string message)
        {
            return new TransitionResult(false, State, null, new[] { message }, movementAuthorityAcquired, presentationAttached);
        }
    }
}
