using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues sprint 3 (M04), see MountedDestinationMarkerPolicy for the native contract. The only write to the
    // native pointer table in the product: one unit's own pointer object is taken out of it and destroyed. No other
    // unit's pointer, no command and no visibility rule is touched; native creates a new pointer the next time that
    // unit's own agent places one.
    internal static class MountedDestinationMarkers
    {
        internal static bool RetireRiderPointer(UnitEntityData rider, bool carrierAdmitted)
        {
            var marks = ClickPointerManager.Instance?.UnitMarks;
            ClickPointerPrefab pointer = null;
            var exists = rider != null && marks != null && marks.TryGetValue(rider, out pointer);
            if (!MountedDestinationMarkerPolicy.RetireRiderPointer(carrierAdmitted, exists)) return false;
            // The entry goes first: the native per-frame pass dereferences every pointer the table still holds.
            marks.Remove(rider);
            if (pointer != null)
            {
                pointer.SetVisible(false);
                UnityEngine.Object.Destroy(pointer.gameObject);
            }
            return true;
        }
    }
}
