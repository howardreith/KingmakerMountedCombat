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
        internal static void ObserveWorkerEntry() => System.Threading.Interlocked.Increment(ref workerEntries);

        internal static Task TaskOf(IEnumerator<object> routine)
        {
            if (routine == null || routine.GetType() != Iterator)
                throw new InvalidOperationException("Native save iterator identity changed.");
            return (Task)SaveTask.GetValue(routine);
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
