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

    internal sealed partial class NativeMountedControlService : IDisposable,
        IClickActionHandler,
        IAbilityTargetSelectionUIHandler
    {
        internal const string MountAbilityGuid = "f053faad986631688defa003cd7bda0e";
        internal const string DismountAbilityGuid = "3af2b81f4d72bbb30501fa730fcdf36e";
        internal const string RiderPrimaryAbilityGuid = "27364df661b3c121eabb97a31aa73a83";
        internal const string MountPrimaryAbilityGuid = "f88a50d6fdbebbd709c3e323d2f52f5e";
        internal const string MountedChargeAbilityGuid = "d79eaec224a7a832e738eb81baef9d49";
        // F2 CTRL-02: the one public Mount / Dismount control.
        internal const string MountStateAbilityGuid = "26151090ad4744ea96dbb7e0537a4a7f";

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
                // The hidden delivery shell of the ordinary Charge while mounted: logs and target selection read "Charge".
                { "KMC.Native.MountedCharge.Name", "Charge" },
                { "KMC.Native.MountedCharge.Description", "Charge an enemy in a clear straight line. While mounted, your mount carries the charge and you make the charge attack; this takes a full-round action." },
                { "KMC.Native.MountState.Name", "Mount / Dismount" },
                { "KMC.Native.MountState.Description", "Mount your eligible companion, or dismount if you are already riding. Outside combat this is free. In combat it costs a Move action." },
                { "KMC.Native.Mount.Description", "Select your active Horse or Mammoth. Walk into reach and mount. Outside combat this is free; during an encounter it costs one Move action, taken by the ability's own move order. You can queue this order while gameplay is paused; it executes after unpausing." },
                { "KMC.Native.Dismount.Name", "Dismount" },
                { "KMC.Native.Dismount.Description", "Get off your companion. Outside combat this is free; during an encounter it costs one Move action, taken by the ability's own move order." },
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
        private BlueprintAbility mountedChargeAbility;
        private BlueprintAbility mountStateAbility;
        private LocalizationPack localizedPack;
        // CTRL-03: actors that had their one automatic placement, placed or migrated a control, or removed it. Belongs to
        // the loaded world; saved as archive placement records and cleared when a new load begins.
        private readonly HashSet<string> placementRecorded = new HashSet<string>(StringComparer.Ordinal);
        // The player's chosen slot of each rider's public control, kept while its fact is temporarily absent (area
        // change, save scope, capital reserve roster, out of the game). Actor ids and indices only; belongs to the loaded
        // world like the placement records, cleared on a new load, disable and disposal.
        private readonly MountedControlPlacementIntent placementIntent = new MountedControlPlacementIntent();
        // From a selected load's start until native loading replaces the player object, the outgoing world writes nothing.
        private readonly MountedControlLoadFence loadFence = new MountedControlLoadFence();
        private bool ArrangementsAccepted => loadFence.Accepts(Game.Instance?.Player);
        private bool restoringSlots;
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

        internal BlueprintAbility MountedChargeAbility => mountedChargeAbility;

        internal BlueprintAbility RiderPrimaryAbility => riderPrimaryAbility;

        internal BlueprintAbility MountPrimaryAbility => mountPrimaryAbility;

        internal BlueprintAbility MountStateAbility => mountStateAbility;

        // Set by persistence: false while a selected load still has slot bindings to restore. Null means no persistence.
        internal Func<bool> PlacementSettled { get; set; }

        // Set by the composition root: true while the prepare-to-remove cleanup save is written, so that archive records
        // no control slot or placement row. Native serialization already blanks the slots themselves.
        internal Func<bool> PersistentRowsSuppressed { get; set; }

        internal long InitialPlacementCount { get; private set; }

        internal long InitialPlacementFullBarCount { get; private set; }

        internal long LegacySlotRepairCount { get; private set; }

        // Public controls returned to the player's chosen slot after a temporary absence.
        internal long PlacementIntentRestoreCount { get; private set; }

        internal int PlacementIntentCount => placementIntent.Count;

        internal long PublicControlCommandRefusedCount { get; private set; }

        internal bool IsPlacementRecorded(UnitEntityData unit) => unit?.UniqueId != null && placementRecorded.Contains(unit.UniqueId);

        // What each automatic placement saw: the main bar before it, and the slot it chose (-1: full). Observation only.
        internal sealed class InitialPlacementObservation
        {
            internal string UnitId;
            internal int Index;
            internal int Frame;
            internal string[] MainBarBefore;
        }

        private readonly List<InitialPlacementObservation> initialPlacements = new List<InitialPlacementObservation>();

        internal IReadOnlyList<InitialPlacementObservation> InitialPlacements => initialPlacements;

        internal bool SerializationSuspended => serializationSuspended;

        // Mounted Combat's services are enabled (CompositionRoot.SetEnabled drives SetEnabled) and not disposed.
        internal bool IsActive => enabled && !disposed;

        // ATTACK-01: Rider Primary and Mount Primary are retired from gameplay. Their definitions stay registered so an
        // older reference resolves; the facts are leased only for a runtime scenario that explicitly selected them.
        internal bool RetiredPrimaryHelpersSelected { get; set; }

        internal long TargetSelectionStartCount { get; private set; }

        internal long TargetSelectionEndCount { get; private set; }

        internal long NativeCastRequestCount { get; private set; }

        internal long NativeRefusalCount { get; private set; }

        internal long DispatchAcceptedCount { get; private set; }

        internal long DispatchRejectedCount { get; private set; }

        internal long NativePrimaryShellPrepareCount { get; private set; }

        internal string LastNativePrimaryShellObservation { get; private set; } = "not-observed";

        // One exact record per native relationship shell, taken at the command's
        // own Init boundary. It binds the transition that shell may later deliver
        // to the caster and the relationship generation it was created against, so
        // a stale shell cannot transition a relationship it never targeted and a
        // repeated delivery of the same shell is recognized as the same control.
        private sealed class NativeRelationshipShell
        {
            // The live native context may outlast its command slot. Its CWT value
            // retains the exact command until context custody itself is released.
            internal readonly UnitUseAbility Command;
            internal NativeRelationshipShell(UnitUseAbility command)
            { Command = command ?? throw new ArgumentNullException(nameof(command)); }
            public NativeMountedControlKind Kind;
            public long GenerationAtInit;
            public MountedPairRequest<UnitEntityData, GameMountedRelationshipService> PairRequest;
            public string CasterId;
            public string TargetId;
            public string ControlIdentity;
            // Bound to the exact AbilityExecutionContext this command created at its
            // own OnAction boundary. That binding, not the caster's current Move
            // slot, is what survives to Deliver.
            public bool ProcessBound;
            // Exactly-once: a shell that has already delivered its transition can
            // never deliver a second one.
            public bool Consumed;
            // A shell refused for a permanent reason is retired and can never become
            // valid again, whichever exact binding later presents it.
            public bool Retired;
            public string RetiredReason;
        }

        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<UnitUseAbility, NativeRelationshipShell>
            relationshipShells = new System.Runtime.CompilerServices.ConditionalWeakTable<UnitUseAbility, NativeRelationshipShell>();
        // The exact process binding. Established at OnAction, consumed at Deliver.
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, NativeRelationshipShell>
            relationshipShellContexts = new System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, NativeRelationshipShell>();
        private long relationshipShellSequence;

        // Read-only diagnostic projection; no lookup creates a shell or changes ownership.
        internal RelationshipCommandIdentity CaptureRelationshipCommandIdentity(UnitUseAbility command)
        {
            NativeRelationshipShell shell;
            if (command == null || !relationshipShells.TryGetValue(command, out shell)) return null;
            return new RelationshipCommandIdentity(command, shell.ControlIdentity,
                command.ExecutionProcess, command.ExecutionProcess?.Context, shell.CasterId,
                shell.TargetId, shell.GenerationAtInit, command.Type.ToString(), command.Spell?.Blueprint?.AssetGuid);
        }

        internal bool HasExactRelationshipProcessBinding(UnitUseAbility command)
        {
            NativeRelationshipShell shell, bound;
            var context = command?.ExecutionProcess?.Context;
            return context != null && relationshipShells.TryGetValue(command, out shell) &&
                relationshipShellContexts.TryGetValue(context, out bound) && ReferenceEquals(shell, bound) &&
                shell.ProcessBound && !poisonedContexts.TryGetValue(context, out _);
        }

        internal long NativeRelationshipShellCount { get; private set; }

        internal long NativeRelationshipProcessBindingCount { get; private set; }

        internal string LastNativeRelationshipShellObservation { get; private set; } = "not-observed";

        // A typed per-command lifecycle ledger. Every stage and every early-return
        // reason is recorded with the state that decided it, so a missing shell names
        // the exact failed predicate instead of only reporting a zero count.
        internal enum NativeShellStage
        {
            InitObserved = 0,
            InitRefused = 1,
            Registered = 2,
            ApproachClamped = 3,
            ProcessBound = 4,
            ProcessBindingRefused = 5,
            DeliverResolved = 6,
            DeliverRefused = 7,
            DeliverConsumed = 8,
            ApproachInterrupted = 9
        }

        internal sealed class NativeShellLifecycleRecord
        {
            public long Sequence;
            public NativeShellStage Stage;
            public string ControlIdentity;
            public string Predicate;
            public string State;

            public override string ToString() =>
                Sequence.ToString(CultureInfo.InvariantCulture) + ":" + Stage +
                ";control=" + (ControlIdentity ?? "<none>") +
                ";predicate=" + (Predicate ?? "<none>") +
                ";" + (State ?? string.Empty);
        }

        private const int MaxShellLifecycleRecords = 64;
        private readonly List<NativeShellLifecycleRecord> shellLifecycle = new List<NativeShellLifecycleRecord>();
        private long shellLifecycleSequence;

        internal IReadOnlyList<NativeShellLifecycleRecord> NativeShellLifecycle => shellLifecycle;

        internal string DescribeNativeShellLifecycle()
        {
            if (shellLifecycle.Count == 0) { return "no-shell-lifecycle-records"; }
            var parts = new string[shellLifecycle.Count];
            for (var index = 0; index < shellLifecycle.Count; index++) { parts[index] = shellLifecycle[index].ToString(); }
            return string.Join(" | ", parts);
        }

        // Contexts whose ownership is unresolvable. A poisoned context is permanently
        // terminal: no binding is admitted on it and no delivery resolves through it,
        // whatever the Move slot later contains.
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, string>
            poisonedContexts = new System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, string>();

        internal long PoisonedExecutionContextCount { get; private set; }

        private void PoisonExecutionContext(
            AbilityExecutionContext context, NativeRelationshipShell first, NativeRelationshipShell second)
        {
            if (context == null) { return; }
            string existing;
            if (!poisonedContexts.TryGetValue(context, out existing))
            {
                poisonedContexts.Add(context,
                    "conflict:" + (first?.ControlIdentity ?? "<none>") + "|" + (second?.ControlIdentity ?? "<none>"));
                PoisonedExecutionContextCount++;
            }
            RetireShell(first, "context-conflict");
            RetireShell(second, "context-conflict");
            RecordShellLifecycle(NativeShellStage.ProcessBindingRefused, second?.ControlIdentity,
                "context-conflict-poisoned",
                "firstControl=" + (first?.ControlIdentity ?? "<none>") +
                ";secondControl=" + (second?.ControlIdentity ?? "<none>") +
                ";bothRetired=" + ((first == null || first.Retired) && (second == null || second.Retired)));
        }

        // Permanent refusal. A retired shell can never be resolved again, so a stale or
        // rejected transition cannot be revived by a later binding.
        private void RetireShell(NativeRelationshipShell shell, string reason)
        {
            if (shell == null || shell.Retired) { return; }
            shell.Retired = true;
            shell.RetiredReason = reason;
            NativeRelationshipShellRetiredCount++;
        }

        internal long NativeRelationshipShellRetiredCount { get; private set; }

        private void RecordShellLifecycle(NativeShellStage stage, string controlIdentity, string predicate, string state)
        {
            shellLifecycle.Add(new NativeShellLifecycleRecord
            {
                Sequence = ++shellLifecycleSequence,
                Stage = stage,
                ControlIdentity = controlIdentity,
                Predicate = predicate,
                State = state
            });
            while (shellLifecycle.Count > MaxShellLifecycleRecords) { shellLifecycle.RemoveAt(0); }
        }

        // The complete predicate state PrepareNativeMountApproach decides from. Every
        // early return names which of these failed rather than returning silently.
        private string DescribeInitPredicates(UnitUseAbility command)
        {
            var blueprint = command?.Spell?.Blueprint;
            var caster = command?.Executor;
            var target = command?.Target?.Unit;
            return "disposed=" + disposed + ";serviceEnabled=" + enabled + ";serviceRegistered=" + registered +
                ";serializationSuspended=" + serializationSuspended +
                ";commandPresent=" + (command != null) +
                ";commandStarted=" + (command == null ? "<none>" : command.IsStarted.ToString()) +
                ";commandFinished=" + (command == null ? "<none>" : command.IsFinished.ToString()) +
                ";blueprint=" + (blueprint == null ? "<none>" : blueprint.AssetGuid) +
                ";isMountBlueprint=" + ReferenceEquals(blueprint, mountAbility) +
                ";isDismountBlueprint=" + ReferenceEquals(blueprint, dismountAbility) +
                ";executor=" + (caster?.UniqueId ?? "<none>") +
                ";spellCaster=" + (command?.Spell?.Caster?.Unit?.UniqueId ?? "<none>") +
                ";executorIsSpellCaster=" + (caster != null && command.Spell?.Caster?.Unit == caster) +
                ";casterView=" + (caster?.View != null) +
                ";target=" + (target?.UniqueId ?? "<none>") +
                ";targetView=" + (target?.View != null) +
                ";targetIsCasterPet=" + (caster != null && target != null && target == caster.Descriptor?.Pet) +
                ";targetMasterIsCaster=" + (caster != null && target != null && target.Descriptor?.Master.Value == caster) +
                ";targetProfileSupported=" + (target != null && SupportedMountedProfiles.IsSupported(target)) +
                ";targetIsCaster=" + (caster != null && target == caster) +
                ";generation=" + relationship.MountedPairGeneration;
        }

        // The exact reason the last relationship delivery was refused, and what its
        // Move slot actually held. A shell refusal is raised as a native warning
        // rather than written to the player-action feedback, so without this a
        // refused delivery is invisible to a scenario that only samples feedback.
        internal string LastRelationshipShellRefusal { get; private set; } = "not-observed";

        // Read-only. Publishes what the shell recorded at its own Init boundary so a
        // scenario can prove the binding's identity instead of only its presence. Nothing
        // is registered, retired, consumed or created by asking.
        internal bool TryDescribeRelationshipShell(
            UnitCommand command,
            out NativeMountedControlKind kind,
            out string casterId,
            out string targetId,
            out long generationAtInit,
            out bool processBound)
        {
            kind = NativeMountedControlKind.None;
            casterId = null;
            targetId = null;
            generationAtInit = 0;
            processBound = false;
            var ability = command as UnitUseAbility;
            NativeRelationshipShell shell;
            if (ability == null || !relationshipShells.TryGetValue(ability, out shell) || shell == null)
            {
                return false;
            }
            kind = shell.Kind;
            casterId = shell.CasterId;
            targetId = shell.TargetId;
            generationAtInit = shell.GenerationAtInit;
            processBound = shell.ProcessBound;
            return true;
        }

        internal string DescribeRelationshipShellState(UnitEntityData caster)
        {
            var slot = caster?.Commands?.GetCommand(UnitCommand.CommandType.Move);
            NativeRelationshipShell shell = null;
            var ability = slot as UnitUseAbility;
            if (ability != null) { relationshipShells.TryGetValue(ability, out shell); }
            var mountFact = caster?.Descriptor?.Abilities?.GetAbility(mountAbility);
            return "lastShellRefusal=" + (LastRelationshipShellRefusal ?? "<none>") +
                ";serviceEnabled=" + enabled + ";serviceRegistered=" + registered +
                ";serializationSuspended=" + serializationSuspended +
                ";mountAbilityFactPresent=" + (mountFact != null) +
                ";registeredShells=" + NativeRelationshipShellCount +
                ";moveSlot=" + (slot == null ? "<none>" : slot.GetType().Name) +
                ";moveSlotIsUseAbility=" + (ability != null) +
                ";moveSlotHasShell=" + (shell != null) +
                ";moveSlotStarted=" + (slot == null ? "<none>" : slot.IsStarted.ToString()) +
                ";moveSlotFinished=" + (slot == null ? "<none>" : slot.IsFinished.ToString()) +
                ";moveSlotProcessPresent=" + (ability?.ExecutionProcess != null) +
                ";processBindings=" + NativeRelationshipProcessBindingCount +
                ";lastShellRegistration=" + (LastNativeRelationshipShellObservation ?? "<none>") +
                ";lifecycle={" + DescribeNativeShellLifecycle() + "}";
        }

        internal void PrepareNativeMountApproach(UnitUseAbility command)
        {
            // The blueprint test comes FIRST so the lifecycle ledger only ever records
            // relationship abilities. Ordinary gameplay casts thousands of other
            // abilities through this same Init postfix, and none of them may churn a
            // diagnostic ledger.
            // not-a-refusal: an unrelated ability is none of this service's business.
            var blueprint = command?.Spell?.Blueprint;
            var isMount = ReferenceEquals(blueprint, mountAbility);
            var isDismount = ReferenceEquals(blueprint, dismountAbility);
            if (!isMount && !isDismount) { return; }

            // From here every early return names the exact predicate that refused it. A
            // missing shell must never be diagnosable only as registeredShells=0.
            if (disposed || !enabled || !registered || serializationSuspended)
            {
                RecordShellLifecycle(NativeShellStage.InitRefused, null, "service-state", DescribeInitPredicates(command));
                return;
            }
            if (command.IsStarted || command.IsFinished)
            {
                RecordShellLifecycle(NativeShellStage.InitRefused, null, "command-state", DescribeInitPredicates(command));
                return;
            }
            RecordShellLifecycle(NativeShellStage.InitObserved, null, "relationship-blueprint", DescribeInitPredicates(command));
            var caster = command.Executor;
            if (caster == null || command.Spell.Caster?.Unit != caster)
            {
                RecordShellLifecycle(NativeShellStage.InitRefused, null, "caster-identity", DescribeInitPredicates(command));
                return;
            }
            var target = command.Target?.Unit;
            if (isMount && (caster.View == null || target?.View == null ||
                target != caster.Descriptor?.Pet || target.Descriptor?.Master.Value != caster ||
                !SupportedMountedProfiles.IsSupported(target)))
            {
                RecordShellLifecycle(NativeShellStage.InitRefused, null, "mount-target-ownership-view-profile",
                    DescribeInitPredicates(command));
                return;
            }
            if (isDismount && target != caster)
            {
                RecordShellLifecycle(NativeShellStage.InitRefused, null, "dismount-target-is-not-caster",
                    DescribeInitPredicates(command));
                return;
            }

            NativeRelationshipShell existing;
            if (!relationshipShells.TryGetValue(command, out existing))
            {
                if (this.relationship.World?.AdmissionFenced() == true)
                {
                    RecordShellLifecycle(NativeShellStage.InitRefused, null, "world-admission-fenced", DescribeInitPredicates(command));
                    return;
                }
                var requestOwner = isMount ? ResolveControlBoundary(caster, true) : ResolveControlBoundary(caster, false);
                var request = requestOwner?.CaptureNativeRequest(caster, isMount ? target : requestOwner.Mount, isDismount);
                if (request == null)
                {
                    RecordShellLifecycle(NativeShellStage.InitRefused, null, "pair-ownership", DescribeInitPredicates(command));
                    return;
                }
                // The local relationship is the captured owner, never the shared bridge projection.
                var relationship = request.Boundary;
                var shell = new NativeRelationshipShell(command)
                {
                    PairRequest = request,
                    Kind = isMount ? NativeMountedControlKind.MountCompanion : NativeMountedControlKind.Dismount,
                    GenerationAtInit = relationship.MountedPairGeneration,
                    CasterId = caster.UniqueId,
                    TargetId = target?.UniqueId,
                    ControlIdentity = "shell:" + (++relationshipShellSequence).ToString(CultureInfo.InvariantCulture) +
                        ":" + (isMount ? "mount" : "dismount") + ":" + caster.UniqueId
                };
                relationshipShells.Add(command, shell);
                admittedControlCommands.Add(new WeakReference<UnitUseAbility>(command));
                NativeRelationshipShellCount++;
                LastNativeRelationshipShellObservation = "control=" + shell.ControlIdentity +
                    ";generationAtInit=" + shell.GenerationAtInit +
                    ";target=" + (shell.TargetId ?? "<none>");
                RecordShellLifecycle(NativeShellStage.Registered, shell.ControlIdentity, "registered",
                    LastNativeRelationshipShellObservation);
            }

            // not-a-refusal: only the Mount approach radius is clamped.
            if (!isMount) { return; }
            float radius;
            if (CombatMountDismountPolicy.TryGetMountApproachRadius(command.ApproachRadius,
                caster.View.Corpulence, target.View.Corpulence, out radius))
            {
                MountApproachRadiusSetter.Invoke(command, new object[] { radius });
                RecordShellLifecycle(NativeShellStage.ApproachClamped, null, "approach-radius-clamped",
                    "radius=" + radius.ToString(CultureInfo.InvariantCulture));
            }
        }

        // The exact process binding, established at the command's own OnAction
        // boundary.
        //
        // This exists because of an exact native fact read from the installed
        // assembly: UnitUseAbility.OnAction (0x06002737) sets ExecutionProcess from
        // RuleCastSpell and then returns a TERMINAL result unless
        // AbilityExecutionProcess.IsEngageUnit (0x06008FD4). For a relationship
        // control the process does not engage a unit, so the command completes and
        // UnitCommands nulls m_Commands[Move] while the process goes on delivering on
        // later frames. Rediscovering ownership through the caster's current Move slot
        // therefore cannot work at Deliver: the command is no longer there.
        //
        // The binding is exact and one-to-one — this shell to this command's own
        // execution context — not a recent-shell or caster-only lookup.
        internal void BindNativeRelationshipProcess(UnitUseAbility command)
        {
            if (disposed || !enabled || !registered || serializationSuspended || command == null) { return; }
            BindNativeIntentProcess(command);
            NativeRelationshipShell shell;
            if (!relationshipShells.TryGetValue(command, out shell) || shell == null) { return; }
            var context = command.ExecutionProcess?.Context;
            if (context == null)
            {
                RecordShellLifecycle(NativeShellStage.ProcessBindingRefused, shell.ControlIdentity,
                    "execution-process-absent-at-onaction",
                    "processPresent=" + (command.ExecutionProcess != null) +
                    ";commandFinished=" + command.IsFinished + ";commandStarted=" + command.IsStarted);
                return;
            }
            NativeRelationshipShell bound;
            if (relationshipShellContexts.TryGetValue(context, out bound))
            {
                if (!ReferenceEquals(bound, shell))
                {
                    // Two different relationship shells claim one execution context. That
                    // is unresolvable, so the context is POISONED rather than resolved in
                    // favour of either: both shells are retired immediately and the
                    // poisoned context makes every later Deliver refuse, including after
                    // the Move slot has disappeared. Neither the first nor the second
                    // binding is preferred, and there is no fall-through to a slot shell.
                    PoisonExecutionContext(context, bound, shell);
                }
                return;
            }
            if (poisonedContexts.TryGetValue(context, out _))
            {
                RecordShellLifecycle(NativeShellStage.ProcessBindingRefused, shell.ControlIdentity,
                    "context-already-poisoned", "no binding is admitted on a poisoned context");
                RetireShell(shell, "poisoned-context");
                return;
            }
            relationshipShellContexts.Add(context, shell);
            shell.ProcessBound = true;
            NativeRelationshipProcessBindingCount++;
            RecordShellLifecycle(NativeShellStage.ProcessBound, shell.ControlIdentity, "bound-to-execution-context",
                "generationAtInit=" + shell.GenerationAtInit + ";caster=" + (shell.CasterId ?? "<none>") +
                ";target=" + (shell.TargetId ?? "<none>") + ";commandFinished=" + command.IsFinished);
        }

        // R5: the save barrier's exact relationship-transition term.
        //
        // A registered Mount/Dismount shell that has not finished defers a save
        // whether it is queued, approaching or delivering. This is strictly more
        // than NativeSaveEffectBoundary.CommandNeedsSettlement covers, because that
        // predicate requires the command to be RUNNING and so would let a queued but
        // unstarted relationship shell through.
        internal bool OwnsUnsettledRelationshipShell(UnitCommand command)
        {
            var ability = command as UnitUseAbility;
            if (ability == null || ability.IsFinished) { return false; }
            NativeRelationshipShell shell;
            return relationshipShells.TryGetValue(ability, out shell) && shell != null;
        }

        // The transition ledger's own in-flight window: between admission and
        // settlement the relationship is being mutated, so a save must not snapshot
        // it. The window is entirely inside one synchronous game-thread call, so this
        // term is a guard that fails closed rather than one a frame boundary is
        // expected to observe; it is queried instead of argued away.
        internal bool HasUnsettledRelationshipTransition => relationship.World == null ? playerAction.HasVoluntaryTransitionInFlight :
            relationship.World.Pairs.Any(pair => pair.PlayerActions.HasVoluntaryTransitionInFlight);

        // Read-only counters of the voluntary transition ledger, for persistence observations.
        internal MountedTransitionLedger RelationshipTransitionLedger => playerAction.TransitionLedger;

        internal string DescribeRelationshipTransitionSettlement() =>
            "ledgerInFlight=" + playerAction.HasVoluntaryTransitionInFlight +
            ";inFlightControl=" + (playerAction.TransitionLedger.InFlightControlIdentity ?? "<none>") +
            ";registeredShells=" + NativeRelationshipShellCount;

        // The exact shell whose delivery is running: the caster's own native Move
        // slot. A relationship control that cannot find its own shell there is not
        // delivering its own admitted command and is refused.
        private NativeRelationshipShell ResolveDeliveringShell(
            NativeMountedControlKind kind, UnitEntityData caster, UnitEntityData target,
            AbilityExecutionContext context, out string refusal)
        {
            // 1. The authoritative binding: the exact execution context this command
            //    established at its own OnAction boundary.
            NativeRelationshipShell contextShell = null;
            string poison = null;
            var contextPoisoned = context != null && poisonedContexts.TryGetValue(context, out poison);
            if (context != null && !contextPoisoned) { relationshipShellContexts.TryGetValue(context, out contextShell); }

            // 2. The synchronous route, and ONLY that. A Move-slot command is an owner
            //    solely when its own execution process created THIS very context, which
            //    is exactly the case where delivery happened inside OnAction before the
            //    postfix could bind. Anything else occupying that slot is a different
            //    command and is never an owner: this is not a recent-shell, last-shell,
            //    caster-only or generation-only lookup.
            var slot = caster?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
            NativeRelationshipShell slotShell = null;
            var slotOwnsThisContext = slot != null && context != null &&
                ReferenceEquals(slot.ExecutionProcess?.Context, context);
            if (slotOwnsThisContext) { relationshipShells.TryGetValue(slot, out slotShell); }

            // 3. One typed decision over the two bindings. NativeShellBindingPolicy owns
            //    the ordering — poisoned first, then disagreement, then the authoritative
            //    context, then the narrow synchronous slot — so every case is testable
            //    offline rather than only readable here.
            var outcome = NativeShellBindingPolicy.Resolve(
                context != null,
                contextPoisoned,
                contextShell != null,
                slot != null,
                slotOwnsThisContext,
                slotShell != null,
                ReferenceEquals(contextShell, slotShell),
                out refusal);
            var ownership = NativeShellBindingPolicy.DescribeOwnership(outcome);

            if (outcome == NativeShellBindingOutcome.PoisonedContext)
            {
                LastRelationshipShellRefusal = refusal + " " + (poison ?? "<none>");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, null, ownership, poison ?? "<none>");
                return null;
            }
            if (outcome == NativeShellBindingOutcome.Disagreement)
            {
                LastRelationshipShellRefusal = refusal;
                // Poison the context as well as retiring both shells, so this stays
                // terminal after the Move slot disappears.
                PoisonExecutionContext(context, contextShell, slotShell);
                RecordShellLifecycle(NativeShellStage.DeliverRefused, contextShell.ControlIdentity,
                    ownership, "slotControl=" + (slotShell.ControlIdentity ?? "<none>"));
                return null;
            }
            if (outcome == NativeShellBindingOutcome.NoExactOwnership)
            {
                LastRelationshipShellRefusal = refusal + " " + DescribeRelationshipShellState(caster);
                RecordShellLifecycle(NativeShellStage.DeliverRefused, null, ownership,
                    "contextPresent=" + (context != null) +
                    ";slotOwnsThisContext=" + slotOwnsThisContext + ";" + DescribeRelationshipShellState(caster));
                return null;
            }

            var shell = outcome == NativeShellBindingOutcome.ExecutionContext ? contextShell : slotShell;
            var relationship = shell.PairRequest?.Boundary;
            if (relationship == null)
            {
                refusal = "The native command has no captured pair owner.";
                RetireShell(shell, "missing-pair-owner");
                return null;
            }
            // A shell that has already been refused for a permanent reason can never
            // become valid again, whichever binding later presents it.
            if (shell.Retired)
            {
                refusal = "This mounted transition was already refused and cannot be revived.";
                LastRelationshipShellRefusal = refusal + " retiredBecause=" + (shell.RetiredReason ?? "<none>");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity, "retired",
                    "ownership=" + ownership + ";retiredBecause=" + (shell.RetiredReason ?? "<none>"));
                return null;
            }
            if (shell.Consumed)
            {
                refusal = "This exact mounted transition has already been delivered.";
                LastRelationshipShellRefusal = refusal;
                RetireShell(shell, "exactly-once");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity, "exactly-once",
                    "ownership=" + ownership);
                return null;
            }
            if (shell.Kind != kind || !string.Equals(shell.CasterId, caster.UniqueId, StringComparison.Ordinal))
            {
                refusal = "This native Move command belongs to a different mounted control.";
                LastRelationshipShellRefusal = refusal;
                RetireShell(shell, "kind-or-caster");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity, "kind-or-caster",
                    "ownership=" + ownership + ";shellKind=" + shell.Kind + ";deliverKind=" + kind);
                return null;
            }
            if (kind == NativeMountedControlKind.MountCompanion &&
                !string.Equals(shell.TargetId, target?.UniqueId, StringComparison.Ordinal))
            {
                refusal = "The mounted transition target changed after its native command was created.";
                LastRelationshipShellRefusal = refusal;
                RetireShell(shell, "target-changed");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity, "target-changed",
                    "ownership=" + ownership);
                return null;
            }
            if (kind == NativeMountedControlKind.Dismount)
            {
                // A Dismount targets the rider itself. Both the target recorded at
                // the command's own Init boundary and the target being delivered now
                // must still be that exact caster, and that caster must still be the
                // exact rider of the live relationship. The decision itself lives in
                // DismountTargetIdentityPolicy so each rejected condition — missing,
                // foreign, changed, changed-rider and stale-generation — is testable.
                var liveRider = shell.PairRequest?.Owner?.Rider;
                refusal = DismountTargetIdentityPolicy.Refuse(
                    target != null,
                    target != null && ReferenceEquals(target, caster),
                    shell.TargetId,
                    caster.UniqueId,
                    liveRider != null,
                    liveRider != null && ReferenceEquals(liveRider, caster),
                    shell.GenerationAtInit,
                    relationship.MountedPairGeneration);
                if (refusal != null)
                {
                    LastRelationshipShellRefusal = refusal;
                    RetireShell(shell, "dismount-target-identity");
                    RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity,
                        "dismount-target-identity", "ownership=" + ownership);
                    return null;
                }
            }
            // Diagnostics-only one-shot seam (CM02-generation-change): null in production; an armed hook may only
            // advance the relationship generation through its guarded diagnostic invalidation, so the stale-shell
            // refusal below is the only thing it can reach.
            var diagnosticInvalidation = MountedRelationshipDeliveryFault.FireBeforeShellGenerationCheck();
            if (diagnosticInvalidation != null)
            {
                RecordShellLifecycle(NativeShellStage.Registered, shell.ControlIdentity, "diagnostic-generation-invalidation",
                    diagnosticInvalidation);
            }
            GameMountedRelationshipService boundOwner;
            if (shell.GenerationAtInit != relationship.MountedPairGeneration ||
                !relationship.ResolveNativeRequest(shell.PairRequest, out boundOwner) ||
                !ReferenceEquals(shell.PairRequest.Rider, caster) ||
                kind == NativeMountedControlKind.MountCompanion && !ReferenceEquals(shell.PairRequest.Mount, target))
            {
                refusal = "The mounted relationship changed after this transition was requested.";
                LastRelationshipShellRefusal = refusal;
                RetireShell(shell, "generation");
                RecordShellLifecycle(NativeShellStage.DeliverRefused, shell.ControlIdentity, "generation",
                    "shellGeneration=" + shell.GenerationAtInit + ";current=" + relationship.MountedPairGeneration);
                return null;
            }
            RecordShellLifecycle(NativeShellStage.DeliverResolved, shell.ControlIdentity, "resolved",
                "ownership=" + ownership + ";processBound=" + shell.ProcessBound +
                ";kind=" + shell.Kind + ";generation=" + shell.GenerationAtInit);
            return shell;
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
            var relationship = ResolveControlBoundary(command?.Executor, false);
            if (relationship == null || !CaptureNativeIntent(command, kind, relationship)) return false;
            var rider = relationship.Rider;
            var exactManagedAbility = ReferenceEquals(blueprint, riderPrimaryAbility) ||
                ReferenceEquals(blueprint, mountPrimaryAbility);
            var casterIsExactActor = command?.Executor != null && command.Spell?.Caster?.Unit == command.Executor &&
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(kind, CombatController.IsInTurnBasedCombat(),
                    settings.UsePairedTurnControls, command.Executor == rider, command.Executor == relationship.Mount);
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
                ClearTargetingPresentation("disabled");
                ClearReserveBindings();
                reserveScan.Reset();
                DropNativeSlotObservations();
                RemoveAllManagedFacts(true);
                ClearSerializationHotbarLeases(false);
                EnsureUnsubscribed();
                // KMC itself emptied the public slots here, which is not the player's removal: placement may run once more.
                placementRecorded.Clear();
                // Disable is cleanup, not a temporary absence: nothing is put back from a remembered slot afterwards.
                placementIntent.Clear();
                loadFence.Reset();
            }
        }

        private bool areaSuspended;
        internal void SuspendAreaControls()
        {
            if (serializationSuspended) throw new InvalidOperationException("Area unload overlaps native save serialization.");
            areaSuspended = true;
            SettleAbortedNativeOperation();
            // RemoveAllManagedFacts(true) below empties still-observed former candidates as well: their departure is
            // classified first, from their bar as the player last left it (the save header's rule).
            ObserveDepartedRiders(CollectCurrentCandidateUnits());
            ObserveArrangementsAtBoundary();
            ClearTargetingPresentation("area-unloading");
            RemoveAllManagedFacts(true);
        }
        internal void ResumeAreaControls()
        {
            areaSuspended = false;
            if (!disposed && enabled) Update();
        }

        internal void Update()
        {
            ObservePendingRiderPrimaryOutcome();
            ReleaseStaleReserveScan();
            // Update never runs inside a native slot operation, so an open scope here was left by an exception: it is closed,
            // keeping only a write native completed before throwing (CloseNativeSlotScope).
            SettleAbortedNativeOperation();
            // The copy registry keeps only the current world's slot objects.
            CopiesOf(null);
            if (disposed || !enabled || serializationSuspended || areaSuspended)
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

            // A locale change replaces the native pack; the owned keys are registered again on the new one.
            if (!ReferenceEquals(LocalizationManager.CurrentPack, localizedPack) && !EnsureLocalization())
            {
                return;
            }

            // The outgoing world of a selected load reconciles nothing (its placement records were cleared for the
            // incoming world, and its riders still pass the current-world checks until native loading replaces them).
            if (!ArrangementsAccepted)
            {
                return;
            }

            ReconcileFacts();
        }

        internal NativeMountedControlAvailability Evaluate(
            NativeMountedControlKind kind,
            UnitEntityData caster)
        {
            if (disposed || !enabled || !registered || serializationSuspended || areaSuspended)
            {
                return new NativeMountedControlAvailability(false, false, "Mounted control services are not active.");
            }
            if (!RetiredPrimaryHelpersSelected && NativeMountedControlPolicy.IsRetiredPrimaryHelper(kind))
            {
                return new NativeMountedControlAvailability(false, false, "Attack with ordinary controls.");
            }
            if (kind == NativeMountedControlKind.MountState)
            {
                // The public control never vanishes; its availability and reason are the current shell's.
                var shell = Evaluate(ResolveMountStateIntent(caster), caster);
                return new NativeMountedControlAvailability(true, shell.IsEnabled, shell.IsTransitionReady, shell.Reason);
            }

            var relationship = ResolveControlBoundary(caster, kind == NativeMountedControlKind.MountCompanion);
            if (relationship == null) return new NativeMountedControlAvailability(false, false, "No exact mounted control owner is available.");
            var playerAction = relationship.PlayerActions;
            var combat = relationship.CombatOwner;
            // The escape hatch precedes the feature gate, through the same typed policy
            // that leasing uses. Without this a mounted rider keeps a VISIBLE but
            // DISABLED Dismount the moment the movement feature is switched off, which
            // strands the pair just as surely as removing the fact would.
            var escapeApplies = caster != null && NativeMountedControlPolicy.IsDismountEscape(
                kind,
                relationship.State == RelationshipState.Mounted,
                relationship.State == RelationshipState.Faulted,
                caster == relationship.Rider);

            if (!escapeApplies && !settings.EnableUnsafeMovementExperiment)
            {
                return new NativeMountedControlAvailability(
                    true,
                    false,
                    "Mounted controls are not available.");
            }

            if (caster == null || caster.Descriptor == null || !caster.IsInGame)
            {
                return new NativeMountedControlAvailability(false, false, "The ability owner is not active in the current area.");
            }

            switch (kind)
            {
                case NativeMountedControlKind.MountCompanion:
                {
                    var availability = playerAction.GetNativeMountAvailability(caster);
                    return RefuseRepeatedRelationshipRequest(
                        NativeMountedControlKind.MountCompanion,
                        caster,
                        availability,
                        "Mount Companion");
                }
                case NativeMountedControlKind.Dismount:
                {
                    var availability = playerAction.GetNativeDismountAvailability(caster);
                    return RefuseRepeatedRelationshipRequest(
                        NativeMountedControlKind.Dismount,
                        caster,
                        availability,
                        "Dismount");
                }
                case NativeMountedControlKind.RiderPrimary:
                    return combat.GetNativeAbilityAvailability(combat.ResolveRiderPrimaryAction(), caster);
                case NativeMountedControlKind.MountPrimary:
                    return combat.GetNativeAbilityAvailability(MountedCombatActionKind.MountPrimaryNatural, caster);
                case NativeMountedControlKind.MountedCharge:
                    return combat.EvaluateMountedCharge(caster);
                default:
                    return new NativeMountedControlAvailability(false, false, "Unknown mounted control.");
            }
        }

        private NativeMountedControlAvailability RefuseRepeatedRelationshipRequest(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            NativeMountedControlAvailability availability,
            string controlName)
        {
            if (!availability.IsEnabled)
            {
                return availability;
            }

            var moveSlot = caster?.Commands?.GetCommand(UnitCommand.CommandType.Move);
            if (!NativeMountedControlPolicy.IsRepeatedRelationshipRequest(
                    kind,
                    targetSelectionMode,
                    OwnsUnsettledRelationshipShell(moveSlot)))
            {
                return availability;
            }

            return new NativeMountedControlAvailability(
                true,
                false,
                false,
                controlName + " already has a pending native relationship command.");
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
            var relationship = ResolveControlBoundary(caster, kind == NativeMountedControlKind.MountCompanion);
            if (relationship == null) return false;
            var playerAction = relationship.PlayerActions;
            var combat = relationship.CombatOwner;
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
                case NativeMountedControlKind.MountedCharge:
                    return combat.CanMountedChargeTarget(caster, target);
                default:
                    return false;
            }
        }

        internal bool TryDispatch(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            UnitEntityData target) => TryDispatch(kind, caster, target, null);

        internal bool TryDispatch(
            NativeMountedControlKind kind,
            UnitEntityData caster,
            UnitEntityData target,
            AbilityExecutionContext context)
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
            string shellRefusal = null;
            NativeRelationshipShell deliveringShell = null;
            if (kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount)
            {
                deliveringShell = ResolveDeliveringShell(kind, caster, target, context, out shellRefusal);
            }
            // Ownership, kind, caster, target and generation are validated by now, so THIS
            // shell has made its one delivery attempt whatever happens next. The terminal
            // state is set in a finally, so a business refusal or an exception cannot
            // leave a reusable shell behind. Native cost already committed by the command
            // stands; nothing is refunded here.
            NativeIntentShell intent = null;
            var relationship = deliveringShell?.PairRequest.Boundary;
            if (kind != NativeMountedControlKind.MountCompanion && kind != NativeMountedControlKind.Dismount)
            {
                intent = ResolveNativeIntent(kind, caster, target, context);
                relationship = intent?.Request.Boundary;
            }
            var playerAction = relationship?.PlayerActions;
            var combat = relationship?.CombatOwner;
            var deliveryOutcome = "not-attempted";
            try
            {
                switch (kind)
                {
                    case NativeMountedControlKind.MountCompanion:
                        accepted = deliveringShell != null &&
                            playerAction.TryExecuteNativeMount(caster, target, deliveringShell.ControlIdentity);
                        deliveryOutcome = accepted ? "consumed-success" : "consumed-refused";
                        break;
                    case NativeMountedControlKind.Dismount:
                        accepted = deliveringShell != null &&
                            playerAction.TryExecuteNativeDismount(caster, deliveringShell.ControlIdentity);
                        deliveryOutcome = accepted ? "consumed-success" : "consumed-refused";
                        break;
                    case NativeMountedControlKind.RiderPrimary:
                        accepted = combat != null && combat.TryExecuteNativeAbility(
                            combat.ResolveRiderPrimaryAction(), caster, target) ==
                            MountedCombatClickResult.HandledAccepted;
                        break;
                    case NativeMountedControlKind.MountPrimary:
                        accepted = combat != null && combat.TryExecuteNativeAbility(
                            MountedCombatActionKind.MountPrimaryNatural, caster, target) ==
                            MountedCombatClickResult.HandledAccepted;
                        break;
                    case NativeMountedControlKind.MountedCharge:
                        accepted = combat != null && combat.TryExecuteMountedCharge(caster, target, context) ==
                            MountedCombatClickResult.HandledAccepted;
                        break;
                    default:
                        accepted = false;
                        break;
                }
            }
            catch (Exception exception)
            {
                deliveryOutcome = "consumed-exception:" + exception.GetType().FullName;
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
            finally
            {
                if (intent != null) intent.Consumed = true;
                // One terminal delivery attempt per resolved shell, on every path out of
                // the business dispatch: success, refusal, or exception.
                if (deliveringShell != null && !deliveringShell.Consumed)
                {
                    deliveringShell.Consumed = true;
                    if (!deliveryOutcome.StartsWith("consumed-success", StringComparison.Ordinal))
                    {
                        RetireShell(deliveringShell, deliveryOutcome);
                    }
                    RecordShellLifecycle(NativeShellStage.DeliverConsumed, deliveringShell.ControlIdentity,
                        deliveryOutcome, "kind=" + deliveringShell.Kind + ";retired=" + deliveringShell.Retired);
                }
            }

            if (accepted)
            {
                DispatchAcceptedCount++;
            }
            else
            {
                DispatchRejectedCount++;
                var refusal = shellRefusal;
                if (string.IsNullOrWhiteSpace(refusal))
                {
                    refusal = kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount
                        ? playerAction?.LastFeedback : combat?.LastFeedback;
                }
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
                relationship?.State == RelationshipState.Mounted)
            {
                if (!awaitingActivations.Contains(activation)) awaitingActivations.Add(activation);
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
            SettleAbortedNativeOperation();
            AdmitReserveBindings();
            ObserveArrangementsAtBoundary();
            CaptureAndClearManagedHotbarSlots();
            RemoveAllManagedFacts(false);
            logger.Info("Native mounted control facts and exact KMC hotbar bindings suspended before save serialization.");
            return true;
        }

        internal IEnumerator<object> WrapSaveRoutine(IEnumerator<object> inner)
        {
            var ownsScope = false;
            return new ScopedEnumerator<object>(inner,
                () =>
                {
                    if (disposed || serializationSuspended)
                        throw new InvalidOperationException("Mounted control save scope is unavailable.");
                    // Set before acquisition so a partial hotbar/fact failure rolls back.
                    ownsScope = true;
                    if (!BeginSaveSerializationScope())
                        throw new InvalidOperationException("Mounted control save scope could not start.");
                },
                () => { if (ownsScope) EndSaveSerializationScope(); });
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

        internal IReadOnlyList<NativeLifecycleDeliveryRecord> SnapshotLifecycleDeliveries()
        {
            return lifecycleLedger.Snapshot();
        }

        public void HandleAbilityTargetSelectionStart(AbilityData ability)
        {
            NoteTargetSelectionOperation("target-selection-start");
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
            NoteTargetSelectionOperation("target-selection-end");
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
            // Any ordinary cast is a deliberate new order for the caster's pair, not only a KMC control.
            VisitInputOwners(owner => owner.ObservePlayerOrderFor(ability?.Caster?.Unit));
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
            // Native ground input raises this event after publishing its new move.
            // Pre-admission already cancels the replaced pair action; only a live
            // stock targeting intent authorizes additional post-input cleanup.
            VisitInputOwners(owner =>
            {
                // The order supersedes Stop whatever becomes of the move; only its own completion can hold the pair.
                owner.ObserveSelectedPlayerOrder();
                if (owner.HasStockAttackIntent)
                    owner.CancelSelectedInput("native move request replaced stock attack intent");
            });
        }

        public void OnItemUseRequested(Kingmaker.Items.ItemEntity item, TargetWrapper target)
        {
            VisitInputOwners(owner =>
            {
                owner.ObserveSelectedPlayerOrder();
                if (owner.HasStockAttackIntent)
                    owner.CancelSelectedInput("native item-use request replaced stock attack intent");
            });
        }

        public void OnAttackRequested(UnitEntityData unit, Kingmaker.View.UnitEntityView target)
        {
            ResolveControlBoundary(unit, false)?.CombatOwner.ObserveStockAttackRequested(unit, target?.EntityData);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            ClearTargetingPresentation("disposed");
            ClearReserveBindings();
            reserveScan.Reset();
            DropNativeSlotObservations();
            RemoveAllManagedFacts(true);
            placementIntent.Clear();
            loadFence.Reset();
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
                MountPrimaryAbilityGuid,
                MountedChargeAbilityGuid,
                MountStateAbilityGuid
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
            if (horseCompanion.MountStateIcon == null)
            {
                throw new InvalidOperationException("The original KMC mount-state icon is unavailable for the Mount / Dismount control.");
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

            mountedChargeAbility = CreateMountedChargeAbility(horseIcon);
            // F2: one public control. It never executes itself: a click runs the current Mount or Dismount shell.
            mountStateAbility = CreateAbility(
                "KMC_MountStateAbility",
                MountStateAbilityGuid,
                "KMC.Native.MountState.Name",
                "KMC.Native.MountState.Description",
                NativeMountedControlKind.MountState,
                AbilityRange.Personal,
                false,
                false,
                true,
                UnitCommand.CommandType.Move,
                horseCompanion.MountStateIcon);
            // The delivery shells stay registered, leased and resolvable for saves, but leave the ability drawer: the
            // player sees Mount / Dismount and the ordinary Charge (Ability.Hidden reads Blueprint.Hidden).
            mountAbility.Hidden = true;
            dismountAbility.Hidden = true;
            mountedChargeAbility.Hidden = true;
            foreach (var ability in EnumerateBlueprints())
            {
                library.BlueprintsByAssetId.Add(ability.AssetGuid, ability);
                blueprintList.Add(ability);
            }
            registered = true;
            logger.Info("Registered six original KMC native mounted-control abilities with action-bar autofill disabled; Mount / Dismount is the public control and Mount, Dismount and the Mounted Charge shell are hidden delivery identities.");
        }

        internal float GetMountedChargeMinimumRange(UnitEntityData caster)
        {
            if (disposed || !enabled || !registered)
            {
                return 0f;
            }

            return ResolveControlBoundary(caster, false)?.CombatOwner.GetMountedChargeMinimumRange(caster) ?? 0f;
        }

        // The fifth original KMC ability: the rider's native full-round Standard charge shell. It differs from
        // the four controls in exactly the ways the stock charge differs from an ordinary ability: a Standard
        // full-round action, an enemy target, equipped weapons required, and a component that also provides the
        // stock minimum charge range. The range enum stays Unlimited so the native shell never asks the rider
        // to approach; the authoritative gate is the component's CanTarget, read from the mount.
        private BlueprintAbility CreateMountedChargeAbility(Sprite icon)
        {
            var ability = CreateOwned<BlueprintAbility>("KMC_MountedChargeAbility");
            ability.AssetGuid = MountedChargeAbilityGuid;
            ability.Type = AbilityType.Extraordinary;
            ability.Range = AbilityRange.Unlimited;
            ability.CanTargetPoint = false;
            ability.CanTargetEnemies = true;
            ability.CanTargetFriends = false;
            ability.CanTargetSelf = false;
            ability.SpellResistance = false;
            ability.LocalizedDuration = NewLocalizedString("KMC.Native.Empty");
            ability.LocalizedSavingThrow = NewLocalizedString("KMC.Native.Empty");
            ability.ActionBarAutoFillIgnored = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = true;
            ability.ActionType = UnitCommand.CommandType.Standard;
            ability.SetIsFullRoundAction(true);
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;
            ability.HasFastAnimation = true;
            ability.DisableLog = false;
            ability.ResourceAssetIds = new string[0];
            DisplayNameField.SetValue(ability, NewLocalizedString("KMC.Native.MountedCharge.Name"));
            DescriptionField.SetValue(ability, NewLocalizedString("KMC.Native.MountedCharge.Description"));
            IconField.SetValue(ability, icon);
            var logic = CreateOwned<MountedChargeAbilityLogic>("KMC_MountedChargeAbility_Logic");
            ability.ComponentsArray = new BlueprintComponent[] { logic };
            return ability;
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
                (ReferenceEquals(ability.Blueprint, mountAbility) || ReferenceEquals(ability.Blueprint, dismountAbility) ||
                 ReferenceEquals(ability.Blueprint, mountStateAbility));
        }

        private NativeMountedControlKind ResolveMountStateIntent(UnitEntityData unit)
        {
            var owner = ResolveControlBoundary(unit, false);
            return NativeMountedControlPolicy.ResolveMountStateIntent(
                owner?.State == RelationshipState.Mounted,
                owner?.State == RelationshipState.Faulted,
                owner != null && unit != null && unit == owner.Rider);
        }

        // The AbilityData the public slot runs right now: the current Mount or Dismount shell for the public control, or
        // the mounted rider's Charge shell for the ordinary Charge. Null keeps the slot's own AbilityData.
        internal AbilityData ResolvePublicSlotDelivery(MechanicActionBarSlotAbility slot, bool mountedCharge)
        {
            var unit = slot?.Unit;
            var blueprint = slot?.Ability?.Blueprint;
            if (blueprint == null || unit?.Descriptor?.Abilities == null || disposed || !enabled || !registered ||
                serializationSuspended || areaSuspended) return null;
            BlueprintAbility shell = null;
            if (ReferenceEquals(blueprint, mountStateAbility))
                shell = ResolveMountStateIntent(unit) == NativeMountedControlKind.Dismount ? dismountAbility : mountAbility;
            else if (mountedCharge)
                shell = mountedChargeAbility;
            var fact = shell == null ? null : unit.Descriptor.Abilities.GetAbility(shell);
            return fact != null && fact.Active ? fact.Data : null;
        }

        // The public control is never a command of its own; any route that tries to cast it directly is refused before
        // the engine can charge for it.
        internal bool AdmitPublicControlCommand(UnitCommand command)
        {
            var cast = command as UnitUseAbility;
            if (mountStateAbility == null || cast?.Spell?.Blueprint == null ||
                !ReferenceEquals(cast.Spell.Blueprint, mountStateAbility)) return true;
            PublicControlCommandRefusedCount++;
            logger.Info("Refused a direct Mount / Dismount command; the public control runs its current shell from the slot.");
            return false;
        }

        // A new native load begins: the previous world's placement records do not belong to it.
        internal void BeginLoadedWorld()
        {
            ClearTargetingPresentation("load");
            placementRecorded.Clear();
            placementIntent.Clear();
            initialPlacements.Clear();
        }

        // A selected native load begins (MountedPersistenceService, at its iterator's first step): the records are cleared
        // for the incoming world, and the outgoing player object, still current here, writes nothing until native loading
        // replaces it (or the load is abandoned). BeginLoadedWorld alone stays fence-free for the diagnostics that re-arm
        // the first placement within one world.
        // loadOwner: the load scope; only it can abandon this fence. The outgoing records are moved aside, not discarded.
        internal void BeginSelectedLoad(object loadOwner)
        {
            ClearTargetingPresentation("load");
            initialPlacements.Clear();
            // The outgoing world's scan bookkeeping goes with it (an abandoned load rescans, which is idempotent).
            reserveScan.Reset();
            // A disabled service holds no records to protect (disable cleared them) and never reaches the update that
            // would release a fence, so it arms none and keeps no reference to the outgoing player.
            loadFence.Begin(Game.Instance?.Player, loadOwner, placementIntent, placementRecorded, enabled && !disposed);
        }

        // The owning load scope ended without native completion (canceled, failed or superseded). If its outgoing world
        // is still current, that world's records (reserve slots, deliberate removals, placement records) come back
        // exactly and reconciliation resumes; another scope's fence is never released.
        internal void AbandonLoadedWorld(object loadOwner) =>
            loadFence.Abandon(loadOwner, Game.Instance?.Player, placementIntent, placementRecorded);

        private void ReconcileFacts()
        {
            AdmitReserveBindings();
            var candidates = CollectCurrentCandidateUnits();
            foreach (var unit in candidates)
            {
                RememberUnit(unit);
                ReconcileRetiredPrimaryReferences(unit);
                var publicFactReplaced = false;
                // A removed or replaced companion retires the old arrangement and its slots before anything below can
                // overwrite the stored pet link (observation or a stale-slot rebind).
                RetireOnCompanionLoss(unit);
                // Before this update withdraws, replaces or re-leases the public fact (active or already inactive), the bar
                // as the player last left it is the arrangement: a removal since the last update stays a removal, while an
                // absence already recorded (native cleanup, KMC suspension, a load) stays pending. Nothing is observed
                // while the fact is live and stays as it is, or while there is nothing to change.
                if (mountStateAbility != null)
                {
                    var publicFacts = unit?.Descriptor?.Abilities?.Enumerable
                        .Where(item => ReferenceEquals(item.Blueprint, mountStateAbility)).ToList() ?? new List<Ability>();
                    var publicDesired = ShouldLease(unit, NativeMountedControlKind.MountState);
                    var unchanged = publicDesired && publicFacts.Count == 1 && publicFacts[0].Active;
                    if (!unchanged && (publicFacts.Count > 0 || publicDesired)) ObserveArrangementOnBar(unit);
                }
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
                    // The public fact is missing, inactive or duplicated while the rider still gets it: native toolbar
                    // cleanup may already have emptied its slot, so the replacement below is a return from a temporary
                    // absence, never mistaken for the player removing a continuously live control.
                    if (desired && ReferenceEquals(ability, mountStateAbility)) publicFactReplaced = true;
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
                ReconcilePublicControlSlots(unit, publicFactReplaced);
                TryInitialPlacement(unit);
            }

            foreach (var prior in observedUnits.ToArray())
            {
                if (prior == null || candidates.Contains(prior))
                {
                    continue;
                }
                // A rider that left the candidate roster but is still the player's (the capital moves companions to the
                // reserve roster) keeps its chosen slot for its return; anything else lost eligibility for good. Its
                // KMC slots are emptied either way: native would empty a slot whose fact is gone (IsBad), and a native
                // save must never hold a KMC identity of a unit KMC no longer reconciles. A replaced object of a
                // character that is present now (a new load keeps actor ids), or an earlier world's object, records nothing.
                var recorded = ObserveDeparture(prior, candidates);
                RemoveManagedFacts(prior, true, recorded);
                observedUnits.Remove(prior);
                observedWorlds.Remove(prior);
            }
            RevalidateAbsentArrangements(candidates);
        }

        // An arrangement kept for a rider that is not reconciled now (the reserve roster) is retired on positive
        // evidence of permanent loss: the rider left the player's roster, or its pet link was cleared or now names
        // another unit. A rider that cannot be resolved proves nothing and keeps its arrangement.
        private void RevalidateAbsentArrangements(IList<UnitEntityData> candidates)
        {
            if (placementIntent.Count == 0) return;
            var characters = Game.Instance?.Player?.AllCharacters;
            foreach (var actorId in placementIntent.ActorIds.ToArray())
            {
                if (candidates.Any(unit => unit != null && unit.UniqueId == actorId)) continue;
                var matches = characters == null ? new UnitEntityData[0] :
                    characters.Where(unit => unit != null && unit.UniqueId == actorId).Take(2).ToArray();
                var unitNow = matches.Length == 1 ? matches[0] : null;
                placementIntent.Revalidate(actorId, unitNow != null, unitNow != null && IsOnPlayerRoster(unitNow),
                    unitNow?.Descriptor?.PetUniqueId);
            }
        }

        // A slot or auto-use selection an older save restored would otherwise outlive its fact. Only these exact entries
        // are touched; every other slot keeps its content and position, and the check is idempotent every update.
        private void ReconcileRetiredPrimaryReferences(UnitEntityData unit)
        {
            if (RetiredPrimaryHelpersSelected || unit == null) return;
            var slots = unit.UISettings?.Slots;
            if (slots != null)
            {
                for (var index = 0; index < slots.Length; index++)
                {
                    var blueprint = (slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint;
                    if (blueprint == null || !NativeMountedControlPolicy.IsRetiredPrimaryHelper(ResolveKind(blueprint))) continue;
                    unit.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, index);
                    RetiredPrimarySlotsCleared++;
                    logger.Info("Retired mounted primary hotbar entry cleared: unitId=" + unit.UniqueId + "; slot=" + index + ".");
                }
            }
            if (NativeMountedControlPolicy.IsRetiredPrimaryHelper(ResolveKind(unit.AutoUseAbility?.Blueprint)))
            {
                unit.AutoUseAbility = null;
                RetiredPrimaryAutoUseCleared++;
                logger.Info("Retired mounted primary auto-use selection cleared: unitId=" + unit.UniqueId + ".");
            }
        }

        // F2: an exact old Mount, Dismount or Mounted Charge binding becomes the public control it now stands for, in
        // the same slot; every other slot keeps its content and position. A stale binding of the public control is
        // rebound to the live fact. Idempotent every update; nothing is matched by display name.
        private void ReconcilePublicControlSlots(UnitEntityData unit, bool publicFactReplaced = false)
        {
            var slots = unit?.UISettings?.Slots;
            var abilities = unit?.Descriptor?.Abilities;
            if (slots == null || abilities == null || mountStateAbility == null) return;
            var publicControl = abilities.GetAbility(mountStateAbility);
            var publicActive = publicControl != null && publicControl.Active;
            // Copies no longer on the bar as the recorded object are released first.
            PruneCopies(unit, slots);
            // The player's effective binding and the copies of an interrupted choice, decided before the repair below
            // rebinds or migrates anything (EffectivePublicIndex: the same decision observation and capture use).
            bool onlyYieldingCopies;
            var effective = EffectivePublicIndex(unit, slots, out onlyYieldingCopies);
            var copies = Enumerable.Range(0, slots.Length).Where(i => IsPublicCopyAt(unit, slots, i)).ToList();
            if (!publicActive)
            {
                // An older alias is emptied below while the control is not live: its index is recorded first. A surviving
                // copy that is the fallback (its interrupted chosen slot is occupied) becomes the choice before KMC's own
                // migration clears it, exactly as a save taken first would keep it.
                if (effective >= 0 && copies.Contains(effective) && ArrangementsAccepted)
                {
                    placementIntent.PromoteFallback(unit.UniqueId, effective, unit.Descriptor?.PetUniqueId);
                    placementRecorded.Add(unit.UniqueId);
                }
                else RecoverLegacyAlias(unit, slots);
            }
            var boundIndex = -1;
            for (var index = 0; index < slots.Length; index++)
            {
                var bound = (slots[index] as MechanicActionBarSlotAbility)?.Ability;
                var kind = ResolveKind(bound?.Blueprint);
                if (kind == NativeMountedControlKind.MountState)
                {
                    if (!publicActive) continue;
                    if (boundIndex < 0) boundIndex = index;
                    if (unit.UniqueId != null) placementRecorded.Add(unit.UniqueId);
                    if (!ReferenceEquals(bound, publicControl.Data))
                    {
                        unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = publicControl.Data }, index);
                        MarkBarChanged(unit);
                    }
                    continue;
                }
                var repair = MountedControlSlotPolicy.RepairLiveSlot(kind);
                if (repair == MountedControlSlotRepair.Keep) continue;
                var replacement = repair == MountedControlSlotRepair.MountState
                    ? (publicActive && !SlotsHold(slots, mountStateAbility) ? publicControl.Data : null)
                    : ResolveStockChargeForSlot(unit, slots);
                unit.UISettings.SetSlot(replacement == null
                    ? (MechanicActionBarSlot)new MechanicActionBarSlotEmpty { Unit = unit }
                    : new MechanicActionBarSlotAbility { Unit = unit, Ability = replacement }, index);
                MarkBarChanged(unit);
                if (repair == MountedControlSlotRepair.MountState && unit.UniqueId != null) placementRecorded.Add(unit.UniqueId);
                if (replacement != null && publicActive && ReferenceEquals(replacement, publicControl.Data) && boundIndex < 0) boundIndex = index;
                LegacySlotRepairCount++;
                logger.Info("Old KMC control slot repaired: unitId=" + unit.UniqueId + "; slot=" + index + "; from=" + kind +
                    "; to=" + (replacement == null ? "empty" : replacement.Blueprint.name) + ".");
            }
            // Copies of an interrupted choice never stay beside the effective binding: every copy other than it goes (one
            // shortcut). The effective binding is the player's own binding wherever it is (a move or a new placement);
            // when only copies were left, they yield to the free chosen slot (the control returns there below), or the
            // first copy is the fallback while the chosen slot is occupied.
            if (publicActive && copies.Count > 0)
            {
                foreach (var copy in copies)
                {
                    if (copy == effective || !IsPublicBindingSlot(slots[copy])) continue;
                    unit.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, copy);
                    logger.Info("Secondary Mount / Dismount copy removed: unitId=" + unit.UniqueId + "; copy=" + copy +
                        "; kept=" + effective + ".");
                }
                MarkBarChanged(unit);
                boundIndex = effective >= 0 && ResolveKind((slots[effective] as MechanicActionBarSlotAbility)?.Ability?.Blueprint) ==
                    NativeMountedControlKind.MountState ? effective : onlyYieldingCopies ? -1 : FirstMountStateIndex(slots);
            }
            ReconcileChosenSlot(unit, slots, publicActive ? publicControl : null, boundIndex, publicFactReplaced);
            // Once no choice is interrupted any more, nothing on the bar is a copy of one.
            int chosen;
            if (unit.UniqueId != null && ArrangementsAccepted && !placementIntent.TryGetInterruptedChoice(unit.UniqueId, out chosen))
                ForgetCopies(unit.UniqueId);
            var autoUse = ResolveKind(unit.AutoUseAbility?.Blueprint);
            if (autoUse == NativeMountedControlKind.MountCompanion || autoUse == NativeMountedControlKind.Dismount ||
                autoUse == NativeMountedControlKind.MountedCharge || autoUse == NativeMountedControlKind.MountState)
                unit.AutoUseAbility = null;
        }

        // The player's arrangement of the public control. A live control in a slot is the current choice (a move updates
        // it). A live control in no slot returns to the remembered slot only after a temporary absence and only into an
        // empty slot; the player removing it while it was live, or filling that slot, is respected. An owner that is still
        // reconciled but no longer gets the control has lost eligibility for good.
        // The decisions and their order are MountedControlPlacementIntent.Reconcile (tested); this reads and writes the bar.
        private void ReconcileChosenSlot(UnitEntityData unit, MechanicActionBarSlot[] slots, Ability livePublicControl, int boundIndex,
            bool publicFactReplaced)
        {
            var id = unit?.UniqueId;
            if (id == null || !ArrangementsAccepted) return;
            var petId = unit.Descriptor?.PetUniqueId;
            var desired = livePublicControl != null || ShouldLease(unit, NativeMountedControlKind.MountState);
            // Confirmed permanent loss (pet link cleared or naming another unit): the rider's public-control slots go
            // too, even on a bar native UI never inspected, so a later companion cannot revive the old binding.
            if (livePublicControl == null && !desired && placementIntent.ProvesPermanentLoss(id, petId))
                ClearPublicControlSlots(unit, slots);
            var index = placementIntent.Reconcile(id, livePublicControl != null,
                desired, publicFactReplaced, boundIndex,
                i => i >= 0 && i < slots.Length && (slots[i] == null || slots[i] is MechanicActionBarSlotEmpty),
                // A save scope's own leases put the slots it emptied back once it ends; until then they are the authority.
                HasPendingSerializationLease(unit), placementRecorded.Contains(id), petId);
            if (index < 0) return;
            unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = livePublicControl.Data }, index);
            MarkBarChanged(unit);
            placementRecorded.Add(id);
            placementIntent.ObserveBound(id, index, petId);
            PlacementIntentRestoreCount++;
            logger.Info("Mount / Dismount returned to the player's chosen slot after a temporary absence: unitId=" + id +
                "; slot=" + index + ".");
        }

        // Native MechanicActionBarSlotAbility.IsBad answered true for a slot holding the public control: native
        // UnitUISettings.GetSlot / CleanupBadSlots is about to replace (or rebind) it because its fact is gone or
        // inactive. That is never the player's removal, even if the fact is active again by the next update. Only the
        // public control is observed; every other slot is ignored.
        internal void ObserveInvalidatedPublicSlot(MechanicActionBarSlotAbility slot)
        {
            // A disabled service acquires no placement intent from native toolbar cleanup.
            if (disposed || !enabled || mountStateAbility == null || slot == null ||
                !MountedControlSlotPolicy.IsPublicControlKind(ResolveKind(slot.Ability?.Blueprint))) return;
            var unit = slot.Unit ?? slot.Ability?.Caster?.Unit;
            // Only a current-world rider's own bar: the exact slot object must still be on it (native reads it from the
            // array before replacing it); any other slot object is ignored.
            if (unit?.UniqueId == null || !IsOnPlayerRoster(unit)) return;
            var slots = unit.UISettings?.Slots;
            var index = slots == null ? -1 : Array.FindIndex(slots, candidate => ReferenceEquals(candidate, slot));
            if (index < 0) return;
            // Only a report inside a native operation that replaces the slot on this bar (GetSlot or CleanupBadSlots) counts
            // (MountedNativeSlotScope); a standalone validity query changes nothing. Native wrote the previous reported slot
            // before moving on to this one: that outcome is applied first, so this report is read against it.
            var settled = new List<PendingSlotInvalidation>();
            if (!nativeSlotScope.Settle(unit.UISettings, index, Time.frameCount, settled)) return;
            ApplyConfirmedSlotReports(settled);
            // The arrangement this report belongs to: the live records, or the load scope's outgoing records while fenced.
            int chosen;
            var hasChoice = OwnerInterruptedChoice(unit, out chosen);
            var alias = !ReferenceEquals(slot.Ability?.Blueprint, mountStateAbility);
            // A copy already identified by its slot object stays one after the choice turns live again (for example a
            // fallback native rebound a moment earlier in this pass): its writes keep being followed until repair removes it.
            var recordedCopy = IsRecordedCopyAt(unit.UniqueId, index, slot);
            var pending = new PendingSlotInvalidation
            {
                World = Game.Instance?.Player, Kind = ResolveKind(slot.Ability?.Blueprint),
                Unit = unit, Slot = slot, Index = index, PetId = unit.Descriptor?.PetUniqueId,
                Desired = ShouldLease(unit, NativeMountedControlKind.MountState),
                Alias = alias, AliasIndex = alias ? index : -1,
                // A copy of an interrupted choice, by identity: the exact binding recorded at its index when the choice was
                // interrupted (or an older alias, which no live control produces). A later placement or move is not.
                Copy = recordedCopy || hasChoice && alias,
                // The other public bindings on the bar right now: the copies, should this report interrupt the choice.
                CopySlots = Enumerable.Range(0, slots.Length).Where(i => i != index && IsPublicBindingSlot(slots[i]))
                    .Select(i => new KeyValuePair<int, MechanicActionBarSlot>(i, slots[i])).ToList(),
                // Whether the owning arrangement's chosen slot is free at this moment (a copy then yields to it).
                ChosenFree = !hasChoice || chosen >= slots.Length || slots[chosen] == null || slots[chosen] is MechanicActionBarSlotEmpty
            };
            // Without an interrupted choice, only the bar's selected binding (or a recorded copy, as a copy) is reported: a
            // report for any other canonical binding or alias is not attributed to the choice (the canonical binding wins,
            // then the first alias).
            if (!hasChoice && !recordedCopy && (alias ? LegacyAliasIndex(slots) != index :
                !MountedControlSlotPolicy.IsSelectedPublicReport(PublicBindingIndex(slots), index))) return;
            // Held until native confirms it wrote the slot: its next report, its normal return, or native's own write found
            // when an aborted scope closes.
            nativeSlotScope.Report(unit.UISettings, index, Time.frameCount, pending, null);
        }

        internal long NativeSlotInvalidationCount { get; private set; }
        internal long NativeSlotReboundCount { get; private set; }

        private sealed class PendingSlotInvalidation
        {
            // The player world the report came from; a deferred report never applies to another world.
            internal object World;
            internal NativeMountedControlKind Kind;
            internal UnitEntityData Unit;
            internal MechanicActionBarSlotAbility Slot;
            internal int Index, AliasIndex;
            internal string PetId;
            internal bool Desired, Alias, ChosenFree, Copy;
            internal List<KeyValuePair<int, MechanicActionBarSlot>> CopySlots;
        }

        private readonly MountedNativeSlotScope<PendingSlotInvalidation> nativeSlotScope = new MountedNativeSlotScope<PendingSlotInvalidation>();
        // Riders whose arrangement a report of the current native operation retired: its later reports (the same stale
        // batch) are not applied; a later operation's reports (a genuine new placement) are.
        private readonly HashSet<string> retiredInNativeOperation = new HashSet<string>(StringComparer.Ordinal);

        internal long NativeSlotReportsDiscarded => nativeSlotScope.Discarded;

        // Native UnitUISettings.GetSlot(index) / CleanupBadSlots begins (exact prefixes; slotIndex -1 for the whole bar). A
        // previous operation that never returned is closed first (CloseNativeSlotScope).
        internal void BeginNativeBadSlotCleanup(Kingmaker.UI.UnitSettings.UnitUISettings settings, int slotIndex = -1)
        {
            CloseNativeSlotScope();
            retiredInNativeOperation.Clear();
            nativeSlotScope.Begin(disposed || !enabled ? null : settings, slotIndex, Time.frameCount);
        }

        // An operation that never returned (it threw): its last report is applied only when the slot holds native's own
        // write (NativeWroteReportedSlot); otherwise it is dropped. Also run by the next update.
        private void CloseNativeSlotScope()
        {
            var recovered = new List<PendingSlotInvalidation>();
            nativeSlotScope.Close(NativeWroteReportedSlot, recovered);
            if (recovered.Count > 0 && enabled && !disposed) ApplyConfirmedSlotReports(recovered);
            retiredInNativeOperation.Clear();
        }

        // What native writes into a reported slot: GetSlot and CleanupBadSlots empty it with a new MechanicActionBarSlotEmpty
        // bound to the rider (Unit = unit / Owner.Unit, contract rows), or CleanupBadSlots rebinds it to the live fact of the
        // same blueprint (IsNativeRebind). The player's removal on the action bar writes null (ActionBarIndexSlot.TryToSet),
        // and another ability is neither, so neither is mistaken for native's write.
        private bool NativeWroteReportedSlot(PendingSlotInvalidation item)
        {
            var slots = item?.Unit?.UISettings?.Slots;
            var current = slots == null || item.Index >= slots.Length ? null : slots[item.Index];
            if (current == null || ReferenceEquals(current, item.Slot)) return false;
            var empty = current as MechanicActionBarSlotEmpty;
            if (empty != null) return ReferenceEquals(empty.Unit, item.Unit);
            var now = current as MechanicActionBarSlotAbility;
            var fact = now?.Ability == null ? null : item.Unit.Descriptor?.Abilities?.GetAbility(now.Ability.Blueprint);
            return MountedControlSlotPolicy.IsNativeRebind(item.Kind, ResolveKind(now?.Ability?.Blueprint),
                fact != null && fact.Active && ReferenceEquals(fact.Data, now.Ability));
        }

        internal void EndNativeBadSlotCleanup(Kingmaker.UI.UnitSettings.UnitUISettings settings)
        {
            ApplyConfirmedSlotReports(nativeSlotScope.End(settings));
        }

        // Each confirmed report, classified from what the slot holds now (MountedControlSlotPolicy.IsNativeRebind): a
        // binding of the same KMC kind holding the live fact's own data is native's rebind (the pinned build writes a new
        // MechanicActionBarSlotAbility for it, contract 'CleanupBadSlots rebinds a bad ability slot with a new slot object');
        // anything else written there is an emptying. A slot still holding the reported object was not written.
        private void ApplyConfirmedSlotReports(List<PendingSlotInvalidation> confirmed)
        {
            if (confirmed == null) return;
            foreach (var item in confirmed)
            {
                if (item?.Unit?.UniqueId == null || retiredInNativeOperation.Contains(item.Unit.UniqueId)) continue;
                var slots = item.Unit.UISettings?.Slots;
                var current = slots == null || item.Index >= slots.Length ? null : slots[item.Index];
                if (ReferenceEquals(current, item.Slot)) continue;
                var now = current as MechanicActionBarSlotAbility;
                var kindNow = ResolveKind(now?.Ability?.Blueprint);
                var fact = now?.Ability == null ? null : item.Unit.Descriptor?.Abilities?.GetAbility(now.Ability.Blueprint);
                var live = fact != null && fact.Active && ReferenceEquals(fact.Data, now.Ability);
                if (ApplySlotInvalidation(item, MountedControlSlotPolicy.IsNativeRebind(item.Kind, kindNow, live), current))
                    retiredInNativeOperation.Add(item.Unit.UniqueId);
            }
        }

        // Disable and dispose: an open scope's reports are dropped (nothing is recorded any more) and no slot object or world
        // is kept.
        private void DropNativeSlotObservations()
        {
            nativeSlotScope.Close();
            retiredInNativeOperation.Clear();
            recordedCopies.Clear();
            recordedCopiesWorld = null;
        }

        // Copies of each rider's interrupted choice, by identity: (index, slot object) pairs recorded when native
        // interrupted the choice. Kept for the current player world only.
        private readonly Dictionary<string, List<KeyValuePair<int, MechanicActionBarSlot>>> recordedCopies =
            new Dictionary<string, List<KeyValuePair<int, MechanicActionBarSlot>>>(StringComparer.Ordinal);
        private object recordedCopiesWorld;

        private List<KeyValuePair<int, MechanicActionBarSlot>> CopiesOf(string actorId)
        {
            var world = Game.Instance?.Player;
            if (!ReferenceEquals(world, recordedCopiesWorld)) { recordedCopies.Clear(); recordedCopiesWorld = world; }
            List<KeyValuePair<int, MechanicActionBarSlot>> copies;
            return actorId != null && recordedCopies.TryGetValue(actorId, out copies) ? copies : null;
        }

        private bool IsRecordedCopyAt(string actorId, int index, MechanicActionBarSlot slot)
        {
            var copies = CopiesOf(actorId);
            return copies != null && slot != null && copies.Exists(pair => pair.Key == index && ReferenceEquals(pair.Value, slot));
        }

        // The interrupted choice of the arrangement a report belongs to: the live records, or the load scope's outgoing
        // records while a selected load fences the current world.
        private bool OwnerInterruptedChoice(UnitEntityData unit, out int index)
        {
            index = MountedControlPlacementIntent.OffBar;
            if (unit?.UniqueId == null) return false;
            return ArrangementsAccepted
                ? placementIntent.TryGetInterruptedChoice(unit.UniqueId, out index)
                : loadFence.TryGetOutgoingInterruptedChoice(Game.Instance?.Player, unit.UniqueId, out index);
        }

        // rebound: native put a live fact of the same blueprint into the slot (the live arrangement there); otherwise the
        // slot was emptied: never the player's removal, the chosen slot returns after the absence.
        // Returns true when the report retired the rider's arrangement (the batch's later reports are then stale).
        private bool ApplySlotInvalidation(PendingSlotInvalidation item, bool rebound, MechanicActionBarSlot written)
        {
            if (disposed || item?.Unit?.UniqueId == null) return false;
            var unit = item.Unit;
            var id = unit.UniqueId;
            // Only in the world that produced the report and for the same rider instance, still on its roster: work
            // left over from a replaced world is discarded (checked before the fence can be released by the new world).
            if (!MountedControlSlotPolicy.DeferredObservationApplies(item.World, Game.Instance?.Player, IsOnPlayerRoster(unit))) return false;
            bool recorded;
            if (!ArrangementsAccepted)
            {
                // A selected load fences this (still current, outgoing) world: the live records belong to the incoming
                // world, so the outcome is kept with the load scope's outgoing records (an abandoned load returns it; a
                // replaced world drops it). A confirmed companion loss retires the outgoing arrangement and clears the
                // rider's public bindings at once, so an abandoned load cannot hand the old binding to the new companion.
                var outgoingSlots = unit.UISettings?.Slots;
                if (loadFence.RetireOutgoingOnCompanionLoss(Game.Instance?.Player, id, item.PetId, item.Desired,
                    outgoingSlots != null && outgoingSlots.Any(IsPublicBindingSlot)))
                {
                    if (outgoingSlots != null) ClearPublicControlSlots(unit, outgoingSlots);
                    ForgetCopies(id);
                    return true;
                }
                recorded = item.Alias && !rebound
                    ? loadFence.AdmitOutgoingSurvivingBinding(Game.Instance?.Player, id, item.AliasIndex, item.PetId, item.Desired, item.ChosenFree, item.Copy)
                    : loadFence.ObserveOutgoingCleanupOutcome(Game.Instance?.Player, id, item.Index, item.PetId, item.Desired, rebound,
                        item.Copy, item.ChosenFree);
            }
            else
            {
                // A slot of a removed or replaced companion is not the next companion's choice: it is retired, not remembered.
                if (RetireOnCompanionLoss(unit)) { ForgetCopies(id); return true; }
                // rebound: a rebind of a recorded copy keeps the interrupted choice while its slot is free; a rebind of
                // anything else is the live arrangement. Emptied: a copy of the interrupted choice is simply gone while the
                // chosen slot is free (the fallback when it is occupied); any other binding (the selected one, or a
                // placement the player made since) becomes the interrupted choice. An older alias with no arrangement is
                // recovered by the reserve rule (ObserveNativeAliasInvalidated).
                recorded = rebound
                    ? placementIntent.ObserveNativeCleanupOutcome(id, item.Index, item.PetId, item.Desired, true, item.Copy, item.ChosenFree)
                    : item.Alias
                        ? placementIntent.ObserveNativeAliasInvalidated(id, item.AliasIndex, item.PetId, item.Desired, item.ChosenFree, item.Copy)
                        : placementIntent.ObserveNativeSlotInvalidated(id, item.Index, item.PetId, item.Desired, item.Copy, item.ChosenFree);
                // The player's shortcut was seen: its one-time placement is satisfied even though native emptied it (no
                // autofill copy if its chosen slot is taken before the control returns).
                if (recorded) placementRecorded.Add(id);
            }
            if (recorded && rebound) NativeSlotReboundCount++;
            else if (recorded) NativeSlotInvalidationCount++;
            TrackCopies(item, rebound, recorded, written);
            return false;
        }

        // The copy registry after one applied report. A report that became the interrupted choice (emptied, not a copy
        // yielding to it) makes the other public bindings on the bar at that moment its copies; a copy native emptied is no
        // longer one (a later placement at its index is the player's); a copy native rebound stays one, as the new slot
        // object native wrote. A rebind of the player's own binding (the effective slot, for example a moved shortcut)
        // keeps the surviving copies identified, so repair and capture still remove and skip them; they are released when
        // repair removes them, when another object replaces them (PruneCopies), or when KMC clears the rider's bindings.
        private void TrackCopies(PendingSlotInvalidation item, bool rebound, bool recorded, MechanicActionBarSlot written)
        {
            var id = item.Unit.UniqueId;
            var copies = CopiesOf(id);
            if (item.Copy)
            {
                if (copies == null) return;
                var at = copies.FindIndex(pair => pair.Key == item.Index && ReferenceEquals(pair.Value, item.Slot));
                if (at < 0) return;
                // A copy that became the fallback (its chosen slot occupied) is the choice now; the other copies stay copies.
                if (rebound && !recorded) copies[at] = new KeyValuePair<int, MechanicActionBarSlot>(item.Index, written);
                else copies.RemoveAt(at);
                return;
            }
            if (!recorded || rebound) return;
            recordedCopies[id] = new List<KeyValuePair<int, MechanicActionBarSlot>>(item.CopySlots ??
                new List<KeyValuePair<int, MechanicActionBarSlot>>());
        }

        private void ForgetCopies(string actorId)
        {
            if (actorId != null) recordedCopies.Remove(actorId);
        }

        // A recorded copy whose index no longer holds that exact slot object (removed, replaced or moved) is released.
        private void PruneCopies(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            var copies = CopiesOf(unit?.UniqueId);
            if (copies == null) return;
            copies.RemoveAll(pair => slots == null || pair.Key < 0 || pair.Key >= slots.Length || !ReferenceEquals(slots[pair.Key], pair.Value));
            if (copies.Count == 0) ForgetCopies(unit.UniqueId);
        }

        // An aborted native operation's completed write is accounted for before anything reads the bar for an archive or a
        // boundary (CloseNativeSlotScope), not only at the next update or slot operation.
        private void SettleAbortedNativeOperation()
        {
            if (nativeSlotScope.Open) CloseNativeSlotScope();
        }

        // Positive evidence that the companion the rider's arrangement belongs to is gone (MountedControlPlacementIntent.
        // RetireOnCompanionLoss: the pet link names a different companion, or it is cleared while the rider no longer
        // gets the control). Checked before any observation can overwrite the stored identity (each update, every bar
        // observation at a boundary, the native cleanup report). The arrangement is retired, older in-world rows cannot
        // recreate it, and the rider's public-control slots are cleared (even on an uninspected bar), so the next
        // companion cannot inherit the old slot. Returns true when it retired something.
        private bool RetireOnCompanionLoss(UnitEntityData unit)
        {
            var id = unit?.UniqueId;
            if (id == null || mountStateAbility == null || !ArrangementsAccepted) return false;
            var slots = unit.UISettings?.Slots;
            var barHolds = slots != null && slots.Any(IsPublicBindingSlot);
            if (!placementIntent.RetireOnCompanionLoss(id, unit.Descriptor?.PetUniqueId,
                ShouldLease(unit, NativeMountedControlKind.MountState), barHolds)) return false;
            if (slots != null) ClearPublicControlSlots(unit, slots);
            return true;
        }

        // An older save can leave a native public-control binding (and other exact KMC bindings) on a current-world
        // rider that is not reconciled now: a reserve or detached companion that has never been observed in this world.
        // No fact stands behind such a binding. Once per unit object: an identifiable public-control binding of a
        // rider whose pet link names a companion is recovered as its chosen slot (a return from absence, with its
        // placement record); every KMC binding on that bar is then cleared, so neither native serialization nor a
        // disabled KMC keeps the runtime reference. Unrelated slots keep their content and position. Still-observed
        // former candidates are left to their departure classification.
        private void AdmitReserveBindings()
        {
            if (disposed || !enabled || mountStateAbility == null || !ArrangementsAccepted) return;
            var player = Game.Instance?.Player;
            var reserveUnits = ReserveRosterUnits(CollectCurrentCandidateUnits());
            // The ledger keeps only this world and the units still on its reserve roster (MountedScanLedger).
            reserveScan.Synchronize(player, reserveUnits);
            if (player == null) return;
            foreach (var unit in reserveUnits)
            {
                if (observedUnits.Contains(unit) || !reserveScan.MarkFirst(unit)) continue;
                var slots = unit.UISettings?.Slots;
                if (slots == null || CountManagedHotbarSlots(unit) == 0) continue;
                var kinds = slots.Select(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint)).ToArray();
                // ReconcilePublicControlSlots' precedence: the public control wins over an older Mount / Dismount alias.
                var bound = MountedControlSlotPolicy.RecoveredPublicSlot(kinds);
                if (bound >= 0 && placementIntent.AdmitSurvivingBinding(unit.UniqueId, bound, unit.Descriptor?.PetUniqueId))
                {
                    placementRecorded.Add(unit.UniqueId);
                    ReserveBindingsRecovered++;
                    logger.Info("Surviving Mount / Dismount binding of a reserve rider recovered as its chosen slot: unitId=" +
                        unit.UniqueId + "; slot=" + bound + ".");
                }
                // Every KMC binding goes; an older Mounted Charge becomes the rider's own stock Charge in the same slot
                // (the existing migration: only when that Charge is available and not already on the bar).
                for (var index = 0; index < slots.Length; index++)
                {
                    if (kinds[index] == NativeMountedControlKind.None) continue;
                    var replacement = MountedControlSlotPolicy.RepairLiveSlot(kinds[index]) == MountedControlSlotRepair.StockCharge
                        ? ResolveStockChargeForSlot(unit, slots) : null;
                    unit.UISettings.SetSlot(replacement == null
                        ? (MechanicActionBarSlot)new MechanicActionBarSlotEmpty { Unit = unit }
                        : new MechanicActionBarSlotAbility { Unit = unit, Ability = replacement }, index);
                    if (replacement != null) LegacySlotRepairCount++;
                }
                MarkBarChanged(unit);
            }
        }

        internal long ReserveBindingsRecovered { get; private set; }

        // A reconciled rider's older exact Mount / Dismount alias (no canonical public binding on the bar) is the
        // player's chosen slot. Repair turns it into the public control only while that control is live; otherwise the
        // alias is emptied, so its index is recorded first (update-first and save-first alike) as a return from absence,
        // by the reserve rule: a different companion, or a cleared link while the rider no longer gets the control,
        // recovers nothing; an arrangement already known wins.
        private bool RecoverLegacyAlias(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            var id = unit?.UniqueId;
            if (id == null || slots == null || mountStateAbility == null || !enabled || !ArrangementsAccepted) return false;
            var at = LegacyAliasIndex(slots);
            if (at < 0) return false;
            if (!placementIntent.AdmitSurvivingBinding(id, at, unit.Descriptor?.PetUniqueId,
                ShouldLease(unit, NativeMountedControlKind.MountState))) return false;
            placementRecorded.Add(id);
            logger.Info("Older Mount / Dismount binding recorded as the chosen slot: unitId=" + id + "; slot=" + at + ".");
            return true;
        }


        // A copy of the rider's interrupted choice at index, by identity: the exact binding recorded there when native
        // interrupted the choice (it stays a copy until it leaves the bar or the copies are forgotten, so a boundary that
        // has already observed the effective binding reads the same bar the same way), or an older alias while the choice is
        // interrupted elsewhere (MountedControlPlacementIntent.IsSecondaryBinding; no live control produces one). A binding
        // the player placed or moved since is a new slot object or another index, so it is never a copy.
        private bool IsPublicCopyAt(UnitEntityData unit, MechanicActionBarSlot[] slots, int index)
        {
            if (unit?.UniqueId == null || slots == null || index < 0 || index >= slots.Length || !IsPublicBindingSlot(slots[index])) return false;
            if (IsRecordedCopyAt(unit.UniqueId, index, slots[index])) return true;
            var alias = !ReferenceEquals((slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, mountStateAbility);
            return placementIntent.IsSecondaryBinding(unit.UniqueId, index, alias);
        }

        // The bar's effective public binding (MountedControlSlotPolicy.EffectiveBindingIndex): the player's own binding
        // wins wherever it is; copies yield to a free chosen slot (onlyYieldingCopies) or the first is the fallback while
        // it is occupied. Live repair, boundary observation, capture classification and the live archive writer all use it.
        private int EffectivePublicIndex(UnitEntityData unit, MechanicActionBarSlot[] slots, out bool onlyYieldingCopies)
        {
            onlyYieldingCopies = false;
            if (slots == null) return -1;
            var kinds = slots.Select(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint)).ToArray();
            return MountedControlSlotPolicy.EffectiveBindingIndex(kinds, i => IsPublicCopyAt(unit, slots, i), ChosenSlotFree(unit, slots),
                out onlyYieldingCopies);
        }

        private int FirstMountStateIndex(MechanicActionBarSlot[] slots) => slots == null ? -1 :
            Array.FindIndex(slots, slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint) == NativeMountedControlKind.MountState);

        private bool ChosenSlotFree(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            int chosen;
            return unit?.UniqueId != null && slots != null && placementIntent.TryGetInterruptedChoice(unit.UniqueId, out chosen) &&
                chosen < slots.Length && (slots[chosen] == null || slots[chosen] is MechanicActionBarSlotEmpty);
        }

        private bool IsPublicBindingSlot(MechanicActionBarSlot slot) =>
            MountedControlSlotPolicy.IsPublicControlKind(ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint));

        // The bar's public binding with the repair precedence (MountedControlSlotPolicy.RecoveredPublicSlot): its first
        // canonical binding, otherwise its first exact older alias; -1 when neither is there. Observation, capture
        // classification and the live archive writer all use this one index.
        private int PublicBindingIndex(MechanicActionBarSlot[] slots) => slots == null ? -1 :
            MountedControlSlotPolicy.RecoveredPublicSlot(slots.Select(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint)).ToArray());

        // The index of the bar's first exact older Mount / Dismount alias, or -1 when a canonical public binding is on
        // the bar (it wins) or no alias is.
        private int LegacyAliasIndex(MechanicActionBarSlot[] slots)
        {
            if (slots == null) return -1;
            var kinds = slots.Select(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint)).ToArray();
            var at = MountedControlSlotPolicy.RecoveredPublicSlot(kinds);
            return at < 0 || kinds[at] == NativeMountedControlKind.MountState ? -1 : at;
        }

        // Disable and disposal: every exact KMC binding on a current-world rider that is not reconciled is cleared too.
        private void ClearReserveBindings()
        {
            foreach (var unit in ReserveRosterUnits(CollectCurrentCandidateUnits()))
            {
                if (CountManagedHotbarSlots(unit) == 0) continue;
                ClearManagedHotbarSlots(unit);
                MarkBarChanged(unit);
            }
        }

        // The current world's player characters on the reserve (remote) roster or detached, that are not candidates now.
        private static List<UnitEntityData> ReserveRosterUnits(List<UnitEntityData> candidates)
        {
            var result = new List<UnitEntityData>();
            var player = Game.Instance?.Player;
            if (player == null) return result;
            foreach (var list in new[] { player.RemoteCompanions, player.DetachedPartyCharacters })
            {
                if (list == null) continue;
                foreach (var reference in list)
                {
                    var unit = reference.Value;
                    if (unit?.UniqueId == null || candidates.Contains(unit) || result.Contains(unit) || !IsOnPlayerRoster(unit)) continue;
                    result.Add(unit);
                }
            }
            return result;
        }

        private readonly MountedScanLedger<UnitEntityData> reserveScan = new MountedScanLedger<UnitEntityData>();

        // Every update, before any early return: once the world is gone or replaced, or a scanned unit left the reserve
        // roster, the ledger lets go of it, whether or not reconciliation runs this frame.
        private void ReleaseStaleReserveScan()
        {
            if (reserveScan.IsEmpty) return;
            var player = Game.Instance?.Player;
            reserveScan.Synchronize(player, player == null ? null : ReserveRosterUnits(CollectCurrentCandidateUnits()));
        }

        // Only slots holding a public binding (the public control or an exact older Mount / Dismount alias standing for
        // it); every other slot keeps its content and position.
        private void ClearPublicControlSlots(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            ForgetCopies(unit?.UniqueId);
            var cleared = false;
            for (var index = 0; index < slots.Length; index++)
            {
                if (!IsPublicBindingSlot(slots[index])) continue;
                unit.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, index);
                cleared = true;
            }
            if (!cleared) return;
            MarkBarChanged(unit);
            logger.Info("Mount / Dismount slot cleared after the companion was permanently lost: unitId=" + unit.UniqueId + ".");
        }

        private bool HasPendingSerializationLease(UnitEntityData unit) =>
            hotbarSerializationLeases.Any(lease => ReferenceEquals(lease.Unit, unit));

        // The arrangement on the bar at a boundary that empties the slots (a save scope) is authoritative: a move or a
        // removal made since the last update is observed here, before the slots are leased and the facts removed.
        private void ObserveArrangementsAtBoundary()
        {
            if (mountStateAbility == null) return;
            foreach (var unit in CollectCurrentCandidateUnits()) ObserveArrangementOnBar(unit);
        }

        // One rider's bar as it stands now (binding by blueprint, whatever its fact's state), before KMC empties it.
        private void ObserveArrangementOnBar(UnitEntityData unit)
        {
            var id = unit?.UniqueId;
            var slots = unit?.UISettings?.Slots;
            // A disabled service (for example its save scope still sanitizing surviving bindings) records nothing.
            if (id == null || slots == null || mountStateAbility == null || !enabled || !ArrangementsAccepted) return;
            // A removed or replaced companion is retired before the boundary observation could overwrite its identity.
            if (RetireOnCompanionLoss(unit)) return;
            // An older alias stands for the public control (archive capture can precede the first update).
            RecoverLegacyAlias(unit, slots);
            var control = unit.Descriptor?.Abilities?.GetAbility(mountStateAbility);
            // The visible public binding: canonical first, else an older alias still on the bar (for example one a save
            // scope returned), which is the player's current choice, never an empty slot. A public binding the player
            // placed also satisfies the one-time placement (no autofill copy may follow it).
            // The effective binding (EffectivePublicIndex, the decision live repair makes): the player's own binding wherever
            // it is, or the fallback copy while the chosen slot is occupied. Copies that yield to a free chosen slot leave
            // the interrupted choice as it is.
            bool onlyYieldingCopies;
            var bound = EffectivePublicIndex(unit, slots, out onlyYieldingCopies);
            if (bound >= 0 || onlyYieldingCopies) placementRecorded.Add(id);
            if (bound >= 0)
                placementIntent.ObserveAtBoundary(id, true, bound, placementRecorded.Contains(id), unit.Descriptor?.PetUniqueId);
            else if (!onlyYieldingCopies)
                placementIntent.ObserveAtBoundary(id, control != null && control.Active, bound, placementRecorded.Contains(id),
                    unit.Descriptor?.PetUniqueId);
        }

        private void RecordAbsence(UnitEntityData unit, MountedControlAbsence absence)
        {
            if (unit?.UniqueId != null && enabled && ArrangementsAccepted) placementIntent.ObserveAbsent(unit.UniqueId, absence);
        }

        // This exact unit instance is one of the current world's player characters: in the party, its detached part, or
        // the reserve (remote) roster the capital moves companions to. Ex-companions, units gone from every roster and an
        // earlier world's objects (a load replaces them, keeping actor ids) are not.
        private static bool IsOnPlayerRoster(UnitEntityData unit)
        {
            var player = Game.Instance?.Player;
            if (player == null || unit == null) return false;
            bool Holds(List<UnitReference> list) => list != null && list.Any(reference => ReferenceEquals(reference.Value, unit));
            return !Holds(player.ExCompanions) &&
                (Holds(player.PartyCharacters) || Holds(player.RemoteCompanions) || Holds(player.DetachedPartyCharacters));
        }

        // The player world each observed unit was first reconciled in. A load replaces the world and its unit objects;
        // an earlier world's object is cleaned up but never authors an arrangement fact for the current world.
        private readonly Dictionary<UnitEntityData, object> observedWorlds =
            new Dictionary<UnitEntityData, object>(new ReferenceIdentity<UnitEntityData>());

        private sealed class ReferenceIdentity<T> : IEqualityComparer<T> where T : class
        {
            public bool Equals(T left, T right) => ReferenceEquals(left, right);
            public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }

        private bool IsCurrentWorldUnit(UnitEntityData unit)
        {
            object world;
            return unit != null && (!observedWorlds.TryGetValue(unit, out world) || ReferenceEquals(world, Game.Instance?.Player));
        }

        // A still-observed unit of the current world that is no longer reconciled (it left the travelling roster): its
        // bar as the player last left it is observed, then the departure is classified (still on the player's roster:
        // temporary; otherwise permanent). Shared by the departure cleanup and the save header, so a save between the
        // roster change and that cleanup carries the same arrangement. Returns false (and records nothing) for an
        // earlier world's object or a character that is present now under another object.
        private bool ObserveDeparture(UnitEntityData prior, IList<UnitEntityData> candidates)
        {
            if (prior?.UniqueId == null || !IsCurrentWorldUnit(prior) ||
                candidates.Any(unit => unit != null && unit.UniqueId == prior.UniqueId)) return false;
            ObserveArrangementOnBar(prior);
            RecordAbsence(prior, IsOnPlayerRoster(prior) ? MountedControlAbsence.Temporary : MountedControlAbsence.Permanent);
            return true;
        }

        // CTRL-03: the native new-ability placement, once per actor, into an empty main-bar slot. Never over a slot;
        // a full main bar records the attempt. Waits for the selected load's restored bindings and the native setting.
        private void TryInitialPlacement(UnitEntityData unit)
        {
            if (mountStateAbility == null || unit?.UniqueId == null || unit.UISettings == null) return;
            var publicControl = unit.Descriptor?.Abilities?.GetAbility(mountStateAbility);
            var settled = !restoringSlots && (PlacementSettled == null || PlacementSettled());
            var autofill = Kingmaker.UI.SettingsUI.SettingsRoot.Instance?.AutofillActionbarSlots?.CurrentValue == true;
            // A save scope's pending lease of a public binding is that binding, about to return to its slot: the
            // one-time placement is already satisfied and must not add a second copy meanwhile.
            if (hotbarSerializationLeases.Any(lease => ReferenceEquals(lease.Unit, unit) &&
                MountedControlSlotPolicy.IsPublicControlKind(ResolveKind(lease.Blueprint))))
            {
                placementRecorded.Add(unit.UniqueId);
                return;
            }
            if (!MountedControlSlotPolicy.ShouldAttemptInitialPlacement(publicControl != null && publicControl.Active,
                placementRecorded.Contains(unit.UniqueId), settled, autofill)) return;
            placementRecorded.Add(unit.UniqueId);
            if (unit.UISettings.Slots != null && unit.UISettings.Slots.Any(IsPublicBindingSlot)) return;
            var empty = new bool[MountedControlSlotPolicy.MainBarSlotCount];
            var before = new string[empty.Length];
            for (var index = 0; index < empty.Length; index++)
            {
                var slot = unit.UISettings.GetSlot(index, unit);
                empty[index] = slot is MechanicActionBarSlotEmpty;
                before[index] = empty[index] ? "" : (slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint?.AssetGuid ?? slot?.GetType().Name ?? "";
            }
            var chosen = MountedControlSlotPolicy.SelectInitialPlacementIndex(empty);
            if (initialPlacements.Count < 32)
                initialPlacements.Add(new InitialPlacementObservation { UnitId = unit.UniqueId, Index = chosen, Frame = Time.frameCount, MainBarBefore = before });
            if (chosen < 0)
            {
                InitialPlacementFullBarCount++;
                // Off the bar, with the companion it was decided for.
                placementIntent.ObserveOffBar(unit.UniqueId, unit.Descriptor?.PetUniqueId);
                logger.Info("Mount / Dismount was not placed: the main action bar is full; unitId=" + unit.UniqueId + ".");
                return;
            }
            unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = publicControl.Data }, chosen);
            // The live arrangement and the companion it was placed for, recorded at once: a direct change of companions
            // before the next observation retires it (RetireOnCompanionLoss).
            placementIntent.ObserveBound(unit.UniqueId, chosen, unit.Descriptor?.PetUniqueId);
            MarkBarChanged(unit);
            InitialPlacementCount++;
            logger.Info("Mount / Dismount placed once on the main action bar: unitId=" + unit.UniqueId + "; slot=" + chosen + ".");
        }

        // Native ActionBarManager rebuilds the visible bar only when the unit's UISettings is dirty or an ability fact
        // changes. A slot KMC writes otherwise stays hidden behind the previous content until the next selection change
        // (run 20261105-finalize-f2-r03). Dirty runs the same native rebuild a selection change does.
        private static void MarkBarChanged(UnitEntityData unit) => unit.UISettings?.SetDirty();

        private bool SlotsHold(MechanicActionBarSlot[] slots, BlueprintAbility blueprint) =>
            slots.Any(slot => ReferenceEquals((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint, blueprint));

        // The unit's own exact stock Charge, unless it is already on the bar (one Charge per bar, not a second copy).
        private static AbilityData ResolveStockChargeForSlot(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            var charge = unit.Descriptor.Abilities.Enumerable.FirstOrDefault(fact => fact.Active &&
                MountedChargeSafetyService.IsExactNativeCharge(fact.Data));
            if (charge == null) return null;
            return slots.Any(slot => ReferenceEquals((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint, charge.Blueprint))
                ? null : charge.Data;
        }

        internal long RetiredPrimarySlotsCleared { get; private set; }

        internal long RetiredPrimaryAutoUseCleared { get; private set; }

        private bool ShouldLease(UnitEntityData unit, NativeMountedControlKind kind)
        {
            var relationship = ResolveControlBoundary(unit, false);
            var mounted = relationship?.State == RelationshipState.Mounted;
            var faulted = relationship?.State == RelationshipState.Faulted;
            var ownerHasSupportedMount = unit?.Descriptor?.Pet != null &&
                SupportedMountedProfiles.IsSupported(unit.Descriptor.Pet) &&
                unit.Descriptor.Pet.Descriptor?.Master.Value == unit;
            return NativeMountedControlPolicy.ShouldLease(
                kind,
                settings.EnableUnsafeMovementExperiment,
                settings.UsePairedTurnControls,
                ownerHasSupportedMount,
                mounted,
                faulted,
                relationship != null && unit == relationship.Rider,
                relationship != null && unit == relationship.Mount,
                settings.EnableMountedCharge,
                RetiredPrimaryHelpersSelected);
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
            foreach (var owner in relationship.Ownership.Owners)
            {
                AddUnique(result, owner.Rider); AddUnique(result, owner.Mount);
            }
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
                observedWorlds.Clear();
            }
        }

        private void CancelPendingNativeControls(CleanupTrigger trigger) => CancelPairControls(null, trigger);

        private void CancelPairControls(GameMountedRelationshipService owner, CleanupTrigger trigger)
        {
            foreach (var unit in CollectCandidateUnits().Where(unit => owner == null ||
                ReferenceEquals(owner.Rider, unit) || ReferenceEquals(owner.Mount, unit) || owner.PairOwner?.Contains(unit) == true))
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

        private void RemoveManagedFacts(UnitEntityData unit, bool cleanupSlots, bool interruptsControl = true)
        {
            var abilities = unit?.Descriptor?.Abilities;
            if (abilities == null)
            {
                return;
            }
            // Every KMC removal of the facts (area unload, save scope, disable, roster departure) interrupts the public
            // control; a remembered slot survives it unless the caller already recorded a permanent loss. An earlier
            // world's object never touches the current world's arrangements.
            if (interruptsControl && IsCurrentWorldUnit(unit)) RecordAbsence(unit, MountedControlAbsence.Temporary);
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

        // legacySchema: the held schema-2 writer only knows kinds 1-4, so the public control is written as the shell it
        // runs now and F2's placement records are omitted.
        // includeAbsentIntent: the owned save archive also carries the remembered slot of a rider whose control is
        // temporarily absent (for example a companion on the capital's reserve roster), as the same kind-6 row and
        // placement record a live control writes, so loading that save returns the control to the player's slot. Only
        // a rider that resolves uniquely among the player's characters now is written (the archive's actor graph
        // requires it on load). Area transfers keep that intent in memory and do not need it.
        internal SavedMountedSlot[] CapturePersistentSlots(bool legacySchema = false, bool includeAbsentIntent = false)
        {
            var result = new List<SavedMountedSlot>();
            if (PersistentRowsSuppressed != null && PersistentRowsSuppressed()) return result.ToArray();
            // A write native completed before an operation threw is the bar's state for this archive too (it may be the
            // first boundary after the exception, before any update or slot operation).
            if (enabled && !disposed) SettleAbortedNativeOperation();
            // The archive header can run before the next control update: classify the bar and the fact first, so a
            // binding native cleanup just invalidated, or a fact that is momentarily unavailable, is captured as the
            // remembered slot rather than omitted, and a live control the player removed stays a removal.
            if (includeAbsentIntent && !legacySchema && enabled) ClassifyArrangementsForCapture();
            var written = CaptureLiveSlots(result, legacySchema);
            if (includeAbsentIntent && !legacySchema && enabled) CaptureAbsentIntent(result, written);
            return result.ToArray();
        }

        private void ClassifyArrangementsForCapture()
        {
            if (mountStateAbility == null) return;
            // An older save's surviving binding on a never-observed reserve rider is its chosen slot (captured below).
            AdmitReserveBindings();
            var candidates = CollectCurrentCandidateUnits();
            // A rider that already left the travelling roster but whose departure cleanup has not run yet: its last bar
            // and its departure classification, exactly as that cleanup will record them.
            ObserveDepartedRiders(candidates);
            foreach (var unit in candidates)
            {
                var id = unit?.UniqueId;
                if (id == null) continue;
                ObserveArrangementOnBar(unit);
                var control = unit.Descriptor?.Abilities?.GetAbility(mountStateAbility);
                if (control != null && control.Active)
                {
                    // A slot holding the public control that the live capture will not write (it writes only callable
                    // live data of a rider that still gets the control: ShouldLease, active fact, exact data, caster) is
                    // the player's slot waiting for repair or for its pet: carried as the remembered slot while the rider
                    // still gets the control or its pet link still names the same companion; restore binds the current
                    // fact. A removed control (no slot) stays a removal.
                    // The same effective index the live writer uses (EffectivePublicIndex / LivePublicRows), and the
                    // callability of that exact slot: when the writer skips it, its index is carried as remembered.
                    // Copies that yield to a free chosen slot write nothing: the interrupted choice is carried.
                    var slots = unit.UISettings?.Slots;
                    bool onlyYieldingCopies;
                    var selected = EffectivePublicIndex(unit, slots, out onlyYieldingCopies);
                    var bound = selected >= 0 || onlyYieldingCopies;
                    var written = selected >= 0 && MountedControlSlotPolicy.IsValidSavedRow(selected, MountedControlSlotPolicy.SavedMountState) &&
                        IsCallableLiveBinding(unit, slots[selected] as MechanicActionBarSlotAbility,
                            ResolveKind((slots[selected] as MechanicActionBarSlotAbility)?.Ability?.Blueprint));
                    if (bound && !written && (ShouldLease(unit, NativeMountedControlKind.MountState) ||
                            placementIntent.Revalidate(id, true, true, unit.Descriptor?.PetUniqueId)))
                        placementIntent.ObserveAbsent(id, MountedControlAbsence.Temporary);
                    continue;
                }
                // The same rule as an update: not leaseable now retires the arrangement only on pet-link evidence.
                var petId = unit.Descriptor?.PetUniqueId;
                if (!ShouldLease(unit, NativeMountedControlKind.MountState) && !placementIntent.Revalidate(id, true, true, petId)) continue;
                placementIntent.ObserveAbsent(id, MountedControlAbsence.Temporary);
            }
        }

        // An absent rider's arrangement: its chosen slot (kind 6) and its placement record (kind 7), or the placement
        // record alone when the player left the control off the bar. Retired first on positive evidence of permanent
        // loss. Every required row is written: more than the archive's 128 rows fails its own validation (the existing
        // bounded failure path) instead of silently dropping a rider's arrangement.
        private void CaptureAbsentIntent(List<SavedMountedSlot> result, HashSet<string> written)
        {
            var characters = Game.Instance?.Player?.AllCharacters;
            if (characters == null) return;
            RevalidateAbsentArrangements(CollectCurrentCandidateUnits());
            foreach (var actorId in placementIntent.ActorIds.OrderBy(id => id, StringComparer.Ordinal).ToArray())
            {
                int index;
                if (written.Contains(actorId) || !placementIntent.TryGetArrangement(actorId, out index) ||
                    !MountedSaveData.ActorId(actorId) ||
                    characters.Count(unit => unit != null && unit.UniqueId == actorId) != 1) continue;
                // The live writer's rows for this actor decide: a public binding it already wrote (kind 6, or a live
                // older alias awaiting migration) is the choice, and one placement record per actor.
                var liveRows = result.Where(saved => saved.ActorId == actorId)
                    .Select(saved => new KeyValuePair<int, int>(saved.Index, saved.Kind)).ToArray();
                // The remembered slot belongs to whatever else is on the bar there now (the occupant keeps it; the
                // shortcut is saved off the bar), never a second row at the same actor/index key.
                var owner = characters.Single(unit => unit != null && unit.UniqueId == actorId);
                var ownerSlots = owner.UISettings?.Slots;
                var occupied = index >= 0 && ownerSlots != null && index < ownerSlots.Length && ownerSlots[index] != null &&
                    !(ownerSlots[index] is MechanicActionBarSlotEmpty) && !IsPublicBindingSlot(ownerSlots[index]);
                // The same decision governs the continuing game: the shortcut is off the bar from now on (clearing the
                // occupant later does not bring it back, exactly as reloading this save would not).
                if (MountedControlSlotPolicy.RememberedSlotTaken(index, liveRows, occupied))
                {
                    placementIntent.GiveUpOccupiedSlot(actorId);
                    placementRecorded.Add(actorId);
                }
                foreach (var row in MountedControlSlotPolicy.AbsentRowsAfterLive(index, liveRows, occupied))
                    result.Add(new SavedMountedSlot { ActorId = actorId, Index = row.Key, Kind = row.Value });
            }
        }

        // Callable live data of an exact KMC binding: the rider still gets that control, its fact is active, and the slot
        // holds that fact's own data cast by the rider. Native hotbars can retain AbilityData after its runtime fact was
        // removed by a real dismount; that is not a live binding and must not require inventing a fact after loading.
        private bool IsCallableLiveBinding(UnitEntityData unit, MechanicActionBarSlotAbility slot, NativeMountedControlKind kind)
        {
            var ability = slot?.Ability;
            if (unit == null || ability == null || kind == NativeMountedControlKind.None || !ShouldLease(unit, kind)) return false;
            var fact = unit.Descriptor?.Abilities?.GetAbility(ability.Blueprint);
            return fact != null && fact.Active && ReferenceEquals(fact.Data, ability) && ability.Caster?.Unit == unit;
        }


        private void ObserveDepartedRiders(List<UnitEntityData> candidates)
        {
            foreach (var prior in observedUnits.ToArray())
                if (prior != null && !candidates.Contains(prior)) ObserveDeparture(prior, candidates);
        }

        // Returns the actors that already have a live public-control slot row (kind 6 or its legacy shell kind).
        private HashSet<string> CaptureLiveSlots(List<SavedMountedSlot> result, bool legacySchema)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            foreach (var unit in CollectCurrentCandidateUnits())
            {
                var slots = unit?.UISettings?.Slots;
                if (slots == null) continue;
                var kinds = slots.Select(slot => ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint)).ToArray();
                // Saved indices are bounded to 0-127 by the archive contract; a binding further out is not persisted.
                for (var i = 0; i < slots.Length && i <= 127; i++)
                {
                    var kind = kinds[i];
                    // The current schema writes the public binding once, chosen below; the held legacy writer keeps its
                    // per-slot shell rows.
                    if (!legacySchema && MountedControlSlotPolicy.IsPublicControlKind(kind)) continue;
                    if (!IsCallableLiveBinding(unit, slots[i] as MechanicActionBarSlotAbility, kind)) continue;
                    var savedKind = legacySchema && kind == NativeMountedControlKind.MountState
                        ? (int)ResolveMountStateIntent(unit) : (int)kind;
                    if (legacySchema && savedKind > MountedControlSlotPolicy.SavedMountPrimary) continue;
                    result.Add(new SavedMountedSlot { ActorId = unit.UniqueId, Index = i, Kind = savedKind });
                }
                if (legacySchema || unit.UniqueId == null) continue;
                // The public binding (canonical first, else the first exact older alias; callable only) and the placement
                // record, accounted separately (MountedControlSlotPolicy.LivePublicRows). A disabled or removed KMC writes
                // no placement record.
                var publicControl = mountStateAbility == null ? null : unit.Descriptor?.Abilities?.GetAbility(mountStateAbility);
                // Only the effective binding is written (EffectivePublicIndex, the decision live repair makes): copies that
                // yield to a free chosen slot write nothing (the remembered writer carries the choice).
                bool onlyYieldingCopies;
                var effective = EffectivePublicIndex(unit, slots, out onlyYieldingCopies);
                foreach (var row in MountedControlSlotPolicy.LivePublicRows(kinds,
                    i => IsCallableLiveBinding(unit, slots[i] as MechanicActionBarSlotAbility, kinds[i]),
                    enabled && placementRecorded.Contains(unit.UniqueId), publicControl != null && publicControl.Active, effective))
                {
                    result.Add(new SavedMountedSlot { ActorId = unit.UniqueId, Index = row.Key, Kind = row.Value });
                    if (row.Value == MountedControlSlotPolicy.SavedMountState) written.Add(unit.UniqueId);
                }
            }
            return written;
        }

        // loadedWorld: the rows of a freshly loaded save, which initialise the arrangements. Otherwise the rows replay
        // a capture made earlier in this same world (area transfer, retained remount): an arrangement observed since
        // (a move, or the player removing the control) wins over them.
        internal void RestorePersistentSlots(SavedMountedSlot[] bindings, bool loadedWorld)
        {
            // Records first, so the placement that Update may otherwise attempt cannot run ahead of this load's bindings.
            foreach (var binding in bindings)
                if (binding.Kind == MountedControlSlotPolicy.SavedPlacementRecord) placementRecorded.Add(binding.ActorId);
            // The first saved public-control slot of each actor is its chosen slot, remembered as returning from an
            // absence: the control goes back there when it is live (now, or when a reserve rider returns), never over
            // anything the slot holds by then. A placement record without a slot is a rider that left the control off
            // the bar. A control already live elsewhere keeps its current slot.
            var seeded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings)
                if (binding.ActorId != null && MountedControlSlotPolicy.RestoreSavedSlot(binding.Kind) == MountedControlSlotRepair.MountState &&
                    seeded.Add(binding.ActorId))
                {
                    placementIntent.Seed(binding.ActorId, binding.Index, loadedWorld);
                    placementRecorded.Add(binding.ActorId);
                }
            foreach (var binding in bindings)
                if (binding.ActorId != null && binding.Kind == MountedControlSlotPolicy.SavedPlacementRecord && seeded.Add(binding.ActorId))
                    placementIntent.Seed(binding.ActorId, MountedControlPlacementIntent.OffBar, loadedWorld);
            restoringSlots = true;
            try
            {
                Update();
                var candidates = CollectCandidateUnits();
                foreach (var binding in bindings)
                {
                    if (binding.Kind == MountedControlSlotPolicy.SavedPlacementRecord) continue;
                    var units = candidates.Where(u => u.UniqueId == binding.ActorId).ToArray();
                    if (units.Length != 1) continue;
                    var unit = units[0];
                    var slots = unit.UISettings?.Slots;
                    if (slots == null || binding.Index >= slots.Length) continue;
                    var current = slots[binding.Index];
                    var repair = MountedControlSlotPolicy.RestoreSavedSlot(binding.Kind);
                    if (repair == MountedControlSlotRepair.MountState) placementRecorded.Add(binding.ActorId);
                    // The public control goes only where the actor's current arrangement puts it (the reconciliation
                    // above already placed it there); an older row never resurrects a moved or removed control.
                    if (repair == MountedControlSlotRepair.MountState && !placementIntent.AllowsSavedSlot(binding.ActorId, binding.Index)) continue;
                    var existingKind = ResolveKind((current as MechanicActionBarSlotAbility)?.Ability?.Blueprint);
                    if (existingKind == (NativeMountedControlKind)binding.Kind) continue;
                    if (current != null && !(current is MechanicActionBarSlotEmpty)) continue;
                    AbilityData restored = null;
                    if (repair == MountedControlSlotRepair.MountState)
                    {
                        // Mount, Dismount and the public control collapse to one public control at the first saved index.
                        var publicControl = mountStateAbility == null ? null : unit.Descriptor.Abilities.GetAbility(mountStateAbility);
                        if (publicControl != null && publicControl.Active && !SlotsHold(slots, mountStateAbility)) restored = publicControl.Data;
                    }
                    else if (repair == MountedControlSlotRepair.StockCharge)
                    {
                        restored = ResolveStockChargeForSlot(unit, slots);
                    }
                    else
                    {
                        var blueprint = EnumerateBlueprints().SingleOrDefault(b => (int)ResolveKind(b) == binding.Kind);
                        var fact = blueprint == null ? null : unit.Descriptor.Abilities.GetAbility(blueprint);
                        if (fact != null && fact.Active) restored = fact.Data;
                    }
                    if (restored == null) continue;
                    unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = restored }, binding.Index);
                    MarkBarChanged(unit);
                    if (repair != MountedControlSlotRepair.Keep && binding.Kind != MountedControlSlotPolicy.SavedMountState)
                    {
                        LegacySlotRepairCount++;
                        logger.Info("Saved KMC control slot migrated: unitId=" + unit.UniqueId + "; slot=" + binding.Index +
                            "; savedKind=" + binding.Kind + "; to=" + restored.Blueprint.name + ".");
                    }
                }
            }
            finally
            {
                restoringSlots = false;
            }
        }

        private void CaptureAndClearManagedHotbarSlots()
        {
            hotbarSerializationLeases.Clear();
            // Reserve and detached riders too: a binding still on such a bar (none once admitted) is never serialized.
            var units = CollectCandidateUnits();
            foreach (var reserve in ReserveRosterUnits(CollectCurrentCandidateUnits())) AddUnique(units, reserve);
            foreach (var unit in units)
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

        internal void EndSaveSerializationScope()
        {
            if (!serializationSuspended)
            {
                return;
            }
            serializationSuspended = false;
            try
            {
                if (enabled && !disposed) Update();
            }
            finally
            {
                ClearSerializationHotbarLeases(true);
            }
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
                // The public control, or an older Mount / Dismount alias standing for it, goes back only while this slot
                // is still the rider's arrangement for its current companion: the update that ended the scope may have
                // retired it (companion removed or replaced).
                var arrangementHolds = !MountedControlSlotPolicy.IsPublicControlKind(ResolveKind(lease.Blueprint)) ||
                    placementIntent.AllowsLeaseRestore(lease.Unit.UniqueId, lease.Index, lease.Unit.Descriptor?.PetUniqueId);
                if (restore && fact != null && fact.Active && arrangementHolds)
                {
                    lease.Unit.UISettings.SetSlot(
                        new MechanicActionBarSlotAbility { Unit = lease.Unit, Ability = fact.Data },
                        lease.Index);
                    // The public control, or the older alias standing for it, is live in its slot again: a removal from now
                    // on is the player's (before the alias's migration too).
                    if (enabled && MountedControlSlotPolicy.IsPublicControlKind(ResolveKind(lease.Blueprint)) && lease.Unit.UniqueId != null)
                        placementIntent.ObserveBound(lease.Unit.UniqueId, lease.Index, lease.Unit.Descriptor?.PetUniqueId);
                }
            }
            hotbarSerializationLeases.Clear();
        }

        private void ClearManagedHotbarSlots(UnitEntityData unit)
        {
            // Every KMC binding of this rider leaves the bar (roster departure, area unload, disable): the copy registry
            // releases its slot objects. The chosen index and pet identity stay in the placement records. (An earlier world's
            // object never touches the current world's registry, which a world change clears anyway.)
            if (IsCurrentWorldUnit(unit)) ForgetCopies(unit?.UniqueId);
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
            var relationship = ResolveControlBoundary(caster, kind == NativeMountedControlKind.MountCompanion) ?? this.relationship;
            var deliveries = lifecycleLedger.Snapshot();
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            return new PendingNativeActivation
            {
                Boundary = relationship,
                Generation = relationship.MountedPairGeneration,
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
            if (pendingActivation == null || pendingActivation.Kind != kind || pendingActivation.CasterId != caster?.UniqueId ||
                !ReferenceEquals(pendingActivation.Boundary, ResolveControlBoundary(caster, kind == NativeMountedControlKind.MountCompanion)) ||
                pendingActivation.Generation != pendingActivation.Boundary.MountedPairGeneration)
            {
                pendingActivation = StartActivation(kind, caster);
            }
            return pendingActivation;
        }

        private readonly List<PendingNativeActivation> awaitingActivations = new List<PendingNativeActivation>();

        private void ObservePendingRiderPrimaryOutcome()
        {
            foreach (var activation in awaitingActivations.ToArray()) ObserveRiderPrimaryOutcome(activation);
        }

        private void ObserveRiderPrimaryOutcome(PendingNativeActivation activation)
        {
            var relationship = activation.Boundary;
            var combat = relationship.CombatOwner;
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
                awaitingActivations.Remove(activation);
                if (ReferenceEquals(pendingActivation, activation)) pendingActivation = null;
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
            awaitingActivations.Remove(activation);
            if (ReferenceEquals(pendingActivation, activation)) pendingActivation = null;
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

            var relationship = activation.Boundary;
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
                case NativeMountedControlKind.MountedCharge: return MountedChargeAbilityGuid;
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
            var relationship = ResolveControlBoundary(caster, kind == NativeMountedControlKind.MountCompanion);
            var playerAction = relationship?.PlayerActions;
            var combat = relationship?.CombatOwner;
            if (relationship == null) return "The exact mounted owner is no longer available.";
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
            if (kind == NativeMountedControlKind.MountedCharge)
            {
                return combat.DescribeMountedChargeTargetRejection(caster, target);
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

        // A missing blueprint is never a KMC control, even while a control field is still unregistered (null).
        private NativeMountedControlKind ResolveKind(BlueprintAbility ability) =>
            MountedControlSlotPolicy.ResolveExactKind(ability, mountAbility, dismountAbility, riderPrimaryAbility,
                mountPrimaryAbility, mountedChargeAbility, mountStateAbility);

        private IEnumerable<BlueprintAbility> EnumerateBlueprints()
        {
            if (mountAbility != null) { yield return mountAbility; }
            if (dismountAbility != null) { yield return dismountAbility; }
            if (riderPrimaryAbility != null) { yield return riderPrimaryAbility; }
            if (mountPrimaryAbility != null) { yield return mountPrimaryAbility; }
            if (mountedChargeAbility != null) { yield return mountedChargeAbility; }
            if (mountStateAbility != null) { yield return mountStateAbility; }
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
            localizedPack = pack;
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
                if (relationship.World != null) relationship.World.PairDismounting += CancelPairControls;
                else relationship.Dismounting += CancelPendingNativeControls;
                subscribed = true;
            }
        }

        private void EnsureUnsubscribed()
        {
            if (subscribed)
            {
                EventBus.Unsubscribe(this);
                if (relationship.World != null) relationship.World.PairDismounting -= CancelPairControls;
                else relationship.Dismounting -= CancelPendingNativeControls;
                subscribed = false;
            }
        }

        private void RememberUnit(UnitEntityData unit)
        {
            if (unit != null && !observedUnits.Contains(unit))
            {
                observedUnits.Add(unit);
                observedWorlds[unit] = Game.Instance?.Player;
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
            public GameMountedRelationshipService Boundary;
            public long Generation;
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
