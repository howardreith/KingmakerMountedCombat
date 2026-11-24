using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-07 owner-log evidence (reviews R1-F13, R2-F4): one line per native strike of the exact KMC Horse, read at the
    // strike's own act event. It states handle facts literally (none, or its action, started/acted/interrupted, speedScale and
    // variant) and the installed playback boundary's facts for that handle (the active AnimationBase's clip, weight and state).
    // It does not claim what was rendered: whether the bite is visible is a footage comparison the owner makes.
    public static class HorseStrikeDiagnostic
    {
        public static string HandleState(bool hasHandle, bool started, bool acted, bool interrupted) =>
            !hasHandle ? "none" : (started ? "started" : "not-started") + "," + (acted ? "acted" : "not-acted") + (interrupted ? ",interrupted" : string.Empty);

        public static string Format(string horse, int index, int count, string weapon, string special, bool hasHandle, string action,
            bool started, bool acted, bool interrupted, float speedScale, int variant, int variants, string activeClip, float clipWeight,
            string animationState, string target) =>
            "Horse strike: horse=" + (horse ?? "<none>") + "; index=" + index + "/" + count + "; weapon=" + (weapon ?? "<none>") +
            "; special=" + (special ?? "<none>") + "; handle=" + HandleState(hasHandle, started, acted, interrupted) +
            (hasHandle ? "; action=" + (action ?? "<unnamed>") + "; speedScale=" + speedScale.ToString("R", CultureInfo.InvariantCulture) +
                "; variant=" + variant + "/" + variants + "; activeClip=" + (activeClip ?? "<none>") +
                "; clipWeight=" + clipWeight.ToString("R", CultureInfo.InvariantCulture) + "; animationState=" + (animationState ?? "<none>")
                : string.Empty) +
            "; target=" + (target ?? "<none>") + ".";
    }
}
