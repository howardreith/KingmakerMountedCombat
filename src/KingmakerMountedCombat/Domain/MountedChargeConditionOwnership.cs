using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // An observation ledger for one exact native AddCondition component. The native
    // counter is shared with foreign effects: only the delta at this component's
    // own mutation boundary can acquire or discharge a contribution. Never write
    // the counter or retry a decrement to repair an ambiguous observation.
    public sealed class MountedChargeConditionOwnership
    {
        public sealed class Operation
        {
            internal bool Addition;
            internal int Before;
            internal bool Observed;
            internal bool Ended;
            public bool IsAddition => Addition;
            public int BeforeValue => Before;
            public int? AfterValue { get; internal set; }
            public bool MutationObserved => Observed;
            public bool Completed => Ended;
            public bool ReturnedNormally { get; internal set; }
        }

        private Operation active;
        private readonly List<Operation> operations = new List<Operation>();
        public IReadOnlyList<Operation> Operations => operations;
        public int Additions { get; private set; }
        public int Removals { get; private set; }
        public int NativeExceptions { get; private set; }
        public int Contributions { get; private set; }
        public string Fault { get; private set; }
        public bool Drained => Fault == null && active == null && Contributions == 0;

        public Operation Begin(bool addition, int before)
        {
            if (active != null) Reject("reentrant-component-mutation");
            if (before < 0 || before > sbyte.MaxValue) Reject("invalid-native-counter");
            var operation = new Operation { Addition = addition, Before = before };
            if (operations.Count < 16) operations.Add(operation);
            else Reject("condition-observation-bound-exceeded");
            active = operation;
            return operation;
        }

        public void Observe(Operation operation, int after)
        {
            if (operation == null || !ReferenceEquals(operation, active) || operation.Ended || operation.Observed)
            { Reject("ambiguous-mutation-marker"); return; }
            operation.Observed = true;
            operation.AfterValue = after;
            if (operation.Before < 0 || operation.Before > sbyte.MaxValue || after < 0 || after > sbyte.MaxValue ||
                (operation.Addition ? operation.Before == sbyte.MaxValue || after != operation.Before + 1 :
                    operation.Before <= 0 || after != operation.Before - 1))
            { Reject("unproven-native-delta"); return; }
            if (operation.Addition) { Additions++; Contributions++; }
            else if (Contributions > 0) { Removals++; Contributions--; }
            else Reject("removal-without-owned-contribution");
        }

        public void End(Operation operation, bool returnedNormally)
        {
            if (!returnedNormally) NativeExceptions++;
            if (operation == null || !ReferenceEquals(operation, active) || operation.Ended)
            { Reject("ambiguous-operation-completion"); return; }
            operation.Ended = true;
            operation.ReturnedNormally = returnedNormally;
            if (!operation.Observed) Reject("native-mutation-not-observed");
            active = null;
        }

        public void Reject(string reason)
        {
            if (Fault == null) Fault = reason;
        }
    }
}
