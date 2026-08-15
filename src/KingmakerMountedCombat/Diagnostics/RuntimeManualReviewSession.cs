using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Establishes the exact read-only presentation state and then yields control
    /// to the reviewer. It never advances a scripted scenario or authorizes a save.
    /// </summary>
    internal sealed class RuntimeManualReviewSession : IDisposable
    {
        internal const string ReadyFileName = "manual-review-ready.json";
        internal const string FailureFileName = "manual-review-failure.json";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Error,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly RuntimeRequest request;
        private readonly string loadedModId;
        private readonly RuntimeSaveAuthorization saveAuthorization;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPlayerActionController playerAction;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly Func<int> loadRequestCount;
        private readonly Func<int> saveRequestCount;
        private int stableReadyFrames;
        private bool started;
        private bool disposed;

        public RuntimeManualReviewSession(
            RuntimeRequest request,
            string loadedModId,
            RuntimeSaveAuthorization saveAuthorization,
            GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction,
            DiagnosticSettings settings,
            IModLogger logger,
            Func<int> loadRequestCount,
            Func<int> saveRequestCount)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.loadedModId = loadedModId ?? throw new ArgumentNullException(nameof(loadedModId));
            this.saveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.loadRequestCount = loadRequestCount ?? throw new ArgumentNullException(nameof(loadRequestCount));
            this.saveRequestCount = saveRequestCount ?? throw new ArgumentNullException(nameof(saveRequestCount));
            if (!RuntimeRequest.IsManualReviewScenario(request.Scenario) || request.Fixture == null ||
                request.Fixture.WriteAuthorization == null ||
                !string.Equals(request.Fixture.WriteAuthorization.Mode, "read-only", StringComparison.Ordinal))
            {
                throw new ArgumentException("Manual review session requires an exact read-only manual-review request.", nameof(request));
            }
        }

        public bool IsReady { get; private set; }

        public bool HasFailed { get; private set; }

        public void Update()
        {
            ThrowIfDisposed();
            if (HasFailed)
            {
                return;
            }

            try
            {
                if (!started)
                {
                    Start();
                    return;
                }

                ValidateReadOnlyBoundary();
                if (IsReady)
                {
                    return;
                }

                relationship.ValidateActivePair();
                var runtime = relationship.Runtime;
                var availability = playerAction.GetAvailability();
                var selection = SelectionManager.Instance;
                var selected = selection == null ? null : selection.SelectedUnits;
                var rider = relationship.Rider;
                var exactSelection = selected != null && selected.Count == 1 && rider != null && selected[0] == rider;
                var presentationReady = relationship.State == RelationshipState.Mounted &&
                    runtime.PresentationAttachmentLeaseActive && runtime.RiderParentMatchesAttachment &&
                    runtime.PoseConfigured && runtime.PoseHealthy && runtime.PoseFrameApplied &&
                    runtime.PoseApplicationFrameCount > 0 && runtime.PoseComponentCount == 1 &&
                    runtime.PoseBoneCount == 7 &&
                    string.Equals(runtime.PoseProfileId, MountedRiderPoseProfiles.MediumHumanoidOnMammoth.Id, StringComparison.Ordinal);
                var actionReady = playerAction.OverlayPresent && availability.IsVisible && availability.IsEnabled &&
                    availability.Action == MountedPlayerActionKind.Dismount &&
                    string.Equals(availability.Label, "Dismount", StringComparison.Ordinal);

                if (!presentationReady || !actionReady || !exactSelection)
                {
                    stableReadyFrames = 0;
                    return;
                }

                stableReadyFrames++;
                if (stableReadyFrames < 10)
                {
                    return;
                }

                WriteReadyEvidence(availability, selected.Select(unit => unit.UniqueId).ToArray());
                IsReady = true;
                logger.Info("Manual visual review is READY in exact read-only mounted state; waiting for the reviewer to exit Kingmaker.");
            }
            catch (Exception exception)
            {
                FailAndQuit(exception.GetType().FullName + ": " + exception.Message);
            }
        }

        public void FailAndQuit(string reason)
        {
            if (HasFailed || disposed)
            {
                return;
            }

            HasFailed = true;
            var retainedReason = string.IsNullOrWhiteSpace(reason) ? "Manual review failed without an exact cause." : reason;
            try
            {
                settings.EnableUnsafeMovementExperiment = false;
                relationship.Dismount(CleanupTrigger.Exception);
            }
            catch (Exception cleanupException)
            {
                retainedReason += " Cleanup also failed: " + cleanupException.GetType().FullName + ": " + cleanupException.Message;
            }

            try
            {
                WriteJsonAtomic(Path.Combine(request.EvidenceRoot, FailureFileName), new ManualReviewFailureEvidence
                {
                    SchemaVersion = 1,
                    EvidenceKind = "manual-visual-review-failure",
                    RunId = request.RunId,
                    Scenario = request.Scenario,
                    Status = "FAIL",
                    TransactionToken = request.TransactionToken,
                    FailedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                    ProcessId = Process.GetCurrentProcess().Id,
                    Reason = retainedReason
                });
            }
            catch (Exception evidenceException)
            {
                logger.Exception("Manual review failure evidence", evidenceException);
            }

            logger.Error("Manual visual review failed closed: " + retainedReason);
            try { Application.Quit(); }
            catch (Exception quitException) { logger.Exception("Manual review failure quit", quitException); }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            settings.EnableUnsafeMovementExperiment = false;
            relationship.Dismount(CleanupTrigger.ProcessTeardown);
            disposed = true;
        }

        private void Start()
        {
            ValidateReadOnlyBoundary();
            var game = Game.Instance;
            if (game == null || game.Player == null || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != GameModeType.Default || game.Player.IsInCombat)
            {
                throw new InvalidOperationException("Manual review requires the exact loaded, out-of-combat Default-mode Working fixture.");
            }

            settings.EnableUnsafeMovementExperiment = true;
            var transition = relationship.MountAutomationPair();
            if (!transition.Succeeded || relationship.State != RelationshipState.Mounted || relationship.Rider == null ||
                relationship.Mount == null || relationship.Rider.View == null)
            {
                throw new InvalidOperationException("Exact manual-review rider/Mammoth mount transition failed: " + relationship.LastResult);
            }

            var selection = SelectionManager.Instance;
            if (selection == null)
            {
                throw new InvalidOperationException("Native SelectionManager is unavailable for the review state.");
            }
            selection.SelectUnit(relationship.Rider.View, true, true, false);
            started = true;
            logger.Info("Manual visual review mounted the exact qualified rider/Mammoth pair; stabilizing pose and UI evidence.");
        }

        private void ValidateReadOnlyBoundary()
        {
            if (!saveAuthorization.IsActive || saveAuthorization.FatalViolationCount != 0 ||
                saveAuthorization.AuthorizedWriteCount != 0 || saveAuthorization.UnauthorizedLoadCount != 0 ||
                saveAuthorization.UnauthorizedWriteCount != 0 || saveRequestCount() != 0 || loadRequestCount() != 1)
            {
                throw new InvalidOperationException("Manual review save boundary is no longer exact read-only Working-load-only state.");
            }

            var game = Game.Instance;
            var working = request.Fixture.Working;
            if (game == null || game.Player == null || game.Player.MainCharacter.Value == null ||
                game.CurrentlyLoadedArea == null ||
                !string.Equals(game.Player.GameId, working.GameId, StringComparison.Ordinal) ||
                !string.Equals(game.Player.MainCharacter.Value.CharacterName, working.GameName, StringComparison.Ordinal) ||
                !string.Equals(game.CurrentlyLoadedArea.AssetGuidThreadSafe, working.Area, StringComparison.Ordinal) ||
                game.Player.IsInCombat || game.CurrentMode != GameModeType.Default)
            {
                throw new InvalidOperationException("Manual review left the exact noncombat Working fixture boundary.");
            }
        }

        private void WriteReadyEvidence(MountedPlayerActionAvailability availability, IReadOnlyList<string> selectedUnitIds)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var runtime = relationship.Runtime;
            WriteJsonAtomic(Path.Combine(request.EvidenceRoot, ReadyFileName), new ManualReviewReadyEvidence
            {
                SchemaVersion = 1,
                EvidenceKind = "manual-visual-review-ready",
                RunId = request.RunId,
                Scenario = request.Scenario,
                Status = "READY",
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = request.DllSha256,
                DllMvid = request.DllMvid,
                TransactionToken = request.TransactionToken,
                ReadyAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                LoadedModId = loadedModId,
                GameVersion = GameVersion.GetVersion(),
                ProcessId = Process.GetCurrentProcess().Id,
                CurrentGameMode = Game.Instance.CurrentMode.ToString(),
                LoadedAreaGuid = Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe,
                FixtureIdentityVerified = true,
                WorkingInternalName = request.Fixture.Working.InternalName,
                WorkingFileName = request.Fixture.Working.FileName,
                SaveWriteMode = request.Fixture.WriteAuthorization.Mode,
                LoadRequestCount = loadRequestCount(),
                SaveRequestCount = saveRequestCount(),
                AuthorizedLoadCount = saveAuthorization.AuthorizedLoadCount,
                AuthorizedWriteCount = saveAuthorization.AuthorizedWriteCount,
                UnauthorizedLoadCount = saveAuthorization.UnauthorizedLoadCount,
                UnauthorizedWriteCount = saveAuthorization.UnauthorizedWriteCount,
                RelationshipState = relationship.State.ToString(),
                MovementExperimentEnabled = settings.EnableUnsafeMovementExperiment,
                RiderId = rider.UniqueId,
                MountId = mount.UniqueId,
                MountBlueprintGuid = mount.Blueprint.AssetGuid,
                SelectedUnitIds = selectedUnitIds,
                ActionLabel = availability.Label,
                ActionVisible = availability.IsVisible,
                ActionEnabled = availability.IsEnabled,
                PoseProfileId = runtime.PoseProfileId,
                PoseHealthy = runtime.PoseHealthy,
                PoseFrameApplied = runtime.PoseFrameApplied,
                PoseBoneCount = runtime.PoseBoneCount,
                PoseComponentCount = runtime.PoseComponentCount,
                VisualAcceptance = "PENDING"
            });
        }

        private static void WriteJsonAtomic(string path, object value)
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException("Manual review evidence path already exists.");
            }

            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("Manual review evidence directory is missing.");
            }

            var temporary = Path.Combine(directory, ".manual-review." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(value, JsonSettings));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeManualReviewSession));
            }
        }

        private sealed class ManualReviewReadyEvidence
        {
            public int SchemaVersion { get; set; }
            public string EvidenceKind { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Status { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string TransactionToken { get; set; }
            public string ReadyAtUtc { get; set; }
            public string LoadedModId { get; set; }
            public string GameVersion { get; set; }
            public int ProcessId { get; set; }
            public string CurrentGameMode { get; set; }
            public string LoadedAreaGuid { get; set; }
            public bool FixtureIdentityVerified { get; set; }
            public string WorkingInternalName { get; set; }
            public string WorkingFileName { get; set; }
            public string SaveWriteMode { get; set; }
            public int LoadRequestCount { get; set; }
            public int SaveRequestCount { get; set; }
            public int AuthorizedLoadCount { get; set; }
            public int AuthorizedWriteCount { get; set; }
            public int UnauthorizedLoadCount { get; set; }
            public int UnauthorizedWriteCount { get; set; }
            public string RelationshipState { get; set; }
            public bool MovementExperimentEnabled { get; set; }
            public string RiderId { get; set; }
            public string MountId { get; set; }
            public string MountBlueprintGuid { get; set; }
            public IReadOnlyList<string> SelectedUnitIds { get; set; }
            public string ActionLabel { get; set; }
            public bool ActionVisible { get; set; }
            public bool ActionEnabled { get; set; }
            public string PoseProfileId { get; set; }
            public bool PoseHealthy { get; set; }
            public bool PoseFrameApplied { get; set; }
            public int PoseBoneCount { get; set; }
            public int PoseComponentCount { get; set; }
            public string VisualAcceptance { get; set; }
        }

        private sealed class ManualReviewFailureEvidence
        {
            public int SchemaVersion { get; set; }
            public string EvidenceKind { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Status { get; set; }
            public string TransactionToken { get; set; }
            public string FailedAtUtc { get; set; }
            public int ProcessId { get; set; }
            public string Reason { get; set; }
        }
    }
}
