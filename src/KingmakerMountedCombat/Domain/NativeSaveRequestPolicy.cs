namespace KingmakerMountedCombat.Domain
{
    // Known issues S01 (diagnostic only): what the installed Game.SaveGame (06000CE4) does with one request, in its own
    // order. Only the first refusal goes through SaveManager.IsSaveAllowed; the one-save (Last Azlanti) refusals and the
    // disabled-autosave return follow it and write nothing to the log. Every refusal and the skip return before
    // SaveManager.SaveRoutine is constructed, so no mounted save code runs for them.
    public enum NativeSaveRequestOutcome
    {
        // The request reaches LoadingProcess.StartLoadingProcess(SaveManager.SaveRoutine(...)).
        Queued,
        RefusedSaveNotAllowed,
        // One-save mode: the descriptor is not the campaign's single slot.
        RefusedOneSaveOtherSlot,
        // One-save mode: a manual or quick save of the single slot (only Auto and IronMan types are written).
        RefusedOneSaveManualType,
        SkippedAutosaveDisabled
    }

    public static class NativeSaveRequestPolicy
    {
        public static NativeSaveRequestOutcome Classify(
            bool forImport,
            bool saveAllowed,
            bool onlyOneSave,
            bool oneSaveSlot,
            bool autoType,
            bool ironManType,
            bool autosaveEnabled)
        {
            // An import descriptor skips every check.
            if (forImport) return NativeSaveRequestOutcome.Queued;
            if (!saveAllowed) return NativeSaveRequestOutcome.RefusedSaveNotAllowed;
            if (onlyOneSave)
            {
                if (!oneSaveSlot) return NativeSaveRequestOutcome.RefusedOneSaveOtherSlot;
                if (!autoType && !ironManType) return NativeSaveRequestOutcome.RefusedOneSaveManualType;
                // Native rewrites the type to IronMan here, so the autosave setting no longer applies to it.
                return NativeSaveRequestOutcome.Queued;
            }
            return autoType && !autosaveEnabled ? NativeSaveRequestOutcome.SkippedAutosaveDisabled : NativeSaveRequestOutcome.Queued;
        }

        public static string Describe(NativeSaveRequestOutcome outcome)
        {
            switch (outcome)
            {
                case NativeSaveRequestOutcome.Queued: return "queued";
                case NativeSaveRequestOutcome.RefusedSaveNotAllowed: return "refused (saving is not allowed now)";
                case NativeSaveRequestOutcome.RefusedOneSaveOtherSlot: return "refused (one-save mode: not the campaign's single save)";
                case NativeSaveRequestOutcome.RefusedOneSaveManualType: return "refused (one-save mode: manual and quick saves are not written)";
                case NativeSaveRequestOutcome.SkippedAutosaveDisabled: return "skipped (autosave is disabled)";
                default: return "unknown";
            }
        }
    }
}
