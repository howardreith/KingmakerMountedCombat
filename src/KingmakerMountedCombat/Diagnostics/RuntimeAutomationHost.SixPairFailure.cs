using System;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeAutomationHost
    {
        // Owner-selected unattended exit for the disposable six-pair runs only: the
        // zero-write lifecycle (world engine), and the mounted Manual producer and its
        // zero-write cold consumer (persistence engine). When the bounded completion
        // drain cannot settle, an honest atomic FAIL result is published and Unity is
        // asked to quit normally once. Every native write the run requested must have
        // completed within its declared quota first. Retained volatile custody stays
        // FAILED: nothing is marked settled, cleaned up, disposed or written, and the
        // outer runner still restores every surface. The same exit also covers a
        // two-pair owned persistence run (b195 x4c: a refused area restore held custody
        // until the runner's 900-second deadline), bound to its own declared quotas.
        private const double RetainedSixPairExitRetrySeconds = 1.0;
        private double retainedSixPairExitAttemptedAt = double.NegativeInfinity;

        private void TryQuitRetainedSixPairFailure()
        {
            var now = runtimeClock.Elapsed.TotalSeconds;
            if (now - retainedSixPairExitAttemptedAt < RetainedSixPairExitRetrySeconds) return;
            retainedSixPairExitAttemptedAt = now;
            if (completionStarted || completed || File.Exists(resultPath) || !RetainedSixPairFailureExitReady()) return;
            RuntimeGameResultV2 result;
            try { result = CreateRetainedSixPairFailure(); }
            catch (Exception error) { logger.Exception("Retained six-pair FAIL result", error); return; }
            // No frame runs between this recheck, the atomic publication and the quit
            // request. A refused recheck writes no result and retries later; only the
            // replaceable artifact manifest may already have been republished.
            if (!RetainedSixPairFailureExitReady()) return;
            try { WriteJsonAtomic(resultPath, result); }
            catch (Exception error) { logger.Exception("Retained six-pair FAIL publication", error); return; }
            completionStarted = true;
            completed = true;
            logger.Warning("Published the retained six-pair terminal FAIL; requesting one normal Unity quit.");
            try { Application.Quit(); }
            catch (Exception error) { logger.Exception("Retained six-pair FAIL quit", error); }
        }

        private bool RetainedSixPairFailureExitReady()
        {
            var selection = request.OwnedPairs;
            if (selection == null) return Decline("the run has no owned-pair selection");
            var sixPair = selection.SixPairLineage;
            var twoPairPersistence = !sixPair && selection.Persistence && worldPersistenceEngine != null && worldEngine == null;
            if (!sixPair && !twoPairPersistence) return Decline("the selection is neither six-pair lineage nor an owned persistence run");
            var stress = sixPair && selection.IsSixPairStress;
            var declaredWrites = sixPair ? (selection.IsSixPairSave ? 1 : 0) : selection.ExpectedNativeWriteCount;
            var declaredLoads = sixPair ? 1 : selection.ExpectedNativeLoadCount;
            if (!ReferenceEquals(active, this) || disposed || request.SchemaVersion != RuntimeRequest.SaveBackedSchemaVersion ||
                declaredLoads < 1 || declaredLoads > 3 || declaredWrites < 0 || declaredWrites > 2 ||
                selection.ExpectedNativeLoadCount != declaredLoads || selection.ExpectedNativeWriteCount != declaredWrites ||
                selection.Validate(request.Scenario, request.RunId, request.PersistenceCase).Count != 0 ||
                !chunk7Completion.Pending || !chunk7Completion.DeadlineExceeded || chunk7Completion.Settled ||
                chunk7CompletionStatus != "FAIL" || !fixtureIdentityVerified ||
                (stress ? worldEngine == null || worldPersistenceEngine != null : worldPersistenceEngine == null || worldEngine != null) ||
                saveRequestCount > declaredWrites || loadRequestCount < 1 || loadRequestCount > declaredLoads ||
                saveAuthorization.BaselineLoadRequestCount != 0 || saveAuthorization.AuthorizedLoadCount != loadRequestCount ||
                saveAuthorization.UnauthorizedLoadCount != 0 || saveAuthorization.AuthorizedWriteCount != saveRequestCount ||
                saveAuthorization.UnauthorizedWriteCount != 0 || saveAuthorization.SuppressedWorkingWriteCount != 0 ||
                persistence == null || persistence.HasActiveSaveScope ||
                persistence.SaveDraining || persistence.ActiveSaveWorkerRunning || persistence.ActiveSaveWorkerCached ||
                persistence.HasSaveAdmissionFence || persistence.SaveSuspended || persistence.AreaTransitionPending ||
                persistence.LoadInFlight || persistence.SnapshotCount != saveRequestCount ||
                persistence.CompletedSaveCount != saveRequestCount || NativePersistenceIsolation.HasPendingWrites ||
                relationship.World == null || relationship.World.SerializationBusy)
            {
                return Decline("declared quotas, completion state or native save/load state are not settled");
            }
            var loading = LoadingProcess.Instance;
            if (loading == null || loading.IsLoadingInProcess || loading.QueuedNames.Any()) return Decline("native loading is in progress");
            try
            {
                if (stress) worldEngine.RequireRetainedSixFailureScope();
                else if (sixPair) worldPersistenceEngine.RequireRetainedSixFailureScope();
                else worldPersistenceEngine.RequireRetainedFailureScope();
                // Act only for the exact launched request, token and request bytes.
                var arguments = Environment.GetCommandLineArgs();
                var requestFile = Path.GetFullPath(FindSingleArgument(arguments, RequestArgument));
                if (!string.Equals(requestFile, Path.GetFullPath(Path.Combine(request.EvidenceRoot, "runtime-request.json")), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(FindSingleArgument(arguments, TokenArgument), request.TransactionToken, StringComparison.Ordinal) ||
                    !string.Equals(ComputeSha256(requestFile), FindSingleArgument(arguments, RequestHashArgument), StringComparison.Ordinal))
                {
                    return Decline("the launched request, token or request bytes differ");
                }
                var pending = Path.Combine(request.EvidenceRoot, stress ? Chunk7WorldScenarioEngine.CleanupPendingFileName :
                    Chunk7PersistenceScenario.CleanupPendingFileName);
                var committed = stress ? worldEngine.RetainedFailureArtifactSha256 : worldPersistenceEngine.RetainedFailureArtifactSha256;
                return !string.IsNullOrEmpty(committed) && File.Exists(pending) &&
                    string.Equals(ComputeSha256(pending), committed, StringComparison.Ordinal) ||
                    Decline("the committed retained-failure artifact is absent or changed");
            }
            catch (Exception error)
            {
                return Decline("the retained failure scope was refused: " + error.Message);
            }
        }

        // Once the bounded drain has expired, name each distinct reason the exit declines, once.
        private readonly System.Collections.Generic.HashSet<string> retainedExitDeclines =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        private bool Decline(string reason)
        {
            if (chunk7Completion != null && chunk7Completion.DeadlineExceeded && retainedExitDeclines.Add(reason))
                logger.Warning("Retained terminal FAIL exit declined: " + reason + ".");
            return false;
        }

        private RuntimeGameResultV2 CreateRetainedSixPairFailure()
        {
            const string reason = "Disposable six-pair run failed with retained volatile cleanup custody after its bounded drain. " +
                "No save, load or serialization work remains; a normal process exit is requested without claiming cleanup or qualification.";
            const string twoPairReason = "Disposable owned-pair persistence run failed with retained volatile cleanup custody after its bounded drain. " +
                "No save, load or serialization work remains; a normal process exit is requested without claiming cleanup or qualification.";
            var sixPair = request.OwnedPairs.SixPairLineage;
            var exitReason = sixPair ? reason : twoPairReason;
            var stress = sixPair && request.OwnedPairs.IsSixPairStress;
            var engineErrors = stress ? worldEngine.Errors : worldPersistenceEngine.Errors;
            var engineResults = stress ? worldEngine.Results : worldPersistenceEngine.Results;
            var modAssembly = typeof(Main).Assembly;
            var umm = typeof(UnityModManager).Assembly;
            var harmony = Path.Combine(Path.GetDirectoryName(umm.Location), "0Harmony12.dll");
            var result = CreateBootstrapFailureV2(request, loadedModId, modAssembly, typeof(Kingmaker.Game).Assembly, umm,
                harmony, DateTimeOffset.UtcNow, stress ? "C7-world-cleanup" : "C7-persistence-cleanup", exitReason);
            result.StartupTrace = startupTrace.Capture();
            result.EngineLogTail = engineLogTail.Capture();
            result.StartedAtUtc = startedAt.ToString("o");
            result.FrameCount = frameCount;
            result.ElapsedSeconds = elapsedSeconds;
            result.SaveRequestCount = saveRequestCount;
            result.LoadRequestCount = loadRequestCount;
            result.FixtureIdentityVerified = fixtureIdentityVerified;
            result.BaselineLoadRequestCount = saveAuthorization.BaselineLoadRequestCount;
            result.WorkingLoadRequestCount = saveAuthorization.AuthorizedLoadCount;
            result.WorkingSaveRequestCount = saveAuthorization.AuthorizedWriteCount;
            result.SuppressedWorkingSaveRequestCount = saveAuthorization.SuppressedWorkingWriteCount;
            result.UnauthorizedLoadRequestCount = saveAuthorization.UnauthorizedLoadCount;
            result.UnauthorizedSaveRequestCount = saveAuthorization.UnauthorizedWriteCount;
            try { result.RelationshipState = relationshipStateProvider() ?? "Unavailable"; } catch { result.RelationshipState = "Unavailable"; }
            try { result.MovementExperimentEnabled = movementExperimentProvider(); } catch { result.MovementExperimentEnabled = false; }
            // The engine's own rows carry the causal failure; the host's requested
            // completion errors and recorded drain failures follow, then this exit.
            result.Errors = engineErrors.Concat(chunk7CompletionErrors ?? new string[0]).Concat(scenarioEngineErrors)
                .Distinct(StringComparer.Ordinal).Concat(new[] { exitReason }).ToArray();
            result.SubscenarioResults = engineResults.ToArray();
            result.SubscenarioTotal = result.SubscenarioResults.Count;
            result.SubscenarioPassCount = result.SubscenarioResults.Count(row => row.Status == "PASS");
            result.SubscenarioFailCount = result.SubscenarioResults.Count(row => row.Status != "PASS");
            result.AssertionPassCount = result.SubscenarioResults.Sum(row => row.AssertionPassCount);
            result.AssertionFailCount = result.SubscenarioResults.Sum(row => row.AssertionFailCount);
            if (result.SubscenarioFailCount == 0 || result.AssertionFailCount == 0)
                throw new InvalidOperationException("Retained six-pair terminal FAIL lost its causal failed row.");
            result.EvidenceManifestSha256 = PublishRuntimeArtifactManifest();
            return result;
        }
    }
}
