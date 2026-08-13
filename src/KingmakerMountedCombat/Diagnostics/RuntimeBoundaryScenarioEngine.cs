using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;

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

        private static readonly string[] SuiteRows =
        {
            "mounted-pair-turn-based-entry-cleanup",
            "mounted-pair-realtime-entry-cleanup",
            "mounted-pair-save-safety",
            "mounted-pair-load-safety",
            "mounted-pair-area-transition-safety"
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly RuntimeSaveAuthorization saveAuthorization;
        private readonly WorkingFixtureLoader fixtureLoader;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch suiteClock = new Stopwatch();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly BoundaryFailureDrain failureDrain = new BoundaryFailureDrain();

        private IReadOnlyList<string> selectedRows;
        private AssertionRecorder assertions;
        private PairSnapshot snapshot;
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
        private long fileLengthBefore;
        private long fileWriteTicksBefore;
        private string fileHashBefore;
        private bool asynchronousCallback;
        private bool loadingObserved;
        private bool boundaryCleanupVerified;
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
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.saveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            this.fixtureLoader = fixtureLoader ?? throw new ArgumentNullException(nameof(fixtureLoader));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                throw new InvalidOperationException("Boundary scenario engine has already started.");
            }

            selectedRows = SelectRows(request.Scenario);
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

            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
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
                case EngineStep.AwaitAreaCleanup:
                    VerifyAreaCleanup();
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
            stableWorldFrames = 0;
            asynchronousCallback = false;
            loadingObserved = false;
            boundaryCleanupVerified = false;
            CaptureAuthorizationCounts();
            rowClock.Restart();

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

            if (string.Equals(currentRow, "mounted-pair-turn-based-entry-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandleTurnBasedModeStateChanged(true);
                AssertTrigger(CleanupTrigger.TurnBasedModeChanged);
                AssertUnmountedAndRestored(snapshot);
                AwaitSimpleCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-realtime-entry-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandleTurnBasedModeStateChanged(false);
                AssertTrigger(CleanupTrigger.RealtimeModeChanged);
                AssertUnmountedAndRestored(snapshot);
                AwaitSimpleCleanupFrame();
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
            else
            {
                AbortSuite("Unsupported boundary row reached execution: " + currentRow + ".");
            }
        }

        private void BeginExactWorkingSave()
        {
            var descriptor = fixtureLoader.Descriptor;
            string descriptorError;
            assertions.Check(VerifyExactWorkingDescriptor(descriptor, fixtureLoader.WorkingPath, out descriptorError),
                "Initial loader descriptor still identified exact Working.",
                "Initial loader descriptor was not exact Working: " + (descriptorError ?? "unknown error"));
            if (descriptorError != null)
            {
                AbortSuite("Refused save because the supplied descriptor was not exact Working.");
                return;
            }

            var before = new FileInfo(fixtureLoader.WorkingPath);
            if (!before.Exists || (before.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                AbortSuite("Exact Working path disappeared or became a reparse point before save.");
                return;
            }
            fileLengthBefore = before.Length;
            fileWriteTicksBefore = before.LastWriteTimeUtc.Ticks;
            fileHashBefore = ComputeSha256(fixtureLoader.WorkingPath);

            // Exercise the same cleanup service used by the exact-token SaveRoutine
            // prefix, but do not enter stock SaveRoutine: Kingmaker allocates a second
            // Working-named leaf before replacing the requested slot, which cannot be
            // made crash-safe by the Phase 1 exact-file transaction.
            var guarded = relationship.GuardBoundary(CleanupTrigger.SaveRequested);
            assertions.Check(guarded,
                "Save boundary cleanup cleared the runtime-only relationship before serialization.",
                "Save boundary cleanup retained mounted state or residue.");
            AssertTrigger(CleanupTrigger.SaveRequested);
            AssertUnmountedAndRestored(snapshot);
            boundaryCleanupVerified = true;
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitSimpleCleanupFrame;
        }

        private void BeginExactWorkingLoad()
        {
            FileIdentitySnapshot beforeDescriptorRead;
            string identityError;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out beforeDescriptorRead, out identityError))
            {
                assertions.Fail("Exact Working identity could not be captured before LoadZipSave: " + identityError);
                AbortSuite("Refused load because exact Working identity could not be captured.");
                return;
            }
            assertions.Check(beforeDescriptorRead.Matches(request.Fixture.Working),
                "Exact Working length, timestamp, and SHA-256 matched the qualified request before LoadZipSave.",
                "Exact Working length, timestamp, or SHA-256 differed from the qualified request before LoadZipSave.");
            if (!beforeDescriptorRead.Matches(request.Fixture.Working))
            {
                AbortSuite("Refused load because exact Working bytes or metadata changed after fixture qualification.");
                return;
            }

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

            FileIdentitySnapshot beforeDispatch;
            if (!FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out beforeDispatch, out identityError))
            {
                assertions.Fail("Exact Working identity could not be recaptured before load dispatch: " + identityError);
                AbortSuite("Refused load because exact Working identity could not be recaptured after LoadZipSave.");
                return;
            }
            assertions.Check(beforeDispatch.Equals(beforeDescriptorRead) && beforeDispatch.Matches(request.Fixture.Working),
                "LoadZipSave left exact Working length, timestamp, and SHA-256 unchanged through dispatch.",
                "Exact Working length, timestamp, or SHA-256 changed between LoadZipSave and dispatch.");
            if (!beforeDispatch.Equals(beforeDescriptorRead) || !beforeDispatch.Matches(request.Fixture.Working))
            {
                AbortSuite("Refused load dispatch because LoadZipSave changed exact Working bytes or metadata.");
                return;
            }

            Game.Instance.SaveManager.AddCallbackAfterLoad(HandleAsynchronousCallback);
            Game.Instance.LoadGame(descriptor);
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitLoadCompletion;
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

            lifecycle.OnAreaBeginUnloading();
            AssertTrigger(CleanupTrigger.AreaUnloading);
            AssertUnmountedAndRestored(snapshot);
            boundaryCleanupVerified = true;
            Game.Instance.ReloadArea();
            boundaryFrame = frameNumber;
            step = EngineStep.AwaitAreaCleanup;
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

            AssertUnmountedAndRestored(snapshot);
            snapshot.AssertOverrideComponentCount(assertions);
            AssertAuthorizationCounts(0, 0);
            if (string.Equals(currentRow, "mounted-pair-save-safety", StringComparison.Ordinal))
            {
                var after = new FileInfo(fixtureLoader.WorkingPath);
                assertions.Check(after.Exists && (after.Attributes & FileAttributes.ReparsePoint) == 0 &&
                    after.Length == fileLengthBefore && after.LastWriteTimeUtc.Ticks == fileWriteTicksBefore &&
                    string.Equals(ComputeSha256(fixtureLoader.WorkingPath), fileHashBefore, StringComparison.Ordinal),
                    "Save-safety probe left exact Working bytes and metadata unchanged.",
                    "Save-safety probe changed exact Working despite the no-serialization Phase 1 policy.");
            }
            FinishCurrentRow();
        }

        private void VerifyLoadCompletion()
        {
            ObserveDeferredBoundaryCleanup(CleanupTrigger.LoadRequested);
            if (IsLoading())
            {
                loadingObserved = true;
                stableWorldFrames = 0;
                return;
            }
            if (!asynchronousCallback || !IsWorldReady())
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
            assertions.Check(loadingObserved || asynchronousCallback,
                "Exact Working load completed through the engine callback.",
                "Exact Working load completion was not observed.");
            AssertAuthorizationCounts(1, 0);
            AssertLoadedFixtureIdentity();
            AssertExactWorkingFileIdentity("post-load");
            AssertFreshWorldHasNoMountedState("post-load");
            FinishCurrentRow();
        }

        private void ObserveDeferredBoundaryCleanup(CleanupTrigger expected)
        {
            if (boundaryCleanupVerified || relationship.State != RelationshipState.Unmounted)
            {
                return;
            }

            AssertTrigger(expected);
            AssertUnmountedAndRestored(snapshot, expected == CleanupTrigger.LoadRequested);
            boundaryCleanupVerified = true;
        }

        private void VerifyAreaCleanup()
        {
            if (IsLoading())
            {
                loadingObserved = true;
            }

            if (relationship.State == RelationshipState.Unmounted && !boundaryCleanupVerified)
            {
                AssertTrigger(CleanupTrigger.AreaUnloading);
                AssertUnmountedAndRestored(snapshot, true);
                boundaryCleanupVerified = true;
                step = EngineStep.AwaitAreaCompletion;
            }
            else if (boundaryCleanupVerified && IsLoading())
            {
                loadingObserved = true;
                step = EngineStep.AwaitAreaCompletion;
            }
        }

        private void VerifyAreaCompletion()
        {
            if (IsLoading())
            {
                loadingObserved = true;
                stableWorldFrames = 0;
                return;
            }
            if (!loadingObserved || !IsWorldReady())
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
            AssertAuthorizationCounts(0, 0);
            AssertLoadedFixtureIdentity();
            AssertFreshWorldHasNoMountedState("post-area-reload");
            FinishCurrentRow();
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
            assertions.Check(snapshot.MountView.AgentASP == snapshot.MountStockAgent && snapshot.MountStockAgent.enabled,
                "Mount stock movement agent remained authoritative.",
                "Mount stock movement agent was changed or disabled.");
        }

        private void AssertTrigger(CleanupTrigger expected)
        {
            var transition = relationship.LastTransition;
            assertions.Check(transition != null && transition.Trigger == expected && transition.Succeeded &&
                !transition.MovementAuthorityResidual && !transition.PresentationResidual,
                expected + " boundary requested the exact cleanup trigger.",
                expected + " boundary did not report its exact cleanup trigger: " + relationship.LastResult);
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

        private void AssertFreshWorldHasNoMountedState(string phase)
        {
            assertions.Check(relationship.State == RelationshipState.Unmounted && relationship.Rider == null &&
                relationship.Mount == null && relationship.Runtime.MovementAgent == null,
                phase + " relationship owner contained no KMC runtime state.",
                phase + " relationship owner retained KMC runtime state.");

            UnitEntityData rider;
            UnitEntityData mount;
            string error;
            var resolved = relationship.TryResolveAutomationPair(out rider, out mount, out error);
            assertions.Check(resolved,
                phase + " exact automation pair resolved from fresh live state.",
                phase + " exact automation pair did not resolve: " + (error ?? "unknown error"));
            if (!resolved)
            {
                return;
            }

            var clean = rider.View != null && mount.View != null && rider.View.AgentASP != null && mount.View.AgentASP != null &&
                rider.View.AgentASP.enabled && mount.View.AgentASP.enabled &&
                !rider.View.AgentASP.AvoidanceDisabled && !mount.View.AgentASP.AvoidanceDisabled &&
                rider.View.AgentOverride == null && mount.View.AgentOverride == null;
            assertions.Check(clean,
                phase + " fresh pair exposed enabled stock agents, ordinary avoidance, and no overrides.",
                phase + " fresh pair exposed movement-agent, avoidance, or override residue.");
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

        private void AssertExactWorkingFileIdentity(string phase)
        {
            FileIdentitySnapshot observed;
            string error;
            var captured = FileIdentitySnapshot.TryCapture(fixtureLoader.WorkingPath, out observed, out error);
            assertions.Check(captured && observed.Matches(request.Fixture.Working),
                phase + " exact Working length, timestamp, and SHA-256 remained equal to the qualified request.",
                phase + " exact Working file identity changed or could not be read: " + (error ?? "identity mismatch"));
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
        }

        private void CaptureAuthorizationCounts()
        {
            authorizedLoadsBefore = saveAuthorization.AuthorizedLoadCount;
            authorizedWritesBefore = saveAuthorization.AuthorizedWriteCount;
            unauthorizedLoadsBefore = saveAuthorization.UnauthorizedLoadCount;
            unauthorizedWritesBefore = saveAuthorization.UnauthorizedWriteCount;
            baselineLoadsBefore = saveAuthorization.BaselineLoadRequestCount;
            fatalViolationsBefore = saveAuthorization.FatalViolationCount;
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

        private void AbortSuite(string message)
        {
            if (assertions == null)
            {
                assertions = new AssertionRecorder();
            }
            if (failureDrain.State == BoundaryFailureDrainState.Inactive)
            {
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
            var result = new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };
            results.Add(result);
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
            if (string.Equals(scenario, "boundary-suite", StringComparison.Ordinal))
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
            return null;
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
            AwaitAreaCleanup,
            AwaitAreaCompletion
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

            public bool Matches(RuntimeSaveDescriptor expected)
            {
                return expected != null && Length == expected.Length &&
                    LastWriteTimeUtcTicks == expected.LastWriteTimeUtcTicks &&
                    string.Equals(Sha256, expected.Sha256, StringComparison.Ordinal);
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
                    RiderOverrideComponentCount = rider.View.GetComponents<RiderMovementAgent>().Length
                };
            }

            public void AssertOverrideComponentCount(AssertionRecorder recorder)
            {
                recorder.Check(RiderView != null && RiderView.GetComponents<RiderMovementAgent>().Length == RiderOverrideComponentCount,
                    "Owned RiderMovementAgent component count returned to its exact prior value on the post-cleanup frame.",
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
