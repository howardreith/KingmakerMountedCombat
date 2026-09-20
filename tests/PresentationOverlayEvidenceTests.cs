using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class PresentationOverlayEvidenceTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("presentation overlay snapshot qualifies exact mounted repaint", QualifiesExactMountedRepaint);
            runner.Run("presentation overlay snapshot survives later dismounted state", SnapshotSurvivesLaterDismountedState);
        }

        private static void QualifiesExactMountedRepaint()
        {
            var evidence = MountedEvidence();

            TestRunner.True(evidence.IsQualifiedDismountOverlay,
                "Exact mounted Dismount overlay repaint was not qualified.");
            TestRunner.Equal("Dismount", evidence.Label, "Mounted overlay label changed.");
            TestRunner.Equal(true, evidence.Enabled, "Mounted overlay was not enabled.");
            TestRunner.Equal(23L, evidence.RepaintCountBefore, "Pre-observation repaint count changed.");
            TestRunner.Equal(229L, evidence.RepaintCountAfter, "Post-observation repaint count changed.");
        }

        private static void SnapshotSurvivesLaterDismountedState()
        {
            var mountedSnapshot = MountedEvidence();
            var laterDismountedState = new PresentationOverlayEvidence(
                23L,
                240L,
                true,
                true,
                false,
                "Mount",
                180f,
                28f,
                0L);

            TestRunner.True(mountedSnapshot.IsQualifiedDismountOverlay,
                "Captured mounted evidence was mutated by a later state.");
            TestRunner.Equal("Dismount", mountedSnapshot.Label,
                "Captured mounted label was replaced by the later dismounted label.");
            TestRunner.Equal(false, laterDismountedState.IsQualifiedDismountOverlay,
                "Disabled post-cleanup Mount state qualified as mounted evidence.");
        }

        private static PresentationOverlayEvidence MountedEvidence()
        {
            return new PresentationOverlayEvidence(
                23L,
                229L,
                true,
                true,
                true,
                "Dismount",
                180f,
                28f,
                0L);
        }
    }
}
