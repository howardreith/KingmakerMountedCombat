using System;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owned only by the selected Chunk 7 native case. This suppresses two optional
    // captures; the host heartbeat, exact-world guards and production records remain.
    internal sealed class MountedOptionalDiagnosticsWindow : IDisposable
    {
        internal const int MinimumFrames = 12;
        internal const int MaximumFrames = 1800;
        internal const double MinimumSeconds = 0.25;
        internal const double MaximumSeconds = 15.0;
        private static volatile MountedOptionalDiagnosticsWindow current;
        private readonly MovementTelemetryWriter writer;
        private readonly string label;
        private JObject before;
        private int startFrame;
        private double startSeconds, requiredSeconds;
        private bool installed, disposed;

        internal MountedOptionalDiagnosticsWindow(MovementTelemetryWriter writer, string label)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.label = label ?? throw new ArgumentNullException(nameof(label));
        }
        internal static bool IsActive => current != null;
        internal static bool Suppresses(MovementTelemetryWriter writer)
        {
            var active = current; return active != null && ReferenceEquals(active.writer, writer);
        }
        internal void Install(int frame, double seconds)
        {
            if (installed || disposed || current != null || MountedRuntimeMeasurement.Current != null)
                throw new InvalidOperationException("Optional-capture quiet window needs absent prior window and measurement ownership.");
            ValidateClock(frame, seconds);
            // Capture every fallible observation before publishing the gate. The
            // caller already owns this object if acquisition/capture throws.
            var captured = CaptureWriter();
            var interval = (double)captured["samplingIntervalSeconds"];
            if (double.IsNaN(interval) || double.IsInfinity(interval) || interval <= 0 || interval > 1)
                throw new InvalidOperationException("Optional telemetry has an invalid captured sampling interval.");
            requiredSeconds = Math.Max(MinimumSeconds, interval * 2);
            startFrame = frame; startSeconds = seconds; before = captured;
            installed = true; current = this;
        }
        internal bool Ready(int frame, double seconds)
        {
            RequireOwned(); ValidateClock(frame, seconds);
            var frames = frame - startFrame; var elapsed = seconds - startSeconds;
            if (frames < 0 || elapsed < 0 || frames > MaximumFrames || elapsed > MaximumSeconds)
                throw new InvalidOperationException("Optional-capture quiet window escaped its bounded native interval.");
            return frames >= MinimumFrames && elapsed >= requiredSeconds;
        }
        internal JObject Complete(int frame, double seconds)
        {
            if (!Ready(frame, seconds)) throw new InvalidOperationException("Optional-capture quiet window is not yet a bounded multi-frame observation.");
            var after = CaptureWriter();
            if ((double)before["lastSampleSeconds"] != (double)after["lastSampleSeconds"] ||
                (long)before["sampleSequence"] != (long)after["sampleSequence"] ||
                (bool)before["fileCreated"] != (bool)after["fileCreated"] ||
                (double)before["samplingIntervalSeconds"] != (double)after["samplingIntervalSeconds"] ||
                (double)after["clockSeconds"] - (double)before["clockSeconds"] < requiredSeconds)
                throw new InvalidOperationException("Optional telemetry advanced or did not span its real sampling clock while gated.");
            var result = new JObject {
                ["contract"] = "chunk7-optional-capture-quiet-window-v1", ["label"] = label,
                ["startFrame"] = startFrame, ["endFrame"] = frame, ["startSeconds"] = startSeconds, ["endSeconds"] = seconds,
                ["minimumFrames"] = MinimumFrames, ["maximumFrames"] = MaximumFrames,
                ["minimumSeconds"] = MinimumSeconds, ["maximumSeconds"] = MaximumSeconds,
                ["telemetryBefore"] = before, ["telemetryAfter"] = after,
                ["measurementBeforePresent"] = false, ["measurementAfterPresent"] = false,
                ["fullyInert"] = false, ["performanceProof"] = false, ["allocationRateProof"] = false,
                ["remainingObservers"] = new JArray("guarded-host-heartbeat", "exact-native-world-guard", "production-lifecycle-records", "native-command-settlement-polling", "entry-exit-snapshots")
            };
            Dispose(); result["gateReleased"] = true;
            return result;
        }
        private JObject CaptureWriter() => new JObject {
            ["lastSampleSeconds"] = writer.LastSampleSeconds, ["sampleSequence"] = writer.SampleSequence,
            ["fileCreated"] = writer.HasCreatedTelemetry, ["samplingIntervalSeconds"] = writer.SamplingIntervalSeconds,
            ["clockSeconds"] = writer.SamplingClockSeconds
        };
        private void RequireOwned()
        {
            if (!installed || disposed || !ReferenceEquals(current, this) || MountedRuntimeMeasurement.Current != null)
                throw new InvalidOperationException("Optional-capture quiet window lost exact ownership or admitted a measurement observer.");
        }
        private static void ValidateClock(int frame, double seconds)
        {
            if (frame < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new InvalidOperationException("Optional-capture quiet window clock is invalid.");
        }
        public void Dispose()
        {
            if (disposed) return;
            if (installed)
            {
                if (!ReferenceEquals(current, this)) throw new InvalidOperationException("Optional-capture cleanup refuses another window's gate.");
                current = null;
            }
            disposed = true;
        }
    }
}
