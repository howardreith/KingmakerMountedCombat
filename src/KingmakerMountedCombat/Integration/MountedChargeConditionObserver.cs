using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    // Observe only the exact Charge fact retained by its lease. Wrappers invoke the
    // same native method once and preserve its exception. The finally scope is
    // necessary with Harmony12: a postfix alone cannot close a throwing callback.
    internal sealed class MountedChargeConditionObserver
    {
        private static readonly List<MountedChargeConditionObserver> owners = new List<MountedChargeConditionObserver>();
        private static readonly FieldInfo CounterField = ResolveCounterField();
        [ThreadStatic] private static Scope current;
        private readonly Buff buff;
        private readonly UnitState state;
        private readonly int ownerThread;
        private AddCondition component;
        private readonly MountedChargeConditionOwnership ownership = new MountedChargeConditionOwnership();

        private sealed class Scope
        {
            internal MountedChargeConditionObserver Owner;
            internal MountedChargeConditionOwnership.Operation Operation;
            internal UnitState State;
            internal UnitCondition Condition;
            internal Buff Source;
            internal bool MarkerCaptured;
        }

        internal MountedChargeConditionObserver(Buff buff, UnitState state)
        {
            this.buff = buff ?? throw new ArgumentNullException(nameof(buff));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            ownerThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (owners.Any(owner => ReferenceEquals(owner.buff, buff)))
                throw new InvalidOperationException("Duplicate charge condition observer.");
            owners.Add(this);
        }

        // Zero operations is possible when activation failed before AddCondition.
        // The lease also proves that activation returned and all listeners/facts
        // are gone, so an unacquired contribution need not be manufactured.
        internal bool Drained => ownership.Drained;

        internal void Release()
        {
            if (!Drained) throw new InvalidOperationException("Live charge condition observation cannot be discarded.");
            owners.Remove(this);
        }

        private static FieldInfo ResolveCounterField()
        {
            var field = typeof(UnitState).GetField("m_Conditions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != 0x040015F9 || field.FieldType != typeof(sbyte[]))
                throw new MissingFieldException(typeof(UnitState).FullName, "m_Conditions");
            return field;
        }

        private static int Counter(UnitState state, UnitCondition condition) => ((sbyte[])CounterField.GetValue(state))[(int)condition];

        internal static void Add(UnitState state, UnitCondition condition, Buff source, AddCondition component) =>
            Invoke(state, condition, source, component, true, () => state.AddCondition(condition, source));

        internal static void Remove(UnitState state, UnitCondition condition, AddCondition component) =>
            Invoke(state, condition, null, component, false, () => state.RemoveCondition(condition));

        private static void Invoke(UnitState state, UnitCondition condition, Buff source, AddCondition component,
            bool addition, Action native)
        {
            var owner = owners.FirstOrDefault(item => ReferenceEquals(item.buff, component?.Fact));
            if (owner == null) { native(); return; }
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != owner.ownerThread)
                owner.ownership.Reject("foreign-condition-thread");
            if (component.GetType() != typeof(AddCondition) || !ReferenceEquals(owner.state, state) ||
                condition != UnitCondition.StealthForbidden || component.Condition != condition ||
                (source != null && !ReferenceEquals(source, owner.buff)) ||
                (!ReferenceEquals(owner.component, null) && !ReferenceEquals(owner.component, component)))
                owner.ownership.Reject("native-condition-identity-changed");
            if (ReferenceEquals(owner.component, null)) owner.component = component;
            var scope = new Scope { Owner = owner, State = state, Condition = condition, Source = source,
                Operation = owner.ownership.Begin(addition, Counter(state, condition)) };
            var previous = current;
            current = scope;
            var returned = false;
            try { native(); returned = true; }
            finally
            {
                current = previous;
                owner.ownership.End(scope.Operation, returned);
            }
        }

        // At this exact native entry the increment/decrement has occurred, and no
        // status callback has yet run. Later foreign calls cannot become our delta.
        internal static void ObserveMutation(UnitState state, UnitCondition condition, Buff source)
        {
            var scope = current;
            if (scope == null || scope.MarkerCaptured || !ReferenceEquals(scope.State, state) ||
                scope.Condition != condition || !ReferenceEquals(scope.Source, source)) return;
            scope.MarkerCaptured = true;
            scope.Owner.ownership.Observe(scope.Operation, Counter(state, condition));
        }

        internal static IEnumerable<CodeInstruction> Wrap(IEnumerable<CodeInstruction> source, MethodBase original)
        {
            var add = original.MetadataToken == 0x06002448 || original.MetadataToken == 0x0600244A;
            if (original.DeclaringType != typeof(AddCondition) || (!add && original.MetadataToken != 0x06002449))
                throw new InvalidOperationException("Unpinned charge condition observation call site.");
            var code = source.ToList();
            var token = add ? 0x06001FB7 : 0x06001FB9;
            var sites = code.Where(instruction => instruction.operand is MethodInfo method &&
                method.DeclaringType == typeof(UnitState) && method.MetadataToken == token).ToArray();
            if (sites.Length != 1 || sites[0].opcode != OpCodes.Callvirt)
                throw new InvalidOperationException("Native condition callback no longer has exactly one expected call.");
            var call = sites[0];
            var instance = new CodeInstruction(OpCodes.Ldarg_0);
            instance.labels.AddRange(call.labels); call.labels.Clear();
            instance.blocks.AddRange(call.blocks); call.blocks.Clear();
            code.Insert(code.IndexOf(call), instance);
            call.opcode = OpCodes.Call;
            call.operand = typeof(MountedChargeConditionObserver).GetMethod(add ? nameof(Add) : nameof(Remove),
                BindingFlags.Static | BindingFlags.NonPublic);
            return code;
        }

        internal JObject CaptureEvidence() => new JObject
        {
            ["componentObserved"] = !ReferenceEquals(component, null),
            ["condition"] = (int)UnitCondition.StealthForbidden,
            ["additions"] = ownership.Additions,
            ["removals"] = ownership.Removals,
            ["contributions"] = ownership.Contributions,
            ["nativeExceptions"] = ownership.NativeExceptions,
            ["fault"] = ownership.Fault,
            ["drained"] = Drained,
            ["operations"] = new JArray(ownership.Operations.Select(operation => new JObject
            {
                ["addition"] = operation.IsAddition, ["before"] = operation.BeforeValue,
                ["after"] = operation.AfterValue, ["mutationObserved"] = operation.MutationObserved,
                ["completed"] = operation.Completed, ["returnedNormally"] = operation.ReturnedNormally
            }))
        };
    }
}
