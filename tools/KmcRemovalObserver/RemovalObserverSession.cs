using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.LoadingScreen;
using Kingmaker.UI.Selection;
using Kingmaker.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KmcRemovalObserver
{
    // The genuine no-DLL observation. The KingmakerMountedCombat assembly is
    // not installed in this process at all; this session only routes native
    // saves into the owned profile, proves that absence, asks the engine to
    // load the prepared cleanup archive through its own main-menu path, watches
    // the load, orders ordinary movement of the main character, records what
    // it saw, and quits. It never writes a save and never touches human roots.
    internal sealed class RemovalObserverSession
    {
        private const string RequestArgument = "-kmcObserverRequest";
        private const string TokenArgument = "-kmcObserverToken";
        private const string RequestHashArgument = "-kmcObserverRequestSha256";
        private const string RequestFileName = "observer-request.json";
        private const string ResultFileName = "observer-result.json";
        private const string ObservationsFileName = "observer-observations.jsonl";
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error };

        private readonly UnityModManager.ModEntry modEntry;
        private readonly ObserverRequest request;
        private readonly string requestSha256;
        private readonly string evidenceRoot;
        private readonly DateTime startedAtUtc = DateTime.UtcNow;
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly List<string> errors = new List<string>();
        private readonly List<JObject> checks = new List<JObject>();
        private readonly JObject observations = new JObject();
        private readonly List<string> firstLogErrors = new List<string>();
        private readonly List<string> kmcRelatedLog = new List<string>();
        private int logErrors, logExceptions, loadWindowErrors, loadWindowExceptions, observationSequence;
        private bool loadWindow, loadCallback, completed;
        private int stage, frames;
        private UnitEntityData mover;
        private Vector3 origin, destination;
        private string archivePath, archiveSha256Before;

        private RemovalObserverSession(UnityModManager.ModEntry modEntry, ObserverRequest request, string requestSha256, string evidenceRoot)
        {
            this.modEntry = modEntry; this.request = request; this.requestSha256 = requestSha256; this.evidenceRoot = evidenceRoot;
        }

        internal string RunId => request.RunId;

        internal static RemovalObserverSession CreateFromCommandLine(UnityModManager.ModEntry modEntry)
        {
            var arguments = Environment.GetCommandLineArgs();
            var requestPath = FindSingleArgument(arguments, RequestArgument);
            if (requestPath == null) return null;
            var full = Path.GetFullPath(requestPath);
            if (!string.Equals(Path.GetFileName(full), RequestFileName, StringComparison.Ordinal))
                throw new InvalidOperationException("Observer request must use the exact observer-request.json filename.");
            var bytes = File.ReadAllBytes(full);
            var sha = Sha256(bytes);
            var expectedSha = FindSingleArgument(arguments, RequestHashArgument);
            if (expectedSha == null || !string.Equals(expectedSha, sha, StringComparison.Ordinal))
                throw new InvalidOperationException("Observer request bytes do not match the command-line SHA-256 binding.");
            var request = JsonConvert.DeserializeObject<ObserverRequest>(new UTF8Encoding(false, true).GetString(bytes), JsonSettings);
            if (request == null) throw new InvalidOperationException("Observer request deserialized to null.");
            var problems = request.Validate();
            if (problems.Count != 0) throw new InvalidOperationException("Observer request is invalid: " + string.Join("; ", problems));
            var token = FindSingleArgument(arguments, TokenArgument);
            if (token == null || !string.Equals(token, request.TransactionToken, StringComparison.Ordinal))
                throw new InvalidOperationException("Observer transaction token is missing or does not match the request.");
            var evidenceRoot = Path.GetFullPath(request.EvidenceRoot).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.Equals(evidenceRoot, Path.GetDirectoryName(full).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Observer request evidenceRoot does not match its own directory.");
            if (File.Exists(Path.Combine(evidenceRoot, ResultFileName)))
                throw new InvalidOperationException("Observer result already exists for this run.");
            var session = new RemovalObserverSession(modEntry, request, sha, evidenceRoot);
            session.Arm();
            return session;
        }

        // A bootstrap failure still leaves the launcher a result to read, so a
        // broken observer never holds the game open until the harness timeout.
        internal static void TryReportBootstrapFailure(UnityModManager.ModEntry modEntry, Exception exception)
        {
            try
            {
                var requestPath = FindSingleArgument(Environment.GetCommandLineArgs(), RequestArgument);
                if (requestPath == null) return;
                var root = Path.GetDirectoryName(Path.GetFullPath(requestPath));
                var path = Path.Combine(root, ResultFileName);
                if (File.Exists(path)) return;
                WriteJsonCreateNew(path, new JObject {
                    ["schemaVersion"] = 1, ["evidenceKind"] = "kmc-removal-observer", ["runId"] = null, ["transactionToken"] = null,
                    ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["startedAtUtc"] = DateTime.UtcNow.ToString("o"),
                    ["completedAtUtc"] = DateTime.UtcNow.ToString("o"), ["status"] = "FAIL", ["observerVersion"] = ObserverIdentity.ProductVersion,
                    ["observerAssembly"] = typeof(RemovalObserverSession).Assembly.GetName().Name, ["requestSha256"] = null, ["stage"] = -1,
                    ["checks"] = new JArray(), ["checkPassCount"] = 0, ["checkFailCount"] = 0,
                    ["errors"] = new JArray("Observer bootstrap failed: " + exception.GetType().Name + ": " + exception.Message),
                    ["observations"] = new JObject() });
                try { Application.Quit(); } catch (Exception quitException) { modEntry.Logger.LogException("Quit", quitException); }
            }
            catch (Exception reportingException)
            {
                modEntry.Logger.LogException("Bootstrap failure reporting", reportingException);
            }
        }

        private void Arm()
        {
            // Routing first: from here every native save-root resolution in this
            // process lands inside the owned profile, before any menu exists.
            NativeSaveRouting.Install(request.ProfileRoot);
            Application.logMessageReceived += HandleLog;
            observations["routing"] = new JObject { ["savePath"] = NativeSaveRouting.SavesRoot, ["stashFolder"] = NativeSaveRouting.StashFolder };
            Write("armed", new JObject { ["savePath"] = NativeSaveRouting.SavesRoot, ["stashFolder"] = NativeSaveRouting.StashFolder,
                ["observerVersion"] = ObserverIdentity.ProductVersion });
        }

        internal void Update()
        {
            if (completed) return;
            if (clock.Elapsed.TotalSeconds > request.TimeoutSeconds) { Fail("Observer timed out at stage " + stage + " after " + frames + " frames."); return; }
            var game = Game.Instance;
            switch (stage)
            {
                case 0:
                    if (game == null || game.SaveManager == null || game.CurrentlyLoadedArea != null || !game.IsControllerMouse ||
                        !SceneManager.GetSceneByName(SceneName.MainMenu).isLoaded || game.UI == null || game.UI.MainMenu == null ||
                        LoadingScreen.Instance == null || LoadingProcess.Instance.IsLoadingInProcess) { frames = 0; return; }
                    if (++frames < 30) return;
                    BeginLoad(game);
                    return;
                case 1:
                    if (!loadCallback || game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                        game.Player == null || game.Player.MainCharacter.Value == null || game.CurrentMode != GameModeType.Default) { frames = 0; return; }
                    if (++frames < 10) return;
                    loadWindow = false;
                    ObserveLoadedWorld(game);
                    return;
                case 2:
                    var moved = GeometryUtils.MechanicsDistance(mover.Position, origin);
                    if (moved < 1.5f || !mover.Commands.Empty)
                    { if (++frames > 1200) Fail("The main character never moved without KMC: " + moved + " m."); return; }
                    CompleteMovement(game, moved);
                    return;
            }
        }

        private void BeginLoad(Game game)
        {
            var modsRoot = Path.GetFullPath(request.ModsRoot).TrimEnd(Path.DirectorySeparatorChar);
            var directories = Directory.GetDirectories(modsRoot).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var kmcDirectories = directories.Where(d => string.Equals(d, request.KmcModId, StringComparison.OrdinalIgnoreCase)).ToArray();
            var kmcDllFiles = Directory.GetFiles(modsRoot, "*.dll", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).StartsWith(request.KmcModId, StringComparison.OrdinalIgnoreCase)).ToArray();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var kmcAssemblies = assemblies.Where(n => n.IndexOf(request.KmcModId, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            var entries = UnityModManager.modEntries.ToArray();
            var modEntries = entries.Select(e => e.Info.Id + (e.Active ? ":active" : ":inactive")).ToArray();
            var kmcEntries = entries.Where(e => string.Equals(e.Info.Id, request.KmcModId, StringComparison.OrdinalIgnoreCase)).Select(e => e.Info.Id).ToArray();
            var observerEntry = entries.FirstOrDefault(e => string.Equals(e.Info.Id, request.ObserverModId, StringComparison.Ordinal));
            var owners = NativeSaveRouting.PatchOwners();
            var kmcOwners = owners.Where(o => request.KmcHarmonyIds.Contains(o) || o.IndexOf(request.KmcModId, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            observations["absence"] = new JObject {
                ["modsRoot"] = modsRoot, ["modDirectories"] = new JArray(directories), ["kmcModsDirectories"] = new JArray(kmcDirectories),
                ["kmcDllFiles"] = new JArray(kmcDllFiles), ["loadedAssemblies"] = assemblies.Length, ["kmcAssemblies"] = new JArray(kmcAssemblies),
                ["modEntries"] = new JArray(modEntries), ["kmcModEntries"] = new JArray(kmcEntries),
                ["observerModEntryPresent"] = observerEntry != null && observerEntry.Active,
                ["harmonyOwners"] = new JArray(owners), ["kmcHarmonyOwners"] = new JArray(kmcOwners),
                ["kmcAssemblyLoaded"] = kmcAssemblies.Length != 0, ["kmcModsDirectoryPresent"] = kmcDirectories.Length != 0 };
            Check(kmcDirectories.Length == 0 && kmcDllFiles.Length == 0, "no-KMC-directory-or-DLL-under-Mods");
            Check(kmcAssemblies.Length == 0, "no-KMC-assembly-loaded-in-the-process");
            Check(kmcEntries.Length == 0 && observerEntry != null && observerEntry.Active, "UMM-lists-the-observer-and-no-KMC-entry");
            Check(kmcOwners.Length == 0, "no-KMC-Harmony-patch-owner");
            var savePath = game.SaveManager.SavePath;
            var stash = NativeSaveRouting.ObservedStashFolder;
            observations["routing"] = new JObject { ["savePath"] = savePath, ["stashFolder"] = stash,
                ["expectedSavePath"] = NativeSaveRouting.SavesRoot, ["expectedStashFolder"] = NativeSaveRouting.StashFolder };
            Check(string.Equals(savePath, NativeSaveRouting.SavesRoot, StringComparison.Ordinal) &&
                string.Equals(stash, NativeSaveRouting.StashFolder, StringComparison.Ordinal), "native-save-and-stash-paths-resolve-inside-the-owned-profile");
            archivePath = Path.Combine(savePath, request.Archive.FileName);
            var file = new FileInfo(archivePath);
            archiveSha256Before = file.Exists ? Sha256File(archivePath) : null;
            Check(file.Exists && (file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length == request.Archive.Length &&
                string.Equals(archiveSha256Before, request.Archive.Sha256, StringComparison.Ordinal), "cleanup-archive-bytes-are-exactly-the-prepared-ones");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            var descriptor = game.SaveManager.LoadZipSave(archivePath);
            var area = descriptor == null || descriptor.Area == null ? null : descriptor.Area.AssetGuidThreadSafe;
            observations["descriptor"] = descriptor == null ? null : new JObject {
                ["name"] = descriptor.Name, ["type"] = descriptor.Type.ToString(), ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName,
                ["area"] = area, ["fileName"] = descriptor.FileName, ["path"] = descriptor.FolderName };
            Check(descriptor != null && descriptor.Name == request.Archive.InternalName && descriptor.Type == SaveInfo.SaveType.Manual &&
                descriptor.GameId == request.Archive.GameId && descriptor.GameName == request.Archive.GameName && area == request.Archive.Area &&
                string.Equals(descriptor.FolderName, archivePath, StringComparison.Ordinal), "engine-reads-the-cleanup-archive-header-as-declared");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            game.SaveManager.AddCallbackAfterLoad(() => loadCallback = true);
            loadWindow = true;
            game.UI.MainMenu.LoadGame(descriptor);
            Write("load-requested", new JObject { ["archive"] = archivePath, ["sha256"] = archiveSha256Before, ["seconds"] = clock.Elapsed.TotalSeconds });
            frames = 0; stage = 1;
        }

        private void ObserveLoadedWorld(Game game)
        {
            var player = game.Player;
            var party = player.Party.ToArray();
            var units = game.State.Units.ToArray();
            var kmcUnits = units.Where(u => u.Blueprint != null && request.KmcBlueprintGuids.Contains(u.Blueprint.AssetGuid)).ToArray();
            var mammoths = units.Count(u => u.Blueprint != null && u.Blueprint.AssetGuid == request.MammothBlueprintGuid);
            mover = player.MainCharacter.Value;
            var areaGuid = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
            observations["world"] = new JObject {
                ["gameId"] = player.GameId, ["gameName"] = mover.CharacterName, ["area"] = areaGuid, ["mode"] = game.CurrentMode.ToString(),
                ["party"] = party.Length, ["partyIds"] = new JArray(party.Select(u => u.UniqueId)), ["mainCharacter"] = mover.UniqueId,
                ["units"] = units.Length, ["kmcBlueprintUnits"] = kmcUnits.Length, ["kmcBlueprintUnitIds"] = new JArray(kmcUnits.Select(u => u.UniqueId)),
                ["mammothUnits"] = mammoths, ["paused"] = game.IsPaused, ["inCombat"] = player.IsInCombat,
                ["loadSeconds"] = clock.Elapsed.TotalSeconds };
            Check(player.GameId == request.Archive.GameId && areaGuid == request.Archive.Area, "the-cleanup-archive-opened-its-own-campaign-and-area");
            Check(party.Length >= 1 && kmcUnits.Length == 0, "the-world-holds-a-party-and-no-KMC-blueprint-unit");
            Check(loadWindowExceptions == 0, "the-engine-raised-no-exception-while-loading-without-KMC");
            Check(kmcRelatedLog.Count == 0, "the-engine-logged-nothing-about-KMC-or-its-save-member");
            Write("loaded", new JObject { ["world"] = observations["world"].DeepClone(), ["loadWindowErrors"] = loadWindowErrors, ["loadWindowExceptions"] = loadWindowExceptions });
            if (errors.Count != 0) { Complete("FAIL"); return; }
            if (game.IsPaused) game.IsPaused = false;
            origin = mover.Position;
            destination = FindWalkableNear(origin, 3f);
            SelectionManager.Instance.SelectUnit(mover.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
            Write("movement-dispatched", new JObject { ["mover"] = mover.UniqueId,
                ["origin"] = new JArray(origin.x, origin.y, origin.z), ["destination"] = new JArray(destination.x, destination.y, destination.z) });
            frames = 0; stage = 2;
        }

        private void CompleteMovement(Game game, float moved)
        {
            observations["movement"] = new JObject { ["mover"] = mover.UniqueId, ["displacement"] = moved,
                ["origin"] = new JArray(origin.x, origin.y, origin.z), ["destination"] = new JArray(destination.x, destination.y, destination.z),
                ["inCombat"] = game.Player.IsInCombat, ["mode"] = game.CurrentMode.ToString() };
            Check(moved >= 1.5f && !game.Player.IsInCombat && game.CurrentMode == GameModeType.Default, "the-main-character-moved-through-ordinary-native-input-without-KMC");
            var after = Sha256File(archivePath);
            observations["archive"] = new JObject { ["path"] = archivePath, ["fileName"] = request.Archive.FileName, ["sha256Before"] = archiveSha256Before,
                ["sha256After"] = after, ["changedOnDisk"] = !string.Equals(after, archiveSha256Before, StringComparison.Ordinal),
                ["length"] = new FileInfo(archivePath).Length };
            Write("movement-complete", new JObject { ["displacement"] = moved, ["archiveSha256After"] = after });
            Complete(errors.Count == 0 ? "PASS" : "FAIL");
        }

        internal void Fail(string reason)
        {
            if (completed) return;
            errors.Add(reason);
            Complete("FAIL");
        }

        private void Complete(string status)
        {
            if (completed) return;
            completed = true;
            Application.logMessageReceived -= HandleLog;
            observations["log"] = new JObject { ["errors"] = logErrors, ["exceptions"] = logExceptions, ["loadWindowErrors"] = loadWindowErrors,
                ["loadWindowExceptions"] = loadWindowExceptions, ["first"] = new JArray(firstLogErrors), ["kmcRelated"] = new JArray(kmcRelatedLog) };
            var passed = checks.Count(c => (bool)c["passed"]);
            var result = new JObject {
                ["schemaVersion"] = 1, ["evidenceKind"] = "kmc-removal-observer", ["runId"] = request.RunId, ["transactionToken"] = request.TransactionToken,
                ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["startedAtUtc"] = startedAtUtc.ToString("o"),
                ["completedAtUtc"] = DateTime.UtcNow.ToString("o"), ["status"] = status, ["observerVersion"] = ObserverIdentity.ProductVersion,
                ["observerAssembly"] = typeof(RemovalObserverSession).Assembly.GetName().Name, ["requestSha256"] = requestSha256, ["stage"] = stage,
                ["checks"] = new JArray(checks), ["checkPassCount"] = passed, ["checkFailCount"] = checks.Count - passed,
                ["errors"] = new JArray(errors), ["observations"] = observations };
            try { WriteJsonCreateNew(Path.Combine(evidenceRoot, ResultFileName), result); }
            catch (Exception exception) { modEntry.Logger.LogException("Result write", exception); }
            modEntry.Logger.Log("KMC removal observer completed: " + status + " (" + passed + "/" + (checks.Count - passed) + ").");
            try { Application.Quit(); } catch (Exception exception) { modEntry.Logger.LogException("Quit", exception); }
        }

        private void Check(bool passed, string name)
        {
            checks.Add(new JObject { ["name"] = name, ["passed"] = passed });
            if (!passed) errors.Add("Check failed: " + name);
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            logErrors++;
            if (type == LogType.Exception) logExceptions++;
            if (loadWindow) { loadWindowErrors++; if (type == LogType.Exception) loadWindowExceptions++; }
            var text = type + ": " + (condition ?? string.Empty);
            if (text.Length > 400) text = text.Substring(0, 400);
            if (firstLogErrors.Count < 10) firstLogErrors.Add(text);
            var haystack = (condition ?? string.Empty) + "\n" + (stackTrace ?? string.Empty);
            if (haystack.IndexOf("KingmakerMountedCombat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                haystack.IndexOf("kmc-mounted-state", StringComparison.OrdinalIgnoreCase) >= 0)
            { if (kmcRelatedLog.Count < 10) kmcRelatedLog.Add(text); }
        }

        private void Write(string kind, JObject detail)
        {
            try
            {
                var row = new JObject { ["sequence"] = ++observationSequence, ["kind"] = kind, ["runId"] = request.RunId,
                    ["atUtc"] = DateTime.UtcNow.ToString("o"), ["stage"] = stage, ["detail"] = detail };
                File.AppendAllText(Path.Combine(evidenceRoot, ObservationsFileName), row.ToString(Formatting.None) + "\n", new UTF8Encoding(false));
            }
            catch (Exception exception) { modEntry.Logger.LogException("Observation write", exception); }
        }

        private static Vector3 FindWalkableNear(Vector3 origin, float distance)
        {
            for (var i = 0; i < 16; i++)
            {
                var wanted = origin + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(origin, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f && GeometryUtils.MechanicsDistance(actual, origin) > distance - 0.5f) return actual;
            }
            throw new InvalidOperationException("No native walkable destination exists near the main character.");
        }

        private static string FindSingleArgument(string[] arguments, string name)
        {
            string found = null;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], name, StringComparison.Ordinal)) continue;
                if (found != null || index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                    throw new InvalidOperationException(name + " command-line argument is missing or ambiguous.");
                found = arguments[index + 1];
            }
            return found;
        }

        private static void WriteJsonCreateNew(string path, JObject value)
        {
            var temporary = path + ".partial";
            if (File.Exists(temporary)) File.Delete(temporary);
            File.WriteAllText(temporary, value.ToString(Formatting.Indented), new UTF8Encoding(false));
            File.Move(temporary, path);
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string Sha256File(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
