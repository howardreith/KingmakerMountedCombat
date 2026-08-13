namespace KingmakerMountedCombat.Diagnostics
{
    internal enum WorkingFixtureLoadWatchdogState
    {
        Pending,
        CallbackCompleted,
        PipelineStoppedBeforeCallback
    }

    /// <summary>
    /// Tracks the only asynchronous ambiguity left after the native main-menu load
    /// path is entered: a failed Kingmaker loading process clears its own queue and
    /// therefore cannot deliver SaveManager's callback.
    /// </summary>
    internal sealed class WorkingFixtureLoadWatchdog
    {
        private bool observedActivePipeline;

        public WorkingFixtureLoadWatchdogState State { get; private set; } = WorkingFixtureLoadWatchdogState.Pending;

        public bool ObservedActivePipeline => observedActivePipeline;

        public WorkingFixtureLoadWatchdogState Observe(bool pipelineActive, bool callbackCompleted)
        {
            if (State != WorkingFixtureLoadWatchdogState.Pending)
            {
                return State;
            }

            if (callbackCompleted)
            {
                State = WorkingFixtureLoadWatchdogState.CallbackCompleted;
                return State;
            }

            if (pipelineActive)
            {
                observedActivePipeline = true;
                return State;
            }

            if (observedActivePipeline)
            {
                State = WorkingFixtureLoadWatchdogState.PipelineStoppedBeforeCallback;
            }

            return State;
        }
    }
}
