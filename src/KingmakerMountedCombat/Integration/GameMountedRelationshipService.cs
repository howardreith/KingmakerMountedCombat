using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class GameMountedRelationshipService : IDisposable
    {
        private readonly IModLogger logger;
        private readonly DiagnosticSettings settings;
        private readonly KingmakerMountedPairRuntime runtime;
        private readonly MountedRelationshipCoordinator coordinator;
        private bool cleanupRetryRequired;
        private CleanupTrigger cleanupRetryTrigger = CleanupTrigger.Exception;
        private bool disposed;

        public GameMountedRelationshipService(IModLogger logger, DiagnosticSettings settings)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            runtime = new KingmakerMountedPairRuntime(logger, settings);
            coordinator = new MountedRelationshipCoordinator(runtime);
        }

        public RelationshipState State => coordinator.State;

        public UnitEntityData Rider => runtime.Rider;

        public UnitEntityData Mount => runtime.Mount;

        internal KingmakerMountedPairRuntime Runtime => runtime;

        public long RiderGroundPlacementSuppressionCount { get; private set; }

        public string LastResult { get; private set; } = "No diagnostic action has run.";

        public TransitionResult LastTransition { get; private set; }

        public TransitionResult MountSelectedRider()
        {
            ThrowIfDisposed();
            if (!settings.EnableUnsafeMovementExperiment)
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Movement experiment is disabled." }, false, false));
            }

            if (coordinator.State != RelationshipState.Unmounted)
            {
                return Record(coordinator.Mount(null));
            }

            var selection = SelectionManager.Instance?.SelectedUnits;
            if (selection == null || selection.Count != 1 || selection[0] == null)
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Select exactly one Medium rider with an active rank-7+ Mammoth companion." }, false, false));
            }

            var rider = selection[0];
            var mount = rider.Descriptor?.Pet;
            if (mount == null)
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Selected rider has no active companion." }, false, false));
            }

            runtime.Prepare(rider, mount);
            var result = coordinator.Mount(runtime.CreateCandidate());
            ObserveCleanupState(result);
            if (!result.Succeeded)
            {
                runtime.ClearPreparedPairWhenUnmounted();
            }
            return Record(result);
        }

        public bool TryResolveAutomationPair(out UnitEntityData rider, out UnitEntityData mount, out string error)
        {
            ThrowIfDisposed();
            rider = null;
            mount = null;
            error = null;
            var party = Game.Instance?.Player?.Party;
            if (party == null)
            {
                error = "Loaded party is unavailable.";
                return false;
            }

            var candidates = party.Where(unit =>
                unit != null && unit.Descriptor?.Pet != null && unit.Descriptor.Pet.Blueprint != null &&
                string.Equals(unit.Descriptor.Pet.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal)).ToList();
            if (candidates.Count != 1)
            {
                error = "Expected exactly one party rider with the exact Mammoth active companion; observed " + candidates.Count + ".";
                return false;
            }

            rider = candidates[0];
            mount = rider.Descriptor.Pet;
            var candidate = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
            error = candidate == null ? "Automation pair candidate could not be created." : candidate.Validate();
            if (error != null)
            {
                rider = null;
                mount = null;
                return false;
            }

            return true;
        }

        public TransitionResult MountAutomationPair()
        {
            ThrowIfDisposed();
            if (!settings.EnableUnsafeMovementExperiment)
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Movement experiment is disabled." }, false, false));
            }
            if (coordinator.State != RelationshipState.Unmounted)
            {
                return Record(coordinator.Mount(null));
            }

            UnitEntityData rider;
            UnitEntityData mount;
            string error;
            if (!TryResolveAutomationPair(out rider, out mount, out error))
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { error }, false, false));
            }

            runtime.Prepare(rider, mount);
            var result = coordinator.Mount(runtime.CreateCandidate());
            ObserveCleanupState(result);
            if (!result.Succeeded)
            {
                runtime.ClearPreparedPairWhenUnmounted();
            }
            return Record(result);
        }

        internal TransitionResult RejectSyntheticInvalidPairForAutomation()
        {
            ThrowIfDisposed();
            if (coordinator.State != RelationshipState.Unmounted)
            {
                return Record(new TransitionResult(false, coordinator.State, null,
                    new[] { "Synthetic invalid-pair probe requires Unmounted state." }, false, false));
            }

            return Record(coordinator.Mount(new MountedPairCandidate("kmc-invalid-same-unit", "kmc-invalid-same-unit")));
        }

        public TransitionResult Dismount(CleanupTrigger trigger)
        {
            if (disposed && coordinator.State == RelationshipState.Disposed)
            {
                return new TransitionResult(true, coordinator.State, trigger, new string[0], false, false);
            }

            var result = coordinator.Dismount(trigger);
            ObserveCleanupState(result);
            runtime.ClearPreparedPairWhenUnmounted();
            return Record(result);
        }

        public bool RouteGroundCommand(ref UnitEntityData unit)
        {
            if (unit == null || coordinator.State != RelationshipState.Mounted || coordinator.ActivePair == null)
            {
                return true;
            }

            var decision = CommandRouter.RouteGroundMove(coordinator.ActivePair, unit.UniqueId);
            if (decision.Action == CommandRoutingAction.SuppressDuplicateMount)
            {
                return false;
            }

            if (decision.Action == CommandRoutingAction.RewriteRiderToMount)
            {
                if (runtime.Mount == null)
                {
                    Dismount(CleanupTrigger.Exception);
                    return false;
                }
                unit = runtime.Mount;
            }

            return true;
        }

        public bool NormalizeSingleSelection(ref UnitEntityView view, bool single)
        {
            if (coordinator.State == RelationshipState.Mounted && runtime.Mount?.View == view && runtime.Rider?.View != null)
            {
                view = runtime.Rider.View;
            }

            if (!single && coordinator.State == RelationshipState.Mounted && view == runtime.Rider?.View)
            {
                var selected = SelectionManager.Instance?.SelectedUnits;
                if (selected != null && selected.Contains(runtime.Rider))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySuppressRiderGroundPlacement(UnitEntityView view)
        {
            var riderView = runtime.Rider?.View;
            if (riderView == null || view == null)
            {
                return false;
            }

            if (!MountedRiderGroundingPolicy.ShouldSuppress(
                coordinator.State,
                coordinator.ActivePair != null,
                riderView,
                view))
            {
                return false;
            }

            RiderGroundPlacementSuppressionCount++;
            return true;
        }

        public void NormalizeMultiSelection(ref IEnumerable<UnitEntityView> views)
        {
            if (views == null || coordinator.State != RelationshipState.Mounted || runtime.Rider?.View == null || runtime.Mount?.View == null)
            {
                return;
            }

            var normalized = new List<UnitEntityView>();
            foreach (var view in views)
            {
                var effective = view == runtime.Mount.View ? runtime.Rider.View : view;
                if (effective != null && !normalized.Contains(effective))
                {
                    normalized.Add(effective);
                }
            }
            views = normalized;
        }

        public void ForwardStopOrHold()
        {
            if (coordinator.State == RelationshipState.Mounted)
            {
                runtime.CancelMountMovement();
            }
        }

        public void HandleUnexpectedPairCommand(UnitEntityData executor)
        {
            if (coordinator.State == RelationshipState.Mounted && (executor == runtime.Rider || executor == runtime.Mount))
            {
                Dismount(CleanupTrigger.UnexpectedCommand);
            }
        }

        public bool GuardBoundary(CleanupTrigger trigger)
        {
            var result = Dismount(trigger);
            return result.Succeeded && !result.MovementAuthorityResidual && !result.PresentationResidual &&
                coordinator.State == RelationshipState.Unmounted;
        }

        public void ValidateActivePair()
        {
            if (cleanupRetryRequired || coordinator.State == RelationshipState.Faulted)
            {
                RetryFailedCleanupOrThrow();
            }

            if (coordinator.State != RelationshipState.Mounted)
            {
                return;
            }

            var error = runtime.ValidateMountedInvariants();
            if (error != null)
            {
                logger.Warning("Mounted invariant invalidated: " + error);
                Dismount(CleanupTrigger.CompanionInvalidated);
                if (cleanupRetryRequired || coordinator.State == RelationshipState.Faulted)
                {
                    RetryFailedCleanupOrThrow();
                }
            }
        }

        private void ObserveCleanupState(TransitionResult result)
        {
            if (result.State == RelationshipState.Unmounted && !result.MovementAuthorityResidual && !result.PresentationResidual)
            {
                cleanupRetryRequired = false;
                cleanupRetryTrigger = CleanupTrigger.Exception;
            }
            else if (result.State == RelationshipState.Faulted)
            {
                cleanupRetryRequired = true;
                cleanupRetryTrigger = result.Trigger ?? CleanupTrigger.Exception;
            }
        }

        private void RetryFailedCleanupOrThrow()
        {
            var trigger = cleanupRetryTrigger;
            logger.Warning("Retrying a failed mounted cleanup before any further relationship work.");
            var retry = Dismount(trigger);
            if (!retry.Succeeded || retry.MovementAuthorityResidual || retry.PresentationResidual ||
                coordinator.State != RelationshipState.Unmounted)
            {
                throw new InvalidOperationException("Mounted cleanup retry retained runtime residue: " +
                    string.Join(" | ", retry.Errors.ToArray()));
            }
        }

        public TransitionResult Shutdown()
        {
            if (disposed)
            {
                return new TransitionResult(true, coordinator.State, CleanupTrigger.ModDisabled, new string[0], false, false);
            }

            var result = Dismount(CleanupTrigger.ModDisabled);
            if (!result.Succeeded || result.MovementAuthorityResidual || result.PresentationResidual)
            {
                return result;
            }

            coordinator.Dispose();
            if (coordinator.State != RelationshipState.Disposed)
            {
                return new TransitionResult(false, coordinator.State, CleanupTrigger.ModDisabled,
                    new[] { "Coordinator could not enter Disposed without residue." }, false, false);
            }

            runtime.ClearPreparedPairWhenUnmounted();
            disposed = true;
            return new TransitionResult(true, coordinator.State, CleanupTrigger.ModDisabled, new string[0], false, false);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            var result = Shutdown();
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Mounted relationship shutdown retained residue: " + string.Join(" | ", result.Errors.ToArray()));
            }
        }

        private TransitionResult Record(TransitionResult result)
        {
            LastTransition = result;
            LastResult = result.Succeeded
                ? "PASS: state=" + result.State + (result.Trigger.HasValue ? ", trigger=" + result.Trigger.Value : string.Empty)
                : "FAIL: state=" + result.State + ", errors=" + string.Join(" | ", result.Errors.ToArray());
            if (result.Succeeded)
            {
                logger.Info(LastResult);
            }
            else
            {
                logger.Warning(LastResult);
            }
            return result;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(GameMountedRelationshipService));
            }
        }
    }
}
