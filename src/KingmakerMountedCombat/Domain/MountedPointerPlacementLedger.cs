using System;
using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // Known issues M04 closeout, review of b1a88b4e (R3). Which command each unit's destination pointer was last placed
    // for. The Integration seam records only when native ClickPointerManager.AddPointer actually ran for the unit (its
    // postfix), so a placement native declined (OnMovementStarted without AddPointer) leaves the earlier record standing,
    // and a pointer still showing where an earlier command put it is seen as such. Weak on both sides: the unit is a
    // ConditionalWeakTable key and the command a WeakReference, so nothing here keeps either alive.
    public sealed class MountedPointerPlacementLedger<TUnit, TCommand>
        where TUnit : class
        where TCommand : class
    {
        private sealed class Placement
        {
            internal WeakReference Command;
            internal int Frame;
        }

        private ConditionalWeakTable<TUnit, Placement> placements = new ConditionalWeakTable<TUnit, Placement>();

        // AddPointer ran for the unit: its pointer now stands for placedFor (null when no qualifying command), wherever it
        // was placed, the same coordinates included.
        public void Record(TUnit unit, TCommand placedFor, int frame)
        {
            if (unit == null) return;
            var placement = placements.GetOrCreateValue(unit);
            placement.Command = placedFor == null ? null : new WeakReference(placedFor);
            placement.Frame = frame;
        }

        public bool PlacedFor(TUnit unit, TCommand command, out int frame)
        {
            frame = -1;
            if (unit == null || command == null || !placements.TryGetValue(unit, out var placement)) return false;
            frame = placement.Frame;
            return ReferenceEquals(placement.Command?.Target, command);
        }

        // World replacement, disable and disposal: nothing recorded survives.
        public void Reset() => placements = new ConditionalWeakTable<TUnit, Placement>();
    }
}
