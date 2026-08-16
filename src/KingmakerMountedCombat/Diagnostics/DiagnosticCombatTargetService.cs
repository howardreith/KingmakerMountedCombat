using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Groups;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class DiagnosticCombatTargetService :
        IUnitLifeStateChanged,
        IGlobalRulebookHandler<RuleAttackWithWeapon>,
        IGlobalRulebookHandler<RuleDealDamage>,
        IDisposable
    {
        private const float MinimumPlacementDistance = 3f;
        private const float MaximumPlacementDistance = 20f;
        private const int DiagnosticDurabilityTemporaryHitPoints = 128;
        private const string DiagnosticDurabilitySource = "KMC diagnostic target durability";
        private const string RuntimeGroupPrefix = "KMC.RuntimeHostile.";
        private static readonly FieldInfo AiBackingField = typeof(UnitEntityData).GetField(
            "m_AiEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly IModLogger logger;
        private readonly DiagnosticCombatTargetLifecycle lifecycle = new DiagnosticCombatTargetLifecycle();
        private readonly IDisposable lifeStateSubscription;
        private BlueprintFaction runtimeFaction;
        private UnitGroup runtimeGroup;
        private string runtimeGroupId;
        private UnitEntityData target;
        private UnitEntityData expectedRider;
        private UnitEntityData expectedMount;
        private DiagnosticNonPairPartyAiLease nonPairPartyAiLease;
        private UnitEntityData combatMemoryObserver;
        private UnitEntityData combatMemoryTarget;
        private UnitGroup combatMemoryObserverGroup;
        private UnitGroup combatMemoryTargetGroup;
        private ModifiableValue.Modifier targetDurabilityModifier;
        private bool targetDurabilityLeaseRequired;
        private bool targetDurabilityLeaseActive;
        private bool targetSleeplessBefore;
        private bool targetSleeplessLeaseActive;
        private bool combatMemoryQueued;
        private bool combatMemoryRemoved = true;
        private bool expectedAttackDispatchStarted;
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

        public int TargetTemporaryHitPointsBefore { get; private set; }

        public int TargetTemporaryHitPointsAfterProvisioning { get; private set; }

        public int TargetDurabilityLeaseAmount { get; private set; }

        public bool TargetDurabilityLeaseAcquired { get; private set; }

        public bool TargetDurabilityLeaseReleased { get; private set; } = true;

        public bool TargetFogOfWarCleared { get; private set; }

        public bool TargetViewVisible { get; private set; }

        public bool TargetVisibleForPlayer { get; private set; }

        public bool TargetCommandsEmptyAtClick { get; private set; }

        public bool TargetAgentEnabledAtClick { get; private set; }

        public bool TargetAgentStoppedAtClick { get; private set; }

        public DiagnosticTargetLifeSnapshot LifeImmediatelyAfterCreation { get; private set; }

        public DiagnosticTargetLifeSnapshot LifeAtActivation { get; private set; }

        public DiagnosticTargetLifeSnapshot LastObservedLife { get; private set; }

        public DiagnosticTargetLifeTransition FirstLifeTransition { get; private set; }

        public int LifeTransitionCount { get; private set; }

        public int IncomingAttackRuleCount { get; private set; }

        public int IncomingDamageRuleCount { get; private set; }

        public int PreDispatchIncomingAttackRuleCount { get; private set; }

        public int PreDispatchIncomingDamageRuleCount { get; private set; }

        public bool ExpectedAttackDispatchStarted => expectedAttackDispatchStarted;

        public DiagnosticIncomingAttackSnapshot FirstIncomingAttack { get; private set; }

        public DiagnosticIncomingDamageSnapshot FirstIncomingDamage { get; private set; }

        public DiagnosticNonPairPartyAiLease NonPairPartyAiLease => nonPairPartyAiLease;

        public bool NonPairPartyAiLeaseRestored =>
            nonPairPartyAiLease == null || nonPairPartyAiLease.Restored;

        public DiagnosticCombatTargetService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            lifeStateSubscription = EventBus.Subscribe(this);
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
            bool exactWorkingAuthorized,
            bool requireDurabilityLease)
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
                expectedRider = rider;
                expectedMount = mount;
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

                // Acquire before the hostile target exists. The lease refuses any
                // pre-existing non-pair command and therefore never clears or owns a
                // player command in order to make target provisioning deterministic.
                nonPairPartyAiLease = new DiagnosticNonPairPartyAiLease();
                nonPairPartyAiLease.Acquire(rider, mount);

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
                LifeImmediatelyAfterCreation = DiagnosticTargetLifeSnapshot.Capture(target);
                LastObservedLife = LifeImmediatelyAfterCreation;
                AcquireTargetDurabilityLease(target, requireDurabilityLease);
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
                var durabilityPolicyPassed = requireDurabilityLease
                    ? TargetTemporaryHitPointsBefore == 0 &&
                        TargetTemporaryHitPointsAfterProvisioning == DiagnosticDurabilityTemporaryHitPoints &&
                        TargetDurabilityLeaseAmount == DiagnosticDurabilityTemporaryHitPoints &&
                        TargetDurabilityLeaseAcquired && !TargetDurabilityLeaseReleased
                    : TargetTemporaryHitPointsBefore == TargetTemporaryHitPointsAfterProvisioning &&
                        TargetDurabilityLeaseAmount == 0 && !TargetDurabilityLeaseAcquired &&
                        TargetDurabilityLeaseReleased;
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
                if (!lifecycle.Activate("pending:" + runId, safety.AllPassed && durabilityPolicyPassed))
                {
                    throw new InvalidOperationException(
                        "Diagnostic target failed its exact transient safety gates: " + safety.FailureSummary +
                        "; durabilityPolicy=" + durabilityPolicyPassed + ".");
                }

                LifeAtActivation = DiagnosticTargetLifeSnapshot.Capture(target);
                LastObservedLife = LifeAtActivation;

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
                TargetDurabilityLeaseReleased && TargetSleeplessLeaseReleased && NonPairPartyAiLeaseRestored)
            {
                if (State == DiagnosticCombatTargetState.DestroyRequested)
                {
                    return lifecycle.ConfirmRemoved(lifecycle.TargetId, true);
                }
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
            TargetCommandsEmptyAtClick = false;
            TargetAgentEnabledAtClick = false;
            TargetAgentStoppedAtClick = false;
            if (expectedTarget == null || expectedTarget != target ||
                State != DiagnosticCombatTargetState.Active ||
                !target.IsInState || target.View == null || target.View.AgentASP == null)
            {
                return false;
            }

            target.Commands.InterruptAll();
            target.Commands.RemoveFinishedAndUpdateQueue();
            target.View.AgentASP.Stop();
            target.IsInFogOfWar = false;
            target.View.SetVisible(true, true);
            TargetFogOfWarCleared = !target.IsInFogOfWar;
            TargetViewVisible = target.View.IsVisible;
            TargetVisibleForPlayer = target.IsVisibleForPlayer;
            TargetCommandsEmptyAtClick = target.Commands.Empty;
            TargetAgentEnabledAtClick = target.View.AgentASP.enabled;
            TargetAgentStoppedAtClick = !target.View.AgentASP.WantsToMove &&
                !target.View.AgentASP.IsReallyMoving &&
                target.View.AgentASP.Speed == 0f &&
                target.View.AgentASP.Velocity.sqrMagnitude == 0f;
            return TargetFogOfWarCleared && TargetViewVisible && TargetVisibleForPlayer &&
                TargetCommandsEmptyAtClick && TargetAgentEnabledAtClick && TargetAgentStoppedAtClick;
        }

        public bool CaptureCurrentLife(UnitEntityData expectedTarget)
        {
            ThrowIfDisposed();
            if (expectedTarget == null || expectedTarget != target || !target.IsInState)
            {
                return false;
            }
            var current = DiagnosticTargetLifeSnapshot.Capture(target);
            if (current == null)
            {
                return false;
            }
            LastObservedLife = current;
            return true;
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

        public void ObserveTargetLifeState()
        {
            ThrowIfDisposed();
            if (target != null)
            {
                LastObservedLife = DiagnosticTargetLifeSnapshot.Capture(target);
            }
        }

        public bool BeginExpectedAttackDispatch(UnitEntityData expectedTarget)
        {
            ThrowIfDisposed();
            if (expectedAttackDispatchStarted || expectedTarget == null || expectedTarget != target ||
                State != DiagnosticCombatTargetState.Active || !expectedTarget.IsInState)
            {
                return false;
            }

            expectedAttackDispatchStarted = true;
            return true;
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (!IsIncomingTarget(evt?.Target))
            {
                return;
            }

            IncomingAttackRuleCount++;
            var beforeExpectedDispatch = !expectedAttackDispatchStarted;
            if (beforeExpectedDispatch)
            {
                PreDispatchIncomingAttackRuleCount++;
            }
            if (FirstIncomingAttack == null)
            {
                FirstIncomingAttack = DiagnosticIncomingAttackSnapshot.Capture(
                    evt,
                    beforeExpectedDispatch,
                    expectedRider,
                    expectedMount,
                    AiBackingField);
            }
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!IsIncomingTarget(evt?.Target))
            {
                return;
            }

            IncomingDamageRuleCount++;
            var beforeExpectedDispatch = !expectedAttackDispatchStarted;
            if (beforeExpectedDispatch)
            {
                PreDispatchIncomingDamageRuleCount++;
            }
            if (FirstIncomingDamage == null)
            {
                FirstIncomingDamage = DiagnosticIncomingDamageSnapshot.Capture(
                    evt,
                    beforeExpectedDispatch);
            }
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            if (disposed || unit == null || unit != target)
            {
                return;
            }

            var current = DiagnosticTargetLifeSnapshot.Capture(unit);
            LifeTransitionCount++;
            if (FirstLifeTransition == null)
            {
                FirstLifeTransition = new DiagnosticTargetLifeTransition(
                    prevLifeState.ToString(),
                    current.LifeState,
                    current);
            }
            LastObservedLife = current;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            try
            {
                if (!DestroyAndVerify())
                {
                    throw new InvalidOperationException("Diagnostic target cleanup retained entity or faction residue.");
                }
            }
            finally
            {
                try { nonPairPartyAiLease?.Dispose(); }
                finally
                {
                    lifeStateSubscription.Dispose();
                    disposed = true;
                }
            }
        }

        private bool BestEffortDestroy()
        {
            var current = target;
            var combatMemoryClean = RemoveCombatMemory();
            var durabilityLeaseClean = ReleaseTargetDurabilityLease(current);
            var sleeplessLeaseClean = ReleaseTargetSleeplessLease(current);
            if (current != null && durabilityLeaseClean)
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
            var nonPairPartyAiClean = targetRemoved && groupRemoved && RuntimeFactionRemoved &&
                (nonPairPartyAiLease == null || nonPairPartyAiLease.RestoreAndVerify());
            return targetRemoved && groupRemoved && RuntimeFactionRemoved && combatMemoryClean &&
                durabilityLeaseClean && sleeplessLeaseClean && nonPairPartyAiClean;
        }

        private void AcquireTargetDurabilityLease(UnitEntityData current, bool required)
        {
            var temporaryHitPoints = current?.Descriptor?.Stats?.TemporaryHitPoints;
            if (temporaryHitPoints == null)
            {
                throw new InvalidOperationException("Diagnostic target temporary-hit-point stat is unavailable.");
            }

            targetDurabilityLeaseRequired = required;
            TargetTemporaryHitPointsBefore = temporaryHitPoints.ModifiedValue;
            TargetTemporaryHitPointsAfterProvisioning = TargetTemporaryHitPointsBefore;
            TargetDurabilityLeaseAmount = 0;
            TargetDurabilityLeaseAcquired = false;
            TargetDurabilityLeaseReleased = true;
            if (!required)
            {
                return;
            }
            if (TargetTemporaryHitPointsBefore != 0)
            {
                throw new InvalidOperationException(
                    "Diagnostic target durability lease requires exactly zero pre-existing temporary hit points.");
            }

            TargetDurabilityLeaseReleased = false;
            targetDurabilityModifier = temporaryHitPoints.AddModifier(
                DiagnosticDurabilityTemporaryHitPoints,
                (Fact)null,
                DiagnosticDurabilitySource,
                ModifierDescriptor.UntypedStackable);
            TargetDurabilityLeaseAmount = DiagnosticDurabilityTemporaryHitPoints;
            TargetTemporaryHitPointsAfterProvisioning = temporaryHitPoints.ModifiedValue;
            targetDurabilityLeaseActive = targetDurabilityModifier != null &&
                targetDurabilityModifier.AppliedTo == temporaryHitPoints;
            TargetDurabilityLeaseAcquired = targetDurabilityLeaseActive &&
                targetDurabilityModifier.ModValue == DiagnosticDurabilityTemporaryHitPoints &&
                TargetTemporaryHitPointsAfterProvisioning ==
                    TargetTemporaryHitPointsBefore + DiagnosticDurabilityTemporaryHitPoints;
            if (!TargetDurabilityLeaseAcquired)
            {
                throw new InvalidOperationException(
                    "Diagnostic target durability lease did not acquire its exact temporary-hit-point modifier.");
            }
        }

        private bool ReleaseTargetDurabilityLease(UnitEntityData current)
        {
            if (!targetDurabilityLeaseRequired)
            {
                TargetDurabilityLeaseReleased = true;
                return true;
            }
            var temporaryHitPoints = current?.Descriptor?.Stats?.TemporaryHitPoints;
            if (temporaryHitPoints == null)
            {
                return false;
            }

            var modifierClean = true;
            if (targetDurabilityModifier != null)
            {
                if (targetDurabilityModifier.AppliedTo == temporaryHitPoints)
                {
                    modifierClean = targetDurabilityModifier.Remove();
                }
                else if (targetDurabilityModifier.AppliedTo != null)
                {
                    modifierClean = false;
                }
            }
            TargetDurabilityLeaseReleased = modifierClean &&
                temporaryHitPoints.ModifiedValue == TargetTemporaryHitPointsBefore;
            if (TargetDurabilityLeaseReleased)
            {
                targetDurabilityModifier = null;
                targetDurabilityLeaseActive = false;
            }
            return TargetDurabilityLeaseReleased;
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
                !combatMemoryObserver.IsInState || !combatMemoryTarget.IsInState ||
                nonPairPartyAiLease == null || !nonPairPartyAiLease.ValidateActive())
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

        private bool IsIncomingTarget(UnitEntityData observedTarget)
        {
            return !disposed && target != null && observedTarget == target;
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

    internal sealed class DiagnosticIncomingAttackSnapshot
    {
        private DiagnosticIncomingAttackSnapshot()
        {
        }

        public bool BeforeExpectedDispatch { get; private set; }

        public string InitiatorId { get; private set; }

        public string InitiatorBlueprintId { get; private set; }

        public bool InitiatorIsPlayerFaction { get; private set; }

        public bool InitiatorIsPlayersEnemy { get; private set; }

        public string InitiatorGroupId { get; private set; }

        public bool InitiatorGroupIsPlayerParty { get; private set; }

        public bool InitiatorSharesRiderGroup { get; private set; }

        public bool InitiatorSharesMountGroup { get; private set; }

        public bool InitiatorDirectlyControllable { get; private set; }

        public bool InitiatorEffectiveAiEnabled { get; private set; }

        public bool InitiatorRawAiEnabled { get; private set; }

        public bool InitiatorCommandsEmpty { get; private set; }

        public string WeaponBlueprintId { get; private set; }

        public bool IsAttackOfOpportunity { get; private set; }

        public bool IsCharge { get; private set; }

        public static DiagnosticIncomingAttackSnapshot Capture(
            RuleAttackWithWeapon rule,
            bool beforeExpectedDispatch,
            UnitEntityData expectedRider,
            UnitEntityData expectedMount,
            FieldInfo aiBackingField)
        {
            if (rule?.Initiator == null || rule.Target == null || rule.Weapon?.Blueprint == null ||
                aiBackingField == null || aiBackingField.FieldType != typeof(bool))
            {
                return null;
            }

            var initiatorGroup = rule.Initiator.Group;
            var riderGroup = expectedRider?.Group;
            var mountGroup = expectedMount?.Group;

            return new DiagnosticIncomingAttackSnapshot
            {
                BeforeExpectedDispatch = beforeExpectedDispatch,
                InitiatorId = rule.Initiator.UniqueId,
                InitiatorBlueprintId = rule.Initiator.Blueprint?.AssetGuid,
                InitiatorIsPlayerFaction = rule.Initiator.IsPlayerFaction,
                InitiatorIsPlayersEnemy = rule.Initiator.IsPlayersEnemy,
                InitiatorGroupId = initiatorGroup?.Id,
                InitiatorGroupIsPlayerParty = initiatorGroup != null && initiatorGroup.IsPlayerParty,
                InitiatorSharesRiderGroup = initiatorGroup != null && initiatorGroup == riderGroup,
                InitiatorSharesMountGroup = initiatorGroup != null && initiatorGroup == mountGroup,
                InitiatorDirectlyControllable = rule.Initiator.IsDirectlyControllable,
                InitiatorEffectiveAiEnabled = rule.Initiator.IsAIEnabled,
                InitiatorRawAiEnabled = (bool)aiBackingField.GetValue(rule.Initiator),
                InitiatorCommandsEmpty = rule.Initiator.Commands != null && rule.Initiator.Commands.Empty,
                WeaponBlueprintId = rule.Weapon.Blueprint.AssetGuid,
                IsAttackOfOpportunity = rule.IsAttackOfOpportunity,
                IsCharge = rule.IsCharge
            };
        }
    }

    internal sealed class DiagnosticIncomingDamageSnapshot
    {
        private DiagnosticIncomingDamageSnapshot()
        {
        }

        public bool BeforeExpectedDispatch { get; private set; }

        public string InitiatorId { get; private set; }

        public string InitiatorBlueprintId { get; private set; }

        public bool InitiatorIsPlayerFaction { get; private set; }

        public bool InitiatorIsPlayersEnemy { get; private set; }

        public int Damage { get; private set; }

        public bool IsFake { get; private set; }

        public bool IsDot { get; private set; }

        public bool AttackRollPresent { get; private set; }

        public string WeaponBlueprintId { get; private set; }

        public string SourceAbilityBlueprintId { get; private set; }

        public string SourceAreaBlueprintId { get; private set; }

        public static DiagnosticIncomingDamageSnapshot Capture(
            RuleDealDamage rule,
            bool beforeExpectedDispatch)
        {
            if (rule?.Initiator == null || rule.Target == null)
            {
                return null;
            }

            return new DiagnosticIncomingDamageSnapshot
            {
                BeforeExpectedDispatch = beforeExpectedDispatch,
                InitiatorId = rule.Initiator.UniqueId,
                InitiatorBlueprintId = rule.Initiator.Blueprint?.AssetGuid,
                InitiatorIsPlayerFaction = rule.Initiator.IsPlayerFaction,
                InitiatorIsPlayersEnemy = rule.Initiator.IsPlayersEnemy,
                Damage = rule.Damage,
                IsFake = rule.IsFake,
                IsDot = rule.IsDot,
                AttackRollPresent = rule.AttackRoll != null,
                WeaponBlueprintId = rule.AttackRoll?.Weapon?.Blueprint?.AssetGuid,
                SourceAbilityBlueprintId = rule.SourceAbility?.AssetGuid,
                SourceAreaBlueprintId = rule.SourceArea?.AssetGuid
            };
        }
    }

    internal sealed class DiagnosticTargetLifeSnapshot
    {
        private DiagnosticTargetLifeSnapshot()
        {
        }

        public string LifeState { get; private set; }

        public bool Conscious { get; private set; }

        public bool Dead { get; private set; }

        public bool FinallyDead { get; private set; }

        public int Damage { get; private set; }

        public int NonLethalDamage { get; private set; }

        public int HitPoints { get; private set; }

        public int Constitution { get; private set; }

        public bool ForceKill { get; private set; }

        public bool MarkedForDeath { get; private set; }

        public static DiagnosticTargetLifeSnapshot Capture(UnitEntityData unit)
        {
            var state = unit?.Descriptor?.State;
            if (unit == null || state == null || unit.Stats == null)
            {
                return null;
            }

            return new DiagnosticTargetLifeSnapshot
            {
                LifeState = state.LifeState.ToString(),
                Conscious = state.IsConscious,
                Dead = state.IsDead,
                FinallyDead = state.IsFinallyDead,
                Damage = unit.Damage,
                NonLethalDamage = unit.DamageNonLethal,
                HitPoints = (int)unit.Stats.HitPoints,
                Constitution = (int)unit.Stats.Constitution,
                ForceKill = state.ForceKill,
                MarkedForDeath = state.MarkedForDeath
            };
        }
    }

    internal sealed class DiagnosticTargetLifeTransition
    {
        public DiagnosticTargetLifeTransition(
            string previousLifeState,
            string currentLifeState,
            DiagnosticTargetLifeSnapshot snapshot)
        {
            PreviousLifeState = previousLifeState;
            CurrentLifeState = currentLifeState;
            Snapshot = snapshot;
        }

        public string PreviousLifeState { get; }

        public string CurrentLifeState { get; }

        public DiagnosticTargetLifeSnapshot Snapshot { get; }
    }
}
