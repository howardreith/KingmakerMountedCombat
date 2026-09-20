using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class UnifiedMountedTurnPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("unified turn skips only the exact active mount", SkipsOnlyExactActiveMount);
            runner.Run("unified turn defers an exact mount skip until current turn clears", DefersSkipUntilCurrentTurnClears);
            runner.Run("unified turn defers split through the current round", DefersSplitThroughCurrentRound);
            runner.Run("unified turn remains open for an actionable mount", KeepsTurnOpenForMount);
            runner.Run("unified turn admits only exact owned mount commands", AdmitsOnlyExactMountCommand);
            runner.Run("unified turn mirrors only prepared rider initiative", MirrorsPreparedRiderInitiative);
            runner.Run("unified turn prepares mount ledger once per rider turn", PreparesMountLedgerOnce);
            runner.Run("unified turn admits mount action from rider turn and selection", AdmitsMountActionFromRiderPrincipal);
            runner.Run("mounted five-foot step suppression is pair local", SuppressesOnlyExactMountedStep);
            runner.Run("mount movement cooldown transfer preserves rider and caps mount", TransfersMovementCooldown);
            runner.Run("mounted five-foot step translates rider ledger distance to mount speed", TranslatesFiveFootDistance);
        }

        private static void SkipsOnlyExactActiveMount()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(true, true, true, true, false, 2, -1),
                "Exact active mount was not skipped.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(true, true, true, false, false, 2, -1),
                "An unrelated combatant was skipped.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(false, true, true, true, false, 2, -1),
                "Fallback separate-turn mode skipped the mount.");
        }

        private static void DefersSkipUntilCurrentTurnClears()
        {
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldAdvancePastSkippedCandidate(true, false),
                "Exact mount skip advanced while the ended rider turn was still bound.");
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldAdvancePastSkippedCandidate(true, true),
                "Exact mount skip did not advance after the ended rider turn cleared.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldAdvancePastSkippedCandidate(false, true),
                "An unrelated pending unit was advanced by the pair-local skip seam.");
        }

        private static void DefersSplitThroughCurrentRound()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(true, true, false, true, true, 4, 4),
                "Former mount was restored during the dismount round.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(true, true, false, true, true, 5, 4),
                "Former mount remained suppressed after the safe round boundary.");
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldRestoreSplitParticipation(true, true, 5, 4),
                "Pending split did not restore after the round advanced.");
        }

        private static void KeepsTurnOpenForMount()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldKeepRiderTurnOpen(
                    true, true, true, true, true, true, false, false, false),
                "Available mount Standard did not keep the rider-led turn open.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldKeepRiderTurnOpen(
                    true, true, true, true, true, false, false, false, false),
                "Exhausted pair retained an empty turn.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldKeepRiderTurnOpen(
                    true, true, false, true, true, true, true, true, true),
                "An unrelated turn was retained.");
        }

        private static void AdmitsOnlyExactMountCommand()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldAdmitMountCommand(
                    true, true, true, true, true, true, true),
                "Exact shared-turn mount command was rejected.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldAdmitMountCommand(
                    true, true, true, true, true, true, false),
                "Foreign mount command was admitted.");
        }

        private static void MirrorsPreparedRiderInitiative()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldMirrorInitiative(true, true, true, true),
                "Exact mount initiative was not mirrored.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldMirrorInitiative(true, true, false, true),
                "Unprepared rider initiative was used.");
        }

        private static void PreparesMountLedgerOnce()
        {
            TestRunner.True(
                UnifiedMountedTurnPolicy.ShouldPrepareMountLedger(true, true, true, true, false),
                "New rider turn did not prepare the mount ledger.");
            TestRunner.True(
                !UnifiedMountedTurnPolicy.ShouldPrepareMountLedger(true, true, true, true, true),
                "Mount ledger was prepared twice for one rider turn.");
        }

        private static void AdmitsMountActionFromRiderPrincipal()
        {
            TestRunner.True(
                MountedPairTurnPolicy.CanIssueSharedAction(
                    true, true, true, false, false, true),
                "Mount-owned action was not admitted during the rider-led shared turn.");
            TestRunner.True(
                MountedTurnSelectionPolicy.IsExpectedActionSelection(
                    true, true, true, true, false),
                "Rider principal selection was rejected for a shared-turn Mount primary.");
            TestRunner.Equal(
                MountedSelectionDisposition.ProjectMountToRider,
                MountedTurnSelectionPolicy.Classify(true, true, true, true, true),
                "Unified mode preserved a separately selected mount turn.");
        }

        private static void SuppressesOnlyExactMountedStep()
        {
            TestRunner.True(
                MountedFiveFootStepPolicy.ShouldSuppressDisengageOpportunity(
                    true, true, true, true, true, true, true, false, 1f, 2.286f),
                "Exact mounted step did not suppress its disengage AoO.");
            TestRunner.True(
                !MountedFiveFootStepPolicy.ShouldSuppressDisengageOpportunity(
                    true, true, true, true, true, true, false, false, 1f, 2.286f),
                "Ordinary movement received step immunity.");
            TestRunner.True(
                !MountedFiveFootStepPolicy.ShouldSuppressDisengageOpportunity(
                    true, true, true, true, false, true, true, false, 1f, 2.286f),
                "An unrelated target received mounted step immunity.");
        }

        private static void TransfersMovementCooldown()
        {
            TestRunner.Equal(
                2f,
                MountedFiveFootStepPolicy.TransferMoveCooldown(1f, 2f, 1f),
                "Mount movement delta was not transferred.");
            TestRunner.Equal(
                6f,
                MountedFiveFootStepPolicy.TransferMoveCooldown(0f, 8f, 0f),
                "Mount movement cooldown exceeded the native cap.");
            var rejected = false;
            try
            {
                MountedFiveFootStepPolicy.TransferMoveCooldown(float.NaN, 0f, 0f);
            }
            catch (ArgumentOutOfRangeException)
            {
                rejected = true;
            }
            TestRunner.True(rejected, "Nonfinite movement telemetry was accepted.");
        }

        private static void TranslatesFiveFootDistance()
        {
            var ledger = MountedFiveFootStepPolicy.ToRiderLedgerDelta(0.25f, 3f, 6f, true);
            TestRunner.Equal(0.5f, ledger, "Mount physical movement was not converted to the rider ledger's speed basis.");
            var physical = MountedFiveFootStepPolicy.ToMountPhysicalDelta(0.3f, 3f, 6f, true);
            TestRunner.Equal(0.15f, physical, "A capped rider ledger delta was not converted back to mount physical time.");
            TestRunner.Equal(
                0.25f,
                MountedFiveFootStepPolicy.ToRiderLedgerDelta(0.25f, 3f, 6f, false),
                "Ordinary movement was incorrectly speed-scaled.");
        }
    }
}
