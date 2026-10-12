using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

enum OwnedWorkerTeardownVerdict { Settled, Refused }
class Persistence { public OwnedWorkerTeardownVerdict Verdict; public OwnedWorkerTeardownVerdict DrainForTeardown(int count) { return Verdict; } }
class Clock { public TimeSpan Elapsed = TimeSpan.FromSeconds(60); }
class View { public Unit Owner; }
class Unit { public bool IsInState = true; public View View; }
class SelectionManager
{
    public static SelectionManager Instance;
    public Unit[] SelectedUnits = new Unit[0];
    public void MultiSelect(IEnumerable<View> values, bool extend) { Counters.Selection++; SelectedUnits = values.Select(value => value.Owner).ToArray(); }
}
class Game
{
    public static Game Instance;
    private bool paused;
    public bool IsPaused { get { return paused; } set { Counters.Pause++; paused = value; } }
}
class Lease : IDisposable
{
    public bool Throw;
    public void Dispose() { Counters.Lease++; if (Throw) throw new InvalidOperationException("lease refused"); }
}
class Field
{
    public bool Owned = true, Ready = true, Throw;
    public bool TryDrain() { Counters.Field++; if (Throw) throw new InvalidOperationException("foreign selector"); if (Ready) Owned = false; return Ready; }
}
class Request { }
class Selection { public string SourceArtifactSha256 = "source", SourceArchiveSha256 = "archive"; }
static class Counters
{
    public static int Field, Selection, Pause, Lease, Snapshots;
    public static List<string> Artifacts = new List<string>();
    public static void Reset() { Field = Selection = Pause = Lease = Snapshots = 0; Artifacts.Clear(); }
}
static class RuntimeLoaderInventory
{
    public static void WriteArtifact(Request r, string name, string kind, string status, JObject body, List<string> errors) { Counters.Artifacts.Add(name + ":" + status); }
}
public class ActualFieldCleanup
{
    private const string EvidenceFileName = "final", EvidenceKind = "field", PendingFileName = "pending", PendingKind = "pending";
    private bool completed, settled, captured = true, pending, busy, refuseGuard, refuseOwner;
    private bool pause = true;
    private double cleanupStart;
    private Persistence persistence = new Persistence();
    private Clock clock = new Clock();
    private Request request = new Request();
    private Selection selection = new Selection();
    private Field field = new Field();
    private Unit[] originalSelection = new Unit[0];
    private IDisposable ummLease = new Lease();
    private string sourcePath = "source", archivePath = "archive";
    private JObject evidence = new JObject();
    private List<string> errors = new List<string>();
    private ActualFieldCleanup() { Counters.Reset(); Game.Instance = new Game(); SelectionManager.Instance = new SelectionManager(); }
    private bool Busy() { return busy; }
    private void AssertInput() { if (refuseGuard) throw new InvalidOperationException("world or authority changed"); }
    private void RequireNoOwners() { if (refuseOwner) throw new InvalidOperationException("owner remains"); }
    private void AssertUnchangedProgression() { }
    private JObject AuthoritySnapshot() { Counters.Snapshots++; return new JObject(); }
    private static string Hash(string path) { return path; }
    private void Pass(string row) { }
    private void Fail(string row, string error) { if (!errors.Contains(error)) errors.Add(error); }
    private static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
/*POLL*/
/*RETAIN*/
    private static int passes, failures;
    private static void Check(string name, Func<ActualFieldCleanup, bool> test)
    {
        var value = new ActualFieldCleanup(); bool ok = false; Exception caught = null;
        try { ok = test(value); } catch (Exception error) { caught = error; }
        if (ok) { passes++; Console.WriteLine("PASS " + name); }
        else { failures++; Console.WriteLine("FAIL " + name + " " + caught); }
    }
    public static int Verify()
    {
        Check("same owned world settles normally", a => { a.PollCleanup(); return a.completed && a.settled && Counters.Artifacts.SequenceEqual(new[] { "final:PASS" }); });
        Check("thrown guard retains custody and emits bounded pending FAIL before native mutation", a => { a.refuseGuard = true; a.PollCleanup(); return !a.completed && !a.settled && a.errors.Count > 0 && Counters.Field == 0 && Counters.Selection == 0 && Counters.Pause == 0 && Counters.Lease == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Check("early thrown guard records error without premature terminal or pending artifact", a => { a.clock.Elapsed = TimeSpan.FromSeconds(2); a.refuseGuard = true; a.PollCleanup(); return !a.completed && !a.settled && a.errors.Count > 0 && Counters.Artifacts.Count == 0; });
        Check("repeated guard refusal writes only one pending artifact", a => { a.refuseGuard = true; a.PollCleanup(); a.PollCleanup(); return !a.completed && !a.settled && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }) && Counters.Field == 0; });
        Check("restored exact guard permits retry while preserving final FAIL", a => { a.refuseGuard = true; a.PollCleanup(); a.refuseGuard = false; a.PollCleanup(); return a.completed && a.settled && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL", "final:FAIL" }); });
        Check("native worker retains custody before guard or UI", a => { a.busy = true; a.refuseGuard = true; a.PollCleanup(); return !a.completed && Counters.Field == 0 && Counters.Selection == 0 && Counters.Lease == 0 && Counters.Pause == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Check("refused teardown worker cannot release UI", a => { a.persistence.Verdict = OwnedWorkerTeardownVerdict.Refused; a.PollCleanup(); return !a.completed && Counters.Field == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Check("owned async selector remains pending", a => { a.field.Ready = false; a.PollCleanup(); return !a.completed && a.field.Owned && Counters.Selection == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Check("foreign selector throw records pending and preserves ownership", a => { a.field.Throw = true; a.PollCleanup(); return !a.completed && a.field.Owned && Counters.Selection == 0 && Counters.Lease == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Check("failed lease disposal retains reference for a later safe retry", a => { var lease = (Lease)a.ummLease; lease.Throw = true; a.PollCleanup(); if (a.completed || !ReferenceEquals(a.ummLease, lease)) return false; lease.Throw = false; a.PollCleanup(); return a.completed && Counters.Lease == 2 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL", "final:FAIL" }); });
        Check("uncaptured admission failure settles without acquiring cleanup input", a => { a.captured = false; a.refuseGuard = true; a.PollCleanup(); return a.completed && a.settled && Counters.Field == 0 && Counters.Selection == 0 && Counters.Lease == 0 && Counters.Pause == 0 && Counters.Artifacts.SequenceEqual(new[] { "final:FAIL" }); });
        Check("retained owner cannot restore selection or release lease", a => { a.refuseOwner = true; a.PollCleanup(); return !a.completed && Counters.Selection == 0 && Counters.Lease == 0 && Counters.Pause == 0 && Counters.Artifacts.SequenceEqual(new[] { "pending:FAIL" }); });
        Console.WriteLine("ACTUAL FIELD CLEANUP PASS " + passes + " FAIL " + failures); return failures;
    }
}
