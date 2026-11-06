namespace KingmakerMountedCombat.Diagnostics
{
    internal enum Finalize3EscModeAction { None, CloseTutorial, CloseEscMenu, RefuseTutorialSetting, RefuseUnidentified }

    // F3 owned-run cleanup: at most one EscMode close, only after the fixture's own restorations,
    // only through the native window that owns the mode, never by pressing Escape.
    internal static class Finalize3CleanupEscModePolicy
    {
        internal static Finalize3EscModeAction Decide(bool restorationsDone, bool alreadyIntervened, bool escMode, bool paused,
            bool tutorialShown, bool? tutorialDontShow, bool escMenuShown)
        {
            if (!restorationsDone || alreadyIntervened || !escMode || paused) return Finalize3EscModeAction.None;
            if (tutorialShown)
                return tutorialDontShow == false ? Finalize3EscModeAction.CloseTutorial : Finalize3EscModeAction.RefuseTutorialSetting;
            return escMenuShown ? Finalize3EscModeAction.CloseEscMenu : Finalize3EscModeAction.RefuseUnidentified;
        }
    }
}
