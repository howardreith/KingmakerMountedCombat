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

        // The exact KMC identity of a slot's ability blueprint, by reference. A missing blueprint (an item, an empty or
        // another non-ability slot) is never a KMC control, including before or after registration when some control
        // blueprints are still (or again) null: only an identifiable registered blueprint matches.
        public static NativeMountedControlKind ResolveExactKind<T>(T ability, T mount, T dismount, T riderPrimary,
            T mountPrimary, T mountedCharge, T mountState) where T : class
        {
            if (ability == null) return NativeMountedControlKind.None;
            if (ReferenceEquals(ability, mount)) return NativeMountedControlKind.MountCompanion;
            if (ReferenceEquals(ability, dismount)) return NativeMountedControlKind.Dismount;
            if (ReferenceEquals(ability, riderPrimary)) return NativeMountedControlKind.RiderPrimary;
            if (ReferenceEquals(ability, mountPrimary)) return NativeMountedControlKind.MountPrimary;
            if (ReferenceEquals(ability, mountedCharge)) return NativeMountedControlKind.MountedCharge;
            if (ReferenceEquals(ability, mountState)) return NativeMountedControlKind.MountState;
            return NativeMountedControlKind.None;
        }

        // A binding that stands for the rider's one public Mount / Dismount control: the public control itself, or an
        // older exact Mount or Dismount alias that repair turns into it.
        public static bool IsPublicControlKind(NativeMountedControlKind kind) =>
            kind == NativeMountedControlKind.MountState || RepairLiveSlot(kind) == MountedControlSlotRepair.MountState;

        // An archive row that restores as the rider's public control: kind 6, or an older Mount / Dismount kind (the
        // live writer emits the alias kind while a live alias awaits migration, or the legacy schema's shell kinds).
        public static bool IsPublicBindingRow(int savedKind) =>
            savedKind != SavedPlacementRecord && RestoreSavedSlot(savedKind) == MountedControlSlotRepair.MountState;

        // The rows an absent arrangement adds for one actor after the live writer ran, given the kinds it already wrote
        // for that actor. None when the live writer already wrote the actor's public binding (the effective choice; the
        // archive keys rows by actor and index, so the same choice is never written twice); otherwise
        // AbsentArrangementRows, without a second placement record.
        public static IList<KeyValuePair<int, int>> AbsentRowsAfterLive(int index, IEnumerable<int> liveKindsForActor)
        {
            var live = liveKindsForActor == null ? new List<int>() : new List<int>(liveKindsForActor);
            var result = new List<KeyValuePair<int, int>>();
            if (live.Exists(IsPublicBindingRow)) return result;
            foreach (var row in AbsentArrangementRows(index))
                if (row.Value != SavedPlacementRecord || !live.Contains(SavedPlacementRecord)) result.Add(row);
            return result;
        }

        // The slot an older bar's public control is recovered from, with ReconcilePublicControlSlots' precedence: an
        // existing public Mount / Dismount binding wins; otherwise the first exact older Mount or Dismount binding.
        // -1 when the bar holds neither.
        public static int RecoveredPublicSlot(IList<NativeMountedControlKind> kinds)
        {
            if (kinds == null) return -1;
            for (var index = 0; index < kinds.Count; index++)
                if (kinds[index] == NativeMountedControlKind.MountState) return index;
            for (var index = 0; index < kinds.Count; index++)
                if (RepairLiveSlot(kinds[index]) == MountedControlSlotRepair.MountState) return index;
            return -1;
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

        // The owned archive's control-row bound (MountedArchiveData.ValidateSlots). A capture that needs more rows fails
        // that validation, the archive's bounded failure path, rather than dropping any row.
        public const int MaximumArchiveRows = 128;

        public static bool FitsArchive(int rowCount) => rowCount >= 0 && rowCount <= MaximumArchiveRows;

        // The archive rows of an absent rider's arrangement (index, kind): its chosen slot when it fits the saved bound,
        // and always its placement record, so a rider that left the control off the bar is never placed again.
        public static IEnumerable<KeyValuePair<int, int>> AbsentArrangementRows(int chosenIndex)
        {
            if (IsValidSavedRow(chosenIndex, SavedMountState))
                yield return new KeyValuePair<int, int>(chosenIndex, SavedMountState);
            yield return new KeyValuePair<int, int>(0, SavedPlacementRecord);
        }

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
