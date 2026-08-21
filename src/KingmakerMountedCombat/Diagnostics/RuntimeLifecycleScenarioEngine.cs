using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Executes bounded relationship-lifecycle and transient player-action rows. Every action is
    /// advanced by Update so cleanup is observed on a later game frame rather
    /// than being accepted from the transition return value alone.
    /// </summary>
    internal sealed class RuntimeLifecycleScenarioEngine : IDisposable
    {
        private const double RowTimeoutSeconds = 15.0d;
        private const double SuiteTimeoutSeconds = 120.0d;
        private const string EvidenceFileName = "lifecycle-scenario-evidence.jsonl";
        private const string DirectInvocationClaimLimit =
            "Direct service/handler invocation only; native EventBus/UMM delivery was not exercised.";
        private const string PlayerActionClaimLimit =
            "Runtime player-action controller invocation; Unity OnGUI button delivery remains separately observed.";
        private const string NativeIncapacitationClaimLimit =
            "Real UnitEntityData.Damage mutation followed by stock UnitLifeController/EventBus delivery; no direct life-state or lifecycle-handler invocation.";

        private static readonly JsonSerializerSettings EvidenceJsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private static readonly string[] SuiteRows =
        {
            "mounted-pair-create-and-clear",
            "mounted-pair-double-mount-rejected",
            "mounted-pair-invalid-pair-rejected",
            "mounted-pair-cleanup-idempotent",
            "mounted-pair-death-cleanup",
            "mounted-pair-combat-start-cleanup",
            "mounted-pair-area-unload-cleanup",
            "mounted-pair-mod-disable-cleanup"
        };

        private static readonly string[] PlayerActionRows =
        {
            "player-action-availability",
            "mount-dismount-user-flow"
        };

        private static readonly string[] CombatLifecycleRows =
        {
            "mounted-pair-combat-start-retained",
            "mounted-pair-combat-end-retained",
            "mounted-pair-rider-death-cleanup",
            "mounted-pair-mount-death-cleanup",
            "mounted-pair-rider-incapacitated-cleanup",
            "mounted-pair-mount-incapacitated-cleanup",
            "mounted-pair-companion-removal-cleanup",
            "mounted-pair-view-destroyed-cleanup",
            "mounted-pair-exception-cleanup"
        };

        private static readonly string[] NativeIncapacitationRows =
        {
            "mounted-pair-rider-native-incapacitated-cleanup",
            "mounted-pair-mount-native-incapacitated-cleanup"
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly MountedPlayerActionController playerAction;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch suiteClock = new Stopwatch();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly string evidencePath;
        private readonly string dllSha256;
        private readonly string dllMvid;

        private IReadOnlyList<string> selectedRows;
        private AssertionRecorder assertions;
        private PairSnapshot snapshot;
        private PairSnapshot evidenceSnapshot;
        private TransitionResult lastCleanupTransition;
        private string currentRow;
        private string lastEvidenceRow;
        private int rowIndex;
        private int frameNumber;
        private int cleanupFrame;
        private long evidenceSequence;
        private EngineStep step;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool evidenceCreated;
        private bool evidenceFailed;
        private string evidenceFailureMessage;
        private bool evidenceFinalized;
        private bool cleanupFrameEvidenceWritten;
        private bool attachmentLeaseAcquiredThisRow;
        private bool poseLeaseAcquiredThisRow;
        private long lifecycleDeliveryBaselineSequence;
        private BoundaryExerciseEvidence boundaryExercise;
        private UnitEntityData incapacitationActor;
        private ActorLifeTransitionEvidence actorLifeTransition;

        public RuntimeLifecycleScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedLifecycleSubscriber lifecycle,
            MountedPlayerActionController playerAction,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            evidencePath = Path.Combine(request.EvidenceRoot, EvidenceFileName);
            var assembly = typeof(Main).Assembly;
            dllSha256 = ComputeSha256(assembly.Location);
            dllMvid = assembly.ManifestModule.ModuleVersionId.ToString();
        }

        public bool IsCompleted => completed;

        public IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        public IReadOnlyList<string> Errors => errors;

        internal static bool SupportsScenario(string scenario)
        {
            return SelectRows(scenario) != null;
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Lifecycle scenario engine has already started.");
            }

            selectedRows = SelectRows(request.Scenario);
            started = true;
            if (selectedRows == null)
            {
                errors.Add("Scenario is not a lifecycle-suite or exact lifecycle mission row: " + request.Scenario + ".");
                completed = true;
                return;
            }

            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
            suiteClock.Start();
            step = EngineStep.BeginRow;
            logger.Info("Lifecycle runtime engine started for " + request.Scenario + ".");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started)
            {
                throw new InvalidOperationException("Lifecycle scenario engine must be started before Update.");
            }
            if (completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                if (suiteClock.Elapsed.TotalSeconds > SuiteTimeoutSeconds)
                {
                    FailCurrent("Lifecycle suite exceeded its " + SuiteTimeoutSeconds + " second monotonic deadline.");
                    CompleteRemainingAsNotRun("Lifecycle suite monotonic deadline expired.");
                    Complete();
                    return;
                }
                if (currentRow != null && rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds &&
                    step != EngineStep.AwaitCleanupFrame && step != EngineStep.AwaitFirstIdempotentCleanupFrame)
                {
                    FailCurrent("Lifecycle row exceeded its " + RowTimeoutSeconds + " second monotonic deadline.");
                    RequestCleanup(CleanupTrigger.Exception);
                    return;
                }

                Advance();
            }
            catch (Exception exception)
            {
                logger.Exception("Lifecycle runtime row threw", exception);
                FailCurrent(exception.GetType().Name + ": " + exception.Message);
                RequestCleanup(CleanupTrigger.Exception);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (started && !completed)
            {
                errors.Add("Lifecycle engine was disposed before completing its selected rows.");
            }

            TransitionResult teardownCleanup = null;
            try
            {
                if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
                {
                    teardownCleanup = relationship.Dismount(CleanupTrigger.ProcessTeardown);
                    lastCleanupTransition = teardownCleanup;
                    if (!teardownCleanup.Succeeded || teardownCleanup.MovementAuthorityResidual || teardownCleanup.PresentationResidual)
                    {
                        errors.Add("Process-teardown cleanup retained mounted runtime residue.");
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add("Process-teardown cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Lifecycle runtime teardown cleanup threw", exception);
            }
            finally
            {
                FinalizeEvidence(teardownCleanup ?? lastCleanupTransition);
                RestoreSettings();
                suiteClock.Stop();
                rowClock.Stop();
                disposed = true;
            }
        }

        private void Advance()
        {
            switch (step)
            {
                case EngineStep.BeginRow:
                    BeginRow();
                    break;
                case EngineStep.AwaitMountedFrame:
                    ExerciseMountedRow();
                    break;
                case EngineStep.AwaitNativeIncapacitation:
                    AwaitNativeIncapacitation();
                    break;
                case EngineStep.AwaitFirstIdempotentCleanupFrame:
                    VerifyFirstIdempotentCleanupAndRepeat();
                    break;
                case EngineStep.AwaitCleanupFrame:
                    VerifyCleanupAndFinishRow();
                    break;
                default:
                    throw new InvalidOperationException("Unexpected lifecycle engine step: " + step + ".");
            }
        }

        private void BeginRow()
        {
            if (rowIndex >= selectedRows.Count)
            {
                Complete();
                return;
            }

            currentRow = selectedRows[rowIndex];
            lastEvidenceRow = currentRow;
            assertions = new AssertionRecorder();
            snapshot = null;
            lastCleanupTransition = null;
            cleanupFrameEvidenceWritten = false;
            attachmentLeaseAcquiredThisRow = false;
            poseLeaseAcquiredThisRow = false;
            boundaryExercise = null;
            incapacitationActor = null;
            actorLifeTransition = null;
            lifecycleDeliveryBaselineSequence = lifecycle.SnapshotNativeDeliveries()
                .Select(record => record.Sequence)
                .DefaultIfEmpty(0L)
                .Max();
            rowClock.Restart();
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began the row Unmounted.",
                "Relationship began the row in " + relationship.State + " rather than Unmounted.");
            if (relationship.State != RelationshipState.Unmounted)
            {
                TryWriteEvidence("pre-mount", null, null);
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            UnitEntityData rider;
            UnitEntityData mount;
            string resolutionError;
            var resolved = relationship.TryResolveAutomationPair(out rider, out mount, out resolutionError);
            assertions.Check(resolved,
                "Exact automation pair resolved.",
                "Exact automation pair did not resolve: " + (resolutionError ?? "unknown error"));
            if (!resolved)
            {
                TryWriteEvidence("pre-mount", null, null);
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            string snapshotError;
            snapshot = PairSnapshot.TryCreate(rider, mount, out snapshotError);
            assertions.Check(snapshot != null,
                "Pre-mount stock movement state was captured.",
                "Pre-mount stock movement state could not be captured: " + (snapshotError ?? "unknown error"));
            if (snapshot == null)
            {
                TryWriteEvidence("pre-mount", null, null);
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }
            if (IsNativeIncapacitationRow(currentRow))
            {
                var actorIsRider = string.Equals(
                    currentRow,
                    "mounted-pair-rider-native-incapacitated-cleanup",
                    StringComparison.Ordinal);
                incapacitationActor = actorIsRider ? snapshot.Rider : snapshot.Mount;
                var hitPoints = incapacitationActor == null ? 0 : (int)incapacitationActor.Stats.HitPoints;
                actorLifeTransition = ActorLifeTransitionEvidence.Before(
                    actorIsRider ? "rider" : "mount",
                    incapacitationActor,
                    hitPoints + 1);
            }
            evidenceSnapshot = snapshot;
            if (!TryWriteEvidence("pre-mount", null, null))
            {
                FinishCurrentRow();
                return;
            }
            if (!AssertPreMountBaseline())
            {
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            if (string.Equals(currentRow, "player-action-availability", StringComparison.Ordinal))
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, false, false);
                var available = playerAction.GetAvailability();
                assertions.Check(available.IsVisible,
                    "Transient player action was visible for the selected eligible rider.",
                    "Transient player action was hidden for the selected eligible rider.");
                assertions.Check(available.IsEnabled && available.Action == MountedPlayerActionKind.Mount,
                    "Transient player action exposed enabled Mount state.",
                    "Transient player action was not an enabled Mount action: " + available.Feedback);
                assertions.Check(available.UnavailableReasons.Count == 0,
                    "Eligible live pair exposed no rejection reason.",
                    "Eligible live pair exposed rejection reasons: " + available.Feedback);

                settings.EnableUnsafeMovementExperiment = false;
                var disabled = playerAction.GetAvailability();
                assertions.Check(disabled.IsVisible && !disabled.IsEnabled,
                    "Disabled private-alpha feature retained visible eligibility feedback without an executable action.",
                    "Disabled private-alpha feature did not fail closed with visible feedback.");
                assertions.Check(disabled.Feedback.IndexOf("Enable the private-alpha", StringComparison.Ordinal) >= 0,
                    "Disabled feature explained the exact enablement requirement.",
                    "Disabled feature did not expose its exact enablement requirement: " + disabled.Feedback);
                settings.EnableUnsafeMovementExperiment = true;
                RequestCleanup(CleanupTrigger.Manual);
                return;
            }

            if (string.Equals(currentRow, "mounted-pair-invalid-pair-rejected", StringComparison.Ordinal))
            {
                var invalid = relationship.RejectSyntheticInvalidPairForAutomation();
                assertions.Check(!invalid.Succeeded,
                    "Synthetic same-unit pair was rejected by the live relationship coordinator.",
                    "Synthetic same-unit pair was unexpectedly accepted.");
                assertions.Check(relationship.State == RelationshipState.Unmounted,
                    "Invalid-pair rejection preserved Unmounted state.",
                    "Invalid-pair rejection changed relationship state to " + relationship.State + ".");
                RequestCleanup(CleanupTrigger.Manual);
                return;
            }

            TransitionResult mounted;
            if (string.Equals(currentRow, "mount-dismount-user-flow", StringComparison.Ordinal))
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, false, false);
                var activated = playerAction.Activate();
                mounted = relationship.LastTransition;
                assertions.Check(activated,
                    "Transient player action activated the eligible live pair.",
                    "Transient player action did not activate the eligible live pair: " + playerAction.LastFeedback);
            }
            else
            {
                mounted = relationship.MountAutomationPair();
            }
            assertions.Check(mounted.Succeeded,
                "Valid automation pair mounted.",
                "Valid automation pair mount failed: " + FormatTransitionErrors(mounted));
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship entered Mounted.",
                "Relationship state after mount was " + relationship.State + ".");
            if (!mounted.Succeeded || relationship.State != RelationshipState.Mounted)
            {
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            AssertMountedAuthority();
            attachmentLeaseAcquiredThisRow = relationship.Runtime.PresentationAttachmentLeaseActive;
            poseLeaseAcquiredThisRow = relationship.Runtime.PoseConfigured;
            step = EngineStep.AwaitMountedFrame;
        }

        private bool AssertPreMountBaseline()
        {
            var exact = snapshot.RiderStockAgent.enabled && snapshot.MountStockAgent.enabled &&
                !snapshot.RiderStockAgent.AvoidanceDisabled && !snapshot.MountStockAgent.AvoidanceDisabled &&
                !snapshot.RiderView.ForbidRotation && !snapshot.MountView.ForbidRotation &&
                snapshot.RiderOverride == null && snapshot.MountOverride == null &&
                snapshot.RiderOverrideComponentCount == 0 && snapshot.MountOverrideComponentCount == 0 &&
                snapshot.RiderPoseComponentCount == 0 && snapshot.MountPoseComponentCount == 0;
            assertions.Check(exact,
                "Exact clean stock-agent, avoidance, rotation, override, movement-component, and pose-component baseline was captured.",
                "Pre-mount pair contained disabled stock authority or retained avoidance, rotation, override, movement-component, or pose-component state.");
            return exact;
        }

        private void ExerciseMountedRow()
        {
            TryWriteEvidence("mounted-next-frame", null, null);
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship remained Mounted for at least one game frame.",
                "Relationship did not remain Mounted through the next game frame; observed " + relationship.State + ".");
            if (relationship.State != RelationshipState.Mounted)
            {
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            assertions.Check(relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy &&
                    relationship.Runtime.PoseFrameApplied && relationship.Runtime.PoseApplicationFrameCount > 0,
                "Exact supported rider pose was applied after ordinary animation on the next mounted frame.",
                "Supported rider pose was not healthy and active on the next mounted frame: " +
                    (relationship.Runtime.PoseFailure ?? "no adapter failure detail"));

            if (string.Equals(currentRow, "mounted-pair-create-and-clear", StringComparison.Ordinal))
            {
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mount-dismount-user-flow", StringComparison.Ordinal))
            {
                var availability = playerAction.GetAvailability();
                assertions.Check(availability.IsVisible && availability.IsEnabled &&
                        availability.Action == MountedPlayerActionKind.Dismount &&
                        string.Equals(availability.Label, "Dismount", StringComparison.Ordinal),
                    "Mounted player action became an enabled Dismount action.",
                    "Mounted player action did not become enabled Dismount: " + availability.Feedback);
                var selected = SelectionManager.Instance?.SelectedUnits;
                assertions.Check(selected != null && selected.Count == 1 && selected[0] == snapshot.Rider,
                    "Player action normalized selection to the rider.",
                    "Player action did not retain exactly the rider as selected.");
                var activated = playerAction.Activate();
                lastCleanupTransition = relationship.LastTransition;
                assertions.Check(activated,
                    "Dismount player action completed through the relationship service.",
                    "Dismount player action failed: " + playerAction.LastFeedback);
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Manual),
                    "Dismount player action retained the Manual cleanup trigger.",
                    "Dismount player action did not retain Manual cleanup: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-double-mount-rejected", StringComparison.Ordinal))
            {
                var secondMount = relationship.MountAutomationPair();
                assertions.Check(!secondMount.Succeeded,
                    "Second mount request was rejected.",
                    "Second mount request unexpectedly succeeded.");
                assertions.Check(relationship.State == RelationshipState.Mounted,
                    "Rejected second mount preserved the original Mounted relationship.",
                    "Rejected second mount changed relationship state to " + relationship.State + ".");
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-cleanup-idempotent", StringComparison.Ordinal))
            {
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
                cleanupFrame = frameNumber;
                step = EngineStep.AwaitFirstIdempotentCleanupFrame;
            }
            else if (string.Equals(currentRow, "mounted-pair-death-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandleUnitDeath(snapshot.Rider);
                lastCleanupTransition = relationship.LastTransition;
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Death),
                    "Death lifecycle handler requested the Death cleanup trigger.",
                    "Death lifecycle handler did not report the Death cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-combat-start-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandlePartyCombatStateChanged(true);
                lastCleanupTransition = relationship.LastTransition;
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.CombatStarted),
                    "Combat lifecycle handler requested the CombatStarted cleanup trigger.",
                    "Combat lifecycle handler did not report the CombatStarted cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-area-unload-cleanup", StringComparison.Ordinal))
            {
                lifecycle.OnAreaBeginUnloading();
                lastCleanupTransition = relationship.LastTransition;
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.AreaUnloading),
                    "Area lifecycle handler requested the AreaUnloading cleanup trigger.",
                    "Area lifecycle handler did not report the AreaUnloading cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-mod-disable-cleanup", StringComparison.Ordinal))
            {
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.ModDisabled), CleanupTrigger.ModDisabled);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-combat-start-retained", StringComparison.Ordinal))
            {
                lifecycle.HandlePartyCombatStateChanged(true);
                CaptureBoundaryExercise("pair", "IPartyCombatHandler.HandlePartyCombatStateChanged(true)");
                assertions.Check(relationship.State == RelationshipState.Mounted,
                    "Combat-start delivery retained the valid mounted pair.",
                    "Combat-start delivery changed the valid pair to " + relationship.State + ".");
                AssertObservedBoundary(
                    NativeLifecycleBoundary.CombatStarted,
                    "IPartyCombatHandler.HandlePartyCombatStateChanged(true)",
                    RelationshipState.Mounted,
                    RelationshipState.Mounted,
                    null,
                    false);
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-combat-end-retained", StringComparison.Ordinal))
            {
                lifecycle.HandlePartyCombatStateChanged(true);
                lifecycle.HandlePartyCombatStateChanged(false);
                CaptureBoundaryExercise("pair", "IPartyCombatHandler.HandlePartyCombatStateChanged(true/false)");
                assertions.Check(relationship.State == RelationshipState.Mounted,
                    "Combat-end delivery cancelled combat work without dismounting the valid pair.",
                    "Combat-end delivery changed the valid pair to " + relationship.State + ".");
                AssertObservedBoundary(
                    NativeLifecycleBoundary.CombatStarted,
                    "IPartyCombatHandler.HandlePartyCombatStateChanged(true)",
                    RelationshipState.Mounted,
                    RelationshipState.Mounted,
                    null,
                    false);
                AssertObservedBoundary(
                    NativeLifecycleBoundary.CombatEnded,
                    "IPartyCombatHandler.HandlePartyCombatStateChanged(false)",
                    RelationshipState.Mounted,
                    RelationshipState.Mounted,
                    null,
                    false);
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-rider-death-cleanup", StringComparison.Ordinal) ||
                string.Equals(currentRow, "mounted-pair-mount-death-cleanup", StringComparison.Ordinal))
            {
                var actorIsRider = string.Equals(currentRow, "mounted-pair-rider-death-cleanup", StringComparison.Ordinal);
                var actor = actorIsRider ? snapshot.Rider : snapshot.Mount;
                lifecycle.HandleUnitDeath(actor);
                lastCleanupTransition = relationship.LastTransition;
                CaptureBoundaryExercise(actorIsRider ? "rider" : "mount", "IUnitHandler.HandleUnitDeath");
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Death),
                    "Exact pair-unit death delivery completed Death cleanup.",
                    "Pair-unit death delivery did not complete Death cleanup: " + relationship.LastResult);
                AssertObservedBoundary(
                    NativeLifecycleBoundary.UnitDeath,
                    "IUnitHandler.HandleUnitDeath",
                    RelationshipState.Mounted,
                    RelationshipState.Unmounted,
                    CleanupTrigger.Death,
                    true);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-rider-incapacitated-cleanup", StringComparison.Ordinal) ||
                string.Equals(currentRow, "mounted-pair-mount-incapacitated-cleanup", StringComparison.Ordinal))
            {
                var actorIsRider = string.Equals(currentRow, "mounted-pair-rider-incapacitated-cleanup", StringComparison.Ordinal);
                lastCleanupTransition = relationship.Dismount(CleanupTrigger.Incapacitated);
                CaptureBoundaryExercise(actorIsRider ? "rider" : "mount", "relationship.Dismount(Incapacitated)");
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Incapacitated),
                    "Direct fail-safe incapacitation boundary completed exact cleanup.",
                    "Direct fail-safe incapacitation boundary did not complete exact cleanup: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (IsNativeIncapacitationRow(currentRow))
            {
                var actorIsRider = string.Equals(
                    currentRow,
                    "mounted-pair-rider-native-incapacitated-cleanup",
                    StringComparison.Ordinal);
                incapacitationActor = actorIsRider ? snapshot.Rider : snapshot.Mount;
                var state = incapacitationActor?.Descriptor?.State;
                var hitPoints = incapacitationActor == null ? 0 : (int)incapacitationActor.Stats.HitPoints;
                var constitution = incapacitationActor == null ? 0 : (int)incapacitationActor.Stats.Constitution;
                var damageBefore = incapacitationActor?.Damage ?? 0;
                var requestedDamage = hitPoints + 1;
                if (actorLifeTransition == null)
                {
                    actorLifeTransition = ActorLifeTransitionEvidence.Before(
                        actorIsRider ? "rider" : "mount",
                        incapacitationActor,
                        requestedDamage);
                }
                assertions.Check(state != null && state.IsConscious && !state.IsDead && !state.IsFinallyDead,
                    "Exact pair actor began the native incapacitation probe conscious and alive.",
                    "Pair actor was not conscious and alive before the native incapacitation probe.");
                assertions.Check(hitPoints > 0 && constitution > 1 && damageBefore < hitPoints &&
                        requestedDamage > hitPoints && requestedDamage < hitPoints + constitution,
                    "Requested damage is inside the exact stock unconscious-but-not-dead band.",
                    "Requested damage was outside the stock unconscious band: damage=" + damageBefore +
                        ";HP=" + hitPoints + ";Constitution=" + constitution +
                        ";requested=" + requestedDamage + ".");
                if (assertions.FailureCount != 0)
                {
                    RequestCleanup(CleanupTrigger.Exception);
                    return;
                }

                incapacitationActor.Damage = requestedDamage;
                actorLifeTransition.MutationIssued = true;
                step = EngineStep.AwaitNativeIncapacitation;
            }
            else if (string.Equals(currentRow, "mounted-pair-companion-removal-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandleCompanionRemoved(snapshot.Mount);
                lastCleanupTransition = relationship.LastTransition;
                CaptureBoundaryExercise("mount", "IPartyHandler.HandleCompanionRemoved");
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.CompanionInvalidated),
                    "Companion-removal delivery completed CompanionInvalidated cleanup.",
                    "Companion-removal delivery did not complete exact cleanup: " + relationship.LastResult);
                AssertObservedBoundary(
                    NativeLifecycleBoundary.PartyRemoved,
                    "IPartyHandler.HandleCompanionRemoved",
                    RelationshipState.Mounted,
                    RelationshipState.Unmounted,
                    CleanupTrigger.CompanionInvalidated,
                    true);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-view-destroyed-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandleUnitDestroyed(snapshot.Rider);
                lastCleanupTransition = relationship.LastTransition;
                CaptureBoundaryExercise("rider", "IUnitHandler.HandleUnitDestroyed");
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.ViewDetached),
                    "Pair-view destruction delivery completed ViewDetached cleanup.",
                    "Pair-view destruction delivery did not complete exact cleanup: " + relationship.LastResult);
                AssertObservedBoundary(
                    NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed,
                    "IUnitHandler.HandleUnitDestroyed",
                    RelationshipState.Mounted,
                    RelationshipState.Unmounted,
                    CleanupTrigger.ViewDetached,
                    true);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-exception-cleanup", StringComparison.Ordinal))
            {
                lastCleanupTransition = relationship.Dismount(CleanupTrigger.Exception);
                CaptureBoundaryExercise("pair", "relationship.Dismount(Exception)");
                assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Exception),
                    "Exception recovery completed exact relationship cleanup.",
                    "Exception recovery did not complete exact cleanup: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else
            {
                FailCurrent("Unsupported lifecycle row reached mounted execution: " + currentRow + ".");
                RequestCleanup(CleanupTrigger.Exception);
            }
        }

        private void VerifyFirstIdempotentCleanupAndRepeat()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }

            try
            {
                AssertUnmountedAndRestored();
            }
            catch (Exception exception)
            {
                FailCurrent("Post-cleanup verification threw " + exception.GetType().Name + ": " + exception.Message);
                WriteCleanupFrameEvidenceOnce();
                FinishCurrentRow();
                return;
            }
            WriteCleanupFrameEvidenceOnce();
            var repeated = relationship.Dismount(CleanupTrigger.Manual);
            lastCleanupTransition = repeated;
            assertions.Check(repeated.Succeeded,
                "Repeated cleanup succeeded.",
                "Repeated cleanup failed: " + FormatTransitionErrors(repeated));
            assertions.Check(repeated.State == RelationshipState.Unmounted,
                "Repeated cleanup remained Unmounted.",
                "Repeated cleanup ended in " + repeated.State + ".");
            assertions.Check(!repeated.MovementAuthorityResidual && !repeated.PresentationResidual,
                "Repeated cleanup reported no owned residue.",
                "Repeated cleanup reported movement or presentation residue.");
            AwaitCleanupFrame();
        }

        private void VerifyCleanupAndFinishRow()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }

            try
            {
                AssertUnmountedAndRestored();
            }
            catch (Exception exception)
            {
                FailCurrent("Post-cleanup verification threw " + exception.GetType().Name + ": " + exception.Message);
            }
            WriteCleanupFrameEvidenceOnce();
            FinishCurrentRow();
        }

        private void AssertMountedAuthority()
        {
            assertions.Check(snapshot.RiderView.AgentASP == snapshot.RiderStockAgent,
                "Rider retained its exact stock-agent object.",
                "Rider stock-agent object changed while mounted.");
            assertions.Check(!snapshot.RiderStockAgent.enabled,
                "Rider stock movement agent was disabled.",
                "Rider stock movement agent remained enabled while mounted.");
            assertions.Check(snapshot.RiderStockAgent.AvoidanceDisabled,
                "Rider stock avoidance was disabled under the owned lease.",
                "Rider stock avoidance was not disabled while mounted.");
            assertions.Check(snapshot.RiderView.AgentOverride == relationship.Runtime.MovementAgent && relationship.Runtime.MovementAgent != null,
                "Rider installed only the owned movement override.",
                "Rider did not expose the owned movement override.");
            assertions.Check(snapshot.RiderView.GetComponents<RiderMovementAgent>().Length == snapshot.RiderOverrideComponentCount + 1,
                "Rider added exactly one owned RiderMovementAgent component.",
                "Mounted rider did not contain exactly one additional RiderMovementAgent component.");
            assertions.Check(snapshot.RiderView.ForbidRotation,
                "Rider rotation was held by the owned mounted lease.",
                "Rider rotation was not held while mounted.");
            assertions.Check(snapshot.MountView.AgentASP == snapshot.MountStockAgent && snapshot.MountStockAgent.enabled,
                "Mount stock movement agent remained authoritative.",
                "Mount stock movement agent was changed or disabled.");
            assertions.Check(snapshot.MountStockAgent.AvoidanceDisabled == snapshot.MountAvoidanceWasDisabled,
                "Mount stock avoidance state remained unchanged.",
                "Mount stock avoidance state changed while mounted.");
            assertions.Check(ReferenceEquals(snapshot.MountView.AgentOverride, snapshot.MountOverride) && snapshot.MountOverride == null,
                "Mount retained no movement override.",
                "Mount gained or changed a movement override while mounted.");
            assertions.Check(snapshot.MountView.GetComponents<RiderMovementAgent>().Length == snapshot.MountOverrideComponentCount,
                "Mount RiderMovementAgent component count remained unchanged.",
                "Mount RiderMovementAgent component count changed while mounted.");
            assertions.Check(snapshot.MountView.ForbidRotation == snapshot.MountForbidRotationWasEnabled,
                "Mount rotation state remained unchanged.",
                "Mount rotation state changed while mounted.");
            assertions.Check(relationship.Runtime.PresentationAttachmentLeaseActive &&
                    relationship.Runtime.RiderParentMatchesAttachment &&
                    relationship.Runtime.HasPresentationAttachmentResidue &&
                    !relationship.Runtime.PresentationAttachmentRestoreVerified &&
                    string.Equals(relationship.Runtime.PresentationAttachmentParentName, "KMC_RiderPositionAnchor", StringComparison.Ordinal) &&
                    string.Equals(relationship.Runtime.PresentationSourceAnchorName, "Spine", StringComparison.Ordinal) &&
                    string.Equals(relationship.Runtime.PresentationAttachmentRiskState, "active and internally consistent", StringComparison.Ordinal),
                "Rider owned one internally consistent scoped position-attachment lease.",
                "Rider scoped position-attachment lease was absent, restored early, or internally inconsistent.");
            assertions.Check(relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy &&
                    relationship.Runtime.PoseComponentCount == snapshot.RiderPoseComponentCount + 1 &&
                    relationship.Runtime.PoseBoneCount == 7 &&
                    string.Equals(relationship.Runtime.PoseProfileId, "medium-humanoid-mammoth-v1", StringComparison.Ordinal),
                "Rider owned exactly one healthy seven-bone Medium-humanoid Mammoth pose adapter.",
                "Rider pose adapter count, health, profile, or typed bone inventory was not exact.");
        }

        private void AssertCleanupTransition(TransitionResult result, CleanupTrigger expectedTrigger)
        {
            lastCleanupTransition = result;
            assertions.Check(result.Succeeded,
                expectedTrigger + " cleanup transition succeeded.",
                expectedTrigger + " cleanup transition failed: " + FormatTransitionErrors(result));
            assertions.Check(result.Trigger == expectedTrigger,
                expectedTrigger + " cleanup trigger was retained.",
                "Cleanup returned trigger " + (result.Trigger.HasValue ? result.Trigger.Value.ToString() : "null") + " rather than " + expectedTrigger + ".");
            assertions.Check(!result.MovementAuthorityResidual && !result.PresentationResidual,
                expectedTrigger + " cleanup reported no owned residue.",
                expectedTrigger + " cleanup reported movement or presentation residue.");
        }

        private void AssertUnmountedAndRestored()
        {
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship was Unmounted on the post-cleanup frame.",
                "Post-cleanup relationship state was " + relationship.State + ".");
            assertions.Check(relationship.Rider == null && relationship.Mount == null,
                "Prepared rider and mount references were released.",
                "Prepared rider or mount reference remained after cleanup.");
            assertions.Check(relationship.Runtime.MovementAgent == null,
                "Owned rider override reference was released.",
                "Owned rider override reference remained after cleanup.");

            if (snapshot == null)
            {
                assertions.Fail("No retained pre-mount pair snapshot was available for residue checks.");
                return;
            }

            var riderViewAlive = snapshot.RiderView != null;
            var mountViewAlive = snapshot.MountView != null;
            assertions.Check(riderViewAlive && mountViewAlive,
                "Retained rider and mount views remained alive for cleanup verification.",
                "Rider or mount view was destroyed before cleanup verification.");
            var currentRiderView = snapshot.Rider == null ? null : snapshot.Rider.View;
            var currentMountView = snapshot.Mount == null ? null : snapshot.Mount.View;
            assertions.Check(riderViewAlive && mountViewAlive &&
                    currentRiderView != null && currentMountView != null &&
                    ReferenceEquals(currentRiderView, snapshot.RiderView) &&
                    ReferenceEquals(currentMountView, snapshot.MountView),
                "Exact rider and mount views remained attached during the bounded lifecycle row.",
                "Rider or mount view identity changed during the bounded lifecycle row.");

            var riderStockAgentAlive = snapshot.RiderStockAgent != null;
            assertions.Check(riderStockAgentAlive,
                "Retained rider stock agent remained alive for cleanup verification.",
                "Rider stock agent was destroyed before cleanup verification.");
            if (riderViewAlive && riderStockAgentAlive)
            {
                assertions.Check(ReferenceEquals(snapshot.RiderView.AgentASP, snapshot.RiderStockAgent),
                    "Exact rider stock-agent reference was restored.",
                    "Rider stock-agent reference changed after cleanup.");
                assertions.Check(snapshot.RiderStockAgent.enabled == snapshot.RiderAgentWasEnabled,
                    "Rider stock-agent enabled flag was restored.",
                    "Rider stock-agent enabled flag was not restored.");
                assertions.Check(snapshot.RiderStockAgent.AvoidanceDisabled == snapshot.RiderAvoidanceWasDisabled,
                    "Rider avoidance flag was restored.",
                    "Rider avoidance flag was not restored.");
                assertions.Check(ReferenceEquals(snapshot.RiderView.AgentOverride, snapshot.RiderOverride),
                    "Rider AgentOverride was restored to its exact prior reference.",
                    "Rider AgentOverride retained or replaced an override after cleanup.");
                assertions.Check(snapshot.RiderView.GetComponents<RiderMovementAgent>().Length == snapshot.RiderOverrideComponentCount,
                    "Owned RiderMovementAgent component count returned to its exact prior value.",
                    "A RiderMovementAgent component remained or disappeared after cleanup.");
                assertions.Check(snapshot.RiderView.GetComponents<MountedRiderPoseAdapter>().Length == snapshot.RiderPoseComponentCount &&
                        (!poseLeaseAcquiredThisRow || relationship.Runtime.PoseBaselineRestoreVerified),
                    "Owned pose component count returned to its exact prior value and its bone baseline restoration was verified.",
                    "A mounted pose adapter or unverified bone baseline remained after cleanup.");
                assertions.Check(snapshot.RiderView.ForbidRotation == snapshot.RiderForbidRotationWasEnabled,
                    "Rider rotation lease returned to its exact prior value.",
                    "Rider rotation lease remained changed after cleanup.");
                assertions.Check(snapshot.RiderAttachmentParentRestored() &&
                        snapshot.RiderAttachmentSiblingIndexRestored() &&
                        snapshot.RiderAttachmentLocalScaleRestored() &&
                        (!attachmentLeaseAcquiredThisRow || relationship.Runtime.PresentationAttachmentRestoreVerified) &&
                        !relationship.Runtime.PresentationAttachmentLeaseActive &&
                        !relationship.Runtime.HasPresentationAttachmentResidue &&
                        string.Equals(relationship.Runtime.PresentationAttachmentRiskState, "none", StringComparison.Ordinal),
                    "Scoped rider attachment restored its captured parent, sibling index, and local scale and verified the full lease contract.",
                    "Scoped rider attachment did not verify restoration or retained parent/carrier residue.");
            }

            var mountStockAgentAlive = snapshot.MountStockAgent != null;
            assertions.Check(mountStockAgentAlive,
                "Retained mount stock agent remained alive for cleanup verification.",
                "Mount stock agent was destroyed before cleanup verification.");
            if (mountViewAlive && mountStockAgentAlive)
            {
                assertions.Check(ReferenceEquals(snapshot.MountView.AgentASP, snapshot.MountStockAgent),
                    "Exact mount stock-agent reference was preserved.",
                    "Mount stock-agent reference changed after cleanup.");
                assertions.Check(snapshot.MountStockAgent.enabled == snapshot.MountAgentWasEnabled,
                    "Mount stock-agent enabled flag was preserved.",
                    "Mount stock-agent enabled flag changed after cleanup.");
                assertions.Check(snapshot.MountStockAgent.AvoidanceDisabled == snapshot.MountAvoidanceWasDisabled,
                    "Mount avoidance flag was preserved.",
                    "Mount avoidance flag changed after cleanup.");
                assertions.Check(ReferenceEquals(snapshot.MountView.AgentOverride, snapshot.MountOverride),
                    "Mount AgentOverride was preserved.",
                    "Mount AgentOverride changed after cleanup.");
                assertions.Check(snapshot.MountView.GetComponents<RiderMovementAgent>().Length == snapshot.MountOverrideComponentCount,
                    "Mount RiderMovementAgent component count returned to its exact prior value.",
                    "Mount RiderMovementAgent component count changed after cleanup.");
                assertions.Check(snapshot.MountView.ForbidRotation == snapshot.MountForbidRotationWasEnabled,
                    "Mount rotation state returned to its exact prior value.",
                    "Mount rotation state changed after cleanup.");
            }
        }

        private void RequestCleanup(CleanupTrigger trigger)
        {
            try
            {
                var cleanup = relationship.Dismount(trigger);
                lastCleanupTransition = cleanup;
                if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                {
                    FailCurrent(trigger + " best-effort cleanup failed or reported residue: " + FormatTransitionErrors(cleanup));
                }
            }
            catch (Exception exception)
            {
                FailCurrent(trigger + " best-effort cleanup threw " + exception.GetType().Name + ": " + exception.Message);
            }
            AwaitCleanupFrame();
        }

        private void AwaitCleanupFrame()
        {
            cleanupFrame = frameNumber;
            step = EngineStep.AwaitCleanupFrame;
        }

        private void WriteCleanupFrameEvidenceOnce()
        {
            if (cleanupFrameEvidenceWritten)
            {
                return;
            }

            cleanupFrameEvidenceWritten = true;
            TryWriteEvidence("cleanup-next-frame", lastCleanupTransition, null);
        }

        private void FailCurrent(string message)
        {
            if (assertions == null)
            {
                assertions = new AssertionRecorder();
            }
            assertions.Fail(message);
        }

        private void FinishCurrentRow()
        {
            TryWriteEvidence("row-finish", lastCleanupTransition,
                assertions == null ? null : assertions.Errors);
            var rowResult = new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };
            results.Add(rowResult);
            foreach (var error in assertions.Errors)
            {
                errors.Add(currentRow + ": " + error);
            }

            if (string.Equals(rowResult.Status, "PASS", StringComparison.Ordinal))
            {
                logger.Info("Lifecycle runtime row PASS: " + currentRow + " (" + assertions.PassCount + " assertions).");
            }
            else
            {
                logger.Warning("Lifecycle runtime row FAIL: " + currentRow + " (" + assertions.FailureCount + " failed assertions).");
            }

            rowIndex++;
            currentRow = null;
            assertions = null;
            snapshot = null;
            rowClock.Reset();
            step = EngineStep.BeginRow;
        }

        private void CompleteRemainingAsNotRun(string reason)
        {
            if (currentRow != null)
            {
                FinishCurrentRow();
            }
            while (rowIndex < selectedRows.Count)
            {
                var row = selectedRows[rowIndex++];
                results.Add(new RuntimeSubscenarioResult
                {
                    Name = row,
                    Status = "FAIL",
                    AssertionPassCount = 0,
                    AssertionFailCount = 1,
                    Errors = new[] { reason }
                });
                errors.Add(row + ": " + reason);
            }
        }

        private void Complete()
        {
            TransitionResult finalCleanup = null;
            if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
            {
                try
                {
                    finalCleanup = relationship.Dismount(CleanupTrigger.Exception);
                    lastCleanupTransition = finalCleanup;
                    if (!finalCleanup.Succeeded || finalCleanup.MovementAuthorityResidual || finalCleanup.PresentationResidual)
                    {
                        errors.Add("Final lifecycle-engine cleanup retained mounted runtime residue.");
                    }
                }
                catch (Exception exception)
                {
                    errors.Add("Final lifecycle-engine cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                    logger.Exception("Final lifecycle-engine cleanup threw", exception);
                }
            }

            FinalizeEvidence(finalCleanup ?? lastCleanupTransition);
            RestoreSettings();
            suiteClock.Stop();
            rowClock.Stop();
            completed = true;
            logger.Info("Lifecycle runtime engine completed with " + results.Count + " row result(s).");
        }

        private void FinalizeEvidence(TransitionResult cleanup)
        {
            if (evidenceFinalized)
            {
                return;
            }

            TryWriteEvidence("engine-finalization", cleanup, errors);
            evidenceFinalized = true;
        }

        private bool TryWriteEvidence(
            string phase,
            TransitionResult cleanup,
            IReadOnlyList<string> recordErrors)
        {
            if (evidenceFailed || (evidenceFinalized && !string.Equals(phase, "engine-finalization", StringComparison.Ordinal)))
            {
                if (assertions != null && !string.IsNullOrEmpty(evidenceFailureMessage) &&
                    !assertions.Errors.Contains(evidenceFailureMessage))
                {
                    assertions.Fail(evidenceFailureMessage);
                }
                return false;
            }

            try
            {
                var record = CreateEvidenceRecord(phase, cleanup, recordErrors);
                var json = JsonConvert.SerializeObject(record, EvidenceJsonSettings);
                var mode = evidenceCreated ? FileMode.Append : FileMode.CreateNew;
                using (var stream = new FileStream(evidencePath, mode, FileAccess.Write, FileShare.Read))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
                    {
                        writer.WriteLine(json);
                        writer.Flush();
                    }
                    stream.Flush(true);
                }
                evidenceCreated = true;
                return true;
            }
            catch (Exception exception)
            {
                evidenceFailed = true;
                evidenceFailureMessage = "Lifecycle structured evidence write failed: " +
                    exception.GetType().Name + ": " + exception.Message;
                if (assertions != null)
                {
                    assertions.Fail(evidenceFailureMessage);
                }
                else
                {
                    errors.Add(evidenceFailureMessage);
                }
                logger.Exception("Lifecycle structured evidence write failed", exception);
                return false;
            }
        }

        private LifecycleEvidenceRecord CreateEvidenceRecord(
            string phase,
            TransitionResult cleanup,
            IReadOnlyList<string> recordErrors)
        {
            var pair = snapshot ?? evidenceSnapshot;
            var rider = pair == null ? relationship.Rider : pair.Rider;
            var mount = pair == null ? relationship.Mount : pair.Mount;
            var selection = SelectionManager.Instance == null ? null : SelectionManager.Instance.SelectedUnits;
            var riderSelected = selection == null || rider == null ? (bool?)null : selection.Contains(rider);
            var mountSelected = selection == null || mount == null ? (bool?)null : selection.Contains(mount);
            var agent = relationship.Runtime.MovementAgent;
            var mountView = pair == null ? mount?.View : pair.MountView;
            var riderView = pair == null ? rider?.View : pair.RiderView;
            var spine = mountView == null ? null : FindTransform(mountView.transform, "Spine");
            var game = Game.Instance;

            PositionEvidence expectedPosition = null;
            RotationEvidence expectedRotation = null;
            double? currentPositionResidual = null;
            double? currentRotationResidual = null;
            if (agent != null && agent.IsConfigured)
            {
                var expected = agent.ExpectedPosition;
                var expectedQuaternion = agent.ExpectedRotation;
                expectedPosition = PositionEvidence.From(expected);
                expectedRotation = RotationEvidence.From(expectedQuaternion);
                if (riderView != null)
                {
                    currentPositionResidual = MovementTelemetrySample.CalculateDistance(
                        expected.x, expected.y, expected.z,
                        riderView.transform.position.x, riderView.transform.position.y, riderView.transform.position.z);
                    currentRotationResidual = Quaternion.Angle(expectedQuaternion, riderView.transform.rotation);
                }
            }

            return new LifecycleEvidenceRecord
            {
                SchemaVersion = IsNativeIncapacitationRow(currentRow ?? lastEvidenceRow)
                    ? 4
                    : IsCombatLifecycleRow(currentRow ?? lastEvidenceRow) ? 3 : 2,
                RunId = request.RunId,
                Scenario = request.Scenario,
                Row = currentRow ?? lastEvidenceRow,
                Phase = phase,
                UtcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = dllSha256,
                DllMvid = dllMvid,
                Sequence = evidenceSequence++,
                Frame = frameNumber,
                RelationshipState = relationship.State.ToString(),
                TriggerScope = CreateTriggerScope(currentRow ?? lastEvidenceRow),
                RowStatus = string.Equals(phase, "row-finish", StringComparison.Ordinal) && assertions != null
                    ? (assertions.FailureCount == 0 ? "PASS" : "FAIL")
                    : null,
                AssertionPassCount = string.Equals(phase, "row-finish", StringComparison.Ordinal) && assertions != null
                    ? (int?)assertions.PassCount
                    : null,
                AssertionFailCount = string.Equals(phase, "row-finish", StringComparison.Ordinal) && assertions != null
                    ? (int?)assertions.FailureCount
                    : null,
                Cleanup = CleanupEvidence.From(cleanup),
                PartyCombat = game?.Player?.IsInCombat,
                RiderCombat = rider == null ? (bool?)null : rider.IsInCombat,
                MountCombat = mount == null ? (bool?)null : mount.IsInCombat,
                TurnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                Paused = game?.IsPaused,
                CurrentGameMode = game == null ? null : game.CurrentMode.ToString(),
                Rider = CreateUnitEvidence(rider, pair, true, riderSelected),
                Mount = CreateUnitEvidence(mount, pair, false, mountSelected),
                Selection = new SelectionEvidence
                {
                    Available = selection != null,
                    RiderSelected = riderSelected,
                    MountSelected = mountSelected,
                    SelectedUnitIds = selection == null
                        ? new string[0]
                        : selection.Where(unit => unit != null).Select(unit => unit.UniqueId == null ? null : unit.UniqueId.ToString()).ToArray()
                },
                Spine = spine == null ? null : new TransformEvidence
                {
                    Name = spine.name,
                    WorldPosition = PositionEvidence.From(spine.position),
                    WorldRotation = RotationEvidence.From(spine.rotation)
                },
                Anchor = new AnchorEvidence
                {
                    Name = agent == null ? null : agent.AnchorName,
                    ExpectedPosition = expectedPosition,
                    ExpectedRotation = expectedRotation,
                    CurrentPositionResidualWorldUnits = currentPositionResidual,
                    CurrentRotationResidualDegrees = currentRotationResidual,
                    PreCorrectionPositionResidualWorldUnits = agent == null ? (double?)null : agent.LatestPreCorrectionPositionResidualWorldUnits,
                    PreCorrectionRotationResidualDegrees = agent == null ? (double?)null : agent.LatestPreCorrectionRotationResidualDegrees,
                    PostCorrectionPositionResidualWorldUnits = agent == null ? (double?)null : agent.LatestPostCorrectionPositionResidualWorldUnits,
                    PostCorrectionRotationResidualDegrees = agent == null ? (double?)null : agent.LatestPostCorrectionRotationResidualDegrees
                },
                Attachment = CreateAttachmentEvidence(pair, riderView),
                Pose = IsCombatLifecycleRow(currentRow ?? lastEvidenceRow) ? CreatePoseEvidence(riderView) : null,
                BoundaryExercise = IsCombatLifecycleRow(currentRow ?? lastEvidenceRow)
                    ? (boundaryExercise ?? BoundaryExerciseEvidence.Pending(currentRow ?? lastEvidenceRow))
                    : null,
                ActorLifeTransition = IsNativeIncapacitationRow(currentRow ?? lastEvidenceRow)
                    ? actorLifeTransition
                    : null,
                RecordErrors = recordErrors == null ? new string[0] : recordErrors.ToArray()
            };
        }

        private TriggerScopeEvidence CreateTriggerScope(string row)
        {
            var nativeIncapacitation = IsNativeIncapacitationRow(row);
            return new TriggerScopeEvidence
            {
                ExpectedCleanupTrigger = GetExpectedCleanupTrigger(row).ToString(),
                InvocationPath = nativeIncapacitation
                    ? "stock-life-controller-eventbus"
                    : IsPlayerActionRow(row)
                        ? "player-action-controller-direct"
                        : (UsesLifecycleHandler(row) ? "lifecycle-handler-direct" : "relationship-service-direct"),
                NativeDeliveryObserved = nativeIncapacitation,
                ClaimLimit = nativeIncapacitation
                    ? NativeIncapacitationClaimLimit
                    : IsPlayerActionRow(row) ? PlayerActionClaimLimit : DirectInvocationClaimLimit
            };
        }

        private AttachmentEvidence CreateAttachmentEvidence(PairSnapshot pair, UnitEntityView riderView)
        {
            var riderTransform = riderView == null ? null : riderView.transform;
            return new AttachmentEvidence
            {
                LeaseContract = "parent+sibling+world-position+world-rotation+local-scale",
                LeaseActive = relationship.Runtime.PresentationAttachmentLeaseActive,
                RestoreVerified = relationship.Runtime.PresentationAttachmentRestoreVerified,
                Residue = relationship.Runtime.HasPresentationAttachmentResidue,
                RiderParentMatchesAttachment = relationship.Runtime.RiderParentMatchesAttachment,
                CurrentRiderParent = riderTransform == null ? null : BuildHierarchyName(riderTransform.parent),
                OriginalRiderParent = pair == null ? null : BuildHierarchyName(pair.RiderParent),
                RiderParentMatchesOriginal = pair != null && riderTransform != null && riderTransform.parent == pair.RiderParent,
                CurrentRiderSiblingIndex = riderTransform == null ? (int?)null : riderTransform.GetSiblingIndex(),
                OriginalRiderSiblingIndex = pair == null ? (int?)null : pair.RiderSiblingIndex,
                RiderSiblingIndexMatchesOriginal = pair != null && riderTransform != null &&
                    riderTransform.GetSiblingIndex() == pair.RiderSiblingIndex,
                CurrentRiderLocalScale = riderTransform == null ? null : PositionEvidence.From(riderTransform.localScale),
                OriginalRiderLocalScale = pair == null ? null : PositionEvidence.From(pair.RiderLocalScale),
                RiderLocalScaleMatchesOriginal = pair != null && riderTransform != null &&
                    Vector3.Distance(riderTransform.localScale, pair.RiderLocalScale) <= 0.0001f,
                AttachmentParent = relationship.Runtime.PresentationAttachmentParentName,
                SourceAnchor = relationship.Runtime.PresentationSourceAnchorName,
                RiskState = relationship.Runtime.PresentationAttachmentRiskState
            };
        }

        private PoseEvidence CreatePoseEvidence(UnitEntityView riderView)
        {
            return new PoseEvidence
            {
                ProfileId = relationship.Runtime.PoseProfileId,
                BoneInventory = relationship.Runtime.PoseBoneInventory,
                Configured = relationship.Runtime.PoseConfigured,
                Healthy = relationship.Runtime.PoseHealthy,
                FrameApplied = relationship.Runtime.PoseFrameApplied,
                BaselineRestoreVerified = relationship.Runtime.PoseBaselineRestoreVerified,
                ComponentCount = riderView == null ? (int?)null : riderView.GetComponents<MountedRiderPoseAdapter>().Length,
                BoneCount = relationship.Runtime.PoseBoneCount,
                ApplicationFrameCount = relationship.Runtime.PoseApplicationFrameCount,
                FootTargetClampCount = relationship.Runtime.PoseFootTargetClampCount,
                MaximumFootTargetResidualWorldUnits = relationship.Runtime.PoseMaximumFootTargetResidualWorldUnits,
                MaximumKneeTargetResidualWorldUnits = relationship.Runtime.PoseMaximumKneeTargetResidualWorldUnits,
                MaximumSegmentLengthResidualWorldUnits = relationship.Runtime.PoseMaximumSegmentLengthResidualWorldUnits,
                MaximumApplyMicroseconds = relationship.Runtime.PoseMaximumApplyMicroseconds,
                AverageApplyMicroseconds = relationship.Runtime.PoseAverageApplyMicroseconds,
                Failure = relationship.Runtime.PoseFailure
            };
        }

        private static UnitEvidence CreateUnitEvidence(
            UnitEntityData unit,
            PairSnapshot pair,
            bool rider,
            bool? selected)
        {
            if (unit == null)
            {
                return null;
            }

            var view = pair == null
                ? unit.View
                : (rider ? pair.RiderView : pair.MountView);
            var stockAgent = pair == null
                ? view?.AgentASP
                : (rider ? pair.RiderStockAgent : pair.MountStockAgent);
            var move = unit.Commands == null ? null : unit.Commands.Move;
            var state = unit.Descriptor == null ? null : unit.Descriptor.State;
            return new UnitEvidence
            {
                UniqueId = unit.UniqueId == null ? null : unit.UniqueId.ToString(),
                SizeOrdinal = state == null ? (int?)null : (int)state.Size,
                InCombat = unit.IsInCombat,
                StockAgentEnabled = stockAgent == null ? (bool?)null : stockAgent.enabled,
                AvoidanceDisabled = stockAgent == null ? (bool?)null : stockAgent.AvoidanceDisabled,
                ForbidRotation = view == null ? (bool?)null : view.ForbidRotation,
                AgentOverrideType = view == null || view.AgentOverride == null
                    ? null
                    : view.AgentOverride.GetType().FullName,
                OverrideComponentCount = view == null
                    ? (int?)null
                    : view.GetComponents<RiderMovementAgent>().Length,
                EntityPosition = PositionEvidence.From(unit.Position),
                EntityRotationDegrees = unit.Orientation,
                ViewPosition = view == null ? null : PositionEvidence.From(view.transform.position),
                ViewRotation = view == null ? null : RotationEvidence.From(view.transform.rotation),
                MoveCommandType = move == null ? null : move.GetType().FullName,
                MoveTarget = move == null ? null : PositionEvidence.From(move.Target),
                ActiveCommandTypes = unit.Commands == null || unit.Commands.Raw == null
                    ? new string[0]
                    : unit.Commands.Raw.Where(command => command != null).Select(command => command.GetType().FullName).ToArray(),
                Selected = selected
            };
        }

        private static Transform FindTransform(Transform root, string exactName)
        {
            if (root == null)
            {
                return null;
            }
            if (string.Equals(root.name, exactName, StringComparison.Ordinal))
            {
                return root;
            }
            for (var index = 0; index < root.childCount; index++)
            {
                var found = FindTransform(root.GetChild(index), exactName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static string BuildHierarchyName(Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Add(current.name ?? "<unnamed>");
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string ComputeSha256(string filePath)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void RestoreSettings()
        {
            if (!settingLeaseOwned)
            {
                return;
            }

            settings.EnableUnsafeMovementExperiment = originalUnsafeExperimentSetting;
            settingLeaseOwned = false;
        }

        private static IReadOnlyList<string> SelectRows(string scenario)
        {
            if (string.Equals(scenario, "lifecycle-suite", StringComparison.Ordinal))
            {
                return SuiteRows;
            }
            foreach (var row in SuiteRows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }
            foreach (var row in PlayerActionRows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }
            if (string.Equals(scenario, "combat-lifecycle-suite", StringComparison.Ordinal))
            {
                return CombatLifecycleRows;
            }
            foreach (var row in CombatLifecycleRows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }
            foreach (var row in NativeIncapacitationRows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }
            return null;
        }

        private static bool IsCombatLifecycleRow(string row)
        {
            return Array.IndexOf(CombatLifecycleRows, row) >= 0 || IsNativeIncapacitationRow(row);
        }

        private static bool IsNativeIncapacitationRow(string row)
        {
            return Array.IndexOf(NativeIncapacitationRows, row) >= 0;
        }

        private static bool IsPlayerActionRow(string row)
        {
            return Array.IndexOf(PlayerActionRows, row) >= 0;
        }

        private static CleanupTrigger GetExpectedCleanupTrigger(string row)
        {
            if (string.Equals(row, "mounted-pair-death-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.Death;
            }
            if (string.Equals(row, "mounted-pair-combat-start-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.CombatStarted;
            }
            if (string.Equals(row, "mounted-pair-area-unload-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.AreaUnloading;
            }
            if (string.Equals(row, "mounted-pair-mod-disable-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.ModDisabled;
            }
            if (string.Equals(row, "mounted-pair-rider-death-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-mount-death-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.Death;
            }
            if (string.Equals(row, "mounted-pair-rider-incapacitated-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-mount-incapacitated-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.Incapacitated;
            }
            if (IsNativeIncapacitationRow(row))
            {
                return CleanupTrigger.Incapacitated;
            }
            if (string.Equals(row, "mounted-pair-companion-removal-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.CompanionInvalidated;
            }
            if (string.Equals(row, "mounted-pair-view-destroyed-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.ViewDetached;
            }
            if (string.Equals(row, "mounted-pair-exception-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.Exception;
            }
            return CleanupTrigger.Manual;
        }

        private static bool UsesLifecycleHandler(string row)
        {
            return string.Equals(row, "mounted-pair-death-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-combat-start-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-area-unload-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-combat-start-retained", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-combat-end-retained", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-rider-death-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-mount-death-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-companion-removal-cleanup", StringComparison.Ordinal) ||
                string.Equals(row, "mounted-pair-view-destroyed-cleanup", StringComparison.Ordinal);
        }

        private void CaptureBoundaryExercise(string actorRole, string invocationPath)
        {
            var deliveries = lifecycle.SnapshotNativeDeliveries()
                .Where(record => record.Sequence > lifecycleDeliveryBaselineSequence)
                .Select(BoundaryDeliveryEvidence.From)
                .ToArray();
            boundaryExercise = new BoundaryExerciseEvidence
            {
                Observed = true,
                Row = currentRow,
                ActorRole = actorRole,
                ActorId = string.Equals(actorRole, "rider", StringComparison.Ordinal)
                    ? snapshot.Rider.UniqueId.ToString()
                    : (string.Equals(actorRole, "mount", StringComparison.Ordinal)
                        ? snapshot.Mount.UniqueId.ToString()
                        : null),
                InvocationPath = invocationPath,
                RelationshipStateAfterBoundary = relationship.State.ToString(),
                Deliveries = deliveries
            };
        }

        private void AssertObservedBoundary(
            NativeLifecycleBoundary boundary,
            string source,
            RelationshipState before,
            RelationshipState after,
            CleanupTrigger? trigger,
            bool cleanupAttempted)
        {
            var matches = lifecycle.SnapshotNativeDeliveries().Where(record =>
                record.Sequence > lifecycleDeliveryBaselineSequence &&
                record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal)).ToArray();
            var exact = matches.Length == 1 && matches[0].StateBefore == before &&
                matches[0].StateAfter == after && matches[0].CleanupTrigger == trigger &&
                matches[0].CleanupAttempted == cleanupAttempted && matches[0].CleanupSucceeded;
            assertions.Check(exact,
                "Exact lifecycle delivery was recorded for " + boundary + ".",
                "Lifecycle delivery was missing, duplicated, or semantically wrong for " + boundary + ".");
        }

        private static string FormatTransitionErrors(TransitionResult result)
        {
            if (result == null)
            {
                return "transition result was null";
            }
            return result.Errors == null || result.Errors.Count == 0
                ? "state=" + result.State
                : string.Join(" | ", result.Errors);
        }

        private bool HasExactSuccessfulTrigger(CleanupTrigger trigger)
        {
            var result = relationship.LastTransition;
            return result != null && result.Succeeded && result.Trigger == trigger &&
                !result.MovementAuthorityResidual && !result.PresentationResidual;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeLifecycleScenarioEngine));
            }
        }

        private enum EngineStep
        {
            BeginRow,
            AwaitMountedFrame,
            AwaitNativeIncapacitation,
            AwaitFirstIdempotentCleanupFrame,
            AwaitCleanupFrame
        }

        private sealed class LifecycleEvidenceRecord
        {
            public int SchemaVersion { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Row { get; set; }
            public string Phase { get; set; }
            public string UtcTimestamp { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public long Sequence { get; set; }
            public int Frame { get; set; }
            public string RelationshipState { get; set; }
            public TriggerScopeEvidence TriggerScope { get; set; }
            public string RowStatus { get; set; }
            public int? AssertionPassCount { get; set; }
            public int? AssertionFailCount { get; set; }
            public CleanupEvidence Cleanup { get; set; }
            public bool? PartyCombat { get; set; }
            public bool? RiderCombat { get; set; }
            public bool? MountCombat { get; set; }
            public bool? TurnBased { get; set; }
            public bool? Paused { get; set; }
            public string CurrentGameMode { get; set; }
            public UnitEvidence Rider { get; set; }
            public UnitEvidence Mount { get; set; }
            public SelectionEvidence Selection { get; set; }
            public TransformEvidence Spine { get; set; }
            public AnchorEvidence Anchor { get; set; }
            public AttachmentEvidence Attachment { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public PoseEvidence Pose { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public BoundaryExerciseEvidence BoundaryExercise { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ActorLifeTransitionEvidence ActorLifeTransition { get; set; }
            public IReadOnlyList<string> RecordErrors { get; set; }
        }

        private sealed class ActorLifeTransitionEvidence
        {
            public string ActorRole { get; set; }
            public string ActorId { get; set; }
            public string MutationProperty { get; set; }
            public bool MutationIssued { get; set; }
            public string LifeStateBefore { get; set; }
            public string LifeStateAfter { get; set; }
            public bool ConsciousBefore { get; set; }
            public bool ConsciousAfter { get; set; }
            public bool DeadAfter { get; set; }
            public bool FinallyDeadAfter { get; set; }
            public int DamageBefore { get; set; }
            public int RequestedDamage { get; set; }
            public int DamageAfter { get; set; }
            public int HitPoints { get; set; }
            public int Constitution { get; set; }
            public int NativeDeliveryCount { get; set; }

            public static ActorLifeTransitionEvidence Before(string actorRole, UnitEntityData actor, int requestedDamage)
            {
                var state = actor?.Descriptor?.State;
                return new ActorLifeTransitionEvidence
                {
                    ActorRole = actorRole,
                    ActorId = actor?.UniqueId,
                    MutationProperty = "UnitEntityData.Damage",
                    MutationIssued = false,
                    LifeStateBefore = state?.LifeState.ToString(),
                    LifeStateAfter = null,
                    ConsciousBefore = state != null && state.IsConscious,
                    ConsciousAfter = false,
                    DeadAfter = false,
                    FinallyDeadAfter = false,
                    DamageBefore = actor?.Damage ?? 0,
                    RequestedDamage = requestedDamage,
                    DamageAfter = 0,
                    HitPoints = actor == null ? 0 : (int)actor.Stats.HitPoints,
                    Constitution = actor == null ? 0 : (int)actor.Stats.Constitution,
                    NativeDeliveryCount = 0
                };
            }

            public void CaptureAfter(UnitEntityData actor, int nativeDeliveryCount)
            {
                var state = actor?.Descriptor?.State;
                LifeStateAfter = state?.LifeState.ToString();
                ConsciousAfter = state != null && state.IsConscious;
                DeadAfter = state != null && state.IsDead;
                FinallyDeadAfter = state != null && state.IsFinallyDead;
                DamageAfter = actor?.Damage ?? 0;
                NativeDeliveryCount = nativeDeliveryCount;
            }
        }

        private sealed class BoundaryExerciseEvidence
        {
            public bool Observed { get; set; }
            public string Row { get; set; }
            public string ActorRole { get; set; }
            public string ActorId { get; set; }
            public string InvocationPath { get; set; }
            public string RelationshipStateAfterBoundary { get; set; }
            public IReadOnlyList<BoundaryDeliveryEvidence> Deliveries { get; set; }

            public static BoundaryExerciseEvidence Pending(string row)
            {
                return new BoundaryExerciseEvidence
                {
                    Observed = false,
                    Row = row,
                    ActorRole = null,
                    ActorId = null,
                    InvocationPath = null,
                    RelationshipStateAfterBoundary = null,
                    Deliveries = new BoundaryDeliveryEvidence[0]
                };
            }
        }

        private void AwaitNativeIncapacitation()
        {
            if (incapacitationActor?.Descriptor?.State == null || actorLifeTransition == null)
            {
                FailCurrent("Native incapacitation actor or life evidence became unavailable.");
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            var state = incapacitationActor.Descriptor.State;
            var deliveries = lifecycle.SnapshotNativeDeliveries()
                .Where(record => record.Sequence > lifecycleDeliveryBaselineSequence)
                .ToArray();
            if (state.IsConscious || relationship.State != RelationshipState.Unmounted || deliveries.Length == 0)
            {
                return;
            }

            actorLifeTransition.CaptureAfter(incapacitationActor, deliveries.Length);
            lastCleanupTransition = relationship.LastTransition;
            CaptureBoundaryExercise(
                actorLifeTransition.ActorRole,
                "UnitEntityData.Damage -> UnitLifeController.TickOnUnit -> IUnitLifeStateChanged.HandleUnitLifeStateChanged");
            assertions.Check(string.Equals(actorLifeTransition.LifeStateBefore, "Conscious", StringComparison.Ordinal) &&
                    string.Equals(actorLifeTransition.LifeStateAfter, "Unconscious", StringComparison.Ordinal) &&
                    actorLifeTransition.ConsciousBefore && !actorLifeTransition.ConsciousAfter &&
                    !actorLifeTransition.DeadAfter && !actorLifeTransition.FinallyDeadAfter,
                "Stock life controller transitioned the exact actor from Conscious to Unconscious without death.",
                "Stock life transition was not exact: " + actorLifeTransition.LifeStateBefore + " -> " +
                    actorLifeTransition.LifeStateAfter + ".");
            assertions.Check(actorLifeTransition.MutationIssued &&
                    actorLifeTransition.DamageAfter == actorLifeTransition.RequestedDamage &&
                    actorLifeTransition.DamageAfter > actorLifeTransition.HitPoints &&
                    actorLifeTransition.DamageAfter < actorLifeTransition.HitPoints + actorLifeTransition.Constitution,
                "Exact diagnostic damage mutation remained inside the unconscious band.",
                "Diagnostic damage mutation or unconscious-band evidence was not exact.");
            assertions.Check(HasExactSuccessfulTrigger(CleanupTrigger.Incapacitated),
                "Native life-state delivery completed exact Incapacitated cleanup.",
                "Native life-state delivery did not complete Incapacitated cleanup: " + relationship.LastResult);
            AssertObservedBoundary(
                NativeLifecycleBoundary.UnitIncapacitated,
                "IUnitLifeStateChanged.HandleUnitLifeStateChanged",
                RelationshipState.Mounted,
                RelationshipState.Unmounted,
                CleanupTrigger.Incapacitated,
                true);
            assertions.Check(deliveries.Length == 1,
                "Exactly one native pair-incapacitation lifecycle delivery was observed.",
                "Native pair-incapacitation delivery count was " + deliveries.Length + " rather than one.");
            AwaitCleanupFrame();
        }

        private sealed class BoundaryDeliveryEvidence
        {
            public string Boundary { get; set; }
            public string Source { get; set; }
            public string StateBefore { get; set; }
            public string StateAfter { get; set; }
            public string CleanupTrigger { get; set; }
            public bool CleanupAttempted { get; set; }
            public bool CleanupSucceeded { get; set; }

            public static BoundaryDeliveryEvidence From(NativeLifecycleDeliveryRecord record)
            {
                return new BoundaryDeliveryEvidence
                {
                    Boundary = record.Boundary.ToString(),
                    Source = record.Source,
                    StateBefore = record.StateBefore.ToString(),
                    StateAfter = record.StateAfter.ToString(),
                    CleanupTrigger = record.CleanupTrigger.HasValue ? record.CleanupTrigger.Value.ToString() : null,
                    CleanupAttempted = record.CleanupAttempted,
                    CleanupSucceeded = record.CleanupSucceeded
                };
            }
        }

        private sealed class TriggerScopeEvidence
        {
            public string ExpectedCleanupTrigger { get; set; }
            public string InvocationPath { get; set; }
            public bool NativeDeliveryObserved { get; set; }
            public string ClaimLimit { get; set; }
        }

        private sealed class CleanupEvidence
        {
            public string Trigger { get; set; }
            public string Result { get; set; }
            public bool? Succeeded { get; set; }
            public string State { get; set; }
            public bool? MovementAuthorityResidual { get; set; }
            public bool? PresentationResidual { get; set; }
            public IReadOnlyList<string> Errors { get; set; }

            public static CleanupEvidence From(TransitionResult result)
            {
                var cleanUnmounted = result != null && result.Succeeded &&
                    result.State == RelationshipState.Unmounted &&
                    !result.MovementAuthorityResidual && !result.PresentationResidual;
                return new CleanupEvidence
                {
                    Trigger = result == null || !result.Trigger.HasValue ? null : result.Trigger.Value.ToString(),
                    Result = result == null ? null : (cleanUnmounted ? "PASS" : "FAIL"),
                    Succeeded = result == null ? (bool?)null : result.Succeeded,
                    State = result == null ? null : result.State.ToString(),
                    MovementAuthorityResidual = result == null ? (bool?)null : result.MovementAuthorityResidual,
                    PresentationResidual = result == null ? (bool?)null : result.PresentationResidual,
                    Errors = result == null || result.Errors == null ? new string[0] : result.Errors.ToArray()
                };
            }
        }

        private sealed class UnitEvidence
        {
            public string UniqueId { get; set; }
            public int? SizeOrdinal { get; set; }
            public bool? InCombat { get; set; }
            public bool? StockAgentEnabled { get; set; }
            public bool? AvoidanceDisabled { get; set; }
            public bool? ForbidRotation { get; set; }
            public string AgentOverrideType { get; set; }
            public int? OverrideComponentCount { get; set; }
            public PositionEvidence EntityPosition { get; set; }
            public float? EntityRotationDegrees { get; set; }
            public PositionEvidence ViewPosition { get; set; }
            public RotationEvidence ViewRotation { get; set; }
            public string MoveCommandType { get; set; }
            public PositionEvidence MoveTarget { get; set; }
            public IReadOnlyList<string> ActiveCommandTypes { get; set; }
            public bool? Selected { get; set; }
        }

        private sealed class SelectionEvidence
        {
            public bool Available { get; set; }
            public bool? RiderSelected { get; set; }
            public bool? MountSelected { get; set; }
            public IReadOnlyList<string> SelectedUnitIds { get; set; }
        }

        private sealed class TransformEvidence
        {
            public string Name { get; set; }
            public PositionEvidence WorldPosition { get; set; }
            public RotationEvidence WorldRotation { get; set; }
        }

        private sealed class AnchorEvidence
        {
            public string Name { get; set; }
            public PositionEvidence ExpectedPosition { get; set; }
            public RotationEvidence ExpectedRotation { get; set; }
            public double? CurrentPositionResidualWorldUnits { get; set; }
            public double? CurrentRotationResidualDegrees { get; set; }
            public double? PreCorrectionPositionResidualWorldUnits { get; set; }
            public double? PreCorrectionRotationResidualDegrees { get; set; }
            public double? PostCorrectionPositionResidualWorldUnits { get; set; }
            public double? PostCorrectionRotationResidualDegrees { get; set; }
        }

        private sealed class AttachmentEvidence
        {
            public string LeaseContract { get; set; }
            public bool LeaseActive { get; set; }
            public bool RestoreVerified { get; set; }
            public bool Residue { get; set; }
            public bool RiderParentMatchesAttachment { get; set; }
            public string CurrentRiderParent { get; set; }
            public string OriginalRiderParent { get; set; }
            public bool RiderParentMatchesOriginal { get; set; }
            public int? CurrentRiderSiblingIndex { get; set; }
            public int? OriginalRiderSiblingIndex { get; set; }
            public bool RiderSiblingIndexMatchesOriginal { get; set; }
            public PositionEvidence CurrentRiderLocalScale { get; set; }
            public PositionEvidence OriginalRiderLocalScale { get; set; }
            public bool RiderLocalScaleMatchesOriginal { get; set; }
            public string AttachmentParent { get; set; }
            public string SourceAnchor { get; set; }
            public string RiskState { get; set; }
        }

        private sealed class PoseEvidence
        {
            public string ProfileId { get; set; }
            public string BoneInventory { get; set; }
            public bool Configured { get; set; }
            public bool Healthy { get; set; }
            public bool FrameApplied { get; set; }
            public bool BaselineRestoreVerified { get; set; }
            public int? ComponentCount { get; set; }
            public int BoneCount { get; set; }
            public long ApplicationFrameCount { get; set; }
            public long FootTargetClampCount { get; set; }
            public double MaximumFootTargetResidualWorldUnits { get; set; }
            public double MaximumKneeTargetResidualWorldUnits { get; set; }
            public double MaximumSegmentLengthResidualWorldUnits { get; set; }
            public double MaximumApplyMicroseconds { get; set; }
            public double AverageApplyMicroseconds { get; set; }
            public string Failure { get; set; }
        }

        private sealed class PositionEvidence
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static PositionEvidence From(Vector3 value)
            {
                return new PositionEvidence { X = value.x, Y = value.y, Z = value.z };
            }
        }

        private sealed class RotationEvidence
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float W { get; set; }

            public static RotationEvidence From(Quaternion value)
            {
                return new RotationEvidence { X = value.x, Y = value.y, Z = value.z, W = value.w };
            }
        }

        private sealed class PairSnapshot
        {
            public UnitEntityData Rider { get; private set; }

            public UnitEntityData Mount { get; private set; }

            public UnitEntityView RiderView { get; private set; }

            public UnitEntityView MountView { get; private set; }

            public UnitMovementAgent RiderStockAgent { get; private set; }

            public UnitMovementAgent MountStockAgent { get; private set; }

            public bool RiderAgentWasEnabled { get; private set; }

            public bool MountAgentWasEnabled { get; private set; }

            public bool RiderAvoidanceWasDisabled { get; private set; }

            public bool MountAvoidanceWasDisabled { get; private set; }

            public object RiderOverride { get; private set; }

            public object MountOverride { get; private set; }

            public int RiderOverrideComponentCount { get; private set; }

            public int MountOverrideComponentCount { get; private set; }

            public int RiderPoseComponentCount { get; private set; }

            public int MountPoseComponentCount { get; private set; }

            public bool RiderForbidRotationWasEnabled { get; private set; }

            public bool MountForbidRotationWasEnabled { get; private set; }

            public Transform RiderParent { get; private set; }

            public int RiderSiblingIndex { get; private set; }

            public Vector3 RiderLocalScale { get; private set; }

            public bool RiderAttachmentParentRestored()
            {
                return RiderView != null && RiderView.transform.parent == RiderParent;
            }

            public bool RiderAttachmentSiblingIndexRestored()
            {
                return RiderView != null && RiderView.transform.GetSiblingIndex() == RiderSiblingIndex;
            }

            public bool RiderAttachmentLocalScaleRestored()
            {
                return RiderView != null && Vector3.Distance(RiderView.transform.localScale, RiderLocalScale) <= 0.0001f;
            }

            public static PairSnapshot TryCreate(UnitEntityData rider, UnitEntityData mount, out string error)
            {
                error = null;
                if (rider == null || mount == null || rider.View == null || mount.View == null ||
                    rider.View.AgentASP == null || mount.View.AgentASP == null)
                {
                    error = "Resolved pair does not expose both views and stock agents.";
                    return null;
                }

                return new PairSnapshot
                {
                    Rider = rider,
                    Mount = mount,
                    RiderView = rider.View,
                    MountView = mount.View,
                    RiderStockAgent = rider.View.AgentASP,
                    MountStockAgent = mount.View.AgentASP,
                    RiderAgentWasEnabled = rider.View.AgentASP.enabled,
                    MountAgentWasEnabled = mount.View.AgentASP.enabled,
                    RiderAvoidanceWasDisabled = rider.View.AgentASP.AvoidanceDisabled,
                    MountAvoidanceWasDisabled = mount.View.AgentASP.AvoidanceDisabled,
                    RiderOverride = rider.View.AgentOverride,
                    MountOverride = mount.View.AgentOverride,
                    RiderOverrideComponentCount = rider.View.GetComponents<RiderMovementAgent>().Length,
                    MountOverrideComponentCount = mount.View.GetComponents<RiderMovementAgent>().Length,
                    RiderPoseComponentCount = rider.View.GetComponents<MountedRiderPoseAdapter>().Length,
                    MountPoseComponentCount = mount.View.GetComponents<MountedRiderPoseAdapter>().Length,
                    RiderForbidRotationWasEnabled = rider.View.ForbidRotation,
                    MountForbidRotationWasEnabled = mount.View.ForbidRotation,
                    RiderParent = rider.View.transform.parent,
                    RiderSiblingIndex = rider.View.transform.GetSiblingIndex(),
                    RiderLocalScale = rider.View.transform.localScale
                };
            }
        }

        private sealed class AssertionRecorder
        {
            private readonly List<string> errors = new List<string>();

            public int PassCount { get; private set; }

            public int FailureCount { get; private set; }

            public IReadOnlyList<string> Errors => errors;

            public void Check(bool condition, string success, string failure)
            {
                if (condition)
                {
                    PassCount++;
                }
                else
                {
                    Fail(failure);
                }
            }

            public void Fail(string message)
            {
                FailureCount++;
                errors.Add(message);
            }
        }
    }
}
