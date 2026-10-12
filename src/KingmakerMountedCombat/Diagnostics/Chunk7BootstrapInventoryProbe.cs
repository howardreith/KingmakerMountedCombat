using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class Chunk7BootstrapInventoryProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.BootstrapColdInventory";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static Chunk7BootstrapInventoryProbe active;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(128);
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray(), hooks = new JArray();
        private HarmonyInstance harmony;
        private bool installed, installationStarted, disposed, inside;
        private int entryFrame, entries, exits;
        internal Player ObservedPlayer { get; private set; }
        internal bool InSynchronousCallback => inside && entryFrame == Time.frameCount;
        internal bool Complete => installed && entries == 1 && exits == 1 && errors.Count == 0 && identities.FaultCount == 0;
        internal bool IdentityReleased => identities.Released;

        internal Chunk7BootstrapInventoryProbe(Game game)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            manager = game.SaveManager ?? throw new ArgumentException("Native SaveManager is unavailable.");
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Chunk7BootstrapInventoryProbe));
            if (installed) return;
            if (installationStarted || active != null) throw new InvalidOperationException("Cold inventory observer already owns or attempted installation.");
            installationStarted = true;
            // Native contract validation; tests substitute only metadata/Harmony boundaries.
            if (!ReferenceEquals(Game.Instance, game) || game.CurrentlyLoadedArea != null ||
                typeof(Player).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Cold inventory observation requires the exact pre-load native Game and module.");
            var method = typeof(Player).GetMethod("PostLoad", Flags, null, Type.EmptyTypes, null);
            if (method == null || method.MetadataToken != 0x06000DC7)
                throw new MissingMethodException("Exact native Player.PostLoad contract changed.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            harmony.Patch(method, new HarmonyMethod(typeof(Chunk7BootstrapInventoryProbe).GetMethod(nameof(Before), Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Chunk7BootstrapInventoryProbe).GetMethod(nameof(After), Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["declaringType"] = method.DeclaringType.FullName, ["method"] = method.Name,
                ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
            installed = true;
        }
        internal int Identity(object value) => identities.Get(value);
        private void Observe(Player player, bool before)
        {
            try
            {
                if (before) { entries++; inside = true; entryFrame = Time.frameCount; }
                else exits++;
                if (entries != 1 || exits > 1 || !ReferenceEquals(Game.Instance, game) || !ReferenceEquals(game.SaveManager, manager) ||
                    !ReferenceEquals(game.Player, player) || !before && (!InSynchronousCallback || !ReferenceEquals(ObservedPlayer, player)))
                    throw new InvalidOperationException("Native PostLoad replaced the admitted Game, manager or callback Player.");
                if (before) ObservedPlayer = player;
                if (events.Count == 2) throw new InvalidOperationException("Cold inventory exceeded its one-load native observation.");
                events.Add(new JObject { ["kind"] = before ? "native-player-postload-before" : "native-player-postload-after",
                    ["frame"] = Time.frameCount, ["game"] = Identity(game), ["saveManager"] = Identity(manager), ["player"] = Identity(player),
                    ["gameId"] = player.GameId, ["mainActorId"] = player.MainCharacter.Value?.UniqueId });
            }
            catch (Exception error) { var text = error.ToString(); if (!errors.Contains(text)) errors.Add(text); }
            finally { if (!before) inside = false; }
        }
        private static void Before(Player __instance) => active?.Observe(__instance, true);
        private static void After(Player __instance) => active?.Observe(__instance, false);
        internal JObject Capture() => new JObject { ["contract"] = "bootstrap-native-postload-v1", ["installed"] = installed,
            ["installationAttempted"] = installationStarted, ["entries"] = entries, ["exits"] = exits, ["complete"] = Complete,
            ["events"] = events.DeepClone(), ["hooks"] = hooks.DeepClone(), ["errors"] = new JArray(errors),
            ["identities"] = new JObject { ["contract"] = "retained-native-object-identity-v1", ["capacity"] = identities.Capacity,
                ["ids"] = new JArray(identities.Ids), ["retainedCount"] = identities.RetainedCount, ["faults"] = identities.FaultCount, ["released"] = identities.Released } };
        public void Dispose()
        {
            if (disposed) return;
            if (InSynchronousCallback) throw new InvalidOperationException("Native PostLoad still owns the observer.");
            harmony?.UnpatchAll(HarmonyId); identities.Dispose();
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
    }
}
