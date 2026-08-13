using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.LoadingScreen;
using KingmakerMountedCombat.Logging;
using UnityEngine.SceneManagement;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum WorkingFixtureLoadState
    {
        NotStarted,
        Loading,
        LoadedAndVerified,
        Failed
    }

    /// <summary>
    /// Loads only the externally qualified Working fixture. It never enumerates save
    /// slots and never constructs a path for Baseline or any foreign save.
    /// </summary>
    internal sealed class WorkingFixtureLoader
    {
        private readonly RuntimeRequest request;
        private readonly IModLogger logger;
        private readonly List<string> errors = new List<string>();
        private SaveInfo descriptor;
        private bool loadRoutineCompleted;
        private int stableReadyFrames;
        private readonly WorkingFixtureLoadWatchdog loadWatchdog = new WorkingFixtureLoadWatchdog();

        public WorkingFixtureLoader(RuntimeRequest request, IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (request.SchemaVersion != RuntimeRequest.SaveBackedSchemaVersion || request.Fixture == null)
            {
                throw new ArgumentException("Working fixture loader requires an exact schema-v2 request.", nameof(request));
            }
        }

        public WorkingFixtureLoadState State { get; private set; } = WorkingFixtureLoadState.NotStarted;

        public IReadOnlyList<string> Errors => errors;

        public SaveInfo Descriptor => descriptor;

        public string WorkingPath { get; private set; }

        public void Start()
        {
            if (State != WorkingFixtureLoadState.NotStarted)
            {
                throw new InvalidOperationException("Working fixture load may start only once.");
            }

            try
            {
                var validationErrors = request.Validate();
                if (validationErrors.Count != 0)
                {
                    throw new InvalidOperationException("Runtime request is not valid: " + string.Join("; ", validationErrors));
                }

                var game = Game.Instance;
                if (game == null || game.SaveManager == null)
                {
                    throw new InvalidOperationException("Kingmaker game/save manager is unavailable.");
                }
                if (game.CurrentlyLoadedArea != null)
                {
                    throw new InvalidOperationException("Working fixture loader refuses to replace an already loaded area.");
                }
                if (!game.IsControllerMouse || !SceneManager.GetSceneByName(SceneName.MainMenu).isLoaded)
                {
                    throw new InvalidOperationException("Working fixture loader requires the exact PC main-menu scene.");
                }
                if (game.UI == null || game.UI.MainMenu == null || LoadingScreen.Instance == null)
                {
                    throw new InvalidOperationException("Kingmaker native main-menu or loading-screen bootstrap is unavailable.");
                }
                if (LoadingProcess.Instance.IsLoadingInProcess)
                {
                    throw new InvalidOperationException("Working fixture loader refuses to overlap an active Kingmaker loading process.");
                }

                var root = Path.GetFullPath(game.SaveManager.SavePath).TrimEnd(Path.DirectorySeparatorChar);
                var working = request.Fixture.Working;
                var candidate = Path.GetFullPath(Path.Combine(root, working.FileName));
                if (!string.Equals(Path.GetDirectoryName(candidate).TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Path.GetFileName(candidate), working.FileName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Working fixture leaf escaped the exact engine save root.");
                }

                var file = new FileInfo(candidate);
                if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException("Exact Working fixture is missing or is a reparse point.");
                }
                if (file.Length != working.Length || file.LastWriteTimeUtc.Ticks != working.LastWriteTimeUtcTicks ||
                    !string.Equals(ComputeSha256(candidate), working.Sha256, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Exact Working fixture file identity differs from the request.");
                }

                WorkingPath = candidate;
                descriptor = game.SaveManager.LoadZipSave(candidate);
                if (descriptor == null)
                {
                    throw new InvalidOperationException("Kingmaker could not read the exact Working descriptor.");
                }
                VerifyDescriptor(descriptor, working, candidate);

                file.Refresh();
                if (!file.Exists || file.Length != working.Length || file.LastWriteTimeUtc.Ticks != working.LastWriteTimeUtcTicks ||
                    !string.Equals(ComputeSha256(candidate), working.Sha256, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Exact Working fixture changed between descriptor inspection and load authorization.");
                }

                State = WorkingFixtureLoadState.Loading;
                game.SaveManager.AddCallbackAfterLoad(HandleLoadRoutineCompleted);
                game.UI.MainMenu.LoadGame(descriptor);
                logger.Info("Requested native main-menu load of the exact qualified KMC Working fixture.");
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
                throw;
            }
        }

        public bool TryCompleteVerification()
        {
            if (State == WorkingFixtureLoadState.LoadedAndVerified)
            {
                return true;
            }
            if (State != WorkingFixtureLoadState.Loading)
            {
                return false;
            }

            try
            {
                var watchdogState = loadWatchdog.Observe(LoadingProcess.Instance.IsLoadingInProcess, loadRoutineCompleted);
                if (watchdogState == WorkingFixtureLoadWatchdogState.PipelineStoppedBeforeCallback)
                {
                    throw new InvalidOperationException("Kingmaker loading pipeline stopped before the exact Working load callback completed.");
                }
                if (watchdogState != WorkingFixtureLoadWatchdogState.CallbackCompleted)
                {
                    return false;
                }

                var game = Game.Instance;
                if (game == null || game.CurrentlyLoadedArea == null || game.Player == null || game.Player.MainCharacter.Value == null ||
                    game.CurrentMode != GameModeType.Default || LoadingProcess.Instance.IsLoadingInProcess)
                {
                    stableReadyFrames = 0;
                    return false;
                }

                var fixture = request.Fixture.Working;
                if (!string.Equals(game.Player.GameId, fixture.GameId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Loaded Player.GameId differs from the qualified Working descriptor.");
                }
                if (!string.Equals(game.Player.MainCharacter.Value.CharacterName, fixture.GameName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Loaded main-character/GameName identity differs from the qualified Working descriptor.");
                }
                if (!string.Equals(game.CurrentlyLoadedArea.AssetGuidThreadSafe, fixture.Area, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Loaded area identity differs from the qualified Working descriptor.");
                }

                stableReadyFrames++;
                if (stableReadyFrames < 10)
                {
                    return false;
                }

                State = WorkingFixtureLoadState.LoadedAndVerified;
                logger.Info("Exact KMC Working fixture load identity verified in the live game state.");
                return true;
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
                throw;
            }
        }

        private void HandleLoadRoutineCompleted()
        {
            loadRoutineCompleted = true;
        }

        private void Fail(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
            State = WorkingFixtureLoadState.Failed;
        }

        private static void VerifyDescriptor(SaveInfo observed, RuntimeSaveDescriptor expected, string expectedPath)
        {
            var observedArea = observed.Area == null ? null : observed.Area.AssetGuidThreadSafe;
            if (!string.Equals(observed.Name, RuntimeRequest.WorkingSaveName, StringComparison.Ordinal) ||
                !string.Equals(observed.FileName, expected.FileName, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFullPath(observed.FolderName), expectedPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(observed.GameId, expected.GameId, StringComparison.Ordinal) ||
                !string.Equals(observed.GameName, expected.GameName, StringComparison.Ordinal) ||
                !string.Equals(observedArea, expected.Area, StringComparison.Ordinal) ||
                observed.Type != SaveInfo.SaveType.Manual || observed.CompatibilityVersion != 1)
            {
                throw new InvalidOperationException("Kingmaker Working SaveInfo differs from the request-bound descriptor.");
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
