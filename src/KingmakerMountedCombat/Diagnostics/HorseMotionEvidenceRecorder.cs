using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observation only. The scenario owns all commands; this recorder owns only capture evidence.
    internal sealed class HorseMotionEvidenceRecorder : IDisposable
    {
        private const int MaximumFrames = 160;
        private const int MaximumFramesPerPhase = 24;
        private readonly string evidenceRoot;
        private readonly GameMountedRelationshipService relationship;
        private readonly MovementScreenshotCaptureCoordinator captures;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<string, int> phaseCounts = new Dictionary<string, int>();
        private readonly JArray frames = new JArray();
        private readonly JArray failures = new JArray();
        private long nextCaptureMilliseconds;
        private int requested;
        private bool disposed;

        internal HorseMotionEvidenceRecorder(string evidenceRoot, GameMountedRelationshipService relationship, IModLogger logger)
        {
            this.evidenceRoot = Path.Combine(evidenceRoot, "movement-visuals");
            Directory.CreateDirectory(this.evidenceRoot);
            if ((File.GetAttributes(this.evidenceRoot) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Horse motion evidence directory is a reparse point.");
            }
            this.relationship = relationship;
            captures = new MovementScreenshotCaptureCoordinator(Commit, Fail, logger);
        }

        internal void Tick(string phase)
        {
            if (disposed) { return; }
            captures.Pump(Time.frameCount);
            if (phase == null || requested >= MaximumFrames || captures.PendingCount != 0 ||
                clock.ElapsedMilliseconds < nextCaptureMilliseconds) { return; }
            int count;
            phaseCounts.TryGetValue(phase, out count);
            // Reserve the dense sequence for Horse approach/strike/recovery;
            // early rider cooldown waits must not consume all later Bite frames.
            var phaseLimit = phase.Contains("-horse-") ? MaximumFramesPerPhase : 8;
            if (count >= phaseLimit) { return; }
            phaseCounts[phase] = count + 1;
            requested++;
            nextCaptureMilliseconds = clock.ElapsedMilliseconds + 80;
            captures.Enqueue(phase, "horse-motion-" + requested.ToString("D3"), Time.frameCount);
            captures.Pump(Time.frameCount);
        }

        private void Commit(MovementScreenshotCaptureRequest capture, byte[] bytes)
        {
            var name = capture.Milestone + ".png";
            string hash;
            using (var algorithm = SHA256.Create())
            {
                hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            }
            using (var stream = new FileStream(Path.Combine(evidenceRoot, name), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            frames.Add(new JObject
            {
                ["phase"] = capture.Row,
                ["requestedFrame"] = capture.ReadyFrame,
                ["capturedFrame"] = Time.frameCount,
                ["elapsedMilliseconds"] = clock.ElapsedMilliseconds,
                ["capturedAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["relativePath"] = "movement-visuals/" + name,
                ["length"] = bytes.Length,
                ["sha256"] = hash,
                ["presentationAfterRender"] = relationship.CapturePresentationObservation(false)
            });
        }

        private void Fail(MovementScreenshotCaptureRequest capture, string reason)
        {
            failures.Add(new JObject { ["phase"] = capture.Row, ["milestone"] = capture.Milestone, ["reason"] = reason });
        }

        internal JObject Snapshot()
        {
            return new JObject
            {
                ["scope"] = "Read-only gameplay-camera sequence; no pointer, UI-chrome or human visual acceptance.",
                ["requested"] = requested,
                ["frames"] = frames,
                ["failures"] = failures
            };
        }

        public void Dispose()
        {
            if (disposed) { return; }
            captures.Dispose();
            disposed = true;
        }
    }
}
