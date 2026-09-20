using System;
using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Bounded, observation-only summary of the exact action actor's native real-time
    /// combat-cooldown ticks. It never changes a cooldown or game object.
    /// </summary>
    public sealed class DiagnosticCombatInitiativeObservation
    {
        private const float Tolerance = 0.000001f;
        private bool hasPreviousPostfix;
        private float previousPostfixInitiative;

        public int CallbackCount { get; private set; }

        public int DecreaseCount { get; private set; }

        public int IncreaseCount { get; private set; }

        public int UnchangedCount { get; private set; }

        public int CrossTickRewriteCount { get; private set; }

        public int PositiveGameDeltaCount { get; private set; }

        public int PreparedCount { get; private set; }

        public int InCombatCount { get; private set; }

        public int AwakeCount { get; private set; }

        public float FirstPrefixInitiative { get; private set; }

        public float LastPostfixInitiative { get; private set; }

        public double PositiveGameDeltaTotal { get; private set; }

        public double NativeDecreaseTotal { get; private set; }

        public void Reset()
        {
            CallbackCount = 0;
            DecreaseCount = 0;
            IncreaseCount = 0;
            UnchangedCount = 0;
            CrossTickRewriteCount = 0;
            PositiveGameDeltaCount = 0;
            PreparedCount = 0;
            InCombatCount = 0;
            AwakeCount = 0;
            FirstPrefixInitiative = 0f;
            LastPostfixInitiative = 0f;
            PositiveGameDeltaTotal = 0d;
            NativeDecreaseTotal = 0d;
            hasPreviousPostfix = false;
            previousPostfixInitiative = 0f;
        }

        public void Observe(
            float prefixInitiative,
            float postfixInitiative,
            float gameDeltaTime,
            bool prepared,
            bool inCombat,
            bool awake)
        {
            if (CallbackCount == 0)
            {
                FirstPrefixInitiative = prefixInitiative;
            }
            else if (hasPreviousPostfix &&
                IsFinite(prefixInitiative) && IsFinite(previousPostfixInitiative) &&
                Math.Abs(prefixInitiative - previousPostfixInitiative) > Tolerance)
            {
                CrossTickRewriteCount++;
            }

            CallbackCount++;
            LastPostfixInitiative = postfixInitiative;
            if (IsFinite(prefixInitiative) && IsFinite(postfixInitiative))
            {
                var change = postfixInitiative - prefixInitiative;
                if (change < -Tolerance)
                {
                    DecreaseCount++;
                    NativeDecreaseTotal += -change;
                }
                else if (change > Tolerance)
                {
                    IncreaseCount++;
                }
                else
                {
                    UnchangedCount++;
                }
            }

            if (IsFinite(gameDeltaTime) && gameDeltaTime > 0f)
            {
                PositiveGameDeltaCount++;
                PositiveGameDeltaTotal += gameDeltaTime;
            }
            if (prepared) { PreparedCount++; }
            if (inCombat) { InCombatCount++; }
            if (awake) { AwakeCount++; }
            previousPostfixInitiative = postfixInitiative;
            hasPreviousPostfix = true;
        }

        public string Describe()
        {
            return "callbacks=" + CallbackCount.ToString(CultureInfo.InvariantCulture) +
                ";decreases=" + DecreaseCount.ToString(CultureInfo.InvariantCulture) +
                ";increases=" + IncreaseCount.ToString(CultureInfo.InvariantCulture) +
                ";unchanged=" + UnchangedCount.ToString(CultureInfo.InvariantCulture) +
                ";crossTickRewrites=" + CrossTickRewriteCount.ToString(CultureInfo.InvariantCulture) +
                ";positiveGameDeltas=" + PositiveGameDeltaCount.ToString(CultureInfo.InvariantCulture) +
                ";positiveGameDeltaTotal=" + PositiveGameDeltaTotal.ToString("R", CultureInfo.InvariantCulture) +
                ";nativeDecreaseTotal=" + NativeDecreaseTotal.ToString("R", CultureInfo.InvariantCulture) +
                ";preparedCallbacks=" + PreparedCount.ToString(CultureInfo.InvariantCulture) +
                ";inCombatCallbacks=" + InCombatCount.ToString(CultureInfo.InvariantCulture) +
                ";awakeCallbacks=" + AwakeCount.ToString(CultureInfo.InvariantCulture) +
                ";firstPrefix=" + Format(FirstPrefixInitiative, CallbackCount != 0) +
                ";lastPostfix=" + Format(LastPostfixInitiative, CallbackCount != 0);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string Format(float value, bool observed)
        {
            return observed ? value.ToString("R", CultureInfo.InvariantCulture) : "not-observed";
        }
    }
}
