using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.LoadingScreen;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KmcRemovalObserver
{
    // The request-free normal-player control. KingmakerMountedCombat is installed
    // and loaded exactly as for a player, with NO automation request. This session
    // only routes native saves into the owned profile, inspects (read-only, by
    // name) that KMC is active on its ordinary path, loads the declared disposable
    // fixture through the main menu, and drives the player's own native inputs:
    // the Mount ability on the rider's own Horse, one ground click, the Dismount
    // ability and one unmounted ground click. It never calls KMC code, grants
    // facts or resources, writes a save, or changes a setting.
    internal sealed class NormalPlayerSession : IObserverSession
    {
        private const string ResultFileName = "observer-result.json";
        private const string ObservationsFileName = "observer-observations.jsonl";
        private const string AnchorName = "KMC_RiderPositionAnchor";
        private static readonly string[] KmcRuntimeArguments = { "-kmcRuntimeRequest", "-kmcRuntimeToken", "-kmcRuntimeRequestSha256" };

        private readonly UnityModManager.ModEntry modEntry;
        private readonly ObserverRequest request;
        private readonly ObserverNormalPlayer roles;
        private readonly string requestSha256, evidenceRoot;
        private readonly DateTime startedAtUtc = DateTime.UtcNow;
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly List<string> errors = new List<string>();
        private readonly List<JObject> checks = new List<JObject>();
        private readonly JObject observations = new JObject();
        private readonly JArray inputs = new JArray();
        private readonly List<string> kmcLog = new List<string>(), firstLogErrors = new List<string>();
        private readonly List<float> windowFrames = new List<float>();
        private int logErrors, logExceptions, kmcLogErrors, kmcLogWarnings, kmcLogLines, observationSequence;
        private bool loadCallback, completed;
        private int stage, frames, gcAtWindowStart;
        private string archivePath, archiveSha256Before, windowLabel;
        private UnitEntityData rider, mount, neighborRider, neighborMount;
        private Vector3 riderOrigin, mountOrigin, neighborRiderOrigin, neighborMountOrigin, destination;
        private UnitUseAbility shell;

        private NormalPlayerSession(UnityModManager.ModEntry modEntry, ObserverRequest request, string requestSha256, string evidenceRoot)
        {
            this.modEntry = modEntry; this.request = request; roles = request.NormalPlayer;
            this.requestSha256 = requestSha256; this.evidenceRoot = evidenceRoot;
        }

        public string RunId => request.RunId;

        internal static NormalPlayerSession Arm(UnityModManager.ModEntry modEntry, ObserverRequest request, string requestSha256, string evidenceRoot)
        {
            var session = new NormalPlayerSession(modEntry, request, requestSha256, evidenceRoot);
            // Routing first: every native save-root resolution lands in the owned
            // profile before any menu or KMC persistence hook can resolve a path.
            NativeSaveRouting.Install(request.ProfileRoot);
            Application.logMessageReceived += session.HandleLog;
            session.observations["routing"] = new JObject { ["savePath"] = NativeSaveRouting.SavesRoot, ["stashFolder"] = NativeSaveRouting.StashFolder };
            session.Write("armed", new JObject { ["observerVersion"] = ObserverIdentity.ProductVersion, ["mode"] = "normal-player" });
            return session;
        }

        public void Update()
        {
            if (completed) return;
            if (clock.Elapsed.TotalSeconds > request.TimeoutSeconds) { Fail("Normal-player observer timed out at stage " + stage + " after " + frames + " frames."); return; }
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
                        game.Player == null || game.CurrentMode != GameModeType.Default) { frames = 0; return; }
                    if (++frames < 30) return;
                    ObserveLoadedWorld(game);
                    return;
                case 2: if (CollectWindow()) { CloseWindow("idle-unmounted"); IssueAbility(game, rider, mount, roles.MountAbilityGuid, "mount"); stage = 3; } return;
                case 3: if (AwaitAbility("mount", attached: true)) { OpenWindow("idle-mounted"); stage = 4; } return;
                case 4: if (CollectWindow()) { CloseWindow("idle-mounted"); IssueGroundClick("mounted-move"); stage = 5; } return;
                case 5: if (AwaitMove("mounted-move", pairMoves: true)) { IssueAbility(game, rider, rider, roles.DismountAbilityGuid, "dismount"); stage = 6; } return;
                case 6: if (AwaitAbility("dismount", attached: false)) { IssueGroundClick("unmounted-move"); stage = 7; } return;
                case 7: if (AwaitMove("unmounted-move", pairMoves: false)) Finish(game); return;
            }
        }

        private void BeginLoad(Game game)
        {
            observations["presenceAtMenu"] = InspectPresence("menu");
            var savePath = game.SaveManager.SavePath;
            observations["routing"]["observedSavePath"] = savePath;
            observations["routing"]["observedStashFolder"] = NativeSaveRouting.ObservedStashFolder;
            Check(string.Equals(savePath, NativeSaveRouting.SavesRoot, StringComparison.Ordinal) &&
                string.Equals(NativeSaveRouting.ObservedStashFolder, NativeSaveRouting.StashFolder, StringComparison.Ordinal), "native-save-and-stash-paths-resolve-inside-the-owned-profile");
            archivePath = Path.Combine(savePath, request.Archive.FileName);
            var file = new FileInfo(archivePath);
            archiveSha256Before = file.Exists ? RemovalObserverSession.Sha256File(archivePath) : null;
            observations["profileBefore"] = ProfileListing();
            Check(file.Exists && (file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length == request.Archive.Length &&
                string.Equals(archiveSha256Before, request.Archive.Sha256, StringComparison.Ordinal), "fixture-archive-bytes-are-exactly-the-declared-copy");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            var descriptor = game.SaveManager.LoadZipSave(archivePath);
            var area = descriptor == null || descriptor.Area == null ? null : descriptor.Area.AssetGuidThreadSafe;
            Check(descriptor != null && descriptor.Name == request.Archive.InternalName && descriptor.Type == SaveInfo.SaveType.Manual &&
                descriptor.GameId == request.Archive.GameId && area == request.Archive.Area, "engine-reads-the-fixture-header-as-declared");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            game.SaveManager.AddCallbackAfterLoad(() => loadCallback = true);
            game.UI.MainMenu.LoadGame(descriptor);
            Write("load-requested", new JObject { ["archive"] = archivePath, ["sha256"] = archiveSha256Before });
            frames = 0; stage = 1;
        }

        private void ObserveLoadedWorld(Game game)
        {
            var units = game.State.Units.ToArray();
            rider = Find(units, roles.RiderId); mount = Find(units, roles.MountId);
            neighborRider = Find(units, roles.NeighborRiderId); neighborMount = Find(units, roles.NeighborMountId);
            var areaGuid = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
            observations["world"] = new JObject { ["gameId"] = game.Player.GameId, ["area"] = areaGuid, ["mode"] = game.CurrentMode.ToString(),
                ["party"] = game.Player.Party.Count, ["inCombat"] = game.Player.IsInCombat, ["paused"] = game.IsPaused,
                ["loadSeconds"] = clock.Elapsed.TotalSeconds };
            Check(game.Player.GameId == request.Archive.GameId && areaGuid == request.Archive.Area && !game.Player.IsInCombat, "the-fixture-opened-its-own-campaign-and-area-out-of-combat");
            Check(rider != null && mount != null && neighborRider != null && neighborMount != null &&
                rider.View != null && mount.View != null && neighborRider.View != null && neighborMount.View != null, "all-four-declared-roles-are-loaded-with-views");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            Check(mount.Descriptor.Master.Value == rider && neighborMount.Descriptor.Master.Value == neighborRider, "each-declared-Horse-is-its-rider's-own-native-pet");
            Check(AttachedTo(rider) == null && AttachedTo(neighborRider) == null && rider.Commands.Empty && mount.Commands.Empty, "the-fixture-starts-unmounted-and-idle");
            var mountFact = FindAbility(rider, roles.MountAbilityGuid);
            observations["controlsBefore"] = Controls("before");
            Check(mountFact != null && mountFact.Data != null, "the-rider-owns-the-ordinary-native-Mount-ability-without-any-grant");
            observations["presenceLoaded"] = InspectPresence("loaded");
            if (errors.Count != 0) { Complete("FAIL"); return; }
            if (game.IsPaused) game.IsPaused = false;
            riderOrigin = rider.Position; mountOrigin = mount.Position;
            neighborRiderOrigin = neighborRider.Position; neighborMountOrigin = neighborMount.Position;
            Write("loaded", new JObject { ["world"] = observations["world"].DeepClone() });
            OpenWindow("idle-unmounted"); stage = 2;
        }

        // Ordinary player input through the native selected-ability handler: the
        // same path a hotbar click arms and a world click completes.
        private void IssueAbility(Game game, UnitEntityData caster, UnitEntityData target, string guid, string label)
        {
            var fact = FindAbility(caster, guid);
            var handler = game.SelectedAbilityHandler;
            var row = new JObject { ["label"] = label, ["casterId"] = caster.UniqueId, ["targetId"] = target.UniqueId, ["abilityGuid"] = guid,
                ["factPresent"] = fact != null, ["atSeconds"] = clock.Elapsed.TotalSeconds };
            inputs.Add(row);
            if (fact == null || fact.Data == null || handler == null || target.View == null) { Fail("Rejected input: native " + label + " ability or handler was unavailable."); return; }
            SelectionManager.Instance.SelectUnit(caster.View, true, true, false);
            handler.SetAbility(fact.Data);
            bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            shell = caster.Commands.Raw.OfType<UnitUseAbility>().FirstOrDefault(c => c.Spell != null && c.Spell.Blueprint != null && c.Spell.Blueprint.AssetGuid == guid);
            row["clicked"] = clicked; row["commandCreated"] = shell != null;
            if (!clicked || shell == null) { Fail("Rejected input: native " + label + " click created no ability command."); return; }
            frames = 0;
            Write("input", row);
        }

        private bool AwaitAbility(string label, bool attached)
        {
            var finished = shell.IsFinished && (shell.ExecutionProcess == null || shell.ExecutionProcess.IsEnded);
            var anchor = AttachedTo(rider);
            var correct = attached ? anchor == mount && Horizontal(rider.Position, mount.Position) < 1.0f : anchor == null;
            if (!finished || !correct || !rider.Commands.Empty || !mount.Commands.Empty)
            {
                if (++frames > 1800)
                    Fail((finished ? (anchor != null && anchor != mount ? "Wrong target: rider attached to another actor" : "Terminal state wrong") : "Unresolved shell") +
                        " after native " + label + " (finished=" + finished + ", attachedTo=" + (anchor == null ? "none" : anchor.UniqueId) + ").");
                return false;
            }
            var row = (JObject)inputs[inputs.Count - 1];
            row["result"] = shell.Result.ToString(); row["settledFrames"] = frames;
            row["attachedTo"] = anchor == null ? null : anchor.UniqueId; row["riderMountDistance"] = Horizontal(rider.Position, mount.Position);
            row["controlsAfter"] = Controls(label);
            row["neighborUnchanged"] = NeighborUnchanged();
            Check(shell.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success, "native-" + label + "-command-completed-successfully");
            Check(attached ? anchor == mount : anchor == null, attached ? "the-rider-is-attached-to-its-own-Horse" : "the-rider-is-detached-after-Dismount");
            Check(NeighborUnchanged(), "the-neighboring-pair-is-untouched-after-" + label);
            if (attached) Check(FindAbility(rider, roles.DismountAbilityGuid) != null, "ordinary-Dismount-control-is-available-while-mounted");
            Write(label + "-settled", row);
            shell = null; frames = 0;
            return errors.Count == 0;
        }

        private void IssueGroundClick(string label)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var origin = rider.Position;
            destination = RemovalObserverSession.FindWalkableNear(origin, 4f);
            riderOrigin = origin; mountOrigin = mount.Position;
            ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
            var row = new JObject { ["label"] = label, ["moverId"] = rider.UniqueId, ["origin"] = Vec(origin), ["destination"] = Vec(destination),
                ["selected"] = new JArray(Game.Instance.UI.SelectionManager.SelectedUnits.Select(u => u.UniqueId)) };
            inputs.Add(row); frames = 0;
            Write("input", row);
        }

        private bool AwaitMove(string label, bool pairMoves)
        {
            var riderMoved = GeometryUtils.MechanicsDistance(rider.Position, riderOrigin);
            var mountMoved = GeometryUtils.MechanicsDistance(mount.Position, mountOrigin);
            var settled = rider.Commands.Empty && (!pairMoves || mount.Commands.Empty);
            if (riderMoved < 1.5f || !settled || (pairMoves && mountMoved < 1.5f))
            {
                if (++frames > 1800) Fail("Movement did not settle after " + label + " (rider " + riderMoved + " m, Horse " + mountMoved + " m, settled=" + settled + ").");
                return false;
            }
            var anchor = AttachedTo(rider);
            var row = (JObject)inputs[inputs.Count - 1];
            row["riderDisplacement"] = riderMoved; row["mountDisplacement"] = mountMoved; row["settledFrames"] = frames;
            row["attachedTo"] = anchor == null ? null : anchor.UniqueId; row["neighborUnchanged"] = NeighborUnchanged();
            row["destinationResidual"] = GeometryUtils.MechanicsDistance(rider.Position, destination);
            Check(pairMoves ? anchor == mount && mountMoved >= 1.5f : anchor == null, pairMoves ? "the-ground-click-moved-the-mounted-pair-together" : "the-unmounted-rider-moves-under-ordinary-authority");
            Check(NeighborUnchanged(), "the-neighboring-pair-is-untouched-after-" + label);
            Write(label + "-settled", row);
            frames = 0;
            return errors.Count == 0;
        }

        private void Finish(Game game)
        {
            observations["inputs"] = inputs;
            observations["controlsAfter"] = Controls("after");
            observations["presenceAfter"] = InspectPresence("after");
            var after = RemovalObserverSession.Sha256File(archivePath);
            observations["archive"] = new JObject { ["path"] = archivePath, ["sha256Before"] = archiveSha256Before, ["sha256After"] = after,
                ["changedOnDisk"] = !string.Equals(after, archiveSha256Before, StringComparison.Ordinal) };
            observations["profileAfter"] = ProfileListing();
            Check(string.Equals(after, archiveSha256Before, StringComparison.Ordinal), "the-fixture-archive-is-unchanged");
            Check(JToken.DeepEquals(observations["profileBefore"]["saves"], observations["profileAfter"]["saves"]), "no-save-file-was-written-or-changed");
            Check(kmcLogErrors == 0, "KMC-logged-no-error-or-exception-in-ordinary-play");
            Complete(errors.Count == 0 ? "PASS" : "FAIL");
        }

        // Read-only, by-name inspection of the loaded KMC assembly. Nothing is
        // invoked; fields are read to show the ordinary no-request composition.
        private JObject InspectPresence(string label)
        {
            var arguments = Environment.GetCommandLineArgs();
            var runtimeArguments = arguments.Where(a => KmcRuntimeArguments.Contains(a, StringComparer.OrdinalIgnoreCase)).ToArray();
            var entries = UnityModManager.modEntries.ToArray();
            var kmcEntry = entries.FirstOrDefault(e => e.Info.Id == request.KmcModId);
            var observerEntry = entries.FirstOrDefault(e => e.Info.Id == request.ObserverModId);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == request.KmcModId).ToArray();
            var kmc = assemblies.Length == 1 ? assemblies[0] : null;
            string location = null, sha = null, mvid = null;
            if (kmc != null) { location = kmc.Location; mvid = kmc.ManifestModule.ModuleVersionId.ToString(); if (!string.IsNullOrEmpty(location) && File.Exists(location)) sha = RemovalObserverSession.Sha256File(location); }
            var owners = NativeSaveRouting.PatchOwners();
            var kmcOwners = owners.Where(o => request.KmcHarmonyIds.Contains(o)).ToArray();
            object root = Static(kmc, "KingmakerMountedCombat.Main", "root");
            var settings = Field(root, "settings");
            var overlayType = kmc == null ? null : kmc.GetType("KingmakerMountedCombat.Integration.MountedPlayerActionOverlay");
            var overlays = overlayType == null ? -1 : UnityEngine.Object.FindObjectsOfType(overlayType).Length;
            var settingValues = new JObject();
            if (settings != null)
                foreach (var property in settings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(bool)).OrderBy(p => p.Name, StringComparer.Ordinal))
                    settingValues[property.Name] = (bool)property.GetValue(settings, null);
            var presence = new JObject {
                ["label"] = label, ["kmcRuntimeArguments"] = new JArray(runtimeArguments),
                ["modEntries"] = new JArray(entries.Select(e => e.Info.Id + (e.Active ? ":active" : ":inactive"))),
                ["kmcAssemblies"] = assemblies.Length, ["kmcLocation"] = location, ["kmcDllSha256"] = sha, ["kmcMvid"] = mvid,
                ["kmcHarmonyOwners"] = new JArray(kmcOwners), ["rootPresent"] = root != null,
                ["automationHostField"] = root == null ? "unavailable" : Field(root, "runtimeAutomation") == null ? "null" : "present",
                ["staticAutomationHost"] = Static(kmc, "KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost", "active") == null ? "null" : "present",
                ["measurementSession"] = Static(kmc, "KingmakerMountedCombat.Diagnostics.MountedRuntimeMeasurement", "current") == null ? "null" : "present",
                ["movementTelemetry"] = root == null ? "unavailable" : Field(root, "movementTelemetry") == null ? "null" : "present",
                ["overlayObjects"] = overlays, ["effectiveSettings"] = settingValues };
            var prefix = "at-" + label + "-";
            Check(runtimeArguments.Length == 0, prefix + "no-KMC-automation-argument-reached-the-process");
            Check(entries.Length == 2 && kmcEntry != null && kmcEntry.Active && observerEntry != null && observerEntry.Active,
                prefix + "UMM-runs-exactly-KMC-and-the-observer-both-active");
            Check(kmc != null && sha == request.Candidate.DllSha256 && mvid == request.Candidate.DllMvid, prefix + "the-loaded-KMC-assembly-is-the-candidate");
            Check(kmcOwners.Length != 0 && root != null, prefix + "KMC-integration-is-active");
            Check(root != null && Field(root, "runtimeAutomation") == null && (string)presence["staticAutomationHost"] == "null" &&
                (string)presence["measurementSession"] == "null" && Field(root, "movementTelemetry") == null, prefix + "no-automation-host-measurement-or-telemetry");
            Check(overlays == 0 && settings != null && settingValues["EnableDiagnosticOverlay"] != null && !(bool)settingValues["EnableDiagnosticOverlay"],
                prefix + "no-diagnostic-overlay-in-the-shipped-configuration");
            return presence;
        }

        private static object Static(Assembly assembly, string typeName, string field)
        {
            var type = assembly == null ? null : assembly.GetType(typeName);
            var info = type == null ? null : type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return info == null ? null : info.GetValue(null);
        }

        private static object Field(object instance, string field)
        {
            if (instance == null) return null;
            var info = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return info == null ? null : info.GetValue(instance);
        }

        private JObject Controls(string label)
        {
            JArray Facts(UnitEntityData unit) => new JArray(unit.Descriptor.Abilities.RawFacts.Where(f => f.Blueprint != null &&
                (f.Blueprint.AssetGuid == roles.MountAbilityGuid || f.Blueprint.AssetGuid == roles.DismountAbilityGuid)).Select(f => f.Blueprint.AssetGuid));
            JArray Slots(UnitEntityData unit)
            {
                var slots = unit.UISettings == null ? null : unit.UISettings.Slots;
                var result = new JArray();
                if (slots == null) return result;
                for (var i = 0; i < slots.Length; i++)
                {
                    var ability = slots[i] as MechanicActionBarSlotAbility;
                    if (ability != null && ability.Ability != null && ability.Ability.Blueprint != null)
                        result.Add(new JObject { ["index"] = i, ["abilityGuid"] = ability.Ability.Blueprint.AssetGuid });
                }
                return result;
            }
            return new JObject { ["label"] = label, ["riderFacts"] = Facts(rider), ["riderAbilitySlots"] = Slots(rider),
                ["selected"] = new JArray(Game.Instance.UI.SelectionManager.SelectedUnits.Select(u => u.UniqueId)) };
        }

        private static Kingmaker.UnitLogic.Abilities.Ability FindAbility(UnitEntityData unit, string guid) =>
            unit.Descriptor.Abilities.RawFacts.OfType<Kingmaker.UnitLogic.Abilities.Ability>().FirstOrDefault(f => f.Blueprint != null && f.Blueprint.AssetGuid == guid);

        private static UnitEntityData Find(UnitEntityData[] units, string id)
        {
            var found = units.Where(u => u.UniqueId == id).ToArray();
            return found.Length == 1 ? found[0] : null;
        }

        // The rider's view hangs under a KMC position anchor parented to exactly
        // one mount view; the actor owning that view is the attachment target.
        private UnitEntityData AttachedTo(UnitEntityData unit)
        {
            if (unit.View == null) return null;
            var sawAnchor = false;
            for (var t = unit.View.transform.parent; t != null; t = t.parent)
            {
                if (t.name == AnchorName) sawAnchor = true;
                var view = t.GetComponent<Kingmaker.View.UnitEntityView>();
                if (view != null && view != unit.View) return sawAnchor ? view.EntityData : null;
            }
            return null;
        }

        private bool NeighborUnchanged() =>
            GeometryUtils.MechanicsDistance(neighborRider.Position, neighborRiderOrigin) < 0.25f &&
            GeometryUtils.MechanicsDistance(neighborMount.Position, neighborMountOrigin) < 0.25f && AttachedTo(neighborRider) == null;

        private void OpenWindow(string label) { windowLabel = label; windowFrames.Clear(); gcAtWindowStart = GC.CollectionCount(0); frames = 0; }

        private bool CollectWindow()
        {
            windowFrames.Add(Time.unscaledDeltaTime * 1000f);
            return windowFrames.Count >= roles.IdleWindowFrames;
        }

        private void CloseWindow(string label)
        {
            var sorted = windowFrames.OrderBy(v => v).ToArray();
            var windows = observations["frameWindows"] as JArray ?? new JArray();
            windows.Add(new JObject { ["label"] = label, ["frames"] = sorted.Length, ["meanMs"] = sorted.Average(),
                ["p50Ms"] = sorted[sorted.Length / 2], ["p95Ms"] = sorted[(int)(sorted.Length * 0.95)], ["maxMs"] = sorted[sorted.Length - 1],
                ["gen0Collections"] = GC.CollectionCount(0) - gcAtWindowStart, ["kmcLogLinesSoFar"] = kmcLogLines });
            observations["frameWindows"] = windows;
        }

        private JObject ProfileListing()
        {
            var saves = new JArray();
            foreach (var path in Directory.GetFiles(NativeSaveRouting.SavesRoot).OrderBy(p => p, StringComparer.Ordinal))
            {
                var info = new FileInfo(path);
                saves.Add(new JObject { ["name"] = info.Name, ["length"] = info.Length, ["lastWriteTicks"] = info.LastWriteTimeUtc.Ticks });
            }
            return new JObject { ["saves"] = saves };
        }

        public void Fail(string reason)
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
            observations["log"] = new JObject { ["errors"] = logErrors, ["exceptions"] = logExceptions, ["first"] = new JArray(firstLogErrors),
                ["kmcLines"] = kmcLogLines, ["kmcWarnings"] = kmcLogWarnings, ["kmcErrors"] = kmcLogErrors, ["kmcFirst"] = new JArray(kmcLog) };
            if (observations["inputs"] == null) observations["inputs"] = inputs;
            var passed = checks.Count(c => (bool)c["passed"]);
            var result = new JObject {
                ["schemaVersion"] = 1, ["evidenceKind"] = "kmc-normal-player-observer", ["runId"] = request.RunId, ["transactionToken"] = request.TransactionToken,
                ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["startedAtUtc"] = startedAtUtc.ToString("o"),
                ["completedAtUtc"] = DateTime.UtcNow.ToString("o"), ["status"] = status, ["observerVersion"] = ObserverIdentity.ProductVersion,
                ["observerAssembly"] = typeof(NormalPlayerSession).Assembly.GetName().Name, ["requestSha256"] = requestSha256, ["stage"] = stage,
                ["checks"] = new JArray(checks), ["checkPassCount"] = passed, ["checkFailCount"] = checks.Count - passed,
                ["errors"] = new JArray(errors), ["observations"] = observations };
            try { RemovalObserverSession.WriteJsonCreateNew(Path.Combine(evidenceRoot, ResultFileName), result); }
            catch (Exception exception) { modEntry.Logger.LogException("Result write", exception); }
            modEntry.Logger.Log("KMC normal-player observer completed: " + status + " (" + passed + "/" + (checks.Count - passed) + ").");
            try { Application.Quit(); } catch (Exception exception) { modEntry.Logger.LogException("Quit", exception); }
        }

        private void Check(bool passed, string name)
        {
            checks.Add(new JObject { ["name"] = name, ["passed"] = passed });
            if (!passed) errors.Add("Check failed: " + name);
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            var text = condition ?? string.Empty;
            var kmcRelated = text.IndexOf("KingmakerMountedCombat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (stackTrace ?? string.Empty).IndexOf("KingmakerMountedCombat", StringComparison.Ordinal) >= 0;
            if (kmcRelated)
            {
                kmcLogLines++;
                if (type == LogType.Warning) kmcLogWarnings++;
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) kmcLogErrors++;
                if (kmcLog.Count < 20) kmcLog.Add(type + ": " + (text.Length > 300 ? text.Substring(0, 300) : text));
            }
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            logErrors++;
            if (type == LogType.Exception) logExceptions++;
            if (firstLogErrors.Count < 10) firstLogErrors.Add(type + ": " + (text.Length > 400 ? text.Substring(0, 400) : text));
        }

        private void Write(string kind, JObject detail)
        {
            try
            {
                var row = new JObject { ["sequence"] = ++observationSequence, ["kind"] = kind, ["runId"] = request.RunId,
                    ["atUtc"] = DateTime.UtcNow.ToString("o"), ["stage"] = stage, ["detail"] = detail };
                File.AppendAllText(Path.Combine(evidenceRoot, ObservationsFileName), row.ToString(Newtonsoft.Json.Formatting.None) + "\n", new System.Text.UTF8Encoding(false));
            }
            catch (Exception exception) { modEntry.Logger.LogException("Observation write", exception); }
        }

        private static float Horizontal(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
        private static JArray Vec(Vector3 v) => new JArray(v.x, v.y, v.z);
    }
}
