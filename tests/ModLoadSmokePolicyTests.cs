using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    /// <summary>
    /// The mod-load smoke changes nothing, so its verdict rests entirely on what it read.
    /// These tests hold two properties that matter more than the happy path: every
    /// forbidden active state is refused with its own named reason, and a field the run
    /// never observed refuses rather than reading as clean.
    /// </summary>
    internal static class ModLoadSmokePolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("a clean observed load passes with no errors", CleanLoadPasses);
            runner.Run("the smoke refuses every forbidden active state by name", RefusesEveryActiveState);
            runner.Run("an unobserved field refuses instead of reading as clean", RefusesUnobservedFields);
            runner.Run("the smoke refuses an inexact identity and a mutated setting", RefusesInexactIdentity);
            runner.Run("the published field set matches the observation it describes", PublishedFieldsMatchObservation);
        }

        private static ModLoadSmokeObservation CleanObservation()
        {
            return new ModLoadSmokeObservation
            {
                LoadedModId = "KingmakerMountedCombat",
                ExpectedModId = "KingmakerMountedCombat",
                ProductVersion = "0.1.0-chunk6a-preview.109",
                ExpectedProductVersion = "0.1.0-chunk6a-preview.109",
                GamePresent = true,
                LoadedAreaPresent = false,
                CurrentGameMode = "None",
                // The product really does ship the movement experiment ENABLED. The smoke
                // publishes that rather than suppressing it, and it is not a failure at a
                // menu with no area loaded.
                ShippedMovementExperimentEnabled = true,
                ShippedPairedActivationEnabled = false,
                ShippedUnifiedMountedTurnEnabled = false,
                ShippedPairedCommandSchedulerEnabled = false,
                ShippedDiagnosticOverlayEnabled = false,
                SettingsMutatedByScenario = false,
                RelationshipState = "Unmounted",
                SaveRequestCount = 0,
                LoadRequestCount = 0,
                RelationshipTransitionInFlight = false,
                RegisteredRelationshipShellCount = 0,
                RelationshipProcessBindingCount = 0,
                PoisonedExecutionContextCount = 0,
                NativeCastRequestCount = 0,
                DispatchAcceptedCount = 0,
                DispatchRejectedCount = 0,
                PairedActivationPresent = false,
                PairedPartnerContextPresent = false,
                ManagedControlFactCount = 0,
                DuplicateControlFactCount = 0,
                ManagedHotbarSlotCount = 0,
                OverlayObjectPresent = false,
                ControlServiceSerializationSuspended = false
            };
        }

        private static void CleanLoadPasses()
        {
            var errors = ModLoadSmokePolicy.Validate(CleanObservation());
            TestRunner.Equal(0, errors.Count,
                "A clean observed load produced errors: " + string.Join(" ", errors));
            // A shipped movement experiment that is ENABLED is lawful; it is published, not gated.
            var disabled = CleanObservation();
            disabled.ShippedMovementExperimentEnabled = false;
            TestRunner.Equal(0, ModLoadSmokePolicy.Validate(disabled).Count,
                "The shipped movement-experiment default was treated as a gate rather than an observation.");
            var overlayShipped = CleanObservation();
            overlayShipped.ShippedDiagnosticOverlayEnabled = true;
            TestRunner.Equal(0, ModLoadSmokePolicy.Validate(overlayShipped).Count,
                "A shipped diagnostic-overlay setting was treated as a live overlay object.");
            try
            {
                ModLoadSmokePolicy.Validate(null);
                TestRunner.True(false, "A null observation was validated.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        private static void RefusesEveryActiveState()
        {
            AssertRefused("a loaded campaign area", "loaded campaign area",
                o => o.LoadedAreaPresent = true);
            AssertRefused("save traffic", "save/load traffic", o => o.SaveRequestCount = 1);
            AssertRefused("load traffic", "save/load traffic", o => o.LoadRequestCount = 1);
            AssertRefused("a mounted relationship", "instead of Unmounted",
                o => o.RelationshipState = "Mounted");
            AssertRefused("a faulted relationship", "instead of Unmounted",
                o => o.RelationshipState = "Faulted");
            AssertRefused("a transition in flight", "a mounted transition in flight",
                o => o.RelationshipTransitionInFlight = true);
            AssertRefused("a registered shell", "registered mounted relationship shell",
                o => o.RegisteredRelationshipShellCount = 1);
            AssertRefused("a process binding", "mounted relationship process binding",
                o => o.RelationshipProcessBindingCount = 1);
            AssertRefused("a poisoned context", "poisoned mounted execution context",
                o => o.PoisonedExecutionContextCount = 1);
            AssertRefused("a native cast request", "native mounted cast request",
                o => o.NativeCastRequestCount = 1);
            AssertRefused("an accepted dispatch", "accepted mounted dispatch",
                o => o.DispatchAcceptedCount = 1);
            AssertRefused("a rejected dispatch", "rejected mounted dispatch",
                o => o.DispatchRejectedCount = 1);
            AssertRefused("a paired activation", "a live paired activation",
                o => o.PairedActivationPresent = true);
            AssertRefused("a partner turn context", "private paired partner turn context",
                o => o.PairedPartnerContextPresent = true);
            AssertRefused("a leased control fact", "leased mounted control fact",
                o => o.ManagedControlFactCount = 1);
            AssertRefused("a duplicate control fact", "duplicate mounted control fact",
                o => o.DuplicateControlFactCount = 1);
            AssertRefused("a managed hotbar slot", "managed mounted hotbar slot",
                o => o.ManagedHotbarSlotCount = 1);
            AssertRefused("an overlay object", "a diagnostic overlay object",
                o => o.OverlayObjectPresent = true);
            AssertRefused("a suspended serialization scope", "suspended mounted control serialization scope",
                o => o.ControlServiceSerializationSuspended = true);
        }

        private static void RefusesUnobservedFields()
        {
            // A null is "not observed". It must never read as absent or zero, because that
            // is exactly how a run that failed before observing anything would pass.
            AssertRefused("an unobserved shipped movement default", "did not record",
                o => o.ShippedMovementExperimentEnabled = null);
            AssertRefused("an unobserved shipped paired-activation default", "did not record",
                o => o.ShippedPairedActivationEnabled = null);
            AssertRefused("an unobserved shipped unified-turn default", "did not record",
                o => o.ShippedUnifiedMountedTurnEnabled = null);
            AssertRefused("an unobserved shipped scheduler default", "did not record",
                o => o.ShippedPairedCommandSchedulerEnabled = null);
            AssertRefused("an unobserved shipped overlay default", "did not record",
                o => o.ShippedDiagnosticOverlayEnabled = null);
            AssertRefused("an unobserved transition state", "did not observe whether",
                o => o.RelationshipTransitionInFlight = null);
            AssertRefused("an unobserved paired activation", "did not observe whether",
                o => o.PairedActivationPresent = null);
            AssertRefused("an unobserved partner context", "did not observe whether",
                o => o.PairedPartnerContextPresent = null);
            AssertRefused("an unobserved overlay object", "did not observe whether",
                o => o.OverlayObjectPresent = null);
            AssertRefused("an unobserved serialization scope", "did not observe whether",
                o => o.ControlServiceSerializationSuspended = null);
            AssertRefused("an uncounted shell registration", "did not count",
                o => o.RegisteredRelationshipShellCount = null);
            AssertRefused("an uncounted process binding", "did not count",
                o => o.RelationshipProcessBindingCount = null);
            AssertRefused("an uncounted poisoned context", "did not count",
                o => o.PoisonedExecutionContextCount = null);
            AssertRefused("an uncounted cast request", "did not count",
                o => o.NativeCastRequestCount = null);
            AssertRefused("an uncounted accepted dispatch", "did not count",
                o => o.DispatchAcceptedCount = null);
            AssertRefused("an uncounted rejected dispatch", "did not count",
                o => o.DispatchRejectedCount = null);
            AssertRefused("an uncounted control fact", "did not count",
                o => o.ManagedControlFactCount = null);
            AssertRefused("an uncounted duplicate fact", "did not count",
                o => o.DuplicateControlFactCount = null);
            AssertRefused("an uncounted hotbar slot", "did not count",
                o => o.ManagedHotbarSlotCount = null);

            // The bootstrap-failure shape -- nothing observed at all -- must produce a
            // refusal for every unobserved field rather than a small handful.
            var nothing = new ModLoadSmokeObservation
            {
                LoadedModId = "KingmakerMountedCombat",
                ExpectedModId = "KingmakerMountedCombat",
                ProductVersion = "0.1.0-chunk6a-preview.109",
                ExpectedProductVersion = "0.1.0-chunk6a-preview.109",
                GamePresent = true,
                CurrentGameMode = "None",
                RelationshipState = "Unmounted"
            };
            var unobservedErrors = ModLoadSmokePolicy.Validate(nothing);
            TestRunner.True(unobservedErrors.Count >= 19,
                "A wholly unobserved load produced only " + unobservedErrors.Count + " refusals.");
        }

        private static void RefusesInexactIdentity()
        {
            AssertRefused("a foreign mod id", "instead of the exact",
                o => o.LoadedModId = "SomeOtherMod");
            AssertRefused("a missing mod id", "instead of the exact",
                o => o.LoadedModId = null);
            AssertRefused("a mismatched product version", "instead of the exact",
                o => o.ProductVersion = "0.1.0-chunk6a-preview.108");
            AssertRefused("a missing game singleton", "no game singleton",
                o => o.GamePresent = false);
            AssertRefused("a blank game mode", "no current game mode",
                o => o.CurrentGameMode = "   ");
            AssertRefused("a mutated diagnostic setting", "must only observe",
                o => o.SettingsMutatedByScenario = true);
        }

        private static void PublishedFieldsMatchObservation()
        {
            // The list the validator mirrors must describe real observation properties, so
            // a field cannot be advertised in the evidence contract and never populated.
            var properties = typeof(ModLoadSmokeObservation).GetProperties()
                .Select(item => char.ToLowerInvariant(item.Name[0]) + item.Name.Substring(1))
                .ToList();
            foreach (var field in ModLoadSmokePolicy.PublishedFields)
            {
                TestRunner.True(properties.Contains(field),
                    "The published field set advertises an observation that does not exist: " + field);
            }
            TestRunner.True(ModLoadSmokePolicy.PublishedFields.Length ==
                    ModLoadSmokePolicy.PublishedFields.Distinct().Count(),
                "The published field set repeats a field.");
            TestRunner.Equal("mod-load-smoke", ModLoadSmokePolicy.ScenarioId,
                "The mod-load smoke scenario id changed.");
        }

        private static void AssertRefused(string what, string expectedFragment,
            Action<ModLoadSmokeObservation> mutate)
        {
            var observation = CleanObservation();
            mutate(observation);
            var errors = ModLoadSmokePolicy.Validate(observation);
            TestRunner.True(errors.Count > 0, "The smoke accepted " + what + ".");
            TestRunner.True(errors.Any(error => error.IndexOf(expectedFragment, StringComparison.Ordinal) >= 0),
                "The smoke refused " + what + " without naming it (expected '" + expectedFragment +
                "'): " + string.Join(" ", errors));
        }
    }
}
