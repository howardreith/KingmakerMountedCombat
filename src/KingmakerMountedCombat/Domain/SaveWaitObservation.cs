using System;
using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // The readiness term that held a deferred save, in the order the production
    // predicate evaluates them. None means every term is settled.
    internal enum SaveWaitBlockerKind
    {
        None,
        AdmissionFence,
        DiagnosticFault,
        PairOwnership,
        NativePreparation,
        RelationshipTransition,
        RelationshipShell,
        Command,
        RelationshipShellCommand,
        Projectile,
        Ability
    }

    // What the native world did to a waiting save, decided once per tick.
    internal enum SaveWaitWorldState
    {
        Unchanged,
        WorldReplaced,
        LoadingScreen,
        ModeChanged
    }

    internal enum SaveWaitOutcome
    {
        Serialized,
        TimedOut,
        ModeChanged,
        LoadingScreen,
        WorldReplaced,
        Cancelled,
        Faulted
    }

    // Incident 2026-11-22: one generic "the native world changed" covered a
    // replaced world, a loading screen and an ordinary menu or dialog. The
    // decision is unchanged (each still refuses the save before serialization);
    // only the reason is now named.
    internal static class SaveWaitWorldPolicy
    {
        // The existing per-request bounded wait (unchanged): a fallback, not a repair.
        internal const double MaximumWaitSeconds = 30d;

        internal static SaveWaitWorldState Decide(bool worldBound, bool sameWorld, bool manualLoadingScreen, bool ordinaryPlay)
        {
            if (!worldBound || !sameWorld) return SaveWaitWorldState.WorldReplaced;
            if (manualLoadingScreen) return SaveWaitWorldState.LoadingScreen;
            if (!ordinaryPlay) return SaveWaitWorldState.ModeChanged;
            return SaveWaitWorldState.Unchanged;
        }

        internal static SaveWaitOutcome Outcome(SaveWaitWorldState state)
        {
            switch (state)
            {
                case SaveWaitWorldState.WorldReplaced: return SaveWaitOutcome.WorldReplaced;
                case SaveWaitWorldState.LoadingScreen: return SaveWaitOutcome.LoadingScreen;
                case SaveWaitWorldState.ModeChanged: return SaveWaitOutcome.ModeChanged;
                default: throw new ArgumentOutOfRangeException(nameof(state), "An unchanged world is not a failed wait.");
            }
        }

        // Player-facing; "while it waited for X" names the blocker the save was
        // actually holding for, so a menu opened during a long wait is not read
        // as an area change.
        internal static string FailureMessage(SaveWaitWorldState state, string mode, string waitingFor)
        {
            var subject = string.IsNullOrEmpty(waitingFor) ? "current actions to finish" : waitingFor;
            switch (state)
            {
                case SaveWaitWorldState.WorldReplaced:
                    return "Save was not written: its native world was replaced while it waited for " + subject + ".";
                case SaveWaitWorldState.LoadingScreen:
                    return "Save was not written: a loading screen started while it waited for " + subject + ".";
                case SaveWaitWorldState.ModeChanged:
                    return "Save was not written: " + DescribeMode(mode) + " while it waited for " + subject + ".";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), "An unchanged world is not a failed wait.");
            }
        }

        internal static string TimeoutMessage(double seconds, string waitingFor) =>
            "Save was not written: " + (string.IsNullOrEmpty(waitingFor) ? "the current native effect" : waitingFor) +
            " did not finish within " + seconds.ToString("0", CultureInfo.InvariantCulture) + " seconds.";

        // Kingmaker 2.1.7b GameModeType names; an unknown name is reported as is.
        internal static string DescribeMode(string mode)
        {
            switch (mode)
            {
                case "EscMode": return "the game menu opened";
                case "FullScreenUi": return "a full-screen window opened";
                case "Dialog": return "a dialog started";
                case "Cutscene": case "CutsceneGlobalMap": return "a cutscene started";
                case "Rest": return "resting started";
                case "GlobalMap": return "the world map opened";
                case "Kingdom": case "KingdomSettlement": return "the kingdom screen opened";
                case "GameOver": return "the game ended";
                case "BugReport": return "the bug report window opened";
                default: return "the game mode changed to " + (string.IsNullOrEmpty(mode) ? "an unknown mode" : mode);
            }
        }
    }

    // One deferred save request's wait, observed from the production readiness
    // result. Holds strings and an identity number only, never the blocking
    // object, so the observer cannot keep a finished command or projectile
    // alive. Reports only transitions and the end, with a bounded count.
    internal sealed class SaveWaitObservation
    {
        internal const int MaximumTransitionReports = 12;

        private readonly long request;
        private readonly string label;
        private readonly double began;
        private SaveWaitBlockerKind kind;
        private int identity;
        private string summary;
        private string detail;
        private int changes;
        private int suppressed;
        private bool ended;

        internal SaveWaitObservation(long request, string label, double now)
        {
            this.request = request;
            this.label = string.IsNullOrEmpty(label) ? "<unnamed>" : label;
            began = now;
        }

        internal long Request => request;
        internal SaveWaitBlockerKind Kind => kind;
        internal int Changes => changes;
        internal bool HasBlocker => kind != SaveWaitBlockerKind.None;
        internal bool Ended => ended;

        // The short player-facing name of what the save is waiting for.
        internal string WaitingFor => summary;

        // Returns a report line when the blocker differs from the last one seen,
        // otherwise null. A settled (None) result changes nothing: the end line
        // names the last blocker, and a later blocker is a new transition.
        internal string Observe(SaveWaitBlockerKind observed, int observedIdentity,
            Func<string> describeSummary, Func<string> describeDetail, double now)
        {
            if (ended || observed == SaveWaitBlockerKind.None) return null;
            if (observed == kind && observedIdentity == identity) return null;
            kind = observed;
            identity = observedIdentity;
            summary = Safe(describeSummary);
            detail = Safe(describeDetail);
            changes++;
            if (changes > MaximumTransitionReports)
            {
                suppressed++;
                return null;
            }
            return "Mounted save wait: request=" + request + "; name=" + label + "; t=" + Seconds(now - began) +
                "s; blocker=" + kind + "; waitingFor=" + summary + "; detail=[" + detail + "]; change=" + changes +
                (changes == MaximumTransitionReports ? "; further changes are counted, not reported" : string.Empty) + ".";
        }

        // Exactly one end line per observed wait; a wait that never saw a
        // blocker (a save that serialized at once) reports nothing.
        internal string End(SaveWaitOutcome outcome, string reason, double now)
        {
            if (ended) return null;
            ended = true;
            if (changes == 0) return null;
            return "Mounted save wait ended: request=" + request + "; name=" + label + "; outcome=" + outcome +
                (string.IsNullOrEmpty(reason) ? string.Empty : "; reason=" + reason) + "; elapsed=" + Seconds(now - began) +
                "s; lastBlocker=" + kind + "; waitingFor=" + summary + "; detail=[" + detail + "]; blockerChanges=" + changes +
                "; unreported=" + suppressed + ".";
        }

        private static string Safe(Func<string> describe)
        {
            if (describe == null) return "<none>";
            try { return describe() ?? "<none>"; }
            catch (Exception exception) { return "<description failed: " + exception.GetType().Name + ">"; }
        }

        private static string Seconds(double value) =>
            double.IsNaN(value) || double.IsInfinity(value) ? "?" : value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
