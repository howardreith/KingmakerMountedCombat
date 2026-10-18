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
            runner.Run("ready mount progresses when rider recovers before its native routine ends", ReadyActorsProgress);
            runner.Run("repeated same-target input preserves the waiting native actor", RepeatedInputPreservesPreference);
            runner.Run("actor preference cannot create native readiness or overlapping commands", PreferenceRequiresReadiness);
            runner.Run("RT actor preference preserves native ranged reach and TB order", PreferencePreservesModeAndRange);
            runner.Run("Stop and retarget discard prior actor preference", ReplacementClearsPreference);
            runner.Run("Stop ground retarget and explicit controls invalidate old dispatch generations", ReplacementPrecedence);
            runner.Run("hostile initiation survives precombat wait but observes later combat end", CombatStartBoundary);
            runner.Run("expected target death waits for released native child completion", ReleasedAttackTargetDeath);
            runner.Run("unconscious despawn and hostility changes cancel without inventing success", ExpectedTargetInvalidation);
            runner.Run("animated saddle follows absolute displacement without drift or mechanics writes", AnimatedSaddleFollows);
            runner.Run("animated saddle restores its lease and recalibrates for replacement views", AnimatedSaddleCleanup);
            runner.Run("mounted stock attack requires an exact frame-bounded native request", RequiresExactNativeRequest);
            runner.Run("a refused AI proposal neither erases nor borrows a pending player click", ProposalCannotEraseOrBorrowClick);
            runner.Run("a click whose own Run carried another command leaves nothing to borrow", ClickBoundToItsOwnRun);
            runner.Run("an unbound click observation expires at the next frame's Run", StaleObservationExpires);
            runner.Run("two pairs keep separate click ownership across selection changes", ClickOwnershipIsPairLocal);
            runner.Run("hidden-mount feedback belongs only to a real mount click", HiddenMountFeedbackOnlyForClicks);
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

        private static void ReadyActorsProgress()
        {
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(new object(), null, false, true);
            // CM native trace: both budgets were ready when the rider's full
            // routine finished; the Horse was idle, eligible and in reach.
            for (var routine = 0; routine < 6; routine++)
            {
                var next = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false,
                    true, true, false, true, preferMount: intent.PreferMount);
                var expected = routine % 2 == 0 ? MountedStockAttackDecision.DispatchRider : MountedStockAttackDecision.DispatchMount;
                TestRunner.Equal(expected, next, "A ready native actor was indefinitely passed over.");
                intent.ObserveDispatch(next == MountedStockAttackDecision.DispatchMount);
            }
        }

        private static void RepeatedInputPreservesPreference()
        {
            var target = new object();
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(target, null, false, true);
            var generation = intent.Generation;
            intent.ObserveDispatch(false);
            for (var click = 0; click < 100; click++)
            {
                TestRunner.True(intent.CanContinue(target, null, false), "Same-target input replaced its intent.");
                TestRunner.True(intent.Owns(target, generation), "Repeated click changed generation.");
            }
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false,
                    true, true, false, true, preferMount: intent.PreferMount), "Repeated input lost the waiting Horse.");
        }

        private static void PreferenceRequiresReadiness()
        {
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, false, false, true, preferMount: true),
                "Preference dispatched a mount without its native action.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, true, false, true, mountIsLegalActor: false, preferMount: true),
                "Preference dispatched an ineligible actor.");
            TestRunner.Equal(MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, true, false, true, true, false, true, preferMount: true),
                "Preference overlapped an active native command.");
            TestRunner.Equal(MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, false, false, true, preferMount: true),
                "Preference manufactured a native action.");
        }

        private static void PreferencePreservesModeAndRange()
        {
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, true, true, false, true, preferMount: true),
                "RT preference changed TB rider ordering.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, true, true, false, preferMount: true),
                "Ranged intent pulled the Horse into melee.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, true, true, true, preferMount: true),
                "Ranged intent passed over a ready Horse already in native melee reach.");
        }

        private static void ReplacementClearsPreference()
        {
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(new object(), null, false, true);
            intent.ObserveDispatch(false);
            intent.Begin(new object(), null, false, true);
            TestRunner.True(!intent.PreferMount, "A retarget inherited the old actor preference.");
            intent.ObserveDispatch(false);
            intent.Cancel();
            intent.ObserveDispatch(false);
            TestRunner.True(!intent.PreferMount, "Stop retained or recreated the old actor preference.");
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
            // Measured preview.3 final-render pelvis/chest centerline. The rider's
            // anatomical right has positive mount-root X, independently of heading.
            var centered = new AnimatedSaddlePosition();
            centered.Acquire(new PoseVector3(0f, 1.5024f, 0.4674f),
                new PoseVector3(-0.1892f, 2.3625f, 0.4849f), MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
            TestRunner.True(Math.Abs(centered.Project(new PoseVector3(0f, 1.5024f, 0.4674f)).X) < 0.002f,
                "Measured Horse pelvis remains left of the anatomical centerline.");
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

        // Mirrors MountedCombatController.TryRouteMountedStockAttack: exact admission requires the bound
        // player command plus the principal, target and frame; only the bound command consumes it.
        private static bool Route(ObservedPlayerAttackRequest<object, object> request, object principal, object target,
            object command, int frame)
        {
            var admitted = request.IsPlayerCommand(command) && UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                true, true, true, true, ReferenceEquals(request.Unit, principal), ReferenceEquals(request.Target, target), frame, request.Frame);
            request.Consume(command);
            return admitted;
        }

        // Native ClickUnitHandler raises OnAttackRequested(unit, target) and then runs that unit's command
        // as adjacent synchronous statements; the prefix binds before routing.
        private static bool Click(ObservedPlayerAttackRequest<object, object> request, object rider, object target, object command, int frame)
        {
            request.Observe(rider, target, frame);
            request.Bind(rider, command, frame);
            return Route(request, rider, target, command, frame);
        }

        private static void ProposalCannotEraseOrBorrowClick()
        {
            var request = new ObservedPlayerAttackRequest<object, object>();
            var rider = new object(); var target = new object(); var other = new object();
            var click = new object();
            request.Observe(rider, target, 100);
            request.Bind(rider, click, 100);
            // A refused proposal reaching the same controller on a mismatching target, then on the same
            // actor and target: neither is admitted and neither erases the pending click.
            var mismatching = new object(); var sameTarget = new object();
            request.Bind(rider, mismatching, 100);
            TestRunner.True(!Route(request, rider, other, mismatching, 100), "A mismatching proposal was admitted.");
            TestRunner.True(!Route(request, rider, target, sameTarget, 100), "A same-target proposal borrowed the click.");
            TestRunner.True(ReferenceEquals(request.Command, click), "A refused proposal erased the pending click.");
            TestRunner.True(Route(request, rider, target, click, 100), "The click's own command was refused.");
            TestRunner.True(!Route(request, rider, target, click, 100), "One click was admitted twice.");
            TestRunner.True(!Route(request, rider, target, sameTarget, 101), "A later proposal borrowed a consumed click.");
        }

        private static void ClickBoundToItsOwnRun()
        {
            var request = new ObservedPlayerAttackRequest<object, object>();
            var rider = new object(); var target = new object();
            var autoUse = new object();
            request.Observe(rider, target, 200);
            request.Bind(rider, autoUse, 200);
            var proposal = new object();
            request.Bind(rider, proposal, 200);
            TestRunner.True(!Route(request, rider, target, proposal, 200), "An AI attack borrowed a click whose Run carried auto-use.");
            TestRunner.True(!Route(request, rider, target, proposal, 201), "An AI attack borrowed the click one frame later.");
            TestRunner.True(ReferenceEquals(request.Command, autoUse), "The click lost its own bound command.");
        }

        private static void StaleObservationExpires()
        {
            var request = new ObservedPlayerAttackRequest<object, object>();
            var rider = new object(); var target = new object();
            request.Observe(rider, target, 300);
            var proposal = new object();
            request.Bind(rider, proposal, 301);
            TestRunner.True(request.Unit == null && request.Command == null, "An unbound observation survived into the next frame.");
            TestRunner.True(!Route(request, rider, target, proposal, 301), "A next-frame proposal borrowed an unbound click.");
        }

        private static void ClickOwnershipIsPairLocal()
        {
            var a = new ObservedPlayerAttackRequest<object, object>(); var b = new ObservedPlayerAttackRequest<object, object>();
            var riderA = new object(); var riderB = new object(); var target = new object();
            var clickA = new object(); var clickB = new object(); var proposalB = new object();
            // Both riders selected: the native loop observes and runs each in turn.
            a.Observe(riderA, target, 400); a.Bind(riderA, clickA, 400);
            b.Observe(riderB, target, 400); b.Bind(riderB, clickB, 400);
            TestRunner.True(!Route(b, riderB, target, proposalB, 400), "Pair B's proposal was admitted.");
            TestRunner.True(Route(a, riderA, target, clickA, 400), "Pair A's click was refused after pair B's proposal.");
            TestRunner.True(Route(b, riderB, target, clickB, 400), "Pair B's click was refused.");
            // Selection then changes to rider B only: a fresh click on B admits; A holds nothing.
            TestRunner.True(Click(b, riderB, target, new object(), 401), "A click after a selection change was refused.");
            TestRunner.True(a.Unit == null, "Pair A retained a click after its command was consumed.");
            // Repeated ordinary player attacks each carry their own command.
            TestRunner.True(Click(a, riderA, target, new object(), 402) && Click(a, riderA, target, new object(), 403),
                "Repeated player attacks were not each admitted.");
        }

        private static void HiddenMountFeedbackOnlyForClicks()
        {
            var request = new ObservedPlayerAttackRequest<object, object>();
            var mount = new object(); var target = new object();
            var proposal = new object();
            TestRunner.True(!request.Consume(proposal), "A brain proposal on the hidden mount earned player feedback.");
            var click = new object();
            request.Observe(mount, target, 500); request.Bind(mount, click, 500);
            TestRunner.True(!request.Consume(proposal), "A brain proposal consumed a real mount click.");
            TestRunner.True(request.Consume(click), "A real mount click did not earn its feedback.");
            TestRunner.True(!request.Consume(click), "One mount click earned feedback twice.");
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
