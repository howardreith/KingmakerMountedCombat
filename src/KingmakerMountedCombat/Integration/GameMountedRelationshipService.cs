using System;
using System.Collections.Generic;
using System.Linq;
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

        public string LastResult { get; private set; } = "No diagnostic action has run.";

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
            if (!result.Succeeded)
            {
                runtime.ClearPreparedPairWhenUnmounted();
            }
            return Record(result);
        }

        public TransitionResult Dismount(CleanupTrigger trigger)
        {
            if (disposed && coordinator.State == RelationshipState.Disposed)
            {
                return new TransitionResult(true, coordinator.State, trigger, new string[0], false, false);
            }

            var result = coordinator.Dismount(trigger);
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
            if (coordinator.State != RelationshipState.Mounted)
            {
                return;
            }

            var error = runtime.ValidateMountedInvariants();
            if (error != null)
            {
                logger.Warning("Mounted invariant invalidated: " + error);
                Dismount(CleanupTrigger.CompanionInvalidated);
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
