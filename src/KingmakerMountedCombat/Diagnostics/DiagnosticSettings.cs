namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class DiagnosticSettings
    {
        public DiagnosticSettings()
        {
            EnableUnsafeMovementExperiment = true;
            EnableDiagnosticOverlay = false;
            MaximumAnchorResidualWorldUnits = 0.10d;
            TelemetryIntervalSeconds = 0.10d;
            RiderOffsetX = 0f;
            RiderOffsetY = 0f;
            RiderOffsetZ = 0f;
            RiderYawDegrees = 0f;
        }

        public bool EnableUnsafeMovementExperiment { get; set; }

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
