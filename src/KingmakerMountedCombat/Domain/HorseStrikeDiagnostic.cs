using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-07 owner-log evidence (review R1-F13): one line per native horse strike at its act event. playback separates the
    // cases an owner cannot tell apart on screen: no-handle (native had no animation handle for this strike, so nothing
    // played), interrupted, not-acted (the handle never reached its act frame), and played (the handle started and acted;
    // with speedScale above 1 the native routine compressed every limb's clip into its window, so a short bite is fast).
    public static class HorseStrikeDiagnostic
    {
        public static string Playback(bool hasHandle, bool started, bool acted, bool interrupted) =>
            !hasHandle ? "no-handle" : interrupted ? "interrupted" : !started || !acted ? "not-acted" : "played";

        public static string Format(int index, int count, string weapon, string special, bool hasHandle, string action,
            bool started, bool acted, bool interrupted, float speedScale, int variant, int variants, string target) =>
            "Horse strike: index=" + index + "/" + count + "; weapon=" + (weapon ?? "<none>") + "; special=" + (special ?? "<none>") +
            "; playback=" + Playback(hasHandle, started, acted, interrupted) +
            (hasHandle ? "; action=" + (action ?? "<unnamed>") + "; speedScale=" + speedScale.ToString("R", CultureInfo.InvariantCulture) +
                "; variant=" + variant + "/" + variants : string.Empty) +
            "; target=" + (target ?? "<none>") + ".";
    }
}
