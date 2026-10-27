using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Harmony12;
using KingmakerMountedCombat.Diagnostics;

internal static class InstalledWorkingLoadMatcherTests
{
    private static int passed, failed;
    private static void Check(bool ok, string name) { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (ok) passed++; else failed++; }
    private static void Refuses(Action action, string name)
    { try { action(); Check(false, name); } catch (InvalidOperationException) { Check(true, name); } }
    private static string Hash(byte[] data) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
    private static void Returned(object value, object receiver, int kind) { }
    private static void Receiver(object receiver, int kind) { }
    private static readonly Dictionary<ushort, OpCode> Opcodes = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(x => (ushort)x.Value);
    private sealed class Row { internal int Offset; internal OpCode Code; internal object Operand; }
    // Original IL metadata decoder. It constructs instruction metadata only; it
    // never emits or invokes any decoded native method or its field/property.
    private static List<CodeInstruction> Read(MethodInfo method)
    {
        var body = method.GetMethodBody(); var bytes = body.GetILAsByteArray(); var rows = new List<Row>(); int p = 0;
        while (p < bytes.Length)
        {
            var row = new Row { Offset = p }; ushort value = bytes[p++]; if (value == 0xfe) value = (ushort)(0xfe00 | bytes[p++]);
            row.Code = Opcodes[value];
            switch (row.Code.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: row.Operand = (sbyte)bytes[p++]; break;
                case OperandType.InlineI: row.Operand = BitConverter.ToInt32(bytes, p); p += 4; break;
                case OperandType.InlineI8: row.Operand = BitConverter.ToInt64(bytes, p); p += 8; break;
                case OperandType.ShortInlineR: row.Operand = BitConverter.ToSingle(bytes, p); p += 4; break;
                case OperandType.InlineR: row.Operand = BitConverter.ToDouble(bytes, p); p += 8; break;
                case OperandType.ShortInlineVar: row.Operand = (int)bytes[p++]; break;
                case OperandType.InlineVar: row.Operand = (int)BitConverter.ToUInt16(bytes, p); p += 2; break;
                case OperandType.InlineBrTarget: int distance = BitConverter.ToInt32(bytes, p); p += 4; row.Operand = p + distance; break;
                case OperandType.ShortInlineBrTarget: int shortDistance = (sbyte)bytes[p++]; row.Operand = p + shortDistance; break;
                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(bytes, p); p += 4; int end = p + 4 * count; var targets = new int[count];
                    for (int i = 0; i < count; i++) { targets[i] = end + BitConverter.ToInt32(bytes, p); p += 4; } row.Operand = targets; break;
                default:
                    int token = BitConverter.ToInt32(bytes, p); p += 4;
                    switch (row.Code.OperandType)
                    {
                        case OperandType.InlineField: row.Operand = method.Module.ResolveField(token); break;
                        case OperandType.InlineMethod: row.Operand = method.Module.ResolveMethod(token); break;
                        case OperandType.InlineType: row.Operand = method.Module.ResolveType(token); break;
                        case OperandType.InlineString: row.Operand = method.Module.ResolveString(token); break;
                        case OperandType.InlineTok: row.Operand = method.Module.ResolveMember(token); break;
                        case OperandType.InlineSig: row.Operand = token; break;
                        default: throw new InvalidOperationException("Unsupported metadata operand.");
                    } break;
            }
            rows.Add(row);
        }
        var generator = new DynamicMethod("UnusedMetadataLabels", typeof(void), Type.EmptyTypes).GetILGenerator();
        var labels = new Dictionary<int, Label>();
        foreach (var row in rows)
        {
            if (row.Code.OperandType == OperandType.InlineBrTarget || row.Code.OperandType == OperandType.ShortInlineBrTarget)
            { int target = (int)row.Operand; if (!labels.ContainsKey(target)) labels.Add(target, generator.DefineLabel()); }
            if (row.Code.OperandType == OperandType.InlineSwitch) foreach (int target in (int[])row.Operand) if (!labels.ContainsKey(target)) labels.Add(target, generator.DefineLabel());
        }
        var instructions = new List<CodeInstruction>(); var byOffset = new Dictionary<int, CodeInstruction>();
        foreach (var row in rows)
        {
            object operand = row.Operand;
            if (row.Code.OperandType == OperandType.InlineBrTarget || row.Code.OperandType == OperandType.ShortInlineBrTarget) operand = labels[(int)operand];
            else if (row.Code.OperandType == OperandType.InlineSwitch) operand = ((int[])operand).Select(x => labels[x]).ToArray();
            var instruction = new CodeInstruction(row.Code, operand); Label label;
            if (labels.TryGetValue(row.Offset, out label)) instruction.labels.Add(label);
            instructions.Add(instruction); byOffset.Add(row.Offset, instruction);
        }
        foreach (var group in body.ExceptionHandlingClauses.Cast<ExceptionHandlingClause>().GroupBy(e => new { e.TryOffset, e.TryLength }))
        {
            AddBlock(byOffset[group.Key.TryOffset], "BeginExceptionBlock", null);
            foreach (var clause in group)
            {
                string kind = clause.Flags == ExceptionHandlingClauseOptions.Clause ? "BeginCatchBlock" :
                    clause.Flags == ExceptionHandlingClauseOptions.Finally ? "BeginFinallyBlock" : clause.Flags == ExceptionHandlingClauseOptions.Fault ? "BeginFaultBlock" : null;
                if (kind == null) throw new InvalidOperationException("Native filter region is outside the selected adapter.");
                AddBlock(byOffset[clause.HandlerOffset], kind, clause.Flags == ExceptionHandlingClauseOptions.Clause ? clause.CatchType : null);
            }
            int end = group.Max(e => e.HandlerOffset + e.HandlerLength);
            if (byOffset.ContainsKey(end)) AddBlock(byOffset[end], "EndExceptionBlock", null);
        }
        return instructions;
    }
    private static void AddBlock(CodeInstruction instruction, string kind, Type caught)
    {
        var list = (System.Collections.IList)instruction.blocks;
        var blockType = list.GetType().GetGenericArguments()[0]; var enumType = blockType.GetField("blockType").FieldType;
        list.Add(Activator.CreateInstance(blockType, new[] { Enum.Parse(enumType, kind), (object)caught }));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var c = new NativeWorkingLoadContract(); var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var ret = typeof(InstalledWorkingLoadMatcherTests).GetMethod(nameof(Returned), flags);
        var receive = typeof(InstalledWorkingLoadMatcherTests).GetMethod(nameof(Receiver), flags);
        int[] additions = { 6, 14, 14, 3, 7 };
        for (int owner = 0; owner < c.Owners.Length; owner++)
        {
            var method = c.Owners[owner]; var before = Hash(method.GetMethodBody().GetILAsByteArray()); var code = Read(method);
            var original = code.ToArray(); var labels = original.Select(x => x.labels.ToArray()).ToArray(); var blocks = original.Select(x => x.blocks.ToArray()).ToArray();
            var result = NativeWorkingLoadRewrite.Rewrite(code, method, c, ret, receive);
            Check(result.Count == original.Length + additions[owner], "exact installed owner insertion count " + owner);
            Check(result.Where(x => original.Contains(x)).SequenceEqual(original), "exact installed original instruction order " + owner);
            Check(original.Select((x, i) => x.labels.SequenceEqual(labels[i]) && x.blocks.SequenceEqual(blocks[i])).All(x => x), "original branch/EH metadata preserved " + owner);
            Check(before == Hash(method.GetMethodBody().GetILAsByteArray()), "installed IL bytes unchanged " + owner);
            Refuses(() => NativeWorkingLoadRewrite.Rewrite(result, method, c, ret, receive), "duplicate instrumentation refuses " + owner);
        }
        var player = Read(c.Owners[1]); int create = player.FindIndex(x => Equals(x.operand, c.CreateSerializer));
        player.RemoveAt(create); Refuses(() => NativeWorkingLoadRewrite.Rewrite(player, c.Owners[1], c, ret, receive), "missing serializer producer refuses");
        player = Read(c.Owners[1]); create = player.FindIndex(x => Equals(x.operand, c.CreateSerializer));
        player.Insert(create, new CodeInstruction(OpCodes.Call, c.CreateSerializer)); Refuses(() => NativeWorkingLoadRewrite.Rewrite(player, c.Owners[1], c, ret, receive), "duplicate serializer producer refuses");
        player = Read(c.Owners[1]); create = player.FindIndex(x => Equals(x.operand, c.CreateSerializer));
        player[create].labels.Add(new DynamicMethod("UnusedBranch", typeof(void), Type.EmptyTypes).GetILGenerator().DefineLabel());
        Refuses(() => NativeWorkingLoadRewrite.Rewrite(player, c.Owners[1], c, ret, receive), "branch label on insertion producer refuses");
        player = Read(c.Owners[1]); create = player.FindIndex(x => Equals(x.operand, c.CreateSerializer)); AddBlock(player[create + 1], "EndExceptionBlock", null);
        Refuses(() => NativeWorkingLoadRewrite.Rewrite(player, c.Owners[1], c, ret, receive), "exception end on insertion successor refuses");
        player = Read(c.Owners[1]); int caught = player.FindIndex(x => Equals(x.operand, c.LogException)); player[caught].blocks.Clear(); AddBlock(player[caught], "BeginFinallyBlock", null);
        Refuses(() => NativeWorkingLoadRewrite.Rewrite(player, c.Owners[1], c, ret, receive), "foreign catch boundary refuses");
        var iterator = Read(c.Owners[0]); iterator.Insert(0, new CodeInstruction(OpCodes.Callvirt, c.Start));
        Refuses(() => NativeWorkingLoadRewrite.Rewrite(iterator, c.Owners[0], c, ret, receive), "unrelated extra native Start producer refuses");
    }
    private static int Main(string[] args)
    {
        try
        {
            var managed = args[0];
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                var leaf = new AssemblyName(e.Name).Name + ".dll";
                foreach (var dir in new[] { managed, Path.Combine(managed, "UnityModManager") }) { var path = Path.Combine(dir, leaf); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            Check(Hash(File.ReadAllBytes(Path.Combine(managed, "Assembly-CSharp.dll"))) == "3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb", "exact installed game metadata bytes");
            Run();
        }
        catch (Exception error) { failed++; Console.WriteLine("FAIL matcher boundary: " + error); }
        Console.WriteLine("WORKING LOAD INSTALLED MATCHER PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
