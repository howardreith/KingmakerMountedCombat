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
            runner.Run("MMR-01: the Horse seat does not depend on the rider's pose at attachment", AnimatedSaddleDeclaredHorseBasis);
            runner.Run("MMR-03: one ordinary paired order carries to the horse on its own lawful activation", PairOrderHorseFollowThrough);
            runner.Run("MMR-06: saved-Horse repair runs once per loaded object, only on a settled world, and retries a failed pass", SavedHorseRepairLedger);
            runner.Run("R1-F4/F6: follow-through uses dispatch admission and the accepted kind; newer work keeps the horse out", FollowThroughEligibility);
            runner.Run("R1-F13/R2-F4 (formatter only): the Horse strike line states handle and active-clip facts literally", HorseStrikeLines);
            runner.Run("mounted stock attack requires an exact frame-bounded native request", RequiresExactNativeRequest);
            runner.Run("a refused AI proposal neither erases nor borrows a pending player click", ProposalCannotEraseOrBorrowClick);
            runner.Run("restored rider Standard debt routes the stock click to the ready mount first", RestoredDebtRoutesMountFirst);
            runner.Run("a click whose own Run carried another command leaves nothing to borrow", ClickBoundToItsOwnRun);
            runner.Run("an unbound click observation expires at the next frame's Run", StaleObservationExpires);
            runner.Run("two pairs keep separate click ownership across selection changes", ClickOwnershipIsPairLocal);
            runner.Run("hidden-mount feedback belongs only to a real mount click", HiddenMountFeedbackOnlyForClicks);
            runner.Run("mounted stock attack sequences rider before mount", SequencesRiderBeforeMount);
            runner.Run("mounted ranged stock attack never pulls mount into melee", RangedDoesNotForceMountMelee);
            runner.Run("mounted stock attack waits persistently only in real time", WaitsPersistentlyOnlyInRealTime);
            runner.Run("mounted stock attack cancels invalid target exactly", CancelsInvalidTarget);
            runner.Run("a dispatched rider or mount attack ends quietly when its native Standard is spent", CompletedAttackFeedback);
            runner.Run("a fresh spent-actor click keeps its refusal without using the partner's action", FreshSpentActorFeedback);
            runner.Run("same-target continuation does not turn attack completion into a new refusal", ContinuedAttackFeedback);
            runner.Run("Stop retires feedback and a fresh click on the same target earns its own answer", CancelledAttackFeedback);
            runner.Run("retarget and action-context replacement restore fresh-request feedback", ReplacementAttackFeedback);
            runner.Run("automatic attack proposals and continuations produce no player refusal", AutomaticAttackFeedback);
            runner.Run("promoting an automatic intent preserves whether an attack was already dispatched", PromotedAttackFeedback);
            runner.Run("one mounted pair's completed attack does not hide another pair's fresh refusal", PairLocalAttackFeedback);
            runner.Run("a later real-time dispatch refusal is quiet until a new player request", RealTimeContinuationFeedback);
            runner.Run("a later dispatch fault or configuration refusal still answers after an accepted attack", LaterDispatchFaultFeedback);
        }

        // The refusal the actual evaluator returns for an otherwise eligible rider attack with one fact changed.
        private static MountedCombatActionAvailability Refusal(Action<MountedCombatActionContext> change)
        {
            var context = MountedCombatDomainTests.Eligible(MountedCombatActionKind.RiderMelee);
            change(context);
            var availability = MountedCombatActionEvaluator.Evaluate(context);
            TestRunner.True(!availability.IsAllowed, "The changed fact did not refuse the attack.");
            return availability;
        }

        // Offline intent/feedback behavior. Single and full native routines share this dispatch/terminal
        // sequence; these checks do not execute a game command or claim native resource-spending evidence.
        private static void CompletedAttackFeedback()
        {
            foreach (var mountActor in new[] { false, true })
            {
                var intent = new MountedAttackIntent<object, object>();
                var target = new object(); var turn = new object();
                intent.Begin(target, turn, mountActor, true);
                TestRunner.Equal(mountActor ? MountedStockAttackDecision.DispatchMount : MountedStockAttackDecision.DispatchRider,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, true, true, false, true,
                        riderIsLegalActor: !mountActor, mountIsLegalActor: mountActor), "The requested actor did not receive its attack.");
                intent.ObserveDispatch(mountActor);
                // The command has ended, its slot is no longer active, and its actor's Standard is spent.
                // Its partner remains ready; this player's intent must not spend that partner's action.
                TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, mountActor, !mountActor, false, true,
                        riderIsLegalActor: !mountActor, mountIsLegalActor: mountActor), "A completed attack dispatched its partner or stayed pending.");
                var refusal = UnifiedMountedStockAttackPolicy.NoStandardFeedback(
                    mountActor ? "Greased Lightning" : "Royon", mountActor ? "Royon" : "Greased Lightning", true);
                TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "Ordinary attack completion displayed a spent-action refusal.");
                TestRunner.True(intent.CanContinue(target, turn, mountActor), "Reading feedback changed the accepted intent.");
            }
        }

        private static void FreshSpentActorFeedback()
        {
            foreach (var mountActor in new[] { false, true })
            {
                var intent = new MountedAttackIntent<object, object>();
                intent.Begin(new object(), new object(), mountActor, true);
                TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, mountActor, !mountActor, false, true,
                        riderIsLegalActor: !mountActor, mountIsLegalActor: mountActor), "A fresh spent-actor request used its ready partner's action.");
                var refusal = UnifiedMountedStockAttackPolicy.NoStandardFeedback(
                    mountActor ? "Greased Lightning" : "Royon", mountActor ? "Royon" : "Greased Lightning", true);
                TestRunner.Equal(refusal, intent.GetRejectionFeedback(refusal), "A genuinely new invalid click lost its actor-specific explanation.");
                intent.Cancel();
                TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "A retired click could produce another refusal.");
            }
        }

        private static void ContinuedAttackFeedback()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object(); var rider = new object(); var mount = new object();
            var weapon = new object(); var mountWeapon = new object();
            intent.Begin(target, turn, false, true, rider, mount, weapon, mountWeapon, 1);
            var generation = intent.Generation;
            intent.ObserveDispatch(false);
            TestRunner.True(intent.CanContinue(target, turn, false, rider, mount, weapon, mountWeapon, 1), "The repeated target did not preserve its current attack.");
            intent.PromoteToPlayer();
            TestRunner.Equal(null, intent.GetRejectionFeedback("The action actor has no Standard action available."),
                "A repeated same-target click made the already dispatched attack warn on completion.");
            TestRunner.True(intent.Owns(target, generation), "Repeated input replaced the accepted generation.");
        }

        private static void CancelledAttackFeedback()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            const string refusal = "Royon has no Standard action left this turn.";
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "An idle pair produced a player refusal.");
            intent.Begin(target, turn, false, true);
            intent.ObserveDispatch(false);
            intent.Cancel();
            intent.ObserveDispatch(false); // A late observation without a live intent must not revive it.
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "A cancelled attack revived its feedback.");
            intent.Begin(target, turn, false, true);
            TestRunner.Equal(refusal, intent.GetRejectionFeedback(refusal), "Stop followed by a new click inherited the old completion's silence.");
        }

        private static void ReplacementAttackFeedback()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var next = new object(); var turn = new object();
            var rider = new object(); var mount = new object(); var weapon = new object(); var mountWeapon = new object();
            const string refusal = "The target is not currently attackable.";
            intent.Begin(target, turn, false, true, rider, mount, weapon, mountWeapon, 1);
            intent.ObserveDispatch(false);
            TestRunner.True(!intent.CanContinue(next, turn, false, rider, mount, weapon, mountWeapon, 1), "Retarget retained the old request.");
            intent.Begin(next, turn, false, true, rider, mount, weapon, mountWeapon, 1);
            TestRunner.Equal(refusal, intent.GetRejectionFeedback(refusal), "A new target inherited the old request's dispatch history.");
            intent.ObserveDispatch(false);
            TestRunner.True(!intent.CanContinue(next, turn, false, rider, mount, weapon, mountWeapon, 2), "A changed action context retained the old request.");
            intent.Begin(next, turn, false, true, rider, mount, weapon, mountWeapon, 2);
            TestRunner.Equal(refusal, intent.GetRejectionFeedback(refusal), "A new action context inherited the old request's dispatch history.");
        }

        private static void AutomaticAttackFeedback()
        {
            var intent = new MountedAttackIntent<object, object>();
            const string refusal = "The action actor has no Standard action available.";
            intent.Begin(new object(), null, false, true, automatic: true);
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "An automatic proposal produced player feedback before dispatch.");
            intent.ObserveDispatch(false);
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "Automatic continuation produced player feedback after dispatch.");
            intent.Cancel();
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "A retired automatic intent produced player feedback.");
        }

        private static void PromotedAttackFeedback()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object();
            const string refusal = "The target is not currently attackable.";
            intent.Begin(target, null, false, true, automatic: true);
            intent.PromoteToPlayer();
            TestRunner.Equal(refusal, intent.GetRejectionFeedback(refusal), "A player request that never dispatched lost its explanation after promotion.");
            intent.Begin(target, null, false, true, automatic: true);
            intent.ObserveDispatch(false);
            intent.PromoteToPlayer();
            TestRunner.Equal(null, intent.GetRejectionFeedback(refusal), "Promoting an already dispatched attack made its later completion a new refusal.");
        }

        private static void PairLocalAttackFeedback()
        {
            var first = new MountedAttackIntent<object, object>(); var second = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            const string refusal = "The action actor has no Standard action available.";
            first.Begin(target, turn, false, true);
            second.Begin(target, turn, true, true);
            first.ObserveDispatch(false);
            TestRunner.Equal(null, first.GetRejectionFeedback(refusal), "The first pair warned after its attack.");
            TestRunner.Equal(refusal, second.GetRejectionFeedback(refusal), "The first pair's attack hid the second pair's fresh refusal.");
            second.ObserveDispatch(true);
            first.Cancel();
            first.Begin(target, turn, false, true);
            TestRunner.Equal(refusal, first.GetRejectionFeedback(refusal), "The second pair's attack hid the first pair's new refusal.");
            TestRunner.Equal(null, second.GetRejectionFeedback(refusal), "Restarting the first pair revived the second pair's completion warning.");
        }

        private static void RealTimeContinuationFeedback()
        {
            var target = new object();
            var routine = new Action<MountedCombatActionContext>[]
            {
                c => c.ActionActorHasStandardAction = false,
                c => c.TargetAttackable = false,
                c => c.TargetVisible = false,
                c => c.ActionActorOwnsCurrentTurnOrRealTime = false,
                c => c.TransactionIdle = false,
                c => c.PathKnownUnavailable = true,
                c => c.WithinSupportedRangeEnvelope = false,
                c => c.ExactRiderSelection = false
            };
            foreach (var change in routine)
            {
                var refusal = Refusal(change);
                var intent = new MountedAttackIntent<object, object>();
                intent.Begin(target, null, false, true);
                intent.ObserveDispatch(false);
                TestRunner.Equal(MountedStockAttackDecision.Wait,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, false, false, true),
                    "Native cooldown ended the real-time intent.");
                TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                    UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, false, false, true),
                    "A ready actor could not resume its real-time intent.");
                // A later admission may refuse after the decision; it is not a new click from the player.
                TestRunner.Equal(null, intent.GetDispatchRejectionFeedback(refusal.Feedback, refusal.RejectionCodes),
                    "A later routine refusal warned for the original successful click: " + refusal.Feedback);
                intent.Cancel();
                intent.Begin(target, null, false, true);
                TestRunner.Equal(refusal.Feedback, intent.GetDispatchRejectionFeedback(refusal.Feedback, refusal.RejectionCodes),
                    "A new real-time player request lost its rejection feedback: " + refusal.Feedback);
            }
        }

        private static void LaterDispatchFaultFeedback()
        {
            var target = new object();
            var meaningful = new Action<MountedCombatActionContext>[]
            {
                c => c.CommandAdmissionReady = false,
                c => c.RangeOriginConsistent = false,
                c => c.FeatureEnabled = false,
                c => c.SupportedMountProfile = false,
                c => c.RiderHasEligibleWeapon = false,
                c => c.RiderWeaponIsRanged = true,
                // A routine fact beside a fault still reports the fault.
                c => { c.ActionActorHasStandardAction = false; c.CommandAdmissionReady = false; }
            };
            foreach (var change in meaningful)
            {
                var refusal = Refusal(change);
                var intent = new MountedAttackIntent<object, object>();
                intent.Begin(target, null, false, true);
                intent.ObserveDispatch(false);
                TestRunner.Equal(refusal.Feedback, intent.GetDispatchRejectionFeedback(refusal.Feedback, refusal.RejectionCodes),
                    "A fault after an accepted attack lost its explanation: " + refusal.Feedback);
                // An automatic intent never answers a player; the controller logs the refusal instead.
                intent.Begin(target, null, false, true, automatic: true);
                intent.ObserveDispatch(false);
                TestRunner.Equal(null, intent.GetDispatchRejectionFeedback(refusal.Feedback, refusal.RejectionCodes),
                    "An automatic continuation produced player feedback: " + refusal.Feedback);
                intent.Cancel();
                TestRunner.Equal(null, intent.GetDispatchRejectionFeedback(refusal.Feedback, refusal.RejectionCodes),
                    "A retired intent produced player feedback.");
            }
            // A scheduler or command-container admission failure reports this code alone.
            const string admission = "Mounted pair command failed to enter the action actor Standard slot.";
            var later = new MountedAttackIntent<object, object>();
            later.Begin(target, null, false, true);
            later.ObserveDispatch(true);
            TestRunner.Equal(admission, later.GetDispatchRejectionFeedback(admission, new[] { MountedCombatRejectionCode.CommandAdmissionFailure }),
                "A native admission failure after an accepted attack was hidden.");
            TestRunner.Equal(admission, later.GetDispatchRejectionFeedback(admission, new MountedCombatRejectionCode[0]),
                "An uncoded refusal after an accepted attack was treated as routine.");
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

        private static void AnimatedSaddleDeclaredHorseBasis()
        {
            // Measured preview.3 sample: the basis HorseAnimatedSeatCorrection was tuned against.
            var chest = new PoseVector3(0f, 1.5024f, 0.4674f);
            var idlePelvis = new PoseVector3(-0.1892f, 2.3625f, 0.4849f);
            // A first-mount sample taken mid-cast or while an approach walk blends out: pelvis higher and displaced.
            var castPelvis = new PoseVector3(-0.05f, 2.6125f, 0.60f);
            var declared = (PoseVector3?)AnimatedSaddleCalibration.MediumHumanoidHorseSeatFromSource;
            PoseVector3 Seat(PoseVector3 observed, PoseVector3? basis, PoseVector3 animatedChest)
            {
                var saddle = new AnimatedSaddlePosition();
                saddle.Acquire(chest, AnimatedSaddleCalibration.CalibrationPelvis(chest, observed, basis), MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
                return saddle.Project(animatedChest);
            }
            var accepted = Seat(idlePelvis, null, chest);
            foreach (var animatedChest in new[] { chest, new PoseVector3(0.03f, 1.62f, 0.41f) })
            {
                var first = Seat(castPelvis, declared, animatedChest);
                var remount = Seat(idlePelvis, declared, animatedChest);
                TestRunner.True((first - remount).Magnitude < 0.00001f, "First mount and remount calibrated different Horse seats.");
            }
            TestRunner.True((Seat(castPelvis, declared, chest) - accepted).Magnitude < 0.0005f, "The declared Horse basis moved the accepted seat.");
            // Without a declaration (Mammoth) the observed sample is retained: the pose-dependent height the Horse no longer takes.
            TestRunner.True(Seat(castPelvis, null, chest).Y - accepted.Y > 0.2f, "The observed path no longer reflects its sample.");
            // Release and reacquire through the same policy inherit nothing.
            var reused = new AnimatedSaddlePosition();
            reused.Acquire(chest, AnimatedSaddleCalibration.CalibrationPelvis(chest, castPelvis, null), MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
            reused.Release();
            reused.Acquire(chest, AnimatedSaddleCalibration.CalibrationPelvis(chest, castPelvis, declared), MountedRiderPoseProfiles.HorseAnimatedSeatCorrection);
            TestRunner.True((reused.Project(chest) - accepted).Magnitude < 0.0005f, "A remount inherited the earlier pose-dependent calibration.");
        }

        private static void FollowThroughEligibility()
        {
            // The production dispatcher admission (the action evaluator) decides each actor's eligibility.
            bool Admitted(MountedCombatActionKind action, Action<MountedCombatActionContext> change)
            {
                var context = MountedCombatDomainTests.Eligible(action);
                change(context);
                return MountedCombatActionEvaluator.Evaluate(context).IsAllowed;
            }
            MountedStockAttackDecision Next(bool riderEligible, bool mountEligible, bool melee = true, bool overridden = false, bool explicitMount = false) =>
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, riderEligible, mountEligible, !melee, true,
                    riderIsLegalActor: !explicitMount,
                    mountIsLegalActor: UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, explicitMount, false, true, false, true, melee, overridden));
            var ready = Admitted(MountedCombatActionKind.RiderMelee, context => { });
            var endedRider = Admitted(MountedCombatActionKind.RiderMelee, context => context.ActionActorOwnsCurrentTurnOrRealTime = false);
            var prohibitedRider = Admitted(MountedCombatActionKind.RiderMelee, context => context.ActionActorCapabilityBlock = MountedActorCapabilityBlock.CanNotAttack);
            var horse = Admitted(MountedCombatActionKind.MountPrimaryNatural, context => { });
            var prohibitedHorse = Admitted(MountedCombatActionKind.MountPrimaryNatural, context => context.ActionActorCapabilityBlock = MountedActorCapabilityBlock.CanNotAttack);
            TestRunner.True(ready && horse && !endedRider && !prohibitedRider && !prohibitedHorse, "The evaluator inputs did not behave as dispatch admission.");
            // R1-F6: an individually ended rider, or one that may not attack despite a Standard, leaves the eligible horse its attack.
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, Next(endedRider, horse), "An ended rider discarded the horse's attack.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, Next(prohibitedRider, horse), "A prohibited rider discarded the horse's attack.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider, Next(ready, prohibitedHorse), "The reciprocal: the rider lost its attack.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(prohibitedRider, prohibitedHorse), "A prohibited horse attacked.");
            // R1-F4/R2-F1: the order's accepted kind (fixed at acceptance) scopes participation: a bow order never spends the horse,
            // even if the rider changes to a melee weapon before the next decision; a melee order reaches the eligible horse once.
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(false, horse, melee: false), "A retained bow order spent the horse.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, Next(false, horse), "The melee order did not reach the eligible horse.");
            // R1-F5: newer player work recorded for the order (whether still queued or already finished) keeps the horse out;
            // the pair's own command, reactions and coexisting Free/Swift actions do not override.
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(false, horse, overridden: true), "A newer order was overridden.");
            // How the controller derives "melee" and "overridden" (the retained-order ledger at acceptance and at native
            // acceptance) is exercised in MountedAlpha2LifecycleTests.
            TestRunner.True(UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, true, false, true, false, true, false, true), "An explicit mount order lost its meaning.");
        }

        private static void HorseStrikeLines()
        {
            var bite = HorseStrikeDiagnostic.Format("horse-1", 1, 3, "Bite1d4", "Bite", true, "HorseAnimationSet_Bite", true, true, false, 2.4f, 0, 1,
                "BiteAttack", 0.8f, "Playing", "kobold");
            TestRunner.True(bite.Contains("horse=horse-1") && bite.Contains("handle=started,acted") && bite.Contains("action=HorseAnimationSet_Bite") &&
                bite.Contains("speedScale=2.4") && bite.Contains("activeClip=BiteAttack") && bite.Contains("clipWeight=0.8") &&
                bite.Contains("animationState=Playing") && bite.Contains("index=1/3") && !bite.Contains("playback"), "A stock Bite handle was not stated literally: " + bite);
            var missing = HorseStrikeDiagnostic.Format("horse-1", 1, 3, "Bite1d4", "Bite", false, null, false, false, false, 0f, 0, 0, null, 0f, null, "kobold");
            TestRunner.True(missing.Contains("handle=none") && !missing.Contains("activeClip"), "A missing Bite handle was not identifiable: " + missing);
            TestRunner.Equal("started,not-acted", HorseStrikeDiagnostic.HandleState(true, true, false, false), "An unacted handle was misstated.");
            TestRunner.Equal("started,acted,interrupted", HorseStrikeDiagnostic.HandleState(true, true, true, true), "An interrupted handle was misstated.");
            var hoof = HorseStrikeDiagnostic.Format("horse-1", 2, 3, "Hoof1d4", "Hoof", true, "HorseAnimationSet_Hoof", true, true, false, 2.4f, 1, 2,
                "Left_Hoof_Attack", 1f, "Playing", "kobold");
            TestRunner.True(hoof.Contains("special=Hoof") && hoof.Contains("activeClip=Left_Hoof_Attack") && hoof.Contains("index=2/3"),
                "The hoof control was not distinguishable: " + hoof);
        }

        private sealed class LoadedUnit { public LoadedUnit(string id) { Id = id; } public string Id { get; } public int Stale = 1; }

        private static void SavedHorseRepairLedger()
        {
            var ledger = new LoadedInstanceRepairLedger<LoadedUnit>();
            var removals = 0;
            Func<LoadedUnit, int> repair = unit => { var n = unit.Stale; unit.Stale = 0; removals += n; return n; };
            int changed;
            var first = new LoadedUnit("horse-1");
            // The composition root's write gate (shared with Mounted Shield) through disable, save suspension, an area transition,
            // a pending restoration, a save worker and an owned load, plus a native load: zero removals, object still pending.
            bool Admitted(bool enabled = true, bool suspended = false, bool area = false, bool restoration = false, bool worker = false,
                bool load = false, bool nativeLoading = false) =>
                HorseInheritedFactRepairPolicy.MayRepair(true, !nativeLoading,
                    !MountedWorldWriteGate.Allows(enabled, suspended, area, restoration, worker, load));
            foreach (var admitted in new[] { Admitted(enabled: false), Admitted(suspended: true), Admitted(area: true),
                Admitted(restoration: true), Admitted(worker: true), Admitted(load: true), Admitted(nativeLoading: true) })
            {
                TestRunner.True(!ledger.TryRun(first, admitted, repair, out changed) && changed == 0 && removals == 0 && !ledger.IsCompleted(first),
                    "A protected phase repaired or completed the Horse.");
            }
            TestRunner.True(Admitted() && !HorseInheritedFactRepairPolicy.MayRepair(false, true, false), "The settled gate or a disposed service was misread.");
            // A failed pass leaves the object pending; the next settled update retries it once.
            var failed = false;
            try { ledger.TryRun(first, true, unit => { throw new InvalidOperationException("simulated"); }, out changed); }
            catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && !ledger.IsCompleted(first) && removals == 0, "A failed pass was recorded as complete.");
            TestRunner.True(ledger.TryRun(first, Admitted(), repair, out changed) && changed == 1 &&
                ledger.IsCompleted(first), "The settled retry did not repair the Horse.");
            TestRunner.True(!ledger.TryRun(first, true, repair, out changed) && removals == 1, "A completed Horse was repaired twice.");
            // A reload: a new object with the same stable ID is examined again, independently of the old one.
            var reloaded = new LoadedUnit("horse-1");
            TestRunner.True(!ledger.IsCompleted(reloaded) && ledger.TryRun(reloaded, true, repair, out changed) && changed == 1 && removals == 2,
                "A reloaded Horse with the same ID was skipped.");
        }

        private static void PairOrderHorseFollowThrough()
        {
            // The legality input comes from a genuine paired activation's CanAddress, as the controller passes it.
            MountedStockAttackDecision Next(PairedActivation<object, object> pair, object boundary, bool explicitMount, bool automatic,
                bool pairedActivation, bool riderStandard, bool mountStandard) =>
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, riderStandard, mountStandard, false, true,
                    riderIsLegalActor: !explicitMount,
                    mountIsLegalActor: UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, explicitMount, false, pairedActivation, automatic,
                        pair.CanAddress(pair.Partner, boundary)));
            PairedActivation<object, object> Prepared(out object boundary)
            {
                var pair = new PairedActivation<object, object>(new object(), new object());
                boundary = new object();
                pair.Begin(boundary);
                pair.BeginActorPreparation(pair.Principal, boundary); pair.FinishActorPreparation(pair.Principal);
                pair.BeginActorPreparation(pair.Partner, boundary); pair.FinishActorPreparation(pair.Partner);
                return pair;
            }
            object turn;
            var active = Prepared(out turn);
            // One rider click: the rider strikes first, then (rider spent) the horse follows through, then the intent completes.
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider, Next(active, turn, false, false, true, true, true), "The rider did not strike first.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, Next(active, turn, false, false, true, false, true), "The ready horse did not follow the pair order.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(active, turn, false, false, true, false, false), "A spent horse was asked to attack again.");
            // An ended horse (used, End, or adopted mid-encounter with its participation spent) is never reactivated.
            active.EndActor(active.Partner);
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(active, turn, false, false, true, false, true), "An ended horse attacked.");
            var adopted = new PairedActivation<object, object>(new object(), new object()); var adoptedTurn = new object();
            adopted.AdoptRunningBoundary(adoptedTurn, MidEncounterAdoption.RetainPartnerParticipation);
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(adopted, adoptedTurn, false, false, true, false, true), "An already-used adopted partner attacked.");
            // Not the activation's boundary (another round), unprepared, suspended (delay), ending (End Turn) or split: no follow-through.
            var other = Prepared(out turn);
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(other, new object(), false, false, true, false, true), "A horse attacked across the activation boundary.");
            other.Suspend(turn);
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(other, turn, false, false, true, false, true), "A suspended horse attacked.");
            var ending = Prepared(out turn); ending.BeginEnding();
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(ending, turn, false, false, true, false, true), "A horse attacked after End Turn.");
            var split = Prepared(out turn); split.Detach();
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(split, turn, false, false, true, false, true), "A split horse attacked.");
            var unprepared = new PairedActivation<object, object>(new object(), new object()); var early = new object(); unprepared.Begin(early);
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(unprepared, early, false, false, true, false, true), "An unprepared horse attacked.");
            // Explicit per-actor orders keep their meaning; an automatic intent and separate turns never spend the partner.
            var fresh = Prepared(out turn);
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, Next(fresh, turn, true, false, true, true, true), "An explicit mount order did not reach the horse.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(fresh, turn, true, false, true, true, false), "An explicit mount order spent the rider.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(fresh, turn, false, true, true, false, true), "An automatic intent spent the horse.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, Next(fresh, turn, false, false, false, false, true), "A separate-turn rider order spent the horse.");
            TestRunner.True(UnifiedMountedStockAttackPolicy.MountJoinsIntent(false, false, false, false, true, false), "Real time lost its pair dispatch.");
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

        // Sprint 1 UPG-01 (r15): a cold-loaded rider carries its saved Standard cooldown; the same RT stock click
        // that dispatches a ready rider first dispatches the ready mount instead, and waits when neither is ready.
        private static void RestoredDebtRoutesMountFirst()
        {
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, true, false, false), "Restored rider debt did not route the ready melee mount first.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, true, true, false, false), "A ready rider lost its first attack.");
            TestRunner.Equal(MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, false, false, false), "Two spent actors did not wait.");
            TestRunner.Equal(MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, true, true, false), "A ranged rider with its mount out of reach did not wait.");
            TestRunner.Equal(MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, true, false, false, true, false, false), "An active pair command did not wait.");
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
