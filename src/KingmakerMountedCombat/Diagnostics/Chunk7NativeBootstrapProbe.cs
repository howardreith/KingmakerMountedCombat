using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Pure observation of the real authored new-game call and its native first
    // autosave. The caller owns this object before the fallible Install method.
    internal sealed class Chunk7NativeBootstrapProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.NativeMainBootstrap";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Chunk7NativeBootstrapProbe active;
        private readonly Game game;
        private readonly Player menuPlayer;
        private readonly SaveManager saveManager;
        private readonly BlueprintAreaPreset preset;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(128);
        private readonly List<string> errors = new List<string>();
        private readonly JArray hooks = new JArray(), events = new JArray();
        private HarmonyInstance harmony;
        private bool disposed, installed, installationStarted, insideCall;
        private int entryFrame = -1, entered, exited, minted, saveRequests, prepared;
        internal Player NativePlayer { get; private set; }
        internal string NativeGameId { get; private set; }
        internal SaveInfo NativeSave { get; private set; }
        internal bool InSynchronousCallback => insideCall && entryFrame == Time.frameCount;
        internal bool Complete => installed && errors.Count == 0 && identities.FaultCount == 0 && entered == 1 && exited == 1 && minted == 1 && saveRequests == 1 && prepared == 1;
        internal bool IdentityReleased => identities.Released;

        internal Chunk7NativeBootstrapProbe(Game game, BlueprintAreaPreset preset)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.preset = preset ?? throw new ArgumentNullException(nameof(preset));
            menuPlayer = game.Player; saveManager = game.SaveManager;
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Chunk7NativeBootstrapProbe));
            if (installed) return;
            if (installationStarted || active != null) throw new InvalidOperationException("Native bootstrap observer already owns or attempted installation.");
            installationStarted = true;
            // Native contract validation; acquisition tests substitute only metadata/Harmony boundaries.
            if (typeof(Game).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native bootstrap observer requires the exact installed gameplay module.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(Game), "LoadNewGame", 0x06000CDC, new[] { typeof(BlueprintAreaPreset), typeof(SaveInfo) }, "NewGameBefore", "NewGameAfter");
            Patch(typeof(Player), "set_GameId", 0x06000DE3, new[] { typeof(string) }, null, "GameIdAfter");
            Patch(typeof(SaveManager), "SaveRoutine", 0x06008029, new[] { typeof(SaveInfo), typeof(bool) }, "SaveBefore", null);
            Patch(typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "PreparedAfter");
            installed = true;
        }
        private void Patch(Type type, string name, int token, Type[] parameters, string prefix, string postfix)
        {
            var method = type.GetMethod(name, Flags, null, parameters, null);
            if (method == null || method.MetadataToken != token) throw new MissingMethodException(type.FullName, name);
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = name, ["token"] = token.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        internal int Identity(object value) => identities.Get(value);
        internal JObject Capture() => new JObject { ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(), ["errors"] = new JArray(errors),
            ["installed"] = installed, ["installationAttempted"] = installationStarted, ["entered"] = entered, ["exited"] = exited,
            ["minted"] = minted, ["saveRequests"] = saveRequests, ["prepared"] = prepared, ["complete"] = Complete,
            ["identities"] = new JObject { ["contract"] = "retained-native-object-identity-v1", ["capacity"] = identities.Capacity, ["ids"] = new JArray(identities.Ids),
                ["retainedCount"] = identities.RetainedCount, ["faults"] = identities.FaultCount, ["released"] = identities.Released } };
        private void Safe(Action action)
        {
            try { action(); } catch (Exception error) { var value = error.ToString(); if (!errors.Contains(value)) errors.Add(value); }
        }
        private void Record(string kind, SaveInfo save = null)
        {
            if (events.Count >= 16) throw new InvalidOperationException("Native bootstrap observer exceeded its bounded event inventory.");
            events.Add(new JObject { ["sequence"] = events.Count + 1, ["kind"] = kind, ["frame"] = Time.frameCount,
                ["game"] = Identity(Game.Instance), ["saveManager"] = Identity(saveManager), ["player"] = Identity(game.Player), ["menuPlayer"] = Identity(menuPlayer),
                ["sameMenuPlayer"] = ReferenceEquals(game.Player, menuPlayer), ["gameId"] = game.Player?.GameId, ["mainActorId"] = game.Player?.MainCharacter.Value?.UniqueId,
                ["preset"] = Identity(preset), ["presetGuid"] = preset.AssetGuid, ["insideNativeNewGame"] = InSynchronousCallback,
                ["save"] = Identity(save), ["saveType"] = save?.Type.ToString(), ["saveLeaf"] = save?.FileName,
                ["saveName"] = save?.Name, ["savePath"] = save?.FolderName, ["saveArea"] = save?.Area?.AssetGuidThreadSafe,
                ["saveGameId"] = save?.GameId, ["saveGameName"] = save?.GameName, ["autoLevelup"] = save?.IsAutoLevelupSave });
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static class Hooks
        {
            internal static void NewGameBefore(Game __instance, BlueprintAreaPreset preset, SaveInfo importFrom)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.entered++; p.insideCall = true; p.entryFrame = Time.frameCount;
                    Require(p.entered == 1 && ReferenceEquals(__instance, p.game) && ReferenceEquals(Game.Instance, p.game) &&
                        ReferenceEquals(preset, p.preset) && importFrom == null && Game.NewGameUnit == null, "Native new-game entry substituted its exact game, authored preset, import or character product.");
                    p.Record("native-new-game-enter");
                });
            }
            internal static void GameIdAfter(Player __instance, string value)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.minted++; Guid id;
                    Require(p.InSynchronousCallback && p.minted == 1 && ReferenceEquals(Game.Instance, p.game) &&
                        ReferenceEquals(__instance, p.game.Player) && ReferenceEquals(__instance, p.menuPlayer) &&
                        Guid.TryParseExact(value, "D", out id) && id != Guid.Empty && id.ToString("D") == value, "Native campaign identity was not minted once inside the exact new-game call.");
                    p.NativePlayer = __instance; p.NativeGameId = value; p.Record("native-campaign-minted");
                });
            }
            internal static void SaveBefore(SaveManager __instance, SaveInfo saveInfo)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.saveRequests++;
                    Require(p.saveRequests == 1 && p.InSynchronousCallback && ReferenceEquals(Game.Instance, p.game) && ReferenceEquals(__instance, p.saveManager) &&
                        ReferenceEquals(p.game.SaveManager, p.saveManager) && p.NativePlayer != null && ReferenceEquals(p.NativePlayer, p.game.Player) &&
                        saveInfo != null && saveInfo.Type == SaveInfo.SaveType.Auto && !saveInfo.IsAutoLevelupSave, "First native write was not the authored new-game autosave.");
                    p.NativeSave = saveInfo; p.Record("native-bootstrap-autosave-request", saveInfo);
                });
            }
            internal static void NewGameAfter(Game __instance)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.exited++; Require(p.InSynchronousCallback && p.exited == 1 && ReferenceEquals(__instance, p.game) &&
                        ReferenceEquals(p.NativePlayer, p.game.Player) && p.game.Player.GameId == p.NativeGameId, "Native new-game return lost its admitted new world.");
                    p.Record("native-new-game-exit");
                });
                p.insideCall = false;
            }
            internal static void PreparedAfter(SaveManager __instance, SaveInfo save)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.prepared++; Require(p.prepared == 1 && ReferenceEquals(Game.Instance, p.game) && ReferenceEquals(__instance, p.saveManager) &&
                        ReferenceEquals(p.game.SaveManager, p.saveManager) && ReferenceEquals(p.NativePlayer, p.game.Player) &&
                        ReferenceEquals(save, p.NativeSave) && save.Type == SaveInfo.SaveType.Auto && save.FileName == Chunk7NativeBootstrapSelection.AutoLeaf &&
                        save.GameId == p.NativeGameId && !save.IsAutoLevelupSave, "Prepared native bootstrap autosave changed descriptor, world, leaf or category.");
                    p.Record("native-bootstrap-autosave-prepared", save);
                });
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (InSynchronousCallback) throw new InvalidOperationException("Native new-game callback still owns the observer.");
            harmony?.UnpatchAll(HarmonyId); identities.Dispose();
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
    }
}
