using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Diagnostics.WorkingLoadPrototype;

namespace KingmakerMountedCombat.Diagnostics
{
    // The host must retain this owner before calling Install after the original
    // qualified Working guards. No native loader or callback is invoked here.
    internal sealed class NativeWorkingLoadObserver
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.WorkingLoadIdentity";
        private const BindingFlags HooksFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static volatile NativeWorkingLoadObserver active;
        private readonly object gate = new object();
        private readonly NativeWorkingLoadContract contract;
        // Harmony12 cannot generate a valid wrapper for the native bool MoveNext,
        // even with a pass-through transpiler. Retain its exact field contract,
        // but observe its factory and the loader's Task-returning Start instead.
        private readonly bool[] attempted = new bool[6], rewritten = new bool[5];
        private readonly object[] callbacks = new object[2];
        private Game game;
        private SaveManager manager;
        private SaveInfo save;
        private object iterator, loader, beforePlayer, beforeScene;
        private Task task;
        private HarmonyInstance harmony;
        private bool started, installed;
        internal WorkingLoadObservation Observation { get; }
        internal bool Installed => installed;
        internal bool Released => Observation.Released;
        internal bool LoaderObserved { get { lock (gate) return loader != null; } }
        internal bool TaskObserved { get { lock (gate) return task != null; } }
        internal bool TaskSettled { get { lock (gate) return task != null && task.IsCompleted; } }
        internal bool TaskSucceeded { get { lock (gate) return task != null && task.Status == TaskStatus.RanToCompletion; } }

