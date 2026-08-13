using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using KingmakerMountedCombat.Logging;
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
        private readonly string resultPath;
        private readonly DateTimeOffset startedAt;
        private int frameCount;
        private double elapsedSeconds;
        private bool completed;
        private bool disposed;
        private int saveRequestCount;
        private int loadRequestCount;

        private static RuntimeAutomationHost active;

        public string EvidenceRoot => request.EvidenceRoot;

        public string Scenario => request.Scenario;

        public string RunId => request.RunId;

        private RuntimeAutomationHost(IModLogger logger, RuntimeRequest request, string loadedModId, Func<string> relationshipStateProvider, Func<bool> movementExperimentProvider)
        {
            this.logger = logger;
            this.request = request;
            this.loadedModId = loadedModId;
            this.relationshipStateProvider = relationshipStateProvider;
            this.movementExperimentProvider = movementExperimentProvider;
            resultPath = Path.Combine(request.EvidenceRoot, "runtime-game-result.json");
            startedAt = DateTimeOffset.UtcNow;
            active = this;
        }

        public static RuntimeAutomationHost CreateFromCommandLine(IModLogger logger, string loadedModId, Func<string> relationshipStateProvider, Func<bool> movementExperimentProvider)
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

            var json = File.ReadAllText(fullRequestPath);
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
            return new RuntimeAutomationHost(logger, request, loadedModId, relationshipStateProvider, movementExperimentProvider);
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
                if (requestPath == null || token == null)
                {
                    return false;
                }

                var fullRequestPath = Path.GetFullPath(requestPath);
                var request = JsonConvert.DeserializeObject<RuntimeRequest>(File.ReadAllText(fullRequestPath), JsonSettings);
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
                    Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : null,
                    Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : null,
                    RelationshipState = "Unmounted",
                    MovementExperimentEnabled = false,
                    ProcessId = Process.GetCurrentProcess().Id,
                    CurrentGameMode = Kingmaker.Game.Instance == null ? null : Kingmaker.Game.Instance.CurrentMode.ToString(),
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
            elapsedSeconds += Math.Max(0.0f, deltaTime);
            if (frameCount < 10 || elapsedSeconds < 1.0d)
            {
                return;
            }

            try
            {
                if (!string.Equals(request.Scenario, "mod-load-smoke", StringComparison.Ordinal))
                {
                    Complete("FAIL", new[] { "This diagnostic build currently implements only mod-load-smoke." });
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

        public void Dispose()
        {
            disposed = true;
            if (ReferenceEquals(active, this)) { active = null; }
        }

        private void Complete(string status, IReadOnlyList<string> errors)
        {
            if (completed)
            {
                return;
            }

            var modAssembly = typeof(Main).Assembly;
            var gameAssembly = typeof(Kingmaker.Game).Assembly;
            var ummAssembly = typeof(UnityModManager).Assembly;
            var ummDirectory = Path.GetDirectoryName(ummAssembly.Location);
            var harmonyPath = Path.Combine(ummDirectory, "0Harmony12.dll");
            var result = new RuntimeGameResult
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

            WriteJsonAtomic(resultPath, result);
            completed = true;
            try
            {
                logger.Info("Runtime automation result committed: " + status);
            }
            finally
            {
                Application.Quit();
            }
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
    }
}
