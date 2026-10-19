using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum WorkingLoadNotice
    {
        LoaderStored, TaskStored, PlayerSerializer, PlayerReturned, PlayerStored, PlayerCaught,
        PartySerializer, PartyReturned, PartyStored, PartyCaught, CrossSceneStored, SceneBefore, SceneAfter
    }
    // Every original CodeInstruction instance remains in order. The only accepted
    // EH boundary is the pinned native Exception catch/log instruction, whose
    // BeginCatch metadata stays on that original instruction before our call.
    internal static class NativeWorkingLoadRewrite
    {
        internal static List<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> incoming, MethodBase owner,
            NativeWorkingLoadContract c, MethodInfo returnHook, MethodInfo receiverHook)
        {
            NativeWorkingLoadContract.Require(incoming != null && c != null && returnHook != null && receiverHook != null, "Native rewrite lacks bindings.");
            var code = incoming.Take(2049).ToList();
            NativeWorkingLoadContract.Require(code.Count > 0 && code.Count <= 2048 && code.All(x => x != null) &&
                !code.Any(x => Equals(x.operand, returnHook) || Equals(x.operand, receiverHook)), "Invalid or already observed native body.");
            NativeWorkingLoadContract.Require(returnHook.IsStatic && returnHook.ReturnType == typeof(void) &&
                returnHook.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(object), typeof(object), typeof(int) }) &&
                receiverHook.IsStatic && receiverHook.ReturnType == typeof(void) &&
                receiverHook.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(object), typeof(int) }), "Notification stack signature changed.");
            var add = new Dictionary<int, List<CodeInstruction>>();
            Action<int, WorkingLoadNotice, bool, bool> point = (at, kind, returns, catchLog) => {
                NativeWorkingLoadContract.Require(at >= 0 && at + 1 < code.Count && !add.ContainsKey(at), "Invalid native insertion site.");
                NativeWorkingLoadContract.Require(code[at].labels.Count == 0 && (code[at + 1].labels.Count == 0 ||
                    kind == WorkingLoadNotice.CrossSceneStored && ExactCrossSceneSkip(code, at, c)) &&
                    (code[at + 1].blocks.Count == 0 || catchLog && ExactEndBlock(code[at + 1])) &&
                    (code[at].blocks.Count == 0 || catchLog && ExactCatchBlock(code[at])),
                    "Unsupported native branch/EH boundary at " + kind + " labels=" + code[at].labels.Count + "/" + code[at + 1].labels.Count +
                    " blocks=" + code[at].blocks.Count + "/" + code[at + 1].blocks.Count + ".");
                var steps = new List<CodeInstruction>();
                if (returns) steps.Add(new CodeInstruction(OpCodes.Dup));
                steps.Add(new CodeInstruction(OpCodes.Ldarg_0));
                steps.Add(new CodeInstruction(OpCodes.Ldc_I4, (int)kind));
                steps.Add(new CodeInstruction(OpCodes.Call, returns ? returnHook : receiverHook));
                add.Add(at, steps);
            };
            if (Equals(owner, c.Owners[0]))
            {
                // The same fields are cleared later by native cleanup. Find the
                // sole stores immediately sourced by constructor / Start instead.
                int loader = StoreAfter(code, c.IteratorLoader, OpCodes.Newobj, c.LoaderConstructor);
                int task = StoreAfter(code, c.IteratorTask, OpCodes.Callvirt, c.Start);
                NativeWorkingLoadContract.Require(loader < task, "Native task precedes loader acquisition.");
                point(loader, WorkingLoadNotice.LoaderStored, false, false); point(task, WorkingLoadNotice.TaskStored, false, false);
            }
            else if (Equals(owner, c.Owners[1]) || Equals(owner, c.Owners[2]))
            {
                bool player = Equals(owner, c.Owners[1]); var deserialize = player ? c.DeserializePlayer : c.DeserializeParty;
                int serializer = One(code, OpCodes.Call, c.CreateSerializer), returned = One(code, OpCodes.Callvirt, deserialize);
                var storedField = player ? c.PlayerState : c.PartyResult;
                int stored = One(code, OpCodes.Stfld, storedField), caught = One(code, OpCodes.Call, c.LogException);
                NativeWorkingLoadContract.Require(serializer < returned && stored == returned + 1 && stored < caught, "Native callback producer/store/log order changed.");
                point(serializer, player ? WorkingLoadNotice.PlayerSerializer : WorkingLoadNotice.PartySerializer, true, false);
                point(returned, player ? WorkingLoadNotice.PlayerReturned : WorkingLoadNotice.PartyReturned, true, false);
                point(stored, player ? WorkingLoadNotice.PlayerStored : WorkingLoadNotice.PartyStored, false, false);
                point(caught, player ? WorkingLoadNotice.PlayerCaught : WorkingLoadNotice.PartyCaught, false, true);
            }
            else if (Equals(owner, c.Owners[3])) point(One(code, OpCodes.Stfld, c.CrossScene), WorkingLoadNotice.CrossSceneStored, false, false);
            else if (Equals(owner, c.Owners[4]))
            {
                int call = One(code, OpCodes.Callvirt, c.ScenePostLoad);
                NativeWorkingLoadContract.Require(call > 0 && code[call - 1].opcode == OpCodes.Ldfld && Equals(code[call - 1].operand, c.CrossScene), "Native scene receiver changed.");
                point(call - 1, WorkingLoadNotice.SceneBefore, true, false); point(call, WorkingLoadNotice.SceneAfter, false, false);
            }
            else throw new InvalidOperationException("Unpinned native observer owner.");
            var result = new List<CodeInstruction>(code.Count + add.Sum(p => p.Value.Count));
            for (int i = 0; i < code.Count; i++) { result.Add(code[i]); List<CodeInstruction> inserted; if (add.TryGetValue(i, out inserted)) result.AddRange(inserted); }
            return result;
        }
        private static int One(List<CodeInstruction> code, OpCode opcode, object operand)
        {
            var sites = code.Select((value, index) => new { value, index }).Where(x => Equals(x.value.operand, operand)).ToArray();
            NativeWorkingLoadContract.Require(sites.Length == 1 && sites[0].value.opcode == opcode, "Native call/field site is not unique and exact.");
            return sites[0].index;
        }
        private static int StoreAfter(List<CodeInstruction> code, FieldInfo field, OpCode producerCode, MethodBase producer)
        {
            int produced = One(code, producerCode, producer);
            var sites = code.Select((value, index) => new { value, index }).Where(x => x.value.opcode == OpCodes.Stfld && Equals(x.value.operand, field) &&
                x.index == produced + 1).ToArray();
            NativeWorkingLoadContract.Require(sites.Length == 1, "Native original producer/store pair changed."); return sites[0].index;
        }
        private static bool ExactCrossSceneSkip(List<CodeInstruction> code, int at, NativeWorkingLoadContract c)
        {
            if (at < 8 || code[at + 1].labels.Count != 1 ||
                !(code[at + 1].opcode == OpCodes.Leave_S || code[at + 1].opcode == OpCodes.Leave)) return false;
            var singleton = code[at - 5].operand as MethodInfo;
            return code[at - 8].opcode == OpCodes.Ldloc_1 && code[at - 7].opcode == OpCodes.Ldfld && Equals(code[at - 7].operand, c.PartyResult) &&
                (code[at - 6].opcode == OpCodes.Brfalse_S || code[at - 6].opcode == OpCodes.Brfalse) && Equals(code[at - 6].operand, code[at + 1].labels[0]) &&
                code[at - 5].opcode == OpCodes.Call && singleton != null && singleton.Module.ModuleVersionId == NativeWorkingLoadContract.GameMvid &&
                singleton.DeclaringType == c.GameState.DeclaringType && singleton.Name == "get_Instance" && singleton.IsStatic && singleton.ReturnType == c.GameState.DeclaringType && singleton.GetParameters().Length == 0 &&
                code[at - 4].opcode == OpCodes.Ldfld && Equals(code[at - 4].operand, c.GameState) &&
                code[at - 3].opcode == OpCodes.Ldfld && Equals(code[at - 3].operand, c.PlayerState) &&
                code[at - 2].opcode == OpCodes.Ldloc_1 && code[at - 1].opcode == OpCodes.Ldfld && Equals(code[at - 1].operand, c.PartyResult) &&
                code.Skip(at - 8).Take(8).All(x => x.labels.Count == 0 && x.blocks.Count == 0);
        }
        private static bool ExactCatchBlock(CodeInstruction instruction)
        {
            if (instruction.blocks.Count != 1) return false;
            // Harmony12's installed shim exposes the underlying block metadata;
            // reflection here is installation-only, never a game callback.
            object block = instruction.blocks[0]; var type = block.GetType();
            var kind = type.GetField("blockType"); var caught = type.GetField("catchType");
            return kind != null && caught != null && kind.GetValue(block).ToString() == "BeginCatchBlock" && Equals(caught.GetValue(block), typeof(Exception));
        }
        private static bool ExactEndBlock(CodeInstruction instruction)
        {
            if (instruction.blocks.Count != 1) return false;
            object block = instruction.blocks[0]; var kind = block.GetType().GetField("blockType");
            return kind != null && kind.GetValue(block).ToString() == "EndExceptionBlock";
        }
    }
}
