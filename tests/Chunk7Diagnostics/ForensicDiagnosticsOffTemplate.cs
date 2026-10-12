// The adapter inserts actual production methods. Native cleanup/attack and managed
// stack boundaries are spies; selection, reference guards and cleanup ordering are real.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace System.Diagnostics
{
    internal sealed class StackTrace
    {
        internal static int Created;
        internal static bool Fail;
        public StackTrace(int skip, bool fileInfo)
        { Created++; if (Fail) throw new InvalidOperationException("stack failure"); }
        public StackFrame[] GetFrames() => new[] { new StackFrame() };
        internal static void Reset() { Created = 0; Fail = false; }
    }
    internal sealed class StackFrame
    {
        public MethodBase GetMethod() => typeof(NativeBoundary).GetMethod("ObservedNativeCaller");
    }
}
internal static class NativeBoundary { public static void ObservedNativeCaller() { } }
internal sealed class RuntimeRequest
{
    internal string Scenario = "persistence-p04-load", PersistenceCase = "combat-mount-rt";
    __CHARGE_CASE__
}
internal sealed class Relationship { }
internal sealed class MountedCombatController { internal MountedPairTurnParticipant TurnParticipant; }
internal sealed class Logger
{
    internal int Calls;
    internal bool Fail;
    internal string Last;
    internal Action DuringLog;
    internal void Info(string value)
    { Calls++; Last = value; DuringLog?.Invoke(); if (Fail) throw new InvalidOperationException("logger failure"); }
}
internal sealed class Activation { internal string Identity = "exact-old-activation"; }
internal sealed class MountedPairTurnParticipant
{
    internal Relationship Relationship = new Relationship();
    internal bool IsDisposed;
    internal long PersistenceWorldDiscardCount;
    internal string LastPersistenceWorldDiscardObservation = "none";
    internal readonly Logger logger = new Logger();
    internal Activation activation = new Activation();
    internal readonly Dictionary<int, object> nativePreparationCommands = new Dictionary<int, object> { { 1, new object() } };
    internal object armedRider = new object(), armedMount = new object(), activationSession = new object();
    internal object preparingConfusionActor = new object(), preparedRiderTurn = new object(), resumingContext = new object(), nativeConditionForfeitContext = new object();
    internal object pendingSplitMount = new object();
    internal int pendingSplitRound = 7, splitReleaseRound = 8;
    internal double pairedRenewalNotBefore = 99;
    internal int CleanupCalls;
    internal bool FailCleanup;
    internal readonly double[] NativeDebt = { 6, 3, 1, 9 };
    private void DisposePartnerContext()
    { CleanupCalls++; if (FailCleanup) throw new InvalidOperationException("cleanup failure"); }
    internal bool Cleared => CleanupCalls == 1 && nativePreparationCommands.Count == 0 &&
        activation == null && armedRider == null && armedMount == null && activationSession == null &&
        preparingConfusionActor == null && preparedRiderTurn == null && resumingContext == null &&
        nativeConditionForfeitContext == null && pendingSplitMount == null && pendingSplitRound == -1 &&
        splitReleaseRound == -1 && pairedRenewalNotBefore == 0;
    __DISCARD_METHODS__
}
internal sealed class RuntimePersistenceScenario
{
    internal bool disposed, Completed;
    internal RuntimeRequest request;
    internal MountedCombatController combat;
    internal Relationship relationship;
    __VALIDATION_CASE__
    __ENGINE_METHODS__
}
internal sealed class RuntimeAutomationHost
{
    internal static RuntimeAutomationHost active;
    internal bool disposed, completed, completionStarted;
    internal MountedCombatController combat;
    internal Relationship relationship;
    internal RuntimeRequest request;
    internal RuntimePersistenceScenario persistenceEngine;
    __HOST_METHODS__
}
internal sealed class NativeAnimation { internal bool IsFinished => true; }
internal class UnitAttack
{
    internal int NativeEnded, NativeReads;
    internal bool Raised, FailNative;
    protected string Result { get { NativeReads++; return "Success"; } }
    protected bool IsActed { get { NativeReads++; return true; } }
    protected float TimeSinceStart { get { NativeReads++; return 2; } }
    protected object LastAttackRule { get { NativeReads++; return this; } }
    protected NativeAnimation Animation { get { NativeReads++; return new NativeAnimation(); } }
    protected virtual void OnEnded(bool raiseEvent = true)
    { NativeEnded++; Raised = raiseEvent; if (FailNative) throw new InvalidOperationException("native failure"); }
}
internal class MountedPairSingleAttack : UnitAttack
{
    __ATTACK_METHODS__
    internal void End(bool raiseEvent) => OnEnded(raiseEvent);
}
internal static class Program
{
    private static int passed, failed;
    private static void Check(string name, bool ok)
    { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (ok) passed++; else failed++; }
    private static string Error(Action action)
    { try { action(); return null; } catch (Exception error) { return error.Message; } }
    private static MountedPairTurnParticipant Fresh()
    { StackTrace.Reset(); RuntimeAutomationHost.active = null; return new MountedPairTurnParticipant(); }
    private static RuntimeAutomationHost Arm(MountedPairTurnParticipant participant, bool started = true)
    {
        var host = new RuntimeAutomationHost { combat = new MountedCombatController { TurnParticipant = participant },
            relationship = participant.Relationship, request = new RuntimeRequest() };
        if (started) host.persistenceEngine = new RuntimePersistenceScenario {
            request = host.request, combat = host.combat, relationship = host.relationship };
        RuntimeAutomationHost.active = host; return host;
    }
    private static void ExpectOff(string name, Action<RuntimeAutomationHost, MountedPairTurnParticipant> setup, bool started = true)
    {
        var pair = Fresh(); var host = Arm(pair, started); setup(host, pair);
        var error = Error(pair.DiscardParticipation);
        Check(name + " skips stack and forensic log", error == null && StackTrace.Created == 0 && pair.logger.Calls == 0);
        Check(name + " records uncaptured count and preserves real cleanup", pair.PersistenceWorldDiscardCount == 1 &&
            pair.LastPersistenceWorldDiscardObservation == "not-captured" && pair.Cleared && pair.NativeDebt.SequenceEqual(new double[] { 6, 3, 1, 9 }));
    }
    private static void ExpectOn(string name, string scenario, string checkpoint, bool started)
    {
        var pair = Fresh(); var host = Arm(pair, started); host.request.Scenario = scenario; host.request.PersistenceCase = checkpoint;
        var error = Error(pair.DiscardParticipation);
        Check(name + " captures exactly once before cleanup", error == null && StackTrace.Created == 1 && pair.logger.Calls == 1 &&
            pair.LastPersistenceWorldDiscardObservation.StartsWith("discard;count=1;activation=exact-old-activation;callers=", StringComparison.Ordinal) && pair.Cleared);
    }
    public static int Main()
    {
        ExpectOff("no host", (host, subject) => RuntimeAutomationHost.active = null);
        ExpectOff("host disposed", (host, subject) => host.disposed = true);
        ExpectOff("host completed", (host, subject) => host.completed = true);
        ExpectOff("engine disposed", (host, subject) => host.persistenceEngine.disposed = true);
        ExpectOff("engine completed", (host, subject) => host.persistenceEngine.Completed = true);
        ExpectOff("participant disposed", (host, subject) => subject.IsDisposed = true);
        ExpectOff("different participant same relationship", (host, subject) => host.combat.TurnParticipant = new MountedPairTurnParticipant { Relationship = subject.Relationship });
        ExpectOff("host different relationship", (host, subject) => host.relationship = new Relationship());
        ExpectOff("engine different participant", (host, subject) => host.persistenceEngine.combat = new MountedCombatController { TurnParticipant = new MountedPairTurnParticipant() });
        ExpectOff("engine different relationship", (host, subject) => host.persistenceEngine.relationship = new Relationship());
        ExpectOff("unrelated P01 consumer", (host, subject) => host.request.Scenario = "persistence-p01-load");
        ExpectOff("quiet world measurement consumer", (host, subject) => host.request.Scenario = "chunk7-world-exploration", false);
        ExpectOff("new world persistence consumer", (host, subject) => host.request.Scenario = "chunk7-persistence-load", false);
        ExpectOff("unrelated validation", (host, subject) => { host.request.Scenario = "persistence-p06-load"; host.request.PersistenceCase = "schema1"; }, false);
        ExpectOff("unrelated P07", (host, subject) => { host.request.Scenario = "persistence-p07-save"; host.request.PersistenceCase = "death"; }, false);
        ExpectOff("unconstructed completion", (host, subject) => host.completionStarted = true, false);
        ExpectOff("different active host", (host, subject) => Arm(new MountedPairTurnParticipant()));
        foreach (var scenario in new[] { "persistence-p02-save", "persistence-p02-load", "persistence-p03-save", "persistence-p03-load", "persistence-p04-save", "persistence-p04-load" })
        { ExpectOn(scenario + " before load", scenario, "partial-movement", false); ExpectOn(scenario + " live observer", scenario, "partial-movement", true); }
        ExpectOn("invalid combat pre-load", "persistence-p06-load", "combat-missing", false);
        ExpectOn("invalid combat retry", "persistence-p06-load", "combat-ai", true);
        ExpectOn("selected charge lifecycle pre-load", "persistence-p07-save", "mounted-charge-area", false);
        ExpectOn("selected charge lifecycle", "persistence-p07-save", "mounted-charge-area", true);

        var pair = Fresh(); var owner = Arm(pair); owner.completionStarted = true;
        Check("retained live engine cleanup remains observable", Error(pair.DiscardParticipation) == null && StackTrace.Created == 1 && pair.Cleared);
        pair = Fresh(); owner = Arm(pair); pair.DiscardParticipation(); owner.persistenceEngine.disposed = true; pair.DiscardParticipation();
        Check("inactive discard never reuses previous captured text", pair.PersistenceWorldDiscardCount == 2 && StackTrace.Created == 1 && pair.logger.Calls == 1 && pair.LastPersistenceWorldDiscardObservation == "not-captured");
        pair = Fresh(); Arm(pair); pair.logger.Fail = true;
        Check("forensic log failure propagates after real cleanup", Error(pair.DiscardParticipation) == "logger failure" && pair.Cleared && pair.PersistenceWorldDiscardCount == 1);
        pair = Fresh(); Arm(pair); StackTrace.Fail = true;
        Check("stack failure propagates after real cleanup without stale trace", Error(pair.DiscardParticipation) == "stack failure" && pair.Cleared && pair.PersistenceWorldDiscardCount == 1 && pair.LastPersistenceWorldDiscardObservation == "not-captured");
        pair = Fresh(); pair.FailCleanup = true;
        Check("native cleanup failure propagates without invented settlement", Error(pair.DiscardParticipation) == "cleanup failure" && pair.CleanupCalls == 1 && pair.nativePreparationCommands.Count == 1 && pair.activation != null && pair.PersistenceWorldDiscardCount == 1);
        pair = Fresh(); owner = Arm(pair); pair.logger.DuringLog = () => owner.persistenceEngine.disposed = true;
        Check("consumer disposal during synchronous log retains captured row and cleanup", Error(pair.DiscardParticipation) == null && pair.Cleared && StackTrace.Created == 1);
        pair.DiscardParticipation();
        Check("subsequent disposal callback cannot capture", StackTrace.Created == 1 && pair.logger.Calls == 1 && pair.LastPersistenceWorldDiscardObservation == "not-captured");
        pair = Fresh(); Arm(pair, false); pair.DiscardParticipation(); var initial = pair.LastPersistenceWorldDiscardObservation;
        owner = RuntimeAutomationHost.active; owner.persistenceEngine = new RuntimePersistenceScenario { request = owner.request, combat = owner.combat, relationship = owner.relationship };
        Check("constructed observer sees retained initial cold-load evidence", initial == pair.LastPersistenceWorldDiscardObservation && initial.StartsWith("discard;count=1;activation=exact-old-activation;callers=", StringComparison.Ordinal));

        StackTrace.Reset(); var attack = new MountedPairSingleAttack(); attack.End(true);
        Check("native attack terminal reaches base once without forensic reads", attack.NativeEnded == 1 && attack.Raised && attack.NativeReads == 0 && StackTrace.Created == 0);
        StackTrace.Reset(); attack = new MountedPairSingleAttack(); attack.End(false);
        Check("native attack terminal preserves raiseEvent false", attack.NativeEnded == 1 && !attack.Raised && attack.NativeReads == 0 && StackTrace.Created == 0);
        StackTrace.Reset(); StackTrace.Fail = true; attack = new MountedPairSingleAttack();
        Check("unused stack cannot prevent native attack completion", Error(() => attack.End(true)) == null && attack.NativeEnded == 1 && StackTrace.Created == 0);
        StackTrace.Reset(); attack = new MountedPairSingleAttack { FailNative = true };
        Check("native completion error still propagates once", Error(() => attack.End(false)) == "native failure" && attack.NativeEnded == 1 && !attack.Raised && StackTrace.Created == 0);
        Console.WriteLine("FORENSIC DIAGNOSTICS OFF PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
