using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedStockAttackPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("same living target continues approach windup cooldown and released attack generation", SameTargetContinuation);
            runner.Run("ordinary Pause admits queued orders without admitting modal UI", PausedOrderAdmission);
            runner.Run("native round movement prepayment survives preparation once in both turn orders", MovementPrepayment);
            runner.Run("fallback defaults admit ordinary real-time melee and ranged input", FallbackRealTimeAdmission);
            runner.Run("separate turns admit only the native current actor", SeparateTurnAdmission);
            runner.Run("ordinary party selection preserves unrelated units and includes the exact principal", PartySelection);
            runner.Run("ordinary separate TB dispatch stays actor local", SeparateTurnDispatch);
            runner.Run("ordinary RT cooldown waits retain the same intent for both weapon modes", RepeatsAfterCooldown);
            runner.Run("Stop ground retarget and explicit controls invalidate old dispatch generations", ReplacementPrecedence);
            runner.Run("hostile initiation survives precombat wait but observes later combat end", CombatStartBoundary);
            runner.Run("expected target death waits for released native child completion", ReleasedAttackTargetDeath);
            runner.Run("unconscious despawn and hostility changes cancel without inventing success", ExpectedTargetInvalidation);
            runner.Run("animated saddle follows absolute displacement without drift or mechanics writes", AnimatedSaddleFollows);
            runner.Run("animated saddle restores its lease and recalibrates for replacement views", AnimatedSaddleCleanup);
            runner.Run("mounted stock attack requires an exact frame-bounded native request", RequiresExactNativeRequest);
            runner.Run("mounted stock attack sequences rider before mount", SequencesRiderBeforeMount);
            runner.Run("mounted ranged stock attack never pulls mount into melee", RangedDoesNotForceMountMelee);
            runner.Run("mounted stock attack waits persistently only in real time", WaitsPersistentlyOnlyInRealTime);
            runner.Run("mounted stock attack cancels invalid target exactly", CancelsInvalidTarget);
        }

        private static void MovementPrepayment()
        {
            var mount = new object(); var controller = new object();
            foreach (var riderFirst in new[] { true, false })
            {
                var ledger = new MountedMovePrepayment();
                for (var round = 1; round <= 2; round++)
                {
                    ledger.ObserveEpoch(mount, controller, round, round * 600L);
                    if (!riderFirst) { TestRunner.Equal(0f, ledger.ReconcileNativePreparation(0f), "Mount-first native allowance changed."); }
                    ledger.RecordPhysicalMove(0f, 0.5f);
                    // Stop, reselection and remount of the same companion are not an epoch.
                    ledger.ObserveEpoch(mount, controller, round, round * 600L);
                    ledger.RecordPhysicalMove(0.5f, 1.25f);
                    var move = riderFirst ? ledger.ReconcileNativePreparation(0f) : 1.25f;
                    TestRunner.Equal(1.25f, move, "Native preparation replenished prepaid physical movement.");
                    TestRunner.Equal(move, ledger.ReconcileNativePreparation(move), "Repeated observation double charged.");
                }
            }
            var alreadyCharged = new MountedMovePrepayment();
            alreadyCharged.ObserveEpoch(mount, controller, 1, 600L);
            alreadyCharged.RecordPhysicalMove(0, 1);
            TestRunner.Equal(3f, alreadyCharged.ReconcileNativePreparation(3f), "Native existing cost double charged.");
        }

        private static void SameTargetContinuation()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            intent.Begin(target, turn, false, true);
            var generation = intent.Generation;
            foreach (var phase in new[] { "approach", "windup", "cooldown", "released" })
            {
                TestRunner.True(intent.CanContinue(target, turn, false), "Same target interrupted " + phase);
                TestRunner.True(intent.Owns(target, generation), "Continuation changed generation.");
            }
            TestRunner.True(!intent.CanContinue(new object(), turn, false), "Retarget swallowed.");
            TestRunner.True(!intent.CanContinue(target, new object(), false), "Wrong native turn retained.");
            TestRunner.True(!intent.CanContinue(target, turn, true), "Wrong actor retained.");
            intent.Cancel();
            TestRunner.True(!intent.CanContinue(target, turn, false), "Stop retained continuation.");
            var rider = new object(); var mount = new object(); var bow = new object(); var bite = new object();
            intent.Begin(target, null, false, true, rider, mount, bow, bite, 2);
            TestRunner.True(intent.CanContinue(target, null, false, rider, mount, bow, bite, 2), "Exact RT bow context rejected.");
            TestRunner.True(!intent.CanContinue(target, null, false, rider, mount, new object(), bite, 2), "Weapon change swallowed.");
            TestRunner.True(!intent.CanContinue(target, null, false, rider, new object(), bow, bite, 2), "Pair replacement swallowed.");
            TestRunner.True(!intent.CanContinue(target, null, false, rider, mount, bow, bite, 1), "Action mode change swallowed.");
        }

        private static void PausedOrderAdmission()
        {
            TestRunner.True(MountedGameModePolicy.CanQueueMountedAction("Pause"), "Ordinary pause blocked native queue.");
            foreach (var mode in new[] { "FullScreenUi", "EscMode", "Cutscene", "Loading", "None" })
                TestRunner.True(!MountedGameModePolicy.CanQueueMountedAction(mode), "Modal state admitted: " + mode);
            TestRunner.True(!MountedGameModePolicy.CanAdmitMountedAction("Pause"), "Pause advanced gameplay.");
        }

        private static void AnimatedSaddleFollows()
        {
            var saddle = new AnimatedSaddlePosition();
            saddle.Acquire(new PoseVector3(0, 1, 2), new PoseVector3(0, 2, 2), new PoseVector3(0, 0, -0.18f));
            // Root-relative samples: idle breathing, locomotion, rear, stop, return to idle.
            foreach (var sample in new[] { new PoseVector3(0, 1.02f, 2), new PoseVector3(0.1f, 1.2f, 1.8f),
                new PoseVector3(0, 2.2f, 1.2f), new PoseVector3(0, 1, 2) })
            {
                var projected = saddle.Project(sample);
                TestRunner.True(Math.Abs(projected.X - sample.X) < 0.00001f &&
                    Math.Abs(projected.Y - sample.Y - 1f) < 0.00001f &&
                    Math.Abs(projected.Z - sample.Z + 0.18f) < 0.00001f, "Seat-relative offset drifted.");
                var repeat = saddle.Project(sample);
                TestRunner.True((repeat - projected).Magnitude < 0.00001f, "Repeated phase accumulated translation.");
            }
        }

        private static void AnimatedSaddleCleanup()
        {
            var saddle = new AnimatedSaddlePosition();
            saddle.Acquire(new PoseVector3(0, 1, 0), new PoseVector3(0, 2, 0), new PoseVector3(0, 0, -0.18f));
            saddle.Release(); saddle.Release();
            TestRunner.True(!saddle.IsAcquired, "Saddle lease survived teardown.");
            saddle.Acquire(new PoseVector3(4, 5, 6), new PoseVector3(4, 7, 6), new PoseVector3(0, 0, 0));
            TestRunner.True(Math.Abs(saddle.Project(new PoseVector3(4, 5, 6)).Y - 7) < 0.00001f, "Replacement inherited old calibration.");
        }

        private static void PartySelection()
        {
            var rider = new object(); var mount = new object(); var ally = new object();
            var selected = new[] { ally, rider, mount };
            TestRunner.True(UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, rider), "Group rejected rider.");
            TestRunner.True(UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(new[] { mount }, mount), "Mount turn selection rejected.");
            TestRunner.True(!UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(new[] { ally }, rider), "Unrelated selection adopted.");
            TestRunner.True(ReferenceEquals(selected[0], ally) && ReferenceEquals(selected[1], rider) &&
                ReferenceEquals(selected[2], mount), "Selection mutated.");
        }

        private static void SeparateTurnDispatch()
        {
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, false, true, false, true, true, false),
                "Spent rider appended off-turn mount attack.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, true, true, false, true, false, true),
                "Mount turn spent rider action.");
        }

        private static void RepeatsAfterCooldown()
        {
            foreach (var ranged in new[] { false, true })
            {
                var intent = new MountedAttackIntent<object, object>(); var target = new object();
                intent.Begin(target, null, false, true); var generation = intent.Generation;
                TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, false, ranged, false), "First attack.");
                TestRunner.Equal(MountedStockAttackDecision.Wait,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, false, ranged, false), "Cooldown discarded intent.");
                TestRunner.True(intent.Owns(target, generation), "Intent changed while waiting.");
                TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, false, ranged, false), "Surviving target did not repeat.");
            }
        }

        private static void ReplacementPrecedence()
        {
            foreach (var reason in new[] { "Stop", "Hold", "ground", "retarget", "Primary", "lifecycle" })
            {
                var intent = new MountedAttackIntent<object, object>(); var old = new object(); var next = new object();
                intent.Begin(old, null, false, true); var oldGeneration = intent.Generation;
                intent.Cancel(); intent.Cancel();
                TestRunner.True(!intent.Owns(old, oldGeneration), reason + " retained dispatch ownership.");
                intent.Begin(next, null, false, true);
                TestRunner.True(!intent.Owns(old, oldGeneration) && intent.Owns(next, intent.Generation), reason + " resurrected old target.");
                intent.Begin(old, null, false, true);
                TestRunner.True(!intent.Owns(old, oldGeneration), "Returning to same target reused canceled generation.");
            }
        }

        private static void CombatStartBoundary()
        {
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(new object(), null, false, false);
            TestRunner.True(!intent.ObserveCombatEnded(false), "Initial out-of-combat click canceled.");
            TestRunner.True(!intent.ObserveCombatEnded(true), "Combat entry canceled.");
            TestRunner.True(intent.ObserveCombatEnded(false), "Combat end retained stale intent.");
        }

        private static void ReleasedAttackTargetDeath()
        {
            TestRunner.Equal(MountedTargetTerminationDecision.ContinueNativeLifecycle,
                MountedTargetTerminationPolicy.Decide(false, true, true, true, false), "Death interrupted a released child before native outcome.");
            TestRunner.Equal(MountedTargetTerminationDecision.ObserveNativeTerminal,
                MountedTargetTerminationPolicy.Decide(false, true, true, true, true), "Native terminal was replaced with target exception.");
        }

        private static void ExpectedTargetInvalidation()
        {
            TestRunner.Equal(MountedTargetTerminationDecision.CancelExpectedInvalidation,
                MountedTargetTerminationPolicy.Decide(false, true, true, false, false), "Unreleased attack survived invalidation.");
            TestRunner.Equal(MountedTargetTerminationDecision.CancelExpectedInvalidation,
                MountedTargetTerminationPolicy.Decide(false, false, true, true, false), "Despawn was treated as success.");
            TestRunner.Equal(MountedTargetTerminationDecision.CancelExpectedInvalidation,
                MountedTargetTerminationPolicy.Decide(false, true, false, true, false), "Changed hostility retained intent.");
        }

        private static void FallbackRealTimeAdmission()
        {
            var settings = new KingmakerMountedCombat.Diagnostics.DiagnosticSettings();
            TestRunner.True(!settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler,
                "Shipped experimental defaults changed.");
            foreach (var ranged in new[] { false, true })
            {
                TestRunner.True(UnifiedMountedStockAttackPolicy.AllowsOrdinaryInput(
                    false, settings.EnableUnifiedMountedTurn, false),
                    "Fallback rejected native RT " + (ranged ? "ranged" : "melee") + " request.");
            }
        }

        private static void SeparateTurnAdmission()
        {
            TestRunner.True(UnifiedMountedStockAttackPolicy.AllowsOrdinaryInput(true, false, true),
                "Native current actor rejected with separate turns.");
            TestRunner.True(!UnifiedMountedStockAttackPolicy.AllowsOrdinaryInput(true, false, false),
                "Off-turn actor admitted with separate turns.");
        }

        private static void RequiresExactNativeRequest()
        {
            TestRunner.True(
                UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, true, true, true, true, 100, 99),
                "Exact native hostile-click request was not admitted.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, true, true, true, true, 102, 99),
                "Stale native request was admitted.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, false, true, true, true, 100, 100),
                "Non-principal selection was admitted.");
        }

        private static void SequencesRiderBeforeMount()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, true, true, false, true),
                "Shared attack did not dispatch rider first.");
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, false, true, false, true),
                "Shared attack did not dispatch available mount second.");
        }

        private static void RangedDoesNotForceMountMelee()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, true, true, false),
                "Ranged intent forced a ready mount to approach melee.");
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, true, true, true),
                "Ranged intent rejected a mount already in legal melee.");
        }

        private static void WaitsPersistentlyOnlyInRealTime()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, false, false, true),
                "RT intent did not persist across native cooldown.");
            TestRunner.Equal(
                MountedStockAttackDecision.CompleteTurnBasedIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, false, false, false, true),
                "Exhausted TB intent persisted past its shared turn.");
        }

        private static void CancelsInvalidTarget()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.CancelInvalidIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, false, false, false, true, true, false, true),
                "Invalid target retained attack intent.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsValidTarget(true, true, false, true, true),
                "Incapacitated target remained valid.");
        }
    }
}
