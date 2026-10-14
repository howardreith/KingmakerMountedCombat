// Actual publisher, immutable artifact writer, hashing and atomic replacement methods
// are inserted. Only native loader inventory enumeration is replaced by a fixture.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
internal sealed class Main { }
internal sealed class RuntimeRequest
{
    internal string EvidenceRoot, RunId, Scenario;
    internal string Branch = "private-generated-fixture", Commit = new string('a', 40), ProductVersion = "private-generated-fixture";
}
__CONSTANT_CLASSES__
internal static class RuntimeLoaderInventory
{
    __LOADER_CONSTANTS__
    internal static void EnsureWritten(RuntimeRequest request)
    {
        if (!File.Exists(Path.Combine(request.EvidenceRoot, EvidenceFileName)))
            WriteArtifact(request, EvidenceFileName, EvidenceKind, "PASS", new JObject { ["externalNativeLoaderStub"] = true }, Array.Empty<string>());
    }
    __WRITER_METHODS__
}
internal static class RuntimeAutomationHost
{
    __JSON_SETTINGS__
    __PUBLISHER_METHODS__
    internal static string Publish(RuntimeRequest request) => PublishRuntimeArtifactManifest(request);
}
internal static class Program
{
    public static int Main(string[] args)
    {
        var managed = Path.GetFullPath(Path.GetDirectoryName(args[1]));
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            var name = new System.Reflection.AssemblyName(request.Name).Name;
            if (name != "UnityEngine" && !name.StartsWith("UnityEngine.", StringComparison.Ordinal)) return null;
            var path = Path.GetFullPath(Path.Combine(managed, name + ".dll"));
            if (!path.StartsWith(managed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        };
        System.Reflection.Assembly.LoadFrom(args[1]);
        return ArtifactTest.Run(args);
    }
}
internal static class ArtifactTest
{
    private static int passed, failed;
    private static readonly JArray cases = new JArray();
    private static string root;
    private static void Check(string name, bool value)
    { Console.WriteLine((value ? "PASS " : "FAIL ") + name); if (value) passed++; else failed++; }
    private static bool Throws(Action action)
    { try { action(); return false; } catch (Exception error) when (error is IOException || error is InvalidOperationException) { return true; } }
    private static RuntimeRequest Request(string name, string scenario = "chunk7-native-main-bootstrap")
    { var path = Path.Combine(root, name); Directory.CreateDirectory(path); return new RuntimeRequest { EvidenceRoot = path, RunId = name, Scenario = scenario }; }
    private static JObject Read(string path) => JObject.Parse(File.ReadAllText(path));
    private static string Hash(string path)
    { using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    private static void Write(RuntimeRequest request, string leaf, string kind, string status = "FAIL")
    { RuntimeLoaderInventory.WriteArtifact(request, leaf, kind, status, new JObject { ["cleanupCustodySettled"] = true, ["nativeLoads"] = 0, ["nativeWrites"] = 0 }, status == "FAIL" ? new[] { "preserved native preflight refusal" } : Array.Empty<string>()); }
    private static void CheckManifest(RuntimeRequest request, params string[] expectedLeaves)
    {
        var manifestPath = Path.Combine(request.EvidenceRoot, "runtime-artifacts.json");
        var hash = RuntimeAutomationHost.Publish(request);
        var artifactRows = (JArray)Read(manifestPath)["artifacts"];
        Check(request.RunId + " exact manifest hash", hash == Hash(manifestPath));
        Check(request.RunId + " exact closed artifact set", artifactRows.Count == expectedLeaves.Length + 1 &&
            expectedLeaves.All(leaf => artifactRows.Count(row => (string)row["relativePath"] == leaf) == 1) &&
            artifactRows.Count(row => (string)row["relativePath"] == RuntimeLoaderInventory.EvidenceFileName) == 1);
        Check(request.RunId + " all observed bytes and hashes match", artifactRows.All(row => {
            var path = Path.Combine(request.EvidenceRoot, (string)row["relativePath"]);
            return (long)row["length"] == new FileInfo(path).Length && (string)row["sha256"] == Hash(path); }));
        cases.Add(new JObject { ["evidenceRoot"] = request.EvidenceRoot, ["runId"] = request.RunId, ["scenario"] = request.Scenario });
    }
    private static void Neighbor(string name, string scenario, string finalLeaf, string finalKind, string pendingLeaf = null, string pendingKind = null)
    {
        var request = Request(name, scenario); Write(request, finalLeaf, finalKind);
        if (pendingLeaf != null) { Write(request, pendingLeaf, pendingKind); CheckManifest(request, finalLeaf, pendingLeaf); }
        else CheckManifest(request, finalLeaf);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static int Run(string[] args)
    {
        root = args[0]; Directory.CreateDirectory(root);
        var request = Request("bootstrap-failure");
        Write(request, Chunk7NativeBootstrapScenario.EvidenceFileName, Chunk7NativeBootstrapScenario.EvidenceKind);
        CheckManifest(request, Chunk7NativeBootstrapScenario.EvidenceFileName);
        var artifactPath = Path.Combine(request.EvidenceRoot, Chunk7NativeBootstrapScenario.EvidenceFileName);
        var failureHash = Hash(artifactPath);
        Check("manifest publication preserves native FAIL and refusal", (string)Read(artifactPath)["status"] == "FAIL" && (string)Read(artifactPath)["errors"][0] == "preserved native preflight refusal");
        Check("immutable final writer refuses duplicate", Throws(() => Write(request, Chunk7NativeBootstrapScenario.EvidenceFileName, Chunk7NativeBootstrapScenario.EvidenceKind)) && Hash(artifactPath) == failureHash);
        request = Request("bootstrap-success"); Write(request, Chunk7NativeBootstrapScenario.EvidenceFileName, Chunk7NativeBootstrapScenario.EvidenceKind, "PASS");
        CheckManifest(request, Chunk7NativeBootstrapScenario.EvidenceFileName);
        request = Request("bootstrap-pending-only"); Write(request, Chunk7NativeBootstrapScenario.PendingFileName, Chunk7NativeBootstrapScenario.PendingKind);
        CheckManifest(request, Chunk7NativeBootstrapScenario.PendingFileName);
        request = Request("bootstrap-pending-final"); Write(request, Chunk7NativeBootstrapScenario.PendingFileName, Chunk7NativeBootstrapScenario.PendingKind);
        var pendingPath = Path.Combine(request.EvidenceRoot, Chunk7NativeBootstrapScenario.PendingFileName);
        var pendingHash = Hash(pendingPath);
        var priorManifestHash = RuntimeAutomationHost.Publish(request);
        Write(request, Chunk7NativeBootstrapScenario.EvidenceFileName, Chunk7NativeBootstrapScenario.EvidenceKind);
        var manifestPath = Path.Combine(request.EvidenceRoot, "runtime-artifacts.json");
        using (var hold = new FileStream(manifestPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check("locked manifest publication fails instead of claiming flush", Throws(() => RuntimeAutomationHost.Publish(request)));
        Check("failed publication preserves prior manifest and pending evidence", Hash(manifestPath) == priorManifestHash && Hash(pendingPath) == pendingHash && Directory.GetFiles(request.EvidenceRoot, "*.tmp").Length == 0);
        CheckManifest(request, Chunk7NativeBootstrapScenario.PendingFileName, Chunk7NativeBootstrapScenario.EvidenceFileName);
        Check("retry adds final artifact without stale manifest reuse", Hash(manifestPath) != priorManifestHash && Hash(pendingPath) == pendingHash &&
            ((JArray)Read(manifestPath)["artifacts"]).Any(row => (string)row["relativePath"] == Chunk7NativeBootstrapScenario.EvidenceFileName));
        request = Request("bootstrap-no-artifact"); CheckManifest(request);
        request = Request("bootstrap-empty-artifact"); File.WriteAllText(Path.Combine(request.EvidenceRoot, Chunk7NativeBootstrapScenario.EvidenceFileName), "");
        Check("empty bootstrap evidence fails producer guard", Throws(() => RuntimeAutomationHost.Publish(request)));

        Neighbor("neighbor-assisted", Chunk7AssistedFixtureSelection.Scenario, Chunk7AssistedFixtureScenario.EvidenceFileName, Chunk7AssistedFixtureScenario.EvidenceKind, Chunk7AssistedFixtureScenario.PendingFileName, Chunk7AssistedFixtureScenario.PendingKind);
        Neighbor("neighbor-working-observation", "export-mounted-contracts", WorkingLoadDiagnosticSession.EvidenceFileName, WorkingLoadDiagnosticSession.EvidenceKind, WorkingLoadDiagnosticSession.PendingFileName, WorkingLoadDiagnosticSession.PendingKind);
        Neighbor("neighbor-acquired-manual", Chunk7AcquiredManualSelection.Scenario, Chunk7AcquiredManualScenario.EvidenceFileName, Chunk7AcquiredManualScenario.EvidenceKind, Chunk7AcquiredManualScenario.PendingFileName, Chunk7AcquiredManualScenario.PendingKind);
        Neighbor("neighbor-manual-derived", Chunk7ManualDerivedSelection.Scenario, Chunk7ManualDerivedScenario.EvidenceFileName, Chunk7ManualDerivedScenario.EvidenceKind, Chunk7ManualDerivedScenario.PendingFileName, Chunk7ManualDerivedScenario.PendingKind);
        Neighbor("neighbor-manual-seed", Chunk7ManualSeedSelection.Scenario, Chunk7ManualSeedScenario.EvidenceFileName, Chunk7ManualSeedScenario.EvidenceKind, Chunk7ManualSeedScenario.PendingFileName, Chunk7ManualSeedScenario.PendingKind);
        Neighbor("neighbor-inventory", Chunk7BootstrapInventorySelection.Scenario, Chunk7BootstrapInventoryScenario.EvidenceFileName, Chunk7BootstrapInventoryScenario.EvidenceKind, Chunk7BootstrapInventoryScenario.PendingFileName, Chunk7BootstrapInventoryScenario.PendingKind);
        Neighbor("neighbor-small", Chunk7SmallCalibrationSelection.Scenario, Chunk7SmallCalibrationScenario.EvidenceFileName, Chunk7SmallCalibrationScenario.EvidenceKind, Chunk7SmallCalibrationScenario.PendingFileName, Chunk7SmallCalibrationScenario.PendingKind);
        Neighbor("neighbor-progression", Chunk7ProgressionSelection.Scenario, Chunk7ProgressionScenario.EvidenceFileName, Chunk7ProgressionScenario.EvidenceKind, Chunk7ProgressionScenario.PendingFileName, Chunk7ProgressionScenario.PendingKind);
        Neighbor("neighbor-field", Chunk7OrdinaryFieldSelection.Scenario, Chunk7OrdinaryFieldScenario.EvidenceFileName, Chunk7OrdinaryFieldScenario.EvidenceKind, Chunk7OrdinaryFieldScenario.PendingFileName, Chunk7OrdinaryFieldScenario.PendingKind);
        Neighbor("neighbor-acquisition", Chunk7OrdinaryAcquisitionSelection.Scenario, Chunk7OrdinaryAcquisitionScenario.EvidenceFileName, Chunk7OrdinaryAcquisitionScenario.EvidenceKind, Chunk7OrdinaryAcquisitionScenario.PendingFileName, Chunk7OrdinaryAcquisitionScenario.PendingKind);
        Neighbor("neighbor-world", Chunk7WorldScenarioEngine.ScenarioName, Chunk7WorldScenarioEngine.EvidenceFileName, Chunk7WorldScenarioEngine.EvidenceKind, "chunk7-world-cleanup-pending.json", "chunk7-world-cleanup-pending");
        Neighbor("neighbor-persistence", "chunk7-persistence-load", Chunk7PersistenceScenario.EvidenceFileName, Chunk7PersistenceScenario.EvidenceKind, "chunk7-persistence-cleanup-pending.json", "chunk7-persistence-cleanup-pending");
        Neighbor("neighbor-combat", Chunk7CombatScenario.RiderFirst, Chunk7CombatScenario.EvidenceFileName, Chunk7CombatScenario.EvidenceKind, "chunk7-combat-cleanup-pending.json", "chunk7-combat-cleanup-pending");
        Neighbor("neighbor-matched-pool", Chunk7WorldScenarioEngine.ScenarioName, Chunk7MatchedPoolCollector.FileName, Chunk7MatchedPoolCollector.Kind);
        File.WriteAllText(Path.Combine(root, "cases.json"), cases.ToString(Formatting.Indented));
        File.WriteAllText(Path.Combine(root, "producer-summary.json"), new JObject { ["pass"] = passed, ["fail"] = failed }.ToString());
        Console.WriteLine("ACTUAL ARTIFACT PRODUCER PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
