using System;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat
{
    internal sealed class CompositionRoot : IDisposable
    {
        private readonly IModLogger logger;
        private readonly RuntimeAutomationHost runtimeAutomation;
        private readonly RuntimeSaveAuthorization saveAuthorization;
        private readonly DiagnosticSettings settings;
        private readonly HorseCompanionBlueprintService horseCompanion;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly NativeLifecycleDeliveryLedger lifecycleLedger;
        private readonly MountedPatchController patches;
        private readonly MountedPlayerActionController playerAction;
        private readonly NativeMountedControlService nativeControls;
        private readonly MountedCombatController combat;
        private readonly MountedPairCommandScheduler pairedCommandScheduler;
        private readonly UnifiedMountedTurnCoordinator unifiedTurn;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly MountedAnimationAdapter animation;
        private readonly MountedDollRoomIkAdapter dollRoomIk;
        private readonly MovementTelemetryWriter movementTelemetry;
        private bool disposed;

        public CompositionRoot(IModLogger logger, string loadedModId)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            try
            {
                settings = new DiagnosticSettings();
                horseCompanion = new HorseCompanionBlueprintService(logger);
                relationship = new GameMountedRelationshipService(logger, settings);
                lifecycleLedger = new NativeLifecycleDeliveryLedger();
                horsePrimaryAttackAnimation = new HorsePrimaryAttackAnimationAdapter(relationship, logger);
                pairedCommandScheduler = new MountedPairCommandScheduler(relationship, settings, logger);
                unifiedTurn = new UnifiedMountedTurnCoordinator(
                    relationship,
                    settings,
                    pairedCommandScheduler,
                    logger);
                combat = new MountedCombatController(
                    relationship,
                    settings,
                    horsePrimaryAttackAnimation,
                    unifiedTurn,
                    pairedCommandScheduler,
                    logger);
                unifiedTurn.BindCombat(combat);
                animation = new MountedAnimationAdapter(relationship, combat, horsePrimaryAttackAnimation, logger);
                dollRoomIk = new MountedDollRoomIkAdapter(relationship, logger);
                lifecycle = new MountedLifecycleSubscriber(relationship, lifecycleLedger, combat, unifiedTurn);
                saveAuthorization = new RuntimeSaveAuthorization();
                playerAction = new MountedPlayerActionController(relationship, settings, logger, combat);
                nativeControls = new NativeMountedControlService(
                    relationship,
                    playerAction,
                    combat,
                    horseCompanion,
                    settings,
                    lifecycleLedger,
                    logger);
                patches = new MountedPatchController(relationship, playerAction, combat, unifiedTurn, nativeControls, animation, dollRoomIk, saveAuthorization, lifecycleLedger, logger);
                runtimeAutomation = RuntimeAutomationHost.CreateFromCommandLine(
                    logger,
                    loadedModId,
                    () => relationship.State.ToString(),
                    () => settings.EnableUnsafeMovementExperiment,
                    saveAuthorization,
                    relationship,
                    lifecycle,
                    playerAction,
                    combat,
                    horseCompanion,
                    nativeControls,
                    animation,
                    dollRoomIk,
                    settings,
                    Main.InvokeRegisteredToggleForAutomation);
                if (runtimeAutomation != null && !runtimeAutomation.IsManualReview)
                {
                    movementTelemetry = new MovementTelemetryWriter(
                        runtimeAutomation.Request,
                        relationship.Runtime,
                        () => relationship.State.ToString(),
                        () => runtimeAutomation.CurrentMovementRow,
                        settings.TelemetryIntervalSeconds);
                }
            }
            catch (Exception constructionException)
            {
                Exception rollbackException = null;
                try { movementTelemetry?.Dispose(); } catch (Exception exception) { rollbackException = exception; }
                try { runtimeAutomation?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { patches?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { playerAction?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { nativeControls?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { combat?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { unifiedTurn?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { pairedCommandScheduler?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { lifecycle?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { relationship?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { horseCompanion?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                if (rollbackException != null)
                {
                    throw new AggregateException("Composition-root construction and rollback both failed.", constructionException, rollbackException);
                }
                throw;
            }
        }

        public bool IsEnabled { get; private set; }

        internal RuntimeSaveAuthorization SaveAuthorization => saveAuthorization;

        internal MountedLifecycleSubscriber Lifecycle => lifecycle;

        public bool SetEnabled(bool enabled)
        {
            ThrowIfDisposed();
            if (enabled)
            {
                if (IsEnabled) { return true; }
                if (!horseCompanion.SetSelectionEnabled(true))
                {
                    logger.Error("Private-alpha services could not enable the exact Ranger horse selection transaction.");
                    return false;
                }
                IsEnabled = true;
                nativeControls.SetEnabled(true);
                playerAction.SetOverlayEnabled(settings.EnableDiagnosticOverlay || runtimeAutomation != null);
                logger.Info("Private-alpha services and native mounted abilities enabled; diagnostic overlay=" +
                    (settings.EnableDiagnosticOverlay || runtimeAutomation != null) + ".");
                return true;
            }

            // Always execute idempotent cleanup on a disable request. A prior
            // update failure may already have cleared IsEnabled while a partial
            // runtime operation still needs best-effort cleanup.
            if (!lifecycle.HandleModDisable())
            {
                logger.Error("Diagnostic services could not be disabled because mounted cleanup retained residue.");
                return false;
            }
            if (!horseCompanion.SetSelectionEnabled(false))
            {
                logger.Error("Private-alpha services could not disable without overwriting an externally changed Ranger selection.");
                return false;
            }
            IsEnabled = false;
            nativeControls.SetEnabled(false);
            playerAction.SetOverlayEnabled(false);
            logger.Info("Private-alpha services disabled; native control facts and transient UI removed with no mounted state retained.");
            return true;
        }

        public void HandleUpdateFailure(Exception exception)
        {
            ThrowIfDisposed();
            Exception first = null;
            try { runtimeAutomation?.Abort(exception); }
            catch (Exception abortException) { first = abortException; }

            if (runtimeAutomation != null && runtimeAutomation.IsSaveBackedFailurePending)
            {
                logger.Warning("Save-backed update failure is latched until the active Kingmaker loading pipeline stops; relationship cleanup and process completion remain deferred.");
                return;
            }

            try
            {
                combat.Cancel("update failure");
                settings.EnableUnsafeMovementExperiment = false;
                var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    throw new InvalidOperationException("Update-failure cleanup retained mounted runtime residue.");
                }
                IsEnabled = false;
            }
            catch (Exception cleanupException)
            {
                first = first ?? cleanupException;
            }

            if (first != null)
            {
                throw new InvalidOperationException("Runtime update failure could not be handled without residue.", first);
            }
        }

        public void Update(float deltaTime)
        {
            ThrowIfDisposed();
            horseCompanion.Update();
            nativeControls.Update();
            runtimeAutomation?.Update(deltaTime);
            if (runtimeAutomation != null && runtimeAutomation.IsSaveBackedFailurePending)
            {
                return;
            }
            if (runtimeAutomation == null || !runtimeAutomation.IsCompleted)
            {
                movementTelemetry?.Update(deltaTime);
            }
            if (!IsEnabled)
            {
                return;
            }

            combat.Update();
            unifiedTurn.Update();
            relationship.ValidateActivePair();
        }

        public void DrawGui()
        {
            ThrowIfDisposed();
            GUILayout.Label("Phase 2 private-alpha presentation work. The mounted relationship is transient and is cleaned before save/load/area boundaries.");
            settings.EnableUnsafeMovementExperiment = GUILayout.Toggle(settings.EnableUnsafeMovementExperiment, "Enable private-alpha mounted player action");
            settings.EnableUnifiedMountedTurn = GUILayout.Toggle(settings.EnableUnifiedMountedTurn, "Enable Phase 3D unified mounted turn (fallback: Phase 3C separate turns)");
            settings.EnablePairedCommandScheduler = GUILayout.Toggle(
                settings.EnablePairedCommandScheduler,
                "Enable experimental Phase 3E paired-command scheduler");
            var diagnosticOverlay = GUILayout.Toggle(settings.EnableDiagnosticOverlay, "Show diagnostic mounted-control overlay");
            if (diagnosticOverlay != settings.EnableDiagnosticOverlay)
            {
                settings.EnableDiagnosticOverlay = diagnosticOverlay;
                playerAction.SetOverlayEnabled(diagnosticOverlay || runtimeAutomation != null);
            }
            GUILayout.Label("Candidate: selected supported Medium rider + exact active, currently larger Mammoth or KMC Horse companion.");
            GUILayout.BeginHorizontal();
            var availability = playerAction.GetAvailability();
            var priorEnabled = GUI.enabled;
            GUI.enabled = availability.IsEnabled && availability.Action == MountedPlayerActionKind.Mount;
            if (GUILayout.Button("Mount selected rider"))
            {
                playerAction.Activate();
            }
            GUI.enabled = relationship.State == RelationshipState.Mounted || relationship.State == RelationshipState.Faulted;
            if (GUILayout.Button("Dismount / clear"))
            {
                playerAction.Activate();
            }
            GUI.enabled = priorEnabled;
            GUILayout.EndHorizontal();
            GUILayout.Label("Relationship: " + relationship.State);
            GUILayout.Label("Horse companion blueprints: " + horseCompanion.State +
                (string.IsNullOrEmpty(horseCompanion.Failure) ? string.Empty : " — " + horseCompanion.Failure));
            GUILayout.Label(relationship.LastResult);
            if (combat.CanShowCombatActions)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Arm Rider Primary")) { combat.ArmRiderPrimary(); }
                if (GUILayout.Button("Arm " + playerAction.MountPrimaryLabel)) { combat.Arm(MountedCombatActionKind.MountPrimaryNatural); }
                GUILayout.EndHorizontal();
                GUILayout.Label(combat.LastFeedback);
            }
            if (!availability.IsEnabled) { GUILayout.Label(availability.Feedback); }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (!lifecycle.HandleModDisable())
            {
                throw new InvalidOperationException("Composition root cannot dispose while mounted cleanup residue remains.");
            }

            movementTelemetry?.Dispose();
            runtimeAutomation?.Dispose();
            patches.Dispose();
            nativeControls.Dispose();
            playerAction.Dispose();
            combat.Dispose();
            unifiedTurn.Dispose();
            pairedCommandScheduler.Dispose();
            lifecycle.Dispose();
            relationship.Dispose();
            horseCompanion.Dispose();
            IsEnabled = false;
            disposed = true;
            logger.Info("Composition root disposed; no mounted relationship was serialized.");
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CompositionRoot));
            }
        }
    }
}
