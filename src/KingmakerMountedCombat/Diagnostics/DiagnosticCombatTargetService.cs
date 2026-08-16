using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Kingmaker.UnitLogic.Groups;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class DiagnosticCombatTargetService : IDisposable
    {
        private const float MinimumPlacementDistance = 3f;
        private const float MaximumPlacementDistance = 20f;
        private const string RuntimeGroupPrefix = "KMC.RuntimeHostile.";
        private static readonly FieldInfo AiBackingField = typeof(UnitEntityData).GetField(
            "m_AiEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly IModLogger logger;
        private readonly DiagnosticCombatTargetLifecycle lifecycle = new DiagnosticCombatTargetLifecycle();
        private BlueprintFaction runtimeFaction;
        private UnitGroup runtimeGroup;
        private string runtimeGroupId;
        private UnitEntityData target;
        private bool runtimeFactionDestroyPending;
        private bool disposed;

        public DiagnosticCombatTargetService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UnitEntityData Target => target;

        public string TargetId => target?.UniqueId;

        public DiagnosticCombatTargetState State => lifecycle.State;

        public bool TargetEntityRemoved => target == null;

        public bool RuntimeGroupRemoved => runtimeGroup == null && string.IsNullOrEmpty(runtimeGroupId);

        public bool RuntimeFactionRemoved => runtimeFaction == null && !runtimeFactionDestroyPending;

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
                var groupsController = game?.UnitGroupsController;
                if (state == null || game.EntityCreator == null || game.EntityDestroyer == null ||
                    groupsController == null || playerFaction == null || AiBackingField == null ||
                    AiBackingField.FieldType != typeof(bool))
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
                var proposedRuntimeGroupId = RuntimeGroupPrefix + runId;
                if (groupsController.Groups.Any(group =>
                    group != null && string.Equals(group.Id, proposedRuntimeGroupId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Diagnostic target runtime group identity already exists.");
                }
                runtimeGroupId = proposedRuntimeGroupId;

                var groupsBeforeSpawn = groupsController.Groups.Where(group => group != null).ToArray();
                target = game.EntityCreator.SpawnUnit(blueprint, position, Quaternion.identity, state);
                game.EntityCreator.Tick();
                if (target == null || !target.IsInState || target.View == null || target.View.AgentASP == null)
                {
                    throw new InvalidOperationException("Diagnostic Mammoth target did not enter the exact loaded state.");
                }
                var spawnGroup = target.Group;
                target.GroupId = runtimeGroupId;
                var detachedFromSpawnGroup = ReleaseDetachedSpawnGroup(
                    groupsController,
                    groupsBeforeSpawn,
                    spawnGroup,
                    target);
                target.Descriptor.SwitchFactions(runtimeFaction, true);
                runtimeGroup = target.Group;
                target.GiveExperienceOnDeath = false;
                target.IsAIEnabled = false;
                target.HoldState = true;
                target.Commands.InterruptAll();

                var primary = target.Body?.AdditionalLimbs?.FirstOrDefault(slot => slot.HasWeapon)?.MaybeWeapon;
                var dedicatedRuntimeGroup = detachedFromSpawnGroup && runtimeGroup != null &&
                    !runtimeGroup.IsPlayerParty &&
                    string.Equals(runtimeGroup.Id, runtimeGroupId, StringComparison.Ordinal) &&
                    runtimeGroup.Count == 1 && runtimeGroup[0] == target;
                var inventoryHasNoLoot = target.Inventory == null || !target.Inventory.HasLoot;
                var aiBackingDisabled = !(bool)AiBackingField.GetValue(target);
                var safety = new DiagnosticCombatTargetSafetySnapshot(
                    target.Blueprint == blueprint,
                    !target.IsDirectlyControllable,
                    !target.Descriptor.IsPet,
                    target.Descriptor.Master.Value == null,
                    target.Faction == runtimeFaction,
                    dedicatedRuntimeGroup,
                    target.IsEnemy(rider),
                    rider.IsEnemy(target),
                    !target.GiveExperienceOnDeath,
                    aiBackingDisabled,
                    target.Commands.Empty,
                    inventoryHasNoLoot,
                    primary?.Blueprint != null,
                    primary?.Blueprint != null && primary.Blueprint.IsNatural,
                    primary?.Blueprint != null && !primary.Blueprint.IsRanged);
                if (!lifecycle.Activate("pending:" + runId, safety.AllPassed))
                {
                    throw new InvalidOperationException(
                        "Diagnostic target failed its exact transient safety gates: " + safety.FailureSummary + ".");
                }

                logger.Info("Created runtime-only diagnostic Mammoth target " + target.UniqueId + ".");
                return target;
            }
            catch (Exception exception)
            {
                lifecycle.RequestDestroy("failed creation cleanup");
                var cleanupPassed = BestEffortDestroy();
                if (!lifecycle.ConfirmRemoved(lifecycle.TargetId, cleanupPassed))
                {
                    lifecycle.Fault();
                    throw new InvalidOperationException(
                        "Diagnostic target creation failed and exact cleanup could not be proven.",
                        exception);
                }
                throw;
            }
        }

        public bool DestroyAndVerify()
        {
            if (TargetEntityRemoved && RuntimeGroupRemoved && RuntimeFactionRemoved)
            {
                return State == DiagnosticCombatTargetState.Absent ||
                    State == DiagnosticCombatTargetState.Removed;
            }

            lifecycle.RequestDestroy("bounded cleanup");
            var zeroResidue = BestEffortDestroy();
            var confirmed = lifecycle.ConfirmRemoved(lifecycle.TargetId, zeroResidue);
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

        private bool BestEffortDestroy()
        {
            var current = target;
            if (current != null)
            {
                current.Commands.InterruptAll();
                current.View?.StopMoving();
                current.Destroy();
                Game.Instance?.EntityDestroyer?.Tick();
            }
            var targetRemoved = current == null ||
                (!current.IsInState &&
                    (Game.Instance?.State?.Units == null || !Game.Instance.State.Units.Contains(current)));
            if (targetRemoved)
            {
                target = null;
            }
            var groupRemoved = targetRemoved && ReleaseRuntimeGroup();
            if (targetRemoved && groupRemoved && runtimeFaction != null && !runtimeFactionDestroyPending)
            {
                UnityEngine.Object.Destroy(runtimeFaction);
                runtimeFactionDestroyPending = true;
            }
            if (targetRemoved && groupRemoved && runtimeFaction == null)
            {
                runtimeFaction = null;
                runtimeFactionDestroyPending = false;
            }
            return targetRemoved && groupRemoved && RuntimeFactionRemoved;
        }

        private bool ReleaseRuntimeGroup()
        {
            if (runtimeGroup == null)
            {
                if (string.IsNullOrEmpty(runtimeGroupId))
                {
                    return true;
                }
                var lookupController = Game.Instance?.UnitGroupsController;
                if (lookupController == null)
                {
                    return false;
                }
                var matchingGroups = lookupController.Groups.Where(group =>
                    group != null && string.Equals(group.Id, runtimeGroupId, StringComparison.Ordinal)).ToArray();
                if (matchingGroups.Length == 0)
                {
                    runtimeGroupId = null;
                    return true;
                }
                if (matchingGroups.Length != 1)
                {
                    return false;
                }
                runtimeGroup = matchingGroups[0];
            }
            var groupsController = Game.Instance?.UnitGroupsController;
            if (groupsController == null || !runtimeGroup.Empty())
            {
                return false;
            }
            if (groupsController.Groups.Any(group =>
                group != runtimeGroup && group != null &&
                string.Equals(group.Id, runtimeGroupId, StringComparison.Ordinal)))
            {
                return false;
            }
            groupsController.Groups.Remove(runtimeGroup);
            if (!runtimeGroup.Disposed)
            {
                runtimeGroup.Dispose();
            }
            var removed = runtimeGroup.Disposed && !groupsController.Groups.Contains(runtimeGroup);
            if (removed)
            {
                runtimeGroup = null;
                runtimeGroupId = null;
            }
            return removed;
        }

        private static bool ReleaseDetachedSpawnGroup(
            UnitGroupsController groupsController,
            UnitGroup[] groupsBeforeSpawn,
            UnitGroup spawnGroup,
            UnitEntityData spawnedTarget)
        {
            if (groupsController == null || groupsBeforeSpawn == null || spawnGroup == null ||
                spawnGroup.Any(unit => unit == spawnedTarget))
            {
                return false;
            }
            if (groupsBeforeSpawn.Contains(spawnGroup))
            {
                return true;
            }
            if (spawnGroup.IsPlayerParty || !spawnGroup.Empty())
            {
                return false;
            }
            groupsController.Groups.Remove(spawnGroup);
            if (!spawnGroup.Disposed)
            {
                spawnGroup.Dispose();
            }
            return spawnGroup.Disposed && !groupsController.Groups.Contains(spawnGroup);
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
