using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Immutable identity of one native request. A process does not exist at Init/click;
    // the complete identity is frozen when native OnAction creates it. Earlier samples
    // are compared by reference to its command and shell, never filled in retroactively.
    internal sealed class RelationshipCommandIdentity
    {
        internal RelationshipCommandIdentity(object command, string control, object process,
            object context, string caster, string target, long generation, string action, string ability)
        {
            Command = command; Control = control; Process = process; Context = context;
            Caster = caster; Target = target; Generation = generation; Action = action; Ability = ability;
        }
        internal object Command { get; }
        internal string Control { get; }
        internal object Process { get; }
        internal object Context { get; }
        internal string Caster { get; }
        internal string Target { get; }
        internal long Generation { get; }
        internal string Action { get; }
        internal string Ability { get; }
        internal bool Complete => Command != null && Process != null && Context != null &&
            !string.IsNullOrEmpty(Control) && !string.IsNullOrEmpty(Caster) &&
            !string.IsNullOrEmpty(Target) && Action == "Move" && !string.IsNullOrEmpty(Ability);

        internal bool Matches(RelationshipCommandIdentity sample, bool beforeProcess)
        {
            if (!Complete || sample == null || !ReferenceEquals(Command, sample.Command) ||
                Control != sample.Control || Caster != sample.Caster || Target != sample.Target ||
                Generation != sample.Generation || Action != sample.Action || Ability != sample.Ability)
                return false;
            return beforeProcess && sample.Process == null && sample.Context == null ||
                ReferenceEquals(Process, sample.Process) && ReferenceEquals(Context, sample.Context);
        }
    }

    internal static class NativeResourceWindowPolicy
    {
        internal static bool EndpointConserved(double before, double after, double elapsed,
            bool turnBased, double tolerance)
        {
            if (double.IsNaN(before) || double.IsNaN(after) || double.IsNaN(elapsed) ||
                double.IsInfinity(before) || double.IsInfinity(after) || double.IsInfinity(elapsed) ||
                elapsed < 0 || tolerance < 0) return false;
            var expected = turnBased ? before : Math.Max(0, before - elapsed);
            return Math.Abs(after - expected) <= tolerance;
        }

        internal static bool ExactMoveCallback(bool acted, bool inCombat, bool turnBased,
            double timeSinceStart, double beforeMove, double afterMove,
            double beforeStandard, double afterStandard, double beforeSwift, double afterSwift)
        {
            if (!acted || double.IsNaN(timeSinceStart) || timeSinceStart < 0) return false;
            var expected = !inCombat ? beforeMove : turnBased ? beforeMove + 3 : 3 - timeSinceStart;
            return Math.Abs(afterMove - expected) <= 0.0001 &&
                Math.Abs(afterStandard - beforeStandard) <= 0.0001 &&
                Math.Abs(afterSwift - beforeSwift) <= 0.0001;
        }
    }
}
