using System;

namespace KingmakerMountedCombat.Domain
{
    internal enum HorseCompanionDeadlineKind
    {
        None,
        Scenario,
        Lifecycle
    }

    /// <summary>
    /// Keeps the existing aggregate creation/movement/combat budget independent
    /// from the short post-combat death, recovery, and respec verification.
    /// </summary>
    internal static class HorseCompanionScenarioDeadlinePolicy
    {
        internal static HorseCompanionDeadlineKind Evaluate(
            double scenarioElapsedSeconds,
            double scenarioTimeoutSeconds,
            bool lifecyclePhaseActive,
            double lifecycleStartedAtSeconds,
            double lifecycleTimeoutSeconds)
        {
            if (lifecyclePhaseActive)
            {
                if (!IsFiniteNonNegative(scenarioElapsedSeconds) ||
                    !IsFiniteNonNegative(lifecycleStartedAtSeconds) ||
                    !IsFinitePositive(lifecycleTimeoutSeconds) ||
                    lifecycleStartedAtSeconds > scenarioElapsedSeconds)
                {
                    return HorseCompanionDeadlineKind.Lifecycle;
                }

                return scenarioElapsedSeconds - lifecycleStartedAtSeconds > lifecycleTimeoutSeconds
                    ? HorseCompanionDeadlineKind.Lifecycle
                    : HorseCompanionDeadlineKind.None;
            }

            if (!IsFiniteNonNegative(scenarioElapsedSeconds) ||
                !IsFinitePositive(scenarioTimeoutSeconds))
            {
                return HorseCompanionDeadlineKind.Scenario;
            }

            return scenarioElapsedSeconds > scenarioTimeoutSeconds
                ? HorseCompanionDeadlineKind.Scenario
                : HorseCompanionDeadlineKind.None;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0;
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }
    }
}
