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
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly MountedPatchController patches;
        private readonly MovementTelemetryWriter movementTelemetry;
        private bool disposed;

        public CompositionRoot(IModLogger logger, string loadedModId)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            try
            {
                settings = new DiagnosticSettings();
                relationship = new GameMountedRelationshipService(logger, settings);
                lifecycle = new MountedLifecycleSubscriber(relationship);
                saveAuthorization = new RuntimeSaveAuthorization();
                patches = new MountedPatchController(relationship, saveAuthorization, logger);
                runtimeAutomation = RuntimeAutomationHost.CreateFromCommandLine(
                    logger,
                    loadedModId,
                    () => relationship.State.ToString(),
                    () => settings.EnableUnsafeMovementExperiment,
                    saveAuthorization,
                    relationship,
                    lifecycle,
                    settings);
                if (runtimeAutomation != null)
                {
                    movementTelemetry = new MovementTelemetryWriter(runtimeAutomation.Request, relationship.Runtime, () => relationship.State.ToString(), settings.TelemetryIntervalSeconds);
                }
            }
            catch (Exception constructionException)
            {
                Exception rollbackException = null;
                try { movementTelemetry?.Dispose(); } catch (Exception exception) { rollbackException = exception; }
                try { runtimeAutomation?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { patches?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { lifecycle?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
                try { relationship?.Dispose(); } catch (Exception exception) { rollbackException = rollbackException ?? exception; }
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
            if (IsEnabled == enabled)
            {
                return true;
            }

            if (!enabled)
            {
                var result = relationship.Dismount(CleanupTrigger.ModDisabled);
                if (!result.Succeeded || result.MovementAuthorityResidual || result.PresentationResidual)
                {
                    logger.Error("Diagnostic services could not be disabled because mounted cleanup retained residue.");
                    return false;
                }
            }
            IsEnabled = enabled;
            logger.Info(enabled ? "Diagnostic services enabled." : "Diagnostic services disabled; no mounted state is retained.");
            return true;
        }

        public void Update(float deltaTime)
        {
            ThrowIfDisposed();
            runtimeAutomation?.Update(deltaTime);
            movementTelemetry?.Update(deltaTime);
            if (!IsEnabled)
            {
                return;
            }

            relationship.ValidateActivePair();
        }

        public void DrawGui()
        {
            ThrowIfDisposed();
            GUILayout.Label("Phase 1 movement-only diagnostics. No combat behavior or save persistence is enabled.");
            settings.EnableUnsafeMovementExperiment = GUILayout.Toggle(settings.EnableUnsafeMovementExperiment, "Enable default-off Mammoth movement experiment");
            GUILayout.Label("Candidate: selected Medium rider + exact active rank-7+ Mammoth companion. Anchor: Spine; initial offset is zero.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mount selected rider"))
            {
                relationship.MountSelectedRider();
            }
            if (GUILayout.Button("Dismount / clear"))
            {
                relationship.Dismount(CleanupTrigger.Manual);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Relationship: " + relationship.State);
            GUILayout.Label(relationship.LastResult);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            var cleanup = relationship.Dismount(CleanupTrigger.ModDisabled);
            if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
            {
                throw new InvalidOperationException("Composition root cannot dispose while mounted cleanup residue remains.");
            }

            movementTelemetry?.Dispose();
            runtimeAutomation?.Dispose();
            patches.Dispose();
            lifecycle.Dispose();
            relationship.Dispose();
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
