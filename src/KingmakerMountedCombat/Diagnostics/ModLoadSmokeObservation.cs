using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Everything the no-save mod-load smoke observes, and nothing it changes.
    ///
    /// The smoke proves one thing: this exact DLL loads cleanly at the main menu and
    /// nothing gameplay-bearing is live. It used to prove that by switching every
    /// experiment off for its own duration and then asserting they were off, which meant
    /// the run's own scope produced the result it reported. It now reads the live state
    /// and publishes it verbatim, including the defaults the product actually ships with,
    /// and asserts the absence of gameplay state directly.
    ///
    /// Every field is published in the schema-v1 evidence, so the validator, the emitted
    /// result and the bootstrap-failure result all describe the same observation. A
    /// nullable field means "not observed" and is lawful only on a bootstrap failure,
    /// where the composition root never finished building.
    /// </summary>
    public sealed class ModLoadSmokeObservation
    {
        // ---- exact identity ----
        public string LoadedModId { get; set; }

        public string ExpectedModId { get; set; }

        public string ProductVersion { get; set; }

        public string ExpectedProductVersion { get; set; }

        public bool GamePresent { get; set; }

        public bool LoadedAreaPresent { get; set; }

        public string CurrentGameMode { get; set; }

        // ---- the settings this build shipped with: observed, published, never written ----
        public bool? ShippedMovementExperimentEnabled { get; set; }

        public bool? ShippedPairedActivationEnabled { get; set; }

        public bool? ShippedUnifiedMountedTurnEnabled { get; set; }

        public bool? ShippedPairedCommandSchedulerEnabled { get; set; }

        public bool? ShippedDiagnosticOverlayEnabled { get; set; }

        /// <summary>
        /// False for every lawful smoke. The smoke asserts its own purity rather than
        /// leaving the reader to infer it from the absence of mutation code.
        /// </summary>
        public bool SettingsMutatedByScenario { get; set; }

        // ---- gameplay-bearing state, all of which must be absent ----
        public string RelationshipState { get; set; }

        public long SaveRequestCount { get; set; }

        public long LoadRequestCount { get; set; }

        public bool? RelationshipTransitionInFlight { get; set; }

        public long? RegisteredRelationshipShellCount { get; set; }

        public long? RelationshipProcessBindingCount { get; set; }

        public long? PoisonedExecutionContextCount { get; set; }

        public long? NativeCastRequestCount { get; set; }

        public long? DispatchAcceptedCount { get; set; }

        public long? DispatchRejectedCount { get; set; }

        public bool? PairedActivationPresent { get; set; }

        public bool? PairedPartnerContextPresent { get; set; }

        public long? ManagedControlFactCount { get; set; }

        public long? DuplicateControlFactCount { get; set; }

        public long? ManagedHotbarSlotCount { get; set; }

        public bool? OverlayObjectPresent { get; set; }

        public bool? ControlServiceSerializationSuspended { get; set; }
    }

    public static class ModLoadSmokePolicy
    {
        public const string ScenarioId = "mod-load-smoke";

        /// <summary>
        /// The exact property set the schema-v1 evidence adds for this scenario, in the
        /// camelCase spelling the JSON uses. The validator pins this same list, so a field
        /// cannot be added on one side and forgotten on the other.
        /// </summary>
        public static readonly string[] PublishedFields =
        {
            "expectedModId",
            "expectedProductVersion",
            "gamePresent",
            "shippedMovementExperimentEnabled",
            "shippedPairedActivationEnabled",
            "shippedUnifiedMountedTurnEnabled",
            "shippedPairedCommandSchedulerEnabled",
            "shippedDiagnosticOverlayEnabled",
            "settingsMutatedByScenario",
            "relationshipTransitionInFlight",
            "registeredRelationshipShellCount",
            "relationshipProcessBindingCount",
            "poisonedExecutionContextCount",
            "nativeCastRequestCount",
            "dispatchAcceptedCount",
            "dispatchRejectedCount",
            "pairedActivationPresent",
            "pairedPartnerContextPresent",
            "managedControlFactCount",
            "duplicateControlFactCount",
            "managedHotbarSlotCount",
            "overlayObjectPresent",
            "controlServiceSerializationSuspended"
        };

        /// <summary>
        /// Every reason this observation is not a clean load, in a stable order. An empty
        /// list is the only thing that may be reported as PASS.
        /// </summary>
        public static IReadOnlyList<string> Validate(ModLoadSmokeObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            var errors = new List<string>();

            // Exact identity. A smoke that cannot name the build it loaded proves nothing.
            if (string.IsNullOrEmpty(observation.LoadedModId) ||
                !string.Equals(observation.LoadedModId, observation.ExpectedModId, StringComparison.Ordinal))
            {
                errors.Add("Mod-load smoke observed the mod id '" + Describe(observation.LoadedModId) +
                    "' instead of the exact '" + Describe(observation.ExpectedModId) + "'.");
            }
            if (string.IsNullOrEmpty(observation.ProductVersion) ||
                !string.Equals(observation.ProductVersion, observation.ExpectedProductVersion, StringComparison.Ordinal))
            {
                errors.Add("Mod-load smoke observed the product version '" + Describe(observation.ProductVersion) +
                    "' instead of the exact '" + Describe(observation.ExpectedProductVersion) + "'.");
            }
            if (!observation.GamePresent)
            {
                errors.Add("Mod-load smoke observed no game singleton.");
            }
            if (string.IsNullOrWhiteSpace(observation.CurrentGameMode))
            {
                errors.Add("Mod-load smoke observed no current game mode.");
            }

            // The smoke is observational. It is a defect for it to have written a setting.
            if (observation.SettingsMutatedByScenario)
            {
                errors.Add("Mod-load smoke mutated a diagnostic setting; the smoke must only observe.");
            }

            // Every shipped setting must be recorded. Either value is lawful -- these are
            // published facts about the product, not gates -- but an unrecorded one would
            // let the evidence claim a default it never read.
            RequireObserved(errors, observation.ShippedMovementExperimentEnabled.HasValue,
                "the shipped mounted-movement experiment default");
            RequireObserved(errors, observation.ShippedPairedActivationEnabled.HasValue,
                "the shipped paired-activation default");
            RequireObserved(errors, observation.ShippedUnifiedMountedTurnEnabled.HasValue,
                "the shipped unified-mounted-turn default");
            RequireObserved(errors, observation.ShippedPairedCommandSchedulerEnabled.HasValue,
                "the shipped paired-command-scheduler default");
            RequireObserved(errors, observation.ShippedDiagnosticOverlayEnabled.HasValue,
                "the shipped diagnostic-overlay default");

            // No campaign, no persistence traffic.
            if (observation.LoadedAreaPresent)
            {
                errors.Add("Mod-load smoke observed a loaded campaign area.");
            }
            if (observation.SaveRequestCount != 0 || observation.LoadRequestCount != 0)
            {
                errors.Add("Mod-load smoke observed save/load traffic: saves=" +
                    observation.SaveRequestCount + " loads=" + observation.LoadRequestCount + ".");
            }

            // No mounted relationship, and nothing in flight.
            if (!string.Equals(observation.RelationshipState, "Unmounted", StringComparison.Ordinal))
            {
                errors.Add("Mod-load smoke observed the relationship state '" +
                    Describe(observation.RelationshipState) + "' instead of Unmounted.");
            }
            RequireAbsent(errors, observation.RelationshipTransitionInFlight, "a mounted transition in flight");
            RequireZero(errors, observation.RegisteredRelationshipShellCount, "registered mounted relationship shell");
            RequireZero(errors, observation.RelationshipProcessBindingCount, "mounted relationship process binding");
            RequireZero(errors, observation.PoisonedExecutionContextCount, "poisoned mounted execution context");
            RequireZero(errors, observation.NativeCastRequestCount, "native mounted cast request");
            RequireZero(errors, observation.DispatchAcceptedCount, "accepted mounted dispatch");
            RequireZero(errors, observation.DispatchRejectedCount, "rejected mounted dispatch");

            // No paired ownership of any kind.
            RequireAbsent(errors, observation.PairedActivationPresent, "a live paired activation");
            RequireAbsent(errors, observation.PairedPartnerContextPresent, "a private paired partner turn context");

            // No leased facts, no hotbar writes, no overlay object, no suspended writes.
            RequireZero(errors, observation.ManagedControlFactCount, "leased mounted control fact");
            RequireZero(errors, observation.DuplicateControlFactCount, "duplicate mounted control fact");
            RequireZero(errors, observation.ManagedHotbarSlotCount, "managed mounted hotbar slot");
            RequireAbsent(errors, observation.OverlayObjectPresent, "a diagnostic overlay object");
            RequireAbsent(errors, observation.ControlServiceSerializationSuspended,
                "a suspended mounted control serialization scope");

            return errors;
        }

        private static void RequireObserved(List<string> errors, bool observed, string what)
        {
            if (!observed)
            {
                errors.Add("Mod-load smoke did not record " + what + ".");
            }
        }

        private static void RequireAbsent(List<string> errors, bool? observed, string what)
        {
            if (!observed.HasValue)
            {
                errors.Add("Mod-load smoke did not observe whether " + what + " was present.");
                return;
            }
            if (observed.Value)
            {
                errors.Add("Mod-load smoke observed " + what + ".");
            }
        }

        private static void RequireZero(List<string> errors, long? observed, string what)
        {
            if (!observed.HasValue)
            {
                errors.Add("Mod-load smoke did not count the " + what + "s it observed.");
                return;
            }
            if (observed.Value != 0)
            {
                errors.Add("Mod-load smoke observed " + observed.Value + " " + what + "(s).");
            }
        }

        private static string Describe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value;
        }
    }
}
