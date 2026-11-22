// Focused regression for the 2026-11-22 post-combat save and door incident.
// The production members marked __...__ are spliced in verbatim by
// scripts/Test-DeferredSaveWait.ps1: the deferred wait itself, its readiness
// result type and observers, the native loading-queue adapter (activation,
// tick retirement, Waiting) and the command hold. Only game objects and the
// readiness evaluation are fakes. No native gameplay claim is made here.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using KingmakerMountedCombat.Domain;

namespace Kingmaker.GameModes { enum GameModeType { None, Default, GlobalMap, Dialog, Pause, FullScreenUi, EscMode, Cutscene, Rest } }

namespace KingmakerMountedCombat.Integration
{
    using Kingmaker.GameModes;

    class Player { }
    class Game
    {
        internal static Game Instance = new Game();
        internal Player Player = new Player();
        internal GameModeType CurrentMode = GameModeType.Default;
        internal bool IsPaused;
    }
    class ILoadingScreen { }
    class LoadingProcess
    {
        internal static LoadingProcess Instance = new LoadingProcess();
        internal bool IsManualLoadingScreenActive;
        internal object CurrentProcess;
        internal bool IsLoadingInProcess
        {
            get { var result = CurrentProcess != null; PatchMethods.DeferredSaveLoadingPostfix(this, ref result); return result; }
        }
    }
    class UnitEntityData { }
    class UnitCommand { }
    class DoorCommand : UnitCommand { }
    class ReactionCommand : UnitCommand { }
    class SaveInfo { internal enum SaveType { Manual, Quick, Auto } internal SaveType Type = SaveType.Quick; internal string Name = "Quicksave1"; }
    class SaveScope { internal bool Began; internal SaveInfo Requested = new SaveInfo(); }
    class SaveWaitFault { internal bool Active; }
    class Logger
    {
        internal readonly List<string> Lines = new List<string>();
        internal bool Throws;
        internal void Info(string line) { if (Throws) throw new InvalidOperationException("logger failed"); Lines.Add(line); }
    }

    // Writer stands in for the native SaveRoutine wrapped by the scope; its
    // first step marks the scope begun exactly as the production scope does.
    static class Clock { internal static double Now; }

    class Writer : IEnumerator<object>
    {
        private readonly SaveScope scope;
        internal int Moves, Disposals;
        internal bool ThrowOnMove, ThrowOnDispose;
        internal Writer(SaveScope scope) { this.scope = scope; }
        public object Current => null;
        public bool MoveNext() { if (ThrowOnMove) throw new InvalidOperationException("native writer step failed"); scope.Began = true; return ++Moves < 2; }
        public void Reset() { throw new NotSupportedException(); }
        public void Dispose() { Disposals++; if (ThrowOnDispose) throw new InvalidOperationException("writer dispose failed"); }
    }

    class Persistence
    {
        internal int DeferredSaveCount, Released, Fences;
        internal readonly List<string> Notices = new List<string>();
        internal readonly Logger logger = new Logger();
        internal SaveWaitBlockerKind Blocker;
        internal object Subject;
        internal string Summary = "current actions";
        internal string Condition = "condition";
        internal bool ThrowReadiness, ThrowNotice;
        private long saveWaitSequence;

        // The production clock is a Stopwatch per request; this one is deterministic.
        private Func<double> SaveWaitClock() => () => Clock.Now;
        private bool AcquireWorldSaveFence(SaveScope scope) { Fences++; return true; }
        private void ReleaseUnstartedSaveFence(SaveScope scope) { Released++; }
        private void NotifySaveStatus(string message) { if (ThrowNotice) throw new InvalidOperationException("notice failed"); Notices.Add(message); }
        private SaveReadiness EvaluateSaveEffects()
        {
            if (ThrowReadiness) throw new InvalidOperationException("readiness probe failed");
            return Blocker == SaveWaitBlockerKind.None ? default(SaveReadiness) : new SaveReadiness { Kind = Blocker, Subject = Subject };
        }
        private string SummarizeReadiness(SaveReadiness readiness) => Summary;
        private string DescribeReadiness(SaveReadiness readiness) => "fake=" + readiness.Kind + "; at=" + Clock.Now.ToString("0.00");
        private string ConditionOfReadiness(SaveReadiness readiness) => Condition;

