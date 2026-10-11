using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Lab-only: actual live slot origin. No slot creation, selection/resource write or ability assignment.
    internal sealed class NativeMountActionBarProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.MountActionBar";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeMountActionBarProbe active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider, target;
        private readonly ActionBarSlot slot;
        private readonly MechanicActionBarSlotAbility mechanic;
        private readonly AbilityData ability;
        private readonly ClickWithSelectedAbilityHandler handler;
        private readonly Func<JObject> state;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private readonly JObject baseline, registration;
        private int groupDepth, slotDepth, mechanicDepth;
        private bool disposed, invoked;

        internal NativeMountActionBarProbe(UnitEntityData rider, UnitEntityData target, ActionBarSlot slot, Func<JObject> capture)
        {
            if (active != null) throw new InvalidOperationException("An action-bar Mount observer is already active.");
            RequireSelection(rider);
            this.rider = rider; this.target = target; this.slot = slot;
            mechanic = slot?.MechanicSlot as MechanicActionBarSlotAbility;
            ability = mechanic?.Ability; handler = Game.Instance.SelectedAbilityHandler; state = capture;
            if (target?.View == null || slot == null || !slot.gameObject.activeInHierarchy || mechanic == null ||
                ability == null || ability.Caster.Unit != rider || ability.Blueprint.AssetGuid != "f053faad986631688defa003cd7bda0e" ||
                !ReferenceEquals(typeof(ActionBarSlot).GetField("Selected", Flags)?.GetValue(slot), rider) || handler == null || handler.Ability != null || capture == null)
                throw new InvalidOperationException("Action-bar Mount requires the exact rider's active live slot and an empty native targeting handler.");
            if (typeof(UnitCommand).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Action-bar observation requires the pinned native assembly.");
            // The caller has selected and verified the rider; no ability activation precedes this baseline.
            registration = RequireLiveRegistration(rider, slot);
            baseline = (JObject)capture().DeepClone();
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(ActionBarGroupSlot), 0x060044BA, "GroupBefore", "GroupAfter");
                Patch(typeof(ActionBarSlot), 0x06004504, "SlotBefore", "SlotAfter");
                Patch(typeof(MechanicActionBarSlotAbility), 0x06002F5D, "MechanicBefore", "MechanicAfter");
                Patch(typeof(ClickWithSelectedAbilityHandler), 0x060093F8, "AbilityBefore", "AbilityAfter");
                Patch(typeof(ClickWithSelectedAbilityHandler), 0x060093F6, "ClickBefore", "ClickAfter");
            }
            catch { Dispose(); throw; }
        }
        internal static void RequireSelection(UnitEntityData rider)
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            if (rider == null || selected == null || selected.Count != 1 || selected[0] != rider)
                throw new InvalidOperationException("Action-bar requires exact rider selection: rider=" + rider?.UniqueId +
                    "; count=" + (selected?.Count ?? -1) + "; selected=" +
                    (selected == null ? "unavailable" : string.Join(",", selected.Select(x => x?.UniqueId ?? "null"))));
        }
        internal bool Invoke()
        {
            if (disposed || invoked) throw new InvalidOperationException("Action-bar Mount invocation is exactly once.");
            RequireSelection(rider);
            if (!ReferenceEquals(slot.MechanicSlot, mechanic) || !ReferenceEquals(mechanic.Ability, ability) || handler.Ability != null)
                throw new InvalidOperationException("Live action-bar identity changed before activation.");
            RequireLiveRegistration(rider, slot);
            invoked = true;
            slot.OnClick();
            if (!ReferenceEquals(handler.Ability, ability) || errors.Count != 0)
                throw new InvalidOperationException("Native action-bar callbacks did not install the exact selected ability: " + Capture());
            RequireSelection(rider);
            // SetAbility is intentionally absent: only the live native mechanic callback selects the ability.
            return handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
        }
        internal static void OpenNativeAbilityGroup(UnitEntityData rider)
        {
            RequireSelection(rider);
            var manager = ActionBarManager.Instance;
            if (manager == null || !ReferenceEquals(typeof(ActionBarManager).GetField("m_Selected", Flags)?.GetValue(manager), rider))
                throw new InvalidOperationException("Native action bar has not settled on the exact rider.");
            var groups = manager.Group?.GroupElements?.Where(g => g != null && g.SlotType == ActionBarSlotType.ActivatableAbility).ToArray();
            if (groups == null || groups.Length != 1) throw new InvalidOperationException("One real native ability group is required.");
            if (!groups[0].ToggleState) groups[0].OnClick();
        }
        private static JObject RequireLiveRegistration(UnitEntityData rider, ActionBarSlot slot)
        {
            var manager = ActionBarManager.Instance;
            if (manager == null || !ReferenceEquals(typeof(ActionBarManager).GetField("m_Selected", Flags)?.GetValue(manager), rider))
                throw new InvalidOperationException("Native action bar is owned by another selected unit.");
            object owner = null; string route = null; int occurrences = 0;
            var main = manager.Slots;
            var mainSlots = main == null ? null : typeof(ActionBarSlots).GetField("m_Slots", Flags)?.GetValue(main) as System.Collections.IEnumerable;
            if (mainSlots != null) foreach(var item in mainSlots) if(ReferenceEquals(item,slot)) {owner=main;route="main";occurrences++;}
            if(manager.Group?.GroupElements != null) foreach(var group in manager.Group.GroupElements)
            {
                if(group == null)continue;
                var slots=typeof(ActionBarGroupElement).GetField("m_Slots",Flags)?.GetValue(group) as System.Collections.IEnumerable;
                if(slots == null)continue;
                foreach(var item in slots) if(ReferenceEquals(item,slot))
                {
                    if(!group.ToggleState || !ReferenceEquals(typeof(ActionBarGroupElement).GetField("m_Selected",Flags)?.GetValue(group),rider))
                        throw new InvalidOperationException("Native group slot is hidden or belongs to another actor.");
                    owner=group;route="group";occurrences++;
                }
            }
            if(occurrences!=1 || !slot.gameObject.activeInHierarchy)throw new InvalidOperationException("Mount slot is not uniquely registered and active in the live native action bar.");
            return new JObject { ["managerObject"]=Id(manager),["ownerObject"]=Id(owner),["slotObject"]=Id(slot),
                ["route"]=route,["occurrences"]=occurrences,["casterId"]=rider.UniqueId,["active"]=true };
        }
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).Single(m => m.MetadataToken == token);
            harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["method"] = method.DeclaringType.FullName + "." + method.Name,
                ["token"] = token.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString(), ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private static int Id(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        private void Record(string boundary, object instance, AbilityData argument = null, bool? result = null)
        {
            if (events.Count >= 32) { if (errors.Count == 0) errors.Add("Action-bar callback bound exceeded."); return; }
            RequireSelection(rider);
            var command = rider.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
            events.Add(new JObject { ["sequence"] = events.Count + 1, ["boundary"] = boundary,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["instanceObject"] = Id(instance), ["groupDepth"] = groupDepth, ["slotDepth"] = slotDepth, ["mechanicDepth"] = mechanicDepth,
                ["slotObject"] = Id(slot), ["mechanicObject"] = Id(mechanic), ["abilityObject"] = Id(ability), ["handlerObject"] = Id(handler),
                ["argumentAbilityObject"] = Id(argument), ["selectedAbilityObject"] = Id(handler.Ability), ["casterId"] = rider.UniqueId,
                ["targetId"] = target.UniqueId, ["abilityGuid"] = ability.Blueprint.AssetGuid,
                ["liveSlotUnchanged"] = ReferenceEquals(slot.MechanicSlot, mechanic) && ReferenceEquals(mechanic.Ability, ability),
                ["commandObject"] = Id(command), ["commandAbilityObject"] = Id(command?.Spell),
                ["commandTargetId"] = command?.Target?.Unit?.UniqueId, ["clicked"] = result.HasValue ? new JValue(result.Value) : JValue.CreateNull(),
                ["state"] = state() });
        }
        private void Safe(Action observe) { try { observe(); } catch(Exception e) { if (errors.Count == 0) errors.Add(e.ToString()); } }
        internal JObject Capture() => new JObject { ["contract"] = "live-action-bar-origin-for-one-exact-mount",
            ["baseline"] = baseline.DeepClone(), ["registration"] = registration.DeepClone(), ["slotObject"] = Id(slot), ["groupSlot"] = slot is ActionBarGroupSlot,
            ["mechanicObject"] = Id(mechanic), ["abilityObject"] = Id(ability), ["handlerObject"] = Id(handler),
            ["events"] = events.DeepClone(), ["observerHooks"] = hooks.DeepClone(), ["errors"] = errors.DeepClone(),
            ["complete"] = !disposed && invoked && groupDepth == 0 && slotDepth == 0 && mechanicDepth == 0 && errors.Count == 0 };
        public void Dispose() { if(disposed)return; disposed=true; harmony.UnpatchAll(HarmonyId); if(ReferenceEquals(active,this))active=null; }
        private static class Hooks
        {
            internal static void GroupBefore(ActionBarGroupSlot __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.slot))return;p.Safe(()=>{p.groupDepth++;p.Record("group-before",__instance);}); }
            internal static void GroupAfter(ActionBarGroupSlot __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.slot))return;p.Safe(()=>{p.Record("group-after",__instance);p.groupDepth--;}); }
            internal static void SlotBefore(ActionBarSlot __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.slot))return;p.Safe(()=>{p.slotDepth++;p.Record("slot-before",__instance);}); }
            internal static void SlotAfter(ActionBarSlot __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.slot))return;p.Safe(()=>{p.Record("slot-after",__instance);p.slotDepth--;}); }
            internal static void MechanicBefore(MechanicActionBarSlotAbility __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.mechanic))return;p.Safe(()=>{p.mechanicDepth++;p.Record("mechanic-before",__instance);}); }
            internal static void MechanicAfter(MechanicActionBarSlotAbility __instance) { var p=active;if(p==null||!ReferenceEquals(__instance,p.mechanic))return;p.Safe(()=>{p.Record("mechanic-after",__instance);p.mechanicDepth--;}); }
            internal static void AbilityBefore(ClickWithSelectedAbilityHandler __instance,AbilityData ability) { var p=active;if(p==null)return;p.Safe(()=>{if(!ReferenceEquals(__instance,p.handler)||!ReferenceEquals(ability,p.ability))p.errors.Add("Unexpected selected-ability identity.");p.Record("ability-before",__instance,ability);}); }
            internal static void AbilityAfter(ClickWithSelectedAbilityHandler __instance,AbilityData ability) { var p=active;if(p==null)return;p.Safe(()=>p.Record("ability-after",__instance,ability)); }
            internal static void ClickBefore(ClickWithSelectedAbilityHandler __instance,GameObject gameObject,int button,bool simulate,bool muteEvents) { var p=active;if(p==null)return;p.Safe(()=>{if(!ReferenceEquals(__instance,p.handler)||!ReferenceEquals(gameObject,p.target.View.gameObject)||button!=0||simulate||muteEvents)p.errors.Add("Unexpected native target click arguments.");p.Record("click-before",__instance);}); }
            internal static void ClickAfter(ClickWithSelectedAbilityHandler __instance,bool __result) { var p=active;if(p==null)return;p.Safe(()=>p.Record("click-after",__instance,null,__result)); }
        }
    }
}
