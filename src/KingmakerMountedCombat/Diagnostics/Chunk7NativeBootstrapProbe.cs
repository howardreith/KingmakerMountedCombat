using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        private bool disposed, installed, installationStarted, insideCall, identityCallsiteInstalled;
        private int entryFrame = -1, entered, exited, minted, saveRequests, prepared;
        internal Player NativePlayer { get; private set; }
        internal string NativeGameId { get; private set; }
        internal SaveInfo NativeRequestedSave { get; private set; }
        internal SaveInfo NativeSave { get; private set; }
        private string requestedName;
        internal bool InSynchronousCallback => insideCall && entryFrame == Time.frameCount;
        internal bool Complete => installed && identityCallsiteInstalled && errors.Count == 0 && identities.FaultCount == 0 && entered == 1 && exited == 1 && minted == 1 && saveRequests == 1 && prepared == 1;
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
            GetIdentityContract();
            Patch(typeof(Game), "LoadNewGame", 0x06000CDC, new[] { typeof(BlueprintAreaPreset), typeof(SaveInfo) }, "NewGameBefore", "NewGameAfter", "IdentityCallsite");
            Require(identityCallsiteInstalled, "Native bootstrap identity callsite was not installed.");
            Patch(typeof(SaveManager), "SaveRoutine", 0x06008029, new[] { typeof(SaveInfo), typeof(bool) }, "SaveBefore", null);
            Patch(typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "PreparedAfter");
            installed = true;
        }
        private void Patch(Type type, string name, int token, Type[] parameters, string prefix, string postfix, string transpiler = null)
        {
            var method = type.GetMethod(name, Flags, null, parameters, null);
            if (method == null || method.MetadataToken != token) throw new MissingMethodException(type.FullName, name);
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last },
                transpiler == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(transpiler, Flags)));
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = name, ["token"] = token.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D"), ["transpiler"] = transpiler });
        }
        // The installed four-instruction GameId setter did not deliver its
        // postfix in Native02. Observe its exact callsite instead; never depend
        // on whether the runtime inlines that setter or on a save-time inference.
        private sealed class IdentityContract
        {
            internal MethodInfo Owner, Setter, NewGuid, ToStringMethod;
            internal FieldInfo State, Player;
        }
        private static IdentityContract GetIdentityContract()
        {
            var module = typeof(Game).Module;
            Require(module.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"), "Native bootstrap gameplay module changed.");
            var owner = typeof(Game).GetMethod("LoadNewGame", Flags, null, new[] { typeof(BlueprintAreaPreset), typeof(SaveInfo) }, null);
            var setter = typeof(Player).GetMethod("set_GameId", Flags, null, new[] { typeof(string) }, null);
            var state = typeof(Game).GetField("State", Flags);
            var player = state?.FieldType.GetField("PlayerState", Flags);
            Require(owner != null && owner.MetadataToken == 0x06000CDC && setter != null && setter.MetadataToken == 0x06000DE3 &&
                state != null && state.MetadataToken == 0x040006B6 && player != null && player.MetadataToken == 0x0400533C &&
                player.FieldType == typeof(Player) && setter.ReturnType == typeof(void) &&
                owner.GetMethodBody()?.LocalVariables.Count > 5 && owner.GetMethodBody().LocalVariables[5].LocalType == typeof(Guid),
                "Native bootstrap identity member or local contract changed.");
            return new IdentityContract { Owner = owner, Setter = setter, State = state, Player = player,
                NewGuid = typeof(Guid).GetMethod("NewGuid", Type.EmptyTypes), ToStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes) };
        }
        private static List<CodeInstruction> RewriteIdentityCallsite(IEnumerable<CodeInstruction> instructions, MethodBase original, IdentityContract contract)
        {
            Require(instructions != null && contract != null && Equals(original, contract.Owner), "Unpinned native bootstrap identity owner.");
            var code = instructions.ToList();
            Require(!code.Any(instruction => Equals(instruction.operand, typeof(Hooks).GetMethod("IdentityAssigned", Flags))),
                "Native bootstrap identity observer is already present in this instruction stream.");
            var sites = code.Select((instruction, index) => new { instruction, index }).Where(value => Equals(value.instruction.operand, contract.Setter)).ToArray();
            Require(sites.Length == 1 && sites[0].instruction.opcode == OpCodes.Callvirt, "Native bootstrap must contain exactly one original identity setter call.");
            var at = sites[0].index;
            Require(at >= 8 && code.Count(instruction => Equals(instruction.operand, contract.NewGuid)) == 1 &&
                code[at - 8].opcode == OpCodes.Ldarg_0 &&
                code[at - 7].opcode == OpCodes.Ldfld && Equals(code[at - 7].operand, contract.State) &&
                code[at - 6].opcode == OpCodes.Ldfld && Equals(code[at - 6].operand, contract.Player) &&
                code[at - 5].opcode == OpCodes.Call && Equals(code[at - 5].operand, contract.NewGuid) &&
                (code[at - 4].opcode == OpCodes.Stloc_S || code[at - 4].opcode == OpCodes.Stloc) &&
                (code[at - 3].opcode == OpCodes.Ldloca_S || code[at - 3].opcode == OpCodes.Ldloca) &&
                IsGuidLocal(code[at - 4].operand) && IsGuidLocal(code[at - 3].operand) &&
                code[at - 2].opcode == OpCodes.Constrained && Equals(code[at - 2].operand, typeof(Guid)) &&
                code[at - 1].opcode == OpCodes.Callvirt && Equals(code[at - 1].operand, contract.ToStringMethod) &&
                code.Skip(at - 7).Take(8).All(instruction => instruction.labels.Count == 0 && instruction.blocks.Count == 0),
                "Native bootstrap Guid source, receiver, local or uninterrupted assignment sequence changed.");
            // Keep every original instruction/operand/label/exception block. The
            // two new instructions run only after the native setter returns.
            code.InsertRange(at + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod("IdentityAssigned", Flags)) });
            return code;
        }
        private static bool IsGuidLocal(object value)
        {
            var local = value as LocalVariableInfo;
            return local != null && local.LocalIndex == 5 && local.LocalType == typeof(Guid);
        }
        internal int Identity(object value) => identities.Get(value);
        internal JObject Capture() => new JObject { ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(), ["errors"] = new JArray(errors),
            ["installed"] = installed, ["installationAttempted"] = installationStarted, ["entered"] = entered, ["exited"] = exited,
            ["minted"] = minted, ["saveRequests"] = saveRequests, ["prepared"] = prepared, ["complete"] = Complete,
            ["identityCallsite"] = new JObject { ["contract"] = "load-new-game-guid-setter-callsite-v1", ["installed"] = identityCallsiteInstalled,
                ["ownerToken"] = "06000CDC", ["setterToken"] = "06000DE3", ["stateFieldToken"] = "040006B6", ["playerFieldToken"] = "0400533C",
                ["setterIlOffset"] = 0x012E, ["moduleMvid"] = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7",
                ["nativeSetterPreserved"] = true, ["setterPostfixUsed"] = false, ["matchedSites"] = identityCallsiteInstalled ? 1 : 0 },
            ["identities"] = new JObject { ["contract"] = "retained-native-object-identity-v1", ["capacity"] = identities.Capacity, ["ids"] = new JArray(identities.Ids),
                ["retainedCount"] = identities.RetainedCount, ["faults"] = identities.FaultCount, ["released"] = identities.Released } };
        private void Safe(Action action)
        {
            try { action(); } catch (Exception error) { var value = error.ToString(); if (!errors.Contains(value)) errors.Add(value); }
        }
        private JObject Record(string kind, SaveInfo save = null)
        {
            if (events.Count >= 16) throw new InvalidOperationException("Native bootstrap observer exceeded its bounded event inventory.");
            var item = new JObject { ["sequence"] = events.Count + 1, ["kind"] = kind, ["frame"] = Time.frameCount,
                ["game"] = Identity(Game.Instance), ["saveManager"] = Identity(saveManager), ["player"] = Identity(game.Player), ["menuPlayer"] = Identity(menuPlayer),
                ["sameMenuPlayer"] = ReferenceEquals(game.Player, menuPlayer), ["gameId"] = game.Player?.GameId, ["mainActorId"] = game.Player?.MainCharacter.Value?.UniqueId,
                ["preset"] = Identity(preset), ["presetGuid"] = preset.AssetGuid, ["insideNativeNewGame"] = InSynchronousCallback,
                ["save"] = Identity(save), ["saveType"] = save?.Type.ToString(), ["saveLeaf"] = save?.FileName,
                ["saveName"] = save?.Name, ["savePath"] = save?.FolderName, ["saveArea"] = save?.Area?.AssetGuidThreadSafe,
                ["saveGameId"] = save?.GameId, ["saveGameName"] = save?.GameName, ["autoLevelup"] = save?.IsAutoLevelupSave };
            events.Add(item); return item;
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
                        ReferenceEquals(preset, p.preset) && importFrom == null && Game.NewGameUnit == null &&
                        ReferenceEquals(p.game.Player, p.menuPlayer) && ReferenceEquals(p.game.SaveManager, p.saveManager) &&
                        p.menuPlayer.MainCharacter.Value == null && string.IsNullOrEmpty(p.menuPlayer.GameId), "Native new-game entry substituted its exact game, authored preset, import or character product.");
                    p.Record("native-new-game-enter");
                });
            }
            internal static IEnumerable<CodeInstruction> IdentityCallsite(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            {
                var p = active;
                Require(p != null && p.installationStarted && !p.disposed, "Native bootstrap identity rewrite has no owned installation.");
                var code = RewriteIdentityCallsite(instructions, __originalMethod, GetIdentityContract());
                p.identityCallsiteInstalled = true;
                return code;
            }
            internal static void IdentityAssigned(Game __instance)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.minted++; Guid id;
                    var player = __instance?.Player; var value = player?.GameId;
                    Require(p.identityCallsiteInstalled && p.InSynchronousCallback && p.entered == 1 && p.exited == 0 &&
                        p.minted == 1 && p.saveRequests == 0 && p.prepared == 0 && ReferenceEquals(Game.Instance, p.game) &&
                        ReferenceEquals(__instance, p.game) && ReferenceEquals(player, p.menuPlayer) &&
                        ReferenceEquals(p.game.SaveManager, p.saveManager) && player.MainCharacter.Value != null &&
                        Guid.TryParseExact(value, "D", out id) && id != Guid.Empty && id.ToString("D") == value,
                        "Native campaign identity was not assigned once at the exact new-game Guid callsite.");
                    p.NativePlayer = player; p.NativeGameId = value; p.Record("native-campaign-minted");
                });
            }
            internal static void SaveBefore(SaveManager __instance, SaveInfo saveInfo)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.saveRequests++;
                    Require(p.saveRequests == 1 && p.InSynchronousCallback && ReferenceEquals(Game.Instance, p.game) && ReferenceEquals(__instance, p.saveManager) &&
                        ReferenceEquals(p.game.SaveManager, p.saveManager) && p.NativePlayer != null && ReferenceEquals(p.NativePlayer, p.game.Player) &&
                        p.minted == 1 && p.exited == 0 && p.game.Player.GameId == p.NativeGameId &&
                        saveInfo != null && saveInfo.Type == SaveInfo.SaveType.Auto && !saveInfo.IsAutoLevelupSave &&
                        !string.IsNullOrWhiteSpace(saveInfo.Name), "First native write was not the authored new-game autosave.");
                    p.NativeRequestedSave = saveInfo; p.requestedName = saveInfo.Name; p.Record("native-bootstrap-autosave-request", saveInfo);
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
                    p.prepared++;
                    // SaveRoutine retains the requested object, then gives
                    // PrepareSave a distinct native Type/Name copy. Preserve the
                    // observed arguments even when an invariant refuses it.
                    var observation = p.Record("native-bootstrap-autosave-prepared", save);
                    observation["requestedSave"] = p.Identity(p.NativeRequestedSave);
                    observation["observedSaveManager"] = p.Identity(__instance);
                    observation["accepted"] = false;
                    var order = p.prepared == 1 && p.saveRequests == 1 && p.minted == 1 && p.entered == 1 && p.exited == 1 && !p.insideCall;
                    var sameGame = ReferenceEquals(Game.Instance, p.game);
                    var sameManager = ReferenceEquals(__instance, p.saveManager) && ReferenceEquals(p.game.SaveManager, p.saveManager);
                    var samePlayer = p.NativePlayer != null && ReferenceEquals(p.NativePlayer, p.game.Player);
                    var requestRetained = p.NativeRequestedSave != null && p.NativeRequestedSave.Type == SaveInfo.SaveType.Auto &&
                        !p.NativeRequestedSave.IsAutoLevelupSave && p.NativeRequestedSave.Name == p.requestedName;
                    var distinct = save != null && !ReferenceEquals(save, p.NativeRequestedSave);
                    var sameName = save != null && save.Name == p.requestedName;
                    var category = save != null && save.Type == SaveInfo.SaveType.Auto && !save.IsAutoLevelupSave;
                    var leaf = save != null && save.FileName == Chunk7NativeBootstrapSelection.AutoLeaf;
                    var campaign = save != null && save.GameId == p.NativeGameId && p.game.Player?.GameId == p.NativeGameId;
                    observation["checks"] = new JObject { ["ordered"] = order, ["sameGame"] = sameGame, ["sameManager"] = sameManager,
                        ["samePlayer"] = samePlayer, ["requestRetained"] = requestRetained, ["distinctDescriptor"] = distinct,
                        ["sameName"] = sameName, ["autoCategory"] = category, ["expectedLeaf"] = leaf, ["sameCampaign"] = campaign };
                    Require(order && sameGame && sameManager && samePlayer && requestRetained && distinct && sameName && category && leaf && campaign,
                        "Prepared native bootstrap autosave changed its retained request, descriptor copy, world, leaf or category.");
                    p.NativeSave = save; observation["accepted"] = true;
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