        internal DeferredSaveEnumerator<object> Wrap(out Writer writer)
        {
            var scope = new SaveScope();
            writer = new Writer(scope);
            return (DeferredSaveEnumerator<object>)DeferNativeSave(writer, null, scope);
        }

__READINESS_TYPE__
__DEFER_METHOD__
__OBSERVE_METHOD__
__END_METHOD__
__OUTCOME_PROPERTY__
    }

    static class MountedPatchController
    {
        internal static readonly List<string> NativeLoadingFailures = new List<string>();
        internal static void ReportFailedNativeLoading(Exception exception) { NativeLoadingFailures.Add(exception.Message); }
    }

    class BridgePersistence { internal bool Enabled = true, CombatRestorationPending; }
    class BridgeControls { internal bool OwnsUnsettledRelationshipShell(UnitCommand command) => false; }
    class BridgeTurn { internal bool MayStartNativePreparationDuringSave(UnitCommand command) => false; }
    static class PatchBridge
    {
        internal static BridgePersistence Persistence = new BridgePersistence();
        internal static BridgeControls NativeControls = new BridgeControls();
        internal static BridgeTurn UnifiedTurn = new BridgeTurn();
    }
    static class NativeSaveEffectBoundary { internal static bool MayStartDuringWait(UnitCommand command) => command is ReactionCommand; }

    static class PatchMethods
    {
        internal static bool ChargeExecutionPrefix(UnitCommand command) => true;
__PATCH_METHODS__
    }

    // The native LoadingProcess adapter. The queue record is the enumerator
    // itself here, so Enumerator() is the identity and retirement mirrors the
    // production condition without the reflective callback write.
    static class NativeDeferredSave
    {
        internal static int Screens;
        internal static bool ThrowScreen;
        internal static readonly List<string> Retired = new List<string>();
        private static readonly FieldInfo Current = typeof(LoadingProcess).GetField("CurrentProcess", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ShowScreen = typeof(NativeDeferredSave).GetMethod("Screen", BindingFlags.Static | BindingFlags.NonPublic);
        private static void Screen(ILoadingScreen screen) { if (ThrowScreen) throw new InvalidOperationException("loading screen start failed"); Screens++; }
        private static IEnumerator Enumerator(object queued) => queued as IEnumerator;
        private static bool RetireFailedSave(object record, DeferredSaveEnumerator<object> operation, Exception exception)
        {
            if (record == null || operation == null || (!operation.FailedBeforeSerialization && !operation.FailedAfterNativeCleanup) ||
                !ReferenceEquals(Enumerator(record), operation)) return false;
            Retired.Add(exception.Message);
            return true;
        }
__NATIVE_METHODS__
    }

    static class Program
    {
        static int pass, fail;

        static void Check(string name, Action test)
        {
            Game.Instance = new Game();
            LoadingProcess.Instance = new LoadingProcess();
            PatchBridge.Persistence = new BridgePersistence();
            NativeDeferredSave.Screens = 0;
            NativeDeferredSave.ThrowScreen = false;
            Clock.Now = 0;
            NativeDeferredSave.Retired.Clear();
            MountedPatchController.NativeLoadingFailures.Clear();
            try { test(); pass++; Console.WriteLine("PASS " + name); }
            catch (Exception exception) { fail++; Console.WriteLine("FAIL " + name + " :: " + exception.Message); }
        }

        static void True(bool condition, string message) { if (!condition) throw new Exception(message); }

        // Native StartLoadingProcessInternal: the (transpiled) screen call, then m_CurrentProcess.
        static void Activate(DeferredSaveEnumerator<object> operation)
        {
            NativeDeferredSave.StartScreen(LoadingProcess.Instance, new ILoadingScreen(), operation);
            LoadingProcess.Instance.CurrentProcess = operation;
        }

        // Native TickLoading's MoveNext through the production adapter; false ends the record.
        static bool Tick(DeferredSaveEnumerator<object> operation)
        {
            var running = NativeDeferredSave.MoveNext(operation, LoadingProcess.Instance);
            if (!running) LoadingProcess.Instance.CurrentProcess = null;
            return running;
        }

        static bool DoorAdmitted() => PatchMethods.SaveCommandStartPrefix(new DoorCommand());
        static string Lines(Persistence persistence) => string.Join(" | ", persistence.logger.Lines);

