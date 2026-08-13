using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class MovementTelemetryWriter : IDisposable
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly string path;
        private readonly string scenario;
        private readonly string runId;
        private readonly string branch;
        private readonly string commit;
        private readonly string productVersion;
        private readonly string dllSha256;
        private readonly string dllMvid;
        private readonly KingmakerMountedPairRuntime runtime;
        private readonly Func<string> relationshipState;
        private readonly double intervalSeconds;
        private bool created;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastSampleSeconds;
        private long sequence;

        public MovementTelemetryWriter(RuntimeRequest request, KingmakerMountedPairRuntime runtime, Func<string> relationshipState, double intervalSeconds)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }
            scenario = request.Scenario;
            runId = request.RunId;
            branch = request.Branch;
            commit = request.Commit;
            productVersion = request.ProductVersion;
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.relationshipState = relationshipState ?? throw new ArgumentNullException(nameof(relationshipState));
            this.intervalSeconds = intervalSeconds;
            var assembly = typeof(Main).Assembly;
            dllSha256 = ComputeSha256(assembly.Location);
            dllMvid = assembly.ManifestModule.ModuleVersionId.ToString();
            path = Path.Combine(request.EvidenceRoot, "movement-telemetry.jsonl");
        }

        public void Update(float deltaTime)
        {
            var nowSeconds = clock.Elapsed.TotalSeconds;
            if (nowSeconds - lastSampleSeconds < intervalSeconds) { return; }
            lastSampleSeconds = nowSeconds;
            var rider = runtime.Rider;
            var mount = runtime.Mount;
            var agent = runtime.MovementAgent;
            if (rider == null || mount == null || agent == null || !agent.IsConfigured) { return; }
            var expected = agent.ExpectedPosition;
            var expectedRotation = agent.ExpectedRotation;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var mountAgent = mount.View?.AgentASP;
            var mountPath = mountAgent?.Path;
            var move = mount.Commands?.Move;
            var sample = new
            {
                schemaVersion = 1,
                scenario,
                runId,
                branch,
                commit,
                productVersion,
                dllSha256,
                dllMvid,
                sequence = sequence++,
                utcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                riderId = rider.UniqueId,
                mountId = mount.UniqueId,
                relationshipState = relationshipState(),
                combat = rider.IsInCombat || mount.IsInCombat,
                partyCombat = Game.Instance?.Player?.IsInCombat,
                currentGameMode = Game.Instance == null ? null : Game.Instance.CurrentMode.ToString(),
                paused = Game.Instance?.IsPaused,
                turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                authoritativeMover = "mount",
                requestedDestination = move == null ? (UnityEngine.Vector3?)null : move.Target,
                riderStockAgentEnabled = rider.View?.AgentASP?.enabled,
                mountStockAgentEnabled = mount.View?.AgentASP?.enabled,
                riderAvoidanceDisabled = rider.View?.AgentASP?.AvoidanceDisabled,
                mountAvoidanceDisabled = mount.View?.AgentASP?.AvoidanceDisabled,
                riderEntityPosition = rider.Position,
                mountEntityPosition = mount.Position,
                riderEntityOrientation = rider.Orientation,
                mountEntityOrientation = mount.Orientation,
                riderViewPosition = rider.View?.transform.position,
                mountViewPosition = mount.View?.transform.position,
                riderViewRotation = rider.View?.transform.rotation.eulerAngles,
                mountViewRotation = mount.View?.transform.rotation.eulerAngles,
                anchor = agent.AnchorName,
                expectedAnchorPosition = expected,
                expectedAnchorRotation = expectedRotation.eulerAngles,
                residualPositionWorldUnits = Domain.MovementTelemetrySample.CalculateDistance(expected.x, expected.y, expected.z, rider.View.transform.position.x, rider.View.transform.position.y, rider.View.transform.position.z),
                residualRotationDegrees = UnityEngine.Quaternion.Angle(expectedRotation, rider.View.transform.rotation),
                riderSelected = selected != null && selected.Contains(rider),
                mountSelected = selected != null && selected.Contains(mount),
                selectedUnitIds = selected == null ? new string[0] : selected.Where(unit => unit != null).Select(unit => unit.UniqueId).ToArray(),
                riderCommandCount = rider.Commands?.Raw == null ? (int?)null : rider.Commands.Raw.Length,
                mountCommandCount = mount.Commands?.Raw == null ? (int?)null : mount.Commands.Raw.Length,
                riderActiveCommandTypes = rider.Commands?.Raw == null ? new string[0] : rider.Commands.Raw.Where(command => command != null).Select(command => command.GetType().FullName).ToArray(),
                mountActiveCommandTypes = mount.Commands?.Raw == null ? new string[0] : mount.Commands.Raw.Where(command => command != null).Select(command => command.GetType().FullName).ToArray(),
                mountIsReallyMoving = mountAgent?.IsReallyMoving,
                mountVelocity = mountAgent?.Velocity,
                mountSpeed = mountAgent?.Speed,
                mountMoveDirection = mountAgent?.MoveDirection,
                mountPathId = mountPath == null ? (uint?)null : mountPath.pathID,
                mountPathFailed = mountAgent?.PathFailed,
                mountRepathNeeded = mountAgent?.RepathNeeded,
                mountPathError = mountPath?.error,
                mountPathErrorLog = mountPath?.errorLog,
                mountPathPointCount = mountPath?.vectorPath?.Count,
                mountPathLength = mountPath == null ? (float?)null : mountPath.GetTotalLength(),
                synchronizationPhase = agent.LatestSynchronizationPhase.ToString(),
                synchronizationSampleCount = agent.SampleCount,
                synchronizationCorrectionCount = agent.CorrectionCount,
                initialConfigurationSynchronizationSampleCount = agent.InitialConfigurationSampleCount,
                initialConfigurationSynchronizationCorrectionCount = agent.InitialConfigurationCorrectionCount,
                updateSynchronizationSampleCount = agent.UpdateSampleCount,
                updateSynchronizationCorrectionCount = agent.UpdateCorrectionCount,
                lateUpdateSynchronizationSampleCount = agent.LateUpdateSampleCount,
                lateUpdateSynchronizationCorrectionCount = agent.LateUpdateCorrectionCount,
                preCorrectionPositionResidualWorldUnits = agent.LatestPreCorrectionPositionResidualWorldUnits,
                preCorrectionRotationResidualDegrees = agent.LatestPreCorrectionRotationResidualDegrees,
                postCorrectionPositionResidualWorldUnits = agent.LatestPostCorrectionPositionResidualWorldUnits,
                postCorrectionRotationResidualDegrees = agent.LatestPostCorrectionRotationResidualDegrees,
                maximumPreCorrectionPositionResidualWorldUnits = agent.MaximumPreCorrectionPositionResidualWorldUnits,
                maximumPreCorrectionRotationResidualDegrees = agent.MaximumPreCorrectionRotationResidualDegrees,
                maximumPostCorrectionPositionResidualWorldUnits = agent.MaximumPostCorrectionPositionResidualWorldUnits,
                maximumPostCorrectionRotationResidualDegrees = agent.MaximumPostCorrectionRotationResidualDegrees,
                maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits = agent.MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits,
                maximumUpdatePreCorrectionPositionResidualWorldUnits = agent.MaximumUpdatePreCorrectionPositionResidualWorldUnits,
                maximumUpdatePreCorrectionRotationResidualDegrees = agent.MaximumUpdatePreCorrectionRotationResidualDegrees,
                maximumUpdatePostCorrectionPositionResidualWorldUnits = agent.MaximumUpdatePostCorrectionPositionResidualWorldUnits,
                maximumUpdatePostCorrectionRotationResidualDegrees = agent.MaximumUpdatePostCorrectionRotationResidualDegrees,
                maximumLateUpdatePreCorrectionPositionResidualWorldUnits = agent.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits,
                maximumLateUpdatePreCorrectionRotationResidualDegrees = agent.MaximumLateUpdatePreCorrectionRotationResidualDegrees,
                maximumLateUpdatePostCorrectionPositionResidualWorldUnits = agent.MaximumLateUpdatePostCorrectionPositionResidualWorldUnits,
                maximumLateUpdatePostCorrectionRotationResidualDegrees = agent.MaximumLateUpdatePostCorrectionRotationResidualDegrees,
                maximumResidualWorldUnits = agent.MaximumResidualWorldUnits,
                maximumRotationResidualDegrees = agent.MaximumRotationResidualDegrees
            };
            // Close every diagnostic sample before returning. Runtime completion
            // hashes the evidence while the process is still alive; retaining a
            // write handle would prevent both the host and external validator from
            // opening the file under Windows sharing rules.
            var mode = created ? FileMode.Append : FileMode.CreateNew;
            using (var sampleWriter = new StreamWriter(new FileStream(path, mode, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false)))
            {
                sampleWriter.WriteLine(JsonConvert.SerializeObject(sample, JsonSettings));
                sampleWriter.Flush();
            }
            created = true;
        }

        public void Dispose()
        {
            // Each sample is independently flushed and closed.
        }

        private static string ComputeSha256(string filePath)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
