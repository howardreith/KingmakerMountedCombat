using System;
using System.Linq;
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
        private readonly MountedWorldService world;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly NativeLifecycleDeliveryLedger lifecycleLedger;
        private readonly MountedPatchController patches;
        private readonly MountedPlayerActionController playerAction;
        private readonly NativeMountedControlService nativeControls;
        private readonly MountedPersistenceService persistence;
        private readonly MountedRemovalPreparation removal;
        private readonly MountedCombatController combat;
        private readonly MountedPairCommandScheduler pairedCommandScheduler;
        private readonly UnifiedMountedTurnCoordinator unifiedTurn;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly MountedAnimationAdapter animation;
        private readonly MountedDollRoomIkAdapter dollRoomIk;
        private readonly MovementTelemetryWriter movementTelemetry;
        private bool disposed;
        // An update failure whose destructive cleanup was deferred because an
        // owned save was in flight; consumed by Update once that save settles.
        private bool failureCleanupPending;

        public CompositionRoot(IModLogger logger, string loadedModId)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            try
            {
                settings = new DiagnosticSettings();
                horseCompanion = new HorseCompanionBlueprintService(logger);
                world = new MountedWorldService(logger, settings);
                relationship = world.Primary.Relationship;
                combat = world.Primary.Combat;
                playerAction = world.Primary.PlayerActions;
                pairedCommandScheduler = world.Primary.Scheduler;
                horsePrimaryAttackAnimation = world.Primary.HorseAnimation;
                animation = world.Primary.Animation;
                dollRoomIk = world.Primary.DollRoomIk;
                unifiedTurn = world.UnifiedTurn;
                lifecycleLedger = new NativeLifecycleDeliveryLedger();
                saveAuthorization = new RuntimeSaveAuthorization();
                nativeControls = new NativeMountedControlService(relationship, playerAction, combat,
                    horseCompanion, settings, lifecycleLedger, logger);
                world.BindControls(nativeControls);
                persistence = new MountedPersistenceService(world, settings, logger);
                world.ArchiveWorkerRunning = () => persistence.ActiveSaveWorkerRunning;
                world.AdmissionFenced = () => persistence.HasSaveAdmissionFence;
                lifecycle = new MountedLifecycleSubscriber(relationship, lifecycleLedger, combat, unifiedTurn, persistence);
                removal = new MountedRemovalPreparation(relationship, persistence, combat, horseCompanion, lifecycle.HandleModDisable, logger);
                patches = new MountedPatchController(relationship, playerAction, combat, unifiedTurn, nativeControls, persistence, animation, dollRoomIk, saveAuthorization, lifecycleLedger, logger);
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
                    persistence,
                    removal,
                    animation,
                    dollRoomIk,
                    settings,
                    Main.InvokeRegisteredToggleForAutomation,
                    DetachIntegrationForAutomation,
                    () => unifiedTurn.ActivationIdentity != null,
                    () => unifiedTurn.PartnerContext != null,
                    () => movementTelemetry);
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
                try { world?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { nativeControls?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { lifecycle?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { horseCompanion?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                if (rollbackException != null)
                {
                    throw new AggregateException("Composition-root construction and rollback both failed.", constructionException, rollbackException);
                }
                throw;
            }
        }

        // A native archive worker writes a few megabytes; this is generous for
        // that and still short enough that a stuck one cannot hang an unload.
        private const int TeardownDrainMilliseconds = 15000;

        public bool IsEnabled { get; private set; }

        // Only an isolated automation process may detach KMC's gameplay and
        // persistence integration in place: services off, mounted cleanup run,
        // every Harmony guard removed. The run-scoped save isolation is a
        // separate Harmony owner and stays, so the process still cannot touch
        // human saves. The DLL itself remains loaded; this is "integration
        // absent", not "mod absent", and is reported as exactly that.
        internal bool DetachIntegrationForAutomation()
        {
            ThrowIfDisposed();
            if (!NativePersistenceIsolation.IsIsolated) return false;
            if (IsEnabled && !SetEnabled(false)) return false;
            patches.Dispose();
            // The persistence controller's LoadRoutine wrapper is gone with the
            // patches; the isolation keeps its own read-only load scope so the
            // engine's header update during the load stays swallowed, exactly
            // as in every isolated load. Run final94-p07-absent measured the
            // fail-closed commit guard refusing that update without this seam.
            NativePersistenceIsolation.InstallDetachedReadOnlyLoad();
            logger.Info("KMC gameplay and persistence integration detached for an isolated automation load; save isolation retained.");
            return !MountedPatchController.BridgeInstalled && NativePersistenceIsolation.DetachedReadOnlyLoadInstalled;
        }

        internal RuntimeSaveAuthorization SaveAuthorization => saveAuthorization;

        internal MountedLifecycleSubscriber Lifecycle => lifecycle;

        internal MountedPersistenceService Persistence => persistence;

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
                persistence.Enabled = true;
                nativeControls.SetEnabled(true);
                var overlayEnabled = settings.EnableDiagnosticOverlay ||
                    (runtimeAutomation != null && runtimeAutomation.RequiresLegacyDiagnosticOverlay);
                playerAction.SetOverlayEnabled(overlayEnabled);
                logger.Info("Private-alpha services and native mounted abilities enabled; diagnostic overlay=" +
                    overlayEnabled + ".");
                return true;
            }

            // Disabling runs a full mounted cleanup over the exact live Player
            // and cross-scene graphs an owned archive worker serializes on its
            // own thread. Nothing can cancel a started worker, so refuse the
            // disable instead of mutating the write in flight; the save always
            // settles, and its drain keeps running because persistence.Update
            // is deliberately not gated on IsEnabled.
            if (persistence.SaveSuspended || world.AnyChargeFenced || !relationship.AllTeardownOwnersReady)
            {
                logger.Error("Diagnostic services cannot be disabled while a mounted save is still being written; " +
                    "retry once it finishes.");
                return false;
            }

            // The same argument for the other live operation: cleanup during an
            // owned world replacement would tear the pair down across a world
            // being replaced underneath it. Bounded by the load finishing.
            if (persistence.LoadInFlight)
            {
                logger.Error("Diagnostic services cannot be disabled while a mounted save is still being loaded; " +
                    "retry once the area has finished loading.");
                return false;
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
            persistence.Enabled = false;
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

            // Update-failure cleanup is a full mounted cleanup over the exact live
            // graphs an owned archive worker may be serializing. While an owned
            // save is in flight it is latched, not skipped: persistence.Update
            // runs first in every frame and keeps draining, and the cleanup runs
            // on the first frame after the save settles.
            if (persistence.SaveSuspended)
            {
                failureCleanupPending = true;
                logger.Warning("Update failure latched: an owned save is still in flight; mounted cleanup runs once it settles.");
                if (first != null)
                {
                    throw new InvalidOperationException("Runtime update failure could not be handled without residue.", first);
                }
                return;
            }

            try { PerformFailureCleanup(); }
            catch (Exception cleanupException)
            {
                first = first ?? cleanupException;
            }

            if (first != null)
            {
                throw new InvalidOperationException("Runtime update failure could not be handled without residue.", first);
            }
        }

        private void PerformFailureCleanup()
        {
            var errors = world.Visit(pair =>
            {
                pair.Combat.Cancel("world update failure");
                var cleanup = pair.Relationship.Dismount(CleanupTrigger.Exception);
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    throw new InvalidOperationException("Update-failure cleanup retained mounted runtime residue.");
            });
            if (errors.Length != 0) throw new AggregateException("World update cleanup retained owners.", errors);
            IsEnabled = false;
        }

        internal void ObserveStartupCallbackEntry() => runtimeAutomation?.ObserveStartupCallbackEntry();
        internal void ObserveStartupCallbackExit() => runtimeAutomation?.ObserveStartupCallbackExit();

        public void Update(float deltaTime)
        {
            var measurement = MountedRuntimeMeasurement.Current;
            if (measurement == null) { UpdateCore(deltaTime); return; }
            var started = measurement.BeginFrame();
            try { UpdateCore(deltaTime); }
            finally { measurement.EndFrame(started); }
        }

        private void UpdateCore(float deltaTime)
        {
            runtimeAutomation?.ObserveStartupPreHostStart();
            ThrowIfDisposed();
            horseCompanion.Update();
            persistence.Update();
            // Debt retries precede the ordinary disabled/save/area/restore update gates.
            if (!persistence.ActiveSaveWorkerRunning) world.Visit(pair => pair.Combat.UpdateChargeCleanup());
            removal.Update();
            // A latched update-failure cleanup runs on the first frame after the
            // owned save that deferred it has settled.
            if (failureCleanupPending && !persistence.SaveSuspended)
            {
                failureCleanupPending = false;
                PerformFailureCleanup();
            }
            nativeControls.Update();
            runtimeAutomation?.ObserveStartupPreHostEnd();
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

            if (persistence.SaveSuspended || persistence.AreaTransitionPending || persistence.CombatRestorationPending) return;
            world.UpdatePairs();
            unifiedTurn.Update();
        }

        public void DrawGui()
        {
            ThrowIfDisposed();
            GUILayout.Label("Kingmaker Mounted Combat — private engineering candidate");
            GUILayout.Label("CRPG transport: the mount moves the pair; rider and mount retain their own actions, health and effects.");
            GUILayout.Label("Select the rider, open their ability drawer, and use Mount Companion on their own eligible companion.");
            GUILayout.Label("Use Dismount on the rider to separate them. You can drag a control to an empty action-bar slot.");
            GUILayout.Label("Use ordinary movement, attacks and spells. Rider Primary and Mount Primary select the corresponding actor's attack.");
            GUILayout.Label("Mounted pairs: " + world.Registry.Owners.Count(owner => owner.State == MountedPairOwnershipState.Active) +
                " / " + GameMountedRelationshipService.NativePairCapacity + ". Paired turns are the supported configuration.");
            var selected = world.Selected;
            if (selected != null && !string.IsNullOrEmpty(selected.PlayerActions.LastFeedback))
                GUILayout.Label(selected.PlayerActions.LastFeedback);
            if (selected != null && !string.IsNullOrEmpty(selected.Combat.LastFeedback))
                GUILayout.Label(selected.Combat.LastFeedback);
            GUILayout.Label(persistence.Feedback);
            var priorEnabled = GUI.enabled;
            GUI.enabled = priorEnabled && unifiedTurn.CanConfigurePairedActivation &&
                !nativeControls.HasUnsettledRelationshipTransition && !world.AnyChargeFenced &&
                !persistence.HasSaveAdmissionFence && !persistence.LoadInFlight &&
                world.Pairs.All(pair => !nativeControls.HasUnsettledShellFor(pair.Relationship));
            settings.EnableMountedCharge = GUILayout.Toggle(settings.EnableMountedCharge,
                "Enable optional real-time Mounted Charge");
            GUI.enabled = priorEnabled;
            GUILayout.Label("Change Charge outside combat with every pair dismounted. Turn-based Charge is unavailable and refuses without spending actions.");
            GUILayout.Label("Mounts: your supported Mammoth or the Horse chosen through Ranger companion progression. Unsupported sizes, forms and rigs refuse admission.");
            GUILayout.Label("Before removing KMC, prepare a clean save. A campaign containing a KMC Horse still requires the mod.");
            GUI.enabled = priorEnabled && removal.State != RemovalPreparationState.Saving;
            if (GUILayout.Button("Prepare to disable / remove KMC")) removal.Begin();
            GUI.enabled = priorEnabled;
            GUILayout.Label(removal.Status);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // Unload is the one place a bounded wait for an owned archive worker
            // is right: patches.Dispose below removes the commit transpiler the
            // worker is still running through. The verdict is CONSUMED, never
            // assumed. A wait that expires, or ownership that could not be
            // established, refuses the unload: nothing has been released, the
            // root, its hooks and its per-frame drain stay intact, and the throw
            // reaches Main.OnUnload, which reports false to UMM. The installed
            // UMM honours that: ModEntry.Reload aborts on a false OnUnload before
            // any cache, assembly or hook teardown (IL_007D brfalse IL_0335).
            var verdict = persistence.DrainForTeardown(TeardownDrainMilliseconds);
            if (verdict == OwnedWorkerTeardownVerdict.Refused)
            {
                logger.Error("Unload refused: an owned archive worker can still commit, or its ownership could not " +
                    "be established, within the bounded teardown wait. Nothing was unpatched or cleaned up; retry " +
                    "once the save settles.");
                throw new InvalidOperationException(
                    "Composition root refuses to dispose while an owned archive worker can still commit.");
            }

            if (!lifecycle.HandleModDisable())
            {
                throw new InvalidOperationException("Composition root cannot dispose while mounted cleanup residue remains.");
            }

            movementTelemetry?.Dispose();
            runtimeAutomation?.Dispose();
            patches.Dispose();
            nativeControls.Dispose();
            lifecycle.Dispose();
            world.Dispose();
            horseCompanion.Dispose();
            IsEnabled = false;
            disposed = true;
            logger.Info("Composition root disposed; transient mounted references released.");
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
