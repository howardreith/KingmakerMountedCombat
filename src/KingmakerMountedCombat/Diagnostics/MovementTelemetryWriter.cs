using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            Formatting = Formatting.None
        };

        private readonly string path;
        private readonly string scenario;
        private readonly string runId;
        private readonly KingmakerMountedPairRuntime runtime;
        private readonly Func<string> relationshipState;
        private readonly double intervalSeconds;
        private StreamWriter writer;
        private double elapsed;
        private long sequence;

        public MovementTelemetryWriter(string evidenceRoot, string scenario, string runId, KingmakerMountedPairRuntime runtime, Func<string> relationshipState, double intervalSeconds)
        {
            this.scenario = scenario;
            this.runId = runId;
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.relationshipState = relationshipState ?? throw new ArgumentNullException(nameof(relationshipState));
            this.intervalSeconds = intervalSeconds;
            path = Path.Combine(evidenceRoot, "movement-telemetry.jsonl");
        }

        public void Update(float deltaTime)
        {
            elapsed += Math.Max(0.0f, deltaTime);
            if (elapsed < intervalSeconds) { return; }
            elapsed = 0.0d;
            var rider = runtime.Rider;
            var mount = runtime.Mount;
            var agent = runtime.MovementAgent;
            if (rider == null || mount == null || agent == null || !agent.IsConfigured) { return; }
            if (writer == null)
            {
                writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false));
            }
            var expected = agent.ExpectedPosition;
            var expectedRotation = agent.ExpectedRotation;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var sample = new
            {
                schemaVersion = 1,
                scenario,
                runId,
                sequence = sequence++,
                utcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                riderId = rider.UniqueId,
                mountId = mount.UniqueId,
                relationshipState = relationshipState(),
                combat = rider.IsInCombat || mount.IsInCombat,
                authoritativeMover = "mount",
                riderStockAgentEnabled = rider.View?.AgentASP?.enabled,
                mountStockAgentEnabled = mount.View?.AgentASP?.enabled,
                riderAvoidanceDisabled = rider.View?.AgentASP?.AvoidanceDisabled,
                mountAvoidanceDisabled = mount.View?.AgentASP?.AvoidanceDisabled,
                riderEntityPosition = rider.Position,
                mountEntityPosition = mount.Position,
                riderViewPosition = rider.View?.transform.position,
                mountViewPosition = mount.View?.transform.position,
                anchor = agent.AnchorName,
                expectedAnchorPosition = expected,
                expectedAnchorRotation = expectedRotation.eulerAngles,
                residualPositionWorldUnits = Domain.MovementTelemetrySample.CalculateDistance(expected.x, expected.y, expected.z, rider.View.transform.position.x, rider.View.transform.position.y, rider.View.transform.position.z),
                residualRotationDegrees = Domain.MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, rider.View.transform.rotation.eulerAngles.y),
                riderSelected = selected != null && selected.Contains(rider),
                mountSelected = selected != null && selected.Contains(mount),
                riderCommandCount = rider.Commands?.Raw == null ? (int?)null : rider.Commands.Raw.Length,
                mountCommandCount = mount.Commands?.Raw == null ? (int?)null : mount.Commands.Raw.Length,
                maximumResidualWorldUnits = agent.MaximumResidualWorldUnits,
                maximumRotationResidualDegrees = agent.MaximumRotationResidualDegrees
            };
            writer.WriteLine(JsonConvert.SerializeObject(sample, JsonSettings));
            writer.Flush();
        }

        public void Dispose()
        {
            writer?.Dispose();
            writer = null;
        }
    }
}
