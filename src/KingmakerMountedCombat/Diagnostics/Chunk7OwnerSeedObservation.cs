using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using NativeDialogController = Kingmaker.Controllers.Dialog.DialogController;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observation only, installed before the owner-seed load and released at
    // cleanup. Three installed boundaries are observed; no argument, return value
    // or exception is rewritten, swallowed or added:
    //  - Assembly-CSharp ResourcesLibrary.TryGetBlueprint(string) 06009708: a null
    //    result for a non-empty id is a blueprint the library could not resolve;
    //  - Newtonsoft.Json DefaultSerializationBinder.BindToType(string,string)
    //    060004C8: a call whose postfix never ran (the next call on that thread
    //    began first, an exception was logged on it, or none followed) threw;
    //  - Assembly-CSharp-firstpass UberDebug.LogException(Exception,Object)
    //    06000664 and LogExceptionChannel 06000665: the game's own log of the
    //    exceptions ThreadedGameLoader catches (it swallows a failed party restore).
    internal sealed class Chunk7OwnerSeedLoadProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.OwnerSeedLoad";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal const int Capacity = 256;
        private static Chunk7OwnerSeedLoadProbe active;
        private readonly object sync = new object();
        private readonly List<string> missing = new List<string>(), failedBinds = new List<string>();
        private readonly Dictionary<int, string> pendingBinds = new Dictionary<int, string>();
        private readonly JArray logged = new JArray(), hooks = new JArray();
        private HarmonyInstance harmony;
        private bool installed, installationStarted, disposed;
        private int failures, dropped, loggedCount, failedBindCount;
        internal bool Installed => installed;
        internal int FailureCount { get { lock (sync) return failures; } }
        internal int Dropped { get { lock (sync) return dropped; } }

        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Chunk7OwnerSeedLoadProbe));
            if (installed) return;
            if (installationStarted || active != null) throw new InvalidOperationException("Owner-seed load observer already owns or attempted installation.");
            installationStarted = true;
            if (Game.Instance?.CurrentlyLoadedArea != null || typeof(ResourcesLibrary).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                typeof(UberDebug).Module.ModuleVersionId != new Guid("57f03756-55de-42f5-8bb3-e983306082b2") ||
                typeof(Newtonsoft.Json.Serialization.DefaultSerializationBinder).Module.ModuleVersionId != new Guid("4f213745-d60e-425f-b763-df319c826ddf"))
                throw new InvalidOperationException("Owner-seed load observation requires the exact pre-load native modules.");
            var resolve = typeof(ResourcesLibrary).GetMethod("TryGetBlueprint", Flags, null, new[] { typeof(string) }, null);
            var bind = typeof(Newtonsoft.Json.Serialization.DefaultSerializationBinder).GetMethod("BindToType", Flags, null, new[] { typeof(string), typeof(string) }, null);
            var log = typeof(UberDebug).GetMethod("LogException", Flags, null, new[] { typeof(Exception), typeof(UnityEngine.Object) }, null);
            var channel = typeof(UberDebug).GetMethod("LogExceptionChannel", Flags, null, new[] { typeof(string), typeof(Exception), typeof(UnityEngine.Object) }, null);
            if (resolve == null || resolve.MetadataToken != 0x06009708 || resolve.ReturnType != typeof(BlueprintScriptableObject) ||
                bind == null || bind.MetadataToken != 0x060004C8 || log == null || log.MetadataToken != 0x06000664 ||
                channel == null || channel.MetadataToken != 0x06000665)
                throw new MissingMethodException("Exact native blueprint, binder or exception-log contract changed.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(resolve, null, nameof(ResolveAfter));
            Patch(bind, nameof(BindBefore), nameof(BindAfter));
            Patch(log, nameof(LogBefore), null);
            Patch(channel, nameof(ChannelBefore), null);
            installed = true;
        }
        private void Patch(MethodInfo method, string prefix, string postfix)
        {
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Chunk7OwnerSeedLoadProbe).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                postfix == null ? null : new HarmonyMethod(typeof(Chunk7OwnerSeedLoadProbe).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["declaringType"] = method.DeclaringType.FullName, ["method"] = method.Name,
                ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        private static void ResolveAfter(string assetId, BlueprintScriptableObject __result)
        {
            var probe = active;
            if (probe == null || __result != null || string.IsNullOrEmpty(assetId)) return;
            lock (probe.sync)
            {
                probe.failures++;
                if (probe.missing.Contains(assetId)) return;
                if (probe.missing.Count >= Capacity) { probe.dropped++; return; }
                probe.missing.Add(Bound(assetId, 64));
            }
        }
        private static void BindBefore(string assemblyName, string typeName)
        {
            var probe = active; if (probe == null) return;
            var thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            lock (probe.sync)
            {
                if (probe.pendingBinds.TryGetValue(thread, out var prior)) probe.FailedBind(prior);
                probe.pendingBinds[thread] = Bound(typeName) + ", " + Bound(assemblyName);
            }
        }
        private static void BindAfter()
        {
            var probe = active; if (probe == null) return;
            lock (probe.sync) probe.pendingBinds.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
        }
        private void FailedBind(string value)
        {
            failedBindCount++;
            if (!failedBinds.Contains(value)) { if (failedBinds.Count < Capacity) failedBinds.Add(value); else dropped++; }
        }
        private static void LogBefore(Exception ex) => active?.Logged("UberDebug.LogException", ex);
        private static void ChannelBefore(Exception ex) => active?.Logged("UberDebug.LogExceptionChannel", ex);
        private void Logged(string source, Exception ex)
        {
            lock (sync)
            {
                loggedCount++;
                var thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                string pending;
                if (pendingBinds.TryGetValue(thread, out pending)) { pendingBinds.Remove(thread); FailedBind(pending); }
                if (logged.Count >= 64) { dropped++; return; }
                var chain = new JArray();
                for (var current = ex; current != null && chain.Count < 8; current = current.InnerException)
                    chain.Add(new JObject { ["type"] = current.GetType().FullName, ["message"] = Bound(current.Message, 2048) });
                logged.Add(new JObject { ["source"] = source, ["thread"] = thread, ["pendingBind"] = pending, ["exceptionChain"] = chain });
            }
        }
        private static string Bound(string value, int length = 512) => value == null ? null : value.Length > length ? value.Substring(0, length) : value;
        internal string[] UnresolvedBlueprints { get { lock (sync) return missing.ToArray(); } }
        internal string[] FailedBinds { get { lock (sync) return failedBinds.Concat(pendingBinds.Values.Where(value => !failedBinds.Contains(value))).Take(Capacity).ToArray(); } }
        internal string[] LoggedMessages { get { lock (sync) return logged.SelectMany(row => ((JArray)row["exceptionChain"]).Select(item => (string)item["type"] + ": " + (string)item["message"])).ToArray(); } }
        internal int LoggedCount { get { lock (sync) return loggedCount; } }
        internal JObject Capture()
        {
            lock (sync) return new JObject { ["contract"] = "owner-seed-native-load-observation-v1", ["installed"] = installed,
                ["capacity"] = Capacity, ["blueprintFailureCount"] = failures, ["dropped"] = dropped,
                ["unresolvedBlueprintIds"] = new JArray(missing.ToArray()), ["failedTypeBindCount"] = failedBindCount + pendingBinds.Count,
                ["failedTypeBinds"] = new JArray(failedBinds.Concat(pendingBinds.Values.Where(value => !failedBinds.Contains(value))).Take(Capacity).ToArray()),
                ["loggedExceptionCount"] = loggedCount, ["loggedExceptions"] = logged.DeepClone(), ["hooks"] = hooks.DeepClone() };
        }
        public void Dispose()
        {
            if (disposed) return;
            harmony?.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
    }


    // Owner-seed qualification reads only installed getters and fields. It
    // certifies nothing that it did not observe in this one cold load.
    internal static class Chunk7OwnerSeedObservation
    {
        internal const string Contract = "owner-seed-cold-qualification-v1";
        // Same installed-field contract as the assisted story boundary: exact token,
        // declared type, instance field and module MVID, read only.
        private static T ReadField<T>(Type type, object owner, string name, int token)
        {
            if (type.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")) throw new InvalidOperationException("Installed owner-seed module changed.");
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null || field.MetadataToken != token || field.FieldType != typeof(T) || field.IsStatic) throw new InvalidOperationException("Installed owner-seed field changed: " + name);
            return (T)field.GetValue(owner);
        }
        private static readonly System.Text.RegularExpressions.Regex QualifiedTypeName = new System.Text.RegularExpressions.Regex(
            "\"([A-Za-z_][A-Za-z0-9_+`]{0,127}(?:\\.[A-Za-z_][A-Za-z0-9_+`]{0,127}){1,16}, [A-Za-z_][A-Za-z0-9_.-]{0,127})\"", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        // Read-only: a clone of the admitted descriptor's native ZipSaver (ISaver.Clone) reads party.json and the loaded area's state
        // from the run's own copied archive (never the master); nothing is saved.
        // Every quoted "Type, Assembly" ("$type" values and Type-keyed dictionary
        // keys) whose assembly is not loaded, or whose type that assembly lacks, is
        // state the game could not have materialized.
        internal static JObject DeclaredForeignTypes(SaveInfo descriptor, string archivePath, string area)
        {
            var rows = new Dictionary<string, JObject>(StringComparer.Ordinal);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().GroupBy(value => value.GetName().Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var entries = new JArray(); string error = null;
            try
            {
                if (descriptor?.Saver == null || string.IsNullOrEmpty(descriptor.FolderName) || !string.Equals(Path.GetFullPath(descriptor.FolderName), Path.GetFullPath(archivePath), StringComparison.OrdinalIgnoreCase) || descriptor.Saver.GetType().FullName != "Kingmaker.EntitySystem.Persistence.ZipSaver")
                    throw new InvalidOperationException("The admitted descriptor does not own the run's copied archive.");
                using (var saver = descriptor.Saver.Clone())
                    foreach (var entry in new[] { "party", area })
                    {
                        var json = saver.ReadJson(entry);
                        entries.Add(new JObject { ["entry"] = entry + ".json", ["present"] = json != null, ["length"] = json?.Length ?? 0 });
                        if (json == null) continue;
                        foreach (System.Text.RegularExpressions.Match match in QualifiedTypeName.Matches(json))
                        {
                            if (!Chunk7OwnerSeedFailureClassifier.TrySplitTypeName(match.Groups[1].Value, out var type, out var assembly)) continue;
                            Assembly loaded;
                            if (assemblies.TryGetValue(assembly, out loaded) && loaded.GetType(type, false) != null) continue;
                            var key = type + ", " + assembly + "|" + entry;
                            if (!rows.TryGetValue(key, out var row))
                            {
                                if (rows.Count >= 256) { error = "Declared foreign type inventory exceeded its 256-row bound."; continue; }
                                rows[key] = row = new JObject { ["type"] = type + ", " + assembly, ["assemblyLoaded"] = loaded != null, ["entry"] = entry + ".json", ["count"] = 0 };
                            }
                            row["count"] = (int)row["count"] + 1;
                        }
                    }
            }
            catch (Exception exception) { error = exception.GetType().FullName + ": " + exception.Message; }
            return new JObject { ["entriesRead"] = entries, ["types"] = new JArray(rows.Values), ["error"] = error,
                ["limits"] = "Type-level, not per-unit: counts occurrences per archive entry; only party.json and the loaded area's state are read." };
        }

        internal static JObject Capture(Game game, UnitEntityData[] actors, UnitEntityData[] characters, SaveInfo descriptor,
            string[] headerDlc, Chunk7OwnerSeedLoadProbe resolution, MountedPersistenceService persistence, bool nativeBusy, string archivePath)
        {
            var failures = new List<string>();
            var main = game.Player.MainCharacter.Value;
            var party = new JArray(); var unresolvedFacts = new JArray(); var unresolvedItems = new JArray();
            foreach (var actor in actors)
            {
                var d = actor.Descriptor;
                var collections = new[] {
                    new KeyValuePair<string, FactCollection>("features", d.Progression.Features),
                    new KeyValuePair<string, FactCollection>("buffs", d.Buffs),
                    new KeyValuePair<string, FactCollection>("abilities", d.Abilities),
                    new KeyValuePair<string, FactCollection>("activatableAbilities", d.ActivatableAbilities),
                    new KeyValuePair<string, FactCollection>("logic", d.Logic) };
                var counts = new JObject(); var total = 0;
                foreach (var pair in collections)
                {
                    if (pair.Value == null) { failures.Add("Actor " + actor.UniqueId + " has no native " + pair.Key + " collection."); counts[pair.Key] = null; continue; }
                    var raw = pair.Value.RawFacts;
                    counts[pair.Key] = raw.Count; total += raw.Count;
                    for (var i = 0; i < raw.Count && unresolvedFacts.Count < 256; i++)
                        if (raw[i] == null || raw[i].Blueprint == null)
                            unresolvedFacts.Add(new JObject { ["actorId"] = actor.UniqueId, ["collection"] = pair.Key, ["index"] = i });
                }
                counts["total"] = total;
                var equipment = new JArray(); var slotIndex = 0;
                foreach (var slot in d.Body.AllSlots.Take(129))
                {
                    var item = slot?.HasItem == true ? slot.Item : null;
                    var guid = item?.Blueprint?.AssetGuid;
                    equipment.Add(new JObject { ["index"] = slotIndex, ["slotType"] = slot?.GetType().Name, ["hasItem"] = item != null, ["itemBlueprint"] = guid });
                    if (item != null && guid == null && unresolvedItems.Count < 256)
                        unresolvedItems.Add(new JObject { ["actorId"] = actor.UniqueId, ["slotIndex"] = slotIndex });
                    slotIndex++;
                }
                if (slotIndex > 128) failures.Add("Actor " + actor.UniqueId + " exceeds the 128-slot equipment bound.");
                party.Add(new JObject {
                    ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint?.AssetGuid, ["name"] = actor.CharacterName,
                    ["isMain"] = ReferenceEquals(actor, main), ["isCharacter"] = characters.Contains(actor),
                    ["petId"] = d.Pet?.UniqueId, ["masterId"] = d.Master.Value?.UniqueId,
                    ["characterLevel"] = d.Progression.CharacterLevel, ["experience"] = d.Progression.Experience,
                    ["classes"] = new JArray(d.Progression.Classes.Take(21).Where(value => value?.CharacterClass != null)
                        .OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                        .Select(value => new JObject { ["guid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })),
                    ["alive"] = !d.State.IsDead, ["conscious"] = d.State.IsConscious, ["inGame"] = actor.IsInGame, ["inState"] = actor.IsInState,
                    ["outOfCombat"] = !actor.IsInCombat, ["commandsEmpty"] = actor.Commands.Empty, ["queuedCommands"] = actor.Commands.Queue.Count,
                    ["directlyControllable"] = actor.IsDirectlyControllable, ["factCounts"] = counts, ["equipment"] = equipment });
            }
            var sharedItems = game.Player.Inventory?.Items;
            if (sharedItems == null) failures.Add("Native shared inventory is unavailable.");
            else for (var i = 0; i < sharedItems.Count && unresolvedItems.Count < 256; i++)
                if (sharedItems[i] == null || sharedItems[i].Blueprint == null) unresolvedItems.Add(new JObject { ["actorId"] = null, ["inventoryIndex"] = i });
            var resolutionTrace = resolution.Capture();
            var unresolved = new JObject {
                ["method"] = "structural walk of every party actor's native fact collections, equipment slots and the shared inventory for null blueprints; every native ResourcesLibrary.TryGetBlueprint miss, failed Newtonsoft type bind and game-logged exception from before the load until qualification; and the copied archive's declared serialized types whose assembly or type is absent from this process",
                ["blueprintResolution"] = resolutionTrace, ["nullBlueprintFacts"] = unresolvedFacts, ["nullBlueprintItems"] = unresolvedItems,
                ["total"] = resolution.FailureCount + unresolvedFacts.Count + unresolvedItems.Count };
            if (!resolution.Installed) failures.Add("Blueprint resolution observer was not installed before load.");
            if (resolution.FailureCount != 0 || resolution.Dropped != 0)
                failures.Add("The native library failed to resolve " + resolution.FailureCount + " blueprint request(s) during the load: " + string.Join(", ", ((JArray)resolutionTrace["unresolvedBlueprintIds"]).Select(value => (string)value)));
            if (unresolvedFacts.Count != 0) failures.Add("Party facts lost their blueprint: " + unresolvedFacts.ToString(Newtonsoft.Json.Formatting.None));
            if (unresolvedItems.Count != 0) failures.Add("Party items lost their blueprint: " + unresolvedItems.ToString(Newtonsoft.Json.Formatting.None));
            var failedBinds = resolution.FailedBinds;
            if (failedBinds.Length != 0) failures.Add("The native deserializer could not bind serialized type(s): " + string.Join("; ", failedBinds));
            if (resolution.LoggedCount != 0) failures.Add("The game logged " + resolution.LoggedCount + " exception(s) during the load: " + string.Join(" | ", resolution.LoggedMessages.Take(8)));
            var declared = DeclaredForeignTypes(descriptor, archivePath, game.CurrentlyLoadedArea.AssetGuidThreadSafe);
            unresolved["declaredForeignTypes"] = declared;
            var declaredNames = ((JArray)declared["types"]).Select(row => (string)row["type"]).ToArray();
            if (declared["error"].Type != JTokenType.Null) failures.Add("The copied archive's declared types could not be read: " + (string)declared["error"]);
            if (declaredNames.Length != 0) failures.Add("The seed declares serialized state of types absent from this configuration, so it cannot have loaded (lost content): " +
                string.Join("; ", ((JArray)declared["types"]).Select(row => (string)row["type"] + " x" + (int)row["count"] + " in " + (string)row["entry"])));
            unresolved["total"] = (int)unresolved["total"] + failedBinds.Length + resolution.LoggedCount + declaredNames.Length;
            unresolved["classification"] = Chunk7OwnerSeedFailureClassifier.Classify(declaredNames.Concat(failedBinds), resolution.UnresolvedBlueprints,
                resolution.LoggedMessages, false);

            // DLC: the game's own verdicts (DlcRoot.Get 0600977D, DlcData.Enabled 0600C538,
            // DlcData.IsRequiredInSaves 04009276, DlcRoot.ListForSaves 0600977F,
            // SaveInfo.DLC 06007FFD and SaveInfo.DlcRequired 06008003). Entitlement is never bypassed.
            var settings = BlueprintRoot.Instance?.DlcSettings;
            var dlcEntries = new JArray(); var unavailable = new JArray();
            var listForSaves = settings?.ListForSaves;
            foreach (var name in headerDlc)
            {
                DlcType type;
                if (settings == null || name == null || !Enum.TryParse(name, false, out type) || !Enum.IsDefined(typeof(DlcType), type) || type == DlcType.None || type.ToString() != name)
                { unavailable.Add(name); dlcEntries.Add(new JObject { ["dlc"] = name, ["recognized"] = false }); continue; }
                var data = settings.Get(type); var enabled = data != null && data.Enabled;
                dlcEntries.Add(new JObject { ["dlc"] = name, ["recognized"] = true, ["enabled"] = enabled,
                    ["requiredInSaves"] = data != null && data.IsRequiredInSaves, ["inListForSaves"] = listForSaves != null && listForSaves.Contains(type) });
                if (!enabled) unavailable.Add(name);
            }
            var descriptorDlc = descriptor.DLC == null ? null : descriptor.DLC.Take(9).Select(value => value.ToString()).ToArray();
            var dlcRequired = descriptor.DlcRequired;
            var dlc = new JObject { ["headerDlc"] = new JArray(headerDlc), ["loadedDescriptorDlc"] = descriptorDlc == null ? null : new JArray(descriptorDlc),
                ["descriptorDlcRequired"] = dlcRequired, ["entries"] = dlcEntries, ["unavailable"] = unavailable };
            if (unavailable.Count != 0) failures.Add("Required DLC unavailable on this installation: " + string.Join(", ", unavailable.Select(value => (string)value)));
            if (dlcRequired) failures.Add("The game reports that the loaded save requires DLC this installation does not provide.");
            if (descriptorDlc == null || !descriptorDlc.SequenceEqual(headerDlc, StringComparer.Ordinal))
                failures.Add("The loaded descriptor's DLC list differs from the registered seed header.");

            // Control: input owner, view and selection readiness; nothing is clicked or selected.
            var builder = game.UI == null ? null : game.UI.CharacterBuildController;
            var dialogController = ReadField<NativeDialogController>(typeof(Game), game, "DialogController", 0x040006C7);
            var cuePlay = dialogController != null && ReadField<bool>(typeof(NativeDialogController), dialogController, "m_CuePlayScheduled", 0x04005E6F);
            var dialogStop = dialogController != null && ReadField<bool>(typeof(NativeDialogController), dialogController, "m_DialogStopScheduled", 0x04005E70);
            var popup = DialogMessageBox.Instance != null && DialogMessageBox.Instance.IsShown;
            var control = new JObject {
                ["pcController"] = game.IsControllerMouse, ["clickEventsController"] = game.ClickEventsController != null,
                ["selectionManager"] = game.UI != null && game.UI.SelectionManagerPC != null,
                ["mainViewReady"] = main.View != null && main.View.AgentASP != null,
                ["partySelectable"] = characters.All(value => value.IsDirectlyControllable && value.View != null && value.IsInGame),
                ["characterBuilderIdle"] = builder != null && builder.LevelUpController == null && builder.Unit == null,
                ["mode"] = game.CurrentMode.ToString(), ["cutscene"] = game.CutsceneLock, ["dialogActive"] = game.IsModeActive(GameModeType.Dialog),
                ["dialogScheduled"] = game.Player.Dialog?.Scheduled != null, ["cuePlayScheduled"] = cuePlay, ["dialogStopScheduled"] = dialogStop,
                ["popupShown"] = popup, ["inCombat"] = game.Player.IsInCombat, ["nativeLoading"] = nativeBusy,
                ["kmcAreaTransitionPending"] = persistence.AreaTransitionPending, ["paused"] = game.IsPaused };
            foreach (var key in new[] { "pcController", "clickEventsController", "selectionManager", "mainViewReady", "partySelectable", "characterBuilderIdle" })
                if (!(bool)control[key]) failures.Add("Party control is not ready: " + key + " is false.");
            foreach (var key in new[] { "cutscene", "dialogActive", "dialogScheduled", "cuePlayScheduled", "dialogStopScheduled", "popupShown", "inCombat", "nativeLoading", "kmcAreaTransitionPending" })
                if ((bool)control[key]) failures.Add("A blocking native state is present: " + key + ".");
            if (game.CurrentMode != GameModeType.Default) failures.Add("Native mode is not Default.");

            var kind = persistence.LoadedArchiveKind;
            var metadata = new JObject { ["kind"] = kind?.ToString(), ["canRestore"] = persistence.LoadedArchiveCanRestore,
                ["loadedArchivePresent"] = persistence.LoadedArchive != null, ["mountedRestorationProven"] = false,
                ["statement"] = "The seed carries no KMC metadata; the supported missing-metadata path selected no archive. This proves no mounted restoration." };
            if (kind != MountedSaveReadKind.Missing || persistence.LoadedArchiveCanRestore || persistence.LoadedArchive != null)
                failures.Add("KMC metadata is not the supported Missing path.");

            return new JObject { ["contract"] = Contract, ["qualified"] = failures.Count == 0, ["failures"] = new JArray(failures),
                ["identity"] = new JObject { ["gameId"] = game.Player.GameId, ["gameName"] = main.CharacterName, ["mainActorId"] = main.UniqueId,
                    ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["descriptorType"] = descriptor.Type.ToString(),
                    ["compatibilityVersion"] = descriptor.CompatibilityVersion },
                ["party"] = party, ["unresolvedContent"] = unresolved, ["dlc"] = dlc, ["control"] = control, ["kmcMetadata"] = metadata };
        }
    }
}
