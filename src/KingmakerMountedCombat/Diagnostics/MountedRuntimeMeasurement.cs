using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A bounded, explicitly installed observer. Ordinary play has no observer,
    // sampling buffers, timers or log counters. Heap change is not allocation rate.
    internal sealed class MountedRuntimeMeasurement : IDisposable
    {
        internal const int WarmupFrames = 30;
        internal const int SampleFrames = 120;
        private static volatile MountedRuntimeMeasurement current;
        internal static MountedRuntimeMeasurement Current => current;
        private readonly MountedWorldService world;
        private readonly string label;
        private readonly long[] serviceTicks = new long[SampleFrames];
        private readonly float[] frameSeconds = new float[SampleFrames];
        private readonly JObject before;
        private readonly JObject subscriptionsBefore;
        private int warmup, count, startFrame;
        private long heapBefore, messages, characters;
        private int[] collectionsBefore;
        private bool disposed;
        internal bool Ready => count == SampleFrames;
        internal int Count => count;

        internal MountedRuntimeMeasurement(string label, MountedWorldService world)
        {
            if (current != null || world == null || MountedOptionalDiagnosticsWindow.IsActive)
                throw new InvalidOperationException("Runtime measurement already owned, optional capture is suppressed, or world missing.");
            this.label = label ?? throw new ArgumentNullException(nameof(label)); this.world = world;
            before = CaptureOwners(world); subscriptionsBefore = Chunk4SubscriptionSnapshot.Capture();
            current = this;
        }
        internal long BeginFrame() => disposed || Ready ? 0 : Stopwatch.GetTimestamp();
        internal void EndFrame(long started)
        {
            if (disposed || Ready || started == 0) return;
            if (warmup++ < WarmupFrames) return;
            if (count == 0)
            {
                startFrame = Time.frameCount; heapBefore = GC.GetTotalMemory(false);
                collectionsBefore = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                Interlocked.Exchange(ref messages, 0); Interlocked.Exchange(ref characters, 0);
            }
            serviceTicks[count] = Stopwatch.GetTimestamp() - started;
            frameSeconds[count] = Time.unscaledDeltaTime;
            count++;
        }
        internal static void ObserveLog(string message)
        {
            var active = current;
            if (active == null || active.disposed || active.Ready || active.count == 0) return;
            Interlocked.Increment(ref active.messages);
            Interlocked.Add(ref active.characters, message?.Length ?? 0);
        }
        internal JObject Complete()
        {
            if (!Ready || disposed) throw new InvalidOperationException("Runtime sample did not reach its complete bounded window.");
            var heapAfter = GC.GetTotalMemory(false);
            var collectionsAfter = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
            Dispose();
            var ticks = serviceTicks.OrderBy(value => value).ToArray();
            var frames = frameSeconds.OrderBy(value => value).ToArray();
            return new JObject {
                ["label"] = label, ["warmupFrames"] = WarmupFrames, ["sampleFrames"] = count,
                ["startFrame"] = startFrame, ["endFrame"] = Time.frameCount,
                ["stopwatchFrequency"] = Stopwatch.Frequency,
                ["serviceTicks"] = new JArray(serviceTicks), ["frameSeconds"] = new JArray(frameSeconds),
                ["serviceMeanMs"] = serviceTicks.Average() * 1000d / Stopwatch.Frequency,
                ["serviceP50Ms"] = ticks[(count - 1) / 2] * 1000d / Stopwatch.Frequency,
                ["serviceP95Ms"] = ticks[(int)Math.Ceiling(count * .95d) - 1] * 1000d / Stopwatch.Frequency,
                ["frameP50Ms"] = frames[(count - 1) / 2] * 1000d,
                ["frameP95Ms"] = frames[(int)Math.Ceiling(count * .95d) - 1] * 1000d,
                ["managedHeapBefore"] = heapBefore, ["managedHeapAfter"] = heapAfter,
                ["managedHeapDelta"] = heapAfter - heapBefore, ["forcedCollection"] = false,
                ["collectionsBefore"] = new JArray(collectionsBefore), ["collectionsAfter"] = new JArray(collectionsAfter),
                ["kmcLogMessages"] = Interlocked.Read(ref messages), ["kmcLogCharacters"] = Interlocked.Read(ref characters),
                ["ownersBefore"] = before, ["ownersAfter"] = CaptureOwners(world),
                ["subscriptionsBefore"] = subscriptionsBefore, ["subscriptionsAfter"] = Chunk4SubscriptionSnapshot.Capture(),
                ["limits"] = "Main-thread KMC Update includes observer overhead; frame delta includes native game/rendering. Managed heap delta is process-wide retained memory, not allocated bytes. No GC is forced. Other game/loader logs are excluded."
            };
        }
        private static JObject CaptureOwners(MountedWorldService world) => new JObject {
            ["boundaries"] = world.BoundaryCount, ["failedConstructions"] = world.FailedConstructionCount,
            ["owners"] = world.Registry.Owners.Length,
            ["active"] = world.Registry.ActiveCount, ["allocationActors"] = world.UnifiedTurn.TrackedAllocationActorCount,
            ["participants"] = world.UnifiedTurn.SnapshotParticipants.Length,
            ["preparedOrViewResidue"] = world.Pairs.Count(pair => !pair.Relationship.Runtime.NoPreparedPairResidue),
            ["liveControlBoundaries"] = world.Pairs.Count(pair => world.NativeControls?.HasUnsettledShellFor(pair.Relationship) == true)
        };
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ReferenceEquals(current, this)) current = null;
        }
    }
}
