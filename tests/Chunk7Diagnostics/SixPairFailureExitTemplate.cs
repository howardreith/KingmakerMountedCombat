// Compiles the unchanged retained six-pair FAIL exit partial with the actual
// completion gate and the host's actual drain, atomic writer, argument and hash
// methods. Native persistence, loading and the scenario engine are facades; the
// process command line is real. Offline evidence only; no native qualification.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine { static class Application { internal static int Quits; internal static bool FailQuit; internal static void Quit() { Quits++; if (FailQuit) throw new InvalidOperationException("quit refused"); } } }
namespace UnityModManagerNet { class UnityModManager { } }
namespace Kingmaker { class Game { } }
namespace Kingmaker.EntitySystem.Persistence { class LoadingProcess { internal static LoadingProcess Instance = new LoadingProcess(); internal bool IsLoadingInProcess; internal string[] QueuedNames = new string[0]; } }
namespace KingmakerMountedCombat { class Main { } }
namespace KingmakerMountedCombat.Integration { static class NativePersistenceIsolation { internal static bool HasPendingWrites; } }
namespace KingmakerMountedCombat.Diagnostics
{
    using Kingmaker.EntitySystem.Persistence;
    using KingmakerMountedCombat.Integration;

    class RuntimeSubscenarioResult { public string Name, Status; public int AssertionPassCount, AssertionFailCount; public IReadOnlyList<string> Errors; }
    class RuntimeGameResultV2
    {
        public object StartupTrace; public string Status, StartedAtUtc, RelationshipState, EvidenceManifestSha256;
        public int FrameCount, SaveRequestCount, LoadRequestCount, BaselineLoadRequestCount, WorkingLoadRequestCount, WorkingSaveRequestCount,
            SuppressedWorkingSaveRequestCount, UnauthorizedLoadRequestCount, UnauthorizedSaveRequestCount,
            SubscenarioTotal, SubscenarioPassCount, SubscenarioFailCount, AssertionPassCount, AssertionFailCount;
        public double ElapsedSeconds; public bool FixtureIdentityVerified, MovementExperimentEnabled;
        public IReadOnlyList<string> Errors; public IReadOnlyList<RuntimeSubscenarioResult> SubscenarioResults;
    }
    class Selection
    {
        public bool IsSixPairStress = true; public int ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount; public List<string> Errors = new List<string>();
        public IReadOnlyList<string> Validate(string scenario, string runId, string checkpoint) =>
            scenario == "chunk7-owned-two-pair-campaign" && runId == "b188-exit" && checkpoint == null ? Errors : new List<string> { "selection" };
    }
    class RuntimeRequest
    {
        public const int SaveBackedSchemaVersion = 2; public int SchemaVersion = 2; public Selection OwnedPairs = new Selection();
        public string Scenario = "chunk7-owned-two-pair-campaign", RunId = "b188-exit", PersistenceCase, EvidenceRoot, TransactionToken;
    }
    class SaveAuthorization { public int BaselineLoadRequestCount, AuthorizedLoadCount = 1, UnauthorizedLoadCount, AuthorizedWriteCount, UnauthorizedWriteCount, SuppressedWorkingWriteCount; }
    class Persistence { public bool HasActiveSaveScope, SaveDraining, ActiveSaveWorkerRunning, ActiveSaveWorkerCached, HasSaveAdmissionFence, SaveSuspended, AreaTransitionPending, LoadInFlight; public int SnapshotCount; }
    class World { public bool SerializationBusy; }
    class Relationship { public World World = new World(); }
    class Chunk7WorldScenarioEngine
    {
        internal const string CleanupPendingFileName = "chunk7-world-cleanup-pending.json";
        public bool RefuseScope; public List<string> ErrorList = new List<string> { "C7 causal retained cleanup failure" };
        public List<RuntimeSubscenarioResult> Rows = new List<RuntimeSubscenarioResult> {
            new RuntimeSubscenarioResult { Name = "C7-six-native-mounts", Status = "PASS", AssertionPassCount = 1, Errors = new string[0] },
            new RuntimeSubscenarioResult { Name = "C7-world-cleanup", Status = "FAIL", AssertionFailCount = 1, Errors = new[] { "C7 causal retained cleanup failure" } } };
        internal IReadOnlyList<string> Errors => ErrorList;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => Rows;
        internal string RetainedFailureArtifactSha256;
        internal void RequireRetainedSixFailureScope() { if (RefuseScope) throw new InvalidOperationException("No committed retained six-pair failure owns this world."); }
    }
    class Log { public int Exceptions; public List<string> Warnings = new List<string>(); public void Exception(string text, Exception error) { Exceptions++; Console.WriteLine("LOG " + text + ": " + error.GetType().Name + ": " + error.Message); } public void Warning(string text) { Warnings.Add(text); } }
    class Trace { public object Capture() => "startup-trace"; }
    class Clock { public TimeSpan Elapsed; }

