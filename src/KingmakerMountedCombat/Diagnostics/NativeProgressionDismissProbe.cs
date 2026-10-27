using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class NativeProgressionDismissProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7ProgressionDismiss";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static NativeProgressionDismissProbe active;
        private readonly UnitEntityData actor;
        private readonly object player;
        private readonly Action assertScope;
        private readonly Func<bool> expectedConfirmation;
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray();
        private JToken inventoryBefore;
        private HarmonyInstance harmony;
        private bool disposed, installed, installationStarted, inside;
        private int frame = -1, entries, exits;
        internal NativeProgressionDismissProbe(UnitEntityData actor, Action assertScope, Func<bool> expectedConfirmation)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.assertScope = assertScope ?? throw new ArgumentNullException(nameof(assertScope));
            this.expectedConfirmation = expectedConfirmation ?? throw new ArgumentNullException(nameof(expectedConfirmation));
            player = Game.Instance?.Player;
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeProgressionDismissProbe));
            if (installed) return;
            if (installationStarted || active != null) throw new InvalidOperationException("A dismissal observer already owns or attempted installation.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            var method = typeof(Player).GetMethod("DismissCompanion", Flags, null, new[] { typeof(UnitEntityData) }, null);
            if (method == null || method.MetadataToken != 0x06000DD7 ||
                method.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Installed native dismissal contract changed.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod("Before", Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod("After", Flags)) { prioritiy = Priority.Last });
            installed = true;
        }
        internal bool InSynchronousCallback => inside && frame == Time.frameCount;
        internal bool Complete => installed && entries == 1 && exits == 1 && errors.Count == 0;
        internal JObject Capture() => new JObject { ["installed"] = installed, ["installationAttempted"] = installationStarted,
            ["nativeEntries"] = entries, ["nativeExits"] = exits, ["complete"] = Complete,
            ["errors"] = new JArray(errors), ["events"] = events.DeepClone(),
            ["inventoryBefore"] = inventoryBefore?.DeepClone(), ["inventoryAtCapture"] = InventorySnapshot() };
        private JToken InventorySnapshot()
        {
            try
            {
                if (!ReferenceEquals(Game.Instance?.Player, player)) return new JObject { ["error"] = "Dismissal player changed." };
                var items = Game.Instance.Player.Inventory.Items;
                var rows = new JArray();
                // The list is read only at the native entry or after the failed
                // call has returned. Bound the record without changing inventory.
                for (var i = 0; i < items.Count && i < 128; i++)
                {
                    var item = items[i];
                    rows.Add(new JObject { ["index"] = i, ["object"] = item == null ? (JToken)JValue.CreateNull() : RuntimeHelpers.GetHashCode(item),
                        ["blueprint"] = item?.Blueprint?.AssetGuid, ["count"] = item?.Count,
                        ["stackable"] = item?.IsStackable, ["targetOwner"] = ReferenceEquals(item?.Owner, actor.Descriptor),
                        ["slotObject"] = item?.HoldingSlot == null ? (JToken)JValue.CreateNull() : RuntimeHelpers.GetHashCode(item.HoldingSlot) });
                }
                return new JObject { ["count"] = items.Count, ["truncated"] = items.Count > 128, ["rows"] = rows };
            }
            catch (Exception exception) { return new JObject { ["error"] = exception.ToString() }; }
        }
        private void Safe(Action operation)
        {
            try { assertScope(); operation(); }
            catch (Exception exception) { var error = exception.ToString(); if (!errors.Contains(error)) errors.Add(error); }
        }
        private void Record(string kind)
        {
            if (events.Count >= 8) throw new InvalidOperationException("Native dismissal observer exceeded its event bound.");
            events.Add(new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                ["actorId"] = actor.UniqueId, ["actorObject"] = RuntimeHelpers.GetHashCode(actor),
                ["playerObject"] = RuntimeHelpers.GetHashCode(Game.Instance.Player),
                ["isInState"] = actor.IsInState, ["isInGame"] = actor.IsInGame, ["destroyed"] = actor.Destroyed,
                ["petId"] = actor.Descriptor.Pet?.UniqueId, ["insideExpectedNativeConfirmation"] = expectedConfirmation() });
        }
        private static class Hooks
        {
            internal static void Before(Player __instance, UnitEntityData value)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    if (p.entries != 0 || !p.expectedConfirmation() || !ReferenceEquals(__instance, p.player) ||
                        !ReferenceEquals(Game.Instance.Player, p.player) || !ReferenceEquals(value, p.actor) ||
                        !value.Descriptor.State.CanRemoveFromParty || ReferenceEquals(__instance.MainCharacter.Value, value))
                        throw new InvalidOperationException("Native dismissal is outside its exact expected custom-companion confirmation.");
                    p.entries++; p.inside = true; p.frame = Time.frameCount;
                    p.inventoryBefore = p.InventorySnapshot(); p.Record("native-dismiss-enter");
                });
            }
            internal static void After(Player __instance, UnitEntityData value)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    if (!p.InSynchronousCallback || p.exits != 0 || !p.expectedConfirmation() ||
                        !ReferenceEquals(__instance, p.player) || !ReferenceEquals(value, p.actor))
                        throw new InvalidOperationException("Native dismissal exit lost its original actor or confirmation window.");
                    p.exits++; p.Record("native-dismiss-exit"); p.inside = false;
                });
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (InSynchronousCallback) throw new InvalidOperationException("Native dismissal callback retains its observer.");
            harmony?.UnpatchAll(HarmonyId); disposed = true;
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
