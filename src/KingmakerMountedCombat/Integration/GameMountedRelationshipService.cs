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
using TurnBased.Controllers;

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
        private bool nativeTurnBasedExitAiLeaseReassertionPending;
        private bool nativeTurnBasedExitUiLeaseRestorePending;
        private long mountedPairGeneration;
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

        internal long MountedPairGeneration => mountedPairGeneration;

        internal KingmakerMountedPairRuntime Runtime => runtime;

        internal bool IsExactCapturedView(UnitEntityData unit) => runtime.IsExactCapturedView(unit);

        internal bool IsChangedViewChildOfOwnedAnchor(UnitEntityData unit) => runtime.IsChangedViewChildOfOwnedAnchor(unit);

        internal string CapturePresentationObservation(bool includeUiOwnership = true) =>
            "relationship=" + State + ";" + runtime.CapturePresentationObservation(includeUiOwnership);

        public long RiderGroundPlacementSuppressionCount { get; private set; }

        internal int NativeTurnBasedExitAiLeaseReassertionArmedCount { get; private set; }

        internal int NativeTurnBasedExitAiLeaseReassertionAttemptCount { get; private set; }

        internal int NativeTurnBasedExitAiLeaseReassertionMutationCount { get; private set; }

        internal int NativeTurnBasedExitAiLeaseReassertionSuccessCount { get; private set; }

        internal string NativeTurnBasedExitAiLeaseReassertionResult { get; private set; } = "not-requested";

        internal int NativeTurnBasedExitUiLeaseRestoreArmedCount { get; private set; }

        internal int NativeTurnBasedExitUiLeaseRestoreAttemptCount { get; private set; }

        internal int NativeTurnBasedExitUiLeaseRestoreMutationCount { get; private set; }

        internal int NativeTurnBasedExitUiLeaseRestoreSuccessCount { get; private set; }

        internal string NativeTurnBasedExitUiLeaseRestoreResult { get; private set; } = "not-requested";

        public string LastResult { get; private set; } = "No diagnostic action has run.";

        public TransitionResult LastTransition { get; private set; }

        internal event Action<CleanupTrigger> Dismounting;

        internal event Action<UnitEntityData, UnitEntityData> MountedPairActivated;

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
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Select exactly one Medium rider with an active supported companion." }, false, false));
            }

            var rider = selection[0];
            var mount = rider.Descriptor?.Pet;
            if (mount == null)
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { "Selected rider has no active companion." }, false, false));
            }

            return MountRiderOn(rider, mount);
        }

        public TransitionResult MountRiderOn(UnitEntityData rider, UnitEntityData mount)
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
            if (rider == null || mount == null)
            {
                return Record(new TransitionResult(false, coordinator.State, null,
                    new[] { "An exact rider and supported active companion are required." }, false, false));
            }

            runtime.Prepare(rider, mount);
            var result = coordinator.Mount(runtime.CreateCandidate());
            ObserveCleanupState(result);
            if (result.Succeeded)
            {
                mountedPairGeneration = checked(mountedPairGeneration + 1);
                ResetNativeTurnBasedExitAiLeaseEvidence();
                MountedPairActivated?.Invoke(rider, mount);
            }
            if (!result.Succeeded)
            {
                runtime.ClearPreparedPairWhenUnmounted();
            }
            return Record(result);
        }

        public bool TryResolveAutomationPair(out UnitEntityData rider, out UnitEntityData mount, out string error)
        {
            return TryResolveAutomationPair(KingmakerMountedPairRuntime.MammothBlueprintGuid, out rider, out mount, out error);
        }

        public bool TryResolveAutomationPair(
            string expectedMountBlueprintGuid,
            out UnitEntityData rider,
            out UnitEntityData mount,
            out string error)
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
                string.Equals(unit.Descriptor.Pet.Blueprint.AssetGuid, expectedMountBlueprintGuid, StringComparison.Ordinal)).ToList();
            if (candidates.Count != 1)
            {
                var profile = SupportedMountedProfiles.Resolve(expectedMountBlueprintGuid);
                error = "Expected exactly one party rider with the exact " +
                    (profile?.DisplayName ?? "supported mount") + " active companion; observed " + candidates.Count + ".";
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
            return MountAutomationPair(KingmakerMountedPairRuntime.MammothBlueprintGuid);
        }

        public TransitionResult MountAutomationPair(string expectedMountBlueprintGuid)
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
            if (!TryResolveAutomationPair(expectedMountBlueprintGuid, out rider, out mount, out error))
            {
                return Record(new TransitionResult(false, coordinator.State, null, new[] { error }, false, false));
            }

            return MountRiderOn(rider, mount);
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

            try
            {
                Dismounting?.Invoke(trigger);
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted combat cancellation before relationship cleanup", exception);
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

            var selected = SelectionManager.Instance?.SelectedUnits;
            var exactMountSelection = selected != null && selected.Count == 1 && selected[0] == runtime.Mount;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (MountedTurnSelectionPolicy.CanUseNativeMountTurnGroundCommand(
                true,
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == runtime.Mount,
                unit == runtime.Mount,
                exactMountSelection))
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
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var disposition = MountedTurnSelectionPolicy.Classify(
                coordinator.State == RelationshipState.Mounted,
                runtime.Mount?.View == view,
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == runtime.Mount);
            if (disposition == MountedSelectionDisposition.ProjectMountToRider && runtime.Rider?.View != null)
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
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var preserveNativeMountTurn = MountedTurnSelectionPolicy.Classify(
                true,
                true,
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == runtime.Mount) == MountedSelectionDisposition.PreserveNativeMountTurn;
            foreach (var view in views)
            {
                var effective = view == runtime.Mount.View && !preserveNativeMountTurn ? runtime.Rider.View : view;
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

        internal bool IsExactActivePairUnit(UnitEntityData unit)
        {
            return coordinator.State == RelationshipState.Mounted &&
                unit != null && (unit == runtime.Rider || unit == runtime.Mount);
        }

        public bool RouteContinuousMove(ref UnitEntityData executor)
        {
            if (!IsExactActivePairUnit(executor))
            {
                return true;
            }

            if (executor == runtime.Rider)
            {
                if (runtime.Mount == null)
                {
                    logger.Warning("Rejected mounted continuous movement because the exact physical mover was unavailable; relationship retained for invariant validation.");
                    return false;
                }
                executor = runtime.Mount;
                logger.Info("Routed exact rider continuous movement through the mounted physical mover without relationship cleanup.");
            }

            return true;
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
                nativeTurnBasedExitAiLeaseReassertionPending = false;
                nativeTurnBasedExitUiLeaseRestorePending = false;
                return;
            }

            CompleteNativeTurnBasedExitAiLeaseReassertion();
            CompleteNativeTurnBasedExitUiLeaseRestore();

            var error = runtime.ValidateMountedInvariants();
            if (error != null)
            {
                logger.Warning("Mounted invariant invalidated: " + error);
                Dismount(runtime.HasRiderViewReplacement ? CleanupTrigger.ViewReplaced : CleanupTrigger.CompanionInvalidated);
                if (cleanupRetryRequired || coordinator.State == RelationshipState.Faulted)
                {
                    RetryFailedCleanupOrThrow();
                }
            }
        }

        internal void ObserveNativeTurnBasedModeChanged(bool enabled)
        {
            if (enabled || coordinator.State != RelationshipState.Mounted)
            {
                nativeTurnBasedExitAiLeaseReassertionPending = false;
                nativeTurnBasedExitUiLeaseRestorePending = false;
                return;
            }

            nativeTurnBasedExitAiLeaseReassertionPending = true;
            NativeTurnBasedExitAiLeaseReassertionArmedCount++;
            NativeTurnBasedExitAiLeaseReassertionResult = "armed";
            nativeTurnBasedExitUiLeaseRestorePending = true;
            NativeTurnBasedExitUiLeaseRestoreArmedCount++;
            NativeTurnBasedExitUiLeaseRestoreResult = "armed";
        }

        private void CompleteNativeTurnBasedExitAiLeaseReassertion()
        {
            var controller = Game.Instance?.TurnBasedCombatController;
            var disposition = NativeTurnBasedExitAiLeasePolicy.Classify(
                nativeTurnBasedExitAiLeaseReassertionPending,
                coordinator.State == RelationshipState.Mounted,
                CombatController.IsInTurnBasedCombat(),
                controller != null && controller.Initialized,
                runtime.MountAiLeaseOwned,
                runtime.MountRawAiEnabled);
            if (disposition == NativeTurnBasedExitAiLeaseDisposition.NotPending ||
                disposition == NativeTurnBasedExitAiLeaseDisposition.AwaitNativeControllerClear)
            {
                return;
            }

            nativeTurnBasedExitAiLeaseReassertionPending = false;
            NativeTurnBasedExitAiLeaseReassertionAttemptCount++;
            if (disposition == NativeTurnBasedExitAiLeaseDisposition.RejectInexactLease)
            {
                NativeTurnBasedExitAiLeaseReassertionResult = "rejected-inexact-lease";
                return;
            }

            if (disposition == NativeTurnBasedExitAiLeaseDisposition.AlreadyExact)
            {
                NativeTurnBasedExitAiLeaseReassertionSuccessCount++;
                NativeTurnBasedExitAiLeaseReassertionResult = "already-exact";
                return;
            }

            NativeTurnBasedExitAiLeaseReassertionMutationCount++;
            if (runtime.ReassertMountAiLeaseAfterNativeTurnBasedExit())
            {
                NativeTurnBasedExitAiLeaseReassertionSuccessCount++;
                NativeTurnBasedExitAiLeaseReassertionResult = "reasserted";
                logger.Info("Reasserted the exact owned Mammoth AI-disable lease after native TB combat-controller shutdown.");
            }
            else
            {
                NativeTurnBasedExitAiLeaseReassertionResult = "reassertion-failed";
            }
        }

        private void ResetNativeTurnBasedExitAiLeaseEvidence()
        {
            nativeTurnBasedExitAiLeaseReassertionPending = false;
            nativeTurnBasedExitUiLeaseRestorePending = false;
            NativeTurnBasedExitAiLeaseReassertionArmedCount = 0;
            NativeTurnBasedExitAiLeaseReassertionAttemptCount = 0;
            NativeTurnBasedExitAiLeaseReassertionMutationCount = 0;
            NativeTurnBasedExitAiLeaseReassertionSuccessCount = 0;
            NativeTurnBasedExitAiLeaseReassertionResult = "not-requested";
            NativeTurnBasedExitUiLeaseRestoreArmedCount = 0;
            NativeTurnBasedExitUiLeaseRestoreAttemptCount = 0;
            NativeTurnBasedExitUiLeaseRestoreMutationCount = 0;
            NativeTurnBasedExitUiLeaseRestoreSuccessCount = 0;
            NativeTurnBasedExitUiLeaseRestoreResult = "not-requested";
        }

        private void CompleteNativeTurnBasedExitUiLeaseRestore()
        {
            var rider = runtime.Rider;
            var selection = SelectionManager.Instance;
            var selected = selection?.SelectedUnits;
            var exactRiderSelected = rider != null && selected != null &&
                selected.Count == 1 && selected[0] == rider;
            var aiLeaseBoundaryCompleted =
                NativeTurnBasedExitAiLeaseReassertionAttemptCount == 1 &&
                NativeTurnBasedExitAiLeaseReassertionSuccessCount == 1;
            var disposition = NativeTurnBasedExitUiLeasePolicy.Classify(
                nativeTurnBasedExitUiLeaseRestorePending,
                coordinator.State == RelationshipState.Mounted,
                CombatController.IsInTurnBasedCombat(),
                Game.Instance?.CurrentMode.ToString(),
                aiLeaseBoundaryCompleted,
                rider != null && runtime.IsExactCapturedView(rider),
                exactRiderSelected);
            if (disposition == NativeTurnBasedExitUiLeaseDisposition.NotPending ||
                disposition == NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary)
            {
                return;
            }

            nativeTurnBasedExitUiLeaseRestorePending = false;
            NativeTurnBasedExitUiLeaseRestoreAttemptCount++;
            if (disposition == NativeTurnBasedExitUiLeaseDisposition.RejectInexactPair)
            {
                NativeTurnBasedExitUiLeaseRestoreResult = "rejected-inexact-pair";
                return;
            }

            if (disposition == NativeTurnBasedExitUiLeaseDisposition.AlreadyExact)
            {
                NativeTurnBasedExitUiLeaseRestoreSuccessCount++;
                NativeTurnBasedExitUiLeaseRestoreResult = "already-exact";
                return;
            }

            NativeTurnBasedExitUiLeaseRestoreMutationCount++;
            if (selection != null && rider?.View != null)
            {
                selection.SelectUnit(rider.View, true, true, false);
                selected = selection.SelectedUnits;
            }
            if (selected != null && selected.Count == 1 && selected[0] == rider)
            {
                NativeTurnBasedExitUiLeaseRestoreSuccessCount++;
                NativeTurnBasedExitUiLeaseRestoreResult = "reselected-rider";
                logger.Info("Restored the exact rider selection/UI principal after native TB combat-controller shutdown.");
            }
            else
            {
                NativeTurnBasedExitUiLeaseRestoreResult = "selection-restore-failed";
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
