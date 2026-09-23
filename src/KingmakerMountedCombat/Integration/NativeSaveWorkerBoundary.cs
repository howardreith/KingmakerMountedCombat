using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence;

namespace KingmakerMountedCombat.Integration
{
    internal static class NativeSaveWorkerBoundary
    {
        private static readonly Type Iterator = typeof(SaveManager).GetNestedType("<SaveRoutine>d__46", BindingFlags.NonPublic);
        private static readonly FieldInfo SaveTask = NativeCombatActorPersistence.Field(
            Iterator, "<saveTask>5__2", 0x04008CEA, typeof(Task));

        // Observation only. The native worker body runs on a background thread,
        // so this counts entries atomically and changes nothing else. It exists
        // because a cancellation request can only be qualified if it is timed to
        // land after serialization has actually started, not merely after the
        // iterator was queued.
        private static int workerEntries;
        internal static int WorkerEntryCount => System.Threading.Volatile.Read(ref workerEntries);

        // Diagnostic hold. Inert unless an isolated run arms it, and then it
        // holds exactly ONE already-authorized owned save worker at its entry,
        // on that worker's own background thread. The main thread keeps running,
        // so the per-frame drain and required completion work continue. It is
        // bounded and self-releasing: a failed test cannot leave a save
        // suspended, and the serializer and commit path run unchanged afterwards.
        private static readonly object holdLock = new object();
        private static bool holdArmed;
        private static System.Threading.ManualResetEventSlim holdGate;
        private static int holdMilliseconds;
        internal static string HeldWorkerLeaf { get; private set; }
        internal static int WorkerHoldCount { get; private set; }
        internal static bool WorkerHeld { get { lock (holdLock) { return holdGate != null && !holdGate.IsSet; } } }

