using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal interface IChunk7MatchedPoolWindow : IDisposable
    {
        bool Ready { get; }
        int Count { get; }
        JObject Complete();
    }

    // The only substituted boundaries in component tests are native observation
    // and the Unity frame sampler. No native input or resource mutation belongs here.
    internal sealed class Chunk7MatchedPoolCollector : IDisposable
    {
        internal const string FileName = "chunk7-matched-pool-measurements.json";
        internal const string Kind = "chunk7-matched-pool-measurements";
        internal const string Contract = "chunk7-matched-resident-pool-v2";
        internal const int WindowsPerState = 3;
        internal static readonly string[] Labels = {
            "resident-0-after-cycle-1-window-1", "resident-0-after-cycle-1-window-2", "resident-0-after-cycle-1-window-3",
            "resident-1-cycle-2-window-1", "resident-1-cycle-2-window-2", "resident-1-cycle-2-window-3",
            "resident-2-cycle-2-window-1", "resident-2-cycle-2-window-2", "resident-2-cycle-2-window-3",
            "resident-0-after-cycle-2-window-1", "resident-0-after-cycle-2-window-2", "resident-0-after-cycle-2-window-3"
        };
        internal static readonly int[] Active = { 0, 0, 0, 1, 1, 1, 2, 2, 2, 0, 0, 0 };
        private readonly Action<int> guard;
        private readonly Func<int, JObject> capture;
        private readonly Action<JObject, JObject> compare;
        private readonly Func<string, IChunk7MatchedPoolWindow> factory;
        private readonly JArray rows = new JArray();
        private IChunk7MatchedPoolWindow window;
        private JObject before, aborted, pendingRow;
        private bool disposed;
        internal bool Complete => rows.Count == Labels.Length && window == null;
        internal int CompletedCount => rows.Count;
        internal bool Released => disposed && window == null;

        internal Chunk7MatchedPoolCollector(Action<int> guard, Func<int, JObject> capture, Action<JObject, JObject> compare, Func<string, IChunk7MatchedPoolWindow> factory)
        {
            this.guard = guard ?? throw new ArgumentNullException(nameof(guard));
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
            this.compare = compare ?? throw new ArgumentNullException(nameof(compare));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        internal void Begin(int index)
        {
            if (disposed || window != null || index != rows.Count || index >= Labels.Length)
                throw new InvalidOperationException("Matched-pool windows must follow their exact one-pass sequence.");
            guard(Active[index]);
            before = capture(Active[index]) ?? throw new InvalidOperationException("Native sample entry is absent.");
            window = factory(Labels[index]) ?? throw new InvalidOperationException("Native sampler was not acquired.");
        }
        internal bool Poll()
        {
            if (disposed || window == null) throw new InvalidOperationException("Matched-pool sampler is not owned.");
            var index = rows.Count;
            guard(Active[index]); // Also checked while warming up; a ready timer cannot excuse changed custody.
            if (pendingRow == null)
            {
                if (!window.Ready) return false;
                var after = capture(Active[index]) ?? throw new InvalidOperationException("Native sample exit is absent.");
                compare(before, after);
                var sample = window.Complete();
                if (sample == null || (string)sample["label"] != Labels[index])
                    throw new InvalidOperationException("Native sampler returned another window.");
                pendingRow = new JObject { ["index"] = index, ["label"] = Labels[index], ["activePairs"] = Active[index],
                    ["before"] = before, ["after"] = after, ["sample"] = sample };
            }
            // A failed Dispose retains the owner and cannot publish an apparently complete row.
            window.Dispose();
            window = null;
            rows.Add(pendingRow);
            before = null; pendingRow = null;
            return true;
        }
        internal JObject Evidence() => new JObject {
            ["contract"] = Contract, ["complete"] = Complete, ["observerReleased"] = Released,
            ["residentBoundaries"] = 2, ["residentParticipants"] = 2, ["warmupNativeCycle"] = 1,
            ["measuredNativeCycle"] = 2, ["additionalNativeInputs"] = 0,
            ["inclusiveUpdateIncludesObserverWork"] = true, ["managedAllocationRateMeasured"] = false,
            ["fullyInertDiagnostics"] = false, ["performanceAccepted"] = false,
            ["rows"] = rows.DeepClone(), ["abortedWindow"] = aborted?.DeepClone(),
            ["limits"] = "Inclusive KMC Update contains native safety/scenario and sampler work. Whole-frame delta is native game/render elapsed time. Managed heap occupancy includes other code and uncollected garbage; it is not allocated bytes, allocation rate or leak proof. This sequence matches resident pools but does not remove time/order or observer overhead." };
        public void Dispose()
        {
            if (disposed) return;
            if (window != null)
            {
                if (aborted == null) aborted = new JObject { ["index"] = rows.Count, ["label"] = Labels[rows.Count], ["sampleFrames"] = window.Count,
                    ["completedRawRow"] = pendingRow?.DeepClone() };
                window.Dispose(); // Do not clear ownership after an external release failure.
                window = null;
            }
            before = null; pendingRow = null; disposed = true;
        }
    }

    // Actor, descriptor, view, world and resident-service references are retained,
    // compared by reference, and only then labeled. Hash collisions refuse.
    internal sealed class Chunk7MatchedPoolPins : IDisposable
    {
        internal const int PinCount = 22;
        private readonly string[] actorIds;
        private object[] values;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(PinCount);
        internal Chunk7MatchedPoolPins(string[] actorIds, object[] values)
        {
            if (actorIds == null || actorIds.Length != 4 || actorIds.Any(string.IsNullOrEmpty) || actorIds.Distinct(StringComparer.Ordinal).Count() != 4 ||
                values == null || values.Length != PinCount || values.Any(value => ReferenceEquals(value, null)))
                throw new InvalidOperationException("Matched-pool admission requires four exact actors and all native identity pins.");
            this.actorIds = (string[])actorIds.Clone(); this.values = (object[])values.Clone();
            foreach (var value in values) identities.Get(value);
            if (identities.RetainedCount != PinCount) throw new InvalidOperationException("Matched-pool native scope aliases distinct ownership roles.");
        }
        internal void Require(string[] actualIds, object[] actual)
        {
            if (values == null || actualIds == null || !actorIds.SequenceEqual(actualIds, StringComparer.Ordinal) || actual == null || actual.Length != values.Length)
                throw new InvalidOperationException("Matched-pool identity scope changed.");
            for (var i = 0; i < values.Length; i++)
                if (!ReferenceEquals(values[i], actual[i])) throw new InvalidOperationException("Matched-pool native reference changed at pin " + i + ".");
        }
        internal JArray Labels => new JArray(values.Select(value => identities.Get(value)));
        internal void Release() => Dispose();
        public void Dispose() { values = null; identities.Dispose(); }
    }
}
