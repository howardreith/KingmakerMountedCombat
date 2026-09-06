using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPlayerActionController : IDisposable
    {
        internal const string OverlayObjectName = "KMC_PlayerActionOverlay";

        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly MountedCombatController combat;
        private readonly MountedOverlayWorldInputGuard mountTargetWorldInputGuard = new MountedOverlayWorldInputGuard();
        private readonly MountedPlayerActionFeedbackState feedbackState =
            new MountedPlayerActionFeedbackState("Ready to mount when the selected rider is eligible.");
        private GameObject overlayObject;
        private MountedPlayerActionOverlay overlay;
        private UnitEntityData armedRider;
        private UnitEntityData armedMount;
        private TransitionResult lastObservedTransition;
        private bool disposed;

        public MountedPlayerActionController(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            IModLogger logger,
            MountedCombatController combat)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        public MountedPlayerActionAvailability GetAvailability()
        {
            return GetAvailability(false);
        }

        private MountedPlayerActionAvailability GetAvailability(bool nativeMoveActionShellAdmitted, UnitEntityData executionCaster = null)
        {
            ThrowIfDisposed();
            var context = CaptureContext(executionCaster);
            context.NativeMoveActionShellAdmitted = nativeMoveActionShellAdmitted;
            var availability = MountedPlayerActionEvaluator.Evaluate(context);
            if (IsMountTargetArmed &&
                (availability.Action != MountedPlayerActionKind.Mount || !availability.IsEnabled))
            {
                ClearMountTargetSelection();
            }
            feedbackState.ObserveAvailability(availability);
            var transition = relationship.LastTransition;
            if (transition != null && !ReferenceEquals(transition, lastObservedTransition))
            {
                lastObservedTransition = transition;
                if (transition.Succeeded && transition.State == RelationshipState.Unmounted &&
                    transition.Trigger.HasValue && transition.Trigger.Value != CleanupTrigger.Manual)
                {
                    feedbackState.SetOperationFeedback(MountedCleanupFeedbackPolicy.Describe(transition.Trigger.Value));
                }
            }
            return availability;
        }

        public string LastFeedback => feedbackState.LastFeedback;

        internal bool CombatActionsVisible => combat.CanShowCombatActions;

        internal string CombatFeedback => combat.LastFeedback;

        internal MountedCombatActionKind ArmedCombatAction => combat.ArmedAction;

        internal bool IsMountTargetArmed => armedRider != null && armedMount != null;

        internal string MountPrimaryLabel => (relationship.Runtime.MountDisplayName ?? ResolveSelectedMountProfile()?.DisplayName ?? "Mount") + " primary";

        internal long MountTargetArmCount { get; private set; }

        internal long MountTargetClickCount { get; private set; }

        internal bool ArmCombatAction(MountedCombatActionKind action)
        {
            return combat.Arm(action);
        }

        internal bool ArmCombatActionFromOverlay(MountedCombatActionKind action)
        {
            ObserveOverlayButtonActivation();
            return ArmCombatAction(action);
        }

        internal bool ArmRiderPrimaryFromOverlay()
        {
            ObserveOverlayButtonActivation();
            return combat.ArmRiderPrimary();
        }

        internal bool ArmMountTargetFromOverlay()
        {
            ObserveOverlayButtonActivation();
            return ArmMountTarget();
        }

        internal bool ArmMountTarget()
        {
            ThrowIfDisposed();
            if (IsSelectedOrMountedRiderInCombat())
            {
                feedbackState.SetOperationFeedback(
                    "Use the native Mount Companion ability during combat so Kingmaker charges the rider's Move action.");
                return false;
            }
            if (IsMountTargetArmed)
            {
                ClearMountTargetSelection();
                feedbackState.SetOperationFeedback("Mount targeting cancelled.");
                return false;
            }

            var availability = GetAvailability();
            if (!availability.IsVisible || !availability.IsEnabled ||
                availability.Action != MountedPlayerActionKind.Mount)
            {
                feedbackState.SetOperationFeedback(availability.Feedback);
                return false;
            }

            var selection = SelectionManager.Instance?.SelectedUnits;
            var rider = selection != null && selection.Count == 1 ? selection[0] : null;
            var mount = rider?.Descriptor?.Pet;
            var profile = SupportedMountedProfiles.Resolve(mount);
            if (rider == null || mount == null || profile == null)
            {
                feedbackState.SetOperationFeedback("Mount targeting failed closed: the selected rider's supported active companion changed.");
                return false;
            }

            armedRider = rider;
            armedMount = mount;
            MountTargetArmCount++;
            feedbackState.SetOperationFeedback("Mount armed: click the exact active " + profile.DisplayName + ".");
            logger.Info("Target-selected Mount armed: riderId=" + rider.UniqueId +
                "; mountId=" + mount.UniqueId + "; profile=" + profile.Id + ".");
            return true;
        }

        internal NativeMountedControlAvailability GetNativeMountAvailability(
            UnitEntityData caster,
            bool nativeMoveActionShellAdmitted = false)
        {
            ThrowIfDisposed();
            var availability = GetAvailability(nativeMoveActionShellAdmitted, nativeMoveActionShellAdmitted ? caster : null);
            var selection = SelectionManager.Instance?.SelectedUnits;
            var exactCasterSelected = caster != null && (nativeMoveActionShellAdmitted ||
                selection != null && selection.Count == 1 && selection[0] == caster);
            if (!availability.IsVisible || availability.Action != MountedPlayerActionKind.Mount)
            {
                return new NativeMountedControlAvailability(false, false, "Mount Companion is available only while unmounted.");
            }
            if (!exactCasterSelected)
            {
                return new NativeMountedControlAvailability(true, false, "Select the exact prospective rider.");
            }
            return new NativeMountedControlAvailability(true, availability.IsEnabled, availability.Feedback);
        }

        internal NativeMountedControlAvailability GetNativeDismountAvailability(
            UnitEntityData caster,
            bool nativeMoveActionShellAdmitted = false)
        {
            ThrowIfDisposed();
            var availability = GetAvailability(nativeMoveActionShellAdmitted, nativeMoveActionShellAdmitted ? caster : null);
            var exactRider = caster != null && caster == relationship.Rider;
            var game = Game.Instance;
            if (game == null || game.CurrentlyLoadedArea == null ||
                !MountedGameModePolicy.CanQueueMountedAction(game.CurrentMode.ToString()) ||
                exactRider && IsLoadingOrCutscene(game, caster, relationship.Mount))
            {
                return new NativeMountedControlAvailability(true, false, "Dismount orders require the active world view.");
            }
            if (!availability.IsVisible || availability.Action != MountedPlayerActionKind.Dismount || !exactRider)
            {
                return new NativeMountedControlAvailability(false, false, "Dismount is available only to the exact mounted rider.");
            }
            return new NativeMountedControlAvailability(true, availability.IsEnabled, availability.Feedback);
        }

        internal bool CanNativeMountTarget(UnitEntityData caster, UnitEntityData target)
        {
            if (!GetNativeMountAvailability(caster).IsEnabled)
            {
                return false;
            }
            var mount = caster?.Descriptor?.Pet;
            return target != null && target == mount &&
                mount.Descriptor?.Master.Value == caster &&
                SupportedMountedProfiles.IsSupported(mount);
        }

        internal string DescribeNativeMountTargetRejection(
            UnitEntityData caster,
            UnitEntityData target,
            bool nativeMoveActionShellAdmitted = false)
        {
            var availability = GetNativeMountAvailability(caster, nativeMoveActionShellAdmitted);
            if (!availability.IsEnabled)
            {
                return availability.Reason;
            }
            var mount = caster?.Descriptor?.Pet;
            var mountName = SupportedMountedProfiles.Resolve(mount)?.DisplayName ?? "supported companion";
            return target == null
                ? "Mount Companion requires a creature target. Click the rider's exact active " + mountName + "."
                : "Mount target rejected: click the selected rider's exact active " + mountName + ".";
        }

        internal bool TryExecuteNativeMount(UnitEntityData caster, UnitEntityData target)
        {
            ThrowIfDisposed();
            if (!MountedGameModePolicy.CanAdmitMountedAction(Game.Instance?.CurrentMode.ToString())) { return false; }
            MountTargetClickCount++;
            var availability = GetNativeMountAvailability(caster, true);
            var mount = caster?.Descriptor?.Pet;
            var exactTarget = target != null && target == mount &&
                mount.Descriptor?.Master.Value == caster && SupportedMountedProfiles.IsSupported(mount);
            if (exactTarget && (caster.View == null || mount.View == null ||
                !CombatMountDismountPolicy.IsAdjacent(caster.DistanceTo(mount), caster.View.Corpulence, mount.View.Corpulence)))
            {
                feedbackState.SetOperationFeedback("Move next to your companion before mounting.");
                return false;
            }
            if (!availability.IsEnabled || !exactTarget)
            {
                var reason = !availability.IsEnabled
                    ? availability.Reason
                    : DescribeNativeMountTargetRejection(caster, target, true);
                feedbackState.SetOperationFeedback(reason);
                logger.Info("Native Mount Companion target rejected: casterId=" +
                    (caster?.UniqueId ?? "<none>") + "; targetId=" +
                    (target?.UniqueId ?? "<none>") + "; reason=" + reason + ".");
                return false;
            }

            try
            {
                ClearMountTargetSelection();
                var transition = relationship.MountRiderOn(caster, target);
                feedbackState.SetOperationFeedback(relationship.LastResult);
                logger.Info("Native Mount Companion dispatch: riderId=" + caster.UniqueId +
                    "; mountId=" + target.UniqueId + "; succeeded=" + transition.Succeeded + ".");
                return transition.Succeeded;
            }
            catch (Exception exception)
            {
                feedbackState.SetOperationFeedback("Native Mount Companion failed closed: " +
                    exception.GetType().Name + ".");
                logger.Exception("Native Mount Companion", exception);
                var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    throw new InvalidOperationException("Native Mount failure cleanup retained mounted residue.", exception);
                }
                return false;
            }
        }

        internal bool TryExecuteNativeDismount(UnitEntityData caster)
        {
            ThrowIfDisposed();
            if (!MountedGameModePolicy.CanAdmitMountedAction(Game.Instance?.CurrentMode.ToString())) { return false; }
            var availability = GetNativeDismountAvailability(caster, true);
            if (!availability.IsEnabled)
            {
                feedbackState.SetOperationFeedback(availability.Reason);
                return false;
            }

            try
            {
                ClearMountTargetSelection();
                var transition = relationship.Dismount(CleanupTrigger.Manual);
                feedbackState.SetOperationFeedback(relationship.LastResult);
                logger.Info("Native Dismount dispatch: riderId=" + caster.UniqueId +
                    "; succeeded=" + transition.Succeeded + ".");
                return transition.Succeeded;
            }
            catch (Exception exception)
            {
                feedbackState.SetOperationFeedback("Native Dismount failed closed: " +
                    exception.GetType().Name + ".");
                logger.Exception("Native Dismount", exception);
                var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    throw new InvalidOperationException("Native Dismount failure cleanup retained residue.", exception);
                }
                return false;
            }
        }

        internal MountedCombatClickResult TryHandleMountTargetClick(
            GameObject gameObject,
            int button,
            bool simulate)
        {
            if (disposed || !IsMountTargetArmed || simulate || button != 0)
            {
                return MountedCombatClickResult.NotHandled;
            }
            if (mountTargetWorldInputGuard.TryConsumePropagatedWorldClick(Time.frameCount))
            {
                logger.Info("Suppressed one frame-bounded world click propagated from the target-selected Mount overlay; targeting remains armed.");
                return MountedCombatClickResult.HandledRejected;
            }

            MountTargetClickCount++;
            var targetView = gameObject == null ? null : gameObject.GetComponent<UnitEntityView>();
            var target = targetView?.EntityData;
            var selection = SelectionManager.Instance?.SelectedUnits;
            var exactRiderStillSelected = selection != null && selection.Count == 1 && selection[0] == armedRider;
            if (target != armedMount || targetView == null || !exactRiderStillSelected ||
                armedRider.Descriptor?.Pet != armedMount || armedMount.Descriptor?.Master.Value != armedRider ||
                !SupportedMountedProfiles.IsSupported(armedMount))
            {
                feedbackState.SetOperationFeedback(
                    "Mount target rejected: click the selected rider's exact active supported companion.");
                logger.Info("Target-selected Mount rejected: targetId=" + (target?.UniqueId ?? "<none>") +
                    "; expectedMountId=" + (armedMount?.UniqueId ?? "<none>") +
                    "; exactRiderSelection=" + exactRiderStillSelected + ".");
                return MountedCombatClickResult.HandledRejected;
            }

            var rider = armedRider;
            var mount = armedMount;
            ClearMountTargetSelection();
            try
            {
                var transition = relationship.MountRiderOn(rider, mount);
                NormalizeSelectionToRider(rider);
                feedbackState.SetOperationFeedback(relationship.LastResult);
                return transition.Succeeded
                    ? MountedCombatClickResult.HandledAccepted
                    : MountedCombatClickResult.HandledRejected;
            }
            catch (Exception exception)
            {
                feedbackState.SetOperationFeedback("Target-selected Mount failed closed: " + exception.GetType().Name + ".");
                logger.Exception("Target-selected Mount click", exception);
                var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    throw new InvalidOperationException("Target-selected Mount failure cleanup retained mounted residue.", exception);
                }
                return MountedCombatClickResult.HandledRejected;
            }
        }

        internal bool OverlayPresent => overlay != null && overlayObject != null;

        internal long OverlayRepaintCount { get; private set; }

        internal long OverlayButtonActivationCount { get; private set; }

        internal bool LastOverlayVisible { get; private set; }

        internal bool LastOverlayEnabled { get; private set; }

        internal string LastOverlayLabel { get; private set; }

        internal string LastOverlayFeedback { get; private set; }

        internal Rect LastOverlayRect { get; private set; }

        internal static int CountOverlayObjects()
        {
            var count = 0;
            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform != null && string.Equals(transform.name, OverlayObjectName, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        public void SetOverlayEnabled(bool enabled)
        {
            ThrowIfDisposed();
            if (enabled)
            {
                EnsureOverlay();
                overlay.enabled = true;
                return;
            }

            ClearMountTargetSelection();
            DestroyOverlay();
        }

        public bool Activate()
        {
            ThrowIfDisposed();
            var availability = GetAvailability();
            if (!availability.IsVisible || !availability.IsEnabled)
            {
                feedbackState.SetOperationFeedback(availability.Feedback);
                return false;
            }

            if (IsSelectedOrMountedRiderInCombat())
            {
                feedbackState.SetOperationFeedback(
                    "Use the native " +
                    (availability.Action == MountedPlayerActionKind.Dismount ? "Dismount" : "Mount Companion") +
                    " ability during combat so Kingmaker charges the rider's Move action.");
                return false;
            }

            try
            {
                ClearMountTargetSelection();
                UnitEntityData rider = relationship.Rider;
                TransitionResult transition;
                if (availability.Action == MountedPlayerActionKind.Dismount)
                {
                    transition = relationship.Dismount(CleanupTrigger.Manual);
                }
                else if (availability.Action == MountedPlayerActionKind.Mount)
                {
                    var selection = SelectionManager.Instance?.SelectedUnits;
                    rider = selection != null && selection.Count == 1 ? selection[0] : null;
                    transition = relationship.MountSelectedRider();
                }
                else
                {
                    feedbackState.SetOperationFeedback("No mounted player action is currently available.");
                    return false;
                }

                NormalizeSelectionToRider(rider);
                feedbackState.SetOperationFeedback(relationship.LastResult);
                return transition.Succeeded;
            }
            catch (Exception exception)
            {
                feedbackState.SetOperationFeedback("Mounted action failed closed: " + exception.GetType().Name + ".");
                logger.Error(LastFeedback);
                var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    throw new InvalidOperationException("Player-action failure cleanup retained mounted residue.", exception);
                }
                return false;
            }
        }

        internal void HandleOverlayFailure(Exception exception)
        {
            if (disposed)
            {
                return;
            }

            feedbackState.SetOperationFeedback("Transient mounted-action UI disabled after an exception: " + exception.GetType().Name + ".");
            ClearMountTargetSelection();
            logger.Error(LastFeedback);
            var cleanup = relationship.Dismount(CleanupTrigger.Exception);
            if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
            {
                logger.Error("Transient UI failure cleanup retained mounted residue.");
            }
            if (overlay != null)
            {
                overlay.enabled = false;
            }
        }

        internal void ObserveOverlayRepaint(
            MountedPlayerActionAvailability availability,
            Rect panel,
            string feedback)
        {
            if (disposed || availability == null)
            {
                return;
            }

            OverlayRepaintCount++;
            LastOverlayVisible = availability.IsVisible;
            LastOverlayEnabled = availability.IsEnabled;
            LastOverlayLabel = availability.Label;
            LastOverlayFeedback = feedback;
            LastOverlayRect = panel;
        }

        internal void ObserveOverlayButtonActivation()
        {
            if (!disposed)
            {
                OverlayButtonActivationCount++;
                mountTargetWorldInputGuard.MarkActivation(Time.frameCount);
                combat.MarkPlayerFacingOverlayActivation(Time.frameCount);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DestroyOverlay();
            ClearMountTargetSelection();
            disposed = true;
        }

        private MountedPlayerActionContext CaptureContext(UnitEntityData executionCaster = null)
        {
            var state = relationship.State;
            var game = Game.Instance;
            var gameAvailable = game != null && game.Player != null && game.CurrentlyLoadedArea != null;
            var context = new MountedPlayerActionContext
            {
                RelationshipState = state,
                GameAvailable = gameAvailable,
                FeatureEnabled = settings.EnableUnsafeMovementExperiment,
                ConflictingMountedRelationship = state != RelationshipState.Unmounted &&
                    state != RelationshipState.Mounted &&
                    state != RelationshipState.Faulted
            };

            if (!gameAvailable)
            {
                return context;
            }

            var selection = SelectionManager.Instance?.SelectedUnits;
            UnitEntityData rider;
            UnitEntityData mount;
            if (state == RelationshipState.Mounted || state == RelationshipState.Faulted)
            {
                rider = relationship.Rider;
                mount = relationship.Mount;
                context.ExactlyOneRiderSelected = executionCaster != null ? executionCaster == rider :
                    selection != null && selection.Count == 1 && selection[0] == rider;
            }
            else
            {
                context.ExactlyOneRiderSelected = executionCaster != null || selection != null && selection.Count == 1 &&
                    selection[0] != null;
                rider = executionCaster ?? (context.ExactlyOneRiderSelected ? selection[0] : null);
                mount = rider?.Descriptor?.Pet;
            }
            if (!context.ExactlyOneRiderSelected)
            {
                return context;
            }

            var riderState = rider.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var profile = SupportedMountedProfiles.Resolve(mount);
            var exactOwnership = profile != null && rider.Descriptor.Pet == mount &&
                mount.Descriptor?.Master.Value == rider && mount.Descriptor.IsPet;

            context.RiderIsExactlyMedium = riderState != null && (int)riderState.Size == 4;
            context.RiderBodyProfileSupported = IsSupportedRiderBodySurface(rider, profile);
            context.ExactActiveOwnedSupportedMount = exactOwnership;
            context.MountDisplayName = profile?.DisplayName;
            context.MountIsStrictlyLarger = riderState != null && mountState != null &&
                (int)mountState.Size > (int)riderState.Size;
            context.RiderIsAliveAndConscious = riderState != null && riderState.IsConscious && !riderState.IsFinallyDead;
            context.MountIsAliveAndConscious = mountState != null && mountState.IsConscious && !mountState.IsFinallyDead;
            context.RiderIsDirectlyControllableAndInGame = rider.IsInGame && rider.IsDirectlyControllable;
            context.MountIsDirectlyControllableAndInGame = mount != null && mount.IsInGame && mount.IsDirectlyControllable;
            context.UnsupportedPolymorphOrSizeState = rider.GetActivePolymorph() != null ||
                (mount != null && mount.GetActivePolymorph() != null) || !context.RiderIsExactlyMedium;
            context.LoadingTransitionOrCutscene = IsLoadingOrCutscene(game, rider, mount);
            context.InCombat = rider.IsInCombat || (mount?.IsInCombat ?? false) || game.Player.IsInCombat;
            var turnBased = CombatController.IsInTurnBasedCombat();
            var turn = game.TurnBasedCombatController?.CurrentTurn;
            context.CombatTurnEligible = CombatMountDismountPolicy.IsTurnEligible(
                turnBased,
                turn?.Unit == rider,
                turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing);
            context.RiderHasMoveAction = rider.HasMoveAction();
            context.PairAdjacent = mount != null && rider.View != null && mount.View != null &&
                CombatMountDismountPolicy.IsAdjacent(
                    rider.DistanceTo(mount),
                    rider.View.Corpulence,
                    mount.View.Corpulence);
            context.SafeGameMode = executionCaster != null
                ? MountedGameModePolicy.CanAdmitMountedAction(game.CurrentMode.ToString())
                : MountedGameModePolicy.CanQueueMountedAction(game.CurrentMode.ToString());
            context.ViewsAndStockAgentsAvailable = rider.View != null && rider.View.AgentASP != null &&
                mount?.View != null && mount.View.AgentASP != null;
            context.StockAgentsReady = context.ViewsAndStockAgentsAvailable &&
                rider.View.AgentASP.enabled && mount.View.AgentASP.enabled;
            context.AgentOverridesAvailable = rider.View != null && rider.View.AgentOverride == null &&
                mount?.View != null && mount.View.AgentOverride == null;
            return context;
        }

        private bool IsSelectedOrMountedRiderInCombat()
        {
            var rider = relationship.Rider;
            if (rider == null)
            {
                var selection = SelectionManager.Instance?.SelectedUnits;
                rider = selection != null && selection.Count == 1 ? selection[0] : null;
            }
            var mount = relationship.Mount ?? rider?.Descriptor?.Pet;
            return rider != null &&
                (rider.IsInCombat || (mount?.IsInCombat ?? false) ||
                 (Game.Instance?.Player?.IsInCombat ?? false));
        }

        private static bool IsLoadingOrCutscene(Game game, UnitEntityData rider, UnitEntityData mount)
        {
            var loading = LoadingProcess.Instance;
            return (loading != null && loading.IsLoadingInProcess) || game.CutsceneLock ||
                rider.CutceneControlledUnit != null || (mount != null && mount.CutceneControlledUnit != null) ||
                game.CurrentMode == GameModeType.Cutscene || game.CurrentMode == GameModeType.CutsceneGlobalMap;
        }

        private static bool IsSupportedRiderBodySurface(UnitEntityData rider, SupportedMountedProfile profile)
        {
            if (rider?.View == null || rider.GetActivePolymorph() != null || profile == null)
            {
                return false;
            }

            string ignoredError;
            return MountedRiderPoseAdapter.TryValidateSupportedSurface(
                rider.View,
                profile.RiderPoseProfile,
                out ignoredError);
        }

        private SupportedMountedProfile ResolveSelectedMountProfile()
        {
            var selection = SelectionManager.Instance?.SelectedUnits;
            var rider = selection != null && selection.Count == 1 ? selection[0] : null;
            return SupportedMountedProfiles.Resolve(rider?.Descriptor?.Pet);
        }

        private void ClearMountTargetSelection()
        {
            armedRider = null;
            armedMount = null;
            mountTargetWorldInputGuard.Clear();
        }

        private static void NormalizeSelectionToRider(UnitEntityData rider)
        {
            if (rider?.View == null || SelectionManager.Instance == null)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
        }

        private void EnsureOverlay()
        {
            if (overlay != null && overlayObject != null)
            {
                return;
            }

            overlayObject = new GameObject(OverlayObjectName);
            overlayObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(overlayObject);
            overlay = overlayObject.AddComponent<MountedPlayerActionOverlay>();
            overlay.Configure(this);
        }

        private void DestroyOverlay()
        {
            if (overlay != null)
            {
                overlay.Deconfigure();
                overlay.enabled = false;
            }
            if (overlayObject != null)
            {
                UnityEngine.Object.Destroy(overlayObject);
            }
            overlay = null;
            overlayObject = null;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MountedPlayerActionController));
            }
        }
    }
}
