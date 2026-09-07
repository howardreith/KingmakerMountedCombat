namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class DiagnosticSettings
    {
        public DiagnosticSettings()
        {
            EnableUnsafeMovementExperiment = true;
            EnableUnifiedMountedTurn = false;
            EnablePairedCommandScheduler = false;
            EnableDiagnosticOverlay = false;
            MaximumAnchorResidualWorldUnits = 0.10d;
            TelemetryIntervalSeconds = 0.10d;
            RiderOffsetX = 0f;
            RiderOffsetY = 0f;
            RiderOffsetZ = 0f;
            RiderYawDegrees = 0f;
        }

        public bool EnableUnsafeMovementExperiment { get; set; }

        public bool EnableUnifiedMountedTurn { get; set; }

        public bool EnablePairedCommandScheduler { get; set; }

        // Developer-only coherent lifecycle. The two retired experimental paths
        // are bypassed whenever this path is selected; all defaults remain false.
        public bool EnablePairedActivation { get; set; }

        internal bool UseLegacyUnifiedTurn => EnableUnifiedMountedTurn && !EnablePairedActivation;
        internal bool UsePairedTurnControls => EnablePairedActivation || EnableUnifiedMountedTurn;

        public bool EnableDiagnosticOverlay { get; set; }

        public double MaximumAnchorResidualWorldUnits { get; set; }

        public double TelemetryIntervalSeconds { get; set; }

        public float RiderOffsetX { get; set; }

        public float RiderOffsetY { get; set; }

        public float RiderOffsetZ { get; set; }

        public float RiderYawDegrees { get; set; }

        public string Validate()
        {
            if (MaximumAnchorResidualWorldUnits <= 0.0d || MaximumAnchorResidualWorldUnits > 0.10d)
            {
                return "MaximumAnchorResidualWorldUnits must be greater than zero and no greater than 0.10.";
            }

            if (TelemetryIntervalSeconds <= 0.0d || TelemetryIntervalSeconds > 1.0d)
            {
                return "TelemetryIntervalSeconds must be greater than zero and no greater than 1.0.";
            }

            return null;
        }
    }
}
