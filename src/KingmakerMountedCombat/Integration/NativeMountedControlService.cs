using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class NativeMountedControlSnapshot
    {
        public bool Registered { get; set; }
        public bool Enabled { get; set; }
        public bool SerializationSuspended { get; set; }
        public int ExactFactCount { get; set; }
        public int DuplicateFactCount { get; set; }
        public int ManagedHotbarSlotCount { get; set; }
        public long TargetSelectionStartCount { get; set; }
        public long TargetSelectionEndCount { get; set; }
        public long NativeCastRequestCount { get; set; }
        public long NativeRefusalCount { get; set; }
        public long DispatchAcceptedCount { get; set; }
        public long DispatchRejectedCount { get; set; }
        public long NativePrimaryShellPrepareCount { get; set; }
        public string LastNativePrimaryShellObservation { get; set; }
        public int ActivationRecordCount { get; set; }
        public int RiderPrimaryRelationshipEndCount { get; set; }
    }

    internal sealed class NativeMountedControlService : IDisposable,
        IClickActionHandler,
        IAbilityTargetSelectionUIHandler
    {
        internal const string MountAbilityGuid = "f053faad986631688defa003cd7bda0e";
        internal const string DismountAbilityGuid = "3af2b81f4d72bbb30501fa730fcdf36e";
        internal const string RiderPrimaryAbilityGuid = "27364df661b3c121eabb97a31aa73a83";
        internal const string MountPrimaryAbilityGuid = "f88a50d6fdbebbd709c3e323d2f52f5e";

        private const int DisplayNameFieldToken = 0x04006955;
        private const int DescriptionFieldToken = 0x04006956;
        private const int IconFieldToken = 0x04006957;
        private const int LocalizedStringKeyFieldToken = 0x04004C56;

        private static readonly FieldInfo DisplayNameField = ResolveField(
            typeof(BlueprintUnitFact), "m_DisplayName", DisplayNameFieldToken, typeof(LocalizedString));
        private static readonly FieldInfo DescriptionField = ResolveField(
            typeof(BlueprintUnitFact), "m_Description", DescriptionFieldToken, typeof(LocalizedString));
        private static readonly FieldInfo IconField = ResolveField(
            typeof(BlueprintUnitFact), "m_Icon", IconFieldToken, typeof(Sprite));
        private static readonly FieldInfo LocalizedStringKeyField = ResolveField(
            typeof(LocalizedString), "m_Key", LocalizedStringKeyFieldToken, typeof(string));
        private static readonly MethodInfo MountApproachRadiusSetter = ResolveMountApproachRadiusSetter();

        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPlayerActionController playerAction;
        private readonly MountedCombatController combat;
        private readonly HorseCompanionBlueprintService horseCompanion;
        private readonly DiagnosticSettings settings;
        private readonly NativeLifecycleDeliveryLedger lifecycleLedger;
        private readonly NativeMountedAbilityActivationLedger activationLedger =
            new NativeMountedAbilityActivationLedger();
        private readonly IModLogger logger;
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private readonly List<UnitEntityData> observedUnits = new List<UnitEntityData>();
        private readonly List<HotbarSerializationLease> hotbarSerializationLeases =
            new List<HotbarSerializationLease>();
        private readonly Dictionary<string, string> localization =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "KMC.Native.Mount.Name", "Mount Companion" },
                { "KMC.Native.Empty", "" },
                { "KMC.Native.Mount.Description", "Outside combat, select your active Horse or Mammoth. Walk into reach and mount. You can queue this order while gameplay is paused; it executes after unpausing." },
                { "KMC.Native.Dismount.Name", "Dismount" },
                { "KMC.Native.Dismount.Description", "Get off your companion. Mounted seating is temporary and is cleared before saving or changing areas." },
                { "KMC.Native.RiderPrimary.Name", "Rider Primary" },
                { "KMC.Native.RiderPrimary.Description", "Make one mounted attack with your equipped melee or ranged weapon. Uses the rider's Standard action. In turn-based mode, use this on the rider's own turn. Private preview: mounted turn-based movement costs are not yet qualified." },
                { "KMC.Native.MountPrimary.Name", "Mount Primary" },
                { "KMC.Native.MountPrimary.Description", "Make one natural attack with your Horse or Mammoth, using its Standard action. In turn-based mode, select the companion on its own turn. You may then use its remaining legal actions. Private preview: mounted turn-based movement costs are not yet qualified." }
            };

        private List<BlueprintScriptableObject> blueprintList;
        private BlueprintAbility mountAbility;
        private BlueprintAbility dismountAbility;
        private BlueprintAbility riderPrimaryAbility;
        private BlueprintAbility mountPrimaryAbility;
        private bool registered;
        private bool enabled;
        private bool subscribed;
        private bool serializationSuspended;
        private bool targetSelectionMode;
        private PendingNativeActivation pendingActivation;
        private bool disposed;

        public NativeMountedControlService(
            GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction,
            MountedCombatController combat,
            HorseCompanionBlueprintService horseCompanion,
            DiagnosticSettings settings,
            NativeLifecycleDeliveryLedger lifecycleLedger,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.horseCompanion = horseCompanion ?? throw new ArgumentNullException(nameof(horseCompanion));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.lifecycleLedger = lifecycleLedger ?? throw new ArgumentNullException(nameof(lifecycleLedger));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            NativeMountedAbilityBridge.Service = this;
        }

        internal BlueprintAbility MountAbility => mountAbility;

        internal BlueprintAbility DismountAbility => dismountAbility;

        internal BlueprintAbility RiderPrimaryAbility => riderPrimaryAbility;

        internal BlueprintAbility MountPrimaryAbility => mountPrimaryAbility;

        internal bool SerializationSuspended => serializationSuspended;

        internal long TargetSelectionStartCount { get; private set; }

        internal long TargetSelectionEndCount { get; private set; }

        internal long NativeCastRequestCount { get; private set; }

        internal long NativeRefusalCount { get; private set; }

        internal long DispatchAcceptedCount { get; private set; }

        internal long DispatchRejectedCount { get; private set; }

        internal long NativePrimaryShellPrepareCount { get; private set; }

        internal string LastNativePrimaryShellObservation { get; private set; } = "not-observed";

        internal void PrepareNativeMountApproach(UnitUseAbility command)
        {
            if (disposed || !enabled || !registered || serializationSuspended ||
                command == null || command.IsStarted || command.IsFinished ||
                !ReferenceEquals(command.Spell?.Blueprint, mountAbility)) { return; }
            var caster = command.Executor;
            var target = command.Target?.Unit;
            if (caster?.View == null || target?.View == null || command.Spell.Caster?.Unit != caster ||
                target != caster.Descriptor?.Pet || target.Descriptor?.Master.Value != caster ||
                !SupportedMountedProfiles.IsSupported(target)) { return; }
            float radius;
            if (CombatMountDismountPolicy.TryGetMountApproachRadius(command.ApproachRadius,
                caster.View.Corpulence, target.View.Corpulence, out radius))
            {
                MountApproachRadiusSetter.Invoke(command, new object[] { radius });
            }
        }

        private static MethodInfo ResolveMountApproachRadiusSetter()
        {
            var setter = typeof(UnitCommand).GetProperty("ApproachRadius")?.GetSetMethod(true);
            if (setter == null || setter.MetadataToken != 0x06002767 || setter.IsStatic ||
                setter.ReturnType != typeof(void) || setter.GetParameters().Length != 1 ||
                setter.GetParameters()[0].ParameterType != typeof(float))
            {
                throw new InvalidOperationException("Exact native Mount approach setter is unavailable.");
            }
            return setter;
        }

        internal bool PrepareNativePrimaryIntentShell(UnitUseAbility command)
        {
            var blueprint = command?.Spell?.Blueprint;
            var kind = ResolveKind(blueprint);
            var rider = relationship.Rider;
            var exactManagedAbility = ReferenceEquals(blueprint, riderPrimaryAbility) ||
                ReferenceEquals(blueprint, mountPrimaryAbility);
            var casterIsExactActor = command?.Executor != null && command.Spell?.Caster?.Unit == command.Executor &&
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(kind, CombatController.IsInTurnBasedCombat(),
                    settings.EnableUnifiedMountedTurn, command.Executor == rider, command.Executor == relationship.Mount);
            if (!NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    kind,
                    !disposed && enabled && registered && !serializationSuspended,
                    relationship.State == RelationshipState.Mounted,
                    exactManagedAbility,
                    casterIsExactActor))
            {
                return false;
            }

            var needLineOfSightBefore = command.NeedLoS;
            var ignoreCooldownBefore = command.IsIgnoreCooldown;
            command.NeedLoS = false;
            command.IgnoreCooldown();
            NativePrimaryShellPrepareCount++;
            LastNativePrimaryShellObservation = "kind=" + kind +
                ";commandOwner=" + command.Executor.UniqueId +
                ";target=" + (command.Target?.Unit?.UniqueId ?? "<none>") +
                ";needLoSBefore=" + needLineOfSightBefore +
                ";needLoSAfter=" + command.NeedLoS +
                ";ignoreCooldownBefore=" + ignoreCooldownBefore +
                ";ignoreCooldownAfter=" + command.IsIgnoreCooldown +
                ";approachRadius=" + command.ApproachRadius.ToString("R", CultureInfo.InvariantCulture);
            logger.Info("Prepared exact mounted primary native intent shell: " +
                LastNativePrimaryShellObservation + ".");
            return true;
        }

        internal void SetEnabled(bool value)
        {
            ThrowIfDisposed();
            enabled = value;
            if (enabled)
            {
                EnsureSubscribed();
                Update();
            }
            else
            {
                RemoveAllManagedFacts(true);
                ClearSerializationHotbarLeases(false);
                EnsureUnsubscribed();
            }
        }

        internal void Update()
        {
            ObservePendingRiderPrimaryOutcome();
            if (disposed || !enabled || serializationSuspended)
            {
                return;
            }

            if (!registered)
            {
                TryRegister();
            }
            if (!registered)
            {
                return;
            }

            ReconcileFacts();
        }

        internal NativeMountedControlAvailability Evaluate(
            NativeMountedControlKind kind,
            UnitEntityData caster)
        {
            if (disposed || !enabled || !registered || serializationSuspended)
            {
                return new NativeMountedControlAvailability(false, false, "Mounted control services are not active.");
            }

            if (!settings.EnableUnsafeMovementExperiment)
            {
                return new NativeMountedControlAvailability(
                    true,
                    false,
                    "Enable private-alpha mounted controls in the KMC mod settings.");
            }

            if (caster == null || caster.Descriptor == null || !caster.IsInGame)
            {
                return new NativeMountedControlAvailability(false, false, "The ability owner is not active in the current area.");
            }

            switch (kind)
            {
                case NativeMountedControlKind.MountCompanion:
                    return playerAction.GetNativeMountAvailability(caster);
                case NativeMountedControlKind.Dismount:
                    return playerAction.GetNativeDismountAvailability(caster);
                case NativeMountedControlKind.RiderPrimary:
                    return combat.GetNativeAbilityAvailability(combat.ResolveRiderPrimaryAction(), caster);
                case NativeMountedControlKind.MountPrimary:
                    return combat.GetNativeAbilityAvailability(MountedCombatActionKind.MountPrimaryNatural, caster);
                default:
                    return new NativeMountedControlAvailability(false, false, "Unknown mounted control.");
            }
        }

        internal bool CanTarget(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            UnitEntityData target)
        {
            if (!Evaluate(kind, caster).IsEnabled)
            {
                return false;
            }

            switch (kind)
            {
                case NativeMountedControlKind.MountCompanion:
                    return playerAction.CanNativeMountTarget(caster, target);
                case NativeMountedControlKind.Dismount:
                    return target == caster;
                case NativeMountedControlKind.RiderPrimary:
                    return combat.CanNativeAbilityTarget(combat.ResolveRiderPrimaryAction(), caster, target);
                case NativeMountedControlKind.MountPrimary:
                    return combat.CanNativeAbilityTarget(MountedCombatActionKind.MountPrimaryNatural, caster, target);
                default:
                    return false;
            }
        }

        internal bool TryDispatch(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            UnitEntityData target)
        {
            var activation = EnsureActivation(kind, caster);
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.DispatchStarted,
                caster,
                target,
                null,
                "dispatch-started");
            bool accepted;
            try
            {
                switch (kind)
                {
                    case NativeMountedControlKind.MountCompanion:
                        accepted = playerAction.TryExecuteNativeMount(caster, target);
                        break;
                    case NativeMountedControlKind.Dismount:
                        accepted = playerAction.TryExecuteNativeDismount(caster);
                        break;
                    case NativeMountedControlKind.RiderPrimary:
                        accepted = combat.TryExecuteNativeAbility(
                            combat.ResolveRiderPrimaryAction(), caster, target) ==
                            MountedCombatClickResult.HandledAccepted;
                        break;
                    case NativeMountedControlKind.MountPrimary:
                        accepted = combat.TryExecuteNativeAbility(
                            MountedCombatActionKind.MountPrimaryNatural, caster, target) ==
                            MountedCombatClickResult.HandledAccepted;
                        break;
                    default:
                        accepted = false;
                        break;
                }
            }
            catch (Exception exception)
            {
                RecordActivation(
                    activation,
                    NativeMountedAbilityActivationPhase.DispatchCompleted,
                    caster,
                    target,
                    false,
                    "exception:" + exception.GetType().FullName);
                pendingActivation = null;
                throw;
            }

            if (accepted)
            {
                DispatchAcceptedCount++;
            }
            else
            {
                DispatchRejectedCount++;
                var refusal = kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount
                    ? playerAction.LastFeedback : combat.LastFeedback;
                if (string.IsNullOrWhiteSpace(refusal)) refusal = DescribeRefusal(kind, caster, target);
                RaiseWarning(refusal);
                logger.Info("Native mounted dispatch refusal: " + refusal);
            }
            logger.Info("Native mounted ability dispatch: kind=" + kind +
                "; casterId=" + (caster?.UniqueId ?? "<none>") +
                "; targetId=" + (target?.UniqueId ?? "<none>") +
                "; accepted=" + accepted + ".");
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.DispatchCompleted,
                caster,
                target,
                accepted,
                accepted ? "accepted" : "rejected");
            if (kind == NativeMountedControlKind.RiderPrimary && accepted &&
                relationship.State == RelationshipState.Mounted)
            {
                activation.AwaitingCommandTerminal = true;
                activation.ActiveCommandObserved = combat.HasActiveCommand;
                activation.TerminalDeadline = Time.realtimeSinceStartup + 90.0f;
            }
            else
            {
                pendingActivation = null;
            }
            return accepted;
        }

        internal bool BeginSaveSerializationScope()
        {
            if (disposed || serializationSuspended)
            {
                return false;
            }

            serializationSuspended = true;
            CaptureAndClearManagedHotbarSlots();
            RemoveAllManagedFacts(false);
            logger.Info("Native mounted control facts and exact KMC hotbar bindings suspended before save serialization.");
            return true;
        }

        internal IEnumerator<object> WrapSaveRoutine(IEnumerator<object> inner)
        {
            if (inner == null)
            {
                EndSaveSerializationScope();
                yield break;
            }

            try
            {
                while (inner.MoveNext())
                {
                    yield return inner.Current;
                }
            }
            finally
            {
                var disposable = inner as IDisposable;
                disposable?.Dispose();
                EndSaveSerializationScope();
            }
        }

        internal NativeMountedControlSnapshot CaptureSnapshot()
        {
            var units = CollectCandidateUnits();
            var exactCount = 0;
            var duplicateCount = 0;
            var slotCount = 0;
            foreach (var unit in units)
            {
                foreach (var blueprint in EnumerateBlueprints())
                {
                    var count = CountFacts(unit, blueprint);
                    exactCount += count;
                    duplicateCount += Math.Max(0, count - 1);
                }
                slotCount += CountManagedHotbarSlots(unit);
            }

            var activations = activationLedger.Snapshot();
            return new NativeMountedControlSnapshot
            {
                Registered = registered,
                Enabled = enabled,
                SerializationSuspended = serializationSuspended,
                ExactFactCount = exactCount,
                DuplicateFactCount = duplicateCount,
                ManagedHotbarSlotCount = slotCount,
                TargetSelectionStartCount = TargetSelectionStartCount,
                TargetSelectionEndCount = TargetSelectionEndCount,
                NativeCastRequestCount = NativeCastRequestCount,
                NativeRefusalCount = NativeRefusalCount,
                DispatchAcceptedCount = DispatchAcceptedCount,
                DispatchRejectedCount = DispatchRejectedCount,
                NativePrimaryShellPrepareCount = NativePrimaryShellPrepareCount,
                LastNativePrimaryShellObservation = LastNativePrimaryShellObservation,
                ActivationRecordCount = activations.Count,
                RiderPrimaryRelationshipEndCount = activations.Count(record =>
                    record.Kind == NativeMountedControlKind.RiderPrimary && record.RelationshipEnded)
            };
        }

        internal IReadOnlyList<NativeMountedAbilityActivationRecord> SnapshotAbilityActivations()
        {
            return activationLedger.Snapshot();
        }

        public void HandleAbilityTargetSelectionStart(AbilityData ability)
        {
            var kind = ResolveKind(ability?.Blueprint);
            if (kind == NativeMountedControlKind.None)
            {
                return;
            }
            TargetSelectionStartCount++;
            targetSelectionMode = true;
            pendingActivation = StartActivation(kind, ability?.Caster?.Unit);
            RecordActivation(
                pendingActivation,
                NativeMountedAbilityActivationPhase.TargetSelectionStarted,
                ability?.Caster?.Unit,
                null,
                null,
                "target-selection-started");
            logger.Info("Native mounted target selection started: kind=" + kind +
                "; casterId=" + (ability?.Caster?.Unit?.UniqueId ?? "<none>") +
                "; frame=" + Time.frameCount + ".");
        }

        public void HandleAbilityTargetSelectionEnd(AbilityData ability)
        {
            var kind = ResolveKind(ability?.Blueprint);
            if (kind == NativeMountedControlKind.None)
            {
                return;
            }
            TargetSelectionEndCount++;
            var activation = EnsureActivation(kind, ability?.Caster?.Unit);
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.TargetSelectionEnded,
                ability?.Caster?.Unit,
                null,
                null,
                activation.CastRequested ? "selection-ended-after-cast" : "target-selection-cancelled");
            targetSelectionMode = false;
            if (!activation.CastRequested)
            {
                pendingActivation = null;
            }
            logger.Info("Native mounted target selection ended: kind=" + kind +
                "; frame=" + Time.frameCount + ".");
        }

        public void OnCastRequested(AbilityData ability, TargetWrapper target)
        {
            var kind = ResolveKind(ability?.Blueprint);
            if (kind == NativeMountedControlKind.None)
            {
                return;
            }
            NativeCastRequestCount++;
            var activation = EnsureActivation(kind, ability?.Caster?.Unit);
            activation.CastRequested = true;
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.CastRequested,
                ability?.Caster?.Unit,
                target?.Unit,
                null,
                "native-cast-requested");
            logger.Info("Native mounted cast requested: kind=" + kind +
                "; casterId=" + (ability?.Caster?.Unit?.UniqueId ?? "<none>") +
                "; targetId=" + (target?.Unit?.UniqueId ?? "<none>") +
                "; frame=" + Time.frameCount + ".");
        }

        public void OnAbilityCastRefused(AbilityData ability, TargetWrapper target)
        {
            var kind = ResolveKind(ability?.Blueprint);
            if (kind == NativeMountedControlKind.None)
            {
                return;
            }
            NativeRefusalCount++;
            var activation = EnsureActivation(kind, ability?.Caster?.Unit);
            var reason = DescribeRefusal(kind, ability?.Caster?.Unit, target?.Unit);
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.CastRefused,
                ability?.Caster?.Unit,
                target?.Unit,
                false,
                reason);
            pendingActivation = null;
            RaiseWarning(reason);
            logger.Info("Native mounted target rejected: kind=" + kind +
                "; casterId=" + (ability?.Caster?.Unit?.UniqueId ?? "<none>") +
                "; targetId=" + (target?.Unit?.UniqueId ?? "<none>") +
                "; reason=" + reason + ".");
        }

        public void OnMoveRequested(Vector3 target)
        {
            if (combat.HasStockAttackIntent)
            {
                combat.CancelSelectedInput("native move request replaced stock attack intent");
            }
        }

        public void OnItemUseRequested(Kingmaker.Items.ItemEntity item, TargetWrapper target)
        {
            if (combat.HasStockAttackIntent)
            {
                combat.CancelSelectedInput("native item-use request replaced stock attack intent");
            }
        }

        public void OnAttackRequested(UnitEntityData unit, Kingmaker.View.UnitEntityView target)
        {
            combat.ObserveStockAttackRequested(unit, target?.EntityData);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            RemoveAllManagedFacts(true);
            ClearSerializationHotbarLeases(false);
            EnsureUnsubscribed();
            RemoveRegisteredBlueprints();
            RemoveLocalization();
            if (ReferenceEquals(NativeMountedAbilityBridge.Service, this))
            {
                NativeMountedAbilityBridge.Service = null;
            }
            disposed = true;
        }

        private void TryRegister()
        {
            var library = ResourcesLibrary.LibraryObject;
            if (library == null || library.BlueprintsByAssetId == null || library.BlueprintsByAssetId.Count == 0 ||
                horseCompanion.HorseFeature == null || !EnsureLocalization())
            {
                return;
            }

            blueprintList = library.GetAllBlueprints();
            if (blueprintList == null)
            {
                throw new InvalidOperationException("The initialized blueprint library has no canonical list.");
            }

            foreach (var guid in new[]
            {
                MountAbilityGuid,
                DismountAbilityGuid,
                RiderPrimaryAbilityGuid,
                MountPrimaryAbilityGuid
            })
            {
                AssertGuidAbsent(library, guid);
            }

            var horseIcon = horseCompanion.HorseFeature.Icon;
            var saddleIcon = horseCompanion.MountSaddleIcon;
            if (horseIcon == null)
            {
                throw new InvalidOperationException("The exact Horse feature has no icon for native controls.");
            }
            if (saddleIcon == null || horseCompanion.DismountSaddleIcon == null)
            {
                throw new InvalidOperationException("The original KMC saddle icon is unavailable for Mount/Dismount controls.");
            }

            mountAbility = CreateAbility(
                "KMC_MountCompanionAbility",
                MountAbilityGuid,
                "KMC.Native.Mount.Name",
                "KMC.Native.Mount.Description",
                NativeMountedControlKind.MountCompanion,
                AbilityRange.Unlimited,
                false,
                true,
                false,
                UnitCommand.CommandType.Move,
                saddleIcon);
            dismountAbility = CreateAbility(
                "KMC_DismountAbility",
                DismountAbilityGuid,
                "KMC.Native.Dismount.Name",
                "KMC.Native.Dismount.Description",
                NativeMountedControlKind.Dismount,
                AbilityRange.Personal,
                false,
                false,
                true,
                UnitCommand.CommandType.Move,
                horseCompanion.DismountSaddleIcon);
            riderPrimaryAbility = CreateAbility(
                "KMC_RiderPrimaryAbility",
                RiderPrimaryAbilityGuid,
                "KMC.Native.RiderPrimary.Name",
                "KMC.Native.RiderPrimary.Description",
                NativeMountedControlKind.RiderPrimary,
                AbilityRange.Unlimited,
                true,
                false,
                false,
                UnitCommand.CommandType.Free,
                horseIcon);
            mountPrimaryAbility = CreateAbility(
                "KMC_MountPrimaryAbility",
                MountPrimaryAbilityGuid,
                "KMC.Native.MountPrimary.Name",
                "KMC.Native.MountPrimary.Description",
                NativeMountedControlKind.MountPrimary,
                AbilityRange.Unlimited,
                true,
                false,
                false,
                UnitCommand.CommandType.Free,
                horseIcon);

            foreach (var ability in EnumerateBlueprints())
            {
                library.BlueprintsByAssetId.Add(ability.AssetGuid, ability);
                blueprintList.Add(ability);
            }
            registered = true;
            logger.Info("Registered four original KMC native mounted-control abilities with action-bar autofill disabled.");
        }

        private BlueprintAbility CreateAbility(
            string objectName,
            string guid,
            string nameKey,
            string descriptionKey,
            NativeMountedControlKind kind,
            AbilityRange range,
            bool targetEnemies,
            bool targetFriends,
            bool targetSelf,
            UnitCommand.CommandType actionType,
            Sprite icon)
        {
            var ability = CreateOwned<BlueprintAbility>(objectName);
            ability.AssetGuid = guid;
            ability.Type = AbilityType.Extraordinary;
            ability.Range = range;
            ability.CanTargetPoint = false;
            ability.CanTargetEnemies = targetEnemies;
            ability.CanTargetFriends = targetFriends;
            ability.CanTargetSelf = targetSelf;
            ability.SpellResistance = false;
            ability.LocalizedDuration = NewLocalizedString("KMC.Native.Empty");
            ability.LocalizedSavingThrow = NewLocalizedString("KMC.Native.Empty");
            ability.ActionBarAutoFillIgnored = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.ActionType = actionType;
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;
            ability.HasFastAnimation = true;
            ability.DisableLog = false;
            ability.ResourceAssetIds = new string[0];
            DisplayNameField.SetValue(ability, NewLocalizedString(nameKey));
            DescriptionField.SetValue(ability, NewLocalizedString(descriptionKey));
            IconField.SetValue(ability, icon);
            var logic = CreateOwned<NativeMountedAbilityLogic>(objectName + "_Logic");
            logic.Kind = kind;
            ability.ComponentsArray = new BlueprintComponent[] { logic };
            return ability;
        }

        internal bool IsPlayerOnlyRelationshipControl(AbilityData ability)
        {
            return ability?.Blueprint != null &&
                (ReferenceEquals(ability.Blueprint, mountAbility) || ReferenceEquals(ability.Blueprint, dismountAbility));
        }

        private void ReconcileFacts()
        {
            var candidates = CollectCurrentCandidateUnits();
            foreach (var unit in candidates)
            {
                RememberUnit(unit);
                foreach (var ability in EnumerateBlueprints())
                {
                    var kind = ResolveKind(ability);
                    var desired = ShouldLease(unit, kind);
                    var facts = unit?.Descriptor?.Abilities?.Enumerable
                        .Where(item => ReferenceEquals(item.Blueprint, ability)).ToList() ??
                        new List<Ability>();
                    if (desired && facts.Count == 1 && facts[0].Active)
                    {
                        continue;
                    }
                    foreach (var fact in facts)
                    {
                        unit.Descriptor.Abilities.RemoveFact(fact);
                    }
                    if (!desired)
                    {
                        continue;
                    }
                    var added = unit.Descriptor.Abilities.AddFact(ability, null) as Ability;
                    if (added == null || !added.Active || !ReferenceEquals(added.Blueprint, ability))
                    {
                        throw new InvalidOperationException("The exact native control fact could not be leased: " + ability.name + ".");
                    }
                    logger.Info("Native mounted control fact leased: ability=" + ability.name +
                        "; unitId=" + unit.UniqueId + ".");
                }
            }

            foreach (var prior in observedUnits.ToArray())
            {
                if (prior == null || candidates.Contains(prior))
                {
                    continue;
                }
                RemoveManagedFacts(prior, false);
                observedUnits.Remove(prior);
            }
        }

        private bool ShouldLease(UnitEntityData unit, NativeMountedControlKind kind)
        {
            var mounted = relationship.State == RelationshipState.Mounted;
            var faulted = relationship.State == RelationshipState.Faulted;
            var ownerHasSupportedMount = unit?.Descriptor?.Pet != null &&
                SupportedMountedProfiles.IsSupported(unit.Descriptor.Pet) &&
                unit.Descriptor.Pet.Descriptor?.Master.Value == unit;
            return NativeMountedControlPolicy.ShouldLease(
                kind,
                settings.EnableUnsafeMovementExperiment,
                settings.EnableUnifiedMountedTurn,
                ownerHasSupportedMount,
                mounted,
                faulted,
                unit == relationship.Rider,
                unit == relationship.Mount);
        }

        private List<UnitEntityData> CollectCurrentCandidateUnits()
        {
            var result = new List<UnitEntityData>();
            var player = Game.Instance?.Player;
            if (player?.PartyCharacters != null)
            {
                foreach (var reference in player.PartyCharacters)
                {
                    var unit = reference.Value;
                    AddUnique(result, unit);
                    AddUnique(result, unit?.Descriptor?.Pet);
                }
            }
            AddUnique(result, relationship.Rider);
            AddUnique(result, relationship.Mount);
            return result;
        }

        private List<UnitEntityData> CollectCandidateUnits()
        {
            var result = CollectCurrentCandidateUnits();
            foreach (var unit in observedUnits)
            {
                AddUnique(result, unit);
            }
            return result;
        }

        private void RemoveAllManagedFacts(bool cleanupSlots)
        {
            CancelPendingNativeControls(CleanupTrigger.ModDisabled);
            foreach (var unit in CollectCandidateUnits())
            {
                RemoveManagedFacts(unit, cleanupSlots);
            }
            if (cleanupSlots)
            {
                observedUnits.Clear();
            }
        }

        private void CancelPendingNativeControls(CleanupTrigger trigger)
        {
            foreach (var unit in CollectCandidateUnits())
            {
                if (unit?.Commands == null) { continue; }
                var commands = unit.Commands.Raw.Concat(unit.Commands.Queue).OfType<UnitUseAbility>().Distinct().ToArray();
                foreach (var command in commands)
                {
                    if (NativeMountedControlPolicy.OwnsPendingControl(ResolveKind(command.Spell?.Blueprint),
                        command.Executor == unit && command.Spell?.Caster?.Unit == unit, command.IsStarted, command.IsFinished))
                        command.Interrupt();
                }
            }
        }

        private void RemoveManagedFacts(UnitEntityData unit, bool cleanupSlots)
        {
            var abilities = unit?.Descriptor?.Abilities;
            if (abilities == null)
            {
                return;
            }
            foreach (var blueprint in EnumerateBlueprints())
            {
                foreach (var fact in abilities.Enumerable
                    .Where(item => ReferenceEquals(item.Blueprint, blueprint)).ToList())
                {
                    abilities.RemoveFact(fact);
                }
            }
            if (cleanupSlots)
            {
                ClearManagedHotbarSlots(unit);
            }
        }

        private void CaptureAndClearManagedHotbarSlots()
        {
            hotbarSerializationLeases.Clear();
            foreach (var unit in CollectCandidateUnits())
            {
                var slots = unit?.UISettings?.Slots;
                if (slots == null)
                {
                    continue;
                }
                for (var index = 0; index < slots.Length; index++)
                {
                    var abilitySlot = slots[index] as MechanicActionBarSlotAbility;
                    var blueprint = abilitySlot?.Ability?.Blueprint;
                    if (ResolveKind(blueprint) == NativeMountedControlKind.None)
                    {
                        continue;
                    }
                    var placeholder = new MechanicActionBarSlotEmpty { Unit = unit };
                    hotbarSerializationLeases.Add(new HotbarSerializationLease(unit, index, blueprint, placeholder));
                    unit.UISettings.SetSlot(placeholder, index);
                }
            }
        }

        private void EndSaveSerializationScope()
        {
            if (!serializationSuspended)
            {
                return;
            }
            serializationSuspended = false;
            if (enabled && !disposed)
            {
                Update();
            }
            ClearSerializationHotbarLeases(true);
            logger.Info("Native mounted control save-serialization suspension ended; current runtime facts were rebuilt without saved residue.");
        }

        private void ClearSerializationHotbarLeases(bool restore)
        {
            foreach (var lease in hotbarSerializationLeases)
            {
                var slots = lease.Unit?.UISettings?.Slots;
                if (slots == null || lease.Index < 0 || lease.Index >= slots.Length ||
                    !ReferenceEquals(slots[lease.Index], lease.Placeholder))
                {
                    continue;
                }
                var fact = lease.Unit.Descriptor?.Abilities?.GetAbility(lease.Blueprint);
                if (restore && fact != null && fact.Active)
                {
                    lease.Unit.UISettings.SetSlot(
                        new MechanicActionBarSlotAbility { Unit = lease.Unit, Ability = fact.Data },
                        lease.Index);
                }
            }
            hotbarSerializationLeases.Clear();
        }

        private void ClearManagedHotbarSlots(UnitEntityData unit)
        {
            var slots = unit?.UISettings?.Slots;
            if (slots == null)
            {
                return;
            }
            for (var index = 0; index < slots.Length; index++)
            {
                var ability = (slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint;
                if (ResolveKind(ability) != NativeMountedControlKind.None)
                {
                    unit.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, index);
                }
            }
        }

        private int CountManagedHotbarSlots(UnitEntityData unit)
        {
            var slots = unit?.UISettings?.Slots;
            return slots == null
                ? 0
                : slots.Count(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint) !=
                    NativeMountedControlKind.None);
        }

        private PendingNativeActivation StartActivation(
            NativeMountedControlKind kind,
            UnitEntityData caster)
        {
            var deliveries = lifecycleLedger.Snapshot();
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            return new PendingNativeActivation
            {
                Id = activationLedger.BeginActivation(),
                Kind = kind,
                StateAtStart = relationship.State,
                RiderIdAtStart = rider?.UniqueId,
                MountIdAtStart = mount?.UniqueId,
                RiderViewAtStart = ViewIdentity(rider),
                MountViewAtStart = ViewIdentity(mount),
                LifecycleSequenceAtStart = deliveries.Count == 0 ? 0 : deliveries[deliveries.Count - 1].Sequence,
                TransitionAtStart = relationship.LastTransition,
                CasterId = caster?.UniqueId
            };
        }

        private PendingNativeActivation EnsureActivation(
            NativeMountedControlKind kind,
            UnitEntityData caster)
        {
            if (pendingActivation == null || pendingActivation.Kind != kind)
            {
                pendingActivation = StartActivation(kind, caster);
            }
            return pendingActivation;
        }

        private void ObservePendingRiderPrimaryOutcome()
        {
            var activation = pendingActivation;
            if (disposed || activation == null ||
                activation.Kind != NativeMountedControlKind.RiderPrimary ||
                !activation.AwaitingCommandTerminal)
            {
                return;
            }

            if (relationship.State != RelationshipState.Mounted)
            {
                RecordActivation(
                    activation,
                    NativeMountedAbilityActivationPhase.RelationshipEnded,
                    relationship.Rider,
                    null,
                    true,
                    "relationship-ended-before-primary-terminal");
                pendingActivation = null;
                return;
            }

            if (combat.HasActiveCommand)
            {
                activation.ActiveCommandObserved = true;
                return;
            }

            if (!activation.ActiveCommandObserved && Time.realtimeSinceStartup < activation.TerminalDeadline)
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var terminal = outcome == null
                ? "no-active-command-observed"
                : (outcome.Result ?? "<no-result>") + ":" + (outcome.TerminalReason ?? "<no-reason>");
            RecordActivation(
                activation,
                NativeMountedAbilityActivationPhase.CommandTerminal,
                relationship.Rider,
                null,
                true,
                terminal);
            pendingActivation = null;
        }

        private void RecordActivation(
            PendingNativeActivation activation,
            NativeMountedAbilityActivationPhase phase,
            UnitEntityData caster,
            UnitEntityData target,
            bool? dispatchAccepted,
            string terminalResult)
        {
            if (activation == null)
            {
                return;
            }

            var deliveries = lifecycleLedger.Snapshot();
            var newDeliveries = deliveries.Where(delivery =>
                delivery.Sequence > activation.LifecycleSequenceAtStart).ToArray();
            var lastDelivery = newDeliveries.LastOrDefault(delivery => delivery.CleanupAttempted) ??
                newDeliveries.LastOrDefault();
            var transition = relationship.LastTransition;
            var transitionChanged = !ReferenceEquals(activation.TransitionAtStart, transition);
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var riderView = ViewIdentity(rider);
            var mountView = ViewIdentity(mount);
            var relationshipEnded = activation.StateAtStart == RelationshipState.Mounted &&
                relationship.State != RelationshipState.Mounted;
            var cleanupTrigger = lastDelivery?.CleanupTrigger;
            if (!cleanupTrigger.HasValue && relationshipEnded && transitionChanged)
            {
                cleanupTrigger = transition?.Trigger;
            }
            var selected = SelectionManager.Instance?.SelectedUnits;
            var selectedIds = selected == null || selected.Count == 0
                ? "<none>"
                : string.Join(",", selected.Select(unit => unit?.UniqueId ?? "<null>").ToArray());
            var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var record = new NativeMountedAbilityActivationRecord
            {
                ActivationId = activation.Id,
                Phase = phase,
                Kind = activation.Kind,
                AbilityGuid = AbilityGuid(activation.Kind),
                Frame = Time.frameCount,
                CasterId = caster?.UniqueId ?? activation.CasterId ?? "<none>",
                ActiveSelectedUnitIds = selectedIds,
                TargetId = target?.UniqueId ?? "<none>",
                TargetSelectionMode = targetSelectionMode,
                RelationshipStateAtStart = activation.StateAtStart,
                RelationshipStateObserved = relationship.State,
                RiderIdAtStart = activation.RiderIdAtStart ?? "<none>",
                MountIdAtStart = activation.MountIdAtStart ?? "<none>",
                RiderViewAtStart = activation.RiderViewAtStart,
                MountViewAtStart = activation.MountViewAtStart,
                RiderViewObserved = riderView,
                MountViewObserved = mountView,
                RiderViewChanged = !string.Equals(activation.RiderViewAtStart, riderView, StringComparison.Ordinal),
                MountViewChanged = !string.Equals(activation.MountViewAtStart, mountView, StringComparison.Ordinal),
                InCombat = rider?.IsInCombat == true || mount?.IsInCombat == true,
                TurnBased = CombatController.IsInTurnBasedCombat(),
                GameMode = Game.Instance == null ? "<none>" : Game.Instance.CurrentMode.ToString(),
                CurrentTurnUnitId = currentTurn?.Unit?.UniqueId ?? "<none>",
                LifecycleSequenceAtStart = activation.LifecycleSequenceAtStart,
                LifecycleSequenceObserved = deliveries.Count == 0 ? 0 : deliveries[deliveries.Count - 1].Sequence,
                LifecycleDeliveries = newDeliveries.Length == 0
                    ? "<none>"
                    : string.Join("|", newDeliveries.Select(item =>
                        item.Sequence + ":" + item.Boundary + ":" + item.Source +
                        (item.CleanupTrigger.HasValue ? ":cleanup=" + item.CleanupTrigger.Value : string.Empty)).ToArray()),
                CleanupTrigger = cleanupTrigger,
                DispatchAccepted = dispatchAccepted,
                RelationshipEnded = relationshipEnded,
                RelationshipTransitionChanged = transitionChanged,
                RelationshipTransitionResult = transition == null
                    ? "<none>"
                    : "succeeded=" + transition.Succeeded + ";state=" + transition.State +
                        ";trigger=" + (transition.Trigger.HasValue ? transition.Trigger.Value.ToString() : "<none>"),
                TerminalResult = terminalResult ?? string.Empty
            };
            activationLedger.Record(record);
            logger.Info("Native mounted ability activation: activationId=" + record.ActivationId +
                "; phase=" + record.Phase +
                "; kind=" + record.Kind +
                "; casterId=" + record.CasterId +
                "; selected=" + record.ActiveSelectedUnitIds +
                "; targetId=" + record.TargetId +
                "; targetSelection=" + record.TargetSelectionMode +
                "; state=" + record.RelationshipStateAtStart + "->" + record.RelationshipStateObserved +
                "; lifecycle=" + record.LifecycleDeliveries +
                "; cleanup=" + (record.CleanupTrigger.HasValue ? record.CleanupTrigger.Value.ToString() : "<none>") +
                "; ended=" + record.RelationshipEnded +
                "; terminal=" + record.TerminalResult + ".");
        }

        private static string AbilityGuid(NativeMountedControlKind kind)
        {
            switch (kind)
            {
                case NativeMountedControlKind.MountCompanion: return MountAbilityGuid;
                case NativeMountedControlKind.Dismount: return DismountAbilityGuid;
                case NativeMountedControlKind.RiderPrimary: return RiderPrimaryAbilityGuid;
                case NativeMountedControlKind.MountPrimary: return MountPrimaryAbilityGuid;
                default: return "<none>";
            }
        }

        private static string ViewIdentity(UnitEntityData unit)
        {
            return unit?.View == null ? "<none>" : unit.View.GetInstanceID().ToString();
        }

        private int CountFacts(UnitEntityData unit, BlueprintAbility blueprint)
        {
            return unit?.Descriptor?.Abilities?.Enumerable.Count(
                item => ReferenceEquals(item.Blueprint, blueprint)) ?? 0;
        }

        private string DescribeRefusal(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            UnitEntityData target)
        {
            var availability = Evaluate(kind, caster);
            if (!availability.IsEnabled)
            {
                return availability.Reason;
            }
            if (kind == NativeMountedControlKind.MountCompanion)
            {
                return playerAction.DescribeNativeMountTargetRejection(caster, target);
            }
            if (kind == NativeMountedControlKind.RiderPrimary)
            {
                return combat.DescribeNativeAbilityTargetRejection(
                    combat.ResolveRiderPrimaryAction(), caster, target);
            }
            if (kind == NativeMountedControlKind.MountPrimary)
            {
                return combat.DescribeNativeAbilityTargetRejection(
                    MountedCombatActionKind.MountPrimaryNatural, caster, target);
            }
            return "The mounted control target is no longer valid.";
        }

        private void RaiseWarning(string reason)
        {
            var message = string.IsNullOrWhiteSpace(reason)
                ? "The mounted control request was rejected."
                : reason;
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler => handler.HandleWarning(message, true));
        }

        private NativeMountedControlKind ResolveKind(BlueprintAbility ability)
        {
            if (ReferenceEquals(ability, mountAbility)) { return NativeMountedControlKind.MountCompanion; }
            if (ReferenceEquals(ability, dismountAbility)) { return NativeMountedControlKind.Dismount; }
            if (ReferenceEquals(ability, riderPrimaryAbility)) { return NativeMountedControlKind.RiderPrimary; }
            if (ReferenceEquals(ability, mountPrimaryAbility)) { return NativeMountedControlKind.MountPrimary; }
            return NativeMountedControlKind.None;
        }

        private IEnumerable<BlueprintAbility> EnumerateBlueprints()
        {
            if (mountAbility != null) { yield return mountAbility; }
            if (dismountAbility != null) { yield return dismountAbility; }
            if (riderPrimaryAbility != null) { yield return riderPrimaryAbility; }
            if (mountPrimaryAbility != null) { yield return mountPrimaryAbility; }
        }

        private void RemoveRegisteredBlueprints()
        {
            var library = ResourcesLibrary.LibraryObject;
            foreach (var ability in EnumerateBlueprints().ToArray())
            {
                BlueprintScriptableObject current;
                if (library?.BlueprintsByAssetId != null &&
                    library.BlueprintsByAssetId.TryGetValue(ability.AssetGuid, out current) &&
                    ReferenceEquals(current, ability))
                {
                    library.BlueprintsByAssetId.Remove(ability.AssetGuid);
                }
                blueprintList?.Remove(ability);
            }
            for (var index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null)
                {
                    UnityEngine.Object.Destroy(ownedObjects[index]);
                }
            }
            ownedObjects.Clear();
            registered = false;
        }

        private bool EnsureLocalization()
        {
            var pack = LocalizationManager.CurrentPack;
            if (pack?.Strings == null)
            {
                return false;
            }
            foreach (var entry in localization)
            {
                string current;
                if (pack.Strings.TryGetValue(entry.Key, out current))
                {
                    if (!string.Equals(current, entry.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("A localization key collision exists for " + entry.Key + ".");
                    }
                }
                else
                {
                    pack.Strings.Add(entry.Key, entry.Value);
                }
            }
            return true;
        }

        private void RemoveLocalization()
        {
            var strings = LocalizationManager.CurrentPack?.Strings;
            if (strings == null)
            {
                return;
            }
            foreach (var entry in localization)
            {
                string current;
                if (strings.TryGetValue(entry.Key, out current) &&
                    string.Equals(current, entry.Value, StringComparison.Ordinal))
                {
                    strings.Remove(entry.Key);
                }
            }
        }

        private LocalizedString NewLocalizedString(string key)
        {
            var value = new LocalizedString();
            LocalizedStringKeyField.SetValue(value, key);
            return value;
        }

        private T CreateOwned<T>(string name) where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            value.name = name;
            value.hideFlags = HideFlags.HideAndDontSave;
            ownedObjects.Add(value);
            return value;
        }

        private void AssertGuidAbsent(LibraryScriptableObject library, string guid)
        {
            if (library.BlueprintsByAssetId.ContainsKey(guid) ||
                blueprintList.Any(item => item != null && string.Equals(item.AssetGuid, guid, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Reserved native mounted-control GUID collision: " + guid + ".");
            }
        }

        private void EnsureSubscribed()
        {
            if (!subscribed)
            {
                EventBus.Subscribe(this);
                relationship.Dismounting += CancelPendingNativeControls;
                subscribed = true;
            }
        }

        private void EnsureUnsubscribed()
        {
            if (subscribed)
            {
                EventBus.Unsubscribe(this);
                relationship.Dismounting -= CancelPendingNativeControls;
                subscribed = false;
            }
        }

        private void RememberUnit(UnitEntityData unit)
        {
            if (unit != null && !observedUnits.Contains(unit))
            {
                observedUnits.Add(unit);
            }
        }

        private static void AddUnique(List<UnitEntityData> units, UnitEntityData unit)
        {
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
            }
        }

        private static FieldInfo ResolveField(Type declaringType, string name, int token, Type fieldType)
        {
            var field = declaringType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.MetadataToken != token || field.FieldType != fieldType)
            {
                throw new MissingFieldException(declaringType.FullName, name + " exact token " + token.ToString("X8"));
            }
            return field;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NativeMountedControlService));
            }
        }

        private sealed class HotbarSerializationLease
        {
            public HotbarSerializationLease(
                UnitEntityData unit,
                int index,
                BlueprintAbility blueprint,
                MechanicActionBarSlotEmpty placeholder)
            {
                Unit = unit;
                Index = index;
                Blueprint = blueprint;
                Placeholder = placeholder;
            }

            public UnitEntityData Unit { get; }
            public int Index { get; }
            public BlueprintAbility Blueprint { get; }
            public MechanicActionBarSlotEmpty Placeholder { get; }
        }

        private sealed class PendingNativeActivation
        {
            public long Id { get; set; }
            public NativeMountedControlKind Kind { get; set; }
            public RelationshipState StateAtStart { get; set; }
            public string RiderIdAtStart { get; set; }
            public string MountIdAtStart { get; set; }
            public string RiderViewAtStart { get; set; }
            public string MountViewAtStart { get; set; }
            public long LifecycleSequenceAtStart { get; set; }
            public TransitionResult TransitionAtStart { get; set; }
            public string CasterId { get; set; }
            public bool CastRequested { get; set; }
            public bool AwaitingCommandTerminal { get; set; }
            public bool ActiveCommandObserved { get; set; }
            public float TerminalDeadline { get; set; }
        }
    }
}
