using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.UnitSettings;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Lab-only native UI lease. Uses actual registered groups and slots; creates no ability or slot.
    internal sealed class NativeMountActionBarUiLease : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly UnitEntityData rider;
        private readonly ActionBarManager manager;
        private readonly ActionBarGroupElement[] groups;
        private readonly bool[] original;
        private readonly ActionBarGroupElement abilityGroup;
        private readonly JObject evidence;
        private bool disposed;
        internal static bool IsReady(UnitEntityData rider)
        {
            NativeMountActionBarProbe.RequireSelection(rider);
            var manager = ActionBarManager.Instance;
            return manager != null && ReferenceEquals(typeof(ActionBarManager).GetField("m_Selected", Flags)?.GetValue(manager), rider);
        }
        internal NativeMountActionBarUiLease(UnitEntityData rider)
        {
            if (!IsReady(rider)) throw new InvalidOperationException("Exact selected rider action bar is not ready.");
            this.rider = rider; manager = ActionBarManager.Instance;
            groups = manager.Group?.GroupElements?.Where(x => x != null).ToArray();
            if (groups == null || groups.Length == 0) throw new InvalidOperationException("Native action bar groups unavailable.");
            var abilityGroups = groups.Where(g => g.SlotType == ActionBarSlotType.ActivatableAbility).ToArray();
            if (abilityGroups.Length != 1) throw new InvalidOperationException("One registered native ability group required.");
            abilityGroup = abilityGroups[0]; original = groups.Select(g => g.ToggleState).ToArray();
            evidence = new JObject { ["contract"] = "native-action-bar-group-input-and-exact-restoration",
                ["casterId"] = rider.UniqueId, ["managerObject"] = Id(manager), ["mainOwnerObject"] = Id(manager.Slots),
                ["abilityGroupObject"] = Id(abilityGroup), ["before"] = Snapshot(), ["restored"] = false };
            try
            {
                if (!abilityGroup.ToggleState) abilityGroup.OnClick();
                evidence["opened"] = Snapshot();
                if (!abilityGroup.ToggleState) throw new InvalidOperationException("Native ability group refused its ordinary OnClick.");
            }
            catch { Dispose(); throw; }
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private JArray Snapshot() => new JArray(groups.Select((g, i) => new JObject {
            ["index"] = i, ["groupObject"] = Id(g), ["type"] = g.SlotType.ToString(), ["toggle"] = g.ToggleState }));
        internal ActionBarSlot FindMountSlot() => FindControlSlot("f053faad986631688defa003cd7bda0e");
        internal ActionBarSlot FindControlSlot(string abilityGuid)
        {
            if(abilityGuid!="f053faad986631688defa003cd7bda0e"&&abilityGuid!="3af2b81f4d72bbb30501fa730fcdf36e")throw new InvalidOperationException("Exact relationship control slot required.");
            if (disposed || !IsReady(rider) || !ReferenceEquals(ActionBarManager.Instance, manager))
                throw new InvalidOperationException("Native action bar lease lost exact selected rider/manager.");
            var matches = new List<ActionBarSlot>();
            Action<object> inspect = item => {
                var slot = item as ActionBarSlot;
                var mechanic = slot?.MechanicSlot as MechanicActionBarSlotAbility;
                if (slot != null && slot.gameObject.activeInHierarchy && mechanic?.Ability?.Blueprint?.AssetGuid == abilityGuid &&
                    mechanic.Ability.Caster.Unit == rider && ReferenceEquals(typeof(ActionBarSlot).GetField("Selected", Flags)?.GetValue(slot), rider))
                    matches.Add(slot);
            };
            var main = manager.Slots;
            var slots = main == null ? null : typeof(ActionBarSlots).GetField("m_Slots", Flags)?.GetValue(main) as IEnumerable;
            if (slots != null) foreach (var slot in slots) inspect(slot);
            // Prefer one existing main-bar assignment; otherwise use the real opened ability group.
            if (matches.Count == 0)
            {
                slots = typeof(ActionBarGroupElement).GetField("m_Slots", Flags)?.GetValue(abilityGroup) as IEnumerable;
                if (slots != null) foreach (var slot in slots) inspect(slot);
            }
            evidence["matchingSlotObjects"] = new JArray(matches.Select(Id));
            evidence["current"] = Snapshot();
            if (matches.Count > 1) throw new InvalidOperationException("More than one eligible native Mount slot exists in the selected route.");
            return matches.Count == 1 ? matches[0] : null;
        }
        internal JObject Capture() => (JObject)evidence.DeepClone();
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!IsReady(rider) || !ReferenceEquals(ActionBarManager.Instance, manager))
                throw new InvalidOperationException("Cannot restore native group lease after selected actor/manager changed.");
            for (var i = 0; i < groups.Length; i++)
                if (!original[i] && groups[i].ToggleState) groups[i].OnClick();
            for (var i = 0; i < groups.Length; i++)
                if (original[i] && !groups[i].ToggleState) groups[i].OnClick();
            evidence["after"] = Snapshot();
            evidence["restored"] = groups.Select((g, i) => g.ToggleState == original[i]).All(x => x);
            if (!(bool)evidence["restored"]) throw new InvalidOperationException("Native group input failed exact toggle restoration.");
        }
    }
}