using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Whether the exact rider owns the native Move a mounted transition needs right now,
    /// or whether Kingmaker's own clock is still restoring one it charged earlier.
    /// </summary>
    public enum MountedNativeMoveReadiness
    {
        /// <summary>Kingmaker reports the native Move available, so the request is lawful now.</summary>
        Available,

        /// <summary>
        /// The native Move is spent and Kingmaker's own real-time clock is still draining the
        /// cooldown it set. Waiting is the only lawful response: the engine restores the
        /// resource itself, and nothing may write, clear or refund it to arrive sooner.
        /// </summary>
        RestoringOnNativeClock,

        /// <summary>
        /// The native Move is spent and no restoration is pending: a turn-based rider has
        /// already spent its Move on this turn, or a real-time cooldown has drained and the
        /// resource is still refused. A request here is genuinely unlawful, so the exact
        /// refusal belongs in the evidence rather than being waited out.
        /// </summary>
        Unavailable
    }

    /// <summary>
    /// The one pure decision about waiting for a native Move.
    ///
    /// A refused delivery after native commitment keeps its cost, so the attempt that
    /// follows it can only become lawful when Kingmaker itself restores the rider's Move.
    /// The distinction this policy draws is between a resource the engine is actively
    /// restoring -- where waiting is the whole of the correct behaviour -- and one nothing
    /// will restore, where waiting would only convert an exact refusal into a deadline.
    /// </summary>
    public static class MountedNativeMoveReadinessPolicy
    {
        public static MountedNativeMoveReadiness Decide(
            bool hasMoveAction, float moveCooldownSeconds, bool turnBased)
        {
            if (hasMoveAction)
            {
                return MountedNativeMoveReadiness.Available;
            }
            // In turn-based play the cooldown is static between boundaries: a spent Move
            // stays spent until the rider's next turn, and forcing a turn boundary to get
            // one back is exactly what the mounted-cost contract forbids.
            if (turnBased)
            {
                return MountedNativeMoveReadiness.Unavailable;
            }
            // In real time a charge RAISES the cooldown and the engine drains it. A positive
            // remaining cooldown is therefore a restoration in progress; anything else --
            // zero, negative or not-a-number -- is a refusal with another cause.
            return moveCooldownSeconds > 0f
                ? MountedNativeMoveReadiness.RestoringOnNativeClock
                : MountedNativeMoveReadiness.Unavailable;
        }

        public static bool ShouldWait(MountedNativeMoveReadiness readiness) =>
            readiness == MountedNativeMoveReadiness.RestoringOnNativeClock;

        public static string Describe(
            MountedNativeMoveReadiness readiness, float moveCooldownSeconds, bool turnBased)
        {
            switch (readiness)
            {
                case MountedNativeMoveReadiness.Available:
                    return "The rider owns its native Move.";
                case MountedNativeMoveReadiness.RestoringOnNativeClock:
                    return "Kingmaker is still draining the native Move cooldown it charged: " +
                        moveCooldownSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) +
                        "s remain, and only the engine's own clock may restore it.";
                case MountedNativeMoveReadiness.Unavailable:
                    return turnBased
                        ? "The rider has already spent its native Move on this turn and no lawful boundary will return it."
                        : "The rider has no native Move available and no cooldown is draining, so another condition refuses it.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(readiness));
            }
        }
    }
}