    internal sealed partial class RuntimeAutomationHost
    {
        private const string RequestArgument = "-kmcRuntimeRequest";
        private const string TokenArgument = "-kmcRuntimeToken";
        private const string RequestHashArgument = "-kmcRuntimeRequestSha256";
        // The game's Unity build of Json.NET installs Unity converters when settings are
        // constructed; defaults serialize this facade result without loading Unity.
        private static readonly JsonSerializerSettings JsonSettings = null;
        private static RuntimeAutomationHost active;
        internal readonly RuntimeRequest request;
        private readonly string resultPath;
        internal readonly Log logger = new Log();
        private readonly Clock runtimeClock = new Clock();
        internal bool completionStarted, completed, disposed;
        private readonly Chunk7HostCompletionGate chunk7Completion = new Chunk7HostCompletionGate();
        internal string chunk7CompletionStatus = "FAIL";
        private IReadOnlyList<string> chunk7CompletionErrors = new[] { "Chunk 7 owned-pair fixture reported FAIL." };
        private readonly List<string> scenarioEngineErrors = new List<string>();
        internal Chunk7WorldScenarioEngine worldEngine = new Chunk7WorldScenarioEngine();
        internal bool fixtureIdentityVerified = true;
        internal int saveRequestCount, loadRequestCount = 1;
        internal readonly SaveAuthorization saveAuthorization = new SaveAuthorization();
        internal Persistence persistence = new Persistence();
        internal readonly Relationship relationship = new Relationship();
        private readonly Trace startupTrace = new Trace();
        private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        private readonly int frameCount = 4000;
        private readonly double elapsedSeconds = 120;
        private readonly string loadedModId = "KingmakerMountedCombat";
        private readonly Func<string> relationshipStateProvider = () => "Faulted";
        private readonly Func<bool> movementExperimentProvider = () => false;
        internal bool OwnersSettle; internal int OrdinaryCompletions, ManifestPublications; internal Action DuringManifest;

        internal RuntimeAutomationHost(string evidenceRoot, string token)
        {
            request = new RuntimeRequest { EvidenceRoot = evidenceRoot, TransactionToken = token };
            resultPath = Path.Combine(evidenceRoot, "runtime-game-result.json");
            active = this;
            chunk7Completion.Request(0);
        }
        internal string ResultPath => resultPath;
        internal void At(double seconds) => runtimeClock.Elapsed = TimeSpan.FromSeconds(seconds);
        internal void Drain() => DrainChunk7Completion();
        internal static void Deactivate() => active = null;
        private bool TrySettleChunk7Custody() => chunk7Completion.Poll(runtimeClock.Elapsed.TotalSeconds, false,
            () => true, () => true, () => true, () => OwnersSettle, error => { if (!scenarioEngineErrors.Contains(error)) scenarioEngineErrors.Add(error); });
        private void CompleteSaveBackedWhenIdle(string status, IReadOnlyList<string> errors) { OrdinaryCompletions++; completionStarted = completed = true; }
        private string PublishRuntimeArtifactManifest() { ManifestPublications++; DuringManifest?.Invoke(); return new string('a', 64); }
        private static RuntimeGameResultV2 CreateBootstrapFailureV2(RuntimeRequest request, string loadedModId, Assembly modAssembly, Assembly gameAssembly,
            Assembly ummAssembly, string harmonyPath, DateTimeOffset now, string failureName, string failureText) =>
            new RuntimeGameResultV2 { Status = "FAIL", Errors = new[] { failureText },
                SubscenarioResults = new[] { new RuntimeSubscenarioResult { Name = failureName, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { failureText } } } };

