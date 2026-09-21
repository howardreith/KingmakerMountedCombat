using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Keeps native QueuedProcess records, callbacks, ordering and completion.
    // Only this mod's selected save can delay its screen/game-clock pause.
    internal static class NativeDeferredSave
    {
        private static readonly Type Queued = typeof(LoadingProcess).GetNestedType("QueuedProcess", BindingFlags.NonPublic);
        private static readonly FieldInfo Current = NativeCombatActorPersistence.Field(
            typeof(LoadingProcess), "m_CurrentProcess", 0x040053DD, Queued);
        private static readonly FieldInfo Queue = NativeCombatActorPersistence.Field(
            typeof(LoadingProcess), "m_Queue", 0x040053DF, typeof(Queue<>).MakeGenericType(Queued));
        private static readonly FieldInfo Process = NativeCombatActorPersistence.Field(
            Queued, "Process", 0x04008CAE, typeof(IEnumerator));
        private static readonly FieldInfo Callback = NativeCombatActorPersistence.Field(
            Queued, "Callback", 0x04008CAF, typeof(Action));
        private static readonly MethodInfo ShowScreen = ResolveScreen();

        private static MethodInfo ResolveScreen()
        {
            var method = typeof(LoadingProcess).GetMethod("StartLoadingScreen",
                BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(ILoadingScreen) }, null);
            if (method == null || method.MetadataToken != 0x06007FC9)
                throw new InvalidOperationException("Native loading screen signature changed.");
            return method;
        }

        private static IEnumerator Enumerator(object queued) =>
            queued == null ? null : (IEnumerator)Process.GetValue(queued);

        internal static bool Waiting(LoadingProcess owner) =>
            !ReferenceEquals(owner, null) && Enumerator(Current.GetValue(owner)) is DeferredSaveEnumerator<object> operation && operation.Waiting;

        internal static void StartScreen(LoadingProcess owner, ILoadingScreen screen, object queued)
        {
            Action start = () => ShowScreen.Invoke(owner, new object[] { screen });
            if (!(Enumerator(queued) is DeferredSaveEnumerator<object> operation)) { start(); return; }
            try { operation.Activate(start); }
            catch (Exception exception)
            {
                if (!RetireFailedSave(queued, operation, exception)) throw;
            }
        }

        internal static bool MoveNext(IEnumerator iterator, LoadingProcess owner)
        {
            try { return iterator.MoveNext(); }
            catch (Exception exception)
            {
                var record = Current.GetValue(owner);
                if (!ReferenceEquals(Enumerator(record), iterator) ||
                    !RetireFailedSave(record, iterator as DeferredSaveEnumerator<object>, exception)) throw;
                // Native TickLoading still owns progress, screen release and the
                // next queued operation. A failed write has no success callback.
                return false;
            }
        }

        private static bool RetireFailedSave(object record, DeferredSaveEnumerator<object> operation, Exception exception)
        {
            if (record == null || operation == null || (!operation.FailedBeforeSerialization && !operation.FailedAfterNativeCleanup) ||
                !ReferenceEquals(Enumerator(record), operation)) return false;
            Callback.SetValue(record, null);
            MountedPatchController.ReportFailedSave(exception);
            return true;
        }

        internal static IEnumerable<CodeInstruction> TransformTick(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var move = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext));
            var hits = Enumerable.Range(0, code.Count).Where(i => Equals(code[i].operand, move)).ToArray();
            if (hits.Length != 1 || code[hits[0]].labels.Count != 0 ||
                code.Count(i => Equals(i.operand, Callback)) != 1)
                throw new InvalidOperationException("Native loading completion/callback order changed.");
            var call = code[hits[0]];
            call.opcode = OpCodes.Call;
            call.operand = typeof(NativeDeferredSave).GetMethod(nameof(MoveNext), BindingFlags.Static | BindingFlags.NonPublic);
            code.Insert(hits[0], new CodeInstruction(OpCodes.Ldarg_0));
            return code;
        }

        internal static void AbandonOwned(LoadingProcess owner, Action<Exception> report)
        {
            // Native StopAll drops enumerators without disposing them. Capture
            // only KMC-owned wrappers; never dispose a foreign/native coroutine.
            var records = new[] { Current.GetValue(owner) }
                .Concat(((IEnumerable)Queue.GetValue(owner)).Cast<object>()).ToArray();
            foreach (var iterator in records.Select(Enumerator).Distinct())
            {
                if (!(iterator is DeferredSaveEnumerator<object>) && !(iterator is ScopedEnumerator<object>)) continue;
                try { ((IDisposable)iterator).Dispose(); }
                catch (Exception exception) { try { report(exception); } catch { /* Native StopAll must still clear its queue. */ } }
            }
        }

        internal static IEnumerable<CodeInstruction> TransformStart(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var hits = Enumerable.Range(0, code.Count).Where(i => Equals(code[i].operand, ShowScreen)).ToArray();
            if (hits.Length != 1 || code[hits[0]].labels.Count != 0 ||
                code.Count(i => Equals(i.operand, Current)) != 1 ||
                hits[0] >= code.FindIndex(i => Equals(i.operand, Current)))
                throw new InvalidOperationException("Native loading activation/screen order changed.");
            var call = code[hits[0]];
            call.opcode = OpCodes.Call;
            call.operand = typeof(NativeDeferredSave).GetMethod(nameof(StartScreen), BindingFlags.Static | BindingFlags.NonPublic);
            code.Insert(hits[0], new CodeInstruction(OpCodes.Ldarg_1));
            return code;
        }
    }
}
