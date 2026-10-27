using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KingmakerMountedCombat.Diagnostics.WorkingLoadPrototype
{
    // Detached observation policy. A real adapter must bind the exact pinned native
    // members and caller authority; this class never installs a patch or loads a save.
    internal enum DocumentLane { Player = 0, Party = 1 }
    internal enum ObservationFault { ForeignOwner, ForeignCallback, BadOrder, Duplicate, ReadFailure, EventLimit, IdentityLimit, BusyRelease, WrongReleaseThread, UnpatchFailure }

    internal interface IFieldObservationSource
    {
        string MainId(object player);
        int EntityCount(object scene);
        object EntityAt(object scene, int index);
        string EntityId(object entity);
        object HoldingScene(object entity);
        string TypeLabel(object value);
        object CurrentScene(object player);
        int ConverterCount(object serializer);
        object ConverterAt(object serializer, int index);
        bool IsExactCollectionsConverter(object converter);
        bool InitializedCollectionsEnablement(object exactConverter);
        string ContractResolverType(object serializer);
        string ExistingReferenceResolverType(object serializer);
    }

    internal sealed class EntityObservation
    {
        internal int Identity;
        internal string Id, Type;
        internal bool TextComplete, HoldingSceneMatches;
        internal bool? MainIdMatches;
    }

    internal sealed class SceneObservation
    {
        internal int PlayerIdentity, SceneIdentity, CurrentSceneIdentity, EntityCount, EndEntityCount;
        internal string MainId;
        // Complete means the bounded reads finished, never atomicity or authority.
        internal bool MainIdComplete, Complete;
        internal bool AtomicityProven => false;
        internal int MatchingPrefixCount;
        internal EntityObservation[] Prefix;
    }

    internal sealed class SerializerObservation
    {
        internal int Identity, ConverterCount;
        internal string ContractResolverType, ExistingReferenceResolverType;
        internal string[] ConverterTypes;
        internal bool Complete, CollectionsEnablementAvailable, CollectionsEnabled;
    }

    internal sealed class ObservationEvent
    {
        internal int Sequence, ThreadId;
        internal string Phase;
        internal DocumentLane? Lane;
        internal int ValueIdentity;
        internal SceneObservation Scene;
        internal SerializerObservation Serializer;
    }

    internal sealed class WorkingLoadObservation
    {
        internal const int EventCapacity = 32, IdentityCapacity = 128, EntityCapacity = 16, ConverterCapacity = 32;
        internal const int IdLimit = 128, TypeLimit = 192;
        private readonly object gate = new object();
        private IFieldObservationSource source;
        private readonly int ownerThread, hookCount;
        private object expectedManager, expectedSave, loader, beforePlayer, beforeScene;
        private Task task;
        private readonly LaneState[] lanes = { new LaneState(), new LaneState() };
        private readonly List<object> identities = new List<object>(IdentityCapacity);
        private readonly List<ObservationEvent> events = new List<ObservationEvent>(EventCapacity);
        private readonly HashSet<ObservationFault> faults = new HashSet<ObservationFault>();
        private readonly bool[] unpatched;
        private bool active = true, released, assignedToPlayer, before, after;
        private int callbacks;

        private sealed class LaneState
        {
            internal object Callback, Serializer, Returned;
            internal bool SerializerSeen, ReturnSeen, Assigned, Caught;
            internal void Clear() { Callback = Serializer = Returned = null; }
        }

        internal WorkingLoadObservation(object manager, object save, IFieldObservationSource source, int ownedHookCount)
        {
            if (manager == null || save == null || source == null) throw new ArgumentNullException();
            if (ownedHookCount < 1 || ownedHookCount > 8) throw new ArgumentOutOfRangeException(nameof(ownedHookCount));
            expectedManager = manager; expectedSave = save; this.source = source;
            hookCount = ownedHookCount; unpatched = new bool[ownedHookCount];
            ownerThread = Thread.CurrentThread.ManagedThreadId;
        }

        internal bool Released { get { lock (gate) return released; } }
        internal int RetainedCount { get { lock (gate) return identities.Count; } }
        internal ObservationFault[] Faults { get { lock (gate) { var result = new ObservationFault[faults.Count]; faults.CopyTo(result); return result; } } }
        internal void AdapterFault(ObservationFault fault) { Safe(() => faults.Add(fault)); }
        // A caller must serialize this detached array only after its own scheduling
        // boundary. Events contain scalar observations, never native references.
        internal ObservationEvent[] Events { get { lock (gate) return events.ToArray(); } }

        private void Safe(Action action)
        {
            lock (gate)
            {
                if (!active) return;
                callbacks++;
                try { action(); }
                catch (Exception) { faults.Add(ObservationFault.ReadFailure); }
                finally { callbacks--; }
            }
        }
        private bool Require(bool condition, ObservationFault fault)
        {
            if (!condition) faults.Add(fault);
            return condition;
        }
        private bool Owner(object actualLoader) => Require(loader != null && ReferenceEquals(loader, actualLoader), ObservationFault.ForeignOwner);
        private LaneState Admit(object actualLoader, object callback, DocumentLane lane)
        {
            if (!Owner(actualLoader) || !Require(callback != null && (lane == DocumentLane.Player || lane == DocumentLane.Party), ObservationFault.ForeignCallback)) return null;
            var state = lanes[(int)lane];
            if (state.Callback == null) state.Callback = callback;
            return Require(ReferenceEquals(state.Callback, callback), ObservationFault.ForeignCallback) ? state : null;
        }
        private int Identity(object value)
        {
            if (value == null) return 0;
            for (int index = 0; index < identities.Count; index++) if (ReferenceEquals(identities[index], value)) return index + 1;
            if (!Require(identities.Count < IdentityCapacity, ObservationFault.IdentityLimit)) return -1;
            identities.Add(value); return identities.Count;
        }
        private ObservationEvent Record(string phase, DocumentLane? lane = null, object value = null)
        {
            if (!Require(events.Count < EventCapacity, ObservationFault.EventLimit)) return null;
            var row = new ObservationEvent { Sequence = events.Count + 1, ThreadId = Thread.CurrentThread.ManagedThreadId,
                Phase = phase, Lane = lane, ValueIdentity = Identity(value) };
            events.Add(row); return row;
        }
        // Over-limit strings are unavailable, not silently shortened identities.
        private static string Text(string value, int limit, out bool complete)
        {
            complete = value == null || value.Length <= limit;
            if (!complete) return null;
            if (value != null) for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]) || (char.IsSurrogate(value[i]) &&
                    (!char.IsHighSurrogate(value[i]) || i + 1 >= value.Length || !char.IsLowSurrogate(value[++i]))))
                { complete = false; return null; }
            }
            return value;
        }

        internal void LoaderCreated(object manager, object save, object actualLoader, bool smokeTest)
        {
            Safe(() => {
                if (!Require(ReferenceEquals(manager, expectedManager) && ReferenceEquals(save, expectedSave) && actualLoader != null && !smokeTest, ObservationFault.ForeignOwner)) return;
                if (!Require(loader == null, ObservationFault.Duplicate)) return;
                loader = actualLoader; Identity(expectedManager); Identity(expectedSave); Record("loader-created", null, loader);
            });
        }
        internal void TaskReturned(object actualLoader, Task actualTask)
        {
            Safe(() => {
                if (!Owner(actualLoader) || !Require(actualTask != null && task == null, ObservationFault.Duplicate)) return;
                task = actualTask; Record("task-returned", null, actualTask);
            });
        }
        internal void SerializerReturned(object actualLoader, object callback, DocumentLane lane, object serializer)
        {
            Safe(() => {
                var state = Admit(actualLoader, callback, lane); if (state == null) return;
                if (!Require(!state.SerializerSeen && !state.Caught && serializer != null, ObservationFault.BadOrder)) return;
                state.SerializerSeen = true; state.Serializer = serializer;
                var row = Record("serializer-returned", lane, serializer); if (row != null) row.Serializer = CaptureSerializer(serializer);
            });
        }
        internal void ObjectReturned(object actualLoader, object callback, DocumentLane lane, object value)
        {
            Safe(() => {
                var state = Admit(actualLoader, callback, lane); if (state == null) return;
                if (!Require(state.SerializerSeen && !state.ReturnSeen && !state.Caught, ObservationFault.BadOrder)) return;
                state.ReturnSeen = true; state.Returned = value; Record("object-returned", lane, value);
            });
        }
        internal void ObjectAssigned(object actualLoader, object callback, DocumentLane lane, object value)
        {
            Safe(() => {
                var state = Admit(actualLoader, callback, lane); if (state == null) return;
                if (!Require(state.ReturnSeen && !state.Assigned && !state.Caught && ReferenceEquals(state.Returned, value), ObservationFault.BadOrder)) return;
                state.Assigned = true; Record("object-assigned", lane, value);
            });
        }
        internal void NativeCaughtPath(object actualLoader, object callback, DocumentLane lane)
        {
            Safe(() => {
                var state = Admit(actualLoader, callback, lane); if (state == null) return;
                if (!Require(!state.Caught, ObservationFault.Duplicate)) return;
                state.Caught = true; Record("native-caught-after-log", lane);
            });
        }
        internal void CrossSceneAssigned(object actualLoader, object player, object scene)
        {
            Safe(() => {
                if (!Owner(actualLoader) || !Require(!assignedToPlayer && lanes[0].Assigned && lanes[1].Assigned &&
                    player != null && scene != null && ReferenceEquals(lanes[0].Returned, player) && ReferenceEquals(lanes[1].Returned, scene), ObservationFault.BadOrder)) return;
                assignedToPlayer = true; var row = Record("cross-scene-assigned", null, scene);
                if (row != null) row.Scene = CaptureScene(player, scene);
            });
        }
        internal void BeforeScenePostLoad(object actualLoader, object player, object scene)
        {
            Safe(() => {
                if (!Owner(actualLoader) || !Require(!before && player != null && scene != null && task != null && task.Status == TaskStatus.RanToCompletion, ObservationFault.BadOrder)) return;
                before = true; beforePlayer = player; beforeScene = scene;
                var row = Record("scene-postload-before", null, scene); if (row != null) row.Scene = CaptureScene(player, scene);
            });
        }
        internal void AfterScenePostLoad(object actualLoader, object player, object scene)
        {
            Safe(() => {
                if (!Owner(actualLoader) || !Require(before && !after && ReferenceEquals(beforePlayer, player) && ReferenceEquals(beforeScene, scene), ObservationFault.BadOrder)) return;
                after = true; var row = Record("scene-postload-after", null, scene); if (row != null) row.Scene = CaptureScene(player, scene);
            });
        }

        private SceneObservation CaptureScene(object player, object scene)
        {
            var count = source.EntityCount(scene);
            if (count < 0) throw new InvalidOperationException();
            bool mainComplete;
            var result = new SceneObservation { PlayerIdentity = Identity(player), SceneIdentity = Identity(scene),
                CurrentSceneIdentity = Identity(source.CurrentScene(player)), EntityCount = count,
                MainId = Text(source.MainId(player), IdLimit, out mainComplete), MainIdComplete = mainComplete,
                Complete = mainComplete && count <= EntityCapacity, Prefix = new EntityObservation[Math.Min(count, EntityCapacity)] };
            mainComplete &= !string.IsNullOrEmpty(result.MainId);
            result.MainIdComplete = mainComplete; result.Complete &= mainComplete;
            for (int i = 0; i < result.Prefix.Length; i++)
            {
                var entity = source.EntityAt(scene, i); bool idComplete, typeComplete;
                var id = Text(entity == null ? null : source.EntityId(entity), IdLimit, out idComplete);
                idComplete &= !string.IsNullOrEmpty(id);
                var type = Text(entity == null ? null : source.TypeLabel(entity), TypeLimit, out typeComplete);
                bool? match = entity != null && mainComplete && idComplete ? (bool?)string.Equals(result.MainId, id, StringComparison.Ordinal) : null;
                result.Prefix[i] = new EntityObservation { Identity = Identity(entity), Id = id, Type = type,
                    TextComplete = idComplete && typeComplete, MainIdMatches = match,
                    HoldingSceneMatches = entity != null && ReferenceEquals(source.HoldingScene(entity), scene) };
                if (match == true) result.MatchingPrefixCount++;
                result.Complete &= entity != null && idComplete && typeComplete;
            }
            result.EndEntityCount = source.EntityCount(scene);
            result.Complete &= result.EndEntityCount == count;
            return result;
        }
        private SerializerObservation CaptureSerializer(object serializer)
        {
            var count = source.ConverterCount(serializer); if (count < 0) throw new InvalidOperationException();
            bool contractComplete, referenceComplete;
            var result = new SerializerObservation { Identity = Identity(serializer), ConverterCount = count,
                ConverterTypes = new string[Math.Min(count, ConverterCapacity)], Complete = count <= ConverterCapacity,
                ContractResolverType = Text(source.ContractResolverType(serializer), TypeLimit, out contractComplete),
                ExistingReferenceResolverType = Text(source.ExistingReferenceResolverType(serializer), TypeLimit, out referenceComplete) };
            result.Complete &= contractComplete && referenceComplete;
            for (int i = 0; i < result.ConverterTypes.Length; i++)
            {
                var converter = source.ConverterAt(serializer, i); bool complete;
                result.ConverterTypes[i] = Text(converter == null ? null : source.TypeLabel(converter), TypeLimit, out complete);
                result.Complete &= complete && converter != null;
                if (converter != null && source.IsExactCollectionsConverter(converter))
                {
                    result.CollectionsEnabled = source.InitializedCollectionsEnablement(converter);
                    result.CollectionsEnablementAvailable = true;
                }
            }
            result.Complete &= source.ConverterCount(serializer) == count;
            return result;
        }

        internal bool TryRelease(bool nativeBusy, Func<int, bool> unpatch)
        {
            lock (gate)
            {
                if (released) return true;
                if (!Require(Thread.CurrentThread.ManagedThreadId == ownerThread, ObservationFault.WrongReleaseThread)) return false;
                // Exact native DoLoad joins file and both deserializer tasks on
                // its successful path. A faulted/cancelled outer task can exit
                // before those joins, so IsCompleted alone grants no release.
                if (!Require(!nativeBusy && callbacks == 0 && (loader == null || (task != null && task.Status == TaskStatus.RanToCompletion)), ObservationFault.BusyRelease)) return false;
                for (int i = 0; i < hookCount; i++)
                {
                    if (unpatched[i]) continue;
                    try { unpatched[i] = unpatch != null && unpatch(i); }
                    catch (Exception) { unpatched[i] = false; }
                    if (!unpatched[i]) faults.Add(ObservationFault.UnpatchFailure);
                }
                for (int i = 0; i < hookCount; i++) if (!unpatched[i]) return false;
                // No native references are relinquished until every owned hook is gone.
                active = false; released = true;
                expectedManager = expectedSave = loader = beforePlayer = beforeScene = null; task = null; source = null;
                foreach (var state in lanes) state.Clear();
                identities.Clear(); return true;
            }
        }
    }
}