        static int Main()
        {
            Check("an idle world serializes at once: no wait, no notice, no hold, no wait lines", () =>
            {
                var persistence = new Persistence();
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                True(!operation.Waiting && operation.SerializationStarted && NativeDeferredSave.Screens == 1, "idle save did not serialize at once");
                True(persistence.DeferredSaveCount == 0 && persistence.Notices.Count == 0 && persistence.logger.Lines.Count == 0,
                    "idle save waited, notified or logged a wait: " + Lines(persistence));
                True(DoorAdmitted(), "a door command was held although no save waits");
                while (Tick(operation)) { }
                True(writer.Disposals == 1 && persistence.Released == 0 && persistence.LastSaveWaitOutcome == SaveWaitOutcome.ReadyToSerialize &&
                    persistence.logger.Lines.Count == 0, "idle save cleanup changed or a settled-at-once wait logged");
            });

            Check("a menu opened during the wait refuses before serialization and names the menu and the blocker", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Subject = new object(), Summary = "Leinna's AcidSplash projectile" };
                Game.Instance.IsPaused = true;
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                True(operation.Waiting && NativeDeferredSave.Screens == 0 && persistence.DeferredSaveCount == 1 && persistence.Notices.Count == 1,
                    "blocked save did not enter its wait");
                True(!Game.Instance.IsPaused && !LoadingProcess.Instance.IsLoadingInProcess, "waiting world was paused or reported as loading");
                True(!DoorAdmitted() && PatchMethods.SaveCommandStartPrefix(new ReactionCommand()),
                    "the hold did not hold an ordinary door command while still admitting a charged reaction");
                True(Tick(operation) && operation.Waiting, "the wait ended while its blocker remained");
                Game.Instance.CurrentMode = GameModeType.EscMode;
                True(!Tick(operation), "refused wait kept its native record running");
                True(NativeDeferredSave.Retired.Count == 1, "the refusal was not retired as a failed save");
                var message = NativeDeferredSave.Retired[0];
                True(message.Contains("the game menu opened") && message.Contains("Leinna's AcidSplash projectile") &&
                    message.IndexOf("native world changed", StringComparison.Ordinal) < 0, "refusal misnamed: " + message);
                True(writer.Moves == 0 && writer.Disposals == 1 && persistence.Released == 1 && persistence.LastSaveWaitOutcome == SaveWaitOutcome.ModeChanged,
                    "the refused request serialized, leaked its fence or recorded the wrong outcome");
                True(Game.Instance.IsPaused, "an initially paused save did not restore the pause");
                True(DoorAdmitted(), "a door command stayed held after the request retired");
                True(persistence.logger.Lines.Count == 2 && persistence.logger.Lines[0].StartsWith("Mounted save wait: request=1; name=Quick:Quicksave1;") &&
                    persistence.logger.Lines[0].Contains("blocker=Projectile") && persistence.logger.Lines[1].Contains("outcome=ModeChanged") &&
                    persistence.logger.Lines[1].Contains("mode=EscMode"), "wait lines: " + Lines(persistence));
            });

            Check("queued requests during the menu each retire with their own named refusal and nothing holds afterwards", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack" };
                var first = persistence.Wrap(out var w1); var second = persistence.Wrap(out var w2); var third = persistence.Wrap(out var w3);
                Activate(first);
                True(first.Waiting, "first request did not wait");
                Game.Instance.CurrentMode = GameModeType.EscMode;
                True(!Tick(first), "first refusal kept running");
                foreach (var queued in new[] { second, third })
                {
                    Activate(queued);
                    True(!queued.Waiting && !queued.SerializationStarted, "a queued request waited or serialized in the menu");
                    True(!Tick(queued), "a retired queued request restarted");
                }
                True(NativeDeferredSave.Retired.Count == 3 && NativeDeferredSave.Retired.All(m => m.Contains("the game menu opened") && m.Contains("Akasa's UnitAttack")),
                    "queued refusals: " + string.Join(" | ", NativeDeferredSave.Retired));
                True(persistence.Released == 3 && w1.Moves + w2.Moves + w3.Moves == 0 && w1.Disposals + w2.Disposals + w3.Disposals == 3,
                    "a queued request serialized or leaked its fence");
                foreach (var request in new[] { 1, 2, 3 })
                    True(persistence.logger.Lines.Count(l => l.StartsWith("Mounted save wait ended: request=" + request + ";") && l.Contains("outcome=ModeChanged")) == 1,
                        "request " + request + " did not report its own end: " + Lines(persistence));
                Game.Instance.CurrentMode = GameModeType.Default;
                True(DoorAdmitted() && LoadingProcess.Instance.CurrentProcess == null, "a door command stayed held after the last request retired");
                persistence.Blocker = SaveWaitBlockerKind.None;
                var next = persistence.Wrap(out var w4);
                Activate(next);
                True(next.SerializationStarted && !next.Waiting && persistence.DeferredSaveCount == 3, "a later settled save waited again");
            });