        internal NativeWorkingLoadObserver(Game game, SaveManager manager, SaveInfo save)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            NativeWorkingLoadContract.Require(ReferenceEquals(game.SaveManager, manager), "Working observer manager changed.");
            contract = new NativeWorkingLoadContract();
            Observation = new WorkingLoadObservation(manager, save, new NativeWorkingLoadFields(contract), attempted.Length);
        }
        internal void Install()
        {
            lock (gate)
            {
                NativeWorkingLoadContract.Require(!started && active == null && !Released, "Working observer already owns an installation.");
                started = true; harmony = HarmonyInstance.Create(HarmonyId); active = this;
                attempted[0] = true;
                harmony.Patch(contract.LoadRoutineFactory, null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.IteratorReturned), HooksFlags)), null);
                attempted[1] = true;
                harmony.Patch(contract.Start, null,
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.StartReturned), HooksFlags)), null);
                for (int i = 2; i < attempted.Length; i++)
                {
                    attempted[i] = true; // Custody precedes each fallible patch.
                    harmony.Patch(contract.Owners[i - 1], null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.Rewrite), HooksFlags)));
                    NativeWorkingLoadContract.Require(rewritten[i - 1], "Exact Working observer rewrite did not run.");
                }
                installed = true;
            }
        }
        internal bool TryRelease(bool nativeBusy)
        {
            lock (gate)
            {
                if (Released) return true;
                if (!Observation.TryRelease(nativeBusy, i => {
                    if (!attempted[i]) return true;
                    var method = i == 0 ? contract.LoadRoutineFactory : i == 1 ? contract.Start : contract.Owners[i - 1];
                    var kind = i < 2 ? HarmonyPatchType.Postfix : HarmonyPatchType.Transpiler;
                    harmony.Unpatch(method, kind, HarmonyId);
                    var info = harmony.GetPatchInfo(method);
                    return info == null || !(i < 2 ? info.Postfixes : info.Transpilers).Any(p => p.owner == HarmonyId);
                })) return false;
                if (ReferenceEquals(active, this)) active = null;
                game = null; manager = null; save = null; iterator = loader = beforePlayer = beforeScene = null; task = null;
                callbacks[0] = callbacks[1] = null; harmony = null; return true;
            }
        }
        private bool Require(bool condition, ObservationFault fault)
        {
            if (!condition) Observation.AdapterFault(fault); return condition;
        }
        private bool Iterator(object value)
        {
            return Require(value != null && value.GetType() == contract.Owners[0].DeclaringType, ObservationFault.ForeignOwner) &&
                Require(ReferenceEquals(contract.IteratorManager.GetValue(value), manager) && ReferenceEquals(contract.IteratorSave.GetValue(value), save) &&
                    !(bool)contract.IteratorSmoke.GetValue(value), ObservationFault.ForeignOwner);
        }
        private void CaptureIterator(object value)
        {
            lock (gate)
            {
                if (Released || !ReferenceEquals(active, this)) return;
                try
                {
                    if (!Iterator(value) || !Require(iterator == null, ObservationFault.Duplicate)) return;
                    iterator = value;
                }
                catch (Exception) { Observation.AdapterFault(ObservationFault.ReadFailure); }
            }
        }
        private void CaptureStart(object actualLoader, Task actualTask)
        {
            lock (gate)
            {
                if (Released || !ReferenceEquals(active, this)) return;
                try
                {
                    if (!Require(iterator != null && actualLoader != null && actualLoader.GetType() == contract.LoaderType &&
                        ReferenceEquals(contract.IteratorLoader.GetValue(iterator), actualLoader) && loader == null &&
                        actualTask != null, ObservationFault.ForeignOwner)) return;
                    loader = actualLoader; task = actualTask;
                    Observation.LoaderCreated(manager, save, loader, false);
                    Observation.TaskReturned(loader, task);
                }
                catch (Exception) { Observation.AdapterFault(ObservationFault.ReadFailure); }
            }
        }
        private bool Callback(object value, DocumentLane lane)
        {
            int index = (int)lane;
            if (!Require(loader != null && value != null && value.GetType() == contract.Owners[index + 1].DeclaringType, ObservationFault.ForeignCallback)) return false;
            var parent = lane == DocumentLane.Player ? value : contract.PartyClosure.GetValue(value);
            if (!Require(parent != null && parent.GetType() == contract.Owners[1].DeclaringType &&
                ReferenceEquals(contract.ClosureLoader.GetValue(parent), loader), ObservationFault.ForeignCallback)) return false;
            if (callbacks[index] == null) callbacks[index] = value;
            return Require(ReferenceEquals(callbacks[index], value), ObservationFault.ForeignCallback);
        }
        private void Receive(object receiver, object value, WorkingLoadNotice kind)
        {
            // This entire notification, including field admission, is inside the
            // no-throw boundary. The original producer/store/log is outside it.
            lock (gate)
            {
                if (Released || !ReferenceEquals(active, this)) return;
                try
                {
                    switch (kind)
                    {
                        case WorkingLoadNotice.LoaderStored:
                            if (!Iterator(receiver) || !Require(iterator == null, ObservationFault.Duplicate)) return;
                            var actual = contract.IteratorLoader.GetValue(receiver);
                            if (!Require(actual != null && actual.GetType() == contract.LoaderType, ObservationFault.ForeignOwner)) return;
                            iterator = receiver; loader = actual; Observation.LoaderCreated(manager, save, loader, false); return;
                        case WorkingLoadNotice.TaskStored:
                            if (!Iterator(receiver) || !Require(ReferenceEquals(iterator, receiver) && ReferenceEquals(contract.IteratorLoader.GetValue(receiver), loader) && task == null, ObservationFault.ForeignOwner)) return;
                            task = contract.IteratorTask.GetValue(receiver) as Task;
                            if (!Require(task != null, ObservationFault.BadOrder)) return;
                            Observation.TaskReturned(loader, task); return;
                        case WorkingLoadNotice.PlayerSerializer:
                        case WorkingLoadNotice.PartySerializer:
                            var serializerLane = kind == WorkingLoadNotice.PlayerSerializer ? DocumentLane.Player : DocumentLane.Party;
                            if (!Callback(receiver, serializerLane) || !Require(value != null && contract.SerializerType.IsInstanceOfType(value), ObservationFault.BadOrder)) return;
                            Observation.SerializerReturned(loader, receiver, serializerLane, value); return;
                        case WorkingLoadNotice.PlayerReturned:
                        case WorkingLoadNotice.PartyReturned:
                            var returnLane = kind == WorkingLoadNotice.PlayerReturned ? DocumentLane.Player : DocumentLane.Party;
                            if (!Callback(receiver, returnLane) || !Require(value == null || (returnLane == DocumentLane.Player ? contract.PlayerType : contract.SceneType).IsInstanceOfType(value), ObservationFault.BadOrder)) return;
                            Observation.ObjectReturned(loader, receiver, returnLane, value); return;
                        case WorkingLoadNotice.PlayerStored:
                            if (!Callback(receiver, DocumentLane.Player)) return;
                            Observation.ObjectAssigned(loader, receiver, DocumentLane.Player, contract.CurrentPlayer(game)); return;
                        case WorkingLoadNotice.PartyStored:
                            if (!Callback(receiver, DocumentLane.Party)) return;
                            Observation.ObjectAssigned(loader, receiver, DocumentLane.Party, contract.PartyResult.GetValue(receiver)); return;
                        case WorkingLoadNotice.PlayerCaught:
                        case WorkingLoadNotice.PartyCaught:
                            var caughtLane = kind == WorkingLoadNotice.PlayerCaught ? DocumentLane.Player : DocumentLane.Party;
                            if (Callback(receiver, caughtLane)) Observation.NativeCaughtPath(loader, receiver, caughtLane); return;
                        case WorkingLoadNotice.CrossSceneStored:
                            if (!Require(loader != null && ReferenceEquals(receiver, loader), ObservationFault.ForeignOwner)) return;
                            var player = contract.CurrentPlayer(game);
                            if (!Require(player != null, ObservationFault.BadOrder)) return;
                            Observation.CrossSceneAssigned(loader, player, contract.CrossScene.GetValue(player)); return;
                        case WorkingLoadNotice.SceneBefore:
                            if (!Require(loader != null && task != null && task.Status == TaskStatus.RanToCompletion && receiver != null &&
                                ReferenceEquals(receiver, contract.CurrentPlayer(game)) && ReferenceEquals(value, contract.CrossScene.GetValue(receiver)) && beforePlayer == null, ObservationFault.ForeignOwner)) return;
                            beforePlayer = receiver; beforeScene = value; Observation.BeforeScenePostLoad(loader, receiver, value); return;
                        case WorkingLoadNotice.SceneAfter:
                            if (!Require(beforePlayer != null && ReferenceEquals(receiver, beforePlayer) && ReferenceEquals(receiver, contract.CurrentPlayer(game)), ObservationFault.ForeignOwner)) return;
                            Observation.AfterScenePostLoad(loader, receiver, beforeScene); return;
                        default: Observation.AdapterFault(ObservationFault.BadOrder); return;
                    }
                }
                catch (Exception) { Observation.AdapterFault(ObservationFault.ReadFailure); }
            }
        }
        private static class Hooks
        {
            internal static void IteratorReturned(System.Collections.Generic.IEnumerator<object> __result)
            {
                try { active?.CaptureIterator(__result); } catch (Exception) { }
            }
            internal static void StartReturned(Task __result, object __instance)
            {
                try { active?.CaptureStart(__instance, __result); } catch (Exception) { }
            }
            internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            {
                var owner = active;
                NativeWorkingLoadContract.Require(owner != null && owner.started && !owner.Released, "Native rewrite lacks retained owner.");
                int index = Array.FindIndex(owner.contract.Owners, p => Equals(p, __originalMethod));
                NativeWorkingLoadContract.Require(index >= 0 && !owner.rewritten[index], "Foreign or repeated native rewrite.");
                var result = NativeWorkingLoadRewrite.Rewrite(instructions, __originalMethod, owner.contract,
                    typeof(Hooks).GetMethod(nameof(Returned), HooksFlags), typeof(Hooks).GetMethod(nameof(Receiver), HooksFlags));
                owner.rewritten[index] = true; return result;
            }
            internal static void Returned(object value, object receiver, int kind)
            {
                try { var owner = active; if (owner != null) owner.Receive(receiver, value, (WorkingLoadNotice)kind); } catch (Exception) { }
            }
            internal static void Receiver(object receiver, int kind)
            {
                try { var owner = active; if (owner != null) owner.Receive(receiver, null, (WorkingLoadNotice)kind); } catch (Exception) { }
            }
        }
    }
}
