using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    // command put it. Nothing here keeps a unit or a command alive.
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

        private sealed class Placement
        {
            internal WeakReference Command;
            internal int Frame;
        }

        private static ConditionalWeakTable<UnitEntityData, Placement> placements = new ConditionalWeakTable<UnitEntityData, Placement>();

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

        // From the postfix of native UnitEntityView.OnMovementStarted when its placement ran (not skipped, not a
        // preview): the unit's pointer now stands for the command native placed it for.
        internal static void RecordPlacement(UnitEntityData unit, int frame)
        {
            if (unit == null) return;
            var marks = ClickPointerManager.Instance?.UnitMarks;
            if (marks == null || !marks.TryGetValue(unit, out var pointer) || pointer == null || !pointer.IsVisible) return;
            var record = placements.GetOrCreateValue(unit);
            record.Command = new WeakReference(QualifyingCommand(unit));
            record.Frame = frame;
        }

        internal static bool PlacedFor(UnitEntityData unit, UnitCommand command, out int frame)
        {
            frame = -1;
            if (unit == null || command == null || !placements.TryGetValue(unit, out var record)) return false;
            frame = record.Frame;
            return ReferenceEquals(record.Command?.Target, command);
        }

        // World replacement, disable and disposal: nothing recorded survives.
        internal static void ResetPlacements() => placements = new ConditionalWeakTable<UnitEntityData, Placement>();
    }

    // Known issues M04 closeout: the frame in which the native ClickUnitHandler.OnClick runs (opened by its prefix when
    // native proceeds, closed by its postfix). Only a command run on that same frame is inside the click; a click that
    // throws leaves nothing open past its frame.
    internal static class MountedNativeUnitClickScope
    {
        private static int openFrame = -1;

        internal static void Open() => openFrame = UnityEngine.Time.frameCount;

        internal static void Close() => openFrame = -1;

        internal static bool IsOpen => openFrame >= 0 && openFrame == UnityEngine.Time.frameCount;
    }
}
