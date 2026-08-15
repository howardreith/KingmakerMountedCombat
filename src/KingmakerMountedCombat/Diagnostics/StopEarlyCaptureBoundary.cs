namespace KingmakerMountedCombat.Diagnostics
{
    internal enum StopEarlyCaptureDecision
    {
        None,
        CaptureAndWait,
        Wait,
        Stop
    }

    internal sealed class StopEarlyCaptureBoundary
    {
        private bool boundaryObserved;

        public StopEarlyCaptureDecision Observe(bool thresholdReached, bool movingCaptureTaken)
        {
            if (!thresholdReached)
            {
                return StopEarlyCaptureDecision.None;
            }
            if (!boundaryObserved)
            {
                boundaryObserved = true;
                return movingCaptureTaken
                    ? StopEarlyCaptureDecision.Wait
                    : StopEarlyCaptureDecision.CaptureAndWait;
            }
            return StopEarlyCaptureDecision.Stop;
        }

        public void Reset()
        {
            boundaryObserved = false;
        }
    }
}
