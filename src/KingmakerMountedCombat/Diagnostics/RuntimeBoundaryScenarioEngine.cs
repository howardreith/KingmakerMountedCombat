using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
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
    /// Executes the Phase 1 mode/save/area boundary rows against only the
    /// externally-qualified Working fixture. The engine never enumerates save
    /// slots and never constructs or opens the Baseline path.
    /// </summary>
    internal sealed class RuntimeBoundaryScenarioEngine : IDisposable
    {
        private const double RowTimeoutSeconds = 45.0d;
        private const double SuiteTimeoutSeconds = 260.0d;
        private const int StableWorldFramesRequired = 10;
        private const string AnchorObjectName = "KMC_RiderPositionAnchor";

        private static readonly JsonSerializerSettings EvidenceJsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly RuntimeSaveAuthorization saveAuthorization;
        private readonly WorkingFixtureLoader fixtureLoader;
        private readonly MountedPlayerActionController playerAction;
        private readonly DiagnosticSettings settings;
        private readonly Func<bool, bool> registeredToggle;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch suiteClock = new Stopwatch();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly BoundaryFailureDrain failureDrain = new BoundaryFailureDrain();
        private readonly string evidencePath;
        private readonly string dllSha256;
        private readonly string dllMvid;

        private IReadOnlyList<string> selectedRows;
        private BoundaryEvidenceSequenceGuard evidenceSequenceGuard;
        private BoundaryEvidenceJournal evidenceJournal;
        private AssertionRecorder assertions;
        private PairSnapshot snapshot;
        private UnitEntityData freshRider;
        private UnitEntityData freshMount;
        private FileIdentitySnapshot postInitialLoadWorkingIdentity;
        private FileIdentitySnapshot currentWorkingIdentityAuthority;
        private FileIdentitySnapshot rowStartWorkingIdentity;
        private FileIdentitySnapshot preDispatchWorkingIdentity;
        private FileIdentitySnapshot completedBoundaryWorkingIdentity;
        private BoundaryCleanupEvidence cleanupLatch;
        private FreshWorldEvidence freshWorldEvidence;
        private DescriptorIdentityEvidence descriptorIdentityForRow;
        private string currentRow;
        private int rowIndex;
        private int frameNumber;
        private int boundaryFrame;
        private int stableWorldFrames;
        private int authorizedLoadsBefore;
        private int authorizedWritesBefore;
        private int unauthorizedLoadsBefore;
        private int unauthorizedWritesBefore;
        private int baselineLoadsBefore;
        private int fatalViolationsBefore;
        private int suppressedWorkingWritesBefore;
        private long fileLengthBefore;
        private long fileWriteTicksBefore;
        private string fileHashBefore;
        private bool asynchronousCallback;
        private bool loadingObserved;
        private bool loadingStartObserved;
        private bool loadingStopObserved;
        private bool loadingStartEvidenceWritten;
        private bool loadingStopEvidenceWritten;
        private bool boundaryCleanupVerified;
        private bool descriptorVerifiedForRow;
        private bool descriptorVerificationObserved;
        private bool realWorkingLoadDispatched;
        private bool realWorkingSaveDispatched;
        private bool realAreaReloadDispatched;
        private long nativeDeliveryBaselineSequence;
        private CleanupTrigger? currentExpectedCleanupTrigger;
        private IDisposable saveSuppressionLease;
        private NativeModeTransitionProbe nativeModeProbe;
        private NativeModeProbeEvidence nativeModeEvidence;
        private ModDisableProbeEvidence modDisableEvidence;
        private NativeAreaBoundaryProgress nativeAreaProgress;
        private long evidenceSequence;
        private bool evidenceFailed;
        private string evidenceFailureMessage;
        private bool rowStartEvidenceWritten;
        private EngineStep step;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;

        public RuntimeBoundaryScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedLifecycleSubscriber lifecycle,
            RuntimeSaveAuthorization saveAuthorization,
            WorkingFixtureLoader fixtureLoader,
            MountedPlayerActionController playerAction,
            DiagnosticSettings settings,
            Func<bool, bool> registeredToggle,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.saveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            this.fixtureLoader = fixtureLoader ?? throw new ArgumentNullException(nameof(fixtureLoader));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.registeredToggle = registeredToggle ?? throw new ArgumentNullException(nameof(registeredToggle));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            evidencePath = Path.Combine(request.EvidenceRoot, BoundaryScenarioEvidenceContract.EvidenceFileName);
            var assembly = typeof(Main).Assembly;
            dllSha256 = ComputeSha256(assembly.Location);
            dllMvid = assembly.ManifestModule.ModuleVersionId.ToString();
        }

        public bool IsCompleted => completed;

        internal bool IsFailurePending => failureDrain.IsLatched;

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
                throw new InvalidOperationException("Boundary scenario engine has already started.");
            }

            selectedRows = BoundaryScenarioEvidenceContract.SelectRows(request.Scenario);
            started = true;
            if (selectedRows == null)
            {
                errors.Add("Scenario is not boundary-suite or an exact boundary mission row: " + request.Scenario + ".");
                completed = true;
                return;
            }
            if (request.SchemaVersion != RuntimeRequest.SaveBackedSchemaVersion || request.Fixture == null)
            {
                errors.Add("Boundary scenarios require an exact schema-v2 fixture request.");
                completed = true;
                return;
            }
            if (!saveAuthorization.IsActive || fixtureLoader.State != WorkingFixtureLoadState.LoadedAndVerified ||
                string.IsNullOrWhiteSpace(fixtureLoader.WorkingPath))
            {
                errors.Add("Boundary scenarios require an active Working-only authorization lease and a verified Working load.");
                completed = true;
                return;
            }

            string identityError;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out postInitialLoadWorkingIdentity, out identityError))
            {
                errors.Add("Boundary scenarios could not capture the post-initial-load Working identity: " + identityError + ".");
                completed = true;
                return;
            }
            currentWorkingIdentityAuthority = postInitialLoadWorkingIdentity;

            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            evidenceSequenceGuard = new BoundaryEvidenceSequenceGuard(selectedRows);
            evidenceJournal = new BoundaryEvidenceJournal(evidencePath);
            suiteClock.Start();
            step = EngineStep.BeginRow;
            logger.Info("Boundary runtime engine started for " + request.Scenario + ".");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started)
            {
                throw new InvalidOperationException("Boundary scenario engine must be started before Update.");
            }
            if (completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                if (failureDrain.State != BoundaryFailureDrainState.Inactive)
                {
                    DrainPendingFailure();
                    return;
                }

                if (suiteClock.Elapsed.TotalSeconds > SuiteTimeoutSeconds)
                {
                    AbortSuite("Boundary suite exceeded its " + SuiteTimeoutSeconds + " second monotonic deadline.");
                    return;
                }
                if (currentRow != null && rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds)
                {
                    AbortSuite("Boundary row exceeded its " + RowTimeoutSeconds + " second monotonic deadline.");
                    return;
                }
                if (saveAuthorization.FatalViolationCount != fatalViolationsBefore && currentRow != null)
                {
                    AbortSuite(saveAuthorization.LastFatalViolation ?? "Runtime save authorization reported an unspecified fatal violation.");
                    return;
                }

                Advance();
                if (!completed && failureDrain.State == BoundaryFailureDrainState.Inactive &&
                    currentRow != null && assertions != null && assertions.FailureCount != 0)
                {
                    AbortSuite("Boundary row recorded a failed assertion; no later boundary row may execute.");
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Boundary runtime row threw", exception);
                AbortSuite(exception.GetType().Name + ": " + exception.Message);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                BestEffortCleanup();
            }
            finally
            {
                saveSuppressionLease?.Dispose();
                saveSuppressionLease = null;
                nativeModeProbe?.Dispose();
                nativeModeProbe = null;
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
                    BeginBoundary();
                    break;
                case EngineStep.AwaitSimpleCleanupFrame:
                    VerifySimpleCleanupAndFinish();
                    break;
                case EngineStep.AwaitLoadCompletion:
                    VerifyLoadCompletion();
                    break;
                case EngineStep.AwaitAreaCompletion:
                    VerifyAreaCompletion();
                    break;
                default:
                    throw new InvalidOperationException("Unexpected boundary engine step: " + step + ".");
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
            assertions = new AssertionRecorder();
            snapshot = null;
            freshRider = null;
            freshMount = null;
            preDispatchWorkingIdentity = null;
            rowStartWorkingIdentity = null;
            completedBoundaryWorkingIdentity = null;
            cleanupLatch = null;
            freshWorldEvidence = null;
            descriptorIdentityForRow = null;
            stableWorldFrames = 0;
            asynchronousCallback = false;
            loadingObserved = false;
            loadingStartObserved = false;
            loadingStopObserved = false;
            loadingStartEvidenceWritten = false;
            loadingStopEvidenceWritten = false;
            boundaryCleanupVerified = false;
            descriptorVerifiedForRow = false;
            descriptorVerificationObserved = false;
            realWorkingLoadDispatched = false;
            realWorkingSaveDispatched = false;
            realAreaReloadDispatched = false;
            currentExpectedCleanupTrigger = null;
            saveSuppressionLease?.Dispose();
            saveSuppressionLease = null;
            nativeModeProbe?.Dispose();
            nativeModeProbe = null;
            nativeModeEvidence = null;
            modDisableEvidence = null;
            nativeAreaProgress = string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal)
                ? new NativeAreaBoundaryProgress()
                : null;
            nativeDeliveryBaselineSequence = LastNativeDeliverySequence();
            rowStartEvidenceWritten = false;
            CaptureAuthorizationCounts();
            rowClock.Restart();

            var expectedAuthorizedLoadsBefore = 1;
            for (var priorRowIndex = 0; priorRowIndex < rowIndex; priorRowIndex++)
            {
                if (string.Equals(selectedRows[priorRowIndex], "mounted-pair-load-safety", StringComparison.Ordinal))
                {
                    expectedAuthorizedLoadsBefore++;
                }
            }
            var authorizationBaselineExact = authorizedLoadsBefore == expectedAuthorizedLoadsBefore &&
                authorizedWritesBefore == 0 && unauthorizedLoadsBefore == 0 && unauthorizedWritesBefore == 0 &&
                baselineLoadsBefore == 0 && fatalViolationsBefore == 0 &&
                suppressedWorkingWritesBefore == 0 && !saveAuthorization.IsOneShotWorkingWriteSuppressionArmed;
            assertions.Check(authorizationBaselineExact,
                "Save authorization counters proved only the initial exact Working load and prior suite load row, if any.",
                "Save authorization counters contained an unexpected load, write, Baseline request, or fatal violation before the row.");
            if (!authorizationBaselineExact)
            {
                AbortSuite("Boundary row refused to mutate runtime state from an inexact save-authorization baseline.");
                return;
            }

            assertions.Check(IsWorldReady(),
                "Fixture world was stable before the boundary row.",
                "Fixture world was not in a stable Default-mode loaded-area state before the boundary row.");
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began the row Unmounted.",
                "Relationship began the row in " + relationship.State + " rather than Unmounted.");
            if (!IsWorldReady() || relationship.State != RelationshipState.Unmounted)
            {
                AbortSuite("Boundary row could not begin from a stable Unmounted fixture state.");
                return;
            }

            UnitEntityData rider;
            UnitEntityData mount;
            string resolutionError;
            var resolved = relationship.TryResolveAutomationPair(out rider, out mount, out resolutionError);
            assertions.Check(resolved,
                "Exact Working automation pair resolved.",
                "Exact Working automation pair did not resolve: " + (resolutionError ?? "unknown error"));
            if (!resolved)
            {
                AbortSuite("Exact Working automation pair could not be resolved.");
                return;
            }

            string snapshotError;
            snapshot = PairSnapshot.TryCreate(rider, mount, out snapshotError);
            assertions.Check(snapshot != null,
                "Pre-mount pair movement state was captured.",
                "Pre-mount pair movement state could not be captured: " + (snapshotError ?? "unknown error"));
            if (snapshot == null)
            {
                AbortSuite("Pair movement state could not be retained for residue verification.");
                return;
            }
            assertions.Check(!snapshot.RiderAvoidanceWasDisabled && !snapshot.MountAvoidanceWasDisabled,
                "Fresh rider and mount both began with ordinary avoidance enabled.",
                "Fresh rider or mount began with AvoidanceDisabled=true before KMC acquired authority.");
            if (snapshot.RiderAvoidanceWasDisabled || snapshot.MountAvoidanceWasDisabled)
            {
                AbortSuite("Refused to mount a pair with pre-existing avoidance suppression.");
                return;
            }

            var exactBaseline = snapshot.RiderAgentWasEnabled && snapshot.MountAgentWasEnabled &&
                snapshot.RiderOverride == null && snapshot.MountOverride == null &&
                snapshot.RiderOverrideComponentCount == 0 && snapshot.MountOverrideComponentCount == 0 &&
                !snapshot.RiderForbidRotationWasEnabled && !snapshot.MountForbidRotationWasEnabled &&
                snapshot.RiderMoveCommand == null && snapshot.MountMoveCommand == null &&
                !relationship.Runtime.PresentationAttachmentLeaseActive &&
                !relationship.Runtime.HasPresentationAttachmentResidue && CountKmcAnchorObjects() == 0 &&
                CountKmcRiderMovementAgents() == 0;
            assertions.Check(exactBaseline,
                "Fresh pair began with exact stock movement, rotation, command, component, and attachment state.",
                "Fresh pair began with movement, rotation, command, component, or attachment residue.");
            if (!exactBaseline)
            {
                AbortSuite("Refused to mount a pair that did not expose the exact clean boundary baseline.");
                return;
            }

            if (string.Equals(currentRow, "native-mode-transition-cleanup", StringComparison.Ordinal))
            {
                nativeModeProbe = new NativeModeTransitionProbe();
                currentExpectedCleanupTrigger = nativeModeProbe.TemporaryValue
                    ? CleanupTrigger.TurnBasedModeChanged
                    : CleanupTrigger.RealtimeModeChanged;
                nativeModeEvidence = NativeModeProbeEvidence.Capture(nativeModeProbe);
            }

            // The first CreateNew record is durably flushed after read-only pair
            // resolution but before this engine changes even the diagnostic
            // setting that permits a mount. Later rows use the same boundary.
            if (!CommitRowStartEvidence())
            {
                AbortSuite("Boundary row could not commit its durable pre-mutation evidence record.");
                return;
            }
            if (!settingLeaseOwned)
            {
                settings.EnableUnsafeMovementExperiment = true;
                settingLeaseOwned = true;
            }

            var mounted = relationship.MountAutomationPair();
            assertions.Check(mounted.Succeeded,
                "Valid automation pair mounted before the boundary.",
                "Valid automation pair mount failed: " + FormatTransitionErrors(mounted));
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship entered Mounted before the boundary.",
                "Relationship state after mount was " + relationship.State + ".");
            if (!mounted.Succeeded || relationship.State != RelationshipState.Mounted)
            {
                AbortSuite("Pair could not enter Mounted before the boundary.");
                return;
            }

            AssertMountedAuthority();
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitMountedFrame;
        }

        private void BeginBoundary()
        {
            if (frameNumber <= boundaryFrame)
            {
                return;
            }

            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship remained Mounted through the pre-boundary frame.",
                "Relationship left Mounted before the requested boundary; observed " + relationship.State + ".");
            if (relationship.State != RelationshipState.Mounted)
            {
                AbortSuite("Relationship invalidated before the requested boundary.");
                return;
            }

            AssertWorkingIdentityEquals(rowStartWorkingIdentity,
                "Exact Working file identity remained stable from row-start through mounted boundary dispatch.");

            AssertMountedAuthority();
            if (assertions.FailureCount != 0 || !TryWriteEvidence("mounted", true, false, null, null))
            {
                AbortSuite("Mounted boundary authority could not be proven before dispatch.");
                return;
            }

            if (string.Equals(currentRow, "mounted-pair-turn-based-entry-cleanup", StringComparison.Ordinal))
            {
                BeginDirectModeBoundary(true, CleanupTrigger.TurnBasedModeChanged);
            }
            else if (string.Equals(currentRow, "mounted-pair-realtime-entry-cleanup", StringComparison.Ordinal))
            {
                BeginDirectModeBoundary(false, CleanupTrigger.RealtimeModeChanged);
            }
            else if (string.Equals(currentRow, "mounted-pair-save-safety", StringComparison.Ordinal))
            {
                BeginExactWorkingSave();
            }
            else if (string.Equals(currentRow, "mounted-pair-load-safety", StringComparison.Ordinal))
            {
                BeginExactWorkingLoad();
            }
            else if (string.Equals(currentRow, "mounted-pair-area-transition-safety", StringComparison.Ordinal))
            {
                BeginExactAreaReload();
            }
            else if (string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal))
            {
                BeginNativeWorkingSave();
            }
            else if (string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal))
            {
                BeginNativeAreaReload();
            }
            else if (string.Equals(currentRow, "native-mode-transition-cleanup", StringComparison.Ordinal))
            {
                BeginNativeModeBoundary();
            }
            else if (string.Equals(currentRow, "presentation-residue-and-uninstall-safety", StringComparison.Ordinal))
            {
                BeginNativeModDisable();
            }
            else
            {
                AbortSuite("Unsupported boundary row reached execution: " + currentRow + ".");
            }
        }

        private void BeginDirectModeBoundary(bool enabled, CleanupTrigger expected)
        {
            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Mode boundary pre-dispatch evidence could not be committed.");
                return;
            }

            lifecycle.HandleTurnBasedModeStateChanged(enabled);
            CaptureAndAssertCleanupLatch(expected);
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Mode boundary cleanup latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Mode boundary cleanup latch retained runtime residue.");
                return;
            }

            AwaitSimpleCleanupFrame();
        }

        private void BeginExactWorkingSave()
        {
            var descriptor = fixtureLoader.Descriptor;
            string descriptorError;
            var descriptorVerified = VerifyExactWorkingDescriptor(
                descriptor,
                fixtureLoader.WorkingPath,
                out descriptorError);
            assertions.Check(descriptorVerified,
                "Initial loader descriptor still identified exact Working.",
                "Initial loader descriptor was not exact Working: " + (descriptorError ?? "unknown error"));
            if (!descriptorVerified)
            {
                AbortSuite("Refused save because the supplied descriptor was not exact Working.");
                return;
            }
            descriptorVerificationObserved = true;
            descriptorVerifiedForRow = true;
            descriptorIdentityForRow = DescriptorIdentityEvidence.From(descriptor);

            var before = new FileInfo(fixtureLoader.WorkingPath);
            if (!before.Exists || (before.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                AbortSuite("Exact Working path disappeared or became a reparse point before save.");
                return;
            }
            fileLengthBefore = before.Length;
            fileWriteTicksBefore = before.LastWriteTimeUtc.Ticks;
            fileHashBefore = ComputeSha256(fixtureLoader.WorkingPath);

            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Save boundary pre-dispatch evidence could not be committed.");
                return;
            }

            // Exercise the same cleanup service used by the exact-token SaveRoutine
            // prefix, but do not enter stock SaveRoutine: Kingmaker allocates a second
            // Working-named leaf before replacing the requested slot, which cannot be
            // made crash-safe by the Phase 1 exact-file transaction.
            var guarded = relationship.GuardBoundary(CleanupTrigger.SaveRequested);
            assertions.Check(guarded,
                "Save boundary cleanup cleared the runtime-only relationship before serialization.",
                "Save boundary cleanup retained mounted state or residue.");
            CaptureAndAssertCleanupLatch(CleanupTrigger.SaveRequested);
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Save boundary cleanup latch evidence could not be committed.");
                return;
            }
            if (!guarded || assertions.FailureCount != 0)
            {
                AbortSuite("Save boundary cleanup latch retained runtime residue.");
                return;
            }
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitSimpleCleanupFrame;
        }

        private void BeginNativeWorkingSave()
        {
            var descriptor = fixtureLoader.Descriptor;
            string descriptorError;
            var descriptorVerified = VerifyExactWorkingDescriptor(
                descriptor,
                fixtureLoader.WorkingPath,
                out descriptorError);
            assertions.Check(descriptorVerified,
                "Native save probe descriptor identified exact Working.",
                "Native save probe descriptor was not exact Working: " + (descriptorError ?? "unknown error"));
            if (!descriptorVerified)
            {
                AbortSuite("Refused native save probe because its descriptor was not exact Working.");
                return;
            }

            var nativeSaveEntryAllowed = Game.Instance != null && Game.Instance.SaveManager != null &&
                Game.Instance.SaveManager.IsSaveAllowed() && SettingsRoot.Instance != null &&
                SettingsRoot.Instance.OnlyOneSave != null && !SettingsRoot.Instance.OnlyOneSave.CurrentValue;
            assertions.Check(nativeSaveEntryAllowed,
                "Native Game.SaveGame preconditions allowed an ordinary manual save without ironman coercion.",
                "Native Game.SaveGame would reject the request or coerce the exact Working descriptor under current game settings.");
            if (!nativeSaveEntryAllowed)
            {
                AbortSuite("Refused native save dispatch because Game.SaveGame preconditions were not exact and safe.");
                return;
            }

            descriptorVerificationObserved = true;
            descriptorVerifiedForRow = true;
            descriptorIdentityForRow = DescriptorIdentityEvidence.From(descriptor);
            string identityError;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out preDispatchWorkingIdentity, out identityError) ||
                !preDispatchWorkingIdentity.Equals(rowStartWorkingIdentity))
            {
                assertions.Fail("Native save immediate pre-dispatch Working identity differed: " +
                    (identityError ?? "length/timestamp/SHA-256 mismatch"));
                AbortSuite("Refused native save dispatch after exact Working identity revalidation failed.");
                return;
            }
            fileLengthBefore = preDispatchWorkingIdentity.Length;
            fileWriteTicksBefore = preDispatchWorkingIdentity.LastWriteTimeUtcTicks;
            fileHashBefore = preDispatchWorkingIdentity.Sha256;

            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Native save pre-dispatch evidence could not be committed.");
                return;
            }

            saveSuppressionLease = saveAuthorization.ArmOneShotWorkingWriteSuppression();
            realWorkingSaveDispatched = true;
            try
            {
                Game.Instance.SaveGame(descriptor, HandleAsynchronousCallback);
            }
            finally
            {
                saveSuppressionLease.Dispose();
                saveSuppressionLease = null;
            }

            assertions.Check(saveAuthorization.SuppressedWorkingWriteCount - suppressedWorkingWritesBefore == 1 &&
                    !saveAuthorization.IsOneShotWorkingWriteSuppressionArmed,
                "Exact one-shot Working serialization suppression was consumed once by the native SaveRoutine prefix.",
                "Native save did not consume exactly one bounded nonfatal Working-write suppression.");
            CaptureAndAssertCleanupLatch(CleanupTrigger.SaveRequested);
            AssertExactNativeCleanupDelivery(
                NativeLifecycleBoundary.SaveRequest,
                CleanupTrigger.SaveRequested,
                "SaveManager.SaveRoutine Harmony12 prefix");
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Native save cleanup-latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Native save cleanup or suppression latch was inexact.");
                return;
            }

            boundaryFrame = frameNumber;
            step = EngineStep.AwaitSimpleCleanupFrame;
        }

        private void BeginNativeModeBoundary()
        {
            if (nativeModeProbe == null || !currentExpectedCleanupTrigger.HasValue)
            {
                AbortSuite("Native mode probe was not prepared before mounted dispatch.");
                return;
            }
            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Native mode pre-dispatch evidence could not be committed.");
                return;
            }

            nativeModeProbe.DispatchTemporaryValue();
            CaptureAndAssertCleanupLatch(currentExpectedCleanupTrigger.Value);
            var temporaryBoundary = nativeModeProbe.TemporaryValue
                ? NativeLifecycleBoundary.TurnBasedEnabled
                : NativeLifecycleBoundary.RealtimeEnabled;
            AssertExactNativeCleanupDelivery(
                temporaryBoundary,
                currentExpectedCleanupTrigger.Value,
                "ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(" + nativeModeProbe.TemporaryValue + ")");
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Native mode cleanup-latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Native mode transition cleanup retained residue or lacked exact EventBus delivery.");
                return;
            }

            nativeModeProbe.DispatchRestoreAndRestoreRawCache();
            var restoreBoundary = nativeModeProbe.OriginalValue
                ? NativeLifecycleBoundary.TurnBasedEnabled
                : NativeLifecycleBoundary.RealtimeEnabled;
            var restoreTrigger = nativeModeProbe.OriginalValue
                ? CleanupTrigger.TurnBasedModeChanged
                : CleanupTrigger.RealtimeModeChanged;
            AssertNativeRestoreDelivery(
                restoreBoundary,
                restoreTrigger,
                "ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(" + nativeModeProbe.OriginalValue + ")");
            nativeModeEvidence = NativeModeProbeEvidence.Capture(nativeModeProbe);
            nativeModeProbe.Dispose();
            nativeModeProbe = null;
            AwaitSimpleCleanupFrame();
        }

        private void BeginNativeAreaReload()
        {
            assertions.Check(Game.Instance != null && Game.Instance.CurrentlyLoadedArea != null,
                "A loaded area was present for native ReloadArea delivery.",
                "Native ReloadArea was refused because no area was loaded.");
            if (Game.Instance == null || Game.Instance.CurrentlyLoadedArea == null)
            {
                AbortSuite("Native area probe requires the verified loaded Working area.");
                return;
            }
            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Native area pre-dispatch evidence could not be committed.");
                return;
            }

            boundaryFrame = frameNumber;
            step = EngineStep.AwaitAreaCompletion;
            realAreaReloadDispatched = true;
            Game.Instance.ReloadArea();
            ObserveNativeAreaBoundaryProgress();
        }

        private void BeginNativeModDisable()
        {
            modDisableEvidence = new ModDisableProbeEvidence
            {
                Executed = true,
                OverlayPresentBeforeDisable = playerAction.OverlayPresent,
                OverlayObjectCountBeforeDisable = MountedPlayerActionController.CountOverlayObjects()
            };
            assertions.Check(modDisableEvidence.OverlayPresentBeforeDisable == true &&
                    modDisableEvidence.OverlayObjectCountBeforeDisable == 1,
                "Exactly one transient player-action overlay was owned before registered UMM disable.",
                "Registered UMM disable probe did not begin with exactly one owned transient overlay.");
            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Native mod-disable pre-dispatch evidence could not be committed.");
                return;
            }

            modDisableEvidence.DisableCallbackSucceeded = registeredToggle(false);
            modDisableEvidence.OverlayReferenceAbsentImmediately = !playerAction.OverlayPresent;
            CaptureAndAssertCleanupLatch(CleanupTrigger.ModDisabled);
            AssertExactNativeCleanupDelivery(
                NativeLifecycleBoundary.ModDisable,
                CleanupTrigger.ModDisabled,
                "UnityModManager.ModEntry.OnToggle(false)/shutdown");
            assertions.Check(modDisableEvidence.DisableCallbackSucceeded == true,
                "Exact registered UMM OnToggle(false) delegate completed successfully.",
                "Exact registered UMM OnToggle(false) delegate rejected disable.");
            assertions.Check(modDisableEvidence.OverlayReferenceAbsentImmediately == true,
                "Transient player-action overlay ownership was released synchronously on disable.",
                "Transient player-action overlay remained owned after disable.");
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Native mod-disable cleanup-latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Registered UMM disable delivery retained residue or failed.");
                return;
            }

            boundaryFrame = frameNumber;
            step = EngineStep.AwaitSimpleCleanupFrame;
        }

        private void BeginExactWorkingLoad()
        {
            string identityError;
            FileIdentitySnapshot beforeDescriptorRead;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out beforeDescriptorRead, out identityError) ||
                !beforeDescriptorRead.Equals(postInitialLoadWorkingIdentity))
            {
                assertions.Fail("Exact Working identity differed from the captured post-initial-load identity before LoadZipSave: " +
                    (identityError ?? "length/timestamp/SHA-256 mismatch"));
                AbortSuite("Refused load because exact Working changed after the verified initial load.");
                return;
            }
            assertions.Check(true,
                "Exact Working length, timestamp, and SHA-256 matched the post-initial-load identity before descriptor read.",
                "Exact Working post-initial-load identity comparison unexpectedly failed.");

            SaveInfo descriptor;
            string descriptorError;
            if (!TryReadExactWorkingDescriptor(out descriptor, out descriptorError))
            {
                assertions.Fail("Exact Working descriptor could not be prepared for reload: " + descriptorError);
                AbortSuite("Refused load because the direct Working descriptor was not exact.");
                return;
            }
            assertions.Check(true,
                "Direct descriptor read opened only the exact Working path.",
                "Direct descriptor read unexpectedly failed.");
            descriptorVerificationObserved = true;
            descriptorVerifiedForRow = true;
            descriptorIdentityForRow = DescriptorIdentityEvidence.From(descriptor);

            FileIdentitySnapshot afterDescriptorRead;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out afterDescriptorRead, out identityError) ||
                !afterDescriptorRead.Equals(postInitialLoadWorkingIdentity))
            {
                assertions.Fail("Exact Working identity could not be verified after descriptor read: " +
                    (identityError ?? "length/timestamp/SHA-256 mismatch"));
                AbortSuite("Refused load because exact Working identity could not be recaptured after LoadZipSave.");
                return;
            }

            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Load boundary pre-dispatch evidence could not be committed.");
                return;
            }

            // This is intentionally after the durable pre-boundary record and
            // immediately before dispatch. It compares to the bytes observed
            // after the harness's first verified Working load, not to the stale
            // request hash that Kingmaker may have legitimately refreshed.
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out preDispatchWorkingIdentity, out identityError) ||
                !preDispatchWorkingIdentity.Equals(postInitialLoadWorkingIdentity))
            {
                assertions.Fail("Immediate pre-dispatch Working identity differed from the post-initial-load snapshot: " +
                    (identityError ?? "length/timestamp/SHA-256 mismatch"));
                AbortSuite("Refused exact Working load dispatch after immediate identity revalidation failed.");
                return;
            }

            Game.Instance.SaveManager.AddCallbackAfterLoad(HandleAsynchronousCallback);
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitLoadCompletion;
            realWorkingLoadDispatched = true;
            Game.Instance.LoadGame(descriptor);

            // LoadRoutine's exact-token Harmony prefix runs synchronously while
            // the old objects are still inspectable. Capture only primitives so
            // later validation never dereferences a disposed old-world view.
            CaptureAndAssertCleanupLatch(CleanupTrigger.LoadRequested);
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Load boundary cleanup latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Load boundary cleanup latch retained runtime residue.");
            }
        }

        private void BeginExactAreaReload()
        {
            assertions.Check(Game.Instance != null && Game.Instance.CurrentlyLoadedArea != null,
                "A loaded area was present for exact ReloadArea.",
                "ReloadArea was refused because no area was loaded.");
            if (Game.Instance == null || Game.Instance.CurrentlyLoadedArea == null)
            {
                AbortSuite("ReloadArea requires the verified loaded Working area.");
                return;
            }

            if (!TryWriteEvidence("pre-boundary", true, false, null, null))
            {
                AbortSuite("Area boundary pre-dispatch evidence could not be committed.");
                return;
            }

            lifecycle.OnAreaBeginUnloading();
            CaptureAndAssertCleanupLatch(CleanupTrigger.AreaUnloading);
            if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
            {
                AbortSuite("Area boundary cleanup latch evidence could not be committed.");
                return;
            }
            if (assertions.FailureCount != 0)
            {
                AbortSuite("Area boundary cleanup latch retained runtime residue; ReloadArea was suppressed.");
                return;
            }

            boundaryFrame = frameNumber;
            step = EngineStep.AwaitAreaCompletion;
            realAreaReloadDispatched = true;
            Game.Instance.ReloadArea();
        }

        private void AwaitSimpleCleanupFrame()
        {
            boundaryCleanupVerified = true;
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitSimpleCleanupFrame;
        }

        private void VerifySimpleCleanupAndFinish()
        {
            if (frameNumber <= boundaryFrame)
            {
                return;
            }
            if (string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal) &&
                (IsLoading() || !asynchronousCallback))
            {
                return;
            }

            if (string.Equals(currentRow, "presentation-residue-and-uninstall-safety", StringComparison.Ordinal))
            {
                modDisableEvidence.OverlayPresentOnDisabledFrame = playerAction.OverlayPresent;
                modDisableEvidence.OverlayObjectCountOnDisabledFrame = MountedPlayerActionController.CountOverlayObjects();
                assertions.Check(modDisableEvidence.OverlayPresentOnDisabledFrame == false &&
                        modDisableEvidence.OverlayObjectCountOnDisabledFrame == 0,
                    "Disabled-frame observation contained no transient player-action overlay object or owned reference.",
                    "Transient player-action overlay residue remained on the disabled frame.");
                modDisableEvidence.ReenableCallbackSucceeded = registeredToggle(true);
                modDisableEvidence.OverlayPresentAfterReenable = playerAction.OverlayPresent;
                modDisableEvidence.OverlayObjectCountAfterReenable = MountedPlayerActionController.CountOverlayObjects();
                assertions.Check(modDisableEvidence.ReenableCallbackSucceeded == true &&
                        modDisableEvidence.OverlayPresentAfterReenable == true &&
                        modDisableEvidence.OverlayObjectCountAfterReenable == 1,
                    "Exact registered UMM OnToggle(true) restored one transient overlay after the clean disabled frame.",
                    "Registered UMM re-enable did not restore exactly one transient overlay.");
            }

            AssertUnmountedAndRestored(snapshot);
            snapshot.AssertOverrideComponentCount(assertions);
            assertions.Check(CountKmcAnchorObjects() == 0,
                "No KMC rider anchor object remained on the post-boundary frame.",
                "A KMC rider anchor object remained on the post-boundary frame.");
            assertions.Check(CountKmcRiderMovementAgents() == 0,
                "No KMC RiderMovementAgent component remained anywhere in the live object graph.",
                "A KMC RiderMovementAgent component remained outside the restored pair.");
            AssertAuthorizationCounts(0, 0);
            AssertWorkingIdentityEquals(rowStartWorkingIdentity,
                "Exact Working file identity remained stable throughout the non-load boundary row.");
            if (string.Equals(currentRow, "mounted-pair-save-safety", StringComparison.Ordinal))
            {
                var after = new FileInfo(fixtureLoader.WorkingPath);
                assertions.Check(after.Exists && (after.Attributes & FileAttributes.ReparsePoint) == 0 &&
                    after.Length == fileLengthBefore && after.LastWriteTimeUtc.Ticks == fileWriteTicksBefore &&
                    string.Equals(ComputeSha256(fixtureLoader.WorkingPath), fileHashBefore, StringComparison.Ordinal),
                    "Save-safety probe left exact Working bytes and metadata unchanged.",
                    "Save-safety probe changed exact Working despite the no-serialization Phase 1 policy.");
            }
            if (string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal))
            {
                var after = new FileInfo(fixtureLoader.WorkingPath);
                assertions.Check(asynchronousCallback,
                    "Native Game.SaveGame pipeline completed its registered callback after suppression.",
                    "Native Game.SaveGame pipeline did not complete its registered callback.");
                assertions.Check(after.Exists && (after.Attributes & FileAttributes.ReparsePoint) == 0 &&
                        after.Length == fileLengthBefore && after.LastWriteTimeUtc.Ticks == fileWriteTicksBefore &&
                        string.Equals(ComputeSha256(fixtureLoader.WorkingPath), fileHashBefore, StringComparison.Ordinal),
                    "Native save probe left exact Working bytes and metadata unchanged because serialization was suppressed.",
                    "Native save probe changed exact Working bytes or metadata.");
            }
            if (string.Equals(currentRow, "native-mode-transition-cleanup", StringComparison.Ordinal))
            {
                assertions.Check(nativeModeEvidence != null && nativeModeEvidence.RestoreDeliveryCompleted == true &&
                        nativeModeEvidence.PersistedValueUnchanged == true,
                    "Native mode callback restored the exact cached value and left persisted settings unchanged.",
                    "Native mode callback did not prove exact cache and persisted-setting restoration.");
            }
            if (!TryWriteEvidence("post-boundary", true, false, null, null))
            {
                AbortSuite("Post-boundary evidence could not be committed.");
                return;
            }
            FinishCurrentRow();
        }

        private void VerifyLoadCompletion()
        {
            if (IsLoading())
            {
                loadingObserved = true;
                loadingStartObserved = true;
                stableWorldFrames = 0;
                if (!loadingStartEvidenceWritten)
                {
                    loadingStartEvidenceWritten = true;
                    if (!TryWriteEvidence("loading-start", true, false, null, null))
                    {
                        AbortSuite("Load boundary loading-start evidence could not be committed.");
                    }
                }
                return;
            }
            if (loadingStartObserved && !loadingStopEvidenceWritten)
            {
                loadingStopObserved = true;
                loadingStopEvidenceWritten = true;
                if (!asynchronousCallback)
                {
                    assertions.Fail("Exact Working loading pipeline stopped before its registered completion callback.");
                }
                if (!TryWriteEvidence("loading-stop", true, false, null, null))
                {
                    AbortSuite("Load boundary loading-stop evidence could not be committed.");
                    return;
                }
                if (!asynchronousCallback)
                {
                    AbortSuite("Load boundary stopped before callback; no fresh-world PASS may be claimed.");
                    return;
                }
            }
            if (!loadingStartObserved || !loadingStopObserved || !asynchronousCallback || !IsWorldReady())
            {
                stableWorldFrames = 0;
                return;
            }

            stableWorldFrames++;
            if (stableWorldFrames < StableWorldFramesRequired)
            {
                return;
            }

            assertions.Check(boundaryCleanupVerified,
                "Mounted cleanup completed before the exact Working load disposed live state.",
                "Mounted cleanup was not verified before the exact Working load.");
            assertions.Check(loadingObserved && loadingStartObserved && loadingStopObserved && asynchronousCallback,
                "Exact Working loading start, stop, and completion callback were all observed.",
                "Exact Working loading start, stop, or completion callback was not observed.");
            AssertAuthorizationCounts(1, 0);
            AssertLoadedFixtureIdentity();
            CaptureAndAssertFreshWorld("post-load");
            CaptureCompletedBoundaryWorkingIdentity(false);
            if (!TryWriteEvidence("fresh-world", true, false, null, null))
            {
                AbortSuite("Load boundary fresh-world evidence could not be committed.");
                return;
            }
            FinishCurrentRow();
        }

        private void VerifyAreaCompletion()
        {
            if (string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal))
            {
                ObserveNativeAreaBoundaryProgress();
                if (failureDrain.State != BoundaryFailureDrainState.Inactive)
                {
                    return;
                }
            }

            if (IsLoading())
            {
                stableWorldFrames = 0;
                if (!string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal))
                {
                    loadingObserved = true;
                    loadingStartObserved = true;
                    if (!loadingStartEvidenceWritten)
                    {
                        loadingStartEvidenceWritten = true;
                        if (!TryWriteEvidence("loading-start", true, false, null, null))
                        {
                            AbortSuite("Area boundary loading-start evidence could not be committed.");
                        }
                    }
                }
                return;
            }
            if (loadingStartObserved && !loadingStopEvidenceWritten)
            {
                loadingStopObserved = true;
                loadingStopEvidenceWritten = true;
                if (!TryWriteEvidence("loading-stop", true, false, null, null))
                {
                    AbortSuite("Area boundary loading-stop evidence could not be committed.");
                    return;
                }
            }
            if (!loadingObserved || !loadingStartObserved || !loadingStopObserved || !IsWorldReady() ||
                (string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal) &&
                 !HasNativeAreaStageOrder()))
            {
                stableWorldFrames = 0;
                return;
            }

            stableWorldFrames++;
            if (stableWorldFrames < StableWorldFramesRequired)
            {
                return;
            }

            assertions.Check(boundaryCleanupVerified,
                "Area-unload lifecycle cleanup completed before reload qualification.",
                "Area reload completed without observed mounted cleanup.");
            assertions.Check(loadingObserved && loadingStartObserved && loadingStopObserved,
                "Area reload loading start and stop were both observed.",
                "Area reload loading start or stop was not observed.");
            if (string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal))
            {
                AssertNativeAreaStageOrder();
            }
            AssertAuthorizationCounts(0, 0);
            AssertLoadedFixtureIdentity();
            CaptureAndAssertFreshWorld("post-area-reload");
            CaptureCompletedBoundaryWorkingIdentity(true);
            if (!TryWriteEvidence("fresh-world", true, false, null, null))
            {
                AbortSuite("Area boundary fresh-world evidence could not be committed.");
                return;
            }
            FinishCurrentRow();
        }

        private void ObserveNativeAreaBoundaryProgress()
        {
            if (nativeAreaProgress == null || (boundaryCleanupVerified && loadingStartEvidenceWritten))
            {
                return;
            }

            var cleanupDeliveryObserved = HasNativeBoundaryDelivery(
                NativeLifecycleBoundary.AreaBeginUnload,
                "ISceneHandler.OnAreaBeginUnloading");
            var decision = nativeAreaProgress.Observe(cleanupDeliveryObserved, IsLoading());
            if (decision.CaptureCleanupLatch)
            {
                CaptureAndAssertCleanupLatch(CleanupTrigger.AreaUnloading);
                AssertExactNativeCleanupDelivery(
                    NativeLifecycleBoundary.AreaBeginUnload,
                    CleanupTrigger.AreaUnloading,
                    "ISceneHandler.OnAreaBeginUnloading");
                if (!TryWriteEvidence("cleanup-latch", true, false, null, null))
                {
                    AbortSuite("Native area cleanup-latch evidence could not be committed.");
                    return;
                }
                if (assertions.FailureCount != 0)
                {
                    AbortSuite("Native area unload delivery retained residue or was not exact.");
                    return;
                }
            }

            if (decision.CaptureLoadingStart)
            {
                loadingObserved = true;
                loadingStartObserved = true;
                loadingStartEvidenceWritten = true;
                if (!TryWriteEvidence("loading-start", true, false, null, null))
                {
                    AbortSuite("Native area loading-start evidence could not be committed after cleanup delivery.");
                }
            }
        }

        private void AssertMountedAuthority()
        {
            assertions.Check(snapshot.RiderView.AgentASP == snapshot.RiderStockAgent && !snapshot.RiderStockAgent.enabled,
                "Rider retained its disabled exact stock-agent object while mounted.",
                "Rider stock-agent identity or enabled state did not match mounted authority.");
            assertions.Check(snapshot.RiderStockAgent.AvoidanceDisabled,
                "Rider avoidance was disabled under the owned lease.",
                "Rider avoidance was not disabled while mounted.");
            assertions.Check(snapshot.RiderView.AgentOverride == relationship.Runtime.MovementAgent && relationship.Runtime.MovementAgent != null,
                "Rider installed only the KMC-owned movement override.",
                "Rider did not expose the KMC-owned movement override.");
            assertions.Check(snapshot.RiderView.GetComponents<RiderMovementAgent>().Length == snapshot.RiderOverrideComponentCount + 1,
                "Mounted rider exposed exactly one KMC-owned RiderMovementAgent component.",
                "Mounted rider component count did not prove one owned movement override.");
            assertions.Check(CountKmcRiderMovementAgents() == 1,
                "The mounted rider owned the only live KMC RiderMovementAgent component.",
                "The live object graph did not contain exactly one KMC RiderMovementAgent while mounted.");
            assertions.Check(snapshot.RiderView.ForbidRotation,
                "Mounted rider held the scoped ForbidRotation lease.",
                "Mounted rider did not hold ForbidRotation.");
            assertions.Check(relationship.Runtime.PresentationAttachmentLeaseActive &&
                relationship.Runtime.RiderParentMatchesAttachment && relationship.Runtime.HasPresentationAttachmentResidue,
                "Mounted rider held the exact owned presentation attachment lease.",
                "Mounted rider did not expose the exact presentation attachment ownership state.");
            assertions.Check(snapshot.MountView.AgentASP == snapshot.MountStockAgent && snapshot.MountStockAgent.enabled,
                "Mount stock movement agent remained authoritative.",
                "Mount stock movement agent was changed or disabled.");
            assertions.Check(!snapshot.MountStockAgent.AvoidanceDisabled && ReferenceEquals(snapshot.MountView.AgentOverride, snapshot.MountOverride),
                "Mount retained ordinary avoidance and its exact prior override reference.",
                "Mount avoidance or override ownership changed while mounted.");
        }

        private void CaptureAndAssertCleanupLatch(CleanupTrigger expected)
        {
            cleanupLatch = BoundaryCleanupEvidence.Capture(frameNumber, expected, relationship, snapshot);
            boundaryCleanupVerified = cleanupLatch.AllRestored == true;
            assertions.Check(cleanupLatch.Captured && cleanupLatch.AllRestored == true,
                expected + " cleanup was synchronously latched with complete stock/presentation/selection restoration.",
                expected + " cleanup latch retained movement, presentation, rotation, command, or selection residue.");
        }

        private void AssertUnmountedAndRestored(PairSnapshot retained, bool allowDetachedViews = false)
        {
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship was Unmounted after the boundary.",
                "Relationship state after the boundary was " + relationship.State + ".");
            assertions.Check(relationship.Rider == null && relationship.Mount == null,
                "Prepared rider and mount references were released.",
                "Prepared rider or mount reference remained after the boundary.");
            assertions.Check(relationship.Runtime.MovementAgent == null,
                "KMC-owned movement override reference was released.",
                "KMC-owned movement override reference remained after the boundary.");

            if (retained == null)
            {
                assertions.Fail("No retained pre-mount pair snapshot was available for residue checks.");
                return;
            }
            if (allowDetachedViews && (retained.RiderView == null || retained.MountView == null))
            {
                assertions.Check(relationship.LastTransition != null && relationship.LastTransition.Succeeded &&
                    !relationship.LastTransition.MovementAuthorityResidual && !relationship.LastTransition.PresentationResidual,
                    "Cleanup completed without owned residue before boundary-driven view detachment.",
                    "Cleanup transition reported residue before boundary-driven view detachment.");
            }
            else
            {
                retained.AssertRestored(assertions);
            }
        }

        private void CaptureAndAssertFreshWorld(string phase)
        {
            string error;
            var resolved = relationship.TryResolveAutomationPair(out freshRider, out freshMount, out error);
            freshWorldEvidence = FreshWorldEvidence.Capture(
                IsWorldReady(),
                resolved,
                relationship,
                freshRider,
                freshMount,
                snapshot,
                request.Fixture.Working);
            assertions.Check(resolved,
                phase + " exact automation pair resolved from fresh live state.",
                phase + " exact automation pair did not resolve: " + (error ?? "unknown error"));
            assertions.Check(freshWorldEvidence.AllClean == true,
                phase + " fresh world contained exact Working identity and no relationship, movement, selection, rotation, component, or anchor residue.",
                phase + " fresh world retained KMC state or differed from the exact Working identity.");
        }

        private void AssertLoadedFixtureIdentity()
        {
            var game = Game.Instance;
            var working = request.Fixture.Working;
            var valid = game != null && game.Player != null && game.Player.MainCharacter.Value != null &&
                game.CurrentlyLoadedArea != null &&
                string.Equals(game.Player.GameId, working.GameId, StringComparison.Ordinal) &&
                string.Equals(game.Player.MainCharacter.Value.CharacterName, working.GameName, StringComparison.Ordinal) &&
                string.Equals(game.CurrentlyLoadedArea.AssetGuidThreadSafe, working.Area, StringComparison.Ordinal);
            assertions.Check(valid,
                "Loaded GameId, GameName, and Area remained the qualified Working identity.",
                "Loaded GameId, GameName, or Area differed from the qualified Working identity.");
        }

        private void AssertAuthorizationCounts(int expectedLoadDelta, int expectedWriteDelta)
        {
            assertions.Check(saveAuthorization.AuthorizedLoadCount - authorizedLoadsBefore == expectedLoadDelta,
                "Authorized Working load count changed by exactly " + expectedLoadDelta + ".",
                "Authorized Working load count delta was " + (saveAuthorization.AuthorizedLoadCount - authorizedLoadsBefore) +
                    " rather than " + expectedLoadDelta + ".");
            assertions.Check(saveAuthorization.AuthorizedWriteCount - authorizedWritesBefore == expectedWriteDelta,
                "Authorized Working write count changed by exactly " + expectedWriteDelta + ".",
                "Authorized Working write count delta was " + (saveAuthorization.AuthorizedWriteCount - authorizedWritesBefore) +
                    " rather than " + expectedWriteDelta + ".");
            assertions.Check(saveAuthorization.UnauthorizedLoadCount == unauthorizedLoadsBefore &&
                saveAuthorization.UnauthorizedWriteCount == unauthorizedWritesBefore,
                "No unauthorized load or write request occurred.",
                "An unauthorized load or write request occurred.");
            assertions.Check(saveAuthorization.BaselineLoadRequestCount == baselineLoadsBefore,
                "No Baseline load request occurred.",
                "A Baseline load request occurred.");
            assertions.Check(saveAuthorization.FatalViolationCount == fatalViolationsBefore,
                "No save-authorization fatal violation occurred.",
                "A save-authorization fatal violation occurred: " + (saveAuthorization.LastFatalViolation ?? "unspecified"));
            var expectedSuppressedDelta = string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal)
                ? 1
                : 0;
            assertions.Check(saveAuthorization.SuppressedWorkingWriteCount - suppressedWorkingWritesBefore == expectedSuppressedDelta &&
                    !saveAuthorization.IsOneShotWorkingWriteSuppressionArmed,
                "Suppressed Working-write count changed by exactly " + expectedSuppressedDelta + " and no suppression remains armed.",
                "Suppressed Working-write count or armed state differed from the exact row contract.");
        }

        private void CaptureAuthorizationCounts()
        {
            authorizedLoadsBefore = saveAuthorization.AuthorizedLoadCount;
            authorizedWritesBefore = saveAuthorization.AuthorizedWriteCount;
            unauthorizedLoadsBefore = saveAuthorization.UnauthorizedLoadCount;
            unauthorizedWritesBefore = saveAuthorization.UnauthorizedWriteCount;
            baselineLoadsBefore = saveAuthorization.BaselineLoadRequestCount;
            fatalViolationsBefore = saveAuthorization.FatalViolationCount;
            suppressedWorkingWritesBefore = saveAuthorization.SuppressedWorkingWriteCount;
        }

        private long LastNativeDeliverySequence()
        {
            var records = lifecycle.SnapshotNativeDeliveries();
            return records.Count == 0 ? 0L : records[records.Count - 1].Sequence;
        }

        private IReadOnlyList<NativeLifecycleDeliveryRecord> CurrentNativeDeliveries()
        {
            return lifecycle.SnapshotNativeDeliveries()
                .Where(record => record.Sequence > nativeDeliveryBaselineSequence)
                .ToArray();
        }

        private void AssertExactNativeCleanupDelivery(
            NativeLifecycleBoundary boundary,
            CleanupTrigger trigger,
            string source)
        {
            var matches = CurrentNativeDeliveries().Where(record =>
                record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal) &&
                record.CleanupTrigger == trigger).ToArray();
            var exact = matches.Length == 1 &&
                matches[0].StateBefore == RelationshipState.Mounted &&
                matches[0].StateAfter == RelationshipState.Unmounted &&
                matches[0].CleanupAttempted && matches[0].CleanupSucceeded;
            assertions.Check(exact,
                "Exact native " + boundary + " delivery recorded Mounted-to-Unmounted " + trigger + " cleanup.",
                "Native " + boundary + " delivery was missing, duplicated, or did not record exact successful cleanup.");
        }

        private void AssertNativeRestoreDelivery(
            NativeLifecycleBoundary boundary,
            CleanupTrigger trigger,
            string source)
        {
            var matches = CurrentNativeDeliveries().Where(record =>
                record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal) &&
                record.CleanupTrigger == trigger).ToArray();
            var exact = matches.Length == 1 &&
                matches[0].StateBefore == RelationshipState.Unmounted &&
                matches[0].StateAfter == RelationshipState.Unmounted &&
                matches[0].CleanupAttempted && matches[0].CleanupSucceeded;
            assertions.Check(exact,
                "Native restore-mode delivery was observed while already Unmounted.",
                "Native restore-mode delivery was missing, duplicated, or changed clean Unmounted state.");
        }

        private void AssertNativeAreaStageOrder()
        {
            var deliveries = CurrentNativeDeliveries();
            var unload = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaBeginUnload, "ISceneHandler.OnAreaBeginUnloading");
            var scenes = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaScenesLoaded, "IAreaLoadingStagesHandler.OnAreaScenesLoaded");
            var didLoad = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaDidLoad, "ISceneHandler.OnAreaDidLoad");
            var complete = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaLoadingComplete, "IAreaLoadingStagesHandler.OnAreaLoadingComplete");
            assertions.Check(unload > nativeDeliveryBaselineSequence && unload < scenes && scenes < didLoad && didLoad < complete,
                "Native area delivery order was begin-unload, scenes-loaded, area-did-load, loading-complete.",
                "Native area delivery stages were missing, duplicated, or out of order.");
        }

        private bool HasNativeAreaStageOrder()
        {
            var deliveries = CurrentNativeDeliveries();
            var unload = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaBeginUnload, "ISceneHandler.OnAreaBeginUnloading");
            var scenes = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaScenesLoaded, "IAreaLoadingStagesHandler.OnAreaScenesLoaded");
            var didLoad = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaDidLoad, "ISceneHandler.OnAreaDidLoad");
            var complete = FindNativeSequence(deliveries, NativeLifecycleBoundary.AreaLoadingComplete, "IAreaLoadingStagesHandler.OnAreaLoadingComplete");
            return unload > nativeDeliveryBaselineSequence && unload < scenes && scenes < didLoad && didLoad < complete;
        }

        private static long FindNativeSequence(
            IReadOnlyList<NativeLifecycleDeliveryRecord> records,
            NativeLifecycleBoundary boundary,
            string source)
        {
            var matches = records.Where(record => record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal)).ToArray();
            return matches.Length == 1 ? matches[0].Sequence : -1L;
        }

        private bool TryReadExactWorkingDescriptor(out SaveInfo descriptor, out string error)
        {
            descriptor = null;
            error = null;
            try
            {
                var game = Game.Instance;
                if (game == null || game.SaveManager == null)
                {
                    error = "Game or SaveManager was unavailable.";
                    return false;
                }

                var root = Path.GetFullPath(game.SaveManager.SavePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var expectedPath = Path.GetFullPath(Path.Combine(root, request.Fixture.Working.FileName));
                if (!string.Equals(expectedPath, Path.GetFullPath(fixtureLoader.WorkingPath), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Path.GetDirectoryName(expectedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Path.GetFileName(expectedPath), request.Fixture.Working.FileName, StringComparison.Ordinal))
                {
                    error = "Working path was not the exact direct child bound by the request.";
                    return false;
                }

                var file = new FileInfo(expectedPath);
                if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    error = "Working path was missing or a reparse point.";
                    return false;
                }

                descriptor = game.SaveManager.LoadZipSave(expectedPath);
                return VerifyExactWorkingDescriptor(descriptor, expectedPath, out error);
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                descriptor = null;
                return false;
            }
        }

        private bool VerifyExactWorkingDescriptor(SaveInfo descriptor, string expectedPath, out string error)
        {
            error = null;
            if (descriptor == null)
            {
                error = "SaveInfo was null.";
                return false;
            }

            var expected = request.Fixture.Working;
            var observedArea = descriptor.Area == null ? null : descriptor.Area.AssetGuidThreadSafe;
            if (!string.Equals(descriptor.Name, RuntimeRequest.WorkingSaveName, StringComparison.Ordinal) ||
                !string.Equals(descriptor.FileName, expected.FileName, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFullPath(descriptor.FolderName), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(descriptor.GameId, expected.GameId, StringComparison.Ordinal) ||
                !string.Equals(descriptor.GameName, expected.GameName, StringComparison.Ordinal) ||
                !string.Equals(observedArea, expected.Area, StringComparison.Ordinal) ||
                descriptor.Type != SaveInfo.SaveType.Manual || descriptor.CompatibilityVersion != 1)
            {
                error = "SaveInfo name/path/type/GameId/GameName/Area identity differed from exact Working.";
                return false;
            }

            return true;
        }

        private bool IsWorldReady()
        {
            var game = Game.Instance;
            return game != null && game.SaveManager != null && game.Player != null && game.CurrentlyLoadedArea != null &&
                game.CurrentMode == GameModeType.Default && !IsLoading();
        }

        private static bool IsLoading()
        {
            return LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess;
        }

        private void HandleAsynchronousCallback()
        {
            asynchronousCallback = true;
        }

        private bool TryWriteEvidence(
            string phase,
            bool executed,
            bool suppressed,
            string rowStatus,
            IReadOnlyList<string> recordErrors,
            int? assertionPassCount = null,
            int? assertionFailCount = null)
        {
            if (evidenceFailed)
            {
                if (assertions != null && !assertions.Errors.Contains(evidenceFailureMessage))
                {
                    assertions.Fail(evidenceFailureMessage);
                }
                return false;
            }

            try
            {
                var record = CreateEvidenceRecord(
                    phase,
                    executed,
                    suppressed,
                    rowStatus,
                    recordErrors,
                    assertionPassCount,
                    assertionFailCount);
                var json = JsonConvert.SerializeObject(record, EvidenceJsonSettings);
                evidenceSequenceGuard.Accept(currentRow, phase, rowStatus, executed, suppressed);
                evidenceJournal.AppendSerializedRecord(json);
                evidenceSequence++;
                return true;
            }
            catch (Exception exception)
            {
                evidenceFailed = true;
                evidenceFailureMessage = "Boundary structured evidence write failed: " +
                    exception.GetType().Name + ": " + exception.Message;
                if (assertions != null)
                {
                    assertions.Fail(evidenceFailureMessage);
                }
                else
                {
                    errors.Add(evidenceFailureMessage);
                }
                logger.Exception("Boundary structured evidence write failed", exception);
                return false;
            }
        }

        private bool CommitRowStartEvidence()
        {
            if (rowStartEvidenceWritten)
            {
                return true;
            }

            string identityError;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out rowStartWorkingIdentity, out identityError))
            {
                assertions.Fail("Boundary row-start Working identity could not be captured: " + identityError);
                return false;
            }

            var matchesCurrentAuthority = currentWorkingIdentityAuthority != null &&
                rowStartWorkingIdentity.Equals(currentWorkingIdentityAuthority);
            assertions.Check(matchesCurrentAuthority,
                "Boundary row-start Working identity continued exactly from the prior qualified segment.",
                "Boundary row-start Working identity did not continue from the post-initial-load or prior row-result authority.");

            if (!TryWriteEvidence("row-start", true, false, null, null))
            {
                return false;
            }

            rowStartEvidenceWritten = true;
            return matchesCurrentAuthority;
        }

        private void AssertWorkingIdentityEquals(FileIdentitySnapshot expected, string success)
        {
            FileIdentitySnapshot observed;
            string identityError = null;
            var exact = expected != null &&
                FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out observed, out identityError) &&
                observed.Equals(expected);
            assertions.Check(exact,
                success,
                "Exact Working file identity changed or could not be recaptured: " +
                    (identityError ?? "length/timestamp/SHA-256 mismatch"));
        }

        private void CaptureCompletedBoundaryWorkingIdentity(bool requireRowStartMatch)
        {
            string identityError;
            if (!FileIdentitySnapshot.TryCapture(
                fixtureLoader.WorkingPath,
                out completedBoundaryWorkingIdentity,
                out identityError))
            {
                assertions.Fail("Completed boundary Working identity could not be captured: " + identityError);
                return;
            }

            if (requireRowStartMatch)
            {
                assertions.Check(completedBoundaryWorkingIdentity.Equals(rowStartWorkingIdentity),
                    "Area reload left the row-start Working bytes and metadata unchanged.",
                    "Area reload changed Working bytes or metadata.");
            }
        }

        private BoundaryEvidenceRecord CreateEvidenceRecord(
            string phase,
            bool executed,
            bool suppressed,
            string rowStatus,
            IReadOnlyList<string> recordErrors,
            int? assertionPassCount,
            int? assertionFailCount)
        {
            string observedIdentitySource;
            var observed = CaptureOrSelectObservedWorkingIdentity(phase, out observedIdentitySource);

            UnitEntityData evidenceRider;
            UnitEntityData evidenceMount;
            var oldWorldMayBeDisposed = (realWorkingLoadDispatched || realAreaReloadDispatched) &&
                !string.Equals(phase, "cleanup-latch", StringComparison.Ordinal);
            if (freshRider != null || freshMount != null)
            {
                evidenceRider = freshRider;
                evidenceMount = freshMount;
            }
            else if (oldWorldMayBeDisposed)
            {
                evidenceRider = null;
                evidenceMount = null;
            }
            else
            {
                evidenceRider = snapshot == null ? relationship.Rider : snapshot.Rider;
                evidenceMount = snapshot == null ? relationship.Mount : snapshot.Mount;
            }
            var captureSelection = !oldWorldMayBeDisposed || evidenceRider != null || evidenceMount != null;
            var terminal = string.Equals(phase, "row-result", StringComparison.Ordinal);
            return new BoundaryEvidenceRecord
            {
                SchemaVersion = BoundaryScenarioEvidenceContract.SchemaVersion,
                ArtifactKind = BoundaryScenarioEvidenceContract.ArtifactKind,
                RunId = request.RunId,
                Scenario = request.Scenario,
                Row = currentRow,
                Phase = phase,
                UtcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = dllSha256,
                DllMvid = dllMvid,
                Sequence = evidenceSequence,
                RowIndex = rowIndex,
                Frame = frameNumber,
                Executed = executed,
                Suppressed = suppressed,
                RowStatus = terminal ? rowStatus : null,
                AssertionPassCount = terminal
                    ? (assertionPassCount ?? (assertions == null ? 0 : assertions.PassCount))
                    : (int?)null,
                AssertionFailCount = terminal
                    ? (assertionFailCount ?? (assertions == null ? 0 : assertions.FailureCount))
                    : (int?)null,
                TriggerScope = CreateTriggerScope(),
                WorkingIdentity = WorkingIdentityEvidence.Create(
                    request.Fixture.Working,
                    fixtureLoader.WorkingPath,
                    postInitialLoadWorkingIdentity,
                    preDispatchWorkingIdentity,
                    observed,
                    observedIdentitySource,
                    descriptorVerificationObserved ? (bool?)descriptorVerifiedForRow : null,
                    descriptorIdentityForRow),
                Authorization = AuthorizationEvidence.Capture(
                    saveAuthorization,
                    authorizedLoadsBefore,
                    authorizedWritesBefore,
                    unauthorizedLoadsBefore,
                    unauthorizedWritesBefore,
                    baselineLoadsBefore,
                    fatalViolationsBefore,
                    suppressedWorkingWritesBefore),
                Loading = new LoadingEvidence
                {
                    Observed = loadingObserved,
                    StartObserved = loadingStartObserved,
                    StopObserved = loadingStopObserved,
                    CallbackObserved = asynchronousCallback
                },
                Relationship = RelationshipEvidence.Capture(
                    relationship,
                    evidenceRider,
                    evidenceMount,
                    captureSelection),
                Cleanup = cleanupLatch ?? BoundaryCleanupEvidence.NotCaptured(ExpectedCleanupTrigger()),
                FreshWorld = freshWorldEvidence ?? FreshWorldEvidence.NotObserved(),
                NativeLifecycle = NativeLifecycleEvidence.Capture(
                    nativeDeliveryBaselineSequence,
                    CurrentNativeDeliveries()),
                NativeMode = nativeModeEvidence ?? NativeModeProbeEvidence.NotExecuted(),
                ModDisable = modDisableEvidence ?? ModDisableProbeEvidence.NotExecuted(),
                RecordErrors = recordErrors == null ? new string[0] : recordErrors.ToArray()
            };
        }

        private FileIdentitySnapshot CaptureOrSelectObservedWorkingIdentity(
            string phase,
            out string observedIdentitySource)
        {
            if (string.Equals(phase, "row-start", StringComparison.Ordinal))
            {
                if (rowStartWorkingIdentity == null)
                {
                    throw new IOException("Exact row-start Working identity was unavailable for its durable evidence record.");
                }

                // This is the live snapshot captured immediately before the
                // first durable row record, not a later recapture.
                observedIdentitySource = phase;
                return rowStartWorkingIdentity;
            }

            var observation = BoundaryScenarioEvidenceContract.SelectWorkingIdentityObservation(
                currentRow,
                phase,
                realWorkingLoadDispatched,
                realAreaReloadDispatched);
            if (observation == BoundaryWorkingIdentityObservation.CachedImmediatePreDispatch)
            {
                if (preDispatchWorkingIdentity == null)
                {
                    throw new IOException("Exact pre-dispatch Working identity was unavailable for the active load phase.");
                }

                // Reopening or hashing the archive while Kingmaker owns its load
                // pipeline can contend with the reader or observe a torn file.
                // This exact snapshot was captured immediately before dispatch;
                // its explicit source prevents it from being mistaken for a
                // contemporaneous observation.
                observedIdentitySource = BoundaryScenarioEvidenceContract.CachedImmediatePreDispatchSource;
                return preDispatchWorkingIdentity;
            }

            if (observation == BoundaryWorkingIdentityObservation.CachedRowStart)
            {
                if (rowStartWorkingIdentity == null)
                {
                    throw new IOException("Exact row-start Working identity was unavailable for the active area-load phase.");
                }

                observedIdentitySource = BoundaryScenarioEvidenceContract.CachedRowStartSource;
                return rowStartWorkingIdentity;
            }

            FileIdentitySnapshot observed;
            string identityError;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out observed, out identityError))
            {
                throw new IOException("Exact Working identity capture failed for " + phase + ": " + identityError + ".");
            }

            observedIdentitySource = ObservedIdentitySource(phase);
            return observed;
        }

        private TriggerScopeEvidence CreateTriggerScope()
        {
            var scope = new TriggerScopeEvidence
            {
                ExpectedCleanupTrigger = ExpectedCleanupTrigger().ToString(),
                NativeDeliveryObserved = false,
                StockSaveRoutineInvoked = realWorkingSaveDispatched,
                RealWorkingSaveDispatched = realWorkingSaveDispatched,
                RealWorkingLoadDispatched = realWorkingLoadDispatched,
                RealAreaReloadDispatched = realAreaReloadDispatched
            };

            if (string.Equals(currentRow, "mounted-pair-turn-based-entry-cleanup", StringComparison.Ordinal))
            {
                scope.InvocationPath = "mounted-lifecycle-handler-direct";
                scope.ClaimLimit = "Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.";
            }
            else if (string.Equals(currentRow, "mounted-pair-realtime-entry-cleanup", StringComparison.Ordinal))
            {
                scope.InvocationPath = "mounted-lifecycle-handler-direct";
                scope.ClaimLimit = "Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.";
            }
            else if (string.Equals(currentRow, "mounted-pair-save-safety", StringComparison.Ordinal))
            {
                scope.InvocationPath = "relationship-guard-boundary-direct";
                scope.ClaimLimit = "Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.";
            }
            else if (string.Equals(currentRow, "mounted-pair-load-safety", StringComparison.Ordinal))
            {
                scope.InvocationPath = "game-loadgame-exact-working";
                // Authorization is called only from the exact-token LoadRoutine
                // prefix, so its row-local delta proves native prefix delivery;
                // dispatch intent remains a separate field.
                scope.NativeDeliveryObserved = saveAuthorization.AuthorizedLoadCount - authorizedLoadsBefore > 0;
                scope.ClaimLimit = "Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.";
            }
            else if (string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal))
            {
                scope.InvocationPath = "game-savegame-exact-working+one-shot-prefix-suppression";
                scope.NativeDeliveryObserved = HasExactNativeCleanupDelivery(
                    NativeLifecycleBoundary.SaveRequest,
                    CleanupTrigger.SaveRequested,
                    "SaveManager.SaveRoutine Harmony12 prefix");
                scope.ClaimLimit = "Real Game.SaveGame entered the exact SaveRoutine Harmony12 prefix and callback pipeline; a one-shot exact-Working diagnostic guard suppressed the stock iterator body before serialization, so no disk write or save UI delivery is claimed.";
            }
            else if (string.Equals(currentRow, "native-area-clean-dismount", StringComparison.Ordinal))
            {
                scope.InvocationPath = "game-reloadarea+native-eventbus-area-stages";
                scope.NativeDeliveryObserved = HasExactNativeCleanupDelivery(
                    NativeLifecycleBoundary.AreaBeginUnload,
                    CleanupTrigger.AreaUnloading,
                    "ISceneHandler.OnAreaBeginUnloading");
                scope.ClaimLimit = "Real Game.ReloadArea exercised native EventBus area-unload and loading-stage delivery in the exact Working fixture; no cross-area destination transition or UI command was exercised.";
            }
            else if (string.Equals(currentRow, "native-mode-transition-cleanup", StringComparison.Ordinal))
            {
                scope.InvocationPath = "settings-oninvokeupdatecallback+gamesettingscontroller-eventbus";
                scope.NativeDeliveryObserved = currentExpectedCleanupTrigger.HasValue && HasExactNativeCleanupDelivery(
                    currentExpectedCleanupTrigger.Value == CleanupTrigger.TurnBasedModeChanged
                        ? NativeLifecycleBoundary.TurnBasedEnabled
                        : NativeLifecycleBoundary.RealtimeEnabled,
                    currentExpectedCleanupTrigger.Value,
                    "ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(" +
                        (currentExpectedCleanupTrigger.Value == CleanupTrigger.TurnBasedModeChanged) + ")");
                scope.ClaimLimit = "Diagnostic-only in-memory SettingsEntityBool cache substitution invoked the exact registered GameSettingsController callback and EventBus path, then restored it; no SettingsProvider/PlayerPrefs write or settings-UI click is claimed.";
            }
            else if (string.Equals(currentRow, "presentation-residue-and-uninstall-safety", StringComparison.Ordinal))
            {
                scope.InvocationPath = "registered-umm-ontoggle-delegate(false)+registered-umm-ontoggle-delegate(true)";
                scope.NativeDeliveryObserved = HasExactNativeCleanupDelivery(
                    NativeLifecycleBoundary.ModDisable,
                    CleanupTrigger.ModDisabled,
                    "UnityModManager.ModEntry.OnToggle(false)/shutdown");
                scope.ClaimLimit = "The exact registered Unity Mod Manager OnToggle delegate was invoked diagnostically for disable and re-enable; a user click in the UMM manager and physical file deletion were not exercised.";
            }
            else
            {
                scope.InvocationPath = "lifecycle-area-precleanup-direct+game-reloadarea";
                scope.ClaimLimit = "Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.";
            }

            return scope;
        }

        private CleanupTrigger ExpectedCleanupTrigger()
        {
            if (currentExpectedCleanupTrigger.HasValue)
            {
                return currentExpectedCleanupTrigger.Value;
            }
            if (string.Equals(currentRow, "mounted-pair-turn-based-entry-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.TurnBasedModeChanged;
            }
            if (string.Equals(currentRow, "mounted-pair-realtime-entry-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.RealtimeModeChanged;
            }
            if (string.Equals(currentRow, "mounted-pair-save-safety", StringComparison.Ordinal))
            {
                return CleanupTrigger.SaveRequested;
            }
            if (string.Equals(currentRow, "mounted-pair-load-safety", StringComparison.Ordinal))
            {
                return CleanupTrigger.LoadRequested;
            }
            if (string.Equals(currentRow, "native-save-clean-dismount", StringComparison.Ordinal))
            {
                return CleanupTrigger.SaveRequested;
            }
            if (string.Equals(currentRow, "native-mode-transition-cleanup", StringComparison.Ordinal))
            {
                return CleanupTrigger.TurnBasedModeChanged;
            }
            if (string.Equals(currentRow, "presentation-residue-and-uninstall-safety", StringComparison.Ordinal))
            {
                return CleanupTrigger.ModDisabled;
            }
            return CleanupTrigger.AreaUnloading;
        }

        private bool HasExactNativeCleanupDelivery(
            NativeLifecycleBoundary boundary,
            CleanupTrigger trigger,
            string source)
        {
            var matches = CurrentNativeDeliveries().Where(record =>
                record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal) &&
                record.CleanupTrigger == trigger &&
                record.StateBefore == RelationshipState.Mounted &&
                record.StateAfter == RelationshipState.Unmounted &&
                record.CleanupAttempted && record.CleanupSucceeded).ToArray();
            return matches.Length == 1;
        }

        private bool HasNativeBoundaryDelivery(
            NativeLifecycleBoundary boundary,
            string source)
        {
            return CurrentNativeDeliveries().Any(record =>
                record.Boundary == boundary &&
                string.Equals(record.Source, source, StringComparison.Ordinal));
        }

        private static string ObservedIdentitySource(string phase)
        {
            if (string.Equals(phase, "pre-boundary", StringComparison.Ordinal))
            {
                return "immediate-pre-dispatch";
            }
            if (string.Equals(phase, "cleanup-latch", StringComparison.Ordinal))
            {
                return "immediate-post-dispatch";
            }
            return phase;
        }

        private static int CountKmcAnchorObjects()
        {
            var count = 0;
            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform != null && string.Equals(transform.name, AnchorObjectName, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountKmcRiderMovementAgents()
        {
            var count = 0;
            foreach (var movementAgent in Resources.FindObjectsOfTypeAll<RiderMovementAgent>())
            {
                if (movementAgent != null)
                {
                    count++;
                }
            }
            return count;
        }

        private void AbortSuite(string message)
        {
            if (assertions == null)
            {
                assertions = new AssertionRecorder();
            }
            if (failureDrain.State == BoundaryFailureDrainState.Inactive)
            {
                if (currentRow != null && !rowStartEvidenceWritten && !evidenceFailed)
                {
                    CommitRowStartEvidence();
                }
                assertions.Fail(message);
                failureDrain.Request(message, IsLoading());
                rowClock.Stop();
                if (failureDrain.State == BoundaryFailureDrainState.DrainingActiveLoad)
                {
                    logger.Warning("Boundary failure is pending until the active Kingmaker loading pipeline stops: " + message);
                    return;
                }
            }

            DrainPendingFailure();
        }

        private void DrainPendingFailure()
        {
            if (failureDrain.Observe(IsLoading()) == BoundaryFailureDrainState.DrainingActiveLoad)
            {
                return;
            }
            if (failureDrain.State != BoundaryFailureDrainState.ReadyToFinalize)
            {
                return;
            }

            // This is the only failure-finalization path. It is deliberately
            // unreachable while LoadingProcess remains active so cleanup cannot
            // race Kingmaker's entity/view disposal and the host cannot observe
            // IsCompleted early and quit the process under an active load.
            var cleanupSucceeded = BestEffortCleanup();
            assertions.Check(cleanupSucceeded &&
                (relationship.State == RelationshipState.Unmounted || relationship.State == RelationshipState.Disposed) &&
                relationship.Rider == null && relationship.Mount == null && relationship.Runtime.MovementAgent == null,
                "Pending boundary failure drained, then cleanup left no KMC relationship residue.",
                "Pending boundary failure drained, but cleanup left KMC relationship residue.");
            if (snapshot != null && relationship.State != RelationshipState.Disposed)
            {
                AssertUnmountedAndRestored(snapshot, true);
            }
            CompleteRemainingAsNotRun("Boundary suite stopped after a safety-significant row failure: " + failureDrain.Failure);
            CompleteCore();
        }

        private bool BestEffortCleanup()
        {
            if (IsLoading())
            {
                return false;
            }

            try
            {
                saveSuppressionLease?.Dispose();
                saveSuppressionLease = null;
                if (nativeModeProbe != null)
                {
                    nativeModeProbe.Dispose();
                    nativeModeEvidence = NativeModeProbeEvidence.Capture(nativeModeProbe);
                    nativeModeProbe = null;
                }
                if (modDisableEvidence != null && modDisableEvidence.Executed &&
                    modDisableEvidence.DisableCallbackSucceeded == true &&
                    modDisableEvidence.ReenableCallbackSucceeded != true)
                {
                    modDisableEvidence.ReenableCallbackSucceeded = registeredToggle(true);
                    modDisableEvidence.OverlayPresentAfterReenable = playerAction.OverlayPresent;
                    modDisableEvidence.OverlayObjectCountAfterReenable = MountedPlayerActionController.CountOverlayObjects();
                }
                if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    {
                        errors.Add("Boundary-engine best-effort cleanup retained mounted runtime residue.");
                        return false;
                    }
                }
                return relationship.State == RelationshipState.Unmounted || relationship.State == RelationshipState.Disposed;
            }
            catch (Exception exception)
            {
                errors.Add("Boundary-engine best-effort cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Boundary-engine best-effort cleanup threw", exception);
                return false;
            }
        }

        private void FinishCurrentRow()
        {
            var failed = FinalizeCurrentRowResult();
            if (failed)
            {
                var reason = "Boundary suite stopped after a row assertion or evidence failure.";
                CompleteRemainingAsNotRun(reason);
                CompleteCore();
            }
        }

        private bool FinalizeCurrentRowResult()
        {
            if (rowStartWorkingIdentity != null)
            {
                var expectedFinalIdentity = string.Equals(currentRow, "mounted-pair-load-safety", StringComparison.Ordinal)
                    ? completedBoundaryWorkingIdentity
                    : rowStartWorkingIdentity;
                if (expectedFinalIdentity != null)
                {
                    AssertWorkingIdentityEquals(expectedFinalIdentity,
                        "Exact Working file identity remained stable through row-result finalization.");
                }
            }

            var statusBeforeEvidence = assertions.FailureCount == 0 ? "PASS" : "FAIL";
            TryWriteEvidence(
                "row-result",
                true,
                false,
                statusBeforeEvidence,
                assertions.Errors,
                assertions.PassCount,
                assertions.FailureCount);

            var result = new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };
            results.Add(result);
            if (string.Equals(result.Status, "PASS", StringComparison.Ordinal))
            {
                currentWorkingIdentityAuthority = string.Equals(currentRow, "mounted-pair-load-safety", StringComparison.Ordinal)
                    ? completedBoundaryWorkingIdentity
                    : rowStartWorkingIdentity;
            }
            foreach (var error in assertions.Errors)
            {
                errors.Add(currentRow + ": " + error);
            }

            if (string.Equals(result.Status, "PASS", StringComparison.Ordinal))
            {
                logger.Info("Boundary runtime row PASS: " + currentRow + " (" + assertions.PassCount + " assertions).");
            }
            else
            {
                logger.Warning("Boundary runtime row FAIL: " + currentRow + " (" + assertions.FailureCount + " failed assertions).");
            }

            rowIndex++;
            currentRow = null;
            assertions = null;
            snapshot = null;
            freshRider = null;
            freshMount = null;
            rowClock.Reset();
            step = EngineStep.BeginRow;
            return !string.Equals(result.Status, "PASS", StringComparison.Ordinal);
        }

        private void CompleteRemainingAsNotRun(string reason)
        {
            if (currentRow != null)
            {
                FinalizeCurrentRowResult();
            }
            while (rowIndex < selectedRows.Count)
            {
                currentRow = selectedRows[rowIndex];
                assertions = new AssertionRecorder();
                snapshot = null;
                freshRider = null;
                freshMount = null;
                rowStartWorkingIdentity = null;
                preDispatchWorkingIdentity = null;
                completedBoundaryWorkingIdentity = null;
                cleanupLatch = null;
                freshWorldEvidence = null;
                descriptorIdentityForRow = null;
                descriptorVerificationObserved = false;
                descriptorVerifiedForRow = false;
                realWorkingLoadDispatched = false;
                realWorkingSaveDispatched = false;
                realAreaReloadDispatched = false;
                currentExpectedCleanupTrigger = null;
                nativeModeEvidence = null;
                modDisableEvidence = null;
                nativeAreaProgress = null;
                nativeDeliveryBaselineSequence = LastNativeDeliverySequence();
                asynchronousCallback = false;
                loadingObserved = false;
                loadingStartObserved = false;
                loadingStopObserved = false;
                rowStartEvidenceWritten = false;
                CaptureAuthorizationCounts();
                TryWriteEvidence("row-result", false, true, "FAIL", new[] { reason }, 0, 1);
                var row = currentRow;
                results.Add(new RuntimeSubscenarioResult
                {
                    Name = row,
                    Status = "FAIL",
                    AssertionPassCount = 0,
                    AssertionFailCount = 1,
                    Errors = new[] { reason }
                });
                errors.Add(row + ": " + reason);
                rowIndex++;
                currentRow = null;
                assertions = null;
            }
        }

        private void Complete()
        {
            if (IsLoading())
            {
                AbortSuite("Boundary engine reached completion while Kingmaker loading was still active.");
                return;
            }

            CompleteCore();
        }

        private void CompleteCore()
        {
            BestEffortCleanup();
            if (evidenceSequenceGuard != null && !evidenceSequenceGuard.IsComplete && !evidenceFailed)
            {
                errors.Add("Boundary evidence ended before every selected row received a terminal row-result.");
            }
            RestoreSettings();
            suiteClock.Stop();
            rowClock.Stop();
            completed = true;
            logger.Info("Boundary runtime engine completed with " + results.Count + " row result(s).");
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
            return BoundaryScenarioEvidenceContract.SelectRows(scenario);
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

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeBoundaryScenarioEngine));
            }
        }

        private enum EngineStep
        {
            BeginRow,
            AwaitMountedFrame,
            AwaitSimpleCleanupFrame,
            AwaitLoadCompletion,
            AwaitAreaCompletion
        }

        private sealed class BoundaryEvidenceRecord
        {
            public int SchemaVersion { get; set; }
            public string ArtifactKind { get; set; }
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
            public int RowIndex { get; set; }
            public int Frame { get; set; }
            public bool Executed { get; set; }
            public bool Suppressed { get; set; }
            public string RowStatus { get; set; }
            public int? AssertionPassCount { get; set; }
            public int? AssertionFailCount { get; set; }
            public TriggerScopeEvidence TriggerScope { get; set; }
            public WorkingIdentityEvidence WorkingIdentity { get; set; }
            public AuthorizationEvidence Authorization { get; set; }
            public LoadingEvidence Loading { get; set; }
            public RelationshipEvidence Relationship { get; set; }
            public BoundaryCleanupEvidence Cleanup { get; set; }
            public FreshWorldEvidence FreshWorld { get; set; }
            public NativeLifecycleEvidence NativeLifecycle { get; set; }
            public NativeModeProbeEvidence NativeMode { get; set; }
            public ModDisableProbeEvidence ModDisable { get; set; }
            public IReadOnlyList<string> RecordErrors { get; set; }
        }

        private sealed class TriggerScopeEvidence
        {
            public string ExpectedCleanupTrigger { get; set; }
            public string InvocationPath { get; set; }
            public bool NativeDeliveryObserved { get; set; }
            public bool StockSaveRoutineInvoked { get; set; }
            public bool RealWorkingSaveDispatched { get; set; }
            public bool RealWorkingLoadDispatched { get; set; }
            public bool RealAreaReloadDispatched { get; set; }
            public string ClaimLimit { get; set; }
        }

        private sealed class WorkingIdentityEvidence
        {
            public string InternalName { get; set; }
            public string FileName { get; set; }
            public string Path { get; set; }
            public string GameId { get; set; }
            public string GameName { get; set; }
            public string Area { get; set; }
            public long RequestLength { get; set; }
            public long RequestLastWriteTimeUtcTicks { get; set; }
            public string RequestSha256 { get; set; }
            public long PostInitialLoadLength { get; set; }
            public long PostInitialLoadLastWriteTimeUtcTicks { get; set; }
            public string PostInitialLoadSha256 { get; set; }
            public long? PreDispatchLength { get; set; }
            public long? PreDispatchLastWriteTimeUtcTicks { get; set; }
            public string PreDispatchSha256 { get; set; }
            public long? ObservedLength { get; set; }
            public long? ObservedLastWriteTimeUtcTicks { get; set; }
            public string ObservedSha256 { get; set; }
            public string ObservedSource { get; set; }
            public bool? MatchesPostInitialLoad { get; set; }
            public bool? DescriptorVerified { get; set; }
            public string DescriptorInternalName { get; set; }
            public string DescriptorFileName { get; set; }
            public string DescriptorPath { get; set; }
            public string DescriptorGameId { get; set; }
            public string DescriptorGameName { get; set; }
            public string DescriptorArea { get; set; }
            public string DescriptorSaveType { get; set; }
            public int? DescriptorCompatibilityVersion { get; set; }

            public static WorkingIdentityEvidence Create(
                RuntimeSaveDescriptor requested,
                string path,
                FileIdentitySnapshot postInitial,
                FileIdentitySnapshot preDispatch,
                FileIdentitySnapshot observed,
                string observedSource,
                bool? descriptorVerified,
                DescriptorIdentityEvidence descriptor)
            {
                return new WorkingIdentityEvidence
                {
                    InternalName = requested.InternalName,
                    FileName = requested.FileName,
                    Path = path,
                    GameId = requested.GameId,
                    GameName = requested.GameName,
                    Area = requested.Area,
                    RequestLength = requested.Length,
                    RequestLastWriteTimeUtcTicks = requested.LastWriteTimeUtcTicks,
                    RequestSha256 = requested.Sha256,
                    PostInitialLoadLength = postInitial.Length,
                    PostInitialLoadLastWriteTimeUtcTicks = postInitial.LastWriteTimeUtcTicks,
                    PostInitialLoadSha256 = postInitial.Sha256,
                    PreDispatchLength = preDispatch == null ? (long?)null : preDispatch.Length,
                    PreDispatchLastWriteTimeUtcTicks = preDispatch == null ? (long?)null : preDispatch.LastWriteTimeUtcTicks,
                    PreDispatchSha256 = preDispatch == null ? null : preDispatch.Sha256,
                    ObservedLength = observed == null ? (long?)null : observed.Length,
                    ObservedLastWriteTimeUtcTicks = observed == null ? (long?)null : observed.LastWriteTimeUtcTicks,
                    ObservedSha256 = observed == null ? null : observed.Sha256,
                    ObservedSource = observedSource,
                    MatchesPostInitialLoad = observed == null ? (bool?)null : observed.Equals(postInitial),
                    DescriptorVerified = descriptorVerified,
                    DescriptorInternalName = descriptor == null ? null : descriptor.InternalName,
                    DescriptorFileName = descriptor == null ? null : descriptor.FileName,
                    DescriptorPath = descriptor == null ? null : descriptor.Path,
                    DescriptorGameId = descriptor == null ? null : descriptor.GameId,
                    DescriptorGameName = descriptor == null ? null : descriptor.GameName,
                    DescriptorArea = descriptor == null ? null : descriptor.Area,
                    DescriptorSaveType = descriptor == null ? null : descriptor.SaveType,
                    DescriptorCompatibilityVersion = descriptor == null ? (int?)null : descriptor.CompatibilityVersion
                };
            }
        }

        private sealed class DescriptorIdentityEvidence
        {
            public string InternalName { get; set; }
            public string FileName { get; set; }
            public string Path { get; set; }
            public string GameId { get; set; }
            public string GameName { get; set; }
            public string Area { get; set; }
            public string SaveType { get; set; }
            public int CompatibilityVersion { get; set; }

            public static DescriptorIdentityEvidence From(SaveInfo descriptor)
            {
                return new DescriptorIdentityEvidence
                {
                    InternalName = descriptor.Name,
                    FileName = descriptor.FileName,
                    Path = descriptor.FolderName,
                    GameId = descriptor.GameId,
                    GameName = descriptor.GameName,
                    Area = descriptor.Area == null ? null : descriptor.Area.AssetGuidThreadSafe,
                    SaveType = descriptor.Type.ToString(),
                    CompatibilityVersion = descriptor.CompatibilityVersion
                };
            }
        }

        private sealed class AuthorizationEvidence
        {
            public int AuthorizedLoadsBefore { get; set; }
            public int AuthorizedLoadsAfter { get; set; }
            public int AuthorizedLoadsDelta { get; set; }
            public int AuthorizedWritesBefore { get; set; }
            public int AuthorizedWritesAfter { get; set; }
            public int AuthorizedWritesDelta { get; set; }
            public int UnauthorizedLoadsBefore { get; set; }
            public int UnauthorizedLoadsAfter { get; set; }
            public int UnauthorizedLoadsDelta { get; set; }
            public int UnauthorizedWritesBefore { get; set; }
            public int UnauthorizedWritesAfter { get; set; }
            public int UnauthorizedWritesDelta { get; set; }
            public int BaselineLoadsBefore { get; set; }
            public int BaselineLoadsAfter { get; set; }
            public int BaselineLoadsDelta { get; set; }
            public int FatalViolationsBefore { get; set; }
            public int FatalViolationsAfter { get; set; }
            public int FatalViolationsDelta { get; set; }
            public int SuppressedWorkingWritesBefore { get; set; }
            public int SuppressedWorkingWritesAfter { get; set; }
            public int SuppressedWorkingWritesDelta { get; set; }
            public bool OneShotWorkingWriteSuppressionArmed { get; set; }

            public static AuthorizationEvidence Capture(
                RuntimeSaveAuthorization authorization,
                int authorizedLoadsBefore,
                int authorizedWritesBefore,
                int unauthorizedLoadsBefore,
                int unauthorizedWritesBefore,
                int baselineLoadsBefore,
                int fatalViolationsBefore,
                int suppressedWorkingWritesBefore)
            {
                return new AuthorizationEvidence
                {
                    AuthorizedLoadsBefore = authorizedLoadsBefore,
                    AuthorizedLoadsAfter = authorization.AuthorizedLoadCount,
                    AuthorizedLoadsDelta = authorization.AuthorizedLoadCount - authorizedLoadsBefore,
                    AuthorizedWritesBefore = authorizedWritesBefore,
                    AuthorizedWritesAfter = authorization.AuthorizedWriteCount,
                    AuthorizedWritesDelta = authorization.AuthorizedWriteCount - authorizedWritesBefore,
                    UnauthorizedLoadsBefore = unauthorizedLoadsBefore,
                    UnauthorizedLoadsAfter = authorization.UnauthorizedLoadCount,
                    UnauthorizedLoadsDelta = authorization.UnauthorizedLoadCount - unauthorizedLoadsBefore,
                    UnauthorizedWritesBefore = unauthorizedWritesBefore,
                    UnauthorizedWritesAfter = authorization.UnauthorizedWriteCount,
                    UnauthorizedWritesDelta = authorization.UnauthorizedWriteCount - unauthorizedWritesBefore,
                    BaselineLoadsBefore = baselineLoadsBefore,
                    BaselineLoadsAfter = authorization.BaselineLoadRequestCount,
                    BaselineLoadsDelta = authorization.BaselineLoadRequestCount - baselineLoadsBefore,
                    FatalViolationsBefore = fatalViolationsBefore,
                    FatalViolationsAfter = authorization.FatalViolationCount,
                    FatalViolationsDelta = authorization.FatalViolationCount - fatalViolationsBefore,
                    SuppressedWorkingWritesBefore = suppressedWorkingWritesBefore,
                    SuppressedWorkingWritesAfter = authorization.SuppressedWorkingWriteCount,
                    SuppressedWorkingWritesDelta = authorization.SuppressedWorkingWriteCount - suppressedWorkingWritesBefore,
                    OneShotWorkingWriteSuppressionArmed = authorization.IsOneShotWorkingWriteSuppressionArmed
                };
            }
        }

        private sealed class LoadingEvidence
        {
            public bool Observed { get; set; }
            public bool StartObserved { get; set; }
            public bool StopObserved { get; set; }
            public bool CallbackObserved { get; set; }
        }

        private sealed class RelationshipEvidence
        {
            public string State { get; set; }
            public string RiderUniqueId { get; set; }
            public string MountUniqueId { get; set; }
            public bool OwnerReferencesPresent { get; set; }
            public bool MovementAgentPresent { get; set; }
            public bool? RiderStockAgentEnabled { get; set; }
            public bool? MountStockAgentEnabled { get; set; }
            public bool? RiderAvoidanceDisabled { get; set; }
            public bool? MountAvoidanceDisabled { get; set; }
            public bool? RiderOverridePresent { get; set; }
            public bool? MountOverridePresent { get; set; }
            public int? RiderMovementAgentComponentCount { get; set; }
            public int? MountMovementAgentComponentCount { get; set; }
            public bool? RiderForbidRotation { get; set; }
            public bool? MountForbidRotation { get; set; }
            public bool AttachmentLeaseActive { get; set; }
            public bool AttachmentRestoreVerified { get; set; }
            public bool AttachmentResidue { get; set; }
            public bool RiderParentMatchesAttachment { get; set; }
            public string AttachmentParent { get; set; }
            public string SourceAnchor { get; set; }
            public int KmcAnchorObjectCount { get; set; }
            public int KmcRiderMovementAgentComponentCount { get; set; }
            public bool? RiderMoveCommandPresent { get; set; }
            public bool? MountMoveCommandPresent { get; set; }
            public string[] SelectedUnitIds { get; set; }

            public static RelationshipEvidence Capture(
                GameMountedRelationshipService relationship,
                UnitEntityData rider,
                UnitEntityData mount,
                bool captureSelection)
            {
                var riderView = rider == null ? null : rider.View;
                var mountView = mount == null ? null : mount.View;
                return new RelationshipEvidence
                {
                    State = relationship.State.ToString(),
                    RiderUniqueId = rider == null || rider.UniqueId == null ? null : rider.UniqueId.ToString(),
                    MountUniqueId = mount == null || mount.UniqueId == null ? null : mount.UniqueId.ToString(),
                    OwnerReferencesPresent = relationship.Rider != null || relationship.Mount != null,
                    MovementAgentPresent = relationship.Runtime.MovementAgent != null,
                    RiderStockAgentEnabled = riderView == null || riderView.AgentASP == null ? (bool?)null : riderView.AgentASP.enabled,
                    MountStockAgentEnabled = mountView == null || mountView.AgentASP == null ? (bool?)null : mountView.AgentASP.enabled,
                    RiderAvoidanceDisabled = riderView == null || riderView.AgentASP == null ? (bool?)null : riderView.AgentASP.AvoidanceDisabled,
                    MountAvoidanceDisabled = mountView == null || mountView.AgentASP == null ? (bool?)null : mountView.AgentASP.AvoidanceDisabled,
                    RiderOverridePresent = riderView == null ? (bool?)null : riderView.AgentOverride != null,
                    MountOverridePresent = mountView == null ? (bool?)null : mountView.AgentOverride != null,
                    RiderMovementAgentComponentCount = riderView == null ? (int?)null : riderView.GetComponents<RiderMovementAgent>().Length,
                    MountMovementAgentComponentCount = mountView == null ? (int?)null : mountView.GetComponents<RiderMovementAgent>().Length,
                    RiderForbidRotation = riderView == null ? (bool?)null : riderView.ForbidRotation,
                    MountForbidRotation = mountView == null ? (bool?)null : mountView.ForbidRotation,
                    AttachmentLeaseActive = relationship.Runtime.PresentationAttachmentLeaseActive,
                    AttachmentRestoreVerified = relationship.Runtime.PresentationAttachmentRestoreVerified,
                    AttachmentResidue = relationship.Runtime.HasPresentationAttachmentResidue,
                    RiderParentMatchesAttachment = relationship.Runtime.RiderParentMatchesAttachment,
                    AttachmentParent = relationship.Runtime.PresentationAttachmentParentName,
                    SourceAnchor = relationship.Runtime.PresentationSourceAnchorName,
                    KmcAnchorObjectCount = CountKmcAnchorObjects(),
                    KmcRiderMovementAgentComponentCount = CountKmcRiderMovementAgents(),
                    RiderMoveCommandPresent = rider == null || rider.Commands == null ? (bool?)null : rider.Commands.Move != null,
                    MountMoveCommandPresent = mount == null || mount.Commands == null ? (bool?)null : mount.Commands.Move != null,
                    // During an active/disposed-world load phase the global
                    // selection can still contain old UnitEntityData objects.
                    // Do not dereference them; selection continuity is proved
                    // at cleanup-latch and again from the stable fresh world.
                    SelectedUnitIds = captureSelection ? CaptureSelectedUnitIds() : new string[0]
                };
            }
        }

        private sealed class NativeLifecycleEvidence
        {
            public long BaselineSequence { get; set; }
            public int DeliveryCount { get; set; }
            public IReadOnlyList<NativeDeliveryEvidence> Deliveries { get; set; }

            public static NativeLifecycleEvidence Capture(
                long baselineSequence,
                IReadOnlyList<NativeLifecycleDeliveryRecord> records)
            {
                var deliveries = records.Select(record => new NativeDeliveryEvidence
                {
                    Sequence = record.Sequence,
                    Boundary = record.Boundary.ToString(),
                    Source = record.Source,
                    StateBefore = record.StateBefore.ToString(),
                    StateAfter = record.StateAfter.ToString(),
                    CleanupTrigger = record.CleanupTrigger.HasValue ? record.CleanupTrigger.Value.ToString() : null,
                    CleanupAttempted = record.CleanupAttempted,
                    CleanupSucceeded = record.CleanupSucceeded
                }).ToArray();
                return new NativeLifecycleEvidence
                {
                    BaselineSequence = baselineSequence,
                    DeliveryCount = deliveries.Length,
                    Deliveries = deliveries
                };
            }
        }

        private sealed class NativeDeliveryEvidence
        {
            public long Sequence { get; set; }
            public string Boundary { get; set; }
            public string Source { get; set; }
            public string StateBefore { get; set; }
            public string StateAfter { get; set; }
            public string CleanupTrigger { get; set; }
            public bool CleanupAttempted { get; set; }
            public bool CleanupSucceeded { get; set; }
        }

        private sealed class NativeModeProbeEvidence
        {
            public bool Executed { get; set; }
            public bool? OriginalValue { get; set; }
            public bool? TemporaryValue { get; set; }
            public bool? OriginalRawCacheHadValue { get; set; }
            public string PersistedValueBefore { get; set; }
            public string PersistedValueAfter { get; set; }
            public bool? TemporaryDeliveryAttempted { get; set; }
            public bool? RestoreDeliveryCompleted { get; set; }
            public bool? PersistedValueUnchanged { get; set; }

            public static NativeModeProbeEvidence Capture(NativeModeTransitionProbe probe)
            {
                return new NativeModeProbeEvidence
                {
                    Executed = true,
                    OriginalValue = probe.OriginalValue,
                    TemporaryValue = probe.TemporaryValue,
                    OriginalRawCacheHadValue = probe.OriginalRawCacheHadValue,
                    PersistedValueBefore = probe.PersistedValueBefore,
                    PersistedValueAfter = probe.PersistedValueAfter,
                    TemporaryDeliveryAttempted = probe.TemporaryDeliveryAttempted,
                    RestoreDeliveryCompleted = probe.RestoreDeliveryCompleted,
                    PersistedValueUnchanged = probe.PersistedValueUnchanged
                };
            }

            public static NativeModeProbeEvidence NotExecuted()
            {
                return new NativeModeProbeEvidence { Executed = false };
            }
        }

        private sealed class ModDisableProbeEvidence
        {
            public bool Executed { get; set; }
            public bool? OverlayPresentBeforeDisable { get; set; }
            public int? OverlayObjectCountBeforeDisable { get; set; }
            public bool? DisableCallbackSucceeded { get; set; }
            public bool? OverlayReferenceAbsentImmediately { get; set; }
            public bool? OverlayPresentOnDisabledFrame { get; set; }
            public int? OverlayObjectCountOnDisabledFrame { get; set; }
            public bool? ReenableCallbackSucceeded { get; set; }
            public bool? OverlayPresentAfterReenable { get; set; }
            public int? OverlayObjectCountAfterReenable { get; set; }

            public static ModDisableProbeEvidence NotExecuted()
            {
                return new ModDisableProbeEvidence { Executed = false };
            }
        }

        private sealed class BoundaryCleanupEvidence
        {
            public bool Captured { get; set; }
            public int? CaptureFrame { get; set; }
            public string ExpectedTrigger { get; set; }
            public string ActualTrigger { get; set; }
            public bool? TransitionSucceeded { get; set; }
            public bool? MovementAuthorityResidual { get; set; }
            public bool? PresentationResidual { get; set; }
            public bool? RelationshipUnmounted { get; set; }
            public bool? OwnerReferencesReleased { get; set; }
            public bool? MovementAgentReleased { get; set; }
            public bool? StockAgentsRestored { get; set; }
            public bool? AvoidanceRestored { get; set; }
            public bool? OverridesRestored { get; set; }
            public bool? RiderMovementAgentComponentsRestored { get; set; }
            public bool? ForbidRotationRestored { get; set; }
            public bool? AttachmentRestored { get; set; }
            public bool? SelectionRestored { get; set; }
            public bool? MoveCommandsRestored { get; set; }
            public bool? KmcAnchorObjectsAbsent { get; set; }
            public bool? AllRestored { get; set; }

            public static BoundaryCleanupEvidence NotCaptured(CleanupTrigger expected)
            {
                return new BoundaryCleanupEvidence
                {
                    Captured = false,
                    ExpectedTrigger = expected.ToString()
                };
            }

            public static BoundaryCleanupEvidence Capture(
                int frame,
                CleanupTrigger expected,
                GameMountedRelationshipService relationship,
                PairSnapshot pair)
            {
                var transition = relationship.LastTransition;
                var exactTrigger = transition != null && transition.Trigger == expected;
                var transitionClean = transition != null && transition.Succeeded &&
                    !transition.MovementAuthorityResidual && !transition.PresentationResidual;
                var relationshipUnmounted = relationship.State == RelationshipState.Unmounted;
                var ownerReleased = relationship.Rider == null && relationship.Mount == null;
                var movementReleased = relationship.Runtime.MovementAgent == null;
                var stock = pair != null && pair.StockAgentsRestored();
                var avoidance = pair != null && pair.AvoidanceRestored();
                var overrides = pair != null && pair.OverridesRestored();
                var components = pair != null && pair.OverrideComponentsRestored();
                var forbid = pair != null && pair.ForbidRotationRestored();
                var attachment = pair != null && pair.AttachmentTransformRestored() &&
                    !relationship.Runtime.PresentationAttachmentLeaseActive &&
                    relationship.Runtime.PresentationAttachmentRestoreVerified &&
                    !relationship.Runtime.HasPresentationAttachmentResidue;
                var selection = pair != null && pair.SelectionRestored();
                var commands = pair != null && pair.MoveCommandsRestored();
                var anchorsAbsent = CountKmcAnchorObjects() == 0;
                return new BoundaryCleanupEvidence
                {
                    Captured = true,
                    CaptureFrame = frame,
                    ExpectedTrigger = expected.ToString(),
                    ActualTrigger = transition == null || !transition.Trigger.HasValue ? null : transition.Trigger.Value.ToString(),
                    TransitionSucceeded = transition == null ? (bool?)null : transition.Succeeded,
                    MovementAuthorityResidual = transition == null ? (bool?)null : transition.MovementAuthorityResidual,
                    PresentationResidual = transition == null ? (bool?)null : transition.PresentationResidual,
                    RelationshipUnmounted = relationshipUnmounted,
                    OwnerReferencesReleased = ownerReleased,
                    MovementAgentReleased = movementReleased,
                    StockAgentsRestored = stock,
                    AvoidanceRestored = avoidance,
                    OverridesRestored = overrides,
                    RiderMovementAgentComponentsRestored = components,
                    ForbidRotationRestored = forbid,
                    AttachmentRestored = attachment,
                    SelectionRestored = selection,
                    MoveCommandsRestored = commands,
                    KmcAnchorObjectsAbsent = anchorsAbsent,
                    // Unity destroys both the hidden anchor and the owned
                    // RiderMovementAgent at the end of the frame. Synchronous
                    // safety is proved by released override/attachment ownership
                    // plus exact stock-agent and rider-transform restoration. The
                    // next/fresh-world phase separately requires zero pair-local
                    // and global component/anchor objects.
                    AllRestored = exactTrigger && transitionClean && relationshipUnmounted && ownerReleased && movementReleased &&
                        stock && avoidance && overrides && forbid && attachment && selection && commands
                };
            }
        }

        private sealed class FreshWorldEvidence
        {
            public bool Observed { get; set; }
            public bool? WorldReady { get; set; }
            public bool? PairResolved { get; set; }
            public string GameId { get; set; }
            public string GameName { get; set; }
            public string Area { get; set; }
            public bool? GameIdMatches { get; set; }
            public bool? GameNameMatches { get; set; }
            public bool? AreaMatches { get; set; }
            public bool? RelationshipClean { get; set; }
            public bool? StockAgentsEnabled { get; set; }
            public bool? AvoidanceOrdinary { get; set; }
            public bool? OverridesAbsent { get; set; }
            public bool? RiderMovementAgentComponentsAbsent { get; set; }
            public bool? ForbidRotationOrdinary { get; set; }
            public bool? AttachmentResidueAbsent { get; set; }
            public bool? SelectionRestored { get; set; }
            public bool? MoveCommandsAbsent { get; set; }
            public bool? KmcAnchorObjectsAbsent { get; set; }
            public bool? AllClean { get; set; }

            public static FreshWorldEvidence NotObserved()
            {
                return new FreshWorldEvidence { Observed = false };
            }

            public static FreshWorldEvidence Capture(
                bool worldReady,
                bool pairResolved,
                GameMountedRelationshipService relationship,
                UnitEntityData rider,
                UnitEntityData mount,
                PairSnapshot prior,
                RuntimeSaveDescriptor expected)
            {
                var game = Game.Instance;
                var riderView = rider == null ? null : rider.View;
                var mountView = mount == null ? null : mount.View;
                var observedGameId = game == null || game.Player == null ? null : game.Player.GameId;
                var observedGameName = game == null || game.Player == null || game.Player.MainCharacter.Value == null
                    ? null
                    : game.Player.MainCharacter.Value.CharacterName;
                var observedArea = game == null || game.CurrentlyLoadedArea == null
                    ? null
                    : game.CurrentlyLoadedArea.AssetGuidThreadSafe;
                var gameId = string.Equals(observedGameId, expected.GameId, StringComparison.Ordinal);
                var gameName = string.Equals(observedGameName, expected.GameName, StringComparison.Ordinal);
                var area = string.Equals(observedArea, expected.Area, StringComparison.Ordinal);
                var relationshipClean = relationship.State == RelationshipState.Unmounted && relationship.Rider == null &&
                    relationship.Mount == null && relationship.Runtime.MovementAgent == null;
                var agents = pairResolved && riderView != null && mountView != null &&
                    riderView.AgentASP != null && mountView.AgentASP != null &&
                    riderView.AgentASP.enabled && mountView.AgentASP.enabled;
                var avoidance = agents && !riderView.AgentASP.AvoidanceDisabled && !mountView.AgentASP.AvoidanceDisabled;
                var overrides = pairResolved && riderView != null && mountView != null &&
                    riderView.AgentOverride == null && mountView.AgentOverride == null;
                var components = pairResolved && riderView != null && mountView != null &&
                    riderView.GetComponents<RiderMovementAgent>().Length == 0 &&
                    mountView.GetComponents<RiderMovementAgent>().Length == 0 &&
                    CountKmcRiderMovementAgents() == 0;
                var forbid = pairResolved && riderView != null && mountView != null && prior != null &&
                    riderView.ForbidRotation == prior.RiderForbidRotationWasEnabled &&
                    mountView.ForbidRotation == prior.MountForbidRotationWasEnabled;
                var attachment = !relationship.Runtime.PresentationAttachmentLeaseActive &&
                    !relationship.Runtime.HasPresentationAttachmentResidue;
                var selection = prior != null && prior.SelectionRestored();
                var commands = pairResolved && rider != null && mount != null && rider.Commands != null && mount.Commands != null &&
                    rider.Commands.Move == null && mount.Commands.Move == null;
                var anchors = CountKmcAnchorObjects() == 0;
                return new FreshWorldEvidence
                {
                    Observed = true,
                    WorldReady = worldReady,
                    PairResolved = pairResolved,
                    GameId = observedGameId,
                    GameName = observedGameName,
                    Area = observedArea,
                    GameIdMatches = gameId,
                    GameNameMatches = gameName,
                    AreaMatches = area,
                    RelationshipClean = relationshipClean,
                    StockAgentsEnabled = agents,
                    AvoidanceOrdinary = avoidance,
                    OverridesAbsent = overrides,
                    RiderMovementAgentComponentsAbsent = components,
                    ForbidRotationOrdinary = forbid,
                    AttachmentResidueAbsent = attachment,
                    SelectionRestored = selection,
                    MoveCommandsAbsent = commands,
                    KmcAnchorObjectsAbsent = anchors,
                    AllClean = worldReady && pairResolved && gameId && gameName && area && relationshipClean && agents &&
                        avoidance && overrides && components && forbid && attachment && selection && commands && anchors
                };
            }
        }

        private static string[] CaptureSelectedUnitIds()
        {
            var selected = SelectionManager.Instance == null ? null : SelectionManager.Instance.SelectedUnits;
            return selected == null
                ? new string[0]
                : selected.Where(unit => unit != null)
                    .Select(unit => unit.UniqueId == null ? null : unit.UniqueId.ToString())
                    .ToArray();
        }

        private sealed class FileIdentitySnapshot
        {
            public long Length { get; private set; }

            public long LastWriteTimeUtcTicks { get; private set; }

            public string Sha256 { get; private set; }

            public static bool TryCapture(string path, out FileIdentitySnapshot snapshot, out string error)
            {
                snapshot = null;
                error = null;
                try
                {
                    var file = new FileInfo(path);
                    if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        error = "path was missing or a reparse point";
                        return false;
                    }

                    snapshot = new FileIdentitySnapshot
                    {
                        Length = file.Length,
                        LastWriteTimeUtcTicks = file.LastWriteTimeUtc.Ticks,
                        Sha256 = ComputeSha256(path)
                    };
                    return true;
                }
                catch (Exception exception)
                {
                    error = exception.GetType().Name + ": " + exception.Message;
                    return false;
                }
            }

            public bool Equals(FileIdentitySnapshot other)
            {
                return other != null && Length == other.Length &&
                    LastWriteTimeUtcTicks == other.LastWriteTimeUtcTicks &&
                    string.Equals(Sha256, other.Sha256, StringComparison.Ordinal);
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

            public bool RiderForbidRotationWasEnabled { get; private set; }

            public bool MountForbidRotationWasEnabled { get; private set; }

            public Transform RiderParent { get; private set; }

            public int RiderSiblingIndex { get; private set; }

            public Vector3 RiderLocalScale { get; private set; }

            public object RiderMoveCommand { get; private set; }

            public object MountMoveCommand { get; private set; }

            public string[] SelectedUnitIds { get; private set; }

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
                    RiderForbidRotationWasEnabled = rider.View.ForbidRotation,
                    MountForbidRotationWasEnabled = mount.View.ForbidRotation,
                    RiderParent = rider.View.transform.parent,
                    RiderSiblingIndex = rider.View.transform.GetSiblingIndex(),
                    RiderLocalScale = rider.View.transform.localScale,
                    RiderMoveCommand = rider.Commands == null ? null : rider.Commands.Move,
                    MountMoveCommand = mount.Commands == null ? null : mount.Commands.Move,
                    SelectedUnitIds = CaptureSelectedUnitIds()
                };
            }

            public void AssertOverrideComponentCount(AssertionRecorder recorder)
            {
                recorder.Check(OverrideComponentsRestored(),
                    "Owned RiderMovementAgent component counts returned to their exact prior values on the post-cleanup frame.",
                    "A RiderMovementAgent component remained or disappeared on the post-cleanup frame.");
            }

            public void AssertRestored(AssertionRecorder recorder)
            {
                recorder.Check(Rider != null && Mount != null && Rider.View == RiderView && Mount.View == MountView,
                    "Retained rider/mount references preserved their exact views through cleanup.",
                    "Retained rider/mount view identity changed before cleanup residue was verified.");
                recorder.Check(RiderView != null && RiderView.AgentASP == RiderStockAgent && RiderStockAgent != null &&
                    RiderStockAgent.enabled == RiderAgentWasEnabled,
                    "Rider exact stock-agent reference and enabled flag were restored.",
                    "Rider stock-agent reference or enabled flag retained residue.");
                recorder.Check(RiderStockAgent != null && RiderStockAgent.AvoidanceDisabled == RiderAvoidanceWasDisabled,
                    "Rider avoidance flag was restored.",
                    "Rider avoidance flag retained residue.");
                recorder.Check(RiderView != null && ReferenceEquals(RiderView.AgentOverride, RiderOverride),
                    "Rider AgentOverride was restored to its exact prior reference.",
                    "Rider AgentOverride retained or replaced an override.");
                recorder.Check(MountView != null && MountView.AgentASP == MountStockAgent && MountStockAgent != null &&
                    MountStockAgent.enabled == MountAgentWasEnabled,
                    "Mount exact stock-agent reference and enabled flag were preserved.",
                    "Mount stock-agent reference or enabled flag changed.");
                recorder.Check(MountStockAgent != null && MountStockAgent.AvoidanceDisabled == MountAvoidanceWasDisabled,
                    "Mount avoidance flag was preserved.",
                    "Mount avoidance flag changed.");
                recorder.Check(MountView != null && ReferenceEquals(MountView.AgentOverride, MountOverride),
                    "Mount AgentOverride was preserved.",
                    "Mount AgentOverride changed.");
                recorder.Check(OverrideComponentsRestored(),
                    "Rider and mount RiderMovementAgent component counts were restored.",
                    "Rider or mount retained a RiderMovementAgent component.");
                recorder.Check(ForbidRotationRestored(),
                    "Rider and mount ForbidRotation flags were restored.",
                    "Rider or mount retained a ForbidRotation change.");
                recorder.Check(AttachmentTransformRestored(),
                    "Rider parent, sibling index, and local scale were restored.",
                    "Rider attachment transform state retained residue.");
                recorder.Check(SelectionRestored(),
                    "Selected unit identities were restored exactly.",
                    "Selected unit identities retained boundary normalization residue.");
                recorder.Check(MoveCommandsRestored(),
                    "Rider and mount movement-command references were restored.",
                    "Rider or mount movement command retained boundary residue.");
            }

            public bool StockAgentsRestored()
            {
                try
                {
                    return RiderView != null && MountView != null && RiderStockAgent != null && MountStockAgent != null &&
                        RiderView.AgentASP == RiderStockAgent && MountView.AgentASP == MountStockAgent &&
                        RiderStockAgent.enabled == RiderAgentWasEnabled && MountStockAgent.enabled == MountAgentWasEnabled;
                }
                catch { return false; }
            }

            public bool AvoidanceRestored()
            {
                try
                {
                    return RiderStockAgent != null && MountStockAgent != null &&
                        RiderStockAgent.AvoidanceDisabled == RiderAvoidanceWasDisabled &&
                        MountStockAgent.AvoidanceDisabled == MountAvoidanceWasDisabled;
                }
                catch { return false; }
            }

            public bool OverridesRestored()
            {
                try
                {
                    return RiderView != null && MountView != null &&
                        ReferenceEquals(RiderView.AgentOverride, RiderOverride) &&
                        ReferenceEquals(MountView.AgentOverride, MountOverride);
                }
                catch { return false; }
            }

            public bool OverrideComponentsRestored()
            {
                try
                {
                    return RiderView != null && MountView != null &&
                        RiderView.GetComponents<RiderMovementAgent>().Length == RiderOverrideComponentCount &&
                        MountView.GetComponents<RiderMovementAgent>().Length == MountOverrideComponentCount;
                }
                catch { return false; }
            }

            public bool ForbidRotationRestored()
            {
                try
                {
                    return RiderView != null && MountView != null &&
                        RiderView.ForbidRotation == RiderForbidRotationWasEnabled &&
                        MountView.ForbidRotation == MountForbidRotationWasEnabled;
                }
                catch { return false; }
            }

            public bool AttachmentTransformRestored()
            {
                try
                {
                    return RiderView != null && RiderView.transform.parent == RiderParent &&
                        RiderView.transform.GetSiblingIndex() == RiderSiblingIndex &&
                        Vector3.Distance(RiderView.transform.localScale, RiderLocalScale) <= 0.0001f;
                }
                catch { return false; }
            }

            public bool SelectionRestored()
            {
                try
                {
                    return SelectedUnitIds.SequenceEqual(CaptureSelectedUnitIds(), StringComparer.Ordinal);
                }
                catch { return false; }
            }

            public bool MoveCommandsRestored()
            {
                try
                {
                    return Rider != null && Mount != null && Rider.Commands != null && Mount.Commands != null &&
                        ReferenceEquals(Rider.Commands.Move, RiderMoveCommand) &&
                        ReferenceEquals(Mount.Commands.Move, MountMoveCommand);
                }
                catch { return false; }
            }

            private static string[] CaptureSelectedUnitIds()
            {
                var selected = SelectionManager.Instance == null ? null : SelectionManager.Instance.SelectedUnits;
                return selected == null
                    ? new string[0]
                    : selected.Where(unit => unit != null)
                        .Select(unit => unit.UniqueId == null ? null : unit.UniqueId.ToString())
                        .ToArray();
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
