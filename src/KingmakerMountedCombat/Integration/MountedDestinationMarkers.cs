using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues sprint 3 (M04), see MountedDestinationMarkerPolicy for the native contract. The only write to the
    // native pointer table in the product: one unit's own pointer object is taken out of it and destroyed. No other
    // unit's pointer, no command and no visibility rule is touched; native creates a new pointer the next time that
    // unit's own agent places one.
    // Known issues M04 closeout: the placement of each unit's pointer is recorded (weakly: the command it was placed for
    // and the frame), so an observation can tell a pointer shown for its own command from one shown where an earlier
    // command put it. Nothing here keeps a unit or a command alive. Review of b1a88b4e (R3): recorded only when native
    // ClickPointerManager.AddPointer (06002D62) actually ran for the unit (its postfix), never inferred from a visible
    // pointer after OnMovementStarted, which can decline AddPointer while an older pointer stays visible.
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

        private static readonly MountedPointerPlacementLedger<UnitEntityData, UnitCommand> placements =
            new MountedPointerPlacementLedger<UnitEntityData, UnitCommand>();

        // The command the native ClickPointerManager.Update shows a unit's pointer for: a live UnitMoveTo with
        // ShowTargetMarker (the Move slot, or the first one queued), else a live UnitAreaTransition; null when none.
        internal static UnitCommand QualifyingCommand(UnitEntityData unit)
        {
            var commands = unit?.Commands;
            if (commands == null) return null;
            var move = commands.Move ?? commands.Queue.OfType<UnitMoveTo>().FirstOrDefault();
            if (move != null && !move.IsFinished && move.ShowTargetMarker) return move;
            var transition = commands.GetCommand<UnitAreaTransition>();
            return transition != null && !transition.IsFinished ? transition : null;
        }

        // From the postfix of native ClickPointerManager.AddPointer (06002D62), the one method that places an ordinary
        // gameplay pointer (previews use AddPreviewPointer): the unit's pointer now stands for the command native placed
        // it for, wherever it was placed (the same coordinates included).
        internal static void RecordPlacement(UnitEntityData unit, int frame)
        {
            if (unit == null) return;
            placements.Record(unit, QualifyingCommand(unit), frame);
        }

        internal static bool PlacedFor(UnitEntityData unit, UnitCommand command, out int frame) => placements.PlacedFor(unit, command, out frame);

        // World replacement, disable and disposal: nothing recorded survives.
        internal static void ResetPlacements() => placements.Reset();
    }

}
