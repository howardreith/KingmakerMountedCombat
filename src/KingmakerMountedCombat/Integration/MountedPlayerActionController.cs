using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPlayerActionController : IDisposable
    {
        internal const string OverlayObjectName = "KMC_PlayerActionOverlay";

        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly MountedPlayerActionFeedbackState feedbackState =
            new MountedPlayerActionFeedbackState("Ready to mount when the selected rider is eligible.");
        private GameObject overlayObject;
        private MountedPlayerActionOverlay overlay;
        private bool disposed;

        public MountedPlayerActionController(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public MountedPlayerActionAvailability GetAvailability()
        {
            ThrowIfDisposed();
            var availability = MountedPlayerActionEvaluator.Evaluate(CaptureContext());
            feedbackState.ObserveAvailability(availability);
            return availability;
        }

        public string LastFeedback => feedbackState.LastFeedback;

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

            try
            {
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
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DestroyOverlay();
            disposed = true;
        }

        private MountedPlayerActionContext CaptureContext()
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

            if (state == RelationshipState.Mounted || state == RelationshipState.Faulted || !gameAvailable)
            {
                return context;
            }

            var selection = SelectionManager.Instance?.SelectedUnits;
            context.ExactlyOneRiderSelected = selection != null && selection.Count == 1 && selection[0] != null;
            if (!context.ExactlyOneRiderSelected)
            {
                return context;
            }

            var rider = selection[0];
            var mount = rider.Descriptor?.Pet;
            var riderState = rider.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var exactMammoth = mount != null && mount.Blueprint != null &&
                string.Equals(mount.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal);
            var exactOwnership = exactMammoth && rider.Descriptor.Pet == mount &&
                mount.Descriptor?.Master.Value == rider && mount.Descriptor.IsPet;

            context.RiderIsExactlyMedium = riderState != null && (int)riderState.Size == 4;
            context.RiderBodyProfileSupported = IsSupportedRiderBodySurface(rider);
            context.ExactActiveOwnedMammoth = exactOwnership;
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
            context.SafeGameMode = game.CurrentMode == GameModeType.Default || game.CurrentMode == GameModeType.Pause;
            context.ViewsAndStockAgentsAvailable = rider.View != null && rider.View.AgentASP != null &&
                mount?.View != null && mount.View.AgentASP != null;
            context.StockAgentsReady = context.ViewsAndStockAgentsAvailable &&
                rider.View.AgentASP.enabled && mount.View.AgentASP.enabled;
            context.AgentOverridesAvailable = rider.View != null && rider.View.AgentOverride == null &&
                mount?.View != null && mount.View.AgentOverride == null;
            return context;
        }

        private static bool IsLoadingOrCutscene(Game game, UnitEntityData rider, UnitEntityData mount)
        {
            var loading = LoadingProcess.Instance;
            return (loading != null && loading.IsLoadingInProcess) || game.CutsceneLock ||
                rider.CutceneControlledUnit != null || (mount != null && mount.CutceneControlledUnit != null) ||
                game.CurrentMode == GameModeType.Cutscene || game.CurrentMode == GameModeType.CutsceneGlobalMap;
        }

        private static bool IsSupportedRiderBodySurface(UnitEntityData rider)
        {
            if (rider?.View == null || rider.GetActivePolymorph() != null)
            {
                return false;
            }

            string ignoredError;
            return MountedRiderPoseAdapter.TryValidateSupportedSurface(
                rider.View,
                MountedRiderPoseProfiles.MediumHumanoidOnMammoth,
                out ignoredError);
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
