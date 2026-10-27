using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Harmony12;
using KingmakerMountedCombat.Diagnostics;

internal static class GeneratedWorkingLoadRewriteTests
{
    private static int passed, failed;
    private static readonly List<string> trace = new List<string>();
    private static bool failCreate, failDeserialize, failFinally, failLog, failScene, failObservation;
    private static readonly Exception nativeError = new ApplicationException("generated native boundary");
    private static Exception caughtError;
    private static readonly FakeGame game = new FakeGame();
    private static void Check(bool value, string name) { Console.WriteLine((value ? "PASS " : "FAIL ") + name); if (value) passed++; else failed++; }
    public sealed class FakeGame { public FakeState State = new FakeState(); }
    public sealed class FakeState { public FakePlayer Player = new FakePlayer(); }
    public sealed class FakePlayer
    {
        public FakeScene Scene;
    }
    public sealed class FakeScene
    {
        public void PostLoad() { trace.Add("scene"); if (failScene) throw nativeError; }
    }
    public sealed class FakeParty { public FakeScene Result; }
    public sealed class FakeCallback { public FakeState State = new FakeState(); }
    public sealed class FakeSerializer
    {
        public FakePlayer Deserialize(object reader) { trace.Add("deserialize"); if (failDeserialize) throw nativeError; return returned; }
    }
    private static FakePlayer returned = new FakePlayer();
    private static FakeSerializer Create(object settings) { trace.Add("create"); if (failCreate) throw nativeError; return new FakeSerializer(); }
    private static FakeGame GetGame() { trace.Add("game"); return game; }
    private static void NativeFinally() { trace.Add("finally"); if (failFinally) throw nativeError; }
    private static void NativeLog(Exception error) { trace.Add("log"); caughtError = error; if (failLog) throw nativeError; }
    private static void Returned(object value, object receiver, int kind)
    { try { trace.Add("observe:" + kind); if (failObservation) throw new InvalidOperationException("observer boundary"); } catch (Exception) { } }
    private static void Receiver(object receiver, int kind)
    { try { trace.Add("observe:" + kind); if (failObservation) throw new InvalidOperationException("observer boundary"); } catch (Exception) { } }
    private static MethodInfo Method(string name) => typeof(GeneratedWorkingLoadRewriteTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
    private static FieldInfo Field(Type type, string name) => type.GetField(name);
    private static CodeInstruction Ins(OpCode op, object operand = null) => new CodeInstruction(op, operand);
    private static void Block(CodeInstruction ins, string kind, Type caught = null)
    {
        var list = (IList)ins.blocks; var block = list.GetType().GetGenericArguments()[0];
        list.Add(Activator.CreateInstance(block, new[] { Enum.Parse(block.GetField("blockType").FieldType, kind), (object)caught }));
    }
    // Emits only synthetic control flow after all native member operands have
    // been mapped to the explicit classes above. No installed method is emitted
    // or invoked; native method metadata is used solely by the actual matcher.
    private static void Emit(ILGenerator il, IEnumerable<CodeInstruction> code, IDictionary<object, object> mapping)
    {
        foreach (var ins in code)
        {
            foreach (object block in ins.blocks)
            {
                var type = block.GetType(); string kind = type.GetField("blockType").GetValue(block).ToString();
                if (kind == "BeginExceptionBlock") il.BeginExceptionBlock();
                else if (kind == "BeginCatchBlock") il.BeginCatchBlock((Type)type.GetField("catchType").GetValue(block));
                else if (kind == "BeginFinallyBlock") il.BeginFinallyBlock();
                else if (kind == "EndExceptionBlock") il.EndExceptionBlock();
                else throw new InvalidOperationException("Unsupported generated block.");
            }
            foreach (var label in ins.labels) il.MarkLabel(label);
            object value = ins.operand; object substitute; if (value != null && mapping.TryGetValue(value, out substitute)) value = substitute;
            var member = value as MemberInfo;
            if (member != null && (member.Module.ModuleVersionId == NativeWorkingLoadContract.GameMvid || member.Module.ModuleVersionId == NativeWorkingLoadContract.JsonMvid))
                throw new InvalidOperationException("An installed member escaped the generated boundary mapping.");
            if (value == null) il.Emit(ins.opcode);
            else if (value is MethodInfo) il.Emit(ins.opcode, (MethodInfo)value);
            else if (value is ConstructorInfo) il.Emit(ins.opcode, (ConstructorInfo)value);
            else if (value is FieldInfo) il.Emit(ins.opcode, (FieldInfo)value);
            else if (value is Label) il.Emit(ins.opcode, (Label)value);
            else if (value is int) il.Emit(ins.opcode, (int)value);
            else throw new InvalidOperationException("Unsupported generated operand.");
        }
    }
    private static Action<FakeCallback> Callback(NativeWorkingLoadContract c, bool observe)
    {
        var dm = new DynamicMethod("GeneratedCallback", typeof(void), new[] { typeof(FakeCallback) }, typeof(GeneratedWorkingLoadRewriteTests), true);
        var il = dm.GetILGenerator(); il.DeclareLocal(typeof(FakeSerializer));
        var code = new List<CodeInstruction> { Ins(OpCodes.Nop), Ins(OpCodes.Nop), Ins(OpCodes.Ldnull), Ins(OpCodes.Call, c.CreateSerializer),
            Ins(OpCodes.Stloc_0), Ins(OpCodes.Ldarg_0), Ins(OpCodes.Ldfld, Field(typeof(FakeCallback), "State")), Ins(OpCodes.Ldloc_0),
            Ins(OpCodes.Ldnull), Ins(OpCodes.Callvirt, c.DeserializePlayer), Ins(OpCodes.Stfld, c.PlayerState), Ins(OpCodes.Nop),
            Ins(OpCodes.Call, Method(nameof(NativeFinally))), Ins(OpCodes.Nop), Ins(OpCodes.Call, c.LogException), Ins(OpCodes.Nop), Ins(OpCodes.Nop), Ins(OpCodes.Ret) };
        Block(code[0], "BeginExceptionBlock"); Block(code[1], "BeginExceptionBlock"); Block(code[12], "BeginFinallyBlock");
        Block(code[13], "EndExceptionBlock"); Block(code[14], "BeginCatchBlock", typeof(Exception)); Block(code[16], "EndExceptionBlock");
        var mapped = new Dictionary<object, object> { { c.CreateSerializer, Method(nameof(Create)) }, { c.DeserializePlayer, typeof(FakeSerializer).GetMethod("Deserialize") },
            { c.PlayerState, Field(typeof(FakeState), "Player") }, { c.LogException, Method(nameof(NativeLog)) } };
        Emit(il, observe ? NativeWorkingLoadRewrite.Rewrite(code, c.Owners[1], c, Method(nameof(Returned)), Method(nameof(Receiver))) : code, mapped);
        return (Action<FakeCallback>)dm.CreateDelegate(typeof(Action<FakeCallback>));
    }
    private static Action<FakePlayer> Scene(NativeWorkingLoadContract c, bool observe)
    {
        var dm = new DynamicMethod("GeneratedScene", typeof(void), new[] { typeof(FakePlayer) }, typeof(GeneratedWorkingLoadRewriteTests), true);
        var code = new List<CodeInstruction> { Ins(OpCodes.Ldarg_0), Ins(OpCodes.Ldfld, c.CrossScene), Ins(OpCodes.Callvirt, c.ScenePostLoad), Ins(OpCodes.Nop), Ins(OpCodes.Ret) };
        Emit(dm.GetILGenerator(), observe ? NativeWorkingLoadRewrite.Rewrite(code, c.Owners[4], c, Method(nameof(Returned)), Method(nameof(Receiver))) : code,
            new Dictionary<object, object> { { c.CrossScene, Field(typeof(FakePlayer), "Scene") }, { c.ScenePostLoad, typeof(FakeScene).GetMethod("PostLoad") } });
        return (Action<FakePlayer>)dm.CreateDelegate(typeof(Action<FakePlayer>));
    }
    private static Action<object, FakeParty> CrossScene(NativeWorkingLoadContract c, bool observe)
    {
        var dm = new DynamicMethod("GeneratedCrossScene", typeof(void), new[] { typeof(object), typeof(FakeParty) }, typeof(GeneratedWorkingLoadRewriteTests), true);
        var il = dm.GetILGenerator(); il.DeclareLocal(typeof(object)); il.DeclareLocal(typeof(FakeParty)); var skip = il.DefineLabel(); var end = il.DefineLabel();
        var getter = c.GameState.Module.ResolveMethod(0x06000C8F);
        var code = new List<CodeInstruction> { Ins(OpCodes.Ldarg_1), Ins(OpCodes.Stloc_1), Ins(OpCodes.Nop), Ins(OpCodes.Ldloc_1), Ins(OpCodes.Ldfld, c.PartyResult),
            Ins(OpCodes.Brfalse, skip), Ins(OpCodes.Call, getter), Ins(OpCodes.Ldfld, c.GameState), Ins(OpCodes.Ldfld, c.PlayerState),
            Ins(OpCodes.Ldloc_1), Ins(OpCodes.Ldfld, c.PartyResult), Ins(OpCodes.Stfld, c.CrossScene), Ins(OpCodes.Leave, end),
            Ins(OpCodes.Call, Method(nameof(NativeFinally))), Ins(OpCodes.Nop), Ins(OpCodes.Ret) };
        Block(code[2], "BeginExceptionBlock"); code[12].labels.Add(skip); Block(code[13], "BeginFinallyBlock"); Block(code[14], "EndExceptionBlock"); code[14].labels.Add(end);
        var mapped = new Dictionary<object, object> { { getter, Method(nameof(GetGame)) }, { c.PartyResult, Field(typeof(FakeParty), "Result") },
            { c.GameState, Field(typeof(FakeGame), "State") }, { c.PlayerState, Field(typeof(FakeState), "Player") }, { c.CrossScene, Field(typeof(FakePlayer), "Scene") } };
        Emit(il, observe ? NativeWorkingLoadRewrite.Rewrite(code, c.Owners[3], c, Method(nameof(Returned)), Method(nameof(Receiver))) : code, mapped);
        return (Action<object, FakeParty>)dm.CreateDelegate(typeof(Action<object, FakeParty>));
    }
    private static void Reset()
    {
        trace.Clear(); failCreate = failDeserialize = failFinally = failLog = failScene = failObservation = false;
        caughtError = null; returned = new FakePlayer(); game.State = new FakeState();
    }
    private static Exception Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var c = new NativeWorkingLoadContract(); var original = Callback(c, false); var instrumented = Callback(c, true);
        foreach (string mode in new[] { "success", "create", "deserialize", "finally", "log", "observer" })
        {
            Reset(); failCreate = mode == "create"; failDeserialize = mode == "deserialize" || mode == "log"; failFinally = mode == "finally"; failLog = mode == "log"; failObservation = mode == "observer";
            var old = new FakeCallback(); var oldResult = Capture(() => original(old)); var oldTrace = trace.ToArray(); var oldStored = ReferenceEquals(old.State.Player, returned); var oldCaught = caughtError;
            Reset(); failCreate = mode == "create"; failDeserialize = mode == "deserialize" || mode == "log"; failFinally = mode == "finally"; failLog = mode == "log"; failObservation = mode == "observer";
            var current = new FakeCallback(); var result = Capture(() => instrumented(current));
            Check(ReferenceEquals(result, oldResult), "callback same exception identity " + mode);
            Check(trace.Where(x => !x.StartsWith("observe:", StringComparison.Ordinal)).SequenceEqual(oldTrace), "callback original calls/order/finally " + mode);
            Check(ReferenceEquals(current.State.Player, returned) == oldStored && ReferenceEquals(caughtError, oldCaught), "callback same store and catch result " + mode);
        }
        Reset(); instrumented(new FakeCallback()); Check(trace.SequenceEqual(new[] { "create", "observe:2", "deserialize", "observe:3", "observe:4", "finally" }), "callback post-return/store notices at exact points");
        Reset(); failDeserialize = true; instrumented(new FakeCallback()); Check(trace.SequenceEqual(new[] { "create", "observe:2", "deserialize", "finally", "log", "observe:5" }), "caught notice follows native log without return/store claims");
        var scene = Scene(c, true); Reset(); scene(new FakePlayer { Scene = new FakeScene() }); Check(trace.SequenceEqual(new[] { "observe:11", "scene", "observe:12" }), "scene return preserves stack and before/after order");
        Reset(); failScene = true; Check(ReferenceEquals(Capture(() => scene(new FakePlayer { Scene = new FakeScene() })), nativeError) && trace.SequenceEqual(new[] { "observe:11", "scene" }), "scene native exception propagates and after is absent");
        Reset(); Check(Capture(() => scene(new FakePlayer())) is NullReferenceException && trace.SequenceEqual(new[] { "observe:11" }), "null native scene still throws at original call");
        var cross = CrossScene(c, true);
        Reset(); var target = new FakeScene(); cross(new object(), new FakeParty { Result = target }); Check(ReferenceEquals(game.State.Player.Scene, target) && trace.SequenceEqual(new[] { "game", "observe:10", "finally" }), "exact cross-scene store and finally run once");
        Reset(); var previous = new FakeScene(); game.State.Player.Scene = previous; cross(new object(), new FakeParty()); Check(ReferenceEquals(game.State.Player.Scene, previous) && trace.SequenceEqual(new[] { "finally" }), "native null branch skips store and inserted observation");
        Reset(); failObservation = true; cross(new object(), new FakeParty { Result = target }); Check(ReferenceEquals(game.State.Player.Scene, target) && trace.SequenceEqual(new[] { "game", "observe:10", "finally" }), "contained observer failure does not change original store/finally");
        Reset(); failFinally = true; Check(ReferenceEquals(Capture(() => cross(new object(), new FakeParty { Result = target })), nativeError), "cross-scene native finally exception remains original");
    }
    private static int Main(string[] args)
    {
        try
        {
            var managed = args[0]; AppDomain.CurrentDomain.AssemblyResolve += (s, e) => { var leaf = new AssemblyName(e.Name).Name + ".dll";
                foreach (var dir in new[] { managed, Path.Combine(managed, "UnityModManager") }) { var path = Path.Combine(dir, leaf); if (File.Exists(path)) return Assembly.LoadFrom(path); } return null; };
            Run();
        }
        catch (Exception error) { failed++; Console.WriteLine("FAIL generated boundary: " + error); }
        Console.WriteLine("WORKING LOAD GENERATED REWRITE PASS=" + passed + " FAIL=" + failed); return failed == 0 ? 0 : 1;
    }
}
