using System;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class MatchedPoolCollectorTests
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    private static void Reject(Action work, string name) { bool refused = false; try { work(); } catch { refused = true; } Check(refused, name); }
    private sealed class Window : IChunk7MatchedPoolWindow
    {
        internal string Label;
        internal bool IsReady, FailRelease;
        internal int CompleteCount, ReleaseCount;
        public bool Ready => IsReady;
        public int Count => IsReady ? 120 : 37;
        public JObject Complete() { CompleteCount++; if (!Ready) throw new Exception("not ready"); return new JObject { ["label"] = Label }; }
        public void Dispose() { ReleaseCount++; if (FailRelease) throw new Exception("observer retained"); }
    }
    private sealed class Boundary
    {
        internal int Active, Guards, Captures, Creates, Comparisons;
        internal bool Refuse, Changed;
        internal Window Window;
        internal Chunk7MatchedPoolCollector Collector;
        internal Boundary()
        {
            Collector = new Chunk7MatchedPoolCollector(expected => {
                Guards++; if (Refuse || Active != expected) throw new Exception("native custody changed");
            }, expected => { Captures++; return new JObject { ["native"] = expected }; }, (before, after) => {
                Comparisons++; if (Changed || !JToken.DeepEquals(before, after)) throw new Exception("resource or owner changed");
            }, label => { Creates++; return Window = new Window { Label = label }; });
        }
    }
    public static int Main(string[] args)
    {
        try
        {
            var b = new Boundary();
            Reject(() => b.Collector.Begin(1), "cannot skip warmed settled-zero entry");
            Check(b.Creates == 0, "invalid sequence does not acquire sampler");
            b.Active = 1; Reject(() => b.Collector.Begin(0), "native expected owner count gates acquisition");
            Check(b.Creates == 0, "failed native admission leaves no sampler");
            b.Active = 0; b.Collector.Begin(0);
            Reject(() => b.Collector.Begin(0), "duplicate entry refuses without replacing observer");
            Check(!b.Collector.Poll() && b.Captures == 1 && b.Window.CompleteCount == 0, "warmup polls native guard without snapshots or completion");
            b.Refuse = true; Reject(() => b.Collector.Poll(), "changed native identity or custody refuses while warming up");
            Check(b.Window.ReleaseCount == 0 && !b.Collector.Released, "guard refusal retains acquired observer until cleanup");
            b.Collector.Dispose(); Check(b.Window.ReleaseCount == 1 && b.Collector.Released && !b.Collector.Complete, "partial cleanup releases observer without claiming completion");
            Check((string)b.Collector.Evidence()["abortedWindow"]["label"] == Chunk7MatchedPoolCollector.Labels[0] && (int)b.Collector.Evidence()["abortedWindow"]["sampleFrames"] == 37,
                "partial failure preserves exact aborted window summary");
            Reject(() => b.Collector.Begin(0), "disposed collector cannot restart");

            b = new Boundary(); b.Collector.Begin(0); b.Window.IsReady = true; b.Changed = true;
            Reject(() => b.Collector.Poll(), "native resource or owner change prevents timer completion");
            Check(b.Window.CompleteCount == 0 && b.Window.ReleaseCount == 0, "failed endpoint comparison preserves sampler ownership"); b.Collector.Dispose();

            b = new Boundary(); b.Collector.Begin(0); b.Window.IsReady = true; b.Window.FailRelease = true;
            Reject(() => b.Collector.Poll(), "failed sampler release prevents published row");
            Check(((JArray)b.Collector.Evidence()["rows"]).Count == 0 && b.Window.CompleteCount == 1, "release failure retains completed raw sample without row promotion");
            b.Window.FailRelease = false; Check(b.Collector.Poll() && b.Window.CompleteCount == 1 && b.Window.ReleaseCount == 2,
                "release retry does not resample or replay completion"); b.Collector.Dispose();

            b = new Boundary(); b.Collector.Begin(0); b.Window.IsReady = true; b.Window.FailRelease = true;
            Reject(() => b.Collector.Poll(), "completed observation retains failed release before scenario abort");
            Reject(() => b.Collector.Dispose(), "abort also retains same failed release");
            b.Window.FailRelease = false; b.Collector.Dispose();
            Check(b.Collector.Released && !b.Collector.Complete && (string)b.Collector.Evidence()["abortedWindow"]["completedRawRow"]["sample"]["label"] == Chunk7MatchedPoolCollector.Labels[0],
                "abort preserves completed raw sample without promoting incomplete sequence");

            b = new Boundary(); b.Collector.Begin(0); b.Window.FailRelease = true;
            Reject(() => b.Collector.Dispose(), "cleanup release failure remains retryable");
            Check(!b.Collector.Released, "failed cleanup cannot mark disposed");
            b.Window.FailRelease = false; b.Collector.Dispose(); Check(b.Collector.Released, "same observer later settles");

            b = new Boundary();
            for (int i = 0; i < 4; i++) { b.Active = Chunk7MatchedPoolCollector.Active[i]; b.Collector.Begin(i); b.Window.IsReady = true; Check(b.Collector.Poll(), "exact bounded window " + i); }
            Check(b.Collector.Complete && !b.Collector.Released && b.Creates == 4 && b.Comparisons == 4, "exact four-window collection finishes before observer release");
            Reject(() => b.Collector.Begin(4), "fifth window refused"); b.Collector.Dispose();
            var evidence = b.Collector.Evidence();
            Check((bool)evidence["complete"] && (bool)evidence["observerReleased"] && !(bool)evidence["performanceAccepted"] && !(bool)evidence["managedAllocationRateMeasured"] && !(bool)evidence["fullyInertDiagnostics"],
                "completed collection states observation limits rather than performance proof");
            Check(evidence["rows"].Select(row => (string)row["label"]).SequenceEqual(Chunk7MatchedPoolCollector.Labels), "actual collector serializes exact ordered labels");
            File.WriteAllText(args[0], evidence.ToString(Formatting.None));

            var ids = new[] { "a", "b", "c", "d" }; var pins = Enumerable.Range(0, 22).Select(_ => new object()).ToArray();
            var scope = new Chunk7MatchedPoolPins(ids, pins); scope.Require(ids, pins); Check(scope.Labels.Count == 22, "all resident/world/actor references are observed");
            for (int i = 0; i < 22; i++) { var changed = (object[])pins.Clone(); changed[i] = new object(); Reject(() => scope.Require(ids, changed), "native reference substitution refused at pin " + i); }
            Reject(() => scope.Require(new[] { "a", "b", "c", "other" }, pins), "same object cannot change actor identity");
            Reject(() => scope.Require(ids, pins.Take(21).ToArray()), "missing resident or actor pin refuses");
            Reject(() => new Chunk7MatchedPoolPins(new[] { "a", "b", "c", "c" }, pins), "duplicate actors cannot enter matched pool");
            var missing = (object[])pins.Clone(); missing[21] = null; Reject(() => new Chunk7MatchedPoolPins(ids, missing), "missing native view refuses entry");
            var aliased = (object[])pins.Clone(); aliased[7] = pins[9]; Reject(() => new Chunk7MatchedPoolPins(ids, aliased), "distinct native ownership roles cannot share a pin");
            scope.Dispose(); Reject(() => scope.Require(ids, pins), "released exact pins cannot authorize another window");
            Console.WriteLine("TOTAL PASS=" + checks + " FAIL=0; native boundaries substituted, no gameplay or performance qualification"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
