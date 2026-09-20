using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Retains an operation result while its availability projection is stable,
    /// but immediately replaces stale feedback when external relationship state
    /// changes the visible action or eligibility reasons.
    /// </summary>
    public sealed class MountedPlayerActionFeedbackState
    {
        private bool observed;
        private bool visible;
        private bool enabled;
        private MountedPlayerActionKind action;
        private string availabilityFeedback;

        public MountedPlayerActionFeedbackState(string initialFeedback)
        {
            LastFeedback = initialFeedback ?? string.Empty;
        }

        public string LastFeedback { get; private set; }

        public string ObserveAvailability(MountedPlayerActionAvailability availability)
        {
            if (availability == null)
            {
                throw new ArgumentNullException(nameof(availability));
            }

            var nextFeedback = availability.Feedback;
            if (!observed || visible != availability.IsVisible || enabled != availability.IsEnabled ||
                action != availability.Action || !string.Equals(availabilityFeedback, nextFeedback, StringComparison.Ordinal))
            {
                LastFeedback = nextFeedback;
            }

            observed = true;
            visible = availability.IsVisible;
            enabled = availability.IsEnabled;
            action = availability.Action;
            availabilityFeedback = nextFeedback;
            return LastFeedback;
        }

        public void SetOperationFeedback(string feedback)
        {
            LastFeedback = feedback ?? string.Empty;
        }
    }
}
