using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues S01 (2026-11-15). Diagnostic only: the saving report's cause is not established, and nothing here
    // repairs it. The request line now says which native return a request takes, because every refusal after
    // SaveManager.IsSaveAllowed is silent in the native log. That the installed Game.SaveGame has exactly this order is
    // checked against its IL by scripts/Test-PersistenceContracts.ps1.
    internal static class SaveRequestObservationTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("S01 diagnostic: the native save request outcome follows the installed Game.SaveGame order for every input", OutcomeFollowsNativeOrder);
            runner.Run("S01 diagnostic: each silent native refusal is named, and a one-save campaign refuses manual and quick saves", SilentRefusalsAreNamed);
        }

        private enum SaveType { Manual, Quick, Auto, IronMan, ForImport }

        // Game.SaveGame (06000CE4) as installed, statement by statement.
        private static NativeSaveRequestOutcome Native(SaveType type, bool saveAllowed, bool onlyOneSave, bool oneSaveSlot, bool autosaveEnabled)
        {
            if (type != SaveType.ForImport)
            {
                if (!saveAllowed) return NativeSaveRequestOutcome.RefusedSaveNotAllowed;
                if (onlyOneSave)
                {
                    if (!oneSaveSlot) return NativeSaveRequestOutcome.RefusedOneSaveOtherSlot;
                    if (type != SaveType.Auto && type != SaveType.IronMan) return NativeSaveRequestOutcome.RefusedOneSaveManualType;
                    type = SaveType.IronMan;
                }
                if (type == SaveType.Auto && !autosaveEnabled) return NativeSaveRequestOutcome.SkippedAutosaveDisabled;
            }
            return NativeSaveRequestOutcome.Queued;
        }

        private static NativeSaveRequestOutcome Classify(SaveType type, bool saveAllowed, bool onlyOneSave, bool oneSaveSlot, bool autosaveEnabled) =>
            NativeSaveRequestPolicy.Classify(type == SaveType.ForImport, saveAllowed, onlyOneSave, oneSaveSlot, type == SaveType.Auto,
                type == SaveType.IronMan, autosaveEnabled);

        private static void OutcomeFollowsNativeOrder()
        {
            var cases = 0;
            foreach (SaveType type in Enum.GetValues(typeof(SaveType)))
                for (var bits = 0; bits < 16; bits++)
                {
                    bool allowed = (bits & 1) != 0, one = (bits & 2) != 0, slot = (bits & 4) != 0, autosave = (bits & 8) != 0;
                    TestRunner.Equal(Native(type, allowed, one, slot, autosave), Classify(type, allowed, one, slot, autosave),
                        "The observed outcome differs from the native order for " + type + " allowed=" + allowed + " onlyOneSave=" + one +
                        " oneSaveSlot=" + slot + " autosaveEnabled=" + autosave + ".");
                    cases++;
                }
            TestRunner.Equal(80, cases, "The decision table was not covered.");
        }

        private static void SilentRefusalsAreNamed()
        {
            // An ordinary campaign: manual, quick and enabled autosaves are queued; a disabled autosave is skipped.
            TestRunner.Equal(NativeSaveRequestOutcome.Queued, Classify(SaveType.Manual, true, false, false, true), "An ordinary manual save was not queued.");
            TestRunner.Equal(NativeSaveRequestOutcome.Queued, Classify(SaveType.Quick, true, false, true, false), "An ordinary quick save was not queued.");
            TestRunner.Equal(NativeSaveRequestOutcome.SkippedAutosaveDisabled, Classify(SaveType.Auto, true, false, true, false), "A disabled autosave was not skipped.");
            // A one-save campaign: only the single slot is written, and only by an autosave; the autosave setting does not apply.
            TestRunner.Equal(NativeSaveRequestOutcome.RefusedOneSaveManualType, Classify(SaveType.Manual, true, true, true, true), "A one-save manual save was not refused.");
            TestRunner.Equal(NativeSaveRequestOutcome.RefusedOneSaveManualType, Classify(SaveType.Quick, true, true, true, true), "A one-save quick save was not refused.");
            TestRunner.Equal(NativeSaveRequestOutcome.RefusedOneSaveOtherSlot, Classify(SaveType.Auto, true, true, false, true), "A one-save write to another slot was not refused.");
            TestRunner.Equal(NativeSaveRequestOutcome.Queued, Classify(SaveType.Auto, true, true, true, false), "The one-save autosave was skipped by the autosave setting.");
            // IsSaveAllowed is decided first; an import descriptor skips every check.
            TestRunner.Equal(NativeSaveRequestOutcome.RefusedSaveNotAllowed, Classify(SaveType.Manual, false, true, false, false), "IsSaveAllowed was not the first refusal.");
            TestRunner.Equal(NativeSaveRequestOutcome.Queued, Classify(SaveType.ForImport, false, true, false, false), "An import descriptor was checked.");
            // Every outcome has its own description, and only one of them says the request was queued.
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (NativeSaveRequestOutcome outcome in Enum.GetValues(typeof(NativeSaveRequestOutcome)))
            {
                var text = NativeSaveRequestPolicy.Describe(outcome);
                TestRunner.True(!string.IsNullOrWhiteSpace(text) && text != "unknown" && seen.Add(text), outcome + " has no distinct description.");
                TestRunner.Equal(outcome == NativeSaveRequestOutcome.Queued, text == "queued", outcome + " is described as queued or not queued incorrectly.");
            }
        }
    }
}
