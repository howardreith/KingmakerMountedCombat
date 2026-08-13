using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Executes only the Phase 1 relationship-lifecycle rows.  Every action is
    /// advanced by Update so cleanup is observed on a later game frame rather
    /// than being accepted from the transition return value alone.
    /// </summary>
    internal sealed class RuntimeLifecycleScenarioEngine : IDisposable
    {
        private const double RowTimeoutSeconds = 15.0d;
        private const double SuiteTimeoutSeconds = 120.0d;

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

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch suiteClock = new Stopwatch();
        private readonly Stopwatch rowClock = new Stopwatch();

        private IReadOnlyList<string> selectedRows;
        private AssertionRecorder assertions;
        private PairSnapshot snapshot;
        private string currentRow;
        private int rowIndex;
        private int frameNumber;
        private int cleanupFrame;
        private EngineStep step;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;

        public RuntimeLifecycleScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedLifecycleSubscriber lifecycle,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
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

            try
            {
                if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.ProcessTeardown);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
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
            assertions = new AssertionRecorder();
            snapshot = null;
            rowClock.Restart();
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began the row Unmounted.",
                "Relationship began the row in " + relationship.State + " rather than Unmounted.");
            if (relationship.State != RelationshipState.Unmounted)
            {
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
                RequestCleanup(CleanupTrigger.Exception);
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

            var mounted = relationship.MountAutomationPair();
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
            step = EngineStep.AwaitMountedFrame;
        }

        private void ExerciseMountedRow()
        {
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship remained Mounted for at least one game frame.",
                "Relationship did not remain Mounted through the next game frame; observed " + relationship.State + ".");
            if (relationship.State != RelationshipState.Mounted)
            {
                RequestCleanup(CleanupTrigger.Exception);
                return;
            }

            if (string.Equals(currentRow, "mounted-pair-create-and-clear", StringComparison.Ordinal))
            {
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.Manual), CleanupTrigger.Manual);
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
                assertions.Check(relationship.LastResult.IndexOf("trigger=Death", StringComparison.Ordinal) >= 0,
                    "Death lifecycle handler requested the Death cleanup trigger.",
                    "Death lifecycle handler did not report the Death cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-combat-start-cleanup", StringComparison.Ordinal))
            {
                lifecycle.HandlePartyCombatStateChanged(true);
                assertions.Check(relationship.LastResult.IndexOf("trigger=CombatStarted", StringComparison.Ordinal) >= 0,
                    "Combat lifecycle handler requested the CombatStarted cleanup trigger.",
                    "Combat lifecycle handler did not report the CombatStarted cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-area-unload-cleanup", StringComparison.Ordinal))
            {
                lifecycle.OnAreaBeginUnloading();
                assertions.Check(relationship.LastResult.IndexOf("trigger=AreaUnloading", StringComparison.Ordinal) >= 0,
                    "Area lifecycle handler requested the AreaUnloading cleanup trigger.",
                    "Area lifecycle handler did not report the AreaUnloading cleanup trigger: " + relationship.LastResult);
                AwaitCleanupFrame();
            }
            else if (string.Equals(currentRow, "mounted-pair-mod-disable-cleanup", StringComparison.Ordinal))
            {
                AssertCleanupTransition(relationship.Dismount(CleanupTrigger.ModDisabled), CleanupTrigger.ModDisabled);
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

            AssertUnmountedAndRestored();
            var repeated = relationship.Dismount(CleanupTrigger.Manual);
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

            AssertUnmountedAndRestored();
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
            assertions.Check(snapshot.MountView.AgentASP == snapshot.MountStockAgent && snapshot.MountStockAgent.enabled,
                "Mount stock movement agent remained authoritative.",
                "Mount stock movement agent was changed or disabled.");
        }

        private void AssertCleanupTransition(TransitionResult result, CleanupTrigger expectedTrigger)
        {
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

            assertions.Check(snapshot.Rider.View == snapshot.RiderView && snapshot.Mount.View == snapshot.MountView,
                "Exact rider and mount views remained attached during the bounded lifecycle row.",
                "Rider or mount view identity changed during the bounded lifecycle row.");
            assertions.Check(snapshot.RiderView.AgentASP == snapshot.RiderStockAgent,
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
            assertions.Check(snapshot.MountView.AgentASP == snapshot.MountStockAgent,
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
        }

        private void RequestCleanup(CleanupTrigger trigger)
        {
            try
            {
                var cleanup = relationship.Dismount(trigger);
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
            if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
            {
                try
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
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

            RestoreSettings();
            suiteClock.Stop();
            rowClock.Stop();
            completed = true;
            logger.Info("Lifecycle runtime engine completed with " + results.Count + " row result(s).");
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
            AwaitFirstIdempotentCleanupFrame,
            AwaitCleanupFrame
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
                    MountOverride = mount.View.AgentOverride
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
