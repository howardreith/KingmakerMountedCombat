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
        private UnitEntityData combatMemoryObserver;
        private UnitEntityData combatMemoryTarget;
        private UnitGroup combatMemoryObserverGroup;
        private UnitGroup combatMemoryTargetGroup;
        private bool targetSleeplessBefore;
        private bool targetSleeplessLeaseActive;
        private bool combatMemoryQueued;
        private bool combatMemoryRemoved = true;
        private bool runtimeFactionDestroyPending;
        private bool disposed;

        public string CreatedRuntimeGroupId { get; private set; }

        public string BlueprintEmptyHandWeaponBlueprintId { get; private set; }

        public string TargetNativeSingleAttackWeaponBlueprintId { get; private set; }

        public string TargetNativeSingleAttackSlot { get; private set; }

        public int TargetPrimaryMainAttacks { get; private set; }

        public int TargetSecondaryMainAttacks { get; private set; }

        public int AdditionalLimbCountBefore { get; private set; }

        public int AdditionalLimbCountAfter { get; private set; }

        public bool NoWeaponProvisioningMutation { get; private set; }

        public bool TargetPrimaryHandHasItem { get; private set; }

        public bool TargetWeaponUsesEmptyHandFallback { get; private set; }

        public bool TargetNativeSingleAttackWeaponIsNatural { get; private set; }

        public bool TargetNativeSingleAttackWeaponIsMelee { get; private set; }

        public bool TargetHasNoLoot { get; private set; }

        public bool RawAiBackingDisabled { get; private set; }

        public bool BidirectionalHostilityVerified { get; private set; }

        public bool NoExperienceReward { get; private set; }

        public bool TargetSleeplessBefore => targetSleeplessBefore;

        public bool TargetSleeplessLeaseAcquired { get; private set; }

        public bool TargetSleeplessLeaseReleased { get; private set; } = true;

        public bool TargetFogOfWarCleared { get; private set; }

        public bool TargetViewVisible { get; private set; }

        public bool TargetVisibleForPlayer { get; private set; }

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

        public bool CombatMemoryQueued => combatMemoryQueued;

        public bool PlayerGroupMemoryContainsTarget =>
            combatMemoryObserver != null && combatMemoryTarget != null &&
            combatMemoryObserverGroup != null && !combatMemoryObserverGroup.Disposed &&
            combatMemoryObserver.Group == combatMemoryObserverGroup &&
            combatMemoryObserverGroup.Memory.Contains(combatMemoryTarget);

        public bool TargetGroupMemoryContainsRider =>
            combatMemoryObserver != null && combatMemoryTarget != null &&
            combatMemoryTargetGroup != null && !combatMemoryTargetGroup.Disposed &&
            combatMemoryTarget.Group == combatMemoryTargetGroup &&
            combatMemoryTargetGroup.Memory.Contains(combatMemoryObserver);

        public bool CombatMemoryRemoved => combatMemoryRemoved;

        public UnitEntityData Spawn(
            UnitEntityData rider,
            UnitEntityData mount,
            Vector3 position,
            string runId,
            bool exactWorkingAuthorized)
        {
            ThrowIfDisposed();
            if (rider == null || !rider.IsInState || mount == null || !mount.IsInState ||
                !string.Equals(mount.Blueprint?.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(runId))
            {
                throw new InvalidOperationException("Diagnostic target requires the exact live rider, Mammoth profile, and run ID.");
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

                var proposedRuntimeGroupId = RuntimeGroupPrefix + runId;
                if (groupsController.Groups.Any(group =>
                    group != null && string.Equals(group.Id, proposedRuntimeGroupId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Diagnostic target runtime group identity already exists.");
                }

                var blueprintPrimary = blueprint.Body?.EmptyHandWeapon;
                if (blueprint.Body == null || blueprint.Body.DisableHands ||
                    blueprintPrimary == null || !blueprintPrimary.IsNatural ||
                    blueprintPrimary.IsRanged ||
                    string.IsNullOrWhiteSpace(blueprintPrimary.AssetGuid))
                {
                    throw new InvalidOperationException("Exact stock Mammoth empty-hand natural-weapon source is unavailable.");
                }
                BlueprintEmptyHandWeaponBlueprintId = blueprintPrimary.AssetGuid;

                runtimeFaction = ScriptableObject.CreateInstance<BlueprintFaction>();
                runtimeFaction.name = "KMC_RuntimeHostile_" + runId;
                runtimeFaction.hideFlags = HideFlags.HideAndDontSave;
                runtimeFaction.AttackFactions = new[] { playerFaction };
                runtimeFaction.AlwaysEnemy = true;
                runtimeGroupId = proposedRuntimeGroupId;
                CreatedRuntimeGroupId = proposedRuntimeGroupId;

                var groupsBeforeSpawn = groupsController.Groups.Where(group => group != null).ToArray();
                target = game.EntityCreator.SpawnUnit(blueprint, position, Quaternion.identity, state);
                game.EntityCreator.Tick();
                if (target == null || !target.IsInState || target.View == null || target.View.AgentASP == null)
                {
                    throw new InvalidOperationException("Diagnostic Mammoth target did not enter the exact loaded state.");
                }
                targetSleeplessBefore = target.Sleepless;
                target.Sleepless = true;
                targetSleeplessLeaseActive = target.Sleepless;
                TargetSleeplessLeaseAcquired = !targetSleeplessBefore && targetSleeplessLeaseActive;
                TargetSleeplessLeaseReleased = false;
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

                AdditionalLimbCountBefore = target.Body.AdditionalLimbs.Count;
                var primaryHandHasItemBefore = target.Body.PrimaryHand.HasItem;
                var primaryHandItemBefore = target.Body.PrimaryHand.MaybeItem;
                var emptyHandWeaponBefore = target.Body.EmptyHandWeapon;
                var nativePrimary = NativeSingleAttackWeaponResolver.Resolve(target);
                AdditionalLimbCountAfter = target.Body.AdditionalLimbs.Count;
                var primary = nativePrimary?.Weapon;
                TargetNativeSingleAttackWeaponBlueprintId = primary?.Blueprint?.AssetGuid;
                TargetNativeSingleAttackSlot = nativePrimary?.Kind.ToString();
                TargetPrimaryMainAttacks = nativePrimary?.PrimaryMainAttacks ?? 0;
                TargetSecondaryMainAttacks = nativePrimary?.SecondaryMainAttacks ?? 0;
                NoWeaponProvisioningMutation = AdditionalLimbCountAfter == AdditionalLimbCountBefore &&
                    target.Body.PrimaryHand.HasItem == primaryHandHasItemBefore &&
                    target.Body.PrimaryHand.MaybeItem == primaryHandItemBefore &&
                    target.Body.EmptyHandWeapon == emptyHandWeaponBefore;
                TargetPrimaryHandHasItem = primaryHandHasItemBefore;
                TargetWeaponUsesEmptyHandFallback = !primaryHandHasItemBefore &&
                    primary == emptyHandWeaponBefore && primary?.Blueprint == blueprintPrimary;
                TargetNativeSingleAttackWeaponIsNatural = primary?.Blueprint != null && primary.Blueprint.IsNatural;
                TargetNativeSingleAttackWeaponIsMelee = primary?.Blueprint != null && !primary.Blueprint.IsRanged;
                var nativePrimaryResolvedWithoutProvisioning = NoWeaponProvisioningMutation &&
                    nativePrimary?.Kind == NativeSingleAttackSlotKind.PrimaryHand &&
                    nativePrimary.Slot == target.Body.PrimaryHand &&
                    nativePrimary.PrimaryMainAttacks > 0 &&
                    (TargetPrimaryHandHasItem != TargetWeaponUsesEmptyHandFallback);
                var dedicatedRuntimeGroup = detachedFromSpawnGroup && runtimeGroup != null &&
                    !runtimeGroup.IsPlayerParty &&
                    string.Equals(runtimeGroup.Id, runtimeGroupId, StringComparison.Ordinal) &&
                    runtimeGroup.Count == 1 && runtimeGroup[0] == target;
                var inventoryHasNoLoot = target.Inventory == null || !target.Inventory.HasLoot;
                var aiBackingDisabled = !(bool)AiBackingField.GetValue(target);
                var targetTreatsRiderAsEnemy = target.IsEnemy(rider);
                var riderTreatsTargetAsEnemy = rider.IsEnemy(target);
                var bidirectionalHostility = targetTreatsRiderAsEnemy && riderTreatsTargetAsEnemy;
                TargetHasNoLoot = inventoryHasNoLoot;
                RawAiBackingDisabled = aiBackingDisabled;
                BidirectionalHostilityVerified = bidirectionalHostility;
                NoExperienceReward = !target.GiveExperienceOnDeath;
                var safety = new DiagnosticCombatTargetSafetySnapshot(
                    target.Blueprint == blueprint,
                    !target.IsDirectlyControllable,
                    !target.Descriptor.IsPet,
                    target.Descriptor.Master.Value == null,
                    target.Faction == runtimeFaction,
                    dedicatedRuntimeGroup,
                    targetTreatsRiderAsEnemy,
                    riderTreatsTargetAsEnemy,
                    NoExperienceReward,
                    aiBackingDisabled,
                    TargetSleeplessLeaseAcquired,
                    target.Commands.Empty,
                    inventoryHasNoLoot,
                    nativePrimaryResolvedWithoutProvisioning,
                    primary?.Blueprint != null,
                    TargetNativeSingleAttackWeaponIsNatural,
                    TargetNativeSingleAttackWeaponIsMelee);
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
                        "Diagnostic target creation failed and immediate cleanup proof was unavailable: " + exception.Message,
                        exception);
                }
                throw;
            }
        }

        public bool DestroyAndVerify()
        {
            if (TargetEntityRemoved && RuntimeGroupRemoved && RuntimeFactionRemoved && CombatMemoryRemoved &&
                TargetSleeplessLeaseReleased)
            {
                return State == DiagnosticCombatTargetState.Absent ||
                    State == DiagnosticCombatTargetState.Removed;
            }

            lifecycle.RequestDestroy("bounded cleanup");
            var zeroResidue = BestEffortDestroy();
            var confirmed = lifecycle.ConfirmRemoved(lifecycle.TargetId, zeroResidue);
            return zeroResidue && confirmed;
        }

        public bool PrepareForPlayerClick(UnitEntityData expectedTarget)
        {
            ThrowIfDisposed();
            TargetFogOfWarCleared = false;
            TargetViewVisible = false;
            TargetVisibleForPlayer = false;
            if (expectedTarget == null || expectedTarget != target ||
                State != DiagnosticCombatTargetState.Active ||
                !target.IsInState || target.View == null)
            {
                return false;
            }

            target.IsInFogOfWar = false;
            target.View.SetVisible(true, true);
            TargetFogOfWarCleared = !target.IsInFogOfWar;
            TargetViewVisible = target.View.IsVisible;
            TargetVisibleForPlayer = target.IsVisibleForPlayer;
            return TargetFogOfWarCleared && TargetViewVisible && TargetVisibleForPlayer;
        }

        public bool QueueBidirectionalCombatMemory(UnitEntityData rider, UnitEntityData expectedTarget)
        {
            ThrowIfDisposed();
            if (combatMemoryQueued || rider == null || expectedTarget == null || expectedTarget != target ||
                State != DiagnosticCombatTargetState.Active || !rider.IsInState || !target.IsInState ||
                !targetSleeplessLeaseActive || !target.Sleepless ||
                rider.Group == null || target.Group == null || rider.Group == target.Group ||
                !rider.IsEnemy(target) || !target.IsEnemy(rider) ||
                !TargetFogOfWarCleared || !TargetViewVisible || !TargetVisibleForPlayer)
            {
                return false;
            }

            var game = Game.Instance;
            if (game?.TimeController == null)
            {
                return false;
            }

            combatMemoryObserver = rider;
            combatMemoryTarget = target;
            combatMemoryObserverGroup = rider.Group;
            combatMemoryTargetGroup = target.Group;
            combatMemoryRemoved = false;
            combatMemoryQueued = TryRefreshBidirectionalCombatMemoryLease();
            if (!combatMemoryQueued)
            {
                RemoveCombatMemory();
            }
            return combatMemoryQueued;
        }

        public bool RefreshBidirectionalCombatMemoryLease()
        {
            ThrowIfDisposed();
            return combatMemoryQueued && !combatMemoryRemoved &&
                TryRefreshBidirectionalCombatMemoryLease();
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
            var combatMemoryClean = RemoveCombatMemory();
            var sleeplessLeaseClean = ReleaseTargetSleeplessLease(current);
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
            return targetRemoved && groupRemoved && RuntimeFactionRemoved && combatMemoryClean && sleeplessLeaseClean;
        }

        private bool RemoveCombatMemory()
        {
            if (combatMemoryRemoved)
            {
                return true;
            }
            if (combatMemoryObserver == null || combatMemoryTarget == null ||
                combatMemoryObserverGroup == null || combatMemoryTargetGroup == null)
            {
                return false;
            }

            combatMemoryObserverGroup.Memory.Remove(combatMemoryTarget);
            combatMemoryTargetGroup.Memory.Remove(combatMemoryObserver);
            combatMemoryRemoved = !combatMemoryObserverGroup.Memory.Contains(combatMemoryTarget) &&
                !combatMemoryTargetGroup.Memory.Contains(combatMemoryObserver);
            if (combatMemoryRemoved)
            {
                combatMemoryObserver = null;
                combatMemoryTarget = null;
                combatMemoryObserverGroup = null;
                combatMemoryTargetGroup = null;
            }
            return combatMemoryRemoved;
        }

        private bool TryRefreshBidirectionalCombatMemoryLease()
        {
            var game = Game.Instance;
            if (game?.TimeController == null || combatMemoryObserver == null || combatMemoryTarget == null ||
                combatMemoryObserverGroup == null || combatMemoryTargetGroup == null ||
                combatMemoryObserverGroup.Disposed || combatMemoryTargetGroup.Disposed ||
                combatMemoryObserver.Group != combatMemoryObserverGroup ||
                combatMemoryTarget.Group != combatMemoryTargetGroup ||
                !targetSleeplessLeaseActive || !combatMemoryTarget.Sleepless ||
                !combatMemoryObserver.IsInState || !combatMemoryTarget.IsInState)
            {
                return false;
            }

            var observedTarget = combatMemoryObserverGroup.Memory.Add(combatMemoryTarget);
            var observedRider = combatMemoryTargetGroup.Memory.Add(combatMemoryObserver);
            if (observedTarget == null || observedRider == null)
            {
                return false;
            }
            observedTarget.LastDetectTime = game.TimeController.GameTime;
            observedRider.LastDetectTime = game.TimeController.GameTime;
            if (!combatMemoryObserver.IsAwake)
            {
                combatMemoryObserver.Wake();
            }
            if (!combatMemoryTarget.IsAwake)
            {
                combatMemoryTarget.Wake();
            }
            return PlayerGroupMemoryContainsTarget && TargetGroupMemoryContainsRider;
        }

        private bool ReleaseTargetSleeplessLease(UnitEntityData current)
        {
            if (!targetSleeplessLeaseActive)
            {
                return TargetSleeplessLeaseReleased;
            }
            if (current == null)
            {
                targetSleeplessLeaseActive = false;
                TargetSleeplessLeaseReleased = true;
                return true;
            }

            current.Sleepless = targetSleeplessBefore;
            TargetSleeplessLeaseReleased = current.Sleepless == targetSleeplessBefore;
            if (TargetSleeplessLeaseReleased)
            {
                targetSleeplessLeaseActive = false;
            }
            return TargetSleeplessLeaseReleased;
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
