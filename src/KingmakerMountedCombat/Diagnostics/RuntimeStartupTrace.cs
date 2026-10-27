using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    // Only an accepted automation host creates this recorder. It never retains game
    // objects, writes files, dispatches actions, or changes the host deadline.
    internal sealed class RuntimeStartupTrace
    {
        internal const int CallbackCapacity = 10;
        internal const int SampleCapacity = CallbackCapacity * 5;
        private readonly long frequency;
        private readonly Func<long> elapsedTicks;
        private readonly Func<int> unityFrame;
        private readonly Func<bool> isFocused, runInBackground;
        private readonly RuntimeStartupTraceSample[] samples = new RuntimeStartupTraceSample[SampleCapacity];
        private int count, callbacks, closed, nextPhase;
        private bool inCallback, orderingFailure, readFailure;
        private long lastTicks;

        internal RuntimeStartupTrace(long frequency, Func<long> elapsedTicks, Func<int> unityFrame,
            Func<bool> isFocused, Func<bool> runInBackground)
        {
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            this.frequency = frequency;
            this.elapsedTicks = elapsedTicks ?? throw new ArgumentNullException(nameof(elapsedTicks));
            this.unityFrame = unityFrame ?? throw new ArgumentNullException(nameof(unityFrame));
            this.isFocused = isFocused ?? throw new ArgumentNullException(nameof(isFocused));
            this.runInBackground = runInBackground ?? throw new ArgumentNullException(nameof(runInBackground));
        }

        internal void BeginCallback()
        {
            if (orderingFailure || callbacks == CallbackCapacity && !inCallback) return;
            if (inCallback) { orderingFailure = true; return; }
            callbacks++;
            inCallback = true;
            nextPhase = 1;
            Add("on-update-entry");
        }

        internal void PreHostStart() { Advance(1, "pre-host-start"); }
        internal void PreHostEnd() { Advance(2, "pre-host-end"); }
        internal void HostEntry() { Advance(3, "host-entry"); }

        internal void EndCallback()
        {
            if (orderingFailure || callbacks == CallbackCapacity && !inCallback) return;
            if (!inCallback) { orderingFailure = true; return; }
            // The existing native catch may have left only a prefix. Closing that
            // callback does not invent the phases that never ran.
            Add("on-update-exit");
            closed++;
            inCallback = false;
        }

        private void Advance(int expected, string phase)
        {
            if (orderingFailure || callbacks == CallbackCapacity && !inCallback) return;
            if (!inCallback || nextPhase != expected) { orderingFailure = true; return; }
            Add(phase);
            nextPhase++;
        }

        private void Add(string phase)
        {
            long? ticks = null;
            int? frame = null;
            bool? focused = null, background = null;
            try
            {
                var observed = elapsedTicks();
                if (observed < 0 || observed < lastTicks) readFailure = true;
                else { ticks = observed; lastTicks = observed; }
            }
            catch (Exception) { readFailure = true; }
            try
            {
                var observed = unityFrame();
                if (observed < 0) readFailure = true;
                else frame = observed;
            }
            catch (Exception) { readFailure = true; }
            try { focused = isFocused(); } catch (Exception) { readFailure = true; }
            try { background = runInBackground(); } catch (Exception) { readFailure = true; }
            samples[count++] = new RuntimeStartupTraceSample(callbacks, phase, ticks, frame, focused, background);
        }

        internal RuntimeStartupTraceSnapshot Capture()
        {
            var copy = new RuntimeStartupTraceSample[count];
            Array.Copy(samples, copy, count);
            return new RuntimeStartupTraceSnapshot(frequency, callbacks, closed,
                orderingFailure, readFailure, copy);
        }
    }

    internal struct RuntimeStartupTraceSample
    {
        internal RuntimeStartupTraceSample(int callback, string phase, long? elapsedTicks, int? unityFrame,
            bool? isFocused, bool? runInBackground)
        {
            Callback = callback; Phase = phase; ElapsedTicks = elapsedTicks; UnityFrame = unityFrame;
            IsFocused = isFocused; RunInBackground = runInBackground;
        }
        public int Callback { get; private set; }
        public string Phase { get; private set; }
        public long? ElapsedTicks { get; private set; }
        public int? UnityFrame { get; private set; }
        public bool? IsFocused { get; private set; }
        public bool? RunInBackground { get; private set; }
    }

    internal sealed class RuntimeStartupTraceSnapshot
    {
        internal RuntimeStartupTraceSnapshot(long frequency, int callbacks, int closed,
            bool orderingFailure, bool readFailure, IReadOnlyList<RuntimeStartupTraceSample> samples)
        {
            ClockFrequency = frequency; CallbacksObserved = callbacks; ClosedCallbacks = closed;
            OrderingFailure = orderingFailure; ReadFailure = readFailure; Samples = samples;
        }
        public int SchemaVersion => 1;
        public string Kind => "runtime-startup-callback-trace";
        public string ClockOrigin => "accepted-runtime-host";
        public int CallbackCapacity => RuntimeStartupTrace.CallbackCapacity;
        public int SampleCapacity => RuntimeStartupTrace.SampleCapacity;
        public long ClockFrequency { get; private set; }
        public int CallbacksObserved { get; private set; }
        public int ClosedCallbacks { get; private set; }
        public bool OrderingFailure { get; private set; }
        public bool ReadFailure { get; private set; }
        public IReadOnlyList<RuntimeStartupTraceSample> Samples { get; private set; }
    }
}
