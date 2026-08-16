using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class DiagnosticCombatTargetService : IDisposable
    {
        private const float MinimumPlacementDistance = 3f;
        private const float MaximumPlacementDistance = 20f;
        private readonly IModLogger logger;
        private readonly DiagnosticCombatTargetLifecycle lifecycle = new DiagnosticCombatTargetLifecycle();
        private BlueprintFaction runtimeFaction;
        private UnitEntityData target;
        private bool disposed;

        public DiagnosticCombatTargetService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UnitEntityData Target => target;

        public string TargetId => target?.UniqueId;

        public DiagnosticCombatTargetState State => lifecycle.State;

        public UnitEntityData Spawn(
            UnitEntityData rider,
            Vector3 position,
            string runId,
            bool exactWorkingAuthorized)
        {
            ThrowIfDisposed();
            if (rider == null || !rider.IsInState || string.IsNullOrWhiteSpace(runId))
            {
                throw new InvalidOperationException("Diagnostic target requires an exact live rider and run ID.");
            }
            if (!lifecycle.BeginCreate("pending:" + runId, exactWorkingAuthorized))
            {
                throw new InvalidOperationException("Diagnostic target lifecycle rejected creation or Working authorization.");
            }

            try
            {
                ValidatePlacement(rider.Position, position);
                var game = Game.Instance;
                var state = game?.State?.LoadedAreaState?.MainState;
                var playerFaction = game?.BlueprintRoot?.PlayerFaction;
                if (state == null || game.EntityCreator == null || game.EntityDestroyer == null || playerFaction == null)
                {
                    throw new InvalidOperationException("Loaded-area target creation services are unavailable.");
                }

                var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(KingmakerMountedPairRuntime.MammothBlueprintGuid);
                if (blueprint == null || !string.Equals(blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Exact stock Mammoth source blueprint is unavailable.");
                }

                runtimeFaction = ScriptableObject.CreateInstance<BlueprintFaction>();
                runtimeFaction.name = "KMC_RuntimeHostile_" + runId;
                runtimeFaction.hideFlags = HideFlags.HideAndDontSave;
                runtimeFaction.AttackFactions = new[] { playerFaction };
                runtimeFaction.AlwaysEnemy = true;

                target = game.EntityCreator.SpawnUnit(blueprint, position, Quaternion.identity, state);
                game.EntityCreator.Tick();
                if (target == null || !target.IsInState || target.View == null || target.View.AgentASP == null)
                {
                    throw new InvalidOperationException("Diagnostic Mammoth target did not enter the exact loaded state.");
                }
                target.Descriptor.SwitchFactions(runtimeFaction, true);
                target.GiveExperienceOnDeath = false;
                target.IsAIEnabled = false;
                target.HoldState = true;
                target.Commands.InterruptAll();

                var primary = target.Body?.AdditionalLimbs?.FirstOrDefault(slot => slot.HasWeapon && slot.HasItem)?.MaybeWeapon;
                var zeroInventory = target.Inventory == null || target.Inventory.Items.Count == 0;
                var safetyGates = target.Blueprint == blueprint &&
                    !target.IsDirectlyControllable &&
                    !target.Descriptor.IsPet &&
                    target.Descriptor.Master.Value == null &&
                    target.Faction == runtimeFaction &&
                    target.IsEnemy(rider) && rider.IsEnemy(target) &&
                    !target.GiveExperienceOnDeath &&
                    !target.IsAIEnabled &&
                    target.Commands.Empty &&
                    zeroInventory &&
                    primary?.Blueprint != null &&
                    !primary.Blueprint.IsRanged;
                if (!lifecycle.Activate("pending:" + runId, safetyGates))
                {
                    throw new InvalidOperationException("Diagnostic target failed its exact transient safety gates.");
                }

                logger.Info("Created runtime-only diagnostic Mammoth target " + target.UniqueId + ".");
                return target;
            }
            catch
            {
                BestEffortDestroy();
                lifecycle.RequestDestroy("failed creation cleanup");
                if (!lifecycle.ConfirmRemoved(lifecycle.TargetId, true))
                {
                    lifecycle.Fault();
                }
                throw;
            }
        }

        public bool DestroyAndVerify()
        {
            if (target == null)
            {
                return State == DiagnosticCombatTargetState.Absent ||
                    State == DiagnosticCombatTargetState.Removed;
            }

            lifecycle.RequestDestroy("bounded cleanup");
            target.Commands.InterruptAll();
            target.View?.StopMoving();
            target.Destroy();
            Game.Instance?.EntityDestroyer?.Tick();
            var zeroResidue = !target.IsInState &&
                (Game.Instance?.State?.Units == null || !Game.Instance.State.Units.Contains(target));
            var confirmed = lifecycle.ConfirmRemoved(lifecycle.TargetId, zeroResidue);
            if (zeroResidue)
            {
                target = null;
                if (runtimeFaction != null)
                {
                    UnityEngine.Object.Destroy(runtimeFaction);
                    runtimeFaction = null;
                }
            }
            return zeroResidue && confirmed;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            if (!DestroyAndVerify())
            {
                throw new InvalidOperationException("Diagnostic target cleanup retained entity or faction residue.");
            }
            disposed = true;
        }

        private void BestEffortDestroy()
        {
            try
            {
                if (target != null)
                {
                    target.Commands.InterruptAll();
                    target.Destroy();
                    Game.Instance?.EntityDestroyer?.Tick();
                    target = null;
                }
            }
            finally
            {
                if (runtimeFaction != null)
                {
                    UnityEngine.Object.Destroy(runtimeFaction);
                    runtimeFaction = null;
                }
            }
        }

        private static void ValidatePlacement(Vector3 riderPosition, Vector3 targetPosition)
        {
            var delta = targetPosition - riderPosition;
            delta.y = 0f;
            if (delta.magnitude < MinimumPlacementDistance || delta.magnitude > MaximumPlacementDistance)
            {
                throw new InvalidOperationException("Diagnostic target placement is outside the bounded rider-relative range.");
            }
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Diagnostic target placement requires the active native navigation graph.");
            }
            var nearest = global::AstarPath.active.GetNearest(targetPosition);
            if (nearest.node == null || !nearest.node.Walkable)
            {
                throw new InvalidOperationException("Diagnostic target placement is not on a walkable native node.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(DiagnosticCombatTargetService));
            }
        }
    }
}