        internal static IDisposable ArmWorkerHold(int milliseconds)
        {
            if (milliseconds < 100 || milliseconds > 30000)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), "An owned worker hold must be bounded.");
            // Authorization belongs here, on the game thread: the worker receives
            // the PREPARED descriptor, whose folder differs from the requested
            // one during commit, so validating it on the worker thread refuses
            // every legitimate hold.
            if (!NativePersistenceIsolation.IsIsolated)
                throw new InvalidOperationException("An owned worker hold requires the isolated save authority.");
            lock (holdLock)
            {
                if (holdArmed || holdGate != null)
                    throw new InvalidOperationException("An owned save worker hold is already armed.");
                holdArmed = true;
                holdMilliseconds = milliseconds;
            }
            return new WorkerHoldRelease();
        }

        internal static void ReleaseWorkerHold()
        {
            lock (holdLock) { holdArmed = false; if (holdGate != null && !holdGate.IsSet) holdGate.Set(); }
        }

        private sealed class WorkerHoldRelease : IDisposable
        {
            public void Dispose() => ReleaseWorkerHold();
        }

        internal static void ObserveWorkerEntry(SaveInfo saveInfo)
        {
            System.Threading.Interlocked.Increment(ref workerEntries);
            System.Threading.ManualResetEventSlim gate;
            lock (holdLock)
            {
                // Arming was already authorized on the game thread; this consumes
                // it once, for the next owned worker, and nothing else.
                if (!holdArmed) return;
                holdArmed = false;
                gate = holdGate = new System.Threading.ManualResetEventSlim(false);
                HeldWorkerLeaf = saveInfo == null ? null : saveInfo.FileName;
                WorkerHoldCount++;
            }
            try { gate.Wait(holdMilliseconds); }
            finally
            {
                lock (holdLock)
                {
                    if (ReferenceEquals(holdGate, gate)) holdGate = null;
                    gate.Dispose();
                }
            }
        }

        internal static Task TaskOf(IEnumerator<object> routine)
        {
            if (routine == null || routine.GetType() != Iterator)
                throw new InvalidOperationException("Native save iterator identity changed.");
            return (Task)SaveTask.GetValue(routine);
        }

        // Observation form: yields null instead of throwing for a routine that is
        // not the native save iterator, so capturing the worker never changes
        // control flow or masks another failure.
        internal static Task TaskIfNative(IEnumerator<object> routine) =>
            routine != null && routine.GetType() == Iterator ? (Task)SaveTask.GetValue(routine) : null;

        // Authoritative read of an operation's archive worker.
        //
        // Static inspection of the installed assembly settles what this can rely
        // on: the native iterator stores <saveTask>5__2 (0x04008CEA) exactly once,
        // at IL_0621 of MoveNext, and nothing -- no Dispose, no finally -- ever
        // writes it again. The field therefore outlives disposal, so reading it at
        // the release boundary is trustworthy even after the routine is disposed.
        //
        // Returns true only when the answer is established: the routine is the
        // native iterator and its field was read (the task may legitimately be
        // null, meaning no worker was ever created), or the routine is not the
        // native iterator at all and so owns no worker of ours. A read that fails
        // returns false, and "could not establish" must never be treated as
        // "verified no worker ever started".
        internal static bool TryReadWorker(IEnumerator<object> routine, out Task worker)
        {
            worker = null;
            if (routine == null) return false;
            if (routine.GetType() != Iterator) return true;
            try { worker = (Task)SaveTask.GetValue(routine); return true; }
            catch (Exception) { worker = null; return false; }
        }

        // May an owned save scope release its protections now? Only when the
        // worker question is settled AND either no worker was ever created or the
        // one that was can no longer commit. A faulted or canceled task is
        // finished and therefore releasable; a running one is not, and nothing in
        // the engine can cancel it. An unestablished answer defers.
        internal static bool CanReleaseScope(bool established, Task worker) =>
            established && (worker == null || worker.IsCompleted);

        // Bounded wait for a worker that cannot be canceled, used only where
        // there is no later frame to drain it. A faulted or canceled worker
        // makes Wait throw and is nonetheless finished, which is the only thing
        // this boundary decides, so that case settles rather than propagating.
        internal static bool WaitForWorkerSettlement(Task worker, int milliseconds)
        {
            if (worker == null) return true;
            try { return worker.Wait(milliseconds); }
            catch (Exception) { return worker.IsCompleted; }
        }

        internal static void RestoreCompletedPlayerReference(IEnumerator<object> routine, Player world, SceneEntitiesState party)
        {
            if (world == null || party == null) return;
            var task = TaskOf(routine);
            if (task == null || !task.IsCompleted) return;
            // The worker temporarily removes this reference for native JSON.
            // A failed serializer may skip its assignment back. All four tasks
            // have drained before native failure cleanup reaches this boundary.
            if (ReferenceEquals(Game.Instance?.Player, world) && world.CrossSceneState == null)
                world.CrossSceneState = party;
        }

        internal static int WaitForAll(Task[] pair, Task party, Task statistic)
        {
            if (pair == null || pair.Length != 2 || pair[0] == null || pair[1] == null || party == null || statistic == null)
                throw new InvalidOperationException("Native save task set changed.");
            // WaitAll reports failure only after every serializer settles. The
            // original WaitAny result is discarded by the installed worker.
            Task.WaitAll(pair[0], party, pair[1], statistic);
            return 0;
        }

        internal static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            if (original?.Module != typeof(SaveManager).Module || original.MetadataToken != 0x0600802A)
                throw new InvalidOperationException("Native save worker identity changed.");
            var code = instructions.ToList();
            var starts = Enumerable.Range(0, code.Count).Where(i => code[i].operand is MethodInfo method &&
                method.Module == typeof(SaveManager).Module && method.MetadataToken == 0x0600805A).ToArray();
            var wait = typeof(Task).GetMethod(nameof(Task.WaitAny), new[] { typeof(Task[]) });
            var hits = Enumerable.Range(0, code.Count).Where(i => Equals(code[i].operand, wait)).ToArray();
            if (starts.Length != 4 || hits.Length != 1 || code[hits[0]].labels.Count != 0 ||
                code[hits[0] + 1].opcode != OpCodes.Pop ||
                !starts.Select(i => LocalIndex(code[i + 1])).SequenceEqual(new[] { 3, 4, 5, 6 }) ||
                !original.GetMethodBody().LocalVariables.Where(l => l.LocalType == typeof(Task))
                    .Select(l => l.LocalIndex).SequenceEqual(new[] { 3, 4, 5, 6 }))
                throw new InvalidOperationException("Native save task creation/wait contract changed.");
            code[hits[0]].opcode = OpCodes.Call;
            code[hits[0]].operand = typeof(NativeSaveWorkerBoundary).GetMethod(nameof(WaitForAll), BindingFlags.NonPublic | BindingFlags.Static);
            code.InsertRange(hits[0], new[] {
                new CodeInstruction(OpCodes.Ldloc_S, code[starts[1] + 1].operand),
                new CodeInstruction(OpCodes.Ldloc_S, code[starts[3] + 1].operand) });
            return code;
        }

        private static int LocalIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_3) return 3;
            if (instruction.opcode != OpCodes.Stloc_S && instruction.opcode != OpCodes.Stloc) return -1;
            if (instruction.operand is LocalVariableInfo local) return local.LocalIndex;
            return -1;
        }
    }
}
