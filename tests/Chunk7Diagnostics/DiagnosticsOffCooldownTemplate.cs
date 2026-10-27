using System;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Diagnostics;

internal static class NativeReads
{
    internal static int Count;
    internal static void Read() { Count++; }
}

namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitEntityData
    {
        internal CombatState State = new CombatState();
        internal bool Combat = true;
        internal CombatState CombatState { get { NativeReads.Read(); return State; } }
        internal bool IsInCombat { get { NativeReads.Read(); return Combat; } }
    }
    internal sealed class CombatState
    {
        internal Cooldowns Cooldowns = new Cooldowns();
        internal bool IsPrepared = true;
        internal Cooldowns Cooldown { get { NativeReads.Read(); return Cooldowns; } }
        internal bool Prepared { get { NativeReads.Read(); return IsPrepared; } }
    }
    internal sealed class Cooldowns
    {
        internal float Value = 4f;
        internal float Initiative { get { NativeReads.Read(); return Value; } }
    }
}

namespace Kingmaker
{
    internal sealed class Game
    {
        internal static Game Current;
        internal static Game Instance { get { NativeReads.Read(); return Current; } }
        internal TimeController Time = new TimeController();
        internal State StateValue = new State();
        internal TimeController TimeController { get { NativeReads.Read(); return Time; } }
        internal State State { get { NativeReads.Read(); return StateValue; } }
    }
    internal sealed class TimeController
    {
        internal float Delta = .25f;
        internal float GameDeltaTime { get { NativeReads.Read(); return Delta; } }
    }
    internal sealed class State
    {
        internal AwakeList Units = new AwakeList();
        internal AwakeList AwakeUnits { get { NativeReads.Read(); return Units; } }
    }
    internal sealed class AwakeList
    {
        internal bool Result = true;
        internal bool Contains(UnitEntityData unit) { NativeReads.Read(); return Result; }
    }
}

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class RuntimeAutomationHost
    {
        internal static RuntimeAutomationHost active;
        internal bool completed;
        internal bool disposed;
        internal RuntimeCombatScenarioEngine combatEngine;
        __HOST_METHODS__
    }
    internal enum CombatEngineStep { Other, AwaitCombatFrame }
    internal sealed class RuntimeCombatScenarioEngine
    {
        internal bool completed;
        internal bool disposed;
        internal CombatEngineStep step = CombatEngineStep.AwaitCombatFrame;
        internal UnitEntityData AttackActor;
        internal readonly TickObservation initiativeTickObservation = new TickObservation();
        __ENGINE_METHODS__
    }
    internal sealed class TickObservation
    {
        internal int Count;
        internal float Before, After, Delta;
        internal bool Prepared, InCombat, Awake;
        internal void Observe(float before, float after, float delta, bool prepared, bool inCombat, bool awake)
        {
            Count++; Before = before; After = after; Delta = delta;
            Prepared = prepared; InCombat = inCombat; Awake = awake;
        }
    }
}

internal sealed class PersistenceStub
{
    internal bool Pending;
    internal int GuardReads;
    internal bool CombatRestorationPending { get { GuardReads++; return Pending; } }
}
internal static class PatchBridge
{
    internal static PersistenceStub Persistence;
}
internal static class PatchMethods
{
    __PATCH_METHODS__
}

internal static class DiagnosticsOffCooldownTests
{
    private static int passed, failed;
    private static UnitEntityData actor;
    private static RuntimeCombatScenarioEngine consumer;
    private static RuntimeAutomationHost host;
    private static Kingmaker.Game game;
    private static PersistenceStub persistence;

