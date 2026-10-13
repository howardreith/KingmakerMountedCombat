using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Diagnostics.WorkingLoadPrototype;

// Native and Harmony types are external substitutes only in this standalone
// compilation. The observer and recorder files are compiled byte-for-byte.
namespace Kingmaker.EntitySystem.Persistence { public sealed class SaveManager { } public sealed class SaveInfo { } }
namespace Kingmaker
{
    public sealed class Game
    {
        public EntitySystem.Persistence.SaveManager SaveManager;
        public AdapterState State = new AdapterState();
    }
}
public sealed class AdapterState { public AdapterPlayer Player = new AdapterPlayer(); }
public sealed class AdapterPlayer { public AdapterScene Scene = new AdapterScene(); public string MainId = "main"; public void PostLoad() { } }
public sealed class AdapterScene { public AdapterEntity[] Entities = new AdapterEntity[0]; }
public sealed class AdapterEntity { public string Id = "main"; public AdapterScene Scene; }
public sealed class AdapterSerializer { }
public sealed class AdapterLoader { public void DoLoad() { } }
public sealed class AdapterIterator
{
    public Kingmaker.EntitySystem.Persistence.SaveManager Manager;
    public Kingmaker.EntitySystem.Persistence.SaveInfo Save;
    public bool Smoke;
    public AdapterLoader Loader;
    public Task Task;
    public bool MoveNext() { return false; }
}
public sealed class AdapterPlayerClosure { public AdapterLoader Loader; public void Invoke() { } }
public sealed class AdapterPartyClosure { public AdapterPlayerClosure Parent; public AdapterScene Result; public void Invoke() { } }
namespace Harmony12
{
    public sealed class CodeInstruction { }
    public enum HarmonyPatchType { Transpiler }
    public sealed class HarmonyMethod { public readonly MethodInfo Method; public HarmonyMethod(MethodInfo method) { Method = method; } }
    public sealed class Patch { public string owner; }
    public sealed class Patches { public readonly List<Patch> Transpilers = new List<Patch>(); }
    public sealed class HarmonyInstance
    {
        public static int FailPatch, FailUnpatch, RefuseRemoval, PatchCalls;
        public static readonly Dictionary<MethodBase, int> UnpatchCalls = new Dictionary<MethodBase, int>();
        private readonly Dictionary<MethodBase, Patches> owned = new Dictionary<MethodBase, Patches>();
        private readonly string id;
        private HarmonyInstance(string id) { this.id = id; }
        public static HarmonyInstance Create(string id) { return new HarmonyInstance(id); }
        public void Patch(MethodBase method, HarmonyMethod prefix, HarmonyMethod postfix, HarmonyMethod transpiler)
        {
            PatchCalls++; var p = new Patches(); p.Transpilers.Add(new Patch { owner = id }); owned.Add(method, p);
            if (PatchCalls == FailPatch) throw new InvalidOperationException("external patch failure after acquisition");
            transpiler.Method.Invoke(null, new object[] { new CodeInstruction[0], method });
        }
        public void Unpatch(MethodBase method, HarmonyPatchType type, string owner)
        {
            int count; UnpatchCalls.TryGetValue(method, out count); UnpatchCalls[method] = count + 1;
            int index = Array.IndexOf(NativeWorkingLoadContract.TestOwners, method) + 1;
            if (index == FailUnpatch) throw new InvalidOperationException("external unpatch failure");
            if (index != RefuseRemoval) owned.Remove(method);
        }
        public Patches GetPatchInfo(MethodBase method) { Patches value; return owned.TryGetValue(method, out value) ? value : null; }
        public static void Reset() { FailPatch = FailUnpatch = RefuseRemoval = PatchCalls = 0; UnpatchCalls.Clear(); }
    }
}
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class NativeWorkingLoadContract
    {
        internal static readonly MethodInfo[] TestOwners = { typeof(AdapterIterator).GetMethod("MoveNext"), typeof(AdapterPlayerClosure).GetMethod("Invoke"),
            typeof(AdapterPartyClosure).GetMethod("Invoke"), typeof(AdapterLoader).GetMethod("DoLoad"), typeof(AdapterPlayer).GetMethod("PostLoad") };
        internal readonly MethodInfo[] Owners = TestOwners;
        internal readonly FieldInfo IteratorManager = typeof(AdapterIterator).GetField("Manager"), IteratorSave = typeof(AdapterIterator).GetField("Save"),
            IteratorSmoke = typeof(AdapterIterator).GetField("Smoke"), IteratorLoader = typeof(AdapterIterator).GetField("Loader"), IteratorTask = typeof(AdapterIterator).GetField("Task"),
            PartyClosure = typeof(AdapterPartyClosure).GetField("Parent"), ClosureLoader = typeof(AdapterPlayerClosure).GetField("Loader"), PartyResult = typeof(AdapterPartyClosure).GetField("Result"),
            CrossScene = typeof(AdapterPlayer).GetField("Scene");
        internal readonly Type LoaderType = typeof(AdapterLoader), SerializerType = typeof(AdapterSerializer), PlayerType = typeof(AdapterPlayer), SceneType = typeof(AdapterScene);
        internal object CurrentPlayer(object game) => ((Kingmaker.Game)game).State.Player;
        internal static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
    internal static class NativeWorkingLoadRewrite
    {
        // Only external Harmony dispatch is replaced; exact installed rewriting
        // and executable stack/EH behavior have separate actual-source tests.
        internal static IEnumerable<Harmony12.CodeInstruction> Rewrite(IEnumerable<Harmony12.CodeInstruction> code, MethodBase owner, NativeWorkingLoadContract c, MethodInfo a, MethodInfo b) => code;
    }
    internal enum WorkingLoadNotice { LoaderStored, TaskStored, PlayerSerializer, PlayerReturned, PlayerStored, PlayerCaught,
        PartySerializer, PartyReturned, PartyStored, PartyCaught, CrossSceneStored, SceneBefore, SceneAfter }
    internal sealed class NativeWorkingLoadFields : IFieldObservationSource
    {
        internal static int Reads; internal static bool Throw;
        internal NativeWorkingLoadFields(NativeWorkingLoadContract c) { }
        private static void Read() { Reads++; if (Throw) throw new InvalidOperationException("external field failure"); }
        public string MainId(object player) { Read(); return ((AdapterPlayer)player).MainId; }
        public object CurrentScene(object player) { Read(); return ((AdapterPlayer)player).Scene; }
        public int EntityCount(object scene) { Read(); return ((AdapterScene)scene).Entities.Length; }
        public object EntityAt(object scene, int index) { Read(); return ((AdapterScene)scene).Entities[index]; }
        public string EntityId(object entity) { Read(); return ((AdapterEntity)entity).Id; }
        public object HoldingScene(object entity) { Read(); return ((AdapterEntity)entity).Scene; }
        public string TypeLabel(object value) { Read(); return value.GetType().FullName; }
        public int ConverterCount(object serializer) { Read(); return 0; }
        public object ConverterAt(object serializer, int index) { throw new InvalidOperationException(); }
        public bool IsExactCollectionsConverter(object value) { return false; }
        public bool InitializedCollectionsEnablement(object value) { throw new InvalidOperationException(); }
        public string ContractResolverType(object serializer) { Read(); return null; }
        public string ExistingReferenceResolverType(object serializer) { Read(); return null; }
    }
}
internal static class WorkingLoadAdapterBoundaryTests
{
    private static int passed, failed;
    private static readonly Type hooks = typeof(NativeWorkingLoadObserver).GetNestedType("Hooks", BindingFlags.NonPublic);
    private static readonly MethodInfo receiver = hooks.GetMethod("Receiver", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo returned = hooks.GetMethod("Returned", BindingFlags.Static | BindingFlags.NonPublic);
    private static void Check(bool value, string name) { Console.WriteLine((value ? "PASS " : "FAIL ") + name); if (value) passed++; else failed++; }
    private static void Notify(WorkingLoadNotice kind, object owner, object value = null)
    {
        if (kind == WorkingLoadNotice.PlayerSerializer || kind == WorkingLoadNotice.PartySerializer || kind == WorkingLoadNotice.PlayerReturned || kind == WorkingLoadNotice.PartyReturned || kind == WorkingLoadNotice.SceneBefore)
            returned.Invoke(null, new[] { value, owner, (object)(int)kind });
        else receiver.Invoke(null, new[] { owner, (object)(int)kind });
    }
    private sealed class Fixture
    {
        internal readonly Kingmaker.Game Game = new Kingmaker.Game { SaveManager = new Kingmaker.EntitySystem.Persistence.SaveManager() };
        internal readonly Kingmaker.EntitySystem.Persistence.SaveInfo Save = new Kingmaker.EntitySystem.Persistence.SaveInfo();
        internal readonly TaskCompletionSource<int> Completion = new TaskCompletionSource<int>();
        internal readonly NativeWorkingLoadObserver Owner;
        internal readonly AdapterIterator Iterator;
        internal readonly AdapterPlayerClosure Player;
        internal readonly AdapterPartyClosure Party;
        internal Fixture(bool install = true)
        {
            Harmony12.HarmonyInstance.Reset(); NativeWorkingLoadFields.Reads = 0; NativeWorkingLoadFields.Throw = false;
            Owner = new NativeWorkingLoadObserver(Game, Game.SaveManager, Save);
            Iterator = new AdapterIterator { Manager = Game.SaveManager, Save = Save, Loader = new AdapterLoader(), Task = Completion.Task };
            Player = new AdapterPlayerClosure { Loader = Iterator.Loader }; Party = new AdapterPartyClosure { Parent = Player, Result = Game.State.Player.Scene };
            if (install) Owner.Install();
        }
        internal void Loader() { Notify(WorkingLoadNotice.LoaderStored, Iterator); }
        internal void Task() { Notify(WorkingLoadNotice.TaskStored, Iterator); }
        internal void PlayerResult()
        {
            Notify(WorkingLoadNotice.PlayerSerializer, Player, new AdapterSerializer()); Notify(WorkingLoadNotice.PlayerReturned, Player, Game.State.Player); Notify(WorkingLoadNotice.PlayerStored, Player);
        }
        internal void PartyResult()
        {
            Notify(WorkingLoadNotice.PartySerializer, Party, new AdapterSerializer()); Notify(WorkingLoadNotice.PartyReturned, Party, Party.Result); Notify(WorkingLoadNotice.PartyStored, Party);
        }
        internal void Release() { if (!Completion.Task.IsCompleted) Completion.SetResult(0); if (!Owner.TryRelease(false)) throw new InvalidOperationException("test owner failed to release"); }
    }
    private static void Run()
    {
        Notify(WorkingLoadNotice.PlayerSerializer, new object(), new object()); Check(NativeWorkingLoadFields.Reads == 0, "no installed owner performs no field reads");
        var f = new Fixture(); Check(f.Owner.Installed && Harmony12.HarmonyInstance.PatchCalls == 5, "all five exact external patch targets acquired");
        Notify(WorkingLoadNotice.PlayerSerializer, f.Player, new AdapterSerializer()); Check(NativeWorkingLoadFields.Reads == 0 && f.Owner.Observation.Events.Length == 0, "first callback before loader provenance refuses without reads");
        var foreign = new AdapterIterator { Manager = f.Game.SaveManager, Save = new Kingmaker.EntitySystem.Persistence.SaveInfo(), Loader = f.Iterator.Loader, Task = f.Iterator.Task };
        Notify(WorkingLoadNotice.LoaderStored, foreign); Check(f.Owner.Observation.Events.Length == 0, "foreign saved descriptor cannot establish first loader");
        f.Iterator.Smoke = true; f.Loader(); Check(f.Owner.Observation.Events.Length == 0, "smoke-test iterator refuses"); f.Iterator.Smoke = false; f.Loader();
        Check(f.Owner.Observation.Events.Single().Phase == "loader-created", "exact iterator manager/save establishes actual loader");
        Notify(WorkingLoadNotice.PlayerSerializer, new AdapterPlayerClosure { Loader = new AdapterLoader() }, new AdapterSerializer());
        Notify(WorkingLoadNotice.PartySerializer, new AdapterPartyClosure { Parent = new AdapterPlayerClosure { Loader = new AdapterLoader() } }, new AdapterSerializer());
        Check(NativeWorkingLoadFields.Reads == 0, "forged initial player and party closure refuse before field traversal");
        f.PlayerResult(); f.PartyResult(); Check(f.Owner.Observation.Events.Count(e => e.Phase == "object-assigned") == 2, "sourced callbacks may run before native Task store");
        int events = f.Owner.Observation.Events.Length; foreign.Save = f.Save; Notify(WorkingLoadNotice.TaskStored, foreign);
        Check(f.Owner.Observation.Events.Length == events, "same manager/save foreign iterator cannot adopt first Task"); f.Task();
        Notify(WorkingLoadNotice.CrossSceneStored, new AdapterLoader()); Check(f.Owner.Observation.Events.Length == events + 1, "foreign loader cannot claim css assignment");
        Notify(WorkingLoadNotice.CrossSceneStored, f.Iterator.Loader); Check(f.Owner.Observation.Events.Last().Phase == "cross-scene-assigned", "actual stored player and scene are observed");
        int reads = NativeWorkingLoadFields.Reads; Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, f.Party.Result);
        Check(NativeWorkingLoadFields.Reads == reads, "unsettled actual Task cannot admit scene traversal");
        Check(!f.Owner.TryRelease(false) && f.Owner.Observation.RetainedCount > 0 && Harmony12.HarmonyInstance.UnpatchCalls.Count == 0, "unsettled Task retains owner and hooks");
        f.Completion.SetResult(0); Notify(WorkingLoadNotice.SceneBefore, new AdapterPlayer { Scene = f.Party.Result }, f.Party.Result);
        Check(NativeWorkingLoadFields.Reads == reads, "foreign current player cannot admit first post-load snapshot");
        Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, new AdapterScene()); Check(NativeWorkingLoadFields.Reads == reads, "foreign stack scene cannot admit first post-load snapshot");
        Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, f.Party.Result); Notify(WorkingLoadNotice.SceneAfter, f.Game.State.Player);
        Check(f.Owner.Observation.Events.Count(e => e.Phase.StartsWith("scene-postload-", StringComparison.Ordinal)) == 2, "exact current native player and scene bracket are recorded");
        Check(!f.Owner.TryRelease(true) && Harmony12.HarmonyInstance.UnpatchCalls.Count == 0, "native queued/loading fence retains hooks after Task settles");
        Check(!System.Threading.Tasks.Task.Run(() => f.Owner.TryRelease(false)).Result, "foreign release thread cannot dispose owner");
        Harmony12.HarmonyInstance.FailUnpatch = 2; Check(!f.Owner.TryRelease(false) && Harmony12.HarmonyInstance.UnpatchCalls.Count == 5 && f.Owner.Observation.RetainedCount > 0, "failed unpatch retains references while all targets are attempted");
        Harmony12.HarmonyInstance.FailUnpatch = 0; f.Release();
        Check(f.Owner.Released && f.Owner.Observation.RetainedCount == 0 && Harmony12.HarmonyInstance.UnpatchCalls.Values.Count(x => x == 2) == 1, "retry disposes only failed target and then releases references");
        events = f.Owner.Observation.Events.Length; reads = NativeWorkingLoadFields.Reads; Notify(WorkingLoadNotice.PlayerSerializer, f.Player, new AdapterSerializer());
        Check(f.Owner.Observation.Events.Length == events && NativeWorkingLoadFields.Reads == reads, "released stale callbacks are inert");
        f = new Fixture(); f.Loader(); f.Task(); NativeWorkingLoadFields.Throw = true;
        Notify(WorkingLoadNotice.PlayerSerializer, f.Player, new AdapterSerializer()); Check(f.Owner.Observation.Faults.Contains(ObservationFault.ReadFailure), "actual no-throw hook contains field failure"); f.Release();
        f = new Fixture(); f.Loader(); f.Task(); f.PlayerResult(); f.PartyResult(); f.Completion.SetResult(0); NativeWorkingLoadFields.Throw = true;
        Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, f.Party.Result); Check(f.Owner.Observation.Faults.Contains(ObservationFault.ReadFailure), "actual no-throw hook contains bounded list read failure"); f.Release();
        f = new Fixture(false); Harmony12.HarmonyInstance.FailPatch = 3; bool failedInstall = false; try { f.Owner.Install(); } catch (InvalidOperationException) { failedInstall = true; }
        Check(failedInstall && !f.Owner.Installed && !f.Owner.Released, "partial acquisition failure remains retained before native load"); f.Release();
        Check(Harmony12.HarmonyInstance.UnpatchCalls.Count == 3, "partial installation releases exactly all attempted targets");
        f = new Fixture(); Harmony12.HarmonyInstance.RefuseRemoval = 4;
        Check(!f.Owner.TryRelease(false) && !f.Owner.Released, "unpatch return without owned-hook disappearance refuses release");
        Harmony12.HarmonyInstance.RefuseRemoval = 0; f.Release(); Check(f.Owner.Released, "verified later hook removal completes retained retry");
        f = new Fixture(); f.Loader(); f.Task(); f.PlayerResult(); var replacement = new AdapterPlayer(); f.Game.State.Player = replacement;
        Notify(WorkingLoadNotice.PlayerStored, f.Player); Check(f.Owner.Observation.Faults.Contains(ObservationFault.BadOrder), "replacement after native return cannot be credited as that return"); f.Release();
        Check(Harmony12.HarmonyInstance.UnpatchCalls.Count == 5, "final generated owner fully released");
        foreach (var absentId in new string[] { null, "" })
        {
            f = new Fixture(); f.Loader(); f.Task(); f.Completion.SetResult(0);
            f.Game.State.Player.MainId = absentId;
            f.Party.Result.Entities = new[] { new AdapterEntity { Id = absentId, Scene = f.Party.Result } };
            Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, f.Party.Result);
            var scene = f.Owner.Observation.Events.Last().Scene;
            Check(scene.Prefix[0].MainIdMatches == null && scene.MatchingPrefixCount == 0 && !scene.MainIdComplete && !scene.Complete,
                "absent main and entity IDs never produce a literal match " + (absentId == null ? "null" : "empty"));
            f.Release();
        }
        f = new Fixture(); f.Loader(); f.Task();
        var incompleteChild = new TaskCompletionSource<int>();
        f.Completion.SetException(new InvalidOperationException("generated outer failure before native child join"));
        reads = NativeWorkingLoadFields.Reads;
        Notify(WorkingLoadNotice.SceneBefore, f.Game.State.Player, f.Party.Result);
        Check(NativeWorkingLoadFields.Reads == reads, "faulted outer task does not prove child settlement for scene reads");
        Check(!f.Owner.TryRelease(false) && !f.Owner.Released && !incompleteChild.Task.IsCompleted &&
            Harmony12.HarmonyInstance.UnpatchCalls.Count == 0, "faulted outer task with pending child retains all custody");
        // This final generated owner intentionally remains retained on repair;
        // no test claims successful teardown of the unproven native child lane.
    }
    private static int Main()
    {
        try { Run(); } catch (Exception error) { failed++; Console.WriteLine("FAIL external adapter boundary: " + error); }
        Console.WriteLine("WORKING LOAD ADAPTER BOUNDARY PASS=" + passed + " FAIL=" + failed); return failed == 0 ? 0 : 1;
    }
}