        /* DRAIN */
        /* WRITE_JSON */
        /* FIND_ARGUMENT */
        /* SHA_PATH */

        internal static string Argument(string name) => FindSingleArgument(Environment.GetCommandLineArgs(), name);
        internal static string Sha(string path) => ComputeSha256(path);
    }
}

internal static class Program
{
    private static int pass, fail;
    private static string root, requestPath, token, pending;
    private static byte[] requestBytes;

    private static void Check(string name, Func<bool> check)
    {
        try { if (!check()) throw new Exception("predicate false"); pass++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { fail++; Console.WriteLine("FAIL " + name + ": " + exception.Message); }
    }

    private static KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost Fresh(string evidenceRoot = null)
    {
        var folder = evidenceRoot ?? root;
        var result = Path.Combine(folder, "runtime-game-result.json");
        if (Directory.Exists(result)) Directory.Delete(result);
        if (File.Exists(result)) File.Delete(result);
        File.WriteAllText(Path.Combine(folder, KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine.CleanupPendingFileName), "{\"status\":\"FAIL\"}");
        UnityEngine.Application.Quits = 0; UnityEngine.Application.FailQuit = false;
        Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance = new Kingmaker.EntitySystem.Persistence.LoadingProcess();
        KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = false;
        var host = new KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost(folder, token);
        host.worldEngine.RetainedFailureArtifactSha256 = KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Sha(pending);
        return host;
    }

    private static bool Untouched(KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost host) =>
        !File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 0 && !host.completed && !host.completionStarted && host.OrdinaryCompletions == 0;

    private static void Refuses(string name, Action<KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost> mutate)
    {
        Check("retained exit refuses " + name, () => { var host = Fresh(); mutate(host); host.At(46); host.Drain(); return Untouched(host); });
    }

    private static int Main()
    {
        // The game's Unity build of Json.NET initializes Unity converters; resolve those
        // managed assemblies from the installed game instead of copying them.
        var managed = KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Argument("-kmcManagedDirectory");
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var candidate = Path.Combine(managed, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };
        requestPath = Path.GetFullPath(KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Argument("-kmcRuntimeRequest"));
        token = KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Argument("-kmcRuntimeToken");
        root = Path.GetDirectoryName(requestPath);
        pending = Path.Combine(root, KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine.CleanupPendingFileName);
        requestBytes = File.ReadAllBytes(requestPath);

        Check("ordinary settled completion never enters the retained exit", () =>
        { var host = Fresh(); host.OwnersSettle = true; host.At(1); host.Drain(); return host.OrdinaryCompletions == 1 && !File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 0; });
        Check("bounded drain before its deadline never exits", () => { var host = Fresh(); host.At(30); host.Drain(); return Untouched(host); });
        Check("exact retained six-pair FAIL publishes one honest result before one quit", () =>
        {
            var host = Fresh(); host.At(46); host.Drain();
            if (!File.Exists(host.ResultPath) || UnityEngine.Application.Quits != 1 || !host.completed || !host.completionStarted || host.OrdinaryCompletions != 0) return false;
            var json = JObject.Parse(File.ReadAllText(host.ResultPath));
            var errors = json["Errors"].Select(item => (string)item).ToArray();
            var once = KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Sha(host.ResultPath);
            host.At(60); host.Drain();
            return (string)json["Status"] == "FAIL" && (int)json["SubscenarioFailCount"] == 1 && (int)json["SubscenarioPassCount"] == 1 &&
                (int)json["SubscenarioTotal"] == 2 && (int)json["AssertionFailCount"] == 1 && (int)json["SaveRequestCount"] == 0 &&
                (int)json["LoadRequestCount"] == 1 && (int)json["WorkingLoadRequestCount"] == 1 && (int)json["WorkingSaveRequestCount"] == 0 &&
                (bool)json["FixtureIdentityVerified"] && errors[0] == "C7 causal retained cleanup failure" &&
                errors.Contains("Chunk 7 owned-pair fixture reported FAIL.") && errors.Any(error => error.StartsWith("Chunk 7 cleanup exceeded its bounded drain", StringComparison.Ordinal)) &&
                errors.Last().StartsWith("Disposable six-pair run failed with retained volatile cleanup custody", StringComparison.Ordinal) &&
                host.ManifestPublications == 1 && UnityEngine.Application.Quits == 1 && KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Sha(host.ResultPath) == once;
        });
        Check("retained exit attempts at most once per second", () =>
        {
            var host = Fresh(); KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = true; host.At(46); host.Drain();
            KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = false; host.At(46.5); host.Drain();
            var throttled = Untouched(host); host.At(47.1); host.Drain();
            return throttled && File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 1;
        });
        Check("facts changed during result creation publish nothing, then retry exactly", () =>
        {
            var host = Fresh(); host.DuringManifest = () => KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = true;
            host.At(46); host.Drain(); var refused = Untouched(host);
            KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = false; host.DuringManifest = null; host.At(47.5); host.Drain();
            return refused && File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 1;
        });
        Check("publication failure never quits and later publishes once", () =>
        {
            var host = Fresh(); Directory.CreateDirectory(host.ResultPath); host.At(46); host.Drain();
            var refused = !File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 0 && !host.completed && host.logger.Exceptions == 1;
            Directory.Delete(host.ResultPath); host.At(47.5); host.Drain();
            return refused && File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 1;
        });
        Check("a result without its causal failed row is refused", () =>
        { var host = Fresh(); host.worldEngine.Rows.RemoveAt(1); host.At(46); host.Drain(); return Untouched(host) && host.logger.Exceptions == 1; });
        Check("a refused quit is contained after its single request", () =>
        {
            var host = Fresh(); UnityEngine.Application.FailQuit = true; host.At(46); host.Drain(); host.At(60); host.Drain();
            return File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 1 && host.completed && host.logger.Exceptions == 1;
        });

        Refuses("a non-six-pair selection", host => host.request.OwnedPairs.IsSixPairStress = false);
        Refuses("a declared native write", host => host.request.OwnedPairs.ExpectedNativeWriteCount = 1);
        Refuses("a second declared load", host => host.request.OwnedPairs.ExpectedNativeLoadCount = 2);
        Refuses("an invalid selection", host => host.request.OwnedPairs.Errors.Add("invalid"));
        Refuses("another scenario", host => host.request.Scenario = "chunk7-two-pair-exploration");
        Refuses("a no-save request", host => host.request.SchemaVersion = 1);
        Refuses("a PASS completion", host => host.chunk7CompletionStatus = "PASS");
        Refuses("an absent world engine", host => host.worldEngine = null);
        Refuses("an unverified fixture", host => host.fixtureIdentityVerified = false);
        Refuses("a save request", host => host.saveRequestCount = 1);
        Refuses("a second load request", host => host.loadRequestCount = 2);
        Refuses("a baseline load", host => host.saveAuthorization.BaselineLoadRequestCount = 1);
        Refuses("no authorized load", host => host.saveAuthorization.AuthorizedLoadCount = 0);
        Refuses("an unauthorized load", host => host.saveAuthorization.UnauthorizedLoadCount = 1);
        Refuses("an authorized write", host => host.saveAuthorization.AuthorizedWriteCount = 1);
        Refuses("an unauthorized write", host => host.saveAuthorization.UnauthorizedWriteCount = 1);
        Refuses("a suppressed write", host => host.saveAuthorization.SuppressedWorkingWriteCount = 1);
        Refuses("absent persistence", host => host.persistence = null);
        Refuses("an active save scope", host => host.persistence.HasActiveSaveScope = true);
        Refuses("a draining save", host => host.persistence.SaveDraining = true);
        Refuses("a running save worker", host => host.persistence.ActiveSaveWorkerRunning = true);
        Refuses("a cached save worker", host => host.persistence.ActiveSaveWorkerCached = true);
        Refuses("a save admission fence", host => host.persistence.HasSaveAdmissionFence = true);
        Refuses("a suspended save", host => host.persistence.SaveSuspended = true);
        Refuses("a pending area transition", host => host.persistence.AreaTransitionPending = true);
        Refuses("an in-flight load", host => host.persistence.LoadInFlight = true);
        Refuses("a persistence snapshot", host => host.persistence.SnapshotCount = 1);
        Refuses("pending native writes", host => KingmakerMountedCombat.Integration.NativePersistenceIsolation.HasPendingWrites = true);
        Refuses("an absent world", host => host.relationship.World = null);
        Refuses("busy serialization", host => host.relationship.World.SerializationBusy = true);
        Refuses("an active native load", host => Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess = true);
        Refuses("a queued native load", host => Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.QueuedNames = new[] { "queued" });
        Refuses("an unadmitted engine scope", host => host.worldEngine.RefuseScope = true);
        Refuses("another transaction token", host => host.request.TransactionToken = "other-token");
        Refuses("a missing retained artifact", host => File.Delete(pending));
        Refuses("a changed retained artifact", host => File.AppendAllText(pending, " "));
        Refuses("an unhashed retained artifact", host => host.worldEngine.RetainedFailureArtifactSha256 = null);
        Refuses("an inactive host", host => KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Deactivate());
        Refuses("a disposed host", host => host.disposed = true);
        Check("retained exit refuses a started completion", () =>
        { var host = Fresh(); host.completionStarted = true; host.At(46); host.Drain(); return !File.Exists(host.ResultPath) && UnityEngine.Application.Quits == 0 && !host.completed; });
        Check("retained exit refuses changed request bytes", () =>
        {
            var host = Fresh(); File.AppendAllText(requestPath, " ");
            try { host.At(46); host.Drain(); return Untouched(host); }
            finally { File.WriteAllBytes(requestPath, requestBytes); }
        });
        Check("retained exit refuses another request location", () =>
        {
            var other = Path.Combine(root, "other");
            Directory.CreateDirectory(other); File.WriteAllBytes(Path.Combine(other, "runtime-request.json"), requestBytes);
            File.WriteAllText(Path.Combine(other, KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine.CleanupPendingFileName), "{\"status\":\"FAIL\"}");
            var host = Fresh(other); host.worldEngine.RetainedFailureArtifactSha256 = KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.Sha(Path.Combine(other, KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine.CleanupPendingFileName));
            host.At(46); host.Drain(); return Untouched(host);
        });
        Check("retained exit never replaces a preexisting result", () =>
        {
            var host = Fresh(); File.WriteAllText(host.ResultPath, "prior"); host.At(46); host.Drain();
            return File.ReadAllText(host.ResultPath) == "prior" && UnityEngine.Application.Quits == 0 && !host.completed;
        });

        Console.WriteLine("SIX-PAIR FAILURE EXIT PASS=" + pass + " FAIL=" + fail);
        return fail == 0 ? 0 : 1;
    }
}