            Check("a loading screen during the wait is named", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Ability, Subject = new object(), Summary = "Fireball cast by Leinna" };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                LoadingProcess.Instance.IsManualLoadingScreenActive = true;
                True(!Tick(operation) && NativeDeferredSave.Retired.Count == 1, "loading screen did not refuse the wait");
                True(NativeDeferredSave.Retired[0].Contains("a loading screen started while it waited for Fireball cast by Leinna") &&
                    persistence.LastSaveWaitOutcome == SaveWaitOutcome.LoadingScreen && persistence.Released == 1, "loading refusal: " + NativeDeferredSave.Retired[0]);
            });

            Check("a replaced native world is named", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Subject = new object(), Summary = "a projectile" };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                Game.Instance.Player = new Player();
                True(!Tick(operation) && NativeDeferredSave.Retired.Count == 1, "foreign world did not refuse the wait");
                True(NativeDeferredSave.Retired[0].Contains("its native world was replaced") && writer.Moves == 0 &&
                    persistence.LastSaveWaitOutcome == SaveWaitOutcome.WorldReplaced && persistence.Released == 1, "world refusal: " + NativeDeferredSave.Retired[0]);
            });

            Check("each blocker change is reported once and a settled wait serializes with its pause restored", () =>
            {
                var a = new object(); var b = new object();
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = a, Summary = "Akasa's UnitAttack" };
                Game.Instance.IsPaused = true;
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                Tick(operation); Tick(operation);
                True(persistence.logger.Lines.Count == 1, "an unchanged blocker was reported again: " + Lines(persistence));
                persistence.Subject = b; persistence.Blocker = SaveWaitBlockerKind.Projectile; persistence.Summary = "Leinna's AcidSplash projectile";
                Tick(operation);
                True(persistence.logger.Lines.Count == 2 && persistence.logger.Lines[1].Contains("change=2"), "a changed blocker was not reported once");
                persistence.Blocker = SaveWaitBlockerKind.None;
                True(Tick(operation) && operation.SerializationStarted && NativeDeferredSave.Screens == 1 && Game.Instance.IsPaused,
                    "a settled wait did not serialize with the player's pause restored");
                while (Tick(operation)) { }
                True(persistence.logger.Lines.Count == 3 && persistence.logger.Lines[2].Contains("outcome=ReadyToSerialize") &&
                    persistence.logger.Lines[2].Contains("blockerChanges=2") && persistence.LastSaveWaitOutcome == SaveWaitOutcome.ReadyToSerialize &&
                    writer.Disposals == 1 && persistence.Released == 0, "settled wait end: " + Lines(persistence));
                True(DoorAdmitted(), "a door command stayed held after serialization finished");
            });

            Check("native cancellation while waiting reports Cancelled and releases the fence, pause and hold", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack" };
                Game.Instance.IsPaused = true;
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                operation.Dispose();
                LoadingProcess.Instance.CurrentProcess = null;
                True(!operation.Waiting && writer.Disposals == 1 && persistence.Released == 1 && Game.Instance.IsPaused &&
                    persistence.LastSaveWaitOutcome == SaveWaitOutcome.Cancelled && persistence.logger.Lines.Last().Contains("outcome=Cancelled"),
                    "cancellation: " + Lines(persistence));
                True(DoorAdmitted(), "a door command stayed held after cancellation");
            });

            Check("a readiness fault refuses before serialization and is reported as Faulted", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack" };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                persistence.ThrowReadiness = true;
                True(!Tick(operation) && NativeDeferredSave.Retired.Count == 1 && writer.Moves == 0 && persistence.Released == 1, "fault reached serialization");
                True(persistence.LastSaveWaitOutcome == SaveWaitOutcome.Faulted && persistence.logger.Lines.Last().Contains("readiness probe failed"),
                    "fault outcome: " + Lines(persistence));
            });

            Check("a failing logger changes no refusal, outcome, fence or pause", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Subject = new object(), Summary = "Leinna's AcidSplash projectile" };
                persistence.logger.Throws = true;
                Game.Instance.IsPaused = true;
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                Game.Instance.CurrentMode = GameModeType.Dialog;
                True(!Tick(operation) && NativeDeferredSave.Retired.Count == 1 && NativeDeferredSave.Retired[0].Contains("a dialog started"),
                    "logger failure changed the refusal");
                True(persistence.Released == 1 && Game.Instance.IsPaused && persistence.LastSaveWaitOutcome == SaveWaitOutcome.ModeChanged,
                    "logger failure skipped cleanup");
                Game.Instance.CurrentMode = GameModeType.Default;
                var settled = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object() };
                settled.logger.Throws = true;
                var next = settled.Wrap(out var w2);
                Activate(next);
                settled.Blocker = SaveWaitBlockerKind.None;
                True(Tick(next) && next.SerializationStarted, "logger failure prevented a settled save");
            });

            Check("the wait observer keeps no blocking object alive", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Summary = "a projectile" };
                var operation = persistence.Wrap(out var writer);
                var weak = ObserveTransient(persistence, operation);
                persistence.Subject = new object(); persistence.Blocker = SaveWaitBlockerKind.Command;
                Tick(operation);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                True(operation.Waiting && !weak.IsAlive, "the waiting save retained a blocking object it no longer reads");
                operation.Dispose();
            });

            // Review of 0b4b8fa4 (F2), the reviewer's three outcome probes.
            Check("a loading screen that fails to start after the wait settles is Faulted, never ReadyToSerialize", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack" };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                persistence.Blocker = SaveWaitBlockerKind.None;
                NativeDeferredSave.ThrowScreen = true;
                True(!Tick(operation) && !operation.SerializationStarted && operation.FailedBeforeSerialization && writer.Moves == 0,
                    "a failed screen start reached serialization");
                True(persistence.LastSaveWaitOutcome == SaveWaitOutcome.Faulted && persistence.logger.Lines.Count(l => l.Contains("ended")) == 1 &&
                    persistence.logger.Lines.Last().Contains("outcome=Faulted") && persistence.logger.Lines.Last().Contains("loading screen start failed") &&
                    !persistence.logger.Lines.Any(l => l.Contains("ReadyToSerialize")) && persistence.Released == 1, "screen fault: " + Lines(persistence));
            });

            Check("a first native writer step that throws after the wait settled leaves exactly one ReadyToSerialize report", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Subject = new object(), Summary = "a projectile" };
                var operation = persistence.Wrap(out var writer);
                writer.ThrowOnMove = true;
                Activate(operation);
                persistence.Blocker = SaveWaitBlockerKind.None;
                var threw = false;
                try { Tick(operation); } catch (InvalidOperationException) { threw = true; }
                True(threw && operation.SerializationStarted && writer.Moves == 0, "the writer fault was not native flow after serialization started");
                True(persistence.logger.Lines.Count(l => l.Contains("ended")) == 1 && persistence.logger.Lines.Last().Contains("outcome=ReadyToSerialize") &&
                    persistence.LastSaveWaitOutcome == SaveWaitOutcome.ReadyToSerialize, "writer fault: " + Lines(persistence));
            });

            Check("a begin-wait notification that throws is Faulted, not Cancelled", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack", ThrowNotice = true };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                True(NativeDeferredSave.Retired.Count == 1 && !operation.SerializationStarted && persistence.Released == 1, "notice fault reached serialization");
                True(persistence.LastSaveWaitOutcome == SaveWaitOutcome.Faulted && persistence.logger.Lines.Last().Contains("outcome=Faulted") &&
                    persistence.logger.Lines.Last().Contains("notice failed") && !persistence.logger.Lines.Any(l => l.Contains("Cancelled")),
                    "notice fault: " + Lines(persistence));
            });

            // Review of 0b4b8fa4 (F3), the reviewer's deterministic-clock probe, through the real timeout.
            Check("a request queued 20 s then waiting 30.01 s times out with active-wait times and its blocker named", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Projectile, Subject = new object(), Summary = "Leinna's AcidSplash projectile" };
                var operation = persistence.Wrap(out var writer);
                Clock.Now = 20;
                Activate(operation);
                Clock.Now = 30; True(Tick(operation), "the wait ended before its bound");
                Clock.Now = 50.01;
                True(!Tick(operation) && operation.TimedOut && NativeDeferredSave.Retired.Count == 1 && writer.Moves == 0 && persistence.Released == 1,
                    "the bounded wait did not time out before serialization");
                True(NativeDeferredSave.Retired[0] == "Save was not written: Leinna's AcidSplash projectile did not finish within 30 seconds.",
                    "timeout text: " + NativeDeferredSave.Retired[0]);
                True(persistence.logger.Lines[0].Contains("t=0.00s; queuedFor=20.00s") && persistence.logger.Lines.Last().Contains("outcome=TimedOut") &&
                    persistence.logger.Lines.Last().Contains("waited=30.01s; queuedFor=20.00s") && persistence.LastSaveWaitOutcome == SaveWaitOutcome.TimedOut,
                    "timing: " + Lines(persistence));
            });

            // Review of 0b4b8fa4 (F1), through the production observer.
            Check("the same blocking owner changing its refusing condition is reported and the end carries the latest snapshot", () =>
            {
                var subject = new object();
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.PairOwnership, Subject = subject, Summary = "Akasa's mounted pair to settle",
                    Condition = "refusal=native paired preparation is unsettled" };
                var operation = persistence.Wrap(out var writer);
                Activate(operation);
                Clock.Now = 0.1; Tick(operation);
                Clock.Now = 1; persistence.Condition = "refusal=a voluntary Mount/Dismount is in flight"; Tick(operation);
                Clock.Now = 2; Tick(operation);
                Clock.Now = 30.01; Tick(operation);
                True(persistence.logger.Lines.Count == 3 && persistence.logger.Lines[1].Contains("sameOwnerConditionChanged") &&
                    persistence.logger.Lines[1].Contains("refusal=a voluntary Mount/Dismount is in flight"), "condition change: " + Lines(persistence));
                var end = persistence.logger.Lines[2];
                True(end.Contains("outcome=TimedOut") && end.Contains("condition=[refusal=a voluntary Mount/Dismount is in flight]") &&
                    end.Contains("detail=[fake=PairOwnership; at=30.01]") && end.Contains("snapshotAt=30.01s") &&
                    end.IndexOf("native paired preparation", StringComparison.Ordinal) < 0, "stale end: " + end);
            });

            // Pro review of 079f4e9e, note 1: the reviewer's probe, a waiting operation disposed while its writer's cleanup throws.
            Check("a waiting operation disposed while its writer cleanup throws is Faulted, not Cancelled", () =>
            {
                var persistence = new Persistence { Blocker = SaveWaitBlockerKind.Command, Subject = new object(), Summary = "Akasa's UnitAttack" };
                var operation = persistence.Wrap(out var writer);
                writer.ThrowOnDispose = true;
                Activate(operation);
                var threw = false;
                try { operation.Dispose(); } catch (InvalidOperationException) { threw = true; }
                True(threw && persistence.Released == 1 && persistence.LastSaveWaitOutcome == SaveWaitOutcome.Faulted &&
                    persistence.logger.Lines.Last().Contains("outcome=Faulted") && persistence.logger.Lines.Last().Contains("writer dispose failed"),
                    "cleanup fault: " + Lines(persistence));
            });

            Console.WriteLine("DEFERRED SAVE WAIT ACTUAL METHODS PASS=" + pass + " FAIL=" + fail +
                "; game objects and the readiness evaluation are fakes; no native gameplay claim");
            return fail == 0 ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference ObserveTransient(Persistence persistence, DeferredSaveEnumerator<object> operation)
        {
            var subject = new object();
            persistence.Subject = subject;
            Activate(operation);
            Tick(operation);
            persistence.Subject = null;
            return new WeakReference(subject);
        }
    }
}
