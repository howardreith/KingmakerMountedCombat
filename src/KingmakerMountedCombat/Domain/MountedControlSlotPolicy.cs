using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // F2 CTRL-01/02/03: what one exact KMC hotbar identity becomes, and where the public mount-state control is placed
    // the first time. Pure decisions; the control service reads and writes the native slots.
    public enum MountedControlSlotRepair
    {
        Keep,
        // An old public Mount or Dismount slot, or a stale binding of the public control.
        MountState,
        // An old Mounted Charge slot: the ordinary Charge takes its place.
        StockCharge,
        Clear
    }

    public static class MountedControlSlotPolicy
    {
        // KMC archive slot kinds. 1-5 keep their historical meaning; 6 and 7 are F2's.
        public const int SavedMountCompanion = 1;
        public const int SavedDismount = 2;
        public const int SavedRiderPrimary = 3;
        public const int SavedMountPrimary = 4;
        public const int SavedMountedCharge = 5;
        public const int SavedMountState = 6;
        // Not a slot: the actor has had its one placement, or removed the control afterwards. Index is always 0.
        public const int SavedPlacementRecord = 7;
        public const int MaximumSavedKind = SavedPlacementRecord;

        // ActionBarManager.ActionSlotsCountMainBar in the installed assembly.
        public const int MainBarSlotCount = 14;

        public static MountedControlSlotRepair RepairLiveSlot(NativeMountedControlKind kind)
        {
            switch (kind)
            {
                case NativeMountedControlKind.MountCompanion:
                case NativeMountedControlKind.Dismount:
                    return MountedControlSlotRepair.MountState;
                case NativeMountedControlKind.MountedCharge:
                    return MountedControlSlotRepair.StockCharge;
                default:
                    return MountedControlSlotRepair.Keep;
            }
        }

        public static MountedControlSlotRepair RestoreSavedSlot(int savedKind)
        {
            switch (savedKind)
            {
                case SavedMountCompanion:
                case SavedDismount:
                case SavedMountState:
                    return MountedControlSlotRepair.MountState;
                case SavedMountedCharge:
                    return MountedControlSlotRepair.StockCharge;
                default:
                    return MountedControlSlotRepair.Keep;
            }
        }

        // The control-placement record is a separate archive row, so it must not collide with a real slot at index 0.
        public static string SavedRowKey(string actorKey, int index, int kind) =>
            actorKey + (kind == SavedPlacementRecord ? ":placement" : ":" + index);

        public static bool IsValidSavedRow(int index, int kind) =>
            kind >= SavedMountCompanion && kind <= MaximumSavedKind &&
            (kind == SavedPlacementRecord ? index == 0 : index >= 0 && index <= 127);

        // Native SetSlotInternal fills the first empty slot of all 140. The public control goes only to the visible main
        // bar, and never over anything: -1 when that bar is full.
        public static int SelectInitialPlacementIndex(IList<bool> slotIsEmpty)
        {
            if (slotIsEmpty == null) return -1;
            var count = slotIsEmpty.Count < MainBarSlotCount ? slotIsEmpty.Count : MainBarSlotCount;
            for (var index = 0; index < count; index++)
            {
                if (slotIsEmpty[index]) return index;
            }
            return -1;
        }

        // One placement per actor. A control already on the bar, a placed control and a full bar all count as the
        // placement; the native autofill setting being off defers it, as the native new-ability route does.
        public static bool ShouldAttemptInitialPlacement(bool controlLeased, bool alreadyRecorded,
            bool restorationSettled, bool nativeAutofillEnabled) =>
            controlLeased && !alreadyRecorded && restorationSettled && nativeAutofillEnabled;
    }
}
