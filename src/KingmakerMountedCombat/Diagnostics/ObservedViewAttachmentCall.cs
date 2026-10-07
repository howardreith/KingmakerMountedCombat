using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native RestoreView passes null; AttachToViewOnLoad creates the view itself.
    // The argument and the resulting view are separate identities in that call.
    internal sealed class ObservedViewAttachmentCall
    {
        private object beforeView, notifiedView;
        private int invocation;
        internal object Argument { get; private set; }
        internal int Pending { get; private set; }

        internal int Begin(object argument, object currentView)
        {
            if (Pending != 0) throw new InvalidOperationException("Nested native view attachment.");
            Argument = argument; beforeView = currentView; notifiedView = null;
            return Pending = ++invocation;
        }

        internal int Notify(object currentView)
        {
            if (Pending == 0 || notifiedView != null || currentView == null ||
                (Argument != null ? !ReferenceEquals(Argument, currentView) : ReferenceEquals(beforeView, currentView)))
                throw new InvalidOperationException("View notification lacks its exact native replacement call.");
            notifiedView = currentView;
            return Pending;
        }

        internal int End(object argument, object currentView)
        {
            if (Pending == 0 || notifiedView == null || !ReferenceEquals(Argument, argument) ||
                !ReferenceEquals(notifiedView, currentView))
                throw new InvalidOperationException("Unmatched native view return.");
            var completed = Pending;
            Pending = 0; Argument = null; beforeView = null; notifiedView = null;
            return completed;
        }
    }
}
