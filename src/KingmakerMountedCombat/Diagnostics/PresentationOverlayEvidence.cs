using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal readonly struct PresentationOverlayEvidence
    {
        public PresentationOverlayEvidence(
            long repaintCountBefore,
            long repaintCountAfter,
            bool present,
            bool visible,
            bool enabled,
            string label,
            float width,
            float height,
            long buttonActivationCount)
        {
            RepaintCountBefore = repaintCountBefore;
            RepaintCountAfter = repaintCountAfter;
            Present = present;
            Visible = visible;
            Enabled = enabled;
            Label = label;
            Width = width;
            Height = height;
            ButtonActivationCount = buttonActivationCount;
        }

        public long RepaintCountBefore { get; }

        public long RepaintCountAfter { get; }

        public bool Present { get; }

        public bool Visible { get; }

        public bool Enabled { get; }

        public string Label { get; }

        public float Width { get; }

        public float Height { get; }

        public long ButtonActivationCount { get; }

        public bool IsQualifiedDismountOverlay
        {
            get
            {
                return Present && RepaintCountAfter > RepaintCountBefore && Visible && Enabled &&
                    string.Equals(Label, "Dismount", StringComparison.Ordinal) &&
                    Width > 0f && Height > 0f && ButtonActivationCount == 0L;
            }
        }
    }
}
