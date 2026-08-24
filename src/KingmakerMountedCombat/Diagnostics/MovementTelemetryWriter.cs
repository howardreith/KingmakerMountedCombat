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
        private readonly Func<string> movementRow;
        private readonly double intervalSeconds;
        private bool created;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastSampleSeconds;
        private long sequence;

        public MovementTelemetryWriter(
            RuntimeRequest request,
            KingmakerMountedPairRuntime runtime,
            Func<string> relationshipState,
            Func<string> movementRow,
            double intervalSeconds)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }
            scenario = request.Scenario;
            runId = request.RunId;
            branch = request.Branch;
            commit = request.Commit;
            productVersion = request.ProductVersion;
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.relationshipState = relationshipState ?? throw new ArgumentNullException(nameof(relationshipState));
            this.movementRow = movementRow ?? throw new ArgumentNullException(nameof(movementRow));
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
            var row = movementRow();
            if (rider == null || mount == null || agent == null || !agent.IsConfigured || string.IsNullOrEmpty(row)) { return; }
            var expected = agent.ExpectedPosition;
            var expectedRotation = agent.ExpectedRotation;
            var position = agent.LatestPositionObservation;
            var yaw = agent.LatestYawObservation;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var mountAgent = mount.View?.AgentASP;
            var mountPath = mountAgent?.Path;
            var astarPath = AstarPath.active;
            var move = mount.Commands?.Move;
            var riderViewPositionResidual = rider.View == null ? (double?)null : Domain.MovementTelemetrySample.CalculateDistance(
                expected.x, expected.y, expected.z, rider.View.transform.position.x, rider.View.transform.position.y, rider.View.transform.position.z);
            var riderEntityPositionResidual = Domain.MovementTelemetrySample.CalculateDistance(
                expected.x, expected.y, expected.z, rider.Position.x, rider.Position.y, rider.Position.z);
            var riderViewRotationResidual = rider.View == null ? (double?)null : UnityEngine.Quaternion.Angle(expectedRotation, rider.View.transform.rotation);
            var riderEntityRotationResidual = Domain.MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, rider.Orientation);
            var conservativePositionResidual = riderViewPositionResidual.HasValue
                ? System.Math.Max(riderViewPositionResidual.Value, riderEntityPositionResidual)
                : riderEntityPositionResidual;
            var conservativeRotationResidual = riderViewRotationResidual.HasValue
                ? System.Math.Max(riderViewRotationResidual.Value, riderEntityRotationResidual)
                : riderEntityRotationResidual;
            var sample = new
            {
                schemaVersion = 1,
                scenario,
                row,
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
                requestedDestination = move == null ? null : VectorEvidence.From(move.Target),
                riderStockAgentEnabled = rider.View?.AgentASP?.enabled,
                mountStockAgentEnabled = mount.View?.AgentASP?.enabled,
                riderAvoidanceDisabled = rider.View?.AgentASP?.AvoidanceDisabled,
                mountAvoidanceDisabled = mount.View?.AgentASP?.AvoidanceDisabled,
                riderEntityPosition = VectorEvidence.From(rider.Position),
                mountEntityPosition = VectorEvidence.From(mount.Position),
                riderEntityOrientation = rider.Orientation,
                mountEntityOrientation = mount.Orientation,
                riderViewPosition = rider.View == null ? null : VectorEvidence.From(rider.View.transform.position),
                mountViewPosition = mount.View == null ? null : VectorEvidence.From(mount.View.transform.position),
                riderViewRotation = rider.View == null ? null : VectorEvidence.From(rider.View.transform.rotation.eulerAngles),
                mountViewRotation = mount.View == null ? null : VectorEvidence.From(mount.View.transform.rotation.eulerAngles),
                anchor = agent.AnchorName,
                sourceAnchor = runtime.PresentationSourceAnchorName,
                attachmentLeaseActive = runtime.PresentationAttachmentLeaseActive,
                attachmentParent = runtime.PresentationAttachmentParentName,
                riderParentMatchesAttachment = runtime.RiderParentMatchesAttachment,
                attachmentRiskState = runtime.PresentationAttachmentRiskState,
                riderViewParent = rider.View?.transform.parent == null ? null : rider.View.transform.parent.name,
                presentationPositionStrategy = "Mammoth-root static point projected from Spine at lease acquisition",
                presentationRotationStrategy = "upright authoritative-mount-root yaw plus configured rider yaw",
                expectedAnchorPosition = VectorEvidence.From(expected),
                expectedAnchorRotation = VectorEvidence.From(expectedRotation.eulerAngles),
                residualPositionWorldUnits = conservativePositionResidual,
                residualRotationDegrees = conservativeRotationResidual,
                riderViewPositionResidualWorldUnits = riderViewPositionResidual,
                riderEntityPositionResidualWorldUnits = riderEntityPositionResidual,
                riderViewRotationResidualDegrees = riderViewRotationResidual,
                riderEntityRotationResidualDegrees = riderEntityRotationResidual,
                latestAuthoritativePositionSequence = position?.AuthoritativePositionSequence,
                latestCurrentAuthoritativeAnchorX = position?.CurrentAuthoritativeX,
                latestCurrentAuthoritativeAnchorY = position?.CurrentAuthoritativeY,
                latestCurrentAuthoritativeAnchorZ = position?.CurrentAuthoritativeZ,
                latestPreviousAuthoritativePositionSequence = position?.PreviousAuthoritativePositionSequence,
                latestPreviousAuthoritativeAnchorX = position?.PreviousAuthoritativeX,
                latestPreviousAuthoritativeAnchorY = position?.PreviousAuthoritativeY,
                latestPreviousAuthoritativeAnchorZ = position?.PreviousAuthoritativeZ,
                latestPreviousAuthoritativePositionFrame = position?.PreviousAuthoritativeFrame,
                latestPreviousAuthoritativePositionPhase = position?.PreviousAuthoritativePhase?.ToString(),
                latestPreviousAuthoritativePositionReferenceKind = position == null
                    ? null
                    : position.PreviousAuthoritativeSameFrame && position.PreviousAuthoritativePhase == Domain.MovementSynchronizationPhase.Update
                        ? "same-frame-update"
                        : position.PreviousAuthoritativePhase == Domain.MovementSynchronizationPhase.Update
                            ? "prior-frame-update"
                            : "none",
                latestPreviousAuthoritativePositionSameFrame = position?.PreviousAuthoritativeSameFrame,
                latestPreviousAuthoritativePositionReferenceEligible = position?.PreviousAuthoritativeReferenceEligible,
                latestAuthoritativePositionDeltaWorldUnits = position?.AuthoritativePositionDeltaWorldUnits,
                latestViewCurrentPositionResidualWorldUnits = position?.ViewCurrentPositionResidualWorldUnits,
                latestEntityRawCurrentPositionResidualWorldUnits = position?.EntityRawCurrentPositionResidualWorldUnits,
                latestEntityPreviousAuthoritativePositionResidualWorldUnits = position?.EntityPreviousAuthoritativePositionResidualWorldUnits,
                latestEntityPhaseAdjustedPositionResidualWorldUnits = position?.EntityPhaseAdjustedPositionResidualWorldUnits,
                latestEntityRawPositionLagBoundWorldUnits = position?.EntityRawLagBoundWorldUnits,
                latestEntityRawPositionLagExcessWorldUnits = position?.EntityRawLagExcessWorldUnits,
                latestEntityPositionAuthorityAgeSteps = position?.EntityPositionAuthorityAgeSteps,
                latestPositionPhaseLagObserved = position?.PhaseLagObserved,
                latestPositionPhaseLagPermitted = position?.PhaseLagPermitted,
                latestPositionPhaseLagViolation = position?.PhaseLagViolation,
                latestPositionRecoveryRequiredBeforeSample = position?.RecoveryRequiredBeforeSample,
                latestPositionRecoveryUpdateObserved = position?.RecoveryUpdateObserved,
                latestPositionRecoverySatisfied = position?.RecoverySatisfied,
                latestPositionRecoveryViolation = position?.RecoveryViolation,
                latestPositionRecoveryPendingAfterSample = position?.RecoveryPendingAfterSample,
                latestPositionStationaryAuthority = position?.StationaryAuthority,
                latestStationaryPositionCorrectionViolation = position?.StationaryPositionCorrectionViolation,
                latestSynchronizationFrame = yaw?.Frame,
                latestAuthoritativeYawSequence = yaw?.AuthoritativeYawSequence,
                latestCurrentAuthoritativeYawDegrees = yaw?.CurrentAuthoritativeYawDegrees,
                latestCurrentMountEntityAuthoritativeYawDegrees = yaw?.MountEntityAuthoritativeYawDegrees,
                latestMountEntityRootYawResidualDegrees = yaw?.MountEntityRootYawResidualDegrees,
                latestPreviousAuthoritativeYawSequence = yaw?.PreviousAuthoritativeYawSequence,
                latestPreviousAuthoritativeYawDegrees = yaw?.PreviousAuthoritativeYawDegrees,
                latestPreviousAuthoritativeFrame = yaw?.PreviousAuthoritativeFrame,
                latestPreviousAuthoritativePhase = yaw?.PreviousAuthoritativePhase?.ToString(),
                latestPreviousAuthoritativeReferenceKind = yaw == null
                    ? null
                    : yaw.PreviousAuthoritativeSameFrame && yaw.PreviousAuthoritativePhase == Domain.MovementSynchronizationPhase.Update
                        ? "same-frame-update"
                        : yaw.PreviousAuthoritativePhase == Domain.MovementSynchronizationPhase.Update
                            ? "prior-frame-update"
                            : "none",
                latestPreviousAuthoritativeSameFrame = yaw?.PreviousAuthoritativeSameFrame,
                latestPreviousAuthoritativeReferenceEligible = yaw?.PreviousAuthoritativeReferenceEligible,
                latestAuthoritativeYawDeltaDegrees = yaw?.AuthoritativeYawDeltaDegrees,
                latestViewCurrentYawResidualDegrees = yaw?.ViewCurrentYawResidualDegrees,
                latestFullViewCurrentRotationResidualDegrees = agent.LatestPreCorrectionFullViewCurrentRotationResidualDegrees,
                latestEntityRawCurrentYawResidualDegrees = yaw?.EntityRawCurrentYawResidualDegrees,
                latestEntityPreviousAuthoritativeYawResidualDegrees = yaw?.EntityPreviousAuthoritativeYawResidualDegrees,
                latestEntityPhaseAdjustedYawResidualDegrees = yaw?.EntityPhaseAdjustedYawResidualDegrees,
                latestEntityRawLagBoundDegrees = yaw?.EntityRawLagBoundDegrees,
                latestEntityRawLagExcessDegrees = yaw?.EntityRawLagExcessDegrees,
                latestEntityYawAuthorityAgeSteps = yaw?.EntityYawAuthorityAgeSteps,
                latestPhaseLagObserved = yaw?.PhaseLagObserved,
                latestPhaseLagPermitted = yaw?.PhaseLagPermitted,
                latestPhaseLagViolation = yaw?.PhaseLagViolation,
                latestRecoveryRequiredBeforeSample = yaw?.RecoveryRequiredBeforeSample,
                latestRecoveryUpdateObserved = yaw?.RecoveryUpdateObserved,
                latestRecoverySatisfied = yaw?.RecoverySatisfied,
                latestRecoveryViolation = yaw?.RecoveryViolation,
                latestRecoveryPendingAfterSample = yaw?.RecoveryPendingAfterSample,
                latestStationaryAuthority = yaw?.StationaryAuthority,
                latestStationaryYawCorrectionViolation = yaw?.StationaryYawCorrectionViolation,
                riderSelected = selected != null && selected.Contains(rider),
                mountSelected = selected != null && selected.Contains(mount),
                selectedUnitIds = selected == null ? new string[0] : selected.Where(unit => unit != null).Select(unit => unit.UniqueId).ToArray(),
                riderCommandCount = rider.Commands?.Raw == null ? (int?)null : rider.Commands.Raw.Length,
                mountCommandCount = mount.Commands?.Raw == null ? (int?)null : mount.Commands.Raw.Length,
                riderActiveCommandTypes = rider.Commands?.Raw == null ? new string[0] : rider.Commands.Raw.Where(command => command != null).Select(command => command.GetType().FullName).ToArray(),
                mountActiveCommandTypes = mount.Commands?.Raw == null ? new string[0] : mount.Commands.Raw.Where(command => command != null).Select(command => command.GetType().FullName).ToArray(),
                mountIsReallyMoving = mountAgent?.IsReallyMoving,
                mountVelocity = mountAgent == null ? null : VectorEvidence.From(mountAgent.Velocity),
                mountSpeed = mountAgent?.Speed,
                mountMoveDirection = mountAgent == null ? null : VectorEvidence.From(mountAgent.MoveDirection),
                mountPathId = mountPath == null ? (uint?)null : mountPath.pathID,
                mountPathFailed = mountAgent?.PathFailed,
                mountRepathNeeded = mountAgent?.RepathNeeded,
                mountPathError = mountPath?.error,
                mountPathErrorLog = mountPath?.errorLog,
                mountPathPointCount = mountPath?.vectorPath?.Count,
                mountPathLength = mountPath == null ? (float?)null : mountPath.GetTotalLength(),
                astarPathPresent = astarPath != null,
                astarGraphUpdatesQueued = astarPath == null ? (bool?)null : astarPath.IsAnyGraphUpdatesQueued,
                unityFrameCount = UnityEngine.Time.frameCount,
                tileHandlerLastUpdateFrame = Pathfinding.Util.TileHandler.LastUpdateFrame,
                unityFrameStrictlyAfterTileHandlerLastUpdate =
                    UnityEngine.Time.frameCount > Pathfinding.Util.TileHandler.LastUpdateFrame,
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
                preCorrectionRawCurrentPositionResidualWorldUnits = agent.LatestPreCorrectionRawCurrentPositionResidualWorldUnits,
                preCorrectionViewCurrentPositionResidualWorldUnits = agent.LatestPreCorrectionViewCurrentPositionResidualWorldUnits,
                preCorrectionRotationResidualDegrees = agent.LatestPreCorrectionRotationResidualDegrees,
                postCorrectionPositionResidualWorldUnits = agent.LatestPostCorrectionPositionResidualWorldUnits,
                postCorrectionRotationResidualDegrees = agent.LatestPostCorrectionRotationResidualDegrees,
                maximumPreCorrectionPositionResidualWorldUnits = agent.MaximumPreCorrectionPositionResidualWorldUnits,
                maximumPreCorrectionRawCurrentPositionResidualWorldUnits = agent.MaximumPreCorrectionRawCurrentPositionResidualWorldUnits,
                maximumPreCorrectionRotationResidualDegrees = agent.MaximumPreCorrectionRotationResidualDegrees,
                maximumPostCorrectionPositionResidualWorldUnits = agent.MaximumPostCorrectionPositionResidualWorldUnits,
                maximumPostCorrectionRotationResidualDegrees = agent.MaximumPostCorrectionRotationResidualDegrees,
                maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits = agent.MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits,
                maximumUpdatePreCorrectionPositionResidualWorldUnits = agent.MaximumUpdatePreCorrectionPositionResidualWorldUnits,
                maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits = agent.MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits,
                maximumUpdatePreCorrectionRotationResidualDegrees = agent.MaximumUpdatePreCorrectionRotationResidualDegrees,
                maximumUpdatePostCorrectionPositionResidualWorldUnits = agent.MaximumUpdatePostCorrectionPositionResidualWorldUnits,
                maximumUpdatePostCorrectionRotationResidualDegrees = agent.MaximumUpdatePostCorrectionRotationResidualDegrees,
                maximumLateUpdatePreCorrectionPositionResidualWorldUnits = agent.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits,
                maximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits = agent.MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits,
                maximumLateUpdatePreCorrectionRotationResidualDegrees = agent.MaximumLateUpdatePreCorrectionRotationResidualDegrees,
                maximumLateUpdatePostCorrectionPositionResidualWorldUnits = agent.MaximumLateUpdatePostCorrectionPositionResidualWorldUnits,
                maximumLateUpdatePostCorrectionRotationResidualDegrees = agent.MaximumLateUpdatePostCorrectionRotationResidualDegrees,
                maximumCalibratedViewCurrentPositionResidualWorldUnits = agent.MaximumCalibratedViewCurrentPositionResidualWorldUnits,
                maximumCalibratedEntityRawCurrentPositionResidualWorldUnits = agent.MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits,
                maximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits = agent.MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits,
                maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits = agent.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits,
                maximumAuthoritativePositionDeltaWorldUnits = agent.MaximumAuthoritativePositionDeltaWorldUnits,
                maximumEntityRawPositionLagExcessWorldUnits = agent.MaximumEntityRawPositionLagExcessWorldUnits,
                entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits = Domain.MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits,
                positionPhaseLagObservedCount = agent.PositionPhaseLagObservedCount,
                positionPhaseLagPermittedCount = agent.PositionPhaseLagPermittedCount,
                positionPhaseLagSameFrameUpdateReferenceCount = agent.PositionPhaseLagSameFrameUpdateReferenceCount,
                positionPhaseLagEligibleReferenceCount = agent.PositionPhaseLagEligibleReferenceCount,
                positionPhaseLagViolationCount = agent.PositionPhaseLagViolationCount,
                positionPhaseLagRecoveryRequiredRawCount = agent.PositionPhaseLagRecoveryRequiredCount,
                positionPhaseLagRecoveryUpdateRawCount = agent.PositionPhaseLagRecoveryUpdateCount,
                positionPhaseLagRecoverySatisfiedRawCount = agent.PositionPhaseLagRecoverySatisfiedCount,
                positionPhaseLagRecoveryRequiredEffectiveCount = agent.EffectivePositionPhaseLagRecoveryRequiredCount,
                positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount,
                positionPhaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePositionPhaseLagRecoverySatisfiedCount,
                positionPhaseLagRecoveryViolationCount = agent.PositionPhaseLagRecoveryViolationCount,
                stationaryPositionCorrectionViolationCount = agent.StationaryPositionCorrectionViolationCount,
                outstandingPositionPhaseLagRecoveryCount = agent.OutstandingPositionPhaseLagRecoveryCount,
                maximumConsecutiveUnrecoveredPositionPhaseLagCount = agent.MaximumConsecutiveUnrecoveredPositionPhaseLagCount,
                maximumCalibratedViewCurrentYawResidualDegrees = agent.MaximumCalibratedViewCurrentYawResidualDegrees,
                maximumCalibratedFullViewCurrentRotationResidualDegrees = agent.MaximumCalibratedFullViewCurrentRotationResidualDegrees,
                maximumCalibratedMountEntityRootYawResidualDegrees = agent.MaximumCalibratedMountEntityRootYawResidualDegrees,
                maximumCalibratedEntityRawCurrentYawResidualDegrees = agent.MaximumCalibratedEntityRawCurrentYawResidualDegrees,
                maximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees = agent.MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees,
                maximumCalibratedEntityPhaseAdjustedYawResidualDegrees = agent.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees,
                maximumAuthoritativeYawDeltaDegrees = agent.MaximumAuthoritativeYawDeltaDegrees,
                maximumEntityRawLagExcessDegrees = agent.MaximumEntityRawLagExcessDegrees,
                entityRawLagArithmeticCoherenceEpsilonDegrees = Domain.MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees,
                phaseLagObservedCount = agent.PhaseLagObservedCount,
                phaseLagPermittedCount = agent.PhaseLagPermittedCount,
                phaseLagSameFrameUpdateReferenceCount = agent.PhaseLagSameFrameUpdateReferenceCount,
                phaseLagEligibleReferenceCount = agent.PhaseLagEligibleReferenceCount,
                phaseLagViolationCount = agent.PhaseLagViolationCount,
                phaseLagRecoveryRequiredCount = agent.PhaseLagRecoveryRequiredCount,
                phaseLagRecoveryUpdateCount = agent.PhaseLagRecoveryUpdateCount,
                phaseLagRecoverySatisfiedCount = agent.PhaseLagRecoverySatisfiedCount,
                phaseLagRecoveryRequiredRawCount = agent.PhaseLagRecoveryRequiredCount,
                phaseLagRecoveryUpdateRawCount = agent.PhaseLagRecoveryUpdateCount,
                phaseLagRecoverySatisfiedRawCount = agent.PhaseLagRecoverySatisfiedCount,
                phaseLagRecoveryRequiredEffectiveCount = agent.EffectivePhaseLagRecoveryRequiredCount,
                phaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePhaseLagRecoveryUpdateOrBoundaryCount,
                phaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePhaseLagRecoverySatisfiedCount,
                phaseLagRecoveryViolationCount = agent.PhaseLagRecoveryViolationCount,
                stationaryYawCorrectionViolationCount = agent.StationaryYawCorrectionViolationCount,
                outstandingPhaseLagRecoveryCount = agent.OutstandingPhaseLagRecoveryCount,
                maximumConsecutiveUnrecoveredPhaseLagCount = agent.MaximumConsecutiveUnrecoveredPhaseLagCount,
                stationaryBoundaryClosureAttemptCount = agent.StationaryBoundaryClosureAttemptCount,
                stationaryBoundaryClosureSucceededCount = agent.StationaryBoundaryClosureSucceededCount,
                stationaryBoundaryClosureFailedCount = agent.StationaryBoundaryClosureFailedCount,
                yawPhaseLagStationaryBoundaryClosureCount = agent.YawPhaseLagStationaryBoundaryClosureCount,
                positionPhaseLagStationaryBoundaryClosureCount = agent.PositionPhaseLagStationaryBoundaryClosureCount,
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

        private sealed class VectorEvidence
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public static VectorEvidence From(UnityEngine.Vector3 value)
            {
                return new VectorEvidence { X = value.x, Y = value.y, Z = value.z };
            }
        }
    }
}
