using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Pure admission rules over freshly captured native observations. Returning
    // no errors does not authorize load, save, restore, UI input, XP or money.
    // The owning runtime holds exact world/save custody and calls this immediately
    // before constructing the unchanged native BuildDriver.
    internal sealed class Chunk7ManualDerivedNativeReadiness
    {
        internal string MainActorId;
        internal int MainLevel, MainExperience, ExperienceRatePercent, NativeHireCost, DefaultHireExperience;
        internal long Money;
        internal string[] PartyCharacterIds;
        internal int[] XpThresholds;
        internal string ExistingActorId;
        internal int ExistingLevel, ExistingExperience, ExistingRangerLevel;
        internal bool TargetLiving, TargetIsPet, TargetHasPet, TargetHasDualCompanion, TargetInField, TargetIdle, TargetCanLevelUp;
        internal bool ExactSmallCatalogueMember;
        internal bool ExactMediumBodyMember;
        internal bool NativeDepartureCompleted;
        internal int NativeDepartureHireCost;
    }
    internal static class Chunk7ManualDerivedAdmission
    {
        internal static IReadOnlyList<string> Validate(Chunk7ManualDerivedSelection selection, string runId,
            Chunk7ManualDerivedNativeReadiness actual, bool cumulativeAuto = false, bool cumulativeManual = false)
        {
            if (selection == null) return new[] { "Manual-derived selection is missing." };
            var errors = new List<string>(selection.Validate(Chunk7ManualDerivedSelection.Scenario, runId, cumulativeAuto, cumulativeManual));
            if (actual == null) { errors.Add("Fresh native readiness observation is missing."); return errors; }
            if (errors.Count != 0) return errors;
            var pins = selection.Readiness;
            // Pins always describe the unchanged six-character input. Only the
            // owning scenario can supply completion of the exact native selector
            // operation; a caller cannot request a projected roster as input.
            var expectedParty = selection.UsesF09FiveHorseCold
                ? pins.PartyCharacterIds.Where(id => id != Chunk7ManualDerivedSelection.F09DepartureActorId).ToArray()
                : pins.PartyCharacterIds;
            if (selection.UsesF09FiveHorseCold && (!actual.NativeDepartureCompleted || actual.NativeDepartureHireCost <= 0 ||
                actual.NativeHireCost != actual.NativeDepartureHireCost))
                errors.Add("The declared native field departure and retained native hire-price observation are required.");
            if (actual.MainActorId != pins.MainActorId || actual.MainLevel != pins.MainLevel || actual.MainExperience != pins.MainExperience ||
                actual.Money != pins.Money || actual.ExperienceRatePercent != pins.ExperienceRatePercent ||
                (!selection.UsesF09FiveHorseCold && actual.NativeHireCost != pins.NativeHireCost) || actual.DefaultHireExperience != pins.DefaultHireExperience ||
                actual.PartyCharacterIds == null || !actual.PartyCharacterIds.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expectedParty))
                errors.Add("Live native readiness differs from the qualified source pins.");
            var table = actual.XpThresholds;
            if (table == null || table.Length != 21 || table.Any(value => value < 0) ||
                table.Skip(1).Where((value, index) => value < table[index]).Any())
            { errors.Add("Native XP table indices zero through twenty are missing or unordered."); return errors; }
            if (actual.MainLevel < 1 || actual.MainLevel > 20 || actual.DefaultHireExperience != table[actual.MainLevel])
                errors.Add("Native default hire XP does not match the attained main level's table entry.");
            if (selection.PaidHire)
            {
                if (actual.MainLevel < Chunk7ManualDerivedSelection.RangerAcquisitionLevel ||
                    ReadyLevel(table, actual.DefaultHireExperience) < Chunk7ManualDerivedSelection.RangerAcquisitionLevel ||
                    actual.NativeHireCost <= 0 || actual.Money < actual.NativeHireCost || expectedParty.Length >= 6 ||
                    (selection.Case == Chunk7ManualDerivedSelection.PaidSmallCase ? !actual.ExactSmallCatalogueMember : !actual.ExactMediumBodyMember))
                    errors.Add("A legal paid hire lacks attained level, funds, its exact declared body or a native field slot.");
            }
            else
            {
                if (actual.ExistingActorId != selection.ExistingActorId || actual.ExistingLevel != selection.ExistingLevel ||
                    actual.ExistingExperience != selection.ExistingExperience || actual.ExistingRangerLevel != selection.ExistingRangerLevel)
                    errors.Add("The live existing actor's exact progression differs from source pins.");
                if (!actual.TargetLiving || actual.TargetIsPet || actual.TargetHasPet || actual.TargetHasDualCompanion || !actual.TargetInField ||
                    !actual.TargetIdle || !actual.TargetCanLevelUp || actual.ExistingLevel < 1 || actual.ExistingLevel >= 20 ||
                    actual.ExistingRangerLevel < 0 || actual.ExistingRangerLevel >= Chunk7ManualDerivedSelection.RangerAcquisitionLevel ||
                    actual.ExistingRangerLevel + ReadyLevel(table, actual.ExistingExperience) - actual.ExistingLevel < Chunk7ManualDerivedSelection.RangerAcquisitionLevel)
                    errors.Add("Existing owner lacks a living idle pet-free native Ranger acquisition opportunity.");
            }
            return errors;
        }
        private static int ReadyLevel(int[] table, int xp)
        { var level = 0; while (level < 20 && table[level + 1] <= xp) level++; return level; }
    }
}
