using System;

namespace KingmakerMountedCombat.Integration
{
    // PR-5 round 7 (R4-F2): the one engine metadata read of a Horse animation action's asset name (UnityEngine.Object.name, an engine
    // call that is unavailable outside Unity). Registration, refresh, the binding report, the strike line and the terminal description
    // all read action names here, so an offline fixture can substitute exactly this read and nothing else. Production reads the engine.
    internal static class HorseAnimationActionMetadata
    {
        internal static Func<UnityEngine.Object, string> EngineName = action => action.name;

        internal static string Name(UnityEngine.Object action) => ReferenceEquals(action, null) ? null : EngineName(action);
    }
}