    private static void Reset()
    {
        actor = new UnitEntityData();
        consumer = new RuntimeCombatScenarioEngine { AttackActor = actor };
        host = new RuntimeAutomationHost { combatEngine = consumer };
        RuntimeAutomationHost.active = host;
        game = new Kingmaker.Game(); Kingmaker.Game.Current = game;
        persistence = new PersistenceStub(); PatchBridge.Persistence = persistence;
        NativeReads.Count = 0;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Test(string name, Action action)
    {
        Reset();
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static bool Invoke(UnitEntityData prefixActor = null, UnitEntityData postfixActor = null, Action between = null)
    {
        var runNative = PatchMethods.CombatCooldownPrefix(prefixActor ?? actor, out var state);
        between?.Invoke();
        PatchMethods.CombatCooldownPostfix(postfixActor ?? prefixActor ?? actor, state);
        return runNative;
    }
    private static void Inert()
    {
        Require(Invoke(), "native cooldown admission changed");
        Require(NativeReads.Count == 0, "inactive consumer read " + NativeReads.Count + " native diagnostic values");
        Require(consumer.initiativeTickObservation.Count == 0, "inactive consumer received a sample");
        Require(persistence.GuardReads == 1, "persistence admission was skipped");
    }
    private static void DropDuringCallback(Action change, UnitEntityData postfixActor = null)
    {
        var next = new RuntimeCombatScenarioEngine { AttackActor = actor };
        var beforePostfix = -1;
        var allowed = Invoke(postfixActor: postfixActor, between: () => {
            change(); beforePostfix = NativeReads.Count;
        });
        Require(allowed, "native cooldown admission changed");
        Require(NativeReads.Count == beforePostfix, "stale callback sampled native postfix state");
        Require(consumer.initiativeTickObservation.Count == 0, "retired consumer received sample");
        if (RuntimeAutomationHost.active?.combatEngine != null)
            Require(RuntimeAutomationHost.active.combatEngine.initiativeTickObservation.Count == 0, "replacement consumer inherited prefix sample");
        Require(persistence.GuardReads == 1, "persistence admission was skipped");
    }

    public static int Main()
    {
        Test("no request host leaves native diagnostic state unread", () => { RuntimeAutomationHost.active = null; Inert(); });
        Test("host without exact combat consumer leaves state unread", () => { host.combatEngine = null; Inert(); });
        Test("unrelated actor leaves state unread", () => { consumer.AttackActor = new UnitEntityData(); Inert(); });
        Test("non-observing row phase leaves state unread", () => { consumer.step = CombatEngineStep.Other; Inert(); });
        Test("completed consumer leaves state unread", () => { consumer.completed = true; Inert(); });
        Test("disposed consumer leaves state unread", () => { consumer.disposed = true; Inert(); });
        Test("completed host leaves state unread", () => { host.completed = true; Inert(); });
        Test("disposed host leaves state unread", () => { host.disposed = true; Inert(); });
        Test("exact live actor retains prefix and postfix raw values", () => {
            Require(Invoke(between: () => actor.State.Cooldowns.Value = 2.25f), "native admission changed");
            var row = consumer.initiativeTickObservation;
            Require(row.Count == 1 && row.Before == 4f && row.After == 2.25f && row.Delta == .25f && row.Prepared && row.InCombat && row.Awake, "native sample changed");
            Require(actor.State.Cooldowns.Value == 2.25f && persistence.GuardReads == 1, "probe mutated native resources or omitted guard");
        });
        Test("exact live actor retains false native state", () => {
            actor.State.IsPrepared = false; actor.Combat = false; game.StateValue.Units.Result = false;
            Invoke(); var row = consumer.initiativeTickObservation;
            Require(row.Count == 1 && !row.Prepared && !row.InCombat && !row.Awake && row.Before == 4f && row.After == 4f, "false native state changed");
        });
        Test("missing game preserves bounded raw fallback", () => {
            Kingmaker.Game.Current = null; Invoke(); var row = consumer.initiativeTickObservation;
            Require(row.Count == 1 && row.Delta == 0f && !row.Awake && row.Before == 4f, "missing game fallback changed");
        });
        Test("missing combat state preserves NaN sample", () => {
            actor.State = null; Invoke(); var row = consumer.initiativeTickObservation;
            Require(row.Count == 1 && float.IsNaN(row.Before) && float.IsNaN(row.After) && !row.Prepared, "missing state fallback changed");
        });
        Test("null actor does not start a diagnostic sample", () => {
            Require(PatchMethods.CombatCooldownPrefix(null, out var state), "native null admission changed");
            PatchMethods.CombatCooldownPostfix(null, state);
            Require(NativeReads.Count == 0 && consumer.initiativeTickObservation.Count == 0 && persistence.GuardReads == 1, "null actor caused diagnostics or skipped persistence");
        });
        Test("off consumer never bypasses restoration refusal", () => {
            RuntimeAutomationHost.active = null; persistence.Pending = true;
            Require(!Invoke() && persistence.GuardReads == 1, "restoration refusal bypassed");
            Require(NativeReads.Count == 0 && actor.State.Cooldowns.Value == 4f, "off refusal sampled or mutated native budget");
        });
        Test("live consumer never bypasses restoration refusal", () => {
            persistence.Pending = true; Require(!Invoke() && persistence.GuardReads == 1, "restoration refusal bypassed");
            var row = consumer.initiativeTickObservation;
            Require(row.Count == 1 && row.Before == 4f && row.After == 4f && actor.State.Cooldowns.Value == 4f, "refused callback raw state changed");
        });
        Test("absent persistence preserves native admission", () => { PatchBridge.Persistence = null; Require(Invoke(), "null persistence changed native admission"); });
        Test("removed consumer cannot receive postfix", () => DropDuringCallback(() => host.combatEngine = null));
        Test("replaced consumer cannot inherit prefix", () => DropDuringCallback(() => host.combatEngine = new RuntimeCombatScenarioEngine { AttackActor = actor }));
        Test("disposed consumer during callback cannot receive postfix", () => DropDuringCallback(() => consumer.disposed = true));
        Test("completed consumer during callback cannot receive postfix", () => DropDuringCallback(() => consumer.completed = true));
        Test("consumer phase changes before postfix", () => DropDuringCallback(() => consumer.step = CombatEngineStep.Other));
        Test("consumer target changes before postfix", () => DropDuringCallback(() => consumer.AttackActor = new UnitEntityData()));
        Test("disposed host during callback cannot receive postfix", () => DropDuringCallback(() => host.disposed = true));
        Test("completed host during callback cannot receive postfix", () => DropDuringCallback(() => host.completed = true));
        Test("removed host during callback cannot receive postfix", () => DropDuringCallback(() => RuntimeAutomationHost.active = null));
        Test("new host and consumer cannot inherit prefix", () => DropDuringCallback(() => RuntimeAutomationHost.active = new RuntimeAutomationHost { combatEngine = new RuntimeCombatScenarioEngine { AttackActor = actor } }));
        Test("consumer starting after inactive prefix waits for next native callback", () => {
            RuntimeAutomationHost.active = null; Invoke(between: () => RuntimeAutomationHost.active = host);
            Require(NativeReads.Count == 0 && consumer.initiativeTickObservation.Count == 0, "new consumer inherited inactive prefix");
            Invoke(); Require(consumer.initiativeTickObservation.Count == 1, "next complete callback not observed");
        });
        Test("changed native callback actor cannot inherit prefix even if now selected", () => {
            var replacement = new UnitEntityData(); DropDuringCallback(() => consumer.AttackActor = replacement, replacement);
        });
        Test("retired callback does not poison next consumer sample", () => {
            DropDuringCallback(() => host.combatEngine = new RuntimeCombatScenarioEngine { AttackActor = actor });
            consumer = host.combatEngine; Invoke();
            Require(consumer.initiativeTickObservation.Count == 1 && consumer.initiativeTickObservation.Before == 4f, "new valid callback not observed exactly once");
        });
        Console.WriteLine("DIAGNOSTICS OFF ACTUAL METHODS PASS=" + passed + " FAIL=" + failed + "; external native boundaries stubbed; no native/performance proof.");
        return failed == 0 ? 0 : 1;
    }
}
