using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using KingmakerMountedCombat.Logging;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class RuntimeAutomationHost : IDisposable
    {
        private const string RequestArgument = "-kmcRuntimeRequest";
        private const string TokenArgument = "-kmcRuntimeToken";
        private const string RequestHashArgument = "-kmcRuntimeRequestSha256";
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Error,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly IModLogger logger;
        private readonly RuntimeRequest request;
        private readonly string loadedModId;
        private readonly Func<string> relationshipStateProvider;
        private readonly Func<bool> movementExperimentProvider;
        private readonly RuntimeSaveAuthorization saveAuthorization;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly DiagnosticSettings diagnosticSettings;
        private readonly string resultPath;
        private readonly DateTimeOffset startedAt;
        private readonly Stopwatch runtimeClock;
        private int frameCount;
        private double elapsedSeconds;
        private bool completed;
        private bool disposed;
        private int saveRequestCount;
        private int loadRequestCount;
        private IDisposable saveAuthorizationLease;
        private WorkingFixtureLoader fixtureLoader;
        private bool fixtureLoaderStarted;
        private bool fixtureIdentityVerified;
        private bool fixtureScenarioCompleted;
        private IReadOnlyList<RuntimeSubscenarioResult> subscenarioResults;
        private RuntimeLifecycleScenarioEngine lifecycleEngine;
        private RuntimeMovementScenarioEngine movementEngine;
        private RuntimeBoundaryScenarioEngine boundaryEngine;
        private readonly List<string> scenarioEngineErrors = new List<string>();
        private readonly BoundaryFailureDrain saveBackedFailureDrain = new BoundaryFailureDrain();
        private IReadOnlyList<string> saveBackedFailureErrors;
        private bool completionStarted;

        private static RuntimeAutomationHost active;

        public string EvidenceRoot => request.EvidenceRoot;

        public string Scenario => request.Scenario;

        public string RunId => request.RunId;

        public bool IsCompleted => completed;

        internal RuntimeRequest Request => request;

        internal string CurrentMovementRow => movementEngine == null ? null : movementEngine.CurrentRow;

        internal bool IsSaveBackedFailurePending => request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion &&
            !completed && !saveBackedFailureDrain.MayAdvanceScenario;

        public void Abort(Exception exception)
        {
            if (disposed || completed)
            {
                return;
            }

            TryCompleteFailure(exception ?? new InvalidOperationException("Runtime automation was aborted after an unspecified update failure."));
        }

        private RuntimeAutomationHost(
            IModLogger logger,
            RuntimeRequest request,
            string loadedModId,
            Func<string> relationshipStateProvider,
            Func<bool> movementExperimentProvider,
            RuntimeSaveAuthorization saveAuthorization,
            GameMountedRelationshipService relationship,
            MountedLifecycleSubscriber lifecycle,
            DiagnosticSettings diagnosticSettings)
        {
            this.logger = logger;
            this.request = request;
            this.loadedModId = loadedModId;
            this.relationshipStateProvider = relationshipStateProvider;
            this.movementExperimentProvider = movementExperimentProvider;
            this.saveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.diagnosticSettings = diagnosticSettings ?? throw new ArgumentNullException(nameof(diagnosticSettings));
            resultPath = Path.Combine(request.EvidenceRoot, "runtime-game-result.json");
            startedAt = DateTimeOffset.UtcNow;
            runtimeClock = Stopwatch.StartNew();
            try
            {
                if (request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion)
                {
                    ActivateSaveBackedBoundary();
                }
                active = this;
            }
            catch
            {
                saveAuthorizationLease?.Dispose();
                saveAuthorizationLease = null;
                throw;
            }
        }

        public static RuntimeAutomationHost CreateFromCommandLine(
            IModLogger logger,
            string loadedModId,
            Func<string> relationshipStateProvider,
            Func<bool> movementExperimentProvider,
            RuntimeSaveAuthorization saveAuthorization,
            GameMountedRelationshipService relationship,
            MountedLifecycleSubscriber lifecycle,
            DiagnosticSettings diagnosticSettings)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (string.IsNullOrWhiteSpace(loadedModId) || relationshipStateProvider == null || movementExperimentProvider == null)
            {
                throw new ArgumentException("Observed runtime providers and mod identity are required.");
            }

            var arguments = Environment.GetCommandLineArgs();
            var requestPath = FindSingleArgument(arguments, RequestArgument);
            if (requestPath == null)
            {
                return null;
            }

            var fullRequestPath = Path.GetFullPath(requestPath);
            if (!string.Equals(Path.GetFileName(fullRequestPath), "runtime-request.json", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Runtime request must use the exact runtime-request.json filename.");
            }

            var requestBytes = File.ReadAllBytes(fullRequestPath);
            var commandLineRequestHash = FindSingleArgument(arguments, RequestHashArgument);
            if (commandLineRequestHash == null || !string.Equals(commandLineRequestHash, ComputeSha256(requestBytes), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Runtime request bytes do not match the command-line SHA-256 binding.");
            }
            var json = new UTF8Encoding(false, true).GetString(requestBytes);
            var request = JsonConvert.DeserializeObject<RuntimeRequest>(json, JsonSettings);
            if (request == null)
            {
                throw new InvalidOperationException("Runtime request deserialized to null.");
            }

            var errors = request.Validate();
            if (errors.Count != 0)
            {
                throw new InvalidOperationException("Runtime request is invalid: " + string.Join("; ", errors));
            }

            var commandLineToken = FindSingleArgument(arguments, TokenArgument);
            if (commandLineToken == null || !string.Equals(commandLineToken, request.TransactionToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Runtime transaction token is missing or does not match the request.");
            }

            var evidenceRoot = Path.GetFullPath(request.EvidenceRoot).TrimEnd(Path.DirectorySeparatorChar);
            var requestRoot = Path.GetDirectoryName(fullRequestPath).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.Equals(evidenceRoot, requestRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Runtime request evidenceRoot does not match its own directory.");
            }

            var assembly = typeof(Main).Assembly;
            var observedHash = ComputeSha256(assembly.Location);
            var observedMvid = assembly.ManifestModule.ModuleVersionId.ToString();
            if (!string.Equals(request.DllSha256, observedHash, StringComparison.Ordinal) ||
                !string.Equals(request.DllMvid, observedMvid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Loaded KMC assembly does not match the requested SHA-256 and MVID.");
            }

            logger.Info("Runtime automation request accepted: " + request.RunId + " / " + request.Scenario);
            return new RuntimeAutomationHost(logger, request, loadedModId, relationshipStateProvider, movementExperimentProvider,
                saveAuthorization, relationship, lifecycle, diagnosticSettings);
        }

        internal static void ObserveSaveRequest()
        {
            if (active != null) { active.saveRequestCount++; }
        }

        internal static void ObserveLoadRequest()
        {
            if (active != null) { active.loadRequestCount++; }
        }

        internal static bool TryReportBootstrapFailure(IModLogger logger, string loadedModId, Exception exception)
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var requestPath = FindSingleArgument(arguments, RequestArgument);
                var token = FindSingleArgument(arguments, TokenArgument);
                var requestHash = FindSingleArgument(arguments, RequestHashArgument);
                if (requestPath == null || token == null || requestHash == null)
                {
                    return false;
                }

                var fullRequestPath = Path.GetFullPath(requestPath);
                var requestBytes = File.ReadAllBytes(fullRequestPath);
                if (!string.Equals(requestHash, ComputeSha256(requestBytes), StringComparison.Ordinal))
                {
                    return false;
                }
                var request = JsonConvert.DeserializeObject<RuntimeRequest>(new UTF8Encoding(false, true).GetString(requestBytes), JsonSettings);
                if (request == null || request.Validate().Count != 0 || !string.Equals(token, request.TransactionToken, StringComparison.Ordinal) ||
                    !string.Equals(Path.GetFullPath(request.EvidenceRoot).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(fullRequestPath).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var modAssembly = typeof(Main).Assembly;
                if (!string.Equals(request.DllSha256, ComputeSha256(modAssembly.Location), StringComparison.Ordinal) ||
                    !string.Equals(request.DllMvid, modAssembly.ManifestModule.ModuleVersionId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var gameAssembly = typeof(Kingmaker.Game).Assembly;
                var ummAssembly = typeof(UnityModManager).Assembly;
                var harmonyPath = Path.Combine(Path.GetDirectoryName(ummAssembly.Location), "0Harmony12.dll");
                var now = DateTimeOffset.UtcNow;
                if (request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion)
                {
                    var failureName = RuntimeRequest.IsMissionScenario(request.Scenario)
                        ? request.Scenario
                        : "observe-mount-diagnostic-availability";
                    var failureText = exception.GetType().FullName + ": " + exception.Message;
                    var resultV2 = CreateBootstrapFailureV2(request, loadedModId, modAssembly, gameAssembly, ummAssembly, harmonyPath, now, failureName, failureText);
                    WriteJsonAtomic(Path.Combine(request.EvidenceRoot, "runtime-game-result.json"), resultV2);
                    logger.Warning("Runtime bootstrap failure evidence committed; requesting clean process exit.");
                    return true;
                }

                var result = new RuntimeGameResult
                {
                    SchemaVersion = 1,
                    RunId = request.RunId,
                    Scenario = request.Scenario,
                    Status = "FAIL",
                    Branch = request.Branch,
                    Commit = request.Commit,
                    ProductVersion = request.ProductVersion,
                    DllSha256 = ComputeSha256(modAssembly.Location),
                    DllMvid = modAssembly.ManifestModule.ModuleVersionId.ToString(),
                    TransactionToken = request.TransactionToken,
                    StartedAtUtc = now.ToString("o"),
                    CompletedAtUtc = now.ToString("o"),
                    LoadedModId = loadedModId,
                    GameVersion = Kingmaker.GameVersion.GetVersion(),
                    GameAssemblySha256 = ComputeSha256(gameAssembly.Location),
                    GameAssemblyMvid = gameAssembly.ManifestModule.ModuleVersionId.ToString(),
                    UmmVersion = ummAssembly.GetName().Version.ToString(),
                    UmmSha256 = ComputeSha256(ummAssembly.Location),
                    Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : "Unavailable",
                    Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : new string('0', 64),
                    RelationshipState = "Unmounted",
                    MovementExperimentEnabled = false,
                    ProcessId = Process.GetCurrentProcess().Id,
                    CurrentGameMode = Kingmaker.Game.Instance == null ? "Unavailable" : Kingmaker.Game.Instance.CurrentMode.ToString(),
                    LoadedAreaPresent = Kingmaker.Game.Instance != null && Kingmaker.Game.Instance.CurrentlyLoadedArea != null,
                    SaveRequestCount = 0,
                    LoadRequestCount = 0,
                    FrameCount = 0,
                    ElapsedSeconds = 0,
                    Errors = new[] { exception.GetType().FullName + ": " + exception.Message }
                };
                WriteJsonAtomic(Path.Combine(request.EvidenceRoot, "runtime-game-result.json"), result);
                logger.Warning("Runtime bootstrap failure evidence committed; requesting clean process exit.");
                return true;
            }
            catch (Exception reportingException)
            {
                logger.Exception("Runtime bootstrap failure reporting", reportingException);
                return false;
            }
            finally
            {
                if (Array.Exists(Environment.GetCommandLineArgs(), argument => string.Equals(argument, RequestArgument, StringComparison.Ordinal)))
                {
                    Application.Quit();
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (disposed || completed)
            {
                return;
            }

            frameCount++;
            elapsedSeconds = runtimeClock.Elapsed.TotalSeconds;
            try
            {
                if (request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion && !saveBackedFailureDrain.MayAdvanceScenario)
                {
                    DrainSaveBackedFailure();
                    return;
                }
                if (frameCount < 10 || elapsedSeconds < 1.0d)
                {
                    return;
                }
                if (elapsedSeconds > 300.0d)
                {
                    if (IsLoadingProcessActive())
                    {
                        return;
                    }
                    Complete("FAIL", new[] { "Runtime automation exceeded the bounded 300-second in-process deadline." });
                    return;
                }

                if (request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion)
                {
                    UpdateSaveBackedScenario();
                    return;
                }

                if (!string.Equals(request.Scenario, "mod-load-smoke", StringComparison.Ordinal))
                {
                    Complete("FAIL", new[] { "Schema-v1 diagnostic build implements only mod-load-smoke." });
                    return;
                }

                var safetyErrors = new List<string>();
                var game = Kingmaker.Game.Instance;
                if (game == null || game.CurrentlyLoadedArea != null)
                {
                    safetyErrors.Add("No-save smoke observed a loaded campaign area or missing game singleton.");
                }
                if (saveRequestCount != 0 || loadRequestCount != 0)
                {
                    safetyErrors.Add("No-save smoke observed a save/load request.");
                }
                if (!string.Equals(relationshipStateProvider(), "Unmounted", StringComparison.Ordinal) || movementExperimentProvider())
                {
                    safetyErrors.Add("No-save smoke observed mounted state or an enabled movement experiment.");
                }

                Complete(safetyErrors.Count == 0 ? "PASS" : "FAIL", safetyErrors);
            }
            catch (Exception exception)
            {
                logger.Exception("Runtime automation", exception);
                TryCompleteFailure(exception);
            }
        }

        private void UpdateSaveBackedScenario()
        {
            if (saveAuthorizationLease == null || fixtureLoader == null)
            {
                throw new InvalidOperationException("Save-backed authorization was not activated during host construction.");
            }

            if (!fixtureLoaderStarted)
            {
                fixtureLoaderStarted = true;
                fixtureLoader.Start();
                return;
            }

            if (saveAuthorization.FatalViolationCount != 0)
            {
                Complete("FAIL", new[] { saveAuthorization.LastFatalViolation ?? "Runtime save authorization reported an unspecified fatal violation." });
                return;
            }

            if (fixtureLoader == null || !fixtureLoader.TryCompleteVerification())
            {
                return;
            }

            fixtureIdentityVerified = true;
            if (fixtureScenarioCompleted)
            {
                return;
            }

            if (RuntimeLifecycleScenarioEngine.SupportsScenario(request.Scenario))
            {
                if (lifecycleEngine == null)
                {
                    lifecycleEngine = new RuntimeLifecycleScenarioEngine(request, relationship, lifecycle, diagnosticSettings, logger);
                    lifecycleEngine.Start();
                }
                lifecycleEngine.Update();
                if (!lifecycleEngine.IsCompleted)
                {
                    return;
                }
                subscenarioResults = lifecycleEngine.Results;
                CollectEngineErrors(lifecycleEngine.Errors, "Lifecycle");
                try { lifecycleEngine.Dispose(); }
                catch (Exception exception) { scenarioEngineErrors.Add("Lifecycle engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(lifecycleEngine.Errors, "Lifecycle");
                lifecycleEngine = null;
            }
            else if (RuntimeMovementScenarioEngine.SupportsScenario(request.Scenario))
            {
                if (movementEngine == null)
                {
                    movementEngine = new RuntimeMovementScenarioEngine(request, relationship, diagnosticSettings,
                        logger, request.EvidenceRoot);
                    movementEngine.Start();
                }
                movementEngine.Update();
                if (!movementEngine.IsCompleted)
                {
                    return;
                }
                subscenarioResults = movementEngine.Results;
                CollectEngineErrors(movementEngine.Errors, "Movement");
                try { movementEngine.Dispose(); }
                catch (Exception exception) { scenarioEngineErrors.Add("Movement engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(movementEngine.Errors, "Movement");
                movementEngine = null;
            }
            else if (RuntimeBoundaryScenarioEngine.SupportsScenario(request.Scenario))
            {
                if (boundaryEngine == null)
                {
                    boundaryEngine = new RuntimeBoundaryScenarioEngine(request, relationship, lifecycle, saveAuthorization,
                        fixtureLoader, diagnosticSettings, logger);
                    boundaryEngine.Start();
                }
                boundaryEngine.Update();
                if (!boundaryEngine.IsCompleted)
                {
                    return;
                }
                subscenarioResults = boundaryEngine.Results;
                CollectEngineErrors(boundaryEngine.Errors, "Boundary");
                try { boundaryEngine.Dispose(); }
                catch (Exception exception) { scenarioEngineErrors.Add("Boundary engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(boundaryEngine.Errors, "Boundary");
                boundaryEngine = null;
            }
            else if (string.Equals(request.Scenario, "fixture-intake", StringComparison.Ordinal))
            {
                subscenarioResults = new[]
                {
                    EvaluateMountedContracts(),
                    EvaluateCandidateRig(),
                    EvaluateDiagnosticAvailability()
                };
            }
            else if (string.Equals(request.Scenario, "export-mounted-contracts", StringComparison.Ordinal))
            {
                subscenarioResults = new[] { EvaluateMountedContracts() };
            }
            else if (string.Equals(request.Scenario, "export-candidate-mount-rigs", StringComparison.Ordinal))
            {
                subscenarioResults = new[] { EvaluateCandidateRig() };
            }
            else if (string.Equals(request.Scenario, "observe-mount-diagnostic-availability", StringComparison.Ordinal))
            {
                subscenarioResults = new[] { EvaluateDiagnosticAvailability() };
            }
            else
            {
                var name = RuntimeRequest.IsMissionScenario(request.Scenario)
                    ? request.Scenario
                    : "observe-mount-diagnostic-availability";
                subscenarioResults = new[]
                {
                    new RuntimeSubscenarioResult
                    {
                        Name = name,
                        Status = "FAIL",
                        AssertionPassCount = 0,
                        AssertionFailCount = 1,
                        Errors = new[] { "This diagnostic build does not yet implement the requested save-backed scenario engine." }
                    }
                };
            }

            fixtureScenarioCompleted = true;

            var failures = new List<string>();
            foreach (var result in subscenarioResults)
            {
                if (!string.Equals(result.Status, "PASS", StringComparison.Ordinal))
                {
                    failures.AddRange(result.Errors);
                }
            }
            failures.AddRange(scenarioEngineErrors);
            Complete(failures.Count == 0 ? "PASS" : "FAIL", failures);
        }

        private void CollectEngineErrors(IReadOnlyList<string> source, string prefix)
        {
            if (source == null)
            {
                return;
            }
            foreach (var error in source)
            {
                var message = prefix + " engine: " + error;
                if (!scenarioEngineErrors.Contains(message))
                {
                    scenarioEngineErrors.Add(message);
                }
            }
        }

        private void ActivateSaveBackedBoundary()
        {
            var game = Kingmaker.Game.Instance;
            if (game == null || game.SaveManager == null || game.CurrentlyLoadedArea != null)
            {
                throw new InvalidOperationException("Save-backed automation must start at the main menu with no loaded area.");
            }

            // Phase 1 never invokes Kingmaker's stock SaveRoutine. That routine writes a
            // newly allocated temporary save leaf before replacing the requested slot,
            // which is not a crash-safe exact-path write boundary. Loads may update the
            // already-transactional Working header; every SaveRoutine request stays denied.
            saveAuthorizationLease = saveAuthorization.Activate(request.Fixture, game.SaveManager.SavePath, false);
            fixtureLoader = new WorkingFixtureLoader(request, logger);
        }

        private RuntimeSubscenarioResult EvaluateMountedContracts()
        {
            var errors = new List<string>();
            var passed = 0;
            var failed = 0;
            AssertRuntime(errors, typeof(Kingmaker.Game).Assembly.ManifestModule.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"),
                "Loaded Kingmaker Assembly-CSharp MVID differs from the qualified contract.", ref passed, ref failed);
            AssertRuntime(errors, typeof(Kingmaker.EntitySystem.Persistence.SaveManager).GetMethod("LoadZipSave", new[] { typeof(string) }) != null,
                "Exact SaveManager.LoadZipSave(string) seam is unavailable.", ref passed, ref failed);
            AssertRuntime(errors, typeof(Kingmaker.Controllers.Units.UnitMoveController).GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null,
                "Exact Kingmaker unit-movement controller seam is unavailable.", ref passed, ref failed);
            return BuildSubscenario("export-mounted-contracts", passed, failed, errors);
        }

        private RuntimeSubscenarioResult EvaluateCandidateRig()
        {
            var errors = new List<string>();
            var passed = 0;
            var failed = 0;
            Kingmaker.EntitySystem.Entities.UnitEntityData rider;
            Kingmaker.EntitySystem.Entities.UnitEntityData mount;
            string pairError;
            var pairResolved = relationship.TryResolveAutomationPair(out rider, out mount, out pairError);
            AssertRuntime(errors, pairResolved, pairError ?? "Exact Mammoth pair was not resolved.", ref passed, ref failed);
            if (pairResolved)
            {
                AssertRuntime(errors, mount.Blueprint != null && string.Equals(mount.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal),
                    "Resolved mount is not the exact Mammoth blueprint.", ref passed, ref failed);
                AssertRuntime(errors, mount.View != null && mount.View.AgentASP != null && mount.View.AgentASP.enabled,
                    "Mammoth stock movement agent is unavailable or disabled.", ref passed, ref failed);
                AssertRuntime(errors, mount.View != null && FindTransform(mount.View.transform, "Spine") != null,
                    "Mammoth runtime view has no exact Spine anchor.", ref passed, ref failed);
            }
            return BuildSubscenario("export-candidate-mount-rigs", passed, failed, errors);
        }

        private RuntimeSubscenarioResult EvaluateDiagnosticAvailability()
        {
            var errors = new List<string>();
            var passed = 0;
            var failed = 0;
            Kingmaker.EntitySystem.Entities.UnitEntityData rider;
            Kingmaker.EntitySystem.Entities.UnitEntityData mount;
            string pairError;
            var resolved = relationship.TryResolveAutomationPair(out rider, out mount, out pairError);
            AssertRuntime(errors, resolved, pairError ?? "Exact automation pair is unavailable.", ref passed, ref failed);
            if (resolved)
            {
                AssertRuntime(errors, rider != mount, "Resolved rider and mount are the same unit.", ref passed, ref failed);
                AssertRuntime(errors, (int)rider.Descriptor.State.Size == 4,
                    "Resolved rider is not currently Medium.", ref passed, ref failed);
                AssertRuntime(errors, (int)mount.Descriptor.State.Size > (int)rider.Descriptor.State.Size,
                    "Resolved Mammoth is not currently larger than the rider.", ref passed, ref failed);
                AssertRuntime(errors, !rider.IsInCombat && !mount.IsInCombat && !(Kingmaker.Game.Instance.Player?.IsInCombat ?? false),
                    "Resolved pair or party is in combat.", ref passed, ref failed);
                AssertRuntime(errors, Kingmaker.Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default,
                    "Loaded fixture is not in Default game mode.", ref passed, ref failed);
            }
            return BuildSubscenario("observe-mount-diagnostic-availability", passed, failed, errors);
        }

        private static RuntimeSubscenarioResult BuildSubscenario(string name, int passed, int failed, IReadOnlyList<string> errors)
        {
            return new RuntimeSubscenarioResult
            {
                Name = name,
                Status = failed == 0 ? "PASS" : "FAIL",
                AssertionPassCount = passed,
                AssertionFailCount = failed,
                Errors = errors
            };
        }

        private static void AssertRuntime(List<string> errors, bool condition, string message, ref int passed, ref int failed)
        {
            if (condition) { passed++; }
            else { failed++; errors.Add(message); }
        }

        private static Transform FindTransform(Transform current, string exactName)
        {
            if (current == null) { return null; }
            if (string.Equals(current.name, exactName, StringComparison.Ordinal)) { return current; }
            for (var index = 0; index < current.childCount; index++)
            {
                var found = FindTransform(current.GetChild(index), exactName);
                if (found != null) { return found; }
            }
            return null;
        }

        public void Dispose()
        {
            disposed = true;
            lifecycleEngine?.Dispose();
            lifecycleEngine = null;
            movementEngine?.Dispose();
            movementEngine = null;
            boundaryEngine?.Dispose();
            boundaryEngine = null;
            saveAuthorizationLease?.Dispose();
            saveAuthorizationLease = null;
            if (ReferenceEquals(active, this)) { active = null; }
        }

        private void Complete(string status, IReadOnlyList<string> errors)
        {
            if (completed)
            {
                return;
            }

            if (request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion)
            {
                CompleteSaveBackedSafely(status, errors);
                return;
            }

            CompleteNoSaveSafely(status, errors);
        }

        private void CompleteNoSaveSafely(string status, IReadOnlyList<string> errors)
        {
            if (completionStarted)
            {
                return;
            }

            completionStarted = true;
            try
            {
                WriteJsonAtomic(resultPath, CreateNoSaveResult(status, errors));
                logger.Info("Runtime automation result committed: " + status);
            }
            catch (Exception exception)
            {
                logger.Exception("No-save runtime completion", exception);
                if (!File.Exists(resultPath))
                {
                    try
                    {
                        WriteJsonAtomic(resultPath, CreateNoSaveResult("FAIL", new[]
                        {
                            "Runtime completion failed: " + exception.GetType().FullName + ": " + exception.Message
                        }));
                    }
                    catch (Exception writeException)
                    {
                        logger.Exception("Emergency no-save runtime result", writeException);
                    }
                }
            }
            finally
            {
                completed = true;
                try { Application.Quit(); }
                catch (Exception exception) { logger.Exception("No-save runtime quit", exception); }
            }
        }

        private RuntimeGameResult CreateNoSaveResult(string status, IReadOnlyList<string> errors)
        {

            var modAssembly = typeof(Main).Assembly;
            var gameAssembly = typeof(Kingmaker.Game).Assembly;
            var ummAssembly = typeof(UnityModManager).Assembly;
            var ummDirectory = Path.GetDirectoryName(ummAssembly.Location);
            var harmonyPath = Path.Combine(ummDirectory, "0Harmony12.dll");
            return new RuntimeGameResult
            {
                SchemaVersion = 1,
                RunId = request.RunId,
                Scenario = request.Scenario,
                Status = status,
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = ComputeSha256(modAssembly.Location),
                DllMvid = modAssembly.ManifestModule.ModuleVersionId.ToString(),
                TransactionToken = request.TransactionToken,
                StartedAtUtc = startedAt.ToString("o"),
                CompletedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                LoadedModId = loadedModId,
                GameVersion = Kingmaker.GameVersion.GetVersion(),
                GameAssemblySha256 = ComputeSha256(gameAssembly.Location),
                GameAssemblyMvid = gameAssembly.ManifestModule.ModuleVersionId.ToString(),
                UmmVersion = ummAssembly.GetName().Version.ToString(),
                UmmSha256 = ComputeSha256(ummAssembly.Location),
                Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : null,
                Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : null,
                RelationshipState = relationshipStateProvider(),
                MovementExperimentEnabled = movementExperimentProvider(),
                ProcessId = Process.GetCurrentProcess().Id,
                CurrentGameMode = Kingmaker.Game.Instance == null ? null : Kingmaker.Game.Instance.CurrentMode.ToString(),
                LoadedAreaPresent = Kingmaker.Game.Instance != null && Kingmaker.Game.Instance.CurrentlyLoadedArea != null,
                SaveRequestCount = saveRequestCount,
                LoadRequestCount = loadRequestCount,
                FrameCount = frameCount,
                ElapsedSeconds = elapsedSeconds,
                Errors = errors
            };
        }

        private void CompleteSaveBackedSafely(string status, IReadOnlyList<string> errors)
        {
            if (completionStarted)
            {
                return;
            }

            if (saveBackedFailureDrain.IsLatched)
            {
                DrainSaveBackedFailure();
                return;
            }
            if (IsLoadingProcessActive())
            {
                RequestSaveBackedFailureDrain(status, errors);
                return;
            }

            CompleteSaveBackedWhenIdle(status, errors);
        }

        private void RequestSaveBackedFailureDrain(string requestedStatus, IReadOnlyList<string> errors)
        {
            if (!saveBackedFailureDrain.IsLatched)
            {
                var retainedErrors = new List<string>();
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            retainedErrors.Add(error);
                        }
                    }
                }
                if (retainedErrors.Count == 0)
                {
                    retainedErrors.Add("Save-backed runtime requested " + requestedStatus + " completion while Kingmaker loading was active.");
                }
                retainedErrors.Add("Save-backed completion, cleanup, engine disposal, and process quit are deferred until the active Kingmaker loading pipeline stops.");
                saveBackedFailureErrors = retainedErrors.ToArray();
                saveBackedFailureDrain.Request(retainedErrors[0], IsLoadingProcessActive());
            }

            DrainSaveBackedFailure();
        }

        private void DrainSaveBackedFailure()
        {
            if (saveBackedFailureDrain.Observe(IsLoadingProcessActive()) == BoundaryFailureDrainState.DrainingActiveLoad)
            {
                return;
            }
            if (saveBackedFailureDrain.State != BoundaryFailureDrainState.ReadyToFinalize)
            {
                return;
            }

            var retainedErrors = saveBackedFailureErrors ?? (IReadOnlyList<string>)new[]
            {
                saveBackedFailureDrain.Failure ?? "Save-backed runtime failure drain did not retain an exact cause."
            };
            CompleteSaveBackedWhenIdle("FAIL", retainedErrors);
        }

        private void CompleteSaveBackedWhenIdle(string status, IReadOnlyList<string> errors)
        {
            if (completionStarted)
            {
                return;
            }
            if (IsLoadingProcessActive())
            {
                throw new InvalidOperationException("Save-backed runtime completion was invoked while Kingmaker loading was active.");
            }

            completionStarted = true;
            try
            {
                CompleteSaveBackedCore(status, errors);
            }
            catch (Exception exception)
            {
                logger.Exception("Save-backed runtime completion", exception);
                if (!File.Exists(resultPath))
                {
                    TryWriteEmergencySaveBackedFailure(exception);
                }
            }
            finally
            {
                completed = true;
                try { Application.Quit(); }
                catch (Exception exception) { logger.Exception("Save-backed runtime quit", exception); }
            }
        }

        private static bool IsLoadingProcessActive()
        {
            var loading = Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance;
            return loading != null && loading.IsLoadingInProcess;
        }

        private void CompleteSaveBackedCore(string status, IReadOnlyList<string> errors)
        {
            var finalErrors = errors == null ? new List<string>() : new List<string>(errors);
            FinalizeActiveScenarioEngines(finalErrors);
            diagnosticSettings.EnableUnsafeMovementExperiment = false;
            var cleanup = relationship.Dismount(Domain.CleanupTrigger.Exception);
            if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual ||
                relationship.State != Domain.RelationshipState.Unmounted)
            {
                finalErrors.Add("Final runtime cleanup retained mounted movement or presentation residue.");
                status = "FAIL";
            }

            var results = subscenarioResults;
            if (results == null || results.Count == 0)
            {
                var name = RuntimeRequest.IsMissionScenario(request.Scenario)
                    ? request.Scenario
                    : "observe-mount-diagnostic-availability";
                results = new[]
                {
                    new RuntimeSubscenarioResult
                    {
                        Name = name,
                        Status = "FAIL",
                        AssertionPassCount = 0,
                        AssertionFailCount = 1,
                        Errors = finalErrors.Count == 0
                            ? (IReadOnlyList<string>)new[] { "Save-backed runtime failed before producing a named result." }
                            : finalErrors
                    }
                };
            }

            if (finalErrors.Count != 0 && string.Equals(status, "PASS", StringComparison.Ordinal))
            {
                status = "FAIL";
            }

            if (string.Equals(status, "FAIL", StringComparison.Ordinal))
            {
                var hasFailedResult = false;
                for (var index = 0; index < results.Count; index++)
                {
                    if (!string.Equals(results[index].Status, "PASS", StringComparison.Ordinal))
                    {
                        hasFailedResult = true;
                        break;
                    }
                }
                if (!hasFailedResult)
                {
                    var first = results[0];
                    results = ReplaceFirstWithFailure(results, first, finalErrors.Count == 0
                        ? (IReadOnlyList<string>)new[] { "Runtime ended in FAIL without a prior failed named assertion." }
                        : finalErrors);
                }
            }

            var subscenarioPassCount = 0;
            var subscenarioFailCount = 0;
            var assertionPassCount = 0;
            var assertionFailCount = 0;
            foreach (var result in results)
            {
                if (string.Equals(result.Status, "PASS", StringComparison.Ordinal)) { subscenarioPassCount++; }
                else { subscenarioFailCount++; }
                assertionPassCount += result.AssertionPassCount;
                assertionFailCount += result.AssertionFailCount;
            }

            var modAssembly = typeof(Main).Assembly;
            var gameAssembly = typeof(Kingmaker.Game).Assembly;
            var ummAssembly = typeof(UnityModManager).Assembly;
            var harmonyPath = Path.Combine(Path.GetDirectoryName(ummAssembly.Location), "0Harmony12.dll");
            var game = Kingmaker.Game.Instance;
            var evidenceManifestSha256 = EnsureRuntimeArtifactManifest();
            var resultPayload = new RuntimeGameResultV2
            {
                SchemaVersion = RuntimeRequest.SaveBackedSchemaVersion,
                RunId = request.RunId,
                Scenario = request.Scenario,
                Status = status,
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = ComputeSha256(modAssembly.Location),
                DllMvid = modAssembly.ManifestModule.ModuleVersionId.ToString(),
                TransactionToken = request.TransactionToken,
                StartedAtUtc = startedAt.ToString("o"),
                CompletedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                LoadedModId = loadedModId,
                GameVersion = Kingmaker.GameVersion.GetVersion(),
                GameAssemblySha256 = ComputeSha256(gameAssembly.Location),
                GameAssemblyMvid = gameAssembly.ManifestModule.ModuleVersionId.ToString(),
                UmmVersion = ummAssembly.GetName().Version.ToString(),
                UmmSha256 = ComputeSha256(ummAssembly.Location),
                Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : null,
                Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : null,
                RelationshipState = relationshipStateProvider(),
                MovementExperimentEnabled = movementExperimentProvider(),
                ProcessId = Process.GetCurrentProcess().Id,
                CurrentGameMode = game == null ? null : game.CurrentMode.ToString(),
                LoadedAreaPresent = game != null && game.CurrentlyLoadedArea != null,
                SaveRequestCount = saveRequestCount,
                LoadRequestCount = loadRequestCount,
                FrameCount = frameCount,
                ElapsedSeconds = elapsedSeconds,
                Errors = finalErrors,
                Fixture = request.Fixture,
                FixtureIdentityVerified = fixtureIdentityVerified,
                BaselineLoadRequestCount = saveAuthorization.BaselineLoadRequestCount,
                WorkingLoadRequestCount = saveAuthorization.AuthorizedLoadCount,
                WorkingSaveRequestCount = saveAuthorization.AuthorizedWriteCount,
                UnauthorizedLoadRequestCount = saveAuthorization.UnauthorizedLoadCount,
                UnauthorizedSaveRequestCount = saveAuthorization.UnauthorizedWriteCount,
                SubscenarioTotal = results.Count,
                SubscenarioPassCount = subscenarioPassCount,
                SubscenarioFailCount = subscenarioFailCount,
                AssertionPassCount = assertionPassCount,
                AssertionFailCount = assertionFailCount,
                EvidenceManifestSha256 = evidenceManifestSha256,
                SubscenarioResults = results
            };

            WriteJsonAtomic(resultPath, resultPayload);
            logger.Info("Runtime automation result committed: " + status);
        }

        private void FinalizeActiveScenarioEngines(List<string> finalErrors)
        {
            if (lifecycleEngine != null)
            {
                var engine = lifecycleEngine;
                lifecycleEngine = null;
                if (!engine.IsCompleted) { finalErrors.Add("Lifecycle engine was interrupted before completing its selected rows."); }
                if ((subscenarioResults == null || subscenarioResults.Count == 0) && engine.Results.Count != 0) { subscenarioResults = engine.Results; }
                CollectEngineErrors(engine.Errors, "Lifecycle");
                try { engine.Dispose(); }
                catch (Exception exception) { finalErrors.Add("Lifecycle engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(engine.Errors, "Lifecycle");
            }
            if (movementEngine != null)
            {
                var engine = movementEngine;
                movementEngine = null;
                if (!engine.IsCompleted) { finalErrors.Add("Movement engine was interrupted before completing its selected rows."); }
                if ((subscenarioResults == null || subscenarioResults.Count == 0) && engine.Results.Count != 0) { subscenarioResults = engine.Results; }
                CollectEngineErrors(engine.Errors, "Movement");
                try { engine.Dispose(); }
                catch (Exception exception) { finalErrors.Add("Movement engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(engine.Errors, "Movement");
            }
            if (boundaryEngine != null)
            {
                var engine = boundaryEngine;
                boundaryEngine = null;
                if (!engine.IsCompleted) { finalErrors.Add("Boundary engine was interrupted before completing its selected rows."); }
                if ((subscenarioResults == null || subscenarioResults.Count == 0) && engine.Results.Count != 0) { subscenarioResults = engine.Results; }
                CollectEngineErrors(engine.Errors, "Boundary");
                try { engine.Dispose(); }
                catch (Exception exception) { finalErrors.Add("Boundary engine disposal failed: " + exception.GetType().Name + ": " + exception.Message); }
                CollectEngineErrors(engine.Errors, "Boundary");
            }
            foreach (var engineError in scenarioEngineErrors)
            {
                if (!finalErrors.Contains(engineError)) { finalErrors.Add(engineError); }
            }
        }

        private void TryWriteEmergencySaveBackedFailure(Exception exception)
        {
            try
            {
                var modAssembly = typeof(Main).Assembly;
                var gameAssembly = typeof(Kingmaker.Game).Assembly;
                var ummAssembly = typeof(UnityModManager).Assembly;
                var harmonyPath = Path.Combine(Path.GetDirectoryName(ummAssembly.Location), "0Harmony12.dll");
                var now = DateTimeOffset.UtcNow;
                var failureText = "Runtime completion failed: " + exception.GetType().FullName + ": " + exception.Message;
                var failureName = RuntimeRequest.IsMissionScenario(request.Scenario)
                    ? request.Scenario
                    : "observe-mount-diagnostic-availability";
                var emergency = CreateBootstrapFailureV2(request, loadedModId, modAssembly, gameAssembly, ummAssembly,
                    harmonyPath, now, failureName, failureText);
                emergency.StartedAtUtc = startedAt.ToString("o");
                emergency.FrameCount = frameCount;
                emergency.ElapsedSeconds = elapsedSeconds;
                emergency.SaveRequestCount = saveRequestCount;
                emergency.LoadRequestCount = loadRequestCount;
                emergency.FixtureIdentityVerified = fixtureIdentityVerified;
                emergency.BaselineLoadRequestCount = saveAuthorization.BaselineLoadRequestCount;
                emergency.WorkingLoadRequestCount = saveAuthorization.AuthorizedLoadCount;
                emergency.WorkingSaveRequestCount = saveAuthorization.AuthorizedWriteCount;
                emergency.UnauthorizedLoadRequestCount = saveAuthorization.UnauthorizedLoadCount;
                emergency.UnauthorizedSaveRequestCount = saveAuthorization.UnauthorizedWriteCount;
                emergency.EvidenceManifestSha256 = EnsureRuntimeArtifactManifest();
                try { emergency.RelationshipState = relationshipStateProvider() ?? "Unavailable"; } catch { emergency.RelationshipState = "Unavailable"; }
                try { emergency.MovementExperimentEnabled = movementExperimentProvider(); } catch { emergency.MovementExperimentEnabled = false; }
                WriteJsonAtomic(resultPath, emergency);
            }
            catch (Exception writeException)
            {
                logger.Exception("Emergency save-backed runtime result", writeException);
            }
        }

        private static IReadOnlyList<RuntimeSubscenarioResult> ReplaceFirstWithFailure(
            IReadOnlyList<RuntimeSubscenarioResult> source,
            RuntimeSubscenarioResult first,
            IReadOnlyList<string> failureErrors)
        {
            var replaced = new List<RuntimeSubscenarioResult>(source.Count);
            replaced.Add(new RuntimeSubscenarioResult
            {
                Name = first.Name,
                Status = "FAIL",
                AssertionPassCount = first.AssertionPassCount,
                AssertionFailCount = Math.Max(1, first.AssertionFailCount),
                Errors = failureErrors
            });
            for (var index = 1; index < source.Count; index++)
            {
                replaced.Add(source[index]);
            }
            return replaced;
        }

        private static RuntimeGameResultV2 CreateBootstrapFailureV2(
            RuntimeRequest request,
            string loadedModId,
            Assembly modAssembly,
            Assembly gameAssembly,
            Assembly ummAssembly,
            string harmonyPath,
            DateTimeOffset now,
            string failureName,
            string failureText)
        {
            var failedSubscenario = new RuntimeSubscenarioResult
            {
                Name = failureName,
                Status = "FAIL",
                AssertionPassCount = 0,
                AssertionFailCount = 1,
                Errors = new[] { failureText }
            };
            var game = Kingmaker.Game.Instance;
            return new RuntimeGameResultV2
            {
                SchemaVersion = RuntimeRequest.SaveBackedSchemaVersion,
                RunId = request.RunId,
                Scenario = request.Scenario,
                Status = "FAIL",
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = ComputeSha256(modAssembly.Location),
                DllMvid = modAssembly.ManifestModule.ModuleVersionId.ToString(),
                TransactionToken = request.TransactionToken,
                StartedAtUtc = now.ToString("o"),
                CompletedAtUtc = now.ToString("o"),
                LoadedModId = loadedModId,
                GameVersion = Kingmaker.GameVersion.GetVersion(),
                GameAssemblySha256 = ComputeSha256(gameAssembly.Location),
                GameAssemblyMvid = gameAssembly.ManifestModule.ModuleVersionId.ToString(),
                UmmVersion = ummAssembly.GetName().Version.ToString(),
                UmmSha256 = ComputeSha256(ummAssembly.Location),
                Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : "Unavailable",
                Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : new string('0', 64),
                RelationshipState = "Unavailable",
                MovementExperimentEnabled = false,
                ProcessId = Process.GetCurrentProcess().Id,
                CurrentGameMode = game == null ? "Unavailable" : game.CurrentMode.ToString(),
                LoadedAreaPresent = game != null && game.CurrentlyLoadedArea != null,
                SaveRequestCount = 0,
                LoadRequestCount = 0,
                FrameCount = 0,
                ElapsedSeconds = 0.0d,
                Errors = new[] { failureText },
                Fixture = request.Fixture,
                FixtureIdentityVerified = false,
                BaselineLoadRequestCount = 0,
                WorkingLoadRequestCount = 0,
                WorkingSaveRequestCount = 0,
                UnauthorizedLoadRequestCount = 0,
                UnauthorizedSaveRequestCount = 0,
                SubscenarioTotal = 1,
                SubscenarioPassCount = 0,
                SubscenarioFailCount = 1,
                AssertionPassCount = 0,
                AssertionFailCount = 1,
                EvidenceManifestSha256 = EnsureRuntimeArtifactManifest(request),
                SubscenarioResults = new[] { failedSubscenario }
            };
        }

        private void TryCompleteFailure(Exception exception)
        {
            try
            {
                Complete("FAIL", new[] { exception.GetType().FullName + ": " + exception.Message });
            }
            catch (Exception writeException)
            {
                logger.Exception("Runtime automation failure result", writeException);
            }
        }

        private static string FindSingleArgument(string[] arguments, string name)
        {
            string found = null;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (found != null || index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                {
                    throw new InvalidOperationException(name + " command-line argument is missing or ambiguous.");
                }

                found = arguments[++index];
            }

            return found;
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private string EnsureRuntimeArtifactManifest()
        {
            return EnsureRuntimeArtifactManifest(request);
        }

        private static string EnsureRuntimeArtifactManifest(RuntimeRequest request)
        {
            var manifestPath = Path.Combine(request.EvidenceRoot, "runtime-artifacts.json");
            if (File.Exists(manifestPath))
            {
                return ComputeSha256(manifestPath);
            }

            var artifacts = new List<RuntimeArtifactRecord>();
            AddRuntimeArtifactIfPresent(artifacts, request.EvidenceRoot, "lifecycle-scenario-evidence.jsonl", "scenario-evidence");
            AddRuntimeArtifactIfPresent(artifacts, request.EvidenceRoot, "movement-telemetry.jsonl", "telemetry");
            AddRuntimeArtifactIfPresent(artifacts, request.EvidenceRoot, "movement-scenario-evidence.jsonl", "scenario-evidence");

            var visualRoot = Path.Combine(request.EvidenceRoot, "movement-visuals");
            if (Directory.Exists(visualRoot))
            {
                if ((File.GetAttributes(visualRoot) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException("Movement visual evidence directory is a reparse point.");
                }
                foreach (var path in Directory.GetFiles(visualRoot).OrderBy(value => value, StringComparer.Ordinal))
                {
                    var leaf = Path.GetFileName(path);
                    if (!leaf.EndsWith(".png", StringComparison.Ordinal) ||
                        leaf.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        throw new InvalidOperationException("Movement visual evidence contains a non-allowlisted file.");
                    }
                    AddRuntimeArtifact(artifacts, path, "movement-visuals/" + leaf, "screenshot");
                }
                if (Directory.GetDirectories(visualRoot).Length != 0)
                {
                    throw new InvalidOperationException("Movement visual evidence contains a nested directory.");
                }
            }

            var manifest = new RuntimeArtifactManifest
            {
                SchemaVersion = 1,
                RunId = request.RunId,
                Scenario = request.Scenario,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                Artifacts = artifacts
            };
            WriteJsonAtomic(manifestPath, manifest);
            return ComputeSha256(manifestPath);
        }

        private static void AddRuntimeArtifactIfPresent(List<RuntimeArtifactRecord> records, string root, string leaf, string kind)
        {
            var path = Path.Combine(root, leaf);
            if (File.Exists(path))
            {
                AddRuntimeArtifact(records, path, leaf, kind);
            }
        }

        private static void AddRuntimeArtifact(List<RuntimeArtifactRecord> records, string path, string relativePath, string kind)
        {
            var item = new FileInfo(path);
            if (!item.Exists || (item.Attributes & FileAttributes.ReparsePoint) != 0 || item.Length <= 0)
            {
                throw new InvalidOperationException("Runtime artifact is missing, empty, or a reparse point: " + relativePath + ".");
            }
            records.Add(new RuntimeArtifactRecord
            {
                RelativePath = relativePath,
                Kind = kind,
                Length = item.Length,
                Sha256 = ComputeSha256(item.FullName)
            });
        }

        private static void WriteJsonAtomic(string path, object value)
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException("Runtime result path already exists.");
            }

            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("Runtime evidence directory is missing.");
            }

            var temporary = Path.Combine(directory, ".runtime-game-result." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(value, JsonSettings));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private sealed class RuntimeGameResult
        {
            public int SchemaVersion { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Status { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string TransactionToken { get; set; }
            public string StartedAtUtc { get; set; }
            public string CompletedAtUtc { get; set; }
            public string LoadedModId { get; set; }
            public string GameVersion { get; set; }
            public string GameAssemblySha256 { get; set; }
            public string GameAssemblyMvid { get; set; }
            public string UmmVersion { get; set; }
            public string UmmSha256 { get; set; }
            public string Harmony12Version { get; set; }
            public string Harmony12Sha256 { get; set; }
            public string RelationshipState { get; set; }
            public bool MovementExperimentEnabled { get; set; }
            public int ProcessId { get; set; }
            public string CurrentGameMode { get; set; }
            public bool LoadedAreaPresent { get; set; }
            public int SaveRequestCount { get; set; }
            public int LoadRequestCount { get; set; }
            public int FrameCount { get; set; }
            public double ElapsedSeconds { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
        }

        private sealed class RuntimeGameResultV2
        {
            public int SchemaVersion { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Status { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string TransactionToken { get; set; }
            public string StartedAtUtc { get; set; }
            public string CompletedAtUtc { get; set; }
            public string LoadedModId { get; set; }
            public string GameVersion { get; set; }
            public string GameAssemblySha256 { get; set; }
            public string GameAssemblyMvid { get; set; }
            public string UmmVersion { get; set; }
            public string UmmSha256 { get; set; }
            public string Harmony12Version { get; set; }
            public string Harmony12Sha256 { get; set; }
            public string RelationshipState { get; set; }
            public bool MovementExperimentEnabled { get; set; }
            public int ProcessId { get; set; }
            public string CurrentGameMode { get; set; }
            public bool LoadedAreaPresent { get; set; }
            public int SaveRequestCount { get; set; }
            public int LoadRequestCount { get; set; }
            public int FrameCount { get; set; }
            public double ElapsedSeconds { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
            public RuntimeFixtureIdentity Fixture { get; set; }
            public bool FixtureIdentityVerified { get; set; }
            public int BaselineLoadRequestCount { get; set; }
            public int WorkingLoadRequestCount { get; set; }
            public int WorkingSaveRequestCount { get; set; }
            public int UnauthorizedLoadRequestCount { get; set; }
            public int UnauthorizedSaveRequestCount { get; set; }
            public int SubscenarioTotal { get; set; }
            public int SubscenarioPassCount { get; set; }
            public int SubscenarioFailCount { get; set; }
            public int AssertionPassCount { get; set; }
            public int AssertionFailCount { get; set; }
            public string EvidenceManifestSha256 { get; set; }
            public IReadOnlyList<RuntimeSubscenarioResult> SubscenarioResults { get; set; }
        }

        private sealed class RuntimeArtifactManifest
        {
            public int SchemaVersion { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string CreatedAtUtc { get; set; }
            public IReadOnlyList<RuntimeArtifactRecord> Artifacts { get; set; }
        }

        private sealed class RuntimeArtifactRecord
        {
            public string RelativePath { get; set; }
            public string Kind { get; set; }
            public long Length { get; set; }
            public string Sha256 { get; set; }
        }
    }
}
