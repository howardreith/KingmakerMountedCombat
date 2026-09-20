using System;

namespace KingmakerMountedCombat.Domain
{
    // Native campaign identity is published after actor PostLoad. Bind the new
    // player object inside the selected load transaction before that late field.
    // No actor or world reference can cross a canceled/replaced restoration.
    internal sealed class NativeLoadWorld<TWorld> where TWorld : class
    {
        private TWorld previous;
        private TWorld current;
        private bool started;
        private bool closed;
        internal bool NativeCompleted { get; private set; }

        internal void Begin(TWorld oldWorld)
        {
            if (started || closed) throw new InvalidOperationException("Load world already started or canceled.");
            previous = oldWorld;
            started = true;
        }

        internal bool TryBind(TWorld world)
        {
            if (!started || closed || world == null || ReferenceEquals(world, previous)) return false;
            if (current != null) return ReferenceEquals(current, world);
            current = world;
            return true;
        }

        internal void Complete(TWorld world)
        {
            if (!TryBind(world)) throw new InvalidOperationException("Native load did not publish a distinct world.");
            NativeCompleted = true;
            previous = null;
        }

        internal bool CanPresent(TWorld world) => NativeCompleted && !closed &&
            current != null && ReferenceEquals(current, world);

        internal void Close()
        {
            closed = true;
            previous = null;
            current = null;
        }
    }
}
